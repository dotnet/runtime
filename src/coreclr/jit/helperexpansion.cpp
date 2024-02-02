// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include <minipal/utf8.h>
#ifdef _MSC_VER
#pragma hdrstop
#endif

// Obtain constant pointer from a tree
static void* GetConstantPointer(Compiler* comp, GenTree* tree)
{
    void* cns = nullptr;
    if (tree->gtEffectiveVal()->IsCnsIntOrI())
    {
        cns = (void*)tree->gtEffectiveVal()->AsIntCon()->IconValue();
    }
    else if (comp->vnStore->IsVNConstant(tree->gtVNPair.GetLiberal()))
    {
        cns = (void*)comp->vnStore->CoercedConstantValue<ssize_t>(tree->gtVNPair.GetLiberal());
    }
    return cns;
}

// Save expression to a local and append it as the last statement in exprBlock
static GenTree* SpillExpression(Compiler* comp, GenTree* expr, BasicBlock* exprBlock, DebugInfo& debugInfo)
{
    unsigned const tmpNum = comp->lvaGrabTemp(true DEBUGARG("spilling expr"));
    Statement*     stmt   = comp->fgNewStmtAtEnd(exprBlock, comp->gtNewTempStore(tmpNum, expr), debugInfo);
    comp->gtSetStmtInfo(stmt);
    comp->fgSetStmtSeq(stmt);

    return comp->gtNewLclVarNode(tmpNum);
};

//------------------------------------------------------------------------------
// SplitAtTreeAndReplaceItWithLocal : Split block at the given tree and replace it with a local
//    See comments in gtSplitTree and fgSplitBlockBeforeTree
//    TODO: use this function in more places in this file.
//
// Arguments:
//    comp        - Compiler instance
//    block       - Block to split
//    stmt        - Statement containing the tree to split at
//    tree        - Tree to split at
//    topBlock    - [out] Top block after the split
//    bottomBlock - [out] Bottom block after the split
//
// Return Value:
//    Number of the local that replaces the tree
//
static unsigned SplitAtTreeAndReplaceItWithLocal(
    Compiler* comp, BasicBlock* block, Statement* stmt, GenTree* tree, BasicBlock** topBlock, BasicBlock** bottomBlock)
{
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    block                    = comp->fgSplitBlockBeforeTree(block, stmt, tree, &newFirstStmt, &callUse);
    assert(prevBb != nullptr && block != nullptr);

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        comp->fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

    // Grab a temp to store the result.
    const unsigned tmpNum         = comp->lvaGrabTemp(true DEBUGARG("replacement local"));
    comp->lvaTable[tmpNum].lvType = tree->TypeGet();

    // Replace the original call with that temp
    *callUse = comp->gtNewLclvNode(tmpNum, tree->TypeGet());

    comp->fgMorphStmtBlockOps(block, stmt);
    comp->gtUpdateStmtSideEffects(stmt);

    *topBlock    = prevBb;
    *bottomBlock = block;
    return tmpNum;
}

//------------------------------------------------------------------------------
// gtNewRuntimeLookupHelperCallNode : Helper to create a runtime lookup call helper node.
//
// Arguments:
//    helper    - Call helper
//    type      - Type of the node
//    args      - Call args
//
// Return Value:
//    New CT_HELPER node
//
GenTreeCall* Compiler::gtNewRuntimeLookupHelperCallNode(CORINFO_RUNTIME_LOOKUP* pRuntimeLookup,
                                                        GenTree*                ctxTree,
                                                        void*                   compileTimeHandle)
{
    // Call the helper
    // - Setup argNode with the pointer to the signature returned by the lookup
    GenTree* argNode = gtNewIconEmbHndNode(pRuntimeLookup->signature, nullptr, GTF_ICON_GLOBAL_PTR, compileTimeHandle);
    GenTreeCall* helperCall = gtNewHelperCallNode(pRuntimeLookup->helper, TYP_I_IMPL, ctxTree, argNode);

    // No need to perform CSE/hoisting for signature node - it is expected to end up in a rarely-taken block after
    // "Expand runtime lookups" phase.
    argNode->gtFlags |= GTF_DONT_CSE;

    // Leave a note that this method has runtime lookups we might want to expand (nullchecks, size checks) later.
    // We can also consider marking current block as a runtime lookup holder to improve TP for Tier0
    impInlineRoot()->setMethodHasExpRuntimeLookup();
    helperCall->SetExpRuntimeLookup();
    if (!impInlineRoot()->GetSignatureToLookupInfoMap()->Lookup(pRuntimeLookup->signature))
    {
        JITDUMP("Registering %p in SignatureToLookupInfoMap\n", pRuntimeLookup->signature)
        impInlineRoot()->GetSignatureToLookupInfoMap()->Set(pRuntimeLookup->signature, *pRuntimeLookup);
    }
    return helperCall;
}

//------------------------------------------------------------------------------
// fgExpandRuntimeLookups : partially expand runtime lookups helper calls
//                          to add a nullcheck [+ size check] and a fast path
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//    The runtime lookup itself is needed to access a handle in code shared between
//    generic instantiations. The lookup depends on the typeContext which is only available at
//    runtime, and not at compile - time. See ASCII block diagrams in comments below for
//    better understanding how this phase expands runtime lookups.
//
PhaseStatus Compiler::fgExpandRuntimeLookups()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if (!doesMethodHaveExpRuntimeLookup())
    {
        // The method being compiled doesn't have expandable runtime lookups. If it does
        // and doesMethodHaveExpRuntimeLookup() still returns false we'll assert in LowerCall
        return result;
    }

    return fgExpandHelper<&Compiler::fgExpandRuntimeLookupsForCall>(false);
}

//------------------------------------------------------------------------------
// fgExpandRuntimeLookupsForCall : partially expand runtime lookups helper calls
//    to add a nullcheck [+ size check] and a fast path
//
// Arguments:
//    pBlock - Block containing the helper call to expand. If expansion is performed,
//             this is updated to the new block that was an outcome of block splitting.
//    stmt   - Statement containing the helper call
//    call   - The helper call
//
// Returns:
//    true if a runtime lookup was found and expanded.
//
// Notes:
//    The runtime lookup itself is needed to access a handle in code shared between
//    generic instantiations. The lookup depends on the typeContext which is only available at
//    runtime, and not at compile - time. See ASCII block diagrams in comments below for
//    better understanding how this phase expands runtime lookups.
//
bool Compiler::fgExpandRuntimeLookupsForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    BasicBlock* block = *pBlock;

    if (!call->IsHelperCall() || !call->IsExpRuntimeLookup())
    {
        return false;
    }

    // Clear ExpRuntimeLookup flag so we won't miss any runtime lookup that needs partial expansion
    call->ClearExpRuntimeLookup();

    if (call->IsTailCall())
    {
        // It is very unlikely to happen and is impossible to represent in C#
        return false;
    }

    assert(call->gtArgs.CountArgs() == 2);
    // The call has the following signature:
    //
    //   type = call(genericCtx, signatureCns);
    //
    void* signature = GetConstantPointer(this, call->gtArgs.GetArgByIndex(1)->GetNode());
    if (signature == nullptr)
    {
        // Technically, it is possible (e.g. it was CSE'd and then VN was erased), but for Debug mode we
        // want to catch such cases as we really don't want to emit just a fallback call - it's too slow
        assert(!"can't restore signature argument value");
        return false;
    }

    JITDUMP("Expanding runtime lookup for [%06d] in " FMT_BB ":\n", dspTreeID(call), block->bbNum)
    DISPTREE(call)
    JITDUMP("\n")

    // Restore runtimeLookup using signature argument via a global dictionary
    CORINFO_RUNTIME_LOOKUP runtimeLookup = {};
    const bool             lookupFound   = GetSignatureToLookupInfoMap()->Lookup(signature, &runtimeLookup);
    assert(lookupFound);

    const bool needsSizeCheck = runtimeLookup.sizeOffset != CORINFO_NO_SIZE_CHECK;
    if (needsSizeCheck)
    {
        JITDUMP("dynamic expansion, needs size check.\n")
    }

    DebugInfo debugInfo = stmt->GetDebugInfo();

    assert(runtimeLookup.indirections != 0);
    assert(runtimeLookup.testForNull);

    // Split block right before the call tree
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    block                    = fgSplitBlockBeforeTree(block, stmt, call, &newFirstStmt, &callUse);
    *pBlock                  = block;
    assert(prevBb != nullptr && block != nullptr);

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse.
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

    GenTreeLclVar* rtLookupLcl = nullptr;

    // Mostly for Tier0: if the current statement is STORE_LCL_VAR(RuntimeLookup)
    // we can drop it and use that LCL as the destination
    if (stmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR) && (stmt->GetRootNode()->AsLclVar()->Data() == *callUse))
    {
        rtLookupLcl = gtNewLclVarNode(stmt->GetRootNode()->AsLclVar()->GetLclNum());
        fgRemoveStmt(block, stmt);
    }

    // Grab a temp to store result (it's assigned from either fastPathBb or fallbackBb)
    if (rtLookupLcl == nullptr)
    {
        // Define a local for the result
        unsigned rtLookupLclNum         = lvaGrabTemp(true DEBUGARG("runtime lookup"));
        lvaTable[rtLookupLclNum].lvType = TYP_I_IMPL;
        rtLookupLcl                     = gtNewLclvNode(rtLookupLclNum, call->TypeGet());

        *callUse = gtClone(rtLookupLcl);

        fgMorphStmtBlockOps(block, stmt);
        gtUpdateStmtSideEffects(stmt);
    }

    GenTree* ctxTree = call->gtArgs.GetArgByIndex(0)->GetNode();
    GenTree* sigNode = call->gtArgs.GetArgByIndex(1)->GetNode();

    // Prepare slotPtr tree (TODO: consider sharing this part with impRuntimeLookup)
    GenTree* slotPtrTree   = gtCloneExpr(ctxTree);
    GenTree* indOffTree    = nullptr;
    GenTree* lastIndOfTree = nullptr;
    for (WORD i = 0; i < runtimeLookup.indirections; i++)
    {
        if ((i == 1 && runtimeLookup.indirectFirstOffset) || (i == 2 && runtimeLookup.indirectSecondOffset))
        {
            indOffTree  = SpillExpression(this, slotPtrTree, prevBb, debugInfo);
            slotPtrTree = gtCloneExpr(indOffTree);
        }

        // The last indirection could be subject to a size check (dynamic dictionary expansion)
        const bool isLastIndirectionWithSizeCheck = (i == runtimeLookup.indirections - 1) && needsSizeCheck;
        if (i != 0)
        {
            GenTreeFlags indirFlags = GTF_IND_NONFAULTING;
            if (!isLastIndirectionWithSizeCheck)
            {
                indirFlags |= GTF_IND_INVARIANT;
            }
            slotPtrTree = gtNewIndir(TYP_I_IMPL, slotPtrTree, indirFlags);
        }

        if ((i == 1 && runtimeLookup.indirectFirstOffset) || (i == 2 && runtimeLookup.indirectSecondOffset))
        {
            slotPtrTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, indOffTree, slotPtrTree);
        }
        if (runtimeLookup.offsets[i] != 0)
        {
            if (isLastIndirectionWithSizeCheck)
            {
                lastIndOfTree = SpillExpression(this, slotPtrTree, prevBb, debugInfo);
                slotPtrTree   = gtCloneExpr(lastIndOfTree);
            }
            slotPtrTree =
                gtNewOperNode(GT_ADD, TYP_I_IMPL, slotPtrTree, gtNewIconNode(runtimeLookup.offsets[i], TYP_I_IMPL));
        }
    }

    // Non-dynamic expansion case (no size check):
    //
    // prevBb(BBJ_ALWAYS):                  [weight: 1.0]
    //     ...
    //
    // nullcheckBb(BBJ_COND):               [weight: 1.0]
    //     if (*fastPathValue == null)
    //         goto fallbackBb;
    //
    // fastPathBb(BBJ_ALWAYS):              [weight: 0.8]
    //     rtLookupLcl = *fastPathValue;
    //     goto block;
    //
    // fallbackBb(BBJ_ALWAYS):              [weight: 0.2]
    //     rtLookupLcl = HelperCall();
    //
    // block(...):                          [weight: 1.0]
    //     use(rtLookupLcl);
    //

    // null-check basic block
    GenTree* fastPathValue = gtNewIndir(TYP_I_IMPL, gtCloneExpr(slotPtrTree), GTF_IND_NONFAULTING);
    // Save dictionary slot to a local (to be used by fast path)
    GenTree* fastPathValueClone =
        opts.OptimizationEnabled() ? fgMakeMultiUse(&fastPathValue) : gtCloneExpr(fastPathValue);
    GenTree* nullcheckOp = gtNewOperNode(GT_EQ, TYP_INT, fastPathValue, gtNewIconNode(0, TYP_I_IMPL));
    nullcheckOp->gtFlags |= GTF_RELOP_JMP_USED;

    // nullcheckBb conditionally jumps to fallbackBb, but we need to initialize fallbackBb last
    // so we can place it after nullcheckBb. So set the jump target later.
    BasicBlock* nullcheckBb =
        fgNewBBFromTreeAfter(BBJ_COND, prevBb, gtNewOperNode(GT_JTRUE, TYP_VOID, nullcheckOp), debugInfo);

    // Fallback basic block
    GenTree*    fallbackValueDef = gtNewStoreLclVarNode(rtLookupLcl->GetLclNum(), call);
    BasicBlock* fallbackBb =
        fgNewBBFromTreeAfter(BBJ_ALWAYS, nullcheckBb, fallbackValueDef, debugInfo, nullcheckBb->Next(), true);

    assert(fallbackBb->JumpsToNext());
    fallbackBb->SetFlags(BBF_NONE_QUIRK);

    // Set nullcheckBb's true jump target
    nullcheckBb->SetTrueTarget(fallbackBb);

    // Fast-path basic block
    GenTree*    fastpathValueDef = gtNewStoreLclVarNode(rtLookupLcl->GetLclNum(), fastPathValueClone);
    BasicBlock* fastPathBb       = fgNewBBFromTreeAfter(BBJ_ALWAYS, nullcheckBb, fastpathValueDef, debugInfo, block);

    // Set nullcheckBb's false jump target
    nullcheckBb->SetFalseTarget(fastPathBb);

    BasicBlock* sizeCheckBb = nullptr;
    if (needsSizeCheck)
    {
        // Dynamic expansion case (sizeCheckBb is added and some preds are changed):
        //
        // prevBb(BBJ_ALWAYS):                  [weight: 1.0]
        //
        // sizeCheckBb(BBJ_COND):               [weight: 1.0]
        //     if (sizeValue <= offsetValue)
        //         goto fallbackBb;
        //     ...
        //
        // nullcheckBb(BBJ_COND):               [weight: 0.8]
        //     if (*fastPathValue == null)
        //         goto fallbackBb;
        //
        // fastPathBb(BBJ_ALWAYS):              [weight: 0.64]
        //     rtLookupLcl = *fastPathValue;
        //     goto block;
        //
        // fallbackBb(BBJ_ALWAYS):              [weight: 0.36]
        //     rtLookupLcl = HelperCall();
        //
        // block(...):                          [weight: 1.0]
        //     use(rtLookupLcl);
        //

        // sizeValue = dictionary[pRuntimeLookup->sizeOffset]
        GenTreeIntCon* sizeOffset = gtNewIconNode(runtimeLookup.sizeOffset, TYP_I_IMPL);
        assert(lastIndOfTree != nullptr);
        GenTree* sizeValueOffset = gtNewOperNode(GT_ADD, TYP_I_IMPL, lastIndOfTree, sizeOffset);
        GenTree* sizeValue       = gtNewIndir(TYP_I_IMPL, sizeValueOffset, GTF_IND_NONFAULTING);

        // sizeCheck fails if sizeValue <= pRuntimeLookup->offsets[i]
        GenTree* offsetValue = gtNewIconNode(runtimeLookup.offsets[runtimeLookup.indirections - 1], TYP_I_IMPL);
        GenTree* sizeCheck   = gtNewOperNode(GT_LE, TYP_INT, sizeValue, offsetValue);
        sizeCheck->gtFlags |= GTF_RELOP_JMP_USED;

        GenTree* jtrue = gtNewOperNode(GT_JTRUE, TYP_VOID, sizeCheck);
        // sizeCheckBb fails - jump to fallbackBb
        sizeCheckBb = fgNewBBFromTreeAfter(BBJ_COND, prevBb, jtrue, debugInfo, fallbackBb);
        sizeCheckBb->SetFalseTarget(nullcheckBb);
    }

    //
    // Update preds in all new blocks
    //
    fgRemoveRefPred(block, prevBb);
    fgAddRefPred(block, fastPathBb);
    fgAddRefPred(block, fallbackBb);
    assert(prevBb->KindIs(BBJ_ALWAYS));

    if (needsSizeCheck)
    {
        // sizeCheckBb is the first block after prevBb
        prevBb->SetTarget(sizeCheckBb);
        fgAddRefPred(sizeCheckBb, prevBb);
        // sizeCheckBb flows into nullcheckBb in case if the size check passes
        fgAddRefPred(nullcheckBb, sizeCheckBb);
        // fallbackBb is reachable from both nullcheckBb and sizeCheckBb
        fgAddRefPred(fallbackBb, nullcheckBb);
        fgAddRefPred(fallbackBb, sizeCheckBb);
        // fastPathBb is only reachable from successful nullcheckBb
        fgAddRefPred(fastPathBb, nullcheckBb);
    }
    else
    {
        // nullcheckBb is the first block after prevBb
        prevBb->SetTarget(nullcheckBb);
        fgAddRefPred(nullcheckBb, prevBb);
        // No size check, nullcheckBb jumps to fast path
        fgAddRefPred(fastPathBb, nullcheckBb);
        // fallbackBb is only reachable from nullcheckBb (jump destination)
        fgAddRefPred(fallbackBb, nullcheckBb);
    }

    //
    // Re-distribute weights (see '[weight: X]' on the diagrams above)
    // TODO: consider marking fallbackBb as rarely-taken
    //
    block->inheritWeight(prevBb);
    if (needsSizeCheck)
    {
        sizeCheckBb->inheritWeight(prevBb);
        // 80% chance we pass nullcheck
        nullcheckBb->inheritWeightPercentage(sizeCheckBb, 80);
        // 64% (0.8 * 0.8) chance we pass both nullcheck and sizecheck
        fastPathBb->inheritWeightPercentage(nullcheckBb, 80);
        // 100-64=36% chance we fail either nullcheck or sizecheck
        fallbackBb->inheritWeightPercentage(sizeCheckBb, 36);
    }
    else
    {
        nullcheckBb->inheritWeight(prevBb);
        // 80% chance we pass nullcheck
        fastPathBb->inheritWeightPercentage(nullcheckBb, 80);
        // 20% chance we fail nullcheck (TODO: Consider making it cold (0%))
        fallbackBb->inheritWeightPercentage(nullcheckBb, 20);
    }

    // All blocks are expected to be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, block));
    assert(BasicBlock::sameEHRegion(prevBb, nullcheckBb));
    assert(BasicBlock::sameEHRegion(prevBb, fastPathBb));
    if (needsSizeCheck)
    {
        assert(BasicBlock::sameEHRegion(prevBb, sizeCheckBb));
    }
    return true;
}

