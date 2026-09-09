// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lower.h"

#ifndef TARGET_64BIT
//------------------------------------------------------------------------
// Lowering::DecomposeLongCompare: Decomposes a TYP_LONG compare node.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The next node to lower.
//
// Notes:
//    This is done during lowering because DecomposeLongs handles only nodes
//    that produce TYP_LONG values. Compare nodes may consume TYP_LONG values
//    but produce TYP_INT values.
//
GenTree* Lowering::DecomposeLongCompare(GenTree* cmp)
{
    assert(cmp->gtGetOp1()->TypeIs(TYP_LONG));

    GenTree* src1 = cmp->gtGetOp1();
    GenTree* src2 = cmp->gtGetOp2();
    assert(src1->OperIs(GT_LONG));
    assert(src2->OperIs(GT_LONG));
    GenTree* loSrc1 = src1->gtGetOp1();
    GenTree* hiSrc1 = src1->gtGetOp2();
    GenTree* loSrc2 = src2->gtGetOp1();
    GenTree* hiSrc2 = src2->gtGetOp2();
    BlockRange().Remove(src1);
    BlockRange().Remove(src2);

    genTreeOps condition = cmp->OperGet();
    GenTree*   loCmp;
    GenTree*   hiCmp;

    if (cmp->OperIs(GT_EQ, GT_NE))
    {
        //
        // Transform (x EQ|NE y) into (((x.lo XOR y.lo) OR (x.hi XOR y.hi)) EQ|NE 0). If y is 0 then this can
        // be reduced to just ((x.lo OR x.hi) EQ|NE 0). The OR is expected to set the condition flags so we
        // don't need to generate a redundant compare against 0, we only generate a SETCC|JCC instruction.
        //
        // XOR is used rather than SUB because it is commutative and thus allows swapping the operands when
        // the first happens to be a constant. Usually only the second compare operand is a constant but it's
        // still possible to have a constant on the left side. For example, when src1 is a uint->ulong cast
        // then hiSrc1 would be 0.
        //

        if (loSrc1->OperIs(GT_CNS_INT))
        {
            std::swap(loSrc1, loSrc2);
        }

        if (loSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(loSrc2);
            loCmp = loSrc1;
        }
        else
        {
            loCmp = m_compiler->gtNewOperNode(GT_XOR, TYP_INT, loSrc1, loSrc2);
            BlockRange().InsertBefore(cmp, loCmp);
            ContainCheckBinary(loCmp->AsOp());
        }

        if (hiSrc1->OperIs(GT_CNS_INT))
        {
            std::swap(hiSrc1, hiSrc2);
        }

        if (hiSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(hiSrc2);
            hiCmp = hiSrc1;
        }
        else
        {
            hiCmp = m_compiler->gtNewOperNode(GT_XOR, TYP_INT, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, hiCmp);
            ContainCheckBinary(hiCmp->AsOp());
        }

        hiCmp = m_compiler->gtNewOperNode(GT_OR, TYP_INT, loCmp, hiCmp);
        BlockRange().InsertBefore(cmp, hiCmp);
        ContainCheckBinary(hiCmp->AsOp());
    }
    else
    {
        assert(cmp->OperIs(GT_LT, GT_LE, GT_GE, GT_GT));

        //
        // If the compare is signed then (x LT|GE y) can be transformed into ((x SUB y) LT|GE 0).
        // If the compare is unsigned we can still use SUB but we need to check the Carry flag,
        // not the actual result. In both cases we can simply check the appropriate condition flags
        // and ignore the actual result:
        //     SUB_LO loSrc1, loSrc2
        //     SUB_HI hiSrc1, hiSrc2
        //     SETCC|JCC (signed|unsigned LT|GE)
        // If loSrc2 happens to be 0 then the first SUB can be eliminated and the second one can
        // be turned into a CMP because the first SUB would have set carry to 0. This effectively
        // transforms a long compare against 0 into an int compare of the high part against 0.
        //
        // (x LE|GT y) can to be transformed into ((x SUB y) LE|GT 0) but checking that a long value
        // is greater than 0 is not so easy. We need to turn this into a positive/negative check
        // like the one we get for LT|GE compares, this can be achieved by swapping the compare:
        //     (x LE|GT y) becomes (y GE|LT x)
        //
        // Having to swap operands is problematic when the second operand is a constant. The constant
        // moves to the first operand where it cannot be contained and thus needs a register. This can
        // be avoided by changing the constant such that LE|GT becomes LT|GE:
        //     (x LE|GT 41) becomes (x LT|GE 42)
        //

        if (cmp->OperIs(GT_LE, GT_GT))
        {
            bool mustSwap = true;

            if (loSrc2->OperIs(GT_CNS_INT) && hiSrc2->OperIs(GT_CNS_INT))
            {
                uint32_t loValue  = static_cast<uint32_t>(loSrc2->AsIntCon()->IconValue());
                uint32_t hiValue  = static_cast<uint32_t>(hiSrc2->AsIntCon()->IconValue());
                uint64_t value    = static_cast<uint64_t>(loValue) | (static_cast<uint64_t>(hiValue) << 32);
                uint64_t maxValue = cmp->IsUnsigned() ? UINT64_MAX : INT64_MAX;

                if (value != maxValue)
                {
                    value++;
                    loValue = value & UINT32_MAX;
                    hiValue = (value >> 32) & UINT32_MAX;
                    // Sign-extend (not zero-extend) so the stored value matches across host pointer sizes.
                    loSrc2->AsIntCon()->SetValueTruncating(static_cast<int32_t>(loValue));
                    hiSrc2->AsIntCon()->SetValueTruncating(static_cast<int32_t>(hiValue));

                    condition = cmp->OperIs(GT_LE) ? GT_LT : GT_GE;
                    mustSwap  = false;
                }
            }

            if (mustSwap)
            {
                std::swap(loSrc1, loSrc2);
                std::swap(hiSrc1, hiSrc2);
                condition = GenTree::SwapRelop(condition);
            }
        }

        assert((condition == GT_LT) || (condition == GT_GE));

        if (loSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(loSrc2);

            // Very conservative dead code removal... but it helps.

            if (loSrc1->OperIs(GT_CNS_INT, GT_LCL_VAR, GT_LCL_FLD))
            {
                BlockRange().Remove(loSrc1);
            }
            else
            {
                loSrc1->SetUnusedValue();
            }

            hiCmp = m_compiler->gtNewOperNode(GT_CMP, TYP_VOID, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, hiCmp);
            ContainCheckCompare(hiCmp->AsOp());
        }
        else
        {
            loCmp = m_compiler->gtNewOperNode(GT_CMP, TYP_VOID, loSrc1, loSrc2);
            loCmp->gtFlags |= GTF_SET_FLAGS;
            hiCmp = m_compiler->gtNewOperNode(GT_SUB_HI, TYP_INT, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, loCmp, hiCmp);
            ContainCheckCompare(loCmp->AsOp());
            ContainCheckBinary(hiCmp->AsOp());

            //
            // Try to move the first SUB_HI operands right in front of it, this allows using
            // a single temporary register instead of 2 (one for CMP and one for SUB_HI). Do
            // this only for locals as they won't change condition flags. Note that we could
            // move constants (except 0 which generates XOR reg, reg) but it's extremely rare
            // to have a constant as the first operand.
            //

            if (hiSrc1->OperIs(GT_LCL_VAR, GT_LCL_FLD) && IsInvariantInRange(hiSrc1, hiCmp))
            {
                BlockRange().Remove(hiSrc1);
                BlockRange().InsertBefore(hiCmp, hiSrc1);
            }
        }
    }

    hiCmp->gtFlags |= GTF_SET_FLAGS;
    if (hiCmp->IsValue())
    {
        hiCmp->SetUnusedValue();
    }

    LIR::Use cmpUse;
    if (BlockRange().TryGetUse(cmp, &cmpUse) && cmpUse.User()->OperIs(GT_JTRUE))
    {
        BlockRange().Remove(cmp);

        GenTree* jcc       = cmpUse.User();
        jcc->AsOp()->gtOp1 = nullptr;
        jcc->ChangeOper(GT_JCC);
        jcc->AsCC()->gtCondition = GenCondition::FromIntegralRelop(condition, cmp->IsUnsigned());
    }
    else
    {
        cmp->AsOp()->gtOp1 = nullptr;
        cmp->AsOp()->gtOp2 = nullptr;
        cmp->ChangeOper(GT_SETCC);
        cmp->AsCC()->gtCondition = GenCondition::FromIntegralRelop(condition, cmp->IsUnsigned());
    }

    return cmp->gtNext;
}
#endif // !TARGET_64BIT

