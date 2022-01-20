// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph EH Optimizations

//------------------------------------------------------------------------
// fgRemoveEmptyFinally: Remove try/finallys where the finally is empty
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//    Removes all try/finallys in the method with empty finallys.
//    These typically arise from inlining empty Dispose methods.
//
//    Converts callfinally to a jump to the finally continuation.
//    Removes the finally, and reparents all blocks in the try to the
//    enclosing try or method region.
//
//    Currently limited to trivially empty finallys: those with one basic
//    block containing only single RETFILT statement. It is possible but
//    not likely that more complex-looking finallys will eventually become
//    empty (from say subsequent optimization). An SPMI run with
//    just the "detection" part of this phase run after optimization
//    found only one example where a new empty finally was detected.
//
PhaseStatus Compiler::fgRemoveEmptyFinally()
{
#if defined(FEATURE_EH_FUNCLETS)
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to remove.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with minOpts, no removal.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.compDbgCode)
    {
        JITDUMP("Method compiled with debug codegen, no removal.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** Before fgRemoveEmptyFinally()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif // DEBUG

    // Look for finallys or faults that are empty.
    unsigned finallyCount = 0;
    unsigned emptyCount   = 0;
    unsigned XTnum        = 0;
    while (XTnum < compHndBBtabCount)
    {
        EHblkDsc* const HBtab = &compHndBBtab[XTnum];

        // Check if this is a try/finally.  We could also look for empty
        // try/fault but presumably those are rare.
        if (!HBtab->HasFinallyHandler())
        {
            JITDUMP("EH#%u is not a try-finally; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        finallyCount++;

        // Look at blocks involved.
        BasicBlock* const firstBlock = HBtab->ebdHndBeg;
        BasicBlock* const lastBlock  = HBtab->ebdHndLast;

        // Limit for now to finallys that are single blocks.
        if (firstBlock != lastBlock)
        {
            JITDUMP("EH#%u finally has multiple basic blocks; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // If the finally's block jumps back to itself, then it is not empty.
        if ((firstBlock->bbJumpKind == BBJ_ALWAYS) && firstBlock->bbJumpDest == firstBlock)
        {
            JITDUMP("EH#%u finally has basic block that jumps to itself; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // Limit for now to finallys that contain only a GT_RETFILT.
        bool isEmpty = true;

        for (Statement* const stmt : firstBlock->Statements())
        {
            GenTree* stmtExpr = stmt->GetRootNode();

            if (stmtExpr->gtOper != GT_RETFILT)
            {
                isEmpty = false;
                break;
            }
        }

        if (!isEmpty)
        {
            JITDUMP("EH#%u finally is not empty; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        JITDUMP("EH#%u has empty finally, removing the region.\n", XTnum);

        // Find all the call finallys that invoke this finally,
        // and modify them to jump to the return point.
        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* endCallFinallyRangeBlock   = nullptr;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &endCallFinallyRangeBlock);

        BasicBlock* currentBlock = firstCallFinallyRangeBlock;

        while (currentBlock != endCallFinallyRangeBlock)
        {
            BasicBlock* nextBlock = currentBlock->bbNext;

            if ((currentBlock->bbJumpKind == BBJ_CALLFINALLY) && (currentBlock->bbJumpDest == firstBlock))
            {
                // Retarget the call finally to jump to the return
                // point.
                //
                // We don't expect to see retless finallys here, since
                // the finally is empty.
                noway_assert(currentBlock->isBBCallAlwaysPair());

                BasicBlock* const leaveBlock          = currentBlock->bbNext;
                BasicBlock* const postTryFinallyBlock = leaveBlock->bbJumpDest;

                JITDUMP("Modifying callfinally " FMT_BB " leave " FMT_BB " finally " FMT_BB " continuation " FMT_BB
                        "\n",
                        currentBlock->bbNum, leaveBlock->bbNum, firstBlock->bbNum, postTryFinallyBlock->bbNum);
                JITDUMP("so that " FMT_BB " jumps to " FMT_BB "; then remove " FMT_BB "\n", currentBlock->bbNum,
                        postTryFinallyBlock->bbNum, leaveBlock->bbNum);

                noway_assert(leaveBlock->bbJumpKind == BBJ_ALWAYS);

                currentBlock->bbJumpDest = postTryFinallyBlock;
                currentBlock->bbJumpKind = BBJ_ALWAYS;

                // Ref count updates.
                fgAddRefPred(postTryFinallyBlock, currentBlock);
                // fgRemoveRefPred(firstBlock, currentBlock);

                // Delete the leave block, which should be marked as
                // keep always.
                assert((leaveBlock->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0);
                nextBlock = leaveBlock->bbNext;

                leaveBlock->bbFlags &= ~BBF_KEEP_BBJ_ALWAYS;
                fgRemoveBlock(leaveBlock, true);

                // Cleanup the postTryFinallyBlock
                fgCleanupContinuation(postTryFinallyBlock);

                // Make sure iteration isn't going off the deep end.
                assert(leaveBlock != endCallFinallyRangeBlock);
            }

            currentBlock = nextBlock;
        }

        JITDUMP("Remove now-unreachable handler " FMT_BB "\n", firstBlock->bbNum);

        // Handler block should now be unreferenced, since the only
        // explicit references to it were in call finallys.
        firstBlock->bbRefs = 0;

        // Remove the handler block.
        const bool unreachable = true;
        firstBlock->bbFlags &= ~BBF_DONT_REMOVE;
        fgRemoveBlock(firstBlock, unreachable);

        // Find enclosing try region for the try, if any, and update
        // the try region. Note the handler region (if any) won't
        // change.
        BasicBlock* const firstTryBlock = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock  = HBtab->ebdTryLast;
        assert(firstTryBlock->getTryIndex() == XTnum);

        for (BasicBlock* const block : Blocks(firstTryBlock))
        {
            // Look for blocks directly contained in this try, and
            // update the try region appropriately.
            //
            // Try region for blocks transitively contained (say in a
            // child try) will get updated by the subsequent call to
            // fgRemoveEHTableEntry.
            if (block->getTryIndex() == XTnum)
            {
                if (firstBlock->hasTryIndex())
                {
                    block->setTryIndex(firstBlock->getTryIndex());
                }
                else
                {
                    block->clearTryIndex();
                }
            }

            if (block == firstTryBlock)
            {
                assert((block->bbFlags & BBF_TRY_BEG) != 0);
                block->bbFlags &= ~BBF_TRY_BEG;
            }

            if (block == lastTryBlock)
            {
                break;
            }
        }

        // Remove the try-finally EH region. This will compact the EH table
        // so XTnum now points at the next entry.
        fgRemoveEHTableEntry(XTnum);

        emptyCount++;
    }

    if (emptyCount > 0)
    {
        JITDUMP("fgRemoveEmptyFinally() removed %u try-finally clauses from %u finallys\n", emptyCount, finallyCount);
        fgOptimizedFinally = true;

#ifdef DEBUG
        if (verbose)
        {
            printf("\n*************** After fgRemoveEmptyFinally()\n");
            fgDispBasicBlocks();
            fgDispHandlerTab();
            printf("\n");
        }

#endif // DEBUG
    }

    return (emptyCount > 0) ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgRemoveEmptyTry: Optimize try/finallys where the try is empty
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//    In runtimes where thread abort is not possible, `try {} finally {S}`
//    can be optimized to simply `S`. This method looks for such
//    cases and removes the try-finally from the EH table, making
//    suitable flow, block flag, statement, and region updates.
//
//    This optimization is not legal in runtimes that support thread
//    abort because those runtimes ensure that a finally is completely
//    executed before continuing to process the thread abort.  With
//    this optimization, the code block `S` can lose special
//    within-finally status and so complete execution is no longer
//    guaranteed.
//
PhaseStatus Compiler::fgRemoveEmptyTry()
{
    JITDUMP("\n*************** In fgRemoveEmptyTry()\n");

#if defined(FEATURE_EH_FUNCLETS)
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

    bool enableRemoveEmptyTry = true;

#ifdef DEBUG
    // Allow override to enable/disable.
    enableRemoveEmptyTry = (JitConfig.JitEnableRemoveEmptyTry() == 1);
#endif // DEBUG

    if (!enableRemoveEmptyTry)
    {
        JITDUMP("Empty try removal disabled.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to remove.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with minOpts, no removal.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.compDbgCode)
    {
        JITDUMP("Method compiled with debug codegen, no removal.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** Before fgRemoveEmptyTry()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif // DEBUG

    // Look for try-finallys where the try is empty.
    unsigned emptyCount = 0;
    unsigned XTnum      = 0;
    while (XTnum < compHndBBtabCount)
    {
        EHblkDsc* const HBtab = &compHndBBtab[XTnum];

        // Check if this is a try/finally.  We could also look for empty
        // try/fault but presumably those are rare.
        if (!HBtab->HasFinallyHandler())
        {
            JITDUMP("EH#%u is not a try-finally; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // Examine the try region
        BasicBlock* const firstTryBlock     = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock      = HBtab->ebdTryLast;
        BasicBlock* const firstHandlerBlock = HBtab->ebdHndBeg;
        BasicBlock* const lastHandlerBlock  = HBtab->ebdHndLast;

        assert(firstTryBlock->getTryIndex() == XTnum);

        // Limit for now to trys that contain only a callfinally pair
        // or branch to same.
        if (!firstTryBlock->isEmpty())
        {
            JITDUMP("EH#%u first try block " FMT_BB " not empty; skipping.\n", XTnum, firstTryBlock->bbNum);
            XTnum++;
            continue;
        }

#if FEATURE_EH_CALLFINALLY_THUNKS

        // Look for blocks that are always jumps to a call finally
        // pair that targets the finally
        if (firstTryBlock->bbJumpKind != BBJ_ALWAYS)
        {
            JITDUMP("EH#%u first try block " FMT_BB " not jump to a callfinally; skipping.\n", XTnum,
                    firstTryBlock->bbNum);
            XTnum++;
            continue;
        }

        BasicBlock* const callFinally = firstTryBlock->bbJumpDest;

        // Look for call always pair. Note this will also disqualify
        // empty try removal in cases where the finally doesn't
        // return.
        if (!callFinally->isBBCallAlwaysPair() || (callFinally->bbJumpDest != firstHandlerBlock))
        {
            JITDUMP("EH#%u first try block " FMT_BB " always jumps but not to a callfinally; skipping.\n", XTnum,
                    firstTryBlock->bbNum);
            XTnum++;
            continue;
        }

        // Try itself must be a single block.
        if (firstTryBlock != lastTryBlock)
        {
            JITDUMP("EH#%u first try block " FMT_BB " not only block in try; skipping.\n", XTnum,
                    firstTryBlock->bbNext->bbNum);
            XTnum++;
            continue;
        }

#else
        // Look for call always pair within the try itself. Note this
        // will also disqualify empty try removal in cases where the
        // finally doesn't return.
        if (!firstTryBlock->isBBCallAlwaysPair() || (firstTryBlock->bbJumpDest != firstHandlerBlock))
        {
            JITDUMP("EH#%u first try block " FMT_BB " not a callfinally; skipping.\n", XTnum, firstTryBlock->bbNum);
            XTnum++;
            continue;
        }

        BasicBlock* const callFinally = firstTryBlock;

        // Try must be a callalways pair of blocks.
        if (firstTryBlock->bbNext != lastTryBlock)
        {
            JITDUMP("EH#%u block " FMT_BB " not last block in try; skipping.\n", XTnum, firstTryBlock->bbNext->bbNum);
            XTnum++;
            continue;
        }

#endif // FEATURE_EH_CALLFINALLY_THUNKS

        JITDUMP("EH#%u has empty try, removing the try region and promoting the finally.\n", XTnum);

        // There should be just one callfinally that invokes this
        // finally, the one we found above. Verify this.
        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* endCallFinallyRangeBlock   = nullptr;
        bool        verifiedSingleCallfinally  = true;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &endCallFinallyRangeBlock);

        for (BasicBlock* block = firstCallFinallyRangeBlock; block != endCallFinallyRangeBlock; block = block->bbNext)
        {
            if ((block->bbJumpKind == BBJ_CALLFINALLY) && (block->bbJumpDest == firstHandlerBlock))
            {
                assert(block->isBBCallAlwaysPair());

                if (block != callFinally)
                {
                    JITDUMP("EH#%u found unexpected callfinally " FMT_BB "; skipping.\n");
                    verifiedSingleCallfinally = false;
                    break;
                }

                block = block->bbNext;
            }
        }

        if (!verifiedSingleCallfinally)
        {
            JITDUMP("EH#%u -- unexpectedly -- has multiple callfinallys; skipping.\n");
            XTnum++;
            assert(verifiedSingleCallfinally);
            continue;
        }

        // Time to optimize.
        //
        // (1) Convert the callfinally to a normal jump to the handler
        callFinally->bbJumpKind = BBJ_ALWAYS;

        // Identify the leave block and the continuation
        BasicBlock* const leave        = callFinally->bbNext;
        BasicBlock* const continuation = leave->bbJumpDest;

        // (2) Cleanup the leave so it can be deleted by subsequent opts
        assert((leave->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0);
        leave->bbFlags &= ~BBF_KEEP_BBJ_ALWAYS;

        // (3) Cleanup the continuation
        fgCleanupContinuation(continuation);

        // (4) Find enclosing try region for the try, if any, and
        // update the try region for the blocks in the try. Note the
        // handler region (if any) won't change.
        //
        // Kind of overkill to loop here, but hey.
        for (BasicBlock* const block : Blocks(firstTryBlock))
        {
            // Look for blocks directly contained in this try, and
            // update the try region appropriately.
            //
            // The try region for blocks transitively contained (say in a
            // child try) will get updated by the subsequent call to
            // fgRemoveEHTableEntry.
            if (block->getTryIndex() == XTnum)
            {
                if (firstHandlerBlock->hasTryIndex())
                {
                    block->setTryIndex(firstHandlerBlock->getTryIndex());
                }
                else
                {
                    block->clearTryIndex();
                }
            }

            if (block == firstTryBlock)
            {
                assert((block->bbFlags & BBF_TRY_BEG) != 0);
                block->bbFlags &= ~BBF_TRY_BEG;
            }

            if (block == lastTryBlock)
            {
                break;
            }
        }

        // (5) Update the directly contained handler blocks' handler index.
        // Handler index of any nested blocks will update when we
        // remove the EH table entry.  Change handler exits to jump to
        // the continuation.  Clear catch type on handler entry.
        // Decrement nesting level of enclosed GT_END_LFINs.
        for (BasicBlock* const block : Blocks(firstHandlerBlock, lastHandlerBlock))
        {
            if (block == firstHandlerBlock)
            {
                block->bbCatchTyp = BBCT_NONE;
            }

            if (block->getHndIndex() == XTnum)
            {
                if (firstTryBlock->hasHndIndex())
                {
                    block->setHndIndex(firstTryBlock->getHndIndex());
                }
                else
                {
                    block->clearHndIndex();
                }

                if (block->bbJumpKind == BBJ_EHFINALLYRET)
                {
                    Statement* finallyRet     = block->lastStmt();
                    GenTree*   finallyRetExpr = finallyRet->GetRootNode();
                    assert(finallyRetExpr->gtOper == GT_RETFILT);
                    fgRemoveStmt(block, finallyRet);
                    block->bbJumpKind = BBJ_ALWAYS;
                    block->bbJumpDest = continuation;
                    fgAddRefPred(continuation, block);
                }
            }

#if !defined(FEATURE_EH_FUNCLETS)
            // If we're in a non-funclet model, decrement the nesting
            // level of any GT_END_LFIN we find in the handler region,
            // since we're removing the enclosing handler.
            for (Statement* const stmt : block->Statements())
            {
                GenTree* expr = stmt->GetRootNode();
                if (expr->gtOper == GT_END_LFIN)
                {
                    const size_t nestLevel = expr->AsVal()->gtVal1;
                    assert(nestLevel > 0);
                    expr->AsVal()->gtVal1 = nestLevel - 1;
                }
            }
#endif // !FEATURE_EH_FUNCLETS
        }

        // (6) Remove the try-finally EH region. This will compact the
        // EH table so XTnum now points at the next entry and will update
        // the EH region indices of any nested EH in the (former) handler.
        fgRemoveEHTableEntry(XTnum);

        // Another one bites the dust...
        emptyCount++;
    }

    if (emptyCount > 0)
    {
        JITDUMP("fgRemoveEmptyTry() optimized %u empty-try try-finally clauses\n", emptyCount);
        fgOptimizedFinally = true;
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgCloneFinally: Optimize normal exit path from a try/finally
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//    Handles finallys that are not enclosed by or enclosing other
//    handler regions.
//
//    Converts the "normal exit" callfinally to a jump to a cloned copy
//    of the finally, which in turn jumps to the finally continuation.
//
//    If all callfinallys for a given finally are converted to jump to
//    the clone, the try-finally is modified into a try-fault,
//    distingushable from organic try-faults by handler type
//    EH_HANDLER_FAULT_WAS_FINALLY vs the organic EH_HANDLER_FAULT.
//
//    Does not yet handle thread abort. The open issues here are how
//    to maintain the proper description of the cloned finally blocks
//    as a handler (for thread abort purposes), how to prevent code
//    motion in or out of these blocks, and how to report this cloned
//    handler to the runtime. Some building blocks for thread abort
//    exist (see below) but more work needed.
//
//    The first and last blocks of the cloned finally are marked with
//    BBF_CLONED_FINALLY_BEGIN and BBF_CLONED_FINALLY_END. However
//    these markers currently can get lost during subsequent
//    optimizations.
//
PhaseStatus Compiler::fgCloneFinally()
{
#if defined(FEATURE_EH_FUNCLETS)
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

    bool enableCloning = true;

#ifdef DEBUG
    // Allow override to enable/disable.
    enableCloning = (JitConfig.JitEnableFinallyCloning() == 1);
#endif // DEBUG

    if (!enableCloning)
    {
        JITDUMP("Finally cloning disabled.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, no cloning.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with minOpts, no cloning.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.compDbgCode)
    {
        JITDUMP("Method compiled with debug codegen, no cloning.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }

    // Verify try-finally exits look good before we start.
    fgDebugCheckTryFinallyExits();

#endif // DEBUG

    // Look for finallys that are not contained within other handlers,
    // and which do not themselves contain EH.
    //
    // Note these cases potentially could be handled, but are less
    // obviously profitable and require modification of the handler
    // table.
    unsigned  XTnum      = 0;
    EHblkDsc* HBtab      = compHndBBtab;
    unsigned  cloneCount = 0;
    for (; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        // Check if this is a try/finally
        if (!HBtab->HasFinallyHandler())
        {
            JITDUMP("EH#%u is not a try-finally; skipping.\n", XTnum);
            continue;
        }

        // Check if enclosed by another handler.
        const unsigned enclosingHandlerRegion = ehGetEnclosingHndIndex(XTnum);

        if (enclosingHandlerRegion != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            JITDUMP("EH#%u is enclosed by handler EH#%u; skipping.\n", XTnum, enclosingHandlerRegion);
            continue;
        }

        bool     containsEH                   = false;
        unsigned exampleEnclosedHandlerRegion = 0;

        // Only need to look at lower numbered regions because the
        // handler table is ordered by nesting.
        for (unsigned i = 0; i < XTnum; i++)
        {
            if (ehGetEnclosingHndIndex(i) == XTnum)
            {
                exampleEnclosedHandlerRegion = i;
                containsEH                   = true;
                break;
            }
        }

        if (containsEH)
        {
            JITDUMP("Finally for EH#%u encloses handler EH#%u; skipping.\n", XTnum, exampleEnclosedHandlerRegion);
            continue;
        }

        // Look at blocks involved.
        BasicBlock* const firstBlock = HBtab->ebdHndBeg;
        BasicBlock* const lastBlock  = HBtab->ebdHndLast;
        assert(firstBlock != nullptr);
        assert(lastBlock != nullptr);
        BasicBlock* nextBlock       = lastBlock->bbNext;
        unsigned    regionBBCount   = 0;
        unsigned    regionStmtCount = 0;
        bool        hasFinallyRet   = false;
        bool        isAllRare       = true;
        bool        hasSwitch       = false;

        for (const BasicBlock* block = firstBlock; block != nextBlock; block = block->bbNext)
        {
            if (block->bbJumpKind == BBJ_SWITCH)
            {
                hasSwitch = true;
                break;
            }

            regionBBCount++;

            // Should we compute statement cost here, or is it
            // premature...? For now just count statements I guess.
            for (Statement* const stmt : block->Statements())
            {
                regionStmtCount++;
            }

            hasFinallyRet = hasFinallyRet || (block->bbJumpKind == BBJ_EHFINALLYRET);
            isAllRare     = isAllRare && block->isRunRarely();
        }

        // Skip cloning if the finally has a switch.
        if (hasSwitch)
        {
            JITDUMP("Finally in EH#%u has a switch; skipping.\n", XTnum);
            continue;
        }

        // Skip cloning if the finally must throw.
        if (!hasFinallyRet)
        {
            JITDUMP("Finally in EH#%u does not return; skipping.\n", XTnum);
            continue;
        }

        // Skip cloning if the finally is rarely run code.
        if (isAllRare)
        {
            JITDUMP("Finally in EH#%u is run rarely; skipping.\n", XTnum);
            continue;
        }

        // Empirical studies from CoreCLR and CoreFX show that less
        // that 1% of finally regions have more than 15
        // statements. So, to avoid potentially excessive code growth,
        // only clone finallys that have 15 or fewer statements.
        const unsigned stmtCountLimit = 15;
        if (regionStmtCount > stmtCountLimit)
        {
            JITDUMP("Finally in EH#%u has %u statements, limit is %u; skipping.\n", XTnum, regionStmtCount,
                    stmtCountLimit);
            continue;
        }

        JITDUMP("EH#%u is a candidate for finally cloning:"
                " %u blocks, %u statements\n",
                XTnum, regionBBCount, regionStmtCount);

        // Walk the try region backwards looking for blocks
        // that transfer control to a callfinally.
        //
        // For non-pgo we find the lexically last block; for
        // pgo we find the highest-weight block.
        //
        BasicBlock* const firstTryBlock = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock  = HBtab->ebdTryLast;
        assert(firstTryBlock->getTryIndex() == XTnum);
        assert(bbInTryRegions(XTnum, lastTryBlock));
        BasicBlock* const beforeTryBlock = firstTryBlock->bbPrev;

        BasicBlock* normalCallFinallyBlock   = nullptr;
        BasicBlock* normalCallFinallyReturn  = nullptr;
        BasicBlock* cloneInsertAfter         = HBtab->ebdTryLast;
        bool        tryToRelocateCallFinally = false;
        const bool  usingProfileWeights      = fgIsUsingProfileWeights();
        weight_t    currentWeight            = BB_ZERO_WEIGHT;

        for (BasicBlock* block = lastTryBlock; block != beforeTryBlock; block = block->bbPrev)
        {
#if FEATURE_EH_CALLFINALLY_THUNKS
            // Blocks that transfer control to callfinallies are usually
            // BBJ_ALWAYS blocks, but the last block of a try may fall
            // through to a callfinally.
            BasicBlock* jumpDest = nullptr;

            if ((block->bbJumpKind == BBJ_NONE) && (block == lastTryBlock))
            {
                jumpDest = block->bbNext;
            }
            else if (block->bbJumpKind == BBJ_ALWAYS)
            {
                jumpDest = block->bbJumpDest;
            }

            if (jumpDest == nullptr)
            {
                continue;
            }

            // The jumpDest must be a callfinally that in turn invokes the
            // finally of interest.
            if (!jumpDest->isBBCallAlwaysPair() || (jumpDest->bbJumpDest != firstBlock))
            {
                continue;
            }
#else
            // Look for call finally pair directly within the try
            if (!block->isBBCallAlwaysPair() || (block->bbJumpDest != firstBlock))
            {
                continue;
            }

            BasicBlock* const jumpDest = block;
#endif // FEATURE_EH_CALLFINALLY_THUNKS

            // Found a block that invokes the finally.
            //
            BasicBlock* const finallyReturnBlock  = jumpDest->bbNext;
            BasicBlock* const postTryFinallyBlock = finallyReturnBlock->bbJumpDest;
            bool              isUpdate            = false;

            // See if this is the one we want to use to inspire cloning.
            //
            if (normalCallFinallyBlock == nullptr)
            {
                normalCallFinallyBlock  = jumpDest;
                normalCallFinallyReturn = postTryFinallyBlock;

                if (usingProfileWeights)
                {
                    if (block->hasProfileWeight())
                    {
                        JITDUMP("Found profiled " FMT_BB " with weight " FMT_WT "\n", block->bbNum, block->bbWeight);
                        currentWeight = block->bbWeight;
                    }
                    else
                    {
                        JITDUMP("Found unprofiled " FMT_BB "\n", block->bbNum);
                    }
                }
            }
            else
            {
                assert(usingProfileWeights);

                if (!block->hasProfileWeight())
                {
                    // An unprofiled block in method with profile data.
                    // We generally don't expect to see these as the
                    // blocks in EH regions must have come from the root
                    // method, which we know has profile data.
                    // Just skip over them for now.
                    //
                    JITDUMP("Skipping past unprofiled " FMT_BB "\n", block->bbNum);
                    continue;
                }

                if (block->bbWeight <= currentWeight)
                {
                    JITDUMP("Skipping past " FMT_BB " with weight " FMT_WT "\n", block->bbNum, block->bbWeight);
                    continue;
                }

                // Prefer this block.
                //
                JITDUMP("Preferring " FMT_BB " since " FMT_WT " >  " FMT_WT "\n", block->bbNum, block->bbWeight,
                        currentWeight);
                normalCallFinallyBlock  = jumpDest;
                normalCallFinallyReturn = postTryFinallyBlock;
                currentWeight           = block->bbWeight;
                isUpdate                = true;
            }

#if FEATURE_EH_CALLFINALLY_THUNKS
            // When there are callfinally thunks, we don't expect to see the
            // callfinally within a handler region either.
            assert(!jumpDest->hasHndIndex());

            // Update the clone insertion point to just after the
            // call always pair.
            cloneInsertAfter = finallyReturnBlock;

            // We will consider moving the callfinally so we can fall
            // through from the try into the clone.
            tryToRelocateCallFinally = true;

            JITDUMP("%s path to clone: try block " FMT_BB " jumps to callfinally at " FMT_BB ";"
                    " the call returns to " FMT_BB " which jumps to " FMT_BB "\n",
                    isUpdate ? "Updating" : "Choosing", block->bbNum, jumpDest->bbNum, finallyReturnBlock->bbNum,
                    postTryFinallyBlock->bbNum);
#else
            JITDUMP("%s path to clone: try block " FMT_BB " is a callfinally;"
                    " the call returns to " FMT_BB " which jumps to " FMT_BB "\n",
                    isUpdate ? "Updating" : "Choosing", block->bbNum, finallyReturnBlock->bbNum,
                    postTryFinallyBlock->bbNum);
#endif // FEATURE_EH_CALLFINALLY_THUNKS

            // For non-pgo just take the first one we find.
            // For pgo, keep searching in case we find one we like better.
            //
            if (!usingProfileWeights)
            {
                break;
            }
        }

        // If there is no call to the finally, don't clone.
        if (normalCallFinallyBlock == nullptr)
        {
            JITDUMP("EH#%u: no calls from the try to the finally, skipping.\n", XTnum);
            continue;
        }

        JITDUMP("Will update callfinally block " FMT_BB " to jump to the clone;"
                " clone will jump to " FMT_BB "\n",
                normalCallFinallyBlock->bbNum, normalCallFinallyReturn->bbNum);

        // If there are multiple callfinallys and we're in the
        // callfinally thunk model, all the callfinallys are placed
        // just outside the try region. We'd like our chosen
        // callfinally to come first after the try, so we can fall out of the try
        // into the clone.
        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* endCallFinallyRangeBlock   = nullptr;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &endCallFinallyRangeBlock);

        if (tryToRelocateCallFinally)
        {
            BasicBlock* firstCallFinallyBlock = nullptr;

            for (BasicBlock* block = firstCallFinallyRangeBlock; block != endCallFinallyRangeBlock;
                 block             = block->bbNext)
            {
                if (block->isBBCallAlwaysPair())
                {
                    if (block->bbJumpDest == firstBlock)
                    {
                        firstCallFinallyBlock = block;
                        break;
                    }
                }
            }

            // We better have found at least one call finally.
            assert(firstCallFinallyBlock != nullptr);

            // If there is more than one callfinally, we'd like to move
            // the one we are going to retarget to be first in the callfinally,
            // but only if it's targeted by the last block in the try range.
            if (firstCallFinallyBlock != normalCallFinallyBlock)
            {
                BasicBlock* const placeToMoveAfter = firstCallFinallyBlock->bbPrev;

                if ((placeToMoveAfter->bbJumpKind == BBJ_ALWAYS) &&
                    (placeToMoveAfter->bbJumpDest == normalCallFinallyBlock))
                {
                    JITDUMP("Moving callfinally " FMT_BB " to be first in line, before " FMT_BB "\n",
                            normalCallFinallyBlock->bbNum, firstCallFinallyBlock->bbNum);

                    BasicBlock* const firstToMove = normalCallFinallyBlock;
                    BasicBlock* const lastToMove  = normalCallFinallyBlock->bbNext;

                    fgUnlinkRange(firstToMove, lastToMove);
                    fgMoveBlocksAfter(firstToMove, lastToMove, placeToMoveAfter);

#ifdef DEBUG
                    // Sanity checks
                    fgDebugCheckBBlist(false, false);
                    fgVerifyHandlerTab();
#endif // DEBUG

                    assert(nextBlock == lastBlock->bbNext);

                    // Update where the callfinally range begins, since we might
                    // have altered this with callfinally rearrangement, and/or
                    // the range begin might have been pretty loose to begin with.
                    firstCallFinallyRangeBlock = normalCallFinallyBlock;
                }
                else
                {
                    JITDUMP("Can't move callfinally " FMT_BB " to be first in line"
                            " -- last finally block " FMT_BB " doesn't jump to it\n",
                            normalCallFinallyBlock->bbNum, placeToMoveAfter->bbNum);
                }
            }
        }

        // Clone the finally and retarget the normal return path and
        // any other path that happens to share that same return
        // point. For instance a construct like:
        //
        //  try { } catch { } finally { }
        //
        // will have two call finally blocks, one for the normal exit
        // from the try, and the the other for the exit from the
        // catch. They'll both pass the same return point which is the
        // statement after the finally, so they can share the clone.
        //
        // Clone the finally body, and splice it into the flow graph
        // within in the parent region of the try.
        //
        const unsigned  finallyTryIndex = firstBlock->bbTryIndex;
        BasicBlock*     insertAfter     = nullptr;
        BlockToBlockMap blockMap(getAllocator());
        bool            clonedOk       = true;
        unsigned        cloneBBCount   = 0;
        weight_t const  originalWeight = firstBlock->hasProfileWeight() ? firstBlock->bbWeight : BB_ZERO_WEIGHT;

        for (BasicBlock* block = firstBlock; block != nextBlock; block = block->bbNext)
        {
            BasicBlock* newBlock;

            if (block == firstBlock)
            {
                // Put first cloned finally block into the appropriate
                // region, somewhere within or after the range of
                // callfinallys, depending on the EH implementation.
                const unsigned    hndIndex = 0;
                BasicBlock* const nearBlk  = cloneInsertAfter;
                newBlock                   = fgNewBBinRegion(block->bbJumpKind, finallyTryIndex, hndIndex, nearBlk);

                // If the clone ends up just after the finally, adjust
                // the stopping point for finally traversal.
                if (newBlock->bbNext == nextBlock)
                {
                    assert(newBlock->bbPrev == lastBlock);
                    nextBlock = newBlock;
                }
            }
            else
            {
                // Put subsequent blocks in the same region...
                const bool extendRegion = true;
                newBlock                = fgNewBBafter(block->bbJumpKind, insertAfter, extendRegion);
            }

            cloneBBCount++;
            assert(cloneBBCount <= regionBBCount);

            insertAfter = newBlock;
            blockMap.Set(block, newBlock);

            clonedOk = BasicBlock::CloneBlockState(this, newBlock, block);

            if (!clonedOk)
            {
                break;
            }

            // Update block flags. Note a block can be both first and last.
            if (block == firstBlock)
            {
                // Mark the block as the start of the cloned finally.
                newBlock->bbFlags |= BBF_CLONED_FINALLY_BEGIN;
            }

            if (block == lastBlock)
            {
                // Mark the block as the end of the cloned finally.
                newBlock->bbFlags |= BBF_CLONED_FINALLY_END;
            }

            // Make sure clone block state hasn't munged the try region.
            assert(newBlock->bbTryIndex == finallyTryIndex);

            // Cloned handler block is no longer within the handler.
            newBlock->clearHndIndex();

            // Jump dests are set in a post-pass; make sure CloneBlockState hasn't tried to set them.
            assert(newBlock->bbJumpDest == nullptr);
        }

        if (!clonedOk)
        {
            // TODO: cleanup the partial clone?
            JITDUMP("Unable to clone the finally; skipping.\n");
            continue;
        }

        // We should have cloned all the finally region blocks.
        assert(cloneBBCount == regionBBCount);

        JITDUMP("Cloned finally blocks are: " FMT_BB " ... " FMT_BB "\n", blockMap[firstBlock]->bbNum,
                blockMap[lastBlock]->bbNum);

        // Redirect redirect any branches within the newly-cloned
        // finally, and any finally returns to jump to the return
        // point.
        for (BasicBlock* block = firstBlock; block != nextBlock; block = block->bbNext)
        {
            BasicBlock* newBlock = blockMap[block];

            if (block->bbJumpKind == BBJ_EHFINALLYRET)
            {
                Statement* finallyRet     = newBlock->lastStmt();
                GenTree*   finallyRetExpr = finallyRet->GetRootNode();
                assert(finallyRetExpr->gtOper == GT_RETFILT);
                fgRemoveStmt(newBlock, finallyRet);
                newBlock->bbJumpKind = BBJ_ALWAYS;
                newBlock->bbJumpDest = normalCallFinallyReturn;

                fgAddRefPred(normalCallFinallyReturn, newBlock);
            }
            else
            {
                optCopyBlkDest(block, newBlock);
                optRedirectBlock(newBlock, &blockMap);
            }
        }

        // Modify the targeting call finallys to branch to the cloned
        // finally. Make a note if we see some calls that can't be
        // retargeted (since they want to return to other places).
        BasicBlock* const firstCloneBlock    = blockMap[firstBlock];
        bool              retargetedAllCalls = true;
        BasicBlock*       currentBlock       = firstCallFinallyRangeBlock;
        weight_t          retargetedWeight   = BB_ZERO_WEIGHT;

        while (currentBlock != endCallFinallyRangeBlock)
        {
            BasicBlock* nextBlockToScan = currentBlock->bbNext;

            if (currentBlock->isBBCallAlwaysPair())
            {
                if (currentBlock->bbJumpDest == firstBlock)
                {
                    BasicBlock* const leaveBlock          = currentBlock->bbNext;
                    BasicBlock* const postTryFinallyBlock = leaveBlock->bbJumpDest;

                    // Note we must retarget all callfinallies that have this
                    // continuation, or we can't clean up the continuation
                    // block properly below, since it will be reachable both
                    // by the cloned finally and by the called finally.
                    if (postTryFinallyBlock == normalCallFinallyReturn)
                    {
                        JITDUMP("Retargeting callfinally " FMT_BB " to clone entry " FMT_BB "\n", currentBlock->bbNum,
                                firstCloneBlock->bbNum);

                        // This call returns to the expected spot, so
                        // retarget it to branch to the clone.
                        currentBlock->bbJumpDest = firstCloneBlock;
                        currentBlock->bbJumpKind = BBJ_ALWAYS;

                        // Ref count updates.
                        fgAddRefPred(firstCloneBlock, currentBlock);
                        // fgRemoveRefPred(firstBlock, currentBlock);

                        // Delete the leave block, which should be marked as
                        // keep always.
                        assert((leaveBlock->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0);
                        nextBlock = leaveBlock->bbNext;

                        leaveBlock->bbFlags &= ~BBF_KEEP_BBJ_ALWAYS;
                        fgRemoveBlock(leaveBlock, true);

                        // Make sure iteration isn't going off the deep end.
                        assert(leaveBlock != endCallFinallyRangeBlock);

                        if (currentBlock->hasProfileWeight())
                        {
                            retargetedWeight += currentBlock->bbWeight;
                        }
                    }
                    else
                    {
                        // We can't retarget this call since it
                        // returns somewhere else.
                        JITDUMP("Can't retarget callfinally in " FMT_BB " as it jumps to " FMT_BB ", not " FMT_BB "\n",
                                currentBlock->bbNum, postTryFinallyBlock->bbNum, normalCallFinallyReturn->bbNum);

                        retargetedAllCalls = false;
                    }
                }
            }

            currentBlock = nextBlockToScan;
        }

        // If we retargeted all calls, modify EH descriptor to be
        // try-fault instead of try-finally, and then non-cloned
        // finally catch type to be fault.
        if (retargetedAllCalls)
        {
            JITDUMP("All callfinallys retargeted; changing finally to fault.\n");
            HBtab->ebdHandlerType  = EH_HANDLER_FAULT_WAS_FINALLY;
            firstBlock->bbCatchTyp = BBCT_FAULT;
        }
        else
        {
            JITDUMP("Some callfinallys *not* retargeted, so region must remain as a finally.\n");
        }

        // Modify first block of cloned finally to be a "normal" block.
        BasicBlock* firstClonedBlock = blockMap[firstBlock];
        firstClonedBlock->bbCatchTyp = BBCT_NONE;

        // Cleanup the continuation
        fgCleanupContinuation(normalCallFinallyReturn);

        // If we have profile data, compute how the weights split,
        // and update the weights in both the clone and the original.
        //
        // TODO: if original weight is zero, we probably should forgo cloning...?
        //
        // TODO: it will frequently be the case that the original scale is 0.0 as
        // all the profiled flow will go to the clone.
        //
        // Decide if we really want to set all those counts to zero, and if so
        // whether we should mark the original as rarely run.
        //
        if (usingProfileWeights && (originalWeight > BB_ZERO_WEIGHT))
        {
            // We can't leave the finally more often than we enter.
            // So cap cloned scale at 1.0
            //
            weight_t const clonedScale = retargetedWeight < originalWeight ? (retargetedWeight / originalWeight) : 1.0;
            weight_t const originalScale = 1.0 - clonedScale;

            JITDUMP("Profile scale factor (" FMT_WT "/" FMT_WT ") => clone " FMT_WT " / original " FMT_WT "\n",
                    retargetedWeight, originalWeight, clonedScale, originalScale);

            for (BasicBlock* const block : Blocks(firstBlock, lastBlock))
            {
                if (block->hasProfileWeight())
                {
                    weight_t const blockWeight = block->bbWeight;
                    block->setBBProfileWeight(blockWeight * originalScale);
                    JITDUMP("Set weight of " FMT_BB " to " FMT_WT "\n", block->bbNum, block->bbWeight);

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
                    // Handle a special case -- some handler entries can't have zero profile count.
                    //
                    if (bbIsHandlerBeg(block) && block->isRunRarely())
                    {
                        JITDUMP("Suppressing zero count for " FMT_BB " as it is a handler entry\n", block->bbNum);
                        block->makeBlockHot();
                    }
#endif

                    BasicBlock* const clonedBlock = blockMap[block];
                    clonedBlock->setBBProfileWeight(blockWeight * clonedScale);
                    JITDUMP("Set weight of " FMT_BB " to " FMT_WT "\n", clonedBlock->bbNum, clonedBlock->bbWeight);
                }
            }
        }

        // Done!
        JITDUMP("\nDone with EH#%u\n\n", XTnum);
        cloneCount++;
    }

    if (cloneCount > 0)
    {
        JITDUMP("fgCloneFinally() cloned %u finally handlers\n", cloneCount);
        fgOptimizedFinally = true;

#ifdef DEBUG
        if (verbose)
        {
            fgDispBasicBlocks();
            fgDispHandlerTab();
            printf("\n");
        }

        fgDebugCheckTryFinallyExits();

#endif // DEBUG
    }

    return (cloneCount > 0 ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING);
}

#ifdef DEBUG

//------------------------------------------------------------------------
// fgDebugCheckTryFinallyExits: validate normal flow from try-finally
// or try-fault-was-finally.
//
// Notes:
//
// Normal control flow exiting the try block of a try-finally must
// pass through the finally. This checker attempts to verify that by
// looking at the control flow graph.
//
// Each path that exits the try of a try-finally (including try-faults
// that were optimized into try-finallys by fgCloneFinally) should
// thus either execute a callfinally to the associated finally or else
// jump to a block with the BBF_CLONED_FINALLY_BEGIN flag set.
//
// Depending on when this check is done, there may also be an empty
// block along the path.
//
// Depending on the model for invoking finallys, the callfinallies may
// lie within the try region (callfinally thunks) or in the enclosing
// region.

void Compiler::fgDebugCheckTryFinallyExits()
{
    unsigned  XTnum            = 0;
    EHblkDsc* HBtab            = compHndBBtab;
    bool      allTryExitsValid = true;
    for (; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        const EHHandlerType handlerType = HBtab->ebdHandlerType;
        const bool          isFinally   = (handlerType == EH_HANDLER_FINALLY);
        const bool          wasFinally  = (handlerType == EH_HANDLER_FAULT_WAS_FINALLY);

        // Screen out regions that are or were not finallys.
        if (!isFinally && !wasFinally)
        {
            continue;
        }

        // Walk blocks of the try, looking for normal control flow to
        // an ancestor region.

        BasicBlock* const firstTryBlock = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock  = HBtab->ebdTryLast;
        assert(firstTryBlock->getTryIndex() <= XTnum);
        assert(lastTryBlock->getTryIndex() <= XTnum);
        BasicBlock* const finallyBlock = isFinally ? HBtab->ebdHndBeg : nullptr;

        for (BasicBlock* const block : Blocks(firstTryBlock, lastTryBlock))
        {
            // Only check the directly contained blocks.
            assert(block->hasTryIndex());

            if (block->getTryIndex() != XTnum)
            {
                continue;
            }

            // Look at each of the normal control flow possibilities.
            for (BasicBlock* const succBlock : block->Succs())
            {
                if (succBlock->hasTryIndex() && succBlock->getTryIndex() <= XTnum)
                {
                    // Successor does not exit this try region.
                    continue;
                }

#if FEATURE_EH_CALLFINALLY_THUNKS

                // When there are callfinally thunks, callfinallies
                // logically "belong" to a child region and the exit
                // path validity will be checked when looking at the
                // try blocks in that region.
                if (block->bbJumpKind == BBJ_CALLFINALLY)
                {
                    continue;
                }

#endif // FEATURE_EH_CALLFINALLY_THUNKS

                // Now we know block lies directly within the try of a
                // try-finally, and succBlock is in an enclosing
                // region (possibly the method region). So this path
                // represents flow out of the try and should be
                // checked.
                //
                // There are various ways control can properly leave a
                // try-finally (or try-fault-was-finally):
                //
                // (a1) via a jump to a callfinally (only for finallys, only for call finally thunks)
                // (a2) via a callfinally (only for finallys, only for !call finally thunks)
                // (b) via a jump to a begin finally clone block
                // (c) via a jump to an empty block to (b)
                // (d) via a fallthrough to an empty block to (b)
                // (e) via the always half of a callfinally pair
                // (f) via an always jump clonefinally exit
                bool isCallToFinally = false;

#if FEATURE_EH_CALLFINALLY_THUNKS
                if (succBlock->bbJumpKind == BBJ_CALLFINALLY)
                {
                    // case (a1)
                    isCallToFinally = isFinally && (succBlock->bbJumpDest == finallyBlock);
                }
#else
                if (block->bbJumpKind == BBJ_CALLFINALLY)
                {
                    // case (a2)
                    isCallToFinally = isFinally && (block->bbJumpDest == finallyBlock);
                }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

                bool isJumpToClonedFinally = false;

                if (succBlock->bbFlags & BBF_CLONED_FINALLY_BEGIN)
                {
                    // case (b)
                    isJumpToClonedFinally = true;
                }
                else if (succBlock->bbJumpKind == BBJ_ALWAYS)
                {
                    if (succBlock->isEmpty())
                    {
                        // case (c)
                        BasicBlock* const succSuccBlock = succBlock->bbJumpDest;

                        if (succSuccBlock->bbFlags & BBF_CLONED_FINALLY_BEGIN)
                        {
                            isJumpToClonedFinally = true;
                        }
                    }
                }
                else if (succBlock->bbJumpKind == BBJ_NONE)
                {
                    if (succBlock->isEmpty())
                    {
                        BasicBlock* const succSuccBlock = succBlock->bbNext;

                        // case (d)
                        if (succSuccBlock->bbFlags & BBF_CLONED_FINALLY_BEGIN)
                        {
                            isJumpToClonedFinally = true;
                        }
                    }
                }

                bool isReturnFromFinally = false;

                // Case (e). Ideally we'd have something stronger to
                // check here -- eg that we are returning from a call
                // to the right finally -- but there are odd cases
                // like orphaned second halves of callfinally pairs
                // that we need to tolerate.
                if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
                {
                    isReturnFromFinally = true;
                }

                // Case (f)
                if (block->bbFlags & BBF_CLONED_FINALLY_END)
                {
                    isReturnFromFinally = true;
                }

                const bool thisExitValid = isCallToFinally || isJumpToClonedFinally || isReturnFromFinally;

                if (!thisExitValid)
                {
                    JITDUMP("fgCheckTryFinallyExitS: EH#%u exit via " FMT_BB " -> " FMT_BB " is invalid\n", XTnum,
                            block->bbNum, succBlock->bbNum);
                }

                allTryExitsValid = allTryExitsValid & thisExitValid;
            }
        }
    }

    if (!allTryExitsValid)
    {
        JITDUMP("fgCheckTryFinallyExits: method contains invalid try exit paths\n");
        assert(allTryExitsValid);
    }
}

#endif // DEBUG

//------------------------------------------------------------------------
// fgCleanupContinuation: cleanup a finally continuation after a
// finally is removed or converted to normal control flow.
//
// Notes:
//    The continuation is the block targeted by the second half of
//    a callfinally/always pair.
//
//    Used by finally cloning, empty try removal, and empty
//    finally removal.
//
//    BBF_FINALLY_TARGET bbFlag is left unchanged by this method
//    since it cannot be incrementally updated. Proper updates happen
//    when fgUpdateFinallyTargetFlags runs after all finally optimizations.

void Compiler::fgCleanupContinuation(BasicBlock* continuation)
{
    // The continuation may be a finalStep block.
    // It is now a normal block, so clear the special keep
    // always flag.
    continuation->bbFlags &= ~BBF_KEEP_BBJ_ALWAYS;

#if !defined(FEATURE_EH_FUNCLETS)
    // Remove the GT_END_LFIN from the continuation,
    // Note we only expect to see one such statement.
    bool foundEndLFin = false;
    for (Statement* const stmt : continuation->Statements())
    {
        GenTree* expr = stmt->GetRootNode();
        if (expr->gtOper == GT_END_LFIN)
        {
            assert(!foundEndLFin);
            fgRemoveStmt(continuation, stmt);
            foundEndLFin = true;
        }
    }
    assert(foundEndLFin);
#endif // !FEATURE_EH_FUNCLETS
}

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

//------------------------------------------------------------------------
// fgUpdateFinallyTargetFlags: recompute BBF_FINALLY_TARGET bits
//    after EH optimizations
//
// Returns:
//   phase status indicating if anything was modified
//
PhaseStatus Compiler::fgUpdateFinallyTargetFlags()
{
    // Any finally targetflag fixup required?
    if (fgOptimizedFinally)
    {
        JITDUMP("updating finally target flag bits\n");
        fgClearAllFinallyTargetBits();
        fgAddFinallyTargetFlags();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }
    else
    {
        JITDUMP("no finally opts, no fixup required\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
}

//------------------------------------------------------------------------
// fgClearAllFinallyTargetBits: Clear all BBF_FINALLY_TARGET bits; these will need to be
// recomputed later.
//
void Compiler::fgClearAllFinallyTargetBits()
{
    JITDUMP("*************** In fgClearAllFinallyTargetBits()\n");

    // Note that we clear the flags even if there are no EH clauses (compHndBBtabCount == 0)
    // in case bits are left over from EH clauses being deleted.

    // Walk all blocks, and reset the target bits.
    for (BasicBlock* const block : Blocks())
    {
        block->bbFlags &= ~BBF_FINALLY_TARGET;
    }
}

//------------------------------------------------------------------------
// fgAddFinallyTargetFlags: Add BBF_FINALLY_TARGET bits to all finally targets.
//
void Compiler::fgAddFinallyTargetFlags()
{
    JITDUMP("*************** In fgAddFinallyTargetFlags()\n");

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, no flags to set.\n");
        return;
    }

    for (BasicBlock* const block : Blocks())
    {
        if (block->isBBCallAlwaysPair())
        {
            BasicBlock* const leave        = block->bbNext;
            BasicBlock* const continuation = leave->bbJumpDest;

            if ((continuation->bbFlags & BBF_FINALLY_TARGET) == 0)
            {
                JITDUMP("Found callfinally " FMT_BB "; setting finally target bit on " FMT_BB "\n", block->bbNum,
                        continuation->bbNum);

                continuation->bbFlags |= BBF_FINALLY_TARGET;
            }
        }
    }
}

#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

//------------------------------------------------------------------------
// fgMergeFinallyChains: tail merge finally invocations
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//
//    Looks for common suffixes in chains of finally invocations
//    (callfinallys) and merges them. These typically arise from
//    try-finallys where there are multiple exit points in the try
//    that have the same target.

PhaseStatus Compiler::fgMergeFinallyChains()
{
#if defined(FEATURE_EH_FUNCLETS)
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to merge.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with minOpts, no merging.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.compDbgCode)
    {
        JITDUMP("Method compiled with debug codegen, no merging.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool enableMergeFinallyChains = true;

#if !defined(FEATURE_EH_FUNCLETS)
    // For non-funclet models (x86) the callfinallys may contain
    // statements and the continuations contain GT_END_LFINs.  So no
    // merging is possible until the GT_END_LFIN blocks can be merged
    // and merging is not safe unless the callfinally blocks are split.
    JITDUMP("EH using non-funclet model; merging not yet implemented.\n");
    enableMergeFinallyChains = false;
#endif // !FEATURE_EH_FUNCLETS

#if !FEATURE_EH_CALLFINALLY_THUNKS
    // For non-thunk EH models (arm32) the callfinallys may contain
    // statements, and merging is not safe unless the callfinally
    // blocks are split.
    JITDUMP("EH using non-callfinally thunk model; merging not yet implemented.\n");
    enableMergeFinallyChains = false;
#endif

    if (!enableMergeFinallyChains)
    {
        JITDUMP("fgMergeFinallyChains disabled\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif // DEBUG

    // Look for finallys.
    bool hasFinally = false;
    for (EHblkDsc* const HBtab : EHClauses(this))
    {
        // Check if this is a try/finally.
        if (HBtab->HasFinallyHandler())
        {
            hasFinally = true;
            break;
        }
    }

    if (!hasFinally)
    {
        JITDUMP("Method does not have any try-finallys; no merging.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Process finallys from outside in, merging as we go. This gives
    // us the desired bottom-up tail merge order for callfinally
    // chains: outer merges may enable inner merges.
    bool            canMerge = false;
    bool            didMerge = false;
    BlockToBlockMap continuationMap(getAllocator());

    // Note XTnum is signed here so we can count down.
    for (int XTnum = compHndBBtabCount - 1; XTnum >= 0; XTnum--)
    {
        EHblkDsc* const HBtab = &compHndBBtab[XTnum];

        // Screen out non-finallys
        if (!HBtab->HasFinallyHandler())
        {
            continue;
        }

        JITDUMP("Examining callfinallys for EH#%d.\n", XTnum);

        // Find all the callfinallys that invoke this finally.
        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* endCallFinallyRangeBlock   = nullptr;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &endCallFinallyRangeBlock);

        // Clear out any stale entries in the continuation map
        continuationMap.RemoveAll();

        // Build a map from each continuation to the "canonical"
        // callfinally for that continuation.
        unsigned          callFinallyCount  = 0;
        BasicBlock* const beginHandlerBlock = HBtab->ebdHndBeg;

        for (BasicBlock* currentBlock = firstCallFinallyRangeBlock; currentBlock != endCallFinallyRangeBlock;
             currentBlock             = currentBlock->bbNext)
        {
            // Ignore "retless" callfinallys (where the finally doesn't return).
            if (currentBlock->isBBCallAlwaysPair() && (currentBlock->bbJumpDest == beginHandlerBlock))
            {
                // The callfinally must be empty, so that we can
                // safely retarget anything that branches here to
                // another callfinally with the same contiuation.
                assert(currentBlock->isEmpty());

                // This callfinally invokes the finally for this try.
                callFinallyCount++;

                // Locate the continuation
                BasicBlock* const leaveBlock        = currentBlock->bbNext;
                BasicBlock* const continuationBlock = leaveBlock->bbJumpDest;

                // If this is the first time we've seen this
                // continuation, register this callfinally as the
                // canonical one.
                if (!continuationMap.Lookup(continuationBlock))
                {
                    continuationMap.Set(continuationBlock, currentBlock);
                }
            }
        }

        // Now we've seen all the callfinallys and their continuations.
        JITDUMP("EH#%i has %u callfinallys, %u continuations\n", XTnum, callFinallyCount, continuationMap.GetCount());

        // If there are more callfinallys than continuations, some of the
        // callfinallys must share a continuation, and we can merge them.
        const bool tryMerge = callFinallyCount > continuationMap.GetCount();

        if (!tryMerge)
        {
            JITDUMP("EH#%i does not have any mergeable callfinallys\n", XTnum);
            continue;
        }

        canMerge = true;

        // Walk the callfinally region, looking for blocks that jump
        // to a callfinally that invokes this try's finally, and make
        // sure they all jump to the appropriate canonical
        // callfinally.
        for (BasicBlock* currentBlock = firstCallFinallyRangeBlock; currentBlock != endCallFinallyRangeBlock;
             currentBlock             = currentBlock->bbNext)
        {
            bool merged = fgRetargetBranchesToCanonicalCallFinally(currentBlock, beginHandlerBlock, continuationMap);
            didMerge    = didMerge || merged;
        }
    }

    if (!canMerge)
    {
        JITDUMP("Method had try-finallys, but did not have any mergeable finally chains.\n");
    }
    else
    {
        if (didMerge)
        {
            JITDUMP("Method had mergeable try-finallys and some callfinally merges were performed.\n");

#ifdef DEBUG
            if (verbose)
            {
                printf("\n*************** After fgMergeFinallyChains()\n");
                fgDispBasicBlocks();
                fgDispHandlerTab();
                printf("\n");
            }

#endif // DEBUG
        }
        else
        {
            // We may not end up doing any merges, because we are only
            // merging continuations for callfinallys that can
            // actually be invoked, and the importer may leave
            // unreachable callfinallys around (for instance, if it
            // is forced to re-import a leave).
            JITDUMP("Method had mergeable try-finallys but no callfinally merges were performed,\n"
                    "likely the non-canonical callfinallys were unreachable\n");
        }
    }

    return didMerge ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgRetargetBranchesToCanonicalCallFinally: find non-canonical callfinally
// invocations and make them canonical.
//
// Arguments:
//     block -- block to examine for call finally invocation
//     handler -- start of the finally region for the try
//     continuationMap -- map giving the canonical callfinally for
//        each continuation
//
// Returns:
//     true iff the block's branch was retargeted.

bool Compiler::fgRetargetBranchesToCanonicalCallFinally(BasicBlock*      block,
                                                        BasicBlock*      handler,
                                                        BlockToBlockMap& continuationMap)
{
    // We expect callfinallys to be invoked by a BBJ_ALWAYS at this
    // stage in compilation.
    if (block->bbJumpKind != BBJ_ALWAYS)
    {
        // Possible paranoia assert here -- no flow successor of
        // this block should be a callfinally for this try.
        return false;
    }

    // Screen out cases that are not callfinallys to the right
    // handler.
    BasicBlock* const callFinally = block->bbJumpDest;

    if (!callFinally->isBBCallAlwaysPair())
    {
        return false;
    }

    if (callFinally->bbJumpDest != handler)
    {
        return false;
    }

    // Ok, this is a callfinally that invokes the right handler.
    // Get its continuation.
    BasicBlock* const leaveBlock        = callFinally->bbNext;
    BasicBlock* const continuationBlock = leaveBlock->bbJumpDest;

    // Find the canonical callfinally for that continuation.
    BasicBlock* const canonicalCallFinally = continuationMap[continuationBlock];
    assert(canonicalCallFinally != nullptr);

    // If the block already jumps to the canoncial call finally, no work needed.
    if (block->bbJumpDest == canonicalCallFinally)
    {
        JITDUMP(FMT_BB " already canonical\n", block->bbNum);
        return false;
    }

    // Else, retarget it so that it does...
    JITDUMP("Redirecting branch in " FMT_BB " from " FMT_BB " to " FMT_BB ".\n", block->bbNum, callFinally->bbNum,
            canonicalCallFinally->bbNum);

    block->bbJumpDest = canonicalCallFinally;
    fgAddRefPred(canonicalCallFinally, block);
    assert(callFinally->bbRefs > 0);
    fgRemoveRefPred(callFinally, block);

    // Update profile counts
    //
    if (block->hasProfileWeight())
    {
        // Add weight to the canonical call finally pair.
        //
        weight_t const canonicalWeight =
            canonicalCallFinally->hasProfileWeight() ? canonicalCallFinally->bbWeight : BB_ZERO_WEIGHT;
        weight_t const newCanonicalWeight = block->bbWeight + canonicalWeight;

        canonicalCallFinally->setBBProfileWeight(newCanonicalWeight);

        BasicBlock* const canonicalLeaveBlock = canonicalCallFinally->bbNext;

        weight_t const canonicalLeaveWeight =
            canonicalLeaveBlock->hasProfileWeight() ? canonicalLeaveBlock->bbWeight : BB_ZERO_WEIGHT;
        weight_t const newLeaveWeight = block->bbWeight + canonicalLeaveWeight;

        canonicalLeaveBlock->setBBProfileWeight(newLeaveWeight);

        // Remove weight from the old call finally pair.
        //
        if (callFinally->hasProfileWeight())
        {
            weight_t const newCallFinallyWeight =
                callFinally->bbWeight > block->bbWeight ? callFinally->bbWeight - block->bbWeight : BB_ZERO_WEIGHT;
            callFinally->setBBProfileWeight(newCallFinallyWeight);
        }

        if (leaveBlock->hasProfileWeight())
        {
            weight_t const newLeaveWeight =
                leaveBlock->bbWeight > block->bbWeight ? leaveBlock->bbWeight - block->bbWeight : BB_ZERO_WEIGHT;
            leaveBlock->setBBProfileWeight(newLeaveWeight);
        }
    }

    return true;
}

//------------------------------------------------------------------------
// fgTailMergeThrows: Tail merge throw blocks and blocks with no return calls.
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//    Scans the flow graph for throw blocks and blocks with no return calls
//    that can be merged, and opportunistically merges them.
//
//    Does not handle throws yet as the analysis is more costly and less
//    likely to pay off. So analysis is restricted to blocks with just one
//    statement.
//
//    For throw helper call merging, we are looking for examples like
//    the below. Here BB17 and BB21 have identical trees that call noreturn
//    methods, so we can modify BB16 to branch to BB21 and delete BB17.
//
//    Also note as a quirk of how we model flow that both BB17 and BB21
//    have successor blocks. We don't turn these into true throw blocks
//    until morph.
//
//    BB16 [005..006) -> BB18 (cond), preds={} succs={BB17,BB18}
//
//    *  JTRUE     void
//    \--*  NE        int
//       ...
//
//    BB17 [005..006), preds={} succs={BB19}
//
//    *  CALL      void   ThrowHelper.ThrowArgumentOutOfRangeException
//    \--*  CNS_INT   int    33
//
//    BB20 [005..006) -> BB22 (cond), preds={} succs={BB21,BB22}
//
//    *  JTRUE     void
//    \--*  LE        int
//       ...
//
//    BB21 [005..006), preds={} succs={BB22}
//
//    *  CALL      void   ThrowHelper.ThrowArgumentOutOfRangeException
//    \--*  CNS_INT   int    33
//
PhaseStatus Compiler::fgTailMergeThrows()
{
    noway_assert(opts.OptimizationEnabled());

    JITDUMP("\n*************** In fgTailMergeThrows\n");

    // Early out case for most methods. Throw helpers are rare.
    if (optNoReturnCallCount < 2)
    {
        JITDUMP("Method does not have multiple noreturn calls.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
    else
    {
        JITDUMP("Scanning the %u candidates\n", optNoReturnCallCount);
    }

    // This transformation requires block pred lists to be built
    // so that flow can be safely updated.
    assert(fgComputePredsDone);

    struct ThrowHelper
    {
        BasicBlock*  m_block;
        GenTreeCall* m_call;

        ThrowHelper() : m_block(nullptr), m_call(nullptr)
        {
        }

        ThrowHelper(BasicBlock* block, GenTreeCall* call) : m_block(block), m_call(call)
        {
        }

        static bool Equals(const ThrowHelper x, const ThrowHelper& y)
        {
            return BasicBlock::sameEHRegion(x.m_block, y.m_block) && GenTreeCall::Equals(x.m_call, y.m_call);
        }

        static unsigned GetHashCode(const ThrowHelper& x)
        {
            return static_cast<unsigned>(reinterpret_cast<uintptr_t>(x.m_call->gtCallMethHnd));
        }
    };

    typedef JitHashTable<ThrowHelper, ThrowHelper, BasicBlock*> CallToBlockMap;

    CompAllocator   allocator(getAllocator(CMK_TailMergeThrows));
    CallToBlockMap  callMap(allocator);
    BlockToBlockMap blockMap(allocator);

    // We run two passes here.
    //
    // The first pass finds candidate blocks. The first candidate for
    // each unique kind of throw is chosen as the canonical example of
    // that kind of throw.  Subsequent matching candidates are mapped
    // to that throw.
    //
    // The second pass modifies flow so that predecessors of
    // non-canonical throw blocks now transfer control to the
    // appropriate canonical block.
    int numCandidates = 0;

    // First pass
    //
    // Scan for THROW blocks. Note early on in compilation (before morph)
    // noreturn blocks are not marked as BBJ_THROW.
    //
    // Walk blocks from last to first so that any branches we
    // introduce to the canonical blocks end up lexically forward
    // and there is less jumbled flow to sort out later.
    for (BasicBlock* block = fgLastBB; block != nullptr; block = block->bbPrev)
    {
        // Workaround: don't consider try entry blocks as candidates
        // for merging; if the canonical throw is later in the same try,
        // we'll create invalid flow.
        if ((block->bbFlags & BBF_TRY_BEG) != 0)
        {
            continue;
        }

        // We only look at the first statement for throw helper calls.
        // Remainder of the block will be dead code.
        //
        // Throw helper calls could show up later in the block; we
        // won't try merging those as we'd need to match up all the
        // prior statements or split the block at this point, etc.
        //
        Statement* const stmt = block->firstStmt();

        if (stmt == nullptr)
        {
            continue;
        }

        // ...that is a call
        GenTree* const tree = stmt->GetRootNode();

        if (!tree->IsCall())
        {
            continue;
        }

        // ...that does not return
        GenTreeCall* const call = tree->AsCall();

        if (!call->IsNoReturn())
        {
            continue;
        }

        // Sanity check -- only user funcs should be marked do not return
        assert(call->gtCallType == CT_USER_FUNC);

        // Ok, we've found a suitable call. See if this is one we know
        // about already, or something new.
        BasicBlock* canonicalBlock = nullptr;

        JITDUMP("\n*** Does not return call\n");
        DISPTREE(call);

        // Have we found an equivalent call already?
        ThrowHelper key(block, call);
        if (callMap.Lookup(key, &canonicalBlock))
        {
            // Yes, this one can be optimized away...
            JITDUMP("    in " FMT_BB " can be dup'd to canonical " FMT_BB "\n", block->bbNum, canonicalBlock->bbNum);
            blockMap.Set(block, canonicalBlock);
            numCandidates++;
        }
        else
        {
            // No, add this as the canonical example
            JITDUMP("    in " FMT_BB " is unique, marking it as canonical\n", block->bbNum);
            callMap.Set(key, block);
        }
    }

    // Bail if no candidates were found
    if (numCandidates == 0)
    {
        JITDUMP("\n*************** no throws can be tail merged, sorry\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    JITDUMP("\n*** found %d merge candidates, rewriting flow\n\n", numCandidates);

    // Second pass.
    //
    // We walk the map rather than the block list, to save a bit of time.
    BlockToBlockMap::KeyIterator iter(blockMap.Begin());
    BlockToBlockMap::KeyIterator end(blockMap.End());
    unsigned                     updateCount = 0;

    for (; !iter.Equal(end); iter++)
    {
        BasicBlock* const nonCanonicalBlock = iter.Get();
        BasicBlock* const canonicalBlock    = iter.GetValue();
        flowList*         nextPredEdge      = nullptr;
        bool              updated           = false;

        // Walk pred list of the non canonical block, updating flow to target
        // the canonical block instead.
        for (flowList* predEdge = nonCanonicalBlock->bbPreds; predEdge != nullptr; predEdge = nextPredEdge)
        {
            BasicBlock* const predBlock = predEdge->getBlock();
            nextPredEdge                = predEdge->flNext;

            switch (predBlock->bbJumpKind)
            {
                case BBJ_NONE:
                {
                    fgTailMergeThrowsFallThroughHelper(predBlock, nonCanonicalBlock, canonicalBlock, predEdge);
                    updated = true;
                }
                break;

                case BBJ_ALWAYS:
                {
                    fgTailMergeThrowsJumpToHelper(predBlock, nonCanonicalBlock, canonicalBlock, predEdge);
                    updated = true;
                }
                break;

                case BBJ_COND:
                {
                    // Flow to non canonical block could be via fall through or jump or both.
                    if (predBlock->bbNext == nonCanonicalBlock)
                    {
                        fgTailMergeThrowsFallThroughHelper(predBlock, nonCanonicalBlock, canonicalBlock, predEdge);
                    }

                    if (predBlock->bbJumpDest == nonCanonicalBlock)
                    {
                        fgTailMergeThrowsJumpToHelper(predBlock, nonCanonicalBlock, canonicalBlock, predEdge);
                    }
                    updated = true;
                }
                break;

                case BBJ_SWITCH:
                {
                    JITDUMP("*** " FMT_BB " now branching to " FMT_BB "\n", predBlock->bbNum, canonicalBlock->bbNum);
                    fgReplaceSwitchJumpTarget(predBlock, canonicalBlock, nonCanonicalBlock);
                    updated = true;
                }
                break;

                default:
                    // We don't expect other kinds of preds, and it is safe to ignore them
                    // as flow is still correct, just not as optimized as it could be.
                    break;
            }
        }

        if (updated)
        {
            updateCount++;
        }
    }

    if (updateCount == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // TODO: Update the count of noreturn call sites -- this feeds a heuristic in morph
    // to determine if these noreturn calls should be tail called.
    //
    // Updating the count does not lead to better results, so deferring for now.
    //
    JITDUMP("Made %u updates\n", updateCount);
    assert(updateCount < optNoReturnCallCount);

    // If we altered flow, reset fgModified. Given where we sit in the
    // phase list, flow-dependent side data hasn't been built yet, so
    // nothing needs invalidation.
    //
    assert(fgModified);
    fgModified = false;
    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// fgTailMergeThrowsFallThroughHelper: fixup flow for fall throughs to mergable throws
//
// Arguments:
//    predBlock - block falling through to the throw helper
//    nonCanonicalBlock - original fall through target
//    canonicalBlock - new (jump) target
//    predEdge - original flow edge
//
// Notes:
//    Alters fall through flow of predBlock so it jumps to the
//    canonicalBlock via a new basic block.  Does not try and fix
//    jump-around flow; we leave that to optOptimizeFlow which runs
//    just afterwards.
//
void Compiler::fgTailMergeThrowsFallThroughHelper(BasicBlock* predBlock,
                                                  BasicBlock* nonCanonicalBlock,
                                                  BasicBlock* canonicalBlock,
                                                  flowList*   predEdge)
{
    assert(predBlock->bbNext == nonCanonicalBlock);

    BasicBlock* const newBlock = fgNewBBafter(BBJ_ALWAYS, predBlock, true);

    JITDUMP("*** " FMT_BB " now falling through to empty " FMT_BB " and then to " FMT_BB "\n", predBlock->bbNum,
            newBlock->bbNum, canonicalBlock->bbNum);

    // Remove the old flow
    fgRemoveRefPred(nonCanonicalBlock, predBlock);

    // Wire up the new flow
    predBlock->bbNext = newBlock;
    fgAddRefPred(newBlock, predBlock, predEdge);

    newBlock->bbJumpDest = canonicalBlock;
    fgAddRefPred(canonicalBlock, newBlock, predEdge);

    // If nonCanonicalBlock has only one pred, all its flow transfers.
    // If it has multiple preds, then we need edge counts or likelihoods
    // to figure things out.
    //
    // For now just do a minimal update.
    //
    newBlock->inheritWeight(nonCanonicalBlock);
}

//------------------------------------------------------------------------
// fgTailMergeThrowsJumpToHelper: fixup flow for jumps to mergable throws
//
// Arguments:
//    predBlock - block jumping to the throw helper
//    nonCanonicalBlock - original jump target
//    canonicalBlock - new jump target
//    predEdge - original flow edge
//
// Notes:
//    Alters jumpDest of predBlock so it jumps to the canonicalBlock.
//
void Compiler::fgTailMergeThrowsJumpToHelper(BasicBlock* predBlock,
                                             BasicBlock* nonCanonicalBlock,
                                             BasicBlock* canonicalBlock,
                                             flowList*   predEdge)
{
    assert(predBlock->bbJumpDest == nonCanonicalBlock);

    JITDUMP("*** " FMT_BB " now branching to " FMT_BB "\n", predBlock->bbNum, canonicalBlock->bbNum);

    // Remove the old flow
    fgRemoveRefPred(nonCanonicalBlock, predBlock);

    // Wire up the new flow
    predBlock->bbJumpDest = canonicalBlock;
    fgAddRefPred(canonicalBlock, predBlock, predEdge);
}