//------------------------------------------------------------------------------
// fgExpandThreadLocalAccess: Inline the CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
//      or CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED helper. See
//      fgExpandThreadLocalAccessForCall for details.
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgExpandThreadLocalAccess()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if (!methodHasTlsFieldAccess())
    {
        // TP: nothing to expand in the current method
        JITDUMP("Nothing to expand.\n")
        return result;
    }

    // Always expand for NativeAOT because the slow TLS access helper will not be generated by NativeAOT
    const bool isNativeAOT = IsTargetAbi(CORINFO_NATIVEAOT_ABI);
    if (isNativeAOT)
    {
        return fgExpandHelper<&Compiler::fgExpandThreadLocalAccessForCallNativeAOT>(
            false /* expand rarely run blocks for NativeAOT */);
    }

    if (opts.OptimizationDisabled())
    {
        JITDUMP("Optimizations aren't allowed - bail out.\n")
        return result;
    }

    // TODO: Replace with opts.compCodeOpt once it's fixed
    const bool preferSize = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT);
    if (preferSize)
    {
        // The optimization comes with a codegen size increase
        JITDUMP("Optimized for size - bail out.\n")
        return result;
    }

    return fgExpandHelper<&Compiler::fgExpandThreadLocalAccessForCall>(true);
}

