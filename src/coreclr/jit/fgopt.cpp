// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lower.h" // for LowerRange()

// Flowgraph Optimization

//------------------------------------------------------------------------
// fgDominate: Returns true if block `b1` dominates block `b2`.
//
// Arguments:
//    b1, b2 -- Two blocks to compare.
//
// Return Value:
//    true if `b1` dominates `b2`. If either b1 or b2 were created after dominators were calculated,
//    but the dominator information still exists, try to determine if we can make a statement about
//    b1 dominating b2 based on existing dominator information and other information, such as
//    predecessor lists or loop information.
//
// Assumptions:
//    -- Dominators have been calculated (`fgDomsComputed` is true).
//
bool Compiler::fgDominate(BasicBlock* b1, BasicBlock* b2)
{
    noway_assert(fgDomsComputed);
    assert(!fgCheapPredsValid);

    //
    // If the fgModified flag is false then we made some modifications to
    // the flow graph, like adding a new block or changing a conditional branch
    // into an unconditional branch.
    //
    // We can continue to use the dominator and reachable information to
    // unmark loops as long as we haven't renumbered the blocks or we aren't
    // asking for information about a new block.
    //

    if (b2->bbNum > fgDomBBcount)
    {
        if (b1 == b2)
        {
            return true;
        }

        for (flowList* pred = b2->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            if (!fgDominate(b1, pred->getBlock()))
            {
                return false;
            }
        }

        return b2->bbPreds != nullptr;
    }

    if (b1->bbNum > fgDomBBcount)
    {
        // if b1 is a loop preheader and Succ is its only successor, then all predecessors of
        // Succ either are b1 itself or are dominated by Succ. Under these conditions, b1
        // dominates b2 if and only if Succ dominates b2 (or if b2 == b1, but we already tested
        // for this case)
        if (b1->bbFlags & BBF_LOOP_PREHEADER)
        {
            noway_assert(b1->bbFlags & BBF_INTERNAL);
            noway_assert(b1->bbJumpKind == BBJ_NONE);
            return fgDominate(b1->bbNext, b2);
        }

        // unknown dominators; err on the safe side and return false
        return false;
    }

    /* Check if b1 dominates b2 */
    unsigned numA = b1->bbNum;
    noway_assert(numA <= fgDomBBcount);
    unsigned numB = b2->bbNum;
    noway_assert(numB <= fgDomBBcount);

    // What we want to ask here is basically if A is in the middle of the path from B to the root (the entry node)
    // in the dominator tree. Turns out that can be translated as:
    //
    //   A dom B <-> preorder(A) <= preorder(B) && postorder(A) >= postorder(B)
    //
    // where the equality holds when you ask if A dominates itself.
    bool treeDom =
        fgDomTreePreOrder[numA] <= fgDomTreePreOrder[numB] && fgDomTreePostOrder[numA] >= fgDomTreePostOrder[numB];

    return treeDom;
}

//------------------------------------------------------------------------
// fgReachable: Returns true if block `b1` can reach block `b2`.
//
// Arguments:
//    b1, b2 -- Two blocks to compare.
//
// Return Value:
//    true if `b1` can reach `b2` via some path. If either b1 or b2 were created after dominators were calculated,
//    but the dominator information still exists, try to determine if we can make a statement about
//    b1 reaching b2 based on existing reachability information and other information, such as
//    predecessor lists.
//
// Assumptions:
//    -- Dominators have been calculated (`fgDomsComputed` is true).
//    -- Reachability information has been calculated (`fgReachabilitySetsValid` is true).
//
bool Compiler::fgReachable(BasicBlock* b1, BasicBlock* b2)
{
    noway_assert(fgDomsComputed);
    assert(!fgCheapPredsValid);

    //
    // If the fgModified flag is false then we made some modifications to
    // the flow graph, like adding a new block or changing a conditional branch
    // into an unconditional branch.
    //
    // We can continue to use the dominator and reachable information to
    // unmark loops as long as we haven't renumbered the blocks or we aren't
    // asking for information about a new block
    //

    if (b2->bbNum > fgDomBBcount)
    {
        if (b1 == b2)
        {
            return true;
        }

        for (flowList* pred = b2->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            if (fgReachable(b1, pred->getBlock()))
            {
                return true;
            }
        }

        return false;
    }

    if (b1->bbNum > fgDomBBcount)
    {
        noway_assert(b1->bbJumpKind == BBJ_NONE || b1->bbJumpKind == BBJ_ALWAYS || b1->bbJumpKind == BBJ_COND);

        if (b1->bbFallsThrough() && fgReachable(b1->bbNext, b2))
        {
            return true;
        }

        if (b1->bbJumpKind == BBJ_ALWAYS || b1->bbJumpKind == BBJ_COND)
        {
            return fgReachable(b1->bbJumpDest, b2);
        }

        return false;
    }

    /* Check if b1 can reach b2 */
    assert(fgReachabilitySetsValid);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgDomBBcount + 1);
    return BlockSetOps::IsMember(this, b2->bbReach, b1->bbNum);
}

//------------------------------------------------------------------------
// fgUpdateChangedFlowGraph: Update changed flow graph information.
//
// If the flow graph has changed, we need to recompute various information if we want to use it again.
//
// Arguments:
//    computeDoms -- `true` if we should recompute dominators
//
void Compiler::fgUpdateChangedFlowGraph(const bool computePreds, const bool computeDoms)
{
    // We need to clear this so we don't hit an assert calling fgRenumberBlocks().
    fgDomsComputed = false;

    JITDUMP("\nRenumbering the basic blocks for fgUpdateChangeFlowGraph\n");
    fgRenumberBlocks();

    if (computePreds) // This condition is only here until all phases don't require it.
    {
        fgComputePreds();
    }
    fgComputeEnterBlocksSet();
    fgComputeReachabilitySets();
    if (computeDoms)
    {
        fgComputeDoms();
    }
}

//------------------------------------------------------------------------
// fgComputeReachabilitySets: Compute the bbReach sets.
//
// This can be called to recompute the bbReach sets after the flow graph changes, such as when the
// number of BasicBlocks change (and thus, the BlockSet epoch changes).
//
// This also sets the BBF_GC_SAFE_POINT flag on blocks.
//
// TODO-Throughput: This algorithm consumes O(n^2) because we're using dense bitsets to
// represent reachability. While this yields O(1) time queries, it bloats the memory usage
// for large code.  We can do better if we try to approach reachability by
// computing the strongly connected components of the flow graph.  That way we only need
// linear memory to label every block with its SCC.
//
// Assumptions:
//    Assumes the predecessor lists are correct.
//
void Compiler::fgComputeReachabilitySets()
{
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);

#ifdef DEBUG
    fgReachabilitySetsValid = false;
#endif // DEBUG

    BasicBlock* block;

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // Initialize the per-block bbReach sets. It creates a new empty set,
        // because the block epoch could change since the previous initialization
        // and the old set could have wrong size.
        block->bbReach = BlockSetOps::MakeEmpty(this);

        /* Mark block as reaching itself */
        BlockSetOps::AddElemD(this, block->bbReach, block->bbNum);
    }

    // Find the reachable blocks. Also, set BBF_GC_SAFE_POINT.

    bool     change;
    BlockSet newReach(BlockSetOps::MakeEmpty(this));
    do
    {
        change = false;

        for (block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            BlockSetOps::Assign(this, newReach, block->bbReach);

            bool predGcSafe = (block->bbPreds != nullptr); // Do all of our predecessor blocks have a GC safe bit?

            for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* predBlock = pred->getBlock();

                /* Union the predecessor's reachability set into newReach */
                BlockSetOps::UnionD(this, newReach, predBlock->bbReach);

                if (!(predBlock->bbFlags & BBF_GC_SAFE_POINT))
                {
                    predGcSafe = false;
                }
            }

            if (predGcSafe)
            {
                block->bbFlags |= BBF_GC_SAFE_POINT;
            }

            if (!BlockSetOps::Equal(this, newReach, block->bbReach))
            {
                BlockSetOps::Assign(this, block->bbReach, newReach);
                change = true;
            }
        }
    } while (change);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAfter computing reachability sets:\n");
        fgDispReach();
    }

    fgReachabilitySetsValid = true;
#endif // DEBUG
}

//------------------------------------------------------------------------
// fgComputeEnterBlocksSet: Compute the entry blocks set.
//
// Initialize fgEnterBlks to the set of blocks for which we don't have explicit control
// flow edges. These are the entry basic block and each of the EH handler blocks.
// For ARM, also include the BBJ_ALWAYS block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair,
// to avoid creating "retless" calls, since we need the BBJ_ALWAYS for the purpose
// of unwinding, even if the call doesn't return (due to an explicit throw, for example).
//
void Compiler::fgComputeEnterBlocksSet()
{
#ifdef DEBUG
    fgEnterBlksSetValid = false;
#endif // DEBUG

    fgEnterBlks = BlockSetOps::MakeEmpty(this);

    /* Now set the entry basic block */
    BlockSetOps::AddElemD(this, fgEnterBlks, fgFirstBB->bbNum);
    assert(fgFirstBB->bbNum == 1);

    if (compHndBBtabCount > 0)
    {
        /* Also 'or' in the handler basic blocks */
        EHblkDsc* HBtab;
        EHblkDsc* HBtabEnd;
        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd; HBtab++)
        {
            if (HBtab->HasFilter())
            {
                BlockSetOps::AddElemD(this, fgEnterBlks, HBtab->ebdFilter->bbNum);
            }
            BlockSetOps::AddElemD(this, fgEnterBlks, HBtab->ebdHndBeg->bbNum);
        }
    }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    // TODO-ARM-Cleanup: The ARM code here to prevent creating retless calls by adding the BBJ_ALWAYS
    // to the enter blocks is a bit of a compromise, because sometimes the blocks are already reachable,
    // and it messes up DFS ordering to have them marked as enter block. We should prevent the
    // creation of retless calls some other way.
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbJumpKind == BBJ_CALLFINALLY)
        {
            assert(block->isBBCallAlwaysPair());

            // Don't remove the BBJ_ALWAYS block that is only here for the unwinder. It might be dead
            // if the finally is no-return, so mark it as an entry point.
            BlockSetOps::AddElemD(this, fgEnterBlks, block->bbNext->bbNum);
        }
    }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

#ifdef DEBUG
    if (verbose)
    {
        printf("Enter blocks: ");
        BlockSetOps::Iter iter(this, fgEnterBlks);
        unsigned          bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            printf(FMT_BB " ", bbNum);
        }
        printf("\n");
    }
#endif // DEBUG

#ifdef DEBUG
    fgEnterBlksSetValid = true;
#endif // DEBUG
}

//------------------------------------------------------------------------
// fgRemoveUnreachableBlocks: Remove unreachable blocks.
//
// Some blocks (marked with BBF_DONT_REMOVE) can't be removed even if unreachable, in which case they
// are converted to `throw` blocks. Internal throw helper blocks and the single return block (if any)
// are never considered unreachable.
//
// Return Value:
//    Return true if any unreachable blocks were removed.
//
// Assumptions:
//    The reachability sets must be computed and valid.
//
// Notes:
//    Sets `fgHasLoops` if there are any loops in the function.
//    Sets `BBF_LOOP_HEAD` flag on a block if that block is the target of a backward branch and the block can
//    reach the source of the branch.
//
bool Compiler::fgRemoveUnreachableBlocks()
{
    assert(!fgCheapPredsValid);
    assert(fgReachabilitySetsValid);

    bool        hasLoops             = false;
    bool        hasUnreachableBlocks = false;
    BasicBlock* block;

    /* Record unreachable blocks */
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        /* Internal throw blocks are also reachable */
        if (fgIsThrowHlpBlk(block))
        {
            goto SKIP_BLOCK;
        }
        else if (block == genReturnBB)
        {
            // Don't remove statements for the genReturnBB block, as we might have special hookups there.
            // For example, <BUGNUM> in VSW 364383, </BUGNUM>
            // the profiler hookup needs to have the "void GT_RETURN" statement
            // to properly set the info.compProfilerCallback flag.
            goto SKIP_BLOCK;
        }
        else
        {
            // If any of the entry blocks can reach this block, then we skip it.
            if (!BlockSetOps::IsEmptyIntersection(this, fgEnterBlks, block->bbReach))
            {
                goto SKIP_BLOCK;
            }
        }

        // Remove all the code for the block
        fgUnreachableBlock(block);

        // Make sure that the block was marked as removed */
        noway_assert(block->bbFlags & BBF_REMOVED);

        // Some blocks mark the end of trys and catches
        // and can't be removed. We convert these into
        // empty blocks of type BBJ_THROW

        if (block->bbFlags & BBF_DONT_REMOVE)
        {
            bool bIsBBCallAlwaysPair = block->isBBCallAlwaysPair();

            /* Unmark the block as removed, */
            /* clear BBF_INTERNAL as well and set BBJ_IMPORTED */

            block->bbFlags &= ~(BBF_REMOVED | BBF_INTERNAL);
            block->bbFlags |= BBF_IMPORTED;
            block->bbJumpKind = BBJ_THROW;
            block->bbSetRunRarely();

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            // If this is a <BBJ_CALLFINALLY, BBJ_ALWAYS> pair, we have to clear BBF_FINALLY_TARGET flag on
            // the target node (of BBJ_ALWAYS) since BBJ_CALLFINALLY node is getting converted to a BBJ_THROW.
            if (bIsBBCallAlwaysPair)
            {
                noway_assert(block->bbNext->bbJumpKind == BBJ_ALWAYS);
                fgClearFinallyTargetBit(block->bbNext->bbJumpDest);
            }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        }
        else
        {
            /* We have to call fgRemoveBlock next */
            hasUnreachableBlocks = true;
        }
        continue;

    SKIP_BLOCK:;

        if (block->bbJumpKind == BBJ_RETURN)
        {
            continue;
        }

        // Set BBF_LOOP_HEAD if we have backwards branches to this block.

        unsigned blockNum = block->bbNum;
        for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            BasicBlock* predBlock = pred->getBlock();
            if (blockNum <= predBlock->bbNum)
            {
                if (predBlock->bbJumpKind == BBJ_CALLFINALLY)
                {
                    continue;
                }

                /* If block can reach predBlock then we have a loop head */
                if (BlockSetOps::IsMember(this, predBlock->bbReach, blockNum))
                {
                    hasLoops = true;

                    /* Set the BBF_LOOP_HEAD flag */
                    block->bbFlags |= BBF_LOOP_HEAD;
                    break;
                }
            }
        }
    }

    fgHasLoops = hasLoops;

    if (hasUnreachableBlocks)
    {
        // Now remove the unreachable blocks
        for (block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            //  If we mark the block with BBF_REMOVED then
            //  we need to call fgRemovedBlock() on it

            if (block->bbFlags & BBF_REMOVED)
            {
                fgRemoveBlock(block, true);

                // When we have a BBJ_CALLFINALLY, BBJ_ALWAYS pair; fgRemoveBlock will remove
                // both blocks, so we must advance 1 extra place in the block list
                //
                if (block->isBBCallAlwaysPair())
                {
                    block = block->bbNext;
                }
            }
        }
    }

    return hasUnreachableBlocks;
}

//------------------------------------------------------------------------
// fgComputeReachability: Compute the dominator and reachable sets.
//
// Use `fgReachable()` to check reachability, `fgDominate()` to check dominance.
//
// Also, compute the list of return blocks `fgReturnBlocks` and set of enter blocks `fgEnterBlks`.
// Delete unreachable blocks.
//
// Via the call to `fgRemoveUnreachableBlocks`, determine if the flow graph has loops and set 'fgHasLoops'
// accordingly. Set the BBF_LOOP_HEAD flag on the block target of backwards branches.
//
// Assumptions:
//    Assumes the predecessor lists are computed and correct.
//
void Compiler::fgComputeReachability()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgComputeReachability\n");
    }

    fgVerifyHandlerTab();

    // Make sure that the predecessor lists are accurate
    assert(fgComputePredsDone);
    fgDebugCheckBBlist();
#endif // DEBUG

    /* Create a list of all BBJ_RETURN blocks. The head of the list is 'fgReturnBlocks'. */
    fgReturnBlocks = nullptr;

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // If this is a BBJ_RETURN block, add it to our list of all BBJ_RETURN blocks. This list is only
        // used to find return blocks.
        if (block->bbJumpKind == BBJ_RETURN)
        {
            fgReturnBlocks = new (this, CMK_Reachability) BasicBlockList(block, fgReturnBlocks);
        }
    }

    // Compute reachability and then delete blocks determined to be unreachable. If we delete blocks, we
    // need to loop, as that might have caused more blocks to become unreachable. This can happen in the
    // case where a call to a finally is unreachable and deleted (maybe the call to the finally is
    // preceded by a throw or an infinite loop), making the blocks following the finally unreachable.
    // However, all EH entry blocks are considered global entry blocks, causing the blocks following the
    // call to the finally to stay rooted, until a second round of reachability is done.
    // The dominator algorithm expects that all blocks can be reached from the fgEnterBlks set.
    unsigned passNum = 1;
    bool     changed;
    do
    {
        // Just to be paranoid, avoid infinite loops; fall back to minopts.
        if (passNum > 10)
        {
            noway_assert(!"Too many unreachable block removal loops");
        }

        // Walk the flow graph, reassign block numbers to keep them in ascending order.
        JITDUMP("\nRenumbering the basic blocks for fgComputeReachability pass #%u\n", passNum);
        passNum++;
        fgRenumberBlocks();

        //
        // Compute fgEnterBlks
        //

        fgComputeEnterBlocksSet();

        //
        // Compute bbReach
        //

        fgComputeReachabilitySets();

        //
        // Use reachability information to delete unreachable blocks.
        // Also, determine if the flow graph has loops and set 'fgHasLoops' accordingly.
        // Set the BBF_LOOP_HEAD flag on the block target of backwards branches.
        //

        changed = fgRemoveUnreachableBlocks();

    } while (changed);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAfter computing reachability:\n");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }

    fgVerifyHandlerTab();
    fgDebugCheckBBlist(true);
#endif // DEBUG

    //
    // Now, compute the dominators
    //

    fgComputeDoms();
}

//-------------------------------------------------------------
// fgDfsInvPostOrder: Helper function for computing dominance information.
//
// In order to be able to compute dominance, we need to first get a DFS reverse post order sort on the basic flow
// graph for the dominance algorithm to operate correctly. The reason why we need the DFS sort is because we will
// build the dominance sets using the partial order induced by the DFS sorting.  With this precondition not
// holding true, the algorithm doesn't work properly.
//
void Compiler::fgDfsInvPostOrder()
{
    // NOTE: This algorithm only pays attention to the actual blocks. It ignores the imaginary entry block.

    // visited   :  Once we run the DFS post order sort recursive algorithm, we mark the nodes we visited to avoid
    //              backtracking.
    BlockSet visited(BlockSetOps::MakeEmpty(this));

    // We begin by figuring out which basic blocks don't have incoming edges and mark them as
    // start nodes.  Later on we run the recursive algorithm for each node that we
    // mark in this step.
    BlockSet_ValRet_T startNodes = fgDomFindStartNodes();

    // Make sure fgEnterBlks are still there in startNodes, even if they participate in a loop (i.e., there is
    // an incoming edge into the block).
    assert(fgEnterBlksSetValid);

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    //
    //    BlockSetOps::UnionD(this, startNodes, fgEnterBlks);
    //
    // This causes problems on ARM, because we for BBJ_CALLFINALLY/BBJ_ALWAYS pairs, we add the BBJ_ALWAYS
    // to the enter blocks set to prevent flow graph optimizations from removing it and creating retless call finallies
    // (BBF_RETLESS_CALL). This leads to an incorrect DFS ordering in some cases, because we start the recursive walk
    // from the BBJ_ALWAYS, which is reachable from other blocks. A better solution would be to change ARM to avoid
    // creating retless calls in a different way, not by adding BBJ_ALWAYS to fgEnterBlks.
    //
    // So, let us make sure at least fgFirstBB is still there, even if it participates in a loop.
    BlockSetOps::AddElemD(this, startNodes, 1);
    assert(fgFirstBB->bbNum == 1);
#else
    BlockSetOps::UnionD(this, startNodes, fgEnterBlks);
#endif

    assert(BlockSetOps::IsMember(this, startNodes, fgFirstBB->bbNum));

    // Call the flowgraph DFS traversal helper.
    unsigned postIndex = 1;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // If the block has no predecessors, and we haven't already visited it (because it's in fgEnterBlks but also
        // reachable from the first block), go ahead and traverse starting from this block.
        if (BlockSetOps::IsMember(this, startNodes, block->bbNum) &&
            !BlockSetOps::IsMember(this, visited, block->bbNum))
        {
            fgDfsInvPostOrderHelper(block, visited, &postIndex);
        }
    }

    // After the DFS reverse postorder is completed, we must have visited all the basic blocks.
    noway_assert(postIndex == fgBBcount + 1);
    noway_assert(fgBBNumMax == fgBBcount);

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nAfter doing a post order traversal of the BB graph, this is the ordering:\n");
        for (unsigned i = 1; i <= fgBBNumMax; ++i)
        {
            printf("%02u -> " FMT_BB "\n", i, fgBBInvPostOrder[i]->bbNum);
        }
        printf("\n");
    }
#endif // DEBUG
}

//-------------------------------------------------------------
// fgDomFindStartNodes: Helper for dominance computation to find the start nodes block set.
//
// The start nodes is a set that represents which basic blocks in the flow graph don't have incoming edges.
// We begin assuming everything is a start block and remove any block that is a successor of another.
//
// Returns:
//    Block set of start nodes.
//
BlockSet_ValRet_T Compiler::fgDomFindStartNodes()
{
    unsigned    j;
    BasicBlock* block;

    BlockSet startNodes(BlockSetOps::MakeFull(this));

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        unsigned cSucc = block->NumSucc(this);
        for (j = 0; j < cSucc; ++j)
        {
            BasicBlock* succ = block->GetSucc(j, this);
            BlockSetOps::RemoveElemD(this, startNodes, succ->bbNum);
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDominator computation start blocks (those blocks with no incoming edges):\n");
        BlockSetOps::Iter iter(this, startNodes);
        unsigned          bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            printf(FMT_BB " ", bbNum);
        }
        printf("\n");
    }
