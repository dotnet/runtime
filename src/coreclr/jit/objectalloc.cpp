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
    if ((comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0 && (comp->optMethodFlags & OMF_HAS_NEWARRAY) == 0)
    {
        JITDUMP("no newobjs or newarr in this method; punting\n");
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
        ComputeStackObjectPointers(&m_bitVecTraits);
        RewriteUses();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }
    else
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
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
        m_EscapingPointers         = BitVecOps::MakeEmpty(&m_bitVecTraits);
        m_ConnGraphAdjacencyMatrix = new (comp->getAllocator(CMK_ObjectAllocator)) BitSetShortLongRep[comp->lvaCount];

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

    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
            ComputeStack  = true,
        };

        BuildConnGraphVisitor(ObjectAllocator* allocator)
            : GenTreeVisitor<BuildConnGraphVisitor>(allocator->comp)
            , m_allocator(allocator)
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
            }
            else if (tree->OperIs(GT_LCL_VAR) && tree->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL))
            {
                assert(tree == m_ancestors.Top());

                if (!m_allocator->CanLclVarEscapeViaParentStack(&m_ancestors, lclNum))
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

    for (BasicBlock* const block : comp->Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            BuildConnGraphVisitor buildConnGraphVisitor(this);
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
    BitSetShortLongRep newEscapingNodes       = BitVecOps::UninitVal();

    unsigned int lclNum;

    bool doOneMoreIteration = true;
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
            }
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
                        if (bitCount > 0)
                        {
                            BitVecOps::Iter iter(bitVecTraits, m_ConnGraphAdjacencyMatrix[lclNum]);
                            unsigned        rhsLclNum               = 0;
                            bool            definitelyStackPointing = true;

                            while (iter.NextElem(&rhsLclNum))
                            {
                                if (!DoesLclVarPointToStack(rhsLclNum))
                                {
                                    definitelyStackPointing = false;
                                    break;
                                }
                            }

                            if (definitelyStackPointing)
                            {
                                // All stores to lclNum local are the definitely-stack-pointing
                                // rhsLclNum locals so lclNum local is also definitely-stack-pointing.
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
                else if (data->IsHelperCall())
                {
                    switch (data->AsCall()->GetHelperNum())
                    {
                        case CORINFO_HELP_NEWARR_1_VC:
                        case CORINFO_HELP_NEWARR_1_OBJ:
                        case CORINFO_HELP_NEWARR_1_DIRECT:
                        case CORINFO_HELP_NEWARR_1_ALIGN8:
                        {
                            if (data->AsCall()->gtArgs.CountArgs() == 2 &&
                                data->AsCall()->gtArgs.GetArgByIndex(1)->GetNode()->IsCnsIntOrI())
                            {
                                allocType = OAT_NEWARR;
                            }
                            break;
                        }
#ifdef FEATURE_READYTORUN
                        case CORINFO_HELP_READYTORUN_NEWARR_1:
                        {
                            if (data->AsCall()->gtArgs.CountArgs() == 1 &&
                                data->AsCall()->gtArgs.GetArgByIndex(0)->GetNode()->IsCnsIntOrI())
                            {
                                allocType = OAT_NEWARR;
                            }
                            break;
                        }
#endif
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
                        GenTree* len = nullptr;
#ifdef FEATURE_READYTORUN
                        if (comp->opts.IsReadyToRun() && data->IsHelperCall(comp, CORINFO_HELP_READYTORUN_NEWARR_1))
                        {
                            len = data->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
                        }
                        else
#endif
                        {
                            len = data->AsCall()->gtArgs.GetArgByIndex(1)->GetNode();
                        }

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
                            m_HeapLocalToStackLocalMap.AddOrUpdate(lclNum, stackLclNum);
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
                    stmt->GetRootNode()->gtBashToNOP();
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
// MorphNewArrNodeIntoStackAlloc: Morph a GT_CALL CORINFO_HELP_NEWARR_1_VC
//                                node into stack allocation.
//
// Arguments:
//    newArr       - GT_CALL that will be replaced by helper call.
//    clsHndclsHnd - class representing the type of the array
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
#ifdef FEATURE_READYTORUN
    assert(newArr->GetHelperNum() == CORINFO_HELP_READYTORUN_NEWARR_1 || !comp->opts.IsReadyToRun());
#endif

    const bool         shortLifetime = false;
    const bool         alignTo8      = newArr->GetHelperNum() == CORINFO_HELP_NEWARR_1_ALIGN8;
    const unsigned int lclNum        = comp->lvaGrabTemp(shortLifetime DEBUGARG("stack allocated array temp"));

    if (alignTo8)
    {
        blockSize = AlignUp(blockSize, TARGET_POINTER_SIZE);
    }

    comp->lvaSetStruct(lclNum, comp->typGetBlkLayout(blockSize), /* unsafeValueClsCheck */ false);

    // Initialize the object memory if necessary.
    bool             bbInALoop  = block->HasFlag(BBF_BACKWARD_JUMP);
    bool             bbIsReturn = block->KindIs(BBJ_RETURN);
    LclVarDsc* const lclDsc     = comp->lvaGetDesc(lclNum);
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
    GenTree*   init     = comp->gtNewStoreLclFldNode(lclNum, TYP_I_IMPL, 0, newArr->gtArgs.GetArgByIndex(0)->GetNode());
    Statement* initStmt = comp->gtNewStmt(init);

    comp->fgInsertStmtBefore(block, stmt, initStmt);

    // Initialize the array length.
    //
    //------------------------------------------------------------------------
    // STMTx (IL 0x... ???)
    //   * STORE_LCL_FLD    int
    //   \--*  CNS_INT    int
    //------------------------------------------------------------------------

    // Pass the total length of the array.
    GenTree*   len     = comp->gtNewStoreLclFldNode(lclNum, TYP_INT, OFFSETOF__CORINFO_Array__length,
                                                    comp->gtNewIconNode(length, TYP_INT));
    Statement* lenStmt = comp->gtNewStmt(len);
    comp->fgInsertStmtBefore(block, stmt, lenStmt);

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

    return lclNum;
}

//------------------------------------------------------------------------
// CanLclVarEscapeViaParentStack: Check if the local variable escapes via the given parent stack.
//                                Update the connection graph as necessary.
//
// Arguments:
//    parentStack     - Parent stack of the current visit
//    lclNum          - Local variable number
//
// Return Value:
//    true if the local can escape via the parent stack; false otherwise
//
// Notes:
//    The method currently treats all locals assigned to a field as escaping.
//    The can potentially be tracked by special field edges in the connection graph.
//
bool ObjectAllocator::CanLclVarEscapeViaParentStack(ArrayStack<GenTree*>* parentStack, unsigned int lclNum)
{
    assert(parentStack != nullptr);
    int parentIndex = 1;

    bool keepChecking                  = true;
    bool canLclVarEscapeViaParentStack = true;

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

        JITDUMP("... L%02u ... checking [%06u]\n", lclNum, comp->dspTreeID(parent));

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
            case GT_BOX:
            case GT_FIELD_ADDR:
            case GT_INDEX_ADDR:
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
                GenTreeCall* asCall = parent->AsCall();

                if (asCall->IsHelperCall())
                {
                    canLclVarEscapeViaParentStack =
                        !Compiler::s_helperCallProperties.IsNoEscape(comp->eeGetHelperNum(asCall->gtCallMethHnd));
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
