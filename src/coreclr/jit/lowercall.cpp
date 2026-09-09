// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lower.h"

//------------------------------------------------------------------------
// LowerArg:
//   Lower one argument of a call. This entails inserting putarg nodes between
//   the call and the argument. This is the point at which the source is
//   consumed and the value transitions from control of the register allocator
//   to the calling convention.
//
// Arguments:
//    call    - The call node
//    callArg - Call argument
//
void Lowering::LowerArg(GenTreeCall* call, CallArg* callArg)
{
    GenTree** ppArg = &callArg->NodeRef();
    GenTree*  arg   = *ppArg;
    assert(arg != nullptr);

    JITDUMP("Lowering arg: \n");
    DISPTREERANGE(BlockRange(), arg);
    assert(arg->IsValue());

    // If we hit this we are probably double-lowering.
    assert(!arg->OperIsPutArg());

    const ABIPassingInformation& abiInfo = callArg->AbiInfo;
    JITDUMP("Passed in ");
    DBEXEC(m_compiler->verbose, abiInfo.Dump());

#if !defined(TARGET_64BIT) && !defined(TARGET_WASM)
    if (m_compiler->opts.compUseSoftFP && arg->TypeIs(TYP_DOUBLE))
    {
        // Unlike TYP_LONG we do no decomposition for doubles, yet we maintain
        // it as a primitive type until lowering. So we need to get it into the
        // right form here.

        unsigned argLclNum = m_compiler->lvaGrabTemp(false DEBUGARG("double arg on softFP"));
        GenTree* store     = m_compiler->gtNewTempStore(argLclNum, arg);
        GenTree* low       = m_compiler->gtNewLclFldNode(argLclNum, TYP_INT, 0);
        GenTree* high      = m_compiler->gtNewLclFldNode(argLclNum, TYP_INT, 4);
        GenTree* longNode  = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, low, high);
        BlockRange().InsertAfter(arg, store, low, high, longNode);

        *ppArg = arg = longNode;

        m_compiler->lvaSetVarDoNotEnregister(argLclNum DEBUGARG(DoNotEnregisterReason::LocalField));

        JITDUMP("Transformed double-typed arg on softFP to LONG node\n");
    }

    if (varTypeIsLong(arg))
    {
        noway_assert(arg->OperIs(GT_LONG));
        GenTreeFieldList* fieldList = new (m_compiler, GT_FIELD_LIST) GenTreeFieldList();
        fieldList->AddFieldLIR(m_compiler, arg->gtGetOp1(), 0, TYP_INT);
        fieldList->AddFieldLIR(m_compiler, arg->gtGetOp2(), 4, TYP_INT);
        BlockRange().InsertBefore(arg, fieldList);

        BlockRange().Remove(arg);
        *ppArg = arg = fieldList;

        JITDUMP("Transformed long arg on 32-bit to FIELD_LIST node\n");
    }
#endif // !defined(TARGET_64BIT) && !defined(TARGET_WASM)

#if FEATURE_ARG_SPLIT
    // Structs can be split into register(s) and stack on some targets
    if (compFeatureArgSplit() && abiInfo.IsSplitAcrossRegistersAndStack())
    {
        SplitArgumentBetweenRegistersAndStack(call, callArg);
        LowerArg(call, callArg);
        return;
    }
    else
#endif // FEATURE_ARG_SPLIT
    {
        if (abiInfo.HasAnyRegisterSegment())
        {
            if (arg->OperIs(GT_FIELD_LIST) || (abiInfo.NumSegments > 1))
            {
                if (!arg->OperIs(GT_FIELD_LIST))
                {
                    // Primitive arg, but the ABI requires it to be split into
                    // registers. Insert the field list here.
                    GenTreeFieldList* fieldList = m_compiler->gtNewFieldList();
                    fieldList->AddFieldLIR(m_compiler, arg, 0, genActualType(arg->TypeGet()));
                    BlockRange().InsertAfter(arg, fieldList);
                    arg = *ppArg = fieldList;
                }

                LowerArgFieldList(callArg, arg->AsFieldList());
                arg = *ppArg;
            }
            else
            {
                assert(abiInfo.HasExactlyOneRegisterSegment());
                InsertPutArgReg(ppArg, abiInfo.Segment(0));
                arg = *ppArg;
            }
        }
        else
        {
            assert(abiInfo.NumSegments == 1);
            const ABIPassingSegment& stackSeg             = abiInfo.Segment(0);
            const bool               putInIncomingArgArea = call->IsFastTailCall();

            GenTree* putArg = new (m_compiler, GT_PUTARG_STK)
                GenTreePutArgStk(GT_PUTARG_STK, TYP_VOID, arg, stackSeg.GetStackOffset(), stackSeg.GetStackSize(), call,
                                 putInIncomingArgArea);

            BlockRange().InsertAfter(arg, putArg);
            *ppArg = arg = putArg;
        }
    }

    if (arg->OperIsPutArgStk())
    {
        LowerPutArgStk(arg->AsPutArgStk());
    }

    DISPTREERANGE(BlockRange(), arg);
}

//------------------------------------------------------------------------
// SplitArgumentBetweenRegistersAndStack:
//   Split an argument that is passed in both registers and stack into two
//   separate arguments, one for registers and one for stack.
//
// Parameters:
//   call    - The call node
//   callArg - Call argument
//
// Remarks:
//   The argument is changed to be its stack part, and a new argument is
//   inserted after it representing its registers.
//
void Lowering::SplitArgumentBetweenRegistersAndStack(GenTreeCall* call, CallArg* callArg)
{
    GenTree** ppArg = &callArg->NodeRef();
    GenTree*  arg   = *ppArg;

    assert(arg->OperIs(GT_BLK, GT_FIELD_LIST) || arg->OperIsLocalRead());
    assert(!call->IsFastTailCall());

    const ABIPassingInformation& abiInfo = callArg->AbiInfo;
    assert(abiInfo.IsSplitAcrossRegistersAndStack());

#ifdef DEBUG
    for (unsigned i = 0; i < abiInfo.NumSegments; i++)
    {
        assert((i < abiInfo.NumSegments - 1) == abiInfo.Segment(i).IsPassedInRegister());
    }
#endif

    unsigned                 numRegs  = abiInfo.NumSegments - 1;
    const ABIPassingSegment& stackSeg = abiInfo.Segment(abiInfo.NumSegments - 1);

    JITDUMP("Dividing split arg [%06u] with %u registers, %u stack space into two arguments\n",
            Compiler::dspTreeID(arg), numRegs, stackSeg.Size);

    ClassLayout* registersLayout = callArg->GetSignatureLayout()->SliceLayout(m_compiler, 0, stackSeg.Offset);
    ClassLayout* stackLayout =
        callArg->GetSignatureLayout()->SliceLayout(m_compiler, stackSeg.Offset,
                                                   callArg->GetSignatureLayout()->GetSize() - stackSeg.Offset);

    GenTree* stackNode     = nullptr;
    GenTree* registersNode = nullptr;

    if (arg->OperIsFieldList())
    {
        JITDUMP("Argument is a FIELD_LIST\n");

        GenTreeFieldList::Use* splitPoint = nullptr;
        // Split the field list into its register and stack parts.
        for (GenTreeFieldList::Use& use : arg->AsFieldList()->Uses())
        {
            if (use.GetOffset() >= stackSeg.Offset)
            {
                splitPoint = &use;
                JITDUMP("Found split point at offset %u\n", splitPoint->GetOffset());
                break;
            }

            if (use.GetOffset() + genTypeSize(use.GetType()) > stackSeg.Offset)
            {
                // Field overlaps partially into the stack segment, cannot
                // handle this without spilling.
                break;
            }
        }

        if (splitPoint == nullptr)
        {
            JITDUMP("No clean split point found, spilling FIELD_LIST\n");

            unsigned int newLcl =
                StoreFieldListToNewLocal(m_compiler->typGetObjLayout(callArg->GetSignatureClassHandle()),
                                         arg->AsFieldList());
            stackNode     = m_compiler->gtNewLclFldNode(newLcl, TYP_STRUCT, stackSeg.Offset, stackLayout);
            registersNode = m_compiler->gtNewLclFldNode(newLcl, TYP_STRUCT, 0, registersLayout);
            BlockRange().InsertBefore(arg, stackNode);
            BlockRange().InsertBefore(arg, registersNode);
        }
        else
        {
            stackNode     = m_compiler->gtNewFieldList();
            registersNode = m_compiler->gtNewFieldList();

            BlockRange().InsertBefore(arg, stackNode);
            BlockRange().InsertBefore(arg, registersNode);

            for (GenTreeFieldList::Use& use : arg->AsFieldList()->Uses())
            {
                if (&use == splitPoint)
                {
                    break;
                }

                registersNode->AsFieldList()->AddFieldLIR(m_compiler, use.GetNode(), use.GetOffset(), use.GetType());
            }

            for (GenTreeFieldList::Use* use = splitPoint; use != nullptr; use = use->GetNext())
            {
                stackNode->AsFieldList()->AddFieldLIR(m_compiler, use->GetNode(), use->GetOffset() - stackSeg.Offset,
                                                      use->GetType());
            }
        }

        BlockRange().Remove(arg);
    }
    else if (arg->OperIs(GT_BLK))
    {
        JITDUMP("Argument is a BLK\n");

        GenTree*       blkAddr = arg->AsBlk()->Addr();
        target_ssize_t offset  = 0;
        m_compiler->gtPeelOffsets(&blkAddr, &offset);

        LIR::Use addrUse;
        bool     gotUse = BlockRange().TryGetUse(blkAddr, &addrUse);
        assert(gotUse);

        unsigned addrLcl;
        if (addrUse.Def()->OperIsScalarLocal() &&
            !m_compiler->lvaGetDesc(addrUse.Def()->AsLclVarCommon())->IsAddressExposed() &&
            IsInvariantInRange(addrUse.Def(), arg))
        {
            JITDUMP("Reusing LCL_VAR\n");
            addrLcl = addrUse.Def()->AsLclVarCommon()->GetLclNum();
        }
        else
        {
            JITDUMP("Spilling address\n");
            addrLcl = addrUse.ReplaceWithLclVar(m_compiler);
        }

        auto createAddr = [=](unsigned offs) {
            GenTree* addr = m_compiler->gtNewLclVarNode(addrLcl);
            offs += (unsigned)offset;
            if (offs != 0)
            {
                GenTree* addrOffs = m_compiler->gtNewIconNode((ssize_t)offs, TYP_I_IMPL);
                addr = m_compiler->gtNewOperNode(GT_ADD, varTypeIsGC(addr) ? TYP_BYREF : TYP_I_IMPL, addr, addrOffs);
            }

            return addr;
        };

        GenTree* addr = createAddr(stackSeg.Offset);
        stackNode     = m_compiler->gtNewBlkIndir(stackLayout, addr, arg->gtFlags & GTF_IND_COPYABLE_FLAGS);
        BlockRange().InsertBefore(arg, LIR::SeqTree(m_compiler, stackNode));
        LowerRange(addr, stackNode);

        registersNode = m_compiler->gtNewFieldList();
        BlockRange().InsertBefore(arg, registersNode);

        for (unsigned i = 0; i < numRegs; i++)
        {
            const ABIPassingSegment& seg = abiInfo.Segment(i);

            GenTree* addr  = createAddr(seg.Offset);
            GenTree* indir = m_compiler->gtNewIndir(seg.GetRegisterType(callArg->GetSignatureLayout()), addr,
                                                    arg->gtFlags & GTF_IND_COPYABLE_FLAGS);
            registersNode->AsFieldList()->AddFieldLIR(m_compiler, indir, seg.Offset, indir->TypeGet());
            BlockRange().InsertBefore(registersNode, LIR::SeqTree(m_compiler, indir));
            LowerRange(addr, indir);
        }

        BlockRange().Remove(arg, /* markOperandsUnused */ true);
    }
    else
    {
        assert(arg->OperIsLocalRead());

        JITDUMP("Argument is a local\n");

        GenTreeLclVarCommon* lcl = arg->AsLclVarCommon();

        stackNode =
            m_compiler->gtNewLclFldNode(lcl->GetLclNum(), TYP_STRUCT, lcl->GetLclOffs() + stackSeg.Offset, stackLayout);
        BlockRange().InsertBefore(arg, stackNode);

        m_compiler->lvaSetVarDoNotEnregister(lcl->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));

        registersNode = m_compiler->gtNewFieldList();
        BlockRange().InsertBefore(arg, registersNode);

        for (unsigned i = 0; i < numRegs; i++)
        {
            const ABIPassingSegment& seg = abiInfo.Segment(i);
            GenTree*                 fldNode =
                m_compiler->gtNewLclFldNode(lcl->GetLclNum(), seg.GetRegisterType(callArg->GetSignatureLayout()),
                                            lcl->GetLclOffs() + seg.Offset);
            registersNode->AsFieldList()->AddFieldLIR(m_compiler, fldNode, seg.Offset, fldNode->TypeGet());
            BlockRange().InsertBefore(registersNode, fldNode);
        }

        BlockRange().Remove(arg);
    }

    JITDUMP("New stack node is:\n");
    DISPTREERANGE(BlockRange(), stackNode);

    JITDUMP("New registers node is:\n");
    DISPTREERANGE(BlockRange(), registersNode);

    ABIPassingSegment     newStackSeg = ABIPassingSegment::OnStack(stackSeg.GetStackOffset(), 0, stackSeg.Size);
    ABIPassingInformation newStackAbi = ABIPassingInformation::FromSegment(m_compiler, false, newStackSeg);

    ABIPassingInformation newRegistersAbi(m_compiler, numRegs);
    for (unsigned i = 0; i < numRegs; i++)
    {
        newRegistersAbi.Segment(i) = abiInfo.Segment(i);
    }

    callArg->AbiInfo = newStackAbi;
    *ppArg = arg = stackNode;

    NewCallArg newRegisterArgAdd = NewCallArg::Struct(registersNode, TYP_STRUCT, registersLayout);
    CallArg*   newRegisterArg    = call->gtArgs.InsertAfter(m_compiler, callArg, newRegisterArgAdd);

    newRegisterArg->AbiInfo = newRegistersAbi;

    if (callArg->GetLateNode() != nullptr)
    {
        newRegisterArg->SetLateNext(callArg->GetLateNext());
        callArg->SetLateNext(newRegisterArg);

        newRegisterArg->SetLateNode(registersNode);
        newRegisterArg->SetEarlyNode(nullptr);
    }

    JITDUMP("Added a new call arg. New call is:\n");
    DISPTREERANGE(BlockRange(), call);
}

