// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
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
    assert(call->IsHelperCall());

    if (!call->IsExpRuntimeLookup())
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
    // prevBb(BBJ_NONE):                    [weight: 1.0]
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
    // fallbackBb(BBJ_NONE):                [weight: 0.2]
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
    BasicBlock* nullcheckBb =
        fgNewBBFromTreeAfter(BBJ_COND, prevBb, gtNewOperNode(GT_JTRUE, TYP_VOID, nullcheckOp), debugInfo);

    // Fallback basic block
    GenTree*    fallbackValueDef = gtNewStoreLclVarNode(rtLookupLcl->GetLclNum(), call);
    BasicBlock* fallbackBb       = fgNewBBFromTreeAfter(BBJ_NONE, nullcheckBb, fallbackValueDef, debugInfo, true);

    // Fast-path basic block
    GenTree*    fastpathValueDef = gtNewStoreLclVarNode(rtLookupLcl->GetLclNum(), fastPathValueClone);
    BasicBlock* fastPathBb       = fgNewBBFromTreeAfter(BBJ_ALWAYS, nullcheckBb, fastpathValueDef, debugInfo);

    BasicBlock* sizeCheckBb = nullptr;
    if (needsSizeCheck)
    {
        // Dynamic expansion case (sizeCheckBb is added and some preds are changed):
        //
        // prevBb(BBJ_NONE):                    [weight: 1.0]
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
        // fallbackBb(BBJ_NONE):                [weight: 0.36]
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
        sizeCheckBb    = fgNewBBFromTreeAfter(BBJ_COND, prevBb, jtrue, debugInfo);
    }

    //
    // Update preds in all new blocks
    //
    fgRemoveRefPred(block, prevBb);
    fgAddRefPred(block, fastPathBb);
    fgAddRefPred(block, fallbackBb);
    nullcheckBb->bbJumpDest = fallbackBb;
    fastPathBb->bbJumpDest  = block;

    if (needsSizeCheck)
    {
        // sizeCheckBb is the first block after prevBb
        fgAddRefPred(sizeCheckBb, prevBb);
        // sizeCheckBb flows into nullcheckBb in case if the size check passes
        fgAddRefPred(nullcheckBb, sizeCheckBb);
        // fallbackBb is reachable from both nullcheckBb and sizeCheckBb
        fgAddRefPred(fallbackBb, nullcheckBb);
        fgAddRefPred(fallbackBb, sizeCheckBb);
        // fastPathBb is only reachable from successful nullcheckBb
        fgAddRefPred(fastPathBb, nullcheckBb);
        // sizeCheckBb fails - jump to fallbackBb
        sizeCheckBb->bbJumpDest = fallbackBb;
    }
    else
    {
        // nullcheckBb is the first block after prevBb
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

    if (!doesMethodHasTlsFieldAccess())
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
    assert(call->IsHelperCall());
    if (!call->IsExpTLSFieldAccess())
    {
        return false;
    }

#ifdef TARGET_ARM
    // On Arm, Thread execution blocks are accessed using co-processor registers and instructions such
    // as MRC and MCR are used to access them. We do not support them and so should never optimize the
    // field access using TLS.
    assert(!"Unsupported scenario of optimizing TLS access on Arm32");
#endif

    JITDUMP("Expanding thread static local access for [%06d] in " FMT_BB ":\n", dspTreeID(call), block->bbNum);
    DISPTREE(call);
    JITDUMP("\n");
    bool isGCThreadStatic =
        eeGetHelperNum(call->gtCallMethHnd) == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED;

    CORINFO_THREAD_STATIC_BLOCKS_INFO threadStaticBlocksInfo;
    info.compCompHnd->getThreadLocalStaticBlocksInfo(&threadStaticBlocksInfo, isGCThreadStatic);

    uint32_t offsetOfMaxThreadStaticBlocksVal = 0;
    uint32_t offsetOfThreadStaticBlocksVal    = 0;

    JITDUMP("getThreadLocalStaticBlocksInfo (%s)\n:", isGCThreadStatic ? "GC" : "Non-GC");
    offsetOfMaxThreadStaticBlocksVal = threadStaticBlocksInfo.offsetOfMaxThreadStaticBlocks;
    offsetOfThreadStaticBlocksVal    = threadStaticBlocksInfo.offsetOfThreadStaticBlocks;

    JITDUMP("tlsIndex= %u\n", (ssize_t)threadStaticBlocksInfo.tlsIndex.addr);
    JITDUMP("offsetOfThreadLocalStoragePointer= %u\n", threadStaticBlocksInfo.offsetOfThreadLocalStoragePointer);
    JITDUMP("offsetOfMaxThreadStaticBlocks= %u\n", offsetOfMaxThreadStaticBlocksVal);
    JITDUMP("offsetOfThreadStaticBlocks= %u\n", offsetOfThreadStaticBlocksVal);
    JITDUMP("offsetOfGCDataPointer= %u\n", threadStaticBlocksInfo.offsetOfGCDataPointer);

    assert(threadStaticBlocksInfo.tlsIndex.accessType == IAT_VALUE);
    assert((eeGetHelperNum(call->gtCallMethHnd) == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED) ||
           (eeGetHelperNum(call->gtCallMethHnd) == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED));

    call->ClearExpTLSFieldAccess();
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

    void** pIdAddr = nullptr;

    size_t   tlsIndexValue = (size_t)threadStaticBlocksInfo.tlsIndex.addr;
    GenTree* dllRef        = nullptr;

    if (tlsIndexValue != 0)
    {
        dllRef = gtNewIconHandleNode(tlsIndexValue * TARGET_POINTER_SIZE, GTF_ICON_TLS_HDL);
    }

    // Mark this ICON as a TLS_HDL, codegen will use FS:[cns] or GS:[cns]
    GenTree* tlsRef = gtNewIconHandleNode(threadStaticBlocksInfo.offsetOfThreadLocalStoragePointer, GTF_ICON_TLS_HDL);

    tlsRef = gtNewIndir(TYP_I_IMPL, tlsRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    if (dllRef != nullptr)
    {
        // Add the dllRef to produce thread local storage reference for coreclr
        tlsRef = gtNewOperNode(GT_ADD, TYP_I_IMPL, tlsRef, dllRef);
    }

    // Base of coreclr's thread local storage
    GenTree* tlsValue = gtNewIndir(TYP_I_IMPL, tlsRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    // Cache the tls value
    unsigned tlsLclNum         = lvaGrabTemp(true DEBUGARG("TLS access"));
    lvaTable[tlsLclNum].lvType = TYP_I_IMPL;
    GenTree* tlsValueDef       = gtNewStoreLclVarNode(tlsLclNum, tlsValue);
    GenTree* tlsLclValueUse    = gtNewLclVarNode(tlsLclNum);

    // Create tree for "maxThreadStaticBlocks = tls[offsetOfMaxThreadStaticBlocks]"
    GenTree* offsetOfMaxThreadStaticBlocks = gtNewIconNode(offsetOfMaxThreadStaticBlocksVal, TYP_I_IMPL);
    GenTree* maxThreadStaticBlocksRef =
        gtNewOperNode(GT_ADD, TYP_I_IMPL, gtCloneExpr(tlsLclValueUse), offsetOfMaxThreadStaticBlocks);
    GenTree* maxThreadStaticBlocksValue =
        gtNewIndir(TYP_INT, maxThreadStaticBlocksRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

    // Create tree for "if (maxThreadStaticBlocks < typeIndex)"
    GenTree* maxThreadStaticBlocksCond =
        gtNewOperNode(GT_LT, TYP_INT, maxThreadStaticBlocksValue, gtCloneExpr(typeThreadStaticBlockIndexValue));
    maxThreadStaticBlocksCond = gtNewOperNode(GT_JTRUE, TYP_VOID, maxThreadStaticBlocksCond);

    // Create tree for "threadStaticBlockBase = tls[offsetOfThreadStaticBlocks]"
    GenTree* offsetOfThreadStaticBlocks = gtNewIconNode(offsetOfThreadStaticBlocksVal, TYP_I_IMPL);
    GenTree* threadStaticBlocksRef =
        gtNewOperNode(GT_ADD, TYP_I_IMPL, gtCloneExpr(tlsLclValueUse), offsetOfThreadStaticBlocks);
    GenTree* threadStaticBlocksValue =
        gtNewIndir(TYP_I_IMPL, threadStaticBlocksRef, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);

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

    // prevBb (BBJ_NONE):                                               [weight: 1.0]
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
    BasicBlock* maxThreadStaticBlocksCondBB = fgNewBBFromTreeAfter(BBJ_COND, prevBb, tlsValueDef, debugInfo);

    fgInsertStmtAfter(maxThreadStaticBlocksCondBB, maxThreadStaticBlocksCondBB->firstStmt(),
                      fgNewStmtFromTree(maxThreadStaticBlocksCond));

    // threadStaticBlockNullCondBB
    BasicBlock* threadStaticBlockNullCondBB =
        fgNewBBFromTreeAfter(BBJ_COND, maxThreadStaticBlocksCondBB, threadStaticBlockBaseDef, debugInfo);
    fgInsertStmtAfter(threadStaticBlockNullCondBB, threadStaticBlockNullCondBB->firstStmt(),
                      fgNewStmtFromTree(threadStaticBlockNullCond));

    // fallbackBb
    GenTree*    fallbackValueDef = gtNewStoreLclVarNode(threadStaticBlockLclNum, call);
    BasicBlock* fallbackBb =
        fgNewBBFromTreeAfter(BBJ_ALWAYS, threadStaticBlockNullCondBB, fallbackValueDef, debugInfo, true);

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
    BasicBlock* fastPathBb = fgNewBBFromTreeAfter(BBJ_ALWAYS, fallbackBb, fastPathValueDef, debugInfo, true);

    //
    // Update preds in all new blocks
    //
    fgRemoveRefPred(block, prevBb);
    fgAddRefPred(maxThreadStaticBlocksCondBB, prevBb);

    fgAddRefPred(threadStaticBlockNullCondBB, maxThreadStaticBlocksCondBB);
    fgAddRefPred(fallbackBb, maxThreadStaticBlocksCondBB);

    fgAddRefPred(fastPathBb, threadStaticBlockNullCondBB);
    fgAddRefPred(fallbackBb, threadStaticBlockNullCondBB);

    fgAddRefPred(block, fastPathBb);
    fgAddRefPred(block, fallbackBb);

    maxThreadStaticBlocksCondBB->bbJumpDest = fallbackBb;
    threadStaticBlockNullCondBB->bbJumpDest = fastPathBb;
    fastPathBb->bbJumpDest                  = block;
    fallbackBb->bbJumpDest                  = block;

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
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
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
            if (!tree->IsHelperCall())
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
    assert(call->IsHelperCall());

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
        fgNewBBFromTreeAfter(BBJ_COND, prevBb, gtNewOperNode(GT_JTRUE, TYP_VOID, isInitedCmp), debugInfo);

    // Fallback basic block
    // TODO-CQ: for JIT we can replace the original call with CORINFO_HELP_INITCLASS
    // that only accepts a single argument
    BasicBlock* helperCallBb = fgNewBBFromTreeAfter(BBJ_NONE, isInitedBb, call, debugInfo, true);

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
    // prevBb(BBJ_NONE):                    [weight: 1.0]
    //     ...
    //
    // isInitedBb(BBJ_COND):                [weight: 1.0]
    //     if (isInited)
    //         goto block;
    //
    // helperCallBb(BBJ_NONE):              [weight: 0.0]
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

    // prevBb always flow into isInitedBb
    fgAddRefPred(isInitedBb, prevBb);

    // Both fastPathBb and helperCallBb have a single common pred - isInitedBb
    fgAddRefPred(helperCallBb, isInitedBb);

    // helperCallBb unconditionally jumps to the last block (jumps over fastPathBb)
    isInitedBb->bbJumpDest = block;

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
