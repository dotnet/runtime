// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "ssaconfig.h"
#include "ssarenamestate.h"
#include "ssabuilder.h"

namespace
{
/**
 * Method that finds a common IDom parent, much like least common ancestor.
 *
 * @param finger1 A basic block that might share IDom ancestor with finger2.
 * @param finger2 A basic block that might share IDom ancestor with finger1.
 *
 * @see "A simple, fast dominance algorithm" by Keith D. Cooper, Timothy J. Harvey, Ken Kennedy.
 *
 * @return A basic block whose IDom is the dominator for finger1 and finger2,
 * or else NULL.  This may be called while immediate dominators are being
 * computed, and if the input values are members of the same loop (each reachable from the other),
 * then one may not yet have its immediate dominator computed when we are attempting
 * to find the immediate dominator of the other.  So a NULL return value means that the
 * the two inputs are in a cycle, not that they don't have a common dominator ancestor.
 */
static inline BasicBlock* IntersectDom(BasicBlock* finger1, BasicBlock* finger2)
{
    while (finger1 != finger2)
    {
        if (finger1 == nullptr || finger2 == nullptr)
        {
            return nullptr;
        }
        while (finger1 != nullptr && finger1->bbPostOrderNum < finger2->bbPostOrderNum)
        {
            finger1 = finger1->bbIDom;
        }
        if (finger1 == nullptr)
        {
            return nullptr;
        }
        while (finger2 != nullptr && finger2->bbPostOrderNum < finger1->bbPostOrderNum)
        {
            finger2 = finger2->bbIDom;
        }
    }
    return finger1;
}

} // end of anonymous namespace.

// =================================================================================
//                                      SSA
// =================================================================================

void Compiler::fgSsaBuild()
{
    // If this is not the first invocation, reset data structures for SSA.
    if (fgSsaPassesCompleted > 0)
    {
        fgResetForSsa();
    }

    SsaBuilder builder(this);
    builder.Build();
    fgSsaPassesCompleted++;
#ifdef DEBUG
    JitTestCheckSSA();
#endif // DEBUG

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\nAfter fgSsaBuild:\n");
        fgDispBasicBlocks(/*dumpTrees*/ true);
    }
#endif // DEBUG
}

void Compiler::fgResetForSsa()
{
    for (unsigned i = 0; i < lvaCount; ++i)
    {
        lvaTable[i].lvPerSsaData.Reset();
    }
    lvMemoryPerSsaData.Reset();
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        m_memorySsaMap[memoryKind] = nullptr;
    }

    for (BasicBlock* blk = fgFirstBB; blk != nullptr; blk = blk->bbNext)
    {
        // Eliminate phis.
        for (MemoryKind memoryKind : allMemoryKinds())
        {
            blk->bbMemorySsaPhiFunc[memoryKind] = nullptr;
        }
        if (blk->bbTreeList != nullptr)
        {
            GenTree* last   = blk->bbTreeList->gtPrev;
            blk->bbTreeList = blk->FirstNonPhiDef();
            if (blk->bbTreeList != nullptr)
            {
                blk->bbTreeList->gtPrev = last;
            }
        }

        // Clear post-order numbers and SSA numbers; SSA construction will overwrite these,
        // but only for reachable code, so clear them to avoid analysis getting confused
        // by stale annotations in unreachable code.
        blk->bbPostOrderNum = 0;
        for (GenTreeStmt* stmt = blk->firstStmt(); stmt != nullptr; stmt = stmt->getNextStmt())
        {
            for (GenTree* tree = stmt->gtStmt.gtStmtList; tree != nullptr; tree = tree->gtNext)
            {
                if (tree->IsLocal())
                {
                    tree->gtLclVarCommon.SetSsaNum(SsaConfig::RESERVED_SSA_NUM);
                    continue;
                }
            }
        }
    }
}

/**
 *  Constructor for the SSA builder.
 *
 *  @param pCompiler Current compiler instance.
 *
 *  @remarks Initializes the class and member pointers/objects that use constructors.
 */
SsaBuilder::SsaBuilder(Compiler* pCompiler)
    : m_pCompiler(pCompiler)
    , m_allocator(pCompiler->getAllocator(CMK_SSA))
    , m_visitedTraits(0, pCompiler) // at this point we do not know the size, SetupBBRoot can add a block
#ifdef SSA_FEATURE_DOMARR
    , m_pDomPreOrder(nullptr)
    , m_pDomPostOrder(nullptr)
#endif
{
}

//------------------------------------------------------------------------
//  TopologicalSort: Topologically sort the graph and return the number of nodes visited.
//
//  Arguments:
//     postOrder - The array in which the arranged basic blocks have to be returned.
//     count - The size of the postOrder array.
//
//  Return Value:
//     The number of nodes visited while performing DFS on the graph.

int SsaBuilder::TopologicalSort(BasicBlock** postOrder, int count)
{
    Compiler* comp = m_pCompiler;

    // TopologicalSort is called first so m_visited should already be empty
    assert(BitVecOps::IsEmpty(&m_visitedTraits, m_visited));

    // Display basic blocks.
    DBEXEC(VERBOSE, comp->fgDispBasicBlocks());
    DBEXEC(VERBOSE, comp->fgDispHandlerTab());

    auto DumpBlockAndSuccessors = [](Compiler* comp, BasicBlock* block) {
#ifdef DEBUG
        if (comp->verboseSsa)
        {
            printf("[SsaBuilder::TopologicalSort] Pushing " FMT_BB ": [", block->bbNum);
            AllSuccessorEnumerator successors(comp, block);
            unsigned               index = 0;
            while (true)
            {
                bool        isEHsucc = successors.IsNextEHSuccessor();
                BasicBlock* succ     = successors.NextSuccessor(comp);

                if (succ == nullptr)
                {
                    break;
                }

                printf("%s%s" FMT_BB, (index++ ? ", " : ""), (isEHsucc ? "[EH]" : ""), succ->bbNum);
            }
            printf("]\n");
        }
#endif
    };

    // Compute order.
    int         postIndex = 0;
    BasicBlock* block     = comp->fgFirstBB;
    BitVecOps::AddElemD(&m_visitedTraits, m_visited, block->bbNum);

    ArrayStack<AllSuccessorEnumerator> blocks(m_allocator);
    blocks.Emplace(comp, block);
    DumpBlockAndSuccessors(comp, block);

    while (blocks.Height() > 0)
    {
        BasicBlock* block = blocks.TopRef().Block();
        BasicBlock* succ  = blocks.TopRef().NextSuccessor(comp);

        if (succ != nullptr)
        {
            // if the block on TOS still has unreached successors, visit them
            if (BitVecOps::TryAddElemD(&m_visitedTraits, m_visited, succ->bbNum))
            {
                blocks.Emplace(comp, succ);
                DumpBlockAndSuccessors(comp, succ);
            }
        }
        else
        {
            // all successors have been visited
            blocks.Pop();

            DBG_SSA_JITDUMP("[SsaBuilder::TopologicalSort] postOrder[%d] = " FMT_BB "\n", postIndex, block->bbNum);
            postOrder[postIndex]  = block;
            block->bbPostOrderNum = postIndex;
            postIndex += 1;
        }
    }

    // In the absence of EH (because catch/finally have no preds), this should be valid.
    // assert(postIndex == (count - 1));

    return postIndex;
}

/**
 * Computes the immediate dominator IDom for each block iteratively.
 *
 * @param postOrder The array of basic blocks arranged in postOrder.
 * @param count The size of valid elements in the postOrder array.
 *
 * @see "A simple, fast dominance algorithm." paper.
 */
void SsaBuilder::ComputeImmediateDom(BasicBlock** postOrder, int count)
{
    JITDUMP("[SsaBuilder::ComputeImmediateDom]\n");

    // TODO-Cleanup: We currently have two dominance computations happening.  We should unify them; for
    // now, at least forget the results of the first.
    for (BasicBlock* blk = m_pCompiler->fgFirstBB; blk != nullptr; blk = blk->bbNext)
    {
        blk->bbIDom = nullptr;
    }

    // Add entry point to visited as its IDom is NULL.
    BitVecOps::ClearD(&m_visitedTraits, m_visited);
    BitVecOps::AddElemD(&m_visitedTraits, m_visited, m_pCompiler->fgFirstBB->bbNum);

    assert(postOrder[count - 1] == m_pCompiler->fgFirstBB);

    bool changed = true;
    while (changed)
    {
        changed = false;

        // In reverse post order, except for the entry block (count - 1 is entry BB).
        for (int i = count - 2; i >= 0; --i)
        {
            BasicBlock* block = postOrder[i];

            DBG_SSA_JITDUMP("Visiting in reverse post order: " FMT_BB ".\n", block->bbNum);

            // Find the first processed predecessor block.
            BasicBlock* predBlock = nullptr;
            for (flowList* pred = m_pCompiler->BlockPredsWithEH(block); pred; pred = pred->flNext)
            {
                if (BitVecOps::IsMember(&m_visitedTraits, m_visited, pred->flBlock->bbNum))
                {
                    predBlock = pred->flBlock;
                    break;
                }
            }

            // There could just be a single basic block, so just check if there were any preds.
            if (predBlock != nullptr)
            {
                DBG_SSA_JITDUMP("Pred block is " FMT_BB ".\n", predBlock->bbNum);
            }

            // Intersect DOM, if computed, for all predecessors.
            BasicBlock* bbIDom = predBlock;
            for (flowList* pred = m_pCompiler->BlockPredsWithEH(block); pred; pred = pred->flNext)
            {
                if (predBlock != pred->flBlock)
                {
                    BasicBlock* domAncestor = IntersectDom(pred->flBlock, bbIDom);
                    // The result may be NULL if "block" and "pred->flBlock" are part of a
                    // cycle -- neither is guaranteed ordered wrt the other in reverse postorder,
                    // so we may be computing the IDom of "block" before the IDom of "pred->flBlock" has
                    // been computed.  But that's OK -- if they're in a cycle, they share the same immediate
                    // dominator, so the contribution of "pred->flBlock" is not necessary to compute
                    // the result.
                    if (domAncestor != nullptr)
                    {
                        bbIDom = domAncestor;
                    }
                }
            }

            // Did we change the bbIDom value?  If so, we go around the outer loop again.
            if (block->bbIDom != bbIDom)
            {
                changed = true;

                // IDom has changed, update it.
                DBG_SSA_JITDUMP("bbIDom of " FMT_BB " becomes " FMT_BB ".\n", block->bbNum, bbIDom ? bbIDom->bbNum : 0);
                block->bbIDom = bbIDom;
            }

            // Mark the current block as visited.
            BitVecOps::AddElemD(&m_visitedTraits, m_visited, block->bbNum);

            DBG_SSA_JITDUMP("Marking block " FMT_BB " as processed.\n", block->bbNum);
        }
    }
}

