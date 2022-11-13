// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX             Lowering for ARM and ARM64 common code                        XX
XX                                                                           XX
XX  This encapsulates common logic for lowering trees for the ARM and ARM64  XX
XX  architectures.  For a more detailed view of what is lowering, please     XX
XX  take a look at Lower.cpp                                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARMARCH // This file is ONLY used for ARM and ARM64 architectures

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsic.h"
#endif

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
    return comp->codeGen->validImmForBL((ssize_t)addr);
}

//------------------------------------------------------------------------
// IsContainableImmed: Is an immediate encodable in-place?
//
// Return Value:
//    True if the immediate can be folded into an instruction,
//    for example small enough and non-relocatable.
//
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
{
    if (!varTypeIsFloating(parentNode->TypeGet()))
    {
#ifdef TARGET_ARM64
        if (parentNode->OperIsCompare() && childNode->IsFloatPositiveZero())
        {
            // Contain 0.0 constant in fcmp on arm64
            // TODO: Enable for arm too (vcmp)

            // We currently don't emit these for floating points
            assert(!parentNode->OperIs(GT_TEST_EQ, GT_TEST_NE));
            return true;
        }
#endif

        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->AsIntCon()->ImmedValNeedsReloc(comp))
            return false;

        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t type.
        target_ssize_t immVal = (target_ssize_t)childNode->AsIntCon()->gtIconVal;
        emitAttr       attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr       size   = EA_SIZE(attr);
#ifdef TARGET_ARM
        insFlags flags = parentNode->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
#endif

        switch (parentNode->OperGet())
        {
            case GT_ADD:
            case GT_SUB:
#ifdef TARGET_ARM64
                return emitter::emitIns_valid_imm_for_add(immVal, size);
            case GT_CMPXCHG:
            case GT_LOCKADD:
            case GT_XORR:
            case GT_XAND:
            case GT_XADD:
                return comp->compOpportunisticallyDependsOn(InstructionSet_Atomics)
                           ? false
                           : emitter::emitIns_valid_imm_for_add(immVal, size);
#elif defined(TARGET_ARM)
                return emitter::emitIns_valid_imm_for_add(immVal, flags);
#endif
                break;

#ifdef TARGET_ARM64
            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
            case GT_CMP:
            case GT_BOUNDS_CHECK:
                return emitter::emitIns_valid_imm_for_cmp(immVal, size);
            case GT_AND:
            case GT_OR:
            case GT_XOR:
            case GT_TEST_EQ:
            case GT_TEST_NE:
                return emitter::emitIns_valid_imm_for_alu(immVal, size);
            case GT_JCMP:
                assert(((parentNode->gtFlags & GTF_JCMP_TST) == 0) ? (immVal == 0) : isPow2(immVal));
                return true;
#elif defined(TARGET_ARM)
            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
            case GT_CMP:
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                return emitter::emitIns_valid_imm_for_alu(immVal);
#endif // TARGET_ARM

#ifdef TARGET_ARM64
            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
                if (immVal == 0)
                    return true;
                break;
#endif

            default:
                break;
        }
    }

    return false;
}

#ifdef TARGET_ARM64
//------------------------------------------------------------------------
// IsContainableBinaryOp: Is the child node a binary op that is containable from the parent node?
//
// Return Value:
//    True if the child node can be contained.
//
// Notes:
//    This can handle the decision to emit 'madd' or 'msub'.
//
bool Lowering::IsContainableBinaryOp(GenTree* parentNode, GenTree* childNode) const
{
    // The node we're checking should be one of the two child nodes
    assert((parentNode->gtGetOp1() == childNode) || (parentNode->gtGetOp2() == childNode));

    // We cannot contain if the parent node
    // * is contained
    // * is not operating on an integer
    // * is already marking a child node as contained
    // * is required to throw on overflow

    if (parentNode->isContained())
        return false;

    if (!varTypeIsIntegral(parentNode))
        return false;

    if (parentNode->gtGetOp1()->isContained() || parentNode->gtGetOp2()->isContained())
        return false;

    if (parentNode->OperMayOverflow() && parentNode->gtOverflow())
        return false;

    // We cannot contain if the child node:
    // * is not operating on an integer
    // * is required to set a flag
    // * is required to throw on overflow

    if (!varTypeIsIntegral(childNode))
        return false;

    if ((childNode->gtFlags & GTF_SET_FLAGS) != 0)
        return false;

    if (childNode->OperMayOverflow() && childNode->gtOverflow())
        return false;

    GenTree* matchedOp = nullptr;

    if (childNode->OperIs(GT_MUL))
    {
        if (childNode->gtGetOp1()->isContained() || childNode->gtGetOp2()->isContained())
        {
            // Cannot contain if either of the childs operands is already contained
            return false;
        }

        if ((parentNode->gtFlags & GTF_SET_FLAGS) != 0)
        {
            // Cannot contain if the parent operation needs to set flags
            return false;
        }

        if (parentNode->OperIs(GT_ADD))
        {
            // Find "c + (a * b)" or "(a * b) + c"
            return IsSafeToContainMem(parentNode, childNode);
        }

        if (parentNode->OperIs(GT_SUB))
        {
            // Find "c - (a * b)"
            assert(childNode == parentNode->gtGetOp2());
            return IsSafeToContainMem(parentNode, childNode);
        }

        // TODO: Handle mneg
        return false;
    }

    if (childNode->OperIs(GT_LSH, GT_RSH, GT_RSZ))
    {
        // Find "a op (b shift cns)"

        if (childNode->gtGetOp1()->isContained())
        {
            // Cannot contain if the childs op1 is already contained
            return false;
        }

        GenTree* shiftAmountNode = childNode->gtGetOp2();

        if (!shiftAmountNode->IsCnsIntOrI())
        {
            // Cannot contain if the childs op2 is not a constant
            return false;
        }

        const ssize_t shiftAmount = shiftAmountNode->AsIntCon()->IconValue();
        const ssize_t maxShift    = (static_cast<ssize_t>(genTypeSize(parentNode)) * BITS_IN_BYTE) - 1;

        if ((shiftAmount < 0x01) || (shiftAmount > maxShift))
        {
            // Cannot contain if the shift amount is less than 1 or greater than maxShift
            return false;
        }

        if (parentNode->OperIs(GT_ADD, GT_SUB, GT_AND))
        {
            // These operations can still report flags

            if (IsSafeToContainMem(parentNode, childNode))
            {
                assert(shiftAmountNode->isContained());
                return true;
            }
        }

        if ((parentNode->gtFlags & GTF_SET_FLAGS) != 0)
        {
            // Cannot contain if the parent operation needs to set flags
            return false;
        }

        if (parentNode->OperIs(GT_CMP, GT_OR, GT_XOR))
        {
            if (IsSafeToContainMem(parentNode, childNode))
            {
                assert(shiftAmountNode->isContained());
                return true;
            }
        }

        // TODO: Handle CMN, NEG/NEGS, BIC/BICS, EON, MVN, ORN, TST
        return false;
    }

    if (childNode->OperIs(GT_CAST))
    {
        // Find "a op cast(b)"
        GenTree* castOp = childNode->AsCast()->CastOp();

        bool isSupportedCast = false;

        if (varTypeIsSmall(childNode->CastToType()))
        {
            // The JIT doesn't track upcasts from small types, instead most types
            // are tracked as TYP_INT and then we get explicit downcasts to the
            // desired small type instead.

            assert(!varTypeIsFloating(castOp));
            isSupportedCast = true;
        }
        else if (childNode->TypeIs(TYP_LONG) && genActualTypeIsInt(castOp))
        {
            // We can handle "INT -> LONG", "INT -> ULONG", "UINT -> LONG", and "UINT -> ULONG"
            isSupportedCast = true;
        }

        if (!isSupportedCast)
        {
            return false;
        }

        if (parentNode->OperIs(GT_ADD, GT_SUB))
        {
            // These operations can still report flags

            if (IsSafeToContainMem(parentNode, childNode))
            {
                return true;
            }
        }

        if ((parentNode->gtFlags & GTF_SET_FLAGS) != 0)
        {
            // Cannot contain if the parent operation needs to set flags
            return false;
        }

        if (parentNode->OperIs(GT_CMP))
        {
            if (IsSafeToContainMem(parentNode, childNode))
            {
                return true;
            }
        }

        // TODO: Handle CMN
        return false;
    }

    return false;
}
#endif // TARGET_ARM64

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Widening small stores (on ARM).
//
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
#ifdef TARGET_ARM
    // On ARM, small stores can cost a bit more in terms of code size so we try to widen them. This is legal
    // as most small locals have 4-byte-wide stack homes, the common exception being (dependent) struct fields.
    //
    if (storeLoc->OperIs(GT_STORE_LCL_VAR) && varTypeIsSmall(storeLoc) && storeLoc->Data()->IsCnsIntOrI())
    {
        LclVarDsc* varDsc = comp->lvaGetDesc(storeLoc);
        if (!varDsc->lvIsStructField && (varDsc->GetStackSlotHomeType() == TYP_INT))
        {
            storeLoc->gtType = TYP_INT;
        }
    }
#endif // TARGET_ARM

    if (storeLoc->OperIs(GT_STORE_LCL_FLD))
    {
        // We should only encounter this for lclVars that are lvDoNotEnregister.
        verifyLclFldDoNotEnregister(storeLoc->GetLclNum());
    }

    ContainCheckStoreLoc(storeLoc);
}