//------------------------------------------------------------------------
// InsertBitCastIfNecessary:
//   Insert a bitcast if a primitive argument being passed in a register is not
//   evaluated in the right type of register.
//
// Arguments:
//    argNode         - Edge for the argument
//    registerSegment - Register that the argument is going into
//
void Lowering::InsertBitCastIfNecessary(GenTree** argNode, const ABIPassingSegment& registerSegment)
{
    if (varTypeUsesIntReg(*argNode) == genIsValidIntReg(registerSegment.GetRegister()))
    {
        return;
    }

    JITDUMP("Argument node [%06u] needs to be passed in %s; inserting bitcast\n", Compiler::dspTreeID(*argNode),
            getRegName(registerSegment.GetRegister()));

    // Due to padding the node may be smaller than the register segment. In
    // such cases we cut off the end of the segment to get an appropriate
    // register type for the bitcast.
    ABIPassingSegment cutRegisterSegment = registerSegment;
    unsigned          argNodeSize        = genTypeSize(genActualType(*argNode));
    if (registerSegment.Size > argNodeSize)
    {
        cutRegisterSegment =
            ABIPassingSegment::InRegister(registerSegment.GetRegister(), registerSegment.Offset, argNodeSize);
    }

    var_types bitCastType = cutRegisterSegment.GetRegisterType();

    GenTreeUnOp* bitCast = m_compiler->gtNewBitCastNode(bitCastType, *argNode);
    BlockRange().InsertAfter(*argNode, bitCast);

    *argNode = bitCast;
    if (!TryRemoveBitCast(bitCast))
    {
        ContainCheckBitCast(bitCast);
    }
}

//------------------------------------------------------------------------
// InsertPutArgReg:
//   Insert a PUTARG_REG node for the specified edge. If the argument node does
//   not fit the register type, then also insert a bitcast.
//
// Arguments:
//    argNode         - Edge for the argument
//    registerSegment - Register that the argument is going into
//
void Lowering::InsertPutArgReg(GenTree** argNode, const ABIPassingSegment& registerSegment)
{
    assert(registerSegment.IsPassedInRegister());

    InsertBitCastIfNecessary(argNode, registerSegment);

#if HAS_FIXED_REGISTER_SET
    GenTree* putArg = m_compiler->gtNewPutArgReg(genActualType(*argNode), *argNode, registerSegment.GetRegister());
    BlockRange().InsertAfter(*argNode, putArg);
    *argNode = putArg;
#endif
}

//------------------------------------------------------------------------
// LowerArgsForCall:
//   Lower the arguments of a call node.
//
// Arguments:
//    call - Call node
//
void Lowering::LowerArgsForCall(GenTreeCall* call)
{
    JITDUMP("Args:\n======\n");
    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        LowerArg(call, &arg);
    }

    JITDUMP("\nLate args:\n======\n");
    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        LowerArg(call, &arg);
    }

#if defined(TARGET_X86) && defined(FEATURE_IJW)
    LowerSpecialCopyArgs(call);
#endif // defined(TARGET_X86) && defined(FEATURE_IJW)

    LegalizeArgPlacement(call);
    AfterLowerArgsForCall(call);
}

#if !defined(TARGET_WASM)

//------------------------------------------------------------------------
// AfterLowerArgsForCall: post processing after call args are lowered
//
// Arguments:
//    call - Call node
//
void Lowering::AfterLowerArgsForCall(GenTreeCall* call)
{
    // no-op for non-Wasm targets
}

#endif // !defined(TARGET_WASM)

#if defined(TARGET_X86) && defined(FEATURE_IJW)
//------------------------------------------------------------------------
// LowerSpecialCopyArgs: Lower special copy arguments for P/Invoke IL stubs
//
// Arguments:
//    call - the call node
//
// Notes:
//    This method is used for P/Invoke IL stubs on x86 to handle arguments with special copy semantics.
//    In particular, this method implements copy-constructor semantics for managed-to-unmanaged IL stubs
//    for C++/CLI. In this case, the managed argument is passed by (managed or unmanaged) pointer in the
//    P/Invoke signature with a speial modreq, but is passed to the unmanaged function by value.
//    The value passed to the unmanaged function must be created through a copy-constructor call copying from
//    the original source argument.
//    We assume that the IL stub will be generated such that the following holds true:
//       - If an argument to the IL stub has the special modreq, then its corresponding argument to the
//         unmanaged function will be passed as the same argument index. Therefore, we can introduce the copy call
//         from the original source argument to the argument slot in the unmanaged call.
void Lowering::LowerSpecialCopyArgs(GenTreeCall* call)
{
    // We only need to use the special copy helper on P/Invoke IL stubs
    // for the unmanaged call.
    if (m_compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) && m_compiler->compMethodRequiresPInvokeFrame() &&
        call->IsUnmanaged() && m_compiler->compHasSpecialCopyArgs())
    {
        // Unmanaged calling conventions on Windows x86 are passed in reverse order
        // of managed args, so we need to count down the number of args.
        // If the call is thiscall, we need to account for the this parameter,
        // which will be first in the list.
        // The this parameter is always passed in registers, so we can ignore it.
        unsigned argIndex = call->gtArgs.CountUserArgs() - 1;
        // The arguments of the unmanaged call are the leading arguments of the IL stub, so the stub
        // cannot have fewer of them. It can have more: an unmanaged CALLI stub takes the call target
        // as an extra trailing argument that is not passed on to the unmanaged call.
        assert(call->gtArgs.CountUserArgs() <= m_compiler->info.compILargsCount);
        bool checkForUnmanagedThisArg = call->GetUnmanagedCallConv() == CorInfoCallConvExtension::Thiscall;
        for (CallArg& arg : call->gtArgs.Args())
        {
            if (!arg.IsUserArg())
            {
                continue;
            }

            if (checkForUnmanagedThisArg && argIndex == call->gtArgs.CountUserArgs() - 1)
            {
                assert(arg.GetNode()->OperIs(GT_PUTARG_REG));
                checkForUnmanagedThisArg = false;
                continue;
            }

            unsigned paramLclNum = m_compiler->compMapILargNum(argIndex);
            assert(paramLclNum < m_compiler->info.compArgsCount);

            // check if parameter at the same index as the IL argument is marked as requiring special copy, assuming
            // that it is being passed 1:1 to the pinvoke
            if (m_compiler->argRequiresSpecialCopy(paramLclNum) && (arg.GetSignatureType() == TYP_STRUCT))
            {
                assert(arg.GetNode()->OperIs(GT_PUTARG_STK));
                InsertSpecialCopyArg(arg.GetNode()->AsPutArgStk(), arg.GetSignatureClassHandle(), paramLclNum);
            }

            argIndex--;
        }
    }
}

//------------------------------------------------------------------------
// InsertSpecialCopyArg: Insert a call to the special copy helper to copy from the (possibly value pointed-to by) local
// lclnum to the argument slot represented by putArgStk
//
// Arguments:
//    putArgStk - the PutArgStk node representing the stack slot of the argument
//    argType - the struct type of the argument
//    lclNum - the local to use as the source for the special copy helper
//
// Notes:
//   This method assumes that lclNum is either a by-ref to a struct of type argType
//   or a struct of type argType.
//   We use this to preserve special copy semantics for interop calls where we pass in a byref to a struct into a
//   P/Invoke with a special modreq and the native function expects to recieve the struct by value with the argument
//   being passed in having been created by the special copy helper.
//
void Lowering::InsertSpecialCopyArg(GenTreePutArgStk* putArgStk, CORINFO_CLASS_HANDLE argType, unsigned lclNum)
{
    assert(putArgStk != nullptr);
    GenTree* dest = m_compiler->gtNewPhysRegNode(REG_SPBASE, TYP_I_IMPL);

    GenTree*  src;
    var_types lclType = m_compiler->lvaGetRealType(lclNum);

    if (lclType == TYP_BYREF || lclType == TYP_I_IMPL)
    {
        src = m_compiler->gtNewLclVarNode(lclNum, lclType);
    }
    else
    {
        assert(lclType == TYP_STRUCT);
        src = m_compiler->gtNewLclAddrNode(lclNum, 0, TYP_I_IMPL);
    }

    GenTree* destPlaceholder = m_compiler->gtNewZeroConNode(dest->TypeGet());
    GenTree* srcPlaceholder  = m_compiler->gtNewZeroConNode(src->TypeGet());

    GenTreeCall* call =
        m_compiler->gtNewUserCallNode(m_compiler->info.compCompHnd->getSpecialCopyHelper(argType), TYP_VOID);

    call->gtArgs.PushBack(m_compiler, NewCallArg::Primitive(destPlaceholder));
    call->gtArgs.PushBack(m_compiler, NewCallArg::Primitive(srcPlaceholder));

    m_compiler->fgMorphArgs(call);

    LIR::Range callRange      = LIR::SeqTree(m_compiler, call);
    GenTree*   callRangeStart = callRange.FirstNode();
    GenTree*   callRangeEnd   = callRange.LastNode();

    BlockRange().InsertAfter(putArgStk, std::move(callRange));
    BlockRange().InsertAfter(putArgStk, dest);
    BlockRange().InsertAfter(putArgStk, src);

    LIR::Use destUse;
    LIR::Use srcUse;
    BlockRange().TryGetUse(destPlaceholder, &destUse);
    BlockRange().TryGetUse(srcPlaceholder, &srcUse);
    destUse.ReplaceWith(dest);
    srcUse.ReplaceWith(src);
    destPlaceholder->SetUnusedValue();
    srcPlaceholder->SetUnusedValue();

    LowerRange(callRangeStart, callRangeEnd);

    // Finally move all GT_PUTARG_* nodes
    // Re-use the existing logic for CFG call args here
    MovePutArgNodesUpToCall(call);

    BlockRange().Remove(destPlaceholder);
    BlockRange().Remove(srcPlaceholder);
}
#endif // defined(TARGET_X86) && defined(FEATURE_IJW)

