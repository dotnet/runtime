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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARMARCH_ // This file is ONLY used for ARM and ARM64 architectures

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
#ifdef _TARGET_ARM64_
    // TODO-ARM64-CQ:  This is a workaround to unblock the JIT from getting calls working.
    // Currently, we'll be generating calls using blr and manually loading an absolute
    // call target in a register using a sequence of load immediate instructions.
    //
    // As you can expect, this is inefficient and it's not the recommended way as per the
    // ARM64 ABI Manual but will get us getting things done for now.
    // The work to get this right would be to implement PC-relative calls, the bl instruction
    // can only address things -128 + 128MB away, so this will require getting some additional
    // code to get jump thunks working.
    return true;
#elif defined(_TARGET_ARM_)
    return comp->codeGen->validImmForBL((ssize_t)addr);
#endif
}

//------------------------------------------------------------------------
// IsContainableImmed: Is an immediate encodable in-place?
//
// Return Value:
//    True if the immediate can be folded into an instruction,
//    for example small enough and non-relocatable.
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
        // We can contain a floating point 0.0 constant in a compare instruction
        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                if (childNode->IsIntegralConst(0))
                {
                    // TODO-ARM-Cleanup: not tested yet.
                    NYI_ARM("ARM IsContainableImmed for floating point type");

                    return true;
                }
                break;
        }
    }
    else
    {
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->IsIconHandle() && comp->opts.compReloc)
            return false;

        ssize_t  immVal = childNode->gtIntCon.gtIconVal;
        emitAttr attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr size   = EA_SIZE(attr);
#ifdef _TARGET_ARM_
        insFlags flags = parentNode->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
#endif

        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_ADD:
            case GT_SUB:
#ifdef _TARGET_ARM64_
                return emitter::emitIns_valid_imm_for_add(immVal, size);
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
            case GT_TEST_EQ:
            case GT_TEST_NE:
                return emitter::emitIns_valid_imm_for_cmp(immVal, size);
                break;
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                return emitter::emitIns_valid_imm_for_alu(immVal, size);
                break;
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
                break;
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_
            case GT_STORE_LCL_VAR:
                if (immVal == 0)
                    return true;
                break;
#endif
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
    GenTreePtr srcAddrOrFill = nullptr;
    bool       isInitBlk     = blkNode->OperIsInitBlkOp();

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
        GenTreePtr initVal = source;
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
#ifdef _TARGET_ARM64_
            // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
            // we should unroll the loop to improve CQ.
            // For reference see the code in lowerxarch.cpp.
            // TODO-ARM-CQ: cpblk loop unrolling is currently not implemented.

            if ((size != 0) && (size <= INITBLK_UNROLL_LIMIT))
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
            else
