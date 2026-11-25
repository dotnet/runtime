// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "knownbitsanalysis.h"

//------------------------------------------------------------------------
// Bounds: Create the bounds that the known bits of a value imply.
//
// Arguments:
//    known - the known bits
//
KnownBitsAnalysis::Bounds::Bounds(const KnownBits& known)
    : UMin(known.getMinValue())
    , UMax(known.getMaxValue())
    , SMin(known.getSignedMinValue())
    , SMax(known.getSignedMaxValue())
{
}

//------------------------------------------------------------------------
// IsUnknown: Check if nothing is known about the value.
//
// Return Value:
//    true if the bounds are those of the type.
//
bool KnownBitsAnalysis::Bounds::IsUnknown() const
{
    return UMin.isZero() && UMax.isAllOnes() && SMin.isMinSignedValue() && SMax.isMaxSignedValue();
}

//------------------------------------------------------------------------
// ApplyRelop: Refine the bounds given that "this relop other" holds.
//
// Arguments:
//    kind  - the kind of the assertion, the relop
//    other - the bounds of the other operand
//
// Notes:
//    The relops that can never be true mean that the code is unreachable, these are ignored.
//
void KnownBitsAnalysis::Bounds::ApplyRelop(Compiler::optAssertionKind kind, const Bounds& other)
{
    switch (kind)
    {
        case Compiler::OAK_EQUAL:
            UMin = APInt::umax(UMin, other.UMin);
            UMax = APInt::umin(UMax, other.UMax);
            SMin = APInt::smax(SMin, other.SMin);
            SMax = APInt::smin(SMax, other.SMax);
            break;
        case Compiler::OAK_LT_UN:
            if (!other.UMax.isZero())
            {
                UMax = APInt::umin(UMax, other.UMax - 1);
            }
            break;
        case Compiler::OAK_LE_UN:
            UMax = APInt::umin(UMax, other.UMax);
            break;
        case Compiler::OAK_GT_UN:
            if (!other.UMin.isAllOnes())
            {
                UMin = APInt::umax(UMin, other.UMin + 1);
            }
            break;
        case Compiler::OAK_GE_UN:
            UMin = APInt::umax(UMin, other.UMin);
            break;
        case Compiler::OAK_LT:
            if (!other.SMax.isMinSignedValue())
            {
                SMax = APInt::smin(SMax, other.SMax - 1);
            }
            break;
        case Compiler::OAK_LE:
            SMax = APInt::smin(SMax, other.SMax);
            break;
        case Compiler::OAK_GT:
            if (!other.SMin.isMaxSignedValue())
            {
                SMin = APInt::smax(SMin, other.SMin + 1);
            }
            break;
        case Compiler::OAK_GE:
            SMin = APInt::smax(SMin, other.SMin);
            break;
        case Compiler::OAK_NOT_EQUAL:
            // Only useful when the constant is one of the bounds.
            if (other.UMin == other.UMax)
            {
                const APInt& cns = other.UMin;
                if ((UMin == cns) && UMin.ult(UMax))
                {
                    UMin = UMin + 1;
                }
                else if ((UMax == cns) && UMin.ult(UMax))
                {
                    UMax = UMax - 1;
                }
                if ((SMin == cns) && SMin.slt(SMax))
                {
                    SMin = SMin + 1;
                }
                else if ((SMax == cns) && SMin.slt(SMax))
                {
                    SMax = SMax - 1;
                }
            }
            break;
        default:
            break;
    }
}

//------------------------------------------------------------------------
// SwapRelop: Get the relop that holds for "y relop' x" when "x relop y" holds.
//
static Compiler::optAssertionKind SwapRelop(Compiler::optAssertionKind kind)
{
    switch (kind)
    {
        case Compiler::OAK_LT:
            return Compiler::OAK_GT;
        case Compiler::OAK_LT_UN:
            return Compiler::OAK_GT_UN;
        case Compiler::OAK_LE:
            return Compiler::OAK_GE;
        case Compiler::OAK_LE_UN:
            return Compiler::OAK_GE_UN;
        case Compiler::OAK_GT:
            return Compiler::OAK_LT;
        case Compiler::OAK_GT_UN:
            return Compiler::OAK_LT_UN;
        case Compiler::OAK_GE:
            return Compiler::OAK_LE;
        case Compiler::OAK_GE_UN:
            return Compiler::OAK_LE_UN;
        default:
            return kind;
    }
}