//------------------------------------------------------------------------
// LegalizeArgPlacement: Move arg placement nodes (PUTARG_*) into a legal
// ordering after they have been created.
//
// Arguments:
//   call - GenTreeCall node that has had PUTARG_* nodes created for arguments.
//
// Remarks:
//   PUTARG_* nodes are created and inserted right after the definitions of the
//   argument values. However, there are constraints on how the PUTARG nodes
//   can appear:
//
//   - No other GT_CALL nodes are allowed between a PUTARG_REG node and the
//   call. For FEATURE_FIXED_OUT_ARGS this condition is also true for
//   PUTARG_STK.
//   - For !FEATURE_FIXED_OUT_ARGS, the PUTARG_STK nodes must come in push
//   order.
//
//   Morph has mostly already solved this problem, but transformations on LIR
//   can make the ordering we end up with here illegal. This function legalizes
//   the placement while trying to minimize the distance between an argument
//   definition and its corresponding placement node.
//
void Lowering::LegalizeArgPlacement(GenTreeCall* call)
{
    size_t numMarked = MarkCallPutArgAndFieldListNodes(call);

    // We currently do not try to resort the PUTARG_STK nodes, but rather just
    // assert here that they are ordered.
#if defined(DEBUG) && !FEATURE_FIXED_OUT_ARGS
    unsigned nextPushOffset = UINT_MAX;
#endif

    GenTree* cur = call->gtPrev;
    while (numMarked > 0)
    {
        assert(cur != nullptr);

        if ((cur->gtLIRFlags & LIR::Flags::Mark) != 0)
        {
            numMarked--;
            cur->gtLIRFlags &= ~LIR::Flags::Mark;

#if defined(DEBUG) && !FEATURE_FIXED_OUT_ARGS
            if (cur->OperIs(GT_PUTARG_STK))
            {
                // For !FEATURE_FIXED_OUT_ARGS (only x86) byte offsets are
                // subtracted from the top of the stack frame; so last pushed
                // arg has highest offset.
                assert(nextPushOffset > cur->AsPutArgStk()->getArgOffset());
                nextPushOffset = cur->AsPutArgStk()->getArgOffset();
            }
#endif
        }

        if (cur->IsCall())
        {
            break;
        }

        cur = cur->gtPrev;
    }

    if (numMarked == 0)
    {
        // Already legal; common case
        return;
    }

    JITDUMP("Call [%06u] has %zu PUTARG nodes that interfere with [%06u]; will move them after it\n",
            Compiler::dspTreeID(call), numMarked, Compiler::dspTreeID(cur));

    // We found interference; remaining PUTARG nodes need to be moved after
    // this point.
    GenTree* insertionPoint = cur;

    while (numMarked > 0)
    {
        assert(cur != nullptr);

        GenTree* prev = cur->gtPrev;
        if ((cur->gtLIRFlags & LIR::Flags::Mark) != 0)
        {
            numMarked--;
            cur->gtLIRFlags &= ~LIR::Flags::Mark;

            // For FEATURE_FIXED_OUT_ARGS: all PUTARG nodes must be moved after the interfering call
            // For !FEATURE_FIXED_OUT_ARGS: only PUTARG_REG nodes must be moved after the interfering call
            if (FEATURE_FIXED_OUT_ARGS || cur->OperIs(GT_FIELD_LIST, GT_PUTARG_REG))
            {
                JITDUMP("Relocating [%06u] after [%06u]\n", Compiler::dspTreeID(cur),
                        Compiler::dspTreeID(insertionPoint));

                BlockRange().Remove(cur);
                BlockRange().InsertAfter(insertionPoint, cur);
            }
        }

        cur = prev;
    }

    JITDUMP("Final result after legalization:\n");
    DISPTREERANGE(BlockRange(), call);
}

// helper that create a node representing a relocatable physical address computation
GenTree* Lowering::AddrGen(ssize_t addr)
{
    // this should end up in codegen as : instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, addr)
    GenTree* result = m_compiler->gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
    return result;
}

// variant that takes a void*
GenTree* Lowering::AddrGen(void* addr)
{
    return AddrGen((ssize_t)addr);
}

//------------------------------------------------------------------------
// LowerCallMemset: Replaces the following memset-like special intrinsics:
//
//    SpanHelpers.Fill<T>(ref dstRef, CNS_SIZE, CNS_VALUE)
//    CORINFO_HELP_MEMSET(ref dstRef, CNS_VALUE, CNS_SIZE)
//    SpanHelpers.ClearWithoutReferences(ref dstRef, CNS_SIZE)
//
//  with a GT_STORE_BLK node:
//
//    *  STORE_BLK struct<CNS_SIZE> (init) (Unroll)
//    +--*  LCL_VAR   byref  dstRef
//    \--*  CNS_INT   int    0
//
// Arguments:
//    tree - GenTreeCall node to replace with STORE_BLK
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::LowerCallMemset(GenTreeCall* call, GenTree** next)
{
    assert(call->IsSpecialIntrinsic(m_compiler, NI_System_SpanHelpers_Fill) ||
           call->IsSpecialIntrinsic(m_compiler, NI_System_SpanHelpers_ClearWithoutReferences) ||
           call->IsHelperCall(CORINFO_HELP_MEMSET));

    JITDUMP("Considering Memset-like call [%06d] for unrolling.. ", m_compiler->dspTreeID(call))

    if (m_compiler->info.compHasNextCallRetAddr)
    {
        JITDUMP("compHasNextCallRetAddr=true so we won't be able to remove the call - bail out.\n");
        return false;
    }

    GenTree* dstRefArg = call->gtArgs.GetUserArgByIndex(0)->GetNode();
    GenTree* lengthArg;
    GenTree* valueArg;

    // Fill<T>'s length is not in bytes, so we need to scale it depending on the signature
    unsigned lengthScale;

    if (call->IsSpecialIntrinsic(m_compiler, NI_System_SpanHelpers_Fill))
    {
        // void SpanHelpers::Fill<T>(ref T refData, nuint numElements, T value)
        //
        assert(call->gtArgs.CountUserArgs() == 3);
        lengthArg             = call->gtArgs.GetUserArgByIndex(1)->GetNode();
        CallArg* valueCallArg = call->gtArgs.GetUserArgByIndex(2);
        valueArg              = valueCallArg->GetNode();

        // Get that <T> from the signature
        lengthScale = genTypeSize(valueCallArg->GetSignatureType());
        // NOTE: structs and TYP_REF will be ignored by the "Value is not a constant" check
        // Some of those cases can be enabled in future, e.g. s
    }
    else if (call->IsHelperCall(CORINFO_HELP_MEMSET))
    {
        // void CORINFO_HELP_MEMSET(ref T refData, byte value, nuint numElements)
        //
        assert(call->gtArgs.CountUserArgs() == 3);
        lengthArg   = call->gtArgs.GetUserArgByIndex(2)->GetNode();
        valueArg    = call->gtArgs.GetUserArgByIndex(1)->GetNode();
        lengthScale = 1; // it's always in bytes
    }
    else
    {
        // void SpanHelpers::ClearWithoutReferences(ref byte b, nuint byteLength)
        //
        assert(call->IsSpecialIntrinsic(m_compiler, NI_System_SpanHelpers_ClearWithoutReferences));
        assert(call->gtArgs.CountUserArgs() == 2);

        // Simple zeroing
        lengthArg   = call->gtArgs.GetUserArgByIndex(1)->GetNode();
        valueArg    = m_compiler->gtNewZeroConNode(TYP_INT);
        lengthScale = 1; // it's always in bytes
    }

    if (!lengthArg->IsIntegralConst())
    {
        JITDUMP("Length is not a constant - bail out.\n");
        return false;
    }

    if (!valueArg->IsCnsIntOrI() || !valueArg->TypeIs(TYP_INT))
    {
        JITDUMP("Value is not a constant - bail out.\n");
        return false;
    }

    // If value is not zero, we can only unroll for single-byte values
    if (!valueArg->IsIntegralConst(0) && (lengthScale != 1))
    {
        JITDUMP("Value is not unroll-friendly - bail out.\n");
        return false;
    }

    // Convert lenCns to bytes
    ssize_t lenCns = lengthArg->AsIntCon()->IconValue();
    if (CheckedOps::MulOverflows((target_ssize_t)lenCns, (target_ssize_t)lengthScale, CheckedOps::Signed))
    {
        // lenCns overflows
        JITDUMP("lenCns * lengthScale overflows - bail out.\n")
        return false;
    }
    lenCns *= (ssize_t)lengthScale;

    // TODO-CQ: drop the whole thing in case of lenCns = 0
    if ((lenCns <= 0) || (lenCns > (ssize_t)m_compiler->getUnrollThreshold(Compiler::UnrollKind::Memset)))
    {
        JITDUMP("Size is either 0 or too big to unroll - bail out.\n")
        return false;
    }

    JITDUMP("Accepted for unrolling!\nOld tree:\n");
    DISPTREERANGE(BlockRange(), call);

    if (!valueArg->IsIntegralConst(0))
    {
        // Non-zero (byte) value, wrap value with GT_INIT_VAL
        GenTree* initVal = valueArg;
        valueArg         = m_compiler->gtNewOperNode(GT_INIT_VAL, TYP_INT, initVal);
        BlockRange().InsertAfter(initVal, valueArg);
    }

    GenTreeBlk* storeBlk  = m_compiler->gtNewStoreBlkNode(m_compiler->typGetBlkLayout((unsigned)lenCns), dstRefArg,
                                                          valueArg, GTF_IND_UNALIGNED);
    storeBlk->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

    // Insert/Remove trees into LIR
    BlockRange().InsertBefore(call, storeBlk);
    if (call->IsSpecialIntrinsic(m_compiler, NI_System_SpanHelpers_ClearWithoutReferences))
    {
        // Value didn't exist in LIR previously
        BlockRange().InsertBefore(storeBlk, valueArg);
    }

    // Remove the call and mark everything as unused ...
    BlockRange().Remove(call, true);
    // ... except the args we're going to re-use
    dstRefArg->ClearUnusedValue();
    valueArg->ClearUnusedValue();
    if (valueArg->OperIs(GT_INIT_VAL))
    {
        valueArg->gtGetOp1()->ClearUnusedValue();
    }

    JITDUMP("\nNew tree:\n");
    DISPTREERANGE(BlockRange(), storeBlk);
    *next = storeBlk;
    return true;
}

