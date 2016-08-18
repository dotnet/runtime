// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//

//
// ==--==

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  SSA                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "ssaconfig.h"
#include "ssarenamestate.h"
#include "ssabuilder.h"

namespace
{
/**
 * Visits basic blocks in the depth first order and arranges them in the order of
 * their DFS finish time.
 *
 * @param block The fgFirstBB or entry block.
 * @param comp A pointer to compiler.
 * @param visited In pointer initialized to false and of size at least fgMaxBBNum.
 * @param count Out pointer for count of all nodes reachable by DFS.
 * @param postOrder Out poitner to arrange the blocks and of size at least fgMaxBBNum.
 */
static void TopologicalSortHelper(BasicBlock* block, Compiler* comp, bool* visited, int* count, BasicBlock** postOrder)
{
    visited[block->bbNum] = true;

    ArrayStack<BasicBlock*>      blocks(comp);
    ArrayStack<AllSuccessorIter> iterators(comp);
    ArrayStack<AllSuccessorIter> ends(comp);

    // there are three stacks used here and all should be same height
    // the first is for blocks
    // the second is the iterator to keep track of what succ of the block we are looking at
    // and the third is the end marker iterator
    blocks.Push(block);
    iterators.Push(block->GetAllSuccs(comp).begin());
    ends.Push(block->GetAllSuccs(comp).end());

    while (blocks.Height() > 0)
    {
        block = blocks.Top();

#ifdef DEBUG
        if (comp->verboseSsa)
        {
            printf("[SsaBuilder::TopologicalSortHelper] Visiting BB%02u: ", block->bbNum);
            printf("[");
            unsigned numSucc = block->NumSucc(comp);
            for (unsigned i = 0; i < numSucc; ++i)
            {
                printf("BB%02u, ", block->GetSucc(i, comp)->bbNum);
            }
            EHSuccessorIter end = block->GetEHSuccs(comp).end();
            for (EHSuccessorIter ehsi = block->GetEHSuccs(comp).begin(); ehsi != end; ++ehsi)
            {
                printf("[EH]BB%02u, ", (*ehsi)->bbNum);
            }
            printf("]\n");
        }
#endif

        if (iterators.TopRef() != ends.TopRef())
        {
            // if the block on TOS still has unreached successors, visit them
            AllSuccessorIter& iter = iterators.TopRef();
            BasicBlock*       succ = *iter;
            ++iter;
            // push the child

            if (!visited[succ->bbNum])
            {
                blocks.Push(succ);
                iterators.Push(succ->GetAllSuccs(comp).begin());
                ends.Push(succ->GetAllSuccs(comp).end());
                visited[succ->bbNum] = true;
            }
        }
        else
        {
            // all successors have been visited
            blocks.Pop();
            iterators.Pop();
            ends.Pop();

            postOrder[*count]     = block;
            block->bbPostOrderNum = *count;
            *count += 1;

            DBG_SSA_JITDUMP("postOrder[%d] = [%p] and BB%02u\n", *count, dspPtr(block), block->bbNum);
        }
    }
}

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
    IAllocator* pIAllocator = new (this, CMK_SSA) CompAllocator(this, CMK_SSA);

    // If this is not the first invocation, reset data structures for SSA.
    if (fgSsaPassesCompleted > 0)
    {
        fgResetForSsa();
    }

