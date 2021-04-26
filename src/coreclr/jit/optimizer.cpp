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
#pragma warning(disable : 4701)
#endif

/*****************************************************************************/

void Compiler::optInit()
{
    optLoopsMarked = false;
    fgHasLoops     = false;

    /* Initialize the # of tracked loops to 0 */
    optLoopCount = 0;
    optLoopTable = nullptr;

#ifdef DEBUG
    loopAlignCandidates = 0;
    loopsAligned        = 0;
#endif

    /* Keep track of the number of calls and indirect calls made by this method */
    optCallCount         = 0;
    optIndirectCallCount = 0;
    optNativeCallCount   = 0;
    optAssertionCount    = 0;
    optAssertionDep      = nullptr;
    optCSECandidateTotal = 0;
    optCSEstart          = UINT_MAX;
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
// Notes:
//    Depends on dominators, and fgReturnBlocks being set.
//
void Compiler::optSetBlockWeights()
{
    noway_assert(opts.OptimizationEnabled());
    assert(fgDomsComputed);

#ifdef DEBUG
    bool changed = false;
#endif

    bool       firstBBDominatesAllReturns = true;
    const bool usingProfileWeights        = fgIsUsingProfileWeights();

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        /* Blocks that can't be reached via the first block are rarely executed */
        if (!fgReachable(fgFirstBB, block))
        {
            block->bbSetRunRarely();
        }

        if (!usingProfileWeights && firstBBDominatesAllReturns)
        {
            if (block->bbWeight != BB_ZERO_WEIGHT)
            {
                // Calculate our bbWeight:
                //
                //  o BB_UNITY_WEIGHT if we dominate all BBJ_RETURN blocks
                //  o otherwise BB_UNITY_WEIGHT / 2
                //
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
                }
                else
                {
                    // If we are not using profile weight then we lower the weight
                    // of blocks that do not dominate a return block
                    //
                    if (!blockDominatesAllReturns)
                    {
                        INDEBUG(changed = true);
                        block->inheritWeightPercentage(block, 50);
                    }
                }
            }
        }
    }

#if DEBUG
    if (changed && verbose)
    {
        printf("\nAfter optSetBlockWeights:\n");
        fgDispBasicBlocks();
        printf("\n");
    }

    /* Check that the flowgraph data (bbNum, bbRefs, bbPreds) is up-to-date */
    fgDebugCheckBBlist();
#endif
}

/*****************************************************************************
 *
 *  Marks the blocks between 'begBlk' and 'endBlk' as part of a loop.
 */

void Compiler::optMarkLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk, bool excludeEndBlk)
{
    /* Calculate the 'loopWeight',
       this is the amount to increase each block in the loop
       Our heuristic is that loops are weighted eight times more
       than straight line code.
       Thus we increase each block by 7 times the weight of
       the loop header block,
       if the loops are all properly formed gives us:
       (assuming that BB_LOOP_WEIGHT_SCALE is 8)

          1 -- non loop basic block
          8 -- single loop nesting
         64 -- double loop nesting
        512 -- triple loop nesting

    */

    noway_assert(begBlk->bbNum <= endBlk->bbNum);
    noway_assert(begBlk->isLoopHead());
    noway_assert(fgReachable(begBlk, endBlk));
    noway_assert(!opts.MinOpts());

#ifdef DEBUG
    if (verbose)
    {
        printf("\nMarking a loop from " FMT_BB " to " FMT_BB, begBlk->bbNum,
               excludeEndBlk ? endBlk->bbPrev->bbNum : endBlk->bbNum);
    }
#endif

    /* Build list of backedges for block begBlk */
    flowList* backedgeList = nullptr;

    for (flowList* pred = begBlk->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        /* Is this a backedge? */
        if (pred->getBlock()->bbNum >= begBlk->bbNum)
        {
            flowList* flow = new (this, CMK_FlowList) flowList(pred->getBlock(), backedgeList);

#if MEASURE_BLOCK_SIZE
            genFlowNodeCnt += 1;
            genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE

            backedgeList = flow;
        }
    }

    /* At least one backedge must have been found (the one from endBlk) */
    noway_assert(backedgeList);

    BasicBlock* curBlk = begBlk;

    while (true)
    {
        noway_assert(curBlk);

        // For curBlk to be part of a loop that starts at begBlk
        // curBlk must be reachable from begBlk and (since this is a loop)
        // likewise begBlk must be reachable from curBlk.
        //

        if (fgReachable(curBlk, begBlk) && fgReachable(begBlk, curBlk))
        {
            /* If this block reaches any of the backedge blocks we set reachable   */
            /* If this block dominates any of the backedge blocks we set dominates */
            bool reachable = false;
            bool dominates = false;

            for (flowList* tmp = backedgeList; tmp != nullptr; tmp = tmp->flNext)
            {
                BasicBlock* backedge = tmp->getBlock();

                if (!curBlk->isRunRarely())
                {
                    reachable |= fgReachable(curBlk, backedge);
                    dominates |= fgDominate(curBlk, backedge);

                    if (dominates && reachable)
                    {
                        break;
                    }
                }
            }

            if (reachable)
            {
                noway_assert(curBlk->bbWeight > BB_ZERO_WEIGHT);

                if (!curBlk->hasProfileWeight())
                {
                    BasicBlock::weight_t scale = BB_LOOP_WEIGHT_SCALE;

                    if (!dominates)
                    {
                        scale = scale / 2;
                    }

                    curBlk->scaleBBWeight(scale);
                }

                JITDUMP("\n    " FMT_BB "(wt=" FMT_WT ")", curBlk->bbNum, curBlk->getBBWeight(this));
            }
        }

        /* Stop if we've reached the last block in the loop */

        if (curBlk == endBlk)
        {
            break;
        }

        curBlk = curBlk->bbNext;

        /* If we are excluding the endBlk then stop if we've reached endBlk */

        if (excludeEndBlk && (curBlk == endBlk))
        {
            break;
        }
    }
}

/*****************************************************************************
 *
 *   Unmark the blocks between 'begBlk' and 'endBlk' as part of a loop.
 */

void Compiler::optUnmarkLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk)
{
    /* A set of blocks that were previously marked as a loop are now
       to be unmarked, since we have decided that for some reason this
       loop no longer exists.
       Basically we are just reseting the blocks bbWeight to their
       previous values.
    */

    noway_assert(begBlk->bbNum <= endBlk->bbNum);
    noway_assert(begBlk->isLoopHead());

    noway_assert(!opts.MinOpts());

    BasicBlock* curBlk;
    unsigned    backEdgeCount = 0;

    for (flowList* pred = begBlk->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        curBlk = pred->getBlock();

        /* is this a backward edge? (from curBlk to begBlk) */

        if (begBlk->bbNum > curBlk->bbNum)
        {
            continue;
        }

        /* We only consider back-edges that are BBJ_COND or BBJ_ALWAYS for loops */

        if ((curBlk->bbJumpKind != BBJ_COND) && (curBlk->bbJumpKind != BBJ_ALWAYS))
        {
            continue;
        }

        backEdgeCount++;
    }

    /* Only unmark the loop blocks if we have exactly one loop back edge */
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
    noway_assert(backEdgeCount == 1);
    noway_assert(fgReachable(begBlk, endBlk));

#ifdef DEBUG
    if (verbose)
    {
        printf("\nUnmarking loop at " FMT_BB, begBlk->bbNum);
    }
#endif

    curBlk = begBlk;
    while (true)
    {
        noway_assert(curBlk);

        // For curBlk to be part of a loop that starts at begBlk
        // curBlk must be reachable from begBlk and (since this is a loop)
        // likewise begBlk must be reachable from curBlk.
        //
        if (!curBlk->isRunRarely() && fgReachable(curBlk, begBlk) && fgReachable(begBlk, curBlk))
        {
            // Don't unmark blocks that are set to BB_MAX_WEIGHT
            // Don't unmark blocks when we are using profile weights
            //
            if (!curBlk->isMaxBBWeight() && !curBlk->hasProfileWeight())
            {
                BasicBlock::weight_t scale = 1.0f / BB_LOOP_WEIGHT_SCALE;

                if (!fgDominate(curBlk, endBlk))
                {
                    scale *= 2;
                }

                curBlk->scaleBBWeight(scale);
            }

            JITDUMP("\n    " FMT_BB "(wt=" FMT_WT ")", curBlk->bbNum, curBlk->getBBWeight(this));
        }

        /* Stop if we've reached the last block in the loop */

        if (curBlk == endBlk)
        {
            break;
        }

        curBlk = curBlk->bbNext;

        /* Stop if we go past the last block in the loop, as it may have been deleted */
        if (curBlk->bbNum > endBlk->bbNum)
        {
            break;
        }
    }

    JITDUMP("\n");

#if FEATURE_LOOP_ALIGN
    if (begBlk->isLoopAlign())
    {
        // Clear the loop alignment bit on the head of a loop, since it's no longer a loop.
        begBlk->bbFlags &= ~BBF_LOOP_ALIGN;
        JITDUMP("Removing LOOP_ALIGN flag from removed loop in " FMT_BB "\n", begBlk->bbNum);
    }
#endif
}

/*****************************************************************************************************
 *
 *  Function called to update the loop table and bbWeight before removing a block
 */

void Compiler::optUpdateLoopsBeforeRemoveBlock(BasicBlock* block, bool skipUnmarkLoop)
{
    if (!optLoopsMarked)
    {
        return;
    }

    noway_assert(!opts.MinOpts());

    bool removeLoop = false;

    /* If an unreachable block was part of a loop entry or bottom then the loop is unreachable */
    /* Special case: the block was the head of a loop - or pointing to a loop entry */

    for (unsigned loopNum = 0; loopNum < optLoopCount; loopNum++)
    {
        LoopDsc& loop = optLoopTable[loopNum];

        /* Some loops may have been already removed by
         * loop unrolling or conditional folding */

        if (loop.lpFlags & LPFLG_REMOVED)
        {
            continue;
        }

        if (block == loop.lpEntry || block == loop.lpBottom)
        {
            loop.lpFlags |= LPFLG_REMOVED;
            continue;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\nUpdateLoopsBeforeRemoveBlock Before: ");
            optPrintLoopInfo(loopNum);
        }
#endif

        /* If the loop is still in the table
         * any block in the loop must be reachable !!! */

        noway_assert(loop.lpEntry != block);
        noway_assert(loop.lpBottom != block);

        if (loop.lpExit == block)
        {
            loop.lpExit = nullptr;
            loop.lpFlags &= ~LPFLG_ONE_EXIT;
        }

        /* If this points to the actual entry in the loop
         * then the whole loop may become unreachable */

        switch (block->bbJumpKind)
        {
            unsigned     jumpCnt;
            BasicBlock** jumpTab;

            case BBJ_NONE:
            case BBJ_COND:
                if (block->bbNext == loop.lpEntry)
                {
                    removeLoop = true;
                    break;
                }
                if (block->bbJumpKind == BBJ_NONE)
                {
                    break;
                }

                FALLTHROUGH;

            case BBJ_ALWAYS:
                noway_assert(block->bbJumpDest);
                if (block->bbJumpDest == loop.lpEntry)
                {
                    removeLoop = true;
                }
                break;

            case BBJ_SWITCH:
                jumpCnt = block->bbJumpSwt->bbsCount;
                jumpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    noway_assert(*jumpTab);
                    if ((*jumpTab) == loop.lpEntry)
                    {
                        removeLoop = true;
                    }
                } while (++jumpTab, --jumpCnt);
                break;

            default:
                break;
        }

        if (removeLoop)
        {
            /* Check if the entry has other predecessors outside the loop
             * TODO: Replace this when predecessors are available */

            BasicBlock* auxBlock;
            for (auxBlock = fgFirstBB; auxBlock; auxBlock = auxBlock->bbNext)
            {
                /* Ignore blocks in the loop */

                if (auxBlock->bbNum > loop.lpHead->bbNum && auxBlock->bbNum <= loop.lpBottom->bbNum)
                {
                    continue;
                }

                switch (auxBlock->bbJumpKind)
                {
                    unsigned     jumpCnt;
                    BasicBlock** jumpTab;

                    case BBJ_NONE:
                    case BBJ_COND:
                        if (auxBlock->bbNext == loop.lpEntry)
                        {
                            removeLoop = false;
                            break;
                        }
                        if (auxBlock->bbJumpKind == BBJ_NONE)
                        {
                            break;
                        }

                        FALLTHROUGH;

                    case BBJ_ALWAYS:
                        noway_assert(auxBlock->bbJumpDest);
                        if (auxBlock->bbJumpDest == loop.lpEntry)
                        {
                            removeLoop = false;
                        }
                        break;

                    case BBJ_SWITCH:
                        jumpCnt = auxBlock->bbJumpSwt->bbsCount;
                        jumpTab = auxBlock->bbJumpSwt->bbsDstTab;

                        do
                        {
                            noway_assert(*jumpTab);
                            if ((*jumpTab) == loop.lpEntry)
                            {
                                removeLoop = false;
                            }
                        } while (++jumpTab, --jumpCnt);
                        break;

                    default:
                        break;
                }
            }

            if (removeLoop)
            {
                loop.lpFlags |= LPFLG_REMOVED;
            }
        }
        else if (loop.lpHead == block)
        {
            /* The loop has a new head - Just update the loop table */
            loop.lpHead = block->bbPrev;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\nUpdateLoopsBeforeRemoveBlock After: ");
            optPrintLoopInfo(loopNum);
        }
#endif
    }

    if ((skipUnmarkLoop == false) && ((block->bbJumpKind == BBJ_ALWAYS) || (block->bbJumpKind == BBJ_COND)) &&
        (block->bbJumpDest->isLoopHead()) && (block->bbJumpDest->bbNum <= block->bbNum) && fgDomsComputed &&
        (fgCurBBEpochSize == fgDomBBcount + 1) && fgReachable(block->bbJumpDest, block))
    {
        optUnmarkLoopBlocks(block->bbJumpDest, block);
    }
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Given the beginBlock of the loop, return the index of this loop
 *  to the loop table.
 */

unsigned Compiler::optFindLoopNumberFromBeginBlock(BasicBlock* begBlk)
{
    unsigned lnum = 0;

    for (lnum = 0; lnum < optLoopCount; lnum++)
    {
        if (optLoopTable[lnum].lpHead->bbNext == begBlk)
        {
            // Found the loop.
            return lnum;
        }
    }

    noway_assert(!"Loop number not found.");

    return optLoopCount;
}

/*****************************************************************************
 *
 *  Print loop info in an uniform way.
 */

void Compiler::optPrintLoopInfo(unsigned      loopInd,
                                BasicBlock*   lpHead,
                                BasicBlock*   lpFirst,
                                BasicBlock*   lpTop,
                                BasicBlock*   lpEntry,
                                BasicBlock*   lpBottom,
                                unsigned char lpExitCnt,
                                BasicBlock*   lpExit,
                                unsigned      parentLoop) const
{
    noway_assert(lpHead);

    printf(FMT_LP ", from " FMT_BB, loopInd, lpFirst->bbNum);
    if (lpTop != lpFirst)
    {
        printf(" (loop top is " FMT_BB ")", lpTop->bbNum);
    }

    printf(" to " FMT_BB " (Head=" FMT_BB ", Entry=" FMT_BB ", ExitCnt=%d", lpBottom->bbNum, lpHead->bbNum,
           lpEntry->bbNum, lpExitCnt);

    if (lpExitCnt == 1)
    {
        printf(" at " FMT_BB, lpExit->bbNum);
    }

    if (parentLoop != BasicBlock::NOT_IN_LOOP)
    {
        printf(", parent loop = " FMT_LP, parentLoop);
    }
    printf(")");
}

/*****************************************************************************
 *
 *  Print loop information given the index of the loop in the loop table.
 */

void Compiler::optPrintLoopInfo(unsigned lnum) const
{
    noway_assert(lnum < optLoopCount);

    const LoopDsc* ldsc = &optLoopTable[lnum]; // lnum is the INDEX to the loop table.

    optPrintLoopInfo(lnum, ldsc->lpHead, ldsc->lpFirst, ldsc->lpTop, ldsc->lpEntry, ldsc->lpBottom, ldsc->lpExitCnt,
                     ldsc->lpExit, ldsc->lpParent);
}

#endif

//------------------------------------------------------------------------
// optPopulateInitInfo: Populate loop init info in the loop table.
//
// Arguments:
//     init     -  the tree that is supposed to initialize the loop iterator.
//     iterVar  -  loop iteration variable.
//
// Return Value:
//     "false" if the loop table could not be populated with the loop iterVar init info.
//
// Operation:
//     The 'init' tree is checked if its lhs is a local and rhs is either
//     a const or a local.
//
bool Compiler::optPopulateInitInfo(unsigned loopInd, GenTree* init, unsigned iterVar)
{
    // Operator should be =
    if (init->gtOper != GT_ASG)
    {
        return false;
    }

    GenTree* lhs = init->AsOp()->gtOp1;
    GenTree* rhs = init->AsOp()->gtOp2;
    // LHS has to be local and should equal iterVar.
    if (lhs->gtOper != GT_LCL_VAR || lhs->AsLclVarCommon()->GetLclNum() != iterVar)
    {
        return false;
    }

    // RHS can be constant or local var.
    // TODO-CQ: CLONE: Add arr length for descending loops.
    if (rhs->gtOper == GT_CNS_INT && rhs->TypeGet() == TYP_INT)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_CONST_INIT;
        optLoopTable[loopInd].lpConstInit = (int)rhs->AsIntCon()->gtIconVal;
    }
    else if (rhs->gtOper == GT_LCL_VAR)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_VAR_INIT;
        optLoopTable[loopInd].lpVarInit = rhs->AsLclVarCommon()->GetLclNum();
    }
    else
    {
        return false;
    }
    return true;
}

//----------------------------------------------------------------------------------
// optCheckIterInLoopTest: Check if iter var is used in loop test.
//
// Arguments:
//      test          "jtrue" tree or an asg of the loop iter termination condition
//      from/to       blocks (beg, end) which are part of the loop.
//      iterVar       loop iteration variable.
//      loopInd       loop index.
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
bool Compiler::optCheckIterInLoopTest(
    unsigned loopInd, GenTree* test, BasicBlock* from, BasicBlock* to, unsigned iterVar)
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

    noway_assert(relop->OperKind() & GTK_RELOP);

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
    else if (limitOp->gtOper == GT_LCL_VAR &&
             !optIsVarAssigned(from, to, nullptr, limitOp->AsLclVarCommon()->GetLclNum()))
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_VAR_LIMIT;
    }
    else if (limitOp->gtOper == GT_ARR_LENGTH)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_ARRLEN_LIMIT;
    }
    else
    {
        return false;
    }
    // Save the type of the comparison between the iterator and the limit.
    optLoopTable[loopInd].lpTestTree = relop;
    return true;
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
//      from, to    - are blocks (beg, end) which are part of the loop.
//      incr        - tree that increments the loop iterator. v+=1 or v=v+1.
//      pIterVar    - see return value.
//
//  Return Value:
//      Returns true if iterVar "v" can be returned in "pIterVar", otherwise returns
//      false.
//
//  Operation:
//      Check if the "incr" tree is a "v=v+1 or v+=1" type tree and make sure it is not
//      assigned in the loop.
//
bool Compiler::optComputeIterInfo(GenTree* incr, BasicBlock* from, BasicBlock* to, unsigned* pIterVar)
{

    unsigned iterVar = optIsLoopIncrTree(incr);
    if (iterVar == BAD_VAR_NUM)
    {
        return false;
    }
    if (optIsVarAssigned(from, to, incr, iterVar))
    {
        JITDUMP("iterVar is assigned in loop\n");
        return false;
    }

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
//      head    - Loop head block
//      bottom  - Loop bottom block
//      top     - Loop top block
//      ppInit  - The init stmt of the loop if found.
//      ppTest  - The test stmt of the loop if found.
//      ppIncr  - The incr stmt of the loop if found.
//
//  Return Value:
//      The results are put in "ppInit", "ppTest" and "ppIncr" if the method
//      returns true. Returns false if the information can't be extracted.
//
//  Operation:
//      Check if the "test" stmt is last stmt in the loop "bottom". If found good,
//      "test" stmt is found. Try to find the "incr" stmt. Check previous stmt of
//      "test" to get the "incr" stmt. If it is not found it could be a loop of the
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
    BasicBlock* head, BasicBlock* bottom, BasicBlock* top, GenTree** ppInit, GenTree** ppTest, GenTree** ppIncr)
{
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
    Statement* phdrStmt = head->firstStmt();
    if (phdrStmt == nullptr)
    {
        return false;
    }

    Statement* initStmt = phdrStmt->GetPrevStmt();
    noway_assert(initStmt != nullptr && (initStmt->GetNextStmt() == nullptr));

    // If it is a duplicated loop condition, skip it.
    if (initStmt->IsCompilerAdded())
    {
        bool doGetPrev = true;
#ifdef DEBUG
        if (opts.optRepeat)
        {
            // Previous optimization passes may have inserted compiler-generated
            // statements other than duplicated loop conditions.
            doGetPrev = (initStmt->GetPrevStmt() != nullptr);
        }
        else
        {
            // Must be a duplicated loop condition.
            noway_assert(initStmt->GetRootNode()->gtOper == GT_JTRUE);
        }
#endif // DEBUG
        if (doGetPrev)
        {
            initStmt = initStmt->GetPrevStmt();
        }
        noway_assert(initStmt != nullptr);
    }

    *ppInit = initStmt->GetRootNode();
    *ppTest = testStmt->GetRootNode();
    *ppIncr = incrStmt->GetRootNode();

    return true;
}

/*****************************************************************************
 *
 *  Record the loop in the loop table.  Return true if successful, false if
 *  out of entries in loop table.
 */

bool Compiler::optRecordLoop(BasicBlock*   head,
                             BasicBlock*   first,
                             BasicBlock*   top,
                             BasicBlock*   entry,
                             BasicBlock*   bottom,
                             BasicBlock*   exit,
                             unsigned char exitCnt)
{
    // Record this loop in the table, if there's room.

    assert(optLoopCount <= MAX_LOOP_NUM);
    if (optLoopCount == MAX_LOOP_NUM)
    {
#if COUNT_LOOPS
        loopOverflowThisMethod = true;
#endif
        return false;
    }

    // Assumed preconditions on the loop we're adding.
    assert(first->bbNum <= top->bbNum);
    assert(top->bbNum <= entry->bbNum);
    assert(entry->bbNum <= bottom->bbNum);
    assert(head->bbNum < top->bbNum || head->bbNum > bottom->bbNum);

    unsigned char loopInd = optLoopCount;

    if (optLoopTable == nullptr)
    {
        assert(loopInd == 0);
        optLoopTable = getAllocator(CMK_LoopOpt).allocate<LoopDsc>(MAX_LOOP_NUM);
    }
    else
    {
        // If the new loop contains any existing ones, add it in the right place.
        for (unsigned char prevPlus1 = optLoopCount; prevPlus1 > 0; prevPlus1--)
        {
            unsigned char prev = prevPlus1 - 1;
            if (optLoopTable[prev].lpContainedBy(first, bottom))
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
        if (optLoopTable[i].lpDisjoint(first, bottom))
        {
            continue;
        }
        // Otherwise, assert complete containment (of optLoopTable[i] in new loop).
        assert(optLoopTable[i].lpContainedBy(first, bottom));
    }
#endif // DEBUG

    optLoopTable[loopInd].lpHead    = head;
    optLoopTable[loopInd].lpFirst   = first;
    optLoopTable[loopInd].lpTop     = top;
    optLoopTable[loopInd].lpBottom  = bottom;
    optLoopTable[loopInd].lpEntry   = entry;
    optLoopTable[loopInd].lpExit    = exit;
    optLoopTable[loopInd].lpExitCnt = exitCnt;

    optLoopTable[loopInd].lpParent  = BasicBlock::NOT_IN_LOOP;
    optLoopTable[loopInd].lpChild   = BasicBlock::NOT_IN_LOOP;
    optLoopTable[loopInd].lpSibling = BasicBlock::NOT_IN_LOOP;

    optLoopTable[loopInd].lpAsgVars = AllVarSetOps::UninitVal();

    optLoopTable[loopInd].lpFlags = 0;

    // We haven't yet recorded any side effects.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        optLoopTable[loopInd].lpLoopHasMemoryHavoc[memoryKind] = false;
    }
    optLoopTable[loopInd].lpFieldsModified         = nullptr;
    optLoopTable[loopInd].lpArrayElemTypesModified = nullptr;

    // If DO-WHILE loop mark it as such.
    if (head->bbNext == entry)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_DO_WHILE;
    }

    // If single exit loop mark it as such.
    if (exitCnt == 1)
    {
        noway_assert(exit);
        optLoopTable[loopInd].lpFlags |= LPFLG_ONE_EXIT;
    }

    //
    // Try to find loops that have an iterator (i.e. for-like loops) "for (init; test; incr){ ... }"
    // We have the following restrictions:
    //     1. The loop condition must be a simple one i.e. only one JTRUE node
    //     2. There must be a loop iterator (a local var) that is
    //        incremented (decremented or lsh, rsh, mul) with a constant value
    //     3. The iterator is incremented exactly once
    //     4. The loop condition must use the iterator.
    //
    if (bottom->bbJumpKind == BBJ_COND)
    {
        GenTree* init;
        GenTree* test;
        GenTree* incr;
        if (!optExtractInitTestIncr(head, bottom, top, &init, &test, &incr))
        {
            goto DONE_LOOP;
        }

        unsigned iterVar = BAD_VAR_NUM;
        if (!optComputeIterInfo(incr, head->bbNext, bottom, &iterVar))
        {
            goto DONE_LOOP;
        }

        // Make sure the "iterVar" initialization is never skipped,
        // i.e. every pred of ENTRY other than HEAD is in the loop.
        for (flowList* predEdge = entry->bbPreds; predEdge; predEdge = predEdge->flNext)
        {
            BasicBlock* predBlock = predEdge->getBlock();
            if ((predBlock != head) && !optLoopTable[loopInd].lpContains(predBlock))
            {
                goto DONE_LOOP;
            }
        }

        if (!optPopulateInitInfo(loopInd, init, iterVar))
        {
            goto DONE_LOOP;
        }

        // Check that the iterator is used in the loop condition.
        if (!optCheckIterInLoopTest(loopInd, test, head->bbNext, bottom, iterVar))
        {
            goto DONE_LOOP;
        }

        // We know the loop has an iterator at this point ->flag it as LPFLG_ITER
        // Record the iterator, the pointer to the test node
        // and the initial value of the iterator (constant or local var)
        optLoopTable[loopInd].lpFlags |= LPFLG_ITER;

        // Record iterator.
        optLoopTable[loopInd].lpIterTree = incr;

#if COUNT_LOOPS
        // Save the initial value of the iterator - can be lclVar or constant
        // Flag the loop accordingly.

        iterLoopCount++;
#endif

#if COUNT_LOOPS
        simpleTestLoopCount++;
#endif

        // Check if a constant iteration loop.
        if ((optLoopTable[loopInd].lpFlags & LPFLG_CONST_INIT) && (optLoopTable[loopInd].lpFlags & LPFLG_CONST_LIMIT))
        {
            // This is a constant loop.
            optLoopTable[loopInd].lpFlags |= LPFLG_CONST;
#if COUNT_LOOPS
            constIterLoopCount++;
#endif
        }

#ifdef DEBUG
        if (verbose && 0)
        {
            printf("\nConstant loop initializer:\n");
            gtDispTree(init);

            printf("\nConstant loop body:\n");

            BasicBlock* block = head;
            do
            {
                block = block->bbNext;
                for (Statement* stmt : block->Statements())
                {
                    if (stmt->GetRootNode() == incr)
                    {
                        break;
                    }
                    printf("\n");
                    gtDispTree(stmt->GetRootNode());
                }
            } while (block != bottom);
        }
#endif // DEBUG
    }

