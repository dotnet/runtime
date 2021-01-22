// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Information

#ifdef DEBUG
// Check to see if block contains a statement but don't spend more than a certain
// budget doing this per method compiled.
// If the budget is exceeded, return 'answerOnBoundExceeded' as the answer.
/* static */
bool Compiler::fgBlockContainsStatementBounded(BasicBlock* block,
                                               Statement*  stmt,
                                               bool        answerOnBoundExceeded /*= true*/)
{
    const __int64 maxLinks = 1000000000;

    __int64* numTraversed = &JitTls::GetCompiler()->compNumStatementLinksTraversed;

    if (*numTraversed > maxLinks)
    {
        return answerOnBoundExceeded;
    }

    Statement* curr = block->firstStmt();
    do
    {
        (*numTraversed)++;
        if (curr == stmt)
        {
            break;
        }
        curr = curr->GetNextStmt();
    } while (curr != nullptr);
    return curr != nullptr;
}
#endif // DEBUG

//------------------------------------------------------------------------
// fgInsertStmtAtBeg: Insert the given statement at the start of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
// Notes:
//    We always insert phi statements at the beginning.
//    In other cases, if there are any phi assignments and/or an assignment of
//    the GT_CATCH_ARG, we insert after those.
//
void Compiler::fgInsertStmtAtBeg(BasicBlock* block, Statement* stmt)
{
    Statement* firstStmt = block->firstStmt();

    if (stmt->IsPhiDefnStmt())
    {
        // The new tree will now be the first one of the block.
        block->bbStmtList = stmt;
        stmt->SetNextStmt(firstStmt);

        // Are there any statements in the block?
        if (firstStmt != nullptr)
        {
            // There is at least one statement already.
            Statement* lastStmt = firstStmt->GetPrevStmt();
            noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);

            // Insert the statement in front of the first one.
            firstStmt->SetPrevStmt(stmt);
            stmt->SetPrevStmt(lastStmt);
        }
        else
        {
            // The block was completely empty.
            stmt->SetPrevStmt(stmt);
        }
    }
    else
    {
        Statement* insertBeforeStmt = block->FirstNonPhiDefOrCatchArgAsg();
        if (insertBeforeStmt != nullptr)
        {
            fgInsertStmtBefore(block, insertBeforeStmt, stmt);
        }
        else
        {
            // There were no non-phi/non-catch arg statements, insert `stmt` at the end.
            fgInsertStmtAtEnd(block, stmt);
        }
    }
}

