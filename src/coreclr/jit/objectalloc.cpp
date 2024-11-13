// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                         ObjectAllocator                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "gentree.h"

//------------------------------------------------------------------------
// DoPhase: Run analysis (if object stack allocation is enabled) and then
//          morph each GT_ALLOCOBJ node either into an allocation helper
//          call or stack allocation.
//
// Returns:
//    PhaseStatus indicating, what, if anything, was modified
//
// Notes:
//    Runs only if Compiler::optMethodFlags has flag OMF_HAS_NEWOBJ set.
//
PhaseStatus ObjectAllocator::DoPhase()
{
    if ((comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0)
    {
        JITDUMP("no newobjs in this method; punting\n");
        comp->fgInvalidateDfsTree();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool        enabled       = IsObjectStackAllocationEnabled();
    const char* disableReason = ": global config";

#ifdef DEBUG
    // Allow disabling based on method hash
    //
    if (enabled)
    {
        static ConfigMethodRange JitObjectStackAllocationRange;
        JitObjectStackAllocationRange.EnsureInit(JitConfig.JitObjectStackAllocationRange());
        const unsigned hash = comp->info.compMethodHash();
        enabled &= JitObjectStackAllocationRange.Contains(hash);
        disableReason = ": range config";
    }
#endif

    if (enabled)
    {
        JITDUMP("enabled, analyzing...\n");
        DoAnalysis();
    }
    else
    {
        JITDUMP("disabled%s, punting\n", IsObjectStackAllocationEnabled() ? disableReason : "");
        m_IsObjectStackAllocationEnabled = false;
    }

    const bool didStackAllocate = MorphAllocObjNodes();

    if (didStackAllocate)
    {
        assert(enabled);
        ComputeStackObjectPointers(&m_bitVecTraits);
        RewriteUses();
    }

    // This phase always changes the IR. It may also modify the flow graph.
    //
    comp->fgInvalidateDfsTree();
    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------------
// MarkLclVarAsEscaping : Mark local variable as escaping.
//
//
// Arguments:
//    lclNum  - Escaping pointing local variable number

void ObjectAllocator::MarkLclVarAsEscaping(unsigned int lclNum)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_EscapingPointers, lclNum);
}

//------------------------------------------------------------------------------
// MarkLclVarAsPossiblyStackPointing : Mark local variable as possibly pointing
//                                     to a stack-allocated object.
//
//
// Arguments:
//    lclNum  - Possibly stack-object-pointing local variable number

void ObjectAllocator::MarkLclVarAsPossiblyStackPointing(unsigned int lclNum)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_PossiblyStackPointingPointers, lclNum);
}

//------------------------------------------------------------------------------
// MarkLclVarAsDefinitelyStackPointing : Mark local variable as definitely pointing
//                                       to a stack-allocated object.
//
//
// Arguments:
//    lclNum  - Definitely stack-object-pointing local variable number

void ObjectAllocator::MarkLclVarAsDefinitelyStackPointing(unsigned int lclNum)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_DefinitelyStackPointingPointers, lclNum);
}

//------------------------------------------------------------------------------
// AddConnGraphEdge : Record that the source local variable may point to the same set of objects
//                    as the set pointed to by target local variable.
//
// Arguments:
//    sourceLclNum  - Local variable number of the edge source
//    targetLclNum  - Local variable number of the edge target

void ObjectAllocator::AddConnGraphEdge(unsigned int sourceLclNum, unsigned int targetLclNum)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_ConnGraphAdjacencyMatrix[sourceLclNum], targetLclNum);
}

//------------------------------------------------------------------------
// DoAnalysis: Walk over basic blocks of the method and detect all local
//             variables that can be allocated on the stack.

void ObjectAllocator::DoAnalysis()
{
    assert(m_IsObjectStackAllocationEnabled);
    assert(!m_AnalysisDone);

    if (comp->lvaCount > 0)
    {
        m_EscapingPointers = BitVecOps::MakeEmpty(&m_bitVecTraits);
        m_ConnGraphAdjacencyMatrix =
            new (comp->getAllocator(CMK_ObjectAllocator)) BitSetShortLongRep[comp->lvaCount + m_maxPseudoLocals + 1];

        // If we are doing conditional escape analysis, we also need to compute dominance.
        //
        if (CanHavePseudoLocals())
        {
            assert(comp->m_dfsTree != nullptr);
            assert(comp->m_domTree == nullptr);
            comp->m_domTree = FlowGraphDominatorTree::Build(comp->m_dfsTree);
        }

        MarkEscapingVarsAndBuildConnGraph();
        ComputeEscapingNodes(&m_bitVecTraits, m_EscapingPointers);
    }

    m_AnalysisDone = true;
}

//------------------------------------------------------------------------------
// NewPseudoLocal: return index of a new pseudo local.
//
// Returns:
//   index to use, or BAD_VAR_NUM if no more indices are available.
//
unsigned ObjectAllocator::NewPseudoLocal()
{
    unsigned result = BAD_VAR_NUM;
    if (m_numPseudoLocals < m_maxPseudoLocals)
    {
        result = comp->lvaCount + m_numPseudoLocals;
        m_numPseudoLocals++;
    }
    return result;
}

//------------------------------------------------------------------------------
// IsGuarded: does evaluation of `tree` depend on a failed GDV check?
//
// Arguments:
//   tree -- tree in question
//   info -- [out] closest enclosing guard info, if method returns true
//
// Returns:
//   true if tree is only evaluated if a GDV check fails. Returns the closest
//   such check (in terms of dominators), along with info on the check.
//
// Notes:
//   * There may be other checks higher in the tree, consider returning all
//     checks rather than just the closest.
//   * Possibly try and recognize user-written type checks...?
//   * Consider bailing out at some point, for deep dominator trees.
//   * R2R/NAOT cases where compile time and runtime handles diverge
//
bool ObjectAllocator::IsGuarded(BasicBlock* block, GenTree* tree, GuardInfo* info)
{
    // Walk up the dominator tree....
    //
    for (BasicBlock* idomBlock = block->bbIDom; idomBlock != nullptr; idomBlock = idomBlock->bbIDom)
    {
        JITDUMP("... checking " FMT_BB, idomBlock->bbNum);
        if (!idomBlock->KindIs(BBJ_COND))
        {
            JITDUMP("... not cond\n");
            continue;
        }

        // We require that one idomBlock successor *not* dominate.
        // (otherwise idomBlock this could be the top of a diamond where both outcomes reach block).
        //
        const bool trueSuccessorDominates  = comp->m_domTree->Dominates(idomBlock->GetTrueTarget(), block);
        const bool falseSuccessorDominates = comp->m_domTree->Dominates(idomBlock->GetFalseTarget(), block);

        if (trueSuccessorDominates && falseSuccessorDominates)
        {
            JITDUMP("... both successors dominate\n");
            continue;
        }

        assert(trueSuccessorDominates || falseSuccessorDominates);

        // Now examine the condition
        //
        Statement* const stmt = idomBlock->lastStmt();

        if (stmt == nullptr)
        {
            JITDUMP("... no last stmt\n");
            return false;
        }

        GenTree* const jumpTree = stmt->GetRootNode();

        if (!jumpTree->OperIs(GT_JTRUE))
        {
            JITDUMP("... no JTRUE\n");
            return false;
        }

        GenTree* const tree = jumpTree->AsOp()->gtOp1;

        // Must be an equality or inequality
        //
        if (!tree->OperIs(GT_NE, GT_EQ))
        {
            JITDUMP("... not NE/EQ\n");
            continue;
        }

        GenTree* op1     = tree->AsOp()->gtOp1;
        GenTree* op2     = tree->AsOp()->gtOp2;
        bool     swapped = false;

        // gdv creates NE(hnd, indir(locl))
        // but let's not rely on that
        //
        if (!op1->OperIs(GT_IND))
        {
            swapped = true;
            std::swap(op1, op2);
        }

        if (!op1->OperIs(GT_IND))
        {
            JITDUMP("... no JTRUE(cmp(ind, ...))\n");
            continue;
        }

        if (!op1->TypeIs(TYP_I_IMPL))
        {
            JITDUMP("... no JTRUE(cmp(ind:int, ...))\n");
            continue;
        }

        GenTree* const addr = op1->AsIndir()->Addr();

        if (!addr->TypeIs(TYP_REF))
        {
            JITDUMP("... no JTRUE(cmp(ind:int(*:ref), ...))\n");
            continue;
        }

        if (!addr->OperIs(GT_LCL_VAR))
        {
            JITDUMP("... no JTRUE(cmp(ind:int(lcl:ref), ...))\n");
            continue;
        }

        if (!op2->IsIconHandle(GTF_ICON_CLASS_HDL))
        {
            JITDUMP("... no JTRUE(cmp(ind:int(lcl:ref), clsHnd))\n");
            continue;
        }

        bool isReachableOnGDVFailure =
            (trueSuccessorDominates && tree->OperIs(GT_NE)) || (falseSuccessorDominates && tree->OperIs(GT_EQ));

        if (!isReachableOnGDVFailure)
        {
            JITDUMP("... guarded by successful GDV\n");
            continue;
        }

        // Passed the checks... fill in the info.
        //
        info->m_local  = addr->AsLclVar()->GetLclNum();
        bool isNonNull = false;
        bool isExact   = false;
        info->m_type   = (CORINFO_CLASS_HANDLE)op2->AsIntCon()->gtIconVal;

        JITDUMP("... under guard V%02u\n", info->m_local);

        return true;
    }

    JITDUMP("... no more doms\n");

    return false;
}

