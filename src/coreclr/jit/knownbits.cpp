// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "knownbits.h"

//------------------------------------------------------------------------
// MergeKnownBitsAssertions: Refine "*pBits" using whatever the live assertions tell us about "num".
//
// Arguments:
//    comp       - the compiler context
//    num        - the value number being analyzed
//    assertions - the assertion set live at the consumer
//    width      - bit width (32 or 64) of "num"
//    budget     - recursive search budget (currently unused here, kept for symmetry with Compute)
//    pBits      - in/out: the lattice for "num" so far; refined in place by intersecting with each
//                 fact this routine can extract from the assertion table
//
static void MergeKnownBitsAssertions(
    Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, unsigned width, int /*budget*/, KnownBits* pBits)
{
    if (BitVecOps::MayBeUninit(assertions) || BitVecOps::IsEmpty(comp->apTraits, assertions) ||
        !comp->optAssertionHasAssertionsForVN(num))
    {
        return;
    }

    const uint64_t  signBit = 1ull << (width - 1);
    const KnownBits signBitZero(signBit, 0);

    // Tightest signed upper bound "num <= signedUpperBound" gathered from signed "num < C" / "num <= C"
    // assertions with a non-negative bound. On its own a signed upper bound says nothing about the high
    // bits (num could be negative), so we only apply it after the loop, and only once we also know num
    // is non-negative (sign bit 0) -- then num is in [0, signedUpperBound] and its upper bits are 0.
    bool     haveSignedUpperBound = false;
    uint64_t signedUpperBound     = 0;

    BitVecOps::Iter iter(comp->apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        const Compiler::AssertionDsc& cur = comp->optGetAssertion(GetAssertionIndex(index));
        if (cur.GetOp1().GetVN() != num)
        {
            continue;
        }

        // "num == const": fully determines the bits.
        if (cur.KindIs(Compiler::OAK_EQUAL))
        {
            int64_t eqCns;
            if (comp->vnStore->IsVNIntegralConstant<int64_t>(cur.GetOp2().GetVN(), &eqCns))
            {
                *pBits = KnownBits::Intersect(*pBits, KnownBits::FromConstant((uint64_t)eqCns, width));
            }
            continue;
        }

        // Relops of the form "num <relop> const".
        if (cur.IsRelop() && cur.GetOp2().KindIs(Compiler::O2K_CONST_INT))
        {
            const int64_t relCns = cur.GetOp2().GetIntConstant();

            if (cur.KindIs(Compiler::OAK_LT_UN) && (relCns > 0))
            {
                // (uint)num < C  =>  num u<= C-1  =>  upper bits are 0.
                *pBits = KnownBits::Intersect(*pBits, KnownBits::FromUnsignedUpperBound((uint64_t)(relCns - 1), width));
            }
            else if (cur.KindIs(Compiler::OAK_LE_UN) && (relCns >= 0))
            {
                // (uint)num <= C  =>  upper bits are 0.
                *pBits = KnownBits::Intersect(*pBits, KnownBits::FromUnsignedUpperBound((uint64_t)relCns, width));
            }
            else if (cur.KindIs(Compiler::OAK_GE) && (relCns >= 0))
            {
                // num >= 0 (signed)  =>  sign bit is 0.
                *pBits = KnownBits::Intersect(*pBits, signBitZero);
            }
            else if (cur.KindIs(Compiler::OAK_GT) && (relCns >= -1))
            {
                // num > -1 (signed)  =>  num >= 0  =>  sign bit is 0.
                *pBits = KnownBits::Intersect(*pBits, signBitZero);
            }
            else if (cur.KindIs(Compiler::OAK_LT) && (relCns >= 1))
            {
                // num < C (signed), C >= 1. If num is also non-negative (handled after the loop),
                // num is in [0, C-1], so record C-1 as a candidate upper bound.
                const uint64_t ub = (uint64_t)(relCns - 1);
                if (!haveSignedUpperBound || (ub < signedUpperBound))
                {
                    haveSignedUpperBound = true;
                    signedUpperBound     = ub;
                }
            }
            else if (cur.KindIs(Compiler::OAK_LE) && (relCns >= 0))
            {
                // num <= C (signed), C >= 0. If num is also non-negative, num is in [0, C].
                const uint64_t ub = (uint64_t)relCns;
                if (!haveSignedUpperBound || (ub < signedUpperBound))
                {
                    haveSignedUpperBound = true;
                    signedUpperBound     = ub;
                }
            }
            continue;
        }

        // "(uint)num </<= (vn + cns)" where (vn + cns) is non-negative => num is non-negative.
        //
        // IsVNNeverNegative on an O2K_VN_ADD_CNS asserts only that the "vn" part is non-negative.
        // The full expression "vn + cns" can only be guaranteed non-negative when cns == 0, so we
        // require it explicitly here -- otherwise a negative cns could make the bound itself
        // negative and we'd derive a false non-negativity fact for num. Same shape as rangecheck.cpp.
        //
        if (cur.KindIs(Compiler::OAK_LT_UN, Compiler::OAK_LE_UN) && cur.GetOp2().KindIs(Compiler::O2K_VN_ADD_CNS) &&
            cur.GetOp2().IsVNNeverNegative() && (cur.GetOp2().GetCns() == 0))
        {
            *pBits = KnownBits::Intersect(*pBits, signBitZero);
        }
    }

    // If we gathered a signed upper bound and num is now known non-negative (from any of the facts
    // above or from its value-number structure), num is in [0, signedUpperBound]: its upper bits are 0.
    // Example: "a > 10 && a < 1000" => sign bit 0 (from a > 10) plus upper bits 0 (from a < 1000),
    // proving a fits in a smaller type (e.g. making "checked((int)a)" non-overflowing).
    if (haveSignedUpperBound && ((pBits->knownZero & signBit) != 0))
    {
        *pBits = KnownBits::Intersect(*pBits, KnownBits::FromUnsignedUpperBound(signedUpperBound, width));
    }
}

