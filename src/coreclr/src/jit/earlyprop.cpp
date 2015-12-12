//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
//                                    Early Value Propagation
//
// This phase performs an SSA-based value propagation optimization, currently only applies to array 
// lengths and runtime type handles. An SSA-based backwards tracking of local variables is performed 
// at each point of interest, e.g., an array length reference site or a method table reference site. 
// The tracking continues until an interesting value is encountered. The value is then used to rewrite
// the source site.
//
///////////////////////////////////////////////////////////////////////////////////////

#include "jitpch.h"
#include "ssabuilder.h"


bool Compiler::optDoEarlyPropForFunc()
{
    bool propArrayLen = (optMethodFlags & OMF_HAS_NEWARRAY) && (optMethodFlags & OMF_HAS_ARRAYREF);
    bool propGetType = (optMethodFlags & OMF_HAS_NEWOBJ) && (optMethodFlags & OMF_HAS_VTABLEREF);
    return propArrayLen || propGetType;
}

bool Compiler::optDoEarlyPropForBlock(BasicBlock* block)
{
    bool bbHasArrayRef = (block->bbFlags & BBF_HAS_INDX) != 0;
    bool bbHasVtableRef = (block->bbFlags & BBF_HAS_VTABREF) != 0;
    return bbHasArrayRef || bbHasVtableRef;
}

//--------------------------------------------------------------------
// gtIsVtableRef: Return true if the tree is a method table reference.
//
// Arguments:
//    tree           - The input tree.
//
// Return Value:
//    Return true if the tree is a method table reference. 

bool Compiler::gtIsVtableRef(GenTreePtr tree)
{
    if (tree->OperGet() == GT_IND)
    {
        GenTreeIndir* indir = tree->AsIndir();

        if (!indir->HasIndex())
        {
            // Check if the base is an reference pointer.
            if (indir->Base()->TypeGet() == TYP_REF)
            {
                return true;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------------
// getArrayLengthFromAllocation: Return the array length for an array allocation 
//                               helper call.
//
// Arguments:
//    tree           - The array allocation helper call.
//
// Return Value:
//    Return the array length node. 

GenTreePtr Compiler::getArrayLengthFromAllocation(GenTreePtr tree)
{
    assert(tree != nullptr);

    if (tree->OperGet() == GT_CALL)
    {
        GenTreeCall* call = tree->AsCall();

        if (call->gtCallType == CT_HELPER)
        {
            CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);

            if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_DIRECT) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_OBJ) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_VC) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_ALIGN8))
            {
                // This is an array allocation site. Grab the array length node.
                return gtArgEntryByArgNum(call, 1)->node;
            }
        }
    }

    return nullptr;
}

//-----------------------------------------------------------------------------
// getObjectHandleNodeFromAllocation: Return the type handle for an object allocation 
//                              helper call.
//
// Arguments:
//    tree           - The object allocation helper call.
//
// Return Value:
//    Return the object type handle node. 

GenTreePtr Compiler::getObjectHandleNodeFromAllocation(GenTreePtr tree)
{
    assert(tree != nullptr);

    if (tree->OperGet() == GT_CALL)
    {
        GenTreeCall* call = tree->AsCall();

        if (call->gtCallType == CT_HELPER)
        {
            CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);

            if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWFAST) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWSFAST) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWSFAST_ALIGN8) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_DIRECT) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_OBJ) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_VC) ||
                call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_NEWARR_1_ALIGN8))
            {
                // This is an object allocation site. Return the runtime type handle node.
                fgArgTabEntryPtr argTabEntry = gtArgEntryByArgNum(call, 0);
                return argTabEntry->node;
            }
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------------------------
// optEarlyProp: The entry point of the early value propagation.
//
// Notes:
//    This phase performs an SSA-based value propagation, including
//      1. Array length propagation.
//      2. Runtime type handle propagation.
//
//    For array length propagation, a demand-driven SSA-based backwards tracking of constant 
//    array lengths is performed at each array length reference site which is in form of a 
//    GT_ARR_LENGTH node. When a GT_ARR_LENGTH node is seen, the array ref pointer which is 
//    the only child node of the GT_ARR_LENGTH is tracked. This is only done for array ref 
//    pointers that have valid SSA forms.The tracking is along SSA use-def chain and stops 
//    at the original array allocation site where we can grab the array length. The 
//    GT_ARR_LENGTH node will then be rewritten to a GT_CNS_INT node if the array length is 
//    constant.
//
//    Similarly, the same algorithm also applies to rewriting a method table (also known as 
//    vtable) reference site which is in form of GT_INDIR node. The base pointer, which is 
//    an object reference pointer, is treated in the same way as an array reference pointer.