//------------------------------------------------------------------------
// LowerStoreIndir: Determine addressing mode for an indirection, and whether operands are contained.
//
// Arguments:
//    node       - The indirect store node (GT_STORE_IND) of interest
//
// Return Value:
//    None.
//
void Lowering::LowerStoreIndir(GenTreeStoreInd* node)
{
    ContainCheckStoreIndir(node);
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL/GT_MULHI/GT_MUL_LONG node.
//
// For ARM64 recognized GT_MULs that can be turned into GT_MUL_LONGs, as
// those are cheaper. Performs contaiment checks.
//
// Arguments:
//    mul - The node to lower
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    assert(mul->OperIsMul());

#ifdef TARGET_ARM64
    if (comp->opts.OptimizationEnabled() && mul->OperIs(GT_MUL) && mul->IsValidLongMul())
    {
        GenTreeCast* op1 = mul->gtGetOp1()->AsCast();
        GenTree*     op2 = mul->gtGetOp2();

        mul->ClearOverflow();
        mul->ClearUnsigned();
        if (op1->IsUnsigned())
        {
            mul->SetUnsigned();
        }

        op1->CastOp()->ClearContained(); // Uncontain any memory operands.
        mul->gtOp1 = op1->CastOp();
        BlockRange().Remove(op1);

        if (op2->OperIs(GT_CAST))
        {
            op2->AsCast()->CastOp()->ClearContained(); // Uncontain any memory operands.
            mul->gtOp2 = op2->AsCast()->CastOp();
            BlockRange().Remove(op2);
        }
        else
        {
            assert(op2->IsIntegralConst());
            assert(FitsIn<int32_t>(op2->AsIntConCommon()->IntegralValue()));

            op2->ChangeType(TYP_INT);
        }

        mul->ChangeOper(GT_MUL_LONG);
    }
#endif // TARGET_ARM64

    ContainCheckMul(mul);

    return mul->gtNext;
}

//------------------------------------------------------------------------
// LowerBinaryArithmetic: lowers the given binary arithmetic node.
//
// Arguments:
//    node - the arithmetic node to lower
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerBinaryArithmetic(GenTreeOp* binOp)
{
    if (comp->opts.OptimizationEnabled() && binOp->OperIs(GT_AND))
    {
        GenTree* opNode  = nullptr;
        GenTree* notNode = nullptr;
        if (binOp->gtGetOp1()->OperIs(GT_NOT))
        {
            notNode = binOp->gtGetOp1();
            opNode  = binOp->gtGetOp2();
        }
        else if (binOp->gtGetOp2()->OperIs(GT_NOT))
        {
            notNode = binOp->gtGetOp2();
            opNode  = binOp->gtGetOp1();
        }

        if (notNode != nullptr)
        {
            binOp->gtOp1 = opNode;
            binOp->gtOp2 = notNode->AsUnOp()->gtGetOp1();
            binOp->ChangeOper(GT_AND_NOT);
            BlockRange().Remove(notNode);
        }
#ifdef TARGET_ARM64
        else
        {
            ContainCheckCompareChainForAnd(binOp);
        }
#endif
    }

    ContainCheckBinary(binOp);

    return binOp->gtNext;
}

//------------------------------------------------------------------------
// LowerBlockStore: Lower a block store node
//
// Arguments:
//    blkNode - The block store node to lower
//
void Lowering::LowerBlockStore(GenTreeBlk* blkNode)
{
    GenTree* dstAddr = blkNode->Addr();
    GenTree* src     = blkNode->Data();
    unsigned size    = blkNode->Size();

    const bool isDstAddrLocal = dstAddr->OperIsLocalAddr();

    if (blkNode->OperIsInitBlkOp())
    {
        if (src->OperIs(GT_INIT_VAL))
        {
            src->SetContained();
            src = src->AsUnOp()->gtGetOp1();
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            blkNode->SetOper(GT_STORE_BLK);
        }

        unsigned initBlockUnrollLimit = INITBLK_UNROLL_LIMIT;

#ifdef TARGET_ARM64
        if (isDstAddrLocal)
        {
            // Since dstAddr points to the stack CodeGen can use more optimal
            // quad-word store SIMD instructions for InitBlock.
            initBlockUnrollLimit = INITBLK_LCL_UNROLL_LIMIT;
        }
#endif

        if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (size <= initBlockUnrollLimit) && src->OperIs(GT_CNS_INT))
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

            // The fill value of an initblk is interpreted to hold a
            // value of (unsigned int8) however a constant of any size
            // may practically reside on the evaluation stack. So extract
            // the lower byte out of the initVal constant and replicate
            // it to a larger constant whose size is sufficient to support
            // the largest width store of the desired inline expansion.

            ssize_t fill = src->AsIntCon()->IconValue() & 0xFF;

            if (fill == 0)
            {
#ifdef TARGET_ARM64
                // On ARM64 we can just use REG_ZR instead of having to load
                // the constant into a real register like on ARM32.
                src->SetContained();
#endif
            }
#ifdef TARGET_ARM64
            else if (size >= REGSIZE_BYTES)
            {
                fill *= 0x0101010101010101LL;
                src->gtType = TYP_LONG;
            }
#endif
            else
            {
                fill *= 0x01010101;
            }

            src->AsIntCon()->SetIconValue(fill);

            ContainBlockStoreAddress(blkNode, size, dstAddr);
        }
        else
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
        }
    }
    else
    {
        assert(src->OperIs(GT_IND, GT_LCL_VAR, GT_LCL_FLD));
        src->SetContained();

        bool isSrcAddrLocal = false;

        if (src->OperIs(GT_IND))
        {
            GenTree* srcAddr = src->AsIndir()->Addr();
            // TODO-Cleanup: Make sure that GT_IND lowering didn't mark the source address as contained.
            // Sometimes the GT_IND type is a non-struct type and then GT_IND lowering may contain the
            // address, not knowing that GT_IND is part of a block op that has containment restrictions.
            srcAddr->ClearContained();
            isSrcAddrLocal = srcAddr->OperIsLocalAddr();
        }
        else
        {
            isSrcAddrLocal = true;

            if (src->OperIs(GT_LCL_VAR))
            {
                // TODO-1stClassStructs: for now we can't work with STORE_BLOCK source in register.
                const unsigned srcLclNum = src->AsLclVar()->GetLclNum();
                comp->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
            }
        }

        unsigned copyBlockUnrollLimit = CPBLK_UNROLL_LIMIT;

#ifdef TARGET_ARM64
        if (isSrcAddrLocal && isDstAddrLocal)
        {
            // Since both srcAddr and dstAddr point to the stack CodeGen can use more optimal
            // quad-word load and store SIMD instructions for CopyBlock.
            copyBlockUnrollLimit = CPBLK_LCL_UNROLL_LIMIT;
        }
#endif

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            if (!blkNode->AsObj()->GetLayout()->HasGCPtr())
            {
                blkNode->SetOper(GT_STORE_BLK);
            }
            else if (isDstAddrLocal && (size <= copyBlockUnrollLimit))
            {
                blkNode->SetOper(GT_STORE_BLK);
                blkNode->gtBlkOpGcUnsafe = true;
            }
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
        }
        else if (blkNode->OperIs(GT_STORE_BLK) && (size <= copyBlockUnrollLimit))
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

            if (src->OperIs(GT_IND))
            {
                ContainBlockStoreAddress(blkNode, size, src->AsIndir()->Addr());
            }

            ContainBlockStoreAddress(blkNode, size, dstAddr);
        }
        else
        {
            assert(blkNode->OperIs(GT_STORE_BLK, GT_STORE_DYN_BLK));

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
        }
    }
}

//------------------------------------------------------------------------
// ContainBlockStoreAddress: Attempt to contain an address used by an unrolled block store.
//
// Arguments:
//    blkNode - the block store node
//    size - the block size
//    addr - the address node to try to contain
//
void Lowering::ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr)
{
    assert(blkNode->OperIs(GT_STORE_BLK) && (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll));
    assert(size < INT32_MAX);

    if (addr->OperIsLocalAddr())
    {
        addr->SetContained();
        return;
    }

    if (!addr->OperIs(GT_ADD) || addr->gtOverflow() || !addr->AsOp()->gtGetOp2()->OperIs(GT_CNS_INT))
    {
        return;
    }

    GenTreeIntCon* offsetNode = addr->AsOp()->gtGetOp2()->AsIntCon();
    ssize_t        offset     = offsetNode->IconValue();

#ifdef TARGET_ARM
    // All integer load/store instructions on Arm support offsets in range -255..255.
    // Of course, this is a rather conservative check.
    if ((offset < -255) || (offset > 255) || (offset + static_cast<int>(size) > 256))
    {
        return;
    }
#else // !TARGET_ARM
    if ((ClrSafeInt<int>(offset) + ClrSafeInt<int>(size)).IsOverflow())
    {
        return;
    }
#endif // !TARGET_ARM

    if (!IsSafeToContainMem(blkNode, addr))
    {
        return;
    }

    BlockRange().Remove(offsetNode);

    addr->ChangeOper(GT_LEA);
    addr->AsAddrMode()->SetIndex(nullptr);
    addr->AsAddrMode()->SetScale(0);
    addr->AsAddrMode()->SetOffset(static_cast<int>(offset));
    addr->SetContained();
}

//------------------------------------------------------------------------
// LowerPutArgStkOrSplit: Lower a GT_PUTARG_STK/GT_PUTARG_SPLIT.
//
// Arguments:
//    putArgStk - The node to lower
//
void Lowering::LowerPutArgStkOrSplit(GenTreePutArgStk* putArgNode)
{
    GenTree* src = putArgNode->Data();

    if (src->TypeIs(TYP_STRUCT))
    {
        // STRUCT args (FIELD_LIST / OBJ / LCL_VAR / LCL_FLD) will always be contained.
        MakeSrcContained(putArgNode, src);

        if (src->OperIs(GT_LCL_VAR))
        {
            // TODO-1stClassStructs: support struct enregistration here by retyping "src" to its register type for
            // the non-split case.
            comp->lvaSetVarDoNotEnregister(src->AsLclVar()->GetLclNum() DEBUGARG(DoNotEnregisterReason::IsStructArg));
        }
    }
}