DONE_LOOP:
    DBEXEC(verbose, optPrintLoopRecording(loopInd));
    optLoopCount++;
    return true;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// optPrintLoopRecording: Print a recording of the loop.
//
// Arguments:
//      loopInd     - loop index.
//
void Compiler::optPrintLoopRecording(unsigned loopInd) const
{
    const LoopDsc& loop = optLoopTable[loopInd];

    printf("Recorded loop %s", (loopInd != optLoopCount ? "(extended) " : ""));
    optPrintLoopInfo(optLoopCount, // Not necessarily the loop index, but the number of loops that have been added.
                     loop.lpHead, loop.lpFirst, loop.lpTop, loop.lpEntry, loop.lpBottom, loop.lpExitCnt, loop.lpExit);

    // If an iterator loop print the iterator and the initialization.
    if (loop.lpFlags & LPFLG_ITER)
    {
        printf(" [over V%02u", loop.lpIterVar());
        printf(" (");
        printf(GenTree::OpName(loop.lpIterOper()));
        printf(" ");
        printf("%d )", loop.lpIterConst());

        if (loop.lpFlags & LPFLG_CONST_INIT)
        {
            printf(" from %d", loop.lpConstInit);
        }
        if (loop.lpFlags & LPFLG_VAR_INIT)
        {
            printf(" from V%02u", loop.lpVarInit);
        }

        // If a simple test condition print operator and the limits */
        printf(GenTree::OpName(loop.lpTestOper()));

        if (loop.lpFlags & LPFLG_CONST_LIMIT)
        {
            printf("%d ", loop.lpConstLimit());
        }

        if (loop.lpFlags & LPFLG_VAR_LIMIT)
        {
            printf("V%02u ", loop.lpVarLimit());
        }

        printf("]");
    }

    printf("\n");
}