void Compiler::optEarlyProp()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optEarlyProp()\n");
    }
#endif

    assert(fgSsaPassesCompleted == 1);

    if (!optDoEarlyPropForFunc())
    {
        return;
    }

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (!optDoEarlyPropForBlock(block))
            continue;

        compCurBB = block;

        for (GenTreeStmt* stmt = block->firstStmt(); stmt != nullptr; )
        {
            // Preserve the next link before the propagation and morph.
            GenTreeStmt* next = stmt->gtNextStmt;

            compCurStmt = stmt;

            // Walk the stmt tree in linear order to rewrite any array length reference with a 
            // constant array length.
            bool isRewritten = false;
            for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree != nullptr; tree = tree->gtNext)
            {
                if (optEarlyPropRewriteTree(tree))
                {
                    isRewritten = true;
                }
            }

            // Morph the stmt and update the evaluation order if the stmt has been rewritten. 
            if (isRewritten)
            {
                gtSetStmtInfo(stmt);
                fgSetStmtSeq(stmt);
            }

            stmt = next;
        }
    } 

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\nAfter optEarlyProp:\n");
        fgDispBasicBlocks(/*dumpTrees*/true);
    }
#endif
}

//----------------------------------------------------------------
// optEarlyPropRewriteValue: Rewrite a tree to the actual value.
//
// Arguments:
//    tree           - The input tree node to be rewritten.
//
// Return Value:
//    Return true iff "tree" is successfully rewritten. 

bool Compiler::optEarlyPropRewriteTree(GenTreePtr tree)
{
    GenTreePtr  objectRefPtr = nullptr;
    optPropKind propKind = optPropKind::OPK_INVALID;

    if (tree->OperGet() == GT_ARR_LENGTH)
    {
        objectRefPtr = tree->gtOp.gtOp1;
        propKind = optPropKind::OPK_ARRAYLEN;
    }
    else if (gtIsVtableRef(tree))
    {
        objectRefPtr = tree->gtOp.gtOp1;
        propKind = optPropKind::OPK_OBJ_GETTYPE;
    }
    else
    {
        return false;
    }

    if (!objectRefPtr->OperIsScalarLocal() ||
        fgExcludeFromSsa(objectRefPtr->AsLclVarCommon()->GetLclNum()))

    {
        return false;
    }

    bool       isRewritten = false;
    GenTreePtr root = compCurStmt;
    unsigned   lclNum = objectRefPtr->AsLclVarCommon()->GetLclNum();
    unsigned   ssaNum = objectRefPtr->AsLclVarCommon()->GetSsaNum();

    GenTreePtr actualVal = optPropGetValue(lclNum, ssaNum, propKind);

    if (actualVal != nullptr)
    {
        if (propKind == optPropKind::OPK_ARRAYLEN)
        {
            assert(actualVal->IsCnsIntOrI());
         
            if (actualVal->gtIntCon.gtIconVal > INT32_MAX)
            {
                // Don't propagate array lengths that are beyond the maximum value of a GT_ARR_LENGTH.
                // node. CORINFO_HELP_NEWARR_1_OBJ helper call allows to take a long integer as the 
                // array length argument, but the type of GT_ARR_LENGTH is always INT32.
                return  false;
            }
        }
        else if (propKind == optPropKind::OPK_OBJ_GETTYPE)
        {
            assert(actualVal->IsCnsIntOrI());
        }
        
#ifdef DEBUG
        if (verbose)
        {
            printf("optEarlyProp Rewriting BB%02u\n", compCurBB->bbNum);
            gtDispTree(root);
            printf("\n");
        }
#endif
        // Rewrite the tree using a copy of "actualVal"
        GenTreePtr actualValCopy;
        var_types  origType = tree->gtType;

        if (actualVal->GetNodeSize() <= tree->GetNodeSize())
        {
            actualValCopy = tree;
        }
        else
        {
            actualValCopy = gtNewLargeOperNode(GT_ADD, TYP_INT);
        }

        fgWalkTreePre(&tree, Compiler::lvaDecRefCntsCB, (void*)this, true);

        actualValCopy->CopyFrom(actualVal, this);
        actualValCopy->gtType = origType;

        fgWalkTreePre(&actualValCopy, Compiler::lvaIncRefCntsCB, (void*)this, true);
        
        if (actualValCopy != tree)
        {
            gtReplaceTree(root, tree, actualValCopy);
        }

        isRewritten = true;

#ifdef DEBUG
        if (verbose)
        {
            printf("to\n");
            gtDispTree(compCurStmt);
            printf("\n");
        }
#endif
    }

    return  isRewritten;
}

