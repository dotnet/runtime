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
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // We need to update the bbPreds lists.
    assert(fgPredsComputed);

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to remove.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with MinOpts, no removal.\n");
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

        // Check if this is a try/finally or try/fault.
        //
        if (!HBtab->HasFinallyOrFaultHandler())
        {
            JITDUMP("EH#%u is not a try-finally or try-fault; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        finallyCount++;

        // Look at blocks involved.
        BasicBlock* const firstBlock = HBtab->ebdHndBeg;
        BasicBlock* const lastBlock  = HBtab->ebdHndLast;

        // Limit for now to handlers that are single blocks.
        if (firstBlock != lastBlock)
        {
            JITDUMP("EH#%u handler has multiple basic blocks; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // If the handler's block jumps back to itself, then it is not empty.
        if (firstBlock->KindIs(BBJ_ALWAYS) && firstBlock->TargetIs(firstBlock))
        {
            JITDUMP("EH#%u handler has basic block that jumps to itself; skipping.\n", XTnum);
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
            JITDUMP("EH#%u handler is not empty; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // Note we may see single empty BBJ_THROW handler blocks for EH regions
        // deemed unreachable.
        //
        assert(lastBlock->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_THROW));

        JITDUMP("EH#%u has empty handler, removing the region.\n", XTnum);

        if (HBtab->HasFinallyHandler())
        {
            // Find all the call finallys that invoke this finally,
            // and modify them to jump to the return point.
            BasicBlock* firstCallFinallyRangeBlock = nullptr;
            BasicBlock* lastCallFinallyRangeBlock  = nullptr;
            ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

            BasicBlock*       currentBlock             = firstCallFinallyRangeBlock;
            BasicBlock* const endCallFinallyRangeBlock = lastCallFinallyRangeBlock->Next();

            while (currentBlock != endCallFinallyRangeBlock)
            {
                BasicBlock* nextBlock = currentBlock->Next();

                if (currentBlock->KindIs(BBJ_CALLFINALLY) && currentBlock->TargetIs(firstBlock))
                {
                    // Retarget the call finally to jump to the return point.
                    //
                    // We don't expect to see retless finallys here, since
                    // the finally is empty.
                    noway_assert(currentBlock->isBBCallFinallyPair());

                    BasicBlock* const leaveBlock          = currentBlock->Next();
                    BasicBlock* const postTryFinallyBlock = leaveBlock->GetFinallyContinuation();

                    JITDUMP("Modifying callfinally " FMT_BB " leave " FMT_BB " finally " FMT_BB " continuation " FMT_BB
                            "\n",
                            currentBlock->bbNum, leaveBlock->bbNum, firstBlock->bbNum, postTryFinallyBlock->bbNum);
                    JITDUMP("so that " FMT_BB " jumps to " FMT_BB "; then remove " FMT_BB "\n", currentBlock->bbNum,
                            postTryFinallyBlock->bbNum, leaveBlock->bbNum);

                    // Remove the `leaveBlock` first.
                    nextBlock = leaveBlock->Next();
                    fgPrepareCallFinallyRetForRemoval(leaveBlock);
                    fgRemoveBlock(leaveBlock, /* unreachable */ true);

                    // Ref count updates.
                    fgRedirectTargetEdge(currentBlock, postTryFinallyBlock);
                    currentBlock->SetKind(BBJ_ALWAYS);
                    currentBlock->RemoveFlags(BBF_RETLESS_CALL); // no longer a BBJ_CALLFINALLY

                    // Update profile data into postTryFinallyBlock
                    if (currentBlock->hasProfileWeight())
                    {
                        postTryFinallyBlock->increaseBBProfileWeight(currentBlock->bbWeight);
                    }

                    // Cleanup the postTryFinallyBlock
                    fgCleanupContinuation(postTryFinallyBlock);

                    // Make sure iteration isn't going off the deep end.
                    assert(leaveBlock != endCallFinallyRangeBlock);
                }

                currentBlock = nextBlock;
            }
        }

        JITDUMP("Remove now-unreachable handler " FMT_BB "\n", firstBlock->bbNum);

        // Handler block should now be unreferenced, since the only
        // explicit references to it were in call finallys.
        firstBlock->bbRefs = 0;

        // Remove the handler block.
        firstBlock->RemoveFlags(BBF_DONT_REMOVE);
        constexpr bool unreachable = true;
        fgRemoveBlock(firstBlock, unreachable);

        // Find enclosing try region for the try, if any, and update
        // the try region. Note the handler region (if any) won't
        // change.
        BasicBlock* const firstTryBlock = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock  = HBtab->ebdTryLast;
        assert(firstTryBlock->getTryIndex() == XTnum);

        for (BasicBlock* const block : Blocks(firstTryBlock, lastTryBlock))
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
        }

        // Update any impacted ACDs.
        //
        fgUpdateACDsBeforeEHTableEntryRemoval(XTnum);

        // Remove the try-finally EH region. This will compact the EH table
        // so XTnum now points at the next entry.
        fgRemoveEHTableEntry(XTnum);

        // First block of the former try no longer needs special protection.
        firstTryBlock->RemoveFlags(BBF_DONT_REMOVE);

        emptyCount++;
    }

    if (emptyCount > 0)
    {
        JITDUMP("fgRemoveEmptyFinally() removed %u try-finally/fault clauses from %u finally/fault(s)\n", emptyCount,
                finallyCount);
        fgInvalidateDfsTree();

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
// fgUpdateACDsBeforeEHTableEntryRemoval: delete, move, or merge ACDs within
//    an EH region we're about to remove.
//
// Arguments:
//    XTNum -- eh region being removed
//
// Notes:
//    XTnum must be the innermost mutual protect region, for a try-catch.
//
//    We assume that the ACDs in the try/handler regions might still be needed
//    (callers may "promote" these blocks to their enclosing regions). If the
//    caller is actually removing the region code instead of merging it to the
//    enclosing region, it is ok to have extra ACDs around.
//
//    ACDs in filter regions are removed.
//
void Compiler::fgUpdateACDsBeforeEHTableEntryRemoval(unsigned XTnum)
{
    if (!fgHasAddCodeDscMap())
    {
        // No ACDs to worry about at this time
        //
        return;
    }

    EHblkDsc* const      ebd = ehGetDsc(XTnum);
    AddCodeDscMap* const map = fgGetAddCodeDscMap();
    for (AddCodeDsc* const add : AddCodeDscMap::ValueIteration(map))
    {
        JITDUMP("Considering ");
        JITDUMPEXEC(add->Dump());

        // Remember the old lookup key
        //
        AddCodeDscKey oldKey(add);

        const bool inHnd     = add->acdHndIndex > 0;
        const bool inTry     = add->acdTryIndex > 0;
        const bool inThisHnd = inHnd && ((unsigned)(add->acdHndIndex - 1) == XTnum);
        const bool inThisFlt = inHnd && ((unsigned)(add->acdHndIndex - 1) == XTnum);
        const bool inThisTry = inTry && ((unsigned)(add->acdTryIndex - 1) == XTnum);

        // If this ACD is in the filter of this region, it is no longer needed
        //
        if (inThisFlt && (add->acdKeyDsg == AcdKeyDesignator::KD_FLT))
        {
            bool const removed = map->Remove(oldKey);
            assert(removed);
            JITDUMP("ACD%u was in EH#%u filter region: removing\n", add->acdNum, XTnum);
            JITDUMPEXEC(add->Dump());
            continue;
        }

        // Note any ACDs in enclosed regions are updated when the region
        // itself is removed.
        //
        if (!inThisTry && !inThisHnd)
        {
            JITDUMP("ACD%u not affected\n", add->acdNum);
            continue;
        }

        bool rekey = false;

        // If this ACD is in the handler of this region, update the
        // enclosing handler index.
        //
        if (inThisHnd)
        {
            if (ebd->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                add->acdHndIndex = 0;
            }
            else
            {
                add->acdHndIndex = ebd->ebdEnclosingHndIndex + 1;
            }

            rekey = (add->acdKeyDsg == AcdKeyDesignator::KD_HND);
        }

        // If this ACD is in the try of this region, update the
        // enclosing try index.
        //
        if (inThisTry)
        {
            if (ebd->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                add->acdTryIndex = 0;
            }
            else
            {
                add->acdTryIndex = ebd->ebdEnclosingTryIndex + 1;
            }
            rekey = (add->acdKeyDsg == AcdKeyDesignator::KD_TRY);
        }

        if (!rekey)
        {
            // If we didn't change the enclosing region for the ACD,
            // the modifications above didn't change the key.
            //
            JITDUMP("ACD%u non-enclosing region updated; key remains the same\n", add->acdNum);
            JITDUMPEXEC(add->Dump());
            continue;
        }

        // Update the ACD key designator (note it may change).
        //
        // Then see if there is already an equivalent ACD in
        // the new enclosing region, and if so, "merge" this ACD into
        // that one (by removing this ACD from the map).
        //
        // If there is no equivalent ACD, re-add this current ACD
        // with an updated key.
        //
        add->UpdateKeyDesignator(this);

        // Remove the ACD from the map via its old key
        //
        bool const removed = map->Remove(oldKey);
        assert(removed);

        // Compute the new key an see if there's an existing
        // ACD with that key.
        //
        AddCodeDscKey newKey(add);
        AddCodeDsc*   existing = nullptr;
        if (map->Lookup(newKey, &existing))
        {
            // If so, this ACD is now redundant
            //
            JITDUMP("ACD%u merged into ACD%u\n", add->acdNum, existing->acdNum);
            JITDUMPEXEC(existing->Dump());
        }
        else
        {
            // If not, re-enter this ACD in the map with the updated key
            //
            JITDUMP("ACD%u updated with new key\n", add->acdNum);
            map->Set(newKey, add);
            JITDUMPEXEC(add->Dump());
        }
    }
}

//------------------------------------------------------------------------
// fgRemoveEmptyTry: Optimize try/finallys where the try is empty,
//    or cannot throw any exceptions
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

    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // We need to update the bbPreds lists.
    assert(fgPredsComputed);

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
        JITDUMP("Method compiled with MinOpts, no removal.\n");
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
        BasicBlock*       callFinally;

        assert(firstTryBlock->getTryIndex() == XTnum);

        // Assume the try is empty
        //
        bool canThrow = false;

        // Limit for now to trys that contain only a callfinally pair
        // or branch to same (we check this later). So we only need
        // check the first block.
        //
        if (!firstTryBlock->isEmpty())
        {
            // Walk statements to see if any can throw an exception.
            //
            for (Statement* const stmt : firstTryBlock->Statements())
            {
                // Not clear when we can trust GTF_EXCEPT alone.
                // GTF_CALL is too broad, but safe.
                //
                if ((stmt->GetRootNode()->gtFlags & (GTF_EXCEPT | GTF_CALL)) != 0)
                {
                    canThrow = true;
                    break;
                }
            }
        }

        if (canThrow)
        {
            JITDUMP("EH#%u first try block " FMT_BB " can throw exception; skipping.\n", XTnum, firstTryBlock->bbNum);
            XTnum++;
            continue;
        }

        if (UsesCallFinallyThunks())
        {
            // Look for blocks that are always jumps to a call finally
            // pair that targets the finally
            if (!firstTryBlock->KindIs(BBJ_ALWAYS))
            {
                JITDUMP("EH#%u first try block " FMT_BB " not jump to a callfinally; skipping.\n", XTnum,
                        firstTryBlock->bbNum);
                XTnum++;
                continue;
            }

            callFinally = firstTryBlock->GetTarget();

            // Look for call finally pair. Note this will also disqualify
            // empty try removal in cases where the finally doesn't
            // return.
            if (!callFinally->isBBCallFinallyPair() || !callFinally->TargetIs(firstHandlerBlock))
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
                        firstTryBlock->Next()->bbNum);
                XTnum++;
                continue;
            }
        }
        else
        {
            // Look for call finally pair within the try itself. Note this
            // will also disqualify empty try removal in cases where the
            // finally doesn't return.
            if (!firstTryBlock->isBBCallFinallyPair() || !firstTryBlock->TargetIs(firstHandlerBlock))
            {
                JITDUMP("EH#%u first try block " FMT_BB " not a callfinally; skipping.\n", XTnum, firstTryBlock->bbNum);
                XTnum++;
                continue;
            }

            callFinally = firstTryBlock;

            // Try must be a callalways pair of blocks.
            if (!firstTryBlock->NextIs(lastTryBlock))
            {
                JITDUMP("EH#%u block " FMT_BB " not last block in try; skipping.\n", XTnum,
                        firstTryBlock->Next()->bbNum);
                XTnum++;
                continue;
            }
        }

        JITDUMP("EH#%u has empty try, removing the try region and promoting the finally.\n", XTnum);

        // There should be just one callfinally that invokes this
        // finally, the one we found above. Verify this.
        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* lastCallFinallyRangeBlock  = nullptr;
        bool        verifiedSingleCallfinally  = true;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

        for (BasicBlock* const block : Blocks(firstCallFinallyRangeBlock, lastCallFinallyRangeBlock))
        {
            if (block->KindIs(BBJ_CALLFINALLY) && block->TargetIs(firstHandlerBlock))
            {
                assert(block->isBBCallFinallyPair());

                // In some cases we may have unreachable callfinallys.
                // If so, skip the optimization; a later pass can catch this
                // once unreachable blocks have been pruned.
                //
                if (block != callFinally)
                {
                    JITDUMP("EH#%u found unexpected (likely unreachable) callfinally " FMT_BB "; skipping.\n", XTnum,
                            block->bbNum);
                    verifiedSingleCallfinally = false;
                    break;
                }
            }
        }

        if (!verifiedSingleCallfinally)
        {
            JITDUMP("EH#%u -- unexpectedly -- has multiple callfinallys; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // Time to optimize.
        //

        // Identify the leave block and the continuation
        BasicBlock* const leave        = callFinally->Next();
        BasicBlock* const continuation = leave->GetFinallyContinuation();

        // (1) Find enclosing try region for the try, if any, and
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

            if (block == lastTryBlock)
            {
                break;
            }
        }

        // (2) Cleanup the leave. Don't do this earlier, as removing the block might remove the
        // last block of the `try`, and that could affect the block iteration above.
        fgPrepareCallFinallyRetForRemoval(leave);
        fgRemoveBlock(leave, /* unreachable */ true);

        // (3) Convert the callfinally to a normal jump to the handler
        assert(callFinally->HasInitializedTarget());
        callFinally->SetKind(BBJ_ALWAYS);
        callFinally->RemoveFlags(BBF_RETLESS_CALL); // no longer a BBJ_CALLFINALLY

        // (4) Cleanup the continuation
        fgCleanupContinuation(continuation);

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

                if (block->KindIs(BBJ_EHFINALLYRET))
                {
                    Statement* finallyRet     = block->lastStmt();
                    GenTree*   finallyRetExpr = finallyRet->GetRootNode();
                    assert(finallyRetExpr->gtOper == GT_RETFILT);
                    fgRemoveStmt(block, finallyRet);
                    FlowEdge* const newEdge = fgAddRefPred(continuation, block);
                    block->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);

                    // Propagate profile weight into the continuation block
                    if (continuation->hasProfileWeight())
                    {
                        continuation->increaseBBProfileWeight(block->bbWeight);
                    }
                }
            }

#if defined(FEATURE_EH_WINDOWS_X86)
            if (!UsesFunclets())
            {
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
            }
#endif // FEATURE_EH_WINDOWS_X86
        }

        // (6) Update any impacted ACDs.
        //
        fgUpdateACDsBeforeEHTableEntryRemoval(XTnum);

        // (7) Remove the try-finally EH region. This will compact the
        // EH table so XTnum now points at the next entry and will update
        // the EH region indices of any nested EH in the (former) handler.
        //
        fgRemoveEHTableEntry(XTnum);

        // (8) The handler entry has an artificial extra ref count. Remove it.
        // There also should be one normal ref, from the try, and the handler
        // may contain internal branches back to its start. So the ref count
        // should currently be at least 2.
        //
        assert(firstHandlerBlock->bbRefs >= 2);
        firstHandlerBlock->bbRefs -= 1;

        // (8) The old try entry no longer needs special protection.
        firstTryBlock->RemoveFlags(BBF_DONT_REMOVE);

        // Another one bites the dust...
        emptyCount++;
    }

    if (emptyCount > 0)
    {
        JITDUMP("fgRemoveEmptyTry() optimized %u empty-try try-finally clauses\n", emptyCount);
        fgInvalidateDfsTree();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgRemoveEmptyTryCatchOrTryFault: Optimize try/catch or try/fault where
//    the try is empty, or cannot throw any exceptions
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//
PhaseStatus Compiler::fgRemoveEmptyTryCatchOrTryFault()
{
    JITDUMP("\n*************** In fgRemoveEmptyTryCatchOrTryFault()\n");

    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    bool enableRemoveEmptyTryCatchOrTryFault = true;

#ifdef DEBUG
    // Allow override to enable/disable.
    enableRemoveEmptyTryCatchOrTryFault = (JitConfig.JitEnableRemoveEmptyTryCatchOrTryFault() == 1);
#endif // DEBUG

    if (!enableRemoveEmptyTryCatchOrTryFault)
    {
        JITDUMP("Empty try/catch/fault removal disabled.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to remove.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with MinOpts, no removal.\n");
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
        printf("\n*************** Before fgRemoveEmptyTryCatchOrTryFault()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif // DEBUG

    // Look for try-catches where the try is empty.
    unsigned emptyCount = 0;
    unsigned XTnum      = 0;
    while (XTnum < compHndBBtabCount)
    {
        EHblkDsc* const HBtab = &compHndBBtab[XTnum];

        // Check if this is a try/catch.
        if (HBtab->HasFinallyHandler())
        {
            JITDUMP("EH#%u is not a try-catch or try-fault; skipping.\n", XTnum);
            XTnum++;
            continue;
        }

        // Examine the try region
        //
        BasicBlock* const firstTryBlock = HBtab->ebdTryBeg;
        BasicBlock* const lastTryBlock  = HBtab->ebdTryLast;

        // Assume the try is empty
        //
        bool canThrow = false;

        // Walk all blocks in the try. Since we are walking
        // try regions inner/outer, if we find an enclosed
        // try, we assume it must be able to throw.
        //
        for (BasicBlock* const tryBlock : Blocks(firstTryBlock, lastTryBlock))
        {
            if (tryBlock->getTryIndex() != XTnum)
            {
                JITDUMP("EH#%u try block " FMT_BB " is nested try entry; skipping.\n", XTnum, tryBlock->bbNum);
                canThrow = true;
                break;
            }

            // Walk statements to see if any can throw an exception.
            //
            for (Statement* const stmt : tryBlock->Statements())
            {
                // Not clear when we can trust GTF_EXCEPT alone.
                // GTF_CALL is perhaps too broad, but safe.
                //
                if ((stmt->GetRootNode()->gtFlags & (GTF_EXCEPT | GTF_CALL)) != 0)
                {
                    JITDUMP("EH#%u " FMT_STMT " in " FMT_BB " can throw; skipping.\n", XTnum, stmt->GetID(),
                            tryBlock->bbNum);
                    canThrow = true;
                    break;
                }
            }

            if (canThrow)
            {
                break;
            }
        }

        if (canThrow)
        {
            // We could accelerate a bit by skipping to the first non-mutual protect region.
            //
            XTnum++;
            continue;
        }

        JITDUMP("EH#%u try has no statements that can throw\n", XTnum);

        // Since there are no tested trys, XTnum should be the try index of
        // all blocks in the try region.
        //
        assert(firstTryBlock->getTryIndex() == XTnum);
        assert(lastTryBlock->getTryIndex() == XTnum);

        // Examine the handler blocks. If we see an enclosed try, we bail out for now.
        // We could handle this, with a bit more work.
        //
        BasicBlock* const firstHndBlock      = HBtab->ebdHndBeg;
        BasicBlock* const lastHndBlock       = HBtab->ebdHndLast;
        bool              handlerEnclosesTry = false;

        for (BasicBlock* const handlerBlock : Blocks(firstHndBlock, lastHndBlock))
        {
            if (bbIsTryBeg(handlerBlock))
            {
                JITDUMP("EH#%u handler block " FMT_BB " is nested try entry; skipping.\n", XTnum, handlerBlock->bbNum);
                handlerEnclosesTry = true;
                break;
            }
        }

        if (handlerEnclosesTry)
        {
            // We could accelerate a bit by skipping to the first non-mutual protect region.
            //
            XTnum++;
            continue;
        }

        // Time to optimize.
        //
        unsigned const enclosingTryIndex = HBtab->ebdEnclosingTryIndex;

        // (1) Find enclosing try region for the try, if any, and
        // update the try region for the blocks in the try. Note the
        // handler region (if any) won't change.
        //
        for (BasicBlock* const tryBlock : Blocks(firstTryBlock, lastTryBlock))
        {
            // Look for blocks directly contained in this try, and
            // update the try region appropriately.
            //
            // The try region for blocks transitively contained (say in a
            // child try) will get updated by the subsequent call to
            // fgRemoveEHTableEntry.
            //
            if (tryBlock->getTryIndex() == XTnum)
            {
                if (enclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    tryBlock->clearTryIndex();
                }
                else
                {
                    tryBlock->setTryIndex(enclosingTryIndex);
                }
            }
        }

        // (2) Remove any filter blocks
        // The first filter block has an artificial ref count
        //
        if (HBtab->HasFilter())
        {
            BasicBlock* const firstFltBlock = HBtab->ebdFilter;
            assert(firstFltBlock->bbRefs == 1);
            firstFltBlock->bbRefs               = 0;
            BasicBlock* const afterLastFltBlock = HBtab->BBFilterLast()->Next();

            // Must do this in two passes to handle loops or lexically
            // backwards references.
            //
            for (BasicBlock* filterBlock = firstFltBlock; filterBlock != afterLastFltBlock;
                 filterBlock             = filterBlock->Next())
            {
                fgRemoveBlockAsPred(filterBlock);
                filterBlock->SetKind(BBJ_THROW);
            }

            for (BasicBlock* filterBlock = firstFltBlock; filterBlock != afterLastFltBlock;
                 filterBlock             = filterBlock->Next())
            {
                filterBlock->RemoveFlags(BBF_DONT_REMOVE);
                fgRemoveBlock(filterBlock, /* unreachable */ true);
            }
        }

        // (3) Remove any handler blocks.
        // The first handler block has an artificial ref count
        //
        assert(firstHndBlock->bbRefs == 1);
        firstHndBlock->bbRefs               = 0;
        BasicBlock* const afterLastHndBlock = lastHndBlock->Next();

        // Must do this in two passes to handle loops or lexically
        // backwards references.
        //
        for (BasicBlock* handlerBlock = firstHndBlock; handlerBlock != afterLastHndBlock;
             handlerBlock             = handlerBlock->Next())
        {
            assert(!bbIsTryBeg(handlerBlock));

            // It's possible to see a callfinally pair in a catch, and if so
            // there may be a pred edge into the pair tail from outside the catch.
            // Handle this specially.
            //
            if (handlerBlock->isBBCallFinallyPair())
            {
                BasicBlock* const tailBlock = handlerBlock->Next();
                fgPrepareCallFinallyRetForRemoval(tailBlock);
            }

            fgRemoveBlockAsPred(handlerBlock);
            handlerBlock->SetKind(BBJ_THROW);
        }

        for (BasicBlock* handlerBlock = firstHndBlock; handlerBlock != afterLastHndBlock;
             handlerBlock             = handlerBlock->Next())
        {
            assert(!bbIsTryBeg(handlerBlock));
            handlerBlock->RemoveFlags(BBF_DONT_REMOVE);
            fgRemoveBlock(handlerBlock, /* unreachable */ true);
        }

        // (4) Update any impacted ACDs.
        //
        fgUpdateACDsBeforeEHTableEntryRemoval(XTnum);

        // (5) Remove the try-catch EH region. This will compact the
        // EH table so XTnum now points at the next entry and will update
        // the EH region indices of any nested EH blocks.
        //
        fgRemoveEHTableEntry(XTnum);

        // (6) The old try entry may no longer need special protection.
        // (it may still be an entry of an enclosing try)
        //
        if (!bbIsTryBeg(firstTryBlock))
        {
            firstTryBlock->RemoveFlags(BBF_DONT_REMOVE);
        }

        // Another one bites the dust...
        emptyCount++;
    }

    if (emptyCount > 0)
    {
        JITDUMP("fgRemoveEmptyTryCatchOrTryFault() optimized %u empty-try catch/fault clauses\n", emptyCount);
        fgInvalidateDfsTree();
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
//    distinguishable from organic try-faults by handler type
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
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // We need to update the bbPreds lists.
    assert(fgPredsComputed);

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
        JITDUMP("Method compiled with MinOpts, no cloning.\n");
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
        BasicBlock* nextBlock       = lastBlock->Next();
        unsigned    regionBBCount   = 0;
        unsigned    regionStmtCount = 0;
        bool        hasFinallyRet   = false;
        bool        hasSwitch       = false;

        for (BasicBlock* const block : Blocks(firstBlock, lastBlock))
        {
            if (block->KindIs(BBJ_SWITCH))
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

            hasFinallyRet = hasFinallyRet || block->KindIs(BBJ_EHFINALLYRET);
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

        JITDUMP("EH#%u is a candidate for finally cloning: %u blocks, %u statements\n", XTnum, regionBBCount,
                regionStmtCount);

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
        BasicBlock* const beforeTryBlock = firstTryBlock->Prev();

        BasicBlock* normalCallFinallyBlock  = nullptr;
        BasicBlock* normalCallFinallyReturn = nullptr;
        BasicBlock* cloneInsertAfter        = HBtab->ebdTryLast;
        const bool  usingProfileWeights     = fgIsUsingProfileWeights();
        weight_t    currentWeight           = BB_ZERO_WEIGHT;

        for (BasicBlock* block = lastTryBlock; block != beforeTryBlock; block = block->Prev())
        {
            BasicBlock* jumpDest = nullptr;

            if (UsesCallFinallyThunks())
            {
                // Blocks that transfer control to callfinallies are usually
                // BBJ_ALWAYS blocks, but the last block of a try may fall
                // through to a callfinally, or could be the target of a BBJ_CALLFINALLYRET,
                // indicating a chained callfinally.

                if (block->KindIs(BBJ_ALWAYS, BBJ_CALLFINALLYRET))
                {
                    jumpDest = block->GetTarget();
                }

                if (jumpDest == nullptr)
                {
                    continue;
                }
            }
            else
            {
                jumpDest = block;
            }

            // The jumpDest must be a callfinally that in turn invokes the
            // finally of interest.
            if (!jumpDest->isBBCallFinallyPair() || !jumpDest->TargetIs(firstBlock))
            {
                continue;
            }

            // Found a block that invokes the finally.
            //
            BasicBlock* const finallyReturnBlock  = jumpDest->Next();
            BasicBlock* const postTryFinallyBlock = finallyReturnBlock->GetFinallyContinuation();
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

            if (UsesCallFinallyThunks())
            {
                // When there are callfinally thunks, we don't expect to see the
                // callfinally within a handler region either.
                assert(!jumpDest->hasHndIndex());

                // Update the clone insertion point to just after the
                // call always pair.
                cloneInsertAfter = finallyReturnBlock;

                JITDUMP("%s path to clone: try block " FMT_BB " jumps to callfinally at " FMT_BB ";"
                        " the call returns to " FMT_BB " which jumps to " FMT_BB "\n",
                        isUpdate ? "Updating" : "Choosing", block->bbNum, jumpDest->bbNum, finallyReturnBlock->bbNum,
                        postTryFinallyBlock->bbNum);
            }
            else
            {
                JITDUMP("%s path to clone: try block " FMT_BB " is a callfinally;"
                        " the call returns to " FMT_BB " which jumps to " FMT_BB "\n",
                        isUpdate ? "Updating" : "Choosing", block->bbNum, finallyReturnBlock->bbNum,
                        postTryFinallyBlock->bbNum);
            }

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

        BasicBlock* firstCallFinallyRangeBlock = nullptr;
        BasicBlock* lastCallFinallyRangeBlock  = nullptr;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

        // Clone the finally and retarget the normal return path and
        // any other path that happens to share that same return
        // point. For instance a construct like:
        //
        //  try { } catch { } finally { }
        //
        // will have two call finally blocks, one for the normal exit
        // from the try, and the other for the exit from the
        // catch. They'll both pass the same return point which is the
        // statement after the finally, so they can share the clone.
        //
        // Clone the finally body, and splice it into the flow graph
        // within the parent region of the try.
        //
        const unsigned  finallyTryIndex = firstBlock->bbTryIndex;
        BasicBlock*     insertAfter     = nullptr;
        BlockToBlockMap blockMap(getAllocator());
        unsigned        cloneBBCount = 0;
        weight_t        originalWeight;

        // When distributing weight between the original and cloned regions,
        // ensure only weight from region entries is considered.
        // Flow from loop backedges within the region should not influence the weight distribution ratio.
        if (firstBlock->hasProfileWeight())
        {
            originalWeight = firstBlock->bbWeight;
            for (BasicBlock* const predBlock : firstBlock->PredBlocks())
            {
                if (!predBlock->KindIs(BBJ_CALLFINALLY))
                {
                    originalWeight = max(0.0, originalWeight - predBlock->bbWeight);
                }
            }
        }
        else
        {
            originalWeight = BB_ZERO_WEIGHT;
        }

        for (BasicBlock* block = firstBlock; block != nextBlock; block = block->Next())
        {
            BasicBlock* newBlock;

            if (block == firstBlock)
            {
                // Put first cloned finally block into the appropriate
                // region, somewhere within or after the range of
                // callfinallys, depending on the EH implementation.
                const unsigned    hndIndex = 0;
                BasicBlock* const nearBlk  = cloneInsertAfter;
                newBlock                   = fgNewBBinRegion(BBJ_ALWAYS, finallyTryIndex, hndIndex, nearBlk);

                // If the clone ends up just after the finally, adjust
                // the stopping point for finally traversal.
                if (newBlock->NextIs(nextBlock))
                {
                    assert(newBlock->PrevIs(lastBlock));
                    nextBlock = newBlock;
                }
            }
            else
            {
                // Put subsequent blocks in the same region...
                const bool extendRegion = true;
                newBlock                = fgNewBBafter(BBJ_ALWAYS, insertAfter, extendRegion);
            }

            cloneBBCount++;
            assert(cloneBBCount <= regionBBCount);

            insertAfter = newBlock;
            blockMap.Set(block, newBlock);

            BasicBlock::CloneBlockState(this, newBlock, block);

            // Update block flags. Note a block can be both first and last.
            if (block == firstBlock)
            {
                // Mark the block as the start of the cloned finally.
                newBlock->SetFlags(BBF_CLONED_FINALLY_BEGIN);

                // Cloned finally entry block does not need any special protection.
                newBlock->RemoveFlags(BBF_DONT_REMOVE);
            }

            if (block == lastBlock)
            {
                // Mark the block as the end of the cloned finally.
                newBlock->SetFlags(BBF_CLONED_FINALLY_END);
            }

            newBlock->RemoveFlags(BBF_DONT_REMOVE);

            // Make sure clone block state hasn't munged the try region.
            assert(newBlock->bbTryIndex == finallyTryIndex);

            // Cloned handler block is no longer within the handler.
            newBlock->clearHndIndex();

            // Jump dests are set in a post-pass; make sure CloneBlockState hasn't tried to set them.
            assert(newBlock->KindIs(BBJ_ALWAYS));
            assert(!newBlock->HasInitializedTarget());
        }

        // We should have cloned all the finally region blocks.
        assert(cloneBBCount == regionBBCount);

        JITDUMP("Cloned finally blocks are: " FMT_BB " ... " FMT_BB "\n", blockMap[firstBlock]->bbNum,
                blockMap[lastBlock]->bbNum);

        // Redirect any branches within the newly-cloned
        // finally, and any finally returns to jump to the return
        // point.
        for (BasicBlock* block = firstBlock; block != nextBlock; block = block->Next())
        {
            BasicBlock* newBlock = blockMap[block];
            // Jump kind/target should not be set yet
            assert(newBlock->KindIs(BBJ_ALWAYS));
            assert(!newBlock->HasInitializedTarget());

            if (block->KindIs(BBJ_EHFINALLYRET))
            {
                Statement* finallyRet     = newBlock->lastStmt();
                GenTree*   finallyRetExpr = finallyRet->GetRootNode();
                assert(finallyRetExpr->gtOper == GT_RETFILT);
                fgRemoveStmt(newBlock, finallyRet);

                FlowEdge* const newEdge = fgAddRefPred(normalCallFinallyReturn, newBlock);
                newBlock->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);
            }
            else
            {
                optSetMappedBlockTargets(block, newBlock, &blockMap);
            }
        }

        // Modify the targeting call finallys to branch to the cloned
        // finally. Make a note if we see some calls that can't be
        // retargeted (since they want to return to other places).
        BasicBlock* const firstCloneBlock    = blockMap[firstBlock];
        bool              retargetedAllCalls = true;
        weight_t          retargetedWeight   = BB_ZERO_WEIGHT;

        BasicBlock*       currentBlock             = firstCallFinallyRangeBlock;
        BasicBlock* const endCallFinallyRangeBlock = lastCallFinallyRangeBlock->Next();
        while (currentBlock != endCallFinallyRangeBlock)
        {
            BasicBlock* nextBlockToScan = currentBlock->Next();

            if (currentBlock->isBBCallFinallyPair() && currentBlock->TargetIs(firstBlock))
            {
                BasicBlock* const leaveBlock          = currentBlock->Next();
                BasicBlock* const postTryFinallyBlock = leaveBlock->GetFinallyContinuation();

                // Note we must retarget all callfinallies that have this
                // continuation, or we can't clean up the continuation
                // block properly below, since it will be reachable both
                // by the cloned finally and by the called finally.
                if (postTryFinallyBlock == normalCallFinallyReturn)
                {
                    JITDUMP("Retargeting callfinally " FMT_BB " to clone entry " FMT_BB "\n", currentBlock->bbNum,
                            firstCloneBlock->bbNum);

                    // Remove the `leaveBlock` first, to avoid asserts.
                    nextBlockToScan = leaveBlock->Next();
                    fgPrepareCallFinallyRetForRemoval(leaveBlock);
                    fgRemoveBlock(leaveBlock, /* unreachable */ true);

                    // Ref count updates.
                    fgRedirectTargetEdge(currentBlock, firstCloneBlock);

                    // This call returns to the expected spot, so retarget it to branch to the clone.
                    currentBlock->RemoveFlags(BBF_RETLESS_CALL); // no longer a BBJ_CALLFINALLY
                    currentBlock->SetKind(BBJ_ALWAYS);

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

            // Change all BBJ_EHFINALLYRET to BBJ_EHFAULTRET in the now-fault region.
            for (BasicBlock* const block : Blocks(HBtab->ebdHndBeg, HBtab->ebdHndLast))
            {
                if (block->KindIs(BBJ_EHFINALLYRET))
                {
                    assert(block->GetEhfTargets()->bbeCount == 0);
                    block->SetKind(BBJ_EHFAULTRET);
                }
            }
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

                    BasicBlock* const clonedBlock = blockMap[block];
                    clonedBlock->setBBProfileWeight(blockWeight * clonedScale);
                    JITDUMP("Set weight of " FMT_BB " to " FMT_WT "\n", clonedBlock->bbNum, clonedBlock->bbWeight);
                }
            }
        }

        // Update flow into normalCallFinallyReturn
        if (normalCallFinallyReturn->hasProfileWeight())
        {
            normalCallFinallyReturn->bbWeight = BB_ZERO_WEIGHT;
            for (FlowEdge* const predEdge : normalCallFinallyReturn->PredEdges())
            {
                normalCallFinallyReturn->increaseBBProfileWeight(predEdge->getLikelyWeight());
            }
        }

        // Done!
        JITDUMP("\nDone with EH#%u\n\n", XTnum);
        cloneCount++;
    }

    if (cloneCount > 0)
    {
        JITDUMP("fgCloneFinally() cloned %u finally handlers\n", cloneCount);

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
//
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

                // When there are callfinally thunks, callfinallies
                // logically "belong" to a child region and the exit
                // path validity will be checked when looking at the
                // try blocks in that region.
                if (UsesCallFinallyThunks() && block->KindIs(BBJ_CALLFINALLY))
                {
                    continue;
                }

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
                // (d) via the callfinallyret half of a callfinally pair
                // (e) via an always jump clonefinally exit
                bool isCallToFinally = false;

                if (UsesCallFinallyThunks() && succBlock->KindIs(BBJ_CALLFINALLY))
                {
                    // case (a1)
                    isCallToFinally = isFinally && succBlock->TargetIs(finallyBlock);
                }
                else if (!UsesCallFinallyThunks() && block->KindIs(BBJ_CALLFINALLY))
                {
                    // case (a2)
                    isCallToFinally = isFinally && block->TargetIs(finallyBlock);
                }

                bool isJumpToClonedFinally = false;

                if (succBlock->HasFlag(BBF_CLONED_FINALLY_BEGIN))
                {
                    // case (b)
                    isJumpToClonedFinally = true;
                }
                else if (succBlock->KindIs(BBJ_ALWAYS))
                {
                    if (succBlock->isEmpty())
                    {
                        // case (c)
                        BasicBlock* const succSuccBlock = succBlock->GetTarget();

                        if (succSuccBlock->HasFlag(BBF_CLONED_FINALLY_BEGIN))
                        {
                            isJumpToClonedFinally = true;
                        }
                    }
                }

                bool isReturnFromFinally = false;

                // Case (d). Ideally we'd have something stronger to
                // check here -- eg that we are returning from a call
                // to the right finally.
                if (block->KindIs(BBJ_CALLFINALLYRET))
                {
                    isReturnFromFinally = true;
                }
                if (block->HasFlag(BBF_KEEP_BBJ_ALWAYS))
                {
                    isReturnFromFinally = true;
                }

                // Case (e)
                if (block->HasFlag(BBF_CLONED_FINALLY_END))
                {
                    isReturnFromFinally = true;
                }

                const bool thisExitValid = isCallToFinally || isJumpToClonedFinally || isReturnFromFinally;

                if (!thisExitValid)
                {
                    JITDUMP("fgCheckTryFinallyExits: EH#%u exit via " FMT_BB " -> " FMT_BB " is invalid\n", XTnum,
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
//    a callfinally pair.
//
//    Used by finally cloning, empty try removal, and empty
//    finally removal.
//
void Compiler::fgCleanupContinuation(BasicBlock* continuation)
{
#if defined(FEATURE_EH_WINDOWS_X86)
    if (!UsesFunclets())
    {
        // The continuation may be a finalStep block.
        // It is now a normal block, so clear the special keep
        // always flag.
        continuation->RemoveFlags(BBF_KEEP_BBJ_ALWAYS);

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
    }
#endif // FEATURE_EH_WINDOWS_X86
}

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
//
PhaseStatus Compiler::fgMergeFinallyChains()
{
    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // We need to update the bbPreds lists.
    assert(fgPredsComputed);

    if (compHndBBtabCount == 0)
    {
        JITDUMP("No EH in this method, nothing to merge.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.MinOpts())
    {
        JITDUMP("Method compiled with MinOpts, no merging.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.compDbgCode)
    {
        JITDUMP("Method compiled with debug codegen, no merging.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool enableMergeFinallyChains = true;

#if defined(FEATURE_EH_WINDOWS_X86)
    if (!UsesFunclets())
    {
        // For non-funclet models (x86) the callfinallys may contain
        // statements and the continuations contain GT_END_LFINs.  So no
        // merging is possible until the GT_END_LFIN blocks can be merged
        // and merging is not safe unless the callfinally blocks are split.
        JITDUMP("EH using non-funclet model; merging not yet implemented.\n");
        enableMergeFinallyChains = false;
    }
#endif // FEATURE_EH_WINDOWS_X86

    if (!UsesCallFinallyThunks())
    {
        // For non-thunk EH models (x86) the callfinallys may contain
        // statements, and merging is not safe unless the callfinally
        // blocks are split.
        JITDUMP("EH using non-callfinally thunk model; merging not yet implemented.\n");
        enableMergeFinallyChains = false;
    }

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
        BasicBlock* lastCallFinallyRangeBlock  = nullptr;
        ehGetCallFinallyBlockRange(XTnum, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

        // Clear out any stale entries in the continuation map
        continuationMap.RemoveAll();

        // Build a map from each continuation to the "canonical"
        // callfinally for that continuation.
        unsigned          callFinallyCount  = 0;
        BasicBlock* const beginHandlerBlock = HBtab->ebdHndBeg;

        for (BasicBlock* const currentBlock : Blocks(firstCallFinallyRangeBlock, lastCallFinallyRangeBlock))
        {
            // Ignore "retless" callfinallys (where the finally doesn't return).
            if (currentBlock->isBBCallFinallyPair() && currentBlock->TargetIs(beginHandlerBlock))
            {
                // The callfinally must be empty, so that we can
                // safely retarget anything that branches here to
                // another callfinally with the same continuation.
                assert(currentBlock->isEmpty());

                // This callfinally invokes the finally for this try.
                callFinallyCount++;

                // Locate the continuation
                BasicBlock* const leaveBlock        = currentBlock->Next();
                BasicBlock* const continuationBlock = leaveBlock->GetFinallyContinuation();

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
        for (BasicBlock* const currentBlock : Blocks(firstCallFinallyRangeBlock, lastCallFinallyRangeBlock))
        {
            const bool merged =
                fgRetargetBranchesToCanonicalCallFinally(currentBlock, beginHandlerBlock, continuationMap);
            didMerge = didMerge || merged;
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
//
bool Compiler::fgRetargetBranchesToCanonicalCallFinally(BasicBlock*      block,
                                                        BasicBlock*      handler,
                                                        BlockToBlockMap& continuationMap)
{
    // We expect callfinallys to be invoked by a BBJ_ALWAYS at this
    // stage in compilation, or BBJ_CALLFINALLYRET in the case of a chain of callfinallys.
    if (!block->KindIs(BBJ_ALWAYS, BBJ_CALLFINALLYRET))
    {
        // Possible paranoia assert here -- no flow successor of
        // this block should be a callfinally for this try.
        return false;
    }

    // Screen out cases that are not callfinallys to the right
    // handler.
    BasicBlock* const callFinally = block->GetTarget();

    if (!callFinally->isBBCallFinallyPair())
    {
        return false;
    }

    if (!callFinally->TargetIs(handler))
    {
        return false;
    }

    // Ok, this is a callfinally that invokes the right handler.
    // Get its continuation.
    BasicBlock* const leaveBlock        = callFinally->Next();
    BasicBlock* const continuationBlock = leaveBlock->GetFinallyContinuation();

    // Find the canonical callfinally for that continuation.
    BasicBlock* const canonicalCallFinally = continuationMap[continuationBlock];
    assert(canonicalCallFinally != nullptr);

    // If the block already jumps to the canonical call finally, no work needed.
    if (block->TargetIs(canonicalCallFinally))
    {
        JITDUMP(FMT_BB " already canonical\n", block->bbNum);
        return false;
    }

    // Else, retarget it so that it does...
    JITDUMP("Redirecting branch in " FMT_BB " from " FMT_BB " to " FMT_BB ".\n", block->bbNum, callFinally->bbNum,
            canonicalCallFinally->bbNum);

    assert(callFinally->bbRefs > 0);
    fgRedirectTargetEdge(block, canonicalCallFinally);

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

        BasicBlock* const canonicalLeaveBlock = canonicalCallFinally->Next();

        weight_t const canonicalLeaveWeight =
            canonicalLeaveBlock->hasProfileWeight() ? canonicalLeaveBlock->bbWeight : BB_ZERO_WEIGHT;
        weight_t const newLeaveWeight = block->bbWeight + canonicalLeaveWeight;

        canonicalLeaveBlock->setBBProfileWeight(newLeaveWeight);

        // Remove weight from the old call finally pair.
        //
        if (callFinally->hasProfileWeight())
        {
            callFinally->decreaseBBProfileWeight(block->bbWeight);
        }

        if (leaveBlock->hasProfileWeight())
        {
            leaveBlock->decreaseBBProfileWeight(block->bbWeight);
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
    assert(fgPredsComputed);

    struct ThrowHelper
    {
        BasicBlock*  m_block;
        GenTreeCall* m_call;

        ThrowHelper()
            : m_block(nullptr)
            , m_call(nullptr)
        {
        }

        ThrowHelper(BasicBlock* block, GenTreeCall* call)
            : m_block(block)
            , m_call(call)
        {
        }

        static bool Equals(const ThrowHelper& x, const ThrowHelper& y)
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

    // First pass
    //
    // Scan for THROW blocks. Note early on in compilation (before morph)
    // noreturn blocks are not marked as BBJ_THROW.
    //
    // Walk blocks from last to first so that any branches we
    // introduce to the canonical blocks end up lexically forward
    // and there is less jumbled flow to sort out later.
    for (BasicBlock* block = fgLastBB; block != nullptr; block = block->Prev())
    {
        // Workaround: don't consider try entry blocks as candidates
        // for merging; if the canonical throw is later in the same try,
        // we'll create invalid flow.
        if (bbIsTryBeg(block))
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
        }
        else
        {
            // No, add this as the canonical example
            JITDUMP("    in " FMT_BB " is unique, marking it as canonical\n", block->bbNum);
            callMap.Set(key, block);
        }
    }

    // Bail if no candidates were found
    const unsigned numCandidates = blockMap.GetCount();
    if (numCandidates == 0)
    {
        JITDUMP("\n*************** no throws can be tail merged, sorry\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    JITDUMP("\n*** found %d merge candidates, rewriting flow\n\n", numCandidates);
    bool modifiedProfile = false;

    // Second pass.
    //
    // We walk the map rather than the block list, to save a bit of time.
    for (BlockToBlockMap::Node* const iter : BlockToBlockMap::KeyValueIteration(&blockMap))
    {
        BasicBlock* const nonCanonicalBlock = iter->GetKey();
        BasicBlock* const canonicalBlock    = iter->GetValue();
        weight_t          removedWeight     = BB_ZERO_WEIGHT;

        // Walk pred list of the non canonical block, updating flow to target
        // the canonical block instead.
        for (FlowEdge* const predEdge : nonCanonicalBlock->PredEdgesEditing())
        {
            removedWeight += predEdge->getLikelyWeight();
            BasicBlock* const predBlock = predEdge->getSourceBlock();
            JITDUMP("*** " FMT_BB " now branching to " FMT_BB "\n", predBlock->bbNum, canonicalBlock->bbNum);
            fgReplaceJumpTarget(predBlock, nonCanonicalBlock, canonicalBlock);
        }

        if (canonicalBlock->hasProfileWeight())
        {
            canonicalBlock->increaseBBProfileWeight(removedWeight);
            modifiedProfile = true;

            // Don't bother updating flow into nonCanonicalBlock, since it is now unreachable
        }
    }

    // In practice, when we have true profile data, we can repair it locally above, since the no-return
    // calls mean that there is no contribution from the throw blocks to any of their successors.
    // However, these blocks won't be morphed into BBJ_THROW blocks until later,
    // so mark profile data as inconsistent for now.
    if (modifiedProfile)
    {
        JITDUMP(
            "fgTailMergeThrows: Modified flow into no-return blocks that still have successors. Data %s inconsistent.\n",
            fgPgoConsistent ? "is now" : "was already");
        fgPgoConsistent = false;
    }

    // Update the count of noreturn call sites
    //
    JITDUMP("Made %u updates\n", numCandidates);
    assert(numCandidates < optNoReturnCallCount);
    optNoReturnCallCount -= numCandidates;

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// fgCloneTryRegion: clone a try region
//
// Arguments:
//    tryEntry     -- try entry block
//    info         -- [in, out] information about the cloning
//    insertAfter  -- [in, out] pointer to block to insert new blocks after
//
// Returns:
//    If insertAfter == nullptr, check if cloning is possible
//      return nullptr if not, tryEntry if so
//    else
//      Return the cloned try entry, or nullptr if cloning failed
//         cloned blocks will be created and scaled by profile weight
//         and if info.AddEdges is true have proper bbkinds and flow edges
//      info data will be updated:
//         Map will be modified to contain keys and for the blocks cloned
//         Visited will include bits for each newly cloned block
//         m_ehRegionShift will describe number of EH regions added
//      insertAfter will point at the lexcially last block cloned
//
// Notes:
//   * if insertAfter is non null, map must also be non null
//
//   * If info.Map is not nullptr, it is not modified unless cloning succeeds
//     When cloning succeeds, entries for the try blocks and related blocks
//     (handler, filter, callfinally) will be updated; other map entries will
//     be left as they were
//
//   * the insertion point must be lexically after the original try region
//     and be a block in the enclosing region for the original try.
//
//   * If cloning and adding edges,
//     The new try region entry will not be reachable by any uncloned block.
//     The new try region exits will target the same uncloned blocks as the original,
//       or as directed by pre-existing map entries.
//
BasicBlock* Compiler::fgCloneTryRegion(BasicBlock* tryEntry, CloneTryInfo& info, BasicBlock** insertAfter)
{
    assert(bbIsTryBeg(tryEntry));
    bool const deferCloning = (insertAfter == nullptr);
    assert(deferCloning || ((*insertAfter != nullptr) && (info.Map != nullptr)));
    INDEBUG(const char* msg = deferCloning ? "Checking if it is possible to clone" : "Cloning";)
    JITDUMP("%s the try region EH#%02u headed by " FMT_BB "\n", msg, tryEntry->getTryIndex(), tryEntry->bbNum);

    // Determine the extent of cloning.
    //
    // We need to clone to the entire try region plus any
    // enclosed regions and any enclosing mutual protect regions,
    // plus all the the associated handlers and filters and any
    // regions they enclose, plus any callfinallies that follow.
    //
    // This is necessary because try regions can't have multiple entries, or
    // share parts in any meaningful way.
    //
    CompAllocator        alloc = getAllocator(CMK_TryRegionClone);
    ArrayStack<unsigned> regionsToProcess(alloc);
    unsigned const       tryIndex              = tryEntry->getTryIndex();
    unsigned             numberOfBlocksToClone = 0;

    // Track blocks to clone for caller, or if we are cloning and
    // caller doesn't care.
    //
    jitstd::vector<BasicBlock*>* blocks = info.BlocksToClone;
    if (!deferCloning && (blocks == nullptr))
    {
        blocks = new (alloc) jitstd::vector<BasicBlock*>(alloc);
    }

    unsigned               regionCount = 0;
    BitVecTraits* const    traits      = &info.Traits;
    BitVec&                visited     = info.Visited;
    BlockToBlockMap* const map         = info.Map;

    auto addBlockToClone = [=, &blocks, &visited, &numberOfBlocksToClone](BasicBlock* block, const char* msg) {
        if (!BitVecOps::TryAddElemD(traits, visited, block->bbID))
        {
            JITDUMP("[already seen]  %s block " FMT_BB "\n", msg, block->bbNum);
            return false;
        }

        JITDUMP("  %s block " FMT_BB "\n", msg, block->bbNum);

        numberOfBlocksToClone++;

        if (blocks != nullptr)
        {
            blocks->push_back(block);
        }
        return true;
    };

    JITDUMP("==> try EH#%02u\n", tryIndex);
    regionsToProcess.Push(tryIndex);

    // Walk through each try region
    //
    while (regionsToProcess.Height() > 0)
    {
        regionCount++;
        unsigned const  regionIndex = regionsToProcess.Pop();
        EHblkDsc* const ebd         = ehGetDsc(regionIndex);
        JITDUMP("== processing try EH#%02u\n", regionIndex);

        // Walk the try region
        //
        BasicBlock* const firstTryBlock = ebd->ebdTryBeg;
        BasicBlock* const lastTryBlock  = ebd->ebdTryLast;

        if (BitVecOps::IsMember(traits, visited, firstTryBlock->bbID))
        {
            JITDUMP("already walked try region for EH#%02u\n", regionIndex);
            assert(BitVecOps::IsMember(traits, visited, lastTryBlock->bbID));
        }
        else
        {
            JITDUMP("walking try region for EH#%02u\n", regionIndex);
            for (BasicBlock* const block : Blocks(firstTryBlock, lastTryBlock))
            {
                bool added = addBlockToClone(block, "try region");
                if (bbIsTryBeg(block) && (block != ebd->ebdTryBeg))
                {
                    assert(added);
                    JITDUMP("==> found try EH#%02u nested in try EH#%02u region at " FMT_BB "\n", block->getTryIndex(),
                            regionIndex, block->bbNum);
                    regionsToProcess.Push(block->getTryIndex());
                }
            }
        }

        // Walk the callfinally region
        //
        if (ebd->HasFinallyHandler())
        {
            BasicBlock* firstCallFinallyRangeBlock = nullptr;
            BasicBlock* lastCallFinallyRangeBlock  = nullptr;
            ehGetCallFinallyBlockRange(regionIndex, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

            // Note this range is potentially quite broad...
            // Instead perhaps just walk preds of the handler?
            //
            JITDUMP("walking callfinally region for EH#%02u [" FMT_BB " ... " FMT_BB "]\n", regionIndex,
                    firstCallFinallyRangeBlock->bbNum, lastCallFinallyRangeBlock->bbNum);

            for (BasicBlock* const block : Blocks(firstCallFinallyRangeBlock, lastCallFinallyRangeBlock))
            {
                if (block->KindIs(BBJ_CALLFINALLY) && block->TargetIs(ebd->ebdHndBeg))
                {
                    addBlockToClone(block, "callfinally");
                }
                else if (block->KindIs(BBJ_CALLFINALLYRET) && block->Prev()->TargetIs(ebd->ebdHndBeg))
                {
                    addBlockToClone(block, "callfinallyret");

#if defined(FEATURE_EH_WINDOWS_X86)

                    // For non-funclet X86 we must also clone the next block after the callfinallyret.
                    // (it will contain an END_LFIN). But if this block is also a CALLFINALLY we
                    // bail out, since we can't clone it in isolation, but we need to clone it.
                    // (a proper fix would be to split the block, perhaps).
                    //
                    if (!UsesFunclets())
                    {
                        BasicBlock* const lfin = block->GetTarget();

                        if (lfin->KindIs(BBJ_CALLFINALLY))
                        {
                            JITDUMP("Can't clone, as an END_LFIN is contained in CALLFINALLY block " FMT_BB "\n",
                                    lfin->bbNum);
                            return nullptr;
                        }
                        addBlockToClone(lfin, "lfin-continuation");
                    }
#endif
                }
            }
        }

        // Walk the filter region
        //
        if (ebd->HasFilter())
        {
            BasicBlock* const firstFltBlock = ebd->ebdFilter;
            BasicBlock* const lastFltBlock  = ebd->BBFilterLast();

            if (BitVecOps::IsMember(traits, visited, firstFltBlock->bbID))
            {
                JITDUMP("already walked filter region for EH#%02u\n", regionIndex);
                assert(BitVecOps::IsMember(traits, visited, lastFltBlock->bbID));
            }
            else
            {
                JITDUMP("walking filter region for EH#%02u\n", regionIndex);
                for (BasicBlock* const block : Blocks(firstFltBlock, lastFltBlock))
                {
                    // A filter cannot enclose another EH region
                    //
                    assert(!bbIsTryBeg(block));
                    addBlockToClone(block, "filter region");
                }
            }
        }

        // Walk the handler region
        //
        BasicBlock* const firstHndBlock = ebd->ebdHndBeg;
        BasicBlock* const lastHndBlock  = ebd->ebdHndLast;

        if (BitVecOps::IsMember(traits, visited, firstHndBlock->bbID))
        {
            JITDUMP("already walked handler region for EH#%02u\n", regionIndex);
            assert(BitVecOps::IsMember(traits, visited, lastHndBlock->bbID));
        }
        else
        {
            JITDUMP("walking handler region for EH#%02u\n", regionIndex);
            for (BasicBlock* const block : Blocks(firstHndBlock, lastHndBlock))
            {
                bool added = addBlockToClone(block, "handler region");
                if (bbIsTryBeg(block))
                {
                    assert(added);
                    JITDUMP("==> found try entry for EH#%02u nested in handler at " FMT_BB "\n", block->bbNum,
                            block->getTryIndex());
                    regionsToProcess.Push(block->getTryIndex());
                }
            }
        }

        // If there is an enclosing mutual-protect region, process it as well
        //
        unsigned const enclosingTryIndex = ebd->ebdEnclosingTryIndex;
        if (enclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            EHblkDsc* const enclosingTryEbd = ehGetDsc(enclosingTryIndex);

            if (EHblkDsc::ebdIsSameTry(ebd, enclosingTryEbd))
            {
                JITDUMP("==> found mutual-protect try EH#%02u for EH#%02u\n", enclosingTryIndex, regionIndex);
                regionsToProcess.Push(enclosingTryIndex);
            }
        }

        JITDUMP("<== finished try EH#%02u\n", regionIndex);
    }

    // Find the outermost mutual-protect try region that begins at tryEntry
    //
    EHblkDsc* const tryEbd            = ehGetDsc(tryIndex);
    unsigned        outermostTryIndex = tryIndex;
    unsigned        enclosingTryIndex = EHblkDsc::NO_ENCLOSING_INDEX;
    {
        EHblkDsc* outermostEbd = ehGetDsc(outermostTryIndex);
        while (true)
        {
            enclosingTryIndex = outermostEbd->ebdEnclosingTryIndex;
            if (enclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                break;
            }
            outermostEbd = ehGetDsc(enclosingTryIndex);
            if (!EHblkDsc::ebdIsSameTry(outermostEbd, tryEbd))
            {
                break;
            }
            outermostTryIndex = enclosingTryIndex;
        }
    }

    unsigned enclosingHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;
    if (tryEntry->hasHndIndex())
    {
        enclosingHndIndex = tryEntry->getHndIndex();
    }

    // Now blocks contains an entry for each block to clone.
    //
    JITDUMP("Will need to clone %u EH regions (outermost: EH#%02u) and %u blocks\n", regionCount, outermostTryIndex,
            numberOfBlocksToClone);

    // Allocate the new EH clauses. First, find the enclosing EH clause, if any...
    // we will want to allocate the new clauses just "before" this point.
    //
    // If the region we're cloning is not enclosed, we put it at the end of the table;
    // this is cheaper than any other insertion point, as no existing regions get renumbered.
    //
    unsigned insertBeforeIndex = enclosingTryIndex;
    if ((enclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) && (enclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX))
    {
        JITDUMP("No enclosing EH region; cloned EH clauses will go at the end of the EH table\n");
        insertBeforeIndex = compHndBBtabCount;
    }
    else if ((enclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) || (enclosingHndIndex < enclosingTryIndex))
    {
        JITDUMP("Cloned EH clauses will go before enclosing handler region EH#%02u\n", enclosingHndIndex);
        insertBeforeIndex = enclosingHndIndex;
    }
    else
    {
        JITDUMP("Cloned EH clauses will go before enclosing try region EH#%02u\n", enclosingTryIndex);
        assert(insertBeforeIndex == enclosingTryIndex);
    }

    // Once we call fgTryAddEHTableEntries with deferCloning = false,
    // all the EH indicies at or above insertBeforeIndex will shift,
    // and the EH table may reallocate.
    //
    // This addition may also fail, if the table would become too large...
    //
    EHblkDsc* const clonedOutermostEbd =
        fgTryAddEHTableEntries(insertBeforeIndex, regionCount, /* deferAdding */ deferCloning);

    if (clonedOutermostEbd == nullptr)
    {
        JITDUMP("fgCloneTryRegion: unable to expand EH table\n");
        return nullptr;
    }

    if (deferCloning)
    {
        JITDUMP("fgCloneTryRegion: cloning is possible\n");
        return tryEntry;
    }

    // None of the EH regions we're cloning should have been renumbered,
    // though their clauses may have been moved to a new table..
    //
    EHblkDsc* const oldTryEbd = ehGetDsc(outermostTryIndex);
    assert(oldTryEbd->ebdTryBeg == tryEntry);

    // Callers will see enclosing EH region indices shift by this much
    //
    info.EHIndexShift = regionCount;

    // The EH table now looks like the following, for a middle insertion:
    //
    // ===================
    // EH 0                     -- unrelated regions
    // ...
    // ---------------
    // EH x                     -- innermost region to clone
    // ...
    // EH x + regionCount - 1   -- outermost region to clone
    // ---------------
    // ---------------
    // EH x + regionCount       -- innermost cloned region
    // ...
    // EH x + 2*regionCount - 1 -- outermost cloned region
    // ---------------
    // ...
    // EH k -- enclosing try / hnd regions (if any), or other regions
    //
    // ===================
    //
    // And like this, for an end insertion:
    //
    // ===================
    // EH 0                     -- unrelated regions
    // ...
    // ---------------
    // EH x                     -- innermost region to clone
    // ...
    // EH x + regionCount - 1   -- outermost region to clone
    // ---------------
    // ...
    // EH k                     -- unrelated regions
    // ...
    // ---------------
    // EH c                     -- innermost cloned region
    // ...
    // EH c + regionCount - 1   -- outermost cloned region
    // ---------------
    // ===================
    //
    // So the cloned clauses will have higher indices, and each cloned clause
    // should be the same distance from its original, but that distance
    // depends on the kind of insertion.
    //
    // Compute that distance as `indexShift`.
    //
    unsigned const clonedOutermostRegionIndex = ehGetIndex(clonedOutermostEbd);
    assert(clonedOutermostRegionIndex > outermostTryIndex);
    unsigned const indexShift = clonedOutermostRegionIndex - outermostTryIndex;

    // Copy over the EH table entries and adjust their enclosing indicies.
    // We will adjust the block references below.
    //
    unsigned const clonedLowestRegionIndex = clonedOutermostRegionIndex - regionCount + 1;
    JITDUMP("New EH regions are EH#%02u ... EH#%02u\n", clonedLowestRegionIndex, clonedOutermostRegionIndex);
    for (unsigned XTnum = clonedLowestRegionIndex; XTnum <= clonedOutermostRegionIndex; XTnum++)
    {
        unsigned originalXTnum = XTnum - indexShift;
        compHndBBtab[XTnum]    = compHndBBtab[originalXTnum];
        EHblkDsc* const ebd    = &compHndBBtab[XTnum];

        // Note the outermost region enclosing indices stay the same, because the original
        // clause entries got adjusted when we inserted the new clauses.
        //
        if (ebd->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            if (XTnum < clonedOutermostRegionIndex)
            {
                ebd->ebdEnclosingTryIndex += (unsigned short)indexShift;
            }
            JITDUMP("EH#%02u now enclosed in try EH#%02u\n", XTnum, ebd->ebdEnclosingTryIndex);
        }
        else
        {
            JITDUMP("EH#%02u not enclosed in any try\n", XTnum);
        }

        if (ebd->ebdEnclosingHndIndex != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            if (XTnum < clonedOutermostRegionIndex)
            {
                ebd->ebdEnclosingHndIndex += (unsigned short)indexShift;
            }
            JITDUMP("EH#%02u now enclosed in handler EH#%02u\n", XTnum, ebd->ebdEnclosingHndIndex);
        }
        else
        {
            JITDUMP("EH#%02u not enclosed in any handler\n", XTnum);
        }
    }

    // Clone the blocks.
    //
    // All blocks are initially put into the enclosing EH region, and it is not
    // extended to cover them all. The step below puts the blocks into the
    // appropriate cloned region and fixes up region extents.
    //
    JITDUMP("Cloning blocks for try...\n");
    for (BasicBlock* const block : *blocks)
    {
        BasicBlock* const newBlock = fgNewBBafter(BBJ_ALWAYS, *insertAfter, /* extendRegion */ false);
        JITDUMP("Adding " FMT_BB " (copy of " FMT_BB ") after " FMT_BB "\n", newBlock->bbNum, block->bbNum,
                (*insertAfter)->bbNum);
        map->Set(block, newBlock, BlockToBlockMap::SetKind::Overwrite);
        BasicBlock::CloneBlockState(this, newBlock, block);
        newBlock->scaleBBWeight(info.ProfileScale);

        if (info.ScaleOriginalBlockProfile)
        {
            weight_t originalScale = max(0.0, 1.0 - info.ProfileScale);
            block->scaleBBWeight(originalScale);
        }

        *insertAfter = newBlock;
    }
    JITDUMP("Done cloning blocks for try...\n");

    // Update the cloned block regions and impacted EH clauses
    //
    // Here we are assuming that the cloned try is always placed lexically *after* thge
    // original, so that if the original try ended at the same point as an enclosing try,
    // the new end point of the enclosing try is in the cloned try.
    //
    JITDUMP("Fixing region indices...\n");
    for (BasicBlock* const block : *blocks)
    {
        BasicBlock* newBlock = nullptr;
        bool        found    = map->Lookup(block, &newBlock);
        assert(found);

        // Update block references in the EH table
        //
        // `region` is the index of a cloned EH clause that may still refer to `block`.
        // Update these block references and those of enclosing regions to refer to `newBlock`.
        //
        auto updateBlockReferences = [=](unsigned region) {
            while (true)
            {
                EHblkDsc* const ebd = ehGetDsc(region);

                if (ebd->ebdTryBeg == block)
                {
                    ebd->ebdTryBeg = newBlock;
                    JITDUMP("Try begin for EH#%02u is " FMT_BB "\n", region, newBlock->bbNum);
                }

                if (ebd->ebdTryLast == block)
                {
                    fgSetTryEnd(ebd, newBlock);
                }

                if (ebd->ebdHndBeg == block)
                {
                    ebd->ebdHndBeg = newBlock;
                    JITDUMP("Handler begin for EH#%02u is " FMT_BB "\n", region, newBlock->bbNum);
                }

                if (ebd->ebdHndLast == block)
                {
                    fgSetHndEnd(ebd, newBlock);
                }

                if (ebd->HasFilter() && (ebd->ebdFilter == block))
                {
                    ebd->ebdFilter = newBlock;
                    JITDUMP("Filter begin for EH#%02u is " FMT_BB "\n", region, newBlock->bbNum);
                }

                bool inTry = false;
                region     = ehGetEnclosingRegionIndex(region, &inTry);

                if (region == EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    break;
                }
            }
        };

        // Fix the EH regions for each cloned block, and the block
        // references in the EH table entries.
        //
        // If the block's try index was outside of the original try region
        // (say a handler for the try) then it is already properly adjusted.
        //
        if (block->hasTryIndex())
        {
            const unsigned originalTryIndex = block->getTryIndex();
            unsigned       cloneTryIndex    = originalTryIndex;

            if (originalTryIndex < enclosingTryIndex)
            {
                cloneTryIndex += indexShift;
            }

            newBlock->setTryIndex(cloneTryIndex);
            updateBlockReferences(cloneTryIndex);
        }

        if (block->hasHndIndex())
        {
            const unsigned originalHndIndex = block->getHndIndex();
            unsigned       cloneHndIndex    = originalHndIndex;

            if (originalHndIndex < enclosingHndIndex)
            {
                cloneHndIndex += indexShift;
            }

            newBlock->setHndIndex(cloneHndIndex);
            updateBlockReferences(cloneHndIndex);

            // Handler and filter entries also have an
            // additional artificial reference count.
            //
            if (bbIsHandlerBeg(newBlock))
            {
                newBlock->bbRefs++;
            }
        }
    }
    JITDUMP("Done fixing region indices\n");

    // Redirect any branches within the newly-cloned blocks or
    // from cloned blocks to non-cloned blocks
    //
    if (info.AddEdges)
    {
        JITDUMP("Adding edges in the newly cloned try\n");
        for (BasicBlock* const block : BlockToBlockMap::KeyIteration(map))
        {
            BasicBlock* newBlock = (*map)[block];
            // Jump kind/target should not be set yet
            assert(newBlock->KindIs(BBJ_ALWAYS));
            assert(!newBlock->HasInitializedTarget());
            optSetMappedBlockTargets(block, newBlock, map);
        }
    }
    else
    {
        JITDUMP("Not adding edges in the newly cloned try\n");
    }

    // If the original regions had any ACDs, create equivalent
    // ones for the cloned regions
    //
    if (fgHasAddCodeDscMap())
    {
        AddCodeDscMap* const    map = fgGetAddCodeDscMap();
        ArrayStack<AddCodeDsc*> cloned(getAllocator(CMK_TryRegionClone));

        assert(clonedLowestRegionIndex >= indexShift);
        assert(clonedOutermostRegionIndex >= indexShift);

        unsigned const originalLowestRegionIndex    = clonedLowestRegionIndex - indexShift;
        unsigned const originalOutermostRegionIndex = clonedOutermostRegionIndex - indexShift;

        for (AddCodeDsc* const add : AddCodeDscMap::ValueIteration(map))
        {
            bool needsCloningForTry = false;
            bool needsCloningForHnd = false;
            bool inTry              = add->acdTryIndex > 0;
            bool inHnd              = add->acdHndIndex > 0;

            // acd region numbers are shifted up by one so
            // that a value of zero means "not in an EH region"
            //
            if (inTry)
            {
                unsigned const trueAcdTryIndex = add->acdTryIndex - 1;

                if ((trueAcdTryIndex >= originalLowestRegionIndex) && (trueAcdTryIndex <= originalOutermostRegionIndex))
                {
                    needsCloningForTry = true;
                }
            }

            if (inHnd)
            {
                unsigned const trueAcdHndIndex = add->acdHndIndex - 1;

                if ((trueAcdHndIndex >= originalLowestRegionIndex) && (trueAcdHndIndex <= originalOutermostRegionIndex))
                {
                    needsCloningForHnd = true;
                }
            }

            if (!needsCloningForTry && !needsCloningForHnd)
            {
                continue;
            }

            JITDUMP("Will need to clone: ");
            JITDUMPEXEC(add->Dump());

            AddCodeDsc* clone = new (this, CMK_Unknown) AddCodeDsc;
            clone->acdDstBlk  = nullptr;

            if (needsCloningForTry)
            {
                clone->acdTryIndex = (unsigned short)(add->acdTryIndex + indexShift);
            }
            else if (inTry)
            {
                clone->acdTryIndex = add->acdTryIndex;
            }
            else
            {
                clone->acdTryIndex = 0;
            }

            if (needsCloningForHnd)
            {
                clone->acdHndIndex = (unsigned short)(add->acdHndIndex + indexShift);
            }
            else if (inHnd)
            {
                clone->acdHndIndex = add->acdHndIndex;
            }
            else
            {
                clone->acdHndIndex = 0;
            }

            clone->acdKeyDsg = add->acdKeyDsg;
            clone->acdKind   = add->acdKind;
            clone->acdUsed   = false;

#if !FEATURE_FIXED_OUT_ARGS
            clone->acdStkLvl     = 0;
            clone->acdStkLvlInit = false;
#endif // !FEATURE_FIXED_OUT_ARGS
            INDEBUG(clone->acdNum = acdCount++);
            cloned.Push(clone);
        }

        while (cloned.Height() > 0)
        {
            AddCodeDsc* const clone = cloned.Pop();
            AddCodeDscKey     key(clone);
            map->Set(key, clone);
            JITDUMP("Added clone: ");
            JITDUMPEXEC(clone->Dump());
        }
    }

    BasicBlock* const clonedTryEntry = (*map)[tryEntry];
    JITDUMP("Done cloning, cloned try entry is " FMT_BB "\n", clonedTryEntry->bbNum);
    return clonedTryEntry;
}

//------------------------------------------------------------------------
// fgCanCloneTryRegion: see if a try region can be cloned
//
// Arguments:
//    tryEntry - try entry block
//
// Returns:
//    true if try region is clonable
//
bool Compiler::fgCanCloneTryRegion(BasicBlock* tryEntry)
{
    assert(bbIsTryBeg(tryEntry));

    BitVecTraits      traits(compBasicBlockID, this);
    CloneTryInfo      info(traits);
    BasicBlock* const result = fgCloneTryRegion(tryEntry, info);
    return result != nullptr;
}

//------------------------------------------------------------------------
// CloneTryInfo::CloneTryInfo: construct an object for cloning a try region
//
// Arguments:
//    traits - bbID based traits to use for the Visited set
//
CloneTryInfo::CloneTryInfo(BitVecTraits& traits)
    : Traits(traits)
    , Visited(BitVecOps::MakeEmpty(&Traits))
{
}
