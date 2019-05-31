// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for AMD64, x86                         XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the AMD64         XX
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

#ifdef _TARGET_XARCH_ // This file is only used for xarch

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

// xarch supports both ROL and ROR instructions so no lowering is required.
void Lowering::LowerRotate(GenTree* tree)
{
    ContainCheckShiftRotate(tree->AsOp());
}

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Handling of contained immediates.
//    - Widening operations of unsigneds.

void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    // Try to widen the ops if they are going into a local var.
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (storeLoc->gtOp1->gtOper == GT_CNS_INT))
    {
        GenTreeIntCon* con  = storeLoc->gtOp1->AsIntCon();
        ssize_t        ival = con->gtIconVal;

        unsigned   varNum = storeLoc->gtLclNum;
        LclVarDsc* varDsc = comp->lvaTable + varNum;

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
            // TODO-XArch-CQ: if the field is promoted shouldn't we also be able to do this?
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
    // Mark all GT_STOREIND nodes to indicate that it is not known
    // whether it represents a RMW memory op.
    node->AsStoreInd()->SetRMWStatusDefault();

    if (!varTypeIsFloating(node))
    {
        // Perform recognition of trees with the following structure:
        //        StoreInd(addr, BinOp(expr, GT_IND(addr)))
        // to be able to fold this into an instruction of the form
        //        BINOP [addr], register
        // where register is the actual place where 'expr' is computed.
        //
        // SSE2 doesn't support RMW form of instructions.
        if (LowerRMWMemOp(node))
        {
            return;
        }
    }
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
    GenTree* dstAddr       = blkNode->Addr();
    unsigned size          = blkNode->gtBlkSize;
    GenTree* source        = blkNode->Data();
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
        // If we have an InitBlk with constant block size we can optimize several ways:
        // a) If the size is smaller than a small memory page but larger than INITBLK_UNROLL_LIMIT bytes
        //    we use rep stosb since this reduces the register pressure in LSRA and we have
        //    roughly the same performance as calling the helper.
        // b) If the size is <= INITBLK_UNROLL_LIMIT bytes and the fill byte is a constant,
        //    we can speed this up by unrolling the loop using SSE2 stores.  The reason for
        //    this threshold is because our last investigation (Fall 2013), more than 95% of initblks
        //    in our framework assemblies are actually <= INITBLK_UNROLL_LIMIT bytes size, so this is the
        //    preferred code sequence for the vast majority of cases.

        // This threshold will decide from using the helper or let the JIT decide to inline
        // a code sequence of its choice.
        unsigned helperThreshold = max(INITBLK_STOS_LIMIT, INITBLK_UNROLL_LIMIT);

        // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
        if (size != 0 && size <= helperThreshold)
        {
            // Always favor unrolling vs rep stos.
            if (size <= INITBLK_UNROLL_LIMIT && initVal->IsCnsIntOrI())
            {
                // The fill value of an initblk is interpreted to hold a
                // value of (unsigned int8) however a constant of any size
                // may practically reside on the evaluation stack. So extract
                // the lower byte out of the initVal constant and replicate
                // it to a larger constant whose size is sufficient to support
                // the largest width store of the desired inline expansion.

                ssize_t fill = initVal->gtIntCon.gtIconVal & 0xFF;
#ifdef _TARGET_AMD64_
                if (size < REGSIZE_BYTES)
                {
                    initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
                }
                else
                {
                    initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * fill;
                    initVal->gtType             = TYP_LONG;
                    if ((fill == 0) && ((size & 0xf) == 0))
                    {
                        MakeSrcContained(blkNode, source);
                    }
                }
#else  // !_TARGET_AMD64_
                initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
#endif // !_TARGET_AMD64_

                if ((fill == 0) && ((size & 0xf) == 0))
                {
                    MakeSrcContained(blkNode, source);
                }
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
            }
        }
        else
        {
#ifdef _TARGET_AMD64_
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
#else  // !_TARGET_AMD64_
            blkNode->gtBlkOpKind            = GenTreeBlk::BlkOpKindRepInstr;
#endif // !_TARGET_AMD64_
        }
    }
    else
    {
        if (blkNode->gtOper == GT_STORE_OBJ)
        {
            // CopyObj

            GenTreeObj* cpObjNode = blkNode->AsObj();

            unsigned slots = cpObjNode->gtSlots;

#ifdef DEBUG
            // CpObj must always have at least one GC-Pointer as a member.
            assert(cpObjNode->gtGcPtrCount > 0);

            assert(dstAddr->gtType == TYP_BYREF || dstAddr->gtType == TYP_I_IMPL);

            CORINFO_CLASS_HANDLE clsHnd    = cpObjNode->gtClass;
            size_t               classSize = comp->info.compCompHnd->getClassSize(clsHnd);
            size_t               blkSize   = roundUp(classSize, TARGET_POINTER_SIZE);

            // Currently, the EE always round up a class data structure so
            // we are not handling the case where we have a non multiple of pointer sized
            // struct. This behavior may change in the future so in order to keeps things correct
            // let's assert it just to be safe. Going forward we should simply
            // handle this case.
            assert(classSize == blkSize);
            assert((blkSize / TARGET_POINTER_SIZE) == slots);
            assert(cpObjNode->HasGCPtr());
#endif

            bool IsRepMovsProfitable = false;

            // If the destination is not on the stack, let's find out if we
            // can improve code size by using rep movsq instead of generating
            // sequences of movsq instructions.
            if (!dstAddr->OperIsLocalAddr())
            {
                // Let's inspect the struct/class layout and determine if it's profitable
                // to use rep movsq for copying non-gc memory instead of using single movsq
                // instructions for each memory slot.
                unsigned i      = 0;
                BYTE*    gcPtrs = cpObjNode->gtGcPtrs;

                do
                {
                    unsigned nonGCSlots = 0;
                    // Measure a contiguous non-gc area inside the struct and note the maximum.
                    while (i < slots && gcPtrs[i] == TYPE_GC_NONE)
                    {
                        nonGCSlots++;
                        i++;
                    }

                    while (i < slots && gcPtrs[i] != TYPE_GC_NONE)
                    {
                        i++;
                    }

                    if (nonGCSlots >= CPOBJ_NONGC_SLOTS_LIMIT)
                    {
                        IsRepMovsProfitable = true;
                        break;
                    }
                } while (i < slots);
            }
            else if (slots >= CPOBJ_NONGC_SLOTS_LIMIT)
            {
                IsRepMovsProfitable = true;
            }

            // There are two cases in which we need to materialize the
            // struct size:
            // a) When the destination is on the stack we don't need to use the
            //    write barrier, we can just simply call rep movsq and get a win in codesize.
            // b) If we determine we have contiguous non-gc regions in the struct where it's profitable
            //    to use rep movsq instead of a sequence of single movsq instructions.  According to the
            //    Intel Manual, the sweet spot for small structs is between 4 to 12 slots of size where
            //    the entire operation takes 20 cycles and encodes in 5 bytes (moving RCX, and calling rep movsq).
            if (IsRepMovsProfitable)
            {
                // We need the size of the contiguous Non-GC-region to be in RCX to call rep movsq.
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
            }
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
        }
        else
        {
            assert(blkNode->OperIs(GT_STORE_BLK, GT_STORE_DYN_BLK));
            // CopyBlk
            // In case of a CpBlk with a constant size and less than CPBLK_MOVS_LIMIT size
            // we can use rep movs to generate code instead of the helper call.

            // This threshold will decide between using the helper or let the JIT decide to inline
            // a code sequence of its choice.
            unsigned helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);

            // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
            if ((!blkNode->OperIs(GT_STORE_DYN_BLK)) && (size <= helperThreshold))
            {
                // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
                // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
                // our framework assemblies, so this is the main code generation scheme we'll use.
                if ((size != 0) && (size <= CPBLK_UNROLL_LIMIT))
                {
                    blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

                    // If src or dst are on stack, we don't have to generate the address
                    // into a register because it's just some constant+SP.
                    if ((srcAddrOrFill != nullptr) && srcAddrOrFill->OperIsLocalAddr())
                    {
                        MakeSrcContained(blkNode, srcAddrOrFill);
                    }

                    if (dstAddr->OperIsLocalAddr())
                    {
                        MakeSrcContained(blkNode, dstAddr);
                    }
                }
                else
                {
                    blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
                }
            }
#ifdef _TARGET_AMD64_
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
            }
#elif defined(_TARGET_X86_)
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
            }
#endif // _TARGET_X86_
            assert(blkNode->gtBlkOpKind != GenTreeBlk::BlkOpKindInvalid);
        }

        // CopyObj or CopyBlk
        if (source->gtOper == GT_IND)
        {
            // The GT_IND is contained, but the address must be in a register unless it is local.
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
// LowerPutArgStk: Lower a GT_PUTARG_STK.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::LowerPutArgStk(GenTreePutArgStk* putArgStk)
{
#ifdef _TARGET_X86_
    if (putArgStk->gtOp1->gtOper == GT_FIELD_LIST)
    {
        putArgStk->gtNumberReferenceSlots = 0;
        putArgStk->gtPutArgStkKind        = GenTreePutArgStk::Kind::Invalid;

        GenTreeFieldList* fieldList = putArgStk->gtOp1->AsFieldList();

        // The code generator will push these fields in reverse order by offset. Reorder the list here s.t. the order
        // of uses is visible to LSRA.
        unsigned          fieldCount = 0;
        GenTreeFieldList* head       = nullptr;
        for (GenTreeFieldList *current = fieldList, *next; current != nullptr; current = next)
        {
            next = current->Rest();

            // First, insert the field node into the sorted list.
            GenTreeFieldList* prev = nullptr;
            for (GenTreeFieldList* cursor = head;; cursor = cursor->Rest())
            {
                // If the offset of the current list node is greater than the offset of the cursor or if we have
                // reached the end of the list, insert the current node before the cursor and terminate.
                if ((cursor == nullptr) || (current->gtFieldOffset > cursor->gtFieldOffset))
                {
                    if (prev == nullptr)
                    {
                        assert(cursor == head);
                        head = current;
                    }
                    else
                    {
                        prev->Rest() = current;
                    }

                    current->Rest() = cursor;
                    break;
                }
            }

            fieldCount++;
        }

        // In theory, the upper bound for the size of a field list is 8: these constructs only appear when passing the
        // collection of lclVars that represent the fields of a promoted struct lclVar, and we do not promote struct
        // lclVars with more than 4 fields. If each of these lclVars is of type long, decomposition will split the
        // corresponding field list nodes in two, giving an upper bound of 8.
        //
        // The reason that this is important is that the algorithm we use above to sort the field list is O(N^2): if
        // the maximum size of a field list grows significantly, we will need to reevaluate it.
        assert(fieldCount <= 8);

        // The sort above may have changed which node is at the head of the list. Update the PUTARG_STK node if
        // necessary.
        if (head != fieldList)
        {
            head->gtFlags |= GTF_FIELD_LIST_HEAD;
            head->SetContained();

            fieldList->ClearContained();
            fieldList->gtFlags &= ~GTF_FIELD_LIST_HEAD;

#ifdef DEBUG
            head->gtSeqNum = fieldList->gtSeqNum;
#endif // DEBUG

            BlockRange().InsertAfter(fieldList, head);
            BlockRange().Remove(fieldList);

            fieldList         = head;
            putArgStk->gtOp1  = fieldList;
            putArgStk->gtType = fieldList->gtType;
        }

        // Now that the fields have been sorted, the kind of code we will generate.
        bool     allFieldsAreSlots = true;
        unsigned prevOffset        = putArgStk->getArgSize();
        for (GenTreeFieldList* current = fieldList; current != nullptr; current = current->Rest())
        {
            GenTree* const  fieldNode   = current->Current();
            const var_types fieldType   = fieldNode->TypeGet();
            const unsigned  fieldOffset = current->gtFieldOffset;
            assert(fieldType != TYP_LONG);

            // We can treat as a slot any field that is stored at a slot boundary, where the previous
            // field is not in the same slot. (Note that we store the fields in reverse order.)
            const bool fieldIsSlot = ((fieldOffset % 4) == 0) && ((prevOffset - fieldOffset) >= 4);
            if (!fieldIsSlot)
            {
                allFieldsAreSlots = false;
            }

            if (varTypeIsGC(fieldType))
            {
                putArgStk->gtNumberReferenceSlots++;
            }

            // For x86 we must mark all integral fields as contained or reg-optional, and handle them
            // accordingly in code generation, since we may have up to 8 fields, which cannot all be in
            // registers to be consumed atomically by the call.
            if (varTypeIsIntegralOrI(fieldNode))
            {
                if (fieldNode->OperGet() == GT_LCL_VAR)
                {
                    LclVarDsc* varDsc = &(comp->lvaTable[fieldNode->AsLclVarCommon()->gtLclNum]);
                    if (!varDsc->lvDoNotEnregister)
                    {
                        fieldNode->SetRegOptional();
                    }
                    else
                    {
                        MakeSrcContained(putArgStk, fieldNode);
                    }
                }
                else if (fieldNode->IsIntCnsFitsInI32())
                {
                    MakeSrcContained(putArgStk, fieldNode);
                }
                else
                {
                    // For the case where we cannot directly push the value, if we run out of registers,
                    // it would be better to defer computation until we are pushing the arguments rather
                    // than spilling, but this situation is not all that common, as most cases of promoted
                    // structs do not have a large number of fields, and of those most are lclVars or
                    // copy-propagated constants.
                    fieldNode->SetRegOptional();
                }
            }

            prevOffset = fieldOffset;
        }

        // Set the copy kind.
        // TODO-X86-CQ: Even if we are using push, if there are contiguous floating point fields, we should
        // adjust the stack once for those fields. The latter is really best done in code generation, but
        // this tuning should probably be undertaken as a whole.
        // Also, if there are  floating point fields, it may be better to use the "Unroll" mode
        // of copying the struct as a whole, if the fields are not register candidates.
        if (allFieldsAreSlots)
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::PushAllSlots;
        }
        else
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
        }
        return;
    }