//------------------------------------------------------------------------
// Normalize: Share the signed and unsigned bounds when that is possible.
//
// Notes:
//    A value that is known to be non-negative (or negative) is ordered the same way
//    by the signed and unsigned comparisons.
//
void KnownBitsAnalysis::Bounds::Normalize()
{
    if (UMax.isNonNegative() || UMin.isNegative())
    {
        SMin = APInt::smax(SMin, UMin);
        SMax = APInt::smin(SMax, UMax);
    }
    if (SMin.isNonNegative() || SMax.isNegative())
    {
        UMin = APInt::umax(UMin, SMin);
        UMax = APInt::umin(UMax, SMax);
        SMin = APInt::smax(SMin, UMin);
        SMax = APInt::smin(SMax, UMax);
    }
}

//------------------------------------------------------------------------
// KnownBitsAnalysis: Create an instance of the analysis, a query.
//
// Arguments:
//    comp     - the compiler instance
//    phiDepth - how many PHIs deep we are (used internally)
//
KnownBitsAnalysis::KnownBitsAnalysis(Compiler* comp, int phiDepth)
    : m_compiler(comp)
    , m_vnStore(comp->vnStore)
    , m_budget(Budget)
    , m_phiDepth(phiDepth)
    , m_maxPhiDepth(MaxPhiDepth)
    , m_followVNAssertions(true)
{
#ifdef DEBUG
    // JIT stress: look deeper to get more coverage of the analysis.
    if (comp->compStressCompile(Compiler::STRESS_KNOWNBITS_BUDGET, 50))
    {
        m_budget *= 16;
        m_maxPhiDepth *= 4;
    }
#endif
}

//------------------------------------------------------------------------
// EvalRelop: Evaluate a relop using the known bits and the bounds of its operands.
//
// Arguments:
//    relop      - the relop
//    assertions - the assertions that hold for the relop, can be uninit
//
// Return Value:
//    The result of the relop if it's the same for all the values its operands can have.
//
RelopEvaluationResult KnownBitsAnalysis::EvalRelop(GenTreeOp* relop, ASSERT_VALARG_TP assertions)
{
    assert(relop->OperIsCompare());
    GenTree* const op1 = relop->gtGetOp1();
    GenTree* const op2 = relop->gtGetOp2();

    // TODO-CQ: handle "op2" that is not a constant, that needs to be cheap: most of
    // the time we know nothing about it.
    const unsigned width = GetWidth(op1->TypeGet());
    if ((width == 0) || (GetWidth(op2->TypeGet()) != width) || !op2->IsIntegralConst() || op2->IsIconHandle())
    {
        return RelopEvaluationResult::Unknown;
    }

    const ValueNum op1VN = m_compiler->optConservativeNormalVN(op1);

    // TP: equality tests are too common to analyze all of them, and we rarely fold
    // anything but the tests of bit patterns (e.g. flag checks) and casts.
    if (relop->OperIs(GT_EQ, GT_NE) INDEBUG(&&!m_compiler->compStressCompile(Compiler::STRESS_KNOWNBITS_RELOPS, 50)))
    {
        VNFuncApp funcApp;
        if (!m_vnStore->GetVNFunc(op1VN, &funcApp) || !funcApp.FuncIs(VNF_AND, VNF_OR, VNF_XOR, VNF_Cast, VNF_CastOvf))
        {
            return RelopEvaluationResult::Unknown;
        }
    }

    Bounds          b1{KnownBits(width)};
    const KnownBits known1 = Compute(op1VN, width, assertions, &b1);
    if (known1.isUnknown() && b1.IsUnknown())
    {
        return RelopEvaluationResult::Unknown;
    }

    const APInt     cns(width, (uint64_t)op2->AsIntConCommon()->IntegralValue());
    const KnownBits known2 = KnownBits::makeConstant(cns);
    const Bounds    b2(known2);

    if (relop->OperIs(GT_EQ, GT_NE))
    {
        RelopEvaluationResult isEqual = KnownBits::eq(known1, known2);
        if ((isEqual == RelopEvaluationResult::Unknown) &&
            (cns.ult(b1.UMin) || cns.ugt(b1.UMax) || cns.slt(b1.SMin) || cns.sgt(b1.SMax)))
        {
            isEqual = RelopEvaluationResult::False;
        }
        if ((isEqual == RelopEvaluationResult::Unknown) || relop->OperIs(GT_EQ))
        {
            return isEqual;
        }
        return (isEqual == RelopEvaluationResult::True) ? RelopEvaluationResult::False : RelopEvaluationResult::True;
    }

    // Normalize to "lo < hi" or "lo <= hi".
    const bool    swap     = relop->OperIs(GT_GT, GT_GE);
    const bool    isStrict = relop->OperIs(GT_LT, GT_GT);
    const Bounds& lo       = swap ? b2 : b1;
    const Bounds& hi       = swap ? b1 : b2;
    if (relop->IsUnsigned())
    {
        if (isStrict ? lo.UMax.ult(hi.UMin) : lo.UMax.ule(hi.UMin))
        {
            return RelopEvaluationResult::True;
        }
        if (isStrict ? lo.UMin.uge(hi.UMax) : lo.UMin.ugt(hi.UMax))
        {
            return RelopEvaluationResult::False;
        }
    }
    else
    {
        if (isStrict ? lo.SMax.slt(hi.SMin) : lo.SMax.sle(hi.SMin))
        {
            return RelopEvaluationResult::True;
        }
        if (isStrict ? lo.SMin.sge(hi.SMax) : lo.SMin.sgt(hi.SMax))
        {
            return RelopEvaluationResult::False;
        }
    }
    return RelopEvaluationResult::Unknown;
}

