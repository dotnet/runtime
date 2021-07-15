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
// TODO-CQ: we can contain a floating point 0.0 constant in a compare instruction
// (vcmp on arm, fcmp on arm64).
//
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
{
    if (!varTypeIsFloating(parentNode->TypeGet()))
    {
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
            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif
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

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Widening operations of unsigneds.
//
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    GenTree* op1 = storeLoc->gtGetOp1();
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (op1->gtOper == GT_CNS_INT))
    {
        // Try to widen the ops if they are going into a local var.
        GenTreeIntCon* con    = op1->AsIntCon();
        ssize_t        ival   = con->gtIconVal;
        unsigned       varNum = storeLoc->GetLclNum();
        LclVarDsc*     varDsc = comp->lvaGetDesc(varNum);

        if (varDsc->lvIsSIMDType())
        {
            noway_assert(storeLoc->gtType != TYP_STRUCT);
        }
        unsigned size = genTypeSize(storeLoc);
        // If we are storing a constant into a local variable
        // we extend the size of the store here
        if ((size < 4) && !varTypeIsStruct(varDsc))
        {
            if (!varTypeIsUnsigned(varDsc))
            {
                if (genTypeSize(storeLoc) == 1)
                {
                    if ((ival & 0x7f) != ival)
                    {
                        ival = ival | 0xffffff00;
                    }
                }
                else
                {
                    assert(genTypeSize(storeLoc) == 2);
                    if ((ival & 0x7fff) != ival)
                    {
                        ival = ival | 0xffff0000;
                    }
                }
            }

            // A local stack slot is at least 4 bytes in size, regardless of
            // what the local var is typed as, so auto-promote it here
            // unless it is a field of a promoted struct
            // TODO-CQ: if the field is promoted shouldn't we also be able to do this?
            if (!varDsc->lvIsStructField)
            {
                storeLoc->gtType = TYP_INT;
                con->SetIconValue(ival);
            }
        }
    }
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

        if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (size <= INITBLK_UNROLL_LIMIT) && src->OperIs(GT_CNS_INT))
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

        if (src->OperIs(GT_IND))
        {
            // TODO-Cleanup: Make sure that GT_IND lowering didn't mark the source address as contained.
            // Sometimes the GT_IND type is a non-struct type and then GT_IND lowering may contain the
            // address, not knowing that GT_IND is part of a block op that has containment restrictions.
            src->AsIndir()->Addr()->ClearContained();
        }
        else if (src->OperIs(GT_LCL_VAR))
        {
            // TODO-1stClassStructs: for now we can't work with STORE_BLOCK source in register.
            const unsigned srcLclNum = src->AsLclVar()->GetLclNum();
            comp->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(Compiler::DNER_BlockOp));
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            if (!blkNode->AsObj()->GetLayout()->HasGCPtr())
            {
                blkNode->SetOper(GT_STORE_BLK);
            }
            else if (dstAddr->OperIsLocalAddr() && (size <= CPBLK_UNROLL_LIMIT))
            {
                // If the size is small enough to unroll then we need to mark the block as non-interruptible
                // to actually allow unrolling. The generated code does not report GC references loaded in the
                // temporary register(s) used for copying.
                blkNode->SetOper(GT_STORE_BLK);
                blkNode->gtBlkOpGcUnsafe = true;
            }
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
        }
        else if (blkNode->OperIs(GT_STORE_BLK) && (size <= CPBLK_UNROLL_LIMIT))
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

    // All integer load/store instructions on both ARM32 and ARM64 support
    // offsets in range -255..255. Of course, this is a rather conservative
    // check. For example, if the offset and size are a multiple of 8 we
    // could allow a combined offset of up to 32760 on ARM64.
    if ((offset < -255) || (offset > 255) || (offset + static_cast<int>(size) > 256))
    {
        return;
    }

#ifdef TARGET_ARM64
    // If we're going to use LDP/STP we need to ensure that the offset is
    // a multiple of 8 since these instructions do not have an unscaled
    // offset variant.
    if ((size >= 2 * REGSIZE_BYTES) && (offset % REGSIZE_BYTES != 0))
    {
        return;
    }