#ifdef SSA_FEATURE_DOMARR
/**
 * Walk the DOM tree and compute pre and post-order arrangement of the tree.
 *
 * @param curBlock The current block being operated on at some recursive level.
 * @param domTree The DOM tree as a map (block -> set of child blocks.)
 * @param preIndex The initial index given to the first block visited in pre order.
 * @param postIndex The initial index given to the first block visited in post order.
 *
 * @remarks This would help us answer queries such as "a dom b?" in constant time.
 *          For example, if a dominated b, then Pre[a] < Pre[b] but Post[a] > Post[b]
 */
void SsaBuilder::DomTreeWalk(BasicBlock* curBlock, BlkToBlkVectorMap* domTree, int* preIndex, int* postIndex)
{
    JITDUMP("[SsaBuilder::DomTreeWalk] block %s:\n", curBlock->dspToString());

    // Store the order number at the block number in the pre order list.
    m_pDomPreOrder[curBlock->bbNum] = *preIndex;
    ++(*preIndex);

    BlkVector* domChildren = domTree->LookupPointer(curBlock);
    if (domChildren != nullptr)
    {
        for (BasicBlock* child : *domChildren)
        {
            if (curBlock != child)
            {
                DomTreeWalk(child, domTree, preIndex, postIndex);
            }
        }
    }

    // Store the order number at the block number in the post order list.
    m_pDomPostOrder[curBlock->bbNum] = *postIndex;
    ++(*postIndex);
}
#endif

/**
 * Using IDom of each basic block, add a mapping from block->IDom -> block.
 * @param pCompiler Compiler instance
 * @param block The basic block that will become the child node of it's iDom.
 * @param domTree The output domTree which will hold the mapping "block->bbIDom" -> "block"
 *
 */
/* static */
void SsaBuilder::ConstructDomTreeForBlock(Compiler* pCompiler, BasicBlock* block, BlkToBlkVectorMap* domTree)
{
    BasicBlock* bbIDom = block->bbIDom;

    // bbIDom for (only) fgFirstBB will be NULL.
    if (bbIDom == nullptr)
    {
        return;
    }

    // If the bbIDom map key doesn't exist, create one.
    BlkVector* domChildren = domTree->Emplace(bbIDom, domTree->GetAllocator());

    DBG_SSA_JITDUMP("Inserting " FMT_BB " as dom child of " FMT_BB ".\n", block->bbNum, bbIDom->bbNum);
    // Insert the block into the block's set.
    domChildren->push_back(block);
}

/**
 * Using IDom of each basic block, compute the whole tree. If a block "b" has IDom "i",
 * then, block "b" is dominated by "i". The mapping then is i -> { ..., b, ... }, in
 * other words, "domTree" is a tree represented by nodes mapped to their children.
 *
 * @param pCompiler Compiler instance
 * @param domTree The output domTree which will hold the mapping "block->bbIDom" -> "block"
 *
 */
/* static */
void SsaBuilder::ComputeDominators(Compiler* pCompiler, BlkToBlkVectorMap* domTree)
{
    JITDUMP("*************** In SsaBuilder::ComputeDominators(Compiler*, ...)\n");

    // Construct the DOM tree from bbIDom
    for (BasicBlock* block = pCompiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        ConstructDomTreeForBlock(pCompiler, block, domTree);
    }

    DBEXEC(pCompiler->verboseSsa, DisplayDominators(domTree));
}

/**
 * Compute the DOM tree into a map(block -> set of blocks) adjacency representation.
 *
 * Using IDom of each basic block, compute the whole tree. If a block "b" has IDom "i",
 * then, block "b" is dominated by "i". The mapping then is i -> { ..., b, ... }
 *
 * @param postOrder The array of basic blocks arranged in postOrder.
 * @param count The size of valid elements in the postOrder array.
 * @param domTree A map of (block -> set of blocks) tree representation that is empty.
 *
 */
void SsaBuilder::ComputeDominators(BasicBlock** postOrder, int count, BlkToBlkVectorMap* domTree)
{
    JITDUMP("*************** In SsaBuilder::ComputeDominators(BasicBlock** postOrder, int count, ...)\n");

    // Construct the DOM tree from bbIDom
    for (int i = 0; i < count; ++i)
    {
        ConstructDomTreeForBlock(m_pCompiler, postOrder[i], domTree);
    }

    DBEXEC(m_pCompiler->verboseSsa, DisplayDominators(domTree));

#ifdef SSA_FEATURE_DOMARR
    // Allocate space for constant time computation of (a DOM b?) query.
    unsigned bbArrSize = m_pCompiler->fgBBNumMax + 1; // We will use 1-based bbNums as indices into these arrays, so
                                                      // add 1.
    m_pDomPreOrder  = new (&m_allocator) int[bbArrSize];
    m_pDomPostOrder = new (&m_allocator) int[bbArrSize];

    // Initial counters.
    int preIndex  = 0;
    int postIndex = 0;

    // Populate the pre and post order of the tree.
    DomTreeWalk(m_pCompiler->fgFirstBB, domTree, &preIndex, &postIndex);
#endif
}

#ifdef DEBUG

/**
 * Display the DOM tree.
 *
 * @param domTree A map of (block -> set of blocks) tree representation.
 */
/* static */
void SsaBuilder::DisplayDominators(BlkToBlkVectorMap* domTree)
{
    printf("After computing dominator tree: \n");
    for (BlkToBlkVectorMap::KeyIterator nodes = domTree->Begin(); !nodes.Equal(domTree->End()); ++nodes)
    {
        printf(FMT_BB " := {", nodes.Get()->bbNum);
        int index = 0;
        for (BasicBlock* child : nodes.GetValue())
        {
            printf("%s" FMT_BB, (index++ == 0) ? "" : ",", child->bbNum);
        }
        printf("}\n");
    }
}

#endif // DEBUG