//------------------------------------------------------------------------
// ComputeBounds: Compute the bounds of the value of a tree.
//
// Arguments:
//    tree       - the tree
//    assertions - the assertions that hold for the tree, can be uninit
//    pBounds    - [out] the bounds
//
// Return Value:
//    true if something is known about the value.
//
bool KnownBitsAnalysis::ComputeBounds(GenTree* tree, ASSERT_VALARG_TP assertions, Bounds* pBounds)
{
    const unsigned width = GetWidth(tree->TypeGet());
    if (width == 0)
    {
        return false;
    }

    *pBounds              = Bounds(KnownBits(width));
    const KnownBits known = Compute(m_compiler->optConservativeNormalVN(tree), width, assertions, pBounds);
    if (known.isUnknown() && pBounds->IsUnknown())
    {
        return false;
    }

    // The known bits can be more precise than the bounds (e.g. after an EQ assertion).
    const Bounds fromKnown(known);
    pBounds->UMin = APInt::umax(pBounds->UMin, fromKnown.UMin);
    pBounds->UMax = APInt::umin(pBounds->UMax, fromKnown.UMax);
    pBounds->SMin = APInt::smax(pBounds->SMin, fromKnown.SMin);
    pBounds->SMax = APInt::smin(pBounds->SMax, fromKnown.SMax);
    return true;
}

//------------------------------------------------------------------------
// IsInRange: Check if the value of a tree is in the given signed range.
//
// Arguments:
//    tree       - the tree, of TYP_INT or TYP_LONG
//    lo         - the lower bound (inclusive), must fit into the type of the tree
//    hi         - the upper bound (inclusive), must fit into the type of the tree
//    assertions - the assertions that hold for the tree, can be uninit
//
// Return Value:
//    true if the value is known to be in [lo..hi].
//
bool KnownBitsAnalysis::IsInRange(GenTree* tree, int64_t lo, int64_t hi, ASSERT_VALARG_TP assertions)
{
    Bounds bounds{KnownBits(1)};
    if (!ComputeBounds(tree, assertions, &bounds))
    {
        return false;
    }

    const unsigned width = bounds.SMin.getBitWidth();
    assert((width == 64) || (FitsIn<int32_t>(lo) && FitsIn<int32_t>(hi)));
    return bounds.SMin.sge(APInt(width, (uint64_t)lo)) && bounds.SMax.sle(APInt(width, (uint64_t)hi));
}

//------------------------------------------------------------------------
// GetRangeProperties: Check if the value of a tree is known to be non-zero or non-negative.
//
// Arguments:
//    tree               - the tree
//    assertions         - the assertions that hold for the tree, can be uninit
//    isKnownNonZero     - [in, out] set to true if the value is known to be non-zero
//    isKnownNonNegative - [in, out] set to true if the value is known to be non-negative
//
void KnownBitsAnalysis::GetRangeProperties(GenTree*         tree,
                                           ASSERT_VALARG_TP assertions,
                                           bool*            isKnownNonZero,
                                           bool*            isKnownNonNegative)
{
    Bounds bounds{KnownBits(1)};
    if (ComputeBounds(tree, assertions, &bounds))
    {
        *isKnownNonZero |= !bounds.UMin.isZero();
        *isKnownNonNegative |= bounds.SMin.isNonNegative();
    }
}