//------------------------------------------------------------------------
// fgNewStmtAtBeg: Insert the given tree as a new statement at the start of the given basic block.
//
// Arguments:
//   block - the block into which 'tree' will be inserted;
//   tree  - the tree to be inserted.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
Statement* Compiler::fgNewStmtAtBeg(BasicBlock* block, GenTree* tree)
{
    Statement* stmt = gtNewStmt(tree);
    fgInsertStmtAtBeg(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtAtEnd: Insert the given statement at the end of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   If the block can be a conditional block, use fgInsertStmtNearEnd.
//
void Compiler::fgInsertStmtAtEnd(BasicBlock* block, Statement* stmt)
{

    assert(stmt->GetNextStmt() == nullptr); // We don't set it, and it needs to be this after the insert

    Statement* firstStmt = block->firstStmt();
    if (firstStmt != nullptr)
    {
        // There is at least one statement already.
        Statement* lastStmt = firstStmt->GetPrevStmt();
        noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);

        // Append the statement after the last one.
        lastStmt->SetNextStmt(stmt);
        stmt->SetPrevStmt(lastStmt);
        firstStmt->SetPrevStmt(stmt);
    }
    else
    {
        // The block is completely empty.
        block->bbStmtList = stmt;
        stmt->SetPrevStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgNewStmtAtEnd: Insert the given tree as a new statement at the end of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   tree  - the tree to be inserted.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
// Note:
//   If the block can be a conditional block, use fgNewStmtNearEnd.
//
Statement* Compiler::fgNewStmtAtEnd(BasicBlock* block, GenTree* tree)
{
    Statement* stmt = gtNewStmt(tree);
    fgInsertStmtAtEnd(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtNearEnd: Insert the given statement at the end of the given basic block,
//   but before the GT_JTRUE, if present.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
void Compiler::fgInsertStmtNearEnd(BasicBlock* block, Statement* stmt)
{
    // This routine can only be used when in tree order.
    assert(fgOrder == FGOrderTree);

    if ((block->bbJumpKind == BBJ_COND) || (block->bbJumpKind == BBJ_SWITCH) || (block->bbJumpKind == BBJ_RETURN))
    {
        Statement* firstStmt = block->firstStmt();
        noway_assert(firstStmt != nullptr);
        Statement* lastStmt = block->lastStmt();
        noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);
        Statement* insertionPoint = lastStmt->GetPrevStmt();

#if DEBUG
        if (block->bbJumpKind == BBJ_COND)
        {
            assert(lastStmt->GetRootNode()->gtOper == GT_JTRUE);
        }
        else if (block->bbJumpKind == BBJ_RETURN)
        {
            assert((lastStmt->GetRootNode()->gtOper == GT_RETURN) || (lastStmt->GetRootNode()->gtOper == GT_JMP) ||
                   // BBJ_RETURN blocks in functions returning void do not get a GT_RETURN node if they
                   // have a .tail prefix (even if canTailCall returns false for these calls)
                   // code:Compiler::impImportBlockCode (search for the RET: label)
                   // Ditto for real tail calls (all code after them has been removed)
                   ((lastStmt->GetRootNode()->gtOper == GT_CALL) &&
                    ((info.compRetType == TYP_VOID) || lastStmt->GetRootNode()->AsCall()->IsTailCall())));
        }
        else
        {
            assert(block->bbJumpKind == BBJ_SWITCH);
            assert(lastStmt->GetRootNode()->gtOper == GT_SWITCH);
        }
#endif // DEBUG

        // Append 'stmt' before 'lastStmt'.
        stmt->SetNextStmt(lastStmt);
        lastStmt->SetPrevStmt(stmt);

        if (firstStmt == lastStmt)
        {
            // There is only one stmt in the block.
            block->bbStmtList = stmt;
            stmt->SetPrevStmt(lastStmt);
        }
        else
        {
            // Append 'stmt' after 'insertionPoint'.
            noway_assert(insertionPoint != nullptr && (insertionPoint->GetNextStmt() == lastStmt));
            insertionPoint->SetNextStmt(stmt);
            stmt->SetPrevStmt(insertionPoint);
        }
    }
    else
    {
        fgInsertStmtAtEnd(block, stmt);
    }
}

//------------------------------------------------------------------------
// fgNewStmtNearEnd: Insert the given tree as a new statement at the end of the given basic block,
//   but before the GT_JTRUE, if present.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   tree  - the tree to be inserted.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
Statement* Compiler::fgNewStmtNearEnd(BasicBlock* block, GenTree* tree)
{
    Statement* stmt = gtNewStmt(tree);
    fgInsertStmtNearEnd(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtAfter: Insert the given statement after the insertion point in the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   insertionPoint - the statement after which `stmt` will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   `block` is needed to update the last statement pointer and for debugging checks.
//
void Compiler::fgInsertStmtAfter(BasicBlock* block, Statement* insertionPoint, Statement* stmt)
{
    assert(block->bbStmtList != nullptr);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint->GetNextStmt() == nullptr)
    {
        // Ok, we want to insert after the last statement of the block.
        stmt->SetNextStmt(nullptr);
        stmt->SetPrevStmt(insertionPoint);

        insertionPoint->SetNextStmt(stmt);

        // Update the backward link of the first statement of the block
        // to point to the new last statement.
        assert(block->bbStmtList->GetPrevStmt() == insertionPoint);
        block->bbStmtList->SetPrevStmt(stmt);
    }
    else
    {
        stmt->SetNextStmt(insertionPoint->GetNextStmt());
        stmt->SetPrevStmt(insertionPoint);

        insertionPoint->GetNextStmt()->SetPrevStmt(stmt);
        insertionPoint->SetNextStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgInsertStmtBefore: Insert the given statement before the insertion point in the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   insertionPoint - the statement before which `stmt` will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   `block` is needed to update the first statement pointer and for debugging checks.
//
void Compiler::fgInsertStmtBefore(BasicBlock* block, Statement* insertionPoint, Statement* stmt)
{
    assert(block->bbStmtList != nullptr);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint == block->bbStmtList)
    {
        // We're inserting before the first statement in the block.
        Statement* first = block->firstStmt();
        Statement* last  = block->lastStmt();

        stmt->SetNextStmt(first);
        stmt->SetPrevStmt(last);

        block->bbStmtList = stmt;
        first->SetPrevStmt(stmt);
    }
    else
    {
        stmt->SetNextStmt(insertionPoint);
        stmt->SetPrevStmt(insertionPoint->GetPrevStmt());

        insertionPoint->GetPrevStmt()->SetNextStmt(stmt);
        insertionPoint->SetPrevStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgInsertStmtListAfter: Insert the list of statements stmtList after the stmtAfter in block.
//
// Arguments:
//   block - the block where stmtAfter is in;
//   stmtAfter - the statement where stmtList should be inserted after;
//   stmtList - the statement list to insert.
//
// Return value:
//   the last statement in the united list.
//
Statement* Compiler::fgInsertStmtListAfter(BasicBlock* block, Statement* stmtAfter, Statement* stmtList)
{
    // Currently we can handle when stmtAfter and stmtList are non-NULL. This makes everything easy.
    noway_assert(stmtAfter != nullptr);
    noway_assert(stmtList != nullptr);

    // Last statement in a non-empty list, circular in the GetPrevStmt() list.
    Statement* stmtLast = stmtList->GetPrevStmt();
    noway_assert(stmtLast != nullptr);
    noway_assert(stmtLast->GetNextStmt() == nullptr);

    Statement* stmtNext = stmtAfter->GetNextStmt();

    if (stmtNext == nullptr)
    {
        stmtAfter->SetNextStmt(stmtList);
        stmtList->SetPrevStmt(stmtAfter);
        block->bbStmtList->SetPrevStmt(stmtLast);
    }
    else
    {
        stmtAfter->SetNextStmt(stmtList);
        stmtList->SetPrevStmt(stmtAfter);

        stmtLast->SetNextStmt(stmtNext);
        stmtNext->SetPrevStmt(stmtLast);
    }

    noway_assert(block->bbStmtList == nullptr || block->bbStmtList->GetPrevStmt()->GetNextStmt() == nullptr);

    return stmtLast;
}

//------------------------------------------------------------------------
// fgGetPredForBlock: Find and return the predecessor edge corresponding to a given predecessor block.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to find in the predecessor list.
//
// Return Value:
//    The flowList edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.

flowList* Compiler::fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred)
{
    assert(block);
    assert(blockPred);
    assert(!fgCheapPredsValid);

    flowList* pred;

    for (pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        if (blockPred == pred->getBlock())
        {
            return pred;
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// fgGetPredForBlock: Find and return the predecessor edge corresponding to a given predecessor block.
// Also returns the address of the pointer that points to this edge, to make it possible to remove this edge from the
// predecessor list without doing another linear search over the edge list.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to find in the predecessor list.
//    ptrToPred -- Out parameter: set to the address of the pointer that points to the returned predecessor edge.
//
// Return Value:
//    The flowList edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.

flowList* Compiler::fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred, flowList*** ptrToPred)
{
    assert(block);
    assert(blockPred);
    assert(ptrToPred);
    assert(!fgCheapPredsValid);

    flowList** predPrevAddr;
    flowList*  pred;

    for (predPrevAddr = &block->bbPreds, pred = *predPrevAddr; pred != nullptr;
         predPrevAddr = &pred->flNext, pred = *predPrevAddr)
    {
        if (blockPred == pred->getBlock())
        {
            *ptrToPred = predPrevAddr;
            return pred;
        }
    }

    *ptrToPred = nullptr;
    return nullptr;
}

//------------------------------------------------------------------------
// fgSpliceOutPred: Removes a predecessor edge for a block from the predecessor list.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    The flowList edge that was removed.
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This simply splices out the flowList object. It doesn't update block ref counts, handle duplicate counts, etc.
//       For that, use fgRemoveRefPred() or fgRemoveAllRefPred().
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- This must walk the predecessor list to find the block in question. If the predecessor edge
//       is found using fgGetPredForBlock(), consider using the version that hands back the predecessor pointer
//       address instead, to avoid this search.
//    -- Marks fgModified = true, since the flow graph has changed.

flowList* Compiler::fgSpliceOutPred(BasicBlock* block, BasicBlock* blockPred)
{
    assert(!fgCheapPredsValid);
    noway_assert(block->bbPreds);

    flowList* oldEdge = nullptr;

    // Is this the first block in the pred list?
    if (blockPred == block->bbPreds->getBlock())
    {
        oldEdge        = block->bbPreds;
        block->bbPreds = block->bbPreds->flNext;
    }
    else
    {
        flowList* pred;
        for (pred = block->bbPreds; (pred->flNext != nullptr) && (blockPred != pred->flNext->getBlock());
             pred = pred->flNext)
        {
            // empty
        }
        oldEdge = pred->flNext;
        if (oldEdge == nullptr)
        {
            noway_assert(!"Should always find the blockPred");
        }
        pred->flNext = pred->flNext->flNext;
    }

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return oldEdge;
}

//------------------------------------------------------------------------
// fgAddRefPred: Increment block->bbRefs by one and add "blockPred" to the predecessor list of "block".
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to add to the predecessor list.
//    oldEdge -- Optional (default: nullptr). If non-nullptr, and a new edge is created (and the dup count
//               of an existing edge is not just incremented), the edge weights are copied from this edge.
//    initializingPreds -- Optional (default: false). Only set to "true" when the initial preds computation is
//    happening.
//
// Return Value:
//    The flow edge representing the predecessor.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- block->bbRefs is incremented by one to account for the reduction in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a new flow edge is created (but not if an existing flow edge dup count is incremented),
//       indicating that the flow graph shape has changed.

flowList* Compiler::fgAddRefPred(BasicBlock* block,
                                 BasicBlock* blockPred,
                                 flowList*   oldEdge /* = nullptr */,
                                 bool        initializingPreds /* = false */)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);

    block->bbRefs++;

    if (!fgComputePredsDone && !initializingPreds)
    {
        // Why is someone trying to update the preds list when the preds haven't been created?
        // Ignore them! This can happen when fgMorph is called before the preds list is created.
        return nullptr;
    }

    assert(!fgCheapPredsValid);

    // Keep the predecessor list in lowest to highest bbNum order. This allows us to discover the loops in
    // optFindNaturalLoops from innermost to outermost.
    //
    // If we are initializing preds, we rely on the fact that we are adding references in increasing
    // order of blockPred->bbNum to avoid searching the list.
    //
    // TODO-Throughput: Inserting an edge for a block in sorted order requires searching every existing edge.
    // Thus, inserting all the edges for a block is quadratic in the number of edges. We need to either
    // not bother sorting for debuggable code, or sort in optFindNaturalLoops, or better, make the code in
    // optFindNaturalLoops not depend on order. This also requires ensuring that nobody else has taken a
    // dependency on this order. Note also that we don't allow duplicates in the list; we maintain a flDupCount
    // count of duplication. This also necessitates walking the flow list for every edge we add.
    //
    flowList*  flow  = nullptr;
    flowList** listp = &block->bbPreds;

    if (initializingPreds)
    {
        // List is sorted order and we're adding references in
        // increasing blockPred->bbNum order. The only possible
        // dup list entry is the last one.
        //
        flowList* flowLast = block->bbLastPred;
        if (flowLast != nullptr)
        {
            listp = &flowLast->flNext;

            assert(flowLast->getBlock()->bbNum <= blockPred->bbNum);

            if (flowLast->getBlock() == blockPred)
            {
                flow = flowLast;
            }
        }
    }
    else
    {
        // References are added randomly, so we have to search.
        //
        while ((*listp != nullptr) && ((*listp)->getBlock()->bbNum < blockPred->bbNum))
        {
            listp = &(*listp)->flNext;
        }

        if ((*listp != nullptr) && ((*listp)->getBlock() == blockPred))
        {
            flow = *listp;
        }
    }

    if (flow != nullptr)
    {
        // The predecessor block already exists in the flow list; simply add to its duplicate count.
        noway_assert(flow->flDupCount > 0);
        flow->flDupCount++;
    }
    else
    {

#if MEASURE_BLOCK_SIZE
        genFlowNodeCnt += 1;
        genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;

        // Create new edge in the list in the correct ordered location.
        //
        flow             = new (this, CMK_FlowList) flowList(blockPred, *listp);
        flow->flDupCount = 1;
        *listp           = flow;

        if (initializingPreds)
        {
            block->bbLastPred = flow;
        }

        if (fgHaveValidEdgeWeights)
        {
            // We are creating an edge from blockPred to block
            // and we have already computed the edge weights, so
            // we will try to setup this new edge with valid edge weights.
            //
            if (oldEdge != nullptr)
            {
                // If our caller has given us the old edge weights
                // then we will use them.
                //
                flow->setEdgeWeights(oldEdge->edgeWeightMin(), oldEdge->edgeWeightMax());
            }
            else
            {
                // Set the max edge weight to be the minimum of block's or blockPred's weight
                //
                BasicBlock::weight_t newWeightMax = min(block->bbWeight, blockPred->bbWeight);

                // If we are inserting a conditional block the minimum weight is zero,
                // otherwise it is the same as the edge's max weight.
                if (blockPred->NumSucc() > 1)
                {
                    flow->setEdgeWeights(BB_ZERO_WEIGHT, newWeightMax);
                }
                else
                {
                    flow->setEdgeWeights(flow->edgeWeightMax(), newWeightMax);
                }
            }
        }
        else
        {
            flow->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT);
        }
    }

    // Pred list should (still) be ordered.
    //
    assert(block->checkPredListOrder());

    return flow;
}

//------------------------------------------------------------------------
// fgRemoveRefPred: Decrements the reference count of a predecessor edge from "blockPred" to "block",
// removing the edge if it is no longer necessary.
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    If the flow edge was removed (the predecessor has a "dup count" of 1),
//        returns the flow graph edge that was removed. This means "blockPred" is no longer a predecessor of "block".
//    Otherwise, returns nullptr. This means that "blockPred" is still a predecessor of "block" (because "blockPred"
//        is a switch with multiple cases jumping to "block", or a BBJ_COND with both conditional and fall-through
//        paths leading to "block").
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- block->bbRefs is decremented by one to account for the reduction in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a flow edge is removed (but not if an existing flow edge dup count is decremented),
//       indicating that the flow graph shape has changed.

flowList* Compiler::fgRemoveRefPred(BasicBlock* block, BasicBlock* blockPred)
{
    noway_assert(block != nullptr);
    noway_assert(blockPred != nullptr);

    noway_assert(block->countOfInEdges() > 0);
    block->bbRefs--;

    // Do nothing if we haven't calculated the predecessor list yet.
    // Yes, this does happen.
    // For example the predecessor lists haven't been created yet when we do fgMorph.
    // But fgMorph calls fgFoldConditional, which in turn calls fgRemoveRefPred.
    if (!fgComputePredsDone)
    {
        return nullptr;
    }

    assert(!fgCheapPredsValid);

    flowList** ptrToPred;
    flowList*  pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    noway_assert(pred);
    noway_assert(pred->flDupCount > 0);

    pred->flDupCount--;

    if (pred->flDupCount == 0)
    {
        // Splice out the predecessor edge since it's no longer necessary.
        *ptrToPred = pred->flNext;

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;

        return pred;
    }
    else
    {
        return nullptr;
    }
}

//------------------------------------------------------------------------
// fgRemoveAllRefPreds: Removes a predecessor edge from one block to another, no matter what the "dup count" is.
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    Returns the flow graph edge that was removed. The dup count on the edge is no longer valid.
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    block->bbRefs is decremented to account for the reduction in incoming edges.

flowList* Compiler::fgRemoveAllRefPreds(BasicBlock* block, BasicBlock* blockPred)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);
    assert(block->countOfInEdges() > 0);

    flowList** ptrToPred;
    flowList*  pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    assert(pred != nullptr);
    assert(pred->flDupCount > 0);

    assert(block->bbRefs >= pred->flDupCount);
    block->bbRefs -= pred->flDupCount;

    // Now splice out the predecessor edge.
    *ptrToPred = pred->flNext;

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return pred;
}

//------------------------------------------------------------------------
// fgRemoveAllRefPreds: Remove a predecessor edge, given the address of a pointer to it in the
// predecessor list, no matter what the "dup count" is.
//
// Arguments:
//    block -- A block with the predecessor list to operate on.
//    ptrToPred -- The address of a pointer to the predecessor to remove.
//
// Return Value:
//    The removed predecessor edge. The dup count on the edge is no longer valid.
//
// Assumptions:
//    -- The predecessor edge must be in the predecessor list for "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    block->bbRefs is decremented by the dup count of the predecessor edge, to account for the reduction in incoming
//    edges.

flowList* Compiler::fgRemoveAllRefPreds(BasicBlock* block, flowList** ptrToPred)
{
    assert(block != nullptr);
    assert(ptrToPred != nullptr);
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);
    assert(block->countOfInEdges() > 0);

    flowList* pred = *ptrToPred;
    assert(pred != nullptr);
    assert(pred->flDupCount > 0);

    assert(block->bbRefs >= pred->flDupCount);
    block->bbRefs -= pred->flDupCount;

    // Now splice out the predecessor edge.
    *ptrToPred = pred->flNext;

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return pred;
}

/*
    Removes all the appearances of block as predecessor of others
*/

void Compiler::fgRemoveBlockAsPred(BasicBlock* block)
{
    assert(!fgCheapPredsValid);

    PREFIX_ASSUME(block != nullptr);

    BasicBlock* bNext;

    switch (block->bbJumpKind)
    {
        case BBJ_CALLFINALLY:
            if (!(block->bbFlags & BBF_RETLESS_CALL))
            {
                assert(block->isBBCallAlwaysPair());

                /* The block after the BBJ_CALLFINALLY block is not reachable */
                bNext = block->bbNext;

                /* bNext is an unreachable BBJ_ALWAYS block */
                noway_assert(bNext->bbJumpKind == BBJ_ALWAYS);

                while (bNext->countOfInEdges() > 0)
                {
                    fgRemoveRefPred(bNext, bNext->bbPreds->getBlock());
                }
            }

            FALLTHROUGH;

        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:

            /* Update the predecessor list for 'block->bbJumpDest' and 'block->bbNext' */
            fgRemoveRefPred(block->bbJumpDest, block);

            if (block->bbJumpKind != BBJ_COND)
            {
                break;
            }

            /* If BBJ_COND fall through */
            FALLTHROUGH;

        case BBJ_NONE:

            /* Update the predecessor list for 'block->bbNext' */
            fgRemoveRefPred(block->bbNext, block);
            break;

        case BBJ_EHFILTERRET:

            block->bbJumpDest->bbRefs++; // To compensate the bbRefs-- inside fgRemoveRefPred
            fgRemoveRefPred(block->bbJumpDest, block);
            break;

        case BBJ_EHFINALLYRET:
        {
            /* Remove block as the predecessor of the bbNext of all
               BBJ_CALLFINALLY blocks calling this finally. No need
               to look for BBJ_CALLFINALLY for fault handlers. */

            unsigned  hndIndex = block->getHndIndex();
            EHblkDsc* ehDsc    = ehGetDsc(hndIndex);

            if (ehDsc->HasFinallyHandler())
            {
                BasicBlock* begBlk;
                BasicBlock* endBlk;
                ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                BasicBlock* finBeg = ehDsc->ebdHndBeg;

                for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                {
                    if ((bcall->bbFlags & BBF_REMOVED) || bcall->bbJumpKind != BBJ_CALLFINALLY ||
                        bcall->bbJumpDest != finBeg)
                    {
                        continue;
                    }

                    assert(bcall->isBBCallAlwaysPair());
                    fgRemoveRefPred(bcall->bbNext, block);
                }
            }
        }
        break;

        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:
        {
            unsigned     jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock** jumpTab = block->bbJumpSwt->bbsDstTab;

            do
            {
                fgRemoveRefPred(*jumpTab, block);
            } while (++jumpTab, --jumpCnt);

            break;
        }

        default:
            noway_assert(!"Block doesn't have a valid bbJumpKind!!!!");
            break;
    }
}

/*****************************************************************************
 *
 *  fgComputeCheapPreds: Function called to compute the BasicBlock::bbCheapPreds lists.
 *
 *  No other block data is changed (e.g., bbRefs, bbFlags).
 *
 *  The cheap preds lists are similar to the normal (bbPreds) predecessor lists, but are cheaper to
 *  compute and store, as follows:
 *  1. A flow edge is typed BasicBlockList, which only has a block pointer and 'next' pointer. It doesn't
 *     have weights or a dup count.
 *  2. The preds list for a block is not sorted by block number.
 *  3. The predecessors of the block following a BBJ_CALLFINALLY (the corresponding BBJ_ALWAYS,
 *     for normal, non-retless calls to the finally) are not computed.
 *  4. The cheap preds lists will contain duplicates if a single switch table has multiple branches
 *     to the same block. Thus, we don't spend the time looking for duplicates for every edge we insert.
 */
void Compiler::fgComputeCheapPreds()
{
    noway_assert(!fgComputePredsDone); // We can't do this if we've got the full preds.
    noway_assert(fgFirstBB != nullptr);

    BasicBlock* block;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgComputeCheapPreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    // Clear out the cheap preds lists.
    fgRemovePreds();

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
            case BBJ_COND:
                fgAddCheapPred(block->bbJumpDest, block);
                fgAddCheapPred(block->bbNext, block);
                break;

            case BBJ_CALLFINALLY:
            case BBJ_LEAVE: // If fgComputeCheapPreds is called before all blocks are imported, BBJ_LEAVE blocks are
                            // still in the BB list.
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                fgAddCheapPred(block->bbJumpDest, block);
                break;

            case BBJ_NONE:
                fgAddCheapPred(block->bbNext, block);
                break;

            case BBJ_EHFILTERRET:
                // Connect end of filter to catch handler.
                // In a well-formed program, this cannot be null.  Tolerate here, so that we can call
                // fgComputeCheapPreds before fgImport on an ill-formed program; the problem will be detected in
                // fgImport.
                if (block->bbJumpDest != nullptr)
                {
                    fgAddCheapPred(block->bbJumpDest, block);
                }
                break;

            case BBJ_SWITCH:
                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    fgAddCheapPred(*jumpTab, block);
                } while (++jumpTab, --jumpCnt);

                break;

            case BBJ_EHFINALLYRET: // It's expensive to compute the preds for this case, so we don't for the cheap
                                   // preds.
            case BBJ_THROW:
            case BBJ_RETURN:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    fgCheapPredsValid = true;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After fgComputeCheapPreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif
}

/*****************************************************************************
 * Add 'blockPred' to the cheap predecessor list of 'block'.
 */

void Compiler::fgAddCheapPred(BasicBlock* block, BasicBlock* blockPred)
{
    assert(!fgComputePredsDone);
    assert(block != nullptr);
    assert(blockPred != nullptr);

    block->bbCheapPreds = new (this, CMK_FlowList) BasicBlockList(blockPred, block->bbCheapPreds);

#if MEASURE_BLOCK_SIZE
    genFlowNodeCnt += 1;
    genFlowNodeSize += sizeof(BasicBlockList);
#endif // MEASURE_BLOCK_SIZE
}

/*****************************************************************************
 * Remove 'blockPred' from the cheap predecessor list of 'block'.
 * If there are duplicate edges, only remove one of them.
 */
void Compiler::fgRemoveCheapPred(BasicBlock* block, BasicBlock* blockPred)
{
    assert(!fgComputePredsDone);
    assert(fgCheapPredsValid);

    flowList* oldEdge = nullptr;

    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(block->bbCheapPreds != nullptr);

    /* Is this the first block in the pred list? */
    if (blockPred == block->bbCheapPreds->block)
    {
        block->bbCheapPreds = block->bbCheapPreds->next;
    }
    else
    {
        BasicBlockList* pred;
        for (pred = block->bbCheapPreds; pred->next != nullptr; pred = pred->next)
        {
            if (blockPred == pred->next->block)
            {
                break;
            }
        }
        noway_assert(pred->next != nullptr); // we better have found it!
        pred->next = pred->next->next;       // splice it out
    }
}

//------------------------------------------------------------------------
// fgRemovePreds - remove all pred information from blocks
//
void Compiler::fgRemovePreds()
{
    C_ASSERT(offsetof(BasicBlock, bbPreds) ==
             offsetof(BasicBlock, bbCheapPreds)); // bbPreds and bbCheapPreds are at the same place in a union,
    C_ASSERT(sizeof(((BasicBlock*)nullptr)->bbPreds) ==
             sizeof(((BasicBlock*)nullptr)->bbCheapPreds)); // and are the same size. So, this function removes both.

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->bbPreds = nullptr;
    }
    fgComputePredsDone = false;
    fgCheapPredsValid  = false;
}

//------------------------------------------------------------------------
// fgComputePreds - compute the bbPreds lists
//
// Notes:
//   Resets and then fills in the list of predecessors for each basic
//   block. Assumes blocks (via bbNext) are in increasing bbNum order.
//
void Compiler::fgComputePreds()
{
    noway_assert(fgFirstBB);

    BasicBlock* block;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgComputePreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    // Reset everything pred related
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->bbPreds    = nullptr;
        block->bbLastPred = nullptr;
        block->bbRefs     = 0;
    }

    // the first block is always reachable
    fgFirstBB->bbRefs = 1;

    // Treat the initial block as a jump target
    fgFirstBB->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

    // Under OSR, we may need to specially protect the original method entry.
    //
    if (opts.IsOSR() && (fgEntryBB != nullptr) && (fgEntryBB->bbFlags & BBF_IMPORTED))
    {
        JITDUMP("OSR: protecting original method entry " FMT_BB "\n", fgEntryBB->bbNum);
        fgEntryBB->bbRefs = 1;
    }

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
            case BBJ_CALLFINALLY:
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());

                    /* Mark the next block as being a jump target,
                       since the call target will return there */
                    PREFIX_ASSUME(block->bbNext != nullptr);
                    block->bbNext->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
                }

                FALLTHROUGH;

            case BBJ_LEAVE: // Sometimes fgComputePreds is called before all blocks are imported, so BBJ_LEAVE
                            // blocks are still in the BB list.
            case BBJ_COND:
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:

                /* Mark the jump dest block as being a jump target */
                block->bbJumpDest->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

                fgAddRefPred(block->bbJumpDest, block, nullptr, true);

                /* Is the next block reachable? */

                if (block->bbJumpKind != BBJ_COND)
                {
                    break;
                }

                noway_assert(block->bbNext);

                /* Fall through, the next block is also reachable */
                FALLTHROUGH;

            case BBJ_NONE:

                fgAddRefPred(block->bbNext, block, nullptr, true);
                break;

            case BBJ_EHFILTERRET:

                // Connect end of filter to catch handler.
                // In a well-formed program, this cannot be null.  Tolerate here, so that we can call
                // fgComputePreds before fgImport on an ill-formed program; the problem will be detected in fgImport.
                if (block->bbJumpDest != nullptr)
                {
                    fgAddRefPred(block->bbJumpDest, block, nullptr, true);
                }
                break;

            case BBJ_EHFINALLYRET:
            {
                /* Connect the end of the finally to the successor of
                  the call to this finally */

                if (!block->hasHndIndex())
                {
                    NO_WAY("endfinally outside a finally/fault block.");
                }

                unsigned  hndIndex = block->getHndIndex();
                EHblkDsc* ehDsc    = ehGetDsc(hndIndex);

                if (!ehDsc->HasFinallyOrFaultHandler())
                {
                    NO_WAY("endfinally outside a finally/fault block.");
                }

                if (ehDsc->HasFinallyHandler())
                {
                    // Find all BBJ_CALLFINALLY that branched to this finally handler.
                    BasicBlock* begBlk;
                    BasicBlock* endBlk;
                    ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                    BasicBlock* finBeg = ehDsc->ebdHndBeg;
                    for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                    {
                        if (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                        {
                            continue;
                        }

                        noway_assert(bcall->isBBCallAlwaysPair());
                        fgAddRefPred(bcall->bbNext, block, nullptr, true);
                    }
                }
            }
            break;

            case BBJ_THROW:
            case BBJ_RETURN:
                break;

            case BBJ_SWITCH:
                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    /* Mark the target block as being a jump target */
                    (*jumpTab)->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

                    fgAddRefPred(*jumpTab, block, nullptr, true);
                } while (++jumpTab, --jumpCnt);

                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    for (unsigned EHnum = 0; EHnum < compHndBBtabCount; EHnum++)
    {
        EHblkDsc* ehDsc = ehGetDsc(EHnum);

        if (ehDsc->HasFilter())
        {
            ehDsc->ebdFilter->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

            // The first block of a filter has an artifical extra refcount.
            ehDsc->ebdFilter->bbRefs++;
        }

        ehDsc->ebdHndBeg->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

        // The first block of a handler has an artificial extra refcount.
        ehDsc->ebdHndBeg->bbRefs++;
    }

    fgModified         = false;
    fgComputePredsDone = true;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After fgComputePreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif
}