//------------------------------------------------------------------------
// LowerCallMemmove: Replace Buffer.Memmove(DST, SRC, CNS_SIZE) with a GT_STORE_BLK:
//    Do the same for CORINFO_HELP_MEMCPY(DST, SRC, CNS_SIZE)
//
//    *  STORE_BLK struct<CNS_SIZE> (copy) (Unroll)
//    +--*  LCL_VAR   byref  dst
//    \--*  IND       struct
//       \--*  LCL_VAR   byref  src
//
// Arguments:
//    tree - GenTreeCall node to replace with STORE_BLK
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::LowerCallMemmove(GenTreeCall* call, GenTree** next)
{
    JITDUMP("Considering Memmove [%06d] for unrolling.. ", m_compiler->dspTreeID(call))
    assert(call->IsHelperCall(CORINFO_HELP_MEMCPY) ||
           (m_compiler->lookupNamedIntrinsic(call->gtCallMethHnd) == NI_System_SpanHelpers_Memmove));

    assert(call->gtArgs.CountUserArgs() == 3);

    if (m_compiler->info.compHasNextCallRetAddr)
    {
        JITDUMP("compHasNextCallRetAddr=true so we won't be able to remove the call - bail out.\n")
        return false;
    }

    GenTree* lengthArg = call->gtArgs.GetUserArgByIndex(2)->GetNode();
    if (lengthArg->IsIntegralConst())
    {
        ssize_t cnsSize = lengthArg->AsIntCon()->IconValue();
        JITDUMP("Size=%zd.. ", cnsSize);
        // TODO-CQ: drop the whole thing in case of 0
        if ((cnsSize > 0) && (cnsSize <= (ssize_t)m_compiler->getUnrollThreshold(Compiler::UnrollKind::Memmove)))
        {
            JITDUMP("Accepted for unrolling!\nOld tree:\n")
            DISPTREE(call);

            GenTree* dstAddr = call->gtArgs.GetUserArgByIndex(0)->GetNode();
            GenTree* srcAddr = call->gtArgs.GetUserArgByIndex(1)->GetNode();
            assert(!dstAddr->isContained());
            assert(!srcAddr->isContained());

            // TODO-CQ: Try to create an addressing mode
            GenTreeIndir* srcBlk = m_compiler->gtNewIndir(TYP_STRUCT, srcAddr);
            srcBlk->SetContained();

            GenTreeBlk* storeBlk = new (m_compiler, GT_STORE_BLK)
                GenTreeBlk(GT_STORE_BLK, TYP_STRUCT, dstAddr, srcBlk, m_compiler->typGetBlkLayout((unsigned)cnsSize));
            storeBlk->gtFlags |= (GTF_IND_UNALIGNED | GTF_ASG | GTF_EXCEPT | GTF_GLOB_REF);

            // For simplicity, we use BlkOpKindUnrollMemmove even for CORINFO_HELP_MEMCPY.
            storeBlk->gtBlkOpKind = GenTreeBlk::BlkOpKindUnrollMemmove;

            BlockRange().InsertBefore(call, srcBlk);
            BlockRange().InsertBefore(call, storeBlk);
            BlockRange().Remove(lengthArg);
            BlockRange().Remove(call);

            // Remove all non-user args (e.g. r2r cell)
            for (CallArg& arg : call->gtArgs.Args())
            {
                if (arg.IsArgAddedLate())
                {
                    arg.GetNode()->SetUnusedValue();
                }
            }

            JITDUMP("\nNew tree:\n")
            DISPTREE(storeBlk);
            // We've just lowered srcBlk and storeBlk here and it's now what genCodeForMemmove expects.
            // So the next node to lower is whatever we have after the storeBlk.
            *next = storeBlk->gtNext;
            return true;
        }
        else
        {
            JITDUMP("Size is either 0 or too big to unroll.\n")
        }
    }
    else
    {
        JITDUMP("size is not a constant.\n")
    }
    return false;
}

//------------------------------------------------------------------------
// LowerCallMemcmp: Replace SpanHelpers.SequenceEqual)(left, right, CNS_SIZE)
//    with a series of merged comparisons (via GT_IND nodes)
//
// Arguments:
//    tree - GenTreeCall node to unroll as memcmp
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::LowerCallMemcmp(GenTreeCall* call, GenTree** next)
{
    JITDUMP("Considering Memcmp [%06d] for unrolling.. ", m_compiler->dspTreeID(call))
    assert(m_compiler->lookupNamedIntrinsic(call->gtCallMethHnd) == NI_System_SpanHelpers_SequenceEqual);
    assert(call->gtArgs.CountUserArgs() == 3);
    assert(TARGET_POINTER_SIZE == 8);

    if (!m_compiler->opts.OptimizationEnabled())
    {
        JITDUMP("Optimizations aren't allowed - bail out.\n")
        return false;
    }

    if (m_compiler->info.compHasNextCallRetAddr)
    {
        JITDUMP("compHasNextCallRetAddr=true so we won't be able to remove the call - bail out.\n")
        return false;
    }

    GenTree* lengthArg = call->gtArgs.GetUserArgByIndex(2)->GetNode();
    if (lengthArg->IsIntegralConst())
    {
        ssize_t cnsSize = lengthArg->AsIntCon()->IconValue();
        JITDUMP("Size=%zd.. ", cnsSize);
        // The case of 0 has been handled earlier with VN
        if (cnsSize > 0)
        {
            GenTree* lArg = call->gtArgs.GetUserArgByIndex(0)->GetNode();
            GenTree* rArg = call->gtArgs.GetUserArgByIndex(1)->GetNode();

            ssize_t MaxUnrollSize = 16;

#ifdef FEATURE_SIMD
#ifdef TARGET_XARCH
            if (m_compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512))
            {
                MaxUnrollSize = 128;
            }
            else if (m_compiler->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                // We need AVX2 for TYP_SIMD32 based op_Equality, fallback to Vector128 if only AVX is available
                MaxUnrollSize = 64;
            }
            else
#endif // TARGET_XARCH
            {
                MaxUnrollSize = 32;
            }
#endif // FEATURE_SIMD

            if (cnsSize <= MaxUnrollSize)
            {
                unsigned  loadWidth = 1 << BitOperations::Log2((unsigned)cnsSize);
                var_types loadType;
                if (loadWidth == 1)
                {
                    loadType = TYP_UBYTE;
                }
                else if (loadWidth == 2)
                {
                    loadType = TYP_USHORT;
                }
                else if (loadWidth == 4)
                {
                    loadType = TYP_INT;
                }
                else if ((loadWidth == 8) || (MaxUnrollSize == 16))
                {
                    loadWidth = 8;
                    loadType  = TYP_LONG;
                }
#ifdef FEATURE_SIMD
                else if ((loadWidth == 16) || (MaxUnrollSize == 32))
                {
                    loadWidth = 16;
                    loadType  = TYP_SIMD16;
                }
#ifdef TARGET_XARCH
                else if ((loadWidth == 32) || (MaxUnrollSize == 64))
                {
                    loadWidth = 32;
                    loadType  = TYP_SIMD32;
                }
                else if ((loadWidth == 64) || (MaxUnrollSize == 128))
                {
                    loadWidth = 64;
                    loadType  = TYP_SIMD64;
                }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
                else
                {
                    unreached();
                }
                var_types actualLoadType = genActualType(loadType);

                GenTree* result = nullptr;

                auto newBinaryOp = [](Compiler* m_compiler, genTreeOps oper, var_types type, GenTree* op1,
                                      GenTree* op2) -> GenTree* {
#ifdef FEATURE_SIMD
                    if (varTypeIsSIMD(op1))
                    {
                        if (GenTree::OperIsCmpCompare(oper))
                        {
                            assert(type == TYP_INT);
                            return m_compiler->gtNewSimdCmpOpAllNode(oper, TYP_INT, op1, op2, TYP_U_IMPL,
                                                                     genTypeSize(op1));
                        }
                        return m_compiler->gtNewSimdBinOpNode(oper, op1->TypeGet(), op1, op2, TYP_U_IMPL,
                                                              genTypeSize(op1));
                    }
#endif
                    return m_compiler->gtNewOperNode(oper, type, op1, op2);
                };

                // loadWidth == cnsSize means a single load is enough for both args
                if (loadWidth == (unsigned)cnsSize)
                {
                    // We're going to emit something like the following:
                    //
                    // bool result = *(int*)leftArg == *(int*)rightArg
                    //
                    // ^ in the given example we unroll for length=4
                    //
                    GenTree* lIndir = m_compiler->gtNewIndir(loadType, lArg);
                    GenTree* rIndir = m_compiler->gtNewIndir(loadType, rArg);
                    result          = newBinaryOp(m_compiler, GT_EQ, TYP_INT, lIndir, rIndir);

                    BlockRange().InsertBefore(call, lIndir, rIndir, result);
                    *next = lIndir;
                }
                else
                {
                    // First, make both args multi-use:
                    LIR::Use lArgUse;
                    LIR::Use rArgUse;
                    bool     lFoundUse = BlockRange().TryGetUse(lArg, &lArgUse);
                    bool     rFoundUse = BlockRange().TryGetUse(rArg, &rArgUse);
                    assert(lFoundUse && rFoundUse);
                    GenTree* lArgClone =
                        m_compiler->gtNewLclvNode(lArgUse.ReplaceWithLclVar(m_compiler), genActualType(lArg));
                    GenTree* rArgClone =
                        m_compiler->gtNewLclvNode(rArgUse.ReplaceWithLclVar(m_compiler), genActualType(rArg));
                    BlockRange().InsertBefore(call, lArgClone, rArgClone);

                    *next = lArgClone;

                    GenTree* l1Indir   = m_compiler->gtNewIndir(loadType, lArgUse.Def());
                    GenTree* r1Indir   = m_compiler->gtNewIndir(loadType, rArgUse.Def());
                    GenTree* l2Offs    = m_compiler->gtNewIconNode(cnsSize - loadWidth, TYP_I_IMPL);
                    GenTree* l2AddOffs = newBinaryOp(m_compiler, GT_ADD, lArg->TypeGet(), lArgClone, l2Offs);
                    GenTree* l2Indir   = m_compiler->gtNewIndir(loadType, l2AddOffs);
                    GenTree* r2Offs    = m_compiler->gtNewIconNode(cnsSize - loadWidth, TYP_I_IMPL);
                    GenTree* r2AddOffs = newBinaryOp(m_compiler, GT_ADD, rArg->TypeGet(), rArgClone, r2Offs);
                    GenTree* r2Indir   = m_compiler->gtNewIndir(loadType, r2AddOffs);

                    BlockRange().InsertAfter(rArgClone, l1Indir, l2Offs, l2AddOffs, l2Indir);
                    BlockRange().InsertAfter(l2Indir, r1Indir, r2Offs, r2AddOffs, r2Indir);

#ifdef TARGET_ARM64
                    if (!varTypeIsSIMD(loadType))
                    {
                        // ARM64 will get efficient ccmp codegen if we emit the normal thing:
                        //
                        // bool result = (*(int*)leftArg == *(int)rightArg) & (*(int*)(leftArg + 1) == *(int*)(rightArg
                        // +
                        // 1))

                        GenTree* eq1 = newBinaryOp(m_compiler, GT_EQ, TYP_INT, l1Indir, r1Indir);
                        GenTree* eq2 = newBinaryOp(m_compiler, GT_EQ, TYP_INT, l2Indir, r2Indir);
                        result       = newBinaryOp(m_compiler, GT_AND, TYP_INT, eq1, eq2);

                        BlockRange().InsertAfter(r2Indir, eq1, eq2, result);
                    }
#endif

                    if (result == nullptr)
                    {
                        // We're going to emit something like the following:
                        //
                        // bool result = ((*(int*)leftArg ^ *(int*)rightArg) |
                        //                (*(int*)(leftArg + 1) ^ *((int*)(rightArg + 1)))) == 0;
                        //
                        // ^ in the given example we unroll for length=5
                        //
                        // In IR:
                        //
                        // *  EQ        int
                        // +--*  OR        int
                        // |  +--*  XOR       int
                        // |  |  +--*  IND       int
                        // |  |  |  \--*  LCL_VAR   byref  V1
                        // |  |  \--*  IND       int
                        // |  |     \--*  LCL_VAR   byref  V2
                        // |  \--*  XOR       int
                        // |     +--*  IND       int
                        // |     |  \--*  ADD       byref
                        // |     |     +--*  LCL_VAR   byref  V1
                        // |     |     \--*  CNS_INT   int    1
                        // |     \--*  IND       int
                        // |        \--*  ADD       byref
                        // |           +--*  LCL_VAR   byref  V2
                        // |           \--*  CNS_INT   int    1
                        // \--*  CNS_INT   int    0
                        //
                        // TODO-CQ: Do this as a general optimization similar to TryLowerAndOrToCCMP.

                        GenTree* lXor     = newBinaryOp(m_compiler, GT_XOR, actualLoadType, l1Indir, r1Indir);
                        GenTree* rXor     = newBinaryOp(m_compiler, GT_XOR, actualLoadType, l2Indir, r2Indir);
                        GenTree* resultOr = newBinaryOp(m_compiler, GT_OR, actualLoadType, lXor, rXor);
                        GenTree* zeroCns  = m_compiler->gtNewZeroConNode(actualLoadType);
                        result            = newBinaryOp(m_compiler, GT_EQ, TYP_INT, resultOr, zeroCns);

                        BlockRange().InsertAfter(r2Indir, lXor, rXor, resultOr, zeroCns);
                        BlockRange().InsertAfter(zeroCns, result);
                    }
                }

                JITDUMP("\nUnrolled to:\n");
                DISPTREE(result);

                LIR::Use use;
                if (BlockRange().TryGetUse(call, &use))
                {
                    use.ReplaceWith(result);
                }
                else
                {
                    result->SetUnusedValue();
                }
                BlockRange().Remove(lengthArg);
                BlockRange().Remove(call);

                // Remove all non-user args (e.g. r2r cell)
                for (CallArg& arg : call->gtArgs.Args())
                {
                    if (!arg.IsUserArg())
                    {
                        arg.GetNode()->SetUnusedValue();
                    }
                }
                return true;
            }
        }
        else
        {
            JITDUMP("Size is either 0 or too big to unroll.\n")
        }
    }
    else
    {
        JITDUMP("size is not a constant.\n")
    }
    return false;
}

