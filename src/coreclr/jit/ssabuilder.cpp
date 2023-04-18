// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        while (finger1 != nullptr && finger1->bbPostorderNum < finger2->bbPostorderNum)
        {
            finger1 = finger1->bbIDom;
        }
        if (finger1 == nullptr)
        {
            return nullptr;
        }
        while (finger2 != nullptr && finger2->bbPostorderNum < finger1->bbPostorderNum)
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

PhaseStatus Compiler::fgSsaBuild()
{
    // If this is not the first invocation, reset data structures for SSA.
    if (fgSsaPassesCompleted > 0)
    {
        fgResetForSsa();
    }

    SsaBuilder builder(this);
    builder.Build();
    fgSsaPassesCompleted++;
    fgSsaChecksEnabled = true;
#ifdef DEBUG
    JitTestCheckSSA();
#endif // DEBUG

    return PhaseStatus::MODIFIED_EVERYTHING;
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

    if (m_outlinedCompositeSsaNums != nullptr)
    {
        m_outlinedCompositeSsaNums->Reset();
    }

    for (BasicBlock* const blk : Blocks())
    {
        // Eliminate phis.
        for (MemoryKind memoryKind : allMemoryKinds())
        {
            blk->bbMemorySsaPhiFunc[memoryKind] = nullptr;
        }
        if (blk->bbStmtList != nullptr)
        {
            Statement* last = blk->lastStmt();
            blk->bbStmtList = blk->FirstNonPhiDef();
            if (blk->bbStmtList != nullptr)
            {
                blk->bbStmtList->SetPrevStmt(last);
            }
        }

        for (Statement* const stmt : blk->Statements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                if (tree->IsLocal())
                {
                    tree->AsLclVarCommon()->SetSsaNum(SsaConfig::RESERVED_SSA_NUM);
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
    , m_renameStack(m_allocator, pCompiler->lvaCount)
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

    while (!blocks.Empty())
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
            block->bbPostorderNum = postIndex;
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
            for (FlowEdge* pred = m_pCompiler->BlockPredsWithEH(block); pred; pred = pred->getNextPredEdge())
            {
                if (BitVecOps::IsMember(&m_visitedTraits, m_visited, pred->getSourceBlock()->bbNum))
                {
                    predBlock = pred->getSourceBlock();
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
            for (FlowEdge* pred = m_pCompiler->BlockPredsWithEH(block); pred; pred = pred->getNextPredEdge())
            {
                if (predBlock != pred->getSourceBlock())
                {
                    BasicBlock* domAncestor = IntersectDom(pred->getSourceBlock(), bbIDom);
                    // The result may be NULL if "block" and "pred->getBlock()" are part of a
                    // cycle -- neither is guaranteed ordered wrt the other in reverse postorder,
                    // so we may be computing the IDom of "block" before the IDom of "pred->getBlock()" has
                    // been computed.  But that's OK -- if they're in a cycle, they share the same immediate
                    // dominator, so the contribution of "pred->getBlock()" is not necessary to compute
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

        FlowEdge* blockPreds = m_pCompiler->BlockPredsWithEH(block);

        // If block has 0/1 predecessor, skip.
        if ((blockPreds == nullptr) || (blockPreds->getNextPredEdge() == nullptr))
        {
            DBG_SSA_JITDUMP("   Has %d preds; skipping.\n", blockPreds == nullptr ? 0 : 1);
            continue;
        }

        // Otherwise, there are > 1 preds.  Each is a candidate B2 in the definition --
        // *unless* it dominates "block"/B3.

        for (FlowEdge* pred = blockPreds; pred != nullptr; pred = pred->getNextPredEdge())
        {
            DBG_SSA_JITDUMP("   Considering predecessor " FMT_BB ".\n", pred->getSourceBlock()->bbNum);

            // If we've found a B2, then consider the possible B1's.  We start with
            // B2, since a block dominates itself, then traverse upwards in the dominator
            // tree, stopping when we reach the root, or the immediate dominator of "block"/B3.
            // (Note that we are guaranteed to encounter this immediate dominator of "block"/B3:
            // a predecessor must be dominated by B3's immediate dominator.)
            // Along this way, make "block"/B3 part of the dom frontier of the B1.
            // When we reach this immediate dominator, the definition no longer applies, since this
            // potential B1 *does* dominate "block"/B3, so we stop.
            for (BasicBlock* b1 = pred->getSourceBlock(); (b1 != nullptr) && (b1 != block->bbIDom); // !root && !loop
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
    for (Statement* const stmt : block->Statements())
    {
        // A prefix of the statements of the block are phi definition nodes. If we complete processing
        // that prefix, exit.
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        GenTree* tree = stmt->GetRootNode();

        GenTree* phiLhs = tree->AsOp()->gtOp1;
        assert(phiLhs->OperGet() == GT_LCL_VAR);
        if (phiLhs->AsLclVarCommon()->GetLclNum() == lclNum)
        {
            return tree->AsOp()->gtOp2;
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// InsertPhi: Insert a new GT_PHI statement.
//
// Arguments:
//    block  - The block where to insert the statement
//    lclNum - The variable number
//
void SsaBuilder::InsertPhi(BasicBlock* block, unsigned lclNum)
{
    var_types type = m_pCompiler->lvaGetDesc(lclNum)->TypeGet();

    GenTree* lhs = m_pCompiler->gtNewLclvNode(lclNum, type);
    // PHIs and all the associated nodes do not generate any code so the costs are always 0
    lhs->SetCosts(0, 0);
    GenTree* phi = new (m_pCompiler, GT_PHI) GenTreePhi(type);
    phi->SetCosts(0, 0);
    GenTree* asg = m_pCompiler->gtNewAssignNode(lhs, phi);
    // Evaluate the assignment RHS (the PHI node) first. This way the LHS will end up right
    // in front of the assignment in the linear order, that ensures that using gtGetParent
    // on the LHS to find the assignment doesn't have to traverse the PHI and its args.
    asg->gtFlags |= GTF_REVERSE_OPS;
    asg->SetCosts(0, 0);

    // Create the statement and chain everything in linear order - PHI, LCL_VAR, ASG
    Statement* stmt = m_pCompiler->gtNewStmt(asg);
    stmt->SetTreeList(phi);
    phi->gtNext = lhs;
    lhs->gtPrev = phi;
    lhs->gtNext = asg;
    asg->gtPrev = lhs;

#ifdef DEBUG
    unsigned seqNum = 1;
    for (GenTree* const node : stmt->TreeList())
    {
        node->gtSeqNum = seqNum++;
    }
#endif // DEBUG

    m_pCompiler->fgInsertStmtAtBeg(block, stmt);

    JITDUMP("Added PHI definition for V%02u at start of " FMT_BB ".\n", lclNum, block->bbNum);
}

//------------------------------------------------------------------------
// AddPhiArg: Add a new GT_PHI_ARG node to an existing GT_PHI node.
//
// Arguments:
//    block  - The block that contains the statement
//    stmt   - The statement that contains the GT_PHI node
//    lclNum - The variable number
//    ssaNum - The SSA number
//    pred   - The predecessor block
//
void SsaBuilder::AddPhiArg(
    BasicBlock* block, Statement* stmt, GenTreePhi* phi, unsigned lclNum, unsigned ssaNum, BasicBlock* pred)
{
#ifdef DEBUG
    // Make sure it isn't already present: we should only add each definition once.
    for (GenTreePhi::Use& use : phi->Uses())
    {
        assert(use.GetNode()->AsPhiArg()->GetSsaNum() != ssaNum);
    }
#endif // DEBUG

    var_types type = m_pCompiler->lvaGetDesc(lclNum)->TypeGet();

    GenTree* phiArg = new (m_pCompiler, GT_PHI_ARG) GenTreePhiArg(type, lclNum, ssaNum, pred);
    // Costs are not relevant for PHI args.
    phiArg->SetCosts(0, 0);
    // The argument order doesn't matter so just insert at the front of the list because
    // it's easier. It's also easier to insert in linear order since the first argument
    // will be first in linear order as well.
    phi->gtUses = new (m_pCompiler, CMK_ASTNode) GenTreePhi::Use(phiArg, phi->gtUses);

    GenTree* head = stmt->GetTreeList();
    assert(head->OperIs(GT_PHI, GT_PHI_ARG));
    stmt->SetTreeList(phiArg);
    phiArg->gtNext = head;
    head->gtPrev   = phiArg;

    LclVarDsc* const    varDsc  = m_pCompiler->lvaGetDesc(lclNum);
    LclSsaVarDsc* const ssaDesc = varDsc->GetPerSsaData(ssaNum);
    ssaDesc->AddPhiUse(block);

#ifdef DEBUG
    unsigned seqNum = 1;
    for (GenTree* const node : stmt->TreeList())
    {
        node->gtSeqNum = seqNum++;
    }
#endif // DEBUG

    DBG_SSA_JITDUMP("Added PHI arg u:%d for V%02u from " FMT_BB " in " FMT_BB ".\n", ssaNum, lclNum, pred->bbNum,
                    block->bbNum);
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
            unsigned lclNum = m_pCompiler->lvaTrackedIndexToLclNum(varIndex);
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
                    InsertPhi(bbInDomFront, lclNum);
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

//------------------------------------------------------------------------
// RenameDef: Rename a local or memory definition generated by a GT_ASG/GT_CALL node.
//
// Arguments:
//    defNode - The GT_ASG/GT_CALL node that generates the definition
//    block - The basic block that contains `defNode`
//
void SsaBuilder::RenameDef(GenTree* defNode, BasicBlock* block)
{
    assert(defNode->OperIsSsaDef());

    if (defNode->OperIs(GT_ASG))
    {
        // This is perhaps temporary -- maybe should be done elsewhere.  Label GT_INDs on LHS of assignments, so we
        // can skip these during (at least) value numbering.
        GenTree* lhs = defNode->gtGetOp1()->gtEffectiveVal(/*commaOnly*/ true);
        if (lhs->OperIs(GT_IND, GT_BLK))
        {
            lhs->gtFlags |= GTF_IND_ASG_LHS;
        }
    }

    GenTreeLclVarCommon* lclNode;
    bool                 isFullDef = false;
    ssize_t              offset    = 0;
    unsigned             storeSize = 0;
    bool                 isLocal   = defNode->DefinesLocal(m_pCompiler, &lclNode, &isFullDef, &offset, &storeSize);

    if (isLocal)
    {
        // This should have been marked as definition.
        assert(((lclNode->gtFlags & GTF_VAR_DEF) != 0) && (((lclNode->gtFlags & GTF_VAR_USEASG) != 0) == !isFullDef));

        unsigned   lclNum = lclNode->GetLclNum();
        LclVarDsc* varDsc = m_pCompiler->lvaGetDesc(lclNum);

        if (m_pCompiler->lvaInSsa(lclNum))
        {
            lclNode->SetSsaNum(RenamePushDef(defNode, block, lclNum, isFullDef));
            assert(!varDsc->IsAddressExposed()); // Cannot define SSA memory.
            return;
        }

        if (varDsc->lvPromoted)
        {
            for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
            {
                unsigned   fieldLclNum = varDsc->lvFieldLclStart + index;
                LclVarDsc* fieldVarDsc = m_pCompiler->lvaGetDesc(fieldLclNum);
                if (m_pCompiler->lvaInSsa(fieldLclNum))
                {
                    ssize_t  fieldStoreOffset;
                    unsigned fieldStoreSize;
                    unsigned ssaNum = SsaConfig::RESERVED_SSA_NUM;

                    // Fast-path the common case of an "entire" store.
                    if (isFullDef)
                    {
                        ssaNum = RenamePushDef(defNode, block, fieldLclNum, /* defIsFull */ true);
                    }
                    else if (m_pCompiler->gtStoreDefinesField(fieldVarDsc, offset, storeSize, &fieldStoreOffset,
                                                              &fieldStoreSize))
                    {
                        ssaNum = RenamePushDef(defNode, block, fieldLclNum,
                                               ValueNumStore::LoadStoreIsEntire(genTypeSize(fieldVarDsc),
                                                                                fieldStoreOffset, fieldStoreSize));
                    }

                    if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        lclNode->SetSsaNum(m_pCompiler, index, ssaNum);
                    }
                }
            }
        }
    }
    else if (defNode->OperIs(GT_CALL))
    {
        // If the current def is a call we either know the call is pure or else has arbitrary memory definitions,
        // the memory effect of the call is captured by the live out state from the block and doesn't need special
        // handling here. If we ever change liveness to more carefully model call effects (from interprecedural
        // information) we might need to revisit this.
        return;
    }

    // Figure out if "defNode" may make a new GC heap state (if we care for this block).
    if (((block->bbMemoryHavoc & memoryKindSet(GcHeap)) == 0) && m_pCompiler->ehBlockHasExnFlowDsc(block))
    {
        bool isAddrExposedLocal = isLocal && m_pCompiler->lvaVarAddrExposed(lclNode->GetLclNum());
        bool hasByrefHavoc      = ((block->bbMemoryHavoc & memoryKindSet(ByrefExposed)) != 0);
        if (!isLocal || (isAddrExposedLocal && !hasByrefHavoc))
        {
            // It *may* define byref memory in a non-havoc way.  Make a new SSA # -- associate with this node.
            unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
            if (!hasByrefHavoc)
            {
                m_renameStack.PushMemory(ByrefExposed, block, ssaNum);
                m_pCompiler->GetMemorySsaMap(ByrefExposed)->Set(defNode, ssaNum);
#ifdef DEBUG
                if (m_pCompiler->verboseSsa)
                {
                    printf("Node ");
                    Compiler::printTreeID(defNode);
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
                    assert(*m_pCompiler->GetMemorySsaMap(GcHeap)->LookupPointer(defNode) == ssaNum);
                    assert(block->bbMemorySsaPhiFunc[GcHeap] == block->bbMemorySsaPhiFunc[ByrefExposed]);
                }
                else
                {
                    if (!hasByrefHavoc)
                    {
                        // Allocate a distinct defnum for the GC Heap
                        ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                    }

                    m_renameStack.PushMemory(GcHeap, block, ssaNum);
                    m_pCompiler->GetMemorySsaMap(GcHeap)->Set(defNode, ssaNum);
                    AddMemoryDefToHandlerPhis(GcHeap, block, ssaNum);
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// RenamePushDef: Create and push a new definition on the renaming stack.
//
// Arguments:
//    defNode   - The store node for the definition
//    block     - The block in which it occurs
//    lclNum    - Number of the local being defined
//    isFullDef - Whether the def is "entire"
//
// Return Value:
//    The pushed SSA number.
//
unsigned SsaBuilder::RenamePushDef(GenTree* defNode, BasicBlock* block, unsigned lclNum, bool isFullDef)
{
    // Promoted variables are not in SSA, only their fields are.
    assert(m_pCompiler->lvaInSsa(lclNum) && !m_pCompiler->lvaGetDesc(lclNum)->lvPromoted);

    LclVarDsc* const varDsc = m_pCompiler->lvaGetDesc(lclNum);
    unsigned const   ssaNum =
        varDsc->lvPerSsaData.AllocSsaNum(m_allocator, block, defNode->OperIs(GT_ASG) ? defNode->AsOp() : nullptr);

    if (!isFullDef)
    {
        // This is a partial definition of a variable. The node records only the SSA number
        // of the def. The SSA number of the old definition (the "use" portion) will be
        // recorded in the SSA descriptor.
        LclSsaVarDsc* const ssaDesc   = varDsc->GetPerSsaData(ssaNum);
        unsigned const      useSsaNum = m_renameStack.Top(lclNum);
        ssaDesc->SetUseDefSsaNum(useSsaNum);

        LclSsaVarDsc* const useSsaDesc = varDsc->GetPerSsaData(useSsaNum);
        useSsaDesc->AddUse(block);
    }

    m_renameStack.Push(block, lclNum, ssaNum);

    // If necessary, add SSA name to the arg list of a phi def in any handlers for try
    // blocks that "block" is within. (But only do this for "real" definitions, not phis.)
    if (!defNode->IsPhiDefn())
    {
        AddDefToHandlerPhis(block, lclNum, ssaNum);
    }

    return ssaNum;
}

//------------------------------------------------------------------------
// RenameLclUse: Rename a use of a local variable.
//
// Arguments:
//    lclNode - A GT_LCL_VAR or GT_LCL_FLD node that is not a definition
//      block - basic block containing the use
//
void SsaBuilder::RenameLclUse(GenTreeLclVarCommon* lclNode, BasicBlock* block)
{
    assert((lclNode->gtFlags & GTF_VAR_DEF) == 0);

    unsigned const   lclNum = lclNode->GetLclNum();
    LclVarDsc* const lclVar = m_pCompiler->lvaGetDesc(lclNum);
    unsigned         ssaNum;

    if (!m_pCompiler->lvaInSsa(lclNum))
    {
        ssaNum = SsaConfig::RESERVED_SSA_NUM;
    }
    else
    {
        // Promoted variables are not in SSA, only their fields are.
        assert(!lclVar->lvPromoted);
        ssaNum                      = m_renameStack.Top(lclNum);
        LclSsaVarDsc* const ssaDesc = lclVar->GetPerSsaData(ssaNum);
        ssaDesc->AddUse(block);
    }

    lclNode->SetSsaNum(ssaNum);
}

void SsaBuilder::AddDefToHandlerPhis(BasicBlock* block, unsigned lclNum, unsigned ssaNum)
{
    assert(m_pCompiler->lvaTable[lclNum].lvTracked); // Precondition.
    unsigned lclIndex = m_pCompiler->lvaTable[lclNum].lvVarIndex;

    EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
    if (tryBlk != nullptr)
    {
        DBG_SSA_JITDUMP("Definition of local V%02u/d:%d in block " FMT_BB
                        " has exn handler; adding as phi arg to handlers.\n",
                        lclNum, ssaNum, block->bbNum);
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
                for (Statement* const stmt : handler->Statements())
                {
                    // If the tree is not an SSA def, break out of the loop: we're done.
                    if (!stmt->IsPhiDefnStmt())
                    {
                        break;
                    }

                    GenTree* tree = stmt->GetRootNode();

                    assert(tree->IsPhiDefn());

                    if (tree->AsOp()->gtOp1->AsLclVar()->GetLclNum() == lclNum)
                    {
                        // It's the definition for the right local.  Add "ssaNum" to the RHS.
                        AddPhiArg(handler, stmt, tree->gtGetOp2()->AsPhi(), lclNum, ssaNum, block);
#ifdef DEBUG
                        phiFound = true;
#endif
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

void SsaBuilder::AddMemoryDefToHandlerPhis(MemoryKind memoryKind, BasicBlock* block, unsigned ssaNum)
{
    if (m_pCompiler->ehBlockHasExnFlowDsc(block))
    {
        // Don't do anything for a compiler-inserted BBJ_ALWAYS that is a "leave helper".
        if ((block->bbFlags & BBF_INTERNAL) && block->isBBCallAlwaysPairTail())
        {
            return;
        }

        // Otherwise...
        DBG_SSA_JITDUMP("Definition of %s/d:%d in block " FMT_BB " has exn handler; adding as phi arg to handlers.\n",
                        memoryKindNames[memoryKind], ssaNum, block->bbNum);
        EHblkDsc* tryBlk = m_pCompiler->ehGetBlockExnFlowDsc(block);
        while (true)
        {
            BasicBlock* handler = tryBlk->ExFlowBlock();

            // Is memoryKind live on entry to the handler?
            if ((handler->bbMemoryLiveIn & memoryKindSet(memoryKind)) != 0)
            {
                // Add "ssaNum" to the phi args of memoryKind.
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
                    handlerMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(ssaNum);
                }
                else
                {
#ifdef DEBUG
                    BasicBlock::MemoryPhiArg* curArg = handler->bbMemorySsaPhiFunc[memoryKind];
                    while (curArg != nullptr)
                    {
                        assert(curArg->GetSsaNum() != ssaNum);
                        curArg = curArg->m_nextArg;
                    }
#endif // DEBUG
                    handlerMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(ssaNum, handlerMemoryPhi);
                }

                DBG_SSA_JITDUMP("   Added phi arg u:%d for %s to phi defn in handler block " FMT_BB ".\n", ssaNum,
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

//------------------------------------------------------------------------
// BlockRenameVariables: Rename all definitions and uses within a block.
//
// Arguments:
//    block - The block
//
void SsaBuilder::BlockRenameVariables(BasicBlock* block)
{
    // First handle the incoming memory states.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // ByrefExposed and GcHeap share any phi this block may have,
            assert(block->bbMemorySsaPhiFunc[memoryKind] == block->bbMemorySsaPhiFunc[ByrefExposed]);
            // so we will have already allocated a defnum for it if needed.
            assert(memoryKind > ByrefExposed);

            block->bbMemorySsaNumIn[memoryKind] = m_renameStack.TopMemory(ByrefExposed);
        }
        else
        {
            // Is there an Phi definition for memoryKind at the start of this block?
            if (block->bbMemorySsaPhiFunc[memoryKind] != nullptr)
            {
                unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                m_renameStack.PushMemory(memoryKind, block, ssaNum);

                DBG_SSA_JITDUMP("Ssa # for %s phi on entry to " FMT_BB " is %d.\n", memoryKindNames[memoryKind],
                                block->bbNum, ssaNum);

                block->bbMemorySsaNumIn[memoryKind] = ssaNum;
            }
            else
            {
                block->bbMemorySsaNumIn[memoryKind] = m_renameStack.TopMemory(memoryKind);
            }
        }
    }

    // Walk the statements of the block and rename definitions and uses.
    for (Statement* const stmt : block->Statements())
    {
        for (GenTree* const tree : stmt->TreeList())
        {
            if (tree->OperIsSsaDef())
            {
                RenameDef(tree, block);
            }
            // PHI_ARG nodes already have SSA numbers so we only need to check LCL_VAR and LCL_FLD nodes.
            else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD) && ((tree->gtFlags & GTF_VAR_DEF) == 0))
            {
                RenameLclUse(tree->AsLclVarCommon(), block);
            }
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

            block->bbMemorySsaNumOut[memoryKind] = m_renameStack.TopMemory(ByrefExposed);
        }
        else
        {
            if ((block->bbMemoryDef & memorySet) != 0)
            {
                unsigned ssaNum = m_pCompiler->lvMemoryPerSsaData.AllocSsaNum(m_allocator);
                m_renameStack.PushMemory(memoryKind, block, ssaNum);
                AddMemoryDefToHandlerPhis(memoryKind, block, ssaNum);

                block->bbMemorySsaNumOut[memoryKind] = ssaNum;
            }
            else
            {
                block->bbMemorySsaNumOut[memoryKind] = m_renameStack.TopMemory(memoryKind);
            }
        }

        DBG_SSA_JITDUMP("Ssa # for %s on entry to " FMT_BB " is %d; on exit is %d.\n", memoryKindNames[memoryKind],
                        block->bbNum, block->bbMemorySsaNumIn[memoryKind], block->bbMemorySsaNumOut[memoryKind]);
    }
}

//------------------------------------------------------------------------
// AddPhiArgsToSuccessors: Add GT_PHI_ARG nodes to the GT_PHI nodes within block's successors.
//
// Arguments:
//    block - The block
//
void SsaBuilder::AddPhiArgsToSuccessors(BasicBlock* block)
{
    for (BasicBlock* succ : block->GetAllSuccs(m_pCompiler))
    {
        // Walk the statements for phi nodes.
        for (Statement* const stmt : succ->Statements())
        {
            // A prefix of the statements of the block are phi definition nodes. If we complete processing
            // that prefix, exit.
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTree*    tree = stmt->GetRootNode();
            GenTreePhi* phi  = tree->gtGetOp2()->AsPhi();

            unsigned lclNum = tree->AsOp()->gtOp1->AsLclVar()->GetLclNum();
            unsigned ssaNum = m_renameStack.Top(lclNum);
            // Search the arglist for an existing definition for ssaNum.
            // (Can we assert that its the head of the list?  This should only happen when we add
            // during renaming for a definition that occurs within a try, and then that's the last
            // value of the var within that basic block.)

            bool found = false;
            for (GenTreePhi::Use& use : phi->Uses())
            {
                if (use.GetNode()->AsPhiArg()->GetSsaNum() == ssaNum)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                AddPhiArg(succ, stmt, phi, lclNum, ssaNum, block);
            }
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

                for (Statement* const stmt : handlerStart->Statements())
                {
                    GenTree* tree = stmt->GetRootNode();

                    // Check if the first n of the statements are phi nodes. If not, exit.
                    if (tree->OperGet() != GT_ASG || tree->AsOp()->gtOp2 == nullptr ||
                        tree->AsOp()->gtOp2->OperGet() != GT_PHI)
                    {
                        break;
                    }

                    // Get the phi node from GT_ASG.
                    GenTree* lclVar = tree->AsOp()->gtOp1;
                    unsigned lclNum = lclVar->AsLclVar()->GetLclNum();

                    // If the variable is live-out of "blk", and is therefore live on entry to the try-block-start
                    // "succ", then we make sure the current SSA name for the
                    // var is one of the args of the phi node.  If not, go on.
                    const LclVarDsc* lclVarDsc = m_pCompiler->lvaGetDesc(lclNum);
                    if (!lclVarDsc->lvTracked ||
                        !VarSetOps::IsMember(m_pCompiler, block->bbLiveOut, lclVarDsc->lvVarIndex))
                    {
                        continue;
                    }

                    GenTreePhi* phi = tree->gtGetOp2()->AsPhi();

                    unsigned ssaNum = m_renameStack.Top(lclNum);

                    // See if this ssaNum is already an arg to the phi.
                    bool alreadyArg = false;
                    for (GenTreePhi::Use& use : phi->Uses())
                    {
                        if (use.GetNode()->AsPhiArg()->GetSsaNum() == ssaNum)
                        {
                            alreadyArg = true;
                            break;
                        }
                    }
                    if (!alreadyArg)
                    {
                        AddPhiArg(handlerStart, stmt, phi, lclNum, ssaNum, block);
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

//------------------------------------------------------------------------
// RenameVariables: Rename all definitions and uses within the compiled method.
//
// Notes:
//    See Briggs, Cooper, Harvey and Simpson "Practical Improvements to the Construction
//    and Destruction of Static Single Assignment Form."
//
void SsaBuilder::RenameVariables()
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

        LclVarDsc* varDsc = m_pCompiler->lvaGetDesc(lclNum);
        assert(varDsc->lvTracked);

        if (varDsc->lvIsParam || m_pCompiler->info.compInitMem || varDsc->lvMustInit ||
            VarSetOps::IsMember(m_pCompiler, m_pCompiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            unsigned ssaNum = varDsc->lvPerSsaData.AllocSsaNum(m_allocator);

            // In ValueNum we'd assume un-inited variables get FIRST_SSA_NUM.
            assert(ssaNum == SsaConfig::FIRST_SSA_NUM);

            m_renameStack.Push(m_pCompiler->fgFirstBB, lclNum, ssaNum);
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
        m_renameStack.PushMemory(memoryKind, m_pCompiler->fgFirstBB, initMemorySsaNum);
    }

    // Initialize the memory ssa numbers for unreachable blocks. ValueNum expects
    // memory ssa numbers to have some initial value.
    for (BasicBlock* const block : m_pCompiler->Blocks())
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

    class SsaRenameDomTreeVisitor : public DomTreeVisitor<SsaRenameDomTreeVisitor>
    {
        SsaBuilder*     m_builder;
        SsaRenameState* m_renameStack;

    public:
        SsaRenameDomTreeVisitor(Compiler* compiler, SsaBuilder* builder, SsaRenameState* renameStack)
            : DomTreeVisitor(compiler, compiler->fgSsaDomTree), m_builder(builder), m_renameStack(renameStack)
        {
        }

        void PreOrderVisit(BasicBlock* block)
        {
            // TODO-Cleanup: Move these functions from SsaBuilder to this class.
            m_builder->BlockRenameVariables(block);
            m_builder->AddPhiArgsToSuccessors(block);
        }

        void PostOrderVisit(BasicBlock* block)
        {
            m_renameStack->PopBlockStacks(block);
        }
    };

    SsaRenameDomTreeVisitor visitor(m_pCompiler, this, &m_renameStack);
    visitor.WalkTree();
}

//------------------------------------------------------------------------
// Build: Build SSA form
//
// Notes:
//
// Sorts the graph topologically.
//   - Collects them in postOrder array.
//
// Identifies each block's immediate dominator.
//   - Computes this in bbIDom of each BasicBlock.
//
// Computes DOM tree relation.
//   - Computes domTree as block -> set of blocks.
//   - Computes pre/post order traversal of the DOM tree.
//
// Inserts phi nodes.
//   - Computes dominance frontier as block -> set of blocks.
//   - Allocates block use/def/livein/liveout and computes it.
//   - Inserts phi nodes with only rhs at the beginning of the blocks.
//
// Renames variables.
//   - Walks blocks in evaluation order and gives uses and defs names.
//   - Gives empty phi nodes their rhs arguments as they become known while renaming.
//
// @see "A simple, fast dominance algorithm" by Keith D. Cooper, Timothy J. Harvey, Ken Kennedy.
// @see Briggs, Cooper, Harvey and Simpson "Practical Improvements to the Construction
//      and Destruction of Static Single Assignment Form."
//
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
        postOrder = (BasicBlock**)_alloca(blockCount * sizeof(BasicBlock*));
    }

    m_visitedTraits = BitVecTraits(blockCount, m_pCompiler);
    m_visited       = BitVecOps::MakeEmpty(&m_visitedTraits);

    // TODO-Cleanup: We currently have two dominance computations happening.  We should unify them; for
    // now, at least forget the results of the first. Note that this does not clear fgDomTreePreOrder
    // and fgDomTreePostOrder nor does the subsequent code call fgNumberDomTree once the new dominator
    // tree is built. The pre/post order numbers that were generated previously and used for loop
    // recognition are still being used by optPerformHoistExpr via fgCreateLoopPreHeader. That's rather
    // odd, considering that SetupBBRoot may have added a new block.
    for (BasicBlock* const block : m_pCompiler->Blocks())
    {
        block->bbIDom         = nullptr;
        block->bbPostorderNum = 0;
    }

    // Topologically sort the graph.
    int count = TopologicalSort(postOrder, blockCount);
    JITDUMP("[SsaBuilder] Topologically sorted the graph.\n");
    EndPhase(PHASE_BUILD_SSA_TOPOSORT);

    // Compute IDom(b).
    ComputeImmediateDom(postOrder, count);

    m_pCompiler->fgSsaDomTree = m_pCompiler->fgBuildDomTree();
    EndPhase(PHASE_BUILD_SSA_DOMS);

    // Compute liveness on the graph.
    m_pCompiler->fgLocalVarLiveness();
    EndPhase(PHASE_BUILD_SSA_LIVENESS);

    m_pCompiler->optRemoveRedundantZeroInits();
    EndPhase(PHASE_ZERO_INITS);

    // Mark all variables that will be tracked by SSA
    for (unsigned lclNum = 0; lclNum < m_pCompiler->lvaCount; lclNum++)
    {
        m_pCompiler->lvaTable[lclNum].lvInSsa = m_pCompiler->lvaGetDesc(lclNum)->lvTracked;
    }

    // Insert phi functions.
    InsertPhiFunctions(postOrder, count);

    // Rename local variables and collect UD information for each ssa var.
    RenameVariables();
    EndPhase(PHASE_BUILD_SSA_RENAME);

    JITDUMPEXEC(m_pCompiler->DumpSsaSummary());
}

void SsaBuilder::SetupBBRoot()
{
    assert(m_pCompiler->fgPredsComputed);

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

    // There's an artificial incoming reference count for the first BB.  We're about to make it no longer
    // the first BB, so decrement that.
    assert(oldFirst->bbRefs > 0);
    oldFirst->bbRefs--;

    m_pCompiler->fgInsertBBbefore(m_pCompiler->fgFirstBB, bbRoot);

    assert(m_pCompiler->fgFirstBB == bbRoot);
    m_pCompiler->fgAddRefPred(oldFirst, bbRoot);
}

#ifdef DEBUG

//------------------------------------------------------------------------
// DumpSsaSummary: dump info about each SSA lifetime
//
void Compiler::DumpSsaSummary()
{
    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc* const varDsc = lvaGetDesc(lclNum);

        if (!varDsc->lvInSsa)
        {
            continue;
        }

        const SsaDefArray<LclSsaVarDsc>& ssaDefs = varDsc->lvPerSsaData;
        unsigned const                   numDefs = ssaDefs.GetCount();

        if (numDefs == 0)
        {
            printf("V%02u: in SSA but no defs\n", lclNum);
        }
        else
        {
            for (unsigned i = 0; i < numDefs; i++)
            {
                // Dump BB00 for def with no block.
                //
                LclSsaVarDsc* const ssaVarDsc = ssaDefs.GetSsaDefByIndex(i);
                const unsigned      ssaNum    = ssaDefs.GetSsaNum(ssaVarDsc);
                BasicBlock* const   block     = ssaVarDsc->GetBlock();
                const unsigned      blockNum  = block != nullptr ? block->bbNum : 0;

                printf("V%02u.%u: defined in " FMT_BB " %u uses (%s)%s\n", lclNum, ssaNum, blockNum,
                       ssaVarDsc->GetNumUses(), ssaVarDsc->HasGlobalUse() ? "global" : "local",
                       ssaVarDsc->HasPhiUse() ? ", has phi uses" : "");
            }
        }
    }
}

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
    for (GenTree* const node : NodeToTestDataMap::KeyIteration(testData))
    {
        TestLabelAndNum tlAndN;
        bool            nodeExists = testData->Lookup(node, &tlAndN);
        assert(nodeExists);
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
                printf(", SSA name = <%d, %d> -- SSA name class %d.\n", lcl->GetLclNum(), lcl->GetSsaNum(),
                       tlAndN.m_num);
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
                bool    ssaExists = ssaToLabel->Lookup(ssaNm, &num2);
                assert(ssaExists);
                // And the mappings must be the same.
                if (tlAndN.m_num != num2)
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->GetLclNum(),
                           lcl->GetSsaNum(), tlAndN.m_num);
                    printf(
                        "but this SSA name <%d,%d> has already been associated with a different SSA name class: %d.\n",
                        ssaNm.m_lvNum, ssaNm.m_ssaNum, num2);
                    unreached();
                }
                // And the current node must be of the specified SSA family.
                if (!(lcl->GetLclNum() == ssaNm.m_lvNum && lcl->GetSsaNum() == ssaNm.m_ssaNum))
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->GetLclNum(),
                           lcl->GetSsaNum(), tlAndN.m_num);
                    printf("but that name class was previously bound to a different SSA name: <%d,%d>.\n",
                           ssaNm.m_lvNum, ssaNm.m_ssaNum);
                    unreached();
                }
            }
            else
            {
                ssaNm.m_lvNum  = lcl->GetLclNum();
                ssaNm.m_ssaNum = lcl->GetSsaNum();
                ssize_t num;
                // The mapping(s) must be one-to-one: if the label has no mapping, then the ssaNm may not, either.
                if (ssaToLabel->Lookup(ssaNm, &num))
                {
                    printf("Node: ");
                    printTreeID(lcl);
                    printf(", SSA name = <%d, %d> was declared in SSA name class %d,\n", lcl->GetLclNum(),
                           lcl->GetSsaNum(), tlAndN.m_num);
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
