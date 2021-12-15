// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX             Lowering for LOONGARCH64 common code                               XX
XX                                                                           XX
XX  This encapsulates common logic for lowering trees for the LOONGARCH64         XX
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

#ifdef TARGET_LOONGARCH64 // This file is ONLY used for LOONGARCH64 architectures

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
    ////TODO for LOONGARCH64: should amend for optimize!
    //assert(!"unimplemented on LOONGARCH yet");
    //return comp->codeGen->validImmForBAL((ssize_t)addr);
    return false;
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
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->AsIntCon()->ImmedValNeedsReloc(comp))
            return false;

        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t type.
        target_ssize_t immVal = (target_ssize_t)childNode->AsIntCon()->gtIconVal;
        emitAttr       attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr       size   = EA_SIZE(attr);

        switch (parentNode->OperGet())
        {
            case GT_ADD:
                return comp->compOpportunisticallyDependsOn(InstructionSet_Atomics) ? false
                                                                  : ((-2048 <= immVal) && (immVal <= 2047));
                break;
            case GT_CMPXCHG:
            case GT_LOCKADD:
            case GT_XADD:
                assert(!"unimplemented on LOONGARCH yet");
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif
                return ((-32768 <= immVal) && (immVal <= 32767));
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                return ((-2048 <= immVal) && (immVal <= 2047));
            case GT_JCMP:
                assert(((parentNode->gtFlags & GTF_JCMP_TST) == 0) ? (immVal == 0) : isPow2(immVal));
                return true;

            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
                if (immVal == 0)
                    return true;
                break;

            default:
                break;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL/GT_MULHI/GT_MUL_LONG node.
//
// TODO: For LoongArch64 recognized GT_MULs that can be turned into GT_MUL_LONGs, as
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

    //if (comp->opts.OptimizationEnabled() && mul->OperIs(GT_MUL) && mul->IsValidLongMul())
    //{
    //    GenTreeCast* op1 = mul->gtGetOp1()->AsCast();
    //    GenTree*     op2 = mul->gtGetOp2();

    //    mul->ClearOverflow();
    //    mul->ClearUnsigned();
    //    if (op1->IsUnsigned())
    //    {
    //        mul->SetUnsigned();
    //    }

    //    mul->gtOp1 = op1->CastOp();
    //    BlockRange().Remove(op1);

    //    if (op2->OperIs(GT_CAST))
    //    {
    //        mul->gtOp2 = op2->AsCast()->CastOp();
    //        BlockRange().Remove(op2);
    //    }
    //    else
    //    {
    //        assert(op2->IsIntegralConst());
    //        assert(FitsIn<int32_t>(op2->AsIntConCommon()->IntegralValue()));

    //        op2->ChangeType(TYP_INT);
    //    }

    //    mul->ChangeOper(GT_MUL_LONG);
    //}

    ContainCheckMul(mul);

    return mul->gtNext;
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
    // Try to widen the ops if they are going into a local var.
    GenTree* op1 = storeLoc->gtGetOp1();
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (op1->gtOper == GT_CNS_INT))
    {
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
// LowerBlockStore: Set block store type
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void Lowering::LowerBlockStore(GenTreeBlk* blkNode)
{
    GenTree*  dstAddr  = blkNode->Addr();
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
                src->SetContained();;
            }
            else if (size >= REGSIZE_BYTES)
            {
                fill *= 0x0101010101010101LL;
                src->gtType = TYP_LONG;
            }
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
            comp->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
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

        // CopyObj or CopyBlk
        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
        }