//------------------------------------------------------------------------
// ComputeWorker: Recursive worker for KnownBits::Compute.
//
// Arguments:
//    comp       - the compiler context
//    num        - the value number to analyze
//    assertions - the assertion set live at the consumer
//    budget     - recursive search budget; decremented at every recursive step. Returns the
//                 fully-unknown lattice when the budget is exhausted.
//    visited    - set of phi VNs we have already entered, used to guard against infinite recursion
//                 on loop-carried phis
//
// Returns:
//    KnownBits for "num" within its natural width (32 or 64). Always truncated to that width on
//    return so the "bits above width are 0/0" invariant holds.
//
static KnownBits ComputeWorker(
    Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, int budget, ValueNumStore::SmallValueNumSet* visited)
{
    KnownBits result;
    if ((num == ValueNumStore::NoVN) || (budget <= 0))
    {
        return result;
    }

    const var_types vnType = comp->vnStore->TypeOfVN(num);
    if (!varTypeIsIntegral(vnType) || varTypeIsGC(vnType))
    {
        // We only reason about (non-GC) integral values.
        return result;
    }

    const unsigned width = (genActualType(vnType) == TYP_LONG) ? 64 : 32;

    // Constants are fully known.
    int64_t cnsVal;
    if (comp->vnStore->IsVNIntegralConstant<int64_t>(num, &cnsVal))
    {
        return KnownBits::FromConstant((uint64_t)cnsVal, width);
    }

    VNFuncApp f;
    if (comp->vnStore->GetVNFunc(num, &f))
    {
        switch (f.GetFunc())
        {
            case VNF_AND:
            case VNF_OR:
            case VNF_UDIV:
            {
                const KnownBits a = ComputeWorker(comp, f.GetArg(0), assertions, --budget, visited);
                const KnownBits b = ComputeWorker(comp, f.GetArg(1), assertions, --budget, visited);

                if (f.FuncIs(VNF_UDIV))
                    result = KnownBitsOps::UDiv(a, b, width);
                else if (f.FuncIs(VNF_AND))
                    result = KnownBitsOps::And(a, b);
                else if (f.FuncIs(VNF_OR))
                    result = KnownBitsOps::Or(a, b);
                else
                    unreached();
                break;
            }

            case VNF_Cast:
            case VNF_CastOvf:
            {
                var_types castToType;
                bool      srcIsUnsigned;
                comp->vnStore->GetCastOperFromVN(f.GetArg(1), &castToType, &srcIsUnsigned);

                const ValueNum  srcVN   = f.GetArg(0);
                const var_types srcType = comp->vnStore->TypeOfVN(srcVN);
                if (varTypeIsIntegral(srcType) && !varTypeIsGC(srcType) && varTypeIsIntegral(castToType))
                {
                    const unsigned  srcWidth = genTypeSize(genActualType(srcType)) * BITS_PER_BYTE;
                    const KnownBits bits     = ComputeWorker(comp, srcVN, assertions, --budget, visited);
                    result                   = KnownBitsOps::Cast(bits, srcWidth, castToType, srcIsUnsigned);
                }
                break;
            }

            case VNF_EQ:
            case VNF_NE:
            case VNF_LT:
            case VNF_LE:
            case VNF_GT:
            case VNF_GE:
            case VNF_LT_UN:
            case VNF_LE_UN:
            case VNF_GT_UN:
            case VNF_GE_UN:
                // A relop always produces 0 or 1; we don't try to fold the comparison here, just
                // record the [0, 1] range so a consumer reading this VN sees a single low bit.
                result = KnownBits::FromUnsignedUpperBound(1, width);
                break;

            case VNF_MDARR_LENGTH:
            case VNF_ARR_LENGTH:
                // Array length is in [0, CORINFO_Array_MaxLength], so its upper bits are 0.
                result = KnownBits::FromUnsignedUpperBound(CORINFO_Array_MaxLength, width);
                break;

            default:
                break;
        }
    }

    result = result.Truncate(width);

    // Phi: a bit is known in the phi result only if it is known and equal along every reaching
    // edge. We Union (LLVM's intersectWith) the per-edge KnownBits to compute that.
    if (!result.IsConstant(width) && comp->vnStore->IsPhiDef(num) && visited->Add(comp, num))
    {
        KnownBits phiBits;
        bool      first   = true;
        auto      visitor = [comp, &phiBits, &first, &budget, visited](ValueNum vn, ASSERT_TP reachAss) {
            const KnownBits edge = ComputeWorker(comp, vn, reachAss, --budget, visited);
            phiBits              = first ? edge : KnownBits::Union(phiBits, edge);
            first                = false;

            // Once nothing is known, merging more edges cannot recover any information.
            return phiBits.IsUnknown() ? Compiler::AssertVisit::Abort : Compiler::AssertVisit::Continue;
        };
        if ((comp->optVisitReachingAssertions(num, visitor) == Compiler::AssertVisit::Continue) && !first)
        {
            result = KnownBits::Intersect(result, phiBits);
        }
    }

    MergeKnownBitsAssertions(comp, num, assertions, width, budget, &result);
    return result.Truncate(width);
}

//------------------------------------------------------------------------
// KnownBits::Compute: Entry point for the bit-level analog of
//    RangeCheck::GetRangeFromAssertions. Returns which bits of "num" are known 0/1, derived from
//    its value-number structure and the incoming assertions. Supports 32- and 64-bit integral VNs;
//    on unsupported types returns the fully-unknown lattice.
//
// See KnownBits::Compute in knownbits.h for the parameter documentation.
//
KnownBits KnownBits::Compute(Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, int budget)
{
    ValueNumStore::SmallValueNumSet visited;
    return ComputeWorker(comp, num, assertions, budget, &visited);
}
