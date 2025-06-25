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
// ObjectAllocator: construct the object allocator object
//
// Arguments:
//    comp - compiler instance
//
// Notes:
//    Runs only if Compiler::optMethodFlags has flag OMF_HAS_NEWOBJ or OMF_HAS_NEWARR.
//
//    Builds a connection graph where nodes mostly represent gc typed local vars,
//    showing how these locals can assign values to one another.
//
//    The graph also includes a abstract node types: a node representing an unknown source of values,
//    pseudo nodes representing assignments that only happen under particular conditions,
//    and nodes representing fields of local structs.
//
ObjectAllocator::ObjectAllocator(Compiler* comp)
    : Phase(comp, PHASE_ALLOCATE_OBJECTS)
    , m_IsObjectStackAllocationEnabled(false)
    , m_AnalysisDone(false)
    , m_isR2R(comp->IsReadyToRun())
    , m_bvCount(0)
    , m_bitVecTraits(BitVecTraits(comp->lvaCount, comp))
    , m_unknownSourceIndex(BAD_VAR_NUM)
    , m_HeapLocalToStackObjLocalMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_HeapLocalToStackArrLocalMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_ConnGraphAdjacencyMatrix(nullptr)
    , m_StackAllocMaxSize(0)
    , m_stackAllocationCount(0)
    , m_EnumeratorLocalToPseudoIndexMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_CloneMap(comp->getAllocator(CMK_ObjectAllocator))
    , m_nextLocalIndex(0)
    , m_firstPseudoIndex(BAD_VAR_NUM)
    , m_numPseudos(0)
    , m_maxPseudos(0)
    , m_regionsToClone(0)
    , m_trackFields(false)
    , m_StoreAddressToIndexMap(comp->getAllocator(CMK_ObjectAllocator))
{
    m_EscapingPointers                = BitVecOps::UninitVal();
    m_PossiblyStackPointingPointers   = BitVecOps::UninitVal();
    m_DefinitelyStackPointingPointers = BitVecOps::UninitVal();
    m_ConnGraphAdjacencyMatrix        = nullptr;
    m_StackAllocMaxSize               = (unsigned)JitConfig.JitObjectStackAllocationSize();
    m_trackFields                     = JitConfig.JitObjectStackAllocationTrackFields() > 0;
}

//------------------------------------------------------------------------
// IsTrackedType: see if this type is being tracked by escape analysis
//
// Arguments:
//    type - type of interest
//
// Returns:
//    true if so
//
bool ObjectAllocator::IsTrackedType(var_types type)
{
    const bool isTrackableScalar = (type == TYP_REF) || (type == TYP_BYREF);
    const bool isTrackableStruct = (type == TYP_STRUCT) && m_trackFields;

    return isTrackableScalar || isTrackableStruct;
}

//------------------------------------------------------------------------
// IsTrackedLocal: see if this local is being tracked by escape analysis
//
// Arguments:
//    lclNum - local of interest
//
// Returns:
//    true if so
//
bool ObjectAllocator::IsTrackedLocal(unsigned lclNum)
{
    assert(lclNum < comp->lvaCount);
    LclVarDsc* const varDsc = comp->lvaGetDesc(lclNum);
    return varDsc->lvTracked;
}

//------------------------------------------------------------------------
// LocalToIndex: get the bit vector index for a local
//
// Arguments:
//    lclNum -- local var num
//
// Returns:
//    bvIndex to use, or BAD_VAR_NUM if local is not tracked
//
unsigned ObjectAllocator::LocalToIndex(unsigned lclNum)
{
    assert(IsTrackedLocal(lclNum));
    LclVarDsc* const varDsc = comp->lvaGetDesc(lclNum);
    unsigned const   result = varDsc->lvVarIndex;
    assert(result < m_bvCount);
    return result;
}

//------------------------------------------------------------------------
// IndexToLocal: get the local num for a bv index
//
// Arguments:
//    bvIndex -- bit vector index
//
// Returns:
//    local num or BAD_VAR_NUM if index is not a tracked local var
//
unsigned ObjectAllocator::IndexToLocal(unsigned bvIndex)
{
    assert(bvIndex < m_bvCount);
    unsigned result = BAD_VAR_NUM;

    if (bvIndex < m_firstPseudoIndex)
    {
        result = comp->lvaTrackedToVarNum[bvIndex];
        assert(IsTrackedLocal(result));
    }
    return result;
}

#ifdef DEBUG
//------------------------------------------------------------------------------
// DumpIndex: write a description of a given bv index
//
// Arguments:
//    bvIndex - index to describe
//
// Notes:
//    includes leading space
//
void ObjectAllocator::DumpIndex(unsigned bvIndex)
{
    if (bvIndex < m_firstPseudoIndex)
    {
        printf(" V%02u", IndexToLocal(bvIndex));
        return;
    }

    if (bvIndex < m_unknownSourceIndex)
    {
        printf(" P%02u", bvIndex);
        return;
    }

    if (bvIndex == m_unknownSourceIndex)
    {
        printf(" U%02u", bvIndex);
        return;
    }

    printf(" ?%02u", bvIndex);
}
#endif

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
//
void ObjectAllocator::MarkLclVarAsEscaping(unsigned int lclNum)
{
    const unsigned bvIndex = LocalToIndex(lclNum);
    MarkIndexAsEscaping(bvIndex);
}

//------------------------------------------------------------------------------
// MarkIndexAsEscaping : Mark resource as escaping.
//
//
// Arguments:
//    index - bv index for the resource
//
void ObjectAllocator::MarkIndexAsEscaping(unsigned int bvIndex)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_EscapingPointers, bvIndex);
}

//------------------------------------------------------------------------------
// MarkLclVarAsPossiblyStackPointing : Mark local variable as possibly pointing
//                                     to a stack-allocated object.
//
//
// Arguments:
//    lclNum  - Possibly stack-object-pointing local variable number
//
void ObjectAllocator::MarkLclVarAsPossiblyStackPointing(unsigned int lclNum)
{
    const unsigned bvIndex = LocalToIndex(lclNum);
    MarkIndexAsPossiblyStackPointing(bvIndex);
}

//------------------------------------------------------------------------------
// MarkIndexAsPossiblyStackPointing : Mark resource as possibly pointing
//                                     to a stack-allocated object.
//
//
// Arguments:
//    index - bv index for the resource
//
void ObjectAllocator::MarkIndexAsPossiblyStackPointing(unsigned int bvIndex)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_PossiblyStackPointingPointers, bvIndex);
}

//------------------------------------------------------------------------------
// MarkLclVarAsDefinitelyStackPointing : Mark local variable as definitely pointing
//                                       to a stack-allocated object.
//
//
// Arguments:
//    lclNum  - Definitely stack-object-pointing local variable number
//
void ObjectAllocator::MarkLclVarAsDefinitelyStackPointing(unsigned int lclNum)
{
    const unsigned bvIndex = LocalToIndex(lclNum);
    MarkIndexAsDefinitelyStackPointing(bvIndex);
}

//------------------------------------------------------------------------------
// MarIndexAsDefinitelyStackPointing : Mark resource as definitely pointing
//                                       to a stack-allocated object.
//
//
// Arguments:
//    index - bv index for the resource
//
void ObjectAllocator::MarkIndexAsDefinitelyStackPointing(unsigned int bvIndex)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_DefinitelyStackPointingPointers, bvIndex);
}

//------------------------------------------------------------------------------
// AddConnGraphEdge : Record that the source local variable may point to the same set of objects
//                    as the set pointed to by target local variable.
//
// Arguments:
//    sourceLclNum  - Local variable number of the edge source
//    targetLclNum  - Local variable number of the edge target
//
void ObjectAllocator::AddConnGraphEdge(unsigned int sourceLclNum, unsigned int targetLclNum)
{
    const unsigned sourceBvIndex = LocalToIndex(sourceLclNum);
    const unsigned targetBvIndex = LocalToIndex(targetLclNum);
    AddConnGraphEdgeIndex(sourceBvIndex, targetBvIndex);
}

//------------------------------------------------------------------------------
// AddConnGraphEdgeIndex : Record that the source resource may point to the same set of objects
//                    as the set pointed to by target resource
//
// Arguments:
//    sourceBvIndex  - index of the edge source
//    targetBvIndex  - index of the edge target
//
void ObjectAllocator::AddConnGraphEdgeIndex(unsigned int sourceBvIndex, unsigned int targetBvIndex)
{
    BitVecOps::AddElemD(&m_bitVecTraits, m_ConnGraphAdjacencyMatrix[sourceBvIndex], targetBvIndex);
}