////////////////////////////////////////////////////////////////////////////////////////////////////////
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
        // Convert ROL into ROR.
        GenTree* rotatedValue        = tree->AsOp()->gtOp1;
        unsigned rotatedValueBitSize = genTypeSize(rotatedValue->gtType) * 8;
        GenTree* rotateLeftIndexNode = tree->AsOp()->gtOp2;

        if (rotateLeftIndexNode->IsCnsIntOrI())
        {
            ssize_t rotateLeftIndex                 = rotateLeftIndexNode->AsIntCon()->gtIconVal;
            ssize_t rotateRightIndex                = rotatedValueBitSize - rotateLeftIndex;
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
assert(!"unimplemented on LOONGARCH yet");
#if 0
    assert(simdNode->gtType != TYP_SIMD32);

    if (simdNode->TypeGet() == TYP_SIMD12)
    {
        // GT_SIMD node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        simdNode->gtType = TYP_SIMD16;
    }

    ContainCheckSIMD(simdNode);
#endif
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
assert(!"unimplemented on LOONGARCH yet");
#if 0
    auto intrinsicID   = node->gtHWIntrinsicId;
    auto intrinsicInfo = HWIntrinsicInfo::lookup(node->gtHWIntrinsicId);

    //
    // Lower unsupported Unsigned Compare Zero intrinsics to their trivial transformations
    //
    // LOONGARCH64 does not support most forms of compare zero for Unsigned values
    // This is because some are non-sensical, and the rest are trivial transformations of other operators
    //
    if ((intrinsicInfo.flags & HWIntrinsicInfo::LowerCmpUZero) && varTypeIsUnsigned(node->gtSIMDBaseType))
    {
        auto setAllVector = node->gtSIMDSize > 8 ? NI_LOONGARCH64_SIMD_SetAllVector128 : NI_LOONGARCH64_SIMD_SetAllVector64;

        auto origOp1 = node->gtOp.gtOp1;

        switch (intrinsicID)
        {
            case NI_LOONGARCH64_SIMD_GT_ZERO:
                // Unsigned > 0 ==> !(Unsigned == 0)
                node->gtOp.gtOp1 =
                    comp->gtNewSimdHWIntrinsicNode(node->TypeGet(), node->gtOp.gtOp1, NI_LOONGARCH64_SIMD_EQ_ZERO,
                                                   node->gtSIMDBaseType, node->gtSIMDSize);
                node->gtHWIntrinsicId = NI_LOONGARCH64_SIMD_BitwiseNot;
                BlockRange().InsertBefore(node, node->gtOp.gtOp1);
                break;
            case NI_LOONGARCH64_SIMD_LE_ZERO:
                // Unsigned <= 0 ==> Unsigned == 0
                node->gtHWIntrinsicId = NI_LOONGARCH64_SIMD_EQ_ZERO;
                break;
            case NI_LOONGARCH64_SIMD_GE_ZERO:
            case NI_LOONGARCH64_SIMD_LT_ZERO:
                // Unsigned >= 0 ==> Always true
                // Unsigned < 0 ==> Always false
                node->gtHWIntrinsicId = setAllVector;
                node->gtOp.gtOp1      = comp->gtNewLconNode((intrinsicID == NI_LOONGARCH64_SIMD_GE_ZERO) ? ~0ULL : 0ULL);
                BlockRange().InsertBefore(node, node->gtOp.gtOp1);
                if ((origOp1->gtFlags & GTF_ALL_EFFECT) == 0)
                {
                    BlockRange().Remove(origOp1, true);
                }
                else
                {
                    origOp1->SetUnusedValue();
                }
                break;
            default:
                assert(!"Unhandled LowerCmpUZero case");
        }
    }

    ContainCheckHWIntrinsic(node);
#endif
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

        idx = comp->gtNewIconNode(0x03, TYP_INT);
        BlockRange().InsertAfter(op2, idx);

        tmp2 = comp->gtNewZeroConNode(TYP_FLOAT);
        BlockRange().InsertAfter(idx, tmp2);
        LowerNode(tmp2);

        op2 = comp->gtNewSimdAsHWIntrinsicNode(simdType, op2, idx, tmp2, NI_AdvSimd_Insert, simdBaseJitType, simdSize);
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
    // There are no contained operands for LOONGARCH.
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
#if 0
assert(!"unimplemented on LOONGARCH yet");
#else

    GenTree* src = node->Data();
    if (!varTypeIsFloating(src->TypeGet()) && src->IsIntegralConst(0))
    {
        // an integer zero for 'src' can be contained.
        MakeSrcContained(node, src);
    }

    ContainCheckIndir(node);

#endif
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
    assert(!"unimplemented on LOONGARCH yet");
    // If indirTree is of TYP_SIMD12, don't mark addr as contained
    // so that it always get computed to a register.  This would
    // mean codegen side logic doesn't need to handle all possible
    // addr expressions that could be contained.
    //
    // TODO-LOONGARCH64-CQ: handle other addr mode expressions that could be marked
    // as contained.
    if (indirNode->TypeGet() == TYP_SIMD12)
    {
        return;
    }
#endif // FEATURE_SIMD

    GenTree* addr          = indirNode->Addr();
    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirNode, addr))
    {
        MakeSrcContained(indirNode, addr);
    }
    else if (addr->OperIs(GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR is a stack addr mode.
        MakeSrcContained(indirNode, addr);
    }
    else if (addr->OperIs(GT_CLS_VAR_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_CLS_VAR_ADDR turns into a constant.
        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(indirNode, addr);
    }

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
    assert(node->OperIs(GT_MOD, GT_UMOD, GT_DIV, GT_UDIV));
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
    if (varTypeIsSIMD(storeLoc))
    {
        // If this is a store to memory, we can initialize a zero vector in memory from REG_ZR.
        if ((op1->IsIntegralConst(0) || op1->IsSIMDZero()) && varDsc->lvDoNotEnregister)
        {
            // For an InitBlk we want op1 to be contained
            MakeSrcContained(storeLoc, op1);
            if (op1->IsSIMDZero())
            {
                MakeSrcContained(op1, op1->gtGetOp1());
            }
        }
        return;
    }
#endif // FEATURE_SIMD
    if (IsContainableImmed(storeLoc, op1))
    {
        MakeSrcContained(storeLoc, op1);
    }

    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    var_types type = varDsc->GetRegisterType(storeLoc);
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(type)))
    {
        MakeSrcContained(storeLoc, op1);
    }
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
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
assert(!"unimplemented on LOONGARCH yet");
#if 0
    switch (simdNode->gtSIMDIntrinsicID)
    {
        GenTree* op1;
        GenTree* op2;

        case SIMDIntrinsicInit:
            op1 = simdNode->gtOp.gtOp1;
            if (op1->IsIntegralConst(0))
            {
                MakeSrcContained(simdNode, op1);
            }
            break;

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            CheckImmedAndMakeContained(simdNode, simdNode->gtGetOp2());
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            // TODO-LOONGARCH64-CQ Support containing 0
            break;

        case SIMDIntrinsicGetItem:
        {
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            op1 = simdNode->gtOp.gtOp1;
            op2 = simdNode->gtOp.gtOp2;

            // If the index is a constant, mark it as contained.
            if (op2->IsCnsIntOrI())
            {
                MakeSrcContained(simdNode, op2);
            }

            if (IsContainableMemoryOp(op1))
            {
                MakeSrcContained(simdNode, op1);
                if (op1->OperGet() == GT_IND)
                {
                    op1->AsIndir()->Addr()->ClearContained();
                }
            }
            break;
        }

        default:
            break;
    }
