// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifdef TARGET_XARCH // This file is only used for xarch

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

        unsigned   varNum = storeLoc->GetLclNum();
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
    else if (node->AsStoreInd()->Data()->OperIs(GT_CNS_DBL))
    {
        // Optimize *x = DCON to *x = ICON which is slightly faster on xarch
        GenTree*  data   = node->AsStoreInd()->Data();
        double    dblCns = data->AsDblCon()->gtDconVal;
        ssize_t   intCns = 0;
        var_types type   = TYP_UNKNOWN;

        if (node->TypeIs(TYP_FLOAT))
        {
            float fltCns = static_cast<float>(dblCns); // should be a safe round-trip
            intCns       = static_cast<ssize_t>(*reinterpret_cast<UINT32*>(&fltCns));
            type         = TYP_UINT;
        }
#ifdef TARGET_AMD64
        else
        {
            assert(node->TypeIs(TYP_DOUBLE));
            intCns = static_cast<ssize_t>(*reinterpret_cast<UINT64*>(&dblCns));
            type   = TYP_ULONG;
        }
#endif

        if (type != TYP_UNKNOWN)
        {
            data->SetContained();
            data->ChangeOperConst(GT_CNS_INT);
            data->AsIntCon()->SetIconValue(intCns);
            data->ChangeType(type);
            node->ChangeType(type);
        }
    }
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
    TryCreateAddrMode(blkNode->Addr(), false);

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

        if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (size <= INITBLK_UNROLL_LIMIT))
        {
            if (!src->OperIs(GT_CNS_INT))
            {
                // TODO-CQ: We could unroll even when the initialization value is not a constant
                // by inserting a MUL init, 0x01010101 instruction. We need to determine if the
                // extra latency that MUL introduces isn't worse that rep stosb. Likely not.
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
            }
            else
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
                    // If the size is multiple of XMM register size there's no need to load 0 in a GPR,
                    // codegen will use xorps to generate 0 directly in the temporary XMM register.
                    if ((size % XMM_REGSIZE_BYTES) == 0)
                    {
                        src->SetContained();
                    }
                }
#ifdef TARGET_AMD64
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
        }
        else
        {
#ifdef TARGET_AMD64
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
#else
            // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
#endif
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

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            if (!blkNode->AsObj()->GetLayout()->HasGCPtr())
            {
                blkNode->SetOper(GT_STORE_BLK);
            }
#ifndef JIT32_GCENCODER
            else if (dstAddr->OperIsLocalAddr() && (size <= CPBLK_UNROLL_LIMIT))
            {
                // If the size is small enough to unroll then we need to mark the block as non-interruptible
                // to actually allow unrolling. The generated code does not report GC references loaded in the
                // temporary register(s) used for copying.
                // This is not supported for the JIT32_GCENCODER.
                blkNode->SetOper(GT_STORE_BLK);
                blkNode->gtBlkOpGcUnsafe = true;
            }
#endif
        }

        if (blkNode->OperIs(GT_STORE_OBJ))
        {
            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));

            // If we have a long enough sequence of slots that do not require write barriers then
            // we can use REP MOVSD/Q instead of a sequence of MOVSD/Q instructions. According to the
            // Intel Manual, the sweet spot for small structs is between 4 to 12 slots of size where
            // the entire operation takes 20 cycles and encodes in 5 bytes (loading RCX and REP MOVSD/Q).
            unsigned nonGCSlots = 0;

            if (dstAddr->OperIsLocalAddr())
            {
                // If the destination is on the stack then no write barriers are needed.
                nonGCSlots = blkNode->GetLayout()->GetSlotCount();
            }
            else
            {
                // Otherwise a write barrier is needed for every GC pointer in the layout
                // so we need to check if there's a long enough sequence of non-GC slots.
                ClassLayout* layout = blkNode->GetLayout();
                unsigned     slots  = layout->GetSlotCount();
                for (unsigned i = 0; i < slots; i++)
                {
                    if (layout->IsGCPtr(i))
                    {
                        nonGCSlots = 0;
                    }
                    else
                    {
                        nonGCSlots++;

                        if (nonGCSlots >= CPOBJ_NONGC_SLOTS_LIMIT)
                        {
                            break;
                        }
                    }
                }
            }

            if (nonGCSlots >= CPOBJ_NONGC_SLOTS_LIMIT)
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
            }
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
            }
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

#ifdef TARGET_AMD64
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindHelper;
#else
            // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindRepInstr;
#endif
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

    if (!addr->OperIsAddrMode() && !TryCreateAddrMode(addr, true))
    {
        return;
    }

    GenTreeAddrMode* addrMode = addr->AsAddrMode();

    // On x64 the address mode displacement is signed so it must not exceed INT32_MAX. This check is
    // an approximation since the last displacement we generate in an unrolled block operation can be
    // up to 16 bytes lower than offset + size. But offsets large enough to hit this case are likely
    // to be extremely rare for this to ever be a CQ issue.
    // On x86 this shouldn't be needed but then again, offsets large enough to hit this are rare.
    if (addrMode->Offset() > (INT32_MAX - static_cast<int>(size)))
    {
        return;
    }

    // Note that the parentNode is always the block node, even if we're dealing with the source address.
    // The source address is not directly used by the block node but by an IND node and that IND node is
    // always contained.
    if (!IsSafeToContainMem(blkNode, addrMode))
    {
        return;
    }

    addrMode->SetContained();
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
    GenTree* src = putArgStk->gtGetOp1();

    if (src->OperIs(GT_FIELD_LIST))
    {
#ifdef TARGET_X86
        putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Invalid;

        GenTreeFieldList* fieldList = src->AsFieldList();

        // The code generator will push these fields in reverse order by offset. Reorder the list here s.t. the order
        // of uses is visible to LSRA.
        assert(fieldList->Uses().IsSorted());
        fieldList->Uses().Reverse();

        // Now that the fields have been sorted, the kind of code we will generate.
        bool     allFieldsAreSlots = true;
        unsigned prevOffset        = putArgStk->GetStackByteSize();
        for (GenTreeFieldList::Use& use : fieldList->Uses())
        {
            GenTree* const  fieldNode   = use.GetNode();
            const var_types fieldType   = fieldNode->TypeGet();
            const unsigned  fieldOffset = use.GetOffset();
            assert(fieldType != TYP_LONG);

            // We can treat as a slot any field that is stored at a slot boundary, where the previous
            // field is not in the same slot. (Note that we store the fields in reverse order.)
            const bool fieldIsSlot = ((fieldOffset % 4) == 0) && ((prevOffset - fieldOffset) >= 4);
            if (!fieldIsSlot)
            {
                allFieldsAreSlots = false;
            }

            // For x86 we must mark all integral fields as contained or reg-optional, and handle them
            // accordingly in code generation, since we may have up to 8 fields, which cannot all be in
            // registers to be consumed atomically by the call.
            if (varTypeIsIntegralOrI(fieldNode))
            {
                if (fieldNode->OperGet() == GT_LCL_VAR)
                {
                    LclVarDsc* varDsc = &(comp->lvaTable[fieldNode->AsLclVarCommon()->GetLclNum()]);
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
#endif // TARGET_X86
        return;
    }

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
#if defined(TARGET_AMD64)
            && !src->IsIntegralConst(0)
#endif // TARGET_AMD64
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
        srcAddr = src->AsOp()->gtOp1;
        assert(srcAddr != nullptr);
        haveLocalAddr = srcAddr->OperIsLocalAddr();
    }
    else
    {
        assert(varTypeIsSIMD(putArgStk));
    }

    ClassLayout* layout = src->AsObj()->GetLayout();

    // In case of a CpBlk we could use a helper call. In case of putarg_stk we
    // can't do that since the helper call could kill some already set up outgoing args.
    // TODO-Amd64-Unix: converge the code for putarg_stk with cpyblk/cpyobj.
    // The cpyXXXX code is rather complex and this could cause it to be more complex, but
    // it might be the right thing to do.

    unsigned size = putArgStk->GetStackByteSize();

    // TODO-X86-CQ: The helper call either is not supported on x86 or required more work
    // (I don't know which).

    if (size <= CPBLK_UNROLL_LIMIT && !layout->HasGCPtr())
    {
#ifdef TARGET_X86
        if (size < XMM_REGSIZE_BYTES)
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
        }
        else
#endif // TARGET_X86
        {
            putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Unroll;
        }
    }
#ifdef TARGET_X86
    else if (layout->HasGCPtr())
    {
        // On x86, we must use `push` to store GC references to the stack in order for the emitter to properly update
        // the function's GC info. These `putargstk` nodes will generate a sequence of `push` instructions.
        putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
    }
#endif // TARGET_X86
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
 * assemblies or in CQ perf benchmarks (1 occurrence in corelib, microsoft.jscript,
 * 1 occurrence in Roslyn and no occurrences in system, system.core, system.numerics
 * system.windows.forms, scimark, fractals, bio mums). If we ever find evidence that
 * doing this optimization is a win, should consider generating in-lined code.
 */
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    GenTree*  castOp     = tree->AsCast()->CastOp();
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
        tree->AsOp()->gtOp1 = tmp;
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
        assert(simdNode->GetSimdBaseType() == TYP_FLOAT);

        int   argCount      = 0;
        int   constArgCount = 0;
        float constArgValues[4]{0, 0, 0, 0};

        for (GenTreeArgList* list = simdNode->gtGetOp1()->AsArgList(); list != nullptr; list = list->Rest())
        {
            GenTree* arg = list->Current();

            assert(arg->TypeGet() == simdNode->GetSimdBaseType());
            assert(argCount < (int)_countof(constArgValues));

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

            assert(sizeof(constArgValues) == 16);

            unsigned cnsSize  = sizeof(constArgValues);
            unsigned cnsAlign = (comp->compCodeOpt() != Compiler::SMALL_CODE) ? cnsSize : 1;

            CORINFO_FIELD_HANDLE hnd =
                comp->GetEmitter()->emitBlkConst(constArgValues, cnsSize, cnsAlign, simdNode->GetSimdBaseType());
            GenTree* clsVarAddr = new (comp, GT_CLS_VAR_ADDR) GenTreeClsVar(GT_CLS_VAR_ADDR, TYP_I_IMPL, hnd, nullptr);
            BlockRange().InsertBefore(simdNode, clsVarAddr);
            simdNode->ChangeOper(GT_IND);
            simdNode->gtOp1 = clsVarAddr;
            ContainCheckIndir(simdNode->AsIndir());

            return;
        }
    }

    ContainCheckSIMD(simdNode);
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// LowerHWIntrinsicCC: Lowers a hardware intrinsic node that produces a boolean value by
//     setting the condition flags.
//
//  Arguments:
//     node - The hardware intrinsic node
//     newIntrinsicId - The intrinsic id of the lowered intrinsic node
//     condition - The condition code of the generated SETCC/JCC node
//
void Lowering::LowerHWIntrinsicCC(GenTreeHWIntrinsic* node, NamedIntrinsic newIntrinsicId, GenCondition condition)
{
    GenTreeCC* cc = LowerNodeCC(node, condition);

    node->gtHWIntrinsicId = newIntrinsicId;
    node->gtType          = TYP_VOID;
    node->ClearUnusedValue();

    bool swapOperands    = false;
    bool canSwapOperands = false;

    switch (newIntrinsicId)
    {
        case NI_SSE_COMISS:
        case NI_SSE_UCOMISS:
        case NI_SSE2_COMISD:
        case NI_SSE2_UCOMISD:
            // In some cases we can generate better code if we swap the operands:
            //   - If the condition is not one of the "preferred" floating point conditions we can swap
            //     the operands and change the condition to avoid generating an extra JP/JNP branch.
            //   - If the first operand can be contained but the second cannot, we can swap operands in
            //     order to be able to contain the first operand and avoid the need for a temp reg.
            // We can't handle both situations at the same time and since an extra branch is likely to
            // be worse than an extra temp reg (x64 has a reasonable number of XMM registers) we'll favor
            // the branch case:
            //   - If the condition is not preferred then swap, even if doing this will later prevent
            //     containment.
            //   - Allow swapping for containment purposes only if this doesn't result in a non-"preferred"
            //     condition being generated.
            if ((cc != nullptr) && cc->gtCondition.PreferSwap())
            {
                swapOperands = true;
            }
            else
            {
                canSwapOperands = (cc == nullptr) || !GenCondition::Swap(cc->gtCondition).PreferSwap();
            }
            break;

        case NI_SSE41_PTEST:
        case NI_AVX_PTEST:
            // If we need the Carry flag then we can't swap operands.
            canSwapOperands = (cc == nullptr) || cc->gtCondition.Is(GenCondition::EQ, GenCondition::NE);
            break;

        default:
            unreached();
    }

    if (canSwapOperands)
    {
        bool op1SupportsRegOptional = false;
        bool op2SupportsRegOptional = false;

        if (!IsContainableHWIntrinsicOp(node, node->gtGetOp2(), &op2SupportsRegOptional) &&
            IsContainableHWIntrinsicOp(node, node->gtGetOp1(), &op1SupportsRegOptional))
        {
            // Swap operands if op2 cannot be contained but op1 can.
            swapOperands = true;
        }
    }

    if (swapOperands)
    {
        std::swap(node->gtOp1, node->gtOp2);

        if (cc != nullptr)
        {
            cc->gtCondition = GenCondition::Swap(cc->gtCondition);
        }
    }
}