//------------------------------------------------------------------------
// LowerCast: Lower GT_CAST(srcType, DstType) nodes.
//
// Arguments:
//    tree - GT_CAST node to be lowered
//
// Return Value:
//    None.
//
// Notes:
//    Casts from float/double to a smaller int type are transformed as follows:
//    GT_CAST(float/double, byte)     =   GT_CAST(GT_CAST(float/double, int32), byte)
//    GT_CAST(float/double, sbyte)    =   GT_CAST(GT_CAST(float/double, int32), sbyte)
//    GT_CAST(float/double, int16)    =   GT_CAST(GT_CAST(double/double, int32), int16)
//    GT_CAST(float/double, uint16)   =   GT_CAST(GT_CAST(double/double, int32), uint16)
//
//    Note that for the overflow conversions we still depend on helper calls and
//    don't expect to see them here.
//    i) GT_CAST(float/double, int type with overflow detection)
//
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    JITDUMP("LowerCast for: ");
    DISPNODE(tree);
    JITDUMP("\n");

    GenTree*  op1     = tree->AsOp()->gtOp1;
    var_types dstType = tree->CastToType();
    var_types srcType = genActualType(op1->TypeGet());

    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        assert(!varTypeIsSmall(dstType)); // fgMorphCast creates intermediate casts when converting from float to small
                                          // int.
    }

    assert(!varTypeIsSmall(srcType));

    // Now determine if we have operands that should be contained.
    ContainCheckCast(tree->AsCast());
}

//------------------------------------------------------------------------
// LowerRotate: Lower GT_ROL and GT_ROR nodes.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
void Lowering::LowerRotate(GenTree* tree)
{
    if (tree->OperGet() == GT_ROL)
    {
        // There is no ROL instruction on ARM. Convert ROL into ROR.
        GenTree* rotatedValue        = tree->AsOp()->gtOp1;
        unsigned rotatedValueBitSize = genTypeSize(rotatedValue->gtType) * 8;
        GenTree* rotateLeftIndexNode = tree->AsOp()->gtOp2;

        if (rotateLeftIndexNode->IsCnsIntOrI())
        {
            ssize_t rotateLeftIndex                    = rotateLeftIndexNode->AsIntCon()->gtIconVal;
            ssize_t rotateRightIndex                   = rotatedValueBitSize - rotateLeftIndex;
            rotateLeftIndexNode->AsIntCon()->gtIconVal = rotateRightIndex;
        }
        else
        {
            GenTree* tmp = comp->gtNewOperNode(GT_NEG, genActualType(rotateLeftIndexNode->gtType), rotateLeftIndexNode);
            BlockRange().InsertAfter(rotateLeftIndexNode, tmp);
            tree->AsOp()->gtOp2 = tmp;
        }
        tree->ChangeOper(GT_ROR);
    }
    ContainCheckShiftRotate(tree->AsOp());
}

#ifdef TARGET_ARM64
//------------------------------------------------------------------------
// LowerModPow2: Lower GT_MOD if the second operand is a constant power of 2.
//
// Arguments:
//    tree - the node to lower
//
// Notes:
//     TODO: We could do this optimization in morph but we do not have
//           a conditional select op in HIR. At some point, we may
//           introduce such an op.
void Lowering::LowerModPow2(GenTree* node)
{
    assert(node->OperIs(GT_MOD));
    GenTreeOp* mod      = node->AsOp();
    GenTree*   dividend = mod->gtGetOp1();
    GenTree*   divisor  = mod->gtGetOp2();

    JITDUMP("Lower: optimize X MOD POW2");

    assert(divisor->IsIntegralConstPow2());

    const var_types type = mod->TypeGet();
    assert((type == TYP_INT) || (type == TYP_LONG));

    ssize_t divisorCnsValue         = static_cast<ssize_t>(divisor->AsIntConCommon()->IntegralValue());
    ssize_t divisorCnsValueMinusOne = divisorCnsValue - 1;

    BlockRange().Remove(divisor);

    // We need to use the dividend node multiple times so its value needs to be
    // computed once and stored in a temp variable.
    LIR::Use opDividend(BlockRange(), &mod->AsOp()->gtOp1, mod);
    dividend = ReplaceWithLclVar(opDividend);

    GenTree* dividend2 = comp->gtClone(dividend);
    BlockRange().InsertAfter(dividend, dividend2);

    GenTreeIntCon* cns = comp->gtNewIconNode(divisorCnsValueMinusOne, type);
    BlockRange().InsertAfter(dividend2, cns);

    GenTree* const trueExpr = comp->gtNewOperNode(GT_AND, type, dividend, cns);
    BlockRange().InsertAfter(cns, trueExpr);
    LowerNode(trueExpr);

    if (divisorCnsValue == 2)
    {
        // {expr} % 2
        // Logically turns into:
        //     let a = {expr}
        //     if a < 0 then -(a & 1) else (a & 1)
        // which then turns into:
        //     and   reg1, reg0, #1
        //     cmp   reg0, #0
        //     cneg  reg0, reg1, lt

        GenTreeIntCon* cnsZero = comp->gtNewIconNode(0, type);
        BlockRange().InsertAfter(trueExpr, cnsZero);

        GenTree* const cmp = comp->gtNewOperNode(GT_CMP, type, dividend2, cnsZero);
        cmp->gtFlags |= GTF_SET_FLAGS;
        BlockRange().InsertAfter(cnsZero, cmp);
        LowerNode(cmp);

        mod->ChangeOper(GT_CNEG_LT);
        mod->gtOp1 = trueExpr;
    }
    else
    {
        // {expr} % {cns}
        // Logically turns into:
        //     let a = {expr}
        //     if a > 0 then (a & ({cns} - 1)) else -(-a & ({cns} - 1))
        // which then turns into:
        //     and   reg1, reg0, #({cns} - 1)
        //     negs  reg0, reg0
        //     and   reg0, reg0, #({cns} - 1)
        //     csneg reg0, reg1, reg0, mi

        GenTree* const neg = comp->gtNewOperNode(GT_NEG, type, dividend2);
        neg->gtFlags |= GTF_SET_FLAGS;
        BlockRange().InsertAfter(trueExpr, neg);

        GenTreeIntCon* cns2 = comp->gtNewIconNode(divisorCnsValueMinusOne, type);
        BlockRange().InsertAfter(neg, cns2);

        GenTree* const falseExpr = comp->gtNewOperNode(GT_AND, type, neg, cns2);
        BlockRange().InsertAfter(cns2, falseExpr);
        LowerNode(falseExpr);

        mod->ChangeOper(GT_CSNEG_MI);
        mod->gtOp1 = trueExpr;
        mod->gtOp2 = falseExpr;
    }

    ContainCheckNode(mod);
}

//------------------------------------------------------------------------
// LowerAddForPossibleContainment: Tries to lower GT_ADD in such a way
//                                 that would allow one of its operands
//                                 to be contained.
//
// Arguments:
//    node - the node to lower
//
GenTree* Lowering::LowerAddForPossibleContainment(GenTreeOp* node)
{
    assert(node->OperIs(GT_ADD));

    if (!comp->opts.OptimizationEnabled())
        return nullptr;

    if (node->isContained())
        return nullptr;

    if (!varTypeIsIntegral(node))
        return nullptr;

    if (node->gtFlags & GTF_SET_FLAGS)
        return nullptr;

    if (node->gtOverflow())
        return nullptr;

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    // If the second operand is a containable immediate,
    // then we do not want to risk moving it around
    // in this transformation.
    if (IsContainableImmed(node, op2))
        return nullptr;

    GenTree* mul = nullptr;
    GenTree* c   = nullptr;
    if (op1->OperIs(GT_MUL))
    {
        // Swap
        mul = op1;
        c   = op2;
    }
    else
    {
        mul = op2;
        c   = op1;
    }

    if (mul->OperIs(GT_MUL) && !(mul->gtFlags & GTF_SET_FLAGS) && varTypeIsIntegral(mul) && !mul->gtOverflow() &&
        !mul->isContained() && !c->isContained())
    {
        GenTree* a = mul->gtGetOp1();
        GenTree* b = mul->gtGetOp2();

        // Transform "-a * b + c" to "c - a * b"
        if (a->OperIs(GT_NEG) && !(a->gtFlags & GTF_SET_FLAGS) && !b->OperIs(GT_NEG) && !a->isContained() &&
            !a->gtGetOp1()->isContained())
        {
            mul->AsOp()->gtOp1 = a->gtGetOp1();
            BlockRange().Remove(a);
            node->gtOp1 = c;
            node->gtOp2 = mul;
            node->ChangeOper(GT_SUB);

            ContainCheckNode(node);

            return node->gtNext;
        }
        // Transform "a * -b + c" to "c - a * b"
        else if (b->OperIs(GT_NEG) && !(b->gtFlags & GTF_SET_FLAGS) && !a->OperIs(GT_NEG) && !b->isContained() &&
                 !b->gtGetOp1()->isContained())
        {
            mul->AsOp()->gtOp2 = b->gtGetOp1();
            BlockRange().Remove(b);
            node->gtOp1 = c;
            node->gtOp2 = mul;
            node->ChangeOper(GT_SUB);

            ContainCheckNode(node);

            return node->gtNext;
        }
        // Transform "a * b + c" to "c + a * b"
        else if (op1->OperIs(GT_MUL))
        {
            node->gtOp1 = c;
            node->gtOp2 = mul;

            ContainCheckNode(node);

            return node->gtNext;
        }
    }

    return nullptr;
}
#endif