#endif // DEBUG

    return startNodes;
}

//------------------------------------------------------------------------
// fgDfsInvPostOrderHelper: Helper to assign post-order numbers to blocks.
//
// Arguments:
//    block   - The starting entry block
//    visited - The set of visited blocks
//    count   - Pointer to the Dfs counter
//
// Notes:
//    Compute a non-recursive DFS traversal of the flow graph using an
//    evaluation stack to assign post-order numbers.
//
void Compiler::fgDfsInvPostOrderHelper(BasicBlock* block, BlockSet& visited, unsigned* count)
{
    // Assume we haven't visited this node yet (callers ensure this).
    assert(!BlockSetOps::IsMember(this, visited, block->bbNum));

    // Allocate a local stack to hold the DFS traversal actions necessary
    // to compute pre/post-ordering of the control flowgraph.
    ArrayStack<DfsBlockEntry> stack(getAllocator(CMK_ArrayStack));

    // Push the first block on the stack to seed the traversal.
    stack.Push(DfsBlockEntry(DSS_Pre, block));

    // Flag the node we just visited to avoid backtracking.
    BlockSetOps::AddElemD(this, visited, block->bbNum);

    // The search is terminated once all the actions have been processed.
    while (!stack.Empty())
    {
        DfsBlockEntry current      = stack.Pop();
        BasicBlock*   currentBlock = current.dfsBlock;

        if (current.dfsStackState == DSS_Pre)
        {
            // This is a pre-visit that corresponds to the first time the
            // node is encountered in the spanning tree and receives pre-order
            // numberings. By pushing the post-action on the stack here we
            // are guaranteed to only process it after all of its successors
            // pre and post actions are processed.
            stack.Push(DfsBlockEntry(DSS_Post, currentBlock));

            unsigned cSucc = currentBlock->NumSucc(this);
            for (unsigned j = 0; j < cSucc; ++j)
            {
                BasicBlock* succ = currentBlock->GetSucc(j, this);

                // If this is a node we haven't seen before, go ahead and process
                if (!BlockSetOps::IsMember(this, visited, succ->bbNum))
                {
                    // Push a pre-visit action for this successor onto the stack and
                    // mark it as visited in case this block has multiple successors
                    // to the same node (multi-graph).
                    stack.Push(DfsBlockEntry(DSS_Pre, succ));
                    BlockSetOps::AddElemD(this, visited, succ->bbNum);
                }
            }
        }
        else
        {
            // This is a post-visit that corresponds to the last time the
            // node is visited in the spanning tree and only happens after
            // all descendents in the spanning tree have had pre and post
            // actions applied.

            assert(current.dfsStackState == DSS_Post);

            unsigned invCount = fgBBcount - *count + 1;
            assert(1 <= invCount && invCount <= fgBBNumMax);
            fgBBInvPostOrder[invCount]   = currentBlock;
            currentBlock->bbPostOrderNum = invCount;
            ++(*count);
        }
    }
}

//------------------------------------------------------------------------
// fgComputeDoms: Computer dominators. Use `fgDominate()` to check dominance.
//
// Compute immediate dominators, the dominator tree and and its pre/post-order traversal numbers.
//
// Also sets BBF_DOMINATED_BY_EXCEPTIONAL_ENTRY flag on blocks dominated by exceptional entry blocks.
//
// Notes:
//    Immediate dominator computation is based on "A Simple, Fast Dominance Algorithm"
//    by Keith D. Cooper, Timothy J. Harvey, and Ken Kennedy.
//
void Compiler::fgComputeDoms()
{
    assert(!fgCheapPredsValid);

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgComputeDoms\n");
    }

    fgVerifyHandlerTab();

    // Make sure that the predecessor lists are accurate.
    // Also check that the blocks are properly, densely numbered (so calling fgRenumberBlocks is not necessary).
    fgDebugCheckBBlist(true);

    // Assert things related to the BlockSet epoch.
    assert(fgBBcount == fgBBNumMax);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgBBNumMax + 1);
#endif // DEBUG

    BlockSet processedBlks(BlockSetOps::MakeEmpty(this));

    fgBBInvPostOrder = new (this, CMK_DominatorMemory) BasicBlock*[fgBBNumMax + 1]{};

    fgDfsInvPostOrder();
    noway_assert(fgBBInvPostOrder[0] == nullptr);

    // flRoot and bbRoot represent an imaginary unique entry point in the flow graph.
    // All the orphaned EH blocks and fgFirstBB will temporarily have its predecessors list
    // (with bbRoot as the only basic block in it) set as flRoot.
    // Later on, we clear their predecessors and let them to be nullptr again.
    // Since we number basic blocks starting at one, the imaginary entry block is conveniently numbered as zero.

    BasicBlock bbRoot;

    bbRoot.bbPreds        = nullptr;
    bbRoot.bbNum          = 0;
    bbRoot.bbIDom         = &bbRoot;
    bbRoot.bbPostOrderNum = 0;
    bbRoot.bbFlags        = 0;

    flowList flRoot(&bbRoot, nullptr);

    fgBBInvPostOrder[0] = &bbRoot;

    // Mark both bbRoot and fgFirstBB processed
    BlockSetOps::AddElemD(this, processedBlks, 0); // bbRoot    == block #0
    BlockSetOps::AddElemD(this, processedBlks, 1); // fgFirstBB == block #1
    assert(fgFirstBB->bbNum == 1);

    // Special case fgFirstBB to say its IDom is bbRoot.
    fgFirstBB->bbIDom = &bbRoot;

    BasicBlock* block = nullptr;

    for (block = fgFirstBB->bbNext; block != nullptr; block = block->bbNext)
    {
        // If any basic block has no predecessors then we flag it as processed and temporarily
        // mark its precedessor list to be flRoot.  This makes the flowgraph connected,
        // a precondition that is needed by the dominance algorithm to operate properly.
        if (block->bbPreds == nullptr)
        {
            block->bbPreds = &flRoot;
            block->bbIDom  = &bbRoot;
            BlockSetOps::AddElemD(this, processedBlks, block->bbNum);
        }
        else
        {
            block->bbIDom = nullptr;
        }
    }

    // Mark the EH blocks as entry blocks and also flag them as processed.
    if (compHndBBtabCount > 0)
    {
        EHblkDsc* HBtab;
        EHblkDsc* HBtabEnd;
        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd; HBtab++)
        {
            if (HBtab->HasFilter())
            {
                HBtab->ebdFilter->bbIDom = &bbRoot;
                BlockSetOps::AddElemD(this, processedBlks, HBtab->ebdFilter->bbNum);
            }
            HBtab->ebdHndBeg->bbIDom = &bbRoot;
            BlockSetOps::AddElemD(this, processedBlks, HBtab->ebdHndBeg->bbNum);
        }
    }

    // Now proceed to compute the immediate dominators for each basic block.
    bool changed = true;
    while (changed)
    {
        changed = false;
        // Process each actual block; don't process the imaginary predecessor block.
        for (unsigned i = 1; i <= fgBBNumMax; ++i)
        {
            flowList*   first   = nullptr;
            BasicBlock* newidom = nullptr;
            block               = fgBBInvPostOrder[i];

            // If we have a block that has bbRoot as its bbIDom
            // it means we flag it as processed and as an entry block so
            // in this case we're all set.
            if (block->bbIDom == &bbRoot)
            {
                continue;
            }

            // Pick up the first processed predecesor of the current block.
            for (first = block->bbPreds; first != nullptr; first = first->flNext)
            {
                if (BlockSetOps::IsMember(this, processedBlks, first->getBlock()->bbNum))
                {
                    break;
                }
            }
            noway_assert(first != nullptr);

            // We assume the first processed predecessor will be the
            // immediate dominator and then compute the forward flow analysis.
            newidom = first->getBlock();
            for (flowList* p = block->bbPreds; p != nullptr; p = p->flNext)
            {
                if (p->getBlock() == first->getBlock())
                {
                    continue;
                }
                if (p->getBlock()->bbIDom != nullptr)
                {
                    // fgIntersectDom is basically the set intersection between
                    // the dominance sets of the new IDom and the current predecessor
                    // Since the nodes are ordered in DFS inverse post order and
                    // IDom induces a tree, fgIntersectDom actually computes
                    // the lowest common ancestor in the dominator tree.
                    newidom = fgIntersectDom(p->getBlock(), newidom);
                }
            }

            // If the Immediate dominator changed, assign the new one
            // to the current working basic block.
            if (block->bbIDom != newidom)
            {
                noway_assert(newidom != nullptr);
                block->bbIDom = newidom;
                changed       = true;
            }
            BlockSetOps::AddElemD(this, processedBlks, block->bbNum);
        }
    }

    // As stated before, once we have computed immediate dominance we need to clear
    // all the basic blocks whose predecessor list was set to flRoot.  This
    // reverts that and leaves the blocks the same as before.
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbPreds == &flRoot)
        {
            block->bbPreds = nullptr;
        }
    }

    fgCompDominatedByExceptionalEntryBlocks();

#ifdef DEBUG
    if (verbose)
    {
        fgDispDoms();
    }
#endif

    fgNumberDomTree(fgBuildDomTree());

    fgModified   = false;
    fgDomBBcount = fgBBcount;
    assert(fgBBcount == fgBBNumMax);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgDomBBcount + 1);

    fgDomsComputed = true;
}

//------------------------------------------------------------------------
// fgBuildDomTree: Build the dominator tree for the current flowgraph.
//
// Returns:
//    An array of dominator tree nodes, indexed by BasicBlock::bbNum.
//
// Notes:
//    Immediate dominators must have already been computed in BasicBlock::bbIDom
//    before calling this.
//
DomTreeNode* Compiler::fgBuildDomTree()
{
    JITDUMP("\nInside fgBuildDomTree\n");

    unsigned     bbArraySize = fgBBNumMax + 1;
    DomTreeNode* domTree     = new (this, CMK_DominatorMemory) DomTreeNode[bbArraySize]{};

    BasicBlock* imaginaryRoot = fgFirstBB->bbIDom;

    if (imaginaryRoot != nullptr)
    {
        // If the first block has a dominator then this must be the imaginary entry block added
        // by fgComputeDoms, it is not actually part of the flowgraph and should have number 0.
        assert(imaginaryRoot->bbNum == 0);
        assert(imaginaryRoot->bbIDom == imaginaryRoot);

        // Clear the imaginary dominator to turn the tree back to a forest.
        fgFirstBB->bbIDom = nullptr;
    }

    // If the imaginary root is present then we'll need to create a forest instead of a tree.
    // Forest roots are chained via DomTreeNode::nextSibling and we keep track of this list's
    // tail in order to append to it. The head of the list is fgFirstBB, by construction.
    BasicBlock* rootListTail = fgFirstBB;

    // Traverse the entire block list to build the dominator tree. Skip fgFirstBB
    // as it is always a root of the dominator forest.
    for (BasicBlock* block = fgFirstBB->bbNext; block != nullptr; block = block->bbNext)
    {
        BasicBlock* parent = block->bbIDom;

        if (parent != imaginaryRoot)
        {
            assert(block->bbNum < bbArraySize);
            assert(parent->bbNum < bbArraySize);

            domTree[block->bbNum].nextSibling = domTree[parent->bbNum].firstChild;
            domTree[parent->bbNum].firstChild = block;
        }
        else if (imaginaryRoot != nullptr)
        {
            assert(rootListTail->bbNum < bbArraySize);

            domTree[rootListTail->bbNum].nextSibling = block;
            rootListTail                             = block;

            // Clear the imaginary dominator to turn the tree back to a forest.
            block->bbIDom = nullptr;
        }
    }

    JITDUMP("\nAfter computing the Dominance Tree:\n");
    DBEXEC(verbose, fgDispDomTree(domTree));

    return domTree;
}

#ifdef DEBUG
void Compiler::fgDispDomTree(DomTreeNode* domTree)
{
    for (unsigned i = 1; i <= fgBBNumMax; ++i)
    {
        if (domTree[i].firstChild != nullptr)
        {
            printf(FMT_BB " : ", i);
            for (BasicBlock* child = domTree[i].firstChild; child != nullptr; child = domTree[child->bbNum].nextSibling)
            {
                printf(FMT_BB " ", child->bbNum);
            }
            printf("\n");
        }
    }
    printf("\n");
}
#endif // DEBUG

//------------------------------------------------------------------------
// fgNumberDomTree: Assign pre/post-order numbers to the dominator tree.
//
// Arguments:
//    domTree - The dominator tree node array
//
// Notes:
//    Runs a non-recursive DFS traversal of the dominator tree to assign
//    pre-order and post-order numbers. These numbers are used to provide
//    constant time lookup ancestor/descendent tests between pairs of nodes
//    in the tree.
//
void Compiler::fgNumberDomTree(DomTreeNode* domTree)
{
    class NumberDomTreeVisitor : public DomTreeVisitor<NumberDomTreeVisitor>
    {
        unsigned m_preNum;
        unsigned m_postNum;

    public:
        NumberDomTreeVisitor(Compiler* compiler, DomTreeNode* domTree) : DomTreeVisitor(compiler, domTree)
        {
        }

        void Begin()
        {
            unsigned bbArraySize           = m_compiler->fgBBNumMax + 1;
            m_compiler->fgDomTreePreOrder  = new (m_compiler, CMK_DominatorMemory) unsigned[bbArraySize]{};
            m_compiler->fgDomTreePostOrder = new (m_compiler, CMK_DominatorMemory) unsigned[bbArraySize]{};

            // The preorder and postorder numbers.
            // We start from 1 to match the bbNum ordering.
            m_preNum  = 1;
            m_postNum = 1;
        }

        void PreOrderVisit(BasicBlock* block)
        {
            m_compiler->fgDomTreePreOrder[block->bbNum] = m_preNum++;
        }

        void PostOrderVisit(BasicBlock* block)
        {
            m_compiler->fgDomTreePostOrder[block->bbNum] = m_postNum++;
        }

        void End()
        {
            noway_assert(m_preNum == m_compiler->fgBBNumMax + 1);
            noway_assert(m_postNum == m_compiler->fgBBNumMax + 1);

            noway_assert(m_compiler->fgDomTreePreOrder[0] == 0);  // Unused first element
            noway_assert(m_compiler->fgDomTreePostOrder[0] == 0); // Unused first element
            noway_assert(m_compiler->fgDomTreePreOrder[1] == 1);  // First block should be first in pre order

#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("\nAfter numbering the dominator tree:\n");
                for (unsigned i = 1; i <= m_compiler->fgBBNumMax; ++i)
                {
                    printf(FMT_BB ": pre=%02u, post=%02u\n", i, m_compiler->fgDomTreePreOrder[i],
                           m_compiler->fgDomTreePostOrder[i]);
                }
            }
#endif // DEBUG
        }
    };

    NumberDomTreeVisitor visitor(this, domTree);
    visitor.WalkTree();
}

//-------------------------------------------------------------
// fgIntersectDom: Intersect two immediate dominator sets.
//
// Find the lowest common ancestor in the dominator tree between two basic blocks. The LCA in the dominance tree
// represents the closest dominator between the two basic blocks. Used to adjust the IDom value in fgComputDoms.
//
// Arguments:
//    a, b - two blocks to intersect
//
// Returns:
//    The least common ancestor of `a` and `b` in the IDom tree.
//
BasicBlock* Compiler::fgIntersectDom(BasicBlock* a, BasicBlock* b)
{
    BasicBlock* finger1 = a;
    BasicBlock* finger2 = b;
    while (finger1 != finger2)
    {
        while (finger1->bbPostOrderNum > finger2->bbPostOrderNum)
        {
            finger1 = finger1->bbIDom;
        }
        while (finger2->bbPostOrderNum > finger1->bbPostOrderNum)
        {
            finger2 = finger2->bbIDom;
        }
    }
    return finger1;
}

//-------------------------------------------------------------
// fgGetDominatorSet: Return a set of blocks that dominate `block`.
//
// Note: this is slow compared to calling fgDominate(), especially if doing a single check comparing
// two blocks.
//
// Arguments:
//    block - get the set of blocks which dominate this block
//
// Returns:
//    A set of blocks which dominate `block`.
//
BlockSet_ValRet_T Compiler::fgGetDominatorSet(BasicBlock* block)
{
    assert(block != nullptr);

    BlockSet domSet(BlockSetOps::MakeEmpty(this));

    do
    {
        BlockSetOps::AddElemD(this, domSet, block->bbNum);
        if (block == block->bbIDom)
        {
            break; // We found a cycle in the IDom list, so we're done.
        }
        block = block->bbIDom;
    } while (block != nullptr);

    return domSet;
}

//-------------------------------------------------------------
// fgInitBlockVarSets: Initialize the per-block variable sets (used for liveness analysis).
//
// Notes:
//   Initializes:
//      bbVarUse, bbVarDef, bbLiveIn, bbLiveOut,
//      bbMemoryUse, bbMemoryDef, bbMemoryLiveIn, bbMemoryLiveOut,
//      bbScope
//
void Compiler::fgInitBlockVarSets()
{
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->InitVarSets(this);
    }

    fgBBVarSetsInited = true;
}

//------------------------------------------------------------------------
// fgRemoveEmptyBlocks: clean up flow graph after importation
//
// Notes:
//
//  Find and remove any basic blocks that are useless (e.g. they have not been
//  imported because they are not reachable, or they have been optimized away).
//
//  Remove try regions where no blocks in the try were imported.
//  Update the end of try and handler regions where trailing blocks were not imported.
//  Update the start of try regions that were partially imported (OSR)
//
void Compiler::fgRemoveEmptyBlocks()
{
    JITDUMP("\n*************** In fgRemoveEmptyBlocks\n");

    BasicBlock* cur;
    BasicBlock* nxt;

    // If we remove any blocks, we'll have to do additional work
    unsigned removedBlks = 0;

    for (cur = fgFirstBB; cur != nullptr; cur = nxt)
    {
        // Get hold of the next block (in case we delete 'cur')
        nxt = cur->bbNext;

        // Should this block be removed?
        if (!(cur->bbFlags & BBF_IMPORTED))
        {
            noway_assert(cur->isEmpty());

            if (ehCanDeleteEmptyBlock(cur))
            {
                JITDUMP(FMT_BB " was not imported, marking as removed (%d)\n", cur->bbNum, removedBlks);

                cur->bbFlags |= BBF_REMOVED;
                removedBlks++;

                // Drop the block from the list.
                //
                // We rely on the fact that this does not clear out
                // cur->bbNext or cur->bbPrev in the code that
                // follows.
                fgUnlinkBlock(cur);
            }
            else
            {
                // We were prevented from deleting this block by EH
                // normalization. Mark the block as imported.
                cur->bbFlags |= BBF_IMPORTED;
            }
        }
    }

    // If no blocks were removed, we're done
    if (removedBlks == 0)
    {
        return;
    }

    // Update all references in the exception handler table.
    //
    // We may have made the entire try block unreachable.
    // Check for this case and remove the entry from the EH table.
    //
    // For OSR, just the initial part of a try range may become
    // unreachable; if so we need to shrink the try range down
    // to the portion that was imported.
    unsigned  XTnum;
    EHblkDsc* HBtab;
    unsigned  delCnt = 0;

    // Walk the EH regions from inner to outer
    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
    AGAIN:

        // If start of a try region was not imported, then we either
        // need to trim the region extent, or remove the region
        // entirely.
        //
        // In normal importation, it is not valid to jump into the
        // middle of a try, so if the try entry was not imported, the
        // entire try can be removed.
        //
        // In OSR importation the entry patchpoint may be in the
        // middle of a try, and we need to determine how much of the
        // try ended up getting imported.  Because of backwards
        // branches we may end up importing the entire try even though
        // execution starts in the middle.
        //
        // Note it is common in both cases for the ends of trys (and
        // associated handlers) to end up not getting imported, so if
        // the try region is not removed, we always check if we need
        // to trim the ends.
        //
        if (HBtab->ebdTryBeg->bbFlags & BBF_REMOVED)
        {
            // Usual case is that the entire try can be removed.
            bool removeTryRegion = true;

            if (opts.IsOSR())
            {
                // For OSR we may need to trim the try region start.
                //
                // We rely on the fact that removed blocks have been snipped from
                // the main block list, but that those removed blocks have kept
                // their bbprev (and bbnext) links.
                //
                // Find the first unremoved block before the try entry block.
                //
                BasicBlock* const oldTryEntry  = HBtab->ebdTryBeg;
                BasicBlock*       tryEntryPrev = oldTryEntry->bbPrev;
                while ((tryEntryPrev != nullptr) && ((tryEntryPrev->bbFlags & BBF_REMOVED) != 0))
                {
                    tryEntryPrev = tryEntryPrev->bbPrev;
                }

                // Because we've added an unremovable scratch block as
                // fgFirstBB, this backwards walk should always find
                // some block.
                assert(tryEntryPrev != nullptr);

                // If there is a next block of this prev block, and that block is
                // contained in the current try, we'd like to make that block
                // the new start of the try, and keep the region.
                BasicBlock* newTryEntry    = tryEntryPrev->bbNext;
                bool        updateTryEntry = false;

                if ((newTryEntry != nullptr) && bbInTryRegions(XTnum, newTryEntry))
                {
                    // We want to trim the begin extent of the current try region to newTryEntry.
                    //
                    // This method is invoked after EH normalization, so we may need to ensure all
                    // try regions begin at blocks that are not the start or end of some other try.
                    //
                    // So, see if this block is already the start or end of some other EH region.
                    if (bbIsTryBeg(newTryEntry))
                    {
                        // We've already end-trimmed the inner try. Do the same now for the
                        // current try, so it is easier to detect when they mutually protect.
                        // (we will call this again later, which is harmless).
                        fgSkipRmvdBlocks(HBtab);

                        // If this try and the inner try form a "mutually protected try region"
                        // then we must continue to share the try entry block.
                        EHblkDsc* const HBinner = ehGetBlockTryDsc(newTryEntry);
                        assert(HBinner->ebdTryBeg == newTryEntry);

                        if (HBtab->ebdTryLast != HBinner->ebdTryLast)
                        {
                            updateTryEntry = true;
                        }
                    }
                    // Also, a try and handler cannot start at the same block
                    else if (bbIsHandlerBeg(newTryEntry))
                    {
                        updateTryEntry = true;
                    }

                    if (updateTryEntry)
                    {
                        // We need to trim the current try to begin at a different block. Normally
                        // this would be problematic as we don't have enough context to redirect
                        // all the incoming edges, but we know oldTryEntry is unreachable.
                        // So there are no incoming edges to worry about.
                        //
                        assert(!tryEntryPrev->bbFallsThrough());

                        // What follows is similar to fgNewBBInRegion, but we can't call that
                        // here as the oldTryEntry is no longer in the main bb list.
                        newTryEntry = bbNewBasicBlock(BBJ_NONE);
                        newTryEntry->bbFlags |= (BBF_IMPORTED | BBF_INTERNAL);

                        // Set the right EH region indices on this new block.
                        //
                        // Patchpoints currently cannot be inside handler regions,
                        // and so likewise the old and new try region entries.
                        assert(!oldTryEntry->hasHndIndex());
                        newTryEntry->setTryIndex(XTnum);
                        newTryEntry->clearHndIndex();
                        fgInsertBBafter(tryEntryPrev, newTryEntry);

                        JITDUMP("OSR: changing start of try region #%u from " FMT_BB " to new " FMT_BB "\n",
                                XTnum + delCnt, oldTryEntry->bbNum, newTryEntry->bbNum);
                    }
                    else
                    {
                        // We can just trim the try to newTryEntry as it is not part of some inner try or handler.
                        JITDUMP("OSR: changing start of try region #%u from " FMT_BB " to " FMT_BB "\n", XTnum + delCnt,
                                oldTryEntry->bbNum, newTryEntry->bbNum);
                    }

                    // Update the handler table
                    fgSetTryBeg(HBtab, newTryEntry);

                    // Try entry blocks get specially marked and have special protection.
                    HBtab->ebdTryBeg->bbFlags |= BBF_DONT_REMOVE | BBF_TRY_BEG;

                    // We are keeping this try region
                    removeTryRegion = false;
                }
            }

            if (removeTryRegion)
            {
                // In the dump, refer to the region by its original index.
                JITDUMP("Try region #%u (" FMT_BB " -- " FMT_BB ") not imported, removing try from the EH table\n",
                        XTnum + delCnt, HBtab->ebdTryBeg->bbNum, HBtab->ebdTryLast->bbNum);

                delCnt++;

                fgRemoveEHTableEntry(XTnum);

                if (XTnum < compHndBBtabCount)
                {
                    // There are more entries left to process, so do more. Note that
                    // HBtab now points to the next entry, that we copied down to the
                    // current slot. XTnum also stays the same.
                    goto AGAIN;
                }

                // no more entries (we deleted the last one), so exit the loop
                break;
            }
        }

        // If we get here, the try entry block was not removed.
        // Check some invariants.
        assert(HBtab->ebdTryBeg->bbFlags & BBF_IMPORTED);
        assert(HBtab->ebdTryBeg->bbFlags & BBF_DONT_REMOVE);
        assert(HBtab->ebdHndBeg->bbFlags & BBF_IMPORTED);
        assert(HBtab->ebdHndBeg->bbFlags & BBF_DONT_REMOVE);

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdFilter->bbFlags & BBF_IMPORTED);
            assert(HBtab->ebdFilter->bbFlags & BBF_DONT_REMOVE);
        }

        // Finally, do region end trimming -- update try and handler ends to reflect removed blocks.
        fgSkipRmvdBlocks(HBtab);
    }

    // Renumber the basic blocks
    JITDUMP("\nRenumbering the basic blocks for fgRemoveEmptyBlocks\n");
    fgRenumberBlocks();