//------------------------------------------------------------------------
// Lowering::OptimizeConstCompare: Performs various "compare with const" optimizations.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The original compare node if lowering should proceed as usual or the next node
//    to lower if the compare node was changed in such a way that lowering is no
//    longer needed.
//
// Notes:
//    - Narrow operands to enable memory operand containment (XARCH specific).
//    - Transform cmp(and(x, y), 0) into test(x, y) (XARCH/Arm64 specific but could
//      be used for ARM as well if support for GT_TEST_EQ/GT_TEST_NE is added).
//    - Transform TEST(x, LSH(1, y)) into BT(x, y) (XARCH specific)
//    - Transform RELOP(OP, 0) into SETCC(OP) or JCC(OP) if OP can set the
//      condition flags appropriately (XARCH/ARM64 specific but could be extended
//      to ARM32 as well if ARM32 codegen supports GTF_SET_FLAGS).
//
GenTree* Lowering::OptimizeConstCompare(GenTree* cmp)
{
    assert(cmp->gtGetOp2()->IsIntegralConst());

    GenTree*             op1 = cmp->gtGetOp1();
    GenTreeIntConCommon* op2 = cmp->gtGetOp2()->AsIntConCommon();

#if defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_RISCV64)

    // If 'test' is a single bit test, leaves the tested expr in the left op, the bit index in the right op, and returns
    // true. Otherwise, returns false.
    auto tryReduceSingleBitTestOps = [this](GenTreeOp* test) -> bool {
        assert(test->OperIs(GT_AND, GT_TEST_EQ, GT_TEST_NE));
        GenTree* testedOp = test->gtOp1;
        GenTree* bitOp    = test->gtOp2;
#ifdef TARGET_RISCV64
        if (bitOp->IsIntegralConstUnsignedPow2())
        {
            UINT64 bit  = bitOp->AsIntConCommon()->UnsignedIntegralValue();
            int    log2 = BitOperations::Log2(bit);
            bitOp->AsIntConCommon()->SetIntegralValue(log2);
            return true;
        }
#endif
        if (!bitOp->OperIs(GT_LSH))
            std::swap(bitOp, testedOp);

        if (bitOp->OperIs(GT_LSH) && varTypeIsIntOrI(bitOp) && bitOp->gtGetOp1()->IsIntegralConst(1))
        {
            BlockRange().Remove(bitOp->gtGetOp1());
            BlockRange().Remove(bitOp);
            test->gtOp1 = testedOp;
            test->gtOp2 = bitOp->gtGetOp2();
            return true;
        }

#ifdef TARGET_XARCH
        // Also recognize the arithmetic form `(x >> y) & 1`, i.e. AND(RSH|RSZ(x, y), 1), which
        // tests bit `y` of `x` just like `x & (1 << y)`. Only bit 0 of the shifted value is kept so
        // the shift kind is irrelevant, and `bt` masks the bit index modulo the operand size, which
        // matches the C# masked-shift semantics even for an out-of-range `y`. Restricted to a
        // variable index because a constant index keeps the shift, and `bt` has no immediate form
        // here (a constant mask `test` is already optimal).
        GenTree* shiftOp = test->gtOp1;
        GenTree* oneOp   = test->gtOp2;
        if (!oneOp->IsIntegralConst(1))
            std::swap(shiftOp, oneOp);

        if (oneOp->IsIntegralConst(1) && shiftOp->OperIs(GT_RSH, GT_RSZ) && varTypeIsIntOrI(shiftOp) &&
            !shiftOp->gtGetOp2()->IsIntegralConst())
        {
            BlockRange().Remove(oneOp);
            BlockRange().Remove(shiftOp);
            test->gtOp1 = shiftOp->gtGetOp1();
            test->gtOp2 = shiftOp->gtGetOp2();

            // ContainCheckCompare is skipped when this transform succeeds, so clear any containment
            // the value operand picked up from the removed shift (e.g. a `shrx` memory source) --
            // the reg,reg `bt` form requires it in a register.
            test->gtOp1->ClearContained();
            return true;
        }
#endif // TARGET_XARCH
        return false;
    };

    INT64 op2Value = op2->IntegralValue();

#ifdef TARGET_XARCH
    var_types op1Type = op1->TypeGet();
    if (IsContainableMemoryOp(op1) && varTypeIsSmall(op1Type) && FitsIn(op1Type, op2Value))
    {
        //
        // If op1's type is small then try to narrow op2 so it has the same type as op1.
        // Small types are usually used by memory loads and if both compare operands have
        // the same type then the memory load can be contained. In certain situations
        // (e.g "cmp ubyte, 200") we also get a smaller instruction encoding.
        //

        op2->gtType = op1Type;
    }
    else
#endif
        if (op1->OperIs(GT_CAST) && !op1->gtOverflow())
    {
        GenTreeCast* cast       = op1->AsCast();
        var_types    castToType = cast->CastToType();
        GenTree*     castOp     = cast->gtGetOp1();

        if ((castToType == TYP_UBYTE) && FitsIn<UINT8>(op2Value))
        {
            //
            // Since we're going to remove the cast we need to be able to narrow the cast operand
            // to the cast type. This can be done safely only for certain opers (e.g AND, OR, XOR).
            // Some opers just can't be narrowed (e.g DIV, MUL) while other could be narrowed but
            // doing so would produce incorrect results (e.g. RSZ, RSH).
            //
            // The below list of handled opers is conservative but enough to handle the most common
            // situations.
            //
            bool removeCast =
#ifdef TARGET_ARM64
                (op2Value == 0) && cmp->OperIs(GT_EQ, GT_NE, GT_GT) && !castOp->isContained() &&
#elif defined(TARGET_RISCV64)
                false && // disable, comparisons and bit operations are full-register only
#endif
                (castOp->OperIs(GT_LCL_VAR, GT_CALL, GT_OR, GT_XOR, GT_AND)
#ifdef TARGET_XARCH
                 || IsContainableMemoryOp(castOp)
#endif
                );

            if (removeCast)
            {
                assert(!castOp->gtOverflowEx()); // Must not be an overflow checking operation

#ifdef TARGET_ARM64
                bool cmpEq = cmp->OperIs(GT_EQ);

                cmp->SetOperRaw(cmpEq ? GT_TEST_EQ : GT_TEST_NE);
                op2->SetIntegralValue(0xff);
                op2->gtType = castOp->gtType;
#else
                castOp->gtType = castToType;
                op2->gtType    = castToType;
#endif
                // If we have any contained memory ops on castOp, they must now not be contained.
                castOp->ClearContained();

                if (castOp->OperIs(GT_OR, GT_XOR, GT_AND))
                {
                    castOp->gtGetOp1()->ClearContained();
                    castOp->gtGetOp2()->ClearContained();
                    ContainCheckBinary(castOp->AsOp());
                }

                cmp->AsOp()->gtOp1 = castOp;

                BlockRange().Remove(cast);
            }
        }
#ifdef TARGET_XARCH
        else if ((castToType == TYP_BYTE) && FitsIn<INT8>(op2Value))
        {
            //
            // Mirror the TYP_UBYTE case above for signed byte casts. Removing the cast lets
            // codegen emit a single byte-sized `cmp` (e.g. `cmp cl, 0xC0`) instead of first
            // sign-extending the operand with `movsx`. The compare stays signed because
            // TYP_BYTE is a signed small type, so LowerCompare's small-unsigned promotion
            // does not apply.
            //
            // Bail out for `x < 0` / `x >= 0` against a zero constant: codegen has a
            // sign-bit-shift optimization (`mov + shr`) that assumes the operand is
            // sign-extended to the full register width. After narrowing the operand to
            // TYP_BYTE, the register no longer carries that sign extension and the shift
            // amount would be wrong (it uses `emitActualTypeSize` which is still 4 for
            // TYP_BYTE). Letting the cast survive keeps that path correct.
            //
            const bool isSignBitTest = (op2Value == 0) && cmp->OperIs(GT_LT, GT_GE);
            bool       removeCast    = !isSignBitTest && (castOp->OperIs(GT_LCL_VAR, GT_CALL, GT_OR, GT_XOR, GT_AND) ||
                                                 IsContainableMemoryOp(castOp));

            if (removeCast)
            {
                assert(!castOp->gtOverflowEx()); // Must not be an overflow checking operation

                castOp->gtType = castToType;
                op2->gtType    = castToType;

                // If we have any contained memory ops on castOp, they must now not be contained.
                castOp->ClearContained();

                if (castOp->OperIs(GT_OR, GT_XOR, GT_AND))
                {
                    castOp->gtGetOp1()->ClearContained();
                    castOp->gtGetOp2()->ClearContained();
                    ContainCheckBinary(castOp->AsOp());
                }

                cmp->AsOp()->gtOp1 = castOp;

                BlockRange().Remove(cast);
            }
        }
#endif // TARGET_XARCH
    }
    else if (op1->OperIs(GT_AND) && cmp->OperIs(GT_EQ, GT_NE))
    {
        //
        // Transform ((x AND y) EQ|NE 0) into (x TEST_EQ|TEST_NE y) when possible.
        //

        GenTree* andOp1 = op1->gtGetOp1();
        GenTree* andOp2 = op1->gtGetOp2();

        //
        // If we don't have a 0 compare we can get one by transforming ((x AND mask) EQ|NE mask)
        // into ((x AND mask) NE|EQ 0) when mask is a single bit.
        //
        // TODO-Wasm: would like to use
        //   andOp2->IsIntegralValue(op2Value);
        //
        if ((op2Value != 0) && genExactlyOneBit(op2Value) && andOp2->IsIntegralConst() &&
            (andOp2->AsIntConCommon()->IntegralValue() == op2Value))
        {
            op2Value = 0;
            op2->SetIntegralValue(0);
            cmp->SetOperRaw(GenTree::ReverseRelop(cmp->OperGet()));
        }

#ifdef TARGET_RISCV64
        if (op2Value == 0 && !andOp2->isContained() && tryReduceSingleBitTestOps(op1->AsOp()))
        {
            GenTree* testedOp   = op1->gtGetOp1();
            GenTree* bitIndexOp = op1->gtGetOp2();

            if (bitIndexOp->IsIntegralConst())
            {
                // Shift the tested bit into the sign bit, then check if negative/positive.
                // Work on whole registers because comparisons and compressed shifts are full-register only.
                INT64 bitIndex     = bitIndexOp->AsIntConCommon()->IntegralValue();
                INT64 signBitIndex = genTypeSize(TYP_I_IMPL) * 8 - 1;
                if (bitIndex < signBitIndex)
                {
                    bitIndexOp->AsIntConCommon()->SetIntegralValue(signBitIndex - bitIndex);
                    bitIndexOp->SetContained();
                    op1->SetOperRaw(GT_LSH);
                    op1->gtType = TYP_I_IMPL;
                }
                else
                {
                    // The tested bit is the sign bit, remove "AND bitIndex" and only check if negative/positive
                    assert(bitIndex == signBitIndex);
                    assert(genActualType(testedOp) == TYP_I_IMPL);
                    BlockRange().Remove(bitIndexOp);
                    BlockRange().Remove(op1);
                    cmp->AsOp()->gtOp1 = testedOp;
                }

                op2->gtType = TYP_I_IMPL;
                cmp->SetOperRaw(cmp->OperIs(GT_NE) ? GT_LT : GT_GE);
                cmp->ClearUnsigned();

                return cmp;
            }

            // Shift the tested bit into the lowest bit, then AND with 1.
            // The "EQ|NE 0" comparison is folded below as necessary.
            var_types type     = genActualType(testedOp);
            op1->AsOp()->gtOp1 = andOp1 = m_compiler->gtNewOperNode(GT_RSH, type, testedOp, bitIndexOp);
            op1->AsOp()->gtOp2 = andOp2 = m_compiler->gtNewIconNode(1, type);
            BlockRange().InsertBefore(op1, andOp1, andOp2);
            andOp2->SetContained();
        }
#endif // TARGET_RISCV64

        // Optimizes (X & 1) != 0 to (X & 1)
        // Optimizes (X & 1) == 0 to ((NOT X) & 1)
        // (== 1 or != 1) cases are transformed to (!= 0 or == 0) above
        // The compiler requires jumps to have relop operands, so we do not fold that case.

        const bool optimizeToAnd    = (op2Value == 0) && cmp->OperIs(GT_NE);
        const bool optimizeToNotAnd = (op2Value == 0) && cmp->OperIs(GT_EQ);

        if ((andOp2->IsIntegralConst(1)) && (genActualType(op1) == cmp->TypeGet()) &&
            (optimizeToAnd || optimizeToNotAnd))
        {
            LIR::Use cmpUse;
            if (BlockRange().TryGetUse(cmp, &cmpUse) && !cmpUse.User()->OperIs(GT_JTRUE) &&
                !cmpUse.User()->OperIsConditional())
            {
                GenTree* next = cmp->gtNext;

                if (optimizeToNotAnd)
                {
                    GenTree* notNode   = m_compiler->gtNewOperNode(GT_NOT, andOp1->TypeGet(), andOp1);
                    op1->AsOp()->gtOp1 = notNode;
                    BlockRange().InsertAfter(andOp1, notNode);
                }

                cmpUse.ReplaceWith(op1);

                BlockRange().Remove(cmp->gtGetOp2());
                BlockRange().Remove(cmp);

                return next;
            }
        }

        if (op2Value == 0)
        {
#ifndef TARGET_RISCV64
            BlockRange().Remove(op1);
            BlockRange().Remove(op2);

            cmp->SetOperRaw(cmp->OperIs(GT_EQ) ? GT_TEST_EQ : GT_TEST_NE);
            cmp->AsOp()->gtOp1 = andOp1;
            cmp->AsOp()->gtOp2 = andOp2;
            // We will re-evaluate containment below
            andOp1->ClearContained();
            andOp2->ClearContained();

#ifdef TARGET_XARCH
            if (IsContainableMemoryOp(andOp1) && andOp2->IsIntegralConst())
            {
                //
                // For "test" we only care about the bits that are set in the second operand (mask).
                // If the mask fits in a small type then we can narrow both operands to generate a "test"
                // instruction with a smaller encoding ("test" does not have a r/m32, imm8 form) and avoid
                // a widening load in some cases.
                //
                // For 16 bit operands we narrow only if the memory operand is already 16 bit. This matches
                // the behavior of a previous implementation and avoids adding more cases where we generate
                // 16 bit instructions that require a length changing prefix (0x66). These suffer from
                // significant decoder stalls on Intel CPUs.
                //
                // We could also do this for 64 bit masks that fit into 32 bit but it doesn't help.
                // In such cases morph narrows down the existing GT_AND by inserting a cast between it and
                // the memory operand so we'd need to add more code to recognize and eliminate that cast.
                //

                size_t mask = static_cast<size_t>(andOp2->AsIntCon()->IconValue());

                if (FitsIn<UINT8>(mask))
                {
                    andOp1->gtType = TYP_UBYTE;
                    andOp2->gtType = TYP_UBYTE;
                }
                else if (FitsIn<UINT16>(mask) && genTypeSize(andOp1) == 2)
                {
                    andOp1->gtType = TYP_USHORT;
                    andOp2->gtType = TYP_USHORT;
                }
            }
#endif
#endif // !TARGET_RISCV64
        }
        else if (andOp2->IsIntegralConst() && GenTree::Compare(andOp2, op2))
        {
            //
            // Transform EQ|NE(AND(x, y), y) into EQ|NE(AND(NOT(x), y), 0) when y is a constant.
            //

            andOp1->ClearContained();
            GenTree* notNode               = m_compiler->gtNewOperNode(GT_NOT, andOp1->TypeGet(), andOp1);
            cmp->gtGetOp1()->AsOp()->gtOp1 = notNode;
            BlockRange().InsertAfter(andOp1, notNode);
            op2->BashToZeroConst(op2->TypeGet());

            andOp1   = notNode;
            op2Value = 0;
        }
    }

#ifdef TARGET_XARCH
    if (cmp->OperIs(GT_TEST_EQ, GT_TEST_NE))
    {
        //
        // Transform TEST_EQ|NE(x, LSH(1, y)) or TEST_EQ|NE(LSH(1, y), x) into BT(x, y) when possible. Using BT
        // results in smaller and faster code. It also doesn't have special register
        // requirements, unlike LSH that requires the shift count to be in ECX.
        // Note that BT has the same behavior as LSH when the bit index exceeds the
        // operand bit size - it uses (bit_index MOD bit_size).
        //
        if (tryReduceSingleBitTestOps(cmp->AsOp()))
        {
            cmp->SetOper(cmp->OperIs(GT_TEST_EQ) ? GT_BITTEST_EQ : GT_BITTEST_NE);
            cmp->gtGetOp2()->ClearContained();
            return cmp->gtNext;
        }
    }
#endif // TARGET_XARCH
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_RISCV64)

    // Optimize EQ/NE(relop/SETCC, 0) into (maybe reversed) cond.
    if (cmp->OperIs(GT_EQ, GT_NE) && op2->IsIntegralConst(0) && (op1->OperIsCompare() || op1->OperIs(GT_SETCC)))
    {
        LIR::Use use;
        if (BlockRange().TryGetUse(cmp, &use))
        {
            if (cmp->OperIs(GT_EQ))
            {
                GenTree* reversed = m_compiler->gtReverseCond(op1);
                assert(reversed == op1);
            }

            // Relops and SETCC can be either TYP_INT or TYP_LONG typed, so we
            // may need to retype it.
            op1->gtType = cmp->TypeGet();

            GenTree* next = cmp->gtNext;
            use.ReplaceWith(op1);
            BlockRange().Remove(cmp->gtGetOp2());
            BlockRange().Remove(cmp);
            return next;
        }
    }

    // Optimize EQ/NE/GT/GE/LT/LE(op_that_sets_zf, 0) into op_that_sets_zf with GTF_SET_FLAGS + SETCC.
    LIR::Use use;
    if (((cmp->OperIs(GT_EQ, GT_NE) && op2->IsIntegralConst(0) && op1->SupportsSettingZeroFlag()) ||
         (cmp->OperIs(GT_GT, GT_GE, GT_LT, GT_LE) && op2->IsIntegralConst(0) &&
          op1->SupportsSettingFlagsAsCompareToZero())) &&
        BlockRange().TryGetUse(cmp, &use) && IsProfitableToSetZeroFlag(op1))
    {
        // For unsigned compares against zero that rely on flags-as-compare-to-zero,
        // we cannot use UGT/UGE/ULT/ULE directly because op1 may set only NZ flags
        // (e.g. ANDS on ARM64 clears C/V). UGT/ULT depend on carry, which would be wrong.
        // If we'd need to bail, do it before touching the LIR.
        const bool isUnsignedZeroCompare = cmp->IsUnsigned() && op2->IsIntegralConst(0);
        if (isUnsignedZeroCompare && cmp->OperIs(GT_GT, GT_GE, GT_LT, GT_LE))
        {
            if (cmp->OperIs(GT_GE, GT_LT))
            {
                // x >= 0U is always true and x < 0U is always false; keep the compare for correctness.
                return cmp;
            }
        }

        op1->gtFlags |= GTF_SET_FLAGS;
        op1->SetUnusedValue();

        GenTree* next = cmp->gtNext;
        BlockRange().Remove(cmp);
        BlockRange().Remove(op2);

        GenCondition cmpCondition = GenCondition::FromRelop(cmp);

        // Use Z/N where possible.
        if (isUnsignedZeroCompare)
        {
            if (cmp->OperIs(GT_GT))
            {
                // x > 0U  <=> x != 0
                cmpCondition = GenCondition::NE;
            }
            else if (cmp->OperIs(GT_LE))
            {
                // x <= 0U <=> x == 0
                cmpCondition = GenCondition::EQ;
            }
        }
        GenTreeCC* setcc = m_compiler->gtNewCC(GT_SETCC, cmp->TypeGet(), cmpCondition);
        BlockRange().InsertAfter(op1, setcc);

        use.ReplaceWith(setcc);
        return next;
    }

    return cmp;
}