#ifdef FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// LowerHWIntrinsicFusedMultiplyAddScalar: Lowers AdvSimd_FusedMultiplyAddScalar intrinsics
//   when some of the operands are negated by "containing" such negation.
//
//  Arguments:
//     node - The original hardware intrinsic node
//
// |  op1 | op2 | op3 |
// |  +   |  +  |  +  | AdvSimd_FusedMultiplyAddScalar
// |  +   |  +  |  -  | AdvSimd_FusedMultiplySubtractScalar
// |  +   |  -  |  +  | AdvSimd_FusedMultiplySubtractScalar
// |  +   |  -  |  -  | AdvSimd_FusedMultiplyAddScalar
// |  -   |  +  |  +  | AdvSimd_FusedMultiplySubtractNegatedScalar
// |  -   |  +  |  -  | AdvSimd_FusedMultiplyAddNegatedScalar
// |  -   |  -  |  +  | AdvSimd_FusedMultiplyAddNegatedScalar
// |  -   |  -  |  -  | AdvSimd_FusedMultiplySubtractNegatedScalar
//
void Lowering::LowerHWIntrinsicFusedMultiplyAddScalar(GenTreeHWIntrinsic* node)
{
    assert(node->GetHWIntrinsicId() == NI_AdvSimd_FusedMultiplyAddScalar);

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);
    GenTree* op3 = node->Op(3);

    auto lowerOperand = [this](GenTree* op) {
        bool wasNegated = false;

        if (op->OperIsHWIntrinsic() &&
            ((op->AsHWIntrinsic()->GetHWIntrinsicId() == NI_AdvSimd_Arm64_DuplicateToVector64) ||
             (op->AsHWIntrinsic()->GetHWIntrinsicId() == NI_Vector64_CreateScalarUnsafe)))
        {
            GenTreeHWIntrinsic* createVector64 = op->AsHWIntrinsic();
            GenTree*            valueOp        = createVector64->Op(1);

            if (valueOp->OperIs(GT_NEG))
            {
                createVector64->Op(1) = valueOp->gtGetOp1();
                BlockRange().Remove(valueOp);
                wasNegated = true;
            }
        }

        return wasNegated;
    };

    const bool op1WasNegated = lowerOperand(op1);
    const bool op2WasNegated = lowerOperand(op2);
    const bool op3WasNegated = lowerOperand(op3);

    if (op1WasNegated)
    {
        if (op2WasNegated != op3WasNegated)
        {
            node->ChangeHWIntrinsicId(NI_AdvSimd_FusedMultiplyAddNegatedScalar);
        }
        else
        {
            node->ChangeHWIntrinsicId(NI_AdvSimd_FusedMultiplySubtractNegatedScalar);
        }
    }
    else if (op2WasNegated != op3WasNegated)
    {
        node->ChangeHWIntrinsicId(NI_AdvSimd_FusedMultiplySubtractScalar);
    }
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
    assert(node->TypeGet() != TYP_SIMD32);

    if (node->TypeGet() == TYP_SIMD12)
    {
        // GT_HWINTRINSIC node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        node->gtType = TYP_SIMD16;
    }

    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();

    switch (intrinsicId)
    {
        case NI_Vector64_Create:
        case NI_Vector128_Create:
        {
            // We don't directly support the Vector64.Create or Vector128.Create methods in codegen
            // and instead lower them to other intrinsic nodes in LowerHWIntrinsicCreate so we expect
            // that the node is modified to either not be a HWIntrinsic node or that it is no longer
            // the same intrinsic as when it came in.

            return LowerHWIntrinsicCreate(node);
        }

        case NI_Vector64_Dot:
        case NI_Vector128_Dot:
        {
            return LowerHWIntrinsicDot(node);
        }

        case NI_Vector64_op_Equality:
        case NI_Vector128_op_Equality:
        {
            return LowerHWIntrinsicCmpOp(node, GT_EQ);
        }

        case NI_Vector64_op_Inequality:
        case NI_Vector128_op_Inequality:
        {
            return LowerHWIntrinsicCmpOp(node, GT_NE);
        }

        case NI_AdvSimd_FusedMultiplyAddScalar:
            LowerHWIntrinsicFusedMultiplyAddScalar(node);
            break;

        default:
            break;
    }

    ContainCheckHWIntrinsic(node);
    return node->gtNext;
}

//----------------------------------------------------------------------------------------------
// Lowering::IsValidConstForMovImm: Determines if the given node can be replaced by a mov/fmov immediate instruction
//
//  Arguments:
//     node - The hardware intrinsic node.
//
//  Returns:
//     true if the node can be replaced by a mov/fmov immediate instruction; otherwise, false
//
//  IMPORTANT:
//     This check may end up modifying node->gtOp1 if it is a cast node that can be removed
bool Lowering::IsValidConstForMovImm(GenTreeHWIntrinsic* node)
{
    assert((node->GetHWIntrinsicId() == NI_Vector64_Create) || (node->GetHWIntrinsicId() == NI_Vector128_Create) ||
           (node->GetHWIntrinsicId() == NI_Vector64_CreateScalarUnsafe) ||
           (node->GetHWIntrinsicId() == NI_Vector128_CreateScalarUnsafe) ||
           (node->GetHWIntrinsicId() == NI_AdvSimd_DuplicateToVector64) ||
           (node->GetHWIntrinsicId() == NI_AdvSimd_DuplicateToVector128) ||
           (node->GetHWIntrinsicId() == NI_AdvSimd_Arm64_DuplicateToVector64) ||
           (node->GetHWIntrinsicId() == NI_AdvSimd_Arm64_DuplicateToVector128));
    assert(node->GetOperandCount() == 1);

    GenTree* op1    = node->Op(1);
    GenTree* castOp = nullptr;

    // TODO-Casts: why don't we fold the casts? MinOpts?
    if (varTypeIsIntegral(node->GetSimdBaseType()) && op1->OperIs(GT_CAST))
    {
        // We will sometimes get a cast around a constant value (such as for
        // certain long constants) which would block the below containment.
        // So we will temporarily check what the cast is from instead so we
        // can catch those cases as well.

        castOp = op1->AsCast()->CastOp();
        op1    = castOp;
    }

    if (op1->IsCnsIntOrI())
    {
        const ssize_t dataValue = op1->AsIntCon()->gtIconVal;

        if (comp->GetEmitter()->emitIns_valid_imm_for_movi(dataValue, emitActualTypeSize(node->GetSimdBaseType())))
        {
            if (castOp != nullptr)
            {
                // We found a containable immediate under
                // a cast, so remove the cast from the LIR.

                BlockRange().Remove(node->Op(1));
                node->Op(1) = op1;
            }
            return true;
        }
    }
    else if (op1->IsCnsFltOrDbl())
    {
        assert(varTypeIsFloating(node->GetSimdBaseType()));
        assert(castOp == nullptr);

        const double dataValue = op1->AsDblCon()->DconValue();
        return comp->GetEmitter()->emitIns_valid_imm_for_fmov(dataValue);
    }

    return false;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCmpOp: Lowers a Vector128 or Vector256 comparison intrinsic
//
//  Arguments:
//     node  - The hardware intrinsic node.
//     cmpOp - The comparison operation, currently must be GT_EQ or GT_NE
//
GenTree* Lowering::LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    assert((intrinsicId == NI_Vector64_op_Equality) || (intrinsicId == NI_Vector64_op_Inequality) ||
           (intrinsicId == NI_Vector128_op_Equality) || (intrinsicId == NI_Vector128_op_Inequality));

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);
    assert(node->gtType == TYP_BOOL);
    assert((cmpOp == GT_EQ) || (cmpOp == GT_NE));

    // We have the following (with the appropriate simd size and where the intrinsic could be op_Inequality):
    //          /--*  op2  simd
    //          /--*  op1  simd
    //   node = *  HWINTRINSIC   simd   T op_Equality

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);

    // Optimize comparison against Vector64/128<>.Zero via UMAXV:
    //
    //   bool eq = v == Vector128<integer>.Zero
    //
    // to:
    //
    //   bool eq = AdvSimd.Arm64.MaxPairwise(v.AsUInt16(), v.AsUInt16()).GetElement(0) == 0;
    //
    GenTree* op     = nullptr;
    GenTree* opZero = nullptr;
    if (op1->IsVectorZero())
    {
        op     = op2;
        opZero = op1;
    }
    else if (op2->IsVectorZero())
    {
        op     = op1;
        opZero = op2;
    }

    // Special case: "vec ==/!= zero_vector"
    if (!varTypeIsFloating(simdBaseType) && (op != nullptr) && (simdSize != 12))
    {
        GenTree* cmp = op;
        if (simdSize != 8) // we don't need compression for Vector64
        {
            node->Op(1) = op;
            LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
            ReplaceWithLclVar(tmp1Use);
            op               = node->Op(1);
            GenTree* opClone = comp->gtClone(op);
            BlockRange().InsertAfter(op, opClone);

            cmp = comp->gtNewSimdHWIntrinsicNode(simdType, op, opClone, NI_AdvSimd_Arm64_MaxPairwise, CORINFO_TYPE_UINT,
                                                 simdSize);
            BlockRange().InsertBefore(node, cmp);
            LowerNode(cmp);
        }

        BlockRange().Remove(opZero);

        GenTree* zroCns = comp->gtNewIconNode(0, TYP_INT);
        BlockRange().InsertAfter(cmp, zroCns);

        GenTree* val =
            comp->gtNewSimdHWIntrinsicNode(TYP_LONG, cmp, zroCns, NI_AdvSimd_Extract, CORINFO_TYPE_ULONG, simdSize);
        BlockRange().InsertAfter(zroCns, val);
        LowerNode(val);

        GenTree* cmpZeroCns = comp->gtNewIconNode(0, TYP_LONG);
        BlockRange().InsertAfter(val, cmpZeroCns);

        node->ChangeOper(cmpOp);
        node->gtType        = TYP_INT;
        node->AsOp()->gtOp1 = val;
        node->AsOp()->gtOp2 = cmpZeroCns;
        LowerNodeCC(node, (cmpOp == GT_EQ) ? GenCondition::EQ : GenCondition::NE);
        node->gtType = TYP_VOID;
        node->ClearUnusedValue();
        LowerNode(node);
        return node->gtNext;
    }

    NamedIntrinsic cmpIntrinsic;

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        case TYP_FLOAT:
        {
            cmpIntrinsic = NI_AdvSimd_CompareEqual;
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        case TYP_DOUBLE:
        {
            cmpIntrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_CompareEqualScalar : NI_AdvSimd_Arm64_CompareEqual;
            break;
        }

        default:
        {
            unreached();
        }
    }

    GenTree* cmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, cmpIntrinsic, simdBaseJitType, simdSize);
    BlockRange().InsertBefore(node, cmp);
    LowerNode(cmp);

    if ((simdBaseType == TYP_FLOAT) && (simdSize == 12))
    {
        // For TYP_SIMD12 we don't want the upper bits to participate in the comparison. So, we will insert all ones
        // into those bits of the result, "as if" the upper bits are equal. Then if all lower bits are equal, we get the
        // expected all-ones result, and will get the expected 0's only where there are non-matching bits.

        GenTree* idxCns = comp->gtNewIconNode(3, TYP_INT);
        BlockRange().InsertAfter(cmp, idxCns);

        GenTree* insCns = comp->gtNewIconNode(-1, TYP_INT);
        BlockRange().InsertAfter(idxCns, insCns);

        GenTree* tmp = comp->gtNewSimdHWIntrinsicNode(simdType, cmp, idxCns, insCns, NI_AdvSimd_Insert,
                                                      CORINFO_TYPE_INT, simdSize);
        BlockRange().InsertAfter(insCns, tmp);
        LowerNode(tmp);

        cmp = tmp;
    }

    if (simdSize != 8) // we don't need compression for Vector64
    {
        GenTree* msk;

        // Save cmp into a temp as we're going to need to pass it twice to MinPairwise
        node->Op(1) = cmp;
        LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
        ReplaceWithLclVar(tmp1Use);
        cmp               = node->Op(1);
        GenTree* cmpClone = comp->gtClone(cmp);
        BlockRange().InsertAfter(cmp, cmpClone);

        msk = comp->gtNewSimdHWIntrinsicNode(simdType, cmp, cmpClone, NI_AdvSimd_Arm64_MinPairwise, CORINFO_TYPE_UINT,
                                             simdSize);
        BlockRange().InsertAfter(cmpClone, msk);
        LowerNode(msk);

        cmp = msk;
    }

    GenTree* zroCns = comp->gtNewIconNode(0, TYP_INT);
    BlockRange().InsertAfter(cmp, zroCns);

    GenTree* val =
        comp->gtNewSimdHWIntrinsicNode(TYP_LONG, cmp, zroCns, NI_AdvSimd_Extract, CORINFO_TYPE_ULONG, simdSize);
    BlockRange().InsertAfter(zroCns, val);
    LowerNode(val);

    GenTree* bitMskCns = comp->gtNewIconNode(static_cast<ssize_t>(0xffffffffffffffff), TYP_LONG);
    BlockRange().InsertAfter(val, bitMskCns);

    node->ChangeOper(cmpOp);

    node->gtType        = TYP_LONG;
    node->AsOp()->gtOp1 = val;
    node->AsOp()->gtOp2 = bitMskCns;

    // The CompareEqual will set (condition is true) or clear (condition is false) all bits of the respective element
    // The MinAcross then ensures we get either all bits set (all conditions are true) or clear (any condition is false)
    // So, we need to invert the condition from the operation since we compare against zero

    GenCondition cmpCnd = (cmpOp == GT_EQ) ? GenCondition::EQ : GenCondition::NE;
    LowerNodeCC(node, cmpCnd);

    node->gtType = TYP_VOID;
    node->ClearUnusedValue();

    LowerNode(node);
    return node->gtNext;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector64 or Vector128 Create call