#endif // _TARGET_X86_

    GenTree* src = putArgStk->gtOp1;

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    if (src->TypeGet() != TYP_STRUCT)
#endif // FEATURE_PUT_STRUCT_ARG_STK
    {
        // If the child of GT_PUTARG_STK is a constant, we don't need a register to
        // move it to memory (stack location).
        //
        // On AMD64, we don't want to make 0 contained, because we can generate smaller code
        // by zeroing a register and then storing it. E.g.:
        //      xor rdx, rdx
        //      mov gword ptr [rsp+28H], rdx
        // is 2 bytes smaller than:
        //      mov gword ptr [rsp+28H], 0
        //
        // On x86, we push stack arguments; we don't use 'mov'. So:
        //      push 0
        // is 1 byte smaller than:
        //      xor rdx, rdx
        //      push rdx

        if (IsContainableImmed(putArgStk, src)
#if defined(_TARGET_AMD64_)
            && !src->IsIntegralConst(0)
#endif // _TARGET_AMD64_
                )
        {
            MakeSrcContained(putArgStk, src);
        }
        return;
    }

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    GenTree* srcAddr = nullptr;

    bool haveLocalAddr = false;
    if ((src->OperGet() == GT_OBJ) || (src->OperGet() == GT_IND))
    {
        srcAddr = src->gtOp.gtOp1;
        assert(srcAddr != nullptr);
        haveLocalAddr = srcAddr->OperIsLocalAddr();
    }
    else
    {
        assert(varTypeIsSIMD(putArgStk));
    }

    // In case of a CpBlk we could use a helper call. In case of putarg_stk we
    // can't do that since the helper call could kill some already set up outgoing args.
    // TODO-Amd64-Unix: converge the code for putarg_stk with cpyblk/cpyobj.
    // The cpyXXXX code is rather complex and this could cause it to be more complex, but
    // it might be the right thing to do.

    // This threshold will decide from using the helper or let the JIT decide to inline
    // a code sequence of its choice, but currently we use CPBLK_UNROLL_LIMIT, see #20549.
    ssize_t helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);
    ssize_t size            = putArgStk->gtNumSlots * TARGET_POINTER_SIZE;

    // TODO-X86-CQ: The helper call either is not supported on x86 or required more work
    // (I don't know which).

    // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
    // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
    // our framework assemblies, so this is the main code generation scheme we'll use.
    if (size <= CPBLK_UNROLL_LIMIT && putArgStk->gtNumberReferenceSlots == 0)
    {
#ifdef _TARGET_X86_
        if (size < XMM_REGSIZE_BYTES)
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
        }
        else
#endif // _TARGET_X86_
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Unroll;
        }
    }
#ifdef _TARGET_X86_
    else if (putArgStk->gtNumberReferenceSlots != 0)
    {
        // On x86, we must use `push` to store GC references to the stack in order for the emitter to properly update
        // the function's GC info. These `putargstk` nodes will generate a sequence of `push` instructions.
        putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
    }
#endif // _TARGET_X86_
    else
    {
        putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::RepInstr;
    }
    // Always mark the OBJ and ADDR as contained trees by the putarg_stk. The codegen will deal with this tree.
    MakeSrcContained(putArgStk, src);
    if (haveLocalAddr)
    {
        // If the source address is the address of a lclVar, make the source address contained to avoid unnecessary
        // copies.
        //
        MakeSrcContained(putArgStk, srcAddr);
    }
#endif // FEATURE_PUT_STRUCT_ARG_STK
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
 * Note that for the following conversions we still depend on helper calls and
 * don't expect to see them here.
 *  i) GT_CAST(float/double, uint64)
 * ii) GT_CAST(float/double, int type with overflow detection)
 *
 * TODO-XArch-CQ: (Low-pri): Jit64 generates in-line code of 8 instructions for (i) above.
 * There are hardly any occurrences of this conversion operation in platform
 * assemblies or in CQ perf benchmarks (1 occurrence in mscorlib, microsoft.jscript,
 * 1 occurence in Roslyn and no occurrences in system, system.core, system.numerics
 * system.windows.forms, scimark, fractals, bio mums). If we ever find evidence that
 * doing this optimization is a win, should consider generating in-lined code.
 */
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    GenTree*  castOp     = tree->gtCast.CastOp();
    var_types castToType = tree->CastToType();
    var_types srcType    = castOp->TypeGet();
    var_types tmpType    = TYP_UNDEF;

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    // We should never see the following casts as they are expected to be lowered
    // apropriately or converted into helper calls by front-end.
    //   srcType = float/double                    castToType = * and overflow detecting cast
    //       Reason: must be converted to a helper call
    //   srcType = float/double,                   castToType = ulong
    //       Reason: must be converted to a helper call
    //   srcType = uint                            castToType = float/double
    //       Reason: uint -> float/double = uint -> long -> float/double
    //   srcType = ulong                           castToType = float
    //       Reason: ulong -> float = ulong -> double -> float
    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        noway_assert(castToType != TYP_ULONG);
    }
    else if (srcType == TYP_UINT)
    {
        noway_assert(!varTypeIsFloating(castToType));
    }
    else if (srcType == TYP_ULONG)
    {
        noway_assert(castToType != TYP_FLOAT);
    }

    // Case of src is a small type and dst is a floating point type.
    if (varTypeIsSmall(srcType) && varTypeIsFloating(castToType))
    {
        // These conversions can never be overflow detecting ones.
        noway_assert(!tree->gtOverflow());
        tmpType = TYP_INT;
    }
    // case of src is a floating point type and dst is a small type.
    else if (varTypeIsFloating(srcType) && varTypeIsSmall(castToType))
    {
        tmpType = TYP_INT;
    }

    if (tmpType != TYP_UNDEF)
    {
        GenTree* tmp = comp->gtNewCastNode(tmpType, castOp, tree->IsUnsigned(), tmpType);
        tmp->gtFlags |= (tree->gtFlags & (GTF_OVERFLOW | GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->gtOp.gtOp1 = tmp;
        BlockRange().InsertAfter(castOp, tmp);
        ContainCheckCast(tmp->AsCast());
    }

    // Now determine if we have operands that should be contained.
    ContainCheckCast(tree->AsCast());
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
    if (simdNode->TypeGet() == TYP_SIMD12)
    {
        // GT_SIMD node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        simdNode->gtType = TYP_SIMD16;
    }

    if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicInitN)
    {
        assert(simdNode->gtSIMDBaseType == TYP_FLOAT);

        int   argCount      = 0;
        int   constArgCount = 0;
        float constArgValues[4]{0, 0, 0, 0};

        for (GenTreeArgList* list = simdNode->gtGetOp1()->AsArgList(); list != nullptr; list = list->Rest())
        {
            GenTree* arg = list->Current();

            assert(arg->TypeGet() == simdNode->gtSIMDBaseType);
            assert(argCount < _countof(constArgValues));

            if (arg->IsCnsFltOrDbl())
            {
                constArgValues[constArgCount] = static_cast<float>(arg->AsDblCon()->gtDconVal);
                constArgCount++;
            }

            argCount++;
        }

        if (constArgCount == argCount)
        {
            for (GenTreeArgList* list = simdNode->gtGetOp1()->AsArgList(); list != nullptr; list = list->Rest())
            {
                BlockRange().Remove(list->Current());
            }

            CORINFO_FIELD_HANDLE hnd =
                comp->getEmitter()->emitAnyConst(constArgValues, sizeof(constArgValues), emitDataAlignment::Required);
            GenTree* clsVarAddr = new (comp, GT_CLS_VAR_ADDR) GenTreeClsVar(GT_CLS_VAR_ADDR, TYP_I_IMPL, hnd, nullptr);
            BlockRange().InsertBefore(simdNode, clsVarAddr);
            simdNode->ChangeOper(GT_IND);
            simdNode->gtOp1 = clsVarAddr;
            ContainCheckIndir(simdNode->AsIndir());

            return;
        }
    }

#ifdef _TARGET_XARCH_
    if ((simdNode->gtSIMDIntrinsicID == SIMDIntrinsicGetItem) && (simdNode->gtGetOp1()->OperGet() == GT_IND))
    {
        // If SIMD vector is already in memory, we force its
        // addr to be evaluated into a reg.  This would allow
        // us to generate [regBase] or [regBase+offset] or
        // [regBase+sizeOf(SIMD vector baseType)*regIndex]
        // to access the required SIMD vector element directly
        // from memory.
        //
        // TODO-CQ-XARCH: If addr of GT_IND is GT_LEA, we
        // might be able update GT_LEA to fold the regIndex
        // or offset in some cases.  Instead with this
        // approach we always evaluate GT_LEA into a reg.
        // Ideally, we should be able to lower GetItem intrinsic
        // into GT_IND(newAddr) where newAddr combines
        // the addr of SIMD vector with the given index.
        simdNode->gtOp1->gtFlags |= GTF_IND_REQ_ADDR_IN_REG;
    }
    else if (simdNode->IsSIMDEqualityOrInequality())
    {
        LIR::Use simdUse;

        if (BlockRange().TryGetUse(simdNode, &simdUse))
        {
            //
            // Try to transform JTRUE(EQ|NE(SIMD<OpEquality|OpInEquality>(x, y), 0|1)) into
            // JCC(SIMD<OpEquality|OpInEquality>(x, y)). SIMD<OpEquality|OpInEquality>(x, y)
            // is expected to set the Zero flag appropriately.
            // All the involved nodes must form a continuous range, there's no other way to
            // guarantee that condition flags aren't changed between the SIMD node and the JCC
            // node.
            //

            bool     transformed = false;
            GenTree* simdUser    = simdUse.User();

            if (simdUser->OperIs(GT_EQ, GT_NE) && simdUser->gtGetOp2()->IsCnsIntOrI() &&
                (simdNode->gtNext == simdUser->gtGetOp2()) && (simdUser->gtGetOp2()->gtNext == simdUser))
            {
                ssize_t relopOp2Value = simdUser->gtGetOp2()->AsIntCon()->IconValue();

                if ((relopOp2Value == 0) || (relopOp2Value == 1))
                {
                    GenTree* jtrue = simdUser->gtNext;

                    if ((jtrue != nullptr) && jtrue->OperIs(GT_JTRUE) && (jtrue->gtGetOp1() == simdUser))
                    {
                        if ((simdNode->gtSIMDIntrinsicID == SIMDIntrinsicOpEquality) != simdUser->OperIs(GT_EQ))
                        {
                            relopOp2Value ^= 1;
                        }

                        jtrue->ChangeOper(GT_JCC);
                        GenTreeCC* jcc = jtrue->AsCC();
                        jcc->gtFlags |= GTF_USE_FLAGS;
                        jcc->gtCondition = (relopOp2Value == 0) ? GenCondition::NE : GenCondition::EQ;

                        BlockRange().Remove(simdUser->gtGetOp2());
                        BlockRange().Remove(simdUser);
                        transformed = true;
                    }
                }
            }

            if (!transformed)
            {
                //
                // The code generated for SIMD SIMD<OpEquality|OpInEquality>(x, y) nodes sets
                // the Zero flag like integer compares do so we can simply use SETCC<EQ|NE>
                // to produce the desired result. This avoids the need for subsequent phases
                // to have to handle 2 cases (set flags/set destination register).
                //

                GenCondition condition =
                    (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicOpEquality) ? GenCondition::EQ : GenCondition::NE;
                GenTreeCC* setcc = new (comp, GT_SETCC) GenTreeCC(GT_SETCC, condition, simdNode->TypeGet());
                setcc->gtFlags |= GTF_USE_FLAGS;
                BlockRange().InsertAfter(simdNode, setcc);
                simdUse.ReplaceWith(comp, setcc);
            }
        }

        simdNode->gtFlags |= GTF_SET_FLAGS;
        simdNode->gtType = TYP_VOID;
    }
#endif
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
    ContainCheckHWIntrinsic(node);
}
#endif // FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// Lowering::IsRMWIndirCandidate:
//    Returns true if the given operand is a candidate indirection for a read-modify-write
//    operator.
//
//  Arguments:
//     operand - The operand to consider.
//     storeInd - The indirect store that roots the possible RMW operator.
//
bool Lowering::IsRMWIndirCandidate(GenTree* operand, GenTree* storeInd)
{
    // If the operand isn't an indirection, it's trivially not a candidate.
    if (operand->OperGet() != GT_IND)
    {
        return false;
    }

    // If the indirection's source address isn't equivalent to the destination address of the storeIndir, then the
    // indirection is not a candidate.
    GenTree* srcAddr = operand->gtGetOp1();
    GenTree* dstAddr = storeInd->gtGetOp1();
    if ((srcAddr->OperGet() != dstAddr->OperGet()) || !IndirsAreEquivalent(operand, storeInd))
    {
        return false;
    }

    // If it is not safe to contain the entire tree rooted at the indirection, then the indirection is not a
    // candidate. Crawl the IR from the node immediately preceding the storeIndir until the last node in the
    // indirection's tree is visited and check the side effects at each point.

    m_scratchSideEffects.Clear();

    assert((operand->gtLIRFlags & LIR::Flags::Mark) == 0);
    operand->gtLIRFlags |= LIR::Flags::Mark;

    unsigned markCount = 1;
    GenTree* node;
    for (node = storeInd->gtPrev; markCount > 0; node = node->gtPrev)
    {
        assert(node != nullptr);

        if ((node->gtLIRFlags & LIR::Flags::Mark) == 0)
        {
            m_scratchSideEffects.AddNode(comp, node);
        }
        else
        {
            node->gtLIRFlags &= ~LIR::Flags::Mark;
            markCount--;

            if (m_scratchSideEffects.InterferesWith(comp, node, false))
            {
                // The indirection's tree contains some node that can't be moved to the storeInder. The indirection is
                // not a candidate. Clear any leftover mark bits and return.
                for (; markCount > 0; node = node->gtPrev)
                {
                    if ((node->gtLIRFlags & LIR::Flags::Mark) != 0)
                    {
                        node->gtLIRFlags &= ~LIR::Flags::Mark;
                        markCount--;
                    }
                }
                return false;
            }

            node->VisitOperands([&markCount](GenTree* nodeOperand) -> GenTree::VisitResult {
                assert((nodeOperand->gtLIRFlags & LIR::Flags::Mark) == 0);
                nodeOperand->gtLIRFlags |= LIR::Flags::Mark;
                markCount++;
                return GenTree::VisitResult::Continue;
            });
        }
    }

    // At this point we've verified that the operand is an indirection, its address is equivalent to the storeIndir's
    // destination address, and that it and the transitive closure of its operand can be safely contained by the
    // storeIndir. This indirection is therefore a candidate for an RMW op.
    return true;
}

