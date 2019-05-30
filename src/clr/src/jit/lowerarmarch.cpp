// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef _TARGET_ARMARCH_ // This file is ONLY used for ARM and ARM64 architectures

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
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (!varTypeIsFloating(parentNode->TypeGet()))
    {
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->gtIntCon.ImmedValNeedsReloc(comp))
            return false;

        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t type.
        target_ssize_t immVal = (target_ssize_t)childNode->gtIntCon.gtIconVal;
        emitAttr       attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr       size   = EA_SIZE(attr);
#ifdef _TARGET_ARM_
        insFlags flags = parentNode->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
#endif

        switch (parentNode->OperGet())
        {
            case GT_ADD:
            case GT_SUB:
#ifdef _TARGET_ARM64_
            case GT_CMPXCHG:
            case GT_LOCKADD:
            case GT_XADD:
                return comp->compSupports(InstructionSet_Atomics) ? false
                                                                  : emitter::emitIns_valid_imm_for_add(immVal, size);
#elif defined(_TARGET_ARM_)
                return emitter::emitIns_valid_imm_for_add(immVal, flags);
#endif
                break;

#ifdef _TARGET_ARM64_
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
#elif defined(_TARGET_ARM_)
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
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_
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
    // Try to widen the ops if they are going into a local var.
    GenTree* op1 = storeLoc->gtGetOp1();
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (op1->gtOper == GT_CNS_INT))
    {
        GenTreeIntCon* con    = op1->AsIntCon();
        ssize_t        ival   = con->gtIconVal;
        unsigned       varNum = storeLoc->gtLclNum;
        LclVarDsc*     varDsc = comp->lvaTable + varNum;

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
        verifyLclFldDoNotEnregister(storeLoc->gtLclNum);
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
void Lowering::LowerStoreIndir(GenTreeIndir* node)
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
    unsigned  size     = blkNode->gtBlkSize;
    GenTree*  source   = blkNode->Data();
    Compiler* compiler = comp;

    // Sources are dest address and initVal or source.
    GenTree* srcAddrOrFill = nullptr;
    bool     isInitBlk     = blkNode->OperIsInitBlkOp();

    if (!isInitBlk)
    {
        // CopyObj or CopyBlk
        if ((blkNode->OperGet() == GT_STORE_OBJ) && ((blkNode->AsObj()->gtGcPtrCount == 0) || blkNode->gtBlkOpGcUnsafe))
        {
            blkNode->SetOper(GT_STORE_BLK);
        }
        if (source->gtOper == GT_IND)
        {
            srcAddrOrFill = blkNode->Data()->gtGetOp1();
        }
    }

    if (isInitBlk)
    {
        GenTree* initVal = source;
        if (initVal->OperIsInitVal())
        {
            initVal->SetContained();
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;

#ifdef _TARGET_ARM64_
        if ((size != 0) && (size <= INITBLK_UNROLL_LIMIT) && initVal->IsCnsIntOrI())
        {
            // TODO-ARM-CQ: Currently we generate a helper call for every
            // initblk we encounter.  Later on we should implement loop unrolling
            // code sequences to improve CQ.
            // For reference see the code in LowerXArch.cpp.
            NYI_ARM("initblk loop unrolling is currently not implemented.");

            // The fill value of an initblk is interpreted to hold a
            // value of (unsigned int8) however a constant of any size
            // may practically reside on the evaluation stack. So extract
            // the lower byte out of the initVal constant and replicate
            // it to a larger constant whose size is sufficient to support
            // the largest width store of the desired inline expansion.

            ssize_t fill = initVal->gtIntCon.gtIconVal & 0xFF;
            if (fill == 0)
            {
                MakeSrcContained(blkNode, source);
            }
            else if (size < REGSIZE_BYTES)
            {
                initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
            }
            else
            {
                initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * fill;
                initVal->gtType             = TYP_LONG;
            }
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
        }
        else
#endif // _TARGET_ARM64_
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
        }
    }
    else
    {
        // CopyObj or CopyBlk
        // Sources are src and dest and size if not constant.

        if (blkNode->OperGet() == GT_STORE_OBJ)
        {
            // CopyObj
            GenTreeObj* objNode = blkNode->AsObj();

            unsigned slots = objNode->gtSlots;

#ifdef DEBUG
            // CpObj must always have at least one GC-Pointer as a member.
            assert(objNode->gtGcPtrCount > 0);

            assert(dstAddr->gtType == TYP_BYREF || dstAddr->gtType == TYP_I_IMPL);

            CORINFO_CLASS_HANDLE clsHnd    = objNode->gtClass;
            size_t               classSize = compiler->info.compCompHnd->getClassSize(clsHnd);
            size_t               blkSize   = roundUp(classSize, TARGET_POINTER_SIZE);

            // Currently, the EE always round up a class data structure so
            // we are not handling the case where we have a non multiple of pointer sized
            // struct. This behavior may change in the future so in order to keeps things correct
            // let's assert it just to be safe. Going forward we should simply
            // handle this case.
            assert(classSize == blkSize);
            assert((blkSize / TARGET_POINTER_SIZE) == slots);
            assert(objNode->HasGCPtr());
#endif

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
        }
        else // CopyBlk
        {
            // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
            // we should unroll the loop to improve CQ.
            // For reference see the code in lowerxarch.cpp.

            if ((size != 0) && (size <= CPBLK_UNROLL_LIMIT))
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
            else
            {
                // In case we have a constant integer this means we went beyond
                // CPBLK_UNROLL_LIMIT bytes of size, still we should never have the case of
                // any GC-Pointers in the src struct.
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
            }
        }
        // CopyObj or CopyBlk
        if (source->gtOper == GT_IND)
        {
            MakeSrcContained(blkNode, source);
            GenTree* addr = source->AsIndir()->Addr();
            if (!addr->OperIsLocalAddr())
            {
                addr->ClearContained();
            }
        }
        else if (!source->IsMultiRegCall() && !source->OperIsSimdOrHWintrinsic())
        {
            assert(source->IsLocal());
            MakeSrcContained(blkNode, source);
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

    GenTree*  op1     = tree->gtOp.gtOp1;
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
        tree->gtOp.gtOp1 = tmp;
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
        GenTree* rotatedValue        = tree->gtOp.gtOp1;
        unsigned rotatedValueBitSize = genTypeSize(rotatedValue->gtType) * 8;
        GenTree* rotateLeftIndexNode = tree->gtOp.gtOp2;

        if (rotateLeftIndexNode->IsCnsIntOrI())
        {
            ssize_t rotateLeftIndex                 = rotateLeftIndexNode->gtIntCon.gtIconVal;
            ssize_t rotateRightIndex                = rotatedValueBitSize - rotateLeftIndex;
            rotateLeftIndexNode->gtIntCon.gtIconVal = rotateRightIndex;
        }
        else
        {
            GenTree* tmp = comp->gtNewOperNode(GT_NEG, genActualType(rotateLeftIndexNode->gtType), rotateLeftIndexNode);
            BlockRange().InsertAfter(rotateLeftIndexNode, tmp);
            tree->gtOp.gtOp2 = tmp;
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
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
    auto intrinsicID   = node->gtHWIntrinsicId;
    auto intrinsicInfo = HWIntrinsicInfo::lookup(node->gtHWIntrinsicId);

    //
    // Lower unsupported Unsigned Compare Zero intrinsics to their trivial transformations
    //
    // ARM64 does not support most forms of compare zero for Unsigned values
    // This is because some are non-sensical, and the rest are trivial transformations of other operators
    //
    if ((intrinsicInfo.flags & HWIntrinsicInfo::LowerCmpUZero) && varTypeIsUnsigned(node->gtSIMDBaseType))
    {
        auto setAllVector = node->gtSIMDSize > 8 ? NI_ARM64_SIMD_SetAllVector128 : NI_ARM64_SIMD_SetAllVector64;

        auto origOp1 = node->gtOp.gtOp1;

        switch (intrinsicID)
        {
            case NI_ARM64_SIMD_GT_ZERO:
                // Unsigned > 0 ==> !(Unsigned == 0)
                node->gtOp.gtOp1 =
                    comp->gtNewSimdHWIntrinsicNode(node->TypeGet(), node->gtOp.gtOp1, NI_ARM64_SIMD_EQ_ZERO,
                                                   node->gtSIMDBaseType, node->gtSIMDSize);
                node->gtHWIntrinsicId = NI_ARM64_SIMD_BitwiseNot;
                BlockRange().InsertBefore(node, node->gtOp.gtOp1);
                break;
            case NI_ARM64_SIMD_LE_ZERO:
                // Unsigned <= 0 ==> Unsigned == 0
                node->gtHWIntrinsicId = NI_ARM64_SIMD_EQ_ZERO;
                break;
            case NI_ARM64_SIMD_GE_ZERO:
            case NI_ARM64_SIMD_LT_ZERO:
                // Unsigned >= 0 ==> Always true
                // Unsigned < 0 ==> Always false
                node->gtHWIntrinsicId = setAllVector;
                node->gtOp.gtOp1      = comp->gtNewLconNode((intrinsicID == NI_ARM64_SIMD_GE_ZERO) ? ~0ULL : 0ULL);
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
void Lowering::ContainCheckStoreIndir(GenTreeIndir* node)
{
#ifdef _TARGET_ARM64_
    GenTree* src = node->gtOp.gtOp2;
    if (!varTypeIsFloating(src->TypeGet()) && src->IsIntegralConst(0))
    {
        // an integer zero for 'src' can be contained.
        MakeSrcContained(node, src);
    }
#endif // _TARGET_ARM64_
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

    GenTree* addr          = indirNode->Addr();
    bool     makeContained = true;
    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirNode, addr))
    {
#ifdef _TARGET_ARM_
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
#endif
        if (makeContained)
        {
            MakeSrcContained(indirNode, addr);
        }
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

#ifdef _TARGET_ARM_
    GenTree* source = node->gtOp1;
    if (node->OperIs(GT_LSH_HI, GT_RSH_LO))
    {
        assert(source->OperGet() == GT_LONG);
        MakeSrcContained(node, source);
    }
#endif // _TARGET_ARM_

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
void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    assert(storeLoc->OperIsLocalStore());
    GenTree* op1 = storeLoc->gtGetOp1();

#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(storeLoc))
    {
        if (op1->IsIntegralConst(0))
        {
            // For an InitBlk we want op1 to be contained
            MakeSrcContained(storeLoc, op1);
        }
        return;
    }
#endif // FEATURE_SIMD

    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(storeLoc)))
    {
        MakeSrcContained(storeLoc, op1);
    }
#ifdef _TARGET_ARM_
    else if (op1->OperGet() == GT_LONG)
    {
        MakeSrcContained(storeLoc, op1);
    }
#endif // _TARGET_ARM_
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
#ifdef _TARGET_ARM_
    GenTree*  castOp     = node->CastOp();
    var_types castToType = node->CastToType();
    var_types srcType    = castOp->TypeGet();

    if (varTypeIsLong(castOp))
    {
        assert(castOp->OperGet() == GT_LONG);
        MakeSrcContained(node, castOp);
    }
#endif // _TARGET_ARM_
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
            // TODO-ARM64-CQ Support containing 0
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
                if (op3->OperIs(GT_HWIntrinsic) && (op3->AsHWIntrinsic()->gtHWIntrinsicId == NI_ARM64_SIMD_GetItem))
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
}
#endif // FEATURE_HW_INTRINSICS

#endif // _TARGET_ARMARCH_