#endif

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
    var_types tmpType = TYP_UNDEF;

    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        assert(!varTypeIsSmall(dstType)); // fgMorphCast creates intermediate casts when converting from float to small
                                          // int.
    }

    assert(!varTypeIsSmall(srcType));

    if (tmpType != TYP_UNDEF)
    {
        GenTree* tmp = comp->gtNewCastNode(tmpType, op1, tree->IsUnsigned(), tmpType);
        tmp->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->AsOp()->gtOp1 = tmp;
        BlockRange().InsertAfter(op1, tmp);
    }

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

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------------------
// Lowering::LowerSIMD: Perform containment analysis for a SIMD intrinsic node.
//
//  Arguments:
//     simdNode - The SIMD intrinsic node.
//
void Lowering::LowerSIMD(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtType != TYP_SIMD32);

    if (simdNode->TypeGet() == TYP_SIMD12)
    {
        // GT_SIMD node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        simdNode->gtType = TYP_SIMD16;
    }

    ContainCheckSIMD(simdNode);
}
#endif // FEATURE_SIMD

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
    assert(node->gtHWIntrinsicId == NI_AdvSimd_FusedMultiplyAddScalar);

    const HWIntrinsic intrin(node);

    GenTree* op1 = intrin.op1;
    GenTree* op2 = intrin.op2;
    GenTree* op3 = intrin.op3;

    auto lowerOperand = [this](GenTree* op) {
        bool wasNegated = false;

        if (op->OperIsHWIntrinsic() &&
            ((op->AsHWIntrinsic()->gtHWIntrinsicId == NI_AdvSimd_Arm64_DuplicateToVector64) ||
             (op->AsHWIntrinsic()->gtHWIntrinsicId == NI_Vector64_CreateScalarUnsafe)))
        {
            GenTreeHWIntrinsic* createVector64 = op->AsHWIntrinsic();
            GenTree*            valueOp        = createVector64->gtGetOp1();

            if (valueOp->OperIs(GT_NEG))
            {
                createVector64->gtOp1 = valueOp->gtGetOp1();
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
            node->gtHWIntrinsicId = NI_AdvSimd_FusedMultiplyAddNegatedScalar;
        }
        else
        {
            node->gtHWIntrinsicId = NI_AdvSimd_FusedMultiplySubtractNegatedScalar;
        }
    }
    else if (op2WasNegated != op3WasNegated)
    {
        node->gtHWIntrinsicId = NI_AdvSimd_FusedMultiplySubtractScalar;
    }
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
    assert(node->TypeGet() != TYP_SIMD32);

    if (node->TypeGet() == TYP_SIMD12)
    {
        // GT_HWINTRINSIC node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        node->gtType = TYP_SIMD16;
    }

    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;

    switch (intrinsicId)
    {
        case NI_Vector64_Create:
        case NI_Vector128_Create:
        {
            // We don't directly support the Vector64.Create or Vector128.Create methods in codegen
            // and instead lower them to other intrinsic nodes in LowerHWIntrinsicCreate so we expect
            // that the node is modified to either not be a HWIntrinsic node or that it is no longer
            // the same intrinsic as when it came in.

            LowerHWIntrinsicCreate(node);
            assert(!node->OperIsHWIntrinsic() || (node->gtHWIntrinsicId != intrinsicId));
            LowerNode(node);
            return;
        }

        case NI_Vector64_Dot:
        case NI_Vector128_Dot:
        {
            LowerHWIntrinsicDot(node);
            return;
        }

        case NI_Vector64_op_Equality:
        case NI_Vector128_op_Equality:
        {
            LowerHWIntrinsicCmpOp(node, GT_EQ);
            return;
        }

        case NI_Vector64_op_Inequality:
        case NI_Vector128_op_Inequality:
        {
            LowerHWIntrinsicCmpOp(node, GT_NE);
            return;
        }

        case NI_AdvSimd_FusedMultiplyAddScalar:
            LowerHWIntrinsicFusedMultiplyAddScalar(node);
            break;

        default:
            break;
    }

    ContainCheckHWIntrinsic(node);
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
    assert((node->gtHWIntrinsicId == NI_Vector64_Create) || (node->gtHWIntrinsicId == NI_Vector128_Create) ||
           (node->gtHWIntrinsicId == NI_Vector64_CreateScalarUnsafe) ||
           (node->gtHWIntrinsicId == NI_Vector128_CreateScalarUnsafe) ||
           (node->gtHWIntrinsicId == NI_AdvSimd_DuplicateToVector64) ||
           (node->gtHWIntrinsicId == NI_AdvSimd_DuplicateToVector128) ||
           (node->gtHWIntrinsicId == NI_AdvSimd_Arm64_DuplicateToVector64) ||
           (node->gtHWIntrinsicId == NI_AdvSimd_Arm64_DuplicateToVector128));
    assert(HWIntrinsicInfo::lookupNumArgs(node) == 1);

    GenTree* op1    = node->gtOp1;
    GenTree* castOp = nullptr;

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

                BlockRange().Remove(node->gtOp1);
                node->gtOp1 = op1;
            }
            return true;
        }
    }
    else if (op1->IsCnsFltOrDbl())
    {
        assert(varTypeIsFloating(node->GetSimdBaseType()));
        assert(castOp == nullptr);

        const double dataValue = op1->AsDblCon()->gtDconVal;
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
void Lowering::LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
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

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

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
            cmpIntrinsic = NI_AdvSimd_Arm64_CompareEqual;
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

        GenTree* tmp = comp->gtNewSimdAsHWIntrinsicNode(simdType, cmp, idxCns, insCns, NI_AdvSimd_Insert,
                                                        CORINFO_TYPE_INT, simdSize);
        BlockRange().InsertAfter(insCns, tmp);
        LowerNode(tmp);

        cmp = tmp;
    }

    GenTree* msk =
        comp->gtNewSimdHWIntrinsicNode(simdType, cmp, NI_AdvSimd_Arm64_MinAcross, CORINFO_TYPE_UBYTE, simdSize);
    BlockRange().InsertAfter(cmp, msk);
    LowerNode(msk);

    GenTree* zroCns = comp->gtNewIconNode(0, TYP_INT);
    BlockRange().InsertAfter(msk, zroCns);

    GenTree* val =
        comp->gtNewSimdAsHWIntrinsicNode(TYP_UBYTE, msk, zroCns, NI_AdvSimd_Extract, CORINFO_TYPE_UBYTE, simdSize);
    BlockRange().InsertAfter(zroCns, val);
    LowerNode(val);

    zroCns = comp->gtNewIconNode(0, TYP_INT);
    BlockRange().InsertAfter(val, zroCns);

    node->ChangeOper(cmpOp);

    node->gtType = TYP_INT;
    node->gtOp1  = val;
    node->gtOp2  = zroCns;

    // The CompareEqual will set (condition is true) or clear (condition is false) all bits of the respective element
    // The MinAcross then ensures we get either all bits set (all conditions are true) or clear (any condition is false)
    // So, we need to invert the condition from the operation since we compare against zero

    GenCondition cmpCnd = (cmpOp == GT_EQ) ? GenCondition::NE : GenCondition::EQ;
    GenTree*     cc     = LowerNodeCC(node, cmpCnd);

    node->gtType = TYP_VOID;
    node->ClearUnusedValue();

    LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector64 or Vector128 Create call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
    var_types      simdType        = node->gtType;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    VectorConstant vecCns          = {};

    if ((simdSize == 8) && (simdType == TYP_DOUBLE))
    {
        // TODO-Cleanup: Struct retyping means we have the wrong type here. We need to
        //               manually fix it up so the simdType checks below are correct.
        simdType = TYP_SIMD8;
    }

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTreeArgList* argList = nullptr;
    GenTree*        op1     = node->gtGetOp1();
    GenTree*        op2     = node->gtGetOp2();

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;
    GenTree* tmp3 = nullptr;

    assert(op1 != nullptr);

    unsigned argCnt    = 0;
    unsigned cnsArgCnt = 0;

    if (op1->OperIsList())
    {
        assert(op2 == nullptr);

        for (argList = op1->AsArgList(); argList != nullptr; argList = argList->Rest())
        {
            if (HandleArgForHWIntrinsicCreate(argList->Current(), argCnt, vecCns, simdBaseType))
            {
                cnsArgCnt += 1;
            }
            argCnt += 1;
        }
    }
    else
    {
        if (HandleArgForHWIntrinsicCreate(op1, argCnt, vecCns, simdBaseType))
        {
            cnsArgCnt += 1;
        }
        argCnt += 1;

        if (op2 != nullptr)
        {
            if (HandleArgForHWIntrinsicCreate(op2, argCnt, vecCns, simdBaseType))
            {
                cnsArgCnt += 1;
            }
            argCnt += 1;
        }
        else if (cnsArgCnt == 1)
        {
            // These intrinsics are meant to set the same value to every element
            // so we'll just specially handle it here and copy it into the remaining
            // indices.

            for (unsigned i = 1; i < simdSize / genTypeSize(simdBaseType); i++)
            {
                HandleArgForHWIntrinsicCreate(op1, i, vecCns, simdBaseType);
            }
        }
    }
    assert((argCnt == 1) || (argCnt == (simdSize / genTypeSize(simdBaseType))));

    if ((argCnt == cnsArgCnt) && (argCnt == 1))
    {
        GenTree* castOp = nullptr;

        if (varTypeIsIntegral(simdBaseType) && op1->OperIs(GT_CAST))
        {
            // We will sometimes get a cast around a constant value (such as for
            // certain long constants) which would block the below containment.
            // So we will temporarily check what the cast is from instead so we
            // can catch those cases as well.

            castOp = op1->AsCast()->CastOp();
            op1    = castOp;
        }

        if (IsValidConstForMovImm(node))
        {
            // Set the cnsArgCnt to zero so we get lowered to a DuplicateToVector
            // intrinsic, which will itself mark the node as contained.
            cnsArgCnt = 0;

            // Reacquire op1 as the above check may have removed a cast node and
            // changed op1.
            op1 = node->gtOp1;
        }
    }

    if (argCnt == cnsArgCnt)
    {
        if (op1->OperIsList())
        {
            for (argList = op1->AsArgList(); argList != nullptr; argList = argList->Rest())
            {
                BlockRange().Remove(argList->Current());
            }
        }
        else
        {
            BlockRange().Remove(op1);

            if (op2 != nullptr)
            {
                BlockRange().Remove(op2);
            }
        }

        assert((simdSize == 8) || (simdSize == 16));

        if (VectorConstantIsBroadcastedI64(vecCns, simdSize / 8))
        {
            // If we are a single constant or if all parts are the same, we might be able to optimize
            // this even further for certain values, such as Zero or AllBitsSet.

            if (vecCns.i64[0] == 0)
            {
                node->gtOp1           = nullptr;
                node->gtOp2           = nullptr;
                node->gtHWIntrinsicId = (simdSize == 8) ? NI_Vector64_get_Zero : NI_Vector128_get_Zero;
                return;
            }
            else if (vecCns.i64[0] == -1)
            {
                node->gtOp1           = nullptr;
                node->gtOp2           = nullptr;
                node->gtHWIntrinsicId = (simdSize == 8) ? NI_Vector64_get_AllBitsSet : NI_Vector128_get_AllBitsSet;
                return;
            }
        }

        unsigned  cnsSize  = (simdSize == 12) ? 16 : simdSize;
        unsigned  cnsAlign = cnsSize;
        var_types dataType = Compiler::getSIMDTypeForSize(simdSize);

        UNATIVE_OFFSET       cnum = comp->GetEmitter()->emitDataConst(&vecCns, cnsSize, cnsAlign, dataType);
        CORINFO_FIELD_HANDLE hnd  = comp->eeFindJitDataOffs(cnum);
        GenTree* clsVarAddr = new (comp, GT_CLS_VAR_ADDR) GenTreeClsVar(GT_CLS_VAR_ADDR, TYP_I_IMPL, hnd, nullptr);
        BlockRange().InsertBefore(node, clsVarAddr);

        node->ChangeOper(GT_IND);
        node->gtOp1 = clsVarAddr;

        // TODO-ARM64-CQ: We should be able to modify at least the paths that use Insert to trivially support partial
        // vector constants. With this, we can create a constant if say 50% of the inputs are also constant and just
        // insert the non-constant values which should still allow some gains.

        return;
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
            node->gtHWIntrinsicId =
                (simdType == TYP_SIMD8) ? NI_AdvSimd_Arm64_DuplicateToVector64 : NI_AdvSimd_Arm64_DuplicateToVector128;
        }
        else
        {
            node->gtHWIntrinsicId =
                (simdType == TYP_SIMD8) ? NI_AdvSimd_DuplicateToVector64 : NI_AdvSimd_DuplicateToVector128;
        }
        return;
    }

    // We have the following (where simd is simd8 or simd16):
    //          /--*  op1 T
    //          +--*  ... T
    //          +--*  opN T
    //   node = *  HWINTRINSIC   simd   T Create

    if (op1->OperIsList())
    {
        argList = op1->AsArgList();
        op1     = argList->Current();
        argList = argList->Rest();
    }

    // We will be constructing the following parts:
    //          /--*  op1  T
    //   tmp1 = *  HWINTRINSIC   simd8  T CreateScalarUnsafe
    //   ...

    // This is roughly the following managed code:
    //   var tmp1 = Vector64.CreateScalarUnsafe(op1);
    //   ...

    NamedIntrinsic createScalarUnsafe =
        (simdType == TYP_SIMD8) ? NI_Vector64_CreateScalarUnsafe : NI_Vector128_CreateScalarUnsafe;

    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, createScalarUnsafe, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(op1, tmp1);
    LowerNode(tmp1);

    unsigned N   = 0;
    GenTree* opN = nullptr;

    for (N = 1; N < argCnt - 1; N++)
    {
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

        opN = argList->Current();

        idx = comp->gtNewIconNode(N, TYP_INT);
        BlockRange().InsertBefore(opN, idx);

        tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, idx, opN, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(opN, tmp1);
        LowerNode(tmp1);

        argList = argList->Rest();
    }

    assert(N == (argCnt - 1));

    // We will be constructing the following parts:
    //   idx  =    CNS_INT       int    N
    //          /--*  tmp1 simd
    //          +--*  idx  int
    //          +--*  opN  T
    //   node = *  HWINTRINSIC   simd   T Insert

    // This is roughly the following managed code:
    //   ...
    //   tmp1 = AdvSimd.Insert(tmp1, N, opN);
    //   ...

    opN = (argCnt == 2) ? op2 : argList->Current();

    idx = comp->gtNewIconNode(N, TYP_INT);
    BlockRange().InsertBefore(opN, idx);

    node->gtOp1 = comp->gtNewArgList(tmp1, idx, opN);
    node->gtOp2 = nullptr;

    node->gtHWIntrinsicId = NI_AdvSimd_Insert;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector64 or Vector128 Dot call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicDot(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    assert((intrinsicId == NI_Vector64_Dot) || (intrinsicId == NI_Vector128_Dot));
    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    assert(op1 != nullptr);
    assert(op2 != nullptr);
    assert(!op1->OperIsList());

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

        op1 = comp->gtNewSimdAsHWIntrinsicNode(simdType, op1, idx, tmp1, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(tmp1, op1);
        LowerNode(op1);
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

    NamedIntrinsic multiply = (simdBaseType == TYP_DOUBLE) ? NI_AdvSimd_Arm64_Multiply : NI_AdvSimd_Multiply;
    assert(!varTypeIsLong(simdBaseType));

    tmp1 = comp->gtNewSimdAsHWIntrinsicNode(simdType, op1, op2, multiply, simdBaseJitType, simdSize);
    BlockRange().InsertBefore(node, tmp1);
    LowerNode(tmp1);

    if (varTypeIsFloating(simdBaseType))
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

        node->gtOp1 = tmp1;
        LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
        ReplaceWithLclVar(tmp1Use);
        tmp1 = node->gtOp1;

        tmp2 = comp->gtClone(tmp1);
        BlockRange().InsertAfter(tmp1, tmp2);

        if (simdSize == 8)
        {
            assert(simdBaseType == TYP_FLOAT);

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

            tmp1 = comp->gtNewSimdAsHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_AddPairwise, simdBaseJitType,
                                                    simdSize);
            BlockRange().InsertAfter(tmp2, tmp1);
            LowerNode(tmp1);
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

            tmp1 = comp->gtNewSimdAsHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_Arm64_AddPairwise, simdBaseJitType,
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

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                tmp1 = comp->gtNewSimdAsHWIntrinsicNode(simdType, tmp1, tmp2, NI_AdvSimd_Arm64_AddPairwise,
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

        // We will be constructing the following parts:
        //   ...
        //          /--*  tmp1 simd16
        //   tmp2 = *  HWINTRINSIC   simd16 T AddAcross
        //   ...

        // This is roughly the following managed code:
        //   ...
        //   var tmp2 = AdvSimd.Arm64.AddAcross(tmp1);
        //   ...

        tmp2 = comp->gtNewSimdAsHWIntrinsicNode(simdType, tmp1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(tmp1, tmp2);
        LowerNode(tmp2);
    }

    // We will be constructing the following parts:
    //   ...
    //          /--*  tmp2 simd16
    //   node = *  HWINTRINSIC   simd16 T ToScalar

    // This is roughly the following managed code:
    //   ...
    //   return tmp2.ToScalar();

    node->gtOp1 = tmp2;
    node->gtOp2 = nullptr;

    node->gtHWIntrinsicId = (simdSize == 8) ? NI_Vector64_ToScalar : NI_Vector128_ToScalar;
    LowerNode(node);

    return;
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
    // Check and make op2 contained (if it is a containable immediate)
    CheckImmedAndMakeContained(node, node->gtOp2);
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
    assert(node->OperIs(GT_DIV, GT_UDIV));

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

#if 0
    // TODO-ARMARCH-CQ: support contained bitcast under STORE_LCL_VAR/FLD,
    // currently codegen does not expect it.
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
#endif

    const LclVarDsc* varDsc = comp->lvaGetDesc(storeLoc);

#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(storeLoc))
    {
        // If this is a store to memory, we can initialize a zero vector in memory from REG_ZR.
        if ((op1->IsIntegralConst(0) || op1->IsSIMDZero()) && varDsc->lvDoNotEnregister)
        {
            MakeSrcContained(storeLoc, op1);
            if (op1->IsSIMDZero())
            {
                MakeSrcContained(op1, op1->gtGetOp1());
            }
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
#ifdef TARGET_ARM
    GenTree*  castOp     = node->CastOp();
    var_types castToType = node->CastToType();
    var_types srcType    = castOp->TypeGet();

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

//------------------------------------------------------------------------
// ContainCheckBoundsChk: determine whether any source of a bounds check node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckBoundsChk(GenTreeBoundsChk* node)
{
    assert(node->OperIsBoundsCheck());
    if (!CheckImmedAndMakeContained(node, node->gtIndex))
    {
        CheckImmedAndMakeContained(node, node->gtArrLen);
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
    switch (simdNode->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
        {
            GenTree* op1 = simdNode->AsOp()->gtOp1;
            if (op1->IsIntegralConst(0))
            {
                MakeSrcContained(simdNode, op1);
            }
            break;
        }

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            CheckImmedAndMakeContained(simdNode, simdNode->gtGetOp2());
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

                        const double dataValue = intrin.op3->AsDblCon()->gtDconVal;

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

            case NI_Vector64_CreateScalarUnsafe:
            case NI_Vector128_CreateScalarUnsafe:
            case NI_AdvSimd_DuplicateToVector64:
            case NI_AdvSimd_DuplicateToVector128:
            case NI_AdvSimd_Arm64_DuplicateToVector64:
            case NI_AdvSimd_Arm64_DuplicateToVector128:
                if (IsValidConstForMovImm(node))
                {
                    // Use node->gtOp1 as the above check may
                    // have removed a cast node and changed op1

                    MakeSrcContained(node, node->gtOp1);
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
}
#endif // FEATURE_HW_INTRINSICS

#endif // TARGET_ARMARCH
