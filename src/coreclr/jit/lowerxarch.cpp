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
//    - Widening some small stores.
//
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    // Most small locals (the exception is dependently promoted fields) have 4 byte wide stack slots, so
    // we can widen the store, if profitable. The widening is only (largely) profitable for 2 byte stores.
    // We could widen bytes too but that would only be better when the constant is zero and reused, which
    // we presume is not common enough.
    //
    if (storeLoc->OperIs(GT_STORE_LCL_VAR) && (genTypeSize(storeLoc) == 2) && storeLoc->Data()->IsCnsIntOrI())
    {
        if (!comp->lvaGetDesc(storeLoc)->lvIsStructField)
        {
            storeLoc->gtType = TYP_INT;
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
    // Mark all GT_STOREIND nodes to indicate that it is not known
    // whether it represents a RMW memory op.
    node->SetRMWStatusDefault();

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

    // Optimization: do not unnecessarily zero-extend the result of setcc.
    if (varTypeIsByte(node) && (node->Data()->OperIsCompare() || node->Data()->OperIs(GT_SETCC)))
    {
        node->Data()->ChangeType(TYP_BYTE);
    }
    ContainCheckStoreIndir(node);

#if defined(FEATURE_HW_INTRINSICS)
    if (comp->IsBaselineVector512IsaSupportedOpportunistically() ||
        comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
    {
        if (!node->Data()->OperIs(GT_CNS_VEC))
        {
            return;
        }

        if (!node->Data()->AsVecCon()->TypeIs(TYP_SIMD32, TYP_SIMD64))
        {
            return;
        }
        if (node->Data()->AsVecCon()->IsAllBitsSet() || node->Data()->AsVecCon()->IsZero())
        {
            // To avoid some unexpected regression, this optimization only applies to non-all 1/0 constant vectors.
            return;
        }

        TryCompressConstVecData(node);
    }
#endif
}

//----------------------------------------------------------------------------------------------
// Lowering::TryLowerMulWithConstant:
//    Lowers a tree MUL(X, CNS) to LSH(X, CNS_SHIFT)
//    or
//    Lowers a tree MUL(X, CNS) to SUB(LSH(X, CNS_SHIFT), X)
//    or
//    Lowers a tree MUL(X, CNS) to ADD(LSH(X, CNS_SHIFT), X)
//
// Arguments:
//    node - GT_MUL node of integral type
//
// Return Value:
//    Returns the replacement node if one is created else nullptr indicating no replacement
//
// Notes:
//    Performs containment checks on the replacement node if one is created
GenTree* Lowering::TryLowerMulWithConstant(GenTreeOp* node)
{
    assert(node->OperIs(GT_MUL));

    // Do not do these optimizations when min-opts enabled.
    if (comp->opts.MinOpts())
        return nullptr;

    if (!varTypeIsIntegral(node))
        return nullptr;

    if (node->gtOverflow())
        return nullptr;

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    if (op1->isContained() || op2->isContained())
        return nullptr;

    if (!op2->IsCnsIntOrI())
        return nullptr;

    GenTreeIntConCommon* cns    = op2->AsIntConCommon();
    ssize_t              cnsVal = cns->IconValue();

    // Use GT_LEA if cnsVal is 3, 5, or 9.
    // These are handled in codegen.
    if (cnsVal == 3 || cnsVal == 5 || cnsVal == 9)
        return nullptr;

    // Use GT_LSH if cnsVal is a power of two.
    if (isPow2(cnsVal))
    {
        // Use shift for constant multiply when legal
        unsigned int shiftAmount = genLog2(static_cast<uint64_t>(static_cast<size_t>(cnsVal)));

        cns->SetIconValue(shiftAmount);
        node->ChangeOper(GT_LSH);

        ContainCheckShiftRotate(node);

        return node;
    }

// We do not do this optimization in X86 as it is not recommended.
#if TARGET_X86
    return nullptr;
#endif // TARGET_X86

    ssize_t cnsValPlusOne  = cnsVal + 1;
    ssize_t cnsValMinusOne = cnsVal - 1;

    bool useSub = isPow2(cnsValPlusOne);

    if (!useSub && !isPow2(cnsValMinusOne))
        return nullptr;

    LIR::Use op1Use(BlockRange(), &node->gtOp1, node);
    op1 = ReplaceWithLclVar(op1Use);

    if (useSub)
    {
        cnsVal = cnsValPlusOne;
        node->ChangeOper(GT_SUB);
    }
    else
    {
        cnsVal = cnsValMinusOne;
        node->ChangeOper(GT_ADD);
    }

    unsigned int shiftAmount = genLog2(static_cast<uint64_t>(static_cast<size_t>(cnsVal)));
    cns->SetIconValue(shiftAmount);

    node->gtOp1 = comp->gtNewOperNode(GT_LSH, node->gtType, op1, cns);
    node->gtOp2 = comp->gtClone(op1);

    BlockRange().Remove(op1);
    BlockRange().Remove(cns);
    BlockRange().InsertBefore(node, node->gtGetOp2());
    BlockRange().InsertBefore(node, cns);
    BlockRange().InsertBefore(node, op1);
    BlockRange().InsertBefore(node, node->gtGetOp1());

    ContainCheckBinary(node);
    ContainCheckShiftRotate(node->gtGetOp1()->AsOp());

    return node;
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL/GT_MULHI/GT_MUL_LONG node.
//
// Currently only performs containment checks.
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

    if (mul->OperIs(GT_MUL))
    {
        GenTree* replacementNode = TryLowerMulWithConstant(mul);
        if (replacementNode != nullptr)
        {
            return replacementNode->gtNext;
        }
    }

    ContainCheckMul(mul);

    return mul->gtNext;
}

//------------------------------------------------------------------------
// LowerBinaryArithmetic: lowers the given binary arithmetic node.
//
// Recognizes opportunities for using target-independent "combined" nodes
// Performs containment checks.
//
// Arguments:
//    node - the arithmetic node to lower
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerBinaryArithmetic(GenTreeOp* binOp)
{
#ifdef FEATURE_HW_INTRINSICS
    if (comp->opts.OptimizationEnabled() && varTypeIsIntegral(binOp))
    {
        if (binOp->OperIs(GT_AND))
        {
            GenTree* replacementNode = TryLowerAndOpToAndNot(binOp);
            if (replacementNode != nullptr)
            {
                return replacementNode->gtNext;
            }

            replacementNode = TryLowerAndOpToResetLowestSetBit(binOp);
            if (replacementNode != nullptr)
            {
                return replacementNode->gtNext;
            }

            replacementNode = TryLowerAndOpToExtractLowestSetBit(binOp);
            if (replacementNode != nullptr)
            {
                return replacementNode->gtNext;
            }
        }
        else if (binOp->OperIs(GT_XOR))
        {
            GenTree* replacementNode = TryLowerXorOpToGetMaskUpToLowestSetBit(binOp);
            if (replacementNode != nullptr)
            {
                return replacementNode->gtNext;
            }
        }
    }
#endif

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
    TryCreateAddrMode(blkNode->Addr(), false, blkNode);

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

        if (!blkNode->OperIs(GT_STORE_DYN_BLK) && (size <= comp->getUnrollThreshold(Compiler::UnrollKind::Memset)))
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
                // The fill value of an initblk is interpreted to hold a
                // value of (unsigned int8) however a constant of any size
                // may practically reside on the evaluation stack. So extract
                // the lower byte out of the initVal constant and replicate
                // it to a larger constant whose size is sufficient to support
                // the largest width store of the desired inline expansion.

                ssize_t fill = src->AsIntCon()->IconValue() & 0xFF;

                const bool canUseSimd = !blkNode->IsOnHeapAndContainsReferences() && comp->IsBaselineSimdIsaSupported();
                if (size > comp->getUnrollThreshold(Compiler::UnrollKind::Memset, canUseSimd))
                {
                    // It turns out we can't use SIMD so the default threshold is too big
                    goto TOO_BIG_TO_UNROLL;
                }
                if (canUseSimd && (size >= XMM_REGSIZE_BYTES))
                {
                    // We're going to use SIMD (and only SIMD - we don't want to occupy a GPR register
                    // with a fill value just to handle the remainder when we can do that with
                    // an overlapped SIMD load).
                    src->SetContained();
                }
                else if (fill == 0)
                {
                    // Leave as is - zero shouldn't be contained when we don't use SIMD.
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

                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;
                src->AsIntCon()->SetIconValue(fill);

                ContainBlockStoreAddress(blkNode, size, dstAddr, nullptr);
            }
        }
        else
        {
        TOO_BIG_TO_UNROLL:
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

        if (src->OperIs(GT_LCL_VAR))
        {
            // TODO-1stClassStructs: for now we can't work with STORE_BLOCK source in register.
            const unsigned srcLclNum = src->AsLclVar()->GetLclNum();
            comp->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DoNotEnregisterReason::StoreBlkSrc));
        }

        ClassLayout* layout               = blkNode->GetLayout();
        bool         doCpObj              = !blkNode->OperIs(GT_STORE_DYN_BLK) && layout->HasGCPtr();
        unsigned     copyBlockUnrollLimit = comp->getUnrollThreshold(Compiler::UnrollKind::Memcpy, false);

#ifndef JIT32_GCENCODER
        if (doCpObj && (size <= copyBlockUnrollLimit))
        {
            // No write barriers are needed on the stack.
            // If the layout contains a byref, then we know it must live on the stack.
            if (dstAddr->OperIs(GT_LCL_ADDR) || layout->HasGCByRef())
            {
                // If the size is small enough to unroll then we need to mark the block as non-interruptible
                // to actually allow unrolling. The generated code does not report GC references loaded in the
                // temporary register(s) used for copying. This is not supported for the JIT32_GCENCODER.
                doCpObj                  = false;
                blkNode->gtBlkOpGcUnsafe = true;
            }
        }
#endif

        if (doCpObj)
        {
            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));

            // If we have a long enough sequence of slots that do not require write barriers then
            // we can use REP MOVSD/Q instead of a sequence of MOVSD/Q instructions. According to the
            // Intel Manual, the sweet spot for small structs is between 4 to 12 slots of size where
            // the entire operation takes 20 cycles and encodes in 5 bytes (loading RCX and REP MOVSD/Q).
            unsigned nonGCSlots = 0;

            if (dstAddr->OperIs(GT_LCL_ADDR))
            {
                // If the destination is on the stack then no write barriers are needed.
                nonGCSlots = layout->GetSlotCount();
            }
            else
            {
                // Otherwise a write barrier is needed for every GC pointer in the layout
                // so we need to check if there's a long enough sequence of non-GC slots.
                unsigned slots = layout->GetSlotCount();
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
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindCpObjRepInstr;
            }
            else
            {
                blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindCpObjUnroll;
            }
        }
        else if (blkNode->OperIs(GT_STORE_BLK) &&
                 (size <= comp->getUnrollThreshold(Compiler::UnrollKind::Memcpy, !layout->HasGCPtr())))
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

            if (src->OperIs(GT_IND))
            {
                ContainBlockStoreAddress(blkNode, size, src->AsIndir()->Addr(), src->AsIndir());
            }

            ContainBlockStoreAddress(blkNode, size, dstAddr, nullptr);
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

    assert(blkNode->gtBlkOpKind != GenTreeBlk::BlkOpKindInvalid);

#ifndef TARGET_X86
    if ((MIN_ARG_AREA_FOR_CALL > 0) && (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper))
    {
        RequireOutgoingArgSpace(blkNode, MIN_ARG_AREA_FOR_CALL);
    }
#endif
}

//------------------------------------------------------------------------
// ContainBlockStoreAddress: Attempt to contain an address used by an unrolled block store.
//
// Arguments:
//    blkNode - the block store node
//    size - the block size
//    addr - the address node to try to contain
//    addrParent - the parent of addr, in case this is checking containment of the source address.
//
void Lowering::ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr, GenTree* addrParent)
{
    assert(blkNode->OperIs(GT_STORE_BLK) && (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll));
    assert(size < INT32_MAX);

    if (addr->OperIs(GT_LCL_ADDR))
    {
        addr->SetContained();
        return;
    }

    if (!addr->OperIsAddrMode() && !TryCreateAddrMode(addr, true, blkNode))
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
    if (!IsInvariantInRange(addrMode, blkNode, addrParent))
    {
        return;
    }

    addrMode->SetContained();
}

//------------------------------------------------------------------------
// LowerPutArgStkOrSplit: Lower a GT_PUTARG_STK/GT_PUTARG_SPLIT.
//
// Arguments:
//    putArgNode - The node of interest
//
void Lowering::LowerPutArgStkOrSplit(GenTreePutArgStk* putArgNode)
{
    assert(putArgNode->OperIs(GT_PUTARG_STK)); // No split args on XArch.
    LowerPutArgStk(putArgNode);
}