    SsaBuilder builder(this, pIAllocator);
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
    for (BasicBlock* blk = fgFirstBB; blk != nullptr; blk = blk->bbNext)
    {
        // Eliminate phis.
        blk->bbHeapSsaPhiFunc = nullptr;
        if (blk->bbTreeList != nullptr)
        {
            GenTreePtr last = blk->bbTreeList->gtPrev;
            blk->bbTreeList = blk->FirstNonPhiDef();
            if (blk->bbTreeList != nullptr)
            {
                blk->bbTreeList->gtPrev = last;
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
SsaBuilder::SsaBuilder(Compiler* pCompiler, IAllocator* pIAllocator)
    : m_pCompiler(pCompiler)
    , m_allocator(pIAllocator)

#ifdef SSA_FEATURE_DOMARR
    , m_pDomPreOrder(NULL)
    , m_pDomPostOrder(NULL)
#endif
#ifdef SSA_FEATURE_USEDEF
    , m_uses(jitstd::allocator<void>(pIAllocator))
    , m_defs(jitstd::allocator<void>(pIAllocator))
#endif
{
}

/**
 *  Topologically sort the graph and return the number of nodes visited.
 *
 *  @param postOrder The array in which the arranged basic blocks have to be returned.
 *  @param count The size of the postOrder array.
 *
 *  @return The number of nodes visited while performing DFS on the graph.
 */
int SsaBuilder::TopologicalSort(BasicBlock** postOrder, int count)
{
    // Allocate and initialize visited flags.
    bool* visited = (bool*)alloca(count * sizeof(bool));
    memset(visited, 0, count * sizeof(bool));

    // Display basic blocks.
    DBEXEC(VERBOSE, m_pCompiler->fgDispBasicBlocks());
    DBEXEC(VERBOSE, m_pCompiler->fgDispHandlerTab());

    // Call the recursive helper.
    int postIndex = 0;
    TopologicalSortHelper(m_pCompiler->fgFirstBB, m_pCompiler, visited, &postIndex, postOrder);

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

    // Add entry point to processed as its IDom is NULL.
    BitVecTraits traits(m_pCompiler->fgBBNumMax + 1, m_pCompiler);
    BitVec       BITVEC_INIT_NOCOPY(processed, BitVecOps::MakeEmpty(&traits));

    BitVecOps::AddElemD(&traits, processed, m_pCompiler->fgFirstBB->bbNum);
    assert(postOrder[count - 1] == m_pCompiler->fgFirstBB);

    bool changed = true;
    while (changed)
    {
        changed = false;

        // In reverse post order, except for the entry block (count - 1 is entry BB).
        for (int i = count - 2; i >= 0; --i)
        {
            BasicBlock* block = postOrder[i];

            DBG_SSA_JITDUMP("Visiting in reverse post order: BB%02u.\n", block->bbNum);

            // Find the first processed predecessor block.
            BasicBlock* predBlock = nullptr;
            for (flowList* pred = m_pCompiler->BlockPredsWithEH(block); pred; pred = pred->flNext)
            {
                if (BitVecOps::IsMember(&traits, processed, pred->flBlock->bbNum))
                {
                    predBlock = pred->flBlock;
                    break;
                }
            }

            // There could just be a single basic block, so just check if there were any preds.
            if (predBlock != nullptr)
            {
                DBG_SSA_JITDUMP("Pred block is BB%02u.\n", predBlock->bbNum);
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
                DBG_SSA_JITDUMP("bbIDom of BB%02u becomes BB%02u.\n", block->bbNum, bbIDom ? bbIDom->bbNum : 0);
                block->bbIDom = bbIDom;
            }

            // Mark the current block as processed.
            BitVecOps::AddElemD(&traits, processed, block->bbNum);

            DBG_SSA_JITDUMP("Marking block BB%02u as processed.\n", block->bbNum);
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
void SsaBuilder::DomTreeWalk(BasicBlock* curBlock, BlkToBlkSetMap* domTree, int* preIndex, int* postIndex)
{
    JITDUMP("[SsaBuilder::DomTreeWalk] block [%p], BB%02u:\n", dspPtr(curBlock), curBlock->bbNum);

    // Store the order number at the block number in the pre order list.
    m_pDomPreOrder[curBlock->bbNum] = *preIndex;
    ++(*preIndex);

    BlkSet* pBlkSet;
    if (domTree->Lookup(curBlock, &pBlkSet))
    {
        for (BlkSet::KeyIterator ki = pBlkSet->Begin(); !ki.Equal(pBlkSet->End()); ++ki)
        {
            if (curBlock != ki.Get())
            {
                DomTreeWalk(ki.Get(), domTree, preIndex, postIndex);
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
void SsaBuilder::ConstructDomTreeForBlock(Compiler* pCompiler, BasicBlock* block, BlkToBlkSetMap* domTree)
{
    BasicBlock* bbIDom = block->bbIDom;

    // bbIDom for (only) fgFirstBB will be NULL.
    if (bbIDom == nullptr)
    {
        return;
    }

    // If the bbIDom map key doesn't exist, create one.
    BlkSet* pBlkSet;
    if (!domTree->Lookup(bbIDom, &pBlkSet))
    {
        pBlkSet = new (pCompiler->getAllocator()) BlkSet(pCompiler->getAllocator());
        domTree->Set(bbIDom, pBlkSet);
    }

    DBG_SSA_JITDUMP("Inserting BB%02u as dom child of BB%02u.\n", block->bbNum, bbIDom->bbNum);
    // Insert the block into the block's set.
    pBlkSet->Set(block, true);
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
void SsaBuilder::ComputeDominators(Compiler* pCompiler, BlkToBlkSetMap* domTree)
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
void SsaBuilder::ComputeDominators(BasicBlock** postOrder, int count, BlkToBlkSetMap* domTree)
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
    m_pDomPreOrder  = jitstd::utility::allocate<int>(m_allocator, bbArrSize);
    m_pDomPostOrder = jitstd::utility::allocate<int>(m_allocator, bbArrSize);

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
void SsaBuilder::DisplayDominators(BlkToBlkSetMap* domTree)
{
    printf("After computing dominator tree: \n");
    for (BlkToBlkSetMap::KeyIterator nodes = domTree->Begin(); !nodes.Equal(domTree->End()); ++nodes)
    {
        printf("BB%02u := {", nodes.Get()->bbNum);

        BlkSet* pBlkSet = nodes.GetValue();
        for (BlkSet::KeyIterator ki = pBlkSet->Begin(); !ki.Equal(pBlkSet->End()); ++ki)
        {
            if (!ki.Equal(pBlkSet->Begin()))
            {
                printf(",");
            }
            printf("BB%02u", ki.Get()->bbNum);
        }
        printf("}\n");
    }
}

#endif // DEBUG

// (Spec comment at declaration.)
// See "A simple, fast dominance algorithm", by Cooper, Harvey, and Kennedy.
// First we compute the dominance frontier for each block, then we convert these to iterated
// dominance frontiers by a closure operation.
BlkToBlkSetMap* SsaBuilder::ComputeIteratedDominanceFrontier(BasicBlock** postOrder, int count)
{
    BlkToBlkSetMap* frontier = new (m_pCompiler->getAllocator()) BlkToBlkSetMap(m_pCompiler->getAllocator());

    DBG_SSA_JITDUMP("Computing IDF: First computing DF.\n");

    for (int i = 0; i < count; ++i)
    {
        BasicBlock* block = postOrder[i];

        DBG_SSA_JITDUMP("Considering block BB%02u.\n", block->bbNum);

        // Recall that B3 is in the dom frontier of B1 if there exists a B2
        // such that B1 dom B2, !(B1 dom B3), and B3 is an immediate successor
        // of B2.  (Note that B1 might be the same block as B2.)
        // In that definition, we're considering "block" to be B3, and trying
        // to find B1's.  To do so, first we consider the predecessors of "block",
        // searching for candidate B2's -- "block" is obviously an immediate successor
        // of its immediate predecessors.  If there are zero or one preds, then there
        // is no pred, or else the single pred dominates "block", so no B2 exists.

        flowList* blockPreds = m_pCompiler->BlockPredsWithEH(block);

        // If block has more 0/1 predecessor, skip.
        if (blockPreds == nullptr || blockPreds->flNext == nullptr)
        {
            DBG_SSA_JITDUMP("   Has %d preds; skipping.\n", blockPreds == nullptr ? 0 : 1);
            continue;
        }

        // Otherwise, there are > 1 preds.  Each is a candidate B2 in the definition --
        // *unless* it dominates "block"/B3.

        for (flowList* pred = blockPreds; pred; pred = pred->flNext)
        {
            DBG_SSA_JITDUMP("   Considering predecessor BB%02u.\n", pred->flBlock->bbNum);

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
                DBG_SSA_JITDUMP("      Adding BB%02u to dom frontier of pred dom BB%02u.\n", block->bbNum, b1->bbNum);
                BlkSet* pBlkSet;
                if (!frontier->Lookup(b1, &pBlkSet))
                {
                    pBlkSet = new (m_pCompiler->getAllocator()) BlkSet(m_pCompiler->getAllocator());
                    frontier->Set(b1, pBlkSet);
                }
                pBlkSet->Set(block, true);
            }
        }
    }

#ifdef DEBUG
    if (m_pCompiler->verboseSsa)
    {
        printf("\nComputed DF:\n");
        for (int i = 0; i < count; ++i)
        {
            BasicBlock* block = postOrder[i];
            printf("Block BB%02u := {", block->bbNum);

            bool    first = true;
            BlkSet* blkDf;
            if (frontier->Lookup(block, &blkDf))
            {
                for (BlkSet::KeyIterator blkDfIter = blkDf->Begin(); !blkDfIter.Equal(blkDf->End()); blkDfIter++)
                {
                    if (!first)
                    {
                        printf(",");
                    }
                    printf("BB%02u", blkDfIter.Get()->bbNum);
                    first = false;
                }
            }
            printf("}\n");
        }
    }
#endif

    // Now do the closure operation to make the dominance frontier into an IDF.
    // There's probably a better way to do this...
    BlkToBlkSetMap* idf = new (m_pCompiler->getAllocator()) BlkToBlkSetMap(m_pCompiler->getAllocator());
    for (BlkToBlkSetMap::KeyIterator kiFrontBlks = frontier->Begin(); !kiFrontBlks.Equal(frontier->End());
         kiFrontBlks++)
    {
        // Create IDF(b)
        BlkSet* blkIdf = new (m_pCompiler->getAllocator()) BlkSet(m_pCompiler->getAllocator());
        idf->Set(kiFrontBlks.Get(), blkIdf);

        // Keep track of what got newly added to the IDF, so we can go after their DFs.
        BlkSet* delta = new (m_pCompiler->getAllocator()) BlkSet(m_pCompiler->getAllocator());
        delta->Set(kiFrontBlks.Get(), true);

        // Now transitively add DF+(delta) to IDF(b), each step gathering new "delta."
        while (delta->GetCount() > 0)
        {
            // Extract a block x to be worked on.
            BlkSet::KeyIterator ki     = delta->Begin();
            BasicBlock*         curBlk = ki.Get();
            // TODO-Cleanup: Remove(ki) doesn't work correctly in SimplerHash.
            delta->Remove(curBlk);

            // Get DF(x).
            BlkSet* blkDf;
            if (frontier->Lookup(curBlk, &blkDf))
            {
                // Add DF(x) to IDF(b) and update "delta" i.e., new additions to IDF(b).
                for (BlkSet::KeyIterator ki = blkDf->Begin(); !ki.Equal(blkDf->End()); ki++)
                {
                    if (!blkIdf->Lookup(ki.Get()))
                    {
                        delta->Set(ki.Get(), true);
                        blkIdf->Set(ki.Get(), true);
                    }
                }
            }
        }
    }

#ifdef DEBUG
    if (m_pCompiler->verboseSsa)
    {
        printf("\nComputed IDF:\n");
        for (int i = 0; i < count; ++i)
        {
            BasicBlock* block = postOrder[i];
            printf("Block BB%02u := {", block->bbNum);

            bool    first = true;
            BlkSet* blkIdf;
            if (idf->Lookup(block, &blkIdf))
            {
                for (BlkSet::KeyIterator ki = blkIdf->Begin(); !ki.Equal(blkIdf->End()); ki++)
                {
                    if (!first)
                    {
                        printf(",");
                    }
                    printf("BB%02u", ki.Get()->bbNum);
                    first = false;
                }
            }
            printf("}\n");
        }
    }
#endif

    return idf;
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
    for (GenTreePtr stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
    {
        // A prefix of the statements of the block are phi definition nodes. If we complete processing
        // that prefix, exit.
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

        GenTreePtr phiLhs = tree->gtOp.gtOp1;
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

    // Compute liveness on the graph.
    m_pCompiler->fgLocalVarLiveness();
    EndPhase(PHASE_BUILD_SSA_LIVENESS);

    // Compute dominance frontier.
    BlkToBlkSetMap* frontier = ComputeIteratedDominanceFrontier(postOrder, count);
    EndPhase(PHASE_BUILD_SSA_IDF);

    JITDUMP("Inserting phi functions:\n");

    for (int i = 0; i < count; ++i)
    {
        BasicBlock* block = postOrder[i];
        DBG_SSA_JITDUMP("Considering dominance frontier of block BB%02u:\n", block->bbNum);

        // If the block's dominance frontier is empty, go on to the next block.
        BlkSet* blkIdf;
        if (!frontier->Lookup(block, &blkIdf))
        {
            continue;
        }

        // For each local var number "lclNum" that "block" assigns to...
        VARSET_ITER_INIT(m_pCompiler, defVars, block->bbVarDef, varIndex);
        while (defVars.NextElem(m_pCompiler, &varIndex))
        {
            unsigned lclNum = m_pCompiler->lvaTrackedToVarNum[varIndex];
            DBG_SSA_JITDUMP("  Considering local var V%02u:\n", lclNum);

            if (m_pCompiler->fgExcludeFromSsa(lclNum))
            {
                DBG_SSA_JITDUMP("  Skipping because it is excluded.\n");
                continue;
            }

            // For each block "bbInDomFront" that is in the dominance frontier of "block"...
            for (BlkSet::KeyIterator iterBlk = blkIdf->Begin(); !iterBlk.Equal(blkIdf->End()); ++iterBlk)
            {
                BasicBlock* bbInDomFront = iterBlk.Get();
                DBG_SSA_JITDUMP("     Considering BB%02u in dom frontier of BB%02u:\n", bbInDomFront->bbNum,
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
                    JITDUMP("Inserting phi definition for V%02u at start of BB%02u.\n", lclNum, bbInDomFront->bbNum);

                    GenTreePtr phiLhs = m_pCompiler->gtNewLclvNode(lclNum, m_pCompiler->lvaTable[lclNum].TypeGet());

                    // Create 'phiRhs' as a GT_PHI node for 'lclNum', it will eventually hold a GT_LIST of GT_PHI_ARG
                    // nodes. However we have to construct this list so for now the gtOp1 of 'phiRhs' is a nullptr.
                    // It will get replaced with a GT_LIST of GT_PHI_ARG nodes in
                    // SsaBuilder::AssignPhiNodeRhsVariables() and in SsaBuilder::AddDefToHandlerPhis()

                    GenTreePtr phiRhs =
                        m_pCompiler->gtNewOperNode(GT_PHI, m_pCompiler->lvaTable[lclNum].TypeGet(), nullptr);

                    GenTreePtr phiAsg = m_pCompiler->gtNewAssignNode(phiLhs, phiRhs);

                    GenTreePtr stmt = m_pCompiler->fgInsertStmtAtBeg(bbInDomFront, phiAsg);
                    m_pCompiler->gtSetStmtInfo(stmt);
                    m_pCompiler->fgSetStmtSeq(stmt);
                }
            }
        }

        // Now make a similar phi definition if the block defines Heap.
        if (block->bbHeapDef)
        {
            // For each block "bbInDomFront" that is in the dominance frontier of "block".
            for (BlkSet::KeyIterator iterBlk = blkIdf->Begin(); !iterBlk.Equal(blkIdf->End()); ++iterBlk)
            {
                BasicBlock* bbInDomFront = iterBlk.Get();
                DBG_SSA_JITDUMP("     Considering BB%02u in dom frontier of BB%02u for Heap phis:\n",
                                bbInDomFront->bbNum, block->bbNum);

                // Check if Heap is live into block "*iterBlk".
                if (!bbInDomFront->bbHeapLiveIn)
                {
                    continue;
                }

                // Check if we've already inserted a phi node.
                if (bbInDomFront->bbHeapSsaPhiFunc == nullptr)
                {
                    // We have a variable i that is defined in block j and live at l, and l belongs to dom frontier of
                    // j. So insert a phi node at l.
                    JITDUMP("Inserting phi definition for Heap at start of BB%02u.\n", bbInDomFront->bbNum);
                    bbInDomFront->bbHeapSsaPhiFunc = BasicBlock::EmptyHeapPhiDef;
                }
            }
        }
    }
    EndPhase(PHASE_BUILD_SSA_INSERT_PHIS);
}

#ifdef SSA_FEATURE_USEDEF
/**
 * Record a use point of a variable.
 *
 * The use point is just the tree that is a local variable use.
 *
 * @param tree Tree node where an SSA variable is used.
 *
 * @remarks The result is in the m_uses map :: [lclNum, ssaNum] -> tree.
 */
void SsaBuilder::AddUsePoint(GenTree* tree)
{
    assert(tree->IsLocal());
    SsaVarName          key(tree->gtLclVarCommon.gtLclNum, tree->gtLclVarCommon.gtSsaNum);
    VarToUses::iterator iter = m_uses.find(key);
    if (iter == m_uses.end())
    {
        iter = m_uses.insert(key, VarToUses::mapped_type(m_uses.get_allocator()));
    }
    (*iter).second.push_back(tree);
}
#endif // !SSA_FEATURE_USEDEF

/**
 * Record a def point of a variable.
 *
 * The def point is just the tree that is a local variable def.
 *
 * @param tree Tree node where an SSA variable is def'ed.
 *
 * @remarks The result is in the m_defs map :: [lclNum, ssaNum] -> tree.
 */
void SsaBuilder::AddDefPoint(GenTree* tree, BasicBlock* blk)
{
    Compiler::IndirectAssignmentAnnotation* pIndirAnnot;
    // In the case of an "indirect assignment", where the LHS is IND of a byref to the local actually being assigned,
    // we make the ASG tree the def point.
    assert(tree->IsLocal() || IsIndirectAssign(tree, &pIndirAnnot));
    unsigned lclNum;
    unsigned defSsaNum;
    if (tree->IsLocal())
    {
        lclNum    = tree->gtLclVarCommon.gtLclNum;
        defSsaNum = m_pCompiler->GetSsaNumForLocalVarDef(tree);
    }
    else
    {
        bool b = m_pCompiler->GetIndirAssignMap()->Lookup(tree, &pIndirAnnot);
        assert(b);
        lclNum    = pIndirAnnot->m_lclNum;
        defSsaNum = pIndirAnnot->m_defSsaNum;
    }
#ifdef DEBUG
    // Record that there's a new SSA def.
    m_pCompiler->lvaTable[lclNum].lvNumSsaNames++;
#endif
    // Record where the defn happens.
    LclSsaVarDsc* ssaDef    = m_pCompiler->lvaTable[lclNum].GetPerSsaData(defSsaNum);
    ssaDef->m_defLoc.m_blk  = blk;
    ssaDef->m_defLoc.m_tree = tree;

#ifdef SSA_FEATURE_USEDEF
    SsaVarName         key(lclNum, defSsaNum);
    VarToDef::iterator iter = m_defs.find(key);
    if (iter == m_defs.end())
    {
        iter = m_defs.insert(key, tree);
        return;
    }
    // There can only be a single definition for an SSA var.
    unreached();
#endif
}

bool SsaBuilder::IsIndirectAssign(GenTreePtr tree, Compiler::IndirectAssignmentAnnotation** ppIndirAssign)
{
    return tree->OperGet() == GT_ASG && m_pCompiler->m_indirAssignMap != nullptr &&
           m_pCompiler->GetIndirAssignMap()->Lookup(tree, ppIndirAssign);
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
    if (tree->OperIsAssignment())
    {
        GenTreePtr lhs     = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
        GenTreePtr trueLhs = lhs->gtEffectiveVal(/*commaOnly*/ true);
        if (trueLhs->OperIsIndir())
        {
            trueLhs->gtFlags |= GTF_IND_ASG_LHS;
        }
        else if (trueLhs->OperGet() == GT_CLS_VAR)
        {
            trueLhs->gtFlags |= GTF_CLS_VAR_ASG_LHS;
        }
    }

    // Figure out if "tree" may make a new heap state (if we care for this block).
    if (!block->bbHeapHavoc)
    {
        if (tree->OperIsAssignment() || tree->OperIsBlkOp())
        {
            if (m_pCompiler->ehBlockHasExnFlowDsc(block))
            {
                GenTreeLclVarCommon* lclVarNode;
                if (!tree->DefinesLocal(m_pCompiler, &lclVarNode))
                {
                    // It *may* define the heap in a non-havoc way.  Make a new SSA # -- associate with this node.
                    unsigned count = pRenameState->CountForHeapDef();
                    pRenameState->PushHeap(block, count);
                    m_pCompiler->GetHeapSsaMap()->Set(tree, count);
#ifdef DEBUG
                    if (JitTls::GetCompiler()->verboseSsa)
                    {
                        printf("Node ");
                        Compiler::printTreeID(tree);
                        printf(" (in try block) may define heap; ssa # = %d.\n", count);
                    }
#endif // DEBUG

                    // Now add this SSA # to all phis of the reachable catch blocks.
                    AddHeapDefToHandlerPhis(block, count);
                }
            }
        }
    }

    Compiler::IndirectAssignmentAnnotation* pIndirAssign = nullptr;
    if (!tree->IsLocal() && !IsIndirectAssign(tree, &pIndirAssign))
    {
        return;
    }

    if (pIndirAssign != nullptr)
    {
        unsigned lclNum = pIndirAssign->m_lclNum;
        // Is this a variable we exclude from SSA?
        if (m_pCompiler->fgExcludeFromSsa(lclNum))
        {
            pIndirAssign->m_defSsaNum = SsaConfig::RESERVED_SSA_NUM;
            return;
        }
        // Otherwise...
        if (!pIndirAssign->m_isEntire)
        {
            pIndirAssign->m_useSsaNum = pRenameState->CountForUse(lclNum);
        }
        unsigned count            = pRenameState->CountForDef(lclNum);
        pIndirAssign->m_defSsaNum = count;
        pRenameState->Push(block, lclNum, count);
        AddDefPoint(tree, block);
    }
    else
    {
        unsigned lclNum = tree->gtLclVarCommon.gtLclNum;
        // Is this a variable we exclude from SSA?
        if (m_pCompiler->fgExcludeFromSsa(lclNum))
        {
            tree->gtLclVarCommon.SetSsaNum(SsaConfig::RESERVED_SSA_NUM);
            return;
        }

        if (tree->gtFlags & GTF_VAR_DEF)
        {
            if (tree->gtFlags & GTF_VAR_USEASG)
            {
                // This the "x" in something like "x op= y"; it is both a use (first), then a def.
                // The def will define a new SSA name, and record that in "x".  If we need the SSA
                // name of the use, we record it in a map reserved for that purpose.
                unsigned count = pRenameState->CountForUse(lclNum);
                tree->gtLclVarCommon.SetSsaNum(count);
#ifdef SSA_FEATURE_USEDEF
                AddUsePoint(tree);
#endif
            }

            // Give a count and increment.
            unsigned count = pRenameState->CountForDef(lclNum);
            if (tree->gtFlags & GTF_VAR_USEASG)
            {
                m_pCompiler->GetOpAsgnVarDefSsaNums()->Set(tree, count);
            }
            else
            {
                tree->gtLclVarCommon.SetSsaNum(count);
            }
            pRenameState->Push(block, lclNum, count);
            AddDefPoint(tree, block);

            // If necessary, add "lclNum/count" to the arg list of a phi def in any
            // handlers for try blocks that "block" is within.  (But only do this for "real" definitions,
            // not phi definitions.)
            if (!isPhiDefn)
            {
                AddDefToHandlerPhis(block, lclNum, count);
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
#ifdef SSA_FEATURE_USEDEF
            AddUsePoint(tree);
#endif
        }
    }
}

void SsaBuilder::AddDefToHandlerPhis(BasicBlock* block, unsigned lclNum, unsigned count)
{
    assert(m_pCompiler->lvaTable[lclNum].lvTracked); // Precondition.
    unsigned lclIndex = m_pCompiler->lvaTable[lclNum].lvVarIndex;

    EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
    if (tryBlk != nullptr)
    {
        DBG_SSA_JITDUMP(
            "Definition of local V%02u/d:%d in block BB%02u has exn handler; adding as phi arg to handlers.\n", lclNum,
            count, block->bbNum);
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
                for (GenTreePtr stmt = handler->bbTreeList; stmt; stmt = stmt->gtNext)
                {
                    // If the tree is not an SSA def, break out of the loop: we're done.
                    if (!stmt->IsPhiDefnStmt())
                    {
                        break;
                    }

                    GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

                    assert(tree->IsPhiDefn());

                    if (tree->gtOp.gtOp1->gtLclVar.gtLclNum == lclNum)
                    {
                        // It's the definition for the right local.  Add "count" to the RHS.
                        GenTreePtr      phi  = tree->gtOp.gtOp2;
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
                        DBG_SSA_JITDUMP("   Added phi arg u:%d for V%02u to phi defn in handler block BB%02u.\n", count,
                                        lclNum, handler->bbNum);
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

void SsaBuilder::AddHeapDefToHandlerPhis(BasicBlock* block, unsigned count)
{
    if (m_pCompiler->ehBlockHasExnFlowDsc(block))
    {
        // Don't do anything for a compiler-inserted BBJ_ALWAYS that is a "leave helper".
        if (block->bbJumpKind == BBJ_ALWAYS && (block->bbFlags & BBF_INTERNAL) && (block->bbPrev->isBBCallAlwaysPair()))
        {
            return;
        }

        // Otherwise...
        DBG_SSA_JITDUMP("Definition of Heap/d:%d in block BB%02u has exn handler; adding as phi arg to handlers.\n",
                        count, block->bbNum);
        EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
        while (true)
        {
            BasicBlock* handler = tryBlk->ExFlowBlock();

            // Is Heap live on entry to the handler?
            if (handler->bbHeapLiveIn)
            {
                assert(handler->bbHeapSsaPhiFunc != nullptr);

                // Add "count" to the phi args of Heap.
                if (handler->bbHeapSsaPhiFunc == BasicBlock::EmptyHeapPhiDef)
                {
                    handler->bbHeapSsaPhiFunc = new (m_pCompiler) BasicBlock::HeapPhiArg(count);
                }
                else
                {
#ifdef DEBUG
                    BasicBlock::HeapPhiArg* curArg = handler->bbHeapSsaPhiFunc;
                    while (curArg != nullptr)
                    {
                        assert(curArg->GetSsaNum() != count);
                        curArg = curArg->m_nextArg;
                    }
#endif // DEBUG
                    handler->bbHeapSsaPhiFunc =
                        new (m_pCompiler) BasicBlock::HeapPhiArg(count, handler->bbHeapSsaPhiFunc);
                }

                DBG_SSA_JITDUMP("   Added phi arg u:%d for Heap to phi defn in handler block BB%02u.\n", count,
                                handler->bbNum);
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

    // First handle the incoming Heap state.

    // Is there an Phi definition for heap at the start of this block?
    if (block->bbHeapSsaPhiFunc != nullptr)
    {
        unsigned count = pRenameState->CountForHeapDef();
        pRenameState->PushHeap(block, count);

        DBG_SSA_JITDUMP("Ssa # for Heap phi on entry to BB%02u is %d.\n", block->bbNum, count);
    }

    // Record the "in" Ssa # for Heap.
    block->bbHeapSsaNumIn = pRenameState->CountForHeapUse();

    // We need to iterate over phi definitions, to give them SSA names, but we need
    // to know which are which, so we don't add phi definitions to handler phi arg lists.
    // Statements are phi defns until they aren't.
    bool       isPhiDefn   = true;
    GenTreePtr firstNonPhi = block->FirstNonPhiDef();
    for (GenTreePtr stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
    {
        if (stmt == firstNonPhi)
        {
            isPhiDefn = false;
        }

        for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
        {
            TreeRenameVariables(tree, block, pRenameState, isPhiDefn);
        }
    }

    // Now handle the final heap state.

    // If the block defines Heap, allocate an SSA variable for the final heap state in the block.
    // (This may be redundant with the last SSA var explicitly created, but there's no harm in that.)
    if (block->bbHeapDef)
    {
        unsigned count = pRenameState->CountForHeapDef();
        pRenameState->PushHeap(block, count);
        AddHeapDefToHandlerPhis(block, count);
    }

    // Record the "out" Ssa" # for Heap.
    block->bbHeapSsaNumOut = pRenameState->CountForHeapUse();

    DBG_SSA_JITDUMP("Ssa # for Heap on entry to BB%02u is %d; on exit is %d.\n", block->bbNum, block->bbHeapSsaNumIn,
                    block->bbHeapSsaNumOut);
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
    BasicBlock::AllSuccs allSuccs    = block->GetAllSuccs(m_pCompiler);
    AllSuccessorIter     allSuccsEnd = allSuccs.end();
    for (AllSuccessorIter allSuccsIter = allSuccs.begin(); allSuccsIter != allSuccsEnd; ++allSuccsIter)
    {
        BasicBlock* succ = (*allSuccsIter);
        // Walk the statements for phi nodes.
        for (GenTreePtr stmt = succ->bbTreeList; stmt != nullptr && stmt->IsPhiDefnStmt(); stmt = stmt->gtNext)
        {
            GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
            assert(tree->IsPhiDefn());

            // Get the phi node from GT_ASG.
            GenTreePtr phiNode = tree->gtOp.gtOp2;
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
                GenTreePtr newPhiArg =
                    new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(tree->gtOp.gtOp1->TypeGet(), lclNum, ssaNum, block);
                argList             = (phiNode->gtOp.gtOp1 == nullptr ? nullptr : phiNode->gtOp.gtOp1->AsArgList());
                phiNode->gtOp.gtOp1 = new (m_pCompiler, GT_LIST) GenTreeArgList(newPhiArg, argList);
                DBG_SSA_JITDUMP("  Added phi arg u:%d for V%02u from BB%02u in BB%02u.\n", ssaNum, lclNum, block->bbNum,
                                succ->bbNum);
            }

            m_pCompiler->gtSetStmtInfo(stmt);
            m_pCompiler->fgSetStmtSeq(stmt);
        }

        // Now handle Heap.
        if (succ->bbHeapSsaPhiFunc != nullptr)
        {
            if (succ->bbHeapSsaPhiFunc == BasicBlock::EmptyHeapPhiDef)
            {
                succ->bbHeapSsaPhiFunc = new (m_pCompiler) BasicBlock::HeapPhiArg(block);
            }
            else
            {
                BasicBlock::HeapPhiArg* curArg = succ->bbHeapSsaPhiFunc;
                bool                    found  = false;
                // This is a quadratic algorithm.  We might need to consider some switch over to a hash table
                // representation for the arguments of a phi node, to make this linear.
                while (curArg != nullptr)
                {
                    if (curArg->m_predBB == block)
                    {
                        found = true;
                        break;
                    }
                    curArg = curArg->m_nextArg;
                }
                if (!found)
                {
                    succ->bbHeapSsaPhiFunc = new (m_pCompiler) BasicBlock::HeapPhiArg(block, succ->bbHeapSsaPhiFunc);
                }
            }
            DBG_SSA_JITDUMP("  Added phi arg for Heap from BB%02u in BB%02u.\n", block->bbNum, succ->bbNum);
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

                for (GenTreePtr stmt = handlerStart->bbTreeList; stmt; stmt = stmt->gtNext)
                {
                    GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

                    // Check if the first n of the statements are phi nodes. If not, exit.
                    if (tree->OperGet() != GT_ASG || tree->gtOp.gtOp2 == nullptr ||
                        tree->gtOp.gtOp2->OperGet() != GT_PHI)
                    {
                        break;
                    }

                    // Get the phi node from GT_ASG.
                    GenTreePtr lclVar = tree->gtOp.gtOp1;
                    unsigned   lclNum = lclVar->gtLclVar.gtLclNum;

                    // If the variable is live-out of "blk", and is therefore live on entry to the try-block-start
                    // "succ", then we make sure the current SSA name for the
                    // var is one of the args of the phi node.  If not, go on.
                    LclVarDsc* lclVarDsc = &m_pCompiler->lvaTable[lclNum];
                    if (!lclVarDsc->lvTracked ||
                        !VarSetOps::IsMember(m_pCompiler, block->bbLiveOut, lclVarDsc->lvVarIndex))
                    {
                        continue;
                    }

                    GenTreePtr phiNode = tree->gtOp.gtOp2;
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
                        GenTreePtr newPhiArg =
                            new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(lclVar->TypeGet(), lclNum, ssaNum, block);
                        phiNode->gtOp.gtOp1 = new (m_pCompiler, GT_LIST) GenTreeArgList(newPhiArg, argList);

                        DBG_SSA_JITDUMP("  Added phi arg u:%d for V%02u from BB%02u in BB%02u.\n", ssaNum, lclNum,
                                        block->bbNum, handlerStart->bbNum);

                        m_pCompiler->gtSetStmtInfo(stmt);
                        m_pCompiler->fgSetStmtSeq(stmt);
                    }
                }

                // Now handle Heap.
                if (handlerStart->bbHeapSsaPhiFunc != nullptr)
                {
                    if (handlerStart->bbHeapSsaPhiFunc == BasicBlock::EmptyHeapPhiDef)
                    {
                        handlerStart->bbHeapSsaPhiFunc = new (m_pCompiler) BasicBlock::HeapPhiArg(block);
                    }
                    else
                    {
#ifdef DEBUG
                        BasicBlock::HeapPhiArg* curArg = handlerStart->bbHeapSsaPhiFunc;
                        while (curArg != nullptr)
                        {
                            assert(curArg->m_predBB != block);
                            curArg = curArg->m_nextArg;
                        }
#endif // DEBUG
                        handlerStart->bbHeapSsaPhiFunc =
                            new (m_pCompiler) BasicBlock::HeapPhiArg(block, handlerStart->bbHeapSsaPhiFunc);
                    }
                    DBG_SSA_JITDUMP("  Added phi arg for Heap from BB%02u in BB%02u.\n", block->bbNum,
                                    handlerStart->bbNum);
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

    // And for Heap.
    pRenameState->PopBlockHeapStack(block);
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

void SsaBuilder::RenameVariables(BlkToBlkSetMap* domTree, SsaRenameState* pRenameState)
{
    JITDUMP("*************** In SsaBuilder::RenameVariables()\n");

    // The first thing we do is treat parameters and must-init variables as if they have a
    // virtual definition before entry -- they start out at SSA name 1.
    for (unsigned i = 0; i < m_pCompiler->lvaCount; i++)
    {
        LclVarDsc* varDsc = &m_pCompiler->lvaTable[i];

#ifdef DEBUG
        varDsc->lvNumSsaNames = SsaConfig::UNINIT_SSA_NUM; // Start off fresh...
#endif

        if (varDsc->lvIsParam || m_pCompiler->info.compInitMem || varDsc->lvMustInit ||
            (varDsc->lvTracked &&
             VarSetOps::IsMember(m_pCompiler, m_pCompiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex)))
        {
            unsigned count = pRenameState->CountForDef(i);

            // In ValueNum we'd assume un-inited variables get FIRST_SSA_NUM.
            assert(count == SsaConfig::FIRST_SSA_NUM);
#ifdef DEBUG
            varDsc->lvNumSsaNames++;
#endif
            pRenameState->Push(nullptr, i, count);
        }
    }
    // In ValueNum we'd assume un-inited heap gets FIRST_SSA_NUM.
    // The heap is a parameter.  Use FIRST_SSA_NUM as first SSA name.
    unsigned initHeapCount = pRenameState->CountForHeapDef();
    assert(initHeapCount == SsaConfig::FIRST_SSA_NUM);
    pRenameState->PushHeap(m_pCompiler->fgFirstBB, initHeapCount);

    // Initialize the heap ssa numbers for unreachable blocks. ValueNum expects
    // heap ssa numbers to have some intitial value.
    for (BasicBlock* block = m_pCompiler->fgFirstBB; block; block = block->bbNext)
    {
        if (block->bbIDom == nullptr)
        {
            block->bbHeapSsaNumIn  = initHeapCount;
            block->bbHeapSsaNumOut = initHeapCount;
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
    BlockWorkStack*                   blocksToDo =
        new (jitstd::utility::allocate<BlockWorkStack>(m_allocator), jitstd::placement_t()) BlockWorkStack(m_allocator);

    blocksToDo->push_back(BlockWork(m_pCompiler->fgFirstBB)); // Probably have to include other roots of dom tree.

    while (blocksToDo->size() != 0)
    {
        BlockWork blockWrk = blocksToDo->back();
        blocksToDo->pop_back();
        BasicBlock* block = blockWrk.m_blk;

        DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables](BB%02u, processed = %d)\n", block->bbNum, blockWrk.m_processed);

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
            BlkSet* pBlkSet;
            if (domTree->Lookup(block, &pBlkSet))
            {
                for (BlkSet::KeyIterator child = pBlkSet->Begin(); !child.Equal(pBlkSet->End()); ++child)
                {
                    DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables](pushing dom child BB%02u)\n", child.Get()->bbNum);
                    blocksToDo->push_back(BlockWork(child.Get()));
                }
            }
        }
        else
        {
            // Done, pop all the stack count, if there is one for this block.
            BlockPopStacks(block, pRenameState);
            DBG_SSA_JITDUMP("[SsaBuilder::RenameVariables] done with BB%02u\n", block->bbNum);
        }
    }

    // Remember the number of Heap SSA names.
    m_pCompiler->lvHeapNumSsaNames = pRenameState->HeapCount();
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
        printf("After SSA BB%02u:\n", postOrder[i]->bbNum);
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
    BasicBlock** postOrder = (BasicBlock**)alloca(blockCount * sizeof(BasicBlock*));

    // Topologically sort the graph.
    int count = TopologicalSort(postOrder, blockCount);
    JITDUMP("[SsaBuilder] Topologically sorted the graph.\n");
    EndPhase(PHASE_BUILD_SSA_TOPOSORT);

    // Compute IDom(b).
    ComputeImmediateDom(postOrder, count);

    // Compute the dominator tree.
    BlkToBlkSetMap* domTree = new (m_pCompiler->getAllocator()) BlkToBlkSetMap(m_pCompiler->getAllocator());
    ComputeDominators(postOrder, count, domTree);
    EndPhase(PHASE_BUILD_SSA_DOMS);

    // Insert phi functions.
    InsertPhiFunctions(postOrder, count);

    // Rename local variables and collect UD information for each ssa var.
    SsaRenameState* pRenameState = new (jitstd::utility::allocate<SsaRenameState>(m_allocator), jitstd::placement_t())
        SsaRenameState(m_allocator, m_pCompiler->lvaCount);
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

    typedef SimplerHashTable<ssize_t, SmallPrimitiveKeyFuncs<ssize_t>, SSAName, JitSimplerHashBehavior>
        LabelToSSANameMap;
    typedef SimplerHashTable<SSAName, SSAName, ssize_t, JitSimplerHashBehavior> SSANameToLabelMap;

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
        GenTreePtr      node = ki.Get();
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