//----------------------------------------------------------------------------------------------
// LowerFusedMultiplyAdd: Changes NI_FMA_MultiplyAddScalar produced by Math(F).FusedMultiplyAdd
//     to a better FMA intrinsics if there are GT_NEG around in order to eliminate them.
//
//  Arguments:
//     node - The hardware intrinsic node
//
//  Notes:
//     Math(F).FusedMultiplyAdd is expanded into NI_FMA_MultiplyAddScalar and
//     depending on additional GT_NEG nodes around it can be:
//
//      x *  y + z -> NI_FMA_MultiplyAddScalar
//      x * -y + z -> NI_FMA_MultiplyAddNegatedScalar
//     -x *  y + z -> NI_FMA_MultiplyAddNegatedScalar
//     -x * -y + z -> NI_FMA_MultiplyAddScalar
//      x *  y - z -> NI_FMA_MultiplySubtractScalar
//      x * -y - z -> NI_FMA_MultiplySubtractNegatedScalar
//     -x *  y - z -> NI_FMA_MultiplySubtractNegatedScalar
//     -x * -y - z -> NI_FMA_MultiplySubtractScalar
//
void Lowering::LowerFusedMultiplyAdd(GenTreeHWIntrinsic* node)
{
    assert(node->gtHWIntrinsicId == NI_FMA_MultiplyAddScalar);
    GenTreeArgList*     argList = node->gtGetOp1()->AsArgList();
    GenTreeHWIntrinsic* createScalarOps[3];

    for (GenTreeHWIntrinsic*& createScalarOp : createScalarOps)
    {
        GenTree*& current = argList->Current();
        assert(current != nullptr);
        if (!current->OperIsHWIntrinsic())
        {
            return; // Math(F).FusedMultiplyAdd is expected to emit three NI_Vector128_CreateScalarUnsafe
                    // but it's also possible to use NI_FMA_MultiplyAddScalar directly with any operands
        }
        GenTreeHWIntrinsic* hwArg = current->AsHWIntrinsic();
        if (hwArg->gtHWIntrinsicId != NI_Vector128_CreateScalarUnsafe)
        {
            return;
        }
        createScalarOp = hwArg;
        argList        = argList->Rest();
    }
    assert(argList == nullptr);

    GenTree* argX = createScalarOps[0]->gtGetOp1();
    GenTree* argY = createScalarOps[1]->gtGetOp1();
    GenTree* argZ = createScalarOps[2]->gtGetOp1();

    const bool negMul = argX->OperIs(GT_NEG) != argY->OperIs(GT_NEG);
    if (argX->OperIs(GT_NEG))
    {
        createScalarOps[0]->gtOp1 = argX->gtGetOp1();
        BlockRange().Remove(argX);
    }
    if (argY->OperIs(GT_NEG))
    {
        createScalarOps[1]->gtOp1 = argY->gtGetOp1();
        BlockRange().Remove(argY);
    }
    if (argZ->OperIs(GT_NEG))
    {
        createScalarOps[2]->gtOp1 = argZ->gtGetOp1();
        BlockRange().Remove(argZ);
        node->gtHWIntrinsicId = negMul ? NI_FMA_MultiplySubtractNegatedScalar : NI_FMA_MultiplySubtractScalar;
    }
    else
    {
        node->gtHWIntrinsicId = negMul ? NI_FMA_MultiplyAddNegatedScalar : NI_FMA_MultiplyAddScalar;
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
    if (node->TypeGet() == TYP_SIMD12)
    {
        // GT_HWINTRINSIC node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        node->gtType = TYP_SIMD16;
    }

    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;

    switch (intrinsicId)
    {
        case NI_Vector128_Create:
        case NI_Vector256_Create:
        {
            // We don't directly support the Vector128.Create or Vector256.Create methods in codegen
            // and instead lower them to other intrinsic nodes in LowerHWIntrinsicCreate so we expect
            // that the node is modified to either not be a HWIntrinsic node or that it is no longer
            // the same intrinsic as when it came in. In the case of Vector256.Create, we may lower
            // it into 2x Vector128.Create intrinsics which themselves are also lowered into other
            // intrinsics that are not Vector*.Create

            LowerHWIntrinsicCreate(node);
            assert(!node->OperIsHWIntrinsic() || (node->gtHWIntrinsicId != intrinsicId));
            LowerNode(node);
            return;
        }

        case NI_Vector128_Dot:
        case NI_Vector256_Dot:
        {
            LowerHWIntrinsicDot(node);
            return;
        }

        case NI_Vector128_GetElement:
        case NI_Vector256_GetElement:
        {
            LowerHWIntrinsicGetElement(node);

            if ((node->gtHWIntrinsicId == NI_Vector128_GetElement) ||
                (node->gtHWIntrinsicId == NI_Vector256_GetElement))
            {
                // Most NI_Vector*_GetElement intrinsics are lowered to
                // alternative nodes, such as the Extract intrinsics,
                // which are themselves lowered.
                //
                // However, certain types may not have a direct equivalent
                // in which case we specially handle them directly as GetElement
                // and want to do the relevant containment checks.
                break;
            }
            return;
        }

        case NI_Vector128_WithElement:
        case NI_Vector256_WithElement:
        {
            LowerHWIntrinsicWithElement(node);
            return;
        }

        case NI_Vector128_op_Equality:
        case NI_Vector256_op_Equality:
        {
            LowerHWIntrinsicCmpOp(node, GT_EQ);
            return;
        }

        case NI_Vector128_op_Inequality:
        case NI_Vector256_op_Inequality:
        {
            LowerHWIntrinsicCmpOp(node, GT_NE);
            return;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        {
            LowerHWIntrinsicToScalar(node);
            break;
        }

        case NI_SSE41_Extract:
        {
            if (varTypeIsFloating(node->GetSimdBaseType()))
            {
                assert(node->GetSimdBaseType() == TYP_FLOAT);
                assert(node->gtOp1 != nullptr);
                assert(node->gtOp2 != nullptr);
                assert(node->GetSimdSize() == 16);

                GenTree* op2 = node->gtGetOp2();

                if (!op2->OperIsConst())
                {
                    // Extract allows the full range while GetElement only allows
                    // 0-3, so we need to mask the index here so codegen works.

                    GenTree* msk = comp->gtNewIconNode(3, TYP_INT);
                    BlockRange().InsertAfter(op2, msk);

                    GenTree* tmp = comp->gtNewOperNode(GT_AND, TYP_INT, op2, msk);
                    BlockRange().InsertAfter(msk, tmp);
                    LowerNode(tmp);

                    node->gtOp2 = tmp;
                }

                node->gtHWIntrinsicId = NI_Vector128_GetElement;
                LowerNode(node);
            }
            break;
        }

        case NI_SSE2_Insert:
        case NI_SSE41_Insert:
        case NI_SSE41_X64_Insert:
        {
            assert(HWIntrinsicInfo::lookupNumArgs(node) == 3);

            GenTreeArgList* argList = node->gtOp1->AsArgList();

            // Insert takes either a 32-bit register or a memory operand.
            // In either case, only SimdBaseType bits are read and so
            // widening or narrowing the operand may be unnecessary and it
            // can just be used directly.

            argList->Rest()->gtOp1 = TryRemoveCastIfPresent(node->GetSimdBaseType(), argList->Rest()->gtOp1);
            break;
        }

        case NI_SSE42_Crc32:
        {
            assert(HWIntrinsicInfo::lookupNumArgs(node) == 2);

            // Crc32 takes either a bit register or a memory operand.
            // In either case, only gtType bits are read and so widening
            // or narrowing the operand may be unnecessary and it can
            // just be used directly.

            node->gtOp2 = TryRemoveCastIfPresent(node->gtType, node->gtOp2);
            break;
        }

        case NI_SSE2_CompareGreaterThan:
        {
            if (node->GetSimdBaseType() != TYP_DOUBLE)
            {
                assert(varTypeIsIntegral(node->GetSimdBaseType()));
                break;
            }

            FALLTHROUGH;
        }

        case NI_SSE_CompareGreaterThan:
        case NI_SSE_CompareGreaterThanOrEqual:
        case NI_SSE_CompareNotGreaterThan:
        case NI_SSE_CompareNotGreaterThanOrEqual:
        case NI_SSE2_CompareGreaterThanOrEqual:
        case NI_SSE2_CompareNotGreaterThan:
        case NI_SSE2_CompareNotGreaterThanOrEqual:
        {
            assert((node->GetSimdBaseType() == TYP_FLOAT) || (node->GetSimdBaseType() == TYP_DOUBLE));

            if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                break;
            }

            // pre-AVX doesn't actually support these intrinsics in hardware so we need to swap the operands around
            std::swap(node->gtOp1, node->gtOp2);
            break;
        }

        case NI_SSE2_CompareLessThan:
        case NI_SSE42_CompareLessThan:
        case NI_AVX2_CompareLessThan:
        {
            if (node->GetSimdBaseType() == TYP_DOUBLE)
            {
                break;
            }
            assert(varTypeIsIntegral(node->GetSimdBaseType()));

            // this isn't actually supported in hardware so we need to swap the operands around
            std::swap(node->gtOp1, node->gtOp2);
            break;
        }

        case NI_SSE_CompareScalarOrderedEqual:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FEQ);
            break;
        case NI_SSE_CompareScalarOrderedNotEqual:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FNEU);
            break;
        case NI_SSE_CompareScalarOrderedLessThan:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FLT);
            break;
        case NI_SSE_CompareScalarOrderedLessThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FLE);
            break;
        case NI_SSE_CompareScalarOrderedGreaterThan:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FGT);
            break;
        case NI_SSE_CompareScalarOrderedGreaterThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE_COMISS, GenCondition::FGE);
            break;

        case NI_SSE_CompareScalarUnorderedEqual:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FEQ);
            break;
        case NI_SSE_CompareScalarUnorderedNotEqual:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FNEU);
            break;
        case NI_SSE_CompareScalarUnorderedLessThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FLE);
            break;
        case NI_SSE_CompareScalarUnorderedLessThan:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FLT);
            break;
        case NI_SSE_CompareScalarUnorderedGreaterThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FGE);
            break;
        case NI_SSE_CompareScalarUnorderedGreaterThan:
            LowerHWIntrinsicCC(node, NI_SSE_UCOMISS, GenCondition::FGT);
            break;

        case NI_SSE2_CompareScalarOrderedEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FEQ);
            break;
        case NI_SSE2_CompareScalarOrderedNotEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FNEU);
            break;
        case NI_SSE2_CompareScalarOrderedLessThan:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FLT);
            break;
        case NI_SSE2_CompareScalarOrderedLessThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FLE);
            break;
        case NI_SSE2_CompareScalarOrderedGreaterThan:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FGT);
            break;
        case NI_SSE2_CompareScalarOrderedGreaterThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_COMISD, GenCondition::FGE);
            break;

        case NI_SSE2_CompareScalarUnorderedEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FEQ);
            break;
        case NI_SSE2_CompareScalarUnorderedNotEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FNEU);
            break;
        case NI_SSE2_CompareScalarUnorderedLessThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FLE);
            break;
        case NI_SSE2_CompareScalarUnorderedLessThan:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FLT);
            break;
        case NI_SSE2_CompareScalarUnorderedGreaterThanOrEqual:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FGE);
            break;
        case NI_SSE2_CompareScalarUnorderedGreaterThan:
            LowerHWIntrinsicCC(node, NI_SSE2_UCOMISD, GenCondition::FGT);
            break;

        case NI_SSE41_TestC:
            LowerHWIntrinsicCC(node, NI_SSE41_PTEST, GenCondition::C);
            break;
        case NI_SSE41_TestZ:
            LowerHWIntrinsicCC(node, NI_SSE41_PTEST, GenCondition::EQ);
            break;
        case NI_SSE41_TestNotZAndNotC:
            LowerHWIntrinsicCC(node, NI_SSE41_PTEST, GenCondition::UGT);
            break;

        case NI_AVX_TestC:
            LowerHWIntrinsicCC(node, NI_AVX_PTEST, GenCondition::C);
            break;
        case NI_AVX_TestZ:
            LowerHWIntrinsicCC(node, NI_AVX_PTEST, GenCondition::EQ);
            break;
        case NI_AVX_TestNotZAndNotC:
            LowerHWIntrinsicCC(node, NI_AVX_PTEST, GenCondition::UGT);
            break;

        case NI_FMA_MultiplyAddScalar:
            LowerFusedMultiplyAdd(node);
            break;

        default:
            break;
    }

    ContainCheckHWIntrinsic(node);
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

    assert((intrinsicId == NI_Vector128_op_Equality) || (intrinsicId == NI_Vector128_op_Inequality) ||
           (intrinsicId == NI_Vector256_op_Equality) || (intrinsicId == NI_Vector256_op_Inequality));

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

    GenCondition cmpCnd = (cmpOp == GT_EQ) ? GenCondition::EQ : GenCondition::NE;

    if (op2->IsIntegralConstVector(0) && comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        // On SSE4.1 or higher we can optimize comparisons against zero to
        // just use PTEST. We can't support it for floating-point, however,
        // as it has both +0.0 and -0.0 where +0.0 == -0.0

        node->gtOp1 = op1;
        BlockRange().Remove(op2);

        LIR::Use op1Use(BlockRange(), &node->gtOp1, node);
        ReplaceWithLclVar(op1Use);
        op1 = node->gtOp1;

        op2 = comp->gtClone(op1);
        BlockRange().InsertAfter(op1, op2);
        node->gtOp2 = op2;

        if (simdSize == 32)
        {
            node->gtHWIntrinsicId = NI_AVX_TestZ;
            LowerHWIntrinsicCC(node, NI_AVX_PTEST, cmpCnd);
        }
        else
        {
            node->gtHWIntrinsicId = NI_SSE41_TestZ;
            LowerHWIntrinsicCC(node, NI_SSE41_PTEST, cmpCnd);
        }

        return;
    }

    NamedIntrinsic cmpIntrinsic;
    CorInfoType    cmpJitType;
    NamedIntrinsic mskIntrinsic;
    CorInfoType    mskJitType;
    int            mskConstant;

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        {
            cmpJitType = simdBaseJitType;
            mskJitType = CORINFO_TYPE_UBYTE;

            if (simdSize == 32)
            {
                cmpIntrinsic = NI_AVX2_CompareEqual;
                mskIntrinsic = NI_AVX2_MoveMask;
                mskConstant  = -1;
            }
            else
            {
                assert(simdSize == 16);

                cmpIntrinsic = NI_SSE2_CompareEqual;
                mskIntrinsic = NI_SSE2_MoveMask;
                mskConstant  = 0xFFFF;
            }
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            mskJitType = CORINFO_TYPE_UBYTE;

            if (simdSize == 32)
            {
                cmpIntrinsic = NI_AVX2_CompareEqual;
                cmpJitType   = simdBaseJitType;
                mskIntrinsic = NI_AVX2_MoveMask;
                mskConstant  = -1;
            }
            else
            {
                assert(simdSize == 16);

                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    cmpIntrinsic = NI_SSE41_CompareEqual;
                    cmpJitType   = simdBaseJitType;
                }
                else
                {
                    cmpIntrinsic = NI_SSE2_CompareEqual;
                    cmpJitType   = CORINFO_TYPE_UINT;
                }

                mskIntrinsic = NI_SSE2_MoveMask;
                mskConstant  = 0xFFFF;
            }
            break;
        }

        case TYP_FLOAT:
        {
            cmpJitType = simdBaseJitType;
            mskJitType = simdBaseJitType;

            if (simdSize == 32)
            {
                cmpIntrinsic = NI_AVX_CompareEqual;
                mskIntrinsic = NI_AVX_MoveMask;
                mskConstant  = 0xFF;
            }
            else
            {
                cmpIntrinsic = NI_SSE_CompareEqual;
                mskIntrinsic = NI_SSE_MoveMask;

                if (simdSize == 16)
                {
                    mskConstant = 0xF;
                }
                else if (simdSize == 12)
                {
                    mskConstant = 0x7;
                }
                else
                {
                    assert(simdSize == 8);
                    mskConstant = 0x3;
                }
            }
            break;
        }

        case TYP_DOUBLE:
        {
            cmpJitType = simdBaseJitType;
            mskJitType = simdBaseJitType;

            if (simdSize == 32)
            {
                cmpIntrinsic = NI_AVX_CompareEqual;
                mskIntrinsic = NI_AVX_MoveMask;
                mskConstant  = 0xF;
            }
            else
            {
                assert(simdSize == 16);

                cmpIntrinsic = NI_SSE2_CompareEqual;
                mskIntrinsic = NI_SSE2_MoveMask;
                mskConstant  = 0x3;
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    GenTree* cmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, cmpIntrinsic, cmpJitType, simdSize);
    BlockRange().InsertBefore(node, cmp);
    LowerNode(cmp);

    GenTree* msk = comp->gtNewSimdHWIntrinsicNode(TYP_INT, cmp, mskIntrinsic, mskJitType, simdSize);
    BlockRange().InsertAfter(cmp, msk);
    LowerNode(msk);

    GenTree* mskCns = comp->gtNewIconNode(mskConstant, TYP_INT);
    BlockRange().InsertAfter(msk, mskCns);

    if ((simdBaseType == TYP_FLOAT) && (simdSize < 16))
    {
        // For TYP_SIMD8 and TYP_SIMD12 we need to clear the upper bits and can't assume their value

        GenTree* tmp = comp->gtNewOperNode(GT_AND, TYP_INT, msk, mskCns);
        BlockRange().InsertAfter(mskCns, tmp);
        LowerNode(tmp);

        msk = tmp;

        mskCns = comp->gtNewIconNode(mskConstant, TYP_INT);
        BlockRange().InsertAfter(msk, mskCns);
    }

    node->ChangeOper(cmpOp);

    node->gtType = TYP_INT;
    node->gtOp1  = msk;
    node->gtOp2  = mskCns;

    GenTree* cc = LowerNodeCC(node, cmpCnd);

    node->gtType = TYP_VOID;
    node->ClearUnusedValue();

    LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector128 or Vector256 Create call
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

    if (argCnt == cnsArgCnt)
    {
        if (op1->OperIsList())
        {
            for (argList = op1->AsArgList(); argList != nullptr; argList = argList->Rest())
            {
                GenTree* arg = argList->Current();

#if !defined(TARGET_64BIT)
                if (arg->OperIsLong())
                {
                    BlockRange().Remove(arg->AsOp()->gtOp1);
                    BlockRange().Remove(arg->AsOp()->gtOp2);
                }
#endif // !TARGET_64BIT

                BlockRange().Remove(arg);
            }
        }
        else
        {
#if !defined(TARGET_64BIT)
            if (op1->OperIsLong())
            {
                BlockRange().Remove(op1->AsOp()->gtOp1);
                BlockRange().Remove(op1->AsOp()->gtOp2);
            }
#endif // !TARGET_64BIT

            BlockRange().Remove(op1);

            if (op2 != nullptr)
            {
#if defined(TARGET_64BIT)
                if (op2->OperIsLong())
                {
                    BlockRange().Remove(op2->AsOp()->gtOp1);
                    BlockRange().Remove(op2->AsOp()->gtOp2);
                }
#endif // !TARGET_64BIT

                BlockRange().Remove(op2);
            }
        }

        assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16) || (simdSize == 32));

        if (((simdSize == 16) || (simdSize == 32)) && VectorConstantIsBroadcastedI64(vecCns, simdSize / 8))
        {
            // If we are a single constant or if all parts are the same, we might be able to optimize
            // this even further for certain values, such as Zero or AllBitsSet.

            if (vecCns.i64[0] == 0)
            {
                node->gtOp1           = nullptr;
                node->gtOp2           = nullptr;
                node->gtHWIntrinsicId = (simdSize == 16) ? NI_Vector128_get_Zero : NI_Vector256_get_Zero;
                return;
            }
            else if (vecCns.i64[0] == -1)
            {
                node->gtOp1           = nullptr;
                node->gtOp2           = nullptr;
                node->gtHWIntrinsicId = (simdSize == 16) ? NI_Vector128_get_AllBitsSet : NI_Vector256_get_AllBitsSet;
                return;
            }
        }

        unsigned cnsSize = (simdSize != 12) ? simdSize : 16;
        unsigned cnsAlign =
            (comp->compCodeOpt() != Compiler::SMALL_CODE) ? cnsSize : emitter::dataSection::MIN_DATA_ALIGN;
        var_types dataType = Compiler::getSIMDTypeForSize(simdSize);

        UNATIVE_OFFSET       cnum = comp->GetEmitter()->emitDataConst(&vecCns, cnsSize, cnsAlign, dataType);
        CORINFO_FIELD_HANDLE hnd  = comp->eeFindJitDataOffs(cnum);
        GenTree* clsVarAddr = new (comp, GT_CLS_VAR_ADDR) GenTreeClsVar(GT_CLS_VAR_ADDR, TYP_I_IMPL, hnd, nullptr);
        BlockRange().InsertBefore(node, clsVarAddr);

        node->ChangeOper(GT_IND);
        node->gtOp1 = clsVarAddr;

        // TODO-XARCH-CQ: We should be able to modify at least the paths that use Insert to trivially support partial
        // vector constants. With this, we can create a constant if say 50% of the inputs are also constant and just
        // insert the non-constant values which should still allow some gains.

        return;
    }
    else if (argCnt == 1)
    {
        // We have the following (where simd is simd16 or simd32):
        //          /--*  op1  T
        //   node = *  HWINTRINSIC   simd   T Create

        if (intrinsicId == NI_Vector256_Create)
        {
            if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // We will be constructing the following parts:
                //          /--*  op1  T
                //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                //          /--*  tmp1 simd16
                //   node = *  HWINTRINSIC   simd32 T BroadcastScalarToVector256

                // This is roughly the following managed code:
                //   var tmp1 = Vector128.CreateScalarUnsafe(op1);
                //   return Avx2.BroadcastScalarToVector256(tmp1);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector128_CreateScalarUnsafe, simdBaseJitType,
                                                      16);
                BlockRange().InsertAfter(op1, tmp1);
                LowerNode(tmp1);

                node->gtOp1 = tmp1;
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = NI_AVX2_BroadcastScalarToVector256;
                return;
            }

            assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

            // We will be constructing the following parts:
            //          /--*  op1  T
            //   tmp1 = *  HWINTRINSIC   simd16 T Create
            //          /--*  tmp1 simd16
            //          *  STORE_LCL_VAR simd16
            //   tmp1 =    LCL_VAR       simd16
            //   tmp2 =    LCL_VAR       simd16
            //          /--*  tmp2 simd16
            //   tmp3 = *  HWINTRINSIC   simd16 T ToVector256Unsafe
            //   idx  =    CNS_INT       int    0
            //          /--*  tmp3 simd32
            //          +--*  tmp1 simd16
            //          +--*  idx  int
            //   node = *  HWINTRINSIC simd32 T InsertVector128

            // This is roughly the following managed code:
            //   var tmp1 = Vector128.Create(op1);
            //   var tmp2 = tmp1;
            //   var tmp3 = tmp2.ToVector256Unsafe();
            //   return Avx.InsertVector128(tmp3, tmp1, 0x01);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector128_Create, simdBaseJitType, 16);
            BlockRange().InsertAfter(op1, tmp1);
            LowerNode(tmp1);

            node->gtOp1 = tmp1;
            LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
            ReplaceWithLclVar(tmp1Use);
            tmp1 = node->gtOp1;

            tmp2 = comp->gtClone(tmp1);
            BlockRange().InsertAfter(tmp1, tmp2);

            tmp3 =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, tmp2, NI_Vector128_ToVector256Unsafe, simdBaseJitType, 16);
            BlockRange().InsertAfter(tmp2, tmp3);
            LowerNode(tmp3);

            idx = comp->gtNewIconNode(0x01, TYP_INT);
            BlockRange().InsertAfter(tmp3, idx);

            node->gtOp1 = comp->gtNewArgList(tmp3, tmp1, idx);
            node->gtOp2 = nullptr;

            node->gtHWIntrinsicId = NI_AVX_InsertVector128;
            return;
        }

        // We will be constructing the following parts:
        //          /--*  op1  T
        //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
        //   ...

        // This is roughly the following managed code:
        //   var tmp1 = Vector128.CreateScalarUnsafe(op1);
        //   ...

        tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector128_CreateScalarUnsafe, simdBaseJitType, 16);
        BlockRange().InsertAfter(op1, tmp1);
        LowerNode(tmp1);

        if ((simdBaseJitType != CORINFO_TYPE_DOUBLE) && comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
        {
            // We will be constructing the following parts:
            //   ...
            //           /--*  tmp1 simd16
            //   node  = *  HWINTRINSIC   simd16 T BroadcastScalarToVector128

            // This is roughly the following managed code:
            //   ...
            //   return Avx2.BroadcastScalarToVector128(tmp1);

            node->gtOp1 = tmp1;
            node->gtOp2 = nullptr;

            node->gtHWIntrinsicId = NI_AVX2_BroadcastScalarToVector128;
            return;
        }

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                {
                    // We will be constructing the following parts:
                    //   ...
                    //   tmp2 =    HWINTRINSIC   simd16 ubyte get_Zero
                    //         /--*  tmp1 simd16
                    //         +--*  tmp2 simd16
                    //   node = *  HWINTRINSIC   simd16 ubyte Shuffle

                    // This is roughly the following managed code:
                    //   ...
                    //   var tmp2 = Vector128<byte>.Zero;
                    //   return Ssse3.Shuffle(tmp1, tmp2);

                    tmp2 =
                        comp->gtNewSimdHWIntrinsicNode(simdType, NI_Vector128_get_Zero, CORINFO_TYPE_UBYTE, simdSize);
                    BlockRange().InsertAfter(tmp1, tmp2);
                    LowerNode(tmp2);

                    node->gtOp1 = tmp1;
                    node->gtOp2 = tmp2;

                    node->gtHWIntrinsicId = NI_SSSE3_Shuffle;
                    break;
                }

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   tmp1 = *  HWINTRINSIC   simd16 ubyte UnpackLow
                //   ...

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   tmp1 = Sse2.UnpackLow(tmp1, tmp2);
                //   ...

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_SSE2_UnpackLow, CORINFO_TYPE_UBYTE,
                                                      simdSize);
                BlockRange().InsertAfter(tmp2, tmp1);
                LowerNode(tmp1);

                FALLTHROUGH;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   tmp1 = *  HWINTRINSIC   simd16 ushort UnpackLow
                //   ...

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   tmp1 = Sse2.UnpackLow(tmp1, tmp2);
                //   ...

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_SSE2_UnpackLow, CORINFO_TYPE_USHORT,
                                                      simdSize);
                BlockRange().InsertAfter(tmp2, tmp1);
                LowerNode(tmp1);

                FALLTHROUGH;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                // We will be constructing the following parts:
                //   ...
                //   idx  =    CNS_INT       int    0
                //          /--*  tmp1 simd16
                //          +--*  idx  int
                //   node = *  HWINTRINSIC   simd16 uint Shuffle

                // This is roughly the following managed code:
                //   ...
                //   return Sse2.Shuffle(tmp1, 0x00);

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

                idx = comp->gtNewIconNode(0x00, TYP_INT);
                BlockRange().InsertAfter(tmp1, idx);

                node->gtOp1 = tmp1;
                node->gtOp2 = idx;

                node->gtHWIntrinsicId = NI_SSE2_Shuffle;
                node->SetSimdBaseJitType(CORINFO_TYPE_UINT);

                break;
            }