//----------------------------------------------------------------------------------------------
// Returns true if this tree is bin-op of a GT_STOREIND of the following form
//      storeInd(subTreeA, binOp(gtInd(subTreeA), subtreeB)) or
//      storeInd(subTreeA, binOp(subtreeB, gtInd(subTreeA)) in case of commutative bin-ops
//
// The above form for storeInd represents a read-modify-write memory binary operation.
//
// Parameters
//     tree   -   GentreePtr of binOp
//
// Return Value
//     True if 'tree' is part of a RMW memory operation pattern
//
bool Lowering::IsBinOpInRMWStoreInd(GenTree* tree)
{
    // Must be a non floating-point type binary operator since SSE2 doesn't support RMW memory ops
    assert(!varTypeIsFloating(tree));
    assert(GenTree::OperIsBinary(tree->OperGet()));

    // Cheap bail out check before more expensive checks are performed.
    // RMW memory op pattern requires that one of the operands of binOp to be GT_IND.
    if (tree->gtGetOp1()->OperGet() != GT_IND && tree->gtGetOp2()->OperGet() != GT_IND)
    {
        return false;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(tree, &use) || use.User()->OperGet() != GT_STOREIND || use.User()->gtGetOp2() != tree)
    {
        return false;
    }

    // Since it is not relatively cheap to recognize RMW memory op pattern, we
    // cache the result in GT_STOREIND node so that while lowering GT_STOREIND
    // we can use the result.
    GenTree* indirCandidate = nullptr;
    GenTree* indirOpSource  = nullptr;
    return IsRMWMemOpRootedAtStoreInd(use.User(), &indirCandidate, &indirOpSource);
}

//----------------------------------------------------------------------------------------------
// This method recognizes the case where we have a treeNode with the following structure:
//         storeInd(IndirDst, binOp(gtInd(IndirDst), indirOpSource)) OR
//         storeInd(IndirDst, binOp(indirOpSource, gtInd(IndirDst)) in case of commutative operations OR
//         storeInd(IndirDst, unaryOp(gtInd(IndirDst)) in case of unary operations
//
// Terminology:
//         indirDst = memory write of an addr mode  (i.e. storeind destination)
//         indirSrc = value being written to memory (i.e. storeind source which could either be a binary or unary op)
//         indirCandidate = memory read i.e. a gtInd of an addr mode
//         indirOpSource = source operand used in binary/unary op (i.e. source operand of indirSrc node)
//
// In x86/x64 this storeInd pattern can be effectively encoded in a single instruction of the
// following form in case of integer operations:
//         binOp [addressing mode], RegIndirOpSource
//         binOp [addressing mode], immediateVal
// where RegIndirOpSource is the register where indirOpSource was computed.
//
// Right now, we recognize few cases:
//     a) The gtInd child is a lea/lclVar/lclVarAddr/clsVarAddr/constant
//     b) BinOp is either add, sub, xor, or, and, shl, rsh, rsz.
//     c) unaryOp is either not/neg
//
// Implementation Note: The following routines need to be in sync for RMW memory op optimization
// to be correct and functional.
//     IndirsAreEquivalent()
//     NodesAreEquivalentLeaves()
//     Codegen of GT_STOREIND and genCodeForShiftRMW()
//     emitInsRMW()
//
//  TODO-CQ: Enable support for more complex indirections (if needed) or use the value numbering
//  package to perform more complex tree recognition.
//
//  TODO-XArch-CQ: Add support for RMW of lcl fields (e.g. lclfield binop= source)
//
//  Parameters:
//     tree               -  GT_STOREIND node
//     outIndirCandidate  -  out param set to indirCandidate as described above
//     ouutIndirOpSource  -  out param set to indirOpSource as described above
//
//  Return value
//     True if there is a RMW memory operation rooted at a GT_STOREIND tree
//     and out params indirCandidate and indirOpSource are set to non-null values.
//     Otherwise, returns false with indirCandidate and indirOpSource set to null.
//     Also updates flags of GT_STOREIND tree with its RMW status.
//
bool Lowering::IsRMWMemOpRootedAtStoreInd(GenTree* tree, GenTree** outIndirCandidate, GenTree** outIndirOpSource)
{
    assert(!varTypeIsFloating(tree));
    assert(outIndirCandidate != nullptr);
    assert(outIndirOpSource != nullptr);

    *outIndirCandidate = nullptr;
    *outIndirOpSource  = nullptr;

    // Early out if storeInd is already known to be a non-RMW memory op
    GenTreeStoreInd* storeInd = tree->AsStoreInd();
    if (storeInd->IsNonRMWMemoryOp())
    {
        return false;
    }

    GenTree*   indirDst = storeInd->gtGetOp1();
    GenTree*   indirSrc = storeInd->gtGetOp2();
    genTreeOps oper     = indirSrc->OperGet();

    // Early out if it is already known to be a RMW memory op
    if (storeInd->IsRMWMemoryOp())
    {
        if (GenTree::OperIsBinary(oper))
        {
            if (storeInd->IsRMWDstOp1())
            {
                *outIndirCandidate = indirSrc->gtGetOp1();
                *outIndirOpSource  = indirSrc->gtGetOp2();
            }
            else
            {
                assert(storeInd->IsRMWDstOp2());
                *outIndirCandidate = indirSrc->gtGetOp2();
                *outIndirOpSource  = indirSrc->gtGetOp1();
            }
            assert(IndirsAreEquivalent(*outIndirCandidate, storeInd));
        }
        else
        {
            assert(GenTree::OperIsUnary(oper));
            assert(IndirsAreEquivalent(indirSrc->gtGetOp1(), storeInd));
            *outIndirCandidate = indirSrc->gtGetOp1();
            *outIndirOpSource  = indirSrc->gtGetOp1();
        }

        return true;
    }

    // If reached here means that we do not know RMW status of tree rooted at storeInd
    assert(storeInd->IsRMWStatusUnknown());

    // Early out if indirDst is not one of the supported memory operands.
    if (indirDst->OperGet() != GT_LEA && indirDst->OperGet() != GT_LCL_VAR && indirDst->OperGet() != GT_LCL_VAR_ADDR &&
        indirDst->OperGet() != GT_CLS_VAR_ADDR && indirDst->OperGet() != GT_CNS_INT)
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
        return false;
    }

    // We can not use Read-Modify-Write instruction forms with overflow checking instructions
    // because we are not allowed to modify the target until after the overflow check.
    if (indirSrc->gtOverflowEx())
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
        return false;
    }

    // At this point we can match one of two patterns:
    //
    //     t_ind = indir t_addr_0
    //       ...
    //     t_value = binop t_ind, t_other
    //       ...
    //     storeIndir t_addr_1, t_value
    //
    // or
    //
    //     t_ind = indir t_addr_0
    //       ...
    //     t_value = unop t_ind
    //       ...
    //     storeIndir t_addr_1, t_value
    //
    // In all cases, we will eventually make the binop that produces t_value and the entire dataflow tree rooted at
    // t_ind contained by t_value.

    GenTree*  indirCandidate = nullptr;
    GenTree*  indirOpSource  = nullptr;
    RMWStatus status         = STOREIND_RMW_STATUS_UNKNOWN;
    if (GenTree::OperIsBinary(oper))
    {
        // Return if binary op is not one of the supported operations for RMW of memory.
        if (!GenTree::OperIsRMWMemOp(oper))
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
            return false;
        }

        if (GenTree::OperIsShiftOrRotate(oper) && varTypeIsSmall(storeInd))
        {
            // In ldind, Integer values smaller than 4 bytes, a boolean, or a character converted to 4 bytes
            // by sign or zero-extension as appropriate. If we directly shift the short type data using sar, we
            // will lose the sign or zero-extension bits.
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_TYPE);
            return false;
        }

        // In the common case, the second operand to the binop will be the indir candidate.
        GenTreeOp* binOp = indirSrc->AsOp();
        if (GenTree::OperIsCommutative(oper) && IsRMWIndirCandidate(binOp->gtOp2, storeInd))
        {
            indirCandidate = binOp->gtOp2;
            indirOpSource  = binOp->gtOp1;
            status         = STOREIND_RMW_DST_IS_OP2;
        }
        else if (IsRMWIndirCandidate(binOp->gtOp1, storeInd))
        {
            indirCandidate = binOp->gtOp1;
            indirOpSource  = binOp->gtOp2;
            status         = STOREIND_RMW_DST_IS_OP1;
        }
        else
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
            return false;
        }
    }
    else if (GenTree::OperIsUnary(oper))
    {
        // Nodes other than GT_NOT and GT_NEG are not yet supported.
        if (oper != GT_NOT && oper != GT_NEG)
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
            return false;
        }

        if (indirSrc->gtGetOp1()->OperGet() != GT_IND)
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
            return false;
        }

        GenTreeUnOp* unOp = indirSrc->AsUnOp();
        if (IsRMWIndirCandidate(unOp->gtOp1, storeInd))
        {
            // src and dest are the same in case of unary ops
            indirCandidate = unOp->gtOp1;
            indirOpSource  = unOp->gtOp1;
            status         = STOREIND_RMW_DST_IS_OP1;
        }
        else
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
            return false;
        }
    }
    else
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
        return false;
    }

    // By this point we've verified that we have a supported operand with a supported address. Now we need to ensure
    // that we're able to move the destination address for the source indirection forwards.
    if (!IsSafeToContainMem(storeInd, indirDst))
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
        return false;
    }

    assert(indirCandidate != nullptr);
    assert(indirOpSource != nullptr);
    assert(status != STOREIND_RMW_STATUS_UNKNOWN);

    *outIndirCandidate = indirCandidate;
    *outIndirOpSource  = indirOpSource;
    storeInd->SetRMWStatus(status);
    return true;
}

// anything is in range for AMD64
bool Lowering::IsCallTargetInRange(void* addr)
{
    return true;
}

// return true if the immediate can be folded into an instruction, for example small enough and non-relocatable
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (!childNode->IsIntCnsFitsInI32())
    {
        return false;
    }

    // At this point we know that it is an int const fits within 4-bytes and hence can safely cast to IntConCommon.
    // Icons that need relocation should never be marked as contained immed
    if (childNode->AsIntConCommon()->ImmedValNeedsReloc(comp))
    {
        return false;
    }

    return true;
}