//------------------------------------------------------------------------
// ComputeDominanceFrontiers: Compute flow graph dominance frontiers
//
// Arguments:
//    postOrder - an array containing all flow graph blocks
//    count     - the number of blocks in the postOrder array
//    mapDF     - a caller provided hashtable that will be populated
//                with blocks and their dominance frontiers (only those
//                blocks that have non-empty frontiers will be included)
//
// Notes:
//     Recall that the dominance frontier of a block B is the set of blocks
//     B3 such that there exists some B2 s.t. B3 is a successor of B2, and
//     B dominates B2. Note that this dominance need not be strict -- B2
//     and B may be the same node.
//     See "A simple, fast dominance algorithm", by Cooper, Harvey, and Kennedy.
//
void SsaBuilder::ComputeDominanceFrontiers(BasicBlock** postOrder, int count, BlkToBlkVectorMap* mapDF)
{
    DBG_SSA_JITDUMP("Computing DF:\n");

    for (int i = 0; i < count; ++i)
    {
        BasicBlock* block = postOrder[i];

        DBG_SSA_JITDUMP("Considering block " FMT_BB ".\n", block->bbNum);

        // Recall that B3 is in the dom frontier of B1 if there exists a B2
        // such that B1 dom B2, !(B1 dom B3), and B3 is an immediate successor
        // of B2.  (Note that B1 might be the same block as B2.)
        // In that definition, we're considering "block" to be B3, and trying
        // to find B1's.  To do so, first we consider the predecessors of "block",
        // searching for candidate B2's -- "block" is obviously an immediate successor
        // of its immediate predecessors.  If there are zero or one preds, then there
        // is no pred, or else the single pred dominates "block", so no B2 exists.

        flowList* blockPreds = m_pCompiler->BlockPredsWithEH(block);

        // If block has 0/1 predecessor, skip.
        if ((blockPreds == nullptr) || (blockPreds->flNext == nullptr))
        {
            DBG_SSA_JITDUMP("   Has %d preds; skipping.\n", blockPreds == nullptr ? 0 : 1);
            continue;
        }

        // Otherwise, there are > 1 preds.  Each is a candidate B2 in the definition --
        // *unless* it dominates "block"/B3.

        for (flowList* pred = blockPreds; pred != nullptr; pred = pred->flNext)
        {
            DBG_SSA_JITDUMP("   Considering predecessor " FMT_BB ".\n", pred->flBlock->bbNum);

            // If we've found a B2, then consider the possible B1's.  We start with
            // B2, since a block dominates itself, then traverse upwards in the dominator
            // tree, stopping when we reach the root, or the immediate dominator of "block"/B3.
            // (Note that we are guaranteed to encounter this immediate dominator of "block"/B3:
            // a predecessor must be dominated by B3's immediate dominator.)
            // Along this way, make "block"/B3 part of the dom frontier of the B1.
            // When we reach this immediate dominator, the definition no longer applies, since this
            // potential B1 *does* dominate "block"/B3, so we stop.
            for (BasicBlock* b1 = pred->flBlock; (b1 != nullptr) && (b1 != block->bbIDom); // !root && !loop
                 b1             = b1->bbIDom)
            {
                DBG_SSA_JITDUMP("      Adding " FMT_BB " to dom frontier of pred dom " FMT_BB ".\n", block->bbNum,
                                b1->bbNum);

                BlkVector& b1DF = *mapDF->Emplace(b1, m_allocator);
                // It's possible to encounter the same DF multiple times, ensure that we don't add duplicates.
                if (b1DF.empty() || (b1DF.back() != block))
                {
                    b1DF.push_back(block);
                }
            }
        }
    }

#ifdef DEBUG
    if (m_pCompiler->verboseSsa)
    {
        printf("\nComputed DF:\n");
        for (int i = 0; i < count; ++i)
        {
            BasicBlock* b = postOrder[i];
            printf("Block " FMT_BB " := {", b->bbNum);

            BlkVector* bDF = mapDF->LookupPointer(b);
            if (bDF != nullptr)
            {
                int index = 0;
                for (BasicBlock* f : *bDF)
                {
                    printf("%s" FMT_BB, (index++ == 0) ? "" : ",", f->bbNum);
                }
            }
            printf("}\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// ComputeIteratedDominanceFrontier: Compute the iterated dominance frontier
// for the specified block.
//
// Arguments:
//    b     - the block to computed the frontier for
//    mapDF - a map containing the dominance frontiers of all blocks
//    bIDF  - a caller provided vector where the IDF is to be stored
//
// Notes:
//    The iterated dominance frontier is formed by a closure operation:
//    the IDF of B is the smallest set that includes B's dominance frontier,
//    and also includes the dominance frontier of all elements of the set.
//
void SsaBuilder::ComputeIteratedDominanceFrontier(BasicBlock* b, const BlkToBlkVectorMap* mapDF, BlkVector* bIDF)
{
    assert(bIDF->empty());

    BlkVector* bDF = mapDF->LookupPointer(b);

    if (bDF != nullptr)
    {
        // Compute IDF(b) - start by adding DF(b) to IDF(b).
        bIDF->reserve(bDF->size());
        BitVecOps::ClearD(&m_visitedTraits, m_visited);

        for (BasicBlock* f : *bDF)
        {
            BitVecOps::AddElemD(&m_visitedTraits, m_visited, f->bbNum);
            bIDF->push_back(f);
        }

        // Now for each block f from IDF(b) add DF(f) to IDF(b). This may result in new
        // blocks being added to IDF(b) and the process repeats until no more new blocks
        // are added. Note that since we keep adding to bIDF we can't use iterators as
        // they may get invalidated. This happens to be a convenient way to avoid having
        // to track newly added blocks in a separate set.
        for (size_t newIndex = 0; newIndex < bIDF->size(); newIndex++)
        {
            BasicBlock* f   = (*bIDF)[newIndex];
            BlkVector*  fDF = mapDF->LookupPointer(f);

            if (fDF != nullptr)
            {
                for (BasicBlock* ff : *fDF)
                {
                    if (BitVecOps::TryAddElemD(&m_visitedTraits, m_visited, ff->bbNum))
                    {
                        bIDF->push_back(ff);
                    }
                }
            }
        }
    }

#ifdef DEBUG
    if (m_pCompiler->verboseSsa)
    {
        printf("IDF(" FMT_BB ") := {", b->bbNum);
        int index = 0;
        for (BasicBlock* f : *bIDF)
        {
            printf("%s" FMT_BB, (index++ == 0) ? "" : ",", f->bbNum);
        }
        printf("}\n");
    }
#endif
}

/**
 * Returns the phi GT_PHI node if the variable already has a phi node.
 *
 * @param block The block for which the existence of a phi node needs to be checked.
 * @param lclNum The lclNum for which the occurrence of a phi node needs to be checked.
 *
 * @return If there is a phi node for the lclNum, returns the GT_PHI tree, else NULL.
 */
static GenTree* GetPhiNode(BasicBlock* block, unsigned lclNum)
{
    // Walk the statements for phi nodes.
    for (GenTree* stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
    {
        // A prefix of the statements of the block are phi definition nodes. If we complete processing
        // that prefix, exit.
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        GenTree* tree = stmt->gtStmt.gtStmtExpr;

        GenTree* phiLhs = tree->gtOp.gtOp1;
        assert(phiLhs->OperGet() == GT_LCL_VAR);
        if (phiLhs->gtLclVarCommon.gtLclNum == lclNum)
        {
            return tree->gtOp.gtOp2;
        }
    }
    return nullptr;
}

/**
 * Inserts phi functions at DF(b) for variables v that are live after the phi
 * insertion point i.e., v in live-in(b).
 *
 * To do so, the function computes liveness, dominance frontier and inserts a phi node,
 * if we have var v in def(b) and live-in(l) and l is in DF(b).
 *
 * @param postOrder The array of basic blocks arranged in postOrder.
 * @param count The size of valid elements in the postOrder array.
 */
void SsaBuilder::InsertPhiFunctions(BasicBlock** postOrder, int count)
{
    JITDUMP("*************** In SsaBuilder::InsertPhiFunctions()\n");

    // Compute dominance frontier.
    BlkToBlkVectorMap mapDF(m_allocator);
    ComputeDominanceFrontiers(postOrder, count, &mapDF);
    EndPhase(PHASE_BUILD_SSA_DF);

    // Use the same IDF vector for all blocks to avoid unnecessary memory allocations
    BlkVector blockIDF(m_allocator);

    JITDUMP("Inserting phi functions:\n");

    for (int i = 0; i < count; ++i)
    {
        BasicBlock* block = postOrder[i];
        DBG_SSA_JITDUMP("Considering dominance frontier of block " FMT_BB ":\n", block->bbNum);

        blockIDF.clear();
        ComputeIteratedDominanceFrontier(block, &mapDF, &blockIDF);

        if (blockIDF.empty())
        {
            continue;
        }

        // For each local var number "lclNum" that "block" assigns to...
        VarSetOps::Iter defVars(m_pCompiler, block->bbVarDef);
        unsigned        varIndex = 0;
        while (defVars.NextElem(&varIndex))
        {
            unsigned lclNum = m_pCompiler->lvaTrackedToVarNum[varIndex];
            DBG_SSA_JITDUMP("  Considering local var V%02u:\n", lclNum);

            if (!m_pCompiler->lvaInSsa(lclNum))
            {
                DBG_SSA_JITDUMP("  Skipping because it is excluded.\n");
                continue;
            }

            // For each block "bbInDomFront" that is in the dominance frontier of "block"...
            for (BasicBlock* bbInDomFront : blockIDF)
            {
                DBG_SSA_JITDUMP("     Considering " FMT_BB " in dom frontier of " FMT_BB ":\n", bbInDomFront->bbNum,
                                block->bbNum);

                // Check if variable "lclNum" is live in block "*iterBlk".
                if (!VarSetOps::IsMember(m_pCompiler, bbInDomFront->bbLiveIn, varIndex))
                {
                    continue;
                }

                // Check if we've already inserted a phi node.
                if (GetPhiNode(bbInDomFront, lclNum) == nullptr)
                {
                    // We have a variable i that is defined in block j and live at l, and l belongs to dom frontier of
                    // j. So insert a phi node at l.
                    JITDUMP("Inserting phi definition for V%02u at start of " FMT_BB ".\n", lclNum,
                            bbInDomFront->bbNum);

                    GenTree* phiLhs = m_pCompiler->gtNewLclvNode(lclNum, m_pCompiler->lvaTable[lclNum].TypeGet());

                    // Create 'phiRhs' as a GT_PHI node for 'lclNum', it will eventually hold a GT_LIST of GT_PHI_ARG
                    // nodes. However we have to construct this list so for now the gtOp1 of 'phiRhs' is a nullptr.
                    // It will get replaced with a GT_LIST of GT_PHI_ARG nodes in
                    // SsaBuilder::AssignPhiNodeRhsVariables() and in SsaBuilder::AddDefToHandlerPhis()

                    GenTree* phiRhs =
                        m_pCompiler->gtNewOperNode(GT_PHI, m_pCompiler->lvaTable[lclNum].TypeGet(), nullptr);

                    GenTree* phiAsg = m_pCompiler->gtNewAssignNode(phiLhs, phiRhs);

                    GenTree* stmt = m_pCompiler->fgInsertStmtAtBeg(bbInDomFront, phiAsg);
                    m_pCompiler->gtSetStmtInfo(stmt);
                    m_pCompiler->fgSetStmtSeq(stmt);
                }
            }
        }

        // Now make a similar phi definition if the block defines memory.
        if (block->bbMemoryDef != 0)
        {
            // For each block "bbInDomFront" that is in the dominance frontier of "block".
            for (BasicBlock* bbInDomFront : blockIDF)
            {
                DBG_SSA_JITDUMP("     Considering " FMT_BB " in dom frontier of " FMT_BB " for Memory phis:\n",
                                bbInDomFront->bbNum, block->bbNum);

                for (MemoryKind memoryKind : allMemoryKinds())
                {
                    if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
                    {
                        // Share the PhiFunc with ByrefExposed.
                        assert(memoryKind > ByrefExposed);
                        bbInDomFront->bbMemorySsaPhiFunc[memoryKind] = bbInDomFront->bbMemorySsaPhiFunc[ByrefExposed];
                        continue;
                    }

                    // Check if this memoryKind is defined in this block.
                    if ((block->bbMemoryDef & memoryKindSet(memoryKind)) == 0)
                    {
                        continue;
                    }

                    // Check if memoryKind is live into block "*iterBlk".
                    if ((bbInDomFront->bbMemoryLiveIn & memoryKindSet(memoryKind)) == 0)
                    {
                        continue;
                    }

                    // Check if we've already inserted a phi node.
                    if (bbInDomFront->bbMemorySsaPhiFunc[memoryKind] == nullptr)
                    {
                        // We have a variable i that is defined in block j and live at l, and l belongs to dom frontier
                        // of
                        // j. So insert a phi node at l.
                        JITDUMP("Inserting phi definition for %s at start of " FMT_BB ".\n",
                                memoryKindNames[memoryKind], bbInDomFront->bbNum);
                        bbInDomFront->bbMemorySsaPhiFunc[memoryKind] = BasicBlock::EmptyMemoryPhiDef;
                    }
                }
            }
        }
    }
    EndPhase(PHASE_BUILD_SSA_INSERT_PHIS);
}

/**
 * Rename the local variable tree node.
 *
 * If the given tree node is a local variable, then for a def give a new count, if use,
 * then give the count in the top of stack, i.e., current count (used for last def.)
 *
 * @param tree Tree node where an SSA variable is used or def'ed.
 * @param pRenameState The incremental rename information stored during renaming process.
 *
 * @remarks This method has to maintain parity with TreePopStacks corresponding to pushes
 *          it makes for defs.
 */
void SsaBuilder::TreeRenameVariables(GenTree* tree, BasicBlock* block, SsaRenameState* pRenameState, bool isPhiDefn)
{
    // This is perhaps temporary -- maybe should be done elsewhere.  Label GT_INDs on LHS of assignments, so we
    // can skip these during (at least) value numbering.
    if (tree->OperIs(GT_ASG))
    {
        GenTree* lhs     = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
        GenTree* trueLhs = lhs->gtEffectiveVal(/*commaOnly*/ true);
        if (trueLhs->OperIsIndir())
        {
            trueLhs->gtFlags |= GTF_IND_ASG_LHS;
        }
        else if (trueLhs->OperGet() == GT_CLS_VAR)
        {
            trueLhs->gtFlags |= GTF_CLS_VAR_ASG_LHS;
        }
    }

    // Figure out if "tree" may make a new GC heap state (if we care for this block).
    if ((block->bbMemoryHavoc & memoryKindSet(GcHeap)) == 0)
    {
        if (tree->OperIs(GT_ASG) || tree->OperIsBlkOp())
        {
            if (m_pCompiler->ehBlockHasExnFlowDsc(block))
            {
                GenTreeLclVarCommon* lclVarNode;

                bool isLocal            = tree->DefinesLocal(m_pCompiler, &lclVarNode);
                bool isAddrExposedLocal = isLocal && m_pCompiler->lvaVarAddrExposed(lclVarNode->gtLclNum);
                bool hasByrefHavoc      = ((block->bbMemoryHavoc & memoryKindSet(ByrefExposed)) != 0);
                if (!isLocal || (isAddrExposedLocal && !hasByrefHavoc))
                {
                    // It *may* define byref memory in a non-havoc way.  Make a new SSA # -- associate with this node.
                    unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                    if (!hasByrefHavoc)
                    {
                        pRenameState->PushMemory(ByrefExposed, block, ssaNum);
                        m_pCompiler->GetMemorySsaMap(ByrefExposed)->Set(tree, ssaNum);
#ifdef DEBUG
                        if (JitTls::GetCompiler()->verboseSsa)
                        {
                            printf("Node ");
                            Compiler::printTreeID(tree);
                            printf(" (in try block) may define memory; ssa # = %d.\n", ssaNum);
                        }
#endif // DEBUG

                        // Now add this SSA # to all phis of the reachable catch blocks.
                        AddMemoryDefToHandlerPhis(ByrefExposed, block, ssaNum);
                    }

                    if (!isLocal)
                    {
                        // Add a new def for GcHeap as well
                        if (m_pCompiler->byrefStatesMatchGcHeapStates)
                        {
                            // GcHeap and ByrefExposed share the same stacks, SsaMap, and phis
                            assert(!hasByrefHavoc);
                            assert(pRenameState->CountForMemoryUse(GcHeap) == ssaNum);
                            assert(*m_pCompiler->GetMemorySsaMap(GcHeap)->LookupPointer(tree) == ssaNum);
                            assert(block->bbMemorySsaPhiFunc[GcHeap] == block->bbMemorySsaPhiFunc[ByrefExposed]);
                        }
                        else
                        {
                            if (!hasByrefHavoc)
                            {
                                // Allocate a distinct defnum for the GC Heap
                                ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                            }

                            pRenameState->PushMemory(GcHeap, block, ssaNum);
                            m_pCompiler->GetMemorySsaMap(GcHeap)->Set(tree, ssaNum);
                            AddMemoryDefToHandlerPhis(GcHeap, block, ssaNum);
                        }
                    }
                }
            }
        }
    }

    if (!tree->IsLocal())
    {
        return;
    }

    unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
    // Is this a variable we exclude from SSA?
    if (!m_pCompiler->lvaInSsa(lclNum))
    {
        tree->gtLclVarCommon.SetSsaNum(SsaConfig::RESERVED_SSA_NUM);
        return;
    }

    if ((tree->gtFlags & GTF_VAR_DEF) != 0)
    {
        // Allocate a new SSA number for this definition tree.
        unsigned ssaNum = m_pCompiler->lvaTable[lclNum].lvPerSsaData.AllocSsaNum(m_allocator, block, tree);

        if ((tree->gtFlags & GTF_VAR_USEASG) != 0)
        {
            // This is a partial definition of a variable. The node records only the SSA number
            // of the use that is implied by this partial definition. The SSA number of the new
            // definition will be recorded in the m_opAsgnVarDefSsaNums map.
            tree->AsLclVarCommon()->SetSsaNum(pRenameState->CountForUse(lclNum));

            m_pCompiler->GetOpAsgnVarDefSsaNums()->Set(tree, ssaNum);
        }
        else
        {
            tree->AsLclVarCommon()->SetSsaNum(ssaNum);
        }

        pRenameState->Push(block, lclNum, ssaNum);

        // If necessary, add "lclNum/count" to the arg list of a phi def in any
        // handlers for try blocks that "block" is within.  (But only do this for "real" definitions,
        // not phi definitions.)
        if (!isPhiDefn)
        {
            AddDefToHandlerPhis(block, lclNum, ssaNum);
        }
    }
    else if (!isPhiDefn) // Phi args already have ssa numbers.
    {
        // This case is obviated by the short-term "early-out" above...but it's in the right direction.
        // Is it a promoted struct local?
        if (m_pCompiler->lvaTable[lclNum].lvPromoted)
        {
            assert(tree->TypeGet() == TYP_STRUCT);
            LclVarDsc* varDsc = &m_pCompiler->lvaTable[lclNum];
            // If has only a single field var, treat this as a use of that field var.
            // Otherwise, we don't give SSA names to uses of promoted struct vars.
            if (varDsc->lvFieldCnt == 1)
            {
                lclNum = varDsc->lvFieldLclStart;
            }
            else
            {
                tree->gtLclVarCommon.SetSsaNum(SsaConfig::RESERVED_SSA_NUM);
                return;
            }
        }
        // Give the count as top of stack.
        unsigned count = pRenameState->CountForUse(lclNum);
        tree->gtLclVarCommon.SetSsaNum(count);
    }
}

void SsaBuilder::AddDefToHandlerPhis(BasicBlock* block, unsigned lclNum, unsigned count)
{
    assert(m_pCompiler->lvaTable[lclNum].lvTracked); // Precondition.
    unsigned lclIndex = m_pCompiler->lvaTable[lclNum].lvVarIndex;

    EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
    if (tryBlk != nullptr)
    {
        DBG_SSA_JITDUMP("Definition of local V%02u/d:%d in block " FMT_BB
                        " has exn handler; adding as phi arg to handlers.\n",
                        lclNum, count, block->bbNum);
        while (true)
        {
            BasicBlock* handler = tryBlk->ExFlowBlock();

            // Is "lclNum" live on entry to the handler?
            if (VarSetOps::IsMember(m_pCompiler, handler->bbLiveIn, lclIndex))
            {
#ifdef DEBUG
                bool phiFound = false;
#endif
                // A prefix of blocks statements will be SSA definitions.  Search those for "lclNum".
                for (GenTree* stmt = handler->bbTreeList; stmt; stmt = stmt->gtNext)
                {
                    // If the tree is not an SSA def, break out of the loop: we're done.
                    if (!stmt->IsPhiDefnStmt())
                    {
                        break;
                    }

                    GenTree* tree = stmt->gtStmt.gtStmtExpr;

                    assert(tree->IsPhiDefn());

                    if (tree->gtOp.gtOp1->gtLclVar.gtLclNum == lclNum)
                    {
                        // It's the definition for the right local.  Add "count" to the RHS.
                        GenTree*        phi  = tree->gtOp.gtOp2;
                        GenTreeArgList* args = nullptr;
                        if (phi->gtOp.gtOp1 != nullptr)
                        {
                            args = phi->gtOp.gtOp1->AsArgList();
                        }
#ifdef DEBUG
                        // Make sure it isn't already present: we should only add each definition once.
                        for (GenTreeArgList* curArgs = args; curArgs != nullptr; curArgs = curArgs->Rest())
                        {
                            GenTreePhiArg* phiArg = curArgs->Current()->AsPhiArg();
                            assert(phiArg->gtSsaNum != count);
                        }
#endif
                        var_types      typ = m_pCompiler->lvaTable[lclNum].TypeGet();
                        GenTreePhiArg* newPhiArg =
                            new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(typ, lclNum, count, block);

                        phi->gtOp.gtOp1 = new (m_pCompiler, GT_LIST) GenTreeArgList(newPhiArg, args);
                        m_pCompiler->gtSetStmtInfo(stmt);
                        m_pCompiler->fgSetStmtSeq(stmt);
#ifdef DEBUG
                        phiFound = true;
#endif
                        DBG_SSA_JITDUMP("   Added phi arg u:%d for V%02u to phi defn in handler block " FMT_BB ".\n",
                                        count, lclNum, handler->bbNum);
                        break;
                    }
                }
                assert(phiFound);
            }

            unsigned nextTryIndex = tryBlk->ebdEnclosingTryIndex;
            if (nextTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                break;
            }

            tryBlk = m_pCompiler->ehGetDsc(nextTryIndex);
        }
    }
}

void SsaBuilder::AddMemoryDefToHandlerPhis(MemoryKind memoryKind, BasicBlock* block, unsigned count)
{
    if (m_pCompiler->ehBlockHasExnFlowDsc(block))
    {
        // Don't do anything for a compiler-inserted BBJ_ALWAYS that is a "leave helper".
        if (block->bbJumpKind == BBJ_ALWAYS && (block->bbFlags & BBF_INTERNAL) && (block->bbPrev->isBBCallAlwaysPair()))
        {
            return;
        }

        // Otherwise...
        DBG_SSA_JITDUMP("Definition of %s/d:%d in block " FMT_BB " has exn handler; adding as phi arg to handlers.\n",
                        memoryKindNames[memoryKind], count, block->bbNum);
        EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
        while (true)
        {
            BasicBlock* handler = tryBlk->ExFlowBlock();

            // Is memoryKind live on entry to the handler?
            if ((handler->bbMemoryLiveIn & memoryKindSet(memoryKind)) != 0)
            {
                assert(handler->bbMemorySsaPhiFunc != nullptr);

                // Add "count" to the phi args of memoryKind.
                BasicBlock::MemoryPhiArg*& handlerMemoryPhi = handler->bbMemorySsaPhiFunc[memoryKind];

#if DEBUG
                if (m_pCompiler->byrefStatesMatchGcHeapStates)
                {
                    // When sharing phis for GcHeap and ByrefExposed, callers should ask to add phis
                    // for ByrefExposed only.
                    assert(memoryKind != GcHeap);
                    if (memoryKind == ByrefExposed)
                    {
                        // The GcHeap and ByrefExposed phi funcs should always be in sync.
                        assert(handlerMemoryPhi == handler->bbMemorySsaPhiFunc[GcHeap]);
                    }
                }
#endif

                if (handlerMemoryPhi == BasicBlock::EmptyMemoryPhiDef)
                {
                    handlerMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(count);
                }
                else
                {
#ifdef DEBUG
                    BasicBlock::MemoryPhiArg* curArg = handler->bbMemorySsaPhiFunc[memoryKind];
                    while (curArg != nullptr)
                    {
                        assert(curArg->GetSsaNum() != count);
                        curArg = curArg->m_nextArg;
                    }
#endif // DEBUG
                    handlerMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(count, handlerMemoryPhi);
                }

                DBG_SSA_JITDUMP("   Added phi arg u:%d for %s to phi defn in handler block " FMT_BB ".\n", count,
                                memoryKindNames[memoryKind], memoryKind, handler->bbNum);

                if ((memoryKind == ByrefExposed) && m_pCompiler->byrefStatesMatchGcHeapStates)
                {
                    // Share the phi between GcHeap and ByrefExposed.
                    handler->bbMemorySsaPhiFunc[GcHeap] = handlerMemoryPhi;
                }
            }
            unsigned tryInd = tryBlk->ebdEnclosingTryIndex;
            if (tryInd == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                break;
            }
            tryBlk = m_pCompiler->ehGetDsc(tryInd);
        }
    }
}

/**
 * Walk the block's tree in the evaluation order and give var definitions and uses their
 * SSA names.
 *
 * @param block Block for which SSA variables have to be renamed.
 * @param pRenameState The incremental rename information stored during renaming process.
 *
 */
void SsaBuilder::BlockRenameVariables(BasicBlock* block, SsaRenameState* pRenameState)
{
    // Walk the statements of the block and rename the tree variables.

    // First handle the incoming memory states.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // ByrefExposed and GcHeap share any phi this block may have,
            assert(block->bbMemorySsaPhiFunc[memoryKind] == block->bbMemorySsaPhiFunc[ByrefExposed]);
            // so we will have already allocated a defnum for it if needed.
            assert(memoryKind > ByrefExposed);
            assert(pRenameState->CountForMemoryUse(memoryKind) == pRenameState->CountForMemoryUse(ByrefExposed));
        }
        else
        {
            // Is there an Phi definition for memoryKind at the start of this block?
            if (block->bbMemorySsaPhiFunc[memoryKind] != nullptr)
            {
                unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                pRenameState->PushMemory(memoryKind, block, ssaNum);

                DBG_SSA_JITDUMP("Ssa # for %s phi on entry to " FMT_BB " is %d.\n", memoryKindNames[memoryKind],
                                block->bbNum, ssaNum);
            }
        }

        // Record the "in" Ssa # for memoryKind.
        block->bbMemorySsaNumIn[memoryKind] = pRenameState->CountForMemoryUse(memoryKind);
    }

    // We need to iterate over phi definitions, to give them SSA names, but we need
    // to know which are which, so we don't add phi definitions to handler phi arg lists.
    // Statements are phi defns until they aren't.
    bool     isPhiDefn   = true;
    GenTree* firstNonPhi = block->FirstNonPhiDef();
    for (GenTree* stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
    {
        if (stmt == firstNonPhi)
        {
            isPhiDefn = false;
        }

        for (GenTree* tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
        {
            TreeRenameVariables(tree, block, pRenameState, isPhiDefn);
        }
    }

    // Now handle the final memory states.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        MemoryKindSet memorySet = memoryKindSet(memoryKind);

        // If the block defines memory, allocate an SSA variable for the final memory state in the block.
        // (This may be redundant with the last SSA var explicitly created, but there's no harm in that.)
        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // We've already allocated the SSA num and propagated it to shared phis, if needed,
            // when processing ByrefExposed.
            assert(memoryKind > ByrefExposed);
            assert(((block->bbMemoryDef & memorySet) != 0) ==
                   ((block->bbMemoryDef & memoryKindSet(ByrefExposed)) != 0));
            assert(pRenameState->CountForMemoryUse(memoryKind) == pRenameState->CountForMemoryUse(ByrefExposed));
        }
        else
        {
            if ((block->bbMemoryDef & memorySet) != 0)
            {
                unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                pRenameState->PushMemory(memoryKind, block, ssaNum);
                AddMemoryDefToHandlerPhis(memoryKind, block, ssaNum);
            }
        }

        // Record the "out" Ssa" # for memoryKind.
        block->bbMemorySsaNumOut[memoryKind] = pRenameState->CountForMemoryUse(memoryKind);

        DBG_SSA_JITDUMP("Ssa # for %s on entry to " FMT_BB " is %d; on exit is %d.\n", memoryKindNames[memoryKind],
                        block->bbNum, block->bbMemorySsaNumIn[memoryKind], block->bbMemorySsaNumOut[memoryKind]);
    }
}

/**
 * Walk through the phi nodes of a given block and assign rhs variables to them.
 *
 * Also renumber the rhs variables from top of the stack.
 *
 * @param block Block for which phi nodes have to be assigned their rhs arguments.
 * @param pRenameState The incremental rename information stored during renaming process.
 *
 */
void SsaBuilder::AssignPhiNodeRhsVariables(BasicBlock* block, SsaRenameState* pRenameState)
{
    for (BasicBlock* succ : block->GetAllSuccs(m_pCompiler))
    {
        // Walk the statements for phi nodes.
        for (GenTree* stmt = succ->bbTreeList; stmt != nullptr && stmt->IsPhiDefnStmt(); stmt = stmt->gtNext)
        {
            GenTree* tree = stmt->gtStmt.gtStmtExpr;
            assert(tree->IsPhiDefn());

            // Get the phi node from GT_ASG.
            GenTree* phiNode = tree->gtOp.gtOp2;
            assert(phiNode->gtOp.gtOp1 == nullptr || phiNode->gtOp.gtOp1->OperGet() == GT_LIST);

            unsigned lclNum = tree->gtOp.gtOp1->gtLclVar.gtLclNum;
            unsigned ssaNum = pRenameState->CountForUse(lclNum);
            // Search the arglist for an existing definition for ssaNum.
            // (Can we assert that its the head of the list?  This should only happen when we add
            // during renaming for a definition that occurs within a try, and then that's the last
            // value of the var within that basic block.)
            GenTreeArgList* argList = (phiNode->gtOp.gtOp1 == nullptr ? nullptr : phiNode->gtOp.gtOp1->AsArgList());
            bool            found   = false;
            while (argList != nullptr)
            {
                if (argList->Current()->AsLclVarCommon()->GetSsaNum() == ssaNum)
                {
                    found = true;
                    break;
                }
                argList = argList->Rest();
            }
            if (!found)
            {
                GenTree* newPhiArg =
                    new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(tree->gtOp.gtOp1->TypeGet(), lclNum, ssaNum, block);
                argList             = (phiNode->gtOp.gtOp1 == nullptr ? nullptr : phiNode->gtOp.gtOp1->AsArgList());
                phiNode->gtOp.gtOp1 = new (m_pCompiler, GT_LIST) GenTreeArgList(newPhiArg, argList);
                DBG_SSA_JITDUMP("  Added phi arg u:%d for V%02u from " FMT_BB " in " FMT_BB ".\n", ssaNum, lclNum,
                                block->bbNum, succ->bbNum);
            }

            m_pCompiler->gtSetStmtInfo(stmt);
            m_pCompiler->fgSetStmtSeq(stmt);
        }

        // Now handle memory.
        for (MemoryKind memoryKind : allMemoryKinds())
        {
            BasicBlock::MemoryPhiArg*& succMemoryPhi = succ->bbMemorySsaPhiFunc[memoryKind];
            if (succMemoryPhi != nullptr)
            {
                if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
                {
                    // We've already propagated the "out" number to the phi shared with ByrefExposed,
                    // but still need to update bbMemorySsaPhiFunc to be in sync between GcHeap and ByrefExposed.
                    assert(memoryKind > ByrefExposed);
                    assert(block->bbMemorySsaNumOut[memoryKind] == block->bbMemorySsaNumOut[ByrefExposed]);
                    assert((succ->bbMemorySsaPhiFunc[ByrefExposed] == succMemoryPhi) ||
                           (succ->bbMemorySsaPhiFunc[ByrefExposed]->m_nextArg ==
                            (succMemoryPhi == BasicBlock::EmptyMemoryPhiDef ? nullptr : succMemoryPhi)));
                    succMemoryPhi = succ->bbMemorySsaPhiFunc[ByrefExposed];

                    continue;
                }

                if (succMemoryPhi == BasicBlock::EmptyMemoryPhiDef)
                {
                    succMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(block->bbMemorySsaNumOut[memoryKind]);
                }
                else
                {
                    BasicBlock::MemoryPhiArg* curArg = succMemoryPhi;
                    unsigned                  ssaNum = block->bbMemorySsaNumOut[memoryKind];
                    bool                      found  = false;
                    // This is a quadratic algorithm.  We might need to consider some switch over to a hash table
                    // representation for the arguments of a phi node, to make this linear.
                    while (curArg != nullptr)
                    {
                        if (curArg->m_ssaNum == ssaNum)
                        {
                            found = true;
                            break;
                        }
                        curArg = curArg->m_nextArg;
                    }
                    if (!found)
                    {
                        succMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(ssaNum, succMemoryPhi);
                    }
                }
                DBG_SSA_JITDUMP("  Added phi arg for %s u:%d from " FMT_BB " in " FMT_BB ".\n",
                                memoryKindNames[memoryKind], block->bbMemorySsaNumOut[memoryKind], block->bbNum,
                                succ->bbNum);
            }
        }

        // If "succ" is the first block of a try block (and "block" is not also in that try block)
        // then we must look at the vars that have phi defs in the corresponding handler;
        // the current SSA name for such vars must be included as an argument to that phi.
        if (m_pCompiler->bbIsTryBeg(succ))
        {
            assert(succ->hasTryIndex());
            unsigned tryInd = succ->getTryIndex();

            while (tryInd != EHblkDsc::NO_ENCLOSING_INDEX)
            {
                // Check if the predecessor "block" is within the same try block.
                if (block->hasTryIndex())
                {
                    for (unsigned blockTryInd = block->getTryIndex(); blockTryInd != EHblkDsc::NO_ENCLOSING_INDEX;
                         blockTryInd          = m_pCompiler->ehGetEnclosingTryIndex(blockTryInd))
                    {
                        if (blockTryInd == tryInd)
                        {
                            // It is; don't execute the loop below.
                            tryInd = EHblkDsc::NO_ENCLOSING_INDEX;
                            break;
                        }
                    }

                    // The loop just above found that the predecessor "block" is within the same
                    // try block as "succ."  So we don't need to process this try, or any
                    // further outer try blocks here, since they would also contain both "succ"
                    // and "block".
                    if (tryInd == EHblkDsc::NO_ENCLOSING_INDEX)
                    {
                        break;
                    }
                }

                EHblkDsc* succTry = m_pCompiler->ehGetDsc(tryInd);
                // This is necessarily true on the first iteration, but not
                // necessarily on the second and subsequent.
                if (succTry->ebdTryBeg != succ)
                {
                    break;
                }

                // succ is the first block of this try.  Look at phi defs in the handler.
                // For a filter, we consider the filter to be the "real" handler.
                BasicBlock* handlerStart = succTry->ExFlowBlock();

                for (GenTree* stmt = handlerStart->bbTreeList; stmt; stmt = stmt->gtNext)
                {
                    GenTree* tree = stmt->gtStmt.gtStmtExpr;

                    // Check if the first n of the statements are phi nodes. If not, exit.
                    if (tree->OperGet() != GT_ASG || tree->gtOp.gtOp2 == nullptr ||
                        tree->gtOp.gtOp2->OperGet() != GT_PHI)
                    {
                        break;
                    }

                    // Get the phi node from GT_ASG.
                    GenTree* lclVar = tree->gtOp.gtOp1;
                    unsigned lclNum = lclVar->gtLclVar.gtLclNum;

                    // If the variable is live-out of "blk", and is therefore live on entry to the try-block-start
                    // "succ", then we make sure the current SSA name for the
                    // var is one of the args of the phi node.  If not, go on.
                    LclVarDsc* lclVarDsc = &m_pCompiler->lvaTable[lclNum];
                    if (!lclVarDsc->lvTracked ||
                        !VarSetOps::IsMember(m_pCompiler, block->bbLiveOut, lclVarDsc->lvVarIndex))
                    {
                        continue;
                    }

                    GenTree* phiNode = tree->gtOp.gtOp2;
                    assert(phiNode->gtOp.gtOp1 == nullptr || phiNode->gtOp.gtOp1->OperGet() == GT_LIST);
                    GenTreeArgList* argList = reinterpret_cast<GenTreeArgList*>(phiNode->gtOp.gtOp1);

                    // What is the current SSAName from the predecessor for this local?
                    unsigned ssaNum = pRenameState->CountForUse(lclNum);

                    // See if this ssaNum is already an arg to the phi.
                    bool alreadyArg = false;
                    for (GenTreeArgList* curArgs = argList; curArgs != nullptr; curArgs = curArgs->Rest())
                    {
                        if (curArgs->Current()->gtPhiArg.gtSsaNum == ssaNum)
                        {
                            alreadyArg = true;
                            break;
                        }
                    }
                    if (!alreadyArg)
                    {
                        // Add the new argument.
                        GenTree* newPhiArg =
                            new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(lclVar->TypeGet(), lclNum, ssaNum, block);
                        phiNode->gtOp.gtOp1 = new (m_pCompiler, GT_LIST) GenTreeArgList(newPhiArg, argList);

                        DBG_SSA_JITDUMP("  Added phi arg u:%d for V%02u from " FMT_BB " in " FMT_BB ".\n", ssaNum,
                                        lclNum, block->bbNum, handlerStart->bbNum);

                        m_pCompiler->gtSetStmtInfo(stmt);
                        m_pCompiler->fgSetStmtSeq(stmt);
                    }
                }

                // Now handle memory.
                for (MemoryKind memoryKind : allMemoryKinds())
                {
                    BasicBlock::MemoryPhiArg*& handlerMemoryPhi = handlerStart->bbMemorySsaPhiFunc[memoryKind];
                    if (handlerMemoryPhi != nullptr)
                    {
                        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
                        {
                            // We've already added the arg to the phi shared with ByrefExposed if needed,
                            // but still need to update bbMemorySsaPhiFunc to stay in sync.
                            assert(memoryKind > ByrefExposed);
                            assert(block->bbMemorySsaNumOut[memoryKind] == block->bbMemorySsaNumOut[ByrefExposed]);
                            assert(handlerStart->bbMemorySsaPhiFunc[ByrefExposed]->m_ssaNum ==
                                   block->bbMemorySsaNumOut[memoryKind]);
                            handlerMemoryPhi = handlerStart->bbMemorySsaPhiFunc[ByrefExposed];

                            continue;
                        }

                        if (handlerMemoryPhi == BasicBlock::EmptyMemoryPhiDef)
                        {
                            handlerMemoryPhi =
                                new (m_pCompiler) BasicBlock::MemoryPhiArg(block->bbMemorySsaNumOut[memoryKind]);
                        }
                        else
                        {
                            // This path has a potential to introduce redundant phi args, due to multiple
                            // preds of the same try-begin block having the same live-out memory def, and/or
                            // due to nested try-begins each having preds with the same live-out memory def.
                            // Avoid doing quadratic processing on handler phis, and instead live with the
                            // occasional redundancy.
                            handlerMemoryPhi = new (m_pCompiler)
                                BasicBlock::MemoryPhiArg(block->bbMemorySsaNumOut[memoryKind], handlerMemoryPhi);
                        }
                        DBG_SSA_JITDUMP("  Added phi arg for %s u:%d from " FMT_BB " in " FMT_BB ".\n",
                                        memoryKindNames[memoryKind], block->bbMemorySsaNumOut[memoryKind], block->bbNum,
                                        handlerStart->bbNum);
                    }
                }

                tryInd = succTry->ebdEnclosingTryIndex;
            }
        }
    }
}

/**
 * Walk the block's tree in the evaluation order and reclaim rename stack for var definitions.
 *
 * @param block Block for which SSA variables have to be renamed.
 * @param pRenameState The incremental rename information stored during renaming process.
 *
 */
void SsaBuilder::BlockPopStacks(BasicBlock* block, SsaRenameState* pRenameState)
{
    // Pop the names given to the non-phi nodes.
    pRenameState->PopBlockStacks(block);

    // And for memory.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // GcHeap and ByrefExposed share a rename stack, so don't try
            // to pop it a second time.
            continue;
        }
        pRenameState->PopBlockMemoryStack(memoryKind, block);
    }
}

/**
 * Perform variable renaming.
 *
 * Walks the blocks and renames all var defs with ssa numbers and all uses with the
 * current count that is in the top of the stack. Assigns phi node rhs variables
 * (i.e., the arguments to the phi.) Then, calls the function recursively on child
 * nodes in the DOM tree to continue the renaming process.
 *
 * @param block Block for which SSA variables have to be renamed.
 * @param pRenameState The incremental rename information stored during renaming process.
 *
 * @remarks At the end of the method, m_uses and m_defs should be populated linking the
 *          uses and defs.
 *
 * @see Briggs, Cooper, Harvey and Simpson "Practical Improvements to the Construction
 *      and Destruction of Static Single Assignment Form."
 */

void SsaBuilder::RenameVariables(BlkToBlkVectorMap* domTree, SsaRenameState* pRenameState)
{
    JITDUMP("*************** In SsaBuilder::RenameVariables()\n");

    // The first thing we do is treat parameters and must-init variables as if they have a
    // virtual definition before entry -- they start out at SSA name 1.
    for (unsigned lclNum = 0; lclNum < m_pCompiler->lvaCount; lclNum++)
    {
        if (!m_pCompiler->lvaInSsa(lclNum))
        {
            continue;
        }

        LclVarDsc* varDsc = &m_pCompiler->lvaTable[lclNum];
        assert(varDsc->lvTracked);

        if (varDsc->lvIsParam || m_pCompiler->info.compInitMem || varDsc->lvMustInit ||
            VarSetOps::IsMember(m_pCompiler, m_pCompiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            unsigned ssaNum = varDsc->lvPerSsaData.AllocSsaNum(m_allocator);

            // In ValueNum we'd assume un-inited variables get FIRST_SSA_NUM.
            assert(ssaNum == SsaConfig::FIRST_SSA_NUM);

            pRenameState->Push(nullptr, lclNum, ssaNum);
        }
    }

    // In ValueNum we'd assume un-inited memory gets FIRST_SSA_NUM.
    // The memory is a parameter.  Use FIRST_SSA_NUM as first SSA name.
    unsigned initMemorySsaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
    assert(initMemorySsaNum == SsaConfig::FIRST_SSA_NUM);
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // GcHeap shares its stack with ByrefExposed; don't re-push.
            continue;
        }
        pRenameState->PushMemory(memoryKind, m_pCompiler->fgFirstBB, initMemorySsaNum);
    }

    // Initialize the memory ssa numbers for unreachable blocks. ValueNum expects
    // memory ssa numbers to have some intitial value.
    for (BasicBlock* block = m_pCompiler->fgFirstBB; block; block = block->bbNext)
    {
        if (block->bbIDom == nullptr)
        {
            for (MemoryKind memoryKind : allMemoryKinds())
            {
                block->bbMemorySsaNumIn[memoryKind]  = initMemorySsaNum;
                block->bbMemorySsaNumOut[memoryKind] = initMemorySsaNum;
            }
        }
    }

    struct BlockWork
    {
        BasicBlock* m_blk;
        bool        m_processed; // Whether the this block have already been processed: its var renamed, and children
                                 // processed.
                                 // If so, awaiting only BlockPopStacks.
        BlockWork(BasicBlock* blk, bool processed = false) : m_blk(blk), m_processed(processed)
        {
        }
    };
    typedef jitstd::vector<BlockWork> BlockWorkStack;

    BlockWorkStack* blocksToDo = new (m_allocator) BlockWorkStack(m_allocator);
    blocksToDo->push_back(BlockWork(m_pCompiler->fgFirstBB)); // Probably have to include other roots of dom tree.

    while (blocksToDo->size() != 0)
    {
        BlockWork blockWrk = blocksToDo->back();
        blocksToDo->pop_back();
        BasicBlock* block = blockWrk.m_blk;

        DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables](" FMT_BB ", processed = %d)\n", block->bbNum,
                        blockWrk.m_processed);

        if (!blockWrk.m_processed)
        {
            // Push the block back on the stack with "m_processed" true, to record the fact that when its children have
            // been (recursively) processed, we still need to call BlockPopStacks on it.
            blocksToDo->push_back(BlockWork(block, true));

            // Walk the block give counts to DEFs and give top of stack count for USEs.
            BlockRenameVariables(block, pRenameState);

            // Assign arguments to the phi node of successors, corresponding to the block's index.
            AssignPhiNodeRhsVariables(block, pRenameState);

            // Recurse with the block's DOM children.
            BlkVector* domChildren = domTree->LookupPointer(block);
            if (domChildren != nullptr)
            {
                for (BasicBlock* child : *domChildren)
                {
                    DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables](pushing dom child " FMT_BB ")\n", child->bbNum);
                    blocksToDo->push_back(BlockWork(child));
                }
            }
        }
        else
        {
            // Done, pop all the stack count, if there is one for this block.
            BlockPopStacks(block, pRenameState);
            DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables] done with " FMT_BB "\n", block->bbNum);
        }
    }
}