unsigned Compiler::fgNSuccsOfFinallyRet(BasicBlock* block)
{
    BasicBlock* bb;
    unsigned    res;
    fgSuccOfFinallyRetWork(block, ~0, &bb, &res);
    return res;
}

BasicBlock* Compiler::fgSuccOfFinallyRet(BasicBlock* block, unsigned i)
{
    BasicBlock* bb;
    unsigned    res;
    fgSuccOfFinallyRetWork(block, i, &bb, &res);
    return bb;
}

void Compiler::fgSuccOfFinallyRetWork(BasicBlock* block, unsigned i, BasicBlock** bres, unsigned* nres)
{
    assert(block->hasHndIndex()); // Otherwise, endfinally outside a finally/fault block?

    unsigned  hndIndex = block->getHndIndex();
    EHblkDsc* ehDsc    = ehGetDsc(hndIndex);

    assert(ehDsc->HasFinallyOrFaultHandler()); // Otherwise, endfinally outside a finally/fault block.

    *bres            = nullptr;
    unsigned succNum = 0;

    if (ehDsc->HasFinallyHandler())
    {
        BasicBlock* begBlk;
        BasicBlock* endBlk;
        ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

        BasicBlock* finBeg = ehDsc->ebdHndBeg;

        for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
        {
            if (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
            {
                continue;
            }

            assert(bcall->isBBCallAlwaysPair());

            if (succNum == i)
            {
                *bres = bcall->bbNext;
                return;
            }
            succNum++;
        }
    }
    assert(i == ~0u || ehDsc->HasFaultHandler()); // Should reach here only for fault blocks.
    if (i == ~0u)
    {
        *nres = succNum;
    }
}

Compiler::SwitchUniqueSuccSet Compiler::GetDescriptorForSwitch(BasicBlock* switchBlk)
{
    assert(switchBlk->bbJumpKind == BBJ_SWITCH);
    BlockToSwitchDescMap* switchMap = GetSwitchDescMap();
    SwitchUniqueSuccSet   res;
    if (switchMap->Lookup(switchBlk, &res))
    {
        return res;
    }
    else
    {
        // We must compute the descriptor. Find which are dups, by creating a bit set with the unique successors.
        // We create a temporary bitset of blocks to compute the unique set of successor blocks,
        // since adding a block's number twice leaves just one "copy" in the bitset. Note that
        // we specifically don't use the BlockSet type, because doing so would require making a
        // call to EnsureBasicBlockEpoch() to make sure the epoch is up-to-date. However, that
        // can create a new epoch, thus invalidating all existing BlockSet objects, such as
        // reachability information stored in the blocks. To avoid that, we just use a local BitVec.

        BitVecTraits blockVecTraits(fgBBNumMax + 1, this);
        BitVec       uniqueSuccBlocks(BitVecOps::MakeEmpty(&blockVecTraits));
        BasicBlock** jumpTable = switchBlk->bbJumpSwt->bbsDstTab;
        unsigned     jumpCount = switchBlk->bbJumpSwt->bbsCount;
        for (unsigned i = 0; i < jumpCount; i++)
        {
            BasicBlock* targ = jumpTable[i];
            BitVecOps::AddElemD(&blockVecTraits, uniqueSuccBlocks, targ->bbNum);
        }
        // Now we have a set of unique successors.
        unsigned numNonDups = BitVecOps::Count(&blockVecTraits, uniqueSuccBlocks);

        BasicBlock** nonDups = new (getAllocator()) BasicBlock*[numNonDups];

        unsigned nonDupInd = 0;
        // At this point, all unique targets are in "uniqueSuccBlocks".  As we encounter each,
        // add to nonDups, remove from "uniqueSuccBlocks".
        for (unsigned i = 0; i < jumpCount; i++)
        {
            BasicBlock* targ = jumpTable[i];
            if (BitVecOps::IsMember(&blockVecTraits, uniqueSuccBlocks, targ->bbNum))
            {
                nonDups[nonDupInd] = targ;
                nonDupInd++;
                BitVecOps::RemoveElemD(&blockVecTraits, uniqueSuccBlocks, targ->bbNum);
            }
        }

        assert(nonDupInd == numNonDups);
        assert(BitVecOps::Count(&blockVecTraits, uniqueSuccBlocks) == 0);
        res.numDistinctSuccs = numNonDups;
        res.nonDuplicates    = nonDups;
        switchMap->Set(switchBlk, res);
        return res;
    }
}

void Compiler::SwitchUniqueSuccSet::UpdateTarget(CompAllocator alloc,
                                                 BasicBlock*   switchBlk,
                                                 BasicBlock*   from,
                                                 BasicBlock*   to)
{
    assert(switchBlk->bbJumpKind == BBJ_SWITCH); // Precondition.
    unsigned     jmpTabCnt = switchBlk->bbJumpSwt->bbsCount;
    BasicBlock** jmpTab    = switchBlk->bbJumpSwt->bbsDstTab;

    // Is "from" still in the switch table (because it had more than one entry before?)
    bool fromStillPresent = false;
    for (unsigned i = 0; i < jmpTabCnt; i++)
    {
        if (jmpTab[i] == from)
        {
            fromStillPresent = true;
            break;
        }
    }

    // Is "to" already in "this"?
    bool toAlreadyPresent = false;
    for (unsigned i = 0; i < numDistinctSuccs; i++)
    {
        if (nonDuplicates[i] == to)
        {
            toAlreadyPresent = true;
            break;
        }
    }

    // Four cases:
    //   If "from" is still present, and "to" is already present, do nothing
    //   If "from" is still present, and "to" is not, must reallocate to add an entry.
    //   If "from" is not still present, and "to" is not present, write "to" where "from" was.
    //   If "from" is not still present, but "to" is present, remove "from".
    if (fromStillPresent && toAlreadyPresent)
    {
        return;
    }
    else if (fromStillPresent && !toAlreadyPresent)
    {
        // reallocate to add an entry
        BasicBlock** newNonDups = new (alloc) BasicBlock*[numDistinctSuccs + 1];
        memcpy(newNonDups, nonDuplicates, numDistinctSuccs * sizeof(BasicBlock*));
        newNonDups[numDistinctSuccs] = to;
        numDistinctSuccs++;
        nonDuplicates = newNonDups;
    }
    else if (!fromStillPresent && !toAlreadyPresent)
    {
#ifdef DEBUG
        // write "to" where "from" was
        bool foundFrom = false;
#endif // DEBUG
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = to;
#ifdef DEBUG
                foundFrom = true;
#endif // DEBUG
                break;
            }
        }
        assert(foundFrom);
    }
    else
    {
        assert(!fromStillPresent && toAlreadyPresent);
#ifdef DEBUG
        // remove "from".
        bool foundFrom = false;
#endif // DEBUG
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = nonDuplicates[numDistinctSuccs - 1];
                numDistinctSuccs--;
#ifdef DEBUG
                foundFrom = true;
#endif // DEBUG
                break;
            }
        }
        assert(foundFrom);
    }
}