//-----------------------------------------------------------------------
// PreferredRegOptionalOperand: returns one of the operands of given
// binary oper that is to be preferred for marking as reg optional.
//
// Since only one of op1 or op2 can be a memory operand on xarch, only
// one of  them have to be marked as reg optional.  Since Lower doesn't
// know apriori which of op1 or op2 is not likely to get a register, it
// has to make a guess. This routine encapsulates heuristics that
// guess whether it is likely to be beneficial to mark op1 or op2 as
// reg optional.
//
//
// Arguments:
//     tree  -  a binary-op tree node that is either commutative
//              or a compare oper.
//
// Returns:
//     Returns op1 or op2 of tree node that is preferred for
//     marking as reg optional.
//
// Note: if the tree oper is neither commutative nor a compare oper
// then only op2 can be reg optional on xarch and hence no need to
// call this routine.
GenTree* Lowering::PreferredRegOptionalOperand(GenTree* tree)
{
    assert(GenTree::OperIsBinary(tree->OperGet()));
    assert(tree->OperIsCommutative() || tree->OperIsCompare() || tree->OperIs(GT_CMP));

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();
    assert(!op1->IsRegOptional() && !op2->IsRegOptional());

    // We default to op1, as op2 is likely to have the shorter lifetime.
    GenTree* preferredOp = op1;

    // This routine uses the following heuristics:
    //
    // a) If both are register candidates, marking the one with lower weighted
    // ref count as reg-optional would likely be beneficial as it has
    // higher probability of not getting a register. Note that we use !lvDoNotEnregister
    // here because this is being done while we are adding lclVars for Lowering.
    //
    // b) op1 = tracked local and op2 = untracked local: LSRA creates two
    // ref positions for op2: a def and use position. op2's def position
    // requires a reg and it is allocated a reg by spilling another
    // interval (if required) and that could be even op1.  For this reason
    // it is beneficial to mark op1 as reg optional.
    //
    // TODO: It is not always mandatory for a def position of an untracked
    // local to be allocated a register if it is on rhs of an assignment
    // and its use position is reg-optional and has not been assigned a
    // register.  Reg optional def positions is currently not yet supported.
    //
    // c) op1 = untracked local and op2 = tracked local: marking op1 as
    // reg optional is beneficial, since its use position is less likely
    // to get a register.
    //
    // d) If both are untracked locals (i.e. treated like tree temps by
    // LSRA): though either of them could be marked as reg optional,
    // marking op1 as reg optional is likely to be beneficial because
    // while allocating op2's def position, there is a possibility of
    // spilling op1's def and in which case op1 is treated as contained
    // memory operand rather than requiring to reload.
    //
    // e) If only one of them is a local var, prefer to mark it as
    // reg-optional.  This is heuristic is based on the results
    // obtained against CQ perf benchmarks.
    //
    // f) If neither of them are local vars (i.e. tree temps), prefer to
    // mark op1 as reg optional for the same reason as mentioned in (d) above.
    if (op1->OperGet() == GT_LCL_VAR && op2->OperGet() == GT_LCL_VAR)
    {
        LclVarDsc* v1 = comp->lvaTable + op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* v2 = comp->lvaTable + op2->AsLclVarCommon()->GetLclNum();

        bool v1IsRegCandidate = !v1->lvDoNotEnregister;
        bool v2IsRegCandidate = !v2->lvDoNotEnregister;
        if (v1IsRegCandidate && v2IsRegCandidate)
        {
            // Both are enregisterable locals.  The one with lower weight is less likely
            // to get a register and hence beneficial to mark the one with lower
            // weight as reg optional.
            // If either is not tracked, it may be that it was introduced after liveness
            // was run, in which case we will always prefer op1 (should we use raw refcnt??).
            if (v1->lvTracked && v2->lvTracked && (v1->lvRefCntWtd() >= v2->lvRefCntWtd()))
            {
                preferredOp = op2;
            }
        }
    }
    else if (!(op1->OperGet() == GT_LCL_VAR) && (op2->OperGet() == GT_LCL_VAR))
    {
        preferredOp = op2;
    }

    return preferredOp;
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
    if (call->gtCallType == CT_INDIRECT)
    {
        // either gtControlExpr != null or gtCallAddr != null.
        // Both cannot be non-null at the same time.
        assert(ctrlExpr == nullptr);
        assert(call->gtCallAddr != nullptr);
        ctrlExpr = call->gtCallAddr;

#ifdef _TARGET_X86_
        // Fast tail calls aren't currently supported on x86, but if they ever are, the code
        // below that handles indirect VSD calls will need to be fixed.
        assert(!call->IsFastTailCall() || !call->IsVirtualStub());
#endif // _TARGET_X86_
    }

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        // we should never see a gtControlExpr whose type is void.
        assert(ctrlExpr->TypeGet() != TYP_VOID);

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (!call->IsFastTailCall())
        {
#ifdef _TARGET_X86_
            // On x86, we need to generate a very specific pattern for indirect VSD calls:
            //
            //    3-byte nop
            //    call dword ptr [eax]
            //
            // Where EAX is also used as an argument to the stub dispatch helper. Make
            // sure that the call target address is computed into EAX in this case.
            if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT))
            {
                assert(ctrlExpr->isIndir());
                MakeSrcContained(call, ctrlExpr);
            }
            else
#endif // _TARGET_X86_
                if (ctrlExpr->isIndir())
            {
                // We may have cases where we have set a register target on the ctrlExpr, but if it
                // contained we must clear it.
                ctrlExpr->gtRegNum = REG_NA;
                MakeSrcContained(call, ctrlExpr);
            }
        }
    }

    GenTree* args = call->gtCallArgs;
    while (args)
    {
        GenTree* arg = args->gtOp.gtOp1;
        if (arg->gtOper == GT_PUTARG_STK)
        {
            LowerPutArgStk(arg->AsPutArgStk());
        }
        args = args->gtOp.gtOp2;
    }
    args = call->gtCallLateArgs;
    while (args)
    {
        GenTree* arg = args->gtOp.gtOp1;
        if (arg->gtOper == GT_PUTARG_STK)
        {
            LowerPutArgStk(arg->AsPutArgStk());
        }
        args = args->gtOp.gtOp2;
    }
}

//------------------------------------------------------------------------
// ContainCheckIndir: Determine whether operands of an indir should be contained.
//
// Arguments:
//    node       - The indirection node of interest
//
// Notes:
//    This is called for both store and load indirections. In the former case, it is assumed that
//    LowerStoreIndir() has already been called to check for RMW opportunities.
//
// Return Value:
//    None.
//
void Lowering::ContainCheckIndir(GenTreeIndir* node)
{
    GenTree* addr = node->Addr();

    // If this is the rhs of a block copy it will be handled when we handle the store.
    if (node->TypeGet() == TYP_STRUCT)
    {
        return;
    }

#ifdef FEATURE_SIMD
    // If indirTree is of TYP_SIMD12, don't mark addr as contained
    // so that it always get computed to a register.  This would
    // mean codegen side logic doesn't need to handle all possible
    // addr expressions that could be contained.
    //
    // TODO-XArch-CQ: handle other addr mode expressions that could be marked
    // as contained.
    if (node->TypeGet() == TYP_SIMD12)
    {
        return;
    }
#endif // FEATURE_SIMD

    if ((node->gtFlags & GTF_IND_REQ_ADDR_IN_REG) != 0)
    {
        // The address of an indirection that requires its address in a reg.
        // Skip any further processing that might otherwise make it contained.
    }
    else if ((addr->OperGet() == GT_CLS_VAR_ADDR) || (addr->OperGet() == GT_LCL_VAR_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_CLS_VAR_ADDR turns into a constant.
        // - GT_LCL_VAR_ADDR is a stack addr mode.

        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(node, addr);
    }
    else if (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp))
    {
        // Amd64:
        // We can mark any pc-relative 32-bit addr as containable, except for a direct VSD call address.
        // (i.e. those VSD calls for which stub addr is known during JIT compilation time).  In this case,
        // VM requires us to pass stub addr in VirtualStubParam.reg - see LowerVirtualStubCall().  For
        // that reason we cannot mark such an addr as contained.  Note that this is not an issue for
        // indirect VSD calls since morphArgs() is explicitly materializing hidden param as a non-standard
        // argument.
        //
        // Workaround:
        // Note that LowerVirtualStubCall() sets addr->gtRegNum to VirtualStubParam.reg and Lowering::doPhase()
        // sets destination candidates on such nodes and resets addr->gtRegNum to REG_NA.
        // Ideally we should set a flag on addr nodes that shouldn't be marked as contained
        // (in LowerVirtualStubCall()), but we don't have any GTF_* flags left for that purpose.  As a workaround
        // an explicit check is made here.
        //
        // On x86, direct VSD is done via a relative branch, and in fact it MUST be contained.
        MakeSrcContained(node, addr);
    }
    else if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(node, addr))
    {
        MakeSrcContained(node, addr);
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
    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    GenTree* src = node->gtOp.gtOp2;
    if (IsContainableImmed(node, src) &&
        (!src->IsIntegralConst(0) || varTypeIsSmall(node) || node->gtGetOp1()->OperGet() == GT_CLS_VAR_ADDR))
    {
        MakeSrcContained(node, src);
    }
    ContainCheckIndir(node);
}

//------------------------------------------------------------------------
// ContainCheckMul: determine whether the sources of a MUL node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckMul(GenTreeOp* node)
{
#if defined(_TARGET_X86_)
    assert(node->OperIs(GT_MUL, GT_MULHI, GT_MUL_LONG));
#else
    assert(node->OperIs(GT_MUL, GT_MULHI));
#endif

    // Case of float/double mul.
    if (varTypeIsFloating(node->TypeGet()))
    {
        ContainCheckFloatBinary(node);
        return;
    }

    GenTree* op1 = node->gtOp.gtOp1;
    GenTree* op2 = node->gtOp.gtOp2;

    bool isSafeToContainOp1 = true;
    bool isSafeToContainOp2 = true;

    bool     isUnsignedMultiply    = ((node->gtFlags & GTF_UNSIGNED) != 0);
    bool     requiresOverflowCheck = node->gtOverflowEx();
    bool     useLeaEncoding        = false;
    GenTree* memOp                 = nullptr;

    bool                 hasImpliedFirstOperand = false;
    GenTreeIntConCommon* imm                    = nullptr;
    GenTree*             other                  = nullptr;

    // Multiply should never be using small types
    assert(!varTypeIsSmall(node->TypeGet()));

    // We do use the widening multiply to implement
    // the overflow checking for unsigned multiply
    //
    if (isUnsignedMultiply && requiresOverflowCheck)
    {
        hasImpliedFirstOperand = true;
    }
    else if (node->OperGet() == GT_MULHI)
    {
        hasImpliedFirstOperand = true;
    }
#if defined(_TARGET_X86_)
    else if (node->OperGet() == GT_MUL_LONG)
    {
        hasImpliedFirstOperand = true;
    }
#endif
    else if (IsContainableImmed(node, op2) || IsContainableImmed(node, op1))
    {
        if (IsContainableImmed(node, op2))
        {
            imm   = op2->AsIntConCommon();
            other = op1;
        }
        else
        {
            imm   = op1->AsIntConCommon();
            other = op2;
        }

        // CQ: We want to rewrite this into a LEA
        ssize_t immVal = imm->AsIntConCommon()->IconValue();
        if (!requiresOverflowCheck && (immVal == 3 || immVal == 5 || immVal == 9))
        {
            useLeaEncoding = true;
        }

        MakeSrcContained(node, imm); // The imm is always contained
        if (IsContainableMemoryOp(other))
        {
            memOp = other; // memOp may be contained below
        }
    }

    // We allow one operand to be a contained memory operand.
    // The memory op type must match with the 'node' type.
    // This is because during codegen we use 'node' type to derive EmitTypeSize.
    // E.g op1 type = byte, op2 type = byte but GT_MUL node type is int.
    //
    if (memOp == nullptr)
    {
        if ((op2->TypeGet() == node->TypeGet()) && IsContainableMemoryOp(op2))
        {
            isSafeToContainOp2 = IsSafeToContainMem(node, op2);
            if (isSafeToContainOp2)
            {
                memOp = op2;
            }
        }

        if ((memOp == nullptr) && (op1->TypeGet() == node->TypeGet()) && IsContainableMemoryOp(op1))
        {
            isSafeToContainOp1 = IsSafeToContainMem(node, op1);
            if (isSafeToContainOp1)
            {
                memOp = op1;
            }
        }
    }
    else
    {
        if ((memOp->TypeGet() != node->TypeGet()))
        {
            memOp = nullptr;
        }
        else if (!IsSafeToContainMem(node, memOp))
        {
            if (memOp == op1)
            {
                isSafeToContainOp1 = false;
            }
            else
            {
                isSafeToContainOp2 = false;
            }
            memOp = nullptr;
        }
    }
    // To generate an LEA we need to force memOp into a register
    // so don't allow memOp to be 'contained'
    //
    if (!useLeaEncoding)
    {
        if (memOp != nullptr)
        {
            MakeSrcContained(node, memOp);
        }
        else
        {
            // IsSafeToContainMem is expensive so we call it at most once for each operand
            // in this method. If we already called IsSafeToContainMem, it must have returned false;
            // otherwise, memOp would be set to the corresponding operand (op1 or op2).
            if (imm != nullptr)
            {
                // Has a contained immediate operand.
                // Only 'other' operand can be marked as reg optional.
                assert(other != nullptr);

                isSafeToContainOp1 = ((other == op1) && isSafeToContainOp1 && IsSafeToContainMem(node, op1));
                isSafeToContainOp2 = ((other == op2) && isSafeToContainOp2 && IsSafeToContainMem(node, op2));
            }
            else if (hasImpliedFirstOperand)
            {
                // Only op2 can be marked as reg optional.
                isSafeToContainOp1 = false;
                isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToContainMem(node, op2);
            }
            else
            {
                // If there are no containable operands, we can make either of op1 or op2
                // as reg optional.
                isSafeToContainOp1 = isSafeToContainOp1 && IsSafeToContainMem(node, op1);
                isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToContainMem(node, op2);
            }
            SetRegOptionalForBinOp(node, isSafeToContainOp1, isSafeToContainOp2);
        }
    }
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
    assert(node->OperIs(GT_DIV, GT_MOD, GT_UDIV, GT_UMOD));

    if (varTypeIsFloating(node->TypeGet()))
    {
        ContainCheckFloatBinary(node);
        return;
    }

    GenTree* divisor = node->gtGetOp2();

    bool divisorCanBeRegOptional = true;
#ifdef _TARGET_X86_
    GenTree* dividend = node->gtGetOp1();
    if (dividend->OperGet() == GT_LONG)
    {
        divisorCanBeRegOptional = false;
        MakeSrcContained(node, dividend);
    }
#endif

    // divisor can be an r/m, but the memory indirection must be of the same size as the divide
    if (IsContainableMemoryOp(divisor) && (divisor->TypeGet() == node->TypeGet()))
    {
        MakeSrcContained(node, divisor);
    }
    else if (divisorCanBeRegOptional)
    {
        // If there are no containable operands, we can make an operand reg optional.
        // Div instruction allows only divisor to be a memory op.
        divisor->SetRegOptional();
    }
}