#ifdef DEBUG
    fgVerifyHandlerTab();
#endif // DEBUG
}

//-------------------------------------------------------------
// fgCanCompactBlocks: Determine if a block and its bbNext successor can be compacted.
//
// Arguments:
//    block - block to check. If nullptr, return false.
//    bNext - bbNext of `block`. If nullptr, return false.
//
// Returns:
//    true if compaction is allowed
//
bool Compiler::fgCanCompactBlocks(BasicBlock* block, BasicBlock* bNext)
{
    if ((block == nullptr) || (bNext == nullptr))
    {
        return false;
    }

    noway_assert(block->bbNext == bNext);

    if (block->bbJumpKind != BBJ_NONE)
    {
        return false;
    }

    // If the next block has multiple incoming edges, we can still compact if the first block is empty.
    // However, not if it is the beginning of a handler.
    if (bNext->countOfInEdges() != 1 &&
        (!block->isEmpty() || (block->bbFlags & BBF_FUNCLET_BEG) || (block->bbCatchTyp != BBCT_NONE)))
    {
        return false;
    }

    if (bNext->bbFlags & BBF_DONT_REMOVE)
    {
        return false;
    }

    // Don't compact the first block if it was specially created as a scratch block.
    if (fgBBisScratch(block))
    {
        return false;
    }

    // Don't compact away any loop entry blocks that we added in optCanonicalizeLoops
    if (optIsLoopEntry(block))
    {
        return false;
    }

#if defined(TARGET_ARM)
    // We can't compact a finally target block, as we need to generate special code for such blocks during code
    // generation
    if ((bNext->bbFlags & BBF_FINALLY_TARGET) != 0)
        return false;
#endif

    // We don't want to compact blocks that are in different Hot/Cold regions
    //
    if (fgInDifferentRegions(block, bNext))
    {
        return false;
    }

    // We cannot compact two blocks in different EH regions.
    //
    if (fgCanRelocateEHRegions)
    {
        if (!BasicBlock::sameEHRegion(block, bNext))
        {
            return false;
        }
    }

    // If there is a switch predecessor don't bother because we'd have to update the uniquesuccs as well
    // (if they are valid).
    for (flowList* pred = bNext->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        if (pred->getBlock()->bbJumpKind == BBJ_SWITCH)
        {
            return false;
        }
    }

    return true;
}

//-------------------------------------------------------------
// fgCompactBlocks: Compact two blocks into one.
//
// Assumes that all necessary checks have been performed, i.e. fgCanCompactBlocks returns true.
//
// Uses for this function - whenever we change links, insert blocks, ...
// It will keep the flowgraph data in synch - bbNum, bbRefs, bbPreds
//
// Arguments:
//    block - move all code into this block.
//    bNext - bbNext of `block`. This block will be removed.
//
void Compiler::fgCompactBlocks(BasicBlock* block, BasicBlock* bNext)
{
    noway_assert(block != nullptr);
    noway_assert((block->bbFlags & BBF_REMOVED) == 0);
    noway_assert(block->bbJumpKind == BBJ_NONE);

    noway_assert(bNext == block->bbNext);
    noway_assert(bNext != nullptr);
    noway_assert((bNext->bbFlags & BBF_REMOVED) == 0);
    noway_assert(bNext->countOfInEdges() == 1 || block->isEmpty());
    noway_assert(bNext->bbPreds);

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    noway_assert((bNext->bbFlags & BBF_FINALLY_TARGET) == 0);
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    // Make sure the second block is not the start of a TRY block or an exception handler

    noway_assert(bNext->bbCatchTyp == BBCT_NONE);
    noway_assert((bNext->bbFlags & BBF_TRY_BEG) == 0);
    noway_assert((bNext->bbFlags & BBF_DONT_REMOVE) == 0);

    /* both or none must have an exception handler */
    noway_assert(block->hasTryIndex() == bNext->hasTryIndex());

#ifdef DEBUG
    if (verbose)
    {
        printf("\nCompacting blocks " FMT_BB " and " FMT_BB ":\n", block->bbNum, bNext->bbNum);
    }
#endif

    if (bNext->countOfInEdges() > 1)
    {
        JITDUMP("Second block has multiple incoming edges\n");

        assert(block->isEmpty());
        for (flowList* pred = bNext->bbPreds; pred; pred = pred->flNext)
        {
            fgReplaceJumpTarget(pred->getBlock(), block, bNext);

            if (pred->getBlock() != block)
            {
                fgAddRefPred(block, pred->getBlock());
            }
        }
        bNext->bbPreds = nullptr;
    }
    else
    {
        noway_assert(bNext->bbPreds->flNext == nullptr);
        noway_assert(bNext->bbPreds->getBlock() == block);
    }

    /* Start compacting - move all the statements in the second block to the first block */

    // First move any phi definitions of the second block after the phi defs of the first.
    // TODO-CQ: This may be the wrong thing to do.  If we're compacting blocks, it's because a
    // control-flow choice was constant-folded away.  So probably phi's need to go away,
    // as well, in favor of one of the incoming branches.  Or at least be modified.

    assert(block->IsLIR() == bNext->IsLIR());
    if (block->IsLIR())
    {
        LIR::Range& blockRange = LIR::AsRange(block);
        LIR::Range& nextRange  = LIR::AsRange(bNext);

        // Does the next block have any phis?
        GenTree*           nextFirstNonPhi = nullptr;
        LIR::ReadOnlyRange nextPhis        = nextRange.PhiNodes();
        if (!nextPhis.IsEmpty())
        {
            GenTree* blockLastPhi = blockRange.LastPhiNode();
            nextFirstNonPhi       = nextPhis.LastNode()->gtNext;

            LIR::Range phisToMove = nextRange.Remove(std::move(nextPhis));
            blockRange.InsertAfter(blockLastPhi, std::move(phisToMove));
        }
        else
        {
            nextFirstNonPhi = nextRange.FirstNode();
        }

        // Does the block have any other code?
        if (nextFirstNonPhi != nullptr)
        {
            LIR::Range nextNodes = nextRange.Remove(nextFirstNonPhi, nextRange.LastNode());
            blockRange.InsertAtEnd(std::move(nextNodes));
        }
    }
    else
    {
        Statement* blkNonPhi1   = block->FirstNonPhiDef();
        Statement* bNextNonPhi1 = bNext->FirstNonPhiDef();
        Statement* blkFirst     = block->firstStmt();
        Statement* bNextFirst   = bNext->firstStmt();

        // Does the second have any phis?
        if (bNextFirst != nullptr && bNextFirst != bNextNonPhi1)
        {
            Statement* bNextLast = bNextFirst->GetPrevStmt();
            assert(bNextLast->GetNextStmt() == nullptr);

            // Does "blk" have phis?
            if (blkNonPhi1 != blkFirst)
            {
                // Yes, has phis.
                // Insert after the last phi of "block."
                // First, bNextPhis after last phi of block.
                Statement* blkLastPhi;
                if (blkNonPhi1 != nullptr)
                {
                    blkLastPhi = blkNonPhi1->GetPrevStmt();
                }
                else
                {
                    blkLastPhi = blkFirst->GetPrevStmt();
                }

                blkLastPhi->SetNextStmt(bNextFirst);
                bNextFirst->SetPrevStmt(blkLastPhi);

                // Now, rest of "block" after last phi of "bNext".
                Statement* bNextLastPhi = nullptr;
                if (bNextNonPhi1 != nullptr)
                {
                    bNextLastPhi = bNextNonPhi1->GetPrevStmt();
                }
                else
                {
                    bNextLastPhi = bNextFirst->GetPrevStmt();
                }

                bNextLastPhi->SetNextStmt(blkNonPhi1);
                if (blkNonPhi1 != nullptr)
                {
                    blkNonPhi1->SetPrevStmt(bNextLastPhi);
                }
                else
                {
                    // block has no non phis, so make the last statement be the last added phi.
                    blkFirst->SetPrevStmt(bNextLastPhi);
                }

                // Now update the bbStmtList of "bNext".
                bNext->bbStmtList = bNextNonPhi1;
                if (bNextNonPhi1 != nullptr)
                {
                    bNextNonPhi1->SetPrevStmt(bNextLast);
                }
            }
            else
            {
                if (blkFirst != nullptr) // If "block" has no statements, fusion will work fine...
                {
                    // First, bNextPhis at start of block.
                    Statement* blkLast = blkFirst->GetPrevStmt();
                    block->bbStmtList  = bNextFirst;
                    // Now, rest of "block" (if it exists) after last phi of "bNext".
                    Statement* bNextLastPhi = nullptr;
                    if (bNextNonPhi1 != nullptr)
                    {
                        // There is a first non phi, so the last phi is before it.
                        bNextLastPhi = bNextNonPhi1->GetPrevStmt();
                    }
                    else
                    {
                        // All the statements are phi defns, so the last one is the prev of the first.
                        bNextLastPhi = bNextFirst->GetPrevStmt();
                    }
                    bNextFirst->SetPrevStmt(blkLast);
                    bNextLastPhi->SetNextStmt(blkFirst);
                    blkFirst->SetPrevStmt(bNextLastPhi);
                    // Now update the bbStmtList of "bNext"
                    bNext->bbStmtList = bNextNonPhi1;
                    if (bNextNonPhi1 != nullptr)
                    {
                        bNextNonPhi1->SetPrevStmt(bNextLast);
                    }
                }
            }
        }

        // Now proceed with the updated bbTreeLists.
        Statement* stmtList1 = block->firstStmt();
        Statement* stmtList2 = bNext->firstStmt();

        /* the block may have an empty list */

        if (stmtList1 != nullptr)
        {
            Statement* stmtLast1 = block->lastStmt();

            /* The second block may be a GOTO statement or something with an empty bbStmtList */
            if (stmtList2 != nullptr)
            {
                Statement* stmtLast2 = bNext->lastStmt();

                /* append list2 to list 1 */

                stmtLast1->SetNextStmt(stmtList2);
                stmtList2->SetPrevStmt(stmtLast1);
                stmtList1->SetPrevStmt(stmtLast2);
            }
        }
        else
        {
            /* block was formerly empty and now has bNext's statements */
            block->bbStmtList = stmtList2;
        }
    }

    // Note we could update the local variable weights here by
    // calling lvaMarkLocalVars, with the block and weight adjustment.

    // If either block or bNext has a profile weight
    // or if both block and bNext have non-zero weights
    // then we select the highest weight block.

    if (block->hasProfileWeight() || bNext->hasProfileWeight() || (block->bbWeight && bNext->bbWeight))
    {
        // We are keeping block so update its fields
        // when bNext has a greater weight

        if (block->bbWeight < bNext->bbWeight)
        {
            block->bbWeight = bNext->bbWeight;

            block->bbFlags |= (bNext->bbFlags & BBF_PROF_WEIGHT); // Set the profile weight flag (if necessary)
            assert(block->bbWeight != BB_ZERO_WEIGHT);
            block->bbFlags &= ~BBF_RUN_RARELY; // Clear any RarelyRun flag
        }
    }
    // otherwise if either block has a zero weight we select the zero weight
    else
    {
        noway_assert((block->bbWeight == BB_ZERO_WEIGHT) || (bNext->bbWeight == BB_ZERO_WEIGHT));
        block->bbWeight = BB_ZERO_WEIGHT;
        block->bbFlags |= BBF_RUN_RARELY; // Set the RarelyRun flag
    }

    /* set the right links */

    block->bbJumpKind = bNext->bbJumpKind;
    VarSetOps::AssignAllowUninitRhs(this, block->bbLiveOut, bNext->bbLiveOut);

    // Update the beginning and ending IL offsets (bbCodeOffs and bbCodeOffsEnd).
    // Set the beginning IL offset to the minimum, and the ending offset to the maximum, of the respective blocks.
    // If one block has an unknown offset, we take the other block.
    // We are merging into 'block', so if its values are correct, just leave them alone.
    // TODO: we should probably base this on the statements within.

    if (block->bbCodeOffs == BAD_IL_OFFSET)
    {
        block->bbCodeOffs = bNext->bbCodeOffs; // If they are both BAD_IL_OFFSET, this doesn't change anything.
    }
    else if (bNext->bbCodeOffs != BAD_IL_OFFSET)
    {
        // The are both valid offsets; compare them.
        if (block->bbCodeOffs > bNext->bbCodeOffs)
        {
            block->bbCodeOffs = bNext->bbCodeOffs;
        }
    }

    if (block->bbCodeOffsEnd == BAD_IL_OFFSET)
    {
        block->bbCodeOffsEnd = bNext->bbCodeOffsEnd; // If they are both BAD_IL_OFFSET, this doesn't change anything.
    }
    else if (bNext->bbCodeOffsEnd != BAD_IL_OFFSET)
    {
        // The are both valid offsets; compare them.
        if (block->bbCodeOffsEnd < bNext->bbCodeOffsEnd)
        {
            block->bbCodeOffsEnd = bNext->bbCodeOffsEnd;
        }
    }

    if (((block->bbFlags & BBF_INTERNAL) != 0) && ((bNext->bbFlags & BBF_INTERNAL) == 0))
    {
        // If 'block' is an internal block and 'bNext' isn't, then adjust the flags set on 'block'.
        block->bbFlags &= ~BBF_INTERNAL; // Clear the BBF_INTERNAL flag
        block->bbFlags |= BBF_IMPORTED;  // Set the BBF_IMPORTED flag
    }

    /* Update the flags for block with those found in bNext */

    block->bbFlags |= (bNext->bbFlags & BBF_COMPACT_UPD);

    /* mark bNext as removed */

    bNext->bbFlags |= BBF_REMOVED;

    /* Unlink bNext and update all the marker pointers if necessary */

    fgUnlinkRange(block->bbNext, bNext);

    // If bNext was the last block of a try or handler, update the EH table.

    ehUpdateForDeletedBlock(bNext);

    /* Set the jump targets */

    switch (bNext->bbJumpKind)
    {
        case BBJ_CALLFINALLY:
            // Propagate RETLESS property
            block->bbFlags |= (bNext->bbFlags & BBF_RETLESS_CALL);

            FALLTHROUGH;

        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
            block->bbJumpDest = bNext->bbJumpDest;

            /* Update the predecessor list for 'bNext->bbJumpDest' */
            fgReplacePred(bNext->bbJumpDest, bNext, block);

            /* Update the predecessor list for 'bNext->bbNext' if it is different than 'bNext->bbJumpDest' */
            if (bNext->bbJumpKind == BBJ_COND && bNext->bbJumpDest != bNext->bbNext)
            {
                fgReplacePred(bNext->bbNext, bNext, block);
            }
            break;

        case BBJ_NONE:
            /* Update the predecessor list for 'bNext->bbNext' */
            fgReplacePred(bNext->bbNext, bNext, block);
            break;

        case BBJ_EHFILTERRET:
            fgReplacePred(bNext->bbJumpDest, bNext, block);
            break;

        case BBJ_EHFINALLYRET:
        {
            unsigned  hndIndex = block->getHndIndex();
            EHblkDsc* ehDsc    = ehGetDsc(hndIndex);

            if (ehDsc->HasFinallyHandler()) // No need to do this for fault handlers
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

                    noway_assert(bcall->isBBCallAlwaysPair());
                    fgReplacePred(bcall->bbNext, bNext, block);
                }
            }
        }
        break;

        case BBJ_THROW:
        case BBJ_RETURN:
            /* no jumps or fall through blocks to set here */
            break;

        case BBJ_SWITCH:
            block->bbJumpSwt = bNext->bbJumpSwt;
            // We are moving the switch jump from bNext to block.  Examine the jump targets
            // of the BBJ_SWITCH at bNext and replace the predecessor to 'bNext' with ones to 'block'
            fgChangeSwitchBlock(bNext, block);
            break;

        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
    }

    if (bNext->isLoopAlign())
    {
        block->bbFlags |= BBF_LOOP_ALIGN;
        JITDUMP("Propagating LOOP_ALIGN flag from " FMT_BB " to " FMT_BB " during compacting.\n", bNext->bbNum,
                block->bbNum);
    }

    // If we're collapsing a block created after the dominators are
    // computed, copy block number the block and reuse dominator
    // information from bNext to block.
    //
    // Note we have to do this renumbering after the full set of pred list
    // updates above, since those updates rely on stable bbNums; if we renumber
    // before the updates, we can create pred lists with duplicate m_block->bbNum
    // values (though different m_blocks).
    //
    if (fgDomsComputed && (block->bbNum > fgDomBBcount))
    {
        BlockSetOps::Assign(this, block->bbReach, bNext->bbReach);
        BlockSetOps::ClearD(this, bNext->bbReach);

        block->bbIDom = bNext->bbIDom;
        bNext->bbIDom = nullptr;

        // In this case, there's no need to update the preorder and postorder numbering
        // since we're changing the bbNum, this makes the basic block all set.
        //
        JITDUMP("Renumbering " FMT_BB " to be " FMT_BB " to preserve dominator information\n", block->bbNum,
                bNext->bbNum);

        block->bbNum = bNext->bbNum;

        // Because we may have reordered pred lists when we swapped in
        // block for bNext above, we now need to re-reorder pred lists
        // to reflect the bbNum update.
        //
        // This process of reordering and re-reordering could likely be avoided
        // via a different update strategy. But because it's probably rare,
        // and we avoid most of the work if pred lists are already in order,
        // we'll just ensure everything is properly ordered.
        //
        for (BasicBlock* checkBlock = fgFirstBB; checkBlock != nullptr; checkBlock = checkBlock->bbNext)
        {
            checkBlock->ensurePredListOrder(this);
        }
    }

    fgUpdateLoopsAfterCompacting(block, bNext);

#if DEBUG
    if (verbose && 0)
    {
        printf("\nAfter compacting:\n");
        fgDispBasicBlocks(false);
    }
#endif

#if DEBUG
    if (JitConfig.JitSlowDebugChecksEnabled() != 0)
    {
        // Make sure that the predecessor lists are accurate
        fgDebugCheckBBlist();
    }
#endif // DEBUG
}