//------------------------------------------------------------------------
// PrepareAnalysis: determine how to model the escape analysis problem
//    with bit vectors.
//
void ObjectAllocator::PrepareAnalysis()
{
    // Determine how locals map to indicies in the bit vectors / connection graph.
    //
    // In "lcl num" space
    //
    // We reserve the range [0...L-1] for the initial set of locals.
    // Here L is the initial lvaCount.
    //
    // If conditional escape analysis is enabled, we reserve the range [L...L+M-1]
    // for locals allocated during the conditional escape analysis expansions,
    // where M is the maximum number of pseudos.
    //
    // In "bv" space
    //
    // We reserve the range [0...N-1] for the initial set of tracked locals.
    // Here N <= L is the number of tracked locals, determined below, an each
    // tracked local has an index assigned in this range.
    //
    // If conditional escape analysis is enabled, we reserve the range [N...N+M-1]
    // for locals allocated during the conditional escape analysis expansions,
    // where M is the maximum number of pseudos.
    //
    // We reserve the range [N+M ... N+2M-1] for pseudos.
    //
    // We reserve the singleton [N+2M] for the "unknown source" local
    //
    // LocalToIndex translates from "lcl num" space to "bv" space
    // IndexToLocal translates from "bv" space space to "lcl num" space
    //
    const unsigned localCount = comp->lvaCount;
    unsigned       bvNext     = 0;

    // Enumerate which locals are going to appear in our connection
    // graph, and assign them BV indicies.
    //
    for (unsigned lclNum = 0; lclNum < localCount; lclNum++)
    {
        LclVarDsc* const varDsc = comp->lvaGetDesc(lclNum);

        if (IsTrackedType(varDsc->TypeGet()))
        {
            varDsc->lvTracked  = 1;
            varDsc->lvVarIndex = (unsigned short)bvNext;
            bvNext++;
        }
        else
        {
            varDsc->lvTracked  = 0;
            varDsc->lvVarIndex = 0;
        }
    }

    m_nextLocalIndex = bvNext;

    // If we are going to do any conditional escape analysis, determine
    // how much extra BV space we'll need.
    //
    bool const hasEnumeratorLocals = comp->hasImpEnumeratorGdvLocalMap();

    if (hasEnumeratorLocals)
    {
        unsigned const enumeratorLocalCount = comp->getImpEnumeratorGdvLocalMap()->GetCount();
        assert(enumeratorLocalCount > 0);

        // For now, disable conditional escape analysis with OSR
        // since the dominance picture is muddled at this point.
        //
        // The conditionally escaping allocation sites will likely be in loops anyways.
        //
        bool const enableConditionalEscape = JitConfig.JitObjectStackAllocationConditionalEscape() > 0;
        bool const isOSR                   = comp->opts.IsOSR();

        if (enableConditionalEscape && !isOSR)
        {

#ifdef DEBUG
            static ConfigMethodRange JitObjectStackAllocationConditionalEscapeRange;
            JitObjectStackAllocationConditionalEscapeRange.EnsureInit(
                JitConfig.JitObjectStackAllocationConditionalEscapeRange());
            const unsigned hash    = comp->info.compMethodHash();
            const bool     inRange = JitObjectStackAllocationConditionalEscapeRange.Contains(hash);
#else
            const bool inRange = true;
#endif

            if (inRange)
            {
                JITDUMP("Enabling conditional escape analysis [%u pseudos]\n", enumeratorLocalCount);
                m_maxPseudos = enumeratorLocalCount;
            }
            else
            {
                JITDUMP("Not enabling conditional escape analysis (disabled by range config)\n");
            }
        }
        else
        {
            JITDUMP("Not enabling conditional escape analysis [%u pseudos]: %s\n", enumeratorLocalCount,
                    enableConditionalEscape ? "OSR" : "disabled by config");
        }
    }

#ifdef DEBUG
    if (m_trackFields)
    {
        static ConfigMethodRange JitObjectStackAllocationTrackFieldsRange;
        JitObjectStackAllocationTrackFieldsRange.EnsureInit(JitConfig.JitObjectStackAllocationTrackFieldsRange());
        const unsigned hash    = comp->info.compMethodHash();
        const bool     inRange = JitObjectStackAllocationTrackFieldsRange.Contains(hash);

        if (!inRange)
        {
            JITDUMP("Disabling field wise escape analysis per range config\n");
            m_trackFields = false;
        }
    }
#endif

    // When we clone to prevent conditional escape, we'll also create a new local
    // var that we will track. So we need to leave room for these vars. There can
    // be as many of these as there are pseudos.
    //
    const unsigned maxTrackedLclNum = localCount + m_maxPseudos;
    m_firstPseudoIndex              = bvNext + m_maxPseudos; // N, per above
    bvNext += 2 * m_maxPseudos;

    // A bv index for an unknown source of values
    //
    m_unknownSourceIndex = bvNext;
    bvNext++;

    // Now set up the BV traits.
    //
    m_bvCount      = bvNext;
    m_bitVecTraits = BitVecTraits(m_bvCount, comp);

    // Create the reverse mapping from bvIndex to local var index
    // (leave room for locals we may allocate)
    //
    if (comp->lvaTrackedToVarNumSize < maxTrackedLclNum)
    {
        comp->lvaTrackedToVarNumSize = maxTrackedLclNum;
        comp->lvaTrackedToVarNum     = new (comp->getAllocator(CMK_LvaTable)) unsigned[comp->lvaTrackedToVarNumSize];
    }

    for (unsigned lclNum = 0; lclNum < localCount; lclNum++)
    {
        LclVarDsc* const varDsc = comp->lvaGetDesc(lclNum);

        if (varDsc->lvTracked)
        {
            comp->lvaTrackedToVarNum[varDsc->lvVarIndex] = lclNum;
        }
    }

    JITDUMP("%u locals, %u tracked by escape analysis\n", localCount, m_nextLocalIndex);
    JITDUMP("Local field tracking is %s\n", m_trackFields ? "enabled" : "disabled");

    if (m_nextLocalIndex > 0)
    {
        JITDUMP("\nLocal      var    range [%02u...%02u]\n", 0, localCount - 1);
        if (m_maxPseudos > 0)
        {
            JITDUMP("Enumerator var    range [%02u...%02u]\n", localCount, localCount + m_maxPseudos - 1);
        }

        JITDUMP("\nLocal      var bv range [%02u...%02u]\n", 0, m_nextLocalIndex - 1);
        if (m_maxPseudos > 0)
        {
            JITDUMP("Enumerator var bv range [%02u...%02u]\n", m_nextLocalIndex, m_nextLocalIndex + m_maxPseudos - 1);
            JITDUMP("Pseudo     var bv range [%02u...%02u]\n", m_nextLocalIndex + m_maxPseudos,
                    m_nextLocalIndex + 2 * m_maxPseudos - 1);
        }
        JITDUMP("Unknown    var bv range [%02u...%02u]\n", m_unknownSourceIndex, m_unknownSourceIndex);
    }
}