void Compiler::optCheckPreds()
{
    BasicBlock* block;
    BasicBlock* blockPred;
    flowList*   pred;

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        for (pred = block->bbPreds; pred; pred = pred->flNext)
        {
            // make sure this pred is part of the BB list
            for (blockPred = fgFirstBB; blockPred; blockPred = blockPred->bbNext)
            {
                if (blockPred == pred->getBlock())
                {
                    break;
                }
            }
            noway_assert(blockPred);
            switch (blockPred->bbJumpKind)
            {
                case BBJ_COND:
                    if (blockPred->bbJumpDest == block)
                    {
                        break;
                    }
                    FALLTHROUGH;
                case BBJ_NONE:
                    noway_assert(blockPred->bbNext == block);
                    break;
                case BBJ_EHFILTERRET:
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                    noway_assert(blockPred->bbJumpDest == block);
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
//             moving blocks to make the loop body contiguous, and recording
//             the loop.
//
// We will use the following terminology:
//   HEAD    - the basic block that flows into the loop ENTRY block (Currently MUST be lexically before entry).
//             Not part of the looping of the loop.
//   FIRST   - the lexically first basic block (in bbNext order) within this loop.
//   TOP     - the target of the backward edge from BOTTOM. In most cases FIRST and TOP are the same.
//   BOTTOM  - the lexically last block in the loop (i.e. the block from which we jump to the top)
//   EXIT    - the predecessor of loop's unique exit edge, if it has a unique exit edge; else nullptr
//   ENTRY   - the entry in the loop (not necessarly the TOP), but there must be only one entry
//
//   We (currently) require the body of a loop to be a contiguous (in bbNext order) sequence of basic blocks.
//   When the loop is identified, blocks will be moved out to make it a compact contiguous region if possible,
//   and in cases where compaction is not possible, we'll subsequently treat all blocks in the lexical range
//   between TOP and BOTTOM as part of the loop even if they aren't part of the SCC.
//   Regarding nesting:  Since a given block can only have one back-edge (we only detect loops with back-edges
//   from BBJ_COND or BBJ_ALWAYS blocks), no two loops will share the same BOTTOM.  Two loops may share the
//   same FIRST/TOP/ENTRY as reported by LoopSearch, and optCanonicalizeLoopNest will subsequently re-write
//   the CFG so that no two loops share the same FIRST/TOP/ENTRY anymore.
//
//        |
//        v
//      head
//        |
//        |  top/first <--+
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
            return BlockSetOps::IsMember(comp, oldBlocksInLoop, blockNum);
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
    BasicBlock* first;  // Lexically first in-loop block
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
        /* At this point we have a compact loop - record it in the loop table
        * If we found only one exit, record it in the table too
        * (otherwise an exit = nullptr in the loop table means multiple exits) */

        BasicBlock* onlyExit = (exitCount == 1 ? lastExit : nullptr);
        if (comp->optRecordLoop(head, first, top, entry, bottom, onlyExit, exitCount))
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
        /* Is this a loop candidate? - We look for "back edges", i.e. an edge from BOTTOM
        * to TOP (note that this is an abuse of notation since this is not necessarily a back edge
        * as the definition says, but merely an indication that we have a loop there).
        * Thus, we have to be very careful and after entry discovery check that it is indeed
        * the only place we enter the loop (especially for non-reducible flow graphs).
        */

        if (top->bbNum > bottom->bbNum) // is this a backward edge? (from BOTTOM to TOP)
        {
            // Edge from BOTTOM to TOP is not a backward edge
            return false;
        }

        if (bottom->bbNum > oldBlockMaxNum)
        {
            // Not a true back-edge; bottom is a block added to reconnect fall-through during
            // loop processing, so its block number does not reflect its position.
            return false;
        }

        if ((bottom->bbJumpKind == BBJ_EHFINALLYRET) || (bottom->bbJumpKind == BBJ_EHFILTERRET) ||
            (bottom->bbJumpKind == BBJ_EHCATCHRET) || (bottom->bbJumpKind == BBJ_CALLFINALLY) ||
            (bottom->bbJumpKind == BBJ_SWITCH))
        {
            /* BBJ_EHFINALLYRET, BBJ_EHFILTERRET, BBJ_EHCATCHRET, and BBJ_CALLFINALLY can never form a loop.
            * BBJ_SWITCH that has a backward jump appears only for labeled break. */
            return false;
        }

        /* The presence of a "back edge" is an indication that a loop might be present here
        *
        * LOOP:
        *        1. A collection of STRONGLY CONNECTED nodes i.e. there is a path from any
        *           node in the loop to any other node in the loop (wholly within the loop)
        *        2. The loop has a unique ENTRY, i.e. there is only one way to reach a node
        *           in the loop from outside the loop, and that is through the ENTRY
        */

        /* Let's find the loop ENTRY */
        BasicBlock* entry = FindEntry(head, top, bottom);

        if (entry == nullptr)
        {
            // For now, we only recognize loops where HEAD has some successor ENTRY in the loop.
            return false;
        }

        // Passed the basic checks; initialize instance state for this back-edge.
        this->head      = head;
        this->top       = top;
        this->entry     = entry;
        this->bottom    = bottom;
        this->lastExit  = nullptr;
        this->exitCount = 0;

        // Now we find the "first" block -- the earliest block reachable within the loop.
        // With our current algorithm, this is always the same as "top".
        this->first = top;

        if (!HasSingleEntryCycle())
        {
            // There isn't actually a loop between TOP and BOTTOM
            return false;
        }

        if (!loopBlocks.IsMember(top->bbNum))
        {
            // The "back-edge" we identified isn't actually part of the flow cycle containing ENTRY
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

        if (bottom->hasTryIndex() && !comp->bbInTryRegions(bottom->getTryIndex(), first))
        {
            JITDUMP("Loop 'first' " FMT_BB " is in an outer EH region compared to loop 'bottom' " FMT_BB ". Rejecting "
                    "loop.\n",
                    first->bbNum, bottom->bbNum);
            return false;
        }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        // Disqualify loops where the first block of the loop is a finally target.
        // The main problem is when multiple loops share a 'first' block that is a finally
        // target and we canonicalize the loops by adding a new loop head. In that case, we
        // need to update the blocks so the finally target bit is moved to the newly created
        // block, and removed from the old 'first' block. This is 'hard', so at this point
        // in the RyuJIT codebase (when we don't expect to keep the "old" ARM32 code generator
        // long-term), it's easier to disallow the loop than to update the flow graph to
        // support this case.

        if ((first->bbFlags & BBF_FINALLY_TARGET) != 0)
        {
            JITDUMP("Loop 'first' " FMT_BB " is a finally target. Rejecting loop.\n", first->bbNum);
            return false;
        }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

        // Compact the loop (sweep through it and move out any blocks that aren't part of the
        // flow cycle), and find the exits.
        if (!MakeCompactAndFindExits())
        {
            // Unable to preserve well-formed loop during compaction.
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
                /* OK - we enter somewhere within the loop */

                /* some useful asserts
                * Cannot enter at the top - should have being caught by redundant jumps */

                assert((head->bbJumpDest != top) || (head->bbFlags & BBF_KEEP_BBJ_ALWAYS));

                return head->bbJumpDest;
            }
            else
            {
                /* special case - don't consider now */
                // assert (!"Loop entered in weird way!");
                return nullptr;
            }
        }
        // Can we fall through into the loop?
        else if (head->bbJumpKind == BBJ_NONE || head->bbJumpKind == BBJ_COND)
        {
            /* The ENTRY is at the TOP (a do-while loop) */
            return top;
        }
        else
        {
            return nullptr; // head does not flow into the loop bail for now
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
    //    Will mark (in `loopBlocks`) all blocks found to participate in the
    //    cycle.
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

            /* Make sure ENTRY dominates all blocks in the loop
            * This is necessary to ensure condition 2. above
            */
            if (block->bbNum > oldBlockMaxNum)
            {
                // This is a new block we added to connect fall-through, so the
                // recorded dominator information doesn't cover it.  Just continue,
                // and when we process its unique predecessor we'll abort if ENTRY
                // doesn't dominate that.
            }
            else if (!comp->fgDominate(entry, block))
            {
                return false;
            }

            // Add preds to the worklist, checking for side-entries.
            for (flowList* predIter = block->bbPreds; predIter != nullptr; predIter = predIter->flNext)
            {
                BasicBlock* pred = predIter->getBlock();

                unsigned int testNum = PositionNum(pred);

                if ((testNum < top->bbNum) || (testNum > bottom->bbNum))
                {
                    // Pred is out of loop range
                    if (block == entry)
                    {
                        if (pred == head)
                        {
                            // This is the single entry we expect.
                            continue;
                        }
                        // ENTRY has some pred other than head outside the loop.  If ENTRY does not
                        // dominate this pred, we'll consider this a side-entry and skip this loop;
                        // otherwise the loop is still valid and this may be a (flow-wise) back-edge
                        // of an outer loop.  For the dominance test, if `pred` is a new block, use
                        // its unique predecessor since the dominator tree has info for that.
                        BasicBlock* effectivePred = (pred->bbNum > oldBlockMaxNum ? pred->bbPrev : pred);
                        if (comp->fgDominate(entry, effectivePred))
                        {
                            // Outer loop back-edge
                            continue;
                        }
                    }

                    // There are multiple entries to this loop, don't consider it.
                    return false;
                }

                bool isFirstVisit;
                if (pred == entry)
                {
                    // We have indeed found a cycle in the flow graph.
                    isFirstVisit = !foundCycle;
                    foundCycle   = true;
                    assert(loopBlocks.IsMember(pred->bbNum));
                }
                else if (loopBlocks.TestAndInsert(pred->bbNum))
                {
                    // Already visited this pred
                    isFirstVisit = false;
                }
                else
                {
                    // Add this pred to the worklist
                    worklist.push_back(pred);
                    isFirstVisit = true;
                }

                if (isFirstVisit && (pred->bbNext != nullptr) && (PositionNum(pred->bbNext) == pred->bbNum))
                {
                    // We've created a new block immediately after `pred` to
                    // reconnect what was fall-through.  Mark it as in-loop also;
                    // it needs to stay with `prev` and if it exits the loop we'd
                    // just need to re-create it if we tried to move it out.
                    loopBlocks.Insert(pred->bbNext->bbNum);
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
            assert(block->bbPreds->getBlock() == block->bbPrev);
            assert(block->bbPreds->flNext == nullptr);
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

            // This blocks is lexically between TOP and BOTTOM, but it does not
            // participate in the flow cycle.  Check for a run of consecutive
            // such blocks.
            BasicBlock* lastNonLoopBlock = block;
            BasicBlock* nextLoopBlock    = block->bbNext;
            while (!loopBlocks.IsMember(nextLoopBlock->bbNum))
            {
                lastNonLoopBlock = nextLoopBlock;
                nextLoopBlock    = nextLoopBlock->bbNext;
                // This loop must terminate because we know BOTTOM is in loopBlocks.
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

        if ((newMoveAfter->bbJumpKind == BBJ_ALWAYS) || (newMoveAfter->bbJumpKind == BBJ_COND))
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
        for (flowList* predIter = newMoveAfter->bbPreds; predIter != nullptr; predIter = predIter->flNext)
        {
            unsigned int predNum = predIter->getBlock()->bbNum;

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
        BasicBlock* nextLoopBlock = lastNonLoopBlock->bbNext;
        for (BasicBlock* testBlock = firstNonLoopBlock; testBlock != nextLoopBlock; testBlock = testBlock->bbNext)
        {
            for (flowList* predIter = testBlock->bbPreds; predIter != nullptr; predIter = predIter->flNext)
            {
                BasicBlock*  testPred           = predIter->getBlock();
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
                /* Reverse the jump condition */
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
        }

        // Make sure we don't leave around a goto-next unless it's marked KEEP_BBJ_ALWAYS.
        assert(((block->bbJumpKind != BBJ_COND) && (block->bbJumpKind != BBJ_ALWAYS)) ||
               (block->bbJumpDest != newNext) || ((block->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0));
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
                    /* exit from a block other than BOTTOM */
                    lastExit = block;
                    exitCount++;
                }
                break;

            case BBJ_NONE:
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                /* The "try" associated with this "finally" must be in the
                * same loop, so the finally block will return control inside the loop */
                break;

            case BBJ_THROW:
            case BBJ_RETURN:
                /* those are exits from the loop */
                lastExit = block;
                exitCount++;
                break;

            case BBJ_SWITCH:

                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    noway_assert(*jumpTab);
                    exitPoint = *jumpTab;

                    if (!loopBlocks.IsMember(exitPoint->bbNum))
                    {
                        lastExit = block;
                        exitCount++;
                    }
                } while (++jumpTab, --jumpCnt);
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
}

/*****************************************************************************
 * Find the natural loops, using dominators. Note that the test for
 * a loop is slightly different from the standard one, because we have
 * not done a depth first reordering of the basic blocks.
 */

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

    for (BasicBlock* head = fgFirstBB; head->bbNext; head = head->bbNext)
    {
        BasicBlock* top = head->bbNext;

        //  Blocks that are rarely run have a zero bbWeight and should
        //  never be optimized here

        if (top->bbWeight == BB_ZERO_WEIGHT)
        {
            continue;
        }

        for (flowList* pred = top->bbPreds; pred; pred = pred->flNext)
        {
            if (search.FindLoop(head, top, pred->getBlock()))
            {
                // Found a loop; record it and see if we've hit the limit.
                bool recordedLoop = search.RecordLoop();

                (void)recordedLoop; // avoid unusued variable warnings in COUNT_LOOPS and !DEBUG

#if COUNT_LOOPS
                if (!hasMethodLoops)
                {
                    /* mark the method as containing natural loops */
                    totalLoopMethods++;
                    hasMethodLoops = true;
                }

                /* increment total number of loops found */
                totalLoopCount++;
                loopsThisMethod++;

                /* keep track of the number of exits */
                loopExitCountTable.record(static_cast<unsigned>(search.GetExitCount()));

                // Note that we continue to look for loops even if
                // (optLoopCount == MAX_LOOP_NUM), in contrast to the !COUNT_LOOPS code below.
                // This gives us a better count and stats. Hopefully it doesn't affect actual codegen.
                CLANG_FORMAT_COMMENT_ANCHOR;

#else  // COUNT_LOOPS
                assert(recordedLoop);
                if (optLoopCount == MAX_LOOP_NUM)
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
        // until after canonicalizing loops.  Dominator information is
        // recorded in terms of block numbers, so flag it invalid.
        fgDomsComputed = false;
        fgRenumberBlocks();
    }

    // Now the loop indices are stable.  We can figure out parent/child relationships
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

    // Now label the blocks with the innermost loop to which they belong.  Since parents
    // precede children in the table, doing the labeling for each loop in order will achieve
    // this -- the innermost loop labeling will be done last.
    for (unsigned char loopInd = 0; loopInd < optLoopCount; loopInd++)
    {
        BasicBlock* first  = optLoopTable[loopInd].lpFirst;
        BasicBlock* bottom = optLoopTable[loopInd].lpBottom;
        for (BasicBlock* blk = first; blk != nullptr; blk = blk->bbNext)
        {
            blk->bbNatLoopNum = loopInd;
            if (blk == bottom)
            {
                break;
            }
            assert(blk->bbNext != nullptr); // We should never reach nullptr.
        }
    }

    // Make sure that loops are canonical: that every loop has a unique "top", by creating an empty "nop"
    // one, if necessary, for loops containing others that share a "top."
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
        constexpr bool computePreds = true;
        fgUpdateChangedFlowGraph(computePreds);
    }

#ifdef DEBUG
    if (verbose && optLoopCount > 0)
    {
        printf("\nFinal natural loop table:\n");
        for (unsigned loopInd = 0; loopInd < optLoopCount; loopInd++)
        {
            optPrintLoopInfo(loopInd);
            printf("\n");
        }
    }
#endif // DEBUG
}

//-----------------------------------------------------------------------------
//
// All the inner loops that whose block weight meets a threshold are marked
// as needing alignment.
//

void Compiler::optIdentifyLoopsForAlignment()
{
#if FEATURE_LOOP_ALIGN
    if (codeGen->ShouldAlignLoops())
    {
        for (unsigned char loopInd = 0; loopInd < optLoopCount; loopInd++)
        {
            BasicBlock* first = optLoopTable[loopInd].lpFirst;

            // An innerloop candidate that might need alignment
            if (optLoopTable[loopInd].lpChild == BasicBlock::NOT_IN_LOOP)
            {
                if (first->getBBWeight(this) >= (opts.compJitAlignLoopMinBlockWeight * BB_UNITY_WEIGHT))
                {
                    first->bbFlags |= BBF_LOOP_ALIGN;
                    JITDUMP(FMT_LP " that starts at " FMT_BB " needs alignment, weight=" FMT_WT ".\n", loopInd,
                            first->bbNum, first->getBBWeight(this));
                }
                else
                {
                    JITDUMP("Skip alignment for " FMT_LP " that starts at " FMT_BB " weight=" FMT_WT ".\n", loopInd,
                            first->bbNum, first->getBBWeight(this));
                }
            }
        }
    }
#endif
}

void Compiler::optRedirectBlock(BasicBlock* blk, BlockToBlockMap* redirectMap, const bool addPreds)
{
    BasicBlock* newJumpDest = nullptr;
    switch (blk->bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFILTERRET:
        case BBJ_EHFINALLYRET:
        case BBJ_EHCATCHRET:
            // These have no jump destination to update.
            break;

        case BBJ_NONE:
            if (addPreds)
            {
                fgAddRefPred(blk->bbNext, blk);
            }
            break;

        case BBJ_ALWAYS:
        case BBJ_LEAVE:
        case BBJ_CALLFINALLY:
        case BBJ_COND:
            // All of these have a single jump destination to update.
            if (redirectMap->Lookup(blk->bbJumpDest, &newJumpDest))
            {
                blk->bbJumpDest = newJumpDest;
            }
            if (addPreds)
            {
                fgAddRefPred(blk->bbJumpDest, blk);
                if (blk->bbJumpKind == BBJ_COND)
                {
                    // Add a pred for the fall-through path.
                    fgAddRefPred(blk->bbNext, blk);
                }
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
                    switchDest                   = newJumpDest;
                    blk->bbJumpSwt->bbsDstTab[i] = newJumpDest;
                    redirected                   = true;
                }
                if (addPreds)
                {
                    fgAddRefPred(switchDest, blk);
                }
            }
            // If any redirections happend, invalidate the switch table map for the switch.
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
        {
            to->bbJumpSwt            = new (this, CMK_BasicBlock) BBswtDesc();
            to->bbJumpSwt->bbsCount  = from->bbJumpSwt->bbsCount;
            to->bbJumpSwt->bbsDstTab = new (this, CMK_BasicBlock) BasicBlock*[from->bbJumpSwt->bbsCount];

            for (unsigned i = 0; i < from->bbJumpSwt->bbsCount; i++)
            {
                to->bbJumpSwt->bbsDstTab[i] = from->bbJumpSwt->bbsDstTab[i];
            }
        }
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
        // Traverse the outermost loops as entries into the loop nest; so skip non-outermost.
        if (optLoopTable[loopInd].lpEntry == block)
        {
            return true;
        }
    }
    return false;
}

// Canonicalize the loop nest rooted at parent loop 'loopInd'.
// Returns 'true' if the flow graph is modified.
bool Compiler::optCanonicalizeLoopNest(unsigned char loopInd)
{
    bool modified = false;

    // Is the top of the current loop not in any nested loop?
    if (optLoopTable[loopInd].lpTop->bbNatLoopNum != loopInd)
    {
        if (optCanonicalizeLoop(loopInd))
        {
            modified = true;
        }
    }

    for (unsigned char child = optLoopTable[loopInd].lpChild; child != BasicBlock::NOT_IN_LOOP;
         child               = optLoopTable[child].lpSibling)
    {
        if (optCanonicalizeLoopNest(child))
        {
            modified = true;
        }
    }

    return modified;
}

bool Compiler::optCanonicalizeLoop(unsigned char loopInd)
{
    // Is the top uniquely part of the current loop?
    BasicBlock* t = optLoopTable[loopInd].lpTop;

    if (t->bbNatLoopNum == loopInd)
    {
        return false;
    }

    JITDUMP("in optCanonicalizeLoop: " FMT_LP " has top " FMT_BB " (bottom " FMT_BB ") with natural loop number " FMT_LP
            ": need to canonicalize\n",
            loopInd, t->bbNum, optLoopTable[loopInd].lpBottom->bbNum, t->bbNatLoopNum);

    // Otherwise, the top of this loop is also part of a nested loop.
    //
    // Insert a new unique top for this loop. We must be careful to put this new
    // block in the correct EH region. Note that f->bbPrev might be in a different
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

    BasicBlock* h = optLoopTable[loopInd].lpHead;
    BasicBlock* f = optLoopTable[loopInd].lpFirst;
    BasicBlock* b = optLoopTable[loopInd].lpBottom;

    // The loop must be entirely contained within a single handler region.
    assert(BasicBlock::sameHndRegion(f, b));

    // If the bottom block is in the same "try" region, then we extend the EH
    // region. Otherwise, we add the new block outside the "try" region.
    bool        extendRegion = BasicBlock::sameTryRegion(f, b);
    BasicBlock* newT         = fgNewBBbefore(BBJ_NONE, f, extendRegion);
    if (!extendRegion)
    {
        // We need to set the EH region manually. Set it to be the same
        // as the bottom block.
        newT->copyEHRegion(b);
    }

    // The new block can reach the same set of blocks as the old one, but don't try to reflect
    // that in its reachability set here -- creating the new block may have changed the BlockSet
    // representation from short to long, and canonicalizing loops is immediately followed by
    // a call to fgUpdateChangedFlowGraph which will recompute the reachability sets anyway.

    // Redirect the "bottom" of the current loop to "newT".
    BlockToBlockMap* blockMap = new (getAllocatorLoopHoist()) BlockToBlockMap(getAllocatorLoopHoist());
    blockMap->Set(t, newT);
    optRedirectBlock(b, blockMap);

    // Redirect non-loop preds of "t" to also go to "newT". Inner loops that also branch to "t" should continue
    // to do so. However, there maybe be other predecessors from outside the loop nest that need to be updated
    // to point to "newT". This normally wouldn't happen, since they too would be part of the loop nest. However,
    // they might have been prevented from participating in the loop nest due to different EH nesting, or some
    // other reason.
    //
    // Note that optRedirectBlock doesn't update the predecessors list. So, if the same 't' block is processed
    // multiple times while canonicalizing multiple loop nests, we'll attempt to redirect a predecessor multiple times.
    // This is ok, because after the first redirection, the topPredBlock branch target will no longer match the source
    // edge of the blockMap, so nothing will happen.
    bool firstPred = true;
    for (flowList* topPred = t->bbPreds; topPred != nullptr; topPred = topPred->flNext)
    {
        BasicBlock* topPredBlock = topPred->getBlock();

        // Skip if topPredBlock is in the loop.
        // Note that this uses block number to detect membership in the loop. We are adding blocks during
        // canonicalization, and those block numbers will be new, and larger than previous blocks. However, we work
        // outside-in, so we shouldn't encounter the new blocks at the loop boundaries, or in the predecessor lists.
        if (t->bbNum <= topPredBlock->bbNum && topPredBlock->bbNum <= b->bbNum)
        {
            JITDUMP("in optCanonicalizeLoop: 'top' predecessor " FMT_BB " is in the range of " FMT_LP " (" FMT_BB
                    ".." FMT_BB "); not redirecting its bottom edge\n",
                    topPredBlock->bbNum, loopInd, t->bbNum, b->bbNum);
            continue;
        }

        JITDUMP("in optCanonicalizeLoop: redirect top predecessor " FMT_BB " to " FMT_BB "\n", topPredBlock->bbNum,
                newT->bbNum);
        optRedirectBlock(topPredBlock, blockMap);

        // When we have profile data then the 'newT' block will inherit topPredBlock profile weight
        if (topPredBlock->hasProfileWeight())
        {
            // This corrects an issue when the topPredBlock has a profile based weight
            //
            if (firstPred)
            {
                JITDUMP("in optCanonicalizeLoop: block " FMT_BB " will inheritWeight from " FMT_BB "\n", newT->bbNum,
                        topPredBlock->bbNum);

                newT->inheritWeight(topPredBlock);
                firstPred = false;
            }
            else
            {
                JITDUMP("in optCanonicalizeLoop: block " FMT_BB " will also contribute to the weight of " FMT_BB "\n",
                        newT->bbNum, topPredBlock->bbNum);

                BasicBlock::weight_t newWeight = newT->getBBWeight(this) + topPredBlock->getBBWeight(this);
                newT->setBBProfileWeight(newWeight);
            }
        }
    }

    assert(newT->bbNext == f);
    if (f != t)
    {
        newT->bbJumpKind = BBJ_ALWAYS;
        newT->bbJumpDest = t;
        newT->bbStmtList = nullptr;
        fgInsertStmtAtEnd(newT, fgNewStmtFromTree(gtNewOperNode(GT_NOP, TYP_VOID, nullptr)));
    }

    // If it had been a do-while loop (top == entry), update entry, as well.
    BasicBlock* origE = optLoopTable[loopInd].lpEntry;
    if (optLoopTable[loopInd].lpTop == origE)
    {
        optLoopTable[loopInd].lpEntry = newT;
    }
    optLoopTable[loopInd].lpTop   = newT;
    optLoopTable[loopInd].lpFirst = newT;

    newT->bbNatLoopNum = loopInd;

    JITDUMP("in optCanonicalizeLoop: made new block " FMT_BB " [%p] the new unique top of loop %d.\n", newT->bbNum,
            dspPtr(newT), loopInd);

    // Make sure the head block still goes to the entry...
    if (h->bbJumpKind == BBJ_NONE && h->bbNext != optLoopTable[loopInd].lpEntry)
    {
        h->bbJumpKind = BBJ_ALWAYS;
        h->bbJumpDest = optLoopTable[loopInd].lpEntry;
    }
    else if (h->bbJumpKind == BBJ_COND && h->bbNext == newT && newT != optLoopTable[loopInd].lpEntry)
    {
        BasicBlock* h2               = fgNewBBafter(BBJ_ALWAYS, h, /*extendRegion*/ true);
        optLoopTable[loopInd].lpHead = h2;
        h2->bbJumpDest               = optLoopTable[loopInd].lpEntry;
        h2->bbStmtList               = nullptr;
        fgInsertStmtAtEnd(h2, fgNewStmtFromTree(gtNewOperNode(GT_NOP, TYP_VOID, nullptr)));
    }

    // If any loops nested in "loopInd" have the same head and entry as "loopInd",
    // it must be the case that they were do-while's (since "h" fell through to the entry).
    // The new node "newT" becomes the head of such loops.
    for (unsigned char childLoop = optLoopTable[loopInd].lpChild; childLoop != BasicBlock::NOT_IN_LOOP;
         childLoop               = optLoopTable[childLoop].lpSibling)
    {
        if (optLoopTable[childLoop].lpEntry == origE && optLoopTable[childLoop].lpHead == h &&
            newT->bbJumpKind == BBJ_NONE && newT->bbNext == origE)
        {
            optUpdateLoopHead(childLoop, h, newT);
        }
    }
    return true;
}

bool Compiler::optLoopContains(unsigned l1, unsigned l2)
{
    assert(l1 != BasicBlock::NOT_IN_LOOP);
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

void Compiler::optUpdateLoopHead(unsigned loopInd, BasicBlock* from, BasicBlock* to)
{
    assert(optLoopTable[loopInd].lpHead == from);
    optLoopTable[loopInd].lpHead = to;
    for (unsigned char childLoop = optLoopTable[loopInd].lpChild; childLoop != BasicBlock::NOT_IN_LOOP;
         childLoop               = optLoopTable[childLoop].lpSibling)
    {
        if (optLoopTable[childLoop].lpHead == from)
        {
            optUpdateLoopHead(childLoop, from, to);
        }
    }
}

/*****************************************************************************
 * If the : i += const" will cause an overflow exception for the small types.
 */

bool jitIterSmallOverflow(int iterAtExit, var_types incrType)
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

/*****************************************************************************
 * If the "i -= const" will cause an underflow exception for the small types
 */

bool jitIterSmallUnderflow(int iterAtExit, var_types decrType)
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

/*****************************************************************************
 *
 *  Helper for unroll loops - Computes the number of repetitions
 *  in a constant loop. If it cannot prove the number is constant returns false
 */

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

    /* If iterInc is zero we have an infinite loop */
    if (iterInc == 0)
    {
        return false;
    }

    /* Set iterSign to +1 for positive iterInc and -1 for negative iterInc */
    iterSign = (iterInc > 0) ? +1 : -1;

    /* Initialize loopCount to zero */
    loopCount = 0;

    // If dupCond is true then the loop head contains a test which skips
    // this loop, if the constInit does not pass the loop test
    // Such a loop can execute zero times.
    // If dupCond is false then we have a true do-while loop which we
    // always execute the loop once before performing the loop test
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

    /* Compute the number of repetitions */

    switch (testOper)
    {
        __int64 iterAtExitX;

        case GT_EQ:
            /* something like "for (i=init; i == lim; i++)" doesn't make any sense */
            return false;

        case GT_NE:
            /*  "for (i=init; i != lim; i+=const)" - this is tricky since it may
             *  have a constant number of iterations or loop forever -
             *  we have to compute (lim-init) mod iterInc to see if it is zero.
             * If mod iterInc is not zero then the limit test will miss an a wrap will occur
             * which is probably not what the end user wanted, but it is legal.
             */

            if (iterInc > 0)
            {
                /* Stepping by one, i.e. Mod with 1 is always zero */
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
                /* Stepping by -1, i.e. Mod with 1 is always zero */
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
                    if (jitIterSmallOverflow((int)iterAtExitX, iterOperType))
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
                    if (jitIterSmallOverflow((int)iterAtExitX, iterOperType))
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
                    if (jitIterSmallOverflow((int)iterAtExitX, iterOperType))
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
                    if (jitIterSmallUnderflow((int)iterAtExitX, iterOperType))
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
                    if (jitIterSmallUnderflow((int)iterAtExitX, iterOperType))
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

/*****************************************************************************
 *
 *  Look for loop unrolling candidates and unroll them
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void Compiler::optUnrollLoops()
{
    if (compCodeOpt() == SMALL_CODE)
    {
        return;
    }

    if (optLoopCount == 0)
    {
        return;
    }

#ifdef DEBUG
    if (JitConfig.JitNoUnroll())
    {
        return;
    }
#endif

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optUnrollLoops()\n");
    }
#endif
    /* Look for loop unrolling candidates */

    bool change = false;

    // Visit loops from highest to lowest number to visit them in innermost
    // to outermost order.
    for (unsigned lnum = optLoopCount - 1; lnum != ~0U; --lnum)
    {
        // This is necessary due to an apparent analysis limitation since
        // optLoopCount must be strictly greater than 0 upon entry and lnum
        // cannot wrap due to the loop termination condition.
        PREFAST_ASSUME(lnum != 0U - 1);

        BasicBlock* block;
        BasicBlock* head;
        BasicBlock* bottom;

        bool       dupCond;
        int        lval;
        int        lbeg;         // initial value for iterator
        int        llim;         // limit value for iterator
        unsigned   lvar;         // iterator lclVar #
        int        iterInc;      // value to increment the iterator
        genTreeOps iterOper;     // type of iterator increment (i.e. ADD, SUB, etc.)
        var_types  iterOperType; // type result of the oper (for overflow instrs)
        genTreeOps testOper;     // type of loop test (i.e. GT_LE, GT_GE, etc.)
        bool       unsTest;      // Is the comparison u/int

        unsigned loopRetCount;  // number of BBJ_RETURN blocks in loop
        unsigned totalIter;     // total number of iterations in the constant loop
        unsigned loopFlags;     // actual lpFlags
        unsigned requiredFlags; // required lpFlags

        static const int ITER_LIMIT[COUNT_OPT_CODE + 1] = {
            10, // BLENDED_CODE
            0,  // SMALL_CODE
            20, // FAST_CODE
            0   // COUNT_OPT_CODE
        };

        noway_assert(ITER_LIMIT[SMALL_CODE] == 0);
        noway_assert(ITER_LIMIT[COUNT_OPT_CODE] == 0);

        unsigned iterLimit = (unsigned)ITER_LIMIT[compCodeOpt()];

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

        noway_assert(UNROLL_LIMIT_SZ[SMALL_CODE] == 0);
        noway_assert(UNROLL_LIMIT_SZ[COUNT_OPT_CODE] == 0);

        int unrollLimitSz = (unsigned)UNROLL_LIMIT_SZ[compCodeOpt()];

        loopFlags = optLoopTable[lnum].lpFlags;
        // Check for required flags:
        // LPFLG_DO_WHILE - required because this transform only handles loops of this form
        // LPFLG_CONST - required because this transform only handles full unrolls
        requiredFlags = LPFLG_DO_WHILE | LPFLG_CONST;

        /* Ignore the loop if we don't have a do-while
        that has a constant number of iterations */

        if ((loopFlags & requiredFlags) != requiredFlags)
        {
            continue;
        }

        /* ignore if removed or marked as not unrollable */

        if (loopFlags & (LPFLG_DONT_UNROLL | LPFLG_REMOVED))
        {
            continue;
        }

        head = optLoopTable[lnum].lpHead;
        noway_assert(head);
        bottom = optLoopTable[lnum].lpBottom;
        noway_assert(bottom);

        /* Get the loop data:
            - initial constant
            - limit constant
            - iterator
            - iterator increment
            - increment operation type (i.e. ADD, SUB, etc...)
            - loop test type (i.e. GT_GE, GT_LT, etc...)
            */

        lbeg     = optLoopTable[lnum].lpConstInit;
        llim     = optLoopTable[lnum].lpConstLimit();
        testOper = optLoopTable[lnum].lpTestOper();

        lvar     = optLoopTable[lnum].lpIterVar();
        iterInc  = optLoopTable[lnum].lpIterConst();
        iterOper = optLoopTable[lnum].lpIterOper();

        iterOperType = optLoopTable[lnum].lpIterOperType();
        unsTest      = (optLoopTable[lnum].lpTestTree->gtFlags & GTF_UNSIGNED) != 0;

        if (lvaTable[lvar].lvAddrExposed)
        { // If the loop iteration variable is address-exposed then bail
            continue;
        }
        if (lvaTable[lvar].lvIsStructField)
        { // If the loop iteration variable is a promoted field from a struct then
            // bail
            continue;
        }

        // Locate/initialize the increment/test statements.
        Statement* initStmt = head->lastStmt();
        noway_assert((initStmt != nullptr) && (initStmt->GetNextStmt() == nullptr));

        Statement* testStmt = bottom->lastStmt();
        noway_assert((testStmt != nullptr) && (testStmt->GetNextStmt() == nullptr));
        Statement* incrStmt = testStmt->GetPrevStmt();
        noway_assert(incrStmt != nullptr);

        if (initStmt->IsCompilerAdded())
        {
            /* Must be a duplicated loop condition */
            noway_assert(initStmt->GetRootNode()->gtOper == GT_JTRUE);

            dupCond  = true;
            initStmt = initStmt->GetPrevStmt();
            noway_assert(initStmt != nullptr);
        }
        else
        {
            dupCond = false;
        }

        /* Find the number of iterations - the function returns false if not a constant number */

        if (!optComputeLoopRep(lbeg, llim, iterInc, iterOper, iterOperType, testOper, unsTest, dupCond, &totalIter))
        {
            continue;
        }

        /* Forget it if there are too many repetitions or not a constant loop */

        if (totalIter > iterLimit)
        {
            continue;
        }

        if (INDEBUG(compStressCompile(STRESS_UNROLL_LOOPS, 50) ||) false)
        {
            // In stress mode, quadruple the size limit, and drop
            // the restriction that loop limit must be vector element count.
            unrollLimitSz *= 4;
        }
        else if (totalIter <= 1)
        {
            // No limit for single iteration loops
            unrollLimitSz = INT_MAX;
        }
        else if (!(loopFlags & LPFLG_SIMD_LIMIT))
        {
            // Otherwise unroll only if limit is Vector_.Length
            // (as a heuristic, not for correctness/structural reasons)
            continue;
        }

        GenTree* incr = incrStmt->GetRootNode();

        // Don't unroll loops we don't understand.
        if (incr->gtOper != GT_ASG)
        {
            continue;
        }
        incr = incr->AsOp()->gtOp2;

        GenTree* init = initStmt->GetRootNode();

        /* Make sure everything looks ok */
        if ((init->gtOper != GT_ASG) || (init->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
            (init->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() != lvar) ||
            (init->AsOp()->gtOp2->gtOper != GT_CNS_INT) || (init->AsOp()->gtOp2->AsIntCon()->gtIconVal != lbeg) ||

            !((incr->gtOper == GT_ADD) || (incr->gtOper == GT_SUB)) || (incr->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
            (incr->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() != lvar) ||
            (incr->AsOp()->gtOp2->gtOper != GT_CNS_INT) || (incr->AsOp()->gtOp2->AsIntCon()->gtIconVal != iterInc) ||

            (testStmt->GetRootNode()->gtOper != GT_JTRUE))
        {
            noway_assert(!"Bad precondition in Compiler::optUnrollLoops()");
            continue;
        }

        /* heuristic - Estimated cost in code size of the unrolled loop */

        {
            ClrSafeInt<unsigned> loopCostSz; // Cost is size of one iteration

            block         = head->bbNext;
            auto tryIndex = block->bbTryIndex;

            loopRetCount = 0;
            for (;; block = block->bbNext)
            {
                if (block->bbTryIndex != tryIndex)
                {
                    // Unrolling would require cloning EH regions
                    goto DONE_LOOP;
                }

                if (block->bbJumpKind == BBJ_RETURN)
                {
                    ++loopRetCount;
                }

                // Visit all the statements in the block.

                for (Statement* stmt : block->Statements())
                {
                    /* Calculate GetCostSz() */
                    gtSetStmtInfo(stmt);

                    /* Update loopCostSz */
                    loopCostSz += stmt->GetCostSz();
                }

                if (block == bottom)
                {
                    break;
                }
            }

#ifdef JIT32_GCENCODER
            if (fgReturnCount + loopRetCount * (totalIter - 1) > SET_EPILOGCNT_MAX)
            {
                // Jit32 GC encoder can't report more than SET_EPILOGCNT_MAX epilogs.
                goto DONE_LOOP;
            }
#endif // !JIT32_GCENCODER

            /* Compute the estimated increase in code size for the unrolled loop */

            ClrSafeInt<unsigned> fixedLoopCostSz(8);

            ClrSafeInt<int> unrollCostSz = ClrSafeInt<int>(loopCostSz * ClrSafeInt<unsigned>(totalIter)) -
                                           ClrSafeInt<int>(loopCostSz + fixedLoopCostSz);

            /* Don't unroll if too much code duplication would result. */

            if (unrollCostSz.IsOverflow() || (unrollCostSz.Value() > unrollLimitSz))
            {
                goto DONE_LOOP;
            }

            /* Looks like a good idea to unroll this loop, let's do it! */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (verbose)
            {
                printf("\nUnrolling loop " FMT_BB, head->bbNext->bbNum);
                if (head->bbNext->bbNum != bottom->bbNum)
                {
                    printf(".." FMT_BB, bottom->bbNum);
                }
                printf(" over V%02u from %u to %u", lvar, lbeg, llim);
                printf(" unrollCostSz = %d\n", unrollCostSz);
                printf("\n");
            }
#endif
        }

#if FEATURE_LOOP_ALIGN
        for (block = head->bbNext;; block = block->bbNext)
        {
            if (block->isLoopAlign())
            {
                block->bbFlags &= ~BBF_LOOP_ALIGN;
                JITDUMP("Removing LOOP_ALIGN flag from unrolled loop in " FMT_BB "\n", block->bbNum);
            }

            if (block == bottom)
            {
                break;
            }
        }
#endif

        /* Create the unrolled loop statement list */
        {
            BlockToBlockMap blockMap(getAllocator(CMK_LoopOpt));
            BasicBlock*     insertAfter = bottom;

            for (lval = lbeg; totalIter; totalIter--)
            {
                for (block = head->bbNext;; block = block->bbNext)
                {
                    BasicBlock* newBlock = insertAfter =
                        fgNewBBafter(block->bbJumpKind, insertAfter, /*extendRegion*/ true);
                    blockMap.Set(block, newBlock, BlockToBlockMap::Overwrite);

                    if (!BasicBlock::CloneBlockState(this, newBlock, block, lvar, lval))
                    {
                        // cloneExpr doesn't handle everything
                        BasicBlock* oldBottomNext = insertAfter->bbNext;
                        bottom->bbNext            = oldBottomNext;
                        oldBottomNext->bbPrev     = bottom;
                        optLoopTable[lnum].lpFlags |= LPFLG_DONT_UNROLL;
                        goto DONE_LOOP;
                    }

                    // Block weight should no longer have the loop multiplier
                    //
                    // Note this is not quite right, as we may not have upscaled by this amount
                    // and we might not have upscaled at all, if we had profile data.
                    //
                    newBlock->scaleBBWeight(1.0f / BB_LOOP_WEIGHT_SCALE);

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

                        // Exit this loop; we've walked all the blocks.
                        break;
                    }
                }

                // Now redirect any branches within the newly-cloned iteration
                for (block = head->bbNext; block != bottom; block = block->bbNext)
                {
                    BasicBlock* newBlock = blockMap[block];
                    optCopyBlkDest(block, newBlock);
                    optRedirectBlock(newBlock, &blockMap);
                }

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

            // Gut the old loop body
            for (block = head->bbNext;; block = block->bbNext)
            {
                block->bbStmtList = nullptr;
                block->bbJumpKind = BBJ_NONE;
                block->bbFlags &= ~BBF_LOOP_HEAD;
                if (block->bbJumpDest != nullptr)
                {
                    block->bbJumpDest = nullptr;
                }

                if (block == bottom)
                {
                    break;
                }
            }

            /* if the HEAD is a BBJ_COND drop the condition (and make HEAD a BBJ_NONE block) */

            if (head->bbJumpKind == BBJ_COND)
            {
                Statement* preHeaderStmt = head->firstStmt();
                noway_assert(preHeaderStmt != nullptr);
                testStmt = preHeaderStmt->GetPrevStmt();

                noway_assert((testStmt != nullptr) && (testStmt->GetNextStmt() == nullptr));
                noway_assert(testStmt->GetRootNode()->gtOper == GT_JTRUE);

                initStmt = testStmt->GetPrevStmt();
                noway_assert((initStmt != nullptr) && (initStmt->GetNextStmt() == testStmt));

                initStmt->SetNextStmt(nullptr);
                preHeaderStmt->SetPrevStmt(initStmt);
                head->bbJumpKind = BBJ_NONE;
            }
            else
            {
                /* the loop must execute */
                noway_assert(head->bbJumpKind == BBJ_NONE);
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("Whole unrolled loop:\n");

                gtDispTree(initStmt->GetRootNode());
                printf("\n");
                fgDumpTrees(head->bbNext, insertAfter);
            }
#endif

            /* Remember that something has changed */

            change = true;

            /* Make sure to update loop table */

            /* Use the LPFLG_REMOVED flag and update the bbLoopMask accordingly
                * (also make head and bottom NULL - to hit an assert or an access violation) */

            optLoopTable[lnum].lpFlags |= LPFLG_REMOVED;
            optLoopTable[lnum].lpHead = optLoopTable[lnum].lpBottom = nullptr;

            // Note if we created new BBJ_RETURNs
            fgReturnCount += loopRetCount * (totalIter - 1);
        }

    DONE_LOOP:;
    }

    if (change)
    {
        constexpr bool computePreds = true;
        fgUpdateChangedFlowGraph(computePreds);
    }

#ifdef DEBUG
    fgDebugCheckBBlist(true);
#endif
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

    noway_assert(topBB->bbNum <= botBB->bbNum);

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

// static
Compiler::fgWalkResult Compiler::optInvertCountTreeInfo(GenTree** pTree, fgWalkData* data)
{
    OptInvertCountTreeInfoType* o = (OptInvertCountTreeInfoType*)data->pCallbackData;

    if (Compiler::IsSharedStaticHelper(*pTree))
    {
        o->sharedStaticHelperCount += 1;
    }

    if ((*pTree)->OperGet() == GT_ARR_LENGTH)
    {
        o->arrayLengthCount += 1;
    }

    return WALK_CONTINUE;
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
//          ..stmts..               // duplicated cond block statments
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
void Compiler::optInvertWhileLoop(BasicBlock* block)
{
    assert(opts.OptimizationEnabled());
    assert(compCodeOpt() != SMALL_CODE);

    /* Does the BB end with an unconditional jump? */

    if (block->bbJumpKind != BBJ_ALWAYS || (block->bbFlags & BBF_KEEP_BBJ_ALWAYS))
    {
        // It can't be one of the ones we use for our exception magic
        return;
    }

    // block can't be the scratch bb, since we prefer to keep flow
    // out of the scratch bb as BBJ_ALWAYS or BBJ_NONE.
    if (fgBBisScratch(block))
    {
        return;
    }

    // Get hold of the jump target
    BasicBlock* bTest = block->bbJumpDest;

    // Does the block consist of 'jtrue(cond) block' ?
    if (bTest->bbJumpKind != BBJ_COND)
    {
        return;
    }

    // bTest must be a backwards jump to block->bbNext
    if (bTest->bbJumpDest != block->bbNext)
    {
        return;
    }

    // Since test is a BBJ_COND it will have a bbNext
    noway_assert(bTest->bbNext != nullptr);

    // 'block' must be in the same try region as the condition, since we're going to insert
    // a duplicated condition in 'block', and the condition might include exception throwing code.
    if (!BasicBlock::sameTryRegion(block, bTest))
    {
        return;
    }

    // We're going to change 'block' to branch to bTest->bbNext, so that also better be in the
    // same try region (or no try region) to avoid generating illegal flow.
    BasicBlock* bTestNext = bTest->bbNext;
    if (bTestNext->hasTryIndex() && !BasicBlock::sameTryRegion(block, bTestNext))
    {
        return;
    }

    // It has to be a forward jump. Defer this check until after all the cheap checks
    // are done, since it iterates forward in the block list looking for bbJumpDest.
    //  TODO-CQ: Check if we can also optimize the backwards jump as well.
    //
    if (!fgIsForwardBranch(block))
    {
        return;
    }

    // Find the loop termination test at the bottom of the loop.
    Statement* condStmt = bTest->lastStmt();

    // Verify the test block ends with a conditional that we can manipulate.
    GenTree* const condTree = condStmt->GetRootNode();
    noway_assert(condTree->gtOper == GT_JTRUE);
    if (!condTree->AsOp()->gtOp1->OperIsCompare())
    {
        return;
    }

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

    unsigned estDupCostSz = 0;

    for (Statement* stmt : bTest->Statements())
    {
        GenTree* tree = stmt->GetRootNode();
        gtPrepareCost(tree);
        estDupCostSz += tree->GetCostSz();
    }

    BasicBlock::weight_t       loopIterations            = BB_LOOP_WEIGHT_SCALE;
    bool                       allProfileWeightsAreValid = false;
    BasicBlock::weight_t const weightBlock               = block->bbWeight;
    BasicBlock::weight_t const weightTest                = bTest->bbWeight;
    BasicBlock::weight_t const weightNext                = block->bbNext->bbWeight;

    // If we have profile data then we calculate the number of times
    // the loop will iterate into loopIterations
    if (fgIsUsingProfileWeights())
    {
        // Only rely upon the profile weight when all three of these blocks
        // have good profile weights
        if (block->hasProfileWeight() && bTest->hasProfileWeight() && block->bbNext->hasProfileWeight())
        {
            // If this while loop never iterates then don't bother transforming
            //
            if (weightNext == BB_ZERO_WEIGHT)
            {
                return;
            }

            // We generally expect weightTest == weightNext + weightBlock.
            //
            // Tolerate small inconsistencies...
            //
            if (!fgProfileWeightsConsistent(weightBlock + weightNext, weightTest))
            {
                JITDUMP("Profile weights locally inconsistent: block " FMT_WT ", next " FMT_WT ", test " FMT_WT "\n",
                        weightBlock, weightNext, weightTest);
            }
            else
            {
                allProfileWeightsAreValid = true;

                // Determine iteration count
                //
                //   weightNext is the number of time this loop iterates
                //   weightBlock is the number of times that we enter the while loop
                //   loopIterations is the average number of times that this loop iterates
                //
                loopIterations = weightNext / weightBlock;
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

        for (Statement* stmt : bTest->Statements())
        {
            GenTree* tree = stmt->GetRootNode();

            OptInvertCountTreeInfoType optInvertInfo = {};
            fgWalkTreePre(&tree, Compiler::optInvertCountTreeInfo, &optInvertInfo);
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
        return;
    }

    bool foundCondTree = false;

    // Clone each statement in bTest and append to block.
    for (Statement* stmt : bTest->Statements())
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

        Statement* clonedStmt = fgNewStmtAtEnd(block, clonedTree);

        if (opts.compDbgInfo)
        {
            clonedStmt->SetILOffsetX(stmt->GetILOffsetX());
        }

        clonedStmt->SetCompilerAdded();
    }

    assert(foundCondTree);

    // Flag the block that received the copy as potentially having an array/vtable
    // reference, nullcheck, object/array allocation if the block copied from did;
    // this is a conservative guess.
    if (auto copyFlags = bTest->bbFlags & (BBF_HAS_IDX_LEN | BBF_HAS_NULLCHECK | BBF_HAS_NEWOBJ | BBF_HAS_NEWARRAY))
    {
        block->bbFlags |= copyFlags;
    }

    /* Change the block to end with a conditional jump */

    block->bbJumpKind = BBJ_COND;
    block->bbJumpDest = bTest->bbNext;

    /* Update bbRefs and bbPreds for 'block->bbNext' 'bTest' and 'bTest->bbNext' */

    fgAddRefPred(block->bbNext, block);

    fgRemoveRefPred(bTest, block);
    fgAddRefPred(bTest->bbNext, block);

    // If we have profile data for all blocks and we know that we are cloning the
    // bTest block into block and thus changing the control flow from block so
    // that it no longer goes directly to bTest anymore, we have to adjust
    // various weights.
    //
    if (allProfileWeightsAreValid)
    {
        // Update the weight for bTest
        //
        JITDUMP("Reducing profile weight of " FMT_BB " from " FMT_WT " to " FMT_WT "\n", bTest->bbNum, weightTest,
                weightNext);
        bTest->bbWeight = weightNext;

        // Determine the new edge weights.
        //
        // We project the next/jump ratio for block and bTest by using
        // the original likelihoods out of bTest.
        //
        // Note "next" is the loop top block, not bTest's bbNext,
        // we'll call this latter block "after".
        //
        BasicBlock::weight_t const testToNextLikelihood  = weightNext / weightTest;
        BasicBlock::weight_t const testToAfterLikelihood = 1.0f - testToNextLikelihood;

        // Adjust edges out of bTest (which now has weight weightNext)
        //
        BasicBlock::weight_t const testToNextWeight  = weightNext * testToNextLikelihood;
        BasicBlock::weight_t const testToAfterWeight = weightNext * testToAfterLikelihood;

        flowList* const edgeTestToNext  = fgGetPredForBlock(bTest->bbJumpDest, bTest);
        flowList* const edgeTestToAfter = fgGetPredForBlock(bTest->bbNext, bTest);

        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (iterate loop)\n", bTest->bbNum,
                bTest->bbJumpDest->bbNum, testToNextWeight);
        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (exit loop)\n", bTest->bbNum,
                bTest->bbNext->bbNum, testToAfterWeight);

        edgeTestToNext->setEdgeWeights(testToNextWeight, testToNextWeight, bTest->bbJumpDest);
        edgeTestToAfter->setEdgeWeights(testToAfterWeight, testToAfterWeight, bTest->bbNext);

        // Adjust edges out of block, using the same distribution.
        //
        JITDUMP("Profile weight of " FMT_BB " remains unchanged at " FMT_WT "\n", block->bbNum, weightBlock);

        BasicBlock::weight_t const blockToNextLikelihood  = testToNextLikelihood;
        BasicBlock::weight_t const blockToAfterLikelihood = testToAfterLikelihood;

        BasicBlock::weight_t const blockToNextWeight  = weightBlock * blockToNextLikelihood;
        BasicBlock::weight_t const blockToAfterWeight = weightBlock * blockToAfterLikelihood;

        flowList* const edgeBlockToNext  = fgGetPredForBlock(block->bbNext, block);
        flowList* const edgeBlockToAfter = fgGetPredForBlock(block->bbJumpDest, block);

        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (enter loop)\n", block->bbNum,
                block->bbNext->bbNum, blockToNextWeight);
        JITDUMP("Setting weight of " FMT_BB " -> " FMT_BB " to " FMT_WT " (avoid loop)\n", block->bbNum,
                block->bbJumpDest->bbNum, blockToAfterWeight);

        edgeBlockToNext->setEdgeWeights(blockToNextWeight, blockToNextWeight, block->bbNext);
        edgeBlockToAfter->setEdgeWeights(blockToAfterWeight, blockToAfterWeight, block->bbJumpDest);

#ifdef DEBUG
        // Verify profile for the two target blocks is consistent.
        //
        fgDebugCheckIncomingProfileData(block->bbNext);
        fgDebugCheckIncomingProfileData(block->bbJumpDest);
#endif // DEBUG
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDuplicated loop exit block at " FMT_BB " for loop (" FMT_BB " - " FMT_BB ")\n", block->bbNum,
               block->bbNext->bbNum, bTest->bbNum);
        printf("Estimated code size expansion is %d\n", estDupCostSz);

        fgDumpBlock(block);
        fgDumpBlock(bTest);
    }
#endif // DEBUG
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

    if (compCodeOpt() == SMALL_CODE)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        // Make sure the appropriate fields are initialized
        //
        if (block->bbWeight == BB_ZERO_WEIGHT)
        {
            /* Zero weighted block can't have a LOOP_HEAD flag */
            noway_assert(block->isLoopHead() == false);
            continue;
        }

        optInvertWhileLoop(block);
    }

    bool madeChanges = fgModified;

    if (madeChanges)
    {
        // Reset fgModified here as we've done a consistent set of edits.
        //
        fgModified = false;
    }

    // optInvertWhileLoop can cause IR changes even if it does not modify
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
PhaseStatus Compiler::optOptimizeLayout()
{
    noway_assert(opts.OptimizationEnabled());
    noway_assert(fgModified == false);

    bool       madeChanges          = false;
    const bool allowTailDuplication = true;

    madeChanges |= fgUpdateFlowGraph(allowTailDuplication);
    madeChanges |= fgReorderBlocks();
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
// optFindLoops: find and classify natural loops
//
// Notes:
//  Also (re)sets all non-IBC block weights, and marks loops potentially needing
//  alignment padding.
//
PhaseStatus Compiler::optFindLoops()
{
    noway_assert(opts.OptimizationEnabled());

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optFindLoops()\n");
    }
#endif

    optSetBlockWeights();

    /* Were there any loops in the flow graph? */

    if (fgHasLoops)
    {
        /* now that we have dominator information we can find loops */

        optFindNaturalLoops();

        unsigned loopNum = 0;

        /* Iterate over the flow graph, marking all loops */

        /* We will use the following terminology:
         * top        - the first basic block in the loop (i.e. the head of the backward edge)
         * bottom     - the last block in the loop (i.e. the block from which we jump to the top)
         * lastBottom - used when we have multiple back-edges to the same top
         */

        flowList* pred;

        BasicBlock* top;

        for (top = fgFirstBB; top; top = top->bbNext)
        {
            BasicBlock* foundBottom = nullptr;

            for (pred = top->bbPreds; pred; pred = pred->flNext)
            {
                /* Is this a loop candidate? - We look for "back edges" */

                BasicBlock* bottom = pred->getBlock();

                /* is this a backward edge? (from BOTTOM to TOP) */

                if (top->bbNum > bottom->bbNum)
                {
                    continue;
                }

                /* 'top' also must have the BBF_LOOP_HEAD flag set */

                if (top->isLoopHead() == false)
                {
                    continue;
                }

                /* We only consider back-edges that are BBJ_COND or BBJ_ALWAYS for loops */

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
                loopNum++;

                /* Mark all blocks between 'top' and 'bottom' */

                optMarkLoopBlocks(top, foundBottom, false);
            }

            // We track at most 255 loops
            if (loopNum == 255)
            {
#if COUNT_LOOPS
                totalUnnatLoopOverflows++;
#endif
                break;
            }
        }

        // Check if any of the loops need alignment

        JITDUMP("\n");
        optIdentifyLoopsForAlignment();

#if COUNT_LOOPS
        totalUnnatLoopCount += loopNum;
#endif

#ifdef DEBUG
        if (verbose)
        {
            if (loopNum > 0)
            {
                printf("\nFound a total of %d loops.", loopNum);
                printf("\nAfter loop weight marking:\n");
                fgDispBasicBlocks();
                printf("\n");
            }
        }

        fgDebugCheckLoopTable();
#endif
        optLoopsMarked = true;
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// optDeriveLoopCloningConditions: Derive loop cloning conditions.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning info is kept.
//
// Return Value:
//     "false" if conditions cannot be obtained. "true" otherwise.
//     The cloning conditions are updated in the "conditions"[loopNum] field
//     of the "context" parameter.
//
// Operation:
//     Inspect the loop cloning optimization candidates and populate the conditions necessary
//     for each optimization candidate. Checks if the loop stride is "> 0" if the loop
//     condition is "less than". If the initializer is "var" init then adds condition
//     "var >= 0", and if the loop is var limit then, "var >= 0" and "var <= a.len"
//     are added to "context". These conditions are checked in the pre-header block
//     and the cloning choice is made.
//
// Assumption:
//      Callers should assume AND operation is used i.e., if all conditions are
//      true, then take the fast path.
//
bool Compiler::optDeriveLoopCloningConditions(unsigned loopNum, LoopCloneContext* context)
{
    JITDUMP("------------------------------------------------------------\n");
    JITDUMP("Deriving cloning conditions for " FMT_LP "\n", loopNum);

    LoopDsc*                         loop     = &optLoopTable[loopNum];
    JitExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);

    if (loop->lpTestOper() == GT_LT)
    {
        // Stride conditions
        if (loop->lpIterConst() <= 0)
        {
            JITDUMP("> Stride %d is invalid\n", loop->lpIterConst());
            return false;
        }

        // Init conditions
        if (loop->lpFlags & LPFLG_CONST_INIT)
        {
            // Only allowing non-negative const init at this time.
            // REVIEW: why?
            if (loop->lpConstInit < 0)
            {
                JITDUMP("> Init %d is invalid\n", loop->lpConstInit);
                return false;
            }
        }
        else if (loop->lpFlags & LPFLG_VAR_INIT)
        {
            // initVar >= 0
            LC_Condition geZero(GT_GE, LC_Expr(LC_Ident(loop->lpVarInit, LC_Ident::Var)),
                                LC_Expr(LC_Ident(0, LC_Ident::Const)));
            context->EnsureConditions(loopNum)->Push(geZero);
        }
        else
        {
            JITDUMP("> Not variable init\n");
            return false;
        }

        // Limit Conditions
        LC_Ident ident;
        if (loop->lpFlags & LPFLG_CONST_LIMIT)
        {
            int limit = loop->lpConstLimit();
            if (limit < 0)
            {
                JITDUMP("> limit %d is invalid\n", limit);
                return false;
            }
            ident = LC_Ident(static_cast<unsigned>(limit), LC_Ident::Const);
        }
        else if (loop->lpFlags & LPFLG_VAR_LIMIT)
        {
            unsigned limitLcl = loop->lpVarLimit();
            ident             = LC_Ident(limitLcl, LC_Ident::Var);

            LC_Condition geZero(GT_GE, LC_Expr(ident), LC_Expr(LC_Ident(0, LC_Ident::Const)));

            context->EnsureConditions(loopNum)->Push(geZero);
        }
        else if (loop->lpFlags & LPFLG_ARRLEN_LIMIT)
        {
            ArrIndex* index = new (getAllocator(CMK_LoopClone)) ArrIndex(getAllocator(CMK_LoopClone));
            if (!loop->lpArrLenLimit(this, index))
            {
                JITDUMP("> ArrLen not matching");
                return false;
            }
            ident = LC_Ident(LC_Array(LC_Array::Jagged, index, LC_Array::ArrLen));

            // Ensure that this array must be dereference-able, before executing the actual condition.
            LC_Array array(LC_Array::Jagged, index, LC_Array::None);
            context->EnsureDerefs(loopNum)->Push(array);
        }
        else
        {
            JITDUMP("> Undetected limit\n");
            return false;
        }

        for (unsigned i = 0; i < optInfos->Size(); ++i)
        {
            LcOptInfo* optInfo = optInfos->GetRef(i);
            switch (optInfo->GetOptType())
            {
                case LcOptInfo::LcJaggedArray:
                {
                    // limit <= arrLen
                    LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                    LC_Array arrLen(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::ArrLen);
                    LC_Ident arrLenIdent = LC_Ident(arrLen);

                    LC_Condition cond(GT_LE, LC_Expr(ident), LC_Expr(arrLenIdent));
                    context->EnsureConditions(loopNum)->Push(cond);

                    // Ensure that this array must be dereference-able, before executing the actual condition.
                    LC_Array array(LC_Array::Jagged, &arrIndexInfo->arrIndex, arrIndexInfo->dim, LC_Array::None);
                    context->EnsureDerefs(loopNum)->Push(array);
                }
                break;
                case LcOptInfo::LcMdArray:
                {
                    // limit <= mdArrLen
                    LcMdArrayOptInfo* mdArrInfo = optInfo->AsLcMdArrayOptInfo();
                    LC_Condition      cond(GT_LE, LC_Expr(ident),
                                      LC_Expr(LC_Ident(LC_Array(LC_Array::MdArray, mdArrInfo->GetArrIndexForDim(
                                                                                       getAllocator(CMK_LoopClone)),
                                                                mdArrInfo->dim, LC_Array::None))));
                    context->EnsureConditions(loopNum)->Push(cond);
                }
                break;

                default:
                    JITDUMP("Unknown opt\n");
                    return false;
            }
        }
        JITDUMP("Conditions: (");
        DBEXEC(verbose, context->PrintConditions(loopNum));
        JITDUMP(")\n");
        return true;
    }
    return false;
}

//------------------------------------------------------------------------------------
// optComputeDerefConditions: Derive loop cloning conditions for dereferencing arrays.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning info is kept.
//
// Return Value:
//     "false" if conditions cannot be obtained. "true" otherwise.
//     The deref conditions are updated in the "derefConditions"[loopNum] field
//     of the "context" parameter.
//
// Definition of Deref Conditions:
//     To be able to check for the loop cloning condition that (limitVar <= a.len)
//     we should first be able to dereference "a". i.e., "a" is non-null.
//
//     Example:
//
//     for (i in 0..n)
//       for (j in 0..n)
//         for (k in 0..n)      // Inner most loop is being cloned. Cloning needs to check if
//                              // (n <= a[i][j].len) and other safer conditions to take the fast path
//           a[i][j][k] = 0
//
//     Now, we want to deref a[i][j] to invoke length operator on it to perform the cloning fast path check.
//     This involves deref of (a), (a[i]), (a[i][j]), therefore, the following should first
//     be true to do the deref.
//
//     (a != null) && (i < a.len) && (a[i] != null) && (j < a[i].len) && (a[i][j] != null) --> condition set (1)
//
//     Note the short circuiting AND. Implication: these conditions should be performed in separate
//     blocks each of which will branch to slow path if the condition evaluates to false.
//
//     Now, imagine a situation where, in the inner loop above, in addition to "a[i][j][k] = 0" we
//     also have:
//        a[x][y][k] = 20
//     where x and y are parameters, then our conditions will have to include:
//        (x < a.len) &&
//        (y < a[x].len)
//     in addition to the above conditions (1) to get rid of bounds check on index 'k'
//
//     But these conditions can be checked together with conditions
//     (i < a.len) without a need for a separate block. In summary, the conditions will be:
//
//     (a != null) &&
//     ((i < a.len) & (x < a.len)) &&      <-- Note the bitwise AND here.
//     (a[i] != null & a[x] != null) &&    <-- Note the bitwise AND here.
//     (j < a[i].len & y < a[x].len) &&    <-- Note the bitwise AND here.
//     (a[i][j] != null & a[x][y] != null) <-- Note the bitwise AND here.
//
//     This naturally yields a tree style pattern, where the nodes of the tree are
//     the array and indices respectively.
//
//     Example:
//         a => {
//             i => {
//                 j => {
//                     k => {}
//                 }
//             },
//             x => {
//                 y => {
//                     k => {}
//                 }
//             }
//         }
//
//         Notice that the variables in the same levels can have their conditions combined in the
//         same block with a bitwise AND. Whereas, the conditions in consecutive levels will be
//         combined with a short-circuiting AND (i.e., different basic blocks).
//
//  Operation:
//      Construct a tree of array indices and the array which will generate the optimal
//      conditions for loop cloning.
//
//      a[i][j][k], b[i] and a[i][y][k] are the occurrences in the loop. Then, the tree should be:
//
//      a => {
//          i => {
//              j => {
//                  k => {}
//              },
//              y => {
//                  k => {}
//              },
//          }
//      },
//      b => {
//          i => {}
//      }
//
//      In this method, we will construct such a tree by descending depth first into the array
//      index operation and forming a tree structure as we encounter the array or the index variables.
//
//      This tree structure will then be used to generate conditions like below:
//      (a != null) & (b != null) &&       // from the first level of the tree.
//
//      (i < a.len) & (i < b.len) &&       // from the second level of the tree. Levels can be combined.
//      (a[i] != null) & (b[i] != null) && // from the second level of the tree.
//
//      (j < a[i].len) & (y < a[i].len) &&       // from the third level.
//      (a[i][j] != null) & (a[i][y] != null) && // from the third level.
//
//      and so on.
//
bool Compiler::optComputeDerefConditions(unsigned loopNum, LoopCloneContext* context)
{
    JitExpandArrayStack<LC_Deref*> nodes(getAllocator(CMK_LoopClone));
    int                            maxRank = -1;

    // Get the dereference-able arrays.
    JitExpandArrayStack<LC_Array>* deref = context->EnsureDerefs(loopNum);

    // For each array in the dereference list, construct a tree,
    // where the nodes are array and index variables and an edge 'u-v'
    // exists if a node 'v' indexes node 'u' directly as in u[v] or an edge
    // 'u-v-w' transitively if u[v][w] occurs.
    for (unsigned i = 0; i < deref->Size(); ++i)
    {
        LC_Array& array = (*deref)[i];

        // First populate the array base variable.
        LC_Deref* node = LC_Deref::Find(&nodes, array.arrIndex->arrLcl);
        if (node == nullptr)
        {
            node = new (getAllocator(CMK_LoopClone)) LC_Deref(array, 0 /*level*/);
            nodes.Push(node);
        }

        // For each dimension (level) for the array, populate the tree with the variable
        // from that dimension.
        unsigned rank = (unsigned)array.GetDimRank();
        for (unsigned i = 0; i < rank; ++i)
        {
            node->EnsureChildren(getAllocator(CMK_LoopClone));
            LC_Deref* tmp = node->Find(array.arrIndex->indLcls[i]);
            if (tmp == nullptr)
            {
                tmp = new (getAllocator(CMK_LoopClone)) LC_Deref(array, node->level + 1);
                node->children->Push(tmp);
            }

            // Descend one level down.
            node = tmp;
        }

        // Keep the maxRank of all array dereferences.
        maxRank = max((int)rank, maxRank);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Deref condition tree:\n");
        for (unsigned i = 0; i < nodes.Size(); ++i)
        {
            if (i != 0)
            {
                printf(",");
            }
            nodes[i]->Print();
            printf("\n");
        }
    }
#endif

    if (maxRank == -1)
    {
        return false;
    }

    // First level will always yield the null-check, since it is made of the array base variables.
    // All other levels (dimensions) will yield two conditions ex: (i < a.length && a[i] != null)
    // So add 1 after rank * 2.
    unsigned condBlocks = (unsigned)maxRank * 2 + 1;

    // Heuristic to not create too many blocks.
    // REVIEW: due to the definition of `condBlocks`, above, the effective max is 3 blocks, meaning
    // `maxRank` of 1. Question: should the heuristic allow more blocks to be created in some situations?
    // REVIEW: make this based on a COMPlus configuration?
    if (condBlocks > 4)
    {
        return false;
    }

    // Derive conditions into an 'array of level x array of conditions' i.e., levelCond[levels][conds]
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond =
        context->EnsureBlockConditions(loopNum, condBlocks);
    for (unsigned i = 0; i < nodes.Size(); ++i)
    {
        nodes[i]->DeriveLevelConditions(levelCond);
    }

    DBEXEC(verbose, context->PrintBlockConditions(loopNum));
    return true;
}

#ifdef DEBUG
//----------------------------------------------------------------------------
// optDebugLogLoopCloning:  Insert a call to jithelper that prints a message.
//
// Arguments:
//      block        - the block in which the helper call needs to be inserted.
//      insertBefore - the stmt before which the helper call will be inserted.
//
void Compiler::optDebugLogLoopCloning(BasicBlock* block, Statement* insertBefore)
{
    if (JitConfig.JitDebugLogLoopCloning() == 0)
    {
        return;
    }
    GenTree*   logCall = gtNewHelperCallNode(CORINFO_HELP_DEBUG_LOG_LOOP_CLONING, TYP_VOID);
    Statement* stmt    = fgNewStmtFromTree(logCall);
    fgInsertStmtBefore(block, insertBefore, stmt);
    fgMorphBlockStmt(block, stmt DEBUGARG("Debug log loop cloning"));
}
#endif // DEBUG

//------------------------------------------------------------------------
// optPerformStaticOptimizations: Perform the optimizations for the optimization
//      candidates gathered during the cloning phase.
//
// Arguments:
//     loopNum     -  the current loop index for which the optimizations are performed.
//     context     -  data structure where all loop cloning info is kept.
//     dynamicPath -  If true, the optimization is performed in the fast path among the
//                    cloned loops. If false, it means this is the only path (i.e.,
//                    there is no slow path.)
//
// Operation:
//      Perform the optimizations on the fast path i.e., the path in which the
//      optimization candidates were collected at the time of identifying them.
//      The candidates store all the information necessary (the tree/stmt/block
//      they are from) to perform the optimization.
//
// Assumption:
//      The unoptimized path is either already cloned when this method is called or
//      there is no unoptimized path (got eliminated statically.) So this method
//      performs the optimizations assuming that the path in which the candidates
//      were collected is the fast path in which the optimizations will be performed.
//
void Compiler::optPerformStaticOptimizations(unsigned loopNum, LoopCloneContext* context DEBUGARG(bool dynamicPath))
{
    JitExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);
    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* optInfo = optInfos->GetRef(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            {
                LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                compCurBB                          = arrIndexInfo->arrIndex.useBlock;
                optRemoveCommaBasedRangeCheck(arrIndexInfo->arrIndex.bndsChks[arrIndexInfo->dim], arrIndexInfo->stmt);
                DBEXEC(dynamicPath, optDebugLogLoopCloning(arrIndexInfo->arrIndex.useBlock, arrIndexInfo->stmt));
            }
            break;
            case LcOptInfo::LcMdArray:
                // TODO-CQ: CLONE: Implement.
                break;
            default:
                break;
        }
    }
}

//----------------------------------------------------------------------------
// optLoopCloningEnabled: Determine whether loop cloning is allowed. It is allowed
// in release builds. For debug builds, use the value of the COMPlus_JitCloneLoops
// flag (which defaults to 1, or allowed).
//
// Return Value:
//      true if loop cloning is allowed, false if disallowed.
//
bool Compiler::optLoopCloningEnabled()
{
#ifdef DEBUG
    return JitConfig.JitCloneLoops() != 0;
#else
    return true;
#endif
}

//----------------------------------------------------------------------------
// optIsLoopClonable: Determine whether this loop can be cloned.
//
// Arguments:
//      loopInd     loop index which needs to be checked if it can be cloned.
//
// Return Value:
//      Returns true if the loop can be cloned. If it returns false,
//      it prints a message to the JIT dump describing why the loop can't be cloned.
//
// Notes: if `true` is returned, then `fgReturnCount` is increased by the number of
// return blocks in the loop that will be cloned. (REVIEW: this 'predicate' function
// doesn't seem like the right place to do this change.)
//
bool Compiler::optIsLoopClonable(unsigned loopInd)
{
    const LoopDsc& loop = optLoopTable[loopInd];

    if (!(loop.lpFlags & LPFLG_ITER))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". No LPFLG_ITER flag.\n", loopInd);
        return false;
    }

    if (loop.lpFlags & LPFLG_REMOVED)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It is marked LPFLG_REMOVED.\n", loopInd);
        return false;
    }

    // Make sure the loop doesn't have any embedded exception handling.
    // Walk the loop blocks from lexically first to lexically last (all blocks in this region must be
    // part of the loop), looking for a `try` begin block. Note that a loop must entirely contain any
    // EH region, or be itself entirely contained within an EH region. Thus, looking just for a `try`
    // begin is sufficient; there is no need to look for other EH constructs, such as a `catch` begin.
    //
    // TODO: this limitation could be removed if we do the work to insert new EH regions in the exception table,
    // for the cloned loop (and its embedded EH regions).
    //
    // Also, count the number of return blocks within the loop for future use.
    BasicBlock* stopAt       = loop.lpBottom->bbNext;
    unsigned    loopRetCount = 0;
    for (BasicBlock* blk = loop.lpFirst; blk != stopAt; blk = blk->bbNext)
    {
        if (blk->bbJumpKind == BBJ_RETURN)
        {
            loopRetCount++;
        }
        if (bbIsTryBeg(blk))
        {
            JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It has a `try` begin.\n", loopInd);
            return false;
        }
    }

    // Is the entry block a handler or filter start?  If so, then if we cloned, we could create a jump
    // into the middle of a handler (to go to the cloned copy.)  Reject.
    if (bbIsHandlerBeg(loop.lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Entry block is a handler start.\n", loopInd);
        return false;
    }

    // If the head and entry are in different EH regions, reject.
    if (!BasicBlock::sameEHRegion(loop.lpHead, loop.lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Head and entry blocks are in different EH regions.\n",
                loopInd);
        return false;
    }

    // Is the first block after the last block of the loop a handler or filter start?
    // Usually, we create a dummy block after the orginal loop, to skip over the loop clone
    // and go to where the original loop did.  That raises problems when we don't actually go to
    // that block; this is one of those cases.  This could be fixed fairly easily; for example,
    // we could add a dummy nop block after the (cloned) loop bottom, in the same handler scope as the
    // loop.  This is just a corner to cut to get this working faster.
    BasicBlock* bbAfterLoop = loop.lpBottom->bbNext;
    if (bbAfterLoop != nullptr && bbIsHandlerBeg(bbAfterLoop))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Next block after bottom is a handler start.\n", loopInd);
        return false;
    }

    // We've previously made a decision whether to have separate return epilogs, or branch to one.
    // There's a GCInfo limitation in the x86 case, so that there can be no more than SET_EPILOGCNT_MAX separate
    // epilogs.  Other architectures have a limit of 4 here for "historical reasons", but this should be revisited
    // (or return blocks should not be considered part of the loop, rendering this issue moot).
    unsigned epilogLimit = 4;
#ifdef JIT32_GCENCODER
    epilogLimit = SET_EPILOGCNT_MAX;
#endif // JIT32_GCENCODER
    if (fgReturnCount + loopRetCount > epilogLimit)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". It has %d returns;"
                " if added to previously existing %d returns, it would exceed the limit of %d.\n",
                loopInd, loopRetCount, fgReturnCount, epilogLimit);
        return false;
    }

    unsigned ivLclNum = loop.lpIterVar();
    if (lvaVarAddrExposed(ivLclNum))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Rejected V%02u as iter var because is address-exposed.\n",
                loopInd, ivLclNum);
        return false;
    }

    BasicBlock* head = loop.lpHead;
    BasicBlock* end  = loop.lpBottom;
    BasicBlock* beg  = head->bbNext;

    if (end->bbJumpKind != BBJ_COND)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Couldn't find termination test.\n", loopInd);
        return false;
    }

    if (end->bbJumpDest != beg)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Branch at loop 'end' not looping to 'begin'.\n", loopInd);
        return false;
    }

    // TODO-CQ: CLONE: Mark increasing or decreasing loops.
    if ((loop.lpIterOper() != GT_ADD) || (loop.lpIterConst() != 1))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop iteration operator not matching.\n", loopInd);
        return false;
    }

    if ((loop.lpFlags & LPFLG_CONST_LIMIT) == 0 && (loop.lpFlags & LPFLG_VAR_LIMIT) == 0 &&
        (loop.lpFlags & LPFLG_ARRLEN_LIMIT) == 0)
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop limit is neither constant, variable or array length.\n",
                loopInd);
        return false;
    }

    if (!((GenTree::StaticOperIs(loop.lpTestOper(), GT_LT, GT_LE) && (loop.lpIterOper() == GT_ADD)) ||
          (GenTree::StaticOperIs(loop.lpTestOper(), GT_GT, GT_GE) && (loop.lpIterOper() == GT_SUB))))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP
                ". Loop test (%s) doesn't agree with the direction (%s) of the loop.\n",
                loopInd, GenTree::OpName(loop.lpTestOper()), GenTree::OpName(loop.lpIterOper()));
        return false;
    }

    if (!(loop.lpTestTree->OperKind() & GTK_RELOP) || !(loop.lpTestTree->gtFlags & GTF_RELOP_ZTT))
    {
        JITDUMP("Loop cloning: rejecting loop " FMT_LP ". Loop inversion NOT present, loop test [%06u] may not protect "
                "entry from head.\n",
                loopInd, loop.lpTestTree->gtTreeID);
        return false;
    }