//-------------------------------------------------------------
// fgUpdateLoopsAfterCompacting: Update the loop table after block compaction.
//
// Arguments:
//    block - target of compaction.
//    bNext - bbNext of `block`. This block has been removed.
//
void Compiler::fgUpdateLoopsAfterCompacting(BasicBlock* block, BasicBlock* bNext)
{
    /* Check if the removed block is not part the loop table */
    noway_assert(bNext);

    for (unsigned loopNum = 0; loopNum < optLoopCount; loopNum++)
    {
        /* Some loops may have been already removed by
         * loop unrolling or conditional folding */

        if (optLoopTable[loopNum].lpFlags & LPFLG_REMOVED)
        {
            continue;
        }

        /* Check the loop head (i.e. the block preceding the loop) */

        if (optLoopTable[loopNum].lpHead == bNext)
        {
            optLoopTable[loopNum].lpHead = block;
        }

        /* Check the loop bottom */

        if (optLoopTable[loopNum].lpBottom == bNext)
        {
            optLoopTable[loopNum].lpBottom = block;
        }

        /* Check the loop exit */

        if (optLoopTable[loopNum].lpExit == bNext)
        {
            noway_assert(optLoopTable[loopNum].lpExitCnt == 1);
            optLoopTable[loopNum].lpExit = block;
        }

        /* Check the loop entry */

        if (optLoopTable[loopNum].lpEntry == bNext)
        {
            optLoopTable[loopNum].lpEntry = block;
        }

        /* Check the loop's first block */

        if (optLoopTable[loopNum].lpFirst == bNext)
        {
            optLoopTable[loopNum].lpFirst = block;
        }

        /* Check the loop top */

        if (optLoopTable[loopNum].lpTop == bNext)
        {
            optLoopTable[loopNum].lpTop = block;
        }
    }
}

//-------------------------------------------------------------
// fgUnreachableBlock: Remove a block when it is unreachable.
//
// This function cannot remove the first block.
//
// Arguments:
//    block - unreachable block to remove
//
void Compiler::fgUnreachableBlock(BasicBlock* block)
{
    // genReturnBB should never be removed, as we might have special hookups there.
    // Therefore, we should never come here to remove the statements in the genReturnBB block.
    // For example, <BUGNUM> in VSW 364383, </BUGNUM>
    // the profiler hookup needs to have the "void GT_RETURN" statement
    // to properly set the info.compProfilerCallback flag.
    noway_assert(block != genReturnBB);

    if (block->bbFlags & BBF_REMOVED)
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nRemoving unreachable " FMT_BB "\n", block->bbNum);
    }
#endif // DEBUG

    noway_assert(block->bbPrev != nullptr); // Can't use this function to remove the first block

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    assert(!block->isBBCallAlwaysPairTail()); // can't remove the BBJ_ALWAYS of a BBJ_CALLFINALLY / BBJ_ALWAYS pair
#endif

    /* First walk the statement trees in this basic block and delete each stmt */

    /* Make the block publicly available */
    compCurBB = block;

    if (block->IsLIR())
    {
        LIR::Range& blockRange = LIR::AsRange(block);
        if (!blockRange.IsEmpty())
        {
            blockRange.Delete(this, block, blockRange.FirstNode(), blockRange.LastNode());
        }
    }
    else
    {
        // TODO-Cleanup: I'm not sure why this happens -- if the block is unreachable, why does it have phis?
        // Anyway, remove any phis.

        Statement* firstNonPhi = block->FirstNonPhiDef();
        if (block->bbStmtList != firstNonPhi)
        {
            if (firstNonPhi != nullptr)
            {
                firstNonPhi->SetPrevStmt(block->lastStmt());
            }
            block->bbStmtList = firstNonPhi;
        }

        for (Statement* stmt : block->Statements())
        {
            fgRemoveStmt(block, stmt);
        }
        noway_assert(block->bbStmtList == nullptr);
    }

    /* Next update the loop table and bbWeights */
    optUpdateLoopsBeforeRemoveBlock(block);

    /* Mark the block as removed */
    block->bbFlags |= BBF_REMOVED;

    /* update bbRefs and bbPreds for the blocks reached by this block */
    fgRemoveBlockAsPred(block);
}

//-------------------------------------------------------------
// fgRemoveConditionalJump: Remove or morph a jump when we jump to the same
// block when both the condition is true or false. Remove the branch condition,
// but leave any required side effects.
//
// Arguments:
//    block - block with conditional branch
//
void Compiler::fgRemoveConditionalJump(BasicBlock* block)
{
    noway_assert(block->bbJumpKind == BBJ_COND && block->bbJumpDest == block->bbNext);
    assert(compRationalIRForm == block->IsLIR());

    flowList* flow = fgGetPredForBlock(block->bbNext, block);
    noway_assert(flow->flDupCount == 2);

    // Change the BBJ_COND to BBJ_NONE, and adjust the refCount and dupCount.
    block->bbJumpKind = BBJ_NONE;
    --block->bbNext->bbRefs;
    --flow->flDupCount;

#ifdef DEBUG
    block->bbJumpDest = nullptr;
    if (verbose)
    {
        printf("Block " FMT_BB " becoming a BBJ_NONE to " FMT_BB " (jump target is the same whether the condition"
               " is true or false)\n",
               block->bbNum, block->bbNext->bbNum);
    }
#endif

    // Remove the block jump condition

    if (block->IsLIR())
    {
        LIR::Range& blockRange = LIR::AsRange(block);

        GenTree* test = blockRange.LastNode();
        assert(test->OperIsConditionalJump());

        bool               isClosed;
        unsigned           sideEffects;
        LIR::ReadOnlyRange testRange = blockRange.GetTreeRange(test, &isClosed, &sideEffects);

        // TODO-LIR: this should really be checking GTF_ALL_EFFECT, but that produces unacceptable
        //            diffs compared to the existing backend.
        if (isClosed && ((sideEffects & GTF_SIDE_EFFECT) == 0))
        {
            // If the jump and its operands form a contiguous, side-effect-free range,
            // remove them.
            blockRange.Delete(this, block, std::move(testRange));
        }
        else
        {
            // Otherwise, just remove the jump node itself.
            blockRange.Remove(test, true);
        }
    }
    else
    {
        Statement* test = block->lastStmt();
        GenTree*   tree = test->GetRootNode();

        noway_assert(tree->gtOper == GT_JTRUE);

        GenTree* sideEffList = nullptr;

        if (tree->gtFlags & GTF_SIDE_EFFECT)
        {
            gtExtractSideEffList(tree, &sideEffList);

            if (sideEffList)
            {
                noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                if (verbose)
                {
                    printf("Extracted side effects list from condition...\n");
                    gtDispTree(sideEffList);
                    printf("\n");
                }
#endif
            }
        }

        // Delete the cond test or replace it with the side effect tree
        if (sideEffList == nullptr)
        {
            fgRemoveStmt(block, test);
        }
        else
        {
            test->SetRootNode(sideEffList);

            if (fgStmtListThreaded)
            {
                gtSetStmtInfo(test);
                fgSetStmtSeq(test);
            }
        }
    }
}

//-------------------------------------------------------------
// fgOptimizeBranchToEmptyUnconditional:
//    Optimize a jump to an empty block which ends in an unconditional branch.
//
// Arguments:
//    block - source block
//    bDest - destination
//
// Returns: true if changes were made
//
bool Compiler::fgOptimizeBranchToEmptyUnconditional(BasicBlock* block, BasicBlock* bDest)
{
    bool optimizeJump = true;

    assert(bDest->isEmpty());
    assert(bDest->bbJumpKind == BBJ_ALWAYS);

    // We do not optimize jumps between two different try regions.
    // However jumping to a block that is not in any try region is OK
    //
    if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
    {
        optimizeJump = false;
    }

    // Don't optimize a jump to a removed block
    if (bDest->bbJumpDest->bbFlags & BBF_REMOVED)
    {
        optimizeJump = false;
    }

    // Don't optimize a jump to a cloned finally
    if (bDest->bbFlags & BBF_CLONED_FINALLY_BEGIN)
    {
        optimizeJump = false;
    }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    // Don't optimize a jump to a finally target. For BB1->BB2->BB3, where
    // BB2 is a finally target, if we changed BB1 to jump directly to BB3,
    // it would skip the finally target. BB1 might be a BBJ_ALWAYS block part
    // of a BBJ_CALLFINALLY/BBJ_ALWAYS pair, so changing the finally target
    // would change the unwind behavior.
    if (bDest->bbFlags & BBF_FINALLY_TARGET)
    {
        optimizeJump = false;
    }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    // Must optimize jump if bDest has been removed
    //
    if (bDest->bbFlags & BBF_REMOVED)
    {
        optimizeJump = true;
    }

    // If we are optimizing using real profile weights
    // then don't optimize a conditional jump to an unconditional jump
    // until after we have computed the edge weights
    //
    if (fgIsUsingProfileWeights() && !fgEdgeWeightsComputed)
    {
        fgNeedsUpdateFlowGraph = true;
        optimizeJump           = false;
    }

    if (optimizeJump)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nOptimizing a jump to an unconditional jump (" FMT_BB " -> " FMT_BB " -> " FMT_BB ")\n",
                   block->bbNum, bDest->bbNum, bDest->bbJumpDest->bbNum);
        }
#endif // DEBUG

        //
        // When we optimize a branch to branch we need to update the profile weight
        // of bDest by subtracting out the block/edge weight of the path that is being optimized.
        //
        if (fgHaveValidEdgeWeights && bDest->hasProfileWeight())
        {
            flowList* edge1 = fgGetPredForBlock(bDest, block);
            noway_assert(edge1 != nullptr);

            BasicBlock::weight_t edgeWeight;

            if (edge1->edgeWeightMin() != edge1->edgeWeightMax())
            {
                //
                // We only have an estimate for the edge weight
                //
                edgeWeight = (edge1->edgeWeightMin() + edge1->edgeWeightMax()) / 2;
                //
                //  Clear the profile weight flag
                //
                bDest->bbFlags &= ~BBF_PROF_WEIGHT;
            }
            else
            {
                //
                // We only have the exact edge weight
                //
                edgeWeight = edge1->edgeWeightMin();
            }

            //
            // Update the bDest->bbWeight
            //
            if (bDest->bbWeight > edgeWeight)
            {
                bDest->bbWeight -= edgeWeight;
            }
            else
            {
                bDest->bbWeight = BB_ZERO_WEIGHT;
                bDest->bbFlags |= BBF_RUN_RARELY; // Set the RarelyRun flag
            }

            flowList* edge2 = fgGetPredForBlock(bDest->bbJumpDest, bDest);

            if (edge2 != nullptr)
            {
                //
                // Update the edge2 min/max weights
                //
                BasicBlock::weight_t newEdge2Min;
                BasicBlock::weight_t newEdge2Max;

                if (edge2->edgeWeightMin() > edge1->edgeWeightMin())
                {
                    newEdge2Min = edge2->edgeWeightMin() - edge1->edgeWeightMin();
                }
                else
                {
                    newEdge2Min = BB_ZERO_WEIGHT;
                }

                if (edge2->edgeWeightMax() > edge1->edgeWeightMin())
                {
                    newEdge2Max = edge2->edgeWeightMax() - edge1->edgeWeightMin();
                }
                else
                {
                    newEdge2Max = BB_ZERO_WEIGHT;
                }
                edge2->setEdgeWeights(newEdge2Min, newEdge2Max, bDest);
            }
        }

        // Optimize the JUMP to empty unconditional JUMP to go to the new target
        block->bbJumpDest = bDest->bbJumpDest;

        fgAddRefPred(bDest->bbJumpDest, block, fgRemoveRefPred(bDest, block));

        return true;
    }
    return false;
}

//-------------------------------------------------------------
// fgOptimizeEmptyBlock:
//   Does flow optimization of an empty block (can remove it in some cases)
//
// Arguments:
//    block - an empty block
//
// Returns: true if changes were made
//
bool Compiler::fgOptimizeEmptyBlock(BasicBlock* block)
{
    assert(block->isEmpty());

    BasicBlock* bPrev = block->bbPrev;

    switch (block->bbJumpKind)
    {
        case BBJ_COND:
        case BBJ_SWITCH:

            /* can never happen */
            noway_assert(!"Conditional or switch block with empty body!");
            break;

        case BBJ_THROW:
        case BBJ_CALLFINALLY:
        case BBJ_RETURN:
        case BBJ_EHCATCHRET:
        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:

            /* leave them as is */
            /* some compilers generate multiple returns and put all of them at the end -
             * to solve that we need the predecessor list */

            break;

        case BBJ_ALWAYS:

            // A GOTO cannot be to the next block since that
            // should have been fixed by the  optimization above
            // An exception is made for a jump from Hot to Cold
            noway_assert(block->bbJumpDest != block->bbNext || block->isBBCallAlwaysPairTail() ||
                         fgInDifferentRegions(block, block->bbNext));

            /* Cannot remove the first BB */
            if (!bPrev)
            {
                break;
            }

            /* Do not remove a block that jumps to itself - used for while (true){} */
            if (block->bbJumpDest == block)
            {
                break;
            }

            /* Empty GOTO can be removed iff bPrev is BBJ_NONE */
            if (bPrev->bbJumpKind != BBJ_NONE)
            {
                break;
            }

            // can't allow fall through into cold code
            if (block->bbNext == fgFirstColdBlock)
            {
                break;
            }

            /* Can fall through since this is similar with removing
             * a BBJ_NONE block, only the successor is different */

            FALLTHROUGH;

        case BBJ_NONE:

            /* special case if this is the first BB */
            if (!bPrev)
            {
                assert(block == fgFirstBB);
            }
            else
            {
                /* If this block follows a BBJ_CALLFINALLY do not remove it
                 * (because we don't know who may jump to it) */
                if (bPrev->bbJumpKind == BBJ_CALLFINALLY)
                {
                    break;
                }
            }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            /* Don't remove finally targets */
            if (block->bbFlags & BBF_FINALLY_TARGET)
                break;
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

#if defined(FEATURE_EH_FUNCLETS)
            /* Don't remove an empty block that is in a different EH region
             * from its successor block, if the block is the target of a
             * catch return. It is required that the return address of a
             * catch be in the correct EH region, for re-raise of thread
             * abort exceptions to work. Insert a NOP in the empty block
             * to ensure we generate code for the block, if we keep it.
             */
            {
                BasicBlock* succBlock;

                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    succBlock = block->bbJumpDest;
                }
                else
                {
                    succBlock = block->bbNext;
                }

                if ((succBlock != nullptr) && !BasicBlock::sameEHRegion(block, succBlock))
                {
                    // The empty block and the block that follows it are in different
                    // EH regions. Is this a case where they can't be merged?

                    bool okToMerge = true; // assume it's ok
                    for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
                    {
                        if (pred->getBlock()->bbJumpKind == BBJ_EHCATCHRET)
                        {
                            assert(pred->getBlock()->bbJumpDest == block);
                            okToMerge = false; // we can't get rid of the empty block
                            break;
                        }
                    }

                    if (!okToMerge)
                    {
                        // Insert a NOP in the empty block to ensure we generate code
                        // for the catchret target in the right EH region.
                        GenTree* nop = new (this, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);

                        if (block->IsLIR())
                        {
                            LIR::AsRange(block).InsertAtEnd(nop);
                            LIR::ReadOnlyRange range(nop, nop);
                            m_pLowering->LowerRange(block, range);
                        }
                        else
                        {
                            Statement* nopStmt = fgNewStmtAtEnd(block, nop);
                            fgSetStmtSeq(nopStmt);
                            gtSetStmtInfo(nopStmt);
                        }

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("\nKeeping empty block " FMT_BB " - it is the target of a catch return\n",
                                   block->bbNum);
                        }
#endif // DEBUG

                        break; // go to the next block
                    }
                }
            }
#endif // FEATURE_EH_FUNCLETS

            if (!ehCanDeleteEmptyBlock(block))
            {
                // We're not allowed to remove this block due to reasons related to the EH table.
                break;
            }

            /* special case if this is the last BB */
            if (block == fgLastBB)
            {
                if (!bPrev)
                {
                    break;
                }
                fgLastBB = bPrev;
            }

            // When using profile weights, fgComputeEdgeWeights expects the first non-internal block to have profile
            // weight.
            // Make sure we don't break that invariant.
            if (fgIsUsingProfileWeights() && block->hasProfileWeight() && (block->bbFlags & BBF_INTERNAL) == 0)
            {
                BasicBlock* bNext = block->bbNext;

                // Check if the next block can't maintain the invariant.
                if ((bNext == nullptr) || ((bNext->bbFlags & BBF_INTERNAL) != 0) || !bNext->hasProfileWeight())
                {
                    // Check if the current block is the first non-internal block.
                    BasicBlock* curBB = bPrev;
                    while ((curBB != nullptr) && (curBB->bbFlags & BBF_INTERNAL) != 0)
                    {
                        curBB = curBB->bbPrev;
                    }
                    if (curBB == nullptr)
                    {
                        // This block is the first non-internal block and it has profile weight.
                        // Don't delete it.
                        break;
                    }
                }
            }

            /* Remove the block */
            compCurBB = block;
            fgRemoveBlock(block, false);
            return true;

        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
    }
    return false;
}

//-------------------------------------------------------------
// fgOptimizeSwitchBranches:
//   Does flow optimization for a switch - bypasses jumps to empty unconditional branches,
//   and transforms degenerate switch cases like those with 1 or 2 targets.
//
// Arguments:
//    block - block with switch
//
// Returns: true if changes were made
//
bool Compiler::fgOptimizeSwitchBranches(BasicBlock* block)
{
    assert(block->bbJumpKind == BBJ_SWITCH);

    unsigned     jmpCnt = block->bbJumpSwt->bbsCount;
    BasicBlock** jmpTab = block->bbJumpSwt->bbsDstTab;
    BasicBlock*  bNewDest; // the new jump target for the current switch case
    BasicBlock*  bDest;
    bool         returnvalue = false;

    do
    {
    REPEAT_SWITCH:;
        bDest    = *jmpTab;
        bNewDest = bDest;

        // Do we have a JUMP to an empty unconditional JUMP block?
        if (bDest->isEmpty() && (bDest->bbJumpKind == BBJ_ALWAYS) &&
            (bDest != bDest->bbJumpDest)) // special case for self jumps
        {
            bool optimizeJump = true;

            // We do not optimize jumps between two different try regions.
            // However jumping to a block that is not in any try region is OK
            //
            if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
            {
                optimizeJump = false;
            }

            // If we are optimize using real profile weights
            // then don't optimize a switch jump to an unconditional jump
            // until after we have computed the edge weights
            //
            if (fgIsUsingProfileWeights() && !fgEdgeWeightsComputed)
            {
                fgNeedsUpdateFlowGraph = true;
                optimizeJump           = false;
            }

            if (optimizeJump)
            {
                bNewDest = bDest->bbJumpDest;
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nOptimizing a switch jump to an empty block with an unconditional jump (" FMT_BB
                           " -> " FMT_BB " -> " FMT_BB ")\n",
                           block->bbNum, bDest->bbNum, bNewDest->bbNum);
                }
#endif // DEBUG
            }
        }

        if (bNewDest != bDest)
        {
            //
            // When we optimize a branch to branch we need to update the profile weight
            // of bDest by subtracting out the block/edge weight of the path that is being optimized.
            //
            if (fgIsUsingProfileWeights() && bDest->hasProfileWeight())
            {
                if (fgHaveValidEdgeWeights)
                {
                    flowList*            edge                = fgGetPredForBlock(bDest, block);
                    BasicBlock::weight_t branchThroughWeight = edge->edgeWeightMin();

                    if (bDest->bbWeight > branchThroughWeight)
                    {
                        bDest->bbWeight -= branchThroughWeight;
                    }
                    else
                    {
                        bDest->bbWeight = BB_ZERO_WEIGHT;
                        bDest->bbFlags |= BBF_RUN_RARELY;
                    }
                }
            }

            // Update the switch jump table
            *jmpTab = bNewDest;

            // Maintain, if necessary, the set of unique targets of "block."
            UpdateSwitchTableTarget(block, bDest, bNewDest);

            fgAddRefPred(bNewDest, block, fgRemoveRefPred(bDest, block));

            // we optimized a Switch label - goto REPEAT_SWITCH to follow this new jump
            returnvalue = true;

            goto REPEAT_SWITCH;
        }
    } while (++jmpTab, --jmpCnt);

    Statement*  switchStmt = nullptr;
    LIR::Range* blockRange = nullptr;

    GenTree* switchTree;
    if (block->IsLIR())
    {
        blockRange = &LIR::AsRange(block);
        switchTree = blockRange->LastNode();

        assert(switchTree->OperGet() == GT_SWITCH_TABLE);
    }
    else
    {
        switchStmt = block->lastStmt();
        switchTree = switchStmt->GetRootNode();

        assert(switchTree->OperGet() == GT_SWITCH);
    }

    noway_assert(switchTree->gtType == TYP_VOID);

    // At this point all of the case jump targets have been updated such
    // that none of them go to block that is an empty unconditional block
    //
    jmpTab = block->bbJumpSwt->bbsDstTab;
    jmpCnt = block->bbJumpSwt->bbsCount;

    // Now check for two trivial switch jumps.
    //
    if (block->NumSucc(this) == 1)
    {
        // Use BBJ_ALWAYS for a switch with only a default clause, or with only one unique successor.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nRemoving a switch jump with a single target (" FMT_BB ")\n", block->bbNum);
            printf("BEFORE:\n");
        }