/*****************************************************************************
 *
 *  Simple utility function to remove an entry for a block in the switch desc
 *  map. So it can be called from other phases.
 *
 */
void Compiler::fgInvalidateSwitchDescMapEntry(BasicBlock* block)
{
    // Check if map has no entries yet.
    if (m_switchDescMap != nullptr)
    {
        m_switchDescMap->Remove(block);
    }
}

void Compiler::UpdateSwitchTableTarget(BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to)
{
    if (m_switchDescMap == nullptr)
    {
        return; // No mappings, nothing to do.
    }

    // Otherwise...
    BlockToSwitchDescMap* switchMap = GetSwitchDescMap();
    SwitchUniqueSuccSet*  res       = switchMap->LookupPointer(switchBlk);
    if (res != nullptr)
    {
        // If no result, nothing to do. Otherwise, update it.
        res->UpdateTarget(getAllocator(), switchBlk, from, to);
    }
}

/*****************************************************************************
 *  For a block that is in a handler region, find the first block of the most-nested
 *  handler containing the block.
 */
BasicBlock* Compiler::fgFirstBlockOfHandler(BasicBlock* block)
{
    assert(block->hasHndIndex());
    return ehGetDsc(block->getHndIndex())->ebdHndBeg;
}

/*****************************************************************************
 *  Returns the handler nesting level of the block.
 *  *pFinallyNesting is set to the nesting level of the inner-most
 *  finally-protected try the block is in.
 */

