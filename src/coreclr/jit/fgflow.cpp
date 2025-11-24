// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Predecessors and Successors

//------------------------------------------------------------------------
// fgGetPredForBlock: Find and return the predecessor edge corresponding to a given predecessor block.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to find in the predecessor list.
//
// Return Value:
//    The FlowEdge edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
FlowEdge* Compiler::fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred)
{
    assert(block);
    assert(blockPred);

    for (FlowEdge* const pred : block->PredEdges())
    {
        if (blockPred == pred->getSourceBlock())
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
//    The FlowEdge edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
FlowEdge* Compiler::fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred, FlowEdge*** ptrToPred)
{
    assert(block);
    assert(blockPred);
    assert(ptrToPred);

    FlowEdge** predPrevAddr;
    FlowEdge*  pred;

    for (predPrevAddr = &block->bbPreds, pred = *predPrevAddr; pred != nullptr;
         predPrevAddr = pred->getNextPredEdgeRef(), pred = *predPrevAddr)
    {
        if (blockPred == pred->getSourceBlock())
        {
            *ptrToPred = predPrevAddr;
            return pred;
        }
    }

    *ptrToPred = nullptr;
    return nullptr;
}

//------------------------------------------------------------------------
// fgAddRefPred: Increment block->bbRefs by one and add "blockPred" to the predecessor list of "block".
//
// Arguments:
//    initializingPreds -- Optional (default: false). Only set to "true" when the initial preds computation is
//                         happening.
//
//    block     -- A block to operate on.
//    blockPred -- The predecessor block to add to the predecessor list.
//    oldEdge   -- Optional (default: nullptr). If non-nullptr, and a new edge is created (and the dup count
//                 of an existing edge is not just incremented), the edge weights are copied from this edge.
//
// Return Value:
//    The flow edge representing the predecessor.
//
// Notes:
//    -- block->bbRefs is incremented by one to account for the increase in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a new flow edge is created (but not if an existing flow edge dup count is incremented),
//       indicating that the flow graph shape has changed.
//
template <bool initializingPreds>
FlowEdge* Compiler::fgAddRefPred(BasicBlock* block, BasicBlock* blockPred, FlowEdge* oldEdge /* = nullptr */)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(fgPredsComputed ^ initializingPreds);

    block->bbRefs++;

    // Keep the predecessor list in lowest to highest bbID order.
    //
    // If we are initializing preds, we rely on the fact that we are adding references in increasing
    // order of blockPred->bbID to avoid searching the list.
    //
    FlowEdge*  flow = nullptr;
    FlowEdge** listp;

    if (initializingPreds)
    {
        // List is sorted order and we're adding references in
        // increasing blockPred->bbID order. The only possible
        // dup list entry is the last one.
        //
        listp              = &block->bbPreds;
        FlowEdge* flowLast = block->bbLastPred;
        if (flowLast != nullptr)
        {
            listp = flowLast->getNextPredEdgeRef();

            assert(flowLast->getSourceBlock()->bbID <= blockPred->bbID);

            if (flowLast->getSourceBlock() == blockPred)
            {
                flow = flowLast;
            }
        }
    }
    else
    {
        // References are added randomly, so we have to search.
        //
        listp = fgGetPredInsertPoint(blockPred, block);

        if ((*listp != nullptr) && ((*listp)->getSourceBlock() == blockPred))
        {
            flow = *listp;
        }
    }

    if (flow != nullptr)
    {
        // The predecessor block already exists in the flow list; simply add to its duplicate count.
        //
        noway_assert(flow->getDupCount());
        flow->incrementDupCount();
    }
    else
    {
        // Create a new edge
        //
        // We may be disallowing edge creation, except for edges targeting special blocks.
        //
        assert(fgSafeFlowEdgeCreation || block->HasFlag(BBF_CAN_ADD_PRED));

#if MEASURE_BLOCK_SIZE
        genFlowNodeCnt += 1;
        genFlowNodeSize += sizeof(FlowEdge);
#endif // MEASURE_BLOCK_SIZE

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;

        // Create new edge in the list in the correct ordered location.
        //
        flow = new (this, CMK_FlowEdge) FlowEdge(blockPred, block, *listp);
        flow->incrementDupCount();
        *listp = flow;

        if (initializingPreds)
        {
            block->bbLastPred = flow;
        }
        else if (oldEdge != nullptr)
        {
            // Copy likelihood from old edge.
            //
            flow->setLikelihood(oldEdge->getLikelihood());
            flow->setHeuristicBased(oldEdge->isHeuristicBased());
        }
    }

    // Pred list should (still) be ordered.
    //
    assert(block->checkPredListOrder());

    return flow;
}

// Add explicit instantiations.
template FlowEdge* Compiler::fgAddRefPred<false>(BasicBlock* block,
                                                 BasicBlock* blockPred,
                                                 FlowEdge*   oldEdge /* = nullptr */);
template FlowEdge* Compiler::fgAddRefPred<true>(BasicBlock* block,
                                                BasicBlock* blockPred,
                                                FlowEdge*   oldEdge /* = nullptr */);