#endif // DEBUG

        if (block->IsLIR())
        {
            bool               isClosed;
            unsigned           sideEffects;
            LIR::ReadOnlyRange switchTreeRange = blockRange->GetTreeRange(switchTree, &isClosed, &sideEffects);

            // The switch tree should form a contiguous, side-effect free range by construction. See
            // Lowering::LowerSwitch for details.
            assert(isClosed);
            assert((sideEffects & GTF_ALL_EFFECT) == 0);

            blockRange->Delete(this, block, std::move(switchTreeRange));
        }
        else
        {
            /* check for SIDE_EFFECTS */
            if (switchTree->gtFlags & GTF_SIDE_EFFECT)
            {
                /* Extract the side effects from the conditional */
                GenTree* sideEffList = nullptr;

                gtExtractSideEffList(switchTree, &sideEffList);

                if (sideEffList == nullptr)
                {
                    goto NO_SWITCH_SIDE_EFFECT;
                }

                noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);

#ifdef DEBUG
                if (verbose)
                {
                    printf("\nSwitch expression has side effects! Extracting side effects...\n");
                    gtDispTree(switchTree);
                    printf("\n");
                    gtDispTree(sideEffList);
                    printf("\n");
                }
#endif // DEBUG

                /* Replace the conditional statement with the list of side effects */
                noway_assert(sideEffList->gtOper != GT_SWITCH);

                switchStmt->SetRootNode(sideEffList);

                if (fgStmtListThreaded)
                {
                    compCurBB = block;

                    /* Update ordering, costs, FP levels, etc. */
                    gtSetStmtInfo(switchStmt);

                    /* Re-link the nodes for this statement */
                    fgSetStmtSeq(switchStmt);
                }
            }
            else
            {

            NO_SWITCH_SIDE_EFFECT:

                /* conditional has NO side effect - remove it */
                fgRemoveStmt(block, switchStmt);
            }
        }

        // Change the switch jump into a BBJ_ALWAYS
        block->bbJumpDest = block->bbJumpSwt->bbsDstTab[0];
        block->bbJumpKind = BBJ_ALWAYS;
        if (jmpCnt > 1)
        {
            for (unsigned i = 1; i < jmpCnt; ++i)
            {
                (void)fgRemoveRefPred(jmpTab[i], block);
            }
        }

        return true;
    }
    else if (block->bbJumpSwt->bbsCount == 2 && block->bbJumpSwt->bbsDstTab[1] == block->bbNext)
    {
        /* Use a BBJ_COND(switchVal==0) for a switch with only one
           significant clause besides the default clause, if the
           default clause is bbNext */
        GenTree* switchVal = switchTree->AsOp()->gtOp1;
        noway_assert(genActualTypeIsIntOrI(switchVal->TypeGet()));

        // If we are in LIR, remove the jump table from the block.
        if (block->IsLIR())
        {
            GenTree* jumpTable = switchTree->AsOp()->gtOp2;
            assert(jumpTable->OperGet() == GT_JMPTABLE);
            blockRange->Remove(jumpTable);
        }

        // Change the GT_SWITCH(switchVal) into GT_JTRUE(GT_EQ(switchVal==0)).
        // Also mark the node as GTF_DONT_CSE as further down JIT is not capable of handling it.
        // For example CSE could determine that the expression rooted at GT_EQ is a candidate cse and
        // replace it with a COMMA node.  In such a case we will end up with GT_JTRUE node pointing to
        // a COMMA node which results in noway asserts in fgMorphSmpOp(), optAssertionGen() and rpPredictTreeRegUse().
        // For the same reason fgMorphSmpOp() marks GT_JTRUE nodes with RELOP children as GTF_DONT_CSE.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        if (verbose)
        {
            printf("\nConverting a switch (" FMT_BB ") with only one significant clause besides a default target to a "
                   "conditional branch\n",
                   block->bbNum);
        }
#endif // DEBUG

        switchTree->ChangeOper(GT_JTRUE);
        GenTree* zeroConstNode    = gtNewZeroConNode(genActualType(switchVal->TypeGet()));
        GenTree* condNode         = gtNewOperNode(GT_EQ, TYP_INT, switchVal, zeroConstNode);
        switchTree->AsOp()->gtOp1 = condNode;
        switchTree->AsOp()->gtOp1->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

        if (block->IsLIR())
        {
            blockRange->InsertAfter(switchVal, zeroConstNode, condNode);
            LIR::ReadOnlyRange range(zeroConstNode, switchTree);
            m_pLowering->LowerRange(block, range);
        }
        else if (fgStmtListThreaded)
        {
            gtSetStmtInfo(switchStmt);
            fgSetStmtSeq(switchStmt);
        }

        block->bbJumpDest = block->bbJumpSwt->bbsDstTab[0];
        block->bbJumpKind = BBJ_COND;

        return true;
    }
    return returnvalue;
}

//-------------------------------------------------------------
// fgBlockEndFavorsTailDuplication:
//     Heuristic function that returns true if this block ends in a statement that looks favorable
//     for tail-duplicating its successor (such as assigning a constant to a local).
//
//  Arguments:
//      block: BasicBlock we are considering duplicating the successor of
//      lclNum: local that is used by the successor block, provided by
//        prior call to fgBlockIsGoodTailDuplicationCandidate
//
//  Returns:
//     true if block end is favorable for tail duplication
//
//  Notes:
//     This is the second half of the evaluation for tail duplication, where we try
//     to determine if this predecessor block assigns a constant or provides useful
//     information about a local that is tested in an unconditionally executed successor.
//     If so then duplicating the successor will likely allow the test to be
//     optimized away.
//
bool Compiler::fgBlockEndFavorsTailDuplication(BasicBlock* block, unsigned lclNum)
{
    if (block->isRunRarely())
    {
        return false;
    }

    // If the local is address exposed, we currently can't optimize.
    //
    LclVarDsc* const lclDsc = lvaGetDesc(lclNum);

    if (lclDsc->lvAddrExposed)
    {
        return false;
    }

    Statement* const lastStmt  = block->lastStmt();
    Statement* const firstStmt = block->FirstNonPhiDef();

    if (lastStmt == nullptr)
    {
        return false;
    }

    // Tail duplication tends to pay off when the last statement
    // is an assignment of a constant, arraylength, or a relop.
    // This is because these statements produce information about values
    // that would otherwise be lost at the upcoming merge point.
    //
    // Check up to N statements...
    //
    const int  limit = 2;
    int        count = 0;
    Statement* stmt  = lastStmt;

    while (count < limit)
    {
        count++;
        GenTree* const tree = stmt->GetRootNode();
        if (tree->OperIs(GT_ASG) && !tree->OperIsBlkOp())
        {
            GenTree* const op1 = tree->AsOp()->gtOp1;

            if (op1->IsLocal())
            {
                const unsigned op1LclNum = op1->AsLclVarCommon()->GetLclNum();

                if (op1LclNum == lclNum)
                {
                    GenTree* const op2 = tree->AsOp()->gtOp2;

                    if (op2->OperIs(GT_ARR_LENGTH) || op2->OperIsConst() || op2->OperIsCompare())
                    {
                        return true;
                    }
                }
            }
        }

        Statement* const prevStmt = stmt->GetPrevStmt();

        // The statement list prev links wrap from first->last, so exit
        // when we see lastStmt again, as we've now seen all statements.
        //
        if (prevStmt == lastStmt)
        {
            break;
        }

        stmt = prevStmt;
    }

    return false;
}

//-------------------------------------------------------------
// fgBlockIsGoodTailDuplicationCandidate:
//     Heuristic function that examines a block (presumably one that is a merge point) to determine
//     if it is a good candidate to be duplicated.
//
// Arguments:
//     target - the tail block (candidate for duplication)
//
// Returns:
//     true if this is a good candidate, false otherwise
//     if true, lclNum is set to lcl to scan for in predecessor block
//
// Notes:
//     The current heuristic is that tail duplication is deemed favorable if this
//     block simply tests the value of a local against a constant or some other local.
//
//     This is the first half of the evaluation for tail duplication. We subsequently
//     need to check if predecessors of this block assigns a constant to the local.
//
bool Compiler::fgBlockIsGoodTailDuplicationCandidate(BasicBlock* target, unsigned* lclNum)
{
    *lclNum = BAD_VAR_NUM;

    // Here we are looking for blocks with a single statement feeding a conditional branch.
    // These blocks are small, and when duplicated onto the tail of blocks that end in
    // assignments, there is a high probability of the branch completely going away.
    //
    // This is by no means the only kind of tail that it is beneficial to duplicate,
    // just the only one we recognize for now.
    if (target->bbJumpKind != BBJ_COND)
    {
        return false;
    }

    // No point duplicating this block if it's not a control flow join.
    if (target->bbRefs < 2)
    {
        return false;
    }

    Statement* stmt = target->FirstNonPhiDef();

    if (stmt != target->lastStmt())
    {
        return false;
    }

    GenTree* tree = stmt->GetRootNode();

    if (tree->gtOper != GT_JTRUE)
    {
        return false;
    }

    // must be some kind of relational operator
    GenTree* const cond = tree->AsOp()->gtOp1;
    if (!cond->OperIsCompare())
    {
        return false;
    }

    // op1 must be some combinations of casts of local or constant
    GenTree* op1 = cond->AsOp()->gtOp1;
    while (op1->gtOper == GT_CAST)
    {
        op1 = op1->AsOp()->gtOp1;
    }

    if (!op1->IsLocal() && !op1->OperIsConst())
    {
        return false;
    }

    // op2 must be some combinations of casts of local or constant
    GenTree* op2 = cond->AsOp()->gtOp2;
    while (op2->gtOper == GT_CAST)
    {
        op2 = op2->AsOp()->gtOp1;
    }

    if (!op2->IsLocal() && !op2->OperIsConst())
    {
        return false;
    }

    // Tree must have one constant and one local, or be comparing
    // the same local to itself.
    unsigned lcl1 = BAD_VAR_NUM;
    unsigned lcl2 = BAD_VAR_NUM;

    if (op1->IsLocal())
    {
        lcl1 = op1->AsLclVarCommon()->GetLclNum();
    }

    if (op2->IsLocal())
    {
        lcl2 = op2->AsLclVarCommon()->GetLclNum();
    }

    if ((lcl1 != BAD_VAR_NUM) && op2->OperIsConst())
    {
        *lclNum = lcl1;
    }
    else if ((lcl2 != BAD_VAR_NUM) && op1->OperIsConst())
    {
        *lclNum = lcl2;
    }
    else if ((lcl1 != BAD_VAR_NUM) && (lcl1 == lcl2))
    {
        *lclNum = lcl1;
    }
    else
    {
        return false;
    }

    return true;
}

//-------------------------------------------------------------
// fgOptimizeUncondBranchToSimpleCond:
//    For a block which has an unconditional branch, look to see if its target block
//    is a good candidate for tail duplication, and if so do that duplication.
//
// Arguments:
//    block  - block with uncond branch
//    target - block which is target of first block
//
// Returns: true if changes were made
//
// Notes:
//   This optimization generally reduces code size and path length.
//
bool Compiler::fgOptimizeUncondBranchToSimpleCond(BasicBlock* block, BasicBlock* target)
{
    if (!BasicBlock::sameEHRegion(block, target))
    {
        return false;
    }

    unsigned lclNum = BAD_VAR_NUM;

    // First check if the successor tests a local and then branches on the result
    // of a test, and obtain the local if so.
    //
    if (!fgBlockIsGoodTailDuplicationCandidate(target, &lclNum))
    {
        return false;
    }

    // See if this block assigns constant or other interesting tree to that same local.
    //
    if (!fgBlockEndFavorsTailDuplication(block, lclNum))
    {
        return false;
    }

    // NOTE: we do not currently hit this assert because this function is only called when
    // `fgUpdateFlowGraph` has been called with `doTailDuplication` set to true, and the
    // backend always calls `fgUpdateFlowGraph` with `doTailDuplication` set to false.
    assert(!block->IsLIR());

    Statement* stmt = target->FirstNonPhiDef();
    assert(stmt == target->lastStmt());

    // Duplicate the target block at the end of this block
    GenTree* cloned = gtCloneExpr(stmt->GetRootNode());
    noway_assert(cloned);
    Statement* jmpStmt = gtNewStmt(cloned);

    block->bbJumpKind = BBJ_COND;
    block->bbJumpDest = target->bbJumpDest;
    fgAddRefPred(block->bbJumpDest, block);
    fgRemoveRefPred(target, block);

    // add an unconditional block after this block to jump to the target block's fallthrough block
    BasicBlock* next = fgNewBBafter(BBJ_ALWAYS, block, true);

    // The new block 'next' will inherit its weight from 'block'
    next->inheritWeight(block);
    next->bbJumpDest = target->bbNext;
    fgAddRefPred(next, block);
    fgAddRefPred(next->bbJumpDest, next);

    JITDUMP("fgOptimizeUncondBranchToSimpleCond(from " FMT_BB " to cond " FMT_BB "), created new uncond " FMT_BB "\n",
            block->bbNum, target->bbNum, next->bbNum);
    JITDUMP("   expecting opts to key off V%02u, added cloned compare [%06u] to " FMT_BB "\n", lclNum,
            dspTreeID(cloned), block->bbNum);

    if (fgStmtListThreaded)
    {
        gtSetStmtInfo(jmpStmt);
    }

    fgInsertStmtAtEnd(block, jmpStmt);

    return true;
}

//-------------------------------------------------------------
// fgOptimizeBranchToNext:
//    Optimize a block which has a branch to the following block
//
// Arguments:
//    block - block with a branch
//    bNext - block which is both next and the target of the first block
//    bPrev - block which is prior to the first block
//
// Returns: true if changes were made
//
bool Compiler::fgOptimizeBranchToNext(BasicBlock* block, BasicBlock* bNext, BasicBlock* bPrev)
{
    assert(block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_ALWAYS);
    assert(block->bbJumpDest == bNext);
    assert(block->bbNext == bNext);
    assert(block->bbPrev == bPrev);

    if (block->bbJumpKind == BBJ_ALWAYS)
    {
        // We can't remove it if it is a branch from hot => cold
        if (!fgInDifferentRegions(block, bNext))
        {
            // We can't remove if it is marked as BBF_KEEP_BBJ_ALWAYS
            if (!(block->bbFlags & BBF_KEEP_BBJ_ALWAYS))
            {
                // We can't remove if the BBJ_ALWAYS is part of a BBJ_CALLFINALLY pair
                if (!block->isBBCallAlwaysPairTail())
                {
                    /* the unconditional jump is to the next BB  */
                    block->bbJumpKind = BBJ_NONE;
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nRemoving unconditional jump to next block (" FMT_BB " -> " FMT_BB
                               ") (converted " FMT_BB " to "
                               "fall-through)\n",
                               block->bbNum, bNext->bbNum, block->bbNum);
                    }
#endif // DEBUG
                    return true;
                }
            }
        }
    }
    else
    {
        /* remove the conditional statement at the end of block */
        noway_assert(block->bbJumpKind == BBJ_COND);
        noway_assert(block->isValid());

#ifdef DEBUG
        if (verbose)
        {
            printf("\nRemoving conditional jump to next block (" FMT_BB " -> " FMT_BB ")\n", block->bbNum,
                   bNext->bbNum);
        }
#endif // DEBUG

        if (block->IsLIR())
        {
            LIR::Range& blockRange = LIR::AsRange(block);
            GenTree*    jmp        = blockRange.LastNode();
            assert(jmp->OperIsConditionalJump());
            if (jmp->OperGet() == GT_JTRUE)
            {
                jmp->AsOp()->gtOp1->gtFlags &= ~GTF_SET_FLAGS;
            }

            bool               isClosed;
            unsigned           sideEffects;
            LIR::ReadOnlyRange jmpRange = blockRange.GetTreeRange(jmp, &isClosed, &sideEffects);

            // TODO-LIR: this should really be checking GTF_ALL_EFFECT, but that produces unacceptable
            //            diffs compared to the existing backend.
            if (isClosed && ((sideEffects & GTF_SIDE_EFFECT) == 0))
            {
                // If the jump and its operands form a contiguous, side-effect-free range,
                // remove them.
                blockRange.Delete(this, block, std::move(jmpRange));
            }
            else
            {
                // Otherwise, just remove the jump node itself.
                blockRange.Remove(jmp, true);
            }
        }
        else
        {
            Statement* condStmt = block->lastStmt();
            GenTree*   cond     = condStmt->GetRootNode();
            noway_assert(cond->gtOper == GT_JTRUE);

            /* check for SIDE_EFFECTS */
            if (cond->gtFlags & GTF_SIDE_EFFECT)
            {
                /* Extract the side effects from the conditional */
                GenTree* sideEffList = nullptr;

                gtExtractSideEffList(cond, &sideEffList);

                if (sideEffList == nullptr)
                {
                    compCurBB = block;
                    fgRemoveStmt(block, condStmt);
                }
                else
                {
                    noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nConditional has side effects! Extracting side effects...\n");
                        gtDispTree(cond);
                        printf("\n");
                        gtDispTree(sideEffList);
                        printf("\n");
                    }
#endif // DEBUG

                    /* Replace the conditional statement with the list of side effects */
                    noway_assert(sideEffList->gtOper != GT_JTRUE);

                    condStmt->SetRootNode(sideEffList);

                    if (fgStmtListThreaded)
                    {
                        compCurBB = block;

                        /* Update ordering, costs, FP levels, etc. */
                        gtSetStmtInfo(condStmt);

                        /* Re-link the nodes for this statement */
                        fgSetStmtSeq(condStmt);
                    }
                }
            }
            else
            {
                compCurBB = block;
                /* conditional has NO side effect - remove it */
                fgRemoveStmt(block, condStmt);
            }
        }

        /* Conditional is gone - simply fall into the next block */

        block->bbJumpKind = BBJ_NONE;

        /* Update bbRefs and bbNum - Conditional predecessors to the same
         * block are counted twice so we have to remove one of them */

        noway_assert(bNext->countOfInEdges() > 1);
        fgRemoveRefPred(bNext, block);

        return true;
    }
    return false;
}

//-------------------------------------------------------------
// fgOptimizeBranch: Optimize an unconditional branch that branches to a conditional branch.
//
// Currently we require that the conditional branch jump back to the block that follows the unconditional
// branch. We can improve the code execution and layout by concatenating a copy of the conditional branch
// block at the end of the conditional branch and reversing the sense of the branch.
//
// This is only done when the amount of code to be copied is smaller than our calculated threshold
// in maxDupCostSz.
//
// Arguments:
//    bJump - block with branch
//
// Returns: true if changes were made
//
bool Compiler::fgOptimizeBranch(BasicBlock* bJump)
{
    if (opts.MinOpts())
    {
        return false;
    }

    if (bJump->bbJumpKind != BBJ_ALWAYS)
    {
        return false;
    }

    if (bJump->bbFlags & BBF_KEEP_BBJ_ALWAYS)
    {
        return false;
    }

    // Don't hoist a conditional branch into the scratch block; we'd prefer it stay
    // either BBJ_NONE or BBJ_ALWAYS.
    if (fgBBisScratch(bJump))
    {
        return false;
    }

    BasicBlock* bDest = bJump->bbJumpDest;

    if (bDest->bbJumpKind != BBJ_COND)
    {
        return false;
    }

    if (bDest->bbJumpDest != bJump->bbNext)
    {
        return false;
    }

    // 'bJump' must be in the same try region as the condition, since we're going to insert
    // a duplicated condition in 'bJump', and the condition might include exception throwing code.
    if (!BasicBlock::sameTryRegion(bJump, bDest))
    {
        return false;
    }

    // do not jump into another try region
    BasicBlock* bDestNext = bDest->bbNext;
    if (bDestNext->hasTryIndex() && !BasicBlock::sameTryRegion(bJump, bDestNext))
    {
        return false;
    }

    // This function is only called by fgReorderBlocks, which we do not run in the backend.
    // If we wanted to run block reordering in the backend, we would need to be able to
    // calculate cost information for LIR on a per-node basis in order for this function
    // to work.
    assert(!bJump->IsLIR());
    assert(!bDest->IsLIR());

    unsigned estDupCostSz = 0;
    for (Statement* stmt : bDest->Statements())
    {
        // We want to compute the costs of the statement. Unfortunately, gtPrepareCost() / gtSetStmtInfo()
        // call gtSetEvalOrder(), which can reorder nodes. If it does so, we need to re-thread the gtNext/gtPrev
        // links. We don't know if it does or doesn't reorder nodes, so we end up always re-threading the links.

        gtSetStmtInfo(stmt);
        if (fgStmtListThreaded)
        {
            fgSetStmtSeq(stmt);
        }

        GenTree* expr = stmt->GetRootNode();
        estDupCostSz += expr->GetCostSz();
    }

    bool                 allProfileWeightsAreValid = false;
    BasicBlock::weight_t weightJump                = bJump->bbWeight;
    BasicBlock::weight_t weightDest                = bDest->bbWeight;
    BasicBlock::weight_t weightNext                = bJump->bbNext->bbWeight;
    bool                 rareJump                  = bJump->isRunRarely();
    bool                 rareDest                  = bDest->isRunRarely();
    bool                 rareNext                  = bJump->bbNext->isRunRarely();

    // If we have profile data then we calculate the number of time
    // the loop will iterate into loopIterations
    if (fgIsUsingProfileWeights())
    {
        // Only rely upon the profile weight when all three of these blocks
        // have either good profile weights or are rarelyRun
        //
        if ((bJump->bbFlags & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)) &&
            (bDest->bbFlags & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)) &&
            (bJump->bbNext->bbFlags & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)))
        {
            allProfileWeightsAreValid = true;

            if ((weightJump * 100) < weightDest)
            {
                rareJump = true;
            }

            if ((weightNext * 100) < weightDest)
            {
                rareNext = true;
            }

            if (((weightDest * 100) < weightJump) && ((weightDest * 100) < weightNext))
            {
                rareDest = true;
            }
        }
    }

    unsigned maxDupCostSz = 6;

    //
    // Branches between the hot and rarely run regions
    // should be minimized.  So we allow a larger size
    //
    if (rareDest != rareJump)
    {
        maxDupCostSz += 6;
    }

    if (rareDest != rareNext)
    {
        maxDupCostSz += 6;
    }

    //
    // We we are ngen-ing:
    // If the uncondional branch is a rarely run block then
    // we are willing to have more code expansion since we
    // won't be running code from this page
    //
    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        if (rareJump)
        {
            maxDupCostSz *= 2;
        }
    }

    // If the compare has too high cost then we don't want to dup

    bool costIsTooHigh = (estDupCostSz > maxDupCostSz);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDuplication of the conditional block " FMT_BB " (always branch from " FMT_BB
               ") %s, because the cost of duplication (%i) is %s than %i, validProfileWeights = %s\n",
               bDest->bbNum, bJump->bbNum, costIsTooHigh ? "not done" : "performed", estDupCostSz,
               costIsTooHigh ? "greater" : "less or equal", maxDupCostSz, allProfileWeightsAreValid ? "true" : "false");
    }