//
// Performs the following transformations:
//  1. If all the arguments are constant (including the broadcast case), the vector
//     will be loaded from the data section, or turned into Zero/AllBitsSet, if possible.
//  2. Non-constant broadcasts (argCnt == 1) are turned into DuplicateToVector intrinsics.
//  3. Remaining cases get a chain of "Insert"s, from the second element to the last, where
//     the vector to be inserted into is created with CreateUnsafeScalar from the first element.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    var_types      simdType        = node->TypeGet();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    simd32_t       simd32Val       = {};

    if ((simdSize == 8) && (simdType == TYP_DOUBLE))
    {
        // TODO-Cleanup: Struct retyping means we have the wrong type here. We need to
        //               manually fix it up so the simdType checks below are correct.
        simdType = TYP_SIMD8;
    }

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    bool   isConstant = GenTreeVecCon::IsHWIntrinsicCreateConstant(node, simd32Val);
    size_t argCnt     = node->GetOperandCount();

    // Check if we have a cast that we can remove. Note that "IsValidConstForMovImm"
    // will reset Op(1) if it finds such a cast, so we do not need to handle it here.
    // TODO-Casts: why are casts from constants checked for here?
    if (isConstant && (argCnt == 1) && IsValidConstForMovImm(node))
    {
        // Set isConstant to false so we get lowered to a DuplicateToVector
        // intrinsic, which will itself mark the node as contained.
        isConstant = false;
    }

    if (isConstant)
    {
        assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));

        for (GenTree* arg : node->Operands())
        {
            BlockRange().Remove(arg);
        }

        GenTreeVecCon* vecCon = comp->gtNewVconNode(simdType);

        vecCon->gtSimd32Val = simd32Val;
        BlockRange().InsertBefore(node, vecCon);

        LIR::Use use;
        if (BlockRange().TryGetUse(node, &use))
        {
            use.ReplaceWith(vecCon);
        }
        else
        {
            vecCon->SetUnusedValue();
        }

        BlockRange().Remove(node);

        return LowerNode(vecCon);
    }
    else if (argCnt == 1)
    {
        // We have the following (where simd is simd8 or simd16):
        //          /--*  op1  T
        //   node = *  HWINTRINSIC   simd   T Create

        // We will be constructing the following parts:
        //           /--*  op1  T
        //   node  = *  HWINTRINSIC   simd   T DuplicateToVector

        // This is roughly the following managed code:
        //   return AdvSimd.Arm64.DuplicateToVector(op1);

        if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
        {
            node->ChangeHWIntrinsicId((simdType == TYP_SIMD8) ? NI_AdvSimd_Arm64_DuplicateToVector64
                                                              : NI_AdvSimd_Arm64_DuplicateToVector128);
        }
        else
        {
            node->ChangeHWIntrinsicId((simdType == TYP_SIMD8) ? NI_AdvSimd_DuplicateToVector64
                                                              : NI_AdvSimd_DuplicateToVector128);
        }

        return LowerNode(node);
    }

    // We have the following (where simd is simd8 or simd16):
    //          /--*  op1 T
    //          +--*  ... T
    //          +--*  opN T
    //   node = *  HWINTRINSIC   simd   T Create

    // We will be constructing the following parts:
    //          /--*  op1  T
    //   tmp1 = *  HWINTRINSIC   simd8  T CreateScalarUnsafe
    //   ...

    // This is roughly the following managed code:
    //   var tmp1 = Vector64.CreateScalarUnsafe(op1);
    //   ...

    NamedIntrinsic createScalar =
        (simdType == TYP_SIMD8) ? NI_Vector64_CreateScalarUnsafe : NI_Vector128_CreateScalarUnsafe;

    GenTree* tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, node->Op(1), createScalar, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(node->Op(1), tmp1);
    LowerNode(tmp1);

    // We will be constructing the following parts:
    //   ...
    //   idx  =    CNS_INT       int    N
    //          /--*  tmp1 simd
    //          +--*  idx  int
    //          +--*  opN  T
    //   tmp1 = *  HWINTRINSIC   simd   T Insert
    //   ...

    // This is roughly the following managed code:
    //   ...
    //   tmp1 = AdvSimd.Insert(tmp1, N, opN);
    //   ...

    unsigned N   = 0;
    GenTree* opN = nullptr;
    GenTree* idx = nullptr;

    for (N = 1; N < argCnt - 1; N++)
    {
        opN = node->Op(N + 1);
        idx = comp->gtNewIconNode(N);
        BlockRange().InsertBefore(opN, idx);

        tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, idx, opN, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(opN, tmp1);
        LowerNode(tmp1);
    }

    assert(N == (argCnt - 1));

    // For the last insert, we will reuse the existing node and so handle it here, outside the loop.
    opN = node->Op(argCnt);
    idx = comp->gtNewIconNode(N);
    BlockRange().InsertBefore(opN, idx);

    node->ResetHWIntrinsicId(NI_AdvSimd_Insert, comp, tmp1, idx, opN);

    return LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector64 or Vector128 Dot call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicDot(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    assert((intrinsicId == NI_Vector64_Dot) || (intrinsicId == NI_Vector128_Dot));
    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;

    if (simdSize == 12)
    {
        assert(simdBaseType == TYP_FLOAT);

        // For 12 byte SIMD, we need to clear the upper 4 bytes:
        //   idx  =    CNS_INT       int    0x03
        //   tmp1 = *  CNS_DLB       float  0.0
        //          /--*  op1  simd16
        //          +--*  idx  int
        //          +--*  tmp1 simd16
        //   op1  = *  HWINTRINSIC   simd16 T Insert
        //   ...

        // This is roughly the following managed code:
        //    op1 = AdvSimd.Insert(op1, 0x03, 0.0f);
        //    ...

        idx = comp->gtNewIconNode(0x03, TYP_INT);
        BlockRange().InsertAfter(op1, idx);

        tmp1 = comp->gtNewZeroConNode(TYP_FLOAT);
        BlockRange().InsertAfter(idx, tmp1);
        LowerNode(tmp1);

        op1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, idx, tmp1, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(tmp1, op1);
        LowerNode(op1);

        idx = comp->gtNewIconNode(0x03, TYP_INT);
        BlockRange().InsertAfter(op2, idx);

        tmp2 = comp->gtNewZeroConNode(TYP_FLOAT);
        BlockRange().InsertAfter(idx, tmp2);
        LowerNode(tmp2);

        op2 = comp->gtNewSimdHWIntrinsicNode(simdType, op2, idx, tmp2, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(tmp2, op2);
        LowerNode(op2);
    }

    // We will be constructing the following parts:
    //   ...
    //          /--*  op1  simd16
    //          +--*  op2  simd16
    //   tmp1 = *  HWINTRINSIC   simd16 T Multiply
    //   ...

    // This is roughly the following managed code:
    //   ...
    //   var tmp1 = AdvSimd.Multiply(op1, op2);
    //   ...

    NamedIntrinsic multiply = NI_AdvSimd_Multiply;

    if (simdBaseType == TYP_DOUBLE)
    {
        multiply = (simdSize == 8) ? NI_AdvSimd_MultiplyScalar : NI_AdvSimd_Arm64_Multiply;
    }
    assert(!varTypeIsLong(simdBaseType));

    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, multiply, simdBaseJitType, simdSize);
    BlockRange().InsertBefore(node, tmp1);
    LowerNode(tmp1);

    if (varTypeIsFloating(simdBaseType))
    {
        if ((simdSize != 8) || (simdBaseType == TYP_FLOAT))
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          *  STORE_LCL_VAR simd16
            //   tmp1 =    LCL_VAR       simd16
            //   tmp2 =    LCL_VAR       simd16
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = tmp1;
            //   ...

            node->Op(1) = tmp1;
            LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
            ReplaceWithLclVar(tmp1Use);
            tmp1 = node->Op(1);

            tmp2 = comp->gtClone(tmp1);
            BlockRange().InsertAfter(tmp1, tmp2);
        }

        if (simdSize == 8)
        {
            if (simdBaseType == TYP_FLOAT)
            {
                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd8
                //          +--*  tmp2 simd8
                //   tmp1 = *  HWINTRINSIC   simd8  T AddPairwise
                //   ...

                // This is roughly the following managed code:
                //   ...
                //   var tmp1 = AdvSimd.AddPairwise(tmp1, tmp2);
                //   ...

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_AddPairwise, simdBaseJitType,
                                                      simdSize);
                BlockRange().InsertAfter(tmp2, tmp1);
                LowerNode(tmp1);
            }
            else
            {
                // No pairs to add for double, as its a single element
            }
        }
        else
        {
            assert((simdSize == 12) || (simdSize == 16));

            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   tmp2 = *  HWINTRINSIC   simd16 T AddPairwise
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   var tmp1 = AdvSimd.Arm64.AddPairwise(tmp1, tmp2);
            //   ...

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_Arm64_AddPairwise, simdBaseJitType,
                                                  simdSize);
            BlockRange().InsertAfter(tmp2, tmp1);
            LowerNode(tmp1);

            if (simdBaseType == TYP_FLOAT)
            {
                // Float needs an additional pairwise add to finish summing the parts
                // The first will have summed e0 with e1 and e2 with e3 and then repeats that for the upper half
                // So, we will have a vector that looks like this:
                //    < e0 + e1, e2 + e3, e0 + e1, e2 + e3>
                // Doing a second horizontal add with itself will then give us
                //    e0 + e1 + e2 + e3 in all elements of the vector

                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   tmp2 = *  HWINTRINSIC   simd16 T AddPairwise
                //   ...

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   var tmp1 = AdvSimd.Arm64.AddPairwise(tmp1, tmp2);
                //   ...

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_Arm64_AddPairwise,
                                                      simdBaseJitType, simdSize);
                BlockRange().InsertAfter(tmp2, tmp1);
                LowerNode(tmp1);
            }
        }

        tmp2 = tmp1;
    }
    else
    {
        assert(varTypeIsIntegral(simdBaseType));

        if ((simdSize == 8) && ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT)))
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          *  STORE_LCL_VAR simd16
            //   tmp1 =    LCL_VAR       simd16
            //   tmp2 =    LCL_VAR       simd16
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = tmp1;
            //   ...

            node->Op(1) = tmp1;
            LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
            ReplaceWithLclVar(tmp1Use);
            tmp1 = node->Op(1);

            tmp2 = comp->gtClone(tmp1);
            BlockRange().InsertAfter(tmp1, tmp2);

            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          /--*  tmp2 simd16
            //   tmp2 = *  HWINTRINSIC   simd8 T AddPairwise
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = AdvSimd.AddPairwise(tmp1, tmp2);
            //   ...

            tmp1 =
                comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_AddPairwise, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(tmp2, tmp1);
            LowerNode(tmp1);

            tmp2 = tmp1;
        }
        else
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //   tmp2 = *  HWINTRINSIC   simd16 T AddAcross
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = AdvSimd.Arm64.AddAcross(tmp1);
            //   ...

            tmp2 =
                comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(tmp1, tmp2);
            LowerNode(tmp2);
        }
    }

    // We will be constructing the following parts:
    //   ...
    //          /--*  tmp2 simd16
    //   node = *  HWINTRINSIC   simd16 T ToScalar

    // This is roughly the following managed code:
    //   ...
    //   return tmp2.ToScalar();

    node->ResetHWIntrinsicId((simdSize == 8) ? NI_Vector64_ToScalar : NI_Vector128_ToScalar, tmp2);

    LowerNode(node);
    return node->gtNext;
}
#endif // FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// Containment analysis
//------------------------------------------------------------------------