//------------------------------------------------------------------------
// Lowering::LowerCompare: Lowers a compare node.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerCompare(GenTree* cmp)
{
#if LOWER_DECOMPOSE_LONGS
    if (cmp->gtGetOp1()->TypeIs(TYP_LONG))
    {
        return DecomposeLongCompare(cmp);
    }
#endif // LOWER_DECOMPOSE_LONGS

    if (cmp->gtGetOp2()->IsIntegralConst() && !m_compiler->opts.MinOpts())
    {
        GenTree* next = OptimizeConstCompare(cmp);

        // If OptimizeConstCompare return the compare node as "next" then we need to continue lowering.
        if (next != cmp)
        {
            return next;
        }
    }

#ifdef TARGET_XARCH
    if (cmp->gtGetOp1()->TypeGet() == cmp->gtGetOp2()->TypeGet())
    {
        if (varTypeIsSmall(cmp->gtGetOp1()->TypeGet()) && varTypeIsUnsigned(cmp->gtGetOp1()->TypeGet()))
        {
            //
            // If both operands have the same type then codegen will use the common operand type to
            // determine the instruction type. For small types this would result in performing a
            // signed comparison of two small unsigned values without zero extending them to TYP_INT
            // which is incorrect. Note that making the comparison unsigned doesn't imply that codegen
            // has to generate a small comparison, it can still correctly generate a TYP_INT comparison.
            //

            cmp->SetUnsigned();
        }
    }
#elif defined(TARGET_RISCV64)
    if (varTypeUsesIntReg(cmp->gtGetOp1()))
    {
        if (GenTree* next = LowerSavedIntegerCompare(cmp); next != cmp)
            return next;

        // Integer comparisons are full-register only.
        SignExtendIfNecessary(&cmp->AsOp()->gtOp1);
        SignExtendIfNecessary(&cmp->AsOp()->gtOp2);
    }
#endif // TARGET_RISCV64

    ContainCheckCompare(cmp->AsOp());
    return cmp->gtNext;
}