#endif // DEBUG

    if (costIsTooHigh)
    {
        return false;
    }

    /* Looks good - duplicate the conditional block */

    Statement* newStmtList = nullptr; // new stmt list to be added to bJump
    Statement* newLastStmt = nullptr;

    /* Visit all the statements in bDest */

    for (Statement* curStmt : bDest->Statements())
    {
        // Clone/substitute the expression.
        Statement* stmt = gtCloneStmt(curStmt);

        // cloneExpr doesn't handle everything.
        if (stmt == nullptr)
        {
            return false;
        }

        if (fgStmtListThreaded)
        {
            gtSetStmtInfo(stmt);
            fgSetStmtSeq(stmt);
        }

        /* Append the expression to our list */

        if (newStmtList != nullptr)
        {
            newLastStmt->SetNextStmt(stmt);
        }
        else
        {
            newStmtList = stmt;
        }

        stmt->SetPrevStmt(newLastStmt);
        newLastStmt = stmt;
    }

    // Get to the condition node from the statement tree.
    GenTree* condTree = newLastStmt->GetRootNode();
    noway_assert(condTree->gtOper == GT_JTRUE);

    // Set condTree to the operand to the GT_JTRUE.
    condTree = condTree->AsOp()->gtOp1;

    // This condTree has to be a RelOp comparison.
    if (condTree->OperIsCompare() == false)
    {
        return false;
    }

    // Join the two linked lists.
    Statement* lastStmt = bJump->lastStmt();

    if (lastStmt != nullptr)
    {
        Statement* stmt = bJump->firstStmt();
        stmt->SetPrevStmt(newLastStmt);
        lastStmt->SetNextStmt(newStmtList);
        newStmtList->SetPrevStmt(lastStmt);
    }
    else
    {
        bJump->bbStmtList = newStmtList;
        newStmtList->SetPrevStmt(newLastStmt);
    }

    //
    // Reverse the sense of the compare
    //
    gtReverseCond(condTree);

    // We need to update the following flags of the bJump block if they were set in the bDest block
    bJump->bbFlags |= (bDest->bbFlags & (BBF_HAS_NEWOBJ | BBF_HAS_NEWARRAY | BBF_HAS_NULLCHECK | BBF_HAS_IDX_LEN));

    bJump->bbJumpKind = BBJ_COND;
    bJump->bbJumpDest = bDest->bbNext;

    /* Update bbRefs and bbPreds */

    // bJump now falls through into the next block
    //
    fgAddRefPred(bJump->bbNext, bJump);

    // bJump no longer jumps to bDest
    //
    fgRemoveRefPred(bDest, bJump);

    // bJump now jumps to bDest->bbNext
    //
    fgAddRefPred(bDest->bbNext, bJump);

    if (weightJump > 0)
    {
        if (allProfileWeightsAreValid)
        {
            if (weightDest > weightJump)
            {
                bDest->bbWeight = (weightDest - weightJump);
            }
            else if (!bDest->isRunRarely())
            {
                bDest->bbWeight = BB_UNITY_WEIGHT;
            }
        }
        else
        {
            BasicBlock::weight_t newWeightDest = 0;

            if (weightDest > weightJump)
            {
                newWeightDest = (weightDest - weightJump);
            }
            if (weightDest >= (BB_LOOP_WEIGHT_SCALE * BB_UNITY_WEIGHT) / 2)
            {
                newWeightDest = (weightDest * 2) / (BB_LOOP_WEIGHT_SCALE * BB_UNITY_WEIGHT);
            }
            if (newWeightDest > 0)
            {
                bDest->bbWeight = newWeightDest;
            }
        }
    }

#if DEBUG
    if (verbose)
    {
        // Dump out the newStmtList that we created
        printf("\nfgOptimizeBranch added these statements(s) at the end of " FMT_BB ":\n", bJump->bbNum);
        for (Statement* stmt : StatementList(newStmtList))
        {
            gtDispStmt(stmt);
        }
        printf("\nfgOptimizeBranch changed block " FMT_BB " from BBJ_ALWAYS to BBJ_COND.\n", bJump->bbNum);

        printf("\nAfter this change in fgOptimizeBranch the BB graph is:");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    return true;
}

/*****************************************************************************
 *
 *  Function called to optimize switch statements
 */

bool Compiler::fgOptimizeSwitchJumps()
{
    bool result = false; // Our return value

#if 0
    // TODO-CQ: Add switch jump optimizations?
    if (!fgHasSwitch)
        return false;

    if (!fgHaveValidEdgeWeights)
        return false;

    for (BasicBlock* bSrc = fgFirstBB; bSrc != NULL; bSrc = bSrc->bbNext)
    {
        if (bSrc->bbJumpKind == BBJ_SWITCH)
        {
            unsigned        jumpCnt; jumpCnt = bSrc->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab; jumpTab = bSrc->bbJumpSwt->bbsDstTab;

            do
            {
                BasicBlock*   bDst       = *jumpTab;
                flowList*     edgeToDst  = fgGetPredForBlock(bDst, bSrc);
                double        outRatio   = (double) edgeToDst->edgeWeightMin()  / (double) bSrc->bbWeight;

                if (outRatio >= 0.60)
                {
                    // straighten switch here...
                }
            }
            while (++jumpTab, --jumpCnt);
        }
    }
#endif

    return result;
}

//-----------------------------------------------------------------------------
// fgExpandRunRarelyBlocks: given the current set of run rarely blocks,
//   see if we can deduce that some other blocks are run rarely.
//
// Returns:
//    True if new block was marked as run rarely.
//
bool Compiler::fgExpandRarelyRunBlocks()
{
    bool result = false;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgExpandRarelyRunBlocks()\n");
    }

    const char* reason = nullptr;
#endif

    // Helper routine to figure out the lexically earliest predecessor
    // of bPrev that could become run rarely, given that bPrev
    // has just become run rarely.
    //
    // Note this is potentially expensive for large flow graphs and blocks
    // with lots of predecessors.
    //
    auto newRunRarely = [](BasicBlock* block, BasicBlock* bPrev) {
        // Figure out earliest block that might be impacted
        BasicBlock* bPrevPrev = nullptr;
        BasicBlock* tmpbb;

        if ((bPrev->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0)
        {
            // If we've got a BBJ_CALLFINALLY/BBJ_ALWAYS pair, treat the BBJ_CALLFINALLY as an
            // additional predecessor for the BBJ_ALWAYS block
            tmpbb = bPrev->bbPrev;
            noway_assert(tmpbb != nullptr);
#if defined(FEATURE_EH_FUNCLETS)
            noway_assert(tmpbb->isBBCallAlwaysPair());
            bPrevPrev = tmpbb;
#else
            if (tmpbb->bbJumpKind == BBJ_CALLFINALLY)
            {
                bPrevPrev = tmpbb;
            }
#endif
        }

        flowList* pred = bPrev->bbPreds;

        if (pred != nullptr)
        {
            // bPrevPrev will be set to the lexically
            // earliest predecessor of bPrev.

            while (pred != nullptr)
            {
                if (bPrevPrev == nullptr)
                {
                    // Initially we select the first block in the bbPreds list
                    bPrevPrev = pred->getBlock();
                    continue;
                }

                // Walk the flow graph lexically forward from pred->getBlock()
                // if we find (block == bPrevPrev) then
                // pred->getBlock() is an earlier predecessor.
                for (tmpbb = pred->getBlock(); tmpbb != nullptr; tmpbb = tmpbb->bbNext)
                {
                    if (tmpbb == bPrevPrev)
                    {
                        /* We found an ealier predecessor */
                        bPrevPrev = pred->getBlock();
                        break;
                    }
                    else if (tmpbb == bPrev)
                    {
                        // We have reached bPrev so stop walking
                        // as this cannot be an earlier predecessor
                        break;
                    }
                }

                // Onto the next predecessor
                pred = pred->flNext;
            }
        }

        if (bPrevPrev != nullptr)
        {
            // Walk the flow graph forward from bPrevPrev
            // if we don't find (tmpbb == bPrev) then our candidate
            // bPrevPrev is lexically after bPrev and we do not
            // want to select it as our new block

            for (tmpbb = bPrevPrev; tmpbb != nullptr; tmpbb = tmpbb->bbNext)
            {
                if (tmpbb == bPrev)
                {
                    // Set up block back to the lexically
                    // earliest predecessor of pPrev

                    return bPrevPrev;
                }
            }
        }

        // No reason to backtrack
        //
        return (BasicBlock*)nullptr;
    };

    // We expand the number of rarely run blocks by observing
    // that a block that falls into or jumps to a rarely run block,
    // must itself be rarely run and when we have a conditional
    // jump in which both branches go to rarely run blocks then
    // the block must itself be rarely run

    BasicBlock* block;
    BasicBlock* bPrev;

    for (bPrev = fgFirstBB, block = bPrev->bbNext; block != nullptr; bPrev = block, block = block->bbNext)
    {
        if (bPrev->isRunRarely())
        {
            continue;
        }

        if (bPrev->hasProfileWeight())
        {
            continue;
        }

        const char* reason = nullptr;

        switch (bPrev->bbJumpKind)
        {
            case BBJ_ALWAYS:

                if (bPrev->bbJumpDest->isRunRarely())
                {
                    reason = "Unconditional jump to a rarely run block";
                }
                break;

            case BBJ_CALLFINALLY:

                if (bPrev->isBBCallAlwaysPair() && block->isRunRarely())
                {
                    reason = "Call of finally followed by a rarely run block";
                }
                break;

            case BBJ_NONE:

                if (block->isRunRarely())
                {
                    reason = "Falling into a rarely run block";
                }
                break;

            case BBJ_COND:

                if (block->isRunRarely() && bPrev->bbJumpDest->isRunRarely())
                {
                    reason = "Both sides of a conditional jump are rarely run";
                }
                break;

            default:
                break;
        }

        if (reason != nullptr)
        {
            JITDUMP("%s, marking " FMT_BB " as rarely run\n", reason, bPrev->bbNum);

            // Must not have previously been marked
            noway_assert(!bPrev->isRunRarely());

            // Mark bPrev as a new rarely run block
            bPrev->bbSetRunRarely();

            // We have marked at least one block.
            //
            result = true;

            // See if we should to backtrack.
            //
            BasicBlock* bContinue = newRunRarely(block, bPrev);

            // If so, reset block to the backtrack point.
            //
            if (bContinue != nullptr)
            {
                block = bContinue;
            }
        }
    }

    // Now iterate over every block to see if we can prove that a block is rarely run
    // (i.e. when all predecessors to the block are rarely run)
    //
    for (bPrev = fgFirstBB, block = bPrev->bbNext; block != nullptr; bPrev = block, block = block->bbNext)
    {
        // If block is not run rarely, then check to make sure that it has
        // at least one non-rarely run block.

        if (!block->isRunRarely())
        {
            bool rare = true;

            /* Make sure that block has at least one normal predecessor */
            for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
            {
                /* Find the fall through predecessor, if any */
                if (!pred->getBlock()->isRunRarely())
                {
                    rare = false;
                    break;
                }
            }

            if (rare)
            {
                // If 'block' is the start of a handler or filter then we cannot make it
                // rarely run because we may have an exceptional edge that
                // branches here.
                //
                if (bbIsHandlerBeg(block))
                {
                    rare = false;
                }
            }

            if (rare)
            {
                block->bbSetRunRarely();
                result = true;

#ifdef DEBUG
                if (verbose)
                {
                    printf("All branches to " FMT_BB " are from rarely run blocks, marking as rarely run\n",
                           block->bbNum);
                }
#endif // DEBUG

                // When marking a BBJ_CALLFINALLY as rarely run we also mark
                // the BBJ_ALWAYS that comes after it as rarely run
                //
                if (block->isBBCallAlwaysPair())
                {
                    BasicBlock* bNext = block->bbNext;
                    PREFIX_ASSUME(bNext != nullptr);
                    bNext->bbSetRunRarely();
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Also marking the BBJ_ALWAYS at " FMT_BB " as rarely run\n", bNext->bbNum);
                    }
#endif // DEBUG
                }
            }
        }

        /* COMPACT blocks if possible */
        if (bPrev->bbJumpKind == BBJ_NONE)
        {
            if (fgCanCompactBlocks(bPrev, block))
            {
                fgCompactBlocks(bPrev, block);

                block = bPrev;
                continue;
            }
        }
        //
        // if bPrev->bbWeight is not based upon profile data we can adjust
        // the weights of bPrev and block
        //
        else if (bPrev->isBBCallAlwaysPair() &&          // we must have a BBJ_CALLFINALLY and BBK_ALWAYS pair
                 (bPrev->bbWeight != block->bbWeight) && // the weights are currently different
                 !bPrev->hasProfileWeight())             // and the BBJ_CALLFINALLY block is not using profiled
                                                         // weights
        {
            if (block->isRunRarely())
            {
                bPrev->bbWeight =
                    block->bbWeight; // the BBJ_CALLFINALLY block now has the same weight as the BBJ_ALWAYS block
                bPrev->bbFlags |= BBF_RUN_RARELY; // and is now rarely run
#ifdef DEBUG
                if (verbose)
                {
                    printf("Marking the BBJ_CALLFINALLY block at " FMT_BB " as rarely run because " FMT_BB
                           " is rarely run\n",
                           bPrev->bbNum, block->bbNum);
                }
#endif // DEBUG
            }
            else if (bPrev->isRunRarely())
            {
                block->bbWeight =
                    bPrev->bbWeight; // the BBJ_ALWAYS block now has the same weight as the BBJ_CALLFINALLY block
                block->bbFlags |= BBF_RUN_RARELY; // and is now rarely run
#ifdef DEBUG
                if (verbose)
                {
                    printf("Marking the BBJ_ALWAYS block at " FMT_BB " as rarely run because " FMT_BB
                           " is rarely run\n",
                           block->bbNum, bPrev->bbNum);
                }
#endif // DEBUG
            }
            else // Both blocks are hot, bPrev is known not to be using profiled weight
            {
                bPrev->bbWeight =
                    block->bbWeight; // the BBJ_CALLFINALLY block now has the same weight as the BBJ_ALWAYS block
            }
            noway_assert(block->bbWeight == bPrev->bbWeight);
        }
    }

    return result;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

