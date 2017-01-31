// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for ARM64                              XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the ARM64         XX
XX  architecture.  For a more detailed view of what is lowering, please      XX
XX  take a look at Lower.cpp                                                 XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM64_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Widening operations of unsigneds.

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
            // TODO-ARM64-CQ: if the field is promoted shouldn't we also be able to do this?
            if (!varDsc->lvIsStructField)
            {
                storeLoc->gtType = TYP_INT;
                con->SetIconValue(ival);
            }
        }
    }
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
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;

#if 0
        // TODO-ARM64-CQ: Currently we generate a helper call for every
        // initblk we encounter.  Later on we should implement loop unrolling
        // code sequences to improve CQ.
        // For reference see the code in LowerXArch.cpp.
        if ((size != 0) && (size <= INITBLK_UNROLL_LIMIT) && initVal->IsCnsIntOrI())
        {
            // The fill value of an initblk is interpreted to hold a
            // value of (unsigned int8) however a constant of any size
            // may practically reside on the evaluation stack. So extract
            // the lower byte out of the initVal constant and replicate
            // it to a larger constant whose size is sufficient to support
            // the largest width store of the desired inline expansion.

            ssize_t fill = initVal->gtIntCon.gtIconVal & 0xFF;
            if (size < REGSIZE_BYTES)
            {
                initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
            }
            else
            {
                initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * fill;
                initVal->gtType = TYP_LONG;
            }
            initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindUnroll;
        }
        else
#endif // 0
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
        else
        {
            // CopyBlk
            short     internalIntCount      = 0;
            regMaskTP internalIntCandidates = RBM_NONE;

#if 0
            // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
            // we should unroll the loop to improve CQ.
            // For reference see the code in lowerxarch.cpp.

            // TODO-ARM64-CQ: cpblk loop unrolling is currently not implemented.

            if ((size != 0) && (size <= INITBLK_UNROLL_LIMIT))
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
            else
#endif // 0
            {
                // In case we have a constant integer this means we went beyond
                // CPBLK_UNROLL_LIMIT bytes of size, still we should never have the case of
                // any GC-Pointers in the src struct.
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
            }
        }
    }
}

/* Lower GT_CAST(srcType, DstType) nodes.
 *
 * Casts from small int type to float/double are transformed as follows:
 * GT_CAST(byte, float/double)     =   GT_CAST(GT_CAST(byte, int32), float/double)
 * GT_CAST(sbyte, float/double)    =   GT_CAST(GT_CAST(sbyte, int32), float/double)
 * GT_CAST(int16, float/double)    =   GT_CAST(GT_CAST(int16, int32), float/double)
 * GT_CAST(uint16, float/double)   =   GT_CAST(GT_CAST(uint16, int32), float/double)
 *
 * SSE2 conversion instructions operate on signed integers. casts from Uint32/Uint64
 * are morphed as follows by front-end and hence should not be seen here.
 * GT_CAST(uint32, float/double)   =   GT_CAST(GT_CAST(uint32, long), float/double)
 * GT_CAST(uint64, float)          =   GT_CAST(GT_CAST(uint64, double), float)
 *
 *
 * Similarly casts from float/double to a smaller int type are transformed as follows:
 * GT_CAST(float/double, byte)     =   GT_CAST(GT_CAST(float/double, int32), byte)
 * GT_CAST(float/double, sbyte)    =   GT_CAST(GT_CAST(float/double, int32), sbyte)
 * GT_CAST(float/double, int16)    =   GT_CAST(GT_CAST(double/double, int32), int16)
 * GT_CAST(float/double, uint16)   =   GT_CAST(GT_CAST(double/double, int32), uint16)
 *
 * SSE2 has instructions to convert a float/double vlaue into a signed 32/64-bit
 * integer.  The above transformations help us to leverage those instructions.
 *
 * Note that for the overflow conversions we still depend on helper calls and
 * don't expect to see them here.
 * i) GT_CAST(float/double, int type with overflow detection)
 *
 */
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    GenTreePtr op1     = tree->gtOp.gtOp1;
    var_types  dstType = tree->CastToType();
    var_types  srcType = op1->TypeGet();
    var_types  tmpType = TYP_UNDEF;

    // We should never see the following casts as they are expected to be lowered
    // apropriately or converted into helper calls by front-end.
    //   srcType = float/double   dstType = * and overflow detecting cast
    //       Reason: must be converted to a helper call
    //
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
}

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
}

// returns true if the tree can use the read-modify-write memory instruction form
bool Lowering::isRMWRegOper(GenTreePtr tree)
{
    return false;
}

bool Lowering::IsCallTargetInRange(void* addr)
{
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
}

// return true if the immediate can be folded into an instruction, for example small enough and non-relocatable
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
                    return true;
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

        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_ADD:
            case GT_SUB:
                if (emitter::emitIns_valid_imm_for_add(immVal, size))
                    return true;
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                if (emitter::emitIns_valid_imm_for_cmp(immVal, size))
                    return true;
                break;

            case GT_AND:
            case GT_OR:
            case GT_XOR:
                if (emitter::emitIns_valid_imm_for_alu(immVal, size))
                    return true;
                break;

            case GT_STORE_LCL_VAR:
                if (immVal == 0)
                    return true;
                break;
        }
    }

    return false;
}

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