//------------------------------------------------------------------------
// ContainCheckCallOperands: Determine whether operands of a call should be contained.
//
// Arguments:
//    call       - The call node of interest
//
// Return Value:
//    None.
//
void Lowering::ContainCheckCallOperands(GenTreeCall* call)
{
    // There are no contained operands for arm.
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
#ifdef TARGET_ARM64
    GenTree* src = node->Data();
    if (src->IsIntegralConst(0))
    {
        // an integer zero for 'src' can be contained.
        MakeSrcContained(node, src);
    }
#endif // TARGET_ARM64
    ContainCheckIndir(node);
}

//------------------------------------------------------------------------
// ContainCheckIndir: Determine whether operands of an indir should be contained.
//
// Arguments:
//    indirNode - The indirection node of interest
//
// Notes:
//    This is called for both store and load indirections.
//
// Return Value:
//    None.
//
void Lowering::ContainCheckIndir(GenTreeIndir* indirNode)
{
    // If this is the rhs of a block copy it will be handled when we handle the store.
    if (indirNode->TypeGet() == TYP_STRUCT)
    {
        return;
    }

#ifdef FEATURE_SIMD
    // If indirTree is of TYP_SIMD12, don't mark addr as contained
    // so that it always get computed to a register.  This would
    // mean codegen side logic doesn't need to handle all possible
    // addr expressions that could be contained.
    //
    // TODO-ARM64-CQ: handle other addr mode expressions that could be marked
    // as contained.
    if (indirNode->TypeGet() == TYP_SIMD12)
    {
        return;
    }
#endif // FEATURE_SIMD

    GenTree* addr = indirNode->Addr();

    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirNode, addr))
    {
        bool makeContained = true;

#ifdef TARGET_ARM
        // ARM floating-point load/store doesn't support a form similar to integer
        // ldr Rdst, [Rbase + Roffset] with offset in a register. The only supported
        // form is vldr Rdst, [Rbase + imm] with a more limited constraint on the imm.
        GenTreeAddrMode* lea = addr->AsAddrMode();
        int              cns = lea->Offset();
        if (lea->HasIndex() || !emitter::emitIns_valid_imm_for_vldst_offset(cns))
        {
            if (indirNode->OperGet() == GT_STOREIND)
            {
                if (varTypeIsFloating(indirNode->AsStoreInd()->Data()))
                {
                    makeContained = false;
                }
            }
            else if (indirNode->OperGet() == GT_IND)
            {
                if (varTypeIsFloating(indirNode))
                {
                    makeContained = false;
                }
            }
        }
#endif // TARGET_ARM

        if (makeContained)
        {
            MakeSrcContained(indirNode, addr);
        }
    }
    else if (addr->OperIs(GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR is a stack addr mode.
        MakeSrcContained(indirNode, addr);
    }
#ifdef TARGET_ARM64
    else if (addr->OperIs(GT_CLS_VAR_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_CLS_VAR_ADDR turns into a constant.
        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(indirNode, addr);
    }
#endif // TARGET_ARM64
}

//------------------------------------------------------------------------
// ContainCheckBinary: Determine whether a binary op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckBinary(GenTreeOp* node)
{
    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    if (CheckImmedAndMakeContained(node, op2))
    {
        return;
    }

    if (node->OperIsCommutative() && CheckImmedAndMakeContained(node, op1))
    {
        MakeSrcContained(node, op1);
        std::swap(node->gtOp1, node->gtOp2);
        return;
    }

#ifdef TARGET_ARM64
    if (comp->opts.OptimizationEnabled())
    {
        if (IsContainableBinaryOp(node, op2))
        {
            if (op2->OperIs(GT_CAST))
            {
                // We want to prefer the combined op here over containment of the cast op
                op2->AsCast()->CastOp()->ClearContained();
            }
            MakeSrcContained(node, op2);

            return;
        }

        if (node->OperIsCommutative() && IsContainableBinaryOp(node, op1))
        {
            if (op1->OperIs(GT_CAST))
            {
                // We want to prefer the combined op here over containment of the cast op
                op1->AsCast()->CastOp()->ClearContained();
            }
            MakeSrcContained(node, op1);

            std::swap(node->gtOp1, node->gtOp2);
            return;
        }
    }
#endif
}

//------------------------------------------------------------------------
// ContainCheckMul: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckMul(GenTreeOp* node)
{
    ContainCheckBinary(node);
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
    assert(node->OperIs(GT_DIV, GT_UDIV, GT_MOD));

    // ARM doesn't have a div instruction with an immediate operand
}

//------------------------------------------------------------------------
// ContainCheckShiftRotate: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
    GenTree* shiftBy = node->gtOp2;
    assert(node->OperIsShiftOrRotate());

#ifdef TARGET_ARM
    GenTree* source = node->gtOp1;
    if (node->OperIs(GT_LSH_HI, GT_RSH_LO))
    {
        assert(source->OperGet() == GT_LONG);
        MakeSrcContained(node, source);
    }
#endif // TARGET_ARM

    if (shiftBy->IsCnsIntOrI())
    {
        MakeSrcContained(node, shiftBy);
    }
}