//------------------------------------------------------------------------
// LowerPutArgStk: Lower a GT_PUTARG_STK.
//
// Arguments:
//    putArgStk - The node of interest
//
void Lowering::LowerPutArgStk(GenTreePutArgStk* putArgStk)
{
    GenTree* src        = putArgStk->Data();
    bool     srcIsLocal = src->OperIsLocalRead();

    if (src->OperIs(GT_FIELD_LIST))
    {
#ifdef TARGET_X86
        GenTreeFieldList* fieldList = src->AsFieldList();

        // The code generator will push these fields in reverse order by offset. Reorder the list here s.t. the order
        // of uses is visible to LSRA.
        assert(fieldList->Uses().IsSorted());
        fieldList->Uses().Reverse();

        // Containment checks.
        for (GenTreeFieldList::Use& use : fieldList->Uses())
        {
            GenTree* const  fieldNode = use.GetNode();
            const var_types fieldType = use.GetType();
            assert(!fieldNode->TypeIs(TYP_LONG));

            // For x86 we must mark all integral fields as contained or reg-optional, and handle them
            // accordingly in code generation, since we may have up to 8 fields, which cannot all be in
            // registers to be consumed atomically by the call.
            if (varTypeIsIntegralOrI(fieldNode))
            {
                if (IsContainableImmed(putArgStk, fieldNode))
                {
                    MakeSrcContained(putArgStk, fieldNode);
                }
                else if (IsContainableMemoryOp(fieldNode) && IsSafeToContainMem(putArgStk, fieldNode))
                {
                    MakeSrcContained(putArgStk, fieldNode);
                }
                else
                {
                    // For the case where we cannot directly push the value, if we run out of registers,
                    // it would be better to defer computation until we are pushing the arguments rather
                    // than spilling, but this situation is not all that common, as most cases of FIELD_LIST
                    // are promoted structs, which do not not have a large number of fields, and of those
                    // most are lclVars or copy-propagated constants.

                    fieldNode->SetRegOptional();
                }
            }
        }

        // Set the copy kind.
        // TODO-X86-CQ: Even if we are using push, if there are contiguous floating point fields, we should
        // adjust the stack once for those fields. The latter is really best done in code generation, but
        // this tuning should probably be undertaken as a whole.
        // Also, if there are  floating point fields, it may be better to use the "Unroll" mode
        // of copying the struct as a whole, if the fields are not register candidates.
        putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
#endif // TARGET_X86
        return;
    }

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    if (src->TypeIs(TYP_STRUCT))
    {
        assert(src->OperIs(GT_BLK) || src->OperIsLocalRead());

        ClassLayout* layout  = src->GetLayout(comp);
        var_types    regType = layout->GetRegisterType();

        if (regType == TYP_UNDEF)
        {
            // In case of a CpBlk we could use a helper call. In case of putarg_stk we
            // can't do that since the helper call could kill some already set up outgoing args.
            // TODO-Amd64-Unix: converge the code for putarg_stk with cpyblk/cpyobj.
            // The cpyXXXX code is rather complex and this could cause it to be more complex, but
            // it might be the right thing to do.

            // If possible, widen the load, this results in more compact code.
            unsigned loadSize = srcIsLocal ? roundUp(layout->GetSize(), TARGET_POINTER_SIZE) : layout->GetSize();
            putArgStk->SetArgLoadSize(loadSize);

            // TODO-X86-CQ: The helper call either is not supported on x86 or required more work
            // (I don't know which).
            if (!layout->HasGCPtr())
            {
#ifdef TARGET_X86
                // Codegen for "Kind::Push" will always load bytes in TARGET_POINTER_SIZE
                // chunks. As such, we'll only use this path for correctly-sized sources.
                if ((loadSize < XMM_REGSIZE_BYTES) && ((loadSize % TARGET_POINTER_SIZE) == 0))
                {
                    putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
                }
                else
#endif // TARGET_X86
                    if (loadSize <= comp->getUnrollThreshold(Compiler::UnrollKind::Memcpy))
                {
                    putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Unroll;
                }
                else
                {
                    putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::RepInstr;
                }
            }
            else // There are GC pointers.
            {
#ifdef TARGET_X86
                // On x86, we must use `push` to store GC references to the stack in order for the emitter to
                // properly update the function's GC info. These `putargstk` nodes will generate a sequence of
                // `push` instructions.
                putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::Push;
#else  // !TARGET_X86
                putArgStk->gtPutArgStkKind = GenTreePutArgStk::Kind::PartialRepInstr;
#endif // !TARGET_X86
            }

            if (src->OperIs(GT_LCL_VAR))
            {
                comp->lvaSetVarDoNotEnregister(src->AsLclVar()->GetLclNum()
                                                   DEBUGARG(DoNotEnregisterReason::IsStructArg));
            }

            // Always mark the OBJ/LCL_VAR/LCL_FLD as contained trees.
            MakeSrcContained(putArgStk, src);
        }
        else
        {
            // The ABI allows upper bits of small struct args to remain undefined,
            // so if possible, widen the load to avoid the sign/zero-extension.
            if (varTypeIsSmall(regType) && srcIsLocal)
            {
                assert(genTypeSize(TYP_INT) <= putArgStk->GetStackByteSize());
                regType = TYP_INT;
            }

            src->ChangeType(regType);

            if (src->OperIs(GT_BLK))
            {
                src->SetOper(GT_IND);
                LowerIndir(src->AsIndir());
            }
        }
    }

    if (src->TypeIs(TYP_STRUCT))
    {
        return;
    }
#endif // FEATURE_PUT_STRUCT_ARG_STK

    assert(!src->TypeIs(TYP_STRUCT));

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
#ifdef TARGET_X86
    else if (genTypeSize(src) == TARGET_POINTER_SIZE)
    {
        // We can use "src" directly from memory with "push [mem]".
        TryMakeSrcContainedOrRegOptional(putArgStk, src);
    }
#endif // TARGET_X86
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
        srcType = varTypeToUnsigned(srcType);
    }

    // We should never see the following casts as they are expected to be lowered
    // appropriately or converted into helper calls by front-end.
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
        assert(castToType != TYP_FLOAT || comp->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
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

    assert((HWIntrinsicInfo::lookupNumArgs(newIntrinsicId) == 2) || (newIntrinsicId == NI_AVX512F_KORTEST));
    node->ChangeHWIntrinsicId(newIntrinsicId);
    node->gtType = TYP_VOID;
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
        {
            // If we need the Carry flag then we can't swap operands.
            canSwapOperands = (cc == nullptr) || cc->gtCondition.Is(GenCondition::EQ, GenCondition::NE);
            break;
        }

        case NI_AVX512F_KORTEST:
        case NI_AVX512F_KTEST:
        {
            // No containment support, so no reason to swap operands
            canSwapOperands = false;
            break;
        }

        default:
            unreached();
    }

    if (canSwapOperands)
    {
        bool op1SupportsRegOptional = false;
        bool op2SupportsRegOptional = false;

        if (!IsContainableHWIntrinsicOp(node, node->Op(2), &op2SupportsRegOptional) &&
            IsContainableHWIntrinsicOp(node, node->Op(1), &op1SupportsRegOptional))
        {
            // Swap operands if op2 cannot be contained but op1 can.
            swapOperands = true;
        }
    }

    if (swapOperands)
    {
        std::swap(node->Op(1), node->Op(2));

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
    assert(node->GetHWIntrinsicId() == NI_FMA_MultiplyAddScalar);
    GenTreeHWIntrinsic* createScalarOps[3];

    for (size_t i = 1; i <= 3; i++)
    {
        GenTree* arg = node->Op(i);

        if (!arg->OperIsHWIntrinsic() || (arg->AsHWIntrinsic()->GetHWIntrinsicId() != NI_Vector128_CreateScalarUnsafe))
        {
            return;
        }

        createScalarOps[i - 1] = arg->AsHWIntrinsic();
    }

    GenTree* argX = createScalarOps[0]->Op(1);
    GenTree* argY = createScalarOps[1]->Op(1);
    GenTree* argZ = createScalarOps[2]->Op(1);

    const bool negMul = argX->OperIs(GT_NEG) != argY->OperIs(GT_NEG);
    if (argX->OperIs(GT_NEG))
    {
        createScalarOps[0]->Op(1) = argX->gtGetOp1();
        BlockRange().Remove(argX);

        createScalarOps[0]->Op(1)->ClearContained();
        ContainCheckHWIntrinsic(createScalarOps[0]);
    }
    if (argY->OperIs(GT_NEG))
    {
        createScalarOps[1]->Op(1) = argY->gtGetOp1();
        BlockRange().Remove(argY);

        createScalarOps[1]->Op(1)->ClearContained();
        ContainCheckHWIntrinsic(createScalarOps[1]);
    }
    if (argZ->OperIs(GT_NEG))
    {
        createScalarOps[2]->Op(1) = argZ->gtGetOp1();
        BlockRange().Remove(argZ);

        createScalarOps[2]->Op(1)->ClearContained();
        ContainCheckHWIntrinsic(createScalarOps[2]);

        node->ChangeHWIntrinsicId(negMul ? NI_FMA_MultiplySubtractNegatedScalar : NI_FMA_MultiplySubtractScalar);
    }
    else
    {
        node->ChangeHWIntrinsicId(negMul ? NI_FMA_MultiplyAddNegatedScalar : NI_FMA_MultiplyAddScalar);
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
    if (node->TypeGet() == TYP_SIMD12)
    {
        // GT_HWINTRINSIC node requiring to produce TYP_SIMD12 in fact
        // produces a TYP_SIMD16 result
        node->gtType = TYP_SIMD16;
    }

    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();

    switch (intrinsicId)
    {
        case NI_Vector128_ConditionalSelect:
        case NI_Vector256_ConditionalSelect:
        case NI_Vector512_ConditionalSelect:
        {
            return LowerHWIntrinsicCndSel(node);
        }

        case NI_Vector128_Create:
        case NI_Vector256_Create:
        case NI_Vector512_Create:
        case NI_Vector128_CreateScalar:
        case NI_Vector256_CreateScalar:
        case NI_Vector512_CreateScalar:
        {
            // We don't directly support the Vector128.Create or Vector256.Create methods in codegen
            // and instead lower them to other intrinsic nodes in LowerHWIntrinsicCreate so we expect
            // that the node is modified to either not be a HWIntrinsic node or that it is no longer
            // the same intrinsic as when it came in. In the case of Vector256.Create, we may lower
            // it into 2x Vector128.Create intrinsics which themselves are also lowered into other
            // intrinsics that are not Vector*.Create

            return LowerHWIntrinsicCreate(node);
        }

        case NI_Vector128_Dot:
        case NI_Vector256_Dot:
        {
            return LowerHWIntrinsicDot(node);
        }

        case NI_Vector128_GetElement:
        case NI_Vector256_GetElement:
        case NI_Vector512_GetElement:
        {
            return LowerHWIntrinsicGetElement(node);
        }

        case NI_Vector256_GetUpper:
        {
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
            var_types simdBaseType = node->GetSimdBaseType();

            if (varTypeIsFloating(simdBaseType) || !comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                intrinsicId = NI_AVX_ExtractVector128;
            }
            else
            {
                intrinsicId = NI_AVX2_ExtractVector128;
            }

            GenTree* op1 = node->Op(1);

            GenTree* op2 = comp->gtNewIconNode(1);
            BlockRange().InsertBefore(node, op2);
            LowerNode(op2);

            node->ResetHWIntrinsicId(intrinsicId, comp, op1, op2);
            break;
        }

        case NI_Vector512_GetUpper:
        {
            assert(comp->IsBaselineVector512IsaSupportedDebugOnly());
            var_types simdBaseType = node->GetSimdBaseType();

            intrinsicId = NI_AVX512F_ExtractVector256;

            if ((genTypeSize(simdBaseType) == 4) && !comp->compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
            {
                intrinsicId = NI_AVX512DQ_ExtractVector256;
            }

            GenTree* op1 = node->Op(1);

            GenTree* op2 = comp->gtNewIconNode(1);
            BlockRange().InsertBefore(node, op2);
            LowerNode(op2);

            node->ResetHWIntrinsicId(intrinsicId, comp, op1, op2);
            break;
        }

        case NI_Vector128_WithElement:
        case NI_Vector256_WithElement:
        case NI_Vector512_WithElement:
        {
            return LowerHWIntrinsicWithElement(node);
        }

        case NI_Vector256_WithLower:
        case NI_Vector256_WithUpper:
        {
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
            var_types simdBaseType = node->GetSimdBaseType();
            int       index        = (intrinsicId == NI_Vector256_WithUpper) ? 1 : 0;

            if (varTypeIsFloating(simdBaseType) || !comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                intrinsicId = NI_AVX_InsertVector128;
            }
            else
            {
                intrinsicId = NI_AVX2_InsertVector128;
            }

            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);

            GenTree* op3 = comp->gtNewIconNode(index);
            BlockRange().InsertBefore(node, op3);
            LowerNode(op3);

            node->ResetHWIntrinsicId(intrinsicId, comp, op1, op2, op3);
            break;
        }

        case NI_Vector512_WithLower:
        case NI_Vector512_WithUpper:
        {
            assert(comp->IsBaselineVector512IsaSupportedDebugOnly());
            var_types simdBaseType = node->GetSimdBaseType();
            int       index        = (intrinsicId == NI_Vector512_WithUpper) ? 1 : 0;

            intrinsicId = NI_AVX512F_InsertVector256;

            if ((genTypeSize(simdBaseType) == 4) && !comp->compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
            {
                intrinsicId = NI_AVX512DQ_InsertVector256;
            }

            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);

            GenTree* op3 = comp->gtNewIconNode(index);
            BlockRange().InsertBefore(node, op3);
            LowerNode(op3);

            node->ResetHWIntrinsicId(intrinsicId, comp, op1, op2, op3);
            break;
        }

        case NI_Vector128_op_Equality:
        case NI_Vector256_op_Equality:
        case NI_Vector512_op_Equality:
        {
            return LowerHWIntrinsicCmpOp(node, GT_EQ);
        }

        case NI_Vector128_op_Inequality:
        case NI_Vector256_op_Inequality:
        case NI_Vector512_op_Inequality:
        {
            return LowerHWIntrinsicCmpOp(node, GT_NE);
        }

        case NI_AVX512F_CompareEqualMask:
        case NI_AVX512F_CompareNotEqualMask:
        {
            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);

            if (!varTypeIsFloating(node->GetSimdBaseType()) && op2->IsVectorZero())
            {
                NamedIntrinsic testIntrinsicId;

                if (intrinsicId == NI_AVX512F_CompareEqualMask)
                {
                    // We have `CompareEqual(x, Zero)` where a given element
                    // equaling zero returns 1. We can therefore use `vptestnm(x, x)`
                    // since it does `(x & x) == 0`, thus giving us `1` if zero and `0`
                    // if non-zero

                    testIntrinsicId = NI_AVX512F_PTESTNM;
                }
                else
                {
                    // We have `CompareNotEqual(x, Zero)` where a given element
                    // equaling zero returns 0. We can therefore use `vptestm(x, x)`
                    // since it does `(x & x) != 0`, thus giving us `1` if non-zero and `0`
                    // if zero

                    assert(intrinsicId == NI_AVX512F_CompareNotEqualMask);
                    testIntrinsicId = NI_AVX512F_PTESTM;
                }

                node->Op(1) = op1;
                BlockRange().Remove(op2);

                LIR::Use op1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(op1Use);
                op1 = node->Op(1);

                op2 = comp->gtClone(op1);
                BlockRange().InsertAfter(op1, op2);
                node->Op(2) = op2;

                node->ChangeHWIntrinsicId(testIntrinsicId);
                return LowerNode(node);
            }
            break;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        {
            return LowerHWIntrinsicToScalar(node);
        }

        case NI_SSE41_Extract:
        {
            if (varTypeIsFloating(node->GetSimdBaseType()))
            {
                assert(node->GetSimdBaseType() == TYP_FLOAT);
                assert(node->GetSimdSize() == 16);

                GenTree* op2 = node->Op(2);

                if (!op2->OperIsConst())
                {
                    // Extract allows the full range while GetElement only allows
                    // 0-3, so we need to mask the index here so codegen works.

                    GenTree* msk = comp->gtNewIconNode(3, TYP_INT);
                    BlockRange().InsertAfter(op2, msk);

                    GenTree* tmp = comp->gtNewOperNode(GT_AND, TYP_INT, op2, msk);
                    BlockRange().InsertAfter(msk, tmp);
                    LowerNode(tmp);

                    node->Op(2) = tmp;
                }

                node->ChangeHWIntrinsicId(NI_Vector128_GetElement);
                return LowerNode(node);
            }
            break;
        }

        case NI_SSE2_Insert:
        case NI_SSE41_Insert:
        case NI_SSE41_X64_Insert:
        {
            assert(node->GetOperandCount() == 3);

            var_types simdBaseType = node->GetSimdBaseType();

            // Insert takes either a 32-bit register or a memory operand.
            // In either case, only SimdBaseType bits are read and so
            // widening or narrowing the operand may be unnecessary and it
            // can just be used directly.

            node->Op(2) = TryRemoveCastIfPresent(simdBaseType, node->Op(2));

            if (simdBaseType != TYP_FLOAT)
            {
                break;
            }
            assert(intrinsicId == NI_SSE41_Insert);

            // We have Sse41.Insert in which case we can specially handle
            // a couple of interesting scenarios involving chains of Inserts
            // where one of them involves inserting zero
            //
            // Given Sse41.Insert has an index:
            //  * Bits 0-3: zmask
            //  * Bits 4-5: count_d
            //  * Bits 6-7: count_s (register form only)
            //
            // Where zmask specifies which elements to zero
            // Where count_d specifies the destination index the value is being inserted to
            // Where count_s specifies the source index of the value being inserted
            //
            // We can recognize  `Insert(Insert(vector, zero, index1), value, index2)` and
            // transform it into just `Insert(vector, value, index)`. This is because we
            // can remove the inner insert and update the relevant index fields.
            //
            // We can likewise recognize `Insert(Insert(vector, value, index1), zero, index2)`
            // and do a similar transformation.

            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);
            GenTree* op3 = node->Op(3);

            bool op1IsVectorZero = op1->IsVectorZero();
            bool op2IsVectorZero = op2->IsVectorZero();

            if (op1IsVectorZero && op2IsVectorZero)
            {
                // We need to change op1's type to the return type of the node in this case.
                // We have to do this because we are going to propagate the constant up.
                op1->ChangeType(node->TypeGet());

                // Ensure the upper values are zero by zero-initialization.
                op1->AsVecCon()->gtSimdVal = {};

                // While this case is unlikely, we'll handle it here to simplify some
                // of the logic that exists below. Effectively `Insert(zero, zero, idx)`
                // is always going to produce zero, so we'll just replace ourselves with
                // zero. This ensures we don't need to handle a case where op2 is zero
                // but not contained.

                GenTree* nextNode = node->gtNext;

                LIR::Use use;

                if (BlockRange().TryGetUse(node, &use))
                {
                    use.ReplaceWith(op1);
                }
                else
                {
                    op1->SetUnusedValue();
                }

                BlockRange().Remove(op2);
                op3->SetUnusedValue();
                BlockRange().Remove(node);

                return nextNode;
            }

            if (!op3->IsCnsIntOrI())
            {
                // Nothing to do if op3 isn't a constant
                break;
            }

            ssize_t ival = op3->AsIntConCommon()->IconValue();

            ssize_t zmask   = (ival & 0x0F);
            ssize_t count_d = (ival & 0x30) >> 4;
            ssize_t count_s = (ival & 0xC0) >> 6;

            if (op1IsVectorZero)
            {
                // When op1 is zero, we can modify the mask to zero
                // everything except for the element we're inserting

                zmask |= ~(ssize_t(1) << count_d);
                zmask &= 0x0F;

                ival = (count_s << 6) | (count_d << 4) | (zmask);
                op3->AsIntConCommon()->SetIconValue(ival);
            }
            else if (op2IsVectorZero)
            {
                // When op2 is zero, we can modify the mask to
                // directly zero the element we're inserting

                zmask |= (ssize_t(1) << count_d);
                zmask &= 0x0F;

                ival = (count_s << 6) | (count_d << 4) | (zmask);
                op3->AsIntConCommon()->SetIconValue(ival);
            }

            if (zmask == 0x0F)
            {
                // This is another unlikely case, we'll handle it here to simplify some
                // of the logic that exists below. In this case, the zmask says all entries
                // should be zeroed out, so we'll just replace ourselves with zero.

                GenTree* nextNode = node->gtNext;

                LIR::Use use;

                if (BlockRange().TryGetUse(node, &use))
                {
                    GenTree* zeroNode = comp->gtNewZeroConNode(TYP_SIMD16);
                    BlockRange().InsertBefore(node, zeroNode);
                    use.ReplaceWith(zeroNode);
                }
                else
                {
                    // We're an unused zero constant node, so don't both creating
                    // a new node for something that will never be consumed
                }

                op1->SetUnusedValue();
                op2->SetUnusedValue();
                op3->SetUnusedValue();
                BlockRange().Remove(node);

                return nextNode;
            }

            if (!op1->OperIsHWIntrinsic())
            {
                // Nothing to do if op1 isn't an intrinsic
                break;
            }

            GenTreeHWIntrinsic* op1Intrinsic = op1->AsHWIntrinsic();

            if ((op1Intrinsic->GetHWIntrinsicId() != NI_SSE41_Insert) || (op1Intrinsic->GetSimdBaseType() != TYP_FLOAT))
            {
                // Nothing to do if op1 isn't a float32 Sse41.Insert
                break;
            }

            GenTree* op1Idx = op1Intrinsic->Op(3);

            if (!op1Idx->IsCnsIntOrI())
            {
                // Nothing to do if op1's index isn't a constant
                break;
            }

            if (!IsInvariantInRange(op1, node))
            {
                // What we're doing here is effectively similar to containment,
                // except for we're deleting the node entirely, so don't we have
                // nothing to do if there are side effects between node and op1
                break;
            }

            if (op1Intrinsic->Op(2)->IsVectorZero())
            {
                // First build up the new index by updating zmask to include
                // the zmask from op1. We expect that op2 has already been
                // lowered and therefore the containment checks have happened

                // Since this is a newer operation, we need to account for
                // the possibility of `op1Intrinsic` zeroing the same element
                // we're setting here.

                assert(op1Intrinsic->Op(2)->isContained());

                ssize_t op1Ival = op1Idx->AsIntConCommon()->IconValue();
                ival |= ((op1Ival & 0x0F) & ~(1 << count_d));
                op3->AsIntConCommon()->SetIconValue(ival);

                // Then we'll just carry the original non-zero input and
                // remove the now unused constant nodes

                node->Op(1) = op1Intrinsic->Op(1);

                BlockRange().Remove(op1Intrinsic->Op(2));
                BlockRange().Remove(op1Intrinsic->Op(3));
                BlockRange().Remove(op1Intrinsic);
            }
            else if (op2IsVectorZero)
            {
                // Since we've already updated zmask to take op2 being zero into
                // account, we can basically do the same thing here by merging this
                // zmask into the ival from op1.

                // Since this is a later op, direct merging is safe

                ssize_t op1Ival = op1Idx->AsIntConCommon()->IconValue();
                ival            = op1Ival | zmask;
                op3->AsIntConCommon()->SetIconValue(ival);

                // Then we'll just carry the inputs from op1 and remove the now
                // unused constant nodes

                node->Op(1) = op1Intrinsic->Op(1);
                node->Op(2) = op1Intrinsic->Op(2);

                BlockRange().Remove(op2);
                BlockRange().Remove(op1Intrinsic->Op(3));
                BlockRange().Remove(op1Intrinsic);
            }
            break;
        }

        case NI_SSE42_Crc32:
        {
            assert(node->GetOperandCount() == 2);

            // Crc32 takes either a bit register or a memory operand.
            // In either case, only gtType bits are read and so widening
            // or narrowing the operand may be unnecessary and it can
            // just be used directly.

            node->Op(2) = TryRemoveCastIfPresent(node->TypeGet(), node->Op(2));
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
            std::swap(node->Op(1), node->Op(2));
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
            std::swap(node->Op(1), node->Op(2));
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

        case NI_SSE_And:
        case NI_SSE2_And:
        case NI_AVX_And:
        case NI_AVX2_And:
        case NI_AVX512F_And:
        case NI_AVX512DQ_And:
        case NI_SSE_Or:
        case NI_SSE2_Or:
        case NI_AVX_Or:
        case NI_AVX2_Or:
        case NI_AVX512F_Or:
        case NI_AVX512DQ_Or:
        case NI_SSE_Xor:
        case NI_SSE2_Xor:
        case NI_AVX_Xor:
        case NI_AVX2_Xor:
        case NI_AVX512F_Xor:
        case NI_AVX512DQ_Xor:
        {
            if (!comp->IsBaselineVector512IsaSupportedOpportunistically())
            {
                break;
            }
            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);

            LIR::Use use;
            if (BlockRange().TryGetUse(node, &use))
            {
                // search for structure like:
                /*
                        /- A
                        +- B
                    t1 = binary logical op1

                        /- C
                        +- t1
                    t2 = binary logical op2
                */
                GenTree* second = use.User();
                if (!second->OperIs(GT_HWINTRINSIC) || !second->AsHWIntrinsic()->OperIsBitwiseHWIntrinsic())
                {
                    break;
                }

                if (second->AsHWIntrinsic()->HWOperGet() == GT_AND_NOT)
                {
                    // currently ANDNOT logic cannot be optimized by the ternary node.
                    break;
                }
                GenTree* op3 = second->AsHWIntrinsic()->Op(1) == node ? second->AsHWIntrinsic()->Op(2)
                                                                      : second->AsHWIntrinsic()->Op(1);
                GenTree*    control         = comp->gtNewIconNode(node->GetTernaryControlByte(second->AsHWIntrinsic()));
                CorInfoType simdBaseJitType = node->GetSimdBaseJitType();
                unsigned    simdSize        = node->GetSimdSize();
                var_types   simdType        = Compiler::getSIMDTypeForSize(simdSize);
                GenTree*    ternaryNode =
                    comp->gtNewSimdTernaryLogicNode(simdType, op1, op2, op3, control, simdBaseJitType, simdSize);
                BlockRange().InsertBefore(second, control, ternaryNode);
                LIR::Use finalRes;
                if (BlockRange().TryGetUse(second, &finalRes))
                {
                    finalRes.ReplaceWith(ternaryNode);
                }
                else
                {
                    ternaryNode->SetUnusedValue();
                }
                GenTree* next = node->gtNext;
                BlockRange().Remove(node);
                BlockRange().Remove(second);
                return next;
            }
            break;
        }

        default:
            break;
    }

    ContainCheckHWIntrinsic(node);
    return node->gtNext;
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

    assert((intrinsicId == NI_Vector128_op_Equality) || (intrinsicId == NI_Vector128_op_Inequality) ||
           (intrinsicId == NI_Vector256_op_Equality) || (intrinsicId == NI_Vector256_op_Inequality) ||
           (intrinsicId == NI_Vector512_op_Equality) || (intrinsicId == NI_Vector512_op_Inequality));

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);
    assert(node->TypeIs(TYP_INT));
    assert((cmpOp == GT_EQ) || (cmpOp == GT_NE));

    // We have the following (with the appropriate simd size and where the intrinsic could be op_Inequality):
    //          /--*  op2  simd
    //          /--*  op1  simd
    //   node = *  HWINTRINSIC   simd   T op_Equality

    GenTree*     op1    = node->Op(1);
    GenTree*     op2    = node->Op(2);
    GenCondition cmpCnd = (cmpOp == GT_EQ) ? GenCondition::EQ : GenCondition::NE;

    if (!varTypeIsFloating(simdBaseType) && (simdSize != 64) && op2->IsVectorZero() &&
        comp->compOpportunisticallyDependsOn(InstructionSet_SSE41) &&
        !op1->OperIsHWIntrinsic(NI_AVX512F_ConvertMaskToVector))
    {
        // On SSE4.1 or higher we can optimize comparisons against zero to
        // just use PTEST. We can't support it for floating-point, however,
        // as it has both +0.0 and -0.0 where +0.0 == -0.0

        bool skipReplaceOperands = false;

        if (op1->OperIsHWIntrinsic())
        {
            GenTreeHWIntrinsic* op1Intrinsic   = op1->AsHWIntrinsic();
            NamedIntrinsic      op1IntrinsicId = op1Intrinsic->GetHWIntrinsicId();

            GenTree* nestedOp1           = nullptr;
            GenTree* nestedOp2           = nullptr;
            bool     isEmbeddedBroadcast = false;

            if (op1Intrinsic->GetOperandCount() == 2)
            {
                nestedOp1 = op1Intrinsic->Op(1);
                nestedOp2 = op1Intrinsic->Op(2);

                assert(!nestedOp1->isContained());
                isEmbeddedBroadcast = nestedOp2->isContained() && nestedOp2->OperIsHWIntrinsic();
            }

            switch (op1IntrinsicId)
            {
                case NI_SSE_And:
                case NI_SSE2_And:
                case NI_AVX_And:
                case NI_AVX2_And:
                {
                    // We can optimize to TestZ(op1.op1, op1.op2)

                    if (isEmbeddedBroadcast)
                    {
                        // PTEST doesn't support embedded broadcast
                        break;
                    }

                    node->Op(1) = nestedOp1;
                    node->Op(2) = nestedOp2;

                    BlockRange().Remove(op1);
                    BlockRange().Remove(op2);

                    skipReplaceOperands = true;
                    break;
                }

                case NI_SSE_AndNot:
                case NI_SSE2_AndNot:
                case NI_AVX_AndNot:
                case NI_AVX2_AndNot:
                {
                    // We can optimize to TestC(op1.op1, op1.op2)

                    if (isEmbeddedBroadcast)
                    {
                        // PTEST doesn't support embedded broadcast
                        break;
                    }

                    cmpCnd = (cmpOp == GT_EQ) ? GenCondition::C : GenCondition::NC;

                    node->Op(1) = nestedOp1;
                    node->Op(2) = nestedOp2;

                    BlockRange().Remove(op1);
                    BlockRange().Remove(op2);

                    skipReplaceOperands = true;
                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        if (!skipReplaceOperands)
        {
            // Default handler, emit a TestZ(op1, op1)

            node->Op(1) = op1;
            BlockRange().Remove(op2);

            LIR::Use op1Use(BlockRange(), &node->Op(1), node);
            ReplaceWithLclVar(op1Use);
            op1 = node->Op(1);

            op2 = comp->gtClone(op1);
            BlockRange().InsertAfter(op1, op2);
            node->Op(2) = op2;
        }

        if (simdSize == 32)
        {
            // TODO-Review: LowerHWIntrinsicCC resets the id again, so why is this needed?
            node->ChangeHWIntrinsicId(NI_AVX_TestZ);
            LowerHWIntrinsicCC(node, NI_AVX_PTEST, cmpCnd);
        }
        else
        {
            assert(simdSize == 16);

            // TODO-Review: LowerHWIntrinsicCC resets the id again, so why is this needed?
            node->ChangeHWIntrinsicId(NI_SSE41_TestZ);
            LowerHWIntrinsicCC(node, NI_SSE41_PTEST, cmpCnd);
        }

        return LowerNode(node);
    }

    // TODO-XARCH-AVX512: We should handle TYP_SIMD12 here under the EVEX path, but doing
    // so will require us to account for the unused 4th element.

    if ((simdType != TYP_SIMD12) && comp->IsBaselineVector512IsaSupportedOpportunistically())
    {
        // The EVEX encoded versions of the comparison instructions all return a kmask
        //
        // For the comparisons against zero that we normally optimize to use `PTEST` we
        // have to make a decision to use EVEX and emit 2 instructions (vpcmp + kortest)
        // or to continue emitting PTEST and hope that the register allocator isn't limited
        // by it not supporting the extended register set.
        //
        // Ideally we'd opt to not use PTEST when EVEX is available, This would be done so we can
        // best take advantage of EVEX exclusive features such as embedded broadcast and the
        // 16 additional registers. In many cases this allows for overall denser codegen where
        // we are doing more in the same number of bytes, even though the individual instruction
        // is 1-2 bytes larger. Even though there may be cases where continuing to use PTEST for select-
        // 128/256-bit code paths would still be beneficial, the additional complexity required
        // to detect and account for those differences is not likely to be worth the tradeoff.
        //
        // TODO-XARCH-AVX512: Given the above don't emit the PTEST path above when AVX-512 is available
        // This will require exposing `NI_AVX512F_TestZ` so that we can keep codegen optimized to just
        // `vptestm` followed by `kortest`. This will be one instruction more than just `vptest` but
        // it has the advantages detailed above.
        //
        // For other comparisons, using EVEX allows us to avoid leaving the SIMD domain, avoids
        // needing to use a general-purpose register, and allows us to generate less instructions.

        GenTree* maskNode = node;
        GenTree* nextNode = node->gtNext;

        NamedIntrinsic maskIntrinsicId = NI_AVX512F_CompareEqualMask;
        uint32_t       count           = simdSize / genTypeSize(simdBaseType);

        // KORTEST does a bitwise or on the result and sets ZF if it is zero and CF if it is all
        // bits set. Because of this, when we have at least 8 elements to compare we can use a
        // normal comparison alongside CF.
        //
        // That is, if the user wants `x == y`, we can keep it as `mask = (x == y)` and then emit
        // `kortest mask, mask` and check `CF == 1`. This will be true if all elements matched and
        // false otherwise. Things work out nicely and we keep readable disasm.
        //
        // Likewise, if the user wants `x != y`, we can keep it as `mask = (x != y)` and then emit
        // `kortest mask, mask` and check `ZF != 0`. This will be true if any elements mismatched.
        //
        // However, if we have less than 8 elements then we have to change it up since we have less
        // than 8 bits in the output mask and unused bits will be set to 0. This occurs for 32-bit
        // for Vector128 and and 64-bit elements when using either Vector128 or Vector256.
        //
        // To account for this, we will invert the comparison being done. So if the user wants
        // `x == y`, we will instead emit `mask = (x != y)`, we will still emit `kortest mask, mask`,
        // but we will then check for `ZF == 0`. This works since that equates to all elements being equal
        //
        // Likewise for `x != y` we will instead emit `mask = (x == y)`, then `kortest mask, mask`,
        // and will then check for `CF == 0` which equates to one or more elements not being equal

        // The scenarios we have to for a full mask are:
        // * No matches:      0000_0000 - ZF == 1, CF == 0
        // * Partial matches: 0000_1111 - ZF == 0, CF == 0
        // * All matches:     1111_1111 - ZF == 0, CF == 1
        //
        // The scenarios we have to for a partial mask are:
        // * No matches:      0000_0000 - ZF == 1, CF == 0
        // * Partial matches: 0000_0011 - ZF == 0, CF == 0
        // * All matches:     0000_1111 - ZF == 0, CF == 0
        //
        // When we have less than a full mask worth of elements, we need to account for the upper
        // bits being implicitly zero. To do that, we may need to invert the comparison.
        //
        // By inverting the comparison we'll get:
        // * All matches:     0000_0000 - ZF == 1, CF == 0
        // * Partial matches: 0000_0011 - ZF == 0, CF == 0
        // * No matches:      0000_1111 - ZF == 0, CF == 0
        //
        // This works since the upper bits are implicitly zero and so by inverting matches also become
        // zero, which in turn means that `AllBitsSet` will become `Zero` and other cases become non-zero

        if (op1->OperIsHWIntrinsic(NI_AVX512F_ConvertMaskToVector) && op2->IsCnsVec())
        {
            // We want to specially handle the common cases of `mask op Zero` and `mask op AllBitsSet`
            //
            // These get created for the various `gtNewSimdCmpOpAnyNode` and `gtNewSimdCmpOpAllNode`
            // scenarios and we want to ensure they still get "optimal" codegen. To handle that, we
            // simply consume the mask directly and preserve the intended comparison by tweaking the
            // compare condition passed down into `KORTEST`

            maskNode = op1->AsHWIntrinsic()->Op(1);
            assert(maskNode->TypeIs(TYP_MASK));

            bool           isHandled = false;
            GenTreeVecCon* vecCon    = op2->AsVecCon();

            if (vecCon->IsZero())
            {
                // We have `mask == Zero` which is the same as checking that nothing in the mask
                // is set. This scenario can be handled by `kortest` and then checking that `ZF == 1`
                //
                // -or-
                //
                // We have `mask != Zero` which is the same as checking that something in the mask
                // is set. This scenario can be handled by `kortest` and then checking that `ZF == 0`
                //
                // Since this is the default state for `CompareEqualMask` + `GT_EQ`/`GT_NE`, there is nothing
                // for us to change. This also applies to cases where we have less than a full mask of
                // elements since the upper mask bits are implicitly zero.

                isHandled = true;
            }
            else if (vecCon->IsAllBitsSet())
            {
                // We have `mask == AllBitsSet` which is the same as checking that everything in the mask
                // is set. This scenario can be handled by `kortest` and then checking that `CF == 1` for
                // a full mask and checking `ZF == 1` for a partial mask using an inverted comparison
                //
                // -or-
                //
                // We have `mask != AllBitsSet` which is the same as checking that something in the mask
                // is set. This scenario can be handled by `kortest` and then checking that `CF == 0` for
                // a full mask and checking `ZF != 0` for a partial mask using an inverted comparison

                if (count < 8)
                {
                    assert((count == 1) || (count == 2) || (count == 4));

                    maskIntrinsicId = NI_Illegal;

                    if (maskNode->OperIsHWIntrinsic())
                    {
                        maskIntrinsicId = maskNode->AsHWIntrinsic()->GetHWIntrinsicId();
                    }

                    switch (maskIntrinsicId)
                    {
                        case NI_AVX512F_CompareEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareNotEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareGreaterThanMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareNotGreaterThanMask;
                            break;
                        }

                        case NI_AVX512F_CompareGreaterThanOrEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareNotGreaterThanOrEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareLessThanMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareNotLessThanMask;
                            break;
                        }

                        case NI_AVX512F_CompareLessThanOrEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareNotLessThanOrEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareNotEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareNotGreaterThanMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareGreaterThanMask;
                            break;
                        }

                        case NI_AVX512F_CompareNotGreaterThanOrEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareGreaterThanOrEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareNotLessThanMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareLessThanMask;
                            break;
                        }

                        case NI_AVX512F_CompareNotLessThanOrEqualMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareLessThanOrEqualMask;
                            break;
                        }

                        case NI_AVX512F_CompareOrderedMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareUnorderedMask;
                            break;
                        }

                        case NI_AVX512F_CompareUnorderedMask:
                        {
                            maskIntrinsicId = NI_AVX512F_CompareOrderedMask;
                            break;
                        }

                        default:
                        {
                            // We don't have a well known intrinsic, so we need to inverse the mask keeping the upper
                            // n-bits clear. If we have 1 element, then the upper 7-bits need to be cleared. If we have
                            // 2, then the upper 6-bits, and if we have 4, then the upper 4-bits.
                            //
                            // There isn't necessarily a trivial way to do this outside not, shift-left by n,
                            // shift-right by n. This preserves count bits, while clearing the upper n-bits

                            GenTree* cnsNode;

                            maskNode = comp->gtNewSimdHWIntrinsicNode(TYP_MASK, maskNode, NI_AVX512F_NotMask,
                                                                      simdBaseJitType, simdSize);
                            BlockRange().InsertBefore(node, maskNode);

                            cnsNode = comp->gtNewIconNode(8 - count);
                            BlockRange().InsertAfter(maskNode, cnsNode);

                            maskNode =
                                comp->gtNewSimdHWIntrinsicNode(TYP_MASK, maskNode, cnsNode, NI_AVX512F_ShiftLeftMask,
                                                               simdBaseJitType, simdSize);
                            BlockRange().InsertAfter(cnsNode, maskNode);
                            LowerNode(maskNode);

                            cnsNode = comp->gtNewIconNode(8 - count);
                            BlockRange().InsertAfter(maskNode, cnsNode);

                            maskNode =
                                comp->gtNewSimdHWIntrinsicNode(TYP_MASK, maskNode, cnsNode, NI_AVX512F_ShiftRightMask,
                                                               simdBaseJitType, simdSize);
                            BlockRange().InsertAfter(cnsNode, maskNode);

                            maskIntrinsicId = NI_AVX512F_ShiftRightMask;
                            break;
                        }
                    }

                    maskNode->AsHWIntrinsic()->ChangeHWIntrinsicId(maskIntrinsicId);
                    LowerNode(maskNode);
                }
                else if (cmpOp == GT_EQ)
                {
                    cmpCnd = GenCondition::C;
                }
                else
                {
                    cmpCnd = GenCondition::NC;
                }
                isHandled = true;
            }

            if (isHandled)
            {
                LIR::Use use;
                if (BlockRange().TryGetUse(node, &use))
                {
                    use.ReplaceWith(maskNode);
                }
                else
                {
                    maskNode->SetUnusedValue();
                }

                BlockRange().Remove(op2);
                BlockRange().Remove(op1);
                BlockRange().Remove(node);

                op1 = nullptr;
                op2 = nullptr;
            }
        }

        if (!varTypeIsFloating(simdBaseType) && (op2 != nullptr) && op2->IsVectorZero())
        {
            NamedIntrinsic testIntrinsicId     = NI_AVX512F_PTESTM;
            bool           skipReplaceOperands = false;

            if (op1->OperIsHWIntrinsic())
            {
                GenTreeHWIntrinsic* op1Intrinsic   = op1->AsHWIntrinsic();
                NamedIntrinsic      op1IntrinsicId = op1Intrinsic->GetHWIntrinsicId();

                switch (op1IntrinsicId)
                {
                    case NI_AVX512F_And:
                    case NI_AVX512DQ_And:
                    {
                        // We have `(x & y) == 0` with GenCondition::EQ (jz, setz, cmovz)
                        // or `(x & y) != 0`with GenCondition::NE (jnz, setnz, cmovnz)
                        //
                        // `vptestnm(x, y)` does the equivalent of `(x & y) == 0`,
                        // thus giving us `1` if zero and `0` if non-zero
                        //
                        // `vptestm(x, y)` does the equivalent of `(x & y) != 0`,
                        // thus giving us `1` if non-zero and `0` if zero
                        //
                        // Given the mask produced `m`, we then do `zf: (m == Zero)`, `cf: (m == AllBitsSet)`
                        //
                        // Thus, for either we can first emit `vptestm(x, y)`. This gives us a mask where
                        // `0` means the corresponding elements compared to zero. The subsequent `kortest`
                        // will then set `ZF: 1` if all elements were 0 and `ZF: 0` if any elements were
                        // non-zero. The default GenCondition then remain correct

                        assert(testIntrinsicId == NI_AVX512F_PTESTM);

                        node->Op(1) = op1Intrinsic->Op(1);
                        node->Op(2) = op1Intrinsic->Op(2);

                        BlockRange().Remove(op1);
                        BlockRange().Remove(op2);

                        skipReplaceOperands = true;
                        break;
                    }

                    default:
                    {
                        // We cannot optimize `AndNot` since `vptestnm` does ~(x & y)
                        break;
                    }
                }
            }

            if (!skipReplaceOperands)
            {
                node->Op(1) = op1;
                BlockRange().Remove(op2);

                LIR::Use op1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(op1Use);
                op1 = node->Op(1);

                op2 = comp->gtClone(op1);
                BlockRange().InsertAfter(op1, op2);
                node->Op(2) = op2;
            }

            node->gtType = TYP_MASK;
            node->ChangeHWIntrinsicId(testIntrinsicId);

            LowerNode(node);
            maskNode = node;
        }

        if (maskNode->gtType != TYP_MASK)
        {
            assert(node == maskNode);

            // We have `x == y` or `x != y` both of which where we want to find `AllBitsSet` in the mask since
            // we can directly do the relevant comparison. Given the above tables then when we have a full mask
            // we can simply check against `CF == 1` for `op_Equality` and `ZF == 0` for `op_Inequality`.
            //
            // For a partial mask, we need to invert the `op_Equality` comparisons which means that we now need
            // to check for `ZF == 1` (we're looking for `AllBitsSet`, that is `all match`). For `op_Inequality`
            // we can keep things as is since we're looking for `any match` and just want to check `ZF == 0`

            if (count < 8)
            {
                assert((count == 1) || (count == 2) || (count == 4));
                maskIntrinsicId = NI_AVX512F_CompareNotEqualMask;
            }
            else
            {
                assert((count == 8) || (count == 16) || (count == 32) || (count == 64));

                if (cmpOp == GT_EQ)
                {
                    cmpCnd = GenCondition::C;
                }
                else
                {
                    maskIntrinsicId = NI_AVX512F_CompareNotEqualMask;
                }
            }

            node->gtType = TYP_MASK;
            node->ChangeHWIntrinsicId(maskIntrinsicId);

            LowerNode(node);
            maskNode = node;
        }

        LIR::Use use;
        if (BlockRange().TryGetUse(maskNode, &use))
        {
            GenTreeHWIntrinsic* cc;

            cc = comp->gtNewSimdHWIntrinsicNode(simdType, maskNode, NI_AVX512F_KORTEST, simdBaseJitType, simdSize);
            BlockRange().InsertBefore(nextNode, cc);

            use.ReplaceWith(cc);
            LowerHWIntrinsicCC(cc, NI_AVX512F_KORTEST, cmpCnd);

            nextNode = cc->gtNext;
        }
        return nextNode;
    }

    assert(simdSize != 64);

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
            cmpJitType = simdBaseJitType;

            if (simdSize == 32)
            {
                cmpIntrinsic = NI_AVX2_CompareEqual;
                mskIntrinsic = NI_AVX2_MoveMask;
                mskConstant  = -1;
            }
            else
            {
                assert(simdSize == 16);

                if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    cmpIntrinsic = NI_SSE41_CompareEqual;
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
    node->ChangeType(TYP_INT);
    node->AsOp()->gtOp1 = msk;
    node->AsOp()->gtOp2 = mskCns;

    GenTree* cc = LowerNodeCC(node, cmpCnd);

    node->gtType = TYP_VOID;
    node->ClearUnusedValue();

    return LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCndSel: Lowers a Vector128 or Vector256 Conditional Select call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicCndSel(GenTreeHWIntrinsic* node)
{
    assert(!comp->compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

    var_types   simdType        = node->gtType;
    CorInfoType simdBaseJitType = node->GetSimdBaseJitType();
    var_types   simdBaseType    = node->GetSimdBaseType();
    unsigned    simdSize        = node->GetSimdSize();

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    // Get the three arguments to ConditionalSelect we stored in node
    // op1: the condition vector
    // op2: the left vector
    // op3: the right vector
    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);
    GenTree* op3 = node->Op(3);

    // If the condition vector comes from a hardware intrinsic that
    // returns a per-element mask (marked with HW_Flag_ReturnsPerElementMask),
    // we can optimize the entire conditional select to
    // a single BlendVariable instruction (if supported by the architecture)

    // TODO-XARCH-AVX512 Use VPBLENDM* and take input directly from K registers if cond is from MoveMaskToVectorSpecial.
    // First, determine if the condition is a per-element mask
    if (op1->OperIsHWIntrinsic() && HWIntrinsicInfo::ReturnsPerElementMask(op1->AsHWIntrinsic()->GetHWIntrinsicId()))
    {
        // Next, determine if the target architecture supports BlendVariable
        NamedIntrinsic blendVariableId = NI_Illegal;

        // For Vector256 (simdSize == 32), BlendVariable for floats/doubles is available on AVX, whereas other types
        // require AVX2
        if (simdSize == 32)
        {
            if (varTypeIsFloating(simdBaseType))
            {
                // This should have already been confirmed
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                blendVariableId = NI_AVX_BlendVariable;
            }
            else if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                blendVariableId = NI_AVX2_BlendVariable;
            }
        }
        // For Vector128, BlendVariable is available on SSE41
        else if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
        {
            blendVariableId = NI_SSE41_BlendVariable;
        }

        // If blendVariableId has been set, the architecture supports BlendVariable, so we can optimize
        if (blendVariableId != NI_Illegal)
        {
            // result = BlendVariable op3 (right) op2 (left) op1 (mask)
            node->ResetHWIntrinsicId(blendVariableId, comp, op3, op2, op1);
            return LowerNode(node);
        }
    }

    // We cannot optimize, so produce unoptimized instructions

    // We will be constructing the following parts:
    //          /--*  op1 simd16
    //          *  STORE_LCL_VAR simd16
    //   op1  =    LCL_VAR       simd16
    //   tmp1 =    LCL_VAR       simd16
    //   ...

    GenTree* tmp1;
    GenTree* tmp2;
    GenTree* tmp3;
    GenTree* tmp4;

    LIR::Use op1Use(BlockRange(), &node->Op(1), node);
    ReplaceWithLclVar(op1Use);
    op1 = node->Op(1);

    tmp1 = comp->gtClone(op1);
    BlockRange().InsertAfter(op1, tmp1);

    // ...
    // tmp2 = op1 & op2
    // ...
    tmp2 = comp->gtNewSimdBinOpNode(GT_AND, simdType, op1, op2, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(op2, tmp2);
    LowerNode(tmp2);

    // ...
    // tmp3 = op3 & ~tmp1
    // ...
    tmp3 = comp->gtNewSimdBinOpNode(GT_AND_NOT, simdType, op3, tmp1, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(op3, tmp3);
    LowerNode(tmp3);

    // ...
    // tmp4 = tmp2 | tmp3
    // ...
    tmp4 = comp->gtNewSimdBinOpNode(GT_OR, simdType, tmp2, tmp3, simdBaseJitType, simdSize);
    BlockRange().InsertBefore(node, tmp4);

    LIR::Use use;
    if (BlockRange().TryGetUse(node, &use))
    {
        use.ReplaceWith(tmp4);
    }
    else
    {
        tmp4->SetUnusedValue();
    }

    BlockRange().Remove(node);
    return LowerNode(tmp4);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector128 or Vector256 or Vector512 Create call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    var_types      simdType        = node->gtType;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    simd_t         simdVal         = {};

    if ((simdSize == 8) && (simdType == TYP_DOUBLE))
    {
        // TODO-Cleanup: Struct retyping means we have the wrong type here. We need to
        //               manually fix it up so the simdType checks below are correct.
        simdType = TYP_SIMD8;
    }

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->Op(1);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;
    GenTree* tmp3 = nullptr;

    bool isConstant     = GenTreeVecCon::IsHWIntrinsicCreateConstant<simd_t>(node, simdVal);
    bool isCreateScalar = (intrinsicId == NI_Vector128_CreateScalar) || (intrinsicId == NI_Vector256_CreateScalar) ||
                          (intrinsicId == NI_Vector512_CreateScalar);
    size_t argCnt = node->GetOperandCount();

    if (isConstant)
    {
        assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16) || (simdSize == 32) || (simdSize == 64));

        for (GenTree* arg : node->Operands())
        {
#if !defined(TARGET_64BIT)
            if (arg->OperIsLong())
            {
                BlockRange().Remove(arg->AsOp()->gtGetOp1());
                BlockRange().Remove(arg->AsOp()->gtGetOp2());
            }
#endif // !TARGET_64BIT
            BlockRange().Remove(arg);
        }

        GenTreeVecCon* vecCon = comp->gtNewVconNode(simdType);
        memcpy(&vecCon->gtSimdVal, &simdVal, simdSize);
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
        if (isCreateScalar)
        {
            node->gtType = TYP_SIMD16;
            node->SetSimdSize(16);

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                {
                    // Types need to be explicitly zero-extended to ensure upper-bits are zero
                    //
                    // We need to explicitly use TYP_UBYTE since unsigned is ignored for small types
                    // Explicitly handle both BYTE and UBYTE to account for reinterpret casts and the like
                    //
                    // The from type is INT since that is the input type tracked by IR, where-as the target
                    // type needs to be UBYTE so it implicitly zero-extends back to TYP_INT

                    tmp1 = comp->gtNewCastNode(TYP_INT, op1, /* unsigned */ true, TYP_UBYTE);
                    BlockRange().InsertAfter(op1, tmp1);
                    LowerNode(tmp1);

                    node->ChangeHWIntrinsicId(NI_SSE2_ConvertScalarToVector128Int32, tmp1);
                    node->SetSimdBaseJitType(CORINFO_TYPE_INT);
                    break;
                }

                case TYP_SHORT:
                case TYP_USHORT:
                {
                    // Types need to be explicitly zero-extended to ensure upper-bits are zero
                    //
                    // We need to explicitly use TYP_USHORT since unsigned is ignored for small types
                    // Explicitly handle both SHORT and USHORT to account for reinterpret casts and the like
                    //
                    // The from type is INT since that is the input type tracked by IR, where-as the target
                    // type needs to be USHORT so it implicitly zero-extends back to TYP_INT

                    tmp1 = comp->gtNewCastNode(TYP_INT, op1, /* unsigned */ true, TYP_USHORT);
                    BlockRange().InsertAfter(op1, tmp1);
                    LowerNode(tmp1);

                    node->ChangeHWIntrinsicId(NI_SSE2_ConvertScalarToVector128Int32, tmp1);
                    node->SetSimdBaseJitType(CORINFO_TYPE_INT);
                    break;
                }

                case TYP_INT:
                {
                    node->ChangeHWIntrinsicId(NI_SSE2_ConvertScalarToVector128Int32);
                    break;
                }

                case TYP_UINT:
                {
                    node->ChangeHWIntrinsicId(NI_SSE2_ConvertScalarToVector128UInt32);
                    break;
                }

#if defined(TARGET_AMD64)
                case TYP_LONG:
                {
                    node->ChangeHWIntrinsicId(NI_SSE2_X64_ConvertScalarToVector128Int64);
                    break;
                }

                case TYP_ULONG:
                {
                    node->ChangeHWIntrinsicId(NI_SSE2_X64_ConvertScalarToVector128UInt64);
                    break;
                }
#endif // TARGET_AMD64

                case TYP_FLOAT:
                {
                    tmp1 = comp->gtNewZeroConNode(simdType);
                    BlockRange().InsertBefore(op1, tmp1);
                    LowerNode(tmp1);

                    if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        // Sse41.Insert has:
                        //  * Bits 0-3: zmask
                        //  * Bits 4-5: count_d
                        //  * Bits 6-7: count_s (register form only)
                        //
                        // We want zmask 0b1110 (0xE) to zero elements 1/2/3
                        // We want count_d 0b00 (0x0) to insert the value to element 0
                        // We want count_s 0b00 (0x0) as we're just taking element 0 of the source

                        idx = comp->gtNewIconNode(0x0E);
                        BlockRange().InsertAfter(op1, idx);
                        LowerNode(idx);

                        node->ResetHWIntrinsicId(NI_SSE41_Insert, comp, tmp1, op1, idx);
                    }
                    else
                    {
                        node->ResetHWIntrinsicId(NI_SSE_MoveScalar, comp, tmp1, op1);
                    }
                    break;
                }

                case TYP_DOUBLE:
                {
                    tmp1 = comp->gtNewZeroConNode(simdType);
                    BlockRange().InsertBefore(op1, tmp1);
                    LowerNode(tmp1);

                    node->ResetHWIntrinsicId(NI_SSE2_MoveScalar, comp, tmp1, op1);
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            if (simdSize == 32)
            {
                // We're creating a Vector256 scalar so we need to treat the original op as Vector128,
                // we need to unsafely extend up to Vector256 (which is actually safe since the 128-bit
                // op will zero extend up to 256-bits), and then we need to replace the original use
                // with the new TYP_SIMD32 node.

                node->ChangeType(TYP_SIMD16);
                node->SetSimdSize(16);
                LowerNode(node);

                tmp2 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, node, NI_Vector128_ToVector256Unsafe, simdBaseJitType,
                                                      16);

                LIR::Use use;
                bool     foundUse = BlockRange().TryGetUse(node, &use);
                BlockRange().InsertAfter(node, tmp2);

                if (foundUse)
                {
                    use.ReplaceWith(tmp2);
                }
                else
                {
                    node->ClearUnusedValue();
                    tmp2->SetUnusedValue();
                }

                node = tmp2->AsHWIntrinsic();
            }

            return LowerNode(node);
        }

        // We have the following (where simd is simd16, simd32 or simd64):
        //          /--*  op1  T
        //   node = *  HWINTRINSIC   simd   T Create

        if (intrinsicId == NI_Vector512_Create)
        {
            assert(comp->IsBaselineVector512IsaSupportedDebugOnly());
            // We will be constructing the following parts:
            //          /--*  op1  T
            //   tmp1 = *  HWINTRINSIC   simd32 T CreateScalarUnsafe
            //          /--*  tmp1 simd16
            //   node = *  HWINTRINSIC   simd64 T BroadcastScalarToVector512

            // This is roughly the following managed code:
            //   var tmp1 = Vector256.CreateScalarUnsafe(op1);
            //   return Avx512.BroadcastScalarToVector512(tmp1);

            tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op1, simdBaseJitType, 16);
            LowerNode(tmp1);
            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_SHORT:
                case TYP_USHORT:
                {
                    assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
                    node->ResetHWIntrinsicId(NI_AVX512BW_BroadcastScalarToVector512, tmp1);
                    break;
                }

                case TYP_INT:
                case TYP_UINT:
                case TYP_FLOAT:
                case TYP_DOUBLE:
                case TYP_LONG:
                case TYP_ULONG:
                {
                    assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    node->ResetHWIntrinsicId(NI_AVX512F_BroadcastScalarToVector512, tmp1);
                    break;
                }
                default:
                {
                    unreached();
                }
            }
            return LowerNode(node);
        }

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

                tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op1, simdBaseJitType, 16);
                LowerNode(tmp1);

                node->ResetHWIntrinsicId(NI_AVX2_BroadcastScalarToVector256, tmp1);
                return LowerNode(node);
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
            //   node = *  HWINTRINSIC simd32 T WithUpper

            // This is roughly the following managed code:
            //   var tmp1 = Vector128.Create(op1);
            //   var tmp2 = tmp1;
            //   var tmp3 = tmp2.ToVector256Unsafe();
            //   return tmp3.WithUpper(tmp1);

            tmp1 = comp->gtNewSimdCreateBroadcastNode(TYP_SIMD16, op1, simdBaseJitType, 16);
            BlockRange().InsertAfter(op1, tmp1);

            node->Op(1) = tmp1;
            LowerNode(tmp1);

            LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
            ReplaceWithLclVar(tmp1Use);
            tmp1 = node->Op(1);

            tmp2 = comp->gtClone(tmp1);
            BlockRange().InsertAfter(tmp1, tmp2);

            tmp3 =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, tmp2, NI_Vector128_ToVector256Unsafe, simdBaseJitType, 16);
            BlockRange().InsertAfter(tmp2, tmp3);

            node->ResetHWIntrinsicId(NI_Vector256_WithUpper, comp, tmp3, tmp1);
            LowerNode(tmp3);

            return LowerNode(node);
        }

        assert(intrinsicId == NI_Vector128_Create);

        // We will be constructing the following parts:
        //          /--*  op1  T
        //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
        //   ...

        // This is roughly the following managed code:
        //   var tmp1 = Vector128.CreateScalarUnsafe(op1);
        //   ...

        tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op1, simdBaseJitType, 16);
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

            node->ChangeHWIntrinsicId(NI_AVX2_BroadcastScalarToVector128, tmp1);
            return LowerNode(node);
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
                    //   tmp2 =    CNS_VEC   simd16 0
                    //         /--*  tmp1 simd16
                    //         +--*  tmp2 simd16
                    //   node = *  HWINTRINSIC   simd16 ubyte Shuffle

                    // This is roughly the following managed code:
                    //   ...
                    //   var tmp2 = Vector128<byte>.Zero;
                    //   return Ssse3.Shuffle(tmp1, tmp2);

                    tmp2 = comp->gtNewZeroConNode(simdType);
                    BlockRange().InsertAfter(tmp1, tmp2);
                    LowerNode(tmp2);

                    node->ResetHWIntrinsicId(NI_SSSE3_Shuffle, tmp1, tmp2);
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

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

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

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

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

                node->ResetHWIntrinsicId(NI_SSE2_Shuffle, tmp1, idx);
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

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                node->ResetHWIntrinsicId(NI_SSE2_UnpackLow, tmp1, tmp2);
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

                    node->ResetHWIntrinsicId(NI_AVX_Permute, tmp1, idx);
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

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                idx = comp->gtNewIconNode(0x00, TYP_INT);
                BlockRange().InsertAfter(tmp2, idx);

                node->ResetHWIntrinsicId(NI_SSE_Shuffle, comp, tmp1, tmp2, idx);
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

                    node->ChangeHWIntrinsicId(NI_SSE3_MoveAndDuplicate, tmp1);
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

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                node->ResetHWIntrinsicId(NI_SSE_MoveLowToHigh, tmp1, tmp2);
                node->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);
                break;
            }

            default:
            {
                unreached();
            }
        }

        return LowerNode(node);
    }

    GenTree* op2 = node->Op(2);

    // TODO-XArch-AVX512 : Merge the NI_Vector512_Create and NI_Vector256_Create paths below.
    // We have the following (where simd is simd16 or simd32):
    //          /--*  op1 T
    //          +--*  ... T
    //          +--*  opN T
    //   node = *  HWINTRINSIC   simd   T Create
    if (intrinsicId == NI_Vector512_Create)
    {
        assert(comp->IsBaselineVector512IsaSupportedDebugOnly());

        // We will be constructing the following parts:
        //          /--*  op1 T
        //          +--*  ... T
        //   lo   = *  HWINTRINSIC   simd32 T Create
        //          /--*  ... T
        //          +--*  opN T
        //   hi   = *  HWINTRINSIC   simd32 T Create
        //          /--*  lo   simd64
        //          +--*  hi   simd32
        //   node = *  HWINTRINSIC   simd64 T WithUpper

        // This is roughly the following managed code:
        //   ...
        //   var lo   = Vector256.Create(op1, ...);
        //   var hi   = Vector256.Create(..., opN);
        //   return lo.WithUpper(hi);

        // Each Vector256.Create call gets half the operands. That is:
        //   lo = Vector256.Create(op1, op2);
        //   hi = Vector256.Create(op3, op4);
        // -or-
        //   lo = Vector256.Create(op1,  ..., op4);
        //   hi = Vector256.Create(op5,  ..., op8);
        // -or-
        //   lo = Vector256.Create(op1,  ..., op8);
        //   hi = Vector256.Create(op9,  ..., op16);
        // -or-
        //   lo = Vector256.Create(op1,  ..., op16);
        //   hi = Vector256.Create(op17, ..., op32);

        size_t halfArgCnt = argCnt / 2;
        assert((halfArgCnt * 2) == argCnt);

        GenTree* loInsertionPoint = LIR::LastNode(node->GetOperandArray(), halfArgCnt);

        GenTree* lo = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, node->GetOperandArray(), halfArgCnt,
                                                     NI_Vector256_Create, simdBaseJitType, 32);
        BlockRange().InsertAfter(loInsertionPoint, lo);

        GenTree* hiInsertionPoint = LIR::LastNode(node->GetOperandArray(halfArgCnt), halfArgCnt);
        GenTree* hi = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, node->GetOperandArray(halfArgCnt), halfArgCnt,
                                                     NI_Vector256_Create, simdBaseJitType, 32);
        BlockRange().InsertAfter(hiInsertionPoint, hi);

        assert(argCnt >= 7);
        node->ResetHWIntrinsicId(NI_Vector512_WithUpper, comp, lo, hi);

        LowerNode(lo);
        LowerNode(hi);

        return LowerNode(node);
    }
    else if (intrinsicId == NI_Vector256_Create)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        // We will be constructing the following parts:
        //          /--*  op1 T
        //          +--*  ... T
        //   lo   = *  HWINTRINSIC   simd16 T Create
        //          /--*  ... T
        //          +--*  opN T
        //   hi   = *  HWINTRINSIC   simd16 T Create
        //          /--*  lo   simd32
        //          +--*  hi   simd16
        //   node = *  HWINTRINSIC   simd32 T WithUpper

        // This is roughly the following managed code:
        //   ...
        //   var lo   = Vector128.Create(op1, ...);
        //   var hi   = Vector128.Create(..., opN);
        //   return lo.WithUpper(hi);

        // Each Vector128.Create call gets half the operands. That is:
        //   lo = Vector128.Create(op1, op2);
        //   hi = Vector128.Create(op3, op4);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op4);
        //   hi = Vector128.Create(op5,  ..., op8);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op8);
        //   hi = Vector128.Create(op9,  ..., op16);
        // -or-
        //   lo = Vector128.Create(op1,  ..., op16);
        //   hi = Vector128.Create(op17, ..., op32);

        size_t halfArgCnt = argCnt / 2;
        assert((halfArgCnt * 2) == argCnt);

        GenTree* loInsertionPoint = LIR::LastNode(node->GetOperandArray(), halfArgCnt);

        GenTree* lo = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, node->GetOperandArray(), halfArgCnt,
                                                     NI_Vector128_Create, simdBaseJitType, 16);
        BlockRange().InsertAfter(loInsertionPoint, lo);

        GenTree* hiInsertionPoint = LIR::LastNode(node->GetOperandArray(halfArgCnt), halfArgCnt);

        GenTree* hi = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, node->GetOperandArray(halfArgCnt), halfArgCnt,
                                                     NI_Vector128_Create, simdBaseJitType, 16);
        BlockRange().InsertAfter(hiInsertionPoint, hi);

        assert(argCnt >= 3);
        node->ResetHWIntrinsicId(NI_Vector256_WithUpper, comp, lo, hi);

        LowerNode(lo);
        LowerNode(hi);

        return LowerNode(node);
    }

    // We will be constructing the following parts:
    //          /--*  op1  T
    //   tmp1 = *  HWINTRINSIC   simd16 T CreateScalarUnsafe
    //   ...

    // This is roughly the following managed code:
    //   var tmp1 = Vector128.CreateScalarUnsafe(op1);
    //   ...

    tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op1, simdBaseJitType, 16);
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

                    opN = node->Op(N + 1);

                    idx = comp->gtNewIconNode(N, TYP_INT);
                    // Place the insert as early as possible to avoid creating a lot of long lifetimes.
                    GenTree* insertionPoint = LIR::LastNode(tmp1, opN);

                    tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, opN, idx, insIntrinsic, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(insertionPoint, idx, tmp1);
                    LowerNode(tmp1);
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

                opN = node->Op(argCnt);

                idx = comp->gtNewIconNode(N, TYP_INT);
                BlockRange().InsertAfter(opN, idx);

                node->ResetHWIntrinsicId(insIntrinsic, comp, tmp1, opN, idx);
                break;
            }

            assert((simdBaseType != TYP_SHORT) && (simdBaseType != TYP_USHORT));
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));

            GenTree* op[16];
            op[0] = tmp1;

            for (N = 1; N < argCnt; N++)
            {
                opN = node->Op(N + 1);

                op[N] = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, opN, simdBaseJitType, 16);
                LowerNode(op[N]);
            }

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
                    BlockRange().InsertAfter(LIR::LastNode(op[N], op[O]), tmp1);
                    LowerNode(tmp1);

                    tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, op[P], op[Q], NI_SSE2_UnpackLow, CORINFO_TYPE_UBYTE,
                                                          simdSize);
                    BlockRange().InsertAfter(LIR::LastNode(op[P], op[Q]), tmp2);
                    LowerNode(tmp2);

                    tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, NI_SSE2_UnpackLow, CORINFO_TYPE_USHORT,
                                                          simdSize);
                    BlockRange().InsertAfter(LIR::LastNode(tmp1, tmp2), tmp3);
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
            BlockRange().InsertAfter(LIR::LastNode(op[0], op[1]), tmp1);
            LowerNode(tmp1);

            tmp2 =
                comp->gtNewSimdHWIntrinsicNode(simdType, op[2], op[3], NI_SSE2_UnpackLow, CORINFO_TYPE_UINT, simdSize);
            BlockRange().InsertAfter(LIR::LastNode(op[2], op[3]), tmp2);
            LowerNode(tmp2);

            node->ResetHWIntrinsicId(NI_SSE2_UnpackLow, tmp1, tmp2);
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

                node->ResetHWIntrinsicId(NI_SSE41_X64_Insert, comp, tmp1, op2, idx);
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

            tmp2 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op2, simdBaseJitType, 16);
            LowerNode(tmp2);

            node->ResetHWIntrinsicId(NI_SSE2_UnpackLow, tmp1, tmp2);
            break;
        }