//------------------------------------------------------------------------------
// fgExpandThreadLocalAccessForCallNativeAOT : Expand the access of tlsRoot needed
//  to access fields marked with [ThreadLocal].
//
// Arguments:
//    pBlock - Block containing the helper call to expand. If expansion is performed,
//             this is updated to the new block that was an outcome of block splitting.
//    stmt   - Statement containing the helper call
//    call   - The helper call
//
//
// Returns:
//    true if we expanded any field access, false otherwise.
//
bool Compiler::fgExpandThreadLocalAccessForCallNativeAOT(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    assert(IsTargetAbi(CORINFO_NATIVEAOT_ABI));
    BasicBlock*     block  = *pBlock;
    CorInfoHelpFunc helper = call->GetHelperNum();

    const bool isExpTLSFieldAccess = (helper == CORINFO_HELP_READYTORUN_THREADSTATIC_BASE_NOCTOR);
    if (!call->IsHelperCall() || !isExpTLSFieldAccess)
    {
        return false;
    }

    JITDUMP("Expanding thread static local access for [%06d] in " FMT_BB ":\n", dspTreeID(call), block->bbNum);
    DISPTREE(call);
    JITDUMP("\n");

    CORINFO_THREAD_STATIC_INFO_NATIVEAOT threadStaticInfo;
    memset(&threadStaticInfo, 0, sizeof(CORINFO_THREAD_STATIC_INFO_NATIVEAOT));

    info.compCompHnd->getThreadLocalStaticInfo_NativeAOT(&threadStaticInfo);

    JITDUMP("tlsRootObject= %p\n", dspPtr(threadStaticInfo.tlsRootObject.addr));
    JITDUMP("tlsIndexObject= %p\n", dspPtr(threadStaticInfo.tlsIndexObject.addr));
    JITDUMP("offsetOfThreadLocalStoragePointer= %u\n", dspOffset(threadStaticInfo.offsetOfThreadLocalStoragePointer));
    JITDUMP("threadStaticBaseSlow= %p\n", dspPtr(threadStaticInfo.threadStaticBaseSlow.addr));

    // Split block right before the call tree
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    DebugInfo   debugInfo    = stmt->GetDebugInfo();
    block                    = fgSplitBlockBeforeTree(block, stmt, call, &newFirstStmt, &callUse);
    *pBlock                  = block;
    var_types callType       = call->TypeGet();
    assert(prevBb != nullptr && block != nullptr);

    unsigned finalLclNum = lvaGrabTemp(true DEBUGARG("Final offset"));
    // Note, `tlsRoot` refers to the TLS blob object, which is an unpinned managed object,
    // thus the type of the local is TYP_REF
    lvaTable[finalLclNum].lvType = TYP_REF;
    GenTree* finalLcl            = gtNewLclVarNode(finalLclNum);

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse.
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

#ifdef TARGET_64BIT
    // prevBb (BBJ_NONE):                                               [weight: 1.0]
    //      ...
    //
    // tlsRootNullCondBB (BBJ_COND):                                    [weight: 1.0]
    //      fastPathValue = [tlsRootAddress]
    //      if (fastPathValue != nullptr)
    //          goto fastPathBb;
    //
    // fallbackBb (BBJ_ALWAYS):                                         [weight: 0]
    //      tlsRoot = HelperCall();
    //      goto block;
    //
    // fastPathBb(BBJ_ALWAYS):                                          [weight: 1.0]
    //      tlsRoot = fastPathValue;
    //
    // block (...):                                                     [weight: 1.0]
    //      use(tlsRoot);
    // ...

    GenTree*               tlsRootAddr   = nullptr;
    CORINFO_GENERIC_HANDLE tlsRootObject = threadStaticInfo.tlsRootObject.handle;

    if (TargetOS::IsWindows)
    {
        // Mark this ICON as a TLS_HDL, codegen will use FS:[cns] or GS:[cns]
        GenTree* tlsValue = gtNewIconHandleNode(threadStaticInfo.offsetOfThreadLocalStoragePointer, GTF_ICON_TLS_HDL);
        tlsValue          = gtNewIndir(TYP_I_IMPL, tlsValue, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

        CORINFO_CONST_LOOKUP tlsIndexObject = threadStaticInfo.tlsIndexObject;

        GenTree* dllRef = gtNewIconHandleNode((size_t)tlsIndexObject.handle, GTF_ICON_OBJ_HDL);
        dllRef          = gtNewIndir(TYP_INT, dllRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
        dllRef          = gtNewOperNode(GT_MUL, TYP_I_IMPL, dllRef, gtNewIconNode(TARGET_POINTER_SIZE, TYP_INT));

        // Add the dllRef to produce thread local storage reference for coreclr
        tlsValue = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsValue, dllRef);

        // Base of coreclr's thread local storage
        tlsValue = gtNewIndir(TYP_I_IMPL, tlsValue, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

        // This resolves to an offset which is TYP_INT
        GenTree* tlsRootOffset = gtNewIconNode((size_t)tlsRootObject, TYP_INT);
        tlsRootOffset->gtFlags |= GTF_ICON_SECREL_OFFSET;

        // Add the tlsValue and tlsRootOffset to produce tlsRootAddr.
        tlsRootAddr = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsValue, tlsRootOffset);
    }
    else if (TargetOS::IsUnix)
    {
        if (TargetArchitecture::IsX64)
        {
            // Code sequence to access thread local variable on linux/x64:
            //      data16
            //      lea      rdi, 0x7FE5C418CD28  ; tlsRootObject
            //      data16 data16
            //      call     _tls_get_addr
            //
            // This sequence along with `data16` prefix is expected by the linker so it
            // will patch these with TLS access.
            GenTree* tls_get_addr_val =
                gtNewIconHandleNode((size_t)threadStaticInfo.tlsGetAddrFtnPtr.handle, GTF_ICON_FTN_ADDR);
            tls_get_addr_val->SetContained();

            GenTreeCall* tlsRefCall = gtNewIndCallNode(tls_get_addr_val, TYP_I_IMPL);
            tlsRefCall->gtFlags |= GTF_TLS_GET_ADDR;

            // This is an indirect call which takes an argument.
            // Populate and set the ABI appropriately.
            assert(tlsRootObject != 0);
            GenTree* tlsArg = gtNewIconNode((size_t)tlsRootObject, TYP_I_IMPL);
            tlsArg->gtFlags |= GTF_ICON_TLSGD_OFFSET;
            tlsRefCall->gtArgs.PushBack(this, NewCallArg::Primitive(tlsArg));

            fgMorphArgs(tlsRefCall);

            tlsRefCall->gtFlags |= GTF_EXCEPT | (tls_get_addr_val->gtFlags & GTF_GLOB_EFFECT);
            tlsRootAddr = tlsRefCall;
        }
        else if (TargetArchitecture::IsArm64)
        {
            /*
            x0 = adrp :tlsdesc:tlsRoot ; 1st parameter
            x0 += tlsdesc_lo12:tlsRoot ; update 1st parameter

            x1 = tpidr_el0             ; 2nd parameter

            x2 = [x0]                  ; call
            blr x2

            */

            GenTree* tlsRootOffset = gtNewIconHandleNode((size_t)tlsRootObject, GTF_ICON_TLS_HDL);
            tlsRootOffset->gtFlags |= GTF_ICON_TLSGD_OFFSET;

            GenTree*     tlsCallIndir = gtCloneExpr(tlsRootOffset);
            GenTreeCall* tlsRefCall   = gtNewIndCallNode(tlsCallIndir, TYP_I_IMPL);
            tlsRefCall->gtFlags |= GTF_TLS_GET_ADDR;
            fgMorphArgs(tlsRefCall);

            tlsRefCall->gtFlags |= GTF_EXCEPT | (tlsCallIndir->gtFlags & GTF_GLOB_EFFECT);
            tlsRootAddr = tlsRefCall;
        }
        else
        {
            unreached();
        }
    }
    else
    {
        unreached();
    }

    // Cache the TlsRootAddr value
    unsigned tlsRootAddrLclNum         = lvaGrabTemp(true DEBUGARG("TlsRootAddr access"));
    lvaTable[tlsRootAddrLclNum].lvType = TYP_I_IMPL;
    GenTree* tlsRootAddrDef            = gtNewStoreLclVarNode(tlsRootAddrLclNum, tlsRootAddr);
    GenTree* tlsRootAddrUse            = gtNewLclVarNode(tlsRootAddrLclNum);

    // See comments near finalLclNum above regarding TYP_REF
    GenTree* tlsRootVal = gtNewIndir(TYP_REF, tlsRootAddrUse, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    GenTree* tlsRootDef = gtNewStoreLclVarNode(finalLclNum, tlsRootVal);

    GenTree* tlsRootNullCond = gtNewOperNode(GT_NE, TYP_INT, gtCloneExpr(finalLcl), gtNewIconNode(0, TYP_I_IMPL));
    tlsRootNullCond          = gtNewOperNode(GT_JTRUE, TYP_VOID, tlsRootNullCond);

    // tlsRootNullCondBB
    BasicBlock* tlsRootNullCondBB = fgNewBBFromTreeAfter(BBJ_COND, prevBb, tlsRootAddrDef, debugInfo);
    fgInsertStmtAfter(tlsRootNullCondBB, tlsRootNullCondBB->firstStmt(), fgNewStmtFromTree(tlsRootNullCond));
    fgInsertStmtAfter(tlsRootNullCondBB, tlsRootNullCondBB->firstStmt(), fgNewStmtFromTree(tlsRootDef));

    CORINFO_CONST_LOOKUP threadStaticSlowHelper = threadStaticInfo.threadStaticBaseSlow;

    // See comments near finalLclNum above regarding TYP_REF
    GenTreeCall* slowHelper =
        gtNewIndCallNode(gtNewIconHandleNode((size_t)threadStaticSlowHelper.addr, GTF_ICON_TLS_HDL), TYP_REF);
    GenTree* helperArg = gtClone(tlsRootAddrUse);
    slowHelper->gtArgs.PushBack(this, NewCallArg::Primitive(helperArg));
    fgMorphArgs(slowHelper);

    // fallbackBb
    GenTree*    fallbackValueDef = gtNewStoreLclVarNode(finalLclNum, slowHelper);
    BasicBlock* fallbackBb =
        fgNewBBFromTreeAfter(BBJ_ALWAYS, tlsRootNullCondBB, fallbackValueDef, debugInfo, block, true);

    GenTree*    fastPathValueDef = gtNewStoreLclVarNode(finalLclNum, gtCloneExpr(finalLcl));
    BasicBlock* fastPathBb = fgNewBBFromTreeAfter(BBJ_ALWAYS, fallbackBb, fastPathValueDef, debugInfo, block, true);

    *callUse = finalLcl;

    fgMorphStmtBlockOps(block, stmt);
    gtUpdateStmtSideEffects(stmt);

    //
    // Update preds in all new blocks
    //
    fgAddRefPred(fallbackBb, tlsRootNullCondBB);
    fgAddRefPred(fastPathBb, tlsRootNullCondBB);

    fgAddRefPred(block, fallbackBb);
    fgAddRefPred(block, fastPathBb);

    tlsRootNullCondBB->SetTrueTarget(fastPathBb);
    tlsRootNullCondBB->SetFalseTarget(fallbackBb);

    // Inherit the weights
    block->inheritWeight(prevBb);
    tlsRootNullCondBB->inheritWeight(prevBb);
    fastPathBb->inheritWeight(prevBb);

    // fallback will just execute first time
    fallbackBb->bbSetRunRarely();

    fgRemoveRefPred(block, prevBb);
    fgAddRefPred(tlsRootNullCondBB, prevBb);
    prevBb->SetTarget(tlsRootNullCondBB);

    // All blocks are expected to be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, block));
    assert(BasicBlock::sameEHRegion(prevBb, tlsRootNullCondBB));
    assert(BasicBlock::sameEHRegion(prevBb, fastPathBb));

    JITDUMP("tlsRootNullCondBB: " FMT_BB "\n", tlsRootNullCondBB->bbNum);
    JITDUMP("fallbackBb: " FMT_BB "\n", fallbackBb->bbNum);
    JITDUMP("fastPathBb: " FMT_BB "\n", fastPathBb->bbNum);
#else
    unreached();

#endif // TARGET_64BIT

    return true;
}

//------------------------------------------------------------------------------
// fgExpandThreadLocalAccessForCall : Expand the CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED
//  or CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED, that access fields marked with [ThreadLocal].
//
// Arguments:
//    pBlock - Block containing the helper call to expand. If expansion is performed,
//             this is updated to the new block that was an outcome of block splitting.
//    stmt   - Statement containing the helper call
//    call   - The helper call
//
//
// Returns:
//    true if we expanded any field access, false otherwise.
//
// Notes:
//    A cache is stored in thread local storage (TLS) of coreclr. It maps the typeIndex (embedded in
//    the code at the JIT time) to the base of static blocks. This method generates code to
//    extract the TLS, get the entry at which the cache is stored. Then it checks if the typeIndex of
//    enclosing type of current field is present in the cache and if yes, extract out that can be directly
//    accessed at the uses.
//    If the entry is not present, the helper is called, which would make an entry of current static block
//    in the cache.
//
bool Compiler::fgExpandThreadLocalAccessForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    assert(!opts.IsReadyToRun());

    BasicBlock* block = *pBlock;

    CorInfoHelpFunc helper = call->GetHelperNum();

    if ((helper != CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED) &&
        (helper != CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED))
    {
        return false;
    }

    if (TargetOS::IsUnix)
    {
#if defined(TARGET_ARM) || !defined(TARGET_64BIT)
        // On Arm, Thread execution blocks are accessed using co-processor registers and instructions such
        // as MRC and MCR are used to access them. We do not support them and so should never optimize the
        // field access using TLS.
        noway_assert(!"Unsupported scenario of optimizing TLS access on Linux Arm32/x86");
#endif
    }
    else
    {
#ifdef TARGET_ARM
        // On Arm, Thread execution blocks are accessed using co-processor registers and instructions such
        // as MRC and MCR are used to access them. We do not support them and so should never optimize the
        // field access using TLS.
        noway_assert(!"Unsupported scenario of optimizing TLS access on Windows Arm32");
#endif
    }

    JITDUMP("Expanding thread static local access for [%06d] in " FMT_BB ":\n", dspTreeID(call), block->bbNum);
    DISPTREE(call);
    JITDUMP("\n");

    bool isGCThreadStatic =
        eeGetHelperNum(call->gtCallMethHnd) == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED;

    CORINFO_THREAD_STATIC_BLOCKS_INFO threadStaticBlocksInfo;
    memset(&threadStaticBlocksInfo, 0, sizeof(CORINFO_THREAD_STATIC_BLOCKS_INFO));

    info.compCompHnd->getThreadLocalStaticBlocksInfo(&threadStaticBlocksInfo, isGCThreadStatic);

    JITDUMP("getThreadLocalStaticBlocksInfo (%s)\n:", isGCThreadStatic ? "GC" : "Non-GC");
    JITDUMP("tlsIndex= %p\n", dspPtr(threadStaticBlocksInfo.tlsIndex.addr));
    JITDUMP("tlsGetAddrFtnPtr= %p\n", dspPtr(threadStaticBlocksInfo.tlsGetAddrFtnPtr));
    JITDUMP("tlsIndexObject= %p\n", dspPtr(threadStaticBlocksInfo.tlsIndexObject));
    JITDUMP("threadVarsSection= %p\n", dspPtr(threadStaticBlocksInfo.threadVarsSection));
    JITDUMP("offsetOfThreadLocalStoragePointer= %u\n",
            dspOffset(threadStaticBlocksInfo.offsetOfThreadLocalStoragePointer));
    JITDUMP("offsetOfMaxThreadStaticBlocks= %u\n", dspOffset(threadStaticBlocksInfo.offsetOfMaxThreadStaticBlocks));
    JITDUMP("offsetOfThreadStaticBlocks= %u\n", dspOffset(threadStaticBlocksInfo.offsetOfThreadStaticBlocks));
    JITDUMP("offsetOfGCDataPointer= %u\n", dspOffset(threadStaticBlocksInfo.offsetOfGCDataPointer));

    assert(call->gtArgs.CountArgs() == 1);

    // Split block right before the call tree
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    DebugInfo   debugInfo    = stmt->GetDebugInfo();
    block                    = fgSplitBlockBeforeTree(block, stmt, call, &newFirstStmt, &callUse);
    *pBlock                  = block;
    var_types callType       = call->TypeGet();
    assert(prevBb != nullptr && block != nullptr);

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse.
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

    GenTreeLclVar* threadStaticBlockLcl = nullptr;

    // Grab a temp to store result (it's assigned from either fastPathBb or fallbackBb)
    unsigned threadStaticBlockLclNum         = lvaGrabTemp(true DEBUGARG("TLS field access"));
    lvaTable[threadStaticBlockLclNum].lvType = callType;
    threadStaticBlockLcl                     = gtNewLclvNode(threadStaticBlockLclNum, callType);

    *callUse = gtClone(threadStaticBlockLcl);

    fgMorphStmtBlockOps(block, stmt);
    gtUpdateStmtSideEffects(stmt);

    GenTree* tlsValue                   = nullptr;
    unsigned tlsLclNum                  = lvaGrabTemp(true DEBUGARG("TLS access"));
    lvaTable[tlsLclNum].lvType          = TYP_I_IMPL;
    GenTree* maxThreadStaticBlocksValue = nullptr;
    GenTree* threadStaticBlocksValue    = nullptr;
    GenTree* tlsValueDef                = nullptr;

    if (TargetOS::IsWindows)
    {
        size_t   tlsIndexValue = (size_t)threadStaticBlocksInfo.tlsIndex.addr;
        GenTree* dllRef        = nullptr;
        if (tlsIndexValue != 0)
        {
            dllRef = gtNewIconHandleNode(tlsIndexValue * TARGET_POINTER_SIZE, GTF_ICON_TLS_HDL);
        }

        // Mark this ICON as a TLS_HDL, codegen will use FS:[cns] or GS:[cns]
        tlsValue = gtNewIconHandleNode(threadStaticBlocksInfo.offsetOfThreadLocalStoragePointer, GTF_ICON_TLS_HDL);
        tlsValue = gtNewIndir(TYP_I_IMPL, tlsValue, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

        if (dllRef != nullptr)
        {
            // Add the dllRef to produce thread local storage reference for coreclr
            tlsValue = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsValue, dllRef);
        }

        // Base of coreclr's thread local storage
        tlsValue = gtNewIndir(TYP_I_IMPL, tlsValue, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
    }
    else if (TargetOS::IsApplePlatform)
    {
        // For Apple x64/arm64, we need to get the address of relevant __thread_vars section of
        // the thread local variable `t_ThreadStatics`. Address of `tlv_get_address` is stored
        // in this entry, which we dereference and invoke it, passing the __thread_vars address
        // present in `threadVarsSection`.
        //
        // Code sequence to access thread local variable on Apple/x64:
        //
        //      mov rdi, threadVarsSection
        //      call     [rdi]
        //
        // Code sequence to access thread local variable on Apple/arm64:
        //
        //      mov x0, threadVarsSection
        //      mov x1, [x0]
        //      blr x1
        //
        size_t   threadVarsSectionVal = (size_t)threadStaticBlocksInfo.threadVarsSection;
        GenTree* tls_get_addr_val     = gtNewIconHandleNode(threadVarsSectionVal, GTF_ICON_FTN_ADDR);

        tls_get_addr_val = gtNewIndir(TYP_I_IMPL, tls_get_addr_val, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

        tlsValue                = gtNewIndCallNode(tls_get_addr_val, TYP_I_IMPL);
        GenTreeCall* tlsRefCall = tlsValue->AsCall();

        // This is a call which takes an argument.
        // Populate and set the ABI appropriately.
        assert(opts.altJit || threadVarsSectionVal != 0);
        GenTree* tlsArg = gtNewIconNode(threadVarsSectionVal, TYP_I_IMPL);
        tlsRefCall->gtArgs.PushBack(this, NewCallArg::Primitive(tlsArg));

        fgMorphArgs(tlsRefCall);

        tlsRefCall->gtFlags |= GTF_EXCEPT | (tls_get_addr_val->gtFlags & GTF_GLOB_EFFECT);
    }
    else if (TargetOS::IsUnix)
    {
#if defined(TARGET_AMD64)
        // Code sequence to access thread local variable on linux/x64:
        //
        //      mov      rdi, 0x7FE5C418CD28  ; tlsIndexObject
        //      mov      rax, 0x7FE5C47AFDB0  ; _tls_get_addr
        //      call     rax
        //
        GenTree* tls_get_addr_val =
            gtNewIconHandleNode((size_t)threadStaticBlocksInfo.tlsGetAddrFtnPtr, GTF_ICON_FTN_ADDR);
        tlsValue                = gtNewIndCallNode(tls_get_addr_val, TYP_I_IMPL);
        GenTreeCall* tlsRefCall = tlsValue->AsCall();

        // This is an indirect call which takes an argument.
        // Populate and set the ABI appropriately.
        assert(opts.altJit || threadStaticBlocksInfo.tlsIndexObject != 0);
        GenTree* tlsArg = gtNewIconNode((size_t)threadStaticBlocksInfo.tlsIndexObject, TYP_I_IMPL);
        tlsRefCall->gtArgs.PushBack(this, NewCallArg::Primitive(tlsArg));

        fgMorphArgs(tlsRefCall);

        tlsRefCall->gtFlags |= GTF_EXCEPT | (tls_get_addr_val->gtFlags & GTF_GLOB_EFFECT);
#ifdef UNIX_X86_ABI
        tlsRefCall->gtFlags &= ~GTF_CALL_POP_ARGS;
#endif // UNIX_X86_ABI
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Code sequence to access thread local variable on linux/arm64:
        //
        //      mrs xt, tpidr_elf0
        //      mov xd, [xt+cns]
        //
        // Code sequence to access thread local variable on linux/loongarch64:
        //
        //      ori, targetReg, $tp, 0
        //      load rd, targetReg, cns
        //
        // Code sequence to access thread local variable on linux/riscv64:
        //
        //      mov targetReg, $tp
        //      ld rd, targetReg(cns)
        tlsValue = gtNewIconHandleNode(0, GTF_ICON_TLS_HDL);
#else
        assert(!"Unsupported scenario of optimizing TLS access on Linux Arm32/x86");
#endif
    }

    // Cache the tls value
    tlsValueDef             = gtNewStoreLclVarNode(tlsLclNum, tlsValue);
    GenTree* tlsLclValueUse = gtNewLclVarNode(tlsLclNum);

    size_t offsetOfThreadStaticBlocksVal    = threadStaticBlocksInfo.offsetOfThreadStaticBlocks;
    size_t offsetOfMaxThreadStaticBlocksVal = threadStaticBlocksInfo.offsetOfMaxThreadStaticBlocks;

    // Create tree for "maxThreadStaticBlocks = tls[offsetOfMaxThreadStaticBlocks]"
    GenTree* offsetOfMaxThreadStaticBlocks = gtNewIconNode(offsetOfMaxThreadStaticBlocksVal, TYP_I_IMPL);
    GenTree* maxThreadStaticBlocksRef =
        gtNewOperNode(GT_ADD, TYP_I_IMPL, gtCloneExpr(tlsLclValueUse), offsetOfMaxThreadStaticBlocks);
    maxThreadStaticBlocksValue = gtNewIndir(TYP_INT, maxThreadStaticBlocksRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    GenTree* threadStaticBlocksRef = gtNewOperNode(GT_ADD, TYP_I_IMPL, gtCloneExpr(tlsLclValueUse),
                                                   gtNewIconNode(offsetOfThreadStaticBlocksVal, TYP_I_IMPL));
    threadStaticBlocksValue = gtNewIndir(TYP_I_IMPL, threadStaticBlocksRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    // Create tree for "if (maxThreadStaticBlocks < typeIndex)"
    GenTree* typeThreadStaticBlockIndexValue = call->gtArgs.GetArgByIndex(0)->GetNode();
    GenTree* maxThreadStaticBlocksCond =
        gtNewOperNode(GT_LT, TYP_INT, maxThreadStaticBlocksValue, gtCloneExpr(typeThreadStaticBlockIndexValue));
    maxThreadStaticBlocksCond = gtNewOperNode(GT_JTRUE, TYP_VOID, maxThreadStaticBlocksCond);

    // Create tree to "threadStaticBlockValue = threadStaticBlockBase[typeIndex]"
    typeThreadStaticBlockIndexValue = gtNewOperNode(GT_MUL, TYP_INT, gtCloneExpr(typeThreadStaticBlockIndexValue),
                                                    gtNewIconNode(TARGET_POINTER_SIZE, TYP_INT));
    GenTree* typeThreadStaticBlockRef =
        gtNewOperNode(GT_ADD, TYP_I_IMPL, threadStaticBlocksValue, typeThreadStaticBlockIndexValue);
    GenTree* typeThreadStaticBlockValue = gtNewIndir(TYP_I_IMPL, typeThreadStaticBlockRef, GTF_IND_NONFAULTING);

    // Cache the threadStaticBlock value
    unsigned threadStaticBlockBaseLclNum         = lvaGrabTemp(true DEBUGARG("ThreadStaticBlockBase access"));
    lvaTable[threadStaticBlockBaseLclNum].lvType = TYP_I_IMPL;
    GenTree* threadStaticBlockBaseDef = gtNewStoreLclVarNode(threadStaticBlockBaseLclNum, typeThreadStaticBlockValue);
    GenTree* threadStaticBlockBaseLclValueUse = gtNewLclVarNode(threadStaticBlockBaseLclNum);

    // Create tree for "if (threadStaticBlockValue != nullptr)"
    GenTree* threadStaticBlockNullCond =
        gtNewOperNode(GT_NE, TYP_INT, threadStaticBlockBaseLclValueUse, gtNewIconNode(0, TYP_I_IMPL));
    threadStaticBlockNullCond = gtNewOperNode(GT_JTRUE, TYP_VOID, threadStaticBlockNullCond);

    // prevBb (BBJ_ALWAYS):                                             [weight: 1.0]
    //      ...
    //
    // maxThreadStaticBlocksCondBB (BBJ_COND):                          [weight: 1.0]
    //      tlsValue = tls_access_code
    //      if (maxThreadStaticBlocks < typeIndex)
    //          goto fallbackBb;
    //
    // threadStaticBlockNullCondBB (BBJ_COND):                          [weight: 1.0]
    //      fastPathValue = t_threadStaticBlocks[typeIndex]
    //      if (fastPathValue != nullptr)
    //          goto fastPathBb;
    //
    // fallbackBb (BBJ_ALWAYS):                                         [weight: 0]
    //      threadStaticBlockBase = HelperCall();
    //      goto block;
    //
    // fastPathBb(BBJ_ALWAYS):                                          [weight: 1.0]
    //      threadStaticBlockBase = fastPathValue;
    //
    // block (...):                                                     [weight: 1.0]
    //      use(threadStaticBlockBase);

    // maxThreadStaticBlocksCondBB

    // maxThreadStaticBlocksCondBB conditionally jumps to fallbackBb, but fallbackBb must be initialized last
    // so it can be placed after it. So set the jump target later.
    BasicBlock* maxThreadStaticBlocksCondBB = fgNewBBFromTreeAfter(BBJ_COND, prevBb, tlsValueDef, debugInfo);

    fgInsertStmtAfter(maxThreadStaticBlocksCondBB, maxThreadStaticBlocksCondBB->firstStmt(),
                      fgNewStmtFromTree(maxThreadStaticBlocksCond));

    // Similarly, set threadStaticBlockNulLCondBB to jump to fastPathBb once the latter exists.
    BasicBlock* threadStaticBlockNullCondBB =
        fgNewBBFromTreeAfter(BBJ_COND, maxThreadStaticBlocksCondBB, threadStaticBlockBaseDef, debugInfo);
    fgInsertStmtAfter(threadStaticBlockNullCondBB, threadStaticBlockNullCondBB->firstStmt(),
                      fgNewStmtFromTree(threadStaticBlockNullCond));

    // fallbackBb
    GenTree*    fallbackValueDef = gtNewStoreLclVarNode(threadStaticBlockLclNum, call);
    BasicBlock* fallbackBb =
        fgNewBBFromTreeAfter(BBJ_ALWAYS, threadStaticBlockNullCondBB, fallbackValueDef, debugInfo, block, true);

    // fastPathBb
    if (isGCThreadStatic)
    {
        // Need to add extra indirection to access the data pointer.

        threadStaticBlockBaseLclValueUse = gtNewIndir(callType, threadStaticBlockBaseLclValueUse, GTF_IND_NONFAULTING);
        threadStaticBlockBaseLclValueUse =
            gtNewOperNode(GT_ADD, callType, threadStaticBlockBaseLclValueUse,
                          gtNewIconNode(threadStaticBlocksInfo.offsetOfGCDataPointer, TYP_I_IMPL));
    }

    GenTree* fastPathValueDef =
        gtNewStoreLclVarNode(threadStaticBlockLclNum, gtCloneExpr(threadStaticBlockBaseLclValueUse));
    BasicBlock* fastPathBb = fgNewBBFromTreeAfter(BBJ_ALWAYS, fallbackBb, fastPathValueDef, debugInfo, block, true);

    // Set maxThreadStaticBlocksCondBB's jump targets
    maxThreadStaticBlocksCondBB->SetTrueTarget(fallbackBb);
    maxThreadStaticBlocksCondBB->SetFalseTarget(threadStaticBlockNullCondBB);

    // Set threadStaticBlockNullCondBB's jump targets
    threadStaticBlockNullCondBB->SetTrueTarget(fastPathBb);
    threadStaticBlockNullCondBB->SetFalseTarget(fallbackBb);

    //
    // Update preds in all new blocks
    //
    assert(prevBb->KindIs(BBJ_ALWAYS));
    prevBb->SetTarget(maxThreadStaticBlocksCondBB);
    fgRemoveRefPred(block, prevBb);
    fgAddRefPred(maxThreadStaticBlocksCondBB, prevBb);

    fgAddRefPred(threadStaticBlockNullCondBB, maxThreadStaticBlocksCondBB);
    fgAddRefPred(fallbackBb, maxThreadStaticBlocksCondBB);

    fgAddRefPred(fastPathBb, threadStaticBlockNullCondBB);
    fgAddRefPred(fallbackBb, threadStaticBlockNullCondBB);

    fgAddRefPred(block, fastPathBb);
    fgAddRefPred(block, fallbackBb);

    // Inherit the weights
    block->inheritWeight(prevBb);
    maxThreadStaticBlocksCondBB->inheritWeight(prevBb);
    threadStaticBlockNullCondBB->inheritWeight(prevBb);
    fastPathBb->inheritWeight(prevBb);

    // fallback will just execute first time
    fallbackBb->bbSetRunRarely();

    // All blocks are expected to be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, block));
    assert(BasicBlock::sameEHRegion(prevBb, maxThreadStaticBlocksCondBB));
    assert(BasicBlock::sameEHRegion(prevBb, threadStaticBlockNullCondBB));
    assert(BasicBlock::sameEHRegion(prevBb, fastPathBb));

    return true;
}

//------------------------------------------------------------------------------
// fgExpandHelper: Expand the helper using ExpansionFunction.
//
// Returns:
//    true if there was any helper that was expanded.
//
template <bool (Compiler::*ExpansionFunction)(BasicBlock**, Statement*, GenTreeCall*)>
PhaseStatus Compiler::fgExpandHelper(bool skipRarelyRunBlocks)
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->Next())
    {
        if (skipRarelyRunBlocks && block->isRunRarely())
        {
            // It's just an optimization - don't waste time on rarely executed blocks
            continue;
        }

        // Expand and visit the last block again to find more candidates
        INDEBUG(BasicBlock* origBlock = block);
        while (fgExpandHelperForBlock<ExpansionFunction>(&block))
        {
            result = PhaseStatus::MODIFIED_EVERYTHING;
#ifdef DEBUG
            assert(origBlock != block);
            origBlock = block;
#endif
        }
    }

    if ((result == PhaseStatus::MODIFIED_EVERYTHING) && opts.OptimizationEnabled())
    {
        fgRenumberBlocks();
    }

    return result;
}

//------------------------------------------------------------------------------
// fgExpandHelperForBlock: Scans through all the statements of the `block` and
//    invoke `fgExpand` if any of the tree node was a helper call.
//
// Arguments:
//    pBlock   - Block containing the helper call to expand. If expansion is performed,
//               this is updated to the new block that was an outcome of block splitting.
//    fgExpand - function that expands the helper call
//
// Returns:
//    true if a helper was expanded
//
template <bool (Compiler::*ExpansionFunction)(BasicBlock**, Statement*, GenTreeCall*)>
bool Compiler::fgExpandHelperForBlock(BasicBlock** pBlock)
{
    for (Statement* const stmt : (*pBlock)->NonPhiStatements())
    {
        if ((stmt->GetRootNode()->gtFlags & GTF_CALL) == 0)
        {
            // TP: Stmt has no calls - bail out
            continue;
        }

        for (GenTree* const tree : stmt->TreeList())
        {
            if (!tree->IsCall())
            {
                continue;
            }

            if ((this->*ExpansionFunction)(pBlock, stmt, tree->AsCall()))
            {
                return true;
            }
        }
    }
    return false;
}

//------------------------------------------------------------------------------
// fgExpandStaticInit: Partially expand static initialization calls, e.g.:
//
//    tmp = CORINFO_HELP_X_NONGCSTATIC_BASE();
//
// into:
//
//    if (isClassAlreadyInited)
//        CORINFO_HELP_X_NONGCSTATIC_BASE();
//    tmp = fastPath;
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgExpandStaticInit()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if (!doesMethodHaveStaticInit())
    {
        // TP: nothing to expand in the current method
        JITDUMP("Nothing to expand.\n")
        return result;
    }

    // Always expand for NativeAOT, see
    // https://github.com/dotnet/runtime/issues/68278#issuecomment-1543322819
    const bool isNativeAOT = IsTargetAbi(CORINFO_NATIVEAOT_ABI);

    if (!isNativeAOT && opts.OptimizationDisabled())
    {
        JITDUMP("Optimizations aren't allowed - bail out.\n")
        return result;
    }

    return fgExpandHelper<&Compiler::fgExpandStaticInitForCall>(/*skipRarelyRunBlocks*/ !isNativeAOT);
}

//------------------------------------------------------------------------------
// fgExpandStaticInitForCall: Partially expand given static initialization call.
//    Also, see fgExpandStaticInit's comments.
//
// Arguments:
//    pBlock - Block containing the helper call to expand. If expansion is performed,
//             this is updated to the new block that was an outcome of block splitting.
//    stmt   - Statement containing the helper call
//    call   - The helper call
//
// Returns:
//    true if a static initialization was expanded
//
bool Compiler::fgExpandStaticInitForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    BasicBlock* block = *pBlock;
    if (!call->IsHelperCall())
    {
        return false;
    }

    bool                    isGc       = false;
    StaticHelperReturnValue retValKind = {};
    if (!IsStaticHelperEligibleForExpansion(call, &isGc, &retValKind))
    {
        return false;
    }

    assert(!call->IsTailCall());

    if (call->gtInitClsHnd == NO_CLASS_HANDLE)
    {
        assert(!"helper call was created without gtInitClsHnd or already visited");
        return false;
    }

    int                  isInitOffset = 0;
    CORINFO_CONST_LOOKUP flagAddr     = {};
    if (!info.compCompHnd->getIsClassInitedFlagAddress(call->gtInitClsHnd, &flagAddr, &isInitOffset))
    {
        JITDUMP("getIsClassInitedFlagAddress returned false - bail out.\n")
        return false;
    }

    CORINFO_CONST_LOOKUP staticBaseAddr = {};
    if ((retValKind == SHRV_STATIC_BASE_PTR) &&
        !info.compCompHnd->getStaticBaseAddress(call->gtInitClsHnd, isGc, &staticBaseAddr))
    {
        JITDUMP("getStaticBaseAddress returned false - bail out.\n")
        return false;
    }

    JITDUMP("Expanding static initialization for '%s', call: [%06d] in " FMT_BB "\n",
            eeGetClassName(call->gtInitClsHnd), dspTreeID(call), block->bbNum);

    DebugInfo debugInfo = stmt->GetDebugInfo();

    // Split block right before the call tree
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    block                    = fgSplitBlockBeforeTree(block, stmt, call, &newFirstStmt, &callUse);
    *pBlock                  = block;
    assert(prevBb != nullptr && block != nullptr);

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse.
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

    //
    // Create new blocks. Essentially, we want to transform this:
    //
    //   staticBase = helperCall();
    //
    // into:
    //
    //   if (!isInitialized)
    //   {
    //       helperCall(); // we don't use its return value
    //   }
    //   staticBase = fastPath;
    //

    // The initialization check looks like this for JIT:
    //
    // *  JTRUE     void
    // \--*  EQ        int
    //    +--*  AND       int
    //    |  +--*  IND       int
    //    |  |  \--*  CNS_INT(h) long   0x.... const ptr
    //    |  \--*  CNS_INT   int    1 (bit mask)
    //    \--*  CNS_INT   int    1
    //
    // For NativeAOT it's:
    //
    // *  JTRUE     void
    // \--*  EQ        int
    //    +--*  IND       nint
    //    |  \--*  ADD       long
    //    |     +--*  CNS_INT(h) long   0x.... const ptr
    //    |     \--*  CNS_INT   int    -8 (offset)
    //    \--*  CNS_INT   int    0
    //
    assert(flagAddr.accessType == IAT_VALUE);

    GenTree* cachedStaticBase = nullptr;
    GenTree* isInitedActualValueNode;
    GenTree* isInitedExpectedValue;
    if (IsTargetAbi(CORINFO_NATIVEAOT_ABI))
    {
        GenTree* baseAddr = gtNewIconHandleNode((size_t)flagAddr.addr, GTF_ICON_GLOBAL_PTR);

        // Save it to a temp - we'll be using its value for the replacementNode.
        // This leads to some size savings on NativeAOT
        if ((staticBaseAddr.addr == flagAddr.addr) && (staticBaseAddr.accessType == flagAddr.accessType))
        {
            cachedStaticBase = fgInsertCommaFormTemp(&baseAddr);
        }

        // Don't fold ADD(CNS1, CNS2) here since the result won't be reloc-friendly for AOT
        GenTree* offsetNode     = gtNewOperNode(GT_ADD, TYP_I_IMPL, baseAddr, gtNewIconNode(isInitOffset));
        isInitedActualValueNode = gtNewIndir(TYP_I_IMPL, offsetNode, GTF_IND_NONFAULTING);

        // 0 means "initialized" on NativeAOT
        isInitedExpectedValue = gtNewIconNode(0, TYP_I_IMPL);
    }
    else
    {
        assert(isInitOffset == 0);

        isInitedActualValueNode = gtNewIndOfIconHandleNode(TYP_INT, (size_t)flagAddr.addr, GTF_ICON_GLOBAL_PTR, false);

        // Check ClassInitFlags::INITIALIZED_FLAG bit
        isInitedActualValueNode = gtNewOperNode(GT_AND, TYP_INT, isInitedActualValueNode, gtNewIconNode(1));
        isInitedExpectedValue   = gtNewIconNode(1);
    }

    GenTree* isInitedCmp = gtNewOperNode(GT_EQ, TYP_INT, isInitedActualValueNode, isInitedExpectedValue);
    isInitedCmp->gtFlags |= GTF_RELOP_JMP_USED;
    BasicBlock* isInitedBb =
        fgNewBBFromTreeAfter(BBJ_COND, prevBb, gtNewOperNode(GT_JTRUE, TYP_VOID, isInitedCmp), debugInfo, block);

    // Fallback basic block
    // TODO-CQ: for JIT we can replace the original call with CORINFO_HELP_INITCLASS
    // that only accepts a single argument
    BasicBlock* helperCallBb = fgNewBBFromTreeAfter(BBJ_ALWAYS, isInitedBb, call, debugInfo, isInitedBb->Next(), true);
    assert(helperCallBb->JumpsToNext());
    helperCallBb->SetFlags(BBF_NONE_QUIRK);

    GenTree* replacementNode = nullptr;
    if (retValKind == SHRV_STATIC_BASE_PTR)
    {
        // Replace the call with a constant pointer to the statics base
        assert(staticBaseAddr.addr != nullptr);

        // Use local if the addressed is already materialized and cached
        if (cachedStaticBase != nullptr)
        {
            assert(staticBaseAddr.accessType == IAT_VALUE);
            replacementNode = cachedStaticBase;
        }
        else if (staticBaseAddr.accessType == IAT_VALUE)
        {
            replacementNode = gtNewIconHandleNode((size_t)staticBaseAddr.addr, GTF_ICON_STATIC_HDL);
        }
        else
        {
            assert(staticBaseAddr.accessType == IAT_PVALUE);
            replacementNode =
                gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)staticBaseAddr.addr, GTF_ICON_GLOBAL_PTR, false);
        }
    }

    if (replacementNode == nullptr)
    {
        (*callUse)->gtBashToNOP();
    }
    else
    {
        *callUse = replacementNode;
    }

    fgMorphStmtBlockOps(block, stmt);
    gtUpdateStmtSideEffects(stmt);

    // Final block layout looks like this:
    //
    // prevBb(BBJ_ALWAYS):                  [weight: 1.0]
    //     ...
    //
    // isInitedBb(BBJ_COND):                [weight: 1.0]
    //     if (isInited)
    //         goto block;
    //
    // helperCallBb(BBJ_ALWAYS):            [weight: 0.0]
    //     helperCall();
    //
    // block(...):                          [weight: 1.0]
    //     use(staticBase);
    //
    // Whether we use helperCall's value or not depends on the helper itself.

    //
    // Update preds in all new blocks
    //

    // Unlink block and prevBb
    fgRemoveRefPred(block, prevBb);

    // Block has two preds now: either isInitedBb or helperCallBb
    fgAddRefPred(block, isInitedBb);
    fgAddRefPred(block, helperCallBb);

    // prevBb always flows into isInitedBb
    assert(prevBb->KindIs(BBJ_ALWAYS));
    prevBb->SetTarget(isInitedBb);
    prevBb->SetFlags(BBF_NONE_QUIRK);
    assert(prevBb->JumpsToNext());
    fgAddRefPred(isInitedBb, prevBb);

    // Both fastPathBb and helperCallBb have a single common pred - isInitedBb
    isInitedBb->SetFalseTarget(helperCallBb);
    fgAddRefPred(helperCallBb, isInitedBb);

    //
    // Re-distribute weights
    //

    block->inheritWeight(prevBb);
    isInitedBb->inheritWeight(prevBb);
    helperCallBb->bbSetRunRarely();

    // All blocks are expected to be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, block));
    assert(BasicBlock::sameEHRegion(prevBb, isInitedBb));

    // Extra step: merge prevBb with isInitedBb if possible
    if (fgCanCompactBlocks(prevBb, isInitedBb))
    {
        fgCompactBlocks(prevBb, isInitedBb);
    }

    // Clear gtInitClsHnd as a mark that we've already visited this call
    call->gtInitClsHnd = NO_CLASS_HANDLE;
    return true;
}

