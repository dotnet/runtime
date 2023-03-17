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
    unsigned const tmpNum  = comp->lvaGrabTemp(true DEBUGARG("spilling expr"));
    Statement*     asgStmt = comp->fgNewStmtAtEnd(exprBlock, comp->gtNewTempAssign(tmpNum, expr), debugInfo);
    comp->gtSetStmtInfo(asgStmt);
    comp->fgSetStmtSeq(asgStmt);
    return comp->gtNewLclvNode(tmpNum, genActualType(expr));
};

// Create block from the given tree
static BasicBlock* CreateBlockFromTree(Compiler*   comp,
                                       BasicBlock* insertAfter,
                                       BBjumpKinds blockKind,
                                       GenTree*    tree,
                                       DebugInfo&  debugInfo,
                                       bool        updateSideEffects = false)
{
    // Fast-path basic block
    BasicBlock* newBlock = comp->fgNewBBafter(blockKind, insertAfter, true);
    newBlock->bbFlags |= BBF_INTERNAL;
    Statement* stmt = comp->fgNewStmtFromTree(tree, debugInfo);
    comp->fgInsertStmtAtEnd(newBlock, stmt);
    newBlock->bbCodeOffs    = insertAfter->bbCodeOffsEnd;
    newBlock->bbCodeOffsEnd = insertAfter->bbCodeOffsEnd;
    if (updateSideEffects)
    {
        comp->gtUpdateStmtSideEffects(stmt);
    }
    return newBlock;
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

    // Find all calls with GTF_CALL_M_EXP_RUNTIME_LOOKUP flag
    // We don't use Blocks() iterator here as we modify `block` variable
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
    SCAN_BLOCK_AGAIN:
        for (Statement* const stmt : block->Statements())
        {
            if ((stmt->GetRootNode()->gtFlags & GTF_CALL) == 0)
            {
                // TP: Stmt has no calls - bail out
                continue;
            }

            for (GenTree* const tree : stmt->TreeList())
            {
                // We only need calls with IsExpRuntimeLookup() flag
                if (!tree->IsCall() || !tree->AsCall()->IsExpRuntimeLookup())
                {
                    continue;
                }
                assert(tree->IsHelperCall());
                JITDUMP("Expanding runtime lookup for [%06d] in " FMT_BB ":\n", dspTreeID(tree), block->bbNum)
                DISPTREE(tree)
                JITDUMP("\n")

                GenTreeCall* call = tree->AsCall();

                // Clear ExpRuntimeLookup flag so we won't miss any runtime lookup that needs partial expansion
                call->ClearExpRuntimeLookup();

                if (call->IsTailCall())
                {
                    // It is very unlikely to happen and is impossible to represent in C#
                    continue;
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
                    continue;
                }

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

                // Mostly for Tier0: if the current statement is ASG(LCL, RuntimeLookup)
                // we can drop it and use that LCL as the destination
                if (stmt->GetRootNode()->OperIs(GT_ASG))
                {
                    GenTree* lhs = stmt->GetRootNode()->gtGetOp1();
                    GenTree* rhs = stmt->GetRootNode()->gtGetOp2();
                    if (lhs->OperIs(GT_LCL_VAR) && rhs == *callUse)
                    {
                        rtLookupLcl = gtClone(lhs)->AsLclVar();
                        fgRemoveStmt(block, stmt);
                    }
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
                        slotPtrTree = gtNewOperNode(GT_IND, TYP_I_IMPL, slotPtrTree);
                        slotPtrTree->gtFlags |= GTF_IND_NONFAULTING;
                        if (!isLastIndirectionWithSizeCheck)
                        {
                            slotPtrTree->gtFlags |= GTF_IND_INVARIANT;
                        }
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
                        slotPtrTree = gtNewOperNode(GT_ADD, TYP_I_IMPL, slotPtrTree,
                                                    gtNewIconNode(runtimeLookup.offsets[i], TYP_I_IMPL));
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
                GenTree* fastPathValue = gtNewOperNode(GT_IND, TYP_I_IMPL, gtCloneExpr(slotPtrTree));
                fastPathValue->gtFlags |= GTF_IND_NONFAULTING;
                // Save dictionary slot to a local (to be used by fast path)
                GenTree* fastPathValueClone =
                    opts.OptimizationEnabled() ? fgMakeMultiUse(&fastPathValue) : gtCloneExpr(fastPathValue);
                GenTree* nullcheckOp = gtNewOperNode(GT_EQ, TYP_INT, fastPathValue, gtNewIconNode(0, TYP_I_IMPL));
                nullcheckOp->gtFlags |= GTF_RELOP_JMP_USED;
                BasicBlock* nullcheckBb =
                    CreateBlockFromTree(this, prevBb, BBJ_COND, gtNewOperNode(GT_JTRUE, TYP_VOID, nullcheckOp),
                                        debugInfo);

                // Fallback basic block
                GenTree*    asgFallbackValue = gtNewAssignNode(gtClone(rtLookupLcl), call);
                BasicBlock* fallbackBb =
                    CreateBlockFromTree(this, nullcheckBb, BBJ_NONE, asgFallbackValue, debugInfo, true);

                // Fast-path basic block
                GenTree*    asgFastpathValue = gtNewAssignNode(gtClone(rtLookupLcl), fastPathValueClone);
                BasicBlock* fastPathBb =
                    CreateBlockFromTree(this, nullcheckBb, BBJ_ALWAYS, asgFastpathValue, debugInfo);

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
                    GenTree* sizeValue       = gtNewOperNode(GT_IND, TYP_I_IMPL, sizeValueOffset);
                    sizeValue->gtFlags |= GTF_IND_NONFAULTING;

                    // sizeCheck fails if sizeValue <= pRuntimeLookup->offsets[i]
                    GenTree* offsetValue =
                        gtNewIconNode(runtimeLookup.offsets[runtimeLookup.indirections - 1], TYP_I_IMPL);
                    GenTree* sizeCheck = gtNewOperNode(GT_LE, TYP_INT, sizeValue, offsetValue);
                    sizeCheck->gtFlags |= GTF_RELOP_JMP_USED;

                    GenTree* jtrue = gtNewOperNode(GT_JTRUE, TYP_VOID, sizeCheck);
                    sizeCheckBb    = CreateBlockFromTree(this, prevBb, BBJ_COND, jtrue, debugInfo);
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
                // Update loop info if loop table is known to be valid
                //
                if (optLoopTableValid && prevBb->bbNatLoopNum != BasicBlock::NOT_IN_LOOP)
                {
                    nullcheckBb->bbNatLoopNum = prevBb->bbNatLoopNum;
                    fastPathBb->bbNatLoopNum  = prevBb->bbNatLoopNum;
                    fallbackBb->bbNatLoopNum  = prevBb->bbNatLoopNum;
                    if (needsSizeCheck)
                    {
                        sizeCheckBb->bbNatLoopNum = prevBb->bbNatLoopNum;
                    }
                    // Update lpBottom after block split
                    if (optLoopTable[prevBb->bbNatLoopNum].lpBottom == prevBb)
                    {
                        optLoopTable[prevBb->bbNatLoopNum].lpBottom = block;
                    }
                }

                // All blocks are expected to be in the same EH region
                assert(BasicBlock::sameEHRegion(prevBb, block));
                assert(BasicBlock::sameEHRegion(prevBb, nullcheckBb));
                assert(BasicBlock::sameEHRegion(prevBb, fastPathBb));
                if (needsSizeCheck)
                {
                    assert(BasicBlock::sameEHRegion(prevBb, sizeCheckBb));
                }

                // Scan current block again, the current call will be ignored because of ClearExpRuntimeLookup.
                // We don't try to re-use expansions for the same lookups in the current block here - CSE is responsible
                // for that
                result = PhaseStatus::MODIFIED_EVERYTHING;

                // We've modified the graph and the current "block" might still have more runtime lookups
                goto SCAN_BLOCK_AGAIN;
            }
        }
    }

    if (result == PhaseStatus::MODIFIED_EVERYTHING)
    {
        if (opts.OptimizationEnabled())
        {
            fgReorderBlocks(/* useProfileData */ false);
            fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_BASICS);
        }
    }
    return result;
}