#if defined(TARGET_AMD64)
            case TYP_LONG:
            case TYP_ULONG:
            {
                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   node = *  HWINTRINSIC simd16 ulong UnpackLow

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   return Sse2.UnpackLow(tmp1, tmp2);

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                node->gtOp1 = tmp1;
                node->gtOp2 = tmp2;

                node->gtHWIntrinsicId = NI_SSE2_UnpackLow;
                break;
            }
#endif // TARGET_AMD64

            case TYP_FLOAT:
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX))
                {
                    // We will be constructing the following parts:
                    //   ...
                    //   idx  =    CNS_INT       int    0
                    //          /--*  tmp1 simd16
                    //          +--*  idx  int
                    //   node = *  HWINTRINSIC   simd16 float Permute

                    // This is roughly the following managed code:
                    //   ...
                    //   return Avx.Permute(tmp1, 0x00);

                    idx = comp->gtNewIconNode(0x00, TYP_INT);
                    BlockRange().InsertAfter(tmp1, idx);

                    node->gtOp1 = tmp1;
                    node->gtOp2 = idx;

                    node->gtHWIntrinsicId = NI_AVX_Permute;
                    break;
                }

                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //   idx  =    CNS_INT       int    0
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //          +--*  idx  int
                //   node = *  HWINTRINSIC   simd16 float Shuffle

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   return Sse.Shuffle(tmp1, tmp2, 0x00);

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE));

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                idx = comp->gtNewIconNode(0x00, TYP_INT);
                BlockRange().InsertAfter(tmp2, idx);

                node->gtOp1 = comp->gtNewArgList(tmp1, tmp2, idx);
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = NI_SSE_Shuffle;
                break;
            }

            case TYP_DOUBLE:
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE3))
                {
                    // We will be constructing the following parts:
                    //   ...
                    //          /--*  tmp1 simd16
                    //   node = *  HWINTRINSIC   simd16 double MoveAndDuplicate

                    // This is roughly the following managed code:
                    //   ...
                    //   return Sse3.MoveAndDuplicate(tmp1);

                    node->gtOp1 = tmp1;
                    node->gtOp2 = nullptr;

                    node->gtHWIntrinsicId = NI_SSE3_MoveAndDuplicate;
                    break;
                }

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   node = *  HWINTRINSIC   simd16 float MoveLowToHigh

                // This is roughly the following managed code:
                //   ...
                //   var tmp2 = tmp1;
                //   return Sse.MoveLowToHigh(tmp1, tmp2);

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                node->gtOp1 = tmp1;
                node->gtOp2 = tmp2;

                node->gtHWIntrinsicId = NI_SSE_MoveLowToHigh;
                node->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);

                break;
            }

            default:
            {
                unreached();
            }
        }

        return;
    }

    // We have the following (where simd is simd16 or simd32):
    //          /--*  op1 T
    //          +--*  ... T
    //          +--*  opN T
    //   node = *  HWINTRINSIC   simd   T Create

    if (intrinsicId == NI_Vector256_Create)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        // We will be constructing the following parts:
        //          /--*  op1 T
        //          +--*  ... T
        //   lo   = *  HWINTRINSIC   simd16 T Create
        //          /--*  ... T
        //          +--*  opN T
        //   hi   = *  HWINTRINSIC   simd16 T Create
        //   idx  =    CNS_INT       int    1
        //          /--*  lo   simd32
        //          +--*  hi   simd16
        //          +--*  idx  int
        //   node = *  HWINTRINSIC   simd32 T InsertVector128

        // This is roughly the following managed code:
        //   ...
        //   var lo   = Vector128.Create(op1, ...);
        //   var hi   = Vector128.Create(..., opN);
        //   return Avx.InsertVector128(lo, hi, 0x01);

        // Each Vector128.Create call gets half the operands. That is:
        //   lo = Vector128.Create(op1, op2);
        //   hi = Vector128.Create(op3, op4);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op3);
        //   hi = Vector128.Create(op4,  ..., op7);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op7);
        //   hi = Vector128.Create(op8,  ..., op15);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op15);
        //   hi = Vector128.Create(op16, ..., op31);

        unsigned halfArgCnt = argCnt / 2;
        assert((halfArgCnt * 2) == argCnt);

        argList = op1->AsArgList();

        for (unsigned i = 0; i < halfArgCnt; i++)
        {
            op2     = argList;
            argList = argList->Rest();
        }

        op2->AsArgList()->gtOp2 = nullptr;
        op2                     = argList;

        // The above for loop splits the operand count into exactly half.
        // Once it exits, op1 will point to op1 and op2 will point to the
        // last operand that will be passed to the first Vector128.Create
        // We will set its op2 to null, terminating the chain and then
        // assign  op2 to be argList, which is the first operand that will
        // get passed to the second Vector128.Create

        GenTree* lo = nullptr;
        GenTree* hi = nullptr;

        if (halfArgCnt == 2)
        {
            // The Vector256.Create calls that take 4 operands are special
            // because the half argument count is 2, which means we can't
            // actually use the GT_LIST anymore and need to pass them as
            // explicit operands instead.

            argList = op1->AsArgList();

            tmp1 = argList->Current();
            tmp2 = argList->Rest()->Current();

            lo = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, tmp2, NI_Vector128_Create, simdBaseJitType, 16);
            BlockRange().InsertAfter(tmp2, lo);
            LowerNode(lo);

            argList = op2->AsArgList();

            tmp1 = argList->Current();
            tmp2 = argList->Rest()->Current();

            hi = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, tmp2, NI_Vector128_Create, simdBaseJitType, 16);
            BlockRange().InsertAfter(tmp2, hi);
            LowerNode(hi);
        }
        else
        {
            // The rest of the Vector256.Create calls take at least 8 operands
            // and so the half count is at least 4 and we have to continue
            // passing around GT_LIST nodes in op1 with a null op2
            assert(halfArgCnt >= 4);

            tmp1 = op2->AsArgList()->Current();

            lo = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector128_Create, simdBaseJitType, 16);
            BlockRange().InsertBefore(tmp1, lo);
            LowerNode(lo);

            hi = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_Vector128_Create, simdBaseJitType, 16);
            BlockRange().InsertBefore(node, hi);
            LowerNode(hi);
        }

        idx = comp->gtNewIconNode(0x01, TYP_INT);
        BlockRange().InsertAfter(hi, idx);

        node->gtOp1 = comp->gtNewArgList(lo, hi, idx);
        node->gtOp2 = nullptr;

        node->gtHWIntrinsicId = NI_AVX_InsertVector128;
        return;
    }

    if (op1->OperIsList())
    {
        argList = op1->AsArgList();
        op1     = argList->Current();
        argList = argList->Rest();
    }

    // We will be constructing the following parts:
    //          /--*  op1  T
    //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
    //   ...

    // This is roughly the following managed code:
    //   var tmp1 = Vector128.CreateScalarUnsafe(op1);
    //   ...

    tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector128_CreateScalarUnsafe, simdBaseJitType, 16);
    BlockRange().InsertAfter(op1, tmp1);
    LowerNode(tmp1);

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        {
            unsigned       N            = 0;
            GenTree*       opN          = nullptr;
            NamedIntrinsic insIntrinsic = NI_Illegal;

            if ((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_USHORT))
            {
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));
                insIntrinsic = NI_SSE2_Insert;
            }
            else if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                insIntrinsic = NI_SSE41_Insert;
            }

            if (insIntrinsic != NI_Illegal)
            {
                for (N = 1; N < argCnt - 1; N++)
                {
                    // We will be constructing the following parts:
                    //   ...
                    //   idx  =    CNS_INT       int    N
                    //          /--*  tmp1 simd16
                    //          +--*  opN  T
                    //          +--*  idx  int
                    //   tmp1 = *  HWINTRINSIC   simd16 T Insert
                    //   ...

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp1 = Sse?.Insert(tmp1, opN, N);
                    //   ...

                    opN = argList->Current();

                    idx = comp->gtNewIconNode(N, TYP_INT);
                    BlockRange().InsertAfter(opN, idx);

                    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, opN, idx, insIntrinsic, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(idx, tmp1);
                    LowerNode(tmp1);

                    argList = argList->Rest();
                }

                assert(N == (argCnt - 1));

                // We will be constructing the following parts:
                //   idx  =    CNS_INT       int    N
                //          /--*  tmp1 simd16
                //          +--*  opN  T
                //          +--*  idx  int
                //   node = *  HWINTRINSIC   simd16 T Insert

                // This is roughly the following managed code:
                //   ...
                //   tmp1 = Sse?.Insert(tmp1, opN, N);
                //   ...

                opN = argList->Current();

                idx = comp->gtNewIconNode(N, TYP_INT);
                BlockRange().InsertAfter(opN, idx);

                node->gtOp1 = comp->gtNewArgList(tmp1, opN, idx);
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = insIntrinsic;
                break;
            }

            assert((simdBaseType != TYP_SHORT) && (simdBaseType != TYP_USHORT));
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

            GenTree* op[16];
            op[0] = tmp1;

            for (N = 1; N < argCnt; N++)
            {
                opN = argList->Current();

                op[N] = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, opN, NI_Vector128_CreateScalarUnsafe,
                                                       simdBaseJitType, 16);
                BlockRange().InsertAfter(opN, op[N]);
                LowerNode(op[N]);

                argList = argList->Rest();
            }
            assert(argList == nullptr);

            if ((simdBaseType == TYP_BYTE) || (simdBaseType == TYP_UBYTE))
            {
                for (N = 0; N < argCnt; N += 4)
                {
                    // We will be constructing the following parts:
                    //   ...
                    //          /--*  opN  T
                    //   opN  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                    //          /--*  opO  T
                    //   opO  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                    //          /--*  opN  simd16
                    //          +--*  opO  simd16
                    //   tmp1 = *  HWINTRINSIC   simd16 T UnpackLow
                    //          /--*  opP  T
                    //   opP  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                    //          /--*  opQ  T
                    //   opQ  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                    //          /--*  opP  simd16
                    //          +--*  opQ  simd16
                    //   tmp2 = *  HWINTRINSIC   simd16 T UnpackLow
                    //          /--*  tmp1 simd16
                    //          +--*  tmp2 simd16
                    //   tmp3  = *  HWINTRINSIC   simd16 T UnpackLow
                    //   ...

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp1 = Sse2.UnpackLow(opN, opO);
                    //   tmp2 = Sse2.UnpackLow(opP, opQ);
                    //   tmp3 = Sse2.UnpackLow(tmp1, tmp2);
                    //   ...

                    unsigned O = N + 1;
                    unsigned P = N + 2;
                    unsigned Q = N + 3;

                    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op[N], op[O], NI_SSE2_UnpackLow, CORINFO_TYPE_UBYTE,
                                                          simdSize);
                    BlockRange().InsertAfter(op[O], tmp1);
                    LowerNode(tmp1);

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, op[P], op[Q], NI_SSE2_UnpackLow, CORINFO_TYPE_UBYTE,
                                                          simdSize);
                    BlockRange().InsertAfter(op[Q], tmp2);
                    LowerNode(tmp2);

                    tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_SSE2_UnpackLow, CORINFO_TYPE_USHORT,
                                                          simdSize);
                    BlockRange().InsertAfter(tmp2, tmp3);
                    LowerNode(tmp3);

                    // This caches the result in index 0 through 3, depending on which
                    // loop iteration this is and allows the rest of the logic to be
                    // shared with the TYP_INT and TYP_UINT path.

                    op[N / 4] = tmp3;
                }
            }

            // We will be constructing the following parts:
            //   ...
            //          /--*  opN  T
            //   opN  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opO  T
            //   opO  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opN  simd16
            //          +--*  opO  simd16
            //   tmp1 = *  HWINTRINSIC   simd16 T UnpackLow
            //          /--*  opP  T
            //   opP  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opQ  T
            //   opQ  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opP  simd16
            //          +--*  opQ  simd16
            //   tmp2 = *  HWINTRINSIC   simd16 T UnpackLow
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   node = *  HWINTRINSIC   simd16 T UnpackLow

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Sse2.UnpackLow(opN, opO);
            //   tmp2 = Sse2.UnpackLow(opP, opQ);
            //   return Sse2.UnpackLow(tmp1, tmp2);

            tmp1 =
                comp->gtNewSimdHWIntrinsicNode(simdType, op[0], op[1], NI_SSE2_UnpackLow, CORINFO_TYPE_UINT, simdSize);
            BlockRange().InsertAfter(op[1], tmp1);
            LowerNode(tmp1);

            tmp2 =
                comp->gtNewSimdHWIntrinsicNode(simdType, op[2], op[3], NI_SSE2_UnpackLow, CORINFO_TYPE_UINT, simdSize);
            BlockRange().InsertAfter(op[3], tmp2);
            LowerNode(tmp2);

            node->gtOp1 = tmp1;
            node->gtOp2 = tmp2;

            node->gtHWIntrinsicId = NI_SSE2_UnpackLow;
            node->SetSimdBaseJitType(CORINFO_TYPE_ULONG);
            break;
        }