//------------------------------------------------------------------------------
// fgVNBasedIntrinsicExpansion: Expand specific calls marked as intrinsics using VN.
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgVNBasedIntrinsicExpansion()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if (!doesMethodHaveSpecialIntrinsics() || opts.OptimizationDisabled())
    {
        return result;
    }

    // TODO: Replace with opts.compCodeOpt once it's fixed
    const bool preferSize = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT);
    if (preferSize)
    {
        // The optimization comes with a codegen size increase
        JITDUMP("Optimized for size - bail out.\n")
        return result;
    }
    return fgExpandHelper<&Compiler::fgVNBasedIntrinsicExpansionForCall>(true);
}

//------------------------------------------------------------------------------
// fgVNBasedIntrinsicExpansionForCall : Expand specific calls marked as intrinsics using VN.
//
// Arguments:
//    block - Block containing the intrinsic call to expand
//    stmt  - Statement containing the call
//    call  - The intrinsic call
//
// Returns:
//    True if expanded, false otherwise.
//
bool Compiler::fgVNBasedIntrinsicExpansionForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) == 0)
    {
        return false;
    }

    NamedIntrinsic ni = lookupNamedIntrinsic(call->gtCallMethHnd);
    if (ni == NI_System_Text_UTF8Encoding_UTF8EncodingSealed_ReadUtf8)
    {
        return fgVNBasedIntrinsicExpansionForCall_ReadUtf8(pBlock, stmt, call);
    }

    // TODO: Expand IsKnownConstant here
    // Also, move various unrollings here

    return false;
}