unsigned Compiler::fgGetNestingLevel(BasicBlock* block, unsigned* pFinallyNesting)
{
    unsigned  curNesting = 0;            // How many handlers is the block in
    unsigned  tryFin     = (unsigned)-1; // curNesting when we see innermost finally-protected try
    unsigned  XTnum;
    EHblkDsc* HBtab;

    /* We find the block's handler nesting level by walking over the
       complete exception table and find enclosing clauses. */

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        noway_assert(HBtab->ebdTryBeg && HBtab->ebdHndBeg);

        if (HBtab->HasFinallyHandler() && (tryFin == (unsigned)-1) && bbInTryRegions(XTnum, block))
        {
            tryFin = curNesting;
        }
        else if (bbInHandlerRegions(XTnum, block))
        {
            curNesting++;
        }
    }

    if (tryFin == (unsigned)-1)
    {
        tryFin = curNesting;
    }

    if (pFinallyNesting)
    {
        *pFinallyNesting = curNesting - tryFin;
    }

    return curNesting;
}

/*****************************************************************************
 * This function returns true if tree is a node with a call
 * that unconditionally throws an exception
 */

bool Compiler::fgIsThrow(GenTree* tree)
{
    if ((tree->gtOper != GT_CALL) || (tree->AsCall()->gtCallType != CT_HELPER))
    {
        return false;
    }

    // TODO-Throughput: Replace all these calls to eeFindHelper() with a table based lookup

    if ((tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_OVERFLOW)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_VERIFICATION)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_RNGCHKFAIL)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWDIVZERO)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWNULLREF)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROW)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_RETHROW)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED)) ||
        (tree->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED)))
    {
        noway_assert(tree->gtFlags & GTF_CALL);
        noway_assert(tree->gtFlags & GTF_EXCEPT);
        return true;
    }

    // TODO-CQ: there are a bunch of managed methods in System.ThrowHelper
    // that would be nice to recognize.

    return false;
}

/*****************************************************************************
 * This function returns true if tree is a GT_COMMA node with a call
 * that unconditionally throws an exception
 */

bool Compiler::fgIsCommaThrow(GenTree* tree, bool forFolding /* = false */)
{
    // Instead of always folding comma throws,
    // with stress enabled we only fold half the time

    if (forFolding && compStressCompile(STRESS_FOLD, 50))
    {
        return false; /* Don't fold */
    }

    /* Check for cast of a GT_COMMA with a throw overflow */
    if ((tree->gtOper == GT_COMMA) && (tree->gtFlags & GTF_CALL) && (tree->gtFlags & GTF_EXCEPT))
    {
        return (fgIsThrow(tree->AsOp()->gtOp1));
    }
    return false;
}

//------------------------------------------------------------------------
// fgIsIndirOfAddrOfLocal: Determine whether "tree" is an indirection of a local.
//
// Arguments:
//    tree - The tree node under consideration
//
// Return Value:
//    If "tree" is a indirection (GT_IND, GT_BLK, or GT_OBJ) whose arg is:
//    - an ADDR, whose arg in turn is a LCL_VAR, return that LCL_VAR node;
//    - a LCL_VAR_ADDR, return that LCL_VAR_ADDR;
//    - else nullptr.
//
// static
GenTreeLclVar* Compiler::fgIsIndirOfAddrOfLocal(GenTree* tree)
{
    GenTreeLclVar* res = nullptr;
    if (tree->OperIsIndir())
    {
        GenTree* addr = tree->AsIndir()->Addr();

        // Post rationalization, we can have Indir(Lea(..) trees. Therefore to recognize
        // Indir of addr of a local, skip over Lea in Indir(Lea(base, index, scale, offset))
        // to get to base variable.
        if (addr->OperGet() == GT_LEA)
        {
            // We use this method in backward dataflow after liveness computation - fgInterBlockLocalVarLiveness().
            // Therefore it is critical that we don't miss 'uses' of any local.  It may seem this method overlooks
            // if the index part of the LEA has indir( someAddrOperator ( lclVar ) ) to search for a use but it's
            // covered by the fact we're traversing the expression in execution order and we also visit the index.
            GenTreeAddrMode* lea  = addr->AsAddrMode();
            GenTree*         base = lea->Base();

            if (base != nullptr)
            {
                if (base->OperGet() == GT_IND)
                {
                    return fgIsIndirOfAddrOfLocal(base);
                }
                // else use base as addr
                addr = base;
            }
        }

        if (addr->OperGet() == GT_ADDR)
        {
            GenTree* lclvar = addr->AsOp()->gtOp1;
            if (lclvar->OperGet() == GT_LCL_VAR)
            {
                res = lclvar->AsLclVar();
            }
        }
        else if (addr->OperGet() == GT_LCL_VAR_ADDR)
        {
            res = addr->AsLclVar();
        }
    }
    return res;
}

//------------------------------------------------------------------------------
// fgAddrCouldBeNull : Check whether the address tree can represent null.
//
//
// Arguments:
//    addr     -  Address to check
//
// Return Value:
//    True if address could be null; false otherwise