#endif // TARGET_AMD64

        case TYP_FLOAT:
        {
            unsigned N   = 0;
            GenTree* opN = nullptr;

            if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
            {
                assert(argCnt <= 4);
                GenTree* insertedNodes[4];

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

                    opN = node->Op(N + 1);

                    tmp2 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, opN, simdBaseJitType, 16);
                    LowerNode(tmp2);

                    idx = comp->gtNewIconNode(N << 4, TYP_INT);

                    // Place the insert as early as possible to avoid creating a lot of long lifetimes.
                    GenTree* insertionPoint = LIR::LastNode(tmp1, tmp2);

                    tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp1, tmp2, idx, NI_SSE41_Insert, simdBaseJitType,
                                                          simdSize);
                    BlockRange().InsertAfter(insertionPoint, idx, tmp3);

                    insertedNodes[N] = tmp3;
                    tmp1             = tmp3;
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

                opN = node->Op(argCnt);

                tmp2 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, opN, simdBaseJitType, 16);
                LowerNode(tmp2);

                idx = comp->gtNewIconNode((argCnt - 1) << 4, TYP_INT);
                BlockRange().InsertAfter(tmp2, idx);

                node->ResetHWIntrinsicId(NI_SSE41_Insert, comp, tmp1, tmp2, idx);

                for (N = 1; N < argCnt - 1; N++)
                {
                    // LowerNode for NI_SSE41_Insert specially handles zeros, constants, and certain mask values
                    // to do the minimal number of operations and may merge together two neighboring inserts that
                    // don't have any side effects between them. Because of this and because of the interdependence
                    // of the inserts we've created above, we need to wait to lower the generated inserts until after
                    // we've completed the chain.

                    GenTree* insertedNode = insertedNodes[N];
                    LowerNode(insertedNode);
                }
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
                opN = node->Op(N + 1);

                op[N] = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, opN, simdBaseJitType, 16);
                LowerNode(op[N]);
            }

            tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op[0], op[1], NI_SSE_UnpackLow, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(LIR::LastNode(op[0], op[1]), tmp1);
            LowerNode(tmp1);

            tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, op[2], op[3], NI_SSE_UnpackLow, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(LIR::LastNode(op[2], op[3]), tmp2);
            LowerNode(tmp2);

            node->ResetHWIntrinsicId(NI_SSE_MoveLowToHigh, tmp1, tmp2);
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

            tmp2 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op2, simdBaseJitType, 16);
            LowerNode(tmp2);

            node->ResetHWIntrinsicId(NI_SSE_MoveLowToHigh, tmp1, tmp2);
            node->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);
            break;
        }

        default:
        {
            unreached();
        }
    }

    return LowerNode(node);
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicGetElement: Lowers a vector GetElement call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicGetElement(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    var_types      simdType        = node->gtType;
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    assert((intrinsicId == NI_Vector128_GetElement) || (intrinsicId == NI_Vector256_GetElement) ||
           (intrinsicId == NI_Vector512_GetElement));

    assert(!varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);

    if (op2->IsIntegralConst(0))
    {
        // Specially handle as ToScalar
        BlockRange().Remove(op2);

        if (simdSize == 64)
        {
            intrinsicId = NI_Vector512_ToScalar;
        }
        else if (simdSize == 32)
        {
            intrinsicId = NI_Vector256_ToScalar;
        }
        else
        {
            intrinsicId = NI_Vector128_ToScalar;
        }

        node->ResetHWIntrinsicId(intrinsicId, op1);
        return LowerNode(node);
    }

    uint32_t count    = simdSize / genTypeSize(simdBaseType);
    uint32_t elemSize = genTypeSize(simdBaseType);

    if (op1->OperIs(GT_IND))
    {
        // We want to optimize GetElement down to an Indir where possible as
        // this unlocks additional containment opportunities for various nodes

        GenTree* newBase;
        GenTree* newIndex;
        uint32_t newScale;
        int32_t  newOffset;

        GenTreeIndir* indir = op1->AsIndir();
        GenTree*      addr  = indir->Addr();

        if (addr->OperIsAddrMode())
        {
            // We have an existing addressing mode, so we want to try and
            // combine with that where possible to keep things as a 1x LEA

            GenTreeAddrMode* addrMode = addr->AsAddrMode();

            newBase   = addrMode->Base();
            newIndex  = addrMode->Index();
            newScale  = addrMode->GetScale();
            newOffset = addrMode->Offset();

            if (op2->OperIsConst() && (newOffset < (INT32_MAX - static_cast<int>(simdSize))))
            {
                // op2 is a constant, so add it to the existing offset

                BlockRange().Remove(addrMode);
                BlockRange().Remove(op2);

                int32_t addOffset = (static_cast<uint8_t>(op2->AsIntCon()->IconValue()) % count);
                addOffset *= static_cast<int32_t>(elemSize);

                newOffset += addOffset;
            }
            else if (newIndex == nullptr)
            {
                // op2 is not a constant and the addressing mode doesn't
                // have its own existing index, so use our index and scale

                BlockRange().Remove(addrMode);

                newIndex = op2;
                newScale = elemSize;
            }
            else if (addrMode->GetScale() == elemSize)
            {
                // op2 is not a constant but the addressing mode has its
                // own already with a matching scale, so add ours to theirs

                BlockRange().Remove(addrMode);

                newIndex = comp->gtNewOperNode(GT_ADD, TYP_I_IMPL, newIndex, op2);
                BlockRange().InsertBefore(node, newIndex);

                LowerNode(newIndex);
            }
            else
            {
                // op2 is not a constant but the addressing mode is already
                // complex, so build a new addressing mode with the prev as our base

                newBase   = addrMode;
                newIndex  = op2;
                newScale  = elemSize;
                newOffset = 0;
            }
        }
        else if (op2->OperIsConst())
        {
            // We don't have an addressing mode, so build one with the old addr
            // as the base and the offset using the op2 constant and scale

            BlockRange().Remove(op2);

            newBase   = addr;
            newIndex  = nullptr;
            newScale  = 0;
            newOffset = (static_cast<uint8_t>(op2->AsIntCon()->IconValue()) % count);
            newOffset *= static_cast<int32_t>(elemSize);
        }
        else
        {
            // We don't have an addressing mode, so build one with the old addr
            // as the base and the index set to op2

            newBase   = addr;
            newIndex  = op2;
            newScale  = elemSize;
            newOffset = 0;
        }

        if (newBase != nullptr)
        {
            newBase->ClearContained();
        }

        if (newIndex != nullptr)
        {
            newIndex->ClearContained();
        }

        GenTreeAddrMode* newAddr =
            new (comp, GT_LEA) GenTreeAddrMode(addr->TypeGet(), newBase, newIndex, newScale, newOffset);
        BlockRange().InsertBefore(node, newAddr);

        GenTreeIndir* newIndir =
            comp->gtNewIndir(JITtype2varType(simdBaseJitType), newAddr, (indir->gtFlags & GTF_IND_FLAGS));
        BlockRange().InsertBefore(node, newIndir);

        LIR::Use use;
        if (BlockRange().TryGetUse(node, &use))
        {
            use.ReplaceWith(newIndir);
        }
        else
        {
            newIndir->SetUnusedValue();
        }

        BlockRange().Remove(op1);
        BlockRange().Remove(node);

        assert(newAddr->gtNext == newIndir);
        return LowerNode(newAddr);
    }

    if (!op2->OperIsConst())
    {
        // We will specially handle GetElement in codegen when op2 isn't a constant
        ContainCheckHWIntrinsic(node);
        return node->gtNext;
    }

    // We should have a bounds check inserted for any index outside the allowed range
    // but we need to generate some code anyways, and so we'll simply mask here for simplicity.

    uint32_t imm8      = static_cast<uint8_t>(op2->AsIntCon()->IconValue()) % count;
    uint32_t simd16Cnt = 16 / elemSize;
    uint32_t simd16Idx = imm8 / simd16Cnt;

    assert((0 <= imm8) && (imm8 < count));

    if (IsContainableMemoryOp(op1))
    {
        // We will specially handle GetElement when op1 is already in memory

        if (op1->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            // We want to optimize GetElement down to a LclFld where possible as
            // this unlocks additional containment opportunities for various nodes

            GenTreeLclVarCommon* lclVar  = op1->AsLclVarCommon();
            uint32_t             lclOffs = lclVar->GetLclOffs() + (imm8 * elemSize);
            LclVarDsc*           lclDsc  = comp->lvaGetDesc(lclVar);

            if (lclDsc->lvDoNotEnregister && (lclOffs <= 0xFFFF) && ((lclOffs + elemSize) <= lclDsc->lvExactSize()))
            {
                GenTree* lclFld = comp->gtNewLclFldNode(lclVar->GetLclNum(), JITtype2varType(simdBaseJitType),
                                                        static_cast<uint16_t>(lclOffs));
                BlockRange().InsertBefore(node, lclFld);

                LIR::Use use;
                if (BlockRange().TryGetUse(node, &use))
                {
                    use.ReplaceWith(lclFld);
                }
                else
                {
                    lclFld->SetUnusedValue();
                }

                BlockRange().Remove(op1);
                BlockRange().Remove(op2);
                BlockRange().Remove(node);

                return LowerNode(lclFld);
            }
        }

        if (IsSafeToContainMem(node, op1))
        {
            // Handle other cases in codegen

            op2->AsIntCon()->SetIconValue(imm8);
            ContainCheckHWIntrinsic(node);

            return node->gtNext;
        }
    }

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
#if defined(TARGET_AMD64)
        case TYP_LONG:
        case TYP_ULONG:
#endif // TARGET_AMD64
        {
            // Using software fallback if simdBaseType is not supported by hardware
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41));
            break;
        }

        case TYP_DOUBLE:
        case TYP_FLOAT:
        case TYP_SHORT:
        case TYP_USHORT:
        {
            assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE2));
            break;
        }

        default:
        {
            unreached();
        }
    }

    // Remove the index node up front to simplify downstream logic
    BlockRange().Remove(op2);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;

    if (intrinsicId == NI_Vector512_GetElement)
    {
        assert(comp->IsBaselineVector512IsaSupportedDebugOnly());

        if (simd16Idx == 0)
        {
            // We will be constructing the following parts:
            //   ...
            //         /--*  op1  simd64
            //   op1 = *  HWINTRINSIC   simd64 T GetLower128

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower().GetLower();

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector512_GetLower128, simdBaseJitType, simdSize);
            BlockRange().InsertBefore(node, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            assert((simd16Idx >= 1) && (simd16Idx <= 3));

            // We will be constructing the following parts:
            //   ...

            //          /--*  op1  simd64
            //          +--*  idx  int
            //   tmp1 = *  HWINTRINSIC   simd64 T ExtractVector128

            // This is roughly the following managed code:
            //   ...
            //   tmp1  = Avx512F.ExtractVector128(op1, idx);

            imm8 -= (simd16Idx * simd16Cnt);

            idx = comp->gtNewIconNode(simd16Idx);
            BlockRange().InsertBefore(node, idx);
            LowerNode(idx);

            NamedIntrinsic extractIntrinsicId = NI_AVX512F_ExtractVector128;

            if ((genTypeSize(simdBaseType) == 8) && !comp->compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
            {
                extractIntrinsicId = NI_AVX512DQ_ExtractVector128;
            }

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, idx, extractIntrinsicId, simdBaseJitType, simdSize);
            BlockRange().InsertBefore(node, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;
    }
    else if (intrinsicId == NI_Vector256_GetElement)
    {
        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        if (simd16Idx == 0)
        {
            // We will be constructing the following parts:
            //   ...
            //         /--*  op1  simd32
            //   op1 = *  HWINTRINSIC   simd32 T GetLower

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower();

            tmp1 = comp->gtNewSimdGetLowerNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);
            BlockRange().InsertBefore(node, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            assert(simd16Idx == 1);

            // We will be constructing the following parts:
            //   ...

            //          /--*  op1   simd32
            //   tmp1 = *  HWINTRINSIC   simd32 T GetUpper

            // This is roughly the following managed code:
            //   ...
            //   tmp1  = op1.GetUpper();

            imm8 -= count / 2;

            tmp1 = comp->gtNewSimdGetUpperNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);
            BlockRange().InsertBefore(node, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;
    }

    NamedIntrinsic resIntrinsic = NI_Illegal;

    if (imm8 == 0)
    {
        // Specially handle as ToScalar

        node->SetSimdSize(16);
        node->ResetHWIntrinsicId(NI_Vector128_ToScalar, op1);

        return LowerNode(node);
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

        node->SetSimdSize(16);
        node->ResetHWIntrinsicId(resIntrinsic, op1, op2);
    }

    GenTree* next = node->gtNext;

    if (node->GetHWIntrinsicId() != intrinsicId)
    {
        next = LowerNode(node);
    }
    else
    {
        ContainCheckHWIntrinsic(node);
    }

    if ((simdBaseType == TYP_BYTE) || (simdBaseType == TYP_SHORT))
    {
        // The extract intrinsics zero the upper bits, so we need an explicit
        // cast to ensure the result is properly sign extended

        LIR::Use use;

        bool foundUse     = BlockRange().TryGetUse(node, &use);
        bool fromUnsigned = false;

        GenTreeCast* cast = comp->gtNewCastNode(TYP_INT, node, fromUnsigned, simdBaseType);
        BlockRange().InsertAfter(node, cast);

        if (foundUse)
        {
            use.ReplaceWith(cast);
        }
        else
        {
            node->ClearUnusedValue();
            cast->SetUnusedValue();
        }
        next = LowerNode(cast);
    }

    return next;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicWithElement: Lowers a Vector128 or Vector256 WithElement call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicWithElement(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    var_types      simdType        = node->TypeGet();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);
    GenTree* op3 = node->Op(3);

    assert(op2->OperIsConst());

    ssize_t count     = simdSize / genTypeSize(simdBaseType);
    ssize_t imm8      = op2->AsIntCon()->IconValue();
    ssize_t simd16Cnt = 16 / genTypeSize(simdBaseType);
    ssize_t simd16Idx = imm8 / simd16Cnt;

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
    GenTree*            idx    = nullptr;
    GenTree*            tmp1   = nullptr;
    GenTree*            tmp2   = nullptr;
    GenTreeHWIntrinsic* result = node;

    if (intrinsicId == NI_Vector512_WithElement)
    {
        // If we have a simd64 WithElement, we will spill the original
        // simd64 source into a local, extract the relevant simd16 from
        // it and then operate on that. At the end, we will insert the simd16
        // result back into the simd64 local, producing our final value.

        assert(comp->IsBaselineVector512IsaSupportedDebugOnly());

        // This copy of "node" will have the simd16 value we need.
        result = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, op3, intrinsicId, simdBaseJitType, 16);
        BlockRange().InsertBefore(node, result);

        // We will be constructing the following parts:
        //   ...
        //          /--*  op1 simd64
        //          *  STORE_LCL_VAR simd64
        //  tmp64 =    LCL_VAR       simd64
        //  op1   =    LCL_VAR       simd64

        // TODO-CQ: move the tmp64 node closer to the final InsertVector128.
        LIR::Use op1Use(BlockRange(), &node->Op(1), node);
        ReplaceWithLclVar(op1Use);
        GenTree* tmp64 = node->Op(1);

        op1 = comp->gtClone(tmp64);
        BlockRange().InsertBefore(op3, op1);

        if (simd16Idx == 0)
        {
            // We will be constructing the following parts:
            //   ...
            //         /--*  op1  simd64
            //   op1 = *  HWINTRINSIC   simd64 T GetLower128

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower().GetLower();

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector512_GetLower128, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op1, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            assert((simd16Idx >= 1) && (simd16Idx <= 3));

            // We will be constructing the following parts:
            //   ...

            //          /--*  op1  simd64
            //          +--*  idx  int
            //   tmp1 = *  HWINTRINSIC   simd64 T ExtractVector128

            // This is roughly the following managed code:
            //   ...
            //   tmp1  = Avx512F.ExtractVector128(op1, idx);

            imm8 -= (simd16Idx * simd16Cnt);

            idx = comp->gtNewIconNode(simd16Idx);
            BlockRange().InsertAfter(op1, idx);
            LowerNode(idx);

            NamedIntrinsic extractIntrinsicId = NI_AVX512F_ExtractVector128;

            if ((genTypeSize(simdBaseType) == 8) && !comp->compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
            {
                extractIntrinsicId = NI_AVX512DQ_ExtractVector128;
            }

            tmp1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, idx, extractIntrinsicId, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(idx, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;

        // Now we will insert our "result" into our simd64 temporary.

        idx = comp->gtNewIconNode(simd16Idx);
        BlockRange().InsertBefore(node, idx);
        LowerNode(idx);

        NamedIntrinsic insertIntrinsicId = NI_AVX512F_InsertVector128;

        if ((genTypeSize(simdBaseType) == 8) && !comp->compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
        {
            insertIntrinsicId = NI_AVX512DQ_InsertVector128;
        }

        node->ResetHWIntrinsicId(insertIntrinsicId, comp, tmp64, result, idx);
    }
    else if (intrinsicId == NI_Vector256_WithElement)
    {
        // If we have a simd32 WithElement, we will spill the original
        // simd32 source into a local, extract the lower/upper half from
        // it and then operate on that. At the end, we will insert the simd16
        // result back into the simd32 local, producing our final value.

        assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

        // This copy of "node" will have the simd16 value we need.
        result = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, op3, intrinsicId, simdBaseJitType, 16);
        BlockRange().InsertBefore(node, result);

        // We will be constructing the following parts:
        //   ...
        //          /--*  op1 simd32
        //          *  STORE_LCL_VAR simd32
        //  tmp32 =    LCL_VAR       simd32
        //  op1   =    LCL_VAR       simd32

        // TODO-CQ: move the tmp32 node closer to the final InsertVector128.
        LIR::Use op1Use(BlockRange(), &node->Op(1), node);
        ReplaceWithLclVar(op1Use);
        GenTree* tmp32 = node->Op(1);

        op1 = comp->gtClone(tmp32);
        BlockRange().InsertBefore(op3, op1);

        if (simd16Idx == 0)
        {
            // We will be constructing the following parts:
            //   ...
            //         /--*  op1  simd32
            //   op1 = *  HWINTRINSIC   simd32 T GetLower

            // This is roughly the following managed code:
            //   ...
            //   op1 = op1.GetLower();

            tmp1 = comp->gtNewSimdGetLowerNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op1, tmp1);
            LowerNode(tmp1);
        }
        else
        {
            assert(simd16Idx == 1);

            // We will be constructing the following parts:
            //   ...

            //          /--*  op1   simd32
            //   tmp1 = *  HWINTRINSIC   simd32 T GetUpper

            // This is roughly the following managed code:
            //   ...
            //   tmp1  = op1.GetUpper();

            imm8 -= count / 2;

            tmp1 = comp->gtNewSimdGetUpperNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op1, tmp1);
            LowerNode(tmp1);
        }

        op1 = tmp1;

        // Now we will insert our "result" into our simd32 temporary.
        if (simd16Idx == 0)
        {
            node->ResetHWIntrinsicId(NI_Vector256_WithLower, comp, tmp32, result);
        }
        else
        {
            node->ResetHWIntrinsicId(NI_Vector256_WithUpper, comp, tmp32, result);
        }
    }
    else
    {
        assert(simd16Idx == 0);
    }

    switch (simdBaseType)
    {
        case TYP_LONG:
        case TYP_ULONG:
        {
            idx = comp->gtNewIconNode(imm8);
            BlockRange().InsertBefore(result, idx);
            result->ChangeHWIntrinsicId(NI_SSE41_X64_Insert, op1, op3, idx);
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

            tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op3, CORINFO_TYPE_FLOAT, 16);
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

                    result->ResetHWIntrinsicId(NI_SSE_MoveScalar, op1, tmp1);
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

                    result->Op(1) = op1;
                    LIR::Use op1Use(BlockRange(), &result->Op(1), result);
                    ReplaceWithLclVar(op1Use);
                    op2 = result->Op(1);

                    tmp2 = comp->gtClone(op2);
                    BlockRange().InsertAfter(tmp1, tmp2);

                    ssize_t controlBits1;
                    ssize_t controlBits2;

                    // The comments beside the control bits below are listed using the managed API operands
                    //
                    // In practice, for the first step the value being inserted (op3) is in tmp1
                    // while the other elements of the result (op1) are in tmp2. The result ends
                    // up containing the value being inserted and its immediate neighbor.
                    //
                    // The second step takes that result (which is in op1) plus the other elements
                    // from op2 (a clone of op1/tmp2 from the previous step) and combines them to
                    // create the final result.

                    switch (imm8)
                    {
                        case 1:
                        {
                            controlBits1 = 0;   // 00 00 00 00;  op1 = { X = op3,   Y = op3,   Z = op1.X, W = op1.X }
                            controlBits2 = 226; // 11 10 00 10; node = { X = op1.X, Y = op3,   Z = op1.Z, W = op1.W }
                            break;
                        }

                        case 2:
                        {
                            controlBits1 = 15; // 00 00 11 11;  op1 = { X = op1.W, Y = op1.W, Z = op3, W = op3 }
                            controlBits2 = 36; // 00 10 01 00; node = { X = op1.X, Y = op1.Y, Z = op3, W = op1.W }
                            break;
                        }

                        case 3:
                        {
                            controlBits1 = 10;  // 00 00 10 10;  op1 = { X = op1.Z, Y = op1.Z, Z = op3,   W = op3 }
                            controlBits2 = 132; // 10 00 01 00; node = { X = op1.X, Y = op1.Y, Z = op1.Z, W = op3 }
                            break;
                        }

                        default:
                            unreached();
                    }

                    idx = comp->gtNewIconNode(controlBits1);
                    BlockRange().InsertAfter(tmp2, idx);

                    if (imm8 != 1)
                    {
                        std::swap(tmp1, tmp2);
                    }

                    op1 = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, tmp2, idx, NI_SSE_Shuffle,
                                                         CORINFO_TYPE_FLOAT, 16);
                    BlockRange().InsertAfter(idx, op1);
                    LowerNode(op1);

                    idx = comp->gtNewIconNode(controlBits2);
                    BlockRange().InsertAfter(op1, idx);

                    if (imm8 != 1)
                    {
                        std::swap(op1, op2);
                    }

                    result->ChangeHWIntrinsicId(NI_SSE_Shuffle, op1, op2, idx);
                }
                break;
            }
            else
            {
                imm8 = imm8 * 16;
                op3  = tmp1;
                FALLTHROUGH;
            }
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
        {
            idx = comp->gtNewIconNode(imm8);
            BlockRange().InsertBefore(result, idx);
            result->ChangeHWIntrinsicId(NI_SSE41_Insert, op1, op3, idx);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            idx = comp->gtNewIconNode(imm8);
            BlockRange().InsertBefore(result, idx);
            result->ChangeHWIntrinsicId(NI_SSE2_Insert, op1, op3, idx);
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

            tmp1 = InsertNewSimdCreateScalarUnsafeNode(TYP_SIMD16, op3, CORINFO_TYPE_DOUBLE, 16);
            LowerNode(tmp1);

            result->ResetHWIntrinsicId((imm8 == 0) ? NI_SSE2_MoveScalar : NI_SSE2_UnpackLow, op1, tmp1);
            break;
        }

        default:
            unreached();
    }

    assert(result->GetHWIntrinsicId() != intrinsicId);
    GenTree* nextNode = LowerNode(result);

    if (intrinsicId == NI_Vector512_WithElement)
    {
        // Now that we have finalized the shape of the tree, lower the insertion node as well.

        assert((node->GetHWIntrinsicId() == NI_AVX512F_InsertVector128) ||
               (node->GetHWIntrinsicId() == NI_AVX512DQ_InsertVector128));
        assert(node != result);

        nextNode = LowerNode(node);
    }
    else if (intrinsicId == NI_Vector256_WithElement)
    {
        // Now that we have finalized the shape of the tree, lower the insertion node as well.

        assert((node->GetHWIntrinsicId() == NI_Vector256_WithLower) ||
               (node->GetHWIntrinsicId() == NI_Vector256_WithUpper));
        assert(node != result);

        nextNode = LowerNode(node);
    }
    else
    {
        assert(node == result);
    }

    return nextNode;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector128 or Vector256 Dot call
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
    unsigned       simd16Count     = comp->getSIMDVectorLength(16, simdBaseType);

    assert((intrinsicId == NI_Vector128_Dot) || (intrinsicId == NI_Vector256_Dot));
    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);
    assert(varTypeIsSIMD(node));

    GenTree* op1 = node->Op(1);
    GenTree* op2 = node->Op(2);

    // Spare GenTrees to be used for the lowering logic below
    // Defined upfront to avoid naming conflicts, etc...
    GenTree* idx  = nullptr;
    GenTree* tmp1 = nullptr;
    GenTree* tmp2 = nullptr;
    GenTree* tmp3 = nullptr;

    NamedIntrinsic horizontalAdd = NI_Illegal;
    NamedIntrinsic shuffle       = NI_Illegal;

    if (simdSize == 32)
    {
        switch (simdBaseType)
        {
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            case TYP_UINT:
            {
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX2));
                horizontalAdd = NI_AVX2_HorizontalAdd;
                break;
            }

            case TYP_FLOAT:
            {
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));

                // We will be constructing the following parts:
                //   idx  =    CNS_INT       int    0xFF
                //          /--*  op1  simd16
                //          +--*  op2  simd16
                //          +--*  idx  int
                //   tmp1 = *  HWINTRINSIC   simd32 T DotProduct
                //          /--*  tmp1 simd32
                //          *  STORE_LCL_VAR simd32
                //   tmp1 =    LCL_VAR       simd32
                //   tmp2 =    LCL_VAR       simd32
                //   tmp3 =    LCL_VAR       simd32
                //          /--*  tmp2 simd32
                //          +--*  tmp3 simd32
                //          +--*  CNS_INT    int    0x01
                //   tmp2 = *  HWINTRINSIC   simd32 T Permute
                //          /--*  tmp1 simd32
                //          +--*  tmp2 simd32
                //   node = *  HWINTRINSIC   simd32 T Add

                // This is roughly the following managed code:
                //   var tmp1 = Avx.DotProduct(op1, op2, 0xFF);
                //   var tmp2 = Avx.Permute2x128(tmp1, tmp1, 0x4E);
                //   return Avx.Add(tmp1, tmp2);

                idx = comp->gtNewIconNode(0xFF, TYP_INT);
                BlockRange().InsertBefore(node, idx);

                tmp1 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_AVX_DotProduct, simdBaseJitType,
                                                      simdSize);
                BlockRange().InsertAfter(idx, tmp1);
                LowerNode(tmp1);

                node->Op(1) = tmp1;
                LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp1Use);
                tmp1 = node->Op(1);

                tmp2 = comp->gtClone(tmp1);
                BlockRange().InsertAfter(tmp1, tmp2);

                tmp3 = comp->gtClone(tmp2);
                BlockRange().InsertAfter(tmp2, tmp3);

                idx = comp->gtNewIconNode(0x01, TYP_INT);
                BlockRange().InsertAfter(tmp3, idx);

                tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, tmp3, idx, NI_AVX_Permute2x128, simdBaseJitType,
                                                      simdSize);
                BlockRange().InsertAfter(idx, tmp2);
                LowerNode(tmp2);

                tmp1 = comp->gtNewSimdBinOpNode(GT_ADD, simdType, tmp1, tmp2, simdBaseJitType, simdSize);
                BlockRange().InsertAfter(tmp2, tmp1);

                // We're producing a vector result, so just return the result directly
                LIR::Use use;

                if (BlockRange().TryGetUse(node, &use))
                {
                    use.ReplaceWith(tmp1);
                }
                else
                {
                    tmp1->SetUnusedValue();
                }

                BlockRange().Remove(node);
                return LowerNode(tmp1);
            }

            case TYP_DOUBLE:
            {
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_AVX));
                horizontalAdd = NI_AVX_HorizontalAdd;
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
                horizontalAdd = NI_SSSE3_HorizontalAdd;

                if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSSE3))
                {
                    shuffle = NI_SSE2_ShuffleLow;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                assert(comp->compIsaSupportedDebugOnly(InstructionSet_SSE41));
                horizontalAdd = NI_SSSE3_HorizontalAdd;
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
                        idx = comp->gtNewIconNode(0x3F, TYP_INT);
                    }
                    else if (simdSize == 12)
                    {
                        idx = comp->gtNewIconNode(0x7F, TYP_INT);
                    }
                    else
                    {
                        assert(simdSize == 16);
                        idx = comp->gtNewIconNode(0xFF, TYP_INT);
                    }
                    BlockRange().InsertBefore(node, idx);

                    if (varTypeIsSIMD(node->gtType))
                    {
                        // We're producing a vector result, so just emit DotProduct directly
                        node->ResetHWIntrinsicId(NI_SSE41_DotProduct, comp, op1, op2, idx);
                    }
                    else
                    {
                        // We're producing a scalar result, so we only need the result in element 0
                        //
                        // However, doing that would break/limit CSE and requires a partial write so
                        // it's better to just broadcast the value to the entire vector

                        tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_SSE41_DotProduct,
                                                              simdBaseJitType, simdSize);
                        BlockRange().InsertAfter(idx, tmp3);
                        LowerNode(tmp3);

                        node->ResetHWIntrinsicId(NI_Vector128_ToScalar, tmp3);
                    }

                    return LowerNode(node);
                }

                horizontalAdd = NI_SSE3_HorizontalAdd;

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

                    idx = comp->gtNewIconNode(0x33, TYP_INT);
                    BlockRange().InsertBefore(node, idx);

                    if (varTypeIsSIMD(node->gtType))
                    {
                        // We're producing a vector result, so just emit DotProduct directly
                        node->ResetHWIntrinsicId(NI_SSE41_DotProduct, comp, op1, op2, idx);
                    }
                    else
                    {
                        // We're producing a scalar result, so we only need the result in element 0
                        //
                        // However, doing that would break/limit CSE and requires a partial write so
                        // it's better to just broadcast the value to the entire vector

                        tmp3 = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, idx, NI_SSE41_DotProduct,
                                                              simdBaseJitType, simdSize);
                        BlockRange().InsertAfter(idx, tmp3);
                        LowerNode(tmp3);

                        node->ResetHWIntrinsicId(NI_Vector128_ToScalar, tmp3);
                    }

                    return LowerNode(node);
                }

                horizontalAdd = NI_SSE3_HorizontalAdd;

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

            // We need to mask off the most significant element to avoid the shuffle + add
            // from including it in the computed result. We need to do this for both op1 and
            // op2 in case one of them is `NaN` (because Zero * NaN == NaN)

            simd16_t simd16Val = {};

            simd16Val.i32[0] = -1;
            simd16Val.i32[1] = -1;
            simd16Val.i32[2] = -1;
            simd16Val.i32[3] = +0;

            simdType = TYP_SIMD16;
            simdSize = 16;

            // We will be constructing the following parts:
            //   ...
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    0
            //   tmp1 = *  HWINTRINSIC   simd16 T Create
            //          /--*  op1 simd16
            //          +--*  tmp1 simd16
            //   op1  = *  HWINTRINSIC   simd16 T And
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   tmp1 = Vector128.Create(-1, -1, -1, 0);
            //   op1  = Sse.And(op1, tmp1);
            //   ...

            GenTreeVecCon* vecCon1 = comp->gtNewVconNode(simdType);
            memcpy(&vecCon1->gtSimdVal, &simd16Val, sizeof(simd16_t));
            BlockRange().InsertAfter(op1, vecCon1);

            op1 = comp->gtNewSimdBinOpNode(GT_AND, simdType, op1, vecCon1, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(vecCon1, op1);

            LowerNode(vecCon1);
            LowerNode(op1);

            // We will be constructing the following parts:
            //   ...
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    -1
            //          +--*  CNS_INT    int    0
            //   tmp2 = *  HWINTRINSIC   simd16 T Create
            //          /--*  op2 simd16
            //          +--*  tmp2 simd16
            //   op2  = *  HWINTRINSIC   simd16 T And
            //   ...

            // This is roughly the following managed code:
            //   ...
            //   tmp2 = Vector128.Create(-1, -1, -1, 0);
            //   op2  = Sse.And(op2, tmp2);
            //   ...

            GenTreeVecCon* vecCon2 = comp->gtNewVconNode(simdType);
            memcpy(&vecCon2->gtSimdVal, &simd16Val, sizeof(simd16_t));
            BlockRange().InsertAfter(op2, vecCon2);

            op2 = comp->gtNewSimdBinOpNode(GT_AND, simdType, op2, vecCon2, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(vecCon2, op2);

            LowerNode(vecCon2);
            LowerNode(op2);
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

    tmp1 = comp->gtNewSimdBinOpNode(GT_MUL, simdType, op1, op2, simdBaseJitType, simdSize);
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

        node->Op(1) = tmp1;
        LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
        ReplaceWithLclVar(tmp1Use);
        tmp1 = node->Op(1);

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

                    shuffleConst = 0x4E;
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

                node->Op(1) = tmp2;
                LIR::Use tmp2Use(BlockRange(), &node->Op(1), node);
                ReplaceWithLclVar(tmp2Use);
                tmp2 = node->Op(1);

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

            tmp1 = comp->gtNewSimdBinOpNode(GT_ADD, simdType, tmp1, tmp2, simdBaseJitType, simdSize);
        }

        BlockRange().InsertAfter(tmp2, tmp1);
        LowerNode(tmp1);
    }

    if (simdSize == 32)
    {
        // We will be constructing the following parts:
        //   ...
        //          /--*  tmp1 simd32
        //          *  STORE_LCL_VAR simd32
        //   tmp1 =    LCL_VAR       simd32
        //   tmp2 =    LCL_VAR       simd32
        //          /--*  tmp2 simd32
        //          +--*  CNS_INT    int    0x01
        //   tmp2 = *  HWINTRINSIC   simd32 float Permute
        //          /--*  tmp1 simd32
        //          +--*  tmp2 simd32
        //   tmp1 = *  HWINTRINSIC   simd32 T Add
        //   ...

        // This is roughly the following managed code:
        //   ...
        //   var tmp2 = Isa.Permute2x128(tmp1, tmp2, 0x01);
        //   tmp1 = Isa.Add(tmp1, tmp2);
        //   ...

        assert(simdBaseType != TYP_FLOAT);

        node->Op(1) = tmp1;
        LIR::Use tmp1Use(BlockRange(), &node->Op(1), node);
        ReplaceWithLclVar(tmp1Use);
        tmp1 = node->Op(1);

        tmp2 = comp->gtClone(tmp1);
        BlockRange().InsertAfter(tmp1, tmp2);

        tmp3 = comp->gtClone(tmp2);
        BlockRange().InsertAfter(tmp2, tmp3);

        idx = comp->gtNewIconNode(0x01, TYP_INT);
        BlockRange().InsertAfter(tmp3, idx);

        NamedIntrinsic permute2x128 = (simdBaseType == TYP_DOUBLE) ? NI_AVX_Permute2x128 : NI_AVX2_Permute2x128;

        tmp2 = comp->gtNewSimdHWIntrinsicNode(simdType, tmp2, tmp3, idx, permute2x128, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(idx, tmp2);
        LowerNode(tmp2);

        tmp1 = comp->gtNewSimdBinOpNode(GT_ADD, simdType, tmp1, tmp2, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(tmp2, tmp1);
        LowerNode(tmp1);
    }

    // We're producing a vector result, so just return the result directly
    LIR::Use use;

    if (BlockRange().TryGetUse(node, &use))
    {
        use.ReplaceWith(tmp1);
    }
    else
    {
        tmp1->SetUnusedValue();
    }

    BlockRange().Remove(node);
    return tmp1->gtNext;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicToScalar: Lowers a Vector128 or Vector256 ToScalar call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicToScalar(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId     = node->GetHWIntrinsicId();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    assert((intrinsicId == NI_Vector128_ToScalar) || (intrinsicId == NI_Vector256_ToScalar) ||
           (intrinsicId == NI_Vector512_ToScalar));

    assert(varTypeIsSIMD(simdType));
    assert(varTypeIsArithmetic(simdBaseType));
    assert(simdSize != 0);

    GenTree* op1 = node->Op(1);

    if (IsContainableMemoryOp(op1))
    {
        // We will specially handle ToScalar when op1 is already in memory

        if (op1->OperIs(GT_IND))
        {
            // We want to optimize ToScalar down to an Indir where possible as
            // this unlocks additional containment opportunities for various nodes

            GenTreeIndir* indir = op1->AsIndir();

            GenTreeIndir* newIndir =
                comp->gtNewIndir(JITtype2varType(simdBaseJitType), indir->Addr(), (indir->gtFlags & GTF_IND_FLAGS));
            BlockRange().InsertBefore(node, newIndir);

            LIR::Use use;
            if (BlockRange().TryGetUse(node, &use))
            {
                use.ReplaceWith(newIndir);
            }
            else
            {
                newIndir->SetUnusedValue();
            }

            BlockRange().Remove(op1);
            BlockRange().Remove(node);

            return LowerNode(newIndir);
        }

        if (op1->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            uint32_t elemSize = genTypeSize(simdBaseType);

            // We want to optimize ToScalar down to a LclFld where possible as
            // this unlocks additional containment opportunities for various nodes

            GenTreeLclVarCommon* lclVar  = op1->AsLclVarCommon();
            uint32_t             lclOffs = lclVar->GetLclOffs() + (0 * elemSize);
            LclVarDsc*           lclDsc  = comp->lvaGetDesc(lclVar);

            if (lclDsc->lvDoNotEnregister && (lclOffs <= 0xFFFF) && ((lclOffs + elemSize) <= lclDsc->lvExactSize()))
            {
                GenTree* lclFld =
                    comp->gtNewLclFldNode(lclVar->GetLclNum(), JITtype2varType(simdBaseJitType), lclVar->GetLclOffs());
                BlockRange().InsertBefore(node, lclFld);

                LIR::Use use;
                if (BlockRange().TryGetUse(node, &use))
                {
                    use.ReplaceWith(lclFld);
                }
                else
                {
                    lclFld->SetUnusedValue();
                }

                BlockRange().Remove(op1);
                BlockRange().Remove(node);

                return LowerNode(lclFld);
            }
        }

        if (IsSafeToContainMem(node, op1))
        {
            // Handle other cases in codegen
            ContainCheckHWIntrinsic(node);
            return node->gtNext;
        }
    }

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_SHORT:
        case TYP_INT:
        {
            node->gtType = TYP_INT;
            node->SetSimdBaseJitType(CORINFO_TYPE_INT);
            node->ChangeHWIntrinsicId(NI_SSE2_ConvertToInt32);
            break;
        }

        case TYP_UBYTE:
        case TYP_USHORT:
        case TYP_UINT:
        {
            node->gtType = TYP_INT;
            node->SetSimdBaseJitType(CORINFO_TYPE_UINT);
            node->ChangeHWIntrinsicId(NI_SSE2_ConvertToUInt32);
            break;
        }

#if defined(TARGET_AMD64)
        case TYP_LONG:
        {
            node->ChangeHWIntrinsicId(NI_SSE2_X64_ConvertToInt64);
            break;
        }

        case TYP_ULONG:
        {
            node->ChangeHWIntrinsicId(NI_SSE2_X64_ConvertToUInt64);
            break;
        }
#endif // TARGET_AMD64

        case TYP_FLOAT:
        case TYP_DOUBLE:
        {
            ContainCheckHWIntrinsic(node);
            return node->gtNext;
        }

        default:
        {
            unreached();
        }
    }

    GenTree* next = LowerNode(node);

    if (genTypeSize(simdBaseType) < 4)
    {
        // The move intrinsics do not touch the upper bits, so we need an explicit
        // cast to ensure the result is properly sign extended

        LIR::Use use;

        bool foundUse     = BlockRange().TryGetUse(node, &use);
        bool fromUnsigned = varTypeIsUnsigned(simdBaseType);

        GenTreeCast* cast = comp->gtNewCastNode(TYP_INT, node, fromUnsigned, simdBaseType);
        BlockRange().InsertAfter(node, cast);

        if (foundUse)
        {
            use.ReplaceWith(cast);
        }
        else
        {
            node->ClearUnusedValue();
            cast->SetUnusedValue();
        }
        next = LowerNode(cast);
    }

    return next;
}

//----------------------------------------------------------------------------------------------
// Lowering::TryLowerAndOpToResetLowestSetBit: Lowers a tree AND(X, ADD(X, -1)) to HWIntrinsic::ResetLowestSetBit
//
// Arguments:
//    andNode - GT_AND node of integral type
//
// Return Value:
//    Returns the replacement node if one is created else nullptr indicating no replacement
//
// Notes:
//    Performs containment checks on the replacement node if one is created
GenTree* Lowering::TryLowerAndOpToResetLowestSetBit(GenTreeOp* andNode)
{
    assert(andNode->OperIs(GT_AND) && varTypeIsIntegral(andNode));

    GenTree* op1 = andNode->gtGetOp1();
    if (!op1->OperIs(GT_LCL_VAR) || comp->lvaGetDesc(op1->AsLclVar())->IsAddressExposed())
    {
        return nullptr;
    }

    GenTree* op2 = andNode->gtGetOp2();
    if (!op2->OperIs(GT_ADD))
    {
        return nullptr;
    }

    GenTree* addOp2 = op2->gtGetOp2();
    if (!addOp2->IsIntegralConst(-1))
    {
        return nullptr;
    }

    GenTree* addOp1 = op2->gtGetOp1();
    if (!addOp1->OperIs(GT_LCL_VAR) || (addOp1->AsLclVar()->GetLclNum() != op1->AsLclVar()->GetLclNum()))
    {
        return nullptr;
    }

    // Subsequent nodes may rely on CPU flags set by these nodes in which case we cannot remove them
    if (((addOp2->gtFlags & GTF_SET_FLAGS) != 0) || ((op2->gtFlags & GTF_SET_FLAGS) != 0) ||
        ((andNode->gtFlags & GTF_SET_FLAGS) != 0))
    {
        return nullptr;
    }

    NamedIntrinsic intrinsic;
    if (op1->TypeIs(TYP_LONG) && comp->compOpportunisticallyDependsOn(InstructionSet_BMI1_X64))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_X64_ResetLowestSetBit;
    }
    else if (comp->compOpportunisticallyDependsOn(InstructionSet_BMI1))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_ResetLowestSetBit;
    }
    else
    {
        return nullptr;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(andNode, &use))
    {
        return nullptr;
    }

    GenTreeHWIntrinsic* blsrNode = comp->gtNewScalarHWIntrinsicNode(andNode->TypeGet(), op1, intrinsic);

    JITDUMP("Lower: optimize AND(X, ADD(X, -1))\n");
    DISPNODE(andNode);
    JITDUMP("to:\n");
    DISPNODE(blsrNode);

    use.ReplaceWith(blsrNode);

    BlockRange().InsertBefore(andNode, blsrNode);
    BlockRange().Remove(andNode);
    BlockRange().Remove(op2);
    BlockRange().Remove(addOp1);
    BlockRange().Remove(addOp2);

    ContainCheckHWIntrinsic(blsrNode);

    return blsrNode;
}

