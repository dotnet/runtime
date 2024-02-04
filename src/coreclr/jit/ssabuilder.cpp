// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "ssaconfig.h"
#include "ssarenamestate.h"
#include "ssabuilder.h"

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
    fgSsaValid = true;
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
                if (tree->IsAnyLocal())
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

        // If block has 0/1 predecessor, skip, apart from handler entry blocks
        // that are always in the dominance frontier of its enclosed blocks.
        if (!m_pCompiler->bbIsHandlerBeg(block) &&
            ((blockPreds == nullptr) || (blockPreds->getNextPredEdge() == nullptr)))
        {
            DBG_SSA_JITDUMP("   Has %d preds; skipping.\n", blockPreds == nullptr ? 0 : 1);
            continue;
        }

        // Otherwise, there are > 1 preds.  Each is a candidate B2 in the definition --
        // *unless* it dominates "block"/B3.

        FlowGraphDfsTree*       dfsTree = m_pCompiler->m_dfsTree;
        FlowGraphDominatorTree* domTree = m_pCompiler->m_domTree;

        for (FlowEdge* pred = blockPreds; pred != nullptr; pred = pred->getNextPredEdge())
        {
            BasicBlock* predBlock = pred->getSourceBlock();
            DBG_SSA_JITDUMP("   Considering predecessor " FMT_BB ".\n", predBlock->bbNum);

            if (!dfsTree->Contains(predBlock))
            {
                DBG_SSA_JITDUMP("    Unreachable node\n");
                continue;
            }

            // If we've found a B2, then consider the possible B1's.  We start with
            // B2, since a block dominates itself, then traverse upwards in the dominator
            // tree, stopping when we reach the root, or the immediate dominator of "block"/B3.
            // (Note that we are guaranteed to encounter this immediate dominator of "block"/B3:
            // a predecessor must be dominated by B3's immediate dominator.)
            // Along this way, make "block"/B3 part of the dom frontier of the B1.
            // When we reach this immediate dominator, the definition no longer applies, since this
            // potential B1 *does* dominate "block"/B3, so we stop.
            for (BasicBlock* b1 = predBlock; (b1 != nullptr) && (b1 != block->bbIDom); // !root && !loop
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
            BitVecOps::AddElemD(&m_visitedTraits, m_visited, f->bbPostorderNum);
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
                    if (BitVecOps::TryAddElemD(&m_visitedTraits, m_visited, ff->bbPostorderNum))
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
        if (tree->AsLclVar()->GetLclNum() == lclNum)
        {
            return tree->AsLclVar()->Data();
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

    // PHIs and all the associated nodes do not generate any code so the costs are always 0
    GenTree* phi = new (m_pCompiler, GT_PHI) GenTreePhi(type);
    phi->SetCosts(0, 0);
    GenTree* store = m_pCompiler->gtNewStoreLclVarNode(lclNum, phi);
    store->SetCosts(0, 0);
    store->gtType = type; // TODO-ASG-Cleanup: delete. This quirk avoided diffs from costing-induced tail dup.

    // Create the statement and chain everything in linear order - PHI, STORE_LCL_VAR.
    Statement* stmt = m_pCompiler->gtNewStmt(store);
    stmt->SetTreeList(phi);
    phi->gtNext   = store;
    store->gtPrev = phi;

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
// AddPhiArg: Ensure an existing GT_PHI node contains an appropriate PhiArg
//    for an ssa def arriving via pred
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
    // If there's already a phi arg for this pred, it had better have
    // matching ssaNum, unless this block is a handler entry.
    //
    const bool isHandlerEntry = m_pCompiler->bbIsHandlerBeg(block);

    for (GenTreePhi::Use& use : phi->Uses())
    {
        GenTreePhiArg* const phiArg = use.GetNode()->AsPhiArg();

        if (phiArg->gtPredBB == pred)
        {
            if (phiArg->GetSsaNum() == ssaNum)
            {
                // We already have this (pred, ssaNum) phiArg
                return;
            }

            // Add another ssaNum for this pred?
            // Should only be possible at handler entries.
            //
            noway_assert(isHandlerEntry);
        }
    }

    // Didn't find a match, add a new phi arg
    //
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
 */
void SsaBuilder::InsertPhiFunctions()
{
    JITDUMP("*************** In SsaBuilder::InsertPhiFunctions()\n");

    FlowGraphDfsTree* dfsTree   = m_pCompiler->m_dfsTree;
    BasicBlock**      postOrder = dfsTree->GetPostOrder();
    unsigned          count     = dfsTree->GetPostOrderCount();

    // Compute dominance frontier.
    BlkToBlkVectorMap mapDF(m_allocator);
    ComputeDominanceFrontiers(postOrder, count, &mapDF);
    EndPhase(PHASE_BUILD_SSA_DF);

    // Use the same IDF vector for all blocks to avoid unnecessary memory allocations
    BlkVector blockIDF(m_allocator);

    JITDUMP("Inserting phi functions:\n");

    for (unsigned i = 0; i < count; ++i)
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
        if (block->bbMemoryDef != emptyMemoryKindSet)
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
// RenameDef: Rename a local or memory definition generated by a store/GT_CALL node.
//
// Arguments:
//    defNode - The store/GT_CALL node that generates the definition
//    block   - The basic block that contains `defNode`
//
void SsaBuilder::RenameDef(GenTree* defNode, BasicBlock* block)
{
    assert(defNode->OperIsStore() || defNode->OperIs(GT_CALL));

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
                AddMemoryDefToEHSuccessorPhis(ByrefExposed, block, ssaNum);
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
                    AddMemoryDefToEHSuccessorPhis(GcHeap, block, ssaNum);
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
        varDsc->lvPerSsaData.AllocSsaNum(m_allocator, block, !defNode->IsCall() ? defNode->AsLclVarCommon() : nullptr);

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
    if (!defNode->IsPhiDefn() && block->HasPotentialEHSuccs(m_pCompiler))
    {
        AddDefToEHSuccessorPhis(block, lclNum, ssaNum);
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

void SsaBuilder::AddDefToEHSuccessorPhis(BasicBlock* block, unsigned lclNum, unsigned ssaNum)
{
    assert(block->HasPotentialEHSuccs(m_pCompiler));
    assert(m_pCompiler->lvaTable[lclNum].lvTracked);

    DBG_SSA_JITDUMP("Definition of local V%02u/d:%d in block " FMT_BB
                    " has potential EH successors; adding as phi arg to EH successors\n",
                    lclNum, ssaNum, block->bbNum);

    unsigned lclIndex = m_pCompiler->lvaTable[lclNum].lvVarIndex;

    block->VisitEHSuccs(m_pCompiler, [=](BasicBlock* succ) {
        // Is "lclNum" live on entry to the handler?
        if (!VarSetOps::IsMember(m_pCompiler, succ->bbLiveIn, lclIndex))
        {
            return BasicBlockVisit::Continue;
        }

#ifdef DEBUG
        bool phiFound = false;
#endif
        // A prefix of blocks statements will be SSA definitions.  Search those for "lclNum".
        for (Statement* const stmt : succ->Statements())
        {
            // If the tree is not an SSA def, break out of the loop: we're done.
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTreeLclVar* phiDef = stmt->GetRootNode()->AsLclVar();
            assert(phiDef->IsPhiDefn());

            if (phiDef->GetLclNum() == lclNum)
            {
                // It's the definition for the right local.  Add "ssaNum" to the RHS.
                AddPhiArg(succ, stmt, phiDef->Data()->AsPhi(), lclNum, ssaNum, block);
#ifdef DEBUG
                phiFound = true;
#endif
                break;
            }
        }
        assert(phiFound);

        return BasicBlockVisit::Continue;

    });
}

void SsaBuilder::AddMemoryDefToEHSuccessorPhis(MemoryKind memoryKind, BasicBlock* block, unsigned ssaNum)
{
    assert(block->HasPotentialEHSuccs(m_pCompiler));

    // Don't do anything for a compiler-inserted BBJ_CALLFINALLYRET that is a "leave helper".
    if (block->isBBCallFinallyPairTail())
    {
        return;
    }

    // Otherwise...
    DBG_SSA_JITDUMP("Definition of %s/d:%d in block " FMT_BB
                    " has potential EH successors; adding as phi arg to EH successors.\n",
                    memoryKindNames[memoryKind], ssaNum, block->bbNum);

    block->VisitEHSuccs(m_pCompiler, [=](BasicBlock* succ) {
        // Is memoryKind live on entry to the handler?
        if ((succ->bbMemoryLiveIn & memoryKindSet(memoryKind)) == 0)
        {
            return BasicBlockVisit::Continue;
        }

        // Add "ssaNum" to the phi args of memoryKind.
        BasicBlock::MemoryPhiArg*& handlerMemoryPhi = succ->bbMemorySsaPhiFunc[memoryKind];

#if DEBUG
        if (m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // When sharing phis for GcHeap and ByrefExposed, callers should ask to add phis
            // for ByrefExposed only.
            assert(memoryKind != GcHeap);
            if (memoryKind == ByrefExposed)
            {
                // The GcHeap and ByrefExposed phi funcs should always be in sync.
                assert(handlerMemoryPhi == succ->bbMemorySsaPhiFunc[GcHeap]);
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
            BasicBlock::MemoryPhiArg* curArg = succ->bbMemorySsaPhiFunc[memoryKind];
            while (curArg != nullptr)
            {
                assert(curArg->GetSsaNum() != ssaNum);
                curArg = curArg->m_nextArg;
            }
#endif // DEBUG
            handlerMemoryPhi = new (m_pCompiler) BasicBlock::MemoryPhiArg(ssaNum, handlerMemoryPhi);
        }

        DBG_SSA_JITDUMP("   Added phi arg u:%d for %s to phi defn in handler block " FMT_BB ".\n", ssaNum,
                        memoryKindNames[memoryKind], memoryKind, succ->bbNum);

        if ((memoryKind == ByrefExposed) && m_pCompiler->byrefStatesMatchGcHeapStates)
        {
            // Share the phi between GcHeap and ByrefExposed.
            succ->bbMemorySsaPhiFunc[GcHeap] = handlerMemoryPhi;
        }

        return BasicBlockVisit::Continue;
    });
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
            if (tree->OperIsStore() || tree->OperIs(GT_CALL))
            {
                RenameDef(tree, block);
            }
            // PHI_ARG nodes already have SSA numbers so we only need to check LCL_VAR and LCL_FLD nodes.
            else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD))
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
                if (block->HasPotentialEHSuccs(m_pCompiler))
                {
                    AddMemoryDefToEHSuccessorPhis(memoryKind, block, ssaNum);
                }

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
    block->VisitAllSuccs(m_pCompiler, [this, block](BasicBlock* succ) {
        // Walk the statements for phi nodes.
        for (Statement* const stmt : succ->Statements())
        {
            // A prefix of the statements of the block are phi definition nodes. If we complete processing
            // that prefix, exit.
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTreeLclVar* store  = stmt->GetRootNode()->AsLclVar();
            GenTreePhi*    phi    = store->Data()->AsPhi();
            unsigned       lclNum = store->GetLclNum();
            unsigned       ssaNum = m_renameStack.Top(lclNum);

            AddPhiArg(succ, stmt, phi, lclNum, ssaNum, block);
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

                if (succTry->HasFilter())
                {
                    AddPhiArgsToNewlyEnteredHandler(block, succ, succTry->ebdFilter);
                }

                AddPhiArgsToNewlyEnteredHandler(block, succ, succTry->ebdHndBeg);

                tryInd = succTry->ebdEnclosingTryIndex;
            }
        }

        return BasicBlockVisit::Continue;
    });
}

//------------------------------------------------------------------------
// AddPhiArgsToNewlyEnteredHandler: As part of entering a new try-region, add
// initial values of locals that are live into the handler.
//
// Arguments:
//    predEnterBlock - Predecessor of block in the new try region
//    enterBlock     - Block in the new try region
//    handlerStart   - Handler block of the new try region
//
void SsaBuilder::AddPhiArgsToNewlyEnteredHandler(BasicBlock* predEnterBlock,
                                                 BasicBlock* enterBlock,
                                                 BasicBlock* handlerStart)
{
    for (Statement* const stmt : handlerStart->Statements())
    {
        GenTree* tree = stmt->GetRootNode();

        // Check if the first n of the statements are phi nodes. If not, exit.
        if (!tree->IsPhiDefn())
        {
            break;
        }

        // If the variable is live-out of "blk", and is therefore live on entry to the try-block-start
        // "succ", then we make sure the current SSA name for the var is one of the args of the phi node.
        // If not, go on.
        const unsigned   lclNum    = tree->AsLclVar()->GetLclNum();
        const LclVarDsc* lclVarDsc = m_pCompiler->lvaGetDesc(lclNum);
        if (!lclVarDsc->lvTracked ||
            !VarSetOps::IsMember(m_pCompiler, predEnterBlock->bbLiveOut, lclVarDsc->lvVarIndex))
        {
            continue;
        }

        GenTreePhi* phi    = tree->AsLclVar()->Data()->AsPhi();
        unsigned    ssaNum = m_renameStack.Top(lclNum);

        AddPhiArg(handlerStart, stmt, phi, lclNum, ssaNum, enterBlock);
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
                assert(predEnterBlock->bbMemorySsaNumOut[memoryKind] ==
                       predEnterBlock->bbMemorySsaNumOut[ByrefExposed]);
                assert(handlerStart->bbMemorySsaPhiFunc[ByrefExposed]->m_ssaNum ==
                       predEnterBlock->bbMemorySsaNumOut[memoryKind]);
                handlerMemoryPhi = handlerStart->bbMemorySsaPhiFunc[ByrefExposed];

                continue;
            }

            if (handlerMemoryPhi == BasicBlock::EmptyMemoryPhiDef)
            {
                handlerMemoryPhi =
                    new (m_pCompiler) BasicBlock::MemoryPhiArg(predEnterBlock->bbMemorySsaNumOut[memoryKind]);
            }
            else
            {
                // This path has a potential to introduce redundant phi args, due to multiple
                // preds of the same try-begin block having the same live-out memory def, and/or
                // due to nested try-begins each having preds with the same live-out memory def.
                // Avoid doing quadratic processing on handler phis, and instead live with the
                // occasional redundancy.
                handlerMemoryPhi = new (m_pCompiler)
                    BasicBlock::MemoryPhiArg(predEnterBlock->bbMemorySsaNumOut[memoryKind], handlerMemoryPhi);
            }
            DBG_SSA_JITDUMP("  Added phi arg for %s u:%d from " FMT_BB " in " FMT_BB ".\n", memoryKindNames[memoryKind],
                            predEnterBlock->bbMemorySsaNumOut[memoryKind], predEnterBlock->bbNum, handlerStart->bbNum);
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
        if (!m_pCompiler->m_dfsTree->Contains(block))
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
            : DomTreeVisitor(compiler), m_builder(builder), m_renameStack(renameStack)
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
    visitor.WalkTree(m_pCompiler->m_domTree);
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
    JITDUMP("*************** In SsaBuilder::Build()\n");

    m_visitedTraits = m_pCompiler->m_dfsTree->PostOrderTraits();
    m_visited       = BitVecOps::MakeEmpty(&m_visitedTraits);

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
    InsertPhiFunctions();

    // Rename local variables and collect UD information for each ssa var.
    RenameVariables();
    EndPhase(PHASE_BUILD_SSA_RENAME);

    JITDUMPEXEC(m_pCompiler->DumpSsaSummary());
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