// do lowering steps for a call
// this includes:
//   - adding the placement nodes (either stack or register variety) for arguments
//   - lowering the expression that calculates the target address
//   - adding nodes for other operations that occur after the call sequence starts and before
//        control transfer occurs (profiling and tail call helpers, pinvoke incantations)
//
GenTree* Lowering::LowerCall(GenTree* node)
{
    GenTreeCall* call = node->AsCall();

    JITDUMP("lowering call (before):\n");
    DISPTREERANGE(BlockRange(), call);
    JITDUMP("\n");

    // All runtime lookups are expected to be expanded in fgExpandRuntimeLookups
    assert(!call->IsRuntimeLookupHelperCall(m_compiler) ||
           (call->gtCallDebugFlags & GTF_CALL_MD_RUNTIME_LOOKUP_EXPANDED) != 0);

    // Also, always expand static cctor helper for NativeAOT, see
    // https://github.com/dotnet/runtime/issues/68278#issuecomment-1543322819
    if (m_compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI) && m_compiler->IsStaticHelperEligibleForExpansion(call))
    {
        assert(call->gtInitClsHnd == nullptr);
    }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    GenTree* nextNode = nullptr;
    if (call->IsSpecialIntrinsic())
    {
        switch (m_compiler->lookupNamedIntrinsic(call->gtCallMethHnd))
        {
            case NI_System_SpanHelpers_Memmove:
                if (LowerCallMemmove(call, &nextNode))
                {
                    return nextNode;
                }
                break;

            case NI_System_SpanHelpers_SequenceEqual:
                if (LowerCallMemcmp(call, &nextNode))
                {
                    return nextNode;
                }
                break;

            case NI_System_SpanHelpers_Fill:
            case NI_System_SpanHelpers_ClearWithoutReferences:
                if (LowerCallMemset(call, &nextNode))
                {
                    return nextNode;
                }
                break;

            default:
                break;
        }
    }

    // Try to lower CORINFO_HELP_MEMCPY to unrollable STORE_BLK
    if (call->IsHelperCall(CORINFO_HELP_MEMCPY) && LowerCallMemmove(call, &nextNode))
    {
        return nextNode;
    }

    // Try to lower CORINFO_HELP_MEMSET to unrollable STORE_BLK
    if (call->IsHelperCall(CORINFO_HELP_MEMSET) && LowerCallMemset(call, &nextNode))
    {
        return nextNode;
    }
#endif

    call->gtDirectCallAddress = nullptr; // Clear out any stale data from the union.
    call->ClearOtherRegs();

#if HAS_FIXED_REGISTER_SET
    if ((call->gtCallType == CT_INDIRECT) && m_compiler->opts.Tier0OptimizationEnabled())
    {
        OptimizeCallIndirectTargetEvaluation(call);
    }
#endif

    LowerArgsForCall(call);

    // note that everything generated from this point might run AFTER the outgoing args are placed
    GenTree* controlExpr          = nullptr;
    bool     callWasExpandedEarly = false;

    // for x86, this is where we record ESP for checking later to make sure stack is balanced

    // Check for Delegate.Invoke(). If so, we inline it. We get the
    // target-object and target-function from the delegate-object, and do
    // an indirect call.
    if (call->IsDelegateInvoke())
    {
        controlExpr = LowerDelegateInvoke(call);
    }
    else
    {
        //  Virtual and interface calls
        switch (call->gtFlags & GTF_CALL_VIRT_KIND_MASK)
        {
            case GTF_CALL_VIRT_STUB:
                controlExpr = LowerVirtualStubCall(call);
                break;

            case GTF_CALL_VIRT_VTABLE:
                assert(call->IsVirtualVtable());
                if (!call->IsExpandedEarly())
                {
                    assert(call->gtControlExpr == nullptr);
                    controlExpr = LowerVirtualVtableCall(call);
                }
                else
                {
                    callWasExpandedEarly = true;
                    controlExpr          = call->gtControlExpr;
                }
                break;

            case GTF_CALL_NONVIRT:
                if (call->IsUnmanaged())
                {
                    controlExpr = LowerNonvirtPinvokeCall(call);
                }
                else if (call->gtCallType != CT_INDIRECT)
                {
                    controlExpr = LowerDirectCall(call);
                }
                break;

            default:
                noway_assert(!"strange call type");
                break;
        }
    }

    // We shouldn't be trying to assign a new control target to indirect calls.
    assert((call->gtCallType != CT_INDIRECT) || (controlExpr == nullptr));

    if (call->IsTailCallViaJitHelper())
    {
        // Either controlExpr or "gtControlExpr" must contain real call target.
        if (controlExpr != nullptr)
        {
            // Link controlExpr into the IR before the call.
            // The callTarget tree needs to be sequenced.
            LIR::Range callTargetRange = LIR::SeqTree(m_compiler, controlExpr);
            ContainCheckRange(callTargetRange);
            BlockRange().InsertBefore(call, std::move(callTargetRange));
        }
        else
        {
            // "gtControlExpr" is already sequenced and just before the call.
            assert(call->gtCallType == CT_INDIRECT);
            assert(call->gtControlExpr != nullptr);
            controlExpr = call->gtControlExpr;
        }

        // LowerTailCallViaJitHelper will turn the control expr
        // into an arg and produce a new control expr for the helper call.
        controlExpr = LowerTailCallViaJitHelper(call, controlExpr);
    }

    // Check if we need to thread a newly created controlExpr into the LIR
    //
    if ((controlExpr != nullptr) && !callWasExpandedEarly)
    {
        LIR::Range controlExprRange = LIR::SeqTree(m_compiler, controlExpr);

        JITDUMP("results of lowering call:\n");
        DISPRANGE(controlExprRange);

        ContainCheckRange(controlExprRange);

        BlockRange().InsertBefore(call, std::move(controlExprRange));
        call->gtControlExpr = controlExpr;

#ifdef TARGET_RISCV64
        // If controlExpr is a constant, we should contain it inside the call so that we can move the lower 12-bits of
        // the value to call instruction's (JALR) offset.
        if (controlExpr->IsCnsIntOrI() && !controlExpr->AsIntCon()->ImmedValNeedsReloc(m_compiler) &&
            !call->IsFastTailCall())
        {
            MakeSrcContained(call, controlExpr);
        }
#endif // TARGET_RISCV64
    }

    if (m_compiler->opts.IsCFGEnabled())
    {
        LowerCFGCall(call);
    }

    if (call->IsFastTailCall())
    {
        // Lower fast tail call can introduce new temps to set up args correctly for Callee.
        // This involves patching LCL_VAR and LCL_VAR_ADDR nodes holding Caller stack args
        // and replacing them with a new temp. Control expr also can contain nodes that need
        // to be patched.
        // Therefore lower fast tail call must be done after controlExpr is inserted into LIR.
        // There is one side effect which is flipping the order of PME and control expression
        // since LowerFastTailCall calls InsertPInvokeMethodEpilog.
        LowerFastTailCall(call);
    }
    else
    {
        if (!call->IsHelperCall(CORINFO_HELP_VALIDATE_INDIRECT_CALL))
        {
            RequireOutgoingArgSpace(call, call->gtArgs.OutgoingArgsStackSize());
        }
    }

#ifdef TARGET_WASM
    // For any type of managed call, if we have portable entry points enabled, we need to lower
    // the call according to the portable entrypoint abi
    if (!call->IsUnmanaged() && m_compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PORTABLE_ENTRY_POINTS))
    {
        LowerPEPCall(call);
    }
#endif // TARGET_WASM

    if (varTypeIsStruct(call))
    {
        LowerCallStruct(call);
    }

    ContainCheckCallOperands(call);
    JITDUMP("lowering call (after):\n");
    DISPTREERANGE(BlockRange(), call);
    JITDUMP("\n");
    return nullptr;
}