//----------------------------------------------------------------------------------------------
// Lowering::TryLowerAndOpToExtractLowestSetIsolatedBit: Lowers a tree AND(X, NEG(X)) to
// HWIntrinsic::ExtractLowestSetBit
//
// Arguments:
//    andNode - GT_AND node of integral type
//
// Return Value:
//    Returns the replacement node if one is created else nullptr indicating no replacement
//
// Notes:
//    Performs containment checks on the replacement node if one is created
GenTree* Lowering::TryLowerAndOpToExtractLowestSetBit(GenTreeOp* andNode)
{
    GenTree* opNode  = nullptr;
    GenTree* negNode = nullptr;
    if (andNode->gtGetOp1()->OperIs(GT_NEG))
    {
        negNode = andNode->gtGetOp1();
        opNode  = andNode->gtGetOp2();
    }
    else if (andNode->gtGetOp2()->OperIs(GT_NEG))
    {
        negNode = andNode->gtGetOp2();
        opNode  = andNode->gtGetOp1();
    }

    if (opNode == nullptr)
    {
        return nullptr;
    }

    GenTree* negOp = negNode->AsUnOp()->gtGetOp1();
    if (!negOp->OperIs(GT_LCL_VAR) || !opNode->OperIs(GT_LCL_VAR) ||
        (negOp->AsLclVar()->GetLclNum() != opNode->AsLclVar()->GetLclNum()))
    {
        return nullptr;
    }

    // Subsequent nodes may rely on CPU flags set by these nodes in which case we cannot remove them
    if (((opNode->gtFlags & GTF_SET_FLAGS) != 0) || ((negNode->gtFlags & GTF_SET_FLAGS) != 0))
    {
        return nullptr;
    }

    NamedIntrinsic intrinsic;
    if (andNode->TypeIs(TYP_LONG) && comp->compOpportunisticallyDependsOn(InstructionSet_BMI1_X64))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_X64_ExtractLowestSetBit;
    }
    else if (comp->compOpportunisticallyDependsOn(InstructionSet_BMI1))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_ExtractLowestSetBit;
    }
    else
    {
        return nullptr;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(andNode, &use))
    {
        return nullptr;
    }

    GenTreeHWIntrinsic* blsiNode = comp->gtNewScalarHWIntrinsicNode(andNode->TypeGet(), opNode, intrinsic);

    JITDUMP("Lower: optimize AND(X, NEG(X)))\n");
    DISPNODE(andNode);
    JITDUMP("to:\n");
    DISPNODE(blsiNode);

    use.ReplaceWith(blsiNode);

    BlockRange().InsertBefore(andNode, blsiNode);
    BlockRange().Remove(andNode);
    BlockRange().Remove(negNode);
    BlockRange().Remove(negOp);

    ContainCheckHWIntrinsic(blsiNode);

    return blsiNode;
}