#ifdef DEBUG
    GenTree* op1 = loop.lpIterator();
    assert((op1->gtOper == GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == ivLclNum));
#endif

    // Otherwise, we're going to add those return blocks.
    fgReturnCount += loopRetCount;

    return true;
}

//------------------------------------------------------------------------
// optCloneLoops: Implements loop cloning optimization.
//
// Identify loop cloning opportunities, derive loop cloning conditions,
// perform loop cloning, use the derived conditions to choose which
// path to take.
//
void Compiler::optCloneLoops()
{
    JITDUMP("\n*************** In optCloneLoops()\n");
    if (optLoopCount == 0)
    {
        JITDUMP("  No loops to clone\n");
        return;
    }
    if (!optLoopCloningEnabled())
    {
        JITDUMP("  Loop cloning disabled\n");
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBefore loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }
#endif

    unsigned optStaticallyOptimizedLoops = 0;

    LoopCloneContext context(optLoopCount, getAllocator(CMK_LoopClone));

    // Obtain array optimization candidates in the context.
    optObtainLoopCloningOpts(&context);

    // For each loop, derive cloning conditions for the optimization candidates.
    for (unsigned i = 0; i < optLoopCount; ++i)
    {
        JitExpandArrayStack<LcOptInfo*>* optInfos = context.GetLoopOptInfo(i);
        if (optInfos == nullptr)
        {
            continue;
        }

        if (!optDeriveLoopCloningConditions(i, &context) || !optComputeDerefConditions(i, &context))
        {
            JITDUMP("> Conditions could not be obtained\n");
            context.CancelLoopOptInfo(i);
        }
        else
        {
            bool allTrue  = false;
            bool anyFalse = false;
            context.EvaluateConditions(i, &allTrue, &anyFalse DEBUGARG(verbose));
            if (anyFalse)
            {
                context.CancelLoopOptInfo(i);
            }
            if (allTrue)
            {
                // Perform static optimizations on the fast path since we always
                // have to take the cloned path.
                optPerformStaticOptimizations(i, &context DEBUGARG(false));

                ++optStaticallyOptimizedLoops;

                // No need to clone.
                context.CancelLoopOptInfo(i);
            }
        }
    }

#if 0
    // The code in this #if has been useful in debugging loop cloning issues, by
    // enabling selective enablement of the loop cloning optimization according to
    // method hash.
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("loopclonehashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
        // methHashLo = (unsigned(atoi(lostr)) << 2);  // So we don't have to use negative numbers.
    }
    char* histr = getenv("loopclonehashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
        // methHashHi = (unsigned(atoi(histr)) << 2);  // So we don't have to use negative numbers.
    }
    if (methHash < methHashLo || methHash > methHashHi)
        return;
#endif
#endif

    for (unsigned i = 0; i < optLoopCount; ++i)
    {
        if (context.GetLoopOptInfo(i) != nullptr)
        {
            optLoopsCloned++;
            context.OptimizeConditions(i DEBUGARG(verbose));
            context.OptimizeBlockConditions(i DEBUGARG(verbose));
            optCloneLoop(i, &context);
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Loops cloned: %d\n", optLoopsCloned);
        printf("Loops statically optimized: %d\n", optStaticallyOptimizedLoops);
        printf("After loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }

    fgDebugCheckLoopTable();
#endif
}

//------------------------------------------------------------------------
// optCloneLoop: Perform the mechanical cloning of the specified loop
//
// Arguments:
//    loopInd - loop index of loop to clone
//    context - data structure where all loop cloning info is kept.
//
void Compiler::optCloneLoop(unsigned loopInd, LoopCloneContext* context)
{
    assert(loopInd < optLoopCount);

    LoopDsc& loop = optLoopTable[loopInd];

    JITDUMP("\nCloning loop " FMT_LP ": [head: " FMT_BB ", first: " FMT_BB ", top: " FMT_BB ", entry: " FMT_BB
            ", bottom: " FMT_BB ", child: " FMT_LP "].\n",
            loopInd, loop.lpHead->bbNum, loop.lpFirst->bbNum, loop.lpTop->bbNum, loop.lpEntry->bbNum,
            loop.lpBottom->bbNum, loop.lpChild);

    // Determine the depth of the loop, so we can properly weight blocks added (outside the cloned loop blocks).
    unsigned             depth         = optLoopDepth(loopInd);
    BasicBlock::weight_t ambientWeight = 1;
    for (unsigned j = 0; j < depth; j++)
    {
        BasicBlock::weight_t lastWeight = ambientWeight;
        ambientWeight *= BB_LOOP_WEIGHT_SCALE;
        assert(ambientWeight > lastWeight);
    }

    // If we're in a non-natural loop, the ambient weight might be higher than we computed above.
    // Be safe by taking the max with the head block's weight.
    ambientWeight = max(ambientWeight, loop.lpHead->bbWeight);

    // This is the containing loop, if any -- to label any blocks we create that are outside
    // the loop being cloned.
    unsigned char ambientLoop = loop.lpParent;

    // First, make sure that the loop has a unique header block, creating an empty one if necessary.
    optEnsureUniqueHead(loopInd, ambientWeight);

    // We're going to transform this loop:
    //
    // H --> E    (or, H conditionally branches around the loop and has fall-through to F == T == E)
    // F
    // T
    // E
    // B ?-> T
    // X
    //
    // to this pair of loops:
    //
    // H ?-> E2
    // H2--> E    (Optional; if E == T == F, let H fall through to F/T/E)
    // F
    // T
    // E
    // B  ?-> T
    // X2--> X
    // F2
    // T2
    // E2
    // B2 ?-> T2
    // X

    BasicBlock* h = loop.lpHead;
    if (h->bbJumpKind != BBJ_NONE && h->bbJumpKind != BBJ_ALWAYS)
    {
        // Make a new block to be the unique entry to the loop.
        JITDUMP("Create new unique single-successor entry to loop\n");
        assert((h->bbJumpKind == BBJ_COND) && (h->bbNext == loop.lpEntry));
        BasicBlock* newH = fgNewBBafter(BBJ_NONE, h, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", newH->bbNum, h->bbNum);
        newH->bbWeight = newH->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
        BlockSetOps::Assign(this, newH->bbReach, h->bbReach);
        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        newH->bbNatLoopNum = ambientLoop;
        optUpdateLoopHead(loopInd, h, newH);

        fgAddRefPred(newH, h); // Add h->newH pred edge
        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, newH->bbNum);
        fgReplacePred(newH->bbNext, h, newH); // Replace pred in COND fall-through block.
        JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", h->bbNum, newH->bbNext->bbNum,
                newH->bbNum, newH->bbNext->bbNum);

        h = newH;
    }
    assert(h == loop.lpHead);

    // Make X2 after B, if necessary.  (Not necessary if B is a BBJ_ALWAYS.)
    // "newPred" will be the predecessor of the blocks of the cloned loop.
    BasicBlock* b       = loop.lpBottom;
    BasicBlock* newPred = b;
    if (b->bbJumpKind != BBJ_ALWAYS)
    {
        assert(b->bbJumpKind == BBJ_COND);

        BasicBlock* x = b->bbNext;
        if (x != nullptr)
        {
            JITDUMP("Create branch around cloned loop\n");
            BasicBlock* x2 = fgNewBBafter(BBJ_ALWAYS, b, /*extendRegion*/ true);
            JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", x2->bbNum, b->bbNum);
            x2->bbWeight = x2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

            // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
            x2->bbNatLoopNum = ambientLoop;

            x2->bbJumpDest = x;
            BlockSetOps::Assign(this, x2->bbReach, h->bbReach);

            fgAddRefPred(x2, b); // Add b->x2 pred edge
            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", b->bbNum, x2->bbNum);
            fgReplacePred(x, b, x2); // The pred of x is now x2, not the fall-through of COND b.
            JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", b->bbNum, x->bbNum, x2->bbNum,
                    x->bbNum);

            newPred = x2;
        }
    }

    // Now we'll make "h2", after "h" to go to "e" -- unless the loop is a do-while,
    // so that "h" already falls through to "e" (e == t == f).
    // It might look like this code is unreachable, since "h" must be a BBJ_ALWAYS, but
    // later we will change "h" to a BBJ_COND along with a set of loop conditions.
    // TODO: it still might be unreachable, since cloning currently is restricted to "do-while" loop forms.
    BasicBlock* h2 = nullptr;
    if (h->bbNext != loop.lpEntry)
    {
        assert(h->bbJumpKind == BBJ_ALWAYS);
        JITDUMP("Create branch to entry of optimized loop\n");
        BasicBlock* h2 = fgNewBBafter(BBJ_ALWAYS, h, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", h2->bbNum, h->bbNum);
        h2->bbWeight = h2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        h2->bbNatLoopNum = ambientLoop;

        h2->bbJumpDest = loop.lpEntry;

        fgAddRefPred(h2, h); // Add h->h2 pred edge
        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, h2->bbNum);
        fgReplacePred(loop.lpEntry, h, h2);
        JITDUMP("Replace " FMT_BB " -> " FMT_BB " with " FMT_BB " -> " FMT_BB "\n", h->bbNum, loop.lpEntry->bbNum,
                h2->bbNum, loop.lpEntry->bbNum);

        optUpdateLoopHead(loopInd, h, h2);

        // NOTE: 'h' is no longer the loop head; 'h2' is!
    }

    // Now we'll clone the blocks of the loop body.
    BasicBlock* newFirst = nullptr;

    BlockToBlockMap* blockMap = new (getAllocator(CMK_LoopClone)) BlockToBlockMap(getAllocator(CMK_LoopClone));
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
    {
        BasicBlock* newBlk = fgNewBBafter(blk->bbJumpKind, newPred, /*extendRegion*/ true);
        JITDUMP("Adding " FMT_BB " (copy of " FMT_BB ") after " FMT_BB "\n", newBlk->bbNum, blk->bbNum, newPred->bbNum);

        // Call CloneBlockState to make a copy of the block's statements (and attributes), and assert that it
        // has a return value indicating success, because optCanOptimizeByLoopCloningVisitor has already
        // checked them to guarantee they are clonable.
        bool cloneOk = BasicBlock::CloneBlockState(this, newBlk, blk);
        noway_assert(cloneOk);

        // We're going to create the preds below, which will set the bbRefs properly,
        // so clear out the cloned bbRefs field.
        newBlk->bbRefs = 0;

#if FEATURE_LOOP_ALIGN
        // If the original loop is aligned, do not align the cloned loop because cloned loop will be executed in
        // rare scenario. Additionally, having to align cloned loop will force us to disable some VEX prefix encoding
        // and adding compensation for over-estimated instructions.
        if (blk->isLoopAlign())
        {
            newBlk->bbFlags &= ~BBF_LOOP_ALIGN;
            JITDUMP("Removing LOOP_ALIGN flag from cloned loop in " FMT_BB "\n", newBlk->bbNum);
        }
#endif

        // TODO-Cleanup: The above clones the bbNatLoopNum, which is incorrect.  Eventually, we should probably insert
        // the cloned loop in the loop table.  For now, however, we'll just make these blocks be part of the surrounding
        // loop, if one exists -- the parent of the loop we're cloning.
        newBlk->bbNatLoopNum = loop.lpParent;

        if (newFirst == nullptr)
        {
            newFirst = newBlk;
        }
        newPred = newBlk;
        blockMap->Set(blk, newBlk);
    }

    // Perform the static optimizations on the fast path.
    optPerformStaticOptimizations(loopInd, context DEBUGARG(true));

    // Now go through the new blocks, remapping their jump targets within the loop
    // and updating the preds lists.
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
    {
        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);

        assert(blk->bbJumpKind == newblk->bbJumpKind);

        // First copy the jump destination(s) from "blk".
        optCopyBlkDest(blk, newblk);

        // Now redirect the new block according to "blockMap".
        const bool addPreds = true;
        optRedirectBlock(newblk, blockMap, addPreds);

        blk->ensurePredListOrder(this);
    }

#ifdef DEBUG
    // Display the preds for the new blocks, after all the new blocks have been redirected.
    JITDUMP("Preds after loop copy:\n");
    for (BasicBlock* blk = loop.lpFirst; blk != loop.lpBottom->bbNext; blk = blk->bbNext)
    {
        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);
        JITDUMP(FMT_BB ":", newblk->bbNum);
        for (flowList* pred = newblk->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            JITDUMP(" " FMT_BB, pred->getBlock()->bbNum);
        }
        JITDUMP("\n");
    }