#ifdef DEBUG
/**
 * Print the blocks, the phi nodes get printed as well.
 * @example:
 * After SSA BB02:
 *                [0027CC0C] -----------                 stmtExpr  void  (IL 0x019...0x01B)
 * N001 (  1,  1)       [0027CB70] -----------                 const     int    23
 * N003 (  3,  3)    [0027CBD8] -A------R--                 =         int
 * N002 (  1,  1)       [0027CBA4] D------N---                 lclVar    int    V01 arg1         d:5
 *
 * After SSA BB04:
 *                [0027D530] -----------                 stmtExpr  void  (IL   ???...  ???)
 * N002 (  0,  0)       [0027D4C8] -----------                 phi       int
 *                            [0027D8CC] -----------                 lclVar    int    V01 arg1         u:5
 *                            [0027D844] -----------                 lclVar    int    V01 arg1         u:4
 * N004 (  2,  2)    [0027D4FC] -A------R--                 =         int
 * N003 (  1,  1)       [0027D460] D------N---                 lclVar    int    V01 arg1         d:3
 */
void SsaBuilder::Print(BasicBlock** postOrder, int count)
{
    for (int i = count - 1; i >= 0; --i)
    {
        printf("After SSA " FMT_BB ":\n", postOrder[i]->bbNum);
        m_pCompiler->gtDispTreeList(postOrder[i]->bbTreeList);
    }
}
#endif // DEBUG