//-------------------------------------------------------------------------------------------
// optPropGetValue: Given an SSA object ref pointer, get the value needed based on valueKind. 
//
// Arguments:
//    lclNum         - The local var number of the ref pointer.
//    ssaNum         - The SSA var number of the ref pointer.
//    valueKind      - The kind of value of interest.
//
// Return Value:
//    Return the corresponding value based on valueKind.

GenTreePtr Compiler::optPropGetValue(unsigned lclNum, unsigned ssaNum, optPropKind valueKind)
{
    return optPropGetValueRec(lclNum, ssaNum, valueKind, 0);
}

//-----------------------------------------------------------------------------------
// optPropGetValueRec: Given an SSA object ref pointer, get the value needed based on valueKind
//                     within a recursion bound. 
//
// Arguments:
//    lclNum         - The local var number of the array pointer.
//    ssaNum         - The SSA var number of the array pointer.
//    valueKind      - The kind of value of interest.
//    walkDepth      - Current recursive walking depth.
//
// Return Value:
//    Return the corresponding value based on valueKind.

GenTreePtr Compiler::optPropGetValueRec(unsigned lclNum, unsigned ssaNum, optPropKind valueKind, int walkDepth)
{
    if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return nullptr;
    }

    SSAName    ssaName(lclNum, ssaNum);
    GenTreePtr value = nullptr;

    // Bound the recursion with a hard limit. 
    if (walkDepth > optEarlyPropRecurBound)
    {
        return nullptr;
    }

    // Track along the use-def chain to get the array length
    GenTreePtr  treelhs = lvaTable[lclNum].GetPerSsaData(ssaNum)->m_defLoc.m_tree;

    if (treelhs == nullptr)
    {
        // Incoming parameters or live-in variables don't have actual definition tree node
        // for their FIRST_SSA_NUM. See SsaBuilder::RenameVariables.
        assert(ssaNum == SsaConfig::FIRST_SSA_NUM);
    }
    else
    {
        GenTreePtr *lhsPtr;
        GenTreePtr  treeDefParent = treelhs->gtGetParent(&lhsPtr);

        if (treeDefParent->OperGet() == GT_ASG)
        {
            assert(treelhs == treeDefParent->gtGetOp1());
            GenTreePtr treeRhs = treeDefParent->gtGetOp2();

            if (treeRhs->OperIsScalarLocal() && !fgExcludeFromSsa(treeRhs->AsLclVarCommon()->GetLclNum()))
            {
                // Recursively track the Rhs
                unsigned rhsLclNum = treeRhs->AsLclVarCommon()->GetLclNum();
                unsigned rhsSsaNum = treeRhs->AsLclVarCommon()->GetSsaNum();

                value = optPropGetValueRec(rhsLclNum, rhsSsaNum, valueKind, walkDepth + 1);
            }
            else
            {
                if (valueKind == optPropKind::OPK_ARRAYLEN)
                {
                    value = getArrayLengthFromAllocation(treeRhs);
                    if (value != nullptr)
                    {
                        if (!value->IsCnsIntOrI())
                        {
                            // Leave out non-constant-sized array
                            value = nullptr;
                        }
                    }
                }
                else if(valueKind == optPropKind::OPK_OBJ_GETTYPE)
                {
                    value = getObjectHandleNodeFromAllocation(treeRhs);
                    if (value != nullptr)
                    {
                        if (!value->IsCnsIntOrI())
                        {
                            // Leave out non-constant-sized array
                            value = nullptr;
                        }
                    }
                }
            }
        }
    }

    return value;
}
