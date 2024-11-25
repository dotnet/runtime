// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//                                    Early Value Propagation
//
// This phase performs an SSA-based value propagation optimization that currently only applies to array
// lengths and explicit null checks. An SSA-based backwards tracking of local variables
// is performed at each point of interest, e.g., an array length reference site, a method table reference site, or
// an indirection.
// The tracking continues until an interesting value is encountered. The value is then used to rewrite
// the source site or the value.
//
///////////////////////////////////////////////////////////////////////////////////////

#include "jitpch.h"

bool Compiler::optDoEarlyPropForFunc()
{
    // TODO-MDArray: bool propMDArrayLen = (optMethodFlags & OMF_HAS_MDNEWARRAY) && (optMethodFlags &
    // OMF_HAS_MDARRAYREF);
    bool propArrayLen  = (optMethodFlags & OMF_HAS_NEWARRAY) && (optMethodFlags & OMF_HAS_ARRAYREF);
    bool propNullCheck = (optMethodFlags & OMF_HAS_NULLCHECK) != 0;
    return propArrayLen || propNullCheck;
}

bool Compiler::optDoEarlyPropForBlock(BasicBlock* block)
{
    // TODO-MDArray: bool bbHasMDArrayRef = block->HasFlag(BBF_HAS_MID_IDX_LEN);
    bool bbHasArrayRef  = block->HasFlag(BBF_HAS_IDX_LEN);
    bool bbHasNullCheck = block->HasFlag(BBF_HAS_NULLCHECK);
    return bbHasArrayRef || bbHasNullCheck;
}

#ifdef DEBUG
//-----------------------------------------------------------------------------
// optCheckFlagsAreSet: Check that the method flag and the basic block flag are set.
//
// Arguments:
//    methodFlag           - The method flag to check.
//    methodFlagStr        - String representation of the method flag.
//    bbFlag               - The basic block flag to check.
//    bbFlagStr            - String representation of the basic block flag.
//    tree                 - Tree that makes the flags required.
//    basicBlock           - The basic block to check the flag on.

void Compiler::optCheckFlagsAreSet(unsigned    methodFlag,
                                   const char* methodFlagStr,
                                   unsigned    bbFlag,
                                   const char* bbFlagStr,
                                   GenTree*    tree,
                                   BasicBlock* basicBlock)
{
    if ((optMethodFlags & methodFlag) == 0)
    {
        printf("%s is not set on optMethodFlags but is required because of the following tree\n", methodFlagStr);
        gtDispTree(tree);
        assert(false);
    }

    if (!basicBlock->HasFlag((BasicBlockFlags)bbFlag))
    {
        printf("%s is not set on " FMT_BB " but is required because of the following tree \n", bbFlagStr,
               basicBlock->bbNum);
        gtDispTree(tree);
        assert(false);
    }
}
#endif