//------------------------------------------------------------------------
// ContainCheckShiftRotate: determine whether the sources of a shift/rotate node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
    assert(node->OperIsShiftOrRotate());
#ifdef _TARGET_X86_
    GenTree* source = node->gtOp1;
    if (node->OperIsShiftLong())
    {
        assert(source->OperGet() == GT_LONG);
        MakeSrcContained(node, source);
    }
#endif // !_TARGET_X86_

    GenTree* shiftBy = node->gtOp2;
    if (IsContainableImmed(node, shiftBy) && (shiftBy->gtIntConCommon.IconValue() <= 255) &&
        (shiftBy->gtIntConCommon.IconValue() >= 0))
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
#ifdef _TARGET_X86_
    else if (op1->OperGet() == GT_LONG)
    {
        MakeSrcContained(storeLoc, op1);
    }
#endif // _TARGET_X86_
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
    var_types srcType    = castOp->TypeGet();

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (node->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    if (!node->gtOverflow() && (varTypeIsFloating(castToType) || varTypeIsFloating(srcType)))
    {
#ifdef DEBUG
        // If converting to float/double, the operand must be 4 or 8 byte in size.
        if (varTypeIsFloating(castToType))
        {
            unsigned opSize = genTypeSize(srcType);
            assert(opSize == 4 || opSize == 8);
        }
#endif // DEBUG

        // U8 -> R8 conversion requires that the operand be in a register.
        if (srcType != TYP_ULONG)
        {
            if (IsContainableMemoryOp(castOp) || castOp->IsCnsNonZeroFltOrDbl())
            {
                MakeSrcContained(node, castOp);
            }
            else
            {
                // Mark castOp as reg optional to indicate codegen
                // can still generate code if it is on stack.
                castOp->SetRegOptional();
            }
        }
    }
#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(srcType))
    {
        noway_assert(castOp->OperGet() == GT_LONG);
        castOp->SetContained();
    }
#endif // !defined(_TARGET_64BIT_)
}

//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    assert(cmp->OperIsCompare() || cmp->OperIs(GT_CMP));

    GenTree*  op1     = cmp->gtOp.gtOp1;
    GenTree*  op2     = cmp->gtOp.gtOp2;
    var_types op1Type = op1->TypeGet();
    var_types op2Type = op2->TypeGet();

    // If either of op1 or op2 is floating point values, then we need to use
    // ucomiss or ucomisd to compare, both of which support the following form:
    //     ucomis[s|d] xmm, xmm/mem
    // That is only the second operand can be a memory op.
    //
    // Second operand is a memory Op:  Note that depending on comparison operator,
    // the operands of ucomis[s|d] need to be reversed.  Therefore, either op1 or
    // op2 can be a memory op depending on the comparison operator.
    if (varTypeIsFloating(op1Type))
    {
        // The type of the operands has to be the same and no implicit conversions at this stage.
        assert(op1Type == op2Type);

        GenTree* otherOp;
        if (GenCondition::FromFloatRelop(cmp).PreferSwap())
        {
            otherOp = op1;
        }
        else
        {
            otherOp = op2;
        }

        assert(otherOp != nullptr);
        bool isSafeToContainOtherOp = true;
        if (otherOp->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(cmp, otherOp);
        }
        else if (IsContainableMemoryOp(otherOp))
        {
            isSafeToContainOtherOp = IsSafeToContainMem(cmp, otherOp);
            if (isSafeToContainOtherOp)
            {
                MakeSrcContained(cmp, otherOp);
            }
        }

        if (!otherOp->isContained() && isSafeToContainOtherOp && IsSafeToContainMem(cmp, otherOp))
        {
            // SSE2 allows only otherOp to be a memory-op. Since otherOp is not
            // contained, we can mark it reg-optional.
            // IsSafeToContainMem is expensive so we call it at most once for otherOp.
            // If we already called IsSafeToContainMem, it must have returned false;
            // otherwise, otherOp would be contained.
            otherOp->SetRegOptional();
        }

        return;
    }

    // TODO-XArch-CQ: factor out cmp optimization in 'genCondSetFlags' to be used here
    // or in other backend.

    if (CheckImmedAndMakeContained(cmp, op2))
    {
        // If the types are the same, or if the constant is of the correct size,
        // we can treat the MemoryOp as contained.
        if (op1Type == op2Type)
        {
            if (IsContainableMemoryOp(op1))
            {
                MakeSrcContained(cmp, op1);
            }
            else
            {
                op1->SetRegOptional();
            }
        }
    }
    else if (op1Type == op2Type)
    {
        // Note that TEST does not have a r,rm encoding like CMP has but we can still
        // contain the second operand because the emitter maps both r,rm and rm,r to
        // the same instruction code. This avoids the need to special case TEST here.

        bool isSafeToContainOp1 = true;
        bool isSafeToContainOp2 = true;

        if (IsContainableMemoryOp(op2))
        {
            isSafeToContainOp2 = IsSafeToContainMem(cmp, op2);
            if (isSafeToContainOp2)
            {
                MakeSrcContained(cmp, op2);
            }
        }

        if (!op2->isContained() && IsContainableMemoryOp(op1))
        {
            isSafeToContainOp1 = IsSafeToContainMem(cmp, op1);
            if (isSafeToContainOp1)
            {
                MakeSrcContained(cmp, op1);
            }
        }

        if (!op1->isContained() && !op2->isContained())
        {
            // One of op1 or op2 could be marked as reg optional
            // to indicate that codegen can still generate code
            // if one of them is on stack.
            GenTree* regOptionalCandidate = op1->IsCnsIntOrI() ? op2 : PreferredRegOptionalOperand(cmp);

            // IsSafeToContainMem is expensive so we call it at most once for each operand
            // in this method. If we already called IsSafeToContainMem, it must have returned false;
            // otherwise, the corresponding operand (op1 or op2) would be contained.
            bool setRegOptional = (regOptionalCandidate == op1) ? isSafeToContainOp1 && IsSafeToContainMem(cmp, op1)
                                                                : isSafeToContainOp2 && IsSafeToContainMem(cmp, op2);
            if (setRegOptional)
            {
                regOptionalCandidate->SetRegOptional();
            }
        }
    }
}

//------------------------------------------------------------------------
// LowerRMWMemOp: Determine if this is a valid RMW mem op, and if so lower it accordingly
//
// Arguments:
//    node       - The indirect store node (GT_STORE_IND) of interest
//
// Return Value:
//    Returns true if 'node' is a valid RMW mem op; false otherwise.
//
bool Lowering::LowerRMWMemOp(GenTreeIndir* storeInd)
{
    assert(storeInd->OperGet() == GT_STOREIND);

    // SSE2 doesn't support RMW on float values
    assert(!varTypeIsFloating(storeInd));

    // Terminology:
    // indirDst = memory write of an addr mode  (i.e. storeind destination)
    // indirSrc = value being written to memory (i.e. storeind source which could a binary/unary op)
    // indirCandidate = memory read i.e. a gtInd of an addr mode
    // indirOpSource = source operand used in binary/unary op (i.e. source operand of indirSrc node)

    GenTree* indirCandidate = nullptr;
    GenTree* indirOpSource  = nullptr;

    if (!IsRMWMemOpRootedAtStoreInd(storeInd, &indirCandidate, &indirOpSource))
    {
        JITDUMP("Lower of StoreInd didn't mark the node as self contained for reason: %d\n",
                storeInd->AsStoreInd()->GetRMWStatus());
        DISPTREERANGE(BlockRange(), storeInd);
        return false;
    }

    GenTree*   indirDst = storeInd->gtGetOp1();
    GenTree*   indirSrc = storeInd->gtGetOp2();
    genTreeOps oper     = indirSrc->OperGet();

    // At this point we have successfully detected a RMW memory op of one of the following forms
    //         storeInd(indirDst, indirSrc(indirCandidate, indirOpSource)) OR
    //         storeInd(indirDst, indirSrc(indirOpSource, indirCandidate) in case of commutative operations OR
    //         storeInd(indirDst, indirSrc(indirCandidate) in case of unary operations
    //
    // Here indirSrc = one of the supported binary or unary operation for RMW of memory
    //      indirCandidate = a GT_IND node
    //      indirCandidateChild = operand of GT_IND indirCandidate
    //
    // The logic below does the following
    //      Make indirOpSource contained.
    //      Make indirSrc contained.
    //      Make indirCandidate contained.
    //      Make indirCandidateChild contained.
    //      Make indirDst contained except when it is a GT_LCL_VAR or GT_CNS_INT that doesn't fit within addr
    //      base.
    //

    // We have already done containment analysis on the indirSrc op.
    // If any of its operands are marked regOptional, reset that now.
    indirSrc->AsOp()->gtOp1->ClearRegOptional();
    if (GenTree::OperIsBinary(oper))
    {
        // On Xarch RMW operations require the source to be an immediate or in a register.
        // Therefore, if we have previously marked the indirOpSource as contained while lowering
        // the binary node, we need to reset that now.
        if (IsContainableMemoryOp(indirOpSource))
        {
            indirOpSource->ClearContained();
        }
        indirSrc->AsOp()->gtOp2->ClearRegOptional();
        JITDUMP("Lower succesfully detected an assignment of the form: *addrMode BinOp= source\n");
    }
    else
    {
        assert(GenTree::OperIsUnary(oper));
        JITDUMP("Lower succesfully detected an assignment of the form: *addrMode = UnaryOp(*addrMode)\n");
    }
    DISPTREERANGE(BlockRange(), storeInd);

    indirSrc->SetContained();
    indirCandidate->SetContained();

    GenTree* indirCandidateChild = indirCandidate->gtGetOp1();
    indirCandidateChild->SetContained();

    if (indirCandidateChild->OperGet() == GT_LEA)
    {
        GenTreeAddrMode* addrMode = indirCandidateChild->AsAddrMode();

        if (addrMode->HasBase())
        {
            assert(addrMode->Base()->OperIsLeaf());
            addrMode->Base()->SetContained();
        }

        if (addrMode->HasIndex())
        {
            assert(addrMode->Index()->OperIsLeaf());
            addrMode->Index()->SetContained();
        }

        indirDst->SetContained();
    }
    else
    {
        assert(indirCandidateChild->OperGet() == GT_LCL_VAR || indirCandidateChild->OperGet() == GT_LCL_VAR_ADDR ||
               indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR || indirCandidateChild->OperGet() == GT_CNS_INT);

        // If it is a GT_LCL_VAR, it still needs the reg to hold the address.
        // We would still need a reg for GT_CNS_INT if it doesn't fit within addressing mode base.
        // For GT_CLS_VAR_ADDR, we don't need a reg to hold the address, because field address value is known at jit
        // time. Also, we don't need a reg for GT_CLS_VAR_ADDR.
        if (indirCandidateChild->OperGet() == GT_LCL_VAR_ADDR || indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR)
        {
            indirDst->SetContained();
        }
        else if (indirCandidateChild->IsCnsIntOrI() && indirCandidateChild->AsIntConCommon()->FitsInAddrBase(comp))
        {
            indirDst->SetContained();
        }
    }
    return true;
}