//------------------------------------------------------------------------
// ContainCheckStoreLoc: determine whether the source of a STORE_LCL* should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const
{
    assert(storeLoc->OperIsLocalStore());
    GenTree* op1 = storeLoc->gtGetOp1();

    if (op1->OperIs(GT_BITCAST))
    {
        // If we know that the source of the bitcast will be in a register, then we can make
        // the bitcast itself contained. This will allow us to store directly from the other
        // type if this node doesn't get a register.
        GenTree* bitCastSrc = op1->gtGetOp1();
        if (!bitCastSrc->isContained() && !bitCastSrc->IsRegOptional())
        {
            op1->SetContained();
            return;
        }
    }

    const LclVarDsc* varDsc = comp->lvaGetDesc(storeLoc);

#ifdef FEATURE_SIMD
    if (storeLoc->TypeIs(TYP_SIMD8, TYP_SIMD12))
    {
        // If this is a store to memory, we can initialize a zero vector in memory from REG_ZR.
        if ((op1->IsIntegralConst(0) || op1->IsVectorZero()) && varDsc->lvDoNotEnregister)
        {
            MakeSrcContained(storeLoc, op1);
        }
        return;
    }
#endif // FEATURE_SIMD

#ifdef TARGET_ARM64
    if (IsContainableImmed(storeLoc, op1))
    {
        MakeSrcContained(storeLoc, op1);
    }
#else
    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    var_types type = varDsc->GetRegisterType(storeLoc);
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(type)))
    {
        MakeSrcContained(storeLoc, op1);
    }
    else if (op1->OperGet() == GT_LONG)
    {
        MakeSrcContained(storeLoc, op1);
    }
#endif // TARGET_ARM
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
    GenTree*  castOp     = node->CastOp();
    var_types castToType = node->CastToType();

    if (comp->opts.OptimizationEnabled() && !node->gtOverflow() && varTypeIsIntegral(castOp) &&
        varTypeIsIntegral(castToType))
    {
        // Most integral casts can be re-expressed as loads, except those that would be changing the sign.
        if (!varTypeIsSmall(castOp) || (varTypeIsUnsigned(castOp) == node->IsZeroExtending()))
        {
            bool srcIsContainable = false;

            // Make sure to only contain indirections codegen can handle.
            if (castOp->OperIs(GT_IND))
            {
                GenTreeIndir* indir = castOp->AsIndir();

                if (!indir->IsVolatile() && !indir->IsUnaligned())
                {
                    GenTree* addr = indir->Addr();

                    if (!addr->isContained())
                    {
                        srcIsContainable = true;
                    }
                    else if (addr->OperIs(GT_LEA) && !addr->AsAddrMode()->HasIndex())
                    {
                        var_types loadType = varTypeIsSmall(castToType) ? castToType : castOp->TypeGet();

                        if (emitter::emitIns_valid_imm_for_ldst_offset(addr->AsAddrMode()->Offset(),
                                                                       emitTypeSize(loadType)))
                        {
                            srcIsContainable = true;
                        }
                    }
                }
            }
            else
            {
                assert(castOp->OperIsLocalRead() || !IsContainableMemoryOp(castOp));
                srcIsContainable = true;
            }

            if (srcIsContainable && IsSafeToContainMem(node, castOp))
            {
                if (IsContainableMemoryOp(castOp))
                {
                    MakeSrcContained(node, castOp);
                }
                else
                {
                    castOp->SetRegOptional();
                }
            }
        }
    }

#ifdef TARGET_ARM
    if (varTypeIsLong(castOp))
    {
        assert(castOp->OperGet() == GT_LONG);
        MakeSrcContained(node, castOp);
    }
#endif // TARGET_ARM
}

//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    CheckImmedAndMakeContained(cmp, cmp->gtOp2);
}

#ifdef TARGET_ARM64
//------------------------------------------------------------------------
// IsValidCompareChain : Determine if the node contains a valid chain of ANDs and CMPs.
//
// Arguments:
//    child - pointer to the node being checked.
//    parent - parent node of the child.
//
// Return value:
//    True if a valid chain is found.
//
// Notes:
//    A compare chain is a sequence of CMP nodes connected by AND nodes.
//    For example:   AND (AND (CMP A B) (CMP C D)) (CMP E F)
//    The chain can just be a single compare node, however it's parent
//    must always be an AND or SELECT node.
//    If a CMP or AND node is contained then it and all it's children are
//    considered to be in a valid chain.
//    Chains are built up during the lowering of each successive parent.
//
bool Lowering::IsValidCompareChain(GenTree* child, GenTree* parent)
{
    assert(parent->OperIs(GT_AND) || parent->OperIs(GT_SELECT));

    if (parent->isContainedCompareChainSegment(child))
    {
        // Already have a chain.
        return true;
    }
    else if (child->OperIs(GT_AND))
    {
        // Count both sides.
        return IsValidCompareChain(child->AsOp()->gtGetOp2(), child) &&
               IsValidCompareChain(child->AsOp()->gtGetOp1(), child);
    }
    else if (child->OperIsCmpCompare() && varTypeIsIntegral(child->gtGetOp1()) && varTypeIsIntegral(child->gtGetOp2()))
    {
        // Can the child compare be contained.
        return IsSafeToContainMem(parent, child);
    }

    return false;
}