//------------------------------------------------------------------------
// DoAnalysis: Walk over basic blocks of the method and detect all local
//             variables that can be allocated on the stack.
//
void ObjectAllocator::DoAnalysis()
{
    assert(m_IsObjectStackAllocationEnabled);
    assert(!m_AnalysisDone);

    PrepareAnalysis();

    if (m_bvCount > 0)
    {
        m_EscapingPointers         = BitVecOps::MakeEmpty(&m_bitVecTraits);
        m_ConnGraphAdjacencyMatrix = new (comp->getAllocator(CMK_ObjectAllocator)) BitSetShortLongRep[m_bvCount];

        // If we are doing conditional escape analysis, we also need to compute dominance.
        //
        if (CanHavePseudos())
        {
            assert(comp->m_dfsTree != nullptr);
            assert(comp->m_domTree == nullptr);
            comp->m_domTree = FlowGraphDominatorTree::Build(comp->m_dfsTree);
        }

        for (unsigned int i = 0; i < m_bvCount; i++)
        {
            m_ConnGraphAdjacencyMatrix[i] = BitVecOps::MakeEmpty(&m_bitVecTraits);
        }

        MarkEscapingVarsAndBuildConnGraph();
        ComputeEscapingNodes(&m_bitVecTraits, m_EscapingPointers);
    }

#ifdef DEBUG
    // Print the connection graph
    //
    if (JitConfig.JitObjectStackAllocationDumpConnGraph() > 0)
    {
        JITDUMP("digraph ConnectionGraph {\n");
        for (unsigned int i = 0; i < m_bvCount; i++)
        {
            BitVecOps::Iter iterator(&m_bitVecTraits, m_ConnGraphAdjacencyMatrix[i]);
            unsigned int    lclIndex;
            while (iterator.NextElem(&lclIndex))
            {
                JITDUMPEXEC(DumpIndex(lclIndex));
                JITDUMP(" -> ");
                JITDUMPEXEC(DumpIndex(i));
                JITDUMP(";\n");
            }

            if (CanIndexEscape(i))
            {
                JITDUMPEXEC(DumpIndex(i));
                JITDUMP(" -> E;\n");
            }
        }
        JITDUMP("}\n");
    }
#endif

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
            DoPostOrder   = true,
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
            LclVarDsc* const lclDsc = m_compiler->lvaGetDesc(lclNum);

            // Are we tracking this local?
            //
            if (!m_allocator->IsTrackedLocal(lclNum))
            {
                return Compiler::fgWalkResult::WALK_CONTINUE;
            }

            const unsigned lclIndex = m_allocator->LocalToIndex(lclNum);

            // If this local already escapes, no need to look further.
            //
            if (m_allocator->CanIndexEscape(lclIndex))
            {
                return Compiler::fgWalkResult::WALK_CONTINUE;
            }

            if (tree->OperIsLocalStore())
            {
                m_allocator->CheckForGuardedAllocationOrCopy(m_block, m_stmt, use, lclNum);
            }
            else if (tree->OperIs(GT_LCL_VAR))
            {
                assert(tree == m_ancestors.Top());
                m_allocator->AnalyzeParentStack(&m_ancestors, lclIndex, m_block);
            }
            else if (tree->OperIs(GT_LCL_ADDR) && lclDsc->TypeIs(TYP_STRUCT))
            {
                assert(tree == m_ancestors.Top());
                m_allocator->AnalyzeParentStack(&m_ancestors, lclIndex, m_block);
            }
            else if (tree->OperIs(GT_LCL_FLD))
            {
                // We generally don't see these in early IR. Bail for now.
                //
                JITDUMP("V%02u local field at [%06u]\n", lclNum, m_compiler->dspTreeID(tree));
                m_allocator->MarkLclVarAsEscaping(lclNum);
            }
            else
            {
                assert((tree->OperIs(GT_LCL_ADDR) && !lclDsc->TypeIs(TYP_STRUCT)));
                JITDUMP("V%02u address taken at [%06u]\n", lclNum, m_compiler->dspTreeID(tree));
                m_allocator->MarkLclVarAsEscaping(lclNum);
            }

            if (!m_allocator->CanIndexEscape(lclIndex) && !tree->OperIsLocalStore())
            {
                // Note uses of variables of interest to conditional escape analysis.
                //
                m_allocator->RecordAppearance(lclNum, m_block, m_stmt, use);
            }

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }

        Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;

            if (tree->OperIsLocalStore())
            {
                GenTreeLclVarCommon* const lclTree = tree->AsLclVarCommon();
                unsigned const             lclNum  = lclTree->GetLclNum();
                if (m_allocator->IsTrackedLocal(lclNum) && !m_allocator->CanLclVarEscape(lclNum))
                {
                    // See if we connected it to a source.
                    //
                    StoreInfo* const info = m_allocator->m_StoreAddressToIndexMap.LookupPointer(tree);

                    if ((info == nullptr) || !info->m_connected)
                    {
                        // This store was not modelled in the connection graph.
                        //
                        // If the stored value was was not a stack-viable allocation or null,
                        // add an edge to unknown source. This will ensure this local does
                        // not get retyped as TYP_I_IMPL.
                        //
                        GenTree* const             data = lclTree->Data();
                        ObjectAllocationType const oat  = m_allocator->AllocationKind(data);
                        bool const                 valueIsUnknown =
                            (oat == OAT_NEWOBJ_HEAP) || ((oat == OAT_NONE) && !data->IsIntegralConst(0));

                        if (valueIsUnknown)
                        {
                            // Add a connection to the unknown source.
                            //
                            JITDUMP("V%02u value unknown at [%06u]\n", lclNum, m_compiler->dspTreeID(tree));
                            m_allocator->AddConnGraphEdgeIndex(m_allocator->LocalToIndex(lclNum),
                                                               m_allocator->m_unknownSourceIndex);
                        }
                    }
                    else
                    {
                        JITDUMP(" ... Already connected at [%06u]\n", m_compiler->dspTreeID(tree));
                    }
                }
                else
                {
                    JITDUMP(" ... Not a GC store at [%06u]\n", m_compiler->dspTreeID(tree));
                }
            }
            else if (tree->OperIs(GT_STOREIND, GT_STORE_BLK))
            {
                // Is this a GC store?
                //
                bool isGCStore = true;

                if (!m_allocator->IsTrackedType(tree->TypeGet()))
                {
                    isGCStore = false;
                }
                else if (tree->OperIs(GT_STORE_BLK))
                {
                    isGCStore = tree->AsBlk()->GetLayout()->HasGCPtr();
                }

                // If so, did we model it yet?
                //
                if (isGCStore)
                {
                    // See if we have an index for the destination, and if we connected it to a source.
                    //
                    StoreInfo* const info = m_allocator->m_StoreAddressToIndexMap.LookupPointer(tree);

                    // Note here, unlike the local case above, we do not implicitly know the destination
                    // of the store. So if we have no info, we assume the store is to some place we don't track.
                    //
                    if ((info != nullptr) && !info->m_connected)
                    {
                        assert(info->m_index != BAD_VAR_NUM);
                        const unsigned dstIndex = info->m_index;

                        JITDUMP(" ... Unmodelled GC store to");
                        JITDUMPEXEC(m_allocator->DumpIndex(dstIndex));
                        JITDUMP(" at [%06u]\n", m_compiler->dspTreeID(tree));

                        // Look for stores of nullptrs; these do not need to create a connection.
                        //
                        GenTree* const data           = tree->AsIndir()->Data();
                        bool const     valueIsUnknown = !data->IsIntegralConst(0);

                        if (valueIsUnknown)
                        {
                            m_allocator->AddConnGraphEdgeIndex(dstIndex, m_allocator->m_unknownSourceIndex);
                            JITDUMPEXEC(m_allocator->DumpIndex(dstIndex))
                            JITDUMP(" ... value unknown at [%06u]\n", m_compiler->dspTreeID(tree));
                        }
                        else
                        {
                            JITDUMP(" ... Store of nullptr(s) at [%06u]\n", m_compiler->dspTreeID(tree));
                        }

                        info->m_connected = true;
                    }
                    else if (info == nullptr)
                    {
                        JITDUMP(" ... No store info for [%06u]\n", m_compiler->dspTreeID(tree));
                    }
                    else
                    {
                        JITDUMP(" ... Already connected at [%06u]\n", m_compiler->dspTreeID(tree));
                    }
                }
                else
                {
                    JITDUMP(" ... Not a GC store at [%06u]\n", m_compiler->dspTreeID(tree));
                }
            }
            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    for (unsigned int lclNum = 0; lclNum < comp->lvaCount; ++lclNum)
    {
        if (!IsTrackedLocal(lclNum))
        {
            continue;
        }

        LclVarDsc* const lclDsc  = comp->lvaGetDesc(lclNum);
        const unsigned   bvIndex = LocalToIndex(lclNum);

        if (lclDsc->IsAddressExposed())
        {
            JITDUMP("   V%02u is address exposed\n", lclNum);
            MarkIndexAsEscaping(bvIndex);
            continue;
        }

        if (lclNum == comp->info.compRetBuffArg)
        {
            JITDUMP("   V%02u is retbuff\n", lclNum);
            MarkIndexAsEscaping(bvIndex);
            continue;
        }

#if FEATURE_IMPLICIT_BYREFS
        // We have to mark all implicit byref params as escaping, because
        // their GC reporting is controlled by the caller
        //
        if (lclDsc->lvIsParam && lclDsc->lvIsImplicitByRef)
        {
            JITDUMP("   V%02u is an implicit byref param\n", lclNum);
            MarkIndexAsEscaping(bvIndex);
            continue;
        }
#endif

        // Parameters have unknown initial values.
        // OSR locals have unknown initial values.
        //
        if (lclDsc->lvIsParam || lclDsc->lvIsOSRLocal)
        {
            AddConnGraphEdgeIndex(bvIndex, m_unknownSourceIndex);
        }
    }

    MarkIndexAsEscaping(m_unknownSourceIndex);

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
        unsigned int       lclIndex;

        while (doOneMoreIteration)
        {
            BitVecOps::Iter iterator(bitVecTraits, escapingNodesToProcess);
            doOneMoreIteration = false;

            while (iterator.NextElem(&lclIndex))
            {
                if (m_ConnGraphAdjacencyMatrix[lclIndex] != nullptr)
                {
                    doOneMoreIteration = true;

                    // newEscapingNodes         = adjacentNodes[lclNum]
                    BitVecOps::Assign(bitVecTraits, newEscapingNodes, m_ConnGraphAdjacencyMatrix[lclIndex]);
                    // newEscapingNodes         = newEscapingNodes \ escapingNodes
                    BitVecOps::DiffD(bitVecTraits, newEscapingNodes, escapingNodes);
                    // escapingNodesToProcess   = escapingNodesToProcess U newEscapingNodes
                    BitVecOps::UnionD(bitVecTraits, escapingNodesToProcess, newEscapingNodes);
                    // escapingNodes = escapingNodes U newEscapingNodes
                    BitVecOps::UnionD(bitVecTraits, escapingNodes, newEscapingNodes);
                    // escapingNodesToProcess   = escapingNodesToProcess \ { lclIndex }
                    BitVecOps::RemoveElemD(bitVecTraits, escapingNodesToProcess, lclIndex);

#ifdef DEBUG
                    // Print the first witness to new escapes.
                    //
                    if (!BitVecOps::IsEmpty(bitVecTraits, newEscapingNodes))
                    {
                        BitVecOps::Iter iterator(bitVecTraits, newEscapingNodes);
                        unsigned int    newLclIndex;
                        while (iterator.NextElem(&newLclIndex))
                        {
                            JITDUMPEXEC(DumpIndex(lclIndex));
                            JITDUMP(" causes ");
                            JITDUMPEXEC(DumpIndex(newLclIndex));
                            JITDUMP(" to escape\n");
                        }
                    }
#endif
                }
            }
        }
    };

    computeClosure();

    if (m_numPseudos > 0)
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
    // Keep track of locals that we know may point at the heap
    //
    BitVec possiblyHeapPointingPointers = BitVecOps::MakeEmpty(&m_bitVecTraits);
    BitVecOps::AddElemD(bitVecTraits, possiblyHeapPointingPointers, m_unknownSourceIndex);

    bool     changed = true;
    unsigned pass    = 0;
    while (changed)
    {
        JITDUMP("\n---- computing stack pointing locals, pass %u\n", pass++);
        changed = false;
        for (unsigned int index = 0; index < m_bvCount; index++)
        {
            if (!MayIndexPointToStack(index) &&
                !BitVecOps::IsEmptyIntersection(bitVecTraits, m_PossiblyStackPointingPointers,
                                                m_ConnGraphAdjacencyMatrix[index]))
            {
                // We discovered a new pointer that may point to the stack.
                JITDUMPEXEC(DumpIndex(index));
                JITDUMP(" may point to the stack\n");
                MarkIndexAsPossiblyStackPointing(index);
                changed = true;
            }

            if (!BitVecOps::IsMember(bitVecTraits, possiblyHeapPointingPointers, index) &&
                !BitVecOps::IsEmptyIntersection(bitVecTraits, possiblyHeapPointingPointers,
                                                m_ConnGraphAdjacencyMatrix[index]))
            {
                // We discovered a new pointer that may point to the heap.
                JITDUMPEXEC(DumpIndex(index));
                JITDUMP(" may point to the heap\n");
                BitVecOps::AddElemD(bitVecTraits, possiblyHeapPointingPointers, index);
                changed = true;
            }
        }
    }
    JITDUMP("\n---- done computing stack pointing locals\n");

    // If a local is possibly stack pointing and not possibly heap pointing, then it is definitely stack pointing.
    //
    BitVec newDefinitelyStackPointingPointers = BitVecOps::UninitVal();
    BitVecOps::Assign(bitVecTraits, newDefinitelyStackPointingPointers, m_PossiblyStackPointingPointers);
    BitVecOps::DiffD(bitVecTraits, newDefinitelyStackPointingPointers, possiblyHeapPointingPointers);

    // We should have only added to the set of things that are definitely stack pointing.
    //
    assert(BitVecOps::IsSubset(bitVecTraits, m_DefinitelyStackPointingPointers, newDefinitelyStackPointingPointers));
    BitVecOps::AssignNoCopy(bitVecTraits, m_DefinitelyStackPointingPointers, newDefinitelyStackPointingPointers);