#endif
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
assert(!"unimplemented on LOONGARCH yet");
#if 0
    GenTreeArgList* argList = nullptr;
    GenTree*        op1     = node->gtOp.gtOp1;
    GenTree*        op2     = node->gtOp.gtOp2;

    if (op1->OperIs(GT_LIST))
    {
        argList = op1->AsArgList();
        op1     = argList->Current();
        op2     = argList->Rest()->Current();
    }

    switch (HWIntrinsicInfo::lookup(node->gtHWIntrinsicId).form)
    {
        case HWIntrinsicInfo::SimdExtractOp:
            if (op2->IsCnsIntOrI())
            {
                MakeSrcContained(node, op2);
            }
            break;

        case HWIntrinsicInfo::SimdInsertOp:
            if (op2->IsCnsIntOrI())
            {
                MakeSrcContained(node, op2);

#if 0
                // This is currently not supported downstream. The following (at least) need to be modifed:
                //   GenTree::isContainableHWIntrinsic() needs to handle this.
                //   CodeGen::genConsumRegs()
                //
                GenTree* op3 = argList->Rest()->Rest()->Current();

                // In the HW intrinsics C# API there is no direct way to specify a vector element to element mov
                //   VX[a] = VY[b]
                // In C# this would naturally be expressed by
                //   Insert(VX, a, Extract(VY, b))
                // If both a & b are immediate constants contain the extract/getItem so that we can emit
                //   the single instruction mov Vx[a], Vy[b]
                if (op3->OperIs(GT_HWIntrinsic) && (op3->AsHWIntrinsic()->gtHWIntrinsicId == NI_LOONGARCH64_SIMD_GetItem))
                {
                    ContainCheckHWIntrinsic(op3->AsHWIntrinsic());

                    if (op3->gtOp.gtOp2->isContained())
                    {
                        MakeSrcContained(node, op3);
                    }
                }
#endif
            }
            break;

        default:
            break;
    }
#endif
}
#endif // FEATURE_HW_INTRINSICS

#endif // TARGET_LOONGARCH64