//------------------------------------------------------------------------------------------
// optEarlyProp: The entry point of the early value propagation.
//
// Returns:
//    suitable phase status
//
// Notes:
//    This phase performs an SSA-based value propagation, including array
//    length propagation and null check folding.
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
//    Null check folding tries to find GT_INDIR(obj + const) that GT_NULLCHECK(obj) can be folded into
//    and removed. Currently, the algorithm only matches GT_INDIR and GT_NULLCHECK in the same basic block.
//
//    TODO: support GT_MDARR_LENGTH, GT_MDARRAY_LOWER_BOUND
//
PhaseStatus Compiler::optEarlyProp()
{
    if (!optDoEarlyPropForFunc())
    {
        // We perhaps should verify the OMF are set properly
        //
        JITDUMP("no arrays or null checks in the method\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    assert(fgSsaPassesCompleted == 1);
    unsigned numChanges = 0;

    for (BasicBlock* const block : Blocks())
    {
#ifndef DEBUG
        if (!optDoEarlyPropForBlock(block))
        {
            continue;
        }
#endif

        compCurBB = block;

        CompAllocator                 allocator(getAllocator(CMK_EarlyProp));
        LocalNumberToNullCheckTreeMap nullCheckMap(allocator);

        for (Statement* stmt = block->firstStmt(); stmt != nullptr;)
        {
            // Preserve the next link before the propagation and morph.
            Statement* next = stmt->GetNextStmt();

            compCurStmt = stmt;

            // Walk the stmt tree in linear order to rewrite any array length reference with a
            // constant array length.
            bool isRewritten = false;
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                GenTree* rewrittenTree = optEarlyPropRewriteTree(tree, &nullCheckMap);
                if (rewrittenTree != nullptr)
                {
                    gtUpdateSideEffects(stmt, rewrittenTree);
                    isRewritten = true;
                    tree        = rewrittenTree;
                }
            }

            // Update the evaluation order and the statement info if the stmt has been rewritten.
            if (isRewritten)
            {
                // Make sure the transformation happens in debug, check, and release build.
                assert(optDoEarlyPropForFunc() && optDoEarlyPropForBlock(block));
                gtSetStmtInfo(stmt);
                fgSetStmtSeq(stmt);
                numChanges++;
            }

            stmt = next;
        }
    }

    JITDUMP("\nOptimized %u trees\n", numChanges);
    return numChanges > 0 ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//----------------------------------------------------------------
// optEarlyPropRewriteValue: Rewrite a tree to the actual value.
//
// Arguments:
//    tree           - The input tree node to be rewritten.
//    nullCheckMap   - Map of the local numbers to the latest NULLCHECKs on those locals in the current basic block.
//
// Return Value:
//    Return a new tree if the original tree was successfully rewritten.
//    The containing tree links are updated.
//
GenTree* Compiler::optEarlyPropRewriteTree(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap)
{
    if (tree->OperIsIndirOrArrMetaData() && optFoldNullCheck(tree, nullCheckMap))
    {
        // optFoldNullCheck takes care of updating statement info if a null check is removed.
        return tree;
    }
    return nullptr;
}

//----------------------------------------------------------------
// optFoldNullChecks: Try to find a GT_NULLCHECK node that can be folded into the indirection node mark it for removal
// if possible.
//
// Arguments:
//    tree           - The input indirection tree.
//    nullCheckMap   - Map of the local numbers to the latest NULLCHECKs on those locals in the current basic block
//
// Returns:
//    true if a null check was folded
//
// Notes:
//    If a GT_NULLCHECK node is post-dominated by an indirection node on the same local and the trees between
//    the GT_NULLCHECK and the indirection don't have unsafe side effects, the GT_NULLCHECK can be removed.
//    The indir will cause a NullReferenceException if and only if GT_NULLCHECK will cause the same
//    NullReferenceException.

bool Compiler::optFoldNullCheck(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap)
{
#ifdef DEBUG
    if (tree->OperGet() == GT_NULLCHECK)
    {
        optCheckFlagsAreSet(OMF_HAS_NULLCHECK, "OMF_HAS_NULLCHECK", BBF_HAS_NULLCHECK, "BBF_HAS_NULLCHECK", tree,
                            compCurBB);
    }
#else
    if (!compCurBB->HasFlag(BBF_HAS_NULLCHECK))
    {
        return false;
    }
#endif

    GenTree*   nullCheckTree   = optFindNullCheckToFold(tree, nullCheckMap);
    GenTree*   nullCheckParent = nullptr;
    Statement* nullCheckStmt   = nullptr;
    bool       folded          = false;
    if ((nullCheckTree != nullptr) && optIsNullCheckFoldingLegal(tree, nullCheckTree, &nullCheckParent, &nullCheckStmt))
    {
#ifdef DEBUG
        // Make sure the transformation happens in debug, check, and release build.
        assert(optDoEarlyPropForFunc() && optDoEarlyPropForBlock(compCurBB) && compCurBB->HasFlag(BBF_HAS_NULLCHECK));
        if (verbose)
        {
            printf("optEarlyProp Marking a null check for removal\n");
            gtDispTree(nullCheckTree);
            printf("\n");
        }
#endif
        // Remove the null check
        nullCheckTree->gtFlags &= ~(GTF_EXCEPT | GTF_DONT_CSE);

        // Set this flag to prevent reordering
        nullCheckTree->SetHasOrderingSideEffect();
        nullCheckTree->gtFlags |= GTF_IND_NONFAULTING;

        if (nullCheckParent != nullptr)
        {
            nullCheckParent->gtFlags &= ~GTF_DONT_CSE;
        }

        nullCheckMap->Remove(nullCheckTree->gtGetOp1()->AsLclVarCommon()->GetLclNum());

        // Re-morph the statement.
        Statement* curStmt = compCurStmt;
        fgMorphBlockStmt(compCurBB, nullCheckStmt DEBUGARG("optFoldNullCheck"));
        optRecordSsaUses(nullCheckStmt->GetRootNode(), compCurBB);
        compCurStmt = curStmt;

        folded = true;
    }

    if ((tree->OperGet() == GT_NULLCHECK) && (tree->gtGetOp1()->OperGet() == GT_LCL_VAR))
    {
        nullCheckMap->Set(tree->gtGetOp1()->AsLclVarCommon()->GetLclNum(), tree,
                          LocalNumberToNullCheckTreeMap::SetKind::Overwrite);
    }

    return folded;
}

//----------------------------------------------------------------
// optFindNullCheckToFold: Try to find a GT_NULLCHECK node that can be folded into the indirection node.
//
// Arguments:
//    tree           - The input indirection tree.
//    nullCheckMap   - Map of the local numbers to the latest NULLCHECKs on those locals in the current basic block
//
// Notes:
//    Check for cases where
//    1. One of the following trees
//
//       nullcheck(x)
//       or
//       x = comma(nullcheck(y), add(y, const1))
//
//       is post-dominated in the same basic block by one of the following trees
//
//       indir(x)
//       or
//       indir(add(x, const2))
//
//       (indir is any node for which OperIsIndirOrArrMetaData() is true.)
//
//     2.  const1 + const2 if sufficiently small.

GenTree* Compiler::optFindNullCheckToFold(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap)
{
    assert(tree->OperIsIndirOrArrMetaData());

    GenTree* addr = tree->GetIndirOrArrMetaDataAddr()->gtEffectiveVal();

    ssize_t offsetValue = 0;

    if ((addr->OperGet() == GT_ADD) && addr->gtGetOp2()->IsCnsIntOrI())
    {
        offsetValue += addr->gtGetOp2()->AsIntConCommon()->IconValue();
        addr = addr->gtGetOp1();
    }

    if (addr->OperGet() != GT_LCL_VAR)
    {
        return nullptr;
    }

    GenTreeLclVarCommon* const lclVarNode = addr->AsLclVarCommon();
    const unsigned             ssaNum     = lclVarNode->GetSsaNum();

    if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return nullptr;
    }

    const unsigned lclNum          = lclVarNode->GetLclNum();
    GenTree*       nullCheckTree   = nullptr;
    unsigned       nullCheckLclNum = BAD_VAR_NUM;

    // Check if we saw a nullcheck on this local in this basic block
    // This corresponds to nullcheck(x) tree in the header comment.
    if (nullCheckMap->Lookup(lclNum, &nullCheckTree))
    {
        GenTree* nullCheckAddr = nullCheckTree->AsIndir()->Addr();
        if ((nullCheckAddr->OperGet() != GT_LCL_VAR) || (nullCheckAddr->AsLclVarCommon()->GetSsaNum() != ssaNum))
        {
            nullCheckTree = nullptr;
        }
        else
        {
            nullCheckLclNum = nullCheckAddr->AsLclVarCommon()->GetLclNum();
        }
    }

    if (nullCheckTree == nullptr)
    {
        // Check if we have x = comma(nullcheck(y), add(y, const1)) pattern.

        // Find the definition of the indirected local ('x' in the pattern above).
        LclSsaVarDsc* defLoc = lvaTable[lclNum].GetPerSsaData(ssaNum);

        if (compCurBB != defLoc->GetBlock())
        {
            return nullptr;
        }

        GenTreeLclVarCommon* defNode = defLoc->GetDefNode();
        if ((defNode == nullptr) || !defNode->OperIs(GT_STORE_LCL_VAR) || (defNode->GetLclNum() != lclNum))
        {
            return nullptr;
        }

        GenTree* defValue = defNode->Data();
        if (defValue->OperGet() != GT_COMMA)
        {
            return nullptr;
        }

        GenTree* commaOp1EffectiveValue = defValue->gtGetOp1()->gtEffectiveVal();

        if (commaOp1EffectiveValue->OperGet() != GT_NULLCHECK)
        {
            return nullptr;
        }

        GenTree* nullCheckAddress = commaOp1EffectiveValue->gtGetOp1();

        if ((nullCheckAddress->OperGet() != GT_LCL_VAR) || (defValue->gtGetOp2()->OperGet() != GT_ADD))
        {
            return nullptr;
        }

        // We found a candidate for 'y' in the pattern above.

        GenTree* additionNode = defValue->gtGetOp2();
        GenTree* additionOp1  = additionNode->gtGetOp1();
        GenTree* additionOp2  = additionNode->gtGetOp2();
        if ((additionOp1->OperGet() == GT_LCL_VAR) &&
            (additionOp1->AsLclVarCommon()->GetLclNum() == nullCheckAddress->AsLclVarCommon()->GetLclNum()) &&
            (additionOp2->IsCnsIntOrI()))
        {
            offsetValue += additionOp2->AsIntConCommon()->IconValue();
            nullCheckTree = commaOp1EffectiveValue;
        }
    }

    if (fgIsBigOffset(offsetValue))
    {
        return nullptr;
    }
    else
    {
        return nullCheckTree;
    }
}

//----------------------------------------------------------------
// optIsNullCheckFoldingLegal: Check the nodes between the GT_NULLCHECK node and the indirection to determine
//                             if null check folding is legal.
//
// Arguments:
//    tree                - The input indirection tree.
//    nullCheckTree       - The GT_NULLCHECK tree that is a candidate for removal.
//    nullCheckParent     - The parent of the GT_NULLCHECK tree that is a candidate for removal (out-parameter).
//    nullCheckStatement  - The statement of the GT_NULLCHECK tree that is a candidate for removal (out-parameter).

bool Compiler::optIsNullCheckFoldingLegal(GenTree*    tree,
                                          GenTree*    nullCheckTree,
                                          GenTree**   nullCheckParent,
                                          Statement** nullCheckStmt)
{
    // Check all nodes between the GT_NULLCHECK and the indirection to see
    // if any nodes have unsafe side effects.
    unsigned       nullCheckLclNum    = nullCheckTree->gtGetOp1()->AsLclVarCommon()->GetLclNum();
    bool           isInsideTry        = compCurBB->hasTryIndex();
    bool           canRemoveNullCheck = true;
    const unsigned maxNodesWalked     = 50;
    unsigned       nodesWalked        = 0;

    // First walk the nodes in the statement containing the GT_NULLCHECK in forward execution order
    // until we get to the indirection or process the statement root.
    GenTree* previousTree = nullCheckTree;
    GenTree* currentTree  = nullCheckTree->gtNext;
    assert(fgNodeThreading == NodeThreading::AllTrees);
    while (canRemoveNullCheck && (currentTree != tree) && (currentTree != nullptr))
    {
        if ((*nullCheckParent == nullptr) && currentTree->TryGetUse(nullCheckTree))
        {
            *nullCheckParent = currentTree;
        }
        const bool checkExceptionSummary = false;
        if ((nodesWalked++ > maxNodesWalked) ||
            !optCanMoveNullCheckPastTree(currentTree, nullCheckLclNum, isInsideTry, checkExceptionSummary))
        {
            canRemoveNullCheck = false;
        }
        else
        {
            previousTree = currentTree;
            currentTree  = currentTree->gtNext;
        }
    }

    if (currentTree == tree)
    {
        // The GT_NULLCHECK and the indirection are in the same statements.
        *nullCheckStmt = compCurStmt;
    }
    else
    {
        // The GT_NULLCHECK and the indirection are in different statements.
        // Walk the nodes in the statement containing the indirection
        // in reverse execution order starting with the indirection's
        // predecessor.
        GenTree* nullCheckStatementRoot = previousTree;
        currentTree                     = tree->gtPrev;
        while (canRemoveNullCheck && (currentTree != nullptr))
        {
            const bool checkExceptionSummary = false;
            if ((nodesWalked++ > maxNodesWalked) ||
                !optCanMoveNullCheckPastTree(currentTree, nullCheckLclNum, isInsideTry, checkExceptionSummary))
            {
                canRemoveNullCheck = false;
            }
            else
            {
                currentTree = currentTree->gtPrev;
            }
        }

        // Finally, walk the statement list in reverse execution order
        // until we get to the statement containing the null check.
        // We only check the side effects at the root of each statement.
        Statement* curStmt = compCurStmt->GetPrevStmt();
        currentTree        = curStmt->GetRootNode();
        while (canRemoveNullCheck && (currentTree != nullCheckStatementRoot))
        {
            const bool checkExceptionSummary = true;
            if ((nodesWalked++ > maxNodesWalked) ||
                !optCanMoveNullCheckPastTree(currentTree, nullCheckLclNum, isInsideTry, checkExceptionSummary))
            {
                canRemoveNullCheck = false;
            }
            else
            {
                curStmt     = curStmt->GetPrevStmt();
                currentTree = curStmt->GetRootNode();
            }
        }
        *nullCheckStmt = curStmt;
    }

    if (canRemoveNullCheck && (*nullCheckParent == nullptr))
    {
        *nullCheckParent = nullCheckTree->gtGetParent(nullptr);
    }

    return canRemoveNullCheck;
}

//----------------------------------------------------------------
// optCanMoveNullCheckPastTree: Check if a nullcheck node that is before `tree`
//                              in execution order may be folded into an indirection node that
//                              is after `tree` is execution order.
//
// Arguments:
//    tree                  - The tree to check.
//    nullCheckLclNum       - The local variable that GT_NULLCHECK checks.
//    isInsideTry           - True if tree is inside try, false otherwise.
//    checkSideEffectSummary -If true, check side effect summary flags only,
//                            otherwise check the side effects of the operation itself.
//
// Return Value:
//    True if nullcheck may be folded into a node that is after tree in execution order,
//    false otherwise.

bool Compiler::optCanMoveNullCheckPastTree(GenTree* tree,
                                           unsigned nullCheckLclNum,
                                           bool     isInsideTry,
                                           bool     checkSideEffectSummary)
{
    bool result = true;

    if ((tree->gtFlags & GTF_CALL) != 0)
    {
        result = !checkSideEffectSummary && !tree->OperRequiresCallFlag(this);
    }

    if (result && (tree->gtFlags & GTF_EXCEPT) != 0)
    {
        result = !checkSideEffectSummary && !tree->OperMayThrow(this);
    }

    if (result && ((tree->gtFlags & GTF_ASG) != 0))
    {
        if (tree->OperIsStore())
        {
            if (checkSideEffectSummary && ((tree->Data()->gtFlags & GTF_ASG) != 0))
            {
                result = false;
            }
            else if (isInsideTry)
            {
                // Inside try we allow only stores to locals not live in handlers.
                result = tree->OperIs(GT_STORE_LCL_VAR) && !lvaTable[tree->AsLclVar()->GetLclNum()].lvLiveInOutOfHndlr;
            }
            else
            {
                // We disallow stores to global memory.
                result = tree->OperIsLocalStore() && !lvaGetDesc(tree->AsLclVarCommon())->IsAddressExposed();

                // TODO-ASG-Cleanup: delete this zero-diff quirk. Some setup args for by-ref args do not have GLOB_REF.
                if ((tree->gtFlags & GTF_GLOB_REF) == 0)
                {
                    result = true;
                }
            }
        }
        else if (checkSideEffectSummary)
        {
            result = !isInsideTry && ((tree->gtFlags & GTF_GLOB_REF) == 0);
        }
        else
        {
            result = !isInsideTry && (!tree->OperRequiresAsgFlag() || ((tree->gtFlags & GTF_GLOB_REF) == 0));
        }
    }

    return result;
}