#ifdef DEBUG
    if (comp->verbose)
    {
        printf("Definitely stack-pointing locals:");
        {
            BitVecOps::Iter iter(bitVecTraits, m_DefinitelyStackPointingPointers);
            unsigned        index = 0;
            while (iter.NextElem(&index))
            {
                DumpIndex(index);
            }
            printf("\n");
        }

        printf("Possibly stack-pointing locals:");
        {
            BitVecOps::Iter iter(bitVecTraits, m_PossiblyStackPointingPointers);
            unsigned        index = 0;
            while (iter.NextElem(&index))
            {
                if (!BitVecOps::IsMember(bitVecTraits, m_DefinitelyStackPointingPointers, index))
                {
                    DumpIndex(index);
                }
            }
            printf("\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// CanAllocateLclVarOnStack: Returns true iff local variable can be
//                           allocated on the stack.
//
// Arguments:
//    lclNum   - Local variable number
//    clsHnd   - Class/struct handle of the variable class
//    allocType - Type of allocation (newobj or newarr)
//    length    - Length of the array (for newarr)
//    blockSize - [out, optional] exact size of the object
//    reason   - [out, required] if result is false, reason why
//    preliminaryCheck - if true, allow checking before analysis is done
//                 (for things that inherently disqualify the local)
//
// Return Value:
//    Returns true iff local variable can be allocated on the stack.
//
bool ObjectAllocator::CanAllocateLclVarOnStack(unsigned int         lclNum,
                                               CORINFO_CLASS_HANDLE clsHnd,
                                               ObjectAllocationType allocType,
                                               ssize_t              length,
                                               unsigned int*        blockSize,
                                               const char**         reason,
                                               bool                 preliminaryCheck)
{
    assert(preliminaryCheck || m_AnalysisDone);

    bool enableBoxedValueClasses = true;
    bool enableRefClasses        = true;
    bool enableArrays            = true;
    *reason                      = "[ok]";

#ifdef DEBUG
    enableBoxedValueClasses = (JitConfig.JitObjectStackAllocationBoxedValueClass() != 0);
    enableRefClasses        = (JitConfig.JitObjectStackAllocationRefClass() != 0);
    enableArrays            = (JitConfig.JitObjectStackAllocationArray() != 0);
#endif

    unsigned classSize = 0;

    if (allocType == OAT_NEWOBJ_HEAP)
    {
        *reason = "[runtime disallows]";
        return false;
    }
    if (allocType == OAT_NEWARR)
    {
        if (!enableArrays)
        {
            *reason = "[disabled by config]";
            return false;
        }

        if ((length < 0) || (length > CORINFO_Array_MaxLength))
        {
            *reason = "[invalid array length]";
            return false;
        }

        ClassLayout* const layout = comp->typGetArrayLayout(clsHnd, (unsigned)length);
        classSize                 = layout->GetSize();
    }
    else if (allocType == OAT_NEWOBJ)
    {
        if (comp->info.compCompHnd->isValueClass(clsHnd))
        {
            if (!enableBoxedValueClasses)
            {
                *reason = "[disabled by config]";
                return false;
            }

            classSize = comp->info.compCompHnd->getClassSize(clsHnd);
        }
        else
        {
            if (!enableRefClasses)
            {
                *reason = "[disabled by config]";
                return false;
            }

            assert(comp->info.compCompHnd->canAllocateOnStack(clsHnd));
            classSize = comp->info.compCompHnd->getHeapClassSize(clsHnd);
        }
    }
    else
    {
        assert(!"Unexpected allocation type");
        return false;
    }

    if (classSize > m_StackAllocMaxSize)
    {
        *reason = "[too large]";
        return false;
    }

    if (preliminaryCheck)
    {
        return true;
    }

    const bool escapes = CanLclVarEscape(lclNum);

    if (escapes)
    {
        *reason = "[escapes]";
        return false;
    }

    if (blockSize != nullptr)
    {
        *blockSize = classSize;
    }

    return true;
}

//------------------------------------------------------------------------
// AllocationKind: return kind of stack-allocatable object made by this tree (if any)
//
// Arguments:
//   tree -- tree in question
//
// Returns:
//   value indicating type of allocation
//
ObjectAllocator::ObjectAllocationType ObjectAllocator::AllocationKind(GenTree* tree)
{
    ObjectAllocationType allocType = OAT_NONE;
    if (tree->OperIs(GT_ALLOCOBJ))
    {
        GenTreeAllocObj* const allocObj = tree->AsAllocObj();
        CORINFO_CLASS_HANDLE   clsHnd   = allocObj->gtAllocObjClsHnd;
        assert(clsHnd != NO_CLASS_HANDLE);
        const bool isValueClass = comp->info.compCompHnd->isValueClass(clsHnd);
        bool const canBeOnStack = isValueClass || comp->info.compCompHnd->canAllocateOnStack(clsHnd);
        allocType               = canBeOnStack ? OAT_NEWOBJ : OAT_NEWOBJ_HEAP;
    }
    else if (!m_isR2R && tree->IsHelperCall())
    {
        GenTreeCall* const call = tree->AsCall();
        switch (call->GetHelperNum())
        {
            case CORINFO_HELP_NEWARR_1_VC:
            case CORINFO_HELP_NEWARR_1_PTR:
            case CORINFO_HELP_NEWARR_1_DIRECT:
            case CORINFO_HELP_NEWARR_1_ALIGN8:
            {
                if ((call->gtArgs.CountUserArgs() == 2) && call->gtArgs.GetUserArgByIndex(1)->GetNode()->IsCnsIntOrI())
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

    return allocType;
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
    m_stackAllocationCount            = 0;
    m_PossiblyStackPointingPointers   = BitVecOps::MakeEmpty(&m_bitVecTraits);
    m_DefinitelyStackPointingPointers = BitVecOps::MakeEmpty(&m_bitVecTraits);

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
            GenTree* const stmtExpr = stmt->GetRootNode();

            if (!stmtExpr->OperIs(GT_STORE_LCL_VAR) || !stmtExpr->TypeIs(TYP_REF))
            {
                // We assume that GT_ALLOCOBJ nodes are always present in the canonical form.
                assert(!comp->gtTreeContainsOper(stmtExpr, GT_ALLOCOBJ));
                continue;
            }

            const unsigned int         lclNum    = stmtExpr->AsLclVar()->GetLclNum();
            GenTree* const             data      = stmtExpr->AsLclVar()->Data();
            ObjectAllocationType const allocType = AllocationKind(data);

            if (allocType == OAT_NONE)
            {
                continue;
            }

            AllocationCandidate c(block, stmt, stmtExpr, lclNum, allocType);
            MorphAllocObjNode(c);
        }
    }

    return (m_stackAllocationCount > 0);
}

//------------------------------------------------------------------------
// MorphAllocObjNode: Transform an allocation site, possibly into as stack allocation
//
// Arguments:
//    candidate -- allocation candidate
//
// Return Value:
//    True if candidate was stack allocated
//    If false, candidate reason is updated to explain why not
//
void ObjectAllocator::MorphAllocObjNode(AllocationCandidate& candidate)
{
    const bool     didStackAllocate = MorphAllocObjNodeHelper(candidate);
    const unsigned lclNum           = candidate.m_lclNum;

    if (didStackAllocate)
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
        unsigned pseudoIndex = BAD_VAR_NUM;
        if (m_EnumeratorLocalToPseudoIndexMap.TryGetValue(lclNum, &pseudoIndex))
        {
            CloneInfo* info = nullptr;
            if (m_CloneMap.Lookup(pseudoIndex, &info))
            {
                if (info->m_willClone)
                {
                    JITDUMP("Connecting stack allocated enumerator V%02u to its address var V%02u\n", lclNum,
                            info->m_enumeratorLocal);
                    AddConnGraphEdge(lclNum, info->m_enumeratorLocal);
                    MarkLclVarAsPossiblyStackPointing(info->m_enumeratorLocal);
                    MarkLclVarAsDefinitelyStackPointing(info->m_enumeratorLocal);
                }
            }
        }

        if (candidate.m_bashCall)
        {
            candidate.m_statement->GetRootNode()->gtBashToNOP();
        }

        comp->optMethodFlags |= OMF_HAS_OBJSTACKALLOC;
        m_stackAllocationCount++;
    }
    else
    {
        assert(candidate.m_onHeapReason != nullptr);
        JITDUMP("Allocating V%02u on the heap: %s\n", lclNum, candidate.m_onHeapReason);
        if ((candidate.m_allocType == OAT_NEWOBJ) || (candidate.m_allocType == OAT_NEWOBJ_HEAP))
        {
            GenTree* const stmtExpr      = candidate.m_tree;
            GenTree* const oldData       = stmtExpr->AsLclVar()->Data();
            GenTree* const newData       = MorphAllocObjNodeIntoHelperCall(oldData->AsAllocObj());
            stmtExpr->AsLclVar()->Data() = newData;
            stmtExpr->AddAllEffectsFlags(newData);
        }

        if (IsTrackedLocal(lclNum))
        {
            AddConnGraphEdgeIndex(LocalToIndex(lclNum), m_unknownSourceIndex);
        }
    }
}

//------------------------------------------------------------------------
// MorphAllocObjNodeHelper: See if we can stack allocate a GT_ALLOCOBJ or GT_NEWARR
//
// Arguments:
//    candidate -- allocation candidate
//
// Return Value:
//    True if candidate was stack allocated
//    If false, candidate reason is updated to explain why not
//
bool ObjectAllocator::MorphAllocObjNodeHelper(AllocationCandidate& candidate)
{
    if (!IsObjectStackAllocationEnabled())
    {
        candidate.m_onHeapReason = "[object stack allocation disabled]";
        return false;
    }

    // Don't attempt to do stack allocations inside basic blocks that may be in a loop.
    //
    if (candidate.m_block->HasFlag(BBF_BACKWARD_JUMP))
    {
        candidate.m_onHeapReason = "[alloc in loop]";
        return false;
    }

    switch (candidate.m_allocType)
    {
        case OAT_NEWARR:
            return MorphAllocObjNodeHelperArr(candidate);
        case OAT_NEWOBJ:
            return MorphAllocObjNodeHelperObj(candidate);
        case OAT_NEWOBJ_HEAP:
            candidate.m_onHeapReason = "[runtime disallows]";
            return false;
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// MorphAllocObjNodeHelperObj: See if we can stack allocate a GT_NEWARR
//
// Arguments:
//    candidate -- allocation candidate
//
// Return Value:
//    True if candidate was stack allocated
//    If false, candidate reason is updated to explain why not
//
bool ObjectAllocator::MorphAllocObjNodeHelperArr(AllocationCandidate& candidate)
{
    assert(candidate.m_block->HasFlag(BBF_HAS_NEWARR));

    // R2R not yet supported
    //
    if (m_isR2R)
    {
        candidate.m_onHeapReason = "[R2R array not yet supported]";
        return false;
    }

    GenTree* const data = candidate.m_tree->AsLclVar()->Data();

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
    CORINFO_CLASS_HANDLE clsHnd    = comp->gtGetHelperCallClassHandle(data->AsCall(), &isExact, &isNonNull);
    GenTree* const       len       = data->AsCall()->gtArgs.GetUserArgByIndex(1)->GetNode();

    assert(len != nullptr);

    unsigned int blockSize = 0;
    comp->Metrics.NewArrayHelperCalls++;

    if (!isExact || !isNonNull)
    {
        candidate.m_onHeapReason = "[array type is either non-exact or null]";
        return false;
    }

    if (!len->IsCnsIntOrI())
    {
        candidate.m_onHeapReason = "[non-constant array size]";
        return false;
    }

    if (!CanAllocateLclVarOnStack(candidate.m_lclNum, clsHnd, candidate.m_allocType, len->AsIntCon()->IconValue(),
                                  &blockSize, &candidate.m_onHeapReason))
    {
        // reason set by the call
        return false;
    }

    JITDUMP("Allocating V%02u on the stack\n", candidate.m_lclNum);
    const unsigned int stackLclNum =
        MorphNewArrNodeIntoStackAlloc(data->AsCall(), clsHnd, (unsigned int)len->AsIntCon()->IconValue(), blockSize,
                                      candidate.m_block, candidate.m_statement);

    // Keep track of this new local for later type updates.
    //
    m_HeapLocalToStackArrLocalMap.AddOrUpdate(candidate.m_lclNum, stackLclNum);
    comp->Metrics.StackAllocatedArrays++;

    return true;
}

//------------------------------------------------------------------------
// MorphAllocObjNodeHelperObj: See if we can stack allocate a GT_ALLOCOBJ
//
// Arguments:
//    candidate -- allocation candidate
//
// Return Value:
//    True if candidate was stack allocated
//    If false, candidate reason is updated to explain why not
//
bool ObjectAllocator::MorphAllocObjNodeHelperObj(AllocationCandidate& candidate)
{
    assert(candidate.m_block->HasFlag(BBF_HAS_NEWOBJ));

    //------------------------------------------------------------------------
    // We expect the following expression tree at this point
    //  STMTx (IL 0x... ???)
    //    * STORE_LCL_VAR   ref
    //    \--*  ALLOCOBJ  ref
    //       \--*  CNS_INT(h) long
    //------------------------------------------------------------------------

    unsigned const       lclNum       = candidate.m_lclNum;
    GenTree* const       data         = candidate.m_tree->AsLclVar()->Data();
    CORINFO_CLASS_HANDLE clsHnd       = data->AsAllocObj()->gtAllocObjClsHnd;
    const bool           isValueClass = comp->info.compCompHnd->isValueClass(clsHnd);

    if (isValueClass)
    {
        comp->Metrics.NewBoxedValueClassHelperCalls++;
    }
    else
    {
        comp->Metrics.NewRefClassHelperCalls++;
    }

    if (!CanAllocateLclVarOnStack(lclNum, clsHnd, candidate.m_allocType, 0, nullptr, &candidate.m_onHeapReason))
    {
        // reason set by the call
        return false;
    }

    JITDUMP("Allocating V%02u on the stack\n", lclNum);

    ClassLayout* layout = nullptr;

    if (isValueClass)
    {
        CORINFO_CLASS_HANDLE boxedClsHnd = comp->info.compCompHnd->getTypeForBox(clsHnd);
        assert(boxedClsHnd != NO_CLASS_HANDLE);
        ClassLayout* structLayout = comp->typGetObjLayout(boxedClsHnd);
        layout                    = GetBoxedLayout(structLayout);
        comp->Metrics.StackAllocatedBoxedValueClasses++;
    }
    else
    {
        layout = comp->typGetObjLayout(clsHnd);
        comp->Metrics.StackAllocatedRefClasses++;
    }

    const unsigned int stackLclNum =
        MorphAllocObjNodeIntoStackAlloc(data->AsAllocObj(), layout, candidate.m_block, candidate.m_statement);
    m_HeapLocalToStackObjLocalMap.AddOrUpdate(lclNum, stackLclNum);

    candidate.m_bashCall = true;

    return true;
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
        assert(comp->IsAot());
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
//    layout       - layout for the stack allocated objectd
//    block        - a basic block where allocObj is
//    stmt         - a statement where allocObj is
//
// Return Value:
//    local num for the new stack allocated local
//
// Notes:
//    This function can insert additional statements before stmt.
//
unsigned int ObjectAllocator::MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj,
                                                              ClassLayout*     layout,
                                                              BasicBlock*      block,
                                                              Statement*       stmt)
{
    assert(allocObj != nullptr);
    assert(m_AnalysisDone);

#ifdef DEBUG
    const char* lclName = comp->printfAlloc("stack allocated %.110s", layout->GetShortClassName());
#endif

    const bool         shortLifetime = false;
    const unsigned int lclNum        = comp->lvaGrabTemp(shortLifetime DEBUGARG(lclName));
    comp->lvaSetStruct(lclNum, layout, /* unsafeValueClsCheck */ false);

    LclVarDsc* const lclDsc        = comp->lvaGetDesc(lclNum);
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
// AnalyzeParentStack: Check if the local variable escapes via the given parent stack.
//                                Update the connection graph as necessary.
//
// Arguments:
//    parentStack     - Parent stack of the current visit
//    lclIndex        - Index for a tracked, unescaped local referenced at the top of the stack
//    block           - basic block holding the trees
//
void ObjectAllocator::AnalyzeParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclIndex, BasicBlock* block)
{
    assert(parentStack != nullptr);
    assert(!CanIndexEscape(lclIndex));

    int              parentIndex = 1;
    const unsigned   lclNum      = IndexToLocal(lclIndex);
    LclVarDsc* const lclDsc      = comp->lvaGetDesc(lclNum);

    bool       keepChecking                  = true;
    bool       canLclVarEscapeViaParentStack = true;
    bool       isCopy                        = true;
    bool const isEnumeratorLocal             = lclDsc->lvIsEnumerator;
    bool       isAddress                     = parentStack->Top()->OperIs(GT_LCL_ADDR);

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
            case GT_STORE_LCL_VAR:
            {
                // If the store value is a local address, anything assigned to that local escapes
                //
                if (isAddress)
                {
                    break;
                }

                const unsigned int dstLclNum = parent->AsLclVar()->GetLclNum();

                // If we're not tracking stores to the dest local, the value does not escape.
                //
                if (!IsTrackedLocal(dstLclNum))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                const unsigned dstIndex = LocalToIndex(dstLclNum);

                // Add an edge to the connection graph.
                //
                AddConnGraphEdgeIndex(dstIndex, lclIndex);
                canLclVarEscapeViaParentStack = false;

                // If the source of this store is an enumerator local,
                // then the dest also becomes an enumerator local.
                //
                if (isCopy)
                {
                    CheckForEnumeratorUse(lclNum, dstLclNum);
                }

                // Note that we modelled this store in the connection graph
                //
                m_StoreAddressToIndexMap.Set(parent, StoreInfo(dstIndex, /* connected */ true));
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
            case GT_BOUNDS_CHECK:
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
            case GT_FIELD_ADDR:
                // Check whether the local escapes higher up
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_SUB:
                // Sub of two GC refs is no longer a GC ref.
                if (!parent->TypeIs(TYP_BYREF, TYP_REF))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                // Check whether the local escapes higher up
                isAddress = false;
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
            {
                // Is this a GC store?
                //
                if (!IsTrackedType(parent->TypeGet()))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                if (tree->OperIs(GT_STORE_BLK))
                {
                    ClassLayout* const layout = parent->AsBlk()->GetLayout();

                    if (!layout->HasGCPtr())
                    {
                        canLclVarEscapeViaParentStack = false;
                        break;
                    }
                }
                GenTree* const addr = parent->AsIndir()->Addr();
                if (tree == addr)
                {
                    if (isAddress)
                    {
                        // Remember the resource being stored to.
                        //
                        JITDUMP("... store address is local\n");
                        m_StoreAddressToIndexMap.Set(parent, StoreInfo(lclIndex));
                    }

                    // The address does not escape
                    //
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                // If we're walking the value tree, model the store.
                //
                StoreInfo* const dstInfo = m_StoreAddressToIndexMap.LookupPointer(parent);
                if (dstInfo != nullptr)
                {
                    assert(dstInfo->m_index != BAD_VAR_NUM);
                    assert(!dstInfo->m_connected);
                    JITDUMP("... local.field store\n");

                    // Note that we will model this store
                    //
                    dstInfo->m_connected = true;

                    JITDUMP(" ... Modelled GC store to");
                    JITDUMPEXEC(DumpIndex(dstInfo->m_index));
                    JITDUMP(" at [%06u]\n", comp->dspTreeID(parent));

                    if (isAddress)
                    {
                        AddConnGraphEdgeIndex(dstInfo->m_index, m_unknownSourceIndex);
                    }
                    else
                    {
                        AddConnGraphEdgeIndex(dstInfo->m_index, lclIndex);
                        canLclVarEscapeViaParentStack = false;
                        break;
                    }
                }

                // We're storing the value somewhere unknown. Assume the worst.
                break;
            }

            case GT_STORE_LCL_FLD:
            {
                // Does this store a type we're tracking?
                //
                if (!IsTrackedType(tree->TypeGet()))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                unsigned const dstLclNum = parent->AsLclVarCommon()->GetLclNum();

                if (IsTrackedLocal(dstLclNum))
                {
                    JITDUMP("... local V%02u.f store\n", dstLclNum);
                    const unsigned dstIndex = LocalToIndex(dstLclNum);
                    AddConnGraphEdgeIndex(dstIndex, lclIndex);
                    canLclVarEscapeViaParentStack = false;

                    // Note that we modelled this store in the connection graph
                    //
                    m_StoreAddressToIndexMap.Set(parent, StoreInfo(dstIndex, /* connected */ true));
                }

                // Else we're storing the value somewhere unknown.
                // Assume the worst.
                break;
            }

            case GT_IND:
            case GT_BLK:
            {
                // Does this load a type we're tracking?
                //
                if (!IsTrackedType(parent->TypeGet()))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                // For structs we need to check the layout as well
                //
                if (parent->OperIs(GT_BLK))
                {
                    ClassLayout* const layout = parent->AsBlk()->GetLayout();

                    if (!layout->HasGCPtr())
                    {
                        canLclVarEscapeViaParentStack = false;
                        break;
                    }
                }

                GenTree* const addr = parent->AsIndir()->Addr();

                // For loads from local structs we may be tracking the underlying fields.
                //
                // We can assume that the local being read is lclNum,
                // since we have walked up to this node from a leaf local.
                //
                // We only track through the first indir.
                //
                if (m_trackFields && isAddress)
                {
                    JITDUMP("... load local.field\n");
                    ++parentIndex;
                    isAddress    = false;
                    keepChecking = true;
                    break;
                }

                // Address doesn't refer to any location we track
                //
                canLclVarEscapeViaParentStack = false;
                break;
            }

            case GT_LCL_FLD:
            {
                // Does this load a type we're tracking?
                //
                if (!IsTrackedType(parent->TypeGet()))
                {
                    canLclVarEscapeViaParentStack = false;
                    break;
                }

                // For loads from local structs we may be tracking the underlying fields.
                //
                if (m_trackFields && (lclDsc->TypeGet() == TYP_STRUCT))
                {
                    JITDUMP("... load local.field\n");
                    ++parentIndex;
                    isAddress    = false;
                    keepChecking = true;
                    break;
                }

                // Load from some untracked local's fields.
                //
                canLclVarEscapeViaParentStack = false;
                break;
            }

            case GT_CALL:
            {
                GenTreeCall* const call = parent->AsCall();

                if (call->IsHelperCall())
                {
                    canLclVarEscapeViaParentStack =
                        !Compiler::s_helperCallProperties.IsNoEscape(comp->eeGetHelperNum(call->gtCallMethHnd));
                }
                else if (call->IsSpecialIntrinsic())
                {
                    // Some known special intrinsics don't escape. At this moment, only the ones accepting byrefs
                    // are supported. In order to support more intrinsics accepting objects, we need extra work
                    // on the VM side which is not ready for that yet.
                    //
                    switch (comp->lookupNamedIntrinsic(call->gtCallMethHnd))
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
                else if (call->IsDelegateInvoke())
                {
                    if (tree == call->gtArgs.GetThisArg()->GetNode())
                    {
                        JITDUMP("Delegate invoke this...\n");
                        canLclVarEscapeViaParentStack = false;
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
                // point of escape. We would have to add pseudos for this like we do for GDV, but we wouldn't
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

    if (canLclVarEscapeViaParentStack && !CanIndexEscape(lclIndex))
    {
        JITDUMPEXEC(DumpIndex(lclIndex));
        JITDUMP(" first escapes via [%06u]...[%06u]\n", comp->dspTreeID(parentStack->Top()),
                comp->dspTreeID(parentStack->Top(parentIndex)));
        MarkLclVarAsEscaping(lclNum);
    }
}

//------------------------------------------------------------------------
// UpdateAncestorTypes: Update types of some ancestor nodes of a possibly-stack-pointing
//                      tree from TYP_REF to TYP_BYREF or TYP_I_IMPL.
//
// Arguments:
//    tree            - Possibly-stack-pointing tree
//    parentStack     - Parent stack of the possibly-stack-pointing tree
//    newType         - New type of the possibly-stack-pointing tree
//    newLayout       - Layout for a retyped local struct
//    retypeFields    - Inspiring local is a retyped local struct; retype fields.
//
// Notes:
//                      If newType is TYP_I_IMPL, the tree is definitely pointing to the stack (or is null);
//                      if newType is TYP_BYREF, the tree may point to the stack.
//                      In addition to updating types this method may set GTF_IND_TGT_NOT_HEAP on ancestor
//                      indirections to help codegen with write barrier selection.
//
void ObjectAllocator::UpdateAncestorTypes(
    GenTree* tree, ArrayStack<GenTree*>* parentStack, var_types newType, ClassLayout* newLayout, bool retypeFields)
{
    assert(newType == TYP_BYREF || newType == TYP_I_IMPL);
    assert(parentStack != nullptr);
    int  parentIndex  = 1;
    bool keepChecking = true;
    bool sawIndir     = false;

    while (keepChecking && (parentStack->Height() > parentIndex))
    {
        GenTree* parent = parentStack->Top(parentIndex);
        keepChecking    = false;

        switch (parent->OperGet())
        {
            case GT_STORE_LCL_VAR:
            {
                if (parent->TypeGet() != newType)
                {
                    // If we have retyped the local, retype the store.
                    // Else keep TYP_BYREF.
                    //
                    GenTreeLclVarCommon* const lclParent = parent->AsLclVarCommon();
                    LclVarDsc* const           lclDsc    = comp->lvaGetDesc(lclParent);
                    if (parent->TypeIs(TYP_REF) || (lclDsc->TypeGet() == newType))
                    {
                        parent->ChangeType(newType);
                    }
                }
                break;
            }

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_GT:
            case GT_LE:
            case GT_GE:
            {
                // We may see sibling null refs. Retype them as appropriate.
                //
                GenTree* const lhs = parent->AsOp()->gtGetOp1();
                GenTree* const rhs = parent->AsOp()->gtGetOp2();

                if (lhs == tree)
                {
                    if (rhs->IsIntegralConst(0))
                    {
                        rhs->ChangeType(newType);
                    }
                }
                else if (rhs == tree)
                {
                    if (lhs->IsIntegralConst(0))
                    {
                        lhs->ChangeType(newType);
                    }
                }
                break;
            }

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
            case GT_FIELD_ADDR:
            case GT_BOX:
                if (parent->TypeGet() != newType)
                {
                    parent->ChangeType(newType);
                }
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_INDEX_ADDR:
                // We are not retyping array "fields" yet
                // so we can stop updating here.
                //
                if (parent->TypeGet() != newType)
                {
                    parent->ChangeType(newType);
                }
                break;

            case GT_SUB:
            {
                // Parent type can be TYP_I_IMPL, TYP_BYREF.
                // But not TYP_REF.
                //
                var_types parentType = parent->TypeGet();
                assert(parentType != TYP_REF);

                // New type can be TYP_I_IMPL, TYP_BYREF.
                // But TYP_BYREF only if parent is also
                //
                if (parentType != newType)
                {
                    // We must be retyping TYP_BYREF to TYP_I_IMPL.
                    //
                    assert(newType == TYP_I_IMPL);
                    assert(parentType == TYP_BYREF);
                    parent->ChangeType(newType);

                    // Propagate that upwards.
                    //
                    ++parentIndex;
                    keepChecking = true;
                }
                break;
            }

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
            {
                if (tree == parent->AsIndir()->Addr())
                {
                    // The new target could be *not* on the heap.
                    parent->gtFlags &= ~GTF_IND_TGT_HEAP;

                    if (newType != TYP_BYREF)
                    {
                        // This indicates that a write barrier is not needed when writing
                        // to this field/indirection since the address is not pointing to the heap.
                        // It's either null or points to inside a stack-allocated object.
                        parent->gtFlags |= GTF_IND_TGT_NOT_HEAP;
                    }
                }
                else
                {
                    assert(tree == parent->AsIndir()->Data());

                    // If we are storing to a GC struct field, we may need to retype the store
                    //
                    if (varTypeIsGC(parent->TypeGet()))
                    {
                        parent->ChangeType(newType);
                    }
                    else if (retypeFields && parent->OperIs(GT_STORE_BLK))
                    {
                        GenTreeBlk* const  block     = parent->AsBlk();
                        ClassLayout* const oldLayout = block->GetLayout();

                        if (oldLayout->HasGCPtr())
                        {
                            if (newLayout->GetSize() == oldLayout->GetSize())
                            {
                                block->SetLayout(newLayout);
                            }
                            else
                            {
                                // We must be storing just a portion of the original local
                                //
                                assert(newLayout->GetSize() > oldLayout->GetSize());

                                if (newLayout->HasGCPtr())
                                {
                                    block->SetLayout(GetByrefLayout(oldLayout));
                                }
                                else
                                {
                                    block->SetLayout(GetNonGCLayout(oldLayout));
                                }
                            }
                        }
                    }
                }
                break;
            }

            case GT_IND:
            case GT_BLK:
            {
                // If we are loading from a GC struct field, we may need to retype the load
                //
                if (retypeFields && !sawIndir)
                {
                    bool didRetype = false;

                    if (varTypeIsGC(parent->TypeGet()))
                    {
                        parent->ChangeType(newType);
                        didRetype = true;
                    }
                    else if (parent->OperIs(GT_BLK))
                    {
                        GenTreeBlk* const  block     = parent->AsBlk();
                        ClassLayout* const oldLayout = block->GetLayout();

                        if (oldLayout->HasGCPtr())
                        {
                            if (newLayout->GetSize() == oldLayout->GetSize())
                            {
                                block->SetLayout(newLayout);
                            }
                            else
                            {
                                // We must be loading just a portion of the original local
                                //
                                assert(newLayout->GetSize() > oldLayout->GetSize());

                                if (newLayout->HasGCPtr())
                                {
                                    block->SetLayout(GetByrefLayout(oldLayout));
                                }
                                else
                                {
                                    block->SetLayout(GetNonGCLayout(oldLayout));
                                }
                            }

                            didRetype = true;
                        }
                    }

                    if (didRetype)
                    {
                        ++parentIndex;
                        keepChecking = true;
                        sawIndir     = true;
                    }
                }

                break;
            }

            case GT_CALL:
                break;

            default:
                JITDUMP("UpdateAncestorTypes: unexpected op %s in [%06u]\n", GenTree::OpName(parent->OperGet()),
                        comp->dspTreeID(parent));
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
//
// Notes:
//   Also retypes GC typed locals that now may or must refer to stack objects
//
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

            const unsigned int lclNum       = tree->AsLclVarCommon()->GetLclNum();
            LclVarDsc*         lclVarDsc    = m_compiler->lvaGetDesc(lclNum);
            bool               retypeFields = false;

            // Revise IR for local that were retyped or are mapped to stack locals
            //
            if (!lclVarDsc->lvTracked)
            {
                return Compiler::fgWalkResult::WALK_CONTINUE;
            }

            unsigned int newLclNum = BAD_VAR_NUM;
            var_types    newType   = lclVarDsc->TypeGet();
            ClassLayout* newLayout = nullptr;

            if (m_allocator->m_HeapLocalToStackObjLocalMap.TryGetValue(lclNum, &newLclNum))
            {
                assert(tree->OperIs(GT_LCL_VAR)); // Must be a use.
                newType = TYP_I_IMPL;
                tree    = m_compiler->gtNewLclVarAddrNode(newLclNum);
                *use    = tree;

                JITDUMP("Update V%02u to V%02u in use [%06u]\n", lclNum, newLclNum, m_compiler->dspTreeID(tree));
                DISPTREE(tree);
            }
            else if (newType == TYP_STRUCT)
            {
                newLayout    = lclVarDsc->GetLayout();
                newType      = newLayout->HasGCPtr() ? TYP_BYREF : TYP_I_IMPL;
                retypeFields = true;
            }
            else
            {
                tree->ChangeType(newType);
            }

            m_allocator->UpdateAncestorTypes(tree, &m_ancestors, newType, newLayout, retypeFields);

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
            // Expand delegate invoke for calls where "this" is possibly stack pointing
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
                else if (call->IsDelegateInvoke())
                {
                    CallArg* const thisArg      = call->gtArgs.GetThisArg();
                    GenTree* const delegateThis = thisArg->GetNode();

                    if (delegateThis->OperIs(GT_LCL_VAR, GT_LCL_ADDR))
                    {
                        GenTreeLclVarCommon* const lcl = delegateThis->AsLclVarCommon();
                        bool const                 isStackAllocatedDelegate =
                            delegateThis->OperIs(GT_LCL_ADDR) || m_allocator->DoesLclVarPointToStack(lcl->GetLclNum());

                        if (isStackAllocatedDelegate)
                        {
                            JITDUMP("Expanding delegate invoke [%06u]\n", m_compiler->dspTreeID(call));

                            // Expand the delgate invoke early, so that physical promotion has
                            // a chance to promote the delegate fields.
                            //
                            // Note the instance field may also be stack allocatable (someday)
                            //
                            GenTree* const cloneThis      = m_compiler->gtClone(lcl, /* complexOk */ true);
                            unsigned const instanceOffset = m_compiler->eeGetEEInfo()->offsetOfDelegateInstance;
                            GenTree* const newThisAddr =
                                m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, cloneThis,
                                                          m_compiler->gtNewIconNode(instanceOffset, TYP_I_IMPL));

                            // For now assume the instance field is on the heap...
                            //
                            GenTree* const newThis = m_compiler->gtNewIndir(TYP_REF, newThisAddr);
                            thisArg->SetEarlyNode(newThis);

                            // the control target is
                            // [originalThis + firstTgtOffs]
                            //
                            unsigned const targetOffset = m_compiler->eeGetEEInfo()->offsetOfDelegateFirstTarget;
                            GenTree* const targetAddr =
                                m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, lcl,
                                                          m_compiler->gtNewIconNode(targetOffset, TYP_I_IMPL));
                            GenTree* const target = m_compiler->gtNewIndir(TYP_I_IMPL, targetAddr);

                            // Update call state -- now an indirect call to the delegate target
                            //
                            call->gtCallAddr = target;
                            call->gtCallType = CT_INDIRECT;
                            call->gtCallMoreFlags &= ~(GTF_CALL_M_DELEGATE_INV | GTF_CALL_M_WRAPPER_DELEGATE_INV);
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

    // Determine which locals should be retyped, and retype them.
    // Use lvTracked to remember which locals were retyped or will be replaced.
    //
    for (unsigned lclNum = 0; lclNum < comp->lvaCount; lclNum++)
    {
        LclVarDsc* const lclVarDsc = comp->lvaGetDesc(lclNum);

        if (!lclVarDsc->lvTracked)
        {
            JITDUMP("V%02u not tracked\n", lclNum);
            continue;
        }

        if (!MayLclVarPointToStack(lclNum))
        {
            JITDUMP("V%02u not possibly stack pointing\n", lclNum);
            lclVarDsc->lvTracked = 0;
            continue;
        }

        var_types newType = TYP_UNDEF;
        if (m_HeapLocalToStackObjLocalMap.Contains(lclNum))
        {
            // Appearances of lclNum will be replaced. We need to retype.
            //
            newType = TYP_I_IMPL;
        }
        else if (m_HeapLocalToStackArrLocalMap.Contains(lclNum))
        {
            // Appearances of lclNum will be NOT be replaced. We need to retype.
            //
            newType = TYP_I_IMPL;
        }
        else
        {
            newType = DoesLclVarPointToStack(lclNum) ? TYP_I_IMPL : TYP_BYREF;
        }

        // For local structs, retype the GC fields.
        //
        if (lclVarDsc->lvType == TYP_STRUCT)
        {
            assert(m_trackFields);

            ClassLayout* const layout    = lclVarDsc->GetLayout();
            ClassLayout*       newLayout = nullptr;

            if (!layout->HasGCPtr())
            {
                assert(newType == TYP_I_IMPL);
                JITDUMP("V%02u not GC\n", lclNum);
                lclVarDsc->lvTracked = 0;
                continue;
            }

            if (newType == TYP_I_IMPL)
            {
                // New layout with no gc refs + padding
                newLayout = GetNonGCLayout(layout);
                JITDUMP("Changing layout of struct V%02u to block\n", lclNum);
                lclVarDsc->ChangeLayout(newLayout);
            }
            else
            {
                // New layout with all gc refs as byrefs + padding
                // (todo, perhaps: see if old layout was already all byrefs)
                newLayout = GetByrefLayout(layout);
                JITDUMP("Changing layout of struct V%02u to byref\n", lclNum);
                lclVarDsc->ChangeLayout(newLayout);
            }
        }
        else
        {
            // For non-struct locals, retype the local
            //
            if (!varTypeIsGC(lclVarDsc->TypeGet()))
            {
                JITDUMP("V%02u not GC\n", lclNum);
                lclVarDsc->lvTracked = 0;
                continue;
            }

            if (lclVarDsc->lvType != newType)
            {
                // Params should only retype from ref->byref as they have unknown initial value
                //
                assert(!(lclVarDsc->lvIsParam && (newType == TYP_I_IMPL)));
                JITDUMP("Changing the type of V%02u from %s to %s\n", lclNum, varTypeName(lclVarDsc->lvType),
                        varTypeName(newType));
                lclVarDsc->lvType = newType;
            }
            else
            {
                JITDUMP("V%02u already properly typed\n", lclNum);
                lclVarDsc->lvTracked = 0;
            }
        }
    }

    // Update locals and types in the IR to match.
    //
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
//   and var references and connected them to a pseduo, along with information
//   about how we could clone blocks to ensure that the object could be stack allocated.
//
//   The current assumption is that these nodes do not escape, but to ensure
//   that we must be able to clone the code and remove the potential for escape
//
//   So, we  verify for each case that we can clone; if not, mark we the Pseudo
//   as escaping. If any pseudo now escapes, we return true so that the main
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

    for (unsigned p = 0; p < m_numPseudos; p++)
    {
        unsigned const pseudoIndex = p + m_firstPseudoIndex;
        bool           canClone    = true;
        CloneInfo*     info        = nullptr;

        const bool hasInfo = m_CloneMap.Lookup(pseudoIndex, &info);
        if (!hasInfo)
        {
            // We never found any conditional allocation attached to this pseudoIndex.
            //
            JITDUMPEXEC(DumpIndex(pseudoIndex));
            JITDUMP("  has no guard info\n");
            canClone = false;
            break;
        }

        // See what locals were "assigned" to the pseudo.
        //
        BitVec pseudoAdjacencies = m_ConnGraphAdjacencyMatrix[pseudoIndex];

        // If we found an allocation but didn't find any conditionally escaping uses, then cloning is of no use
        //
        if (BitVecOps::IsEmpty(bitVecTraits, pseudoAdjacencies))
        {
            JITDUMP("   No conditionally escaping uses under");
            JITDUMPEXEC(DumpIndex(pseudoIndex));
            JITDUMP(", so no reason to clone\n");
            canClone = false;
            break;
        }

        // Check if each conditionally escaping local escapes on its own; if so cloning is of no use
        //
        BitVecOps::Iter iterator(bitVecTraits, pseudoAdjacencies);
        unsigned        lclNumIndex = BAD_VAR_NUM;
        while (canClone && iterator.NextElem(&lclNumIndex))
        {
            if (BitVecOps::IsMember(bitVecTraits, escapingNodes, lclNumIndex))
            {
                // The enumerator var or a related var had escaping uses somewhere in the method,
                // not under a failing GDV or any GDV.
                //
                JITDUMPEXEC(DumpIndex(lclNumIndex));
                JITDUMP("   escapes independently of", IndexToLocal(lclNumIndex));
                JITDUMPEXEC(DumpIndex(pseudoIndex));
                JITDUMP("\n");
                canClone = false;
                break;
            }
        }

        // Also check the alloc temps
        //
        if (canClone && (info->m_allocTemps != nullptr))
        {
            for (unsigned v : *(info->m_allocTemps))
            {
                if (BitVecOps::IsMember(bitVecTraits, escapingNodes, LocalToIndex(v)))
                {
                    JITDUMP("   alloc temp");
                    JITDUMPEXEC(DumpIndex(v));
                    JITDUMP("   escapes independently of", IndexToLocal(lclNumIndex));
                    JITDUMPEXEC(DumpIndex(pseudoIndex));
                    JITDUMP("\n");

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
            JITDUMPEXEC(DumpIndex(pseudoIndex));
            JITDUMP("   is guarding the escape of V%02u\n", info->m_local);
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
            JITDUMP("\n*** Can prevent escape under");
            JITDUMPEXEC(DumpIndex(pseudoIndex));
            JITDUMP(" via cloning ***\n");

            info->m_willClone = true;
            m_regionsToClone++;
        }
        else
        {
            JITDUMP("   not optimizing, so will mark");
            JITDUMPEXEC(DumpIndex(pseudoIndex));
            JITDUMP(" as escaping\n");
            MarkIndexAsEscaping(pseudoIndex);
            BitVecOps::AddElemD(bitVecTraits, escapingNodesToProcess, pseudoIndex);
            newEscapes = true;
        }
    }

    return newEscapes;
}

//------------------------------------------------------------------------------
// NewPseudoIndex: return index of a new pseudo.
//
// Returns:
//   index to use, or BAD_VAR_NUM if no more indices are available.
//
unsigned ObjectAllocator::NewPseudoIndex()
{
    unsigned result = BAD_VAR_NUM;
    if (m_numPseudos >= m_maxPseudos)
    {
        assert(!"unexpected number of pseudos");
    }
    else
    {
        result = m_firstPseudoIndex + m_numPseudos;
        m_numPseudos++;
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
    // Find pseudo...
    //
    unsigned pseudoIndex = BAD_VAR_NUM;
    if (!m_EnumeratorLocalToPseudoIndexMap.TryGetValue(lclNum, &pseudoIndex))
    {
        JITDUMP("... no pseudo?\n");
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

    // Find the GDV guard for the pseudo
    //
    CloneInfo* pseudoGuardInfo;
    if (!m_CloneMap.Lookup(pseudoIndex, &pseudoGuardInfo))
    {
        JITDUMP("... under non-gdv guard?\n");
        return false;
    }

    // Verify this appearance is under the same guard
    //
    if ((info.m_local == lclNum) && (pseudoGuardInfo->m_local == lclNum) && (info.m_type == pseudoGuardInfo->m_type))
    {
        // If so, track this as an assignment pseudoIndex = ...
        //
        // Later if we don't clone and split off the failing GDV paths,
        // we will mark pseudoIndex as escaped, and that will lead
        // to lclNum escaping as well.
        //
        JITDUMP("... under GDV; tracking via pseudo index");
        JITDUMPEXEC(DumpIndex(pseudoIndex));
        JITDUMP("\n")
        AddConnGraphEdgeIndex(pseudoIndex, LocalToIndex(lclNum));
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

    if (!CanHavePseudos())
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
                ObjectAllocationType oat    = AllocationKind(data);
                if (CanAllocateLclVarOnStack(enumeratorLocal, clsHnd, oat, length, &size, &reason,
                                             /* preliminaryCheck */ true))
                {
                    // We are going to conditionally track accesses to the enumerator local via a pseudo.
                    //
                    const unsigned pseudoIndex = NewPseudoIndex();
                    assert(pseudoIndex != BAD_VAR_NUM);
                    bool added = m_EnumeratorLocalToPseudoIndexMap.AddOrUpdate(enumeratorLocal, pseudoIndex);

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
                    info->m_pseudoIndex   = pseudoIndex;
                    info->m_appearanceMap = new (alloc) EnumeratorVarMap(alloc);
                    info->m_allocBlock    = block;
                    info->m_allocStmt     = stmt;
                    info->m_allocTree     = data;
                    info->m_domBlock      = controllingGDV.m_block;
                    m_CloneMap.Set(pseudoIndex, info);

                    JITDUMP("Enumerator allocation [%06u]: will track accesses to V%02u guarded by type %s via",
                            comp->dspTreeID(data), enumeratorLocal, comp->eeGetClassName(clsHnd));
                    JITDUMPEXEC(DumpIndex(pseudoIndex));
                    JITDUMP("\n");

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
    unsigned pseudoIndex = BAD_VAR_NUM;

    if (m_EnumeratorLocalToPseudoIndexMap.TryGetValue(dstLclNum, &pseudoIndex))
    {
        // We already knew dstLclNum was a potential copy
        //
        return true;
    }

    if (!m_EnumeratorLocalToPseudoIndexMap.TryGetValue(lclNum, &pseudoIndex))
    {
        // lclNum is not a potential source
        //
        return false;
    }

    CloneInfo* info = nullptr;
    if (!m_CloneMap.Lookup(pseudoIndex, &info))
    {
        // We aren't interested in locals under this guard
        //
        return false;
    }

    // lclNum is an interesting enumerator var, so now so is dstLclNum.
    //
    const bool added = m_EnumeratorLocalToPseudoIndexMap.AddOrUpdate(dstLclNum, pseudoIndex);

    assert(added);

    JITDUMP("Enumerator allocation: will also track accesses to V%02u via", dstLclNum);
    JITDUMPEXEC(DumpIndex(pseudoIndex));
    JITDUMP("\n");

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
    unsigned Pseudo = BAD_VAR_NUM;
    if (!m_EnumeratorLocalToPseudoIndexMap.TryGetValue(lclNum, &Pseudo))
    {
        return;
    }

    CloneInfo* info;
    if (!m_CloneMap.Lookup(Pseudo, &info))
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

        JITDUMP("Cloned blocks for");
        JITDUMPEXEC(DumpIndex(info->m_pseudoIndex));
        JITDUMP(" overlap with those for");
        JITDUMPEXEC(DumpIndex(c->m_pseudoIndex));
        JITDUMP(" unable to clone\n");

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
            JITDUMP("Rejecting");
            JITDUMPEXEC(DumpIndex(info->m_pseudoIndex));
            JITDUMP(" cloning: exceeds size limit %u\n", sizeLimit);
            return false;
        }
        size += blockSize;
    }

    // TODO: some kind of profile check...
    //
    JITDUMP("Accepting");
    JITDUMPEXEC(DumpIndex(info->m_pseudoIndex));
    JITDUMP(" cloning: size %u does not exceed size limit %u\n", size, sizeLimit);
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
    LclVarDsc* const newEnumeratorDsc = comp->lvaGetDesc(newEnumeratorLocal);

    newEnumeratorDsc->lvType      = TYP_REF;
    newEnumeratorDsc->lvSingleDef = 1;
    comp->lvaSetClass(newEnumeratorLocal, info->m_type, /* isExact */ true);

    newEnumeratorDsc->lvTracked  = 1;
    newEnumeratorDsc->lvVarIndex = (unsigned short)m_nextLocalIndex; // grr
    assert(newEnumeratorDsc->lvVarIndex < comp->lvaTrackedToVarNumSize);
    comp->lvaTrackedToVarNum[newEnumeratorDsc->lvVarIndex]   = newEnumeratorLocal;
    m_ConnGraphAdjacencyMatrix[newEnumeratorDsc->lvVarIndex] = BitVecOps::MakeEmpty(&m_bitVecTraits);
    m_nextLocalIndex++;
    assert(m_maxPseudos > 0);
    assert(newEnumeratorDsc->lvVarIndex < m_firstPseudoIndex);

    JITDUMP("Tracking V%02u via 0x%02x\n", newEnumeratorLocal, newEnumeratorDsc->lvVarIndex);

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

//------------------------------------------------------------------------------
// GetBoxedLayout: get a layout for a boxed version of a struct
//
// Arguments:
//   layout - layout of the struct
//
// Notes:
//   For Nullable<T>, layout class should be T
//
ClassLayout* ObjectAllocator::GetBoxedLayout(ClassLayout* layout)
{
    assert(layout->IsValueClass());

    ClassLayoutBuilder b(comp, TARGET_POINTER_SIZE + layout->GetSize());
    b.CopyPaddingFrom(TARGET_POINTER_SIZE, layout);
    b.CopyGCInfoFrom(TARGET_POINTER_SIZE, layout);

#ifdef DEBUG
    b.CopyNameFrom(layout, "[boxed] ");
#endif

    return comp->typGetCustomLayout(b);
}

//------------------------------------------------------------------------------
// GetNonGCLayout: get a layout with the same size and padding as an existing
//   layout, but with no GC fields.
//
// Arguments:
//   layout - existing layout to use as template
//
ClassLayout* ObjectAllocator::GetNonGCLayout(ClassLayout* layout)
{
    assert(layout->HasGCPtr());
    ClassLayoutBuilder b(comp, layout->GetSize());
    b.CopyPaddingFrom(0, layout);

#ifdef DEBUG
    b.CopyNameFrom(layout, "[nongc] ");
#endif

    return comp->typGetCustomLayout(b);
}

//------------------------------------------------------------------------------
// GetByrefLayout: get a layout with the same size and padding as an existing
//   layout, but with all GC fields retyped to byref.
//
// Arguments:
//   layout - existing layout to use as template
//
ClassLayout* ObjectAllocator::GetByrefLayout(ClassLayout* layout)
{
    assert(layout->HasGCPtr());
    ClassLayoutBuilder b(comp, layout->GetSize());
    b.CopyPaddingFrom(0, layout);

    if (layout->GetGCPtrCount() > 0)
    {
        for (unsigned slot = 0; slot < layout->GetSlotCount(); slot++)
        {
            var_types gcType = layout->GetGCPtrType(slot);
            if (gcType == TYP_REF)
            {
                gcType = TYP_BYREF;
            }
            b.SetGCPtrType(slot, gcType);
        }
    }

#ifdef DEBUG
    b.CopyNameFrom(layout, "[byref] ");
#endif

    return comp->typGetCustomLayout(b);
}