//------------------------------------------------------------------------------
// fgVNBasedIntrinsicExpansionForCall_ReadUtf8 : Expand NI_System_Text_UTF8Encoding_UTF8EncodingSealed_ReadUtf8
//    when src data is a string literal (UTF16) that can be converted to UTF8, e.g.:
//
//      string str = "Hello, world!";
//      int bytesWritten = ReadUtf8(ref str[0], str.Length, buffer, buffer.Length);
//
//    becomes:
//
//      bytesWritten = 0; // default value
//      if (buffer.Length >= 13)
//      {
//          memcpy(buffer, "Hello, world!"u8, 13); // note the u8 suffix
//          bytesWritten = 13;
//      }
//
// Arguments:
//    block - Block containing the intrinsic call to expand
//    stmt  - Statement containing the call
//    call  - The intrinsic call
//
// Returns:
//    True if expanded, false otherwise.
//
bool Compiler::fgVNBasedIntrinsicExpansionForCall_ReadUtf8(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    BasicBlock* block = *pBlock;

    assert(call->gtArgs.CountUserArgs() == 4);

    GenTree* srcPtr = call->gtArgs.GetUserArgByIndex(0)->GetNode();

    // We're interested in a case when srcPtr is a string literal and srcLen is a constant
    // srcLen doesn't have to match the srcPtr's length, but it should not exceed it.
    ssize_t               strObjOffset = 0;
    CORINFO_OBJECT_HANDLE strObj       = nullptr;
    if (!GetObjectHandleAndOffset(srcPtr, &strObjOffset, &strObj) || ((size_t)strObjOffset > INT_MAX))
    {
        // We might want to support more cases here, e.g. ROS<char> RVA data.
        // Also, we check that strObjOffset (which is in most cases is expected to be just
        // OFFSETOF__CORINFO_String__chars) doesn't exceed INT_MAX since we'll need to cast
        // it to int for getObjectContent API.
        JITDUMP("ReadUtf8: srcPtr is not an object handle\n")
        return false;
    }

    assert(strObj != nullptr);

    // We mostly expect string literal objects here, but let's be more agile just in case
    if (!info.compCompHnd->isObjectImmutable(strObj))
    {
        JITDUMP("ReadUtf8: srcPtr is not immutable (not a frozen string object?)\n")
        return false;
    }

    const GenTree* srcLen = call->gtArgs.GetUserArgByIndex(1)->GetNode();
    if (!srcLen->gtVNPair.BothEqual() || !vnStore->IsVNInt32Constant(srcLen->gtVNPair.GetLiberal()))
    {
        JITDUMP("ReadUtf8: srcLen is not constant\n")
        return false;
    }

    // Source UTF16 (U16) string length in characters
    const unsigned srcLenCnsU16            = (unsigned)vnStore->GetConstantInt32(srcLen->gtVNPair.GetLiberal());
    const int      MaxU16BufferSizeInChars = 256;
    if ((srcLenCnsU16 == 0) || (srcLenCnsU16 > MaxU16BufferSizeInChars))
    {
        // TODO: handle srcLenCns == 0 if it's a common case
        JITDUMP("ReadUtf8: srcLenCns is 0 or > MaxPossibleUnrollThreshold\n")
        return false;
    }

    uint16_t bufferU16[MaxU16BufferSizeInChars];

    // getObjectContent is expected to validate the offset and length
    // NOTE: (int) casts should not overflow:
    //  * srcLenCns is <= MaxUTF16BufferSizeInChars
    //  * strObjOffset is already checked to be <= INT_MAX
    if (!info.compCompHnd->getObjectContent(strObj, (uint8_t*)bufferU16, (int)(srcLenCnsU16 * sizeof(uint16_t)),
                                            (int)strObjOffset))
    {
        JITDUMP("ReadUtf8: getObjectContent returned false.\n")
        return false;
    }

    const int MaxU8BufferSizeInBytes = 256;
    uint8_t   bufferU8[MaxU8BufferSizeInBytes];

    const int srcLenU8 = (int)minipal_convert_utf16_to_utf8((const CHAR16_T*)bufferU16, srcLenCnsU16, (char*)bufferU8,
                                                            MaxU8BufferSizeInBytes, 0);
    if (srcLenU8 <= 0)
    {
        // E.g. output buffer is too small
        JITDUMP("ReadUtf8: minipal_convert_utf16_to_utf8 returned <= 0\n")
        return false;
    }

    // The API is expected to return [1..MaxU8BufferSizeInBytes] real length of the UTF-8 value
    // stored in bufferU8
    assert((unsigned)srcLenU8 <= MaxU8BufferSizeInBytes);

    // Now that we know the exact UTF8 buffer length we can check if it's unrollable
    if (srcLenU8 > (int)getUnrollThreshold(UnrollKind::Memcpy))
    {
        JITDUMP("ReadUtf8: srcLenU8 is out of unrollable range\n")
        return false;
    }

    DebugInfo debugInfo = stmt->GetDebugInfo();

    // Split block right before the call tree (this is a standard pattern we use in helperexpansion.cpp)
    BasicBlock* prevBb       = block;
    GenTree**   callUse      = nullptr;
    Statement*  newFirstStmt = nullptr;
    block                    = fgSplitBlockBeforeTree(block, stmt, call, &newFirstStmt, &callUse);
    assert(prevBb != nullptr && block != nullptr);
    *pBlock = block;

    // If we suddenly need to use these arguments, we'll have to reload them from the call
    // after the split, so let's null them to prevent accidental use.
    srcLen = nullptr;
    srcPtr = nullptr;

    // Block ops inserted by the split need to be morphed here since we are after morph.
    // We cannot morph stmt yet as we may modify it further below, and the morphing
    // could invalidate callUse
    while ((newFirstStmt != nullptr) && (newFirstStmt != stmt))
    {
        fgMorphStmtBlockOps(block, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }

    // We don't need this flag anymore.
    call->gtCallMoreFlags &= ~GTF_CALL_M_SPECIAL_INTRINSIC;

    // Grab a temp to store the result.
    // The result corresponds the number of bytes written to dstPtr (int32).
    assert(call->TypeIs(TYP_INT));
    const unsigned resultLclNum   = lvaGrabTemp(true DEBUGARG("local for result"));
    lvaTable[resultLclNum].lvType = TYP_INT;
    *callUse                      = gtNewLclvNode(resultLclNum, TYP_INT);
    fgMorphStmtBlockOps(block, stmt);
    gtUpdateStmtSideEffects(stmt);

    // srcLenU8 is the length of the string literal in chars (UTF16)
    // but we're going to use the same value as the "bytesWritten" result in the fast path and in the length check.
    GenTree* srcLenU8Node = gtNewIconNode(srcLenU8);
    fgValueNumberTreeConst(srcLenU8Node);

    // We're going to insert the following blocks:
    //
    //  prevBb:
    //
    //  lengthCheckBb:
    //      bytesWritten = -1;
    //      if (dstLen < srcLenU8)
    //          goto block;
    //
    //  fastpathBb:
    //      <unrolled block copy>
    //      bytesWritten = srcLenU8;
    //
    //  block:
    //      use(bytesWritten)
    //

    //
    // Block 1: lengthCheckBb (we check that dstLen < srcLen)
    //
    BasicBlock* lengthCheckBb = fgNewBBafter(BBJ_COND, prevBb, true, block);
    lengthCheckBb->SetFlags(BBF_INTERNAL);

    // Set bytesWritten -1 by default, if the fast path is not taken we'll return it as the result.
    GenTree* bytesWrittenDefaultVal = gtNewStoreLclVarNode(resultLclNum, gtNewIconNode(-1));
    fgInsertStmtAtEnd(lengthCheckBb, fgNewStmtFromTree(bytesWrittenDefaultVal, debugInfo));

    GenTree* dstLen      = call->gtArgs.GetUserArgByIndex(3)->GetNode();
    GenTree* lengthCheck = gtNewOperNode(GT_LT, TYP_INT, gtCloneExpr(dstLen), srcLenU8Node);
    lengthCheck->gtFlags |= GTF_RELOP_JMP_USED;
    Statement* lengthCheckStmt = fgNewStmtFromTree(gtNewOperNode(GT_JTRUE, TYP_VOID, lengthCheck), debugInfo);
    fgInsertStmtAtEnd(lengthCheckBb, lengthCheckStmt);
    lengthCheckBb->bbCodeOffs    = block->bbCodeOffsEnd;
    lengthCheckBb->bbCodeOffsEnd = block->bbCodeOffsEnd;

    //
    // Block 2: fastpathBb - unrolled loop that copies the UTF8 const data to the destination
    //
    // We're going to emit a series of loads and stores to copy the data.
    // In theory, we could just emit the const U8 data to the data section and use GT_BLK here
    // but that would be a bit less efficient since we would have to load the data from memory.
    //
    BasicBlock* fastpathBb = fgNewBBafter(BBJ_ALWAYS, lengthCheckBb, true, lengthCheckBb->Next());
    assert(fastpathBb->JumpsToNext());
    fastpathBb->SetFlags(BBF_INTERNAL | BBF_NONE_QUIRK);

    // The widest type we can use for loads
    const var_types maxLoadType = roundDownMaxType(srcLenU8);
    assert(genTypeSize(maxLoadType) > 0);

    // How many iterations we need to copy UTF8 const data to the destination
    unsigned iterations = srcLenU8 / genTypeSize(maxLoadType);

    // Add one more iteration if we have a remainder
    iterations += (srcLenU8 % genTypeSize(maxLoadType) == 0) ? 0 : 1;

    GenTree* dstPtr = call->gtArgs.GetUserArgByIndex(2)->GetNode();
    for (unsigned i = 0; i < iterations; i++)
    {
        ssize_t offset = (ssize_t)i * genTypeSize(maxLoadType);

        // Last iteration: overlap with previous load if needed
        if (i == iterations - 1)
        {
            offset = (ssize_t)srcLenU8 - genTypeSize(maxLoadType);
        }

        // We're going to emit the following tree (in case of SIMD16 load):
        //
        // -A-XG------         *  STOREIND  simd16 (copy)
        // -------N---         +--*  ADD       byref
        // -----------         |  +--*  LCL_VAR   byref
        // -----------         |  \--*  CNS_INT   int
        // -----------         \--*  CNS_VEC   simd16

        GenTreeIntCon* offsetNode = gtNewIconNode(offset, TYP_I_IMPL);
        fgValueNumberTreeConst(offsetNode);

        // Grab a chunk from srcUtf8cnsData for the given offset and width
        GenTree* utf8cnsChunkNode = gtNewGenericCon(maxLoadType, bufferU8 + offset);
        fgValueNumberTreeConst(utf8cnsChunkNode);

        GenTree*   dstAddOffsetNode = gtNewOperNode(GT_ADD, dstPtr->TypeGet(), gtCloneExpr(dstPtr), offsetNode);
        GenTreeOp* storeInd         = gtNewStoreIndNode(maxLoadType, dstAddOffsetNode, utf8cnsChunkNode);
        fgInsertStmtAtEnd(fastpathBb, fgNewStmtFromTree(storeInd, debugInfo));
    }

    // Finally, store the number of bytes written to the resultLcl local
    Statement* finalStmt = fgNewStmtFromTree(gtNewStoreLclVarNode(resultLclNum, gtCloneExpr(srcLenU8Node)), debugInfo);
    fgInsertStmtAtEnd(fastpathBb, finalStmt);
    fastpathBb->bbCodeOffs    = block->bbCodeOffsEnd;
    fastpathBb->bbCodeOffsEnd = block->bbCodeOffsEnd;

    //
    // Update preds in all new blocks
    //
    // block is no longer a predecessor of prevBb
    fgRemoveRefPred(block, prevBb);
    // prevBb flows into lengthCheckBb
    assert(prevBb->KindIs(BBJ_ALWAYS));
    prevBb->SetTarget(lengthCheckBb);
    prevBb->SetFlags(BBF_NONE_QUIRK);
    assert(prevBb->JumpsToNext());
    fgAddRefPred(lengthCheckBb, prevBb);
    // lengthCheckBb has two successors: block and fastpathBb
    lengthCheckBb->SetFalseTarget(fastpathBb);
    fgAddRefPred(fastpathBb, lengthCheckBb);
    fgAddRefPred(block, lengthCheckBb);
    // fastpathBb flows into block
    fgAddRefPred(block, fastpathBb);

    //
    // Re-distribute weights
    //
    lengthCheckBb->inheritWeight(prevBb);
    fastpathBb->inheritWeight(lengthCheckBb);
    block->inheritWeight(prevBb);

    // All blocks are expected to be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, block));
    assert(BasicBlock::sameEHRegion(prevBb, lengthCheckBb));
    assert(BasicBlock::sameEHRegion(prevBb, fastpathBb));

    // Extra step: merge prevBb with lengthCheckBb if possible
    if (fgCanCompactBlocks(prevBb, lengthCheckBb))
    {
        fgCompactBlocks(prevBb, lengthCheckBb);
    }

    JITDUMP("ReadUtf8: succesfully expanded!\n")
    return true;
}

//------------------------------------------------------------------------------
// fgLateCastExpansion: Partially inline various cast helpers, e.g.:
//
//    tmp = CORINFO_HELP_ISINSTANCEOFINTERFACE(clsHandle, obj);
//
// into:
//
//    tmp = obj;
//    if ((obj != null) && (obj->pMT != likelyClassHandle))
//    {
//        tmp = CORINFO_HELP_ISINSTANCEOFINTERFACE(clsHandle, obj);
//    }
//
// The goal is to move cast expansion logic from the importer to this phase, for now,
// this phase only supports "isinst" and for profiled casts only.
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgLateCastExpansion()
{
    if (!doesMethodHaveExpandableCasts())
    {
        // Nothing to expand in the current method
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!opts.IsOptimizedWithProfile())
    {
        // Currently, we're only interested in expanding cast helpers using profile data
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (JitConfig.JitConsumeProfileForCasts() == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    const bool preferSize = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT);
    if (preferSize)
    {
        // The optimization comes with a codegen size increase
        JITDUMP("Optimized for size - bail out.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // TODO-InlineCast: should we still inline some trivial cases even in cold blocks?
    const bool skipForRarelyRunBlocks = true;
    return fgExpandHelper<&Compiler::fgLateCastExpansionForCall>(skipForRarelyRunBlocks);
}

enum class TypeCheckFailedAction
{
    ReturnNull,
    CallHelper,
    CallHelper_AlwaysThrows
};

enum class TypeCheckPassedAction
{
    ReturnObj,
    ReturnNull,
};

//------------------------------------------------------------------------------
// PickCandidateForTypeCheck: picks a class to use as a fast type check against
//    the object being casted. The function also defines the strategy to follow
//    if the type check fails or passes.
//
// Arguments:
//    comp               - Compiler instance
//    castHelper         - Cast helper call to expand
//    commonCls          - [out] Common denominator class for the fast and the fallback paths.
//    likelihood         - [out] Likelihood of successful type check [0..100]
//    typeCheckFailed    - [out] Action to perform if the type check fails
//    typeCheckPassed    - [out] Action to perform if the type check passes
//
// Returns:
//    Likely class handle or NO_CLASS_HANDLE
//
static CORINFO_CLASS_HANDLE PickCandidateForTypeCheck(Compiler*              comp,
                                                      GenTreeCall*           castHelper,
                                                      CORINFO_CLASS_HANDLE*  commonCls,
                                                      unsigned*              likelihood,
                                                      TypeCheckFailedAction* typeCheckFailed,
                                                      TypeCheckPassedAction* typeCheckPassed)
{
    if (!castHelper->IsHelperCall() || ((castHelper->gtCallMoreFlags & GTF_CALL_M_CAST_CAN_BE_EXPANDED) == 0))
    {
        // It's not eligible for expansion (already expanded in importer)
        // To be removed once we move cast expansion here completely.
        return NO_CLASS_HANDLE;
    }

    // Helper calls are never tail calls
    assert(!castHelper->IsTailCall());

    // is it "castclass" or "isinst"?
    bool isCastClass;

    const unsigned helper = castHelper->GetHelperNum();
    switch (helper)
    {
        case CORINFO_HELP_CHKCASTARRAY:
        case CORINFO_HELP_CHKCASTANY:
        case CORINFO_HELP_CHKCASTINTERFACE:
        case CORINFO_HELP_CHKCASTCLASS:
            isCastClass = true;
            break;

        case CORINFO_HELP_ISINSTANCEOFARRAY:
        case CORINFO_HELP_ISINSTANCEOFCLASS:
        case CORINFO_HELP_ISINSTANCEOFANY:
        case CORINFO_HELP_ISINSTANCEOFINTERFACE:
            isCastClass = false;
            break;

        // These are never expanded:
        // CORINFO_HELP_ISINSTANCEOF_EXCEPTION
        // CORINFO_HELP_CHKCASTCLASS_SPECIAL
        // CORINFO_HELP_READYTORUN_ISINSTANCEOF,
        // CORINFO_HELP_READYTORUN_CHKCAST,

        // Other helper calls are not cast helpers

        default:
            return NO_CLASS_HANDLE;
    }

    // result is the class we're going to use as a guess for the type check.
    CORINFO_CLASS_HANDLE result = NO_CLASS_HANDLE;

    // First, let's grab the expected class we're casting to/checking instance of:
    // E.g. "call CORINFO_HELP_ISINSTANCEOFCLASS(castToCls, obj)"
    GenTree*             clsArg    = castHelper->gtArgs.GetUserArgByIndex(0)->GetNode();
    CORINFO_CLASS_HANDLE castToCls = comp->gtGetHelperArgClassHandle(clsArg);
    if (castToCls == NO_CLASS_HANDLE)
    {
        // clsArg doesn't represent a class handle - bail out
        // TODO-InlineCast: if CSE becomes a problem - move the whole phase after assertion prop,
        // so we can still rely on VN to get the class handle.
        JITDUMP("clsArg is not a constant handle - bail out.\n");
        return NO_CLASS_HANDLE;
    }

    // Assume that the type check will pass with 50% probability by default
    *likelihood = 50;

    // Assume that in the slow path (fallback) we'll always invoke the helper.
    // In some cases we can optimize this further e.g. either mark it additionally
    // as no-return (BBJ_THROW) or simply return null.
    *typeCheckFailed = TypeCheckFailedAction::CallHelper;

    // A common denominator class for the fast and the fallback paths
    // can be used as a class for LCL_VAR storing the result of the expansion.
    *commonCls = castToCls;

    //
    // Now we need to figure out what class to use for the fast path, we have 4 options:
    //  1) If "cast to" class is already exact we can go ahead and make some decisions
    //  2) If VM can tell us the exact class for this class/interface via getExactClasses - use it
    //     e.g. NativeAOT can promise us that for e.g. "foo is IMyInterface" foo can only ever be
    //     MyImpl and no other implementation of IMyInterface can be loaded dynamically.
    //  3) If we have PGO data and there is a dominating candidate - use it.
    //  4) Try to speculate and make optimistic guesses
    //

    // 1) If "cast to" class is already exact we can go ahead and make some decisions
    const bool isCastToExact = comp->info.compCompHnd->isExactType(castToCls);
    if (isCastToExact && ((helper == CORINFO_HELP_CHKCASTCLASS) || (helper == CORINFO_HELP_CHKCASTARRAY)))
    {
        // (string)obj
        // (string[])obj
        //
        // Fallbacks for these expansions always throw InvalidCastException
        *typeCheckFailed = TypeCheckFailedAction::CallHelper_AlwaysThrows;

        // Assume that exceptions are rare
        *likelihood = 100;

        // We're done, there is no need in consulting with PGO data
    }
    else if (isCastToExact &&
             ((helper == CORINFO_HELP_ISINSTANCEOFARRAY) || (helper == CORINFO_HELP_ISINSTANCEOFCLASS)))
    {
        // obj is string
        // obj is string[]
        //
        // Fallbacks for these expansions simply return null
        *typeCheckFailed = TypeCheckFailedAction::ReturnNull;
    }
    else
    {
        // 2) If VM can tell us the exact class for this "cast to" class - use it.
        // Just make sure the class is truly exact.
        if ((comp->info.compCompHnd->getExactClasses(castToCls, 1, &result) == 1) &&
            comp->info.compCompHnd->isExactType(result))
        {
            if (isCastClass)
            {
                // Fallback call is only needed for castclass and only to throw InvalidCastException
                *typeCheckFailed = TypeCheckFailedAction::CallHelper_AlwaysThrows;

                // Assume that exceptions are rare
                *likelihood = 100;
            }
            else
            {
                // Fallback for isinst simply returns null here
                *typeCheckFailed = TypeCheckFailedAction::ReturnNull;
            }

            // Update the common denominator class to be more exact
            *commonCls = result;
        }
        else
        {
            // 3) Consult with PGO data
            LikelyClassMethodRecord likelyClasses[MAX_GDV_TYPE_CHECKS];
            unsigned                likelyClassCount =
                getLikelyClasses(likelyClasses, MAX_GDV_TYPE_CHECKS, comp->fgPgoSchema, comp->fgPgoSchemaCount,
                                 comp->fgPgoData, (int)castHelper->gtCastHelperILOffset);

            if (likelyClassCount != 0)
            {
#ifdef DEBUG
                // Print all the candidates and their likelihoods to the log
                for (UINT32 i = 0; i < likelyClassCount; i++)
                {
                    const char* className = comp->eeGetClassName((CORINFO_CLASS_HANDLE)likelyClasses[i].handle);
                    JITDUMP("  %u) %p (%s) [likelihood:%u%%]\n", i + 1, likelyClasses[i].handle, className,
                            likelyClasses[i].likelihood);
                }

                // Optional stress mode to pick a random known class, rather than
                // the most likely known class.
                if (JitConfig.JitRandomGuardedDevirtualization() != 0)
                {
                    // Reuse the random inliner's random state.
                    CLRRandom* const random = comp->impInlineRoot()->m_inlineStrategy->GetRandom(
                        JitConfig.JitRandomGuardedDevirtualization());
                    unsigned index = static_cast<unsigned>(random->Next(static_cast<int>(likelyClassCount)));

                    likelyClasses[0].likelihood = 100;
                    likelyClasses[0].handle     = likelyClasses[index].handle;
                }
#endif

                // if there is a dominating candidate with >= 50% likelihood, use it
                const unsigned likelihoodMinThreshold = 50;
                if (likelyClasses[0].likelihood < likelihoodMinThreshold)
                {
                    JITDUMP("Likely class likelihood is below %u%% - bail out.\n", likelihoodMinThreshold);
                    return NO_CLASS_HANDLE;
                }

                *likelihood = likelyClasses[0].likelihood;
                result      = (CORINFO_CLASS_HANDLE)likelyClasses[0].handle;

                // Validate static profile data
                if ((comp->info.compCompHnd->getClassAttribs(result) &
                     (CORINFO_FLG_INTERFACE | CORINFO_FLG_ABSTRACT)) != 0)
                {
                    // Possible scenario: someone changed Foo to be an interface/abstract class/static class,
                    // but static profile data still reports it as a normal likely class.
                    JITDUMP("Likely class is abstract/interface - bail out (stale PGO data?).\n");
                    return NO_CLASS_HANDLE;
                }
            }
            //
            // 4) Last chance: let's try to speculate!
            //
            else if (helper == CORINFO_HELP_CHKCASTINTERFACE)
            {
                // Nothing to speculate here, e.g. (IDisposable)obj
                return NO_CLASS_HANDLE;
            }
            else if (helper == CORINFO_HELP_CHKCASTARRAY)
            {
                // CHKCASTARRAY against exact classes is already handled above, so it's not exact here.
                //
                //   (int[])obj - can we use int[] as a guess? No! It's an overhead if obj is uint[]
                //                or any int-backed enum
                //
                return NO_CLASS_HANDLE;
            }
            else if (helper == CORINFO_HELP_CHKCASTCLASS)
            {
                // CHKCASTCLASS against exact classes is already handled above, so it's not exact here.
                //
                // let's use castToCls as a guess, we might regress some cases, but at least we know that unrelated
                // types are going to throw InvalidCastException, so we can assume the overhead happens rarely.
                result = castToCls;
            }
            else if (helper == CORINFO_HELP_CHKCASTANY)
            {
                // Same as CORINFO_HELP_CHKCASTCLASS above, the only difference - let's check castToCls for
                // being non-abstract and non-interface first as it makes no sense to speculate on those.
                if ((comp->info.compCompHnd->getClassAttribs(castToCls) &
                     (CORINFO_FLG_INTERFACE | CORINFO_FLG_ABSTRACT)) != 0)
                {
                    return NO_CLASS_HANDLE;
                }
                result = castToCls;
            }
            else if (helper == CORINFO_HELP_ISINSTANCEOFINTERFACE)
            {
                // Nothing to speculate here, e.g. obj is IDisposable
                return NO_CLASS_HANDLE;
            }
            else if (helper == CORINFO_HELP_ISINSTANCEOFARRAY)
            {
                // ISINSTANCEOFARRAY against exact classes is already handled above, so it's not exact here.
                //
                //  obj is int[] - can we use int[] as a guess? No! It's an overhead if obj is uint[]
                //                 or any int-backed enum[]
                return NO_CLASS_HANDLE;
            }
            else if (helper == CORINFO_HELP_ISINSTANCEOFCLASS)
            {
                // ISINSTANCEOFCLASS against exact classes is already handled above, so it's not exact here.
                //
                //  obj is MyClass - can we use MyClass as a guess? No! It's an overhead for any other type except
                //                   MyClass and its subclasses - chances of hitting that overhead are too high.
                //
                return NO_CLASS_HANDLE;
            }
            else if (helper == CORINFO_HELP_ISINSTANCEOFANY)
            {
                // ditto + type variance, etc.
                return NO_CLASS_HANDLE;
            }
            else
            {
                unreached();
            }
        }
    }

    if (result == NO_CLASS_HANDLE)
    {
        // TODO-InlineCast: null coming from PGO data could be a hint for us to only expand the null check
        return NO_CLASS_HANDLE;
    }

    const TypeCompareState castResult = comp->info.compCompHnd->compareTypesForCast(result, castToCls);
    if (castResult == TypeCompareState::May)
    {
        // TODO-InlineCast: do we need to check for May here? Conservatively assume that we do.
        JITDUMP("compareTypesForCast returned May for this candidate\n");
        return NO_CLASS_HANDLE;
    }
    else if (castResult == TypeCompareState::Must)
    {
        // return actual object on successful type check
        *typeCheckPassed = TypeCheckPassedAction::ReturnObj;
    }
    else if (castResult == TypeCompareState::MustNot)
    {
        // Our likely candidate never passes the type check (may happen with PGO-driven expansion),
        if (!isCastClass)
        {
            // return null on successful type check
            *typeCheckPassed = TypeCheckPassedAction::ReturnNull;
        }
        else
        {
            // give up on castclass - it's going to throw InvalidCastException anyway
            return NO_CLASS_HANDLE;
        }
    }
    else
    {
        unreached();
    }

    if (isCastClass && (result == castToCls) && (*typeCheckFailed == TypeCheckFailedAction::CallHelper))
    {
        // TODO-InlineCast: Change helper to faster CORINFO_HELP_CHKCASTCLASS_SPECIAL
        // it won't check for null and castToCls assuming we've already done it inline.
    }

    assert(result != NO_CLASS_HANDLE);
    return result;
}

//------------------------------------------------------------------------------
// fgLateCastExpansionForCall : Expand specific cast helper, see
//    fgLateCastExpansion's comments.
//
// Arguments:
//    block - Block containing the cast helper to expand
//    stmt  - Statement containing the cast helper
//    call  - The cast helper
//
// Returns:
//    True if expanded, false otherwise.
//
bool Compiler::fgLateCastExpansionForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call)
{
    unsigned              likelihood;
    TypeCheckFailedAction typeCheckFailedAction;
    TypeCheckPassedAction typeCheckPassedAction;
    CORINFO_CLASS_HANDLE  commonCls;
    CORINFO_CLASS_HANDLE  expectedExactCls =
        PickCandidateForTypeCheck(this, call, &commonCls, &likelihood, &typeCheckFailedAction, &typeCheckPassedAction);
    if (expectedExactCls == NO_CLASS_HANDLE)
    {
        return false;
    }

    BasicBlock* block = *pBlock;
    JITDUMP("Expanding cast helper call in " FMT_BB "...\n", block->bbNum);
    DISPTREE(call);
    JITDUMP("\n");

    DebugInfo debugInfo = stmt->GetDebugInfo();

    BasicBlock*    firstBb;
    BasicBlock*    lastBb;
    const unsigned tmpNum = SplitAtTreeAndReplaceItWithLocal(this, block, stmt, call, &firstBb, &lastBb);
    lvaSetClass(tmpNum, commonCls);
    GenTree* tmpNode = gtNewLclvNode(tmpNum, call->TypeGet());
    *pBlock          = lastBb;

    // We're going to expand this "isinst" like this:
    //
    // prevBb:                                      [weight: 1.0]
    //     ...
    //
    // nullcheckBb (BBJ_COND):                      [weight: 1.0]
    //     tmp = obj;
    //     if (tmp == null)
    //         goto lastBb;
    //
    // typeCheckBb (BBJ_COND):                      [weight: 0.5]
    //     if (tmp->pMT == likelyCls)
    //         goto typeCheckSucceedBb;
    //
    // fallbackBb (BBJ_ALWAYS):                     [weight: <profile> or 0]
    //     tmp = helper_call(expectedCls, obj);
    //     goto lastBb;
    //     // NOTE: as an optimization we can omit the call and return null instead
    //     // or mark the call as no-return in certain cases.
    //
    //
    // typeCheckSucceedBb (BBJ_ALWAYS):             [weight: <profile>]
    //     no-op (or tmp = null; in case of 'MustNot')
    //
    // lastBb (BBJ_any):                            [weight: 1.0]
    //     use(tmp);
    //

    // Block 1: nullcheckBb
    // TODO-InlineCast: assertionprop should leave us a mark that objArg is never null, so we can omit this check
    // it's too late to rely on upstream phases to do this for us (unless we do optRepeat).
    GenTree* nullcheckOp = gtNewOperNode(GT_EQ, TYP_INT, tmpNode, gtNewNull());
    nullcheckOp->gtFlags |= GTF_RELOP_JMP_USED;
    BasicBlock* nullcheckBb = fgNewBBFromTreeAfter(BBJ_COND, firstBb, gtNewOperNode(GT_JTRUE, TYP_VOID, nullcheckOp),
                                                   debugInfo, lastBb, true);

    // The very first statement in the whole expansion is to assign obj to tmp.
    // We assume it's the value we're going to return in most cases.
    GenTree*   originalObj = gtCloneExpr(call->gtArgs.GetUserArgByIndex(1)->GetNode());
    Statement* assignTmp   = fgNewStmtAtBeg(nullcheckBb, gtNewTempStore(tmpNum, originalObj), debugInfo);
    gtSetStmtInfo(assignTmp);
    fgSetStmtSeq(assignTmp);

    // Block 2: typeCheckBb
    // TODO-InlineCast: if likelyCls == expectedCls we can consider saving to a local to re-use.
    GenTree* likelyClsNode = gtNewIconEmbClsHndNode(expectedExactCls);
    GenTree* mtCheck       = gtNewOperNode(GT_EQ, TYP_INT, gtNewMethodTableLookup(gtCloneExpr(tmpNode)), likelyClsNode);
    mtCheck->gtFlags |= GTF_RELOP_JMP_USED;
    GenTree*    jtrue       = gtNewOperNode(GT_JTRUE, TYP_VOID, mtCheck);
    BasicBlock* typeCheckBb = fgNewBBFromTreeAfter(BBJ_COND, nullcheckBb, jtrue, debugInfo, lastBb, true);

    // Block 3: fallbackBb
    BasicBlock* fallbackBb;
    if (typeCheckFailedAction == TypeCheckFailedAction::CallHelper_AlwaysThrows)
    {
        // fallback call is used only to throw InvalidCastException
        call->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;
        fallbackBb = fgNewBBFromTreeAfter(BBJ_THROW, typeCheckBb, call, debugInfo, nullptr, true);
    }
    else if (typeCheckFailedAction == TypeCheckFailedAction::ReturnNull)
    {
        // if fallback call is not needed, we just assign null to tmp
        GenTree* fallbackTree = gtNewTempStore(tmpNum, gtNewNull());
        fallbackBb            = fgNewBBFromTreeAfter(BBJ_ALWAYS, typeCheckBb, fallbackTree, debugInfo, lastBb, true);
    }
    else
    {
        GenTree* fallbackTree = gtNewTempStore(tmpNum, call);
        fallbackBb            = fgNewBBFromTreeAfter(BBJ_ALWAYS, typeCheckBb, fallbackTree, debugInfo, lastBb, true);
    }

    // Block 4: typeCheckSucceedBb
    GenTree* typeCheckSucceedTree;
    if (typeCheckPassedAction == TypeCheckPassedAction::ReturnNull)
    {
        typeCheckSucceedTree = gtNewTempStore(tmpNum, gtNewNull());
    }
    else
    {
        assert(typeCheckPassedAction == TypeCheckPassedAction::ReturnObj);
        // No-op because tmp was already assigned to obj
        typeCheckSucceedTree = gtNewNothingNode();
    }
    BasicBlock* typeCheckSucceedBb =
        fgNewBBFromTreeAfter(BBJ_ALWAYS, fallbackBb, typeCheckSucceedTree, debugInfo, lastBb);

    //
    // Wire up the blocks
    //
    firstBb->SetTarget(nullcheckBb);
    nullcheckBb->SetTrueTarget(lastBb);
    nullcheckBb->SetFalseTarget(typeCheckBb);
    typeCheckBb->SetTrueTarget(typeCheckSucceedBb);
    typeCheckBb->SetFalseTarget(fallbackBb);
    fgRemoveRefPred(lastBb, firstBb);
    fgAddRefPred(nullcheckBb, firstBb);
    fgAddRefPred(typeCheckBb, nullcheckBb);
    fgAddRefPred(lastBb, nullcheckBb);
    fgAddRefPred(fallbackBb, typeCheckBb);
    fgAddRefPred(lastBb, typeCheckSucceedBb);
    fgAddRefPred(typeCheckSucceedBb, typeCheckBb);
    if (typeCheckFailedAction != TypeCheckFailedAction::CallHelper_AlwaysThrows)
    {
        // if fallbackBb is BBJ_THROW then it has no successors
        fgAddRefPred(lastBb, fallbackBb);
    }

    //
    // Re-distribute weights
    // We assume obj is 50%/50% null/not-null (TODO-InlineCast: rely on PGO)
    // and rely on profile for the slow path.
    //
    nullcheckBb->inheritWeight(firstBb);
    typeCheckBb->inheritWeightPercentage(nullcheckBb, 50);
    fallbackBb->inheritWeightPercentage(typeCheckBb,
                                        (typeCheckFailedAction == TypeCheckFailedAction::CallHelper_AlwaysThrows)
                                            ? 0
                                            : 100 - likelihood);
    typeCheckSucceedBb->inheritWeightPercentage(typeCheckBb, likelihood);
    lastBb->inheritWeight(firstBb);

    //
    // Validate EH regions
    //
    assert(BasicBlock::sameEHRegion(firstBb, lastBb));
    assert(BasicBlock::sameEHRegion(firstBb, nullcheckBb));
    assert(BasicBlock::sameEHRegion(firstBb, fallbackBb));
    assert(BasicBlock::sameEHRegion(firstBb, typeCheckBb));

    // Bonus step: merge prevBb with nullcheckBb as they are likely to be mergeable
    if (fgCanCompactBlocks(firstBb, nullcheckBb))
    {
        fgCompactBlocks(firstBb, nullcheckBb);
    }

    return true;
}