//------------------------------------------------------------------------
// ComputeKnownBits: Compute the known bits of the value of a tree.
//
// Arguments:
//    tree       - the tree, of TYP_INT or TYP_LONG
//    assertions - the assertions that hold for the tree, can be uninit
//
// Return Value:
//    The known bits, of the width of the tree's type.
//
KnownBits KnownBitsAnalysis::ComputeKnownBits(GenTree* tree, ASSERT_VALARG_TP assertions)
{
    const unsigned width = GetWidth(tree->TypeGet());
    assert(width != 0);
    return Compute(m_compiler->optConservativeNormalVN(tree), width, assertions);
}

//------------------------------------------------------------------------
// NeverOverflows: Check if an overflow checking ADD/SUB/MUL can never overflow.
//
// Arguments:
//    tree       - the ADD/SUB/MUL node
//    assertions - the assertions that hold for the tree, can be uninit
//
// Return Value:
//    true if the operation can't overflow for any values of its operands.
//
bool KnownBitsAnalysis::NeverOverflows(GenTreeOp* tree, ASSERT_VALARG_TP assertions)
{
    assert(tree->OperIs(GT_ADD, GT_SUB, GT_MUL) && tree->gtOverflow());

    GenTree* const op1   = tree->gtGetOp1();
    GenTree* const op2   = tree->gtGetOp2();
    const unsigned width = GetWidth(tree->TypeGet());
    if ((width == 0) || (GetWidth(op1->TypeGet()) != width) || (GetWidth(op2->TypeGet()) != width))
    {
        return false;
    }

    Bounds b1{KnownBits(width)};
    Bounds b2{KnownBits(width)};
    if (!ComputeBounds(op1, assertions, &b1) || !ComputeBounds(op2, assertions, &b2))
    {
        return false;
    }

    bool ov1 = false;
    bool ov2 = false;
    if (tree->IsUnsigned())
    {
        switch (tree->OperGet())
        {
            case GT_ADD:
                b1.UMax.uadd_ov(b2.UMax, ov1);
                return !ov1;
            case GT_SUB:
                return b1.UMin.uge(b2.UMax);
            default:
                b1.UMax.umul_ov(b2.UMax, ov1);
                return !ov1;
        }
    }

    switch (tree->OperGet())
    {
        case GT_ADD:
            b1.SMax.sadd_ov(b2.SMax, ov1);
            b1.SMin.sadd_ov(b2.SMin, ov2);
            return !ov1 && !ov2;
        case GT_SUB:
            b1.SMax.ssub_ov(b2.SMin, ov1);
            b1.SMin.ssub_ov(b2.SMax, ov2);
            return !ov1 && !ov2;
        default:
        {
            // |op1| * |op2| <= SMAX is a (slightly conservative) sufficient condition.
            const APInt abs1 =
                APInt::umax(b1.SMax.isNegative() ? -b1.SMax : b1.SMax, b1.SMin.isNegative() ? -b1.SMin : b1.SMin);
            const APInt abs2 =
                APInt::umax(b2.SMax.isNegative() ? -b2.SMax : b2.SMax, b2.SMin.isNegative() ? -b2.SMin : b2.SMin);
            const APInt res = abs1.umul_ov(abs2, ov1);
            return !ov1 && res.isNonNegative();
        }
    }
}

//------------------------------------------------------------------------
// GetWidth: Get the bit width of the values of the given type.
//
// Arguments:
//    type - the type
//
// Return Value:
//    32 for TYP_INT (and the small types), 64 for TYP_LONG, 0 if the type is not supported.
//
unsigned KnownBitsAnalysis::GetWidth(var_types type)
{
    switch (genActualType(type))
    {
        case TYP_INT:
            return 32;
        case TYP_LONG:
            return 64;
        default:
            return 0;
    }
}

