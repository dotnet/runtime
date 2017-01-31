// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for AMD64                              XX
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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_XARCH_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

// xarch supports both ROL and ROR instructions so no lowering is required.
void Lowering::LowerRotate(GenTreePtr tree)
{
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

void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    GenTree* op1 = storeLoc->gtGetOp1();

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
    GenTree*   dstAddr       = blkNode->Addr();
    unsigned   size          = blkNode->gtBlkSize;
    GenTree*   source        = blkNode->Data();
    Compiler*  compiler      = comp;
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
        GenTree* initVal = source;
        if (initVal->OperIsInitVal())
        {
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
                }
#else  // !_TARGET_AMD64_
                initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
#endif // !_TARGET_AMD64_

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
    else if (blkNode->gtOper == GT_STORE_OBJ)
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
        assert((blkNode->OperGet() == GT_STORE_BLK) || (blkNode->OperGet() == GT_STORE_DYN_BLK));
        // CopyBlk
        // In case of a CpBlk with a constant size and less than CPBLK_MOVS_LIMIT size
        // we can use rep movs to generate code instead of the helper call.

        // This threshold will decide between using the helper or let the JIT decide to inline
        // a code sequence of its choice.
        unsigned helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);

        // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
        if ((size != 0) && (size <= helperThreshold))
        {
            // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
            // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
            // our framework assemblies, so this is the main code generation scheme we'll use.
            if (size <= CPBLK_UNROLL_LIMIT)
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
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
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
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
            fieldList->gtFlags &= ~GTF_FIELD_LIST_HEAD;

#ifdef DEBUG
            head->gtSeqNum = fieldList->gtSeqNum;
#endif // DEBUG

            head->gtLsraInfo = fieldList->gtLsraInfo;
            head->gtClearReg(comp);

            BlockRange().InsertAfter(fieldList, head);
            BlockRange().Remove(fieldList);

            fieldList        = head;
            putArgStk->gtOp1 = fieldList;
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

    if (putArgStk->TypeGet() != TYP_STRUCT)
    {
        return;
    }

    GenTreePtr dst     = putArgStk;
    GenTreePtr src     = putArgStk->gtOp1;
    GenTreePtr srcAddr = nullptr;

    // In case of a CpBlk we could use a helper call. In case of putarg_stk we
    // can't do that since the helper call could kill some already set up outgoing args.
    // TODO-Amd64-Unix: converge the code for putarg_stk with cpyblk/cpyobj.
    // The cpyXXXX code is rather complex and this could cause it to be more complex, but
    // it might be the right thing to do.

    // This threshold will decide from using the helper or let the JIT decide to inline
    // a code sequence of its choice.
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
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

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

    GenTreePtr op1     = tree->gtOp.gtOp1;
    var_types  dstType = tree->CastToType();
    var_types  srcType = op1->TypeGet();
    var_types  tmpType = TYP_UNDEF;

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    // We should never see the following casts as they are expected to be lowered
    // apropriately or converted into helper calls by front-end.
    //   srcType = float/double                    dstType = * and overflow detecting cast
    //       Reason: must be converted to a helper call
    //   srcType = float/double,                   dstType = ulong
    //       Reason: must be converted to a helper call
    //   srcType = uint                            dstType = float/double
    //       Reason: uint -> float/double = uint -> long -> float/double
    //   srcType = ulong                           dstType = float
    //       Reason: ulong -> float = ulong -> double -> float
    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        noway_assert(dstType != TYP_ULONG);
    }
    else if (srcType == TYP_UINT)
    {
        noway_assert(!varTypeIsFloating(dstType));
    }
    else if (srcType == TYP_ULONG)
    {
        noway_assert(dstType != TYP_FLOAT);
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

            for (GenTree* nodeOperand : node->Operands())
            {
                assert((nodeOperand->gtLIRFlags & LIR::Flags::Mark) == 0);
                nodeOperand->gtLIRFlags |= LIR::Flags::Mark;
                markCount++;
            }
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
bool Lowering::IsBinOpInRMWStoreInd(GenTreePtr tree)
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
    GenTreePtr indirCandidate = nullptr;
    GenTreePtr indirOpSource  = nullptr;
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
bool Lowering::IsRMWMemOpRootedAtStoreInd(GenTreePtr tree, GenTreePtr* outIndirCandidate, GenTreePtr* outIndirOpSource)
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

    GenTreePtr indirDst = storeInd->gtGetOp1();
    GenTreePtr indirSrc = storeInd->gtGetOp2();
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
        if (oper != GT_ADD && oper != GT_SUB && oper != GT_AND && oper != GT_OR && oper != GT_XOR &&
            !GenTree::OperIsShiftOrRotate(oper))
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

//------------------------------------------------------------------------------
// isRMWRegOper: Can this binary tree node be used in a Read-Modify-Write format
//
// Arguments:
//    tree      - a binary tree node
//
// Return Value:
//    Returns true if we can use the read-modify-write instruction form
//
// Notes:
//    This is used to determine whether to preference the source to the destination register.
//
bool Lowering::isRMWRegOper(GenTreePtr tree)
{
    // TODO-XArch-CQ: Make this more accurate.
    // For now, We assume that most binary operators are of the RMW form.
    assert(tree->OperIsBinary());

    if (tree->OperIsCompare())
    {
        return false;
    }

    switch (tree->OperGet())
    {
        // These Opers either support a three op form (i.e. GT_LEA), or do not read/write their first operand
        case GT_LEA:
        case GT_STOREIND:
        case GT_ARR_INDEX:
        case GT_STORE_BLK:
        case GT_STORE_OBJ:
            return false;

        // x86/x64 does support a three op multiply when op2|op1 is a contained immediate
        case GT_MUL:
            return (!IsContainableImmed(tree, tree->gtOp.gtOp2) && !IsContainableImmed(tree, tree->gtOp.gtOp1));

        default:
            return true;
    }
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
    assert(tree->OperIsCommutative() || tree->OperIsCompare());

    GenTree* op1         = tree->gtGetOp1();
    GenTree* op2         = tree->gtGetOp2();
    GenTree* preferredOp = nullptr;

    // This routine uses the following heuristics:
    //
    // a) If both are tracked locals, marking the one with lower weighted
    // ref count as reg-optional would likely be beneficial as it has
    // higher probability of not getting a register.
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

        if (v1->lvTracked && v2->lvTracked)
        {
            // Both are tracked locals.  The one with lower weight is less likely
            // to get a register and hence beneficial to mark the one with lower
            // weight as reg optional.
            if (v1->lvRefCntWtd < v2->lvRefCntWtd)
            {
                preferredOp = op1;
            }
            else
            {
                preferredOp = op2;
            }
        }
        else if (v2->lvTracked)
        {
            // v1 is an untracked lcl and it is use position is less likely to
            // get a register.
            preferredOp = op1;
        }
        else if (v1->lvTracked)
        {
            // v2 is an untracked lcl and its def position always
            // needs a reg.  Hence it is better to mark v1 as
            // reg optional.
            preferredOp = op1;
        }
        else
        {
            preferredOp = op1;
            ;
        }
    }
    else if (op1->OperGet() == GT_LCL_VAR)
    {
        preferredOp = op1;
    }
    else if (op2->OperGet() == GT_LCL_VAR)
    {
        preferredOp = op2;
    }
    else
    {
        // Neither of the operands is a local, prefer marking
        // operand that is evaluated first as reg optional
        // since its use position is less likely to get a register.
        bool reverseOps = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);
        preferredOp     = reverseOps ? op2 : op1;
    }

    return preferredOp;
}

#endif // _TARGET_XARCH_

#endif // !LEGACY_BACKEND