//-----------------------------------------------------------------------------
// fgReorderBlocks: reorder blocks to favor frequent fall through paths,
//     move rare blocks to the end of the method/eh region, and move
//     funclets to the ends of methods.
//
// Returns:
//    True if anything got reordered. Reordering blocks may require changing
//    IR to reverse branch conditions.
//
bool Compiler::fgReorderBlocks()
{
    noway_assert(opts.compDbgCode == false);

#if defined(FEATURE_EH_FUNCLETS)
    assert(fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // We can't relocate anything if we only have one block
    if (fgFirstBB->bbNext == nullptr)
    {
        return false;
    }

    bool newRarelyRun      = false;
    bool movedBlocks       = false;
    bool optimizedSwitches = false;
    bool optimizedBranches = false;

    // First let us expand the set of run rarely blocks
    newRarelyRun |= fgExpandRarelyRunBlocks();

#if !defined(FEATURE_EH_FUNCLETS)
    movedBlocks |= fgRelocateEHRegions();
#endif // !FEATURE_EH_FUNCLETS

    //
    // If we are using profile weights we can change some
    // switch jumps into conditional test and jump
    //
    if (fgIsUsingProfileWeights())
    {
        //
        // Note that this is currently not yet implemented
        //
        optimizedSwitches = fgOptimizeSwitchJumps();
        if (optimizedSwitches)
        {
            fgUpdateFlowGraph();
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgReorderBlocks()\n");

        printf("\nInitial BasicBlocks");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    BasicBlock* bNext;
    BasicBlock* bPrev;
    BasicBlock* block;
    unsigned    XTnum;
    EHblkDsc*   HBtab;

    // Iterate over every block, remembering our previous block in bPrev
    for (bPrev = fgFirstBB, block = bPrev->bbNext; block != nullptr; bPrev = block, block = block->bbNext)
    {
        //
        // Consider relocating the rarely run blocks such that they are at the end of the method.
        // We also consider reversing conditional branches so that they become a not taken forwards branch.
        //

        // If block is marked with a BBF_KEEP_BBJ_ALWAYS flag then we don't move the block
        if ((block->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0)
        {
            continue;
        }

        // Finally and handlers blocks are to be kept contiguous.
        // TODO-CQ: Allow reordering within the handler region
        if (block->hasHndIndex() == true)
        {
            continue;
        }

        bool        reorderBlock   = true; // This is set to false if we decide not to reorder 'block'
        bool        isRare         = block->isRunRarely();
        BasicBlock* bDest          = nullptr;
        bool        forwardBranch  = false;
        bool        backwardBranch = false;

        // Setup bDest
        if ((bPrev->bbJumpKind == BBJ_COND) || (bPrev->bbJumpKind == BBJ_ALWAYS))
        {
            bDest          = bPrev->bbJumpDest;
            forwardBranch  = fgIsForwardBranch(bPrev);
            backwardBranch = !forwardBranch;
        }

        // We will look for bPrev as a non rarely run block followed by block as a rarely run block
        //
        if (bPrev->isRunRarely())
        {
            reorderBlock = false;
        }

        // If the weights of the bPrev, block and bDest were all obtained from a profile run
        // then we can use them to decide if it is useful to reverse this conditional branch

        BasicBlock::weight_t profHotWeight = -1;

        if (bPrev->hasProfileWeight() && block->hasProfileWeight() && ((bDest == nullptr) || bDest->hasProfileWeight()))
        {
            //
            // All blocks have profile information
            //
            if (forwardBranch)
            {
                if (bPrev->bbJumpKind == BBJ_ALWAYS)
                {
                    // We can pull up the blocks that the unconditional jump branches to
                    // if the weight of bDest is greater or equal to the weight of block
                    // also the weight of bDest can't be zero.
                    //
                    if ((bDest->bbWeight < block->bbWeight) || (bDest->bbWeight == BB_ZERO_WEIGHT))
                    {
                        reorderBlock = false;
                    }
                    else
                    {
                        //
                        // If this remains true then we will try to pull up bDest to succeed bPrev
                        //
                        bool moveDestUp = true;

                        if (fgHaveValidEdgeWeights)
                        {
                            //
                            // The edge bPrev -> bDest must have a higher minimum weight
                            // than every other edge into bDest
                            //
                            flowList* edgeFromPrev = fgGetPredForBlock(bDest, bPrev);
                            noway_assert(edgeFromPrev != nullptr);

                            // Examine all of the other edges into bDest
                            for (flowList* edge = bDest->bbPreds; edge != nullptr; edge = edge->flNext)
                            {
                                if (edge != edgeFromPrev)
                                {
                                    if (edge->edgeWeightMax() >= edgeFromPrev->edgeWeightMin())
                                    {
                                        moveDestUp = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //
                            // The block bPrev must have a higher weight
                            // than every other block that goes into bDest
                            //

                            // Examine all of the other edges into bDest
                            for (flowList* edge = bDest->bbPreds; edge != nullptr; edge = edge->flNext)
                            {
                                BasicBlock* bTemp = edge->getBlock();

                                if ((bTemp != bPrev) && (bTemp->bbWeight >= bPrev->bbWeight))
                                {
                                    moveDestUp = false;
                                    break;
                                }
                            }
                        }

                        // Are we still good to move bDest up to bPrev?
                        if (moveDestUp)
                        {
                            //
                            // We will consider all blocks that have less weight than profHotWeight to be
                            // uncommonly run blocks as compared with the hot path of bPrev taken-jump to bDest
                            //
                            profHotWeight = bDest->bbWeight - 1;
                        }
                        else
                        {
                            if (block->isRunRarely())
                            {
                                // We will move any rarely run blocks blocks
                                profHotWeight = 0;
                            }
                            else
                            {
                                // We will move all blocks that have a weight less or equal to our fall through block
                                profHotWeight = block->bbWeight + 1;
                            }
                            // But we won't try to connect with bDest
                            bDest = nullptr;
                        }
                    }
                }
                else // (bPrev->bbJumpKind == BBJ_COND)
                {
                    noway_assert(bPrev->bbJumpKind == BBJ_COND);
                    //
                    // We will reverse branch if the taken-jump to bDest ratio (i.e. 'takenRatio')
                    // is more than 51%
                    //
                    // We will setup profHotWeight to be maximum bbWeight that a block
                    // could have for us not to want to reverse the conditional branch
                    //
                    // We will consider all blocks that have less weight than profHotWeight to be
                    // uncommonly run blocks as compared with the hot path of bPrev taken-jump to bDest
                    //
                    if (fgHaveValidEdgeWeights)
                    {
                        // We have valid edge weights, however even with valid edge weights
                        // we may have a minimum and maximum range for each edges value
                        //
                        // We will check that the min weight of the bPrev to bDest edge
                        //  is more than twice the max weight of the bPrev to block edge.
                        //
                        //                  bPrev -->   [BB04, weight 31]
                        //                                     |         \.
                        //          edgeToBlock -------------> O          \.
                        //          [min=8,max=10]             V           \.
                        //                  block -->   [BB05, weight 10]   \.
                        //                                                   \.
                        //          edgeToDest ----------------------------> O
                        //          [min=21,max=23]                          |
                        //                                                   V
                        //                  bDest --------------->   [BB08, weight 21]
                        //
                        flowList* edgeToDest  = fgGetPredForBlock(bDest, bPrev);
                        flowList* edgeToBlock = fgGetPredForBlock(block, bPrev);
                        noway_assert(edgeToDest != nullptr);
                        noway_assert(edgeToBlock != nullptr);
                        //
                        // Calculate the taken ratio
                        //   A takenRation of 0.10 means taken 10% of the time, not taken 90% of the time
                        //   A takenRation of 0.50 means taken 50% of the time, not taken 50% of the time
                        //   A takenRation of 0.90 means taken 90% of the time, not taken 10% of the time
                        //
                        double takenCount =
                            ((double)edgeToDest->edgeWeightMin() + (double)edgeToDest->edgeWeightMax()) / 2.0;
                        double notTakenCount =
                            ((double)edgeToBlock->edgeWeightMin() + (double)edgeToBlock->edgeWeightMax()) / 2.0;
                        double totalCount = takenCount + notTakenCount;
                        double takenRatio = takenCount / totalCount;

                        // If the takenRatio is greater or equal to 51% then we will reverse the branch
                        if (takenRatio < 0.51)
                        {
                            reorderBlock = false;
                        }
                        else
                        {
                            // set profHotWeight
                            profHotWeight = (edgeToBlock->edgeWeightMin() + edgeToBlock->edgeWeightMax()) / 2 - 1;
                        }
                    }
                    else
                    {
                        // We don't have valid edge weight so we will be more conservative
                        // We could have bPrev, block or bDest as part of a loop and thus have extra weight
                        //
                        // We will do two checks:
                        //   1. Check that the weight of bDest is at least two times more than block
                        //   2. Check that the weight of bPrev is at least three times more than block
                        //
                        //                  bPrev -->   [BB04, weight 31]
                        //                                     |         \.
                        //                                     V          \.
                        //                  block -->   [BB05, weight 10]  \.
                        //                                                  \.
                        //                                                  |
                        //                                                  V
                        //                  bDest --------------->   [BB08, weight 21]
                        //
                        //  For this case weightDest is calculated as (21+1)/2  or 11
                        //            and weightPrev is calculated as (31+2)/3  also 11
                        //
                        //  Generally both weightDest and weightPrev should calculate
                        //  the same value unless bPrev or bDest are part of a loop
                        //
                        BasicBlock::weight_t weightDest =
                            bDest->isMaxBBWeight() ? bDest->bbWeight : (bDest->bbWeight + 1) / 2;
                        BasicBlock::weight_t weightPrev =
                            bPrev->isMaxBBWeight() ? bPrev->bbWeight : (bPrev->bbWeight + 2) / 3;

                        // select the lower of weightDest and weightPrev
                        profHotWeight = (weightDest < weightPrev) ? weightDest : weightPrev;

                        // if the weight of block is greater (or equal) to profHotWeight then we don't reverse the cond
                        if (block->bbWeight >= profHotWeight)
                        {
                            reorderBlock = false;
                        }
                    }
                }
            }
            else // not a forwardBranch
            {
                if (bPrev->bbFallsThrough())
                {
                    goto CHECK_FOR_RARE;
                }

                // Here we should pull up the highest weight block remaining
                // and place it here since bPrev does not fall through.

                BasicBlock::weight_t highestWeight           = 0;
                BasicBlock*          candidateBlock          = nullptr;
                BasicBlock*          lastNonFallThroughBlock = bPrev;
                BasicBlock*          bTmp                    = bPrev->bbNext;

                while (bTmp != nullptr)
                {
                    // Don't try to split a Call/Always pair
                    //
                    if (bTmp->isBBCallAlwaysPair())
                    {
                        // Move bTmp forward
                        bTmp = bTmp->bbNext;
                    }

                    //
                    // Check for loop exit condition
                    //
                    if (bTmp == nullptr)
                    {
                        break;
                    }

                    //
                    // if its weight is the highest one we've seen and
                    //  the EH regions allow for us to place bTmp after bPrev
                    //
                    if ((bTmp->bbWeight > highestWeight) && fgEhAllowsMoveBlock(bPrev, bTmp))
                    {
                        // When we have a current candidateBlock that is a conditional (or unconditional) jump
                        // to bTmp (which is a higher weighted block) then it is better to keep out current
                        // candidateBlock and have it fall into bTmp
                        //
                        if ((candidateBlock == nullptr) ||
                            ((candidateBlock->bbJumpKind != BBJ_COND) && (candidateBlock->bbJumpKind != BBJ_ALWAYS)) ||
                            (candidateBlock->bbJumpDest != bTmp))
                        {
                            // otherwise we have a new candidateBlock
                            //
                            highestWeight  = bTmp->bbWeight;
                            candidateBlock = lastNonFallThroughBlock->bbNext;
                        }
                    }

                    if ((bTmp->bbFallsThrough() == false) || (bTmp->bbWeight == BB_ZERO_WEIGHT))
                    {
                        lastNonFallThroughBlock = bTmp;
                    }

                    bTmp = bTmp->bbNext;
                }

                // If we didn't find a suitable block then skip this
                if (highestWeight == 0)
                {
                    reorderBlock = false;
                }
                else
                {
                    noway_assert(candidateBlock != nullptr);

                    // If the candidateBlock is the same a block then skip this
                    if (candidateBlock == block)
                    {
                        reorderBlock = false;
                    }
                    else
                    {
                        // Set bDest to the block that we want to come after bPrev
                        bDest = candidateBlock;

                        // set profHotWeight
                        profHotWeight = highestWeight - 1;
                    }
                }
            }
        }
        else // we don't have good profile info (or we are falling through)
        {

        CHECK_FOR_RARE:;

            /* We only want to reorder when we have a rarely run   */
            /* block right after a normal block,                   */
            /* (bPrev is known to be a normal block at this point) */
            if (!isRare)
            {
                if ((bDest == block->bbNext) && (block->bbJumpKind == BBJ_RETURN) && (bPrev->bbJumpKind == BBJ_ALWAYS))
                {
                    // This is a common case with expressions like "return Expr1 && Expr2" -- move the return
                    // to establish fall-through.
                }
                else
                {
                    reorderBlock = false;
                }
            }
            else
            {
                /* If the jump target bDest is also a rarely run block then we don't want to do the reversal */
                if (bDest && bDest->isRunRarely())
                {
                    reorderBlock = false; /* Both block and bDest are rarely run */
                }
                else
                {
                    // We will move any rarely run blocks blocks
                    profHotWeight = 0;
                }
            }
        }

        if (reorderBlock == false)
        {
            //
            // Check for an unconditional branch to a conditional branch
            // which also branches back to our next block
            //
            const bool optimizedBranch = fgOptimizeBranch(bPrev);
            if (optimizedBranch)
            {
                noway_assert(bPrev->bbJumpKind == BBJ_COND);
                optimizedBranches = true;
            }
            continue;
        }

        //  Now we need to determine which blocks should be moved
        //
        //  We consider one of two choices:
        //
        //  1. Moving the fall-through blocks (or rarely run blocks) down to
        //     later in the method and hopefully connecting the jump dest block
        //     so that it becomes the fall through block
        //
        //  And when bDest in not NULL, we also consider:
        //
        //  2. Moving the bDest block (or blocks) up to bPrev
        //     so that it could be used as a fall through block
        //
        //  We will prefer option #1 if we are able to connect the jump dest
        //  block as the fall though block otherwise will we try to use option #2
        //

        //
        //  Consider option #1: relocating blocks starting at 'block'
        //    to later in flowgraph
        //
        // We set bStart to the first block that will be relocated
        // and bEnd to the last block that will be relocated

        BasicBlock* bStart   = block;
        BasicBlock* bEnd     = bStart;
        bNext                = bEnd->bbNext;
        bool connected_bDest = false;

        if ((backwardBranch && !isRare) ||
            ((block->bbFlags & BBF_DONT_REMOVE) != 0)) // Don't choose option #1 when block is the start of a try region
        {
            bStart = nullptr;
            bEnd   = nullptr;
        }
        else
        {
            while (true)
            {
                // Don't try to split a Call/Always pair
                //
                if (bEnd->isBBCallAlwaysPair())
                {
                    // Move bEnd and bNext forward
                    bEnd  = bNext;
                    bNext = bNext->bbNext;
                }

                //
                // Check for loop exit condition
                //
                if (bNext == nullptr)
                {
                    break;
                }

#if defined(FEATURE_EH_FUNCLETS)
                // Check if we've reached the funclets region, at the end of the function
                if (fgFirstFuncletBB == bEnd->bbNext)
                {
                    break;
                }
#endif // FEATURE_EH_FUNCLETS

                if (bNext == bDest)
                {
                    connected_bDest = true;
                    break;
                }

                // All the blocks must have the same try index
                // and must not have the BBF_DONT_REMOVE flag set

                if (!BasicBlock::sameTryRegion(bStart, bNext) || ((bNext->bbFlags & BBF_DONT_REMOVE) != 0))
                {
                    // exit the loop, bEnd is now set to the
                    // last block that we want to relocate
                    break;
                }

                // If we are relocating rarely run blocks..
                if (isRare)
                {
                    // ... then all blocks must be rarely run
                    if (!bNext->isRunRarely())
                    {
                        // exit the loop, bEnd is now set to the
                        // last block that we want to relocate
                        break;
                    }
                }
                else
                {
                    // If we are moving blocks that are hot then all
                    // of the blocks moved must be less than profHotWeight */
                    if (bNext->bbWeight >= profHotWeight)
                    {
                        // exit the loop, bEnd is now set to the
                        // last block that we would relocate
                        break;
                    }
                }

                // Move bEnd and bNext forward
                bEnd  = bNext;
                bNext = bNext->bbNext;
            }

            // Set connected_bDest to true if moving blocks [bStart .. bEnd]
            //  connects with the the jump dest of bPrev (i.e bDest) and
            // thus allows bPrev fall through instead of jump.
            if (bNext == bDest)
            {
                connected_bDest = true;
            }
        }

        //  Now consider option #2: Moving the jump dest block (or blocks)
        //    up to bPrev
        //
        // The variables bStart2, bEnd2 and bPrev2 are used for option #2
        //
        // We will setup bStart2 to the first block that will be relocated
        // and bEnd2 to the last block that will be relocated
        // and bPrev2 to be the lexical pred of bDest
        //
        // If after this calculation bStart2 is NULL we cannot use option #2,
        // otherwise bStart2, bEnd2 and bPrev2 are all non-NULL and we will use option #2

        BasicBlock* bStart2 = nullptr;
        BasicBlock* bEnd2   = nullptr;
        BasicBlock* bPrev2  = nullptr;

        // If option #1 didn't connect bDest and bDest isn't NULL
        if ((connected_bDest == false) && (bDest != nullptr) &&
            //  The jump target cannot be moved if it has the BBF_DONT_REMOVE flag set
            ((bDest->bbFlags & BBF_DONT_REMOVE) == 0))
        {
            // We will consider option #2: relocating blocks starting at 'bDest' to succeed bPrev
            //
            // setup bPrev2 to be the lexical pred of bDest

            bPrev2 = block;
            while (bPrev2 != nullptr)
            {
                if (bPrev2->bbNext == bDest)
                {
                    break;
                }

                bPrev2 = bPrev2->bbNext;
            }

            if ((bPrev2 != nullptr) && fgEhAllowsMoveBlock(bPrev, bDest))
            {
                // We have decided that relocating bDest to be after bPrev is best
                // Set bStart2 to the first block that will be relocated
                // and bEnd2 to the last block that will be relocated
                //
                // Assigning to bStart2 selects option #2
                //
                bStart2 = bDest;
                bEnd2   = bStart2;
                bNext   = bEnd2->bbNext;

                while (true)
                {
                    // Don't try to split a Call/Always pair
                    //
                    if (bEnd2->isBBCallAlwaysPair())
                    {
                        noway_assert(bNext->bbJumpKind == BBJ_ALWAYS);
                        // Move bEnd2 and bNext forward
                        bEnd2 = bNext;
                        bNext = bNext->bbNext;
                    }

                    // Check for the Loop exit conditions

                    if (bNext == nullptr)
                    {
                        break;
                    }

                    if (bEnd2->bbFallsThrough() == false)
                    {
                        break;
                    }

                    // If we are relocating rarely run blocks..
                    // All the blocks must have the same try index,
                    // and must not have the BBF_DONT_REMOVE flag set

                    if (!BasicBlock::sameTryRegion(bStart2, bNext) || ((bNext->bbFlags & BBF_DONT_REMOVE) != 0))
                    {
                        // exit the loop, bEnd2 is now set to the
                        // last block that we want to relocate
                        break;
                    }

                    if (isRare)
                    {
                        /* ... then all blocks must not be rarely run */
                        if (bNext->isRunRarely())
                        {
                            // exit the loop, bEnd2 is now set to the
                            // last block that we want to relocate
                            break;
                        }
                    }
                    else
                    {
                        // If we are relocating hot blocks
                        // all blocks moved must be greater than profHotWeight
                        if (bNext->bbWeight <= profHotWeight)
                        {
                            // exit the loop, bEnd2 is now set to the
                            // last block that we want to relocate
                            break;
                        }
                    }

                    // Move bEnd2 and bNext forward
                    bEnd2 = bNext;
                    bNext = bNext->bbNext;
                }
            }
        }

        // If we are using option #1 then ...
        if (bStart2 == nullptr)
        {
            // Don't use option #1 for a backwards branch
            if (bStart == nullptr)
            {
                continue;
            }

            // .... Don't move a set of blocks that are already at the end of the main method
            if (bEnd == fgLastBBInMainFunction())
            {
                continue;
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            if (bDest != nullptr)
            {
                if (bPrev->bbJumpKind == BBJ_COND)
                {
                    printf("Decided to reverse conditional branch at block " FMT_BB " branch to " FMT_BB " ",
                           bPrev->bbNum, bDest->bbNum);
                }
                else if (bPrev->bbJumpKind == BBJ_ALWAYS)
                {
                    printf("Decided to straighten unconditional branch at block " FMT_BB " branch to " FMT_BB " ",
                           bPrev->bbNum, bDest->bbNum);
                }
                else
                {
                    printf("Decided to place hot code after " FMT_BB ", placed " FMT_BB " after this block ",
                           bPrev->bbNum, bDest->bbNum);
                }

                if (profHotWeight > 0)
                {
                    printf("because of IBC profile data\n");
                }
                else
                {
                    if (bPrev->bbFallsThrough())
                    {
                        printf("since it falls into a rarely run block\n");
                    }
                    else
                    {
                        printf("since it is succeeded by a rarely run block\n");
                    }
                }
            }
            else
            {
                printf("Decided to relocate block(s) after block " FMT_BB " since they are %s block(s)\n", bPrev->bbNum,
                       block->isRunRarely() ? "rarely run" : "uncommonly run");
            }
        }
#endif // DEBUG

        // We will set insertAfterBlk to the block the precedes our insertion range
        // We will set bStartPrev to be the block that precedes the set of blocks that we are moving
        BasicBlock* insertAfterBlk;
        BasicBlock* bStartPrev;

        if (bStart2 != nullptr)
        {
            // Option #2: relocating blocks starting at 'bDest' to follow bPrev

            // Update bStart and bEnd so that we can use these two for all later operations
            bStart = bStart2;
            bEnd   = bEnd2;

            // Set bStartPrev to be the block that comes before bStart
            bStartPrev = bPrev2;

            // We will move [bStart..bEnd] to immediately after bPrev
            insertAfterBlk = bPrev;
        }
        else
        {
            // option #1: Moving the fall-through blocks (or rarely run blocks) down to later in the method

            // Set bStartPrev to be the block that come before bStart
            bStartPrev = bPrev;

            // We will move [bStart..bEnd] but we will pick the insert location later
            insertAfterBlk = nullptr;
        }

        // We are going to move [bStart..bEnd] so they can't be NULL
        noway_assert(bStart != nullptr);
        noway_assert(bEnd != nullptr);

        // bEnd can't be a BBJ_CALLFINALLY unless it is a RETLESS call
        noway_assert((bEnd->bbJumpKind != BBJ_CALLFINALLY) || (bEnd->bbFlags & BBF_RETLESS_CALL));

        // bStartPrev must be set to the block that precedes bStart
        noway_assert(bStartPrev->bbNext == bStart);

        // Since we will be unlinking [bStart..bEnd],
        // we need to compute and remember if bStart is in each of
        // the try and handler regions
        //
        bool* fStartIsInTry = nullptr;
        bool* fStartIsInHnd = nullptr;

        if (compHndBBtabCount > 0)
        {
            fStartIsInTry = new (this, CMK_Unknown) bool[compHndBBtabCount];
            fStartIsInHnd = new (this, CMK_Unknown) bool[compHndBBtabCount];

            for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
            {
                fStartIsInTry[XTnum] = HBtab->InTryRegionBBRange(bStart);
                fStartIsInHnd[XTnum] = HBtab->InHndRegionBBRange(bStart);
            }
        }

        /* Temporarily unlink [bStart..bEnd] from the flow graph */
        fgUnlinkRange(bStart, bEnd);

        if (insertAfterBlk == nullptr)
        {
            // Find new location for the unlinked block(s)
            // Set insertAfterBlk to the block which will precede the insertion point

            if (!bStart->hasTryIndex() && isRare)
            {
                // We'll just insert the blocks at the end of the method. If the method
                // has funclets, we will insert at the end of the main method but before
                // any of the funclets. Note that we create funclets before we call
                // fgReorderBlocks().

                insertAfterBlk = fgLastBBInMainFunction();
                noway_assert(insertAfterBlk != bPrev);
            }
            else
            {
                BasicBlock* startBlk;
                BasicBlock* lastBlk;
                EHblkDsc*   ehDsc = ehInitTryBlockRange(bStart, &startBlk, &lastBlk);

                BasicBlock* endBlk;

                /* Setup startBlk and endBlk as the range to search */

                if (ehDsc != nullptr)
                {
                    endBlk = lastBlk->bbNext;

                    /*
                       Multiple (nested) try regions might start from the same BB.
                       For example,

                       try3   try2   try1
                       |---   |---   |---   BB01
                       |      |      |      BB02
                       |      |      |---   BB03
                       |      |             BB04
                       |      |------------ BB05
                       |                    BB06
                       |------------------- BB07

                       Now if we want to insert in try2 region, we will start with startBlk=BB01.
                       The following loop will allow us to start from startBlk==BB04.
                    */
                    while (!BasicBlock::sameTryRegion(startBlk, bStart) && (startBlk != endBlk))
                    {
                        startBlk = startBlk->bbNext;
                    }

                    // startBlk cannot equal endBlk as it must come before endBlk
                    if (startBlk == endBlk)
                    {
                        goto CANNOT_MOVE;
                    }

                    // we also can't start searching the try region at bStart
                    if (startBlk == bStart)
                    {
                        // if bEnd is the last block in the method or
                        // or if bEnd->bbNext is in a different try region
                        // then we cannot move the blocks
                        //
                        if ((bEnd->bbNext == nullptr) || !BasicBlock::sameTryRegion(startBlk, bEnd->bbNext))
                        {
                            goto CANNOT_MOVE;
                        }

                        startBlk = bEnd->bbNext;

                        // Check that the new startBlk still comes before endBlk

                        // startBlk cannot equal endBlk as it must come before endBlk
                        if (startBlk == endBlk)
                        {
                            goto CANNOT_MOVE;
                        }

                        BasicBlock* tmpBlk = startBlk;
                        while ((tmpBlk != endBlk) && (tmpBlk != nullptr))
                        {
                            tmpBlk = tmpBlk->bbNext;
                        }

                        // when tmpBlk is NULL that means startBlk is after endBlk
                        // so there is no way to move bStart..bEnd within the try region
                        if (tmpBlk == nullptr)
                        {
                            goto CANNOT_MOVE;
                        }
                    }
                }
                else
                {
                    noway_assert(isRare == false);

                    /* We'll search through the entire main method */
                    startBlk = fgFirstBB;
                    endBlk   = fgEndBBAfterMainFunction();
                }

                // Calculate nearBlk and jumpBlk and then call fgFindInsertPoint()
                // to find our insertion block
                //
                {
                    // If the set of blocks that we are moving ends with a BBJ_ALWAYS to
                    // another [rarely run] block that comes after bPrev (forward branch)
                    // then we can set up nearBlk to eliminate this jump sometimes
                    //
                    BasicBlock* nearBlk = nullptr;
                    BasicBlock* jumpBlk = nullptr;

                    if ((bEnd->bbJumpKind == BBJ_ALWAYS) && (!isRare || bEnd->bbJumpDest->isRunRarely()) &&
                        fgIsForwardBranch(bEnd, bPrev))
                    {
                        // Set nearBlk to be the block in [startBlk..endBlk]
                        // such that nearBlk->bbNext == bEnd->JumpDest
                        // if no such block exists then set nearBlk to NULL
                        nearBlk = startBlk;
                        jumpBlk = bEnd;
                        do
                        {
                            // We do not want to set nearBlk to bPrev
                            // since then we will not move [bStart..bEnd]
                            //
                            if (nearBlk != bPrev)
                            {
                                // Check if nearBlk satisfies our requirement
                                if (nearBlk->bbNext == bEnd->bbJumpDest)
                                {
                                    break;
                                }
                            }

                            // Did we reach the endBlk?
                            if (nearBlk == endBlk)
                            {
                                nearBlk = nullptr;
                                break;
                            }

                            // advance nearBlk to the next block
                            nearBlk = nearBlk->bbNext;

                        } while (nearBlk != nullptr);
                    }

                    // if nearBlk is NULL then we set nearBlk to be the
                    // first block that we want to insert after.
                    if (nearBlk == nullptr)
                    {
                        if (bDest != nullptr)
                        {
                            // we want to insert after bDest
                            nearBlk = bDest;
                        }
                        else
                        {
                            // we want to insert after bPrev
                            nearBlk = bPrev;
                        }
                    }

                    /* Set insertAfterBlk to the block which we will insert after. */

                    insertAfterBlk =
                        fgFindInsertPoint(bStart->bbTryIndex,
                                          true, // Insert in the try region.
                                          startBlk, endBlk, nearBlk, jumpBlk, bStart->bbWeight == BB_ZERO_WEIGHT);
                }

                /* See if insertAfterBlk is the same as where we started, */
                /*  or if we could not find any insertion point     */

                if ((insertAfterBlk == bPrev) || (insertAfterBlk == nullptr))
                {
                CANNOT_MOVE:;
                    /* We couldn't move the blocks, so put everything back */
                    /* relink [bStart .. bEnd] into the flow graph */

                    bPrev->setNext(bStart);
                    if (bEnd->bbNext)
                    {
                        bEnd->bbNext->bbPrev = bEnd;
                    }
#ifdef DEBUG
                    if (verbose)
                    {
                        if (bStart != bEnd)
                        {
                            printf("Could not relocate blocks (" FMT_BB " .. " FMT_BB ")\n", bStart->bbNum,
                                   bEnd->bbNum);
                        }
                        else
                        {
                            printf("Could not relocate block " FMT_BB "\n", bStart->bbNum);
                        }
                    }
#endif // DEBUG
                    continue;
                }
            }
        }

        noway_assert(insertAfterBlk != nullptr);
        noway_assert(bStartPrev != nullptr);
        noway_assert(bStartPrev != insertAfterBlk);

#ifdef DEBUG
        movedBlocks = true;

        if (verbose)
        {
            const char* msg;
            if (bStart2 != nullptr)
            {
                msg = "hot";
            }
            else
            {
                if (isRare)
                {
                    msg = "rarely run";
                }
                else
                {
                    msg = "uncommon";
                }
            }

            printf("Relocated %s ", msg);
            if (bStart != bEnd)
            {
                printf("blocks (" FMT_BB " .. " FMT_BB ")", bStart->bbNum, bEnd->bbNum);
            }
            else
            {
                printf("block " FMT_BB, bStart->bbNum);
            }

            if (bPrev->bbJumpKind == BBJ_COND)
            {
                printf(" by reversing conditional jump at " FMT_BB "\n", bPrev->bbNum);
            }
            else
            {
                printf("\n", bPrev->bbNum);
            }
        }
#endif // DEBUG

        if (bPrev->bbJumpKind == BBJ_COND)
        {
            /* Reverse the bPrev jump condition */
            Statement* condTestStmt = bPrev->lastStmt();

            GenTree* condTest = condTestStmt->GetRootNode();
            noway_assert(condTest->gtOper == GT_JTRUE);

            condTest->AsOp()->gtOp1 = gtReverseCond(condTest->AsOp()->gtOp1);

            if (bStart2 == nullptr)
            {
                /* Set the new jump dest for bPrev to the rarely run or uncommon block(s) */
                bPrev->bbJumpDest = bStart;
            }
            else
            {
                noway_assert(insertAfterBlk == bPrev);
                noway_assert(insertAfterBlk->bbNext == block);

                /* Set the new jump dest for bPrev to the rarely run or uncommon block(s) */
                bPrev->bbJumpDest = block;
            }
        }

        // If we are moving blocks that are at the end of a try or handler
        // we will need to shorten ebdTryLast or ebdHndLast
        //
        ehUpdateLastBlocks(bEnd, bStartPrev);

        // If we are moving blocks into the end of a try region or handler region
        // we will need to extend ebdTryLast or ebdHndLast so the blocks that we
        // are moving are part of this try or handler region.
        //
        for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
        {
            // Are we moving blocks to the end of a try region?
            if (HBtab->ebdTryLast == insertAfterBlk)
            {
                if (fStartIsInTry[XTnum])
                {
                    // bStart..bEnd is in the try, so extend the try region
                    fgSetTryEnd(HBtab, bEnd);
                }
            }

            // Are we moving blocks to the end of a handler region?
            if (HBtab->ebdHndLast == insertAfterBlk)
            {
                if (fStartIsInHnd[XTnum])
                {
                    // bStart..bEnd is in the handler, so extend the handler region
                    fgSetHndEnd(HBtab, bEnd);
                }
            }
        }

        /* We have decided to insert the block(s) after 'insertAfterBlk' */
        fgMoveBlocksAfter(bStart, bEnd, insertAfterBlk);

        if (bDest)
        {
            /* We may need to insert an unconditional branch after bPrev to bDest */
            fgConnectFallThrough(bPrev, bDest);
        }
        else
        {
            /* If bPrev falls through, we must insert a jump to block */
            fgConnectFallThrough(bPrev, block);
        }

        BasicBlock* bSkip = bEnd->bbNext;

        /* If bEnd falls through, we must insert a jump to bNext */
        fgConnectFallThrough(bEnd, bNext);

        if (bStart2 == nullptr)
        {
            /* If insertAfterBlk falls through, we are forced to     */
            /* add a jump around the block(s) we just inserted */
            fgConnectFallThrough(insertAfterBlk, bSkip);
        }
        else
        {
            /* We may need to insert an unconditional branch after bPrev2 to bStart */
            fgConnectFallThrough(bPrev2, bStart);
        }

#if DEBUG
        if (verbose)
        {
            printf("\nAfter this change in fgReorderBlocks the BB graph is:");
            fgDispBasicBlocks(verboseTrees);
            printf("\n");
        }
        fgVerifyHandlerTab();

        // Make sure that the predecessor lists are accurate
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
#endif // DEBUG

        // Set our iteration point 'block' to be the new bPrev->bbNext
        //  It will be used as the next bPrev
        block = bPrev->bbNext;

    } // end of for loop(bPrev,block)

    const bool changed = movedBlocks || newRarelyRun || optimizedSwitches || optimizedBranches;

    if (changed)
    {
        fgNeedsUpdateFlowGraph = true;
#if DEBUG
        // Make sure that the predecessor lists are accurate
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
#endif // DEBUG
    }

    return changed;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//-------------------------------------------------------------
// fgUpdateFlowGraph: Removes any empty blocks, unreachable blocks, and redundant jumps.
// Most of those appear after dead store removal and folding of conditionals.
// Also, compact consecutive basic blocks.
//
// Arguments:
//    doTailDuplication - true to attempt tail duplication optimization
//
// Returns: true if the flowgraph has been modified
//
// Notes:
//    Debuggable code and Min Optimization JIT also introduces basic blocks
//    but we do not optimize those!
//
bool Compiler::fgUpdateFlowGraph(bool doTailDuplication)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgUpdateFlowGraph()");
    }
#endif // DEBUG

    /* This should never be called for debuggable code */

    noway_assert(opts.OptimizationEnabled());

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBefore updating the flow graph:\n");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    /* Walk all the basic blocks - look for unconditional jumps, empty blocks, blocks to compact, etc...
     *
     * OBSERVATION:
     *      Once a block is removed the predecessors are not accurate (assuming they were at the beginning)
     *      For now we will only use the information in bbRefs because it is easier to be updated
     */

    bool modified = false;
    bool change;
    do
    {
        change = false;

        BasicBlock* block;           // the current block
        BasicBlock* bPrev = nullptr; // the previous non-worthless block
        BasicBlock* bNext;           // the successor of the current block
        BasicBlock* bDest;           // the jump target of the current block

        for (block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            /*  Some blocks may be already marked removed by other optimizations
             *  (e.g worthless loop removal), without being explicitly removed
             *  from the list.
             */

            if (block->bbFlags & BBF_REMOVED)
            {
                if (bPrev)
                {
                    bPrev->setNext(block->bbNext);
                }
                else
                {
                    /* WEIRD first basic block is removed - should have an assert here */
                    noway_assert(!"First basic block marked as BBF_REMOVED???");

                    fgFirstBB = block->bbNext;
                }
                continue;
            }

        /*  We jump to the REPEAT label if we performed a change involving the current block
         *  This is in case there are other optimizations that can show up
         *  (e.g. - compact 3 blocks in a row)
         *  If nothing happens, we then finish the iteration and move to the next block
         */

        REPEAT:;

            bNext = block->bbNext;
            bDest = nullptr;

            if (block->bbJumpKind == BBJ_ALWAYS)
            {
                bDest = block->bbJumpDest;
                if (doTailDuplication && fgOptimizeUncondBranchToSimpleCond(block, bDest))
                {
                    change   = true;
                    modified = true;
                    bDest    = block->bbJumpDest;
                    bNext    = block->bbNext;
                }
            }

            if (block->bbJumpKind == BBJ_NONE)
            {
                bDest = nullptr;
                if (doTailDuplication && fgOptimizeUncondBranchToSimpleCond(block, block->bbNext))
                {
                    change   = true;
                    modified = true;
                    bDest    = block->bbJumpDest;
                    bNext    = block->bbNext;
                }
            }

            // Remove JUMPS to the following block
            // and optimize any JUMPS to JUMPS

            if (block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_ALWAYS)
            {
                bDest = block->bbJumpDest;
                if (bDest == bNext)
                {
                    if (fgOptimizeBranchToNext(block, bNext, bPrev))
                    {
                        change   = true;
                        modified = true;
                        bDest    = nullptr;
                    }
                }
            }

            if (bDest != nullptr)
            {
                // Do we have a JUMP to an empty unconditional JUMP block?
                if (bDest->isEmpty() && (bDest->bbJumpKind == BBJ_ALWAYS) &&
                    (bDest != bDest->bbJumpDest)) // special case for self jumps
                {
                    if (fgOptimizeBranchToEmptyUnconditional(block, bDest))
                    {
                        change   = true;
                        modified = true;
                        goto REPEAT;
                    }
                }

                // Check for cases where reversing the branch condition may enable
                // other flow opts.
                //
                // Current block falls through to an empty bNext BBJ_ALWAYS, and
                // (a) block jump target is bNext's bbNext.
                // (b) block jump target is elsewhere but join free, and
                //      bNext's jump target has a join.
                //
                if ((block->bbJumpKind == BBJ_COND) &&   // block is a BBJ_COND block
                    (bNext != nullptr) &&                // block is not the last block
                    (bNext->bbRefs == 1) &&              // No other block jumps to bNext
                    (bNext->bbJumpKind == BBJ_ALWAYS) && // The next block is a BBJ_ALWAYS block
                    bNext->isEmpty() &&                  // and it is an an empty block
                    (bNext != bNext->bbJumpDest) &&      // special case for self jumps
                    (bDest != fgFirstColdBlock))
                {
                    // case (a)
                    //
                    const bool isJumpAroundEmpty = (bNext->bbNext == bDest);

                    // case (b)
                    //
                    // Note the asymetric checks for refs == 1 and refs > 1 ensures that we
                    // differentiate the roles played by bDest and bNextJumpDest. We need some
                    // sense of which arrangement is preferable to avoid getting stuck in a loop
                    // reversing and re-reversing.
                    //
                    // Other tiebreaking criteria could be considered.
                    //
                    // Pragmatic constraints:
                    //
                    // * don't consider lexical predecessors, or we may confuse loop recognition
                    // * don't consider blocks of different rarities
                    //
                    BasicBlock* const bNextJumpDest    = bNext->bbJumpDest;
                    const bool        isJumpToJoinFree = !isJumpAroundEmpty && (bDest->bbRefs == 1) &&
                                                  (bNextJumpDest->bbRefs > 1) && (bDest->bbNum > block->bbNum) &&
                                                  (block->isRunRarely() == bDest->isRunRarely());

                    bool optimizeJump = isJumpAroundEmpty || isJumpToJoinFree;

                    // We do not optimize jumps between two different try regions.
                    // However jumping to a block that is not in any try region is OK
                    //
                    if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
                    {
                        optimizeJump = false;
                    }

                    // Also consider bNext's try region
                    //
                    if (bNext->hasTryIndex() && !BasicBlock::sameTryRegion(block, bNext))
                    {
                        optimizeJump = false;
                    }

                    // If we are optimizing using real profile weights
                    // then don't optimize a conditional jump to an unconditional jump
                    // until after we have computed the edge weights
                    //
                    if (fgIsUsingProfileWeights())
                    {
                        // if block and bdest are in different hot/cold regions we can't do this this optimization
                        // because we can't allow fall-through into the cold region.
                        if (!fgEdgeWeightsComputed || fgInDifferentRegions(block, bDest))
                        {
                            fgNeedsUpdateFlowGraph = true;
                            optimizeJump           = false;
                        }
                    }

                    if (optimizeJump && isJumpToJoinFree)
                    {
                        // In the join free case, we also need to move bDest right after bNext
                        // to create same flow as in the isJumpAroundEmpty case.
                        //
                        if (!fgEhAllowsMoveBlock(bNext, bDest) || bDest->isBBCallAlwaysPair())
                        {
                            optimizeJump = false;
                        }
                        else
                        {
                            // We don't expect bDest to already be right after bNext.
                            //
                            assert(bDest != bNext->bbNext);

                            JITDUMP("\nMoving " FMT_BB " after " FMT_BB " to enable reversal\n", bDest->bbNum,
                                    bNext->bbNum);

                            // If bDest can fall through we'll need to create a jump
                            // block after it too. Remember where to jump to.
                            //
                            BasicBlock* const bDestNext = bDest->bbNext;

                            // Move bDest
                            //
                            if (ehIsBlockEHLast(bDest))
                            {
                                ehUpdateLastBlocks(bDest, bDest->bbPrev);
                            }

                            fgUnlinkBlock(bDest);
                            fgInsertBBafter(bNext, bDest);

                            if (ehIsBlockEHLast(bNext))
                            {
                                ehUpdateLastBlocks(bNext, bDest);
                            }

                            // Add fall through fixup block, if needed.
                            //
                            if ((bDest->bbJumpKind == BBJ_NONE) || (bDest->bbJumpKind == BBJ_COND))
                            {
                                BasicBlock* const bFixup = fgNewBBafter(BBJ_ALWAYS, bDest, true);
                                bFixup->inheritWeight(bDestNext);
                                bFixup->bbJumpDest = bDestNext;
                                fgReplacePred(bDestNext, bDest, bFixup);
                                fgAddRefPred(bFixup, bDest);
                            }
                        }
                    }

                    if (optimizeJump)
                    {
                        JITDUMP("\nReversing a conditional jump around an unconditional jump (" FMT_BB " -> " FMT_BB
                                " -> " FMT_BB ")\n",
                                block->bbNum, bDest->bbNum, bNextJumpDest->bbNum);

                        //  Reverse the jump condition
                        //
                        GenTree* test = block->lastNode();
                        noway_assert(test->OperIsConditionalJump());

                        if (test->OperGet() == GT_JTRUE)
                        {
                            GenTree* cond = gtReverseCond(test->AsOp()->gtOp1);
                            assert(cond == test->AsOp()->gtOp1); // Ensure `gtReverseCond` did not create a new node.
                            test->AsOp()->gtOp1 = cond;
                        }
                        else
                        {
                            gtReverseCond(test);
                        }

                        // Optimize the Conditional JUMP to go to the new target
                        block->bbJumpDest = bNext->bbJumpDest;

                        fgAddRefPred(bNext->bbJumpDest, block, fgRemoveRefPred(bNext->bbJumpDest, bNext));

                        /*
                          Unlink bNext from the BasicBlock list; note that we can
                          do this even though other blocks could jump to it - the
                          reason is that elsewhere in this function we always
                          redirect jumps to jumps to jump to the final label,
                          so even if another block jumps to bNext it won't matter
                          once we're done since any such jump will be redirected
                          to the final target by the time we're done here.
                        */

                        fgRemoveRefPred(bNext, block);
                        fgUnlinkBlock(bNext);

                        /* Mark the block as removed */
                        bNext->bbFlags |= BBF_REMOVED;

                        // If this is the first Cold basic block update fgFirstColdBlock
                        if (bNext == fgFirstColdBlock)
                        {
                            fgFirstColdBlock = bNext->bbNext;
                        }

                        //
                        // If we removed the end of a try region or handler region
                        // we will need to update ebdTryLast or ebdHndLast.
                        //

                        EHblkDsc* HBtab;
                        EHblkDsc* HBtabEnd;

                        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd;
                             HBtab++)
                        {
                            if ((HBtab->ebdTryLast == bNext) || (HBtab->ebdHndLast == bNext))
                            {
                                fgSkipRmvdBlocks(HBtab);
                            }
                        }

                        // we optimized this JUMP - goto REPEAT to catch similar cases
                        change   = true;
                        modified = true;

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("\nAfter reversing the jump:\n");
                            fgDispBasicBlocks(verboseTrees);
                        }
#endif // DEBUG

                        /*
                           For a rare special case we cannot jump to REPEAT
                           as jumping to REPEAT will cause us to delete 'block'
                           because it currently appears to be unreachable.  As
                           it is a self loop that only has a single bbRef (itself)
                           However since the unlinked bNext has additional bbRefs
                           (that we will later connect to 'block'), it is not really
                           unreachable.
                        */
                        if ((bNext->bbRefs > 0) && (bNext->bbJumpDest == block) && (block->bbRefs == 1))
                        {
                            continue;
                        }

                        goto REPEAT;
                    }
                }
            }

            //
            // Update the switch jump table such that it follows jumps to jumps:
            //
            if (block->bbJumpKind == BBJ_SWITCH)
            {
                if (fgOptimizeSwitchBranches(block))
                {
                    change   = true;
                    modified = true;
                    goto REPEAT;
                }
            }

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            /* COMPACT blocks if possible */

            if (fgCanCompactBlocks(block, bNext))
            {
                fgCompactBlocks(block, bNext);

                /* we compacted two blocks - goto REPEAT to catch similar cases */
                change   = true;
                modified = true;
                goto REPEAT;
            }

            /* Remove unreachable or empty blocks - do not consider blocks marked BBF_DONT_REMOVE or genReturnBB block
             * These include first and last block of a TRY, exception handlers and RANGE_CHECK_FAIL THROW blocks */

            if ((block->bbFlags & BBF_DONT_REMOVE) == BBF_DONT_REMOVE || block == genReturnBB)
            {
                bPrev = block;
                continue;
            }

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            // Don't remove the BBJ_ALWAYS block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair.
            if (block->countOfInEdges() == 0 && bPrev->bbJumpKind == BBJ_CALLFINALLY)
            {
                assert(bPrev->isBBCallAlwaysPair());
                noway_assert(!(bPrev->bbFlags & BBF_RETLESS_CALL));
                noway_assert(block->bbJumpKind == BBJ_ALWAYS);
                bPrev = block;
                continue;
            }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

            noway_assert(!block->bbCatchTyp);
            noway_assert(!(block->bbFlags & BBF_TRY_BEG));

            /* Remove unreachable blocks
             *
             * We'll look for blocks that have countOfInEdges() = 0 (blocks may become
             * unreachable due to a BBJ_ALWAYS introduced by conditional folding for example)
             */

            if (block->countOfInEdges() == 0)
            {
                /* no references -> unreachable - remove it */
                /* For now do not update the bbNum, do it at the end */

                fgRemoveBlock(block, true);

                change   = true;
                modified = true;

                /* we removed the current block - the rest of the optimizations won't have a target
                 * continue with the next one */

                continue;
            }
            else if (block->countOfInEdges() == 1)
            {
                switch (block->bbJumpKind)
                {
                    case BBJ_COND:
                    case BBJ_ALWAYS:
                        if (block->bbJumpDest == block)
                        {
                            fgRemoveBlock(block, true);

                            change   = true;
                            modified = true;

                            /* we removed the current block - the rest of the optimizations
                             * won't have a target so continue with the next block */

                            continue;
                        }
                        break;

                    default:
                        break;
                }
            }

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            /* Remove EMPTY blocks */

            if (block->isEmpty())
            {
                assert(bPrev == block->bbPrev);
                if (fgOptimizeEmptyBlock(block))
                {
                    change   = true;
                    modified = true;
                }

                /* Have we removed the block? */

                if (block->bbFlags & BBF_REMOVED)
                {
                    /* block was removed - no change to bPrev */
                    continue;
                }
            }

            /* Set the predecessor of the last reachable block
             * If we removed the current block, the predecessor remains unchanged
             * otherwise, since the current block is ok, it becomes the predecessor */

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            bPrev = block;
        }
    } while (change);

    fgNeedsUpdateFlowGraph = false;

#ifdef DEBUG
    if (verbose && modified)
    {
        printf("\nAfter updating the flow graph:\n");
        fgDispBasicBlocks(verboseTrees);
        fgDispHandlerTab();
    }

    if (compRationalIRForm)
    {
        for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            LIR::AsRange(block).CheckLIR(this);
        }
    }

    fgVerifyHandlerTab();
    // Make sure that the predecessor lists are accurate
    fgDebugCheckBBlist();
    fgDebugCheckUpdate();
#endif // DEBUG

    return modified;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//-------------------------------------------------------------
// fgGetCodeEstimate: Compute a code size estimate for the block, including all statements
// and block control flow.
//
// Arguments:
//    block - block to consider
//
// Returns:
//    Code size estimate for block
//
unsigned Compiler::fgGetCodeEstimate(BasicBlock* block)
{
    unsigned costSz = 0; // estimate of block's code size cost

    switch (block->bbJumpKind)
    {
        case BBJ_NONE:
            costSz = 0;
            break;
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_LEAVE:
        case BBJ_COND:
            costSz = 2;
            break;
        case BBJ_CALLFINALLY:
            costSz = 5;
            break;
        case BBJ_SWITCH:
            costSz = 10;
            break;
        case BBJ_THROW:
            costSz = 1; // We place a int3 after the code for a throw block
            break;
        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:
            costSz = 1;
            break;
        case BBJ_RETURN: // return from method
            costSz = 3;
            break;
        default:
            noway_assert(!"Bad bbJumpKind");
            break;
    }

    for (Statement* stmt : StatementList(block->FirstNonPhiDef()))
    {
        unsigned char cost = stmt->GetCostSz();
        costSz += cost;
    }

    return costSz;
}

#ifdef FEATURE_JIT_METHOD_PERF

//------------------------------------------------------------------------
// fgMeasureIR: count and return the number of IR nodes in the function.
//
unsigned Compiler::fgMeasureIR()
{
    unsigned nodeCount = 0;

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (!block->IsLIR())
        {
            for (Statement* stmt : block->Statements())
            {
                fgWalkTreePre(stmt->GetRootNodePointer(),
                              [](GenTree** slot, fgWalkData* data) -> Compiler::fgWalkResult {
                                  (*reinterpret_cast<unsigned*>(data->pCallbackData))++;
                                  return Compiler::WALK_CONTINUE;
                              },
                              &nodeCount);
            }
        }
        else
        {
            for (GenTree* node : LIR::AsRange(block))
            {
                nodeCount++;
            }
        }
    }

    return nodeCount;
}

#endif // FEATURE_JIT_METHOD_PERF

//------------------------------------------------------------------------
// fgCompDominatedByExceptionalEntryBlocks: compute blocks that are
// dominated by not normal entry.
//
void Compiler::fgCompDominatedByExceptionalEntryBlocks()
{
    assert(fgEnterBlksSetValid);
    if (BlockSetOps::Count(this, fgEnterBlks) != 1) // There are exception entries.
    {
        for (unsigned i = 1; i <= fgBBNumMax; ++i)
        {
            BasicBlock* block = fgBBInvPostOrder[i];
            if (BlockSetOps::IsMember(this, fgEnterBlks, block->bbNum))
            {
                if (fgFirstBB != block) // skip the normal entry.
                {
                    block->SetDominatedByExceptionalEntryFlag();
                }
            }
            else if (block->bbIDom->IsDominatedByExceptionalEntryFlag())
            {
                block->SetDominatedByExceptionalEntryFlag();
            }
        }
    }
}