//------------------------------------------------------------------------------
// RecordAppearance: note info about an enumerator var appearance
//
// Arguments:
//   lclNum -- enumerator var
//   block  -- block holding the stmt
//   stmt   -- stmt holding the use
//   use    -- local var reference
//   isDef  -- true if this is a def
//
void ObjectAllocator::RecordAppearance(unsigned lclNum, BasicBlock* block, Statement* stmt, GenTree** use, bool isDef)
{
    unsigned pseudoLocal = BAD_VAR_NUM;
    if (!m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
    {
        return;
    }

    GuardInfo info;
    if (!m_GuardMap.Lookup(pseudoLocal, &info))
    {
        return;
    }

    EnumeratorVarAppearance e(block, stmt, use, isDef);
    info.m_appearances->push_back(e);
}

//------------------------------------------------------------------------------
// CanClone: check that cloning can remove all escaping references and
//   is a reasonble thing to do
//
// Arguments:
//   info -- info about the cloning opportunity
//
// Returns:
//   true if cloning can remove all escaping references
//   and if cloning is likely to be a good perf/size tradeoff
//
bool ObjectAllocator::CanClone(GuardInfo& info)
{
    // The allocation site must not be in a loop (stack allocation limitation)
    //
    // Note if we can prove non-escape but can't stack allocate, we might be
    // able to light up an "object is thread exclusive" mode and effectively
    // promote the fields anyways.
    //
    if (info.m_allocBlock->HasFlag(BBF_BACKWARD_JUMP))
    {
        JITDUMP("allocation block " FMT_BB " is (possibly) in a loop\n", info.m_allocBlock->bbNum);
        return false;
    }

    // The guard variable needs to have at most one definition.
    //
    BasicBlock* defBlock = nullptr;
    for (EnumeratorVarAppearance& a : *info.m_appearances)
    {
        if (!a.m_isDef)
        {
            continue;
        }

        if (defBlock != nullptr)
        {
            JITDUMP("V%02u multiply defined: " FMT_BB " and " FMT_BB "\n", info.m_local, defBlock->bbNum,
                    a.m_block->bbNum);
            return false;
        }

        defBlock = a.m_block;
    }

    JITDUMP("V%02u has single def in " FMT_BB "\n", info.m_local, defBlock->bbNum);

    // The definition block must dominate all the uses.
    //
    for (EnumeratorVarAppearance& a : *info.m_appearances)
    {
        if (a.m_isDef)
        {
            continue;
        }

        if (!comp->m_domTree->Dominates(defBlock, a.m_block))
        {
            JITDUMP("V%02u use in " FMT_BB " not dominated by def " FMT_BB "\n", info.m_local, a.m_block->bbNum,
                    defBlock->bbNum);
            return false;
        }
    }

    JITDUMP("The def dominates all the uses\n");

    // The def block must post-dominate the allocation site, and
    // the allocation site should not dominate the def block.
    // (if it does, our optimization does not require cloning as
    // there should be only one reaching def...)
    //
    if (comp->m_domTree->Dominates(info.m_allocBlock, defBlock))
    {
        JITDUMP("Unexpected, alloc site " FMT_BB " dominates def block " FMT_BB "\n", info.m_allocBlock->bbNum,
                defBlock->bbNum);

        return false;
    }

    // We expect to be able to follow all paths from alloc block to defBlock
    // without reaching "beyond" def block.
    //
    // Because we are inside a GDV hammock, we do not expect to see a normal
    // flow path from alloc block that can bypass defBlock. For now we trust
    // that is the case.
    //
    // toVisit: blocks we need to visit to determine extent of cloning
    // visited: block we will need to clone
    // toVisitTryEntry: subset of above that are try entries.
    //
    ArrayStack<BasicBlock*> toVisit(comp->getAllocator(CMK_ObjectAllocator));
    ArrayStack<BasicBlock*> visited(comp->getAllocator(CMK_ObjectAllocator));
    ArrayStack<BasicBlock*> toVisitTryEntry(comp->getAllocator(CMK_ObjectAllocator));

    comp->EnsureBasicBlockEpoch();
    BlockSet visitedBlocks(BlockSetOps::MakeEmpty(comp));
    toVisit.Push(info.m_allocBlock);

    while (toVisit.Height() > 0)
    {
        BasicBlock* const visitBlock = toVisit.Pop();
        if (!BlockSetOps::TryAddElemD(comp, visitedBlocks, visitBlock->bbNum))
        {
            continue;
        }
        visited.Push(visitBlock);

        if (comp->bbIsTryBeg(visitBlock))
        {
            toVisitTryEntry.Push(visitBlock);
        }

        if (visitBlock == defBlock)
        {
            continue;
        }

        JITDUMP("walking through " FMT_BB "\n", visitBlock->bbNum);

        // All successors must be defBlock, or dominated by alloc block,
        // otherwise there is a path from alloc block that avoids def block.
        //
        for (BasicBlock* const succ : visitBlock->Succs())
        {
            if (BlockSetOps::IsMember(comp, visitedBlocks, succ->bbNum))
            {
                continue;
            }
            toVisit.Push(succ);
        }
    }

    JITDUMP("def block " FMT_BB " post-dominates allocation site " FMT_BB "\n", defBlock->bbNum,
            info.m_allocBlock->bbNum);

    // -1 here since we won't need to clone the allocation site itself.
    //
    JITDUMP("allocation side cloning: %u blocks\n", visited.Height() - 1);

    // Determine the initial extent of the cloned region dominated by
    // the def block.
    //
    // Walk back from each use block until we hit closure.
    //
    // We should also be able to walk forward from defBlock to all the uses,
    // skipping any successors that are failed GDVs.
    //
    for (EnumeratorVarAppearance& a : *info.m_appearances)
    {
        if (a.m_isDef)
        {
            continue;
        }

        toVisit.Push(a.m_block);
    }

    while (toVisit.Height() > 0)
    {
        BasicBlock* const visitBlock = toVisit.Pop();
        if (!BlockSetOps::TryAddElemD(comp, visitedBlocks, visitBlock->bbNum))
        {
            continue;
        }
        visited.Push(visitBlock);

        if (comp->bbIsTryBeg(visitBlock))
        {
            toVisitTryEntry.Push(visitBlock);
        }

        JITDUMP("walking back through " FMT_BB "\n", visitBlock->bbNum);

        for (FlowEdge* predEdge = comp->BlockPredsWithEH(visitBlock); predEdge != nullptr;
             predEdge           = predEdge->getNextPredEdge())
        {
            BasicBlock* const predBlock = predEdge->getSourceBlock();

            // We should not be able to reach an un-dominated block.
            // (consider eh paths?)
            //
            assert(comp->m_domTree->Dominates(defBlock, predBlock));
            if (BlockSetOps::IsMember(comp, visitedBlocks, predBlock->bbNum))
            {
                continue;
            }
            toVisit.Push(predBlock);
        }
    }

    JITDUMP("total cloning including all enumerator uses: %u blocks\n", visited.Height() - 1);
    unsigned numberOfEHRegionsToClone = 0;

    // Now we need to check if the entire cloning extent is within the
    // same try region, or crosses into a try somewhere.
    //
    // If the set of blocks to clone crosses into a try we need to
    // expand the set of blocks to clone to the entire try plus any
    // enclosed regions, plus the associated handler / filter and any
    // regions they enclose, plus any callfinallies that follow.
    //
    // This is necessary because try regions can't have multiple entries, or
    // share parts in any meaningful way.
    //
    // We may need to make this more efficient. Also should probably abstract
    // it out somehow.
    //
    while (toVisitTryEntry.Height() > 0)
    {
        numberOfEHRegionsToClone++;
        BasicBlock* const block = toVisitTryEntry.Pop();
        assert(comp->bbIsTryBeg(block));

        unsigned const tryIndex = block->getTryIndex();
        EHblkDsc*      ebd      = comp->ehGetDsc(tryIndex);

        JITDUMP(FMT_BB " is try region entry; walking full extent of EH#%02u\n", block->bbNum, tryIndex);
        BasicBlock* const firstTryBlock = ebd->ebdTryBeg;
        BasicBlock* const lastTryBlock  = ebd->ebdTryLast;

        assert(firstTryBlock == block);

        JITDUMP("Walking try region for EH#%02u\n", tryIndex);
        for (BasicBlock* const block : comp->Blocks(firstTryBlock, lastTryBlock))
        {
            // assert...?
            if (!comp->bbInTryRegions(tryIndex, block))
            {
                continue;
            }

            if (BlockSetOps::TryAddElemD(comp, visitedBlocks, block->bbNum))
            {
                JITDUMP("adding try region block " FMT_BB "\n", block->bbNum);
                visited.Push(block);
            }
        }

        // Walk handler for this region and any enclosing mutual-protect regions.
        //
        unsigned index = tryIndex;
        while (index != EHblkDsc::NO_ENCLOSING_INDEX)
        {
            EHblkDsc* const enclosingEbd = comp->ehGetDsc(index);

            // Stop when we find a region with a different try entry block.
            //
            if (!EHblkDsc::ebdIsSameILTry(ebd, enclosingEbd))
            {
                assert(ebd->ebdTryBeg != block);
                break;
            }
            else
            {
                assert(ebd->ebdTryBeg == block);
            }

            if (index != tryIndex)
            {
                JITDUMP("Found enclosing mutual-protect try EH#%02u\n", index);
            }

            BasicBlock* const firstHndBlock = enclosingEbd->ebdHndBeg;
            BasicBlock* const lastHndBlock  = enclosingEbd->ebdHndLast;

            JITDUMP("walking handler region for EH#%02u [" FMT_BB " ... " FMT_BB "]\n", index, firstHndBlock->bbNum,
                    lastHndBlock->bbNum);

            for (BasicBlock* const block : comp->Blocks(firstHndBlock, lastHndBlock))
            {
                // assert..?
                if (!comp->bbInHandlerRegions(tryIndex, block))
                {
                    continue;
                }

                if (BlockSetOps::TryAddElemD(comp, visitedBlocks, block->bbNum))
                {
                    if (comp->bbIsTryBeg(block))
                    {
                        JITDUMP("found try nested in handler at " FMT_BB "\n", block->bbNum);
                        toVisitTryEntry.Push(block);
                    }

                    JITDUMP("adding handler region block " FMT_BB "\n", block->bbNum);
                    visited.Push(block);
                }
            }

            // Walk filter, if any
            //
            if (enclosingEbd->HasFilter())
            {
                BasicBlock* const firstFltBlock = enclosingEbd->ebdFilter;
                BasicBlock* const lastFltBlock  = enclosingEbd->BBFilterLast();

                JITDUMP("walking filter region for EH#%02u\n", index);

                for (BasicBlock* const block : comp->Blocks(firstFltBlock, lastFltBlock))
                {
                    if (BlockSetOps::TryAddElemD(comp, visitedBlocks, block->bbNum))
                    {
                        JITDUMP("adding filter region block " FMT_BB "\n", block->bbNum);
                        visited.Push(block);
                    }
                }
            }

            // Advance to enclosing region
            //
            index = enclosingEbd->ebdEnclosingTryIndex;
        }

        // Walk the callfinally region for the try, if it has any.
        //
        if (ebd->HasFinallyHandler())
        {
            BasicBlock* firstCallFinallyRangeBlock = nullptr;
            BasicBlock* lastCallFinallyRangeBlock  = nullptr;
            comp->ehGetCallFinallyBlockRange(tryIndex, &firstCallFinallyRangeBlock, &lastCallFinallyRangeBlock);

            // Note this range is potentially quite broad, and needs filtering...
            // Instead perhaps just walk preds of the handler?
            //
            JITDUMP("walking callfinally region for EH#%02u [" FMT_BB " ... " FMT_BB "]\n", tryIndex,
                    firstCallFinallyRangeBlock->bbNum, lastCallFinallyRangeBlock->bbNum);

            for (BasicBlock* const block : comp->Blocks(firstCallFinallyRangeBlock, lastCallFinallyRangeBlock))
            {
                if (block->KindIs(BBJ_CALLFINALLY) && block->TargetIs(ebd->ebdHndBeg))
                {
                    if (BlockSetOps::TryAddElemD(comp, visitedBlocks, block->bbNum))
                    {
                        JITDUMP("adding callfinally block " FMT_BB "\n", block->bbNum);
                        visited.Push(block);
                        continue;
                    }
                }

                if (block->KindIs(BBJ_CALLFINALLYRET) && block->Prev()->TargetIs(ebd->ebdHndBeg))
                {
                    JITDUMP("found callfinally tail block with right target " FMT_BB "\n", block->bbNum);
                    if (BlockSetOps::TryAddElemD(comp, visitedBlocks, block->bbNum))
                    {
                        JITDUMP("adding callfinally tail block " FMT_BB "\n", block->bbNum);
                        visited.Push(block);
                        continue;
                    }
                }
            }
        }
    }

    JITDUMP("total cloning including all uses and subsequent EH: %u blocks\n", visited.Height() - 1);

    // Todo: some kind of costing to decide if this amount of cloning is worth the trouble.
    //
    // We generally expect that if we need to clone multiple EH regions, there is one region
    // that encloses the others. For now we'll simply check that we only need to clone one EH region.

    if (numberOfEHRegionsToClone > 1)
    {
        JITDUMP("Too many EH regions to clone (%u)\n", numberOfEHRegionsToClone);
        return false;
    }

    comp->Metrics.EnumeratorGDVCanCloneToEnsureNoEscape++;

    // Transfer the extent info to the guard info object...
    // Set of blocks to clone
    // EH region to clone (if any)

    return false;
}

//------------------------------------------------------------------------------
// MarkEscapingVarsAndBuildConnGraph : Walk the trees of the method and mark any ref/byref/i_impl
//                                     local variables that may escape. Build a connection graph
//                                     for ref/by_ref/i_impl local variables.
//
// Arguments:
//    sourceLclNum  - Local variable number of the edge source
//    targetLclNum  - Local variable number of the edge target
//
// Notes:
//     The connection graph has an edge from local variable s to local variable t if s may point
//     to the objects t points to at some point in the method. It's a simplified version
//     of the graph described in this paper:
//     https://www.cc.gatech.edu/~harrold/6340/cs6340_fall2009/Readings/choi99escape.pdf
//     We currently don't have field edges and the edges we do have are called "deferred" in the paper.

void ObjectAllocator::MarkEscapingVarsAndBuildConnGraph()
{
    class BuildConnGraphVisitor final : public GenTreeVisitor<BuildConnGraphVisitor>
    {
        ObjectAllocator* m_allocator;
        BasicBlock*      m_block;
        Statement*       m_stmt;

    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
            ComputeStack  = true,
        };

        BuildConnGraphVisitor(ObjectAllocator* allocator, BasicBlock* block, Statement* stmt)
            : GenTreeVisitor<BuildConnGraphVisitor>(allocator->comp)
            , m_allocator(allocator)
            , m_block(block)
            , m_stmt(stmt)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const   tree   = *use;
            unsigned const   lclNum = tree->AsLclVarCommon()->GetLclNum();
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            if (varDsc->lvIsEnumerator)
            {
                JITDUMP("Found enumerator V%02u %s at [%06u]\n", lclNum, tree->OperIsLocalStore() ? "def" : "use",
                        m_compiler->dspTreeID(tree));
                m_allocator->RecordAppearance(lclNum, m_block, m_stmt, use, tree->OperIsLocalStore());
            }

            // If this local already escapes, no need to look further.
            //
            if (m_allocator->CanLclVarEscape(lclNum))
            {
                return Compiler::fgWalkResult::WALK_CONTINUE;
            }

            bool lclEscapes = true;

            if (tree->OperIsLocalStore())
            {
                lclEscapes = false;

                // See if this is enumeratorLocal = ALLOCOBJ
                // If so we will set up a pseudo-var for tracking conditional escapes.
                //
                // Since we are running in RPO, this allocation site will be seen before
                // any guarded uses.
                //
                GenTree* const data = tree->AsLclVarCommon()->Data();

                // Note this may be a conditional allocation. We will try and track the conditions
                // under which it escapes. GDVs are a nice subset because the conditions are stylized,
                // and the condition analysis seems tractable, and we expect the un-inlined failed
                // GDVs to be the main causes of escapes.
                //
                // TODO (perhaps): check this allocation is guarded (though we likely want success, not failure)
                //
                if (data->OperIs(GT_ALLOCOBJ) && m_compiler->hasImpEnumeratorGdvLocalMap())
                {
                    // This is the allocation of concrete enumerator under GDV.
                    // Find the local that will represent its uses (we have kept track of this during
                    // importation and GDV expansion). Note it is usually *not* lclNum.
                    //
                    // We will keep special track of all accesses to this local.
                    //
                    Compiler::NodeToUnsignedMap* const map             = m_compiler->getImpEnumeratorGdvLocalMap();
                    unsigned                           enumeratorLocal = BAD_VAR_NUM;
                    if (map->Lookup(data, &enumeratorLocal))
                    {
                        // If it turns out we can't stack allocate this object even if it does not escape
                        // then don't bother setting up tracking.
                        //
                        CORINFO_CLASS_HANDLE clsHnd = data->AsAllocObj()->gtAllocObjClsHnd;
                        const char*          reason = nullptr;

                        if (m_allocator->CanAllocateLclVarOnStack(enumeratorLocal, clsHnd, &reason))
                        {
                            // We are going to conditionally track accesses to this local via a pseudo local.
                            // We should have been able to predict in advance how many we'll need.
                            //
                            const unsigned pseudoLocal = m_allocator->NewPseudoLocal();
                            assert(pseudoLocal != BAD_VAR_NUM);
                            bool added = m_allocator->m_EnumeratorLocalToPseudoLocalMap.AddOrUpdate(enumeratorLocal,
                                                                                                    pseudoLocal);
                            assert(added);

                            // We will query this info if we see CALL(enumeratorLocal)
                            // during subsequent analysis, to verify that access is
                            // under the same type guard.
                            //
                            CompAllocator alloc(m_compiler->getAllocator(CMK_ObjectAllocator));
                            GuardInfo     info;
                            info.m_local       = enumeratorLocal;
                            info.m_type        = clsHnd;
                            info.m_appearances = new (alloc) jitstd::vector<EnumeratorVarAppearance>(alloc);
                            info.m_allocBlock  = m_block;
                            m_allocator->m_GuardMap.Set(pseudoLocal, info);

                            JITDUMP(
                                "Enumerator allocation [%06u]: will track accesses to V%02u guarded by type %s via P%02u\n",
                                m_compiler->dspTreeID(data), enumeratorLocal, m_compiler->eeGetClassName(clsHnd),
                                pseudoLocal);
                        }
                        else
                        {
                            JITDUMP(
                                "Enumerator allocation [%06u]: enumerator type %s cannot be stack allocated, so not tracking enumerator local V%02u\n",
                                m_compiler->dspTreeID(data), m_compiler->eeGetClassName(clsHnd), enumeratorLocal);
                        }
                    }
                }
            }
            else if (tree->OperIs(GT_LCL_VAR) && tree->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL))
            {
                assert(tree == m_ancestors.Top());
                if (!m_allocator->CanLclVarEscapeViaParentStack(&m_ancestors, lclNum, m_block))
                {
                    lclEscapes = false;
                }
            }

            if (lclEscapes)
            {
                if (!m_allocator->CanLclVarEscape(lclNum))
                {
                    JITDUMP("V%02u first escapes via [%06u]\n", lclNum, m_compiler->dspTreeID(tree));
                }
                m_allocator->MarkLclVarAsEscaping(lclNum);
            }

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    for (unsigned int lclNum = 0; lclNum < comp->lvaCount; ++lclNum)
    {
        var_types type = comp->lvaTable[lclNum].TypeGet();

        if (type == TYP_REF || genActualType(type) == TYP_I_IMPL || type == TYP_BYREF)
        {
            m_ConnGraphAdjacencyMatrix[lclNum] = BitVecOps::MakeEmpty(&m_bitVecTraits);

            if (comp->lvaTable[lclNum].IsAddressExposed())
            {
                JITDUMP("   V%02u is address exposed\n", lclNum);
                MarkLclVarAsEscaping(lclNum);
            }
        }
        else
        {
            // Variable that may not point to objects will not participate in our analysis.
            m_ConnGraphAdjacencyMatrix[lclNum] = BitVecOps::UninitVal();
        }
    }

    for (unsigned int p = 0; p < m_maxPseudoLocals; p++)
    {
        m_ConnGraphAdjacencyMatrix[p + comp->lvaCount] = BitVecOps::MakeEmpty(&m_bitVecTraits);
    }

    // We should have computed the DFS tree already.
    //
    FlowGraphDfsTree* const dfs = comp->m_dfsTree;
    assert(dfs != nullptr);

    // Walk in RPO
    //
    for (unsigned i = dfs->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* const block = dfs->GetPostOrder(i - 1);
        for (Statement* const stmt : block->Statements())
        {
            BuildConnGraphVisitor buildConnGraphVisitor(this, block, stmt);
            buildConnGraphVisitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }
}

//------------------------------------------------------------------------------
// ComputeEscapingNodes : Given an initial set of escaping nodes, update it to contain the full set
//                        of escaping nodes by computing nodes reachable from the given set.
//
// Arguments:
//    bitVecTraits              - Bit vector traits
//    escapingNodes  [in/out]   - Initial set of escaping nodes

void ObjectAllocator::ComputeEscapingNodes(BitVecTraits* bitVecTraits, BitVec& escapingNodes)
{
    BitSetShortLongRep escapingNodesToProcess = BitVecOps::MakeCopy(bitVecTraits, escapingNodes);

    auto computeClosure = [&]() {
        JITDUMP("\nComputing escape closure\n\n");
        bool               doOneMoreIteration = true;
        BitSetShortLongRep newEscapingNodes   = BitVecOps::UninitVal();
        unsigned int       lclNum;

        while (doOneMoreIteration)
        {
            BitVecOps::Iter iterator(bitVecTraits, escapingNodesToProcess);
            doOneMoreIteration = false;

            while (iterator.NextElem(&lclNum))
            {
                if (m_ConnGraphAdjacencyMatrix[lclNum] != nullptr)
                {
                    doOneMoreIteration = true;

                    // newEscapingNodes         = adjacentNodes[lclNum]
                    BitVecOps::Assign(bitVecTraits, newEscapingNodes, m_ConnGraphAdjacencyMatrix[lclNum]);
                    // newEscapingNodes         = newEscapingNodes \ escapingNodes
                    BitVecOps::DiffD(bitVecTraits, newEscapingNodes, escapingNodes);
                    // escapingNodesToProcess   = escapingNodesToProcess U newEscapingNodes
                    BitVecOps::UnionD(bitVecTraits, escapingNodesToProcess, newEscapingNodes);
                    // escapingNodes = escapingNodes U newEscapingNodes
                    BitVecOps::UnionD(bitVecTraits, escapingNodes, newEscapingNodes);
                    // escapingNodesToProcess   = escapingNodesToProcess \ { lclNum }
                    BitVecOps::RemoveElemD(bitVecTraits, escapingNodesToProcess, lclNum);

#ifdef DEBUG
                    // Print the first witness to new escapes.
                    //
                    if (!BitVecOps::IsEmpty(bitVecTraits, newEscapingNodes))
                    {
                        BitVecOps::Iter iterator(bitVecTraits, newEscapingNodes);
                        unsigned int    newLclNum;
                        while (iterator.NextElem(&newLclNum))
                        {
                            // Note P's never are sources of assignments...
                            JITDUMP("%c%02u causes V%02u to escape\n", lclNum >= comp->lvaCount ? 'P' : 'V', lclNum,
                                    newLclNum);
                        }
                    }
#endif
                }
            }
        }
    };

    computeClosure();

    // See if any enumerator locals are currently unescaping and also assigned
    // to a pseudolocal... if so, by suitable cloning and rewriting, we can make
    // sure those locals do not actually escape.
    //
    for (unsigned p = 0; p < m_numPseudoLocals; p++)
    {
        unsigned const  pseudoLocal            = p + comp->lvaCount;
        unsigned        lclNum                 = BAD_VAR_NUM;
        BitVec          pseudoLocalAdjacencies = m_ConnGraphAdjacencyMatrix[pseudoLocal];
        BitVecOps::Iter iterator(bitVecTraits, pseudoLocalAdjacencies);
        while (iterator.NextElem(&lclNum))
        {
            if (BitVecOps::IsMember(bitVecTraits, escapingNodes, lclNum))
            {
                JITDUMP("   V%02u escapes independently of P%02u\n", lclNum, pseudoLocal);
                continue;
            }

            GuardInfo  info;
            const bool hasInfo  = m_GuardMap.Lookup(pseudoLocal, &info);
            bool       canClone = false;

            if (hasInfo)
            {
                JITDUMP("   P%02u is guarding the escape of V%02u\n", pseudoLocal, lclNum);
                JITDUMP("   Escapes only when V%02u.Type NE %s\n", info.m_local, comp->eeGetClassName(info.m_type));
                JITDUMP("   V%02u has %u appearances\n", info.m_local, info.m_appearances->size());

                // We may be able to clone and specialize the enumerator uses to ensure
                // that the allocated enumerator does not escape.
                //
                comp->Metrics.EnumeratorGDVProvisionalNoEscape++;

                // See if cloning is viable...
                //
                canClone = CanClone(info);
            }

            if (!canClone)
            {
                JITDUMP("   not optimizing, so will mark P%02u as escaping\n", pseudoLocal);
                MarkLclVarAsEscaping(pseudoLocal);
                BitVecOps::AddElemD(bitVecTraits, escapingNodesToProcess, pseudoLocal);
            }
        }
    }

    computeClosure();
}

//------------------------------------------------------------------------------
// ComputeStackObjectPointers : Given an initial set of possibly stack-pointing nodes,
//                              and an initial set of definitely stack-pointing nodes,
//                              update both sets by computing nodes reachable from the
//                              given set in the reverse connection graph.
//
// Arguments:
//    bitVecTraits                    - Bit vector traits

void ObjectAllocator::ComputeStackObjectPointers(BitVecTraits* bitVecTraits)
{
    bool changed = true;

    while (changed)
    {
        changed = false;
        for (unsigned int lclNum = 0; lclNum < comp->lvaCount; ++lclNum)
        {
            LclVarDsc* lclVarDsc = comp->lvaGetDesc(lclNum);
            var_types  type      = lclVarDsc->TypeGet();

            if (type == TYP_REF || type == TYP_I_IMPL || type == TYP_BYREF)
            {
                if (!MayLclVarPointToStack(lclNum) &&
                    !BitVecOps::IsEmptyIntersection(bitVecTraits, m_PossiblyStackPointingPointers,
                                                    m_ConnGraphAdjacencyMatrix[lclNum]))
                {
                    // We discovered a new pointer that may point to the stack.
                    MarkLclVarAsPossiblyStackPointing(lclNum);

                    // Check if this pointer always points to the stack.
                    // For OSR the reference may be pointing at the heap-allocated Tier0 version.
                    //
                    if ((lclVarDsc->lvSingleDef == 1) && !comp->opts.IsOSR())
                    {
                        // Check if we know what is assigned to this pointer.
                        unsigned bitCount = BitVecOps::Count(bitVecTraits, m_ConnGraphAdjacencyMatrix[lclNum]);
                        assert(bitCount <= 1);
                        if (bitCount == 1)
                        {
                            BitVecOps::Iter iter(bitVecTraits, m_ConnGraphAdjacencyMatrix[lclNum]);
                            unsigned        rhsLclNum = 0;
                            iter.NextElem(&rhsLclNum);

                            if (DoesLclVarPointToStack(rhsLclNum))
                            {
                                // The only store to lclNum local is the definitely-stack-pointing
                                // rhsLclNum local so lclNum local is also definitely-stack-pointing.
                                MarkLclVarAsDefinitelyStackPointing(lclNum);
                            }
                        }
                    }
                    changed = true;
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// MorphAllocObjNodes: Morph each GT_ALLOCOBJ node either into an
//                     allocation helper call or stack allocation.
//
// Returns:
//    true if any allocation was done as a stack allocation.
//
// Notes:
//    Runs only over the blocks having bbFlags BBF_HAS_NEWOBJ set.

bool ObjectAllocator::MorphAllocObjNodes()
{
    bool didStackAllocate             = false;
    m_PossiblyStackPointingPointers   = BitVecOps::MakeEmpty(&m_bitVecTraits);
    m_DefinitelyStackPointingPointers = BitVecOps::MakeEmpty(&m_bitVecTraits);

    for (BasicBlock* const block : comp->Blocks())
    {
        const bool basicBlockHasNewObj       = block->HasFlag(BBF_HAS_NEWOBJ);
        const bool basicBlockHasBackwardJump = block->HasFlag(BBF_BACKWARD_JUMP);
#ifndef DEBUG
        if (!basicBlockHasNewObj)
        {
            continue;
        }
#endif // DEBUG

        for (Statement* const stmt : block->Statements())
        {
            GenTree* stmtExpr = stmt->GetRootNode();
            GenTree* data     = nullptr;

            bool canonicalAllocObjFound = false;

            if (stmtExpr->OperIs(GT_STORE_LCL_VAR) && stmtExpr->TypeIs(TYP_REF))
            {
                data = stmtExpr->AsLclVar()->Data();

                if (data->OperGet() == GT_ALLOCOBJ)
                {
                    canonicalAllocObjFound = true;
                }
            }

            if (canonicalAllocObjFound)
            {
                assert(basicBlockHasNewObj);
                //------------------------------------------------------------------------
                // We expect the following expression tree at this point
                //  STMTx (IL 0x... ???)
                //    * STORE_LCL_VAR   ref
                //    \--*  ALLOCOBJ  ref
                //       \--*  CNS_INT(h) long
                //------------------------------------------------------------------------

                GenTreeAllocObj*     asAllocObj   = data->AsAllocObj();
                unsigned int         lclNum       = stmtExpr->AsLclVar()->GetLclNum();
                CORINFO_CLASS_HANDLE clsHnd       = data->AsAllocObj()->gtAllocObjClsHnd;
                CORINFO_CLASS_HANDLE stackClsHnd  = clsHnd;
                const bool           isValueClass = comp->info.compCompHnd->isValueClass(clsHnd);
                const char*          onHeapReason = nullptr;
                bool                 canStack     = false;

                if (isValueClass)
                {
                    comp->Metrics.NewBoxedValueClassHelperCalls++;
                    stackClsHnd = comp->info.compCompHnd->getTypeForBoxOnStack(clsHnd);
                }
                else
                {
                    comp->Metrics.NewRefClassHelperCalls++;
                }

                // Don't attempt to do stack allocations inside basic blocks that may be in a loop.
                //
                if (!IsObjectStackAllocationEnabled())
                {
                    onHeapReason = "[object stack allocation disabled]";
                    canStack     = false;
                }
                else if (basicBlockHasBackwardJump)
                {
                    onHeapReason = "[alloc in loop]";
                    canStack     = false;
                }
                else if (!CanAllocateLclVarOnStack(lclNum, clsHnd, &onHeapReason))
                {
                    // reason set by the call
                    canStack = false;
                }
                else if (stackClsHnd == NO_CLASS_HANDLE)
                {
                    assert(isValueClass);
                    onHeapReason = "[no class handle for this boxed value class]";
                    canStack     = false;
                }
                else
                {
                    JITDUMP("Allocating V%02u on the stack\n", lclNum);
                    canStack = true;
                    const unsigned int stackLclNum =
                        MorphAllocObjNodeIntoStackAlloc(asAllocObj, stackClsHnd, isValueClass, block, stmt);
                    m_HeapLocalToStackLocalMap.AddOrUpdate(lclNum, stackLclNum);
                    // We keep the set of possibly-stack-pointing pointers as a superset of the set of
                    // definitely-stack-pointing pointers. All definitely-stack-pointing pointers are in both sets.
                    MarkLclVarAsDefinitelyStackPointing(lclNum);
                    MarkLclVarAsPossiblyStackPointing(lclNum);
                    stmt->GetRootNode()->gtBashToNOP();
                    comp->optMethodFlags |= OMF_HAS_OBJSTACKALLOC;
                    didStackAllocate = true;
                }

                if (canStack)
                {
                    if (isValueClass)
                    {
                        comp->Metrics.StackAllocatedBoxedValueClasses++;
                    }
                    else
                    {
                        comp->Metrics.StackAllocatedRefClasses++;
                    }
                }
                else
                {
                    assert(onHeapReason != nullptr);
                    JITDUMP("Allocating V%02u on the heap: %s\n", lclNum, onHeapReason);
                    data                         = MorphAllocObjNodeIntoHelperCall(asAllocObj);
                    stmtExpr->AsLclVar()->Data() = data;
                    stmtExpr->AddAllEffectsFlags(data);
                }
            }
#ifdef DEBUG
            else
            {
                // We assume that GT_ALLOCOBJ nodes are always present in the canonical form.
                assert(!comp->gtTreeContainsOper(stmt->GetRootNode(), GT_ALLOCOBJ));
            }
#endif // DEBUG
        }
    }

    return didStackAllocate;
}

//------------------------------------------------------------------------
// MorphAllocObjNodeIntoHelperCall: Morph a GT_ALLOCOBJ node into an
//                                  allocation helper call.
//
// Arguments:
//    allocObj - GT_ALLOCOBJ that will be replaced by helper call.
//
// Return Value:
//    Address of helper call node (can be the same as allocObj).
//
// Notes:
//    Must update parents flags after this.

GenTree* ObjectAllocator::MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj)
{
    assert(allocObj != nullptr);

    GenTree*     arg                  = allocObj->gtGetOp1();
    unsigned int helper               = allocObj->gtNewHelper;
    bool         helperHasSideEffects = allocObj->gtHelperHasSideEffects;

#ifdef FEATURE_READYTORUN
    CORINFO_CONST_LOOKUP entryPoint = allocObj->gtEntryPoint;
    if (helper == CORINFO_HELP_READYTORUN_NEW)
    {
        arg = nullptr;
    }
#endif

    const bool morphArgs  = false;
    GenTree*   helperCall = comp->fgMorphIntoHelperCall(allocObj, allocObj->gtNewHelper, morphArgs, arg);
    if (helperHasSideEffects)
    {
        helperCall->AsCall()->gtCallMoreFlags |= GTF_CALL_M_ALLOC_SIDE_EFFECTS;
    }

#ifdef FEATURE_READYTORUN
    if (entryPoint.addr != nullptr)
    {
        assert(comp->opts.IsReadyToRun());
        helperCall->AsCall()->setEntryPoint(entryPoint);
    }
    else
    {
        assert(helper != CORINFO_HELP_READYTORUN_NEW); // If this is true, then we should have collected a non-null
                                                       // entrypoint above
    }
#endif

    return helperCall;
}

//------------------------------------------------------------------------
// MorphAllocObjNodeIntoStackAlloc: Morph a GT_ALLOCOBJ node into stack
//                                  allocation.
// Arguments:
//    allocObj     - GT_ALLOCOBJ that will be replaced by a stack allocation
//    clsHnd       - class representing the stack allocated object
//    isValueClass - we are stack allocating a boxed value class
//    block        - a basic block where allocObj is
//    stmt         - a statement where allocObj is
//
// Return Value:
//    local num for the new stack allocated local
//
// Notes:
//    This function can insert additional statements before stmt.
//
unsigned int ObjectAllocator::MorphAllocObjNodeIntoStackAlloc(
    GenTreeAllocObj* allocObj, CORINFO_CLASS_HANDLE clsHnd, bool isValueClass, BasicBlock* block, Statement* stmt)
{
    assert(allocObj != nullptr);
    assert(m_AnalysisDone);
    assert(clsHnd != NO_CLASS_HANDLE);

    const bool         shortLifetime = false;
    const unsigned int lclNum        = comp->lvaGrabTemp(shortLifetime DEBUGARG(
        isValueClass ? "stack allocated boxed value class temp" : "stack allocated ref class temp"));

    comp->lvaSetStruct(lclNum, clsHnd, /* unsafeValueClsCheck */ false);

    // Initialize the object memory if necessary.
    bool             bbInALoop  = block->HasFlag(BBF_BACKWARD_JUMP);
    bool             bbIsReturn = block->KindIs(BBJ_RETURN);
    LclVarDsc* const lclDsc     = comp->lvaGetDesc(lclNum);
    lclDsc->lvStackAllocatedBox = isValueClass;
    if (comp->fgVarNeedsExplicitZeroInit(lclNum, bbInALoop, bbIsReturn))
    {
        //------------------------------------------------------------------------
        // STMTx (IL 0x... ???)
        //   *  STORE_LCL_VAR   struct
        //   \--*  CNS_INT   int    0
        //------------------------------------------------------------------------

        GenTree*   init     = comp->gtNewStoreLclVarNode(lclNum, comp->gtNewIconNode(0));
        Statement* initStmt = comp->gtNewStmt(init);

        comp->fgInsertStmtBefore(block, stmt, initStmt);
    }
    else
    {
        JITDUMP("\nSuppressing zero-init for V%02u -- expect to zero in prolog\n", lclNum);
        lclDsc->lvSuppressedZeroInit = 1;
        comp->compSuppressedZeroInit = true;
    }

    // Initialize the vtable slot.
    //
    //------------------------------------------------------------------------
    // STMTx (IL 0x... ???)
    //   * STORE_LCL_FLD    long
    //   \--*  CNS_INT(h) long
    //------------------------------------------------------------------------

    // Initialize the method table pointer.
    GenTree*   init     = comp->gtNewStoreLclFldNode(lclNum, TYP_I_IMPL, 0, allocObj->gtGetOp1());
    Statement* initStmt = comp->gtNewStmt(init);

    comp->fgInsertStmtBefore(block, stmt, initStmt);

    // If this allocation is part the special empty static pattern, find the controlling
    // branch and force control to always flow to the new instance side.
    //
    if ((allocObj->gtFlags & GTF_ALLOCOBJ_EMPTY_STATIC) != 0)
    {
        BasicBlock* const predBlock = block->GetUniquePred(comp);
        assert(predBlock != nullptr);
        assert(predBlock->KindIs(BBJ_COND));

        JITDUMP("Empty static pattern controlled by " FMT_BB ", optimizing to always use stack allocated instance\n",
                predBlock->bbNum);
        Statement* const controllingStmt = predBlock->lastStmt();
        GenTree* const   controllingNode = controllingStmt->GetRootNode();
        assert(controllingNode->OperIs(GT_JTRUE));

        FlowEdge* const trueEdge    = predBlock->GetTrueEdge();
        FlowEdge* const falseEdge   = predBlock->GetFalseEdge();
        FlowEdge*       keptEdge    = nullptr;
        FlowEdge*       removedEdge = nullptr;

        if (trueEdge->getDestinationBlock() == block)
        {
            keptEdge    = trueEdge;
            removedEdge = falseEdge;
        }
        else
        {
            assert(falseEdge->getDestinationBlock() == block);
            keptEdge    = falseEdge;
            removedEdge = trueEdge;
        }

        BasicBlock* removedBlock = removedEdge->getDestinationBlock();
        comp->fgRemoveRefPred(removedEdge);
        predBlock->SetKindAndTargetEdge(BBJ_ALWAYS, keptEdge);

        if (predBlock->hasProfileWeight())
        {
            block->setBBProfileWeight(predBlock->bbWeight);
        }

        // Just lop off the JTRUE, the rest can clean up later
        // (eg may have side effects)
        //
        controllingStmt->SetRootNode(controllingNode->AsOp()->gtOp1);

        // We must remove the empty static block now too.
        assert(removedBlock->bbRefs == 0);
        assert(removedBlock->KindIs(BBJ_ALWAYS));
        comp->fgRemoveBlock(removedBlock, /* unreachable */ true);
    }

    return lclNum;
}

//------------------------------------------------------------------------
// CanLclVarEscapeViaParentStack: Check if the local variable escapes via the given parent stack.
//                                Update the connection graph as necessary.
//
// Arguments:
//    parentStack     - Parent stack of the current visit
//    lclNum          - Local variable number
//    block           - basic block holding the trees
//
// Return Value:
//    true if the local can escape via the parent stack; false otherwise
//
// Notes:
//    The method currently treats all locals assigned to a field as escaping.
//    The can potentially be tracked by special field edges in the connection graph.
//
bool ObjectAllocator::CanLclVarEscapeViaParentStack(ArrayStack<GenTree*>* parentStack,
                                                    unsigned int          lclNum,
                                                    BasicBlock*           block)
{
    assert(parentStack != nullptr);
    int parentIndex = 1;

    bool keepChecking                  = true;
    bool canLclVarEscapeViaParentStack = true;
    bool isEnumeratorLocal             = comp->lvaGetDesc(lclNum)->lvIsEnumerator;

    while (keepChecking)
    {
        if (parentStack->Height() <= parentIndex)
        {
            canLclVarEscapeViaParentStack = false;
            break;
        }

        canLclVarEscapeViaParentStack = true;
        GenTree* tree                 = parentStack->Top(parentIndex - 1);
        GenTree* parent               = parentStack->Top(parentIndex);
        keepChecking                  = false;

        JITDUMP("... V%02u ... checking [%06u]\n", lclNum, comp->dspTreeID(parent));

        switch (parent->OperGet())
        {
            // Update the connection graph if we are storing to a local.
            // For all other stores we mark the local as escaping.
            case GT_STORE_LCL_VAR:
            {
                // Add an edge to the connection graph.
                const unsigned int dstLclNum = parent->AsLclVar()->GetLclNum();
                const unsigned int srcLclNum = lclNum;

                AddConnGraphEdge(dstLclNum, srcLclNum);
                canLclVarEscapeViaParentStack = false;
            }
            break;

            case GT_EQ:
            case GT_NE:
            case GT_NULLCHECK:
                canLclVarEscapeViaParentStack = false;
                break;

            case GT_COMMA:
                if (parent->AsOp()->gtGetOp1() == parentStack->Top(parentIndex - 1))
                {
                    // Left child of GT_COMMA, it will be discarded
                    canLclVarEscapeViaParentStack = false;
                    break;
                }
                FALLTHROUGH;
            case GT_COLON:
            case GT_QMARK:
            case GT_ADD:
            case GT_BOX:
            case GT_FIELD_ADDR:
                // Check whether the local escapes via its grandparent.
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_STOREIND:
            case GT_STORE_BLK:
            case GT_BLK:
                if (tree != parent->AsIndir()->Addr())
                {
                    // TODO-ObjectStackAllocation: track stores to fields.
                    break;
                }
                FALLTHROUGH;
            case GT_IND:
                // Address of the field/ind is not taken so the local doesn't escape.
                canLclVarEscapeViaParentStack = false;
                break;

            case GT_CALL:
            {
                GenTreeCall* const asCall = parent->AsCall();

                if (asCall->IsHelperCall())
                {
                    canLclVarEscapeViaParentStack =
                        !Compiler::s_helperCallProperties.IsNoEscape(comp->eeGetHelperNum(asCall->gtCallMethHnd));
                }

                // Note there is nothing special here about this user being a call. We could move all this processing up
                // to the caller and handle any sort of tree that could lead to escapes this way.
                //
                // We have it this way because we currently don't expect to see other escaping references on failed
                // GDV paths, though perhaps with multi-guess GDV that might change?
                //
                // In particular it might be tempting to look for references in uncatchable BBJ_THROWs or similar
                // and enable a kind of "partial escape analysis" where we copy from stack to heap just before the
                // point of escape. We would have to add pseudo-locals for this like we do for GDV, but we wouldn't
                // necessarily need to do the predicate analysis or cloning.
                //
                if (isEnumeratorLocal)
                {
                    JITDUMP("Enumerator V%02u passed to call...\n", lclNum);

                    // Find pseudo local...
                    //
                    unsigned pseudoLocal = BAD_VAR_NUM;
                    if (m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
                    {
                        // Verify that this call is made under the set of conditions tracked by the
                        // pseudo local...
                        //
                        GuardInfo info;
                        if (IsGuarded(block, asCall, &info))
                        {
                            GuardInfo pseudoInfo;
                            if (m_GuardMap.Lookup(pseudoLocal, &pseudoInfo))
                            {
                                if ((info.m_local == lclNum && pseudoInfo.m_local == lclNum) &&
                                    (info.m_type == pseudoInfo.m_type))
                                {
                                    // If so, track this as an assignment PseudoLocal = ...
                                    // Later if we don't clone and split off the failing GDV paths,
                                    // we will mark PseudoLocal as escaped.
                                    //
                                    JITDUMP("... under GDV; tracking via pseudo-local P%02u\n", pseudoLocal);
                                    AddConnGraphEdge(pseudoLocal, lclNum);
                                    canLclVarEscapeViaParentStack = false;
                                }
                                else
                                {
                                    JITDUMP("... under different guard?\n");
                                }
                            }
                            else
                            {
                                JITDUMP("... under non-gdv guard?\n");
                            }
                        }
                        else
                        {
                            JITDUMP("... not guarded?\n");
                        }
                    }
                    else
                    {
                        JITDUMP("... no pseudo local?\n");
                    }
                }
                break;
            }

            default:
                break;
        }
    }

    return canLclVarEscapeViaParentStack;
}

//------------------------------------------------------------------------
// UpdateAncestorTypes: Update types of some ancestor nodes of a possibly-stack-pointing
//                      tree from TYP_REF to TYP_BYREF or TYP_I_IMPL.
//
// Arguments:
//    tree            - Possibly-stack-pointing tree
//    parentStack     - Parent stack of the possibly-stack-pointing tree
//    newType         - New type of the possibly-stack-pointing tree
//
// Notes:
//                      If newType is TYP_I_IMPL, the tree is definitely pointing to the stack (or is null);
//                      if newType is TYP_BYREF, the tree may point to the stack.
//                      In addition to updating types this method may set GTF_IND_TGT_NOT_HEAP on ancestor
//                      indirections to help codegen with write barrier selection.
//
void ObjectAllocator::UpdateAncestorTypes(GenTree* tree, ArrayStack<GenTree*>* parentStack, var_types newType)
{
    assert(newType == TYP_BYREF || newType == TYP_I_IMPL);
    assert(parentStack != nullptr);
    int parentIndex = 1;

    bool keepChecking = true;

    while (keepChecking && (parentStack->Height() > parentIndex))
    {
        GenTree* parent = parentStack->Top(parentIndex);
        keepChecking    = false;

        switch (parent->OperGet())
        {
            case GT_STORE_LCL_VAR:
            case GT_BOX:
                if (parent->TypeGet() == TYP_REF)
                {
                    parent->ChangeType(newType);
                }
                break;

            case GT_EQ:
            case GT_NE:
            case GT_NULLCHECK:
                break;

            case GT_COMMA:
                if (parent->AsOp()->gtGetOp1() == parentStack->Top(parentIndex - 1))
                {
                    // Left child of GT_COMMA, it will be discarded
                    break;
                }
                FALLTHROUGH;
            case GT_QMARK:
            case GT_ADD:
            case GT_FIELD_ADDR:
                if (parent->TypeGet() == TYP_REF)
                {
                    parent->ChangeType(newType);
                }
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_COLON:
            {
                GenTree* const lhs = parent->AsOp()->gtGetOp1();
                GenTree* const rhs = parent->AsOp()->gtGetOp2();

                // We may see sibling null refs. Retype them as appropriate.
                //
                if (lhs == tree)
                {
                    assert(rhs->IsIntegralConst(0));
                    rhs->ChangeType(newType);
                }
                else
                {
                    assert(rhs == tree);
                    assert(lhs->IsIntegralConst(0));
                    lhs->ChangeType(newType);
                }

                parent->ChangeType(newType);

                ++parentIndex;
                keepChecking = true;
            }
            break;

            case GT_STOREIND:
            case GT_STORE_BLK:
            case GT_BLK:
                assert(tree == parent->AsIndir()->Addr());

                // The new target could be *not* on the heap.
                parent->gtFlags &= ~GTF_IND_TGT_HEAP;

                if (newType != TYP_BYREF)
                {
                    // This indicates that a write barrier is not needed when writing
                    // to this field/indirection since the address is not pointing to the heap.
                    // It's either null or points to inside a stack-allocated object.
                    parent->gtFlags |= GTF_IND_TGT_NOT_HEAP;
                }
                break;

            case GT_IND:
            case GT_CALL:
                break;

            default:
                unreached();
        }

        if (keepChecking)
        {
            tree = parentStack->Top(parentIndex - 1);
        }
    }

    return;
}

//------------------------------------------------------------------------
// RewriteUses: Find uses of the newobj temp for stack-allocated
//              objects and replace with address of the stack local.

void ObjectAllocator::RewriteUses()
{
    class RewriteUsesVisitor final : public GenTreeVisitor<RewriteUsesVisitor>
    {
        ObjectAllocator* m_allocator;

    public:
        enum
        {
            DoPreOrder   = true,
            DoPostOrder  = true,
            ComputeStack = true,
        };

        RewriteUsesVisitor(ObjectAllocator* allocator)
            : GenTreeVisitor<RewriteUsesVisitor>(allocator->comp)
            , m_allocator(allocator)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;

            if (!tree->OperIsAnyLocal())
            {
                return Compiler::fgWalkResult::WALK_CONTINUE;
            }

            const unsigned int lclNum    = tree->AsLclVarCommon()->GetLclNum();
            unsigned int       newLclNum = BAD_VAR_NUM;
            LclVarDsc*         lclVarDsc = m_compiler->lvaGetDesc(lclNum);

            if ((lclNum < BitVecTraits::GetSize(&m_allocator->m_bitVecTraits)) &&
                m_allocator->MayLclVarPointToStack(lclNum))
            {
                // Analysis does not handle indirect access to pointer locals.
                assert(tree->OperIsScalarLocal());

                var_types newType;
                if (m_allocator->m_HeapLocalToStackLocalMap.TryGetValue(lclNum, &newLclNum))
                {
                    assert(tree->OperIs(GT_LCL_VAR)); // Must be a use.
                    newType = TYP_I_IMPL;
                    tree    = m_compiler->gtNewLclVarAddrNode(newLclNum);
                    *use    = tree;
                }
                else
                {
                    newType = m_allocator->DoesLclVarPointToStack(lclNum) ? TYP_I_IMPL : TYP_BYREF;
                    if (tree->TypeGet() == TYP_REF)
                    {
                        tree->ChangeType(newType);
                    }
                }

                if (lclVarDsc->lvType != newType)
                {
                    JITDUMP("changing the type of V%02u from %s to %s\n", lclNum, varTypeName(lclVarDsc->lvType),
                            varTypeName(newType));
                    lclVarDsc->lvType = newType;
                }
                m_allocator->UpdateAncestorTypes(tree, &m_ancestors, newType);
            }

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }

        Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;

            // Remove GT_BOX, if stack allocated
            //
            if (tree->OperIs(GT_BOX))
            {
                GenTree* const boxLcl = tree->AsOp()->gtGetOp1();
                assert(boxLcl->OperIs(GT_LCL_VAR, GT_LCL_ADDR));
                if (boxLcl->OperIs(GT_LCL_ADDR))
                {
                    JITDUMP("Removing BOX wrapper [%06u]\n", m_compiler->dspTreeID(tree));
                    *use = boxLcl;
                }
            }
            // Make box accesses explicit for UNBOX_HELPER
            //
            else if (tree->IsCall())
            {
                GenTreeCall* const call = tree->AsCall();

                if (call->IsHelperCall(m_compiler, CORINFO_HELP_UNBOX))
                {
                    JITDUMP("Found unbox helper call [%06u]\n", m_compiler->dspTreeID(call));

                    // See if second arg is possibly a stack allocated box or ref class
                    // (arg will have been retyped local or local address)
                    //
                    CallArg*       secondArg     = call->gtArgs.GetArgByIndex(1);
                    GenTree* const secondArgNode = secondArg->GetNode();

                    if ((secondArgNode->OperIsLocal() || secondArgNode->OperIs(GT_LCL_ADDR)) &&
                        !secondArgNode->TypeIs(TYP_REF))
                    {
                        const bool                 isForEffect = (user == nullptr) || call->TypeIs(TYP_VOID);
                        GenTreeLclVarCommon* const lcl         = secondArgNode->AsLclVarCommon();

                        // Rewrite the call to make the box accesses explicit in jitted code.
                        // user = COMMA(
                        //           CALL(UNBOX_HELPER_TYPETEST, obj->MethodTable, type),
                        //           ADD(obj, TARGET_POINTER_SIZE))
                        //
                        JITDUMP("Rewriting to invoke box type test helper%s\n", isForEffect ? " for side effect" : "");

                        call->gtCallMethHnd = m_compiler->eeFindHelper(CORINFO_HELP_UNBOX_TYPETEST);
                        GenTree* const mt   = m_compiler->gtNewMethodTableLookup(lcl, /* onStack */ true);
                        call->gtArgs.Remove(secondArg);
                        call->gtArgs.PushBack(m_compiler, NewCallArg::Primitive(mt));

                        if (isForEffect)
                        {
                            // call was just for effect, we're done.
                        }
                        else
                        {
                            GenTree* const lclCopy = m_compiler->gtCloneExpr(lcl);
                            GenTree* const payloadAddr =
                                m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, lclCopy,
                                                          m_compiler->gtNewIconNode(TARGET_POINTER_SIZE, TYP_I_IMPL));
                            GenTree* const comma = m_compiler->gtNewOperNode(GT_COMMA, TYP_BYREF, call, payloadAddr);
                            *use                 = comma;
                        }
                    }
                }
            }
            else if (tree->OperIsIndir())
            {
                // Look for cases where the addr is a comma created above, and
                // sink the indir into the comma so later phases can see the access more cleanly.
                //
                GenTreeIndir* const indir = tree->AsIndir();
                GenTree* const      addr  = indir->Addr();

                if (addr->OperIs(GT_COMMA))
                {
                    GenTree* const lastEffect = addr->AsOp()->gtGetOp1();

                    if (lastEffect->IsCall() &&
                        lastEffect->AsCall()->IsHelperCall(m_compiler, CORINFO_HELP_UNBOX_TYPETEST))
                    {
                        GenTree* const actualAddr  = addr->gtEffectiveVal();
                        GenTree*       sideEffects = nullptr;
                        m_compiler->gtExtractSideEffList(indir, &sideEffects, GTF_SIDE_EFFECT, /* ignore root */ true);

                        // indir is based on a local address, no side effect possible.
                        //
                        indir->Addr() = actualAddr;
                        indir->gtFlags &= ~GTF_SIDE_EFFECT;
                        GenTree* const newComma =
                            m_compiler->gtNewOperNode(GT_COMMA, indir->TypeGet(), sideEffects, indir);
                        *use = newComma;
                    }
                }
            }

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    for (BasicBlock* const block : comp->Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            RewriteUsesVisitor rewriteUsesVisitor(this);
            rewriteUsesVisitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }
}