#if !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64) && !defined(TARGET_WASM)
//------------------------------------------------------------------------
// Lowering::LowerJTrue: Lowers a JTRUE node.
//
// Arguments:
//    jtrue - the JTRUE node
//
// Return Value:
//    The next node to lower (usually nullptr).
//
// Notes:
//    On ARM64 this may remove the JTRUE node and transform its associated
//    relop into a JCMP node.
//
GenTree* Lowering::LowerJTrue(GenTreeOp* jtrue)
{
    GenTree* cond = jtrue->gtGetOp1();

    JITDUMP("Lowering JTRUE:\n");
    DISPTREERANGE(BlockRange(), jtrue);
    JITDUMP("\n");

#if defined(TARGET_ARM64)
    if (cond->OperIsCompare() && cond->gtGetOp2()->IsCnsIntOrI())
    {
        GenTree*     relopOp1 = cond->gtGetOp1();
        GenTree*     relopOp2 = cond->gtGetOp2();
        genTreeOps   newOper  = GT_COUNT;
        GenCondition cc;

        if (cond->OperIs(GT_EQ, GT_NE) && relopOp2->IsIntegralConst(0))
        {
            // Codegen will use cbz or cbnz in codegen which do not affect the flag register
            newOper = GT_JCMP;
            cc      = GenCondition::FromRelop(cond);
        }
        else if (cond->OperIs(GT_LT, GT_GE) && !cond->IsUnsigned() && relopOp2->IsIntegralConst(0))
        {
            // Codegen will use tbnz or tbz in codegen which do not affect the flag register
            var_types op1Type = genActualType(relopOp1);

            // Remove cast to sbyte or short and instead check negative bit for those types.
            if (relopOp1->OperIs(GT_CAST))
            {
                GenTreeCast* cast = relopOp1->AsCast();
                if ((cast->CastToType() == TYP_BYTE || cast->CastToType() == TYP_SHORT) && !cast->gtOverflow())
                {
                    op1Type             = cast->CastToType();
                    GenTree* castOp     = cast->CastOp();
                    cond->AsOp()->gtOp1 = castOp;
                    castOp->ClearContained();
                    BlockRange().Remove(cast);
                    relopOp1 = castOp;
                }
            }
            newOper = GT_JTEST;
            cc      = cond->OperIs(GT_LT) ? GenCondition(GenCondition::NE) : GenCondition(GenCondition::EQ);
            // x < 0 => (x & signBit) != 0. Update the constant to be the sign bit.
            relopOp2->AsIntConCommon()->SetIntegralValue((static_cast<INT64>(1) << (8 * genTypeSize(op1Type) - 1)));
        }
        else if (cond->OperIs(GT_TEST_EQ, GT_TEST_NE) && isPow2(relopOp2->AsIntCon()->IconValue()))
        {
            // Codegen will use tbz or tbnz in codegen which do not affect the flag register
            newOper = GT_JTEST;
            cc      = GenCondition::FromRelop(cond);
        }

        if (newOper != GT_COUNT)
        {
            jtrue->ChangeOper(newOper);
            jtrue->gtOp1                 = relopOp1;
            jtrue->gtOp2                 = relopOp2;
            jtrue->AsOpCC()->gtCondition = cc;

            relopOp2->SetContained();

            BlockRange().Remove(cond);
            JITDUMP("Lowered to %s\n", GenTree::OpName(newOper));
            return nullptr;
        }
    }
#endif // TARGET_ARM64

    GenCondition condCode;
    if (TryLowerConditionToFlagsNode(jtrue, cond, &condCode))
    {
        jtrue->SetOper(GT_JCC);
        jtrue->AsCC()->gtCondition = condCode;
    }

    JITDUMP("Lowering JTRUE Result:\n");
    DISPTREERANGE(BlockRange(), jtrue);
    JITDUMP("\n");

    return nullptr;
}
#endif // !TARGET_LOONGARCH64 && !TARGET_RISCV64 && !defined(TARGET_WASM)