//------------------------------------------------------------------------
// Compute: Compute the known bits of a value number.
//
// Arguments:
//    vn                 - the value number
//    width              - the bit width of the value (32 or 64)
//    assertions         - the assertions that hold for "vn", can be uninit
//    pBounds            - [out, optional] the bounds of the value, these can be more
//                         precise than the ones the known bits imply
//
// Return Value:
//    The known bits; "unknown" when nothing is known (including the case of "vn" not
//    being an integer of the given width).
//
KnownBits KnownBitsAnalysis::Compute(ValueNum vn, unsigned width, ASSERT_VALARG_TP assertions, Bounds* pBounds)
{
    assert((width == 32) || (width == 64));
    KnownBits known(width);

    // A GC pointer, a floating point value, etc.
    const var_types vnType = (vn == ValueNumStore::NoVN) ? TYP_UNDEF : m_vnStore->TypeOfVN(vn);
    bool            isDone = GetWidth(vnType) != width;

    if (!isDone && m_vnStore->IsVNConstant(vn))
    {
        // Handles are not known at this point (relocs).
        int64_t cns;
        if (m_vnStore->IsVNIntegralConstant(vn, &cns))
        {
            known = KnownBits::makeConstant(APInt(width, (uint64_t)cns));
        }
        isDone = true;
    }

    if (isDone || (m_budget <= 0))
    {
        if (pBounds != nullptr)
        {
            *pBounds = Bounds(known);
        }
        return known;
    }
    m_budget--;

    // Small unsigned values (e.g. loads) are zero-extended.
    if (varTypeIsSmall(vnType) && varTypeIsUnsigned(vnType))
    {
        known.Zero.setBitsFrom(genTypeSize(vnType) * BITS_PER_BYTE);
    }

    // The number of low bits a value is sign-extended from, if any: we can't express that with known bits.
    unsigned sextBits =
        (varTypeIsSmall(vnType) && !varTypeIsUnsigned(vnType)) ? genTypeSize(vnType) * BITS_PER_BYTE : 0;

    Bounds    funcBounds{KnownBits(width)};
    VNFuncApp funcApp;
    if (m_vnStore->GetVNFunc(vn, &funcApp))
    {
        known = known.unionWith(ComputeForFunc(funcApp, width, assertions, &funcBounds));
        if (funcApp.FuncIs(VNF_Cast, VNF_CastOvf))
        {
            var_types castToType;
            bool      srcIsUnsigned;
            m_vnStore->GetCastOperFromVN(funcApp.GetArg(1), &castToType, &srcIsUnsigned);
            const unsigned srcWidth = GetWidth(m_vnStore->TypeOfVN(funcApp.GetArg(0)));
            if (varTypeIsSmall(castToType) && !varTypeIsUnsigned(castToType))
            {
                sextBits = genTypeSize(castToType) * BITS_PER_BYTE;
            }
            else if ((srcWidth != 0) && (srcWidth < width) && !srcIsUnsigned)
            {
                sextBits = srcWidth;
            }
        }
    }
    else if (m_vnStore->IsPhiDef(vn))
    {
        known = known.unionWith(ComputeForPhi(vn, width));
    }

    Bounds bounds(known);
    if ((sextBits != 0) && (sextBits < width))
    {
        bounds.SMin = APInt::smax(bounds.SMin, APInt::getSignedMinValue(sextBits).sext(width));
        bounds.SMax = APInt::smin(bounds.SMax, APInt::getSignedMaxValue(sextBits).sext(width));
        bounds.Normalize();
    }
    if (!funcBounds.IsUnknown())
    {
        bounds.UMin = APInt::umax(bounds.UMin, funcBounds.UMin);
        bounds.UMax = APInt::umin(bounds.UMax, funcBounds.UMax);
        bounds.SMin = APInt::smax(bounds.SMin, funcBounds.SMin);
        bounds.SMax = APInt::smin(bounds.SMax, funcBounds.SMax);
        bounds.Normalize();
    }
    MergeAssertions(vn, width, assertions, &known, &bounds);

    if (known.hasConflict())
    {
        // Contradicting facts mean that the code is unreachable.
        known.resetAll();
        bounds = Bounds(known);
    }

    if (pBounds != nullptr)
    {
        *pBounds = bounds;
    }
    return known;
}

