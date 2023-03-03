// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------------
// fgExpandRuntimeLookups : partially expand runtime lookups helper calls
//                          to add a nullcheck [+ size check] and a fast path
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgExpandRuntimeLookups()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if (!doesMethodHaveExpRuntimeLookup())
    {

#ifdef DEBUG
        for (BasicBlock* block : Blocks())
        {
            for (Statement* stmt : block->Statements())
            {
                for (GenTree* tree : stmt->TreeList())
                {
                    assert(!tree->IsCall() || (tree->IsCall() && !tree->AsCall()->IsExpRuntimeLookup()));
                }
            }
        }
#endif
        JITDUMP("Current method doesn't have runtime lookups - bail out.")
        return result;
    }

    // Find all calls with GTF_CALL_M_EXP_RUNTIME_LOOKUP flag
    for (BasicBlock* block : Blocks())
    {
    TRAVERSE_BLOCK_AGAIN:

        Statement* prevStmt = nullptr;
        for (Statement* const stmt : block->Statements())
        {
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
                call->ClearExpRuntimeLookup();
                assert(call->gtArgs.CountArgs() == 2);

                if (call->IsTailCall())
                {
                    assert(!"Unexpected runtime lookup as a tail call");
                    continue;
                }

                // call(ctx, signature);
                GenTree* ctxTree = call->gtArgs.GetArgByIndex(0)->GetNode();
                GenTree* sigTree = call->gtArgs.GetArgByIndex(1)->GetNode();

                void* signature = nullptr;
                if (sigTree->IsCnsIntOrI())
                {
                    signature = (void*)sigTree->AsIntCon()->IconValue();
                }
                else
                {
                    // signature is not a constant (CSE'd?) - let's see if we can access it via VN
                    if (vnStore->IsVNConstant(sigTree->gtVNPair.GetLiberal()))
                    {
                        signature = (void*)vnStore->CoercedConstantValue<ssize_t>(sigTree->gtVNPair.GetLiberal());
                    }
                    else
                    {
                        // Technically, it is possible (e.g. it was CSE'd and then VN was erased), but for Debug mode we
                        // want to catch such cases as we really don't want to emit just a fallback call - it's too slow
                        assert(!"can't restore signature argument value");
                        continue;
                    }
                }
                assert(signature != nullptr);

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

                BasicBlockFlags originalFlags = block->bbFlags;
                BasicBlock*     prevBb        = block;

                if (prevStmt == nullptr || opts.OptimizationDisabled())
                {
                    JITDUMP("Splitting " FMT_BB " at the beginning.\n", prevBb->bbNum)
                    block = fgSplitBlockAtBeginning(prevBb);
                }
                else
                {
                    JITDUMP("Splitting " FMT_BB " after statement " FMT_STMT "\n", prevBb->bbNum, prevStmt->GetID())
                    block = fgSplitBlockAfterStatement(prevBb, prevStmt);
                }

                // We split a block, possibly, in the middle - we need to propagate some flags
                prevBb->bbFlags =
                    originalFlags & (~(BBF_SPLIT_LOST | BBF_LOOP_PREHEADER | BBF_RETLESS_CALL) | BBF_GC_SAFE_POINT);
                block->bbFlags |= originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT |
                                                   BBF_LOOP_PREHEADER | BBF_RETLESS_CALL);

                // Define a local for the result
                const unsigned rtLookupLclNum   = lvaGrabTemp(true DEBUGARG("runtime lookup"));
                lvaTable[rtLookupLclNum].lvType = TYP_I_IMPL;
                GenTreeLclVar* rtLookupLcl      = gtNewLclvNode(rtLookupLclNum, call->TypeGet());

                // Save expression to a local and append as the last statement in prevBb
                auto spillExpr = [&](GenTree* expr) -> GenTree* {
                    if (expr->OperIs(GT_LCL_VAR))
                    {
                        return gtClone(expr);
                    }
                    unsigned const tmpNum   = lvaGrabTemp(false DEBUGARG("spilling expr"));
                    lvaTable[tmpNum].lvType = expr->TypeGet();
                    Statement* asgStmt      = fgNewStmtAtEnd(prevBb, gtNewTempAssign(tmpNum, expr));
                    asgStmt->SetDebugInfo(debugInfo);
                    gtSetStmtInfo(asgStmt);
                    fgSetStmtSeq(asgStmt);
                    return gtNewLclvNode(tmpNum, expr->TypeGet());
                };

                // if sigTree was not a constant e.g. COMMA(..., CNS)) - spill it
                if (!sigTree->IsCnsIntOrI())
                {
                    spillExpr(sigTree);
                }

                // Prepare slotPtr tree (TODO: consider sharing this part with impRuntimeLookup)
                ctxTree                = spillExpr(ctxTree);
                GenTree* slotPtrTree   = gtClone(ctxTree);
                GenTree* indOffTree    = nullptr;
                GenTree* lastIndOfTree = nullptr;
                for (WORD i = 0; i < runtimeLookup.indirections; i++)
                {
                    if ((i == 1 && runtimeLookup.indirectFirstOffset) || (i == 2 && runtimeLookup.indirectSecondOffset))
                    {
                        indOffTree  = spillExpr(slotPtrTree);
                        slotPtrTree = gtClone(indOffTree);
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
                            lastIndOfTree = spillExpr(slotPtrTree);
                            slotPtrTree   = gtClone(lastIndOfTree);
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
                BasicBlock* nullcheckBb = fgNewBBafter(BBJ_COND, prevBb, true);
                nullcheckBb->bbFlags |= BBF_INTERNAL;

                GenTree* fastPathValue = gtNewOperNode(GT_IND, TYP_I_IMPL, gtCloneExpr(slotPtrTree));
                fastPathValue->gtFlags |= GTF_IND_NONFAULTING;

                GenTree* fastPathValueClone =
                    opts.OptimizationEnabled() ? fgMakeMultiUse(&fastPathValue) : gtCloneExpr(fastPathValue);

                // Save dictionary slot to a local (to be used by fast path)
                GenTree* nullcheckOp = gtNewOperNode(GT_EQ, TYP_INT, fastPathValue, gtNewIconNode(0, TYP_I_IMPL));
                nullcheckOp->gtFlags |= GTF_RELOP_JMP_USED;
                gtSetEvalOrder(nullcheckOp);
                Statement* nullcheckStmt = fgNewStmtFromTree(gtNewOperNode(GT_JTRUE, TYP_VOID, nullcheckOp));
                nullcheckStmt->SetDebugInfo(debugInfo);
                gtSetStmtInfo(nullcheckStmt);
                fgSetStmtSeq(nullcheckStmt);
                fgInsertStmtAtEnd(nullcheckBb, nullcheckStmt);

                // Fallback basic block
                BasicBlock* fallbackBb = fgNewBBafter(BBJ_NONE, nullcheckBb, true);
                fallbackBb->bbFlags |= BBF_INTERNAL;

                GenTreeCall* fallbackCall = gtCloneExpr(call)->AsCall();
                fallbackCall->gtArgs.GetArgByIndex(0)->SetLateNode(gtClone(ctxTree));
                gtSetEvalOrder(fallbackCall);
                fgMorphCall(fallbackCall);
                assert(!fallbackCall->IsExpRuntimeLookup());
                assert(ctxTree->OperIs(GT_LCL_VAR));
                Statement* asgFallbackStmt = fgNewStmtFromTree(gtNewAssignNode(gtClone(rtLookupLcl), fallbackCall));
                asgFallbackStmt->SetDebugInfo(debugInfo);
                fgInsertStmtAtBeg(fallbackBb, asgFallbackStmt);
                gtSetStmtInfo(asgFallbackStmt);
                fgSetStmtSeq(asgFallbackStmt);
                gtUpdateTreeAncestorsSideEffects(fallbackCall);

                // Fast-path basic block
                BasicBlock* fastPathBb = fgNewBBafter(BBJ_ALWAYS, nullcheckBb, true);
                fastPathBb->bbFlags |= BBF_INTERNAL;
                Statement* asgFastPathValueStmt =
                    fgNewStmtFromTree(gtNewAssignNode(gtClone(rtLookupLcl), fastPathValueClone));
                asgFastPathValueStmt->SetDebugInfo(debugInfo);
                fgInsertStmtAtBeg(fastPathBb, asgFastPathValueStmt);
                gtSetStmtInfo(asgFastPathValueStmt);
                fgSetStmtSeq(asgFastPathValueStmt);

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

                    sizeCheckBb = fgNewBBbefore(BBJ_COND, nullcheckBb, true);
                    sizeCheckBb->bbFlags |= BBF_INTERNAL;

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
                    gtSetEvalOrder(sizeCheck);
                    Statement* sizeCheckStmt = fgNewStmtFromTree(gtNewOperNode(GT_JTRUE, TYP_VOID, sizeCheck));
                    sizeCheckStmt->SetDebugInfo(debugInfo);
                    gtSetStmtInfo(sizeCheckStmt);
                    fgSetStmtSeq(sizeCheckStmt);
                    fgInsertStmtAtEnd(sizeCheckBb, sizeCheckStmt);
                }

                // Replace call with rtLookupLclNum local
                call->ReplaceWith(gtNewLclvNode(rtLookupLclNum, call->TypeGet()), this);
                gtUpdateTreeAncestorsSideEffects(call);
                gtSetStmtInfo(stmt);
                fgSetStmtSeq(stmt);

                // Connect all new blocks together
                fgRemoveRefPred(block, prevBb);
                fgAddRefPred(block, fastPathBb);
                fgAddRefPred(block, fallbackBb);
                nullcheckBb->bbJumpDest = fallbackBb;
                fastPathBb->bbJumpDest  = block;

                if (needsSizeCheck)
                {
                    // Size check is the first block after prevBb
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

                // Some quick validation
                assert(prevBb->NumSucc() == 1);
                if (needsSizeCheck)
                {
                    assert(prevBb->GetSucc(0) == sizeCheckBb);
                    assert(sizeCheckBb->NumSucc() == 2);
                }
                else
                {
                    assert(prevBb->GetSucc(0) == nullcheckBb);
                }
                assert(nullcheckBb->NumSucc() == 2);
                assert(fastPathBb->NumSucc() == 1);
                assert(fallbackBb->NumSucc() == 1);
                assert(fastPathBb->GetSucc(0) == block);
                assert(fallbackBb->GetSucc(0) == block);

                // Re-distribute weights (see '[weight: X]' on the diagrams above)

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

                // Update loop info
                if (prevBb->bbNatLoopNum != BasicBlock::NOT_IN_LOOP)
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
                block  = prevBb;
                goto TRAVERSE_BLOCK_AGAIN;
            }
            prevStmt = stmt;
        }
    }

    if (result == PhaseStatus::MODIFIED_EVERYTHING)
    {
        if (opts.OptimizationEnabled())
        {
            fgReorderBlocks(/* useProfileData */ false);
            fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_BASICS);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\n*************** After fgExpandRuntimeLookups()\n");
            fgDispBasicBlocks(true);
        }
#endif
    }

    return result;
}