/**
 * Build SSA form.
 *
 * Sorts the graph topologically.
 *   - Collects them in postOrder array.
 *
 * Identifies each block's immediate dominator.
 *   - Computes this in bbIDom of each BasicBlock.
 *
 * Computes DOM tree relation.
 *   - Computes domTree as block -> set of blocks.
 *   - Computes pre/post order traversal of the DOM tree.
 *
 * Inserts phi nodes.
 *   - Computes dominance frontier as block -> set of blocks.
 *   - Allocates block use/def/livein/liveout and computes it.
 *   - Inserts phi nodes with only rhs at the beginning of the blocks.
 *
 * Renames variables.
 *   - Walks blocks in evaluation order and gives uses and defs names.
 *   - Gives empty phi nodes their rhs arguments as they become known while renaming.
 *
 * @return true if successful, for now, this must always be true.
 *
 * @see "A simple, fast dominance algorithm" by Keith D. Cooper, Timothy J. Harvey, Ken Kennedy.
 * @see Briggs, Cooper, Harvey and Simpson "Practical Improvements to the Construction
 *      and Destruction of Static Single Assignment Form."
 */
void SsaBuilder::Build()
{
#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        printf("*************** In SsaBuilder::Build()\n");
    }
#endif

    // Ensure that there's a first block outside a try, so that the dominator tree has a unique root.
    SetupBBRoot();

    // Just to keep block no. & index same add 1.
    int blockCount = m_pCompiler->fgBBNumMax + 1;

    JITDUMP("[SsaBuilder] Max block count is %d.\n", blockCount);

    // Allocate the postOrder array for the graph.

    BasicBlock** postOrder;

    if (blockCount > DEFAULT_MIN_OPTS_BB_COUNT)
    {
        postOrder = new (m_allocator) BasicBlock*[blockCount];
    }
    else
    {
        postOrder = (BasicBlock**)alloca(blockCount * sizeof(BasicBlock*));
    }

    m_visitedTraits = BitVecTraits(blockCount, m_pCompiler);
    m_visited       = BitVecOps::MakeEmpty(&m_visitedTraits);

    // Topologically sort the graph.
    int count = TopologicalSort(postOrder, blockCount);
    JITDUMP("[SsaBuilder] Topologically sorted the graph.\n");
    EndPhase(PHASE_BUILD_SSA_TOPOSORT);

    // Compute IDom(b).
    ComputeImmediateDom(postOrder, count);

    // Compute the dominator tree.
    BlkToBlkVectorMap* domTree = new (m_allocator) BlkToBlkVectorMap(m_allocator);
    ComputeDominators(postOrder, count, domTree);
    EndPhase(PHASE_BUILD_SSA_DOMS);

    // Compute liveness on the graph.
    m_pCompiler->fgLocalVarLiveness();
    EndPhase(PHASE_BUILD_SSA_LIVENESS);

    // Mark all variables that will be tracked by SSA
    for (unsigned lclNum = 0; lclNum < m_pCompiler->lvaCount; lclNum++)
    {
        m_pCompiler->lvaTable[lclNum].lvInSsa = IncludeInSsa(lclNum);
    }

    // Insert phi functions.
    InsertPhiFunctions(postOrder, count);

    // Rename local variables and collect UD information for each ssa var.
    SsaRenameState* pRenameState =
        new (m_allocator) SsaRenameState(m_allocator, m_pCompiler->lvaCount, m_pCompiler->byrefStatesMatchGcHeapStates);
    RenameVariables(domTree, pRenameState);
    EndPhase(PHASE_BUILD_SSA_RENAME);