//----------------------------------------------------------------------------------------------
// Lowering::TryLowerAndOpToAndNot: Lowers a tree AND(X, NOT(Y)) to HWIntrinsic::AndNot
//
// Arguments:
//    andNode - GT_AND node of integral type
//
// Return Value:
//    Returns the replacement node if one is created else nullptr indicating no replacement
//
// Notes:
//    Performs containment checks on the replacement node if one is created
GenTree* Lowering::TryLowerAndOpToAndNot(GenTreeOp* andNode)
{
    assert(andNode->OperIs(GT_AND) && varTypeIsIntegral(andNode));

    GenTree* opNode  = nullptr;
    GenTree* notNode = nullptr;
    if (andNode->gtGetOp1()->OperIs(GT_NOT))
    {
        notNode = andNode->gtGetOp1();
        opNode  = andNode->gtGetOp2();
    }
    else if (andNode->gtGetOp2()->OperIs(GT_NOT))
    {
        notNode = andNode->gtGetOp2();
        opNode  = andNode->gtGetOp1();
    }

    if (opNode == nullptr)
    {
        return nullptr;
    }

    // We want to avoid using "andn" when one of the operands is both a source and the destination and is also coming
    // from memory. In this scenario, we will get smaller and likely faster code by using the RMW encoding of `and`
    if (IsBinOpInRMWStoreInd(andNode))
    {
        return nullptr;
    }

    // Subsequent nodes may rely on CPU flags set by these nodes in which case we cannot remove them
    if (((andNode->gtFlags & GTF_SET_FLAGS) != 0) || ((notNode->gtFlags & GTF_SET_FLAGS) != 0))
    {
        return nullptr;
    }

    NamedIntrinsic intrinsic;
    if (andNode->TypeIs(TYP_LONG) && comp->compOpportunisticallyDependsOn(InstructionSet_BMI1_X64))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_X64_AndNot;
    }
    else if (comp->compOpportunisticallyDependsOn(InstructionSet_BMI1))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_AndNot;
    }
    else
    {
        return nullptr;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(andNode, &use))
    {
        return nullptr;
    }

    // note that parameter order for andn is ~y, x so these are purposefully reversed when creating the node
    GenTreeHWIntrinsic* andnNode =
        comp->gtNewScalarHWIntrinsicNode(andNode->TypeGet(), notNode->AsUnOp()->gtGetOp1(), opNode, intrinsic);

    JITDUMP("Lower: optimize AND(X, NOT(Y)))\n");
    DISPNODE(andNode);
    JITDUMP("to:\n");
    DISPNODE(andnNode);

    use.ReplaceWith(andnNode);

    BlockRange().InsertBefore(andNode, andnNode);
    BlockRange().Remove(andNode);
    BlockRange().Remove(notNode);

    ContainCheckHWIntrinsic(andnNode);

    return andnNode;
}

//----------------------------------------------------------------------------------------------
// Lowering::TryLowerXorOpToGetMaskUpToLowestSetBit: Lowers a tree XOR(X, ADD(X, -1)) to
// HWIntrinsic::GetMaskUpToLowestSetBit
//
// Arguments:
//    xorNode - GT_XOR node of integral type
//
// Return Value:
//    Returns the replacement node if one is created else nullptr indicating no replacement
//
// Notes:
//    Performs containment checks on the replacement node if one is created
GenTree* Lowering::TryLowerXorOpToGetMaskUpToLowestSetBit(GenTreeOp* xorNode)
{
    assert(xorNode->OperIs(GT_XOR) && varTypeIsIntegral(xorNode));

    GenTree* op1 = xorNode->gtGetOp1();
    if (!op1->OperIs(GT_LCL_VAR) || comp->lvaGetDesc(op1->AsLclVar())->IsAddressExposed())
    {
        return nullptr;
    }

    GenTree* op2 = xorNode->gtGetOp2();
    if (!op2->OperIs(GT_ADD))
    {
        return nullptr;
    }

    GenTree* addOp2 = op2->gtGetOp2();
    if (!addOp2->IsIntegralConst(-1))
    {
        return nullptr;
    }

    GenTree* addOp1 = op2->gtGetOp1();
    if (!addOp1->OperIs(GT_LCL_VAR) || (addOp1->AsLclVar()->GetLclNum() != op1->AsLclVar()->GetLclNum()))
    {
        return nullptr;
    }

    // Subsequent nodes may rely on CPU flags set by these nodes in which case we cannot remove them
    if (((addOp2->gtFlags & GTF_SET_FLAGS) != 0) || ((op2->gtFlags & GTF_SET_FLAGS) != 0) ||
        ((xorNode->gtFlags & GTF_SET_FLAGS) != 0))
    {
        return nullptr;
    }

    NamedIntrinsic intrinsic;
    if (xorNode->TypeIs(TYP_LONG) && comp->compOpportunisticallyDependsOn(InstructionSet_BMI1_X64))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_X64_GetMaskUpToLowestSetBit;
    }
    else if (comp->compOpportunisticallyDependsOn(InstructionSet_BMI1))
    {
        intrinsic = NamedIntrinsic::NI_BMI1_GetMaskUpToLowestSetBit;
    }
    else
    {
        return nullptr;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(xorNode, &use))
    {
        return nullptr;
    }

    GenTreeHWIntrinsic* blsmskNode = comp->gtNewScalarHWIntrinsicNode(xorNode->TypeGet(), op1, intrinsic);

    JITDUMP("Lower: optimize XOR(X, ADD(X, -1)))\n");
    DISPNODE(xorNode);
    JITDUMP("to:\n");
    DISPNODE(blsmskNode);

    use.ReplaceWith(blsmskNode);

    BlockRange().InsertBefore(xorNode, blsmskNode);
    BlockRange().Remove(xorNode);
    BlockRange().Remove(op2);
    BlockRange().Remove(addOp1);
    BlockRange().Remove(addOp2);

    ContainCheckHWIntrinsic(blsmskNode);

    return blsmskNode;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerBswapOp: Tries to contain GT_BSWAP node when possible