// Inserts profiler hook, GT_PROF_HOOK for a tail call node.
//
// AMD64:
// We need to insert this after all nested calls, but before all the arguments to this call have been set up.
// To do this, we look for the first GT_PUTARG_STK or GT_PUTARG_REG, and insert the hook immediately before
// that. If there are no args, then it should be inserted before the call node.
//
// For example:
//              *  stmtExpr  void  (top level) (IL 0x000...0x010)
// arg0 SETUP   |  /--*  argPlace  ref    REG NA $c5
// this in rcx  |  |     /--*  argPlace  ref    REG NA $c1
//              |  |     |  /--*  call      ref    System.Globalization.CultureInfo.get_InvariantCulture $c2
// arg1 SETUP   |  |     +--*  st.lclVar ref    V02 tmp1          REG NA $c2
//              |  |     |  /--*  lclVar    ref    V02 tmp1         u : 2 (last use) REG NA $c2
// arg1 in rdx  |  |     +--*  putarg_reg ref    REG NA
//              |  |     |  /--*  lclVar    ref    V00 arg0         u : 2 (last use) REG NA $80
// this in rcx  |  |     +--*  putarg_reg ref    REG NA
//              |  |  /--*  call nullcheck ref    System.String.ToLower $c5
//              |  |  {  *  stmtExpr  void  (embedded)(IL 0x000... ? ? ? )
//              |  |  {  \--*  prof_hook void   REG NA
// arg0 in rcx  |  +--*  putarg_reg ref    REG NA
// control expr |  +--*  const(h)  long   0x7ffe8e910e98 ftn REG NA
//              \--*  call      void   System.Runtime.Remoting.Identity.RemoveAppNameOrAppGuidIfNecessary $VN.Void
//
// In this case, the GT_PUTARG_REG src is a nested call. We need to put the instructions after that call
// (as shown). We assume that of all the GT_PUTARG_*, only the first one can have a nested call.
//
// X86:
// Insert the profiler hook immediately before the call. The profiler hook will preserve
// all argument registers (ECX, EDX), but nothing else.
//
// Params:
//    callNode        - tail call node
//    insertionPoint  - if non-null, insert the profiler hook before this point.
//                      If null, insert the profiler hook before args are setup
//                      but after all arg side effects are computed.
//
void Lowering::InsertProfTailCallHook(GenTreeCall* call, GenTree* insertionPoint)
{
    assert(call->IsTailCall());
    assert(m_compiler->compIsProfilerHookNeeded());

#if defined(TARGET_X86)

    if (insertionPoint == nullptr)
    {
        insertionPoint = call;
    }

#else // !defined(TARGET_X86)

    if (insertionPoint == nullptr)
    {
        insertionPoint = FindEarliestPutArg(call);

        if (insertionPoint == nullptr)
        {
            insertionPoint = call;
        }
    }

#endif // !defined(TARGET_X86)

    assert(insertionPoint != nullptr);
    JITDUMP("Inserting profiler tail call before [%06u]\n", m_compiler->dspTreeID(insertionPoint));

    GenTree* profHookNode = new (m_compiler, GT_PROF_HOOK) GenTree(GT_PROF_HOOK, TYP_VOID);
    BlockRange().InsertBefore(insertionPoint, profHookNode);
}

//------------------------------------------------------------------------
// FindEarliestPutArg: Find the earliest direct PUTARG operand of a call node in
// linear order.
//
// Arguments:
//    call - the call
//
// Returns:
//    A PUTARG_* node that is the earliest of the call, or nullptr if the call
//    has no arguments.
//
GenTree* Lowering::FindEarliestPutArg(GenTreeCall* call)
{
    size_t numMarkedNodes = MarkCallPutArgAndFieldListNodes(call);

    if (numMarkedNodes <= 0)
    {
        return nullptr;
    }

    GenTree* node = call;
    do
    {
        node = node->gtPrev;

        assert((node != nullptr) && "Reached beginning of basic block while looking for marked nodes");

        if ((node->gtLIRFlags & LIR::Flags::Mark) != 0)
        {
            node->gtLIRFlags &= ~LIR::Flags::Mark;
            numMarkedNodes--;
        }
    } while (numMarkedNodes > 0);

    assert(node->OperIsPutArg());
    return node;
}

//------------------------------------------------------------------------
// MarkCallPutArgNodes: Mark all operand FIELD_LIST and PUTARG nodes
// corresponding to a call.
//
// Arguments:
//   call - the call
//
// Returns:
//   The number of nodes marked.
//
// Remarks:
//   FIELD_LIST operands are marked too, and their PUTARG operands are in turn
//   marked as well.
//
size_t Lowering::MarkCallPutArgAndFieldListNodes(GenTreeCall* call)
{
    size_t numMarkedNodes = 0;
    for (CallArg& arg : call->gtArgs.Args())
    {
        if (arg.GetEarlyNode() != nullptr)
        {
            numMarkedNodes += MarkPutArgAndFieldListNodes(arg.GetEarlyNode());
        }

        if (arg.GetLateNode() != nullptr)
        {
            numMarkedNodes += MarkPutArgAndFieldListNodes(arg.GetLateNode());
        }
    }

    return numMarkedNodes;
}

//------------------------------------------------------------------------
// MarkPutArgAndFieldListNodes: Mark all operand FIELD_LIST and PUTARG nodes
// with a LIR mark.
//
// Arguments:
//   node - the node (either a FIELD_LIST or PUTARG operand)
//
// Returns:
//   The number of marks added.
//
// Remarks:
//   FIELD_LIST operands are marked too, and their PUTARG operands are in turn
//   marked as well.
//
size_t Lowering::MarkPutArgAndFieldListNodes(GenTree* node)
{
#if !HAS_FIXED_REGISTER_SET
    if (!node->OperIsPutArg() && !node->OperIsFieldList())
        return 0;
#endif

    assert(node->OperIsPutArg() || node->OperIsFieldList());

    assert((node->gtLIRFlags & LIR::Flags::Mark) == 0);
    node->gtLIRFlags |= LIR::Flags::Mark;

    size_t result = 1;
    if (node->OperIsFieldList())
    {
        for (GenTreeFieldList::Use& operand : node->AsFieldList()->Uses())
        {
            result += MarkPutArgAndFieldListNodes(operand.GetNode());
        }
    }

    return result;
}

//------------------------------------------------------------------------
// LowerFastTailCall: Lower a call node dispatched as a fast tailcall (epilog +
// jmp).
//
// Arguments:
//    call - the call node that is being dispatched as a fast tailcall.
//
// Assumptions:
//    call must be non-null.
//
// Notes:
//     For fast tail calls it is necessary to set up stack args in the incoming
//     arg stack space area. When args passed also come from this area we may
//     run into problems because we may end up overwriting the stack slot before
//     using it. For example, for foo(a, b) { return bar(b, a); }, if a and b
//     are on incoming arg stack space in foo they need to be swapped in this
//     area for the call to bar. This function detects this situation and
//     introduces a temp when an outgoing argument would overwrite a later-used
//     incoming argument.
//
//     This function also handles inserting necessary profiler hooks and pinvoke
//     method epilogs in case there are inlined pinvokes.
void Lowering::LowerFastTailCall(GenTreeCall* call)
{
#if FEATURE_FASTTAILCALL
    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((m_compiler->info.compFlags & CORINFO_FLG_SYNCH) == 0); // tail calls from synchronized methods
    assert(!m_compiler->opts.IsReversePInvoke());                  // tail calls reverse pinvoke
    assert(!call->IsUnmanaged());                                  // tail calls to unamanaged methods
    assert(!m_compiler->compLocallocUsed);                         // tail call from methods that also do localloc

    // We expect to see a call that meets the following conditions
    assert(call->IsFastTailCall());

    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (m_compiler->compMethodRequiresPInvokeFrame())
    {
        InsertPInvokeMethodEpilog(m_compiler->compCurBB DEBUGARG(call));
    }

    // Args for tail call are setup in incoming arg area.  The gc-ness of args of
    // caller and callee (which being tail called) may not match.  Therefore, everything
    // from arg setup until the epilog need to be non-interruptible by GC.  This is
    // achieved by inserting GT_START_NONGC before the very first GT_PUTARG_STK node
    // of call is setup.  Note that once a stack arg is setup, it cannot have nested
    // calls subsequently in execution order to setup other args, because the nested
    // call could over-write the stack arg that is setup earlier.
    ArrayStack<GenTree*> putargs(m_compiler->getAllocator(CMK_ArrayStack));

    for (CallArg& arg : call->gtArgs.Args())
    {
        if (arg.GetNode()->OperIs(GT_PUTARG_STK))
        {
            putargs.Push(arg.GetNode());
        }
    }

    GenTree* startNonGCNode = nullptr;
    if (!putargs.Empty())
    {
        GenTree* firstPutargStk   = putargs.Bottom(0);
        GenTree* firstPutargStkOp = FirstOperand(firstPutargStk);
        for (int i = 1; i < putargs.Height(); i++)
        {
            firstPutargStk   = LIR::FirstNode(firstPutargStk, putargs.Bottom(i));
            firstPutargStkOp = LIR::FirstNode(firstPutargStkOp, FirstOperand(putargs.Bottom(i)));
        }
        // Since this is a fast tailcall each PUTARG_STK will place the argument in the
        // _incoming_ arg space area. This will effectively overwrite our already existing
        // incoming args that live in that area. If we have later uses of those args, this
        // is a problem. We introduce a defensive copy into a temp here of those args that
        // potentially may cause problems.
        for (GenTree* const put : putargs.BottomUpOrder())
        {
            GenTreePutArgStk* putArgStk = put->AsPutArgStk();

            unsigned int overwrittenStart = putArgStk->getArgOffset();
            unsigned int overwrittenEnd   = overwrittenStart + putArgStk->GetStackByteSize();

            for (unsigned callerArgLclNum = 0; callerArgLclNum < m_compiler->info.compArgsCount; callerArgLclNum++)
            {
                LclVarDsc* callerArgDsc = m_compiler->lvaGetDesc(callerArgLclNum);

                if (callerArgDsc->lvIsRegArg)
                {
                    continue;
                }

                const ABIPassingInformation& abiInfo = m_compiler->lvaGetParameterABIInfo(callerArgLclNum);
                assert(abiInfo.HasExactlyOneStackSegment());
                const ABIPassingSegment& seg = abiInfo.Segment(0);

                unsigned argStart = seg.GetStackOffset();
                unsigned argEnd   = argStart + seg.GetStackSize();

                // If ranges do not overlap then this PUTARG_STK will not mess up the arg.
                if ((overwrittenEnd <= argStart) || (overwrittenStart >= argEnd))
                {
                    continue;
                }

                JITDUMP(
                    "PUTARG_STK [%06u] overwrites [%06u..%06u); parameter V%03u lives in [%06u..%06u); may need defensive copies\n",
                    Compiler::dspTreeID(put), overwrittenStart, overwrittenEnd, callerArgLclNum, argStart, argEnd);

                // Codegen cannot handle a partially overlapping copy. For
                // example, if we have
                // bar(S16 stack, S32 stack2)
                // foo(S32 stack, S32 stack2) { bar(..., stack) }
                // then we may end up having to move 'stack' in foo 16 bytes
                // ahead. It is possible that this PUTARG_STK is the only use,
                // in which case we will need to introduce a temp, so look for
                // uses starting from it. Note that we assume that in-place
                // copies are ok provided the source is a scalar value.
                GenTree* lookForUsesFrom = put->gtNext;
                if ((overwrittenStart != argStart) || put->gtGetOp1()->OperIsFieldList())
                {
                    JITDUMP("Non-atomic copy may be self-interfering. Expanding search...\n");
                    lookForUsesFrom = firstPutargStkOp;
                }

                RehomeArgForFastTailCall(callerArgLclNum, firstPutargStkOp, lookForUsesFrom, call);
                // The above call can introduce temps and invalidate the pointer.
                callerArgDsc = m_compiler->lvaGetDesc(callerArgLclNum);

                // For promoted locals we have more work to do as its fields could also have been invalidated.
                if (!callerArgDsc->lvPromoted)
                {
                    continue;
                }

                unsigned int fieldsFirst = callerArgDsc->lvFieldLclStart;
                unsigned int fieldsEnd   = fieldsFirst + callerArgDsc->lvFieldCnt;
                for (unsigned int j = fieldsFirst; j < fieldsEnd; j++)
                {
                    RehomeArgForFastTailCall(j, firstPutargStkOp, lookForUsesFrom, call);
                }
            }
        }

        // Now insert GT_START_NONGC node before we evaluate the first PUTARG_STK.
        // Note that if there are no args to be setup on stack, no need to
        // insert GT_START_NONGC node.
        startNonGCNode = new (m_compiler, GT_START_NONGC) GenTree(GT_START_NONGC, TYP_VOID);
        BlockRange().InsertBefore(firstPutargStk, startNonGCNode);

        // Gc-interruptability in the following case:
        //     foo(a, b, c, d, e) { bar(a, b, c, d, e); }
        //     bar(a, b, c, d, e) { foo(a, b, d, d, e); }
        //
        // Since the instruction group starting from the instruction that sets up first
        // stack arg to the end of the tail call is marked as non-gc interruptible,
        // this will form a non-interruptible tight loop causing gc-starvation. To fix
        // this we insert GT_NO_OP as embedded stmt before GT_START_NONGC, if the method
        // has a single basic block and is not a GC-safe point.  The presence of a single
        // nop outside non-gc interruptible region will prevent gc starvation.
        if ((m_compiler->fgBBcount == 1) && !m_compiler->compCurBB->HasFlag(BBF_GC_SAFE_POINT))
        {
            assert(m_compiler->fgFirstBB == m_compiler->compCurBB);
            GenTree* noOp = new (m_compiler, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
            BlockRange().InsertBefore(startNonGCNode, noOp);
        }
    }

    // Insert GT_PROF_HOOK node to emit profiler tail call hook. This should be
    // inserted before the args are setup but after the side effects of args are
    // computed. That is, GT_PROF_HOOK node needs to be inserted before GT_START_NONGC
    // node if one exists.
    if (m_compiler->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, startNonGCNode);
    }

#else // !FEATURE_FASTTAILCALL

    // Platform does not implement fast tail call mechanism. This cannot be
    // reached because we always choose to do a tailcall via helper on those
    // platforms (or no tailcall at all).
    unreached();
#endif
}

