// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
// Notes:
//    Runs only if Compiler::optMethodFlags has flag OMF_HAS_NEWOBJ set.

void ObjectAllocator::DoPhase()
{
    JITDUMP("\n*** ObjectAllocationPhase: ");
    if ((comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0)
    {
        JITDUMP("no newobjs in this method; punting\n");
        return;
    }

    if (IsObjectStackAllocationEnabled())
    {
        JITDUMP("enabled, analyzing...\n");
        DoAnalysis();
    }
    else
    {
        JITDUMP("disabled, punting\n");
    }

    const bool didStackAllocate = MorphAllocObjNodes();

    if (didStackAllocate)
    {
        RewriteUses();
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
// IsLclVarEscaping : Check if the local variable has been marked as escaping.
//
//
// Arguments:
//    lclNum  - Local variable number
//
// Return Value:
//    True if the local variable has been marked as escaping; false otherwise

bool ObjectAllocator::IsLclVarEscaping(unsigned int lclNum)
{
    return BitVecOps::IsMember(&m_bitVecTraits, m_EscapingPointers, lclNum);
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
            : GenTreeVisitor<BuildConnGraphVisitor>(allocator->comp), m_allocator(allocator)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            assert(tree != nullptr);
            assert(tree->IsLocal());

            var_types type = tree->TypeGet();
            if ((tree->OperGet() == GT_LCL_VAR) && (type == TYP_REF || type == TYP_BYREF || type == TYP_I_IMPL))
            {
                unsigned int lclNum = tree->AsLclVar()->GetLclNum();
                assert(tree == m_ancestors.Index(0));

                if (m_allocator->CanLclVarEscapeViaParentStack(&m_ancestors, lclNum))
                {
                    if (!m_allocator->IsLclVarEscaping(lclNum))
                    {
                        JITDUMP("V%02u first escapes via [%06u]\n", lclNum, m_compiler->dspTreeID(tree));
                    }
                    m_allocator->MarkLclVarAsEscaping(lclNum);
                }
            }
            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    for (unsigned int lclNum = 0; lclNum < comp->lvaCount; ++lclNum)
    {
        var_types type = comp->lvaTable[lclNum].TypeGet();

        if (type == TYP_REF || type == TYP_I_IMPL || type == TYP_BYREF)
        {
            m_ConnGraphAdjacencyMatrix[lclNum] = BitVecOps::MakeEmpty(&m_bitVecTraits);

            if (comp->lvaTable[lclNum].lvAddrExposed)
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

    BasicBlock* block;

    foreach_block(comp, block)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            BuildConnGraphVisitor buildConnGraphVisitor(this);
            buildConnGraphVisitor.WalkTree(&stmt->gtStmtExpr, nullptr);
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
    bool didStackAllocate = false;

    BasicBlock* block;

    foreach_block(comp, block)
    {
        const bool basicBlockHasNewObj       = (block->bbFlags & BBF_HAS_NEWOBJ) == BBF_HAS_NEWOBJ;
        const bool basicBlockHasBackwardJump = (block->bbFlags & BBF_BACKWARD_JUMP) == BBF_BACKWARD_JUMP;
#ifndef DEBUG
        if (!basicBlockHasNewObj)
        {
            continue;
        }
#endif // DEBUG

        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            GenTree* stmtExpr = stmt->gtStmtExpr;
            GenTree* op2      = nullptr;

            bool canonicalAllocObjFound = false;

            if (stmtExpr->OperGet() == GT_ASG && stmtExpr->TypeGet() == TYP_REF)
            {
                op2 = stmtExpr->gtGetOp2();

                if (op2->OperGet() == GT_ALLOCOBJ)
                {
                    canonicalAllocObjFound = true;
                }
            }

            if (canonicalAllocObjFound)
            {
                assert(basicBlockHasNewObj);
                //------------------------------------------------------------------------
                // We expect the following expression tree at this point
                //  *  STMT      void
                //  |  /--*  ALLOCOBJ  ref
                //  |  |  \--*  CNS_INT(h) long
                //  \--*  ASG       ref
                //     \--*  LCL_VAR   ref
                //------------------------------------------------------------------------

                GenTree* op1 = stmtExpr->gtGetOp1();

                assert(op1->OperGet() == GT_LCL_VAR);
                assert(op1->TypeGet() == TYP_REF);
                assert(op2 != nullptr);
                assert(op2->OperGet() == GT_ALLOCOBJ);

                GenTreeAllocObj*     asAllocObj = op2->AsAllocObj();
                unsigned int         lclNum     = op1->AsLclVar()->GetLclNum();
                CORINFO_CLASS_HANDLE clsHnd     = op2->AsAllocObj()->gtAllocObjClsHnd;

                // Don't attempt to do stack allocations inside basic blocks that may be in a loop.
                if (IsObjectStackAllocationEnabled() && !basicBlockHasBackwardJump &&
                    CanAllocateLclVarOnStack(lclNum, clsHnd))
                {
                    JITDUMP("Allocating local variable V%02u on the stack\n", lclNum);

                    const unsigned int stackLclNum = MorphAllocObjNodeIntoStackAlloc(asAllocObj, block, stmt);
                    m_HeapLocalToStackLocalMap.AddOrUpdate(lclNum, stackLclNum);
                    stmt->gtStmtExpr->gtBashToNOP();
                    comp->optMethodFlags |= OMF_HAS_OBJSTACKALLOC;
                    didStackAllocate = true;
                }
                else
                {
                    if (IsObjectStackAllocationEnabled())
                    {
                        JITDUMP("Allocating local variable V%02u on the heap\n", lclNum);
                    }

                    op2 = MorphAllocObjNodeIntoHelperCall(asAllocObj);
                }

                // Propagate flags of op2 to its parent.
                stmtExpr->gtOp.gtOp2 = op2;
                stmtExpr->gtFlags |= op2->gtFlags & GTF_ALL_EFFECT;
            }

#ifdef DEBUG
            else
            {
                // We assume that GT_ALLOCOBJ nodes are always present in the
                // canonical form.
                comp->fgWalkTreePre(&stmt->gtStmtExpr, AssertWhenAllocObjFoundVisitor);
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

    GenTree*     op1                  = allocObj->gtGetOp1();
    unsigned int helper               = allocObj->gtNewHelper;
    bool         helperHasSideEffects = allocObj->gtHelperHasSideEffects;

    GenTreeArgList* args;
#ifdef FEATURE_READYTORUN_COMPILER
    CORINFO_CONST_LOOKUP entryPoint = allocObj->gtEntryPoint;
    if (helper == CORINFO_HELP_READYTORUN_NEW)
    {
        args = nullptr;
    }
    else
#endif
    {
        args = comp->gtNewArgList(op1);
    }

    const bool morphArgs  = false;
    GenTree*   helperCall = comp->fgMorphIntoHelperCall(allocObj, allocObj->gtNewHelper, args, morphArgs);
    if (helperHasSideEffects)
    {
        helperCall->gtCall.gtCallMoreFlags |= GTF_CALL_M_ALLOC_SIDE_EFFECTS;
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (entryPoint.addr != nullptr)
    {
        assert(comp->opts.IsReadyToRun());
        helperCall->gtCall.setEntryPoint(entryPoint);
    }
#endif

    return helperCall;
}

//------------------------------------------------------------------------
// MorphAllocObjNodeIntoStackAlloc: Morph a GT_ALLOCOBJ node into stack
//                                  allocation.
// Arguments:
//    allocObj - GT_ALLOCOBJ that will be replaced by a stack allocation
//    block    - a basic block where allocObj is
//    stmt     - a statement where allocObj is
//
// Return Value:
//    local num for the new stack allocated local
//
// Notes:
//    This function can insert additional statements before stmt.

unsigned int ObjectAllocator::MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj,
                                                              BasicBlock*      block,
                                                              GenTreeStmt*     stmt)
{
    assert(allocObj != nullptr);
    assert(m_AnalysisDone);

    const bool         shortLifetime = false;
    const unsigned int lclNum     = comp->lvaGrabTemp(shortLifetime DEBUGARG("MorphAllocObjNodeIntoStackAlloc temp"));
    const int unsafeValueClsCheck = true;
    comp->lvaSetStruct(lclNum, allocObj->gtAllocObjClsHnd, unsafeValueClsCheck);

    // Initialize the object memory if necessary
    if (comp->fgStructTempNeedsExplicitZeroInit(comp->lvaTable + lclNum, block))
    {
        unsigned int structSize = comp->lvaTable[lclNum].lvSize();

        //------------------------------------------------------------------------
        // *  STMT      void
        // |  /--*  CNS_INT   int    0
        // \--*  ASG       struct (init)
        //    \--*  LCL_VAR   struct
        //------------------------------------------------------------------------

        GenTree*   tree        = comp->gtNewLclvNode(lclNum, TYP_STRUCT);
        const bool isVolatile  = false;
        const bool isCopyBlock = false;
        tree = comp->gtNewBlkOpNode(tree, comp->gtNewIconNode(0), structSize, isVolatile, isCopyBlock);

        GenTreeStmt* newStmt = comp->gtNewStmt(tree);

        comp->fgInsertStmtBefore(block, stmt, newStmt);
    }

    //------------------------------------------------------------------------
    // *  STMT      void
    // |  /--*  CNS_INT(h) long
    // \--*  ASG       long
    //    \--*  FIELD     long   #PseudoField:0x0
    //       \--*  ADDR      byref
    //          \--*  LCL_VAR   struct
    //------------------------------------------------------------------------

    // Create a local representing the object
    GenTree* tree = comp->gtNewLclvNode(lclNum, TYP_STRUCT);

    // Add a pseudo-field for the method table pointer and initialize it
    tree = comp->gtNewOperNode(GT_ADDR, TYP_BYREF, tree);
    tree = comp->gtNewFieldRef(TYP_I_IMPL, FieldSeqStore::FirstElemPseudoField, tree, 0);
    tree = comp->gtNewAssignNode(tree, allocObj->gtGetOp1());

    GenTreeStmt* newStmt = comp->gtNewStmt(tree);

    comp->fgInsertStmtBefore(block, stmt, newStmt);

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
        GenTree* tree                 = parentStack->Index(parentIndex - 1);
        GenTree* parent               = parentStack->Index(parentIndex);
        keepChecking                  = false;

        switch (parent->OperGet())
        {
            case GT_ASG:
            {
                // Use the following conservative behavior for GT_ASG parent node:
                //   Consider local variable to be escaping if
                //   1. lclVar appears on the rhs of a GT_ASG node
                //                      AND
                //   2. The lhs of the GT_ASG is not another lclVar

                GenTree* op1 = parent->AsOp()->gtGetOp1();

                if (op1 == tree)
                {
                    // Assigning to a local doesn't make it escaping.
                    // If there is another local variable on the rhs,
                    // we will update the connection graph when we visit it.
                    canLclVarEscapeViaParentStack = false;
                }
                else
                {
                    // lclVar is on the rhs of GT_ASG node
                    assert(parent->AsOp()->gtGetOp2() == tree);

                    // Update the connection graph if we are assigning to a local.
                    // For all other assignments we mark the rhs local as escaping.
                    // TODO-ObjectStackAllocation: track assignments to fields.
                    if (op1->OperGet() == GT_LCL_VAR)
                    {
                        // We expect the following tree at this point
                        //   /--*  GT_LCL_VAR    ref    rhsLclVar
                        // --*  =         ref
                        //   \--*  GT_LCL_VAR    ref    lhsLclVar

                        // Add an edge to the connection graph.
                        const unsigned int lhsLclNum = op1->AsLclVar()->GetLclNum();
                        const unsigned int rhsLclNum = lclNum;

                        AddConnGraphEdge(lhsLclNum, rhsLclNum);
                        canLclVarEscapeViaParentStack = false;
                    }
                }
                break;
            }

            case GT_EQ:
            case GT_NE:
                canLclVarEscapeViaParentStack = false;
                break;

            case GT_COMMA:
                if (parent->AsOp()->gtGetOp1() == parentStack->Index(parentIndex - 1))
                {
                    // Left child of GT_COMMA, it will be discarded
                    canLclVarEscapeViaParentStack = false;
                    break;
                }
                __fallthrough;
            case GT_COLON:
            case GT_QMARK:
            case GT_ADD:
                // Check whether the local escapes via its grandparent.
                ++parentIndex;
                keepChecking = true;
                break;

            case GT_FIELD:
            case GT_IND:
            {
                int grandParentIndex = parentIndex + 1;
                if ((parentStack->Height() > grandParentIndex) &&
                    (parentStack->Index(grandParentIndex)->OperGet() == GT_ADDR))
                {
                    // Check if the address of the field/ind escapes.
                    parentIndex += 2;
                    keepChecking = true;
                }
                else
                {
                    // Address of the field/ind is not taken so the local doesn't escape.
                    canLclVarEscapeViaParentStack = false;
                }
                break;
            }

            case GT_CALL:
            {
                GenTreeCall* asCall = parent->AsCall();

                if (asCall->gtCallType == CT_HELPER)
                {
                    // TODO-ObjectStackAllocation: Special-case helpers here that
                    // 1. Don't make objects escape.
                    // 2. Protect objects as interior (GCPROTECT_BEGININTERIOR() instead of GCPROTECT_BEGIN()).
                    // 3. Don't check that the object is in the heap in ValidateInner.

                    canLclVarEscapeViaParentStack = true;
                }
                break;
            }

            default:
                break;
        }
    }

    return canLclVarEscapeViaParentStack;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// AssertWhenAllocObjFoundVisitor: Look for a GT_ALLOCOBJ node and assert
//                                 when found one.
//
// Arguments:
//    pTree   - Tree to examine
//    data    - Walker data
//
// Return Value:
//    Always returns fgWalkResult::WALK_CONTINUE

Compiler::fgWalkResult ObjectAllocator::AssertWhenAllocObjFoundVisitor(GenTree** pTree, Compiler::fgWalkData* data)
{
    GenTree* tree = *pTree;

    assert(tree != nullptr);
    assert(tree->OperGet() != GT_ALLOCOBJ);

    return Compiler::fgWalkResult::WALK_CONTINUE;
}

#endif // DEBUG

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
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        RewriteUsesVisitor(ObjectAllocator* allocator)
            : GenTreeVisitor<RewriteUsesVisitor>(allocator->comp), m_allocator(allocator)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            assert(tree != nullptr);
            assert(tree->IsLocal());

            const unsigned int lclNum    = tree->AsLclVarCommon()->gtLclNum;
            unsigned int       newLclNum = BAD_VAR_NUM;

            if (m_allocator->m_HeapLocalToStackLocalMap.TryGetValue(lclNum, &newLclNum))
            {
                GenTree* newTree =
                    m_compiler->gtNewOperNode(GT_ADDR, TYP_I_IMPL, m_compiler->gtNewLclvNode(newLclNum, TYP_STRUCT));
                *use = newTree;
            }

            return Compiler::fgWalkResult::WALK_CONTINUE;
        }
    };

    BasicBlock* block;

    foreach_block(comp, block)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            RewriteUsesVisitor rewriteUsesVisitor(this);
            rewriteUsesVisitor.WalkTree(&stmt->gtStmtExpr, nullptr);
        }
    }
}