//------------------------------------------------------------------------
// ContainCheckCompareChain : Determine if a chain of ANDs and CMPs can be contained.
//
// Arguments:
//    child - pointer to the node being checked.
//    parent - parent node of the child.
//    startOfChain - If found, returns the earliest valid op in the chain.
//
// Return value:
//    True if a valid chain is was contained.
//
// Notes:
//    Assumes the chain was checked via IsValidCompareChain.
//
bool Lowering::ContainCheckCompareChain(GenTree* child, GenTree* parent, GenTree** startOfChain)
{
    assert(parent->OperIs(GT_AND) || parent->OperIs(GT_SELECT));
    *startOfChain = nullptr; // Nothing found yet.

    if (parent->isContainedCompareChainSegment(child))
    {
        // Already have a contained chain.
        return true;
    }
    // Can the child be contained.
    else if (IsSafeToContainMem(parent, child))
    {
        if (child->OperIs(GT_AND))
        {
            // If Op2 is not contained, then try to contain it.
            if (!child->isContainedCompareChainSegment(child->AsOp()->gtGetOp2()))
            {
                if (!ContainCheckCompareChain(child->gtGetOp2(), child, startOfChain))
                {
                    // Op2 must be contained in order to contain Op1 or the AND.
                    return false;
                }
            }

            // If Op1 is not contained, then try to contain it.
            if (!child->isContainedCompareChainSegment(child->AsOp()->gtGetOp1()))
            {
                if (!ContainCheckCompareChain(child->gtGetOp1(), child, startOfChain))
                {
                    return false;
                }
            }

            // Contain the AND.
            child->SetContained();
            return true;
        }
        else if (child->OperIsCmpCompare())
        {
            child->AsOp()->SetContained();

            // Ensure the children of the compare are contained correctly.
            child->AsOp()->gtGetOp1()->ClearContained();
            child->AsOp()->gtGetOp2()->ClearContained();
            ContainCheckConditionalCompare(child->AsOp());
            *startOfChain = child;
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// ContainCheckCompareChainForAnd : Determine if an AND is a containable chain
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompareChainForAnd(GenTree* tree)
{
    assert(tree->OperIs(GT_AND));

    if (!comp->opts.OptimizationEnabled())
    {
        return;
    }

    // First check there is a valid chain.
    if (IsValidCompareChain(tree->AsOp()->gtGetOp2(), tree) && IsValidCompareChain(tree->AsOp()->gtGetOp1(), tree))
    {
        GenTree* startOfChain = nullptr;

        // To ensure ordering at code generation, Op1 and the parent can
        // only be contained if Op2 is contained.
        if (ContainCheckCompareChain(tree->AsOp()->gtGetOp2(), tree, &startOfChain))
        {
            if (ContainCheckCompareChain(tree->AsOp()->gtGetOp1(), tree, &startOfChain))
            {
                // If op1 is the start of a chain, then it'll be generated as a standard compare.
                if (startOfChain != nullptr)
                {
                    // The earliest node in the chain will be generated as a standard compare.
                    assert(startOfChain->OperIsCmpCompare());
                    startOfChain->AsOp()->gtGetOp1()->ClearContained();
                    startOfChain->AsOp()->gtGetOp2()->ClearContained();
                    ContainCheckCompare(startOfChain->AsOp());
                }
            }
        }

        JITDUMP("Lowered `AND` chain:\n");
        DISPTREE(tree);
    }
}

//------------------------------------------------------------------------
// ContainCheckConditionalCompare: determine whether the source of a compare within a compare chain should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckConditionalCompare(GenTreeOp* cmp)
{
    assert(cmp->OperIsCmpCompare());
    GenTree* op2 = cmp->gtOp2;

    if (op2->IsCnsIntOrI() && !op2->AsIntCon()->ImmedValNeedsReloc(comp))
    {
        target_ssize_t immVal = (target_ssize_t)op2->AsIntCon()->gtIconVal;

        if (emitter::emitIns_valid_imm_for_ccmp(immVal))
        {
            MakeSrcContained(cmp, op2);
        }
    }
}

//------------------------------------------------------------------------
// ContainCheckSelect : determine whether the source of a select should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckSelect(GenTreeConditional* node)
{
    if (!comp->opts.OptimizationEnabled())
    {
        return;
    }

    if (node->gtCond->OperIsCompare())
    {
        // All compare node types (including TEST_) are containable.
        if (IsSafeToContainMem(node, node->gtCond))
        {
            node->gtCond->AsOp()->SetContained();
        }
    }
    else
    {
        // Check for a compare chain and try to contain it.
        GenTree* startOfChain = nullptr;
        ContainCheckCompareChain(node->gtCond, node, &startOfChain);

        if (startOfChain != nullptr)
        {
            // The earliest node in the chain will be generated as a standard compare.
            assert(startOfChain->OperIsCmpCompare());
            startOfChain->AsOp()->gtGetOp1()->ClearContained();
            startOfChain->AsOp()->gtGetOp2()->ClearContained();
            ContainCheckCompare(startOfChain->AsOp());
        }
    }
}

#endif // TARGET_ARM64

//------------------------------------------------------------------------
// ContainCheckBoundsChk: determine whether any source of a bounds check node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckBoundsChk(GenTreeBoundsChk* node)
{
    assert(node->OperIs(GT_BOUNDS_CHECK));
    if (!CheckImmedAndMakeContained(node, node->GetIndex()))
    {
        CheckImmedAndMakeContained(node, node->GetArrayLength());
    }
}

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------------------
// ContainCheckSIMD: Perform containment analysis for a SIMD intrinsic node.
//
//  Arguments:
//     simdNode - The SIMD intrinsic node.
//
void Lowering::ContainCheckSIMD(GenTreeSIMD* simdNode)
{
    switch (simdNode->GetSIMDIntrinsicId())
    {
        case SIMDIntrinsicInit:
        {
            GenTree* op1 = simdNode->Op(1);
            if (op1->IsIntegralConst(0))
            {
                MakeSrcContained(simdNode, op1);
            }
            break;
        }

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            CheckImmedAndMakeContained(simdNode, simdNode->Op(2));
            break;

        default:
            break;
    }
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// ContainCheckHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node)
{
    const HWIntrinsic intrin(node);

    const bool hasImmediateOperand = HWIntrinsicInfo::HasImmediateOperand(intrin.id);

    if ((intrin.category == HW_Category_ShiftLeftByImmediate) ||
        (intrin.category == HW_Category_ShiftRightByImmediate) ||
        ((intrin.category == HW_Category_SIMDByIndexedElement) && hasImmediateOperand))
    {
        switch (intrin.numOperands)
        {
            case 4:
                assert(varTypeIsIntegral(intrin.op4));
                if (intrin.op4->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op4);
                }
                break;

            case 3:
                assert(varTypeIsIntegral(intrin.op3));
                if (intrin.op3->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op3);
                }
                break;

            case 2:
                assert(varTypeIsIntegral(intrin.op2));
                if (intrin.op2->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op2);
                }
                break;

            default:
                unreached();
        }
    }
    else if (hasImmediateOperand || HWIntrinsicInfo::SupportsContainment(intrin.id))
    {
        switch (intrin.id)
        {
            case NI_AdvSimd_DuplicateSelectedScalarToVector64:
            case NI_AdvSimd_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Extract:
            case NI_AdvSimd_InsertScalar:
            case NI_AdvSimd_LoadAndInsertScalar:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op2));
                if (intrin.op2->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op2);
                }
                break;

            case NI_AdvSimd_ExtractVector64:
            case NI_AdvSimd_ExtractVector128:
            case NI_AdvSimd_StoreSelectedScalar:
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op3));
                if (intrin.op3->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op3);
                }
                break;

            case NI_AdvSimd_Insert:
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op2));

                if (intrin.op2->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op2);

                    if ((intrin.op2->AsIntCon()->gtIconVal == 0) && intrin.op3->IsCnsFltOrDbl())
                    {
                        assert(varTypeIsFloating(intrin.baseType));

                        const double dataValue = intrin.op3->AsDblCon()->DconValue();

                        if (comp->GetEmitter()->emitIns_valid_imm_for_fmov(dataValue))
                        {
                            MakeSrcContained(node, intrin.op3);
                        }
                    }
                }
                break;

            case NI_AdvSimd_Arm64_InsertSelectedScalar:
                assert(hasImmediateOperand);
                assert(intrin.op2->IsCnsIntOrI());
                assert(intrin.op4->IsCnsIntOrI());

                MakeSrcContained(node, intrin.op2);
                MakeSrcContained(node, intrin.op4);
                break;

            case NI_AdvSimd_CompareEqual:
            case NI_AdvSimd_Arm64_CompareEqual:
            case NI_AdvSimd_Arm64_CompareEqualScalar:
            {
                if (intrin.op1->IsVectorZero())
                {
                    GenTree* op1 = intrin.op1;
                    GenTree* op2 = intrin.op2;

                    assert(HWIntrinsicInfo::IsCommutative(intrin.id));
                    MakeSrcContained(node, op1);

                    // Swap the operands here to make the containment checks in codegen simpler
                    node->Op(1) = op2;
                    node->Op(2) = op1;
                }
                else if (intrin.op2->IsVectorZero())
                {
                    MakeSrcContained(node, intrin.op2);
                }
                break;
            }

            case NI_AdvSimd_CompareGreaterThan:
            case NI_AdvSimd_CompareGreaterThanOrEqual:
            case NI_AdvSimd_Arm64_CompareGreaterThan:
            case NI_AdvSimd_Arm64_CompareGreaterThanOrEqual:
            case NI_AdvSimd_Arm64_CompareGreaterThanScalar:
            case NI_AdvSimd_Arm64_CompareGreaterThanOrEqualScalar:
            {
                // Containment is not supported for unsigned base types as the corresponding instructions:
                //    - cmhi
                //    - cmhs
                // require both operands; they do not have a 'with zero'.
                if (intrin.op2->IsVectorZero() && !varTypeIsUnsigned(intrin.baseType))
                {
                    MakeSrcContained(node, intrin.op2);
                }
                break;
            }

            case NI_Vector64_CreateScalarUnsafe:
            case NI_Vector128_CreateScalarUnsafe:
            case NI_AdvSimd_DuplicateToVector64:
            case NI_AdvSimd_DuplicateToVector128:
            case NI_AdvSimd_Arm64_DuplicateToVector64:
            case NI_AdvSimd_Arm64_DuplicateToVector128:
                if (IsValidConstForMovImm(node))
                {
                    MakeSrcContained(node, node->Op(1));
                }
                break;

            case NI_Vector64_GetElement:
            case NI_Vector128_GetElement:
            {
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op2));

                if (intrin.op2->IsCnsIntOrI())
                {
                    MakeSrcContained(node, intrin.op2);
                }

                if (IsContainableMemoryOp(intrin.op1))
                {
                    MakeSrcContained(node, intrin.op1);

                    if (intrin.op1->OperIs(GT_IND))
                    {
                        intrin.op1->AsIndir()->Addr()->ClearContained();
                    }
                }
                break;
            }

            default:
                unreached();
        }
    }
    else if ((intrin.id == NI_AdvSimd_LoadVector128) || (intrin.id == NI_AdvSimd_LoadVector64))
    {
        assert(intrin.numOperands == 1);
        assert(HWIntrinsicInfo::lookupCategory(intrin.id) == HW_Category_MemoryLoad);

        GenTree* addr = node->Op(1);
        if (TryCreateAddrMode(addr, true, node) && IsSafeToContainMem(node, addr))
        {
            assert(addr->OperIs(GT_LEA));
            MakeSrcContained(node, addr);
        }
    }
}
#endif // FEATURE_HW_INTRINSICS

#endif // TARGET_ARMARCH
