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
#include "jitstd/algorithm.h"

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
    if ((comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0 && (comp->optMethodFlags & OMF_HAS_NEWARRAY) == 0)
    {
        JITDUMP("no newobjs or newarr in this method; punting\n");
        comp->fgInvalidateDfsTree();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // If optimizations are disabled and there are no newobjs, we don't need to morph anything.
    if (comp->opts.OptimizationDisabled() && (comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0)
    {
        JITDUMP("optimizations are disabled and there are no newobjs; punting\n");
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

        // If we have to clone some code to guarantee non-escape, do it now.
        //
        CloneAndSpecialize();
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
            GenTree* const tree   = *use;
            unsigned const lclNum = tree->AsLclVarCommon()->GetLclNum();

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
                m_allocator->CheckForGuardedAllocationOrCopy(m_block, m_stmt, use, lclNum);
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
            else if (!tree->OperIsLocalStore())
            {
                // Note uses of variables of interest to conditional escape analysis.
                //
                m_allocator->RecordAppearance(lclNum, m_block, m_stmt, use);
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
    BitVec escapingNodesToProcess = BitVecOps::MakeCopy(bitVecTraits, escapingNodes);

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

    if (m_numPseudoLocals > 0)
    {
        bool newEscapes = AnalyzeIfCloningCanPreventEscape(bitVecTraits, escapingNodes, escapingNodesToProcess);
        if (newEscapes)
        {
            computeClosure();
        }
    }
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

    JITDUMP("Definitely stack-pointing locals:");
    {
        BitVecOps::Iter iter(bitVecTraits, m_DefinitelyStackPointingPointers);
        unsigned        lclNum = 0;
        while (iter.NextElem(&lclNum))
        {
            JITDUMP(" V%02u", lclNum);
        }
        JITDUMP("\n");
    }

    JITDUMP("Possibly stack-pointing locals:");
    {
        BitVecOps::Iter iter(bitVecTraits, m_PossiblyStackPointingPointers);
        unsigned        lclNum = 0;
        while (iter.NextElem(&lclNum))
        {
            if (!BitVecOps::IsMember(bitVecTraits, m_DefinitelyStackPointingPointers, lclNum))
            {
                JITDUMP(" V%02u", lclNum);
            }
        }
        JITDUMP("\n");
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
    const bool isReadyToRun           = comp->opts.IsReadyToRun() && !comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI);

    for (BasicBlock* const block : comp->Blocks())
    {
        const bool basicBlockHasNewObj       = block->HasFlag(BBF_HAS_NEWOBJ);
        const bool basicBlockHasNewArr       = block->HasFlag(BBF_HAS_NEWARR);
        const bool basicBlockHasBackwardJump = block->HasFlag(BBF_BACKWARD_JUMP);

        if (!basicBlockHasNewObj && !basicBlockHasNewArr)
        {
            continue;
        }

        for (Statement* const stmt : block->Statements())
        {
            GenTree* stmtExpr = stmt->GetRootNode();
            GenTree* data     = nullptr;

            ObjectAllocationType allocType = OAT_NONE;

            if (stmtExpr->OperIs(GT_STORE_LCL_VAR) && stmtExpr->TypeIs(TYP_REF))
            {
                data = stmtExpr->AsLclVar()->Data();

                if (data->OperGet() == GT_ALLOCOBJ)
                {
                    allocType = OAT_NEWOBJ;
                }
                else if (!isReadyToRun && data->IsHelperCall())
                {
                    switch (data->AsCall()->GetHelperNum())
                    {
                        case CORINFO_HELP_NEWARR_1_VC:
                        case CORINFO_HELP_NEWARR_1_OBJ:
                        case CORINFO_HELP_NEWARR_1_DIRECT:
                        case CORINFO_HELP_NEWARR_1_ALIGN8:
                        {
                            if ((data->AsCall()->gtArgs.CountUserArgs() == 2) &&
                                data->AsCall()->gtArgs.GetUserArgByIndex(1)->GetNode()->IsCnsIntOrI())
                            {
                                allocType = OAT_NEWARR;
                            }
                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }
                }
            }

            if (allocType != OAT_NONE)
            {
                bool         canStack     = false;
                bool         bashCall     = false;
                const char*  onHeapReason = nullptr;
                unsigned int lclNum       = stmtExpr->AsLclVar()->GetLclNum();

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
                else
                {
                    if (allocType == OAT_NEWARR)
                    {
                        assert(basicBlockHasNewArr);

                        // R2R not yet supported
                        //
                        assert(!isReadyToRun);

                        //------------------------------------------------------------------------
                        // We expect the following expression tree at this point
                        // For non-ReadyToRun:
                        //  STMTx (IL 0x... ???)
                        //    * STORE_LCL_VAR   ref
                        //    \--*  CALL help  ref
                        //       +--*  CNS_INT(h) long
                        //       \--*  CNS_INT long
                        // For ReadyToRun:
                        //  STMTx (IL 0x... ???)
                        //    * STORE_LCL_VAR   ref
                        //    \--*  CALL help  ref
                        //       \--*  CNS_INT long
                        //------------------------------------------------------------------------

                        bool                 isExact   = false;
                        bool                 isNonNull = false;
                        CORINFO_CLASS_HANDLE clsHnd =
                            comp->gtGetHelperCallClassHandle(data->AsCall(), &isExact, &isNonNull);
                        GenTree* const len = data->AsCall()->gtArgs.GetUserArgByIndex(1)->GetNode();

                        assert(len != nullptr);

                        unsigned int blockSize = 0;
                        comp->Metrics.NewArrayHelperCalls++;

                        if (!isExact || !isNonNull)
                        {
                            onHeapReason = "[array type is either non-exact or null]";
                            canStack     = false;
                        }
                        else if (!len->IsCnsIntOrI())
                        {
                            onHeapReason = "[non-constant size]";
                            canStack     = false;
                        }
                        else if (!CanAllocateLclVarOnStack(lclNum, clsHnd, allocType, len->AsIntCon()->IconValue(),
                                                           &blockSize, &onHeapReason))
                        {
                            // reason set by the call
                            canStack = false;
                        }
                        else
                        {
                            JITDUMP("Allocating V%02u on the stack\n", lclNum);
                            canStack = true;
                            const unsigned int stackLclNum =
                                MorphNewArrNodeIntoStackAlloc(data->AsCall(), clsHnd,
                                                              (unsigned int)len->AsIntCon()->IconValue(), blockSize,
                                                              block, stmt);

                            // Note we do not want to rewrite uses of the array temp, so we
                            // do not update m_HeapLocalToStackLocalMap.
                            //
                            comp->Metrics.StackAllocatedArrays++;
                        }
                    }
                    else if (allocType == OAT_NEWOBJ)
                    {
                        assert(basicBlockHasNewObj);
                        //------------------------------------------------------------------------
                        // We expect the following expression tree at this point
                        //  STMTx (IL 0x... ???)
                        //    * STORE_LCL_VAR   ref
                        //    \--*  ALLOCOBJ  ref
                        //       \--*  CNS_INT(h) long
                        //------------------------------------------------------------------------

                        CORINFO_CLASS_HANDLE clsHnd       = data->AsAllocObj()->gtAllocObjClsHnd;
                        CORINFO_CLASS_HANDLE stackClsHnd  = clsHnd;
                        const bool           isValueClass = comp->info.compCompHnd->isValueClass(clsHnd);

                        if (isValueClass)
                        {
                            comp->Metrics.NewBoxedValueClassHelperCalls++;
                            stackClsHnd = comp->info.compCompHnd->getTypeForBoxOnStack(clsHnd);
                        }
                        else
                        {
                            comp->Metrics.NewRefClassHelperCalls++;
                        }

                        if (!CanAllocateLclVarOnStack(lclNum, clsHnd, allocType, 0, nullptr, &onHeapReason))
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
                                MorphAllocObjNodeIntoStackAlloc(data->AsAllocObj(), stackClsHnd, isValueClass, block,
                                                                stmt);
                            m_HeapLocalToStackLocalMap.AddOrUpdate(lclNum, stackLclNum);

                            if (isValueClass)
                            {
                                comp->Metrics.StackAllocatedBoxedValueClasses++;
                            }
                            else
                            {
                                comp->Metrics.StackAllocatedRefClasses++;
                            }

                            bashCall = true;
                        }
                    }
                }

                if (canStack)
                {
                    // We keep the set of possibly-stack-pointing pointers as a superset of the set of
                    // definitely-stack-pointing pointers. All definitely-stack-pointing pointers are in both
                    // sets.
                    MarkLclVarAsDefinitelyStackPointing(lclNum);
                    MarkLclVarAsPossiblyStackPointing(lclNum);

                    // If this was conditionally escaping enumerator, establish a connection between this local
                    // and the enumeratorLocal we already allocated. This is needed because we do early rewriting
                    // in the conditional clone.
                    //
                    unsigned pseudoLocal = BAD_VAR_NUM;
                    if (m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
                    {
                        CloneInfo* info = nullptr;
                        if (m_CloneMap.Lookup(pseudoLocal, &info))
                        {
                            if (info->m_willClone)
                            {
                                JITDUMP("Connecting stack allocated enumerator V%02u to its address var V%02u\n",
                                        lclNum, info->m_enumeratorLocal);
                                AddConnGraphEdge(lclNum, info->m_enumeratorLocal);
                                MarkLclVarAsPossiblyStackPointing(info->m_enumeratorLocal);
                                MarkLclVarAsDefinitelyStackPointing(info->m_enumeratorLocal);
                            }
                        }
                    }

                    if (bashCall)
                    {
                        stmt->GetRootNode()->gtBashToNOP();
                    }

                    comp->optMethodFlags |= OMF_HAS_OBJSTACKALLOC;
                    didStackAllocate = true;
                }
                else
                {
                    assert(onHeapReason != nullptr);
                    JITDUMP("Allocating V%02u on the heap: %s\n", lclNum, onHeapReason);
                    if (allocType == OAT_NEWOBJ)
                    {
                        data                         = MorphAllocObjNodeIntoHelperCall(data->AsAllocObj());
                        stmtExpr->AsLclVar()->Data() = data;
                        stmtExpr->AddAllEffectsFlags(data);
                    }
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
// MorphNewArrNodeIntoStackAlloc: Morph a newarray helper call node into stack allocation.
//
// Arguments:
//    newArr       - GT_CALL that will be replaced by helper call.
//    clsHnd       - class representing the type of the array
//    length       - length of the array
//    blockSize    - size of the layout
//    block        - a basic block where newArr is
//    stmt         - a statement where newArr is
//
// Return Value:
//    local num for the new stack allocated local
//
// Notes:
//    This function can insert additional statements before stmt.
//
unsigned int ObjectAllocator::MorphNewArrNodeIntoStackAlloc(GenTreeCall*         newArr,
                                                            CORINFO_CLASS_HANDLE clsHnd,
                                                            unsigned int         length,
                                                            unsigned int         blockSize,
                                                            BasicBlock*          block,
                                                            Statement*           stmt)
{
    assert(newArr != nullptr);
    assert(m_AnalysisDone);
    assert(clsHnd != NO_CLASS_HANDLE);
    assert(newArr->IsHelperCall());
    assert(newArr->GetHelperNum() != CORINFO_HELP_NEWARR_1_MAYBEFROZEN);

    const bool         shortLifetime = false;
    const bool         alignTo8      = newArr->GetHelperNum() == CORINFO_HELP_NEWARR_1_ALIGN8;
    const unsigned int lclNum        = comp->lvaGrabTemp(shortLifetime DEBUGARG("stack allocated array temp"));
    LclVarDsc* const   lclDsc        = comp->lvaGetDesc(lclNum);

    if (alignTo8)
    {
        blockSize = AlignUp(blockSize, 8);
    }

    comp->lvaSetStruct(lclNum, comp->typGetArrayLayout(clsHnd, length), /* unsafe */ false);
    lclDsc->lvStackAllocatedObject = true;

    // Initialize the object memory if necessary.
    bool bbInALoop  = block->HasFlag(BBF_BACKWARD_JUMP);
    bool bbIsReturn = block->KindIs(BBJ_RETURN);
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

#ifndef TARGET_64BIT
    lclDsc->lvStructDoubleAlign = alignTo8;
#endif

    // Mark the newarr call as being "on stack", and add the address
    // of the stack local as an argument
    //
    GenTree* const stackLocalAddr = comp->gtNewLclAddrNode(lclNum, 0);
    newArr->gtArgs.PushBack(comp, NewCallArg::Primitive(stackLocalAddr).WellKnown(WellKnownArg::StackArrayLocal));
    newArr->gtCallMoreFlags |= GTF_CALL_M_STACK_ARRAY;

    // Retype the call result as an unmanaged pointer
    //
    newArr->ChangeType(TYP_I_IMPL);
    newArr->gtReturnType = TYP_I_IMPL;

    // Note that we have stack allocated arrays in this method
    //
    comp->setMethodHasStackAllocatedArray();

    return lclNum;
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
    bool             bbInALoop     = block->HasFlag(BBF_BACKWARD_JUMP);
    bool             bbIsReturn    = block->KindIs(BBJ_RETURN);
    LclVarDsc* const lclDsc        = comp->lvaGetDesc(lclNum);
    lclDsc->lvStackAllocatedObject = true;
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
        comp->fgRepairProfileCondToUncond(predBlock, keptEdge, removedEdge);

        // Just lop off the JTRUE, the rest can clean up later
        // (eg may have side effects)
        //
        controllingStmt->SetRootNode(controllingNode->gtGetOp1());

        // We must remove the empty static block now too.
        assert(removedBlock->bbRefs == 0);
        assert(removedBlock->KindIs(BBJ_ALWAYS));
        comp->fgRemoveBlock(removedBlock, /* unreachable */ true);
    }
    else
    {
        JITDUMP("ALLOCOBJ [%06u] is not part of an empty static\n", comp->dspTreeID(allocObj));
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
    bool isCopy                        = true;
    bool isEnumeratorLocal             = comp->lvaGetDesc(lclNum)->lvIsEnumerator;

    while (keepChecking)
    {
        if (parentStack->Height() <= parentIndex)
        {
            canLclVarEscapeViaParentStack = false;
            break;
        }

        GenTree* tree    = parentStack->Top(parentIndex - 1);
        GenTree* parent  = parentStack->Top(parentIndex);
        bool     wasCopy = isCopy;

        isCopy                        = false;
        canLclVarEscapeViaParentStack = true;
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

                // If the source of this store is an enumerator local,
                // then the dest also becomes an enumerator local.
                //
                if (isCopy)
                {
                    CheckForEnumeratorUse(srcLclNum, dstLclNum);
                }
            }
            break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_GT:
            case GT_LE:
            case GT_GE:
            case GT_NULLCHECK:
            case GT_ARR_LENGTH:
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
            case GT_SUB:
            case GT_FIELD_ADDR:
                // Check whether the local escapes via its grandparent.
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_BOX:
                isCopy = wasCopy;
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_INDEX_ADDR:
                if (tree == parent->AsIndexAddr()->Index())
                {
                    // The index is not taken so the local doesn't escape.
                    canLclVarEscapeViaParentStack = false;
                    break;
                }
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
                else if (asCall->IsSpecialIntrinsic())
                {
                    // Some known special intrinsics don't escape. At this moment, only the ones accepting byrefs
                    // are supported. In order to support more intrinsics accepting objects, we need extra work
                    // on the VM side which is not ready for that yet.
                    //
                    switch (comp->lookupNamedIntrinsic(asCall->gtCallMethHnd))
                    {
                        case NI_System_SpanHelpers_ClearWithoutReferences:
                        case NI_System_SpanHelpers_Fill:
                        case NI_System_SpanHelpers_Memmove:
                        case NI_System_SpanHelpers_SequenceEqual:
                            canLclVarEscapeViaParentStack = false;
                            break;

                        default:
                            break;
                    }
                }

                // Note there is nothing special here about the parent being a call. We could move all this processing
                // up to the caller and handle any sort of tree that could lead to escapes this way.
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
                    canLclVarEscapeViaParentStack = !CheckForGuardedUse(block, parent, lclNum);
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
            case GT_LT:
            case GT_GT:
            case GT_LE:
            case GT_GE:
            case GT_NULLCHECK:
            case GT_ARR_LENGTH:
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
            case GT_SUB:
            case GT_FIELD_ADDR:
            case GT_INDEX_ADDR:
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
                    JITDUMP("Changing the type of V%02u from %s to %s\n", lclNum, varTypeName(lclVarDsc->lvType),
                            varTypeName(newType));
                    lclVarDsc->lvType = newType;
                }
                m_allocator->UpdateAncestorTypes(tree, &m_ancestors, newType);

                if (newLclNum != BAD_VAR_NUM)
                {
                    JITDUMP("Update V%02u to V%02u from use [%06u]\n", lclNum, newLclNum, m_compiler->dspTreeID(tree));
                    DISPTREE(tree);
                }
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

//------------------------------------------------------------------------------
// AnalyzeIfCloningCanPreventEscape: see if by cloning we can ensure an object
//    does not escape.
//
// Arguments:
//   bitVecTraits                       - Bit vector traits
//   escapingNodes               [in]   - current set of escaping nodes
//   escapingNodesToProcess  [in/out]   - set of newly escaping nodes
//
// Returns:
//   true, if there are any newly escaping nodes
//
// Notes:
//   During our analysis we have may have noted conditionally escaping objects
//   and var references and connected them to a pseduolocal, along with information
//   about how we could clone blocks to ensure that the object could be stack allocated.
//
//   The current assumption is that these nodes do not escape, but to ensure
//   that we must be able to clone the code and remove the potential for escape
//
//   So, we  verify for each case that we can clone; if not, mark we the pseudolocal
//   as escaping. If any pseudlocal now escapes, we return true so that the main
//   analysis can update its closure.
//
//   We may choose not to clone a candiate for several reasons:
//   * too much EH already in the method, or some other reason cloning is infeasible
//   * two different candidates have overlapping clone regions
//   * the cost/benefit analysis does not look favorable
//
bool ObjectAllocator::AnalyzeIfCloningCanPreventEscape(BitVecTraits* bitVecTraits,
                                                       BitVec&       escapingNodes,
                                                       BitVec&       escapingNodesToProcess)
{
    bool newEscapes = false;

    for (unsigned p = 0; p < m_numPseudoLocals; p++)
    {
        unsigned const pseudoLocal = p + comp->lvaCount;
        bool           canClone    = true;
        CloneInfo*     info        = nullptr;

        const bool hasInfo = m_CloneMap.Lookup(pseudoLocal, &info);
        if (!hasInfo)
        {
            // We never found any conditional allocation attached to this pseudoLocal.
            //
            JITDUMP("   P%02u has no guard info\n", pseudoLocal);
            canClone = false;
            break;
        }

        unsigned lclNum                 = BAD_VAR_NUM;
        BitVec   pseudoLocalAdjacencies = m_ConnGraphAdjacencyMatrix[pseudoLocal];

        // If we found an allocation but didn't find any conditionally escaping uses, then cloning is of no use
        //
        if (BitVecOps::IsEmpty(bitVecTraits, pseudoLocalAdjacencies))
        {
            JITDUMP("   No conditionally escaping uses under P%02u, so no reason to clone\n", pseudoLocal);
            canClone = false;
            break;
        }

        // Check if each conditionally escaping local escapes on its own; if so cloning is of no use
        //
        BitVecOps::Iter iterator(bitVecTraits, pseudoLocalAdjacencies);
        while (canClone && iterator.NextElem(&lclNum))
        {
            if (BitVecOps::IsMember(bitVecTraits, escapingNodes, lclNum))
            {
                // The enumerator var or a related var had escaping uses somewhere in the method,
                // not under a failing GDV or any GDV.
                //
                JITDUMP("   V%02u escapes independently of P%02u\n", lclNum, pseudoLocal);
                canClone = false;
                break;
            }
        }

        // Also check the alloc temps
        //
        if (info->m_allocTemps != nullptr)
        {
            for (unsigned v : *(info->m_allocTemps))
            {
                if (BitVecOps::IsMember(bitVecTraits, escapingNodes, v))
                {
                    JITDUMP("   alloc temp V%02u escapes independently of P%02u\n", v, pseudoLocal)
                    canClone = false;
                    break;
                }
            }
        }

        if (canClone)
        {
            // We may be able to clone and specialize the enumerator uses to ensure
            // that the allocated enumerator does not escape.
            //
            JITDUMP("   P%02u is guarding the escape of V%02u\n", pseudoLocal, lclNum);
            if (info->m_allocTemps != nullptr)
            {
                JITDUMP("   along with ");
                for (unsigned v : *(info->m_allocTemps))
                {
                    JITDUMP("V%02u ", v);
                }
                JITDUMP("\n");
            }
            JITDUMP("   they escape only when V%02u.Type NE %s\n", info->m_local, comp->eeGetClassName(info->m_type));
            JITDUMP("   V%02u + secondary vars have %u appearances\n", info->m_local, info->m_appearanceCount);

            comp->Metrics.EnumeratorGDVProvisionalNoEscape++;
        }

        // See if cloning is actually viable.
        //
        if (canClone)
        {
            canClone = CanClone(info);
        }

        // See if this clone would overlap with othr clones
        //
        if (canClone)
        {
            canClone = !CloneOverlaps(info);
        }

        // See if cloning is a good idea.
        //
        if (canClone)
        {
            canClone = ShouldClone(info);
        }

        // All checks are done
        //
        if (canClone)
        {
            JITDUMP("\n*** Can prevent escape under P%02u via cloning ***\n", pseudoLocal);
            info->m_willClone = true;
            m_regionsToClone++;
        }
        else
        {
            JITDUMP("   not optimizing, so will mark P%02u as escaping\n", pseudoLocal);
            MarkLclVarAsEscaping(pseudoLocal);
            BitVecOps::AddElemD(bitVecTraits, escapingNodesToProcess, pseudoLocal);
            newEscapes = true;
        }
    }

    return newEscapes;
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
// IsGuarded: does evaluation of `tree` depend on a GDV type test?
//
// Arguments:
//   block        -- block containing tree
//   tree         -- tree in question
//   info         -- [out] closest enclosing guard info, if method returns true
//   testOutcome  -- outcome of GDV test (true ==> type matches the specific one in the test)
//
// Returns:
//   true if tree is only evaluated under a GDV check, where the check result is testOutcome.
//   Returns closest such check (in terms of dominators), along with info on the check.
//
// Notes:
//   * There may be other checks higher in the tree, consider returning all
//     checks rather than just the closest.
//   * Possibly try and recognize user-written type checks...?
//   * Consider bailing out at some point, for deep dominator trees.
//   * R2R/NAOT cases where compile time and runtime handles diverge
//
bool ObjectAllocator::IsGuarded(BasicBlock* block, GenTree* tree, GuardInfo* info, bool testOutcome)
{
    JITDUMP("Checking if [%06u] in " FMT_BB " executes under a %s GDV type test\n", comp->dspTreeID(tree), block->bbNum,
            testOutcome ? "successful" : "failing");

    // Walk up the dominator tree....
    //
    for (BasicBlock* idomBlock = block->bbIDom; idomBlock != nullptr; idomBlock = idomBlock->bbIDom)
    {
        JITDUMP("... examining dominator " FMT_BB "\n", idomBlock->bbNum);
        if (!idomBlock->KindIs(BBJ_COND))
        {
            JITDUMP("... not BBJ_COND\n");
            continue;
        }

        // We require that one idomBlock successor *not* dominate.
        // (otherwise idomBlock could be the top of a diamond where both outcomes reach block).
        //
        const bool trueSuccessorDominates  = comp->m_domTree->Dominates(idomBlock->GetTrueTarget(), block);
        const bool falseSuccessorDominates = comp->m_domTree->Dominates(idomBlock->GetFalseTarget(), block);

        if (trueSuccessorDominates && falseSuccessorDominates)
        {
            JITDUMP("... both successors dominate?\n");
            continue;
        }

        if (!(trueSuccessorDominates || falseSuccessorDominates))
        {
            JITDUMP("... neither successor dominates\n");
            continue;
        }

        GenTree* const guardingRelop = IsGuard(idomBlock, info);
        if (guardingRelop == nullptr)
        {
            continue;
        }

        // We found a dominating GDV test, see if the condition is the one we're looking for.
        //
        if (testOutcome)
        {
            bool const isReachableOnGDVSuccess = (trueSuccessorDominates && guardingRelop->OperIs(GT_EQ)) ||
                                                 (falseSuccessorDominates && guardingRelop->OperIs(GT_NE));
            if (isReachableOnGDVSuccess)
            {
                info->m_block = idomBlock;
                return true;
            }
            JITDUMP("... guarded by failing GDV\n");
            continue;
        }
        else
        {
            bool const isReachableOnGDVFailure = (trueSuccessorDominates && guardingRelop->OperIs(GT_NE)) ||
                                                 (falseSuccessorDominates && guardingRelop->OperIs(GT_EQ));
            if (isReachableOnGDVFailure)
            {
                info->m_block = idomBlock;
                return true;
            }
            JITDUMP("... guarded by successful GDV\n");
            continue;
        }
    }

    JITDUMP("... no more doms\n");
    return false;
}

//------------------------------------------------------------------------------
// IsGuard: does block look like a GDV guard
//
// Arguments:
//   block -- block in question
//   info -- [out] guard info
//
// Returns:
//   Comparison tree if this is a guard, or nullptr
//
GenTree* ObjectAllocator::IsGuard(BasicBlock* block, GuardInfo* info)
{
    if (!block->KindIs(BBJ_COND))
    {
        JITDUMP("... not BBJ_COND\n");
        return nullptr;
    }

    Statement* const stmt = block->lastStmt();
    if (stmt == nullptr)
    {
        JITDUMP("... no stmt\n");
        return nullptr;
    }

    GenTree* const jumpTree = stmt->GetRootNode();
    if (!jumpTree->OperIs(GT_JTRUE))
    {
        JITDUMP("... no JTRUE\n");
        return nullptr;
    }

    GenTree* const tree = jumpTree->gtGetOp1();

    // Must be an equality or inequality
    //
    if (!tree->OperIs(GT_NE, GT_EQ))
    {
        JITDUMP("... not NE/EQ\n");
        return nullptr;
    }

    GenTree* op1     = tree->gtGetOp1();
    GenTree* op2     = tree->gtGetOp2();
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
        return nullptr;
    }

    if (!op1->TypeIs(TYP_I_IMPL))
    {
        JITDUMP("... no JTRUE(cmp(ind:int, ...))\n");
        return nullptr;
    }

    GenTree* const addr = op1->AsIndir()->Addr();

    if (!addr->TypeIs(TYP_REF))
    {
        JITDUMP("... no JTRUE(cmp(ind:int(*:ref), ...))\n");
        return nullptr;
    }

    if (!addr->OperIs(GT_LCL_VAR))
    {
        JITDUMP("... no JTRUE(cmp(ind:int(lcl:ref), ...))\n");
        return nullptr;
    }

    if (!op2->IsIconHandle(GTF_ICON_CLASS_HDL))
    {
        JITDUMP("... no JTRUE(cmp(ind:int(lcl:ref), clsHnd))\n");
        return nullptr;
    }

    // Passed the checks... fill in the info.
    //
    info->m_local  = addr->AsLclVar()->GetLclNum();
    bool isNonNull = false;
    bool isExact   = false;
    info->m_type   = (CORINFO_CLASS_HANDLE)op2->AsIntCon()->gtCompileTimeHandle;

    JITDUMP("... " FMT_BB " is guard for V%02u\n", block->bbNum, info->m_local);
    return tree;
}

//------------------------------------------------------------------------------
// CheckForGuardedUse - see if this use of lclNum is controlled by a failing
//    GDV check that we're tracking as part of conditional escape.
//
// Arguments:
//    block  - block containing tree
//    tree   - parent tree using the local
//    lclNum - local being read
//
// Returns:
//    true if this use is a conditionally escaping use.
//
bool ObjectAllocator::CheckForGuardedUse(BasicBlock* block, GenTree* tree, unsigned lclNum)
{
    // Find pseudo local...
    //
    unsigned pseudoLocal = BAD_VAR_NUM;
    if (!m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
    {
        JITDUMP("... no pseudo local?\n");
        return false;
    }

    // Verify that this call is made under a **failing** GDV test
    //
    GuardInfo info;
    if (!IsGuarded(block, tree, &info, /* testOutcome */ false))
    {
        JITDUMP("... not guarded?\n");
        return false;
    }

    // Find the GDV guard for the pseudo-local
    //
    CloneInfo* pseudoGuardInfo;
    if (!m_CloneMap.Lookup(pseudoLocal, &pseudoGuardInfo))
    {
        JITDUMP("... under non-gdv guard?\n");
        return false;
    }

    // Verify this appearance is under the same guard
    //
    if ((info.m_local == lclNum) && (pseudoGuardInfo->m_local == lclNum) && (info.m_type == pseudoGuardInfo->m_type))
    {
        // If so, track this as an assignment pseudoLocal = ...
        //
        // Later if we don't clone and split off the failing GDV paths,
        // we will mark pseudoLocal as escaped, and that will lead
        // to lclNum escaping as well.
        //
        JITDUMP("... under GDV; tracking via pseudo-local P%02u\n", pseudoLocal);
        AddConnGraphEdge(pseudoLocal, lclNum);
        return true;
    }

    JITDUMP("... under different guard?\n");
    return false;
}

//------------------------------------------------------------------------------
// CheckForGuardedAllocationOrCopy - see if this store is guarded by GDV and is
//    the store of a newly allocated object, or a copy of a local known to hold
//    references to such an object
//
// Arguments:
//    block  - block containing tree
//    stmt   - statement containing tree
//    use    - pointer to local store node
//    lclNum - local being stored to
//
// Notes:
//    Also keeps track of temporaries that convey the new object to its
//    final GDV "destination" local.
//
void ObjectAllocator::CheckForGuardedAllocationOrCopy(BasicBlock* block,
                                                      Statement*  stmt,
                                                      GenTree**   use,
                                                      unsigned    lclNum)
{
    GenTree* const tree = *use;
    assert(tree->OperIsLocalStore());

    if (!CanHavePseudoLocals())
    {
        // We didn't flag any allocations of interest during importation,
        // so there is nothing to do here.
        return;
    }

    // If we did flag allocations, we should have built dominators
    // (needed by calls to IsGuarded, below).
    //
    assert(comp->m_domTree != nullptr);
    GenTree* const data = tree->AsLclVarCommon()->Data();

    // This may be a conditional allocation. We will try and track the conditions
    // under which it escapes. GDVs are a nice subset because the conditions are stylized,
    // and the condition analysis seems tractable, and we expect the un-inlined failed
    // GDVs to be the main causes of escapes.
    //
    if (data->OperIs(GT_ALLOCOBJ))
    {
        // See if this store is made under a successful GDV test.
        //
        GuardInfo controllingGDV;
        if (IsGuarded(block, tree, &controllingGDV, /* testOutcome */ true))
        {
            // This is the allocation of concrete class under GDV.
            //
            // Find the local that will ultimately represent its uses (we have kept track of
            // this during importation and GDV expansion). Note it is usually *not* lclNum.
            //
            // We will keep special track of all accesses to this local.
            //
            Compiler::NodeToUnsignedMap* const map             = comp->getImpEnumeratorGdvLocalMap();
            unsigned                           enumeratorLocal = BAD_VAR_NUM;
            if (map->Lookup(data, &enumeratorLocal))
            {
                // If it turns out we can't stack allocate this new object even if it does not escape,
                // then don't bother setting up tracking. Note length here is just set to a nominal
                // value that won't cause failure. We will do the real length check later if we decide to allocate.
                //
                CORINFO_CLASS_HANDLE clsHnd = data->AsAllocObj()->gtAllocObjClsHnd;
                const char*          reason = nullptr;
                unsigned             size   = 0;
                unsigned             length = TARGET_POINTER_SIZE;
                if (CanAllocateLclVarOnStack(enumeratorLocal, clsHnd, OAT_NEWOBJ, length, &size, &reason,
                                             /* preliminaryCheck */ true))
                {
                    // We are going to conditionally track accesses to the enumerator local via a pseudo local.
                    //
                    const unsigned pseudoLocal = NewPseudoLocal();
                    assert(pseudoLocal != BAD_VAR_NUM);
                    bool added = m_EnumeratorLocalToPseudoLocalMap.AddOrUpdate(enumeratorLocal, pseudoLocal);

                    if (!added)
                    {
                        // Seems like we have multiple GDVs that can define this local.
                        // Carry on for now, but later we may see these collide
                        // and end up not cloning any of them.
                        //
                        // Since we are walking in RPO we may also be able to see that
                        // they are properly disjoint and things will work out just fine.
                        //
                        JITDUMP("Looks like enumerator var re-use (multiple defining GDVs)\n");
                    }

                    // We will query this info if we see CALL(enumeratorLocal)
                    // during subsequent analysis, to verify that access is
                    // under the same guard conditions.
                    //
                    CompAllocator alloc(comp->getAllocator(CMK_ObjectAllocator));
                    CloneInfo*    info    = new (alloc) CloneInfo();
                    info->m_local         = enumeratorLocal;
                    info->m_type          = clsHnd;
                    info->m_pseudoLocal   = pseudoLocal;
                    info->m_appearanceMap = new (alloc) EnumeratorVarMap(alloc);
                    info->m_allocBlock    = block;
                    info->m_allocStmt     = stmt;
                    info->m_allocTree     = data;
                    info->m_domBlock      = controllingGDV.m_block;
                    m_CloneMap.Set(pseudoLocal, info);

                    JITDUMP("Enumerator allocation [%06u]: will track accesses to V%02u guarded by type %s via P%02u\n",
                            comp->dspTreeID(data), enumeratorLocal, comp->eeGetClassName(clsHnd), pseudoLocal);

                    // If this is not a direct assignment to the enumerator var we also need to
                    // track the temps that will appear in between. Later we will rewrite these
                    // to a fresh set of temps.
                    //
                    if (lclNum != enumeratorLocal)
                    {
                        CheckForEnumeratorUse(enumeratorLocal, lclNum);
                        RecordAppearance(lclNum, block, stmt, use);
                    }
                }
                else
                {
                    JITDUMP(
                        "Enumerator allocation [%06u]: enumerator type %s cannot be stack allocated, so not tracking enumerator local V%02u\n",
                        comp->dspTreeID(data), comp->eeGetClassName(clsHnd), enumeratorLocal);
                }
            }
            else
            {
                // This allocation is not currently of interest
                //
                JITDUMP("Allocation [%06u] was not flagged for conditional escape tracking\n", comp->dspTreeID(data));
            }
        }
        else
        {
            // This allocation was not done under a GDV guard
            //
            JITDUMP("Allocation [%06u] is not under a GDV guard\n", comp->dspTreeID(data));
        }
    }
    else if (data->OperIs(GT_LCL_VAR, GT_BOX))
    {
        // See if we are copying from one enumerator-referring local to another.
        // This need not be under any guard.
        //
        unsigned srcLclNum = BAD_VAR_NUM;
        if (data->OperIs(GT_BOX))
        {
            srcLclNum = data->AsBox()->BoxOp()->AsLclVarCommon()->GetLclNum();
        }
        else
        {
            srcLclNum = data->AsLclVarCommon()->GetLclNum();
        }

        const bool isEnumeratorUse = CheckForEnumeratorUse(srcLclNum, lclNum);

        if (isEnumeratorUse)
        {
            RecordAppearance(lclNum, block, stmt, use);
        }
    }
}

//------------------------------------------------------------------------------
// CheckForEnumeratorUse - see if this is a use of an enumerator var that is
//    copied to another var.
//
// Arguments:
//    lclNum - source of the copy
//    dstLclNum - destination of the copy
//
// Returns:
//    true if this is a copy
//
bool ObjectAllocator::CheckForEnumeratorUse(unsigned lclNum, unsigned dstLclNum)
{
    unsigned pseudoLocal = BAD_VAR_NUM;

    if (m_EnumeratorLocalToPseudoLocalMap.TryGetValue(dstLclNum, &pseudoLocal))
    {
        // We already knew dstLclNum was a potential copy
        //
        return true;
    }

    if (!m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
    {
        // lclNum is not a potential source
        //
        return false;
    }

    CloneInfo* info = nullptr;
    if (!m_CloneMap.Lookup(pseudoLocal, &info))
    {
        // We aren't interested in locals under this guard
        //
        return false;
    }

    // lclNum is an interesting enumerator var, so now so is dstLclNum.
    //
    const bool added = m_EnumeratorLocalToPseudoLocalMap.AddOrUpdate(dstLclNum, pseudoLocal);

    assert(added);

    JITDUMP("Enumerator allocation: will also track accesses to V%02u via P%02u\n", dstLclNum, pseudoLocal);

    if (info->m_allocTemps == nullptr)
    {
        CompAllocator alloc(comp->getAllocator(CMK_ObjectAllocator));
        info->m_allocTemps = new (alloc) jitstd::vector<unsigned>(alloc);
    }

    info->m_allocTemps->push_back(dstLclNum);

    return true;
}

//------------------------------------------------------------------------------
// RecordAppearance: note info about an enumerator var appearance
//
// Arguments:
//   lclNum -- enumerator var
//   block  -- block holding the stmt
//   stmt   -- stmt holding the use
//   use    -- local var reference
//
void ObjectAllocator::RecordAppearance(unsigned lclNum, BasicBlock* block, Statement* stmt, GenTree** use)
{
    unsigned pseudoLocal = BAD_VAR_NUM;
    if (!m_EnumeratorLocalToPseudoLocalMap.TryGetValue(lclNum, &pseudoLocal))
    {
        return;
    }

    CloneInfo* info;
    if (!m_CloneMap.Lookup(pseudoLocal, &info))
    {
        return;
    }

    GenTree* const tree  = *use;
    bool const     isDef = tree->OperIsLocalStore();

    JITDUMP("Found enumerator V%02u %s at [%06u]\n", lclNum, isDef ? "def" : "use", comp->dspTreeID(tree));

    CompAllocator           alloc(comp->getAllocator(CMK_ObjectAllocator));
    EnumeratorVarMap* const varMap = info->m_appearanceMap;
    assert(varMap != nullptr);

    EnumeratorVar* v = nullptr;
    if (!varMap->Lookup(lclNum, &v))
    {
        v                = new (alloc) EnumeratorVar();
        v->m_appearances = new (alloc) jitstd::vector<EnumeratorVarAppearance*>(alloc);
        varMap->Set(lclNum, v);
    }

    EnumeratorVarAppearance* const a = new (alloc) EnumeratorVarAppearance(block, stmt, use, lclNum, isDef);

    if (isDef)
    {
        if (v->m_def != nullptr)
        {
            if (!v->m_hasMultipleDefs)
            {
                JITDUMP("Enumerator V%02u has multiple defs\n");
                v->m_hasMultipleDefs = true;
            }
        }
        else
        {
            v->m_def = a;
        }

        if (stmt == info->m_allocStmt)
        {
            v->m_isInitialAllocTemp = true;
        }
    }

    v->m_appearances->push_back(a);

    info->m_appearanceCount++;
}

//------------------------------------------------------------------------------
// CloneOverlaps: check if this cloning would overlap with other clonings
//
// Arguments:
//   info -- [in, out] info about the cloning opportunity
//
// Returns:
//   true if cloning overlaps with some other cloning
//
bool ObjectAllocator::CloneOverlaps(CloneInfo* info)
{
    bool         overlaps = false;
    BitVecTraits traits(comp->compBasicBlockID, comp);

    for (CloneInfo* const c : CloneMap::ValueIteration(&m_CloneMap))
    {
        if (c == info)
        {
            continue;
        }

        if (!c->m_willClone)
        {
            continue;
        }

        if (BitVecOps::IsEmptyIntersection(&traits, info->m_blocks, c->m_blocks))
        {
            continue;
        }

        JITDUMP("Cloned blocks for P%02u overlap with those for P%02u; unable to clone\n", info->m_pseudoLocal,
                c->m_pseudoLocal);

        overlaps = true;
        break;
    }

    return overlaps;
}

//------------------------------------------------------------------------------
// ShouldClone: check if this cloning looks profitable
//
// Arguments:
//   info        -- info about a cloning opportunity
//
// Returns:
//   true if cloning looks profitable
//
bool ObjectAllocator::ShouldClone(CloneInfo* info)
{
    // For now, use the same cloning size limit we use for loop cloning
    //
    int const      sizeConfig  = JitConfig.JitCloneLoopsSizeLimit();
    unsigned const sizeLimit   = (sizeConfig >= 0) ? (unsigned)sizeConfig : UINT_MAX;
    unsigned       size        = 0;
    bool           shouldClone = true;

    for (BasicBlock* const block : *info->m_blocksToClone)
    {
        // Note this overstates the size a bit since we'll resolve GDVs
        // in the clone and the original...
        //
        unsigned const slack     = sizeLimit - size;
        unsigned       blockSize = 0;
        if (block->ComplexityExceeds(comp, slack, &blockSize))
        {
            JITDUMP("Rejecting P%02u cloning: exceeds size limit %u\n", info->m_pseudoLocal, sizeLimit);
            return false;
        }
        size += blockSize;
    }

    // TODO: some kind of profile check...
    //
    JITDUMP("Accepting P%02u cloning: size %u does not exceed size limit %u\n", info->m_pseudoLocal, size, sizeLimit);
    return true;
}

//------------------------------------------------------------------------------
// CanClone: check that cloning can remove all escaping references and
//   is a reasonble thing to do
//
// Arguments:
//   info -- [in, out] info about the cloning opportunity
//
// Returns:
//   true if cloning can remove all escaping references
//
bool ObjectAllocator::CanClone(CloneInfo* info)
{
    // If we already analyzed this case, return what we learned before.
    //
    if (!info->m_checkedCanClone)
    {
        CheckCanClone(info);
        info->m_checkedCanClone = true;
    }

    return info->m_canClone;
}

//------------------------------------------------------------------------------
// CheckCanClone: check that cloning can remove all escaping references and
//   is a reasonble thing to do
//
// Arguments:
//   info -- [in, out] info about the cloning opportunity
//
// Returns:
//   true if cloning can remove all escaping references
//
bool ObjectAllocator::CheckCanClone(CloneInfo* info)
{
    assert(!info->m_checkedCanClone);
    JITDUMP("** Seeing if we can clone to guarantee non-escape under V%02u\n", info->m_local);
    BasicBlock* const allocBlock = info->m_allocBlock;

    // The allocation site must not be in a loop (stack allocation limitation)
    //
    // Note if we can prove non-escape but can't stack allocate, we might be
    // able to light up an "object is thread exclusive" mode and effectively
    // promote the fields anyways.
    //
    if (allocBlock->HasFlag(BBF_BACKWARD_JUMP))
    {
        JITDUMP("allocation block " FMT_BB " is (possibly) in a loop\n", allocBlock->bbNum);
        return false;
    }

    // Heuristic: if the allocation block was not profiled, or is hit less than 10% of
    // the time this method is called, bail... (note we should really look at weight of uses,
    // not the weight of the allocation).
    //
    if (!allocBlock->hasProfileWeight())
    {
        JITDUMP("alloc block " FMT_BB " was not profiled\n", allocBlock->bbNum);
        return false;
    }

    const weight_t thinProfile = 0.1;
    const weight_t allocWeight = allocBlock->getBBWeight(comp);

    if (allocWeight < thinProfile)
    {
        JITDUMP("alloc block " FMT_BB " relative profile weight too low: " FMT_WT " < " FMT_WT "\n", allocBlock->bbNum,
                allocWeight, thinProfile);
        return false;
    }

    // We should know the full set of locals that can refer to the newly allocated
    // object in the blocks we intend to clone (and beyond). Verify those have the
    // expected def-use behavior.
    //
    // The goal of all this is to try and ensure that if we rewrite all the T,V,U appearances
    // to new locals in the cloned code we get proper behavior.
    //
    // There is one distinguished local V (info.m_local) that holds the result of the
    // initial GDV and is the local tested in subsequent GDVs. It must have a single def.
    //
    // The other locals are either temps T that refer to the allocated object between
    // allocation site and the def of V, or temps U that are copies of V in the code
    // dominated by the def of V.
    //
    // For the T's, there is a "first" T0 that is the destination of the ALLOCOBJ
    // and a "last" Tv that is the source of the assignment to V, and some intermediates Ti.
    //
    // T0 & all Ti should be single def, all uses dominated by their defs.
    // All Ti def sources should be another T.
    //
    // All Ti appearances should be postdominated by the def of V, but we don't explicitly
    // check this -- instead we have verified the path from the alloc block to the def of V.
    //
    // Tv may have two defs (the other thanks to the "empty static" pattern). If so, we can
    // ignore the def not dominated by the allocation block, since we are selectively
    // cloning along a path from this block down to the def of V.
    //
    // Tv's use should be at the def of V.
    //
    // For the U's: all Ui appearances should be dominated by the def of V; all Ui defs
    // should have another Ui or V as their source. (We should also verify each Ui is
    // single-def and the def dominates all the Ui uses, but this may not work out...?)
    //
    // Also we do not expect any Ti or Ui use to be a GDV guard. U's typically arise from
    // inlining under a successful GDV of V, and should have exact types, resolving any
    // potential GDV in the inlinee.
    //
    // First, find the one and only def of V (aka `defBlock`).
    //
    EnumeratorVar* v      = nullptr;
    bool const     foundV = info->m_appearanceMap->Lookup(info->m_local, &v);

    if (!foundV)
    {
        JITDUMP("Unexpected: no appearance info for V%02u\n", info->m_local);
        return false;
    }

    if (v->m_hasMultipleDefs)
    {
        JITDUMP("Unexpected: V%02u multiply defined\n", info->m_local);
        return false;
    }

    BasicBlock* const defBlock = v->m_def->m_block;
    Statement* const  defStmt  = v->m_def->m_stmt;

    JITDUMP("V%02u has single def in " FMT_BB " at [%06u]\n", info->m_local, defBlock->bbNum,
            comp->dspTreeID(defStmt->GetRootNode()));

    // We expect to be able to follow all paths from alloc block to defBlock
    // without reaching "beyond" defBlock.
    //
    // Because we are inside a GDV hammock, we do not expect to see a normal
    // flow path from allocBlock that can bypass defBlock. For now we trust
    // that is the case.
    //
    // toVisit: blocks we need to visit to determine extent of cloning
    // visited: block we will need to clone
    // toVisitTryEntry: subset of above that are try entries.
    //
    CompAllocator                alloc(comp->getAllocator(CMK_ObjectAllocator));
    ArrayStack<BasicBlock*>      toVisit(alloc);
    jitstd::vector<BasicBlock*>* visited         = new (alloc) jitstd::vector<BasicBlock*>(alloc);
    jitstd::vector<BasicBlock*>* toVisitTryEntry = new (alloc) jitstd::vector<BasicBlock*>(alloc);

    BitVecTraits traits(comp->compBasicBlockID, comp);
    BitVec       visitedBlocks(BitVecOps::MakeEmpty(&traits));
    toVisit.Push(allocBlock);
    BitVecOps::AddElemD(&traits, visitedBlocks, allocBlock->bbID);

    // We don't expect to have to search very far
    //
    unsigned       searchCount = 0;
    unsigned const searchLimit = 25;

    while (toVisit.Height() > 0)
    {
        BasicBlock* const block = toVisit.Pop();

        if (searchCount > searchLimit)
        {
            JITDUMP("Too many blocks between alloc and def block\n");
            return false;
        }

        if (block != allocBlock)
        {
            visited->push_back(block);
        }

        // We expect this stretch of blocks to all be in the same EH region.
        //
        if (!BasicBlock::sameEHRegion(allocBlock, block))
        {
            JITDUMP("Unexpected: new EH region at " FMT_BB "\n", block->bbNum);
            return false;
        }

        if (block == defBlock)
        {
            continue;
        }

        JITDUMP("walking through " FMT_BB "\n", block->bbNum);

        block->VisitRegularSuccs(comp, [&](BasicBlock* succ) {
            if (BitVecOps::TryAddElemD(&traits, visitedBlocks, succ->bbID))
            {
                toVisit.Push(succ);
            }
            return BasicBlockVisit::Continue;
        });
    }

    JITDUMP("def block " FMT_BB " post-dominates allocation site " FMT_BB "\n", defBlock->bbNum, allocBlock->bbNum);

    // -1 here since we won't need to clone the allocation site itself.
    //
    JITDUMP("allocation side cloning: %u blocks\n", visited->size() - 1);

    // The allocationBlock should not dominate the defBlock.
    // (if it does, optimization does not require cloning, as
    // there should be only one reaching def...)
    //
    if (comp->m_domTree->Dominates(allocBlock, defBlock))
    {
        JITDUMP("Unexpected, alloc site " FMT_BB " dominates def block " FMT_BB "\n", allocBlock->bbNum,
                defBlock->bbNum);

        return false;
    }
    // Classify the other local appearances
    // as Ts (allocTemps) or Us (useTemps), and look for guard appearances.
    //
    for (unsigned lclNum : EnumeratorVarMap::KeyIteration(info->m_appearanceMap))
    {
        EnumeratorVar* ev = nullptr;
        info->m_appearanceMap->Lookup(lclNum, &ev);
        assert(ev != nullptr);

        for (EnumeratorVarAppearance* const a : *(ev->m_appearances))
        {
            // If this is a use, see if it's part of a GDV guard.
            //
            if (!a->m_isDef && (a->m_stmt == a->m_block->lastStmt()))
            {
                GuardInfo      tempInfo;
                GenTree* const guardingRelop = IsGuard(a->m_block, &tempInfo);
                a->m_isGuard                 = (guardingRelop != nullptr);
            }

            // If this is V, we're done
            //
            if (lclNum == info->m_local)
            {
                continue;
            }

            // Since defBlock postdominates all Ts and dominates all Us,
            // we can use dfs numbers to sort which temps are Ts and which are Us.
            //
            if (defBlock->bbPostorderNum < a->m_block->bbPostorderNum)
            {
                ev->m_isAllocTemp = true;
            }
            else if (defBlock->bbPostorderNum == a->m_block->bbPostorderNum)
            {
                if (defStmt == a->m_stmt)
                {
                    ev->m_isAllocTemp      = true;
                    ev->m_isFinalAllocTemp = true;
                }
                else if (comp->gtLatestStatement(defStmt, a->m_stmt) == a->m_stmt)
                {
                    ev->m_isUseTemp = true;
                }
                else
                {
                    ev->m_isAllocTemp = true;
                }
            }
            else
            {
                ev->m_isUseTemp = true;
            }

            // We don't expect to see allocTemps or useTemps in guards
            //
            if (a->m_isGuard)
            {
                JITDUMP("Unexpected: %s temp V%02u is GDV guard at " FMT_BB "\n", ev->m_isAllocTemp ? "alloc" : "use",
                        a->m_lclNum, a->m_block->bbNum);
                return false;
            }
        }

        // We don't expect a temp to be both an alloc temp and a use temp.
        //
        if (ev->m_isAllocTemp && ev->m_isUseTemp)
        {
            JITDUMP("Unexpected: temp V%02u has appearances both before and after main var assignment in " FMT_BB "\n",
                    lclNum, defBlock->bbNum);

            return false;
        }

        if (ev->m_isAllocTemp || ev->m_isUseTemp)
        {
            JITDUMP("Temp V%02u is a %s temp", lclNum, ev->m_isAllocTemp ? "alloc" : "use");
            if (ev->m_isInitialAllocTemp)
            {
                JITDUMP(" [initial]");
            }
            if (ev->m_isFinalAllocTemp)
            {
                JITDUMP(" [final]");
            }
            JITDUMP("\n");
        }
    }

    // The allocation block must dominate all T appearances, save for the final T use.
    //
    for (unsigned lclNum : EnumeratorVarMap::KeyIteration(info->m_appearanceMap))
    {
        EnumeratorVar* ev = nullptr;
        info->m_appearanceMap->Lookup(lclNum, &ev);
        assert(ev != nullptr);

        if (!ev->m_isAllocTemp)
        {
            continue;
        }

        for (EnumeratorVarAppearance* const a : *(ev->m_appearances))
        {
            if (ev->m_isFinalAllocTemp && (a->m_block == defBlock) && !a->m_isDef)
            {
                continue;
            }

            if (!comp->m_domTree->Dominates(allocBlock, a->m_block))
            {
                JITDUMP("Alloc temp V%02u %s in " FMT_BB " not dominated by alloc " FMT_BB "\n", a->m_lclNum,
                        a->m_isDef ? "def" : "use", a->m_block->bbNum, allocBlock->bbNum);
                return false;
            }
        }
    }

    // The definition block must dominate all the V and U uses.
    //
    // Also collect up all blocks with U appearances; these will help
    // us figure out the full extent of cloning.
    //
    for (unsigned lclNum : EnumeratorVarMap::KeyIteration(info->m_appearanceMap))
    {
        EnumeratorVar* ev = nullptr;
        info->m_appearanceMap->Lookup(lclNum, &ev);
        assert(ev != nullptr);

        if (ev->m_isAllocTemp)
        {
            continue;
        }

        for (EnumeratorVarAppearance* const a : *(ev->m_appearances))
        {
            BasicBlock* const aBlock = a->m_block;
            if (!comp->m_domTree->Dominates(defBlock, aBlock))
            {
                JITDUMP("%sV%02u %s in " FMT_BB " not dominated by def " FMT_BB "\n", ev->m_isUseTemp ? "Use temp" : "",
                        lclNum, a->m_isDef ? "def" : "use", a->m_block->bbNum, defBlock->bbNum);
                return false;
            }

            if (BitVecOps::TryAddElemD(&traits, visitedBlocks, aBlock->bbID))
            {
                toVisit.Push(aBlock);
            }
        }
    }

    JITDUMP("The defBlock dominates the right set of enumerator var uses\n");

    // Determine the initial extent of the cloned region dominated by
    // the def block.
    //
    // Walk back from each use block until we hit closure.
    //
    while (toVisit.Height() > 0)
    {
        BasicBlock* const block = toVisit.Pop();
        visited->push_back(block);

        // If we see try region entries here, we will handle them below.
        //
        if (comp->bbIsTryBeg(block))
        {
            toVisitTryEntry->push_back(block);
        }

        JITDUMP("walking back through " FMT_BB "\n", block->bbNum);

        for (FlowEdge* predEdge = comp->BlockPredsWithEH(block); predEdge != nullptr;
             predEdge           = predEdge->getNextPredEdge())
        {
            BasicBlock* const predBlock = predEdge->getSourceBlock();

            // We should not be able to reach an un-dominated block.
            // (consider eh paths?)
            //
            assert(comp->m_domTree->Dominates(defBlock, predBlock));
            if (BitVecOps::TryAddElemD(&traits, visitedBlocks, predBlock->bbID))
            {
                toVisit.Push(predBlock);
            }
        }
    }

    JITDUMP("total cloning including all enumerator uses: %u blocks\n", visited->size() - 1);
    unsigned numberOfEHRegionsToClone = 0;

    // Now expand the clone block set to include any try regions that need cloning.
    //
    unsigned                    numberOfTryRegionsToClone = 0;
    CloneTryInfo                cloneInfo(traits);
    jitstd::vector<BasicBlock*> tryBlocks(alloc);
    cloneInfo.BlocksToClone = &tryBlocks;

    // Order the try regions to visit from outer to inner
    //
    struct bbTryIndexCmp
    {
        bool operator()(const BasicBlock* bb1, const BasicBlock* bb2)
        {
            return bb1->getTryIndex() > bb2->getTryIndex();
        }
    };
    jitstd::sort(toVisitTryEntry->begin(), toVisitTryEntry->end(), bbTryIndexCmp());

    for (BasicBlock* const block : *toVisitTryEntry)
    {
        if (BitVecOps::IsMember(&traits, cloneInfo.Visited, block->bbID))
        {
            // nested region
            continue;
        }

        // This will not clone, but will check if cloning is possible
        //
        BasicBlock* const result = comp->fgCloneTryRegion(block, cloneInfo);

        if (result == nullptr)
        {
            return false;
        }

        numberOfTryRegionsToClone++;
    }

    // Merge visited and cloneInfo visited
    //
    for (BasicBlock* const block : tryBlocks)
    {
        if (BitVecOps::TryAddElemD(&traits, visitedBlocks, block->bbID))
        {
            visited->push_back(block);
        }
    }

    // Sort blocks to visit in RPO
    //
    struct bbRpoCmp
    {
        bool operator()(const BasicBlock* bb1, const BasicBlock* bb2)
        {
            return bb1->bbPostorderNum > bb2->bbPostorderNum;
        }
    };

    jitstd::sort(visited->begin(), visited->end(), bbRpoCmp());

    assert(defBlock->hasProfileWeight());

    // Determine the profile scale factor.
    //
    weight_t weightForClone = 0.0;

    for (FlowEdge* const predEdge : defBlock->PredEdges())
    {
        if (BitVecOps::IsMember(&traits, visitedBlocks, predEdge->getSourceBlock()->bbID))
        {
            weightForClone += predEdge->getLikelyWeight();
        }
    }

    weight_t scaleFactor = max(1.0, weightForClone / defBlock->bbWeight);
    info->m_profileScale = scaleFactor;
    JITDUMP("Profile weight for clone " FMT_WT " overall " FMT_WT ", will scale clone at " FMT_WT "\n", weightForClone,
            defBlock->bbWeight, scaleFactor);

    // Save off blocks that we need to clone
    //
    info->m_blocksToClone = visited;
    info->m_blocks        = visitedBlocks;
    info->m_canClone      = true;

    JITDUMP("total cloning including all uses and subsequent EH: %u blocks\n",
            BitVecOps::Count(&traits, visitedBlocks));

    comp->Metrics.EnumeratorGDVCanCloneToEnsureNoEscape++;
    return true;
}

//------------------------------------------------------------------------------
// CloneAndSpecialize: clone and specialize blocks and statements so that
//   an enumerator allocation does not escape
//
// Arguments:
//   info -- info about the cloning opportunity
//
//
// Notes:
//   The cloned blocks become the "fast path" where the enumerator object allocated
//   in info.m_allocBlock is used. The original blocks are the slow path where it
//   is unclear which object (and which type of object) is used.
//
//   In the cloned blocks, the enumerator local is updated to a new local.
//
void ObjectAllocator::CloneAndSpecialize(CloneInfo* info)
{
    assert(info->m_canClone);
    assert(info->m_willClone);
    JITDUMP("\nCloning to ensure allocation at " FMT_BB " does not escape\n", info->m_allocBlock->bbNum);

    // Clone blocks in RPO order. If we find a try entry, clone that as a whole,
    // and skip over those blocks subsequently.
    //
    BlockToBlockMap map(comp->getAllocator(CMK_ObjectAllocator));

    // If there is an enclosing EH region, insert the new blocks at the end of this
    // region. Otherwise insert the new blocks just after the allocation site
    //
    BasicBlock* insertionPoint = info->m_allocBlock;

    bool     inTry             = false;
    unsigned enclosingEHRegion = comp->ehGetMostNestedRegionIndex(insertionPoint, &inTry);

    if (enclosingEHRegion != 0)
    {
        EHblkDsc* const ebd = comp->ehGetDsc(enclosingEHRegion - 1);

        if (inTry)
        {
            insertionPoint = ebd->ebdTryLast;
        }
        else
        {
            insertionPoint = ebd->ebdHndLast;
        }

        JITDUMP("Will insert new blocks at end of enclosing EH#%u %s region " FMT_BB "\n", enclosingEHRegion - 1,
                inTry ? "try" : "handler", insertionPoint->bbNum);
    }
    else
    {
        JITDUMP("Will insert new blocks after allocation block " FMT_BB "\n", insertionPoint->bbNum);
    }
    BasicBlock**      insertAfter = &insertionPoint;
    BasicBlock* const oldLast     = insertionPoint;

    // Compute profile scale for the original blocks.
    //
    weight_t originalScale = max(0.0, 1.0 - info->m_profileScale);

    // Seems like if the region exits the try the RPO could mix
    // try and non-try blocks... hmm.
    //
    for (BasicBlock* const block : *info->m_blocksToClone)
    {
        BasicBlock* newBlock = nullptr;
        const bool  isCloned = map.Lookup(block, &newBlock);

        if (isCloned)
        {
            assert(newBlock != nullptr);
            continue;
        }

        if (comp->bbIsTryBeg(block))
        {
            BitVecTraits traits(comp->compBasicBlockID, comp);
            CloneTryInfo cloneTryInfo(traits);
            cloneTryInfo.Map                       = &map;
            cloneTryInfo.AddEdges                  = false;
            cloneTryInfo.ProfileScale              = info->m_profileScale;
            cloneTryInfo.ScaleOriginalBlockProfile = true;
            comp->fgCloneTryRegion(block, cloneTryInfo, insertAfter);
            continue;
        }

        newBlock = comp->fgNewBBafter(BBJ_ALWAYS, *insertAfter, /* extendRegion */ false);
        JITDUMP("Adding " FMT_BB " (copy of " FMT_BB ") after " FMT_BB "\n", newBlock->bbNum, block->bbNum,
                (*insertAfter)->bbNum);
        BasicBlock::CloneBlockState(comp, newBlock, block);

        assert(newBlock->bbRefs == 0);
        newBlock->scaleBBWeight(info->m_profileScale);
        block->scaleBBWeight(originalScale);
        map.Set(block, newBlock, BlockToBlockMap::Overwrite);
        *insertAfter = newBlock;
    }

    // Fix up flow..
    //
    for (BasicBlock* const block : *info->m_blocksToClone)
    {
        BasicBlock* newBlock = nullptr;
        const bool  isCloned = map.Lookup(block, &newBlock);
        assert(isCloned && (newBlock != nullptr));
        assert(!newBlock->HasInitializedTarget());
        JITDUMP("Updating targets: " FMT_BB " mapped to " FMT_BB "\n", block->bbNum, newBlock->bbNum);
        comp->optSetMappedBlockTargets(block, newBlock, &map);
    }

    // Fix up any enclosing EH extents
    //
    if (enclosingEHRegion != 0)
    {
        // Note enclosing region index may shift because of EH cloning, so refetch it.
        //
        bool     postCloneInTry             = false;
        unsigned postCloneEnclosingEHRegion = comp->ehGetMostNestedRegionIndex(info->m_allocBlock, &postCloneInTry);
        assert(postCloneEnclosingEHRegion >= enclosingEHRegion);
        assert(inTry == postCloneInTry);

        // Now update the extents
        //
        BasicBlock* const newLast = *insertAfter;
        EHblkDsc* const   ebd     = comp->ehGetDsc(enclosingEHRegion - 1);
        for (EHblkDsc* const HBtab : EHClauses(comp, ebd))
        {
            if (HBtab->ebdTryLast == oldLast)
            {
                comp->fgSetTryEnd(HBtab, newLast);
            }
            if (HBtab->ebdHndLast == oldLast)
            {
                comp->fgSetHndEnd(HBtab, newLast);
            }
        }
    }

    // Create a new local for the enumerator uses in the cloned code
    //
    // Note: we will map all the allocTemp and useTemp appearances to
    // this variable as well.
    //
    unsigned const newEnumeratorLocal = comp->lvaGrabTemp(/* shortLifetime */ false DEBUGARG("fast-path enumerator"));
    info->m_enumeratorLocal           = newEnumeratorLocal;

    // Type for now as TYP_REF; this will get rewritten later during RewriteUses
    //
    comp->lvaTable[newEnumeratorLocal].lvType      = TYP_REF;
    comp->lvaTable[newEnumeratorLocal].lvSingleDef = 1;
    comp->lvaSetClass(newEnumeratorLocal, info->m_type, /* isExact */ true);

    class ReplaceVisitor final : public GenTreeVisitor<ReplaceVisitor>
    {
        CloneInfo* m_info;
        unsigned   m_newLclNum;

    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        bool MadeChanges = false;

        ReplaceVisitor(Compiler* comp, CloneInfo* info, unsigned newLclNum)
            : GenTreeVisitor(comp)
            , m_info(info)
            , m_newLclNum(newLclNum)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            // We could just bash all defs save for the initial temp def
            // but this would make validating substitutions a bit harder,
            // as some defs or uses would vanish.
            //
            GenTreeLclVarCommon* const node = (*use)->AsLclVarCommon();
            if (node->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
            {
                EnumeratorVar* ev = nullptr;
                if (m_info->m_appearanceMap->Lookup(node->GetLclNum(), &ev))
                {
                    // We leave the initial alloc temp as is; the the
                    // object allocator rewriting will take care of it.
                    //
                    if (!ev->m_isInitialAllocTemp)
                    {
                        node->SetLclNum(m_newLclNum);
                    }
                }
                MadeChanges = true;
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    ReplaceVisitor visitor(comp, info, newEnumeratorLocal);

    // Rewrite enumerator var uses in the cloned (fast) blocks to reference
    // the new enumerator local.
    //
    // Specialize GDV tests in the cloned blocks to always return true.
    //
    // Note we'd figure this out eventually anyways (since the new enumerator
    // var has an the exact type, but we want to accelerate this so that our new
    // enumerator var does not appear to be exposed.
    //
    // Specialize GDV tests in the original code to always return false.
    //
    // We would not figure this out eventually anyways, as the unknown
    // enumerator may well have the right type. The main goal here is
    // to block GDV-inspired cloning of the "slow" loop.
    //
    // (This is inefficient/odd, because for copies or trees with multiple
    // uses we will process each one twice or more).
    //
    // Also we are leaving self-copies around (eg V20 = V20) and this seems
    // to confuse local morph. We can't fix these on the fly because
    // the tree visitor won't do post-order local only traversals. Grr.
    //
    CompAllocator          alloc(comp->getAllocator(CMK_ObjectAllocator));
    ArrayStack<Statement*> defStmts(alloc);

    for (unsigned lclNum : EnumeratorVarMap::KeyIteration(info->m_appearanceMap))
    {
        EnumeratorVar* ev = nullptr;
        info->m_appearanceMap->Lookup(lclNum, &ev);
        assert(ev != nullptr);

        for (EnumeratorVarAppearance* const a : *(ev->m_appearances))
        {
            // We do not rewrite the initial temp appearances. These will be rewritten
            // when the ALLOCOBJ is turned into a stack allocation.
            //
            if (ev->m_isInitialAllocTemp)
            {
                continue;
            }

            // Also, we do not clone the allocBlock, but we may need to rewrite
            // some appearances there.
            //
            BasicBlock* newBlock = nullptr;

            if (a->m_block == info->m_allocBlock)
            {
                newBlock = info->m_allocBlock;

                JITDUMP("Updating V%02u %s in " FMT_BB " (allocation block) to V%02u\n", a->m_lclNum,
                        a->m_isDef ? "def" : "use", newBlock->bbNum, newEnumeratorLocal);
            }
            else
            {
                const bool isCloned = map.Lookup(a->m_block, &newBlock);
                assert(isCloned && (newBlock != nullptr));

                JITDUMP("Updating V%02u %s in " FMT_BB " (clone of " FMT_BB ") to V%02u\n", a->m_lclNum,
                        a->m_isDef ? "def" : "use", newBlock->bbNum, a->m_block->bbNum, newEnumeratorLocal);
            }

            // Find matching stmt/tree in the clone, and update it
            // ... note we could simplify this for the allocBlock case
            //
            Statement* clonedStmt = newBlock->firstStmt();
            for (Statement* const stmt : a->m_block->Statements())
            {
                if (stmt == a->m_stmt)
                {
                    JITDUMP("Before\n");
                    DISPTREE(clonedStmt->GetRootNode());

                    // walk and replace
                    visitor.MadeChanges = false;
                    visitor.WalkTree(clonedStmt->GetRootNodePointer(), nullptr);

                    JITDUMP("After\n");
                    DISPTREE(clonedStmt->GetRootNode());

                    assert(visitor.MadeChanges);

                    if (a->m_isDef)
                    {
                        defStmts.Push(clonedStmt);
                    }
                    break;
                }

                clonedStmt = clonedStmt->GetNextStmt();
            }

            if (!a->m_isGuard)
            {
                continue;
            }

            {
                // Original/Slow path -- gdv will always fail
                //
                GuardInfo      slowGuardInfo;
                GenTree* const slowGuardRelop = IsGuard(a->m_block, &slowGuardInfo);
                assert(slowGuardRelop != nullptr);
                bool const      keepTrueEdge = slowGuardRelop->OperIs(GT_NE);
                FlowEdge* const retainedEdge = keepTrueEdge ? a->m_block->GetTrueEdge() : a->m_block->GetFalseEdge();
                FlowEdge* const removedEdge  = keepTrueEdge ? a->m_block->GetFalseEdge() : a->m_block->GetTrueEdge();

                JITDUMP("Modifying slow path GDV guard " FMT_BB " to always branch to " FMT_BB "\n", a->m_block->bbNum,
                        retainedEdge->getDestinationBlock()->bbNum);
                comp->fgRemoveRefPred(removedEdge);
                a->m_block->SetKindAndTargetEdge(BBJ_ALWAYS, retainedEdge);
                a->m_block->lastStmt()->SetRootNode(slowGuardRelop);
                comp->fgRepairProfileCondToUncond(a->m_block, retainedEdge, removedEdge);
            }

            {
                // Cloned/Fast path -- gdv will always succeed
                //
                GuardInfo      fastGuardInfo;
                GenTree* const fastGuardRelop = IsGuard(newBlock, &fastGuardInfo);
                assert(fastGuardRelop != nullptr);
                bool const      keepTrueEdge = fastGuardRelop->OperIs(GT_EQ);
                FlowEdge* const retainedEdge = keepTrueEdge ? newBlock->GetTrueEdge() : newBlock->GetFalseEdge();
                FlowEdge* const removedEdge  = keepTrueEdge ? newBlock->GetFalseEdge() : newBlock->GetTrueEdge();

                JITDUMP("Modifying fast path GDV guard " FMT_BB " to always branch to " FMT_BB "\n", newBlock->bbNum,
                        retainedEdge->getDestinationBlock()->bbNum);
                comp->fgRemoveRefPred(removedEdge);
                newBlock->SetKindAndTargetEdge(BBJ_ALWAYS, retainedEdge);
                newBlock->lastStmt()->SetRootNode(fastGuardRelop);
                comp->fgRepairProfileCondToUncond(newBlock, retainedEdge, removedEdge);
            }
        }
    }

    // Now revisit all the cloned def stmts, and remove any that are self-assignments.
    //
    while (defStmts.Height() > 0)
    {
        Statement* const defStmt  = defStmts.Pop();
        GenTree* const   rootNode = defStmt->GetRootNode();

        if (rootNode->OperIs(GT_STORE_LCL_VAR))
        {
            GenTree* const data = rootNode->AsOp()->Data();
            if (data->OperIs(GT_LCL_VAR))
            {
                if (rootNode->AsLclVarCommon()->GetLclNum() == data->AsLclVarCommon()->GetLclNum())
                {
                    JITDUMP("Bashing self-copy [%06u] to NOP\n", comp->dspTreeID(rootNode));
                    rootNode->gtBashToNOP();
                }
            }
        }
    }

    // Modify the allocation block to branch to the start of the fast path
    //
    BasicBlock* const firstBlock       = (*info->m_blocksToClone)[0];
    BasicBlock*       firstClonedBlock = nullptr;
    bool const        firstFound       = map.Lookup(firstBlock, &firstClonedBlock);
    assert(firstFound);
    comp->fgRedirectTargetEdge(info->m_allocBlock, firstClonedBlock);

    // If we are subsequently going to do the "empty collection static enumerator" opt,
    // then our profile is now consistent.
    //
    if ((info->m_allocTree->gtFlags & GTF_ALLOCOBJ_EMPTY_STATIC) != 0)
    {
        JITDUMP("Anticipating the empty-collection static enumerator opt for [%06u],"
                " so not adjusting profile in the initial GDV region\n",
                comp->dspTreeID(info->m_allocTree));
        return;
    }

    // If not, we need to do more profile repair in the region from the
    // allocation-dominating GDV down to the (now cloned) defBlock.
    //
    JITDUMP("Profile data needs more repair. Data %s inconsistent.\n",
            comp->fgPgoConsistent ? "is now" : "was already");

    if (comp->fgPgoConsistent)
    {
        comp->fgPgoConsistent = false;
    }
}

//------------------------------------------------------------------------------
// CloneAndSpecializeAll: clone and specialize any regions needed to guarantee
//   objects don't escape
//
void ObjectAllocator::CloneAndSpecialize()
{
    unsigned numberOfClonedRegions = 0;

    for (CloneInfo* const c : CloneMap::ValueIteration(&m_CloneMap))
    {
        if (!c->m_willClone)
        {
            continue;
        }

        CloneAndSpecialize(c);
        numberOfClonedRegions++;
    }

    assert(numberOfClonedRegions == m_regionsToClone);
}