//------------------------------------------------------------------------
// ComputeForPhi: Compute the known bits of a PHI: the bits that all the values
//    reaching it have in common.
//
// Arguments:
//    vn    - the value number of the PHI definition
//    width - the bit width of the value (32 or 64)
//
// Return Value:
//    The known bits.
//
// Notes:
//    The result is computed once (with its own budget) and cached: it depends on the assertions
//    that hold on the edges to the PHI, not on the ones that hold at the point of the query.
//    It is conservative for the PHIs that depend on each other (a PHI that is being computed
//    is "unknown" to the ones it depends on) or that are more than MaxPhiDepth PHIs deep, so
//    the answer for these may depend on which of them is asked about first.
//    In particular, nothing is known about the induction variables.
//
KnownBits KnownBitsAnalysis::ComputeForPhi(ValueNum vn, unsigned width)
{
    if (m_compiler->optKnownBitsPhiCache == nullptr)
    {
        m_compiler->optKnownBitsPhiCache = new (m_compiler, CMK_AssertionProp)
            Compiler::KnownBitsPhiCache(m_compiler->getAllocator(CMK_AssertionProp));
    }
    Compiler::KnownBitsPhiCache* const cache = m_compiler->optKnownBitsPhiCache;

    KnownBits        result(width);
    const KnownBits* cached = cache->LookupPointer(vn);
    if (cached != nullptr)
    {
        // A conflict marks a PHI that is being computed: we have a cycle, nothing is known.
        return cached->hasConflict() ? result : *cached;
    }

    if (m_phiDepth >= m_maxPhiDepth)
    {
        return result;
    }

    result.setAllConflict();
    cache->Set(vn, result);

    KnownBitsAnalysis analysis(m_compiler, m_phiDepth + 1);
    auto              visitor = [&](ValueNum reachingVN, ASSERT_TP reachingAssertions) {
        result = result.intersectWith(analysis.Compute(reachingVN, width, reachingAssertions));
        return result.isUnknown() ? Compiler::AssertVisit::Abort : Compiler::AssertVisit::Continue;
    };

    if ((m_compiler->optVisitReachingAssertions(vn, visitor) != Compiler::AssertVisit::Continue) ||
        result.hasConflict())
    {
        result.resetAll();
    }

    // Replace the "being computed" marker set above.
    cache->Set(vn, result, Compiler::KnownBitsPhiCache::Overwrite);
    return result;
}