bool Compiler::fgAddrCouldBeNull(GenTree* addr)
{
    addr = addr->gtEffectiveVal();
    if ((addr->gtOper == GT_CNS_INT) && addr->IsIconHandle())
    {
        return false;
    }
    else if (addr->OperIs(GT_CNS_STR))
    {
        return false;
    }
    else if (addr->gtOper == GT_LCL_VAR)
    {
        unsigned varNum = addr->AsLclVarCommon()->GetLclNum();

        if (lvaIsImplicitByRefLocal(varNum))
        {
            return false;
        }

        LclVarDsc* varDsc = &lvaTable[varNum];

        if (varDsc->lvStackByref)
        {
            return false;
        }
    }
    else if (addr->gtOper == GT_ADDR)
    {
        if (addr->AsOp()->gtOp1->gtOper == GT_CNS_INT)
        {
            GenTree* cns1Tree = addr->AsOp()->gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                // Indirection of some random constant...
                // It is safest just to return true
                return true;
            }
        }

        return false; // we can't have a null address
    }
    else if (addr->gtOper == GT_ADD)
    {
        if (addr->AsOp()->gtOp1->gtOper == GT_CNS_INT)
        {
            GenTree* cns1Tree = addr->AsOp()->gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                if (!fgIsBigOffset(cns1Tree->AsIntCon()->gtIconVal))
                {
                    // Op1 was an ordinary small constant
                    return fgAddrCouldBeNull(addr->AsOp()->gtOp2);
                }
            }
            else // Op1 was a handle represented as a constant
            {
                // Is Op2 also a constant?
                if (addr->AsOp()->gtOp2->gtOper == GT_CNS_INT)
                {
                    GenTree* cns2Tree = addr->AsOp()->gtOp2;
                    // Is this an addition of a handle and constant
                    if (!cns2Tree->IsIconHandle())
                    {
                        if (!fgIsBigOffset(cns2Tree->AsIntCon()->gtIconVal))
                        {
                            // Op2 was an ordinary small constant
                            return false; // we can't have a null address
                        }
                    }
                }
            }
        }
        else
        {
            // Op1 is not a constant
            // What about Op2?
            if (addr->AsOp()->gtOp2->gtOper == GT_CNS_INT)
            {
                GenTree* cns2Tree = addr->AsOp()->gtOp2;
                // Is this an addition of a small constant
                if (!cns2Tree->IsIconHandle())
                {
                    if (!fgIsBigOffset(cns2Tree->AsIntCon()->gtIconVal))
                    {
                        // Op2 was an ordinary small constant
                        return fgAddrCouldBeNull(addr->AsOp()->gtOp1);
                    }
                }
            }
        }
    }
    return true; // default result: addr could be null
}

bool Compiler::fgCastNeeded(GenTree* tree, var_types toType)
{
    //
    // If tree is a relop and we need an 4-byte integer
    //  then we never need to insert a cast
    //
    if ((tree->OperKind() & GTK_RELOP) && (genActualType(toType) == TYP_INT))
    {
        return false;
    }

    var_types fromType;

    //
    // Is the tree as GT_CAST or a GT_CALL ?
    //
    if (tree->OperGet() == GT_CAST)
    {
        fromType = tree->CastToType();
    }
    else if (tree->OperGet() == GT_CALL)
    {
        fromType = (var_types)tree->AsCall()->gtReturnType;
    }
    else
    {
        fromType = tree->TypeGet();
    }

    //
    // If both types are the same then an additional cast is not necessary
    //
    if (toType == fromType)
    {
        return false;
    }
    //
    // If the sign-ness of the two types are different then a cast is necessary
    //
    if (varTypeIsUnsigned(toType) != varTypeIsUnsigned(fromType))
    {
        return true;
    }
    //
    // If the from type is the same size or smaller then an additional cast is not necessary
    //
    if (genTypeSize(toType) >= genTypeSize(fromType))
    {
        return false;
    }

    //
    // Looks like we will need the cast
    //
    return true;
}

// If assigning to a local var, add a cast if the target is
// marked as NormalizedOnStore. Returns true if any change was made
GenTree* Compiler::fgDoNormalizeOnStore(GenTree* tree)
{
    //
    // Only normalize the stores in the global morph phase
    //
    if (fgGlobalMorph)
    {
        noway_assert(tree->OperGet() == GT_ASG);

        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->gtOper == GT_LCL_VAR && genActualType(op1->TypeGet()) == TYP_INT)
        {
            // Small-typed arguments and aliased locals are normalized on load.
            // Other small-typed locals are normalized on store.
            // If it is an assignment to one of the latter, insert the cast on RHS
            unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
            LclVarDsc* varDsc = &lvaTable[varNum];

            if (varDsc->lvNormalizeOnStore())
            {
                noway_assert(op1->gtType <= TYP_INT);
                op1->gtType = TYP_INT;

                if (fgCastNeeded(op2, varDsc->TypeGet()))
                {
                    op2                 = gtNewCastNode(TYP_INT, op2, false, varDsc->TypeGet());
                    tree->AsOp()->gtOp2 = op2;

                    // Propagate GTF_COLON_COND
                    op2->gtFlags |= (tree->gtFlags & GTF_COLON_COND);
                }
            }
        }
    }

    return tree;
}

/*****************************************************************************
 *
 *  Create a new statement from tree and wire the links up.
 */
Statement* Compiler::fgNewStmtFromTree(GenTree* tree, BasicBlock* block, IL_OFFSETX offs)
{
    Statement* stmt = gtNewStmt(tree, offs);

    if (fgStmtListThreaded)
    {
        gtSetStmtInfo(stmt);
        fgSetStmtSeq(stmt);
    }

#if DEBUG
    if (block != nullptr)
    {
        fgDebugCheckNodeLinks(block, stmt);
    }
#endif

    return stmt;
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree)
{
    return fgNewStmtFromTree(tree, nullptr, BAD_IL_OFFSET);
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree, BasicBlock* block)
{
    return fgNewStmtFromTree(tree, block, BAD_IL_OFFSET);
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree, IL_OFFSETX offs)
{
    return fgNewStmtFromTree(tree, nullptr, offs);
}

//------------------------------------------------------------------------
// fgFindBlockILOffset: Given a block, find the IL offset corresponding to the first statement
//      in the block with a legal IL offset. Skip any leading statements that have BAD_IL_OFFSET.
//      If no statement has an initialized statement offset (including the case where there are
//      no statements in the block), then return BAD_IL_OFFSET. This function is used when
//      blocks are split or modified, and we want to maintain the IL offset as much as possible
//      to preserve good debugging behavior.
//
// Arguments:
//      block - The block to check.
//
// Return Value:
//      The first good IL offset of a statement in the block, or BAD_IL_OFFSET if such an IL offset
//      cannot be found.
//
IL_OFFSET Compiler::fgFindBlockILOffset(BasicBlock* block)
{
    // This function searches for IL offsets in statement nodes, so it can't be used in LIR. We
    // could have a similar function for LIR that searches for GT_IL_OFFSET nodes.
    assert(!block->IsLIR());

    for (Statement* stmt : block->Statements())
    {
        if (stmt->GetILOffsetX() != BAD_IL_OFFSET)
        {
            return jitGetILoffs(stmt->GetILOffsetX());
        }
    }

    return BAD_IL_OFFSET;
}

/*****************************************************************************
 *
 * Remove a useless statement from a basic block.
 *
 */

void Compiler::fgRemoveStmt(BasicBlock* block, Statement* stmt)
{
    assert(fgOrder == FGOrderTree);

#ifdef DEBUG
    if (verbose &&
        stmt->GetRootNode()->gtOper != GT_NOP) // Don't print if it is a GT_NOP. Too much noise from the inliner.
    {
        printf("\nRemoving statement ");
        gtDispStmt(stmt);
        printf(" in " FMT_BB " as useless:\n", block->bbNum);
    }
#endif // DEBUG

    if (opts.compDbgCode && stmt->GetPrevStmt() != stmt && stmt->GetILOffsetX() != BAD_IL_OFFSET)
    {
        /* TODO: For debuggable code, should we remove significant
           statement boundaries. Or should we leave a GT_NO_OP in its place? */
    }

    Statement* firstStmt = block->firstStmt();
    if (firstStmt == stmt) // Is it the first statement in the list?
    {
        if (firstStmt->GetNextStmt() == nullptr)
        {
            assert(firstStmt == block->lastStmt());

            /* this is the only statement - basic block becomes empty */
            block->bbStmtList = nullptr;
        }
        else
        {
            block->bbStmtList = firstStmt->GetNextStmt();
            block->bbStmtList->SetPrevStmt(firstStmt->GetPrevStmt());
        }
    }
    else if (stmt == block->lastStmt()) // Is it the last statement in the list?
    {
        stmt->GetPrevStmt()->SetNextStmt(nullptr);
        block->bbStmtList->SetPrevStmt(stmt->GetPrevStmt());
    }
    else // The statement is in the middle.
    {
        assert(stmt->GetPrevStmt() != nullptr && stmt->GetNextStmt() != nullptr);

        Statement* prev = stmt->GetPrevStmt();

        prev->SetNextStmt(stmt->GetNextStmt());
        stmt->GetNextStmt()->SetPrevStmt(prev);
    }

    noway_assert(!optValnumCSE_phase);

    fgStmtRemoved = true;

#ifdef DEBUG
    if (verbose)
    {
        if (block->bbStmtList == nullptr)
        {
            printf("\n" FMT_BB " becomes empty", block->bbNum);
        }
        printf("\n");
    }
#endif // DEBUG
}

/******************************************************************************/
// Returns true if the operator is involved in control-flow
// TODO-Cleanup: Move this into genTreeKinds in genTree.h

inline bool OperIsControlFlow(genTreeOps oper)
{
    switch (oper)
    {
        case GT_JTRUE:
        case GT_JCMP:
        case GT_JCC:
        case GT_SWITCH:
        case GT_LABEL:

        case GT_CALL:
        case GT_JMP:

        case GT_RETURN:
        case GT_RETFILT:
#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            return true;

        default:
            return false;
    }
}

/******************************************************************************
 *  Tries to throw away a stmt. The statement can be anywhere in block->bbStmtList.
 *  Returns true if it did remove the statement.
 */