#if defined(TARGET_AMD64)
        case TYP_LONG:
        case TYP_ULONG:
        {
            if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41_X64))
            {
                // We will be constructing the following parts:
                //   ...
                //   idx  =    CNS_INT       int    1
                //          /--*  tmp1 simd16
                //          +--*  op2  T
                //          +--*  idx  int
                //   node = *  HWINTRINSIC   simd16 T Insert

                // This is roughly the following managed code:
                //   ...
                //   return Sse41.X64.Insert(tmp1, op2, 0x01);

                idx = comp->gtNewIconNode(0x01, TYP_INT);
                BlockRange().InsertBefore(node, idx);

                node->gtOp1 = comp->gtNewArgList(tmp1, op2, idx);
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = NI_SSE41_X64_Insert;
                break;
            }

            // We will be constructing the following parts:
            //   ...
            //          /--*  op2  T
            //   tmp2 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   node = *  HWINTRINSIC   simd16 T UnpackLow

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = Vector128.CreateScalarUnsafe(op2);
            //   return Sse2.UnpackLow(tmp1, tmp2);

            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

            tmp2 =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_Vector128_CreateScalarUnsafe, simdBaseJitType, 16);
            BlockRange().InsertAfter(op2, tmp2);
            LowerNode(tmp2);

            node->gtOp1 = tmp1;
            node->gtOp2 = tmp2;

            node->gtHWIntrinsicId = NI_SSE2_UnpackLow;
            break;
        }