//------------------------------------------------------------------------
// ComputeForFunc: Compute the known bits of a function application from its operands.
//
// Arguments:
//    funcApp    - the function application
//    width      - the bit width of its value (32 or 64)
//    assertions - the assertions that hold for the operands, can be uninit
//    pBounds    - [out] the bounds of the value, if more precise than the known bits imply
//
// Return Value:
//    The known bits.
//
KnownBits KnownBitsAnalysis::ComputeForFunc(const VNFuncApp& funcApp,
                                            unsigned         width,
                                            ASSERT_VALARG_TP assertions,
                                            Bounds*          pBounds)
{
    KnownBits    known(width);
    const VNFunc func = funcApp.GetFunc();

    if (ValueNumStore::VNFuncIsComparison(func))
    {
        // 0 or 1
        known.Zero.setBitsFrom(1);
        return known;
    }

    switch (func)
    {
        case VNF_AND:
        case VNF_OR:
        case VNF_XOR:
        {
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            KnownBits op2 = Compute(funcApp.GetArg(1), width, assertions);
            return (func == VNF_AND) ? (op1 & op2) : ((func == VNF_OR) ? (op1 | op2) : (op1 ^ op2));
        }

        case VNF_NOT:
        {
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            std::swap(op1.Zero, op1.One);
            return op1;
        }

        case VNF_NEG:
        {
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            return KnownBits::sub(KnownBits::makeConstant(APInt(width, 0)), op1);
        }

        case VNF_ADD:
        case VNF_SUB:
        {
            // Nothing is known about the result if nothing is known about one of the operands.
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            if (!op1.isUnknown())
            {
                KnownBits op2 = Compute(funcApp.GetArg(1), width, assertions);
                return KnownBits::computeForAddSub(func == VNF_ADD, /* NSW */ false, /* NUW */ false, op1, op2);
            }
            break;
        }

        case VNF_ADD_OVF:
        case VNF_ADD_UN_OVF:
        case VNF_SUB_OVF:
        case VNF_SUB_UN_OVF:
        {
            // These produce a value only if they don't overflow.
            const bool isAdd      = (func == VNF_ADD_OVF) || (func == VNF_ADD_UN_OVF);
            const bool isUnsigned = (func == VNF_ADD_UN_OVF) || (func == VNF_SUB_UN_OVF);
            KnownBits  op1        = Compute(funcApp.GetArg(0), width, assertions);
            KnownBits  op2        = Compute(funcApp.GetArg(1), width, assertions);
            return KnownBits::computeForAddSub(isAdd, /* NSW */ !isUnsigned, /* NUW */ isUnsigned, op1, op2);
        }

        case VNF_MUL:
        case VNF_MUL_OVF:
        case VNF_MUL_UN_OVF:
        {
            const bool isSelfMul = funcApp.GetArg(0) == funcApp.GetArg(1);
            KnownBits  op1       = Compute(funcApp.GetArg(0), width, assertions);
            KnownBits  op2       = isSelfMul ? op1 : Compute(funcApp.GetArg(1), width, assertions);
            return KnownBitsTracking::computeKnownBitsMul(op1, op2, /* NSW */ func == VNF_MUL_OVF,
                                                          /* NUW */ func == VNF_MUL_UN_OVF, isSelfMul);
        }

        case VNF_DIV:
        case VNF_MOD:
        case VNF_UDIV:
        case VNF_UMOD:
        {
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            KnownBits op2 = Compute(funcApp.GetArg(1), width, assertions);
            switch (func)
            {
                case VNF_DIV:
                    return KnownBits::sdiv(op1, op2);
                case VNF_MOD:
                    return KnownBits::srem(op1, op2);
                case VNF_UDIV:
                    return KnownBits::udiv(op1, op2);
                default:
                    return KnownBits::urem(op1, op2);
            }
        }

        case VNF_LSH:
        case VNF_RSH:
        case VNF_RSZ:
        {
            // The shift amount can be a native int.
            const ValueNum amountVN    = funcApp.GetArg(1);
            const unsigned amountWidth = GetWidth(m_vnStore->TypeOfVN(amountVN));
            if (amountWidth == 0)
            {
                break;
            }
            KnownBits amount = Compute(amountVN, amountWidth, assertions);
#if TARGET_MASKS_SHIFTS
            // The shift amount is masked, only its low bits matter.
            amount = amount.trunc(32) & KnownBits::makeConstant(APInt(32, width - 1));
#else
            if (amount.getMaxValue().getZExtValue() >= width)
            {
                break;
            }
#endif
            KnownBits op1 = Compute(funcApp.GetArg(0), width, assertions);
            switch (func)
            {
                case VNF_LSH:
                    return KnownBits::shl(op1, amount);
                case VNF_RSH:
                    return KnownBits::ashr(op1, amount);
                default:
                    return KnownBits::lshr(op1, amount);
            }
        }

        case VNF_Cast:
        case VNF_CastOvf:
        {
            var_types castToType;
            bool      srcIsUnsigned;
            m_vnStore->GetCastOperFromVN(funcApp.GetArg(1), &castToType, &srcIsUnsigned);
            if (!varTypeIsIntegral(castToType))
            {
                break;
            }

            const unsigned castBits = genTypeSize(castToType) * BITS_PER_BYTE;
            const unsigned srcWidth = GetWidth(m_vnStore->TypeOfVN(funcApp.GetArg(0)));
            if (srcWidth == 0)
            {
                // A cast from a floating point or GC value, we only know that the result
                // is extended from the target type.
                if ((castBits < width) && varTypeIsUnsigned(castToType))
                {
                    known.Zero.setBitsFrom(castBits);
                }
                break;
            }

            Bounds    srcBounds{KnownBits(srcWidth)};
            KnownBits src = Compute(funcApp.GetArg(0), srcWidth, assertions, &srcBounds);

            // A non-negative source that fits into the target (and its sign bit) keeps its bounds.
            const unsigned fitBits = min(castBits, width);
            if ((pBounds != nullptr) && srcBounds.SMin.isNonNegative() &&
                ((srcWidth - srcBounds.UMax.countLeadingZeros()) < fitBits))
            {
                pBounds->UMin = srcBounds.UMin.zextOrTrunc(width);
                pBounds->UMax = srcBounds.UMax.zextOrTrunc(width);
                pBounds->SMin = pBounds->UMin;
                pBounds->SMax = pBounds->UMax;
            }

            if (castBits < srcWidth)
            {
                // A truncation that is followed by an extension to the stack type (for small types).
                src = src.trunc(castBits);
                if (castBits < width)
                {
                    src = varTypeIsUnsigned(castToType) ? src.zext(width) : src.sext(width);
                }
            }
            else if (castBits > srcWidth)
            {
                src = srcIsUnsigned ? src.zext(width) : src.sext(width);
            }

            if (src.getBitWidth() != width)
            {
                break;
            }
            known = src;

            if ((func == VNF_CastOvf) && (srcIsUnsigned || varTypeIsUnsigned(castToType)))
            {
                // The checked cast produces a value only if the source value, signed or unsigned
                // depending on "srcIsUnsigned", fits into the target type. Here that is a
                // non-negative value that is less than 2^castMaxBits and 2^srcMaxBits.
                const unsigned castMaxBits = varTypeIsUnsigned(castToType) ? castBits : (castBits - 1);
                const unsigned srcMaxBits  = srcIsUnsigned ? srcWidth : (srcWidth - 1);
                const unsigned maxBits     = min(castMaxBits, srcMaxBits);
                if (maxBits < width)
                {
                    known.Zero.setBitsFrom(maxBits);
                }
            }
            return known;
        }

        default:
            break;
    }

    return known;
}