//------------------------------------------------------------------------
// ContainCheckBinary: Determine whether a binary op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckBinary(GenTreeOp* node)
{
    assert(node->OperIsBinary());

    if (varTypeIsFloating(node))
    {
        assert(node->OperIs(GT_ADD, GT_SUB));
        ContainCheckFloatBinary(node);
        return;
    }

    GenTree* op1 = node->gtOp1;
    GenTree* op2 = node->gtOp2;

    // We can directly encode the second operand if it is either a containable constant or a memory-op.
    // In case of memory-op, we can encode it directly provided its type matches with 'tree' type.
    // This is because during codegen, type of 'tree' is used to determine emit Type size. If the types
    // do not match, they get normalized (i.e. sign/zero extended) on load into a register.
    bool     directlyEncodable  = false;
    bool     binOpInRMW         = false;
    GenTree* operand            = nullptr;
    bool     isSafeToContainOp1 = true;
    bool     isSafeToContainOp2 = true;

    if (IsContainableImmed(node, op2))
    {
        directlyEncodable = true;
        operand           = op2;
    }
    else
    {
        binOpInRMW = IsBinOpInRMWStoreInd(node);
        if (!binOpInRMW)
        {
            const unsigned operatorSize = genTypeSize(node->TypeGet());
            if ((genTypeSize(op2->TypeGet()) == operatorSize) && IsContainableMemoryOp(op2))
            {
                isSafeToContainOp2 = IsSafeToContainMem(node, op2);
                if (isSafeToContainOp2)
                {
                    directlyEncodable = true;
                    operand           = op2;
                }
            }

            if ((operand == nullptr) && node->OperIsCommutative())
            {
                // If it is safe, we can reverse the order of operands of commutative operations for efficient
                // codegen
                if (IsContainableImmed(node, op1))
                {
                    directlyEncodable = true;
                    operand           = op1;
                }
                else if ((genTypeSize(op1->TypeGet()) == operatorSize) && IsContainableMemoryOp(op1))
                {
                    isSafeToContainOp1 = IsSafeToContainMem(node, op1);
                    if (isSafeToContainOp1)
                    {
                        directlyEncodable = true;
                        operand           = op1;
                    }
                }
            }
        }
    }

    if (directlyEncodable)
    {
        assert(operand != nullptr);
        MakeSrcContained(node, operand);
    }
    else if (!binOpInRMW)
    {
        // If this binary op neither has contained operands, nor is a
        // Read-Modify-Write (RMW) operation, we can mark its operands
        // as reg optional.

        // IsSafeToContainMem is expensive so we call it at most once for each operand
        // in this method. If we already called IsSafeToContainMem, it must have returned false;
        // otherwise, directlyEncodable would be true.
        isSafeToContainOp1 = isSafeToContainOp1 && IsSafeToContainMem(node, op1);
        isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToContainMem(node, op2);

        SetRegOptionalForBinOp(node, isSafeToContainOp1, isSafeToContainOp2);
    }
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
    GenTree* other;
    if (CheckImmedAndMakeContained(node, node->gtIndex))
    {
        other = node->gtArrLen;
    }
    else if (CheckImmedAndMakeContained(node, node->gtArrLen))
    {
        other = node->gtIndex;
    }
    else if (IsContainableMemoryOp(node->gtIndex))
    {
        other = node->gtIndex;
    }
    else
    {
        other = node->gtArrLen;
    }

    if (node->gtIndex->TypeGet() == node->gtArrLen->TypeGet())
    {
        if (IsContainableMemoryOp(other))
        {
            MakeSrcContained(node, other);
        }
        else
        {
            // We can mark 'other' as reg optional, since it is not contained.
            other->SetRegOptional();
        }
    }
}