#endif // TARGET_AMD64

        case TYP_FLOAT:
        {
            unsigned N   = 0;
            GenTree* opN = nullptr;

            if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                for (N = 1; N < argCnt - 1; N++)
                {
                    // We will be constructing the following parts:
                    //   ...
                    //
                    //          /--*  opN  T
                    //   tmp2 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                    //   idx  =    CNS_INT       int    N
                    //          /--*  tmp1 simd16
                    //          +--*  opN  T
                    //          +--*  idx  int
                    //   tmp1 = *  HWINTRINSIC   simd16 T Insert
                    //   ...

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp2 = Vector128.CreateScalarUnsafe(opN);
                    //   tmp1 = Sse41.Insert(tmp1, tmp2, N << 4);
                    //   ...

                    opN = argList->Current();

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, opN, NI_Vector128_CreateScalarUnsafe,
                                                          simdBaseJitType, 16);
                    BlockRange().InsertAfter(opN, tmp2);
                    LowerNode(tmp2);

                    idx = comp->gtNewIconNode(N << 4, TYP_INT);
                    BlockRange().InsertAfter(tmp2, idx);

                    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, idx, NI_SSE41_Insert, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(idx, tmp1);
                    LowerNode(tmp1);

                    argList = argList->Rest();
                }

                // We will be constructing the following parts:
                //   ...
                //
                //          /--*  opN  T
                //   tmp2 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
                //   idx  =    CNS_INT       int    N
                //          /--*  tmp1 simd16
                //          +--*  opN  T
                //          +--*  idx  int
                //   node = *  HWINTRINSIC   simd16 T Insert

                // This is roughly the following managed code:
                //   ...
                //   tmp2 = Vector128.CreateScalarUnsafe(opN);
                //   return Sse41.Insert(tmp1, tmp2, N << 4);

                opN = argList->Current();

                tmp2 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, opN, NI_Vector128_CreateScalarUnsafe, simdBaseJitType,
                                                      16);
                BlockRange().InsertAfter(opN, tmp2);
                LowerNode(tmp2);

                idx = comp->gtNewIconNode((argCnt - 1) << 4, TYP_INT);
                BlockRange().InsertAfter(tmp2, idx);

                node->gtOp1 = comp->gtNewArgList(tmp1, tmp2, idx);
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = NI_SSE41_Insert;
                break;
            }

            // We will be constructing the following parts:
            //   ...
            //          /--*  opN  T
            //   opN  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opO  T
            //   opO  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opN  simd16
            //          +--*  opO  simd16
            //   tmp1 = *  HWINTRINSIC   simd16 T UnpackLow
            //          /--*  opP  T
            //   opP  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opQ  T
            //   opQ  = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  opP  simd16
            //          +--*  opQ  simd16
            //   tmp2 = *  HWINTRINSIC   simd16 T UnpackLow
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   node = *  HWINTRINSIC   simd16 T MoveLowToHigh

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Sse.UnpackLow(opN, opO);
            //   tmp2 = Sse.UnpackLow(opP, opQ);
            //   return Sse.MoveLowToHigh(tmp1, tmp2);

            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE));

            GenTree* op[4];
            op[0] = tmp1;

            for (N = 1; N < argCnt; N++)
            {
                opN = argList->Current();

                op[N] = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, opN, NI_Vector128_CreateScalarUnsafe,
                                                       simdBaseJitType, 16);
                BlockRange().InsertAfter(opN, op[N]);
                LowerNode(op[N]);

                argList = argList->Rest();
            }
            assert(argList == nullptr);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op[0], op[1], NI_SSE_UnpackLow, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op[1], tmp1);
            LowerNode(tmp1);

            tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, op[2], op[3], NI_SSE_UnpackLow, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op[3], tmp2);
            LowerNode(tmp2);

            node->gtOp1 = tmp1;
            node->gtOp2 = tmp2;

            node->gtHWIntrinsicId = NI_SSE_MoveLowToHigh;
            break;
        }

        case TYP_DOUBLE:
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  op2  T
            //   tmp2 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   node = *  HWINTRINSIC   simd16 T MoveLowToHigh

            // This is roughly the following managed code:
            //   ...
            //   var tmp2 = Vector128.CreateScalarUnsafe(op2);
            //   return Sse.MoveLowToHigh(tmp1, tmp2);

            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

            tmp2 =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_Vector128_CreateScalarUnsafe, simdBaseJitType, 16);
            BlockRange().InsertAfter(op2, tmp2);
            LowerNode(tmp2);

            node->gtOp1 = tmp1;
            node->gtOp2 = tmp2;

            node->gtHWIntrinsicId = NI_SSE_MoveLowToHigh;
            node->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);

            break;
        }

        default:
        {
            unreached();
        }
    }
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicGetElement: Lowers a Vector128 or Vector256 GetElement call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicGetElement(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
    var_types      simdType        = node->gtType;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    assert(!varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    assert(op1 != nullptr);
    assert(op2 != nullptr);

    if (op1->OperIs(GT_IND))
    {
        // If the vector is already in memory, we force its
        // addr to be evaluated into a reg.  This would allow
        // us to generate [regBase] or [regBase + offset] or
        // [regBase + sizeOf(simdBaseType) * regIndex] to access
        // the required vector element directly from memory.
        //
        // TODO-CQ-XARCH: If addr of GT_IND is GT_LEA, we
        // might be able update GT_LEA to fold the regIndex
        // or offset in some cases.  Instead with this
        // approach we always evaluate GT_LEA into a reg.
        // Ideally, we should be able to lower GetItem intrinsic
        // into GT_IND(newAddr) where newAddr combines
        // the addr of the vector with the given index.
        op1->gtFlags |= GTF_IND_REQ_ADDR_IN_REG;
    }

    if (!op2->OperIsConst())
    {
        // We will specially handle GetElement in codegen when op2 isn't a constant
        return;
    }

    // We should have a bounds check inserted for any index outside the allowed range
    // but we need to generate some code anyways, and so we'll simply mask here for simplicity.

    ssize_t count = simdSize / genTypeSize(simdBaseType);
    ssize_t imm8  = static_cast<uint8_t>(op2->AsIntCon()->IconValue()) % count;

    assert(0 <= imm8 && imm8 < count);

    if (IsContainableMemoryOp(op1))
    {
        // We will specially handle GetElement in codegen when op1 is already in memory
        op2->AsIntCon()->SetIconValue(imm8);
        return;
    }

    switch (simdBaseType)
    {
        // Using software fallback if simdBaseType is not supported by hardware
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41));
            break;

        case TYP_LONG:
        case TYP_ULONG:
            // We either support TYP_LONG or we have been decomposed into two TYP_INT inserts
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41_X64));
            break;

        case TYP_DOUBLE:
        case TYP_FLOAT:
        case TYP_SHORT:
        case TYP_USHORT:
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));
            break;

        default:
            unreached();
    }

    // Remove the index node up front to simplify downstream logic
    BlockRange().Remove(op2);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;

    if (intrinsicId == NI_Vector256_GetElement)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        if (imm8 >= count / 2)
        {
            //   idx =    CNS_INT       int    1
            //         /--*  op1  simd32
            //         +--*  idx  int
            //   op1 = *  HWINTRINSIC   simd32 T ExtractVector128

            // This is roughly the following managed code:
            //   ...
            //   op1 = Avx.ExtractVector128(op1, 0x01);

            imm8 -= count / 2;

            idx = comp->gtNewIconNode(1);
            BlockRange().InsertBefore(node, idx);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, idx, NI_AVX_ExtractVector128, simdBaseJitType,
                                                  simdSize);
            BlockRange().InsertAfter(idx, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            //         /--*  op1  simd32
            //   op1 = *  HWINTRINSIC   simd32 T GetLower

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower();

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector256_GetLower, simdBaseJitType, 16);
            BlockRange().InsertBefore(node, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;
    }

    NamedIntrinsic resIntrinsic = NI_Illegal;

    if (imm8 == 0 && (genTypeSize(simdBaseType) >= 4))
    {
        switch (simdBaseType)
        {
            case TYP_LONG:
                resIntrinsic = NI_SSE2_X64_ConvertToInt64;
                break;

            case TYP_ULONG:
                resIntrinsic = NI_SSE2_X64_ConvertToUInt64;
                break;

            case TYP_INT:
                resIntrinsic = NI_SSE2_ConvertToInt32;
                break;

            case TYP_UINT:
                resIntrinsic = NI_SSE2_ConvertToUInt32;
                break;

            case TYP_FLOAT:
            case TYP_DOUBLE:
                resIntrinsic = NI_Vector128_ToScalar;
                break;

            default:
                unreached();
        }

        op2 = nullptr;
    }
    else
    {
        op2 = comp->gtNewIconNode(imm8);
        BlockRange().InsertBefore(node, op2);

        switch (simdBaseType)
        {
            case TYP_LONG:
            case TYP_ULONG:
            {
                resIntrinsic = NI_SSE41_X64_Extract;
                break;
            }

            case TYP_FLOAT:
            case TYP_DOUBLE:
            {
                // We specially handle float and double for more efficient codegen
                resIntrinsic = NI_Vector128_GetElement;
                break;
            }

            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_INT:
            case TYP_UINT:
            {
                resIntrinsic = NI_SSE41_Extract;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                resIntrinsic = NI_SSE2_Extract;
                break;
            }

            default:
                unreached();
        }
    }

    assert(resIntrinsic != NI_Illegal);

    node->gtHWIntrinsicId = resIntrinsic;
    node->gtOp1           = op1;
    node->gtOp2           = op2;
    node->SetSimdSize(16);

    if (!varTypeIsFloating(simdBaseType))
    {
        assert(node->gtHWIntrinsicId != intrinsicId);
        LowerNode(node);
    }

    if ((simdBaseType == TYP_BYTE) || (simdBaseType == TYP_SHORT))
    {
        // The intrinsic zeros the upper bits, so we need an explicit
        // cast to ensure the result is properly sign extended

        LIR::Use use;
        bool     foundUse = BlockRange().TryGetUse(node, &use);

        GenTreeCast* cast = comp->gtNewCastNode(TYP_INT, node, /* isUnsigned */ true, simdBaseType);
        BlockRange().InsertAfter(node, cast);

        if (foundUse)
        {
            use.ReplaceWith(comp, cast);
        }
        LowerNode(cast);
    }
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicWithElement: Lowers a Vector128 or Vector256 WithElement call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicWithElement(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
    var_types      simdType        = node->gtType;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = nullptr;
    GenTree* op3 = nullptr;

    assert(op1->OperIsList());
    assert(node->gtGetOp2() == nullptr);

    GenTreeArgList* argList = op1->AsArgList();

    op1     = argList->Current();
    argList = argList->Rest();

    op2     = argList->Current();
    argList = argList->Rest();

    op3     = argList->Current();
    argList = argList->Rest();

    assert(op1 != nullptr);
    assert(op2 != nullptr);
    assert(op3 != nullptr);

    assert(op2->OperIsConst());
    assert(argList == nullptr);

    ssize_t imm8       = op2->AsIntCon()->IconValue();
    ssize_t cachedImm8 = imm8;
    ssize_t count      = simdSize / genTypeSize(simdBaseType);

    assert(0 <= imm8 && imm8 < count);

    switch (simdBaseType)
    {
        // Using software fallback if simdBaseType is not supported by hardware
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41));
            break;

        case TYP_LONG:
        case TYP_ULONG:
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41_X64));
            break;

        case TYP_DOUBLE:
        case TYP_FLOAT:
        case TYP_SHORT:
        case TYP_USHORT:
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));
            break;

        default:
            unreached();
    }

    // Remove the index node up front to simplify downstream logic
    BlockRange().Remove(op2);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;
    GenTree* tmp3 = nullptr;
    GenTree* tmpv = nullptr;

    if (intrinsicId == NI_Vector256_WithElement)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        // We will be constructing the following parts:
        //   ...
        //          /--*  op1 simd32
        //          *  STORE_LCL_VAR simd32
        //   tmpv =    LCL_VAR       simd32
        //   op1  =    LCL_VAR       simd32

        node->gtOp1 = op1;
        LIR::Use op1Use(BlockRange(), &node->gtOp1, node);
        ReplaceWithLclVar(op1Use);
        tmpv = node->gtOp1;

        op1 = comp->gtClone(tmpv);
        BlockRange().InsertBefore(op3, op1);

        if (imm8 >= count / 2)
        {
            // We will be constructing the following parts:
            //   ...
            //   idx  =    CNS_INT       int    1
            //          /--*  op1   simd32
            //          +--*  idx   int
            //   op1  = *  HWINTRINSIC   simd32 T ExtractVector128

            // This is roughly the following managed code:
            //   ...
            //   op1   = Avx.ExtractVector128(op1, 0x01);

            imm8 -= count / 2;

            idx = comp->gtNewIconNode(1);
            BlockRange().InsertAfter(op1, idx);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, idx, NI_AVX_ExtractVector128, simdBaseJitType,
                                                  simdSize);
            BlockRange().InsertAfter(idx, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            // We will be constructing the following parts:
            //   ...
            //         /--*  op1  simd32
            //   op1 = *  HWINTRINSIC   simd32 T GetLower

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower();

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector256_GetLower, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op1, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;
    }

    NamedIntrinsic resIntrinsic = NI_Illegal;

    idx = comp->gtNewIconNode(imm8);
    BlockRange().InsertBefore(node, idx);

    switch (simdBaseType)
    {
        case TYP_LONG:
        case TYP_ULONG:
        {
            op2          = idx;
            resIntrinsic = NI_SSE41_X64_Insert;
            break;
        }

        case TYP_FLOAT:
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  op3   float
            //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Vector128.CreateScalarUnsafe(op3);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op3, NI_Vector128_CreateScalarUnsafe, CORINFO_TYPE_FLOAT,
                                                  16);
            BlockRange().InsertBefore(idx, tmp1);
            LowerNode(tmp1);

            if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                if (imm8 == 0)
                {
                    // We will be constructing the following parts:
                    //   ...
                    //          /--*  op1   simd16
                    //          +--*  op2   simd16
                    //   node = *  HWINTRINSIC   simd16 T MoveScalar

                    // This is roughly the following managed code:
                    //   ...
                    //   node  = Sse.MoveScalar(op1, op2);

                    op2          = tmp1;
                    resIntrinsic = NI_SSE_MoveScalar;
                }
                else
                {
                    // We will be constructing the following parts:
                    //   ...
                    //          /--*  op1 simd16
                    //          *  STORE_LCL_VAR simd16
                    //   op2  =    LCL_VAR       simd16
                    //   tmp2 =    LCL_VAR       simd16
                    //   idx  =    CNS_INT       int    0
                    //          /--*  tmp1   simd16
                    //          +--*  tmp2   simd16
                    //          +--*  idx    int
                    //   op1  = *  HWINTRINSIC   simd16 T Shuffle
                    //   idx  =    CNS_INT       int    226
                    //          /--*  op1   simd16
                    //          +--*  tmp2   simd16
                    //          +--*  idx    int
                    //   op1  = *  HWINTRINSIC   simd16 T Shuffle

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp2  = Sse.Shuffle(tmp1, op1,   0 or  48 or 32);
                    //   node  = Sse.Shuffle(tmp2, op1, 226 or 132 or 36);

                    node->gtOp1 = op1;
                    LIR::Use op1Use(BlockRange(), &node->gtOp1, node);
                    ReplaceWithLclVar(op1Use);
                    op2 = node->gtOp1;

                    tmp2 = comp->gtClone(op2);
                    BlockRange().InsertAfter(tmp1, tmp2);

                    ssize_t controlBits1;
                    ssize_t controlBits2;

                    switch (imm8)
                    {
                        case 1:
                        {
                            controlBits1 = 0;
                            controlBits2 = 226;
                            break;
                        }

                        case 2:
                        {
                            controlBits1 = 48;
                            controlBits2 = 132;
                            break;
                        }

                        case 3:
                        {
                            controlBits1 = 32;
                            controlBits2 = 36;
                            break;
                        }

                        default:
                            unreached();
                    }

                    idx = comp->gtNewIconNode(controlBits1);
                    BlockRange().InsertAfter(tmp2, idx);

                    if (imm8 == 1)
                    {
                        std::swap(tmp1, tmp2);
                    }

                    op1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp2, tmp1, idx, NI_SSE_Shuffle,
                                                         CORINFO_TYPE_FLOAT, 16);
                    BlockRange().InsertAfter(idx, op1);
                    LowerNode(op1);

                    idx = comp->gtNewIconNode(controlBits2);
                    BlockRange().InsertAfter(op1, idx);

                    op1          = comp->gtNewArgList(op1, op2, idx);
                    op2          = nullptr;
                    resIntrinsic = NI_SSE_Shuffle;
                }
                break;
            }
            else
            {
                op3 = tmp1;
                idx->AsIntCon()->SetIconValue(imm8 * 16);
                FALLTHROUGH;
            }
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
        {
            op1          = comp->gtNewArgList(op1, op3, idx);
            op2          = nullptr;
            resIntrinsic = NI_SSE41_Insert;
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            op1          = comp->gtNewArgList(op1, op3, idx);
            op2          = nullptr;
            resIntrinsic = NI_SSE2_Insert;
            break;
        }

        case TYP_DOUBLE:
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  op3   double
            //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Vector128.CreateScalarUnsafe(op3);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op3, NI_Vector128_CreateScalarUnsafe, CORINFO_TYPE_DOUBLE,
                                                  16);
            BlockRange().InsertBefore(idx, tmp1);
            LowerNode(tmp1);

            op2          = tmp1;
            resIntrinsic = (imm8 == 0) ? NI_SSE2_MoveScalar : NI_SSE2_UnpackLow;
            break;
        }

        default:
            unreached();
    }

    assert(resIntrinsic != NI_Illegal);

    if (tmpv != nullptr)
    {
        tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, resIntrinsic, simdBaseJitType, 16);
        BlockRange().InsertBefore(node, tmp1);
        LowerNode(tmp1);

        idx = comp->gtNewIconNode((cachedImm8 >= count / 2) ? 1 : 0);
        BlockRange().InsertAfter(tmp1, idx);

        op1          = comp->gtNewArgList(tmpv, tmp1, idx);
        op2          = nullptr;
        resIntrinsic = NI_AVX_InsertVector128;
    }

    node->gtHWIntrinsicId = resIntrinsic;
    node->gtOp1           = op1;
    node->gtOp2           = op2;

    assert(node->gtHWIntrinsicId != intrinsicId);
    LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector128 or Vector256 Dot call
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
    unsigned       simd16Count     = comp->getSIMDVectorLength(16, simdBaseType);

    assert((intrinsicId == NI_Vector128_Dot) || (intrinsicId == NI_Vector256_Dot));
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
    GenTree* tmp3 = nullptr;

    NamedIntrinsic multiply      = NI_Illegal;
    NamedIntrinsic horizontalAdd = NI_Illegal;
    NamedIntrinsic add           = NI_Illegal;
    NamedIntrinsic shuffle       = NI_Illegal;

    if (simdSize == 32)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX2));

        switch (simdBaseType)
        {
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            case TYP_UINT:
            {
                multiply      = NI_AVX2_MultiplyLow;
                horizontalAdd = NI_AVX2_HorizontalAdd;
                add           = NI_AVX2_Add;
                break;
            }

            case TYP_FLOAT:
            {
                // We will be constructing the following parts:
                //   idx  =    CNS_INT       int    0xF1
                //          /--*  op1  simd16
                //          +--*  op2  simd16
                //          +--*  idx  int
                //   tmp1 = *  HWINTRINSIC   simd16 T DotProduct
                //          /--*  tmp1 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp1 =    LCL_VAR       simd16
                //   tmp2 =    LCL_VAR       simd16
                //   idx  =    CNS_INT       int    0x01
                //          /--*  tmp2 simd16
                //          +--*  idx  int
                //   tmp2 = *  HWINTRINSIC   simd16 T ExtractVector128
                //          /--*  tmp1 simd16
                //          +--*  tmp2 simd16
                //   tmp3 = *  HWINTRINSIC   simd16 T Add
                //          /--*  tmp3 simd16
                //   node = *  HWINTRINSIC   simd16 T ToScalar

                // This is roughly the following managed code:
                //   var tmp1 = Avx.DotProduct(op1, op2, 0xFF);
                //   var tmp2 = Avx.ExtractVector128(tmp1, 0x01);
                //   var tmp3 = Sse.Add(tmp1, tmp2);
                //   return tmp3.ToScalar();

                idx = comp->gtNewIconNode(0xF1, TYP_INT);
                BlockRange().InsertBefore(node, idx);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_AVX_DotProduct, simdBaseJitType,
                                                      simdSize);
                BlockRange().InsertAfter(idx, tmp1);
                LowerNode(tmp1);

                node->gtOp1 = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->gtOp1;

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                idx = comp->gtNewIconNode(0x01, TYP_INT);
                BlockRange().InsertAfter(tmp2, idx);

                tmp2 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp2, idx, NI_AVX_ExtractVector128, simdBaseJitType,
                                                      simdSize);
                BlockRange().InsertAfter(idx, tmp2);
                LowerNode(tmp2);

                tmp3 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, tmp2, NI_SSE_Add, simdBaseJitType, 16);
                BlockRange().InsertAfter(tmp2, tmp3);
                LowerNode(tmp3);

                node->SetSimdSize(16);

                node->gtOp1 = tmp3;
                node->gtOp2 = nullptr;

                node->gtHWIntrinsicId = NI_Vector128_ToScalar;
                LowerNode(node);

                return;
            }

            case TYP_DOUBLE:
            {
                multiply      = NI_AVX_Multiply;
                horizontalAdd = NI_AVX_HorizontalAdd;
                add           = NI_AVX_Add;
                break;
            }

            default:
            {
                unreached();
            }
        }
    }
    else
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

        switch (simdBaseType)
        {
            case TYP_SHORT:
            case TYP_USHORT:
            {
                multiply      = NI_SSE2_MultiplyLow;
                horizontalAdd = NI_SSSE3_HorizontalAdd;
                add           = NI_SSE2_Add;

                if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                {
                    shuffle = NI_SSE2_ShuffleLow;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                multiply      = NI_SSE41_MultiplyLow;
                horizontalAdd = NI_SSSE3_HorizontalAdd;
                add           = NI_SSE2_Add;

                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41));
                break;
            }

            case TYP_FLOAT:
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    // We will be constructing the following parts:
                    //   idx  =    CNS_INT       int    0xFF
                    //          /--*  op1  simd16
                    //          +--*  op2  simd16
                    //          +--*  idx  int
                    //   tmp3 = *  HWINTRINSIC   simd16 T DotProduct
                    //          /--*  tmp3 simd16
                    //   node = *  HWINTRINSIC   simd16 T ToScalar

                    // This is roughly the following managed code:
                    //   var tmp3 = Avx.DotProduct(op1, op2, 0xFF);
                    //   return tmp3.ToScalar();

                    if (simdSize == 8)
                    {
                        idx = comp->gtNewIconNode(0x31, TYP_INT);
                    }
                    else if (simdSize == 12)
                    {
                        idx = comp->gtNewIconNode(0x71, TYP_INT);
                    }
                    else
                    {
                        assert(simdSize == 16);
                        idx = comp->gtNewIconNode(0xF1, TYP_INT);
                    }
                    BlockRange().InsertBefore(node, idx);

                    tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_SSE41_DotProduct, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(idx, tmp3);
                    LowerNode(tmp3);

                    node->gtOp1 = tmp3;
                    node->gtOp2 = nullptr;

                    node->gtHWIntrinsicId = NI_Vector128_ToScalar;
                    LowerNode(node);

                    return;
                }

                multiply      = NI_SSE_Multiply;
                horizontalAdd = NI_SSE3_HorizontalAdd;
                add           = NI_SSE_Add;

                if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSE3))
                {
                    shuffle = NI_SSE_Shuffle;
                }
                break;
            }

            case TYP_DOUBLE:
            {
                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    // We will be constructing the following parts:
                    //   idx  =    CNS_INT       int    0x31
                    //          /--*  op1  simd16
                    //          +--*  op2  simd16
                    //          +--*  idx  int
                    //   tmp3 = *  HWINTRINSIC   simd16 T DotProduct
                    //          /--*  tmp3 simd16
                    //   node = *  HWINTRINSIC   simd16 T ToScalar

                    // This is roughly the following managed code:
                    //   var tmp3 = Avx.DotProduct(op1, op2, 0x31);
                    //   return tmp3.ToScalar();

                    idx = comp->gtNewIconNode(0x31, TYP_INT);
                    BlockRange().InsertBefore(node, idx);

                    tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_SSE41_DotProduct, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(idx, tmp3);
                    LowerNode(tmp3);

                    node->gtOp1 = tmp3;
                    node->gtOp2 = nullptr;

                    node->gtHWIntrinsicId = NI_Vector128_ToScalar;
                    LowerNode(node);

                    return;
                }

                multiply      = NI_SSE2_Multiply;
                horizontalAdd = NI_SSE3_HorizontalAdd;
                add           = NI_SSE2_Add;

                if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSE3))
                {
                    shuffle = NI_SSE2_Shuffle;
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        if (simdSize == 8)
        {
            assert(simdBaseType == TYP_FLOAT);

            // If simdSize == 8 then we have only two elements, not the 4 that we got from getSIMDVectorLength,
            // which we gave a simdSize of 16. So, we set the simd16Count to 2 so that only 1 hadd will
            // be emitted rather than 2, so that the upper two elements will be ignored.

            simd16Count = 2;
        }
        else if (simdSize == 12)
        {
            assert(simdBaseType == TYP_FLOAT);

            // We will be constructing the following parts:
            //   ...
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    0
            //   tmp1 = *  HWINTRINSIC   simd16 T Create
            //          /--*  op2 simd16
            //          +--*  tmp1 simd16
            //   op1  = *  HWINTRINSIC   simd16 T And
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Vector128.Create(-1, -1, -1, 0);
            //   op1  = Sse.And(op1, tmp2);
            //   ...

            GenTree* cns0 = comp->gtNewIconNode(-1, TYP_INT);
            BlockRange().InsertAfter(op1, cns0);

            GenTree* cns1 = comp->gtNewIconNode(-1, TYP_INT);
            BlockRange().InsertAfter(cns0, cns1);

            GenTree* cns2 = comp->gtNewIconNode(-1, TYP_INT);
            BlockRange().InsertAfter(cns1, cns2);

            GenTree* cns3 = comp->gtNewIconNode(0, TYP_INT);
            BlockRange().InsertAfter(cns2, cns3);

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, cns0, cns1, cns2, cns3, NI_Vector128_Create,
                                                  CORINFO_TYPE_INT, 16);
            BlockRange().InsertAfter(cns3, tmp1);
            LowerNode(tmp1);

            op1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, tmp1, NI_SSE_And, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(tmp1, op1);
            LowerNode(op1);
        }
    }

    // We will be constructing the following parts:
    //          /--*  op1  simd16
    //          +--*  op2  simd16
    //   tmp1 = *  HWINTRINSIC   simd16 T Multiply
    //   ...

    // This is roughly the following managed code:
    //   var tmp1 = Isa.Multiply(op1, op2);
    //   ...

    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, multiply, simdBaseJitType, simdSize);
    BlockRange().InsertBefore(node, tmp1);
    LowerNode(tmp1);

    // HorizontalAdd combines pairs so we need log2(simd16Count) passes to sum all elements together.
    int haddCount = genLog2(simd16Count);

    for (int i = 0; i < haddCount; i++)
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
        //   tmp2 = tmp1;
        //   ...

        node->gtOp1 = tmp1;
        LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
        ReplaceWithLclVar(tmp1Use);
        tmp1 = node->gtOp1;

        tmp2 = comp->gtClone(tmp1);
        BlockRange().InsertAfter(tmp1, tmp2);

        if (shuffle == NI_Illegal)
        {
            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   tmp1 = *  HWINTRINSIC   simd16 T HorizontalAdd
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Isa.HorizontalAdd(tmp1, tmp2);
            //   ...

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, horizontalAdd, simdBaseJitType, simdSize);
        }
        else
        {
            int shuffleConst = 0x00;

            switch (i)
            {
                case 0:
                {
                    assert((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_USHORT) ||
                           varTypeIsFloating(simdBaseType));

                    // Adds (e0 + e1, e1 + e0, e2 + e3, e3 + e2), giving:
                    //   e0, e1, e2, e3 | e4, e5, e6, e7
                    //   e1, e0, e3, e2 | e5, e4, e7, e6
                    //   ...

                    shuffleConst = 0xB1;
                    break;
                }

                case 1:
                {
                    assert((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_USHORT) || (simdBaseType == TYP_FLOAT));

                    // Adds (e0 + e2, e1 + e3, e2 + e0, e3 + e1), giving:
                    //   ...
                    //   e2, e3, e0, e1 | e6, e7, e4, e5
                    //   e3, e2, e1, e0 | e7, e6, e5, e4

                    shuffleConst = 0x4E;
                    break;
                }

                case 2:
                {
                    assert((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_USHORT));

                    // Adds (e0 + e4, e1 + e5, e2 + e6, e3 + e7), giving:
                    //   ...
                    //   e4, e5, e6, e7 | e0, e1, e2, e3
                    //   e5, e4, e7, e6 | e1, e0, e3, e2
                    //   e6, e7, e4, e5 | e2, e3, e0, e1
                    //   e7, e6, e5, e4 | e3, e2, e1, e0

                    shuffleConst = 0x4D;
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            idx = comp->gtNewIconNode(shuffleConst, TYP_INT);
            BlockRange().InsertAfter(tmp2, idx);

            if (varTypeIsFloating(simdBaseType))
            {
                // We will be constructing the following parts:
                //   ...
                //          /--*  tmp2 simd16
                //          *  STORE_LCL_VAR simd16
                //   tmp2 =    LCL_VAR       simd16
                //   tmp3 =    LCL_VAR       simd16
                //   idx  =    CNS_INT       int    shuffleConst
                //          /--*  tmp2 simd16
                //          +--*  tmp3 simd16
                //          +--*  idx  simd16
                //   tmp2 = *  HWINTRINSIC   simd16 T Shuffle
                //   ...

                // This is roughly the following managed code:
                //   ...
                //   tmp3 = tmp2;
                //   tmp2 = Isa.Shuffle(tmp2, tmp3, shuffleConst);
                //   ...

                node->gtOp1 = tmp2;
                LIR::Use tmp2Use(BlockRange(), &node->gtOp1, node);
                ReplaceWithLclVar(tmp2Use);
                tmp2 = node->gtOp1;

                tmp3 = comp->gtClone(tmp2);
                BlockRange().InsertAfter(tmp2, tmp3);

                tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, tmp3, idx, shuffle, simdBaseJitType, simdSize);
            }
            else
            {
                assert((simdBaseType == TYP_SHORT) || (simdBaseType == TYP_USHORT));

                if (i < 2)
                {
                    // We will be constructing the following parts:
                    //   ...
                    //   idx  =    CNS_INT       int    shuffleConst
                    //          /--*  tmp2 simd16
                    //          +--*  idx  simd16
                    //   tmp2 = *  HWINTRINSIC   simd16 T ShuffleLow
                    //   idx  =    CNS_INT       int    shuffleConst
                    //          /--*  tmp2 simd16
                    //          +--*  idx  simd16
                    //   tmp2 = *  HWINTRINSIC   simd16 T ShuffleHigh
                    //   ...

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp2 = Isa.Shuffle(tmp1, shuffleConst);
                    //   ...

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, idx, NI_SSE2_ShuffleLow, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(idx, tmp2);
                    LowerNode(tmp2);

                    idx = comp->gtNewIconNode(shuffleConst, TYP_INT);
                    BlockRange().InsertAfter(tmp2, idx);

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, idx, NI_SSE2_ShuffleHigh, simdBaseJitType,
                                                          simdSize);
                }
                else
                {
                    assert(i == 2);

                    // We will be constructing the following parts:
                    //   ...
                    //   idx  =    CNS_INT       int    shuffleConst
                    //          /--*  tmp2 simd16
                    //          +--*  idx  simd16
                    //   tmp2 = *  HWINTRINSIC   simd16 T ShuffleLow
                    //   ...

                    // This is roughly the following managed code:
                    //   ...
                    //   tmp2 = Isa.Shuffle(tmp1, shuffleConst);
                    //   ...

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, idx, NI_SSE2_Shuffle, CORINFO_TYPE_INT,
                                                          simdSize);
                }
            }

            BlockRange().InsertAfter(idx, tmp2);
            LowerNode(tmp2);

            // We will be constructing the following parts:
            //   ...
            //          /--*  tmp1 simd16
            //          +--*  tmp2 simd16
            //   tmp1 = *  HWINTRINSIC   simd16 T Add
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Isa.Add(tmp1, tmp2);
            //   ...

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, add, simdBaseJitType, simdSize);
        }

        BlockRange().InsertAfter(tmp2, tmp1);
        LowerNode(tmp1);
    }

    if (simdSize == 32)
    {
        // We will be constructing the following parts:
        //   ...
        //          /--*  tmp1 simd16
        //          *  STORE_LCL_VAR simd16
        //   tmp1 =    LCL_VAR       simd16
        //   tmp2 =    LCL_VAR       simd16
        //   idx  =    CNS_INT       int    0x01
        //          /--*  tmp2 simd16
        //          +--*  idx  int
        //   tmp2 = *  HWINTRINSIC   simd16 T ExtractVector128
        //          /--*  tmp1 simd16
        //          +--*  tmp2 simd16
        //   tmp1 = *  HWINTRINSIC   simd16 T Add
        //   ...

        // This is roughly the following managed code:
        //   ...
        //   var tmp2 = tmp1;
        //       tmp2 = Avx.ExtractVector128(tmp2, 0x01);
        //   var tmp1 = Isa.Add(tmp1, tmp2);
        //   ...

        node->gtOp1 = tmp1;
        LIR::Use tmp1Use(BlockRange(), &node->gtOp1, node);
        ReplaceWithLclVar(tmp1Use);
        tmp1 = node->gtOp1;

        tmp2 = comp->gtClone(tmp1);
        BlockRange().InsertAfter(tmp1, tmp2);

        idx = comp->gtNewIconNode(0x01, TYP_INT);
        BlockRange().InsertAfter(tmp2, idx);

        tmp2 =
            comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp2, idx, NI_AVX_ExtractVector128, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(idx, tmp2);
        LowerNode(tmp2);

        tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, tmp2, add, simdBaseJitType, 16);
        BlockRange().InsertAfter(tmp2, tmp1);
        LowerNode(tmp1);

        node->SetSimdSize(16);
    }

    // We will be constructing the following parts:
    //   ...
    //          /--*  tmp1 simd16
    //   node = *  HWINTRINSIC   simd16 T ToScalar

    // This is roughly the following managed code:
    //   ...
    //   return tmp1.ToScalar();

    node->gtOp1 = tmp1;
    node->gtOp2 = nullptr;

    node->gtHWIntrinsicId = NI_Vector128_ToScalar;
    LowerNode(node);

    return;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicToScalar: Lowers a Vector128 or Vector256 ToScalar call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicToScalar(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->gtHWIntrinsicId;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    assert((intrinsicId == NI_Vector128_ToScalar) || (intrinsicId == NI_Vector256_ToScalar));
    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_SHORT:
        case TYP_INT:
        {
            node->gtType = TYP_INT;
            node->SetSimdBaseJitType(CORINFO_TYPE_INT);
            node->gtHWIntrinsicId = NI_SSE2_ConvertToInt32;
            break;
        }

        case TYP_UBYTE:
        case TYP_USHORT:
        case TYP_UINT:
        {
            node->gtType = TYP_UINT;
            node->SetSimdBaseJitType(CORINFO_TYPE_UINT);
            node->gtHWIntrinsicId = NI_SSE2_ConvertToUInt32;
            break;
        }

#if defined(TARGET_AMD64)
        case TYP_LONG:
        {
            node->gtHWIntrinsicId = NI_SSE2_X64_ConvertToInt64;
            break;
        }

        case TYP_ULONG:
        {
            node->gtHWIntrinsicId = NI_SSE2_X64_ConvertToUInt64;
            break;
        }
#endif // TARGET_AMD64

        case TYP_FLOAT:
        case TYP_DOUBLE:
        {
            ContainCheckHWIntrinsic(node);
            return;
        }

        default:
        {
            unreached();
        }
    }

    LowerNode(node);

    if (genTypeSize(simdBaseType) < 4)
    {
        LIR::Use use;
        bool     foundUse = BlockRange().TryGetUse(node, &use);

        GenTreeCast* cast = comp->gtNewCastNode(simdBaseType, node, node->IsUnsigned(), simdBaseType);
        BlockRange().InsertAfter(node, cast);

        if (foundUse)
        {
            use.ReplaceWith(comp, cast);
        }
        LowerNode(cast);
    }
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
    if (!indirDst->OperIs(GT_LEA, GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_CLS_VAR_ADDR, GT_CNS_INT))
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
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
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