//------------------------------------------------------------------------
// MergeAssertions: Refine the known bits and the bounds of a value number using the
//    "vn relop cns" and "vn relop otherVN" assertions. For the latter, the facts about
//    "otherVN" come from its shape and its own "otherVN relop cns" assertions.
//
// Arguments:
//    vn         - the value number
//    width      - the bit width of the value (32 or 64)
//    assertions - the assertions that hold for "vn", can be uninit
//    pKnown     - [in, out] the known bits of "vn"
//    pBounds    - [in, out] the bounds of "vn"
//
void KnownBitsAnalysis::MergeAssertions(
    ValueNum vn, unsigned width, ASSERT_VALARG_TP assertions, KnownBits* pKnown, Bounds* pBounds)
{
    if (BitVecOps::MayBeUninit(assertions) || !m_compiler->optAssertionHasAssertionsForVN(vn))
    {
        return;
    }

    BitVecOps::VisitBits(m_compiler->apTraits, assertions, [&](unsigned index) {
        const Compiler::AssertionDsc& assertion = m_compiler->optGetAssertion(GetAssertionIndex(index));
        if (!assertion.GetOp1().KindIs(Compiler::O1K_VN))
        {
            return true;
        }

        const auto& op2 = assertion.GetOp2();
        if (assertion.GetOp1().GetVN() != vn)
        {
            // "otherVN relop vn", e.g. the "idx u< len" bounds check for "len".
            if (m_followVNAssertions && op2.KindIs(Compiler::O2K_VN_ADD_CNS) && (op2.GetVN() == vn) &&
                (op2.GetCns() == 0) && !assertion.KindIs(Compiler::OAK_NOT_EQUAL))
            {
                Bounds otherBounds{KnownBits(width)};
                m_followVNAssertions  = false;
                const KnownBits other = Compute(assertion.GetOp1().GetVN(), width, assertions, &otherBounds);
                m_followVNAssertions  = true;
                if (op2.IsVNNeverNegative())
                {
                    pBounds->ApplyRelop(Compiler::OAK_GE, KnownBits::makeConstant(APInt(width, 0)));
                }

                pBounds->ApplyRelop(SwapRelop(assertion.GetKind()), otherBounds);
                if (assertion.KindIs(Compiler::OAK_EQUAL))
                {
                    *pKnown = pKnown->unionWith(other);
                }
            }
            return true;
        }

        if (op2.KindIs(Compiler::O2K_CONST_INT) && !op2.HasIconFlag())
        {
            // "vn relop cns". Handles are not known at this point (relocs).
            pBounds->ApplyRelop(assertion.GetKind(),
                                KnownBits::makeConstant(APInt(width, (uint64_t)op2.GetIntConstant())));
        }
        else if (m_followVNAssertions && op2.KindIs(Compiler::O2K_VN_ADD_CNS) && (op2.GetCns() == 0) &&
                 !assertion.KindIs(Compiler::OAK_NOT_EQUAL))
        {
            // "vn relop otherVN". Don't use such assertions for "otherVN" and its operands,
            // that would send us in circles.
            Bounds otherBounds{KnownBits(width)};
            m_followVNAssertions  = false;
            const KnownBits other = Compute(op2.GetVN(), width, assertions, &otherBounds);
            m_followVNAssertions  = true;
            if (op2.IsVNNeverNegative())
            {
                otherBounds.ApplyRelop(Compiler::OAK_GE, KnownBits::makeConstant(APInt(width, 0)));
                otherBounds.Normalize();
            }

            pBounds->ApplyRelop(assertion.GetKind(), otherBounds);
            if (assertion.KindIs(Compiler::OAK_EQUAL))
            {
                *pKnown = pKnown->unionWith(other);
            }
        }
        return true;
    });

    pBounds->Normalize();
    if (pBounds->UMin.ule(pBounds->UMax))
    {
        *pKnown = pKnown->unionWith(KnownBitsRange::toKnownBits(pBounds->UMin, pBounds->UMax));
    }
}