//
// Arguments:
//    node - GT_BSWAP node to contain
//
// Notes:
//    Containment is not performed when optimizations are disabled
//    or when MOVBE instruction set is not found
//
void Lowering::LowerBswapOp(GenTreeOp* node)
{
    assert(node->OperIs(GT_BSWAP, GT_BSWAP16));

    if (!comp->opts.OptimizationEnabled() || !comp->compOpportunisticallyDependsOn(InstructionSet_MOVBE))
    {
        return;
    }

    GenTree* operand  = node->gtGetOp1();
    unsigned swapSize = node->OperIs(GT_BSWAP16) ? 2 : genTypeSize(node);
    if ((swapSize == genTypeSize(operand)) && IsContainableMemoryOp(operand) && IsSafeToContainMem(node, operand))
    {
        MakeSrcContained(node, operand);
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
//     a) The gtInd child is a lea/lclVar/lclVarAddr/constant
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
    if (!indirDst->OperIs(GT_LEA, GT_LCL_VAR, GT_CNS_INT) && !indirDst->IsLclVarAddr())
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
    assert(tree->OperIsCommutative() || tree->OperIsCompare() || tree->OperIs(GT_CMP, GT_TEST));

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
        LclVarDsc* v1 = comp->lvaGetDesc(op1->AsLclVarCommon());
        LclVarDsc* v2 = comp->lvaGetDesc(op2->AsLclVarCommon());

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

    if ((node->gtFlags & GTF_IND_REQ_ADDR_IN_REG) != 0)
    {
        // The address of an indirection that requires its address in a reg.
        // Skip any further processing that might otherwise make it contained.
    }
    else if (addr->OperIs(GT_LCL_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_LCL_ADDR is a stack addr mode.

        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(node, addr);
    }
    else if (addr->IsCnsIntOrI())
    {
        GenTreeIntConCommon* icon = addr->AsIntConCommon();

#if defined(FEATURE_SIMD)
        if (((addr->TypeGet() != TYP_SIMD12) || !icon->ImmedValNeedsReloc(comp)) && icon->FitsInAddrBase(comp))
#else
        if (icon->FitsInAddrBase(comp))
#endif
        {
            // On x86, direct VSD is done via a relative branch, and in fact it MUST be contained.
            //
            // Noting we cannot contain relocatable constants for TYP_SIMD12 today. Doing so would
            // require more advanced changes to the emitter so we can correctly track the handle and
            // the 8-byte offset needed for the second load/store used to process the upper element.

            MakeSrcContained(node, addr);
        }
    }
    else if ((addr->OperGet() == GT_LEA) && IsInvariantInRange(addr, node))
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
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    GenTree* src = node->Data();

    if (IsContainableImmed(node, src) && (!src->IsIntegralConst(0) || varTypeIsSmall(node)))
    {
        MakeSrcContained(node, src);
    }

    // If the source is a BSWAP, contain it on supported hardware to generate a MOVBE.
    if (comp->opts.OptimizationEnabled())
    {
        if (src->OperIs(GT_BSWAP, GT_BSWAP16) && comp->compOpportunisticallyDependsOn(InstructionSet_MOVBE))
        {
            unsigned swapSize = src->OperIs(GT_BSWAP16) ? 2 : genTypeSize(src);

            if ((swapSize == genTypeSize(node)) && IsInvariantInRange(src, node))
            {
                // Prefer containing in the store in case the load has been contained.
                src->gtGetOp1()->ClearContained();

                MakeSrcContained(node, src);
            }
        }
#if defined(FEATURE_HW_INTRINSICS)
        else if (src->OperIsHWIntrinsic())
        {
            GenTreeHWIntrinsic* hwintrinsic        = src->AsHWIntrinsic();
            NamedIntrinsic      intrinsicId        = hwintrinsic->GetHWIntrinsicId();
            var_types           simdBaseType       = hwintrinsic->GetSimdBaseType();
            bool                isContainable      = false;
            GenTree*            clearContainedNode = nullptr;

            switch (intrinsicId)
            {
                case NI_Vector128_ToScalar:
                case NI_Vector256_ToScalar:
                case NI_Vector512_ToScalar:
                {
                    if (varTypeIsFloating(simdBaseType))
                    {
                        // These intrinsics are "ins reg/mem, xmm" or "ins xmm, reg/mem"
                        //
                        // In the case we are coming from and going to memory, we want to
                        // preserve the original containment as we'll end up emitting:
                        //    movss xmm0, [addr1]           ; Size: 4, Latency: 4-7,  TP: 0.5
                        //    movss [addr2], xmm0           ; Size: 4, Latency: 4-10, TP: 1
                        //
                        // However, we want to prefer containing the store over allowing the
                        // input to be regOptional, so track and clear containment if required.

                        clearContainedNode = hwintrinsic->Op(1);
                        isContainable      = !clearContainedNode->isContained();
                    }
                    break;
                }

                case NI_SSE2_ConvertToInt32:
                case NI_SSE2_ConvertToUInt32:
                case NI_SSE2_X64_ConvertToInt64:
                case NI_SSE2_X64_ConvertToUInt64:
                case NI_AVX2_ConvertToInt32:
                case NI_AVX2_ConvertToUInt32:
                {
                    // These intrinsics are "ins reg/mem, xmm"
                    isContainable = varTypeIsIntegral(simdBaseType) && (genTypeSize(src) == genTypeSize(node));
                    break;
                }

                case NI_Vector128_GetElement:
                {
                    // GetElement for floating-point is specially handled since double
                    // doesn't have a direct "extract" instruction and float cannot extract
                    // to a SIMD register.
                    //
                    // However, we still want to do the efficient thing and write directly
                    // to memory in the case where the extract is immediately used by a store

                    if (varTypeIsFloating(simdBaseType) && hwintrinsic->Op(2)->IsCnsIntOrI())
                    {
                        assert(!hwintrinsic->Op(2)->IsIntegralConst(0));

                        if (simdBaseType == TYP_FLOAT)
                        {
                            // SSE41_Extract is "extractps reg/mem, xmm, imm8"
                            //
                            // In the case we are coming from and going to memory, we want to
                            // preserve the original containment as we'll end up emitting:
                            //    movss xmm0, [addr1]           ; Size: 4, Latency: 4-7,  TP: 0.5
                            //    movss [addr2], xmm0           ; Size: 4, Latency: 4-10, TP: 1
                            //
                            // The alternative would be emitting the slightly more expensive
                            //    movups xmm0, [addr1]          ; Size: 4, Latency: 4-7,  TP: 0.5
                            //    extractps [addr2], xmm0, cns  ; Size: 6, Latency: 5-10, TP: 1
                            //
                            // However, we want to prefer containing the store over allowing the
                            // input to be regOptional, so track and clear containment if required.

                            if (comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                            {
                                clearContainedNode = hwintrinsic->Op(1);
                                isContainable      = !clearContainedNode->isContained();
                            }
                        }
                        else
                        {
                            // TODO-XArch-CQ: We really should specially handle TYP_DOUBLE here but
                            // it requires handling GetElement(1) and GT_STOREIND as NI_SSE2_StoreHigh
                            assert(!isContainable);
                        }
                    }
                    break;
                }

                case NI_SSE2_Extract:
                case NI_SSE41_Extract:
                case NI_SSE41_X64_Extract:
                case NI_AVX_ExtractVector128:
                case NI_AVX2_ExtractVector128:
                case NI_AVX512F_ExtractVector128:
                case NI_AVX512F_ExtractVector256:
                case NI_AVX512DQ_ExtractVector128:
                case NI_AVX512DQ_ExtractVector256:
                {
                    // These intrinsics are "ins reg/mem, xmm, imm8"

                    size_t   numArgs = hwintrinsic->GetOperandCount();
                    GenTree* lastOp  = hwintrinsic->Op(numArgs);

                    isContainable = HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && lastOp->IsCnsIntOrI() &&
                                    (genTypeSize(simdBaseType) == genTypeSize(node));

                    if (isContainable && (intrinsicId == NI_SSE2_Extract))
                    {
                        // The encoding that supports containment is SSE4.1 only
                        isContainable = comp->compOpportunisticallyDependsOn(InstructionSet_SSE41);
                    }
                    break;
                }

                case NI_AVX512F_ConvertToVector256Int32:
                case NI_AVX512F_ConvertToVector256UInt32:
                case NI_AVX512F_VL_ConvertToVector128UInt32:
                case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
                {
                    if (varTypeIsFloating(simdBaseType))
                    {
                        break;
                    }
                    FALLTHROUGH;
                }

                case NI_AVX512F_ConvertToVector128Byte:
                case NI_AVX512F_ConvertToVector128ByteWithSaturation:
                case NI_AVX512F_ConvertToVector128Int16:
                case NI_AVX512F_ConvertToVector128Int16WithSaturation:
                case NI_AVX512F_ConvertToVector128SByte:
                case NI_AVX512F_ConvertToVector128SByteWithSaturation:
                case NI_AVX512F_ConvertToVector128UInt16:
                case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
                case NI_AVX512F_ConvertToVector256Int16:
                case NI_AVX512F_ConvertToVector256Int16WithSaturation:
                case NI_AVX512F_ConvertToVector256Int32WithSaturation:
                case NI_AVX512F_ConvertToVector256UInt16:
                case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
                case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
                case NI_AVX512F_VL_ConvertToVector128Byte:
                case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
                case NI_AVX512F_VL_ConvertToVector128Int16:
                case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
                case NI_AVX512F_VL_ConvertToVector128Int32:
                case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
                case NI_AVX512F_VL_ConvertToVector128SByte:
                case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
                case NI_AVX512F_VL_ConvertToVector128UInt16:
                case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
                case NI_AVX512BW_ConvertToVector256Byte:
                case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
                case NI_AVX512BW_ConvertToVector256SByte:
                case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
                case NI_AVX512BW_VL_ConvertToVector128Byte:
                case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
                case NI_AVX512BW_VL_ConvertToVector128SByte:
                case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
                {
                    // These intrinsics are "ins reg/mem, xmm"
                    instruction  ins       = HWIntrinsicInfo::lookupIns(intrinsicId, simdBaseType);
                    insTupleType tupleType = comp->GetEmitter()->insTupleTypeInfo(ins);
                    unsigned     simdSize  = hwintrinsic->GetSimdSize();
                    unsigned     memSize   = 0;

                    switch (tupleType)
                    {
                        case INS_TT_HALF_MEM:
                        {
                            memSize = simdSize / 2;
                            break;
                        }

                        case INS_TT_QUARTER_MEM:
                        {
                            memSize = simdSize / 4;
                            break;
                        }

                        case INS_TT_EIGHTH_MEM:
                        {
                            memSize = simdSize / 8;
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }

                    if (genTypeSize(node) == memSize)
                    {
                        isContainable = true;
                    }
                    break;
                }

                default:
                {
                    break;
                }
            }

            if (isContainable && IsInvariantInRange(src, node))
            {
                MakeSrcContained(node, src);

                if (clearContainedNode != nullptr)
                {
                    // Ensure we aren't marked contained or regOptional
                    clearContainedNode->ClearContained();
                }
            }
        }
#endif // FEATURE_HW_INTRINSICS
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
            if (imm != nullptr)
            {
                // Has a contained immediate operand.
                // Only 'other' operand can be marked as reg optional.
                assert(other != nullptr);

                isSafeToContainOp1 = ((other == op1) && IsSafeToMarkRegOptional(node, op1));
                isSafeToContainOp2 = ((other == op2) && IsSafeToMarkRegOptional(node, op2));
            }
            else if (hasImpliedFirstOperand)
            {
                // Only op2 can be marked as reg optional.
                isSafeToContainOp1 = false;
                isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToMarkRegOptional(node, op2);
            }
            else
            {
                // If there are no containable operands, we can make either of op1 or op2
                // as reg optional.
                isSafeToContainOp1 = isSafeToContainOp1 && IsSafeToMarkRegOptional(node, op1);
                isSafeToContainOp2 = isSafeToContainOp2 && IsSafeToMarkRegOptional(node, op2);
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
    if (IsContainableMemoryOp(divisor) && (divisor->TypeGet() == node->TypeGet()) && IsInvariantInRange(divisor, node))
    {
        MakeSrcContained(node, divisor);
    }
    else if (divisorCanBeRegOptional && IsSafeToMarkRegOptional(node, divisor))
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
#endif

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
        srcType = varTypeToUnsigned(srcType);
    }

    if (!node->gtOverflow())
    {
        // Some casts will be able to use the source from memory.
        bool srcIsContainable = false;

        if (varTypeIsFloating(castToType) || varTypeIsFloating(srcType))
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
                if (castOp->IsCnsNonZeroFltOrDbl())
                {
                    MakeSrcContained(node, castOp);
                }
                else
                {
                    srcIsContainable = true;
                }
            }
        }
        else if (comp->opts.OptimizationEnabled() && varTypeIsIntegral(castOp) && varTypeIsIntegral(castToType))
        {
            // Most integral casts can be re-expressed as loads, except those that would be changing the sign.
            if (!varTypeIsSmall(castOp) || (varTypeIsUnsigned(castOp) == node->IsZeroExtending()))
            {
                srcIsContainable = true;
            }
        }

        if (srcIsContainable)
        {
            TryMakeSrcContainedOrRegOptional(node, castOp);
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
    assert(cmp->OperIsCompare() || cmp->OperIs(GT_CMP, GT_TEST));

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

        if (!otherOp->isContained() && IsSafeToMarkRegOptional(cmp, otherOp))
        {
            // SSE2 allows only otherOp to be a memory-op. Since otherOp is not
            // contained, we can mark it reg-optional.
            // IsSafeToContainMem is expensive so we call it at most once for otherOp.
            // If we already called IsSafeToContainMem, it must have returned false;
            // otherwise, otherOp would be contained.
            MakeSrcRegOptional(cmp, otherOp);
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
            TryMakeSrcContainedOrRegOptional(cmp, op1);
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

            bool setRegOptional =
                (regOptionalCandidate == op1) ? IsSafeToMarkRegOptional(cmp, op1) : IsSafeToMarkRegOptional(cmp, op2);
            if (setRegOptional)
            {
                MakeSrcRegOptional(cmp, regOptionalCandidate);
            }
        }
    }
}

//------------------------------------------------------------------------
// ContainCheckSelect: determine whether the sources of a select should be contained.
//
// Arguments:
//    select - the GT_SELECT or GT_SELECTCC node.
//
void Lowering::ContainCheckSelect(GenTreeOp* select)
{
    assert(select->OperIs(GT_SELECT, GT_SELECTCC));

    if (select->OperIs(GT_SELECTCC))
    {
        GenCondition cc = select->AsOpCC()->gtCondition;

        // op1 and op2 are emitted as two separate instructions due to the
        // conditional nature of cmov, so both operands can usually be
        // contained memory operands. The exception is for compares
        // requiring two cmovs, in which case we do not want to incur the
        // memory access/address calculation twice.
        //
        // See the comment in Codegen::GenConditionDesc::map for why these
        // comparisons are special and end up requiring the two cmovs.
        //
        switch (cc.GetCode())
        {
            case GenCondition::FEQ:
            case GenCondition::FLT:
            case GenCondition::FLE:
            case GenCondition::FNEU:
            case GenCondition::FGEU:
            case GenCondition::FGTU:
                // Skip containment checking below.
                // TODO-CQ: We could allow one of the operands to be a
                // contained memory operand, but it requires updating LSRA
                // build to take it into account.
                return;
            default:
                break;
        }
    }

    GenTree* op1 = select->gtOp1;
    GenTree* op2 = select->gtOp2;

    unsigned operSize = genTypeSize(select);
    assert((operSize == 4) || (operSize == TARGET_POINTER_SIZE));

    if (genTypeSize(op1) == operSize)
    {
        if (IsContainableMemoryOp(op1) && IsSafeToContainMem(select, op1))
        {
            MakeSrcContained(select, op1);
        }
        else if (IsSafeToMarkRegOptional(select, op1))
        {
            MakeSrcRegOptional(select, op1);
        }
    }

    if (genTypeSize(op2) == operSize)
    {
        if (IsContainableMemoryOp(op2) && IsSafeToContainMem(select, op2))
        {
            MakeSrcContained(select, op2);
        }
        else if (IsSafeToMarkRegOptional(select, op2))
        {
            MakeSrcRegOptional(select, op2);
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
        JITDUMP("Lower of StoreInd didn't mark the node as self contained for reason: %s\n",
                RMWStatusDescription(storeInd->AsStoreInd()->GetRMWStatus()));
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
        JITDUMP("Lower successfully detected an assignment of the form: *addrMode BinOp= source\n");
    }
    else
    {
        assert(GenTree::OperIsUnary(oper));
        JITDUMP("Lower successfully detected an assignment of the form: *addrMode = UnaryOp(*addrMode)\n");
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
        assert(indirCandidateChild->OperIs(GT_LCL_VAR, GT_CNS_INT) || indirCandidateChild->IsLclVarAddr());

        // If it is a GT_LCL_VAR, it still needs the reg to hold the address.
        // We would still need a reg for GT_CNS_INT if it doesn't fit within addressing mode base.
        if (indirCandidateChild->OperIs(GT_LCL_ADDR))
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
            if (IsContainableMemoryOpSize(node, op2) && IsContainableMemoryOp(op2))
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
                else if (IsContainableMemoryOpSize(node, op1) && IsContainableMemoryOp(op1))
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

        isSafeToContainOp1 = IsSafeToMarkRegOptional(node, op1);
        isSafeToContainOp2 = IsSafeToMarkRegOptional(node, op2);

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
    assert(node->OperIs(GT_BOUNDS_CHECK));
    GenTree* other;
    if (CheckImmedAndMakeContained(node, node->GetIndex()))
    {
        other = node->GetArrayLength();
    }
    else if (CheckImmedAndMakeContained(node, node->GetArrayLength()))
    {
        other = node->GetIndex();
    }
    else if (IsContainableMemoryOp(node->GetIndex()))
    {
        other = node->GetIndex();
    }
    else
    {
        other = node->GetArrayLength();
    }

    if (node->GetIndex()->TypeGet() == node->GetArrayLength()->TypeGet())
    {
        TryMakeSrcContainedOrRegOptional(node, other);
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
        (intrinsicName == NI_System_Math_Truncate) || (intrinsicName == NI_System_Math_Round) ||
        (intrinsicName == NI_System_Math_Sqrt))
    {
        GenTree* op1 = node->gtGetOp1();

        if (op1->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(node, op1);
        }
        else
        {
            TryMakeSrcContainedOrRegOptional(node, op1);
        }
    }
}

#ifdef FEATURE_HW_INTRINSICS
//----------------------------------------------------------------------------------------------
// IsContainableHWIntrinsicOp: Determines whether a child node is containable for a given HWIntrinsic
//
//  Arguments:
//     [In]  parentNode          - The hardware intrinsic node which is the parent of 'childNode'
//     [In]  childNode           - The node to check if it can be contained by 'parentNode'
//     [Out] supportsRegOptional - On return, this will be true if 'parentNode' supports 'childNode' being regOptional;
//     otherwise, false.
//
// Return Value:
//    true if 'childNode' is a containable by 'parentNode'; otherwise, false.
//
bool Lowering::IsContainableHWIntrinsicOp(GenTreeHWIntrinsic* parentNode, GenTree* childNode, bool* supportsRegOptional)
{
    assert(parentNode != nullptr);
    assert(childNode != nullptr);
    assert(supportsRegOptional != nullptr);

    NamedIntrinsic      parentIntrinsicId = parentNode->GetHWIntrinsicId();
    HWIntrinsicCategory category          = HWIntrinsicInfo::lookupCategory(parentIntrinsicId);

    // We shouldn't have called in here if parentNode doesn't support containment
    assert(HWIntrinsicInfo::SupportsContainment(parentIntrinsicId));

    // parentNode supports nodes that read from an aligned memory address
    //
    // This will generally be an explicit LoadAligned instruction and is false for
    // machines with VEX support when minOpts is enabled. This is because there is
    // currently no way to guarantee that the address read from will always be
    // aligned and we want to assert that the address is aligned when optimizations
    // aren't enabled. However, when optimizations are enabled, we want to allow
    // folding of memory operands as it produces better codegen and allows simpler
    // coding patterns on the managed side.
    bool supportsAlignedSIMDLoads = false;

    // parentNode supports nodes that read from general memory
    //
    // We currently have to assume all "general" loads are unaligned. As such, this is
    // generally used to determine if we can mark the node as `regOptional` in the case
    // where `childNode` is not containable. However, this can also be used to determine whether
    // we can mark other types of reads as contained (such as when directly reading a local).
    bool supportsGeneralLoads = false;

    // parentNode supports nodes that read from a scalar memory address
    //
    // This will generally be an explicit LoadScalar instruction but is also used to determine
    // whether we can read an address of type T (we don't support this when the load would
    // read more than sizeof(T) bytes).
    bool supportsSIMDScalarLoads = false;

    // parentNode supports nodes that read from an unaligned memory address
    //
    // This will generally be an explicit Load instruction and is generally false for machines
    // without VEX support. This is because older hardware required that the SIMD operand always
    // be aligned to the 'natural alignment' of the type.
    bool supportsUnalignedSIMDLoads = false;

    switch (category)
    {
        case HW_Category_MemoryLoad:
        {
            supportsGeneralLoads = !childNode->OperIsHWIntrinsic();
            break;
        }

        case HW_Category_SimpleSIMD:
        {
            switch (parentIntrinsicId)
            {
                case NI_AVX2_BroadcastVector128ToVector256:
                case NI_AVX512F_BroadcastVector128ToVector512:
                case NI_AVX512F_BroadcastVector256ToVector512:
                {
                    if (parentNode->OperIsMemoryLoad())
                    {
                        supportsGeneralLoads = !childNode->OperIsHWIntrinsic();
                        break;
                    }
                    else
                    {
                        supportsGeneralLoads = true;
                        break;
                    }
                }

                case NI_SSE41_ConvertToVector128Int16:
                case NI_SSE41_ConvertToVector128Int32:
                case NI_SSE41_ConvertToVector128Int64:
                case NI_AVX2_ConvertToVector256Int16:
                case NI_AVX2_ConvertToVector256Int32:
                case NI_AVX2_ConvertToVector256Int64:
                {
                    assert(!supportsSIMDScalarLoads);

                    if (!parentNode->OperIsMemoryLoad())
                    {
                        // The containable form is the one that takes a SIMD value, that may be in memory.

                        if (!comp->canUseVexEncoding())
                        {
                            supportsAlignedSIMDLoads   = true;
                            supportsUnalignedSIMDLoads = !supportsAlignedSIMDLoads;
                        }
                        else
                        {
                            supportsAlignedSIMDLoads   = !comp->opts.MinOpts();
                            supportsUnalignedSIMDLoads = true;
                        }

                        // General loads are a bit special where we need at least `sizeof(simdType) / (sizeof(baseType)
                        // * 2)` elements
                        // For example:
                        //  * ConvertToVector128Int16 - sizeof(simdType) = 16; sizeof(baseType) = 1; expectedSize =  8
                        //  * ConvertToVector128Int32 - sizeof(simdType) = 16; sizeof(baseType) = 1 | 2;
                        //  expectedSize =  8 | 4
                        //  * ConvertToVector128Int64 - sizeof(simdType) = 16; sizeof(baseType) = 1 | 2 | 4;
                        //  expectedSize =  8 | 4 | 2
                        //  * ConvertToVector256Int16 - sizeof(simdType) = 32; sizeof(baseType) = 1; expectedSize = 16
                        //  * ConvertToVector256Int32 - sizeof(simdType) = 32; sizeof(baseType) = 1 | 2;
                        //  expectedSize = 16 | 8
                        //  * ConvertToVector256Int64 - sizeof(simdType) = 32; sizeof(baseType) = 1 | 2 | 4;
                        //  expectedSize = 16 | 8 | 4

                        const unsigned sizeof_simdType = genTypeSize(parentNode->TypeGet());
                        const unsigned sizeof_baseType = genTypeSize(parentNode->GetSimdBaseType());

                        assert((sizeof_simdType == 16) || (sizeof_simdType == 32));
                        assert((sizeof_baseType == 1) || (sizeof_baseType == 2) || (sizeof_baseType == 4));

                        const unsigned expectedSize = sizeof_simdType / (sizeof_baseType * 2);
                        const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                        assert((sizeof_simdType != 16) || (expectedSize == 8) || (expectedSize == 4) ||
                               (expectedSize == 2));
                        assert((sizeof_simdType != 32) || (expectedSize == 16) || (expectedSize == 8) ||
                               (expectedSize == 4));

                        supportsGeneralLoads = (operandSize >= expectedSize);
                    }
                    else
                    {
                        // The memory form of this already takes a pointer and should be treated like a MemoryLoad
                        supportsGeneralLoads = !childNode->OperIsHWIntrinsic();
                    }
                    break;
                }

                case NI_SSE2_ConvertToVector128Double:
                case NI_SSE3_MoveAndDuplicate:
                case NI_AVX_ConvertToVector256Double:
                {
                    assert(!supportsSIMDScalarLoads);

                    // Most instructions under the non-VEX encoding require aligned operands.
                    // Those used for Sse2.ConvertToVector128Double (CVTDQ2PD and CVTPS2PD)
                    // and Sse3.MoveAndDuplicate (MOVDDUP) are exceptions and don't fail for
                    // unaligned inputs as they read mem64 (half the vector width) instead

                    supportsAlignedSIMDLoads   = !comp->opts.MinOpts();
                    supportsUnalignedSIMDLoads = true;

                    const unsigned expectedSize = genTypeSize(parentNode->TypeGet()) / 2;
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    // For broadcasts we can only optimize constants and memory operands
                    const bool broadcastIsContainable = childNode->OperIsConst() || childNode->isMemoryOp();
                    supportsGeneralLoads =
                        broadcastIsContainable && supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    break;
                }

                default:
                {
                    assert(!supportsSIMDScalarLoads);

                    if (!comp->canUseVexEncoding())
                    {
                        assert(!supportsUnalignedSIMDLoads);
                        supportsAlignedSIMDLoads = true;
                    }
                    else
                    {
                        supportsAlignedSIMDLoads   = !comp->opts.MinOpts();
                        supportsUnalignedSIMDLoads = true;
                    }

                    const unsigned expectedSize = genTypeSize(parentNode->TypeGet());
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsGeneralLoads = supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    break;
                }
            }

            assert(supportsSIMDScalarLoads == false);
            break;
        }

        case HW_Category_IMM:
        {
            switch (parentIntrinsicId)
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
                case NI_AVX_Permute:
                case NI_AVX_Permute2x128:
                case NI_AVX_Shuffle:
                case NI_AVX2_AlignRight:
                case NI_AVX2_Blend:
                case NI_AVX2_MultipleSumAbsoluteDifferences:
                case NI_AVX2_Permute2x128:
                case NI_AVX2_Permute4x64:
                case NI_AVX2_ShiftLeftLogical:
                case NI_AVX2_ShiftRightArithmetic:
                case NI_AVX2_ShiftRightLogical:
                case NI_AVX2_Shuffle:
                case NI_AVX2_ShuffleHigh:
                case NI_AVX2_ShuffleLow:
                case NI_AVX512F_AlignRight32:
                case NI_AVX512F_AlignRight64:
                case NI_AVX512F_Fixup:
                case NI_AVX512F_GetMantissa:
                case NI_AVX512F_Permute2x64:
                case NI_AVX512F_Permute4x32:
                case NI_AVX512F_Permute4x64:
                case NI_AVX512F_RotateLeft:
                case NI_AVX512F_RotateRight:
                case NI_AVX512F_RoundScale:
                case NI_AVX512F_ShiftLeftLogical:
                case NI_AVX512F_ShiftRightArithmetic:
                case NI_AVX512F_ShiftRightLogical:
                case NI_AVX512F_Shuffle:
                case NI_AVX512F_Shuffle4x128:
                case NI_AVX512F_TernaryLogic:
                case NI_AVX512F_VL_AlignRight32:
                case NI_AVX512F_VL_AlignRight64:
                case NI_AVX512F_VL_Fixup:
                case NI_AVX512F_VL_GetMantissa:
                case NI_AVX512F_VL_RotateLeft:
                case NI_AVX512F_VL_RotateRight:
                case NI_AVX512F_VL_RoundScale:
                case NI_AVX512F_VL_ShiftRightArithmetic:
                case NI_AVX512F_VL_Shuffle2x128:
                case NI_AVX512F_VL_TernaryLogic:
                case NI_AVX512BW_AlignRight:
                case NI_AVX512BW_ShiftLeftLogical:
                case NI_AVX512BW_ShiftRightArithmetic:
                case NI_AVX512BW_ShiftRightLogical:
                case NI_AVX512BW_ShuffleHigh:
                case NI_AVX512BW_ShuffleLow:
                case NI_AVX512BW_SumAbsoluteDifferencesInBlock32:
                case NI_AVX512BW_VL_SumAbsoluteDifferencesInBlock32:
                case NI_AVX512DQ_Range:
                case NI_AVX512DQ_Reduce:
                case NI_AVX512DQ_VL_Range:
                case NI_AVX512DQ_VL_Reduce:
                {
                    assert(!supportsSIMDScalarLoads);

                    const unsigned expectedSize = genTypeSize(parentNode->GetSimdBaseType());
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsAlignedSIMDLoads   = !comp->canUseVexEncoding() || !comp->opts.MinOpts();
                    supportsUnalignedSIMDLoads = comp->canUseVexEncoding();
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    break;
                }

                case NI_SSE2_ShiftLeftLogical128BitLane:
                case NI_SSE2_ShiftRightLogical128BitLane:
                case NI_AVX2_ShiftLeftLogical128BitLane:
                case NI_AVX2_ShiftRightLogical128BitLane:
                case NI_AVX512BW_ShiftLeftLogical128BitLane:
                case NI_AVX512BW_ShiftRightLogical128BitLane:
                {
                    if (comp->IsBaselineVector512IsaSupportedOpportunistically())
                    {
                        assert(!supportsSIMDScalarLoads);

                        const unsigned expectedSize = genTypeSize(parentNode->GetSimdBaseType());
                        const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                        supportsAlignedSIMDLoads   = !comp->canUseVexEncoding() || !comp->opts.MinOpts();
                        supportsUnalignedSIMDLoads = comp->canUseVexEncoding();
                        supportsGeneralLoads       = supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    }
                    else
                    {
                        assert(supportsAlignedSIMDLoads == false);
                        assert(supportsGeneralLoads == false);
                        assert(supportsSIMDScalarLoads == false);
                        assert(supportsUnalignedSIMDLoads == false);
                    }
                    break;
                }

                case NI_AVX_InsertVector128:
                case NI_AVX2_InsertVector128:
                case NI_AVX512F_InsertVector128:
                case NI_AVX512DQ_InsertVector128:
                {
                    // InsertVector128 is special in that it returns a TYP_SIMD32 but takes a TYP_SIMD16.
                    assert(!supportsSIMDScalarLoads);

                    const unsigned expectedSize = 16;
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsAlignedSIMDLoads   = !comp->canUseVexEncoding() || !comp->opts.MinOpts();
                    supportsUnalignedSIMDLoads = comp->canUseVexEncoding();
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    break;
                }

                case NI_AVX512F_InsertVector256:
                case NI_AVX512DQ_InsertVector256:
                {
                    // InsertVector256 is special in that it returns a TYP_SIMD64 but takes a TYP_SIMD32.
                    assert(!supportsSIMDScalarLoads);

                    const unsigned expectedSize = 32;
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsAlignedSIMDLoads   = !comp->canUseEvexEncoding() || !comp->opts.MinOpts();
                    supportsUnalignedSIMDLoads = comp->canUseEvexEncoding();
                    supportsGeneralLoads       = supportsUnalignedSIMDLoads && (operandSize >= expectedSize);
                    break;
                }

                case NI_SSE2_Insert:
                case NI_SSE41_Insert:
                case NI_SSE41_X64_Insert:
                {
                    assert(supportsAlignedSIMDLoads == false);
                    assert(supportsUnalignedSIMDLoads == false);

                    if (parentNode->GetSimdBaseType() == TYP_FLOAT)
                    {
                        assert(parentIntrinsicId == NI_SSE41_Insert);

                        // Sse41.Insert(V128<float>, V128<float>, byte) is a bit special
                        // in that it has different behavior depending on whether the
                        // second operand is coming from a register or memory. When coming
                        // from a register, all 4 elements of the vector can be used and it
                        // is effectively a regular `SimpleSIMD` operation; but when loading
                        // from memory, it only works with the lowest element and is effectively
                        // a `SIMDScalar`.

                        assert(supportsGeneralLoads == false);
                        assert(supportsSIMDScalarLoads == false);

                        GenTree* op1 = parentNode->Op(1);
                        GenTree* op2 = parentNode->Op(2);
                        GenTree* op3 = parentNode->Op(3);

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
                    assert(varTypeIsIntegral(childNode->TypeGet()));
                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(parentNode->GetSimdBaseType());
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsGeneralLoads = (operandSize >= expectedSize);
                    break;
                }

                case NI_AVX_CompareScalar:
                case NI_AVX512F_FixupScalar:
                case NI_AVX512F_GetMantissaScalar:
                case NI_AVX512F_RoundScaleScalar:
                case NI_AVX512DQ_RangeScalar:
                case NI_AVX512DQ_ReduceScalar:
                {
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

            switch (parentIntrinsicId)
            {
                case NI_Vector128_CreateScalarUnsafe:
                case NI_Vector256_CreateScalarUnsafe:
                case NI_Vector512_CreateScalarUnsafe:
                {
                    if (!varTypeIsIntegral(childNode->TypeGet()))
                    {
                        // The floating-point overload doesn't require any special semantics
                        supportsSIMDScalarLoads = true;
                        supportsGeneralLoads    = supportsSIMDScalarLoads;
                        break;
                    }

                    // The integral overloads only take GPR/mem
                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(genActualType(parentNode->GetSimdBaseType()));
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsGeneralLoads = (operandSize >= expectedSize);
                    break;
                }

                case NI_AVX2_BroadcastScalarToVector128:
                case NI_AVX2_BroadcastScalarToVector256:
                case NI_AVX512F_BroadcastScalarToVector512:
                case NI_AVX512BW_BroadcastScalarToVector512:
                {
                    if (!parentNode->OperIsMemoryLoad())
                    {
                        // The containable form is the one that takes a SIMD value, that may be in memory.
                        supportsSIMDScalarLoads = true;
                        supportsGeneralLoads    = supportsSIMDScalarLoads;
                    }
                    else
                    {
                        // The memory form of this already takes a pointer and should be treated like a MemoryLoad
                        supportsGeneralLoads = !childNode->OperIsHWIntrinsic();
                    }

                    supportsGeneralLoads =
                        supportsGeneralLoads && (genTypeSize(childNode) >= genTypeSize(parentNode->GetSimdBaseType()));
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
                case NI_AVX512F_ConvertScalarToVector128Double:
                case NI_AVX512F_ConvertScalarToVector128Single:
                case NI_AVX512F_X64_ConvertScalarToVector128Double:
                case NI_AVX512F_X64_ConvertScalarToVector128Single:
                {
                    if (!varTypeIsIntegral(childNode->TypeGet()))
                    {
                        // The floating-point overload doesn't require any special semantics
                        assert(parentIntrinsicId == NI_SSE2_ConvertScalarToVector128Double);
                        supportsSIMDScalarLoads = true;
                        supportsGeneralLoads    = supportsSIMDScalarLoads;
                        break;
                    }

                    // The integral overloads only take GPR/mem
                    assert(supportsSIMDScalarLoads == false);

                    const unsigned expectedSize = genTypeSize(genActualType(parentNode->GetSimdBaseType()));
                    const unsigned operandSize  = genTypeSize(childNode->TypeGet());

                    supportsGeneralLoads = (operandSize >= expectedSize);
                    break;
                }

                default:
                {
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
            assert(varTypeIsIntegral(childNode->TypeGet()));

            assert(supportsAlignedSIMDLoads == false);
            assert(supportsUnalignedSIMDLoads == false);
            assert(supportsSIMDScalarLoads == false);

            unsigned       expectedSize = genTypeSize(parentNode->TypeGet());
            const unsigned operandSize  = genTypeSize(childNode->TypeGet());

            // CRC32 codegen depends on its second oprand's type.
            // Currently, we are using SIMDBaseType to store the op2Type info.
            if (parentIntrinsicId == NI_SSE42_Crc32)
            {
                var_types op2Type = parentNode->GetSimdBaseType();
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

    *supportsRegOptional = supportsGeneralLoads && IsSafeToMarkRegOptional(parentNode, childNode);

    if (!childNode->OperIsHWIntrinsic())
    {
        bool canBeContained = false;

        if (supportsGeneralLoads)
        {
            if (IsContainableMemoryOp(childNode))
            {
                canBeContained = IsSafeToContainMem(parentNode, childNode);
            }
            else if (childNode->IsCnsNonZeroFltOrDbl())
            {
                // Always safe.
                canBeContained = true;
            }
            else if (childNode->IsCnsVec())
            {
                GenTreeVecCon* vecCon = childNode->AsVecCon();
                canBeContained        = !vecCon->IsAllBitsSet() && !vecCon->IsZero();
            }
        }
        return canBeContained;
    }

    // TODO-XArch: Update this to be table driven, if possible.

    GenTreeHWIntrinsic* hwintrinsic = childNode->AsHWIntrinsic();
    NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

    switch (intrinsicId)
    {
        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        case NI_Vector512_CreateScalarUnsafe:
        {
            if (!supportsSIMDScalarLoads)
            {
                // Nothing to do if the intrinsic doesn't support scalar loads
                return false;
            }

            GenTree* op1 = hwintrinsic->Op(1);

            if (IsInvariantInRange(op1, parentNode, hwintrinsic))
            {
                if (op1->isContained())
                {
                    // We have CreateScalarUnsafe where the underlying scalar is contained
                    // As such, we can contain the CreateScalarUnsafe and consume the value
                    // directly in codegen.

                    return true;
                }

                if (op1->IsRegOptional() && varTypeIsFloating(op1))
                {
                    // We have CreateScalarUnsafe where the underlying scalar was marked reg
                    // optional. As such, we can contain the CreateScalarUnsafe and consume
                    // the value directly in codegen.
                    //
                    // We only want to do this when op1 produces a floating-point value since that means
                    // it will already be in a SIMD register in the scenario it isn't spilled.

                    return true;
                }
            }

            return false;
        }

        case NI_SSE_LoadAlignedVector128:
        case NI_SSE2_LoadAlignedVector128:
        case NI_AVX_LoadAlignedVector256:
        case NI_AVX512F_LoadAlignedVector512:
        {
            return supportsAlignedSIMDLoads;
        }

        case NI_SSE_LoadScalarVector128:
        case NI_SSE2_LoadScalarVector128:
        {
            return supportsSIMDScalarLoads;
        }

        case NI_AVX512F_ConvertToVector256Int32:
        case NI_AVX512F_ConvertToVector256UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
        {
            // These ones are not containable as stores when the base
            // type is a floating-point type
            FALLTHROUGH;
        }

        case NI_Vector128_GetElement:
        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_X64_ConvertToInt64:
        case NI_SSE2_X64_ConvertToUInt64:
        case NI_SSE2_Extract:
        case NI_SSE41_Extract:
        case NI_SSE41_X64_Extract:
        case NI_AVX_ExtractVector128:
        case NI_AVX2_ConvertToInt32:
        case NI_AVX2_ConvertToUInt32:
        case NI_AVX2_ExtractVector128:
        case NI_AVX512F_ExtractVector128:
        case NI_AVX512F_ExtractVector256:
        case NI_AVX512F_ConvertToVector128Byte:
        case NI_AVX512F_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_ConvertToVector128Int16:
        case NI_AVX512F_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_ConvertToVector128SByte:
        case NI_AVX512F_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_ConvertToVector128UInt16:
        case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int16:
        case NI_AVX512F_ConvertToVector256Int16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int32WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt16:
        case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Byte:
        case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int16:
        case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int32:
        case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128SByte:
        case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128UInt16:
        case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512BW_ConvertToVector256Byte:
        case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
        case NI_AVX512BW_ConvertToVector256SByte:
        case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128Byte:
        case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128SByte:
        case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
        case NI_AVX512DQ_ExtractVector128:
        case NI_AVX512DQ_ExtractVector256:
        {
            // These are only containable as part of a store
            return false;
        }

        case NI_Vector128_get_Zero:
        case NI_Vector256_get_Zero:
        {
            // These are only containable as part of Sse41.Insert
            return false;
        }

        case NI_SSE3_MoveAndDuplicate:
        case NI_AVX2_BroadcastScalarToVector128:
        case NI_AVX2_BroadcastScalarToVector256:
        case NI_AVX512F_BroadcastScalarToVector512:
        {
            var_types parentBaseType = parentNode->GetSimdBaseType();
            var_types childBaseType  = hwintrinsic->GetSimdBaseType();

            if (varTypeIsSmall(parentBaseType) || (genTypeSize(parentBaseType) != genTypeSize(childBaseType)))
            {
                // early return if either base type is not embedded broadcast compatible.
                return false;
            }

            // make the broadcast node containable when embedded broadcast can be enabled.
            if (intrinsicId == NI_SSE3_MoveAndDuplicate)
            {
                // NI_SSE3_MoveAndDuplicate is for Vector128<double> only.
                assert(childBaseType == TYP_DOUBLE);
            }

            if (comp->compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL) &&
                parentNode->OperIsEmbBroadcastCompatible())
            {
                GenTree* broadcastOperand = hwintrinsic->Op(1);

                if (broadcastOperand->OperIsHWIntrinsic())
                {
                    GenTreeHWIntrinsic* hwintrinsicOperand = broadcastOperand->AsHWIntrinsic();

                    if (hwintrinsicOperand->OperIsCreateScalarUnsafe())
                    {
                        // CreateScalarUnsafe can contain non-memory operands such as enregistered
                        // locals, so we want to check if its operand is containable instead. This
                        // will result in such enregistered locals returning `false`.
                        broadcastOperand = hwintrinsicOperand->Op(1);
                    }
                }

                bool childSupportsRegOptional;
                if (IsContainableHWIntrinsicOp(hwintrinsic, broadcastOperand, &childSupportsRegOptional))
                {
                    return true;
                }
            }
            return false;
        }

        case NI_AVX_BroadcastScalarToVector128:
        case NI_AVX_BroadcastScalarToVector256:
        {
            var_types parentBaseType = parentNode->GetSimdBaseType();
            var_types childBaseType  = hwintrinsic->GetSimdBaseType();

            if (varTypeIsSmall(parentBaseType) || (genTypeSize(parentBaseType) != genTypeSize(childBaseType)))
            {
                // early return if either base type is not embedded broadcast compatible.
                return false;
            }

            return parentNode->OperIsEmbBroadcastCompatible();
        }

        default:
        {
            assert(!childNode->isContainableHWIntrinsic());
            return false;
        }
    }
}

//----------------------------------------------------------------------------------------------
// TryFoldCnsVecForEmbeddedBroadcast:
//  Unfold the eligible constant vector when embedded broadcast is
//  available.
//
//  Arguments:
//     parentNode - The hardware intrinsic node
//     childNode  - The operand node to try contain
//
void Lowering::TryFoldCnsVecForEmbeddedBroadcast(GenTreeHWIntrinsic* parentNode, GenTreeVecCon* childNode)
{
    assert(!childNode->IsAllBitsSet());
    assert(!childNode->IsZero());
    var_types   simdType            = parentNode->TypeGet();
    var_types   simdBaseType        = parentNode->GetSimdBaseType();
    CorInfoType simdBaseJitType     = parentNode->GetSimdBaseJitType();
    bool        isCreatedFromScalar = true;
    int         elementCount        = GenTreeVecCon::ElementCount(genTypeSize(simdType), simdBaseType);
    switch (simdBaseType)
    {
        case TYP_FLOAT:
        case TYP_INT:
        case TYP_UINT:
        {
            uint32_t firstElement = static_cast<uint32_t>(childNode->gtSimdVal.u32[0]);
            for (int i = 1; i < elementCount; i++)
            {
                uint32_t elementToCheck = static_cast<uint32_t>(childNode->gtSimdVal.u32[i]);
                if (firstElement != elementToCheck)
                {
                    isCreatedFromScalar = false;
                    break;
                }
            }
            break;
        }

        case TYP_DOUBLE:
#if defined(TARGET_AMD64)
        case TYP_LONG:
        case TYP_ULONG:
#endif // TARGET_AMD64
        {
            uint64_t firstElement = static_cast<uint64_t>(childNode->gtSimdVal.u64[0]);
            for (int i = 1; i < elementCount; i++)
            {
                uint64_t elementToCheck = static_cast<uint64_t>(childNode->gtSimdVal.u64[i]);
                if (firstElement != elementToCheck)
                {
                    isCreatedFromScalar = false;
                    break;
                }
            }
            break;
        }

        default:
            isCreatedFromScalar = false;
            break;
    }
    if (isCreatedFromScalar)
    {
        NamedIntrinsic broadcastName = NI_AVX2_BroadcastScalarToVector128;
        if (simdType == TYP_SIMD32)
        {
            broadcastName = NI_AVX2_BroadcastScalarToVector256;
        }
        else if (simdType == TYP_SIMD64)
        {
            broadcastName = NI_AVX512F_BroadcastScalarToVector512;
        }
        else
        {
            assert(simdType == TYP_SIMD16);
        }
        GenTree* constScalar = nullptr;
        switch (simdBaseType)
        {
            case TYP_FLOAT:
            {
                float scalar = static_cast<float>(childNode->gtSimdVal.f32[0]);
                constScalar  = comp->gtNewDconNodeF(scalar);
                break;
            }
            case TYP_DOUBLE:
            {
                double scalar = static_cast<double>(childNode->gtSimdVal.f64[0]);
                constScalar   = comp->gtNewDconNodeD(scalar);
                break;
            }
            case TYP_INT:
            {
                int32_t scalar = static_cast<int32_t>(childNode->gtSimdVal.i32[0]);
                constScalar    = comp->gtNewIconNode(scalar, simdBaseType);
                break;
            }
            case TYP_UINT:
            {
                uint32_t scalar = static_cast<uint32_t>(childNode->gtSimdVal.u32[0]);
                constScalar     = comp->gtNewIconNode(scalar, TYP_INT);
                break;
            }
#if defined(TARGET_AMD64)
            case TYP_LONG:
            {
                int64_t scalar = static_cast<int64_t>(childNode->gtSimdVal.i64[0]);
                constScalar    = comp->gtNewIconNode(scalar, simdBaseType);
                break;
            }
            case TYP_ULONG:
            {
                uint64_t scalar = static_cast<uint64_t>(childNode->gtSimdVal.u64[0]);
                constScalar     = comp->gtNewIconNode(scalar, TYP_LONG);
                break;
            }
#endif // TARGET_AMD64
            default:
                unreached();
        }
        GenTreeHWIntrinsic* createScalar =
            comp->gtNewSimdHWIntrinsicNode(TYP_SIMD16, constScalar, NI_Vector128_CreateScalarUnsafe, simdBaseJitType,
                                           16);
        GenTreeHWIntrinsic* broadcastNode = comp->gtNewSimdHWIntrinsicNode(simdType, createScalar, broadcastName,
                                                                           simdBaseJitType, genTypeSize(simdType));
        BlockRange().InsertBefore(childNode, broadcastNode);
        BlockRange().InsertBefore(broadcastNode, createScalar);
        BlockRange().InsertBefore(createScalar, constScalar);
        LIR::Use use;
        if (BlockRange().TryGetUse(childNode, &use))
        {
            use.ReplaceWith(broadcastNode);
        }
        else
        {
            broadcastNode->SetUnusedValue();
        }

        BlockRange().Remove(childNode);
        LowerNode(createScalar);
        LowerNode(broadcastNode);
        if (varTypeIsFloating(simdBaseType))
        {
            MakeSrcContained(broadcastNode, createScalar);
        }
        else if (constScalar->TypeIs(TYP_INT, TYP_UINT, TYP_LONG, TYP_ULONG))
        {
            MakeSrcContained(broadcastNode, constScalar);
        }
        MakeSrcContained(parentNode, broadcastNode);
        return;
    }
    MakeSrcContained(parentNode, childNode);
}

//----------------------------------------------------------------------------------------------
// TryCompressConstVecData:
//  Try to compress the constant vector input if it has duplicated parts and can be optimized by
//  broadcast
//
//  Arguments:
//     node - the storeind node.
//
//  Return:
//     return true if compress success.
void Lowering::TryCompressConstVecData(GenTreeStoreInd* node)
{
    assert(node->Data()->OperIs(GT_CNS_VEC));
    GenTreeVecCon*      vecCon    = node->Data()->AsVecCon();
    GenTreeHWIntrinsic* broadcast = nullptr;

    if (vecCon->TypeIs(TYP_SIMD32))
    {
        assert(comp->compOpportunisticallyDependsOn(InstructionSet_AVX2));
        if (vecCon->gtSimd32Val.v128[0] == vecCon->gtSimdVal.v128[1])
        {
            simd16_t simd16Val              = {};
            simd16Val.f64[0]                = vecCon->gtSimd32Val.f64[0];
            simd16Val.f64[1]                = vecCon->gtSimd32Val.f64[1];
            GenTreeVecCon* compressedVecCon = comp->gtNewVconNode(TYP_SIMD16);
            memcpy(&compressedVecCon->gtSimdVal, &simd16Val, sizeof(simd16_t));
            BlockRange().InsertBefore(node->Data(), compressedVecCon);
            BlockRange().Remove(vecCon);
            broadcast = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD32, compressedVecCon,
                                                       NI_AVX2_BroadcastVector128ToVector256, CORINFO_TYPE_UINT, 32);
        }
    }
    else
    {
        assert(vecCon->TypeIs(TYP_SIMD64));
        assert(comp->IsBaselineVector512IsaSupportedOpportunistically());
        if (vecCon->gtSimd64Val.v128[0] == vecCon->gtSimd64Val.v128[1] &&
            vecCon->gtSimd64Val.v128[0] == vecCon->gtSimd64Val.v128[2] &&
            vecCon->gtSimd64Val.v128[0] == vecCon->gtSimd64Val.v128[3])
        {
            simd16_t simd16Val              = {};
            simd16Val.f64[0]                = vecCon->gtSimd64Val.f64[0];
            simd16Val.f64[1]                = vecCon->gtSimd64Val.f64[1];
            GenTreeVecCon* compressedVecCon = comp->gtNewVconNode(TYP_SIMD16);
            memcpy(&compressedVecCon->gtSimdVal, &simd16Val, sizeof(simd16_t));
            BlockRange().InsertBefore(node->Data(), compressedVecCon);
            BlockRange().Remove(vecCon);
            broadcast = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD64, compressedVecCon,
                                                       NI_AVX512F_BroadcastVector128ToVector512, CORINFO_TYPE_UINT, 64);
        }
        else if (vecCon->gtSimd64Val.v256[0] == vecCon->gtSimd64Val.v256[1])
        {
            simd32_t simd32Val              = {};
            simd32Val.v128[0]               = vecCon->gtSimd32Val.v128[0];
            simd32Val.v128[1]               = vecCon->gtSimd32Val.v128[1];
            GenTreeVecCon* compressedVecCon = comp->gtNewVconNode(TYP_SIMD32);
            memcpy(&compressedVecCon->gtSimdVal, &simd32Val, sizeof(simd32_t));
            BlockRange().InsertBefore(node->Data(), compressedVecCon);
            BlockRange().Remove(vecCon);
            broadcast =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD64, compressedVecCon, NI_AVX512F_BroadcastVector256ToVector512,
                                               CORINFO_TYPE_ULONG, 64);
        }
    }

    if (broadcast == nullptr)
    {
        return;
    }

    BlockRange().InsertBefore(node, broadcast);
    node->Data() = broadcast;
    LowerNode(broadcast);
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
    TryCreateAddrMode(addr, true, node);
    if ((addr->OperIs(GT_LCL_ADDR, GT_LEA) || (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp))) &&
        IsInvariantInRange(addr, node))
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
    NamedIntrinsic      intrinsicId     = node->GetHWIntrinsicId();
    HWIntrinsicCategory category        = HWIntrinsicInfo::lookupCategory(intrinsicId);
    size_t              numArgs         = node->GetOperandCount();
    CorInfoType         simdBaseJitType = node->GetSimdBaseJitType();
    var_types           simdBaseType    = node->GetSimdBaseType();
    uint32_t            simdSize        = node->GetSimdSize();

    if (!HWIntrinsicInfo::SupportsContainment(intrinsicId))
    {
        // AVX2 gather are not containable and always have constant IMM argument
        if (HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsicId))
        {
            GenTree* lastOp = node->Op(numArgs);
            MakeSrcContained(node, lastOp);
        }
        // Exit early if containment isn't supported
        return;
    }

    bool isContainedImm = false;

    if (HWIntrinsicInfo::lookupCategory(intrinsicId) == HW_Category_IMM)
    {
        GenTree* lastOp = node->Op(numArgs);

        if (HWIntrinsicInfo::isImmOp(intrinsicId, lastOp) && lastOp->IsCnsIntOrI())
        {
            MakeSrcContained(node, lastOp);
            isContainedImm = true;
        }
    }

    if ((simdSize == 8) || (simdSize == 12))
    {
        // We want to handle GetElement/ToScalar still for Vector2/3
        if ((intrinsicId != NI_Vector128_GetElement) && (intrinsicId != NI_Vector128_ToScalar) &&
            (intrinsicId != NI_Vector256_GetElement) && (intrinsicId != NI_Vector256_ToScalar) &&
            (intrinsicId != NI_Vector512_GetElement) && (intrinsicId != NI_Vector512_ToScalar))
        {
            // TODO-XArch-CQ: Ideally we would key this off of the size the containing node
            // expects vs the size node actually is or would be if spilled to the stack
            return;
        }
    }

    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    const bool isCommutative = HWIntrinsicInfo::IsCommutative(intrinsicId);

    GenTree* op1 = nullptr;
    GenTree* op2 = nullptr;
    GenTree* op3 = nullptr;
    GenTree* op4 = nullptr;

    if (numArgs == 1)
    {
        // One argument intrinsics cannot be commutative
        assert(!isCommutative);

        op1 = node->Op(1);

        switch (category)
        {
            case HW_Category_MemoryLoad:
                ContainCheckHWIntrinsicAddr(node, op1);
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
                    case NI_AVX512F_GetExponentScalar:
                    case NI_AVX512F_Reciprocal14Scalar:
                    case NI_AVX512F_ReciprocalSqrt14Scalar:
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
                            // These intrinsics are "ins reg/mem, xmm" and get
                            // contained by the relevant store operation instead.
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
                        if (node->OperIsMemoryLoad())
                        {
                            ContainCheckHWIntrinsicAddr(node, op1);
                            return;
                        }
                        break;
                    }

                    case NI_AVX2_BroadcastScalarToVector128:
                    case NI_AVX2_BroadcastScalarToVector256:
                    case NI_AVX512F_BroadcastScalarToVector512:
                    case NI_AVX512BW_BroadcastScalarToVector512:
                    {
                        if (node->OperIsMemoryLoad())
                        {
                            ContainCheckHWIntrinsicAddr(node, op1);
                            return;
                        }

                        if (varTypeIsIntegral(simdBaseType) && op1->OperIsHWIntrinsic())
                        {
                            GenTreeHWIntrinsic* childNode   = op1->AsHWIntrinsic();
                            NamedIntrinsic      childNodeId = childNode->GetHWIntrinsicId();

                            if ((childNodeId == NI_Vector128_CreateScalarUnsafe) ||
                                (childNodeId == NI_Vector256_CreateScalarUnsafe) ||
                                (childNodeId == NI_Vector512_CreateScalarUnsafe))
                            {
                                // We have a very special case of BroadcastScalarToVector(CreateScalarUnsafe(op1))
                                //
                                // This is one of the only instructions where it supports taking integer types from
                                // a SIMD register or directly as a scalar from memory. Most other instructions, in
                                // comparison, take such values from general-purpose registers instead.
                                //
                                // Because of this, we're going to remove the CreateScalarUnsafe and try to contain
                                // op1 directly, we'll then special case the codegen to materialize the value into a
                                // SIMD register in the case it is marked optional and doesn't get spilled.

                                node->Op(1) = childNode->Op(1);
                                BlockRange().Remove(op1);

                                op1 = node->Op(1);
                                op1->ClearContained();
                            }
                        }
                        break;
                    }

                    case NI_AVX2_BroadcastVector128ToVector256:
                    case NI_AVX512F_BroadcastVector128ToVector512:
                    case NI_AVX512F_BroadcastVector256ToVector512:
                    {
                        if (node->OperIsMemoryLoad())
                        {
                            ContainCheckHWIntrinsicAddr(node, op1);
                            return;
                        }

                        assert(op1->OperIs(GT_CNS_VEC));
                        break;
                    }

                    case NI_AVX512F_ConvertToVector256Int32:
                    case NI_AVX512F_ConvertToVector256UInt32:
                    case NI_AVX512F_VL_ConvertToVector128UInt32:
                    case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
                    {
                        if (varTypeIsFloating(simdBaseType))
                        {
                            // This version is "ins xmm, xmm/mem" and
                            // gets the default containment handling
                            break;
                        }
                        FALLTHROUGH;
                    }

                    case NI_AVX512F_ConvertToVector128Byte:
                    case NI_AVX512F_ConvertToVector128ByteWithSaturation:
                    case NI_AVX512F_ConvertToVector128Int16:
                    case NI_AVX512F_ConvertToVector128Int16WithSaturation:
                    case NI_AVX512F_ConvertToVector128SByte:
                    case NI_AVX512F_ConvertToVector128SByteWithSaturation:
                    case NI_AVX512F_ConvertToVector128UInt16:
                    case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
                    case NI_AVX512F_ConvertToVector256Int16:
                    case NI_AVX512F_ConvertToVector256Int16WithSaturation:
                    case NI_AVX512F_ConvertToVector256Int32WithSaturation:
                    case NI_AVX512F_ConvertToVector256UInt16:
                    case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
                    case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
                    case NI_AVX512F_VL_ConvertToVector128Byte:
                    case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
                    case NI_AVX512F_VL_ConvertToVector128Int16:
                    case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
                    case NI_AVX512F_VL_ConvertToVector128Int32:
                    case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
                    case NI_AVX512F_VL_ConvertToVector128SByte:
                    case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
                    case NI_AVX512F_VL_ConvertToVector128UInt16:
                    case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
                    case NI_AVX512BW_ConvertToVector256Byte:
                    case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
                    case NI_AVX512BW_ConvertToVector256SByte:
                    case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
                    case NI_AVX512BW_VL_ConvertToVector128Byte:
                    case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
                    case NI_AVX512BW_VL_ConvertToVector128SByte:
                    case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
                    {
                        // These intrinsics are "ins reg/mem, xmm" and get
                        // contained by the relevant store operation instead.
                        return;
                    }

                    default:
                    {
                        break;
                    }
                }

                assert(!node->OperIsMemoryLoad());
                bool supportsRegOptional = false;

                if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                {
                    MakeSrcContained(node, op1);
                }
                else if (supportsRegOptional)
                {
                    MakeSrcRegOptional(node, op1);
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
            op1 = node->Op(1);
            op2 = node->Op(2);

            switch (category)
            {
                case HW_Category_MemoryLoad:
                    if ((intrinsicId == NI_AVX_MaskLoad) || (intrinsicId == NI_AVX2_MaskLoad))
                    {
                        ContainCheckHWIntrinsicAddr(node, op1);
                    }
                    else
                    {
                        ContainCheckHWIntrinsicAddr(node, op2);
                    }
                    break;

                case HW_Category_MemoryStore:
                    ContainCheckHWIntrinsicAddr(node, op1);
                    break;

                case HW_Category_SimpleSIMD:
                case HW_Category_SIMDScalar:
                case HW_Category_Scalar:
                {
                    bool supportsOp1RegOptional = false;
                    bool supportsOp2RegOptional = false;

                    if (IsContainableHWIntrinsicOp(node, op2, &supportsOp2RegOptional))
                    {
                        if (op2->OperIs(GT_CNS_VEC) && comp->compOpportunisticallyDependsOn(InstructionSet_AVX512F) &&
                            node->OperIsEmbBroadcastCompatible())
                        {
                            TryFoldCnsVecForEmbeddedBroadcast(node, op2->AsVecCon());
                        }
                        else
                        {
                            MakeSrcContained(node, op2);
                        }
                    }
                    else if ((isCommutative || (intrinsicId == NI_BMI2_MultiplyNoFlags) ||
                              (intrinsicId == NI_BMI2_X64_MultiplyNoFlags)) &&
                             IsContainableHWIntrinsicOp(node, op1, &supportsOp1RegOptional))
                    {
                        if (op1->OperIs(GT_CNS_VEC) && comp->compOpportunisticallyDependsOn(InstructionSet_AVX512F) &&
                            node->OperIsEmbBroadcastCompatible())
                        {
                            TryFoldCnsVecForEmbeddedBroadcast(node, op1->AsVecCon());
                        }
                        else
                        {
                            MakeSrcContained(node, op1);
                        }

                        // Swap the operands here to make the containment checks in codegen significantly simpler
                        std::swap(node->Op(1), node->Op(2));
                    }
                    else if (supportsOp2RegOptional)
                    {
                        MakeSrcRegOptional(node, op2);

                        // TODO-XArch-CQ: For commutative nodes, either operand can be reg-optional.
                        //                https://github.com/dotnet/runtime/issues/6358
                    }
                    else if (supportsOp1RegOptional)
                    {
                        MakeSrcRegOptional(node, op1);

                        // Swap the operands here to make the containment checks in codegen significantly simpler
                        std::swap(node->Op(1), node->Op(2));
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
                        case NI_AVX512F_ExtractVector128:
                        case NI_AVX512F_ExtractVector256:
                        case NI_AVX512DQ_ExtractVector128:
                        case NI_AVX512DQ_ExtractVector256:
                        {
                            // These intrinsics are "ins reg/mem, xmm, imm8" and get
                            // contained by the relevant store operation instead.
                            break;
                        }

                        case NI_SSE2_ShiftLeftLogical:
                        case NI_SSE2_ShiftRightArithmetic:
                        case NI_SSE2_ShiftRightLogical:
                        case NI_AVX2_ShiftLeftLogical:
                        case NI_AVX2_ShiftRightArithmetic:
                        case NI_AVX2_ShiftRightLogical:
                        case NI_AVX512F_RotateLeft:
                        case NI_AVX512F_RotateRight:
                        case NI_AVX512F_ShiftLeftLogical:
                        case NI_AVX512F_ShiftRightArithmetic:
                        case NI_AVX512F_ShiftRightLogical:
                        case NI_AVX512F_VL_RotateLeft:
                        case NI_AVX512F_VL_RotateRight:
                        case NI_AVX512F_VL_ShiftRightArithmetic:
                        case NI_AVX512BW_ShiftLeftLogical:
                        case NI_AVX512BW_ShiftRightArithmetic:
                        case NI_AVX512BW_ShiftRightLogical:
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
                                    MakeSrcRegOptional(node, op2);
                                }
                            }
                            break;
                        }

                        case NI_AVX2_Shuffle:
                        {
                            if (varTypeIsByte(simdBaseType))
                            {
                                // byte and sbyte are: pshufb ymm1, ymm2, ymm3/m256
                                assert(!isCommutative);

                                bool supportsRegOptional = false;

                                if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op2);
                                }
                                break;
                            }
                            FALLTHROUGH;
                        }

                        case NI_SSE2_Shuffle:
                        case NI_SSE2_ShuffleHigh:
                        case NI_SSE2_ShuffleLow:
                        case NI_AVX2_Permute4x64:
                        case NI_AVX2_ShuffleHigh:
                        case NI_AVX2_ShuffleLow:
                        case NI_AVX512F_Permute2x64:
                        case NI_AVX512F_Permute4x32:
                        case NI_AVX512F_Permute4x64:
                        case NI_AVX512F_Shuffle:
                        case NI_AVX512BW_ShuffleHigh:
                        case NI_AVX512BW_ShuffleLow:
                        {
                            // These intrinsics have op2 as an imm and op1 as a reg/mem

                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op1);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op1);
                            }
                            break;
                        }

                        case NI_SSE41_Extract:
                        case NI_SSE41_X64_Extract:
                        {
                            // These intrinsics are "ins reg/mem, xmm" and get
                            // contained by the relevant store operation instead.

                            assert(!varTypeIsFloating(simdBaseType));
                            break;
                        }

                        case NI_AVX_Permute:
                        {
                            // These intrinsics can have op2 be imm or reg/mem
                            // They also can have op1 be reg/mem and op2 be imm

                            if (HWIntrinsicInfo::isImmOp(intrinsicId, op2))
                            {
                                if (!isContainedImm)
                                {
                                    // Don't contain if we're generating a jmp table fallback
                                    break;
                                }

                                if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op1);
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op1);
                                }
                            }
                            else if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op2);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op2);
                            }
                            break;
                        }

                        case NI_AES_KeygenAssist:
                        case NI_AVX512F_GetMantissa:
                        case NI_AVX512F_VL_GetMantissa:
                        case NI_AVX512F_RoundScale:
                        case NI_AVX512F_VL_RoundScale:
                        case NI_AVX512DQ_Reduce:
                        case NI_AVX512DQ_VL_Reduce:
                        {
                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op1);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op1);
                            }
                            break;
                        }

                        case NI_SSE2_ShiftLeftLogical128BitLane:
                        case NI_SSE2_ShiftRightLogical128BitLane:
                        case NI_AVX2_ShiftLeftLogical128BitLane:
                        case NI_AVX2_ShiftRightLogical128BitLane:
                        case NI_AVX512BW_ShiftLeftLogical128BitLane:
                        case NI_AVX512BW_ShiftRightLogical128BitLane:
                        {
                            // These intrinsics have op2 as an imm and op1 as a reg/mem when AVX512BW+VL is supported

                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op1, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op1);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op1);
                            }
                            break;
                        }

                        case NI_AVX512F_GetMantissaScalar:
                        case NI_AVX512F_RoundScaleScalar:
                        case NI_AVX512DQ_ReduceScalar:
                        {
                            // These intrinsics have both 2 and 3-operand overloads.
                            //
                            // The 2-operand overload basically does `intrinsic(op1, op1, cns)`
                            //
                            // Because of this, the operand must be loaded into a register
                            // and cannot be contained.
                            return;
                        }

                        case NI_AVX512F_ShiftLeftMask:
                        case NI_AVX512F_ShiftRightMask:
                        {
                            // These intrinsics don't support a memory operand and
                            // we don't currently generate a jmp table fallback.

                            assert(isContainedImm);
                            return;
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
                        case NI_Vector512_GetElement:
                        {
                            if (op2->OperIsConst())
                            {
                                MakeSrcContained(node, op2);
                            }

                            if (IsContainableMemoryOp(op1) && IsSafeToContainMem(node, op1))
                            {
                                MakeSrcContained(node, op1);
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

            op1 = node->Op(1);
            op2 = node->Op(2);
            op3 = node->Op(3);

            switch (category)
            {
                case HW_Category_MemoryStore:
                    ContainCheckHWIntrinsicAddr(node, op1);
                    break;

                case HW_Category_SimpleSIMD:
                case HW_Category_SIMDScalar:
                case HW_Category_Scalar:
                {
                    if (HWIntrinsicInfo::IsFmaIntrinsic(intrinsicId))
                    {
                        // FMA is special in that any operand can be contained
                        // and any other operand can be the RMW operand.
                        //
                        // This comes about from having:
                        // * 132: op1 = (op1 * [op3]) + op2
                        // * 213: op2 = (op1 * op2) + [op3]
                        // * 231: op2 = (op2 * [op3]) + op1
                        //
                        // Since multiplication is commutative this gives us the
                        // full range of support to emit the best codegen.

                        bool supportsOp1RegOptional = false;
                        bool supportsOp2RegOptional = false;
                        bool supportsOp3RegOptional = false;

                        LIR::Use use;
                        GenTree* user = nullptr;

                        if (BlockRange().TryGetUse(node, &use))
                        {
                            user = use.User();
                        }
                        unsigned resultOpNum = node->GetResultOpNumForRmwIntrinsic(user, op1, op2, op3);

                        // Prioritize Containable op. Check if any one of the op is containable first.
                        // Set op regOptional only if none of them is containable.

                        // Prefer to make op3 contained as it doesn't require reordering operands
                        if ((resultOpNum != 3) && IsContainableHWIntrinsicOp(node, op3, &supportsOp3RegOptional))
                        {
                            // result = (op1 * op2) + [op3]
                            MakeSrcContained(node, op3);
                        }
                        else if ((resultOpNum != 2) && IsContainableHWIntrinsicOp(node, op2, &supportsOp2RegOptional))
                        {
                            // result = (op1 * [op2]) + op3
                            MakeSrcContained(node, op2);
                        }
                        else if ((resultOpNum != 1) && !HWIntrinsicInfo::CopiesUpperBits(intrinsicId) &&
                                 IsContainableHWIntrinsicOp(node, op1, &supportsOp1RegOptional))
                        {
                            // result = ([op1] * op2) + op3
                            MakeSrcContained(node, op1);
                        }
                        else if (supportsOp3RegOptional)
                        {
                            assert(resultOpNum != 3);
                            MakeSrcRegOptional(node, op3);

                            // TODO-XArch-CQ: Any operand can be reg-optional.
                            //                https://github.com/dotnet/runtime/issues/6358
                        }
                        else if (supportsOp2RegOptional)
                        {
                            assert(resultOpNum != 2);
                            MakeSrcRegOptional(node, op2);
                        }
                        else if (supportsOp1RegOptional)
                        {
                            MakeSrcRegOptional(node, op1);
                        }
                    }
                    else if (HWIntrinsicInfo::IsPermuteVar2x(intrinsicId))
                    {
                        // PermuteVar2x is similarly special in that op1 and op3
                        // are commutative and op1 or op2 can be the RMW operand.
                        //
                        // This comes about from having:
                        // * i2: op2 = permutex2var(op1, op2, op3)
                        // * t2: op1 = permutex2var(op1, op2, op3)
                        //
                        // Given op1 and op3 are commutative this also gives us the full
                        // range of support. However, given we can only swap op1/op3 if
                        // we toggle a bit in the indices (op2) and the cost of this is
                        // another memory load if op2 isn't constant, we don't swap in that
                        // case to avoid another memory access for the toggle operand

                        bool supportsOp1RegOptional = false;
                        bool supportsOp3RegOptional = false;
                        bool swapOperands           = false;
                        bool isOp2Cns               = op2->IsCnsVec();

                        LIR::Use use;
                        GenTree* user = nullptr;

                        if (BlockRange().TryGetUse(node, &use))
                        {
                            user = use.User();
                        }
                        unsigned resultOpNum = node->GetResultOpNumForRmwIntrinsic(user, op1, op2, op3);

                        // Prioritize Containable op. Check if any one of the op is containable first.
                        // Set op regOptional only if none of them is containable.

                        // Prefer to make op3 contained as it doesn't require reordering operands
                        if (((resultOpNum != 3) || !isOp2Cns) &&
                            IsContainableHWIntrinsicOp(node, op3, &supportsOp3RegOptional))
                        {
                            MakeSrcContained(node, op3);
                        }
                        else if ((resultOpNum != 2) && isOp2Cns &&
                                 IsContainableHWIntrinsicOp(node, op1, &supportsOp1RegOptional))
                        {
                            MakeSrcContained(node, op1);

                            // Swap the operands here to make the containment checks in codegen significantly simpler
                            swapOperands = true;
                        }
                        else if (supportsOp3RegOptional)
                        {
                            MakeSrcRegOptional(node, op3);

                            // TODO-XArch-CQ: Either op1 or op3 can be reg-optional.
                            //                https://github.com/dotnet/runtime/issues/6358
                        }
                        else if (supportsOp1RegOptional)
                        {
                            MakeSrcRegOptional(node, op1);

                            // Swap the operands here to make the containment checks in codegen significantly simpler
                            swapOperands = true;
                        }

                        if (swapOperands)
                        {
                            assert(op2->IsCnsVec());
                            std::swap(node->Op(1), node->Op(3));

                            uint32_t elemSize  = genTypeSize(simdBaseType);
                            uint32_t elemCount = simdSize / elemSize;
                            uint64_t toggleBit = 0;

                            switch (elemSize)
                            {
                                case 1:
                                {
                                    // We pick a base uint8_t of:
                                    // * TYP_SIMD16: 0x10
                                    // * TYP_SIMD32: 0x20
                                    // * TYP_SIMD64: 0x40
                                    switch (simdSize)
                                    {
                                        case 16:
                                            toggleBit = 0x1010101010101010;
                                            break;
                                        case 32:
                                            toggleBit = 0x2020202020202020;
                                            break;
                                        default:
                                            assert(simdSize == 64);
                                            toggleBit = 0x4040404040404040;
                                            break;
                                    }
                                    break;
                                }

                                case 2:
                                {
                                    // We pick a base uint16_t of:
                                    // * TYP_SIMD16: 0x08
                                    // * TYP_SIMD32: 0x10
                                    // * TYP_SIMD64: 0x20
                                    switch (simdSize)
                                    {
                                        case 16:
                                            toggleBit = 0x0008000800080008;
                                            break;
                                        case 32:
                                            toggleBit = 0x0010001000100010;
                                            break;
                                        default:
                                            assert(simdSize == 64);
                                            toggleBit = 0x0020002000200020;
                                            break;
                                    }
                                    break;
                                }

                                case 4:
                                {
                                    // We pick a base uint32_t of:
                                    // * TYP_SIMD16: 0x04
                                    // * TYP_SIMD32: 0x08
                                    // * TYP_SIMD64: 0x10
                                    switch (simdSize)
                                    {
                                        case 16:
                                            toggleBit = 0x0000000400000004;
                                            break;
                                        case 32:
                                            toggleBit = 0x0000000800000008;
                                            break;
                                        default:
                                            assert(simdSize == 64);
                                            toggleBit = 0x0000001000000010;
                                            break;
                                    }
                                    break;
                                }

                                case 8:
                                {
                                    // We pick a base uint32_t of:
                                    // * TYP_SIMD16: 0x02
                                    // * TYP_SIMD32: 0x04
                                    // * TYP_SIMD64: 0x08
                                    switch (simdSize)
                                    {
                                        case 16:
                                            toggleBit = 0x0000000000000002;
                                            break;
                                        case 32:
                                            toggleBit = 0x0000000000000004;
                                            break;
                                        default:
                                            assert(simdSize == 64);
                                            toggleBit = 0x0000000000000008;
                                            break;
                                    }
                                    break;
                                }

                                default:
                                {
                                    unreached();
                                }
                            }

                            GenTreeVecCon* vecCon = op2->AsVecCon();

                            for (uint32_t i = 0; i < (simdSize / 8); i++)
                            {
                                vecCon->gtSimdVal.u64[i] ^= toggleBit;
                            }
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
                            case NI_AVX512F_BlendVariableMask:
                            {
                                if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op2);
                                }
                                break;
                            }

                            case NI_AVXVNNI_MultiplyWideningAndAdd:
                            case NI_AVXVNNI_MultiplyWideningAndAddSaturate:
                            {
                                if (IsContainableHWIntrinsicOp(node, op3, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op3);
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op3);
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
                                    node->Op(1) = op2;
                                    node->Op(2) = op1;
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op2);
                                }
                                break;
                            }

                            case NI_X86Base_DivRem:
                            case NI_X86Base_X64_DivRem:
                            {
                                // DIV only allows divisor (op3) in memory
                                if (IsContainableHWIntrinsicOp(node, op3, &supportsRegOptional))
                                {
                                    MakeSrcContained(node, op3);
                                }
                                else if (supportsRegOptional)
                                {
                                    MakeSrcRegOptional(node, op3);
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
                        case NI_AVX512F_AlignRight32:
                        case NI_AVX512F_AlignRight64:
                        case NI_AVX512F_GetMantissaScalar:
                        case NI_AVX512F_InsertVector128:
                        case NI_AVX512F_InsertVector256:
                        case NI_AVX512F_RoundScaleScalar:
                        case NI_AVX512F_Shuffle:
                        case NI_AVX512F_Shuffle4x128:
                        case NI_AVX512F_VL_AlignRight32:
                        case NI_AVX512F_VL_AlignRight64:
                        case NI_AVX512F_VL_Shuffle2x128:
                        case NI_AVX512BW_AlignRight:
                        case NI_AVX512BW_SumAbsoluteDifferencesInBlock32:
                        case NI_AVX512BW_VL_SumAbsoluteDifferencesInBlock32:
                        case NI_AVX512DQ_InsertVector128:
                        case NI_AVX512DQ_InsertVector256:
                        case NI_AVX512DQ_Range:
                        case NI_AVX512DQ_RangeScalar:
                        case NI_AVX512DQ_VL_Range:
                        case NI_AVX512DQ_ReduceScalar:
                        case NI_PCLMULQDQ_CarrylessMultiply:
                        {
                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op2, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op2);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op2);
                            }
                            break;
                        }

                        case NI_SSE41_Insert:
                        {
                            GenTree* lastOp = node->Op(numArgs);

                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (simdBaseType == TYP_FLOAT)
                            {
                                assert(lastOp->IsCnsIntOrI());

                                // Sse41.Insert has:
                                //  * Bits 0-3: zmask
                                //  * Bits 4-5: count_d
                                //  * Bits 6-7: count_s (register form only)
                                //
                                // Where zmask specifies which elements to zero
                                // Where count_d specifies the destination index the value is being inserted to
                                // Where count_s specifies the source index of the value being inserted

                                if (op1->IsVectorZero())
                                {
// When op1 is zero, we can contain it and we expect that
// ival is already in the correct state to account for it

#if DEBUG
                                    ssize_t ival = lastOp->AsIntConCommon()->IconValue();

                                    ssize_t zmask   = (ival & 0x0F);
                                    ssize_t count_d = (ival & 0x30) >> 4;
                                    ssize_t count_s = (ival & 0xC0) >> 6;

                                    zmask |= ~(ssize_t(1) << count_d);
                                    zmask &= 0x0F;

                                    ssize_t expected = (count_s << 6) | (count_d << 4) | (zmask);
                                    assert(ival == expected);
#endif

                                    MakeSrcContained(node, op1);
                                }
                                else if (op2->IsVectorZero())
                                {
// When op2 is zero, we can contain it and we expect that
// zmask is already in the correct state to account for it

#if DEBUG
                                    ssize_t ival = lastOp->AsIntConCommon()->IconValue();

                                    ssize_t zmask   = (ival & 0x0F);
                                    ssize_t count_d = (ival & 0x30) >> 4;
                                    ssize_t count_s = (ival & 0xC0) >> 6;

                                    zmask |= (ssize_t(1) << count_d);
                                    zmask &= 0x0F;

                                    ssize_t expected = (count_s << 6) | (count_d << 4) | (zmask);
                                    assert(ival == expected);
#endif

                                    MakeSrcContained(node, op2);
                                }
                            }

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
        else if (numArgs == 4)
        {
            // four argument intrinsics should not be marked commutative
            assert(!isCommutative);

            op1 = node->Op(1);
            op2 = node->Op(2);
            op3 = node->Op(3);
            op4 = node->Op(4);

            switch (category)
            {
                case HW_Category_IMM:
                {
                    bool supportsRegOptional = false;

                    switch (intrinsicId)
                    {
                        case NI_AVX512F_Fixup:
                        case NI_AVX512F_FixupScalar:
                        case NI_AVX512F_VL_Fixup:
                        {
                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op3, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op3);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op3);
                            }

                            if (!node->isRMWHWIntrinsic(comp))
                            {
                                // op1 is never selected by the table so
                                // we can contain and ignore any register
                                // allocated to it resulting in better
                                // non-RMW based codegen.

                                MakeSrcContained(node, op1);
                            }
                            break;
                        }

                        case NI_AVX512F_TernaryLogic:
                        case NI_AVX512F_VL_TernaryLogic:
                        {
                            if (!isContainedImm)
                            {
                                // Don't contain if we're generating a jmp table fallback
                                break;
                            }

                            if (IsContainableHWIntrinsicOp(node, op3, &supportsRegOptional))
                            {
                                MakeSrcContained(node, op3);
                            }
                            else if (supportsRegOptional)
                            {
                                MakeSrcRegOptional(node, op3);
                            }

                            uint8_t                 control  = static_cast<uint8_t>(op4->AsIntCon()->gtIconVal);
                            const TernaryLogicInfo& info     = TernaryLogicInfo::lookup(control);
                            TernaryLogicUseFlags    useFlags = info.GetAllUseFlags();

                            if (useFlags != TernaryLogicUseFlags::ABC)
                            {
                                assert(!node->isRMWHWIntrinsic(comp));

                                // op1, and possibly op2, are never selected
                                // by the table so we can contain and ignore
                                // any register allocated to it resulting in
                                // better non-RMW based codegen.

                                MakeSrcContained(node, op1);

                                if (useFlags == TernaryLogicUseFlags::C)
                                {
                                    MakeSrcContained(node, op2);
                                }
                                else
                                {
                                    assert(useFlags == TernaryLogicUseFlags::BC);
                                }
                            }
                            break;
                        }

                        default:
                        {
                            assert(!"Unhandled containment for quaternary hardware intrinsic with immediate operand");
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
        isSafeToContainOp1 = IsSafeToMarkRegOptional(node, op1);
        isSafeToContainOp2 = IsSafeToMarkRegOptional(node, op2);
        SetRegOptionalForBinOp(node, isSafeToContainOp1, isSafeToContainOp2);
    }
}

#endif // TARGET_XARCH