bool Compiler::fgCheckRemoveStmt(BasicBlock* block, Statement* stmt)
{
    if (opts.compDbgCode)
    {
        return false;
    }

    GenTree*   tree = stmt->GetRootNode();
    genTreeOps oper = tree->OperGet();

    if (OperIsControlFlow(oper) || GenTree::OperIsHWIntrinsic(oper) || oper == GT_NO_OP)
    {
        return false;
    }

    // TODO: Use a recursive version of gtNodeHasSideEffects()
    if (tree->gtFlags & GTF_SIDE_EFFECT)
    {
        return false;
    }

    fgRemoveStmt(block, stmt);
    return true;
}

/*****************************************************************************
 *
 *  Is the BasicBlock bJump a forward branch?
 *   Optionally bSrc can be supplied to indicate that
 *   bJump must be forward with respect to bSrc
 */
bool Compiler::fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bSrc /* = NULL */)
{
    bool result = false;

    if ((bJump->bbJumpKind == BBJ_COND) || (bJump->bbJumpKind == BBJ_ALWAYS))
    {
        BasicBlock* bDest = bJump->bbJumpDest;
        BasicBlock* bTemp = (bSrc == nullptr) ? bJump : bSrc;

        while (true)
        {
            bTemp = bTemp->bbNext;

            if (bTemp == nullptr)
            {
                break;
            }

            if (bTemp == bDest)
            {
                result = true;
                break;
            }
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Returns true if it is allowable (based upon the EH regions)
 *  to place block bAfter immediately after bBefore. It is allowable
 *  if the 'bBefore' and 'bAfter' blocks are in the exact same EH region.
 */

bool Compiler::fgEhAllowsMoveBlock(BasicBlock* bBefore, BasicBlock* bAfter)
{
    return BasicBlock::sameEHRegion(bBefore, bAfter);
}

// return true if there is a possibility that the method has a loop (a backedge is present)
bool Compiler::fgMightHaveLoop()
{
    // Don't use a BlockSet for this temporary bitset of blocks: we don't want to have to call EnsureBasicBlockEpoch()
    // and potentially change the block epoch.

    BitVecTraits blockVecTraits(fgBBNumMax + 1, this);
    BitVec       blocksSeen(BitVecOps::MakeEmpty(&blockVecTraits));

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        BitVecOps::AddElemD(&blockVecTraits, blocksSeen, block->bbNum);

        for (BasicBlock* succ : block->GetAllSuccs(this))
        {
            if (BitVecOps::IsMember(&blockVecTraits, blocksSeen, succ->bbNum))
            {
                return true;
            }
        }
    }
    return false;
}

// We have two edges (bAlt => bCur) and (bCur => bNext).
//
// Returns true if the weight of (bAlt => bCur)
//  is greater than the weight of (bCur => bNext).
// We compare the edge weights if we have valid edge weights
//  otherwise we compare blocks weights.
//
bool Compiler::fgIsBetterFallThrough(BasicBlock* bCur, BasicBlock* bAlt)
{
    // bCur can't be NULL and must be a fall through bbJumpKind
    noway_assert(bCur != nullptr);
    noway_assert(bCur->bbFallsThrough());
    noway_assert(bAlt != nullptr);

    // We only handle the cases when bAlt is a BBJ_ALWAYS or a BBJ_COND
    if ((bAlt->bbJumpKind != BBJ_ALWAYS) && (bAlt->bbJumpKind != BBJ_COND))
    {
        return false;
    }

    // if bAlt doesn't jump to bCur it can't be a better fall through than bCur
    if (bAlt->bbJumpDest != bCur)
    {
        return false;
    }

    // Currently bNext is the fall through for bCur
    BasicBlock* bNext = bCur->bbNext;
    noway_assert(bNext != nullptr);

    // We will set result to true if bAlt is a better fall through than bCur
    bool result;
    if (fgHaveValidEdgeWeights)
    {
        // We will compare the edge weight for our two choices
        flowList* edgeFromAlt = fgGetPredForBlock(bCur, bAlt);
        flowList* edgeFromCur = fgGetPredForBlock(bNext, bCur);
        noway_assert(edgeFromCur != nullptr);
        noway_assert(edgeFromAlt != nullptr);

        result = (edgeFromAlt->edgeWeightMin() > edgeFromCur->edgeWeightMax());
    }
    else
    {
        if (bAlt->bbJumpKind == BBJ_ALWAYS)
        {
            // Our result is true if bAlt's weight is more than bCur's weight
            result = (bAlt->bbWeight > bCur->bbWeight);
        }
        else
        {
            noway_assert(bAlt->bbJumpKind == BBJ_COND);
            // Our result is true if bAlt's weight is more than twice bCur's weight
            result = (bAlt->bbWeight > (2 * bCur->bbWeight));
        }
    }
    return result;
}

// Sequences the tree.
// prevTree is what gtPrev of the first node in execution order gets set to.
// Returns the first node (execution order) in the sequenced tree.
GenTree* Compiler::fgSetTreeSeq(GenTree* tree, GenTree* prevTree, bool isLIR)
{
    GenTree list;

    if (prevTree == nullptr)
    {
        prevTree = &list;
    }
    fgTreeSeqLst = prevTree;
    fgTreeSeqNum = 0;
    fgTreeSeqBeg = nullptr;
    fgSetTreeSeqHelper(tree, isLIR);

    GenTree* result = prevTree->gtNext;
    if (prevTree == &list)
    {
        list.gtNext->gtPrev = nullptr;
    }

    return result;
}

/*****************************************************************************
 *
 *  Assigns sequence numbers to the given tree and its sub-operands, and
 *  threads all the nodes together via the 'gtNext' and 'gtPrev' fields.
 *  Uses 'global' - fgTreeSeqLst
 */

void Compiler::fgSetTreeSeqHelper(GenTree* tree, bool isLIR)
{
    genTreeOps oper;
    unsigned   kind;

    noway_assert(tree);
    assert(!IsUninitialized(tree));

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Is this a leaf/constant node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        fgSetTreeSeqFinish(tree, isLIR);
        return;
    }

    // Special handling for dynamic block ops.
    if (tree->OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK))
    {
        GenTreeDynBlk* dynBlk    = tree->AsDynBlk();
        GenTree*       sizeNode  = dynBlk->gtDynamicSize;
        GenTree*       dstAddr   = dynBlk->Addr();
        GenTree*       src       = dynBlk->Data();
        bool           isReverse = ((dynBlk->gtFlags & GTF_REVERSE_OPS) != 0);
        if (dynBlk->gtEvalSizeFirst)
        {
            fgSetTreeSeqHelper(sizeNode, isLIR);
        }

        // We either have a DYN_BLK or a STORE_DYN_BLK. If the latter, we have a
        // src (the Data to be stored), and isReverse tells us whether to evaluate
        // that before dstAddr.
        if (isReverse && (src != nullptr))
        {
            fgSetTreeSeqHelper(src, isLIR);
        }
        fgSetTreeSeqHelper(dstAddr, isLIR);
        if (!isReverse && (src != nullptr))
        {
            fgSetTreeSeqHelper(src, isLIR);
        }
        if (!dynBlk->gtEvalSizeFirst)
        {
            fgSetTreeSeqHelper(sizeNode, isLIR);
        }
        fgSetTreeSeqFinish(dynBlk, isLIR);
        return;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->gtGetOp2IfPresent();

        // Special handling for GT_LIST
        if (tree->OperGet() == GT_LIST)
        {
            // First, handle the list items, which will be linked in forward order.
            // As we go, we will link the GT_LIST nodes in reverse order - we will number
            // them and update fgTreeSeqList in a subsequent traversal.
            GenTree* nextList = tree;
            GenTree* list     = nullptr;
            while (nextList != nullptr && nextList->OperGet() == GT_LIST)
            {
                list              = nextList;
                GenTree* listItem = list->AsOp()->gtOp1;
                fgSetTreeSeqHelper(listItem, isLIR);
                nextList = list->AsOp()->gtOp2;
                if (nextList != nullptr)
                {
                    nextList->gtNext = list;
                }
                list->gtPrev = nextList;
            }
            // Next, handle the GT_LIST nodes.
            // Note that fgSetTreeSeqFinish() sets the gtNext to null, so we need to capture the nextList
            // before we call that method.
            nextList = list;
            do
            {
                assert(list != nullptr);
                list     = nextList;
                nextList = list->gtNext;
                fgSetTreeSeqFinish(list, isLIR);
            } while (list != tree);
            return;
        }

        /* Special handling for AddrMode */
        if (tree->OperIsAddrMode())
        {
            bool reverse = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);
            if (reverse)
            {
                assert(op1 != nullptr && op2 != nullptr);
                fgSetTreeSeqHelper(op2, isLIR);
            }
            if (op1 != nullptr)
            {
                fgSetTreeSeqHelper(op1, isLIR);
            }
            if (!reverse && op2 != nullptr)
            {
                fgSetTreeSeqHelper(op2, isLIR);
            }

            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Check for a nilary operator */

        if (op1 == nullptr)
        {
            noway_assert(op2 == nullptr);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Is this a unary operator?
         * Although UNARY GT_IND has a special structure */

        if (oper == GT_IND)
        {
            /* Visit the indirection first - op2 may point to the
             * jump Label for array-index-out-of-range */

            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Now this is REALLY a unary operator */

        if (!op2)
        {
            /* Visit the (only) operand and we're done */

            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /*
           For "real" ?: operators, we make sure the order is
           as follows:

               condition
               1st operand
               GT_COLON
               2nd operand
               GT_QMARK
        */

        if (oper == GT_QMARK)
        {
            noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

            fgSetTreeSeqHelper(op1, isLIR);
            // Here, for the colon, the sequence does not actually represent "order of evaluation":
            // one or the other of the branches is executed, not both.  Still, to make debugging checks
            // work, we want the sequence to match the order in which we'll generate code, which means
            // "else" clause then "then" clause.
            fgSetTreeSeqHelper(op2->AsColon()->ElseNode(), isLIR);
            fgSetTreeSeqHelper(op2, isLIR);
            fgSetTreeSeqHelper(op2->AsColon()->ThenNode(), isLIR);

            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        if (oper == GT_COLON)
        {
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* This is a binary operator */

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            fgSetTreeSeqHelper(op2, isLIR);
            fgSetTreeSeqHelper(op1, isLIR);
        }
        else
        {
            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqHelper(op2, isLIR);
        }

        fgSetTreeSeqFinish(tree, isLIR);
        return;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_FIELD:
            noway_assert(tree->AsField()->gtFldObj == nullptr);
            break;

        case GT_CALL:

            /* We'll evaluate the 'this' argument value first */
            if (tree->AsCall()->gtCallThisArg != nullptr)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallThisArg->GetNode(), isLIR);
            }

            for (GenTreeCall::Use& use : tree->AsCall()->Args())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }

            for (GenTreeCall::Use& use : tree->AsCall()->LateArgs())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }

            if ((tree->AsCall()->gtCallType == CT_INDIRECT) && (tree->AsCall()->gtCallCookie != nullptr))
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallCookie, isLIR);
            }

            if (tree->AsCall()->gtCallType == CT_INDIRECT)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallAddr, isLIR);
            }

            if (tree->AsCall()->gtControlExpr)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtControlExpr, isLIR);
            }

            break;

        case GT_ARR_ELEM:

            fgSetTreeSeqHelper(tree->AsArrElem()->gtArrObj, isLIR);

            unsigned dim;
            for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
            {
                fgSetTreeSeqHelper(tree->AsArrElem()->gtArrInds[dim], isLIR);
            }

            break;

        case GT_ARR_OFFSET:
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtOffset, isLIR);
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtIndex, isLIR);
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtArrObj, isLIR);
            break;

        case GT_PHI:
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }
            break;

        case GT_FIELD_LIST:
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }
            break;

        case GT_CMPXCHG:
            // Evaluate the trees left to right
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpLocation, isLIR);
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpValue, isLIR);
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpComparand, isLIR);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            // Evaluate the trees left to right
            fgSetTreeSeqHelper(tree->AsBoundsChk()->gtIndex, isLIR);
            fgSetTreeSeqHelper(tree->AsBoundsChk()->gtArrLen, isLIR);
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            noway_assert(!"DYN_BLK nodes should be sequenced as a special case");
            break;

        case GT_INDEX_ADDR:
            // Evaluate the array first, then the index....
            assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);
            fgSetTreeSeqHelper(tree->AsIndexAddr()->Arr(), isLIR);
            fgSetTreeSeqHelper(tree->AsIndexAddr()->Index(), isLIR);
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
            noway_assert(!"unexpected operator");