//------------------------------------------------------------------------
// FirstOperand:
//   Find the earliest operand of a node.
//
// Arguments:
//   node - The node
//
// Returns:
//   The earliest evaluated operand.
//
GenTree* Lowering::FirstOperand(GenTree* node)
{
    struct Helper
    {
        GenTree* Result = nullptr;

        void Visit(GenTree* node)
        {
            node->VisitOperands([=](GenTree* op) {
                Result = Result == nullptr ? op : LIR::FirstNode(Result, op);

                if (op->isContained())
                {
                    Visit(op);
                }

                return GenTree::VisitResult::Continue;
            });
        }
    };

    Helper helper;
    helper.Visit(node);
    return helper.Result;
}

//------------------------------------------------------------------------
// RehomeArgForFastTailCall: Introduce temps for args that may be overwritten
// during fast tailcall sequence.
//
// Arguments:
//    lclNum - the lcl num of the arg that will be overwritten.
//    insertTempBefore - the node at which to copy the arg into a temp.
//    lookForUsesStart - the node where to start scanning and replacing uses of
//                       the arg specified by lclNum.
//    callNode - the call node that is being dispatched as a fast tailcall.
//
// Assumptions:
//    all args must be non-null.
//
// Notes:
//     This function scans for uses of the arg specified by lclNum starting
//     from the lookForUsesStart node. If it finds any uses it introduces a temp
//     for this argument and updates uses to use this instead. In the situation
//     where it introduces a temp it can thus invalidate pointers to other
//     locals.
//
void Lowering::RehomeArgForFastTailCall(unsigned int lclNum,
                                        GenTree*     insertTempBefore,
                                        GenTree*     lookForUsesStart,
                                        GenTreeCall* callNode)
{
    unsigned int tmpLclNum = BAD_VAR_NUM;
    for (GenTree* treeNode = lookForUsesStart; treeNode != callNode; treeNode = treeNode->gtNext)
    {
        if (!treeNode->OperIsLocal() && !treeNode->OperIs(GT_LCL_ADDR))
        {
            continue;
        }

        GenTreeLclVarCommon* lcl = treeNode->AsLclVarCommon();

        if (lcl->GetLclNum() != lclNum)
        {
            continue;
        }

        // Create tmp and use it in place of callerArgDsc
        if (tmpLclNum == BAD_VAR_NUM)
        {
            tmpLclNum =
                m_compiler->lvaGrabTemp(true DEBUGARG("Fast tail call lowering is creating a new local variable"));

            LclVarDsc* callerArgDsc                = m_compiler->lvaGetDesc(lclNum);
            var_types  tmpTyp                      = genActualType(callerArgDsc->TypeGet());
            m_compiler->lvaTable[tmpLclNum].lvType = tmpTyp;
            // TODO-CQ: I don't see why we should copy doNotEnreg.
            m_compiler->lvaTable[tmpLclNum].lvDoNotEnregister = callerArgDsc->lvDoNotEnregister;
#ifdef DEBUG
            m_compiler->lvaTable[tmpLclNum].SetDoNotEnregReason(callerArgDsc->GetDoNotEnregReason());
#endif // DEBUG

            GenTree* value;
#ifdef TARGET_ARM
            if (tmpTyp == TYP_LONG)
            {
                GenTree* loResult = m_compiler->gtNewLclFldNode(lclNum, TYP_INT, 0);
                GenTree* hiResult = m_compiler->gtNewLclFldNode(lclNum, TYP_INT, 4);
                value             = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
            }
            else
#endif // TARGET_ARM
            {
                value = m_compiler->gtNewLclvNode(lclNum, tmpTyp);
            }

            if (tmpTyp == TYP_STRUCT)
            {
                m_compiler->lvaSetStruct(tmpLclNum, m_compiler->lvaGetDesc(lclNum)->GetLayout(), false);
            }
            GenTreeLclVar* storeLclVar = m_compiler->gtNewStoreLclVarNode(tmpLclNum, value);
            BlockRange().InsertBefore(insertTempBefore, LIR::SeqTree(m_compiler, storeLclVar));
            ContainCheckRange(value, storeLclVar);
            LowerNode(storeLclVar);
        }

        lcl->SetLclNum(tmpLclNum);
    }
}

//------------------------------------------------------------------------
// LowerTailCallViaJitHelper: lower a call via the tailcall JIT helper. Morph
// has already inserted tailcall helper special arguments. This function inserts
// actual data for some placeholders. This function is only used on Windows x86.
//
// Lower
//      tail.call(<function args>, int numberOfOldStackArgs, int dummyNumberOfNewStackArgs, int flags, void* dummyArg)
// as
//      JIT_TailCall(<function args>, int numberOfOldStackArgsWords, int numberOfNewStackArgsWords, int flags, void*
//      callTarget)
// Note that the special arguments are on the stack, whereas the function arguments follow the normal convention.
//
// Also inserts PInvoke method epilog if required.
//
// Arguments:
//    call         -  The call node
//    callTarget   -  The real call target. This is used to replace the dummyArg during lowering.
//
// Return Value:
//    Returns control expression tree for making a call to helper Jit_TailCall.
//
GenTree* Lowering::LowerTailCallViaJitHelper(GenTreeCall* call, GenTree* callTarget)
{
    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((m_compiler->info.compFlags & CORINFO_FLG_SYNCH) == 0); // tail calls from synchronized methods
    assert(!call->IsUnmanaged());                                  // tail calls to unamanaged methods
    assert(!m_compiler->compLocallocUsed);                         // tail call from methods that also do localloc

    // We expect to see a call that meets the following conditions
    assert(call->IsTailCallViaJitHelper());
    assert(callTarget != nullptr);

    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (m_compiler->compMethodRequiresPInvokeFrame())
    {
        InsertPInvokeMethodEpilog(m_compiler->compCurBB DEBUGARG(call));
    }

    // Verify the special args are what we expect, and replace the dummy args with real values.
    // We need to figure out the size of the outgoing stack arguments, not including the special args.
    // The number of 4-byte words is passed to the helper for the incoming and outgoing argument sizes.
    // This number is exactly the next slot number in the call's argument info struct.
    unsigned  nNewStkArgsBytes = call->gtArgs.OutgoingArgsStackSize();
    const int wordSize         = 4;
    unsigned  nNewStkArgsWords = nNewStkArgsBytes / wordSize;
    assert(nNewStkArgsWords >= 4); // There must be at least the four special stack args.
    nNewStkArgsWords -= 4;

    unsigned numArgs = call->gtArgs.CountArgs();

    // arg 0 == callTarget.
    CallArg* argEntry = call->gtArgs.GetArgByIndex(numArgs - 1);
    assert(argEntry != nullptr);
    GenTree* arg0 = argEntry->GetEarlyNode()->AsPutArgStk()->gtGetOp1();

    bool               isClosed;
    LIR::ReadOnlyRange secondArgRange = BlockRange().GetTreeRange(arg0, &isClosed);
    assert(isClosed);
    BlockRange().Remove(std::move(secondArgRange));

    argEntry->GetEarlyNode()->AsPutArgStk()->gtOp1 = callTarget;

    // arg 1 == flags
    argEntry = call->gtArgs.GetArgByIndex(numArgs - 2);
    assert(argEntry != nullptr);
    GenTree* arg1 = argEntry->GetEarlyNode()->AsPutArgStk()->gtGetOp1();
    assert(arg1->OperIs(GT_CNS_INT));

    ssize_t tailCallHelperFlags = 1 |                                  // always restore EDI,ESI,EBX
                                  (call->IsVirtualStub() ? 0x2 : 0x0); // Stub dispatch flag
    arg1->AsIntCon()->SetIconValue(tailCallHelperFlags);

    // arg 2 == numberOfNewStackArgsWords
    argEntry = call->gtArgs.GetArgByIndex(numArgs - 3);
    assert(argEntry != nullptr);
    GenTree* arg2 = argEntry->GetEarlyNode()->AsPutArgStk()->gtGetOp1();
    assert(arg2->OperIs(GT_CNS_INT));

    arg2->AsIntCon()->SetIconValue(nNewStkArgsWords);

#ifdef DEBUG
    // arg 3 == numberOfOldStackArgsWords
    argEntry = call->gtArgs.GetArgByIndex(numArgs - 4);
    assert(argEntry != nullptr);
    GenTree* arg3 = argEntry->GetEarlyNode()->AsPutArgStk()->gtGetOp1();
    assert(arg3->OperIs(GT_CNS_INT));
#endif // DEBUG

    // Now reorder so all the putargs are just before the call.
    MovePutArgNodesUpToCall(call);

    // Transform this call node into a call to Jit tail call helper.
    call->gtCallType    = CT_HELPER;
    call->gtControlExpr = nullptr;
    call->gtCallMethHnd = m_compiler->eeFindHelper(CORINFO_HELP_TAILCALL);
    call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;

    // Lower this as if it were a pure helper call.
    call->gtCallMoreFlags &= ~(GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_JIT_HELPER);
    GenTree* result = LowerDirectCall(call);

    // Now add back tail call flags for identifying this node as tail call dispatched via helper.
    call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_JIT_HELPER;

#ifdef PROFILING_SUPPORTED
    // Insert profiler tail call hook if needed.
    // Since we don't know the insertion point, pass null for second param.
    if (m_compiler->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, nullptr);
    }
#endif // PROFILING_SUPPORTED

    return result;
}