//------------------------------------------------------------------------
// fgRemoveRefPred: Decrements the reference count of `edge`, removing it from its successor block's pred list
// if the reference count is zero.
//
// Arguments:
//    edge -- The FlowEdge* to decrement the reference count of.
//
// Notes:
//    -- succBlock->bbRefs is decremented by one to account for the reduction in incoming edges.
//    -- fgModified is set if a flow edge is removed, indicating that the flow graph shape has changed.
//
void Compiler::fgRemoveRefPred(FlowEdge* edge)
{
    assert(edge != nullptr);
    assert(fgPredsComputed);

    BasicBlock* predBlock = edge->getSourceBlock();
    BasicBlock* succBlock = edge->getDestinationBlock();
    assert(predBlock != nullptr);
    assert(succBlock != nullptr);

    succBlock->bbRefs--;

    assert(edge->getDupCount() > 0);
    edge->decrementDupCount();

    if (edge->getDupCount() == 0)
    {
        // Splice out the predecessor edge in succBlock's pred list, since it's no longer necessary.
        FlowEdge** ptrToPred;
        FlowEdge*  pred = fgGetPredForBlock(succBlock, predBlock, &ptrToPred);
        *ptrToPred      = pred->getNextPredEdge();

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;
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
//
// Notes:
//    block->bbRefs is decremented to account for the reduction in incoming edges.
//
FlowEdge* Compiler::fgRemoveAllRefPreds(BasicBlock* block, BasicBlock* blockPred)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(fgPredsComputed);
    assert(block->countOfInEdges() > 0);

    FlowEdge** ptrToPred;
    FlowEdge*  pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    assert(pred != nullptr);
    assert(pred->getDupCount() > 0);

    assert(block->bbRefs >= pred->getDupCount());
    block->bbRefs -= pred->getDupCount();

    // Now splice out the predecessor edge.
    *ptrToPred = pred->getNextPredEdge();

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return pred;
}

//------------------------------------------------------------------------
// fgRemoveBlockAsPred: Removes all the appearances of block as a predecessor of other blocks
// (namely, as a member of the predecessor lists of this block's successors).
//
// Arguments:
//    block -- A block to operate on.
//
void Compiler::fgRemoveBlockAsPred(BasicBlock* block)
{
    assert(block != nullptr);

    switch (block->GetKind())
    {
        case BBJ_CALLFINALLY:
        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
            fgRemoveRefPred(block->GetTargetEdge());
            break;

        case BBJ_COND:
            fgRemoveRefPred(block->GetTrueEdge());
            fgRemoveRefPred(block->GetFalseEdge());
            break;

        case BBJ_EHFINALLYRET:
        {
            BBJumpTable* const ehfDesc = block->GetEhfTargets();
            for (unsigned i = 0; i < ehfDesc->GetSuccCount(); i++)
            {
                fgRemoveAllRefPreds(ehfDesc->GetSucc(i)->getDestinationBlock(), block);
            }
            break;
        }

        case BBJ_EHFAULTRET:
        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:
        {
            BBswtDesc* const swtDesc = block->GetSwitchTargets();
            for (unsigned i = 0; i < swtDesc->GetSuccCount(); i++)
            {
                fgRemoveAllRefPreds(swtDesc->GetSucc(i)->getDestinationBlock(), block);
            }
            break;
        }

        default:
            noway_assert(!"Block doesn't have a valid bbKind!!!!");
            break;
    }
}

//------------------------------------------------------------------------
// fgGetPredInsertPoint: Searches newTarget->bbPreds for where to insert an edge from blockPred.
//
// Arguments:
//    blockPred -- The block we want to make a predecessor of newTarget (it could already be one).
//    newTarget -- The block whose pred list we will search.
//
// Return Value:
//    Returns a pointer to the next pointer of an edge in newTarget's pred list.
//    A new edge from blockPred to newTarget can be inserted here
//    without disrupting bbPreds' sorting invariant.
//
FlowEdge** Compiler::fgGetPredInsertPoint(BasicBlock* blockPred, BasicBlock* newTarget)
{
    assert(blockPred != nullptr);
    assert(newTarget != nullptr);
    assert(fgPredsComputed);

    FlowEdge** listp = &newTarget->bbPreds;

    // Search pred list for insertion point
    //
    while ((*listp != nullptr) && ((*listp)->getSourceBlock()->bbID < blockPred->bbID))
    {
        listp = (*listp)->getNextPredEdgeRef();
    }

    return listp;
}

//------------------------------------------------------------------------
// fgRedirectEdge: Sets the given edge's target block to 'newTarget',
// updating pred lists as needed.
//
// Arguments:
//    edge -- The edge to update. Note that this is a reference type
//    to support automatic updates to BasicBlock members (bbTargetEdge et al).
//    newTarget -- The new successor of the edge.
//
void Compiler::fgRedirectEdge(FlowEdge*& edge, BasicBlock* newTarget)
{
    assert(edge != nullptr);
    assert(newTarget != nullptr);

    BasicBlock* const block    = edge->getSourceBlock();
    const unsigned    dupCount = edge->getDupCount();
    fgRemoveAllRefPreds(edge->getDestinationBlock(), block);

    FlowEdge** predListPtr = fgGetPredInsertPoint(block, newTarget);
    FlowEdge*  predEdge    = *predListPtr;

    if ((predEdge != nullptr) && (predEdge->getSourceBlock() == block))
    {
        edge = predEdge;
        edge->incrementDupCount(dupCount);
    }
    else
    {
        edge->setNextPredEdge(predEdge);
        *predListPtr = edge;
        edge->setDestinationBlock(newTarget);
    }

    newTarget->bbRefs += dupCount;

    // Pred list of target should still be ordered
    assert(newTarget->checkPredListOrder());
}