//------------------------------------------------------------------------
// ContainCheckIntrinsic: determine whether the source of an INTRINSIC node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckIntrinsic(GenTreeOp* node)
{
    assert(node->OperIs(GT_INTRINSIC));

    CorInfoIntrinsics intrinsicId = node->gtIntrinsic.gtIntrinsicId;

    if (intrinsicId == CORINFO_INTRINSIC_Sqrt || intrinsicId == CORINFO_INTRINSIC_Round ||
        intrinsicId == CORINFO_INTRINSIC_Ceiling || intrinsicId == CORINFO_INTRINSIC_Floor)
    {
        GenTree* op1 = node->gtGetOp1();
        if (IsContainableMemoryOp(op1) || op1->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(node, op1);
        }
        else
        {
            // Mark the operand as reg optional since codegen can still
            // generate code if op1 is on stack.
            op1->SetRegOptional();
        }
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
        {
            op1 = simdNode->gtOp.gtOp1;
#ifndef _TARGET_64BIT_
            if (op1->OperGet() == GT_LONG)
            {
                MakeSrcContained(simdNode, op1);
                GenTree* op1lo = op1->gtGetOp1();
                GenTree* op1hi = op1->gtGetOp2();

                if ((op1lo->IsIntegralConst(0) && op1hi->IsIntegralConst(0)) ||
                    (op1lo->IsIntegralConst(-1) && op1hi->IsIntegralConst(-1)))
                {
                    MakeSrcContained(op1, op1lo);
                    MakeSrcContained(op1, op1hi);
                }
            }
            else
#endif // !_TARGET_64BIT_
                if (op1->IsFPZero() || op1->IsIntegralConst(0) ||
                    (varTypeIsIntegral(simdNode->gtSIMDBaseType) && op1->IsIntegralConst(-1)))
            {
                MakeSrcContained(simdNode, op1);
            }
            else if ((comp->getSIMDSupportLevel() == SIMD_AVX2_Supported) &&
                     ((simdNode->gtSIMDSize == 16) || (simdNode->gtSIMDSize == 32)))
            {
                // Either op1 is a float or dbl constant or an addr
                if (op1->IsCnsFltOrDbl() || op1->OperIsLocalAddr())
                {
                    MakeSrcContained(simdNode, op1);
                }
            }
        }
        break;

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            CheckImmedAndMakeContained(simdNode, simdNode->gtGetOp2());
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            // On SSE4/AVX, we can generate optimal code for (in)equality
            // against zero using ptest. We can safely do this optimization
            // for integral vectors but not for floating-point for the reason
            // that we have +0.0 and -0.0 and +0.0 == -0.0
            op2 = simdNode->gtGetOp2();
            if ((comp->getSIMDSupportLevel() >= SIMD_SSE4_Supported) && op2->IsIntegralConstVector(0))
            {
                MakeSrcContained(simdNode, op2);
            }
            break;

        case SIMDIntrinsicGetItem:
        {
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            op1 = simdNode->gtOp.gtOp1;
            op2 = simdNode->gtOp.gtOp2;

            if (op1->OperGet() == GT_IND)
            {
                assert((op1->gtFlags & GTF_IND_REQ_ADDR_IN_REG) != 0);
                op1->AsIndir()->Addr()->ClearContained();
            }
            // If the index is a constant, mark it as contained.
            CheckImmedAndMakeContained(simdNode, op2);

            if (IsContainableMemoryOp(op1))
            {
                MakeSrcContained(simdNode, op1);
                if (op1->OperGet() == GT_IND)
                {
                    op1->AsIndir()->Addr()->ClearContained();
                }
            }
        }
        break;

        case SIMDIntrinsicShuffleSSE2:
            // Second operand is an integer constant and marked as contained.
            assert(simdNode->gtOp.gtOp2->IsCnsIntOrI());
            MakeSrcContained(simdNode, simdNode->gtOp.gtOp2);
            break;

        default:
            break;
    }
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
//----------------------------------------------------------------------------------------------
// IsContainableHWIntrinsicOp: Return true if 'node' is a containable HWIntrinsic op.
//
//  Arguments:
//     containingNode - The hardware intrinsic node which contains 'node'
//     node - The node to check
//     [Out] supportsRegOptional - On return, this will be true if 'containingNode' supports regOptional operands;
//     otherwise, false.
//
// Return Value:
//    true if 'node' is a containable hardware intrinsic node; otherwise, false.
//
bool Lowering::IsContainableHWIntrinsicOp(GenTreeHWIntrinsic* containingNode, GenTree* node, bool* supportsRegOptional)
{
    NamedIntrinsic      containingIntrinsicId = containingNode->gtHWIntrinsicId;
    HWIntrinsicCategory category              = HWIntrinsicInfo::lookupCategory(containingIntrinsicId);

    // We shouldn't have called in here if containingNode doesn't support containment
    assert(HWIntrinsicInfo::SupportsContainment(containingIntrinsicId));

    // containingNode supports nodes that read from an aligned memory address
    //
    // This will generally be an explicit LoadAligned instruction and is generally
    // false for machines with VEX support. This is because there is currently no way
    // to guarantee that the address read from will always be aligned and we could silently
    // change the behavior of the program in the case where an Access Violation would have
    // otherwise occurred.
    bool supportsAlignedSIMDLoads = false;

    // containingNode supports nodes that read from general memory
    //
    // We currently have to assume all "general" loads are unaligned. As such, this is
    // generally used to determine if we can mark the node as `regOptional` in the case
    // where `node` is not containable. However, this can also be used to determine whether
    // we can mark other types of reads as contained (such as when directly reading a local).
    bool supportsGeneralLoads = false;

    // containingNode supports nodes that read from a scalar memory address
    //
    // This will generally be an explicit LoadScalar instruction but is also used to determine
    // whether we can read an address of type T (we don't support this when the load would
    // read more than sizeof(T) bytes).
    bool supportsSIMDScalarLoads = false;

    // containingNode supports nodes that read from an unaligned memory address
    //
    // This will generally be an explicit Load instruction and is generally false for machines
    // without VEX support. This is because older hardware required that the SIMD operand always
    // be aligned to the 'natural alignment' of the type.
    bool supportsUnalignedSIMDLoads = false;

    switch (category)
    {
        case HW_Category_MemoryLoad:
            supportsGeneralLoads = (!node->OperIsHWIntrinsic());
            break;

        case HW_Category_SimpleSIMD:
        {
            switch (containingIntrinsicId)
            {
                case NI_SSE41_ConvertToVector128Int16:
                case NI_SSE41_ConvertToVector128Int32:
                case NI_SSE41_ConvertToVector128Int64:
                case NI_AVX2_ConvertToVector256Int16:
                case NI_AVX2_ConvertToVector256Int32:
                case NI_AVX2_ConvertToVector256Int64:
                {
                    supportsGeneralLoads = (!node->OperIsHWIntrinsic());
                    break;
                }

                default:
                {
                    // These intrinsics only expect 16 or 32-byte nodes for containment
                    assert((genTypeSize(node->TypeGet()) == 16) || (genTypeSize(node->TypeGet()) == 32));
                    supportsAlignedSIMDLoads =
                        !comp->canUseVexEncoding() && (containingIntrinsicId != NI_SSE2_ConvertToVector128Double);
                    supportsUnalignedSIMDLoads = !supportsAlignedSIMDLoads;
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads;
                    break;
                }
            }

            assert(supportsSIMDScalarLoads == false);
            break;
        }

        case HW_Category_IMM:
        {
            switch (containingIntrinsicId)
            {
                case NI_SSE_Shuffle:
                case NI_SSE2_CompareLessThan:
                case NI_SSE2_ShiftLeftLogical:
                case NI_SSE2_ShiftRightArithmetic:
                case NI_SSE2_ShiftRightLogical:
                case NI_SSE2_Shuffle:
                case NI_SSE2_ShuffleHigh:
                case NI_SSE2_ShuffleLow:
                case NI_SSSE3_AlignRight:
                case NI_SSE41_Blend:
                case NI_SSE41_DotProduct:
                case NI_SSE41_MultipleSumAbsoluteDifferences:
                case NI_AES_KeygenAssist:
                case NI_PCLMULQDQ_CarrylessMultiply:
                case NI_AVX_Blend:
                case NI_AVX_Compare:
                case NI_AVX_DotProduct:
                case NI_AVX_InsertVector128:
                case NI_AVX_Permute:
                case NI_AVX_Permute2x128:
                case NI_AVX2_Blend:
                case NI_AVX2_InsertVector128:
                case NI_AVX2_MultipleSumAbsoluteDifferences:
                case NI_AVX2_Permute2x128:
                case NI_AVX2_Permute4x64:
                case NI_AVX2_ShiftLeftLogical:
                case NI_AVX2_ShiftRightArithmetic:
                case NI_AVX2_ShiftRightLogical:
                case NI_AVX2_ShuffleHigh:
                case NI_AVX2_ShuffleLow:
                {
                    // These intrinsics only expect 16 or 32-byte nodes for containment
                    assert((genTypeSize(node->TypeGet()) == 16) || (genTypeSize(node->TypeGet()) == 32));
                    assert(supportsSIMDScalarLoads == false);

                    supportsAlignedSIMDLoads   = !comp->canUseVexEncoding();
                    supportsUnalignedSIMDLoads = !supportsAlignedSIMDLoads;
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads;

                    break;
                }

                case NI_SSE2_Insert:
                case NI_SSE41_Insert:
                case NI_SSE41_X64_Insert:
                {
                    if (containingNode->gtSIMDBaseType == TYP_FLOAT)
                    {
                        assert(containingIntrinsicId == NI_SSE41_Insert);
                        assert(genTypeSize(node->TypeGet()) == 16);

                        // Sse41.Insert(V128<float>, V128<float>, byte) is a bit special
                        // in that it has different behavior depending on whether the
                        // second operand is coming from a register or memory. When coming
                        // from a register, all 4 elements of the vector can be used and it
                        // is effectively a regular `SimpleSIMD` operation; but when loading
                        // from memory, it only works with the lowest element and is effectively
                        // a `SIMDScalar`.

                        assert(supportsAlignedSIMDLoads == false);
                        assert(supportsUnalignedSIMDLoads == false);
                        assert(supportsGeneralLoads == false);
                        assert(supportsSIMDScalarLoads == false);

                        GenTree* op1 = containingNode->gtGetOp1();
                        GenTree* op2 = nullptr;
                        GenTree* op3 = nullptr;

                        assert(op1->OperIsList());
                        assert(containingNode->gtGetOp2() == nullptr);

                        GenTreeArgList* argList = op1->AsArgList();

                        op1     = argList->Current();
                        argList = argList->Rest();

                        op2     = argList->Current();
                        argList = argList->Rest();

                        assert(node == op2);

                        op3 = argList->Current();

                        // The upper two bits of the immediate value are ignored if
                        // op2 comes from memory. In order to support using the upper
                        // bits, we need to disable containment support if op3 is not
                        // constant or if the constant is greater than 0x3F (which means
                        // at least one of the upper two bits is set).

                        if (op3->IsCnsIntOrI())
                        {
                            ssize_t ival = op3->AsIntCon()->IconValue();
                            assert((ival >= 0) && (ival <= 255));

                            supportsSIMDScalarLoads = (ival <= 0x3F);
                            supportsGeneralLoads    = supportsSIMDScalarLoads;
                        }
                        break;
                    }

                    // We should only get here for integral nodes.
                    assert(varTypeIsIntegral(node->TypeGet()));

                    assert(supportsAlignedSIMDLoads == false);
                    assert(supportsUnalignedSIMDLoads == false);
                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(containingNode->gtSIMDBaseType);
                    const unsigned operandSize  = genTypeSize(node->TypeGet());

                    supportsGeneralLoads = (operandSize >= expectedSize);
                    break;
                }

                case NI_AVX_CompareScalar:
                {
                    // These intrinsics only expect 16 or 32-byte nodes for containment
                    assert((genTypeSize(node->TypeGet()) == 16) || (genTypeSize(node->TypeGet()) == 32));

                    assert(supportsAlignedSIMDLoads == false);
                    assert(supportsUnalignedSIMDLoads == false);

                    supportsSIMDScalarLoads = true;
                    supportsGeneralLoads    = supportsSIMDScalarLoads;
                    break;
                }

                default:
                {
                    assert(supportsAlignedSIMDLoads == false);
                    assert(supportsGeneralLoads == false);
                    assert(supportsSIMDScalarLoads == false);
                    assert(supportsUnalignedSIMDLoads == false);
                    break;
                }
            }
            break;
        }

        case HW_Category_SIMDScalar:
        {
            assert(supportsAlignedSIMDLoads == false);
            assert(supportsUnalignedSIMDLoads == false);

            switch (containingIntrinsicId)
            {
                case NI_Vector128_CreateScalarUnsafe:
                case NI_Vector256_CreateScalarUnsafe:
                {
                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(genActualType(containingNode->gtSIMDBaseType));
                    const unsigned operandSize  = genTypeSize(node->TypeGet());

                    supportsGeneralLoads = (operandSize == expectedSize);
                    break;
                }

                case NI_AVX2_BroadcastScalarToVector128:
                case NI_AVX2_BroadcastScalarToVector256:
                {
                    // The memory form of this already takes a pointer, and cannot be further contained.
                    // The containable form is the one that takes a SIMD value, that may be in memory.
                    supportsGeneralLoads = (node->TypeGet() == TYP_SIMD16);
                    break;
                }

                case NI_SSE_ConvertScalarToVector128Single:
                case NI_SSE2_ConvertScalarToVector128Double:
                case NI_SSE2_ConvertScalarToVector128Int32:
                case NI_SSE2_ConvertScalarToVector128UInt32:
                case NI_SSE_X64_ConvertScalarToVector128Single:
                case NI_SSE2_X64_ConvertScalarToVector128Double:
                case NI_SSE2_X64_ConvertScalarToVector128Int64:
                case NI_SSE2_X64_ConvertScalarToVector128UInt64:
                {
                    if (!varTypeIsIntegral(node->TypeGet()))
                    {
                        // The floating-point overload doesn't require any special semantics
                        assert(containingIntrinsicId == NI_SSE2_ConvertScalarToVector128Double);
                        supportsSIMDScalarLoads = true;
                        supportsGeneralLoads    = supportsSIMDScalarLoads;
                        break;
                    }

                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(genActualType(containingNode->gtSIMDBaseType));
                    const unsigned operandSize  = genTypeSize(node->TypeGet());

                    supportsGeneralLoads = (operandSize == expectedSize);
                    break;
                }

                default:
                {
                    // These intrinsics only expect 16 or 32-byte nodes for containment
                    assert((genTypeSize(node->TypeGet()) == 16) || (genTypeSize(node->TypeGet()) == 32));

                    supportsSIMDScalarLoads = true;
                    supportsGeneralLoads    = supportsSIMDScalarLoads;
                    break;
                }
            }
            break;
        }

        case HW_Category_Scalar:
        {
            // We should only get here for integral nodes.
            assert(varTypeIsIntegral(node->TypeGet()));

            assert(supportsAlignedSIMDLoads == false);
            assert(supportsUnalignedSIMDLoads == false);
            assert(supportsSIMDScalarLoads == false);

            unsigned       expectedSize = genTypeSize(containingNode->TypeGet());
            const unsigned operandSize  = genTypeSize(node->TypeGet());

            // CRC32 codegen depends on its second oprand's type.
            // Currently, we are using SIMDBaseType to store the op2Type info.
            if (containingIntrinsicId == NI_SSE42_Crc32)
            {
                var_types op2Type = containingNode->gtSIMDBaseType;
                expectedSize      = genTypeSize(op2Type);
            }

            supportsGeneralLoads = (operandSize >= expectedSize);
            break;
        }

        default:
        {
            assert(supportsAlignedSIMDLoads == false);
            assert(supportsGeneralLoads == false);
            assert(supportsSIMDScalarLoads == false);
            assert(supportsUnalignedSIMDLoads == false);
            break;
        }
    }

    noway_assert(supportsRegOptional != nullptr);
    *supportsRegOptional = supportsGeneralLoads;

    if (!node->OperIsHWIntrinsic())
    {
        return supportsGeneralLoads && IsContainableMemoryOp(node);
    }

    // TODO-XArch: Update this to be table driven, if possible.

    NamedIntrinsic intrinsicId = node->AsHWIntrinsic()->gtHWIntrinsicId;

    switch (intrinsicId)
    {
        case NI_SSE_LoadAlignedVector128:
        case NI_SSE2_LoadAlignedVector128:
        case NI_AVX_LoadAlignedVector256:
        {
            return supportsAlignedSIMDLoads;
        }

        case NI_SSE_LoadScalarVector128:
        case NI_SSE2_LoadScalarVector128:
        {
            return supportsSIMDScalarLoads;
        }

        // VEX encoding supports unaligned memory ops, so we can fold them
        case NI_SSE_LoadVector128:
        case NI_SSE2_LoadVector128:
        case NI_AVX_LoadVector256:
        {
            return supportsUnalignedSIMDLoads;
        }

        default:
        {
            assert(!node->isContainableHWIntrinsic());
            return false;
        }
    }
}

//----------------------------------------------------------------------------------------------
// ContainCheckHWIntrinsicAddr: Perform containment analysis for an address operand of a hardware
//                              intrinsic node.
//
//  Arguments:
//     node  - The hardware intrinsic node
//     pAddr - The "parent" pointer to the address operand, so that we can update the operand
//             of the parent as needed.
//
void Lowering::ContainCheckHWIntrinsicAddr(GenTreeHWIntrinsic* node, GenTree** pAddr)
{
    assert(((*pAddr)->TypeGet() == TYP_I_IMPL) || ((*pAddr)->TypeGet() == TYP_BYREF));
    TryCreateAddrMode(LIR::Use(BlockRange(), pAddr, node), true);
    GenTree* addr = *pAddr;
    if ((addr->OperIs(GT_CLS_VAR_ADDR, GT_LCL_VAR_ADDR) ||
         (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp)) || (addr->OperGet() == GT_LEA)) &&
        IsSafeToContainMem(node, addr))
    {
        MakeSrcContained(node, addr);
    }
}

