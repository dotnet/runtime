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
    // dependency on this order. Note also that we don't allow duplicates in the list; we maintain a DupCount
    // count of duplication. This also necessitates walking the flow list for every edge we add.
    //
    FlowEdge*  flow  = nullptr;
    FlowEdge** listp = &block->bbPreds;

    if (initializingPreds)
    {
        // List is sorted order and we're adding references in
        // increasing blockPred->bbNum order. The only possible
        // dup list entry is the last one.
        //
        FlowEdge* flowLast = block->bbLastPred;
        if (flowLast != nullptr)
        {
            listp = flowLast->getNextPredEdgeRef();

            assert(flowLast->getSourceBlock()->bbNum <= blockPred->bbNum);

            if (flowLast->getSourceBlock() == blockPred)
            {
                flow = flowLast;

                // This edge should have been given a likelihood when it was created.
                // Since we're increasing its duplicate count, update the likelihood.
                //
                assert(flow->hasLikelihood());
                const unsigned numSucc = blockPred->NumSucc();
                assert(numSucc > 0);

                if (numSucc == 1)
                {
                    // BasicBlock::NumSucc() returns 1 for BBJ_CONDs with the same true/false target.
                    // For blocks that only ever have one successor (BBJ_ALWAYS, BBJ_LEAVE, etc.),
                    // their successor edge should never have a duplicate count over 1.
                    //
                    assert(blockPred->KindIs(BBJ_COND));
                    assert(blockPred->TrueTargetIs(blockPred->GetFalseTarget()));
                    flow->setLikelihood(1.0);
                }
                else
                {
                    // Duplicate count isn't updated until later, so add 1 for now.
                    //
                    const unsigned dupCount = flow->getDupCount() + 1;
                    assert(dupCount > 1);
                    flow->setLikelihood((1.0 / numSucc) * dupCount);
                }
            }
        }
    }
    else
    {
        // References are added randomly, so we have to search.
        //
        while ((*listp != nullptr) && ((*listp)->getSourceBlock()->bbNum < blockPred->bbNum))
        {
            listp = (*listp)->getNextPredEdgeRef();
        }

        if ((*listp != nullptr) && ((*listp)->getSourceBlock() == blockPred))
        {
            flow = *listp;
        }
    }

    if (flow != nullptr)
    {
        // The predecessor block already exists in the flow list; simply add to its duplicate count.
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

            // When initializing preds, ensure edge likelihood is set,
            // such that this edge is as likely as any other successor edge
            //
            const unsigned numSucc = blockPred->NumSucc();
            assert(numSucc > 0);
            assert(flow->getDupCount() == 1);
            flow->setLikelihood(1.0 / numSucc);
        }
        else if ((oldEdge != nullptr) && oldEdge->hasLikelihood())
        {
            // Copy likelihood from old edge, if any.
            //
            flow->setLikelihood(oldEdge->getLikelihood());
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
                flow->setEdgeWeights(oldEdge->edgeWeightMin(), oldEdge->edgeWeightMax(), block);
            }
            else
            {
                // Set the max edge weight to be the minimum of block's or blockPred's weight
                //
                weight_t newWeightMax = min(block->bbWeight, blockPred->bbWeight);

                // If we are inserting a conditional block the minimum weight is zero,
                // otherwise it is the same as the edge's max weight.
                if (blockPred->NumSucc() > 1)
                {
                    flow->setEdgeWeights(BB_ZERO_WEIGHT, newWeightMax, block);
                }
                else
                {
                    flow->setEdgeWeights(flow->edgeWeightMax(), newWeightMax, block);
                }
            }
        }
        else
        {
            flow->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT, block);
        }
    }

    // Pred list should (still) be ordered.
    //
    assert(block->checkPredListOrder());

    // When initializing preds, edge likelihood should always be set.
    //
    assert(!initializingPreds || flow->hasLikelihood());

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
//
// Notes:
//    -- block->bbRefs is decremented by one to account for the reduction in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a flow edge is removed (but not if an existing flow edge dup count is decremented),
//       indicating that the flow graph shape has changed.
//
FlowEdge* Compiler::fgRemoveRefPred(BasicBlock* block, BasicBlock* blockPred)
{
    noway_assert(block != nullptr);
    noway_assert(blockPred != nullptr);
    noway_assert(block->countOfInEdges() > 0);
    assert(fgPredsComputed);
    block->bbRefs--;

    FlowEdge** ptrToPred;
    FlowEdge*  pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    noway_assert(pred != nullptr);
    noway_assert(pred->getDupCount() > 0);

    pred->decrementDupCount();

    if (pred->getDupCount() == 0)
    {
        // Splice out the predecessor edge since it's no longer necessary.
        *ptrToPred = pred->getNextPredEdge();

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
    PREFIX_ASSUME(block != nullptr);

    switch (block->GetKind())
    {
        case BBJ_CALLFINALLY:
        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
            fgRemoveRefPred(block->GetTarget(), block);
            break;

        case BBJ_COND:
            fgRemoveRefPred(block->GetTrueTarget(), block);
            fgRemoveRefPred(block->GetFalseTarget(), block);
            break;

        case BBJ_EHFINALLYRET:
            for (BasicBlock* const succ : block->EHFinallyRetSuccs())
            {
                fgRemoveRefPred(succ, block);
            }
            break;

        case BBJ_EHFAULTRET:
        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:
            for (BasicBlock* const bTarget : block->SwitchTargets())
            {
                fgRemoveRefPred(bTarget, block);
            }
            break;

        default:
            noway_assert(!"Block doesn't have a valid bbKind!!!!");
            break;
    }
}

Compiler::SwitchUniqueSuccSet Compiler::GetDescriptorForSwitch(BasicBlock* switchBlk)
{
    assert(switchBlk->KindIs(BBJ_SWITCH));
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
        for (BasicBlock* const targ : switchBlk->SwitchTargets())
        {
            BitVecOps::AddElemD(&blockVecTraits, uniqueSuccBlocks, targ->bbNum);
        }
        // Now we have a set of unique successors.
        unsigned numNonDups = BitVecOps::Count(&blockVecTraits, uniqueSuccBlocks);

        BasicBlock** nonDups = new (getAllocator()) BasicBlock*[numNonDups];

        unsigned nonDupInd = 0;
        // At this point, all unique targets are in "uniqueSuccBlocks".  As we encounter each,
        // add to nonDups, remove from "uniqueSuccBlocks".
        for (BasicBlock* const targ : switchBlk->SwitchTargets())
        {
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
    assert(switchBlk->KindIs(BBJ_SWITCH)); // Precondition.

    // Is "from" still in the switch table (because it had more than one entry before?)
    bool fromStillPresent = false;
    for (BasicBlock* const bTarget : switchBlk->SwitchTargets())
    {
        if (bTarget == from)
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
        // write "to" where "from" was
        INDEBUG(bool foundFrom = false);
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = to;
                INDEBUG(foundFrom = true);
                break;
            }
        }
        assert(foundFrom);
    }
    else
    {
        assert(!fromStillPresent && toAlreadyPresent);
        // remove "from".
        INDEBUG(bool foundFrom = false);
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = nonDuplicates[numDistinctSuccs - 1];
                numDistinctSuccs--;
                INDEBUG(foundFrom = true);
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
