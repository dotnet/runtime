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

    //
    // Update loop info
    //
    nullcheckBb->bbNatLoopNum = prevBb->bbNatLoopNum;
    fastPathBb->bbNatLoopNum  = prevBb->bbNatLoopNum;
    fallbackBb->bbNatLoopNum  = prevBb->bbNatLoopNum;
    if (needsSizeCheck)
    {
        sizeCheckBb->bbNatLoopNum = prevBb->bbNatLoopNum;
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
//    PhaseStatus indicating what, if anything, was changed.
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
    BasicBlock* block = *pBlock;

    CorInfoHelpFunc helper = call->GetHelperNum();

    if ((helper != CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED) &&
        (helper != CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED))
    {
        return false;
    }

    assert(!opts.IsReadyToRun());

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

    GenTree* typeThreadStaticBlockIndexValue = call->gtArgs.GetArgByIndex(0)->GetNode();
    GenTree* tlsValue                        = nullptr;
    unsigned tlsLclNum                       = lvaGrabTemp(true DEBUGARG("TLS access"));
    lvaTable[tlsLclNum].lvType               = TYP_I_IMPL;
    GenTree* maxThreadStaticBlocksValue      = nullptr;
    GenTree* threadStaticBlocksValue         = nullptr;
    GenTree* tlsValueDef                     = nullptr;

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

    //
    // Update loop info if loop table is known to be valid
    //
    maxThreadStaticBlocksCondBB->bbNatLoopNum = prevBb->bbNatLoopNum;
    threadStaticBlockNullCondBB->bbNatLoopNum = prevBb->bbNatLoopNum;
    fastPathBb->bbNatLoopNum                  = prevBb->bbNatLoopNum;
    fallbackBb->bbNatLoopNum                  = prevBb->bbNatLoopNum;

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
        fgReorderBlocks(/* useProfileData */ false);
        fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_BASICS);
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

    //
    // Update loop info if loop table is known to be valid
    //

    isInitedBb->bbNatLoopNum   = prevBb->bbNatLoopNum;
    helperCallBb->bbNatLoopNum = prevBb->bbNatLoopNum;

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

    //
    // Update bbNatLoopNum for all new blocks
    //
    lengthCheckBb->bbNatLoopNum = prevBb->bbNatLoopNum;
    fastpathBb->bbNatLoopNum    = prevBb->bbNatLoopNum;

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