//----------------------------------------------------------------------------------------------
// ContainCheckHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic      intrinsicId = node->gtHWIntrinsicId;
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                 numArgs     = HWIntrinsicInfo::lookupNumArgs(node);
    var_types           baseType    = node->gtSIMDBaseType;

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();
    GenTree* op3 = nullptr;

    if (!HWIntrinsicInfo::SupportsContainment(intrinsicId))
    {
        // AVX2 gather are not containable and always have constant IMM argument
        if (HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsicId))
        {
            GenTree* lastOp = HWIntrinsicInfo::lookupLastOp(node);
            assert(lastOp != nullptr);
            MakeSrcContained(node, lastOp);
        }
        // Exit early if containment isn't supported
        return;
    }

    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    const bool isCommutative = HWIntrinsicInfo::IsCommutative(intrinsicId);

    if (numArgs == 1)
    {
        // One argument intrinsics cannot be commutative
        assert(!isCommutative);

        assert(!op1->OperIsList());
        assert(op2 == nullptr);

        switch (category)
        {
            case HW_Category_MemoryLoad:
            {
                GenTree** pAddr = &node->gtOp1;
                ContainCheckHWIntrinsicAddr(node, pAddr);
                break;
            }
            case HW_Category_SimpleSIMD:
            case HW_Category_SIMDScalar:
            case HW_Category_Scalar:
            {
                switch (intrinsicId)
                {
                    case NI_SSE_ReciprocalScalar:
                    case NI_SSE_ReciprocalSqrtScalar:
                    case NI_SSE_SqrtScalar:
                    case NI_SSE2_SqrtScalar:
                    case NI_SSE41_CeilingScalar:
                    case NI_SSE41_FloorScalar:
                    case NI_SSE41_RoundCurrentDirectionScalar:
                    case NI_SSE41_RoundToNearestIntegerScalar:
                    case NI_SSE41_RoundToNegativeInfinityScalar:
                    case NI_SSE41_RoundToPositiveInfinityScalar:
                    case NI_SSE41_RoundToZeroScalar:
                    {
                        // These intrinsics have both 1 and 2-operand overloads.
                        //
                        // The 1-operand overload basically does `intrinsic(op1, op1)`
                        //
                        // Because of this, the operand must be loaded into a register
                        // and cannot be contained.
                        return;
                    }

                    case NI_SSE2_ConvertToInt32:
                    case NI_SSE2_X64_ConvertToInt64:
                    case NI_SSE2_ConvertToUInt32:
                    case NI_SSE2_X64_ConvertToUInt64:
                    case NI_AVX2_ConvertToInt32:
                    case NI_AVX2_ConvertToUInt32:
                    {
                        if (varTypeIsIntegral(baseType))
                        {
                            // These intrinsics are "ins reg/mem, xmm" and don't
                            // currently support containment.
                            return;
                        }

                        break;
                    }

                    case NI_SSE41_ConvertToVector128Int16:
                    case NI_SSE41_ConvertToVector128Int32:
                    case NI_SSE41_ConvertToVector128Int64:
                    case NI_AVX2_ConvertToVector256Int16:
                    case NI_AVX2_ConvertToVector256Int32:
                    case NI_AVX2_ConvertToVector256Int64:
                    {
                        if (!varTypeIsSIMD(op1->gtType))
                        {
                            GenTree** pAddr = &node->gtOp1;
                            ContainCheckHWIntrinsicAddr(node, pAddr);
                            return;
                        }
                        break;
                    }

                    default:
                    {
                        break;
                    }
                }

                bool supportsRegOptional = false;

                if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                {
                    MakeSrcContained(node, op1);
                }
                else if (supportsRegOptional)
                {
                    op1->SetRegOptional();
                }
                break;
            }

            default:
            {
                unreached();
                break;
            }
        }
    }
    else
    {
        if (numArgs == 2)
        {
            assert(!op1->OperIsList());
            assert(op2 != nullptr);
            assert(!op2->OperIsList());

            switch (category)
            {
                case HW_Category_MemoryLoad:
                {
                    GenTree** pAddr = nullptr;
                    if ((intrinsicId == NI_AVX_MaskLoad) || (intrinsicId == NI_AVX2_MaskLoad))
                    {
                        pAddr = &node->gtOp.gtOp1;
                    }
                    else
                    {
                        pAddr = &node->gtOp.gtOp2;
                    }
                    ContainCheckHWIntrinsicAddr(node, pAddr);
                    break;
                }
                case HW_Category_MemoryStore:
                {
                    GenTree** pAddr = &node->gtOp1;
                    ContainCheckHWIntrinsicAddr(node, pAddr);
                    break;
                }
                case HW_Category_SimpleSIMD:
                case HW_Category_SIMDScalar:
                case HW_Category_Scalar:
                {
                    if (HWIntrinsicInfo::GeneratesMultipleIns(intrinsicId))
                    {
                        switch (intrinsicId)
                        {
                            case NI_SSE_CompareScalarOrderedLessThan:
                            case NI_SSE_CompareScalarUnorderedLessThan:
                            case NI_SSE_CompareScalarOrderedLessThanOrEqual:
                            case NI_SSE_CompareScalarUnorderedLessThanOrEqual:
                            case NI_SSE2_CompareScalarOrderedLessThan:
                            case NI_SSE2_CompareScalarUnorderedLessThan:
                            case NI_SSE2_CompareScalarOrderedLessThanOrEqual:
                            case NI_SSE2_CompareScalarUnorderedLessThanOrEqual:
                            {
                                // We need to swap the operands for CompareLessThanOrEqual
                                node->gtOp1 = op2;
                                node->gtOp2 = op1;
                                op2         = op1;
                                break;
                            }

                            default:
                            {
                                // TODO-XArch-CQ: The CompareScalarOrdered* and CompareScalarUnordered* methods
                                //                are commutative if you also inverse the intrinsic.
                                break;
                            }
                        }
                    }

                    bool supportsRegOptional = false;

                    if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                    {
                        MakeSrcContained(node, op2);
                    }
                    else if ((isCommutative || (intrinsicId == NI_BMI2_MultiplyNoFlags) ||
                              (intrinsicId == NI_BMI2_X64_MultiplyNoFlags)) &&
                             IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                    {
                        MakeSrcContained(node, op1);

                        // Swap the operands here to make the containment checks in codegen significantly simpler
                        node->gtOp1 = op2;
                        node->gtOp2 = op1;
                    }
                    else if (supportsRegOptional)
                    {
                        op2->SetRegOptional();

                        // TODO-XArch-CQ: For commutative nodes, either operand can be reg-optional.
                        //                https://github.com/dotnet/coreclr/issues/6361
                    }
                    break;
                }

                case HW_Category_IMM:
                {
                    // We don't currently have any IMM intrinsics which are also commutative
                    assert(!isCommutative);
                    bool supportsRegOptional = false;

                    switch (intrinsicId)
                    {
                        case NI_SSE2_ShiftLeftLogical:
                        case NI_SSE2_ShiftRightArithmetic:
                        case NI_SSE2_ShiftRightLogical:
                        case NI_AVX2_ShiftLeftLogical:
                        case NI_AVX2_ShiftRightArithmetic:
                        case NI_AVX2_ShiftRightLogical:
                        {
                            // These intrinsics can have op2 be imm or reg/mem

                            if (!HWIntrinsicInfo::isImmOp(intrinsicId, op2))
                            {
                                if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else if (supportsRegOptional)
                                {
                                    op2->SetRegOptional();
                                }
                            }
                            break;
                        }

                        case NI_SSE2_Shuffle:
                        case NI_SSE2_ShuffleHigh:
                        case NI_SSE2_ShuffleLow:
                        case NI_AVX2_Permute4x64:
                        {
                            // These intrinsics have op2 as an imm and op1 as a reg/mem

                            if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op1);
                            }
                            else if (supportsRegOptional)
                            {
                                op1->SetRegOptional();
                            }
                            break;
                        }

                        case NI_AVX_Permute:
                        {
                            // These intrinsics can have op2 be imm or reg/mem
                            // They also can have op1 be reg/mem and op2 be imm

                            if (HWIntrinsicInfo::isImmOp(intrinsicId, op2))
                            {
                                if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op1);
                                }
                                else if (supportsRegOptional)
                                {
                                    op1->SetRegOptional();
                                }
                            }
                            else if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op2);
                            }
                            else if (supportsRegOptional)
                            {
                                op2->SetRegOptional();
                            }
                            break;
                        }

                        case NI_AES_KeygenAssist:
                        {
                            if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op1);
                            }
                            else if (supportsRegOptional)
                            {
                                op1->SetRegOptional();
                            }
                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    break;
                }

                case HW_Category_Special:
                {
                    if (intrinsicId == NI_SSE2_CompareLessThan)
                    {
                        bool supportsRegOptional = false;

                        if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                        {
                            MakeSrcContained(node, op2);
                        }
                        else if (supportsRegOptional)
                        {
                            op2->SetRegOptional();
                        }
                    }
                    else
                    {
                        unreached();
                    }
                    break;
                }

                default:
                {
                    unreached();
                    break;
                }
            }
        }
        else if (numArgs == 3)
        {
            // three argument intrinsics should not be marked commutative
            assert(!isCommutative);

            assert(op1->OperIsList());
            assert(op2 == nullptr);

            GenTreeArgList* argList         = op1->AsArgList();
            GenTreeArgList* originalArgList = argList;

            op1     = argList->Current();
            argList = argList->Rest();

            op2     = argList->Current();
            argList = argList->Rest();

            op3 = argList->Current();
            assert(argList->Rest() == nullptr);

            switch (category)
            {
                case HW_Category_MemoryStore:
                {
                    GenTree** pAddr = &node->gtOp.gtOp1->gtOp.gtOp1;
                    ContainCheckHWIntrinsicAddr(node, pAddr);
                    break;
                }
                case HW_Category_SimpleSIMD:
                case HW_Category_SIMDScalar:
                case HW_Category_Scalar:
                {
                    if ((intrinsicId >= NI_FMA_MultiplyAdd) && (intrinsicId <= NI_FMA_MultiplySubtractNegatedScalar))
                    {
                        bool supportsRegOptional = false;

                        if (IsContainableHWIntrinsicOp(node, op3, &supportsRegOptional))
                        {
                            // 213 form: op1 = (op2 * op1) + [op3]
                            MakeSrcContained(node, op3);
                        }
                        else if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                        {
                            // 132 form: op1 = (op1 * op3) + [op2]
                            MakeSrcContained(node, op2);
                        }
                        else if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                        {
                            // Intrinsics with CopyUpperBits semantics cannot have op1 be contained

                            if (!HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                            {
                                // 231 form: op3 = (op2 * op3) + [op1]
                                MakeSrcContained(node, op1);
                            }
                        }
                        else
                        {
                            assert(supportsRegOptional);

                            // TODO-XArch-CQ: Technically any one of the three operands can
                            //                be reg-optional. With a limitation on op1 where
                            //                it can only be so if CopyUpperBits is off.
                            //                https://github.com/dotnet/coreclr/issues/6361

                            // 213 form: op1 = (op2 * op1) + op3
                            op3->SetRegOptional();
                        }
                    }
                    else
                    {
                        bool supportsRegOptional = false;

                        switch (intrinsicId)
                        {
                            case NI_SSE41_BlendVariable:
                            case NI_AVX_BlendVariable:
                            case NI_AVX2_BlendVariable:
                            {
                                if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else if (supportsRegOptional)
                                {
                                    op2->SetRegOptional();
                                }
                                break;
                            }

                            case NI_BMI2_MultiplyNoFlags:
                            case NI_BMI2_X64_MultiplyNoFlags:
                            {
                                if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op1);
                                    // MultiplyNoFlags is a Commutative operation, so swap the first two operands here
                                    // to make the containment checks in codegen significantly simpler
                                    *(originalArgList->pCurrent())         = op2;
                                    *(originalArgList->Rest()->pCurrent()) = op1;
                                }
                                else if (supportsRegOptional)
                                {
                                    op2->SetRegOptional();
                                }
                                break;
                            }

                            default:
                            {
                                unreached();
                                break;
                            }
                        }
                    }
                }

                case HW_Category_IMM:
                {
                    bool supportsRegOptional = false;

                    switch (intrinsicId)
                    {
                        case NI_SSE_Shuffle:
                        case NI_SSE2_Insert:
                        case NI_SSE2_Shuffle:
                        case NI_SSSE3_AlignRight:
                        case NI_SSE41_Blend:
                        case NI_SSE41_DotProduct:
                        case NI_SSE41_Insert:
                        case NI_SSE41_X64_Insert:
                        case NI_SSE41_MultipleSumAbsoluteDifferences:
                        case NI_AVX_Blend:
                        case NI_AVX_Compare:
                        case NI_AVX_CompareScalar:
                        case NI_AVX_DotProduct:
                        case NI_AVX_Permute2x128:
                        case NI_AVX_Shuffle:
                        case NI_AVX2_Blend:
                        case NI_AVX2_MultipleSumAbsoluteDifferences:
                        case NI_AVX2_Permute2x128:
                        case NI_PCLMULQDQ_CarrylessMultiply:
                        {
                            if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op2);
                            }
                            else if (supportsRegOptional)
                            {
                                op2->SetRegOptional();
                            }
                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    break;
                }

                default:
                {
                    unreached();
                    break;
                }
            }
        }
        else
        {
            unreached();
        }

        if (HWIntrinsicInfo::lookupCategory(intrinsicId) == HW_Category_IMM)
        {
            GenTree* lastOp = HWIntrinsicInfo::lookupLastOp(node);
            assert(lastOp != nullptr);

            if (HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && lastOp->IsCnsIntOrI())
            {
                MakeSrcContained(node, lastOp);
            }
        }
    }
}
#endif // FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// ContainCheckFloatBinary: determine whether the sources of a floating point binary node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckFloatBinary(GenTreeOp* node)
{
    assert(node->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_DIV) && varTypeIsFloating(node));

    // overflow operations aren't supported on float/double types.
    assert(!node->gtOverflowEx());

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    // No implicit conversions at this stage as the expectation is that
    // everything is made explicit by adding casts.
    assert(op1->TypeGet() == op2->TypeGet());

    bool isSafeToContainOp1 = true;
    bool isSafeToContainOp2 = true;

    if (op2->IsCnsNonZeroFltOrDbl())
    {
        MakeSrcContained(node, op2);
    }
    else if (IsContainableMemoryOp(op2))
    {
        isSafeToContainOp2 = IsSafeToContainMem(node, op2);
        if (isSafeToContainOp2)
        {
            MakeSrcContained(node, op2);
        }
    }

    if (!op2->isContained() && node->OperIsCommutative())
    {
        // Though we have GT_ADD(op1=memOp, op2=non-memOp, we try to reorder the operands
        // as long as it is safe so that the following efficient code sequence is generated:
        //      addss/sd targetReg, memOp    (if op1Reg == targetReg) OR
        //      movaps targetReg, op2Reg; addss/sd targetReg, [memOp]
        //
        // Instead of
        //      movss op1Reg, [memOp]; addss/sd targetReg, Op2Reg  (if op1Reg == targetReg) OR
        //      movss op1Reg, [memOp]; movaps targetReg, op1Reg, addss/sd targetReg, Op2Reg

        if (op1->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(node, op1);
        }
        else if (IsContainableMemoryOp(op1))
        {
            isSafeToContainOp1 = IsSafeToContainMem(node, op1);
            if (isSafeToContainOp1)
            {
                MakeSrcContained(node, op1);
            }
        }
    }

    if (!op1->isContained() && !op2->isContained())
    {
        // If there are no containable operands, we can make an operand reg optional.
        // IsSafeToContainMem is expensive so we call it at most once for each operand
        // in this method. If we already called IsSafeToContainMem, it must have returned false;
        // otherwise, the corresponding operand (op1 or op2) would be contained.
        isSafeToContainOp1 = isSafeToContainOp1 && IsSafeToContainMem(node, op1);
        isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToContainMem(node, op2);
        SetRegOptionalForBinOp(node, isSafeToContainOp1, isSafeToContainOp2);
    }
}

#endif // _TARGET_XARCH_
