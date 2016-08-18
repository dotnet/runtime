// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#if COUNT_RANGECHECKS
/* static */
unsigned Compiler::optRangeChkRmv = 0;
/* static */
unsigned Compiler::optRangeChkAll = 0;
#endif

/*****************************************************************************/

void Compiler::optInit()
{
    optLoopsMarked = false;
    fgHasLoops     = false;

    /* Initialize the # of tracked loops to 0 */
    optLoopCount = 0;
    /* Keep track of the number of calls and indirect calls made by this method */
    optCallCount         = 0;
    optIndirectCallCount = 0;
    optNativeCallCount   = 0;
    optAssertionCount    = 0;
    optAssertionDep      = nullptr;
#if FEATURE_ANYCSE
    optCSECandidateTotal = 0;
    optCSEstart          = UINT_MAX;
    optCSEcount          = 0;
#endif // FEATURE_ANYCSE
}

DataFlow::DataFlow(Compiler* pCompiler) : m_pCompiler(pCompiler)
{
}

/*****************************************************************************
 *
 */

void Compiler::optSetBlockWeights()
{
    noway_assert(!opts.MinOpts() && !opts.compDbgCode);
    assert(fgDomsComputed);

#ifdef DEBUG
    bool changed = false;
#endif

    bool firstBBdomsRets = true;

    BasicBlock* block;

    for (block = fgFirstBB; (block != nullptr); block = block->bbNext)
    {
        /* Blocks that can't be reached via the first block are rarely executed */
        if (!fgReachable(fgFirstBB, block))
        {
            block->bbSetRunRarely();
        }

        if (block->bbWeight != BB_ZERO_WEIGHT)
        {
            // Calculate our bbWeight:
            //
            //  o BB_UNITY_WEIGHT if we dominate all BBJ_RETURN blocks
            //  o otherwise BB_UNITY_WEIGHT / 2
            //
            bool domsRets = true; // Assume that we will dominate

            for (BasicBlockList* retBlocks = fgReturnBlocks; retBlocks != nullptr; retBlocks = retBlocks->next)
            {
                if (!fgDominate(block, retBlocks->block))
                {
                    domsRets = false;
                    break;
                }
            }

            if (block == fgFirstBB)
            {
                firstBBdomsRets = domsRets;
            }

            // If we are not using profile weight then we lower the weight
            // of blocks that do not dominate a return block
            //
            if (firstBBdomsRets && (fgIsUsingProfileWeights() == false) && (domsRets == false))
            {
#if DEBUG
                changed = true;
#endif
                block->modifyBBWeight(block->bbWeight / 2);
                noway_assert(block->bbWeight);
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
       (assuming that BB_LOOP_WEIGHT is 8)

          1 -- non loop basic block
          8 -- single loop nesting
         64 -- double loop nesting
        512 -- triple loop nesting

    */

    noway_assert(begBlk->bbNum <= endBlk->bbNum);
    noway_assert(begBlk->isLoopHead());
    noway_assert(fgReachable(begBlk, endBlk));

#ifdef DEBUG
    if (verbose)
    {
        printf("\nMarking loop L%02u", begBlk->bbLoopNum);
    }
#endif

    noway_assert(!opts.MinOpts());

    /* Build list of backedges for block begBlk */
    flowList* backedgeList = nullptr;

    for (flowList* pred = begBlk->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        /* Is this a backedge? */
        if (pred->flBlock->bbNum >= begBlk->bbNum)
        {
            flowList* flow = new (this, CMK_FlowList) flowList();

#if MEASURE_BLOCK_SIZE
            genFlowNodeCnt += 1;
            genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE

            flow->flNext  = backedgeList;
            flow->flBlock = pred->flBlock;
            backedgeList  = flow;
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
                BasicBlock* backedge = tmp->flBlock;

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

                unsigned weight;

                if ((curBlk->bbFlags & BBF_PROF_WEIGHT) != 0)
                {
                    // We have real profile weights, so we aren't going to change this blocks weight
                    weight = curBlk->bbWeight;
                }
                else
                {
                    if (dominates)
                    {
                        weight = curBlk->bbWeight * BB_LOOP_WEIGHT;
                    }
                    else
                    {
                        weight = curBlk->bbWeight * (BB_LOOP_WEIGHT / 2);
                    }

                    //
                    // The multiplication may have caused us to overflow
                    //
                    if (weight < curBlk->bbWeight)
                    {
                        // The multiplication caused us to overflow
                        weight = BB_MAX_WEIGHT;
                    }
                    //
                    //  Set the new weight
                    //
                    curBlk->modifyBBWeight(weight);
                }
#ifdef DEBUG
                if (verbose)
                {
                    printf("\n    BB%02u(wt=%s)", curBlk->bbNum, refCntWtd2str(curBlk->getBBWeight(this)));
                }
#endif
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
        curBlk = pred->flBlock;

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
                printf("\nNot removing loop L%02u, due to an additional back edge", begBlk->bbLoopNum);
            }
            else if (backEdgeCount == 0)
            {
                printf("\nNot removing loop L%02u, due to no back edge", begBlk->bbLoopNum);
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
        printf("\nUnmarking loop L%02u", begBlk->bbLoopNum);
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
            unsigned weight = curBlk->bbWeight;

            // Don't unmark blocks that are set to BB_MAX_WEIGHT
            // Don't unmark blocks when we are using profile weights
            //
            if (!curBlk->isMaxBBWeight() && ((curBlk->bbFlags & BBF_PROF_WEIGHT) == 0))
            {
                if (!fgDominate(curBlk, endBlk))
                {
                    weight *= 2;
                }
                else
                {
                    /* Merging of blocks can disturb the Dominates
                       information (see RAID #46649) */
                    if (weight < BB_LOOP_WEIGHT)
                    {
                        weight *= 2;
                    }
                }

                // We can overflow here so check for it
                if (weight < curBlk->bbWeight)
                {
                    weight = BB_MAX_WEIGHT;
                }

                assert(weight >= BB_LOOP_WEIGHT);

                curBlk->modifyBBWeight(weight / BB_LOOP_WEIGHT);
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("\n    BB%02u(wt=%s)", curBlk->bbNum, refCntWtd2str(curBlk->getBBWeight(this)));
            }
#endif
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
        /* Some loops may have been already removed by
         * loop unrolling or conditional folding */

        if (optLoopTable[loopNum].lpFlags & LPFLG_REMOVED)
        {
            continue;
        }

        if (block == optLoopTable[loopNum].lpEntry || block == optLoopTable[loopNum].lpBottom)
        {
            optLoopTable[loopNum].lpFlags |= LPFLG_REMOVED;
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

        noway_assert(optLoopTable[loopNum].lpEntry != block);
        noway_assert(optLoopTable[loopNum].lpBottom != block);

        if (optLoopTable[loopNum].lpExit == block)
        {
            optLoopTable[loopNum].lpExit = nullptr;
            optLoopTable[loopNum].lpFlags &= ~LPFLG_ONE_EXIT;
            ;
        }

        /* If this points to the actual entry in the loop
         * then the whole loop may become unreachable */

        switch (block->bbJumpKind)
        {
            unsigned     jumpCnt;
            BasicBlock** jumpTab;

            case BBJ_NONE:
            case BBJ_COND:
                if (block->bbNext == optLoopTable[loopNum].lpEntry)
                {
                    removeLoop = true;
                    break;
                }
                if (block->bbJumpKind == BBJ_NONE)
                {
                    break;
                }

                __fallthrough;

            case BBJ_ALWAYS:
                noway_assert(block->bbJumpDest);
                if (block->bbJumpDest == optLoopTable[loopNum].lpEntry)
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
                    if ((*jumpTab) == optLoopTable[loopNum].lpEntry)
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

                if (auxBlock->bbNum > optLoopTable[loopNum].lpHead->bbNum &&
                    auxBlock->bbNum <= optLoopTable[loopNum].lpBottom->bbNum)
                {
                    continue;
                }

                switch (auxBlock->bbJumpKind)
                {
                    unsigned     jumpCnt;
                    BasicBlock** jumpTab;

                    case BBJ_NONE:
                    case BBJ_COND:
                        if (auxBlock->bbNext == optLoopTable[loopNum].lpEntry)
                        {
                            removeLoop = false;
                            break;
                        }
                        if (auxBlock->bbJumpKind == BBJ_NONE)
                        {
                            break;
                        }

                        __fallthrough;

                    case BBJ_ALWAYS:
                        noway_assert(auxBlock->bbJumpDest);
                        if (auxBlock->bbJumpDest == optLoopTable[loopNum].lpEntry)
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
                            if ((*jumpTab) == optLoopTable[loopNum].lpEntry)
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
                optLoopTable[loopNum].lpFlags |= LPFLG_REMOVED;
            }
        }
        else if (optLoopTable[loopNum].lpHead == block)
        {
            /* The loop has a new head - Just update the loop table */
            optLoopTable[loopNum].lpHead = block->bbPrev;
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
                                unsigned      parentLoop)
{
    noway_assert(lpHead);

    //
    // NOTE: we take "loopInd" as an argument instead of using the one
    //       stored in begBlk->bbLoopNum because sometimes begBlk->bbLoopNum
    //       has not be set correctly. For example, in optRecordLoop().
    //       However, in most of the cases, loops should have been recorded.
    //       Therefore the correct way is to call the Compiler::optPrintLoopInfo(unsigned lnum)
    //       version of this method.
    //
    printf("L%02u, from BB%02u", loopInd, lpFirst->bbNum);
    if (lpTop != lpFirst)
    {
        printf(" (loop top is BB%02u)", lpTop->bbNum);
    }

    printf(" to BB%02u (Head=BB%02u, Entry=BB%02u, ExitCnt=%d", lpBottom->bbNum, lpHead->bbNum, lpEntry->bbNum,
           lpExitCnt);

    if (lpExitCnt == 1)
    {
        printf(" at BB%02u", lpExit->bbNum);
    }

    if (parentLoop != BasicBlock::NOT_IN_LOOP)
    {
        printf(", parent loop = L%02u", parentLoop);
    }
    printf(")");
}

/*****************************************************************************
 *
 *  Print loop information given the index of the loop in the loop table.
 */

void Compiler::optPrintLoopInfo(unsigned lnum)
{
    noway_assert(lnum < optLoopCount);

    LoopDsc* ldsc = &optLoopTable[lnum]; // lnum is the INDEX to the loop table.

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
bool Compiler::optPopulateInitInfo(unsigned loopInd, GenTreePtr init, unsigned iterVar)
{
    // Operator should be =
    if (init->gtOper != GT_ASG)
    {
        return false;
    }

    GenTreePtr lhs = init->gtOp.gtOp1;
    GenTreePtr rhs = init->gtOp.gtOp2;
    // LHS has to be local and should equal iterVar.
    if (lhs->gtOper != GT_LCL_VAR || lhs->gtLclVarCommon.gtLclNum != iterVar)
    {
        return false;
    }

    // RHS can be constant or local var.
    // TODO-CQ: CLONE: Add arr length for descending loops.
    if (rhs->gtOper == GT_CNS_INT && rhs->TypeGet() == TYP_INT)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_CONST_INIT;
        optLoopTable[loopInd].lpConstInit = (int)rhs->gtIntCon.gtIconVal;
    }
    else if (rhs->gtOper == GT_LCL_VAR)
    {
        optLoopTable[loopInd].lpFlags |= LPFLG_VAR_INIT;
        optLoopTable[loopInd].lpVarInit = rhs->gtLclVarCommon.gtLclNum;
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
    unsigned loopInd, GenTreePtr test, BasicBlock* from, BasicBlock* to, unsigned iterVar)
{
    // Obtain the relop from the "test" tree.
    GenTreePtr relop;
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

    GenTreePtr opr1 = relop->gtOp.gtOp1;
    GenTreePtr opr2 = relop->gtOp.gtOp2;

    GenTreePtr iterOp;
    GenTreePtr limitOp;

    // Make sure op1 or op2 is the iterVar.
    if (opr1->gtOper == GT_LCL_VAR && opr1->gtLclVarCommon.gtLclNum == iterVar)
    {
        iterOp  = opr1;
        limitOp = opr2;
    }
    else if (opr2->gtOper == GT_LCL_VAR && opr2->gtLclVarCommon.gtLclNum == iterVar)
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
    }
    else if (limitOp->gtOper == GT_LCL_VAR && !optIsVarAssigned(from, to, nullptr, limitOp->gtLclVarCommon.gtLclNum))
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
unsigned Compiler::optIsLoopIncrTree(GenTreePtr incr)
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
bool Compiler::optComputeIterInfo(GenTreePtr incr, BasicBlock* from, BasicBlock* to, unsigned* pIterVar)
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
//      newStmt     - contains the statement that is the actual test stmt involving
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
bool Compiler::optIsLoopTestEvalIntoTemp(GenTreePtr testStmt, GenTreePtr* newTest)
{
    GenTreePtr test = testStmt->gtStmt.gtStmtExpr;

    if (test->gtOper != GT_JTRUE)
    {
        return false;
    }

    GenTreePtr relop = test->gtGetOp1();
    noway_assert(relop->OperIsCompare());

    GenTreePtr opr1 = relop->gtOp.gtOp1;
    GenTreePtr opr2 = relop->gtOp.gtOp2;

    // Make sure we have jtrue (vtmp != 0)
    if ((relop->OperGet() == GT_NE) && (opr1->OperGet() == GT_LCL_VAR) && (opr2->OperGet() == GT_CNS_INT) &&
        opr2->IsIntegralConst(0))
    {
        // Get the previous statement to get the def (rhs) of Vtmp to see
        // if the "test" is evaluated into Vtmp.
        GenTreePtr prevStmt = testStmt->gtPrev;
        if (prevStmt == nullptr)
        {
            return false;
        }

        GenTreePtr tree = prevStmt->gtStmt.gtStmtExpr;
        if (tree->OperGet() == GT_ASG)
        {
            GenTreePtr lhs = tree->gtOp.gtOp1;
            GenTreePtr rhs = tree->gtOp.gtOp2;

            // Return as the new test node.
            if (lhs->gtOper == GT_LCL_VAR && lhs->AsLclVarCommon()->GetLclNum() == opr1->AsLclVarCommon()->GetLclNum())
            {
                if (rhs->OperIsCompare())
                {
                    *newTest = prevStmt;
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
    BasicBlock* head, BasicBlock* bottom, BasicBlock* top, GenTreePtr* ppInit, GenTreePtr* ppTest, GenTreePtr* ppIncr)
{
    assert(ppInit != nullptr);
    assert(ppTest != nullptr);
    assert(ppIncr != nullptr);

    // Check if last two statements in the loop body are the increment of the iterator
    // and the loop termination test.
    noway_assert(bottom->bbTreeList != nullptr);
    GenTreePtr test = bottom->bbTreeList->gtPrev;
    noway_assert(test != nullptr && test->gtNext == nullptr);

    GenTreePtr newTest;
    if (optIsLoopTestEvalIntoTemp(test, &newTest))
    {
        test = newTest;
    }

    // Check if we have the incr tree before the test tree, if we don't,
    // check if incr is part of the loop "top".
    GenTreePtr incr = test->gtPrev;
    if (incr == nullptr || optIsLoopIncrTree(incr->gtStmt.gtStmtExpr) == BAD_VAR_NUM)
    {
        if (top == nullptr || top->bbTreeList == nullptr || top->bbTreeList->gtPrev == nullptr)
        {
            return false;
        }

        // If the prev stmt to loop test is not incr, then check if we have loop test evaluated into a tmp.
        GenTreePtr topLast = top->bbTreeList->gtPrev;
        if (optIsLoopIncrTree(topLast->gtStmt.gtStmtExpr) != BAD_VAR_NUM)
        {
            incr = topLast;
        }
        else
        {
            return false;
        }
    }

    assert(test != incr);

    // Find the last statement in the loop pre-header which we expect to be the initialization of
    // the loop iterator.
    GenTreePtr phdr = head->bbTreeList;
    if (phdr == nullptr)
    {
        return false;
    }

    GenTreePtr init = phdr->gtPrev;
    noway_assert(init != nullptr && (init->gtNext == nullptr));

    // If it is a duplicated loop condition, skip it.
    if (init->gtFlags & GTF_STMT_CMPADD)
    {
        // Must be a duplicated loop condition.
        noway_assert(init->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);
        init = init->gtPrev;
        noway_assert(init != nullptr);
    }

    noway_assert(init->gtOper == GT_STMT);
    noway_assert(test->gtOper == GT_STMT);
    noway_assert(incr->gtOper == GT_STMT);

    *ppInit = init->gtStmt.gtStmtExpr;
    *ppTest = test->gtStmt.gtStmtExpr;
    *ppIncr = incr->gtStmt.gtStmtExpr;

    return true;
}

/*****************************************************************************
 *
 *  Record the loop in the loop table.
 */

void Compiler::optRecordLoop(BasicBlock*   head,
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
        return;
    }

    // Assumed preconditions on the loop we're adding.
    assert(first->bbNum <= top->bbNum);
    assert(top->bbNum <= entry->bbNum);
    assert(entry->bbNum <= bottom->bbNum);
    assert(head->bbNum < top->bbNum || head->bbNum > bottom->bbNum);

    // If the new loop contains any existing ones, add it in the right place.
    unsigned char loopInd = optLoopCount;
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

    optLoopTable[loopInd].lpFlags = 0;

    // We haven't yet recorded any side effects.
    optLoopTable[loopInd].lpLoopHasHeapHavoc       = false;
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
        GenTreePtr init;
        GenTreePtr test;
        GenTreePtr incr;
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
        // i.e. HEAD dominates the ENTRY.
        if (!fgDominate(head, entry))
        {
            goto DONE_LOOP;
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
                for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
                {
                    if (stmt->gtStmt.gtStmtExpr == incr)
                    {
                        break;
                    }
                    printf("\n");
                    gtDispTree(stmt->gtStmt.gtStmtExpr);
                }
            } while (block != bottom);
        }
#endif // DEBUG
    }

DONE_LOOP:
    DBEXEC(verbose, optPrintLoopRecording(loopInd));
    optLoopCount++;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// optPrintLoopRecording: Print a recording of the loop.
//
// Arguments:
//      loopInd     - loop index.
//
void Compiler::optPrintLoopRecording(unsigned loopInd)
{
    printf("Recorded loop %s", (loopInd != optLoopCount ? "(extended) " : ""));
    optPrintLoopInfo(optLoopCount, // Not necessarily the loop index, but the number of loops that have been added.
                     optLoopTable[loopInd].lpHead, optLoopTable[loopInd].lpFirst, optLoopTable[loopInd].lpTop,
                     optLoopTable[loopInd].lpEntry, optLoopTable[loopInd].lpBottom, optLoopTable[loopInd].lpExitCnt,
                     optLoopTable[loopInd].lpExit);

    // If an iterator loop print the iterator and the initialization.
    if (optLoopTable[loopInd].lpFlags & LPFLG_ITER)
    {
        printf(" [over V%02u", optLoopTable[loopInd].lpIterVar());
        printf(" (");
        printf(GenTree::NodeName(optLoopTable[loopInd].lpIterOper()));
        printf(" ");
        printf("%d )", optLoopTable[loopInd].lpIterConst());

        if (optLoopTable[loopInd].lpFlags & LPFLG_CONST_INIT)
        {
            printf(" from %d", optLoopTable[loopInd].lpConstInit);
        }
        if (optLoopTable[loopInd].lpFlags & LPFLG_VAR_INIT)
        {
            printf(" from V%02u", optLoopTable[loopInd].lpVarInit);
        }

        // If a simple test condition print operator and the limits */
        printf(GenTree::NodeName(optLoopTable[loopInd].lpTestOper()));

        if (optLoopTable[loopInd].lpFlags & LPFLG_CONST_LIMIT)
        {
            printf("%d ", optLoopTable[loopInd].lpConstLimit());
        }

        if (optLoopTable[loopInd].lpFlags & LPFLG_VAR_LIMIT)
        {
            printf("V%02u ", optLoopTable[loopInd].lpVarLimit());
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
                if (blockPred == pred->flBlock)
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
                    __fallthrough;
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

    flowList* pred;
    flowList* predTop;
    flowList* predEntry;

    noway_assert(fgDomsComputed);
    assert(fgHasLoops);

#if COUNT_LOOPS
    hasMethodLoops         = false;
    loopsThisMethod        = 0;
    loopOverflowThisMethod = false;
#endif

    /* We will use the following terminology:
     * HEAD    - the basic block that flows into the loop ENTRY block (Currently MUST be lexically before entry).
                 Not part of the looping of the loop.
     * FIRST   - the lexically first basic block (in bbNext order) within this loop.  (May be part of a nested loop,
     *           but not the outer loop. ???)
     * TOP     - the target of the backward edge from BOTTOM. In most cases FIRST and TOP are the same.
     * BOTTOM  - the lexically last block in the loop (i.e. the block from which we jump to the top)
     * EXIT    - the loop exit or the block right after the bottom
     * ENTRY   - the entry in the loop (not necessarly the TOP), but there must be only one entry
     *
     * We (currently) require the body of a loop to be a contiguous (in bbNext order) sequence of basic blocks.

            |
            v
          head
            |
            |    top/beg <--+
            |       |       |
            |      ...      |
            |       |       |
            |       v       |
            +---> entry     |
                    |       |
                   ...      |
                    |       |
                    v       |
             +-- exit/tail  |
             |      |       |
             |     ...      |
             |      |       |
             |      v       |
             |    bottom ---+
             |
             +------+
                    |
                    v

     */

    BasicBlock*   head;
    BasicBlock*   top;
    BasicBlock*   bottom;
    BasicBlock*   entry;
    BasicBlock*   exit;
    unsigned char exitCount;

    for (head = fgFirstBB; head->bbNext; head = head->bbNext)
    {
        top       = head->bbNext;
        exit      = nullptr;
        exitCount = 0;

        //  Blocks that are rarely run have a zero bbWeight and should
        //  never be optimized here

        if (top->bbWeight == BB_ZERO_WEIGHT)
        {
            continue;
        }

        for (pred = top->bbPreds; pred; pred = pred->flNext)
        {
            /* Is this a loop candidate? - We look for "back edges", i.e. an edge from BOTTOM
             * to TOP (note that this is an abuse of notation since this is not necessarily a back edge
             * as the definition says, but merely an indication that we have a loop there).
             * Thus, we have to be very careful and after entry discovery check that it is indeed
             * the only place we enter the loop (especially for non-reducible flow graphs).
             */

            bottom    = pred->flBlock;
            exitCount = 0;

            if (top->bbNum <= bottom->bbNum) // is this a backward edge? (from BOTTOM to TOP)
            {
                if ((bottom->bbJumpKind == BBJ_EHFINALLYRET) || (bottom->bbJumpKind == BBJ_EHFILTERRET) ||
                    (bottom->bbJumpKind == BBJ_EHCATCHRET) || (bottom->bbJumpKind == BBJ_CALLFINALLY) ||
                    (bottom->bbJumpKind == BBJ_SWITCH))
                {
                    /* BBJ_EHFINALLYRET, BBJ_EHFILTERRET, BBJ_EHCATCHRET, and BBJ_CALLFINALLY can never form a loop.
                     * BBJ_SWITCH that has a backward jump appears only for labeled break. */
                    goto NO_LOOP;
                }

                BasicBlock* loopBlock;

                /* The presence of a "back edge" is an indication that a loop might be present here
                 *
                 * LOOP:
                 *        1. A collection of STRONGLY CONNECTED nodes i.e. there is a path from any
                 *           node in the loop to any other node in the loop (wholly within the loop)
                 *        2. The loop has a unique ENTRY, i.e. there is only one way to reach a node
                 *           in the loop from outside the loop, and that is through the ENTRY
                 */

                /* Let's find the loop ENTRY */

                if (head->bbJumpKind == BBJ_ALWAYS)
                {
                    if (head->bbJumpDest->bbNum <= bottom->bbNum && head->bbJumpDest->bbNum >= top->bbNum)
                    {
                        /* OK - we enter somewhere within the loop */
                        entry = head->bbJumpDest;

                        /* some useful asserts
                         * Cannot enter at the top - should have being caught by redundant jumps */

                        assert((entry != top) || (head->bbFlags & BBF_KEEP_BBJ_ALWAYS));
                    }
                    else
                    {
                        /* special case - don't consider now */
                        // assert (!"Loop entered in weird way!");
                        goto NO_LOOP;
                    }
                }
                // Can we fall through into the loop?
                else if (head->bbJumpKind == BBJ_NONE || head->bbJumpKind == BBJ_COND)
                {
                    /* The ENTRY is at the TOP (a do-while loop) */
                    entry = top;
                }
                else
                {
                    goto NO_LOOP; // head does not flow into the loop bail for now
                }

                // Now we find the "first" block -- the earliest block reachable within the loop.
                // This is usually the same as "top", but can differ in rare cases where "top" is
                // the entry block of a nested loop, and that nested loop branches backwards to a
                // a block before "top".  We find this by searching for such backwards branches
                // in the loop known so far.
                BasicBlock* first = top;
                BasicBlock* newFirst;
                bool        blocksToSearch = true;
                BasicBlock* validatedAfter = bottom->bbNext;
                while (blocksToSearch)
                {
                    blocksToSearch = false;
                    newFirst       = nullptr;
                    blocksToSearch = false;
                    for (loopBlock = first; loopBlock != validatedAfter; loopBlock = loopBlock->bbNext)
                    {
                        unsigned nSucc = loopBlock->NumSucc();
                        for (unsigned j = 0; j < nSucc; j++)
                        {
                            BasicBlock* succ = loopBlock->GetSucc(j);
                            if ((newFirst == nullptr && succ->bbNum < first->bbNum) ||
                                (newFirst != nullptr && succ->bbNum < newFirst->bbNum))
                            {
                                newFirst = succ;
                            }
                        }
                    }
                    if (newFirst != nullptr)
                    {
                        validatedAfter = first;
                        first          = newFirst;
                        blocksToSearch = true;
                    }
                }

                // Is "head" still before "first"?  If not, we don't have a valid loop...
                if (head->bbNum >= first->bbNum)
                {
                    JITDUMP(
                        "Extending loop [BB%02u..BB%02u] 'first' to BB%02u captures head BB%02u.  Rejecting loop.\n",
                        top->bbNum, bottom->bbNum, first->bbNum, head->bbNum);
                    goto NO_LOOP;
                }

                /* Make sure ENTRY dominates all blocks in the loop
                 * This is necessary to ensure condition 2. above
                 * At the same time check if the loop has a single exit
                 * point - those loops are easier to optimize */

                for (loopBlock = top; loopBlock != bottom->bbNext; loopBlock = loopBlock->bbNext)
                {
                    if (!fgDominate(entry, loopBlock))
                    {
                        goto NO_LOOP;
                    }

                    if (loopBlock == bottom)
                    {
                        if (bottom->bbJumpKind != BBJ_ALWAYS)
                        {
                            /* there is an exit at the bottom */

                            noway_assert(bottom->bbJumpDest == top);
                            exit = bottom;
                            exitCount++;
                            continue;
                        }
                    }

                    BasicBlock* exitPoint;

                    switch (loopBlock->bbJumpKind)
                    {
                        case BBJ_COND:
                        case BBJ_CALLFINALLY:
                        case BBJ_ALWAYS:
                        case BBJ_EHCATCHRET:
                            assert(loopBlock->bbJumpDest);
                            exitPoint = loopBlock->bbJumpDest;

                            if (exitPoint->bbNum < top->bbNum || exitPoint->bbNum > bottom->bbNum)
                            {
                                /* exit from a block other than BOTTOM */
                                exit = loopBlock;
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
                            exit = loopBlock;
                            exitCount++;
                            break;

                        case BBJ_SWITCH:

                            unsigned jumpCnt;
                            jumpCnt = loopBlock->bbJumpSwt->bbsCount;
                            BasicBlock** jumpTab;
                            jumpTab = loopBlock->bbJumpSwt->bbsDstTab;

                            do
                            {
                                noway_assert(*jumpTab);
                                exitPoint = *jumpTab;

                                if (exitPoint->bbNum < top->bbNum || exitPoint->bbNum > bottom->bbNum)
                                {
                                    exit = loopBlock;
                                    exitCount++;
                                }
                            } while (++jumpTab, --jumpCnt);
                            break;

                        default:
                            noway_assert(!"Unexpected bbJumpKind");
                            break;
                    }
                }

                /* Make sure we can iterate the loop (i.e. there is a way back to ENTRY)
                 * This is to ensure condition 1. above which prevents marking fake loops
                 *
                 * Below is an example:
                 *          for (....)
                 *          {
                 *            ...
                 *              computations
                 *            ...
                 *            break;
                 *          }
                 * The example above is not a loop since we bail after the first iteration
                 *
                 * The condition we have to check for is
                 *  1. ENTRY must have at least one predecessor inside the loop. Since we know that that block is
                 *     reachable, it can only be reached through ENTRY, therefore we have a way back to ENTRY
                 *
                 *  2. If we have a GOTO (BBJ_ALWAYS) outside of the loop and that block dominates the
                 *     loop bottom then we cannot iterate
                 *
                 * NOTE that this doesn't entirely satisfy condition 1. since "break" statements are not
                 * part of the loop nodes (as per definition they are loop exits executed only once),
                 * but we have no choice but to include them because we consider all blocks within TOP-BOTTOM */

                for (loopBlock = top; loopBlock != bottom; loopBlock = loopBlock->bbNext)
                {
                    switch (loopBlock->bbJumpKind)
                    {
                        case BBJ_ALWAYS:
                        case BBJ_THROW:
                        case BBJ_RETURN:
                            if (fgDominate(loopBlock, bottom))
                            {
                                goto NO_LOOP;
                            }
                        default:
                            break;
                    }
                }

                bool canIterateLoop = false;

                for (predEntry = entry->bbPreds; predEntry; predEntry = predEntry->flNext)
                {
                    if (predEntry->flBlock->bbNum >= top->bbNum && predEntry->flBlock->bbNum <= bottom->bbNum)
                    {
                        canIterateLoop = true;
                        break;
                    }
                    else if (predEntry->flBlock != head)
                    {
                        // The entry block has multiple predecessors outside the loop; the 'head'
                        // block isn't the only one. We only support a single 'head', so bail.
                        goto NO_LOOP;
                    }
                }

                if (!canIterateLoop)
                {
                    goto NO_LOOP;
                }

                /* Double check - make sure that all loop blocks except ENTRY
                 * have no predecessors outside the loop - this ensures only one loop entry and prevents
                 * us from considering non-loops due to incorrectly assuming that we had a back edge
                 *
                 * OBSERVATION:
                 *    Loops of the form "while (a || b)" will be treated as 2 nested loops (with the same header)
                 */

                for (loopBlock = top; loopBlock != bottom->bbNext; loopBlock = loopBlock->bbNext)
                {
                    if (loopBlock == entry)
                    {
                        continue;
                    }

                    for (predTop = loopBlock->bbPreds; predTop != nullptr; predTop = predTop->flNext)
                    {
                        if (predTop->flBlock->bbNum < top->bbNum || predTop->flBlock->bbNum > bottom->bbNum)
                        {
                            // noway_assert(!"Found loop with multiple entries");
                            goto NO_LOOP;
                        }
                    }
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

                if (bottom->hasTryIndex() && !bbInTryRegions(bottom->getTryIndex(), first))
                {
                    JITDUMP("Loop 'first' BB%02u is in an outer EH region compared to loop 'bottom' BB%02u. Rejecting "
                            "loop.\n",
                            first->bbNum, bottom->bbNum);
                    goto NO_LOOP;
                }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
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
                    JITDUMP("Loop 'first' BB%02u is a finally target. Rejecting loop.\n", first->bbNum);
                    goto NO_LOOP;
                }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

                /* At this point we have a loop - record it in the loop table
                 * If we found only one exit, record it in the table too
                 * (otherwise an exit = 0 in the loop table means multiple exits) */

                assert(pred);
                if (exitCount != 1)
                {
                    exit = nullptr;
                }
                optRecordLoop(head, first, top, entry, bottom, exit, exitCount);

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
                loopExitCountTable.record(static_cast<unsigned>(exitCount));
#endif // COUNT_LOOPS
            }

        /* current predecessor not good for a loop - continue with another one, if any */
        NO_LOOP:;
        }
    }

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
    bool mod = false;
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
        fgUpdateChangedFlowGraph();
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

void Compiler::optRedirectBlock(BasicBlock* blk, BlockToBlockMap* redirectMap)
{
    BasicBlock* newJumpDest = nullptr;
    switch (blk->bbJumpKind)
    {
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_NONE:
        case BBJ_EHFILTERRET:
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
                blk->bbJumpDest = newJumpDest;
            }
            break;

        case BBJ_SWITCH:
        {
            bool redirected = false;
            for (unsigned i = 0; i < blk->bbJumpSwt->bbsCount; i++)
            {
                if (redirectMap->Lookup(blk->bbJumpSwt->bbsDstTab[i], &newJumpDest))
                {
                    blk->bbJumpSwt->bbsDstTab[i] = newJumpDest;
                    redirected                   = true;
                }
            }
            // If any redirections happend, invalidate the switch table map for the switch.
            if (redirected)
            {
                GetSwitchDescMap()->Remove(blk);
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

    JITDUMP("in optCanonicalizeLoop: L%02u has top BB%02u (bottom BB%02u) with natural loop number L%02u: need to "
            "canonicalize\n",
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

    BlockSetOps::Assign(this, newT->bbReach, t->bbReach);

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
    for (flowList* topPred = t->bbPreds; topPred != nullptr; topPred = topPred->flNext)
    {
        BasicBlock* topPredBlock = topPred->flBlock;

        // Skip if topPredBlock is in the loop.
        // Note that this uses block number to detect membership in the loop. We are adding blocks during
        // canonicalization, and those block numbers will be new, and larger than previous blocks. However, we work
        // outside-in, so we shouldn't encounter the new blocks at the loop boundaries, or in the predecessor lists.
        if (t->bbNum <= topPredBlock->bbNum && topPredBlock->bbNum <= b->bbNum)
        {
            JITDUMP("in optCanonicalizeLoop: 'top' predecessor BB%02u is in the range of L%02u (BB%02u..BB%02u); not "
                    "redirecting its bottom edge\n",
                    topPredBlock->bbNum, loopInd, t->bbNum, b->bbNum);
            continue;
        }

        JITDUMP("in optCanonicalizeLoop: redirect top predecessor BB%02u to BB%02u\n", topPredBlock->bbNum,
                newT->bbNum);
        optRedirectBlock(topPredBlock, blockMap);
    }

    assert(newT->bbNext == f);
    if (f != t)
    {
        newT->bbJumpKind = BBJ_ALWAYS;
        newT->bbJumpDest = t;
        newT->bbTreeList = nullptr;
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

    JITDUMP("in optCanonicalizeLoop: made new block BB%02u [%p] the new unique top of loop %d.\n", newT->bbNum,
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
        h2->bbTreeList               = nullptr;
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
        case TYP_CHAR:
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
        case TYP_CHAR:
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
        case TYP_CHAR:
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
                case GT_ASG_SUB:
                case GT_SUB:
                    iterInc = -iterInc;
                    __fallthrough;

                case GT_ASG_ADD:
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

                case GT_ASG_MUL:
                case GT_MUL:
                case GT_ASG_DIV:
                case GT_DIV:
                case GT_ASG_RSH:
                case GT_RSH:
                case GT_ASG_LSH:
                case GT_LSH:
                case GT_ASG_UDIV:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_LT:
            switch (iterOper)
            {
                case GT_ASG_SUB:
                case GT_SUB:
                    iterInc = -iterInc;
                    __fallthrough;

                case GT_ASG_ADD:
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

                case GT_ASG_MUL:
                case GT_MUL:
                case GT_ASG_DIV:
                case GT_DIV:
                case GT_ASG_RSH:
                case GT_RSH:
                case GT_ASG_LSH:
                case GT_LSH:
                case GT_ASG_UDIV:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_LE:
            switch (iterOper)
            {
                case GT_ASG_SUB:
                case GT_SUB:
                    iterInc = -iterInc;
                    __fallthrough;

                case GT_ASG_ADD:
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

                case GT_ASG_MUL:
                case GT_MUL:
                case GT_ASG_DIV:
                case GT_DIV:
                case GT_ASG_RSH:
                case GT_RSH:
                case GT_ASG_LSH:
                case GT_LSH:
                case GT_ASG_UDIV:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_GT:
            switch (iterOper)
            {
                case GT_ASG_SUB:
                case GT_SUB:
                    iterInc = -iterInc;
                    __fallthrough;

                case GT_ASG_ADD:
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

                case GT_ASG_MUL:
                case GT_MUL:
                case GT_ASG_DIV:
                case GT_DIV:
                case GT_ASG_RSH:
                case GT_RSH:
                case GT_ASG_LSH:
                case GT_LSH:
                case GT_ASG_UDIV:
                case GT_UDIV:
                    return false;

                default:
                    noway_assert(!"Unknown operator for loop iterator");
                    return false;
            }

        case GT_GE:
            switch (iterOper)
            {
                case GT_ASG_SUB:
                case GT_SUB:
                    iterInc = -iterInc;
                    __fallthrough;

                case GT_ASG_ADD:
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

                case GT_ASG_MUL:
                case GT_MUL:
                case GT_ASG_DIV:
                case GT_DIV:
                case GT_ASG_RSH:
                case GT_RSH:
                case GT_ASG_LSH:
                case GT_LSH:
                case GT_ASG_UDIV:
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

    if (optCanCloneLoops())
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optUnrollLoops()\n");
    }
#endif
    /* Look for loop unrolling candidates */

    /*  Double loop so that after unrolling an inner loop we set change to true
     *  and we then go back over all of the loop candidates and try to unroll
     *  the next outer loop, until we don't unroll any loops,
     *  then change will be false and we are done.
     */
    for (;;)
    {
        bool change = false;

        for (unsigned lnum = 0; lnum < optLoopCount; lnum++)
        {
            BasicBlock* block;
            BasicBlock* head;
            BasicBlock* bottom;

            GenTree* loop;
            GenTree* test;
            GenTree* incr;
            GenTree* phdr;
            GenTree* init;

            bool       dupCond;
            int        lval;
            int        lbeg;         // initial value for iterator
            int        llim;         // limit value for iterator
            unsigned   lvar;         // iterator lclVar #
            int        iterInc;      // value to increment the iterator
            genTreeOps iterOper;     // type of iterator increment (i.e. ASG_ADD, ASG_SUB, etc.)
            var_types  iterOperType; // type result of the oper (for overflow instrs)
            genTreeOps testOper;     // type of loop test (i.e. GT_LE, GT_GE, etc.)
            bool       unsTest;      // Is the comparison u/int

            unsigned totalIter;     // total number of iterations in the constant loop
            unsigned loopCostSz;    // Cost is size of one iteration
            unsigned loopFlags;     // actual lpFlags
            unsigned requiredFlags; // required lpFlags

            GenTree* loopList; // new stmt list of the unrolled loop
            GenTree* loopLast;

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
                30, // BLENDED_CODE
                0,  // SMALL_CODE
                60, // FAST_CODE
                0   // COUNT_OPT_CODE
            };

            noway_assert(UNROLL_LIMIT_SZ[SMALL_CODE] == 0);
            noway_assert(UNROLL_LIMIT_SZ[COUNT_OPT_CODE] == 0);

            int unrollLimitSz = (unsigned)UNROLL_LIMIT_SZ[compCodeOpt()];

#ifdef DEBUG
            if (compStressCompile(STRESS_UNROLL_LOOPS, 50))
            {
                unrollLimitSz *= 10;
            }
#endif

            loopFlags     = optLoopTable[lnum].lpFlags;
            requiredFlags = LPFLG_DO_WHILE | LPFLG_ONE_EXIT | LPFLG_CONST;

            /* Ignore the loop if we don't have a do-while with a single exit
               that has a constant number of iterations */

            if ((loopFlags & requiredFlags) != requiredFlags)
            {
                continue;
            }

            /* ignore if removed or marked as not unrollable */

            if (optLoopTable[lnum].lpFlags & (LPFLG_DONT_UNROLL | LPFLG_REMOVED))
            {
                continue;
            }

            head = optLoopTable[lnum].lpHead;
            noway_assert(head);
            bottom = optLoopTable[lnum].lpBottom;
            noway_assert(bottom);

            /* The single exit must be at the bottom of the loop */
            noway_assert(optLoopTable[lnum].lpExit);
            if (optLoopTable[lnum].lpExit != bottom)
            {
                continue;
            }

            /* Unrolling loops with jumps in them is not worth the headache
             * Later we might consider unrolling loops after un-switching */

            block = head;
            do
            {
                block = block->bbNext;
                noway_assert(block);

                if (block->bbJumpKind != BBJ_NONE)
                {
                    if (block != bottom)
                    {
                        goto DONE_LOOP;
                    }
                }
            } while (block != bottom);

            /* Get the loop data:
                - initial constant
                - limit constant
                - iterator
                - iterator increment
                - increment operation type (i.e. ASG_ADD, ASG_SUB, etc...)
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

            /* Locate the pre-header and initialization and increment/test statements */

            phdr = head->bbTreeList;
            noway_assert(phdr);
            loop = bottom->bbTreeList;
            noway_assert(loop);

            init = head->lastStmt();
            noway_assert(init && (init->gtNext == nullptr));
            test = bottom->lastStmt();
            noway_assert(test && (test->gtNext == nullptr));
            incr = test->gtPrev;
            noway_assert(incr);

            if (init->gtFlags & GTF_STMT_CMPADD)
            {
                /* Must be a duplicated loop condition */
                noway_assert(init->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);

                dupCond = true;
                init    = init->gtPrev;
                noway_assert(init);
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

            noway_assert(init->gtOper == GT_STMT);
            init = init->gtStmt.gtStmtExpr;
            noway_assert(test->gtOper == GT_STMT);
            test = test->gtStmt.gtStmtExpr;
            noway_assert(incr->gtOper == GT_STMT);
            incr = incr->gtStmt.gtStmtExpr;

            // Don't unroll loops we don't understand.
            if (incr->gtOper == GT_ASG)
            {
                continue;
            }

            /* Make sure everything looks ok */
            if ((init->gtOper != GT_ASG) || (init->gtOp.gtOp1->gtOper != GT_LCL_VAR) ||
                (init->gtOp.gtOp1->gtLclVarCommon.gtLclNum != lvar) || (init->gtOp.gtOp2->gtOper != GT_CNS_INT) ||
                (init->gtOp.gtOp2->gtIntCon.gtIconVal != lbeg) ||

                !((incr->gtOper == GT_ASG_ADD) || (incr->gtOper == GT_ASG_SUB)) ||
                (incr->gtOp.gtOp1->gtOper != GT_LCL_VAR) || (incr->gtOp.gtOp1->gtLclVarCommon.gtLclNum != lvar) ||
                (incr->gtOp.gtOp2->gtOper != GT_CNS_INT) || (incr->gtOp.gtOp2->gtIntCon.gtIconVal != iterInc) ||

                (test->gtOper != GT_JTRUE))
            {
                noway_assert(!"Bad precondition in Compiler::optUnrollLoops()");
                continue;
            }

            /* heuristic - Estimated cost in code size of the unrolled loop */

            loopCostSz = 0;

            block = head;

            do
            {
                block = block->bbNext;

                /* Visit all the statements in the block */

                for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
                {
                    /* Get the expression and stop if end reached */

                    GenTreePtr expr = stmt->gtStmtExpr;
                    if (expr == incr)
                    {
                        break;
                    }

                    /* Calculate gtCostSz */
                    gtSetStmtInfo(stmt);

                    /* Update loopCostSz */
                    loopCostSz += stmt->gtCostSz;
                }
            } while (block != bottom);

            /* Compute the estimated increase in code size for the unrolled loop */

            unsigned int fixedLoopCostSz;
            fixedLoopCostSz = 8;

            int unrollCostSz;
            unrollCostSz = (loopCostSz * totalIter) - (loopCostSz + fixedLoopCostSz);

            /* Don't unroll if too much code duplication would result. */

            if (unrollCostSz > unrollLimitSz)
            {
                /* prevent this loop from being revisited */
                optLoopTable[lnum].lpFlags |= LPFLG_DONT_UNROLL;
                goto DONE_LOOP;
            }

            /* Looks like a good idea to unroll this loop, let's do it! */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (verbose)
            {
                printf("\nUnrolling loop BB%02u", head->bbNext->bbNum);
                if (head->bbNext->bbNum != bottom->bbNum)
                {
                    printf("..BB%02u", bottom->bbNum);
                }
                printf(" over V%02u from %u to %u", lvar, lbeg, llim);
                printf(" unrollCostSz = %d\n", unrollCostSz);
                printf("\n");
            }
#endif

            /* Create the unrolled loop statement list */

            loopList = loopLast = nullptr;

            for (lval = lbeg; totalIter; totalIter--)
            {
                block = head;

                do
                {
                    GenTreeStmt* stmt;
                    GenTree*     expr;

                    block = block->bbNext;
                    noway_assert(block);

                    /* Visit all the statements in the block */

                    for (stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
                    {
                        /* Stop if we've reached the end of the loop */

                        if (stmt->gtStmtExpr == incr)
                        {
                            break;
                        }

                        /* Clone/substitute the expression */

                        expr = gtCloneExpr(stmt, 0, lvar, lval);

                        // cloneExpr doesn't handle everything

                        if (!expr)
                        {
                            optLoopTable[lnum].lpFlags |= LPFLG_DONT_UNROLL;
                            goto DONE_LOOP;
                        }

                        /* Append the expression to our list */

                        if (loopList)
                        {
                            loopLast->gtNext = expr;
                        }
                        else
                        {
                            loopList = expr;
                        }

                        expr->gtPrev = loopLast;
                        loopLast     = expr;
                    }
                } while (block != bottom);

                /* update the new value for the unrolled iterator */

                switch (iterOper)
                {
                    case GT_ASG_ADD:
                        lval += iterInc;
                        break;

                    case GT_ASG_SUB:
                        lval -= iterInc;
                        break;

                    case GT_ASG_RSH:
                    case GT_ASG_LSH:
                        noway_assert(!"Unrolling not implemented for this loop iterator");
                        goto DONE_LOOP;

                    default:
                        noway_assert(!"Unknown operator for constant loop iterator");
                        goto DONE_LOOP;
                }
            }

            /* Finish the linked list */

            if (loopList)
            {
                loopList->gtPrev = loopLast;
                loopLast->gtNext = nullptr;
            }

            /* Replace the body with the unrolled one */

            block = head;

            do
            {
                block = block->bbNext;
                noway_assert(block);
                block->bbTreeList = nullptr;
                block->bbJumpKind = BBJ_NONE;
                block->bbFlags &= ~BBF_NEEDS_GCPOLL;
            } while (block != bottom);

            bottom->bbJumpKind = BBJ_NONE;
            bottom->bbTreeList = loopList;
            bottom->bbFlags &= ~BBF_NEEDS_GCPOLL;
            bottom->modifyBBWeight(bottom->bbWeight / BB_LOOP_WEIGHT);

            bool dummy;

            fgMorphStmts(bottom, &dummy, &dummy, &dummy);

            /* Update bbRefs and bbPreds */
            /* Here head->bbNext is bottom !!! - Replace it */

            fgRemoveRefPred(head->bbNext, bottom);

            /* Now change the initialization statement in the HEAD to "lvar = lval;"
             * (the last value of the iterator in the loop)
             * and drop the jump condition since the unrolled loop will always execute */

            init->gtOp.gtOp2->gtIntCon.gtIconVal = lval;

            /* if the HEAD is a BBJ_COND drop the condition (and make HEAD a BBJ_NONE block) */

            if (head->bbJumpKind == BBJ_COND)
            {
                phdr = head->bbTreeList;
                noway_assert(phdr);
                test = phdr->gtPrev;

                noway_assert(test && (test->gtNext == nullptr));
                noway_assert(test->gtOper == GT_STMT);
                noway_assert(test->gtStmt.gtStmtExpr->gtOper == GT_JTRUE);

                init = test->gtPrev;
                noway_assert(init && (init->gtNext == test));
                noway_assert(init->gtOper == GT_STMT);

                init->gtNext     = nullptr;
                phdr->gtPrev     = init;
                head->bbJumpKind = BBJ_NONE;
                head->bbFlags &= ~BBF_NEEDS_GCPOLL;

                /* Update bbRefs and bbPreds */

                fgRemoveRefPred(head->bbJumpDest, head);
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

                GenTreePtr s = loopList;

                while (s)
                {
                    noway_assert(s->gtOper == GT_STMT);
                    gtDispTree(s);
                    s = s->gtNext;
                }
                printf("\n");

                gtDispTree(init);
                printf("\n");
            }
#endif

            /* Remember that something has changed */

            change = true;

            /* Make sure to update loop table */

            /* Use the LPFLG_REMOVED flag and update the bbLoopMask acordingly
             * (also make head and bottom NULL - to hit an assert or GPF) */

            optLoopTable[lnum].lpFlags |= LPFLG_REMOVED;
            optLoopTable[lnum].lpHead = optLoopTable[lnum].lpBottom = nullptr;

        DONE_LOOP:;
        }

        if (!change)
        {
            break;
        }
    }

#ifdef DEBUG
    fgDebugCheckBBlist();
#endif
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Return non-zero if there is a code path from 'topBB' to 'botBB' that will
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

/*****************************************************************************
 *
 * Find the loop termination test at the bottom of the loop
 */

static GenTreePtr optFindLoopTermTest(BasicBlock* bottom)
{
    GenTreePtr testt = bottom->bbTreeList;

    assert(testt && testt->gtOper == GT_STMT);

    GenTreePtr result = testt->gtPrev;

#ifdef DEBUG
    while (testt->gtNext)
    {
        testt = testt->gtNext;
    }

    assert(testt == result);
#endif

    return result;
}

/*****************************************************************************
 * Optimize "jmp C; do{} C:while(cond);" loops to "if (cond){ do{}while(cond}; }"
 */

void Compiler::fgOptWhileLoop(BasicBlock* block)
{
    noway_assert(!opts.MinOpts() && !opts.compDbgCode);
    noway_assert(compCodeOpt() != SMALL_CODE);

    /*
        Optimize while loops into do { } while loop
        Our loop hoisting logic requires do { } while loops.
        Specifically, we're looking for the following case:

                ...
                jmp test
        loop:
                ...
                ...
        test:
                cond
                jtrue   loop

        If we find this, and the condition is simple enough, we change
        the loop to the following:

                ...
                cond
                jfalse done
                // else fall-through
        loop:
                ...
                ...
        test:
                cond
                jtrue   loop
        done:

     */

    /* Does the BB end with an unconditional jump? */

    if (block->bbJumpKind != BBJ_ALWAYS || (block->bbFlags & BBF_KEEP_BBJ_ALWAYS))
    { // It can't be one of the ones we use for our exception magic
        return;
    }

    // It has to be a forward jump
    //  TODO-CQ: Check if we can also optimize the backwards jump as well.
    //
    if (fgIsForwardBranch(block) == false)
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
    noway_assert(bTest->bbNext);

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

    GenTreePtr condStmt = optFindLoopTermTest(bTest);

    // bTest must only contain only a jtrue with no other stmts, we will only clone
    // the conditional, so any other statements will not get cloned
    //  TODO-CQ: consider cloning the whole bTest block as inserting it after block.
    //
    if (bTest->bbTreeList != condStmt)
    {
        return;
    }

    /* Get to the condition node from the statement tree */

    noway_assert(condStmt->gtOper == GT_STMT);

    GenTreePtr condTree = condStmt->gtStmt.gtStmtExpr;
    noway_assert(condTree->gtOper == GT_JTRUE);

    condTree = condTree->gtOp.gtOp1;

    // The condTree has to be a RelOp comparison
    //  TODO-CQ: Check if we can also optimize the backwards jump as well.
    //
    if (condTree->OperIsCompare() == false)
    {
        return;
    }

    /* We call gtPrepareCost to measure the cost of duplicating this tree */

    gtPrepareCost(condTree);
    unsigned estDupCostSz = condTree->gtCostSz;

    double loopIterations = (double)BB_LOOP_WEIGHT;

    bool                 allProfileWeightsAreValid = false;
    BasicBlock::weight_t weightBlock               = block->bbWeight;
    BasicBlock::weight_t weightTest                = bTest->bbWeight;
    BasicBlock::weight_t weightNext                = block->bbNext->bbWeight;

    // If we have profile data then we calculate the number of time
    // the loop will iterate into loopIterations
    if (fgIsUsingProfileWeights())
    {
        // Only rely upon the profile weight when all three of these blocks
        // have good profile weights
        if ((block->bbFlags & BBF_PROF_WEIGHT) && (bTest->bbFlags & BBF_PROF_WEIGHT) &&
            (block->bbNext->bbFlags & BBF_PROF_WEIGHT))
        {
            allProfileWeightsAreValid = true;

            // If this while loop never iterates then don't bother transforming
            if (weightNext == 0)
            {
                return;
            }

            // with (weighNext > 0) we should also have (weightTest >= weightBlock)
            // if the profile weights are all valid.
            //
            //   weightNext is the number of time this loop iterates
            //   weightBlock is the number of times that we enter the while loop
            //   loopIterations is the average number of times that this loop iterates
            //
            if (weightTest >= weightBlock)
            {
                loopIterations = (double)block->bbNext->bbWeight / (double)block->bbWeight;
            }
        }
    }

    unsigned maxDupCostSz = 32;

    // optFastCodeOrBlendedLoop(bTest->bbWeight) does not work here as we have not
    // set loop weights yet
    if ((compCodeOpt() == FAST_CODE) || compStressCompile(STRESS_DO_WHILE_LOOPS, 30))
    {
        maxDupCostSz *= 4;
    }

    // If this loop iterates a lot then raise the maxDupCost
    if (loopIterations >= 12.0)
    {
        maxDupCostSz *= 2;
    }
    if (loopIterations >= 96.0)
    {
        maxDupCostSz *= 2;
    }

    // If the loop condition has a shared static helper, we really want this loop converted
    // as not converting the loop will disable loop hoisting, meaning the shared helper will
    // be executed on every loop iteration.
    int countOfHelpers = 0;
    fgWalkTreePre(&condTree, CountSharedStaticHelper, &countOfHelpers);

    if (countOfHelpers > 0 && compCodeOpt() != SMALL_CODE)
    {
        maxDupCostSz += 24 * min(countOfHelpers, (int)(loopIterations + 1.5));
    }

    // If the compare has too high cost then we don't want to dup

    bool costIsTooHigh = (estDupCostSz > maxDupCostSz);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDuplication of loop condition [%06u] is %s, because the cost of duplication (%i) is %s than %i,"
               "\n   loopIterations = %7.3f, countOfHelpers = %d, validProfileWeights = %s\n",
               condTree->gtTreeID, costIsTooHigh ? "not done" : "performed", estDupCostSz,
               costIsTooHigh ? "greater" : "less or equal", maxDupCostSz, loopIterations, countOfHelpers,
               allProfileWeightsAreValid ? "true" : "false");
    }
#endif

    if (costIsTooHigh)
    {
        return;
    }

    /* Looks good - duplicate the condition test */

    condTree->gtFlags |= GTF_RELOP_ZTT;

    condTree = gtCloneExpr(condTree);
    gtReverseCond(condTree);

    // Make sure clone expr copied the flag
    assert(condTree->gtFlags & GTF_RELOP_ZTT);

    condTree = gtNewOperNode(GT_JTRUE, TYP_VOID, condTree);

    /* Create a statement entry out of the condition and
       append the condition test at the end of 'block' */

    GenTreePtr copyOfCondStmt = fgInsertStmtAtEnd(block, condTree);

    copyOfCondStmt->gtFlags |= GTF_STMT_CMPADD;

#ifdef DEBUGGING_SUPPORT
    if (opts.compDbgInfo)
    {
        copyOfCondStmt->gtStmt.gtStmtILoffsx = condStmt->gtStmt.gtStmtILoffsx;
    }
#endif

    // Flag the block that received the copy as potentially having an array/vtable
    // reference if the block copied from did; this is a conservative guess.
    if (auto copyFlags = bTest->bbFlags & (BBF_HAS_VTABREF | BBF_HAS_IDX_LEN))
    {
        block->bbFlags |= copyFlags;
    }

    // If we have profile data for all blocks and we know that we are cloning the
    //  bTest block into block and thus changing the control flow from block so
    //  that it no longer goes directly to bTest anymore, we have to adjust the
    //  weight of bTest by subtracting out the weight of block.
    //
    if (allProfileWeightsAreValid)
    {
        //
        // Some additional sanity checks before adjusting the weight of bTest
        //
        if ((weightNext > 0) && (weightTest >= weightBlock) && (weightTest != BB_MAX_WEIGHT))
        {
            // Get the two edge that flow out of bTest
            flowList* edgeToNext = fgGetPredForBlock(bTest->bbNext, bTest);
            flowList* edgeToJump = fgGetPredForBlock(bTest->bbJumpDest, bTest);

            // Calculate the new weight for block bTest

            BasicBlock::weight_t newWeightTest =
                (weightTest > weightBlock) ? (weightTest - weightBlock) : BB_ZERO_WEIGHT;
            bTest->bbWeight = newWeightTest;

            if (newWeightTest == BB_ZERO_WEIGHT)
            {
                bTest->bbFlags |= BBF_RUN_RARELY;
                // All out edge weights are set to zero
                edgeToNext->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edgeToNext->flEdgeWeightMax = BB_ZERO_WEIGHT;
                edgeToJump->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edgeToJump->flEdgeWeightMax = BB_ZERO_WEIGHT;
            }
            else
            {
                // Update the our edge weights
                edgeToNext->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edgeToNext->flEdgeWeightMax = min(edgeToNext->flEdgeWeightMax, newWeightTest);
                edgeToJump->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edgeToJump->flEdgeWeightMax = min(edgeToJump->flEdgeWeightMax, newWeightTest);
            }
        }
    }

    /* Change the block to end with a conditional jump */

    block->bbJumpKind = BBJ_COND;
    block->bbJumpDest = bTest->bbNext;

    /* Mark the jump dest block as being a jump target */
    block->bbJumpDest->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

    /* Update bbRefs and bbPreds for 'block->bbNext' 'bTest' and 'bTest->bbNext' */

    fgAddRefPred(block->bbNext, block);

    fgRemoveRefPred(bTest, block);
    fgAddRefPred(bTest->bbNext, block);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDuplicating loop condition in BB%02u for loop (BB%02u - BB%02u)", block->bbNum, block->bbNext->bbNum,
               bTest->bbNum);
        printf("\nEstimated code size expansion is %d\n ", estDupCostSz);

        gtDispTree(copyOfCondStmt);
    }

#endif
}

/*****************************************************************************
 *
 *  Optimize the BasicBlock layout of the method
 */

void Compiler::optOptimizeLayout()
{
    noway_assert(!opts.MinOpts() && !opts.compDbgCode);

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optOptimizeLayout()\n");
        fgDispHandlerTab();
    }

    /* Check that the flowgraph data (bbNum, bbRefs, bbPreds) is up-to-date */
    fgDebugCheckBBlist();
#endif

    noway_assert(fgModified == false);

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        /* Make sure the appropriate fields are initialized */

        if (block->bbWeight == BB_ZERO_WEIGHT)
        {
            /* Zero weighted block can't have a LOOP_HEAD flag */
            noway_assert(block->isLoopHead() == false);
            continue;
        }

        assert(block->bbLoopNum == 0);

        if (compCodeOpt() != SMALL_CODE)
        {
            /* Optimize "while(cond){}" loops to "cond; do{}while(cond);" */

            fgOptWhileLoop(block);
        }
    }

    if (fgModified)
    {
        // Recompute the edge weight if we have modified the flow graph in fgOptWhileLoop
        fgComputeEdgeWeights();
    }

    fgUpdateFlowGraph(true);
    fgReorderBlocks();
    fgUpdateFlowGraph();
}

/*****************************************************************************
 *
 *  Perform loop inversion, find and classify natural loops
 */

void Compiler::optOptimizeLoops()
{
    noway_assert(!opts.MinOpts() && !opts.compDbgCode);

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optOptimizeLoops()\n");
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

                BasicBlock* bottom = pred->flBlock;

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
#ifdef DEBUG
                /* Mark the loop header as such */
                assert(FitsIn<unsigned char>(loopNum));
                top->bbLoopNum = (unsigned char)loopNum;
#endif

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
#endif
        optLoopsMarked = true;
    }
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
    JITDUMP("Deriving cloning conditions for L%02u\n", loopNum);

    LoopDsc*                      loop     = &optLoopTable[loopNum];
    ExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);

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
            // Only allowing const init at this time.
            if (loop->lpConstInit < 0)
            {
                JITDUMP("> Init %d is invalid\n", loop->lpConstInit);
                return false;
            }
        }
        else if (loop->lpFlags & LPFLG_VAR_INIT)
        {
            // limitVar >= 0
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
            ident = LC_Ident(limit, LC_Ident::Const);
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
            ArrIndex* index = new (getAllocator()) ArrIndex(getAllocator());
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
                                      LC_Expr(LC_Ident(LC_Array(LC_Array::MdArray,
                                                                mdArrInfo->GetArrIndexForDim(getAllocator()),
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
//           a[i][j][k] = 0;
//
//     Now, we want to deref a[i][j] to invoke length operator on it to perform the cloning fast path check.
//     This involves deref of (a), (a[i]), (a[i][j]), therefore, the following should first
//     be true to do the deref.
//
//     (a != null) && (i < a.len) && (a[i] != null) && (j < a[i].len) && (a[i][j] != null) --> (1)
//
//     Note the short circuiting AND. Implication: these conditions should be performed in separate
//     blocks each of which will branch to slow path if the condition evaluates to false.
//
//     Now, imagine a situation where we have
//      a[x][y][k] = 20 and a[i][j][k] = 0
//     also in the inner most loop where x, y are parameters, then our conditions will have
//     to include
//     (x < a.len) &&
//     (y < a[x].len)
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
//
bool Compiler::optComputeDerefConditions(unsigned loopNum, LoopCloneContext* context)
{
    ExpandArrayStack<LC_Deref*> nodes(getAllocator());
    int                         maxRank = -1;

    // Get the dereference-able arrays.
    ExpandArrayStack<LC_Array>* deref = context->EnsureDerefs(loopNum);

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
            node = new (getAllocator()) LC_Deref(array, 0 /*level*/);
            nodes.Push(node);
        }

        // For each dimension (level) for the array, populate the tree with the variable
        // from that dimension.
        unsigned rank = (unsigned)array.GetDimRank();
        for (unsigned i = 0; i < rank; ++i)
        {
            node->EnsureChildren(getAllocator());
            LC_Deref* tmp = node->Find(array.arrIndex->indLcls[i]);
            if (tmp == nullptr)
            {
                tmp = new (getAllocator()) LC_Deref(array, node->level + 1);
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

    // Heuristic to not create too many blocks;
    if (condBlocks > 4)
    {
        return false;
    }

    // Derive conditions into an 'array of level x array of conditions' i.e., levelCond[levels][conds]
    ExpandArrayStack<ExpandArrayStack<LC_Condition>*>* levelCond = context->EnsureBlockConditions(loopNum, condBlocks);
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
//      insertBefore - the tree before which the helper call will be inserted.
//
void Compiler::optDebugLogLoopCloning(BasicBlock* block, GenTreePtr insertBefore)
{
    if (JitConfig.JitDebugLogLoopCloning() == 0)
    {
        return;
    }
    GenTreePtr logCall = gtNewHelperCallNode(CORINFO_HELP_DEBUG_LOG_LOOP_CLONING, TYP_VOID);
    GenTreePtr stmt    = fgNewStmtFromTree(logCall);
    fgInsertStmtBefore(block, insertBefore, stmt);
    fgMorphBlockStmt(block, stmt DEBUGARG("Debug log loop cloning"));
}
#endif

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
    ExpandArrayStack<LcOptInfo*>* optInfos = context->GetLoopOptInfo(loopNum);
    for (unsigned i = 0; i < optInfos->Size(); ++i)
    {
        LcOptInfo* optInfo = optInfos->GetRef(i);
        switch (optInfo->GetOptType())
        {
            case LcOptInfo::LcJaggedArray:
            {
                LcJaggedArrayOptInfo* arrIndexInfo = optInfo->AsLcJaggedArrayOptInfo();
                compCurBB                          = arrIndexInfo->arrIndex.useBlock;
                optRemoveRangeCheck(arrIndexInfo->arrIndex.bndsChks[arrIndexInfo->dim], arrIndexInfo->stmt, true,
                                    GTF_ASG, true);
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
//  optCanCloneLoops: Use the environment flag to determine whether loop
//      cloning is allowed to be performed.
//
//  Return Value:
//      Returns true in debug builds if COMPlus_JitCloneLoops flag is set.
//      Disabled for retail for now.
//
bool Compiler::optCanCloneLoops()
{
    // Enabled for retail builds now.
    unsigned cloneLoopsFlag = 1;
#ifdef DEBUG
    cloneLoopsFlag = JitConfig.JitCloneLoops();
#endif
    return (cloneLoopsFlag != 0);
}

//----------------------------------------------------------------------------
//  optIsLoopClonable: Determine whether this loop can be cloned.
//
//  Arguments:
//      loopInd     loop index which needs to be checked if it can be cloned.
//
//  Return Value:
//      Returns true if the loop can be cloned. If it returns false
//      prints a message in debug as why the loop can't be cloned.
//
bool Compiler::optIsLoopClonable(unsigned loopInd)
{
    // First, for now, make sure the loop doesn't have any embedded exception handling -- I don't want to tackle
    // inserting new EH regions in the exception table yet.
    BasicBlock* stopAt       = optLoopTable[loopInd].lpBottom->bbNext;
    unsigned    loopRetCount = 0;
    for (BasicBlock* blk = optLoopTable[loopInd].lpFirst; blk != stopAt; blk = blk->bbNext)
    {
        if (blk->bbJumpKind == BBJ_RETURN)
        {
            loopRetCount++;
        }
        if (bbIsTryBeg(blk))
        {
            JITDUMP("Loop cloning: rejecting loop %d in %s, because it has a try begin.\n", loopInd, info.compFullName);
            return false;
        }
    }

    // Is the entry block a handler or filter start?  If so, then if we cloned, we could create a jump
    // into the middle of a handler (to go to the cloned copy.)  Reject.
    if (bbIsHandlerBeg(optLoopTable[loopInd].lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop because entry block is a handler start.\n");
        return false;
    }

    // If the head and entry are in different EH regions, reject.
    if (!BasicBlock::sameEHRegion(optLoopTable[loopInd].lpHead, optLoopTable[loopInd].lpEntry))
    {
        JITDUMP("Loop cloning: rejecting loop because head and entry blocks are in different EH regions.\n");
        return false;
    }

    // Is the first block after the last block of the loop a handler or filter start?
    // Usually, we create a dummy block after the orginal loop, to skip over the loop clone
    // and go to where the original loop did.  That raises problems when we don't actually go to
    // that block; this is one of those cases.  This could be fixed fairly easily; for example,
    // we could add a dummy nop block after the (cloned) loop bottom, in the same handler scope as the
    // loop.  This is just a corner to cut to get this working faster.
    BasicBlock* bbAfterLoop = optLoopTable[loopInd].lpBottom->bbNext;
    if (bbAfterLoop != nullptr && bbIsHandlerBeg(bbAfterLoop))
    {
        JITDUMP("Loop cloning: rejecting loop because next block after bottom is a handler start.\n");
        return false;
    }

    // We've previously made a decision whether to have separate return epilogs, or branch to one.
    // There's a GCInfo limitation in the x86 case, so that there can be no more than 4 separate epilogs.
    // (I thought this was x86-specific, but it's not if-d.  On other architectures, the decision should be made as a
    // heuristic tradeoff; perhaps we're just choosing to live with 4 as the limit.)
    if (fgReturnCount + loopRetCount > 4)
    {
        JITDUMP("Loop cloning: rejecting loop because it has %d returns; if added to previously-existing %d returns, "
                "would exceed the limit of 4.\n",
                loopRetCount, fgReturnCount);
        return false;
    }

    // Otherwise, we're going to add those return blocks.
    fgReturnCount += loopRetCount;

    return true;
}

/*****************************************************************************
 *
 *  Identify loop cloning opportunities, derive loop cloning conditions,
 *  perform loop cloning, use the derived conditions to choose which
 *  path to take.
 */
void Compiler::optCloneLoops()
{
    JITDUMP("\n*************** In optCloneLoops()\n");
    if (optLoopCount == 0 || !optCanCloneLoops())
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Blocks/Trees at start of phase\n");
        fgDispBasicBlocks(true);
    }
#endif

    LoopCloneContext context(optLoopCount, getAllocator());

    // Obtain array optimization candidates in the context.
    optObtainLoopCloningOpts(&context);

    // For each loop, derive cloning conditions for the optimization candidates.
    for (unsigned i = 0; i < optLoopCount; ++i)
    {
        ExpandArrayStack<LcOptInfo*>* optInfos = context.GetLoopOptInfo(i);
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
        printf("\nAfter loop cloning:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }
#endif
}

void Compiler::optCloneLoop(unsigned loopInd, LoopCloneContext* context)
{
    assert(loopInd < optLoopCount);

    JITDUMP("\nCloning loop %d: [h: %d, f: %d, t: %d, e: %d, b: %d].\n", loopInd, optLoopTable[loopInd].lpHead->bbNum,
            optLoopTable[loopInd].lpFirst->bbNum, optLoopTable[loopInd].lpTop->bbNum,
            optLoopTable[loopInd].lpEntry->bbNum, optLoopTable[loopInd].lpBottom->bbNum);

    // Determine the depth of the loop, so we can properly weight blocks added (outside the cloned loop blocks).
    unsigned depth         = optLoopDepth(loopInd);
    unsigned ambientWeight = 1;
    for (unsigned j = 0; j < depth; j++)
    {
        unsigned lastWeight = ambientWeight;
        ambientWeight *= BB_LOOP_WEIGHT;
        // If the multiplication overflowed, stick at max.
        // (Strictly speaking, a multiplication could overflow and still have a result
        // that is >= lastWeight...but if so, the original weight must be pretty large,
        // and it got bigger, so that's OK.)
        if (ambientWeight < lastWeight)
        {
            ambientWeight = BB_MAX_WEIGHT;
            break;
        }
    }

    // If we're in a non-natural loop, the ambient weight might be higher than we computed above.
    // Be safe by taking the max with the head block's weight.
    ambientWeight = max(ambientWeight, optLoopTable[loopInd].lpHead->bbWeight);

    // This is the containing loop, if any -- to label any blocks we create that are outside
    // the loop being cloned.
    unsigned char ambientLoop = optLoopTable[loopInd].lpParent;

    // First, make sure that the loop has a unique header block, creating an empty one if necessary.
    optEnsureUniqueHead(loopInd, ambientWeight);

    // We're going to make

    // H --> E
    // F
    // T
    // E
    // B  ?-> T
    // X
    //
    //   become
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

    BasicBlock* h = optLoopTable[loopInd].lpHead;
    if (h->bbJumpKind != BBJ_NONE && h->bbJumpKind != BBJ_ALWAYS)
    {
        // Make a new block to be the unique entry to the loop.
        assert(h->bbJumpKind == BBJ_COND && h->bbNext == optLoopTable[loopInd].lpEntry);
        BasicBlock* newH = fgNewBBafter(BBJ_NONE, h,
                                        /*extendRegion*/ true);
        newH->bbWeight = (newH->isRunRarely() ? 0 : ambientWeight);
        BlockSetOps::Assign(this, newH->bbReach, h->bbReach);
        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        newH->bbNatLoopNum = ambientLoop;
        h                  = newH;
        optUpdateLoopHead(loopInd, optLoopTable[loopInd].lpHead, h);
    }

    // First, make X2 after B, if necessary.  (Not necessary if b is a BBJ_ALWAYS.)
    // "newPred" will be the predecessor of the blocks of the cloned loop.
    BasicBlock* b       = optLoopTable[loopInd].lpBottom;
    BasicBlock* newPred = b;
    if (b->bbJumpKind != BBJ_ALWAYS)
    {
        BasicBlock* x = b->bbNext;
        if (x != nullptr)
        {
            BasicBlock* x2 = fgNewBBafter(BBJ_ALWAYS, b, /*extendRegion*/ true);
            x2->bbWeight   = (x2->isRunRarely() ? 0 : ambientWeight);

            // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
            x2->bbNatLoopNum = ambientLoop;

            x2->bbJumpDest = x;
            BlockSetOps::Assign(this, x2->bbReach, h->bbReach);
            newPred = x2;
        }
    }

    // Now we'll make "h2", after "h" to go to "e" -- unless the loop is a do-while,
    // so that "h" already falls through to "e" (e == t == f).
    BasicBlock* h2 = nullptr;
    if (optLoopTable[loopInd].lpHead->bbNext != optLoopTable[loopInd].lpEntry)
    {
        BasicBlock* h2 = fgNewBBafter(BBJ_ALWAYS, optLoopTable[loopInd].lpHead,
                                      /*extendRegion*/ true);
        h2->bbWeight = (h2->isRunRarely() ? 0 : ambientWeight);

        // This is in the scope of a surrounding loop, if one exists -- the parent of the loop we're cloning.
        h2->bbNatLoopNum = ambientLoop;

        h2->bbJumpDest = optLoopTable[loopInd].lpEntry;
        optUpdateLoopHead(loopInd, optLoopTable[loopInd].lpHead, h2);
    }

    // Now we'll clone the blocks of the loop body.
    BasicBlock* newFirst = nullptr;
    BasicBlock* newBot   = nullptr;

    BlockToBlockMap* blockMap = new (getAllocator()) BlockToBlockMap(getAllocator());
    for (BasicBlock* blk = optLoopTable[loopInd].lpFirst; blk != optLoopTable[loopInd].lpBottom->bbNext;
         blk             = blk->bbNext)
    {
        BasicBlock* newBlk = fgNewBBafter(blk->bbJumpKind, newPred,
                                          /*extendRegion*/ true);

        BasicBlock::CloneBlockState(this, newBlk, blk);
        // TODO-Cleanup: The above clones the bbNatLoopNum, which is incorrect.  Eventually, we should probably insert
        // the cloned loop in the loop table.  For now, however, we'll just make these blocks be part of the surrounding
        // loop, if one exists -- the parent of the loop we're cloning.
        newBlk->bbNatLoopNum = optLoopTable[loopInd].lpParent;

        if (newFirst == nullptr)
        {
            newFirst = newBlk;
        }
        newBot  = newBlk; // Continually overwrite to make sure we get the last one.
        newPred = newBlk;
        blockMap->Set(blk, newBlk);
    }

    // Perform the static optimizations on the fast path.
    optPerformStaticOptimizations(loopInd, context DEBUGARG(true));

    // Now go through the new blocks, remapping their jump targets within the loop.
    for (BasicBlock* blk = optLoopTable[loopInd].lpFirst; blk != optLoopTable[loopInd].lpBottom->bbNext;
         blk             = blk->bbNext)
    {

        BasicBlock* newblk = nullptr;
        bool        b      = blockMap->Lookup(blk, &newblk);
        assert(b && newblk != nullptr);

        assert(blk->bbJumpKind == newblk->bbJumpKind);

        // First copy the jump destination(s) from "blk".
        optCopyBlkDest(blk, newblk);

        // Now redirect the new block according to "blockMap".
        optRedirectBlock(newblk, blockMap);
    }

    assert((h->bbJumpKind == BBJ_NONE && (h->bbNext == h2 || h->bbNext == optLoopTable[loopInd].lpEntry)) ||
           (h->bbJumpKind == BBJ_ALWAYS));

    // If all the conditions are true, go to E2.
    BasicBlock* e2      = nullptr;
    bool        foundIt = blockMap->Lookup(optLoopTable[loopInd].lpEntry, &e2);

    h->bbJumpKind = BBJ_COND;

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

    // Create a unique header for the slow path.
    BasicBlock* slowHead   = fgNewBBafter(BBJ_ALWAYS, h, true);
    slowHead->bbWeight     = (h->isRunRarely() ? 0 : ambientWeight);
    slowHead->bbNatLoopNum = ambientLoop;
    slowHead->bbJumpDest   = e2;

    BasicBlock* condLast = optInsertLoopChoiceConditions(context, loopInd, h, slowHead);
    condLast->bbJumpDest = slowHead;

    // If h2 is present it is already the head or replace 'h' by 'condLast'.
    if (h2 == nullptr)
    {
        optUpdateLoopHead(loopInd, optLoopTable[loopInd].lpHead, condLast);
    }
    assert(foundIt && e2 != nullptr);

    fgUpdateChangedFlowGraph();
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
// Return Values:
//      None.
//
// Operation:
//      Create the following structure.
//
//      Note below that the cond0 is inverted in head i.e., if true jump to cond1. This is because
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
//
BasicBlock* Compiler::optInsertLoopChoiceConditions(LoopCloneContext* context,
                                                    unsigned          loopNum,
                                                    BasicBlock*       head,
                                                    BasicBlock*       slowHead)
{
    JITDUMP("Inserting loop cloning conditions\n");
    assert(context->HasBlockConditions(loopNum));

    BasicBlock*                                        curCond   = head;
    ExpandArrayStack<ExpandArrayStack<LC_Condition>*>* levelCond = context->GetBlockConditions(loopNum);
    for (unsigned i = 0; i < levelCond->Size(); ++i)
    {
        bool isHeaderBlock = (curCond == head);

        // Flip the condition if header block.
        context->CondToStmtInBlock(this, *((*levelCond)[i]), curCond, isHeaderBlock);

        // Create each condition block ensuring wiring between them.
        BasicBlock* tmp     = fgNewBBafter(BBJ_COND, isHeaderBlock ? slowHead : curCond, true);
        curCond->bbJumpDest = isHeaderBlock ? tmp : slowHead;
        curCond             = tmp;

        curCond->inheritWeight(head);
        curCond->bbNatLoopNum = head->bbNatLoopNum;
        JITDUMP("Created new block %02d for new level\n", curCond->bbNum);
    }

    // Finally insert cloning conditions after all deref conditions have been inserted.
    context->CondToStmtInBlock(this, *(context->GetConditions(loopNum)), curCond, false);
    return curCond;
}

void Compiler::optEnsureUniqueHead(unsigned loopInd, unsigned ambientWeight)
{
    BasicBlock* h = optLoopTable[loopInd].lpHead;
    BasicBlock* t = optLoopTable[loopInd].lpTop;
    BasicBlock* e = optLoopTable[loopInd].lpEntry;
    BasicBlock* b = optLoopTable[loopInd].lpBottom;

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
    h2->bbNatLoopNum = optLoopTable[loopInd].lpParent;
    h2->bbWeight     = (h2->isRunRarely() ? 0 : ambientWeight);

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
    BlockToBlockMap* blockMap = new (getAllocator()) BlockToBlockMap(getAllocator());
    blockMap->Set(e, h2);

    for (flowList* predEntry = e->bbPreds; predEntry; predEntry = predEntry->flNext)
    {
        BasicBlock* predBlock = predEntry->flBlock;

        // Skip if predBlock is in the loop.
        if (t->bbNum <= predBlock->bbNum && predBlock->bbNum <= b->bbNum)
        {
            continue;
        }
        optRedirectBlock(predBlock, blockMap);
    }

    optUpdateLoopHead(loopInd, optLoopTable[loopInd].lpHead, h2);
}

/*****************************************************************************
 *
 *  Determine the kind of interference for the call.
 */

/* static */ inline Compiler::callInterf Compiler::optCallInterf(GenTreePtr call)
{
    assert(call->gtOper == GT_CALL);

    // if not a helper, kills everything
    if (call->gtCall.gtCallType != CT_HELPER)
    {
        return CALLINT_ALL;
    }

    // setfield and array address store kill all indirections
    switch (eeGetHelperNum(call->gtCall.gtCallMethHnd))
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

        case CORINFO_HELP_ASSIGN_STRUCT: // Not strictly needed as we don't use this in Jit32
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

bool Compiler::optNarrowTree(GenTreePtr tree, var_types srct, var_types dstt, ValueNumPair vnpNarrow, bool doit)
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

    if (kind & GTK_ASGOP)
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

#ifndef _TARGET_64BIT_
            __int64 lval;
            __int64 lmask;

            case GT_CNS_LNG:
                lval  = tree->gtIntConCommon.LngValue();
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
                    case TYP_CHAR:
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
                    tree->gtType             = TYP_INT;
                    tree->gtIntCon.gtIconVal = (int)lval;
                    if (vnStore != nullptr)
                    {
                        fgValueNumberTreeConst(tree);
                    }
                }

                return true;
#endif

            case GT_CNS_INT:

                ssize_t ival;
                ival = tree->gtIntCon.gtIconVal;
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
                    case TYP_CHAR:
                        imask = 0x0000FFFF;
                        break;
#ifdef _TARGET_64BIT_
                    case TYP_INT:
                        imask = 0x7FFFFFFF;
                        break;
                    case TYP_UINT:
                        imask = 0xFFFFFFFF;
                        break;
#endif // _TARGET_64BIT_
                    default:
                        return false;
                }

                if ((ival & imask) != ival)
                {
                    return false;
                }

#ifdef _TARGET_64BIT_
                if (doit)
                {
                    tree->gtType             = TYP_INT;
                    tree->gtIntCon.gtIconVal = (int)ival;
                    if (vnStore != nullptr)
                    {
                        fgValueNumberTreeConst(tree);
                    }
                }
#endif // _TARGET_64BIT_

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
        GenTreePtr op1;
        op1 = tree->gtOp.gtOp1;
        GenTreePtr op2;
        op2 = tree->gtOp.gtOp2;

        switch (tree->gtOper)
        {
            case GT_AND:
                noway_assert(genActualType(tree->gtType) == genActualType(op2->gtType));

                // Is op2 a small constant than can be narrowed into dstt?
                // if so the result of the GT_AND will also fit into 'dstt' and can be narrowed
                if ((op2->gtOper == GT_CNS_INT) && optNarrowTree(op2, srct, dstt, NoVNPair, false))
                {
                    // We will change the type of the tree and narrow op2
                    //
                    if (doit)
                    {
                        tree->gtType = genActualType(dstt);
                        tree->SetVNs(vnpNarrow);

                        optNarrowTree(op2, srct, dstt, NoVNPair, true);
                        // We may also need to cast away the upper bits of op1
                        if (srcSize == 8)
                        {
                            assert(tree->gtType == TYP_INT);
                            op1 = gtNewCastNode(TYP_INT, op1, TYP_INT);
#ifdef DEBUG
                            op1->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
#endif
                            tree->gtOp.gtOp1 = op1;
                        }
                    }
                    return true;
                }

                goto COMMON_BINOP;

            case GT_ADD:
            case GT_MUL:

                if (tree->gtOverflow() || varTypeIsSmall(dstt))
                {
                    noway_assert(doit == false);
                    return false;
                }
                __fallthrough;

            case GT_OR:
            case GT_XOR:
            COMMON_BINOP:
                noway_assert(genActualType(tree->gtType) == genActualType(op1->gtType));
                noway_assert(genActualType(tree->gtType) == genActualType(op2->gtType));

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

                        if (oprSize == dstSize)
                        {
                            // Same size: change the CAST into a NOP
                            tree->ChangeOper(GT_NOP);
                            tree->gtType     = dstt;
                            tree->gtOp.gtOp2 = nullptr;
                            tree->gtVNPair   = op1->gtVNPair; // Set to op1's ValueNumber
                        }
                        else
                        {
                            // oprSize is smaller
                            assert(oprSize < dstSize);

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

Compiler::fgWalkResult Compiler::optIsVarAssgCB(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    if (tree->OperKind() & GTK_ASGOP)
    {
        GenTreePtr dest     = tree->gtOp.gtOp1;
        genTreeOps destOper = dest->OperGet();

        isVarAssgDsc* desc = (isVarAssgDsc*)data->pCallbackData;
        assert(desc && desc->ivaSelf == desc);

        if (destOper == GT_LCL_VAR)
        {
            unsigned tvar = dest->gtLclVarCommon.gtLclNum;
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

            // unsigned    lclNum = dest->gtLclFld.gtLclNum;
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

        desc->ivaMaskCall = optCallInterf(tree);
    }

    return WALK_CONTINUE;
}

/*****************************************************************************/

bool Compiler::optIsVarAssigned(BasicBlock* beg, BasicBlock* end, GenTreePtr skip, unsigned var)
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
        noway_assert(beg);

        for (GenTreeStmt* stmt = beg->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            noway_assert(stmt->gtOper == GT_STMT);
            if (fgWalkTreePre(&stmt->gtStmtExpr, optIsVarAssgCB, &desc))
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

            for (GenTreeStmt* stmt = beg->FirstNonPhiDef(); stmt; stmt = stmt->gtNextStmt)
            {
                noway_assert(stmt->gtOper == GT_STMT);
                fgWalkTreePre(&stmt->gtStmtExpr, optIsVarAssgCB, &desc);

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

void Compiler::optPerformHoistExpr(GenTreePtr origExpr, unsigned lnum)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nHoisting a copy of ");
        printTreeID(origExpr);
        printf(" into PreHeader for loop L%02u <BB%02u..BB%02u>:\n", lnum, optLoopTable[lnum].lpFirst->bbNum,
               optLoopTable[lnum].lpBottom->bbNum);
        gtDispTree(origExpr);
        printf("\n");
    }
#endif

    // This loop has to be in a form that is approved for hoisting.
    assert(optLoopTable[lnum].lpFlags & LPFLG_HOISTABLE);

    // Create a copy of the expression and mark it for CSE's.
    GenTreePtr hoistExpr = gtCloneExpr(origExpr, GTF_MAKE_CSE);

    // At this point we should have a cloned expression, marked with the GTF_MAKE_CSE flag
    assert(hoistExpr != origExpr);
    assert(hoistExpr->gtFlags & GTF_MAKE_CSE);

    GenTreePtr hoist = hoistExpr;
    // The value of the expression isn't used (unless it's an assignment).
    if (hoistExpr->OperGet() != GT_ASG)
    {
        hoist = gtUnusedValNode(hoistExpr);
    }

    /* Put the statement in the preheader */

    fgCreateLoopPreHeader(lnum);

    BasicBlock* preHead = optLoopTable[lnum].lpHead;
    assert(preHead->bbJumpKind == BBJ_NONE);

    // fgMorphTree and lvaRecursiveIncRefCounts requires that compCurBB be the block that contains
    // (or in this case, will contain) the expression.
    compCurBB = preHead;

    // Increment the ref counts of any local vars appearing in "hoist".
    // Note that we need to do this before fgMorphTree() as fgMorph() could constant
    // fold away some of the lcl vars referenced by "hoist".
    lvaRecursiveIncRefCounts(hoist);

    hoist = fgMorphTree(hoist);

    GenTreePtr hoistStmt = gtNewStmt(hoist);
    hoistStmt->gtFlags |= GTF_STMT_CMPADD;

    /* simply append the statement at the end of the preHead's list */

    GenTreePtr treeList = preHead->bbTreeList;

    if (treeList)
    {
        /* append after last statement */

        GenTreePtr last = treeList->gtPrev;
        assert(last->gtNext == nullptr);

        last->gtNext      = hoistStmt;
        hoistStmt->gtPrev = last;
        treeList->gtPrev  = hoistStmt;
    }
    else
    {
        /* Empty pre-header - store the single statement in the block */

        preHead->bbTreeList = hoistStmt;
        hoistStmt->gtPrev   = hoistStmt;
    }

    hoistStmt->gtNext = nullptr;

#ifdef DEBUG
    if (verbose)
    {
        printf("This hoisted copy placed in PreHeader (BB%02u):\n", preHead->bbNum);
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
        GenTreePtr      node = ki.Get();
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
    BasicBlock* block;

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
        printf("optHoistLoopCode for loop L%02u <BB%02u..BB%02u>:\n", lnum, begn, endn);
        printf("  Loop body %s a call\n", pLoopDsc->lpContainsCall ? "contains" : "does not contain");
    }
#endif

    VARSET_TP VARSET_INIT_NOCOPY(loopVars, VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, pLoopDsc->lpVarUseDef));

    pLoopDsc->lpVarInOutCount    = VarSetOps::Count(this, pLoopDsc->lpVarInOut);
    pLoopDsc->lpLoopVarCount     = VarSetOps::Count(this, loopVars);
    pLoopDsc->lpHoistedExprCount = 0;

#ifndef _TARGET_64BIT_
    unsigned longVarsCount = VarSetOps::Count(this, lvaLongVars);

    if (longVarsCount > 0)
    {
        // Since 64-bit variables take up two registers on 32-bit targets, we increase
        //  the Counts such that each TYP_LONG variable counts twice.
        //
        VARSET_TP VARSET_INIT_NOCOPY(loopLongVars, VarSetOps::Intersection(this, loopVars, lvaLongVars));
        VARSET_TP VARSET_INIT_NOCOPY(inOutLongVars, VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, lvaLongVars));

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
#endif // !_TARGET_64BIT_

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
        VARSET_TP VARSET_INIT_NOCOPY(loopFPVars, VarSetOps::Intersection(this, loopVars, lvaFloatVars));
        VARSET_TP VARSET_INIT_NOCOPY(inOutFPVars, VarSetOps::Intersection(this, pLoopDsc->lpVarInOut, lvaFloatVars));

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
    ExpandArrayStack<BasicBlock*> defExec(getAllocatorLoopHoist());
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

    while (defExec.Size() > 0)
    {
        // Consider in reverse order: dominator before dominatee.
        BasicBlock* blk = defExec.Pop();
        optHoistLoopExprsForBlock(blk, lnum, hoistCtxt);
    }
}

// Hoist any expressions in "blk" that are invariant in loop "lnum" outside of "blk" and into a PreHead for loop "lnum".
void Compiler::optHoistLoopExprsForBlock(BasicBlock* blk, unsigned lnum, LoopHoistContext* hoistCtxt)
{
    LoopDsc* pLoopDsc                      = &optLoopTable[lnum];
    bool     firstBlockAndBeforeSideEffect = (blk == pLoopDsc->lpEntry);
    unsigned blkWeight                     = blk->getBBWeight(this);

#ifdef DEBUG
    if (verbose)
    {
        printf("    optHoistLoopExprsForBlock BB%02u (weight=%6s) of loop L%02u <BB%02u..BB%02u>, firstBlock is %s\n",
               blk->bbNum, refCntWtd2str(blkWeight), lnum, pLoopDsc->lpFirst->bbNum, pLoopDsc->lpBottom->bbNum,
               firstBlockAndBeforeSideEffect ? "true" : "false");
        if (blkWeight < (BB_UNITY_WEIGHT / 10))
        {
            printf("      block weight is too small to perform hoisting.\n");
        }
    }
#endif

    if (blkWeight < (BB_UNITY_WEIGHT / 10))
    {
        // Block weight is too small to perform hoisting.
        return;
    }

    for (GenTreeStmt* stmt = blk->FirstNonPhiDef(); stmt; stmt = stmt->gtNextStmt)
    {
        GenTreePtr stmtTree = stmt->gtStmtExpr;
        bool       hoistable;
        (void)optHoistLoopExprsForTree(stmtTree, lnum, hoistCtxt, &firstBlockAndBeforeSideEffect, &hoistable);
        if (hoistable)
        {
            // we will try to hoist the top-level stmtTree
            optHoistCandidate(stmtTree, lnum, hoistCtxt);
        }
    }
}

bool Compiler::optIsProfitableToHoistableTree(GenTreePtr tree, unsigned lnum)
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
#ifdef _TARGET_ARM_
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
#ifndef _TARGET_64BIT_
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
    // to place it in a stack home location (gtCostEx >= 2*IND_COST_EX)
    // as we believe it will be placed in the stack or one of the other
    // loopVars will be spilled into the stack
    //
    if (loopVarCount >= availRegCount)
    {
        // Don't hoist expressions that are not heavy: tree->gtCostEx < (2*IND_COST_EX)
        if (tree->gtCostEx < (2 * IND_COST_EX))
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
    // So we are willing hoist an expression with gtCostEx == MIN_CSE_COST
    //
    if (varInOutCount > availRegCount)
    {
        // Don't hoist expressions that barely meet CSE cost requirements: tree->gtCostEx == MIN_CSE_COST
        if (tree->gtCostEx <= MIN_CSE_COST + 1)
        {
            return false;
        }
    }

    return true;
}

//
//  This function returns true if 'tree' is a loop invariant expression.
//  It also sets '*pHoistable' to true if 'tree' can be hoisted into a loop PreHeader block
//
bool Compiler::optHoistLoopExprsForTree(
    GenTreePtr tree, unsigned lnum, LoopHoistContext* hoistCtxt, bool* pFirstBlockAndBeforeSideEffect, bool* pHoistable)
{
    // First do the children.
    // We must keep track of whether each child node was hoistable or not
    //
    unsigned nChildren = tree->NumChildren();
    bool     childrenHoistable[GenTree::MAX_CHILDREN];

    // Initialize the array elements for childrenHoistable[] to false
    for (unsigned i = 0; i < nChildren; i++)
    {
        childrenHoistable[i] = false;
    }

    bool treeIsInvariant = true;
    for (unsigned childNum = 0; childNum < nChildren; childNum++)
    {
        if (!optHoistLoopExprsForTree(tree->GetChild(childNum), lnum, hoistCtxt, pFirstBlockAndBeforeSideEffect,
                                      &childrenHoistable[childNum]))
        {
            treeIsInvariant = false;
        }
    }

    // If all the children of "tree" are hoistable, then "tree" itself can be hoisted
    //
    bool treeIsHoistable = treeIsInvariant;

    // But we must see if anything else prevents "tree" from being hoisted.
    //
    if (treeIsInvariant)
    {
        // Tree must be a suitable CSE candidate for us to be able to hoist it.
        treeIsHoistable = optIsCSEcandidate(tree);

        // If it's a call, it must be a helper call, and be pure.
        // Further, if it may run a cctor, it must be labeled as "Hoistable"
        // (meaning it won't run a cctor because the class is not precise-init).
        if (treeIsHoistable && tree->OperGet() == GT_CALL)
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
                else if (s_helperCallProperties.MayRunCctor(helpFunc) && (call->gtFlags & GTF_CALL_HOISTABLE) == 0)
                {
                    treeIsHoistable = false;
                }
            }
        }

        if (treeIsHoistable)
        {
            if (!(*pFirstBlockAndBeforeSideEffect))
            {
                // For now, we give up on an expression that might raise an exception if it is after the
                // first possible global side effect (and we assume we're after that if we're not in the first block).
                // TODO-CQ: this is when we might do loop cloning.
                //
                if ((tree->gtFlags & GTF_EXCEPT) != 0)
                {
                    treeIsHoistable = false;
                }
            }
            // Currently we must give up on reads from static variables (even if we are in the first block).
            //
            if (tree->OperGet() == GT_CLS_VAR)
            {
                // TODO-CQ: test that fails if we hoist GT_CLS_VAR: JIT\Directed\Languages\ComponentPascal\pi_r.exe
                // method Main
                treeIsHoistable = false;
            }
        }

        // Is the value of the whole tree loop invariant?
        treeIsInvariant =
            optVNIsLoopInvariant(tree->gtVNPair.GetLiberal(), lnum, &hoistCtxt->m_curLoopVnInvariantCache);

        // Is the value of the whole tree loop invariant?
        if (!treeIsInvariant)
        {
            treeIsHoistable = false;
        }
    }

    // Check if we need to set '*pFirstBlockAndBeforeSideEffect' to false.
    // If we encounter a tree with a call in it
    //  or if we see an assignment to global we set it to false.
    //
    // If we are already set to false then we can skip these checks
    //
    if (*pFirstBlockAndBeforeSideEffect)
    {
        // For this purpose, we only care about memory side effects.  We assume that expressions will
        // be hoisted so that they are evaluated in the same order as they would have been in the loop,
        // and therefore throw exceptions in the same order.  (So we don't use GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS
        // here, since that includes exceptions.)
        if (tree->gtFlags & GTF_CALL)
        {
            *pFirstBlockAndBeforeSideEffect = false;
        }
        else if (tree->OperIsAssignment())
        {
            // If the LHS of the assignment has a global reference, then assume it's a global side effect.
            GenTreePtr lhs = tree->gtOp.gtOp1;
            if (lhs->gtFlags & GTF_GLOB_REF)
            {
                *pFirstBlockAndBeforeSideEffect = false;
            }
        }
        else if (tree->OperIsCopyBlkOp())
        {
            GenTreePtr args = tree->gtOp.gtOp1;
            assert(args->OperGet() == GT_LIST);
            if (args->gtOp.gtOp1->gtFlags & GTF_GLOB_REF)
            {
                *pFirstBlockAndBeforeSideEffect = false;
            }
        }
    }

    // If this 'tree' is hoistable then we return and the caller will
    // decide to hoist it as part of larger hoistable expression.
    //
    if (!treeIsHoistable)
    {
        // We are not hoistable so we will now hoist any hoistable children.
        //
        for (unsigned childNum = 0; childNum < nChildren; childNum++)
        {
            if (childrenHoistable[childNum])
            {
                // We can't hoist the LHS of an assignment, isn't a real use.
                if (childNum == 0 && (tree->OperIsAssignment()))
                {
                    continue;
                }

                GenTreePtr child = tree->GetChild(childNum);

                // We try to hoist this 'child' tree
                optHoistCandidate(child, lnum, hoistCtxt);
            }
        }
    }

    *pHoistable = treeIsHoistable;
    return treeIsInvariant;
}

void Compiler::optHoistCandidate(GenTreePtr tree, unsigned lnum, LoopHoistContext* hoistCtxt)
{
    if (lnum == BasicBlock::NOT_IN_LOOP)
    {
        // The hoisted expression isn't valid at any loop head so don't hoist this expression.
        return;
    }

    // The outer loop also must be suitable for hoisting...
    if ((optLoopTable[lnum].lpFlags & LPFLG_HOISTABLE) == 0)
    {
        return;
    }

    // If the hoisted expression isn't valid at this loop head then break
    if (!optTreeIsValidAtLoopHead(tree, lnum))
    {
        return;
    }

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
#ifndef _TARGET_64BIT_
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
            // First, make sure it's a "proper" phi -- the definition is a Phi application.
            VNFuncApp phiDefValFuncApp;
            if (!vnStore->GetVNFunc(funcApp.m_args[2], &phiDefValFuncApp) || phiDefValFuncApp.m_func != VNF_Phi)
            {
                // It's not *really* a definition, rather a pass-through of some other VN.
                // (This could occur, say if both sides of an if-then-else diamond made the
                // same assignment to a variable.)
                res = optVNIsLoopInvariant(funcApp.m_args[2], lnum, loopVnInvariantCache);
            }
            else
            {
                // Is the definition within the loop?  If so, is not loop-invariant.
                unsigned      lclNum = funcApp.m_args[0];
                unsigned      ssaNum = funcApp.m_args[1];
                LclSsaVarDsc* ssaDef = lvaTable[lclNum].GetPerSsaData(ssaNum);
                res                  = !optLoopContains(lnum, ssaDef->m_defLoc.m_blk->bbNatLoopNum);
            }
        }
        else if (funcApp.m_func == VNF_PhiHeapDef)
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

bool Compiler::optTreeIsValidAtLoopHead(GenTreePtr tree, unsigned lnum)
{
    if (tree->OperIsLocal())
    {
        GenTreeLclVarCommon* lclVar = tree->AsLclVarCommon();
        unsigned             lclNum = lclVar->gtLclNum;

        // The lvlVar must be have an Ssa tracked lifetime
        if (fgExcludeFromSsa(lclNum))
        {
            return false;
        }

        // If the loop does not contains the SSA def we can hoist it.
        if (!optLoopTable[lnum].lpContains(lvaTable[lclNum].GetPerSsaData(lclVar->GetSsaNum())->m_defLoc.m_blk))
        {
            return true;
        }
    }
    else if (tree->OperIsConst())
    {
        return true;
    }
    else // If every one of the children nodes are valid at this Loop's Head.
    {
        unsigned nChildren = tree->NumChildren();
        for (unsigned childNum = 0; childNum < nChildren; childNum++)
        {
            if (!optTreeIsValidAtLoopHead(tree->GetChild(childNum), lnum))
            {
                return false;
            }
        }
        return true;
    }
    return false;
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

#ifdef DEBUG
    if (verbose)
    {
        printf("\nCreated PreHeader (BB%02u) for loop L%02u (BB%02u - BB%02u), with weight = %s\n", preHead->bbNum,
               lnum, top->bbNum, pLoopDsc->lpBottom->bbNum, refCntWtd2str(preHead->getBBWeight(this)));
    }
#endif

    // The preheader block is part of the containing loop (if any).
    preHead->bbNatLoopNum = pLoopDsc->lpParent;

    if (fgIsUsingProfileWeights() && (head->bbJumpKind == BBJ_COND))
    {
        if ((head->bbWeight == 0) || (head->bbNext->bbWeight == 0))
        {
            preHead->bbWeight = 0;
            preHead->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            bool allValidProfileWeights = ((head->bbFlags & BBF_PROF_WEIGHT) != 0) &&
                                          ((head->bbJumpDest->bbFlags & BBF_PROF_WEIGHT) != 0) &&
                                          ((head->bbNext->bbFlags & BBF_PROF_WEIGHT) != 0);

            if (allValidProfileWeights)
            {
                double loopEnteredCount;
                double loopSkippedCount;

                if (fgHaveValidEdgeWeights)
                {
                    flowList* edgeToNext = fgGetPredForBlock(head->bbNext, head);
                    flowList* edgeToJump = fgGetPredForBlock(head->bbJumpDest, head);
                    noway_assert(edgeToNext != nullptr);
                    noway_assert(edgeToJump != nullptr);

                    loopEnteredCount =
                        ((double)edgeToNext->flEdgeWeightMin + (double)edgeToNext->flEdgeWeightMax) / 2.0;
                    loopSkippedCount =
                        ((double)edgeToJump->flEdgeWeightMin + (double)edgeToJump->flEdgeWeightMax) / 2.0;
                }
                else
                {
                    loopEnteredCount = (double)head->bbNext->bbWeight;
                    loopSkippedCount = (double)head->bbJumpDest->bbWeight;
                }

                double loopTakenRatio = loopEnteredCount / (loopEnteredCount + loopSkippedCount);

                // Calculate a good approximation of the preHead's block weight
                unsigned preHeadWeight = (unsigned)(((double)head->bbWeight * loopTakenRatio) + 0.5);
                preHead->setBBWeight(max(preHeadWeight, 1));
                noway_assert(!preHead->isRunRarely());
            }
        }
    }

    // Link in the preHead block.
    fgInsertBBbefore(top, preHead);

    // Ideally we would re-run SSA and VN if we optimized by doing loop hoisting.
    // However, that is too expensive at this point. Instead, we update the phi
    // node block references, if we created pre-header block due to hoisting.
    // This is sufficient because any definition participating in SSA that flowed
    // into the phi via the loop header block will now flow through the preheader
    // block from the header block.

    for (GenTreePtr stmt = top->bbTreeList; stmt; stmt = stmt->gtNext)
    {
        GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
        if (tree->OperGet() != GT_ASG)
        {
            break;
        }
        GenTreePtr op2 = tree->gtGetOp2();
        if (op2->OperGet() != GT_PHI)
        {
            break;
        }
        GenTreeArgList* args = op2->gtGetOp1()->AsArgList();
        while (args != nullptr)
        {
            GenTreePhiArg* phiArg = args->Current()->AsPhiArg();
            if (phiArg->gtPredBB == head)
            {
                phiArg->gtPredBB = preHead;
            }
            args = args->Rest();
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

    preHead->bbRefs = 0;
    fgAddRefPred(preHead, head);
    bool checkNestedLoops = false;

    for (flowList* pred = top->bbPreds; pred; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->flBlock;

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
                __fallthrough;

            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                noway_assert(predBlock->bbJumpDest == top);
                predBlock->bbJumpDest = preHead;
                preHead->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

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
                        preHead->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
                    }
                } while (++jumpTab, --jumpCnt);

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    noway_assert(!fgGetPredForBlock(top, preHead));
    fgRemoveRefPred(top, head);
    fgAddRefPred(top, preHead);

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
                    printf("Same PreHeader (BB%02u) can be used for loop L%02u (BB%02u - BB%02u)\n\n", preHead->bbNum,
                           l, top->bbNum, optLoopTable[l].lpBottom->bbNum);
                }
#endif
            }
        }
    }
}

bool Compiler::optBlockIsLoopEntry(BasicBlock* blk, unsigned* pLnum)
{
    unsigned lnum = blk->bbNatLoopNum;
    while (lnum != BasicBlock::NOT_IN_LOOP)
    {
        if (optLoopTable[lnum].lpEntry == blk)
        {
            *pLnum = lnum;
            return true;
        }
        lnum = optLoopTable[lnum].lpParent;
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
#ifndef _TARGET_64BIT_
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
#ifndef _TARGET_64BIT_
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
    for (BasicBlock* bbInLoop = optLoopTable[lnum].lpFirst; bbInLoop != botNext; bbInLoop = bbInLoop->bbNext)
    {
        optComputeLoopSideEffectsOfBlock(bbInLoop);
    }
}

void Compiler::optComputeLoopSideEffectsOfBlock(BasicBlock* blk)
{
    unsigned mostNestedLoop = blk->bbNatLoopNum;
    assert(mostNestedLoop != BasicBlock::NOT_IN_LOOP);

    AddVariableLivenessAllContainingLoops(mostNestedLoop, blk);

    bool heapHavoc = false; // True ==> there's a call or a memory store that has arbitrary heap effects.

    // Now iterate over the remaining statements, and their trees.
    for (GenTreePtr stmts = blk->FirstNonPhiDef(); (stmts != nullptr); stmts = stmts->gtNext)
    {
        for (GenTreePtr tree = stmts->gtStmt.gtStmtList; (tree != nullptr); tree = tree->gtNext)
        {
            genTreeOps oper = tree->OperGet();

            // Even after we set heapHavoc we still may want to know if a loop contains calls
            if (heapHavoc)
            {
                if (oper == GT_CALL)
                {
                    // Record that this loop contains a call
                    AddContainsCallAllContainingLoops(mostNestedLoop);
                }

                // If we just set lpContainsCall or it was previously set
                if (optLoopTable[mostNestedLoop].lpContainsCall)
                {
                    // We can early exit after both heapHavoc and lpContainsCall are both set to true.
                    break;
                }

                // We are just looking for GT_CALL nodes after heapHavoc was set.
                continue;
            }

            // otherwise heapHavoc is not set
            assert(!heapHavoc);

            // This body is a distillation of the heap-side effect code of value numbering.
            // We also do a very limited analysis if byref PtrTo values, to cover some cases
            // that the compiler creates.

            if (GenTree::OperIsAssignment(oper))
            {
                GenTreePtr lhs = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);

                if (lhs->OperGet() == GT_IND)
                {
                    GenTreePtr    arg           = lhs->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
                    FieldSeqNode* fldSeqArrElem = nullptr;

                    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
                    {
                        heapHavoc = true;
                        continue;
                    }

                    ArrayInfo arrInfo;

                    if (arg->TypeGet() == TYP_BYREF && arg->OperGet() == GT_LCL_VAR)
                    {
                        // If it's a local byref for which we recorded a value number, use that...
                        GenTreeLclVar* argLcl = arg->AsLclVar();
                        if (!fgExcludeFromSsa(argLcl->GetLclNum()))
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
                                // Don't set heapHavoc below.
                                continue;
                            }
                        }
                        // Otherwise...
                        heapHavoc = true;
                    }
                    // Is the LHS an array index expression?
                    else if (lhs->ParseArrayElemForm(this, &arrInfo, &fldSeqArrElem))
                    {
                        // We actually ignore "fldSeq" -- any modification to an S[], at any
                        // field of "S", will lose all information about the array type.
                        CORINFO_CLASS_HANDLE elemTypeEq = EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                        AddModifiedElemTypeAllContainingLoops(mostNestedLoop, elemTypeEq);
                    }
                    else
                    {
                        // We are only interested in IsFieldAddr()'s fldSeq out parameter.
                        //
                        GenTreePtr    obj          = nullptr; // unused
                        GenTreePtr    staticOffset = nullptr; // unused
                        FieldSeqNode* fldSeq       = nullptr;

                        if (arg->IsFieldAddr(this, &obj, &staticOffset, &fldSeq) &&
                            (fldSeq != FieldSeqStore::NotAField()))
                        {
                            // Get the first (object) field from field seq.  Heap[field] will yield the "field map".
                            assert(fldSeq != nullptr);
                            if (fldSeq->IsFirstElemFieldSeq())
                            {
                                fldSeq = fldSeq->m_next;
                                assert(fldSeq != nullptr);
                            }

                            AddModifiedFieldAllContainingLoops(mostNestedLoop, fldSeq->m_fieldHnd);
                        }
                        else
                        {
                            heapHavoc = true;
                        }
                    }
                }
                else if (lhs->OperGet() == GT_CLS_VAR)
                {
                    AddModifiedFieldAllContainingLoops(mostNestedLoop, lhs->gtClsVar.gtClsVarHnd);
                }
                // Otherwise, must be local lhs form.  I should assert that.
                else if (lhs->OperGet() == GT_LCL_VAR)
                {
                    GenTreeLclVar* lhsLcl = lhs->AsLclVar();
                    GenTreePtr     rhs    = tree->gtOp.gtOp2;
                    ValueNum       rhsVN  = rhs->gtVNPair.GetLiberal();
                    // If we gave the RHS a value number, propagate it.
                    if (rhsVN != ValueNumStore::NoVN)
                    {
                        rhsVN = vnStore->VNNormVal(rhsVN);
                        if (!fgExcludeFromSsa(lhsLcl->GetLclNum()))
                        {
                            lvaTable[lhsLcl->GetLclNum()]
                                .GetPerSsaData(lhsLcl->GetSsaNum())
                                ->m_vnPair.SetLiberal(rhsVN);
                        }
                    }
                }
            }
            else // not GenTree::OperIsAssignment(oper)
            {
                switch (oper)
                {
                    case GT_COMMA:
                        tree->gtVNPair = tree->gtOp.gtOp2->gtVNPair;
                        break;

                    case GT_ADDR:
                        // Is it an addr of a array index expression?
                        {
                            GenTreePtr addrArg = tree->gtOp.gtOp1;
                            if (addrArg->OperGet() == GT_IND)
                            {
                                // Is the LHS an array index expression?
                                if (addrArg->gtFlags & GTF_IND_ARR_INDEX)
                                {
                                    ArrayInfo arrInfo;
                                    bool      b = GetArrayInfoMap()->Lookup(addrArg, &arrInfo);
                                    assert(b);
                                    CORINFO_CLASS_HANDLE elemType =
                                        EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                                    tree->gtVNPair.SetBoth(
                                        vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem,
                                                           vnStore->VNForHandle(ssize_t(elemType), GTF_ICON_CLASS_HDL),
                                                           // The rest are dummy arguments.
                                                           vnStore->VNForNull(), vnStore->VNForNull(),
                                                           vnStore->VNForNull()));
                                }
                            }
                        }
                        break;

                    case GT_INITBLK:
                    case GT_COPYBLK:
                    case GT_COPYOBJ:
                    {
                        GenTreeLclVarCommon* lclVarTree;
                        bool                 isEntire;
                        if (!tree->DefinesLocal(this, &lclVarTree, &isEntire))
                        {
                            // For now, assume arbitrary side effects on the heap...
                            // TODO-CQ: Why not be complete, and get this case right?
                            heapHavoc = true;
                        }
                    }
                    break;

                    case GT_LOCKADD: // Binop
                    case GT_XADD:    // Binop
                    case GT_XCHG:    // Binop
                    case GT_CMPXCHG: // Specialop
                    {
                        heapHavoc = true;
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
                                heapHavoc = true;
                            }
                            else if (s_helperCallProperties.MayRunCctor(helpFunc))
                            {
                                // If the call is labeled as "Hoistable", then we've checked the
                                // class that would be constructed, and it is not precise-init, so
                                // the cctor will not be run by this call.  Otherwise, it might be,
                                // and might have arbitrary side effects.
                                if ((tree->gtFlags & GTF_CALL_HOISTABLE) == 0)
                                {
                                    heapHavoc = true;
                                }
                            }
                        }
                        else
                        {
                            heapHavoc = true;
                        }
                        break;
                    }

                    default:
                        // All other gtOper node kinds, leave 'heapHavoc' unchanged (i.e. false)
                        break;
                }
            }
        }
    }

    if (heapHavoc)
    {
        // Record that all loops containing this block have heap havoc effects.
        unsigned lnum = mostNestedLoop;
        while (lnum != BasicBlock::NOT_IN_LOOP)
        {
            optLoopTable[lnum].lpLoopHasHeapHavoc = true;
            lnum                                  = optLoopTable[lnum].lpParent;
        }
    }
}

// Marks the containsCall information to "lnum" and any parent loops.
void Compiler::AddContainsCallAllContainingLoops(unsigned lnum)
{
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

/*****************************************************************************
 *
 *  Helper passed to Compiler::fgWalkAllTreesPre() to decrement the LclVar usage counts
 *  The 'keepList'is either a single tree or a list of trees that are formed by
 *  one or more GT_COMMA nodes.  It is the kept side-effects as returned by the
 *  gtExtractSideEffList method.
 */

/* static */
Compiler::fgWalkResult Compiler::optRemoveTreeVisitor(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree     = *pTree;
    Compiler*  comp     = data->compiler;
    GenTreePtr keepList = (GenTreePtr)(data->pCallbackData);

    // We may have a non-NULL side effect list that is being kept
    //
    if (keepList)
    {
        GenTreePtr keptTree = keepList;
        while (keptTree->OperGet() == GT_COMMA)
        {
            assert(keptTree->OperKind() & GTK_SMPOP);
            GenTreePtr op1 = keptTree->gtOp.gtOp1;
            GenTreePtr op2 = keptTree->gtGetOp2();

            // For the GT_COMMA case the op1 is part of the orginal CSE tree
            // that is being kept because it contains some side-effect
            //
            if (tree == op1)
            {
                // This tree and all of its sub trees are being kept.
                return WALK_SKIP_SUBTREES;
            }

            // For the GT_COMMA case the op2 are the remaining side-effects of the orginal CSE tree
            // which can again be another GT_COMMA or the final side-effect part
            //
            keptTree = op2;
        }
        if (tree == keptTree)
        {
            // This tree and all of its sub trees are being kept.
            return WALK_SKIP_SUBTREES;
        }
    }

    // This node is being removed from the graph of GenTreePtr

    // Look for any local variable references

    if (tree->gtOper == GT_LCL_VAR && comp->lvaLocalVarRefCounted)
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        /* This variable ref is going away, decrease its ref counts */

        lclNum = tree->gtLclVarCommon.gtLclNum;
        assert(lclNum < comp->lvaCount);
        varDsc = comp->lvaTable + lclNum;

        // make sure it's been initialized
        assert(comp->compCurBB != nullptr);
        assert(comp->compCurBB->bbWeight <= BB_MAX_WEIGHT);

        /* Decrement its lvRefCnt and lvRefCntWtd */

        // Use getBBWeight to determine the proper block weight.
        // This impacts the block weights when we have IBC data.
        varDsc->decRefCnts(comp->compCurBB->getBBWeight(comp), comp);
    }

    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Routine called to decrement the LclVar ref counts when removing a tree
 *  during the remove RangeCheck phase.
 *  This method will decrement the refcounts for any LclVars used below 'deadTree',
 *  unless the node is found in the 'keepList' (which are saved side effects)
 *  The keepList is communicated using the walkData.pCallbackData field
 *  Also the compCurBB must be set to the current BasicBlock  which contains
 *  'deadTree' as we need to fetch the block weight when decrementing the ref counts.
 */

void Compiler::optRemoveTree(GenTreePtr deadTree, GenTreePtr keepList)
{
    // We communicate this value using the walkData.pCallbackData field
    //
    fgWalkTreePre(&deadTree, optRemoveTreeVisitor, (void*)keepList);
}

/*****************************************************************************
 *
 *  Given an array index node, mark it as not needing a range check.
 */

void Compiler::optRemoveRangeCheck(
    GenTreePtr tree, GenTreePtr stmt, bool updateCSEcounts, unsigned sideEffFlags, bool forceRemove)
{
    GenTreePtr  add1;
    GenTreePtr* addp;

    GenTreePtr  nop1;
    GenTreePtr* nopp;

    GenTreePtr icon;
    GenTreePtr mult;

    GenTreePtr base;

    ssize_t ival;

#if !REARRANGE_ADDS
    noway_assert(!"can't remove range checks without REARRANGE_ADDS right now");
#endif

    noway_assert(stmt->gtOper == GT_STMT);
    noway_assert(tree->gtOper == GT_COMMA);
    noway_assert(tree->gtOp.gtOp1->gtOper == GT_ARR_BOUNDS_CHECK);
    noway_assert(forceRemove || optIsRangeCheckRemovable(tree->gtOp.gtOp1));

    GenTreeBoundsChk* bndsChk = tree->gtOp.gtOp1->AsBoundsChk();

#ifdef DEBUG
    if (verbose)
    {
        printf("Before optRemoveRangeCheck:\n");
        gtDispTree(tree);
    }
#endif

    GenTreePtr sideEffList = nullptr;
    if (sideEffFlags)
    {
        gtExtractSideEffList(tree->gtOp.gtOp1, &sideEffList, sideEffFlags);
    }

    // Decrement the ref counts for any LclVars that are being deleted
    //
    optRemoveTree(tree->gtOp.gtOp1, sideEffList);

    // Just replace the bndsChk with a NOP as an operand to the GT_COMMA, if there are no side effects.
    tree->gtOp.gtOp1 = (sideEffList != nullptr) ? sideEffList : gtNewNothingNode();

    // TODO-CQ: We should also remove the GT_COMMA, but in any case we can no longer CSE the GT_COMMA.
    tree->gtFlags |= GTF_DONT_CSE;

    /* Recalculate the gtCostSz, etc... */
    gtSetStmtInfo(stmt);

    /* Re-thread the nodes if necessary */
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
}

/*****************************************************************************
 * Return the scale in an array reference, given a pointer to the
 * multiplication node.
 */

ssize_t Compiler::optGetArrayRefScaleAndIndex(GenTreePtr mul, GenTreePtr* pIndex DEBUGARG(bool bRngChk))
{
    assert(mul);
    assert(mul->gtOper == GT_MUL || mul->gtOper == GT_LSH);
    assert(mul->gtOp.gtOp2->IsCnsIntOrI());

    ssize_t scale = mul->gtOp.gtOp2->gtIntConCommon.IconValue();

    if (mul->gtOper == GT_LSH)
    {
        scale = ((ssize_t)1) << scale;
    }

    GenTreePtr index = mul->gtOp.gtOp1;

    if (index->gtOper == GT_MUL && index->gtOp.gtOp2->IsCnsIntOrI())
    {
        // case of two cascading multiplications for constant int (e.g.  * 20 morphed to * 5 * 4):
        // When index->gtOper is GT_MUL and index->gtOp.gtOp2->gtOper is GT_CNS_INT (i.e. * 5),
        //     we can bump up the scale from 4 to 5*4, and then change index to index->gtOp.gtOp1.
        // Otherwise, we cannot optimize it. We will simply keep the original scale and index.
        scale *= index->gtOp.gtOp2->gtIntConCommon.IconValue();
        index = index->gtOp.gtOp1;
    }

    assert(!bRngChk || index->gtOper != GT_COMMA);

    if (pIndex)
    {
        *pIndex = index;
    }

    return scale;
}

/*****************************************************************************
 * Find the last assignment to of the local variable in the block. Return
 * RHS or NULL. If any local variable in the RHS has been killed in
 * intervening code, return NULL. If the variable being searched for is killed
 * in the intervening code, return NULL.
 *
 */

GenTreePtr Compiler::optFindLocalInit(BasicBlock* block,
                                      GenTreePtr  local,
                                      VARSET_TP*  pKilledInOut,
                                      bool*       pLhsRhsKilledAfterInit)
{
    assert(pKilledInOut);
    assert(pLhsRhsKilledAfterInit);

    *pLhsRhsKilledAfterInit = false;

    unsigned LclNum = local->gtLclVarCommon.gtLclNum;

    GenTreePtr list = block->bbTreeList;
    if (list == nullptr)
    {
        return nullptr;
    }

    GenTreePtr rhs  = nullptr;
    GenTreePtr stmt = list;
    do
    {
        stmt = stmt->gtPrev;
        if (stmt == nullptr)
        {
            break;
        }

        GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
        // If we encounter an assignment to a local variable,
        if ((tree->OperKind() & GTK_ASGOP) && tree->gtOp.gtOp1->gtOper == GT_LCL_VAR)
        {
            // And the assigned variable equals the input local,
            if (tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum == LclNum)
            {
                // If the assignment is '=' and it is not a conditional, then return rhs.
                if (tree->gtOper == GT_ASG && !(tree->gtFlags & GTF_COLON_COND))
                {
                    rhs = tree->gtOp.gtOp2;
                }
                // If the assignment is 'op=' or a conditional equal, then the search ends here,
                // as we found a kill to the input local.
                else
                {
                    *pLhsRhsKilledAfterInit = true;
                    assert(rhs == nullptr);
                }
                break;
            }
            else
            {
                LclVarDsc* varDsc = optIsTrackedLocal(tree->gtOp.gtOp1);
                if (varDsc == nullptr)
                {
                    return nullptr;
                }
                VarSetOps::AddElemD(this, *pKilledInOut, varDsc->lvVarIndex);
            }
        }
    } while (stmt != list);

    if (rhs == nullptr)
    {
        return nullptr;
    }

    // If any local in the RHS is killed in intervening code, or RHS has an indirection, return NULL.
    varRefKinds rhsRefs = VR_NONE;
    VARSET_TP   VARSET_INIT_NOCOPY(rhsLocals, VarSetOps::UninitVal());
    bool        b = lvaLclVarRefs(rhs, nullptr, &rhsRefs, &rhsLocals);
    if (!b || !VarSetOps::IsEmptyIntersection(this, rhsLocals, *pKilledInOut) || (rhsRefs != VR_NONE))
    {
        // If RHS has been indirectly referenced, consider it a write and a kill.
        *pLhsRhsKilledAfterInit = true;
        return nullptr;
    }

    return rhs;
}

/*****************************************************************************
 *
 *  Return true if "op1" is guaranteed to be less then or equal to "op2".
 */

#if FANCY_ARRAY_OPT

bool Compiler::optIsNoMore(GenTreePtr op1, GenTreePtr op2, int add1, int add2)
{
    if (op1->gtOper == GT_CNS_INT && op2->gtOper == GT_CNS_INT)
    {
        add1 += op1->gtIntCon.gtIconVal;
        add2 += op2->gtIntCon.gtIconVal;
    }
    else
    {
        /* Check for +/- constant on either operand */

        if (op1->gtOper == GT_ADD && op1->gtOp.gtOp2->gtOper == GT_CNS_INT)
        {
            add1 += op1->gtOp.gtOp2->gtIntCon.gtIconVal;
            op1 = op1->gtOp.gtOp1;
        }

        if (op2->gtOper == GT_ADD && op2->gtOp.gtOp2->gtOper == GT_CNS_INT)
        {
            add2 += op2->gtOp.gtOp2->gtIntCon.gtIconVal;
            op2 = op2->gtOp.gtOp1;
        }

        /* We only allow local variable references */

        if (op1->gtOper != GT_LCL_VAR)
            return false;
        if (op2->gtOper != GT_LCL_VAR)
            return false;
        if (op1->gtLclVarCommon.gtLclNum != op2->gtLclVarCommon.gtLclNum)
            return false;

        /* NOTE: Caller ensures that this variable has only one def */

        // printf("limit [%d]:\n", add1); gtDispTree(op1);
        // printf("size  [%d]:\n", add2); gtDispTree(op2);
        // printf("\n");
    }

    return (bool)(add1 <= add2);
}

#endif

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
        JITDUMP("Considering loop %d to clone for optimizations.\n", i);
        if (optIsLoopClonable(i))
        {
            if (!(optLoopTable[i].lpFlags & LPFLG_REMOVED))
            {
                optIdentifyLoopOptInfo(i, context);
            }
        }
        JITDUMP("------------------------------------------------------------\n");
    }
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// optIdentifyLoopOptInfo: Identify loop optimization candidates an also
//      check if the loop is suitable for the optimizations performed.
//
// Arguments:
//     loopNum     -  the current loop index for which conditions are derived.
//     context     -  data structure where all loop cloning candidates will be
//                    updated.
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

    if (!(pLoop->lpFlags & LPFLG_ITER))
    {
        JITDUMP("> No iter flag on loop %d.\n", loopNum);
        return false;
    }

    unsigned ivLclNum = pLoop->lpIterVar();
    if (lvaVarAddrExposed(ivLclNum))
    {
        JITDUMP("> Rejected V%02u as iter var because is address-exposed.\n", ivLclNum);
        return false;
    }

    BasicBlock* head = pLoop->lpHead;
    BasicBlock* end  = pLoop->lpBottom;
    BasicBlock* beg  = head->bbNext;

    if (end->bbJumpKind != BBJ_COND)
    {
        JITDUMP("> Couldn't find termination test.\n");
        return false;
    }

    if (end->bbJumpDest != beg)
    {
        JITDUMP("> Branch at loop 'end' not looping to 'begin'.\n");
        return false;
    }

    // TODO-CQ: CLONE: Mark increasing or decreasing loops.
    if ((pLoop->lpIterOper() != GT_ASG_ADD && pLoop->lpIterOper() != GT_ADD) || (pLoop->lpIterConst() != 1))
    {
        JITDUMP("> Loop iteration operator not matching\n");
        return false;
    }

    if ((pLoop->lpFlags & LPFLG_CONST_LIMIT) == 0 && (pLoop->lpFlags & LPFLG_VAR_LIMIT) == 0 &&
        (pLoop->lpFlags & LPFLG_ARRLEN_LIMIT) == 0)
    {
        JITDUMP("> Loop limit is neither constant, variable or array length\n");
        return false;
    }

    if (!(((pLoop->lpTestOper() == GT_LT || pLoop->lpTestOper() == GT_LE) &&
           (pLoop->lpIterOper() == GT_ADD || pLoop->lpIterOper() == GT_ASG_ADD)) ||
          ((pLoop->lpTestOper() == GT_GT || pLoop->lpTestOper() == GT_GE) &&
           (pLoop->lpIterOper() == GT_SUB || pLoop->lpIterOper() == GT_ASG_SUB))))
    {
        JITDUMP("> Loop test (%s) doesn't agree with the direction (%s) of the pLoop->\n",
                GenTree::NodeName(pLoop->lpTestOper()), GenTree::NodeName(pLoop->lpIterOper()));
        return false;
    }

    if (!(pLoop->lpTestTree->OperKind() & GTK_RELOP) || !(pLoop->lpTestTree->gtFlags & GTF_RELOP_ZTT))
    {
        JITDUMP("> Loop inversion NOT present, loop test [%06u] may not protect entry from head.\n",
                pLoop->lpTestTree->gtTreeID);
        return false;
    }

#ifdef DEBUG
    GenTreePtr op1 = pLoop->lpIterator();
    noway_assert((op1->gtOper == GT_LCL_VAR) && (op1->gtLclVarCommon.gtLclNum == ivLclNum));
#endif

    JITDUMP("Checking blocks BB%02d..BB%02d for optimization candidates\n", beg->bbNum,
            end->bbNext ? end->bbNext->bbNum : 0);

    LoopCloneVisitorInfo info(context, loopNum, nullptr);
    for (BasicBlock* block = beg; block != end->bbNext; block = block->bbNext)
    {
        compCurBB = block;
        for (GenTreePtr stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            info.stmt = stmt;
            fgWalkTreePre(&stmt->gtStmt.gtStmtExpr, optCanOptimizeByLoopCloningVisitor, &info, false, false);
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
//      lhsNum      for the root level (function is recursive) callers should be BAD_VAR_NUM.
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
//    [000000001AF828D8] ---XG-------                     indir     int
//    [000000001AF872C8] ------------                           const     long   16 Fseq[#FirstElem]
//    [000000001AF87340] ------------                        +         byref
//    [000000001AF87160] -------N----                                 const     long   2
//    [000000001AF871D8] ------------                              <<        long
//    [000000001AF870C0] ------------                                 cast      long <- int
//    [000000001AF86F30] i-----------                                    lclVar    int    V04 loc0
//    [000000001AF87250] ------------                           +         byref
//    [000000001AF86EB8] ------------                              lclVar    ref    V01 arg1
//    [000000001AF87468] ---XG-------                  comma     int
//    [000000001AF87020] ---X--------                     arrBndsChk void
//    [000000001AF86FA8] ---X--------                        arrLen    int
//    [000000001AF827E8] ------------                           lclVar    ref    V01 arg1
//    [000000001AF82860] ------------                        lclVar    int    V04 loc0
//    [000000001AF829F0] -A-XG-------               =         int
//    [000000001AF82978] D------N----                  lclVar    int    V06 tmp0
//
bool Compiler::optExtractArrIndex(GenTreePtr tree, ArrIndex* result, unsigned lhsNum)
{
    if (tree->gtOper != GT_COMMA)
    {
        return false;
    }
    GenTreePtr before = tree->gtGetOp1();
    if (before->gtOper != GT_ARR_BOUNDS_CHECK)
    {
        return false;
    }
    GenTreeBoundsChk* arrBndsChk = before->AsBoundsChk();
    if (arrBndsChk->gtArrLen->gtGetOp1()->gtOper != GT_LCL_VAR)
    {
        return false;
    }
    if (arrBndsChk->gtIndex->gtOper != GT_LCL_VAR)
    {
        return false;
    }
    unsigned arrLcl = arrBndsChk->gtArrLen->gtGetOp1()->gtLclVarCommon.gtLclNum;
    if (lhsNum != BAD_VAR_NUM && arrLcl != lhsNum)
    {
        return false;
    }

    unsigned indLcl = arrBndsChk->gtIndex->gtLclVarCommon.gtLclNum;

    GenTreePtr after = tree->gtGetOp2();

    if (after->gtOper != GT_IND)
    {
        return false;
    }
    GenTreePtr sibo = after->gtGetOp1();
    if (sibo->gtOper != GT_ADD)
    {
        return false;
    }
    GenTreePtr sib = sibo->gtGetOp1();
    GenTreePtr ofs = sibo->gtGetOp2();
    if (ofs->gtOper != GT_CNS_INT)
    {
        return false;
    }
    if (sib->gtOper != GT_ADD)
    {
        return false;
    }
    GenTreePtr si   = sib->gtGetOp2();
    GenTreePtr base = sib->gtGetOp1();
    if (si->gtOper != GT_LSH)
    {
        return false;
    }
    if (base->OperGet() != GT_LCL_VAR || base->gtLclVarCommon.gtLclNum != arrLcl)
    {
        return false;
    }
    GenTreePtr scale = si->gtGetOp2();
    GenTreePtr index = si->gtGetOp1();
    if (scale->gtOper != GT_CNS_INT)
    {
        return false;
    }
#ifdef _TARGET_AMD64_
    if (index->gtOper != GT_CAST)
    {
        return false;
    }
    GenTreePtr indexVar = index->gtGetOp1();
#else
    GenTreePtr indexVar = index;
#endif
    if (indexVar->gtOper != GT_LCL_VAR || indexVar->gtLclVarCommon.gtLclNum != indLcl)
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
//      result      the extracted GT_INDEX information.
//      lhsNum      for the root level (function is recursive) callers should be BAD_VAR_NUM.
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
bool Compiler::optReconstructArrIndex(GenTreePtr tree, ArrIndex* result, unsigned lhsNum)
{
    // If we can extract "tree" (which is a top level comma) return.
    if (optExtractArrIndex(tree, result, lhsNum))
    {
        return true;
    }
    // We have a comma (check if array base expr is computed in "before"), descend further.
    else if (tree->OperGet() == GT_COMMA)
    {
        GenTreePtr before = tree->gtGetOp1();
        // "before" should evaluate an array base for the "after" indexing.
        if (before->OperGet() != GT_ASG)
        {
            return false;
        }
        GenTreePtr lhs = before->gtGetOp1();
        GenTreePtr rhs = before->gtGetOp2();

        // "rhs" should contain an GT_INDEX
        if (!lhs->IsLocal() || !optReconstructArrIndex(rhs, result, lhsNum))
        {
            return false;
        }
        unsigned   lhsNum = lhs->gtLclVarCommon.gtLclNum;
        GenTreePtr after  = tree->gtGetOp2();
        // Pass the "lhsNum", so we can verify if indeed it is used as the array base.
        return optExtractArrIndex(after, result, lhsNum);
    }
    return false;
}

/* static */
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloningVisitor(GenTreePtr* pTree, Compiler::fgWalkData* data)
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
//      If array index can be reconstructed, check if the iter var of the loop matches the
//      array index var in some dim. Also ensure other index vars before the identified
//      dim are loop invariant.
//
//  Return Value:
//      Skip sub trees if the optimization candidate is identified or else continue walking
//
Compiler::fgWalkResult Compiler::optCanOptimizeByLoopCloning(GenTreePtr tree, LoopCloneVisitorInfo* info)
{
    ArrIndex arrIndex(getAllocator());

    // Check if array index can be optimized.
    if (optReconstructArrIndex(tree, &arrIndex, BAD_VAR_NUM))
    {
        assert(tree->gtOper == GT_COMMA);
#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("Found ArrIndex at tree ");
            printTreeID(tree);
            printf(" which is equivalent to: ");
            arrIndex.Print();
            JITDUMP("\n");
        }
#endif
        if (!optIsStackLocalInvariant(info->loopNum, arrIndex.arrLcl))
        {
            return WALK_SKIP_SUBTREES;
        }

        // Walk the dimensions and see if iterVar of the loop is used as index.
        for (unsigned dim = 0; dim < arrIndex.rank; ++dim)
        {
            // Is index variable also used as the loop iter var.
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
                    JITDUMP("Loop %d can be cloned for ArrIndex ", info->loopNum);
                    arrIndex.Print();
                    JITDUMP(" on dim %d\n", dim);
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
Compiler::fgWalkResult Compiler::optValidRangeCheckIndex(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr        tree  = *pTree;
    optRangeCheckDsc* pData = (optRangeCheckDsc*)data->pCallbackData;

    if (tree->gtOper == GT_IND || tree->gtOper == GT_CLS_VAR || tree->gtOper == GT_FIELD || tree->gtOper == GT_LCL_FLD)
    {
        pData->bValidIndex = false;
        return WALK_ABORT;
    }

    if (tree->gtOper == GT_LCL_VAR)
    {
        if (pData->pCompiler->lvaTable[tree->gtLclVarCommon.gtLclNum].lvAddrExposed)
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
bool Compiler::optIsRangeCheckRemovable(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_ARR_BOUNDS_CHECK);
    GenTreeBoundsChk* bndsChk = tree->AsBoundsChk();
    GenTreePtr        pArray  = bndsChk->GetArray();
    if (pArray == nullptr && !bndsChk->gtArrLen->IsCnsIntOrI())
    {
        return false;
    }
    GenTreePtr pIndex = bndsChk->gtIndex;

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
            noway_assert(pArray->gtLclVarCommon.gtLclNum < lvaCount);

            if (lvaTable[pArray->gtLclVarCommon.gtLclNum].lvAddrExposed)
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
    GenTreePtr condStmt = condBlock->bbTreeList->gtPrev->gtStmt.gtStmtExpr;

    noway_assert(condStmt->gtOper == GT_JTRUE);

    bool       isBool;
    GenTreePtr relop;

    GenTreePtr comparand = optIsBoolCond(condStmt, &relop, &isBool);

    if (comparand == nullptr || !varTypeIsGC(comparand->TypeGet()))
    {
        return;
    }

    if (comparand->gtFlags & (GTF_ASG | GTF_CALL | GTF_ORDER_SIDEEFF))
    {
        return;
    }

    GenTreePtr comparandClone = gtCloneExpr(comparand);

    // Bump up the ref-counts of any variables in 'comparandClone'
    compCurBB = condBlock;
    fgWalkTreePre(&comparandClone, Compiler::lvaIncRefCntsCB, (void*)this, true);

    noway_assert(relop->gtOp.gtOp1 == comparand);
    genTreeOps oper   = compStressCompile(STRESS_OPT_BOOLS_GC, 50) ? GT_OR : GT_AND;
    relop->gtOp.gtOp1 = gtNewOperNode(oper, TYP_I_IMPL, comparand, comparandClone);

    // Comparand type is already checked, and we have const int, there is no harm
    // morphing it into a TYP_I_IMPL.
    noway_assert(relop->gtOp.gtOp2->gtOper == GT_CNS_INT);
    relop->gtOp.gtOp2->gtType = TYP_I_IMPL;
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
    GenTree* cond = condBranch->gtOp.gtOp1;

    /* The condition must be "!= 0" or "== 0" */

    if ((cond->gtOper != GT_EQ) && (cond->gtOper != GT_NE))
    {
        return nullptr;
    }

    /* Return the compare node to the caller */

    *compPtr = cond;

    /* Get hold of the comparands */

    GenTree* opr1 = cond->gtOp.gtOp1;
    GenTree* opr2 = cond->gtOp.gtOp2;

    if (opr2->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    if (!opr2->IsIntegralConst(0) && !opr2->IsIntegralConst(1))
    {
        return nullptr;
    }

    ssize_t ival2 = opr2->gtIntCon.gtIconVal;

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

        unsigned lclNum = opr1->gtLclVarCommon.gtLclNum;
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
            opr2->gtIntCon.gtIconVal = 0;
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
                   we wil try to fold it to :
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
                        B1: brtrue((!t1)&&t2, B3)
                        B3:
                */

                sameTarget = false;
            }
            else
            {
                continue;
            }

            /* The second block must contain a single statement */

            GenTreePtr s2 = b2->bbTreeList;
            if (s2->gtPrev != s2)
            {
                continue;
            }

            noway_assert(s2->gtOper == GT_STMT);
            GenTreePtr t2 = s2->gtStmt.gtStmtExpr;
            noway_assert(t2->gtOper == GT_JTRUE);

            /* Find the condition for the first block */

            GenTreePtr s1 = b1->bbTreeList->gtPrev;

            noway_assert(s1->gtOper == GT_STMT);
            GenTreePtr t1 = s1->gtStmt.gtStmtExpr;
            noway_assert(t1->gtOper == GT_JTRUE);

            if (b2->countOfInEdges() > 1)
            {
                continue;
            }

            /* Find the branch conditions of b1 and b2 */

            bool bool1, bool2;

            GenTreePtr c1 = optIsBoolCond(t1, &t1, &bool1);
            if (!c1)
            {
                continue;
            }

            GenTreePtr c2 = optIsBoolCond(t2, &t2, &bool2);
            if (!c2)
            {
                continue;
            }

            noway_assert(t1->gtOper == GT_EQ || t1->gtOper == GT_NE && t1->gtOp.gtOp1 == c1);
            noway_assert(t2->gtOper == GT_EQ || t2->gtOper == GT_NE && t2->gtOp.gtOp1 == c2);

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
#ifdef _TARGET_ARMARCH_
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

            if (c2->gtCostEx > 12)
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
            GenTreePtr cmpOp1 = gtNewOperNode(foldOp, foldType, c1, c2);
            if (bool1 && bool2)
            {
                /* When we 'OR'/'AND' two booleans, the result is boolean as well */
                cmpOp1->gtFlags |= GTF_BOOLEAN;
            }

            t1->SetOper(cmpOp);
            t1->gtOp.gtOp1         = cmpOp1;
            t1->gtOp.gtOp2->gtType = foldType; // Could have been varTypeIsGC()

#if FEATURE_SET_FLAGS
            // For comparisons against zero we will have the GTF_SET_FLAGS set
            // and this can cause an assert to fire in fgMoveOpsLeft(GenTreePtr tree)
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
            // we generate an instuction that sets the flags, which allows us
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

            BasicBlock::weight_t edgeSumMin = edge1->flEdgeWeightMin + edge2->flEdgeWeightMin;
            BasicBlock::weight_t edgeSumMax = edge1->flEdgeWeightMax + edge2->flEdgeWeightMax;
            if ((edgeSumMax >= edge1->flEdgeWeightMax) && (edgeSumMax >= edge2->flEdgeWeightMax))
            {
                edge1->flEdgeWeightMin = edgeSumMin;
                edge1->flEdgeWeightMax = edgeSumMax;
            }
            else
            {
                edge1->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edge1->flEdgeWeightMax = BB_MAX_WEIGHT;
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
                printf("Folded %sboolean conditions of BB%02u and BB%02u to :\n", c2->OperIsLeaf() ? "" : "non-leaf ",
                       b1->bbNum, b2->bbNum);
                gtDispTree(s1);
                printf("\n");
            }
#endif
        }
    } while (change);

#ifdef DEBUG
    fgDebugCheckBBlist();
#endif
}
