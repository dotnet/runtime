// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              Optimizer                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************/

void Compiler::optInit()
{
    fgHasLoops          = false;
    loopAlignCandidates = 0;

    /* Initialize the # of tracked loops to 0 */
    optLoopCount              = 0;
    optLoopTable              = nullptr;
    optLoopTableValid         = false;
    optLoopsRequirePreHeaders = false;
    optCurLoopEpoch           = 0;

#ifdef DEBUG
    loopsAligned = 0;
#endif

    /* Keep track of the number of calls and indirect calls made by this method */
    optCallCount         = 0;
    optIndirectCallCount = 0;
    optNativeCallCount   = 0;
    optAssertionCount    = 0;
    optAssertionDep      = nullptr;
    optCSEstart          = BAD_VAR_NUM;
    optCSEcount          = 0;
}

DataFlow::DataFlow(Compiler* pCompiler) : m_pCompiler(pCompiler)
{
}

//------------------------------------------------------------------------
// optSetBlockWeights: adjust block weights, as follows:
// 1. A block that is not reachable from the entry block is marked "run rarely".
// 2. If we're not using profile weights, then any block with a non-zero weight
//    that doesn't dominate all the return blocks has its weight dropped in half
//    (but only if the first block *does* dominate all the returns).
//
// Returns:
//    Suitable phase status
//
// Notes:
//    Depends on dominators, and fgReturnBlocks being set.
//
PhaseStatus Compiler::optSetBlockWeights()
{
    noway_assert(opts.OptimizationEnabled());
    assert(fgDomsComputed);
    assert(fgReturnBlocksComputed);

    bool       madeChanges                = false;
    bool       firstBBDominatesAllReturns = true;
    const bool usingProfileWeights        = fgIsUsingProfileWeights();

    for (BasicBlock* const block : Blocks())
    {
        /* Blocks that can't be reached via the first block are rarely executed */
        if (!fgReachable(fgFirstBB, block) && !block->isRunRarely())
        {
            madeChanges = true;
            block->bbSetRunRarely();
        }

        if (!usingProfileWeights && firstBBDominatesAllReturns)
        {
            // If the weight is already zero (and thus rarely run), there's no point scaling it.
            if (block->bbWeight != BB_ZERO_WEIGHT)
            {
                // If the block dominates all return blocks, leave the weight alone. Otherwise,
                // scale the weight by 0.5 as a heuristic that some other path gets some of the dynamic flow.
                // Note that `optScaleLoopBlocks` has a similar heuristic for loop blocks that don't dominate
                // their loop back edge.

                bool blockDominatesAllReturns = true; // Assume that we will dominate

                for (BasicBlockList* retBlocks = fgReturnBlocks; retBlocks != nullptr; retBlocks = retBlocks->next)
                {
                    if (!fgDominate(block, retBlocks->block))
                    {
                        blockDominatesAllReturns = false;
                        break;
                    }
                }

                if (block == fgFirstBB)
                {
                    firstBBDominatesAllReturns = blockDominatesAllReturns;

                    // Don't scale the weight of the first block, since it is guaranteed to execute.
                    // If the first block does not dominate all the returns, we won't scale any of the function's
                    // block weights.
                }
                else
                {
                    // If we are not using profile weight then we lower the weight
                    // of blocks that do not dominate a return block
                    //
                    if (!blockDominatesAllReturns)
                    {
                        madeChanges = true;

                        // TODO-Cleanup: we should use:
                        //    block->scaleBBWeight(0.5);
                        // since we are inheriting "from ourselves", but that leads to asm diffs due to minutely
                        // different floating-point value in the calculation, and some code that compares weights
                        // for equality.
                        block->inheritWeightPercentage(block, 50);
                    }
                }
            }
        }
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// optScaleLoopBlocks: Scale the weight of loop blocks from 'begBlk' to 'endBlk'.
//
// Arguments:
//      begBlk - first block of range. Must be marked as a loop head (BBF_LOOP_HEAD).
//      endBlk - last block of range (inclusive). Must be reachable from `begBlk`.
//
// Operation:
//      Calculate the 'loop weight'. This is the amount to scale the weight of each block in the loop.
//      Our heuristic is that loops are weighted eight times more than straight-line code
//      (scale factor is BB_LOOP_WEIGHT_SCALE). If the loops are all properly formed this gives us these weights:
//
//          1 -- non-loop basic block
//          8 -- single loop nesting
//         64 -- double loop nesting
//        512 -- triple loop nesting
//
void Compiler::optScaleLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk)
{
    noway_assert(begBlk->bbNum <= endBlk->bbNum);
    noway_assert(begBlk->isLoopHead());
    noway_assert(fgReachable(begBlk, endBlk));
    noway_assert(!opts.MinOpts());

#ifdef DEBUG
    if (verbose)
    {
        printf("\nMarking a loop from " FMT_BB " to " FMT_BB, begBlk->bbNum, endBlk->bbNum);
    }
#endif

    // Build list of back edges for block begBlk.
    FlowEdge* backedgeList = nullptr;

    for (BasicBlock* const predBlock : begBlk->PredBlocks())
    {
        // Is this a back edge?
        if (predBlock->bbNum >= begBlk->bbNum)
        {
            backedgeList = new (this, CMK_FlowEdge) FlowEdge(predBlock, backedgeList);

#if MEASURE_BLOCK_SIZE
            genFlowNodeCnt += 1;
            genFlowNodeSize += sizeof(FlowEdge);
#endif // MEASURE_BLOCK_SIZE
        }
    }

    // At least one backedge must have been found (the one from endBlk).
    noway_assert(backedgeList);

    auto reportBlockWeight = [&](BasicBlock* blk, const char* message) {
#ifdef DEBUG
        if (verbose)
        {
            printf("\n    " FMT_BB "(wt=" FMT_WT ")%s", blk->bbNum, blk->getBBWeight(this), message);
        }
#endif // DEBUG
    };

    for (BasicBlock* const curBlk : BasicBlockRangeList(begBlk, endBlk))
    {
        // Don't change the block weight if it came from profile data.
        if (curBlk->hasProfileWeight() && fgHaveProfileData())
        {
            reportBlockWeight(curBlk, "; unchanged: has profile weight");
            continue;
        }

        // Don't change the block weight if it's known to be rarely run.
        if (curBlk->isRunRarely())
        {
            reportBlockWeight(curBlk, "; unchanged: run rarely");
            continue;
        }

        // For curBlk to be part of a loop that starts at begBlk, curBlk must be reachable from begBlk and
        // (since this is a loop) begBlk must likewise be reachable from curBlk.

        if (fgReachable(curBlk, begBlk) && fgReachable(begBlk, curBlk))
        {
            // If `curBlk` reaches any of the back edge blocks we set `reachable`.
            // If `curBlk` dominates any of the back edge blocks we set `dominates`.
            bool reachable = false;
            bool dominates = false;

            for (FlowEdge* tmp = backedgeList; tmp != nullptr; tmp = tmp->getNextPredEdge())
            {
                BasicBlock* backedge = tmp->getSourceBlock();

                reachable |= fgReachable(curBlk, backedge);
                dominates |= fgDominate(curBlk, backedge);

                if (dominates && reachable)
                {
                    // No need to keep looking; we've already found all the info we need.
                    break;
                }
            }

            if (reachable)
            {
                // If the block has BB_ZERO_WEIGHT, then it should be marked as rarely run, and skipped, above.
                noway_assert(curBlk->bbWeight > BB_ZERO_WEIGHT);

                weight_t scale = BB_LOOP_WEIGHT_SCALE;

                if (!dominates)
                {
                    // If `curBlk` reaches but doesn't dominate any back edge to `endBlk` then there must be at least
                    // some other path to `endBlk`, so don't give `curBlk` all the execution weight.
                    scale = scale / 2;
                }

                curBlk->scaleBBWeight(scale);

                reportBlockWeight(curBlk, "");
            }
            else
            {
                reportBlockWeight(curBlk, "; unchanged: back edge unreachable");
            }
        }
        else
        {
            reportBlockWeight(curBlk, "; unchanged: block not in loop");
        }
    }
}

//------------------------------------------------------------------------
// optUnmarkLoopBlocks: Unmark the blocks between 'begBlk' and 'endBlk' as part of a loop.
//
// Arguments:
//      begBlk - first block of range. Must be marked as a loop head (BBF_LOOP_HEAD).
//      endBlk - last block of range (inclusive). Must be reachable from `begBlk`.
//
// Operation:
//      A set of blocks that were previously marked as a loop are now to be unmarked, since we have decided that
//      for some reason this loop no longer exists. Basically we are just resetting the blocks bbWeight to their
//      previous values.
//
void Compiler::optUnmarkLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk)
{
    noway_assert(begBlk->bbNum <= endBlk->bbNum);
    noway_assert(begBlk->isLoopHead());
    noway_assert(!opts.MinOpts());

    unsigned backEdgeCount = 0;

    for (BasicBlock* const predBlock : begBlk->PredBlocks())
    {
        // Is this a backward edge? (from predBlock to begBlk)
        if (begBlk->bbNum > predBlock->bbNum)
        {
            continue;
        }

        // We only consider back-edges that are BBJ_COND or BBJ_ALWAYS for loops.
        if (!predBlock->KindIs(BBJ_COND, BBJ_ALWAYS))
        {
            continue;
        }

        backEdgeCount++;
    }

    // Only unmark the loop blocks if we have exactly one loop back edge.
    if (backEdgeCount != 1)
    {
#ifdef DEBUG
        if (verbose)
        {
            if (backEdgeCount > 0)
            {
                printf("\nNot removing loop at " FMT_BB ", due to an additional back edge", begBlk->bbNum);
            }
            else if (backEdgeCount == 0)
            {
                printf("\nNot removing loop at " FMT_BB ", due to no back edge", begBlk->bbNum);
            }
        }
#endif
        return;
    }
    noway_assert(fgReachable(begBlk, endBlk));

#ifdef DEBUG
    if (verbose)
    {
        printf("\nUnmarking a loop from " FMT_BB " to " FMT_BB, begBlk->bbNum, endBlk->bbNum);
    }
#endif

    for (BasicBlock* const curBlk : BasicBlockRangeList(begBlk, endBlk))
    {
        // Stop if we go past the last block in the loop, as it may have been deleted.
        if (curBlk->bbNum > endBlk->bbNum)
        {
            break;
        }

        // Don't change the block weight if it's known to be rarely run.
        if (curBlk->isRunRarely())
        {
            continue;
        }

        // Don't change the block weight if it came from profile data.
        if (curBlk->hasProfileWeight())
        {
            continue;
        }

        // Don't unmark blocks that are maximum weight.
        if (curBlk->isMaxBBWeight())
        {
            continue;
        }

        // For curBlk to be part of a loop that starts at begBlk, curBlk must be reachable from begBlk and
        // (since this is a loop) begBlk must likewise be reachable from curBlk.
        //
        if (fgReachable(curBlk, begBlk) && fgReachable(begBlk, curBlk))
        {
            weight_t scale = 1.0 / BB_LOOP_WEIGHT_SCALE;

            if (!fgDominate(curBlk, endBlk))
            {
                scale *= 2;
            }

            curBlk->scaleBBWeight(scale);

            JITDUMP("\n    " FMT_BB "(wt=" FMT_WT ")", curBlk->bbNum, curBlk->getBBWeight(this));
        }
    }

    JITDUMP("\n");

    begBlk->unmarkLoopAlign(this DEBUG_ARG("Removed loop"));
}

/*****************************************************************************************************
 *
 *  Function called to update the loop table and bbWeight before removing a block
 */

void Compiler::optUpdateLoopsBeforeRemoveBlock(BasicBlock* block, bool skipUnmarkLoop)
{
    if (!optLoopTableValid)
    {
        return;
    }

    noway_assert(!opts.MinOpts());

    // If an unreachable block is a loop entry or bottom then the loop is unreachable.
    // Special case: the block was the head of a loop - or pointing to a loop entry.

    for (unsigned loopNum = 0; loopNum < optLoopCount; loopNum++)
    {
        LoopDsc& loop = optLoopTable[loopNum];

        // Some loops may have been already removed by loop unrolling or conditional folding.
        if (loop.lpIsRemoved())
        {
            continue;
        }

        // Avoid printing to the JitDump unless we're actually going to change something.
        // If we call reportBefore, then we're going to change the loop table, and we should print the
        // `reportAfter` info as well. Only print the `reportBefore` info once, if multiple changes to
        // the table are made.
        INDEBUG(bool reportedBefore = false);

        auto reportBefore = [&]() {
#ifdef DEBUG
            if (verbose && !reportedBefore)
            {
                printf("optUpdateLoopsBeforeRemoveBlock " FMT_BB " Before: ", block->bbNum);
                optPrintLoopInfo(loopNum);
                printf("\n");
                reportedBefore = true;
            }
#endif // DEBUG
        };

        auto reportAfter = [&]() {
#ifdef DEBUG
            if (verbose && reportedBefore)
            {
                printf("optUpdateLoopsBeforeRemoveBlock " FMT_BB "  After: ", block->bbNum);
                optPrintLoopInfo(loopNum);
                printf("\n");
            }
#endif // DEBUG
        };

        if ((block == loop.lpEntry) || (block == loop.lpBottom) || (block == loop.lpTop))
        {
            reportBefore();
            optMarkLoopRemoved(loopNum);
            reportAfter();
            continue;
        }

        // If the loop is still in the table any block in the loop must be reachable.

        noway_assert((loop.lpEntry != block) && (loop.lpBottom != block));

        if (loop.lpExit == block)
        {
            reportBefore();
            assert(loop.lpExitCnt == 1);
            --loop.lpExitCnt;
            loop.lpExit = nullptr;
        }

        // If `block` flows to the loop entry then the whole loop will become unreachable if it is the
        // only non-loop predecessor.

        bool removeLoop = false;
        if (!loop.lpContains(block))
        {
            for (BasicBlock* const succ : block->Succs())
            {
                if (loop.lpEntry == succ)
                {
                    removeLoop = true;
                    break;
                }
            }

            if (removeLoop)
            {
                // If the entry has any non-loop block that is not the known 'block' predecessor of entry
                // (found above), then don't remove the loop.
                for (BasicBlock* const predBlock : loop.lpEntry->PredBlocks())
                {
                    if (!loop.lpContains(predBlock) && (predBlock != block))
                    {
                        removeLoop = false;
                        break;
                    }
                }
            }
        }

        if (removeLoop)
        {
            reportBefore();
            optMarkLoopRemoved(loopNum);
        }
        else if (loop.lpHead == block)
        {
            reportBefore();
            /* The loop has a new head - Just update the loop table */
            loop.lpHead = block->bbPrev;
        }

        reportAfter();
    }

    if ((skipUnmarkLoop == false) &&                  // If we want to unmark this loop...
        block->KindIs(BBJ_ALWAYS, BBJ_COND) &&        // This block reaches conditionally or always
        block->bbJumpDest->isLoopHead() &&            // to a loop head...
        (fgCurBBEpochSize == fgBBNumMax + 1) &&       // We didn't add new blocks since last renumber...
        (block->bbJumpDest->bbNum <= block->bbNum) && // This is a backedge...
        fgDomsComputed &&                             // Given the doms are computed and valid...
        (fgCurBBEpochSize == fgDomBBcount + 1) &&     //
        fgReachable(block->bbJumpDest, block))        // Block's destination (target of back edge) can reach block...
    {
        optUnmarkLoopBlocks(block->bbJumpDest, block); // Unscale the blocks in such loop.
    }
}

//------------------------------------------------------------------------
// optClearLoopIterInfo: Clear the info related to LPFLG_ITER loops in the loop table.
// The various fields related to iterators is known to be valid for loop cloning and unrolling,
// but becomes invalid afterwards. Clear the info that might be used incorrectly afterwards
// in JitDump or by subsequent phases.
//
PhaseStatus Compiler::optClearLoopIterInfo()
{
    for (unsigned lnum = 0; lnum < optLoopCount; lnum++)
    {
        LoopDsc& loop = optLoopTable[lnum];
        loop.lpFlags &= ~(LPFLG_ITER | LPFLG_CONST_INIT | LPFLG_SIMD_LIMIT | LPFLG_VAR_LIMIT | LPFLG_CONST_LIMIT |
                          LPFLG_ARRLEN_LIMIT);

        loop.lpIterTree  = nullptr;
        loop.lpInitBlock = nullptr;
        loop.lpConstInit = -1;
        loop.lpTestTree  = nullptr;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Print loop info in an uniform way.
 */

void Compiler::optPrintLoopInfo(const LoopDsc* loop, bool printVerbose /* = false */)
{
    assert(optLoopTable != nullptr);
    assert((&optLoopTable[0] <= loop) && (loop < &optLoopTable[optLoopCount]));

    unsigned lnum = (unsigned)(loop - optLoopTable);
    assert(lnum < optLoopCount);
    assert(&optLoopTable[lnum] == loop);

    if (loop->lpIsRemoved())
    {
        // If a loop has been removed, it might be dangerous to print its fields (e.g., loop unrolling
        // nulls out the lpHead field).
        printf(FMT_LP " REMOVED", lnum);
        return;
    }

    printf(FMT_LP ", from " FMT_BB " to " FMT_BB " (Head=" FMT_BB ", Entry=" FMT_BB, lnum, loop->lpTop->bbNum,
           loop->lpBottom->bbNum, loop->lpHead->bbNum, loop->lpEntry->bbNum);

    if (loop->lpExitCnt == 1)
    {
        printf(", Exit=" FMT_BB, loop->lpExit->bbNum);
    }
    else
    {
        printf(", ExitCnt=%d", loop->lpExitCnt);
    }

    if (loop->lpParent != BasicBlock::NOT_IN_LOOP)
    {
        printf(", parent=" FMT_LP, loop->lpParent);
    }
    printf(")");

    if (printVerbose)
    {
        if (loop->lpChild != BasicBlock::NOT_IN_LOOP)
        {
            printf(", child loop = " FMT_LP, loop->lpChild);
        }
        if (loop->lpSibling != BasicBlock::NOT_IN_LOOP)
        {
            printf(", sibling loop = " FMT_LP, loop->lpSibling);
        }

        // If an iterator loop print the iterator and the initialization.
        if (loop->lpFlags & LPFLG_ITER)
        {
            printf(" [over V%02u", loop->lpIterVar());
            printf(" (");
            printf(GenTree::OpName(loop->lpIterOper()));
            printf(" %d)", loop->lpIterConst());

            if (loop->lpFlags & LPFLG_CONST_INIT)
            {
                printf(" from %d", loop->lpConstInit);
            }

            if (loop->lpFlags & LPFLG_CONST_INIT)
            {
                if (loop->lpInitBlock != loop->lpHead)
                {
                    printf(" (in " FMT_BB ")", loop->lpInitBlock->bbNum);
                }
            }

            // If a simple test condition print operator and the limits */
            printf(" %s", GenTree::OpName(loop->lpTestOper()));

            if (loop->lpFlags & LPFLG_CONST_LIMIT)
            {
                printf(" %d", loop->lpConstLimit());
                if (loop->lpFlags & LPFLG_SIMD_LIMIT)
                {
                    printf(" (simd)");
                }
            }
            if (loop->lpFlags & LPFLG_VAR_LIMIT)
            {
                printf(" V%02u", loop->lpVarLimit());
            }
            if (loop->lpFlags & LPFLG_ARRLEN_LIMIT)
            {
                ArrIndex* index = new (getAllocator(CMK_DebugOnly)) ArrIndex(getAllocator(CMK_DebugOnly));
                if (loop->lpArrLenLimit(this, index))
                {
                    printf(" ");
                    index->Print();
                    printf(".Length");
                }
                else
                {
                    printf(" ???.Length");
                }
            }

            printf("]");
        }

        // Print the flags

        if (loop->lpFlags & LPFLG_CONTAINS_CALL)
        {
            printf(" call");
        }
        if (loop->lpFlags & LPFLG_HAS_PREHEAD)
        {
            printf(" prehead");
        }
        if (loop->lpFlags & LPFLG_DONT_UNROLL)
        {
            printf(" !unroll");
        }
        if (loop->lpFlags & LPFLG_ASGVARS_YES)
        {
            printf(" avyes");
        }
        if (loop->lpFlags & LPFLG_ASGVARS_INC)
        {
            printf(" avinc");
        }
    }
}

void Compiler::optPrintLoopInfo(unsigned lnum, bool printVerbose /* = false */)
{
    assert(lnum < optLoopCount);

    const LoopDsc& loop = optLoopTable[lnum];
    optPrintLoopInfo(&loop, printVerbose);
}

//------------------------------------------------------------------------
// optPrintLoopTable: Print the loop table
//
void Compiler::optPrintLoopTable()
{
    printf("\n***************  Natural loop table\n");

    if (optLoopCount == 0)
    {
        printf("No loops\n");
    }
    else
    {
        for (unsigned loopInd = 0; loopInd < optLoopCount; loopInd++)
        {
            optPrintLoopInfo(loopInd, /* verbose */ true);
            printf("\n");
        }
    }

    printf("\n");
}

#endif // DEBUG

//------------------------------------------------------------------------
// optPopulateInitInfo: Populate loop init info in the loop table.
// We assume the iteration variable is initialized already and check appropriately.
// This only checks for the special case of a constant initialization.
//
// Arguments:
//     loopInd   -  loop index
//     initBlock -  block in which the initialization lives.
//     init      -  the tree that is supposed to initialize the loop iterator. Might be nullptr.
//     iterVar   -  loop iteration variable.
//
// Return Value:
//     "true" if a constant initializer was found.
//
// Operation:
//     The 'init' tree is checked if its lhs is a local and rhs is a const.
//
bool Compiler::optPopulateInitInfo(unsigned loopInd, BasicBlock* initBlock, GenTree* init, unsigned iterVar)
{
    if (init == nullptr)
    {
        return false;
    }

    // Operator should be =
    if (init->gtOper != GT_ASG)
    {
        return false;
    }

    GenTree* lhs = init->AsOp()->gtOp1;
    GenTree* rhs = init->AsOp()->gtOp2;
    // LHS has to be local and should equal iterVar.
    if ((lhs->gtOper != GT_LCL_VAR) || (lhs->AsLclVarCommon()->GetLclNum() != iterVar))
    {
        return false;
    }

    // RHS can be constant or local var.
    // TODO-CQ: CLONE: Add arr length for descending loops.
    if ((rhs->gtOper != GT_CNS_INT) || (rhs->TypeGet() != TYP_INT))
    {
        return false;
    }

    // We found an initializer in the `initBlock` block. For this to be used, we need to make sure the
    // "iterVar" initialization is never skipped. That is, every pred of ENTRY other than HEAD is in the loop.
    // We allow one special case: the HEAD block is an empty predecessor to ENTRY, and the initBlock is the
    // only predecessor to HEAD. This handles the case where we rebuild the loop table (after inserting
    // pre-headers) and we still want to find the initializer before the pre-header block.
    for (BasicBlock* const predBlock : optLoopTable[loopInd].lpEntry->PredBlocks())
    {
        if (!optLoopTable[loopInd].lpContains(predBlock))
        {
            bool initBlockOk = (predBlock == initBlock);
            if (!initBlockOk)
            {
                if ((predBlock->bbJumpKind == BBJ_NONE) && (predBlock->bbNext == optLoopTable[loopInd].lpEntry) &&
                    (predBlock->countOfInEdges() == 1) && (predBlock->firstStmt() == nullptr) &&
                    (predBlock->bbPrev != nullptr) && predBlock->bbPrev->bbFallsThrough())
                {
                    initBlockOk = true;
                }
            }
            if (!initBlockOk)
            {
                JITDUMP(FMT_LP ": initialization not guaranteed from " FMT_BB " through to entry block " FMT_BB
                               " from pred " FMT_BB "; ignore constant initializer\n",
                        loopInd, initBlock->bbNum, optLoopTable[loopInd].lpEntry->bbNum, predBlock->bbNum);
                return false;
            }
        }
    }

    optLoopTable[loopInd].lpFlags |= LPFLG_CONST_INIT;
    optLoopTable[loopInd].lpConstInit = (int)rhs->AsIntCon()->gtIconVal;
    optLoopTable[loopInd].lpInitBlock = initBlock;

    return true;
}

//----------------------------------------------------------------------------------
// optCheckIterInLoopTest: Check if iter var is used in loop test.
//
// Arguments:
//      loopInd       loopIndex
//      test          "jtrue" tree or an asg of the loop iter termination condition
//      iterVar       loop iteration variable.
//
//  Operation:
//      The test tree is parsed to check if "iterVar" matches the lhs of the condition
//      and the rhs limit is extracted from the "test" tree. The limit information is
//      added to the loop table.
//
//  Return Value:
//      "false" if the loop table could not be populated with the loop test info or
//      if the test condition doesn't involve iterVar.
//
bool Compiler::optCheckIterInLoopTest(unsigned loopInd, GenTree* test, unsigned iterVar)
{
    // Obtain the relop from the "test" tree.
    GenTree* relop;
    if (test->gtOper == GT_JTRUE)
    {
        relop = test->gtGetOp1();
    }
    else
    {
        assert(test->gtOper == GT_ASG);
        relop = test->gtGetOp2();
    }

    noway_assert(relop->OperIsCompare());

    GenTree* opr1 = relop->AsOp()->gtOp1;
    GenTree* opr2 = relop->AsOp()->gtOp2;

    GenTree* iterOp;
    GenTree* limitOp;

    // Make sure op1 or op2 is the iterVar.
    if (opr1->gtOper == GT_LCL_VAR && opr1->AsLclVarCommon()->GetLclNum() == iterVar)
    {
        iterOp  = opr1;
        limitOp = opr2;
    }
    else if (opr2->gtOper == GT_LCL_VAR && opr2->AsLclVarCommon()->GetLclNum() == iterVar)
    {
        iterOp  = opr2;
        limitOp = opr1;
    }
    else
    {
        return false;
    }

    if (iterOp->gtType != TYP_INT)
    {
        return false;
    }

    // Mark the iterator node.
    iterOp->gtFlags |= GTF_VAR_ITERATOR;

    // Check what type of limit we have - constant, variable or arr-len.
    if (limitOp->gtOper == GT_CNS_INT)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_CONST_LIMIT;
        if ((limitOp->gtFlags & GTF_ICON_SIMD_COUNT) != 0)
        {
            optLoopTable[loopInd].lpFlags |= LPFLG_SIMD_LIMIT;
        }
    }
    else if (limitOp->gtOper == GT_LCL_VAR)
    {
        // See if limit var is a loop invariant
        //
        if (!optIsVarAssgLoop(loopInd, limitOp->AsLclVarCommon()->GetLclNum()))
        {
            optLoopTable[loopInd].lpFlags |= LPFLG_VAR_LIMIT;
        }
        else
        {
            JITDUMP("Limit var V%02u modifiable in " FMT_LP "\n", limitOp->AsLclVarCommon()->GetLclNum(), loopInd);
        }
    }
    else if (limitOp->gtOper == GT_ARR_LENGTH)
    {
        // See if limit array is a loop invariant
        //
        GenTree* const array = limitOp->AsArrLen()->ArrRef();

        if (array->OperIs(GT_LCL_VAR))
        {
            if (!optIsVarAssgLoop(loopInd, array->AsLclVarCommon()->GetLclNum()))
            {
                optLoopTable[loopInd].lpFlags |= LPFLG_ARRLEN_LIMIT;
            }
            else
            {
                JITDUMP("Array limit var V%02u modifiable in " FMT_LP "\n", array->AsLclVarCommon()->GetLclNum(),
                        loopInd);
            }
        }
        else
        {
            JITDUMP("Array limit tree [%06u] not analyzable in " FMT_LP "\n", dspTreeID(limitOp), loopInd);
        }
    }
    else
    {
        JITDUMP("Loop limit tree [%06u] not analyzable in " FMT_LP "\n", dspTreeID(limitOp), loopInd);
    }

    // Were we able to successfully analyze the limit?
    //
    const bool analyzedLimit =
        (optLoopTable[loopInd].lpFlags & (LPFLG_CONST_LIMIT | LPFLG_VAR_LIMIT | LPFLG_ARRLEN_LIMIT)) != 0;

    // Save the type of the comparison between the iterator and the limit.
    //
    optLoopTable[loopInd].lpTestTree = relop;

    return analyzedLimit;
}

//----------------------------------------------------------------------------------
// optIsLoopIncrTree: Check if loop is a tree of form v += 1 or v = v + 1
//
// Arguments:
//      incr        The incr tree to be checked. Whether incr tree is
//                  oper-equal(+=, -=...) type nodes or v=v+1 type ASG nodes.
//
//  Operation:
//      The test tree is parsed to check if "iterVar" matches the lhs of the condition
//      and the rhs limit is extracted from the "test" tree. The limit information is
//      added to the loop table.
//
//  Return Value:
//      iterVar local num if the iterVar is found, otherwise BAD_VAR_NUM.
//
unsigned Compiler::optIsLoopIncrTree(GenTree* incr)
{
    GenTree*   incrVal;
    genTreeOps updateOper;
    unsigned   iterVar = incr->IsLclVarUpdateTree(&incrVal, &updateOper);
    if (iterVar != BAD_VAR_NUM)
    {
        // We have v = v op y type asg node.
        switch (updateOper)
        {
            case GT_ADD:
            case GT_SUB:
            case GT_MUL:
            case GT_RSH:
            case GT_LSH:
                break;
            default:
                return BAD_VAR_NUM;
        }

        // Increment should be by a const int.
        // TODO-CQ: CLONE: allow variable increments.
        if ((incrVal->gtOper != GT_CNS_INT) || (incrVal->TypeGet() != TYP_INT))
        {
            return BAD_VAR_NUM;
        }
    }

    return iterVar;
}

//----------------------------------------------------------------------------------
// optComputeIterInfo: Check tree is loop increment of a lcl that is loop-invariant.
//
// Arguments:
//      incr        - tree that increments the loop iterator. v+=1 or v=v+1.
//      from, to    - range of blocks that comprise the loop body
//      pIterVar    - see return value.
//
//  Return Value:
//      Returns true if iterVar "v" can be returned in "pIterVar", otherwise returns
//      false.
//
//  Operation:
//      Check if the "incr" tree is a "v=v+1 or v+=1" type tree and make sure it is not
//      otherwise modified in the loop.
//
bool Compiler::optComputeIterInfo(GenTree* incr, BasicBlock* from, BasicBlock* to, unsigned* pIterVar)
{
    const unsigned iterVar = optIsLoopIncrTree(incr);

    if (iterVar == BAD_VAR_NUM)
    {
        return false;
    }

    // Note we can't use optIsVarAssgLoop here, as iterVar is indeed
    // assigned within the loop.
    //
    // Bail on promoted case, otherwise we'd have to search the loop
    // for both iterVar and its parent.
    //
    // Bail on the potentially aliased case.
    //
    LclVarDsc* const iterVarDsc = lvaGetDesc(iterVar);

    if (iterVarDsc->lvIsStructField)
    {
        JITDUMP("iterVar V%02u is a promoted field\n", iterVar);
        return false;
    }

    if (iterVarDsc->IsAddressExposed())
    {
        JITDUMP("iterVar V%02u is address exposed\n", iterVar);
        return false;
    }

    if (optIsVarAssigned(from, to, incr, iterVar))
    {
        JITDUMP("iterVar V%02u is assigned in loop\n", iterVar);
        return false;
    }

    JITDUMP("iterVar V%02u is invariant in loop (with the exception of the update in [%06u])\n", iterVar,
            dspTreeID(incr));

    *pIterVar = iterVar;
    return true;
}

//----------------------------------------------------------------------------------
// optIsLoopTestEvalIntoTemp:
//      Pattern match if the test tree is computed into a tmp
//      and the "tmp" is used as jump condition for loop termination.
//
// Arguments:
//      testStmt    - is the JTRUE statement that is of the form: jmpTrue (Vtmp != 0)
//                    where Vtmp contains the actual loop test result.
//      newTestStmt - contains the statement that is the actual test stmt involving
//                    the loop iterator.
//
//  Return Value:
//      Returns true if a new test tree can be obtained.
//
//  Operation:
//      Scan if the current stmt is a jtrue with (Vtmp != 0) as condition
//      Then returns the rhs for def of Vtmp as the "test" node.
//
//  Note:
//      This method just retrieves what it thinks is the "test" node,
//      the callers are expected to verify that "iterVar" is used in the test.
//
bool Compiler::optIsLoopTestEvalIntoTemp(Statement* testStmt, Statement** newTestStmt)
{
    GenTree* test = testStmt->GetRootNode();

    if (test->gtOper != GT_JTRUE)
    {
        return false;
    }

    GenTree* relop = test->gtGetOp1();
    noway_assert(relop->OperIsCompare());

    GenTree* opr1 = relop->AsOp()->gtOp1;
    GenTree* opr2 = relop->AsOp()->gtOp2;

    // Make sure we have jtrue (vtmp != 0)
    if ((relop->OperGet() == GT_NE) && (opr1->OperGet() == GT_LCL_VAR) && (opr2->OperGet() == GT_CNS_INT) &&
        opr2->IsIntegralConst(0))
    {
        // Get the previous statement to get the def (rhs) of Vtmp to see
        // if the "test" is evaluated into Vtmp.
        Statement* prevStmt = testStmt->GetPrevStmt();
        if (prevStmt == nullptr)
        {
            return false;
        }

        GenTree* tree = prevStmt->GetRootNode();
        if (tree->OperGet() == GT_ASG)
        {
            GenTree* lhs = tree->AsOp()->gtOp1;
            GenTree* rhs = tree->AsOp()->gtOp2;

            // Return as the new test node.
            if (lhs->gtOper == GT_LCL_VAR && lhs->AsLclVarCommon()->GetLclNum() == opr1->AsLclVarCommon()->GetLclNum())
            {
                if (rhs->OperIsCompare())
                {
                    *newTestStmt = prevStmt;
                    return true;
                }
            }
        }
    }
    return false;
}

//----------------------------------------------------------------------------------
// optExtractInitTestIncr:
//      Extract the "init", "test" and "incr" nodes of the loop.
//
// Arguments:
//      pInitBlock - [IN/OUT] *pInitBlock is the loop head block on entry, and is set to the initBlock on exit,
//                   if `**ppInit` is non-null.
//      bottom     - Loop bottom block
//      top        - Loop top block
//      ppInit     - The init stmt of the loop if found.
//      ppTest     - The test stmt of the loop if found.
//      ppIncr     - The incr stmt of the loop if found.
//
//  Return Value:
//      The results are put in "ppInit", "ppTest" and "ppIncr" if the method
//      returns true. Returns false if the information can't be extracted.
//      Extracting the `init` is optional; if one is not found, *ppInit is set
//      to nullptr. Return value will never be false if `init` is not found.
//
//  Operation:
//      Check if the "test" stmt is last stmt in the loop "bottom". Try to find the "incr" stmt.
//      Check previous stmt of "test" to get the "incr" stmt. If it is not found it could be a loop of the
//      below form.
//
//                     +-------<-----------------<-----------+
//                     |                                     |
//                     v                                     |
//      BBinit(head) -> BBcond(top) -> BBLoopBody(bottom) ---^
//
//      Check if the "incr" tree is present in the loop "top" node as the last stmt.
//      Also check if the "test" tree is assigned to a tmp node and the tmp is used
//      in the jtrue condition.
//
//  Note:
//      This method just retrieves what it thinks is the "test" node,
//      the callers are expected to verify that "iterVar" is used in the test.
//
bool Compiler::optExtractInitTestIncr(
    BasicBlock** pInitBlock, BasicBlock* bottom, BasicBlock* top, GenTree** ppInit, GenTree** ppTest, GenTree** ppIncr)
{
    assert(pInitBlock != nullptr);
    assert(ppInit != nullptr);
    assert(ppTest != nullptr);
    assert(ppIncr != nullptr);

    // Check if last two statements in the loop body are the increment of the iterator
    // and the loop termination test.
    noway_assert(bottom->bbStmtList != nullptr);
    Statement* testStmt = bottom->lastStmt();
    noway_assert(testStmt != nullptr && testStmt->GetNextStmt() == nullptr);

    Statement* newTestStmt;
    if (optIsLoopTestEvalIntoTemp(testStmt, &newTestStmt))
    {
        testStmt = newTestStmt;
    }

    // Check if we have the incr stmt before the test stmt, if we don't,
    // check if incr is part of the loop "top".
    Statement* incrStmt = testStmt->GetPrevStmt();

    // If we've added profile instrumentation, we may need to skip past a BB counter update.
    //
    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR) && (incrStmt != nullptr) &&
        incrStmt->GetRootNode()->IsBlockProfileUpdate())
    {
        incrStmt = incrStmt->GetPrevStmt();
    }

    if (incrStmt == nullptr || optIsLoopIncrTree(incrStmt->GetRootNode()) == BAD_VAR_NUM)
    {
        if (top == nullptr || top->bbStmtList == nullptr || top->bbStmtList->GetPrevStmt() == nullptr)
        {
            return false;
        }

        // If the prev stmt to loop test is not incr, then check if we have loop test evaluated into a tmp.
        Statement* toplastStmt = top->lastStmt();
        if (optIsLoopIncrTree(toplastStmt->GetRootNode()) != BAD_VAR_NUM)
        {
            incrStmt = toplastStmt;
        }
        else
        {
            return false;
        }
    }

    assert(testStmt != incrStmt);

    // Find the last statement in the loop pre-header which we expect to be the initialization of
    // the loop iterator.
    BasicBlock* initBlock = *pInitBlock;
    Statement*  phdrStmt  = initBlock->firstStmt();
    if (phdrStmt == nullptr)
    {
        // When we build the loop table, we canonicalize by introducing loop pre-headers for all loops.
        // If we are rebuilding the loop table, we would already have the pre-header block introduced
        // the first time, which might be empty if no hoisting has yet occurred. In this case, look a
        // little harder for the possible loop initialization statement.
        if ((initBlock->bbJumpKind == BBJ_NONE) && (initBlock->bbNext == top) && (initBlock->countOfInEdges() == 1) &&
            (initBlock->bbPrev != nullptr) && initBlock->bbPrev->bbFallsThrough())
        {
            initBlock = initBlock->bbPrev;
            phdrStmt  = initBlock->firstStmt();
        }
    }

    if (phdrStmt != nullptr)
    {
        Statement* initStmt = phdrStmt->GetPrevStmt();
        noway_assert(initStmt != nullptr && (initStmt->GetNextStmt() == nullptr));

        // If it is a duplicated loop condition, skip it.
        if (initStmt->GetRootNode()->OperIs(GT_JTRUE))
        {
            bool doGetPrev = true;
#ifdef DEBUG
            if (opts.optRepeat)
            {
                // Previous optimization passes may have inserted compiler-generated
                // statements other than duplicated loop conditions.
                doGetPrev = (initStmt->GetPrevStmt() != nullptr);
            }
#endif // DEBUG
            if (doGetPrev)
            {
                initStmt = initStmt->GetPrevStmt();
            }
            noway_assert(initStmt != nullptr);
        }

        *ppInit     = initStmt->GetRootNode();
        *pInitBlock = initBlock;
    }
    else
    {
        *ppInit = nullptr;
    }

    *ppTest = testStmt->GetRootNode();
    *ppIncr = incrStmt->GetRootNode();

    return true;
}

/*****************************************************************************
 *
 *  Record the loop in the loop table.  Return true if successful, false if
 *  out of entries in loop table.
 */

bool Compiler::optRecordLoop(
    BasicBlock* head, BasicBlock* top, BasicBlock* entry, BasicBlock* bottom, BasicBlock* exit, unsigned char exitCnt)
{
    if (exitCnt == 1)
    {
        noway_assert(exit != nullptr);
    }

    // Record this loop in the table, if there's room.

    assert(optLoopCount <= BasicBlock::MAX_LOOP_NUM);
    if (optLoopCount == BasicBlock::MAX_LOOP_NUM)
    {
#if COUNT_LOOPS
        loopOverflowThisMethod = true;
#endif
        return false;
    }

    // Assumed preconditions on the loop we're adding.
    assert(top->bbNum <= entry->bbNum);
    assert(entry->bbNum <= bottom->bbNum);
    assert(head->bbNum < top->bbNum || head->bbNum > bottom->bbNum);

    unsigned char loopInd = optLoopCount;

    if (optLoopTable == nullptr)
    {
        assert(loopInd == 0);
        optLoopTable = getAllocator(CMK_LoopOpt).allocate<LoopDsc>(BasicBlock::MAX_LOOP_NUM);

        NewLoopEpoch();
    }
    else
    {
        // If the new loop contains any existing ones, add it in the right place.
        for (unsigned char prevPlus1 = optLoopCount; prevPlus1 > 0; prevPlus1--)
        {
            unsigned char prev = prevPlus1 - 1;
            if (optLoopTable[prev].lpContainedBy(top, bottom))
            {
                loopInd = prev;
            }
        }
        // Move up any loops if necessary.
        for (unsigned j = optLoopCount; j > loopInd; j--)
        {
            optLoopTable[j] = optLoopTable[j - 1];
        }
    }

#ifdef DEBUG
    for (unsigned i = loopInd + 1; i < optLoopCount; i++)
    {
        // The loop is well-formed.
        assert(optLoopTable[i].lpWellFormed());
        // Check for disjoint.
        if (optLoopTable[i].lpDisjoint(top, bottom))
        {
            continue;
        }
        // Otherwise, assert complete containment (of optLoopTable[i] in new loop).
        assert(optLoopTable[i].lpContainedBy(top, bottom));
    }
#endif // DEBUG

    bool loopInsertedAtEnd = (loopInd == optLoopCount);
    optLoopCount++;

    optLoopTable[loopInd].lpHead    = head;
    optLoopTable[loopInd].lpTop     = top;
    optLoopTable[loopInd].lpBottom  = bottom;
    optLoopTable[loopInd].lpEntry   = entry;
    optLoopTable[loopInd].lpExit    = exit;
    optLoopTable[loopInd].lpExitCnt = exitCnt;

    optLoopTable[loopInd].lpParent  = BasicBlock::NOT_IN_LOOP;
    optLoopTable[loopInd].lpChild   = BasicBlock::NOT_IN_LOOP;
    optLoopTable[loopInd].lpSibling = BasicBlock::NOT_IN_LOOP;

    optLoopTable[loopInd].lpAsgVars = AllVarSetOps::UninitVal();

    optLoopTable[loopInd].lpFlags = LPFLG_EMPTY;

    // We haven't yet recorded any side effects.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        optLoopTable[loopInd].lpLoopHasMemoryHavoc[memoryKind] = false;
    }
    optLoopTable[loopInd].lpFieldsModified         = nullptr;
    optLoopTable[loopInd].lpArrayElemTypesModified = nullptr;

    //
    // Try to find loops that have an iterator (i.e. for-like loops) "for (init; test; incr){ ... }"
    // We have the following restrictions:
    //     1. The loop condition must be a simple one i.e. only one JTRUE node
    //     2. There must be a loop iterator (a local var) that is
    //        incremented (decremented or lsh, rsh, mul) with a constant value
    //     3. The iterator is incremented exactly once
    //     4. The loop condition must use the iterator.
    //     5. Finding a constant initializer is optional; if the initializer is not found, or is not constant,
    //        it is still considered a for-like loop.
    //
    if (bottom->bbJumpKind == BBJ_COND)
    {
        GenTree*    init;
        GenTree*    test;
        GenTree*    incr;
        BasicBlock* initBlock = head;
        if (!optExtractInitTestIncr(&initBlock, bottom, top, &init, &test, &incr))
        {
            JITDUMP(FMT_LP ": couldn't find init/test/incr; not LPFLG_ITER loop\n", loopInd);
            goto DONE_LOOP;
        }

        unsigned iterVar = BAD_VAR_NUM;
        if (!optComputeIterInfo(incr, top, bottom, &iterVar))
        {
            JITDUMP(FMT_LP ": increment expression not appropriate form, or not loop invariant; not LPFLG_ITER loop\n",
                    loopInd);
            goto DONE_LOOP;
        }

        optPopulateInitInfo(loopInd, initBlock, init, iterVar);

        // Check that the iterator is used in the loop condition.
        if (!optCheckIterInLoopTest(loopInd, test, iterVar))
        {
            JITDUMP(FMT_LP ": iterator V%02u fails analysis of loop condition [%06u]; not LPFLG_ITER loop\n", loopInd,
                    iterVar, dspTreeID(test));
            goto DONE_LOOP;
        }

        // We know the loop has an iterator at this point; flag it as LPFLG_ITER.
        JITDUMP(FMT_LP ": setting LPFLG_ITER\n", loopInd);
        optLoopTable[loopInd].lpFlags |= LPFLG_ITER;

        // Record iterator.
        optLoopTable[loopInd].lpIterTree = incr;

#if COUNT_LOOPS
        iterLoopCount++;

        // Check if a constant iteration loop.
        if ((optLoopTable[loopInd].lpFlags & LPFLG_CONST_INIT) && (optLoopTable[loopInd].lpFlags & LPFLG_CONST_LIMIT))
        {
            // This is a constant loop.
            constIterLoopCount++;
        }
#endif
    }

DONE_LOOP:

#ifdef DEBUG
    if (verbose)
    {
        printf("Recorded loop %s", loopInsertedAtEnd ? "" : "(extended) ");
        optPrintLoopInfo(loopInd, /* verbose */ true);
        printf("\n");
    }
#endif // DEBUG

    return true;
}

#ifdef DEBUG
void Compiler::optCheckPreds()
{
    for (BasicBlock* const block : Blocks())
    {
        for (BasicBlock* const predBlock : block->PredBlocks())
        {
            // make sure this pred is part of the BB list
            BasicBlock* bb;
            for (bb = fgFirstBB; bb; bb = bb->bbNext)
            {
                if (bb == predBlock)
                {
                    break;
                }
            }
            noway_assert(bb);
            switch (bb->bbJumpKind)
            {
                case BBJ_COND:
                    if (bb->bbJumpDest == block)
                    {
                        break;
                    }
                    FALLTHROUGH;
                case BBJ_NONE:
                    noway_assert(bb->bbNext == block);
                    break;
                case BBJ_EHFILTERRET:
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                    noway_assert(bb->bbJumpDest == block);
                    break;
                default:
                    break;
            }
        }
    }
}

#endif // DEBUG

namespace
{
//------------------------------------------------------------------------
// LoopSearch: Class that handles scanning a range of blocks to detect a loop,
//             moving blocks to make the loop body contiguous, and recording the loop.
//
// We will use the following terminology:
//   HEAD    - the basic block that flows into the loop ENTRY block (Currently MUST be lexically before entry).
//             Not part of the looping of the loop.
//   TOP     - the target of the backward edge from BOTTOM, and the lexically first basic block (in bbNext order)
//             within this loop.
//   BOTTOM  - the lexically last block in the loop (i.e. the block from which we jump to the top)
//   EXIT    - the predecessor of loop's unique exit edge, if it has a unique exit edge; else nullptr
//   ENTRY   - the entry in the loop (not necessarily the TOP), but there must be only one entry
//
//   We (currently) require the body of a loop to be a contiguous (in bbNext order) sequence of basic blocks.
//   When the loop is identified, blocks will be moved out to make it a compact contiguous region if possible,
//   and in cases where compaction is not possible, we'll subsequently treat all blocks in the lexical range
//   between TOP and BOTTOM as part of the loop even if they aren't part of the SCC.
//   Regarding nesting:  Since a given block can only have one back-edge (we only detect loops with back-edges
//   from BBJ_COND or BBJ_ALWAYS blocks), no two loops will share the same BOTTOM.  Two loops may share the
//   same TOP/ENTRY as reported by LoopSearch, and optCanonicalizeLoopNest will subsequently re-write
//   the CFG so that no two loops share the same TOP/ENTRY anymore.
//
//        |
//        v
//      head
//        |
//        |      top   <--+
//        |       |       |
//        |      ...      |
//        |       |       |
//        |       v       |
//        +---> entry     |
//                |       |
//               ...      |
//                |       |
//                v       |
//         +-- exit/tail  |
//         |      |       |
//         |     ...      |
//         |      |       |
//         |      v       |
//         |    bottom ---+
//         |
//         +------+
//                |
//                v
//
class LoopSearch
{

    // Keeping track of which blocks are in the loop requires two block sets since we may add blocks
    // as we go but the BlockSet type's max ID doesn't increase to accommodate them.  Define a helper
    // struct to make the ensuing code more readable.
    struct LoopBlockSet
    {
    private:
        // Keep track of blocks with bbNum <= oldBlockMaxNum in a regular BlockSet, since
        // it can hold all of them.
        BlockSet oldBlocksInLoop; // Blocks with bbNum <= oldBlockMaxNum

        // Keep track of blocks with bbNum > oldBlockMaxNum in a separate BlockSet, but
        // indexing them by (blockNum - oldBlockMaxNum); since we won't generate more than
        // one new block per old block, this must be sufficient to track any new blocks.
        BlockSet newBlocksInLoop; // Blocks with bbNum > oldBlockMaxNum

        Compiler*    comp;
        unsigned int oldBlockMaxNum;

    public:
        LoopBlockSet(Compiler* comp)
            : oldBlocksInLoop(BlockSetOps::UninitVal())
            , newBlocksInLoop(BlockSetOps::UninitVal())
            , comp(comp)
            , oldBlockMaxNum(comp->fgBBNumMax)
        {
        }

        void Reset(unsigned int seedBlockNum)
        {
            if (BlockSetOps::MayBeUninit(oldBlocksInLoop))
            {
                // Either the block sets are uninitialized (and long), so we need to initialize
                // them (and allocate their backing storage), or they are short and empty, so
                // assigning MakeEmpty to them is as cheap as ClearD.
                oldBlocksInLoop = BlockSetOps::MakeEmpty(comp);
                newBlocksInLoop = BlockSetOps::MakeEmpty(comp);
            }
            else
            {
                // We know the backing storage is already allocated, so just clear it.
                BlockSetOps::ClearD(comp, oldBlocksInLoop);
                BlockSetOps::ClearD(comp, newBlocksInLoop);
            }
            assert(seedBlockNum <= oldBlockMaxNum);
            BlockSetOps::AddElemD(comp, oldBlocksInLoop, seedBlockNum);
        }

        bool CanRepresent(unsigned int blockNum)
        {
            // We can represent old blocks up to oldBlockMaxNum, and
            // new blocks up to 2 * oldBlockMaxNum.
            return (blockNum <= 2 * oldBlockMaxNum);
        }

        bool IsMember(unsigned int blockNum)
        {
            if (blockNum > oldBlockMaxNum)
            {
                return BlockSetOps::IsMember(comp, newBlocksInLoop, blockNum - oldBlockMaxNum);
            }
            else
            {
                return BlockSetOps::IsMember(comp, oldBlocksInLoop, blockNum);
            }
        }

        void Insert(unsigned int blockNum)
        {
            if (blockNum > oldBlockMaxNum)
            {
                BlockSetOps::AddElemD(comp, newBlocksInLoop, blockNum - oldBlockMaxNum);
            }
            else
            {
                BlockSetOps::AddElemD(comp, oldBlocksInLoop, blockNum);
            }
        }

        bool TestAndInsert(unsigned int blockNum)
        {
            if (blockNum > oldBlockMaxNum)
            {
                unsigned int shiftedNum = blockNum - oldBlockMaxNum;
                if (!BlockSetOps::IsMember(comp, newBlocksInLoop, shiftedNum))
                {
                    BlockSetOps::AddElemD(comp, newBlocksInLoop, shiftedNum);
                    return false;
                }
            }
            else
            {
                if (!BlockSetOps::IsMember(comp, oldBlocksInLoop, blockNum))
                {
                    BlockSetOps::AddElemD(comp, oldBlocksInLoop, blockNum);
                    return false;
                }
            }
            return true;
        }
    };

    LoopBlockSet loopBlocks; // Set of blocks identified as part of the loop
    Compiler*    comp;

    // See LoopSearch class comment header for a diagram relating these fields:
    BasicBlock* head;   // Predecessor of unique entry edge
    BasicBlock* top;    // Successor of back-edge from BOTTOM
    BasicBlock* bottom; // Predecessor of back-edge to TOP, also lexically last in-loop block
    BasicBlock* entry;  // Successor of unique entry edge

    BasicBlock*   lastExit;       // Most recently discovered exit block
    unsigned char exitCount;      // Number of discovered exit edges
    unsigned int  oldBlockMaxNum; // Used to identify new blocks created during compaction
    BlockSet      bottomBlocks;   // BOTTOM blocks of already-recorded loops
#ifdef DEBUG
    bool forgotExit = false; // Flags a rare case where lastExit gets nulled out, for assertions
#endif
    bool changedFlowGraph = false; // Signals that loop compaction has modified the flow graph

public:
    LoopSearch(Compiler* comp)
        : loopBlocks(comp), comp(comp), oldBlockMaxNum(comp->fgBBNumMax), bottomBlocks(BlockSetOps::MakeEmpty(comp))
    {
        // Make sure we've renumbered such that the bitsets can hold all the bits
        assert(comp->fgBBNumMax <= comp->fgCurBBEpochSize);
    }

    //------------------------------------------------------------------------
    // RecordLoop: Notify the Compiler that a loop has been found.
    //
    // Return Value:
    //    true  - Loop successfully recorded.
    //    false - Compiler has run out of loop descriptors; loop not recorded.
    //
    bool RecordLoop()
    {
        // At this point we have a compact loop - record it in the loop table.
        // If we found only one exit, record it in the table too
        // (otherwise an exit = nullptr in the loop table means multiple exits).

        BasicBlock* onlyExit = (exitCount == 1 ? lastExit : nullptr);
        if (comp->optRecordLoop(head, top, entry, bottom, onlyExit, exitCount))
        {
            // Record the BOTTOM block for future reference before returning.
            assert(bottom->bbNum <= oldBlockMaxNum);
            BlockSetOps::AddElemD(comp, bottomBlocks, bottom->bbNum);
            return true;
        }

        // Unable to record this loop because the loop descriptor table overflowed.
        return false;
    }

    //------------------------------------------------------------------------
    // ChangedFlowGraph: Determine whether loop compaction has modified the flow graph.
    //
    // Return Value:
    //    true  - The flow graph has been modified; fgUpdateChangedFlowGraph should
    //            be called (which is the caller's responsibility).
    //    false - The flow graph has not been modified by this LoopSearch.
    //
    bool ChangedFlowGraph()
    {
        return changedFlowGraph;
    }

    //------------------------------------------------------------------------
    // FindLoop: Search for a loop with the given HEAD block and back-edge.
    //
    // Arguments:
    //    head - Block to be the HEAD of any loop identified
    //    top - Block to be the TOP of any loop identified
    //    bottom - Block to be the BOTTOM of any loop identified
    //
    // Return Value:
    //    true  - Found a valid loop.
    //    false - Did not find a valid loop.
    //
    // Notes:
    //    May modify flow graph to make loop compact before returning.
    //    Will set instance fields to track loop's extent and exits if a valid
    //    loop is found, and potentially trash them otherwise.
    //
    bool FindLoop(BasicBlock* head, BasicBlock* top, BasicBlock* bottom)
    {
        // Is this a loop candidate? - We look for "back edges", i.e. an edge from BOTTOM
        // to TOP (note that this is an abuse of notation since this is not necessarily a back edge
        // as the definition says, but merely an indication that we have a loop there).
        // Thus, we have to be very careful and after entry discovery check that it is indeed
        // the only place we enter the loop (especially for non-reducible flow graphs).

        JITDUMP("FindLoop: checking head:" FMT_BB " top:" FMT_BB " bottom:" FMT_BB "\n", head->bbNum, top->bbNum,
                bottom->bbNum);

        if (top->bbNum > bottom->bbNum) // is this a backward edge? (from BOTTOM to TOP)
        {
            // Edge from BOTTOM to TOP is not a backward edge
            JITDUMP("    " FMT_BB "->" FMT_BB " is not a backedge\n", bottom->bbNum, top->bbNum);
            return false;
        }

        if (bottom->bbNum > oldBlockMaxNum)
        {
            // Not a true back-edge; bottom is a block added to reconnect fall-through during
            // loop processing, so its block number does not reflect its position.
            JITDUMP("    " FMT_BB "->" FMT_BB " is not a true backedge\n", bottom->bbNum, top->bbNum);
            return false;
        }

        if (bottom->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_EHCATCHRET, BBJ_CALLFINALLY,
                           BBJ_SWITCH))
        {
            JITDUMP("    bottom odd jump kind\n");
            // BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_EHCATCHRET, and BBJ_CALLFINALLY can never form a
            // loop.
            // BBJ_SWITCH that has a backward jump appears only for labeled break.
            return false;
        }

        // The presence of a "back edge" is an indication that a loop might be present here.
        //
        // Definition: A loop is:
        //        1. A collection of STRONGLY CONNECTED nodes i.e. there is a path from any
        //           node in the loop to any other node in the loop (wholly within the loop)
        //        2. The loop has a unique ENTRY, i.e. there is only one way to reach a node
        //           in the loop from outside the loop, and that is through the ENTRY

        // Let's find the loop ENTRY
        BasicBlock* entry = FindEntry(head, top, bottom);

        if (entry == nullptr)
        {
            // For now, we only recognize loops where HEAD has some successor ENTRY in the loop.
            JITDUMP("    can't find entry\n");
            return false;
        }

        // Passed the basic checks; initialize instance state for this back-edge.
        this->head      = head;
        this->top       = top;
        this->entry     = entry;
        this->bottom    = bottom;
        this->lastExit  = nullptr;
        this->exitCount = 0;

        if (!HasSingleEntryCycle())
        {
            // There isn't actually a loop between TOP and BOTTOM
            JITDUMP("    not single entry cycle\n");
            return false;
        }

        if (!loopBlocks.IsMember(top->bbNum))
        {
            // The "back-edge" we identified isn't actually part of the flow cycle containing ENTRY
            JITDUMP("    top not in loop\n");
            return false;
        }

        // Disqualify loops where the first block of the loop is less nested in EH than
        // the bottom block. That is, we don't want to handle loops where the back edge
        // goes from within an EH region to a first block that is outside that same EH
        // region. Note that we *do* handle loops where the first block is the *first*
        // block of a more nested EH region (since it is legal to branch to the first
        // block of an immediately more nested EH region). So, for example, disqualify
        // this:
        //
        // BB02
        // ...
        // try {
        // ...
        // BB10 BBJ_COND => BB02
        // ...
        // }
        //
        // Here, BB10 is more nested than BB02.

        if (bottom->hasTryIndex() && !comp->bbInTryRegions(bottom->getTryIndex(), top))
        {
            JITDUMP("Loop 'top' " FMT_BB " is in an outer EH region compared to loop 'bottom' " FMT_BB ". Rejecting "
                    "loop.\n",
                    top->bbNum, bottom->bbNum);
            return false;
        }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        // Disqualify loops where the first block of the loop is a finally target.
        // The main problem is when multiple loops share a 'top' block that is a finally
        // target and we canonicalize the loops by adding a new loop head. In that case, we
        // need to update the blocks so the finally target bit is moved to the newly created
        // block, and removed from the old 'top' block. This is 'hard', so it's easier to disallow
        // the loop than to update the flow graph to support this case.

        if ((top->bbFlags & BBF_FINALLY_TARGET) != 0)
        {
            JITDUMP("Loop 'top' " FMT_BB " is a finally target. Rejecting loop.\n", top->bbNum);
            return false;
        }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

        // Compact the loop (sweep through it and move out any blocks that aren't part of the
        // flow cycle), and find the exits.
        if (!MakeCompactAndFindExits())
        {
            // Unable to preserve well-formed loop during compaction.
            JITDUMP("    can't compact\n");
            return false;
        }

        // We have a valid loop.
        return true;
    }

    //------------------------------------------------------------------------
    // GetExitCount: Return the exit count computed for the loop
    //
    unsigned char GetExitCount() const
    {
        return exitCount;
    }

private:
    //------------------------------------------------------------------------
    // FindEntry: See if given HEAD flows to valid ENTRY between given TOP and BOTTOM
    //
    // Arguments:
    //    head - Block to be the HEAD of any loop identified
    //    top - Block to be the TOP of any loop identified
    //    bottom - Block to be the BOTTOM of any loop identified
    //
    // Return Value:
    //    Block to be the ENTRY of any loop identified, or nullptr if no
    //    such entry meeting our criteria can be found.
    //
    // Notes:
    //    Returns main entry if one is found, does not check for side-entries.
    //
    BasicBlock* FindEntry(BasicBlock* head, BasicBlock* top, BasicBlock* bottom)
    {
        if (head->bbJumpKind == BBJ_ALWAYS)
        {
            if (head->bbJumpDest->bbNum <= bottom->bbNum && head->bbJumpDest->bbNum >= top->bbNum)
            {
                // OK - we enter somewhere within the loop.
                return head->bbJumpDest;
            }
            else
            {
                // special case - don't consider now
                // assert (!"Loop entered in weird way!");
                return nullptr;
            }
        }
        // Can we fall through into the loop?
        else if (head->KindIs(BBJ_NONE, BBJ_COND))
        {
            // The ENTRY is at the TOP (a do-while loop)
            return top;
        }
        else
        {
            return nullptr; // HEAD does not flow into the loop; bail for now
        }
    }

    //------------------------------------------------------------------------
    // HasSingleEntryCycle: Perform a reverse flow walk from ENTRY, visiting
    //    only blocks between TOP and BOTTOM, to determine if such a cycle
    //    exists and if it has a single entry.
    //
    // Return Value:
    //    true  - Found a single-entry cycle.
    //    false - Did not find a single-entry cycle.
    //
    // Notes:
    //    Will mark (in `loopBlocks`) all blocks found to participate in the cycle.
    //
    bool HasSingleEntryCycle()
    {
        // Now do a backwards flow walk from entry to see if we have a single-entry loop
        bool foundCycle = false;

        // Seed the loop block set and worklist with the entry block.
        loopBlocks.Reset(entry->bbNum);
        jitstd::list<BasicBlock*> worklist(comp->getAllocator(CMK_LoopOpt));
        worklist.push_back(entry);

        while (!worklist.empty())
        {
            BasicBlock* block = worklist.back();
            worklist.pop_back();

            // Make sure ENTRY dominates all blocks in the loop.
            if (block->bbNum > oldBlockMaxNum)
            {
                // This is a new block we added to connect fall-through, so the
                // recorded dominator information doesn't cover it.  Just continue,
                // and when we process its unique predecessor we'll abort if ENTRY
                // doesn't dominate that.
            }
            else if (!comp->fgDominate(entry, block))
            {
                JITDUMP("   (find cycle) entry:" FMT_BB " does not dominate " FMT_BB "\n", entry->bbNum, block->bbNum);
                return false;
            }

            // Add preds to the worklist, checking for side-entries.
            for (BasicBlock* const predBlock : block->PredBlocks())
            {
                unsigned int testNum = PositionNum(predBlock);

                if ((testNum < top->bbNum) || (testNum > bottom->bbNum))
                {
                    // Pred is out of loop range
                    if (block == entry)
                    {
                        if (predBlock == head)
                        {
                            // This is the single entry we expect.
                            continue;
                        }
                        // ENTRY has some pred other than head outside the loop.  If ENTRY does not
                        // dominate this pred, we'll consider this a side-entry and skip this loop;
                        // otherwise the loop is still valid and this may be a (flow-wise) back-edge
                        // of an outer loop.  For the dominance test, if `predBlock` is a new block, use
                        // its unique predecessor since the dominator tree has info for that.
                        BasicBlock* effectivePred = (predBlock->bbNum > oldBlockMaxNum ? predBlock->bbPrev : predBlock);
                        if (comp->fgDominate(entry, effectivePred))
                        {
                            // Outer loop back-edge
                            continue;
                        }
                    }

                    // There are multiple entries to this loop, don't consider it.

                    JITDUMP("   (find cycle) multiple entry:" FMT_BB "\n", block->bbNum);
                    return false;
                }

                bool isFirstVisit;
                if (predBlock == entry)
                {
                    // We have indeed found a cycle in the flow graph.
                    JITDUMP("   (find cycle) found cycle\n");
                    isFirstVisit = !foundCycle;
                    foundCycle   = true;
                    assert(loopBlocks.IsMember(predBlock->bbNum));
                }
                else if (loopBlocks.TestAndInsert(predBlock->bbNum))
                {
                    // Already visited this pred
                    isFirstVisit = false;
                }
                else
                {
                    // Add this predBlock to the worklist
                    worklist.push_back(predBlock);
                    isFirstVisit = true;
                }

                if (isFirstVisit && (predBlock->bbNext != nullptr) &&
                    (PositionNum(predBlock->bbNext) == predBlock->bbNum))
                {
                    // We've created a new block immediately after `predBlock` to
                    // reconnect what was fall-through.  Mark it as in-loop also;
                    // it needs to stay with `prev` and if it exits the loop we'd
                    // just need to re-create it if we tried to move it out.
                    loopBlocks.Insert(predBlock->bbNext->bbNum);
                }
            }
        }

        return foundCycle;
    }

    //------------------------------------------------------------------------
    // PositionNum: Get the number identifying a block's position per the
    //    lexical ordering that existed before searching for (and compacting)
    //    loops.
    //
    // Arguments:
    //    block - Block whose position is desired.
    //
    // Return Value:
    //    A number indicating that block's position relative to others.
    //
    // Notes:
    //    When the given block is a new one created during loop compaction,
    //    the number of its unique predecessor is returned.
    //
    unsigned int PositionNum(BasicBlock* block)
    {
        if (block->bbNum > oldBlockMaxNum)
        {
            // This must be a block we inserted to connect fall-through after moving blocks.
            // To determine if it's in the loop or not, use the number of its unique predecessor
            // block.
            assert(block->bbPreds->getSourceBlock() == block->bbPrev);
            assert(block->bbPreds->getNextPredEdge() == nullptr);
            return block->bbPrev->bbNum;
        }
        return block->bbNum;
    }

    //------------------------------------------------------------------------
    // MakeCompactAndFindExits: Compact the loop (sweep through it and move out
    //   any blocks that aren't part of the flow cycle), and find the exits (set
    //   lastExit and exitCount).
    //
    // Return Value:
    //    true  - Loop successfully compacted (or `loopBlocks` expanded to
    //            include all blocks in the lexical range), exits enumerated.
    //    false - Loop cannot be made compact and remain well-formed.
    //
    bool MakeCompactAndFindExits()
    {
        // Compaction (if it needs to happen) will require an insertion point.
        BasicBlock* moveAfter = nullptr;

        for (BasicBlock* previous = top->bbPrev; previous != bottom;)
        {
            BasicBlock* block = previous->bbNext;

            if (loopBlocks.IsMember(block->bbNum))
            {
                // This block is a member of the loop.  Check to see if it may exit the loop.
                CheckForExit(block);

                // Done processing this block; move on to the next.
                previous = block;
                continue;
            }

            // This block is lexically between TOP and BOTTOM, but it does not
            // participate in the flow cycle.  Check for a run of consecutive
            // such blocks.
            //
            // If blocks have been reordered and bbNum no longer reflects bbNext ordering
            // (say by a call to MakeCompactAndFindExits for an earlier loop or unsuccessful
            // attempt to find a loop), the bottom block of this loop may now appear earlier
            // in the bbNext chain than other loop blocks. So when the previous hasn't reached bottom
            // and block is a non-loop block, and we walk the bbNext chain, we may reach the end.
            // If so, give up on recognition of this loop.
            //
            BasicBlock* lastNonLoopBlock = block;
            BasicBlock* nextLoopBlock    = block->bbNext;
            while ((nextLoopBlock != nullptr) && !loopBlocks.IsMember(nextLoopBlock->bbNum))
            {
                lastNonLoopBlock = nextLoopBlock;
                nextLoopBlock    = nextLoopBlock->bbNext;
            }

            if (nextLoopBlock == nullptr)
            {
                JITDUMP("Did not find expected loop block when walking from " FMT_BB "\n", lastNonLoopBlock->bbNum);
                return false;
            }

            // Choose an insertion point for non-loop blocks if we haven't yet done so.
            if (moveAfter == nullptr)
            {
                moveAfter = FindInsertionPoint();
            }

            if (!BasicBlock::sameEHRegion(previous, nextLoopBlock) || !BasicBlock::sameEHRegion(previous, moveAfter))
            {
                // EH regions would be ill-formed if we moved these blocks out.
                // See if we can consider them loop blocks without introducing
                // a side-entry.
                if (CanTreatAsLoopBlocks(block, lastNonLoopBlock))
                {
                    // The call to `canTreatAsLoop` marked these blocks as part of the loop;
                    // iterate without updating `previous` so that we'll analyze them as part
                    // of the loop.
                    continue;
                }
                else
                {
                    // We can't move these out of the loop or leave them in, so just give
                    // up on this loop.
                    return false;
                }
            }

            // Now physically move the blocks.
            BasicBlock* moveBefore = moveAfter->bbNext;

            comp->fgUnlinkRange(block, lastNonLoopBlock);
            comp->fgMoveBlocksAfter(block, lastNonLoopBlock, moveAfter);
            comp->ehUpdateLastBlocks(moveAfter, lastNonLoopBlock);

            // Apply any adjustments needed for fallthrough at the boundaries of the moved region.
            FixupFallThrough(moveAfter, moveBefore, block);
            FixupFallThrough(lastNonLoopBlock, nextLoopBlock, moveBefore);
            // Also apply any adjustments needed where the blocks were snipped out of the loop.
            BasicBlock* newBlock = FixupFallThrough(previous, block, nextLoopBlock);
            if (newBlock != nullptr)
            {
                // This new block is in the loop and is a loop exit.
                loopBlocks.Insert(newBlock->bbNum);
                lastExit = newBlock;
                ++exitCount;
            }

            // Update moveAfter for the next insertion.
            moveAfter = lastNonLoopBlock;

            // Note that we've changed the flow graph, and continue without updating
            // `previous` so that we'll process nextLoopBlock.
            changedFlowGraph = true;
        }

        if ((exitCount == 1) && (lastExit == nullptr))
        {
            // If we happen to have a loop with two exits, one of which goes to an
            // infinite loop that's lexically nested inside it, where the inner loop
            // can't be moved out,  we can end up in this situation (because
            // CanTreatAsLoopBlocks will have decremented the count expecting to find
            // another exit later).  Bump the exit count to 2, since downstream code
            // will not be prepared for null lastExit with exitCount of 1.
            assert(forgotExit);
            exitCount = 2;
        }

        // Loop compaction was successful
        return true;
    }

    //------------------------------------------------------------------------
    // FindInsertionPoint: Find an appropriate spot to which blocks that are
    //    lexically between TOP and BOTTOM but not part of the flow cycle
    //    can be moved.
    //
    // Return Value:
    //    Block after which to insert moved blocks.
    //
    BasicBlock* FindInsertionPoint()
    {
        // Find an insertion point for blocks we're going to move.  Move them down
        // out of the loop, and if possible find a spot that won't break up fall-through.
        BasicBlock* moveAfter = bottom;
        while (moveAfter->bbFallsThrough())
        {
            // Keep looking for a better insertion point if we can.
            BasicBlock* newMoveAfter = TryAdvanceInsertionPoint(moveAfter);

            if (newMoveAfter == nullptr)
            {
                // Ran out of candidate insertion points, so just split up the fall-through.
                return moveAfter;
            }

            moveAfter = newMoveAfter;
        }

        return moveAfter;
    }

    //------------------------------------------------------------------------
    // TryAdvanceInsertionPoint: Find the next legal insertion point after
    //    the given one, if one exists.
    //
    // Arguments:
    //    oldMoveAfter - Prior insertion point; find the next after this.
    //
    // Return Value:
    //    The next block after `oldMoveAfter` that is a legal insertion point
    //    (i.e. blocks being swept out of the loop can be moved immediately
    //    after it), if one exists, else nullptr.
    //
    BasicBlock* TryAdvanceInsertionPoint(BasicBlock* oldMoveAfter)
    {
        BasicBlock* newMoveAfter = oldMoveAfter->bbNext;

        if (!BasicBlock::sameEHRegion(oldMoveAfter, newMoveAfter))
        {
            // Don't cross an EH region boundary.
            return nullptr;
        }

        if (newMoveAfter->KindIs(BBJ_ALWAYS, BBJ_COND))
        {
            unsigned int destNum = newMoveAfter->bbJumpDest->bbNum;
            if ((destNum >= top->bbNum) && (destNum <= bottom->bbNum) && !loopBlocks.IsMember(destNum))
            {
                // Reversing this branch out of block `newMoveAfter` could confuse this algorithm
                // (in particular, the edge would still be numerically backwards but no longer be
                // lexically backwards, so a lexical forward walk from TOP would not find BOTTOM),
                // so don't do that.
                // We're checking for BBJ_ALWAYS and BBJ_COND only here -- we don't need to
                // check for BBJ_SWITCH because we'd never consider it a loop back-edge.
                return nullptr;
            }
        }

        // Similarly check to see if advancing to `newMoveAfter` would reverse the lexical order
        // of an edge from the run of blocks being moved to `newMoveAfter` -- doing so would
        // introduce a new lexical back-edge, which could (maybe?) confuse the loop search
        // algorithm, and isn't desirable layout anyway.
        for (BasicBlock* const predBlock : newMoveAfter->PredBlocks())
        {
            unsigned int predNum = predBlock->bbNum;

            if ((predNum >= top->bbNum) && (predNum <= bottom->bbNum) && !loopBlocks.IsMember(predNum))
            {
                // Don't make this forward edge a backwards edge.
                return nullptr;
            }
        }

        if (IsRecordedBottom(newMoveAfter))
        {
            // This is the BOTTOM of another loop; don't move any blocks past it, to avoid moving them
            // out of that loop (we should have already done so when processing that loop if it were legal).
            return nullptr;
        }

        // Advancing the insertion point is ok, except that we can't split up any CallFinally/BBJ_ALWAYS
        // pair, so if we've got such a pair recurse to see if we can move past the whole thing.
        return (newMoveAfter->isBBCallAlwaysPair() ? TryAdvanceInsertionPoint(newMoveAfter) : newMoveAfter);
    }

    //------------------------------------------------------------------------
    // isOuterBottom: Determine if the given block is the BOTTOM of a previously
    //    recorded loop.
    //
    // Arguments:
    //    block - Block to check for BOTTOM-ness.
    //
    // Return Value:
    //    true - The blocks was recorded as `bottom` of some earlier-processed loop.
    //    false - No loops yet recorded have this block as their `bottom`.
    //
    bool IsRecordedBottom(BasicBlock* block)
    {
        if (block->bbNum > oldBlockMaxNum)
        {
            // This is a new block, which can't be an outer bottom block because we only allow old blocks
            // as BOTTOM.
            return false;
        }
        return BlockSetOps::IsMember(comp, bottomBlocks, block->bbNum);
    }

    //------------------------------------------------------------------------
    // CanTreatAsLoopBlocks: If the given range of blocks can be treated as
    //    loop blocks, add them to loopBlockSet and return true.  Otherwise,
    //    return false.
    //
    // Arguments:
    //    firstNonLoopBlock - First block in the run to be subsumed.
    //    lastNonLoopBlock - Last block in the run to be subsumed.
    //
    // Return Value:
    //    true - The blocks from `fistNonLoopBlock` to `lastNonLoopBlock` were
    //           successfully added to `loopBlocks`.
    //    false - Treating the blocks from `fistNonLoopBlock` to `lastNonLoopBlock`
    //            would not be legal (it would induce a side-entry).
    //
    // Notes:
    //    `loopBlocks` may be modified even if `false` is returned.
    //    `exitCount` and `lastExit` may be modified if this process identifies
    //    in-loop edges that were previously counted as exits.
    //
    bool CanTreatAsLoopBlocks(BasicBlock* firstNonLoopBlock, BasicBlock* lastNonLoopBlock)
    {
        for (BasicBlock* const testBlock : comp->Blocks(firstNonLoopBlock, lastNonLoopBlock))
        {
            for (BasicBlock* const testPred : testBlock->PredBlocks())
            {
                unsigned int predPosNum         = PositionNum(testPred);
                unsigned int firstNonLoopPosNum = PositionNum(firstNonLoopBlock);
                unsigned int lastNonLoopPosNum  = PositionNum(lastNonLoopBlock);

                if (loopBlocks.IsMember(predPosNum) ||
                    ((predPosNum >= firstNonLoopPosNum) && (predPosNum <= lastNonLoopPosNum)))
                {
                    // This pred is in the loop (or what will be the loop if we determine this
                    // run of exit blocks doesn't include a side-entry).

                    if (predPosNum < firstNonLoopPosNum)
                    {
                        // We've already counted this block as an exit, so decrement the count.
                        --exitCount;
                        if (lastExit == testPred)
                        {
                            // Erase this now-bogus `lastExit` entry.
                            lastExit = nullptr;
                            INDEBUG(forgotExit = true);
                        }
                    }
                }
                else
                {
                    // This pred is not in the loop, so this constitutes a side-entry.
                    return false;
                }
            }

            // Either we're going to abort the loop on a subsequent testBlock, or this
            // testBlock is part of the loop.
            loopBlocks.Insert(testBlock->bbNum);
        }

        // All blocks were ok to leave in the loop.
        return true;
    }

    //------------------------------------------------------------------------
    // FixupFallThrough: Re-establish any broken control flow connectivity
    //    and eliminate any "goto-next"s that were created by changing the
    //    given block's lexical follower.
    //
    // Arguments:
    //    block - Block whose `bbNext` has changed.
    //    oldNext - Previous value of `block->bbNext`.
    //    newNext - New value of `block->bbNext`.
    //
    // Return Value:
    //    If a new block is created to reconnect flow, the new block is
    //    returned; otherwise, nullptr.
    //
    BasicBlock* FixupFallThrough(BasicBlock* block, BasicBlock* oldNext, BasicBlock* newNext)
    {
        // If we create a new block, that will be our return value.
        BasicBlock* newBlock = nullptr;

        if (block->bbFallsThrough())
        {
            // Need to reconnect the flow from `block` to `oldNext`.

            if ((block->bbJumpKind == BBJ_COND) && (block->bbJumpDest == newNext))
            {
                // Reverse the jump condition
                GenTree* test = block->lastNode();
                noway_assert(test->OperIsConditionalJump());

                if (test->OperGet() == GT_JTRUE)
                {
                    GenTree* cond = comp->gtReverseCond(test->AsOp()->gtOp1);
                    assert(cond == test->AsOp()->gtOp1); // Ensure `gtReverseCond` did not create a new node.
                    test->AsOp()->gtOp1 = cond;
                }
                else
                {
                    comp->gtReverseCond(test);
                }

                // Redirect the Conditional JUMP to go to `oldNext`
                block->bbJumpDest = oldNext;
            }
            else
            {
                // Insert an unconditional jump to `oldNext` just after `block`.
                newBlock = comp->fgConnectFallThrough(block, oldNext);
                noway_assert((newBlock == nullptr) || loopBlocks.CanRepresent(newBlock->bbNum));
            }
        }
        else if ((block->bbJumpKind == BBJ_ALWAYS) && (block->bbJumpDest == newNext))
        {
            // We've made `block`'s jump target its bbNext, so remove the jump.
            if (!comp->fgOptimizeBranchToNext(block, newNext, block->bbPrev))
            {
                // If optimizing away the goto-next failed for some reason, mark it KEEP_BBJ_ALWAYS to
                // prevent assertions from complaining about it.
                block->bbFlags |= BBF_KEEP_BBJ_ALWAYS;
            }

            // If block is newNext's only predecessor, move the IR from block to newNext,
            // but keep the now-empty block around.
            //
            // We move the IR because loop recognition has a very limited search capability and
            // won't walk from one block's statements to another, even if the blocks form
            // a linear chain. So this IR move enhances counted loop recognition.
            //
            // The logic here is essentially echoing fgCompactBlocks... but we don't call
            // that here because we don't want to delete block and do the necessary updates
            // to all the other data in flight, and we'd also prefer that newNext be the
            // survivor, not block.
            //
            if ((newNext->bbRefs == 1) && comp->fgCanCompactBlocks(block, newNext))
            {
                JITDUMP("Moving stmts from " FMT_BB " to " FMT_BB "\n", block->bbNum, newNext->bbNum);
                Statement* stmtList1 = block->firstStmt();
                Statement* stmtList2 = newNext->firstStmt();

                // Is there anything to move?
                //
                if (stmtList1 != nullptr)
                {
                    // Append newNext stmts to block's stmts.
                    //
                    if (stmtList2 != nullptr)
                    {
                        Statement* stmtLast1 = block->lastStmt();
                        Statement* stmtLast2 = newNext->lastStmt();

                        stmtLast1->SetNextStmt(stmtList2);
                        stmtList2->SetPrevStmt(stmtLast1);
                        stmtList1->SetPrevStmt(stmtLast2);
                    }

                    // Move block's stmts to newNext
                    //
                    newNext->bbStmtList = stmtList1;
                    block->bbStmtList   = nullptr;

                    // Update newNext's block flags
                    //
                    newNext->bbFlags |= (block->bbFlags & BBF_COMPACT_UPD);
                }
            }
        }

        // Make sure we don't leave around a goto-next unless it's marked KEEP_BBJ_ALWAYS.
        assert(!block->KindIs(BBJ_COND, BBJ_ALWAYS) || (block->bbJumpDest != newNext) ||
               ((block->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0));
        return newBlock;
    }

    //------------------------------------------------------------------------
    // CheckForExit: Check if the given block has any successor edges that are
    //    loop exits, and update `lastExit` and `exitCount` if so.
    //
    // Arguments:
    //    block - Block whose successor edges are to be checked.
    //
    // Notes:
    //    If one block has multiple exiting successor edges, those are counted
    //    as multiple exits in `exitCount`.
    //
    void CheckForExit(BasicBlock* block)
    {
        BasicBlock* exitPoint;

        switch (block->bbJumpKind)
        {
            case BBJ_COND:
            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                assert(block->bbJumpDest);
                exitPoint = block->bbJumpDest;

                if (!loopBlocks.IsMember(exitPoint->bbNum))
                {
                    // Exit from a block other than BOTTOM
                    CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(FEATURE_EH_FUNCLETS)
                    // On non-funclet platforms (x86), the catch exit is a BBJ_ALWAYS, but we don't want that to
                    // be considered a loop exit block, as catch handlers don't have predecessor lists and don't
                    // show up as might be expected in the dominator tree.
                    if (block->bbJumpKind == BBJ_ALWAYS)
                    {
                        if (!BasicBlock::sameHndRegion(block, exitPoint))
                        {
                            break;
                        }
                    }
#endif // !defined(FEATURE_EH_FUNCLETS)

                    lastExit = block;
                    exitCount++;
                }
                break;

            case BBJ_NONE:
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
            case BBJ_EHFILTERRET:
                // The "try" associated with this "finally" must be in the same loop, so the
                // finally block will return control inside the loop.
                break;

            case BBJ_THROW:
            case BBJ_RETURN:
                // Those are exits from the loop
                lastExit = block;
                exitCount++;
                break;

            case BBJ_SWITCH:
                for (BasicBlock* const exitPoint : block->SwitchTargets())
                {
                    if (!loopBlocks.IsMember(exitPoint->bbNum))
                    {
                        lastExit = block;
                        exitCount++;
                    }
                }
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

        if (block->bbFallsThrough() && !loopBlocks.IsMember(block->bbNext->bbNum))
        {
            // Found a fall-through exit.
            lastExit = block;
            exitCount++;
        }
    }
};
} // end (anonymous) namespace

//------------------------------------------------------------------------
// optFindNaturalLoops: Find the natural loops, using dominators. Note that the test for
// a loop is slightly different from the standard one, because we have not done a depth
// first reordering of the basic blocks.
//
// See LoopSearch class comment header for a description of the loops found.
//
// We will find and record a maximum of BasicBlock::MAX_LOOP_NUM loops (currently 64).
//
void Compiler::optFindNaturalLoops()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optFindNaturalLoops()\n");
    }
#endif // DEBUG

    noway_assert(fgDomsComputed);
    assert(fgHasLoops);

#if COUNT_LOOPS
    hasMethodLoops         = false;
    loopsThisMethod        = 0;
    loopOverflowThisMethod = false;
#endif

    LoopSearch search(this);

    for (BasicBlock* head = fgFirstBB; head->bbNext != nullptr; head = head->bbNext)
    {
        BasicBlock* top = head->bbNext;

        // Blocks that are rarely run have a zero bbWeight and should never be optimized here.
        if (top->bbWeight == BB_ZERO_WEIGHT)
        {
            continue;
        }

        for (BasicBlock* const predBlock : top->PredBlocks())
        {
            if (search.FindLoop(head, top, predBlock))
            {
                // Found a loop; record it and see if we've hit the limit.
                bool recordedLoop = search.RecordLoop();

                (void)recordedLoop; // avoid unusued variable warnings in COUNT_LOOPS and !DEBUG

#if COUNT_LOOPS
                if (!hasMethodLoops)
                {
                    // Mark the method as containing natural loops
                    totalLoopMethods++;
                    hasMethodLoops = true;
                }

                // Increment total number of loops found
                totalLoopCount++;
                loopsThisMethod++;

                // Keep track of the number of exits
                loopExitCountTable.record(static_cast<unsigned>(search.GetExitCount()));

                // Note that we continue to look for loops even if
                // (optLoopCount == BasicBlock::MAX_LOOP_NUM), in contrast to the !COUNT_LOOPS code below.
                // This gives us a better count and stats. Hopefully it doesn't affect actual codegen.
                CLANG_FORMAT_COMMENT_ANCHOR;

#else  // COUNT_LOOPS
                assert(recordedLoop);
                if (optLoopCount == BasicBlock::MAX_LOOP_NUM)
                {
                    // We won't be able to record any more loops, so stop looking.
                    goto NO_MORE_LOOPS;
                }
#endif // COUNT_LOOPS

                // Continue searching preds of `top` to see if any other are
                // back-edges (this can happen for nested loops).  The iteration
                // is safe because the compaction we do only modifies predecessor
                // lists of blocks that gain or lose fall-through from their
                // `bbPrev`, but since the motion is from within the loop to below
                // it, we know we're not altering the relationship between `top`
                // and its `bbPrev`.
            }
        }
    }

#if !COUNT_LOOPS
NO_MORE_LOOPS:
#endif // !COUNT_LOOPS

#if COUNT_LOOPS
    loopCountTable.record(loopsThisMethod);
    if (maxLoopsPerMethod < loopsThisMethod)
    {
        maxLoopsPerMethod = loopsThisMethod;
    }
    if (loopOverflowThisMethod)
    {
        totalLoopOverflows++;
    }
#endif // COUNT_LOOPS

    bool mod = search.ChangedFlowGraph();

    if (mod)
    {
        // Need to renumber blocks now since loop canonicalization
        // depends on it; can defer the rest of fgUpdateChangedFlowGraph()
        // until after canonicalizing loops. Dominator information is
        // recorded in terms of block numbers, so flag it invalid.
        fgDomsComputed = false;
        fgRenumberBlocks();
    }

    // Now the loop indices are stable. We can figure out parent/child relationships
    // (using table indices to name loops), and label blocks.
    for (unsigned char loopInd = 1; loopInd < optLoopCount; loopInd++)
    {
        for (unsigned char possibleParent = loopInd; possibleParent > 0;)
        {
            possibleParent--;
            if (optLoopTable[possibleParent].lpContains(optLoopTable[loopInd]))
            {
                optLoopTable[loopInd].lpParent       = possibleParent;
                optLoopTable[loopInd].lpSibling      = optLoopTable[possibleParent].lpChild;
                optLoopTable[possibleParent].lpChild = loopInd;
                break;
            }
        }
    }

    // Now label the blocks with the innermost loop to which they belong. Since parents
    // precede children in the table, doing the labeling for each loop in order will achieve
    // this -- the innermost loop labeling will be done last. (Inner loop blocks will be
    // labeled multiple times before being correct at the end.)
    for (unsigned char loopInd = 0; loopInd < optLoopCount; loopInd++)
    {
        for (BasicBlock* const blk : optLoopTable[loopInd].LoopBlocks())
        {
            blk->bbNatLoopNum = loopInd;
        }
    }

    // Make sure that loops are canonical: that every loop has a unique "top", by creating an empty "nop"
    // one, if necessary, for loops containing others that share a "top."
    //
    // Also make sure that no loop's "bottom" is another loop's "head".
    //
    for (unsigned char loopInd = 0; loopInd < optLoopCount; loopInd++)
    {
        // Traverse the outermost loops as entries into the loop nest; so skip non-outermost.
        if (optLoopTable[loopInd].lpParent != BasicBlock::NOT_IN_LOOP)
        {
            continue;
        }

        // Otherwise...
        if (optCanonicalizeLoopNest(loopInd))
        {
            mod = true;
        }
    }
    if (mod)
    {
        fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_DOMS);
    }

    // Create a loop pre-header for every loop.
    bool modForPreHeader = false;
    for (unsigned loopInd = 0; loopInd < optLoopCount; loopInd++)
    {
        if (fgCreateLoopPreHeader(loopInd))
        {
            modForPreHeader = true;
        }
    }
    if (modForPreHeader)
    {
        fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_DOMS);
    }

    // Starting now, we require all loops to have pre-headers.
    optLoopsRequirePreHeaders = true;

#ifdef DEBUG
    if (verbose && (optLoopCount > 0))
    {
        optPrintLoopTable();
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// optIdentifyLoopsForAlignment: Determine which loops should be considered for alignment.
//
// All innermost loops whose block weight meets a threshold are candidates for alignment.
// The `first` block of the loop is marked with the BBF_LOOP_ALIGN flag to indicate this
// (the loop table itself is not changed).
//
// Depends on the loop table, and on block weights being set.
//
void Compiler::optIdentifyLoopsForAlignment()
{
#if FEATURE_LOOP_ALIGN
    if (codeGen->ShouldAlignLoops())
    {
        for (BasicBlock::loopNumber loopInd = 0; loopInd < optLoopCount; loopInd++)
        {
            // An innerloop candidate that might need alignment
            if (optLoopTable[loopInd].lpChild == BasicBlock::NOT_IN_LOOP)
            {
                BasicBlock* top       = optLoopTable[loopInd].lpTop;
                weight_t    topWeight = top->getBBWeight(this);
                if (topWeight >= (opts.compJitAlignLoopMinBlockWeight * BB_UNITY_WEIGHT))
                {
                    // Sometimes with JitOptRepeat > 1, we might end up finding the loops twice. In such
                    // cases, make sure to count them just once.
                    if (!top->isLoopAlign())
                    {
                        loopAlignCandidates++;
                        top->bbFlags |= BBF_LOOP_ALIGN;
                        JITDUMP(FMT_LP " that starts at " FMT_BB " needs alignment, weight=" FMT_WT ".\n", loopInd,
                                top->bbNum, top->getBBWeight(this));
                    }
                }
                else
                {
                    JITDUMP(";; Skip alignment for " FMT_LP " that starts at " FMT_BB " weight=" FMT_WT ".\n", loopInd,
                            top->bbNum, topWeight);
                }
            }
        }
    }
#endif
}

//------------------------------------------------------------------------
// optRedirectBlock: Replace the branch successors of a block based on a block map.
//
// Updates the successors of `blk`: if `blk2` is a branch successor of `blk`, and there is a mapping
// for `blk2->blk3` in `redirectMap`, change `blk` so that `blk3` is this branch successor.
//
// Arguments:
//     blk          - block to redirect
//     redirectMap  - block->block map specifying how the `blk` target will be redirected.
//     predOption   - specifies how to update the pred lists
//
// Notes:
//     Fall-through successors are assumed correct and are not modified.
//     Pred lists for successors of `blk` may be changed, depending on `predOption`.
//
void Compiler::optRedirectBlock(BasicBlock* blk, BlockToBlockMap* redirectMap, RedirectBlockOption predOption)
{
    const bool updatePreds = (predOption == RedirectBlockOption::UpdatePredLists);
    const bool addPreds    = (predOption == RedirectBlockOption::AddToPredLists);

    if (addPreds && blk->bbFallsThrough())
    {
        fgAddRefPred(blk->bbNext, blk);
    }

    BasicBlock* newJumpDest = nullptr;

    switch (blk->bbJumpKind)
    {
        case BBJ_NONE:
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFILTERRET:
        case BBJ_EHFAULTRET:
        case BBJ_EHFINALLYRET:
        case BBJ_EHCATCHRET:
            // These have no jump destination to update.
            break;

        case BBJ_ALWAYS:
        case BBJ_LEAVE:
        case BBJ_CALLFINALLY:
        case BBJ_COND:
            // All of these have a single jump destination to update.
            if (redirectMap->Lookup(blk->bbJumpDest, &newJumpDest))
            {
                if (updatePreds)
                {
                    fgRemoveRefPred(blk->bbJumpDest, blk);
                }
                if (updatePreds || addPreds)
                {
                    fgAddRefPred(newJumpDest, blk);
                }
                blk->bbJumpDest = newJumpDest;
            }
            else if (addPreds)
            {
                fgAddRefPred(blk->bbJumpDest, blk);
            }
            break;

        case BBJ_SWITCH:
        {
            bool redirected = false;
            for (unsigned i = 0; i < blk->bbJumpSwt->bbsCount; i++)
            {
                BasicBlock* switchDest = blk->bbJumpSwt->bbsDstTab[i];
                if (redirectMap->Lookup(switchDest, &newJumpDest))
                {
                    if (updatePreds)
                    {
                        fgRemoveRefPred(switchDest, blk);
                    }
                    if (updatePreds || addPreds)
                    {
                        fgAddRefPred(newJumpDest, blk);
                    }
                    blk->bbJumpSwt->bbsDstTab[i] = newJumpDest;
                    redirected                   = true;
                }
                else if (addPreds)
                {
                    fgAddRefPred(switchDest, blk);
                }
            }
            // If any redirections happened, invalidate the switch table map for the switch.
            if (redirected)
            {
                // Don't create a new map just to try to remove an entry.
                BlockToSwitchDescMap* switchMap = GetSwitchDescMap(/* createIfNull */ false);
                if (switchMap != nullptr)
                {
                    switchMap->Remove(blk);
                }
            }
        }
        break;

        default:
            unreached();
    }
}

// TODO-Cleanup: This should be a static member of the BasicBlock class.
void Compiler::optCopyBlkDest(BasicBlock* from, BasicBlock* to)
{
    assert(from->bbJumpKind == to->bbJumpKind); // Precondition.

    // copy the jump destination(s) from "from" to "to".
    switch (to->bbJumpKind)
    {
        case BBJ_ALWAYS:
        case BBJ_LEAVE:
        case BBJ_CALLFINALLY:
        case BBJ_COND:
            // All of these have a single jump destination to update.
            to->bbJumpDest = from->bbJumpDest;
            break;

        case BBJ_SWITCH:
            to->bbJumpSwt = new (this, CMK_BasicBlock) BBswtDesc(this, from->bbJumpSwt);
            break;

        default:
            break;
    }
}

// Returns true if 'block' is an entry block for any loop in 'optLoopTable'
bool Compiler::optIsLoopEntry(BasicBlock* block) const
{
    for (unsigned char loopInd = 0; loopInd < optLoopCount; loopInd++)
    {
        if (optLoopTable[loopInd].lpIsRemoved())
        {
            continue;
        }

        if (optLoopTable[loopInd].lpEntry == block)
        {
            return true;
        }
    }
    return false;
}

//-----------------------------------------------------------------------------
// optCanonicalizeLoopNest: Canonicalize a loop nest
//
// Arguments:
//   loopInd - index of outermost loop in the nest
//
// Returns:
//   true if the flow graph was modified
//
// Notes:
//   For loopInd and all contained loops, ensures each loop top's back edges
//   only come from this loop.
//
//   Will split top blocks and redirect edges if needed.
//
bool Compiler::optCanonicalizeLoopNest(unsigned char loopInd)
{
    // First canonicalize the loop.
    //
    bool modified = optCanonicalizeLoop(loopInd);

    // Then any children.
    //
    for (unsigned char child = optLoopTable[loopInd].lpChild; //
         child != BasicBlock::NOT_IN_LOOP;                    //
         child = optLoopTable[child].lpSibling)
    {
        modified |= optCanonicalizeLoopNest(child);
    }

    return modified;
}

//-----------------------------------------------------------------------------
// optCanonicalizeLoop: ensure that each loop top's back edges come only from
//   blocks in the same loop, and that no loop head/bottom blocks coincide.
//
// Arguments:
//   loopInd - index of the loop to consider
//
// Returns:
//   true if flow changes were made
//
// Notes:
//
// Back edges incident on loop top fall into one three groups:
//
// (1) Outer non-loop backedges (preds dominated by entry where pred is not in loop)
// (2) The canonical backedge (pred == bottom)
// (3) Nested loop backedges or nested non-loop backedges
//     (preds dominated by entry, where pred is in loop, pred != bottom)
//
// We assume dominance has already been established by loop recognition (that is,
// anything classified as a loop will have all backedges dominated by loop entry,
// so the only possible non-backedge predecessor of top will be head).
//
// We cannot check dominance here as the flow graph is being modified.
//
// If either set (1) or (3) is non-empty the loop is not canonical.
//
// This method will split the loop top into two or three blocks depending on
// whether (1) or (3) is non-empty, and redirect the edges accordingly.
//
// Loops are canoncalized outer to inner, so inner loops should never see outer loop
// non-backedges, as the parent loop canonicalization should have handled them.
//
bool Compiler::optCanonicalizeLoop(unsigned char loopInd)
{
    bool              modified = false;
    BasicBlock*       h        = optLoopTable[loopInd].lpHead;
    BasicBlock* const t        = optLoopTable[loopInd].lpTop;
    BasicBlock* const e        = optLoopTable[loopInd].lpEntry;
    BasicBlock* const b        = optLoopTable[loopInd].lpBottom;

    // Normally, `head` either falls through to the `top` or branches to a non-`top` middle
    // entry block. If the `head` branches to `top` because it is the BBJ_ALWAYS of a
    // BBJ_CALLFINALLY/BBJ_ALWAYS pair, we canonicalize by introducing a new fall-through
    // head block. See FindEntry() for the logic that allows this.
    if ((h->bbJumpKind == BBJ_ALWAYS) && (h->bbJumpDest == t) && (h->bbFlags & BBF_KEEP_BBJ_ALWAYS))
    {
        // Insert new head

        BasicBlock* const newH = fgNewBBafter(BBJ_NONE, h, /*extendRegion*/ true);
        newH->inheritWeight(h);
        newH->bbNatLoopNum = h->bbNatLoopNum;
        h->bbJumpDest      = newH;

        fgRemoveRefPred(t, h);
        fgAddRefPred(newH, h);
        fgAddRefPred(t, newH);

        optUpdateLoopHead(loopInd, h, newH);

        JITDUMP("in optCanonicalizeLoop: " FMT_LP " head " FMT_BB
                " is BBJ_ALWAYS of BBJ_CALLFINALLY/BBJ_ALWAYS pair that targets top " FMT_BB
                ". Replacing with new BBJ_NONE head " FMT_BB ".",
                loopInd, h->bbNum, t->bbNum, newH->bbNum);

        h        = newH;
        modified = true;
    }

    // Look for case (1)
    //
    bool doOuterCanon = false;

    for (BasicBlock* const topPredBlock : t->PredBlocks())
    {
        const bool predIsInLoop = (t->bbNum <= topPredBlock->bbNum) && (topPredBlock->bbNum <= b->bbNum);
        if (predIsInLoop || (topPredBlock == h))
        {
            // no action needed
        }
        else
        {
            JITDUMP("in optCanonicalizeLoop: " FMT_LP " top " FMT_BB " (entry " FMT_BB " bottom " FMT_BB
                    ") %shas a non-loop backedge from " FMT_BB "%s\n",
                    loopInd, t->bbNum, e->bbNum, b->bbNum, doOuterCanon ? "also " : "", topPredBlock->bbNum,
                    doOuterCanon ? "" : ": need to canonicalize non-loop backedges");
            doOuterCanon = true;
        }
    }

    if (doOuterCanon)
    {
        const bool didCanon = optCanonicalizeLoopCore(loopInd, LoopCanonicalizationOption::Outer);
        assert(didCanon);
        modified |= didCanon;
    }

    // Look for case (3)
    //
    // Outer canon should not update loop top.
    //
    assert(t == optLoopTable[loopInd].lpTop);
    if (t->bbNatLoopNum != loopInd)
    {
        JITDUMP("in optCanonicalizeLoop: " FMT_LP " has top " FMT_BB " (entry " FMT_BB " bottom " FMT_BB
                ") with natural loop number " FMT_LP ": need to canonicalize nested inner loop backedges\n",
                loopInd, t->bbNum, e->bbNum, b->bbNum, t->bbNatLoopNum);

        const bool didCanon = optCanonicalizeLoopCore(loopInd, LoopCanonicalizationOption::Current);
        assert(didCanon);
        modified |= didCanon;
    }

    // Check if this loopInd head is also the bottom of some sibling.
    // If so, add a block in between to serve as the new head.
    //
    auto repairLoop = [this](unsigned char loopInd, unsigned char sibling) {

        BasicBlock* const h        = optLoopTable[loopInd].lpHead;
        BasicBlock* const siblingB = optLoopTable[sibling].lpBottom;

        if (h == siblingB)
        {
            // We have
            //
            //   sibling.B (== loopInd.H) -e-> loopInd.T
            //
            // where e is a "critical edge", that is
            // * sibling.B has other successors (notably sibling.T),
            // * loopInd.T has other predecessors (notably loopInd.B)
            //
            // turn this into
            //
            //  sibling.B -> newH (== loopInd.H) -> loopInd.T
            //
            // Ideally we'd just call fgSplitEdge, but we are
            // not keeping pred lists in good shape.
            //
            BasicBlock* const t = optLoopTable[loopInd].lpTop;
            assert(siblingB->bbJumpKind == BBJ_COND);
            assert(siblingB->bbNext == t);

            JITDUMP(FMT_LP " head " FMT_BB " is also " FMT_LP " bottom\n", loopInd, h->bbNum, sibling);

            BasicBlock* const newH = fgNewBBbefore(BBJ_NONE, t, /*extendRegion*/ true);

            fgRemoveRefPred(t, h);
            fgAddRefPred(t, newH);
            fgAddRefPred(newH, h);

            // Anything that flows into sibling will flow here.
            // So we use sibling.H as our best guess for weight.
            //
            newH->inheritWeight(optLoopTable[sibling].lpHead);
            newH->bbNatLoopNum = optLoopTable[loopInd].lpParent;
            optUpdateLoopHead(loopInd, h, newH);

            return true;
        }
        return false;
    };

    if (optLoopTable[loopInd].lpParent == BasicBlock::NOT_IN_LOOP)
    {
        // check against all other top-level loops
        //
        for (unsigned char sibling = 0; sibling < optLoopCount; sibling++)
        {
            if (optLoopTable[sibling].lpParent != BasicBlock::NOT_IN_LOOP)
            {
                continue;
            }

            modified |= repairLoop(loopInd, sibling);
        }
    }
    else
    {
        // check against all other sibling loops
        //
        const unsigned char parentLoop = optLoopTable[loopInd].lpParent;

        for (unsigned char sibling = optLoopTable[parentLoop].lpChild; //
             sibling != BasicBlock::NOT_IN_LOOP;                       //
             sibling = optLoopTable[sibling].lpSibling)
        {
            if (sibling == loopInd)
            {
                continue;
            }

            modified |= repairLoop(loopInd, sibling);
        }
    }

    if (modified)
    {
        JITDUMP("Done canonicalizing " FMT_LP "\n\n", loopInd);
    }

    return modified;
}

//-----------------------------------------------------------------------------
// optCanonicalizeLoopCore: ensure that each loop top's back edges come do not
//   come from outer/inner loops.
//
// Arguments:
//   loopInd - index of the loop to consider
//   option - which set of edges to move when canonicalizing
//
// Returns:
//   true if flow changes were made
//
// Notes:
//   option ::Outer retargets all backedges that do not come from loops in the block.
//   option ::Current retargets the canonical backedge (from bottom)
//
bool Compiler::optCanonicalizeLoopCore(unsigned char loopInd, LoopCanonicalizationOption option)
{
    // Otherwise, the top of this loop is also part of a nested loop or has
    // non-loop backedges.
    //
    // Insert a new unique top for this loop. We must be careful to put this new
    // block in the correct EH region. Note that t->bbPrev might be in a different
    // EH region. For example:
    //
    // try {
    //      ...
    //      BB07
    // }
    // BB08 // "first"
    //
    // In this case, first->bbPrev is BB07, which is in a different 'try' region.
    // On the other hand, the first block of multiple loops might be the first
    // block of a 'try' region that is completely contained in the multiple loops.
    // for example:
    //
    // BB08 try { }
    // ...
    // BB10 BBJ_ALWAYS => BB08
    // ...
    // BB12 BBJ_ALWAYS => BB08
    //
    // Here, we have two loops, both with BB08 as the "first" block. Block BB08
    // is a single-block "try" region. Neither loop "bottom" block is in the same
    // "try" region as BB08. This is legal because you can jump to the first block
    // of a try region. With EH normalization, no two "try" regions will share
    // this block. In this case, we need to insert a new block for the outer loop
    // in the same EH region as the branch from the "bottom":
    //
    // BB30 BBJ_NONE
    // BB08 try { }
    // ...
    // BB10 BBJ_ALWAYS => BB08
    // ...
    // BB12 BBJ_ALWAYS => BB30
    //
    // Another possibility is that the "first" block of the loop nest can be the first block
    // of a "try" region that also has other predecessors than those in the loop, or even in
    // the "try" region (since blocks can target the first block of a "try" region). For example:
    //
    // BB08 try {
    // ...
    // BB10 BBJ_ALWAYS => BB08
    // ...
    // BB12 BBJ_ALWAYS => BB08
    // BB13 }
    // ...
    // BB20 BBJ_ALWAYS => BB08
    // ...
    // BB25 BBJ_ALWAYS => BB08
    //
    // Here, BB08 has 4 flow graph predecessors: BB10, BB12, BB20, BB25. These are all potential loop
    // bottoms, for four possible nested loops. However, we require all the loop bottoms to be in the
    // same EH region. For loops BB08..BB10 and BB08..BB12, we need to add a new "top" block within
    // the try region, immediately before BB08. The bottom of the loop BB08..BB10 loop will target the
    // old BB08, and the bottom of the BB08..BB12 loop will target the new loop header. The other branches
    // (BB20, BB25) must target the new loop header, both for correctness, and to avoid the illegal
    // situation of branching to a non-first block of a 'try' region.
    //
    // We can also have a loop nest where the "first" block is outside of a "try" region
    // and the back edges are inside a "try" region, for example:
    //
    // BB02 // "first"
    // ...
    // BB09 try { BBJ_COND => BB02
    // ...
    // BB15 BBJ_COND => BB02
    // ...
    // BB21 } // end of "try"
    //
    // In this case, both loop back edges were formed by "leave" instructions that were
    // imported into branches that were later made conditional. In this case, we don't
    // want to copy the EH region of the back edge, since that would create a block
    // outside of and disjoint with the "try" region of the back edge. However, to
    // simplify things, we disqualify this type of loop, so we should never see this here.
    //
    BasicBlock* const b = optLoopTable[loopInd].lpBottom;
    BasicBlock* const t = optLoopTable[loopInd].lpTop;
    BasicBlock* const h = optLoopTable[loopInd].lpHead;

    // The loop must be entirely contained within a single handler region.
    assert(BasicBlock::sameHndRegion(t, b));

    // We expect h to be already "canonical" -- that is, it falls through to t
    // and is not a degenerate BBJ_COND (both branches and falls through to t)
    // or a side entry to the loop.
    //
    // Because of this, introducing a block before t automatically gives us
    // the right flow out of h.
    //
    assert(h->bbNext == t);
    assert(h->bbFallsThrough());
    assert((h->bbJumpKind == BBJ_NONE) || (h->bbJumpKind == BBJ_COND));
    if (h->bbJumpKind == BBJ_COND)
    {
        BasicBlock* const hj = h->bbJumpDest;
        assert((hj->bbNum < t->bbNum) || (hj->bbNum > b->bbNum));
    }

    // If the bottom block is in the same "try" region, then we extend the EH
    // region. Otherwise, we add the new block outside the "try" region.
    //
    const bool        extendRegion = BasicBlock::sameTryRegion(t, b);
    BasicBlock* const newT         = fgNewBBbefore(BBJ_NONE, t, extendRegion);

    fgRemoveRefPred(t, h);
    fgAddRefPred(t, newT);
    fgAddRefPred(newT, h);

    // Initially give newT the same weight as t; we will subtract from
    // this for each edge that does not move from t to newT.
    //
    newT->inheritWeight(t);

    if (!extendRegion)
    {
        // We need to set the EH region manually. Set it to be the same
        // as the bottom block.
        newT->copyEHRegion(b);
    }

    // NewT will be the target for the outer/current loop's backedge(s).
    //
    BlockToBlockMap* const blockMap = new (getAllocator(CMK_LoopOpt)) BlockToBlockMap(getAllocator(CMK_LoopOpt));
    blockMap->Set(t, newT);

    // The new block can reach the same set of blocks as the old one, but don't try to reflect
    // that in its reachability set here -- creating the new block may have changed the BlockSet
    // representation from short to long, and canonicalizing loops is immediately followed by
    // a call to fgUpdateChangedFlowGraph which will recompute the reachability sets anyway.

    bool firstPred = true;
    for (BasicBlock* const topPredBlock : t->PredBlocks())
    {
        // We set profile weight of newT assuming all edges would
        // be redirected there. So, if we don't redirect this edge,
        // this is how much we'll have to adjust newT's weight.
        //
        weight_t weightAdjust = BB_ZERO_WEIGHT;

        if (option == LoopCanonicalizationOption::Current)
        {
            // Redirect the (one and only) true backedge of this loop.
            //
            if (topPredBlock != b)
            {
                if ((topPredBlock != h) && topPredBlock->hasProfileWeight())
                {
                    // Note this may overstate the adjustment, if topPredBlock is BBJ_COND.
                    //
                    weightAdjust = topPredBlock->bbWeight;
                }
            }
            else
            {
                JITDUMP("in optCanonicalizeLoop (current): redirect bottom->top backedge " FMT_BB " -> " FMT_BB
                        " to " FMT_BB " -> " FMT_BB "\n",
                        topPredBlock->bbNum, t->bbNum, topPredBlock->bbNum, newT->bbNum);
                optRedirectBlock(b, blockMap, RedirectBlockOption::UpdatePredLists);
            }
        }
        else if (option == LoopCanonicalizationOption::Outer)
        {
            // Redirect non-loop preds of "t" to go to "newT". Inner loops that also branch to "t" should continue
            // to do so. However, there maybe be other predecessors from outside the loop nest that need to be updated
            // to point to "newT". This normally wouldn't happen, since they too would be part of the loop nest.
            // However,
            // they might have been prevented from participating in the loop nest due to different EH nesting, or some
            // other reason.
            //
            // Skip if topPredBlock is in the loop.
            // Note that this uses block number to detect membership in the loop. We are adding blocks during
            // canonicalization, and those block numbers will be new, and larger than previous blocks. However, we work
            // outside-in, so we shouldn't encounter the new blocks at the loop boundaries, or in the predecessor lists.
            //
            if ((t->bbNum <= topPredBlock->bbNum) && (topPredBlock->bbNum <= b->bbNum))
            {
                if (topPredBlock->hasProfileWeight())
                {
                    // Note this may overstate the adjustment, if topPredBlock is BBJ_COND.
                    //
                    weightAdjust = topPredBlock->bbWeight;
                }
            }
            else
            {
                JITDUMP("in optCanonicalizeLoop (outer): redirect %s->top %sedge " FMT_BB " -> " FMT_BB " to " FMT_BB
                        " -> " FMT_BB "\n",
                        topPredBlock == h ? "head" : "nonloop", topPredBlock == h ? "" : "back", topPredBlock->bbNum,
                        t->bbNum, topPredBlock->bbNum, newT->bbNum);
                optRedirectBlock(topPredBlock, blockMap, RedirectBlockOption::UpdatePredLists);
            }
        }
        else
        {
            unreached();
        }

        if (weightAdjust > BB_ZERO_WEIGHT)
        {
            JITDUMP("in optCanonicalizeLoop: removing block " FMT_BB " weight " FMT_WT " from " FMT_BB "\n",
                    topPredBlock->bbNum, weightAdjust, newT->bbNum);

            if (newT->bbWeight >= weightAdjust)
            {
                newT->setBBProfileWeight(newT->bbWeight - weightAdjust);
            }
            else if (newT->bbWeight > BB_ZERO_WEIGHT)
            {
                newT->setBBProfileWeight(BB_ZERO_WEIGHT);
            }
        }
    }

    assert(h->bbNext == newT);
    assert(newT->bbNext == t);

    // With the Option::Current we are changing which block is loop top.
    // Make suitable updates.
    //
    if (option == LoopCanonicalizationOption::Current)
    {
        JITDUMP("in optCanonicalizeLoop (current): " FMT_BB " is now the top of loop " FMT_LP "\n", newT->bbNum,
                loopInd);

        optLoopTable[loopInd].lpTop = newT;
        newT->bbNatLoopNum          = loopInd;

        // If loopInd was a do-while loop (top == entry), update entry, as well.
        //
        BasicBlock* const origE = optLoopTable[loopInd].lpEntry;
        if (origE == t)
        {
            JITDUMP("updating entry of " FMT_LP " to " FMT_BB "\n", loopInd, newT->bbNum);
            optLoopTable[loopInd].lpEntry = newT;
        }

        // If any loops nested in "loopInd" have the same head and entry as "loopInd",
        // it must be the case that they were do-while's (since "h" fell through to the entry).
        // The new node "newT" becomes the head of such loops.
        for (unsigned char childLoop = optLoopTable[loopInd].lpChild; //
             childLoop != BasicBlock::NOT_IN_LOOP;                    //
             childLoop = optLoopTable[childLoop].lpSibling)
        {
            if ((optLoopTable[childLoop].lpEntry == origE) && (optLoopTable[childLoop].lpHead == h) &&
                (newT->bbJumpKind == BBJ_NONE) && (newT->bbNext == origE))
            {
                optUpdateLoopHead(childLoop, h, newT);

                // Fix pred list here, so when we walk preds of child loop tops
                // we see the right blocks.
                //
                fgReplacePred(optLoopTable[childLoop].lpTop, h, newT);
            }
        }
    }
    else if (option == LoopCanonicalizationOption::Outer)
    {
        JITDUMP("in optCanonicalizeLoop (outer): " FMT_BB " is outside of loop " FMT_LP "\n", newT->bbNum, loopInd);

        // If we are lifting outer backeges, then newT belongs to our parent loop
        //
        newT->bbNatLoopNum = optLoopTable[loopInd].lpParent;

        // newT is now the header of this loop
        //
        optUpdateLoopHead(loopInd, h, newT);
    }

    return true;
}

//-----------------------------------------------------------------------------
// optLoopContains: Check if one loop contains another
//
// Arguments:
//    l1 -- loop num of containing loop (must be valid loop num)
//    l2 -- loop num of contained loop (valid loop num, or NOT_IN_LOOP)
//
// Returns:
//    True if loop described by l2 is contained within l1.
//
// Notes:
//    A loop contains itself.
//
bool Compiler::optLoopContains(unsigned l1, unsigned l2) const
{
    assert(l1 < optLoopCount);
    assert((l2 < optLoopCount) || (l2 == BasicBlock::NOT_IN_LOOP));

    if (l1 == l2)
    {
        return true;
    }
    else if (l2 == BasicBlock::NOT_IN_LOOP)
    {
        return false;
    }
    else
    {
        return optLoopContains(l1, optLoopTable[l2].lpParent);
    }
}

//-----------------------------------------------------------------------------
// optLoopEntry: For a given preheader of a loop, returns the lpEntry.
//
// Arguments:
//    preHeader -- preheader of a loop
//
// Returns:
//    Corresponding loop entry block.
//
BasicBlock* Compiler::optLoopEntry(BasicBlock* preHeader)
{
    assert((preHeader->bbFlags & BBF_LOOP_PREHEADER) != 0);

    if (preHeader->KindIs(BBJ_NONE))
    {
        return preHeader->bbNext;
    }
    else
    {
        assert(preHeader->KindIs(BBJ_ALWAYS));
        return preHeader->bbJumpDest;
    }
}

//-----------------------------------------------------------------------------
// optUpdateLoopHead: Replace the `head` block of a loop in the loop table.
// Considers all child loops that might share the same head (recursively).
//
// Arguments:
//    loopInd -- loop num of loop
//    from    -- current loop head block
//    to      -- replacement loop head block
//
void Compiler::optUpdateLoopHead(unsigned loopInd, BasicBlock* from, BasicBlock* to)
{
    assert(optLoopTable[loopInd].lpHead == from);
    JITDUMP("Replace " FMT_LP " head " FMT_BB " with " FMT_BB "\n", loopInd, from->bbNum, to->bbNum);
    optLoopTable[loopInd].lpHead = to;
    for (unsigned char childLoop = optLoopTable[loopInd].lpChild; //
         childLoop != BasicBlock::NOT_IN_LOOP;                    //
         childLoop = optLoopTable[childLoop].lpSibling)
    {
        if (optLoopTable[childLoop].lpHead == from)
        {
            optUpdateLoopHead(childLoop, from, to);
        }
    }
}

//-----------------------------------------------------------------------------
// optIterSmallOverflow: Helper for loop unrolling. Determine if "i += const" will
// cause an overflow exception for the small types.
//
// Arguments:
//    iterAtExit - iteration constant at loop exit
//    incrType   - type of increment
//
// Returns:
//   true if overflow
//
// static
bool Compiler::optIterSmallOverflow(int iterAtExit, var_types incrType)
{
    int type_MAX;

    switch (incrType)
    {
        case TYP_BYTE:
            type_MAX = SCHAR_MAX;
            break;
        case TYP_UBYTE:
            type_MAX = UCHAR_MAX;
            break;
        case TYP_SHORT:
            type_MAX = SHRT_MAX;
            break;
        case TYP_USHORT:
            type_MAX = USHRT_MAX;
            break;

        case TYP_UINT: // Detected by checking for 32bit ....
        case TYP_INT:
            return false; // ... overflow same as done for TYP_INT

        default:
            NO_WAY("Bad type");
    }

    if (iterAtExit > type_MAX)
    {
        return true;
    }
    else
    {
        return false;
    }
}

//-----------------------------------------------------------------------------
// optIterSmallUnderflow: Helper for loop unrolling. Determine if "i -= const" will
// cause an underflow exception for the small types.
//
// Arguments:
//    iterAtExit - iteration constant at loop exit
//    decrType   - type of decrement
//
// Returns:
//   true if overflow
//
// static
bool Compiler::optIterSmallUnderflow(int iterAtExit, var_types decrType)
{
    int type_MIN;

    switch (decrType)
    {
        case TYP_BYTE:
            type_MIN = SCHAR_MIN;
            break;
        case TYP_SHORT:
            type_MIN = SHRT_MIN;
            break;
        case TYP_UBYTE:
            type_MIN = 0;
            break;
        case TYP_USHORT:
            type_MIN = 0;
            break;

        case TYP_UINT: // Detected by checking for 32bit ....
        case TYP_INT:
            return false; // ... underflow same as done for TYP_INT

        default:
            NO_WAY("Bad type");
    }

    if (iterAtExit < type_MIN)
    {
        return true;
    }
    else
    {
        return false;
    }
}

//-----------------------------------------------------------------------------
// optComputeLoopRep: Helper for loop unrolling. Computes the number of repetitions
// in a constant loop.
//
// Arguments:
//    constInit    - loop constant initial value
//    constLimit   - loop constant limit
//    iterInc      - loop iteration increment
//    iterOper     - loop iteration increment operator (ADD, SUB, etc.)
//    iterOperType - iteration operator type
//    testOper     - type of loop test (i.e. GT_LE, GT_GE, etc.)
//    unsTest      - true if test is unsigned
//    dupCond      - true if the loop head contains a test which skips this loop
//    iterCount    - *iterCount is set to the iteration count, if the function returns `true`
//
// Returns:
//   true if the loop has a constant repetition count, false if that cannot be proven
//
bool Compiler::optComputeLoopRep(int        constInit,
                                 int        constLimit,
                                 int        iterInc,
                                 genTreeOps iterOper,
                                 var_types  iterOperType,
                                 genTreeOps testOper,
                                 bool       unsTest,
                                 bool       dupCond,
                                 unsigned*  iterCount)
{
    noway_assert(genActualType(iterOperType) == TYP_INT);

    __int64 constInitX;
    __int64 constLimitX;

    unsigned loopCount;
    int      iterSign;

    // Using this, we can just do a signed comparison with other 32 bit values.
    if (unsTest)
    {
        constLimitX = (unsigned int)constLimit;
    }
    else
    {
        constLimitX = (signed int)constLimit;
    }

    switch (iterOperType)
    {
// For small types, the iteration operator will narrow these values if big

#define INIT_ITER_BY_TYPE(type)                                                                                        \
    constInitX = (type)constInit;                                                                                      \
    iterInc    = (type)iterInc;

        case TYP_BYTE:
            INIT_ITER_BY_TYPE(signed char);
            break;
        case TYP_UBYTE:
            INIT_ITER_BY_TYPE(unsigned char);
            break;
        case TYP_SHORT:
            INIT_ITER_BY_TYPE(signed short);
            break;
        case TYP_USHORT:
            INIT_ITER_BY_TYPE(unsigned short);
            break;

        // For the big types, 32 bit arithmetic is performed

        case TYP_INT:
        case TYP_UINT:
            if (unsTest)
            {
                constInitX = (unsigned int)constInit;
            }
            else
            {
                constInitX = (signed int)constInit;
            }
            break;

        default:
            noway_assert(!"Bad type");
            NO_WAY("Bad type");
    }

    // If iterInc is zero we have an infinite loop.
    if (iterInc == 0)
    {
        return false;
    }

    // Set iterSign to +1 for positive iterInc and -1 for negative iterInc.
    iterSign = (iterInc > 0) ? +1 : -1;

    // Initialize loopCount to zero.
    loopCount = 0;

    // If dupCond is true then the loop initialization block contains a test which skips
    // this loop, if the constInit does not pass the loop test.
    // Such a loop can execute zero times.
    // If dupCond is false then we have a true do-while loop where we
    // always execute the loop once before performing the loop test.
    if (!dupCond)
    {
        loopCount += 1;
        constInitX += iterInc;
    }

    // bail if count is based on wrap-around math
    if (iterInc > 0)
    {
        if (constLimitX < constInitX)
        {
            return false;
        }
    }
    else if (constLimitX > constInitX)
    {
        return false;
    }

    // Compute the number of repetitions.

    switch (testOper)
    {
        __int64 iterAtExitX;

        case GT_EQ:
            // Something like "for (i=init; i == lim; i++)" doesn't make any sense.
            return false;

        case GT_NE:
            // Consider: "for (i = init; i != lim; i += const)"
            // This is tricky since it may have a constant number of iterations or loop forever.
            // We have to compute "(lim - init) mod iterInc" to see if it is zero.
            // If "mod iterInc" is not zero then the limit test will miss and a wrap will occur
            // which is probably not what the end user wanted, but it is legal.

            if (iterInc > 0)
            {
                // Stepping by one, i.e. Mod with 1 is always zero.
                if (iterInc != 1)
                {
                    if (((constLimitX - constInitX) % iterInc) != 0)
                    {
                        return false;
                    }
                }
            }
            else
            {
                noway_assert(iterInc < 0);
                // Stepping by -1, i.e. Mod with 1 is always zero.
                if (iterInc != -1)
                {
                    if (((constInitX - constLimitX) % (-iterInc)) != 0)
                    {
                        return false;
                    }
                }
            }

            switch (iterOper)
            {
                case GT_SUB:
                    iterInc = -iterInc;
                    FALLTHROUGH;

                case GT_ADD:
                    if (constInitX != constLimitX)
                    {
                        loopCount += (unsigned)((constLimitX - constInitX - iterSign) / iterInc) + 1;
                    }

                    iterAtExitX = (int)(constInitX + iterInc * (int)loopCount);

                    if (unsTest)
                    {
                        iterAtExitX = (unsigned)iterAtExitX;
                    }

                    // Check if iteration incr will cause overflow for small types
                    if (optIterSmallOverflow((int)iterAtExitX, iterOperType))
                    {
                        return false;
                    }

                    // iterator with 32bit overflow. Bad for TYP_(U)INT
                    if (iterAtExitX < constLimitX)
                    {
                        return false;
                    }

                    *iterCount = loopCount;
                    return true;

                case GT_MUL:
                case GT_DIV:
                case GT_RSH:
                case GT_LSH:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_LT:
            switch (iterOper)
            {
                case GT_SUB:
                    iterInc = -iterInc;
                    FALLTHROUGH;

                case GT_ADD:
                    if (constInitX < constLimitX)
                    {
                        loopCount += (unsigned)((constLimitX - constInitX - iterSign) / iterInc) + 1;
                    }

                    iterAtExitX = (int)(constInitX + iterInc * (int)loopCount);

                    if (unsTest)
                    {
                        iterAtExitX = (unsigned)iterAtExitX;
                    }

                    // Check if iteration incr will cause overflow for small types
                    if (optIterSmallOverflow((int)iterAtExitX, iterOperType))
                    {
                        return false;
                    }

                    // iterator with 32bit overflow. Bad for TYP_(U)INT
                    if (iterAtExitX < constLimitX)
                    {
                        return false;
                    }

                    *iterCount = loopCount;
                    return true;

                case GT_MUL:
                case GT_DIV:
                case GT_RSH:
                case GT_LSH:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_LE:
            switch (iterOper)
            {
                case GT_SUB:
                    iterInc = -iterInc;
                    FALLTHROUGH;

                case GT_ADD:
                    if (constInitX <= constLimitX)
                    {
                        loopCount += (unsigned)((constLimitX - constInitX) / iterInc) + 1;
                    }

                    iterAtExitX = (int)(constInitX + iterInc * (int)loopCount);

                    if (unsTest)
                    {
                        iterAtExitX = (unsigned)iterAtExitX;
                    }

                    // Check if iteration incr will cause overflow for small types
                    if (optIterSmallOverflow((int)iterAtExitX, iterOperType))
                    {
                        return false;
                    }

                    // iterator with 32bit overflow. Bad for TYP_(U)INT
                    if (iterAtExitX <= constLimitX)
                    {
                        return false;
                    }

                    *iterCount = loopCount;
                    return true;

                case GT_MUL:
                case GT_DIV:
                case GT_RSH:
                case GT_LSH:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_GT:
            switch (iterOper)
            {
                case GT_SUB:
                    iterInc = -iterInc;
                    FALLTHROUGH;

                case GT_ADD:
                    if (constInitX > constLimitX)
                    {
                        loopCount += (unsigned)((constLimitX - constInitX - iterSign) / iterInc) + 1;
                    }

                    iterAtExitX = (int)(constInitX + iterInc * (int)loopCount);

                    if (unsTest)
                    {
                        iterAtExitX = (unsigned)iterAtExitX;
                    }

                    // Check if small types will underflow
                    if (optIterSmallUnderflow((int)iterAtExitX, iterOperType))
                    {
                        return false;
                    }

                    // iterator with 32bit underflow. Bad for TYP_INT and unsigneds
                    if (iterAtExitX > constLimitX)
                    {
                        return false;
                    }

                    *iterCount = loopCount;
                    return true;

                case GT_MUL:
                case GT_DIV:
                case GT_RSH:
                case GT_LSH:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_GE:
            switch (iterOper)
            {
                case GT_SUB:
                    iterInc = -iterInc;
                    FALLTHROUGH;

                case GT_ADD:
                    if (constInitX >= constLimitX)
                    {
                        loopCount += (unsigned)((constLimitX - constInitX) / iterInc) + 1;
                    }

                    iterAtExitX = (int)(constInitX + iterInc * (int)loopCount);

                    if (unsTest)
                    {
                        iterAtExitX = (unsigned)iterAtExitX;
                    }

                    // Check if small types will underflow
                    if (optIterSmallUnderflow((int)iterAtExitX, iterOperType))
                    {
                        return false;
                    }

                    // iterator with 32bit underflow. Bad for TYP_INT and unsigneds
                    if (iterAtExitX >= constLimitX)
                    {
                        return false;
                    }

                    *iterCount = loopCount;
                    return true;

                case GT_MUL:
                case GT_DIV:
                case GT_RSH:
                case GT_LSH:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        default:
            noway_assert(!"Unknown operator for loop condition");
    }

    return false;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

//-----------------------------------------------------------------------------
// optUnrollLoops: Look for loop unrolling candidates and unroll them.
//
// Loops must be of the form:
//   for (i=icon; i<icon; i++) { ... }
//
// Loops handled are fully unrolled; there is no partial unrolling.
//
// Limitations: only the following loop types are handled:
// 1. "while" loops (top entry)
// 2. constant initializer, constant bound
// 3. The entire loop must be in the same EH region.
// 4. The loop iteration variable can't be address exposed.
// 5. The loop iteration variable can't be a promoted struct field.
// 6. We must be able to calculate the total constant iteration count.
// 7. On x86, there is a limit to the number of return blocks. So if there are return blocks in the loop that
//    would be unrolled, the unrolled code can't exceed that limit.
//
// Cost heuristics:
// 1. there are cost metrics for maximum number of allowed iterations, and maximum unroll size
// 2. single-iteration loops are always allowed (to eliminate the loop structure).
// 3. otherwise, only loops where the limit is Vector<T>.Length are currently allowed
//
// In stress modes, these heuristic limits are expanded, and loops aren't required to have the
// Vector<T>.Length limit.
//
// Loops are processed from innermost to outermost order, to attempt to unroll the most nested loops first.
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::optUnrollLoops()
{
    if (compCodeOpt() == SMALL_CODE)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (optLoopCount == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (JitConfig.JitNoUnroll())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    /* Look for loop unrolling candidates */

    bool change                 = false;
    bool anyIRchange            = false;
    bool anyNestedLoopsUnrolled = false;
    INDEBUG(int unrollCount = 0);    // count of loops unrolled
    INDEBUG(int unrollFailures = 0); // count of loops attempted to be unrolled, but failed

    static const unsigned ITER_LIMIT[COUNT_OPT_CODE + 1] = {
        10, // BLENDED_CODE
        0,  // SMALL_CODE
        20, // FAST_CODE
        0   // COUNT_OPT_CODE
    };

    assert(ITER_LIMIT[SMALL_CODE] == 0);
    assert(ITER_LIMIT[COUNT_OPT_CODE] == 0);

    unsigned iterLimit = ITER_LIMIT[compCodeOpt()];

#ifdef DEBUG
    if (compStressCompile(STRESS_UNROLL_LOOPS, 50))
    {
        iterLimit *= 10;
    }
#endif

    static const int UNROLL_LIMIT_SZ[COUNT_OPT_CODE + 1] = {
        300, // BLENDED_CODE
        0,   // SMALL_CODE
        600, // FAST_CODE
        0    // COUNT_OPT_CODE
    };

    assert(UNROLL_LIMIT_SZ[SMALL_CODE] == 0);
    assert(UNROLL_LIMIT_SZ[COUNT_OPT_CODE] == 0);

    // Visit loops from highest to lowest number to visit them in innermost to outermost order.
    for (unsigned lnum = optLoopCount - 1; lnum != ~0U; --lnum)
    {
        // This is necessary due to an apparent analysis limitation since
        // optLoopCount must be strictly greater than 0 upon entry and lnum
        // cannot wrap due to the loop termination condition.
        PREFAST_ASSUME(lnum != 0U - 1);

        LoopDsc&    loop = optLoopTable[lnum];
        BasicBlock* head;
        BasicBlock* top;
        BasicBlock* bottom;
        BasicBlock* initBlock;

        bool       dupCond;      // Does the 'head' block contain a duplicate loop condition (zero trip test)?
        int        lbeg;         // initial value for iterator
        int        llim;         // limit value for iterator
        unsigned   lvar;         // iterator lclVar #
        int        iterInc;      // value to increment the iterator
        genTreeOps iterOper;     // type of iterator increment (i.e. ADD, SUB, etc.)
        var_types  iterOperType; // type result of the oper (for overflow instrs)
        genTreeOps testOper;     // type of loop test (i.e. GT_LE, GT_GE, etc.)
        bool       unsTest;      // Is the comparison unsigned?

        unsigned loopRetCount; // number of BBJ_RETURN blocks in loop
        unsigned totalIter;    // total number of iterations in the constant loop

        const unsigned loopFlags = loop.lpFlags;

        // Check for required flags:
        // LPFLG_CONST_INIT  - required because this transform only handles full unrolls
        // LPFLG_CONST_LIMIT - required because this transform only handles full unrolls
        const unsigned requiredFlags = LPFLG_CONST_INIT | LPFLG_CONST_LIMIT;
        if ((loopFlags & requiredFlags) != requiredFlags)
        {
            // Don't print to the JitDump about this common case.
            continue;
        }

        // Ignore if removed or marked as not unrollable.
        if (loopFlags & (LPFLG_DONT_UNROLL | LPFLG_REMOVED))
        {
            // Don't print to the JitDump about this common case.
            continue;
        }

        // This transform only handles loops of this form
        if (!loop.lpIsTopEntry())
        {
            JITDUMP("Failed to unroll loop " FMT_LP ": not top entry\n", lnum);
            continue;
        }

        head = loop.lpHead;
        noway_assert(head != nullptr);
        top = loop.lpTop;
        noway_assert(top != nullptr);
        bottom = loop.lpBottom;
        noway_assert(bottom != nullptr);

        // Get the loop data:
        //  - initial constant
        //  - limit constant
        //  - iterator
        //  - iterator increment
        //  - increment operation type (i.e. ADD, SUB, etc...)
        //  - loop test type (i.e. GT_GE, GT_LT, etc...)

        initBlock = loop.lpInitBlock;
        lbeg      = loop.lpConstInit;
        llim      = loop.lpConstLimit();
        testOper  = loop.lpTestOper();

        lvar     = loop.lpIterVar();
        iterInc  = loop.lpIterConst();
        iterOper = loop.lpIterOper();

        iterOperType = loop.lpIterOperType();
        unsTest      = (loop.lpTestTree->gtFlags & GTF_UNSIGNED) != 0;

        if (lvaTable[lvar].IsAddressExposed())
        {
            // If the loop iteration variable is address-exposed then bail
            JITDUMP("Failed to unroll loop " FMT_LP ": V%02u is address exposed\n", lnum, lvar);
            continue;
        }
        if (lvaTable[lvar].lvIsStructField)
        {
            // If the loop iteration variable is a promoted field from a struct then bail
            JITDUMP("Failed to unroll loop " FMT_LP ": V%02u is a promoted struct field\n", lnum, lvar);
            continue;
        }

        // Locate/initialize the increment/test statements.
        Statement* initStmt = initBlock->lastStmt();
        noway_assert((initStmt != nullptr) && (initStmt->GetNextStmt() == nullptr));

        Statement* testStmt = bottom->lastStmt();
        noway_assert((testStmt != nullptr) && (testStmt->GetNextStmt() == nullptr));

        Statement* incrStmt = testStmt->GetPrevStmt();
        noway_assert(incrStmt != nullptr);

        if (initStmt->GetRootNode()->OperIs(GT_JTRUE))
        {
            // Must be a duplicated loop condition.

            dupCond  = true;
            initStmt = initStmt->GetPrevStmt();
            noway_assert(initStmt != nullptr);
        }
        else
        {
            dupCond = false;
        }

        // Find the number of iterations - the function returns false if not a constant number.

        if (!optComputeLoopRep(lbeg, llim, iterInc, iterOper, iterOperType, testOper, unsTest, dupCond, &totalIter))
        {
            JITDUMP("Failed to unroll loop " FMT_LP ": not a constant iteration count\n", lnum);
            continue;
        }

        // Forget it if there are too many repetitions or not a constant loop.

        if (totalIter > iterLimit)
        {
            JITDUMP("Failed to unroll loop " FMT_LP ": too many iterations (%d > %d) (heuristic)\n", lnum, totalIter,
                    iterLimit);
            continue;
        }

        int unrollLimitSz = UNROLL_LIMIT_SZ[compCodeOpt()];

        if (INDEBUG(compStressCompile(STRESS_UNROLL_LOOPS, 50) ||) false)
        {
            // In stress mode, quadruple the size limit, and drop
            // the restriction that loop limit must be vector element count.
            unrollLimitSz *= 4;
        }
        else if (totalIter <= 1)
        {
            // No limit for single iteration loops
            // If there is no iteration (totalIter == 0), we will remove the loop body entirely.
            unrollLimitSz = INT_MAX;
        }
        else if (totalIter <= opts.compJitUnrollLoopMaxIterationCount)
        {
            // We can unroll this
        }
        else if ((loopFlags & LPFLG_SIMD_LIMIT) != 0)
        {
            // We can unroll this
        }
        else
        {
            JITDUMP("Failed to unroll loop " FMT_LP ": insufficiently simple loop (heuristic)\n", lnum);
            continue;
        }

        GenTree* incr = incrStmt->GetRootNode();

        // Don't unroll loops we don't understand.
        if (incr->gtOper != GT_ASG)
        {
            JITDUMP("Failed to unroll loop " FMT_LP ": unknown increment op (%s)\n", lnum,
                    GenTree::OpName(incr->gtOper));
            continue;
        }
        incr = incr->AsOp()->gtOp2;

        GenTree* init = initStmt->GetRootNode();

        // Make sure everything looks ok.
        // clang-format off
        if ((init->gtOper != GT_ASG) ||
            (init->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
            (init->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() != lvar) ||
            (init->AsOp()->gtOp2->gtOper != GT_CNS_INT) ||
            (init->AsOp()->gtOp2->AsIntCon()->gtIconVal != lbeg) ||

            !((incr->gtOper == GT_ADD) || (incr->gtOper == GT_SUB)) ||
            (incr->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
            (incr->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() != lvar) ||
            (incr->AsOp()->gtOp2->gtOper != GT_CNS_INT) ||
            (incr->AsOp()->gtOp2->AsIntCon()->gtIconVal != iterInc) ||

            (testStmt->GetRootNode()->gtOper != GT_JTRUE))
        {
            noway_assert(!"Bad precondition in Compiler::optUnrollLoops()");
            continue;
        }
        // clang-format on

        // After this point, assume we've changed the IR. In particular, we call gtSetStmtInfo() which
        // can modify the IR. We may still fail to unroll if the EH region conditions don't hold, if
        // the size heuristics don't succeed, or if cloning any individual block fails.
        anyIRchange = true;

        // Heuristic: Estimated cost in code size of the unrolled loop.

        {
            ClrSafeInt<unsigned>    loopCostSz; // Cost is size of one iteration
            const BasicBlock* const top = loop.lpTop;

            // Besides calculating the loop cost, also ensure that all loop blocks are within the same EH
            // region, and count the number of BBJ_RETURN blocks in the loop.
            loopRetCount = 0;
            for (BasicBlock* const block : loop.LoopBlocks())
            {
                if (!BasicBlock::sameEHRegion(block, top))
                {
                    // Unrolling would require cloning EH regions
                    // Note that only non-funclet model (x86) could actually have a loop including a handler
                    // but not it's corresponding `try`, if its `try` was moved due to being marked "rare".
                    JITDUMP("Failed to unroll loop " FMT_LP ": EH constraint\n", lnum);
                    goto DONE_LOOP;
                }

                if (block->bbJumpKind == BBJ_RETURN)
                {
                    ++loopRetCount;
                }

                for (Statement* const stmt : block->Statements())
                {
                    gtSetStmtInfo(stmt);
                    loopCostSz += stmt->GetCostSz();
                }
            }

#ifdef JIT32_GCENCODER
            if ((totalIter > 0) && (fgReturnCount + loopRetCount * (totalIter - 1) > SET_EPILOGCNT_MAX))
            {
                // Jit32 GC encoder can't report more than SET_EPILOGCNT_MAX epilogs.
                JITDUMP("Failed to unroll loop " FMT_LP ": GC encoder max epilog constraint\n", lnum);
                goto DONE_LOOP;
            }
#endif // !JIT32_GCENCODER

            // Compute the estimated increase in code size for the unrolled loop.

            ClrSafeInt<unsigned> fixedLoopCostSz(8);

            ClrSafeInt<int> unrollCostSz = ClrSafeInt<int>(loopCostSz * ClrSafeInt<unsigned>(totalIter)) -
                                           ClrSafeInt<int>(loopCostSz + fixedLoopCostSz);

            // Don't unroll if too much code duplication would result.

            if (unrollCostSz.IsOverflow() || (unrollCostSz.Value() > unrollLimitSz))
            {
                JITDUMP("Failed to unroll loop " FMT_LP ": size constraint (%d > %d) (heuristic)\n", lnum,
                        unrollCostSz.Value(), unrollLimitSz);
                goto DONE_LOOP;
            }

            // Looks like a good idea to unroll this loop, let's do it!
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (verbose)
            {
                printf("\nUnrolling loop ");
                optPrintLoopInfo(&loop);
                printf(" over V%02u from %u to %u unrollCostSz = %d\n\n", lvar, lbeg, llim, unrollCostSz);
            }
#endif
        }

#if FEATURE_LOOP_ALIGN
        for (BasicBlock* const block : loop.LoopBlocks())
        {
            block->unmarkLoopAlign(this DEBUG_ARG("Unrolled loop"));
        }
#endif

        // Create the unrolled loop statement list.
        {
            // When unrolling a loop, that loop disappears (and will be removed from the loop table). Each unrolled
            // block will be set to exist within the parent loop, if any. However, if we unroll a loop that has
            // nested loops, we will create multiple copies of the nested loops. This requires adding new loop table
            // entries to represent the new loops. Instead of trying to do this incrementally, in the case where
            // nested loops exist (in any unrolled loop) we rebuild the entire loop table after unrolling.

            BlockToBlockMap        blockMap(getAllocator(CMK_LoopOpt));
            BasicBlock*            insertAfter                    = bottom;
            BasicBlock* const      tail                           = bottom->bbNext;
            BasicBlock::loopNumber newLoopNum                     = loop.lpParent;
            bool                   anyNestedLoopsUnrolledThisLoop = false;
            int                    lval;
            unsigned               iterToUnroll = totalIter; // The number of iterations left to unroll

            for (lval = lbeg; iterToUnroll > 0; iterToUnroll--)
            {
                // Note: we can't use the loop.LoopBlocks() iterator, as it captures loop.lpBottom->bbNext at the
                // beginning of iteration, and we insert blocks before that. So we need to evaluate lpBottom->bbNext
                // every iteration.
                for (BasicBlock* block = loop.lpTop; block != loop.lpBottom->bbNext; block = block->bbNext)
                {
                    BasicBlock* newBlock = insertAfter =
                        fgNewBBafter(block->bbJumpKind, insertAfter, /*extendRegion*/ true);
                    blockMap.Set(block, newBlock, BlockToBlockMap::Overwrite);

                    if (!BasicBlock::CloneBlockState(this, newBlock, block, lvar, lval))
                    {
                        // CloneBlockState (specifically, gtCloneExpr) doesn't handle everything. If it fails
                        // to clone a block in the loop, splice out and forget all the blocks we cloned so far:
                        // put the loop blocks back to how they were before we started cloning blocks,
                        // and abort unrolling the loop.
                        bottom->bbNext = tail;
                        tail->bbPrev   = bottom;
                        loop.lpFlags |= LPFLG_DONT_UNROLL; // Mark it so we don't try to unroll it again.
                        INDEBUG(++unrollFailures);
                        JITDUMP("Failed to unroll loop " FMT_LP ": block cloning failed on " FMT_BB "\n", lnum,
                                block->bbNum);
                        goto DONE_LOOP;
                    }

                    // All blocks in the unrolled loop will now be marked with the parent loop number. Note that
                    // if the loop being unrolled contains nested (child) loops, we will notice this below (when
                    // we set anyNestedLoopsUnrolledThisLoop), and that will cause us to rebuild the entire loop
                    // table and all loop annotations on blocks. However, if the loop contains no nested loops,
                    // setting the block `bbNatLoopNum` here is sufficient to incrementally update the block's
                    // loop info.

                    newBlock->bbNatLoopNum = newLoopNum;

                    // Block weight should no longer have the loop multiplier
                    //
                    // Note this is not quite right, as we may not have upscaled by this amount
                    // and we might not have upscaled at all, if we had profile data.
                    //
                    newBlock->scaleBBWeight(1.0 / BB_LOOP_WEIGHT_SCALE);

                    // Jump dests are set in a post-pass; make sure CloneBlockState hasn't tried to set them.
                    assert(newBlock->bbJumpDest == nullptr);

                    if (block == bottom)
                    {
                        // Remove the test; we're doing a full unroll.

                        Statement* testCopyStmt = newBlock->lastStmt();
                        GenTree*   testCopyExpr = testCopyStmt->GetRootNode();
                        assert(testCopyExpr->gtOper == GT_JTRUE);
                        GenTree* sideEffList = nullptr;
                        gtExtractSideEffList(testCopyExpr, &sideEffList, GTF_SIDE_EFFECT | GTF_ORDER_SIDEEFF);
                        if (sideEffList == nullptr)
                        {
                            fgRemoveStmt(newBlock, testCopyStmt);
                        }
                        else
                        {
                            testCopyStmt->SetRootNode(sideEffList);
                        }
                        newBlock->bbJumpKind = BBJ_NONE;
                    }
                }

                // Now redirect any branches within the newly-cloned iteration.
                // Don't include `bottom` in the iteration, since we've already changed the
                // newBlock->bbJumpKind, above.
                for (BasicBlock* block = loop.lpTop; block != loop.lpBottom; block = block->bbNext)
                {
                    BasicBlock* newBlock = blockMap[block];
                    optCopyBlkDest(block, newBlock);
                    optRedirectBlock(newBlock, &blockMap, RedirectBlockOption::AddToPredLists);
                }

                // We fall into this unroll iteration from the bottom block (first iteration)
                // or from the previous unroll clone of the bottom block (subsequent iterations).
                // After doing this, all the newly cloned blocks now have proper flow and pred lists.
                //
                BasicBlock* const clonedTop = blockMap[loop.lpTop];
                fgAddRefPred(clonedTop, clonedTop->bbPrev);

                /* update the new value for the unrolled iterator */

                switch (iterOper)
                {
                    case GT_ADD:
                        lval += iterInc;
                        break;

                    case GT_SUB:
                        lval -= iterInc;
                        break;

                    case GT_RSH:
                    case GT_LSH:
                        noway_assert(!"Unrolling not implemented for this loop iterator");
                        goto DONE_LOOP;

                    default:
                        noway_assert(!"Unknown operator for constant loop iterator");
                        goto DONE_LOOP;
                }
            }

            // If we get here, we successfully cloned all the blocks in the unrolled loop.
            // Note we may not have done any cloning at all, if the loop iteration count was zero.

            // Gut the old loop body.
            //
            for (BasicBlock* const block : loop.LoopBlocks())
            {
                // Check if the old loop body had any nested loops that got cloned. Note that we need to do this
                // here, and not in the loop above, to handle the special case where totalIter is zero, and the
                // above loop doesn't execute.
                if (block->bbNatLoopNum != lnum)
                {
                    anyNestedLoopsUnrolledThisLoop = true;
                }

                // Scrub all pred list references to block, except for bottom-> bottom->bbNext.
                //
                for (BasicBlock* succ : block->Succs(this))
                {
                    if ((block == bottom) && (succ == bottom->bbNext))
                    {
                        continue;
                    }

                    fgRemoveAllRefPreds(succ, block);
                }

                block->bbStmtList   = nullptr;
                block->bbJumpKind   = BBJ_NONE;
                block->bbJumpDest   = nullptr;
                block->bbNatLoopNum = newLoopNum;

                // Remove a few unnecessary flags (this list is not comprehensive).
                block->bbFlags &= ~(BBF_LOOP_HEAD | BBF_BACKWARD_JUMP_SOURCE | BBF_BACKWARD_JUMP_TARGET |
                                    BBF_HAS_IDX_LEN | BBF_HAS_MD_IDX_LEN | BBF_HAS_MDARRAYREF | BBF_HAS_NEWOBJ);

                JITDUMP("Scrubbed old loop body block " FMT_BB "\n", block->bbNum);
            }

            // The old loop blocks will form an empty linear chain.
            // Add back a suitable pred list links.
            //
            BasicBlock* oldLoopPred = head;
            for (BasicBlock* const block : loop.LoopBlocks())
            {
                if (block != top)
                {
                    fgAddRefPred(block, oldLoopPred);
                }
                oldLoopPred = block;
            }

            if (anyNestedLoopsUnrolledThisLoop)
            {
                anyNestedLoopsUnrolled = true;
            }

            // Now fix up the exterior flow and pred list entries.
            //
            // Control will fall through from the initBlock to its successor, which is either
            // the pre-header HEAD (if it exists), or the now empty TOP (if totalIter == 0),
            // or the first cloned top.
            //
            // If the initBlock is a BBJ_COND drop the condition (and make initBlock a BBJ_NONE block).
            //
            if (initBlock->bbJumpKind == BBJ_COND)
            {
                assert(dupCond);
                Statement* initBlockBranchStmt = initBlock->lastStmt();
                noway_assert(initBlockBranchStmt->GetRootNode()->OperIs(GT_JTRUE));
                fgRemoveStmt(initBlock, initBlockBranchStmt);
                fgRemoveRefPred(initBlock->bbJumpDest, initBlock);
                initBlock->bbJumpKind = BBJ_NONE;
            }
            else
            {
                /* the loop must execute */
                assert(!dupCond);
                assert(totalIter > 0);
                noway_assert(initBlock->bbJumpKind == BBJ_NONE);
            }

            // The loop will be removed, so no need to fix up the pre-header.
            if (loop.lpFlags & LPFLG_HAS_PREHEAD)
            {
                assert(head->bbFlags & BBF_LOOP_PREHEADER);

                // For unrolled loops, all the unrolling preconditions require the pre-header block to fall
                // through into TOP.
                assert(head->bbJumpKind == BBJ_NONE);
            }

            // If we actually unrolled, tail is now reached
            // by the last cloned bottom, and no longer
            // reached by bottom.
            //
            if (totalIter > 0)
            {
                fgAddRefPred(tail, blockMap[bottom]);
                fgRemoveRefPred(tail, bottom);
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("Whole unrolled loop:\n");

                gtDispTree(initStmt->GetRootNode());
                printf("\n");
                fgDumpTrees(top, insertAfter);

                if (anyNestedLoopsUnrolledThisLoop)
                {
                    printf("Unrolled loop " FMT_LP " contains nested loops\n", lnum);
                }
            }
#endif // DEBUG

            // Update loop table.
            optMarkLoopRemoved(lnum);

            // Note if we created new BBJ_RETURNs (or removed some).
            if (totalIter > 0)
            {
                fgReturnCount += loopRetCount * (totalIter - 1);
            }
            else
            {
                assert(totalIter == 0);
                assert(fgReturnCount >= loopRetCount);
                fgReturnCount -= loopRetCount;
            }

            // Remember that something has changed.
            INDEBUG(++unrollCount);
            change = true;
        }

    DONE_LOOP:;
    }

    if (change)
    {
        assert(anyIRchange);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nFinished unrolling %d loops", unrollCount);
            if (unrollFailures > 0)
            {
                printf(", %d failures due to block cloning", unrollFailures);
            }
            printf("\n");
            if (anyNestedLoopsUnrolled)
            {
                printf("At least one unrolled loop contains nested loops; recomputing loop table\n");
            }
        }
#endif // DEBUG

        // If we unrolled any nested loops, we rebuild the loop table (including recomputing the
        // return blocks list).
        //
        if (anyNestedLoopsUnrolled)
        {
            fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_DOMS | FlowGraphUpdates::COMPUTE_RETURNS |
                                     FlowGraphUpdates::COMPUTE_LOOPS);
        }
        else
        {
            fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_DOMS);
        }

        DBEXEC(verbose, fgDispBasicBlocks());
    }
    else
    {
#ifdef DEBUG
        assert(unrollCount == 0);
        assert(!anyNestedLoopsUnrolled);

        if (unrollFailures > 0)
        {
            printf("\nFinished loop unrolling, %d failures due to block cloning\n", unrollFailures);
        }
#endif // DEBUG
    }

#ifdef DEBUG
    fgDebugCheckBBlist(true);
#endif // DEBUG

    return anyIRchange ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Return false if there is a code path from 'topBB' to 'botBB' that might
 *  not execute a method call.
 */

bool Compiler::optReachWithoutCall(BasicBlock* topBB, BasicBlock* botBB)
{
    // TODO-Cleanup: Currently BBF_GC_SAFE_POINT is not set for helper calls,
    // as some helper calls are neither interruptible nor hijackable.
    // When we can determine this, then we can set BBF_GC_SAFE_POINT for
    // those helpers too.

    // We can always check topBB and botBB for any gc safe points and early out

    if ((topBB->bbFlags | botBB->bbFlags) & BBF_GC_SAFE_POINT)
    {
        return false;
    }

    // Otherwise we will need to rely upon the dominator sets

    if (!fgDomsComputed)
    {
        // return a conservative answer of true when we don't have the dominator sets
        return true;
    }

    noway_assert(topBB->bbNum <= botBB->bbNum);

    BasicBlock* curBB = topBB;
    for (;;)
    {
        noway_assert(curBB);

        // If we added a loop pre-header block then we will
        //  have a bbNum greater than fgLastBB, and we won't have
        //  any dominator information about this block, so skip it.
        //
        if (curBB->bbNum <= fgLastBB->bbNum)
        {
            noway_assert(curBB->bbNum <= botBB->bbNum);

            // Does this block contain a gc safe point?

            if (curBB->bbFlags & BBF_GC_SAFE_POINT)
            {
                // Will this block always execute on the way to botBB ?
                //
                // Since we are checking every block in [topBB .. botBB] and we are using
                // a lexical definition of a loop.
                //  (all that we know is that is that botBB is a back-edge to topBB)
                // Thus while walking blocks in this range we may encounter some blocks
                // that are not really part of the loop, and so we need to perform
                // some additional checks:
                //
                // We will check that the current 'curBB' is reachable from 'topBB'
                // and that it dominates the block containing the back-edge 'botBB'
                // When both of these are true then we know that the gcsafe point in 'curBB'
                // will be encountered in the loop and we can return false
                //
                if (fgDominate(curBB, botBB) && fgReachable(topBB, curBB))
                {
                    return false;
                }
            }
            else
            {
                // If we've reached the destination block, then we're done

                if (curBB == botBB)
                {
                    break;
                }
            }
        }

        curBB = curBB->bbNext;
    }

    // If we didn't find any blocks that contained a gc safe point and
    // also met the fgDominate and fgReachable criteria then we must return true
    //
    return true;
}

Compiler::OptInvertCountTreeInfoType Compiler::optInvertCountTreeInfo(GenTree* tree)
{
    class CountTreeInfoVisitor : public GenTreeVisitor<CountTreeInfoVisitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Compiler::OptInvertCountTreeInfoType Result = {};

        CountTreeInfoVisitor(Compiler* comp) : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if (Compiler::IsSharedStaticHelper(*use))
            {
                Result.sharedStaticHelperCount++;
            }

            if ((*use)->OperIsArrLength())
            {
                Result.arrayLengthCount++;
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    CountTreeInfoVisitor walker(this);
    walker.WalkTree(&tree, nullptr);
    return walker.Result;
}

//-----------------------------------------------------------------------------
// optInvertWhileLoop: modify flow and duplicate code so that for/while loops are
//   entered at top and tested at bottom (aka loop rotation or bottom testing).
//   Creates a "zero trip test" condition which guards entry to the loop.
//   Enables loop invariant hoisting and loop cloning, which depend on
//   `do {} while` format loops. Enables creation of a pre-header block after the
//   zero trip test to place code that only runs if the loop is guaranteed to
//   run at least once.
//
// Arguments:
//   block -- block that may be the predecessor of the un-rotated loop's test block.
//
// Returns:
//   true if any IR changes possibly made (used to determine phase return status)
//
// Notes:
//   Uses a simple lexical screen to detect likely loops.
//
//   Specifically, we're looking for the following case:
//
//          ...
//          jmp test                // `block` argument
//   loop:
//          ...
//          ...
//   test:
//          ..stmts..
//          cond
//          jtrue loop
//
//   If we find this, and the condition is simple enough, we change
//   the loop to the following:
//
//          ...
//          ..stmts..               // duplicated cond block statements
//          cond                    // duplicated cond
//          jfalse done
//          // else fall-through
//   loop:
//          ...
//          ...
//   test:
//          ..stmts..
//          cond
//          jtrue loop
//   done:
//
//  Makes no changes if the flow pattern match fails.
//
//  May not modify a loop if profile is unfavorable, if the cost of duplicating
//  code is large (factoring in potential CSEs).
//
bool Compiler::optInvertWhileLoop(BasicBlock* block)
{
    assert(opts.OptimizationEnabled());
    assert(compCodeOpt() != SMALL_CODE);

    // Does the BB end with an unconditional jump?

    if (block->bbJumpKind != BBJ_ALWAYS || (block->bbFlags & BBF_KEEP_BBJ_ALWAYS))
    {
        // It can't be one of the ones we use for our exception magic
        return false;
    }

    // Get hold of the jump target
    BasicBlock* const bTest = block->bbJumpDest;

    // Does the bTest consist of 'jtrue(cond) block' ?
    if (bTest->bbJumpKind != BBJ_COND)
    {
        return false;
    }

    // bTest must be a backwards jump to block->bbNext
    // This will be the top of the loop.
    //
    BasicBlock* const bTop = bTest->bbJumpDest;

    if (bTop != block->bbNext)
    {
        return false;
    }

    // Since bTest is a BBJ_COND it will have a bbNext
    //
    BasicBlock* const bJoin = bTest->bbNext;
    noway_assert(bJoin != nullptr);

    // 'block' must be in the same try region as the condition, since we're going to insert a duplicated condition
    // in a new block after 'block', and the condition might include exception throwing code.
    // On non-funclet platforms (x86), the catch exit is a BBJ_ALWAYS, but we don't want that to
    // be considered as the head of a loop, so also disallow different handler regions.
    if (!BasicBlock::sameEHRegion(block, bTest))
    {
        return false;
    }

    // The duplicated condition block will branch to bTest->bbNext, so that also better be in the
    // same try region (or no try region) to avoid generating illegal flow.
    if (bJoin->hasTryIndex() && !BasicBlock::sameTryRegion(block, bJoin))
    {
        return false;
    }

    // It has to be a forward jump. Defer this check until after all the cheap checks
    // are done, since it iterates forward in the block list looking for bbJumpDest.
    //  TODO-CQ: Check if we can also optimize the backwards jump as well.
    //
    if (!fgIsForwardBranch(block))
    {
        return false;
    }

    // Find the loop termination test at the bottom of the loop.
    Statement* const condStmt = bTest->lastStmt();

    // Verify the test block ends with a conditional that we can manipulate.
    GenTree* const condTree = condStmt->GetRootNode();
    noway_assert(condTree->gtOper == GT_JTRUE);
    if (!condTree->AsOp()->gtOp1->OperIsCompare())
    {
        return false;
    }

    JITDUMP("Matched flow pattern for loop inversion: block " FMT_BB " bTop " FMT_BB " bTest " FMT_BB "\n",
            block->bbNum, bTop->bbNum, bTest->bbNum);

    // Estimate the cost of cloning the entire test block.
    //
    // Note: it would help throughput to compute the maximum cost
    // first and early out for large bTest blocks, as we are doing two
    // tree walks per tree. But because of this helper call scan, the
    // maximum cost depends on the trees in the block.
    //
    // We might consider flagging blocks with hoistable helper calls
    // during importation, so we can avoid the helper search and
    // implement an early bail out for large blocks with no helper calls.
    //
    // Note that gtPrepareCost can cause operand swapping, so we must
    // return `true` (possible IR change) from here on.

    unsigned estDupCostSz = 0;

    for (Statement* const stmt : bTest->Statements())
    {
        GenTree* tree = stmt->GetRootNode();
        gtPrepareCost(tree);
        estDupCostSz += tree->GetCostSz();
    }

    weight_t       loopIterations            = BB_LOOP_WEIGHT_SCALE;
    bool           allProfileWeightsAreValid = false;
    weight_t const weightBlock               = block->bbWeight;
    weight_t const weightTest                = bTest->bbWeight;
    weight_t const weightTop                 = bTop->bbWeight;

    // If we have profile data then we calculate the number of times
    // the loop will iterate into loopIterations
    if (fgIsUsingProfileWeights())
    {
        // Only rely upon the profile weight when all three of these blocks
        // have good profile weights
        if (block->hasProfileWeight() && bTest->hasProfileWeight() && bTop->hasProfileWeight())
        {
            // If this while loop never iterates then don't bother transforming
            //
            if (weightTop == BB_ZERO_WEIGHT)
            {
                return true;
            }

            // We generally expect weightTest > weightTop
            //
            // Tolerate small inconsistencies...
            //
            if (!fgProfileWeightsConsistent(weightBlock + weightTop, weightTest))
            {
                JITDUMP("Profile weights locally inconsistent: block " FMT_WT ", next " FMT_WT ", test " FMT_WT "\n",
                        weightBlock, weightTop, weightTest);
            }
            else
            {
                allProfileWeightsAreValid = true;

                // Determine average iteration count
                //
                //   weightTop is the number of time this loop executes
                //   weightTest is the number of times that we consider entering or remaining in the loop
                //   loopIterations is the average number of times that this loop iterates
                //
                weight_t loopEntries = weightTest - weightTop;

                // If profile is inaccurate, try and use other data to provide a credible estimate.
                // The value should at least be >= weightBlock.
                //
                if (loopEntries < weightBlock)
                {
                    loopEntries = weightBlock;
                }

                loopIterations = weightTop / loopEntries;
            }
        }
        else
        {
            JITDUMP("Missing profile data for loop!\n");
        }
    }

    unsigned maxDupCostSz = 34;

    if ((compCodeOpt() == FAST_CODE) || compStressCompile(STRESS_DO_WHILE_LOOPS, 30))
    {
        maxDupCostSz *= 4;
    }

    // If this loop iterates a lot then raise the maxDupCost
    if (loopIterations >= 12.0)
    {
        maxDupCostSz *= 2;
        if (loopIterations >= 96.0)
        {
            maxDupCostSz *= 2;
        }
    }

    // If the compare has too high cost then we don't want to dup.

    bool costIsTooHigh = (estDupCostSz > maxDupCostSz);

    OptInvertCountTreeInfoType optInvertTotalInfo = {};
    if (costIsTooHigh)
    {
        // If we already know that the cost is acceptable, then don't waste time walking the tree
        // counting things to boost the maximum allowed cost.
        //
        // If the loop condition has a shared static helper, we really want this loop converted
        // as not converting the loop will disable loop hoisting, meaning the shared helper will
        // be executed on every loop iteration.
        //
        // If the condition has array.Length operations, also boost, as they are likely to be CSE'd.

        for (Statement* const stmt : bTest->Statements())
        {
            GenTree* tree = stmt->GetRootNode();

            OptInvertCountTreeInfoType optInvertInfo = optInvertCountTreeInfo(tree);
            optInvertTotalInfo.sharedStaticHelperCount += optInvertInfo.sharedStaticHelperCount;
            optInvertTotalInfo.arrayLengthCount += optInvertInfo.arrayLengthCount;

            if ((optInvertInfo.sharedStaticHelperCount > 0) || (optInvertInfo.arrayLengthCount > 0))
            {
                // Calculate a new maximum cost. We might be able to early exit.

                unsigned newMaxDupCostSz =
                    maxDupCostSz + 24 * min(optInvertTotalInfo.sharedStaticHelperCount, (int)(loopIterations + 1.5)) +
                    8 * optInvertTotalInfo.arrayLengthCount;

                // Is the cost too high now?
                costIsTooHigh = (estDupCostSz > newMaxDupCostSz);
                if (!costIsTooHigh)
                {
                    // No need counting any more trees; we're going to do the transformation.
                    JITDUMP("Decided to duplicate loop condition block after counting helpers in tree [%06u] in "
                            "block " FMT_BB,
                            dspTreeID(tree), bTest->bbNum);
                    maxDupCostSz = newMaxDupCostSz; // for the JitDump output below
                    break;
                }
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        // Note that `optInvertTotalInfo.sharedStaticHelperCount = 0` means either there were zero helpers, or the
        // tree walk to count them was not done.
        printf(
            "\nDuplication of loop condition [%06u] is %s, because the cost of duplication (%i) is %s than %i,"
            "\n   loopIterations = %7.3f, optInvertTotalInfo.sharedStaticHelperCount >= %d, validProfileWeights = %s\n",
            dspTreeID(condTree), costIsTooHigh ? "not done" : "performed", estDupCostSz,
            costIsTooHigh ? "greater" : "less or equal", maxDupCostSz, loopIterations,
            optInvertTotalInfo.sharedStaticHelperCount, dspBool(allProfileWeightsAreValid));
    }
#endif

    if (costIsTooHigh)
    {
        return true;
    }

    bool foundCondTree = false;

    // Create a new block after `block` to put the copied condition code.
    block->bbJumpKind    = BBJ_NONE;
    block->bbJumpDest    = nullptr;
    BasicBlock* bNewCond = fgNewBBafter(BBJ_COND, block, /*extendRegion*/ true);

    // Clone each statement in bTest and append to bNewCond.
    for (Statement* const stmt : bTest->Statements())
    {
        GenTree* originalTree = stmt->GetRootNode();
        GenTree* clonedTree   = gtCloneExpr(originalTree);

        // Special case handling needed for the conditional jump tree
        if (originalTree == condTree)
        {
            foundCondTree = true;

            // Get the compare subtrees
            GenTree* originalCompareTree = originalTree->AsOp()->gtOp1;
            GenTree* clonedCompareTree   = clonedTree->AsOp()->gtOp1;
            assert(originalCompareTree->OperIsCompare());
            assert(clonedCompareTree->OperIsCompare());

            // Flag compare and cloned copy so later we know this loop
            // has a proper zero trip test.
            originalCompareTree->gtFlags |= GTF_RELOP_ZTT;
            clonedCompareTree->gtFlags |= GTF_RELOP_ZTT;

            // The original test branches to remain in the loop.  The
            // new cloned test will branch to avoid the loop.  So the
            // cloned compare needs to reverse the branch condition.
            gtReverseCond(clonedCompareTree);
        }

        Statement* clonedStmt = fgNewStmtAtEnd(bNewCond, clonedTree);

        if (opts.compDbgInfo)
        {
            clonedStmt->SetDebugInfo(stmt->GetDebugInfo());
        }
    }

    assert(foundCondTree);

    // Flag the block that received the copy as potentially having various constructs.
    bNewCond->bbFlags |= bTest->bbFlags & BBF_COPY_PROPAGATE;

    // Fix flow and profile
    //
    bNewCond->bbJumpDest = bJoin;
    bNewCond->inheritWeight(block);

    if (allProfileWeightsAreValid)
    {
        weight_t const delta = weightTest - weightTop;

        // If there is just one outside edge incident on bTest, then ideally delta == block->bbWeight.
        // But this might not be the case if profile data is inconsistent.
        //
        // And if bTest has multiple outside edges we want to account for the weight of them all.
        //
        if (delta > block->bbWeight)
        {
            bNewCond->setBBProfileWeight(delta);
        }
    }

    // Update pred info
    //
    fgAddRefPred(bJoin, bNewCond);
    fgAddRefPred(bTop, bNewCond);

    fgAddRefPred(bNewCond, block);
    fgRemoveRefPred(bTest, block);

    // Move all predecessor edges that look like loop entry edges to point to the new cloned condition
    // block, not the existing condition block. The idea is that if we only move `block` to point to
    // `bNewCond`, but leave other `bTest` predecessors still pointing to `bTest`, when we eventually
    // recognize loops, the loop will appear to have multiple entries, which will prevent optimization.
    // We don't have loops yet, but blocks should be in increasing lexical numbered order, so use that
    // as the proxy for predecessors that are "in" versus "out" of the potential loop. Note that correctness
    // is maintained no matter which condition block we point to, but we'll lose optimization potential
    // (and create spaghetti code) if we get it wrong.
    //
    BlockToBlockMap blockMap(getAllocator(CMK_LoopOpt));
    bool            blockMapInitialized = false;

    unsigned const loopFirstNum  = bTop->bbNum;
    unsigned const loopBottomNum = bTest->bbNum;
    for (BasicBlock* const predBlock : bTest->PredBlocks())
    {
        unsigned const bNum = predBlock->bbNum;
        if ((loopFirstNum <= bNum) && (bNum <= loopBottomNum))
        {
            // Looks like the predecessor is from within the potential loop; skip it.
            continue;
        }

        if (!blockMapInitialized)
        {
            blockMapInitialized = true;
            blockMap.Set(bTest, bNewCond);
        }

        // Redirect the predecessor to the new block.
        JITDUMP("Redirecting non-loop " FMT_BB " -> " FMT_BB " to " FMT_BB " -> " FMT_BB "\n", predBlock->bbNum,
                bTest->bbNum, predBlock->bbNum, bNewCond->bbNum);
        optRedirectBlock(predBlock, &blockMap, RedirectBlockOption::UpdatePredLists);
    }

    // If we have profile data for all blocks and we know that we are cloning the
    // `bTest` block into `bNewCond` and thus changing the control flow from `block` so
    // that it no longer goes directly to `bTest` anymore, we have to adjust
    // various weights.
    //
    if (allProfileWeightsAreValid)
    {
        // Update the weight for bTest. Normally, this reduces the weight of the bTest, except in odd
        // cases of stress modes with inconsistent weights.
        //
        JITDUMP("Reducing profile weight of " FMT_BB " from " FMT_WT " to " FMT_WT "\n", bTest->bbNum, weightTest,
                weightTop);
        bTest->inheritWeight(bTop);

        // Determine the new edge weights.
        //
        // We project the next/jump ratio for block and bTest by using
        // the original likelihoods out of bTest.
        //
        // Note "next" is the loop top block, not bTest's bbNext,
        // we'll call this latter block "after".
        //
        weight_t const testToNextLikelihood  = min(1.0, weightTop / weightTest);
        weight_t const testToAfterLikelihood = 1.0 - testToNextLikelihood;

        // Adjust edges out of bTest (which now has weight weightTop)
        //
        weight_t const testToNextWeight  = weightTop * testToNextLikelihood;
        weight_t const testToAfterWeight = weightTop * testToAfterLikelihood;

        FlowEdge* const edgeTestToNext  = fgGetPredForBlock(bTop, bTest);
        FlowEdge* const edgeTestToAfter = fgGetPredForBlock(bTest->bbNext, bTest);

        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (iterate loop)\n", bTest->bbNum, bTop->bbNum,
                testToNextWeight);
        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (exit loop)\n", bTest->bbNum,
                bTest->bbNext->bbNum, testToAfterWeight);

        edgeTestToNext->setEdgeWeights(testToNextWeight, testToNextWeight, bTop);
        edgeTestToAfter->setEdgeWeights(testToAfterWeight, testToAfterWeight, bTest->bbNext);

        // Adjust edges out of block, using the same distribution.
        //
        JITDUMP("Profile weight of " FMT_BB " remains unchanged at " FMT_WT "\n", block->bbNum, weightBlock);

        weight_t const blockToNextLikelihood  = testToNextLikelihood;
        weight_t const blockToAfterLikelihood = testToAfterLikelihood;

        weight_t const blockToNextWeight  = weightBlock * blockToNextLikelihood;
        weight_t const blockToAfterWeight = weightBlock * blockToAfterLikelihood;

        FlowEdge* const edgeBlockToNext  = fgGetPredForBlock(bNewCond->bbNext, bNewCond);
        FlowEdge* const edgeBlockToAfter = fgGetPredForBlock(bNewCond->bbJumpDest, bNewCond);

        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (enter loop)\n", bNewCond->bbNum,
                bNewCond->bbNext->bbNum, blockToNextWeight);
        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (avoid loop)\n", bNewCond->bbNum,
                bNewCond->bbJumpDest->bbNum, blockToAfterWeight);

        edgeBlockToNext->setEdgeWeights(blockToNextWeight, blockToNextWeight, bNewCond->bbNext);
        edgeBlockToAfter->setEdgeWeights(blockToAfterWeight, blockToAfterWeight, bNewCond->bbJumpDest);

#ifdef DEBUG
        // If we're checkig profile data, see if profile for the two target blocks is consistent.
        //
        if ((activePhaseChecks & PhaseChecks::CHECK_PROFILE) == PhaseChecks::CHECK_PROFILE)
        {
            const ProfileChecks checks        = (ProfileChecks)JitConfig.JitProfileChecks();
            const bool          nextProfileOk = fgDebugCheckIncomingProfileData(bNewCond->bbNext, checks);
            const bool          jumpProfileOk = fgDebugCheckIncomingProfileData(bNewCond->bbJumpDest, checks);

            if (hasFlag(checks, ProfileChecks::RAISE_ASSERT))
            {
                assert(nextProfileOk);
                assert(jumpProfileOk);
            }
        }
#endif // DEBUG
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDuplicated loop exit block at " FMT_BB " for loop (" FMT_BB " - " FMT_BB ")\n", bNewCond->bbNum,
               bNewCond->bbNext->bbNum, bTest->bbNum);
        printf("Estimated code size expansion is %d\n", estDupCostSz);

        fgDumpBlock(bNewCond);
        fgDumpBlock(bTest);
    }
#endif // DEBUG

    return true;
}

//-----------------------------------------------------------------------------
// optInvertLoops: invert while loops in the method
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::optInvertLoops()
{
    noway_assert(opts.OptimizationEnabled());
    noway_assert(fgModified == false);

#if defined(OPT_CONFIG)
    if (!JitConfig.JitDoLoopInversion())
    {
        JITDUMP("Loop inversion disabled\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif // OPT_CONFIG

    bool madeChanges = fgRenumberBlocks();

    if (compCodeOpt() == SMALL_CODE)
    {
        // do not invert any loops
    }
    else
    {
        for (BasicBlock* const block : Blocks())
        {
            // Make sure the appropriate fields are initialized
            //
            if (block->bbWeight == BB_ZERO_WEIGHT)
            {
                // Zero weighted block can't have a LOOP_HEAD flag
                noway_assert(block->isLoopHead() == false);
                continue;
            }

            if (optInvertWhileLoop(block))
            {
                madeChanges = true;
            }
        }
    }

    if (fgModified)
    {
        // Reset fgModified here as we've done a consistent set of edits.
        //
        fgModified = false;
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//-----------------------------------------------------------------------------
// optOptimizeFlow: simplify flow graph
//
// Returns:
//   suitable phase status
//
// Notes:
//   Does not do profile-based reordering to try and ensure that
//   that we recognize and represent as many loops as possible.
//
PhaseStatus Compiler::optOptimizeFlow()
{
    noway_assert(opts.OptimizationEnabled());
    noway_assert(fgModified == false);

    bool madeChanges = false;

    madeChanges |= fgUpdateFlowGraph(/* allowTailDuplication */ true);
    madeChanges |= fgReorderBlocks(/* useProfileData */ false);
    madeChanges |= fgUpdateFlowGraph();

    // fgReorderBlocks can cause IR changes even if it does not modify
    // the flow graph. It calls gtPrepareCost which can cause operand swapping.
    // Work around this for now.
    //
    // Note phase status only impacts dumping and checking done post-phase,
    // it has no impact on a release build.
    //
    madeChanges = true;

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//-----------------------------------------------------------------------------
// optOptimizeLayout: reorder blocks to reduce cost of control flow
//
// Returns:
//   suitable phase status
//
// Notes:
//   Reorders using profile data, if available.
//
PhaseStatus Compiler::optOptimizeLayout()
{
    noway_assert(opts.OptimizationEnabled());

    bool madeChanges = false;

    madeChanges |= fgUpdateFlowGraph(/* allowTailDuplication */ false);
    madeChanges |= fgReorderBlocks(/* useProfile */ true);
    madeChanges |= fgUpdateFlowGraph();

    // fgReorderBlocks can cause IR changes even if it does not modify
    // the flow graph. It calls gtPrepareCost which can cause operand swapping.
    // Work around this for now.
    //
    // Note phase status only impacts dumping and checking done post-phase,
    // it has no impact on a release build.
    //
    madeChanges = true;

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// optMarkLoopHeads: Mark all potential loop heads as BBF_LOOP_HEAD. A potential loop head is a block
// targeted by a lexical back edge, where the source of the back edge is reachable from the block.
// Note that if there are no lexical back edges, there can't be any loops.
//
// If there are any potential loop heads, set `fgHasLoops` to `true`.
//
// Assumptions:
//    The reachability sets must be computed and valid.
//
void Compiler::optMarkLoopHeads()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optMarkLoopHeads()\n");
    }

    assert(fgReachabilitySetsValid);
    fgDebugCheckBBNumIncreasing();

    int loopHeadsMarked = 0;
#endif

    bool hasLoops = false;

    for (BasicBlock* const block : Blocks())
    {
        // Set BBF_LOOP_HEAD if we have backwards branches to this block.

        unsigned blockNum = block->bbNum;
        for (BasicBlock* const predBlock : block->PredBlocks())
        {
            if (blockNum <= predBlock->bbNum)
            {
                if (predBlock->bbJumpKind == BBJ_CALLFINALLY)
                {
                    // Loops never have BBJ_CALLFINALLY as the source of their "back edge".
                    continue;
                }

                // If block can reach predBlock then we have a loop head
                if (BlockSetOps::IsMember(this, predBlock->bbReach, blockNum))
                {
                    hasLoops = true;
                    block->bbFlags |= BBF_LOOP_HEAD;
                    INDEBUG(++loopHeadsMarked);
                    break; // No need to look at more `block` predecessors
                }
            }
        }
    }

    JITDUMP("%d loop heads marked\n", loopHeadsMarked);
    fgHasLoops = hasLoops;
}

//-----------------------------------------------------------------------------
// optResetLoopInfo: reset all loop info in preparation for rebuilding the loop table, or preventing
// future phases from accessing loop-related data.
//
void Compiler::optResetLoopInfo()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optResetLoopInfo()\n");
    }
#endif

    optLoopCount        = 0; // This will force the table to be rebuilt
    loopAlignCandidates = 0;

    // This will cause users to crash if they use the table when it is considered empty.
    // TODO: the loop table is always allocated as the same (maximum) size, so this is wasteful.
    // We could zero it out (possibly only in DEBUG) to be paranoid, but there's no reason to
    // force it to be re-allocated.
    optLoopTable              = nullptr;
    optLoopTableValid         = false;
    optLoopsRequirePreHeaders = false;

    for (BasicBlock* const block : Blocks())
    {
        // If the block weight didn't come from profile data, reset it so it can be calculated again.
        if (!block->hasProfileWeight())
        {
            block->bbWeight = BB_UNITY_WEIGHT;
            block->bbFlags &= ~BBF_RUN_RARELY;
        }

        block->bbFlags &= ~BBF_LOOP_FLAGS;
        block->bbNatLoopNum = BasicBlock::NOT_IN_LOOP;
    }
}

//-----------------------------------------------------------------------------
// optFindAndScaleGeneralLoopBlocks: scale block weights based on loop nesting depth.
// Note that this uses a very general notion of "loop": any block targeted by a reachable
// back-edge is considered a loop.
//
void Compiler::optFindAndScaleGeneralLoopBlocks()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optFindAndScaleGeneralLoopBlocks()\n");
    }
#endif

    // This code depends on block number ordering.
    INDEBUG(fgDebugCheckBBNumIncreasing());

    unsigned generalLoopCount = 0;

    // We will use the following terminology:
    // top        - the first basic block in the loop (i.e. the head of the backward edge)
    // bottom     - the last block in the loop (i.e. the block from which we jump to the top)
    // lastBottom - used when we have multiple back edges to the same top

    for (BasicBlock* const top : Blocks())
    {
        // Only consider `top` blocks already determined to be potential loop heads.
        if (!top->isLoopHead())
        {
            continue;
        }

        BasicBlock* foundBottom = nullptr;

        for (BasicBlock* const bottom : top->PredBlocks())
        {
            // Is this a loop candidate? - We look for "back edges"

            // Is this a backward edge? (from BOTTOM to TOP)
            if (top->bbNum > bottom->bbNum)
            {
                continue;
            }

            // We only consider back-edges that are BBJ_COND or BBJ_ALWAYS for loops.
            if ((bottom->bbJumpKind != BBJ_COND) && (bottom->bbJumpKind != BBJ_ALWAYS))
            {
                continue;
            }

            /* the top block must be able to reach the bottom block */
            if (!fgReachable(top, bottom))
            {
                continue;
            }

            /* Found a new loop, record the longest backedge in foundBottom */

            if ((foundBottom == nullptr) || (bottom->bbNum > foundBottom->bbNum))
            {
                foundBottom = bottom;
            }
        }

        if (foundBottom)
        {
            generalLoopCount++;

            /* Mark all blocks between 'top' and 'bottom' */

            optScaleLoopBlocks(top, foundBottom);
        }

        // We track at most 255 loops
        if (generalLoopCount == 255)
        {
#if COUNT_LOOPS
            totalUnnatLoopOverflows++;
#endif
            break;
        }
    }

    JITDUMP("\nFound a total of %d general loops.\n", generalLoopCount);

#if COUNT_LOOPS
    totalUnnatLoopCount += generalLoopCount;
#endif
}

//-----------------------------------------------------------------------------
// optFindLoops: find loops in the function.
//
// The JIT recognizes two types of loops in a function: natural loops and "general" (or "unnatural") loops.
// Natural loops are those which get added to the loop table. Most downstream optimizations require
// using natural loops. See `optFindNaturalLoops` for a definition of the criteria for recognizing a natural loop.
// A general loop is defined as a lexical (program order) range of blocks where a later block branches to an
// earlier block (that is, there is a back edge in the flow graph), and the later block is reachable from the earlier
// block. General loops are used for weighting flow graph blocks (when there is no block profile data), as well as
// for determining if we require fully interruptible GC information.
//
// Notes:
//  Also (re)sets all non-IBC block weights, and marks loops potentially needing alignment padding.
//
void Compiler::optFindLoops()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optFindLoops()\n");
    }
#endif

    noway_assert(opts.OptimizationEnabled());
    assert(fgDomsComputed);

    optMarkLoopHeads();

    // Were there any potential loops in the flow graph?

    if (fgHasLoops)
    {
        optFindNaturalLoops();
        optFindAndScaleGeneralLoopBlocks();
        optIdentifyLoopsForAlignment(); // Check if any of the loops need alignment
    }

    optLoopTableValid = true;
}

//-----------------------------------------------------------------------------
// optFindLoopsPhase: The wrapper function for the "find loops" phase.
//
PhaseStatus Compiler::optFindLoopsPhase()
{
    optFindLoops();
    return PhaseStatus::MODIFIED_EVERYTHING;
}

/*****************************************************************************
 *
 *  Determine the kind of interference for the call.
 */

/* static */ inline Compiler::callInterf Compiler::optCallInterf(GenTreeCall* call)
{
    // if not a helper, kills everything
    if (call->gtCallType != CT_HELPER)
    {
        return CALLINT_ALL;
    }

    // setfield and array address store kill all indirections
    switch (eeGetHelperNum(call->gtCallMethHnd))
    {
        case CORINFO_HELP_ASSIGN_REF:         // Not strictly needed as we don't make a GT_CALL with this
        case CORINFO_HELP_CHECKED_ASSIGN_REF: // Not strictly needed as we don't make a GT_CALL with this
        case CORINFO_HELP_ASSIGN_BYREF:       // Not strictly needed as we don't make a GT_CALL with this
        case CORINFO_HELP_SETFIELDOBJ:
        case CORINFO_HELP_ARRADDR_ST:

            return CALLINT_REF_INDIRS;

        case CORINFO_HELP_SETFIELDFLOAT:
        case CORINFO_HELP_SETFIELDDOUBLE:
        case CORINFO_HELP_SETFIELD8:
        case CORINFO_HELP_SETFIELD16:
        case CORINFO_HELP_SETFIELD32:
        case CORINFO_HELP_SETFIELD64:

            return CALLINT_SCL_INDIRS;

        case CORINFO_HELP_ASSIGN_STRUCT: // Not strictly needed as we don't use this
        case CORINFO_HELP_MEMSET:        // Not strictly needed as we don't make a GT_CALL with this
        case CORINFO_HELP_MEMCPY:        // Not strictly needed as we don't make a GT_CALL with this
        case CORINFO_HELP_SETFIELDSTRUCT:

            return CALLINT_ALL_INDIRS;

        default:
            break;
    }

    // other helpers kill nothing
    return CALLINT_NONE;
}

/*****************************************************************************
 *
 *  See if the given tree can be computed in the given precision (which must
 *  be smaller than the type of the tree for this to make sense). If 'doit'
 *  is false, we merely check to see whether narrowing is possible; if we
 *  get called with 'doit' being true, we actually perform the narrowing.
 */

bool Compiler::optNarrowTree(GenTree* tree, var_types srct, var_types dstt, ValueNumPair vnpNarrow, bool doit)
{
    genTreeOps oper;
    unsigned   kind;

    noway_assert(tree);
    noway_assert(genActualType(tree->gtType) == genActualType(srct));

    /* Assume we're only handling integer types */
    noway_assert(varTypeIsIntegral(srct));
    noway_assert(varTypeIsIntegral(dstt));

    unsigned srcSize = genTypeSize(srct);
    unsigned dstSize = genTypeSize(dstt);

    /* dstt must be smaller than srct to narrow */
    if (dstSize >= srcSize)
    {
        return false;
    }

    /* Figure out what kind of a node we have */
    oper = tree->OperGet();
    kind = tree->OperKind();

    if (oper == GT_ASG)
    {
        noway_assert(doit == false);
        return false;
    }

    ValueNumPair NoVNPair = ValueNumPair();

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
            /* Constants can usually be narrowed by changing their value */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef TARGET_64BIT
            __int64 lval;
            __int64 lmask;

            case GT_CNS_LNG:
                lval  = tree->AsIntConCommon()->LngValue();
                lmask = 0;

                switch (dstt)
                {
                    case TYP_BYTE:
                        lmask = 0x0000007F;
                        break;
                    case TYP_BOOL:
                    case TYP_UBYTE:
                        lmask = 0x000000FF;
                        break;
                    case TYP_SHORT:
                        lmask = 0x00007FFF;
                        break;
                    case TYP_USHORT:
                        lmask = 0x0000FFFF;
                        break;
                    case TYP_INT:
                        lmask = 0x7FFFFFFF;
                        break;
                    case TYP_UINT:
                        lmask = 0xFFFFFFFF;
                        break;

                    default:
                        return false;
                }

                if ((lval & lmask) != lval)
                    return false;

                if (doit)
                {
                    tree->BashToConst(static_cast<int32_t>(lval));
                    if (vnStore != nullptr)
                    {
                        fgValueNumberTreeConst(tree);
                    }
                }

                return true;
#endif

            case GT_CNS_INT:

                ssize_t ival;
                ival = tree->AsIntCon()->gtIconVal;
                ssize_t imask;
                imask = 0;

                switch (dstt)
                {
                    case TYP_BYTE:
                        imask = 0x0000007F;
                        break;
                    case TYP_BOOL:
                    case TYP_UBYTE:
                        imask = 0x000000FF;
                        break;
                    case TYP_SHORT:
                        imask = 0x00007FFF;
                        break;
                    case TYP_USHORT:
                        imask = 0x0000FFFF;
                        break;
#ifdef TARGET_64BIT
                    case TYP_INT:
                        imask = 0x7FFFFFFF;
                        break;
                    case TYP_UINT:
                        imask = 0xFFFFFFFF;
                        break;
#endif // TARGET_64BIT
                    default:
                        return false;
                }

                if ((ival & imask) != ival)
                {
                    return false;
                }

#ifdef TARGET_64BIT
                if (doit)
                {
                    tree->gtType                = TYP_INT;
                    tree->AsIntCon()->gtIconVal = (int)ival;
                    if (vnStore != nullptr)
                    {
                        fgValueNumberTreeConst(tree);
                    }
                }
#endif // TARGET_64BIT

                return true;

            /* Operands that are in memory can usually be narrowed
               simply by changing their gtType */

            case GT_LCL_VAR:
                /* We only allow narrowing long -> int for a GT_LCL_VAR */
                if (dstSize == sizeof(int))
                {
                    goto NARROW_IND;
                }
                break;

            case GT_LCL_FLD:
                goto NARROW_IND;
            default:
                break;
        }

        noway_assert(doit == false);
        return false;
    }

    if (kind & (GTK_BINOP | GTK_UNOP))
    {
        GenTree* op1;
        op1 = tree->AsOp()->gtOp1;
        GenTree* op2;
        op2 = tree->AsOp()->gtOp2;

        switch (tree->gtOper)
        {
            case GT_AND:
                noway_assert(genActualType(tree->gtType) == genActualType(op1->gtType));
                noway_assert(genActualType(tree->gtType) == genActualType(op2->gtType));

                GenTree* opToNarrow;
                opToNarrow = nullptr;
                GenTree** otherOpPtr;
                otherOpPtr = nullptr;
                bool foundOperandThatBlocksNarrowing;
                foundOperandThatBlocksNarrowing = false;

                // If 'dstt' is unsigned and one of the operands can be narrowed into 'dsst',
                // the result of the GT_AND will also fit into 'dstt' and can be narrowed.
                // The same is true if one of the operands is an int const and can be narrowed into 'dsst'.
                if (!gtIsActiveCSE_Candidate(op2) && ((op2->gtOper == GT_CNS_INT) || varTypeIsUnsigned(dstt)))
                {
                    if (optNarrowTree(op2, srct, dstt, NoVNPair, false))
                    {
                        opToNarrow = op2;
                        otherOpPtr = &tree->AsOp()->gtOp1;
                    }
                    else
                    {
                        foundOperandThatBlocksNarrowing = true;
                    }
                }

                if ((opToNarrow == nullptr) && !gtIsActiveCSE_Candidate(op1) &&
                    ((op1->gtOper == GT_CNS_INT) || varTypeIsUnsigned(dstt)))
                {
                    if (optNarrowTree(op1, srct, dstt, NoVNPair, false))
                    {
                        opToNarrow = op1;
                        otherOpPtr = &tree->AsOp()->gtOp2;
                    }
                    else
                    {
                        foundOperandThatBlocksNarrowing = true;
                    }
                }

                if (opToNarrow != nullptr)
                {
                    // We will change the type of the tree and narrow opToNarrow
                    //
                    if (doit)
                    {
                        tree->gtType = genActualType(dstt);
                        tree->SetVNs(vnpNarrow);

                        optNarrowTree(opToNarrow, srct, dstt, NoVNPair, true);
                        // We may also need to cast away the upper bits of *otherOpPtr
                        if (srcSize == 8)
                        {
                            assert(tree->gtType == TYP_INT);
                            GenTree* castOp = gtNewCastNode(TYP_INT, *otherOpPtr, false, TYP_INT);
#ifdef DEBUG
                            castOp->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                            *otherOpPtr = castOp;
                        }
                    }
                    return true;
                }

                if (foundOperandThatBlocksNarrowing)
                {
                    noway_assert(doit == false);
                    return false;
                }

                goto COMMON_BINOP;

            case GT_ADD:
            case GT_MUL:

                if (tree->gtOverflow() || varTypeIsSmall(dstt))
                {
                    noway_assert(doit == false);
                    return false;
                }
                FALLTHROUGH;

            case GT_OR:
            case GT_XOR:
                noway_assert(genActualType(tree->gtType) == genActualType(op1->gtType));
                noway_assert(genActualType(tree->gtType) == genActualType(op2->gtType));
            COMMON_BINOP:
                if (gtIsActiveCSE_Candidate(op1) || gtIsActiveCSE_Candidate(op2) ||
                    !optNarrowTree(op1, srct, dstt, NoVNPair, doit) || !optNarrowTree(op2, srct, dstt, NoVNPair, doit))
                {
                    noway_assert(doit == false);
                    return false;
                }

                /* Simply change the type of the tree */

                if (doit)
                {
                    if (tree->gtOper == GT_MUL && (tree->gtFlags & GTF_MUL_64RSLT))
                    {
                        tree->gtFlags &= ~GTF_MUL_64RSLT;
                    }

                    tree->gtType = genActualType(dstt);
                    tree->SetVNs(vnpNarrow);
                }

                return true;

            case GT_IND:

            NARROW_IND:

                if ((dstSize > genTypeSize(tree->gtType)) &&
                    (varTypeIsUnsigned(dstt) && !varTypeIsUnsigned(tree->gtType)))
                {
                    return false;
                }

                /* Simply change the type of the tree */

                if (doit && (dstSize <= genTypeSize(tree->gtType)))
                {
                    if (!varTypeIsSmall(dstt))
                    {
                        dstt = varTypeToSigned(dstt);
                    }

                    tree->gtType = dstt;
                    tree->SetVNs(vnpNarrow);
                }

                return true;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GT:
            case GT_GE:

                /* These can always be narrowed since they only represent 0 or 1 */
                return true;

            case GT_CAST:
            {
                var_types cast    = tree->CastToType();
                var_types oprt    = op1->TypeGet();
                unsigned  oprSize = genTypeSize(oprt);

                if (cast != srct)
                {
                    return false;
                }

                if (varTypeIsIntegralOrI(dstt) != varTypeIsIntegralOrI(oprt))
                {
                    return false;
                }

                if (tree->gtOverflow())
                {
                    return false;
                }

                /* Is this a cast from the type we're narrowing to or a smaller one? */

                if (oprSize <= dstSize)
                {
                    /* Bash the target type of the cast */

                    if (doit)
                    {
                        if (!varTypeIsSmall(dstt))
                        {
                            dstt = varTypeToSigned(dstt);
                        }

                        if ((oprSize == dstSize) &&
                            ((varTypeIsUnsigned(dstt) == varTypeIsUnsigned(oprt)) || !varTypeIsSmall(dstt)))
                        {
                            // Same size and there is no signedness mismatch for small types: change the CAST
                            // into a NOP

                            JITDUMP("Cast operation has no effect, bashing [%06d] GT_CAST into a GT_NOP.\n",
                                    dspTreeID(tree));

                            tree->ChangeOper(GT_NOP);
                            tree->gtType = dstt;
                            // Clear the GTF_UNSIGNED flag, as it may have been set on the cast node
                            tree->gtFlags &= ~GTF_UNSIGNED;
                            tree->AsOp()->gtOp2 = nullptr;
                            tree->gtVNPair      = op1->gtVNPair; // Set to op1's ValueNumber
                        }
                        else
                        {
                            // oprSize is smaller or there is a signedness mismatch for small types

                            // Change the CastToType in the GT_CAST node
                            tree->CastToType() = dstt;

                            // The result type of a GT_CAST is never a small type.
                            // Use genActualType to widen dstt when it is a small types.
                            tree->gtType = genActualType(dstt);
                            tree->SetVNs(vnpNarrow);
                        }
                    }

                    return true;
                }
            }
                return false;

            case GT_COMMA:
                if (!gtIsActiveCSE_Candidate(op2) && optNarrowTree(op2, srct, dstt, vnpNarrow, doit))
                {
                    /* Simply change the type of the tree */

                    if (doit)
                    {
                        tree->gtType = genActualType(dstt);
                        tree->SetVNs(vnpNarrow);
                    }
                    return true;
                }
                return false;

            default:
                noway_assert(doit == false);
                return false;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// optIsVarAssignedWithDesc: do a walk to record local modification data for a statement
//
// Arguments:
//     stmt - the statement to walk
//     dsc - [in, out] data for the walk
//
bool Compiler::optIsVarAssignedWithDesc(Statement* stmt, isVarAssgDsc* dsc)
{
    class IsVarAssignedVisitor : public GenTreeVisitor<IsVarAssignedVisitor>
    {
        isVarAssgDsc* m_dsc;

    public:
        enum
        {
            DoPreOrder        = true,
            UseExecutionOrder = true,
        };

        IsVarAssignedVisitor(Compiler* comp, isVarAssgDsc* dsc) : GenTreeVisitor(comp), m_dsc(dsc)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;

            // Can this tree define a local?
            //
            if (!tree->OperIsSsaDef())
            {
                return WALK_CONTINUE;
            }

            // Determine what's written and check for calls.
            //
            if (tree->OperIs(GT_CALL))
            {
                m_dsc->ivaMaskCall = optCallInterf(tree->AsCall());
            }
            else
            {
                assert(tree->OperIs(GT_ASG));

                genTreeOps destOper = tree->gtGetOp1()->OperGet();
                if (destOper == GT_LCL_FLD)
                {
                    // We can't track every field of every var. Moreover, indirections
                    // may access different parts of the var as different (but
                    // overlapping) fields. So just treat them as indirect accesses
                    //
                    // unsigned    lclNum = dest->AsLclFld()->GetLclNum();
                    // noway_assert(lvaTable[lclNum].lvAddrTaken);
                    //
                    varRefKinds refs  = varTypeIsGC(tree->TypeGet()) ? VR_IND_REF : VR_IND_SCL;
                    m_dsc->ivaMaskInd = varRefKinds(m_dsc->ivaMaskInd | refs);
                }
                else if (destOper == GT_IND)
                {
                    // Set the proper indirection bits
                    //
                    varRefKinds refs  = varTypeIsGC(tree->TypeGet()) ? VR_IND_REF : VR_IND_SCL;
                    m_dsc->ivaMaskInd = varRefKinds(m_dsc->ivaMaskInd | refs);
                }
            }

            // Determine if the tree modifies a particular local
            //
            GenTreeLclVarCommon* lcl = nullptr;
            if (tree->DefinesLocal(m_compiler, &lcl))
            {
                const unsigned lclNum = lcl->GetLclNum();

                if (lclNum < lclMAX_ALLSET_TRACKED)
                {
                    AllVarSetOps::AddElemD(m_compiler, m_dsc->ivaMaskVal, lclNum);
                }
                else
                {
                    m_dsc->ivaMaskIncomplete = true;
                }

                // Bail out if we were checking for one particular local
                // and we now see it's modified (ignoring perhaps
                // the one tree where we expect modifications).
                //
                if ((lclNum == m_dsc->ivaVar) && (tree != m_dsc->ivaSkip))
                {
                    return WALK_ABORT;
                }
            }

            return WALK_CONTINUE;
        }
    };

    IsVarAssignedVisitor walker(this, dsc);
    return walker.WalkTree(stmt->GetRootNodePointer(), nullptr) != WALK_CONTINUE;
}

//------------------------------------------------------------------------
// optIsVarAssigned: see if a local is assigned in a range of blocks
//
// Arguments:
//     beg - first block in range
//     end - last block in range
//     skip - tree to ignore (nullptr if none)
//     var - local to check
//
// Returns:
//     true if local is directly modified
//
// Notes:
//     Does a full walk of all blocks/statements/trees, so potentially expensive.
//
//     Does not do proper checks for struct fields or exposed locals.
//
bool Compiler::optIsVarAssigned(BasicBlock* beg, BasicBlock* end, GenTree* skip, unsigned var)
{
    isVarAssgDsc desc;

    desc.ivaSkip     = skip;
    desc.ivaVar      = var;
    desc.ivaMaskCall = CALLINT_NONE;
    AllVarSetOps::AssignNoCopy(this, desc.ivaMaskVal, AllVarSetOps::MakeEmpty(this));

    for (;;)
    {
        noway_assert(beg != nullptr);

        for (Statement* const stmt : beg->Statements())
        {
            if (optIsVarAssignedWithDesc(stmt, &desc))
            {
                return true;
            }
        }

        if (beg == end)
        {
            break;
        }

        beg = beg->bbNext;
    }

    return false;
}

//------------------------------------------------------------------------
// optIsVarAssgLoop: see if a local is assigned in a loop
//
// Arguments:
//     lnum - loop number
//     var - var to check
//
// Returns:
//     true if var can possibly be modified in the loop specified by lnum
//     false if var is a loop invariant
//
bool Compiler::optIsVarAssgLoop(unsigned lnum, unsigned var)
{
    assert(lnum < optLoopCount);

    LclVarDsc* const varDsc = lvaGetDesc(var);
    if (varDsc->IsAddressExposed())
    {
        // Assume the worst (that var is possibly modified in the loop)
        //
        return true;
    }

    if (var < lclMAX_ALLSET_TRACKED)
    {
        ALLVARSET_TP vs(AllVarSetOps::MakeSingleton(this, var));

        // If local is a promoted field, also check for modifications to parent.
        //
        if (varDsc->lvIsStructField)
        {
            unsigned const parentVar = varDsc->lvParentLcl;
            assert(!lvaGetDesc(parentVar)->IsAddressExposed());
            assert(lvaGetDesc(parentVar)->lvPromoted);

            if (parentVar < lclMAX_ALLSET_TRACKED)
            {
                JITDUMP("optIsVarAssgLoop: V%02u promoted, also checking V%02u\n", var, parentVar);
                AllVarSetOps::AddElemD(this, vs, parentVar);
            }
            else
            {
                // Parent var index is too large, assume the worst.
                //
                return true;
            }
        }

        return optIsSetAssgLoop(lnum, vs) != 0;
    }
    else
    {
        if (varDsc->lvIsStructField)
        {
            return true;
        }

        return optIsVarAssigned(optLoopTable[lnum].lpHead->bbNext, optLoopTable[lnum].lpBottom, nullptr, var);
    }
}

//------------------------------------------------------------------------
// optIsSetAssgLoop: see if a set of locals is assigned in a loop
//
// Arguments:
//     lnum - loop number
//     vars - var set to check
//     inds - also consider impact of indirect stores and calls
//
// Returns:
//     true if any of vars are possibly modified in any of the blocks of the
//     loop specified by lnum, or if the loop contains any of the specified
//     aliasing operations.
//
// Notes:
//     Uses a cache to avoid repeatedly scanning the loop blocks. However this
//     cache never invalidates and so this method must be used with care.
//
bool Compiler::optIsSetAssgLoop(unsigned lnum, ALLVARSET_VALARG_TP vars, varRefKinds inds)
{
    noway_assert(lnum < optLoopCount);
    LoopDsc* loop = &optLoopTable[lnum];

    // Do we already know what variables are assigned within this loop?
    //
    if (!(loop->lpFlags & LPFLG_ASGVARS_YES))
    {
        isVarAssgDsc desc;

        // Prepare the descriptor used by the tree walker call-back
        //
        desc.ivaVar  = (unsigned)-1;
        desc.ivaSkip = nullptr;
        AllVarSetOps::AssignNoCopy(this, desc.ivaMaskVal, AllVarSetOps::MakeEmpty(this));
        desc.ivaMaskInd        = VR_NONE;
        desc.ivaMaskCall       = CALLINT_NONE;
        desc.ivaMaskIncomplete = false;

        // Now walk all the statements of the loop
        //
        for (BasicBlock* const block : loop->LoopBlocks())
        {
            for (Statement* const stmt : block->NonPhiStatements())
            {
                optIsVarAssignedWithDesc(stmt, &desc);

                if (desc.ivaMaskIncomplete)
                {
                    loop->lpFlags |= LPFLG_ASGVARS_INC;
                }
            }
        }

        AllVarSetOps::Assign(this, loop->lpAsgVars, desc.ivaMaskVal);
        loop->lpAsgInds = desc.ivaMaskInd;
        loop->lpAsgCall = desc.ivaMaskCall;

        // Now we know what variables are assigned in the loop
        //
        loop->lpFlags |= LPFLG_ASGVARS_YES;
    }

    // Now we can finally test the caller's mask against the loop's
    //
    if (!AllVarSetOps::IsEmptyIntersection(this, loop->lpAsgVars, vars) || (loop->lpAsgInds & inds))
    {
        return true;
    }

    // If caller is worried about possible indirect effects, check
    // what we know about the calls in the loop.
    //
    if (inds != 0)
    {
        switch (loop->lpAsgCall)
        {
            case CALLINT_ALL:
                return true;
            case CALLINT_REF_INDIRS:
                return (inds & VR_IND_REF) != 0;
            case CALLINT_SCL_INDIRS:
                return (inds & VR_IND_SCL) != 0;
            case CALLINT_ALL_INDIRS:
                return (inds & (VR_IND_REF | VR_IND_SCL)) != 0;
            case CALLINT_NONE:
                return false;
            default:
                noway_assert(!"Unexpected lpAsgCall value");
        }
    }

    return false;
}

//------------------------------------------------------------------------
// optRecordSsaUses: note any SSA uses within tree
//
// Arguments:
//   tree     - tree to examine
//   block    - block that does (or will) contain tree
//
// Notes:
//   Ignores SSA defs. We assume optimizations that modify trees with
//   SSA defs are introducing new defs for locals that do not require PHIs
//   or updating existing defs in place.
//
//   Currently does not examine PHI_ARG nodes as no opt phases introduce new PHIs.
//
//   Assumes block is a block that was rewritten by SSA or introduced post-SSA
//   (in particular, block is not unreachable).
//
void Compiler::optRecordSsaUses(GenTree* tree, BasicBlock* block)
{
    class SsaRecordingVisitor : public GenTreeVisitor<SsaRecordingVisitor>
    {
    private:
        BasicBlock* const m_block;

    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true
        };

        SsaRecordingVisitor(Compiler* compiler, BasicBlock* block)
            : GenTreeVisitor<SsaRecordingVisitor>(compiler), m_block(block)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTreeLclVarCommon* const tree  = (*use)->AsLclVarCommon();
            const bool                 isUse = (tree->gtFlags & GTF_VAR_DEF) == 0;

            if (isUse)
            {
                if (tree->HasSsaName())
                {
                    unsigned const      lclNum    = tree->GetLclNum();
                    unsigned const      ssaNum    = tree->GetSsaNum();
                    LclVarDsc* const    varDsc    = m_compiler->lvaGetDesc(lclNum);
                    LclSsaVarDsc* const ssaVarDsc = varDsc->GetPerSsaData(ssaNum);
                    ssaVarDsc->AddUse(m_block);
                }
                else
                {
                    assert(!m_compiler->lvaInSsa(tree->GetLclNum()));
                    assert(!tree->HasCompositeSsaName());
                }
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    SsaRecordingVisitor srv(this, block);
    srv.WalkTree(&tree, nullptr);
}

//------------------------------------------------------------------------
// optPerformHoistExpr: hoist an expression into the preheader of a loop
//
// Arguments:
//   origExpr - tree to hoist
//   exprBb   - block containing the tree
//   lnum     - loop that we're hoisting origExpr out of
//
void Compiler::optPerformHoistExpr(GenTree* origExpr, BasicBlock* exprBb, unsigned lnum)
{
    assert(exprBb != nullptr);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nHoisting a copy of ");
        printTreeID(origExpr);
        printf(" " FMT_VN, origExpr->gtVNPair.GetLiberal());
        printf(" from " FMT_BB " into PreHeader " FMT_BB " for loop " FMT_LP " <" FMT_BB ".." FMT_BB ">:\n",
               exprBb->bbNum, optLoopTable[lnum].lpHead->bbNum, lnum, optLoopTable[lnum].lpTop->bbNum,
               optLoopTable[lnum].lpBottom->bbNum);
        gtDispTree(origExpr);
        printf("\n");
    }
#endif

    // Create a copy of the expression and mark it for CSE's.
    GenTree* hoistExpr = gtCloneExpr(origExpr, GTF_MAKE_CSE);

    // The hoist Expr does not have to computed into a specific register,
    // so clear the RegNum if it was set in the original expression
    hoistExpr->ClearRegNum();

    // Copy any loop memory dependence.
    optCopyLoopMemoryDependence(origExpr, hoistExpr);

    // At this point we should have a cloned expression, marked with the GTF_MAKE_CSE flag
    assert(hoistExpr != origExpr);
    assert(hoistExpr->gtFlags & GTF_MAKE_CSE);

    GenTree* hoist = hoistExpr;
    // The value of the expression isn't used (unless it's an assignment).
    if (hoistExpr->OperGet() != GT_ASG)
    {
        hoist = gtUnusedValNode(hoistExpr);
    }

    /* Put the statement in the preheader */

    INDEBUG(optLoopTable[lnum].lpValidatePreHeader());

    BasicBlock* preHead = optLoopTable[lnum].lpHead;

    // fgMorphTree requires that compCurBB be the block that contains
    // (or in this case, will contain) the expression.
    compCurBB = preHead;
    hoist     = fgMorphTree(hoist);

    // Scan the tree for any new SSA uses.
    //
    optRecordSsaUses(hoist, preHead);

    preHead->bbFlags |= exprBb->bbFlags & BBF_COPY_PROPAGATE;

    Statement* hoistStmt = gtNewStmt(hoist);

    // Simply append the statement at the end of the preHead's list.
    Statement* firstStmt = preHead->firstStmt();
    if (firstStmt != nullptr)
    {
        /* append after last statement */

        Statement* lastStmt = preHead->lastStmt();
        assert(lastStmt->GetNextStmt() == nullptr);

        lastStmt->SetNextStmt(hoistStmt);
        hoistStmt->SetPrevStmt(lastStmt);
        firstStmt->SetPrevStmt(hoistStmt);
    }
    else
    {
        /* Empty pre-header - store the single statement in the block */

        preHead->bbStmtList = hoistStmt;
        hoistStmt->SetPrevStmt(hoistStmt);
    }

    hoistStmt->SetNextStmt(nullptr);

#ifdef DEBUG
    if (verbose)
    {
        printf("This hoisted copy placed in PreHeader (" FMT_BB "):\n", preHead->bbNum);
        gtDispTree(hoist);
        printf("\n");
    }
#endif

    if (fgNodeThreading == NodeThreading::AllTrees)
    {
        gtSetStmtInfo(hoistStmt);
        fgSetStmtSeq(hoistStmt);
    }

#ifdef DEBUG
    if (m_nodeTestData != nullptr)
    {

        // What is the depth of the loop "lnum"?
        ssize_t  depth    = 0;
        unsigned lnumIter = lnum;
        while (optLoopTable[lnumIter].lpParent != BasicBlock::NOT_IN_LOOP)
        {
            depth++;
            lnumIter = optLoopTable[lnumIter].lpParent;
        }

        NodeToTestDataMap* testData = GetNodeTestData();

        TestLabelAndNum tlAndN;
        if (testData->Lookup(origExpr, &tlAndN) && tlAndN.m_tl == TL_LoopHoist)
        {
            if (tlAndN.m_num == -1)
            {
                printf("Node ");
                printTreeID(origExpr);
                printf(" was declared 'do not hoist', but is being hoisted.\n");
                assert(false);
            }
            else if (tlAndN.m_num != depth)
            {
                printf("Node ");
                printTreeID(origExpr);
                printf(" was declared as hoistable from loop at nesting depth %d; actually hoisted from loop at depth "
                       "%d.\n",
                       tlAndN.m_num, depth);
                assert(false);
            }
            else
            {
                // We've correctly hoisted this, so remove the annotation.  Later, we'll check for any remaining "must
                // hoist" annotations.
                testData->Remove(origExpr);
                // Now we insert an annotation to make sure that "hoistExpr" is actually CSE'd.
                tlAndN.m_tl  = TL_CSE_Def;
                tlAndN.m_num = m_loopHoistCSEClass++;
                testData->Set(hoistExpr, tlAndN);
            }
        }
    }
#endif

#if LOOP_HOIST_STATS
    if (!m_curLoopHasHoistedExpression)
    {
        m_loopsWithHoistedExpressions++;
        m_curLoopHasHoistedExpression = true;
    }
    m_totalHoistedExpressions++;
#endif // LOOP_HOIST_STATS
}

//------------------------------------------------------------------------
// optHoistLoopCode: run loop hoisting phase
//
// Returns:
//    suitable phase status
//
PhaseStatus Compiler::optHoistLoopCode()
{
    // If we don't have any loops in the method then take an early out now.
    if (optLoopCount == 0)
    {
        JITDUMP("\nNo loops; no hoisting\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    unsigned jitNoHoist = JitConfig.JitNoHoist();
    if (jitNoHoist > 0)
    {
        JITDUMP("\nJitNoHoist set; no hoisting\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

#if 0
    // The code in this #if has been useful in debugging loop hoisting issues, by
    // enabling selective enablement of the loop hoisting optimization according to
    // method hash.
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("loophoisthashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
        // methHashLo = (unsigned(atoi(lostr)) << 2);  // So we don't have to use negative numbers.
    }
    char* histr = getenv("loophoisthashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
        // methHashHi = (unsigned(atoi(histr)) << 2);  // So we don't have to use negative numbers.
    }
    if (methHash < methHashLo || methHash > methHashHi)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
    printf("Doing loop hoisting in %s (0x%x).\n", info.compFullName, methHash);
#endif // DEBUG
#endif // 0     -- debugging loop hoisting issues

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In optHoistLoopCode()\n");
        fgDispHandlerTab();
        optPrintLoopTable();
    }
#endif

    optComputeInterestingVarSets();

    // Consider all the loop nests, in inner-to-outer order
    //
    bool             modified = false;
    LoopHoistContext hoistCtxt(this);
    for (unsigned lnum = 0; lnum < optLoopCount; lnum++)
    {
        if (optLoopTable[lnum].lpIsRemoved())
        {
            JITDUMP("\nLoop " FMT_LP " was removed\n", lnum);
            continue;
        }

        if (optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP)
        {
            modified |= optHoistLoopNest(lnum, &hoistCtxt);
        }
    }

#ifdef DEBUG
    // Test Data stuff..
    //
    if (m_nodeTestData == nullptr)
    {
        NodeToTestDataMap* testData = GetNodeTestData();
        for (GenTree* const node : NodeToTestDataMap::KeyIteration(testData))
        {
            TestLabelAndNum tlAndN;
            bool            b = testData->Lookup(node, &tlAndN);
            assert(b);
            if (tlAndN.m_tl != TL_LoopHoist)
            {
                continue;
            }
            // Otherwise, it is a loop hoist annotation.
            assert(tlAndN.m_num < 100); // >= 100 indicates nested static field address, should already have been moved.
            if (tlAndN.m_num >= 0)
            {
                printf("Node ");
                printTreeID(node);
                printf(" was declared 'must hoist', but has not been hoisted.\n");
                assert(false);
            }
        }
    }
#endif // DEBUG

    return modified ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// optHoistLoopNest: run loop hoisting for indicated loop and all contained loops
//
// Arguments:
//    lnum - loop to process
//    hoistCtxt - context for the hoisting
//
// Returns:
//    true if any hoisting was done
//
bool Compiler::optHoistLoopNest(unsigned lnum, LoopHoistContext* hoistCtxt)
{
    // Do this loop, then recursively do all nested loops.
    JITDUMP("\n%s " FMT_LP "\n", optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP ? "Loop Nest" : "Nested Loop",
            lnum);

#if LOOP_HOIST_STATS
    // Record stats
    m_curLoopHasHoistedExpression = false;
    m_loopsConsidered++;
#endif // LOOP_HOIST_STATS

    bool modified = false;

    if (optLoopTable[lnum].lpChild != BasicBlock::NOT_IN_LOOP)
    {
        for (unsigned child = optLoopTable[lnum].lpChild; child != BasicBlock::NOT_IN_LOOP;
             child          = optLoopTable[child].lpSibling)
        {
            modified |= optHoistLoopNest(child, hoistCtxt);
        }
    }

    modified |= optHoistThisLoop(lnum, hoistCtxt);

    return modified;
}

//------------------------------------------------------------------------
// optHoistThisLoop: run loop hoisting for the indicated loop
//
// Arguments:
//    lnum - loop to process
//    hoistCtxt - context for the hoisting
//
// Returns:
//    true if any hoisting was done
//
bool Compiler::optHoistThisLoop(unsigned lnum, LoopHoistContext* hoistCtxt)
{
    LoopDsc* pLoopDsc = &optLoopTable[lnum];

    /* If loop was removed continue */

    if (pLoopDsc->lpIsRemoved())
    {
        JITDUMP("   ... not hoisting " FMT_LP ": removed\n", lnum);
        return false;
    }

    // Ensure the per-loop sets/tables are empty.
    hoistCtxt->m_curLoopVnInvariantCache.RemoveAll();

#ifdef DEBUG
    if (verbose)
    {
        printf("optHoistThisLoop for loop " FMT_LP " <" FMT_BB ".." FMT_BB ">:\n", lnum, pLoopDsc->lpTop->bbNum,
               pLoopDsc->lpBottom->bbNum);
        printf("  Loop body %s a call\n", (pLoopDsc->lpFlags & LPFLG_CONTAINS_CALL) ? "contains" : "does not contain");
        printf("  Loop has %s\n", (pLoopDsc->lpExitCnt == 1) ? "single exit" : "multiple exits");
    }
#endif

    VARSET_TP loopVars(VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, pLoopDsc->lpVarUseDef));

    pLoopDsc->lpVarInOutCount    = VarSetOps::Count(this, pLoopDsc->lpVarInOut);
    pLoopDsc->lpLoopVarCount     = VarSetOps::Count(this, loopVars);
    pLoopDsc->lpHoistedExprCount = 0;

#ifndef TARGET_64BIT

    if (!VarSetOps::IsEmpty(this, lvaLongVars))
    {
        // Since 64-bit variables take up two registers on 32-bit targets, we increase
        //  the Counts such that each TYP_LONG variable counts twice.
        //
        VARSET_TP loopLongVars(VarSetOps::Intersection(this, loopVars, lvaLongVars));
        VARSET_TP inOutLongVars(VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, lvaLongVars));

#ifdef DEBUG
        if (verbose)
        {
            printf("\n  LONGVARS(%d)=", VarSetOps::Count(this, lvaLongVars));
            dumpConvertedVarSet(this, lvaLongVars);
        }
#endif
        pLoopDsc->lpLoopVarCount += VarSetOps::Count(this, loopLongVars);
        pLoopDsc->lpVarInOutCount += VarSetOps::Count(this, inOutLongVars);
    }
#endif // !TARGET_64BIT

#ifdef DEBUG
    if (verbose)
    {
        printf("\n  USEDEF  (%d)=", VarSetOps::Count(this, pLoopDsc->lpVarUseDef));
        dumpConvertedVarSet(this, pLoopDsc->lpVarUseDef);

        printf("\n  INOUT   (%d)=", pLoopDsc->lpVarInOutCount);
        dumpConvertedVarSet(this, pLoopDsc->lpVarInOut);

        printf("\n  LOOPVARS(%d)=", pLoopDsc->lpLoopVarCount);
        dumpConvertedVarSet(this, loopVars);
        printf("\n");
    }
#endif

    if (!VarSetOps::IsEmpty(this, lvaFloatVars))
    {
        VARSET_TP loopFPVars(VarSetOps::Intersection(this, loopVars, lvaFloatVars));
        VARSET_TP inOutFPVars(VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, lvaFloatVars));

        pLoopDsc->lpLoopVarFPCount     = VarSetOps::Count(this, loopFPVars);
        pLoopDsc->lpVarInOutFPCount    = VarSetOps::Count(this, inOutFPVars);
        pLoopDsc->lpHoistedFPExprCount = 0;

        pLoopDsc->lpLoopVarCount -= pLoopDsc->lpLoopVarFPCount;
        pLoopDsc->lpVarInOutCount -= pLoopDsc->lpVarInOutFPCount;

#ifdef DEBUG
        if (verbose)
        {
            printf("  INOUT-FP(%d)=", pLoopDsc->lpVarInOutFPCount);
            dumpConvertedVarSet(this, inOutFPVars);

            printf("\n  LOOPV-FP(%d)=", pLoopDsc->lpLoopVarFPCount);
            dumpConvertedVarSet(this, loopFPVars);

            printf("\n");
        }
#endif
    }
    else // lvaFloatVars is empty
    {
        pLoopDsc->lpLoopVarFPCount     = 0;
        pLoopDsc->lpVarInOutFPCount    = 0;
        pLoopDsc->lpHoistedFPExprCount = 0;
    }

    // Find the set of definitely-executed blocks.
    // Ideally, the definitely-executed blocks are the ones that post-dominate the entry block.
    // Until we have post-dominators, we'll special-case for single-exit blocks.
    //
    // Todo: it is not clear if this is a correctness requirement or a profitability heuristic.
    // It seems like the latter. Ideally there are enough safeguards to prevent hoisting exception
    // or side-effect dependent things. Note that HoistVisitor uses `m_beforeSideEffect` to determine if it's
    // ok to hoist a side-effect. It allows this only for the first block (the entry block), before any
    // side-effect has been seen. After the first block, it assumes that there has been a side effect and
    // no further side-effect can be hoisted. It is true that we don't analyze any program behavior in the
    // flow graph between the entry block and the subsequent blocks, whether they be the next block dominating
    // the exit block, or the pre-headers of nested loops.
    //
    // We really should consider hoisting from conditionally executed blocks, if they are frequently executed
    // and it is safe to evaluate the tree early.
    //
    ArrayStack<BasicBlock*> defExec(getAllocatorLoopHoist());

    // Add the pre-headers of any child loops to the list of blocks to consider for hoisting.
    // Note that these are not necessarily definitely executed. However, it is a heuristic that they will
    // often provide good opportunities for further hoisting since we hoist from inside-out,
    // and the inner loop may have already hoisted something loop-invariant to them. If the child
    // loop pre-header block would be added anyway (by dominating the loop exit block), we don't
    // add it here, and let it be added naturally, below.
    //
    // Note that all pre-headers get added first, which means they get considered for hoisting last. It is
    // assumed that the order does not matter for correctness (since there is no execution order known).
    // Note that the order does matter for the hoisting profitability heuristics, as we might
    // run out of hoisting budget when processing the blocks.
    //
    // For example, consider this loop nest:
    //
    // for (....) { // loop L00
    //    pre-header 1
    //    for (...) { // loop L01
    //    }
    //    // pre-header 2
    //    for (...) { // loop L02
    //       // pre-header 3
    //       for (...) { // loop L03
    //       }
    //    }
    // }
    //
    // When processing the outer loop L00 (with an assumed single exit), we will push on the defExec stack
    // pre-header 2, pre-header 1, the loop exit block, any IDom tree blocks leading to the entry block,
    // and finally the entry block. (Note that the child loop iteration order of a loop is from "farthest"
    // from the loop "head" to "nearest".) Blocks are considered for hoisting in the opposite order.
    //
    // Note that pre-header 3 is not pushed, since it is not a direct child. It would have been processed
    // when loop L02 was considered for hoisting.
    //
    // The order of pushing pre-header 1 and pre-header 2 is based on the order in the loop table (which is
    // convenient). But note that it is arbitrary because there is not guaranteed execution order amongst
    // the child loops.

    for (BasicBlock::loopNumber childLoop = pLoopDsc->lpChild; //
         childLoop != BasicBlock::NOT_IN_LOOP;                 //
         childLoop = optLoopTable[childLoop].lpSibling)
    {
        if (optLoopTable[childLoop].lpIsRemoved())
        {
            continue;
        }
        INDEBUG(optLoopTable[childLoop].lpValidatePreHeader());
        BasicBlock* childPreHead = optLoopTable[childLoop].lpHead;
        if (pLoopDsc->lpExitCnt == 1)
        {
            if (fgDominate(childPreHead, pLoopDsc->lpExit))
            {
                // If the child loop pre-header dominates the exit, it will get added in the dominator tree
                // loop below.
                continue;
            }
        }
        else
        {
            // If the child loop pre-header is the loop entry for a multi-exit loop, it will get added below.
            if (childPreHead == pLoopDsc->lpEntry)
            {
                continue;
            }
        }
        JITDUMP("  --  " FMT_BB " (child loop pre-header)\n", childPreHead->bbNum);
        defExec.Push(childPreHead);
    }

    if (pLoopDsc->lpExitCnt == 1)
    {
        assert(pLoopDsc->lpExit != nullptr);
        JITDUMP("  Considering hoisting in blocks that either dominate exit block " FMT_BB
                ", or pre-headers of nested loops, if any:\n",
                pLoopDsc->lpExit->bbNum);

        // Push dominators, until we reach "entry" or exit the loop.

        BasicBlock* cur = pLoopDsc->lpExit;
        while ((cur != nullptr) && (cur != pLoopDsc->lpEntry))
        {
            JITDUMP("  --  " FMT_BB " (dominate exit block)\n", cur->bbNum);
            assert(pLoopDsc->lpContains(cur));
            defExec.Push(cur);
            cur = cur->bbIDom;
        }
        noway_assert(cur == pLoopDsc->lpEntry);
    }
    else // More than one exit
    {
        // We'll assume that only the entry block is definitely executed.
        // We could in the future do better.

        JITDUMP("  Considering hoisting in entry block " FMT_BB " because " FMT_LP " has more than one exit\n",
                pLoopDsc->lpEntry->bbNum, lnum);
    }

    JITDUMP("  --  " FMT_BB " (entry block)\n", pLoopDsc->lpEntry->bbNum);
    defExec.Push(pLoopDsc->lpEntry);

    optHoistLoopBlocks(lnum, &defExec, hoistCtxt);

    const unsigned numHoisted = pLoopDsc->lpHoistedFPExprCount + pLoopDsc->lpHoistedExprCount;
    return numHoisted > 0;
}

bool Compiler::optIsProfitableToHoistTree(GenTree* tree, unsigned lnum)
{
    LoopDsc* pLoopDsc = &optLoopTable[lnum];

    bool loopContainsCall = (pLoopDsc->lpFlags & LPFLG_CONTAINS_CALL) != 0;

    int availRegCount;
    int hoistedExprCount;
    int loopVarCount;
    int varInOutCount;

    if (varTypeUsesIntReg(tree))
    {
        hoistedExprCount = pLoopDsc->lpHoistedExprCount;
        loopVarCount     = pLoopDsc->lpLoopVarCount;
        varInOutCount    = pLoopDsc->lpVarInOutCount;

        availRegCount = CNT_CALLEE_SAVED - 1;
        if (!loopContainsCall)
        {
            availRegCount += CNT_CALLEE_TRASH - 1;
        }
#ifndef TARGET_64BIT
        // For our 32-bit targets Long types take two registers.
        if (varTypeIsLong(tree->TypeGet()))
        {
            availRegCount = (availRegCount + 1) / 2;
        }
#endif
    }
    else
    {
        assert(varTypeUsesFloatReg(tree));

        hoistedExprCount = pLoopDsc->lpHoistedFPExprCount;
        loopVarCount     = pLoopDsc->lpLoopVarFPCount;
        varInOutCount    = pLoopDsc->lpVarInOutFPCount;

        availRegCount = CNT_CALLEE_SAVED_FLOAT;
        if (!loopContainsCall)
        {
            availRegCount += CNT_CALLEE_TRASH_FLOAT - 1;
        }
#ifdef TARGET_ARM
        // For ARM each double takes two FP registers
        // For now on ARM we won't track singles/doubles
        // and instead just assume that we always have doubles.
        //
        availRegCount /= 2;
#endif
    }

    // decrement the availRegCount by the count of expression that we have already hoisted.
    availRegCount -= hoistedExprCount;

    // the variables that are read/written inside the loop should
    // always be a subset of the InOut variables for the loop
    assert(loopVarCount <= varInOutCount);

    // When loopVarCount >= availRegCount we believe that all of the
    // available registers will get used to hold LclVars inside the loop.
    // This pessimistically assumes that each loopVar has a conflicting
    // lifetime with every other loopVar.
    // For this case we will hoist the expression only if is profitable
    // to place it in a stack home location (GetCostEx() >= 2*IND_COST_EX)
    // as we believe it will be placed in the stack or one of the other
    // loopVars will be spilled into the stack
    //
    if (loopVarCount >= availRegCount)
    {
        // Don't hoist expressions that are not heavy: tree->GetCostEx() < (2*IND_COST_EX)
        if (tree->GetCostEx() < (2 * IND_COST_EX))
        {
            JITDUMP("    tree cost too low: %d < %d (loopVarCount %u >= availRegCount %u)\n", tree->GetCostEx(),
                    2 * IND_COST_EX, loopVarCount, availRegCount);
            return false;
        }
    }

    // When varInOutCount < availRegCount we are know that there are
    // some available register(s) when we enter the loop body.
    // When varInOutCount == availRegCount there often will be a register
    // available when we enter the loop body, since a loop often defines a
    // LclVar on exit or there is often at least one LclVar that is worth
    // spilling to the stack to make way for this hoisted expression.
    // So we are willing hoist an expression with GetCostEx() == MIN_CSE_COST
    //
    if (varInOutCount > availRegCount)
    {
        // Don't hoist expressions that barely meet CSE cost requirements: tree->GetCostEx() == MIN_CSE_COST
        if (tree->GetCostEx() <= MIN_CSE_COST + 1)
        {
            JITDUMP("    tree not good CSE: %d <= %d (varInOutCount %u > availRegCount %u)\n", tree->GetCostEx(),
                    2 * MIN_CSE_COST + 1, varInOutCount, availRegCount)
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// optRecordLoopMemoryDependence: record that tree's value number
//   is dependent on a particular memory VN
//
// Arguments:
//   tree -- tree in question
//   block -- block containing tree
//   memoryVN -- VN for a "map" from a select operation encounterd
//     while computing the tree's VN
//
// Notes:
//   Only tracks trees in loops, and memory updates in the same loop nest.
//   So this is a coarse-grained dependence that is only usable for
//   hoisting tree out of its enclosing loops.
//
void Compiler::optRecordLoopMemoryDependence(GenTree* tree, BasicBlock* block, ValueNum memoryVN)
{
    // If tree is not in a loop, we don't need to track its loop dependence.
    //
    unsigned const loopNum = block->bbNatLoopNum;

    if (loopNum == BasicBlock::NOT_IN_LOOP)
    {
        return;
    }

    // Find the loop associated with this memory VN.
    //
    unsigned updateLoopNum = vnStore->LoopOfVN(memoryVN);

    if (updateLoopNum >= BasicBlock::MAX_LOOP_NUM)
    {
        // There should be only two special non-loop loop nums.
        //
        assert((updateLoopNum == BasicBlock::MAX_LOOP_NUM) || (updateLoopNum == BasicBlock::NOT_IN_LOOP));

        // memoryVN defined outside of any loop, we can ignore.
        //
        JITDUMP("      ==> Not updating loop memory dependence of [%06u], memory " FMT_VN " not defined in a loop\n",
                dspTreeID(tree), memoryVN);
        return;
    }

    // If the loop was removed, then record the dependence in the nearest enclosing loop, if any.
    //
    while (optLoopTable[updateLoopNum].lpIsRemoved())
    {
        unsigned const updateParentLoopNum = optLoopTable[updateLoopNum].lpParent;

        if (updateParentLoopNum == BasicBlock::NOT_IN_LOOP)
        {
            // Memory VN was defined in a loop, but no longer.
            //
            JITDUMP("      ==> Not updating loop memory dependence of [%06u], memory " FMT_VN
                    " no longer defined in a loop\n",
                    dspTreeID(tree), memoryVN);
            break;
        }

        JITDUMP("      ==> " FMT_LP " removed, updating dependence to parent " FMT_LP "\n", updateLoopNum,
                updateParentLoopNum);

        updateLoopNum = updateParentLoopNum;
    }

    // If the update block is not the header of a loop containing
    // block, we can also ignore the update.
    //
    if (!optLoopContains(updateLoopNum, loopNum))
    {
        JITDUMP("      ==> Not updating loop memory dependence of [%06u]/" FMT_LP ", memory " FMT_VN "/" FMT_LP
                " is not defined in an enclosing loop\n",
                dspTreeID(tree), loopNum, memoryVN, updateLoopNum);
        return;
    }

    // If we already have a recorded a loop entry block for this
    // tree, see if the new update is for a more closely nested
    // loop.
    //
    NodeToLoopMemoryBlockMap* const map      = GetNodeToLoopMemoryBlockMap();
    BasicBlock*                     mapBlock = nullptr;

    if (map->Lookup(tree, &mapBlock))
    {
        unsigned const mapLoopNum = mapBlock->bbNatLoopNum;

        // If the update loop contains the existing map loop,
        // the existing map loop is more constraining. So no
        // update needed.
        //
        if (optLoopContains(updateLoopNum, mapLoopNum))
        {
            JITDUMP("      ==> Not updating loop memory dependence of [%06u]; alrady constrained to " FMT_LP
                    " nested in " FMT_LP "\n",
                    dspTreeID(tree), mapLoopNum, updateLoopNum);
            return;
        }
    }

    // MemoryVN now describes the most constraining loop memory dependence
    // we know of. Update the map.
    //
    JITDUMP("      ==> Updating loop memory dependence of [%06u] to " FMT_LP "\n", dspTreeID(tree), updateLoopNum);
    map->Set(tree, optLoopTable[updateLoopNum].lpEntry, NodeToLoopMemoryBlockMap::Overwrite);
}

//------------------------------------------------------------------------
// optCopyLoopMemoryDependence: record that tree's loop memory dependence
//   is the same as some other tree.
//
// Arguments:
//   fromTree -- tree to copy dependence from
//   toTree -- tree in question
//
void Compiler::optCopyLoopMemoryDependence(GenTree* fromTree, GenTree* toTree)
{
    NodeToLoopMemoryBlockMap* const map      = GetNodeToLoopMemoryBlockMap();
    BasicBlock*                     mapBlock = nullptr;

    if (map->Lookup(fromTree, &mapBlock))
    {
        map->Set(toTree, mapBlock);
    }
}

//------------------------------------------------------------------------
// optHoistLoopBlocks: Hoist invariant expression out of the loop.
//
// Arguments:
//    loopNum - The number of the loop
//    blocks - A stack of blocks belonging to the loop
//    hoistContext - The loop hoist context
//
// Assumptions:
//    The `blocks` stack contains the definitely-executed blocks in
//    the loop, in the execution order, starting with the loop entry
//    block on top of the stack.
//
void Compiler::optHoistLoopBlocks(unsigned loopNum, ArrayStack<BasicBlock*>* blocks, LoopHoistContext* hoistContext)
{
    class HoistVisitor : public GenTreeVisitor<HoistVisitor>
    {
        class Value
        {
            GenTree* m_node;

        public:
            bool m_hoistable;
            bool m_cctorDependent;
            bool m_invariant;

#ifdef DEBUG
            const char* m_failReason;
#endif

            Value(GenTree* node) : m_node(node), m_hoistable(false), m_cctorDependent(false), m_invariant(false)
            {
#ifdef DEBUG
                m_failReason = "unset";
#endif
            }

            GenTree* Node()
            {
                return m_node;
            }
        };

        ArrayStack<Value> m_valueStack;
        bool              m_beforeSideEffect;
        unsigned          m_loopNum;
        LoopHoistContext* m_hoistContext;
        BasicBlock*       m_currentBlock;

        bool IsNodeHoistable(GenTree* node)
        {
            // TODO-CQ: This is a more restrictive version of a check that optIsCSEcandidate already does - it allows
            // a struct typed node if a class handle can be recovered from it.
            if (node->TypeGet() == TYP_STRUCT)
            {
                return false;
            }
            else if (node->OperIs(GT_NULLCHECK))
            {
                // If a null-check is for `this` object, it is safe to
                // hoist it out of the loop. Assertionprop will get rid
                // of left over nullchecks present inside the loop. Also,
                // since NULLCHECK has no value, it will never be CSE,
                // hence this check is not present in optIsCSEcandidate().
                return true;
            }

            // Tree must be a suitable CSE candidate for us to be able to hoist it.
            return m_compiler->optIsCSEcandidate(node);
        }

        bool IsTreeVNInvariant(GenTree* tree)
        {
            ValueNum vn = tree->gtVNPair.GetLiberal();
            bool     vnIsInvariant =
                m_compiler->optVNIsLoopInvariant(vn, m_loopNum, &m_hoistContext->m_curLoopVnInvariantCache);

            // Even though VN is invariant in the loop (say a constant) its value may depend on position
            // of tree, so for loop hoisting we must also check that any memory read by tree
            // is also invariant in the loop.
            //
            if (vnIsInvariant)
            {
                vnIsInvariant = IsTreeLoopMemoryInvariant(tree);
            }
            return vnIsInvariant;
        }

        bool IsHoistableOverExcepSibling(GenTree* node, bool siblingHasExcep)
        {
            JITDUMP("      [%06u]", dspTreeID(node));

            if ((node->gtFlags & GTF_ALL_EFFECT) != 0)
            {
                // If the hoistable node has any side effects, make sure
                // we don't hoist it past a sibling that throws any exception.
                if (siblingHasExcep)
                {
                    JITDUMP(" not hoistable: cannot move past node that throws exception.\n");
                    return false;
                }
            }
            JITDUMP(" hoistable\n");
            return true;
        }

        //------------------------------------------------------------------------
        // IsTreeLoopMemoryInvariant: determine if the value number of tree
        //   is dependent on the tree being executed within the current loop
        //
        // Arguments:
        //   tree -- tree in question
        //
        // Returns:
        //   true if tree could be evaluated just before loop and get the
        //   same value.
        //
        // Note:
        //   Calls are optimistically assumed to be invariant.
        //   Caller must do their own analysis for these tree types.
        //
        bool IsTreeLoopMemoryInvariant(GenTree* tree)
        {
            if (tree->IsCall())
            {
                // Calls are handled specially by hoisting, and loop memory dependence
                // must be checked by other means.
                //
                return true;
            }

            NodeToLoopMemoryBlockMap* const map            = m_compiler->GetNodeToLoopMemoryBlockMap();
            BasicBlock*                     loopEntryBlock = nullptr;
            if (map->Lookup(tree, &loopEntryBlock))
            {
                for (MemoryKind memoryKind : allMemoryKinds())
                {
                    ValueNum loopMemoryVN =
                        m_compiler->GetMemoryPerSsaData(loopEntryBlock->bbMemorySsaNumIn[memoryKind])
                            ->m_vnPair.GetLiberal();
                    if (!m_compiler->optVNIsLoopInvariant(loopMemoryVN, m_loopNum,
                                                          &m_hoistContext->m_curLoopVnInvariantCache))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

    public:
        enum
        {
            ComputeStack      = false,
            DoPreOrder        = true,
            DoPostOrder       = true,
            DoLclVarsOnly     = false,
            UseExecutionOrder = true,
        };

        HoistVisitor(Compiler* compiler, unsigned loopNum, LoopHoistContext* hoistContext)
            : GenTreeVisitor(compiler)
            , m_valueStack(compiler->getAllocator(CMK_LoopHoist))
            , m_beforeSideEffect(true)
            , m_loopNum(loopNum)
            , m_hoistContext(hoistContext)
            , m_currentBlock(nullptr)
        {
        }

        void HoistBlock(BasicBlock* block)
        {
            m_currentBlock = block;
            for (Statement* const stmt : block->NonPhiStatements())
            {
                WalkTree(stmt->GetRootNodePointer(), nullptr);
                Value& top = m_valueStack.TopRef();
                assert(top.Node() == stmt->GetRootNode());

                // hoist the top node?
                if (top.m_hoistable)
                {
                    m_compiler->optHoistCandidate(stmt->GetRootNode(), block, m_loopNum, m_hoistContext);
                }
                else
                {
                    JITDUMP("      [%06u] %s: %s\n", dspTreeID(top.Node()),
                            top.m_invariant ? "not hoistable" : "not invariant", top.m_failReason);
                }

                m_valueStack.Reset();
            }

            // Only unconditionally executed blocks in the loop are visited (see optHoistThisLoop)
            // so after we're done visiting the first block we need to assume the worst, that the
            // blocks that are not visited have side effects.
            m_beforeSideEffect = false;
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            JITDUMP("----- PreOrderVisit for [%06u] %s\n", dspTreeID(node), GenTree::OpName(node->OperGet()));
            m_valueStack.Emplace(node);
            return fgWalkResult::WALK_CONTINUE;
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            JITDUMP("----- PostOrderVisit for [%06u] %s\n", dspTreeID(tree), GenTree::OpName(tree->OperGet()));

            if (tree->OperIsLocal())
            {
                GenTreeLclVarCommon* lclVar = tree->AsLclVarCommon();
                unsigned             lclNum = lclVar->GetLclNum();

                // To be invariant a LclVar node must not be the LHS of an assignment ...
                bool isInvariant = !user->OperIs(GT_ASG) || (user->AsOp()->gtGetOp1() != tree);
                // and the variable must be in SSA ...
                isInvariant = isInvariant && lclVar->HasSsaName();
                // and the SSA definition must be outside the loop we're hoisting from ...
                isInvariant = isInvariant &&
                              !m_compiler->optLoopTable[m_loopNum].lpContains(
                                  m_compiler->lvaGetDesc(lclNum)->GetPerSsaData(lclVar->GetSsaNum())->GetBlock());
                // and the VN of the tree is considered invariant as well.
                //
                // TODO-CQ: This VN invariance check should not be necessary and in some cases it is conservative - it
                // is possible that the SSA def is outside the loop but VN does not understand what the node is doing
                // (e.g. LCL_FLD-based type reinterpretation) and assigns a "new, unique VN" to the node. This VN is
                // associated with the block where the node is, a loop block, and thus the VN is considered to not be
                // invariant.
                // On the other hand, it is possible for a SSA def to be inside the loop yet the use to be invariant,
                // if the defining expression is also invariant. In such a case the VN invariance would help but it is
                // blocked by the SSA invariance check.
                isInvariant = isInvariant && IsTreeVNInvariant(tree);

                Value& top = m_valueStack.TopRef();
                assert(top.Node() == tree);

                if (isInvariant)
                {
                    top.m_invariant = true;
                    // In general it doesn't make sense to hoist a local node but there are exceptions, for example
                    // LCL_FLD nodes (because then the variable cannot be enregistered and the node always turns
                    // into a memory access).
                    top.m_hoistable = IsNodeHoistable(tree);
                }

#ifdef DEBUG
                if (!isInvariant)
                {
                    top.m_failReason = "local, not rvalue / not in SSA / defined within current loop";
                }
                else if (!top.m_hoistable)
                {
                    top.m_failReason = "not handled by cse";
                }
#endif

                JITDUMP("      [%06u] %s: %s: %s\n", dspTreeID(tree), GenTree::OpName(tree->OperGet()),
                        top.m_invariant ? (top.m_hoistable ? "hoistable" : "not hoistable") : "not invariant",
                        top.m_failReason);

                return fgWalkResult::WALK_CONTINUE;
            }

            // Initclass CLS_VARs and IconHandles are the base cases of cctor dependent trees.
            // In the IconHandle case, it's of course the dereference, rather than the constant itself, that is
            // truly dependent on the cctor.  So a more precise approach would be to separately propagate
            // isCctorDependent and isAddressWhoseDereferenceWouldBeCctorDependent, but we don't for
            // simplicity/throughput; the constant itself would be considered non-hoistable anyway, since
            // optIsCSEcandidate returns false for constants.
            bool treeIsCctorDependent     = tree->OperIs(GT_CNS_INT) && ((tree->gtFlags & GTF_ICON_INITCLASS) != 0);
            bool treeIsInvariant          = true;
            bool treeHasHoistableChildren = false;
            int  childCount;

#ifdef DEBUG
            const char* failReason = "unknown";
#endif

            for (childCount = 0; m_valueStack.TopRef(childCount).Node() != tree; childCount++)
            {
                Value& child = m_valueStack.TopRef(childCount);

                if (child.m_hoistable)
                {
                    treeHasHoistableChildren = true;
                }

                if (!child.m_invariant)
                {
                    treeIsInvariant = false;
                    INDEBUG(failReason = "variant child";)
                }

                if (child.m_cctorDependent)
                {
                    // Normally, a parent of a cctor-dependent tree is also cctor-dependent.
                    treeIsCctorDependent = true;

                    // Check for the case where we can stop propagating cctor-dependent upwards.
                    if (tree->OperIs(GT_COMMA) && (child.Node() == tree->gtGetOp2()))
                    {
                        GenTree* op1 = tree->gtGetOp1();
                        if (op1->OperIs(GT_CALL))
                        {
                            GenTreeCall* call = op1->AsCall();
                            if ((call->gtCallType == CT_HELPER) &&
                                s_helperCallProperties.MayRunCctor(eeGetHelperNum(call->gtCallMethHnd)))
                            {
                                // Hoisting the comma is ok because it would hoist the initialization along
                                // with the static field reference.
                                treeIsCctorDependent = false;
                                // Hoisting the static field without hoisting the initialization would be
                                // incorrect, make sure we consider the field (which we flagged as
                                // cctor-dependent) non-hoistable.
                                noway_assert(!child.m_hoistable);
                            }
                        }
                    }
                }
            }

            // If all the children of "tree" are hoistable, then "tree" itself can be hoisted,
            // unless it has a static var reference that can't be hoisted past its cctor call.
            bool treeIsHoistable = treeIsInvariant && !treeIsCctorDependent;

#ifdef DEBUG
            if (treeIsInvariant && !treeIsHoistable)
            {
                failReason = "cctor dependent";
            }
#endif

            // But we must see if anything else prevents "tree" from being hoisted.
            //
            if (treeIsInvariant)
            {
                if (treeIsHoistable)
                {
                    treeIsHoistable = IsNodeHoistable(tree);
                    if (!treeIsHoistable)
                    {
                        INDEBUG(failReason = "not handled by cse";)
                    }
                }

                // If it's a call, it must be a helper call, and be pure.
                // Further, if it may run a cctor, it must be labeled as "Hoistable"
                // (meaning it won't run a cctor because the class is not precise-init).
                if (treeIsHoistable && tree->IsCall())
                {
                    GenTreeCall* call = tree->AsCall();
                    if (call->gtCallType != CT_HELPER)
                    {
                        INDEBUG(failReason = "non-helper call";)
                        treeIsHoistable = false;
                    }
                    else
                    {
                        CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);
                        if (!s_helperCallProperties.IsPure(helpFunc))
                        {
                            INDEBUG(failReason = "impure helper call";)
                            treeIsHoistable = false;
                        }
                        else if (s_helperCallProperties.MayRunCctor(helpFunc) &&
                                 ((call->gtFlags & GTF_CALL_HOISTABLE) == 0))
                        {
                            INDEBUG(failReason = "non-hoistable helper call";)
                            treeIsHoistable = false;
                        }
                    }
                }

                if (treeIsHoistable)
                {
                    if (!m_beforeSideEffect)
                    {
                        // For now, we give up on an expression that might raise an exception if it is after the
                        // first possible global side effect (and we assume we're after that if we're not in the first
                        // block).
                        // TODO-CQ: this is when we might do loop cloning.
                        //
                        if ((tree->gtFlags & GTF_EXCEPT) != 0)
                        {
                            INDEBUG(failReason = "side effect ordering constraint";)
                            treeIsHoistable = false;
                        }
                    }
                }

                // Is the value of the whole tree loop invariant?
                treeIsInvariant = IsTreeVNInvariant(tree);

                // Is the value of the whole tree loop invariant?
                if (!treeIsInvariant)
                {
                    // Here we have a tree that is not loop invariant and we thus cannot hoist
                    INDEBUG(failReason = "tree VN is loop variant";)
                    treeIsHoistable = false;
                }
            }

            // Next check if we need to set 'm_beforeSideEffect' to false.
            //
            // If we have already set it to false then we can skip these checks
            //
            if (m_beforeSideEffect)
            {
                // Is the value of the whole tree loop invariant?
                if (!treeIsInvariant)
                {
                    // We have a tree that is not loop invariant and we thus cannot hoist
                    assert(treeIsHoistable == false);

                    // Check if we should clear m_beforeSideEffect.
                    // If 'tree' can throw an exception then we need to set m_beforeSideEffect to false.
                    // Note that calls are handled below
                    if (tree->OperMayThrow(m_compiler) && !tree->IsCall())
                    {
                        m_beforeSideEffect = false;
                    }
                }

                // In the section below, we only care about memory side effects.  We assume that expressions will
                // be hoisted so that they are evaluated in the same order as they would have been in the loop,
                // and therefore throw exceptions in the same order.
                //
                if (tree->IsCall())
                {
                    // If it's a call, it must be a helper call that does not mutate the heap.
                    // Further, if it may run a cctor, it must be labeled as "Hoistable"
                    // (meaning it won't run a cctor because the class is not precise-init).
                    GenTreeCall* call = tree->AsCall();
                    if (call->gtCallType != CT_HELPER)
                    {
                        m_beforeSideEffect = false;
                    }
                    else
                    {
                        CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);
                        if (s_helperCallProperties.MutatesHeap(helpFunc))
                        {
                            m_beforeSideEffect = false;
                        }
                        else if (s_helperCallProperties.MayRunCctor(helpFunc) &&
                                 (call->gtFlags & GTF_CALL_HOISTABLE) == 0)
                        {
                            m_beforeSideEffect = false;
                        }

                        // Additional check for helper calls that throw exceptions
                        if (!treeIsInvariant)
                        {
                            // We have a tree that is not loop invariant and we thus cannot hoist
                            assert(treeIsHoistable == false);

                            // Does this helper call throw?
                            if (!s_helperCallProperties.NoThrow(helpFunc))
                            {
                                m_beforeSideEffect = false;
                            }
                        }
                    }
                }
                else if (tree->OperRequiresAsgFlag())
                {
                    // Assume all stores except "ASG(non-addr-exposed LCL, ...)" are globally visible.
                    GenTreeLclVarCommon* lclNode;
                    bool                 isGloballyVisibleStore;
                    if (tree->OperIs(GT_ASG) && tree->DefinesLocal(m_compiler, &lclNode))
                    {
                        isGloballyVisibleStore = m_compiler->lvaGetDesc(lclNode)->IsAddressExposed();
                    }
                    else
                    {
                        isGloballyVisibleStore = true;
                    }

                    if (isGloballyVisibleStore)
                    {
                        INDEBUG(failReason = "store to globally visible memory");
                        treeIsHoistable    = false;
                        m_beforeSideEffect = false;
                    }
                }
            }

            // If this 'tree' is hoistable then we return and the caller will
            // decide to hoist it as part of larger hoistable expression.
            //
            if (!treeIsHoistable && treeHasHoistableChildren)
            {
                // The current tree is not hoistable but it has hoistable children that we need
                // to hoist now.
                //
                // In order to preserve the original execution order, we also need to hoist any
                // other hoistable trees that we encountered so far.
                // At this point the stack contains (in top to bottom order):
                //   - the current node's children
                //   - the current node
                //   - ancestors of the current node and some of their descendants
                //
                // The ancestors have not been visited yet in post order so they're not hoistable
                // (and they cannot become hoistable because the current node is not) but some of
                // their descendants may have already been traversed and be hoistable.
                //
                // The execution order is actually bottom to top so we'll start hoisting from
                // the bottom of the stack, skipping the current node (which is expected to not
                // be hoistable).
                //
                // Note that the treeHasHoistableChildren check avoids unnecessary stack traversing
                // and also prevents hoisting trees too early. If the current tree is not hoistable
                // and it doesn't have any hoistable children then there's no point in hoisting any
                // other trees. Doing so would interfere with the cctor dependent case, where the
                // cctor dependent node is initially not hoistable and may become hoistable later,
                // when its parent comma node is visited.
                //
                // TODO-CQ: Ideally, we should be hoisting all the nodes having side-effects in execution
                // order as well as the ones that don't have side-effects at all. However, currently, we
                // just restrict hoisting a node(s) (that are children of `comma`) if one of the siblings
                // (which is executed before the given node) has side-effects (exceptions). Descendants
                // of ancestors might have side-effects and we might hoist nodes past them. This needs
                // to be addressed properly.
                bool visitedCurr = false;
                bool isCommaTree = tree->OperIs(GT_COMMA);
                bool hasExcep    = false;
                for (int i = 0; i < m_valueStack.Height(); i++)
                {
                    Value& value = m_valueStack.BottomRef(i);

                    if (value.m_hoistable)
                    {
                        assert(value.Node() != tree);

                        if (IsHoistableOverExcepSibling(value.Node(), hasExcep))
                        {
                            m_compiler->optHoistCandidate(value.Node(), m_currentBlock, m_loopNum, m_hoistContext);
                        }

                        // Don't hoist this tree again.
                        value.m_hoistable = false;
                        value.m_invariant = false;
                    }
                    else if (value.Node() != tree)
                    {
                        if (visitedCurr && isCommaTree)
                        {
                            // If we have visited current tree, now we are visiting children.
                            // For GT_COMMA nodes, we want to track if any children throws and
                            // should not hoist further children past it.
                            hasExcep = (tree->gtFlags & GTF_EXCEPT) != 0;
                        }
                        JITDUMP("      [%06u] %s: %s\n", dspTreeID(value.Node()),
                                value.m_invariant ? "not hoistable" : "not invariant", value.m_failReason);
                    }
                    else
                    {
                        visitedCurr = true;
                        JITDUMP("      [%06u] not hoistable : current node\n", dspTreeID(value.Node()));
                    }
                }
            }

            m_valueStack.Pop(childCount);

            Value& top = m_valueStack.TopRef();
            assert(top.Node() == tree);
            top.m_hoistable      = treeIsHoistable;
            top.m_cctorDependent = treeIsCctorDependent;
            top.m_invariant      = treeIsInvariant;

#ifdef DEBUG
            if (!top.m_invariant || !top.m_hoistable)
            {
                top.m_failReason = failReason;
            }
#endif

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    LoopDsc* loopDsc = &optLoopTable[loopNum];
    assert(blocks->Top() == loopDsc->lpEntry);

    HoistVisitor visitor(this, loopNum, hoistContext);

    while (!blocks->Empty())
    {
        BasicBlock* block       = blocks->Pop();
        weight_t    blockWeight = block->getBBWeight(this);

        JITDUMP("\n    optHoistLoopBlocks " FMT_BB " (weight=%6s) of loop " FMT_LP " <" FMT_BB ".." FMT_BB ">\n",
                block->bbNum, refCntWtd2str(blockWeight, /* padForDecimalPlaces */ true), loopNum,
                loopDsc->lpTop->bbNum, loopDsc->lpBottom->bbNum);

        if (blockWeight < (BB_UNITY_WEIGHT / 10))
        {
            JITDUMP("      block weight is too small to perform hoisting.\n");
            continue;
        }

        visitor.HoistBlock(block);
    }

    hoistContext->ResetHoistedInCurLoop();
}

void Compiler::optHoistCandidate(GenTree* tree, BasicBlock* treeBb, unsigned lnum, LoopHoistContext* hoistCtxt)
{
    assert(lnum != BasicBlock::NOT_IN_LOOP);

    // It must pass the hoistable profitablity tests for this loop level
    if (!optIsProfitableToHoistTree(tree, lnum))
    {
        JITDUMP("   ... not profitable to hoist\n");
        return;
    }

    if (hoistCtxt->GetHoistedInCurLoop(this)->Lookup(tree->gtVNPair.GetLiberal()))
    {
        // already hoisted this expression in the current loop, so don't hoist this expression.

        JITDUMP("      [%06u] ... already hoisted " FMT_VN " in " FMT_LP "\n ", dspTreeID(tree),
                tree->gtVNPair.GetLiberal(), lnum);
        return;
    }

    // We should already have a pre-header for the loop.
    INDEBUG(optLoopTable[lnum].lpValidatePreHeader());

    // If the block we're hoisting from and the pre-header are in different EH regions, don't hoist.
    // TODO: we could probably hoist things that won't raise exceptions, such as constants.
    if (!BasicBlock::sameTryRegion(optLoopTable[lnum].lpHead, treeBb))
    {
        JITDUMP("   ... not hoisting in " FMT_LP ", eh region constraint (pre-header try index %d, candidate " FMT_BB
                " try index %d\n",
                lnum, optLoopTable[lnum].lpHead->bbTryIndex, treeBb->bbNum, treeBb->bbTryIndex);
        return;
    }

    // Expression can be hoisted
    optPerformHoistExpr(tree, treeBb, lnum);

    // Increment lpHoistedExprCount or lpHoistedFPExprCount
    if (!varTypeIsFloating(tree->TypeGet()))
    {
        optLoopTable[lnum].lpHoistedExprCount++;
#ifndef TARGET_64BIT
        // For our 32-bit targets Long types take two registers.
        if (varTypeIsLong(tree->TypeGet()))
        {
            optLoopTable[lnum].lpHoistedExprCount++;
        }
#endif
    }
    else // Floating point expr hoisted
    {
        optLoopTable[lnum].lpHoistedFPExprCount++;
    }

    // Record the hoisted expression in hoistCtxt
    hoistCtxt->GetHoistedInCurLoop(this)->Set(tree->gtVNPair.GetLiberal(), true);
}

bool Compiler::optVNIsLoopInvariant(ValueNum vn, unsigned lnum, VNSet* loopVnInvariantCache)
{
    // If it is not a VN, is not loop-invariant.
    if (vn == ValueNumStore::NoVN)
    {
        return false;
    }

    // We'll always short-circuit constants.
    if (vnStore->IsVNConstant(vn) || vn == vnStore->VNForVoid())
    {
        return true;
    }

    // If we've done this query previously, don't repeat.
    bool previousRes = false;
    if (loopVnInvariantCache->Lookup(vn, &previousRes))
    {
        return previousRes;
    }

    bool      res = true;
    VNFuncApp funcApp;
    if (vnStore->GetVNFunc(vn, &funcApp))
    {
        if (funcApp.m_func == VNF_PhiDef)
        {
            // Is the definition within the loop?  If so, is not loop-invariant.
            unsigned      lclNum = funcApp.m_args[0];
            unsigned      ssaNum = funcApp.m_args[1];
            LclSsaVarDsc* ssaDef = lvaTable[lclNum].GetPerSsaData(ssaNum);
            res                  = !optLoopContains(lnum, ssaDef->GetBlock()->bbNatLoopNum);
        }
        else if (funcApp.m_func == VNF_PhiMemoryDef)
        {
            BasicBlock* defnBlk = reinterpret_cast<BasicBlock*>(vnStore->ConstantValue<ssize_t>(funcApp.m_args[0]));
            res                 = !optLoopContains(lnum, defnBlk->bbNatLoopNum);
        }
        else if (funcApp.m_func == VNF_MemOpaque)
        {
            const unsigned vnLoopNum = funcApp.m_args[0];

            // Check for the special "ambiguous" loop MemOpaque VN.
            // This is considered variant in every loop.
            //
            if (vnLoopNum == BasicBlock::MAX_LOOP_NUM)
            {
                res = false;
            }
            else
            {
                res = !optLoopContains(lnum, vnLoopNum);
            }
        }
        else
        {
            for (unsigned i = 0; i < funcApp.m_arity; i++)
            {
                // 4th arg of mapStore identifies the loop where the store happens.
                //
                if (funcApp.m_func == VNF_MapStore)
                {
                    assert(funcApp.m_arity == 4);

                    if (i == 3)
                    {
                        const unsigned vnLoopNum = funcApp.m_args[3];
                        res                      = !optLoopContains(lnum, vnLoopNum);
                        break;
                    }
                }

                // TODO-CQ: We need to either make sure that *all* VN functions
                // always take VN args, or else have a list of arg positions to exempt, as implicitly
                // constant.
                if (!optVNIsLoopInvariant(funcApp.m_args[i], lnum, loopVnInvariantCache))
                {
                    res = false;
                    break;
                }
            }
        }
    }

    loopVnInvariantCache->Set(vn, res);
    return res;
}

//------------------------------------------------------------------------------
// fgCreateLoopPreHeader: Creates a pre-header block for the given loop.
// A pre-header is a block outside the loop that falls through or branches to the loop
// entry block. It is the only non-loop predecessor block to the entry block (thus, it
// dominates the entry block). The pre-header replaces the current lpHead in the loop table.
// The pre-header will be placed immediately before the loop top block, which is the first
// block of the loop in program order.
//
// Once a loop has a pre-header, calling this function will immediately return without
// creating another.
//
// If there already exists a block that meets the pre-header requirements, that block is marked
// as a pre-header, and no flow graph modification is made.
//
// A loop with a pre-header has the flag LPFLG_HAS_PREHEAD, and its pre-header block has the flag BBF_LOOP_PREHEADER.
//
// Note that the pre-header block can be in a different EH region from blocks in the loop, including the
// entry block. Code doing hoisting is required to check the EH legality of hoisting to the pre-header
// before doing so.
//
// Since the flow graph has changed, if needed, fgUpdateChangedFlowGraph() should be called after this
// to update the block numbers, reachability, and dominators. The loop table does not need to be rebuilt.
// The new pre-header block does have a copy of the previous 'head' reachability set, but the pre-header
// itself doesn't exist in any reachability/dominator sets. `fgDominate` has code to specifically
// handle queries about the pre-header dominating other blocks, even without re-computing dominators.
// The preds lists have been maintained.
//
// The code does not depend on the order of the BasicBlock bbNum.
//
// Arguments:
//    lnum  - loop index
//
// Returns:
//    true if new pre-header was created
//
bool Compiler::fgCreateLoopPreHeader(unsigned lnum)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgCreateLoopPreHeader for " FMT_LP "\n", lnum);
    }
#endif // DEBUG

    LoopDsc& loop = optLoopTable[lnum];

    // Have we already created a loop pre-header block?

    if (loop.lpFlags & LPFLG_HAS_PREHEAD)
    {
        JITDUMP("   pre-header already exists\n");
        INDEBUG(loop.lpValidatePreHeader());
        return false;
    }

    // Assert that we haven't created SSA. It is assumed that we create all loop pre-headers before building SSA.
    assert(fgSsaPassesCompleted == 0);

    BasicBlock* head  = loop.lpHead;
    BasicBlock* top   = loop.lpTop;
    BasicBlock* entry = loop.lpEntry;

    // Ensure that lpHead always dominates lpEntry

    noway_assert(fgDominate(head, entry));

    // If `head` is already a valid pre-header, then mark it so.
    if (head->GetUniqueSucc() == entry)
    {
        // The loop entry must have a single non-loop predecessor, which is the pre-header.
        bool loopHasProperEntryBlockPreds = true;
        for (BasicBlock* const predBlock : entry->PredBlocks())
        {
            if (head == predBlock)
            {
                continue;
            }
            const bool intraLoopPred = optLoopContains(lnum, predBlock->bbNatLoopNum);
            if (!intraLoopPred)
            {
                loopHasProperEntryBlockPreds = false;
                break;
            }
        }
        if (loopHasProperEntryBlockPreds)
        {
            // Does this existing region have the same EH region index that we will use when we create the pre-header?
            // If not, we want to create a new pre-header with the expected region.
            bool headHasCorrectEHRegion = false;
            if ((top->bbFlags & BBF_TRY_BEG) != 0)
            {
                assert(top->hasTryIndex());
                unsigned newTryIndex     = ehTrueEnclosingTryIndexIL(top->getTryIndex());
                unsigned compareTryIndex = head->hasTryIndex() ? head->getTryIndex() : EHblkDsc::NO_ENCLOSING_INDEX;
                headHasCorrectEHRegion   = newTryIndex == compareTryIndex;
            }
            else
            {
                headHasCorrectEHRegion = BasicBlock::sameTryRegion(head, top);
            }

            if (headHasCorrectEHRegion)
            {
                JITDUMP("   converting existing header " FMT_BB " into pre-header\n", head->bbNum);
                loop.lpFlags |= LPFLG_HAS_PREHEAD;
                assert((head->bbFlags & BBF_LOOP_PREHEADER) == 0); // It isn't already a loop pre-header
                head->bbFlags |= BBF_LOOP_PREHEADER;
                INDEBUG(loop.lpValidatePreHeader());
                INDEBUG(fgDebugCheckLoopTable());
                return false;
            }
            else
            {
                JITDUMP("   existing head " FMT_BB " doesn't have correct EH region\n", head->bbNum);
            }
        }
        else
        {
            JITDUMP("   existing head " FMT_BB " isn't unique non-loop predecessor of loop entry\n", head->bbNum);
        }
    }
    else
    {
        JITDUMP("   existing head " FMT_BB " doesn't have unique successor branching to loop entry\n", head->bbNum);
    }

    // Allocate a new basic block for the pre-header.

    const bool isTopEntryLoop = loop.lpIsTopEntry();

    BasicBlock* preHead = bbNewBasicBlock(isTopEntryLoop ? BBJ_NONE : BBJ_ALWAYS);
    preHead->bbFlags |= BBF_INTERNAL | BBF_LOOP_PREHEADER;

    if (!isTopEntryLoop)
    {
        preHead->bbJumpDest = entry;
    }

    // Must set IL code offset
    preHead->bbCodeOffs = top->bbCodeOffs;

    // Set the default value of the preHead weight in case we don't have
    // valid profile data and since this blocks weight is just an estimate
    // we clear any BBF_PROF_WEIGHT flag that we may have picked up from head.
    //
    preHead->inheritWeight(head);
    preHead->bbFlags &= ~BBF_PROF_WEIGHT;

    // Copy the bbReach set from head for the new preHead block
    preHead->bbReach = BlockSetOps::MakeEmpty(this);
    BlockSetOps::Assign(this, preHead->bbReach, head->bbReach);
    // Also include 'head' in the preHead bbReach set
    BlockSetOps::AddElemD(this, preHead->bbReach, head->bbNum);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nCreated PreHeader (" FMT_BB ") for loop " FMT_LP " (" FMT_BB " - " FMT_BB, preHead->bbNum, lnum,
               top->bbNum, loop.lpBottom->bbNum);
        if (!isTopEntryLoop)
        {
            printf(", entry " FMT_BB, entry->bbNum);
        }
        printf("), with weight = %s\n", refCntWtd2str(preHead->getBBWeight(this)));
    }
#endif

    // The preheader block is part of the containing loop (if any).
    preHead->bbNatLoopNum = loop.lpParent;

    if (fgIsUsingProfileWeights() && (head->bbJumpKind == BBJ_COND))
    {
        if ((head->bbWeight == BB_ZERO_WEIGHT) || (entry->bbWeight == BB_ZERO_WEIGHT))
        {
            preHead->bbWeight = BB_ZERO_WEIGHT;
            preHead->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            // Allow for either the fall-through or branch to target 'entry'.
            BasicBlock* skipLoopBlock;
            if (head->bbNext == entry)
            {
                skipLoopBlock = head->bbJumpDest;
            }
            else
            {
                skipLoopBlock = head->bbNext;
            }
            assert(skipLoopBlock != entry);

            bool allValidProfileWeights =
                (head->hasProfileWeight() && skipLoopBlock->hasProfileWeight() && entry->hasProfileWeight());

            if (allValidProfileWeights)
            {
                weight_t loopEnteredCount = 0;
                weight_t loopSkippedCount = 0;
                bool     useEdgeWeights   = fgHaveValidEdgeWeights;

                if (useEdgeWeights)
                {
                    const FlowEdge* edgeToEntry    = fgGetPredForBlock(entry, head);
                    const FlowEdge* edgeToSkipLoop = fgGetPredForBlock(skipLoopBlock, head);
                    noway_assert(edgeToEntry != nullptr);
                    noway_assert(edgeToSkipLoop != nullptr);

                    loopEnteredCount = (edgeToEntry->edgeWeightMin() + edgeToEntry->edgeWeightMax()) / 2.0;
                    loopSkippedCount = (edgeToSkipLoop->edgeWeightMin() + edgeToSkipLoop->edgeWeightMax()) / 2.0;

                    // Watch out for cases where edge weights were not properly maintained
                    // so that it appears no profile flow enters the loop.
                    //
                    useEdgeWeights = !fgProfileWeightsConsistent(loopEnteredCount, BB_ZERO_WEIGHT);
                }

                if (!useEdgeWeights)
                {
                    loopEnteredCount = entry->bbWeight;
                    loopSkippedCount = skipLoopBlock->bbWeight;
                }

                weight_t loopTakenRatio = loopEnteredCount / (loopEnteredCount + loopSkippedCount);

                JITDUMP("%s edge weights; loopEnterCount " FMT_WT " loopSkipCount " FMT_WT " taken ratio " FMT_WT "\n",
                        fgHaveValidEdgeWeights ? (useEdgeWeights ? "valid" : "ignored") : "invalid", loopEnteredCount,
                        loopSkippedCount, loopTakenRatio);

                // Calculate a good approximation of the preHead's block weight
                weight_t preHeadWeight = (head->bbWeight * loopTakenRatio);
                preHead->setBBProfileWeight(preHeadWeight);
                noway_assert(!preHead->isRunRarely());
            }
        }
    }

    // Link in the preHead block
    fgInsertBBbefore(top, preHead);

    // In which EH region should the pre-header live?
    //
    // The pre-header block is added immediately before `top`.
    //
    // The `top` block cannot be the first block of a filter or handler: `top` must have a back-edge from a
    // BBJ_COND or BBJ_ALWAYS within the loop, and a filter or handler cannot be branched to like that.
    //
    // The `top` block can be the first block of a `try` region, and you can fall into or branch to the
    // first block of a `try` region. (For top-entry loops, `top` will both be the target of a back-edge
    // and a fall-through from the previous block.)
    //
    // If the `top` block is NOT the first block of a `try` region, the pre-header can simply extend the
    // `top` block region.
    //
    // If the `top` block IS the first block of a `try`, we find its parent region and use that. For mutual-protect
    // regions, we need to find the actual parent, as the block stores the most "nested" mutual region. For
    // non-mutual-protect regions, due to EH canonicalization, we are guaranteed that no other EH regions begin
    // on the same block, so looking to just the parent is sufficient. Note that we can't just extend the EH
    // region of `top` to the pre-header, because `top` will still be the target of backward branches from
    // within the loop. If those backward branches come from outside the `try` (say, only the top half of the loop
    // is a `try` region), then we can't branch to a non-first `try` region block (you always must entry the `try`
    // in the first block).
    //
    // Note that hoisting any code out of a try region, for example, to a pre-header block in a different
    // EH region, needs to ensure that no exceptions will be thrown.

    assert(!fgIsFirstBlockOfFilterOrHandler(top));

    if ((top->bbFlags & BBF_TRY_BEG) != 0)
    {
        // `top` is the beginning of a try block. Figure out the EH region to use.
        assert(top->hasTryIndex());
        unsigned short newTryIndex = (unsigned short)ehTrueEnclosingTryIndexIL(top->getTryIndex());
        if (newTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
        {
            // No EH try index.
            preHead->clearTryIndex();
        }
        else
        {
            preHead->setTryIndex(newTryIndex);
        }

        // What handler region to use? Use the same handler region as `top`.
        preHead->copyHndIndex(top);
    }
    else
    {
        // `top` is not the beginning of a try block. Just extend the EH region to the pre-header.
        // We don't need to call `fgExtendEHRegionBefore()` because all the special handling that function
        // does it to account for `top` being the first block of a `try` or handler region, which we know
        // is not true.

        preHead->copyEHRegion(top);
    }

    // TODO-CQ: set dominators for this block, to allow loop optimizations requiring them
    //        (e.g: hoisting expression in a loop with the same 'head' as this one)

    // Update the loop table

    loop.lpHead = preHead;
    loop.lpFlags |= LPFLG_HAS_PREHEAD;

    // The new block becomes the 'head' of the loop - update bbRefs and bbPreds.
    // All non-loop predecessors of 'entry' now jump to 'preHead'.

    preHead->bbRefs       = 0;
    bool checkNestedLoops = false;

    for (BasicBlock* const predBlock : entry->PredBlocks())
    {
        // Is the predBlock in the loop?
        //
        // We want to use:
        //    const bool intraLoopPred = loop.lpContains(predBlock);
        // but we can't depend on the bbNum ordering.
        //
        // Previously, this code wouldn't redirect predecessors dominated by the entry. However, that can
        // lead to a case where non-loop predecessor is dominated by the loop entry, and that predecessor
        // continues to branch to the entry, not the new pre-header. This is normally ok for hoisting
        // because it will introduce an SSA PHI def within the loop, which will inhibit hoisting. However,
        // it complicates the definition of what a pre-header is.

        const bool intraLoopPred = optLoopContains(lnum, predBlock->bbNatLoopNum);
        if (intraLoopPred)
        {
            if (predBlock != loop.lpBottom)
            {
                checkNestedLoops = true;
            }
            continue;
        }

        switch (predBlock->bbJumpKind)
        {
            case BBJ_NONE:
                // This 'entry' predecessor that isn't dominated by 'entry' must be outside the loop,
                // meaning it must be fall-through to 'entry', and we must have a top-entry loop.
                noway_assert((entry == top) && (predBlock == head) && (predBlock->bbNext == preHead));
                fgRemoveRefPred(entry, predBlock);
                fgAddRefPred(preHead, predBlock);
                break;

            case BBJ_COND:
                if (predBlock->bbJumpDest == entry)
                {
                    predBlock->bbJumpDest = preHead;
                    noway_assert(predBlock->bbNext != preHead);
                }
                else
                {
                    noway_assert((entry == top) && (predBlock == head) && (predBlock->bbNext == preHead));
                }
                fgRemoveRefPred(entry, predBlock);
                fgAddRefPred(preHead, predBlock);
                break;

            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                noway_assert(predBlock->bbJumpDest == entry);
                predBlock->bbJumpDest = preHead;
                fgRemoveRefPred(entry, predBlock);
                fgAddRefPred(preHead, predBlock);
                break;

            case BBJ_SWITCH:
                unsigned jumpCnt;
                jumpCnt = predBlock->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = predBlock->bbJumpSwt->bbsDstTab;

                do
                {
                    assert(*jumpTab);
                    if ((*jumpTab) == entry)
                    {
                        (*jumpTab) = preHead;

                        fgRemoveRefPred(entry, predBlock);
                        fgAddRefPred(preHead, predBlock);
                    }
                } while (++jumpTab, --jumpCnt);

                UpdateSwitchTableTarget(predBlock, entry, preHead);
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    FlowEdge* const edgeToPreHeader = fgGetPredForBlock(preHead, head);
    noway_assert(edgeToPreHeader != nullptr);
    edgeToPreHeader->setEdgeWeights(preHead->bbWeight, preHead->bbWeight, preHead);

    noway_assert(fgGetPredForBlock(entry, preHead) == nullptr);
    FlowEdge* const edgeFromPreHeader = fgAddRefPred(entry, preHead);
    edgeFromPreHeader->setEdgeWeights(preHead->bbWeight, preHead->bbWeight, entry);

    /*
        If we found at least one back-edge in the flowgraph pointing to the entry of the loop
        (other than the back-edge of the loop we are considering) then we likely have nested
        do-while loops with the same entry block and inserting the preheader block changes the head
        of all the nested loops. Now we will update this piece of information in the loop table, and
        mark all nested loops as having a preheader (the preheader block can be shared among all nested
        do-while loops with the same entry block).
    */
    if (checkNestedLoops)
    {
        for (unsigned l = 0; l < optLoopCount; l++)
        {
            if (optLoopTable[l].lpHead == head)
            {
                // loop.lpHead was already changed from 'head' to 'preHead'
                noway_assert(l != lnum);

                // If it shares head, it must be a top-entry loop that shares top.
                noway_assert(optLoopTable[l].lpEntry == top);

                optUpdateLoopHead(l, optLoopTable[l].lpHead, preHead);
                optLoopTable[l].lpFlags |= LPFLG_HAS_PREHEAD;
#ifdef DEBUG
                if (verbose)
                {
                    printf("Same PreHeader (" FMT_BB ") can be used for loop " FMT_LP " (" FMT_BB " - " FMT_BB ")\n\n",
                           preHead->bbNum, l, top->bbNum, optLoopTable[l].lpBottom->bbNum);
                }
#endif
            }
        }
    }

    // We added a new block and altered the preds list; make sure the flow graph has been marked as being modified.
    assert(fgModified);

#ifdef DEBUG
    fgDebugCheckBBlist();
    fgVerifyHandlerTab();
    fgDebugCheckLoopTable();

    if (verbose)
    {
        JITDUMP("*************** After fgCreateLoopPreHeader for " FMT_LP "\n", lnum);
        fgDispBasicBlocks();
        fgDispHandlerTab();
        optPrintLoopTable();
    }
#endif

    return true;
}

bool Compiler::optBlockIsLoopEntry(BasicBlock* blk, unsigned* pLnum)
{
    for (unsigned lnum = blk->bbNatLoopNum; lnum != BasicBlock::NOT_IN_LOOP; lnum = optLoopTable[lnum].lpParent)
    {
        if (optLoopTable[lnum].lpIsRemoved())
        {
            continue;
        }
        if (optLoopTable[lnum].lpEntry == blk)
        {
            *pLnum = lnum;
            return true;
        }
    }
    return false;
}

void Compiler::optComputeLoopSideEffects()
{
    unsigned lnum;
    for (lnum = 0; lnum < optLoopCount; lnum++)
    {
        VarSetOps::AssignNoCopy(this, optLoopTable[lnum].lpVarInOut, VarSetOps::MakeEmpty(this));
        VarSetOps::AssignNoCopy(this, optLoopTable[lnum].lpVarUseDef, VarSetOps::MakeEmpty(this));
        optLoopTable[lnum].lpFlags &= ~LPFLG_CONTAINS_CALL;
    }

    for (lnum = 0; lnum < optLoopCount; lnum++)
    {
        if (optLoopTable[lnum].lpIsRemoved())
        {
            continue;
        }

        if (optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP)
        { // Is outermost...
            optComputeLoopNestSideEffects(lnum);
        }
    }
}

void Compiler::optComputeInterestingVarSets()
{
    VarSetOps::AssignNoCopy(this, lvaFloatVars, VarSetOps::MakeEmpty(this));
#ifndef TARGET_64BIT
    VarSetOps::AssignNoCopy(this, lvaLongVars, VarSetOps::MakeEmpty(this));
#endif

    for (unsigned i = 0; i < lvaCount; i++)
    {
        LclVarDsc* varDsc = lvaGetDesc(i);
        if (varDsc->lvTracked)
        {
            if (varTypeUsesFloatReg(varDsc->lvType))
            {
                VarSetOps::AddElemD(this, lvaFloatVars, varDsc->lvVarIndex);
            }
#ifndef TARGET_64BIT
            else if (varTypeIsLong(varDsc->lvType))
            {
                VarSetOps::AddElemD(this, lvaLongVars, varDsc->lvVarIndex);
            }
#endif
        }
    }
}

void Compiler::optComputeLoopNestSideEffects(unsigned lnum)
{
    JITDUMP("optComputeLoopNestSideEffects for " FMT_LP "\n", lnum);
    assert(optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP); // Requires: lnum is outermost.
    for (BasicBlock* const bbInLoop : optLoopTable[lnum].LoopBlocks())
    {
        if (!optComputeLoopSideEffectsOfBlock(bbInLoop))
        {
            // When optComputeLoopSideEffectsOfBlock returns false, we encountered
            // a block that was moved into the loop range (by fgReorderBlocks),
            // but not marked correctly as being inside the loop.
            // We conservatively mark this loop (and any outer loops)
            // as having memory havoc side effects.
            //
            // Record that all loops containing this block have memory havoc effects.
            //
            optRecordLoopNestsMemoryHavoc(lnum, fullMemoryKindSet);

            // All done, no need to keep visiting more blocks
            break;
        }
    }
}

void Compiler::optRecordLoopNestsMemoryHavoc(unsigned lnum, MemoryKindSet memoryHavoc)
{
    // We should start out with 'lnum' set to a valid natural loop index
    assert(lnum != BasicBlock::NOT_IN_LOOP);

    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        for (MemoryKind memoryKind : allMemoryKinds())
        {
            if ((memoryHavoc & memoryKindSet(memoryKind)) != 0)
            {
                optLoopTable[lnum].lpLoopHasMemoryHavoc[memoryKind] = true;
            }
        }

        // Move lnum to the next outtermost loop that we need to mark
        lnum = optLoopTable[lnum].lpParent;
    }
}

bool Compiler::optComputeLoopSideEffectsOfBlock(BasicBlock* blk)
{
    unsigned mostNestedLoop = blk->bbNatLoopNum;
    JITDUMP("optComputeLoopSideEffectsOfBlock " FMT_BB ", mostNestedLoop %d\n", blk->bbNum, mostNestedLoop);
    if (mostNestedLoop == BasicBlock::NOT_IN_LOOP)
    {
        return false;
    }
    AddVariableLivenessAllContainingLoops(mostNestedLoop, blk);

    // MemoryKinds for which an in-loop call or store has arbitrary effects.
    MemoryKindSet memoryHavoc = emptyMemoryKindSet;

    // Now iterate over the remaining statements, and their trees.
    for (Statement* const stmt : blk->NonPhiStatements())
    {
        for (GenTree* const tree : stmt->TreeList())
        {
            genTreeOps oper = tree->OperGet();

            // Even after we set memoryHavoc we still may want to know if a loop contains calls
            if (memoryHavoc == fullMemoryKindSet)
            {
                if (oper == GT_CALL)
                {
                    // Record that this loop contains a call
                    AddContainsCallAllContainingLoops(mostNestedLoop);
                }

                // If we just set LPFLG_CONTAINS_CALL or it was previously set
                if (optLoopTable[mostNestedLoop].lpFlags & LPFLG_CONTAINS_CALL)
                {
                    // We can early exit after both memoryHavoc and LPFLG_CONTAINS_CALL are both set to true.
                    break;
                }

                // We are just looking for GT_CALL nodes after memoryHavoc was set.
                continue;
            }

            // otherwise memoryHavoc is not set for at least one heap ID
            assert(memoryHavoc != fullMemoryKindSet);

            // This body is a distillation of the memory side-effect code of value numbering.
            // We also do a very limited analysis if byref PtrTo values, to cover some cases
            // that the compiler creates.

            if (oper == GT_ASG)
            {
                GenTree* lhs = tree->gtGetOp1();

                if (lhs->OperIsIndir())
                {
                    GenTree* arg = lhs->AsIndir()->Addr()->gtEffectiveVal(/*commaOnly*/ true);

                    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
                    {
                        memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        continue;
                    }

                    if (arg->TypeGet() == TYP_BYREF && arg->OperGet() == GT_LCL_VAR)
                    {
                        // If it's a local byref for which we recorded a value number, use that...
                        GenTreeLclVar* argLcl = arg->AsLclVar();
                        if (argLcl->HasSsaName())
                        {
                            ValueNum argVN =
                                lvaTable[argLcl->GetLclNum()].GetPerSsaData(argLcl->GetSsaNum())->m_vnPair.GetLiberal();
                            VNFuncApp funcApp;
                            if (argVN != ValueNumStore::NoVN && vnStore->GetVNFunc(argVN, &funcApp) &&
                                funcApp.m_func == VNF_PtrToArrElem)
                            {
                                assert(vnStore->IsVNHandle(funcApp.m_args[0]));
                                CORINFO_CLASS_HANDLE elemType =
                                    CORINFO_CLASS_HANDLE(vnStore->ConstantValue<size_t>(funcApp.m_args[0]));
                                AddModifiedElemTypeAllContainingLoops(mostNestedLoop, elemType);
                                // Don't set memoryHavoc for GcHeap below.  Do set memoryHavoc for ByrefExposed
                                // (conservatively assuming that a byref may alias the array element)
                                memoryHavoc |= memoryKindSet(ByrefExposed);
                                continue;
                            }
                        }
                        // Otherwise...
                        memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                    }
                    else
                    {
                        GenTreeArrAddr* arrAddr  = nullptr;
                        GenTree*        baseAddr = nullptr;
                        FieldSeq*       fldSeq   = nullptr;
                        ssize_t         offset   = 0;

                        if (arg->IsArrayAddr(&arrAddr))
                        {
                            // We will not collect "fldSeq" -- any modification to an S[], at
                            // any field of "S", will lose all information about the array type.
                            CORINFO_CLASS_HANDLE elemTypeEq =
                                EncodeElemType(arrAddr->GetElemType(), arrAddr->GetElemClassHandle());
                            AddModifiedElemTypeAllContainingLoops(mostNestedLoop, elemTypeEq);
                            // Conservatively assume byrefs may alias this array element
                            memoryHavoc |= memoryKindSet(ByrefExposed);
                        }
                        else if (arg->IsFieldAddr(this, &baseAddr, &fldSeq, &offset))
                        {
                            assert(fldSeq != nullptr);

                            FieldKindForVN fieldKind =
                                (baseAddr != nullptr) ? FieldKindForVN::WithBaseAddr : FieldKindForVN::SimpleStatic;
                            AddModifiedFieldAllContainingLoops(mostNestedLoop, fldSeq->GetFieldHandle(), fieldKind);
                            // Conservatively assume byrefs may alias this object.
                            memoryHavoc |= memoryKindSet(ByrefExposed);
                        }
                        else
                        {
                            memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        }
                    }
                }
                else // Otherwise, must be local lhs form.
                {
                    GenTreeLclVarCommon* lhsLcl = lhs->AsLclVarCommon();
                    ValueNum             rhsVN  = tree->AsOp()->gtOp2->gtVNPair.GetLiberal();
                    // If we gave the RHS a value number, propagate it.
                    if (lhsLcl->OperIs(GT_LCL_VAR) && (rhsVN != ValueNumStore::NoVN))
                    {
                        rhsVN = vnStore->VNNormalValue(rhsVN);
                        if (lhsLcl->HasSsaName())
                        {
                            lvaTable[lhsLcl->GetLclNum()]
                                .GetPerSsaData(lhsLcl->GetSsaNum())
                                ->m_vnPair.SetLiberal(rhsVN);
                        }
                    }
                    // If the local is address-exposed, count this as ByrefExposed havoc
                    if (lvaVarAddrExposed(lhsLcl->GetLclNum()))
                    {
                        memoryHavoc |= memoryKindSet(ByrefExposed);
                    }
                }
            }
            else // if (oper != GT_ASG)
            {
                switch (oper)
                {
                    case GT_COMMA:
                        tree->gtVNPair = tree->AsOp()->gtOp2->gtVNPair;
                        break;

                    // Is it an addr of an array index expression?
                    case GT_ARR_ADDR:
                    {
                        CORINFO_CLASS_HANDLE elemTypeEq =
                            EncodeElemType(tree->AsArrAddr()->GetElemType(), tree->AsArrAddr()->GetElemClassHandle());
                        ValueNum elemTypeEqVN = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);

                        // Label this with a "dummy" PtrToArrElem so that we pick it up when looking at the ASG.
                        ValueNum ptrToArrElemVN =
                            vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem, elemTypeEqVN, vnStore->VNForNull(),
                                               vnStore->VNForNull(), vnStore->VNForNull());
                        tree->gtVNPair.SetBoth(ptrToArrElemVN);
                    }
                    break;

#ifdef FEATURE_HW_INTRINSICS
                    case GT_HWINTRINSIC:
                    {
                        GenTreeHWIntrinsic* hwintrinsic = tree->AsHWIntrinsic();
                        NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

                        if (hwintrinsic->OperIsMemoryStoreOrBarrier())
                        {
                            // For barriers, we model the behavior after GT_MEMORYBARRIER
                            memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        }
                        break;
                    }
#endif // FEATURE_HW_INTRINSICS

                    case GT_LOCKADD:
                    case GT_XORR:
                    case GT_XAND:
                    case GT_XADD:
                    case GT_XCHG:
                    case GT_CMPXCHG:
                    case GT_MEMORYBARRIER:
                    case GT_STORE_DYN_BLK:
                    {
                        memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                    }
                    break;

                    case GT_CALL:
                    {
                        GenTreeCall* call = tree->AsCall();

                        // Record that this loop contains a call
                        AddContainsCallAllContainingLoops(mostNestedLoop);

                        if (call->gtCallType == CT_HELPER)
                        {
                            CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);
                            if (s_helperCallProperties.MutatesHeap(helpFunc))
                            {
                                memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                            }
                            else if (s_helperCallProperties.MayRunCctor(helpFunc))
                            {
                                // If the call is labeled as "Hoistable", then we've checked the
                                // class that would be constructed, and it is not precise-init, so
                                // the cctor will not be run by this call.  Otherwise, it might be,
                                // and might have arbitrary side effects.
                                if ((tree->gtFlags & GTF_CALL_HOISTABLE) == 0)
                                {
                                    memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                                }
                            }
                        }
                        else
                        {
                            memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        }
                        break;
                    }

                    default:
                        // All other gtOper node kinds, leave 'memoryHavoc' unchanged (i.e. false)
                        assert(!tree->OperRequiresAsgFlag());
                        break;
                }
            }
        }

        // Clear the Value Number from the statement root node, if it was set above. This is to
        // ensure that for those blocks that are unreachable, which we still handle in this loop
        // but not during Value Numbering, fgDebugCheckExceptionSets() will skip the trees. Ideally,
        // we wouldn't be touching the gtVNPair at all (here) before actual Value Numbering.
        stmt->GetRootNode()->gtVNPair.SetBoth(ValueNumStore::NoVN);
    }

    if (memoryHavoc != emptyMemoryKindSet)
    {
        // Record that all loops containing this block have this kind of memoryHavoc effects.
        optRecordLoopNestsMemoryHavoc(mostNestedLoop, memoryHavoc);
    }
    return true;
}

// Marks the containsCall information to "lnum" and any parent loops.
void Compiler::AddContainsCallAllContainingLoops(unsigned lnum)
{

#if FEATURE_LOOP_ALIGN
    // If this is the inner most loop, reset the LOOP_ALIGN flag
    // because a loop having call will not likely to benefit from
    // alignment
    if (optLoopTable[lnum].lpChild == BasicBlock::NOT_IN_LOOP)
    {
        BasicBlock* top = optLoopTable[lnum].lpTop;

        top->unmarkLoopAlign(this DEBUG_ARG("Loop with call"));
    }
#endif

    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].lpFlags |= LPFLG_CONTAINS_CALL;
        lnum = optLoopTable[lnum].lpParent;
    }
}

// Adds the variable liveness information for 'blk' to 'this' LoopDsc
void Compiler::LoopDsc::AddVariableLiveness(Compiler* comp, BasicBlock* blk)
{
    VarSetOps::UnionD(comp, this->lpVarInOut, blk->bbLiveIn);
    VarSetOps::UnionD(comp, this->lpVarInOut, blk->bbLiveOut);

    VarSetOps::UnionD(comp, this->lpVarUseDef, blk->bbVarUse);
    VarSetOps::UnionD(comp, this->lpVarUseDef, blk->bbVarDef);
}

// Adds the variable liveness information for 'blk' to "lnum" and any parent loops.
void Compiler::AddVariableLivenessAllContainingLoops(unsigned lnum, BasicBlock* blk)
{
    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].AddVariableLiveness(this, blk);
        lnum = optLoopTable[lnum].lpParent;
    }
}

// Adds "fldHnd" to the set of modified fields of "lnum" and any parent loops.
void Compiler::AddModifiedFieldAllContainingLoops(unsigned lnum, CORINFO_FIELD_HANDLE fldHnd, FieldKindForVN fieldKind)
{
    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].AddModifiedField(this, fldHnd, fieldKind);
        lnum = optLoopTable[lnum].lpParent;
    }
}

// Adds "elemType" to the set of modified array element types of "lnum" and any parent loops.
void Compiler::AddModifiedElemTypeAllContainingLoops(unsigned lnum, CORINFO_CLASS_HANDLE elemClsHnd)
{
    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].AddModifiedElemType(this, elemClsHnd);
        lnum = optLoopTable[lnum].lpParent;
    }
}

//------------------------------------------------------------------------------
// optRemoveRangeCheck : Given an indexing node, mark it as not needing a range check.
//
// Arguments:
//    check  -  Range check tree, the raw CHECK node (ARRAY, SIMD or HWINTRINSIC).
//    comma  -  GT_COMMA to which the "check" belongs, "nullptr" if the check is a standalone one.
//    stmt   -  Statement the indexing nodes belong to.
//
// Return Value:
//    Rewritten "check" - no-op if it has no side effects or the tree that contains them.
//
// Notes:
//    This method is capable of removing checks of two kinds: COMMA-based and standalone top-level
//    ones. In case of a COMMA-based check, "check" must be a non-null first operand of a non-null
//    COMMA. In case of a standalone check, "comma" must be null and "check" - "stmt"'s root.
//
//    Does not keep costs or node threading up to date, but does update side effect flags.
//
GenTree* Compiler::optRemoveRangeCheck(GenTreeBoundsChk* check, GenTree* comma, Statement* stmt)
{
#if !REARRANGE_ADDS
    noway_assert(!"can't remove range checks without REARRANGE_ADDS right now");
#endif

    noway_assert(stmt != nullptr);
    noway_assert((comma != nullptr && comma->OperIs(GT_COMMA) && comma->gtGetOp1() == check) ||
                 (check != nullptr && check->OperIs(GT_BOUNDS_CHECK) && comma == nullptr));
    noway_assert(check->OperIs(GT_BOUNDS_CHECK));

    GenTree* tree = comma != nullptr ? comma : check;

#ifdef DEBUG
    if (verbose)
    {
        printf("Before optRemoveRangeCheck:\n");
        gtDispTree(tree);
    }
#endif

    // Extract side effects
    GenTree* sideEffList = nullptr;
    gtExtractSideEffList(check, &sideEffList, GTF_ASG);

    if (sideEffList != nullptr)
    {
        // We've got some side effects.
        if (tree->OperIs(GT_COMMA))
        {
            // Make the comma handle them.
            tree->AsOp()->gtOp1 = sideEffList;
        }
        else
        {
            // Make the statement execute them instead of the check.
            stmt->SetRootNode(sideEffList);
            tree = sideEffList;
        }
    }
    else
    {
        check->gtBashToNOP();
    }

    if (tree->OperIs(GT_COMMA))
    {
        // TODO-CQ: We should also remove the GT_COMMA, but in any case we can no longer CSE the GT_COMMA.
        tree->gtFlags |= GTF_DONT_CSE;
    }

    gtUpdateSideEffects(stmt, tree);

#ifdef DEBUG
    if (verbose)
    {
        // gtUpdateSideEffects can update the side effects for ancestors in the tree, so display the whole statement
        // tree, not just the sub-tree.
        printf("After optRemoveRangeCheck for [%06u]:\n", dspTreeID(tree));
        gtDispTree(stmt->GetRootNode());
    }
#endif

    return check;
}

//------------------------------------------------------------------------------
// optRemoveStandaloneRangeCheck : A thin wrapper over optRemoveRangeCheck that removes standalone checks.
//
// Arguments:
//    check - The standalone top-level CHECK node.
//    stmt  - The statement "check" is a root node of.
//
// Return Value:
//    If "check" has no side effects, it is retuned, bashed to a no-op.
//    If it has side effects, the tree that executes them is returned.
//
GenTree* Compiler::optRemoveStandaloneRangeCheck(GenTreeBoundsChk* check, Statement* stmt)
{
    assert(check != nullptr);
    assert(stmt != nullptr);
    assert(check == stmt->GetRootNode());

    return optRemoveRangeCheck(check, nullptr, stmt);
}

//------------------------------------------------------------------------------
// optRemoveCommaBasedRangeCheck : A thin wrapper over optRemoveRangeCheck that removes COMMA-based checks.
//
// Arguments:
//    comma - GT_COMMA of which the first operand is the CHECK to be removed.
//    stmt  - The statement "comma" belongs to.
//
void Compiler::optRemoveCommaBasedRangeCheck(GenTree* comma, Statement* stmt)
{
    assert(comma != nullptr && comma->OperIs(GT_COMMA));
    assert(stmt != nullptr);
    assert(comma->gtGetOp1()->OperIs(GT_BOUNDS_CHECK));

    optRemoveRangeCheck(comma->gtGetOp1()->AsBoundsChk(), comma, stmt);
}

/*****************************************************************************
 * Return the scale in an array reference, given a pointer to the
 * multiplication node.
 */

ssize_t Compiler::optGetArrayRefScaleAndIndex(GenTree* mul, GenTree** pIndex DEBUGARG(bool bRngChk))
{
    assert(mul);
    assert(mul->gtOper == GT_MUL || mul->gtOper == GT_LSH);
    assert(mul->AsOp()->gtOp2->IsCnsIntOrI());

    ssize_t scale = mul->AsOp()->gtOp2->AsIntConCommon()->IconValue();

    if (mul->gtOper == GT_LSH)
    {
        scale = ((ssize_t)1) << scale;
    }

    GenTree* index = mul->AsOp()->gtOp1;

    if (index->gtOper == GT_MUL && index->AsOp()->gtOp2->IsCnsIntOrI())
    {
        // case of two cascading multiplications for constant int (e.g.  * 20 morphed to * 5 * 4):
        // When index->gtOper is GT_MUL and index->AsOp()->gtOp2->gtOper is GT_CNS_INT (i.e. * 5),
        //     we can bump up the scale from 4 to 5*4, and then change index to index->AsOp()->gtOp1.
        // Otherwise, we cannot optimize it. We will simply keep the original scale and index.
        scale *= index->AsOp()->gtOp2->AsIntConCommon()->IconValue();
        index = index->AsOp()->gtOp1;
    }

    assert(!bRngChk || index->gtOper != GT_COMMA);

    if (pIndex)
    {
        *pIndex = index;
    }

    return scale;
}

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, unsigned> LclVarRefCounts;

//------------------------------------------------------------------------------------------
// optRemoveRedundantZeroInits: Remove redundant zero initializations.
//
// Notes:
//    This phase iterates over basic blocks starting with the first basic block until there is no unique
//    basic block successor or until it detects a loop. It keeps track of local nodes it encounters.
//    When it gets to an assignment to a local variable or a local field, it checks whether the assignment
//    is the first reference to the local (or to the parent of the local field), and, if so,
//    it may do one of two optimizations:
//      1. If the following conditions are true:
//            the local is untracked,
//            the rhs of the assignment is 0,
//            the local is guaranteed to be fully initialized in the prolog,
//         then the explicit zero initialization is removed.
//      2. If the following conditions are true:
//            the assignment is to a local (and not a field),
//            the local is not lvLiveInOutOfHndlr or no exceptions can be thrown between the prolog and the assignment,
//            either the local has no gc pointers or there are no gc-safe points between the prolog and the assignment,
//         then the local is marked with lvHasExplicitInit which tells the codegen not to insert zero initialization
//         for this local in the prolog.

void Compiler::optRemoveRedundantZeroInits()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optRemoveRedundantZeroInits()\n");
    }
#endif // DEBUG

    CompAllocator   allocator(getAllocator(CMK_ZeroInit));
    LclVarRefCounts refCounts(allocator);
    BitVecTraits    bitVecTraits(lvaCount, this);
    BitVec          zeroInitLocals = BitVecOps::MakeEmpty(&bitVecTraits);
    bool            hasGCSafePoint = false;
    bool            canThrow       = false;

    assert(fgNodeThreading == NodeThreading::AllTrees);

    for (BasicBlock* block = fgFirstBB; (block != nullptr) && ((block->bbFlags & BBF_MARKED) == 0);
         block             = block->GetUniqueSucc())
    {
        block->bbFlags |= BBF_MARKED;
        CompAllocator   allocator(getAllocator(CMK_ZeroInit));
        LclVarRefCounts defsInBlock(allocator);
        bool            removedTrackedDefs = false;
        for (Statement* stmt = block->FirstNonPhiDef(); stmt != nullptr;)
        {
            Statement* next = stmt->GetNextStmt();
            for (GenTree* const tree : stmt->TreeList())
            {
                if (((tree->gtFlags & GTF_CALL) != 0))
                {
                    hasGCSafePoint = true;
                }

                if ((tree->gtFlags & GTF_EXCEPT) != 0)
                {
                    canThrow = true;
                }

                switch (tree->gtOper)
                {
                    case GT_LCL_VAR:
                    case GT_LCL_FLD:
                    case GT_LCL_ADDR:
                    {
                        unsigned  lclNum    = tree->AsLclVarCommon()->GetLclNum();
                        unsigned* pRefCount = refCounts.LookupPointer(lclNum);
                        if (pRefCount != nullptr)
                        {
                            *pRefCount = (*pRefCount) + 1;
                        }
                        else
                        {
                            refCounts.Set(lclNum, 1);
                        }

                        if ((tree->gtFlags & GTF_VAR_DEF) == 0)
                        {
                            break;
                        }

                        // We need to count the number of tracked var defs in the block
                        // so that we can update block->bbVarDef if we remove any tracked var defs.

                        LclVarDsc* const lclDsc = lvaGetDesc(lclNum);
                        if (lclDsc->lvTracked)
                        {
                            unsigned* pDefsCount = defsInBlock.LookupPointer(lclNum);
                            if (pDefsCount != nullptr)
                            {
                                *pDefsCount = (*pDefsCount) + 1;
                            }
                            else
                            {
                                defsInBlock.Set(lclNum, 1);
                            }
                        }
                        else if (varTypeIsStruct(lclDsc) && ((tree->gtFlags & GTF_VAR_USEASG) == 0) &&
                                 lvaGetPromotionType(lclDsc) != PROMOTION_TYPE_NONE)
                        {
                            for (unsigned i = lclDsc->lvFieldLclStart; i < lclDsc->lvFieldLclStart + lclDsc->lvFieldCnt;
                                 ++i)
                            {
                                if (lvaGetDesc(i)->lvTracked)
                                {
                                    unsigned* pDefsCount = defsInBlock.LookupPointer(i);
                                    if (pDefsCount != nullptr)
                                    {
                                        *pDefsCount = (*pDefsCount) + 1;
                                    }
                                    else
                                    {
                                        defsInBlock.Set(i, 1);
                                    }
                                }
                            }
                        }

                        break;
                    }
                    // case GT_CALL:
                    // TODO-CQ: Need to remove redundant zero-inits for "return buffer".
                    // assert(!"Need to handle zero inits.\n");
                    // break;
                    case GT_ASG:
                    {
                        GenTreeOp* treeOp = tree->AsOp();

                        GenTreeLclVarCommon* lclVar;
                        bool                 isEntire;

                        if (!tree->DefinesLocal(this, &lclVar, &isEntire))
                        {
                            break;
                        }

                        const unsigned lclNum = lclVar->GetLclNum();

                        LclVarDsc* const lclDsc    = lvaGetDesc(lclNum);
                        unsigned*        pRefCount = refCounts.LookupPointer(lclNum);

                        // pRefCount can't be null because the local node on the lhs of the assignment
                        // must have already been seen.
                        assert(pRefCount != nullptr);
                        if (*pRefCount != 1)
                        {
                            break;
                        }

                        unsigned parentRefCount = 0;
                        if (lclDsc->lvIsStructField && refCounts.Lookup(lclDsc->lvParentLcl, &parentRefCount) &&
                            (parentRefCount != 0))
                        {
                            break;
                        }

                        unsigned fieldRefCount = 0;
                        if (lclDsc->lvPromoted)
                        {
                            for (unsigned i = lclDsc->lvFieldLclStart;
                                 (fieldRefCount == 0) && (i < lclDsc->lvFieldLclStart + lclDsc->lvFieldCnt); ++i)
                            {
                                refCounts.Lookup(i, &fieldRefCount);
                            }
                        }

                        if (fieldRefCount != 0)
                        {
                            break;
                        }

                        // The local hasn't been referenced before this assignment.
                        bool removedExplicitZeroInit = false;

                        if (treeOp->gtGetOp2()->IsIntegralConst(0))
                        {
                            bool bbInALoop  = (block->bbFlags & BBF_BACKWARD_JUMP) != 0;
                            bool bbIsReturn = block->bbJumpKind == BBJ_RETURN;

                            if (!bbInALoop || bbIsReturn)
                            {
                                bool neverTracked = lclDsc->IsAddressExposed() || lclDsc->lvPinned ||
                                                    (lclDsc->lvPromoted && varTypeIsStruct(lclDsc));

                                if (BitVecOps::IsMember(&bitVecTraits, zeroInitLocals, lclNum) ||
                                    (lclDsc->lvIsStructField &&
                                     BitVecOps::IsMember(&bitVecTraits, zeroInitLocals, lclDsc->lvParentLcl)) ||
                                    ((neverTracked || !isEntire) &&
                                     !fgVarNeedsExplicitZeroInit(lclNum, bbInALoop, bbIsReturn)))
                                {
                                    // We are guaranteed to have a zero initialization in the prolog or a
                                    // dominating explicit zero initialization and the local hasn't been redefined
                                    // between the prolog and this explicit zero initialization so the assignment
                                    // can be safely removed.
                                    if (tree == stmt->GetRootNode())
                                    {
                                        fgRemoveStmt(block, stmt);
                                        removedExplicitZeroInit      = true;
                                        lclDsc->lvSuppressedZeroInit = 1;

                                        if (lclDsc->lvTracked)
                                        {
                                            removedTrackedDefs   = true;
                                            unsigned* pDefsCount = defsInBlock.LookupPointer(lclNum);
                                            *pDefsCount          = (*pDefsCount) - 1;
                                        }
                                    }
                                }

                                if (isEntire)
                                {
                                    BitVecOps::AddElemD(&bitVecTraits, zeroInitLocals, lclNum);
                                }
                                *pRefCount = 0;
                            }
                        }

                        if (!removedExplicitZeroInit && isEntire && (!canThrow || !lclDsc->lvLiveInOutOfHndlr))
                        {
                            // If compMethodRequiresPInvokeFrame() returns true, lower may later
                            // insert a call to CORINFO_HELP_INIT_PINVOKE_FRAME which is a gc-safe point.
                            if (!lclDsc->HasGCPtr() ||
                                (!GetInterruptible() && !hasGCSafePoint && !compMethodRequiresPInvokeFrame()))
                            {
                                // The local hasn't been used and won't be reported to the gc between
                                // the prolog and this explicit initialization. Therefore, it doesn't
                                // require zero initialization in the prolog.
                                lclDsc->lvHasExplicitInit = 1;
                                lclVar->gtFlags |= GTF_VAR_EXPLICIT_INIT;
                                JITDUMP("Marking V%02u as having an explicit init\n", lclNum);
                            }
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
            stmt = next;
        }

        if (removedTrackedDefs)
        {
            for (const unsigned int lclNum : LclVarRefCounts::KeyIteration(&defsInBlock))
            {
                if (defsInBlock[lclNum] == 0)
                {
                    VarSetOps::RemoveElemD(this, block->bbVarDef, lvaGetDesc(lclNum)->lvVarIndex);
                }
            }
        }
    }

    for (BasicBlock* block = fgFirstBB; (block != nullptr) && ((block->bbFlags & BBF_MARKED) != 0);
         block             = block->GetUniqueSucc())
    {
        block->bbFlags &= ~BBF_MARKED;
    }
}

//------------------------------------------------------------------------
// optVNBasedDeadStoreRemoval: VN(value)-based dead store removal.
//
// The phase iterates over partial stores referenced by the SSA
// descriptors and deletes those which do not change the local's value.
//
// Return Value:
//    A suitable phase status.
//
PhaseStatus Compiler::optVNBasedDeadStoreRemoval()
{
#ifdef DEBUG
    static ConfigMethodRange JitEnableVNBasedDeadStoreRemovalRange;
    JitEnableVNBasedDeadStoreRemovalRange.EnsureInit(JitConfig.JitEnableVNBasedDeadStoreRemovalRange());

    if (!JitEnableVNBasedDeadStoreRemovalRange.Contains(info.compMethodHash()))
    {
        JITDUMP("VN-based dead store removal disabled by JitEnableVNBasedDeadStoreRemovalRange\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    bool madeChanges = false;

    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        if (!lvaInSsa(lclNum))
        {
            continue;
        }

        LclVarDsc* varDsc   = lvaGetDesc(lclNum);
        unsigned   defCount = varDsc->lvPerSsaData.GetCount();
        if (defCount <= 1)
        {
            continue;
        }

        for (unsigned defIndex = 1; defIndex < defCount; defIndex++)
        {
            LclSsaVarDsc* defDsc = varDsc->lvPerSsaData.GetSsaDefByIndex(defIndex);
            GenTreeOp*    store  = defDsc->GetAssignment();

            if (store != nullptr)
            {
                assert(store->OperIs(GT_ASG) && defDsc->m_vnPair.BothDefined());

                JITDUMP("Considering [%06u] for removal...\n", dspTreeID(store));

                GenTree* lhs = store->gtGetOp1();
                if (lhs->AsLclVarCommon()->GetLclNum() != lclNum)
                {
                    JITDUMP(" -- no; composite definition\n");
                    continue;
                }

                ValueNum oldStoreValue;
                if ((lhs->gtFlags & GTF_VAR_USEASG) == 0)
                {
                    LclSsaVarDsc* lastDefDsc = varDsc->lvPerSsaData.GetSsaDefByIndex(defIndex - 1);
                    if (lastDefDsc->GetBlock() != defDsc->GetBlock())
                    {
                        JITDUMP(" -- no; last def not in the same block\n");
                        continue;
                    }

                    if ((lhs->gtFlags & GTF_VAR_EXPLICIT_INIT) != 0)
                    {
                        // Removing explicit inits is not profitable for primitives and not safe for structs.
                        JITDUMP(" -- no; 'explicit init'\n");
                        continue;
                    }

                    // CQ heuristic: avoid removing defs of enregisterable locals where this is likely to
                    // make them "must-init", extending live ranges. Here we assume the first SSA def was
                    // the implicit "live-in" one, which is not guaranteed, but very likely.
                    if ((defIndex == 1) && (varDsc->TypeGet() != TYP_STRUCT))
                    {
                        JITDUMP(" -- no; first explicit def of a non-STRUCT local\n", lclNum);
                        continue;
                    }

                    oldStoreValue = lastDefDsc->m_vnPair.GetConservative();
                }
                else
                {
                    ValueNum oldLclValue = varDsc->GetPerSsaData(defDsc->GetUseDefSsaNum())->m_vnPair.GetConservative();
                    oldStoreValue =
                        vnStore->VNForLoad(VNK_Conservative, oldLclValue, lvaLclExactSize(lclNum), lhs->TypeGet(),
                                           lhs->AsLclFld()->GetLclOffs(), lhs->AsLclFld()->GetSize());
                }

                GenTree* rhs = store->gtGetOp2();
                ValueNum storeValue;
                if (lhs->TypeIs(TYP_STRUCT) && rhs->IsIntegralConst(0))
                {
                    storeValue = vnStore->VNForZeroObj(lhs->AsLclVarCommon()->GetLayout(this));
                }
                else
                {
                    storeValue = rhs->GetVN(VNK_Conservative);
                }

                if (oldStoreValue == storeValue)
                {
                    JITDUMP("Removed dead store:\n");
                    DISPTREE(store);

                    lhs->gtFlags &= ~(GTF_VAR_DEF | GTF_VAR_USEASG);

                    store->ChangeOper(GT_COMMA);
                    if (store->IsReverseOp())
                    {
                        std::swap(store->gtOp1, store->gtOp2);
                        store->ClearReverseOp();
                    }
                    store->gtType = store->gtGetOp2()->TypeGet();
                    store->SetAllEffectsFlags(store->gtOp1, store->gtOp2);
                    gtUpdateTreeAncestorsSideEffects(store);

                    madeChanges = true;
                }
                else
                {
                    JITDUMP(" -- no; not redundant\n");
                }
            }
        }
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// optAnyChildNotRemoved: Recursively check the child loops of a loop to see if any of them
// are still live (that is, not marked as LPFLG_REMOVED). This check is done when we are
// removing a parent, just to notify that there is something odd about leaving a live child.
//
// Arguments:
//      loopNum - the loop number to check
//
bool Compiler::optAnyChildNotRemoved(unsigned loopNum)
{
    assert(loopNum < optLoopCount);

    // Now recursively mark the children.
    for (BasicBlock::loopNumber l = optLoopTable[loopNum].lpChild; //
         l != BasicBlock::NOT_IN_LOOP;                             //
         l = optLoopTable[l].lpSibling)
    {
        if (!optLoopTable[l].lpIsRemoved())
        {
            return true;
        }

        if (optAnyChildNotRemoved(l))
        {
            return true;
        }
    }

    // All children were removed
    return false;
}

#endif // DEBUG

//------------------------------------------------------------------------
// optMarkLoopRemoved: Mark the specified loop as removed (some optimization, such as unrolling, has made the
// loop no longer exist). Note that only the given loop is marked as being removed; if it has any children,
// they are not touched (but a warning message is output to the JitDump).
// This method resets the `bbNatLoopNum` field to point to either parent's loop number or NOT_IN_LOOP.
// For consistency, it also updates the child loop's `lpParent` field to have its parent
//
// Arguments:
//      loopNum - the loop number to remove
//
void Compiler::optMarkLoopRemoved(unsigned loopNum)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Marking loop " FMT_LP " removed\n", loopNum);
        optPrintLoopTable();
    }
#endif

    assert(loopNum < optLoopCount);
    LoopDsc& loop = optLoopTable[loopNum];
    assert(!loop.lpIsRemoved());

    for (BasicBlock* const auxBlock : loop.LoopBlocks())
    {
        if (auxBlock->bbNatLoopNum == loopNum)
        {
            JITDUMP("Resetting loop number for " FMT_BB " from " FMT_LP " to " FMT_LP ".\n", auxBlock->bbNum,
                    auxBlock->bbNatLoopNum, loop.lpParent);
            auxBlock->bbNatLoopNum = loop.lpParent;
        }
    }

    if (loop.lpParent != BasicBlock::NOT_IN_LOOP)
    {
        // If there is a parent loop, we need to update two things for removed loop `loopNum`:
        // 1. Update its siblings so that they no longer point to the `loopNum`.
        // 2. Update the children loops to make them point to the parent loop instead.
        //
        // When we move all the child loops of current loop `loopNum` to its parent, we insert
        // those child loops at the same spot where `loopnum` was present in the child chain of
        // its parent loop. This is accomplished by updating the existing siblings of `loopNum`
        // to now point to the child loops.
        //
        // If L02 is removed:
        //   1. L01's sibling is updated from L02 to L03.
        //   2. L03 and L06's parents is updated from L02 to L00.
        //   3. L06's sibling is updated from NOT_IN_LOOP to L07.
        //
        //   L00                           L00
        //      L01                           L01
        //      L02              =>           L03
        //         L03                           L04
        //            L04                        L05
        //            L05                     L06
        //         L06                        L07
        //      L07
        //
        LoopDsc&               parentLoop     = optLoopTable[loop.lpParent];
        BasicBlock::loopNumber firstChildLoop = loop.lpChild;
        BasicBlock::loopNumber lastChildLoop  = BasicBlock::NOT_IN_LOOP;
        BasicBlock::loopNumber prevSibling    = BasicBlock::NOT_IN_LOOP;
        BasicBlock::loopNumber nextSibling    = BasicBlock::NOT_IN_LOOP;
        for (BasicBlock::loopNumber l = parentLoop.lpChild; l != BasicBlock::NOT_IN_LOOP; l = optLoopTable[l].lpSibling)
        {
            // We shouldn't see removed loop in loop table.
            assert(!optLoopTable[l].lpIsRemoved());

            nextSibling = optLoopTable[l].lpSibling;
            if (l == loopNum)
            {
                // This condition is not in for-loop just in case there is bad state of loopTable and we
                // end up spining infinitely.
                break;
            }
            prevSibling = l;
        }

        if (firstChildLoop == BasicBlock::NOT_IN_LOOP)
        {
            // There are no child loops in `loop`.
            // Just update `loop`'s siblings and parentLoop's lpChild, if applicable.

            if (parentLoop.lpChild == loopNum)
            {
                // If `loop` was the first child
                assert(prevSibling == BasicBlock::NOT_IN_LOOP);

                JITDUMP(FMT_LP " has no child loops but is the first child of its parent loop " FMT_LP
                               ". Update first child to " FMT_LP ".\n",
                        loopNum, loop.lpParent, nextSibling);

                parentLoop.lpChild = nextSibling;
            }
            else
            {
                // `loop` was non-first child
                assert(prevSibling != BasicBlock::NOT_IN_LOOP);

                JITDUMP(FMT_LP " has no child loops. Update sibling link " FMT_LP " -> " FMT_LP ".\n", loopNum,
                        prevSibling, nextSibling);

                optLoopTable[prevSibling].lpSibling = nextSibling;
            }
        }
        else
        {
            // There are child loops in `loop` that needs to be moved
            // under `loop`'s parents.

            if (parentLoop.lpChild == loopNum)
            {
                // If `loop` was the first child
                assert(prevSibling == BasicBlock::NOT_IN_LOOP);

                JITDUMP(FMT_LP " has child loops and is also the first child of its parent loop " FMT_LP
                               ". Update parent's first child to " FMT_LP ".\n",
                        loopNum, loop.lpParent, firstChildLoop);

                parentLoop.lpChild = firstChildLoop;
            }
            else
            {
                // `loop` was non-first child
                assert(prevSibling != BasicBlock::NOT_IN_LOOP);

                JITDUMP(FMT_LP " has child loops. Update sibling link " FMT_LP " -> " FMT_LP ".\n", loopNum,
                        prevSibling, firstChildLoop);

                optLoopTable[prevSibling].lpSibling = firstChildLoop;
            }

            // Update lpParent of all child loops
            for (BasicBlock::loopNumber l = firstChildLoop; l != BasicBlock::NOT_IN_LOOP; l = optLoopTable[l].lpSibling)
            {
                assert(!optLoopTable[l].lpIsRemoved());

                if (optLoopTable[l].lpSibling == BasicBlock::NOT_IN_LOOP)
                {
                    lastChildLoop = l;
                }

                JITDUMP("Resetting parent of loop number " FMT_LP " from " FMT_LP " to " FMT_LP ".\n", l,
                        optLoopTable[l].lpParent, loop.lpParent);
                optLoopTable[l].lpParent = loop.lpParent;
            }

            if (lastChildLoop != BasicBlock::NOT_IN_LOOP)
            {
                JITDUMP(FMT_LP " has child loops. Update sibling link " FMT_LP " -> " FMT_LP ".\n", loopNum,
                        lastChildLoop, nextSibling);

                optLoopTable[lastChildLoop].lpSibling = nextSibling;
            }
            else
            {
                assert(!"There is atleast one loop, but found none.");
            }

            // Finally, convey that there are no children of `loopNum`
            loop.lpChild = BasicBlock::NOT_IN_LOOP;
        }
    }
    else
    {
        // If there are no top-level loops, then all the child loops,
        // become the top-level loops.
        for (BasicBlock::loopNumber l = loop.lpChild; //
             l != BasicBlock::NOT_IN_LOOP;            //
             l = optLoopTable[l].lpSibling)
        {
            assert(!optLoopTable[l].lpIsRemoved());

            JITDUMP("Marking loop number " FMT_LP " from " FMT_LP " as top level loop.\n", l, optLoopTable[l].lpParent);
            optLoopTable[l].lpParent = BasicBlock::NOT_IN_LOOP;
        }
    }

    // Unmark any preheader
    //
    if ((loop.lpFlags & LPFLG_HAS_PREHEAD) != 0)
    {
        loop.lpHead->bbFlags &= ~BBF_LOOP_PREHEADER;
    }

    loop.lpFlags |= LPFLG_REMOVED;

#ifdef DEBUG
    if (optAnyChildNotRemoved(loopNum))
    {
        JITDUMP("Removed loop " FMT_LP " has one or more live children\n", loopNum);
    }

    if (verbose)
    {
        printf("Removed " FMT_LP "\n", loopNum);
        optPrintLoopTable();
    }

// Note: we can't call `fgDebugCheckLoopTable()` here because if there are live children, it will assert.
// Assume the caller is going to fix up the table and `bbNatLoopNum` block annotations before the next time
// `fgDebugCheckLoopTable()` is called.
#endif // DEBUG
}