#endif // DEBUG

    // We will create the following structure
    //
    // cond0 (in h)  -?> cond1
    // slow          --> e2 (slow) always
    // !cond1        -?> slow
    // !cond2        -?> slow
    // ...
    // !condn        -?> slow
    // h2/entry (fast)
    //
    // We should always have block conditions, at the minimum, the array should be deref-able
    assert(context->HasBlockConditions(loopInd));

    if (h->bbJumpKind == BBJ_NONE)
    {
        assert((h->bbNext == h2) || (h->bbNext == loop.lpEntry));
    }
    else
    {
        assert(h->bbJumpKind == BBJ_ALWAYS);
        assert(h->bbJumpDest == loop.lpEntry);
    }

    // If all the conditions are true, go to E2.
    BasicBlock* e2      = nullptr;
    bool        foundIt = blockMap->Lookup(loop.lpEntry, &e2);

    // We're going to replace the fall-through path from "h".
    if (h->bbJumpKind == BBJ_NONE)
    {
        fgRemoveRefPred(h->bbNext, h);
    }

    // Create a unique header for the slow path.
    JITDUMP("Create unique head block for slow path loop\n");
    BasicBlock* slowHead = fgNewBBafter(BBJ_ALWAYS, h, /*extendRegion*/ true);
    JITDUMP("Adding " FMT_BB " after " FMT_BB "\n", slowHead->bbNum, h->bbNum);
    slowHead->bbWeight     = h->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;
    slowHead->bbNatLoopNum = ambientLoop;
    slowHead->bbJumpDest   = e2;

    fgAddRefPred(slowHead, h);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", h->bbNum, slowHead->bbNum);

    // This is the only predecessor to the copied loop, and it hasn't been added yet.
    fgAddRefPred(slowHead->bbJumpDest, slowHead);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", slowHead->bbNum, slowHead->bbJumpDest->bbNum);

    // "h" is now going to be a COND block
    h->bbJumpKind = BBJ_COND;

    BasicBlock* condLast = optInsertLoopChoiceConditions(context, loopInd, h, slowHead);
    condLast->bbJumpDest = slowHead;

    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", condLast->bbNum, condLast->bbJumpDest->bbNum);
    fgAddRefPred(condLast->bbJumpDest, condLast);

    // Add the fall-through path pred.
    assert(condLast->bbJumpKind == BBJ_COND);
    JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", condLast->bbNum, condLast->bbNext->bbNum);
    fgAddRefPred(condLast->bbNext, condLast);

    // If h2 is present it is already the head or replace 'h' by 'condLast'.
    if (h2 == nullptr)
    {
        optUpdateLoopHead(loopInd, loop.lpHead, condLast);
    }
    assert(foundIt && e2 != nullptr);

    // Don't unroll loops that we've cloned -- the unroller expects any loop it should unroll to
    // initialize the loop counter immediately before entering the loop, but we've left a shared
    // initialization of the loop counter up above the test that determines which version of the
    // loop to take.
    loop.lpFlags |= LPFLG_DONT_UNROLL;

    constexpr bool computePreds = false;
    fgUpdateChangedFlowGraph(computePreds);
}

//--------------------------------------------------------------------------------------------------
// optInsertLoopChoiceConditions - Insert the loop conditions for a loop between loop head and entry
//
// Arguments:
//      context     loop cloning context variable
//      loopNum     the loop index
//      head        loop head for "loopNum"
//      slowHead    the slow path loop head
//
// Return Value:
//      The last conditional block inserted.
//
// Operation:
//      Create the following structure.
//
//      Note below that the cond0 is inverted in head, i.e., if true jump to cond1. This is because
//      condn cannot jtrue to loop head h2. It has to be from a direct pred block.
//
//      cond0 (in h)  -?> cond1
//      slowHead      --> e2 (slowHead) always
//      !cond1        -?> slowHead
//      !cond2        -?> slowHead
//      ...
//      !condn        -?> slowHead
//      h2/entry (fast)
//
//      Insert condition 0 in 'h' and create other condition blocks and insert conditions in them.
//      On entry, block 'h' is a conditional block, but its bbJumpDest hasn't yet been set.
//
BasicBlock* Compiler::optInsertLoopChoiceConditions(LoopCloneContext* context,
                                                    unsigned          loopNum,
                                                    BasicBlock*       head,
                                                    BasicBlock*       slowHead)
{
    JITDUMP("Inserting loop cloning conditions\n");
    assert(context->HasBlockConditions(loopNum));

    BasicBlock*                                              curCond   = head;
    JitExpandArrayStack<JitExpandArrayStack<LC_Condition>*>* levelCond = context->GetBlockConditions(loopNum);
    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        bool isHeaderBlock = (curCond == head);

        // Flip the condition if header block.
        context->CondToStmtInBlock(this, *((*levelCond)[i]), curCond, /*reverse*/ isHeaderBlock);

        // Create each condition block ensuring wiring between them.
        BasicBlock* tmp     = fgNewBBafter(BBJ_COND, isHeaderBlock ? slowHead : curCond, /*extendRegion*/ true);
        curCond->bbJumpDest = isHeaderBlock ? tmp : slowHead;

        JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", curCond->bbNum, curCond->bbJumpDest->bbNum);
        fgAddRefPred(curCond->bbJumpDest, curCond);

        if (!isHeaderBlock)
        {
            JITDUMP("Adding " FMT_BB " -> " FMT_BB "\n", curCond->bbNum, tmp->bbNum);
            fgAddRefPred(tmp, curCond);
        }

        curCond = tmp;

        curCond->inheritWeight(head);
        curCond->bbNatLoopNum = head->bbNatLoopNum;
        JITDUMP("Created new " FMT_BB " for new level %u\n", curCond->bbNum, i);
    }

    // Finally insert cloning conditions after all deref conditions have been inserted.
    context->CondToStmtInBlock(this, *(context->GetConditions(loopNum)), curCond, /*reverse*/ false);
    return curCond;
}

//------------------------------------------------------------------------
// OptEnsureUniqueHead: Ensure that loop "loopInd" has a unique head block.
// If the existing entry has non-loop predecessors other than the head entry,
// create a new, empty block that goes (only) to the entry, and redirects the
// preds of the entry to this new block. Sets the weight of the newly created
// block to "ambientWeight".
//
// Arguments:
//    loopInd       - index of loop to process
//    ambientWeight - weight to give the new head, if created.
//
void Compiler::optEnsureUniqueHead(unsigned loopInd, BasicBlock::weight_t ambientWeight)
{
    LoopDsc& loop = optLoopTable[loopInd];

    BasicBlock* h = loop.lpHead;
    BasicBlock* t = loop.lpTop;
    BasicBlock* e = loop.lpEntry;
    BasicBlock* b = loop.lpBottom;

    // If "h" dominates the entry block, then it is the unique header.
    if (fgDominate(h, e))
    {
        return;
    }

    // Otherwise, create a new empty header block, make it the pred of the entry block,
    // and redirect the preds of the entry block to go to this.

    BasicBlock* beforeTop = t->bbPrev;
    // Make sure that the new block is in the same region as the loop.
    // (We will only create loops that are entirely within a region.)
    BasicBlock* h2 = fgNewBBafter(BBJ_ALWAYS, beforeTop, true);
    // This is in the containing loop.
    h2->bbNatLoopNum = loop.lpParent;
    h2->bbWeight     = h2->isRunRarely() ? BB_ZERO_WEIGHT : ambientWeight;

    // We don't care where it was put; splice it between beforeTop and top.
    if (beforeTop->bbNext != h2)
    {
        h2->bbPrev->setNext(h2->bbNext); // Splice h2 out.
        beforeTop->setNext(h2);          // Splice h2 in, between beforeTop and t.
        h2->setNext(t);
    }

    if (h2->bbNext != e)
    {
        h2->bbJumpKind = BBJ_ALWAYS;
        h2->bbJumpDest = e;
    }
    BlockSetOps::Assign(this, h2->bbReach, e->bbReach);

    // Redirect paths from preds of "e" to go to "h2" instead of "e".
    BlockToBlockMap* blockMap = new (getAllocator(CMK_LoopClone)) BlockToBlockMap(getAllocator(CMK_LoopClone));
    blockMap->Set(e, h2);

    for (flowList* predEntry = e->bbPreds; predEntry; predEntry = predEntry->flNext)
    {
        BasicBlock* predBlock = predEntry->getBlock();

        // Skip if predBlock is in the loop.
        if (t->bbNum <= predBlock->bbNum && predBlock->bbNum <= b->bbNum)
        {
            continue;
        }
        optRedirectBlock(predBlock, blockMap);
    }

    optUpdateLoopHead(loopInd, loop.lpHead, h2);
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
                    tree->ChangeOperConst(GT_CNS_INT);
                    tree->gtType                = TYP_INT;
                    tree->AsIntCon()->gtIconVal = (int)lval;
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

            case GT_CLS_VAR:
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
                    tree->gtType = genSignedType(dstt);
                    tree->SetVNs(vnpNarrow);

                    /* Make sure we don't mess up the variable type */
                    if ((oper == GT_LCL_VAR) || (oper == GT_LCL_FLD))
                    {
                        tree->gtFlags |= GTF_VAR_CAST;
                    }
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
                        dstt = genSignedType(dstt);

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

/*****************************************************************************
 *
 *  The following logic figures out whether the given variable is assigned
 *  somewhere in a list of basic blocks (or in an entire loop).
 */

Compiler::fgWalkResult Compiler::optIsVarAssgCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->OperIs(GT_ASG))
    {
        GenTree*   dest     = tree->AsOp()->gtOp1;
        genTreeOps destOper = dest->OperGet();

        isVarAssgDsc* desc = (isVarAssgDsc*)data->pCallbackData;
        assert(desc && desc->ivaSelf == desc);

        if (destOper == GT_LCL_VAR)
        {
            unsigned tvar = dest->AsLclVarCommon()->GetLclNum();
            if (tvar < lclMAX_ALLSET_TRACKED)
            {
                AllVarSetOps::AddElemD(data->compiler, desc->ivaMaskVal, tvar);
            }
            else
            {
                desc->ivaMaskIncomplete = true;
            }

            if (tvar == desc->ivaVar)
            {
                if (tree != desc->ivaSkip)
                {
                    return WALK_ABORT;
                }
            }
        }
        else if (destOper == GT_LCL_FLD)
        {
            /* We can't track every field of every var. Moreover, indirections
               may access different parts of the var as different (but
               overlapping) fields. So just treat them as indirect accesses */

            // unsigned    lclNum = dest->AsLclFld()->GetLclNum();
            // noway_assert(lvaTable[lclNum].lvAddrTaken);

            varRefKinds refs = varTypeIsGC(tree->TypeGet()) ? VR_IND_REF : VR_IND_SCL;
            desc->ivaMaskInd = varRefKinds(desc->ivaMaskInd | refs);
        }
        else if (destOper == GT_CLS_VAR)
        {
            desc->ivaMaskInd = varRefKinds(desc->ivaMaskInd | VR_GLB_VAR);
        }
        else if (destOper == GT_IND)
        {
            /* Set the proper indirection bits */

            varRefKinds refs = varTypeIsGC(tree->TypeGet()) ? VR_IND_REF : VR_IND_SCL;
            desc->ivaMaskInd = varRefKinds(desc->ivaMaskInd | refs);
        }
    }
    else if (tree->gtOper == GT_CALL)
    {
        isVarAssgDsc* desc = (isVarAssgDsc*)data->pCallbackData;
        assert(desc && desc->ivaSelf == desc);

        desc->ivaMaskCall = optCallInterf(tree->AsCall());
    }

    return WALK_CONTINUE;
}