#ifdef TARGET_X86
        // Fast tail calls aren't currently supported on x86, but if they ever are, the code
        // below that handles indirect VSD calls will need to be fixed.
        assert(!call->IsFastTailCall() || !call->IsVirtualStub());
#endif // TARGET_X86
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
#ifdef TARGET_X86
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
#endif // TARGET_X86
                if (ctrlExpr->isIndir())
            {
                // We may have cases where we have set a register target on the ctrlExpr, but if it
                // contained we must clear it.
                ctrlExpr->SetRegNum(REG_NA);
                MakeSrcContained(call, ctrlExpr);
            }
        }
    }

    for (GenTreeCall::Use& use : call->Args())
    {
        if (use.GetNode()->OperIs(GT_PUTARG_STK))
        {
            LowerPutArgStk(use.GetNode()->AsPutArgStk());
        }
    }

    for (GenTreeCall::Use& use : call->LateArgs())
    {
        if (use.GetNode()->OperIs(GT_PUTARG_STK))
        {
            LowerPutArgStk(use.GetNode()->AsPutArgStk());
        }
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
    else if (addr->OperIs(GT_CLS_VAR_ADDR, GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_CLS_VAR_ADDR turns into a constant.
        // - GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR is a stack addr mode.

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
        // Note that LowerVirtualStubCall() sets addr->GetRegNum() to VirtualStubParam.reg and Lowering::doPhase()
        // sets destination candidates on such nodes and resets addr->GetRegNum() to REG_NA.
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
    GenTree* src = node->AsOp()->gtOp2;
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
#if defined(TARGET_X86)
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

    GenTree* op1 = node->AsOp()->gtOp1;
    GenTree* op2 = node->AsOp()->gtOp2;

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
#if defined(TARGET_X86)
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
#ifdef TARGET_X86
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
#ifdef TARGET_X86
    GenTree* source = node->gtOp1;
    if (node->OperIsShiftLong())
    {
        assert(source->OperGet() == GT_LONG);
        MakeSrcContained(node, source);
    }
#endif // !TARGET_X86

    GenTree* shiftBy = node->gtOp2;
    if (IsContainableImmed(node, shiftBy) && (shiftBy->AsIntConCommon()->IconValue() <= 255) &&
        (shiftBy->AsIntConCommon()->IconValue() >= 0))
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
        assert(!op1->IsCnsIntOrI());
        if (storeLoc->TypeIs(TYP_SIMD12) && op1->IsSIMDZero() && varDsc->lvDoNotEnregister)
        {
            // For a SIMD12 store we can zero from integer registers more easily.
            MakeSrcContained(storeLoc, op1);
            GenTree* constNode = op1->gtGetOp1();
            assert(constNode->OperIsConst());
            constNode->ClearContained();
            constNode->gtType = TYP_INT;
            constNode->SetOper(GT_CNS_INT);
        }
        return;
    }
#endif // FEATURE_SIMD

    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    var_types type = varDsc->GetRegisterType(storeLoc);
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(type)))
    {
        MakeSrcContained(storeLoc, op1);
    }