#ifdef DEBUG
    // At this point we are in SSA form. Print the SSA form.
    if (m_pCompiler->verboseSsa)
    {
        Print(postOrder, count);
    }
#endif
}

void SsaBuilder::SetupBBRoot()
{
    // Allocate a bbroot, if necessary.
    // We need a unique block to be the root of the dominator tree.
    // This can be violated if the first block is in a try, or if it is the first block of
    // a loop (which would necessarily be an infinite loop) -- i.e., it has a predecessor.

    // If neither condition holds, no reason to make a new block.
    if (!m_pCompiler->fgFirstBB->hasTryIndex() && m_pCompiler->fgFirstBB->bbPreds == nullptr)
    {
        return;
    }

    BasicBlock* bbRoot = m_pCompiler->bbNewBasicBlock(BBJ_NONE);
    bbRoot->bbFlags |= BBF_INTERNAL;

    // May need to fix up preds list, so remember the old first block.
    BasicBlock* oldFirst = m_pCompiler->fgFirstBB;

    // Copy the liveness information from the first basic block.
    if (m_pCompiler->fgLocalVarLivenessDone)
    {
        VarSetOps::Assign(m_pCompiler, bbRoot->bbLiveIn, oldFirst->bbLiveIn);
        VarSetOps::Assign(m_pCompiler, bbRoot->bbLiveOut, oldFirst->bbLiveIn);
    }

    // Copy the bbWeight.  (This is technically wrong, if the first block is a loop head, but
    // it shouldn't matter...)
    bbRoot->inheritWeight(oldFirst);

    // There's an artifical incoming reference count for the first BB.  We're about to make it no longer
    // the first BB, so decrement that.
    assert(oldFirst->bbRefs > 0);
    oldFirst->bbRefs--;

    m_pCompiler->fgInsertBBbefore(m_pCompiler->fgFirstBB, bbRoot);

    assert(m_pCompiler->fgFirstBB == bbRoot);
    if (m_pCompiler->fgComputePredsDone)
    {
        m_pCompiler->fgAddRefPred(oldFirst, bbRoot);
    }
}