#endif // DEBUG
            break;
    }

    fgSetTreeSeqFinish(tree, isLIR);
}

void Compiler::fgSetTreeSeqFinish(GenTree* tree, bool isLIR)
{
    // If we are sequencing for LIR:
    // - Clear the reverse ops flag
    // - If we are processing a node that does not appear in LIR, do not add it to the list.
    if (isLIR)
    {
        tree->gtFlags &= ~GTF_REVERSE_OPS;

        if (tree->OperIs(GT_LIST, GT_ARGPLACE))
        {
            return;
        }
    }

    /* Append to the node list */
    ++fgTreeSeqNum;

#ifdef DEBUG
    tree->gtSeqNum = fgTreeSeqNum;

    if (verbose & 0)
    {
        printf("SetTreeOrder: ");
        printTreeID(fgTreeSeqLst);
        printf(" followed by ");
        printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    fgTreeSeqLst->gtNext = tree;
    tree->gtNext         = nullptr;
    tree->gtPrev         = fgTreeSeqLst;
    fgTreeSeqLst         = tree;

    /* Remember the very first node */

    if (!fgTreeSeqBeg)
    {
        fgTreeSeqBeg = tree;
        assert(tree->gtSeqNum == 1);
    }
}

/*****************************************************************************/

void Compiler::fgSetStmtSeq(Statement* stmt)
{
    GenTree list; // helper node that we use to start the StmtList
                  // It's located in front of the first node in the list

    /* Assign numbers and next/prev links for this tree */

    fgTreeSeqNum = 0;
    fgTreeSeqLst = &list;
    fgTreeSeqBeg = nullptr;

    fgSetTreeSeqHelper(stmt->GetRootNode(), false);

    /* Record the address of the first node */

    stmt->SetTreeList(fgTreeSeqBeg);

#ifdef DEBUG

    if (list.gtNext->gtPrev != &list)
    {
        printf("&list ");
        printTreeID(&list);
        printf(" != list.next->prev ");
        printTreeID(list.gtNext->gtPrev);
        printf("\n");
        goto BAD_LIST;
    }

    GenTree* temp;
    GenTree* last;
    for (temp = list.gtNext, last = &list; temp != nullptr; last = temp, temp = temp->gtNext)
    {
        if (temp->gtPrev != last)
        {
            printTreeID(temp);
            printf("->gtPrev = ");
            printTreeID(temp->gtPrev);
            printf(", but last = ");
            printTreeID(last);
            printf("\n");

        BAD_LIST:;

            printf("\n");
            gtDispTree(stmt->GetRootNode());
            printf("\n");

            for (GenTree* bad = &list; bad != nullptr; bad = bad->gtNext)
            {
                printf("  entry at ");
                printTreeID(bad);
                printf(" (prev=");
                printTreeID(bad->gtPrev);
                printf(",next=)");
                printTreeID(bad->gtNext);
                printf("\n");
            }

            printf("\n");
            noway_assert(!"Badly linked tree");
            break;
        }
    }
#endif // DEBUG

    /* Fix the first node's 'prev' link */

    noway_assert(list.gtNext->gtPrev == &list);
    list.gtNext->gtPrev = nullptr;

#ifdef DEBUG
    /* Keep track of the highest # of tree nodes */

    if (BasicBlock::s_nMaxTrees < fgTreeSeqNum)
    {
        BasicBlock::s_nMaxTrees = fgTreeSeqNum;
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// fgGetFirstNode: Get the first node in the tree, in execution order
//
// Arguments:
//    tree - The top node of the tree of interest
//
// Return Value:
//    The first node in execution order, that belongs to tree.
//
// Assumptions:
//     'tree' must either be a leaf, or all of its constituent nodes must be contiguous
//     in execution order.
//     TODO-Cleanup: Add a debug-only method that verifies this.

/* static */
GenTree* Compiler::fgGetFirstNode(GenTree* tree)
{
    GenTree* child = tree;
    while (child->NumChildren() > 0)
    {
        if (child->OperIsBinary() && child->IsReverseOp())
        {
            child = child->GetChild(1);
        }
        else
        {
            child = child->GetChild(0);
        }
    }
    return child;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkThrowCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    // If this tree doesn't have the EXCEPT flag set, then there is no
    // way any of the child nodes could throw, so we can stop recursing.
    if (!(tree->gtFlags & GTF_EXCEPT))
    {
        return Compiler::WALK_SKIP_SUBTREES;
    }

    switch (tree->gtOper)
    {
        case GT_MUL:
        case GT_ADD:
        case GT_SUB:
        case GT_CAST:
            if (tree->gtOverflow())
            {
                return Compiler::WALK_ABORT;
            }
            break;

        case GT_INDEX:
        case GT_INDEX_ADDR:
            // These two call CORINFO_HELP_RNGCHKFAIL for Debug code
            if (tree->gtFlags & GTF_INX_RNGCHK)
            {
                return Compiler::WALK_ABORT;
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
            return Compiler::WALK_ABORT;

        default:
            break;
    }

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkLocAllocCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->gtOper == GT_LCLHEAP)
    {
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkQmarkCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->gtOper == GT_QMARK)
    {
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

//------------------------------------------------------------------------
// fgUseThrowHelperBlocks: Determinate does compiler use throw helper blocks.
//
// Note:
//   For debuggable code, codegen will generate the 'throw' code inline.
// Return Value:
//    true if 'throw' helper block should be created.
bool Compiler::fgUseThrowHelperBlocks()
{
    return !opts.compDbgCode;
}

//------------------------------------------------------------------------
// fgCheckCallArgUpdate: check if we are replacing a call argument and add GT_PUTARG_TYPE if necessary.
//
// Arguments:
//    parent   - the parent that could be a call;
//    child    - the new child node;
//    origType - the original child type;
//
// Returns:
//   PUT_ARG_TYPE node if it is needed, nullptr otherwise.
//
GenTree* Compiler::fgCheckCallArgUpdate(GenTree* parent, GenTree* child, var_types origType)
{
    if ((parent == nullptr) || !parent->IsCall())
    {
        return nullptr;
    }
    const var_types newType = child->TypeGet();
    if (newType == origType)
    {
        return nullptr;
    }
    if (varTypeIsStruct(origType) || (genTypeSize(origType) == genTypeSize(newType)))
    {
        assert(!varTypeIsStruct(newType));
        return nullptr;
    }
    GenTree* putArgType = gtNewOperNode(GT_PUTARG_TYPE, origType, child);
#if defined(DEBUG)
    if (verbose)
    {
        printf("For call [%06d] the new argument's type [%06d]", dspTreeID(parent), dspTreeID(child));
        printf(" does not match the original type size, add a GT_PUTARG_TYPE [%06d]\n", dspTreeID(parent));
    }
#endif
    return putArgType;
}