/*****************************************************************************/

bool Compiler::optIsVarAssigned(BasicBlock* beg, BasicBlock* end, GenTree* skip, unsigned var)
{
    bool         result;
    isVarAssgDsc desc;

    desc.ivaSkip = skip;
#ifdef DEBUG
    desc.ivaSelf = &desc;
#endif
    desc.ivaVar      = var;
    desc.ivaMaskCall = CALLINT_NONE;
    AllVarSetOps::AssignNoCopy(this, desc.ivaMaskVal, AllVarSetOps::MakeEmpty(this));

    for (;;)
    {
        noway_assert(beg != nullptr);

        for (Statement* stmt : beg->Statements())
        {
            if (fgWalkTreePre(stmt->GetRootNodePointer(), optIsVarAssgCB, &desc) != WALK_CONTINUE)
            {
                result = true;
                goto DONE;
            }
        }

        if (beg == end)
        {
            break;
        }

        beg = beg->bbNext;
    }

    result = false;

DONE:

    return result;
}

/*****************************************************************************/
int Compiler::optIsSetAssgLoop(unsigned lnum, ALLVARSET_VALARG_TP vars, varRefKinds inds)
{
    LoopDsc* loop;

    /* Get hold of the loop descriptor */

    noway_assert(lnum < optLoopCount);
    loop = optLoopTable + lnum;

    /* Do we already know what variables are assigned within this loop? */

    if (!(loop->lpFlags & LPFLG_ASGVARS_YES))
    {
        isVarAssgDsc desc;

        BasicBlock* beg;
        BasicBlock* end;

        /* Prepare the descriptor used by the tree walker call-back */

        desc.ivaVar  = (unsigned)-1;
        desc.ivaSkip = nullptr;
#ifdef DEBUG
        desc.ivaSelf = &desc;
#endif
        AllVarSetOps::AssignNoCopy(this, desc.ivaMaskVal, AllVarSetOps::MakeEmpty(this));
        desc.ivaMaskInd        = VR_NONE;
        desc.ivaMaskCall       = CALLINT_NONE;
        desc.ivaMaskIncomplete = false;

        /* Now walk all the statements of the loop */

        beg = loop->lpHead->bbNext;
        end = loop->lpBottom;

        for (/**/; /**/; beg = beg->bbNext)
        {
            noway_assert(beg);

            for (Statement* stmt : StatementList(beg->FirstNonPhiDef()))
            {
                fgWalkTreePre(stmt->GetRootNodePointer(), optIsVarAssgCB, &desc);

                if (desc.ivaMaskIncomplete)
                {
                    loop->lpFlags |= LPFLG_ASGVARS_INC;
                }
            }

            if (beg == end)
            {
                break;
            }
        }

        AllVarSetOps::Assign(this, loop->lpAsgVars, desc.ivaMaskVal);
        loop->lpAsgInds = desc.ivaMaskInd;
        loop->lpAsgCall = desc.ivaMaskCall;

        /* Now we know what variables are assigned in the loop */

        loop->lpFlags |= LPFLG_ASGVARS_YES;
    }

    /* Now we can finally test the caller's mask against the loop's */
    if (!AllVarSetOps::IsEmptyIntersection(this, loop->lpAsgVars, vars) || (loop->lpAsgInds & inds))
    {
        return 1;
    }

    switch (loop->lpAsgCall)
    {
        case CALLINT_ALL:

            /* Can't hoist if the call might have side effect on an indirection. */

            if (loop->lpAsgInds != VR_NONE)
            {
                return 1;
            }

            break;

        case CALLINT_REF_INDIRS:

            /* Can't hoist if the call might have side effect on an ref indirection. */

            if (loop->lpAsgInds & VR_IND_REF)
            {
                return 1;
            }

            break;

        case CALLINT_SCL_INDIRS:

            /* Can't hoist if the call might have side effect on an non-ref indirection. */

            if (loop->lpAsgInds & VR_IND_SCL)
            {
                return 1;
            }

            break;

        case CALLINT_ALL_INDIRS:

            /* Can't hoist if the call might have side effect on any indirection. */

            if (loop->lpAsgInds & (VR_IND_REF | VR_IND_SCL))
            {
                return 1;
            }

            break;

        case CALLINT_NONE:

            /* Other helpers kill nothing */

            break;

        default:
            noway_assert(!"Unexpected lpAsgCall value");
    }

    return 0;
}

void Compiler::optPerformHoistExpr(GenTree* origExpr, unsigned lnum)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nHoisting a copy of ");
        printTreeID(origExpr);
        printf(" into PreHeader for loop " FMT_LP " <" FMT_BB ".." FMT_BB ">:\n", lnum,
               optLoopTable[lnum].lpFirst->bbNum, optLoopTable[lnum].lpBottom->bbNum);
        gtDispTree(origExpr);
        printf("\n");
    }
#endif

    // This loop has to be in a form that is approved for hoisting.
    assert(optLoopTable[lnum].lpFlags & LPFLG_HOISTABLE);

    // Create a copy of the expression and mark it for CSE's.
    GenTree* hoistExpr = gtCloneExpr(origExpr, GTF_MAKE_CSE);

    // The hoist Expr does not have to computed into a specific register,
    // so clear the RegNum if it was set in the original expression
    hoistExpr->ClearRegNum();

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

    fgCreateLoopPreHeader(lnum);

    BasicBlock* preHead = optLoopTable[lnum].lpHead;
    assert(preHead->bbJumpKind == BBJ_NONE);

    // fgMorphTree requires that compCurBB be the block that contains
    // (or in this case, will contain) the expression.
    compCurBB = preHead;
    hoist     = fgMorphTree(hoist);

    Statement* hoistStmt = gtNewStmt(hoist);
    hoistStmt->SetCompilerAdded();

    /* simply append the statement at the end of the preHead's list */

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
    }
#endif

    if (fgStmtListThreaded)
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

void Compiler::optHoistLoopCode()
{
    // If we don't have any loops in the method then take an early out now.
    if (optLoopCount == 0)
    {
        return;
    }

#ifdef DEBUG
    unsigned jitNoHoist = JitConfig.JitNoHoist();
    if (jitNoHoist > 0)
    {
        return;
    }
#endif

#if 0
    // The code in this #if has been useful in debugging loop cloning issues, by
    // enabling selective enablement of the loop cloning optimization according to
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
        return;
    printf("Doing loop hoisting in %s (0x%x).\n", info.compFullName, methHash);
#endif // DEBUG
#endif // 0     -- debugging loop cloning issues

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In optHoistLoopCode()\n");
        printf("Blocks/Trees before phase\n");
        fgDispBasicBlocks(true);
        printf("");
    }
#endif

    // Consider all the loop nests, in outer-to-inner order (thus hoisting expressions outside the largest loop in which
    // they are invariant.)
    LoopHoistContext hoistCtxt(this);
    for (unsigned lnum = 0; lnum < optLoopCount; lnum++)
    {
        if (optLoopTable[lnum].lpFlags & LPFLG_REMOVED)
        {
            continue;
        }

        if (optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP)
        {
            optHoistLoopNest(lnum, &hoistCtxt);
        }
    }

#if DEBUG
    if (fgModified)
    {
        if (verbose)
        {
            printf("Blocks/Trees after optHoistLoopCode() modified flowgraph\n");
            fgDispBasicBlocks(true);
            printf("");
        }

        // Make sure that the predecessor lists are accurate
        fgDebugCheckBBlist();
    }
#endif

#ifdef DEBUG
    // Test Data stuff..
    // If we have no test data, early out.
    if (m_nodeTestData == nullptr)
    {
        return;
    }
    NodeToTestDataMap* testData = GetNodeTestData();
    for (NodeToTestDataMap::KeyIterator ki = testData->Begin(); !ki.Equal(testData->End()); ++ki)
    {
        TestLabelAndNum tlAndN;
        GenTree*        node = ki.Get();
        bool            b    = testData->Lookup(node, &tlAndN);
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
#endif // DEBUG
}

void Compiler::optHoistLoopNest(unsigned lnum, LoopHoistContext* hoistCtxt)
{
    // Do this loop, then recursively do all nested loops.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if LOOP_HOIST_STATS
    // Record stats
    m_curLoopHasHoistedExpression = false;
    m_loopsConsidered++;
#endif // LOOP_HOIST_STATS

    optHoistThisLoop(lnum, hoistCtxt);

    VNSet* hoistedInCurLoop = hoistCtxt->ExtractHoistedInCurLoop();

    if (optLoopTable[lnum].lpChild != BasicBlock::NOT_IN_LOOP)
    {
        // Add the ones hoisted in "lnum" to "hoistedInParents" for any nested loops.
        // TODO-Cleanup: we should have a set abstraction for loops.
        if (hoistedInCurLoop != nullptr)
        {
            for (VNSet::KeyIterator keys = hoistedInCurLoop->Begin(); !keys.Equal(hoistedInCurLoop->End()); ++keys)
            {
#ifdef DEBUG
                bool b;
                assert(!hoistCtxt->m_hoistedInParentLoops.Lookup(keys.Get(), &b));
#endif
                hoistCtxt->m_hoistedInParentLoops.Set(keys.Get(), true);
            }
        }

        for (unsigned child = optLoopTable[lnum].lpChild; child != BasicBlock::NOT_IN_LOOP;
             child          = optLoopTable[child].lpSibling)
        {
            optHoistLoopNest(child, hoistCtxt);
        }

        // Now remove them.
        // TODO-Cleanup: we should have a set abstraction for loops.
        if (hoistedInCurLoop != nullptr)
        {
            for (VNSet::KeyIterator keys = hoistedInCurLoop->Begin(); !keys.Equal(hoistedInCurLoop->End()); ++keys)
            {
                // Note that we asserted when we added these that they hadn't been members, so removing is appropriate.
                hoistCtxt->m_hoistedInParentLoops.Remove(keys.Get());
            }
        }
    }
}

void Compiler::optHoistThisLoop(unsigned lnum, LoopHoistContext* hoistCtxt)
{
    LoopDsc* pLoopDsc = &optLoopTable[lnum];

    /* If loop was removed continue */

    if (pLoopDsc->lpFlags & LPFLG_REMOVED)
    {
        return;
    }

    /* Get the head and tail of the loop */

    BasicBlock* head = pLoopDsc->lpHead;
    BasicBlock* tail = pLoopDsc->lpBottom;
    BasicBlock* lbeg = pLoopDsc->lpEntry;

    // We must have a do-while loop
    if ((pLoopDsc->lpFlags & LPFLG_DO_WHILE) == 0)
    {
        return;
    }

    // The loop-head must dominate the loop-entry.
    // TODO-CQ: Couldn't we make this true if it's not?
    if (!fgDominate(head, lbeg))
    {
        return;
    }

    // if lbeg is the start of a new try block then we won't be able to hoist
    if (!BasicBlock::sameTryRegion(head, lbeg))
    {
        return;
    }

    // We don't bother hoisting when inside of a catch block
    if ((lbeg->bbCatchTyp != BBCT_NONE) && (lbeg->bbCatchTyp != BBCT_FINALLY))
    {
        return;
    }

    pLoopDsc->lpFlags |= LPFLG_HOISTABLE;

    unsigned begn = lbeg->bbNum;
    unsigned endn = tail->bbNum;

    // Ensure the per-loop sets/tables are empty.
    hoistCtxt->m_curLoopVnInvariantCache.RemoveAll();

#ifdef DEBUG
    if (verbose)
    {
        printf("optHoistLoopCode for loop " FMT_LP " <" FMT_BB ".." FMT_BB ">:\n", lnum, begn, endn);
        printf("  Loop body %s a call\n", pLoopDsc->lpContainsCall ? "contains" : "does not contain");
        printf("  Loop has %s\n", (pLoopDsc->lpFlags & LPFLG_ONE_EXIT) ? "single exit" : "multiple exits");
    }
#endif

    VARSET_TP loopVars(VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, pLoopDsc->lpVarUseDef));

    pLoopDsc->lpVarInOutCount    = VarSetOps::Count(this, pLoopDsc->lpVarInOut);
    pLoopDsc->lpLoopVarCount     = VarSetOps::Count(this, loopVars);
    pLoopDsc->lpHoistedExprCount = 0;

#ifndef TARGET_64BIT
    unsigned longVarsCount = VarSetOps::Count(this, lvaLongVars);

    if (longVarsCount > 0)
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
            lvaDispVarSet(lvaLongVars);
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
        lvaDispVarSet(pLoopDsc->lpVarUseDef);

        printf("\n  INOUT   (%d)=", pLoopDsc->lpVarInOutCount);
        lvaDispVarSet(pLoopDsc->lpVarInOut);

        printf("\n  LOOPVARS(%d)=", pLoopDsc->lpLoopVarCount);
        lvaDispVarSet(loopVars);
        printf("\n");
    }
#endif

    unsigned floatVarsCount = VarSetOps::Count(this, lvaFloatVars);

    if (floatVarsCount > 0)
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
            lvaDispVarSet(inOutFPVars);

            printf("\n  LOOPV-FP(%d)=", pLoopDsc->lpLoopVarFPCount);
            lvaDispVarSet(loopFPVars);
        }
#endif
    }
    else // (floatVarsCount == 0)
    {
        pLoopDsc->lpLoopVarFPCount     = 0;
        pLoopDsc->lpVarInOutFPCount    = 0;
        pLoopDsc->lpHoistedFPExprCount = 0;
    }

    // Find the set of definitely-executed blocks.
    // Ideally, the definitely-executed blocks are the ones that post-dominate the entry block.
    // Until we have post-dominators, we'll special-case for single-exit blocks.
    ArrayStack<BasicBlock*> defExec(getAllocatorLoopHoist());
    if (pLoopDsc->lpFlags & LPFLG_ONE_EXIT)
    {
        assert(pLoopDsc->lpExit != nullptr);
        BasicBlock* cur = pLoopDsc->lpExit;
        // Push dominators, until we reach "entry" or exit the loop.
        while (cur != nullptr && pLoopDsc->lpContains(cur) && cur != pLoopDsc->lpEntry)
        {
            defExec.Push(cur);
            cur = cur->bbIDom;
        }
        // If we didn't reach the entry block, give up and *just* push the entry block.
        if (cur != pLoopDsc->lpEntry)
        {
            defExec.Reset();
        }
        defExec.Push(pLoopDsc->lpEntry);
    }
    else // More than one exit
    {
        // We'll assume that only the entry block is definitely executed.
        // We could in the future do better.
        defExec.Push(pLoopDsc->lpEntry);
    }

    optHoistLoopBlocks(lnum, &defExec, hoistCtxt);
}