#ifdef TARGET_X86
    else if (op1->OperGet() == GT_LONG)
    {
        MakeSrcContained(storeLoc, op1);
    }
#endif // TARGET_X86
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
#if !defined(TARGET_64BIT)
    if (varTypeIsLong(srcType))
    {
        noway_assert(castOp->OperGet() == GT_LONG);
        castOp->SetContained();
    }
#endif // !defined(TARGET_64BIT)
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

    GenTree*  op1     = cmp->AsOp()->gtOp1;
    GenTree*  op2     = cmp->AsOp()->gtOp2;
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
        assert(indirCandidateChild->OperIs(GT_LCL_VAR, GT_LCL_VAR_ADDR, GT_CLS_VAR_ADDR, GT_CNS_INT));

        // If it is a GT_LCL_VAR, it still needs the reg to hold the address.
        // We would still need a reg for GT_CNS_INT if it doesn't fit within addressing mode base.
        // For GT_CLS_VAR_ADDR, we don't need a reg to hold the address, because field address value is known at jit
        // time. Also, we don't need a reg for GT_CLS_VAR_ADDR.
        if (indirCandidateChild->OperIs(GT_LCL_VAR_ADDR, GT_CLS_VAR_ADDR))
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

    NamedIntrinsic intrinsicName = node->AsIntrinsic()->gtIntrinsicName;

    if ((intrinsicName == NI_System_Math_Ceiling) || (intrinsicName == NI_System_Math_Floor) ||
        (intrinsicName == NI_System_Math_Round) || (intrinsicName == NI_System_Math_Sqrt))
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
        case SIMDIntrinsicInit:
        {
            GenTree* op1 = simdNode->AsOp()->gtOp1;
#ifndef TARGET_64BIT
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
#endif // !TARGET_64BIT
                if (op1->IsFPZero() || op1->IsIntegralConst(0) ||
                    (varTypeIsIntegral(simdNode->GetSimdBaseType()) && op1->IsIntegralConst(-1)))
            {
                MakeSrcContained(simdNode, op1);
            }
            else if ((comp->getSIMDSupportLevel() == SIMD_AVX2_Supported) &&
                     ((simdNode->GetSimdSize() == 16) || (simdNode->GetSimdSize() == 32)))
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

        case SIMDIntrinsicShuffleSSE2:
            // Second operand is an integer constant and marked as contained.
            assert(simdNode->AsOp()->gtOp2->IsCnsIntOrI());
            MakeSrcContained(simdNode, simdNode->AsOp()->gtOp2);
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
    // This will generally be an explicit LoadAligned instruction and is false for
    // machines with VEX support when minOpts is enabled. This is because there is
    // currently no way to guarantee that the address read from will always be
    // aligned and we want to assert that the address is aligned when optimizations
    // aren't enabled. However, when optimizations are enabled, we want to allow
    // folding of memory operands as it produces better codegen and allows simpler
    // coding patterns on the managed side.
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

                    if (!comp->canUseVexEncoding())
                    {
                        // Most instructions under the non-VEX encoding require aligned operands.
                        // Those used for Sse2.ConvertToVector128Double (CVTDQ2PD and CVTPS2PD)
                        // are exceptions and don't fail for unaligned inputs.

                        supportsAlignedSIMDLoads   = (containingIntrinsicId != NI_SSE2_ConvertToVector128Double);
                        supportsUnalignedSIMDLoads = !supportsAlignedSIMDLoads;
                    }
                    else
                    {
                        supportsAlignedSIMDLoads   = !comp->opts.MinOpts();
                        supportsUnalignedSIMDLoads = true;
                    }

                    supportsGeneralLoads = supportsUnalignedSIMDLoads;
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

                    supportsAlignedSIMDLoads   = !comp->canUseVexEncoding() || !comp->opts.MinOpts();
                    supportsUnalignedSIMDLoads = comp->canUseVexEncoding();
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads;

                    break;
                }

                case NI_SSE2_Insert:
                case NI_SSE41_Insert:
                case NI_SSE41_X64_Insert:
                {
                    if (containingNode->GetSimdBaseType() == TYP_FLOAT)
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

                    const unsigned expectedSize = genTypeSize(containingNode->GetSimdBaseType());
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

                    const unsigned expectedSize = genTypeSize(genActualType(containingNode->GetSimdBaseType()));
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

                    const unsigned expectedSize = genTypeSize(genActualType(containingNode->GetSimdBaseType()));
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
                var_types op2Type = containingNode->GetSimdBaseType();
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

        case NI_SSE_LoadVector128:
        case NI_SSE2_LoadVector128:
        case NI_AVX_LoadVector256:
        {
            return supportsUnalignedSIMDLoads;
        }

        case NI_AVX_ExtractVector128:
        case NI_AVX2_ExtractVector128:
        {
            return false;
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
//     node - The hardware intrinsic node
//     addr - The address node to try contain
//
void Lowering::ContainCheckHWIntrinsicAddr(GenTreeHWIntrinsic* node, GenTree* addr)
{
    assert((addr->TypeGet() == TYP_I_IMPL) || (addr->TypeGet() == TYP_BYREF));
    TryCreateAddrMode(addr, true);
    if ((addr->OperIs(GT_CLS_VAR_ADDR, GT_LCL_VAR_ADDR, GT_LCL_FLD_ADDR, GT_LEA) ||
         (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp))) &&
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
    NamedIntrinsic      intrinsicId     = node->gtHWIntrinsicId;
    HWIntrinsicCategory category        = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                 numArgs         = HWIntrinsicInfo::lookupNumArgs(node);
    CorInfoType         simdBaseJitType = node->GetSimdBaseJitType();
    var_types           simdBaseType    = node->GetSimdBaseType();

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

    if (HWIntrinsicInfo::lookupCategory(intrinsicId) == HW_Category_IMM)
    {
        GenTree* lastOp = HWIntrinsicInfo::lookupLastOp(node);
        assert(lastOp != nullptr);

        if (HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && lastOp->IsCnsIntOrI())
        {
            MakeSrcContained(node, lastOp);
        }
    }

    if ((node->GetSimdSize() == 8) || (node->GetSimdSize() == 12))
    {
        // We want to handle GetElement still for Vector2/3
        if ((intrinsicId != NI_Vector128_GetElement) && (intrinsicId != NI_Vector256_GetElement))
        {
            // TODO-XArch-CQ: Ideally we would key this off of the size containingNode
            // expects vs the size node actually is or would be if spilled to the stack
            return;
        }
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
                ContainCheckHWIntrinsicAddr(node, node->gtGetOp1());
                break;

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
                        if (varTypeIsIntegral(simdBaseType))
                        {
                            // TODO-XARCH-CQ: These intrinsics are "ins reg/mem, xmm" and don't
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
                        if (!varTypeIsSIMD(op1->gtType))
                        {
                            ContainCheckHWIntrinsicAddr(node, node->gtGetOp1());
                            return;
                        }
                        break;

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
                    if ((intrinsicId == NI_AVX_MaskLoad) || (intrinsicId == NI_AVX2_MaskLoad))
                    {
                        ContainCheckHWIntrinsicAddr(node, node->gtGetOp1());
                    }
                    else
                    {
                        ContainCheckHWIntrinsicAddr(node, node->gtGetOp2());
                    }
                    break;

                case HW_Category_MemoryStore:
                    ContainCheckHWIntrinsicAddr(node, node->gtGetOp1());

                    if (((intrinsicId == NI_SSE_Store) || (intrinsicId == NI_SSE2_Store)) && op2->OperIsHWIntrinsic() &&
                        ((op2->AsHWIntrinsic()->gtHWIntrinsicId == NI_AVX_ExtractVector128) ||
                         (op2->AsHWIntrinsic()->gtHWIntrinsicId == NI_AVX2_ExtractVector128)) &&
                        op2->gtGetOp2()->IsIntegralConst())
                    {
                        MakeSrcContained(node, op2);
                    }
                    break;

                case HW_Category_SimpleSIMD:
                case HW_Category_SIMDScalar:
                case HW_Category_Scalar:
                {
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
                        //                https://github.com/dotnet/runtime/issues/6358
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
                        case NI_SSE2_Extract:
                        case NI_AVX_ExtractVector128:
                        case NI_AVX2_ExtractVector128:
                        {
                            // TODO-XARCH-CQ: These intrinsics are "ins reg/mem, xmm, imm8" and don't
                            // currently support containment.
                            break;
                        }

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
                        case NI_AVX2_Shuffle:
                        case NI_AVX2_ShuffleHigh:
                        case NI_AVX2_ShuffleLow:
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

                        case NI_SSE41_Extract:
                        case NI_SSE41_X64_Extract:
                        {
                            assert(!varTypeIsFloating(simdBaseType));
                            // TODO-XARCH-CQ: These intrinsics are "ins reg/mem, xmm, imm8" and don't
                            // currently support containment.
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

                        case NI_SSE2_ShiftLeftLogical128BitLane:
                        case NI_SSE2_ShiftRightLogical128BitLane:
                        case NI_AVX2_ShiftLeftLogical128BitLane:
                        case NI_AVX2_ShiftRightLogical128BitLane:
                        {
#if DEBUG
                            // These intrinsics should have been marked contained by the general-purpose handling
                            // earlier in the method.

                            GenTree* lastOp = HWIntrinsicInfo::lookupLastOp(node);
                            assert(lastOp != nullptr);

                            if (HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && lastOp->IsCnsIntOrI())
                            {
                                assert(lastOp->isContained());
                            }
#endif

                            break;
                        }

                        default:
                        {
                            assert(!"Unhandled containment for binary hardware intrinsic with immediate operand");
                            break;
                        }
                    }

                    break;
                }

                case HW_Category_Helper:
                {
                    // We don't currently have any IMM intrinsics which are also commutative
                    assert(!isCommutative);

                    switch (intrinsicId)
                    {
                        case NI_Vector128_GetElement:
                        case NI_Vector256_GetElement:
                        {
                            if (op1->OperIs(GT_IND))
                            {
                                assert((op1->gtFlags & GTF_IND_REQ_ADDR_IN_REG) != 0);
                                op1->AsIndir()->Addr()->ClearContained();
                            }

                            if (op2->OperIsConst())
                            {
                                MakeSrcContained(node, op2);
                            }

                            if (IsContainableMemoryOp(op1))
                            {
                                MakeSrcContained(node, op1);

                                if (op1->OperIs(GT_IND))
                                {
                                    op1->AsIndir()->Addr()->ClearContained();
                                }
                            }
                            break;
                        }

                        default:
                        {
                            assert(!"Unhandled containment for helper binary hardware intrinsic");
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
                    ContainCheckHWIntrinsicAddr(node, node->gtGetOp1()->AsOp()->gtGetOp1());
                    break;

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
                            //                https://github.com/dotnet/runtime/issues/6358

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
                    break;
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
                        case NI_AVX_InsertVector128:
                        case NI_AVX_Permute2x128:
                        case NI_AVX_Shuffle:
                        case NI_AVX2_AlignRight:
                        case NI_AVX2_Blend:
                        case NI_AVX2_InsertVector128:
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
                            assert(!"Unhandled containment for ternary hardware intrinsic with immediate operand");
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

#endif // TARGET_XARCH