//------------------------------------------------------------------------
// LowerCFGCall: Potentially lower a call to use control-flow guard. This
// expands indirect calls into either a validate+call sequence or to a dispatch
// helper taking the original target in a special register.
//
// Arguments:
//    call         -  The call node
//
void Lowering::LowerCFGCall(GenTreeCall* call)
{
    assert(!call->IsHelperCall(CORINFO_HELP_DISPATCH_INDIRECT_CALL));
    if (call->IsHelperCall(CORINFO_HELP_VALIDATE_INDIRECT_CALL))
    {
        return;
    }
    auto cloneUse = [=](LIR::Use& use, bool cloneConsts) -> GenTree* {
        bool canClone = cloneConsts && use.Def()->IsCnsIntOrI();
        if (!canClone && use.Def()->OperIs(GT_LCL_VAR))
        {
            canClone = !m_compiler->lvaGetDesc(use.Def()->AsLclVarCommon())->IsAddressExposed();
        }

        if (canClone)
        {
            return m_compiler->gtCloneExpr(use.Def());
        }
        else
        {
            unsigned newLcl = use.ReplaceWithLclVar(m_compiler);
            return m_compiler->gtNewLclvNode(newLcl, TYP_I_IMPL);
        }
    };

    GenTree* callTarget = call->gtControlExpr;

    if (callTarget == nullptr)
    {
        assert((call->gtCallType != CT_INDIRECT) && (!call->IsVirtual() || call->IsVirtualStubRelativeIndir()));
        if (!call->IsVirtual())
        {
            // Direct call with stashed address
            return;
        }

        // This is a VSD call with the call target being null because we are
        // supposed to load it from the indir cell. Due to CFG we will need
        // this address twice, and at least on ARM64 we do not want to
        // materialize the constant both times.
        CallArg* indirCellArg = call->gtArgs.FindWellKnownArg(WellKnownArg::VirtualStubCell);
        assert((indirCellArg != nullptr) && indirCellArg->GetNode()->OperIs(GT_PUTARG_REG));

        GenTreeOp* putArgNode = indirCellArg->GetNode()->AsOp();
        LIR::Use   indirCellArgUse(BlockRange(), &putArgNode->gtOp1, putArgNode);

        // On non-xarch, we create a local even for constants. On xarch cloning
        // the constant is better since it can be contained in the load below.
        bool cloneConsts = false;
#ifdef TARGET_XARCH
        cloneConsts = true;
#endif

        GenTree* indirCellClone = cloneUse(indirCellArgUse, cloneConsts);

        if (indirCellArgUse.Def()->OperIs(GT_LCL_VAR) || (cloneConsts && indirCellArgUse.Def()->IsCnsIntOrI()))
        {
            indirCellClone = m_compiler->gtClone(indirCellArgUse.Def());
        }
        else
        {
            unsigned newLcl = indirCellArgUse.ReplaceWithLclVar(m_compiler);
            indirCellClone  = m_compiler->gtNewLclvNode(newLcl, TYP_I_IMPL);
        }

        callTarget                  = Ind(indirCellClone);
        LIR::Range controlExprRange = LIR::SeqTree(m_compiler, callTarget);
        ContainCheckRange(controlExprRange);

        BlockRange().InsertBefore(call, std::move(controlExprRange));
        call->gtControlExpr = callTarget;
    }
    else
    {
        if (callTarget->IsIntegralConst())
        {
            // This is a direct call, no CFG check is necessary.
            return;
        }
    }

    CFGCallKind cfgKind = call->GetCFGCallKind();

    switch (cfgKind)
    {
        case CFGCallKind::ValidateAndCall:
        {
            // To safely apply CFG we need to generate a very specific pattern:
            // in particular, it is a safety issue to allow the JIT to reload
            // the call target from memory between calling
            // CORINFO_HELP_VALIDATE_INDIRECT_CALL and the target. This is
            // something that would easily occur in debug codegen if we
            // produced high-level IR. Instead we will use a GT_PHYSREG node
            // to get the target back from the register that contains the target.
            //
            // Additionally, the validator does not preserve all arg registers,
            // so we have to move all GT_PUTARG_REG nodes that would otherwise
            // be trashed ahead. The JIT also has an internal invariant that
            // once GT_PUTARG nodes start to appear in LIR, the call is coming
            // up. To avoid breaking this invariant we move _all_ GT_PUTARG
            // nodes (in particular, GC info reporting relies on this).
            //
            // To sum up, we end up transforming
            //
            // ta... = <early args>
            // tb... = <late args>
            // tc = callTarget
            // GT_CALL tc, ta..., tb...
            //
            // into
            //
            // ta... = <early args> (without GT_PUTARG_* nodes)
            // tb = callTarget
            // GT_CALL CORINFO_HELP_VALIDATE_INDIRECT_CALL, tb
            // tc = GT_PHYSREG REG_VALIDATE_INDIRECT_CALL_ADDR (preserved by helper)
            // td = <moved GT_PUTARG_* nodes>
            // GT_CALL tb, ta..., td..
            //

            GenTree* regNode = PhysReg(REG_VALIDATE_INDIRECT_CALL_ADDR, TYP_I_IMPL);
            LIR::Use useOfTar;
            bool     gotUse = BlockRange().TryGetUse(callTarget, &useOfTar);
            assert(gotUse);
            useOfTar.ReplaceWith(regNode);

            // Add the call to the validator. Use a placeholder for the target while we
            // morph, sequence and lower, to avoid redoing that for the actual target.
            GenTree*     targetPlaceholder = m_compiler->gtNewZeroConNode(callTarget->TypeGet());
            GenTreeCall* validate = m_compiler->gtNewHelperCallNode(CORINFO_HELP_VALIDATE_INDIRECT_CALL, TYP_VOID);
            NewCallArg   newArg =
                NewCallArg::Primitive(targetPlaceholder).WellKnown(WellKnownArg::ValidateIndirectCallTarget);
            validate->gtArgs.PushFront(m_compiler, newArg);

            m_compiler->fgMorphTree(validate);

            LIR::Range validateRange = LIR::SeqTree(m_compiler, validate);
            GenTree*   validateFirst = validateRange.FirstNode();
            GenTree*   validateLast  = validateRange.LastNode();
            // Insert the validator with the call target before the late args.
            BlockRange().InsertBefore(call, std::move(validateRange));

            // Swap out the target
            gotUse = BlockRange().TryGetUse(targetPlaceholder, &useOfTar);
            assert(gotUse);
            useOfTar.ReplaceWith(callTarget);
            targetPlaceholder->SetUnusedValue();

            LowerRange(validateFirst, validateLast);

            // Insert the PHYSREG node that we must load right after validation.
            BlockRange().InsertAfter(validate, regNode);
            LowerNode(regNode);

            // Finally move all GT_PUTARG_* nodes
            MovePutArgNodesUpToCall(call);
            break;
        }
        case CFGCallKind::Dispatch:
        {
#ifdef REG_DISPATCH_INDIRECT_CALL_ADDR
            // Now insert the call target as an extra argument.
            //
            NewCallArg callTargetNewArg =
                NewCallArg::Primitive(callTarget).WellKnown(WellKnownArg::DispatchIndirectCallTarget);
            CallArg* targetArg = call->gtArgs.PushBack(m_compiler, callTargetNewArg);
            targetArg->SetEarlyNode(nullptr);
            targetArg->SetLateNode(callTarget);
            call->gtArgs.PushLateBack(targetArg);

            // Set up ABI information for this arg.
            targetArg->AbiInfo =
                ABIPassingInformation::FromSegmentByValue(m_compiler,
                                                          ABIPassingSegment::InRegister(REG_DISPATCH_INDIRECT_CALL_ADDR,
                                                                                        0, TARGET_POINTER_SIZE));

            // Lower the newly added args now that call is updated
            LowerArg(call, targetArg);

            // Finally update the call to be a helper call
            call->gtCallType    = CT_HELPER;
            call->gtControlExpr = nullptr;
            call->gtCallMethHnd = Compiler::eeFindHelper(CORINFO_HELP_DISPATCH_INDIRECT_CALL);
            call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;
#ifdef FEATURE_READYTORUN
            call->gtEntryPoint.addr       = nullptr;
            call->gtEntryPoint.accessType = IAT_VALUE;
#endif

            // Now relower the call target
            call->gtControlExpr = LowerDirectCall(call);

            if (call->gtControlExpr != nullptr)
            {
                LIR::Range dispatchControlExprRange = LIR::SeqTree(m_compiler, call->gtControlExpr);

                ContainCheckRange(dispatchControlExprRange);
                BlockRange().InsertBefore(call, std::move(dispatchControlExprRange));
            }
#else
            assert(!"Unexpected CFGCallKind::Dispatch for platform without dispatcher");
#endif
            break;
        }
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// IsCFGCallArgInvariantInRange: A cheap version of IsInvariantInRange to check
// if a node is invariant in the specified range. In other words, can 'node' be
// moved to right before 'endExclusive' without its computation changing
// values?
//
// Arguments:
//    node         -  The node.
//    endExclusive -  The exclusive end of the range to check invariance for.
//
bool Lowering::IsCFGCallArgInvariantInRange(GenTree* node, GenTree* endExclusive)
{
    assert(node->Precedes(endExclusive));

    if (node->IsInvariant())
    {
        return true;
    }

    if (!node->IsValue())
    {
        return false;
    }

    if (node->OperIsLocal())
    {
        GenTreeLclVarCommon* lcl  = node->AsLclVarCommon();
        LclVarDsc*           desc = m_compiler->lvaGetDesc(lcl);
        if (desc->IsAddressExposed())
        {
            return false;
        }

        // Currently, non-address exposed locals have the property that their
        // use occurs at the user, so no further interference check is
        // necessary.
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// MovePutArgUpToCall: Given a call that will be transformed using the
// CFG validate+call or similar scheme, and an argument GT_PUTARG_* or GT_FIELD_LIST node,
// move that node right before the call.
//
// Arguments:
//    call - The call that is being transformed
//    node - The argument node
//
// Remarks:
//    We can always move the GT_PUTARG_* node further ahead as the side-effects
//    of these nodes are handled by LSRA. However, the operands of these nodes
//    are not always safe to move further ahead; for invariant operands, we
//    move them ahead as well to shorten the lifetime of these values.
//
void Lowering::MovePutArgUpToCall(GenTreeCall* call, GenTree* node)
{
    assert(!HAS_FIXED_REGISTER_SET || node->OperIsPutArg() || node->OperIsFieldList());

    if (node->OperIsFieldList())
    {
        JITDUMP("Node is a GT_FIELD_LIST; moving all operands\n");
        for (GenTreeFieldList::Use& operand : node->AsFieldList()->Uses())
        {
            assert(operand.GetNode()->OperIsPutArg());
            MovePutArgUpToCall(call, operand.GetNode());
        }
    }
    else if (node->OperIsPutArg())
    {
        GenTree* operand = node->AsOp()->gtGetOp1();
        JITDUMP("Checking if we can move operand of GT_PUTARG_* node:\n");
        DISPTREE(operand);
        if (((operand->gtFlags & GTF_ALL_EFFECT) == 0) && IsCFGCallArgInvariantInRange(operand, call))
        {
            JITDUMP("...yes, moving to after validator call\n");
            BlockRange().Remove(operand);
            BlockRange().InsertBefore(call, operand);
        }
        else
        {
            JITDUMP("...no, operand has side effects or is not invariant\n");
        }
    }
    else
    {
        assert(!HAS_FIXED_REGISTER_SET);
        // No moving necessary
        return;
    }

    JITDUMP("Moving\n");
    DISPTREE(node);
    JITDUMP("\n");
    BlockRange().Remove(node);
    BlockRange().InsertBefore(call, node);
}

//------------------------------------------------------------------------
// MovePutArgNodesUpToCall: Move all GT_PUTARG_* or GT_FIELD_LIST nodes right before the call.
//
// Arguments:
//    call - The call that is being transformed
//
// Remarks:
//    See comments in MovePutArgUpToCall for more details.
//
void Lowering::MovePutArgNodesUpToCall(GenTreeCall* call)
{
    // Finally move all GT_PUTARG_* nodes
    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        GenTree* node = arg.GetEarlyNode();
        assert(!HAS_FIXED_REGISTER_SET || node->OperIsPutArg() || node->OperIsFieldList());
        MovePutArgUpToCall(call, node);
    }

    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        GenTree* node = arg.GetLateNode();
        assert(!HAS_FIXED_REGISTER_SET || node->OperIsPutArg() || node->OperIsFieldList());
        MovePutArgUpToCall(call, node);
    }
}