#endif // _TARGET_ARM64_
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
        }
        else if (!source->IsMultiRegCall() && !source->OperIsSIMD())
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
//    Casts from small int type to float/double are transformed as follows:
//    GT_CAST(byte, float/double)     =   GT_CAST(GT_CAST(byte, int32), float/double)
//    GT_CAST(sbyte, float/double)    =   GT_CAST(GT_CAST(sbyte, int32), float/double)
//    GT_CAST(int16, float/double)    =   GT_CAST(GT_CAST(int16, int32), float/double)
//    GT_CAST(uint16, float/double)   =   GT_CAST(GT_CAST(uint16, int32), float/double)
//
//    Similarly casts from float/double to a smaller int type are transformed as follows:
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

    GenTreePtr op1     = tree->gtOp.gtOp1;
    var_types  dstType = tree->CastToType();
    var_types  srcType = op1->TypeGet();
    var_types  tmpType = TYP_UNDEF;

    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
    }

    // Case of src is a small type and dst is a floating point type.
    if (varTypeIsSmall(srcType) && varTypeIsFloating(dstType))
    {
        // These conversions can never be overflow detecting ones.
        noway_assert(!tree->gtOverflow());
        tmpType = TYP_INT;
    }
    // case of src is a floating point type and dst is a small type.
    else if (varTypeIsFloating(srcType) && varTypeIsSmall(dstType))
    {
        NYI_ARM("Lowering for cast from float to small type"); // Not tested yet.
        tmpType = TYP_INT;
    }

    if (tmpType != TYP_UNDEF)
    {
        GenTreePtr tmp = comp->gtNewCastNode(tmpType, op1, tmpType);
        tmp->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED | GTF_OVERFLOW | GTF_EXCEPT));

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
void Lowering::LowerRotate(GenTreePtr tree)
{
    if (tree->OperGet() == GT_ROL)
    {
        // There is no ROL instruction on ARM. Convert ROL into ROR.
        GenTreePtr rotatedValue        = tree->gtOp.gtOp1;
        unsigned   rotatedValueBitSize = genTypeSize(rotatedValue->gtType) * 8;
        GenTreePtr rotateLeftIndexNode = tree->gtOp.gtOp2;

        if (rotateLeftIndexNode->IsCnsIntOrI())
        {
            ssize_t rotateLeftIndex                 = rotateLeftIndexNode->gtIntCon.gtIconVal;
            ssize_t rotateRightIndex                = rotatedValueBitSize - rotateLeftIndex;
            rotateLeftIndexNode->gtIntCon.gtIconVal = rotateRightIndex;
        }
        else
        {
            GenTreePtr tmp =
                comp->gtNewOperNode(GT_NEG, genActualType(rotateLeftIndexNode->gtType), rotateLeftIndexNode);
            BlockRange().InsertAfter(rotateLeftIndexNode, tmp);
            tree->gtOp.gtOp2 = tmp;
        }
        tree->ChangeOper(GT_ROR);
    }
    ContainCheckShiftRotate(tree->AsOp());
}

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
    GenTree* ctrlExpr = call->gtControlExpr;
    // If there is an explicit this pointer, we don't want that node to produce anything
    // as it is redundant
    if (call->gtCallObjp != nullptr)
    {
        GenTreePtr thisPtrNode = call->gtCallObjp;

        if (thisPtrNode->canBeContained())
        {
            MakeSrcContained(call, thisPtrNode);
            if (thisPtrNode->gtOper == GT_PUTARG_REG)
            {
                MakeSrcContained(call, thisPtrNode->gtOp.gtOp1);
            }
        }
    }
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

    GenTree* addr          = indirNode->Addr();
    bool     makeContained = true;
    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirNode, addr))
    {
        GenTreeAddrMode* lea   = addr->AsAddrMode();
        GenTree*         base  = lea->Base();
        GenTree*         index = lea->Index();
        int              cns   = lea->Offset();

#ifdef _TARGET_ARM_
        // ARM floating-point load/store doesn't support a form similar to integer
        // ldr Rdst, [Rbase + Roffset] with offset in a register. The only supported
        // form is vldr Rdst, [Rbase + imm] with a more limited constraint on the imm.
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
// ContainCheckShiftRotate: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
    GenTreePtr shiftBy = node->gtOp2;

#ifdef _TARGET_ARM_
    GenTreePtr source = node->gtOp1;
    if (node->OperIs(GT_LSH_HI, GT_RSH_LO))
    {
        assert(source->OperGet() == GT_LONG);
        MakeSrcContained(node, source);
    }
#else  // !_TARGET_ARM_
    assert(node->OperIsShiftOrRotate());
#endif // !_TARGET_ARM_

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
        if (op1->IsCnsIntOrI())
        {
            // For an InitBlk we want op1 to be contained; otherwise we want it to
            // be evaluated into an xmm register.
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
    GenTreePtr castOp     = node->CastOp();
    var_types  castToType = node->CastToType();
    var_types  srcType    = castOp->TypeGet();

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
    ContainCheckBinary(cmp);
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
    GenTreePtr other;
    if (!CheckImmedAndMakeContained(node, node->gtIndex))
    {
        CheckImmedAndMakeContained(node, node->gtArrLen);
    }
}

#endif // _TARGET_ARMARCH_

#endif // !LEGACY_BACKEND