//------------------------------------------------------------------------
// IncludeInSsa: Check if the specified variable can be included in SSA.
//
// Arguments:
//    lclNum - the variable number
//
// Return Value:
//    true if the variable is included in SSA
//
bool SsaBuilder::IncludeInSsa(unsigned lclNum)
{
    LclVarDsc* varDsc = &m_pCompiler->lvaTable[lclNum];

    if (varDsc->lvAddrExposed)
    {
        return false; // We exclude address-exposed variables.
    }
    if (!varDsc->lvTracked)
    {
        return false; // SSA is only done for tracked variables
    }
    // lvPromoted structs are never tracked...
    assert(!varDsc->lvPromoted);

    if (varDsc->lvOverlappingFields)
    {
        return false; // Don't use SSA on structs that have overlapping fields
    }

    if (varDsc->lvIsStructField &&
        (m_pCompiler->lvaGetParentPromotionType(lclNum) != Compiler::PROMOTION_TYPE_INDEPENDENT))
    {
        // SSA must exclude struct fields that are not independent
        // - because we don't model the struct assignment properly when multiple fields can be assigned by one struct
        //   assignment.
        // - SSA doesn't allow a single node to contain multiple SSA definitions.
        // - and PROMOTION_TYPE_DEPENDEDNT fields  are never candidates for a register.
        //
        // Example mscorlib method: CompatibilitySwitches:IsCompatibilitySwitchSet
        //
        return false;
    }
    // otherwise this variable is included in SSA
    return true;
}