bool Compiler::optIsProfitableToHoistableTree(GenTree* tree, unsigned lnum)
{
    LoopDsc* pLoopDsc = &optLoopTable[lnum];

    bool loopContainsCall = pLoopDsc->lpContainsCall;

    int availRegCount;
    int hoistedExprCount;
    int loopVarCount;
    int varInOutCount;

    if (varTypeIsFloating(tree->TypeGet()))
    {
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
    else
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
            return false;
        }
    }

    return true;
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

            Value(GenTree* node) : m_node(node), m_hoistable(false), m_cctorDependent(false), m_invariant(false)
            {
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

        bool IsNodeHoistable(GenTree* node)
        {
            // TODO-CQ: This is a more restrictive version of a check that optIsCSEcandidate already does - it allows
            // a struct typed node if a class handle can be recovered from it.
            if (node->TypeGet() == TYP_STRUCT)
            {
                return false;
            }

            // Tree must be a suitable CSE candidate for us to be able to hoist it.
            return m_compiler->optIsCSEcandidate(node);
        }

        bool IsTreeVNInvariant(GenTree* tree)
        {
            ValueNum vn = tree->gtVNPair.GetLiberal();
            if (m_compiler->vnStore->IsVNConstant(vn))
            {
                // It is unsafe to allow a GT_CLS_VAR that has been assigned a constant.
                // The logic in optVNIsLoopInvariant would consider it to be loop-invariant, even
                // if the assignment of the constant to the GT_CLS_VAR was inside the loop.
                //
                if (tree->OperIs(GT_CLS_VAR))
                {
                    return false;
                }
            }

            return m_compiler->optVNIsLoopInvariant(vn, m_loopNum, &m_hoistContext->m_curLoopVnInvariantCache);
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
        {
        }

        void HoistBlock(BasicBlock* block)
        {
            for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
            {
                WalkTree(stmt->GetRootNodePointer(), nullptr);
                assert(m_valueStack.TopRef().Node() == stmt->GetRootNode());

                if (m_valueStack.TopRef().m_hoistable)
                {
                    m_compiler->optHoistCandidate(stmt->GetRootNode(), m_loopNum, m_hoistContext);
                }

                m_valueStack.Reset();
            }

            // Only uncondtionally executed blocks in the loop are visited (see optHoistThisLoop)
            // so after we're done visiting the first block we need to assume the worst, that the
            // blocks that are not visisted have side effects.
            m_beforeSideEffect = false;
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            m_valueStack.Emplace(node);
            return fgWalkResult::WALK_CONTINUE;
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;

            if (tree->OperIsLocal())
            {
                GenTreeLclVarCommon* lclVar = tree->AsLclVarCommon();
                unsigned             lclNum = lclVar->GetLclNum();

                // To be invariant a LclVar node must not be the LHS of an assignment ...
                bool isInvariant = !user->OperIs(GT_ASG) || (user->AsOp()->gtGetOp1() != tree);
                // and the variable must be in SSA ...
                isInvariant = isInvariant && m_compiler->lvaInSsa(lclNum);
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

                if (isInvariant)
                {
                    Value& top = m_valueStack.TopRef();
                    assert(top.Node() == tree);
                    top.m_invariant = true;
                    // In general it doesn't make sense to hoist a local node but there are exceptions, for example
                    // LCL_FLD nodes (because then the variable cannot be enregistered and the node always turns
                    // into a memory access).
                    top.m_hoistable = IsNodeHoistable(tree);
                }

                return fgWalkResult::WALK_CONTINUE;
            }

            // Initclass CLS_VARs and IconHandles are the base cases of cctor dependent trees.
            // In the IconHandle case, it's of course the dereference, rather than the constant itself, that is
            // truly dependent on the cctor.  So a more precise approach would be to separately propagate
            // isCctorDependent and isAddressWhoseDereferenceWouldBeCctorDependent, but we don't for
            // simplicity/throughput; the constant itself would be considered non-hoistable anyway, since
            // optIsCSEcandidate returns false for constants.
            bool treeIsCctorDependent = ((tree->OperIs(GT_CLS_VAR) && ((tree->gtFlags & GTF_CLS_VAR_INITCLASS) != 0)) ||
                                         (tree->OperIs(GT_CNS_INT) && ((tree->gtFlags & GTF_ICON_INITCLASS) != 0)));
            bool treeIsInvariant          = true;
            bool treeHasHoistableChildren = false;
            int  childCount;

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

            // But we must see if anything else prevents "tree" from being hoisted.
            //
            if (treeIsInvariant)
            {
                if (treeIsHoistable)
                {
                    treeIsHoistable = IsNodeHoistable(tree);
                }

                // If it's a call, it must be a helper call, and be pure.
                // Further, if it may run a cctor, it must be labeled as "Hoistable"
                // (meaning it won't run a cctor because the class is not precise-init).
                if (treeIsHoistable && tree->IsCall())
                {
                    GenTreeCall* call = tree->AsCall();
                    if (call->gtCallType != CT_HELPER)
                    {
                        treeIsHoistable = false;
                    }
                    else
                    {
                        CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);
                        if (!s_helperCallProperties.IsPure(helpFunc))
                        {
                            treeIsHoistable = false;
                        }
                        else if (s_helperCallProperties.MayRunCctor(helpFunc) &&
                                 ((call->gtFlags & GTF_CALL_HOISTABLE) == 0))
                        {
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
                else if (tree->OperIs(GT_ASG))
                {
                    // If the LHS of the assignment has a global reference, then assume it's a global side effect.
                    GenTree* lhs = tree->AsOp()->gtOp1;
                    if (lhs->gtFlags & GTF_GLOB_REF)
                    {
                        m_beforeSideEffect = false;
                    }
                }
                else if (tree->OperIs(GT_XADD, GT_XORR, GT_XAND, GT_XCHG, GT_LOCKADD, GT_CMPXCHG, GT_MEMORYBARRIER))
                {
                    // If this node is a MEMORYBARRIER or an Atomic operation
                    // then don't hoist and stop any further hoisting after this node
                    treeIsHoistable    = false;
                    m_beforeSideEffect = false;
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
                for (int i = 0; i < m_valueStack.Height(); i++)
                {
                    Value& value = m_valueStack.BottomRef(i);

                    if (value.m_hoistable)
                    {
                        assert(value.Node() != tree);

                        // Don't hoist this tree again.
                        value.m_hoistable = false;
                        value.m_invariant = false;

                        m_compiler->optHoistCandidate(value.Node(), m_loopNum, m_hoistContext);
                    }
                }
            }

            m_valueStack.Pop(childCount);

            Value& top = m_valueStack.TopRef();
            assert(top.Node() == tree);
            top.m_hoistable      = treeIsHoistable;
            top.m_cctorDependent = treeIsCctorDependent;
            top.m_invariant      = treeIsInvariant;

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    LoopDsc* loopDsc = &optLoopTable[loopNum];
    assert(blocks->Top() == loopDsc->lpEntry);

    HoistVisitor visitor(this, loopNum, hoistContext);

    while (!blocks->Empty())
    {
        BasicBlock*          block       = blocks->Pop();
        BasicBlock::weight_t blockWeight = block->getBBWeight(this);

        JITDUMP("    optHoistLoopBlocks " FMT_BB " (weight=%6s) of loop " FMT_LP " <" FMT_BB ".." FMT_BB
                ">, firstBlock is %s\n",
                block->bbNum, refCntWtd2str(blockWeight), loopNum, loopDsc->lpFirst->bbNum, loopDsc->lpBottom->bbNum,
                dspBool(block == loopDsc->lpEntry));

        if (blockWeight < (BB_UNITY_WEIGHT / 10))
        {
            JITDUMP("      block weight is too small to perform hoisting.\n");
            continue;
        }

        visitor.HoistBlock(block);
    }
}

void Compiler::optHoistCandidate(GenTree* tree, unsigned lnum, LoopHoistContext* hoistCtxt)
{
    assert(lnum != BasicBlock::NOT_IN_LOOP);
    assert((optLoopTable[lnum].lpFlags & LPFLG_HOISTABLE) != 0);

    // It must pass the hoistable profitablity tests for this loop level
    if (!optIsProfitableToHoistableTree(tree, lnum))
    {
        return;
    }

    bool b;
    if (hoistCtxt->m_hoistedInParentLoops.Lookup(tree->gtVNPair.GetLiberal(), &b))
    {
        // already hoisted in a parent loop, so don't hoist this expression.
        return;
    }

    if (hoistCtxt->GetHoistedInCurLoop(this)->Lookup(tree->gtVNPair.GetLiberal(), &b))
    {
        // already hoisted this expression in the current loop, so don't hoist this expression.
        return;
    }

    // Expression can be hoisted
    optPerformHoistExpr(tree, lnum);

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

bool Compiler::optVNIsLoopInvariant(ValueNum vn, unsigned lnum, VNToBoolMap* loopVnInvariantCache)
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
        else
        {
            for (unsigned i = 0; i < funcApp.m_arity; i++)
            {
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
    else
    {
        // Non-function "new, unique" VN's may be annotated with the loop nest where
        // their definition occurs.
        BasicBlock::loopNumber vnLoopNum = vnStore->LoopOfVN(vn);

        if (vnLoopNum == MAX_LOOP_NUM)
        {
            res = false;
        }
        else
        {
            res = !optLoopContains(lnum, vnLoopNum);
        }
    }

    loopVnInvariantCache->Set(vn, res);
    return res;
}

/*****************************************************************************
 *
 *  Creates a pre-header block for the given loop - a preheader is a BBJ_NONE
 *  header. The pre-header will replace the current lpHead in the loop table.
 *  The loop has to be a do-while loop. Thus, all blocks dominated by lpHead
 *  will also be dominated by the loop-top, lpHead->bbNext.
 *
 */

void Compiler::fgCreateLoopPreHeader(unsigned lnum)
{
    LoopDsc* pLoopDsc = &optLoopTable[lnum];

    /* This loop has to be a "do-while" loop */

    assert(pLoopDsc->lpFlags & LPFLG_DO_WHILE);

    /* Have we already created a loop-preheader block? */

    if (pLoopDsc->lpFlags & LPFLG_HAS_PREHEAD)
    {
        return;
    }

    BasicBlock* head  = pLoopDsc->lpHead;
    BasicBlock* top   = pLoopDsc->lpTop;
    BasicBlock* entry = pLoopDsc->lpEntry;

    // if 'entry' and 'head' are in different try regions then we won't be able to hoist
    if (!BasicBlock::sameTryRegion(head, entry))
    {
        return;
    }

    // Ensure that lpHead always dominates lpEntry

    noway_assert(fgDominate(head, entry));

    /* Get hold of the first block of the loop body */

    assert(top == entry);

    /* Allocate a new basic block */

    BasicBlock* preHead = bbNewBasicBlock(BBJ_NONE);
    preHead->bbFlags |= BBF_INTERNAL | BBF_LOOP_PREHEADER;

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
        printf("\nCreated PreHeader (" FMT_BB ") for loop " FMT_LP " (" FMT_BB " - " FMT_BB "), with weight = %s\n",
               preHead->bbNum, lnum, top->bbNum, pLoopDsc->lpBottom->bbNum, refCntWtd2str(preHead->getBBWeight(this)));
    }
#endif

    // The preheader block is part of the containing loop (if any).
    preHead->bbNatLoopNum = pLoopDsc->lpParent;

    if (fgIsUsingProfileWeights() && (head->bbJumpKind == BBJ_COND))
    {
        if ((head->bbWeight == BB_ZERO_WEIGHT) || (head->bbNext->bbWeight == BB_ZERO_WEIGHT))
        {
            preHead->bbWeight = BB_ZERO_WEIGHT;
            preHead->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            bool allValidProfileWeights =
                (head->hasProfileWeight() && head->bbJumpDest->hasProfileWeight() && head->bbNext->hasProfileWeight());

            if (allValidProfileWeights)
            {
                BasicBlock::weight_t loopEnteredCount;
                BasicBlock::weight_t loopSkippedCount;

                if (fgHaveValidEdgeWeights)
                {
                    flowList* edgeToNext = fgGetPredForBlock(head->bbNext, head);
                    flowList* edgeToJump = fgGetPredForBlock(head->bbJumpDest, head);
                    noway_assert(edgeToNext != nullptr);
                    noway_assert(edgeToJump != nullptr);

                    loopEnteredCount = (edgeToNext->edgeWeightMin() + edgeToNext->edgeWeightMax()) / 2.0f;
                    loopSkippedCount = (edgeToJump->edgeWeightMin() + edgeToJump->edgeWeightMax()) / 2.0f;
                }
                else
                {
                    loopEnteredCount = head->bbNext->bbWeight;
                    loopSkippedCount = head->bbJumpDest->bbWeight;
                }

                JITDUMP("%s; loopEnterCount " FMT_WT " loopSkipCount " FMT_WT "\n",
                        fgHaveValidEdgeWeights ? "valid edge weights" : "no edge weights", loopEnteredCount,
                        loopSkippedCount);

                BasicBlock::weight_t loopTakenRatio = loopEnteredCount / (loopEnteredCount + loopSkippedCount);

                JITDUMP("%s; loopEnterCount " FMT_WT " loopSkipCount " FMT_WT " taken ratio " FMT_WT "\n",
                        fgHaveValidEdgeWeights ? "valid edge weights" : "no edge weights", loopEnteredCount,
                        loopSkippedCount, loopTakenRatio);

                // Calculate a good approximation of the preHead's block weight
                BasicBlock::weight_t preHeadWeight = (head->bbWeight * loopTakenRatio);
                preHead->setBBProfileWeight(preHeadWeight);
                noway_assert(!preHead->isRunRarely());
            }
        }
    }

    // Link in the preHead block
    fgInsertBBbefore(top, preHead);

    // Ideally we would re-run SSA and VN if we optimized by doing loop hoisting.
    // However, that is too expensive at this point. Instead, we update the phi
    // node block references, if we created pre-header block due to hoisting.
    // This is sufficient because any definition participating in SSA that flowed
    // into the phi via the loop header block will now flow through the preheader
    // block from the header block.

    for (Statement* stmt : top->Statements())
    {
        GenTree* tree = stmt->GetRootNode();
        if (tree->OperGet() != GT_ASG)
        {
            break;
        }
        GenTree* op2 = tree->gtGetOp2();
        if (op2->OperGet() != GT_PHI)
        {
            break;
        }
        for (GenTreePhi::Use& use : op2->AsPhi()->Uses())
        {
            GenTreePhiArg* phiArg = use.GetNode()->AsPhiArg();
            if (phiArg->gtPredBB == head)
            {
                phiArg->gtPredBB = preHead;
            }
        }
    }

    // The handler can't begin at the top of the loop.  If it did, it would be incorrect
    // to set the handler index on the pre header without updating the exception table.
    noway_assert(!top->hasHndIndex() || fgFirstBlockOfHandler(top) != top);

    // Update the EH table to make the hoisted block part of the loop's EH block.
    fgExtendEHRegionBefore(top);

    // TODO-CQ: set dominators for this block, to allow loop optimizations requiring them
    //        (e.g: hoisting expression in a loop with the same 'head' as this one)

    /* Update the loop entry */

    pLoopDsc->lpHead = preHead;
    pLoopDsc->lpFlags |= LPFLG_HAS_PREHEAD;

    /* The new block becomes the 'head' of the loop - update bbRefs and bbPreds
       All predecessors of 'beg', (which is the entry in the loop)
       now have to jump to 'preHead', unless they are dominated by 'head' */

    preHead->bbRefs                 = 0;
    flowList* const edgeToPreHeader = fgAddRefPred(preHead, head);
    edgeToPreHeader->setEdgeWeights(preHead->bbWeight, preHead->bbWeight, preHead);
    bool checkNestedLoops = false;

    for (flowList* pred = top->bbPreds; pred; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->getBlock();

        if (fgDominate(top, predBlock))
        {
            // note: if 'top' dominates predBlock, 'head' dominates predBlock too
            // (we know that 'head' dominates 'top'), but using 'top' instead of
            // 'head' in the test allows us to not enter here if 'predBlock == head'

            if (predBlock != pLoopDsc->lpBottom)
            {
                noway_assert(predBlock != head);
                checkNestedLoops = true;
            }
            continue;
        }

        switch (predBlock->bbJumpKind)
        {
            case BBJ_NONE:
                noway_assert(predBlock == head);
                break;

            case BBJ_COND:
                if (predBlock == head)
                {
                    noway_assert(predBlock->bbJumpDest != top);
                    break;
                }
                FALLTHROUGH;

            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                noway_assert(predBlock->bbJumpDest == top);
                predBlock->bbJumpDest = preHead;

                if (predBlock == head)
                {
                    // This is essentially the same case of predBlock being a BBJ_NONE. We may not be
                    // able to make this a BBJ_NONE if it's an internal block (for example, a leave).
                    // Just break, pred will be removed after switch.
                }
                else
                {
                    fgRemoveRefPred(top, predBlock);
                    fgAddRefPred(preHead, predBlock);
                }
                break;

            case BBJ_SWITCH:
                unsigned jumpCnt;
                jumpCnt = predBlock->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = predBlock->bbJumpSwt->bbsDstTab;

                do
                {
                    assert(*jumpTab);
                    if ((*jumpTab) == top)
                    {
                        (*jumpTab) = preHead;

                        fgRemoveRefPred(top, predBlock);
                        fgAddRefPred(preHead, predBlock);
                    }
                } while (++jumpTab, --jumpCnt);
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    noway_assert(!fgGetPredForBlock(top, preHead));
    fgRemoveRefPred(top, head);
    flowList* const edgeFromPreHeader = fgAddRefPred(top, preHead);
    edgeFromPreHeader->setEdgeWeights(preHead->bbWeight, preHead->bbWeight, top);

    /*
        If we found at least one back-edge in the flowgraph pointing to the top/entry of the loop
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
                noway_assert(l != lnum); // pLoopDsc->lpHead was already changed from 'head' to 'preHead'
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
}

bool Compiler::optBlockIsLoopEntry(BasicBlock* blk, unsigned* pLnum)
{
    for (unsigned lnum = blk->bbNatLoopNum; lnum != BasicBlock::NOT_IN_LOOP; lnum = optLoopTable[lnum].lpParent)
    {
        if (optLoopTable[lnum].lpFlags & LPFLG_REMOVED)
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
        optLoopTable[lnum].lpContainsCall = false;
    }

    for (lnum = 0; lnum < optLoopCount; lnum++)
    {
        if (optLoopTable[lnum].lpFlags & LPFLG_REMOVED)
        {
            continue;
        }

        if (optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP)
        { // Is outermost...
            optComputeLoopNestSideEffects(lnum);
        }
    }

    VarSetOps::AssignNoCopy(this, lvaFloatVars, VarSetOps::MakeEmpty(this));
#ifndef TARGET_64BIT
    VarSetOps::AssignNoCopy(this, lvaLongVars, VarSetOps::MakeEmpty(this));
#endif

    for (unsigned i = 0; i < lvaCount; i++)
    {
        LclVarDsc* varDsc = &lvaTable[i];
        if (varDsc->lvTracked)
        {
            if (varTypeIsFloating(varDsc->lvType))
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
    assert(optLoopTable[lnum].lpParent == BasicBlock::NOT_IN_LOOP); // Requires: lnum is outermost.
    BasicBlock* botNext = optLoopTable[lnum].lpBottom->bbNext;
    JITDUMP("optComputeLoopSideEffects botNext is " FMT_BB ", lnum is %d\n", botNext->bbNum, lnum);
    for (BasicBlock* bbInLoop = optLoopTable[lnum].lpFirst; bbInLoop != botNext; bbInLoop = bbInLoop->bbNext)
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
    for (Statement* stmt : StatementList(blk->FirstNonPhiDef()))
    {
        for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
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

                // If we just set lpContainsCall or it was previously set
                if (optLoopTable[mostNestedLoop].lpContainsCall)
                {
                    // We can early exit after both memoryHavoc and lpContainsCall are both set to true.
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
                GenTree* lhs = tree->AsOp()->gtOp1->gtEffectiveVal(/*commaOnly*/ true);

                if (lhs->OperGet() == GT_IND)
                {
                    GenTree*      arg           = lhs->AsOp()->gtOp1->gtEffectiveVal(/*commaOnly*/ true);
                    FieldSeqNode* fldSeqArrElem = nullptr;

                    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
                    {
                        memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        continue;
                    }

                    ArrayInfo arrInfo;

                    if (arg->TypeGet() == TYP_BYREF && arg->OperGet() == GT_LCL_VAR)
                    {
                        // If it's a local byref for which we recorded a value number, use that...
                        GenTreeLclVar* argLcl = arg->AsLclVar();
                        if (lvaInSsa(argLcl->GetLclNum()))
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
                    // Is the LHS an array index expression?
                    else if (lhs->ParseArrayElemForm(this, &arrInfo, &fldSeqArrElem))
                    {
                        // We actually ignore "fldSeq" -- any modification to an S[], at any
                        // field of "S", will lose all information about the array type.
                        CORINFO_CLASS_HANDLE elemTypeEq = EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                        AddModifiedElemTypeAllContainingLoops(mostNestedLoop, elemTypeEq);
                        // Conservatively assume byrefs may alias this array element
                        memoryHavoc |= memoryKindSet(ByrefExposed);
                    }
                    else
                    {
                        // We are only interested in IsFieldAddr()'s fldSeq out parameter.
                        //
                        GenTree*      obj          = nullptr; // unused
                        GenTree*      staticOffset = nullptr; // unused
                        FieldSeqNode* fldSeq       = nullptr;

                        if (arg->IsFieldAddr(this, &obj, &staticOffset, &fldSeq) &&
                            (fldSeq != FieldSeqStore::NotAField()))
                        {
                            // Get the first (object) field from field seq.  GcHeap[field] will yield the "field map".
                            assert(fldSeq != nullptr);
                            if (fldSeq->IsFirstElemFieldSeq())
                            {
                                fldSeq = fldSeq->m_next;
                                assert(fldSeq != nullptr);
                            }

                            AddModifiedFieldAllContainingLoops(mostNestedLoop, fldSeq->m_fieldHnd);
                            // Conservatively assume byrefs may alias this object.
                            memoryHavoc |= memoryKindSet(ByrefExposed);
                        }
                        else
                        {
                            memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                        }
                    }
                }
                else if (lhs->OperIsBlk())
                {
                    GenTreeLclVarCommon* lclVarTree;
                    bool                 isEntire;
                    if (!tree->DefinesLocal(this, &lclVarTree, &isEntire))
                    {
                        // For now, assume arbitrary side effects on GcHeap/ByrefExposed...
                        memoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                    }
                    else if (lvaVarAddrExposed(lclVarTree->GetLclNum()))
                    {
                        memoryHavoc |= memoryKindSet(ByrefExposed);
                    }
                }
                else if (lhs->OperGet() == GT_CLS_VAR)
                {
                    AddModifiedFieldAllContainingLoops(mostNestedLoop, lhs->AsClsVar()->gtClsVarHnd);
                    // Conservatively assume byrefs may alias this static field
                    memoryHavoc |= memoryKindSet(ByrefExposed);
                }
                // Otherwise, must be local lhs form.  I should assert that.
                else if (lhs->OperGet() == GT_LCL_VAR)
                {
                    GenTreeLclVar* lhsLcl = lhs->AsLclVar();
                    GenTree*       rhs    = tree->AsOp()->gtOp2;
                    ValueNum       rhsVN  = rhs->gtVNPair.GetLiberal();
                    // If we gave the RHS a value number, propagate it.
                    if (rhsVN != ValueNumStore::NoVN)
                    {
                        rhsVN = vnStore->VNNormalValue(rhsVN);
                        if (lvaInSsa(lhsLcl->GetLclNum()))
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

                    case GT_ADDR:
                        // Is it an addr of a array index expression?
                        {
                            GenTree* addrArg = tree->AsOp()->gtOp1;
                            if (addrArg->OperGet() == GT_IND)
                            {
                                // Is the LHS an array index expression?
                                if (addrArg->gtFlags & GTF_IND_ARR_INDEX)
                                {
                                    ArrayInfo arrInfo;
                                    bool      b = GetArrayInfoMap()->Lookup(addrArg, &arrInfo);
                                    assert(b);
                                    CORINFO_CLASS_HANDLE elemTypeEq =
                                        EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                                    ValueNum elemTypeEqVN =
                                        vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
                                    ValueNum ptrToArrElemVN =
                                        vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem, elemTypeEqVN,
                                                           // The rest are dummy arguments.
                                                           vnStore->VNForNull(), vnStore->VNForNull(),
                                                           vnStore->VNForNull());
                                    tree->gtVNPair.SetBoth(ptrToArrElemVN);
                                }
                            }
                        }
                        break;

                    case GT_LOCKADD:
                    case GT_XORR:
                    case GT_XAND:
                    case GT_XADD:
                    case GT_XCHG:
                    case GT_CMPXCHG:
                    case GT_MEMORYBARRIER:
                    {
                        assert(!tree->OperIs(GT_LOCKADD) && "LOCKADD should not appear before lowering");
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
                        break;
                }
            }
        }
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
        BasicBlock* first = optLoopTable[lnum].lpFirst;
        first->bbFlags &= ~BBF_LOOP_ALIGN;
        JITDUMP("Removing LOOP_ALIGN flag for " FMT_LP " that starts at " FMT_BB " because loop has a call.\n", lnum,
                first->bbNum);
    }
#endif

    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].lpContainsCall = true;
        lnum                              = optLoopTable[lnum].lpParent;
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
void Compiler::AddModifiedFieldAllContainingLoops(unsigned lnum, CORINFO_FIELD_HANDLE fldHnd)
{
    assert(0 <= lnum && lnum < optLoopCount);
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        optLoopTable[lnum].AddModifiedField(this, fldHnd);
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
// Assumptions:
//    This method is capable of removing checks of two kinds: COMMA-based and standalone top-level ones.
//    In case of a COMMA-based check, "check" must be a non-null first operand of a non-null COMMA.
//    In case of a standalone check, "comma" must be null and "check" - "stmt"'s root.
//
GenTree* Compiler::optRemoveRangeCheck(GenTreeBoundsChk* check, GenTree* comma, Statement* stmt)
{
#if !REARRANGE_ADDS
    noway_assert(!"can't remove range checks without REARRANGE_ADDS right now");
#endif

    noway_assert(stmt != nullptr);
    noway_assert((comma != nullptr && comma->OperIs(GT_COMMA) && comma->gtGetOp1() == check) ||
                 (check != nullptr && check->OperIsBoundsCheck() && comma == nullptr));
    noway_assert(check->OperIsBoundsCheck());

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

    // Recalculate the GetCostSz(), etc...
    gtSetStmtInfo(stmt);

    // Re-thread the nodes if necessary
    if (fgStmtListThreaded)
    {
        fgSetStmtSeq(stmt);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("After optRemoveRangeCheck:\n");
        gtDispTree(tree);
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
    assert(comma->gtGetOp1()->OperIsBoundsCheck());

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

//------------------------------------------------------------------------------
// optObtainLoopCloningOpts: Identify optimization candidates and update
//      the "context" for array optimizations.
//
// Arguments:
//     context     -  data structure where all loop cloning info is kept. The
//                    optInfo fields of the context are updated with the
//                    identified optimization candidates.
//
void Compiler::optObtainLoopCloningOpts(LoopCloneContext* context)
{
    for (unsigned i = 0; i < optLoopCount; i++)
    {
        JITDUMP("Considering loop " FMT_LP " to clone for optimizations.\n", i);
        if (optIsLoopClonable(i))
        {
            optIdentifyLoopOptInfo(i, context);
        }
        JITDUMP("------------------------------------------------------------\n");
    }
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// optIdentifyLoopOptInfo: Identify loop optimization candidates.
// Also, check if the loop is suitable for the optimizations performed.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning candidates will be updated.
//
// Return Value:
//     If the loop is not suitable for the optimizations, return false - context
//     should not contain any optimization candidate for the loop if false.
//     Else return true.
//
// Operation:
//      Check if the loop is well formed for this optimization and identify the
//      optimization candidates and update the "context" parameter with all the
//      contextual information necessary to perform the optimization later.
//
bool Compiler::optIdentifyLoopOptInfo(unsigned loopNum, LoopCloneContext* context)
{
    noway_assert(loopNum < optLoopCount);

    LoopDsc* pLoop = &optLoopTable[loopNum];

    BasicBlock* head = pLoop->lpHead;
    BasicBlock* beg  = head->bbNext; // should this be pLoop->lpFirst or pLoop->lpTop instead?
    BasicBlock* end  = pLoop->lpBottom;

    JITDUMP("Checking blocks " FMT_BB ".." FMT_BB " for optimization candidates\n", beg->bbNum, end->bbNum);

    LoopCloneVisitorInfo info(context, loopNum, nullptr);
    for (BasicBlock* block = beg; block != end->bbNext; block = block->bbNext)
    {
        compCurBB = block;
        for (Statement* stmt : block->Statements())
        {
            info.stmt               = stmt;
            const bool lclVarsOnly  = false;
            const bool computeStack = false;
            fgWalkTreePre(stmt->GetRootNodePointer(), optCanOptimizeByLoopCloningVisitor, &info, lclVarsOnly,
                          computeStack);
        }
    }

    return true;
}

//---------------------------------------------------------------------------------------------------------------
//  optExtractArrIndex: Try to extract the array index from "tree".
//
//  Arguments:
//      tree        the tree to be checked if it is the array [] operation.
//      result      the extracted GT_INDEX information is updated in result.
//      lhsNum      for the root level (function is recursive) callers should pass BAD_VAR_NUM.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. See assumption about
//      what will be extracted. The "result" variable's rank parameter is advanced for every
//      dimension of [] encountered.
//
//  Operation:
//      Given a "tree" extract the GT_INDEX node in "result" as ArrIndex. In FlowGraph morph
//      we have converted a GT_INDEX tree into a scaled index base offset expression. We need
//      to reconstruct this to be able to know if this is an array access.
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
//  TODO-CQ: CLONE: After morph make sure this method extracts values before morph.
//
//  Example tree to pattern match:
//
// *  COMMA     int
// +--*  ARR_BOUNDS_CHECK_Rng void
// |  +--*  LCL_VAR   int    V02 loc1
// |  \--*  ARR_LENGTH int
// |     \--*  LCL_VAR   ref    V00 arg0
// \--*  IND       int
//    \--*  ADD       byref
//       +--*  LCL_VAR   ref    V00 arg0
//       \--*  ADD       long
//          +--*  LSH       long
//          |  +--*  CAST      long <- int
//          |  |  \--*  LCL_VAR   int    V02 loc1
//          |  \--*  CNS_INT   long   2
//          \--*  CNS_INT   long   16 Fseq[#FirstElem]
//
// Note that byte arrays don't require the LSH to scale the index, so look like this:
//
// *  COMMA     ubyte
// +--*  ARR_BOUNDS_CHECK_Rng void
// |  +--*  LCL_VAR   int    V03 loc2
// |  \--*  ARR_LENGTH int
// |     \--*  LCL_VAR   ref    V00 arg0
// \--*  IND       ubyte
//    \--*  ADD       byref
//       +--*  LCL_VAR   ref    V00 arg0
//       \--*  ADD       long
//          +--*  CAST      long <- int
//          |  \--*  LCL_VAR   int    V03 loc2
//          \--*  CNS_INT   long   16 Fseq[#FirstElem]
//
bool Compiler::optExtractArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum)
{
    if (tree->gtOper != GT_COMMA)
    {
        return false;
    }
    GenTree* before = tree->gtGetOp1();
    if (before->gtOper != GT_ARR_BOUNDS_CHECK)
    {
        return false;
    }
    GenTreeBoundsChk* arrBndsChk = before->AsBoundsChk();
    if (arrBndsChk->gtIndex->gtOper != GT_LCL_VAR)
    {
        return false;
    }

    // For span we may see gtArrLen is a local var or local field or constant.
    // We won't try and extract those.
    if (arrBndsChk->gtArrLen->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_CNS_INT))
    {
        return false;
    }
    if (arrBndsChk->gtArrLen->gtGetOp1()->gtOper != GT_LCL_VAR)
    {
        return false;
    }
    unsigned arrLcl = arrBndsChk->gtArrLen->gtGetOp1()->AsLclVarCommon()->GetLclNum();
    if (lhsNum != BAD_VAR_NUM && arrLcl != lhsNum)
    {
        return false;
    }

    unsigned indLcl = arrBndsChk->gtIndex->AsLclVarCommon()->GetLclNum();

    GenTree* after = tree->gtGetOp2();

    if (after->gtOper != GT_IND)
    {
        return false;
    }
    // It used to be the case that arrBndsChks for struct types would fail the previous check because
    // after->gtOper was an address (for a block op).  In order to avoid asmDiffs we will for now
    // return false if the type of 'after' is a struct type.  (This was causing us to clone loops
    // that we were not previously cloning.)
    // TODO-1stClassStructs: Remove this check to enable optimization of array bounds checks for struct
    // types.
    if (varTypeIsStruct(after))
    {
        return false;
    }

    GenTree* sibo = after->gtGetOp1(); // sibo = scale*index + base + offset
    if (sibo->gtOper != GT_ADD)
    {
        return false;
    }
    GenTree* base = sibo->gtGetOp1();
    GenTree* sio  = sibo->gtGetOp2(); // sio == scale*index + offset
    if (base->OperGet() != GT_LCL_VAR || base->AsLclVarCommon()->GetLclNum() != arrLcl)
    {
        return false;
    }
    if (sio->gtOper != GT_ADD)
    {
        return false;
    }
    GenTree* ofs = sio->gtGetOp2();
    GenTree* si  = sio->gtGetOp1(); // si = scale*index
    if (ofs->gtOper != GT_CNS_INT)
    {
        return false;
    }
    GenTree* index;
    if (si->gtOper == GT_LSH)
    {
        GenTree* scale = si->gtGetOp2();
        index          = si->gtGetOp1();
        if (scale->gtOper != GT_CNS_INT)
        {
            return false;
        }
    }
    else
    {
        // No scale (e.g., byte array).
        index = si;
    }
#ifdef TARGET_64BIT
    if (index->gtOper != GT_CAST)
    {
        return false;
    }
    GenTree* indexVar = index->gtGetOp1();
#else
    GenTree* indexVar = index;
#endif
    if (indexVar->gtOper != GT_LCL_VAR || indexVar->AsLclVarCommon()->GetLclNum() != indLcl)
    {
        return false;
    }
    if (lhsNum == BAD_VAR_NUM)
    {
        result->arrLcl = arrLcl;
    }
    result->indLcls.Push(indLcl);
    result->bndsChks.Push(tree);
    result->useBlock = compCurBB;
    result->rank++;

    return true;
}

//---------------------------------------------------------------------------------------------------------------
//  optReconstructArrIndex: Reconstruct array index.
//
//  Arguments:
//      tree        the tree to be checked if it is an array [][][] operation.
//      result      OUT: the extracted GT_INDEX information.
//      lhsNum      for the root level (function is recursive) callers should pass BAD_VAR_NUM.
//
//  Return Value:
//      Returns true if array index can be extracted, else, return false. "rank" field in
//      "result" contains the array access depth. The "indLcls" fields contain the indices.
//
//  Operation:
//      Recursively look for a list of array indices. In the example below, we encounter,
//      V03 = ((V05 = V00[V01]), (V05[V02])) which corresponds to access of V00[V01][V02]
//      The return value would then be:
//      ArrIndex result { arrLcl: V00, indLcls: [V01, V02], rank: 2 }
//
//      V00[V01][V02] would be morphed as:
//
//      [000000001B366848] ---XG-------                        indir     int
//      [000000001B36BC50] ------------                                 V05 + (V02 << 2) + 16
//      [000000001B36C200] ---XG-------                     comma     int
//      [000000001B36BDB8] ---X--------                        arrBndsChk(V05, V02)
//      [000000001B36C278] -A-XG-------                  comma     int
//      [000000001B366730] R--XG-------                           indir     ref
//      [000000001B36C2F0] ------------                             V00 + (V01 << 3) + 24
//      [000000001B36C818] ---XG-------                        comma     ref
//      [000000001B36C458] ---X--------                           arrBndsChk(V00, V01)
//      [000000001B36BB60] -A-XG-------                     =         ref
//      [000000001B36BAE8] D------N----                        lclVar    ref    V05 tmp2
//      [000000001B36A668] -A-XG-------               =         int
//      [000000001B36A5F0] D------N----                  lclVar    int    V03 tmp0
//
//  Assumption:
//      The method extracts only if the array base and indices are GT_LCL_VAR.
//
bool Compiler::optReconstructArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum)
{
    // If we can extract "tree" (which is a top level comma) return.
    if (optExtractArrIndex(tree, result, lhsNum))
    {
        return true;
    }
    // We have a comma (check if array base expr is computed in "before"), descend further.
    else if (tree->OperGet() == GT_COMMA)
    {
        GenTree* before = tree->gtGetOp1();
        // "before" should evaluate an array base for the "after" indexing.
        if (before->OperGet() != GT_ASG)
        {
            return false;
        }
        GenTree* lhs = before->gtGetOp1();
        GenTree* rhs = before->gtGetOp2();

        // "rhs" should contain an GT_INDEX
        if (!lhs->IsLocal() || !optReconstructArrIndex(rhs, result, lhsNum))
        {
            return false;
        }
        unsigned lhsNum = lhs->AsLclVarCommon()->GetLclNum();
        GenTree* after  = tree->gtGetOp2();
        // Pass the "lhsNum", so we can verify if indeed it is used as the array base.
        return optExtractArrIndex(after, result, lhsNum);
    }
    return false;
}

/* static */
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloningVisitor(GenTree** pTree, Compiler::fgWalkData* data)
{
    return data->compiler->optCanOptimizeByLoopCloning(*pTree, (LoopCloneVisitorInfo*)data->pCallbackData);
}

//-------------------------------------------------------------------------
//  optIsStackLocalInvariant: Is stack local invariant in loop.
//
//  Arguments:
//      loopNum      The loop in which the variable is tested for invariance.
//      lclNum       The local that is tested for invariance in the loop.
//
//  Return Value:
//      Returns true if the variable is loop invariant in loopNum.
//
bool Compiler::optIsStackLocalInvariant(unsigned loopNum, unsigned lclNum)
{
    if (lvaVarAddrExposed(lclNum))
    {
        return false;
    }
    if (optIsVarAssgLoop(loopNum, lclNum))
    {
        return false;
    }
    return true;
}

//----------------------------------------------------------------------------------------------
//  optCanOptimizeByLoopCloning: Check if the tree can be optimized by loop cloning and if so,
//      identify as potential candidate and update the loop context.
//
//  Arguments:
//      tree         The tree encountered during the tree walk.
//      info         Supplies information about the current block or stmt in which the tree is.
//                   Also supplies the "context" pointer for updating with loop cloning
//                   candidates. Also supplies loopNum.
//
//  Operation:
//      If array index can be reconstructed, check if the iteration var of the loop matches the
//      array index var in some dimension. Also ensure other index vars before the identified
//      dimension are loop invariant.
//
//  Return Value:
//      Skip sub trees if the optimization candidate is identified or else continue walking
//
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloning(GenTree* tree, LoopCloneVisitorInfo* info)
{
    ArrIndex arrIndex(getAllocator(CMK_LoopClone));

    // Check if array index can be optimized.
    if (optReconstructArrIndex(tree, &arrIndex, BAD_VAR_NUM))
    {
        assert(tree->gtOper == GT_COMMA);

#ifdef DEBUG
        if (verbose)
        {
            printf("Found ArrIndex at tree ");
            printTreeID(tree);
            printf(" which is equivalent to: ");
            arrIndex.Print();
            printf("\n");
        }
#endif

        // Check that the array object local variable is invariant within the loop body.
        if (!optIsStackLocalInvariant(info->loopNum, arrIndex.arrLcl))
        {
            return WALK_SKIP_SUBTREES;
        }

        // Walk the dimensions and see if iterVar of the loop is used as index.
        for (unsigned dim = 0; dim < arrIndex.rank; ++dim)
        {
            // Is index variable also used as the loop iter var?
            if (arrIndex.indLcls[dim] == optLoopTable[info->loopNum].lpIterVar())
            {
                // Check the previous indices are all loop invariant.
                for (unsigned dim2 = 0; dim2 < dim; ++dim2)
                {
                    if (optIsVarAssgLoop(info->loopNum, arrIndex.indLcls[dim2]))
                    {
                        JITDUMP("V%02d is assigned in loop\n", arrIndex.indLcls[dim2]);
                        return WALK_SKIP_SUBTREES;
                    }
                }
#ifdef DEBUG
                if (verbose)
                {
                    printf("Loop " FMT_LP " can be cloned for ArrIndex ", info->loopNum);
                    arrIndex.Print();
                    printf(" on dim %d\n", dim);
                }
#endif
                // Update the loop context.
                info->context->EnsureLoopOptInfo(info->loopNum)
                    ->Push(new (this, CMK_LoopOpt) LcJaggedArrayOptInfo(arrIndex, dim, info->stmt));
            }
            else
            {
                JITDUMP("Induction V%02d is not used as index on dim %d\n", optLoopTable[info->loopNum].lpIterVar(),
                        dim);
            }
        }
        return WALK_SKIP_SUBTREES;
    }
    else if (tree->gtOper == GT_ARR_ELEM)
    {
        // TODO-CQ: CLONE: Implement.
        return WALK_SKIP_SUBTREES;
    }
    return WALK_CONTINUE;
}

struct optRangeCheckDsc
{
    Compiler* pCompiler;
    bool      bValidIndex;
};
/*
    Walk to make sure that only locals and constants are contained in the index
    for a range check
*/
Compiler::fgWalkResult Compiler::optValidRangeCheckIndex(GenTree** pTree, fgWalkData* data)
{
    GenTree*          tree  = *pTree;
    optRangeCheckDsc* pData = (optRangeCheckDsc*)data->pCallbackData;

    if (tree->gtOper == GT_IND || tree->gtOper == GT_CLS_VAR || tree->gtOper == GT_FIELD || tree->gtOper == GT_LCL_FLD)
    {
        pData->bValidIndex = false;
        return WALK_ABORT;
    }

    if (tree->gtOper == GT_LCL_VAR)
    {
        if (pData->pCompiler->lvaTable[tree->AsLclVarCommon()->GetLclNum()].lvAddrExposed)
        {
            pData->bValidIndex = false;
            return WALK_ABORT;
        }
    }

    return WALK_CONTINUE;
}

/*
    returns true if a range check can legally be removed (for the moment it checks
    that the array is a local array (non subject to racing conditions) and that the
    index is either a constant or a local
*/
bool Compiler::optIsRangeCheckRemovable(GenTree* tree)
{
    noway_assert(tree->gtOper == GT_ARR_BOUNDS_CHECK);
    GenTreeBoundsChk* bndsChk = tree->AsBoundsChk();
    GenTree*          pArray  = bndsChk->GetArray();
    if (pArray == nullptr && !bndsChk->gtArrLen->IsCnsIntOrI())
    {
        return false;
    }
    GenTree* pIndex = bndsChk->gtIndex;

    // The length must be a constant (the pArray == NULL case) or the array reference must be a local.
    // Otherwise we can be targeted by malicious race-conditions.
    if (pArray != nullptr)
    {
        if (pArray->gtOper != GT_LCL_VAR)
        {

#ifdef DEBUG
            if (verbose)
            {
                printf("Can't remove range check if the array isn't referenced with a local\n");
                gtDispTree(pArray);
            }
#endif
            return false;
        }
        else
        {
            noway_assert(pArray->gtType == TYP_REF);
            noway_assert(pArray->AsLclVarCommon()->GetLclNum() < lvaCount);

            if (lvaTable[pArray->AsLclVarCommon()->GetLclNum()].lvAddrExposed)
            {
                // If the array address has been taken, don't do the optimization
                // (this restriction can be lowered a bit, but i don't think it's worth it)
                CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
                if (verbose)
                {
                    printf("Can't remove range check if the array has its address taken\n");
                    gtDispTree(pArray);
                }
#endif
                return false;
            }
        }
    }

    optRangeCheckDsc Data;
    Data.pCompiler   = this;
    Data.bValidIndex = true;

    fgWalkTreePre(&pIndex, optValidRangeCheckIndex, &Data);

    if (!Data.bValidIndex)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Can't remove range check with this index");
            gtDispTree(pIndex);
        }
#endif

        return false;
    }

    return true;
}

/******************************************************************************
 *
 * Replace x==null with (x|x)==0 if x is a GC-type.
 * This will stress code-gen and the emitter to make sure they support such trees.
 */

#ifdef DEBUG

void Compiler::optOptimizeBoolsGcStress(BasicBlock* condBlock)
{
    if (!compStressCompile(STRESS_OPT_BOOLS_GC, 20))
    {
        return;
    }

    noway_assert(condBlock->bbJumpKind == BBJ_COND);
    GenTree* cond = condBlock->lastStmt()->GetRootNode();

    noway_assert(cond->gtOper == GT_JTRUE);

    bool     isBool;
    GenTree* relop;

    GenTree* comparand = optIsBoolCond(cond, &relop, &isBool);

    if (comparand == nullptr || !varTypeIsGC(comparand->TypeGet()))
    {
        return;
    }

    if (comparand->gtFlags & (GTF_ASG | GTF_CALL | GTF_ORDER_SIDEEFF))
    {
        return;
    }

    GenTree* comparandClone = gtCloneExpr(comparand);

    noway_assert(relop->AsOp()->gtOp1 == comparand);
    genTreeOps oper      = compStressCompile(STRESS_OPT_BOOLS_GC, 50) ? GT_OR : GT_AND;
    relop->AsOp()->gtOp1 = gtNewOperNode(oper, TYP_I_IMPL, comparand, comparandClone);

    // Comparand type is already checked, and we have const int, there is no harm
    // morphing it into a TYP_I_IMPL.
    noway_assert(relop->AsOp()->gtOp2->gtOper == GT_CNS_INT);
    relop->AsOp()->gtOp2->gtType = TYP_I_IMPL;
}

#endif

/******************************************************************************
 * Function used by folding of boolean conditionals
 * Given a GT_JTRUE node, checks that it is a boolean comparison of the form
 *    "if (boolVal ==/!=  0/1)". This is translated into a GT_EQ node with "op1"
 *    being a boolean lclVar and "op2" the const 0/1.
 * On success, the comparand (ie. boolVal) is returned.   Else NULL.
 * compPtr returns the compare node (i.e. GT_EQ or GT_NE node)
 * boolPtr returns whether the comparand is a boolean value (must be 0 or 1).
 * When return boolPtr equal to true, if the comparison was against a 1 (i.e true)
 * value then we morph the tree by reversing the GT_EQ/GT_NE and change the 1 to 0.
 */

GenTree* Compiler::optIsBoolCond(GenTree* condBranch, GenTree** compPtr, bool* boolPtr)
{
    bool isBool = false;

    noway_assert(condBranch->gtOper == GT_JTRUE);
    GenTree* cond = condBranch->AsOp()->gtOp1;

    /* The condition must be "!= 0" or "== 0" */

    if ((cond->gtOper != GT_EQ) && (cond->gtOper != GT_NE))
    {
        return nullptr;
    }

    /* Return the compare node to the caller */

    *compPtr = cond;

    /* Get hold of the comparands */

    GenTree* opr1 = cond->AsOp()->gtOp1;
    GenTree* opr2 = cond->AsOp()->gtOp2;

    if (opr2->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    if (!opr2->IsIntegralConst(0) && !opr2->IsIntegralConst(1))
    {
        return nullptr;
    }

    ssize_t ival2 = opr2->AsIntCon()->gtIconVal;

    /* Is the value a boolean?
     * We can either have a boolean expression (marked GTF_BOOLEAN) or
     * a local variable that is marked as being boolean (lvIsBoolean) */

    if (opr1->gtFlags & GTF_BOOLEAN)
    {
        isBool = true;
    }
    else if ((opr1->gtOper == GT_CNS_INT) && (opr1->IsIntegralConst(0) || opr1->IsIntegralConst(1)))
    {
        isBool = true;
    }
    else if (opr1->gtOper == GT_LCL_VAR)
    {
        /* is it a boolean local variable */

        unsigned lclNum = opr1->AsLclVarCommon()->GetLclNum();
        noway_assert(lclNum < lvaCount);

        if (lvaTable[lclNum].lvIsBoolean)
        {
            isBool = true;
        }
    }

    /* Was our comparison against the constant 1 (i.e. true) */
    if (ival2 == 1)
    {
        // If this is a boolean expression tree we can reverse the relop
        // and change the true to false.
        if (isBool)
        {
            gtReverseCond(cond);
            opr2->AsIntCon()->gtIconVal = 0;
        }
        else
        {
            return nullptr;
        }
    }

    *boolPtr = isBool;
    return opr1;
}

void Compiler::optOptimizeBools()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optOptimizeBools()\n");
        if (verboseTrees)
        {
            printf("Blocks/Trees before phase\n");
            fgDispBasicBlocks(true);
        }
    }
#endif
    bool change;

    do
    {
        change = false;

        for (BasicBlock* b1 = fgFirstBB; b1; b1 = b1->bbNext)
        {
            /* We're only interested in conditional jumps here */

            if (b1->bbJumpKind != BBJ_COND)
            {
                continue;
            }

            /* If there is no next block, we're done */

            BasicBlock* b2 = b1->bbNext;
            if (!b2)
            {
                break;
            }

            /* The next block must not be marked as BBF_DONT_REMOVE */
            if (b2->bbFlags & BBF_DONT_REMOVE)
            {
                continue;
            }

            /* The next block also needs to be a condition */

            if (b2->bbJumpKind != BBJ_COND)
            {
#ifdef DEBUG
                optOptimizeBoolsGcStress(b1);
#endif
                continue;
            }

            bool sameTarget; // Do b1 and b2 have the same bbJumpDest?

            if (b1->bbJumpDest == b2->bbJumpDest)
            {
                /* Given the following sequence of blocks :
                        B1: brtrue(t1, BX)
                        B2: brtrue(t2, BX)
                        B3:
                   we will try to fold it to :
                        B1: brtrue(t1|t2, BX)
                        B3:
                */

                sameTarget = true;
            }
            else if (b1->bbJumpDest == b2->bbNext) /*b1->bbJumpDest->bbNum == n1+2*/
            {
                /* Given the following sequence of blocks :
                        B1: brtrue(t1, B3)
                        B2: brtrue(t2, BX)
                        B3:
                   we will try to fold it to :
                        B1: brtrue((!t1)&&t2, BX)
                        B3:
                */

                sameTarget = false;
            }
            else
            {
                continue;
            }

            /* The second block must contain a single statement */

            Statement* s2 = b2->firstStmt();
            if (s2->GetPrevStmt() != s2)
            {
                continue;
            }

            GenTree* t2 = s2->GetRootNode();
            noway_assert(t2->gtOper == GT_JTRUE);

            /* Find the condition for the first block */

            Statement* s1 = b1->lastStmt();

            GenTree* t1 = s1->GetRootNode();
            noway_assert(t1->gtOper == GT_JTRUE);

            if (b2->countOfInEdges() > 1)
            {
                continue;
            }

            /* Find the branch conditions of b1 and b2 */

            bool bool1, bool2;

            GenTree* c1 = optIsBoolCond(t1, &t1, &bool1);
            if (!c1)
            {
                continue;
            }

            GenTree* c2 = optIsBoolCond(t2, &t2, &bool2);
            if (!c2)
            {
                continue;
            }

            noway_assert(t1->OperIs(GT_EQ, GT_NE) && t1->AsOp()->gtOp1 == c1);
            noway_assert(t2->OperIs(GT_EQ, GT_NE) && t2->AsOp()->gtOp1 == c2);

            // Leave out floats where the bit-representation is more complicated
            // - there are two representations for 0.
            //
            if (varTypeIsFloating(c1->TypeGet()) || varTypeIsFloating(c2->TypeGet()))
            {
                continue;
            }

            // Make sure the types involved are of the same sizes
            if (genTypeSize(c1->TypeGet()) != genTypeSize(c2->TypeGet()))
            {
                continue;
            }
            if (genTypeSize(t1->TypeGet()) != genTypeSize(t2->TypeGet()))
            {
                continue;
            }
#ifdef TARGET_ARMARCH
            // Skip the small operand which we cannot encode.
            if (varTypeIsSmall(c1->TypeGet()))
                continue;
#endif
            /* The second condition must not contain side effects */

            if (c2->gtFlags & GTF_GLOB_EFFECT)
            {
                continue;
            }

            /* The second condition must not be too expensive */

            gtPrepareCost(c2);

            if (c2->GetCostEx() > 12)
            {
                continue;
            }

            genTreeOps foldOp;
            genTreeOps cmpOp;
            var_types  foldType = c1->TypeGet();
            if (varTypeIsGC(foldType))
            {
                foldType = TYP_I_IMPL;
            }

            if (sameTarget)
            {
                /* Both conditions must be the same */

                if (t1->gtOper != t2->gtOper)
                {
                    continue;
                }

                if (t1->gtOper == GT_EQ)
                {
                    /* t1:c1==0 t2:c2==0 ==> Branch to BX if either value is 0
                       So we will branch to BX if (c1&c2)==0 */

                    foldOp = GT_AND;
                    cmpOp  = GT_EQ;
                }
                else
                {
                    /* t1:c1!=0 t2:c2!=0 ==> Branch to BX if either value is non-0
                       So we will branch to BX if (c1|c2)!=0 */

                    foldOp = GT_OR;
                    cmpOp  = GT_NE;
                }
            }
            else
            {
                /* The b1 condition must be the reverse of the b2 condition */

                if (t1->gtOper == t2->gtOper)
                {
                    continue;
                }

                if (t1->gtOper == GT_EQ)
                {
                    /* t1:c1==0 t2:c2!=0 ==> Branch to BX if both values are non-0
                       So we will branch to BX if (c1&c2)!=0 */

                    foldOp = GT_AND;
                    cmpOp  = GT_NE;
                }
                else
                {
                    /* t1:c1!=0 t2:c2==0 ==> Branch to BX if both values are 0
                       So we will branch to BX if (c1|c2)==0 */

                    foldOp = GT_OR;
                    cmpOp  = GT_EQ;
                }
            }

            // Anding requires both values to be 0 or 1

            if ((foldOp == GT_AND) && (!bool1 || !bool2))
            {
                continue;
            }

            //
            // Now update the trees
            //
            GenTree* cmpOp1 = gtNewOperNode(foldOp, foldType, c1, c2);
            if (bool1 && bool2)
            {
                /* When we 'OR'/'AND' two booleans, the result is boolean as well */
                cmpOp1->gtFlags |= GTF_BOOLEAN;
            }

            t1->SetOper(cmpOp);
            t1->AsOp()->gtOp1         = cmpOp1;
            t1->AsOp()->gtOp2->gtType = foldType; // Could have been varTypeIsGC()

#if FEATURE_SET_FLAGS
            // For comparisons against zero we will have the GTF_SET_FLAGS set
            // and this can cause an assert to fire in fgMoveOpsLeft(GenTree* tree)
            // during the CSE phase.
            //
            // So make sure to clear any GTF_SET_FLAGS bit on these operations
            // as they are no longer feeding directly into a comparisons against zero

            // Make sure that the GTF_SET_FLAGS bit is cleared.
            // Fix 388436 ARM JitStress WP7
            c1->gtFlags &= ~GTF_SET_FLAGS;
            c2->gtFlags &= ~GTF_SET_FLAGS;

            // The new top level node that we just created does feed directly into
            // a comparison against zero, so set the GTF_SET_FLAGS bit so that
            // we generate an instruction that sets the flags, which allows us
            // to omit the cmp with zero instruction.

            // Request that the codegen for cmpOp1 sets the condition flags
            // when it generates the code for cmpOp1.
            //
            cmpOp1->gtRequestSetFlags();
#endif

            flowList* edge1 = fgGetPredForBlock(b1->bbJumpDest, b1);
            flowList* edge2;

            /* Modify the target of the conditional jump and update bbRefs and bbPreds */

            if (sameTarget)
            {
                edge2 = fgGetPredForBlock(b2->bbJumpDest, b2);
            }
            else
            {
                edge2 = fgGetPredForBlock(b2->bbNext, b2);

                fgRemoveRefPred(b1->bbJumpDest, b1);

                b1->bbJumpDest = b2->bbJumpDest;

                fgAddRefPred(b2->bbJumpDest, b1);
            }

            noway_assert(edge1 != nullptr);
            noway_assert(edge2 != nullptr);

            BasicBlock::weight_t edgeSumMin = edge1->edgeWeightMin() + edge2->edgeWeightMin();
            BasicBlock::weight_t edgeSumMax = edge1->edgeWeightMax() + edge2->edgeWeightMax();
            if ((edgeSumMax >= edge1->edgeWeightMax()) && (edgeSumMax >= edge2->edgeWeightMax()))
            {
                edge1->setEdgeWeights(edgeSumMin, edgeSumMax, b1->bbJumpDest);
            }
            else
            {
                edge1->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT, b1->bbJumpDest);
            }

            /* Get rid of the second block (which is a BBJ_COND) */

            noway_assert(b1->bbJumpKind == BBJ_COND);
            noway_assert(b2->bbJumpKind == BBJ_COND);
            noway_assert(b1->bbJumpDest == b2->bbJumpDest);
            noway_assert(b1->bbNext == b2);
            noway_assert(b2->bbNext);

            fgUnlinkBlock(b2);
            b2->bbFlags |= BBF_REMOVED;

            // If b2 was the last block of a try or handler, update the EH table.

            ehUpdateForDeletedBlock(b2);

            /* Update bbRefs and bbPreds */

            /* Replace pred 'b2' for 'b2->bbNext' with 'b1'
             * Remove  pred 'b2' for 'b2->bbJumpDest' */

            fgReplacePred(b2->bbNext, b2, b1);

            fgRemoveRefPred(b2->bbJumpDest, b2);

            /* Update the block numbers and try again */

            change = true;
            /*
                        do
                        {
                            b2->bbNum = ++n1;
                            b2 = b2->bbNext;
                        }
                        while (b2);
            */

            // Update loop table
            fgUpdateLoopsAfterCompacting(b1, b2);

#ifdef DEBUG
            if (verbose)
            {
                printf("Folded %sboolean conditions of " FMT_BB " and " FMT_BB " to :\n",
                       c2->OperIsLeaf() ? "" : "non-leaf ", b1->bbNum, b2->bbNum);
                gtDispStmt(s1);
                printf("\n");
            }
#endif
        }
    } while (change);

#ifdef DEBUG
    fgDebugCheckBBlist();
#endif
}

typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, unsigned> LclVarRefCounts;

//------------------------------------------------------------------------------------------
// optRemoveRedundantZeroInits: Remove redundant zero intializations.
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

    assert(fgStmtListThreaded);

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
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
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
                    case GT_LCL_VAR_ADDR:
                    case GT_LCL_FLD_ADDR:
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
                                if (BitVecOps::IsMember(&bitVecTraits, zeroInitLocals, lclNum) ||
                                    (lclDsc->lvIsStructField &&
                                     BitVecOps::IsMember(&bitVecTraits, zeroInitLocals, lclDsc->lvParentLcl)) ||
                                    (!lclDsc->lvTracked && !fgVarNeedsExplicitZeroInit(lclNum, bbInALoop, bbIsReturn)))
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
                                // the prolog and this explicit intialization. Therefore, it doesn't
                                // require zero initialization in the prolog.
                                lclDsc->lvHasExplicitInit = 1;
                                JITDUMP("Marking L%02u as having an explicit init\n", lclNum);
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
            LclVarRefCounts::KeyIterator iter(defsInBlock.Begin());
            LclVarRefCounts::KeyIterator end(defsInBlock.End());
            for (; !iter.Equal(end); iter++)
            {
                unsigned int lclNum = iter.Get();
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
