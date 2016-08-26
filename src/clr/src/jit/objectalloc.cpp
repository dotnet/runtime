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

//===============================================================================

//------------------------------------------------------------------------
// DoPhase: Run analysis (if object stack allocation is enabled) and then
//          morph each GT_ALLOCOBJ node either into an allocation helper
//          call or stack allocation.
// Notes:
//    Runs only if Compiler::optMethodFlags has flag OMF_HAS_NEWOBJ set.
void ObjectAllocator::DoPhase()
{
    if ((comp->optMethodFlags & OMF_HAS_NEWOBJ) == 0)
    {
        return;
    }

    if (IsObjectStackAllocationEnabled())
    {
        DoAnalysis();
    }

    MorphAllocObjNodes();
}

//------------------------------------------------------------------------
// DoAnalysis: Walk over basic blocks of the method and detect all local
//             variables that can be allocated on the stack.
//
// Assumptions:
//    Must be run after the dominators have been computed (we need this
//    information to detect loops).
void ObjectAllocator::DoAnalysis()
{
    assert(m_IsObjectStackAllocationEnabled);
    assert(comp->fgDomsComputed);
    // TODO-ObjectStackAllocation
    NYI("DoAnalysis");
}

//------------------------------------------------------------------------
// MorphAllocObjNodes: Morph each GT_ALLOCOBJ node either into an
//                     allocation helper call or stack allocation.
//
// Notes:
//    Runs only over the blocks having bbFlags BBF_HAS_NEWOBJ set.
void ObjectAllocator::MorphAllocObjNodes()
{
    BasicBlock* block;

    foreach_block(comp, block)
    {
        const bool basicBlockHasNewObj = (block->bbFlags & BBF_HAS_NEWOBJ) == BBF_HAS_NEWOBJ;
#ifndef DEBUG
        if (!basicBlockHasNewObj)
        {
            continue;
        }
#endif // DEBUG

        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            GenTreePtr stmtExpr = stmt->gtStmtExpr;
            GenTreePtr op2      = nullptr;

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
                //  *  GT_STMT   void  (top level)
                // 	|  /--*  GT_ALLOCOBJ   ref
                // 	\--*  GT_ASG    ref
                // 	   \--*  GT_LCL_VAR    ref
                //------------------------------------------------------------------------

                GenTreePtr op1 = stmtExpr->gtGetOp1();

                assert(op1->OperGet() == GT_LCL_VAR);
                assert(op1->TypeGet() == TYP_REF);
                assert(op2 != nullptr);
                assert(op2->OperGet() == GT_ALLOCOBJ);

                GenTreeAllocObj* asAllocObj = op2->AsAllocObj();
                unsigned int     lclNum     = op1->AsLclVar()->GetLclNum();

                if (IsObjectStackAllocationEnabled() && CanAllocateLclVarOnStack(lclNum))
                {
                    op2 = MorphAllocObjNodeIntoStackAlloc(asAllocObj, block, stmt);
                }
                else
                {
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
GenTreePtr ObjectAllocator::MorphAllocObjNodeIntoHelperCall(GenTreeAllocObj* allocObj)
{
    assert(allocObj != nullptr);

    GenTreePtr op1 = allocObj->gtGetOp1();

    GenTreePtr helperCall = comp->fgMorphIntoHelperCall(allocObj, allocObj->gtNewHelper, comp->gtNewArgList(op1));

    return helperCall;
}

//------------------------------------------------------------------------
// MorphAllocObjNodeIntoStackAlloc: Morph a GT_ALLOCOBJ node into stack
//                                  allocation.
// Arguments:
//    allocObj - GT_ALLOCOBJ that will be replaced by helper call.
//    block    - a basic block where allocObj is
//    stmt     - a statement where allocObj is
//
// Return Value:
//    Address of tree doing stack allocation (can be the same as allocObj).
//
// Notes:
//    Must update parents flags after this.
//    This function can insert additional statements before stmt.
GenTreePtr ObjectAllocator::MorphAllocObjNodeIntoStackAlloc(GenTreeAllocObj* allocObj,
                                                            BasicBlock*      block,
                                                            GenTreeStmt*     stmt)
{
    assert(allocObj != nullptr);
    assert(m_AnalysisDone);

    // TODO-StackAllocation
    NYI("MorphAllocObjIntoStackAlloc");

    return allocObj;
}

#ifdef DEBUG

//------------------------------------------------------------------------
// AssertWhenAllocObjFoundVisitor: Look for a GT_ALLOCOBJ node and assert
//                                 when found one.
Compiler::fgWalkResult ObjectAllocator::AssertWhenAllocObjFoundVisitor(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    assert(tree != nullptr);
    assert(tree->OperGet() != GT_ALLOCOBJ);

    return Compiler::fgWalkResult::WALK_CONTINUE;
}

#endif // DEBUG

//===============================================================================