#ifdef DEBUG
// This method asserts that SSA name constraints specified are satisfied.
void Compiler::JitTestCheckSSA()
{
    struct SSAName
    {
        unsigned m_lvNum;
        unsigned m_ssaNum;

        static unsigned GetHashCode(SSAName ssaNm)
        {
            return ssaNm.m_lvNum << 16 | ssaNm.m_ssaNum;
        }

        static bool Equals(SSAName ssaNm1, SSAName ssaNm2)
        {
            return ssaNm1.m_lvNum == ssaNm2.m_lvNum && ssaNm1.m_ssaNum == ssaNm2.m_ssaNum;
        }
    };

    typedef JitHashTable<ssize_t, JitSmallPrimitiveKeyFuncs<ssize_t>, SSAName> LabelToSSANameMap;
    typedef JitHashTable<SSAName, SSAName, ssize_t>                            SSANameToLabelMap;

    // If we have no test data, early out.
    if (m_nodeTestData == nullptr)
    {
        return;
    }

    NodeToTestDataMap* testData = GetNodeTestData();

    // First we have to know which nodes in the tree are reachable.
    NodeToIntMap* reachable = FindReachableNodesInNodeTestData();

    LabelToSSANameMap* labelToSSA = new (getAllocatorDebugOnly()) LabelToSSANameMap(getAllocatorDebugOnly());
    SSANameToLabelMap* ssaToLabel = new (getAllocatorDebugOnly()) SSANameToLabelMap(getAllocatorDebugOnly());

    if (verbose)
    {
        printf("\nJit Testing: SSA names.\n");
    }
    for (NodeToTestDataMap::KeyIterator ki = testData->Begin(); !ki.Equal(testData->End()); ++ki)
    {
        TestLabelAndNum tlAndN;
        GenTree*        node = ki.Get();
        bool            b    = testData->Lookup(node, &tlAndN);
        assert(b);
        if (tlAndN.m_tl == TL_SsaName)
        {
            if (node->OperGet() != GT_LCL_VAR)
            {
                printf("SSAName constraint put on non-lcl-var expression ");
                printTreeID(node);
                printf(" (of type %s).\n", varTypeName(node->TypeGet()));
                unreached();
            }
            GenTreeLclVarCommon* lcl = node->AsLclVarCommon();

            int dummy;
            if (!reachable->Lookup(lcl, &dummy))
            {
                printf("Node ");
                printTreeID(lcl);
                printf(" had a test constraint declared, but has become unreachable at the time the constraint is "
                       "tested.\n"
                       "(This is probably as a result of some optimization -- \n"
                       "you may need to modify the test case to defeat this opt.)\n");
                unreached();
            }

            if (verbose)
            {
                printf("  Node: ");
                printTreeID(lcl);
                printf(", SSA name = <%d, %d> -- SSA name class %d.\n", lcl->gtLclNum, lcl->gtSsaNum, tlAndN.m_num);
            }
            SSAName ssaNm;
            if (labelToSSA->Lookup(tlAndN.m_num, &ssaNm))
            {
                if (verbose)
                {
                    printf("      Already in hash tables.\n");
                }
                // The mapping(s) must be one-to-one: if the label has a mapping, then the ssaNm must, as well.
                ssize_t num2;
                bool    b = ssaToLabel->Lookup(ssaNm, &num2);
                // And the mappings must be the same.
                if (tlAndN.m_num != num2)
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->gtLclNum, lcl->gtSsaNum,
                           tlAndN.m_num);
                    printf(
                        "but this SSA name <%d,%d> has already been associated with a different SSA name class: %d.\n",
                        ssaNm.m_lvNum, ssaNm.m_ssaNum, num2);
                    unreached();
                }
                // And the current node must be of the specified SSA family.
                if (!(lcl->gtLclNum == ssaNm.m_lvNum && lcl->gtSsaNum == ssaNm.m_ssaNum))
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->gtLclNum, lcl->gtSsaNum,
                           tlAndN.m_num);
                    printf("but that name class was previously bound to a different SSA name: <%d,%d>.\n",
                           ssaNm.m_lvNum, ssaNm.m_ssaNum);
                    unreached();
                }
            }
            else
            {
                ssaNm.m_lvNum  = lcl->gtLclNum;
                ssaNm.m_ssaNum = lcl->gtSsaNum;
                ssize_t num;
                // The mapping(s) must be one-to-one: if the label has no mapping, then the ssaNm may not, either.
                if (ssaToLabel->Lookup(ssaNm, &num))
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->gtLclNum, lcl->gtSsaNum,
                           tlAndN.m_num);
                    printf("but this SSA name has already been associated with a different name class: %d.\n", num);
                    unreached();
                }
                // Add to both mappings.
                labelToSSA->Set(tlAndN.m_num, ssaNm);
                ssaToLabel->Set(ssaNm, tlAndN.m_num);
                if (verbose)
                {
                    printf("      added to hash tables.\n");
                }
            }
        }
    }
}
#endif // DEBUG
