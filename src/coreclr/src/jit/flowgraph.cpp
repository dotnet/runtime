// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          FlowGraph                                        XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h"     // for alloca

/*****************************************************************************/


void                Compiler::fgInit()
{
    impInit();

    /* Initialization for fgWalkTreePre() and fgWalkTreePost() */

    fgFirstBBScratch             = nullptr;

#ifdef DEBUG
    fgInlinedCount               = 0;
    fgPrintInlinedMethods = JitConfig.JitPrintInlinedMethods() == 1;
#endif // DEBUG

    /* We haven't yet computed the bbPreds lists */
    fgComputePredsDone  = false;

    /* We haven't yet computed the bbCheapPreds lists */
    fgCheapPredsValid  = false;

    /* We haven't yet computed the edge weight */
    fgEdgeWeightsComputed    = false;
    fgHaveValidEdgeWeights   = false;
    fgSlopUsedInEdgeWeights  = false;
    fgRangeUsedInEdgeWeights = true;
    fgNeedsUpdateFlowGraph   = false;
    fgCalledWeight           = BB_ZERO_WEIGHT;

    /* We haven't yet computed the dominator sets */
    fgDomsComputed  = false;

#ifdef DEBUG
    fgReachabilitySetsValid = false;
#endif // DEBUG

    /* We don't know yet which loops will always execute calls */
    fgLoopCallMarked = false;

    /* We haven't created GC Poll blocks yet. */
    fgGCPollsCreated = false;

    /* Initialize the basic block list */

    fgFirstBB        = NULL;
    fgLastBB         = NULL;
    fgFirstColdBlock = NULL;

#if FEATURE_EH_FUNCLETS
    fgFirstFuncletBB  = NULL;
    fgFuncletsCreated = false;
#endif // FEATURE_EH_FUNCLETS

    fgBBcount        = 0;

#ifdef DEBUG
    fgBBcountAtCodegen = 0;
#endif // DEBUG

    fgBBNumMax        = 0;
    fgEdgeCount       = 0;
    fgDomBBcount      = 0;
    fgBBVarSetsInited = false;
    fgReturnCount     = 0;

    // Initialize BlockSet data.
    fgCurBBEpoch             = 0;
    fgCurBBEpochSize         = 0;
    fgBBSetCountInSizeTUnits = 0;

    genReturnBB       = NULL;

    /* We haven't reached the global morphing phase */
    fgGlobalMorph   = false;
    fgExpandInline  = false;
    fgModified      = false;

#ifdef DEBUG
    fgSafeBasicBlockCreation = true;
#endif // DEBUG

    fgLocalVarLivenessDone = false;

    /* Statement list is not threaded yet */

    fgStmtListThreaded = false;

    // Initialize the logic for adding code. This is used to insert code such
    // as the code that raises an exception when an array range check fails.

    fgAddCodeList    = 0;
    fgAddCodeModf    = false;

    for (int i = 0; i < SCK_COUNT; i++)
    {
        fgExcptnTargetCache[i] = NULL;
    }

    /* Keep track of the max count of pointer arguments */

    fgPtrArgCntCur   = 0;
    fgPtrArgCntMax   = 0;

    /* This global flag is set whenever we remove a statement */
    fgStmtRemoved   = false;

    /* This global flag is set whenever we add a throw block for a RngChk */
    fgRngChkThrowAdded = false; /* reset flag for fgIsCodeAdded() */

    fgIncrCount     = 0;

    /* We will record a list of all BBJ_RETURN blocks here */
    fgReturnBlocks  = NULL;

    /* This is set by fgComputeReachability */
    fgEnterBlks     = BlockSetOps::UninitVal();

#ifdef DEBUG
    fgEnterBlksSetValid     = false;
#endif // DEBUG

#if !FEATURE_EH_FUNCLETS
    ehMaxHndNestingCount = 0;
#endif // !FEATURE_EH_FUNCLETS

    /* Init the fgBigOffsetMorphingTemps to be BAD_VAR_NUM. */
    for (int i = 0; i < TYP_COUNT; i++)
    {
        fgBigOffsetMorphingTemps[i] = BAD_VAR_NUM;
    }

    fgNoStructPromotion      = false;
    fgNoStructParamPromotion = false;

    optValnumCSE_phase = false;   // referenced in fgMorphSmpOp()

#ifdef DEBUG
    fgNormalizeEHDone = false;
#endif // DEBUG

#ifdef DEBUG
    if (!compIsForInlining())
    {
         if ((JitConfig.JitNoStructPromotion() & 1) == 1)
         {
             fgNoStructPromotion = true;
         }
         if ((JitConfig.JitNoStructPromotion() & 2) == 2)
         {
             fgNoStructParamPromotion = true;
         }
    }
#endif // DEBUG

    if (!compIsForInlining())
    {
        m_promotedStructDeathVars = NULL;
    }
#ifdef FEATURE_SIMD
    fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
#endif
}

bool                 Compiler::fgHaveProfileData()
{
    if (compIsForInlining() || compIsForImportOnly())
        return false;

    return (fgProfileBuffer != NULL);
}

bool                 Compiler::fgGetProfileWeightForBasicBlock(IL_OFFSET offset, unsigned* weightWB)
{
    noway_assert(weightWB != NULL);
    unsigned weight = 0;

#ifdef DEBUG
    unsigned hashSeed = fgStressBBProf();
    if (hashSeed != 0)
    {
        unsigned hash = (info.compMethodHash() * hashSeed) ^ (offset * 1027);

        // We need to especially stress the procedure splitting codepath.  Therefore
        // one third the time we should return a weight of zero.
        // Otherwise we should return some random weight (usually between 0 and 288).
        // The below gives a weight of zero, 44% of the time

        if (hash % 3 == 0)
        {
            weight = 0;
        }
        else if (hash % 11 == 0)
        {
            weight = (hash % 23) * (hash % 29) * (hash % 31);
        }
        else
        {
            weight = (hash % 17) * (hash % 19);
        }

        // The first block is never given a weight of zero
        if ((offset == 0) && (weight == 0))
        {
            weight = 1 + (hash % 5);
        }

        *weightWB = weight;
        return true;
    }
#endif // DEBUG

    if (fgHaveProfileData() == false)
    {
        return false;
    }

    noway_assert(!compIsForInlining());
    for (unsigned i = 0; i < fgProfileBufferCount; i++)
    {
        if (fgProfileBuffer[i].ILOffset == offset)
        {
            weight = fgProfileBuffer[i].ExecutionCount;

            *weightWB = weight;
            return true;
        }
    }

    *weightWB = 0;
    return true;
}

void                Compiler::fgInstrumentMethod()
{
    noway_assert(!compIsForInlining());

    // Count the number of basic blocks in the method

    int countOfBlocks = 0;
    BasicBlock* block;
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        if (!(block->bbFlags & BBF_IMPORTED) || (block->bbFlags & BBF_INTERNAL))
        {
            continue;
        }
        countOfBlocks++;
    }

    // Allocate the profile buffer

    ICorJitInfo::ProfileBuffer* bbProfileBuffer;

    HRESULT res = info.compCompHnd->allocBBProfileBuffer(countOfBlocks, &bbProfileBuffer);

    ICorJitInfo::ProfileBuffer *bbProfileBufferStart = bbProfileBuffer;

    GenTreePtr stmt;

    if (!SUCCEEDED(res))
    {
        // The E_NOTIMPL status is returned when we are profiling a generic method from a different assembly
        if (res == E_NOTIMPL)
        {
           // In such cases we still want to add the method entry callback node

            GenTreeArgList* args = gtNewArgList(gtNewIconEmbMethHndNode(info.compMethodHnd));
            GenTreePtr call      = gtNewHelperCallNode(CORINFO_HELP_BBT_FCN_ENTER, TYP_VOID, 0, args);

            stmt = gtNewStmt(call);
        }
        else
        {
            noway_assert(!"Error:  failed to allocate bbProfileBuffer");
            return;
        }
    }
    else
    {
        // Assign a buffer entry for each basic block

        for (block = fgFirstBB; block; block = block->bbNext)
        {
            if (!(block->bbFlags & BBF_IMPORTED) || (block->bbFlags & BBF_INTERNAL))
            {
                continue;
            }

            bbProfileBuffer->ILOffset = block->bbCodeOffs;

            GenTreePtr  addr;
            GenTreePtr  value;

            value   = gtNewOperNode(GT_IND, TYP_INT,
                                    gtNewIconEmbHndNode((void*) &bbProfileBuffer->ExecutionCount, NULL, GTF_ICON_BBC_PTR));
            value   = gtNewOperNode(GT_ADD, TYP_INT, value, gtNewIconNode(1));

            addr    = gtNewOperNode(GT_IND, TYP_INT,
                                    gtNewIconEmbHndNode((void*) &bbProfileBuffer->ExecutionCount, NULL, GTF_ICON_BBC_PTR));

            addr    = gtNewAssignNode(addr, value);

            fgInsertStmtAtBeg(block, addr);

            countOfBlocks--;
            bbProfileBuffer++;
        }
        noway_assert(countOfBlocks == 0);

        // Add the method entry callback node

        GenTreeArgList* args = gtNewArgList( gtNewIconEmbMethHndNode(info.compMethodHnd));
        GenTreePtr call   = gtNewHelperCallNode(CORINFO_HELP_BBT_FCN_ENTER, TYP_VOID, 0, args);

        GenTreePtr handle = gtNewIconEmbHndNode((void*) &bbProfileBufferStart->ExecutionCount, NULL, GTF_ICON_BBC_PTR);
        GenTreePtr value  = gtNewOperNode(GT_IND, TYP_INT, handle);
        GenTreePtr relop  = gtNewOperNode(GT_NE, TYP_INT, value, gtNewIconNode(0, TYP_INT));
        relop->gtFlags   |= GTF_RELOP_QMARK;
        GenTreePtr colon  = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), call);
        GenTreePtr cond   = gtNewQmarkNode(TYP_VOID, relop, colon);
        stmt = gtNewStmt(cond);
    }

    fgEnsureFirstBBisScratch();

    fgInsertStmtAtEnd(fgFirstBB, stmt);
}

/*****************************************************************************
 *
 *  Create a basic block and append it to the current BB list.
 */

BasicBlock *        Compiler::fgNewBasicBlock(BBjumpKinds jumpKind)
{
    // This method must not be called after the exception table has been
    // constructed, because it doesn't not provide support for patching
    // the exception table.

    noway_assert(compHndBBtabCount == 0);

    BasicBlock* block;

    /* Allocate the block descriptor */

    block = bbNewBasicBlock(jumpKind);
    noway_assert(block->bbJumpKind == jumpKind);

    /* Append the block to the end of the global basic block list */

    if  (fgFirstBB)
    {
        fgLastBB->setNext(block);
    }
    else
    {
        fgFirstBB        = block;
        block->bbPrev = nullptr;
    }

    fgLastBB = block;

    return block;

}

/*****************************************************************************
 *
 *  Ensures that fgFirstBB is a scratch BasicBlock that we have added.
 *  This can be used to add initialization code (without worrying
 *  about other blocks jumping to it).
 *
 *  Callers have to be careful that they do not mess up the order of things
 *  added to fgEnsureFirstBBisScratch in a way as to change semantics.
 */

void                Compiler::fgEnsureFirstBBisScratch()
{
    // Have we already allocated a scratch block?

    if (fgFirstBBisScratch())
        return;

    assert(fgFirstBBScratch == nullptr);

    BasicBlock* block = bbNewBasicBlock(BBJ_NONE);

    if (fgFirstBB != NULL)
    {
        // If we have profile data the new block will inherit fgFirstBlock's weight
        if (fgFirstBB->bbFlags & BBF_PROF_WEIGHT)
        {
            block->inheritWeight(fgFirstBB);
        }
        fgInsertBBbefore(fgFirstBB, block);
    }
    else
    {
        noway_assert(fgLastBB == NULL);
        fgFirstBB = block;
        fgLastBB = block;
    }
   
    noway_assert(fgLastBB != NULL);

    block->bbFlags |= (BBF_INTERNAL | BBF_IMPORTED);

    fgFirstBBScratch = fgFirstBB;

#ifdef DEBUG
    if (verbose)
    {
        printf("New scratch BB%02u\n", block->bbNum);
    }
#endif
}

bool                Compiler::fgFirstBBisScratch()
{
    if (fgFirstBBScratch != nullptr)
    {
        assert(fgFirstBBScratch == fgFirstBB);
        assert(fgFirstBBScratch->bbFlags & BBF_INTERNAL);
        assert(fgFirstBBScratch->countOfInEdges() == 1);

        // Normally, the first scratch block is a fall-through block. However, if the block after it was an empty
        // BBJ_ALWAYS block, it might get removed, and the code that removes it will make the first scratch block
        // a BBJ_ALWAYS block.
        assert((fgFirstBBScratch->bbJumpKind == BBJ_NONE) ||
               (fgFirstBBScratch->bbJumpKind == BBJ_ALWAYS));

        return true;
    }
    else
    {
        return false;
    }
}

bool                Compiler::fgBBisScratch(BasicBlock* block)
{
    return fgFirstBBisScratch() && (block == fgFirstBB);
}

#ifdef DEBUG
// Check to see if block contains a statement but don't spend more than a certain
// budget doing this per method compiled.
// If the budget is exceeded, return 'answerOnBoundExceeded' as the answer.
/* static */
bool Compiler::fgBlockContainsStatementBounded(BasicBlock* block, GenTree* stmt, bool answerOnBoundExceeded /*= true*/)
{
    const __int64 maxLinks = 1000000000;

    assert(stmt->gtOper == GT_STMT);

    __int64 *numTraversed = &GetTlsCompiler()->compNumStatementLinksTraversed;

    if (*numTraversed > maxLinks)
        return answerOnBoundExceeded;

    GenTree* curr = block->firstStmt();
    do
    {
        (*numTraversed)++;
        if (curr == stmt)
            break;
        curr = curr->gtNext;
    }
    while (curr);
    return curr != NULL;
}
#endif // DEBUG

//------------------------------------------------------------------------
// fgInsertStmtAtBeg: Insert the given tree or statement at the start of the given basic block.
//
// Arguments:
//    block     - The block into which 'stmt' will be inserted.
//    stmt      - The statement to be inserted.
//
// Return Value:
//    Returns the new (potentially) GT_STMT node.
//
// Notes:
//    If 'stmt' is not already a statement, a new statement is created from it.
//    We always insert phi statements at the beginning.
//    In other cases, if there are any phi assignments and/or an assignment of
//    the GT_CATCH_ARG, we insert after those.

GenTreePtr          Compiler::fgInsertStmtAtBeg(BasicBlock* block,
                                                GenTreePtr  stmt)
{
    if (stmt->gtOper != GT_STMT)
    {
        stmt = gtNewStmt(stmt);
    }

    GenTreePtr list = block->firstStmt();

    if (!stmt->IsPhiDefnStmt())
    {
        GenTreePtr insertBeforeStmt = block->FirstNonPhiDefOrCatchArgAsg();
        if (insertBeforeStmt != nullptr)
        {
            return fgInsertStmtBefore(block, insertBeforeStmt, stmt);
        }
        else if (list != nullptr)
        {
            return fgInsertStmtAtEnd(block, stmt);
        }
        // Otherwise, we will simply insert at the beginning, below.
    }

    /* The new tree will now be the first one of the block */

    block->bbTreeList = stmt;
    stmt->gtNext      = list;

    /* Are there any statements in the block? */

    if  (list)
    {
        GenTreePtr last;

        /* There is at least one statement already */

        last = list->gtPrev;
        noway_assert(last && last->gtNext == nullptr);

        /* Insert the statement in front of the first one */

        list->gtPrev  = stmt;
        stmt->gtPrev  = last;
    }
    else
    {
        /* The block was completely empty */

        stmt->gtPrev  = stmt;
    }

    return stmt;
}

/*****************************************************************************
 *
 *  Insert the given tree or statement at the end of the given basic block.
 *  Returns the (potentially) new GT_STMT node.
 *  If the block can be a conditional block, use fgInsertStmtNearEnd.
 */

GenTreeStmt*        Compiler::fgInsertStmtAtEnd(BasicBlock* block,
                                                GenTreePtr  node)
{
    GenTreePtr      list = block->firstStmt();
    GenTreeStmt*    stmt;
    
    if (node->gtOper != GT_STMT)
    {
        stmt = gtNewStmt(node);
    }
    else
    {
        stmt = node->AsStmt();
    }

    assert(stmt->gtNext == nullptr); // We don't set it, and it needs to be this after the insert

    if  (list)
    {
        GenTreePtr last;

        /* There is at least one statement already */

        last = list->gtPrev;
        noway_assert(last && last->gtNext == nullptr);

        /* Append the statement after the last one */

        last->gtNext = stmt;
        stmt->gtPrev = last;
        list->gtPrev = stmt;
    }
    else
    {
        /* The block is completely empty */

        block->bbTreeList = stmt;
        stmt->gtPrev      = stmt;
    }

    return stmt;
}

/*****************************************************************************
 *
 *  Insert the given tree or statement at the end of the given basic block, but before
 *  the GT_JTRUE, if present.
 *  Returns the (potentially) new GT_STMT node.
 */

GenTreeStmt*  Compiler::fgInsertStmtNearEnd(BasicBlock* block, GenTreePtr node)
{
    GenTreeStmt* stmt;
    
    // This routine is not aware of embedded stmts and can only be used when in tree order.
    assert(fgOrder == FGOrderTree);

    if ((block->bbJumpKind == BBJ_COND)   ||
        (block->bbJumpKind == BBJ_SWITCH) ||
        (block->bbJumpKind == BBJ_RETURN))
    {
        if (node->gtOper != GT_STMT)
        {
            stmt = gtNewStmt(node);
        }
        else
        {
            stmt = node->AsStmt();
        }

        GenTreeStmt* first = block->firstStmt(); noway_assert(first);
        GenTreeStmt* last  = block->lastStmt();  noway_assert(last && last->gtNext == NULL);
        GenTreePtr   after = last->gtPrev;

#if DEBUG
        if (block->bbJumpKind == BBJ_COND)
        {
            noway_assert(last->gtStmtExpr->gtOper == GT_JTRUE);
        }
        else if (block->bbJumpKind == BBJ_RETURN)
        {
            noway_assert((last->gtStmtExpr->gtOper == GT_RETURN) ||
                         (last->gtStmtExpr->gtOper == GT_JMP) ||
                         // BBJ_RETURN blocks in functions returning void do not get a GT_RETURN node if they
                         // have a .tail prefix (even if canTailCall returns false for these calls)
                         // code:Compiler::impImportBlockCode (search for the RET: label)
                         // Ditto for real tail calls (all code after them has been removed)
                         ((last->gtStmtExpr->gtOper == GT_CALL) &&
                          ((info.compRetType == TYP_VOID) || last->gtStmtExpr->AsCall()->IsTailCall()))
                        );
        }
        else
        {
            noway_assert(block->bbJumpKind == BBJ_SWITCH);
            noway_assert(last->gtStmtExpr->gtOper == GT_SWITCH);
        }
#endif // DEBUG

        /* Append 'stmt' before 'last' */

        stmt->gtNext = last;
        last->gtPrev = stmt;

        if (first == last)
        {
            /* There is only one stmt in the block */

            block->bbTreeList = stmt;
            stmt->gtPrev      = last;
        }
        else
        {
            noway_assert(after && (after->gtNext == last));

            /* Append 'stmt' after 'after' */

            after->gtNext = stmt;
            stmt->gtPrev  = after;
        }

        return stmt;
    }
    else
    {
        return fgInsertStmtAtEnd(block, node);
    }
}


/*****************************************************************************
 *
 *  Insert the given statement "stmt" after GT_STMT node "insertionPoint".
 *  Returns the newly inserted GT_STMT node.
 *  Note that the gtPrev list of statment nodes is circular, but the gtNext list is not.
 */

GenTreePtr          Compiler::fgInsertStmtAfter(BasicBlock* block,
                                                GenTreePtr  insertionPoint,
                                                GenTreePtr  stmt)
{
    assert(block->bbTreeList != nullptr);
    noway_assert(insertionPoint->gtOper == GT_STMT);
    noway_assert(stmt->gtOper == GT_STMT);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint->gtNext == nullptr)
    {
        // Ok, we want to insert after the last statement of the block.
        stmt->gtNext = nullptr;
        stmt->gtPrev = insertionPoint;

        insertionPoint->gtNext = stmt;

        // Update the backward link of the first statement of the block
        // to point to the new last statement.
        assert(block->bbTreeList->gtPrev == insertionPoint);
        block->bbTreeList->gtPrev = stmt;
    }
    else
    {
        stmt->gtNext = insertionPoint->gtNext;
        stmt->gtPrev = insertionPoint;

        insertionPoint->gtNext->gtPrev = stmt;
        insertionPoint->gtNext         = stmt;
    }

    return stmt;
}

//  Insert the given tree or statement before GT_STMT node "insertionPoint".
//  Returns the newly inserted GT_STMT node.

GenTreePtr          Compiler::fgInsertStmtBefore(BasicBlock* block,
                                                 GenTreePtr  insertionPoint,
                                                 GenTreePtr  stmt)
{
    assert(block->bbTreeList != nullptr);
    noway_assert(insertionPoint->gtOper == GT_STMT);
    noway_assert(stmt->gtOper == GT_STMT);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint == block->bbTreeList)
    {
        // We're inserting before the first statement in the block.
        GenTreePtr list = block->bbTreeList;
        GenTreePtr last = list->gtPrev;

        stmt->gtNext = list;
        stmt->gtPrev = last;

        block->bbTreeList = stmt;
        list->gtPrev      = stmt;
    }
    else
    {
        stmt->gtNext = insertionPoint;
        stmt->gtPrev = insertionPoint->gtPrev;

        insertionPoint->gtPrev->gtNext  = stmt;
        insertionPoint->gtPrev          = stmt;
    }

    return stmt;
}

/*****************************************************************************
 *
 *  Insert the list of statements stmtList after the stmtAfter in block.
 *  Return the last statement stmtList.
 */

GenTreePtr          Compiler::fgInsertStmtListAfter(BasicBlock * block,     // the block where stmtAfter is in.
                                                    GenTreePtr   stmtAfter, // the statement where stmtList should be inserted after.
                                                    GenTreePtr   stmtList)
{
    // Currently we can handle when stmtAfter and stmtList are non-NULL. This makes everything easy.
    noway_assert(stmtAfter && stmtAfter->gtOper == GT_STMT);
    noway_assert(stmtList  && stmtList->gtOper  == GT_STMT);

    GenTreePtr stmtLast = stmtList->gtPrev; // Last statement in a non-empty list, circular in the gtPrev list.
    noway_assert(stmtLast);
    noway_assert(stmtLast->gtNext == NULL);

    GenTreePtr stmtNext = stmtAfter->gtNext;

    if (!stmtNext)
    {
        stmtAfter->gtNext = stmtList;
        stmtList->gtPrev  = stmtAfter;
        block->bbTreeList->gtPrev = stmtLast;
        goto _Done;
    }

    stmtAfter->gtNext = stmtList;
    stmtList->gtPrev  = stmtAfter;

    stmtLast->gtNext  = stmtNext;
    stmtNext->gtPrev  = stmtLast;

_Done:

    noway_assert(block->bbTreeList == NULL ||
                 block->bbTreeList->gtPrev->gtNext == NULL);

    return stmtLast;
}

/*
    Removes a block from the return block list
*/
void                Compiler::fgRemoveReturnBlock(BasicBlock* block)
{
    if (fgReturnBlocks == nullptr)
        return;

    if (fgReturnBlocks->block == block)
    {
        // It's the 1st entry, assign new head of list.
        fgReturnBlocks = fgReturnBlocks->next;
        return;
    }

    for (BasicBlockList* retBlocks = fgReturnBlocks; retBlocks->next != nullptr; retBlocks = retBlocks->next)
    {
        if (retBlocks->next->block == block)
        {
            // Found it; splice it out.
            retBlocks->next = retBlocks->next->next;
            return;
        }
    }
}

//------------------------------------------------------------------------
// fgReplaceStmt: Replaces the top-level tree of 'stmt' with newTree
//
// Arguments:
//    stmt    - the statement whose tree we're replacing
//    newTree - the new top-level tree for 'stmt'
//
// Return Value:
//    None.
//
// Operation:
//    This method has two main modes of operation:
//    a) In case we're in Tree Order or we're replacing a top-level statement
//       we first append the replacing statement ahead of the statement to replace
//       and then remove the latter from the CFG.
//    b) If we're replacing an embedded statement (and this naturally assumes we're
//       in linear order), we proceed to do that in-place, i.e. replace the expression
//       inside the statement to replace with the expression contained in the
//       replacing node.
//
// Assumptions:
//    This method will "fixup" any embedded statements from the old tree
//    to the new.  However, this will only work if the node which follows the
//    embedded statement is preserved.  This will be true if the newTree
//    reuses the constituent nodes of the old tree (e.g. in the case where a
//    node is replaced by a helper call with the original arguments to the node,
//    but will not be true for arbitrary tree replacement.)
//
// Notes:
//    This is currently only used in FGOrderLinear.
//    TODO-Cleanup: This should probably simply replace the tree so that the information
//    (such as IL offsets) is preserved, but currently it creates a new statement.

void                Compiler::fgReplaceStmt(BasicBlock* block, GenTreeStmt* stmt, GenTreePtr newTree)
{
    // fgNewStmtFromTree will sequence the nodes in newTree.  Thus, if we are in FGOrderLinear,
    // we will need to fixup any embedded statements after this call.
    GenTreeStmt* newStmt = fgNewStmtFromTree(newTree, block);

    if (stmt->gtStmtIsTopLevel() || fgOrder == FGOrderTree)
    {
        assert(stmt->gtStmtIsTopLevel());
        fgInsertStmtAfter(block, stmt, newStmt);

        // Remove the old statement now we've inserted the new one.
        fgRemoveStmt(block, stmt, false);

        if (fgOrder == FGOrderLinear)
        {
            // Because we are now in linear mode, we may have an embedded statement in the execution
            // stream.  It is too complex to try to sequence the new tree in an ad-hoc fashion,
            // but we can't use the normal sequencing without bypassing the embedded statements.
            // So, we fix them up now that we're done with the new tree.
            // We preserve the order of the embedded statement relative to its gtNext.
            // This is because the new tree may have a different order for its args than the
            // block node did, and statements become embedded because they need to be ordered
            // BEFORE something (not after).
            // TODO-Cleanup: Consider finding an alternate approach to this - it seems risky

            for (GenTreeStmt* embeddedStmt = newStmt->gtNextStmt;
                 embeddedStmt != nullptr && embeddedStmt->gtStmtIsEmbedded();
                 embeddedStmt = embeddedStmt->gtNextStmt)
            {
                GenTreePtr firstEmbeddedNode = embeddedStmt->gtStmtList;
                GenTreePtr lastEmbeddedNode  = embeddedStmt->gtStmtExpr;
                GenTreePtr nextNode = lastEmbeddedNode->gtNext;
                GenTreePtr prevNode = nextNode->gtPrev;
                assert(nextNode != nullptr);
                if (prevNode == nullptr)
                {
                    // We've reordered the nodes such that the embedded statement is now first.
                    // Extract it.
                    firstEmbeddedNode->gtPrev = nullptr;
                    lastEmbeddedNode->gtNext = nullptr;
                    fgRemoveStmt(block, embeddedStmt);
                    fgInsertStmtBefore(block, stmt, embeddedStmt);
                    embeddedStmt->gtFlags |= GTF_STMT_TOP_LEVEL;
                }
                else
                {
                    prevNode->gtNext = firstEmbeddedNode;
                    firstEmbeddedNode->gtPrev = prevNode;
                    nextNode->gtPrev = lastEmbeddedNode;
                    lastEmbeddedNode->gtNext = nextNode;
                }
            }
        }
    }
    else
    {
        assert(fgOrder == FGOrderLinear);

        GenTreePtr stmtExpr = stmt->gtStmtExpr;
        GenTreePtr stmtList = stmt->gtStmtList;

        // First, proceed to wire the first node in
        // execution order
        if (stmtList->gtPrev != nullptr)
        {
            stmtList->gtPrev->gtNext = newStmt->gtStmtList;
        }
        newStmt->gtStmtList->gtPrev = stmtList->gtPrev;

        // Now, in order to wire the last execution order node
        // in a statement, in case it's embedded, we have a special case
        // since it *cannot* be null, its gtNext is connected to the
        // 'resuming' next node in the containing statement.
        // For this, we have to search for the last node in the
        // newly created statement and wire it in accordingly to the
        // rule just mentioned.

        assert(newStmt->gtStmtExpr->gtNext == nullptr);

        if (stmtExpr->gtNext != nullptr)
        {
            stmtExpr->gtNext->gtPrev = newStmt->gtStmtExpr;
        }
        newStmt->gtStmtExpr->gtNext = stmtExpr->gtNext;

        stmt->gtStmtExpr = newStmt->gtStmtExpr;
        stmt->gtStmtList = newStmt->gtStmtList;

#ifdef DEBUG
        fgDebugCheckNodeLinks(compCurBB, stmt);
#endif // DEBUG
    }
}


//------------------------------------------------------------------------
// fgGetPredForBlock: Find and return the predecessor edge corresponding to a given predecessor block.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to find in the predecessor list.
//
// Return Value:
//    The flowList edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.

flowList*           Compiler::fgGetPredForBlock(BasicBlock* block,
                                                BasicBlock* blockPred)
{
    noway_assert(block);
    noway_assert(blockPred);
    assert(!fgCheapPredsValid);

    flowList* pred;

    for (pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        if (blockPred == pred->flBlock)
        {
            return pred;
        }
    }

    return nullptr;
}


//------------------------------------------------------------------------
// fgGetPredForBlock: Find and return the predecessor edge corresponding to a given predecessor block.
// Also returns the address of the pointer that points to this edge, to make it possible to remove this edge from the
// predecessor list without doing another linear search over the edge list.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to find in the predecessor list.
//    ptrToPred -- Out parameter: set to the address of the pointer that points to the returned predecessor edge.
//
// Return Value:
//    The flowList edge corresponding to "blockPred". If "blockPred" is not in the predecessor list of "block",
//    then returns nullptr.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.

flowList*           Compiler::fgGetPredForBlock(BasicBlock* block,
                                                BasicBlock* blockPred,
                                                flowList*** ptrToPred)
{
    assert(block);
    assert(blockPred);
    assert(ptrToPred);
    assert(!fgCheapPredsValid);

    flowList** predPrevAddr;
    flowList* pred;

    for (predPrevAddr = &block->bbPreds, pred = *predPrevAddr; pred != nullptr; predPrevAddr = &pred->flNext, pred = *predPrevAddr)
    {
        if (blockPred == pred->flBlock)
        {
            *ptrToPred = predPrevAddr;
            return pred;
        }
    }

    *ptrToPred = nullptr;
    return nullptr;
}


//------------------------------------------------------------------------
// fgSpliceOutPred: Removes a predecessor edge for a block from the predecessor list.
//
// Arguments:
//    block -- The block with the predecessor list to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    The flowList edge that was removed.
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This simply splices out the flowList object. It doesn't update block ref counts, handle duplicate counts, etc.
//       For that, use fgRemoveRefPred() or fgRemoveAllRefPred().
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- This must walk the predecessor list to find the block in question. If the predecessor edge
//       is found using fgGetPredForBlock(), consider using the version that hands back the predecessor pointer
//       address instead, to avoid this search.
//    -- Marks fgModified = true, since the flow graph has changed.

flowList*           Compiler::fgSpliceOutPred(BasicBlock* block,
                                              BasicBlock* blockPred)
{
    assert(!fgCheapPredsValid);
    noway_assert(block->bbPreds);

    flowList* oldEdge = nullptr;

    // Is this the first block in the pred list?
    if  (blockPred == block->bbPreds->flBlock)
    {
        oldEdge = block->bbPreds;
        block->bbPreds = block->bbPreds->flNext;
    }
    else
    {
        flowList* pred;
        for (pred = block->bbPreds;
             (pred->flNext != nullptr) && (blockPred != pred->flNext->flBlock);
             pred = pred->flNext)
        {
            // empty
        }
        oldEdge = pred->flNext;
        if (oldEdge == nullptr)
        {
            noway_assert(!"Should always find the blockPred");
        }
        pred->flNext = pred->flNext->flNext;
    }

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return oldEdge;
}


//------------------------------------------------------------------------
// fgAddRefPred: Increment block->bbRefs by one and add "blockPred" to the predecessor list of "block".
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to add to the predecessor list.
//    oldEdge -- Optional (default: nullptr). If non-nullptr, and a new edge is created (and the dup count
//               of an existing edge is not just incremented), the edge weights are copied from this edge.
//    initializingPreds -- Optional (default: false). Only set to "true" when the initial preds computation is happening.
//
// Return Value:
//    The flow edge representing the predecessor.
//
// Assumptions:
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- block->bbRefs is incremented by one to account for the reduction in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a new flow edge is created (but not if an existing flow edge dup count is incremented),
//       indicating that the flow graph shape has changed.

flowList*           Compiler::fgAddRefPred(BasicBlock* block,
                                           BasicBlock* blockPred,
                                           flowList*   oldEdge /* = nullptr */,
                                           bool        initializingPreds /* = false */)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);

    block->bbRefs++;

    if (!fgComputePredsDone && !initializingPreds)
    {
        // Why is someone trying to update the preds list when the preds haven't been created?
        // Ignore them! This can happen when fgMorph is called before the preds list is created.
        return nullptr;
    }

    assert(!fgCheapPredsValid);

    flowList* flow = fgGetPredForBlock(block, blockPred);

    if (flow)
    {
        noway_assert(flow->flDupCount > 0);
        flow->flDupCount++;
    }
    else
    {
        flow = new (this, CMK_FlowList) flowList();

#if MEASURE_BLOCK_SIZE
        genFlowNodeCnt  += 1;
        genFlowNodeSize += sizeof(flowList);
#endif // MEASURE_BLOCK_SIZE

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;

        // Keep the predecessor list in lowest to highest bbNum order
        // This allows us to discover the loops in optFindNaturalLoops
        //  from innermost to outermost.

        // TODO-Throughput: This search is quadratic if you have many jumps
        // to the same target.   We need to either not bother sorting for
        // debuggable code, or sort in optFindNaturalLoops, or better, make
        // the code in optFindNaturalLoops not depend on order.

        flowList** listp= &block->bbPreds;
        while (*listp && ((*listp)->flBlock->bbNum < blockPred->bbNum))
        {
            listp = & (*listp)->flNext;
        }

        flow->flNext  = *listp;
        *listp        = flow;

        flow->flBlock = blockPred;
        flow->flDupCount = 1;

        if (fgHaveValidEdgeWeights)
        {
            // We are creating an edge from blockPred to block
            // and we have already computed the edge weights, so
            // we will try to setup this new edge with valid edge weights.
            //
            if (oldEdge != nullptr)
            {
                // If our caller has given us the old edge weights
                // then we will use them.
                //
                flow->flEdgeWeightMin = oldEdge->flEdgeWeightMin;
                flow->flEdgeWeightMax = oldEdge->flEdgeWeightMax;
            }
            else
            {
                // Set the max edge weight to be the minimum of block's or blockPred's weight
                //
                flow->flEdgeWeightMax = min(block->bbWeight, blockPred->bbWeight);

                // If we are inserting a conditional block the minimum weight is zero,
                // otherwise it is the same as the edge's max weight.
                if (blockPred->NumSucc() > 1)
                {
                    flow->flEdgeWeightMin = BB_ZERO_WEIGHT;
                }
                else
                {
                    flow->flEdgeWeightMin = flow->flEdgeWeightMax;
                }
            }
        }
        else
        {
            flow->flEdgeWeightMin = BB_ZERO_WEIGHT;
            flow->flEdgeWeightMax = BB_MAX_WEIGHT;
        }
    }
    return flow;
}


//------------------------------------------------------------------------
// fgRemoveRefPred: Decrements the reference count of a predecessor edge from "blockPred" to "block",
// removing the edge if it is no longer necessary.
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    If the flow edge was removed (the predecessor has a "dup count" of 1),
//        returns the flow graph edge that was removed. This means "blockPred" is no longer a predecessor of "block".
//    Otherwise, returns nullptr. This means that "blockPred" is still a predecessor of "block" (because "blockPred"
//        is a switch with multiple cases jumping to "block", or a BBJ_COND with both conditional and fall-through
//        paths leading to "block").
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    -- block->bbRefs is decremented by one to account for the reduction in incoming edges.
//    -- block->bbRefs is adjusted even if preds haven't been computed. If preds haven't been computed,
//       the preds themselves aren't touched.
//    -- fgModified is set if a flow edge is removed (but not if an existing flow edge dup count is decremented),
//       indicating that the flow graph shape has changed.

flowList*           Compiler::fgRemoveRefPred(BasicBlock* block,
                                              BasicBlock* blockPred)
{
    noway_assert(block != nullptr);
    noway_assert(blockPred != nullptr);

    noway_assert(block->countOfInEdges() > 0);
    block->bbRefs--;

    // Do nothing if we haven't calculated the predecessor list yet.
    // Yes, this does happen.
    // For example the predecessor lists haven't been created yet when we do fgMorph.
    // But fgMorph calls fgFoldConditional, which in turn calls fgRemoveRefPred.
    if (!fgComputePredsDone)
    {
        return nullptr;
    }

    assert(!fgCheapPredsValid);

    flowList** ptrToPred;
    flowList* pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    noway_assert(pred);
    noway_assert(pred->flDupCount > 0);

    pred->flDupCount--;

    if (pred->flDupCount == 0)
    {
        // Splice out the predecessor edge since it's no longer necessary.
        *ptrToPred = pred->flNext;

        // Any changes to the flow graph invalidate the dominator sets.
        fgModified = true;

        return pred;
    }
    else
    {
        return nullptr;
    }
}


//------------------------------------------------------------------------
// fgRemoveAllRefPreds: Removes a predecessor edge from one block to another, no matter what the "dup count" is.
//
// Arguments:
//    block -- A block to operate on.
//    blockPred -- The predecessor block to remove from the predecessor list. It must be a predecessor of "block".
//
// Return Value:
//    Returns the flow graph edge that was removed. The dup count on the edge is no longer valid.
//
// Assumptions:
//    -- "blockPred" must be a predecessor block of "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    block->bbRefs is decremented to account for the reduction in incoming edges.

flowList*           Compiler::fgRemoveAllRefPreds(BasicBlock* block,
                                                  BasicBlock* blockPred)
{
    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);
    assert(block->countOfInEdges() > 0);

    flowList** ptrToPred;
    flowList* pred = fgGetPredForBlock(block, blockPred, &ptrToPred);
    assert(pred != nullptr);
    assert(pred->flDupCount > 0);

    assert(block->bbRefs >= pred->flDupCount);
    block->bbRefs -= pred->flDupCount;

    // Now splice out the predecessor edge.
    *ptrToPred = pred->flNext;

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return pred;
}


//------------------------------------------------------------------------
// fgRemoveAllRefPreds: Remove a predecessor edge, given the address of a pointer to it in the
// predecessor list, no matter what the "dup count" is.
//
// Arguments:
//    block -- A block with the predecessor list to operate on.
//    ptrToPred -- The address of a pointer to the predecessor to remove.
//
// Return Value:
//    The removed predecessor edge. The dup count on the edge is no longer valid.
//
// Assumptions:
//    -- The predecessor edge must be in the predecessor list for "block".
//    -- This only works on the full predecessor lists, not the cheap preds lists.
//
// Notes:
//    block->bbRefs is decremented by the dup count of the predecessor edge, to account for the reduction in incoming edges.

flowList*           Compiler::fgRemoveAllRefPreds(BasicBlock* block,
                                                  flowList** ptrToPred)
{
    assert(block != nullptr);
    assert(ptrToPred != nullptr);
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);
    assert(block->countOfInEdges() > 0);

    flowList* pred = *ptrToPred;
    assert(pred != nullptr);
    assert(pred->flDupCount > 0);

    assert(block->bbRefs >= pred->flDupCount);
    block->bbRefs -= pred->flDupCount;

    // Now splice out the predecessor edge.
    *ptrToPred = pred->flNext;

    // Any changes to the flow graph invalidate the dominator sets.
    fgModified = true;

    return pred;
}

/*
    Removes all the appearances of block as predecessor of others
*/

void                Compiler::fgRemoveBlockAsPred(BasicBlock* block)
{
    assert(!fgCheapPredsValid);

    PREFIX_ASSUME(block != nullptr);

    BasicBlock* bNext;

    switch (block->bbJumpKind)
    {
    case BBJ_CALLFINALLY:
        if (!(block->bbFlags & BBF_RETLESS_CALL))
        {
            assert(block->isBBCallAlwaysPair());

            /* The block after the BBJ_CALLFINALLY block is not reachable */
            bNext = block->bbNext;

            /* bNext is an unreachable BBJ_ALWAYS block */
            noway_assert(bNext->bbJumpKind == BBJ_ALWAYS);

            while (bNext->countOfInEdges() > 0)
            {
                fgRemoveRefPred(bNext, bNext->bbPreds->flBlock);
            }
        }

        __fallthrough;

    case BBJ_COND:
    case BBJ_ALWAYS:
    case BBJ_EHCATCHRET:

        /* Update the predecessor list for 'block->bbJumpDest' and 'block->bbNext' */
        fgRemoveRefPred(block->bbJumpDest, block);

        if (block->bbJumpKind != BBJ_COND)
            break;

        /* If BBJ_COND fall through */
        __fallthrough;

    case BBJ_NONE:

        /* Update the predecessor list for 'block->bbNext' */
        fgRemoveRefPred(block->bbNext, block);
        break;

    case BBJ_EHFILTERRET:

        block->bbJumpDest->bbRefs++;  // To compensate the bbRefs-- inside fgRemoveRefPred
        fgRemoveRefPred(block->bbJumpDest, block);
        break;

    case BBJ_EHFINALLYRET:
        {
            /* Remove block as the predecessor of the bbNext of all
               BBJ_CALLFINALLY blocks calling this finally. No need
               to look for BBJ_CALLFINALLY for fault handlers. */

            unsigned hndIndex = block->getHndIndex();
            EHblkDsc* ehDsc = ehGetDsc(hndIndex);

            if (ehDsc->HasFinallyHandler())
            {
                BasicBlock* begBlk;
                BasicBlock* endBlk;
                ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                BasicBlock* finBeg = ehDsc->ebdHndBeg;

                for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                {
                    if  ((bcall->bbFlags & BBF_REMOVED) ||
                          bcall->bbJumpKind != BBJ_CALLFINALLY ||
                          bcall->bbJumpDest != finBeg)
                    {
                        continue;
                    }

                    assert(bcall->isBBCallAlwaysPair());
                    fgRemoveRefPred(bcall->bbNext, block);
                }
            }
        }
        break;

    case BBJ_THROW:
    case BBJ_RETURN:
        break;

    case BBJ_SWITCH:
    {
        unsigned        jumpCnt = block->bbJumpSwt->bbsCount;
        BasicBlock**    jumpTab = block->bbJumpSwt->bbsDstTab;

        do
        {
            fgRemoveRefPred(*jumpTab, block);
        }
        while (++jumpTab, --jumpCnt);

        break;
    }

    default:
        noway_assert(!"Block doesn't have a valid bbJumpKind!!!!");
        break;
    }
}

/*****************************************************************************
 * fgChangeSwitchBlock:
 *
 * We have a BBJ_SWITCH jump at 'oldSwitchBlock' and we want to move this
 * switch jump over to 'newSwitchBlock'.  All of the blocks that are jumped
 * to from jumpTab[] need to have their predecessor lists updated by removing
 * the 'oldSwitchBlock' and adding 'newSwitchBlock'.
 */

void                Compiler::fgChangeSwitchBlock(BasicBlock* oldSwitchBlock,
                                                  BasicBlock* newSwitchBlock)
{
    noway_assert(oldSwitchBlock != nullptr);
    noway_assert(newSwitchBlock != nullptr);
    noway_assert(oldSwitchBlock->bbJumpKind == BBJ_SWITCH);

    unsigned        jumpCnt = oldSwitchBlock->bbJumpSwt->bbsCount;
    BasicBlock**    jumpTab = oldSwitchBlock->bbJumpSwt->bbsDstTab;

    unsigned i;

    // Walk the switch's jump table, updating the predecessor for each branch.
    for (i = 0; i < jumpCnt; i++)
    {
        BasicBlock* bJump = jumpTab[i];
        noway_assert(bJump != nullptr);

        // Note that if there are duplicate branch targets in the switch jump table,
        // fgRemoveRefPred()/fgAddRefPred() will do the right thing: the second and
        // subsequent duplicates will simply subtract from and add to the duplicate
        // count (respectively).

        //
        // Remove the old edge [oldSwitchBlock => bJump]
        //
        fgRemoveRefPred(bJump, oldSwitchBlock);

        //
        // Create the new edge [newSwitchBlock => bJump]
        //
        fgAddRefPred(bJump, newSwitchBlock);
    }
    
    if (m_switchDescMap != nullptr)
    {
        SwitchUniqueSuccSet uniqueSuccSet;

        // If already computed and cached the unique descriptors for the old block, let's
        // update those for the new block.
        if (m_switchDescMap->Lookup(oldSwitchBlock, &uniqueSuccSet))
        {
            m_switchDescMap->Set(newSwitchBlock, uniqueSuccSet);
        }
        else
        {
            fgInvalidateSwitchDescMapEntry(newSwitchBlock);
        }
        fgInvalidateSwitchDescMapEntry(oldSwitchBlock);
    }
}

/*****************************************************************************
 * fgReplaceSwitchJumpTarget:
 *
 * We have a BBJ_SWITCH at 'blockSwitch' and we want to replace all entries
 * in the jumpTab[] such that so that jumps that previously went to
 * 'oldTarget' now go to 'newTarget'.
 * We also must update the predecessor lists for 'oldTarget' and 'newPred'.
 */

void                Compiler::fgReplaceSwitchJumpTarget(BasicBlock* blockSwitch,
                                                        BasicBlock* newTarget,
                                                        BasicBlock* oldTarget)
{
    noway_assert(blockSwitch != nullptr);
    noway_assert(newTarget   != nullptr);
    noway_assert(oldTarget   != nullptr);
    noway_assert(blockSwitch->bbJumpKind == BBJ_SWITCH);

    // For the jump targets values that match oldTarget of our BBJ_SWITCH
    // replace predecessor 'blockSwitch' with 'newTarget'
    //

    unsigned        jumpCnt = blockSwitch->bbJumpSwt->bbsCount;
    BasicBlock**    jumpTab = blockSwitch->bbJumpSwt->bbsDstTab;

    unsigned i = 0;

    // Walk the switch's jump table looking for blocks to update the preds for
    while (i < jumpCnt)
    {
        if (jumpTab[i] == oldTarget)   // We will update when jumpTab[i] matches
        {
            // Remove the old edge [oldTarget from blockSwitch]
            //
            fgRemoveAllRefPreds(oldTarget, blockSwitch);

            //
            // Change the jumpTab entry to branch to the new location
            //
            jumpTab[i] = newTarget;

            //
            // Create the new edge [newTarget from blockSwitch]
            //
            flowList* newEdge = fgAddRefPred(newTarget, blockSwitch);

            // Now set the correct value of newEdge->flDupCount
            // and replace any other jumps in jumpTab[] that go to oldTarget.
            //
            i++;
            while (i < jumpCnt)
            {
                if (jumpTab[i] == oldTarget)
                {
                    //
                    // We also must update this entry in the jumpTab
                    //
                    jumpTab[i] = newTarget;
                    newTarget->bbRefs++;

                    //
                    // Increment the flDupCount
                    //
                    newEdge->flDupCount++;
                }
                i++;  // Check the next entry in jumpTab[]
            }

            // Maintain, if necessary, the set of unique targets of "block."
            UpdateSwitchTableTarget(blockSwitch, oldTarget, newTarget);

            // Make sure the new target has the proper bits set for being a branch target.
            newTarget->bbFlags |= BBF_HAS_LABEL | BBF_JMP_TARGET;

            return;  // We have replaced the jumps to oldTarget with newTarget
        }
        i++;  // Check the next entry in jumpTab[] for a match
    }
    noway_assert(!"Did not find oldTarget in jumpTab[]");
}

//------------------------------------------------------------------------
// Compiler::fgReplaceJumpTarget: For a given block, replace the target 'oldTarget' with 'newTarget'.
//
// Arguments:
//    block     - the block in which a jump target will be replaced.
//    newTarget - the new branch target of the block.
//    oldTarget - the old branch target of the block.
//
// Notes:
// 1. Only branches are changed: BBJ_ALWAYS, the non-fallthrough path of BBJ_COND, BBJ_SWITCH, etc.
//    We ignore other block types.
// 2. Only the first target found is updated. If there are multiple ways for a block
//    to reach 'oldTarget' (e.g., multiple arms of a switch), only the first one found is changed.
// 3. The predecessor lists are not changed.
// 4. The switch table "unique successor" cache is invalidated.
//
// This function is most useful early, before the full predecessor lists have been computed.
//
void                Compiler::fgReplaceJumpTarget(BasicBlock* block,
                                                  BasicBlock* newTarget,
                                                  BasicBlock* oldTarget)
{
    assert(block != nullptr);

    switch (block->bbJumpKind)
    {
    case BBJ_CALLFINALLY:
    case BBJ_COND:
    case BBJ_ALWAYS:
    case BBJ_EHCATCHRET:
    case BBJ_EHFILTERRET:
    case BBJ_LEAVE:         // This function will be called before import, so we still have BBJ_LEAVE

        if (block->bbJumpDest == oldTarget)
        {
            block->bbJumpDest = newTarget;
        }
        break;

    case BBJ_NONE:
    case BBJ_EHFINALLYRET:
    case BBJ_THROW:
    case BBJ_RETURN:
        break;

    case BBJ_SWITCH:
        unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
        BasicBlock**    jumpTab; jumpTab = block->bbJumpSwt->bbsDstTab;

        for (unsigned i = 0; i < jumpCnt; i++)
        {
            if (jumpTab[i] == oldTarget)
            {
                jumpTab[i] = newTarget;
                break;
            }
        }
        break;

    default:
        assert(!"Block doesn't have a valid bbJumpKind!!!!");
        unreached();
        break;
    }
}

/*****************************************************************************
 * Updates the predecessor list for 'block' by replacing 'oldPred' with 'newPred'.
 * Note that a block can only appear once in the preds list (for normal preds, not
 * cheap preds): if a predecessor has multiple ways to get to this block, then
 * flDupCount will be >1, but the block will still appear exactly once. Thus, this
 * function assumes that all branches from the predecessor (practically, that all
 * switch cases that target this block) are changed to branch from the new predecessor,
 * with the same dup count.
 *
 * Note that the block bbRefs is not changed, since 'block' has the same number of
 * references as before, just from a different predecessor block.
 */

void                Compiler::fgReplacePred(BasicBlock* block,
                                            BasicBlock* oldPred,
                                            BasicBlock* newPred)
{
    noway_assert(block   != nullptr);
    noway_assert(oldPred != nullptr);
    noway_assert(newPred != nullptr);
    assert(!fgCheapPredsValid);

    flowList* pred;

    for (pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        if (oldPred == pred->flBlock)
        {
            pred->flBlock = newPred;
            break;
        }
    }
}

/*****************************************************************************
 *
 *  Returns true if block b1 dominates block b2.
 */

bool                Compiler::fgDominate(BasicBlock* b1, BasicBlock* b2)
{
    noway_assert(fgDomsComputed);
    assert(!fgCheapPredsValid);

    //
    // If the fgModified flag is false then we made some modifications to
    // the flow graph, like adding a new block or changing a conditional branch
    // into an unconditional branch.
    //
    // We can continue to use the dominator and reachable information to
    // unmark loops as long as we haven't renumbered the blocks or we aren't
    // asking for information about a new block
    //

    if (b2->bbNum > fgDomBBcount)
    {
        if (b1 == b2)
            return true;

        for (flowList* pred = b2->bbPreds; pred != NULL; pred = pred->flNext)
        {
            if (!fgDominate(b1, pred->flBlock))
                return false;
        }

        return b2->bbPreds != NULL;
    }

    if (b1->bbNum > fgDomBBcount)
    {
        // if b1 is a loop preheader and Succ is its only successor, then all predecessors of
        // Succ either are b1 itself or are dominated by Succ. Under these conditions, b1
        // dominates b2 if and only if Succ dominates b2 (or if b2 == b1, but we already tested
        // for this case)
        if (b1->bbFlags & BBF_LOOP_PREHEADER)
        {
            noway_assert(b1->bbFlags & BBF_INTERNAL);
            noway_assert(b1->bbJumpKind == BBJ_NONE);
            return fgDominate(b1->bbNext, b2);
        }

        // unknown dominators; err on the safe side and return false
        return false;
    }

    /* Check if b1 dominates b2 */
    unsigned numA = b1->bbNum; noway_assert(numA <= fgDomBBcount);
    unsigned numB = b2->bbNum; noway_assert(numB <= fgDomBBcount);

    // What we want to ask here is basically if A is in the middle of the path from B to the root (the entry node)
    // in the dominator tree. Turns out that can be translated as:
    //
    //   A dom B <-> preorder(A) <= preorder(B) && postorder(A) >= postorder(B)
    //
    // where the equality holds when you ask if A dominates itself.
    bool treeDom =
        fgDomTreePreOrder[numA]  <= fgDomTreePreOrder[numB] &&
        fgDomTreePostOrder[numA] >= fgDomTreePostOrder[numB];

    return treeDom;
}

/*****************************************************************************
 *
 *  Returns true if block b1 can reach block b2.
 */

bool                Compiler::fgReachable(BasicBlock* b1, BasicBlock* b2)
{
    noway_assert(fgDomsComputed);
    assert(!fgCheapPredsValid);

    //
    // If the fgModified flag is false then we made some modifications to
    // the flow graph, like adding a new block or changing a conditional branch
    // into an unconditional branch.
    //
    // We can continue to use the dominator and reachable information to
    // unmark loops as long as we haven't renumbered the blocks or we aren't
    // asking for information about a new block
    //

    if (b2->bbNum > fgDomBBcount)
    {
        if (b1 == b2)
            return true;

        for (flowList* pred = b2->bbPreds; pred != NULL; pred = pred->flNext)
        {
            if (fgReachable(b1, pred->flBlock))
                return true;
        }

        return false;
    }

    if (b1->bbNum > fgDomBBcount)
    {
        noway_assert(b1->bbJumpKind == BBJ_NONE || b1->bbJumpKind == BBJ_ALWAYS || b1->bbJumpKind == BBJ_COND);

        if (b1->bbFallsThrough() && fgReachable(b1->bbNext, b2))
            return true;

        if (b1->bbJumpKind == BBJ_ALWAYS || b1->bbJumpKind == BBJ_COND)
            return fgReachable(b1->bbJumpDest, b2);

        return false;
    }

    /* Check if b1 can reach b2 */
    assert(fgReachabilitySetsValid);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgDomBBcount + 1);
    return BlockSetOps::IsMember(this, b2->bbReach, b1->bbNum);
}


/*****************************************************************************
 *  Update changed flow graph information.
 *
 *  If the flow graph has changed, we need to recompute various information if we want to use
 *  it again.
 */

void                Compiler::fgUpdateChangedFlowGraph()
{
    // We need to clear this so we don't hit an assert calling fgRenumberBlocks().
    fgDomsComputed = false;

    JITDUMP("\nRenumbering the basic blocks for fgUpdateChangeFlowGraph\n");
    fgRenumberBlocks();

    fgComputePreds();
    fgComputeEnterBlocksSet();
    fgComputeReachabilitySets();
    fgComputeDoms();
}

/*****************************************************************************
 *  Compute the bbReach sets.
 *
 *  This can be called to recompute the bbReach sets after the flow graph changes, such as when the
 *  number of BasicBlocks change (and thus, the BlockSet epoch changes).
 *
 *  Finally, this also sets the BBF_GC_SAFE_POINT flag on blocks.
 *
 *  Assumes the predecessor lists are correct.
 *
 *  TODO-Throughput: This algorithm consumes O(n^2) because we're using dense bitsets to
 *  represent reachability. While this yields O(1) time queries, it bloats the memory usage
 *  for large code.  We can do better if we try to approach reachability by
 *  computing the strongly connected components of the flow graph.  That way we only need
 *  linear memory to label every block with its SCC.
 */

void                Compiler::fgComputeReachabilitySets()
{
    assert(fgComputePredsDone);
    assert(!fgCheapPredsValid);

#ifdef DEBUG
    fgReachabilitySetsValid = false;
#endif // DEBUG

    BasicBlock*   block;

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // Initialize the per-block bbReach sets. (Note that we can't just call BlockSetOps::ClearD()
        // when re-running this computation, because if the epoch changes, the size and representation of the
        // sets might change).
        block->bbReach = BlockSetOps::MakeEmpty(this);

        /* Mark block as reaching itself */
        BlockSetOps::AddElemD(this, block->bbReach, block->bbNum);
    }

    /* Find the reachable blocks */
    // Also, set BBF_GC_SAFE_POINT.

    bool change;
    BlockSet BLOCKSET_INIT_NOCOPY(newReach, BlockSetOps::MakeEmpty(this));
    do
    {
        change = false;

        for (block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            BlockSetOps::Assign(this, newReach, block->bbReach);

            bool predGcSafe = (block->bbPreds != nullptr); // Do all of our predecessor blocks have a GC safe bit?

            for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* predBlock = pred->flBlock;

                /* Union the predecessor's reachability set into newReach */
                BlockSetOps::UnionD(this, newReach, predBlock->bbReach);

                if (!(predBlock->bbFlags & BBF_GC_SAFE_POINT))
                {
                    predGcSafe = false;
                }
            }

            if  (predGcSafe)
            {
                block->bbFlags |= BBF_GC_SAFE_POINT;
            }

            if (!BlockSetOps::Equal(this, newReach, block->bbReach))
            {
                BlockSetOps::Assign(this, block->bbReach, newReach);
                change = true;
            }
        }
    }
    while (change);

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nAfter computing reachability sets:\n");
        fgDispReach();
    }

    fgReachabilitySetsValid = true;
#endif // DEBUG
}

/*****************************************************************************
 *  Compute the entry blocks set.
 *
 *  Initialize fgEnterBlks to the set of blocks for which we don't have explicit control
 *  flow edges. These are the entry basic block and each of the EH handler blocks.
 *  For ARM, also include the BBJ_ALWAYS block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair,
 *  to avoid creating "retless" calls, since we need the BBJ_ALWAYS for the purpose
 *  of unwinding, even if the call doesn't return (due to an explicit throw, for example).
 */

void                Compiler::fgComputeEnterBlocksSet()
{
#ifdef DEBUG
    fgEnterBlksSetValid = false;
#endif // DEBUG

    fgEnterBlks = BlockSetOps::MakeEmpty(this);

    /* Now set the entry basic block */
    BlockSetOps::AddElemD(this, fgEnterBlks, fgFirstBB->bbNum);
    assert(fgFirstBB->bbNum == 1);

    if (compHndBBtabCount > 0)
    {
        /* Also 'or' in the handler basic blocks */
        EHblkDsc* HBtab;
        EHblkDsc* HBtabEnd;
        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
             HBtab < HBtabEnd;
             HBtab++)
        {
            if (HBtab->HasFilter())
            {
                BlockSetOps::AddElemD(this, fgEnterBlks, HBtab->ebdFilter->bbNum);
            }
            BlockSetOps::AddElemD(this, fgEnterBlks, HBtab->ebdHndBeg->bbNum);
        }
    }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    // TODO-ARM-Cleanup: The ARM code here to prevent creating retless calls by adding the BBJ_ALWAYS
    // to the enter blocks is a bit of a compromise, because sometimes the blocks are already reachable,
    // and it messes up DFS ordering to have them marked as enter block. We should prevent the
    // creation of retless calls some other way.
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbJumpKind == BBJ_CALLFINALLY)
        {
            assert(block->isBBCallAlwaysPair());

            // Don't remove the BBJ_ALWAYS block that is only here for the unwinder. It might be dead
            // if the finally is no-return, so mark it as an entry point.
            BlockSetOps::AddElemD(this, fgEnterBlks, block->bbNext->bbNum);
        }
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

#ifdef DEBUG
    if (verbose)
    {
        printf("Enter blocks: ");
        BLOCKSET_ITER_INIT(this, iter, fgEnterBlks, bbNum);
        while (iter.NextElem(this, &bbNum))
        {
            printf("BB%02u ", bbNum);
        }
        printf("\n");
    }
#endif // DEBUG

#ifdef DEBUG
    fgEnterBlksSetValid = true;
#endif // DEBUG
}

/*****************************************************************************
 *  Remove unreachable blocks.
 *
 *  Return true if any unreachable blocks were removed.
 */

bool                Compiler::fgRemoveUnreachableBlocks()
{
    assert(!fgCheapPredsValid);
    assert(fgReachabilitySetsValid);

    bool          hasLoops = false;
    bool          hasUnreachableBlocks = false;
    BasicBlock*   block;

    /* Record unreachable blocks */
    for (block  = fgFirstBB;
         block != NULL;
         block = block->bbNext)
    {
        /* Internal throw blocks are also reachable */
        if (fgIsThrowHlpBlk(block))
        {
            goto SKIP_BLOCK;
        }
        else if (block == genReturnBB)
        {
            // Don't remove statements for the genReturnBB block, as we might have special hookups there.
            // For example, <BUGNUM> in VSW 364383, </BUGNUM>
            // the profiler hookup needs to have the "void GT_RETURN" statement
            // to properly set the info.compProfilerCallback flag.
            goto SKIP_BLOCK;
        }
        else
        {
            // If any of the entry blocks can reach this block, then we skip it.
            if (!BlockSetOps::IsEmptyIntersection(this, fgEnterBlks, block->bbReach))
            {
                goto SKIP_BLOCK;
            }
        }

        // Remove all the code for the block
        fgUnreachableBlock(block);

        // Make sure that the block was marked as removed */
        noway_assert(block->bbFlags & BBF_REMOVED);

        // Some blocks mark the end of trys and catches
        // and can't be removed. We convert these into
        // empty blocks of type BBJ_THROW

        if (block->bbFlags & BBF_DONT_REMOVE)
        {
            bool bIsBBCallAlwaysPair = block->isBBCallAlwaysPair();

            /* Unmark the block as removed, */
            /* clear BBF_INTERNAL as well and set BBJ_IMPORTED */

            block->bbFlags    &= ~(BBF_REMOVED | BBF_INTERNAL | BBF_NEEDS_GCPOLL);
            block->bbFlags    |= BBF_IMPORTED;
            block->bbJumpKind  = BBJ_THROW;
            block->bbSetRunRarely();

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
            // If this is a <BBJ_CALLFINALLY, BBJ_ALWAYS> pair, we have to clear BBF_FINALLY_TARGET flag on
            // the target node (of BBJ_ALWAYS) since BBJ_CALLFINALLY node is getting converted to a BBJ_THROW.
            if (bIsBBCallAlwaysPair)
            {
                noway_assert(block->bbNext->bbJumpKind == BBJ_ALWAYS);
                fgClearFinallyTargetBit(block->bbNext->bbJumpDest);
            }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        }
        else
        {
            /* We have to call fgRemoveBlock next */
            hasUnreachableBlocks = true;
        }
        continue;

SKIP_BLOCK:;

        //if (block->isRunRarely())
        //    continue;
        if (block->bbJumpKind == BBJ_RETURN)
            continue;

        /* Set BBF_LOOP_HEAD if we have backwards branches to this block */

        unsigned blockNum = block->bbNum;
        for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            BasicBlock* predBlock = pred->flBlock;
            if (blockNum <= predBlock->bbNum)
            {
                if (predBlock->bbJumpKind == BBJ_CALLFINALLY)
                    continue;

                /* If block can reach predBlock then we have a loop head */
                if (BlockSetOps::IsMember(this, predBlock->bbReach, blockNum))
                {
                    hasLoops = true;

                    /* Set the BBF_LOOP_HEAD flag */
                    block->bbFlags |= BBF_LOOP_HEAD;
                    break;
                }
            }
        }
    }

    fgHasLoops    = hasLoops;

    if (hasUnreachableBlocks)
    {
        // Now remove the unreachable blocks
        for (block  = fgFirstBB;
             block != NULL;
             block = block->bbNext)
        {
            //  If we mark the block with BBF_REMOVED then
            //  we need to call fgRemovedBlock() on it

            if (block->bbFlags & BBF_REMOVED)
            {
                fgRemoveBlock(block, true);

                // When we have a BBJ_CALLFINALLY, BBJ_ALWAYS pair; fgRemoveBlock will remove
                // both blocks, so we must advance 1 extra place in the block list
                //
                if (block->isBBCallAlwaysPair())
                {
                    block = block->bbNext;
                }
            }
        }
    }

    return hasUnreachableBlocks;
}

/*****************************************************************************
 *
 *  Function called to compute the dominator and reachable sets.
 *
 *  Assumes the predecessor lists are computed and correct.
 */

void                Compiler::fgComputeReachability()
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In fgComputeReachability\n");

    fgVerifyHandlerTab();

    // Make sure that the predecessor lists are accurate
    assert(fgComputePredsDone);
    fgDebugCheckBBlist();
#endif // DEBUG

    /* Create a list of all BBJ_RETURN blocks. The head of the list is 'fgReturnBlocks'. */
    fgReturnBlocks = nullptr;

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // If this is a BBJ_RETURN block, add it to our list of all BBJ_RETURN blocks. This list is only
        // used to find return blocks.
        if (block->bbJumpKind == BBJ_RETURN)
        {
            fgReturnBlocks = new(this, CMK_Reachability) BasicBlockList(block, fgReturnBlocks);
        }
    }

    // Compute reachability and then delete blocks determined to be unreachable. If we delete blocks, we
    // need to loop, as that might have caused more blocks to become unreachable. This can happen in the
    // case where a call to a finally is unreachable and deleted (maybe the call to the finally is
    // preceded by a throw or an infinite loop), making the blocks following the finally unreachable.
    // However, all EH entry blocks are considered global entry blocks, causing the blocks following the
    // call to the finally to stay rooted, until a second round of reachability is done.
    // The dominator algorithm expects that all blocks can be reached from the fgEnterBlks set.
    unsigned passNum = 1;
    bool changed;
    do
    {
        // Just to be paranoid, avoid infinite loops; fall back to minopts.
        if (passNum > 10)
        {
            noway_assert(!"Too many unreachable block removal loops");
        }

        /* Walk the flow graph, reassign block numbers to keep them in ascending order */
        JITDUMP("\nRenumbering the basic blocks for fgComputeReachability pass #%u\n", passNum);
        passNum++;
        fgRenumberBlocks();
    
        //
        // Compute fgEnterBlks
        //

        fgComputeEnterBlocksSet();

        //
        // Compute bbReach
        // 

        fgComputeReachabilitySets();

        //
        // Use reachability information to delete unreachable blocks.
        // Also, determine if the flow graph has loops and set 'fgHasLoops' accordingly.
        // Set the BBF_LOOP_HEAD flag on the block target of backwards branches.
        //

        changed = fgRemoveUnreachableBlocks();

    } while (changed);

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nAfter computing reachability:\n");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }

    fgVerifyHandlerTab();
    fgDebugCheckBBlist(true);
#endif // DEBUG

    //
    // Now, compute the dominators
    //

    fgComputeDoms();
}


/** In order to be able to compute dominance, we need to first get a DFS reverse post order sort on the basic flow graph
  * for the dominance algorithm to operate correctly.  The reason why we need the DFS sort is because
  * we will build the dominance sets using the partial order induced by the DFS sorting.  With this
  * precondition not holding true, the algorithm doesn't work properly.
  */
void Compiler::fgDfsInvPostOrder()
{
    // NOTE: This algorithm only pays attention to the actual blocks. It ignores the imaginary entry block.

    // visited   :  Once we run the DFS post order sort recursive algorithm, we mark the nodes we visited to avoid
    //              backtracking.
    BlockSet BLOCKSET_INIT_NOCOPY(visited, BlockSetOps::MakeEmpty(this));

    // We begin by figuring out which basic blocks don't have incoming edges and mark them as
    // start nodes.  Later on we run the recursive algorithm for each node that we
    // mark in this step.
    BlockSet_ValRet_T startNodes = fgDomFindStartNodes();

    // Make sure fgEnterBlks are still there in startNodes, even if they participate in a loop (i.e., there is
    // an incoming edge into the block).
    assert(fgEnterBlksSetValid);

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    //
    //    BlockSetOps::UnionD(this, startNodes, fgEnterBlks);
    //
    // This causes problems on ARM, because we for BBJ_CALLFINALLY/BBJ_ALWAYS pairs, we add the BBJ_ALWAYS
    // to the enter blocks set to prevent flow graph optimizations from removing it and creating retless call finallies
    // (BBF_RETLESS_CALL). This leads to an incorrect DFS ordering in some cases, because we start the recursive walk
    // from the BBJ_ALWAYS, which is reachable from other blocks. A better solution would be to change ARM to avoid
    // creating retless calls in a different way, not by adding BBJ_ALWAYS to fgEnterBlks.
    //
    // So, let us make sure at least fgFirstBB is still there, even if it participates in a loop.
    BlockSetOps::AddElemD(this, startNodes, 1);
    assert(fgFirstBB->bbNum == 1);
#else
    BlockSetOps::UnionD(this, startNodes, fgEnterBlks);
#endif

    assert(BlockSetOps::IsMember(this, startNodes, fgFirstBB->bbNum));

    // Call the recursive helper.
    unsigned postIndex = 1;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // If the block has no predecessors, and we haven't already visited it (because it's in fgEnterBlks but also
        // reachable from the first block), go ahead and traverse starting from this block.
        if (BlockSetOps::IsMember(this, startNodes, block->bbNum) &&
            !BlockSetOps::IsMember(this, visited, block->bbNum))
        {
            fgDfsInvPostOrderHelper(block, visited, &postIndex);
        }
    }

    // After the DFS reverse postorder is completed, we must have visited all the basic blocks.
    noway_assert(postIndex == fgBBcount + 1);
    noway_assert(fgBBNumMax == fgBBcount);

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nAfter doing a post order traversal of the BB graph, this is the ordering:\n");
        for (unsigned i = 1; i <= fgBBNumMax; ++i)
        {
            printf("%02u -> BB%02u\n", i, fgBBInvPostOrder[i]->bbNum);
        }
        printf("\n");
    }
#endif // DEBUG
}

BlockSet_ValRet_T   Compiler::fgDomFindStartNodes()
{
    unsigned j;
    BasicBlock* block;

    // startNodes ::  A set that represents which basic blocks in the flow graph don't have incoming edges.
    // We begin assuming everything is a start block and remove any block that is being referenced by another in its successor list.

    BlockSet BLOCKSET_INIT_NOCOPY(startNodes, BlockSetOps::MakeFull(this));

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        unsigned cSucc = block->NumSucc(this);
        for (j = 0; j < cSucc; ++j)
        {
            BasicBlock* succ = block->GetSucc(j, this);
            BlockSetOps::RemoveElemD(this, startNodes, succ->bbNum);
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nDominator computation start blocks (those blocks with no incoming edges):\n");
        BLOCKSET_ITER_INIT(this, iter, startNodes, bbNum);
        while (iter.NextElem(this, &bbNum))
        {
            printf("BB%02u ", bbNum);
        }
        printf("\n");
    }
#endif // DEBUG

    return startNodes;
}

/** A simple DFS traversal of the flow graph.
  * It computes both preorder and postorder numbering.
  */
void Compiler::fgDfsInvPostOrderHelper(BasicBlock* block, BlockSet& visited, unsigned* count)
{
    // Assume we haven't visited this node yet (callers ensure this).
    assert(!BlockSetOps::IsMember(this, visited, block->bbNum));

    // Flag the node we just visited to avoid backtracking.
    BlockSetOps::AddElemD(this, visited, block->bbNum);

    unsigned cSucc = block->NumSucc(this);
    for (unsigned j = 0; j < cSucc; ++j)
    {
        BasicBlock* succ = block->GetSucc(j, this);
        // If this is a node we haven't seen before, go ahead and recurse
        if (!BlockSetOps::IsMember(this, visited, succ->bbNum))
        {
            fgDfsInvPostOrderHelper(succ, visited, count);
        }
    }

    unsigned invCount = fgBBcount - *count + 1;
    assert(1 <= invCount && invCount <= fgBBNumMax);
    fgBBInvPostOrder[invCount] = block;
    block->bbDfsNum = invCount;
    ++(*count);
}

void Compiler::fgComputeDoms()
{
    assert(!fgCheapPredsValid);

#ifdef DEBUG
    if (verbose)
        printf("*************** In fgComputeDoms\n");

    fgVerifyHandlerTab();

    // Make sure that the predecessor lists are accurate.
    // Also check that the blocks are properly, densely numbered (so calling fgRenumberBlocks is not necessary).
    fgDebugCheckBBlist(true);

    // Assert things related to the BlockSet epoch.
    assert(fgBBcount == fgBBNumMax);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgBBNumMax + 1);
#endif // DEBUG

    BlockSet BLOCKSET_INIT_NOCOPY(processedBlks, BlockSetOps::MakeEmpty(this));

    fgBBInvPostOrder = new(this, CMK_DominatorMemory) BasicBlock*[fgBBNumMax + 1];
    memset(fgBBInvPostOrder, 0, sizeof(BasicBlock*) * (fgBBNumMax + 1));

    fgDfsInvPostOrder();
    noway_assert(fgBBInvPostOrder[0] == nullptr);

    // flRoot and bbRoot represent an imaginary unique entry point in the flow graph.
    // All the orphaned EH blocks and fgFirstBB will temporarily have its predecessors list
    // (with bbRoot as the only basic block in it) set as flRoot.
    // Later on, we clear their predecessors and let them to be nullptr again.
    // Since we number basic blocks starting at one, the imaginary entry block is conveniently numbered as zero.
    flowList flRoot;
    BasicBlock bbRoot;

    bbRoot.bbPreds = nullptr;
    bbRoot.bbNum = 0;
    bbRoot.bbIDom = &bbRoot;
    bbRoot.bbDfsNum = 0;
    flRoot.flNext = 0;
    flRoot.flBlock = &bbRoot;

    fgBBInvPostOrder[0] = &bbRoot;

    // Mark both bbRoot and fgFirstBB processed
    BlockSetOps::AddElemD(this, processedBlks, 0);    // bbRoot    == block #0
    BlockSetOps::AddElemD(this, processedBlks, 1);    // fgFirstBB == block #1
    assert(fgFirstBB->bbNum == 1);

    // Special case fgFirstBB to say its IDom is bbRoot.
    fgFirstBB->bbIDom = &bbRoot;

    BasicBlock*   block = nullptr;

    for (block = fgFirstBB->bbNext; block != nullptr; block = block->bbNext)
    {
        // If any basic block has no predecessors then we flag it as processed and temporarily
        // mark its precedessor list to be flRoot.  This makes the flowgraph connected,
        // a precondition that is needed by the dominance algorithm to operate properly.
        if (block->bbPreds == nullptr)
        {
            block->bbPreds = &flRoot;
            block->bbIDom = &bbRoot;
            BlockSetOps::AddElemD(this, processedBlks, block->bbNum);
        }
        else
        {
            block->bbIDom = nullptr;
        }
    }

    // Mark the EH blocks as entry blocks and also flag them as processed.
    if (compHndBBtabCount > 0)
    {
        EHblkDsc*       HBtab;
        EHblkDsc*       HBtabEnd;
        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
             HBtab < HBtabEnd;
             HBtab++)
        {
            if (HBtab->HasFilter())
            {
                HBtab->ebdFilter->bbIDom = &bbRoot;
                BlockSetOps::AddElemD(this, processedBlks, HBtab->ebdFilter->bbNum);
            }
            HBtab->ebdHndBeg->bbIDom = &bbRoot;
            BlockSetOps::AddElemD(this, processedBlks, HBtab->ebdHndBeg->bbNum);
        }
    }

    // Now proceed to compute the immediate dominators for each basic block.
    bool changed = true;
    while (changed)
    {
        changed = false;
        for (unsigned i = 1; i <= fgBBNumMax; ++i)   // Process each actual block; don't process the imaginary predecessor block.
        {
            flowList* first = nullptr;
            BasicBlock* newidom = nullptr;
            block = fgBBInvPostOrder[i];

            // If we have a block that has bbRoot as its bbIDom
            // it means we flag it as processed and as an entry block so
            // in this case we're all set.
            if (block->bbIDom == &bbRoot)
                continue;

            // Pick up the first processed predecesor of the current block.
            for (first = block->bbPreds; first != nullptr; first = first->flNext)
            {
                if (BlockSetOps::IsMember(this, processedBlks, first->flBlock->bbNum))
                {
                    break;
                }
            }
            noway_assert(first != nullptr);

            // We assume the first processed predecessor will be the
            // immediate dominator and then compute the forward flow analysis.
            newidom = first->flBlock;
            for (flowList* p = block->bbPreds; p != nullptr; p = p->flNext)
            {
                if (p->flBlock == first->flBlock)
                {
                    continue;
                }
                if (p->flBlock->bbIDom != nullptr)
                {
                    // fgIntersectDom is basically the set intersection between
                    // the dominance sets of the new IDom and the current predecessor
                    // Since the nodes are ordered in DFS inverse post order and
                    // IDom induces a tree, fgIntersectDom actually computes
                    // the lowest common ancestor in the dominator tree.
                    newidom = fgIntersectDom(p->flBlock, newidom);
                }
            }

            // If the Immediate dominator changed, assign the new one
            // to the current working basic block.
            if (block->bbIDom != newidom)
            {
                noway_assert(newidom != nullptr);
                block->bbIDom = newidom;
                changed = true;
            }
            BlockSetOps::AddElemD(this, processedBlks, block->bbNum);
        }
    }

    // As stated before, once we have computed immediate dominance we need to clear
    // all the basic blocks whose predecessor list was set to flRoot.  This
    // reverts that and leaves the blocks the same as before.
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbPreds == &flRoot)
        {
            block->bbPreds = nullptr;
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        fgDispDoms();
    }
#endif

    fgBuildDomTree();

    fgModified     = false;
    fgDomBBcount   = fgBBcount;
    assert(fgBBcount == fgBBNumMax);
    assert(BasicBlockBitSetTraits::GetSize(this) == fgDomBBcount + 1);

    fgDomsComputed = true;
}

void Compiler::fgBuildDomTree()
{
    unsigned i;
    BasicBlock* block;

#ifdef DEBUG
    if (verbose)
    {
        printf("\nInside fgBuildDomTree\n");
    }
#endif // DEBUG

    // domTree :: The dominance tree represented using adjacency lists. We use BasicBlockList to represent edges. Indexed by basic block number.
    unsigned bbArraySize         = fgBBNumMax + 1;
    BasicBlockList** domTree     = new(this, CMK_DominatorMemory) BasicBlockList*[bbArraySize];

    fgDomTreePreOrder  = new(this, CMK_DominatorMemory) unsigned[bbArraySize];
    fgDomTreePostOrder = new(this, CMK_DominatorMemory) unsigned[bbArraySize];

    // Initialize all the data structures.
    for (i = 0; i < bbArraySize; ++i)
    {
        domTree[i] = nullptr;
        fgDomTreePreOrder[i] = fgDomTreePostOrder[i] = 0;
    }

    // Build the dominance tree.
    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // If the immediate dominator is not the imaginary root (bbRoot)
        // we proceed to append this block to the children of the dominator node.
        if (block->bbIDom->bbNum != 0)
        {
            int bbNum = block->bbIDom->bbNum;
            domTree[bbNum] = new(this, CMK_DominatorMemory) BasicBlockList(block, domTree[bbNum]);
        }
        else
        {
            // This means this block had bbRoot set as its IDom.  We clear it out
            // and convert the tree back to a forest.
            block->bbIDom = nullptr;
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nAfter computing the Dominance Tree:\n");
        fgDispDomTree(domTree);
    }
#endif // DEBUG

    // Get the bitset that represents the roots of the dominance tree.
    // Something to note here is that the dominance tree has been converted from a forest to a tree
    // by using the bbRoot trick on fgComputeDoms. The reason we have a forest instead of a real tree
    // is because we treat the EH blocks as entry nodes so the real dominance tree is not necessarily connected.
    BlockSet_ValRet_T domTreeEntryNodes = fgDomTreeEntryNodes(domTree);

    // The preorder and postorder numbers.
    // We start from 1 to match the bbNum ordering.
    unsigned preNum  = 1;
    unsigned postNum = 1;

    // There will be nodes in the dominance tree that will not be reachable:
    // the catch blocks that return since they don't have any predecessor.
    // For that matter we'll keep track of how many nodes we can
    // reach and assert at the end that we visited all of them.
    unsigned domTreeReachable = fgBBcount;

    // Once we have the dominance tree computed, we need to traverse it
    // to get the preorder and postorder numbers for each node.  The purpose of
    // this is to achieve O(1) queries for of the form A dominates B.
    for (i = 1; i <= fgBBNumMax; ++i)
    {
        if (BlockSetOps::IsMember(this, domTreeEntryNodes, i))
        {
            if (domTree[i] == nullptr)
            {
                // If this is an entry node but there's no children on this
                // node, it means it's unreachable so we decrement the reachable
                // counter.
                --domTreeReachable;
            }
            else
            {
                // Otherwise, we do a DFS on the tree.
                fgTraverseDomTree(i, domTree, &preNum, &postNum);
            }
        }
    }

    noway_assert(preNum  == domTreeReachable + 1);
    noway_assert(postNum == domTreeReachable + 1);

    // Once we have all the reachable nodes numbered, we proceed to
    // assign numbers to the non-reachable ones, just assign incrementing
    // values.  We must reach fgBBcount at the end.

    for (i = 1; i <= fgBBNumMax; ++i)
    {
        if (BlockSetOps::IsMember(this, domTreeEntryNodes, i))
        {
            if (domTree[i] == nullptr)
            {
                fgDomTreePreOrder[i]  = preNum++;
                fgDomTreePostOrder[i] = postNum++;
            }
        }
    }

    noway_assert(preNum  == fgBBNumMax + 1);
    noway_assert(postNum == fgBBNumMax + 1);
    noway_assert(fgDomTreePreOrder[0] == 0); // Unused first element
    noway_assert(fgDomTreePostOrder[0] == 0); // Unused first element

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nAfter traversing the dominance tree:\n");
        printf("PreOrder:\n");
        for (i = 1; i <= fgBBNumMax; ++i)
        {
            printf("BB%02u : %02u\n", i, fgDomTreePreOrder[i]);
        }
        printf("PostOrder:\n");
        for (i = 1; i <= fgBBNumMax; ++i)
        {
            printf("BB%02u : %02u\n", i, fgDomTreePostOrder[i]);
        }
    }
#endif // DEBUG
}

BlockSet_ValRet_T Compiler::fgDomTreeEntryNodes(BasicBlockList** domTree)
{
    // domTreeEntryNodes ::  Set that represents which basic blocks are roots of the dominator forest.

    BlockSet BLOCKSET_INIT_NOCOPY(domTreeEntryNodes, BlockSetOps::MakeFull(this));

    // First of all we need to find all the roots of the dominance forest.

    for (unsigned i = 1; i <= fgBBNumMax; ++i)
    {
        for (BasicBlockList* current = domTree[i]; current != nullptr; current = current->next)
        {
            BlockSetOps::RemoveElemD(this, domTreeEntryNodes, current->block->bbNum);
        }
    }

    return domTreeEntryNodes;
}

#ifdef DEBUG
void Compiler::fgDispDomTree(BasicBlockList** domTree)
{
    for (unsigned i = 1; i <= fgBBNumMax; ++i)
    {
        if (domTree[i] != nullptr)
        {
            printf("BB%02u : ", i);
            for (BasicBlockList* current = domTree[i]; current != nullptr; current = current->next)
            {
                assert(current->block);
                printf("BB%02u ", current->block->bbNum);
            }
            printf("\n");
        }
    }
    printf("\n");
}
#endif // DEBUG


void Compiler::fgTraverseDomTree(unsigned         bbNum,
                                 BasicBlockList** domTree,
                                 unsigned*        preNum,
                                 unsigned*        postNum)
{
    noway_assert(bbNum <= fgBBNumMax);

    // If the block preorder number is not zero it means we already visited
    // that node, so we skip it.
    if (fgDomTreePreOrder[bbNum] == 0)
    {
        // If this is the first time we visit this node, both preorder and postnumber
        // values must be zero.
        noway_assert(fgDomTreePostOrder[bbNum] == 0);

        fgDomTreePreOrder[bbNum] = (*preNum)++;
        for (BasicBlockList* current = domTree[bbNum]; current != nullptr; current = current->next)
        {
            fgTraverseDomTree(current->block->bbNum, domTree, preNum, postNum);
        }
        fgDomTreePostOrder[bbNum] = (*postNum)++;
    }
}

// This code finds the lowest common ancestor in the
// dominator tree between two basic blocks. The LCA in the Dominance tree
// represents the closest dominator between the two basic blocks. Used to
// adjust the IDom value in fgComputDoms.
BasicBlock*         Compiler::fgIntersectDom(BasicBlock* a, BasicBlock* b)
{
    BasicBlock* finger1 = a;
    BasicBlock* finger2 = b;
    while (finger1 != finger2)
    {
        while (finger1->bbDfsNum > finger2->bbDfsNum)
        {
            finger1 = finger1->bbIDom;
        }
        while (finger2->bbDfsNum > finger1->bbDfsNum)
        {
            finger2 = finger2->bbIDom;
        }
    }
    return finger1;
}


// Return a BlockSet containing all the blocks that dominate 'block'.
BlockSet_ValRet_T   Compiler::fgGetDominatorSet(BasicBlock* block)
{
    assert(block != nullptr);

    BlockSet BLOCKSET_INIT_NOCOPY(domSet, BlockSetOps::MakeEmpty(this));

    do
    {
        BlockSetOps::AddElemD(this, domSet, block->bbNum);
        if (block == block->bbIDom)
        {
            break; // We found a cycle in the IDom list, so we're done.
        }
        block = block->bbIDom;
    }
    while (block != nullptr);

    return domSet;
}

/*****************************************************************************
 *
 *  fgComputeCheapPreds: Function called to compute the BasicBlock::bbCheapPreds lists.
 *
 *  No other block data is changed (e.g., bbRefs, bbFlags).
 *
 *  The cheap preds lists are similar to the normal (bbPreds) predecessor lists, but are cheaper to
 *  compute and store, as follows:
 *  1. A flow edge is typed BasicBlockList, which only has a block pointer and 'next' pointer. It doesn't
 *     have weights or a dup count.
 *  2. The preds list for a block is not sorted by block number.
 *  3. The predecessors of the block following a BBJ_CALLFINALLY (the corresponding BBJ_ALWAYS,
 *     for normal, non-retless calls to the finally) are not computed.
 *  4. The cheap preds lists will contain duplicates if a single switch table has multiple branches
 *     to the same block. Thus, we don't spend the time looking for duplicates for every edge we insert.
 */
void                Compiler::fgComputeCheapPreds()
{
    noway_assert(!fgComputePredsDone);  // We can't do this if we've got the full preds.
    noway_assert(fgFirstBB != nullptr);

    BasicBlock* block;

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** In fgComputeCheapPreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    // Clear out the cheap preds lists.
    fgRemovePreds();

    for (block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
        case BBJ_COND:
            fgAddCheapPred(block->bbJumpDest, block);
            fgAddCheapPred(block->bbNext, block);
            break;

        case BBJ_CALLFINALLY:
        case BBJ_LEAVE: // If fgComputeCheapPreds is called before all blocks are imported, BBJ_LEAVE blocks are still in the BB list.
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
            fgAddCheapPred(block->bbJumpDest, block);
            break;

        case BBJ_NONE:
            fgAddCheapPred(block->bbNext, block);
            break;

        case BBJ_EHFILTERRET:
            // Connect end of filter to catch handler.
            // In a well-formed program, this cannot be null.  Tolerate here, so that we can call
            // fgComputeCheapPreds before fgImport on an ill-formed program; the problem will be detected in fgImport.
            if (block->bbJumpDest != nullptr)
            {
                fgAddCheapPred(block->bbJumpDest, block);
            }
            break;

        case BBJ_SWITCH:
            unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab; jumpTab = block->bbJumpSwt->bbsDstTab;

            do
            {
                fgAddCheapPred(*jumpTab, block);
            }
            while (++jumpTab, --jumpCnt);

            break;

        case BBJ_EHFINALLYRET:   // It's expensive to compute the preds for this case, so we don't for the cheap preds.
        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
        }
    }

    fgCheapPredsValid = true;

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** After fgComputeCheapPreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif
}

/*****************************************************************************
 * Add 'blockPred' to the cheap predecessor list of 'block'.
 */

void                Compiler::fgAddCheapPred(BasicBlock* block,
                                             BasicBlock* blockPred)
{
    assert(!fgComputePredsDone);
    assert(block != nullptr);
    assert(blockPred != nullptr);

    block->bbCheapPreds = new (this, CMK_FlowList) BasicBlockList(blockPred, block->bbCheapPreds);

#if MEASURE_BLOCK_SIZE
    genFlowNodeCnt  += 1;
    genFlowNodeSize += sizeof(BasicBlockList);
#endif // MEASURE_BLOCK_SIZE
}

/*****************************************************************************
 * Remove 'blockPred' from the cheap predecessor list of 'block'.
 * If there are duplicate edges, only remove one of them.
 */
void                Compiler::fgRemoveCheapPred(BasicBlock* block,
                                                BasicBlock* blockPred)
{
    assert(!fgComputePredsDone);
    assert(fgCheapPredsValid);

    flowList* oldEdge = NULL;

    assert(block != nullptr);
    assert(blockPred != nullptr);
    assert(block->bbCheapPreds != nullptr);

    /* Is this the first block in the pred list? */
    if (blockPred == block->bbCheapPreds->block)
    {
        block->bbCheapPreds = block->bbCheapPreds->next;
    }
    else
    {
        BasicBlockList* pred;
        for (pred = block->bbCheapPreds;
             pred->next != nullptr;
             pred = pred->next)
        {
            if (blockPred == pred->next->block)
                break;
        }
        noway_assert(pred->next != nullptr); // we better have found it!
        pred->next = pred->next->next; // splice it out
    }
}

void                Compiler::fgRemovePreds()
{
    C_ASSERT(offsetof(BasicBlock, bbPreds) == offsetof(BasicBlock, bbCheapPreds));              // bbPreds and bbCheapPreds are at the same place in a union,
    C_ASSERT(sizeof( ((BasicBlock*)0)->bbPreds ) == sizeof( ((BasicBlock*)0)->bbCheapPreds ));  // and are the same size. So, this function removes both.

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->bbPreds = nullptr;
    }
    fgComputePredsDone = false;
    fgCheapPredsValid = false;
}

/*****************************************************************************
 *
 *  Function called to compute the bbPreds lists.
 */
void                Compiler::fgComputePreds()
{
    noway_assert(fgFirstBB);

    BasicBlock* block;

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** In fgComputePreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    // reset the refs count for each basic block

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        block->bbRefs = 0;
    }

    /* the first block is always reachable! */
    fgFirstBB->bbRefs = 1;

    /* Treat the initial block as a jump target */
    fgFirstBB->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

    fgRemovePreds();

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
        case BBJ_CALLFINALLY:
            if (!(block->bbFlags & BBF_RETLESS_CALL))
            {
                assert(block->isBBCallAlwaysPair());

                /* Mark the next block as being a jump target,
                   since the call target will return there */
                PREFIX_ASSUME(block->bbNext != NULL);
                block->bbNext->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
            }

            __fallthrough;

        case BBJ_LEAVE: // Sometimes fgComputePreds is called before all blocks are imported, so BBJ_LEAVE
                        // blocks are still in the BB list.
        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:

            /* Mark the jump dest block as being a jump target */
            block->bbJumpDest->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

            fgAddRefPred(block->bbJumpDest, block, nullptr, true);

            /* Is the next block reachable? */

            if  (block->bbJumpKind != BBJ_COND)
                break;

            noway_assert(block->bbNext);

            /* Fall through, the next block is also reachable */
            __fallthrough;

        case BBJ_NONE:

            fgAddRefPred(block->bbNext, block, nullptr, true);
            break;

        case BBJ_EHFILTERRET:

            // Connect end of filter to catch handler.
            // In a well-formed program, this cannot be null.  Tolerate here, so that we can call
            // fgComputePreds before fgImport on an ill-formed program; the problem will be detected in fgImport.
            if (block->bbJumpDest != NULL)
            {
                fgAddRefPred(block->bbJumpDest, block, nullptr, true);
            }
            break;

        case BBJ_EHFINALLYRET:
            {
                /* Connect the end of the finally to the successor of
                  the call to this finally */

                if (!block->hasHndIndex())
                    NO_WAY("endfinally outside a finally/fault block.");

                unsigned      hndIndex = block->getHndIndex();
                EHblkDsc*     ehDsc    = ehGetDsc(hndIndex);

                if (!ehDsc->HasFinallyOrFaultHandler())
                    NO_WAY("endfinally outside a finally/fault block.");

                if (ehDsc->HasFinallyHandler())
                {
                    // Find all BBJ_CALLFINALLY that branched to this finally handler.
                    BasicBlock* begBlk;
                    BasicBlock* endBlk;
                    ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                    BasicBlock* finBeg = ehDsc->ebdHndBeg;
                    for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                    {
                        if  (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                            continue;

                        noway_assert(bcall->isBBCallAlwaysPair());
                        fgAddRefPred(bcall->bbNext, block, nullptr, true);
                    }
                }
            }
            break;

        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:
            unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab; jumpTab = block->bbJumpSwt->bbsDstTab;

            do
            {
                /* Mark the target block as being a jump target */
                (*jumpTab)->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

                fgAddRefPred(*jumpTab, block, nullptr, true);
            }
            while (++jumpTab, --jumpCnt);

            break;

        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
        }
    }

    for (unsigned EHnum = 0; EHnum < compHndBBtabCount; EHnum++)
    {
        EHblkDsc* ehDsc = ehGetDsc(EHnum);

        if (ehDsc->HasFilter())
        {
            ehDsc->ebdFilter->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
        }

        ehDsc->ebdHndBeg->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
    }

    fgModified = false;
    fgComputePredsDone = true;

#ifdef DEBUG
    if  (verbose)
    {
        printf("\n*************** After fgComputePreds()\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif
}

unsigned            Compiler::fgNSuccsOfFinallyRet(BasicBlock* block)
{
    BasicBlock* bb;
    unsigned res;
    fgSuccOfFinallyRetWork(block, ~0, &bb, &res);
    return res;
}

BasicBlock*         Compiler::fgSuccOfFinallyRet(BasicBlock* block, unsigned i)
{
    BasicBlock* bb;
    unsigned res;
    fgSuccOfFinallyRetWork(block, i, &bb, &res);
    return bb;
}

void                Compiler::fgSuccOfFinallyRetWork(BasicBlock* block, unsigned i, BasicBlock** bres, unsigned* nres)
{
    assert(block->hasHndIndex()); // Otherwise, endfinally outside a finally/fault block?

    unsigned      hndIndex = block->getHndIndex();
    EHblkDsc*     ehDsc    = ehGetDsc(hndIndex);

    assert(ehDsc->HasFinallyOrFaultHandler());  // Otherwise, endfinally outside a finally/fault block.

    *bres = NULL;
    unsigned succNum = 0;

    if (ehDsc->HasFinallyHandler())
    {
        BasicBlock* begBlk;
        BasicBlock* endBlk;
        ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

        BasicBlock* finBeg   = ehDsc->ebdHndBeg;

        for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
        {
            if  (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                continue;

            assert(bcall->isBBCallAlwaysPair());

            if (succNum == i)
            {
                *bres = bcall->bbNext;
                return;
            }
            succNum++;
        }
    }
    assert(i == ~0u || ehDsc->HasFaultHandler());  // Should reach here only for fault blocks.
    if (i == ~0u)
    {
        *nres = succNum;
    }
}

Compiler::SwitchUniqueSuccSet Compiler::GetDescriptorForSwitch(BasicBlock* switchBlk)
{
    assert(switchBlk->bbJumpKind == BBJ_SWITCH);
    BlockToSwitchDescMap* switchMap = GetSwitchDescMap();
    SwitchUniqueSuccSet res;
    if (switchMap->Lookup(switchBlk, &res))
    {
        return res;
    }
    else
    {
        // We must compute the descriptor. Find which are dups, by creating a bit set with the unique successors.
        // We create a temporary bitset of blocks to compute the unique set of successor blocks,
        // since adding a block's number twice leaves just one "copy" in the bitset. Note that
        // we specifically don't use the BlockSet type, because doing so would require making a
        // call to EnsureBasicBlockEpoch() to make sure the epoch is up-to-date. However, that
        // can create a new epoch, thus invalidating all existing BlockSet objects, such as
        // reachability information stored in the blocks. To avoid that, we just use a local BitVec.

        BitVecTraits blockVecTraits(fgBBNumMax + 1, this);
        BitVec BITVEC_INIT_NOCOPY(uniqueSuccBlocks, BitVecOps::MakeEmpty(&blockVecTraits));
        BasicBlock** jumpTable = switchBlk->bbJumpSwt->bbsDstTab;
        unsigned jumpCount = switchBlk->bbJumpSwt->bbsCount;
        for (unsigned i = 0; i < jumpCount; i++)
        {
            BasicBlock* targ = jumpTable[i];
            BitVecOps::AddElemD(&blockVecTraits, uniqueSuccBlocks, targ->bbNum);
        }
        // Now we have a set of unique successors.
        unsigned numNonDups = BitVecOps::Count(&blockVecTraits, uniqueSuccBlocks);

        typedef BasicBlock* BasicBlockPtr;
        BasicBlockPtr* nonDups = new (getAllocator()) BasicBlockPtr[numNonDups];

        unsigned nonDupInd = 0;
        // At this point, all unique targets are in "uniqueSuccBlocks".  As we encounter each,
        // add to nonDups, remove from "uniqueSuccBlocks".
        for (unsigned i = 0; i < jumpCount; i++)
        {
            BasicBlock* targ = jumpTable[i];
            if (BitVecOps::IsMember(&blockVecTraits, uniqueSuccBlocks, targ->bbNum))
            {
                nonDups[nonDupInd] = targ;
                nonDupInd++;
                BitVecOps::RemoveElemD(&blockVecTraits, uniqueSuccBlocks, targ->bbNum);
            }
        }

        assert(nonDupInd == numNonDups);
        assert(BitVecOps::Count(&blockVecTraits, uniqueSuccBlocks) == 0);
        res.numDistinctSuccs = numNonDups;
        res.nonDuplicates = nonDups;
        switchMap->Set(switchBlk, res);
        return res;
    }
}

void Compiler::SwitchUniqueSuccSet::UpdateTarget(IAllocator* alloc, BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to)
{
    assert(switchBlk->bbJumpKind == BBJ_SWITCH); // Precondition.
    unsigned jmpTabCnt = switchBlk->bbJumpSwt->bbsCount;
    BasicBlock** jmpTab = switchBlk->bbJumpSwt->bbsDstTab;

    // Is "from" still in the switch table (because it had more than one entry before?)
    bool fromStillPresent = false;
    for (unsigned i = 0; i < jmpTabCnt; i++)
    {
        if (jmpTab[i] == from)
        {
            fromStillPresent = true; break;
        }
    }

    // Is "to" already in "this"?
    bool toAlreadyPresent = false;
    for (unsigned i = 0; i < numDistinctSuccs; i++)
    {
        if (nonDuplicates[i] == to)
        {
            toAlreadyPresent = true; break;
        }
    }

    // Four cases:
    //   If "from" is still present, and "to" is already present, do nothing
    //   If "from" is still present, and "to" is not, must reallocate to add an entry.
    //   If "from" is not still present, and "to" is not present, write "to" where "from" was.
    //   If "from" is not still present, but "to" is present, remove "from".
    if (fromStillPresent && toAlreadyPresent)
    {
        return;
    }
    else if (fromStillPresent && !toAlreadyPresent)
    {
        // reallocate to add an entry
        typedef BasicBlock* BasicBlockPtr;
        BasicBlockPtr* newNonDups = new (alloc) BasicBlockPtr[numDistinctSuccs+1];
        memcpy(newNonDups, nonDuplicates, numDistinctSuccs*sizeof(BasicBlock*));
        newNonDups[numDistinctSuccs] = to;
        numDistinctSuccs++;
        nonDuplicates = newNonDups;
    }
    else if (!fromStillPresent && !toAlreadyPresent)
    {
        // write "to" where "from" was
#ifdef DEBUG
        bool foundFrom = false;
#endif // DEBUG
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = to;
#ifdef DEBUG
                foundFrom = true;
#endif // DEBUG
                break;
            }
        }
        assert(foundFrom);
    }
    else
    {
        assert(!fromStillPresent && toAlreadyPresent);
        // remove "from".
#ifdef DEBUG
        bool foundFrom = false;
#endif // DEBUG
        for (unsigned i = 0; i < numDistinctSuccs; i++)
        {
            if (nonDuplicates[i] == from)
            {
                nonDuplicates[i] = nonDuplicates[numDistinctSuccs-1];
                numDistinctSuccs--;
#ifdef DEBUG
                foundFrom = true;
#endif // DEBUG
                break;
            }
        }
        assert(foundFrom);
    }
}

/*****************************************************************************
 *  
 *  Simple utility function to remove an entry for a block in the switch desc
 *  map. So it can be called from other phases.
 *
 */
void Compiler::fgInvalidateSwitchDescMapEntry(BasicBlock* block)
{
    // Check if map has no entries yet.
    if (m_switchDescMap != nullptr)
    {
        m_switchDescMap->Remove(block);
    }
}

void Compiler::UpdateSwitchTableTarget(BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to)
{
    if (m_switchDescMap == NULL)
        return;  // No mappings, nothing to do.

    // Otherwise...
    BlockToSwitchDescMap* switchMap = GetSwitchDescMap();
    SwitchUniqueSuccSet* res = switchMap->LookupPointer(switchBlk);
    if (res != nullptr)
    {
        // If no result, nothing to do. Otherwise, update it.
        res->UpdateTarget(getAllocator(), switchBlk, from, to);
    }
}


/*****************************************************************************
 *  For a block that is in a handler region, find the first block of the most-nested
 *  handler containing the block.
 */
BasicBlock*         Compiler::fgFirstBlockOfHandler(BasicBlock* block)
{
    assert(block->hasHndIndex());
    return ehGetDsc(block->getHndIndex())->ebdHndBeg;
}


/*****************************************************************************
 *
 *  Function called to find back edges and return blocks and mark them as needing GC Polls.  This marks all
 *  blocks.
 */
void                Compiler::fgMarkGCPollBlocks()
{
    if (GCPOLL_NONE == opts.compGCPollType)
        return;

#ifdef DEBUG
    /* Check that the flowgraph data (bbNum, bbRefs, bbPreds) is up-to-date */
    fgDebugCheckBBlist();
#endif

    BasicBlock* block;

    //Return blocks always need GC polls.  In addition, all back edges (including those from switch
    //statements) need GC polls.  The poll is on the block with the outgoing back edge (or ret), rather than
    //on the destination or on the edge itself.
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        bool blockNeedsPoll = false;
        switch (block->bbJumpKind)
        {
        case BBJ_COND:
        case BBJ_ALWAYS:
            blockNeedsPoll = (block->bbJumpDest->bbNum <= block->bbNum);
            break;

        case BBJ_RETURN:
            blockNeedsPoll = true;
            break;

        case BBJ_SWITCH:
            unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab; jumpTab = block->bbJumpSwt->bbsDstTab;

            do
            {
                if ((*jumpTab)->bbNum <= block->bbNum)
                {
                    blockNeedsPoll = true;
                    break;
                }
            }
            while (++jumpTab, --jumpCnt);
            break;

        default:
            break;
        }

        if (blockNeedsPoll)
        {
            block->bbFlags |= BBF_NEEDS_GCPOLL;
        }
    }
}

void                Compiler::fgInitBlockVarSets()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        block->InitVarSets(this);
    }

    // QMarks are much like blocks, and need their VarSets initialized.
    assert(!compIsForInlining());
    for (unsigned i = 0; i < compQMarks->Size(); i++)
    {
        GenTreePtr qmark = compQMarks->Get(i);
        // Perhaps the gtOper of a QMark node was changed to something else since it was created and put on this list.
        // So can't hurt to check.
        if (qmark->OperGet() == GT_QMARK)
        {
            VarSetOps::AssignAllowUninitRhs(this, qmark->gtQmark.gtThenLiveSet, VarSetOps::UninitVal());
            VarSetOps::AssignAllowUninitRhs(this, qmark->gtQmark.gtElseLiveSet, VarSetOps::UninitVal());
        }
    }
    fgBBVarSetsInited = true;
}

/*****************************************************************************
 *
 *  The following does the final pass on BBF_NEEDS_GCPOLL and then actually creates the GC Polls.
 */
void                Compiler::fgCreateGCPolls()
{
    if (GCPOLL_NONE == opts.compGCPollType)
        return;

    bool createdPollBlocks = false;

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgCreateGCPolls() for %s\n",
               info.compFullName);
    }
#endif // DEBUG

    if (!(opts.MinOpts() || opts.compDbgCode))
    {
        // Remove polls from well formed loops with a constant upper bound.
        for (unsigned lnum = 0; lnum < optLoopCount; ++lnum)
        {
            // Look for constant counted loops that run for a short duration.  This logic is very similar to
            // what's in code:Compiler::optUnrollLoops, since they have similar constraints.  However, this
            // logic is much more permissive since we're not doing a complex transformation.

            /* TODO-Cleanup:
             * I feel bad cloning so much logic from optUnrollLoops
             */

            // Filter out loops not meeting the obvious preconditions.
            //
            if (optLoopTable[lnum].lpFlags & LPFLG_REMOVED)
                continue;

            if (!(optLoopTable[lnum].lpFlags & LPFLG_CONST))
                continue;

            BasicBlock* head   = optLoopTable[lnum].lpHead;
            BasicBlock* bottom = optLoopTable[lnum].lpBottom;

            // Loops dominated by GC_SAFE_POINT won't have this set.
            if (!(bottom->bbFlags & BBF_NEEDS_GCPOLL))
                continue;

            /* Get the loop data:
                - initial constant
                - limit constant
                - iterator
                - iterator increment
                - increment operation type (i.e. ASG_ADD, ASG_SUB, etc...)
                - loop test type (i.e. GT_GE, GT_LT, etc...)
             */

            int lbeg               = optLoopTable[lnum].lpConstInit;
            int llim               = optLoopTable[lnum].lpConstLimit();
            genTreeOps testOper     = optLoopTable[lnum].lpTestOper();

            int lvar               = optLoopTable[lnum].lpIterVar();
            int iterInc            = optLoopTable[lnum].lpIterConst();
            genTreeOps iterOper     = optLoopTable[lnum].lpIterOper();

            var_types iterOperType  = optLoopTable[lnum].lpIterOperType();
            bool unsTest            = (optLoopTable[lnum].lpTestTree->gtFlags & GTF_UNSIGNED) != 0;
            if (lvaTable[lvar].lvAddrExposed) // Can't reason about the value of the iteration variable.
                continue;

            unsigned totalIter;

            /* Find the number of iterations - the function returns false if not a constant number */

            if (!optComputeLoopRep(lbeg, llim,
                                   iterInc, iterOper, iterOperType,
                                   testOper, unsTest,
                                   // The value here doesn't matter for this variation of the optimization
                                   true,
                                   &totalIter))
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf( "Could not compute loop iterations for loop from BB%02u to BB%02u",
                            head->bbNum, bottom->bbNum );
                }
#endif // DEBUG
                (void)head; //suppress gcc error.

                continue;
            }

            /* Forget it if there are too many repetitions or not a constant loop */

            static const unsigned ITER_LIMIT = 256;
            if  (totalIter > ITER_LIMIT)
                continue;

            //It is safe to elminate the poll from this loop.
            bottom->bbFlags &= ~BBF_NEEDS_GCPOLL;

#ifdef DEBUG
            if (verbose)
            {
                printf( "Removing poll in block BB%02u because it forms a bounded counted loop\n",
                        bottom->bbNum );
            }
#endif // DEBUG
        }
    }

    // Final chance to optimize the polls.  Move all polls in loops from the bottom of the loop up to the
    // loop head.  Also eliminate all epilog polls in non-leaf methods.  This only works if we have dominator
    // information.
    if (fgDomsComputed)
    {
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            if (!(block->bbFlags & BBF_NEEDS_GCPOLL))
                continue;

            if (block->bbJumpKind == BBJ_COND ||
                block->bbJumpKind == BBJ_ALWAYS)
            {
                //make sure that this is loop-like
                if (!fgReachable(block->bbJumpDest, block))
                {
                    block->bbFlags &= ~BBF_NEEDS_GCPOLL;
#ifdef DEBUG
                    if (verbose)
                        printf("Removing poll in block BB%02u because it is not loop\n", block->bbNum);
#endif // DEBUG
                    continue;
                }
            }
            else if (!(block->bbJumpKind == BBJ_RETURN ||
                       block->bbJumpKind == BBJ_SWITCH))
            {
                noway_assert(!"GC Poll on a block that has no control transfer.");
#ifdef DEBUG
                if (verbose)
                    printf("Removing poll in block BB%02u because it is not a jump\n", block->bbNum);
#endif // DEBUG
                block->bbFlags &= ~BBF_NEEDS_GCPOLL;
                continue;
            }

            // Because of block compaction, it's possible to end up with a block that is both poll and safe.
            // Clean those up now.

            if (block->bbFlags & BBF_GC_SAFE_POINT)
            {
#ifdef DEBUG
                if (verbose)
                    printf("Removing poll in return block BB%02u because it is GC Safe\n", block->bbNum);
#endif // DEBUG
                block->bbFlags &= ~BBF_NEEDS_GCPOLL;
                continue;
            }

            if (block->bbJumpKind == BBJ_RETURN)
            {
                if (!optReachWithoutCall(fgFirstBB, block))
                {
                    //check to see if there is a call along the path between the first block and the return
                    //block.
                    block->bbFlags &= ~BBF_NEEDS_GCPOLL;
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Removing poll in return block BB%02u because it dominated by a call\n", block->bbNum);
                    }
#endif // DEBUG
                    continue;
                }

            }
        }
    }

    noway_assert(!fgGCPollsCreated);
    BasicBlock*  block;
    fgGCPollsCreated = true;

    // Walk through the blocks and hunt for a block that has BBF_NEEDS_GCPOLL
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        // Because of block compaction, it's possible to end up with a block that is both poll and safe.
        // And if !fgDomsComputed, we won't have cleared them, so skip them now
        if (!(block->bbFlags & BBF_NEEDS_GCPOLL) || (block->bbFlags & BBF_GC_SAFE_POINT))
            continue;

        // This block needs a poll.  We either just insert a callout or we split the block and inline part of
        // the test.  This depends on the value of opts.compGCPollType.

        // If we're doing GCPOLL_CALL, just insert a GT_CALL node before the last node in the block.

#ifdef DEBUG
        switch (block->bbJumpKind)
        {
        case BBJ_RETURN:
        case BBJ_ALWAYS:
        case BBJ_COND:
        case BBJ_SWITCH:
            break;
        default:
            noway_assert(!"Unknown block type for BBF_NEEDS_GCPOLL");
        }
#endif // DEBUG

        noway_assert(opts.compGCPollType);

        GCPollType pollType = opts.compGCPollType;
        //pollType is set to either CALL or INLINE at this point.  Below is the list of places where we
        //can't or don't want to emit an inline check.  Check all of those.  If after all of that we still
        //have INLINE, then emit an inline check.

        if (opts.MinOpts() || opts.compDbgCode)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf( "Selecting CALL poll in block BB%02u because of debug/minopts\n", block->bbNum );
            }
#endif // DEBUG

            // Don't split blocks and create inlined polls unless we're optimizing.
            pollType = GCPOLL_CALL;
        }
        else if (genReturnBB == block)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block BB%02u because it is the single return block\n", block->bbNum);
            }
#endif // DEBUG

            //we don't want to split the single return block
            pollType = GCPOLL_CALL;
        }
        else if (BBJ_SWITCH == block->bbJumpKind)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block BB%02u because it is a loop formed by a SWITCH\n", block->bbNum);
            }
#endif // DEBUG

            // I don't want to deal with all the outgoing edges of a switch block.
            pollType = GCPOLL_CALL;
        }

        // TODO-Cleanup: potentially don't split if we're in an EH region.

        createdPollBlocks |= fgCreateGCPoll(pollType, block);
    }

    // If we split a block to create a GC Poll, then rerun fgReorderBlocks to push the rarely run blocks out
    // past the epilog.  We should never split blocks unless we're optimizing.
    if (createdPollBlocks)
    {
        noway_assert(!opts.MinOpts() && !opts.compDbgCode);
        fgReorderBlocks();
    }
}

/*****************************************************************************
 *
 *  Actually create a GCPoll in the given block. Returns true if it created
 *  a basic block.
 */

bool                Compiler::fgCreateGCPoll(GCPollType pollType, BasicBlock* block)
{
    assert(!(block->bbFlags & BBF_GC_SAFE_POINT));
    bool createdPollBlocks;

    void* addrTrap;
    void* pAddrOfCaptureThreadGlobal;

    addrTrap = info.compCompHnd->getAddrOfCaptureThreadGlobal(&pAddrOfCaptureThreadGlobal);

    // I never want to split blocks if we've got two indirections here.
    // This is a size trade-off assuming the VM has ENABLE_FAST_GCPOLL_HELPER.
    // So don't do it when that is off
#ifdef ENABLE_FAST_GCPOLL_HELPER
    if (pAddrOfCaptureThreadGlobal != NULL)
    {
        pollType = GCPOLL_CALL;
    }
#endif // ENABLE_FAST_GCPOLL_HELPER

    if (GCPOLL_CALL == pollType)
    {
        createdPollBlocks = false;
        GenTreePtr tree = gtNewHelperCallNode(CORINFO_HELP_POLL_GC, TYP_VOID);
#if GTF_CALL_REG_SAVE
        tree->gtCall.gtCallMoreFlags |= GTF_CALL_REG_SAVE;
#endif //GTF_CALL_REG_SAVE

        // for BBJ_ALWAYS I don't need to insert it before the condition.  Just append it.
        if (block->bbJumpKind == BBJ_ALWAYS)
        {
            fgInsertStmtAtEnd(block, tree);
        }
        else
        {
            GenTreeStmt* newStmt = fgInsertStmtNearEnd(block, tree);
            // For DDB156656, we need to associate the GC Poll with the IL offset (and therefore sequence
            // point) of the tree before which we inserted the poll.  One example of when this is a
            // problem:
            //  if (...) {  //1
            //      ...
            //  } //2
            //  else { //3
            //      ...
            //  }
            //  (gcpoll) //4
            //  return. //5
            //
            //  If we take the if statement at 1, we encounter a jump at 2.  This jumps over the else
            //  and lands at 4.  4 is where we inserted the gcpoll.  However, that is associated with
            //  the sequence point a 3.  Therefore, the debugger displays the wrong source line at the
            //  gc poll location.
            //
            //  More formally, if control flow targets an instruction, that instruction must be the
            //  start of a new sequence point.
            if (newStmt->gtNext)
            {
                // Is it possible for gtNext to be NULL?
                noway_assert(newStmt->gtNext->gtOper == GT_STMT);
                newStmt->gtStmtILoffsx = newStmt->gtNextStmt->gtStmtILoffsx;
            }
        }

        block->bbFlags |= BBF_GC_SAFE_POINT;
#ifdef DEBUG
        if (verbose)
        {
            printf("*** creating GC Poll in block BB%02u\n", block->bbNum);
            gtDispTreeList(block->bbTreeList);
        }
#endif // DEBUG
    }
    else
    {
        createdPollBlocks = true;
        // if we're doing GCPOLL_INLINE, then:
        //  1) Create two new blocks: Poll and Bottom.  The original block is called Top.

        // I want to create:
        // top -> poll -> bottom (lexically)
        // so that we jump over poll to get to bottom.
        BasicBlock* top = block;
        BasicBlock* poll = fgNewBBafter(BBJ_NONE, top, true);
        BasicBlock* bottom = fgNewBBafter(top->bbJumpKind, poll, true);
        BBjumpKinds oldJumpKind = top->bbJumpKind;

        // Update block flags
        unsigned originalFlags;
        originalFlags = top->bbFlags | BBF_GC_SAFE_POINT;

        // Unlike Fei's inliner from puclr, I'm allowed to split loops.
        // And we keep a few other flags...
        noway_assert((originalFlags & (BBF_SPLIT_NONEXIST
                                        & ~(BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1))) == 0);
        top->bbFlags    = originalFlags & (~BBF_SPLIT_LOST | BBF_GC_SAFE_POINT);
        bottom->bbFlags |= originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT);
        bottom->inheritWeight(top);
        poll->bbFlags |= originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT);

        //  9) Mark Poll as rarely run.
        poll->bbSetRunRarely();

        //  5) Bottom gets all the outgoing edges and inherited flags of Original.
        bottom->bbJumpDest = top->bbJumpDest;

        //  2) Add a GC_CALL node to Poll.
        GenTreePtr tree = gtNewHelperCallNode(CORINFO_HELP_POLL_GC, TYP_VOID);
#if GTF_CALL_REG_SAVE
        tree->gtCall.gtCallMoreFlags |= GTF_CALL_REG_SAVE;
#endif //GTF_CALL_REG_SAVE
        fgInsertStmtAtEnd(poll, tree);

        //  3) Remove the last statement from Top and add it to Bottom.
        if (oldJumpKind != BBJ_ALWAYS)
        {
            // if I'm always jumping to the target, then this is not a condition that needs moving.
            GenTreeStmt* stmt = top->firstStmt();
            while (stmt->gtNext)
            {
                stmt = stmt->gtNextStmt;
            }
            fgRemoveStmt(top, stmt);
            fgInsertStmtAtEnd(bottom, stmt);
        }
        // for BBJ_ALWAYS blocks, bottom is an empty block.

        //  4) Create a GT_EQ node that checks against g_TrapReturningThreads.  True jumps to Bottom,
        //  false falls through to poll.  Add this to the end of Top.  Top is now BBJ_COND.  Bottom is
        //  now a jump target

#ifdef ENABLE_FAST_GCPOLL_HELPER
        // Prefer the fast gc poll helepr over the double indirection
        noway_assert(pAddrOfCaptureThreadGlobal == nullptr);
#endif

        GenTreePtr trap;
        if (pAddrOfCaptureThreadGlobal != nullptr)
        {
            trap = gtNewOperNode(GT_IND, TYP_I_IMPL, gtNewIconHandleNode((size_t)pAddrOfCaptureThreadGlobal, GTF_ICON_PTR_HDL));
        }
        else
        {
            trap = gtNewIconHandleNode((size_t)addrTrap, GTF_ICON_PTR_HDL);
        }

        GenTreePtr trapRelop = gtNewOperNode(GT_EQ, TYP_INT,
                                         //lhs [g_TrapReturningThreads]
                                             gtNewOperNode(GT_IND, TYP_INT, trap),
                                         //rhs 0
                                             gtNewIconNode(0, TYP_INT)
                                        );
        trapRelop->gtFlags |= GTF_RELOP_JMP_USED | GTF_DONT_CSE; //Treat reading g_TrapReturningThreads as volatile.
        GenTreePtr trapCheck = gtNewOperNode(GT_JTRUE, TYP_VOID, trapRelop);
        fgInsertStmtAtEnd(top, trapCheck);
        top->bbJumpDest = bottom;
        top->bbJumpKind = BBJ_COND;
        bottom->bbFlags |= BBF_JMP_TARGET;

        //  7) Bottom has Top and Poll as its predecessors.  Poll has just Top as a predecessor.
        fgAddRefPred(bottom, poll);
        fgAddRefPred(bottom, top);
        fgAddRefPred(poll, top);

        //  8) Replace Top with Bottom in the predecessor list of all outgoing edges from Bottom (1 for
        //      jumps, 2 for conditional branches, N for switches).
        switch (oldJumpKind)
        {
        case BBJ_RETURN:
            // no successors
            break;
        case BBJ_COND:
            // replace predecessor in the fall through block.
            noway_assert(bottom->bbNext);
            fgReplacePred(bottom->bbNext, top, bottom);

            // fall through for the jump target
            __fallthrough;

        case BBJ_ALWAYS:
            fgReplacePred(bottom->bbJumpDest, top, bottom);
            break;
        case BBJ_SWITCH:
            NO_WAY("SWITCH should be a call rather than an inlined poll.");
            break;
        default:
            NO_WAY("Unknown block type for updating predecessor lists.");
        }

        top->bbFlags &= ~BBF_NEEDS_GCPOLL;
        noway_assert(!(poll->bbFlags & BBF_NEEDS_GCPOLL));
        noway_assert(!(bottom->bbFlags & BBF_NEEDS_GCPOLL));

        if (compCurBB == top)
        {
            compCurBB = bottom;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("*** creating inlined GC Poll in top block BB%02u\n", top->bbNum);
            gtDispTreeList(top->bbTreeList);
            printf(" poll block is BB%02u\n", poll->bbNum);
            gtDispTreeList(poll->bbTreeList);
            printf(" bottom block is BB%02u\n", bottom->bbNum);
            gtDispTreeList(bottom->bbTreeList);
        }
#endif // DEBUG
    }

    return createdPollBlocks;
}


/*****************************************************************************
 *
 *  The following helps find a basic block given its PC offset.
 */

void                Compiler::fgInitBBLookup()
{
    BasicBlock**    dscBBptr;
    BasicBlock*     tmpBBdesc;

    /* Allocate the basic block table */

    dscBBptr = fgBBs = new (this, CMK_BasicBlock) BasicBlock*[fgBBcount];

    /* Walk all the basic blocks, filling in the table */

    for (tmpBBdesc = fgFirstBB; tmpBBdesc; tmpBBdesc = tmpBBdesc->bbNext)
    {
        *dscBBptr++ = tmpBBdesc;
    }

    noway_assert(dscBBptr == fgBBs + fgBBcount);
}


BasicBlock*         Compiler::fgLookupBB(unsigned addr)
{
    unsigned        lo;
    unsigned        hi;

    /* Do a binary search */

    for (lo = 0, hi = fgBBcount - 1;;)
    {

AGAIN:;

        if (lo > hi)
            break;

        unsigned    mid = (lo + hi) / 2;
        BasicBlock* dsc = fgBBs[mid];

        // We introduce internal blocks for BBJ_CALLFINALLY. Skip over these.

        while (dsc->bbFlags & BBF_INTERNAL)
        {
            dsc = dsc->bbNext;
            mid++;

            // We skipped over too many, Set hi back to the original mid - 1

            if (mid > hi)
            {
                mid = (lo + hi) / 2;
                hi = mid - 1;
                goto AGAIN;
            }
        }

        unsigned   pos = dsc->bbCodeOffs;

        if  (pos < addr)
        {
            if ((lo == hi) && (lo == (fgBBcount - 1)))
            {
                noway_assert(addr == dsc->bbCodeOffsEnd);
                return NULL;  // NULL means the end of method
            }
            lo = mid + 1;
            continue;
        }

        if  (pos > addr)
        {
            hi = mid - 1;
            continue;
        }

        return  dsc;
    }
#ifdef DEBUG
    printf("ERROR: Couldn't find basic block at offset %04X\n", addr);
#endif // DEBUG
    NO_WAY("fgLookupBB failed.");
}


/*****************************************************************************
 *
 *  The 'jump target' array uses the following flags to indicate what kind
 *  of label is present.
 */

#define JT_NONE         0x00        // This IL offset is never used
#define JT_ADDR         0x01        // merely make sure this is an OK address
#define JT_JUMP         0x02        // 'normal' jump target
#define JT_MULTI        0x04        // target of multiple jumps

inline
void                Compiler::fgMarkJumpTarget(BYTE* jumpTarget, unsigned offs)
{
    /* Make sure we set JT_MULTI if target of multiple jumps */

    noway_assert(JT_MULTI == JT_JUMP << 1);

    jumpTarget[offs] |= (jumpTarget[offs] & JT_JUMP) << 1 | JT_JUMP;
}

/*****************************************************************************
 *
 *  Walk the instrs and for any jumps we find set the appropriate entry
 *  in the 'jumpTarget' table.
 *  Also sets lvAddrExposed and lvArgWrite in lvaTable[]
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif

//Helpers to track stack transitions for conditional heuristics in fgFindJumpTargets
const unsigned fgStackSlotInvalid = UINT_MAX;
const unsigned fgStackSlotUnknown = 0;
const unsigned fgStackSlotConstant = 1;
const unsigned fgStackSlotArrayLen = 2;
const unsigned fgStackSlotFirstArg = 3;

class fgStack
{
public:
    fgStack() : depth(0)
    {
        slot0 = fgStackSlotInvalid;
        slot1 = fgStackSlotInvalid;
    }

    //Removes everything from the stack.
    inline void clear() { depth = 0; }
    inline void pushUnknown() { push(fgStackSlotUnknown); }
    inline void pushConstant() { push(fgStackSlotConstant); }
    inline void pushArrayLen() { push(fgStackSlotArrayLen); }
    inline void pushArgument(unsigned arg) { push(fgStackSlotFirstArg + arg); }
    inline unsigned getSlot0() { assert(depth >= 1); return slot0; }
    inline unsigned getSlot1() { assert(depth >= 2); return slot1; }
    inline static bool isConstant(unsigned value) { return value == fgStackSlotConstant;}
    inline static bool isArrayLen(unsigned value) { return value == fgStackSlotArrayLen;}
    inline static bool isArgument(unsigned value) { return value >= fgStackSlotFirstArg;}
    inline static unsigned slotTypeToArgNum(unsigned value) { return value - fgStackSlotFirstArg; }
    inline bool isStackTwoDeep() { return depth == 2; }
    inline bool isStackOneDeep() { return depth == 1; }

private:
    inline void push(unsigned type)
    {
        switch (depth)
        {
        case 0:
            ++depth;
            slot0 = type;
            break;
        case 1:
            ++depth;
            __fallthrough;
        case 2:
            slot1 = slot0;
            slot0 = type;
        }
    }
    unsigned slot0;
    unsigned slot1;
    unsigned depth;
};

//------------------------------------------------------------------------
// fgFindJumpTargets: walk the IL stream, determining jump target offsets
//
// Arguments:
//    codeAddr   - base address of the IL code buffer
//    codeSize   - number of bytes in the IL code buffer
//    jumpTarget - [OUT] byte array for flagging jump targets
//
// Notes:
//    If inlining or prejitting the root, this method also makes
//    various observations about the method that factor into inline
//    decisions. It sets `compNativeSizeEstimate` as a side effect.
//
//    May throw an exception if the IL is malformed.
//
//    jumpTarget[N] is set to a JT_* value if IL offset N is a 
//    jump target in the method.

void Compiler::fgFindJumpTargets(const BYTE* codeAddr,
                                 IL_OFFSET   codeSize,
                                 BYTE*       jumpTarget)
{
    const BYTE* codeBegp     = codeAddr;
    const BYTE* codeEndp     = codeAddr + codeSize;
    unsigned    varNum;
    bool        seenJump     = false;
    var_types   varType      = DUMMY_INIT(TYP_UNDEF);  // TYP_ type
    typeInfo    ti;                                // Verifier type.
    bool        typeIsNormed = false;
    unsigned    ldStCount    = 0; // Number of load/store instructions.
    fgStack     pushedStack;      // Keep track of constants and args on the stack.
    const bool  isForceInline = (info.compFlags & CORINFO_FLG_FORCEINLINE) != 0;

    if (compInlineResult != nullptr)
    {
        // Observe force inline state and code size.
        compInlineResult->NoteBool(InlineObservation::CALLEE_IS_FORCE_INLINE, isForceInline);
        compInlineResult->NoteInt(InlineObservation::CALLEE_IL_CODE_SIZE, codeSize);

#ifdef DEBUG

        // If inlining, this method should still be a candidate.
        if (compIsForInlining())
        {
            assert(compInlineResult->IsCandidate());
        }

#endif // DEBUG

        // note that we're starting to look at the opcodes.
        compInlineResult->Note(InlineObservation::CALLEE_BEGIN_OPCODE_SCAN);
    }

    while (codeAddr < codeEndp)
    {
        OPCODE opcode = (OPCODE) getU1LittleEndian(codeAddr);
        codeAddr += sizeof(__int8);
        opts.instrCount++;
        typeIsNormed = false;

DECODE_OPCODE:

        /* Get the size of additional parameters */

        if (opcode >= CEE_COUNT)
            BADCODE3("Illegal opcode", ": %02X", (int) opcode);

        // --- leave ldstcount for now, though it can be moved ---
        if ((opcode >= CEE_LDARG_0 && opcode <= CEE_STLOC_S) ||
            (opcode >= CEE_LDARG   && opcode <= CEE_STLOC))
        {
            opts.lvRefCount++;
            ++ldStCount;
        }
        //Check the rest of the LD/ST ranges
        else if (opcode >= CEE_LDNULL && opcode <= CEE_LDC_R8)
        {
            pushedStack.pushConstant();
            ++ldStCount;
        }
        else if ((opcode >= CEE_LDIND_I1 && opcode <= CEE_STIND_R8)
                 || (opcode >= CEE_LDFLD && opcode <= CEE_STOBJ)
                 || (opcode >= CEE_LDELEMA && opcode <= CEE_STELEM))
            //Don't count LDOBJ or LDSTR.  The former isn't exactly a load and the latter has issues with
            //interning.
        {
            ++ldStCount;
        }
        else if (opcode == CEE_POP)
        {
            ++ldStCount;
        }

        unsigned sz = opcodeSizes[opcode];

        switch (opcode)
        {
              signed        jmpDist;
            unsigned        jmpAddr;

            // For CEE_SWITCH
            unsigned        jmpBase;
            unsigned        jmpCnt;

        case CEE_PREFIX1:
            if (codeAddr >= codeEndp)
                goto TOO_FAR;
            opcode = (OPCODE) (256+getU1LittleEndian(codeAddr));
            codeAddr += sizeof(__int8);
            goto DECODE_OPCODE;

        case CEE_PREFIX2:
        case CEE_PREFIX3:
        case CEE_PREFIX4:
        case CEE_PREFIX5:
        case CEE_PREFIX6:
        case CEE_PREFIX7:
        case CEE_PREFIXREF:
            BADCODE3("Illegal opcode", ": %02X", (int) opcode);

        case CEE_CALL:
        case CEE_CALLVIRT:
            //If the method has a call followed by a ret, assume that it is
            //a wrapper method.
            if (compIsForInlining())
            {
                //There has to be code after the call, otherwise the inlinee is unverifiable.
                noway_assert(codeAddr < codeEndp - sz);
                if ((OPCODE) getU1LittleEndian(codeAddr + sz) == CEE_RET)
                {
                    compInlineResult->Note(InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER);
                }
            }
            break;


        /* Check for an unconditional jump opcode */

        case CEE_LEAVE:
        case CEE_LEAVE_S:
        case CEE_BR:
        case CEE_BR_S:

        /* Check for a conditional jump opcode */

        case CEE_BRFALSE:
        case CEE_BRFALSE_S:
        case CEE_BRTRUE:
        case CEE_BRTRUE_S:
        case CEE_BEQ:
        case CEE_BEQ_S:
        case CEE_BGE:
        case CEE_BGE_S:
        case CEE_BGE_UN:
        case CEE_BGE_UN_S:
        case CEE_BGT:
        case CEE_BGT_S:
        case CEE_BGT_UN:
        case CEE_BGT_UN_S:
        case CEE_BLE:
        case CEE_BLE_S:
        case CEE_BLE_UN:
        case CEE_BLE_UN_S:
        case CEE_BLT:
        case CEE_BLT_S:
        case CEE_BLT_UN:
        case CEE_BLT_UN_S:
        case CEE_BNE_UN:
        case CEE_BNE_UN_S:

            seenJump = true;

            if (codeAddr > codeEndp - sz)
                goto TOO_FAR;

            /* Compute the target address of the jump */

            jmpDist = (sz==1) ? getI1LittleEndian(codeAddr)
                              : getI4LittleEndian(codeAddr);

            if (compIsForInlining() && jmpDist == 0 && (opcode == CEE_LEAVE || opcode == CEE_LEAVE_S
                                                        || opcode == CEE_BR || opcode == CEE_BR_S))
                break;  /* NOP */

            jmpAddr = (IL_OFFSET)(codeAddr - codeBegp) + sz + jmpDist;

            /* Make sure the target address is reasonable */

            if  (jmpAddr >= codeSize)
            {
                BADCODE3("code jumps to outer space",
                         " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
            }

            /* Finally, set the 'jump target' flag */

            fgMarkJumpTarget(jumpTarget, jmpAddr);

            if (compIsForInlining() && opcode != CEE_BR_S && opcode != CEE_BR)
                goto INL_HANDLE_COMPARE;

            break;

        case CEE_SWITCH:

            seenJump = true;

            if (compIsForInlining())
            {
                compInlineResult->Note(InlineObservation::CALLEE_HAS_SWITCH);
                if (compInlineResult->IsFailure()) 
                {
                    return;
                }
            }

            // Make sure we don't go past the end reading the number of cases

            if  (codeAddr > codeEndp - sizeof(DWORD))
                goto TOO_FAR;

            // Read the number of cases

            jmpCnt = getU4LittleEndian(codeAddr);
            codeAddr += sizeof(DWORD);

            if (jmpCnt > codeSize / sizeof(DWORD))
                goto TOO_FAR;

            // Find the end of the switch table

            jmpBase = (unsigned)((codeAddr - codeBegp) + jmpCnt*sizeof(DWORD));

            /* Make sure we have room for the switch table */

            if  (jmpBase >= codeSize)
                goto TOO_FAR;

            // jmpBase is also the target of the default case, so mark it

            fgMarkJumpTarget(jumpTarget, jmpBase);

            /* Process all the entries in the jump table */

            while (jmpCnt)
            {
                jmpAddr = jmpBase + getI4LittleEndian(codeAddr);
                codeAddr += 4;

                if  (jmpAddr >= codeSize)
                    BADCODE3("jump target out of range",
                             " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));

                fgMarkJumpTarget(jumpTarget, jmpAddr);

                jmpCnt--;
            }

            /* We've now consumed the entire switch opcode */

            goto _SkipCodeAddrAdjustment;

        case CEE_UNALIGNED:
        case CEE_CONSTRAINED:
        case CEE_READONLY:
        case CEE_VOLATILE:
        case CEE_TAILCALL:
            if (codeAddr >= codeEndp)
                goto TOO_FAR;
            break;

        case CEE_STARG:
        case CEE_STARG_S:     goto ARG_WRITE;

        case CEE_LDARGA:
        case CEE_LDARGA_S:
        case CEE_LDLOCA:
        case CEE_LDLOCA_S:    goto ADDR_TAKEN;

        // Other opcodes that we know inliner won't handle.
        case CEE_THROW:
            if (seenJump)
                break;
        case CEE_ISINST:
        case CEE_CASTCLASS:
        case CEE_SIZEOF:
        case CEE_LDTOKEN:
        case CEE_UNBOX:
            //Needs weight value in SMWeights.cpp
        case CEE_UNBOX_ANY:
            break;

        case CEE_JMP:
#if !defined(_TARGET_X86_) && !defined(_TARGET_ARM_)
            if (!compIsForInlining())
            {
                // We transform this into a set of ldarg's + tail call and
                // thus may push more onto the stack than originally thought.
                // This doesn't interfere with verification because CEE_JMP
                // is never verifiable, and there's nothing unsafe you can
                // do with a an IL stack overflow if the JIT is expecting it.
                info.compMaxStack = max(info.compMaxStack, info.compILargsCount);
                break;
            }
#endif // !_TARGET_X86_ && !_TARGET_ARM_

            // If we are inlining, we need to fail for a CEE_JMP opcode, just like
            // the list of other opcodes (for all platforms).

            __fallthrough;

        // CEE_CALLI should not be inlined because the JIT cannot generate an inlined call frame. If the call target
        // is a no-marshal CALLI P/Invoke we end up calling the IL stub. We don't NGEN these stubs, so we'll have to
        // JIT an IL stub for a trivial func. It's almost certainly a better choice to leave out the inline candidate
        // so we can generate an inlined call frame. It might be nice to call getCallInfo to figure out what kind of
        // call we have here.
        case CEE_CALLI:
            //Needs weight value in SMWeights.cpp
        case CEE_LOCALLOC:
        case CEE_CPBLK:
        case CEE_MKREFANY:
        case CEE_RETHROW:
            //Consider making this only for not force inline.
            if (compIsForInlining())
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_UNSUPPORTED_OPCODE);
                return;
            }
            break;

        case CEE_LDARG_0:
            varNum = 0; goto ARG_PUSH;
        case CEE_LDARG_1:
            varNum = 1; goto ARG_PUSH;
        case CEE_LDARG_2:
            varNum = 2; goto ARG_PUSH;
        case CEE_LDARG_3:
            varNum = 3; goto ARG_PUSH;
        case CEE_LDARG_S:
        case CEE_LDARG:
            if (codeAddr > codeEndp - sz)
                goto TOO_FAR;
            varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr)
                                          : getU2LittleEndian(codeAddr);
            goto ARG_PUSH;

        case CEE_LDLEN:
            if (compIsForInlining())
                pushedStack.pushArrayLen();

            break;
        case CEE_CEQ:
        case CEE_CGT:
        case CEE_CGT_UN:
        case CEE_CLT:
        case CEE_CLT_UN:
            if (compIsForInlining())
                goto INL_HANDLE_COMPARE;

            break;

        default:
            break;

INL_HANDLE_COMPARE:
            noway_assert(compIsForInlining());
            //We're looking at a comparison.  There are several cases that we would like to recognize for
            //inlining:
            //  Static cases
            //      - An incoming argument is compared against a constant
            //      - An incoming argument is compared against an array length
            //
            //  Dynamic cases
            //      - An incoming argument which is a constant is used in a comparison.

            {
                if (!pushedStack.isStackTwoDeep())
                {
                    //The stack only has to be 1 deep for BRTRUE/FALSE
                    if (pushedStack.isStackOneDeep())
                    {
                        if (opcode == CEE_BRFALSE || opcode == CEE_BRFALSE_S ||
                            opcode == CEE_BRTRUE || opcode == CEE_BRTRUE_S)
                        {
                            unsigned slot0 = pushedStack.getSlot0();
                            if (fgStack::isArgument(slot0))
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);
                                //Check for the double whammy of an incoming constant argument feeding a
                                //constant test.
                                varNum = fgStack::slotTypeToArgNum(slot0);
                                if (impInlineInfo->inlArgInfo[varNum].argNode->OperIsConst())
                                {
                                    compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
                                }
                            }
                        }
                    }
                    break;
                }

                unsigned slot0 = pushedStack.getSlot0();
                unsigned slot1 = pushedStack.getSlot1();

                //Arg feeds constant test.
                if ((fgStack::isConstant(slot0) && fgStack::isArgument(slot1))
                    ||(fgStack::isConstant(slot1) && fgStack::isArgument(slot0)))
                {
                    compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);
                }
                //Arg feeds range check
                if ((fgStack::isArrayLen(slot0) && fgStack::isArgument(slot1))
                    ||(fgStack::isArrayLen(slot1) && fgStack::isArgument(slot0)))
                {
                    compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK);
                }

                //Check for an incoming arg that's a constant.
                if (fgStack::isArgument(slot0))
                {
                    varNum = fgStack::slotTypeToArgNum(slot0);
                    if (impInlineInfo->inlArgInfo[varNum].argNode->OperIsConst())
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
                    }
                }
                if (fgStack::isArgument(slot1))
                {
                    varNum = fgStack::slotTypeToArgNum(slot1);
                    if (impInlineInfo->inlArgInfo[varNum].argNode->OperIsConst())
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
                    }
                }
            }
            break;
ARG_PUSH:
            if (compIsForInlining())
            {
                pushedStack.pushArgument(varNum);
            }

            break;

ADDR_TAKEN:
            noway_assert(sz == sizeof(BYTE) || sz == sizeof(WORD));
            if (codeAddr > codeEndp - sz)
                goto TOO_FAR;
            varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr)
                                          : getU2LittleEndian(codeAddr);


            if (compIsForInlining())
            {
                if (opcode == CEE_LDLOCA   ||
                    opcode == CEE_LDLOCA_S)
                {
                    varType = impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclTypeInfo;
                    ti      = impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclVerTypeInfo;

                    impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclHasLdlocaOp = true;
                }
                else
                {
                    noway_assert(opcode == CEE_LDARGA || opcode == CEE_LDARGA_S);

                    varType = impInlineInfo->lclVarInfo[varNum].lclTypeInfo;
                    ti      = impInlineInfo->lclVarInfo[varNum].lclVerTypeInfo;

                    impInlineInfo->inlArgInfo[varNum].argHasLdargaOp = true;

                    pushedStack.pushArgument(varNum);
                }
            }
            else
            {
                if (opcode == CEE_LDLOCA || opcode == CEE_LDLOCA_S)
                {
                    if (varNum >= info.compMethodInfo->locals.numArgs)
                        BADCODE("bad local number");

                    varNum += info.compArgsCount;
                }
                else
                {
                    noway_assert(opcode == CEE_LDARGA || opcode == CEE_LDARGA_S);
                    if (varNum >= info.compILargsCount)
                        BADCODE("bad argument number");

                    varNum = compMapILargNum(varNum); // account for possible hidden param
                }

                varType = (var_types)lvaTable[varNum].lvType;
                ti      = lvaTable[varNum].lvVerTypeInfo;

                if (!varTypeIsStruct(&lvaTable[varNum]) && // We will put structs in the stack anyway
                                                                // And changing the addrTaken of a local
                                                                // requires an extra pass in the morpher
                                                                // so we won't apply this optimization
                                                                // to structs.
                    codeAddr < codeEndp - sz    && // This is not the last instruction
                    impILConsumesAddr(codeAddr + sz, impTokenLookupContextHandle, info.compScopeHnd) &&
                    opts.compDbgCode == false)   // Debug code spills for every IL instruction, and therefore
                                                 // it will split statements, so we will need the address.
                                                 // Note that this optimization is based in that we know
                                                 // what trees we will generate for this ldfld, and we
                                                 // require that we won't need the address of this local
                                                 // at all
                {
                    // We can skip the addrtaken, as next IL instruction consumes
                    // the address. 
#ifdef DEBUG
                    noway_assert(varNum < lvaTableCnt);
#endif
                }
                else
                {
                    noway_assert(varNum < lvaTableCnt);
                    lvaTable[varNum].lvHasLdAddrOp = 1;
                    if (!info.compIsStatic &&
                        varNum == 0)
                    {
                        // Addr taken on "this" pointer is significant,
                        // go ahead to mark it as permanently addr-exposed here.
                        lvaSetVarAddrExposed(0);
                        // This may be conservative, but probably not very.
                    }
                }
            } // compIsForInlining()

            typeIsNormed = ti.IsValueClass() && !varTypeIsStruct(varType);
            break;

ARG_WRITE:
            if (compIsForInlining())
            {

#ifdef DEBUG
                if (verbose)
                {
                    printf("\n\nInline expansion aborted due to opcode at offset [%02u] which writes to an argument\n",
                           codeAddr-codeBegp-1);
                }
#endif

                /* The inliner keeps the args as trees and clones them.  Storing the arguments breaks that
                 * simplification.  To allow this, flag the argument as written to and spill it before
                 * inlining.  That way the STARG in the inlinee is trivial. */
                compInlineResult->NoteFatal(InlineObservation::CALLEE_STORES_TO_ARGUMENT);
                return;
            }
            else
            {
                noway_assert(sz == sizeof(BYTE) || sz == sizeof(WORD));
                if (codeAddr > codeEndp - sz)
                goto TOO_FAR;
                varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr)
                                              : getU2LittleEndian(codeAddr);
                varNum = compMapILargNum(varNum); // account for possible hidden param

                // This check is only intended to prevent an AV.  Bad varNum values will later
                // be handled properly by the verifier.
                if (varNum < lvaTableCnt)
                    lvaTable[varNum].lvArgWrite = 1;
            }
            break;
        }

        /* Skip any operands this opcode may have */

        codeAddr += sz;

_SkipCodeAddrAdjustment:
        ;

        if (compInlineResult != nullptr)
        {
            InlineObservation obs = typeIsNormed ?
                InlineObservation::CALLEE_OPCODE_NORMED : InlineObservation::CALLEE_OPCODE;
            compInlineResult->NoteInt(obs, opcode);
        }
    }

    if  (codeAddr != codeEndp)
    {
TOO_FAR:
        BADCODE3("Code ends in the middle of an opcode, or there is a branch past the end of the method",
                 " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
    }

    //If this function is mostly loads and stores, we should try harder to inline it.  You can't just use
    //the percentage test because if the method has 8 instructions and 6 are loads, it's only 75% loads.
    //This allows for CALL, RET, and one more non-ld/st instruction.
    if ((opts.instrCount - ldStCount) < 4 || ((double)ldStCount/(double)opts.instrCount) > .90)
    {
        // Note this is the one and only case where we don't guard the
        // observation with compIsForInlining(). The prejit root must
        // also make this observation. We'll fix this eventually as we
        // make the LegacyPolicy smarter about what observations it
        // cares about, and when.
        if (compInlineResult != nullptr)
        {
            compInlineResult->Note(InlineObservation::CALLEE_IS_MOSTLY_LOAD_STORE);
        }
    }

    if (compInlineResult != nullptr)
    {
        compInlineResult->Note(InlineObservation::CALLEE_END_OPCODE_SCAN);

        // If the inline is viable and discretionary, do the
        // profitability screening.
        if (compInlineResult->IsDiscretionaryCandidate())
        {
            // Make some callsite specific observations that will feed
            // into the profitability model.
            impMakeDiscretionaryInlineObservations(impInlineInfo, compInlineResult);

            // None of those observations should have changed the
            // inline's viability.
            assert(compInlineResult->IsCandidate());

            if (compIsForInlining())
            {
                // Assess profitability...
                CORINFO_METHOD_INFO* methodInfo = &impInlineInfo->inlineCandidateInfo->methInfo;
                compInlineResult->DetermineProfitability(methodInfo);
                
                if (compInlineResult->IsFailure())
                {
                    JITDUMP("\n\nInline expansion aborted, inline not profitable\n");
                    return;
                }
                else
                {
                    // The inline is still viable.
                    assert(compInlineResult->IsCandidate());
                }
            }
            else
            {
                // Prejit root case. Profitability assessment for this
                // is done over in compCompileHelper.
            }
        }
    }

    if (!compIsForInlining() && // None of the local vars in the inlinee should have address taken or been written to.
                                // Therefore we should NOT need to enter this "if" statement.
        !info.compIsStatic)
    {
        //If we're verifying, then we can't do this.  This flag makes the method unverifiable from the
        //Importer's point of view.
        if (!tiVerificationNeeded && compStressCompile(STRESS_GENERIC_VARN, 15))
            lvaTable[info.compThisArg].lvArgWrite = true;

        if (lvaTable[info.compThisArg].lvAddrExposed || lvaTable[info.compThisArg].lvArgWrite)
        {
            // If there is a "ldarga 0" or "starg 0", grab and use the temp.
            lvaArg0Var = lvaGrabTemp(false DEBUGARG("Address-exposed, or written, this pointer"));
            noway_assert(lvaArg0Var > (unsigned)info.compThisArg);
            lvaTable[lvaArg0Var].lvType = lvaTable[info.compThisArg].TypeGet();
            lvaTable[lvaArg0Var].lvAddrExposed = lvaTable[info.compThisArg].lvAddrExposed;
            lvaTable[lvaArg0Var].lvDoNotEnregister = lvaTable[info.compThisArg].lvDoNotEnregister;
#ifdef DEBUG
            lvaTable[lvaArg0Var].lvVMNeedsStackAddr = lvaTable[info.compThisArg].lvVMNeedsStackAddr;
            lvaTable[lvaArg0Var].lvLiveInOutOfHndlr = lvaTable[info.compThisArg].lvLiveInOutOfHndlr;
            lvaTable[lvaArg0Var].lvLclFieldExpr = lvaTable[info.compThisArg].lvLclFieldExpr;
            lvaTable[lvaArg0Var].lvLiveAcrossUCall = lvaTable[info.compThisArg].lvLiveAcrossUCall;
#endif
            lvaTable[lvaArg0Var].lvArgWrite = lvaTable[info.compThisArg].lvArgWrite;
            lvaTable[lvaArg0Var].lvVerTypeInfo = lvaTable[info.compThisArg].lvVerTypeInfo;

            // Clear the TI_FLAG_THIS_PTR in the original 'this' pointer.
            noway_assert(lvaTable[lvaArg0Var].lvVerTypeInfo.IsThisPtr());
            lvaTable[info.compThisArg].lvVerTypeInfo.ClearThisPtr();
            lvaTable[info.compThisArg].lvAddrExposed = false;
            lvaTable[info.compThisArg].lvArgWrite = false;
        }
    }

    return;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Finally link up the bbJumpDest of the blocks together
 */

void            Compiler::fgMarkBackwardJump(BasicBlock * startBlock, BasicBlock * endBlock)
{
    noway_assert(startBlock->bbNum <= endBlock->bbNum);

    for (BasicBlock * block = startBlock;
         block != endBlock->bbNext;
         block = block->bbNext)
    {
        if ((block->bbFlags & BBF_BACKWARD_JUMP) == 0)
            block->bbFlags |= BBF_BACKWARD_JUMP;
    }
}

/*****************************************************************************
 *
 *  Finally link up the bbJumpDest of the blocks together
 */

void            Compiler::fgLinkBasicBlocks()
{
    /* Create the basic block lookup tables */

    fgInitBBLookup();

    /* First block is always reachable */

    fgFirstBB->bbRefs = 1;

    /* Walk all the basic blocks, filling in the target addresses */

    for (BasicBlock * curBBdesc = fgFirstBB;
         curBBdesc;
         curBBdesc = curBBdesc->bbNext)
    {
        switch (curBBdesc->bbJumpKind)
        {
        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_LEAVE:
            curBBdesc->bbJumpDest = fgLookupBB(curBBdesc->bbJumpOffs);
            curBBdesc->bbJumpDest->bbRefs++;
            if (curBBdesc->bbJumpDest->bbNum <= curBBdesc->bbNum)
            {
                fgMarkBackwardJump(curBBdesc->bbJumpDest, curBBdesc);
            }

            /* Is the next block reachable? */

            if  (curBBdesc->bbJumpKind == BBJ_ALWAYS ||
                 curBBdesc->bbJumpKind == BBJ_LEAVE)
                break;

            if  (!curBBdesc->bbNext)
                BADCODE("Fall thru the end of a method");

            // Fall through, the next block is also reachable

        case BBJ_NONE:
            curBBdesc->bbNext->bbRefs++;
            break;

        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:
        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:

            unsigned        jumpCnt; jumpCnt = curBBdesc->bbJumpSwt->bbsCount;
            BasicBlock * *  jumpPtr; jumpPtr = curBBdesc->bbJumpSwt->bbsDstTab;

            do
            {
                *jumpPtr = fgLookupBB((unsigned)*(size_t*)jumpPtr);
                (*jumpPtr)->bbRefs++;
                if ((*jumpPtr)->bbNum <= curBBdesc->bbNum)
                {
                    fgMarkBackwardJump(*jumpPtr, curBBdesc);
                }
            }
            while (++jumpPtr, --jumpCnt);

            /* Default case of CEE_SWITCH (next block), is at end of jumpTab[] */

            noway_assert(*(jumpPtr-1) == curBBdesc->bbNext);
            break;

        case BBJ_CALLFINALLY:   // BBJ_CALLFINALLY and BBJ_EHCATCHRET don't appear until later
        case BBJ_EHCATCHRET:
        default:
            noway_assert(!"Unexpected bbJumpKind");
            break;
        }
    }
}


/*****************************************************************************
 *
 *  Walk the instrs to create the basic blocks.
 */

void                Compiler::fgMakeBasicBlocks(const BYTE * codeAddr,
                                                IL_OFFSET    codeSize,
                                                BYTE *       jumpTarget)
{
    const   BYTE *  codeBegp = codeAddr;
    const   BYTE *  codeEndp = codeAddr + codeSize;
    bool            tailCall = false;
    unsigned        curBBoffs;
    BasicBlock  *   curBBdesc;

    /* Clear the beginning offset for the first BB */

    curBBoffs = 0;

#ifdef DEBUGGING_SUPPORT
    if (opts.compDbgCode && (info.compVarScopesCount > 0))
    {
        compResetScopeLists();

        // Ignore scopes beginning at offset 0
        while (compGetNextEnterScope(0)) { /* do nothing */ }
        while (compGetNextExitScope(0))  { /* do nothing */ }
    }
#endif


    BBjumpKinds jmpKind;

    do
    {
        OPCODE          opcode;
        unsigned        sz;
        unsigned        jmpAddr = DUMMY_INIT(BAD_IL_OFFSET);
        unsigned        bbFlags = 0;
        BBswtDesc   *   swtDsc = 0;
        unsigned        nxtBBoffs;

        opcode    = (OPCODE) getU1LittleEndian(codeAddr);
        codeAddr += sizeof(__int8);
        jmpKind    = BBJ_NONE;

DECODE_OPCODE:

        /* Get the size of additional parameters */

        noway_assert(opcode < CEE_COUNT);

        sz = opcodeSizes[opcode];

        switch (opcode)
        {
            signed        jmpDist;


        case CEE_PREFIX1:
            if (jumpTarget[codeAddr - codeBegp] != JT_NONE)
                BADCODE3("jump target between prefix 0xFE and opcode",
                         " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));

            opcode = (OPCODE) (256+getU1LittleEndian(codeAddr));
            codeAddr += sizeof(__int8);
            goto DECODE_OPCODE;

        /* Check to see if we have a jump/return opcode */

        case CEE_BRFALSE:
        case CEE_BRFALSE_S:
        case CEE_BRTRUE:
        case CEE_BRTRUE_S:

        case CEE_BEQ:
        case CEE_BEQ_S:
        case CEE_BGE:
        case CEE_BGE_S:
        case CEE_BGE_UN:
        case CEE_BGE_UN_S:
        case CEE_BGT:
        case CEE_BGT_S:
        case CEE_BGT_UN:
        case CEE_BGT_UN_S:
        case CEE_BLE:
        case CEE_BLE_S:
        case CEE_BLE_UN:
        case CEE_BLE_UN_S:
        case CEE_BLT:
        case CEE_BLT_S:
        case CEE_BLT_UN:
        case CEE_BLT_UN_S:
        case CEE_BNE_UN:
        case CEE_BNE_UN_S:

            jmpKind = BBJ_COND;
            goto JMP;


        case CEE_LEAVE:
        case CEE_LEAVE_S:

            // We need to check if we are jumping out of a finally-protected try.
            jmpKind = BBJ_LEAVE;
            goto JMP;


        case CEE_BR:
        case CEE_BR_S:
            jmpKind = BBJ_ALWAYS;
            goto JMP;

        JMP:

            /* Compute the target address of the jump */

            jmpDist = (sz==1) ? getI1LittleEndian(codeAddr)
                              : getI4LittleEndian(codeAddr);

            if (compIsForInlining() && jmpDist == 0 && (opcode == CEE_BR || opcode == CEE_BR_S))
                continue;    /* NOP */

            jmpAddr = (IL_OFFSET)(codeAddr - codeBegp) + sz + jmpDist;
            break;

        case CEE_SWITCH:
            {
                unsigned        jmpBase;
                unsigned        jmpCnt; // # of switch cases (excluding defualt)

                BasicBlock * *  jmpTab;
                BasicBlock * *  jmpPtr;

                /* Allocate the switch descriptor */

                swtDsc = new (this, CMK_BasicBlock) BBswtDesc;

                /* Read the number of entries in the table */

                jmpCnt = getU4LittleEndian(codeAddr); codeAddr += 4;

                /* Compute  the base offset for the opcode */

                jmpBase = (IL_OFFSET)((codeAddr - codeBegp) + jmpCnt*sizeof(DWORD));

                /* Allocate the jump table */

                jmpPtr = jmpTab = new (this, CMK_BasicBlock) BasicBlock*[jmpCnt+1];

                /* Fill in the jump table */

                for (unsigned count = jmpCnt; count; count--)
                {
                    jmpDist   = getI4LittleEndian(codeAddr);
                    codeAddr += 4;

                    //store the offset in the pointer.  We change these in fgLinkBasicBlocks().
                    *jmpPtr++ = (BasicBlock*)(size_t)(jmpBase + jmpDist);
                }

                /* Append the default label to the target table */

                *jmpPtr++ = (BasicBlock*)(size_t)jmpBase;

                /* Make sure we found the right number of labels */

                noway_assert(jmpPtr == jmpTab + jmpCnt + 1);

                /* Compute the size of the switch opcode operands */

                sz = sizeof(DWORD) + jmpCnt*sizeof(DWORD);

                /* Fill in the remaining fields of the switch descriptor */

                swtDsc->bbsCount  = jmpCnt + 1;
                swtDsc->bbsDstTab = jmpTab;

                /* This is definitely a jump */

                jmpKind = BBJ_SWITCH;
                fgHasSwitch = true;

#ifndef LEGACY_BACKEND
                if (opts.compProcedureSplitting)
                {
                    // TODO-CQ: We might need to create a switch table; we won't know for sure until much later. However, switch tables
                    // don't work with hot/cold splitting, currently. The switch table data needs a relocation such that if the base
                    // (the first block after the prolog) and target of the switch branch are put in different sections, the difference
                    // stored in the table is updated. However, our relocation implementation doesn't support three different pointers
                    // (relocation address, base, and target). So, we need to change our switch table implementation to be more like
                    // JIT64: put the table in the code section, in the same hot/cold section as the switch jump itself (maybe
                    // immediately after the switch jump), and make the "base" address be also in that section, probably the
                    // address after the switch jump.
                    opts.compProcedureSplitting = false;
                    JITDUMP("Turning off procedure splitting for this method, as it might need switch tables; implementation limitation.\n");
                }
#endif // !LEGACY_BACKEND

            }
            goto GOT_ENDP;

        case CEE_ENDFILTER:
            bbFlags |= BBF_DONT_REMOVE;
            jmpKind = BBJ_EHFILTERRET;
            break;

        case CEE_ENDFINALLY:
            jmpKind = BBJ_EHFINALLYRET;
            break;

        case CEE_READONLY:
        case CEE_CONSTRAINED:
        case CEE_TAILCALL:
        case CEE_VOLATILE:
        case CEE_UNALIGNED:
            // fgFindJumpTargets should have ruled out this possibility
            //   (i.e. a prefix opcodes as last intruction in a block)
            noway_assert(codeAddr < codeEndp);

            if (jumpTarget[codeAddr - codeBegp] != JT_NONE)
                BADCODE3("jump target between prefix and an opcode",
                        " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
            break;

        case CEE_CALL:
        case CEE_CALLVIRT:
        case CEE_CALLI:
            {
                if (compIsForInlining() || // Ignore tail call in the inlinee. Period.
                    (!tailCall &&
                    !compTailCallStress())  // A new BB with BBJ_RETURN would have been created

                    // after a tailcall statement.
                    // We need to keep this invariant if we want to stress the tailcall.
                    // That way, the potential (tail)call statement is always the last
                    // statement in the block.
                    // Otherwise, we will assert at the following line in fgMorphCall()
                    //     noway_assert(fgMorphStmt->gtNext == NULL);
                    )
                {
                    // Neither .tailcall prefix, no tailcall stress. So move on.
                    break;
                }

                // Make sure the code sequence is legal for the tail call.
                // If so, mark this BB as having a BBJ_RETURN.

                if (codeAddr >= codeEndp - sz) {
                    BADCODE3("No code found after the call instruction",
                        " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
                }
               
                if (tailCall)
                {
                    bool isCallPopAndRet = false;

                    // impIsTailCallILPattern uses isRecursive flag to determine whether ret in a fallthrough block is
                    // allowed. We don't know at this point whether the call is recursive so we conservatively pass false.
                    // This will only affect explicit tail calls when IL verification is not needed for the method.
                    bool isRecursive = false;
                    if (!impIsTailCallILPattern(tailCall, opcode, codeAddr+sz, codeEndp, isRecursive, &isCallPopAndRet))
                    {
#ifdef _TARGET_AMD64_
                        BADCODE3("tail call not followed by ret or pop+ret",
                            " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
#else
                        BADCODE3("tail call not followed by ret",
                            " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
#endif //_TARGET_AMD64_
                    }

#ifdef _TARGET_AMD64_
                    if (isCallPopAndRet)
                    {
                        // By breaking here, we let pop and ret opcodes to be
                        // imported after tail call.  If tail prefix is honored,
                        // stmts corresponding to pop and ret will be removed
                        // in fgMorphCall().
                        break;
                    }
#endif //_TARGET_AMD64_
                }
                else
                {
                    OPCODE nextOpcode = (OPCODE) getU1LittleEndian(codeAddr + sz);

                    if (nextOpcode != CEE_RET)
                    {
                        noway_assert(compTailCallStress());
                        // Next OPCODE is not a CEE_RET, bail the attempt to stress the tailcall.
                        // (I.e. We will not make a new BB after the "call" statement.)
                        break;
                    }
                }
            }

            /* For tail call, we just call CORINFO_HELP_TAILCALL, and it jumps to the
               target. So we don't need an epilog - just like CORINFO_HELP_THROW.
               Make the block BBJ_RETURN, but we will change it to BBJ_THROW
               if the tailness of the call is satisfied.
               NOTE : The next instruction is guaranteed to be a CEE_RET
               and it will create another BasicBlock. But there may be an
               jump directly to that CEE_RET. If we want to avoid creating
               an unnecessary block, we need to check if the CEE_RETURN is
               the target of a jump.
             */

            // fall-through

        case CEE_JMP:
            /* These are equivalent to a return from the current method
               But instead of directly returning to the caller we jump and
               execute something else in between */
        case CEE_RET:
            jmpKind = BBJ_RETURN;
            break;

        case CEE_THROW:
        case CEE_RETHROW:
            jmpKind  = BBJ_THROW;
            break;

#ifdef DEBUG
        // make certain we did not forget any flow of control instructions
        // by checking the 'ctrl' field in opcode.def. First filter out all
        // non-ctrl instructions
#       define BREAK(name)          case name: break;
#       define NEXT(name)           case name: break;
#       define CALL(name)
#       define THROW(name)
#       undef  RETURN           // undef contract RETURN macro
#       define RETURN(name)
#       define META(name)
#       define BRANCH(name)
#       define COND_BRANCH(name)
#       define PHI(name)

#       define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) ctrl(name)
#       include "opcode.def"
#       undef OPDEF

#       undef PHI
#       undef BREAK
#       undef CALL
#       undef NEXT
#       undef THROW
#       undef RETURN
#       undef META
#       undef BRANCH
#       undef COND_BRANCH

        // These ctrl-flow opcodes don't need any special handling
        case CEE_NEWOBJ:    // CTRL_CALL
            break;

        // what's left are forgotten instructions
        default:
            BADCODE("Unrecognized control Opcode");
            break;
#else // !DEBUG
        default:
            break;
#endif // !DEBUG
        }

        /* Jump over the operand */

        codeAddr += sz;

GOT_ENDP:

        tailCall = (opcode == CEE_TAILCALL);

        /* Make sure a jump target isn't in the middle of our opcode */

        if  (sz)
        {
            IL_OFFSET offs = (IL_OFFSET)(codeAddr - codeBegp) - sz; // offset of the operand

            for (unsigned i=0; i<sz; i++, offs++)
            {
                if  (jumpTarget[offs] != JT_NONE)
                    BADCODE3("jump into the middle of an opcode",
                             " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
            }
        }

        /* Compute the offset of the next opcode */

        nxtBBoffs = (IL_OFFSET)(codeAddr - codeBegp);

#ifdef DEBUGGING_SUPPORT

        bool foundScope     = false;

        if (opts.compDbgCode && (info.compVarScopesCount > 0))
        {
            while (compGetNextEnterScope(nxtBBoffs))  foundScope = true;
            while (compGetNextExitScope(nxtBBoffs))   foundScope = true;
        }
#endif

        /* Do we have a jump? */

        if  (jmpKind == BBJ_NONE)
        {
            /* No jump; make sure we don't fall off the end of the function */

            if  (codeAddr == codeEndp)
                BADCODE3("missing return opcode",
                         " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));

            /* If a label follows this opcode, we'll have to make a new BB */

            bool makeBlock = (jumpTarget[nxtBBoffs] != JT_NONE);

#ifdef DEBUGGING_SUPPORT
            if (!makeBlock && foundScope)
            {
                makeBlock = true;
#ifdef DEBUG
                if (verbose)
                    printf("Splitting at BBoffs = %04u\n", nxtBBoffs);
#endif // DEBUG
            }
#endif // DEBUGGING_SUPPORT

            if (!makeBlock)
                continue;
        }

        /* We need to create a new basic block */

        curBBdesc = fgNewBasicBlock(jmpKind);

        curBBdesc->bbFlags   |= bbFlags;
        curBBdesc->bbRefs     = 0;

        curBBdesc->bbCodeOffs    = curBBoffs;
        curBBdesc->bbCodeOffsEnd = nxtBBoffs;

        unsigned profileWeight;
        if (fgGetProfileWeightForBasicBlock(curBBoffs, &profileWeight))
        {
            curBBdesc->setBBProfileWeight(profileWeight);
            if (profileWeight == 0)
            {
                curBBdesc->bbSetRunRarely();
            }
            else
            {
                // Note that bbNewBasicBlock (called from fgNewBasicBlock) may have
                // already marked the block as rarely run.  In that case (and when we know
                // that the block profile weight is non-zero) we want to unmark that.

                curBBdesc->bbFlags &= ~BBF_RUN_RARELY;
            }
        }

        switch (jmpKind)
        {
        case BBJ_SWITCH:
            curBBdesc->bbJumpSwt  = swtDsc;
            break;

        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_LEAVE:
            noway_assert(jmpAddr != DUMMY_INIT(BAD_IL_OFFSET));
            curBBdesc->bbJumpOffs = jmpAddr;
            break;

        default:
            break;
        }

        DBEXEC(verbose, curBBdesc->dspBlockHeader(this, false, false, false));

        /* Remember where the next BB will start */

        curBBoffs = nxtBBoffs;
    }
    while (codeAddr <  codeEndp);

    noway_assert(codeAddr == codeEndp);

    /* Finally link up the bbJumpDest of the blocks together */

    fgLinkBasicBlocks();
}


/*****************************************************************************
 *
 *  Main entry point to discover the basic blocks for the current function.
 */

void          Compiler::fgFindBasicBlocks()
{
#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgFindBasicBlocks() for %s\n",
               info.compFullName);
#endif

    /* Allocate the 'jump target' vector
     *
     *  We need one extra byte as we mark
     *  jumpTarget[info.compILCodeSize] with JT_ADDR
     *  when we need to add a dummy block
     *  to record the end of a try or handler region.
     */
    BYTE* jumpTarget = new (this, CMK_Unknown) BYTE[info.compILCodeSize+1];
    memset(jumpTarget, JT_NONE, info.compILCodeSize+1);
    noway_assert(JT_NONE == 0);

    /* Walk the instrs to find all jump targets */

    fgFindJumpTargets(info.compCode, info.compILCodeSize, jumpTarget);
    if (compDonotInline())
        return;

    unsigned  XTnum;

    /* Are there any exception handlers? */

    if  (info.compXcptnsCount > 0)
    {
        noway_assert(!compIsForInlining());

        /* Check and mark all the exception handlers */

        for (XTnum = 0; XTnum < info.compXcptnsCount; XTnum++)
        {
            DWORD tmpOffset;
            CORINFO_EH_CLAUSE clause;
            info.compCompHnd->getEHinfo(info.compMethodHnd, XTnum, &clause);
            noway_assert(clause.HandlerLength !=(unsigned) -1);

            if (clause.TryLength <= 0)
                BADCODE("try block length <=0");

            /* Mark the 'try' block extent and the handler itself */

            if (clause.TryOffset > info.compILCodeSize)
                BADCODE("try offset is > codesize");
            if  (jumpTarget[clause.TryOffset       ] == JT_NONE)
                 jumpTarget[clause.TryOffset       ] =  JT_ADDR;

            tmpOffset = clause.TryOffset + clause.TryLength;
            if (tmpOffset > info.compILCodeSize)
                BADCODE("try end is > codesize");
            if  (jumpTarget[tmpOffset              ] == JT_NONE)
                 jumpTarget[tmpOffset              ] =  JT_ADDR;

            if (clause.HandlerOffset > info.compILCodeSize)
                BADCODE("handler offset > codesize");
            if  (jumpTarget[clause.HandlerOffset   ] == JT_NONE)
                 jumpTarget[clause.HandlerOffset   ] =  JT_ADDR;

            tmpOffset = clause.HandlerOffset + clause.HandlerLength;
            if (tmpOffset > info.compILCodeSize)
                BADCODE("handler end > codesize");
            if  (jumpTarget[tmpOffset              ] == JT_NONE)
                 jumpTarget[tmpOffset              ] =  JT_ADDR;

            if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
            {
                if (clause.FilterOffset > info.compILCodeSize)
                    BADCODE("filter offset > codesize");
                if (jumpTarget[clause.FilterOffset ] == JT_NONE)
                    jumpTarget[clause.FilterOffset ] =  JT_ADDR;
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        bool anyJumpTargets = false;
        printf("Jump targets:\n");
        for (unsigned i = 0; i < info.compILCodeSize + 1; i++)
        {
            if (jumpTarget[i] == JT_NONE)
                continue;

            anyJumpTargets = true;
            printf("  IL_%04x", i);

            if (jumpTarget[i] & JT_ADDR)
            {
                printf(" addr");
            }
            if (jumpTarget[i] & JT_MULTI)
            {
                printf(" multi");
            }
            printf("\n");
        }
        if (!anyJumpTargets)
        {
            printf("  none\n");
        }
    }
#endif // DEBUG

    /* Now create the basic blocks */

    fgMakeBasicBlocks(info.compCode, info.compILCodeSize, jumpTarget);

    if (compIsForInlining())
    {
        noway_assert(info.compXcptnsCount == 0);
        compHndBBtab           = impInlineInfo->InlinerCompiler->compHndBBtab;
        compHndBBtabAllocCount = impInlineInfo->InlinerCompiler->compHndBBtabAllocCount; // we probably only use the table, not add to it.
        compHndBBtabCount      = impInlineInfo->InlinerCompiler->compHndBBtabCount;
        info.compXcptnsCount   = impInlineInfo->InlinerCompiler->info.compXcptnsCount;

        if (info.compRetNativeType != TYP_VOID       &&
            fgMoreThanOneReturnBlock())
        {
            // The lifetime of this var might expand multiple BBs. So it is a long lifetime compiler temp.
            lvaInlineeReturnSpillTemp = lvaGrabTemp(false DEBUGARG("Inline candidate multiple BBJ_RETURN spill temp"));
            lvaTable[lvaInlineeReturnSpillTemp].lvType = info.compRetNativeType;
        }
        return;
    }

    /* Mark all blocks within 'try' blocks as such */

    if  (info.compXcptnsCount == 0)
        return;

    if (info.compXcptnsCount > MAX_XCPTN_INDEX)
        IMPL_LIMITATION("too many exception clauses");

    /* Allocate the exception handler table */

    fgAllocEHTable();

    /* Assume we don't need to sort the EH table (such that nested try/catch
     * appear before their try or handler parent). The EH verifier will notice
     * when we do need to sort it.
     */

    fgNeedToSortEHTable = false;

    verInitEHTree(info.compXcptnsCount);
    EHNodeDsc* initRoot = ehnNext; // remember the original root since
                                   // it may get modified during insertion

    // Annotate BBs with exception handling information required for generating correct eh code
    // as well as checking for correct IL

    EHblkDsc *      HBtab;

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        CORINFO_EH_CLAUSE clause;
        info.compCompHnd->getEHinfo(info.compMethodHnd, XTnum, &clause);
        noway_assert(clause.HandlerLength != (unsigned)-1); // @DEPRECATED

#ifdef DEBUG
        if (verbose)
        {
            dispIncomingEHClause(XTnum, clause);
        }
#endif // DEBUG

        IL_OFFSET     tryBegOff = clause.TryOffset;
        IL_OFFSET     tryEndOff = tryBegOff + clause.TryLength;
        IL_OFFSET     filterBegOff = 0;
        IL_OFFSET     hndBegOff = clause.HandlerOffset;
        IL_OFFSET     hndEndOff = hndBegOff + clause.HandlerLength;

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filterBegOff = clause.FilterOffset;
        }

        if  (tryEndOff > info.compILCodeSize)
            BADCODE3("end of try block beyond end of method for try",
                     " at offset %04X",tryBegOff);
        if  (hndEndOff > info.compILCodeSize)
            BADCODE3("end of hnd block beyond end of method for try",
                     " at offset %04X",tryBegOff);

        HBtab->ebdTryBegOffset    = tryBegOff;
        HBtab->ebdTryEndOffset    = tryEndOff;
        HBtab->ebdFilterBegOffset = filterBegOff;
        HBtab->ebdHndBegOffset    = hndBegOff;
        HBtab->ebdHndEndOffset    = hndEndOff;

        /* Convert the various addresses to basic blocks */

        BasicBlock *  tryBegBB  = fgLookupBB(tryBegOff);
        BasicBlock *  tryEndBB  = fgLookupBB(tryEndOff); // note: this can be NULL if the try region is at the end of the function
        BasicBlock *  hndBegBB  = fgLookupBB(hndBegOff);
        BasicBlock *  hndEndBB  = NULL;
        BasicBlock *  filtBB    = NULL;
        BasicBlock *  block;

        //
        // Assert that the try/hnd beginning blocks are set up correctly
        //
        if (tryBegBB == NULL)
        {
            BADCODE("Try Clause is invalid");
        }

        if (hndBegBB == NULL)
        {
            BADCODE("Handler Clause is invalid");
        }

        tryBegBB->bbFlags |= BBF_HAS_LABEL;
        hndBegBB->bbFlags |= BBF_HAS_LABEL | BBF_JMP_TARGET;

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
        // This will change the block weight from 0 to 1
        // and clear the rarely run flag
        hndBegBB->makeBlockHot();
#else
        hndBegBB->bbSetRunRarely();            // handler entry points are rarely executed
#endif

        if (hndEndOff < info.compILCodeSize)
        {
            hndEndBB = fgLookupBB(hndEndOff);
        }

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB = HBtab->ebdFilter = fgLookupBB(clause.FilterOffset);

            filtBB->bbCatchTyp  = BBCT_FILTER;
            filtBB->bbFlags    |= BBF_HAS_LABEL | BBF_JMP_TARGET;

            hndBegBB->bbCatchTyp = BBCT_FILTER_HANDLER;

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
            // This will change the block weight from 0 to 1
            // and clear the rarely run flag
            filtBB->makeBlockHot();
#else
            filtBB->bbSetRunRarely();            // filter entry points are rarely executed
#endif

            // Mark all BBs that belong to the filter with the XTnum of the corresponding handler
            for (block = filtBB; /**/; block = block->bbNext)
            {
                if (block == NULL)
                {
                    BADCODE3("Missing endfilter for filter",
                             " at offset %04X", filtBB->bbCodeOffs);
                    return;
                }

                // Still inside the filter
                block->setHndIndex(XTnum);

                if (block->bbJumpKind == BBJ_EHFILTERRET)
                {
                    // Mark catch handler as successor.
                    block->bbJumpDest = hndBegBB;
                    assert(block->bbJumpDest->bbCatchTyp == BBCT_FILTER_HANDLER);
                    break;
                }
            }

            if (!block->bbNext || block->bbNext != hndBegBB)
                BADCODE3("Filter does not immediately precede handler for filter",
                         " at offset %04X", filtBB->bbCodeOffs);
        }
        else
        {
            HBtab->ebdTyp = clause.ClassToken;

            /* Set bbCatchTyp as appropriate */

            if (clause.Flags & CORINFO_EH_CLAUSE_FINALLY)
            {
                hndBegBB->bbCatchTyp   = BBCT_FINALLY;
            }
            else
            {
                if (clause.Flags & CORINFO_EH_CLAUSE_FAULT)
                {
                    hndBegBB->bbCatchTyp  = BBCT_FAULT;
                }
                else
                {
                    hndBegBB->bbCatchTyp  = clause.ClassToken;

                    // These values should be non-zero value that will
                    // not collide with real tokens for bbCatchTyp
                    if (clause.ClassToken == 0)
                        BADCODE("Exception catch type is Null");

                    noway_assert(clause.ClassToken != BBCT_FAULT);
                    noway_assert(clause.ClassToken != BBCT_FINALLY);
                    noway_assert(clause.ClassToken != BBCT_FILTER);
                    noway_assert(clause.ClassToken != BBCT_FILTER_HANDLER);
                }
            }
        }

        /* Mark the initial block and last blocks in the 'try' region */

        tryBegBB->bbFlags |= BBF_TRY_BEG | BBF_HAS_LABEL;

        /*  Prevent future optimizations of removing the first block   */
        /*  of a TRY block and the first block of an exception handler */

        tryBegBB->bbFlags     |= BBF_DONT_REMOVE;
        hndBegBB->bbFlags     |= BBF_DONT_REMOVE;
        hndBegBB->bbRefs++;                             // The first block of a handler gets an extra, "artificial" reference count.

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB->bbFlags |= BBF_DONT_REMOVE;
            filtBB->bbRefs++;                           // The first block of a filter gets an extra, "artificial" reference count.
        }

        tryBegBB->bbFlags     |= BBF_DONT_REMOVE;
        hndBegBB->bbFlags     |= BBF_DONT_REMOVE;

        //
        // Store the info to the table of EH block handlers
        //

        HBtab->ebdHandlerType = ToEHHandlerType(clause.Flags);

        HBtab->ebdTryBeg  = tryBegBB;
        HBtab->ebdTryLast = (tryEndBB == nullptr) ? fgLastBB : tryEndBB->bbPrev;

        HBtab->ebdHndBeg  = hndBegBB;
        HBtab->ebdHndLast = (hndEndBB == nullptr) ? fgLastBB : hndEndBB->bbPrev;

        //
        // Assert that all of our try/hnd blocks are setup correctly.
        //
        if (HBtab->ebdTryLast == nullptr)
        {
            BADCODE("Try Clause is invalid");
        }

        if (HBtab->ebdHndLast == nullptr)
        {
            BADCODE("Handler Clause is invalid");
        }

        //
        // Verify that it's legal
        //

        verInsertEhNode(&clause, HBtab);

    } // end foreach handler table entry

    fgSortEHTable();

    // Next, set things related to nesting that depend on the sorting being complete.

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        /* Mark all blocks in the finally/fault or catch clause */

        BasicBlock *    tryBegBB = HBtab->ebdTryBeg;
        BasicBlock *    hndBegBB = HBtab->ebdHndBeg;

        IL_OFFSET       tryBegOff = HBtab->ebdTryBegOffset;
        IL_OFFSET       tryEndOff = HBtab->ebdTryEndOffset;

        IL_OFFSET       hndBegOff = HBtab->ebdHndBegOffset;
        IL_OFFSET       hndEndOff = HBtab->ebdHndEndOffset;

        BasicBlock *    block;

        for (block = hndBegBB;
             block && (block->bbCodeOffs < hndEndOff);
             block = block->bbNext)
        {
            if (!block->hasHndIndex())
                block->setHndIndex(XTnum);

            // All blocks in a catch handler or filter are rarely run, except the entry
            if ((block != hndBegBB) && (hndBegBB->bbCatchTyp != BBCT_FINALLY))
                block->bbSetRunRarely();
        }

        /* Mark all blocks within the covered range of the try */

        for (block = tryBegBB;
             block && (block->bbCodeOffs < tryEndOff);
             block = block->bbNext)
        {
            /* Mark this BB as belonging to a 'try' block */

            if (!block->hasTryIndex())
                block->setTryIndex(XTnum);

#ifdef DEBUG
            /* Note: the BB can't span the 'try' block */

            if (!(block->bbFlags & BBF_INTERNAL))
            {
                noway_assert(tryBegOff <= block->bbCodeOffs);
                noway_assert(tryEndOff >= block->bbCodeOffsEnd ||
                             tryEndOff == tryBegOff );
            }
#endif
        }

        /*  Init ebdHandlerNestingLevel of current clause, and bump up value for all
         *  enclosed clauses (which have to be before it in the table).
         *  Innermost try-finally blocks must precede outermost
         *  try-finally blocks.
         */

#if !FEATURE_EH_FUNCLETS
        HBtab->ebdHandlerNestingLevel   = 0;
#endif // !FEATURE_EH_FUNCLETS

        HBtab->ebdEnclosingTryIndex     = EHblkDsc::NO_ENCLOSING_INDEX;
        HBtab->ebdEnclosingHndIndex     = EHblkDsc::NO_ENCLOSING_INDEX;

        noway_assert(XTnum <  compHndBBtabCount);
        noway_assert(XTnum == ehGetIndex(HBtab));

        for (EHblkDsc * xtab = compHndBBtab; xtab < HBtab; xtab++)
        {
#if !FEATURE_EH_FUNCLETS
            if (jitIsBetween(xtab->ebdHndBegOffs(), hndBegOff, hndEndOff))
            {
                xtab->ebdHandlerNestingLevel++;
            }
#endif // !FEATURE_EH_FUNCLETS

            /* If we haven't recorded an enclosing try index for xtab then see
             *  if this EH region should be recorded.  We check if the
             *  first offset in the xtab lies within our region.  If so,
             *  the last offset also must lie within the region, due to
             *  nesting rules. verInsertEhNode(), below, will check for proper nesting.
             */
            if (xtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                bool begBetween = jitIsBetween(xtab->ebdTryBegOffs(), tryBegOff, tryEndOff);
                if (begBetween)
                {
                    // Record the enclosing scope link
                    xtab->ebdEnclosingTryIndex = (unsigned short)XTnum;
                }
            }

            /* Do the same for the enclosing handler index.
             */
            if (xtab->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                bool begBetween = jitIsBetween(xtab->ebdTryBegOffs(), hndBegOff, hndEndOff);
                if (begBetween)
                {
                    // Record the enclosing scope link
                    xtab->ebdEnclosingHndIndex = (unsigned short)XTnum;
                }
            }
        }

    } // end foreach handler table entry

#if !FEATURE_EH_FUNCLETS

    EHblkDsc* HBtabEnd;
    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        if (ehMaxHndNestingCount <= HBtab->ebdHandlerNestingLevel)
            ehMaxHndNestingCount = HBtab->ebdHandlerNestingLevel + 1;
    }

#endif // !FEATURE_EH_FUNCLETS

#ifndef DEBUG
    if (tiVerificationNeeded)
#endif
    {
        // always run these checks for a debug build
        verCheckNestingLevel(initRoot);
    }

    // fgNormalizeEH assumes that this test has been passed.  And Ssa assumes that fgNormalizeEHTable
    // has been run.  So do this unless we're in minOpts mode (and always in debug).
#ifndef DEBUG
    if (tiVerificationNeeded || !opts.MinOpts())
#endif
    {
        fgCheckBasicBlockControlFlow();
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("*************** After fgFindBasicBlocks() has created the EH table\n");
        fgDispHandlerTab();
    }

    // We can't verify the handler table until all the IL legality checks have been done (above), since bad IL
    // (such as illegal nesting of regions) will trigger asserts here.
    fgVerifyHandlerTab();
#endif

    fgNormalizeEH();
}


/*****************************************************************************
 * Check control flow constraints for well formed IL. Bail if any of the constraints
 * are violated.
 */

void            Compiler::fgCheckBasicBlockControlFlow()
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    EHblkDsc *HBtab;

    for (BasicBlock* blk = fgFirstBB; blk; blk = blk->bbNext)
    {
        if (blk->bbFlags & BBF_INTERNAL)
            continue;

        switch (blk->bbJumpKind)
        {
        case BBJ_NONE:       // block flows into the next one (no jump)

            fgControlFlowPermitted(blk,blk->bbNext);

            break;

        case BBJ_ALWAYS:    // block does unconditional jump to target

            fgControlFlowPermitted(blk,blk->bbJumpDest);

            break;

        case BBJ_COND:      // block conditionally jumps to the target

            fgControlFlowPermitted(blk,blk->bbNext);

            fgControlFlowPermitted(blk,blk->bbJumpDest);

            break;

        case BBJ_RETURN:    // block ends with 'ret'

            if (blk->hasTryIndex() || blk->hasHndIndex())
            {
                BADCODE3("Return from a protected block",
                         ". Before offset %04X", blk->bbCodeOffsEnd);
            }
            break;

        case BBJ_EHFINALLYRET:
        case BBJ_EHFILTERRET:

            if (!blk->hasHndIndex())  // must be part of a handler
            {
                BADCODE3("Missing handler",
                         ". Before offset %04X", blk->bbCodeOffsEnd);
            }

            HBtab = ehGetDsc(blk->getHndIndex());

            // Endfilter allowed only in a filter block
            if (blk->bbJumpKind == BBJ_EHFILTERRET)
            {
                if (!HBtab->HasFilter())
                {
                    BADCODE("Unexpected endfilter");
                }
            }
            // endfinally allowed only in a finally/fault block
            else if (!HBtab->HasFinallyOrFaultHandler())
            {
                BADCODE("Unexpected endfinally");
            }

            // The handler block should be the innermost block
            // Exception blocks are listed, innermost first.
            if (blk->hasTryIndex() && (blk->getTryIndex() < blk->getHndIndex()))
            {
                BADCODE("endfinally / endfilter in nested try block");
            }

            break;

        case BBJ_THROW:     // block ends with 'throw'
            /* throw is permitted from every BB, so nothing to check */
            /* importer makes sure that rethrow is done from a catch */
            break;

        case BBJ_LEAVE:      // block always jumps to the target, maybe out of guarded
                             // region. Used temporarily until importing
            fgControlFlowPermitted(blk, blk->bbJumpDest,TRUE);

            break;

        case BBJ_SWITCH:     // block ends with a switch statement

            BBswtDesc* swtDesc;
            swtDesc = blk->bbJumpSwt;

            assert (swtDesc);

            unsigned i;
            for (i=0; i<swtDesc->bbsCount; i++)
            {
                fgControlFlowPermitted(blk,swtDesc->bbsDstTab[i]);
            }

            break;

        case BBJ_EHCATCHRET:        // block ends with a leave out of a catch (only #if FEATURE_EH_FUNCLETS)
        case BBJ_CALLFINALLY:       // block always calls the target finally
        default:
            noway_assert(!"Unexpected bbJumpKind");     // these blocks don't get created until importing
            break;
        }
    }
}

/****************************************************************************
 * Check that the leave from the block is legal.
 * Consider removing this check here if we  can do it cheaply during importing
 */

void           Compiler::fgControlFlowPermitted(BasicBlock*  blkSrc,
                                                BasicBlock*  blkDest,
                                                BOOL         isLeave)
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    unsigned    srcHndBeg,   destHndBeg;
    unsigned    srcHndEnd,   destHndEnd;
    bool        srcInFilter, destInFilter;
    bool        srcInCatch = false;

    EHblkDsc*   srcHndTab;

    srcHndTab = ehInitHndRange(blkSrc,  &srcHndBeg,  &srcHndEnd,  &srcInFilter);
                ehInitHndRange(blkDest, &destHndBeg, &destHndEnd, &destInFilter);

    /* Impose the rules for leaving or jumping from handler blocks */

    if (blkSrc->hasHndIndex())
    {
        srcInCatch = srcHndTab->HasCatchHandler() && srcHndTab->InHndRegionILRange(blkSrc);

        /* Are we jumping within the same handler index? */
        if (BasicBlock::sameHndRegion(blkSrc, blkDest))
        {
             /* Do we have a filter clause? */
            if (srcHndTab->HasFilter())
            {
                /* filters and catch handlers share same eh index  */
                /* we need to check for control flow between them. */
                if (srcInFilter != destInFilter)
                {
                    if (!jitIsBetween(blkDest->bbCodeOffs, srcHndBeg, srcHndEnd))
                        BADCODE3("Illegal control flow between filter and handler",
                                 ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }
        }
        else
        {
            /* The handler indexes of blkSrc and blkDest are different */
            if (isLeave)
            {
                /* Any leave instructions must not enter the dest handler from outside*/
                if (!jitIsBetween(srcHndBeg, destHndBeg, destHndEnd))
                    BADCODE3("Illegal use of leave to enter handler",
                             ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
            else
            {
                /* We must use a leave to exit a handler */
                BADCODE3("Illegal control flow out of a handler",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }

            /* Do we have a filter clause? */
            if (srcHndTab->HasFilter())
            {
                /* It is ok to leave from the handler block of a filter, */
                /* but not from the filter block of a filter             */
                if (srcInFilter != destInFilter)
                {
                    BADCODE3("Illegal to leave a filter handler",
                             ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }

            /* We should never leave a finally handler */
            if (srcHndTab->HasFinallyHandler())
            {
                BADCODE3("Illegal to leave a finally handler",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }

            /* We should never leave a fault handler */
            if (srcHndTab->HasFaultHandler())
            {
                BADCODE3("Illegal to leave a fault handler",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
    }
    else if (blkDest->hasHndIndex())
    {
        /* blkSrc was not inside a handler, but blkDst is inside a handler */
        BADCODE3("Illegal control flow into a handler",
                 ". Before offset %04X", blkSrc->bbCodeOffsEnd);
    }

    /* Are we jumping from a catch handler into the corresponding try? */
    /* VB uses this for "on error goto "                               */

    if (isLeave && srcInCatch)
    {
        // inspect all handlers containing the jump source

        bool bValidJumpToTry = false;       // are we jumping in a valid way from a catch to the corresponding try?
        bool bCatchHandlerOnly = true;      // false if we are jumping out of a non-catch handler
        EHblkDsc* ehTableEnd;
        EHblkDsc* ehDsc;

        for (ehDsc = compHndBBtab, ehTableEnd = compHndBBtab + compHndBBtabCount; bCatchHandlerOnly && ehDsc < ehTableEnd; ehDsc++)
        {
            if (ehDsc->InHndRegionILRange(blkSrc))
            {
                if (ehDsc->HasCatchHandler())
                {
                    if (ehDsc->InTryRegionILRange(blkDest))
                    {
                        // If we already considered the jump for a different try/catch,
                        // we would have two overlapping try regions with two overlapping catch
                        // regions, which is illegal.
                        noway_assert(!bValidJumpToTry);

                        // Allowed if it is the first instruction of an inner try
                        // (and all trys in between)
                        //
                        // try {
                        //  ..
                        // _tryAgain:
                        //  ..
                        //      try {
                        //      _tryNestedInner:
                        //        ..
                        //          try {
                        //          _tryNestedIllegal:
                        //            ..
                        //          } catch {
                        //            ..
                        //          }
                        //        ..
                        //      } catch {
                        //        ..
                        //      }
                        //  ..
                        // } catch {
                        //  ..
                        //  leave _tryAgain         // Allowed
                        //  ..
                        //  leave _tryNestedInner   // Allowed
                        //  ..
                        //  leave _tryNestedIllegal // Not Allowed
                        //  ..
                        // }
                        //
                        // Note: The leave is allowed also from catches nested inside the catch shown above.

                        /* The common case where leave is to the corresponding try */
                        if (ehDsc->ebdIsSameTry(this, blkDest->getTryIndex()) ||
                            /* Also allowed is a leave to the start of a try which starts in the handler's try */
                            fgFlowToFirstBlockOfInnerTry(ehDsc->ebdTryBeg, blkDest, false))
                        {
                            bValidJumpToTry = true;
                        }
                    }
                }
                else
                {
                    // We are jumping from a handler which is not a catch handler.

                    // If it's a handler, but not a catch handler, it must be either a finally or fault
                    if (!ehDsc->HasFinallyOrFaultHandler())
                    {
                        BADCODE3("Handlers must be catch, finally, or fault",
                                 ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                    }

                    // Are we jumping out of this handler?
                    if (!ehDsc->InHndRegionILRange(blkDest))
                        bCatchHandlerOnly = false;
                }
            }
            else if (ehDsc->InFilterRegionILRange(blkSrc))
            {
                  // Are we jumping out of a filter?
                 if (!ehDsc->InFilterRegionILRange(blkDest))
                    bCatchHandlerOnly = false;
            }
        }

        if (bCatchHandlerOnly)
        {
            if (bValidJumpToTry)
                return;
            else
            {
                // FALL THROUGH
                // This is either the case of a leave to outside the try/catch,
                // or a leave to a try not nested in this try/catch.
                // The first case is allowed, the second one will be checked
                // later when we check the try block rules (it is illegal if we
                // jump to the middle of the destination try).
            }
        }
        else
        {
            BADCODE3("illegal leave to exit a finally, fault or filter",
                     ". Before offset %04X", blkSrc->bbCodeOffsEnd);
        }

    }

    /* Check all the try block rules */

    IL_OFFSET   srcTryBeg;
    IL_OFFSET   srcTryEnd;
    IL_OFFSET   destTryBeg;
    IL_OFFSET   destTryEnd;

    ehInitTryRange(blkSrc,  &srcTryBeg,  &srcTryEnd);
    ehInitTryRange(blkDest, &destTryBeg, &destTryEnd);

    /* Are we jumping between try indexes? */
    if (!BasicBlock::sameTryRegion(blkSrc, blkDest))
    {
        // Are we exiting from an inner to outer try?
        if (jitIsBetween(srcTryBeg,   destTryBeg, destTryEnd) &&
            jitIsBetween(srcTryEnd-1, destTryBeg, destTryEnd)   )
        {
            if (!isLeave)
            {
                BADCODE3("exit from try block without a leave",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
        else if (jitIsBetween(destTryBeg, srcTryBeg, srcTryEnd))
        {
            // check that the dest Try is first instruction of an inner try
            if (!fgFlowToFirstBlockOfInnerTry(blkSrc, blkDest, false))
            {
                BADCODE3("control flow into middle of try",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
        else // there is no nesting relationship between src and dest
        {
            if (isLeave)
            {
                // check that the dest Try is first instruction of an inner try sibling
                if (!fgFlowToFirstBlockOfInnerTry(blkSrc, blkDest, true))
                {
                    BADCODE3("illegal leave into middle of try",
                             ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }
            else
            {
                BADCODE3("illegal control flow in to/out of try block",
                         ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
    }
}

/*****************************************************************************
 *  Check that blkDest is the first block of an inner try or a sibling
 *    with no intervening trys in between
 */

bool             Compiler::fgFlowToFirstBlockOfInnerTry(BasicBlock*  blkSrc,
                                                        BasicBlock*  blkDest,
                                                        bool         sibling)
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    noway_assert(blkDest->hasTryIndex());

    unsigned        XTnum     = blkDest->getTryIndex();
    unsigned        lastXTnum = blkSrc->hasTryIndex() ? blkSrc->getTryIndex()
                                                      : compHndBBtabCount;
    noway_assert(XTnum     <  compHndBBtabCount);
    noway_assert(lastXTnum <= compHndBBtabCount);

    EHblkDsc*       HBtab     = ehGetDsc(XTnum);

    // check that we are not jumping into middle of try
    if (HBtab->ebdTryBeg != blkDest)
    {
        return false;
    }

    if (sibling)
    {
        noway_assert(!BasicBlock::sameTryRegion(blkSrc, blkDest));

        // find the l.u.b of the two try ranges
        // Set lastXTnum to the l.u.b.

        HBtab = ehGetDsc(lastXTnum);

        for (lastXTnum++, HBtab++;
             lastXTnum < compHndBBtabCount;
             lastXTnum++, HBtab++)
        {
            if (jitIsBetweenInclusive(blkDest->bbNum,
                                      HBtab->ebdTryBeg->bbNum,
                                      HBtab->ebdTryLast->bbNum))
            {
                break;
            }
        }
    }

    // now check there are no intervening trys between dest and l.u.b
    // (it is ok to have intervening trys as long as they all start at
    //  the same code offset)

    HBtab = ehGetDsc(XTnum);

    for (XTnum++,  HBtab++;
         XTnum < lastXTnum;
         XTnum++,  HBtab++)
    {
        if (HBtab->ebdTryBeg->bbNum < blkDest->bbNum && blkDest->bbNum <= HBtab->ebdTryLast->bbNum)
        {
            return false;
        }
    }

    return true;
}

/*****************************************************************************
 *  Returns the handler nesting level of the block.
 *  *pFinallyNesting is set to the nesting level of the inner-most
 *  finally-protected try the block is in.
 */

unsigned            Compiler::fgGetNestingLevel(BasicBlock *  block,
                                                unsigned  *   pFinallyNesting)
{
    unsigned        curNesting = 0;        // How many handlers is the block in
    unsigned        tryFin = (unsigned)-1; // curNesting when we see innermost finally-protected try
    unsigned        XTnum;
    EHblkDsc *      HBtab;

    /* We find the block's handler nesting level by walking over the
       complete exception table and find enclosing clauses. */

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++,   HBtab++)
    {
        noway_assert(HBtab->ebdTryBeg && HBtab->ebdHndBeg);

        if (HBtab->HasFinallyHandler() &&
            (tryFin == (unsigned)-1) &&
            bbInTryRegions(XTnum, block))
        {
            tryFin = curNesting;
        }
        else if (bbInHandlerRegions(XTnum, block))
        {
            curNesting++;
        }
    }

    if  (tryFin == (unsigned)-1)
        tryFin = curNesting;

    if  (pFinallyNesting)
        *pFinallyNesting = curNesting - tryFin;

    return curNesting;
}

/*****************************************************************************
 *
 *  Import the basic blocks of the procedure.
 */

void             Compiler::fgImport()
{
    fgHasPostfix = false;

    impImport(fgFirstBB);

    if (!(opts.eeFlags & CORJIT_FLG_SKIP_VERIFICATION))
    {
        CorInfoMethodRuntimeFlags verFlag;
        verFlag = tiIsVerifiableCode ? CORINFO_FLG_VERIFIABLE
                                     : CORINFO_FLG_UNVERIFIABLE;
        info.compCompHnd->setMethodAttribs(info.compMethodHnd, verFlag);
    }
}


/*****************************************************************************
 * This function returns true if tree is a node with a call
 * that unconditionally throws an exception
 */

bool         Compiler::fgIsThrow(GenTreePtr     tree)
{
    if ((tree->gtOper               != GT_CALL  ) ||
        (tree->gtCall.gtCallType    != CT_HELPER)   )
    {
        return false;
    }

    // TODO-Throughput: Replace all these calls to eeFindHelper() with a table based lookup

    if ((tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_OVERFLOW)    ) ||
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_VERIFICATION)) ||
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_RNGCHKFAIL)  ) ||
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWDIVZERO)) ||
#if COR_JIT_EE_VERSION > 460
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWNULLREF)) ||
#endif // COR_JIT_EE_VERSION
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROW)       ) ||
        (tree->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_RETHROW)     )   )
    {
        noway_assert(tree->gtFlags & GTF_CALL);
        noway_assert(tree->gtFlags & GTF_EXCEPT);
        return true;
    }

    // TODO-CQ: there are a bunch of managed methods in [mscorlib]System.ThrowHelper
    // that would be nice to recognize.  

    return false;
}

/*****************************************************************************
 * This function returns true for blocks that are in different hot-cold regions.
 * It returns false when the blocks are both in the same regions
 */

bool         Compiler::fgInDifferentRegions(BasicBlock *blk1, BasicBlock *blk2)
{
    noway_assert(blk1 != NULL);
    noway_assert(blk2 != NULL);

    if (fgFirstColdBlock == NULL)
    {
        return false;
    }

    // If one block is Hot and the other is Cold then we are in different regions
    return ((blk1->bbFlags & BBF_COLD)!= (blk2->bbFlags & BBF_COLD));
}

/*****************************************************************************
 * This function returns true if tree is a GT_COMMA node with a call
 * that unconditionally throws an exception
 */

bool                Compiler::fgIsCommaThrow(GenTreePtr tree,
                                             bool       forFolding /* = false */)
{
    // Instead of always folding comma throws,
    // with stress enabled we only fold half the time

    if (forFolding && compStressCompile(STRESS_FOLD, 50))
    {
        return false;         /* Don't fold */
    }

    /* Check for cast of a GT_COMMA with a throw overflow */
    if ((tree->gtOper == GT_COMMA)   &&
        (tree->gtFlags & GTF_CALL)   &&
        (tree->gtFlags & GTF_EXCEPT))
    {
        return (fgIsThrow(tree->gtOp.gtOp1));
    }
    return false;
}


GenTreePtr          Compiler::fgIsIndirOfAddrOfLocal(GenTreePtr tree)
{
    GenTreePtr res = nullptr;
    if (tree->OperGet() == GT_LDOBJ || tree->OperIsIndir())
    {
        GenTreePtr addr = tree->gtOp.gtOp1;

        // Post rationalization, we can have Indir(Lea(..) trees. Therefore to recognize
        // Indir of addr of a local, skip over Lea in Indir(Lea(base, index, scale, offset))
        // to get to base variable.
        if (addr->OperGet() == GT_LEA)
        {
            // We use this method in backward dataflow after liveness computation - fgInterBlockLocalVarLiveness().
            // Therefore it is critical that we don't miss 'uses' of any local.  It may seem this method overlooks
            // if the index part of the LEA has indir( someAddrOperator ( lclVar ) ) to search for a use but it's
            // covered by the fact we're traversing the expression in execution order and we also visit the index.
            GenTreeAddrMode* lea = addr->AsAddrMode();
            GenTreePtr base = lea->Base();            

            if (base != nullptr)
            {
                if (base->OperGet() == GT_IND)
                {
                    return fgIsIndirOfAddrOfLocal(base);
                }
                // else use base as addr
                addr = base;
            }
        }

        if (addr->OperGet() == GT_ADDR)
        {
            GenTreePtr lclvar = addr->gtOp.gtOp1;
            if (lclvar->OperGet() == GT_LCL_VAR)
            {
                res = lclvar;
            }
        } 
        else if (addr->OperGet() == GT_LCL_VAR_ADDR)
        {
            res = addr;
        }
    }
    return res;
}


GenTreePtr          Compiler::fgGetStaticsCCtorHelper(CORINFO_CLASS_HANDLE cls, CorInfoHelpFunc helper)
{
    bool bNeedClassID = true;
    unsigned callFlags = 0;

    var_types type = TYP_BYREF;

    // This is sort of ugly, as we have knowledge of what the helper is returning.
    // We need the return type.
    switch (helper)
    {
    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
        bNeedClassID = false;
        __fallthrough;

    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
        callFlags |= GTF_CALL_HOISTABLE;
        __fallthrough;

    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
    case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
    case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
        // type = TYP_BYREF;
        break;

    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
        bNeedClassID = false;
        __fallthrough;

    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
        callFlags |= GTF_CALL_HOISTABLE;
        __fallthrough;

    case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
    case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
    case CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS:
        type = TYP_I_IMPL;
        break;

    default:
        assert(!"unknown shared statics helper");
        break;
    }

    GenTreeArgList* argList = NULL;

    GenTreePtr opModuleIDArg;
    GenTreePtr opClassIDArg;

    // Get the class ID
    unsigned clsID;
    size_t moduleID;
    void* pclsID;
    void* pmoduleID;

    clsID =  info.compCompHnd->getClassDomainID(cls, &pclsID);

    moduleID =  info.compCompHnd->getClassModuleIdForStatics(cls, NULL, &pmoduleID);

    if (!(callFlags & GTF_CALL_HOISTABLE))
    {
        if (info.compCompHnd->getClassAttribs(cls) & CORINFO_FLG_BEFOREFIELDINIT)
        {
            callFlags |= GTF_CALL_HOISTABLE;
        }
    }

    if (pmoduleID)
    {
        opModuleIDArg = gtNewIconHandleNode((size_t) pmoduleID, GTF_ICON_CIDMID_HDL);
        opModuleIDArg = gtNewOperNode(GT_IND, TYP_I_IMPL, opModuleIDArg);
        opModuleIDArg->gtFlags |= GTF_IND_INVARIANT;
    }
    else
    {
        opModuleIDArg = gtNewIconNode((size_t)moduleID, TYP_I_IMPL);
    }

    if (bNeedClassID)
    {
        if (pclsID)
        {
            opClassIDArg = gtNewIconHandleNode((size_t) pclsID, GTF_ICON_CIDMID_HDL);
            opClassIDArg = gtNewOperNode(GT_IND, TYP_INT, opClassIDArg);
            opClassIDArg->gtFlags |= GTF_IND_INVARIANT;
        }
        else
        {
            opClassIDArg = gtNewIconNode(clsID, TYP_INT);
        }

        // call the helper to get the base
        argList = gtNewArgList(opModuleIDArg, opClassIDArg);
    }
    else
    {
        argList = gtNewArgList(opModuleIDArg);
    }

    if (!s_helperCallProperties.NoThrow(helper))
    {
        callFlags |= GTF_EXCEPT;
    }

    return gtNewHelperCallNode(helper, type, callFlags, argList);
}

GenTreePtr          Compiler::fgGetSharedCCtor(CORINFO_CLASS_HANDLE cls)
{
#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        CORINFO_RESOLVED_TOKEN resolvedToken;
        ZeroMemory(&resolvedToken, sizeof(resolvedToken));
        resolvedToken.hClass = cls;

        return impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_STATIC_BASE, TYP_BYREF);
    }
#endif

    // Call the shared non gc static helper, as its the fastest
    return fgGetStaticsCCtorHelper(cls, info.compCompHnd->getSharedCCtorHelper(cls));
}


//
// Returns true unless the address expression could
// never represent a NULL
//
bool Compiler::fgAddrCouldBeNull(GenTreePtr addr)
{
    if (addr->gtOper == GT_ADDR)
    {
        if (addr->gtOp.gtOp1->gtOper == GT_CNS_INT)
        {
            GenTreePtr cns1Tree = addr->gtOp.gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                // Indirection of some random constant...
                // It is safest just to return true
                return true;
            }
        }
        else if (addr->gtOp.gtOp1->OperIsLocalAddr())
        {
            return false;
        }
        return false;  // we can't have a null address
    }
    else if (addr->gtOper == GT_ADD)
    {
        if (addr->gtOp.gtOp1->gtOper == GT_CNS_INT)
        {
            GenTreePtr cns1Tree = addr->gtOp.gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                if (!fgIsBigOffset(cns1Tree->gtIntCon.gtIconVal))
                {
                    // Op1 was an ordinary small constant
                    return fgAddrCouldBeNull(addr->gtOp.gtOp2);
                }
            }
            else    // Op1 was a handle represented as a constant
            {
                // Is Op2 also a constant?
                if (addr->gtOp.gtOp2->gtOper == GT_CNS_INT)
                {
                    GenTreePtr cns2Tree = addr->gtOp.gtOp2;
                    // Is this an addition of a handle and constant
                    if (!cns2Tree->IsIconHandle())
                    {
                        if (!fgIsBigOffset(cns2Tree->gtIntCon.gtIconVal))
                        {
                             // Op2 was an ordinary small constant
                            return false; // we can't have a null address
                        }
                    }
                }
            }
        }
        else
        {
            // Op1 is not a constant
            // What about Op2?
            if (addr->gtOp.gtOp2->gtOper == GT_CNS_INT)
            {
                GenTreePtr cns2Tree = addr->gtOp.gtOp2;
                // Is this an addition of a small constant
                if (!cns2Tree->IsIconHandle())
                {
                    if (!fgIsBigOffset(cns2Tree->gtIntCon.gtIconVal))
                    {
                        // Op2 was an ordinary small constant
                        return fgAddrCouldBeNull(addr->gtOp.gtOp1);
                    }
                }
            }
        }
    }
    return true;  // default result: addr could be null
}


/*****************************************************************************
 *  Optimize the call to the delegate constructor.
 */

GenTreePtr    Compiler::fgOptimizeDelegateConstructor(GenTreePtr call, CORINFO_CONTEXT_HANDLE * ExactContextHnd)
{
    noway_assert(call->gtOper == GT_CALL);

    noway_assert(call->gtCall.gtCallType == CT_USER_FUNC);
    CORINFO_METHOD_HANDLE methHnd = call->gtCall.gtCallMethHnd;
    CORINFO_CLASS_HANDLE clsHnd = info.compCompHnd->getMethodClass(methHnd);

    GenTreePtr targetMethod = call->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp1;
    noway_assert(targetMethod->TypeGet() == TYP_I_IMPL);
    genTreeOps oper = targetMethod->OperGet();
    if (oper == GT_FTN_ADDR || oper == GT_CALL || oper == GT_QMARK)
    {
        CORINFO_METHOD_HANDLE targetMethodHnd = NULL;
        GenTreePtr qmarkNode = NULL;
        if (oper == GT_FTN_ADDR)
        {
            targetMethodHnd = targetMethod->gtFptrVal.gtFptrMethod;
        }
        else if (oper == GT_CALL && targetMethod->gtCall.gtCallMethHnd == eeFindHelper(CORINFO_HELP_VIRTUAL_FUNC_PTR))
        {
            GenTreePtr handleNode = targetMethod->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp2->gtOp.gtOp1;

            if (handleNode->OperGet() == GT_CNS_INT)
            {
                // it's a ldvirtftn case, fetch the methodhandle off the helper for ldvirtftn. It's the 3rd arg
                targetMethodHnd = CORINFO_METHOD_HANDLE(handleNode->gtIntCon.gtCompileTimeHandle);
            }
            //Sometimes the argument to this is the result of a generic dictionary lookup, which shows
            //up as a GT_QMARK.
            else if (handleNode->OperGet() == GT_QMARK)
            {
                qmarkNode = handleNode;
            }
        }
        //Sometimes we don't call CORINFO_HELP_VIRTUAL_FUNC_PTR but instead just call
        //CORINFO_HELP_RUNTIMEHANDLE_METHOD directly.
        else if (oper == GT_QMARK)
        {
            qmarkNode = targetMethod;
        }
        if (qmarkNode)
        {
            noway_assert(qmarkNode->OperGet() == GT_QMARK);
            //The argument is actually a generic dictionary lookup.  For delegate creation it looks
            //like:
            //GT_QMARK
            //  GT_COLON
            //      op1 -> call
            //              Arg 1 -> token (has compile time handle)
            //      op2 -> lclvar
            //
            //
            //In this case I can find the token (which is a method handle) and that is the compile time
            //handle.
            noway_assert(qmarkNode->gtOp.gtOp2->OperGet() == GT_COLON);
            noway_assert(qmarkNode->gtOp.gtOp2->gtOp.gtOp1->OperGet() == GT_CALL);
            GenTreePtr runtimeLookupCall = qmarkNode->gtOp.gtOp2->gtOp.gtOp1;

            //This could be any of CORINFO_HELP_RUNTIMEHANDLE_(METHOD|CLASS)(_LOG?)
            GenTreePtr tokenNode = runtimeLookupCall->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp1;
            noway_assert(tokenNode->OperGet() == GT_CNS_INT);
            targetMethodHnd = CORINFO_METHOD_HANDLE(tokenNode->gtIntCon.gtCompileTimeHandle);
        }

#ifdef FEATURE_READYTORUN_COMPILER
        if (opts.IsReadyToRun())
        {
            // ReadyToRun has this optimization for a non-virtual function pointers only for now.
            if (oper == GT_FTN_ADDR)
            {
                // The first argument of the helper is delegate this pointer
                GenTreeArgList* helperArgs = gtNewArgList(call->gtCall.gtCallObjp);

                // The second argument of the helper is the target object pointers
                helperArgs->gtOp.gtOp2 = gtNewArgList(call->gtCall.gtCallArgs->gtOp.gtOp1);

                call = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_DELEGATE_CTOR, TYP_VOID, GTF_EXCEPT, helperArgs);
                call->gtCall.gtEntryPoint = targetMethod->gtFptrVal.gtDelegateCtor;
            }
        }
        else
#endif
        if (targetMethodHnd != NULL)
        {
            CORINFO_METHOD_HANDLE alternateCtor = NULL;
            DelegateCtorArgs ctorData;
            ctorData.pMethod = info.compMethodHnd;
            ctorData.pArg3 = NULL;
            ctorData.pArg4 = NULL;
            ctorData.pArg5 = NULL;

            alternateCtor = info.compCompHnd->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, &ctorData);
            if (alternateCtor != methHnd)
            {
                // we erase any inline info that may have been set for generics has it is not needed here,
                // and in fact it will pass the wrong info to the inliner code
                * ExactContextHnd = 0;

                call->gtCall.gtCallMethHnd = alternateCtor;

                noway_assert(call->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp2 == NULL);
                if (ctorData.pArg3)
                {
                    call->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp2 =
                        gtNewArgList(gtNewIconHandleNode(size_t(ctorData.pArg3), GTF_ICON_FTN_ADDR));

                    if (ctorData.pArg4)
                    {
                        call->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp2->gtOp.gtOp2 =
                            gtNewArgList(gtNewIconHandleNode(size_t(ctorData.pArg4), GTF_ICON_FTN_ADDR));

                        if (ctorData.pArg5)
                        {
                            call->gtCall.gtCallArgs->gtOp.gtOp2->gtOp.gtOp2->gtOp.gtOp2->gtOp.gtOp2 =
                                gtNewArgList(gtNewIconHandleNode(size_t(ctorData.pArg5), GTF_ICON_FTN_ADDR));
                        }
                    }
                }
            }
        }
    }

    return call;
}


bool Compiler::fgCastNeeded(GenTreePtr tree, var_types toType)
{
    //
    // If tree is a relop and we need an 4-byte integer
    //  then we never need to insert a cast
    //
    if ((tree->OperKind() & GTK_RELOP) && (genActualType(toType) == TYP_INT))
        return false;

    var_types fromType;

    //
    // Is the tree as GT_CAST or a GT_CALL ?
    //
    if (tree->OperGet() == GT_CAST)
    {
        fromType = tree->CastToType();
    }
    else if (tree->OperGet() == GT_CALL)
    {
        fromType = (var_types) tree->gtCall.gtReturnType;
    }
    else
    {
        fromType = tree->TypeGet();
    }

    //
    // If both types are the same then an additional cast is not necessary
    //
    if (toType == fromType)
        return false;
    //
    // If the sign-ness of the two types are different then a cast is necessary
    //
    if (varTypeIsUnsigned(toType) != varTypeIsUnsigned(fromType))
        return true;
    //
    // If the from type is the same size or smaller then an additional cast is not necessary
    //
    if (genTypeSize(toType) >= genTypeSize(fromType))
        return false;

    //
    // Looks like we will need the cast
    //
    return true;
}

// If assigning to a local var, add a cast if the target is
// marked as NormalizedOnStore. Returns true if any change was made
GenTreePtr Compiler::fgDoNormalizeOnStore(GenTreePtr tree)
{
    //
    // Only normalize the stores in the global morph phase
    //
    if (fgGlobalMorph)
    {
        noway_assert(tree->OperGet()==GT_ASG);

        GenTreePtr      op1     = tree->gtOp.gtOp1;
        GenTreePtr      op2     = tree->gtOp.gtOp2;

        if (op1->gtOper == GT_LCL_VAR && genActualType(op1->TypeGet()) == TYP_INT)
        {
            // Small-typed arguments and aliased locals are normalized on load.
            // Other small-typed locals are normalized on store.
            // If it is an assignment to one of the latter, insert the cast on RHS
            unsigned    varNum = op1->gtLclVarCommon.gtLclNum;
            LclVarDsc * varDsc = &lvaTable[varNum];

            if (varDsc->lvNormalizeOnStore())
            {
                noway_assert(op1->gtType <= TYP_INT);
                op1->gtType = TYP_INT;

                if (fgCastNeeded(op2, varDsc->TypeGet()))
                {
                    op2 = gtNewCastNode(TYP_INT, op2, varDsc->TypeGet());
                    tree->gtOp.gtOp2 = op2;

                    // Propagate GTF_COLON_COND
                    op2->gtFlags|=(tree->gtFlags & GTF_COLON_COND);
                }
            }
        }
    }

    return tree;
}


/*****************************************************************************
 *
 *  Mark whether the edge "srcBB -> dstBB" forms a loop that will always
 *  execute a call or not.
 */

inline
void                Compiler::fgLoopCallTest(BasicBlock *srcBB,
                                             BasicBlock *dstBB)
{
    /* Bail if this is not a backward edge */

    if  (srcBB->bbNum < dstBB->bbNum)
        return;

    /* Unless we already know that there is a loop without a call here ... */

    if  (!(dstBB->bbFlags & BBF_LOOP_CALL0))
    {
        /* Check whether there is a loop path that doesn't call */

        if  (optReachWithoutCall(dstBB, srcBB))
        {
            dstBB->bbFlags |=  BBF_LOOP_CALL0;
            dstBB->bbFlags &= ~BBF_LOOP_CALL1;
        }
        else
            dstBB->bbFlags |=  BBF_LOOP_CALL1;
    }
    //if this loop will always call, then we can omit the GC Poll
    if ((GCPOLL_NONE != opts.compGCPollType) && (dstBB->bbFlags & BBF_LOOP_CALL1))
    {
        srcBB->bbFlags &= ~BBF_NEEDS_GCPOLL;
    }
}

/*****************************************************************************
 *
 *  Mark which loops are guaranteed to execute a call.
 */

void                Compiler::fgLoopCallMark()
{
    BasicBlock  *   block;

    /* If we've already marked all the block, bail */

    if  (fgLoopCallMarked)
        return;

    fgLoopCallMarked = true;

    /* Walk the blocks, looking for backward edges */

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
        case BBJ_COND:
        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
            fgLoopCallTest(block, block->bbJumpDest);
            break;

        case BBJ_SWITCH:

            unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock * *  jumpPtr; jumpPtr = block->bbJumpSwt->bbsDstTab;

            do
            {
                fgLoopCallTest(block, *jumpPtr);
            }
            while (++jumpPtr, --jumpCnt);

            break;

        default:
            break;
        }
    }
}

/*****************************************************************************
 *
 *  Note the fact that the given block is a loop header.
 */

inline
void                Compiler::fgMarkLoopHead(BasicBlock *block)
{
#ifdef DEBUG
    if (verbose)
        printf("fgMarkLoopHead: Checking loop head block BB%02u: ", block->bbNum);
#endif

    /* Have we decided to generate fully interruptible code already? */

    if  (genInterruptible)
    {
#ifdef DEBUG
        if (verbose)
            printf("method is already fully interruptible\n");
#endif
        return;
    }

    /* Is the loop head block known to execute a method call? */

    if  (block->bbFlags & BBF_GC_SAFE_POINT)
    {
#ifdef DEBUG
        if (verbose)
            printf("this block will execute a call\n");
#endif
        // single block loops that contain GC safe points don't need polls.
        block->bbFlags &= ~BBF_NEEDS_GCPOLL;
        return;
    }

    /* Are dominator sets available? */

    if  (fgDomsComputed)
    {
        /* Make sure that we know which loops will always execute calls */

        if  (!fgLoopCallMarked)
            fgLoopCallMark();

        /* Will every trip through our loop execute a call? */

        if  (block->bbFlags & BBF_LOOP_CALL1)
        {
#ifdef DEBUG
            if (verbose)
                printf("this block dominates a block that will execute a call\n");
#endif
            return;
        }
    }

    /*
     *  We have to make this method fully interruptible since we can not
     *  ensure that this loop will execute a call every time it loops.
     *
     *  We'll also need to generate a full register map for this method.
     */

    assert(!codeGen->isGCTypeFixed());

    if (!compCanEncodePtrArgCntMax())
    {
#ifdef DEBUG
        if (verbose)
            printf("a callsite with more than 1023 pushed args exists\n");
#endif
        return;
    }

#ifdef DEBUG
    if (verbose)
        printf("no guaranteed callsite exits, marking method as fully interruptible\n");
#endif

    //only enable fully interruptible code for if we're hijacking.
    if (GCPOLL_NONE == opts.compGCPollType)
    {
        genInterruptible = true;
    }
}


GenTreePtr Compiler::fgGetCritSectOfStaticMethod()
{
    noway_assert(!compIsForInlining());

    noway_assert(info.compIsStatic);  // This method should only be called for static methods.

    GenTreePtr tree = NULL;

    CORINFO_LOOKUP_KIND kind = info.compCompHnd->getLocationOfThisType(info.compMethodHnd);

    if (!kind.needsRuntimeLookup)
    {
        void * critSect = 0, **pCrit = 0;
        critSect = info.compCompHnd->getMethodSync(info.compMethodHnd, (void**) &pCrit);
        noway_assert((!critSect) != (!pCrit));

        tree = gtNewIconEmbHndNode(critSect, pCrit, GTF_ICON_METHOD_HDL);
    }
    else
    {
        // Collectible types requires that for shared generic code, if we use the generic context paramter
        // that we report it. (This is a conservative approach, we could detect some cases particularly when the
        // context parameter is this that we don't need the eager reporting logic.)
        lvaGenericsContextUsed = true;

        switch (kind.runtimeLookupKind)
        {
            case CORINFO_LOOKUP_THISOBJ :
            {
                noway_assert(!"Should never get this for static method.");
                break;
            }

            case CORINFO_LOOKUP_CLASSPARAM :
            {
                // In this case, the hidden param is the class handle.
                tree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                break;
            }

            case CORINFO_LOOKUP_METHODPARAM :
            {
                // In this case, the hidden param is the method handle.
                tree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                // Call helper CORINFO_HELP_GETCLASSFROMMETHODPARAM to get the class handle
                // from the method handle.
                tree = gtNewHelperCallNode(CORINFO_HELP_GETCLASSFROMMETHODPARAM,
                                           TYP_I_IMPL, 0,
                                           gtNewArgList(tree));
                break;
            }

            default:
            {
                noway_assert(!"Unknown LOOKUP_KIND");
                break;
            }
        }

        noway_assert(tree);  // tree should now contain the CORINFO_CLASS_HANDLE for the exact class.

        // Given the class handle, get the pointer to the Monitor.
        tree = gtNewHelperCallNode(CORINFO_HELP_GETSYNCFROMCLASSHANDLE,
                                   TYP_I_IMPL, 0,
                                   gtNewArgList(tree));
    }

    noway_assert(tree);
    return tree;

}


#if !defined(_TARGET_X86_)

/*****************************************************************************
 *
 *  Add monitor enter/exit calls for synchronized methods, and a try/fault
 *  to ensure the 'exit' is called if the 'enter' was successful. On x86, we
 *  generate monitor enter/exit calls and tell the VM the code location of
 *  these calls. When an exception occurs between those locations, the VM
 *  automatically releases the lock. For non-x86 platforms, the JIT is
 *  responsible for creating a try/finally to protect the monitor enter/exit,
 *  and the VM doesn't need to know anything special about the method during
 *  exception processing -- it's just a normal try/finally.
 *
 *  We generate the following code:
 *
 *      void Foo()
 *      {
 *          unsigned byte acquired = 0;
 *          try {
 *              JIT_MonEnterWorker(<lock object>, &acquired);
 *
 *              *** all the preexisting user code goes here ***
 *
 *              JIT_MonExitWorker(<lock object>, &acquired);
 *          } fault {
 *              JIT_MonExitWorker(<lock object>, &acquired);
 *         }
 *      L_return:
 *         ret
 *      }
 *
 *  If the lock is actually acquired, then the 'acquired' variable is set to 1
 *  by the helper call. During normal exit, the finally is called, 'acquired'
 *  is 1, and the lock is released. If an exception occurs before the lock is
 *  acquired, but within the 'try' (extremely unlikely, but possible), 'acquired'
 *  will be 0, and the monitor exit call will quickly return without attempting
 *  to release the lock. Otherwise, 'acquired' will be 1, and the lock will be
 *  released during exception processing.
 *
 *  For synchronized methods, we generate a single return block. 
 *  We can do this without creating additional "step" blocks because "ret" blocks
 *  must occur at the top-level (of the original code), not nested within any EH
 *  constructs. From the CLI spec, 12.4.2.8.2.3 "ret": "Shall not be enclosed in any
 *  protected block, filter, or handler." Also, 3.57: "The ret instruction cannot be
 *  used to transfer control out of a try, filter, catch, or finally block. From within
 *  a try or catch, use the leave instruction with a destination of a ret instruction
 *  that is outside all enclosing exception blocks."
 *
 *  In addition, we can add a "fault" at the end of a method and be guaranteed that no
 *  control falls through. From the CLI spec, section 12.4 "Control flow": "Control is not
 *  permitted to simply fall through the end of a method. All paths shall terminate with one
 *  of these instructions: ret, throw, jmp, or (tail. followed by call, calli, or callvirt)."
 *
 *  We only need to worry about "ret" and "throw", as the CLI spec prevents any other
 *  alternatives. Section 15.4.3.3 "Implementation information" states about exiting
 *  synchronized methods: "Exiting a synchronized method using a tail. call shall be
 *  implemented as though the tail. had not been specified." Section 3.37 "jmp" states:
 *  "The jmp instruction cannot be used to transferred control out of a try, filter,
 *  catch, fault or finally block; or out of a synchronized region." And, "throw" will
 *  be handled naturally; no additional work is required.
 */

void                Compiler::fgAddSyncMethodEnterExit()
{
    if ((info.compFlags & CORINFO_FLG_SYNCH) == 0)
        return;

    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

#if !FEATURE_EH
    // If we don't support EH, we can't add the EH needed by synchronized methods.
    // Of course, we could simply ignore adding the EH constructs, since we don't
    // support exceptions being thrown in this mode, but we would still need to add
    // the monitor enter/exit, and that doesn't seem worth it for this minor case.
    // By the time EH is working, we can just enable the whole thing.
    NYI("No support for synchronized methods");
#endif // !FEATURE_EH

    // Create a scratch first BB where we can put the new variable initialization.
    // Don't put the scratch BB in the protected region.

    fgEnsureFirstBBisScratch();

    // Create a block for the start of the try region, where the monitor enter call
    // will go.

    assert(fgFirstBB->bbFallsThrough());

    BasicBlock* tryBegBB = fgNewBBafter(BBJ_NONE, fgFirstBB, false);
    BasicBlock* tryLastBB = fgLastBB;

    // Create a block for the fault.

    assert(!tryLastBB->bbFallsThrough());
    BasicBlock* faultBB = fgNewBBafter(BBJ_EHFINALLYRET, tryLastBB, false);

    assert(tryLastBB->bbNext == faultBB);
    assert(faultBB->bbNext == NULL);
    assert(faultBB == fgLastBB);

    { // Scope the EH region creation

        // Add the new EH region at the end, since it is the least nested,
        // and thus should be last.

        EHblkDsc *  newEntry;
        unsigned    XTnew = compHndBBtabCount;

        newEntry = fgAddEHTableEntry(XTnew);

        // Initialize the new entry

        newEntry->ebdHandlerType    = EH_HANDLER_FAULT;

        newEntry->ebdTryBeg         = tryBegBB;
        newEntry->ebdTryLast        = tryLastBB;

        newEntry->ebdHndBeg         = faultBB;
        newEntry->ebdHndLast        = faultBB;

        newEntry->ebdTyp            = 0;    // unused for fault

        newEntry->ebdEnclosingTryIndex      = EHblkDsc::NO_ENCLOSING_INDEX;
        newEntry->ebdEnclosingHndIndex      = EHblkDsc::NO_ENCLOSING_INDEX;

        newEntry->ebdTryBegOffset    = tryBegBB->bbCodeOffs;
        newEntry->ebdTryEndOffset    = tryLastBB->bbCodeOffsEnd;
        newEntry->ebdFilterBegOffset = 0;
        newEntry->ebdHndBegOffset    = 0;   // handler doesn't correspond to any IL
        newEntry->ebdHndEndOffset    = 0;   // handler doesn't correspond to any IL

        // Set some flags on the new region. This is the same as when we set up
        // EH regions in fgFindBasicBlocks(). Note that the try has no enclosing
        // handler, and the fault has no enclosing try.

        tryBegBB->bbFlags      |= BBF_HAS_LABEL | BBF_DONT_REMOVE | BBF_TRY_BEG | BBF_IMPORTED;

        faultBB->bbFlags     |= BBF_HAS_LABEL | BBF_DONT_REMOVE | BBF_IMPORTED;
        faultBB->bbCatchTyp   = BBCT_FAULT;

        tryBegBB->setTryIndex(XTnew);
        tryBegBB->clearHndIndex();

        faultBB->clearTryIndex();
        faultBB->setHndIndex(XTnew);

        // Walk the user code blocks and set all blocks that don't already have a try handler
        // to point to the new try handler.

        BasicBlock *    tmpBB;
        for (tmpBB = tryBegBB->bbNext; tmpBB != faultBB; tmpBB = tmpBB->bbNext)
        {
            if (!tmpBB->hasTryIndex())
            {
                tmpBB->setTryIndex(XTnew);
            }
        }

        // Walk the EH table. Make every EH entry that doesn't already have an enclosing
        // try index mark this new entry as their enclosing try index.

        unsigned        XTnum;
        EHblkDsc *      HBtab;

        for (XTnum = 0, HBtab = compHndBBtab;
             XTnum < XTnew;
             XTnum++  , HBtab++)
        {
            if (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                HBtab->ebdEnclosingTryIndex = (unsigned short)XTnew;    // This EH region wasn't previously nested, but now it is.
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("Synchronized method - created additional EH descriptor EH#%u for try/fault wrapping monitor enter/exit\n",
                XTnew);
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }

        fgVerifyHandlerTab();
#endif // DEBUG

    }

    // Create a 'monitor acquired' boolean (actually, an unsigned byte: 1 = acquired, 0 = not acquired).

    var_types typeMonAcquired = TYP_UBYTE;
    this->lvaMonAcquired = lvaGrabTemp(true DEBUGARG("Synchronized method monitor acquired boolean"));
  
        
    lvaTable[lvaMonAcquired].lvType = typeMonAcquired;

    { // Scope the variables of the variable initialization

        // Initialize the 'acquired' boolean.

        GenTreePtr zero     = gtNewZeroConNode(genActualType(typeMonAcquired));
        GenTreePtr varNode  = gtNewLclvNode(lvaMonAcquired, typeMonAcquired);
        GenTreePtr initNode = gtNewAssignNode(varNode, zero);

        fgInsertStmtAtEnd(fgFirstBB, initNode);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add 'acquired' initialization in first block BB%02u [%08p]\n", fgFirstBB, dspPtr(fgFirstBB));
            gtDispTree(initNode);
            printf("\n");
        }
#endif

    }

    // Make a copy of the 'this' pointer to be used in the handler so it does not inhibit enregistration
    // of all uses of the variable.
    unsigned lvaCopyThis = 0;
    if (!info.compIsStatic)
    {
        lvaCopyThis = lvaGrabTemp(true DEBUGARG("Synchronized method monitor acquired boolean"));
        lvaTable[lvaCopyThis].lvType = TYP_REF;

        GenTreePtr thisNode  = gtNewLclvNode(info.compThisArg, TYP_REF);
        GenTreePtr copyNode  = gtNewLclvNode(lvaCopyThis, TYP_REF);
        GenTreePtr initNode = gtNewAssignNode(copyNode, thisNode);

        fgInsertStmtAtEnd(tryBegBB, initNode);
    }

    fgCreateMonitorTree(lvaMonAcquired, info.compThisArg, tryBegBB, true /*enter*/);

    // exceptional case
    fgCreateMonitorTree(lvaMonAcquired, lvaCopyThis, faultBB, false /*exit*/);
    
    // non-exceptional cases
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbJumpKind == BBJ_RETURN)
        {
            fgCreateMonitorTree(lvaMonAcquired, info.compThisArg, block, false /*exit*/);
        }
    }
}

// fgCreateMonitorTree: Create tree to execute a monitor enter or exit operation for synchronized methods
//    lvaMonAcquired: lvaNum of boolean variable that tracks if monitor has been acquired.
//    lvaThisVar: lvaNum of variable being used as 'this' pointer, may not be the original one.  Is only used for nonstatic methods
//    block: block to insert the tree in.  It is inserted at the end or in the case of a return, immediately before the GT_RETURN
//    enter: whether to create a monitor enter or exit

GenTree* Compiler::fgCreateMonitorTree(unsigned lvaMonAcquired, unsigned lvaThisVar, BasicBlock* block, bool enter)
{
    // Insert the expression "enter/exitCrit(this, &acquired)" or "enter/exitCrit(handle, &acquired)"

    var_types typeMonAcquired = TYP_UBYTE;
    GenTreePtr varNode      = gtNewLclvNode(lvaMonAcquired, typeMonAcquired);
    GenTreePtr varAddrNode  = gtNewOperNode(GT_ADDR, TYP_BYREF, varNode);
    GenTreePtr tree;

    if  (info.compIsStatic)
    {
        tree = fgGetCritSectOfStaticMethod();
        tree = gtNewHelperCallNode(enter ? CORINFO_HELP_MON_ENTER_STATIC : CORINFO_HELP_MON_EXIT_STATIC,
                                   TYP_VOID, 0,
                                   gtNewArgList(tree, varAddrNode));
    }
    else
    {
        tree = gtNewLclvNode(lvaThisVar, TYP_REF);
        tree = gtNewHelperCallNode(enter ? CORINFO_HELP_MON_ENTER : CORINFO_HELP_MON_EXIT,
                                   TYP_VOID, 0,
                                   gtNewArgList(tree, varAddrNode));
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nSynchronized method - Add monitor %s call to block BB%02u [%08p]\n", enter?"enter":"exit", block, dspPtr(block));
        gtDispTree(tree);
        printf("\n");
    }
#endif

    
    if (block->bbJumpKind == BBJ_RETURN &&
        block->lastStmt()->gtStmtExpr->gtOper == GT_RETURN) 
    {
        GenTree* retNode = block->lastStmt()->gtStmtExpr;
        GenTree* retExpr = retNode->gtOp.gtOp1;
        
        if (retExpr != nullptr)
        {
            // have to insert this immediately before the GT_RETURN so we transform:
            // ret(...) ->
            // ret(comma(comma(tmp=...,call mon_exit), tmp)
            //
            // 
            // Before morph stage, it is possible to have a case of GT_RETURN(TYP_LONG, op1) where op1's type is
            // TYP_STRUCT (of 8-bytes) and op1 is call node. See the big comment block in impReturnInstruction()
            // for details for the case where info.compRetType is not the same as info.compRetNativeType.  For
            // this reason pass compMethodInfo->args.retTypeClass which is guaranteed to be a valid class handle
            // if the return type is a value class.  Note that fgInsertCommFormTemp() in turn uses this class handle
            // if the type of op1 is TYP_STRUCT to perform lvaSetStruct() on the new temp that is created, which
            // in turn passes it to VM to know the size of value type.
            GenTree* temp = fgInsertCommaFormTemp(&retNode->gtOp.gtOp1, info.compMethodInfo->args.retTypeClass);

            GenTree* lclVar = retNode->gtOp.gtOp1->gtOp.gtOp2;
            retNode->gtOp.gtOp1->gtOp.gtOp2 = gtNewOperNode(GT_COMMA, retExpr->TypeGet(), tree, lclVar);
        }
        else
        {
            // Insert this immediately before the GT_RETURN
            fgInsertStmtNearEnd(block, tree);
        }
    }
    else
    {
        fgInsertStmtAtEnd(block, tree);
    }

    return tree;
}




// Convert a BBJ_RETURN block in a synchronized method to a BBJ_ALWAYS.
// We've previously added a 'try' block around the original program code using fgAddSyncMethodEnterExit().
// Thus, we put BBJ_RETURN blocks inside a 'try'. In IL this is illegal. Instead, we would
// see a 'leave' inside a 'try' that would get transformed into BBJ_CALLFINALLY/BBJ_ALWAYS blocks
// during importing, and the BBJ_ALWAYS would point at an outer block with the BBJ_RETURN.
// Here, we mimic some of the logic of importing a LEAVE to get the same effect for synchronized methods.
void                Compiler::fgConvertSyncReturnToLeave(BasicBlock* block)
{
    assert(!fgFuncletsCreated);
    assert(info.compFlags & CORINFO_FLG_SYNCH);
    assert(genReturnBB != nullptr);
    assert(genReturnBB != block);
    assert(fgReturnCount <= 1);  // We have a single return for synchronized methods
    assert(block->bbJumpKind == BBJ_RETURN);
    assert((block->bbFlags & BBF_HAS_JMP) == 0);
    assert(block->hasTryIndex());
    assert(!block->hasHndIndex());
    assert(compHndBBtabCount >= 1);

    unsigned tryIndex = block->getTryIndex();
    assert(tryIndex == compHndBBtabCount - 1);  // The BBJ_RETURN must be at the top-level before we inserted the try/finally, which must be the last EH region.

    EHblkDsc* ehDsc = ehGetDsc(tryIndex);
    assert(ehDsc->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX);  // There are no enclosing regions of the BBJ_RETURN block
    assert(ehDsc->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX);

    // Convert the BBJ_RETURN to BBJ_ALWAYS, jumping to genReturnBB. 
    block->bbJumpKind   = BBJ_ALWAYS;
    block->bbJumpDest   = genReturnBB;
    block->bbJumpDest->bbRefs++;

#ifdef DEBUG
    if (verbose)
    {
        printf("Synchronized method - convert block BB%02u to BBJ_ALWAYS [targets BB%02u]\n",
            block->bbNum, block->bbJumpDest->bbNum);
    }
#endif
}

#endif // !_TARGET_X86_


/*****************************************************************************
 *
 *  Return 'true' if there is more than one BBJ_RETURN block.
 */

bool                Compiler::fgMoreThanOneReturnBlock()
{
    unsigned    retCnt = 0;

    for (BasicBlock * block = fgFirstBB; block; block = block->bbNext)
    {
        if  (block->bbJumpKind == BBJ_RETURN)
        {
            retCnt++;
            if (retCnt > 1)
            {
                return true;
            }
        }
    }

    return false;
}

/*****************************************************************************
 *
 *  Add any internal blocks/trees we may need
 */

void                Compiler::fgAddInternal()
{
    noway_assert(!compIsForInlining());

    /*
        <BUGNUM> VSW441487 </BUGNUM>

        The "this" pointer is implicitly used in the following cases:
            1. Locking of synchronized methods
            2. Dictionary access of shared generics code
            3. If a method has "catch(FooException<T>)", the EH code accesses "this" to determine T.
            4. Initializing the type from generic methods which require precise cctor semantics
            5. Verifier does special handling of "this" in the .ctor

        However, we might overwrite it with a "starg 0".
        In this case, we will redirect all "ldarg(a)/starg(a) 0" to a temp lvaTable[lvaArg0Var]
    */

    if (!info.compIsStatic)
    {
        if (lvaArg0Var != info.compThisArg)
        {
            // When we're using the general encoder, we mark compThisArg address-taken to ensure that it is not
            // enregistered (since the decoder always reports a stack location for "this" for generics
            // context vars).
            bool lva0CopiedForGenericsCtxt;
#ifndef JIT32_GCENCODER
            lva0CopiedForGenericsCtxt = ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0);
#else   // JIT32_GCENCODER
            lva0CopiedForGenericsCtxt = false;
#endif  // JIT32_GCENCODER
            noway_assert(lva0CopiedForGenericsCtxt || !lvaTable[info.compThisArg].lvAddrExposed);
            noway_assert(!lvaTable[info.compThisArg].lvArgWrite);
            noway_assert(lvaTable[lvaArg0Var].lvAddrExposed || lvaTable[lvaArg0Var].lvArgWrite || lva0CopiedForGenericsCtxt);

            var_types  thisType = lvaTable[info.compThisArg].TypeGet();

            // Now assign the original input "this" to the temp

            GenTreePtr tree;

            tree = gtNewLclvNode(lvaArg0Var, thisType);

            tree = gtNewAssignNode(tree,                                             // dst
                                   gtNewLclvNode(info.compThisArg, thisType)         // src
                                  );

            /* Create a new basic block and stick the assignment in it */

            fgEnsureFirstBBisScratch();

            fgInsertStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nCopy \"this\" to lvaArg0Var in first basic block [%08p]\n", dspPtr(fgFirstBB));
                gtDispTree(tree);
                printf("\n");
            }
#endif
        }
    }

    // Grab a temp for the security object.
    // (Note: opts.compDbgEnC currently also causes the security object to be generated. See Compiler::compCompile)
    if (opts.compNeedSecurityCheck)
    {
        noway_assert(lvaSecurityObject == BAD_VAR_NUM);
        lvaSecurityObject = lvaGrabTempWithImplicitUse(false DEBUGARG("security check"));
        lvaTable[lvaSecurityObject].lvType = TYP_REF;
    }

    /* Assume we will generate a single shared return sequence */

    // This is the node for the oneReturn statement.
    // It could be as simple as a CallNode if we only have
    // only one callout. It will be a comma tree of CallNodes
    // if we have multiple callouts.
    //
    GenTreePtr  oneReturnStmtNode = NULL;
    ULONG       returnWeight      = 0;
    bool        oneReturn;
    bool        allProfWeight;

    //
    //  We will generate just one epilog (return block)
    //   when we are asked to generate enter/leave callbacks
    //   or for methods calling into unmanaged code
    //   or for synchronized methods.
    //
    if ( compIsProfilerHookNeeded()    ||
#if INLINE_NDIRECT
         (info.compCallUnmanaged != 0) ||
#endif
         ((info.compFlags & CORINFO_FLG_SYNCH) != 0))
    {
        // We will generate only one return block
        // We will transform the BBJ_RETURN blocks
        //  into jumps to the one return block
        //
        oneReturn     = true;
        allProfWeight = false;
    }
    else
    {
        //
        // We are allowed to have multiple individual exits
        // However we can still decide to have a single return
        //
        oneReturn     = false;
        allProfWeight = true;

        // Count the BBJ_RETURN blocks and set the returnWeight to the
        // sum of all these blocks.
        //
        fgReturnCount = 0;
        for (BasicBlock * block = fgFirstBB; block; block = block->bbNext)
        {
            if  (block->bbJumpKind == BBJ_RETURN)
            {
                //
                // returnCount is the count of BBJ_RETURN blocks in this method
                //
                fgReturnCount++;
                //
                // If all BBJ_RETURN blocks have a valid profiled weights
                // then allProfWeight will be true, else it is false
                //
                if ((block->bbFlags & BBF_PROF_WEIGHT) == 0)
                {
                    allProfWeight = false;
                }
                //
                // returnWeight is the sum of the weights of all BBJ_RETURN blocks
                returnWeight += block->bbWeight;
            }
        }

        //
        // If we only have one (or zero) return blocks then
        // we do not need a special one return block
        //
        if (fgReturnCount > 1)
        {
            //
            // should we generate a single return block?
            //
            if (fgReturnCount > 4)
            {
                // Our epilog encoding only supports up to 4 epilogs
                // TODO-CQ: support >4 return points for ARM/AMD64, which presumably support any number of epilogs?
                //
                oneReturn = true;
            }
            else if (compCodeOpt() == SMALL_CODE)
            {
                // For the Small_Code case we always generate a
                // single return block when we have multiple
                // return points
                //
                oneReturn = true;
            }
        }
    }

#if !defined(_TARGET_X86_)
    // Add the synchronized method enter/exit calls and try/finally protection. Note
    // that this must happen before the one BBJ_RETURN block is created below, so the
    // BBJ_RETURN block gets placed at the top-level, not within an EH region. (Otherwise,
    // we'd have to be really careful when creating the synchronized method try/finally
    // not to include the BBJ_RETURN block.)
    fgAddSyncMethodEnterExit();
#endif // !_TARGET_X86_

    if  (oneReturn)
    {
        genReturnBB = fgNewBBinRegion(BBJ_RETURN);
        genReturnBB->bbRefs = 1;   // bbRefs gets update later, for now it should be 1
        fgReturnCount++;

        if (allProfWeight)
        {
            //
            // if we have profile data for all BBJ_RETURN blocks
            // then we can set BBF_PROF_WEIGHT for genReturnBB
            //
            genReturnBB->bbFlags |= BBF_PROF_WEIGHT;
        }
        else
        {
            //
            // We can't rely upon the calculated returnWeight unless
            // all of the BBJ_RETURN blocks had valid profile weights
            // So we will use the weight of the first block instead
            //
            returnWeight = fgFirstBB->bbWeight;
        }

        //
        // Set the weight of the oneReturn block
        //
        genReturnBB->bbWeight = min(returnWeight, BB_MAX_WEIGHT);

        if (returnWeight == 0)
        {
            //
            // If necessary set the Run Rarely flag
            //
            genReturnBB->bbFlags |= BBF_RUN_RARELY;
        }
        else
        {
            // Make sure that the RunRarely flag is clear
            // because fgNewBBinRegion will set it to true
            //
            genReturnBB->bbFlags &= ~BBF_RUN_RARELY;
        }

        genReturnBB->bbFlags   |= (BBF_INTERNAL | BBF_DONT_REMOVE);

        noway_assert(genReturnBB->bbNext == NULL);

#ifdef DEBUG
        if (verbose)
            printf("\n genReturnBB [BB%02u] created\n", genReturnBB->bbNum);
#endif
    }
    else
    {
        //
        // We don't have a oneReturn block for this method
        //
        genReturnBB = nullptr;
    }

    // If there is a return value, then create a temp for it.  Real returns will store the value in there and
    // it'll be reloaded by the single return.                                    
    if (genReturnBB && compMethodHasRetVal())
    {
        genReturnLocal = lvaGrabTemp(true DEBUGARG("Single return block return value"));

        if (compMethodReturnsNativeScalarType())
        {
            lvaTable[genReturnLocal].lvType = genActualType(info.compRetNativeType);
        }
        else if (compMethodReturnsRetBufAddr())
        {
            lvaTable[genReturnLocal].lvType = TYP_BYREF;
        }
        else if (compMethodReturnsMultiRegRetType())
        {
            lvaTable[genReturnLocal].lvType = TYP_STRUCT;
            lvaSetStruct(genReturnLocal, info.compMethodInfo->args.retTypeClass, true);
#if FEATURE_MULTIREG_ARGS_OR_RET
            lvaTable[genReturnLocal].lvIsMultiRegArgOrRet = true;
#endif
        }
        else
        {
            assert(!"unreached");
        }

        if (varTypeIsFloating(lvaTable[genReturnLocal].lvType))
        {
            this->compFloatingPointUsed = true;
        }

        if (!varTypeIsFloating(info.compRetType))
            lvaTable[genReturnLocal].setPrefReg(REG_INTRET, this);
#ifdef REG_FLOATRET
        else
            lvaTable[genReturnLocal].setPrefReg(REG_FLOATRET, this);
#endif

#ifdef DEBUG
        // This temporary should not be converted to a double in stress mode,
        // because we introduce assigns to it after the stress conversion
        lvaTable[genReturnLocal].lvKeepType = 1;
#endif
    }
    else
    {
        genReturnLocal = BAD_VAR_NUM;
    }

#if INLINE_NDIRECT
    if (info.compCallUnmanaged != 0)
    {
        // The P/Invoke helpers only require a frame variable, so only allocate the
        // TCB variable if we're not using them.
        if (!opts.ShouldUsePInvokeHelpers())
        {
            info.compLvFrameListRoot = lvaGrabTemp(false DEBUGARG("Pinvoke FrameListRoot"));
        }

        lvaInlinedPInvokeFrameVar = lvaGrabTempWithImplicitUse(false DEBUGARG("Pinvoke FrameVar"));

        LclVarDsc *   varDsc = &lvaTable[lvaInlinedPInvokeFrameVar];
        varDsc->addPrefReg(RBM_PINVOKE_TCB, this);
        varDsc->lvType = TYP_BLK;
        // Make room for the inlined frame.
        varDsc->lvExactSize = eeGetEEInfo()->inlinedCallFrameInfo.size;
#if FEATURE_FIXED_OUT_ARGS
        // Grab and reserve space for TCB, Frame regs used in PInvoke epilog to pop the inlined frame.
        // See genPInvokeMethodEpilog() for use of the grabbed var. This is only necessary if we are
        // not using the P/Invoke helpers.
        if (!opts.ShouldUsePInvokeHelpers() && compJmpOpUsed)
        {
            lvaPInvokeFrameRegSaveVar = lvaGrabTempWithImplicitUse(false DEBUGARG("PInvokeFrameRegSave Var"));
            varDsc = &lvaTable[lvaPInvokeFrameRegSaveVar];
            varDsc->lvType = TYP_BLK;
            varDsc->lvExactSize = 2 * REGSIZE_BYTES;
        }
#endif
    }
#endif

    // Do we need to insert a "JustMyCode" callback?

    CORINFO_JUST_MY_CODE_HANDLE *pDbgHandle = NULL;
    CORINFO_JUST_MY_CODE_HANDLE dbgHandle = NULL;
    if (opts.compDbgCode && !(opts.eeFlags & CORJIT_FLG_IL_STUB))
    {
        dbgHandle = info.compCompHnd->getJustMyCodeHandle(info.compMethodHnd, &pDbgHandle);
    }

#ifdef _TARGET_ARM64_
    // TODO-ARM64-NYI: don't do just-my-code
    dbgHandle = nullptr;
    pDbgHandle = nullptr;
#endif // _TARGET_ARM64_

    noway_assert(!dbgHandle || !pDbgHandle);

    if (dbgHandle || pDbgHandle)
    {
        GenTreePtr guardCheckVal = gtNewOperNode(GT_IND, TYP_INT,
                                        gtNewIconEmbHndNode(dbgHandle, pDbgHandle, GTF_ICON_TOKEN_HDL));
        GenTreePtr guardCheckCond = gtNewOperNode(GT_EQ, TYP_INT,
                                        guardCheckVal, gtNewZeroConNode(TYP_INT));
        guardCheckCond->gtFlags |= GTF_RELOP_QMARK;

        // Create the callback which will yield the final answer

        GenTreePtr callback = gtNewHelperCallNode(CORINFO_HELP_DBG_IS_JUST_MY_CODE, TYP_VOID);
        callback = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), callback);

        // Stick the conditional call at the start of the method

        fgEnsureFirstBBisScratch();
        fgInsertStmtAtEnd(fgFirstBB, gtNewQmarkNode(TYP_VOID, guardCheckCond, callback));
    }

    /* Do we need to call out for security ? */

    if  (tiSecurityCalloutNeeded)
    {
        // We must have grabbed this local.
        noway_assert(opts.compNeedSecurityCheck);
        noway_assert(lvaSecurityObject != BAD_VAR_NUM);

        GenTreePtr      tree;

        /* Insert the expression "call JIT_Security_Prolog(MethodHnd, &SecurityObject)" */

        tree = gtNewIconEmbMethHndNode(info.compMethodHnd);

        tree = gtNewHelperCallNode(info.compCompHnd->getSecurityPrologHelper(info.compMethodHnd),
                                   TYP_VOID,
                                   0,
                                   gtNewArgList(tree,
                                                gtNewOperNode(GT_ADDR,
                                                              TYP_BYREF,
                                                              gtNewLclvNode(lvaSecurityObject, TYP_REF))));

        /* Create a new basic block and stick the call in it */

        fgEnsureFirstBBisScratch();

        fgInsertStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\ntiSecurityCalloutNeeded - Add call JIT_Security_Prolog(%08p) statement ", dspPtr(info.compMethodHnd));
            printTreeID(tree);
            printf(" in first basic block [%08p]\n", dspPtr(fgFirstBB));
            gtDispTree(tree);
            printf("\n");
        }
#endif

    }


#if defined(_TARGET_X86_)

    /* Is this a 'synchronized' method? */

    if  (info.compFlags & CORINFO_FLG_SYNCH)
    {
        GenTreePtr      tree = NULL;

        /* Insert the expression "enterCrit(this)" or "enterCrit(handle)" */

        if  (info.compIsStatic)
        {
            tree = fgGetCritSectOfStaticMethod();

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_ENTER_STATIC,
                                       TYP_VOID, 0,
                                       gtNewArgList(tree));
        }
        else
        {
            noway_assert(lvaTable[info.compThisArg].lvType == TYP_REF);

            tree = gtNewLclvNode(info.compThisArg, TYP_REF);

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_ENTER,
                                       TYP_VOID, 0,
                                       gtNewArgList(tree));
        }

        /* Create a new basic block and stick the call in it */

        fgEnsureFirstBBisScratch();

        fgInsertStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add enterCrit statement in first basic block [%08p]\n", dspPtr(fgFirstBB));
            gtDispTree(tree);
            printf("\n");
        }
#endif

        /* We must be generating a single exit point for this to work */

        noway_assert(oneReturn);
        noway_assert(genReturnBB);

        /* Create the expression "exitCrit(this)" or "exitCrit(handle)" */

        if  (info.compIsStatic)
        {
            tree = fgGetCritSectOfStaticMethod();

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_EXIT_STATIC,
                                       TYP_VOID, 0,
                                       gtNewArgList(tree));
        }
        else
        {
            tree = gtNewLclvNode(info.compThisArg, TYP_REF);

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_EXIT,
                                       TYP_VOID, 0,
                                       gtNewArgList(tree));
        }

        /* Add the exitCrit expression to the oneReturnStmtNode */
        if (oneReturnStmtNode != NULL)
        {
            //
            // We add the newly created "tree" to op1, so we can evaluate the last added
            // expression first.
            //
            oneReturnStmtNode = gtNewOperNode(GT_COMMA,
                                              TYP_VOID,
                                              tree,
                                              oneReturnStmtNode);

        }
        else
        {
            oneReturnStmtNode = tree;
        }


#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add exit expression ");
            printTreeID(tree);
            printf(" to oneReturnStmtNode ");
            printTreeID(oneReturnStmtNode);
            printf("\nCurrent oneReturnStmtNode is\n");
            gtDispTree(oneReturnStmtNode);
            printf("\n");
        }
#endif

        // Reset cookies used to track start and end of the protected region in synchronized methods
        syncStartEmitCookie = NULL;
        syncEndEmitCookie = NULL;
    }

#endif // _TARGET_X86_


    /* Do we need to do runtime call out to check the security? */

    if  (tiRuntimeCalloutNeeded)
    {
        GenTreePtr      tree;

        /* Insert the expression "call verificationRuntimeCheck(MethodHnd)" */

        tree = gtNewIconEmbMethHndNode(info.compMethodHnd);

        tree = gtNewHelperCallNode(CORINFO_HELP_VERIFICATION_RUNTIME_CHECK,
                                   TYP_VOID, 0,
                                   gtNewArgList(tree));

        /* Create a new basic block and stick the call in it */

        fgEnsureFirstBBisScratch();

        fgInsertStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\ntiRuntimeCalloutNeeded - Call verificationRuntimeCheck(%08p) statement in first basic block [%08p]\n", dspPtr(info.compMethodHnd), dspPtr(fgFirstBB));
            gtDispTree(tree);
            printf("\n");
        }
#endif

    }

    //
    //  Add 'return' expression to the return block if we made it as "oneReturn" before.
    //
    if (oneReturn)
    {
        GenTreePtr tree;

        //
        // Make the 'return' expression.
        //

        // spill any value that is currently in the oneReturnStmtNode
        if (oneReturnStmtNode != nullptr)
        {
            fgInsertStmtAtEnd(genReturnBB, oneReturnStmtNode);
            oneReturnStmtNode = nullptr;
        }

        //make sure to reload the return value as part of the return (it is saved by the "real return").
        if (genReturnLocal != BAD_VAR_NUM)
        {
            noway_assert(compMethodHasRetVal());

            GenTreePtr retTemp = gtNewLclvNode(genReturnLocal, lvaTable[genReturnLocal].TypeGet());

            //make sure copy prop ignores this node (make sure it always does a reload from the temp).
            retTemp->gtFlags |= GTF_DONT_CSE;
            tree = gtNewOperNode(GT_RETURN, retTemp->gtType, retTemp);
        }
        else
        {
            noway_assert(info.compRetType == TYP_VOID || varTypeIsStruct(info.compRetType));
            // return void
            tree = new (this, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID );
        }

        /* Add 'return' expression to the return block */

        noway_assert(genReturnBB);

        fgInsertStmtAtEnd(genReturnBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\noneReturn statement tree ");
            printTreeID(tree);
            printf(" added to genReturnBB [%08p]\n", dspPtr(genReturnBB));
            gtDispTree(tree);
            printf("\n");
        }
#endif

    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After fgAddInternal()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif
}


/*****************************************************************************
 *
 *  Create a new statement from tree and wire the links up.
 */
GenTreeStmt* Compiler::fgNewStmtFromTree(GenTreePtr tree, BasicBlock* block, IL_OFFSETX offs)
{
    GenTreeStmt* stmt = gtNewStmt(tree, offs);
    gtSetStmtInfo(stmt);
    fgSetStmtSeq(stmt);

#if DEBUG
    if (block != nullptr)
    {
        fgDebugCheckNodeLinks(block, stmt);
    }
#endif

    return stmt;
}

GenTreeStmt* Compiler::fgNewStmtFromTree(GenTreePtr tree)
{
    return fgNewStmtFromTree(tree, nullptr, BAD_IL_OFFSET);
}

GenTreeStmt* Compiler::fgNewStmtFromTree(GenTreePtr tree, BasicBlock* block)
{
    return fgNewStmtFromTree(tree, block, BAD_IL_OFFSET);
}

GenTreeStmt* Compiler::fgNewStmtFromTree(GenTreePtr tree, IL_OFFSETX offs)
{
    return fgNewStmtFromTree(tree, nullptr, offs);
}

//------------------------------------------------------------------------
// fgFindBlockILOffset: Given a block, find the IL offset corresponding to the first statement
//      in the block with a legal IL offset. Skip any leading statements that have BAD_IL_OFFSET.
//      If no statement has an initialized statement offset (including the case where there are
//      no statements in the block), then return BAD_IL_OFFSET. This function is used when
//      blocks are split or modified, and we want to maintain the IL offset as much as possible
//      to preserve good debugging behavior.
//
// Arguments:
//      block - The block to check.
//
// Return Value:
//      The first good IL offset of a statement in the block, or BAD_IL_OFFSET if such an IL offset
//      cannot be found.
//
//      If we are not built with DEBUGGING_SUPPORT or DEBUG, then always report BAD_IL_OFFSET,
//      since in that case statements don't contain an IL offset. The effect will be that split
//      blocks will lose their IL offset information.

IL_OFFSET Compiler::fgFindBlockILOffset(BasicBlock* block)
{
#if defined(DEBUGGING_SUPPORT) || defined(DEBUG)
    for (GenTree* stmt = block->bbTreeList; stmt != nullptr; stmt = stmt->gtNext)
    {
        assert(stmt->IsStatement());
        if (stmt->gtStmt.gtStmtILoffsx != BAD_IL_OFFSET)
        {
            return jitGetILoffs(stmt->gtStmt.gtStmtILoffsx);
        }
    }
#endif // defined(DEBUGGING_SUPPORT) || defined(DEBUG)

    return BAD_IL_OFFSET;
}

//------------------------------------------------------------------------------
// fgSplitBlockAtEnd - split the given block into two blocks.
//                   All code in the block stays in the original block.
//                   Control falls through from original to new block, and
//                   the new block is returned.
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAtEnd(BasicBlock* curr)
{
    // We'd like to use fgNewBBafter(), but we need to update the preds list before linking in the new block.
    // (We need the successors of 'curr' to be correct when we do this.)
    BasicBlock* newBlock = bbNewBasicBlock(curr->bbJumpKind);

    // Start the new block with no refs. When we set the preds below, this will get updated correctly.
    newBlock->bbRefs = 0;

    // For each successor of the original block, set the new block as their predecessor.
    // Note we are using the "rational" version of the successor iterator that does not hide the finallyret arcs.
    // Without these arcs, a block 'b' may not be a member of succs(preds(b))
    if (curr->bbJumpKind != BBJ_SWITCH)
    {
        unsigned numSuccs = curr->NumSucc(this);
        for (unsigned i = 0; i < numSuccs; i++)
        {
            BasicBlock* succ = curr->GetSucc(i, this);
            if (succ != newBlock)
            {
                JITDUMP("BB%02u previous predecessor was BB%02u, now is BB%02u\n", succ->bbNum, curr->bbNum, newBlock->bbNum);
                fgReplacePred(succ, curr, newBlock);
            }
        }

        newBlock->bbJumpDest = curr->bbJumpDest;
        curr->bbJumpDest = nullptr;
    }
    else
    {
        // In the case of a switch statement there's more complicated logic in order to wire up the predecessor lists
        // but fortunately there's an existing method that implements this functionality.
        newBlock->bbJumpSwt = curr->bbJumpSwt;
        
        fgChangeSwitchBlock(curr, newBlock);

        curr->bbJumpSwt = nullptr;
    }

    newBlock->inheritWeight(curr);

    // Set the new block's flags. Note that the new block isn't BBF_INTERNAL unless the old block is.
    newBlock->bbFlags   = curr->bbFlags;

    // Remove flags that the new block can't have.
    newBlock->bbFlags   &= ~(BBF_TRY_BEG | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_HAS_LABEL | BBF_JMP_TARGET |
                             BBF_FUNCLET_BEG | BBF_LOOP_PREHEADER | BBF_KEEP_BBJ_ALWAYS);

    // Remove the GC safe bit on the new block. It seems clear that if we split 'curr' at the end,
    // such that all the code is left in 'curr', and 'newBlock' just gets the control flow, then
    // both 'curr' and 'newBlock' could accurately retain an existing GC safe bit. However, callers
    // use this function to split blocks in the middle, or at the beginning, and they don't seem to
    // be careful about updating this flag appropriately. So, removing the GC safe bit is simply
    // conservative: some functions might end up being fully interruptible that could be partially
    // interruptible if we exercised more care here.
    newBlock->bbFlags   &= ~BBF_GC_SAFE_POINT;

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    newBlock->bbFlags   &= ~(BBF_FINALLY_TARGET);
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

    // The new block has no code, so we leave bbCodeOffs/bbCodeOffsEnd set to BAD_IL_OFFSET. If a caller
    // puts code in the block, then it needs to update these.

    // Insert the new block in the block list after the 'curr' block.
    fgInsertBBafter(curr, newBlock);
    fgExtendEHRegionAfter(curr);             // The new block is in the same EH region as the old block.

    // Remove flags from the old block that are no longer possible.
    curr->bbFlags       &= ~(BBF_HAS_JMP | BBF_RETLESS_CALL);

    // Default to fallthru, and add the arc for that.
    curr->bbJumpKind = BBJ_NONE;
    fgAddRefPred(newBlock, curr);

    return newBlock;
}

//------------------------------------------------------------------------------
// fgSplitBlockAfterStatement - Split the given block, with all code after
//                              the given statement going into the second block.
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAfterStatement(BasicBlock* curr, GenTree* stmt)
{
    BasicBlock* newBlock = fgSplitBlockAtEnd(curr);

    if (stmt)
    {
        newBlock->bbTreeList = stmt->gtNext;
        if (newBlock->bbTreeList)
        {
            newBlock->bbTreeList->gtPrev = curr->bbTreeList->gtPrev;
        }
        curr->bbTreeList->gtPrev = stmt;
        stmt->gtNext = NULL;

        // Update the IL offsets of the blocks to match the split.

        assert(newBlock->bbCodeOffs    == BAD_IL_OFFSET);
        assert(newBlock->bbCodeOffsEnd == BAD_IL_OFFSET);

        // curr->bbCodeOffs remains the same
        newBlock->bbCodeOffsEnd = curr->bbCodeOffsEnd;

        IL_OFFSET splitPointILOffset = fgFindBlockILOffset(newBlock);

        curr->bbCodeOffsEnd  = splitPointILOffset;
        newBlock->bbCodeOffs = splitPointILOffset;
    }
    else
    {
        assert(curr->bbTreeList == nullptr); // if no tree was given then it better be an empty block
    }

    return newBlock;
}

//------------------------------------------------------------------------------
// fgSplitBlockAtBeginning - Split the given block into two blocks.
//                         Control falls through from original to new block,
//                         and the new block is returned.
//                         All code in the original block goes into the new block
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAtBeginning(BasicBlock* curr)
{
    BasicBlock* newBlock = fgSplitBlockAtEnd(curr);

    newBlock->bbTreeList = curr->bbTreeList;
    curr->bbTreeList = NULL;

    // The new block now has all the code, and the old block has none. Update the
    // IL offsets for the block to reflect this.

    newBlock->bbCodeOffs    = curr->bbCodeOffs;
    newBlock->bbCodeOffsEnd = curr->bbCodeOffsEnd;

    curr->bbCodeOffs    = BAD_IL_OFFSET;
    curr->bbCodeOffsEnd = BAD_IL_OFFSET;

    return newBlock;
}

//------------------------------------------------------------------------
// fgSplitEdge: Splits the edge between a block 'curr' and its successor 'succ' by creating a new block
//              that replaces 'succ' as a successor of 'curr', and which branches unconditionally
//              to (or falls through to) 'succ'. Note that for a BBJ_COND block 'curr',
//              'succ' might be the fall-through path or the branch path from 'curr'.
//
// Arguments:
//    curr - A block which branches conditionally to 'succ'
//    succ - The target block
//
// Return Value:
//    Returns a new block, that is a successor of 'curr' and which branches unconditionally to 'succ'
//
// Assumptions:
//    'curr' must have a bbJumpKind of BBJ_COND or BBJ_SWITCH
//
// Notes:
//    The returned block is empty.

BasicBlock* Compiler::fgSplitEdge(BasicBlock* curr, BasicBlock* succ)
{
    assert(curr->bbJumpKind == BBJ_COND || curr->bbJumpKind == BBJ_SWITCH);
    assert(fgGetPredForBlock(succ, curr) != nullptr);

    BasicBlock* newBlock;
    if (succ == curr->bbNext)
    {
        // The successor is the fall-through path of a BBJ_COND, or
        // an immediately following block of a BBJ_SWITCH (which has
        // no fall-through path). For this case, simply insert a new
        // fall-through block after 'curr'.
        newBlock = fgNewBBafter(BBJ_NONE, curr, true /*extendRegion*/);
    }
    else
    {
        newBlock = fgNewBBinRegion(BBJ_ALWAYS, curr, curr->isRunRarely());
        // The new block always jumps to 'succ'
        newBlock->bbJumpDest = succ;
    }
    newBlock->bbFlags |= (curr->bbFlags & succ->bbFlags & (BBF_BACKWARD_JUMP));

    JITDUMP("Splitting edge from BB%02u to BB%02u; adding BB%02u\n", curr->bbNum, succ->bbNum, newBlock->bbNum);

    if (curr->bbJumpKind == BBJ_COND)
    {
        fgReplacePred(succ, curr, newBlock);
        if (curr->bbJumpDest == succ)
        {
            // Now 'curr' jumps to newBlock
            curr->bbJumpDest = newBlock;
            newBlock->bbFlags |= BBF_JMP_TARGET;
        }
        fgAddRefPred(newBlock, curr);
    }
    else
    {
        assert(curr->bbJumpKind == BBJ_SWITCH);

        // newBlock replaces 'succ' in the switch.
        fgReplaceSwitchJumpTarget(curr, newBlock, succ);

        // And 'succ' has 'newBlock' as a new predecessor.
        fgAddRefPred(succ, newBlock);
    }

    // This isn't accurate, but it is complex to compute a reasonable number so just assume that we take the
    // branch 50% of the time.
    newBlock->inheritWeightPercentage(curr, 50);

    // The bbLiveIn and bbLiveOut are both equal to the bbLiveIn of 'succ'
    if (fgLocalVarLivenessDone)
    {
        VarSetOps::Assign(this, newBlock->bbLiveIn, succ->bbLiveIn);
        VarSetOps::Assign(this, newBlock->bbLiveOut, succ->bbLiveIn);
    }

    return newBlock;
}


#if FEATURE_STACK_FP_X87

/*****************************************************************************/
/*****************************************************************************/

void                Compiler::fgComputeFPlvls(GenTreePtr tree)
{
    genTreeOps      oper;
    unsigned        kind;
    bool            isflt;
    unsigned        savFPstkLevel;

    noway_assert(tree);
    noway_assert(tree->gtOper != GT_STMT);

    /* Figure out what kind of a node we have */

    oper  = tree->OperGet();
    kind  = tree->OperKind();
    isflt = varTypeIsFloating(tree->TypeGet()) ? 1 : 0;

    /* Is this a constant or leaf node? */

    if  (kind & (GTK_CONST|GTK_LEAF))
    {
        codeGen->genFPstkLevel += isflt;
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    if  (kind & GTK_SMPOP)
    {
        GenTreePtr      op1 = tree->gtOp.gtOp1;
        GenTreePtr      op2 = tree->gtGetOp2();

        /* Check for some special cases */

        switch (oper)
        {
        case GT_IND:

            fgComputeFPlvls(op1);

            /* Indirect loads of FP values push a new value on the FP stack */

            codeGen->genFPstkLevel += isflt;
            goto DONE;

        case GT_CAST:

            fgComputeFPlvls(op1);

            /* Casts between non-FP and FP push on / pop from the FP stack */

            if  (varTypeIsFloating(op1->TypeGet()))
            {
                if  (isflt == false)
                    codeGen->genFPstkLevel--;
            }
            else
            {
                if  (isflt != false)
                    codeGen->genFPstkLevel++;
            }

            goto DONE;

        case GT_LIST:   /* GT_LIST presumably part of an argument list */
        case GT_COMMA:  /* Comma tosses the result of the left operand */

            savFPstkLevel = codeGen->genFPstkLevel;
            fgComputeFPlvls(op1);
            codeGen->genFPstkLevel = savFPstkLevel;

            if  (op2)
                fgComputeFPlvls(op2);

            goto DONE;

        default:
            break;
        }

        if  (!op1)
        {
            if  (!op2)
                goto DONE;

            fgComputeFPlvls(op2);
            goto DONE;
        }

        if  (!op2)
        {
            fgComputeFPlvls(op1);
            if (oper == GT_ADDR)
            {
                /* If the operand was floating point pop the value from the stack */
                if (varTypeIsFloating(op1->TypeGet()))
                {
                    noway_assert(codeGen->genFPstkLevel);
                    codeGen->genFPstkLevel--;
                }
            }

            // This is a special case to handle the following
            // optimization: conv.i4(round.d(d)) -> round.i(d)

            if (oper== GT_INTRINSIC && tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round &&
                tree->TypeGet()==TYP_INT)
            {
                codeGen->genFPstkLevel--;
            }

            goto DONE;
        }

        /* FP assignments need a bit special handling */

        if  (isflt && (kind & GTK_ASGOP))
        {
            /* The target of the assignment won't get pushed */

            if  (tree->gtFlags & GTF_REVERSE_OPS)
            {
                fgComputeFPlvls(op2);
                fgComputeFPlvls(op1);
                 op1->gtFPlvl--;
                codeGen->genFPstkLevel--;
            }
            else
            {
                fgComputeFPlvls(op1);
                op1->gtFPlvl--;
                codeGen->genFPstkLevel--;
                fgComputeFPlvls(op2);
            }

            codeGen->genFPstkLevel--;
            goto DONE;
        }

        /* Here we have a binary operator; visit operands in proper order */

        if  (tree->gtFlags & GTF_REVERSE_OPS)
        {
            fgComputeFPlvls(op2);
            fgComputeFPlvls(op1);
        }
        else
        {
            fgComputeFPlvls(op1);
            fgComputeFPlvls(op2);
        }

        /*
            Binary FP operators pop 2 operands and produce 1 result;
            assignments consume 1 value and don't produce any.
         */

        if  (isflt)
            codeGen->genFPstkLevel--;

        /* Float compares remove both operands from the FP stack */

        if  (kind & GTK_RELOP)
        {
            if  (varTypeIsFloating(op1->TypeGet()))
                codeGen->genFPstkLevel -= 2;
        }

        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch  (oper)
    {
    case GT_FIELD:
        fgComputeFPlvls(tree->gtField.gtFldObj);
        codeGen->genFPstkLevel += isflt;
        break;

    case GT_CALL:

        if  (tree->gtCall.gtCallObjp)
            fgComputeFPlvls(tree->gtCall.gtCallObjp);

        if  (tree->gtCall.gtCallArgs)
        {
            savFPstkLevel = codeGen->genFPstkLevel;
            fgComputeFPlvls(tree->gtCall.gtCallArgs);
            codeGen->genFPstkLevel = savFPstkLevel;
        }

        if  (tree->gtCall.gtCallLateArgs)
        {
            savFPstkLevel = codeGen->genFPstkLevel;
            fgComputeFPlvls(tree->gtCall.gtCallLateArgs);
            codeGen->genFPstkLevel = savFPstkLevel;
        }

        codeGen->genFPstkLevel += isflt;
        break;

    case GT_ARR_ELEM:

        fgComputeFPlvls(tree->gtArrElem.gtArrObj);

        unsigned dim;
        for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            fgComputeFPlvls(tree->gtArrElem.gtArrInds[dim]);

        /* Loads of FP values push a new value on the FP stack */
        codeGen->genFPstkLevel += isflt;
        break;

    case GT_CMPXCHG:
        //Evaluate the trees left to right
        fgComputeFPlvls(tree->gtCmpXchg.gtOpLocation);
        fgComputeFPlvls(tree->gtCmpXchg.gtOpValue);
        fgComputeFPlvls(tree->gtCmpXchg.gtOpComparand);
        noway_assert(!isflt);
        break;

    case GT_ARR_BOUNDS_CHECK:
        fgComputeFPlvls(tree->gtBoundsChk.gtArrLen);
        fgComputeFPlvls(tree->gtBoundsChk.gtIndex);
        noway_assert(!isflt);
        break;

#ifdef DEBUG
    default:
        noway_assert(!"Unhandled special operator in fgComputeFPlvls()");
        break;
#endif
    }

DONE:

    noway_assert((unsigned char)codeGen->genFPstkLevel == codeGen->genFPstkLevel);

    tree->gtFPlvl = (unsigned char)codeGen->genFPstkLevel;
}

#endif // FEATURE_STACK_FP_X87

/*****************************************************************************/
/*****************************************************************************/

void                Compiler::fgFindOperOrder()
{
#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgFindOperOrder()\n");
#endif

    BasicBlock*     block;
    GenTreeStmt*    stmt;

    /* Walk the basic blocks and for each statement determine
     * the evaluation order, cost, FP levels, etc... */

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        compCurBB = block;
        for (stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            /* Recursively process the statement */

            gtSetStmtInfo(stmt);
        }
    }
}

/*****************************************************************************/
void Compiler::fgSimpleLowering()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        // Walk the statement trees in this basic block, converting ArrLength nodes.
        compCurBB = block; // Used in fgRngChkTarget.

#if JIT_FEATURE_SSA_SKIP_DEFS
        for (GenTreeStmt* stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNextStmt)
#else
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
#endif
        {
            for (GenTreePtr tree = stmt->gtStmtList; tree; tree = tree->gtNext)
            {
                if (tree->gtOper == GT_ARR_LENGTH)
                {
                    GenTreeArrLen*  arrLen = tree->AsArrLen();
                    GenTreePtr      arr = arrLen->gtArrLen.ArrRef();
                    GenTreePtr      add;
                    GenTreePtr      con;

                    /* Create the expression "*(array_addr + ArrLenOffs)" */

                    noway_assert(arr->gtNext == tree);

                    noway_assert(arrLen->ArrLenOffset() == offsetof(CORINFO_Array, length) ||
                                 arrLen->ArrLenOffset() == offsetof(CORINFO_String, stringLen));

                    if ((arr->gtOper == GT_CNS_INT) &&
                        (arr->gtIntCon.gtIconVal == 0))
                    {
                        // If the array is NULL, then we should get a NULL reference
                        // exception when computing its length.  We need to maintain
                        // an invariant where there is no sum of two constants node, so
                        // let's simply return an indirection of NULL.

                        add = arr;
                    }
                    else
                    {
                        con = gtNewIconNode(arrLen->ArrLenOffset(), TYP_I_IMPL);
                        con->gtRsvdRegs = 0;
                        con->gtCopyFPlvl(arr);
                        add = gtNewOperNode(GT_ADD, TYP_REF, arr, con);
                        add->gtRsvdRegs = arr->gtRsvdRegs;
                        add->gtCopyFPlvl(arr);
                        add->CopyCosts(arr);
                        arr->gtNext = con;
                        con->gtPrev = arr;

                        con->gtNext = add;
                        add->gtPrev = con;

                        add->gtNext = tree;
                        tree->gtPrev = add;
                    }

                    // Change to a GT_IND.
                    tree->ChangeOperUnchecked(GT_IND);

                    tree->gtOp.gtOp1 = add;
                }
                else if (tree->OperGet() == GT_ARR_BOUNDS_CHECK
#ifdef FEATURE_SIMD
                         || tree->OperGet() == GT_SIMD_CHK
#endif // FEATURE_SIMD
                        )
                {
                    // Add in a call to an error routine.
                    fgSetRngChkTarget(tree, false);
                }
            }
        }
    }

#ifdef DEBUG
    if (verbose && fgRngChkThrowAdded)
    {
        printf("\nAfter fgSimpleLowering() added some RngChk throw blocks");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif
}

/*****************************************************************************
 */

void                Compiler::fgUpdateRefCntForClone(BasicBlock* addedToBlock,
                                                     GenTreePtr  clonedTree)
{
    assert(clonedTree->gtOper != GT_STMT);

    if (lvaLocalVarRefCounted)
    {
        compCurBB = addedToBlock;
        fgWalkTreePre(&clonedTree, Compiler::lvaIncRefCntsCB, (void *)this, true);
    }
}

/*****************************************************************************
 */

void                Compiler::fgUpdateRefCntForExtract(GenTreePtr  wholeTree,
                                                       GenTreePtr  keptTree)
{
    if (lvaLocalVarRefCounted)
    {
        /*  Update the refCnts of removed lcl vars - The problem is that
         *  we have to consider back the side effects trees so we first
         *  increment all refCnts for side effects then decrement everything
         *  in the statement
         */
        if (keptTree)
        {
            fgWalkTreePre(&keptTree, Compiler::lvaIncRefCntsCB, (void *)this, true);
        }

        fgWalkTreePre(   &wholeTree, Compiler::lvaDecRefCntsCB, (void *)this, true);
    }
}

VARSET_VALRET_TP    Compiler::fgGetVarBits(GenTreePtr tree)
{
    VARSET_TP VARSET_INIT_NOCOPY(varBits, VarSetOps::MakeEmpty(this));

    assert(tree->gtOper == GT_LCL_VAR ||
           tree->gtOper == GT_LCL_FLD ||
           tree->gtOper == GT_REG_VAR);

    unsigned int lclNum = tree->gtLclVarCommon.gtLclNum;
    LclVarDsc*   varDsc = lvaTable + lclNum;
    if (varDsc->lvTracked)
    {
        VarSetOps::AddElemD(this, varBits, varDsc->lvVarIndex);
    }
    else if (varDsc->lvType == TYP_STRUCT && varDsc->lvPromoted)
    {
        for (unsigned i = varDsc->lvFieldLclStart;
             i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
             ++i)
        {
            noway_assert(lvaTable[i].lvIsStructField);
            if  (lvaTable[i].lvTracked)
            {
                unsigned varIndex = lvaTable[i].lvVarIndex;
                noway_assert(varIndex < lvaTrackedCount);
                VarSetOps::AddElemD(this, varBits, varIndex);
            }
        }
    }
    return varBits;
}

/*****************************************************************************
 *
 *  Find and remove any basic blocks that are useless (e.g. they have not been
 *  imported because they are not reachable, or they have been optimized away).
 */

void                Compiler::fgRemoveEmptyBlocks()
{
    BasicBlock*     cur;
    BasicBlock*     nxt;

    /* If we remove any blocks, we'll have to do additional work */

    unsigned        removedBlks = 0;

    for (cur = fgFirstBB; cur != nullptr; cur = nxt)
    {
        /* Get hold of the next block (in case we delete 'cur') */

        nxt = cur->bbNext;

        /* Should this block be removed? */

        if  (!(cur->bbFlags & BBF_IMPORTED))
        {
            noway_assert(cur->isEmpty());

            if (ehCanDeleteEmptyBlock(cur))
            {
                /* Mark the block as removed */

                cur->bbFlags |= BBF_REMOVED;

                /* Remember that we've removed a block from the list */

                removedBlks++;

#ifdef DEBUG
                if (verbose)
                {
                    printf("BB%02u was not imported, marked as removed (%d)\n",
                        cur->bbNum, removedBlks);
                }
#endif // DEBUG

                /* Drop the block from the list */

                fgUnlinkBlock(cur);
            }
            else
            {
                // We were prevented from deleting this block by EH normalization. Mark the block as imported.
                cur->bbFlags |= BBF_IMPORTED;
            }
        }
    }

    /* If no blocks were removed, we're done */

    if  (removedBlks == 0)
        return;

    /*  Update all references in the exception handler table.
     *  Mark the new blocks as non-removable.
     *
     *  We may have made the entire try block unreachable.
     *  Check for this case and remove the entry from the EH table.
     */

    unsigned        XTnum;
    EHblkDsc*       HBtab;
    INDEBUG(unsigned delCnt = 0;)

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
AGAIN:
        /* If the beginning of the try block was not imported, we
         * need to remove the entry from the EH table. */

        if (HBtab->ebdTryBeg->bbFlags & BBF_REMOVED)
        {
            noway_assert(!(HBtab->ebdTryBeg->bbFlags & BBF_IMPORTED));
#ifdef DEBUG
            if (verbose)
            {
                printf("Beginning of try block (BB%02u) not imported "
                       "- remove index #%u from the EH table\n",
                       HBtab->ebdTryBeg->bbNum,
                       XTnum + delCnt);
            }
            delCnt++;
#endif // DEBUG

            fgRemoveEHTableEntry(XTnum);

            if (XTnum < compHndBBtabCount)
            {
                // There are more entries left to process, so do more. Note that
                // HBtab now points to the next entry, that we copied down to the
                // current slot. XTnum also stays the same.
                goto AGAIN;
            }

            break; // no more entries (we deleted the last one), so exit the loop
        }

        /* At this point we know we have a valid try block */

#ifdef DEBUG
        assert(HBtab->ebdTryBeg->bbFlags & BBF_IMPORTED);
        assert(HBtab->ebdTryBeg->bbFlags & BBF_DONT_REMOVE);

        assert(HBtab->ebdHndBeg->bbFlags & BBF_IMPORTED);
        assert(HBtab->ebdHndBeg->bbFlags & BBF_DONT_REMOVE);

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdFilter->bbFlags & BBF_IMPORTED);
            assert(HBtab->ebdFilter->bbFlags & BBF_DONT_REMOVE);
        }
#endif // DEBUG

        fgSkipRmvdBlocks(HBtab);
    } /* end of the for loop over XTnum */

    // Renumber the basic blocks
    JITDUMP("\nRenumbering the basic blocks for fgRemoveEmptyBlocks\n");
    fgRenumberBlocks();

#ifdef DEBUG
    fgVerifyHandlerTab();
#endif // DEBUG
}

/*****************************************************************************
 * fgRemoveLinearOrderDependencies --
 *
 *   Remove stmt dependencies before removing the stmt itself.
 *
 * If called on a top level statement,
 *
 * All the first level (in a breadth-first order) embedded statements now become top
 * level statements. In a comma world, it is analogous to retaining the exprs
 * within the commas of a statement.
 *
 * If called on an embedded statement,
 *
 * Then the statement rooting the embedded statement's next links are
 * correctly updated to point to any nested embedded statement nodes or
 * the sibling embedded nodes and their prev links are updated to the
 * rooting statement. Also the nodes of the embedded statement on which
 * we are called are dropped from the list.
 *
 * Assumptions:
 * "stmt" should be detached from the bbTreeList after the call.
 *
 */
void Compiler::fgRemoveLinearOrderDependencies(GenTreePtr tree)
{
    assert(fgOrder == FGOrderLinear);
    GenTreeStmt* stmt = tree->AsStmt();
    
    // No embedded statements.
    if (stmt->gtStmtIsTopLevel() &&
        (stmt->gtNext == nullptr || stmt->gtNextStmt->gtStmtIsTopLevel()))
    {
        return;
    }
    // stmt is last embedded statement, assume we have a tree order: prevStmt->stmt->nextStmt.
    // We are dropping "stmt". So fix the next link for "prevStmt" and prev link for "nextStmt".
    if (stmt->gtStmtIsEmbedded() &&
            (stmt->gtNext == nullptr || stmt->gtNextStmt->gtStmtIsTopLevel()))
    {
        if (stmt->gtStmtList->gtPrev)
        {
            stmt->gtStmtList->gtPrev->gtNext = stmt->gtStmtExpr->gtNext;
        }
        if (stmt->gtStmtExpr->gtNext)
        {
            stmt->gtStmtExpr->gtNext->gtPrev = stmt->gtStmtList->gtPrev;
        }
        return;
    }

    //
    // Walk the tree list, and define current statement as
    // the immediate statement (embedded or top) in which
    // the tree resides.
    //
    // Initially, next = stmt, cur is empty.
    //
    // While walking the tree list, we expect to see:
    // 1. next stmt's list
    // 2. or current stmt's expr
    //
    // If current stmt's expr is seen, pop to previous
    // next and call it current.
    // If next stmt's list is seen, then next becomes current.
    //

    ArrayStack<GenTreePtr> stack(this);

    // Consider this example:
    //
    // In stmt order:
    // (top (stmt (emb2, emb3) ) ) where "top" embeds "stmt" and "stmt"
    // nests "emb2" and "emb3". Now we are removing "stmt."
    //
    // In the end we should obtain:
    // (top (emb2, emb3) ). Callers should fix bbTreeList. We only fix tree order.
    // 
    // So in tree order:
    // BEFORE: top:t1 -> stmt:t1 -> emb2:t1 -> stmt:t2 -> emb3:t1 -> stmt:t3 -> top:t2
    // AFTER : top:t1 ->            emb2:t1 ->            emb3:t1 ->            top:t2
    //
    GenTreePtr lastNestEmbedNode = stmt->gtStmtList->gtPrev; // In the example, top:t1.

    GenTreePtr next = stmt;
    GenTreePtr node = stmt->gtStmtList;
    while (node != stmt->gtStmtExpr->gtNext)
    {
        // We are encountering a new stmt. Push it into the stack. It is now current.
        if (next != nullptr && node == next->gtStmt.gtStmtList)
        {
            stack.Push(next);
            next = next->gtNext;

            // Since stack height is 2, we are entering the next level embedded statement
            // from stmt's level which is 1. Reminder: stmt is being removed.
            //
            // If stmt is top level, all level 2 stmts will become top level.
            // So don't fix their prev next links.
            if (stmt->gtStmtIsEmbedded() && stack.Height() == 2)
            {
                //
                // Two cases:
                // Case 1 (Initial case -- we are discovering the first embedded stmt):
                // Before:
                // topList -> stmtList -> emb2List -> emb2Expr -> ... -> stmtExpr -> topExpr
                // Currently: "node" is emb2List and "lastNestEmbedNode" is topList. We started the iteration from stmtList.
                // After:
                // topList ->             emb2List -> emb2Expr -> ... -> stmtExpr -> topExpr.
                //
                // Case 2 (We already discovered an embedded stmt):
                // Before: 
                // ... -> emb2List -> emb2Expr -> stmtNode -> stmtNode -> emb3List -> emb3Expr -> stmtNode -> ... -> stmtExpr
                // Currently, "node" is emb3List and "lastNestEmbedNode" is emb2Expr.
                // After:
                // ... -> emb2List -> emb2Expr ->                      -> emb3List -> emb3Expr -> stmtNode -> ... -> stmtExpr
                //

                // Drop stmtNodes that occur between emb2Expr and emb3List. 
                if (lastNestEmbedNode)
                {
                    lastNestEmbedNode->gtNext = node;
                }
                node->gtPrev = lastNestEmbedNode;
            }
        }
        GenTreePtr cur = stack.Top();
        if (node == cur->gtStmt.gtStmtExpr)
        {
            // A stmt goes out of being current.
            stack.Pop();

            // Keep track of the last nested embedded stmt node. In the example, record emb2Expr or emb3Expr.
            if (stack.Height() == 1)
            {
                lastNestEmbedNode = node;
            }

            // Are we called on a top level statement?
            if (stmt->gtStmtIsTopLevel() && stack.Height() == 1)
            {
                // We are just done visiting the last node of a first level embedded stmt.

                // Before: 
                // stmtList -> emb2List -> emb2Expr -> stmtNode -> stmtNode -> emb3List -> emb3Expr -> stmtNode -> ... -> stmtExpr
                // "stmt" is top level.
                // 
                // Currently, "node" is emb2Expr and "lastNestEmbedNode" is "don't care".
                //
                // After:
                // node =                             stmtNode -> stmtNode -> emb3List -> emb3Expr -> stmtNode -> ... -> stmtExpr
                // nullptr <- emb2List -> emb2Expr -> nullptr
                //
                // stmtList -> emb2List -> emb2Expr -> ...
                // This is inconsistent for stmt, as there is no first level embedded statement now, but since callers
                // are supposed to remove stmt, we don't care.
                //

                // Advance node to next, so we don't set node->next to nullptr below.
                node = node->gtNext;

                noway_assert(cur->gtStmt.gtStmtIsEmbedded());

                // This embedded stmt is now top level since the original top level stmt
                // is going to be removed.
                cur->gtFlags |= GTF_STMT_TOP_LEVEL;

                cur->gtStmt.gtStmtList->gtPrev = nullptr;
                cur->gtStmt.gtStmtExpr->gtNext = nullptr;

                // Don't bother updating stmt's pointers, as we are removing it.
                continue;
            }
        }
        node = node->gtNext;
    }

    // Are we called on an embedded stmt?
    if (stmt->gtStmtIsEmbedded())
    {
        //
        // Before: 
        // ... -> emb2List -> emb2Expr -> stmtNode -> stmtNode -> emb3List -> emb3Expr -> stmtNode -> ... -> stmtExpr -> topNode
        // 
        // Currently, "node" is topNode (i.e., stmtExpr->gtNext) and "lastNestEmbedNode" is emb3Expr.
        //
        // After:
        // ... -> emb2List -> emb2Expr ->                      -> emb3List -> emb3Expr ->                             -> topNode
        //
        if (node)
        {
            node->gtPrev = lastNestEmbedNode;
        }
        if (lastNestEmbedNode)
        {
            lastNestEmbedNode->gtNext = node;
        }
    }
}

/*****************************************************************************
 *
 * Remove a useless statement from a basic block.
 * The default is to decrement ref counts of included vars
 *
 */

void                Compiler::fgRemoveStmt(BasicBlock*     block,
                                           GenTreePtr      node,
                                           // whether to decrement ref counts for tracked vars in statement
                                           bool updateRefCount)
{
    noway_assert(node);

    GenTreeStmt*    tree = block->firstStmt();
    GenTreeStmt*    stmt = node->AsStmt();

#ifdef DEBUG
    if (verbose && stmt->gtStmtExpr->gtOper != GT_NOP) // Don't print if it is a GT_NOP. Too much noise from the inliner.
    {
        printf("\nRemoving statement ");
        printTreeID(stmt);
        printf(" in BB%02u as useless:\n", block->bbNum);
        gtDispTree(stmt);
    }
#endif // DEBUG

    if (opts.compDbgCode &&
        stmt->gtPrev != stmt &&
        stmt->gtStmtILoffsx != BAD_IL_OFFSET)
    {
        /* TODO: For debuggable code, should we remove significant
           statement boundaries. Or should we leave a GT_NO_OP in its place? */
    }

    if (fgOrder == FGOrderLinear)
    {
        fgRemoveLinearOrderDependencies(stmt);
    }
    
    /* Is it the first statement in the list? */

    GenTreeStmt* firstStmt = block->firstStmt();
    if  (firstStmt == stmt)
    {
        if (firstStmt->gtNext == nullptr)
        {
            assert(firstStmt == block->lastStmt());

            /* this is the only statement - basic block becomes empty */
            block->bbTreeList = nullptr;
        }
        else
        {
            block->bbTreeList         = tree->gtNext;
            block->bbTreeList->gtPrev = tree->gtPrev;
        }
        goto DONE;
    }

    /* Is it the last statement in the list? */

    if  (stmt == block->lastStmt())
    {
        stmt->gtPrev->gtNext      = nullptr;
        block->bbTreeList->gtPrev = stmt->gtPrev;
        goto DONE;
    }

    tree = stmt->gtPrevStmt;
    noway_assert(tree);

    tree->gtNext         = stmt->gtNext;
    stmt->gtNext->gtPrev = tree;

DONE:
    fgStmtRemoved = true;

    if (optValnumCSE_phase)
    {
        optValnumCSE_UnmarkCSEs(stmt->gtStmtExpr, nullptr);
    }
    else
    {
        if (updateRefCount)
        {
            if (fgStmtListThreaded)
            {
                fgWalkTreePre(&stmt->gtStmtExpr,
                              Compiler::lvaDecRefCntsCB,
                              (void*) this,
                              true);
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        if  (block->bbTreeList == 0)
        {
            printf("\nBB%02u becomes empty", block->bbNum);
        }
        printf("\n");
    }
#endif // DEBUG
}

/******************************************************************************/
// Returns true if the operator is involved in control-flow
// TODO-Cleanup: Move this into genTreeKinds in genTree.h

inline
bool    OperIsControlFlow(genTreeOps oper)
{
    switch (oper)
    {
    case GT_JTRUE:
    case GT_SWITCH:
    case GT_LABEL:

    case GT_CALL:
    case GT_JMP:

    case GT_RETURN:
    case GT_RETFILT:
#if !FEATURE_EH_FUNCLETS
    case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
        return true;

    default:
        return false;
    }
}

/******************************************************************************
 *  Tries to throw away a stmt. The statement can be anywhere in block->bbTreeList.
 *  Returns true if it did remove the statement.
 */

bool                Compiler::fgCheckRemoveStmt(BasicBlock* block, GenTreePtr node)
{
    if (opts.compDbgCode)
        return false;

    GenTreeStmt* stmt = node->AsStmt();

    GenTreePtr tree = stmt->gtStmtExpr;
    genTreeOps oper = tree->OperGet();

    if (OperIsControlFlow(oper) || oper == GT_NO_OP)
        return false;

    // TODO: Use a recursive version of gtNodeHasSideEffects()
    if (tree->gtFlags & GTF_SIDE_EFFECT)
        return false;

    fgRemoveStmt(block, stmt);
    return true;
}

/****************************************************************************************************
 *
 *
 */
bool                Compiler::fgCanCompactBlocks(BasicBlock* block, BasicBlock* bNext)
{
    if ((block == NULL) || (bNext == NULL))
        return false;

    noway_assert(block->bbNext == bNext);

    if (block->bbJumpKind != BBJ_NONE)
        return false;

    // If the next block has multiple incoming edges, we can still compact if the first block is empty.
    // However, not if it is the beginning of a handler.
    if (bNext->countOfInEdges() != 1 && 
        (!block->isEmpty() || (block->bbFlags & BBF_FUNCLET_BEG) || (block->bbCatchTyp != BBCT_NONE)))
        return false;

    if (bNext->bbFlags & BBF_DONT_REMOVE)
        return false;

    // Don't compact the first block if it was specially created as a scratch block.
    if (fgBBisScratch(block))
        return false;

#if defined(_TARGET_ARM_)
    // We can't compact a finally target block, as we need to generate special code for such blocks during code generation
    if ((bNext->bbFlags & BBF_FINALLY_TARGET) != 0)
        return false;
#endif 

    // We don't want to compact blocks that are in different Hot/Cold regions
    //
    if (fgInDifferentRegions(block, bNext))
    {
        return false;
    }

    // We cannot compact two blocks in different EH regions.
    //
    if (fgCanRelocateEHRegions)
    {
        if (!BasicBlock::sameEHRegion(block, bNext))
        {
            return false;
        }
    }
    // if there is a switch predecessor don't bother because we'd have to update the uniquesuccs as well
    // (if they are valid)
    for (flowList* pred = bNext->bbPreds; pred; pred = pred->flNext)
    {
        if (pred->flBlock->bbJumpKind == BBJ_SWITCH)
            return false;
    }

    return true;
}


/*****************************************************************************************************
 *
 *  Function called to compact two given blocks in the flowgraph
 *  Assumes that all necessary checks have been performed,
 *  i.e. fgCanCompactBlocks returns true.
 *
 *  Uses for this function - whenever we change links, insert blocks,...
 *  It will keep the flowgraph data in synch - bbNum, bbRefs, bbPreds
 */

void                Compiler::fgCompactBlocks(BasicBlock* block, BasicBlock* bNext)
{
    noway_assert(block != NULL);
    noway_assert((block->bbFlags & BBF_REMOVED) == 0);
    noway_assert(block->bbJumpKind == BBJ_NONE);

    noway_assert(bNext == block->bbNext);
    noway_assert(bNext != NULL);
    noway_assert((bNext->bbFlags & BBF_REMOVED) == 0);
    noway_assert(bNext->countOfInEdges() == 1 || block->isEmpty());
    noway_assert(bNext->bbPreds);

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    noway_assert((bNext->bbFlags & BBF_FINALLY_TARGET) == 0);
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

    // Make sure the second block is not the start of a TRY block or an exception handler

    noway_assert(bNext->bbCatchTyp == BBCT_NONE);
    noway_assert((bNext->bbFlags & BBF_TRY_BEG)     == 0);
    noway_assert((bNext->bbFlags & BBF_DONT_REMOVE) == 0);

    /* both or none must have an exception handler */
    noway_assert(block->hasTryIndex() == bNext->hasTryIndex());

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nCompacting blocks BB%02u and BB%02u:\n", block->bbNum, bNext->bbNum);
    }
#endif

    if (bNext->countOfInEdges() > 1)
    {
        JITDUMP("Second block has multiple incoming edges\n");

        assert(block->isEmpty());
        block->bbFlags |= BBF_JMP_TARGET;
        for (flowList* pred = bNext->bbPreds; pred; pred = pred->flNext)
        {
            fgReplaceJumpTarget(pred->flBlock, block, bNext);
                
            if (pred->flBlock != block)
            {
                fgAddRefPred(block, pred->flBlock);
            }
        }
        bNext->bbPreds = nullptr;
    }
    else
    {
        noway_assert(bNext->bbPreds->flNext == nullptr);
        noway_assert(bNext->bbPreds->flBlock == block);
    }

    /* Start compacting - move all the statements in the second block to the first block */

    // First move any phi definitions of the second block after the phi defs of the first.
    // TODO-CQ: This may be the wrong thing to do.  If we're compacting blocks, it's because a
    // control-flow choice was constant-folded away.  So probably phi's need to go away,
    // as well, in favor of one of the incoming branches.  Or at least be modified.
    GenTreePtr blkNonPhi1   = block->FirstNonPhiDef();
    GenTreePtr bNextNonPhi1 = bNext->FirstNonPhiDef();
    GenTreePtr blkFirst     = block->firstStmt();
    GenTreePtr bNextFirst   = bNext->firstStmt();

    // Does the second have any phis?
    if (bNextFirst != NULL && bNextFirst != bNextNonPhi1)
    {
        GenTreePtr bNextLast = bNextFirst->gtPrev;
        assert(bNextLast->gtNext == NULL);

        // Does "blk" have phis?
        if (blkNonPhi1 != blkFirst)
        {
            // Yes, has phis.
            // Insert after the last phi of "block."
            // First, bNextPhis after last phi of block.
            GenTreePtr blkLastPhi;
            if (blkNonPhi1 != NULL)
            {
                blkLastPhi = blkNonPhi1->gtPrev;
            }
            else
            {
                blkLastPhi = blkFirst->gtPrev;
            }

            blkLastPhi->gtNext = bNextFirst;
            bNextFirst->gtPrev = blkLastPhi;

            // Now, rest of "block" after last phi of "bNext".
            GenTreePtr bNextLastPhi = NULL;
            if (bNextNonPhi1 != NULL)
            {
                bNextLastPhi = bNextNonPhi1->gtPrev;
            }
            else
            {
                bNextLastPhi = bNextFirst->gtPrev;
            }

            bNextLastPhi->gtNext = blkNonPhi1;
            if (blkNonPhi1 != NULL)
            {
                blkNonPhi1->gtPrev = bNextLastPhi;
            }
            else
            {
                // block has no non phis, so make the last statement be the last added phi.
                blkFirst->gtPrev = bNextLastPhi;
            }

            // Now update the bbTreeList of "bNext".
            bNext->bbTreeList = bNextNonPhi1;
            if (bNextNonPhi1 != NULL)
            {
                bNextNonPhi1->gtPrev = bNextLast;
            }
        }
        else
        {
            if (blkFirst != NULL) // If "block" has no statements, fusion will work fine...
            {
                // First, bNextPhis at start of block.
                GenTreePtr blkLast = blkFirst->gtPrev;
                block->bbTreeList = bNextFirst;
                // Now, rest of "block" (if it exists) after last phi of "bNext".
                GenTreePtr bNextLastPhi = NULL;
                if (bNextNonPhi1 != NULL)
                {
                    // There is a first non phi, so the last phi is before it.
                    bNextLastPhi = bNextNonPhi1->gtPrev;
                }
                else
                {
                    // All the statements are phi defns, so the last one is the prev of the first.
                    bNextLastPhi = bNextFirst->gtPrev;
                }
                bNextFirst->gtPrev = blkLast;
                bNextLastPhi->gtNext = blkFirst;
                blkFirst->gtPrev = bNextLastPhi;
                // Now update the bbTreeList of "bNext"
                bNext->bbTreeList = bNextNonPhi1;
                if (bNextNonPhi1 != NULL)
                {
                    bNextNonPhi1->gtPrev = bNextLast;
                }
            }
        }
    }

    // Now proceed with the updated bbTreeLists.
    GenTreePtr stmtList1 = block->firstStmt();
    GenTreePtr stmtList2 = bNext->firstStmt();

    /* the block may have an empty list */

    if (stmtList1)
    {
        GenTreePtr stmtLast1 = block->lastStmt();

        /* The second block may be a GOTO statement or something with an empty bbTreeList */
        if (stmtList2)
        {
            GenTreePtr stmtLast2 = bNext->lastStmt();

            /* append list2 to list 1 */

            stmtLast1->gtNext = stmtList2;
                                stmtList2->gtPrev = stmtLast1;
            stmtList1->gtPrev = stmtLast2;
        }
    }
    else
    {
        /* block was formerly empty and now has bNext's statements */
        block->bbTreeList = stmtList2;
    }

    // Note we could update the local variable weights here by
    // calling lvaMarkLocalVars, with the block and weight adjustment.

    // If either block or bNext has a profile weight
    // or if both block and bNext have non-zero weights
    // then we select the highest weight block.

    if ((block->bbFlags & BBF_PROF_WEIGHT)   ||
        (bNext->bbFlags & BBF_PROF_WEIGHT)   ||
        (block->bbWeight && bNext->bbWeight)    )
    {
        // We are keeping block so update its fields
        // when bNext has a greater weight

        if (block->bbWeight < bNext->bbWeight)
        {
            block->bbWeight = bNext->bbWeight;

            block->bbFlags |= (bNext->bbFlags & BBF_PROF_WEIGHT); // Set the profile weight flag (if necessary)
            if (block->bbWeight != 0)
            {
                block->bbFlags &= ~BBF_RUN_RARELY;   // Clear any RarelyRun flag
            }
        }
    }
    // otherwise if either block has a zero weight we select the zero weight
    else
    {
        noway_assert((block->bbWeight == BB_ZERO_WEIGHT) || (bNext->bbWeight == BB_ZERO_WEIGHT));
        block->bbWeight = BB_ZERO_WEIGHT;
        block->bbFlags |= BBF_RUN_RARELY;            // Set the RarelyRun flag
    }

    /* set the right links */

    block->bbJumpKind = bNext->bbJumpKind;
    VarSetOps::AssignAllowUninitRhs(this, block->bbLiveOut, bNext->bbLiveOut);

    // Update the beginning and ending IL offsets (bbCodeOffs and bbCodeOffsEnd).
    // Set the beginning IL offset to the minimum, and the ending offset to the maximum, of the respective blocks.
    // If one block has an unknown offset, we take the other block.
    // We are merging into 'block', so if its values are correct, just leave them alone.
    // TODO: we should probably base this on the statements within.

    if (block->bbCodeOffs == BAD_IL_OFFSET)
    {
        block->bbCodeOffs = bNext->bbCodeOffs; // If they are both BAD_IL_OFFSET, this doesn't change anything.
    }
    else if (bNext->bbCodeOffs != BAD_IL_OFFSET)
    {
        // The are both valid offsets; compare them.
        if (block->bbCodeOffs > bNext->bbCodeOffs)
        {
            block->bbCodeOffs = bNext->bbCodeOffs;
        }
    }

    if (block->bbCodeOffsEnd == BAD_IL_OFFSET)
    {
        block->bbCodeOffsEnd = bNext->bbCodeOffsEnd; // If they are both BAD_IL_OFFSET, this doesn't change anything.
    }
    else if (bNext->bbCodeOffsEnd != BAD_IL_OFFSET)
    {
        // The are both valid offsets; compare them.
        if (block->bbCodeOffsEnd < bNext->bbCodeOffsEnd)
        {
            block->bbCodeOffsEnd = bNext->bbCodeOffsEnd;
        }
    }

    if (((block->bbFlags & BBF_INTERNAL) != 0) &&
        ((bNext->bbFlags & BBF_INTERNAL) == 0)    )
    {
        // If 'block' is an internal block and 'bNext' isn't, then adjust the flags set on 'block'.
        block->bbFlags    &= ~BBF_INTERNAL;             // Clear the BBF_INTERNAL flag
        block->bbFlags    |= BBF_IMPORTED;              // Set the BBF_IMPORTED flag
    }

    /* Update the flags for block with those found in bNext */

    block->bbFlags |= (bNext->bbFlags & BBF_COMPACT_UPD);

    /* mark bNext as removed */

    bNext->bbFlags |= BBF_REMOVED;

    /* Unlink bNext and update all the marker pointers if necessary */

    fgUnlinkRange(block->bbNext, bNext);

    // If bNext was the last block of a try or handler, update the EH table.

    ehUpdateForDeletedBlock(bNext);

    /* If we're collapsing a block created after the dominators are
       computed, rename the block and reuse dominator information from
       the other block */
    if (fgDomsComputed && block->bbNum > fgDomBBcount)
    {
        BlockSetOps::Assign(this, block->bbReach, bNext->bbReach);
        BlockSetOps::ClearD(this, bNext->bbReach);

        block->bbIDom = bNext->bbIDom;
        bNext->bbIDom = nullptr;

        // In this case, there's no need to update the preorder and postorder numbering
        // since we're changing the bbNum, this makes the basic block all set.
        block->bbNum = bNext->bbNum;
    }

    /* Set the jump targets */

    switch (bNext->bbJumpKind)
    {
    case BBJ_CALLFINALLY:
        // Propagate RETLESS property
        block->bbFlags |= (bNext->bbFlags & BBF_RETLESS_CALL);

        __fallthrough;

    case BBJ_COND:
    case BBJ_ALWAYS:
    case BBJ_EHCATCHRET:
        block->bbJumpDest = bNext->bbJumpDest;

        /* Update the predecessor list for 'bNext->bbJumpDest' */
        fgReplacePred(bNext->bbJumpDest, bNext, block);

        /* Update the predecessor list for 'bNext->bbNext' if it is different than 'bNext->bbJumpDest' */
        if (bNext->bbJumpKind == BBJ_COND && bNext->bbJumpDest != bNext->bbNext)
        {
            fgReplacePred(bNext->bbNext, bNext, block);
        }
        break;

    case BBJ_NONE:
        /* Update the predecessor list for 'bNext->bbNext' */
        fgReplacePred(bNext->bbNext,     bNext, block);
        break;

    case BBJ_EHFILTERRET:
        fgReplacePred(bNext->bbJumpDest, bNext, block);
        break;

    case BBJ_EHFINALLYRET:
        {
            unsigned hndIndex = block->getHndIndex();
            EHblkDsc* ehDsc = ehGetDsc(hndIndex);

            if (ehDsc->HasFinallyHandler()) // No need to do this for fault handlers
            {
                BasicBlock* begBlk;
                BasicBlock* endBlk;
                ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                BasicBlock* finBeg = ehDsc->ebdHndBeg;

                for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                {
                    if  (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                        continue;

                    noway_assert(bcall->isBBCallAlwaysPair());
                    fgReplacePred(bcall->bbNext, bNext, block);
                }
            }
        }
        break;

    case BBJ_THROW:
    case BBJ_RETURN:
        /* no jumps or fall through blocks to set here */
        break;

    case BBJ_SWITCH:
        block->bbJumpSwt = bNext->bbJumpSwt;
        // We are moving the switch jump from bNext to block.  Examine the jump targets
        // of the BBJ_SWITCH at bNext and replace the predecessor to 'bNext' with ones to 'block'
        fgChangeSwitchBlock(bNext, block);
        break;

    default:
        noway_assert(!"Unexpected bbJumpKind");
        break;
    }

    fgUpdateLoopsAfterCompacting(block, bNext);

#if DEBUG
    if (verbose && 0)
    {
        printf("\nAfter compacting:\n");
        fgDispBasicBlocks(false);
    }
#endif

#if DEBUG
    if (JitConfig.JitSlowDebugChecksEnabled() != 0)
    {
        // Make sure that the predecessor lists are accurate
        fgDebugCheckBBlist();
    }
#endif // DEBUG
}

void Compiler::fgUpdateLoopsAfterCompacting(BasicBlock * block, BasicBlock* bNext)
{
    /* Check if the removed block is not part the loop table */
    noway_assert(bNext);

    for (unsigned loopNum = 0; loopNum < optLoopCount; loopNum++)
    {
        /* Some loops may have been already removed by
         * loop unrolling or conditional folding */

        if (optLoopTable[loopNum].lpFlags & LPFLG_REMOVED)
            continue;

        /* Check the loop head (i.e. the block preceding the loop) */

        if  (optLoopTable[loopNum].lpHead == bNext)
        {
            optLoopTable[loopNum].lpHead = block;
        }

        /* Check the loop bottom */

        if  (optLoopTable[loopNum].lpBottom == bNext)
        {
            optLoopTable[loopNum].lpBottom = block;
        }

        /* Check the loop exit */

        if  (optLoopTable[loopNum].lpExit == bNext)
        {
            noway_assert(optLoopTable[loopNum].lpExitCnt == 1);
            optLoopTable[loopNum].lpExit = block;
        }

        /* Check the loop entry */

        if  (optLoopTable[loopNum].lpEntry == bNext)
        {
            optLoopTable[loopNum].lpEntry = block;
        }
    }
}

/*****************************************************************************************************
 *
 *  Function called to remove a block when it is unreachable.
 *
 *  This function cannot remove the first block.
 */

void                Compiler::fgUnreachableBlock(BasicBlock* block)
{
    // genReturnBB should never be removed, as we might have special hookups there.
    // Therefore, we should never come here to remove the statements in the genReturnBB block.
    // For example, <BUGNUM> in VSW 364383, </BUGNUM>
    // the profiler hookup needs to have the "void GT_RETURN" statement
    // to properly set the info.compProfilerCallback flag.
    noway_assert(block != genReturnBB);

    if (block->bbFlags & BBF_REMOVED)
        return;

    /* Removing an unreachable block */

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nRemoving unreachable BB%02u\n", block->bbNum);
    }
#endif // DEBUG

    noway_assert(block->bbPrev != nullptr); // Can use this function to remove the first block

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    assert(!block->bbPrev->isBBCallAlwaysPair()); // can't remove the BBJ_ALWAYS of a BBJ_CALLFINALLY / BBJ_ALWAYS pair
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

    /* First walk the statement trees in this basic block and delete each stmt */

    /* Make the block publicly available */
    compCurBB = block;

#if JIT_FEATURE_SSA_SKIP_DEFS
    // TODO-Cleanup: I'm not sure why this happens -- if the block is unreachable, why does it have phis?
    // Anyway, remove any phis.
    GenTreePtr firstNonPhi = block->FirstNonPhiDef();
    if (block->bbTreeList != firstNonPhi)
    {
        if (firstNonPhi != NULL)
        {
            firstNonPhi->gtPrev = block->lastStmt();
        }
        block->bbTreeList = firstNonPhi;
    }
#endif // JIT_FEATURE_SSA_SKIP_DEFS

    for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
    {
        fgRemoveStmt(block, stmt);
    }
    noway_assert(block->bbTreeList == 0);

    /* Next update the loop table and bbWeights */
    optUpdateLoopsBeforeRemoveBlock(block);

    /* Mark the block as removed */
    block->bbFlags |= BBF_REMOVED;

    /* update bbRefs and bbPreds for the blocks reached by this block */
    fgRemoveBlockAsPred(block);
}


/*****************************************************************************************************
 *
 *  Function called to remove or morph a GT_JTRUE statement when we jump to the same
 *  block when both the condition is true or false.
 */
void                Compiler::fgRemoveJTrue(BasicBlock *block)
{
    noway_assert(block->bbJumpKind == BBJ_COND && block->bbJumpDest == block->bbNext);

    flowList* flow = fgGetPredForBlock(block->bbNext, block);
    noway_assert(flow->flDupCount==2);

    // Change the BBJ_COND to BBJ_NONE, and adjust the refCount and dupCount.
    block->bbJumpKind = BBJ_NONE;
    block->bbFlags &= ~BBF_NEEDS_GCPOLL;
    --block->bbNext->bbRefs;
    --flow->flDupCount;

#ifdef DEBUG
    block->bbJumpDest = NULL;
    if (verbose)
        printf("Block BB%02u becoming a BBJ_NONE to BB%02u (jump target is the same whether the condition is true or false)\n",
                block->bbNum, block->bbNext->bbNum);
#endif

    /* Remove the block jump condition */

    GenTreeStmt* test = block->lastTopLevelStmt();

    GenTree* tree = test->gtStmtExpr;

    noway_assert(tree->gtOper == GT_JTRUE);

    GenTree* sideEffList = NULL;

    if (tree->gtFlags & GTF_SIDE_EFFECT)
    {
        if (compRationalIRForm)
        {
            // if we are in rational form don't try to extract the side effects
            // because gtExtractSideEffList will create new comma nodes 
            // (which we would have to rationalize) and fgMorphBlockStmt can't 
            // handle embedded statements.  

            // Instead just transform the JTRUE into a NEG which has the effect of
            // evaluating the side-effecting tree and perform a benign operation on it.
            tree->SetOper(GT_NEG);
            tree->gtType = TYP_I_IMPL;
        }
        else
        {
            gtExtractSideEffList(tree, &sideEffList);

            if (sideEffList)
            {
                noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                if (verbose)
                {
                    printf("Extracted side effects list from condition...\n");
                    gtDispTree(sideEffList); printf("\n");
                }
#endif
            }
        }
    }

    // Delete the cond test or replace it with the side effect tree
    if (sideEffList == NULL)
    {
        if (!compRationalIRForm || (tree->gtFlags & GTF_SIDE_EFFECT) == 0)
            fgRemoveStmt(block, test);
    }
    else
    {
        test->gtStmtExpr = sideEffList;

        fgMorphBlockStmt(block, test DEBUGARG("fgRemoveJTrue"));
    }
}


/*****************************************************************************************************
 *
 *  Function to return the last basic block in the main part of the function. With funclets, it is
 *  the block immediately before the first funclet.
 *  An inclusive end of the main method.
 */

BasicBlock*         Compiler::fgLastBBInMainFunction()
{
#if FEATURE_EH_FUNCLETS

    if (fgFirstFuncletBB != NULL)
    {
        return fgFirstFuncletBB->bbPrev;
    }

#endif // FEATURE_EH_FUNCLETS

    assert(fgLastBB->bbNext == NULL);

    return fgLastBB;
}

/*****************************************************************************************************
 *
 *  Function to return the first basic block after the main part of the function. With funclets, it is
 *  the block of the first funclet.  Otherwise it is NULL if there are no funclets (fgLastBB->bbNext).
 *  This is equivalent to fgLastBBInMainFunction()->bbNext
 *  An exclusive end of the main method.
 */

BasicBlock*         Compiler::fgEndBBAfterMainFunction()
{
#if FEATURE_EH_FUNCLETS

    if (fgFirstFuncletBB != NULL)
    {
        return fgFirstFuncletBB;
    }

#endif // FEATURE_EH_FUNCLETS

    assert(fgLastBB->bbNext == NULL);

    return NULL;
}


// Removes the block from the bbPrev/bbNext chain
// Updates fgFirstBB and fgLastBB if necessary
// Does not update fgFirstFuncletBB or fgFirstColdBlock (fgUnlinkRange does)

void Compiler::fgUnlinkBlock(BasicBlock* block)
{
    if (block->bbPrev)
    {
        block->bbPrev->bbNext = block->bbNext;
        if (block->bbNext)
        {
            block->bbNext->bbPrev = block->bbPrev;
        }
        else
        {
            fgLastBB = block->bbPrev;
        }
    }
    else
    {
        assert(block == fgFirstBB);
        assert(block != fgLastBB);
        assert((fgFirstBBScratch == nullptr) || (fgFirstBBScratch == fgFirstBB));

        fgFirstBB = block->bbNext;
        fgFirstBB->bbPrev = nullptr;

        if (fgFirstBBScratch != nullptr)
        {
#ifdef DEBUG
            // We had created an initial scratch BB, but now we're deleting it.
            if (verbose)
            {
                printf("Unlinking scratch BB%02u\n", block->bbNum);
            }
#endif // DEBUG
            fgFirstBBScratch = nullptr;
        }
    }
}


/*****************************************************************************************************
 *
 *  Function called to unlink basic block range [bBeg .. bEnd] from the basic block list.
 *
 *  'bBeg' can't be the first block.
 */

void                Compiler::fgUnlinkRange(BasicBlock*   bBeg,
                                            BasicBlock*   bEnd)
{
    assert(bBeg != nullptr);
    assert(bEnd != nullptr);

    BasicBlock* bPrev = bBeg->bbPrev;
    assert(bPrev != nullptr); // Can't unlink a range starting with the first block

    bPrev->setNext(bEnd->bbNext);

    /* If we removed the last block in the method then update fgLastBB */
    if (fgLastBB == bEnd)
    {
        fgLastBB = bPrev;
        noway_assert(fgLastBB->bbNext == NULL);
    }

    // If bEnd was the first Cold basic block update fgFirstColdBlock
    if (fgFirstColdBlock == bEnd)
    {
        fgFirstColdBlock = bPrev->bbNext;
    }

#if FEATURE_EH_FUNCLETS
#ifdef DEBUG
    // You can't unlink a range that includes the first funclet block. A range certainly
    // can't cross the non-funclet/funclet region. And you can't unlink the first block
    // of the first funclet with this, either. (If that's necessary, it could be allowed
    // by updating fgFirstFuncletBB to bEnd->bbNext.)
    for (BasicBlock* tempBB = bBeg; tempBB != bEnd->bbNext; tempBB = tempBB->bbNext)
    {
        assert(tempBB != fgFirstFuncletBB);
    }
#endif // DEBUG
#endif // FEATURE_EH_FUNCLETS
}


/*****************************************************************************************************
 *
 *  Function called to remove a basic block
 */

void                Compiler::fgRemoveBlock(BasicBlock*   block,
                                            bool          unreachable)
{
    BasicBlock* bPrev = block->bbPrev;

    /* The block has to be either unreachable or empty */

    PREFIX_ASSUME(block != NULL);

    JITDUMP("fgRemoveBlock BB%02u\n", block->bbNum);

    // If we've cached any mappings from switch blocks to SwitchDesc's (which contain only the
    // *unique* successors of the switch block), invalidate that cache, since an entry in one of
    // the SwitchDescs might be removed.
    InvalidateUniqueSwitchSuccMap();

    noway_assert((block == fgFirstBB) || (bPrev && (bPrev->bbNext == block)));
    noway_assert(!(block->bbFlags & BBF_DONT_REMOVE));

    // Should never remove a genReturnBB, as we might have special hookups there.
    noway_assert(block != genReturnBB);

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    // Don't remove a finally target
    assert(!(block->bbFlags & BBF_FINALLY_TARGET));
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

    if (unreachable)
    {
        PREFIX_ASSUME(bPrev != NULL);

        fgUnreachableBlock(block);

        /* If this is the last basic block update fgLastBB */
        if  (block == fgLastBB)
        {
            fgLastBB = bPrev;
        }

#if FEATURE_EH_FUNCLETS
        // If block was the fgFirstFuncletBB then set fgFirstFuncletBB to block->bbNext
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->bbNext;
        }
#endif // FEATURE_EH_FUNCLETS

        if (bPrev->bbJumpKind == BBJ_CALLFINALLY)
        {
            // bPrev CALL becomes RETLESS as the BBJ_ALWAYS block is unreachable
            bPrev->bbFlags |= BBF_RETLESS_CALL;

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
            NO_WAY("No retless call finally blocks; need unwind target instead");
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        }
        else if (bPrev->bbJumpKind == BBJ_ALWAYS         &&
                 bPrev->bbJumpDest == block->bbNext      &&
                 !(bPrev->bbFlags & BBF_KEEP_BBJ_ALWAYS) &&
                 (block != fgFirstColdBlock)             &&
                 (block->bbNext != fgFirstColdBlock))
        {
            // previous block is a BBJ_ALWAYS to the next block: change to BBJ_NONE.
            // Note that we don't do it if bPrev follows a BBJ_CALLFINALLY block (BBF_KEEP_BBJ_ALWAYS),
            // because that would violate our invariant that BBJ_CALLFINALLY blocks are followed by
            // BBJ_ALWAYS blocks.
            bPrev->bbJumpKind = BBJ_NONE;
            bPrev->bbFlags &= ~BBF_NEEDS_GCPOLL;
        }

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block == fgFirstColdBlock)
        {
            fgFirstColdBlock = block->bbNext;
        }

        /* Unlink this block from the bbNext chain */
        fgUnlinkBlock(block);

        /* At this point the bbPreds and bbRefs had better be zero */
        noway_assert((block->bbRefs == 0) && (block->bbPreds == nullptr));

        /*  A BBJ_CALLFINALLY is usually paired with a BBJ_ALWAYS.
         *  If we delete such a BBJ_CALLFINALLY we also delete the BBJ_ALWAYS
         */
        if (block->isBBCallAlwaysPair())
        {
            BasicBlock* leaveBlk = block->bbNext;
            noway_assert(leaveBlk->bbJumpKind == BBJ_ALWAYS);

            leaveBlk->bbFlags &= ~BBF_DONT_REMOVE;
            leaveBlk->bbRefs   = 0;
            leaveBlk->bbPreds  = nullptr;

            fgRemoveBlock(leaveBlk, true);

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
            fgClearFinallyTargetBit(leaveBlk->bbJumpDest);
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        }
        else if (block->bbJumpKind == BBJ_RETURN)
        {
            fgRemoveReturnBlock(block);
        }
    }
    else // block is empty
    {
        noway_assert(block->isEmpty());

        /* The block cannot follow a non-retless BBJ_CALLFINALLY (because we don't know who may jump to it) */
        noway_assert((bPrev == nullptr) || !bPrev->isBBCallAlwaysPair());

        /* This cannot be the last basic block */
        noway_assert(block != fgLastBB);

#ifdef DEBUG
        if  (verbose)
        {
            printf("Removing empty BB%02u\n", block->bbNum);
        }
#endif // DEBUG

        /* Some extra checks for the empty case */

#ifdef DEBUG
        switch (block->bbJumpKind)
        {
        case BBJ_NONE:
            break;

        case BBJ_ALWAYS:
            /* Do not remove a block that jumps to itself - used for while (true){} */
            noway_assert(block->bbJumpDest != block);

            /* Empty GOTO can be removed iff bPrev is BBJ_NONE */
            noway_assert(bPrev && bPrev->bbJumpKind == BBJ_NONE);
            break;

        default:
            noway_assert(!"Empty block of this type cannot be removed!");
            break;
        }
#endif // DEBUG

        noway_assert(block->bbJumpKind == BBJ_NONE || block->bbJumpKind == BBJ_ALWAYS);

        /* Who is the "real" successor of this block? */

        BasicBlock* succBlock;

        if (block->bbJumpKind == BBJ_ALWAYS)
        {
            succBlock = block->bbJumpDest;
        }
        else
        {
            succBlock = block->bbNext;
        }

        bool skipUnmarkLoop = false;

        // If block is the backedge for a loop and succBlock precedes block
        // then the succBlock becomes the new LOOP HEAD
        // NOTE: there's an assumption here that the blocks are numbered in increasing bbNext order.
        // NOTE 2: if fgDomsComputed is false, then we can't check reachability. However, if this is
        // the case, then the loop structures probably are also invalid, and shouldn't be used. This
        // can be the case late in compilation (such as Lower), where remnants of earlier created
        // structures exist, but haven't been maintained.
        if (block->isLoopHead() && (succBlock->bbNum <= block->bbNum))
        {
            succBlock->bbFlags |= BBF_LOOP_HEAD;
            if (fgDomsComputed && fgReachable(succBlock, block))
            {
                /* Mark all the reachable blocks between 'succBlock' and 'block', excluding 'block' */
                optMarkLoopBlocks(succBlock, block, true);
            }
        }
        else if (succBlock->isLoopHead() && bPrev && (succBlock->bbNum <= bPrev->bbNum))
        {
            skipUnmarkLoop = true;
        }

        noway_assert(succBlock);

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block == fgFirstColdBlock)
        {
            fgFirstColdBlock = block->bbNext;
        }

#if FEATURE_EH_FUNCLETS
        // Update fgFirstFuncletBB if necessary
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->bbNext;
        }
#endif // FEATURE_EH_FUNCLETS

        /* First update the loop table and bbWeights */
        optUpdateLoopsBeforeRemoveBlock(block, skipUnmarkLoop);

        /* Remove the block */

        if (bPrev == NULL)
        {
            /* special case if this is the first BB */

            noway_assert(block == fgFirstBB);

            /* Must be a fall through to next block */

            noway_assert(block->bbJumpKind == BBJ_NONE);

            /* old block no longer gets the extra ref count for being the first block */
            block->bbRefs--;
            succBlock->bbRefs++;

            /* Set the new firstBB */
            fgUnlinkBlock(block);

            /* Always treat the initial block as a jump target */
            fgFirstBB->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
        }
        else
        {
            fgUnlinkBlock(block);
        }

        /* mark the block as removed and set the change flag */

        block->bbFlags |= BBF_REMOVED;

        /* Update bbRefs and bbPreds.
         * All blocks jumping to 'block' now jump to 'succBlock'.
         * First, remove 'block' from the predecessor list of succBlock.
         */

        fgRemoveRefPred(succBlock, block);

        for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
        {
            BasicBlock* predBlock = pred->flBlock;

            /* Are we changing a loop backedge into a forward jump? */

            if ( block->isLoopHead()                   &&
                 (predBlock->bbNum >= block->bbNum)    &&
                 (predBlock->bbNum <= succBlock->bbNum)  )
            {
                /* First update the loop table and bbWeights */
                optUpdateLoopsBeforeRemoveBlock(predBlock);
            }

            /* If predBlock is a new predecessor, then add it to succBlock's
               predecessor's list. */
            if (predBlock->bbJumpKind != BBJ_SWITCH)
            {
                // Even if the pred is not a switch, we could have a conditional branch
                // to the fallthrough, so duplicate there could be preds
                for (unsigned i=0; i<pred->flDupCount; i++)
                    fgAddRefPred(succBlock, predBlock);
            }

            /* change all jumps to the removed block */
            switch (predBlock->bbJumpKind)
            {
            default:
                noway_assert(!"Unexpected bbJumpKind in fgRemoveBlock()");
                break;

            case BBJ_NONE:
                noway_assert(predBlock == bPrev);
                PREFIX_ASSUME(bPrev != NULL);

                /* In the case of BBJ_ALWAYS we have to change the type of its predecessor */
                if (block->bbJumpKind == BBJ_ALWAYS)
                {
                    /* bPrev now becomes a BBJ_ALWAYS */
                    bPrev->bbJumpKind = BBJ_ALWAYS;
                    bPrev->bbJumpDest = succBlock;
                }
                break;

            case BBJ_COND:
                /* The links for the direct predecessor case have already been updated above */
                if (predBlock->bbJumpDest != block)
                {
                    succBlock->bbFlags |= BBF_HAS_LABEL | BBF_JMP_TARGET;
                    break;
                }

                /* Check if both side of the BBJ_COND now jump to the same block */
                if (predBlock->bbNext == succBlock)
                {
                    // Make sure we are replacing "block" with "succBlock" in predBlock->bbJumpDest.
                    noway_assert(predBlock->bbJumpDest == block);
                    predBlock->bbJumpDest = succBlock;
                    fgRemoveJTrue(predBlock);
                    break;
                }

                /* Fall through for the jump case */
                __fallthrough;

            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                noway_assert(predBlock->bbJumpDest == block);
                predBlock->bbJumpDest = succBlock;
                succBlock->bbFlags |= BBF_HAS_LABEL | BBF_JMP_TARGET;
                break;

            case BBJ_SWITCH:
                // Change any jumps from 'predBlock' (a BBJ_SWITCH) to 'block' to jump to 'succBlock'
                //
                // For the jump targets of 'predBlock' (a BBJ_SWITCH) that jump to 'block'
                // remove the old predecessor at 'block' from 'predBlock'  and
                // add the new predecessor at 'succBlock' from 'predBlock'
                //
                fgReplaceSwitchJumpTarget(predBlock, succBlock, block);
                break;
            }
        }
    }

    if (bPrev != NULL)
    {
        switch (bPrev->bbJumpKind)
        {
        case BBJ_CALLFINALLY:
            // If prev is a BBJ_CALLFINALLY it better be marked as RETLESS
            noway_assert(bPrev->bbFlags & BBF_RETLESS_CALL);
            break;

        case BBJ_ALWAYS:
            // Check for branch to next block. Just make sure the BBJ_ALWAYS block is not
            // part of a BBJ_CALLFINALLY/BBJ_ALWAYS pair. We do this here and don't rely on fgUpdateFlowGraph
            // because we can be called by ComputeDominators and it expects it to remove this jump to
            // the next block. This is the safest fix. We should remove all this BBJ_CALLFINALLY/BBJ_ALWAYS
            // pairing.

            if ((bPrev->bbJumpDest == bPrev->bbNext) &&
                !fgInDifferentRegions(bPrev, bPrev->bbJumpDest))   // We don't remove a branch from Hot -> Cold
            {
                if ((bPrev == fgFirstBB) || !bPrev->bbPrev->isBBCallAlwaysPair())
                {
                    // It's safe to change the jump type
                    bPrev->bbJumpKind = BBJ_NONE;
                    bPrev->bbFlags &= ~BBF_NEEDS_GCPOLL;
                }
            }
            break;

        case BBJ_COND:
            /* Check for branch to next block */
            if (bPrev->bbJumpDest == bPrev->bbNext)
            {
                fgRemoveJTrue(bPrev);
            }
            break;

        default:
            break;
        }

        ehUpdateForDeletedBlock(block);
    }
}


/*****************************************************************************
 *
 *  Function called to connect to block that previously had a fall through
 */

BasicBlock*         Compiler::fgConnectFallThrough(BasicBlock* bSrc,
                                                   BasicBlock* bDst)
{
    BasicBlock* jmpBlk = NULL;

    /* If bSrc is non-NULL */

    if (bSrc != NULL)
    {
        /* If bSrc falls through to a block that is not bDst, we will insert a jump to bDst */

        if (bSrc->bbFallsThrough() && (bSrc->bbNext != bDst))
        {
            switch (bSrc->bbJumpKind)
            {

            case BBJ_NONE:
                bSrc->bbJumpKind = BBJ_ALWAYS;
                bSrc->bbJumpDest = bDst;
                bSrc->bbJumpDest->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
#ifdef DEBUG
                if  (verbose)
                {
                    printf("Block BB%02u ended with a BBJ_NONE, Changed to an unconditional jump to BB%02u\n",
                           bSrc->bbNum, bSrc->bbJumpDest->bbNum);
                }
#endif
                break;

            case BBJ_CALLFINALLY:
            case BBJ_COND:

                // Add a new block after bSrc which jumps to 'bDst'
                jmpBlk = fgNewBBafter(BBJ_ALWAYS, bSrc, true);

                if (fgComputePredsDone)
                {
                    fgAddRefPred(jmpBlk, bSrc, fgGetPredForBlock(bDst, bSrc));
                }

                // When adding a new jmpBlk we will set the bbWeight and bbFlags
                //
                if (fgHaveValidEdgeWeights)
                {
                    noway_assert(fgComputePredsDone);

                    flowList* newEdge = fgGetPredForBlock(jmpBlk, bSrc);

                    jmpBlk->bbWeight = (newEdge->flEdgeWeightMin + newEdge->flEdgeWeightMax) / 2;
                    if (bSrc->bbWeight == 0)
                    {
                        jmpBlk->bbWeight = 0;
                    }

                    if (jmpBlk->bbWeight == 0)
                    {
                        jmpBlk->bbFlags |= BBF_RUN_RARELY;
                    }

                    BasicBlock::weight_t weightDiff = (newEdge->flEdgeWeightMax - newEdge->flEdgeWeightMin);
                    BasicBlock::weight_t slop       = BasicBlock::GetSlopFraction(bSrc, bDst);

                    //
                    // If the [min/max] values for our edge weight is within the slop factor
                    //  then we will set the BBF_PROF_WEIGHT flag for the block
                    //
                    if (weightDiff <= slop)
                    {
                        jmpBlk->bbFlags |= BBF_PROF_WEIGHT;
                    }
                }
                else
                {
                    // We set the bbWeight to the smaller of bSrc->bbWeight or bDst->bbWeight
                    if (bSrc->bbWeight < bDst->bbWeight)
                    {
                        jmpBlk->bbWeight  = bSrc->bbWeight;
                        jmpBlk->bbFlags  |= (bSrc->bbFlags & BBF_RUN_RARELY);
                    }
                    else
                    {
                        jmpBlk->bbWeight  = bDst->bbWeight;
                        jmpBlk->bbFlags  |= (bDst->bbFlags & BBF_RUN_RARELY);
                    }
                }

                jmpBlk->bbJumpDest = bDst;
                jmpBlk->bbJumpDest->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);

                if (fgComputePredsDone)
                {
                    fgReplacePred(bDst, bSrc, jmpBlk);
                }
                else
                {
                    jmpBlk->bbFlags |= BBF_IMPORTED;
                }

#ifdef DEBUG
                if  (verbose)
                {
                    printf("Added an unconditional jump to BB%02u after block BB%02u\n",
                           jmpBlk->bbJumpDest->bbNum, bSrc->bbNum);
                }
#endif // DEBUG
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
            }
        }
        else
        {
            // If bSrc is an unconditional branch to the next block
            // then change it to a BBJ_NONE block
            //
            if ( (bSrc->bbJumpKind == BBJ_ALWAYS)       &&
                !(bSrc->bbFlags & BBF_KEEP_BBJ_ALWAYS)  &&
                 (bSrc->bbJumpDest == bSrc->bbNext))
            {
                bSrc->bbJumpKind = BBJ_NONE;
                bSrc->bbFlags &= ~BBF_NEEDS_GCPOLL;
#ifdef DEBUG
                if  (verbose)
                {
                    printf("Changed an unconditional jump from BB%02u to the next block BB%02u into a BBJ_NONE block\n",
                           bSrc->bbNum, bSrc->bbNext->bbNum);
                }
#endif // DEBUG
            }
        }
    }

    return jmpBlk;
}

/*****************************************************************************
 Walk the flow graph, reassign block numbers to keep them in ascending order.
 Returns 'true' if any renumbering was actually done, OR if we change the
 maximum number of assigned basic blocks (this can happen if we do inlining,
 create a new, high-numbered block, then that block goes away. We go to
 renumber the blocks, none of them actually change number, but we shrink the
 maximum assigned block number. This affects the block set epoch).
*/

bool            Compiler::fgRenumberBlocks()
{
    // If we renumber the blocks the dominator information will be out-of-date
    if (fgDomsComputed)
    {
        noway_assert(!"Can't call Compiler::fgRenumberBlocks() when fgDomsComputed==true");
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** Before renumbering the basic blocks\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG

    bool         renumbered = false;
    bool         newMaxBBNum = false;
    BasicBlock*  block;

    unsigned numStart = 1 + (compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : 0);
    unsigned num;

    for (block = fgFirstBB    , num = numStart;
         block != NULL;
         block = block->bbNext, num++)
    {
        noway_assert((block->bbFlags & BBF_REMOVED) == 0);

        if (block->bbNum != num)
        {
            renumbered = true;
#ifdef DEBUG
            if (verbose)
            {
                printf("Renumber BB%02u to BB%02u\n", block->bbNum, num);
            }
#endif // DEBUG
            block->bbNum = num;
        }

        if (block->bbNext == NULL)
        {
            fgLastBB  = block;
            fgBBcount = num - numStart + 1;
            if (compIsForInlining())
            {
                if (impInlineInfo->InlinerCompiler->fgBBNumMax != num)
                {
                    impInlineInfo->InlinerCompiler->fgBBNumMax = num;
                    newMaxBBNum = true;
                }
            }
            else
            {
                if (fgBBNumMax != num)
                {
                    fgBBNumMax = num;
                    newMaxBBNum = true;
                }
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After renumbering the basic blocks\n");
        if (renumbered)
        {
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }
        else
        {
            printf("=============== No blocks renumbered!\n");
        }
    }
#endif // DEBUG

    // Now update the BlockSet epoch, which depends on the block numbers.
    // If any blocks have been renumbered then create a new BlockSet epoch.
    // Even if we have not renumbered any blocks, we might still need to force
    // a new BlockSet epoch, for one of several reasons. If there are any new
    // blocks with higher numbers than the former maximum numbered block, then we
    // need a new epoch with a new size matching the new largest numbered block.
    // Also, if the number of blocks is different from the last time we set the
    // BlockSet epoch, then we need a new epoch. This wouldn't happen if we
    // renumbered blocks after every block addition/deletion, but it might be
    // the case that we can change the number of blocks, then set the BlockSet
    // epoch without renumbering, then change the number of blocks again, then
    // renumber.
    if (renumbered || newMaxBBNum)
    {
        NewBasicBlockEpoch();

        // The key in the unique switch successor map is dependent on the block number, so invalidate that cache.
        InvalidateUniqueSwitchSuccMap();
    }
    else
    {
        EnsureBasicBlockEpoch();
    }

    // Tell our caller if any blocks actually were renumbered.
    return renumbered || newMaxBBNum;
}

/*****************************************************************************
 *
 *  Is the BasicBlock bJump a forward branch?
 *   Optionally bSrc can be supplied to indicate that
 *   bJump must be forward with respect to bSrc
 */
bool                Compiler::fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bSrc /* = NULL */)
{
    bool result = false;

    if ((bJump->bbJumpKind == BBJ_COND)   ||
        (bJump->bbJumpKind == BBJ_ALWAYS)   )
    {
        BasicBlock* bDest = bJump->bbJumpDest;
        BasicBlock* bTemp = (bSrc == NULL) ? bJump : bSrc;

        while (true)
        {
            bTemp = bTemp->bbNext;

            if (bTemp == NULL)
                break;

            if (bTemp == bDest)
            {
                result = true;
                break;
            }
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Function called to expand the set of rarely run blocks
 */

bool                Compiler::fgExpandRarelyRunBlocks()
{
    bool result = false;

#ifdef DEBUG
    if  (verbose)
        printf("\n*************** In fgExpandRarelyRunBlocks()\n");

    const char* reason = NULL;
#endif

    // We expand the number of rarely run blocks by observing
    // that a block that falls into or jumps to a rarely run block,
    // must itself be rarely run and when we have a conditional
    // jump in which both branches go to rarely run blocks then
    // the block must itself be rarely run

    BasicBlock*   block;
    BasicBlock*   bPrev;

    for (bPrev = fgFirstBB, block = bPrev->bbNext;
                            block != NULL;
         bPrev = block,     block = block->bbNext)
    {
        if (bPrev->isRunRarely())
            continue;

        /* bPrev is known to be a normal block here */
        switch (bPrev->bbJumpKind)
        {
        case BBJ_ALWAYS:

            /* Is the jump target rarely run? */
            if (bPrev->bbJumpDest->isRunRarely())
            {
                INDEBUG(reason = "Unconditional jump to a rarely run block";)
                goto NEW_RARELY_RUN;
            }
            break;

        case BBJ_CALLFINALLY:

            // Check for a BBJ_CALLFINALLY followed by a rarely run paired BBJ_ALWAYS
            //
            if (bPrev->isBBCallAlwaysPair())
            {
                /* Is the next block rarely run? */
                if (block->isRunRarely())
                {
                    INDEBUG(reason = "Call of finally followed by a rarely run block";)
                    goto NEW_RARELY_RUN;
                }
            }
            break;

        case BBJ_NONE:

            /* is fall through target rarely run? */
            if (block->isRunRarely())
            {
                INDEBUG(reason = "Falling into a rarely run block";)
                goto NEW_RARELY_RUN;
            }
            break;

        case BBJ_COND:

            if (!block->isRunRarely())
                continue;

            /* If both targets of the BBJ_COND are run rarely then don't reorder */
            if (bPrev->bbJumpDest->isRunRarely())
            {
                /* bPrev should also be marked as run rarely */
                if (!bPrev->isRunRarely())
                {
                    INDEBUG(reason = "Both sides of a conditional jump are rarely run";)

NEW_RARELY_RUN:
                    /* If the weight of the block was obtained from a profile run,
                       than it's more accurate than our static analysis */
                    if (bPrev->bbFlags & BBF_PROF_WEIGHT)
                    {
                        continue;
                    }
                    result = true;

#ifdef DEBUG
                    assert(reason != NULL);
                    if  (verbose)
                    {
                        printf("%s, marking BB%02u as rarely run\n", reason, bPrev->bbNum);
                    }
#endif // DEBUG

                    /* Must not have previously been marked */
                    noway_assert(!bPrev->isRunRarely());

                    /* Mark bPrev as a new rarely run block */
                    bPrev->bbSetRunRarely();

                    BasicBlock* bPrevPrev = NULL;
                    BasicBlock* tmpbb;

                    if ((bPrev->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0)
                    {
                        // If we've got a BBJ_CALLFINALLY/BBJ_ALWAYS pair, treat the BBJ_CALLFINALLY as an additional predecessor for the BBJ_ALWAYS block
                        tmpbb = bPrev->bbPrev;
                        noway_assert(tmpbb != NULL);
#if FEATURE_EH_FUNCLETS
                        noway_assert(tmpbb->isBBCallAlwaysPair());
                        bPrevPrev = tmpbb;
#else
                        if (tmpbb->bbJumpKind == BBJ_CALLFINALLY)
                        {
                            bPrevPrev = tmpbb;
                        }
#endif
                    }

                    /* Now go back to it's earliest predecessor to see */
                    /* if it too should now be marked as rarely run    */
                    flowList* pred = bPrev->bbPreds;

                    if ((pred != NULL) || (bPrevPrev != NULL))
                    {
                        // bPrevPrev will be set to the lexically
                        // earliest predecessor of bPrev.

                        while (pred != NULL)
                        {
                            if (bPrevPrev == NULL)
                            {
                                // Initially we select the first block in the bbPreds list
                                bPrevPrev = pred->flBlock;
                                continue;
                            }

                            // Walk the flow graph lexically forward from pred->flBlock
                            // if we find (block == bPrevPrev) then
                            // pred->flBlock is an earlier predecessor.
                            for (tmpbb  = pred->flBlock;
                                 tmpbb != NULL;
                                 tmpbb  = tmpbb->bbNext)
                            {
                                if (tmpbb == bPrevPrev)
                                {
                                    /* We found an ealier predecessor */
                                    bPrevPrev = pred->flBlock;
                                    break;
                                }
                                else if (tmpbb == bPrev)
                                {
                                    // We have reached bPrev so stop walking
                                    // as this cannot be an earlier predecessor
                                    break;
                                }
                            }

                            // Onto the next predecessor
                            pred = pred->flNext;
                        }

                        // Walk the flow graph forward from bPrevPrev
                        // if we don't find (tmpbb == bPrev) then our candidate
                        // bPrevPrev is lexically after bPrev and we do not
                        // want to select it as our new block

                        for (tmpbb  = bPrevPrev;
                             tmpbb != NULL;
                             tmpbb  = tmpbb->bbNext)
                        {
                            if (tmpbb == bPrev)
                            {
                                // Set up block back to the lexically
                                // earliest predecessor of pPrev

                                block = bPrevPrev;
                            }
                        }
                    }
                }
                break;

            default:
                break;
            }
        }
    }

    // Now iterate over every block to see if we can prove that a block is rarely run
    // (i.e. when all predecessors to the block are rarely run)
    //
    for (bPrev = fgFirstBB, block = bPrev->bbNext;
                            block != NULL;
         bPrev = block,     block = block->bbNext)
    {
        // If block is not run rarely, then check to make sure that it has
        // at least one non-rarely run block.

        if (!block->isRunRarely())
        {
            bool rare = true;

            /* Make sure that block has at least one normal predecessor */
            for (flowList* pred  = block->bbPreds;
                           pred != NULL;
                           pred  = pred->flNext)
            {
                /* Find the fall through predecessor, if any */
                if (!pred->flBlock->isRunRarely())
                {
                    rare = false;
                    break;
                }
            }

            if (rare)
            {
                // If 'block' is the start of a handler or filter then we cannot make it
                // rarely run because we may have an exceptional edge that
                // branches here.
                //
                if (bbIsHandlerBeg(block))
                {
                    rare = false;
                }
            }

            if (rare)
            {
                block->bbSetRunRarely();
                result = true;

#ifdef DEBUG
                if  (verbose)
                {
                    printf("All branches to BB%02u are from rarely run blocks, marking as rarely run\n",
                           block->bbNum);
                }
#endif // DEBUG

                // When marking a BBJ_CALLFINALLY as rarely run we also mark
                // the BBJ_ALWAYS that comes after it as rarely run
                //
                if (block->isBBCallAlwaysPair())
                {
                    BasicBlock* bNext = block->bbNext;
                    PREFIX_ASSUME(bNext != NULL);
                    bNext->bbSetRunRarely();
#ifdef DEBUG
                    if  (verbose)
                    {
                        printf("Also marking the BBJ_ALWAYS at BB%02u as rarely run\n",
                               bNext->bbNum);
                    }
#endif // DEBUG
                }
            }
        }

        /* COMPACT blocks if possible */
        if (bPrev->bbJumpKind == BBJ_NONE)
        {
             if (fgCanCompactBlocks(bPrev, block))
             {
                 fgCompactBlocks(bPrev, block);
                 
                 block = bPrev;
                 continue;
             }
        }
        //
        // if bPrev->bbWeight is not based upon profile data we can adjust
        // the weights of bPrev and block
        //
        else if ( bPrev->isBBCallAlwaysPair()         &&      // we must have a BBJ_CALLFINALLY and BBK_ALWAYS pair
                 (bPrev->bbWeight != block->bbWeight) &&      // the weights are currently different
                 ((bPrev->bbFlags & BBF_PROF_WEIGHT) == 0))   // and the BBJ_CALLFINALLY block is not using profiled weights
        {
            if (block->isRunRarely())
            {
                bPrev->bbWeight  = block->bbWeight;  // the BBJ_CALLFINALLY block now has the same weight as the BBJ_ALWAYS block
                bPrev->bbFlags  |= BBF_RUN_RARELY;   // and is now rarely run
#ifdef DEBUG
                if  (verbose)
                {
                    printf("Marking the BBJ_CALLFINALLY block at BB%02u as rarely run because BB%02u is rarely run\n",
                           bPrev->bbNum, block->bbNum);
                }
#endif // DEBUG
            }
            else if (bPrev->isRunRarely())
            {
                block->bbWeight  = bPrev->bbWeight;  // the BBJ_ALWAYS block now has the same weight as the BBJ_CALLFINALLY block
                block->bbFlags  |= BBF_RUN_RARELY;   // and is now rarely run
#ifdef DEBUG
                if  (verbose)
                {
                    printf("Marking the BBJ_ALWAYS block at BB%02u as rarely run because BB%02u is rarely run\n",
                           block->bbNum, bPrev->bbNum);
                }
#endif // DEBUG
            }
            else  // Both blocks are hot, bPrev is known not to be using profiled weight
            {
                bPrev->bbWeight  = block->bbWeight;  // the BBJ_CALLFINALLY block now has the same weight as the BBJ_ALWAYS block
            }
            noway_assert(block->bbWeight == bPrev->bbWeight);
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Returns true if it is allowable (based upon the EH regions)
 *  to place block bAfter immediately after bBefore. It is allowable
 *  if the 'bBefore' and 'bAfter' blocks are in the exact same EH region.
 */

bool                Compiler::fgEhAllowsMoveBlock(BasicBlock* bBefore,
                                                  BasicBlock* bAfter)
{
    return BasicBlock::sameEHRegion(bBefore, bAfter);
}

/*****************************************************************************
 *
 *  Function called to move the range of blocks [bStart .. bEnd].
 *  The blocks are placed immediately after the insertAfterBlk.
 *  fgFirstFuncletBB is not updated; that is the responsibility of the caller, if necessary.
 */

void Compiler::fgMoveBlocksAfter(BasicBlock* bStart, BasicBlock* bEnd, BasicBlock* insertAfterBlk)
{
    /* We have decided to insert the block(s) after 'insertAfterBlk' */

#ifdef DEBUG
    if  (verbose)
    {
        printf("Relocated block%s [BB%02u..BB%02u] inserted after BB%02u%s\n",
               (bStart == bEnd) ? "" : "s",
               bStart->bbNum, bEnd->bbNum,
               insertAfterBlk->bbNum,
               (insertAfterBlk->bbNext == NULL) ? " at the end of method" : "");
    }
#endif // DEBUG

    /* relink [bStart .. bEnd] into the flow graph */

    bEnd->bbNext           = insertAfterBlk->bbNext;
    if (insertAfterBlk->bbNext)
        insertAfterBlk->bbNext->bbPrev = bEnd;
    insertAfterBlk->setNext(bStart);

    /* If insertAfterBlk was fgLastBB then update fgLastBB */
    if (insertAfterBlk == fgLastBB)
    {
        fgLastBB = bEnd;
        noway_assert(fgLastBB->bbNext == NULL);
    }
}

/*****************************************************************************
 *
 *  Function called to relocate a single range to the end of the method.
 *  Only an entire consecutive region can be moved and it will be kept together.
 *  Except for the first block, the range cannot have any blocks that jump into or out of the region.
 *  When successful we return the bLast block which is the last block that we relocated.
 *  When unsuccessful we return NULL.

    =============================================================
    NOTE: This function can invalidate all pointers into the EH table, as well as change the size of the EH table!
    =============================================================
 */

BasicBlock*   Compiler::fgRelocateEHRange(unsigned regionIndex, FG_RELOCATE_TYPE relocateType)
{
    INDEBUG(const char* reason = "None";)

    unsigned     XTnum;
    EHblkDsc*    HBtab;
    BasicBlock*  bStart  = NULL;
    BasicBlock*  bMiddle = NULL;
    BasicBlock*  bLast   = NULL;
    BasicBlock*  bPrev = NULL;

    // Figure out the range of blocks we're going to move

#if FEATURE_EH_FUNCLETS
    // We don't support moving try regions... yet?
    noway_assert(relocateType == FG_RELOCATE_HANDLER);
#endif // FEATURE_EH_FUNCLETS

    HBtab = ehGetDsc(regionIndex);

    if (relocateType == FG_RELOCATE_TRY)
    {
        bStart = HBtab->ebdTryBeg;
        bLast =  HBtab->ebdTryLast;
    }
    else if (relocateType == FG_RELOCATE_HANDLER)
    {
        if (HBtab->HasFilter())
        {
            // The filter and handler funclets must be moved together, and remain contiguous.
            bStart  = HBtab->ebdFilter;
            bMiddle = HBtab->ebdHndBeg;
            bLast   = HBtab->ebdHndLast;
        }
        else
        {
            bStart  = HBtab->ebdHndBeg;
            bLast   = HBtab->ebdHndLast;
        }
    }

    // Our range must contain either all rarely run blocks or all non-rarely run blocks
    bool inTheRange = false;
    bool validRange = false;

    BasicBlock*   block;

    noway_assert(bStart != NULL && bLast != NULL);
    if (bStart == fgFirstBB)
    {
        INDEBUG(reason = "can not relocate first block";)
        goto FAILURE;
    }

#if !FEATURE_EH_FUNCLETS
    // In the funclets case, we still need to set some information on the handler blocks
    if (bLast->bbNext == NULL)
    {
        INDEBUG(reason = "region is already at the end of the method";)
        goto FAILURE;
    }
#endif // !FEATURE_EH_FUNCLETS

    // Walk the block list for this purpose:
    // 1. Verify that all the blocks in the range are either all rarely run or not rarely run.
    // When creating funclets, we ignore the run rarely flag, as we need to be able to move any blocks
    // in the range.

#if !FEATURE_EH_FUNCLETS
    bool isRare;
    isRare = bStart->isRunRarely();
#endif // !FEATURE_EH_FUNCLETS
    block = fgFirstBB;
    while (true)
    {
        if (block == bStart)
        {
            noway_assert(inTheRange == false);
            inTheRange = true;
        }
        else if (block == bLast->bbNext)
        {
            noway_assert(inTheRange == true);
            inTheRange = false;
            break; // we found the end, so we're done
        }

        if (inTheRange)
        {
#if !FEATURE_EH_FUNCLETS
            // Unless all blocks are (not) run rarely we must return false.
            if (isRare != block->isRunRarely())
            {
                INDEBUG(reason = "this region contains both rarely run and non-rarely run blocks";)
                goto FAILURE;
            }
#endif // !FEATURE_EH_FUNCLETS

            validRange = true;
        }

        if (block == NULL)
            break;

        block = block->bbNext;
    }
    // Ensure that bStart .. bLast defined a valid range
    noway_assert((validRange == true) && (inTheRange == false));

    bPrev = bStart->bbPrev;
    noway_assert(bPrev != nullptr);             // Can't move a range that includes the first block of the function.

    JITDUMP("Relocating %s range BB%02u..BB%02u (EH#%u) to end of BBlist\n",
        (relocateType == FG_RELOCATE_TRY) ? "try" : "handler",
        bStart->bbNum,
        bLast->bbNum,
        regionIndex);

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }

    if (!FEATURE_EH_FUNCLETS)
    {
        // This is really expensive, and quickly becomes O(n^n) with funclets
        // so only do it once after we've created them (see fgCreateFunclets)
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
    }
#endif // DEBUG

#if FEATURE_EH_FUNCLETS

    bStart->bbFlags |= BBF_FUNCLET_BEG; // Mark the start block of the funclet

    if (bMiddle != NULL)
    {
        bMiddle->bbFlags |= BBF_FUNCLET_BEG; // Also mark the start block of a filter handler as a funclet
    }

#endif // FEATURE_EH_FUNCLETS

    BasicBlock* bNext;
    bNext = bLast->bbNext;

    /* Temporarily unlink [bStart .. bLast] from the flow graph */
    fgUnlinkRange(bStart, bLast);

    BasicBlock* insertAfterBlk;
    insertAfterBlk = fgLastBB;

#if FEATURE_EH_FUNCLETS

    // There are several cases we need to consider when moving an EH range.
    // If moving a range X, we must consider its relationship to every other EH
    // range A in the table. Note that each entry in the table represents both
    // a protected region and a handler region (possibly including a filter region
    // that must live before and adjacent to the handler region), so we must
    // consider try and handler regions independently. These are the cases:
    // 1. A is completely contained within X (where "completely contained" means
    //    that the 'begin' and 'last' parts of A are strictly between the 'begin'
    //    and 'end' parts of X, and aren't equal to either, for example, they don't
    //    share 'last' blocks). In this case, when we move X, A moves with it, and
    //    the EH table doesn't need to change.
    // 2. X is completely contained within A. In this case, X gets extracted from A,
    //    and the range of A shrinks, but because A is strictly within X, the EH
    //    table doesn't need to change.
    // 3. A and X have exactly the same range. In this case, A is moving with X and
    //    the EH table doesn't need to change.
    // 4. A and X share the 'last' block. There are two sub-cases:
    //    (a) A is a larger range than X (such that the beginning of A precedes the
    //        beginning of X): in this case, we are moving the tail of A. We set the
    //        'last' block of A to the the block preceding the beginning block of X.
    //    (b) A is a smaller range than X. Thus, we are moving the entirety of A along
    //        with X. In this case, nothing in the EH record for A needs to change.
    // 5. A and X share the 'beginning' block (but aren't the same range, as in #3).
    //    This can never happen here, because we are only moving handler ranges (we don't
    //    move try ranges), and handler regions cannot start at the beginning of a try
    //    range or handler range and be a subset.
    //
    // Note that A and X must properly nest for the table to be well-formed. For example,
    // the beginning of A can't be strictly within the range of X (that is, the beginning
    // of A isn't shared with the beginning of X) and the end of A outside the range.

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        if (XTnum != regionIndex) // we don't need to update our 'last' pointer
        {
            if (HBtab->ebdTryLast == bLast)
            {
                // If we moved a set of blocks that were at the end of
                // a different try region then we may need to update ebdTryLast
                for (block = HBtab->ebdTryBeg; block != NULL; block = block->bbNext)
                {
                    if (block == bPrev)
                    {
                        // We were contained within it, so shrink its region by
                        // setting its 'last'
                        fgSetTryEnd(HBtab, bPrev);
                        break;
                    }
                    else if (block == HBtab->ebdTryLast->bbNext)
                    {
                        // bPrev does not come after the TryBeg, thus we are larger, and
                        // it is moving with us.
                        break;
                    }
                }
            }
            if (HBtab->ebdHndLast == bLast)
            {
                // If we moved a set of blocks that were at the end of
                // a different handler region then we must update ebdHndLast
                for (block = HBtab->ebdHndBeg; block != NULL; block = block->bbNext)
                {
                    if (block == bPrev)
                    {
                        fgSetHndEnd(HBtab, bPrev);
                        break;
                    }
                    else if (block == HBtab->ebdHndLast->bbNext)
                    {
                        // bPrev does not come after the HndBeg
                        break;
                    }
                }
            }
        }
    } // end exception table iteration

    // Insert the block(s) we are moving after fgLastBlock
    fgMoveBlocksAfter(bStart, bLast, insertAfterBlk);

    if (fgFirstFuncletBB == nullptr)      // The funclet region isn't set yet
    {
        fgFirstFuncletBB = bStart;
    }
    else
    {
        assert(fgFirstFuncletBB != insertAfterBlk->bbNext); // We insert at the end, not at the beginning, of the funclet region.
    }

    // These asserts assume we aren't moving try regions (which we might need to do). Only
    // try regions can have fall through into or out of the region.

    noway_assert(!bPrev->bbFallsThrough()); // There can be no fall through into a filter or handler region
    noway_assert(!bLast->bbFallsThrough()); // There can be no fall through out of a handler region

#ifdef DEBUG
    if (verbose)
    {
        printf("Create funclets: moved region\n");
        fgDispHandlerTab();
    }

    // We have to wait to do this until we've created all the additional regions
    // Because this relies on ebdEnclosingTryIndex and ebdEnclosingHndIndex
    if (!FEATURE_EH_FUNCLETS)
    {
        // This is really expensive, and quickly becomes O(n^n) with funclets
        // so only do it once after we've created them (see fgCreateFunclets)
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
    }
#endif // DEBUG

#else // FEATURE_EH_FUNCLETS

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        if (XTnum == regionIndex)
        {
            // Don't update our handler's Last info
            continue;
        }

        if (HBtab->ebdTryLast == bLast)
        {
            // If we moved a set of blocks that were at the end of
            // a different try region then we may need to update ebdTryLast
            for (block = HBtab->ebdTryBeg; block != NULL; block = block->bbNext)
            {
                if (block == bPrev)
                {
                    fgSetTryEnd(HBtab, bPrev);
                    break;
                }
                else if (block == HBtab->ebdTryLast->bbNext)
                {
                    // bPrev does not come after the TryBeg
                    break;
                }
            }
        }
        if (HBtab->ebdHndLast == bLast)
        {
            // If we moved a set of blocks that were at the end of
            // a different handler region then we must update ebdHndLast
            for (block = HBtab->ebdHndBeg; block != NULL; block = block->bbNext)
            {
                if (block == bPrev)
                {
                    fgSetHndEnd(HBtab, bPrev);
                    break;
                }
                else if (block == HBtab->ebdHndLast->bbNext)
                {
                    // bPrev does not come after the HndBeg
                    break;
                }
            }
        }
    } // end exception table iteration

    // We have decided to insert the block(s) after fgLastBlock
    fgMoveBlocksAfter(bStart, bLast, insertAfterBlk);

    // If bPrev falls through, we will insert a jump to block
    fgConnectFallThrough(bPrev, bStart);

    // If bLast falls through, we will insert a jump to bNext
    fgConnectFallThrough(bLast, bNext);

#endif // FEATURE_EH_FUNCLETS

    goto DONE;

FAILURE:

#ifdef DEBUG
    if  (verbose)
    {
        printf("*************** Failed fgRelocateEHRange(BB%02u..BB%02u) because %s\n", bStart->bbNum, bLast->bbNum, reason);
    }
#endif // DEBUG

    bLast = NULL;

DONE:

    return bLast;
}


#if FEATURE_EH_FUNCLETS

#if defined(_TARGET_ARM_)

/*****************************************************************************
 * We just removed a BBJ_CALLFINALLY/BBJ_ALWAYS pair. If this was the only such pair
 * targeting the BBJ_ALWAYS target, then we need to clear the BBF_FINALLY_TARGET bit
 * so that target can also be removed. 'block' is the finally target. Since we just
 * removed the BBJ_ALWAYS, it better have the BBF_FINALLY_TARGET bit set.
 */

void                Compiler::fgClearFinallyTargetBit(BasicBlock* block)
{
    assert((block->bbFlags & BBF_FINALLY_TARGET) != 0);

    for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
    {
        if (pred->flBlock->bbJumpKind == BBJ_ALWAYS &&
            pred->flBlock->bbJumpDest == block)
        {
            BasicBlock* pPrev = pred->flBlock->bbPrev;
            if (pPrev != NULL)
            {
                if (pPrev->bbJumpKind == BBJ_CALLFINALLY)
                {
                    // We found a BBJ_CALLFINALLY / BBJ_ALWAYS that still points to this finally target
                    return;
                }
            }
        }
    }

    // Didn't find any BBJ_CALLFINALLY / BBJ_ALWAYS that still points here, so clear the bit

    block->bbFlags &= ~BBF_FINALLY_TARGET;
}

#endif // defined(_TARGET_ARM_)

/*****************************************************************************
 * Is this an intra-handler control flow edge?
 *
 * 'block' is the head block of a funclet/handler region, or .
 * 'predBlock' is a predecessor block of 'block' in the predecessor list.
 *
 * 'predBlock' can legally only be one of three things:
 * 1. in the same handler region (e.g., the source of a back-edge of a loop from
 *    'predBlock' to 'block'), including in nested regions within the handler,
 * 2. if 'block' begins a handler that is a filter-handler, 'predBlock' must be in the 'filter' region,
 * 3. for other handlers, 'predBlock' must be in the 'try' region corresponding to handler (or any
 *    region nested in the 'try' region).
 *
 * Note that on AMD64/ARM64, the BBJ_CALLFINALLY block that calls a finally handler is not
 * within the corresponding 'try' region: it is placed in the corresponding 'try' region's
 * parent (which might be the main function body). This is how it is represented to the VM
 * (with a special "cloned finally" EH table entry).
 *
 * Return 'true' for case #1, and 'false' otherwise.
 */
bool                Compiler::fgIsIntraHandlerPred(BasicBlock* predBlock, BasicBlock* block)
{
    // Some simple preconditions (as stated above)
    assert(!fgFuncletsCreated);
    assert(fgGetPredForBlock(block, predBlock) != NULL);
    assert(block->hasHndIndex());

    EHblkDsc* xtab = ehGetDsc(block->getHndIndex());

#if FEATURE_EH_CALLFINALLY_THUNKS
    if (xtab->HasFinallyHandler())
    {
        assert((xtab->ebdHndBeg == block) ||  // The normal case
               ((xtab->ebdHndBeg->bbNext == block) && (xtab->ebdHndBeg->bbFlags & BBF_INTERNAL))); // After we've already inserted a header block, and we're trying to decide how to split up the predecessor edges.
        if (predBlock->bbJumpKind == BBJ_CALLFINALLY)
        {
            assert(predBlock->bbJumpDest == block);

            // A BBJ_CALLFINALLY predecessor of the handler can only come from the corresponding try,
            // not from any EH clauses nested in this handler. However, we represent the BBJ_CALLFINALLY
            // as being in the 'try' region's parent EH region, which might be the main function body.

            unsigned tryIndex = xtab->ebdEnclosingTryIndex;
            if (tryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                assert(!predBlock->hasTryIndex());
            }
            else
            {
                assert(predBlock->hasTryIndex());
                assert(tryIndex == predBlock->getTryIndex());
                assert(ehGetDsc(tryIndex)->InTryRegionBBRange(predBlock));
            }
            return false;
        }
    }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

    assert(predBlock->hasHndIndex() || predBlock->hasTryIndex());

    //   We could search the try region looking for predBlock by using bbInTryRegions
    // but that does a lexical search for the block, and then assumes funclets
    // have been created and does a lexical search of all funclets that were pulled
    // out of the parent try region.
    //   First, funclets haven't been created yet, and even if they had been, we shouldn't
    // have any funclet directly branching to another funclet (they have to return first).
    // So we can safely use CheckIsTryRegion instead of bbInTryRegions.
    //   Second, I believe the depth of any EH graph will on average be smaller than the
    // breadth of the blocks within a try body. Thus it is faster to get our answer by
    // looping outward over the region graph. However, I have added asserts, as a
    // precaution, to ensure both algorithms agree. The asserts also check that the only
    // way to reach the head of a funclet is from the corresponding try body or from
    // within the funclet (and *not* any nested funclets).

    if (predBlock->hasTryIndex())
    {
        // Because the EH clauses are listed inside-out, any nested trys will be at a
        // lower index than the current try and if there's no enclosing try, tryIndex
        // will terminate at NO_ENCLOSING_INDEX

        unsigned tryIndex = predBlock->getTryIndex();
        while (tryIndex < block->getHndIndex())
        {
            tryIndex = ehGetEnclosingTryIndex(tryIndex);
        }
        // tryIndex should enclose predBlock
        assert((tryIndex == EHblkDsc::NO_ENCLOSING_INDEX) || ehGetDsc(tryIndex)->InTryRegionBBRange(predBlock));

        // At this point tryIndex is either block's handler's corresponding try body
        // or some outer try region that contains both predBlock & block or
        // NO_ENCLOSING_REGION (because there was no try body that encloses both).
        if (tryIndex == block->getHndIndex())
        {
            assert(xtab->InTryRegionBBRange(predBlock));
            assert(!xtab->InHndRegionBBRange(predBlock));
            return false;
        }
        // tryIndex should enclose block (and predBlock as previously asserted)
        assert((tryIndex == EHblkDsc::NO_ENCLOSING_INDEX) || ehGetDsc(tryIndex)->InTryRegionBBRange(block));
    }
    if (xtab->HasFilter())
    {
        // The block is a handler. Check if the pred block is from its filter. We only need to
        // check the end filter flag, as there is only a single filter for any handler, and we
        // already know predBlock is a predecessor of block.
        if (predBlock->bbJumpKind == BBJ_EHFILTERRET)
        {
            assert(!xtab->InHndRegionBBRange(predBlock));
            return false;
        }
    }
    // It is not in our try region (or filter), so it must be within this handler (or try bodies
    // within this handler)
    assert(!xtab->InTryRegionBBRange(predBlock));
    assert(xtab->InHndRegionBBRange(predBlock));
    return true;
}


/*****************************************************************************
 * Does this block, first block of a handler region, have any predecessor edges
 * that are not from its corresponding try region?
 */

bool                Compiler::fgAnyIntraHandlerPreds(BasicBlock* block)
{
    assert(block->hasHndIndex());
    assert(fgFirstBlockOfHandler(block) == block); // this block is the first block of a handler

    flowList* pred;

    for (pred = block->bbPreds; pred; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->flBlock;

        if (fgIsIntraHandlerPred(predBlock, block))
        {
            // We have a predecessor that is not from our try region
            return true;
        }
    }

    return false;
}


/*****************************************************************************
 * Introduce a new head block of the handler for the prolog to be put in, ahead
 * of the current handler head 'block'.
 * Note that this code has some similarities to fgCreateLoopPreHeader().
 */

void                Compiler::fgInsertFuncletPrologBlock(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("\nCreating funclet prolog header for BB%02u\n", block->bbNum);
#endif

    assert(block->hasHndIndex());
    assert(fgFirstBlockOfHandler(block) == block); // this block is the first block of a handler

    /* Allocate a new basic block */

    BasicBlock* newHead = bbNewBasicBlock(BBJ_NONE);

    // In fgComputePreds() we set the BBF_JMP_TARGET and BBF_HAS_LABEL for all of the handler entry points
    //
    newHead->bbFlags |= (BBF_INTERNAL | BBF_JMP_TARGET | BBF_HAS_LABEL);
    newHead->inheritWeight(block);
    newHead->bbRefs      = 0;

    fgInsertBBbefore(block, newHead); // insert the new block in the block list
    fgExtendEHRegionBefore(block);    // Update the EH table to make the prolog block the first block in the block's EH block.

    // fgExtendEHRegionBefore mucks with the bbRefs without updating the pred list, which we will
    // do below for this block. So, undo that change.
    assert(newHead->bbRefs > 0);
    newHead->bbRefs--;
    block->bbRefs++;

    // Distribute the pred list between newHead and block. Incoming edges coming from outside
    // the handler go to the prolog. Edges coming from with the handler are back-edges, and
    // go to the existing 'block'.

    for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->flBlock;
        if (!fgIsIntraHandlerPred(predBlock, block))
        {
            // It's a jump from outside the handler; add it to the newHead preds list and remove
            // it from the block preds list.

            switch (predBlock->bbJumpKind)
            {
            case BBJ_CALLFINALLY:
                noway_assert(predBlock->bbJumpDest == block);
                predBlock->bbJumpDest = newHead;
                fgRemoveRefPred(block, predBlock);
                fgAddRefPred(newHead, predBlock);
                break;

            default:
                // The only way into the handler is via a BBJ_CALLFINALLY (to a finally handler), or
                // via exception handling.
                noway_assert(false);
                break;
            }
        }
    }

    assert(NULL == fgGetPredForBlock(block, newHead));
    fgAddRefPred(block, newHead);

    assert((newHead->bbFlags & (BBF_INTERNAL|BBF_JMP_TARGET|BBF_HAS_LABEL)) == (BBF_INTERNAL|BBF_JMP_TARGET|BBF_HAS_LABEL));
}


/*****************************************************************************
 *
 * Every funclet will have a prolog. That prolog will be inserted as the first instructions
 * in the first block of the funclet. If the prolog is also the head block of a loop, we
 * would end up with the prolog instructions being executed more than once.
 * Check for this by searching the predecessor list for loops, and create a new prolog header
 * block when needed. We detect a loop by looking for any predecessor that isn't in the
 * handler's try region, since the only way to get into a handler is via that try region.
 */

void                Compiler::fgCreateFuncletPrologBlocks()
{
    noway_assert(fgComputePredsDone);
    noway_assert(!fgDomsComputed); // this function doesn't maintain the dom sets
    assert(!fgFuncletsCreated);

    bool          prologBlocksCreated = false;
    EHblkDsc*     HBtabEnd;
    EHblkDsc*     HBtab;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        BasicBlock* head = HBtab->ebdHndBeg;

        if (fgAnyIntraHandlerPreds(head))
        {
            // We need to create a new block in which to place the prolog, and split the existing
            // head block predecessor edges into those that should point to the prolog, and those
            // that shouldn't.
            //
            // It's arguable that we should just always do this, and not only when we "need to",
            // so there aren't two different code paths. However, it's unlikely to be necessary
            // for catch handlers because they have an incoming argument (the exception object)
            // that needs to get stored or saved, so back-arcs won't normally go to the head. It's
            // possible when writing in IL to generate a legal loop (e.g., push an Exception object
            // on the stack before jumping back to the catch head), but C# probably won't. This will
            // most commonly only be needed for finallys with a do/while loop at the top of the
            // finally.
            //
            // Note that we don't check filters. This might be a bug, but filters always have a filter
            // object live on entry, so it's at least unlikely (illegal?) that a loop edge targets the
            // filter head.

            fgInsertFuncletPrologBlock(head);
            prologBlocksCreated = true;
        }
    }

    if (prologBlocksCreated)
    {
        // If we've modified the graph, reset the 'modified' flag, since the dominators haven't
        // been computed.
        fgModified = false;

#if DEBUG
        if (verbose)
        {
            JITDUMP("\nAfter fgCreateFuncletPrologBlocks()");
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }

        fgVerifyHandlerTab();
        fgDebugCheckBBlist();
#endif // DEBUG
    }
}

/*****************************************************************************
 *
 *  Function to create funclets out of all EH catch/finally/fault blocks.
 *  We only move filter and handler blocks, not try blocks.
 */

void                Compiler::fgCreateFunclets()
{
    assert(!fgFuncletsCreated);

#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgCreateFunclets()\n");
#endif

    fgCreateFuncletPrologBlocks();

    unsigned           XTnum;
    EHblkDsc*          HBtab;
    const unsigned int funcCnt = ehFuncletCount() + 1;

    if (!FitsIn<unsigned short>(funcCnt))
    {
        IMPL_LIMITATION("Too many funclets");
    }

    FuncInfoDsc*       funcInfo = new (this, CMK_BasicBlock) FuncInfoDsc[funcCnt];

    unsigned short     funcIdx;

    // Setup the root FuncInfoDsc and prepare to start associating
    // FuncInfoDsc's with their corresponding EH region
    memset((void*)funcInfo, 0, funcCnt * sizeof(FuncInfoDsc));
    assert(funcInfo[0].funKind == FUNC_ROOT);
    funcIdx = 1;

    // Because we iterate from the top to the bottom of the compHndBBtab array, we are iterating
    // from most nested (innermost) to least nested (outermost) EH region. It would be reasonable
    // to iterate in the opposite order, but the order of funclets shouldn't matter.
    //
    // We move every handler region to the end of the function: each handler will become a funclet.
    //
    // Note that fgRelocateEHRange() can add new entries to the EH table. However, they will always
    // be added *after* the current index, so our iteration here is not invalidated.
    // It *can* invalidate the compHndBBtab pointer itself, though, if it gets reallocated!

    for (XTnum = 0;
         XTnum < compHndBBtabCount;
         XTnum++)
    {
        HBtab = ehGetDsc(XTnum); // must re-compute this every loop, since fgRelocateEHRange changes the table
        if (HBtab->HasFilter())
        {
            assert(funcIdx < funcCnt);
            funcInfo[funcIdx].funKind = FUNC_FILTER;
            funcInfo[funcIdx].funEHIndex = (unsigned short)XTnum;
            funcIdx++;
        }
        assert(funcIdx < funcCnt);
        funcInfo[funcIdx].funKind = FUNC_HANDLER;
        funcInfo[funcIdx].funEHIndex = (unsigned short)XTnum;
        HBtab->ebdFuncIndex = funcIdx;
        funcIdx++;
        fgRelocateEHRange(XTnum, FG_RELOCATE_HANDLER);
    }

    // We better have populated all of them by now
    assert(funcIdx == funcCnt);

    // Publish
    compCurrFuncIdx = 0;
    compFuncInfos = funcInfo;
    compFuncInfoCount = (unsigned short)funcCnt;

    fgFuncletsCreated = true;

#if DEBUG
    if (verbose)
    {
        JITDUMP("\nAfter fgCreateFunclets()");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }

    fgVerifyHandlerTab();
    fgDebugCheckBBlist();
#endif // DEBUG
}

#else // !FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Function called to relocate any and all EH regions.
 *  Only entire consecutive EH regions will be moved and they will be kept together.
 *  Except for the first block, the range can not have any blocks that jump into or out of the region.
 */

bool                Compiler::fgRelocateEHRegions()
{
    bool result = false; // Our return value

#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgRelocateEHRegions()\n");
#endif

    if (fgCanRelocateEHRegions)
    {
        unsigned      XTnum;
        EHblkDsc*     HBtab;

        for (XTnum = 0, HBtab = compHndBBtab;
             XTnum < compHndBBtabCount;
             XTnum++  , HBtab++)
        {
            // Nested EH regions cannot be moved.
            // Also we don't want to relocate an EH region that has a filter
            if ((HBtab->ebdHandlerNestingLevel == 0) && !HBtab->HasFilter())
            {
                bool movedTry = false;
#if DEBUG
                bool movedHnd = false;
#endif // DEBUG

                // Only try to move the outermost try region
                if (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    // Move the entire try region if it can be moved
                    if (HBtab->ebdTryBeg->isRunRarely())
                    {
                        BasicBlock* bTryLastBB = fgRelocateEHRange(XTnum, FG_RELOCATE_TRY);
                        if (bTryLastBB != NULL)
                        {
                            result   = true;
                            movedTry = true;
                        }
                    }
#if DEBUG
                    if (verbose && movedTry)
                    {
                        printf("\nAfter relocating an EH try region");
                        fgDispBasicBlocks();
                        fgDispHandlerTab();

                        // Make sure that the predecessor lists are accurate
                        if (expensiveDebugCheckLevel >= 2)
                        {
                            fgDebugCheckBBlist();
                        }
                    }
#endif // DEBUG
                }

                // Currently it is not good to move the rarely run handler regions to the end of the method
                // because fgDetermineFirstColdBlock() must put the start of any handler region in the hot section.
#if 0
                // Now try to move the entire handler region if it can be moved.
                // Don't try to move a finally handler unless we already moved the try region.
                if (HBtab->ebdHndBeg->isRunRarely() &&
                    !HBtab->ebdHndBeg->hasTryIndex() &&
                    (movedTry || !HBtab->HasFinallyHandler()))
                {
                    BasicBlock* bHndLastBB = fgRelocateEHRange(XTnum, FG_RELOCATE_HANDLER);
                    if (bHndLastBB != NULL)
                    {
                        result   = true;
                        movedHnd = true;
                    }
                }
#endif // 0

#if DEBUG
                if (verbose && movedHnd)
                {
                    printf("\nAfter relocating an EH handler region");
                    fgDispBasicBlocks();
                    fgDispHandlerTab();

                    // Make sure that the predecessor lists are accurate
                    if (expensiveDebugCheckLevel >= 2)
                    {
                        fgDebugCheckBBlist();
                    }
                }
#endif // DEBUG
            }
        }
    }

#if DEBUG
    fgVerifyHandlerTab();

    if (verbose && result)
    {
        printf("\nAfter fgRelocateEHRegions()");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        // Make sure that the predecessor lists are accurate
        fgDebugCheckBBlist();
    }
#endif // DEBUG

    return result;
}

#endif // !FEATURE_EH_FUNCLETS

bool flowList::setEdgeWeightMinChecked(BasicBlock::weight_t newWeight,
                                       BasicBlock::weight_t slop,
                                       bool*                wbUsedSlop)
{
    bool result = false;
    if ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin))
    {
        flEdgeWeightMin = newWeight;
        result = true;
    }
    else if (slop > 0)
    {
        // We allow for a small amount of inaccuracy in block weight counts.
        if (flEdgeWeightMax < newWeight)
        {
            // We have already determined that this edge's weight
            // is less than newWeight, so we just allow for the slop
            if (newWeight <= (flEdgeWeightMax + slop))
            {
                result = true;

                if (flEdgeWeightMax != 0)
                {
                    // We will raise flEdgeWeightMin and Max towards newWeight
                    flEdgeWeightMin = flEdgeWeightMax;
                    flEdgeWeightMax = newWeight;
                }

                if (wbUsedSlop != NULL)
                {
                    *wbUsedSlop = true;
                }
            }
        }
        else
        {
            assert(flEdgeWeightMin > newWeight);

            // We have already determined that this edge's weight
            // is more than newWeight, so we just allow for the slop
            if ((newWeight + slop) >= flEdgeWeightMin)
            {
                result = true;

                assert(flEdgeWeightMax != 0);

                // We will lower flEdgeWeightMin towards newWeight
                flEdgeWeightMin = newWeight;

                if (wbUsedSlop != NULL)
                {
                    *wbUsedSlop = true;
                }
            }
        }

        // If we are returning true then we should have adjusted the range so that
        // the newWeight is in new range [Min..Max] or fgEdjeWeightMax is zero.
        // Also we should have set wbUsedSlop to true.
        if (result == true)
        {
            assert( (flEdgeWeightMax == 0) ||
                    ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin)));

            if (wbUsedSlop != NULL)
            {
                assert(*wbUsedSlop == true);
            }
        }
    }

#if DEBUG
    if (result == false)
    {
        result = false;        // break here
    }
#endif // DEBUG

    return result;
}

bool flowList::setEdgeWeightMaxChecked(BasicBlock::weight_t newWeight,
                                       BasicBlock::weight_t slop,
                                       bool*                wbUsedSlop)
{
    bool result = false;
    if ((newWeight >= flEdgeWeightMin) && (newWeight <= flEdgeWeightMax))
    {
        flEdgeWeightMax = newWeight;
        result = true;
    }
    else if (slop > 0)
    {
        // We allow for a small amount of inaccuracy in block weight counts.
        if (flEdgeWeightMax < newWeight)
        {
            // We have already determined that this edge's weight
            // is less than newWeight, so we just allow for the slop
            if (newWeight <= (flEdgeWeightMax + slop))
            {
                result = true;

                if (flEdgeWeightMax != 0)
                {
                    // We will allow this to raise flEdgeWeightMax towards newWeight
                    flEdgeWeightMax = newWeight;
                }

                if (wbUsedSlop != NULL)
                {
                    *wbUsedSlop = true;
                }
            }
        }
        else
        {
            assert(flEdgeWeightMin > newWeight);

            // We have already determined that this edge's weight
            // is more than newWeight, so we just allow for the slop
            if ((newWeight + slop) >= flEdgeWeightMin)
            {
                result = true;

                assert(flEdgeWeightMax != 0);

                // We will allow this to lower flEdgeWeightMin and Max towards newWeight
                flEdgeWeightMax = flEdgeWeightMin;
                flEdgeWeightMin = newWeight;

                if (wbUsedSlop != NULL)
                {
                    *wbUsedSlop = true;
                }
            }
        }

        // If we are returning true then we should have adjusted the range so that
        // the newWeight is in new range [Min..Max] or fgEdjeWeightMax is zero
        // Also we should have set wbUsedSlop to true, unless it is NULL
        if (result == true)
        {
            assert( (flEdgeWeightMax == 0) ||
                    ((newWeight <= flEdgeWeightMax) && (newWeight >= flEdgeWeightMin)));

            assert((wbUsedSlop == NULL) || (*wbUsedSlop == true));
        }
    }

#if DEBUG
    if (result == false)
    {
        result = false;        // break here
    }
#endif // DEBUG

    return result;
}

#ifdef DEBUG
void                Compiler::fgPrintEdgeWeights()
{
    BasicBlock*  bSrc;
    BasicBlock*  bDst;
    flowList*    edge;

    // Print out all of the edge weights
    for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
    {
        if (bDst->bbPreds != NULL)
        {
            printf("    Edge weights into BB%02u :", bDst->bbNum);
            for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
            {
                bSrc = edge->flBlock;
                // This is the control flow edge (bSrc -> bDst)

                printf("BB%02u ", bSrc->bbNum);

                if (edge->flEdgeWeightMin < BB_MAX_WEIGHT)
                {
                    printf("(%s", refCntWtd2str(edge->flEdgeWeightMin));
                }
                else
                {
                    printf("(MAX");
                }
                if (edge->flEdgeWeightMin != edge->flEdgeWeightMax)
                {
                    if (edge->flEdgeWeightMax < BB_MAX_WEIGHT)
                    {
                        printf("..%s", refCntWtd2str(edge->flEdgeWeightMax));
                    }
                    else
                    {
                        printf("..MAX");
                    }
                }
                printf(")");
                if (edge->flNext != NULL)
                {
                    printf(", ");
                }
            }
            printf("\n");
        }
    }
}
#endif // DEBUG

// return true if there is a possibility that the method has a loop (a backedge is present)
bool Compiler::fgMightHaveLoop()
{
    // Don't use a BlockSet for this temporary bitset of blocks: we don't want to have to call EnsureBasicBlockEpoch()
    // and potentially change the block epoch.

    BitVecTraits blockVecTraits(fgBBNumMax + 1, this);
    BitVec BLOCKSET_INIT_NOCOPY(blocksSeen, BitVecOps::MakeEmpty(&blockVecTraits));

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        BitVecOps::AddElemD(&blockVecTraits, blocksSeen, block->bbNum);

        AllSuccessorIter succsEnd = block->GetAllSuccs(this).end();
        for (AllSuccessorIter succs = block->GetAllSuccs(this).begin(); succs != succsEnd; ++succs)
        {
            BasicBlock* succ = (*succs);
            if (BitVecOps::IsMember(&blockVecTraits, blocksSeen, succ->bbNum))
                return true;
        }
    }
    return false;
}


void                Compiler::fgComputeEdgeWeights()
{
#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgComputeEdgeWeights()\n");
#endif // DEBUG

    if (fgIsUsingProfileWeights() == false)
    {
#ifdef DEBUG
        if  (verbose)
        {
            printf("fgComputeEdgeWeights() we do not have any profile data so we are not using the edge weights\n");
        }
#endif // DEBUG
        fgHaveValidEdgeWeights = false;
        fgCalledWeight = BB_UNITY_WEIGHT;
    }

#if DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        printf("\n");
    }
#endif // DEBUG

    BasicBlock*  bSrc;
    BasicBlock*  bDst;
    flowList*    edge;
    unsigned     iterations               = 0;
    unsigned     goodEdgeCountCurrent     = 0;
    unsigned     goodEdgeCountPrevious    = 0;
    bool         inconsistentProfileData  = false;
    bool         hasIncompleteEdgeWeights = false;
    unsigned     numEdges                 = 0;
    bool         usedSlop                 = false;
    bool         changed;
    bool         modified;

    BasicBlock::weight_t returnWeight;
    BasicBlock::weight_t slop;

    // If we have any blocks that did not have profile derived weight
    // we will try to fix their weight up here
    //
    modified = false;
    do  // while (changed)
    {
        changed = false;
        returnWeight = 0;
        iterations++;

        for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
        {
            if (((bDst->bbFlags & BBF_PROF_WEIGHT) == 0) && (bDst->bbPreds != NULL))
            {
                BasicBlock* bOnlyNext;

                // This block does not have a profile derived weight
                //
                BasicBlock::weight_t newWeight = BB_MAX_WEIGHT;

                if (bDst->countOfInEdges() == 1)
                {
                    // Only one block flows into bDst
                    bSrc = bDst->bbPreds->flBlock;

                    // Does this block flow into only one other block
                    if (bSrc->bbJumpKind == BBJ_NONE)
                    {
                        bOnlyNext = bSrc->bbNext;
                    }
                    else if (bSrc->bbJumpKind == BBJ_ALWAYS)
                    {
                        bOnlyNext = bSrc->bbJumpDest;
                    }
                    else
                    {
                        bOnlyNext = NULL;
                    }

                    if ((bOnlyNext == bDst) && ((bSrc->bbFlags & BBF_PROF_WEIGHT) != 0))
                    {
                        // We know the exact weight of bDst
                        newWeight = bSrc->bbWeight;
                    }
                }

                // Does this block flow into only one other block
                if (bDst->bbJumpKind == BBJ_NONE)
                {
                    bOnlyNext = bDst->bbNext;
                }
                else if (bDst->bbJumpKind == BBJ_ALWAYS)
                {
                    bOnlyNext = bDst->bbJumpDest;
                }
                else
                {
                    bOnlyNext = NULL;
                }

                if ((bOnlyNext != NULL) && (bOnlyNext->bbPreds != NULL))
                {
                    // Does only one block flow into bOnlyNext
                    if (bOnlyNext->countOfInEdges() == 1)
                    {
                        noway_assert(bOnlyNext->bbPreds->flBlock == bDst);

                        // We know the exact weight of bDst
                        newWeight = bOnlyNext->bbWeight;
                    }
                }

                if ((newWeight != BB_MAX_WEIGHT) && (bDst->bbWeight != newWeight))
                {
                    changed  = true;
                    modified = true;
                    bDst->bbWeight = newWeight;
                    if (newWeight == 0)
                    {
                        bDst->bbFlags |= BBF_RUN_RARELY;
                    }
                    else
                    {
                        bDst->bbFlags &= ~BBF_RUN_RARELY;
                    }
                }
            }

            // Sum up the weights of all of the return blocks and throw blocks
            // This is used when we have a back-edge into block 1
            //
            if (((bDst->bbFlags & BBF_PROF_WEIGHT) != 0) &&
                ((bDst->bbJumpKind == BBJ_RETURN) || (bDst->bbJumpKind == BBJ_THROW)))
            {
                returnWeight += bDst->bbWeight;
            }
        }
    } 
    // Generally when we synthesize profile estimates we do it in a way where this algorithm will converge
    // but downstream opts that remove conditional branches may create a situation where this is not the case.  
    // For instance a loop that becomes unreachable creates a sort of 'ring oscillator' (See test b539509)
    while (changed && iterations < 10);

#if DEBUG
    if (verbose && modified)
    {
        printf("fgComputeEdgeWeights() adjusted the weight of some blocks\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif

    // When we are not using profile data we have already setup fgCalledWeight
    // only set it here if we are using profile data
    //
    if (fgIsUsingProfileWeights())
    {
        // If the first block has one ref then it's weight is the fgCalledWeight
        // otherwise we have backedge's into the first block so instead
        // we use the sum of the return block weights.
        // If the profile data has a 0 for the returnWeoght
        // then just use the first block weight rather than the 0
        //
        if ((fgFirstBB->countOfInEdges() == 1) || (returnWeight == 0))
        {
            fgCalledWeight = fgFirstBB->bbWeight;
        }
        else
        {
            fgCalledWeight = returnWeight;
        }
    }

    // Now we will compute the initial flEdgeWeightMin and flEdgeWeightMax values
    for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
    {
        BasicBlock::weight_t bDstWeight = bDst->bbWeight;

        // We subtract out the called count so that bDstWeight is
        // the sum of all edges that go into this block from this method.
        //
        if (bDst == fgFirstBB)
        {
            bDstWeight -= fgCalledWeight;
        }

        for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
        {
            bool assignOK = true;

            bSrc = edge->flBlock;
            // We are processing the control flow edge (bSrc -> bDst)

            numEdges++;

            //
            // If the bSrc or bDst blocks do not have exact profile weights
            // then we must reset any values that they currently have
            //

            if (((bSrc->bbFlags & BBF_PROF_WEIGHT) == 0) ||
                ((bDst->bbFlags & BBF_PROF_WEIGHT) == 0))
            {
                edge->flEdgeWeightMin = BB_ZERO_WEIGHT;
                edge->flEdgeWeightMax = BB_MAX_WEIGHT;
            }

            slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;
            switch (bSrc->bbJumpKind)
            {
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
            case BBJ_NONE:
            case BBJ_CALLFINALLY:
                // We know the exact edge weight
                assignOK &= edge->setEdgeWeightMinChecked(bSrc->bbWeight, slop, &usedSlop);
                assignOK &= edge->setEdgeWeightMaxChecked(bSrc->bbWeight, slop, &usedSlop);
                break;

            case BBJ_COND:
            case BBJ_SWITCH:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                if (edge->flEdgeWeightMax > bSrc->bbWeight)
                {
                    // The maximum edge weight to block can't be greater than the weight of bSrc
                    assignOK &= edge->setEdgeWeightMaxChecked(bSrc->bbWeight, slop, &usedSlop);
                }
                break;

            default:
                // We should never have an edge that starts from one of these jump kinds
                noway_assert(!"Unexpected bbJumpKind");
                break;
            }

            // The maximum edge weight to block can't be greater than the weight of bDst
            if (edge->flEdgeWeightMax > bDstWeight)
            {
                assignOK &= edge->setEdgeWeightMaxChecked(bDstWeight, slop, &usedSlop);
            }

            if (!assignOK)
            {
                // Here we have inconsistent profile data
                inconsistentProfileData = true;
                // No point in continuing
                goto EARLY_EXIT;
            }
        }
    }

    fgEdgeCount = numEdges;

    iterations = 0;

    do
    {
        iterations++;
        goodEdgeCountPrevious    = goodEdgeCountCurrent;
        goodEdgeCountCurrent     = 0;
        hasIncompleteEdgeWeights = false;

        for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
        {
            for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
            {
                bool assignOK = true;

                // We are processing the control flow edge (bSrc -> bDst)
                bSrc = edge->flBlock;

                slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;
                if (bSrc->bbJumpKind == BBJ_COND)
                {
                    int diff;
                    flowList* otherEdge;
                    if (bSrc->bbNext == bDst)
                    {
                        otherEdge = fgGetPredForBlock(bSrc->bbJumpDest, bSrc);
                    }
                    else
                    {
                        otherEdge = fgGetPredForBlock(bSrc->bbNext, bSrc);
                    }
                    noway_assert(edge->flEdgeWeightMin <= edge->flEdgeWeightMax);
                    noway_assert(otherEdge->flEdgeWeightMin <= otherEdge->flEdgeWeightMax);

                    // Adjust edge->flEdgeWeightMin up or adjust otherEdge->flEdgeWeightMax down
                    diff = ((int) bSrc->bbWeight) - ((int) edge->flEdgeWeightMin  + (int) otherEdge->flEdgeWeightMax);
                    if (diff > 0)
                    {
                        assignOK &= edge->setEdgeWeightMinChecked(edge->flEdgeWeightMin + diff, slop, &usedSlop);
                    }
                    else if (diff < 0)
                    {
                        assignOK &= otherEdge->setEdgeWeightMaxChecked(otherEdge->flEdgeWeightMax + diff, slop, &usedSlop);
                    }

                    // Adjust otherEdge->flEdgeWeightMin up or adjust edge->flEdgeWeightMax down
                    diff = ((int) bSrc->bbWeight) - ((int) otherEdge->flEdgeWeightMin  + (int) edge->flEdgeWeightMax);
                    if (diff > 0)
                    {
                        assignOK &= otherEdge->setEdgeWeightMinChecked(otherEdge->flEdgeWeightMin + diff, slop, &usedSlop);
                    }
                    else if (diff < 0)
                    {
                        assignOK &= edge->setEdgeWeightMaxChecked(edge->flEdgeWeightMax + diff, slop, &usedSlop);
                    }

                    if (!assignOK)
                    {
                        // Here we have inconsistent profile data
                        inconsistentProfileData = true;
                        // No point in continuing
                        goto EARLY_EXIT;
                    }
#ifdef DEBUG
                    // Now edge->flEdgeWeightMin and otherEdge->flEdgeWeightMax) should add up to bSrc->bbWeight
                    diff = ((int) bSrc->bbWeight) - ((int) edge->flEdgeWeightMin  + (int) otherEdge->flEdgeWeightMax);
                    noway_assert((-((int) slop) <= diff) && (diff <= ((int) slop)));

                    // Now otherEdge->flEdgeWeightMin and edge->flEdgeWeightMax) should add up to bSrc->bbWeight
                    diff = ((int) bSrc->bbWeight) - ((int) otherEdge->flEdgeWeightMin  + (int) edge->flEdgeWeightMax);
                    noway_assert((-((int) slop) <= diff) && (diff <= ((int) slop)));
#endif // DEBUG
                }
            }
        }

        for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
        {
            BasicBlock::weight_t bDstWeight = bDst->bbWeight;

            if (bDstWeight == BB_MAX_WEIGHT)
            {
                inconsistentProfileData = true;
                // No point in continuing
                goto EARLY_EXIT;
            }
            else
            {
                // We subtract out the called count so that bDstWeight is
                // the sum of all edges that go into this block from this method.
                //
                if (bDst == fgFirstBB)
                {
                    bDstWeight -= fgCalledWeight;
                }

                UINT64 minEdgeWeightSum = 0;
                UINT64 maxEdgeWeightSum = 0;

                // Calculate the sums of the minimum and maximum edge weights
                for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
                {
                    // We are processing the control flow edge (bSrc -> bDst)
                    bSrc = edge->flBlock;

                    maxEdgeWeightSum += edge->flEdgeWeightMax;
                    minEdgeWeightSum += edge->flEdgeWeightMin;
                }

                // maxEdgeWeightSum is the sum of all flEdgeWeightMax values into bDst
                // minEdgeWeightSum is the sum of all flEdgeWeightMin values into bDst

                for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
                {
                    bool assignOK = true;

                    // We are processing the control flow edge (bSrc -> bDst)
                    bSrc = edge->flBlock;
                    slop = BasicBlock::GetSlopFraction(bSrc, bDst) + 1;

                    // otherMaxEdgesWeightSum is the sum of all of the other edges flEdgeWeightMax values
                    // This can be used to compute a lower bound for our minimum edge weight
                    noway_assert(maxEdgeWeightSum >= edge->flEdgeWeightMax);
                    UINT64 otherMaxEdgesWeightSum = maxEdgeWeightSum - edge->flEdgeWeightMax;

                    // otherMinEdgesWeightSum is the sum of all of the other edges flEdgeWeightMin values
                    // This can be used to compute an upper bound for our maximum edge weight
                    noway_assert(minEdgeWeightSum >= edge->flEdgeWeightMin);
                    UINT64 otherMinEdgesWeightSum = minEdgeWeightSum - edge->flEdgeWeightMin;

                    if (bDstWeight >= otherMaxEdgesWeightSum)
                    {
                        // minWeightCalc is our minWeight when every other path to bDst takes it's flEdgeWeightMax value
                        BasicBlock::weight_t minWeightCalc = (BasicBlock::weight_t)(bDstWeight - otherMaxEdgesWeightSum);
                        if (minWeightCalc > edge->flEdgeWeightMin)
                        {
                            assignOK &= edge->setEdgeWeightMinChecked(minWeightCalc, slop, &usedSlop);
                        }
                    }

                    if (bDstWeight >= otherMinEdgesWeightSum)
                    {
                        // maxWeightCalc is our maxWeight when every other path to bDst takes it's flEdgeWeightMin value
                        BasicBlock::weight_t maxWeightCalc = (BasicBlock::weight_t)(bDstWeight - otherMinEdgesWeightSum);
                        if (maxWeightCalc < edge->flEdgeWeightMax)
                        {
                            assignOK &= edge->setEdgeWeightMaxChecked(maxWeightCalc, slop, &usedSlop);
                        }
                    }

                    if (!assignOK)
                    {
                        // Here we have inconsistent profile data
                        inconsistentProfileData = true;
                        // No point in continuing
                        goto EARLY_EXIT;
                    }

                    // When flEdgeWeightMin equals flEdgeWeightMax we have a "good" edge weight
                    if (edge->flEdgeWeightMin == edge->flEdgeWeightMax)
                    {
                        // Count how many "good" edge weights we have
                        // Each time through we should have more "good" weights
                        // We exit the while loop when no longer find any new "good" edges
                        goodEdgeCountCurrent++;
                    }
                    else
                    {
                        // Remember that we have seen at least one "Bad" edge weight
                        // so that we will repeat the while loop again
                        hasIncompleteEdgeWeights = true;
                    }
                }
            }

        }

        if (inconsistentProfileData)
        {
            hasIncompleteEdgeWeights = true;
            break;
        }

        if (numEdges == goodEdgeCountCurrent)
        {
            noway_assert(hasIncompleteEdgeWeights == false);
            break;
        }

    } while (hasIncompleteEdgeWeights && (goodEdgeCountCurrent > goodEdgeCountPrevious) && (iterations < 8));

EARLY_EXIT:;

#ifdef DEBUG
    if  (verbose)
    {
        if (inconsistentProfileData)
        {
            printf("fgComputeEdgeWeights() found inconsistent profile data, not using the edge weights\n");
        }
        else
        {
            if (hasIncompleteEdgeWeights)
            {
                printf("fgComputeEdgeWeights() was able to compute exact edge weights for %3d of the %3d edges, using %d passes.\n",
                    goodEdgeCountCurrent, numEdges, iterations);
            }
            else
            {
                printf("fgComputeEdgeWeights() was able to compute exact edge weights for all of the %3d edges, using %d passes.\n",
                    numEdges, iterations);
            }

            fgPrintEdgeWeights();
        }
    }
#endif // DEBUG

    fgSlopUsedInEdgeWeights = usedSlop;
    fgRangeUsedInEdgeWeights = false;

    // See if any edge weight are expressed in [min..max] form

    for (bDst = fgFirstBB; bDst != NULL; bDst = bDst->bbNext)
    {
        if (bDst->bbPreds != NULL)
        {
            for (edge = bDst->bbPreds; edge != NULL; edge = edge->flNext)
            {
                bSrc = edge->flBlock;
                // This is the control flow edge (bSrc -> bDst)

                if (edge->flEdgeWeightMin != edge->flEdgeWeightMax)
                {
                    fgRangeUsedInEdgeWeights = true;
                    break;
                }
            }
            if (fgRangeUsedInEdgeWeights)
            {
                break;
            }
        }
    }

   fgHaveValidEdgeWeights = !inconsistentProfileData;
   fgEdgeWeightsComputed  = true;
}

// fgOptimizeBranchToEmptyUnconditional:
//    optimize a jump to an empty block which ends in an unconditional branch.
//  Args:
//      block: source block
//      bDest: destination 
//  Returns: true if we changed the code
//
bool Compiler::fgOptimizeBranchToEmptyUnconditional(BasicBlock* block, BasicBlock* bDest)
{
    bool optimizeJump = true;

    assert(bDest->isEmpty());
    assert(bDest->bbJumpKind == BBJ_ALWAYS);

    // We do not optimize jumps between two different try regions.
    // However jumping to a block that is not in any try region is OK
    //
    if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
    {
        optimizeJump = false;
    }

    // Don't optimize a jump to a removed block
    if (bDest->bbJumpDest->bbFlags & BBF_REMOVED)
    {
        optimizeJump = false;
    }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    // Don't optimize a jump to a finally target. For BB1->BB2->BB3, where
    // BB2 is a finally target, if we changed BB1 to jump directly to BB3,
    // it would skip the finally target. BB1 might be a BBJ_ALWAYS block part
    // of a BBJ_CALLFINALLY/BBJ_ALWAYS pair, so changing the finally target
    // would change the unwind behavior.
    if (bDest->bbFlags & BBF_FINALLY_TARGET)
    {
        optimizeJump = false;
    }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

    // Must optimize jump if bDest has been removed
    //
    if (bDest->bbFlags & BBF_REMOVED)
    {
        optimizeJump = true;
    }

    // If we are optimizing using real profile weights
    // then don't optimize a conditional jump to an unconditional jump
    // until after we have computed the edge weights
    //
    if (fgIsUsingProfileWeights() && !fgEdgeWeightsComputed)
    {
        fgNeedsUpdateFlowGraph = true;
        optimizeJump = false;
    }

    if (optimizeJump)
    {
#ifdef DEBUG
        if  (verbose)
        {
            printf("\nOptimizing a jump to an unconditional jump (BB%02u -> BB%02u -> BB%02u)\n",
                   block->bbNum, bDest->bbNum, bDest->bbJumpDest->bbNum);
        }
#endif // DEBUG

        //
        // When we optimize a branch to branch we need to update the profile weight
        // of bDest by subtracting out the block/edge weight of the path that is being optimized.
        //
        if (fgHaveValidEdgeWeights && ((bDest->bbFlags & BBF_PROF_WEIGHT) != 0))
        {
            flowList* edge1 = fgGetPredForBlock(bDest, block);
            noway_assert(edge1 != NULL);

            BasicBlock::weight_t edgeWeight;

            if (edge1->flEdgeWeightMin != edge1->flEdgeWeightMax)
            {
                //
                // We only have an estimate for the edge weight
                //
                edgeWeight = (edge1->flEdgeWeightMin + edge1->flEdgeWeightMax) / 2;
                //
                //  Clear the profile weight flag
                //
                bDest->bbFlags  &= ~BBF_PROF_WEIGHT;
            }
            else
            {
                //
                // We only have the exact edge weight
                //
                edgeWeight = edge1->flEdgeWeightMin;
            }

            //
            // Update the bDest->bbWeight
            //
            if (bDest->bbWeight > edgeWeight)
            {
                bDest->bbWeight -= edgeWeight;
            }
            else
            {
                bDest->bbWeight = BB_ZERO_WEIGHT;
                bDest->bbFlags |= BBF_RUN_RARELY;        // Set the RarelyRun flag
            }

            flowList* edge2 = fgGetPredForBlock(bDest->bbJumpDest, bDest);

            if (edge2 != NULL)
            {
                //
                // Update the edge2 min/max weights
                //
                if (edge2->flEdgeWeightMin > edge1->flEdgeWeightMin)
                {
                    edge2->flEdgeWeightMin -= edge1->flEdgeWeightMin;
                }
                else
                {
                    edge2->flEdgeWeightMin = BB_ZERO_WEIGHT;
                }

                if (edge2->flEdgeWeightMax > edge1->flEdgeWeightMin)
                {
                    edge2->flEdgeWeightMax -= edge1->flEdgeWeightMin;
                }
                else
                {
                    edge2->flEdgeWeightMax = BB_ZERO_WEIGHT;
                }
            }
        }

        // Optimize the JUMP to empty unconditional JUMP to go to the new target
        block->bbJumpDest = bDest->bbJumpDest;

        fgAddRefPred(bDest->bbJumpDest, block, fgRemoveRefPred(bDest, block));

        return true;
    }
    return false;
}

// fgOptimizeEmptyBlock:
//   Does flow optimization of an empty block (can remove it in some cases)
//
//  Args:
//      block: an empty block
//  Returns: true if we changed the code

bool Compiler::fgOptimizeEmptyBlock(BasicBlock* block)
{
    assert(block->isEmpty());

    BasicBlock* bPrev = block->bbPrev;

    switch (block->bbJumpKind)
    {
    case BBJ_COND:
    case BBJ_SWITCH:
    case BBJ_THROW:

        /* can never happen */
        noway_assert(!"Conditional, switch, or throw block with empty body!");
        break;

    case BBJ_CALLFINALLY:
    case BBJ_RETURN:
    case BBJ_EHCATCHRET:
    case BBJ_EHFINALLYRET:
    case BBJ_EHFILTERRET:

        /* leave them as is */
        /* some compilers generate multiple returns and put all of them at the end - 
         * to solve that we need the predecessor list */

        break;

    case BBJ_ALWAYS:

        // A GOTO cannot be to the next block since that
        // should have been fixed by the  optimization above
        // An exception is made for a jump from Hot to Cold
        noway_assert(block->bbJumpDest != block->bbNext ||
                     ((bPrev != nullptr) && bPrev->isBBCallAlwaysPair()) ||
                     fgInDifferentRegions(block, block->bbNext));

        /* Cannot remove the first BB */
        if (!bPrev)
            break;

        /* Do not remove a block that jumps to itself - used for while (true){} */
        if (block->bbJumpDest == block)
            break;

        /* Empty GOTO can be removed iff bPrev is BBJ_NONE */
        if (bPrev->bbJumpKind != BBJ_NONE)
            break;

        // can't allow fall through into cold code
        if (block->bbNext == fgFirstColdBlock)
            break;

        /* Can fall through since this is similar with removing
         * a BBJ_NONE block, only the successor is different */

        __fallthrough;

    case BBJ_NONE:

        /* special case if this is the first BB */
        if (!bPrev)
        {
            assert (block == fgFirstBB);
        }
        else
        {
            /* If this block follows a BBJ_CALLFINALLY do not remove it
             * (because we don't know who may jump to it) */
            if (bPrev->bbJumpKind == BBJ_CALLFINALLY)
                break;
        }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        /* Don't remove finally targets */
        if (block->bbFlags & BBF_FINALLY_TARGET)
            break;
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

#if FEATURE_EH_FUNCLETS
        /* Don't remove an empty block that is in a different EH region
         * from its successor block, if the block is the target of a
         * catch return. It is required that the return address of a
         * catch be in the correct EH region, for re-raise of thread
         * abort exceptions to work. Insert a NOP in the empty block
         * to ensure we generate code for the block, if we keep it.
         */
        {
            BasicBlock* succBlock;

            if (block->bbJumpKind == BBJ_ALWAYS)
            {
                succBlock = block->bbJumpDest;
            }
            else
            {
                succBlock = block->bbNext;
            }


            if ((succBlock != NULL) &&
                !BasicBlock::sameEHRegion(block, succBlock))
            {
                // The empty block and the block that follows it are in different
                // EH regions. Is this a case where they can't be merged?

                bool okToMerge = true; // assume it's ok
                for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
                {
                    if (pred->flBlock->bbJumpKind == BBJ_EHCATCHRET)
                    {
                        assert(pred->flBlock->bbJumpDest == block);
                        okToMerge = false; // we can't get rid of the empty block
                        break;
                    }
                }

                if (!okToMerge)
                {
                    // Insert a NOP in the empty block to ensure we generate code
                    // for the catchret target in the right EH region.
                    GenTreePtr nopStmt =
                        fgInsertStmtAtEnd(block, new (this, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID));
                    fgSetStmtSeq(nopStmt);
                    gtSetStmtInfo(nopStmt);

#ifdef DEBUG
                    if  (verbose)
                    {
                        printf("\nKeeping empty block BB%02u - it is the target of a catch return\n",
                               block->bbNum);
                    }
#endif // DEBUG

                    break; // go to the next block
                }
            }
        }
#endif // FEATURE_EH_FUNCLETS

        if (!ehCanDeleteEmptyBlock(block))
        {
            // We're not allowed to remove this block due to reasons related to the EH table.
            break;
        }

        /* special case if this is the last BB */
        if (block == fgLastBB)
        {
            if (!bPrev)
                break;
            fgLastBB = bPrev;
        }

        /* Remove the block */
        compCurBB = block;
        fgRemoveBlock(block, false);
        return true;

    default:
        noway_assert(!"Unexpected bbJumpKind");
        break;
    }
    return false;

}

// fgOptimizeSwitchBranches:
//   Does flow optimization for a switch - bypasses jumps to empty unconditional branches,
//      and transforms degenerate switch cases like those with 1 or 2 targets
//
//  Args:
//      block: BasicBlock that contains the switch
//  Returns: true if we changed the code
//
bool Compiler::fgOptimizeSwitchBranches(BasicBlock* block)
{
    assert(block->bbJumpKind == BBJ_SWITCH);

    unsigned        jmpCnt = block->bbJumpSwt->bbsCount;
    BasicBlock**    jmpTab = block->bbJumpSwt->bbsDstTab;
    BasicBlock*     bNewDest;  // the new jump target for the current switch case
    BasicBlock*     bDest;
    bool returnvalue = false;

    do
    {
    REPEAT_SWITCH:;
        bDest    = *jmpTab;
        bNewDest = bDest;

        // Do we have a JUMP to an empty unconditional JUMP block?
        if (bDest->isEmpty() &&
            (bDest->bbJumpKind == BBJ_ALWAYS) &&
            (bDest != bDest->bbJumpDest))         // special case for self jumps
        {
            bool optimizeJump = true;

            // We do not optimize jumps between two different try regions.
            // However jumping to a block that is not in any try region is OK
            //
            if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
            {
                optimizeJump = false;
            }

            // If we are optimize using real profile weights
            // then don't optimize a switch jump to an unconditional jump
            // until after we have computed the edge weights
            //
            if (fgIsUsingProfileWeights() && !fgEdgeWeightsComputed)
            {
                fgNeedsUpdateFlowGraph = true;
                optimizeJump = false;
            }

            if (optimizeJump)
            {
                bNewDest = bDest->bbJumpDest;
#ifdef DEBUG
                if  (verbose)
                {
                    printf("\nOptimizing a switch jump to an empty block with an unconditional jump (BB%02u -> BB%02u -> BB%02u)\n",
                           block->bbNum, bDest->bbNum, bNewDest->bbNum);
                }
#endif // DEBUG
            }
        }

        if (bNewDest != bDest)
        {
            //
            // When we optimize a branch to branch we need to update the profile weight
            // of bDest by subtracting out the block/edge weight of the path that is being optimized.
            //
            if (fgIsUsingProfileWeights() && ((bDest->bbFlags & BBF_PROF_WEIGHT) != 0))
            {
                if (fgHaveValidEdgeWeights)
                {
                    flowList* edge = fgGetPredForBlock(bDest, block);
                    BasicBlock::weight_t branchThroughWeight = edge->flEdgeWeightMin;

                    if  (bDest->bbWeight > branchThroughWeight)
                    {
                        bDest->bbWeight -= branchThroughWeight;
                    }
                    else
                    {
                        bDest->bbWeight = BB_ZERO_WEIGHT;
                        bDest->bbFlags |= BBF_RUN_RARELY;
                    }
                }
            }

            // Update the switch jump table
            *jmpTab = bNewDest;

            // Maintain, if necessary, the set of unique targets of "block."
            UpdateSwitchTableTarget(block, bDest, bNewDest);

            fgAddRefPred(bNewDest, block, fgRemoveRefPred(bDest, block));

            // we optimized a Switch label - goto REPEAT_SWITCH to follow this new jump
            returnvalue = true;

            goto REPEAT_SWITCH;
        }
    }
    while (++jmpTab, --jmpCnt);

    GenTreeStmt* switchStmt = block->lastTopLevelStmt();
    GenTreePtr   switchTree = switchStmt->gtStmtExpr;

    // If this is a Lowered switch, it must have no embedded statements, because we pulled
    // out any embedded statements when we cloned the switch value.
    if (switchTree->gtOper == GT_SWITCH_TABLE)
    {
        noway_assert(fgOrder == FGOrderLinear);
        assert(switchStmt->AsStmt()->gtStmtIsTopLevel() &&
               (switchStmt->gtNext == nullptr));
    }
    else
    {
        noway_assert(switchTree->gtOper == GT_SWITCH);
    }
    noway_assert(switchTree->gtType == TYP_VOID);

    
    // At this point all of the case jump targets have been updated such
    // that none of them go to block that is an empty unconditional block
    //
    jmpTab = block->bbJumpSwt->bbsDstTab;
    jmpCnt = block->bbJumpSwt->bbsCount;
    // Now check for two trivial switch jumps.
    //
    if (block->NumSucc(this) == 1)
    {
        // Use BBJ_ALWAYS for a switch with only a default clause, or with only one unique successor.
        BasicBlock* uniqueSucc = jmpTab[0];

#ifdef DEBUG
        if  (verbose)
        {
            printf("\nRemoving a switch jump with a single target (BB%02u)\n",
                   block->bbNum);
            printf("BEFORE:\n");
        }
#endif // DEBUG

        /* check for SIDE_EFFECTS */

        if (switchTree->gtFlags & GTF_SIDE_EFFECT)
        {
            /* Extract the side effects from the conditional */
            GenTreePtr  sideEffList = NULL;

            gtExtractSideEffList(switchTree, &sideEffList);

            if (sideEffList == NULL)
                goto NO_SWITCH_SIDE_EFFECT;

            noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);

#ifdef DEBUG
            if  (verbose)
            {
                printf("\nSwitch expression has side effects! Extracting side effects...\n");
                gtDispTree(switchTree); printf("\n");
                gtDispTree(sideEffList); printf("\n");
            }
#endif // DEBUG

            /* Replace the conditional statement with the list of side effects */
            noway_assert(sideEffList->gtOper != GT_STMT);
            noway_assert(sideEffList->gtOper != GT_SWITCH);

            switchStmt->gtStmtExpr = sideEffList;

            if (fgStmtListThreaded)
            {
                /* Update the lclvar ref counts */
                compCurBB = block;
                fgUpdateRefCntForExtract(switchTree, sideEffList);

                /* Update ordering, costs, FP levels, etc. */
                gtSetStmtInfo(switchStmt);

                /* Re-link the nodes for this statement */
                fgSetStmtSeq(switchStmt);
            }
        }
        else
        {

        NO_SWITCH_SIDE_EFFECT:

            /* conditional has NO side effect - remove it */
            fgRemoveStmt(block, switchStmt);
        }

        // Change the switch jump into a BBJ_ALWAYS
        block->bbJumpDest = block->bbJumpSwt->bbsDstTab[0];
        block->bbJumpKind = BBJ_ALWAYS;
        if (jmpCnt > 1)
        {
            for (unsigned i = 1; i < jmpCnt; ++i)
            {
                (void) fgRemoveRefPred(jmpTab[i], block);
            }
        }

        return true;
    }
    else if (block->bbJumpSwt->bbsCount == 2 &&
             block->bbJumpSwt->bbsDstTab[1] == block->bbNext)
    {
        /* Use a BBJ_COND(switchVal==0) for a switch with only one
           significant clause besides the default clause, if the
           default clause is bbNext */
        GenTree* switchVal = switchTree->gtOp.gtOp1;
        noway_assert(genActualTypeIsIntOrI(switchVal->TypeGet()));

        // Change the GT_SWITCH(switchVal) into GT_JTRUE(GT_EQ(switchVal==0)).
        // Also mark the node as GTF_DONT_CSE as further down JIT is not capable of handling it.
        // For example CSE could determine that the expression rooted at GT_EQ is a candidate cse and
        // replace it with a COMMA node.  In such a case we will end up with GT_JTRUE node pointing to
        // a COMMA node which results in noway asserts in fgMorphSmpOp(), optAssertionGen() and rpPredictTreeRegUse().
        // For the same reason fgMorphSmpOp() marks GT_JTRUE nodes with RELOP children as GTF_DONT_CSE.
#ifdef DEBUG
        if  (verbose)
        {
            printf("\nConverting a switch (BB%02u) with only one significant clause besides a default target to a conditional branch\n",
                   block->bbNum);
        }
#endif // DEBUG

        switchTree->ChangeOper(GT_JTRUE);
        GenTree* zeroConstNode  = gtNewZeroConNode(genActualType(switchVal->TypeGet()));
        GenTree* condNode       = gtNewOperNode(GT_EQ, TYP_INT,
                                               switchVal,
                                               zeroConstNode);
        switchTree->gtOp.gtOp1  = condNode;
        switchTree->gtOp.gtOp1->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);
        // Re-link the nodes for this statement.
        // We know that this is safe for the Lowered form, because we will have eliminated any embedded trees
        // when we cloned the switch condition (it is also asserted above).
        fgSetStmtSeq(switchStmt);
        block->bbJumpDest = block->bbJumpSwt->bbsDstTab[0];
        block->bbJumpKind = BBJ_COND;

        return true;
    }
    return returnvalue;
}

// fgBlockEndFavorsTailDuplication:
//     Heuristic function that returns true if this block ends in a statement that looks favorable
//     for tail-duplicating its successor (such as assigning a constant to a local).
//  Args:
//      block: BasicBlock we are considering duplicating the successor of
//  Returns: 
//      true if it seems like a good idea
//
bool Compiler::fgBlockEndFavorsTailDuplication(BasicBlock *block)
{
    if (block->isRunRarely())
        return false;

    if (!block->lastTopLevelStmt())
        return false;
    else
    {
        // Tail duplication tends to pay off when the last statement
        // is an assignment of a constant, arraylength, or a relop.
        // This is because these statements produce information about values
        // that would otherwise be lost at the upcoming merge point.  

        GenTreeStmt* lastStmt = block->lastTopLevelStmt();
        GenTree* tree = lastStmt->gtStmtExpr;
        if (tree->gtOper != GT_ASG)
            return false;

        GenTree* op2 = tree->gtOp.gtOp2;
        if (op2->gtOper != GT_ARR_LENGTH
            &&  !op2->OperIsConst()
            && ((op2->OperKind() & GTK_RELOP) == 0))
        {
            return false;
        }
    }
    return true;
}


// fgBlockIsGoodTailDuplicationCandidate:
//     Heuristic function that examines a block (presumably one that is a merge point) to determine 
//     if it should be duplicated.
// args:
//     target - the tail block (candidate for duplication)
// returns:
//     true if this block seems like a good candidate for duplication
//
bool Compiler::fgBlockIsGoodTailDuplicationCandidate(BasicBlock *target)
{
    GenTreeStmt* stmt = target->FirstNonPhiDef();

    // Here we are looking for blocks with a single statement feeding a conditional branch.
    // These blocks are small, and when duplicated onto the tail of blocks that end in 
    // assignments, there is a high probability of the branch completely going away.

    // This is by no means the only kind of tail that it is beneficial to duplicate,
    // just the only one we recognize for now.
    
    if (stmt != target->lastStmt())
        return false;

    if (target->bbJumpKind != BBJ_COND)
        return false;

    GenTree* tree = stmt->gtStmtExpr;

    if (tree->gtOper != GT_JTRUE)
        return false;

    // must be some kind of relational operator
    GenTree* cond = tree->gtOp.gtOp1;
    if (!(cond->OperKind() & GTK_RELOP))
        return false;

    // op1 must be some combinations of casts of local or constant
    GenTree* op1 = cond->gtOp.gtOp1;
    while (op1->gtOper == GT_CAST)
        op1 = op1->gtOp.gtOp1;
    if (!op1->IsLocal() && !op1->OperIsConst())
        return false;

    // op2 must be some combinations of casts of local or constant
    GenTree* op2 = cond->gtOp.gtOp2;
    while (op2->gtOper == GT_CAST)
        op2 = op2->gtOp.gtOp1;
    if (!op2->IsLocal() && !op2->OperIsConst())
        return false;

    return true;
}

// fgOptimizeUncondBranchToSimpleCond:
//    For a block which has an unconditional branch, look to see if its target block
//    is a good candidate for tail duplication, and if so do that duplication.
//
// Args:
//    block  - block with uncond branch
//    target - block which is target of first block
// 
// returns: true if changes were made

bool Compiler::fgOptimizeUncondBranchToSimpleCond(BasicBlock* block, BasicBlock* target)
{
    assert(block->bbJumpKind == BBJ_ALWAYS);
    assert(block->bbJumpDest == target);

    // TODO-Review: OK if they are in the same region?
    if (compHndBBtabCount > 0)
        return false;

    if (!fgBlockIsGoodTailDuplicationCandidate(target))
        return false;

    if (!fgBlockEndFavorsTailDuplication(block))
        return false;

    GenTreeStmt* stmt = target->FirstNonPhiDef();

    // Duplicate the target block at the end of this block
    
    GenTree* cloned = gtCloneExpr(stmt->gtStmtExpr);
    noway_assert(cloned);
    GenTree *jmpStmt = gtNewStmt(cloned);

    block->bbJumpKind = BBJ_COND;
    block->bbJumpDest = target->bbJumpDest;
    fgAddRefPred(block->bbJumpDest, block);
    fgRemoveRefPred(target, block);

    // add an unconditional block after this block to jump to the target block's fallthrough block

    BasicBlock *next = fgNewBBafter(BBJ_ALWAYS, block, true);
    next->bbFlags = block->bbFlags | BBF_INTERNAL;
    next->bbFlags &= ~(BBF_TRY_BEG | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_HAS_LABEL | BBF_JMP_TARGET |
                           BBF_FUNCLET_BEG | BBF_LOOP_PREHEADER | BBF_KEEP_BBJ_ALWAYS);
    
    next->bbJumpDest = target->bbNext;
    target->bbNext->bbFlags |= BBF_JMP_TARGET;
    fgAddRefPred(next, block); 
    fgAddRefPred(next->bbJumpDest, next);

#ifdef DEBUG
    if  (verbose)
    {
        printf("fgOptimizeUncondBranchToSimpleCond(from BB%02u to cond BB%02u), created new uncond BB%02u\n", 
                block->bbNum, target->bbNum, next->bbNum);
    }
#endif // DEBUG

    if (fgStmtListThreaded)
    {
        gtSetStmtInfo(jmpStmt);
    }

    fgInsertStmtAtEnd(block, jmpStmt);

    return true;
}


// fgOptimizeBranchToNext:
//    Optimize a block which has a branch to the following block
// Args:
//    block - block with a branch
//    bNext - block which is both next and the target of the first block
//    bPrev - block which is prior to the first block
//
// returns: true if changes were made
//
bool Compiler::fgOptimizeBranchToNext(BasicBlock* block, BasicBlock* bNext, BasicBlock* bPrev)
{
    assert(block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_ALWAYS);
    assert(block->bbJumpDest == bNext);
    assert(block->bbNext = bNext);
    assert(block->bbPrev == bPrev);

    if (block->bbJumpKind == BBJ_ALWAYS)
    {
        // We can't remove it if it is a branch from hot => cold
        if (!fgInDifferentRegions(block, bNext))
        {
            // We can't remove if it is marked as BBF_KEEP_BBJ_ALWAYS
            if (!(block->bbFlags & BBF_KEEP_BBJ_ALWAYS))
            {
                // We can't remove if the BBJ_ALWAYS is part of a BBJ_CALLFINALLY pair
                if ( (bPrev == nullptr) || !bPrev->isBBCallAlwaysPair() )
                {
                    /* the unconditional jump is to the next BB  */
                    block->bbJumpKind = BBJ_NONE;
                    block->bbFlags &= ~BBF_NEEDS_GCPOLL;
                    return true;
#ifdef DEBUG
                    if  (verbose)
                    {
                        printf("\nRemoving unconditional jump to next block (BB%02u -> BB%02u) (converted BB%02u to fall-through)\n",
                               block->bbNum, bNext->bbNum, block->bbNum);
                    }
#endif // DEBUG
                }
            }
        }
    }
    else
    {
        /* remove the conditional statement at the end of block */
        noway_assert(block->bbJumpKind == BBJ_COND);
        noway_assert(block->bbTreeList);

        GenTreeStmt* cond = block->lastTopLevelStmt();
        noway_assert(cond->gtStmtExpr->gtOper == GT_JTRUE);

#ifdef DEBUG
        if  (verbose)
        {
            printf("\nRemoving conditional jump to next block (BB%02u -> BB%02u)\n",
                   block->bbNum, bNext->bbNum);
        }
#endif // DEBUG

        /* check for SIDE_EFFECTS */

        if (cond->gtStmtExpr->gtFlags & GTF_SIDE_EFFECT)
        {
            if (compRationalIRForm)
            {
                // Extracting side-effects won't work in rationalized form.
                // Instead just transform the JTRUE into a NEG which has the effect of
                // evaluating the side-effecting tree and perform a benign operation on it.
                // TODO-CQ: [TFS:1121057] We should be able to simply remove the jump node,
                // and change gtStmtExpr to its op1.
                cond->gtStmtExpr->SetOper(GT_NEG);
                cond->gtStmtExpr->gtType = TYP_I_IMPL;
            }
            else
            {
                /* Extract the side effects from the conditional */
                GenTreePtr  sideEffList = NULL;

                gtExtractSideEffList(cond->gtStmtExpr, &sideEffList);

                if (sideEffList == NULL)
                {
                    compCurBB = block;
                    fgRemoveStmt(block, cond);
                }
                else
                {
                    noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nConditional has side effects! Extracting side effects...\n");
                        gtDispTree(cond); printf("\n");
                        gtDispTree(sideEffList); printf("\n");
                    }
#endif // DEBUG

                    /* Replace the conditional statement with the list of side effects */
                    noway_assert(sideEffList->gtOper != GT_STMT);
                    noway_assert(sideEffList->gtOper != GT_JTRUE);

                    cond->gtStmtExpr = sideEffList;

                    if (fgStmtListThreaded)
                    {
                        /* Update the lclvar ref counts */
                        compCurBB = block;
                        fgUpdateRefCntForExtract(cond->gtStmtExpr, sideEffList);

                        /* Update ordering, costs, FP levels, etc. */
                        gtSetStmtInfo(cond);

                        /* Re-link the nodes for this statement */
                        fgSetStmtSeq(cond);
                    }
                 }
            }
        }
        else
        {
           compCurBB = block;
           /* conditional has NO side effect - remove it */
           fgRemoveStmt(block, cond);
        }

        /* Conditional is gone - simply fall into the next block */

        block->bbJumpKind = BBJ_NONE;
        block->bbFlags &= ~BBF_NEEDS_GCPOLL;

        /* Update bbRefs and bbNum - Conditional predecessors to the same
         * block are counted twice so we have to remove one of them */

        noway_assert(bNext->countOfInEdges() > 1);
        fgRemoveRefPred(bNext, block);

        return true;
    }
    return false;
}

/*****************************************************************************
 *
 *  Function called to optimize an unconditional branch that branches
 *  to a conditional branch.
 *  Currently we require that the conditional branch jump back to the
 *  block that follows the unconditional branch.
 *
 *  We can improve the code execution and layout by concatenating a copy
 *  of the conditional branch block at the end of the conditional branch
 *  and reversing the sense of the branch.
 *
 *  This is only done when the amount of code to be copied is smaller than
 *  our calculated threshold in maxDupCostSz.
 *
 */

bool                Compiler::fgOptimizeBranch(BasicBlock* bJump)
{
    if (opts.MinOpts())
        return false;

    if (bJump->bbJumpKind != BBJ_ALWAYS)
        return false;

    if (bJump->bbFlags & BBF_KEEP_BBJ_ALWAYS)
        return false;

    // Don't hoist a conditional branch into the scratch block; we'd prefer it stay
    // either BBJ_NONE or BBJ_ALWAYS.
    if (fgBBisScratch(bJump))
        return false;

    BasicBlock* bDest = bJump->bbJumpDest;

    if (bDest->bbJumpKind != BBJ_COND)
        return false;

    if (bDest->bbJumpDest != bJump->bbNext)
        return false;

    // 'bJump' must be in the same try region as the condition, since we're going to insert
    // a duplicated condition in 'bJump', and the condition might include exception throwing code.
    if (!BasicBlock::sameTryRegion(bJump, bDest))
        return false;

    // do not jump into another try region
    BasicBlock* bDestNext = bDest->bbNext;
    if (bDestNext->hasTryIndex() && !BasicBlock::sameTryRegion(bJump, bDestNext))
        return false;

    GenTreeStmt* stmt;
    unsigned   estDupCostSz = 0;
    for (stmt = bDest->firstStmt(); stmt; stmt = stmt->gtNextStmt)
    {
        GenTreePtr expr = stmt->gtStmtExpr;

        /* We call gtPrepareCost to measure the cost of duplicating this tree */
        gtPrepareCost(expr);

        estDupCostSz += expr->gtCostSz;
    }

    bool   allProfileWeightsAreValid  = false;
    BasicBlock::weight_t weightJump   = bJump->bbWeight;
    BasicBlock::weight_t weightDest   = bDest->bbWeight;
    BasicBlock::weight_t weightNext   = bJump->bbNext->bbWeight;
    bool rareJump = bJump->isRunRarely();
    bool rareDest = bDest->isRunRarely();
    bool rareNext = bJump->bbNext->isRunRarely();

    // If we have profile data then we calculate the number of time
    // the loop will iterate into loopIterations
    if (fgIsUsingProfileWeights())
    {
        // Only rely upon the profile weight when all three of these blocks
        // have either good profile weights or are rarelyRun
        //
        if ((bJump->bbFlags         & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)) &&
            (bDest->bbFlags         & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)) &&
            (bJump->bbNext->bbFlags & (BBF_PROF_WEIGHT | BBF_RUN_RARELY)))
        {
            allProfileWeightsAreValid = true;

            if ((weightJump * 100) < weightDest)
            {
                rareJump = true;
            }

            if ((weightNext * 100) < weightDest)
            {
                rareNext = true;
            }

            if (((weightDest * 100) < weightJump) &&
                ((weightDest * 100) < weightNext))
            {
                rareDest = true;
            }
        }
    }

    unsigned maxDupCostSz = 6;

    //
    // Branches between the hot and rarely run regions
    // should be minimized.  So we allow a larger size
    //
    if (rareDest != rareJump)
    {
        maxDupCostSz += 6;
    }

    if (rareDest != rareNext)
    {
        maxDupCostSz += 6;
    }

    //
    // We we are ngen-ing:
    // If the uncondional branch is a rarely run block then
    // we are willing to have more code expansion since we
    // won't be running code from this page
    //
    if (opts.eeFlags & CORJIT_FLG_PREJIT)
    {
        if (rareJump)
        {
            maxDupCostSz *= 2;
        }
    }


    // If the compare has too high cost then we don't want to dup

    bool costIsTooHigh = (estDupCostSz > maxDupCostSz);

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nDuplication of the conditional block BB%02u (always branch from BB%02u) %s, because the cost of duplication (%i) is %s than %i,"
               " validProfileWeights = %s\n",
                  bDest->bbNum,
                  bJump->bbNum,
                  costIsTooHigh ? "not done" : "performed",
                  estDupCostSz,
                  costIsTooHigh ? "greater" : "less or equal",
                  maxDupCostSz,
                  allProfileWeightsAreValid ? "true" : "false");
     }
#endif // DEBUG

    if (costIsTooHigh)
    {
        return false;
    }

    /* Looks good - duplicate the conditional block */

    GenTree*   newStmtList     = NULL;           // new stmt list to be added to bJump
    GenTree*   newStmtLast     = NULL;
    bool       cloneExprFailed = false;

    /* Visit all the statements in bDest */

    for (GenTree* curStmt = bDest->bbTreeList; curStmt; curStmt = curStmt->gtNext)
    {
        /* Clone/substitute the expression */

        stmt = gtCloneExpr(curStmt)->AsStmt();

        // cloneExpr doesn't handle everything

        if  (stmt == NULL)
        {
            cloneExprFailed = true;
            break;
        }

        /* Append the expression to our list */

        if  (newStmtList != NULL)
        {
            newStmtLast->gtNext = stmt;
        }
        else
        {
            newStmtList         = stmt;
        }

        stmt->gtPrev = newStmtLast;
        newStmtLast  = stmt;
    }

    if (cloneExprFailed)
        return false;

    noway_assert(newStmtLast != NULL);
    noway_assert(stmt != NULL);
    noway_assert(stmt->gtOper == GT_STMT);

    if ((newStmtLast == NULL) || (stmt == NULL) || (stmt->gtOper != GT_STMT))
        return false;

    /* Get to the condition node from the statement tree */

    GenTreePtr condTree = stmt->gtStmtExpr;
    noway_assert(condTree->gtOper == GT_JTRUE);

    if (condTree->gtOper != GT_JTRUE)
        return false;

    //
    // Set condTree to the operand to the GT_JTRUE
    //
    condTree = condTree->gtOp.gtOp1;

    //
    // This condTree has to be a RelOp comparison
    //
    if (condTree->OperIsCompare() == false)
        return false;

    // Bump up the ref-counts of any variables in 'stmt'
    fgUpdateRefCntForClone(bJump, stmt->gtStmtExpr);

    //
    // Find the last statement in the bJump block
    //
    GenTreeStmt* lastStmt = NULL;
    for (stmt = bJump->firstStmt(); stmt; stmt = stmt->gtNextStmt)
    {
        lastStmt = stmt;
    }
    stmt = bJump->firstStmt();

    /* Join the two linked lists */
    newStmtLast->gtNext = NULL;

    if (lastStmt != NULL)
    {
        stmt->gtPrev        = newStmtLast;
        lastStmt->gtNext    = newStmtList;
        newStmtList->gtPrev = lastStmt;
    }
    else
    {
         bJump->bbTreeList   = newStmtList;
         newStmtList->gtPrev = newStmtLast;
    }

    //
    // Reverse the sense of the compare
    //
    gtReverseCond(condTree);

    bJump->bbJumpKind = BBJ_COND;
    bJump->bbJumpDest = bDest->bbNext;

    /* Mark the jump dest block as being a jump target */
    bJump->bbJumpDest->bbFlags |= BBF_JMP_TARGET|BBF_HAS_LABEL;

    /* Update bbRefs and bbPreds */

    // bJump now falls through into the next block
    //
    fgAddRefPred(bJump->bbNext, bJump);

    // bJump no longer jumps to bDest
    //
    fgRemoveRefPred(bDest, bJump);

    // bJump now jumps to bDest->bbNext
    //
    fgAddRefPred(bDest->bbNext, bJump);

    if (weightJump > 0)
    {
        if (allProfileWeightsAreValid)
        {
            if (weightDest > weightJump)
            {
                bDest->bbWeight = (weightDest - weightJump);
            }
            else if (!bDest->isRunRarely())
            {
                bDest->bbWeight = BB_UNITY_WEIGHT;
            }
        }
        else
        {
            BasicBlock::weight_t newWeightDest = 0;
            BasicBlock::weight_t unloopWeightDest = 0;

            if (weightDest > weightJump)
            {
                newWeightDest = (weightDest - weightJump);
            }
            if (weightDest >= (BB_LOOP_WEIGHT*BB_UNITY_WEIGHT)/2)
            {
                newWeightDest = (weightDest*2) / (BB_LOOP_WEIGHT*BB_UNITY_WEIGHT);
            }
            if ((newWeightDest > 0) || (unloopWeightDest > 0))
            {
                bDest->bbWeight = Max(newWeightDest, unloopWeightDest);
            }
        }
    }

#if DEBUG
    if (verbose)
    {
        printf("\nAfter this change in fgOptimizeBranch");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    return true;
}

/*****************************************************************************
 *
 *  Function called to optimize switch statements
 */

bool                Compiler::fgOptimizeSwitchJumps()
{
    bool result = false; // Our return value

#if 0
    // TODO-CQ: Add switch jump optimizations?
    if (!fgHasSwitch)
        return false;

    if (!fgHaveValidEdgeWeights)
        return false;

    for (BasicBlock* bSrc = fgFirstBB; bSrc != NULL; bSrc = bSrc->bbNext)
    {
        if (bSrc->bbJumpKind == BBJ_SWITCH)
        {
            unsigned        jumpCnt; jumpCnt = bSrc->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab; jumpTab = bSrc->bbJumpSwt->bbsDstTab;

            do
            {
                BasicBlock*   bDst       = *jumpTab;
                flowList*     edgeToDst  = fgGetPredForBlock(bDst, bSrc);
                double        outRatio   = (double) edgeToDst->flEdgeWeightMin  / (double) bSrc->bbWeight;

                if (outRatio >= 0.60)
                {
                    // straighten switch here...
                }
            }
            while (++jumpTab, --jumpCnt);
        }
    }
#endif

    return result;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
/*****************************************************************************
 *
 *  Function called to reorder the flowgraph of BasicBlocks such that any
 *  rarely run blocks are placed at the end of the block list.
 *  If we have profile information we also use that information to reverse
 *  all conditional jumps that would benefit.
 */

void                Compiler::fgReorderBlocks()
{
    noway_assert(opts.compDbgCode == false);

#if FEATURE_EH_FUNCLETS
    assert(fgFuncletsCreated);
#endif // FEATURE_EH_FUNCLETS

    // We can't relocate anything if we only have one block
    if (fgFirstBB->bbNext == NULL)
    {
        return;
    }

    bool newRarelyRun      = false;
    bool movedBlocks       = false;
    bool optimizedSwitches = false;

    // First let us expand the set of run rarely blocks
    newRarelyRun |= fgExpandRarelyRunBlocks();

#if !FEATURE_EH_FUNCLETS
    movedBlocks  |= fgRelocateEHRegions();
#endif // !FEATURE_EH_FUNCLETS

    //
    // If we are using profile weights we can change some
    // switch jumps into conditional test and jump
    //
    if (fgIsUsingProfileWeights())
    {
        //
        // Note that this is currently not yet implemented
        //
        optimizedSwitches = fgOptimizeSwitchJumps();
        if (optimizedSwitches)
        {
            fgUpdateFlowGraph();
        }
    }

#ifdef DEBUG
    if  (verbose)
    {
        printf("*************** In fgReorderBlocks()\n");

        printf("\nInitial BasicBlocks");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    BasicBlock*     bNext;
    BasicBlock*     bPrev;
    BasicBlock*     block;
    unsigned        XTnum;
    EHblkDsc*       HBtab;

    // Iterate over every block, remembering our previous block in bPrev
    for (bPrev = fgFirstBB, block = bPrev->bbNext;
                            block != NULL;
         bPrev = block,     block = block->bbNext)
    {
        //
        // Consider relocating the rarely run blocks such that they are at the end of the method.
        // We also consider reversing conditional branches so that they become a not taken forwards branch.
        //

        // If block is marked with a BBF_KEEP_BBJ_ALWAYS flag then we don't move the block
        if ((block->bbFlags & BBF_KEEP_BBJ_ALWAYS) != 0)
            continue;

        // Finally and handlers blocks are to be kept contiguous.
        // TODO-CQ: Allow reordering within the handler region
        if (block->hasHndIndex() == true)
            continue;

        bool          reorderBlock   = true;   // This is set to false if we decide not to reorder 'block'
        bool          isRare         = block->isRunRarely();
        BasicBlock*   bDest          = NULL;
        bool          forwardBranch  = false;
        bool          backwardBranch = false;

        // Setup bDest
        if ((bPrev->bbJumpKind == BBJ_COND) || (bPrev->bbJumpKind == BBJ_ALWAYS))
        {
            bDest = bPrev->bbJumpDest;
            forwardBranch  = fgIsForwardBranch(bPrev);
            backwardBranch = !forwardBranch;
        }

        // We will look for bPrev as a non rarely run block followed by block as a rarely run block
        //
        if (bPrev->isRunRarely())
        {
            reorderBlock = false;
        }

        // If the weights of the bPrev, block and bDest were all obtained from a profile run
        // then we can use them to decide if it is useful to reverse this conditional branch

        BasicBlock::weight_t profHotWeight = -1;

        if ((bPrev->bbFlags & BBF_PROF_WEIGHT) &&
            (block->bbFlags & BBF_PROF_WEIGHT) &&
            ((bDest == NULL) || (bDest->bbFlags & BBF_PROF_WEIGHT)))
        {
            //
            // All blocks have profile information
            //
            if (forwardBranch)
            {
                if (bPrev->bbJumpKind == BBJ_ALWAYS)
                {
                    // We can pull up the blocks that the unconditional jump branches to
                    // if the weight of bDest is greater or equal to the weight of block
                    // also the weight of bDest can't be zero.
                    //
                    if ((bDest->bbWeight < block->bbWeight) || (bDest->bbWeight == 0))
                    {
                        reorderBlock = false;
                    }
                    else
                    {
                        //
                        // If this remains true then we will try to pull up bDest to succeed bPrev
                        //
                        bool moveDestUp = true;

                        if (fgHaveValidEdgeWeights)
                        {
                            //
                            // The edge bPrev -> bDest must have a higher minimum weight
                            // than every other edge into bDest
                            //
                            flowList*  edgeFromPrev = fgGetPredForBlock(bDest, bPrev);
                            noway_assert(edgeFromPrev != NULL);

                            // Examine all of the other edges into bDest
                            for (flowList* edge = bDest->bbPreds; edge != NULL; edge = edge->flNext)
                            {
                                if (edge != edgeFromPrev)
                                {
                                    if (edge->flEdgeWeightMax >= edgeFromPrev->flEdgeWeightMin)
                                    {
                                        moveDestUp = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //
                            // The block bPrev must have a higher weight
                            // than every other block that goes into bDest
                            //

                            // Examine all of the other edges into bDest
                            for (flowList* edge = bDest->bbPreds; edge != NULL; edge = edge->flNext)
                            {
                                BasicBlock* bTemp = edge->flBlock;

                                if ((bTemp != bPrev) && (bTemp->bbWeight >= bPrev->bbWeight))
                                {
                                    moveDestUp = false;
                                    break;
                                }
                            }
                        }

                        // Are we still good to move bDest up to bPrev?
                        if (moveDestUp)
                        {
                            //
                            // We will consider all blocks that have less weight than profHotWeight to be
                            // uncommonly run blocks as compared with the hot path of bPrev taken-jump to bDest
                            //
                            profHotWeight = bDest->bbWeight - 1;
                        }
                        else
                        {
                            if (block->isRunRarely())
                            {
                                // We will move any rarely run blocks blocks
                                profHotWeight = 0;
                            }
                            else
                            {
                                // We will move all blocks that have a weight less or equal to our fall through block
                                profHotWeight = block->bbWeight + 1;
                            }
                            // But we won't try to connect with bDest
                            bDest = NULL;
                        }
                    }
                }
                else // (bPrev->bbJumpKind == BBJ_COND)
                {
                    noway_assert(bPrev->bbJumpKind == BBJ_COND);
                    //
                    // We will reverse branch if the taken-jump to bDest ratio (i.e. 'takenRatio')
                    // is more than 51%
                    //
                    // We will setup profHotWeight to be maximum bbWeight that a block
                    // could have for us not to want to reverse the conditional branch
                    //
                    // We will consider all blocks that have less weight than profHotWeight to be
                    // uncommonly run blocks as compared with the hot path of bPrev taken-jump to bDest
                    //
                    if (fgHaveValidEdgeWeights)
                    {
                        // We have valid edge weights, however even with valid edge weights
                        // we may have a minimum and maximum range for each edges value
                        //
                        // We will check that the min weight of the bPrev to bDest edge
                        //  is more than twice the max weight of the bPrev to block edge.
                        //
                        //                  bPrev -->   [BB04, weight 31]
                        //                                     |         \
                        //          edgeToBlock -------------> O          \
                        //          [min=8,max=10]             V           \
                        //                  block -->   [BB05, weight 10]   \
                        //                                                   \
                        //          edgeToDest ----------------------------> O
                        //          [min=21,max=23]                          |
                        //                                                   V
                        //                  bDest --------------->   [BB08, weight 21]
                        //
                        flowList* edgeToDest  = fgGetPredForBlock(bDest, bPrev);
                        flowList* edgeToBlock = fgGetPredForBlock(block, bPrev);
                        noway_assert(edgeToDest != NULL);
                        noway_assert(edgeToBlock != NULL);
                        //
                        // Calculate the taken ratio
                        //   A takenRation of 0.10 means taken 10% of the time, not taken 90% of the time
                        //   A takenRation of 0.50 means taken 50% of the time, not taken 50% of the time
                        //   A takenRation of 0.90 means taken 90% of the time, not taken 10% of the time
                        //
                        double takenCount    = ((double) edgeToDest->flEdgeWeightMin  + (double) edgeToDest->flEdgeWeightMax)  / 2.0;
                        double notTakenCount = ((double) edgeToBlock->flEdgeWeightMin + (double) edgeToBlock->flEdgeWeightMax) / 2.0;
                        double totalCount    = takenCount + notTakenCount;
                        double takenRatio    = takenCount / totalCount;

                        // If the takenRatio is greater or equal to 51% then we will reverse the branch
                        if (takenRatio < 0.51)
                        {
                            reorderBlock = false;
                        }
                        else
                        {
                            // set profHotWeight
                            profHotWeight = (edgeToBlock->flEdgeWeightMin + edgeToBlock->flEdgeWeightMax)/2 - 1;
                        }
                    }
                    else
                    {
                        // We don't have valid edge weight so we will be more conservative
                        // We could have bPrev, block or bDest as part of a loop and thus have extra weight
                        //
                        // We will do two checks:
                        //   1. Check that the weight of bDest is at least two times more than block
                        //   2. Check that the weight of bPrev is at least three times more than block
                        //
                        //                  bPrev -->   [BB04, weight 31]
                        //                                     |         \
                        //                                     V          \
                        //                  block -->   [BB05, weight 10]  \
                        //                                                  \
                        //                                                  |
                        //                                                  V
                        //                  bDest --------------->   [BB08, weight 21]
                        //
                        //  For this case weightDest is calculated as (21+1)/2  or 11
                        //            and weightPrev is calculated as (31+2)/3  also 11
                        //
                        //  Generally both weightDest and weightPrev should calculate
                        //  the same value unless bPrev or bDest are part of a loop
                        //
                        BasicBlock::weight_t  weightDest = bDest->isMaxBBWeight() ? bDest->bbWeight : (bDest->bbWeight+1) / 2;
                        BasicBlock::weight_t  weightPrev = bPrev->isMaxBBWeight() ? bPrev->bbWeight : (bPrev->bbWeight+2) / 3;

                        // select the lower of weightDest and weightPrev
                        profHotWeight = (weightDest < weightPrev) ? weightDest : weightPrev;

                        // if the weight of block is greater (or equal) to profHotWeight then we don't reverse the cond
                        if (block->bbWeight >= profHotWeight)
                        {
                            reorderBlock = false;
                        }
                    }
                }
            }
            else // not a forwardBranch
            {
                if (bPrev->bbFallsThrough())
                {
                    goto CHECK_FOR_RARE;
                }

                // Here we should pull up the highest weight block remaining
                // and place it here since bPrev does not fall through.

                BasicBlock::weight_t highestWeight           = 0;
                BasicBlock*          candidateBlock          = NULL;
                BasicBlock*          lastNonFallThroughBlock = bPrev;
                BasicBlock*          bTmp                    = bPrev->bbNext;

                while (bTmp != NULL)
                {
                    // Don't try to split a Call/Always pair
                    //
                    if (bTmp->isBBCallAlwaysPair())
                    {
                        // Move bTmp forward
                        bTmp = bTmp->bbNext;
                    }

                    //
                    // Check for loop exit condition
                    //
                    if (bTmp == NULL)
                        break;

                    //
                    // if its weight is the highest one we've seen and
                    //  the EH regions allow for us to place bTmp after bPrev
                    //
                    if ((bTmp->bbWeight > highestWeight) && fgEhAllowsMoveBlock(bPrev, bTmp))
                    {
                        // When we have a current candidateBlock that is a conditional (or unconditional) jump
                        // to bTmp (which is a higher weighted block) then it is better to keep out current
                        // candidateBlock and have it fall into bTmp
                        //
                        if ((candidateBlock == NULL) ||
                            ((candidateBlock->bbJumpKind != BBJ_COND) && (candidateBlock->bbJumpKind != BBJ_ALWAYS)) ||
                            (candidateBlock->bbJumpDest != bTmp))
                        {
                            // otherwise we have a new candidateBlock
                            //
                            highestWeight  = bTmp->bbWeight;
                            candidateBlock = lastNonFallThroughBlock->bbNext;
                        }
                    }

                    if ((bTmp->bbFallsThrough() == false) || (bTmp->bbWeight == 0))
                    {
                        lastNonFallThroughBlock = bTmp;
                    }

                    bTmp = bTmp->bbNext;
                }

                // If we didn't find a suitable block then skip this
                if (highestWeight == 0)
                {
                    reorderBlock = false;
                }
                else
                {
                    noway_assert(candidateBlock != NULL);

                    // If the candidateBlock is the same a block then skip this
                    if (candidateBlock == block)
                    {
                        reorderBlock = false;
                    }
                    else
                    {
                        // Set bDest to the block that we want to come after bPrev
                        bDest = candidateBlock;

                        // set profHotWeight
                        profHotWeight = highestWeight-1;
                    }
                }
            }
        }
        else // we don't have good profile info (or we are falling through)
        {

CHECK_FOR_RARE:;

            /* We only want to reorder when we have a rarely run   */
            /* block right after a normal block,                   */
            /* (bPrev is known to be a normal block at this point) */
            if (!isRare)
            {
                reorderBlock = false;
            }
            else
            {
                /* If the jump target bDest is also a rarely run block then we don't want to do the reversal */
                if (bDest && bDest->isRunRarely())
                {
                    reorderBlock = false;   /* Both block and bDest are rarely run */
                }
                else
                {
                    // We will move any rarely run blocks blocks
                    profHotWeight = 0;
                }
            }
        }

        if (reorderBlock == false)
        {
            //
            // Check for an unconditional branch to a conditional branch
            // which also branches back to our next block
            //
            if (fgOptimizeBranch(bPrev))
            {
                noway_assert(bPrev->bbJumpKind == BBJ_COND);
            }
            continue;
        }

        //  Now we need to determine which blocks should be moved
        //
        //  We consider one of two choices:
        //
        //  1. Moving the fall-through blocks (or rarely run blocks) down to
        //     later in the method and hopefully connecting the jump dest block
        //     so that it becomes the fall through block
        //
        //  And when bDest in not NULL, we also consider:
        //
        //  2. Moving the bDest block (or blocks) up to bPrev
        //     so that it could be used as a fall through block
        //
        //  We will prefer option #1 if we are able to connect the jump dest
        //  block as the fall though block otherwise will we try to use option #2
        //

        //
        //  Consider option #1: relocating blocks starting at 'block'
        //    to later in flowgraph
        //
        // We set bStart to the first block that will be relocated
        // and bEnd to the last block that will be relocated

        BasicBlock*   bStart  = block;
        BasicBlock*   bEnd    = bStart;
                      bNext   = bEnd->bbNext;
        bool          connected_bDest = false;

        if ((backwardBranch && !isRare) ||
            ((block->bbFlags & BBF_DONT_REMOVE) != 0))   // Don't choose option #1 when block is the start of a try region
        {
            bStart = NULL;
            bEnd   = NULL;
        }
        else
        {
            while (true)
            {
                // Don't try to split a Call/Always pair
                //
                if (bEnd->isBBCallAlwaysPair())
                {
                    // Move bEnd and bNext forward
                    bEnd  = bNext;
                    bNext = bNext->bbNext;
                }

                //
                // Check for loop exit condition
                //
                if (bNext == NULL)
                    break;

#if FEATURE_EH_FUNCLETS
                // Check if we've reached the funclets region, at the end of the function
                if (fgFirstFuncletBB == bEnd->bbNext)
                    break;
#endif // FEATURE_EH_FUNCLETS

                if (bNext == bDest)
                {
                    connected_bDest = true;
                    break;
                }

                // All the blocks must have the same try index
                // and must not have the BBF_DONT_REMOVE flag set

                if ( !BasicBlock::sameTryRegion(bStart, bNext)   ||
                     ((bNext->bbFlags & BBF_DONT_REMOVE) != 0)     )
                {
                    // exit the loop, bEnd is now set to the
                    // last block that we want to relocate
                    break;
                }

                // If we are relocating rarely run blocks..
                if (isRare)
                {
                    // ... then all blocks must be rarely run
                    if (!bNext->isRunRarely())
                    {
                        // exit the loop, bEnd is now set to the
                        // last block that we want to relocate
                        break;
                    }
                }
                else
                {
                    // If we are moving blocks that are hot then all
                    // of the blocks moved must be less than profHotWeight */
                    if (bNext->bbWeight >= profHotWeight)
                    {
                        // exit the loop, bEnd is now set to the
                        // last block that we would relocate
                        break;
                    }
                }

                // Move bEnd and bNext forward
                bEnd  = bNext;
                bNext = bNext->bbNext;
            }

            // Set connected_bDest to true if moving blocks [bStart .. bEnd]
            //  connects with the the jump dest of bPrev (i.e bDest) and
            // thus allows bPrev fall through instead of jump.
            if (bNext == bDest)
            {
                connected_bDest = true;
            }
        }

        //  Now consider option #2: Moving the jump dest block (or blocks)
        //    up to bPrev
        //
        // The variables bStart2, bEnd2 and bPrev2 are used for option #2
        //
        // We will setup bStart2 to the first block that will be relocated
        // and bEnd2 to the last block that will be relocated
        // and bPrev2 to be the lexical pred of bDest
        //
        // If after this calculation bStart2 is NULL we cannot use option #2,
        // otherwise bStart2, bEnd2 and bPrev2 are all non-NULL and we will use option #2

        BasicBlock*   bStart2 = NULL;
        BasicBlock*   bEnd2   = NULL;
        BasicBlock*   bPrev2  = NULL;

        // If option #1 didn't connect bDest and bDest isn't NULL
        if ((connected_bDest == false) &&
            (bDest != NULL) &&
            //  The jump target cannot be moved if it has the BBF_DONT_REMOVE flag set
            ((bDest->bbFlags & BBF_DONT_REMOVE) == 0))
        {
            // We will consider option #2: relocating blocks starting at 'bDest' to succeed bPrev
            //
            // setup bPrev2 to be the lexical pred of bDest

            bPrev2 = block;
            while (bPrev2 != NULL)
            {
                if (bPrev2->bbNext == bDest)
                    break;

                bPrev2 = bPrev2->bbNext;
            }

            if ((bPrev2 != NULL) && fgEhAllowsMoveBlock(bPrev, bDest))
            {
                // We have decided that relocating bDest to be after bPrev is best
                // Set bStart2 to the first block that will be relocated
                // and bEnd2 to the last block that will be relocated
                //
                // Assigning to bStart2 selects option #2
                //
                bStart2 = bDest;
                bEnd2   = bStart2;
                bNext   = bEnd2->bbNext;

                while (true)
                {
                    // Don't try to split a Call/Always pair
                    //
                    if (bEnd2->isBBCallAlwaysPair())
                    {
                        noway_assert(bNext->bbJumpKind == BBJ_ALWAYS);
                        // Move bEnd2 and bNext forward
                        bEnd2 = bNext;
                        bNext = bNext->bbNext;
                    }

                    // Check for the Loop exit conditions

                    if (bNext == NULL)
                        break;

                    if (bEnd2->bbFallsThrough() == false)
                        break;

                    // If we are relocating rarely run blocks..
                    // All the blocks must have the same try index,
                    // and must not have the BBF_DONT_REMOVE flag set

                    if ( !BasicBlock::sameTryRegion(bStart2, bNext)   ||
                         ((bNext->bbFlags & BBF_DONT_REMOVE) != 0)      )
                    {
                        // exit the loop, bEnd2 is now set to the
                        // last block that we want to relocate
                        break;
                    }

                    if (isRare)
                    {
                        /* ... then all blocks must not be rarely run */
                        if (bNext->isRunRarely())
                        {
                            // exit the loop, bEnd2 is now set to the
                            // last block that we want to relocate
                            break;
                        }
                    }
                    else
                    {
                        // If we are relocating hot blocks
                        // all blocks moved must be greater than profHotWeight
                        if (bNext->bbWeight <= profHotWeight)
                        {
                            // exit the loop, bEnd2 is now set to the
                            // last block that we want to relocate
                            break;
                        }
                    }

                    // Move bEnd2 and bNext forward
                    bEnd2 = bNext;
                    bNext = bNext->bbNext;
                }
            }
        }

        // If we are using option #1 then ...
        if (bStart2 == NULL)
        {
            // Don't use option #1 for a backwards branch
            if (bStart == NULL)
                continue;

            // .... Don't move a set of blocks that are already at the end of the main method
            if (bEnd == fgLastBBInMainFunction())
                continue;
        }

#ifdef DEBUG
        if  (verbose)
        {
            if (bDest != NULL)
            {
                if (bPrev->bbJumpKind == BBJ_COND)
                {
                    printf("Decided to reverse conditional branch at block BB%02u branch to BB%02u ", bPrev->bbNum, bDest->bbNum);
                }
                else if (bPrev->bbJumpKind == BBJ_ALWAYS)
                {
                    printf("Decided to straighten unconditional branch at block BB%02u branch to BB%02u ", bPrev->bbNum, bDest->bbNum);
                }
                else
                {
                    printf("Decided to place hot code after BB%02u, placed BB%02u after this block ", bPrev->bbNum, bDest->bbNum);
                }

                if (profHotWeight > 0)
                {
                    printf("because of IBC profile data\n");
                }
                else
                {
                    if (bPrev->bbFallsThrough())
                    {
                        printf("since it falls into a rarely run block\n");
                    }
                    else
                    {
                        printf("since it is succeeded by a rarely run block\n");
                    }
                }
            }
            else
            {
                printf("Decided to relocate block(s) after block BB%02u since they are %s block(s)\n",
                    bPrev->bbNum, block->isRunRarely() ? "rarely run" : "uncommonly run");
            }
        }
#endif // DEBUG

        // We will set insertAfterBlk to the block the precedes our insertion range
        // We will set bStartPrev to be the block that precedes the set of blocks that we are moving
        BasicBlock* insertAfterBlk;
        BasicBlock* bStartPrev;

        if (bStart2 != NULL)
        {
            // Option #2: relocating blocks starting at 'bDest' to follow bPrev

            // Update bStart and bEnd so that we can use these two for all later operations
            bStart     = bStart2;
            bEnd       = bEnd2;

            // Set bStartPrev to be the block that comes before bStart
            bStartPrev = bPrev2;

            // We will move [bStart..bEnd] to immediately after bPrev
            insertAfterBlk = bPrev;
        }
        else
        {
            // option #1: Moving the fall-through blocks (or rarely run blocks) down to later in the method

            // Set bStartPrev to be the block that come before bStart
            bStartPrev = bPrev;

            // We will move [bStart..bEnd] but we will pick the insert location later
            insertAfterBlk = NULL;
        }

        // We are going to move [bStart..bEnd] so they can't be NULL
        noway_assert(bStart != NULL);
        noway_assert(bEnd   != NULL);

        // bEnd can't be a BBJ_CALLFINALLY unless it is a RETLESS call
        noway_assert((bEnd->bbJumpKind != BBJ_CALLFINALLY) || (bEnd->bbFlags & BBF_RETLESS_CALL));

        // bStartPrev must be set to the block that precedes bStart
        noway_assert(bStartPrev->bbNext == bStart);

        // Since we will be unlinking [bStart..bEnd],
        // we need to compute and remember if bStart is in each of
        // the try and handler regions
        //
        bool* fStartIsInTry = NULL;
        bool* fStartIsInHnd = NULL;

        if (compHndBBtabCount > 0)
        {
            fStartIsInTry = new (this, CMK_Unknown) bool[compHndBBtabCount];
            fStartIsInHnd = new (this, CMK_Unknown) bool[compHndBBtabCount];

            for (XTnum = 0, HBtab = compHndBBtab;
                 XTnum < compHndBBtabCount;
                 XTnum++  , HBtab++)
            {
                fStartIsInTry[XTnum] = HBtab->InTryRegionBBRange(bStart);
                fStartIsInHnd[XTnum] = HBtab->InHndRegionBBRange(bStart);
            }
        }

        /* Temporarily unlink [bStart..bEnd] from the flow graph */
        fgUnlinkRange(bStart, bEnd);

        if (insertAfterBlk == NULL)
        {
            // Find new location for the unlinked block(s)
            // Set insertAfterBlk to the block which will precede the insertion point

            if (!bStart->hasTryIndex() && isRare)
            {
                // We'll just insert the blocks at the end of the method. If the method
                // has funclets, we will insert at the end of the main method but before
                // any of the funclets. Note that we create funclets before we call
                // fgReorderBlocks().

                insertAfterBlk = fgLastBBInMainFunction();
                noway_assert(insertAfterBlk != bPrev);
            }
            else
            {
                BasicBlock* startBlk;
                BasicBlock* lastBlk;
                EHblkDsc* ehDsc = ehInitTryBlockRange(bStart, &startBlk, &lastBlk);

                BasicBlock* endBlk;

                /* Setup startBlk and endBlk as the range to search */

                if (ehDsc != nullptr)
                {
                    endBlk = lastBlk->bbNext;

                    /*
                       Multiple (nested) try regions might start from the same BB.
                       For example,

                       try3   try2   try1
                       |---   |---   |---   BB01
                       |      |      |      BB02
                       |      |      |---   BB03
                       |      |             BB04
                       |      |------------ BB05
                       |                    BB06
                       |------------------- BB07

                       Now if we want to insert in try2 region, we will start with startBlk=BB01.
                       The following loop will allow us to start from startBlk==BB04.
                    */
                    while (!BasicBlock::sameTryRegion(startBlk, bStart) && (startBlk != endBlk))
                    {
                        startBlk = startBlk->bbNext;
                    }

                    // startBlk cannot equal endBlk as it must come before endBlk
                    if (startBlk == endBlk)
                        goto CANNOT_MOVE;

                    // we also can't start searching the try region at bStart
                    if (startBlk == bStart)
                    {
                        // if bEnd is the last block in the method or
                        // or if bEnd->bbNext is in a different try region
                        // then we cannot move the blocks
                        //
                        if ((bEnd->bbNext == NULL) || !BasicBlock::sameTryRegion(startBlk, bEnd->bbNext))
                            goto CANNOT_MOVE;

                        startBlk = bEnd->bbNext;

                        // Check that the new startBlk still comes before endBlk

                        // startBlk cannot equal endBlk as it must come before endBlk
                        if (startBlk == endBlk)
                            goto CANNOT_MOVE;

                        BasicBlock* tmpBlk = startBlk;
                        while ((tmpBlk != endBlk) && (tmpBlk != NULL))
                        {
                            tmpBlk = tmpBlk->bbNext;
                        }

                        // when tmpBlk is NULL that means startBlk is after endBlk
                        // so there is no way to move bStart..bEnd within the try region
                        if (tmpBlk == NULL)
                            goto CANNOT_MOVE;
                    }
                }
                else
                {
                    noway_assert(isRare == false);

                    /* We'll search through the entire main method */
                    startBlk = fgFirstBB;
                    endBlk   = fgEndBBAfterMainFunction();
                }

                // Calculate nearBlk and jumpBlk and then call fgFindInsertPoint()
                // to find our insertion block
                //
                {
                    // If the set of blocks that we are moving ends with a BBJ_ALWAYS to
                    // another [rarely run] block that comes after bPrev (forward branch)
                    // then we can set up nearBlk to eliminate this jump sometimes
                    //
                    BasicBlock* nearBlk = NULL;
                    BasicBlock* jumpBlk = NULL;

                    if ((bEnd->bbJumpKind == BBJ_ALWAYS) &&
                        (!isRare || bEnd->bbJumpDest->isRunRarely()) &&
                        fgIsForwardBranch(bEnd, bPrev))
                    {
                        // Set nearBlk to be the block in [startBlk..endBlk]
                        // such that nearBlk->bbNext == bEnd->JumpDest
                        // if no such block exists then set nearBlk to NULL
                        nearBlk = startBlk;
                        jumpBlk = bEnd;
                        do
                        {
                            // We do not want to set nearBlk to bPrev
                            // since then we will not move [bStart..bEnd]
                            //
                            if (nearBlk != bPrev)
                            {
                                // Check if nearBlk satisfies our requirement
                                if (nearBlk->bbNext == bEnd->bbJumpDest)
                                    break;
                            }

                            // Did we reach the endBlk?
                            if (nearBlk == endBlk)
                            {
                                nearBlk = NULL;
                                break;
                            }

                            // advance nearBlk to the next block
                            nearBlk = nearBlk->bbNext;

                        } while (nearBlk != NULL);
                    }

                    // if nearBlk is NULL then we set nearBlk to be the
                    // first block that we want to insert after.
                    if (nearBlk == NULL)
                    {
                        if (bDest != NULL)
                        {
                            // we want to insert after bDest
                            nearBlk = bDest;
                        }
                        else
                        {
                            // we want to insert after bPrev
                            nearBlk = bPrev;
                        }
                    }

                    /* Set insertAfterBlk to the block which we will insert after. */

                    insertAfterBlk = fgFindInsertPoint(bStart->bbTryIndex,
                                                       true,  // Insert in the try region.
                                                       startBlk, endBlk,
                                                       nearBlk, jumpBlk,
                                                       bStart->bbWeight == BB_ZERO_WEIGHT);
                }

                /* See if insertAfterBlk is the same as where we started, */
                /*  or if we could not find any insertion point     */

                if ((insertAfterBlk == bPrev) ||
                    (insertAfterBlk == NULL))
                {
CANNOT_MOVE:;
                    /* We couldn't move the blocks, so put everything back */
                    /* relink [bStart .. bEnd] into the flow graph */

                    bPrev->setNext(bStart);
                    if (bEnd->bbNext)
                        bEnd->bbNext->bbPrev = bEnd;
#ifdef DEBUG
                    if  (verbose)
                    {
                        if (bStart != bEnd)
                        {
                            printf("Could not relocate blocks (BB%02u .. BB%02u)\n",
                                   bStart->bbNum, bEnd->bbNum);
                        }
                        else
                        {
                            printf("Could not relocate block BB%02u\n",
                                   bStart->bbNum);
                        }
                    }
#endif // DEBUG
                    continue;
                }
            }
        }

        noway_assert(insertAfterBlk != NULL);
        noway_assert(bStartPrev != NULL);
        noway_assert(bStartPrev != insertAfterBlk);

#ifdef DEBUG
        movedBlocks = true;

        if  (verbose)
        {
            const char * msg;
            if (bStart2 != NULL)
            {
                msg = "hot";
            }
            else
            {
                if (isRare)
                {
                    msg = "rarely run";
                }
                else
                {
                    msg = "uncommon";
                }
            }

            printf("Relocated %s ", msg);
            if (bStart != bEnd)
            {
                printf("blocks (BB%02u .. BB%02u)",
                       bStart->bbNum, bEnd->bbNum);
            }
            else
            {
                printf("block BB%02u",
                       bStart->bbNum);
            }

            if (bPrev->bbJumpKind == BBJ_COND)
            {
                printf(" by reversing conditional jump at BB%02u\n", bPrev->bbNum);
            }
            else
            {
                printf("\n", bPrev->bbNum);
            }
        }
#endif // DEBUG

        if (bPrev->bbJumpKind == BBJ_COND)
        {
            /* Reverse the bPrev jump condition */
            GenTree* condTest = bPrev->lastStmt();

            condTest = condTest->gtStmt.gtStmtExpr;
            noway_assert(condTest->gtOper == GT_JTRUE);

            condTest->gtOp.gtOp1 = gtReverseCond(condTest->gtOp.gtOp1);

            if (bStart2 == NULL)
            {
                /* Set the new jump dest for bPrev to the rarely run or uncommon block(s) */
                bPrev->bbJumpDest  = bStart;
                bStart->bbFlags   |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
            }
            else
            {
                noway_assert(insertAfterBlk == bPrev);
                noway_assert(insertAfterBlk->bbNext == block);

                /* Set the new jump dest for bPrev to the rarely run or uncommon block(s) */
                bPrev->bbJumpDest  = block;
                block->bbFlags    |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
            }
        }

        // If we are moving blocks that are at the end of a try or handler
        // we will need to shorten ebdTryLast or ebdHndLast
        //
        ehUpdateLastBlocks(bEnd, bStartPrev);

        // If we are moving blocks into the end of a try region or handler region
        // we will need to extend ebdTryLast or ebdHndLast so the blocks that we
        // are moving are part of this try or handler region.
        //
        for (XTnum = 0, HBtab = compHndBBtab;
             XTnum < compHndBBtabCount;
             XTnum++  , HBtab++)
        {
            // Are we moving blocks to the end of a try region?
            if (HBtab->ebdTryLast == insertAfterBlk)
            {
                if  (fStartIsInTry[XTnum])
                {
                    // bStart..bEnd is in the try, so extend the try region
                    fgSetTryEnd(HBtab, bEnd);
                }
            }

            // Are we moving blocks to the end of a handler region?
            if (HBtab->ebdHndLast == insertAfterBlk)
            {
                if (fStartIsInHnd[XTnum])
                {
                    // bStart..bEnd is in the handler, so extend the handler region
                    fgSetHndEnd(HBtab, bEnd);
                }
            }
        }

        /* We have decided to insert the block(s) after 'insertAfterBlk' */
        fgMoveBlocksAfter(bStart, bEnd, insertAfterBlk);

        if (bDest)
        {
            /* We may need to insert an unconditional branch after bPrev to bDest */
            fgConnectFallThrough(bPrev, bDest);
        }
        else
        {
            /* If bPrev falls through, we must insert a jump to block */
            fgConnectFallThrough(bPrev, block);
        }

        BasicBlock* bSkip = bEnd->bbNext;

        /* If bEnd falls through, we must insert a jump to bNext */
        fgConnectFallThrough(bEnd, bNext);

        if (bStart2 == NULL)
        {
            /* If insertAfterBlk falls through, we are forced to     */
            /* add a jump around the block(s) we just inserted */
            fgConnectFallThrough(insertAfterBlk, bSkip);
        }
        else
        {
            /* We may need to insert an unconditional branch after bPrev2 to bStart */
            fgConnectFallThrough(bPrev2, bStart);
        }

#if DEBUG
        if (verbose)
        {
            printf("\nAfter this change in fgReorderBlocks");
            fgDispBasicBlocks(verboseTrees);
            printf("\n");
        }
        fgVerifyHandlerTab();

        // Make sure that the predecessor lists are accurate
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
#endif // DEBUG

        // Set our iteration point 'block' to be the new bPrev->bbNext
        //  It will be used as the next bPrev
        block = bPrev->bbNext;

    } // end of for loop(bPrev,block)

    bool changed = movedBlocks || newRarelyRun || optimizedSwitches;

    if (changed)
    {
        fgNeedsUpdateFlowGraph = true;
#if DEBUG
        // Make sure that the predecessor lists are accurate
        if (expensiveDebugCheckLevel >= 2)
        {
            fgDebugCheckBBlist();
        }
#endif // DEBUG
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*-------------------------------------------------------------------------
 *
 * Walk the basic blocks list to determine the first block to place in the
 * cold section.  This would be the first of a series of rarely executed blocks
 * such that no succeeding blocks are in a try region or an exception handler
 * or are rarely executed.
 */

void                Compiler::fgDetermineFirstColdBlock()
{
#ifdef DEBUG
    if  (verbose)
        printf("\n*************** In fgDetermineFirstColdBlock()\n");
#endif // DEBUG

    // Since we may need to create a new transistion block
    // we assert that it is OK to create new blocks.
    //
    assert(fgSafeBasicBlockCreation);

    fgFirstColdBlock = NULL;

#if FEATURE_STACK_FP_X87
    if (compMayHaveTransitionBlocks)
    {
        opts.compProcedureSplitting = false;

        // See comment above declaration of compMayHaveTransitionBlocks for comments on this
        JITDUMP("Turning off procedure splitting for this method, as it may end up having FP transition blocks\n");
    }
#endif // FEATURE_STACK_FP_X87

    if (!opts.compProcedureSplitting)
    {
        JITDUMP("No procedure splitting will be done for this method\n");
        return;
    }

#ifdef DEBUG
    if ((compHndBBtabCount > 0) && !opts.compProcedureSplittingEH)
    {
        JITDUMP("No procedure splitting will be done for this method with EH (by request)\n");
        return;
    }
#endif // DEBUG

#if FEATURE_EH_FUNCLETS
    // TODO-CQ: handle hot/cold splitting in functions with EH (including synchronized methods
    // that create EH in methods without explicit EH clauses).

    if (compHndBBtabCount > 0)
    {
        JITDUMP("No procedure splitting will be done for this method with EH (implementation limitation)\n");
        return;
    }
#endif // FEATURE_EH_FUNCLETS

    BasicBlock* firstColdBlock       = NULL;
    BasicBlock* prevToFirstColdBlock = NULL;
    BasicBlock* block;
    BasicBlock* lblk;

    for (lblk = NULL,  block = fgFirstBB;
                       block != NULL;
         lblk = block, block = block->bbNext)
    {
        bool blockMustBeInHotSection = false;

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
        if (bbIsHandlerBeg(block))
        {
            blockMustBeInHotSection = true;
        }
#endif // HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION

        // Do we have a candidate for the first cold block?
        if (firstColdBlock != NULL)
        {
            // We have a candidate for first cold block

            // Is this a hot block?
            if (blockMustBeInHotSection || (block->isRunRarely() == false))
            {
                // We have to restart the search for the first cold block
                firstColdBlock       = NULL;
                prevToFirstColdBlock = NULL;
            }
        }
        else // (firstColdBlock == NULL)
        {
            // We don't have a candidate for first cold block

            // Is this a cold block?
            if (!blockMustBeInHotSection && (block->isRunRarely() == true))
            {
                //
                // If the last block that was hot was a BBJ_COND
                // then we will have to add an unconditional jump
                // so the code size for block needs be large
                // enough to make it worth our while
                //
                if ((lblk == NULL)                   ||
                    (lblk->bbJumpKind != BBJ_COND)   ||
                    (fgGetCodeEstimate(block) >= 8)     )
                {
                    // This block is now a candidate for first cold block
                    // Also remember the predecessor to this block
                    firstColdBlock       = block;
                    prevToFirstColdBlock = lblk;
                }
            }
        }
    }

    if (firstColdBlock == fgFirstBB)
    {
        // If the first block is Cold then we can't move any blocks
        // into the cold section

        firstColdBlock = NULL;
    }

    if (firstColdBlock != NULL)
    {
        noway_assert(prevToFirstColdBlock != NULL);

        if (prevToFirstColdBlock == NULL)
            return; // To keep Prefast happy

        // If we only have one cold block
        // then it may not be worth it to move it
        // into the Cold section as a jump to the
        // Cold section is 5 bytes in size.
        //
        if (firstColdBlock->bbNext == NULL)
        {
            // If the size of the cold block is 7 or less
            // then we will keep it in the Hot section.
            //
            if (fgGetCodeEstimate(firstColdBlock) < 8)
            {
                firstColdBlock = NULL;
                goto EXIT;
            }
        }

        // When the last Hot block fall through into the Cold section
        // we may need to add a jump
        //
        if (prevToFirstColdBlock->bbFallsThrough())
        {
            switch (prevToFirstColdBlock->bbJumpKind)
            {
            default:
                noway_assert(!"Unhandled jumpkind in fgDetermineFirstColdBlock()");

            case BBJ_CALLFINALLY:
                // A BBJ_CALLFINALLY that falls through is always followed
                // by an empty BBJ_ALWAYS.
                //
                assert(prevToFirstColdBlock->isBBCallAlwaysPair());
                firstColdBlock = firstColdBlock->bbNext; // Note that this assignment could make firstColdBlock == nullptr
                break;

            case BBJ_COND:
                //
                // This is a slightly more complicated case, because we will
                // probably need to insert a block to jump to the cold section.
                //
                if (firstColdBlock->isEmpty() &&
                    (firstColdBlock->bbJumpKind == BBJ_ALWAYS))
                {
                    // We can just use this block as the transitionBlock
                    firstColdBlock = firstColdBlock->bbNext;
                    // Note that this assignment could make firstColdBlock == NULL
                }
                else
                {
                    BasicBlock* transitionBlock = fgNewBBafter(BBJ_ALWAYS, prevToFirstColdBlock, true);
                    transitionBlock->bbJumpDest = firstColdBlock;
                    transitionBlock->inheritWeight(firstColdBlock);

                    noway_assert(fgComputePredsDone);

                    // Update the predecessor list for firstColdBlock
                    fgReplacePred(firstColdBlock, prevToFirstColdBlock, transitionBlock);

                    // Add prevToFirstColdBlock as a predecessor for transitionBlock
                    fgAddRefPred(transitionBlock, prevToFirstColdBlock);
                }
                break;

            case BBJ_NONE:
                // If the block preceding the first cold block is BBJ_NONE,
                // convert it to BBJ_ALWAYS to force an explicit jump.

                prevToFirstColdBlock->bbJumpDest = firstColdBlock;
                prevToFirstColdBlock->bbJumpKind = BBJ_ALWAYS;
                break;
            }
        }
    }

    if (firstColdBlock != NULL)
    {
        firstColdBlock->bbFlags |= BBF_JMP_TARGET;

        for (block = firstColdBlock; block; block = block->bbNext)
        {
            block->bbFlags |= BBF_COLD;
        }
    }

EXIT:;

#ifdef DEBUG
    if (verbose)
    {
        if (firstColdBlock)
        {
            printf("fgFirstColdBlock is BB%02u.\n", firstColdBlock->bbNum);
        }
        else
        {
            printf("fgFirstColdBlock is NULL.\n");
        }

        fgDispBasicBlocks();
    }

    fgVerifyHandlerTab();
#endif // DEBUG

    fgFirstColdBlock = firstColdBlock;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
/*****************************************************************************
 *
 *  Function called to "comb" the basic block list.
 *  Removes any empty blocks, unreachable blocks and redundant jumps.
 *  Most of those appear after dead store removal and folding of conditionals.
 * 
 *  Returns: true if the flowgraph has been modified
 *
 *  It also compacts basic blocks
 *   (consecutive basic blocks that should in fact be one).
 *
 *  NOTE:
 *    Debuggable code and Min Optimization JIT also introduces basic blocks
 *    but we do not optimize those!
 */

bool                Compiler::fgUpdateFlowGraph(bool doTailDuplication)
{
#ifdef DEBUG
    if  (verbose)
        printf("\n*************** In fgUpdateFlowGraph()");
#endif // DEBUG

    /* This should never be called for debuggable code */

    noway_assert(!opts.MinOpts() && !opts.compDbgCode);

#ifdef DEBUG
    if  (verbose)
    {
        printf("\nBefore updating the flow graph:\n");
        fgDispBasicBlocks(verboseTrees);
        printf("\n");
    }
#endif // DEBUG

    /* Walk all the basic blocks - look for unconditional jumps, empty blocks, blocks to compact, etc...
     *
     * OBSERVATION:
     *      Once a block is removed the predecessors are not accurate (assuming they were at the beginning)
     *      For now we will only use the information in bbRefs because it is easier to be updated
     */

    bool modified = false;
    bool change;
    do
    {
        change = false;

        BasicBlock*   block;              // the current block
        BasicBlock*   bPrev = NULL;       // the previous non-worthless block
        BasicBlock*   bNext;              // the successor of the current block
        BasicBlock*   bDest;              // the jump target of the current block

        for (block = fgFirstBB;
             block != NULL;
             block = block->bbNext)
        {
            /*  Some blocks may be already marked removed by other optimizations
             *  (e.g worthless loop removal), without being explicitly removed
             *  from the list.
             */

            if (block->bbFlags & BBF_REMOVED)
            {
                if (bPrev)
                {
                    bPrev->setNext(block->bbNext);
                }
                else
                {
                    /* WEIRD first basic block is removed - should have an assert here */
                    noway_assert(!"First basic block marked as BBF_REMOVED???");

                    fgFirstBB = block->bbNext;
                }
                continue;
            }

            /*  We jump to the REPEAT label if we performed a change involving the current block
             *  This is in case there are other optimizations that can show up
             *  (e.g. - compact 3 blocks in a row)
             *  If nothing happens, we then finish the iteration and move to the next block
             */

REPEAT:;

            bNext = block->bbNext;
            bDest = NULL;

            if (block->bbJumpKind == BBJ_ALWAYS)
            {
                bDest = block->bbJumpDest;
                if (doTailDuplication && fgOptimizeUncondBranchToSimpleCond(block, bDest))
                {
                    change = true;
                    modified = true;
                    bDest = block->bbJumpDest;
                    bNext = block->bbNext;
                }
            }

            // Remove JUMPS to the following block
            // and optimize any JUMPS to JUMPS

            if (block->bbJumpKind == BBJ_COND   ||
                block->bbJumpKind == BBJ_ALWAYS  )
            {
                bDest = block->bbJumpDest;
                if (bDest == bNext)
                {
                    if (fgOptimizeBranchToNext(block, bNext, bPrev))
                    {
                        change = true;
                        modified = true;
                        bDest = NULL;
                    }
                }
            }

            if (bDest != NULL)
            {
                // Do we have a JUMP to an empty unconditional JUMP block?
                if (bDest->isEmpty() &&
                    (bDest->bbJumpKind == BBJ_ALWAYS) &&
                    (bDest != bDest->bbJumpDest))         // special case for self jumps
                {
                    if (fgOptimizeBranchToEmptyUnconditional(block, bDest))
                    {
                        change = true;
                        modified = true;
                        goto REPEAT;
                    }
                }

                // Check for a conditional branch that just skips over an empty BBJ_ALWAYS block

                if ((block->bbJumpKind == BBJ_COND)   &&     // block is a BBJ_COND block
                    (bNext             != NULL)       &&     // block is not the last block
                    (bNext->bbRefs     == 1)          &&     // No other block jumps to bNext
                    (bNext->bbNext     == bDest)      &&     // The block after bNext is the BBJ_COND jump dest
                    (bNext->bbJumpKind == BBJ_ALWAYS) &&     // The next block is a BBJ_ALWAYS block
                    bNext->isEmpty()                  &&     // and it is an an empty block
                    (bNext != bNext->bbJumpDest)      &&     // special case for self jumps
                    (bDest != fgFirstColdBlock))
                {
                    bool optimizeJump = true;

                    // We do not optimize jumps between two different try regions.
                    // However jumping to a block that is not in any try region is OK
                    //
                    if (bDest->hasTryIndex() && !BasicBlock::sameTryRegion(block, bDest))
                    {
                        optimizeJump = false;
                    }

                    // Also consider bNext's try region
                    //
                    if (bNext->hasTryIndex() && !BasicBlock::sameTryRegion(block, bNext))
                    {
                        optimizeJump = false;
                    }

                    // If we are optimizing using real profile weights
                    // then don't optimize a conditional jump to an unconditional jump
                    // until after we have computed the edge weights
                    //
                    if (fgIsUsingProfileWeights())
                    {
                        // if block and bdest are in different hot/cold regions we can't do this this optimization
                        // because we can't allow fall-through into the cold region.  
                        if (!fgEdgeWeightsComputed || fgInDifferentRegions(block, bDest))
                        {
                            fgNeedsUpdateFlowGraph = true;
                            optimizeJump = false;
                        }
                    }

                    if (optimizeJump)
                    {
#ifdef DEBUG
                        if  (verbose)
                        {
                            printf("\nReversing a conditional jump around an unconditional jump (BB%02u -> BB%02u -> BB%02u)\n",
                                block->bbNum, bDest->bbNum, bNext->bbJumpDest->bbNum);
                        }
#endif // DEBUG
                        /* Reverse the jump condition */

                        GenTreePtr test = block->lastTopLevelStmt();

                        test = test->gtStmt.gtStmtExpr;
                        noway_assert(test->gtOper == GT_JTRUE);

                        test->gtOp.gtOp1 = gtReverseCond(test->gtOp.gtOp1);

                        // Optimize the Conditional JUMP to go to the new target
                        block->bbJumpDest = bNext->bbJumpDest;

                        fgAddRefPred(bNext->bbJumpDest, block, fgRemoveRefPred(bNext->bbJumpDest, bNext));

                        /*
                          Unlink bNext from the BasicBlock list; note that we can
                          do this even though other blocks could jump to it - the
                          reason is that elsewhere in this function we always
                          redirect jumps to jumps to jump to the final label,
                          so even if another block jumps to bNext it won't matter
                          once we're done since any such jump will be redirected
                          to the final target by the time we're done here.
                        */

                        fgRemoveRefPred(bNext, block);
                        fgUnlinkBlock(bNext);

                        /* Mark the block as removed */
                        bNext->bbFlags |= BBF_REMOVED;

                        // If this is the first Cold basic block update fgFirstColdBlock
                        if (bNext == fgFirstColdBlock)
                        {
                            fgFirstColdBlock = bNext->bbNext;
                        }

                        //
                        // If we removed the end of a try region or handler region
                        // we will need to update ebdTryLast or ebdHndLast.
                        //

                        EHblkDsc*   HBtab;
                        EHblkDsc*   HBtabEnd;

                        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
                             HBtab < HBtabEnd;
                             HBtab++)
                        {
                            if ((HBtab->ebdTryLast == bNext) ||
                                (HBtab->ebdHndLast == bNext))
                            {
                                fgSkipRmvdBlocks(HBtab);
                            }
                        }

                        // we optimized this JUMP - goto REPEAT to catch similar cases
                        change   = true;
                        modified = true;

#ifdef DEBUG
                        if  (verbose)
                        {
                            printf("\nAfter reversing the jump:\n");
                            fgDispBasicBlocks(verboseTrees);
                        }
#endif // DEBUG

                        /*
                           For a rare special case we cannot jump to REPEAT
                           as jumping to REPEAT will cause us to delete 'block'
                           because it currently appears to be unreachable.  As
                           it is a self loop that only has a single bbRef (itself)
                           However since the unlinked bNext has additional bbRefs
                           (that we will later connect to 'block'), it is not really
                           unreachable.
                        */
                        if ((bNext->bbRefs > 0) && (bNext->bbJumpDest == block) && (block->bbRefs == 1))
                            continue;

                        goto REPEAT;
                    }
                }
            }

            //
            // Update the switch jump table such that it follows jumps to jumps:
            //
            if (block->bbJumpKind == BBJ_SWITCH)
            {
                if (fgOptimizeSwitchBranches(block))
                {
                    change   = true;
                    modified = true;
                    goto REPEAT;
                }
            }

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            /* COMPACT blocks if possible */

            if (fgCanCompactBlocks(block, bNext))
            {
                fgCompactBlocks(block, bNext);

                /* we compacted two blocks - goto REPEAT to catch similar cases */
                change   = true;
                modified = true;
                goto REPEAT;
            }

            /* Remove unreachable or empty blocks - do not consider blocks marked BBF_DONT_REMOVE or genReturnBB block
             * These include first and last block of a TRY, exception handlers and RANGE_CHECK_FAIL THROW blocks */

            if ((block->bbFlags & BBF_DONT_REMOVE) == BBF_DONT_REMOVE ||
                block == genReturnBB
               )
            {
                bPrev = block;
                continue;
            }

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
            // Don't remove the BBJ_ALWAYS block of a BBJ_CALLFINALLY/BBJ_ALWAYS pair.
            if (block->countOfInEdges() == 0 && bPrev->bbJumpKind == BBJ_CALLFINALLY)
            {
                assert(bPrev->isBBCallAlwaysPair());
                noway_assert(!(bPrev->bbFlags & BBF_RETLESS_CALL));
                noway_assert(block->bbJumpKind == BBJ_ALWAYS);
                bPrev = block;
                continue;
            }
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

            noway_assert(!block->bbCatchTyp);
            noway_assert(!(block->bbFlags & BBF_TRY_BEG));

            /* Remove unreachable blocks
             *
             * We'll look for blocks that have countOfInEdges() = 0 (blocks may become
             * unreachable due to a BBJ_ALWAYS introduced by conditional folding for example)
             */

            if (block->countOfInEdges() == 0)
            {
                /* no references -> unreachable - remove it */
                /* For now do not update the bbNum, do it at the end */

                fgRemoveBlock(block, true);

                change   = true;
                modified = true;

                /* we removed the current block - the rest of the optimizations won't have a target
                 * continue with the next one */

                continue;
            }
            else if (block->countOfInEdges() == 1)
            {
                switch (block->bbJumpKind)
                {
                case BBJ_COND:
                case BBJ_ALWAYS:
                    if (block->bbJumpDest == block)
                    {
                        fgRemoveBlock(block, true);

                        change   = true;
                        modified = true;

                        /* we removed the current block - the rest of the optimizations
                         * won't have a target so continue with the next block */

                        continue;
                    }
                    break;

                default:
                    break;
                }
            }

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            /* Remove EMPTY blocks */

            if (block->isEmpty())
            {
                assert(bPrev == block->bbPrev);
                if (fgOptimizeEmptyBlock(block))
                {
                    change   = true;
                    modified = true;
                }

                /* Have we removed the block? */

                if  (block->bbFlags & BBF_REMOVED)
                {
                    /* block was removed - no change to bPrev */
                    continue;
                }
            }

            /* Set the predecessor of the last reachable block
             * If we removed the current block, the predecessor remains unchanged
             * otherwise, since the current block is ok, it becomes the predecessor */

            noway_assert(!(block->bbFlags & BBF_REMOVED));

            bPrev = block;
        }
    }
    while (change);

    fgNeedsUpdateFlowGraph = false;

#ifdef DEBUG
    if  (verbose && modified)
    {
        printf("\nAfter updating the flow graph:\n");
        fgDispBasicBlocks(verboseTrees);
        fgDispHandlerTab();
    }
    fgVerifyHandlerTab();
    // Make sure that the predecessor lists are accurate
    fgDebugCheckBBlist();
    fgDebugCheckUpdate();
#endif // DEBUG

    return modified;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *  Check that the flow graph is really updated
 */

#ifdef DEBUG

void            Compiler::fgDebugCheckUpdate()
{
    if (!compStressCompile(STRESS_CHK_FLOW_UPDATE, 30))
       return;

    /* We check for these conditions:
     * no unreachable blocks  -> no blocks have countOfInEdges() = 0
     * no empty blocks        -> no blocks have bbTreeList = 0
     * no un-imported blocks  -> no blocks have BBF_IMPORTED not set (this is
     *                           kind of redundand with the above, but to make sure)
     * no un-compacted blocks -> BBJ_NONE followed by block with no jumps to it (countOfInEdges() = 1)
     */

    BasicBlock* prev;
    BasicBlock* block;
    for (prev = NULL , block = fgFirstBB;
                       block != NULL;
         prev = block, block = block->bbNext)
    {
        /* no unreachable blocks */

        if  ((block->countOfInEdges() == 0)     &&
             !(block->bbFlags & BBF_DONT_REMOVE)
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
             // With funclets, we never get rid of the BBJ_ALWAYS part of a BBJ_CALLFINALLY/BBJ_ALWAYS pair,
             // even if we can prove that the finally block never returns.
             && (prev == NULL || block->bbJumpKind != BBJ_ALWAYS || !prev->isBBCallAlwaysPair())
#endif // FEATURE_EH_FUNCLETS
             )
        {
            noway_assert(!"Unreachable block not removed!");
        }

        /* no empty blocks */

        if  (block->isEmpty() &&
             !(block->bbFlags & BBF_DONT_REMOVE))
        {
            switch (block->bbJumpKind)
            {
            case BBJ_CALLFINALLY:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
            case BBJ_RETURN:
                /* for BBJ_ALWAYS is probably just a GOTO, but will have to be treated */
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                /* These jump kinds are allowed to have empty tree lists */
                break;

            default:
                /* it may be the case that the block had more than one reference to it
                 * so we couldn't remove it */

                if (block->countOfInEdges() == 0)
                    noway_assert(!"Empty block not removed!");
                break;
            }
        }

        /* no un-imported blocks */

        if  (!(block->bbFlags & BBF_IMPORTED))
        {
            /* internal blocks do not count */

            if (!(block->bbFlags & BBF_INTERNAL))
                noway_assert(!"Non IMPORTED block not removed!");
        }

        bool prevIsCallAlwaysPair = ((prev != NULL) && prev->isBBCallAlwaysPair());

        // Check for an unnecessary jumps to the next block
        bool doAssertOnJumpToNextBlock = false;   // unless we have a BBJ_COND or BBJ_ALWAYS we can not assert

        if (block->bbJumpKind == BBJ_COND)
        {
            // A conditional branch should never jump to the next block
            // as it can be folded into a BBJ_NONE;
            doAssertOnJumpToNextBlock = true;
        }
        else if (block->bbJumpKind == BBJ_ALWAYS)
        {
            // Generally we will want to assert if a BBJ_ALWAYS branches to the next block
            doAssertOnJumpToNextBlock = true;

            // If the BBF_KEEP_BBJ_ALWAYS flag is set we allow it to jump to the next block
            if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
            {
                doAssertOnJumpToNextBlock = false;
            }

            // A call/always pair is also allowed to jump to the next block
            if (prevIsCallAlwaysPair)
            {
                doAssertOnJumpToNextBlock = false;
            }

            // We are allowed to have a branch from a hot 'block' to a cold 'bbNext'
            //
            if ((block->bbNext != NULL) && fgInDifferentRegions(block, block->bbNext))
            {
                doAssertOnJumpToNextBlock = false;
            }
        }

        if (doAssertOnJumpToNextBlock)
        {
            if (block->bbJumpDest == block->bbNext)
            {
                noway_assert(!"Unnecessary jump to the next block!");
            }
        }

        /* Make sure BBF_KEEP_BBJ_ALWAYS is set correctly */

        if ((block->bbJumpKind == BBJ_ALWAYS) && prevIsCallAlwaysPair)
        {
            noway_assert(block->bbFlags & BBF_KEEP_BBJ_ALWAYS);
        }

        /* For a BBJ_CALLFINALLY block we make sure that we are followed by */
        /* an BBJ_ALWAYS block with BBF_INTERNAL set */
        /* or that it's a BBF_RETLESS_CALL */
        if (block->bbJumpKind == BBJ_CALLFINALLY)
        {
            assert((block->bbFlags & BBF_RETLESS_CALL) || block->isBBCallAlwaysPair());
        }

        /* no un-compacted blocks */

        if (fgCanCompactBlocks(block, block->bbNext))
        {
            noway_assert(!"Found un-compacted blocks!");
        }
    }
}

#endif // DEBUG

/*****************************************************************************
 * We've inserted a new block before 'block' that should be part of the same EH region as 'block'.
 * Update the EH table to make this so. Also, set the new block to have the right EH region data
 * (copy the bbTryIndex, bbHndIndex, and bbCatchTyp from 'block' to the new predecessor, and clear
 * 'bbCatchTyp' from 'block').
 */
void                Compiler::fgExtendEHRegionBefore(BasicBlock* block)
{
    assert(block->bbPrev != nullptr);

    BasicBlock* bPrev = block->bbPrev;

    bPrev->copyEHRegion(block);

    // The first block (and only the first block) of a handler has bbCatchTyp set
    bPrev->bbCatchTyp = block->bbCatchTyp;
    block->bbCatchTyp = BBCT_NONE;

    EHblkDsc*       HBtab;
    EHblkDsc*       HBtabEnd;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        /* Multiple pointers in EHblkDsc can point to same block. We can not early out after the first match. */
        if  (HBtab->ebdTryBeg == block)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("EH#%u: New first block of try: BB%02u\n", ehGetIndex(HBtab), bPrev->bbNum);
            }
#endif // DEBUG
            HBtab->ebdTryBeg = bPrev;
            bPrev->bbFlags |=  BBF_TRY_BEG | BBF_DONT_REMOVE | BBF_HAS_LABEL;
            // clear the TryBeg flag unless it begins another try region
            if (!bbIsTryBeg(block))
            {
                block->bbFlags &= ~BBF_TRY_BEG;
            }
        }

        if  (HBtab->ebdHndBeg == block)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("EH#%u: New first block of handler: BB%02u\n", ehGetIndex(HBtab), bPrev->bbNum);
            }
#endif // DEBUG

            // The first block of a handler has an artificial extra refcount. Transfer that to the new block.
            assert(block->bbRefs > 0);
            block->bbRefs--;

            HBtab->ebdHndBeg = bPrev;
            bPrev->bbFlags |=  BBF_DONT_REMOVE | BBF_HAS_LABEL;
            bPrev->bbRefs++;

            // If this is a handler for a filter, the last block of the filter will end with 
            // a BBJ_EJFILTERRET block that has a bbJumpDest that jumps to the first block of 
            // it's handler.  So we need to update it to keep things in sync.
            //
            if (HBtab->HasFilter())
            {
                BasicBlock* bFilterLast = HBtab->BBFilterLast();
                assert(bFilterLast != nullptr);
                assert(bFilterLast->bbJumpKind == BBJ_EHFILTERRET);
                assert(bFilterLast->bbJumpDest == block);
#ifdef DEBUG
                if (verbose)
                {
                    printf("EH#%u: Updating bbJumpDest for filter ret block: BB%02u => BB%02u\n",
                           ehGetIndex(HBtab), bFilterLast->bbNum, bPrev->bbNum);
                }
#endif // DEBUG
                // Change the bbJumpDest for bFilterLast from the old first 'block' to the new first 'bPrev'
                bFilterLast->bbJumpDest = bPrev;
            }
        }

        if (HBtab->HasFilter() && (HBtab->ebdFilter == block))
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("EH#%u: New first block of filter: BB%02u\n", ehGetIndex(HBtab), bPrev->bbNum);
            }
#endif // DEBUG

            // The first block of a filter has an artificial extra refcount. Transfer that to the new block.
            assert(block->bbRefs > 0);
            block->bbRefs--;

            HBtab->ebdFilter = bPrev;
            bPrev->bbFlags |=  BBF_DONT_REMOVE | BBF_HAS_LABEL;
            bPrev->bbRefs++;
        }
    }
}

/*****************************************************************************
 * We've inserted a new block after 'block' that should be part of the same EH region as 'block'.
 * Update the EH table to make this so. Also, set the new block to have the right EH region data.
 */

void                Compiler::fgExtendEHRegionAfter(BasicBlock* block)
{
    BasicBlock* newBlk = block->bbNext;
    assert(newBlk != nullptr);

    newBlk->copyEHRegion(block);
    newBlk->bbCatchTyp = BBCT_NONE; // Only the first block of a catch has this set, and 'newBlk' can't be the first block of a catch.

    // TODO-Throughput: if the block is not in an EH region, then we don't need to walk the EH table looking for 'last' block pointers to update.
    ehUpdateLastBlocks(block, newBlk);
}

/*****************************************************************************
 *
 * Insert a BasicBlock before the given block.
 */

BasicBlock*         Compiler::fgNewBBbefore(BBjumpKinds  jumpKind,
                                            BasicBlock*  block,
                                            bool         extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = bbNewBasicBlock(jumpKind);
    newBlk->bbFlags    |= BBF_INTERNAL;

    fgInsertBBbefore(block, newBlk);

    newBlk->bbRefs = 0;

    if (newBlk->bbFallsThrough() && block->isRunRarely())
    {
        newBlk->bbSetRunRarely();
    }

    if (extendRegion)
    {
        fgExtendEHRegionBefore(block);
    }
    else
    {
        // When extendRegion is false the caller is responsible for setting these two values
        newBlk->setTryIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
        newBlk->setHndIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
    }

    // We assume that if the block we are inserting before is in the cold region, then this new
    // block will also be in the cold region.
    newBlk->bbFlags |= (block->bbFlags & BBF_COLD);

    return newBlk;
}

/*****************************************************************************
 *
 * Insert a BasicBlock after the given block.
 */

BasicBlock*         Compiler::fgNewBBafter(BBjumpKinds  jumpKind,
                                           BasicBlock*  block,
                                           bool         extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = bbNewBasicBlock(jumpKind);
    newBlk->bbFlags    |= BBF_INTERNAL;

    fgInsertBBafter(block, newBlk);

    newBlk->bbRefs = 0;

    if (block->bbFallsThrough() && block->isRunRarely())
    {
        newBlk->bbSetRunRarely();
    }

    if (extendRegion)
    {
        fgExtendEHRegionAfter(block);
    }
    else
    {
        // When extendRegion is false the caller is responsible for setting these two values
        newBlk->setTryIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
        newBlk->setHndIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
    }

    // If the new block is in the cold region (because the block we are inserting after
    // is in the cold region), mark it as such.
    newBlk->bbFlags |= (block->bbFlags & BBF_COLD);

    return newBlk;
}

/*****************************************************************************
 *  Inserts basic block before existing basic block.
 *
 *  If insertBeforeBlk is in the funclet region, then newBlk will be in the funclet region.
 *  (If insertBeforeBlk is the first block of the funclet region, then 'newBlk' will be the
 *  new first block of the funclet region.)
 */
void                Compiler::fgInsertBBbefore(BasicBlock* insertBeforeBlk,
                                               BasicBlock* newBlk)
{
    if (insertBeforeBlk->bbPrev)
    {
        fgInsertBBafter(insertBeforeBlk->bbPrev, newBlk);
    }
    else
    {
        newBlk->setNext(fgFirstBB);

        fgFirstBB = newBlk;
        newBlk->bbPrev = nullptr;
    }

#if FEATURE_EH_FUNCLETS

    /* Update fgFirstFuncletBB if insertBeforeBlk is the first block of the funclet region. */

    if (fgFirstFuncletBB == insertBeforeBlk)
    {
        fgFirstFuncletBB = newBlk;
    }

#endif // FEATURE_EH_FUNCLETS
}

/*****************************************************************************
 *  Inserts basic block after existing basic block.
 *
 *  If insertBeforeBlk is in the funclet region, then newBlk will be in the funclet region.
 *  (It can't be used to insert a block as the first block of the funclet region).
 */
void                Compiler::fgInsertBBafter(BasicBlock* insertAfterBlk,
                                              BasicBlock* newBlk)
{
    newBlk->bbNext = insertAfterBlk->bbNext;

    if (insertAfterBlk->bbNext)
        insertAfterBlk->bbNext->bbPrev = newBlk;
        
    insertAfterBlk->bbNext = newBlk;
    newBlk->bbPrev = insertAfterBlk;

    if (fgLastBB == insertAfterBlk)
    {
        fgLastBB = newBlk;
        assert(fgLastBB->bbNext == NULL);
    }
}

// We have two edges (bAlt => bCur) and (bCur => bNext).
//
// Returns true if the weight of (bAlt => bCur)
//  is greater than the weight of (bCur => bNext).
// We compare the edge weights if we have valid edge weights
//  otherwise we compare blocks weights.
//
bool Compiler::fgIsBetterFallThrough(BasicBlock* bCur, BasicBlock* bAlt)
{
    // bCur can't be NULL and must be a fall through bbJumpKind
    noway_assert(bCur != NULL);
    noway_assert(bCur->bbFallsThrough());
    noway_assert(bAlt != NULL);

    // We only handle the cases when bAlt is a BBJ_ALWAYS or a BBJ_COND
    if ((bAlt->bbJumpKind != BBJ_ALWAYS) &&
        (bAlt->bbJumpKind != BBJ_COND))
    {
        return false;
    }

    // if bAlt doesn't jump to bCur it can't be a better fall through than bCur
    if (bAlt->bbJumpDest != bCur)
        return false;

    // Currently bNext is the fall through for bCur
    BasicBlock* bNext = bCur->bbNext;
    noway_assert(bNext != NULL);

    // We will set result to true if bAlt is a better fall through than bCur
    bool result;
    if (fgHaveValidEdgeWeights)
    {
        // We will compare the edge weight for our two choices
        flowList* edgeFromAlt = fgGetPredForBlock(bCur,  bAlt);
        flowList* edgeFromCur = fgGetPredForBlock(bNext, bCur);
        noway_assert(edgeFromCur != NULL);
        noway_assert(edgeFromAlt != NULL);

        result = (edgeFromAlt->flEdgeWeightMin > edgeFromCur->flEdgeWeightMax);
    }
    else
    {
        if (bAlt->bbJumpKind == BBJ_ALWAYS)
        {
            // Our result is true if bAlt's weight is more than bCur's weight
            result = (bAlt->bbWeight > bCur->bbWeight);
        }
        else
        {
            noway_assert(bAlt->bbJumpKind == BBJ_COND);
            // Our result is true if bAlt's weight is more than twice bCur's weight
            result = (bAlt->bbWeight > (2*bCur->bbWeight));
        }
    }
    return result;
}

//------------------------------------------------------------------------
// fgCheckEHCanInsertAfterBlock: Determine if a block can be inserted after
// 'blk' and legally be put in the EH region specified by 'regionIndex'. This
// can be true if the most nested region the block is in is already 'regionIndex',
// as we'll just extend the most nested region (and any region ending at the same block).
// It can also be true if it is the end of (a set of) EH regions, such that
// inserting the block and properly extending some EH regions (if necessary)
// puts the block in the correct region. We only consider the case of extending
// an EH region after 'blk' (that is, to include 'blk' and the newly insert block);
// we don't consider inserting a block as the the first block of an EH region following 'blk'.
//
// Consider this example:
//
//      try3   try2   try1
//      |---   |      |      BB01
//      |      |---   |      BB02
//      |      |      |---   BB03
//      |      |      |      BB04
//      |      |---   |---   BB05
//      |                    BB06
//      |-----------------   BB07
//
// Passing BB05 and try1/try2/try3 as the region to insert into (as well as putInTryRegion==true)
// will all return 'true'. Here are the cases:
// 1. Insert into try1: the most nested EH region BB05 is in is already try1, so we can insert after
//    it and extend try1 (and try2).
// 2. Insert into try2: we can extend try2, but leave try1 alone. 
// 3. Insert into try3: we can leave try1 and try2 alone, and put the new block just in try3. Note that
//    in this case, after we "loop outwards" in the EH nesting, we get to a place where we're in the middle
//    of the try3 region, not at the end of it.
// In all cases, it is possible to put a block after BB05 and put it in any of these three 'try' regions legally.
// 
// Filters are ignored; if 'blk' is in a filter, the answer will be false.
//
// Arguments:
//    blk - the BasicBlock we are checking to see if we can insert after.
//    regionIndex - the EH region we want to insert a block into. regionIndex is
//          in the range [0..compHndBBtabCount]; 0 means "main method".
//    putInTryRegion - 'true' if the new block should be inserted in the 'try' region of 'regionIndex'.
//          For regionIndex 0 (the "main method"), this should be 'true'.
//
// Return Value:
//    'true' if a block can be inserted after 'blk' and put in EH region 'regionIndex', else 'false'.
//
bool    Compiler::fgCheckEHCanInsertAfterBlock(BasicBlock* blk, unsigned regionIndex, bool putInTryRegion)
{
    assert(blk != nullptr);
    assert(regionIndex <= compHndBBtabCount);

    if (regionIndex == 0)
    {
        assert(putInTryRegion);
    }

    bool inTryRegion;
    unsigned nestedRegionIndex = ehGetMostNestedRegionIndex(blk, &inTryRegion);

    bool insertOK = true;
    for (;;)
    {
        if (nestedRegionIndex == regionIndex)
        {
            // This block is in the region we want to be in. We can insert here if it's the right type of region.
            // (If we want to be in the 'try' region, but the block is in the handler region, then inserting a
            // new block after 'blk' can't put it in the 'try' region, and vice-versa, since we only consider
            // extending regions after, not prepending to regions.)
            // This check will be 'true' if we are trying to put something in the main function (as putInTryRegion
            // must be 'true' if regionIndex is zero, and inTryRegion will also be 'true' if nestedRegionIndex is zero).
            insertOK = (putInTryRegion == inTryRegion);
            break;
        }
        else if (nestedRegionIndex == 0)
        {
            // The block is in the main function, but we want to put something in a nested region. We can't do that.
            insertOK = false;
            break;
        }

        assert(nestedRegionIndex > 0);
        EHblkDsc* ehDsc = ehGetDsc(nestedRegionIndex - 1);   // ehGetDsc uses [0..compHndBBtabCount) form.

        if (inTryRegion)
        {
            if (blk != ehDsc->ebdTryLast)
            {
                // Not the last block? Then it must be somewhere else within the try region, so we can't insert here.
                insertOK = false;
                break; // exit the 'for' loop
            }
        }
        else
        {
            // We ignore filters.
            if (blk != ehDsc->ebdHndLast)
            {
                // Not the last block? Then it must be somewhere else within the handler region, so we can't insert here.
                insertOK = false;
                break; // exit the 'for' loop
            }
        }

        // Things look good for this region; check the enclosing regions, if any.

        nestedRegionIndex = ehGetEnclosingRegionIndex(nestedRegionIndex - 1, &inTryRegion); // ehGetEnclosingRegionIndex uses [0..compHndBBtabCount) form.

        // Convert to [0..compHndBBtabCount] form.
        nestedRegionIndex = (nestedRegionIndex == EHblkDsc::NO_ENCLOSING_INDEX) ? 0 : nestedRegionIndex + 1;
    } // end of for(;;)

    return insertOK;
}

//------------------------------------------------------------------------
// Finds the block closest to endBlk in the range [startBlk..endBlk) after which a block can be
// inserted easily. Note that endBlk cannot be returned; its predecessor is the last block that can
// be returned. The new block will be put in an EH region described by the arguments regionIndex,
// putInTryRegion, startBlk, and endBlk (explained below), so it must be legal to place to put the
// new block after the insertion location block, give it the specified EH region index, and not break
// EH nesting rules. This function is careful to choose a block in the correct EH region. However,
// it assumes that the new block can ALWAYS be placed at the end (just before endBlk). That means
// that the caller must ensure that is true.
//
// Below are the possible cases for the arguments to this method:
//      1. putInTryRegion == true and regionIndex > 0:
//         Search in the try region indicated by regionIndex.
//      2. putInTryRegion == false and regionIndex > 0:
//         a. If startBlk is the first block of a filter and endBlk is the block after the end of the
//            filter (that is, the startBlk and endBlk match a filter bounds exactly), then choose a
//            location within this filter region. (Note that, due to IL rules, filters do not have any
//            EH nested within them.) Otherwise, filters are skipped.
//         b. Else, search in the handler region indicated by regionIndex.
//      3. regionIndex = 0:
//         Search in the entire main method, excluding all EH regions. In this case, putInTryRegion must be true.
//
// This method makes sure to find an insertion point which would not cause the inserted block to
// be put inside any inner try/filter/handler regions.
//
// The actual insertion occurs after the returned block. Note that the returned insertion point might
// be the last block of a more nested EH region, because the new block will be inserted after the insertion
// point, and will not extend the more nested EH region. For example:
//
//      try3   try2   try1
//      |---   |      |      BB01
//      |      |---   |      BB02
//      |      |      |---   BB03
//      |      |      |      BB04
//      |      |---   |---   BB05
//      |                    BB06
//      |-----------------   BB07
//
// for regionIndex==try3, putInTryRegion==true, we might return BB05, even though BB05 will have a try index
// for try1 (the most nested 'try' region the block is in). That's because when we insert after BB05, the new
// block will be in the correct, desired EH region, since try1 and try2 regions will not be extended to include
// the inserted block. Furthermore, for regionIndex==try2, putInTryRegion==true, we can also return BB05. In this
// case, when the new block is inserted, the try1 region remains the same, but we need extend region 'try2' to
// include the inserted block. (We also need to check all parent regions as well, just in case any parent regions
// also end on the same block, in which case we would also need to extend the parent regions. This is standard
// procedure when inserting a block at the end of an EH region.)
//
// If nearBlk is non-nullptr then we return the closest block after nearBlk that will work best.
//
// We try to find a block in the appropriate region that is not a fallthrough block, so we can insert after it
// without the need to insert a jump around the inserted block.
//
// Note that regionIndex is numbered the same as BasicBlock::bbTryIndex and BasicBlock::bbHndIndex, that is, "0" is
// "main method" and otherwise is +1 from normal, so we can call, e.g., ehGetDsc(tryIndex - 1).
//
// Arguments:
//    regionIndex - the region index where the new block will be inserted. Zero means entire method;
//          non-zero means either a "try" or a "handler" region, depending on what putInTryRegion says.
//    putInTryRegion - 'true' to put the block in the 'try' region corresponding to 'regionIndex', 'false'
//          to put the block in the handler region. Should be 'true' if regionIndex==0.
//    startBlk - start block of range to search.
//    endBlk - end block of range to search (don't include this block in the range). Can be nullptr to indicate
//          the end of the function.
//    nearBlk - If non-nullptr, try to find an insertion location closely after this block. If nullptr, we insert
//          at the best location found towards the end of the acceptable block range.
//    jumpBlk - When nearBlk is set, this can be set to the block which jumps to bNext->bbNext (TODO: need to review this?)
//    runRarely - true if the block being inserted is expected to be rarely run. This helps determine
//          the best place to put the new block, by putting in a place that has the same 'rarely run' characteristic.
//
// Return Value:
//    A block with the desired characteristics, so the new block will be inserted after this one.
//    If there is no suitable location, return nullptr. This should basically never happen.

BasicBlock*  Compiler::fgFindInsertPoint(unsigned              regionIndex,
                                         bool                  putInTryRegion,
                                         BasicBlock*           startBlk,
                                         BasicBlock*           endBlk,
                                         BasicBlock*           nearBlk,
                                         BasicBlock*           jumpBlk,
                                         bool                  runRarely)
{
    noway_assert(startBlk != nullptr);
    noway_assert(startBlk != endBlk);
    noway_assert((regionIndex == 0 && putInTryRegion)                                        ||  // Search in the main method
                 (putInTryRegion  && regionIndex > 0 && startBlk->bbTryIndex == regionIndex) ||  // Search in the specified try     region
                 (!putInTryRegion && regionIndex > 0 && startBlk->bbHndIndex == regionIndex));   // Search in the specified handler region

#ifdef DEBUG
    // Assert that startBlk precedes endBlk in the block list.
    // We don't want to use bbNum to assert this condition, as we cannot depend on the block numbers being
    // sequential at all times.
    for (BasicBlock* b = startBlk; b != endBlk; b = b->bbNext)
    {
        assert(b != nullptr); // We reached the end of the block list, but never found endBlk.
    }
#endif // DEBUG

    JITDUMP("fgFindInsertPoint(regionIndex=%u, putInTryRegion=%s, startBlk=BB%02u, endBlk=BB%02u, nearBlk=BB%02u, jumpBlk=BB%02u, runRarely=%s)\n",
            regionIndex,
            dspBool(putInTryRegion),
            startBlk->bbNum,
            (endBlk == nullptr)  ? 0 : endBlk->bbNum,
            (nearBlk == nullptr) ? 0 : nearBlk->bbNum,
            (jumpBlk == nullptr) ? 0 : jumpBlk->bbNum,
            dspBool(runRarely));

    bool           reachedNear = false;     // Have we reached 'nearBlk' in our search? If not, we'll keep searching.
    bool           inFilter    = false;     // Are we in a filter region that we need to skip?
    BasicBlock*    bestBlk     = nullptr;   // Set to the best insertion point we've found so far that meets all the EH requirements.
    BasicBlock*    goodBlk     = nullptr;   // Set to an acceptable insertion point that we'll use if we don't find a 'best' option.
    BasicBlock*    blk;

    if (nearBlk != nullptr)
    {
        // Does the nearBlk precede the startBlk?
        for (blk = nearBlk; blk != nullptr; blk = blk->bbNext)
        {
            if (blk == startBlk)
            {
                reachedNear = true;
                break;
            }
            else if (blk == endBlk)
            {
                break;
            }
        }
    }

    for (blk = startBlk; blk != endBlk; blk = blk->bbNext)
    {
        // The only way (blk == nullptr) could be true is if the caller passed an endBlk that preceded startBlk in the
        // block list, or if endBlk isn't in the block list at all. In DEBUG, we'll instead hit the similar well-formedness
        // assert earlier in this function.
        noway_assert(blk != nullptr);

        if (blk == nearBlk)
        {
            reachedNear = true;
        }

        if (blk->bbCatchTyp == BBCT_FILTER)
        {
            // Record the fact that we entered a filter region, so we don't insert into filters...
            // Unless the caller actually wanted the block inserted in this exact filter region.
            // Detect this by the fact that startBlk and endBlk point to the filter begin and end.
            if (putInTryRegion ||
                (blk != startBlk) ||
                (startBlk != ehGetDsc(regionIndex - 1)->ebdFilter) ||
                (endBlk   != ehGetDsc(regionIndex - 1)->ebdHndBeg))
            {
                inFilter = true;
            }
        }
        else if (blk->bbCatchTyp == BBCT_FILTER_HANDLER)
        {
            // Record the fact that we exited a filter region.
            inFilter = false;
        }

        // Don't insert a block inside this filter region.
        if (inFilter)
            continue;

        // Note that the new block will be inserted AFTER "blk". We check to make sure that doing so
        // would put the block in the correct EH region. We make an assumption here that you can
        // ALWAYS insert the new block before "endBlk" (that is, at the end of the search range)
        // and be in the correct EH region. This is must be guaranteed by the caller (as it is by
        // fgNewBBinRegion(), which passes the search range as an exact EH region block range).
        // Because of this assumption, we only check the EH information for blocks before the last block.
        if (blk->bbNext != endBlk)
        {
            // We are in the middle of the search range. We can't insert the new block in
            // an inner try or handler region. We can, however, set the insertion
            // point to the last block of an EH try/handler region, if the enclosing
            // region is the region we wish to insert in. (Since multiple regions can
            // end at the same block, we need to search outwards, checking that the
            // block is the last block of every EH region out to the region we want
            // to insert in.) This is especially useful for putting a call-to-finally
            // block on AMD64 immediately after its corresponding 'try' block, so in the
            // common case, we'll just fall through to it. For example:
            //
            //      BB01
            //      BB02 -- first block of try
            //      BB03
            //      BB04 -- last block of try
            //      BB05 -- first block of finally
            //      BB06
            //      BB07 -- last block of handler
            //      BB08
            //
            // Assume there is only one try/finally, so BB01 and BB08 are in the "main function".
            // For AMD64 call-to-finally, we'll want to insert the BBJ_CALLFINALLY in
            // the main function, immediately after BB04. This allows us to do that.

            if (!fgCheckEHCanInsertAfterBlock(blk, regionIndex, putInTryRegion))
            {
                // Can't insert here.
                continue;
            }
        }

        // Look for an insert location:
        // 1. We want blocks that don't end with a fall through,
        // 2. Also, when blk equals nearBlk we may want to insert here.
        if (!blk->bbFallsThrough() || (blk == nearBlk))
        {
            bool updateBestBlk = true;  // We will probably update the bestBlk

            // If blk falls through then we must decide whether to use the nearBlk
            // hint
            if (blk->bbFallsThrough())
            {
                noway_assert(blk == nearBlk);
                if (jumpBlk != nullptr)
                {
                    updateBestBlk = fgIsBetterFallThrough(blk, jumpBlk);
                }
                else
                {
                    updateBestBlk = false;
                }
            }

            // If we already have a best block, see if the 'runRarely' flags influences
            // our choice. If we want a runRarely insertion point, and the existing best
            // block is run rarely but the current block isn't run rarely, then don't
            // update the best block.
            // TODO-CQ: We should also handle the reverse case, where runRarely is false (we
            // want a non-rarely-run block), but bestBlock->isRunRarely() is true. In that
            // case, we should update the block, also. Probably what we want is:
            //    (bestBlk->isRunRarely() != runRarely) && (blk->isRunRarely() == runRarely)
            if (updateBestBlk &&
                (bestBlk != nullptr) &&
                runRarely &&
                bestBlk->isRunRarely() &&
                !blk->isRunRarely())
            {
                updateBestBlk = false;
            }

            if (updateBestBlk)
            {
                // We found a 'best' insertion location, so save it away.
                bestBlk = blk;

                // If we've reached nearBlk, we've satisfied all the criteria,
                // so we're done.
                if (reachedNear)
                    goto DONE;

                // If we haven't reached nearBlk, keep looking for a 'best' location, just
                // in case we'll find one at or after nearBlk. If no nearBlk was specified,
                // we prefer inserting towards the end of the given range, so keep looking
                // for more acceptable insertion locations.
            }
        }

        // No need to update goodBlk after we have set bestBlk, but we could still find a better
        // bestBlk, so keep looking.
        if (bestBlk != nullptr)
            continue;

        // Set the current block as a "good enough" insertion point, if it meets certain criteria.
        // We'll return this block if we don't find a "best" block in the search range. The block
        // can't be a BBJ_CALLFINALLY of a BBJ_CALLFINALLY/BBJ_ALWAYS pair (since we don't want
        // to insert anything between these two blocks). Otherwise, we can use it. However,
        // if we'd previously chosen a BBJ_COND block, then we'd prefer the "good" block to be
        // something else. We keep updating it until we've reached the 'nearBlk', to push it as
        // close to endBlk as possible.
        if (!blk->isBBCallAlwaysPair())
        {
            if (goodBlk == nullptr)
            {
                goodBlk = blk;
            }
            else if ((goodBlk->bbJumpKind == BBJ_COND)  ||
                     (blk->bbJumpKind     != BBJ_COND))
            {
                if ((blk == nearBlk) || !reachedNear)
                {
                    goodBlk = blk;
                }
            }
        }
    }

    // If we didn't find a non-fall_through block, then insert at the last good block.

    if (bestBlk == nullptr)
    {
        bestBlk = goodBlk;
    }

DONE:;

    return bestBlk;
}


//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it in a specific EH region, given by 'tryIndex', 'hndIndex', and 'putInFilter'.
//
// If 'putInFilter' it true, then the block is inserted in the filter region given by 'hndIndex'. In this case, tryIndex
// must be a less nested EH region (that is, tryIndex > hndIndex).
//
// Otherwise, the block is inserted in either the try region or the handler region, depending on which one is the inner region.
// In other words, if the try region indicated by tryIndex is nested in the handler region indicated by hndIndex,
// then the new BB will be created in the try region. Vice versa.
//
// Note that tryIndex and hndIndex are numbered the same as BasicBlock::bbTryIndex and BasicBlock::bbHndIndex, that is, "0" is
// "main method" and otherwise is +1 from normal, so we can call, e.g., ehGetDsc(tryIndex - 1).
//
// To be more specific, this function will create a new BB in one of the following 5 regions (if putInFilter is false):
// 1. When tryIndex = 0 and hndIndex = 0:
//    The new BB will be created in the method region.
// 2. When tryIndex != 0 and hndIndex = 0:
//    The new BB will be created in the try region indicated by tryIndex.
// 3. When tryIndex == 0 and hndIndex != 0:
//    The new BB will be created in the handler region indicated by hndIndex.
// 4. When tryIndex != 0 and hndIndex != 0 and tryIndex < hndIndex:
//    In this case, the try region is nested inside the handler region. Therefore, the new BB will be created 
//    in the try region indicated by tryIndex.
// 5. When tryIndex != 0 and hndIndex != 0 and tryIndex > hndIndex:
//    In this case, the handler region is nested inside the try region. Therefore, the new BB will be created 
//    in the handler region indicated by hndIndex.
//
// Note that if tryIndex != 0 and hndIndex != 0 then tryIndex must not be equal to hndIndex (this makes sense because
// if they are equal, you are asking to put the new block in both the try and handler, which is impossible).
//
// The BasicBlock will not be inserted inside an EH region that is more nested than the requested tryIndex/hndIndex
// region (so the function is careful to skip more nested EH regions when searching for a place to put the new block).
//
// This function cannot be used to insert a block as the first block of any region. It always inserts a block after
// an existing block in the given region.
//
// If nearBlk is nullptr, or the block is run rarely, then the new block is assumed to be run rarely.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    tryIndex - the try region to insert the new block in, described above. This must be a number in the range [0..compHndBBtabCount].
//    hndIndex - the handler region to insert the new block in, described above. This must be a number in the range [0..compHndBBtabCount].
//    nearBlk  - insert the new block closely after this block, if possible. If nullptr, put the new block anywhere in the requested region.
//    putInFilter - put the new block in the filter region given by hndIndex, as described above.
//    runRarely - 'true' if the new block is run rarely.
//    insertAtEnd - 'true' if the block should be inserted at the end of the region. Note: this is currently only implemented when
//                  inserting into the main function (not into any EH region).
//
// Return Value:
//    The new block.

BasicBlock*         Compiler::fgNewBBinRegion(BBjumpKinds  jumpKind,
                                              unsigned     tryIndex,
                                              unsigned     hndIndex,
                                              BasicBlock*  nearBlk,
                                              bool         putInFilter /* = false */,
                                              bool         runRarely /* = false */,
                                              bool         insertAtEnd /* = false */)
{
    assert(tryIndex <= compHndBBtabCount);
    assert(hndIndex <= compHndBBtabCount);

    /* afterBlk is the block which will precede the newBB */
    BasicBlock*   afterBlk;

    // start and end limit for inserting the block
    BasicBlock*   startBlk = nullptr;
    BasicBlock*   endBlk = nullptr;

    bool          putInTryRegion = true;
    unsigned      regionIndex = 0;

    // First, figure out which region (the "try" region or the "handler" region) to put the newBB in.
    if ((tryIndex == 0) && (hndIndex == 0))
    {
        assert(!putInFilter);

        endBlk = fgEndBBAfterMainFunction();  // don't put new BB in funclet region

        if (insertAtEnd || (nearBlk == nullptr))
        {
            /* We'll just insert the block at the end of the method, before the funclets */

            afterBlk = fgLastBBInMainFunction();
            goto _FoundAfterBlk;
        }
        else
        {
            // We'll search through the entire method
            startBlk = fgFirstBB;
        }

        noway_assert(regionIndex == 0);
    }
    else
    {
        noway_assert(tryIndex > 0 || hndIndex > 0);
        PREFIX_ASSUME(tryIndex <= compHndBBtabCount);
        PREFIX_ASSUME(hndIndex <= compHndBBtabCount);

        // Decide which region to put in, the "try" region or the "handler" region.
        if (tryIndex == 0)
        {
            noway_assert(hndIndex > 0);
            putInTryRegion = false;
        }
        else if (hndIndex == 0)
        {
            noway_assert(tryIndex > 0);
            noway_assert(putInTryRegion);
            assert(!putInFilter);
        }
        else
        {
            noway_assert(tryIndex > 0 && hndIndex > 0 && tryIndex != hndIndex);
            putInTryRegion =  (tryIndex <  hndIndex);
        }

        if (putInTryRegion)
        {
            // Try region is the inner region.
            // In other words, try region must be nested inside the handler region.
            noway_assert(hndIndex == 0 || bbInHandlerRegions(hndIndex - 1, ehGetDsc(tryIndex - 1)->ebdTryBeg));
            assert(!putInFilter);
        }
        else
        {
            // Handler region is the inner region.
            // In other words, handler region must be nested inside the try region.
            noway_assert(tryIndex == 0 || bbInTryRegions(tryIndex - 1, ehGetDsc(hndIndex - 1)->ebdHndBeg));
        }

        // Figure out the start and end block range to search for an insertion location. Pick the beginning and
        // ending blocks of the target EH region (the 'endBlk' is one past the last block of the EH region, to make
        // loop iteration easier). Note that, after funclets have been created (for FEATURE_EH_FUNCLETS),
        // this linear block range will not include blocks of handlers for try/handler clauses nested within
        // this EH region, as those blocks have been extracted as funclets. That is ok, though, because we don't
        // want to insert a block in any nested EH region.

        if (putInTryRegion)
        {
            // We will put the newBB in the try region.
            EHblkDsc* ehDsc = ehGetDsc(tryIndex - 1);
            startBlk = ehDsc->ebdTryBeg;
            endBlk   = ehDsc->ebdTryLast->bbNext;
            regionIndex = tryIndex;
        }
        else if (putInFilter)
        {
            // We will put the newBB in the filter region.
            EHblkDsc* ehDsc = ehGetDsc(hndIndex - 1);
            startBlk = ehDsc->ebdFilter;
            endBlk   = ehDsc->ebdHndBeg;
            regionIndex = hndIndex;
        }
        else
        {
            // We will put the newBB in the handler region.
            EHblkDsc* ehDsc = ehGetDsc(hndIndex - 1);
            startBlk = ehDsc->ebdHndBeg;
            endBlk   = ehDsc->ebdHndLast->bbNext;
            regionIndex = hndIndex;
        }

        noway_assert(regionIndex > 0);
    }

    // Now find the insertion point.
    afterBlk = fgFindInsertPoint(regionIndex,
                                 putInTryRegion,
                                 startBlk, endBlk,
                                 nearBlk, nullptr,
                                 runRarely);

_FoundAfterBlk:;

    /* We have decided to insert the block after 'afterBlk'. */
    noway_assert(afterBlk != nullptr);

    JITDUMP("fgNewBBinRegion(jumpKind=%u, tryIndex=%u, hndIndex=%u, putInFilter=%s, runRarely=%s, insertAtEnd=%s): inserting after BB%02u\n",
            jumpKind,
            tryIndex,
            hndIndex,
            dspBool(putInFilter),
            dspBool(runRarely),
            dspBool(insertAtEnd),
            afterBlk->bbNum);

    return fgNewBBinRegionWorker(jumpKind, afterBlk, regionIndex, putInTryRegion);
}

//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it in the same EH region as 'srcBlk'.
//
// See the implementation of fgNewBBinRegion() used by this one for more notes.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    srcBlk   - insert the new block in the same EH region as this block, and closely after it if possible.
//
// Return Value:
//    The new block.

BasicBlock*         Compiler::fgNewBBinRegion(BBjumpKinds  jumpKind,
                                              BasicBlock*  srcBlk,
                                              bool         runRarely /* = false */,
                                              bool         insertAtEnd /* = false */)
{
    assert(srcBlk != nullptr);

    const unsigned tryIndex = srcBlk->bbTryIndex;
    const unsigned hndIndex = srcBlk->bbHndIndex;
    bool putInFilter = false;

    // Check to see if we need to put the new block in a filter. We do if srcBlk is in a filter.
    // This can only be true if there is a handler index, and the handler region is more nested than the
    // try region (if any). This is because no EH regions can be nested within a filter.
    if (BasicBlock::ehIndexMaybeMoreNested(hndIndex, tryIndex))
    {
        assert(hndIndex != 0); // If hndIndex is more nested, we must be in some handler!
        putInFilter = ehGetDsc(hndIndex - 1)->InFilterRegionBBRange(srcBlk);
    }

    return fgNewBBinRegion(jumpKind, tryIndex, hndIndex, srcBlk, putInFilter, runRarely, insertAtEnd);
}

//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it at the end of the function.
//
// See the implementation of fgNewBBinRegion() used by this one for more notes.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//
// Return Value:
//    The new block.

BasicBlock*         Compiler::fgNewBBinRegion(BBjumpKinds  jumpKind)
{
    return fgNewBBinRegion(jumpKind, 0, 0, nullptr, /* putInFilter */ false, /* runRarely */ false, /* insertAtEnd */ true);
}


//------------------------------------------------------------------------
// Creates a new BasicBlock, and inserts it after 'afterBlk'.
//
// The block cannot be inserted into a more nested try/handler region than that specified by 'regionIndex'.
// (It is given exactly 'regionIndex'.) Thus, the parameters must be passed to ensure proper EH nesting
// rules are followed.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    afterBlk - insert the new block after this one.
//    regionIndex - the block will be put in this EH region.
//    putInTryRegion - If true, put the new block in the 'try' region corresponding to 'regionIndex', and
//          set its handler index to the most nested handler region enclosing that 'try' region.
//          Otherwise, put the block in the handler region specified by 'regionIndex', and set its 'try'
//          index to the most nested 'try' region enclosing that handler region.
//
// Return Value:
//    The new block.

BasicBlock*         Compiler::fgNewBBinRegionWorker(BBjumpKinds  jumpKind,
                                                    BasicBlock*  afterBlk,
                                                    unsigned     regionIndex,
                                                    bool         putInTryRegion)
{
    /* Insert the new block */
    BasicBlock* afterBlkNext = afterBlk->bbNext;
    (void)afterBlkNext; //prevent "unused variable" error from GCC
    BasicBlock* newBlk = fgNewBBafter(jumpKind, afterBlk, false);

    if (putInTryRegion)
    {
        noway_assert(regionIndex <= MAX_XCPTN_INDEX);
        newBlk->bbTryIndex = (unsigned short)regionIndex;
        newBlk->bbHndIndex = bbFindInnermostHandlerRegionContainingTryRegion(regionIndex);
    }
    else
    {
        newBlk->bbTryIndex = bbFindInnermostTryRegionContainingHandlerRegion(regionIndex);
        noway_assert(regionIndex <= MAX_XCPTN_INDEX);
        newBlk->bbHndIndex = (unsigned short)regionIndex;
    }

    // We're going to compare for equal try regions (to handle the case of 'mutually protect'
    // regions). We need to save off the current try region, otherwise we might change it
    // before it gets compared later, thereby making future comparisons fail.

    BasicBlock* newTryBeg;
    BasicBlock* newTryLast;
    (void)ehInitTryBlockRange(newBlk, &newTryBeg, &newTryLast);

    unsigned        XTnum;
    EHblkDsc*       HBtab;

    for (XTnum = 0, HBtab = compHndBBtab;
         XTnum < compHndBBtabCount;
         XTnum++  , HBtab++)
    {
        // Is afterBlk at the end of a try region?
        if (HBtab->ebdTryLast == afterBlk)
        {
            noway_assert(afterBlkNext == newBlk->bbNext);

            bool extendTryRegion = false;
            if (newBlk->hasTryIndex())
            {
                // We're adding a block after the last block of some try region. Do
                // we extend the try region to include the block, or not?
                // If the try region is exactly the same as the try region
                // associated with the new block (based on the block's try index,
                // which represents the innermost try the block is a part of), then
                // we extend it.
                // If the try region is a "parent" try region -- an enclosing try region
                // that has the same last block as the new block's try region -- then
                // we also extend. For example:
                //      try { // 1
                //          ...
                //          try { // 2
                //          ...
                //      } /* 2 */ } /* 1 */
                // This example is meant to indicate that both try regions 1 and 2 end at
                // the same block, and we're extending 2. Thus, we must also extend 1. If we
                // only extended 2, we would break proper nesting. (Dev11 bug 137967)

                extendTryRegion = HBtab->ebdIsSameTry(newTryBeg, newTryLast) ||
                                  bbInTryRegions(XTnum, newBlk);
            }

            // Does newBlk extend this try region?
            if (extendTryRegion)
            {
                // Yes, newBlk extends this try region

                // newBlk is the now the new try last block
                fgSetTryEnd(HBtab, newBlk);
            }
        }

        // Is afterBlk at the end of a handler region?
        if (HBtab->ebdHndLast == afterBlk)
        {
            noway_assert(afterBlkNext == newBlk->bbNext);

            // Does newBlk extend this handler region?
            bool extendHndRegion = false;
            if (newBlk->hasHndIndex())
            {
                // We're adding a block after the last block of some handler region. Do
                // we extend the handler region to include the block, or not?
                // If the handler region is exactly the same as the handler region
                // associated with the new block (based on the block's handler index,
                // which represents the innermost handler the block is a part of), then
                // we extend it.
                // If the handler region is a "parent" handler region -- an enclosing
                // handler region that has the same last block as the new block's handler
                // region -- then we also extend. For example:
                //      catch { // 1
                //          ...
                //          catch { // 2
                //          ...
                //      } /* 2 */ } /* 1 */
                // This example is meant to indicate that both handler regions 1 and 2 end at
                // the same block, and we're extending 2. Thus, we must also extend 1. If we
                // only extended 2, we would break proper nesting. (Dev11 bug 372051)

                extendHndRegion = bbInHandlerRegions(XTnum, newBlk);
            }

            if (extendHndRegion)
            {
                // Yes, newBlk extends this handler region

                // newBlk is now the last block of the handler.
                fgSetHndEnd(HBtab, newBlk);
            }
        }
    }

    /* If afterBlk falls through, we insert a jump around newBlk */
    fgConnectFallThrough(afterBlk, newBlk->bbNext);

#ifdef DEBUG
    fgVerifyHandlerTab();
#endif

    return newBlk;
}


/*****************************************************************************
 */

/* static */
unsigned            Compiler::acdHelper(SpecialCodeKind codeKind)
{
    switch (codeKind)
    {
    case SCK_RNGCHK_FAIL: return CORINFO_HELP_RNGCHKFAIL;
#if COR_JIT_EE_VERSION > 460
    case SCK_ARG_EXCPN: return CORINFO_HELP_THROW_ARGUMENTEXCEPTION;
    case SCK_ARG_RNG_EXCPN: return CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION;
#endif //COR_JIT_EE_VERSION
    case SCK_DIV_BY_ZERO: return CORINFO_HELP_THROWDIVZERO;
    case SCK_ARITH_EXCPN: return CORINFO_HELP_OVERFLOW;
    default: assert(!"Bad codeKind"); return 0;
    }
}

/*****************************************************************************
 *
 *  Find/create an added code entry associated with the given block and with
 *  the given kind.
 */

BasicBlock*         Compiler::fgAddCodeRef(BasicBlock*      srcBlk,
                                           unsigned         refData,
                                           SpecialCodeKind  kind,
                                           unsigned         stkDepth)
{
    /* For debuggable code, genJumpToThrowHlpBlk() will generate the 'throw'
       code inline. It has to be kept consistent with fgAddCodeRef() */
    if (opts.compDbgCode)
        return NULL;

    const static
    BBjumpKinds jumpKinds[] =
    {
        BBJ_NONE,               // SCK_NONE
        BBJ_THROW,              // SCK_RNGCHK_FAIL
        BBJ_ALWAYS,             // SCK_PAUSE_EXEC
        BBJ_THROW,              // SCK_DIV_BY_ZERO
        BBJ_THROW,              // SCK_ARITH_EXCP, SCK_OVERFLOW
        BBJ_THROW,              // SCK_ARG_EXCPN
        BBJ_THROW,              // SCK_ARG_RNG_EXCPN
    };

    noway_assert(sizeof(jumpKinds) == SCK_COUNT); // sanity check

    /* First look for an existing entry that matches what we're looking for */

    AddCodeDsc* add = fgFindExcptnTarget(kind, refData);

    if (add) // found it
    {
#ifdef _TARGET_X86_
        // If different range checks happen at different stack levels,
        // they can't all jump to the same "call @rngChkFailed" AND have
        // frameless methods, as the rngChkFailed may need to unwind the
        // stack, and we have to be able to report the stack level.
        //
        // The following check forces most methods that reference an
        // array element in a parameter list to have an EBP frame,
        // this restriction could be removed with more careful code
        // generation for BBJ_THROW (i.e. range check failed).
        //
        if  (add->acdStkLvl != stkDepth)
        {
            codeGen->setFrameRequired(true);
        }
#endif // _TARGET_X86_

        return  add->acdDstBlk;
    }

    /* We have to allocate a new entry and prepend it to the list */

    add = new (this, CMK_Unknown) AddCodeDsc;
    add->acdData   = refData;
    add->acdKind   = kind;
    add->acdStkLvl = (unsigned short)stkDepth;
    noway_assert(add->acdStkLvl == stkDepth);
    add->acdNext   = fgAddCodeList;
                     fgAddCodeList = add;

    /* Create the target basic block */

    BasicBlock*     newBlk;

    newBlk          =
    add->acdDstBlk  = fgNewBBinRegion(jumpKinds[kind], srcBlk, /* runRarely */ true, /* insertAtEnd */ true);

    add->acdDstBlk->bbFlags |= BBF_JMP_TARGET|BBF_HAS_LABEL;

#ifdef DEBUG
    if (verbose)
    {
        const char* msgWhere = "";
        if (!srcBlk->hasTryIndex() && !srcBlk->hasHndIndex())
        {
            msgWhere = "non-EH region";
        }
        else if (!srcBlk->hasTryIndex())
        {
            msgWhere = "handler";
        }
        else if (!srcBlk->hasHndIndex())
        {
            msgWhere = "try";
        }
        else if (srcBlk->getTryIndex() < srcBlk->getHndIndex())
        {
            msgWhere = "try";
        }
        else
        {
            msgWhere = "handler";
        }

        const char* msg;
        switch (kind)
        {
        case SCK_RNGCHK_FAIL:   msg = " for RNGCHK_FAIL";   break;
        case SCK_PAUSE_EXEC:    msg = " for PAUSE_EXEC";    break;
        case SCK_DIV_BY_ZERO:   msg = " for DIV_BY_ZERO";   break;
        case SCK_OVERFLOW:      msg = " for OVERFLOW";      break;
#if COR_JIT_EE_VERSION > 460
        case SCK_ARG_EXCPN:     msg = " for ARG_EXCPN";     break;
        case SCK_ARG_RNG_EXCPN: msg = " for ARG_RNG_EXCPN"; break;
#endif //COR_JIT_EE_VERSION
        default:                msg = " for ??";            break;
        }

        printf("\nfgAddCodeRef -"
               " Add BB in %s%s, new block BB%02u [%08p], stkDepth is %d\n",
               msgWhere, msg, add->acdDstBlk->bbNum, dspPtr(add->acdDstBlk), stkDepth);
    }
#endif // DEBUG


#ifdef DEBUG
    newBlk->bbTgtStkDepth = stkDepth;
#endif // DEBUG

    /* Mark the block as added by the compiler and not removable by future flow
       graph optimizations. Note that no bbJumpDest points to these blocks. */

    newBlk->bbFlags |= BBF_IMPORTED;
    newBlk->bbFlags |= BBF_DONT_REMOVE;

    /* Remember that we're adding a new basic block */

    fgAddCodeModf      = true;
    fgRngChkThrowAdded = true;

    /* Now figure out what code to insert */

    GenTreeCall* tree;
    int          helper = CORINFO_HELP_UNDEF;

    switch (kind)
    {
    case SCK_RNGCHK_FAIL:   helper = CORINFO_HELP_RNGCHKFAIL;
                            break;

    case SCK_DIV_BY_ZERO:   helper = CORINFO_HELP_THROWDIVZERO; 
                            break;

    case SCK_ARITH_EXCPN:   helper = CORINFO_HELP_OVERFLOW;
                            noway_assert(SCK_OVERFLOW == SCK_ARITH_EXCPN);
                            break;

#if COR_JIT_EE_VERSION > 460
    case SCK_ARG_EXCPN:     helper = CORINFO_HELP_THROW_ARGUMENTEXCEPTION;
                            break;

    case SCK_ARG_RNG_EXCPN: helper = CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION;
                            break;
#endif // COR_JIT_EE_VERSION

//  case SCK_PAUSE_EXEC:
//      noway_assert(!"add code to pause exec");

    default:
        noway_assert(!"unexpected code addition kind");
        return nullptr;
    }

    noway_assert(helper != CORINFO_HELP_UNDEF);

    // Add the appropriate helper call.
    tree = gtNewHelperCallNode(helper, TYP_VOID, GTF_EXCEPT);

    // There are no args here but fgMorphArgs has side effects 
    // such as setting the outgoing arg area (which is necessary 
    // on AMD if there are any calls).
    tree = fgMorphArgs(tree);

    // Store the tree in the new basic block.

    fgInsertStmtAtEnd(newBlk, fgNewStmtFromTree(tree));

    return  add->acdDstBlk;
}

/*****************************************************************************
 * Finds the block to jump to, to throw a given kind of exception
 * We maintain a cache of one AddCodeDsc for each kind, to make searching fast.
 * Note : Each block uses the same (maybe shared) block as the jump target for
 * a given type of exception
 */

Compiler::AddCodeDsc*       Compiler::fgFindExcptnTarget(SpecialCodeKind  kind,
                                                         unsigned     refData)
{
    if (!(fgExcptnTargetCache[kind] &&  // Try the cached value first
          fgExcptnTargetCache[kind]->acdData == refData))
    {
        // Too bad, have to search for the jump target for the exception

        AddCodeDsc* add = NULL;

        for (add = fgAddCodeList; add != NULL; add = add->acdNext)
        {
            if  (add->acdData == refData && add->acdKind == kind)
                break;
        }

        fgExcptnTargetCache[kind] = add; // Cache it
    }

    return fgExcptnTargetCache[kind];
}

/*****************************************************************************
 *
 *  The given basic block contains an array range check; return the label this
 *  range check is to jump to upon failure.
 */

BasicBlock*         Compiler::fgRngChkTarget(BasicBlock* block, unsigned stkDepth, SpecialCodeKind kind)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*** Computing fgRngChkTarget for block BB%02u to stkDepth %d\n", block->bbNum, stkDepth);
        gtDispTree(compCurStmt);
    }
#endif // DEBUG

    /* We attach the target label to the containing try block (if any) */
    noway_assert(!compIsForInlining());
    return  fgAddCodeRef(block, bbThrowIndex(block), kind, stkDepth);
}

// Sequences the tree.
// prevTree is what gtPrev of the first node in execution order gets set to.
// Returns the first node (execution order) in the sequenced tree.
GenTree*            Compiler::fgSetTreeSeq(GenTree* tree, GenTree* prevTree)
{
    GenTree list;

    if (prevTree == nullptr)
    {
        prevTree = &list;
    }
    fgTreeSeqLst = prevTree;
    fgTreeSeqNum = 0;
    fgTreeSeqBeg = nullptr;
    fgSetTreeSeqHelper(tree);

    GenTree* result = prevTree->gtNext;
    if (prevTree == &list)
    {
        list.gtNext->gtPrev = nullptr;
    }

    return result;
}

/*****************************************************************************
 *
 *  Assigns sequence numbers to the given tree and its sub-operands, and
 *  threads all the nodes together via the 'gtNext' and 'gtPrev' fields.
 *  Uses 'global' - fgTreeSeqLst
 */

void                Compiler::fgSetTreeSeqHelper(GenTreePtr tree)
{
    genTreeOps      oper;
    unsigned        kind;

    noway_assert(tree);
    assert(!IsUninitialized(tree));
    noway_assert(tree->gtOper != GT_STMT);

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Is this a leaf/constant node? */

    if  (kind & (GTK_CONST|GTK_LEAF))
    {
        fgSetTreeSeqFinish(tree);
        return;
    }

    /* Is it a 'simple' unary/binary operator? */

    if  (kind & GTK_SMPOP)
    {
        GenTreePtr      op1 = tree->gtOp.gtOp1;
        GenTreePtr      op2 = tree->gtGetOp2();

        // Special handling for GT_LIST
        if (tree->OperGet() == GT_LIST)
        {
            
            if (tree->gtOp.gtOp2 != nullptr
                && tree->gtOp.gtOp2->gtOper != GT_LIST)
            {
                // This is a special kind of GT_LIST that only occurs under initBlk and copyBlk.
                // It is used as a pair, where op1 is the dst and op2 is the src (value or location)
                // The use must appear before the def because of the case where a local is cpblk'ed to itself.
                // If it were otherwise, upstream stores to the local would appear to be dead.
                assert(tree->gtOp.gtOp1->gtOper != GT_LIST);
                fgSetTreeSeqHelper(tree->gtOp.gtOp2);
                fgSetTreeSeqHelper(tree->gtOp.gtOp1);
                fgSetTreeSeqFinish(tree);
                return;
            }

            // First, handle the list items, which will be linked in forward order.
            // As we go, we will link the GT_LIST nodes in reverse order - we will number
            // them and update fgTreeSeqList in a subsequent traversal.
            GenTreePtr nextList = tree;
            GenTreePtr list = nullptr;
            while(nextList != nullptr && nextList->OperGet() == GT_LIST)
            {
                list = nextList;
                GenTreePtr listItem = list->gtOp.gtOp1;
                fgSetTreeSeqHelper(listItem);
                nextList = list->gtOp.gtOp2;
                if (nextList != nullptr)
                {
                    nextList->gtNext = list;
                }
                list->gtPrev = nextList;
            }
            // Next, handle the GT_LIST nodes.
            // Note that fgSetTreeSeqFinish() sets the gtNext to null, so we need to capture the nextList
            // before we call that method.
            nextList = list;
            do
            {
                assert(list != nullptr);
                list = nextList;
                nextList = list->gtNext;
                fgSetTreeSeqFinish(list);
            } while (list != tree);
            return;
        }

        /* Special handling for AddrMode */
        if (tree->OperIsAddrMode())
        {
            bool reverse = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);
            if (reverse)
            {
                assert(op1 != NULL && op2 != NULL);
                fgSetTreeSeqHelper(op2);
            }
            if (op1 != NULL)
            {
                fgSetTreeSeqHelper(op1);
            }
            if (!reverse && op2 != NULL)
            {
                fgSetTreeSeqHelper(op2);
            }

            fgSetTreeSeqFinish(tree);
            return;
        }

        /* Handle the case of an LDOBJ with a field list */

        GenTreePtr lclVarTree;
        if ((oper == GT_LDOBJ) &&
            tree->gtLdObj.gtFldTreeList != NULL &&
            impIsAddressInLocal(tree->gtOp.gtOp1, &lclVarTree))
        {
            GenTreePtr* fldTreeList = tree->gtLdObj.gtFldTreeList;
            unsigned fieldCount = lvaTable[lclVarTree->gtLclVarCommon.gtLclNum].lvFieldCnt;

            for (unsigned i = 0; i < fieldCount; i++)
            {
                if (fldTreeList[i] != NULL)
                {
                    fgSetTreeSeqHelper(fldTreeList[i]);
                }
            }
        }

        /* Check for a nilary operator */

        if (op1 == NULL)
        {
            noway_assert(op2 == NULL);
            fgSetTreeSeqFinish(tree);
            return;
        }

        /* Is this a unary operator?
         * Although UNARY GT_IND has a special structure */

        if  (oper == GT_IND)
        {
            /* Visit the indirection first - op2 may point to the
             * jump Label for array-index-out-of-range */

            fgSetTreeSeqHelper(op1);
            fgSetTreeSeqFinish(tree);
            return;
        }

        /* Now this is REALLY a unary operator */

        if  (!op2)
        {
            /* Visit the (only) operand and we're done */

            fgSetTreeSeqHelper(op1);
            fgSetTreeSeqFinish(tree);
            return;
        }

         /*
            For "real" ?: operators, we make sure the order is
            as follows:

                condition
                1st operand
                GT_COLON
                2nd operand
                GT_QMARK
         */

        if  (oper == GT_QMARK)
        {
            noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

            fgSetTreeSeqHelper(op1);
            // Here, for the colon, the sequence does not actually represent "order of evaluation":
            // one or the other of the branches is executed, not both.  Still, to make debugging checks
            // work, we want the sequence to match the order in which we'll generate code, which means
            // "else" clause then "then" clause.
            fgSetTreeSeqHelper(op2->AsColon()->ElseNode());
            fgSetTreeSeqHelper(op2);
            fgSetTreeSeqHelper(op2->AsColon()->ThenNode());

            fgSetTreeSeqFinish(tree);
            return;
        }

        if  (oper == GT_COLON)
        {
            fgSetTreeSeqFinish(tree);
            return;
        }

        /* This is a binary operator */

        if  (tree->gtFlags & GTF_REVERSE_OPS)
        {
            fgSetTreeSeqHelper(op2);
            fgSetTreeSeqHelper(op1);
        }
        else
        {
            fgSetTreeSeqHelper(op1);
            fgSetTreeSeqHelper(op2);
        }

        fgSetTreeSeqFinish(tree);
        return;
    }

    /* See what kind of a special operator we have here */

    switch  (oper)
    {
    case GT_FIELD:
        noway_assert(tree->gtField.gtFldObj == NULL);
        break;

    case GT_CALL:

        /* We'll evaluate the 'this' argument value first */
        if  (tree->gtCall.gtCallObjp)
        {
            fgSetTreeSeqHelper(tree->gtCall.gtCallObjp);
        }

        /* We'll evaluate the arguments next, left to right
         * NOTE: setListOrder needs cleanup - eliminate the #ifdef afterwards */

        if  (tree->gtCall.gtCallArgs)
        {
            fgSetTreeSeqHelper(tree->gtCall.gtCallArgs);
        }

        /* Evaluate the temp register arguments list
         * This is a "hidden" list and its only purpose is to
         * extend the life of temps until we make the call */

        if  (tree->gtCall.gtCallLateArgs)
        {
            fgSetTreeSeqHelper(tree->gtCall.gtCallLateArgs);
        }

        if ((tree->gtCall.gtCallType == CT_INDIRECT) && (tree->gtCall.gtCallCookie != NULL))
        {
            fgSetTreeSeqHelper(tree->gtCall.gtCallCookie);
        }

        if (tree->gtCall.gtCallType == CT_INDIRECT)
        {
            fgSetTreeSeqHelper(tree->gtCall.gtCallAddr);
        }

        if (tree->gtCall.gtControlExpr)
        {
            fgSetTreeSeqHelper(tree->gtCall.gtControlExpr);
        }

        break;

    case GT_ARR_ELEM:

        fgSetTreeSeqHelper(tree->gtArrElem.gtArrObj);

        unsigned dim;
        for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
        {
            fgSetTreeSeqHelper(tree->gtArrElem.gtArrInds[dim]);
        }

        break;

    case GT_ARR_OFFSET:
        fgSetTreeSeqHelper(tree->gtArrOffs.gtOffset);
        fgSetTreeSeqHelper(tree->gtArrOffs.gtIndex);
        fgSetTreeSeqHelper(tree->gtArrOffs.gtArrObj);
        break;

    case GT_CMPXCHG:
        //Evaluate the trees left to right
        fgSetTreeSeqHelper(tree->gtCmpXchg.gtOpLocation);
        fgSetTreeSeqHelper(tree->gtCmpXchg.gtOpValue);
        fgSetTreeSeqHelper(tree->gtCmpXchg.gtOpComparand);
        break;

    case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
    case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        //Evaluate the trees left to right
        fgSetTreeSeqHelper(tree->gtBoundsChk.gtArrLen);
        fgSetTreeSeqHelper(tree->gtBoundsChk.gtIndex);
        break;

    default:
#ifdef DEBUG
        gtDispTree(tree);
        noway_assert(!"unexpected operator");
#endif // DEBUG
        break;
    }

    fgSetTreeSeqFinish(tree);
}

void
Compiler::fgSetTreeSeqFinish(GenTreePtr tree)
{
    /* Append to the node list */
    ++fgTreeSeqNum;

#ifdef DEBUG
    tree->gtSeqNum = fgTreeSeqNum;

    if  (verbose & 0)
    {
        printf("SetTreeOrder: ");
        printTreeID(fgTreeSeqLst);
        printf(" followed by ");
        printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    fgTreeSeqLst->gtNext = tree;
                           tree->gtNext = nullptr;
                           tree->gtPrev = fgTreeSeqLst;
                                          fgTreeSeqLst = tree;

    /* Remember the very first node */

    if  (!fgTreeSeqBeg)
    {
        fgTreeSeqBeg = tree;
        assert(tree->gtSeqNum == 1);
    }
}

/*****************************************************************************
 *
 *  Figure out the order in which operators should be evaluated, along with
 *  other information (such as the register sets trashed by each subtree).
 *  Also finds blocks that need GC polls and inserts them as needed.
 */

void                Compiler::fgSetBlockOrder()
{
#ifdef DEBUG
    if  (verbose)
    {
        printf("*************** In fgSetBlockOrder()\n");
    }
#endif // DEBUG

    /* Walk the basic blocks to assign sequence numbers */

#ifdef DEBUG
    BasicBlock::s_nMaxTrees = 0;
#endif

    /* If we don't compute the doms, then we never mark blocks as loops. */
    if (fgDomsComputed)
    {
        for (BasicBlock * block = fgFirstBB; block; block = block->bbNext)
        {
            /* If this block is a loop header, mark it appropriately */

            if  (block->isLoopHead())
                fgMarkLoopHead(block);
        }
    }
    //only enable fully interruptible code for if we're hijacking.
    else if (GCPOLL_NONE == opts.compGCPollType)
    {
        /* If we don't have the dominators, use an abbreviated test for fully interruptible.  If there are
         * any back edges, check the source and destination blocks to see if they're GC Safe.  If not, then
         * go fully interruptible. */

        /* XXX Mon 1/21/2008
         * Wouldn't it be nice to have a block iterator that can do this loop?
         */
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            //true if the edge is forward, or if it is a back edge and either the source and dest are GC safe.
#define EDGE_IS_GC_SAFE(src, dst) (((src)->bbNum < (dst)->bbNum) || (((src)->bbFlags | (dst)->bbFlags) & BBF_GC_SAFE_POINT))

            bool partiallyInterruptible = true;
            switch (block->bbJumpKind)
            {
            case BBJ_COND:
            case BBJ_ALWAYS:
                partiallyInterruptible = EDGE_IS_GC_SAFE(block, block->bbJumpDest);
                break;

            case BBJ_SWITCH:

                unsigned        jumpCnt; jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock**    jumpPtr; jumpPtr = block->bbJumpSwt->bbsDstTab;

                do
                {
                    partiallyInterruptible &= EDGE_IS_GC_SAFE(block, *jumpPtr);
                }
                while (++jumpPtr, --jumpCnt);

                break;

            default:
                break;
            }

            if (!partiallyInterruptible)
            {
                // DDB 204533:
                // The GC encoding for fully interruptible methods does not
                // support more than 1023 pushed arguments, so we can't set
                // genInterruptible here when we have 1024 or more pushed args
                //
                if (compCanEncodePtrArgCntMax())
                {
                    genInterruptible = true;
                }
                break;
            }
#undef EDGE_IS_GC_SAFE
        }
    }

    if (!fgGCPollsCreated)
    {
        fgCreateGCPolls();
    }

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {

#if FEATURE_FASTTAILCALL
#ifndef JIT32_GCENCODER
        if (block->endsWithTailCallOrJmp(this, true) &&
            !(block->bbFlags & BBF_GC_SAFE_POINT) &&
            optReachWithoutCall(fgFirstBB, block))
        {
            // We have a tail call that is reachable without making any other
            // 'normal' call that would have counted as a GC Poll.  If we were
            // using polls, all return blocks meeting this criteria would have
            // already added polls and then marked as being GC safe
            // (BBF_GC_SAFE_POINT). Thus we can only reach here when *NOT*
            // using GC polls, but instead relying on the JIT to generate
            // fully-interruptible code.
            noway_assert(GCPOLL_NONE == opts.compGCPollType);

            // This tail call might combine with other tail calls to form a
            // loop.  Thus we need to either add a poll, or make the method
            // fully interruptible.  I chose the later because that's what
            // JIT64 does.
            genInterruptible = true;
        }
#endif // !JIT32_GCENCODER
#endif // FEATURE_FASTTAILCALL

        fgSetBlockOrder(block);
    }

    /* Remember that now the tree list is threaded */

    fgStmtListThreaded = true;

#ifdef DEBUG
    if (verbose)
    {
        printf("The biggest BB has %4u tree nodes\n", BasicBlock::s_nMaxTrees);
    }
    fgDebugCheckLinks();
#endif // DEBUG
}


/*****************************************************************************/

void                Compiler::fgSetStmtSeq(GenTreePtr tree)
{
    GenTree         list;            // helper node that we use to start the StmtList
                                     // It's located in front of the first node in the list

    noway_assert(tree->gtOper == GT_STMT);
    noway_assert(tree->gtNext == nullptr || tree->gtNext->gtFlags & GTF_STMT_TOP_LEVEL);

    /* Assign numbers and next/prev links for this tree */

    fgTreeSeqNum = 0;
    fgTreeSeqLst = &list;
    fgTreeSeqBeg = NULL;

    fgSetTreeSeqHelper(tree->gtStmt.gtStmtExpr);

    /* Record the address of the first node */

    tree->gtStmt.gtStmtList = fgTreeSeqBeg;

#ifdef DEBUG

    if  (list.gtNext->gtPrev != &list)
    {
        printf("&list ");
        printTreeID(&list);
        printf(" != list.next->prev ");
        printTreeID(list.gtNext->gtPrev);
        printf("\n");
        goto BAD_LIST;
    }

    GenTreePtr temp;
    GenTreePtr last;
    for (temp = list.gtNext, last = &list; temp; last = temp, temp = temp->gtNext)
    {
        if (temp->gtPrev != last)
        {
            printTreeID(temp);
            printf("->gtPrev = ");
            printTreeID(temp->gtPrev);
            printf(", but last = ");
            printTreeID(last);
            printf("\n");

BAD_LIST:;

            printf("\n");
            gtDispTree(tree->gtStmt.gtStmtExpr);
            printf("\n");

            for (GenTreePtr bad = &list; bad; bad = bad->gtNext)
            {
                printf("  entry at ");
                printTreeID(bad);
                printf(" (prev=");
                printTreeID(bad->gtPrev);
                printf(",next=)");
                printTreeID(bad->gtNext);
                printf("\n");
            }

            printf("\n");
            noway_assert(!"Badly linked tree");
            break;
        }
    }
#endif // DEBUG

    /* Fix the first node's 'prev' link */

    noway_assert(list.gtNext->gtPrev == &list);
    list.gtNext->gtPrev = NULL;

    /* Keep track of the highest # of tree nodes */

#ifdef DEBUG
    if  (BasicBlock::s_nMaxTrees < fgTreeSeqNum)
    {
         BasicBlock::s_nMaxTrees = fgTreeSeqNum;
    }
#endif // DEBUG

}

/*****************************************************************************/

void                Compiler::fgSetBlockOrder(BasicBlock* block)
{
    GenTreePtr      tree;

    tree = block->bbTreeList;
    if  (!tree)
        return;

    for (;;)
    {
        fgSetStmtSeq(tree);

        /* Are there any more trees in this basic block? */

        if (tree->gtNext == nullptr)
        {
            /* last statement in the tree list */
            noway_assert(block->lastStmt() == tree);
            break;
        }

#ifdef DEBUG
        if (block->bbTreeList == tree)
        {
            /* first statement in the list */
            noway_assert(tree->gtPrev->gtNext == nullptr);
        }
        else
        {
            noway_assert(tree->gtPrev->gtNext == tree);
        }

        noway_assert(tree->gtNext->gtPrev == tree);
#endif // DEBUG

        tree = tree->gtNext;
    }
}

/*****************************************************************************
 *
 * For GT_INITBLK and GT_COPYBLK, the tree looks like this :
 *                                tree->gtOp
 *                                 /    \
 *                               /        \.
 *                           GT_LIST  [size/clsHnd]
 *                            /    \
 *                           /      \
 *                       [dest]     [val/src]
 *
 * ie. they are ternary operators. However we use nested binary trees so that
 * GTF_REVERSE_OPS will be set just like for other binary operators. As the
 * operands need to end up in specific registers to issue the "rep stos" or
 * the "rep movs" instruction, if we don't allow the order of evaluation of
 * the 3 operands to be mixed, we may generate really bad code.
 *
 * eg. For "rep stos", [val] has to be in EAX. Then if [size]
 * has a division, we will have to spill [val] from EAX. It will be better to
 * evaluate [size] and the evaluate [val] into EAX.
 *
 * This function stores the operands in the order to be evaluated
 * into opsPtr[]. The regsPtr[] contains reg0,reg1,reg2 in the correspondingly
 * switched order.
 */

void            Compiler::fgOrderBlockOps(GenTreePtr   tree,
                                          regMaskTP    reg0,
                                          regMaskTP    reg1,
                                          regMaskTP    reg2,
                                          GenTreePtr*  opsPtr,   // OUT
                                          regMaskTP*   regsPtr)  // OUT
{
    assert(tree->OperIsBlkOp());

    assert(tree->gtOp.gtOp1 && tree->gtOp.gtOp1->IsList());
    assert(tree->gtOp.gtOp1->gtOp.gtOp1 && tree->gtOp.gtOp1->gtOp.gtOp2);
    assert(tree->gtOp.gtOp2);

    GenTreePtr ops[3] =
    {
        tree->gtOp.gtOp1->gtOp.gtOp1,       // Dest address
        tree->gtOp.gtOp1->gtOp.gtOp2,       // Val / Src address
        tree->gtOp.gtOp2                    // Size of block
    };

    regMaskTP regs[3] = { reg0, reg1, reg2 };

    static int blockOpsOrder[4][3] =
                        //      tree->gtFlags    |  tree->gtOp.gtOp1->gtFlags
    {                   //  ---------------------+----------------------------
        { 0, 1, 2 },    //           -           |              -
        { 2, 0, 1 },    //     GTF_REVERSE_OPS   |              -
        { 1, 0, 2 },    //           -           |       GTF_REVERSE_OPS
        { 2, 1, 0 }     //     GTF_REVERSE_OPS   |       GTF_REVERSE_OPS
    };

    int orderNum =              ((tree->gtFlags & GTF_REVERSE_OPS) != 0) * 1 +
                    ((tree->gtOp.gtOp1->gtFlags & GTF_REVERSE_OPS) != 0) * 2;

    assert(orderNum < 4);

    int* order = blockOpsOrder[orderNum];

    PREFIX_ASSUME(order != NULL);

    // Fill in the OUT arrays according to the order we have selected

     opsPtr[0]  =  ops[ order[0] ];
     opsPtr[1]  =  ops[ order[1] ];
     opsPtr[2]  =  ops[ order[2] ];

    regsPtr[0]  = regs[ order[0] ];
    regsPtr[1]  = regs[ order[1] ];
    regsPtr[2]  = regs[ order[2] ];
}

//------------------------------------------------------------------------
// fgFindTopLevelStmtBackwards: Find the nearest top-level statement to 'stmt', walking the gtPrev links.
//      The nearest one might be 'stmt' itself.
//
// Arguments:
//    stmt - The statment to start the search with.
//
// Return Value:
//    The nearest top-level statement, walking backwards.
//
// Assumptions:
//    We will find one!

/* static */
GenTreeStmt* Compiler::fgFindTopLevelStmtBackwards(GenTreeStmt* stmt)
{
    while (!stmt->gtStmtIsTopLevel())
    {
        stmt = stmt->gtPrev->AsStmt();
    }
    return stmt;
}

//------------------------------------------------------------------------
// fgGetFirstNode: Get the first node in the tree, in execution order
//
// Arguments:
//    tree - The top node of the tree of interest
//
// Return Value:
//    The first node in execution order, that belongs to tree.
//
// Assumptions:
//     'tree' must either be a leaf, or all of its constituent nodes must be contiguous
//     in execution order.
//     TODO-Cleanup: Add a debug-only method that verifies this.

/* static */
GenTreePtr Compiler::fgGetFirstNode(GenTreePtr tree)
{
    GenTreePtr child = tree;
    while (child->NumChildren() > 0)
    {
        if (child->OperIsBinary() && ((child->gtFlags & GTF_REVERSE_OPS) != 0))
        {
            child = child->GetChild(1);
        }
        else
        {
            child = child->GetChild(0);
        }
    }
    return child;
}

//------------------------------------------------------------------------
// fgSnipNode: Remove a single tree node (and not its children, if any) from the execution order.
//
// Arguments:
//    'stmt'    - The statement which currently contains 'node'
//    'node'    - The tree node to be removed
//
// Return Value:
//    None.
//
// Assumptions:
//    'stmt' must be non-null.
//
// Notes:
//    The node may be any node in the statement, including the first or last node in the statement.
//    This is similar to fgDeleteTreeFromList(), but it removes just a single node, not a whole tree.

void Compiler::fgSnipNode(GenTreeStmt* stmt, GenTreePtr node)
{
    assert(stmt != nullptr);
    assert(node != nullptr);
    assert(stmt->gtOper == GT_STMT);
    assert(node->gtOper != GT_STMT);
    assert(fgTreeIsInStmt(node, stmt));

    GenTreePtr prevNode = node->gtPrev;
    GenTreePtr nextNode = node->gtNext;

    if (prevNode != nullptr)
    {
        prevNode->gtNext = nextNode;
    }
    else
    {
        // The node is the first in the statement in execution order.
        assert(stmt->gtStmtList == node);
    }

    // Note that the node may be first but also have a prevNode (if it is embedded)
    if (stmt->gtStmtList == node)
    {
        stmt->gtStmtList = nextNode;
    }

    if (nextNode != nullptr)
    {
        nextNode->gtPrev = prevNode;
    }
    else
    {
        // The node is the last in the statement in execution order.
        assert(stmt->gtStmtExpr == node);
        stmt->gtStmtExpr = prevNode;
    }
}

//------------------------------------------------------------------------
// fgSnipInnerNode: Remove a single tree node (and not its children, if any) from the execution order.
//
// Arguments:
//    'node'    - The tree node to be removed
//
// Return Value:
//    None.
//
// Assumptions:
//    The node may not be the first or last node in the statement. In those cases, fgSnipNode() must be used,
//    which gets passed the parent GT_STMT node and can update gtStmtList and gtStmtExpr, respectively.

/* static */
void Compiler::fgSnipInnerNode(GenTreePtr node)
{
    assert(node != nullptr);
    assert(node->gtOper != GT_STMT);

    GenTreePtr prevNode = node->gtPrev;
    GenTreePtr nextNode = node->gtNext;
    assert(prevNode != nullptr);
    assert(nextNode != nullptr);
    prevNode->gtNext = nextNode;
    nextNode->gtPrev = prevNode;
}

//------------------------------------------------------------------------
// fgDeleteTreeFromList: Remove an entire tree from the execution order.
//
// Arguments:
//    'stmt'    - The statement which currently contains 'tree'
//    'tree'    - The tree to be removed
//
// Return Value:
//    None.
//
// Assumptions:
//    'tree' is in the execution order list for 'stmt'
//
// Notes:
//    This is similar to fgSnipNode(), but it removes a whole tree, not just a single node.

void Compiler::fgDeleteTreeFromList(GenTreeStmt* stmt, GenTreePtr tree)
{
    assert(stmt != nullptr);
    assert(tree != nullptr);
    assert(stmt->gtOper == GT_STMT);
    assert(tree->gtOper != GT_STMT);
    assert(fgTreeIsInStmt(tree, stmt));

    GenTreePtr firstNode = fgGetFirstNode(tree);
    GenTreePtr prevNode = firstNode->gtPrev;
    GenTreePtr nextNode = tree->gtNext;

    if (prevNode != nullptr)
    {
        prevNode->gtNext = nextNode;
    }
    else
    {
        // The first node in the tree is the first in the statement in execution order.
        assert(stmt->gtStmtList == firstNode);
        stmt->gtStmtList = nextNode;
    }

    if (nextNode != nullptr)
    {
        nextNode->gtPrev = prevNode;
    }
    else
    {
        // The last node in the tree is the last in the statement in execution order.
        assert(stmt->gtStmtExpr == tree);
        stmt->gtStmtExpr = prevNode;
    }
}


//------------------------------------------------------------------------
// fgTreeIsInStmt: return 'true' if 'tree' is in the execution order list of statment 'stmt'.
// This works for a single node or an entire tree, assuming a well-formed tree, where the entire
// tree's set of nodes are in the statement execution order list.
//
/* static */
bool Compiler::fgTreeIsInStmt(GenTree* tree, GenTreeStmt* stmt)
{
    assert(tree != nullptr);
    assert(stmt != nullptr);
    assert(tree->gtOper != GT_STMT);
    assert(stmt->gtOper == GT_STMT);
    for (GenTree* curr = stmt->gtStmtList; curr != nullptr; curr = curr->gtNext)
    {
        if (tree == curr)
            return true;
    }
    return false;
}

//------------------------------------------------------------------------
// fgInsertTreeInListAfter: Insert 'tree' in the execution order list before 'insertionPoint'.
// 'stmt' is required, so we can insert before the first node in the statement.
// Assumes that 'tree' and its children are disjoint from 'insertionPoint', and none of them are in 'stmt'.
//
/* static */
void Compiler::fgInsertTreeInListBefore(GenTree* tree, GenTree* insertionPoint, GenTreeStmt* stmt)
{
    assert(tree != nullptr);
    assert(insertionPoint != nullptr);
    assert(stmt != nullptr);
    assert(tree->gtOper != GT_STMT);
    assert(insertionPoint->gtOper != GT_STMT);
    assert(fgTreeIsInStmt(insertionPoint, stmt));
    assert(!fgTreeIsInStmt(tree, stmt));

    GenTree* beforeTree = insertionPoint->gtPrev;

    insertionPoint->gtPrev = tree;
    tree->gtNext = insertionPoint;

    GenTree* first = fgGetFirstNode(tree);

    first->gtPrev = beforeTree;

    if (beforeTree != nullptr)
    {
        beforeTree->gtNext = first;
    }
    else
    {
        assert(stmt->gtStmtList == insertionPoint);
        stmt->gtStmtList = first;
    }
}

//------------------------------------------------------------------------
// fgInsertTreeInListAfter: Insert tree in execution order list after 'insertionPoint'.
// 'stmt' is required, so we can insert after the last node in the statement.
// Assumes that 'tree' and its children are disjoint from 'insertionPoint', and none of them are in 'stmt'.
//
/* static */
void Compiler::fgInsertTreeInListAfter(GenTree* tree, GenTree* insertionPoint, GenTreeStmt* stmt)
{
    assert(tree != nullptr);
    assert(insertionPoint != nullptr);
    assert(stmt != nullptr);
    assert(tree->gtOper != GT_STMT);
    assert(insertionPoint->gtOper != GT_STMT);
    assert(fgTreeIsInStmt(insertionPoint, stmt));
    assert(!fgTreeIsInStmt(tree, stmt));

    GenTree* afterTree = insertionPoint->gtNext;
    GenTree* first = fgGetFirstNode(tree);

    insertionPoint->gtNext = first;
    first->gtPrev = insertionPoint;

    tree->gtNext = afterTree;

    if (afterTree != nullptr)
    {
        afterTree->gtPrev = tree;
    }
    else
    {
        assert(stmt->gtStmtExpr == insertionPoint);
        stmt->gtStmtExpr = tree;
    }
}

//------------------------------------------------------------------------
// fgInsertTreeBeforeAsEmbedded: Insert a tree before 'insertionPoint' as an embedded statement under 'stmt'.
//
GenTreeStmt* Compiler::fgInsertTreeBeforeAsEmbedded(GenTree* tree, GenTree* insertionPoint, GenTreeStmt* stmt, BasicBlock* block)
{
    assert(tree->gtOper != GT_STMT);
    assert(insertionPoint->gtOper != GT_STMT);
    assert(stmt != nullptr);
    assert(stmt->gtOper == GT_STMT);
    assert(fgTreeIsInStmt(insertionPoint, stmt));
    assert(!fgTreeIsInStmt(tree, stmt));

    gtSetEvalOrder(tree);
    fgSetTreeSeq(tree);
    fgInsertTreeInListBefore(tree, insertionPoint, stmt);

    // While inserting a statement as embedded, the parent specified has to be a top-level statement
    // since we could be inserting it ahead of an already existing embedded statment
    // in execution order.
    GenTreeStmt* topStmt = fgFindTopLevelStmtBackwards(stmt);
    GenTreeStmt* result = fgMakeEmbeddedStmt(block, tree, topStmt);

    DBEXEC(true, fgDebugCheckNodeLinks(block, result));
    return result;
}

//------------------------------------------------------------------------
// fgInsertTreeAfterAsEmbedded: Insert a tree after 'insertionPoint' as an embedded statement under 'stmt'.
// If it is inserted after all nodes in the given tree, just make it a new statement.
GenTreeStmt* Compiler::fgInsertTreeAfterAsEmbedded(GenTree* tree, GenTree* insertionPoint, GenTreeStmt* stmt, BasicBlock* block)
{
    assert(tree->gtOper != GT_STMT);
    assert(insertionPoint->gtOper != GT_STMT);
    assert(stmt != nullptr);
    assert(stmt->gtOper == GT_STMT);
    assert(fgTreeIsInStmt(insertionPoint, stmt));
    assert(!fgTreeIsInStmt(tree, stmt));

    GenTreeStmt* result;

    if (insertionPoint->gtNext == nullptr)
    {
        // We're just going to make it a new top-level statement, not an embedded statement,
        // since we're inserting it at the end of the statement list's execution order.

        // we better have been given the right stmt
        assert(insertionPoint == stmt->gtStmtExpr);

        // this sets the sequence
        result = fgNewStmtFromTree(tree, block);

        // Skip all the embedded statements within 'stmt' (which immediately follow 'stmt' in the statement list).
        // Insert after the last such embedded statement.
        GenTreeStmt* stmtAfter = stmt;
        while ((stmtAfter->gtNext != nullptr) && stmtAfter->gtNextStmt->gtStmtIsEmbedded())
        {
            stmtAfter = stmtAfter->gtNextStmt;
        }

        fgInsertStmtAfter(block, stmtAfter, result);
    }
    else
    {
        gtSetEvalOrder(tree);
        fgSetTreeSeq(tree);
        fgInsertTreeInListAfter(tree, insertionPoint, stmt);
        result = fgMakeEmbeddedStmt(block, tree, stmt);
    }

    DBEXEC(true, fgDebugCheckNodeLinks(block, result));
    return result;
}


// Examine the bbTreeList and return the estimated code size for this block
unsigned Compiler::fgGetCodeEstimate(BasicBlock* block)
{
    unsigned costSz = 0;     // estimate of blocks code size cost

    switch (block->bbJumpKind)
    {
    case BBJ_NONE:
        costSz = 0;
        break;
    case BBJ_ALWAYS:
    case BBJ_EHCATCHRET:
    case BBJ_LEAVE:
    case BBJ_COND:
        costSz = 2;
        break;
    case BBJ_CALLFINALLY:
        costSz = 5;
        break;
    case BBJ_SWITCH:
        costSz = 10;
        break;
    case BBJ_THROW:
        costSz = 1;     // We place a int3 after the code for a throw block
        break;
    case BBJ_EHFINALLYRET:
    case BBJ_EHFILTERRET:
        costSz = 1;
        break;
    case BBJ_RETURN:    // return from method
        costSz = 3;
        break;
    default:
        noway_assert(!"Bad bbJumpKind");
        break;
    }

#if JIT_FEATURE_SSA_SKIP_DEFS
    GenTreePtr  tree = block->FirstNonPhiDef();
#else
    GenTreePtr  tree = block->bbTreeList;
#endif

    if  (tree)
    {
        do
        {
            noway_assert(tree->gtOper == GT_STMT);

            if (tree->gtCostSz < MAX_COST)
            {
                costSz += tree->gtCostSz;
            }
            else
            {
                // We could walk the tree to find out the real gtCostSz,
                // but just using MAX_COST for this trees code size works OK
                costSz += tree->gtCostSz;
            }

            tree = tree->gtNext;
        }
        while (tree);
    }

    return costSz;
}

#if DUMP_FLOWGRAPHS

struct escapeMapping_t
{
    char        ch;
    const char* sub;
};

static escapeMapping_t s_EscapeFileMapping[] =
{
    {':', "="},
    {'<', "["},
    {'>', "]"},
    {';', "~semi~"},
    {'|', "~bar~"},
    {'&', "~amp~"},
    {'"', "~quot~"},
    {'*', "~star~"},
    {0, 0}
};

static escapeMapping_t s_EscapeMapping[] =
{
    {'<', "&lt;"},
    {'>', "&gt;"},
    {'&', "&amp;"},
    {'"', "&quot;"},
    {0, 0}
};

const char*   Compiler::fgProcessEscapes(const char* nameIn, escapeMapping_t* map)
{
    const char*  nameOut = nameIn;
    unsigned     lengthOut;
    unsigned     index;
    bool         match;
    bool         subsitutionRequired;
    const char*  pChar;

    lengthOut = 1;
    subsitutionRequired = false;
    pChar = nameIn;
    while (*pChar != '\0')
    {
        match = false;
        index = 0;
        while (map[index].ch != 0)
        {
            if (*pChar == map[index].ch)
            {
                match = true;
                break;
            }
            index++;
        }
        if (match)
        {
            subsitutionRequired = true;
            lengthOut += (unsigned)strlen(map[index].sub);
        }
        else
        {
            lengthOut += 1;
        }
        pChar++;
    }

    if (subsitutionRequired)
    {
        char*   newName = (char*) compGetMemA(lengthOut, CMK_DebugOnly);
        char*   pDest;
        pDest = newName;
        pChar = nameIn;
        while (*pChar != '\0')
        {
            match = false;
            index = 0;
            while (map[index].ch != 0)
            {
                if (*pChar == map[index].ch)
                {
                    match = true;
                    break;
                }
                index++;
            }
            if (match)
            {
                strcpy(pDest, map[index].sub);
                pDest += strlen(map[index].sub);
            }
            else
            {
                *pDest++ = *pChar;
            }
            pChar++;
        }
        *pDest++ = '\0';
        nameOut = (const char*) newName;
    }

    return nameOut;
}

static void fprintfDouble(FILE* fgxFile, double value)
{
    assert(value >= 0.0);

    if ((value >= 0.010) || (value == 0.0))
    {
        fprintf(fgxFile, "\"%7.3f\"", value);
    }
    else if (value >= 0.00010)
    {
        fprintf(fgxFile, "\"%7.5f\"", value);
    }
    else
    {
        fprintf(fgxFile, "\"%7E\"", value);
    }
}

//------------------------------------------------------------------------
// fgOpenFlowGraphFile: Open a file to dump either the xml or dot format flow graph
//
// Arguments:
//    wbDontClose - A boolean out argument that indicates whether the caller should close the file
//    phase       - A phase identifier to indicate which phase is associated with the dump
//    type        - A (wide) string indicating the type of dump, "dot" or "xml"
//
// Return Value:
//    Opens a file to which a flowgraph can be dumped, whose name is based on the current
//    config vales.

FILE*              Compiler::fgOpenFlowGraphFile(bool*  wbDontClose, Phases phase, LPCWSTR type)
{
    FILE*          fgxFile;
    LPCWSTR        pattern  = NULL;
    LPCWSTR        filename = NULL;
    LPCWSTR        pathname = NULL;
    const char*    escapedString;
    bool           createDuplicateFgxFiles = true;

#ifdef DEBUG
    if (opts.eeFlags & CORJIT_FLG_PREJIT)
    {
        pattern = JitConfig.NgenDumpFg();
        filename = JitConfig.NgenDumpFgFile();
        pathname = JitConfig.NgenDumpFgDir();
    }
    else
    {
        pattern = JitConfig.JitDumpFg();
        filename = JitConfig.JitDumpFgFile();
        pathname = JitConfig.JitDumpFgDir();
    }
#endif // DEBUG

    if (fgBBcount <= 1)
        return NULL;

    if (pattern == NULL)
        return NULL;

    if (wcslen(pattern) == 0)
        return NULL;

    LPCWSTR phasePattern = JitConfig.JitDumpFgPhase();
    LPCWSTR phaseName = PhaseShortNames[phase];
    if (phasePattern == 0)
    {
        if (phase != PHASE_DETERMINE_FIRST_COLD_BLOCK)
        {
            return nullptr;
        }
    }
    else if (*phasePattern != W('*'))
    {
        if (wcsstr(phasePattern, phaseName) == nullptr)
        {
            return nullptr;
        }
    }

    if (*pattern != W('*'))
    {
        bool hasColon = (wcschr(pattern, W(':')) != 0);

        if (hasColon)
        {
            const char* className = info.compClassName;
            if (*pattern == W('*'))
            {
                pattern++;
            }
            else
            {
                while ((*pattern != W(':')) && (*pattern != W('*')))
                {
                    if (*pattern != *className)
                        return NULL;

                    pattern++;
                    className++;
                }
                if (*pattern == W('*'))
                {
                    pattern++;
                }
                else
                {
                    if (*className != 0)
                        return NULL;
                }
            }
            if (*pattern != W(':'))
                return NULL;

            pattern++;
        }

        const char* methodName = info.compMethodName;
        if (*pattern == W('*'))
        {
            pattern++;
        }
        else
        {
            while ((*pattern != 0) && (*pattern != W('*')))
            {
                if (*pattern != *methodName)
                    return NULL;

                pattern++;
                methodName++;
            }
            if (*pattern == W('*'))
            {
                pattern++;
            }
            else
            {
                if (*methodName != 0)
                    return NULL;
            }
        }
        if (*pattern != 0)
            return NULL;
    }

    if (filename == NULL)
    {
        filename = W("default");
    }

    if (wcscmp(filename, W("profiled")) == 0)
    {
        if ((fgFirstBB->bbFlags & BBF_PROF_WEIGHT) != 0)
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return NULL;
        }
    }
    if (wcscmp(filename, W("hot")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_HOT)

        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return NULL;
        }
    }
    else if (wcscmp(filename, W("cold")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_COLD)
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return NULL;
        }
    }
    else if (wcscmp(filename, W("jit")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_JIT)
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return NULL;
        }
    }
    else if (wcscmp(filename, W("all")) == 0)
    {
        createDuplicateFgxFiles = true;

ONE_FILE_PER_METHOD:;

        escapedString = fgProcessEscapes(info.compFullName, s_EscapeFileMapping);
        size_t wCharCount = strlen(escapedString) + wcslen(phaseName) + 1 + strlen("~999") + wcslen(type) + 1;
        if (pathname != NULL)
        {
            wCharCount += wcslen(pathname) + 1;
        }
        filename = (LPCWSTR) alloca(wCharCount * sizeof(WCHAR));
        if (pathname != NULL)
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%S-%s.%s"), pathname, escapedString, phaseName, type);
        }
        else
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%S.%s"), escapedString, type);
        }
        fgxFile = _wfopen(filename, W("r"));   // Check if this file already exists
        if (fgxFile != NULL)
        {
            // For Generic methods we will have both hot and cold versions
            if (createDuplicateFgxFiles == false)
            {
                fclose(fgxFile);
                return NULL;
            }
            // Yes, this filename already exists, so create a different one by appending ~2, ~3, etc...
            for (int i = 2; i < 1000; i++)
            {
                fclose(fgxFile);
                if (pathname != NULL)
                {
                    swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%S~%d.%s"), pathname, escapedString, i, type);
                }
                else
                {
                    swprintf_s((LPWSTR)filename, wCharCount, W("%S~%d.%s"), escapedString, i, type);
                }
                fgxFile = _wfopen(filename, W("r"));   // Check if this file exists
                if (fgxFile == NULL)
                    break;
            }
            // If we have already created 1000 files with this name then just fail
            if (fgxFile != NULL)
            {
                fclose(fgxFile);
                return NULL;
            }
        }
        fgxFile = _wfopen(filename, W("a+"));
        *wbDontClose = false;
    }
    else if (wcscmp(filename, W("stdout")) == 0)
    {
        fgxFile = stdout;
        *wbDontClose = true;
    }
    else if (wcscmp(filename, W("stderr")) == 0)
    {
        fgxFile = stderr;
        *wbDontClose = true;
    }
    else
    {
        LPCWSTR origFilename = filename;
        size_t wCharCount = wcslen(origFilename) + wcslen(type) + 2;
        if (pathname != NULL)
        {
            wCharCount += wcslen(pathname) + 1;
        }
        filename = (LPCWSTR) alloca(wCharCount * sizeof(WCHAR));
        if (pathname != NULL)
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%s.%s"), pathname, origFilename, type);
        }
        else
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s.%s"), origFilename, type);
        }
        fgxFile = _wfopen(filename, W("a+"));
        *wbDontClose = false;
    }

    return fgxFile;
}

//------------------------------------------------------------------------
// fgDumpFlowGraph: Dump the xml or dot format flow graph, if enabled for this phase.
//
// Arguments:
//    phase       - A phase identifier to indicate which phase is associated with the dump,
//                  i.e. which phase has just completed.
//
// Return Value:
//    True iff a flowgraph has been dumped.
//
// Notes:
//    The xml dumps are the historical mechanism for dumping the flowgraph.
//    The dot format can be viewed by:
//    - Graphviz (http://www.graphviz.org/)
//      - The command "C:\Program Files (x86)\Graphviz2.38\bin\dot.exe" -Tsvg -oFoo.svg -Kdot Foo.dot
//        will produce a Foo.svg file that can be opened with any svg-capable browser (e.g. IE).
//    - http://rise4fun.com/Agl/
//      - Cut and paste the graph from your .dot file, replacing the digraph on the page, and then click the play button.
//      - It will show a rotating '/' and then render the graph in the browser.
//    MSAGL has also been open-sourced to https://github.com/Microsoft/automatic-graph-layout.git.
//
//    Here are the config values that control it:
//      COMPLUS_JitDumpFg       A string (ala the COMPLUS_JitDump string) indicating what methods to dump flowgraphs for.
//      COMPLUS_JitDumpFgDir    A path to a directory into which the flowgraphs will be dumped.
//      COMPLUS_JitDumpFgFile   The filename to use. The default is "default.[xml|dot]".
//                              Note that the new graphs will be appended to this file if it already exists.
//      COMPLUS_JitDumpFgPhase  Phase(s) after which to dump the flowgraph.
//                              Set to the short name of a phase to see the flowgraph after that phase.
//                              Leave unset to dump after COLD-BLK (determine first cold block) or set to * for all phases.
//      COMPLUS_JitDumpFgDot    Set to non-zero to emit Dot instead of Xml Flowgraph dump. (Default is xml format.)

bool               Compiler::fgDumpFlowGraph(Phases phase)
{
    bool    result    = false;
    bool    dontClose = false;
    bool    createDotFile = false;
    if (JitConfig.JitDumpFgDot())
    {
        createDotFile = true;
    }
            
    FILE*   fgxFile   = fgOpenFlowGraphFile(&dontClose, phase, createDotFile ? W("dot") : W("fgx"));

    if (fgxFile == NULL)
    {
        return false;
    }
    bool           validWeights  = fgHaveValidEdgeWeights;
    unsigned       calledCount   = max(fgCalledWeight, BB_UNITY_WEIGHT) / BB_UNITY_WEIGHT;
    double         weightDivisor = (double) (calledCount * BB_UNITY_WEIGHT);
    const char*    escapedString;
    const char*    regionString  = "NONE";

    if      (info.compMethodInfo->regionKind == CORINFO_REGION_HOT)
    {
        regionString="HOT";
    }
    else if (info.compMethodInfo->regionKind == CORINFO_REGION_COLD)
    {
        regionString="COLD";
    }
    else if (info.compMethodInfo->regionKind == CORINFO_REGION_JIT)
    {
        regionString="JIT";
    }

    if (createDotFile)
    {
        fprintf(fgxFile, "digraph %s\n{\n", info.compMethodName);
        fprintf(fgxFile, "/* Method %d, after phase %s */", Compiler::jitTotalMethodCompiled, PhaseNames[phase]);
    }
    else
    {
        fprintf(fgxFile,   "<method");

        escapedString = fgProcessEscapes(info.compFullName, s_EscapeMapping);
        fprintf(fgxFile, "\n    name=\"%s\"", escapedString);

        escapedString = fgProcessEscapes(info.compClassName, s_EscapeMapping);
        fprintf(fgxFile, "\n    className=\"%s\"", escapedString);

        escapedString = fgProcessEscapes(info.compMethodName, s_EscapeMapping);
        fprintf(fgxFile, "\n    methodName=\"%s\"", escapedString);
        fprintf(fgxFile, "\n    ngenRegion=\"%s\"", regionString);

        fprintf(fgxFile, "\n    bytesOfIL=\"%d\"", info.compILCodeSize);
        fprintf(fgxFile, "\n    localVarCount=\"%d\"", lvaCount);

        if (fgHaveProfileData())
        {
            fprintf(fgxFile, "\n    calledCount=\"%d\"", calledCount);
            fprintf(fgxFile, "\n    profileData=\"true\"");
        }
        if (compHndBBtabCount > 0)
        {
            fprintf(fgxFile, "\n    hasEHRegions=\"true\"");
        }
        if (fgHasLoops)
        {
            fprintf(fgxFile, "\n    hasLoops=\"true\"");
        }
        if (validWeights)
        {
            fprintf(fgxFile, "\n    validEdgeWeights=\"true\"");
            if (!fgSlopUsedInEdgeWeights && !fgRangeUsedInEdgeWeights)
            {
                fprintf(fgxFile, "\n    exactEdgeWeights=\"true\"");
            }
        }
        if (fgFirstColdBlock != NULL)
        {
            fprintf(fgxFile, "\n    firstColdBlock=\"%d\"", fgFirstColdBlock->bbNum);
        }

        fprintf(fgxFile,        ">");

        fprintf(fgxFile, "\n    <blocks");
        fprintf(fgxFile, "\n        blockCount=\"%d\"", fgBBcount);
        fprintf(fgxFile,            ">");
    }

    static const char* kindImage[] = { "EHFINALLYRET", "EHFILTERRET", "EHCATCHRET", 
                                       "THROW", "RETURN", "NONE", "ALWAYS", "LEAVE",
                                       "CALLFINALLY", "COND", "SWITCH" };

    BasicBlock* block;
    unsigned    blockOrdinal;
    for (block = fgFirstBB    , blockOrdinal = 1;
         block != NULL;
         block = block->bbNext, blockOrdinal++)
    {
        if (createDotFile)
        {
            // Add constraint edges to try to keep nodes ordered.
            // It seems to work best if these edges are all created first.
            switch(block->bbJumpKind)
            {
            case BBJ_COND:
            case BBJ_NONE:
                assert(block->bbNext != nullptr);
                fprintf(fgxFile, "    BB%02u -> BB%02u\n", block->bbNum, block->bbNext->bbNum);
                break;
            default:
                // These may or may not have an edge to the next block.
                // Add a transparent edge to keep nodes ordered.
                if (block->bbNext != nullptr)
                {
                    fprintf(fgxFile, "    BB%02u -> BB%02u [arrowtail=none,color=transparent]\n", block->bbNum, block->bbNext->bbNum);
                }
            }
        }
        else
        {
            fprintf(fgxFile,"\n        <block");
            fprintf(fgxFile,"\n            id=\"%d\"", block->bbNum);
            fprintf(fgxFile,"\n            ordinal=\"%d\"", blockOrdinal);
            fprintf(fgxFile,"\n            jumpKind=\"%s\"", kindImage[block->bbJumpKind]);
            if (block->hasTryIndex())
            {
                fprintf(fgxFile,"\n            inTry=\"%s\"", "true");
            }
            if (block->hasHndIndex())
            {
                fprintf(fgxFile,"\n            inHandler=\"%s\"", "true");
            }
            if (((fgFirstBB->bbFlags & BBF_PROF_WEIGHT) != 0) &&
                ((block->bbFlags     & BBF_COLD)        == 0)    )
            {
                fprintf(fgxFile,"\n            hot=\"true\"");
            }
            if (block->bbFlags & (BBF_HAS_NEWOBJ | BBF_HAS_NEWARRAY))
            {
                fprintf(fgxFile,"\n            callsNew=\"true\"");
            }
            if (block->bbFlags & BBF_LOOP_HEAD)
            {
                fprintf(fgxFile,"\n            loopHead=\"true\"");
            }
            fprintf(fgxFile,"\n            weight=");
            fprintfDouble(fgxFile, ((double) block->bbWeight) / weightDivisor);
            fprintf(fgxFile,"\n            codeEstimate=\"%d\"", fgGetCodeEstimate(block));
            fprintf(fgxFile,"\n            startOffset=\"%d\"", block->bbCodeOffs);
            fprintf(fgxFile,"\n            endOffset=\"%d\"", block->bbCodeOffsEnd);
            fprintf(fgxFile,               ">");
            fprintf(fgxFile,"\n        </block>");
        }
    }

    if (!createDotFile)
    {
        fprintf(fgxFile, "\n    </blocks>");

        fprintf(fgxFile, "\n    <edges");
        fprintf(fgxFile, "\n        edgeCount=\"%d\"", fgEdgeCount);
        fprintf(fgxFile,            ">");
    }

    unsigned edgeNum = 1;
    BasicBlock* bTarget;
    for (bTarget = fgFirstBB; bTarget != NULL; bTarget = bTarget->bbNext)
    {
        double targetWeightDivisor;
        if (bTarget->bbWeight == BB_ZERO_WEIGHT)
        {
            targetWeightDivisor = 1.0;
        }
        else
        {
            targetWeightDivisor = (double) bTarget->bbWeight;
        }

        flowList* edge;
        for (edge = bTarget->bbPreds; edge != NULL; edge = edge->flNext, edgeNum++)
        {
            BasicBlock*  bSource = edge->flBlock;
            double       sourceWeightDivisor;
            if (bSource->bbWeight == BB_ZERO_WEIGHT)
            {
                sourceWeightDivisor = 1.0;
            }
            else
            {
                sourceWeightDivisor = (double) bSource->bbWeight;
            }
            if (createDotFile)
            {
                // Don't duplicate the edges we added above.
                if ((bSource->bbNum == (bTarget->bbNum - 1)) &&
                    ((bSource->bbJumpKind == BBJ_NONE) || (bSource->bbJumpKind == BBJ_COND)))
                {
                    continue;
                }
                fprintf(fgxFile, "    BB%02u -> BB%02u", bSource->bbNum, bTarget->bbNum);
                if ((bSource->bbNum > bTarget->bbNum))
                {
                    fprintf(fgxFile, "[arrowhead=normal,arrowtail=none,color=green]\n");
                }
                else
                {
                    fprintf(fgxFile, "\n");
                }
            }
            else
            {
                fprintf(fgxFile,"\n        <edge");
                fprintf(fgxFile,"\n            id=\"%d\"", edgeNum);
                fprintf(fgxFile,"\n            source=\"%d\"", bSource->bbNum);
                fprintf(fgxFile,"\n            target=\"%d\"", bTarget->bbNum);
                if (bSource->bbJumpKind == BBJ_SWITCH)
                {
                    if (edge->flDupCount >= 2)
                    {
                        fprintf(fgxFile,"\n            switchCases=\"%d\"", edge->flDupCount);
                    }
                    if (bSource->bbJumpSwt->getDefault() == bTarget)
                    {
                        fprintf(fgxFile,"\n            switchDefault=\"true\"");
                    }
                }
                if (validWeights)
                {
                    unsigned edgeWeight = (edge->flEdgeWeightMin + edge->flEdgeWeightMax) / 2;
                    fprintf(fgxFile,"\n            weight=");
                    fprintfDouble(fgxFile, ((double) edgeWeight) / weightDivisor);

                    if (edge->flEdgeWeightMin != edge->flEdgeWeightMax)
                    {
                        fprintf(fgxFile,"\n            minWeight=");
                        fprintfDouble(fgxFile, ((double) edge->flEdgeWeightMin) / weightDivisor);
                        fprintf(fgxFile,"\n            maxWeight=");
                        fprintfDouble(fgxFile, ((double) edge->flEdgeWeightMax) / weightDivisor);
                    }

                    if (edgeWeight > 0)
                    {
                        if (edgeWeight < bSource->bbWeight)
                        {
                            fprintf(fgxFile,"\n            out=");
                            fprintfDouble(fgxFile, ((double) edgeWeight) / sourceWeightDivisor );
                        }
                        if (edgeWeight < bTarget->bbWeight)
                        {
                            fprintf(fgxFile,"\n            in=");
                            fprintfDouble(fgxFile, ((double) edgeWeight) / targetWeightDivisor);
                        }
                    }
                }
            }
            if (!createDotFile)
            {
                fprintf(fgxFile,               ">");
                fprintf(fgxFile,"\n        </edge>");
            }
        }
    }
    if (createDotFile)
    {
        fprintf(fgxFile, "}\n");
    }
    else
    {
        fprintf(fgxFile, "\n    </edges>");
        fprintf(fgxFile, "\n</method>\n");
    }

    if (dontClose)
    {
        // fgxFile is stdout or stderr
        fprintf(fgxFile, "\n");
    }
    else
    {
        fclose(fgxFile);
    }

    return result;
}

#endif // DUMP_FLOWGRAPHS

/*****************************************************************************/
#ifdef DEBUG

void                Compiler::fgDispReach()
{
    printf("------------------------------------------------\n");
    printf("BBnum  Reachable by \n");
    printf("------------------------------------------------\n");

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        printf("BB%02u : ", block->bbNum);
        BLOCKSET_ITER_INIT(this, iter, block->bbReach, bbNum);
        while (iter.NextElem(this, &bbNum))
        {
            printf("BB%02u ", bbNum);
        }
        printf("\n");
    }
}

void                Compiler::fgDispDoms()
{
    // Don't bother printing this when we have a large number of BasicBlocks in the method
    if (fgBBcount > 256)
    {
        return;
    }

    printf("------------------------------------------------\n");
    printf("BBnum  Dominated by\n");
    printf("------------------------------------------------\n");

    for (unsigned i = 1; i <= fgBBNumMax; ++i)
    {
        BasicBlock* current = fgBBInvPostOrder[i];
        printf("BB%02u:  ", current->bbNum);
        while (current != current->bbIDom)
        {
            printf("BB%02u ", current->bbNum);
            current = current->bbIDom;
        }
        printf("\n");
    }
}

/*****************************************************************************/

void                Compiler::fgTableDispBasicBlock(BasicBlock* block,
                                                    int ibcColWidth /* = 0 */)
{
    unsigned        flags = block->bbFlags;

    unsigned bbNumMax = compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : fgBBNumMax;
    int maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth = max(maxBlockNumWidth, 2);
    int blockNumWidth = CountDigits(block->bbNum);
    blockNumWidth = max(blockNumWidth, 2);
    int blockNumPadding = maxBlockNumWidth - blockNumWidth;

    printf("BB%02u%*s [%08p] %2u",
           block->bbNum,
           blockNumPadding, "",
           dspPtr(block),
           block->bbRefs);

    //
    // Display EH 'try' region index
    //

    if (block->hasTryIndex())
    {
        printf(" %2u", block->getTryIndex());
    }
    else
    {
        printf("   ");
    }

    //
    // Display EH handler region index
    //

    if (block->hasHndIndex())
    {
        printf(" %2u", block->getHndIndex());
    }
    else
    {
        printf("   ");
    }

    printf(" ");

    //
    // Display block predecessor list
    //

    unsigned charCnt;
    if (fgCheapPredsValid)
    {
        charCnt = block->dspCheapPreds();
    }
    else
    {
        charCnt = block->dspPreds();
    }

    if (charCnt < 19)
    {
        printf("%*s", 19 - charCnt, "");
    }

    printf(" ");

    //
    // Display block weight
    //

    if (block->isMaxBBWeight())
    {
        printf(" MAX  ");
    }
    else
    {
        printf("%6s", refCntWtd2str(block->getBBWeight(this)));
    }

    //
    // Display optional IBC weight column.
    // Note that iColWidth includes one character for a leading space, if there is an IBC column.
    //

    if (ibcColWidth > 0)
    {
        if (block->bbFlags & BBF_PROF_WEIGHT)
        {
            printf("%*u", ibcColWidth, block->bbWeight);
        }
        else
        {
            // No IBC data. Just print spaces to align the column.
            printf("%*s", ibcColWidth, "");
        }
    }

    printf(" ");

    //
    // Display block IL range
    //

    block->dspBlockILRange();

    //
    // Display block branch target
    //

    if  (flags & BBF_REMOVED)
    {
        printf(  "[removed]       ");
    }
    else
    {
        switch (block->bbJumpKind)
        {
        case BBJ_COND:
            printf("-> BB%02u%*s ( cond )", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            break;

        case BBJ_CALLFINALLY:
            printf("-> BB%02u%*s (callf )", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            break;

        case BBJ_ALWAYS:
            if (flags & BBF_KEEP_BBJ_ALWAYS)
            {
                printf("-> BB%02u%*s (ALWAYS)", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            }
            else
            {
                printf("-> BB%02u%*s (always)", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            }
            break;

        case BBJ_LEAVE:
            printf("-> BB%02u%*s (leave )", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            break;

        case BBJ_EHFINALLYRET:
            printf(  "%*s        (finret)", maxBlockNumWidth - 2, "");
            break;

        case BBJ_EHFILTERRET:
            printf(  "%*s        (fltret)", maxBlockNumWidth - 2, "");
            break;

        case BBJ_EHCATCHRET:
            printf("-> BB%02u%*s ( cret )", block->bbJumpDest->bbNum, maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
            break;

        case BBJ_THROW:
            printf(  "%*s        (throw )", maxBlockNumWidth - 2, "");
            break;

        case BBJ_RETURN:
            printf(  "%*s        (return)", maxBlockNumWidth - 2, "");
            break;

        default:
            printf(  "%*s                ", maxBlockNumWidth - 2, "");
            break;

        case BBJ_SWITCH:
            printf("->");

            unsigned        jumpCnt;
                            jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock**    jumpTab;
                            jumpTab = block->bbJumpSwt->bbsDstTab;
            int             switchWidth;
                            switchWidth = 0;
            do
            {
                printf("%cBB%02u",
                       (jumpTab == block->bbJumpSwt->bbsDstTab) ? ' ' : ',',
                       (*jumpTab)->bbNum);
                switchWidth += 1 /* space/comma */ + 2 /* BB */ + max(CountDigits((*jumpTab)->bbNum), 2);
            }
            while (++jumpTab, --jumpCnt);

            if (switchWidth < 7)
            {
                printf("%*s", 8 - switchWidth, "");
            }

            printf(" (switch)");
            break;
        }
    }

    printf(" ");

    //
    // Display block EH region and type, including nesting indicator
    //

    if (block->hasTryIndex())
    {
        printf("T%d ", block->getTryIndex());
    }
    else
    {
        printf("   ");
    }

    if (block->hasHndIndex())
    {
        printf("H%d ", block->getHndIndex());
    }
    else
    {
        printf("   ");
    }

    if (flags & BBF_FUNCLET_BEG)
    {
        printf("F ");
    }
    else
    {
        printf("  ");
    }

    int cnt = 0;

    switch (block->bbCatchTyp)
    {
    case BBCT_NONE:            break;
    case BBCT_FAULT:           printf("fault ");   cnt += 6; break;
    case BBCT_FINALLY:         printf("finally "); cnt += 8; break;
    case BBCT_FILTER:          printf("filter ");  cnt += 7; break;
    case BBCT_FILTER_HANDLER:  printf("filtHnd "); cnt += 8; break;
    default:                   printf("catch ");   cnt += 6; break;
    }

    if (block->bbCatchTyp != BBCT_NONE)
    {
        cnt += 2;
        printf("{ ");
        /* brace matching editor workaround to compensate for the preceding line: } */
    }

    if (flags & BBF_TRY_BEG)
    {
        // Output a brace for every try region that this block opens

        EHblkDsc* HBtab;
        EHblkDsc* HBtabEnd;

        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
             HBtab < HBtabEnd;
             HBtab++)
        {
            if (HBtab->ebdTryBeg == block)
            {
                cnt += 6;
                printf("try { ");
                /* brace matching editor workaround to compensate for the preceding line: } */
            }
        }
    }

    EHblkDsc* HBtab;
    EHblkDsc* HBtabEnd;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount;
         HBtab < HBtabEnd;
         HBtab++)
    {
        if (HBtab->ebdTryLast == block)
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
        if (HBtab->ebdHndLast == block)
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
        if (HBtab->HasFilter() && block->bbNext == HBtab->ebdHndBeg)
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
    }

    while (cnt < 12)
    {
        cnt++;
        printf(" ");
    }

    //
    // Display block flags
    //

    block->dspFlags();

    printf("\n");
}

/****************************************************************************
    Dump blocks from firstBlock to lastBlock.
*/

void                Compiler::fgDispBasicBlocks(BasicBlock*  firstBlock,
                                                BasicBlock*  lastBlock,
                                                bool         dumpTrees)
{
    BasicBlock* block;

    int padWidth = 0;
#ifdef _TARGET_AMD64_
    padWidth = 8;
#endif // _TARGET_AMD64_

    // If any block has IBC data, we add an "IBC weight" column just before the 'IL range' column. This column is as wide as necessary to accommodate
    // all the various IBC weights. It's at least 4 characters wide, to accommodate the "IBC" title and leading space.
    int ibcColWidth = 0;
    for (block = firstBlock; block != nullptr; block = block->bbNext)
    {
        if (block->bbFlags & BBF_PROF_WEIGHT)
        {
            int thisIbcWidth = CountDigits(block->bbWeight);
            ibcColWidth = max(ibcColWidth, thisIbcWidth);
        }

        if (block == lastBlock)
            break;
    }
    if (ibcColWidth > 0)
    {
        ibcColWidth = max(ibcColWidth, 3) + 1; // + 1 for the leading space
    }

    unsigned bbNumMax = compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : fgBBNumMax;
    int maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth = max(maxBlockNumWidth, 2);

    padWidth += maxBlockNumWidth - 2; // Account for functions with a large number of blocks.

    printf("\n");
    printf("------%*s------------------------------------%*s-----------------------%*s----------------------------------------\n",
        padWidth, "------------",
        ibcColWidth, "------------",
        maxBlockNumWidth, "----");
    printf("BBnum %*sdescAddr ref try hnd %s     weight  %*s%s [IL range]      [jump]%*s    [EH region]         [flags]\n",
        padWidth, "",
        fgCheapPredsValid       ? "cheap preds" :
        (fgComputePredsDone     ? "preds      "
                                : "           "),
        ((ibcColWidth > 0) ? ibcColWidth - 3 : 0), "",  // Subtract 3 for the width of "IBC", printed next.
        ((ibcColWidth > 0)      ? "IBC"
                                : ""),
        maxBlockNumWidth, ""
        );
    printf("------%*s------------------------------------%*s-----------------------%*s----------------------------------------\n",
        padWidth, "------------",
        ibcColWidth, "------------",
        maxBlockNumWidth, "----");

    for (block = firstBlock;
         block;
         block = block->bbNext)
    {
        // First, do some checking on the bbPrev links
        if (block->bbPrev)
        {
            if (block->bbPrev->bbNext != block)
            {
                printf("bad prev link\n");
            }
        }
        else if (block != fgFirstBB)
        {
            printf("bad prev link!\n");
        }
            
        if (block == fgFirstColdBlock)
        {
            printf("~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n",
                padWidth, "~~~~~~~~~~~~",
                ibcColWidth, "~~~~~~~~~~~~",
                maxBlockNumWidth, "~~~~");
        }

#if FEATURE_EH_FUNCLETS
        if (block == fgFirstFuncletBB)
        {
            printf("++++++%*s++++++++++++++++++++++++++++++++++++%*s+++++++++++++++++++++++%*s++++++++++++++++++++++++++++++++++++++++ funclets follow\n",
                padWidth, "++++++++++++",
                ibcColWidth, "++++++++++++",
                maxBlockNumWidth, "++++");
        }
#endif // FEATURE_EH_FUNCLETS

        fgTableDispBasicBlock(block, ibcColWidth);

        if (block == lastBlock)
            break;
    }

    printf("------%*s------------------------------------%*s-----------------------%*s----------------------------------------\n",
        padWidth, "------------",
        ibcColWidth, "------------",
        maxBlockNumWidth, "----");

    if (dumpTrees)
    {
        fgDumpTrees(firstBlock, lastBlock);
    }
}

/*****************************************************************************/

void                Compiler::fgDispBasicBlocks(bool dumpTrees)
{
    fgDispBasicBlocks(fgFirstBB, NULL, dumpTrees);
}

/*****************************************************************************/
//  Increment the stmtNum and dump the tree using gtDispTree
//
void                Compiler::fgDumpStmtTree(GenTreePtr stmt, unsigned blkNum)
{
    compCurStmtNum++;  // Increment the current stmtNum

    printf("\n***** BB%02u, stmt %d (%s)\n", blkNum, compCurStmtNum,
           stmt->gtFlags & GTF_STMT_TOP_LEVEL ? "top level" : "embedded");

    if (fgOrder == FGOrderLinear || opts.compDbgInfo)
    {
        gtDispTree(stmt);
    }
    else
    {
        gtDispTree(stmt->gtStmt.gtStmtExpr);
    }
}

/*****************************************************************************/
//  Walk the BasicBlock list calling fgDumpTree once per Stmt
//
void                Compiler::fgDumpTrees(BasicBlock*  firstBlock,
                                          BasicBlock*  lastBlock)
{
    compCurStmtNum = 0;  // Reset the current stmtNum

    /* Walk the basic blocks */

    // Note that typically we have already called fgDispBasicBlocks() 
    //  so we don't need to print the preds and succs again here
    //
    for (BasicBlock* block = firstBlock; block; block = block->bbNext)
    {
        printf("\n------------ ");
        block->dspBlockHeader(this);

        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            fgDumpStmtTree(stmt, block->bbNum);
            if (stmt == block->bbTreeList)
            {
                block->bbStmtNum = compCurStmtNum;  // Set the block->bbStmtNum
            }
        }
        if (block == lastBlock)
            break;
    }
    printf("\n-------------------------------------------------------------------------------------------------------------------\n");
}


/*****************************************************************************
 * Try to create as many candidates for GTF_MUL_64RSLT as possible.
 * We convert 'intOp1*intOp2' into 'int(long(nop(intOp1))*long(intOp2))'.
 */

/* static */
Compiler::fgWalkResult      Compiler::fgStress64RsltMulCB(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    Compiler*  pComp = data->compiler;
    
    if (tree->gtOper != GT_MUL || tree->gtType != TYP_INT || (tree->gtOverflow()))
        return WALK_CONTINUE;

    // To ensure optNarrowTree() doesn't fold back to the original tree.
    tree->gtOp.gtOp1 = pComp->gtNewOperNode(GT_NOP, TYP_LONG, tree->gtOp.gtOp1); 
    tree->gtOp.gtOp1 = pComp->gtNewCastNode(TYP_LONG, tree->gtOp.gtOp1, TYP_LONG);
    tree->gtOp.gtOp2 = pComp->gtNewCastNode(TYP_LONG, tree->gtOp.gtOp2,  TYP_LONG);
    tree->gtType = TYP_LONG;
    *pTree = pComp->gtNewCastNode(TYP_INT, tree, TYP_INT);

    return WALK_SKIP_SUBTREES;
}

void                Compiler::fgStress64RsltMul()
{
    if (!compStressCompile(STRESS_64RSLT_MUL, 20))
        return;

    fgWalkAllTreesPre(fgStress64RsltMulCB, (void*)this);
}


// This variable is used to generate "traversal labels": one-time constants with which
// we label basic blocks that are members of the basic block list, in order to have a
// fast, high-probability test for membership in that list.  Type is "volatile" because
// it's incremented with an atomic operation, which wants a volatile type; "long" so that
// wrap-around to 0 (which I think has the highest probability of accidental collision) is
// postponed a *long* time.
static volatile int bbTraverseLabel = 1;

/*****************************************************************************
 *
 * A DEBUG routine to check the consistency of the flowgraph,
 * i.e. bbNum, bbRefs, bbPreds have to be up to date.
 *
 *****************************************************************************/

void                Compiler::fgDebugCheckBBlist(bool checkBBNum  /* = false */,
                                                 bool checkBBRefs /* = true  */)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgDebugCheckBBlist\n");
    }
#endif // DEBUG

    fgDebugCheckBlockLinks();
    
    if (fgBBcount > 10000 && expensiveDebugCheckLevel < 1)
    {
        // The basic block checks are too expensive if there are too many blocks,
        // so give up unless we've been told to try hard.
        return;
    }

    DWORD startTickCount = GetTickCount();

    BasicBlock* block;
    BasicBlock* prevBlock;
    BasicBlock* blockPred;
    flowList*   pred;
    unsigned    blockRefs;

#if FEATURE_EH_FUNCLETS
    bool            reachedFirstFunclet = false;
    if (fgFuncletsCreated)
    {
        //
        // Make sure that fgFirstFuncletBB is accurate.
        // It should be the first basic block in a handler region.
        //
        if (fgFirstFuncletBB != nullptr)
        {
            assert(fgFirstFuncletBB->hasHndIndex() == true);
            assert(fgFirstFuncletBB->bbFlags & BBF_FUNCLET_BEG);
        }
    }
#endif // FEATURE_EH_FUNCLETS

    /* Check bbNum, bbRefs and bbPreds */
    // First, pick a traversal stamp, and label all the blocks with it.
    unsigned curTraversalStamp = unsigned(InterlockedIncrement((LONG*)&bbTraverseLabel));
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        block->bbTraversalStamp = curTraversalStamp;
    }

    for (prevBlock = nullptr, block = fgFirstBB;
                              block;
         prevBlock = block,   block = block->bbNext)
    {
        blockRefs = 0;

        /* First basic block has countOfInEdges() >= 1 */

        if  (block == fgFirstBB)
        {
            noway_assert(block->countOfInEdges() >= 1);
            blockRefs = 1;
        }

        if (checkBBNum)
        {
            // Check that bbNum is sequential
            noway_assert(block->bbNext == NULL || (block->bbNum + 1 == block->bbNext->bbNum));
        }

        // If the block is a BBJ_COND, a BBJ_SWITCH or a
        // lowered GT_SWITCH_TABLE node then make sure it
        // ends with a GT_JTRUE or a GT_SWITCH

        if (block->bbJumpKind == BBJ_COND)
        {
            noway_assert(block->lastStmt()->gtNext == NULL &&
                         block->lastTopLevelStmt()->gtStmtExpr->gtOper == GT_JTRUE);
        }
        else if (block->bbJumpKind == BBJ_SWITCH)
        {
#ifndef LEGACY_BACKEND
            noway_assert(block->lastStmt()->gtNext == NULL &&
                         (block->lastTopLevelStmt()->gtStmtExpr->gtOper == GT_SWITCH ||
                          block->lastTopLevelStmt()->gtStmtExpr->gtOper == GT_SWITCH_TABLE));
#else // LEGACY_BACKEND
            noway_assert(block->lastStmt()->gtNext == NULL &&
                         block->lastTopLevelStmt()->gtStmtExpr->gtOper == GT_SWITCH);
#endif // LEGACY_BACKEND
        }
        else if (!(   block->bbJumpKind == BBJ_ALWAYS
                   || block->bbJumpKind == BBJ_RETURN))
        {
            //this block cannot have a poll
            noway_assert(!(block->bbFlags & BBF_NEEDS_GCPOLL));
        }

        if (block->bbCatchTyp == BBCT_FILTER)
        {
            if (!fgCheapPredsValid) // Don't check cheap preds
            {
                // A filter has no predecessors
                noway_assert(block->bbPreds == NULL);
            }
        }

#if FEATURE_EH_FUNCLETS
        if (fgFuncletsCreated)
        {
            //
            // There should be no handler blocks until
            // we get to the fgFirstFuncletBB block,
            // then every block should be a handler block
            //
            if (!reachedFirstFunclet)
            {
                if (block == fgFirstFuncletBB)
                {
                    assert(block->hasHndIndex() == true);
                    reachedFirstFunclet = true;
                }
                else
                {
                    assert(block->hasHndIndex() == false);
                }
            }
            else // reachedFirstFunclet
            {
                assert(block->hasHndIndex() == true);
            }
        }
#endif // FEATURE_EH_FUNCLETS

        // Don't check cheap preds.
        for (pred = (fgCheapPredsValid ? nullptr : block->bbPreds); pred != nullptr; blockRefs += pred->flDupCount, pred = pred->flNext)
        {
            assert(fgComputePredsDone); // If this isn't set, why do we have a preds list?

            /*  make sure this pred is part of the BB list */

            blockPred = pred->flBlock;
            noway_assert(blockPred->bbTraversalStamp == curTraversalStamp);

            EHblkDsc* ehTryDsc = ehGetBlockTryDsc(block);
            if (ehTryDsc != nullptr)
            {
                // You can jump to the start of a try
                if (ehTryDsc->ebdTryBeg == block)
                    goto CHECK_HND;

                // You can jump within the same try region
                if (bbInTryRegions(block->getTryIndex(), blockPred))
                    goto CHECK_HND;

                // The catch block can jump back into the middle of the try
                if (bbInCatchHandlerRegions(block, blockPred))
                    goto CHECK_HND;

                // The end of a finally region is a BBJ_EHFINALLYRET block (during importing, BBJ_LEAVE) which
                // is marked as "returning" to the BBJ_ALWAYS block following the BBJ_CALLFINALLY
                // block that does a local call to the finally. This BBJ_ALWAYS is within
                // the try region protected by the finally (for x86, ARM), but that's ok.
                if (prevBlock->bbJumpKind == BBJ_CALLFINALLY &&
                        block->bbJumpKind == BBJ_ALWAYS &&
                    blockPred->bbJumpKind == BBJ_EHFINALLYRET)
                    goto CHECK_HND;

                printf("Jump into the middle of try region: BB%02u branches to BB%02u\n", blockPred->bbNum, block->bbNum);
                noway_assert(!"Jump into middle of try region");
            }

CHECK_HND:;

            EHblkDsc* ehHndDsc = ehGetBlockHndDsc(block);
            if (ehHndDsc != nullptr)
            {
                // You can do a BBJ_EHFINALLYRET or BBJ_EHFILTERRET into a handler region
                if (   (blockPred->bbJumpKind == BBJ_EHFINALLYRET)
                    || (blockPred->bbJumpKind == BBJ_EHFILTERRET))
                    goto CHECK_JUMP;

                // Our try block can call our finally block
                if ((block->bbCatchTyp == BBCT_FINALLY) &&
                    (blockPred->bbJumpKind == BBJ_CALLFINALLY) &&
                    ehCallFinallyInCorrectRegion(blockPred, block->getHndIndex()))
                {
                    goto CHECK_JUMP;
                }

                // You can jump within the same handler region
                if (bbInHandlerRegions(block->getHndIndex(), blockPred))
                    goto CHECK_JUMP;

                // A filter can jump to the start of the filter handler
                if (ehHndDsc->HasFilter())
                    goto CHECK_JUMP;

                printf("Jump into the middle of handler region: BB%02u branches to BB%02u\n", blockPred->bbNum, block->bbNum);
                noway_assert(!"Jump into the middle of handler region");
            }

CHECK_JUMP:;

            switch (blockPred->bbJumpKind)
            {
            case BBJ_COND:
                noway_assert(blockPred->bbNext == block || blockPred->bbJumpDest == block);
                break;

            case BBJ_NONE:
                noway_assert(blockPred->bbNext == block);
                break;

            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
                noway_assert(blockPred->bbJumpDest == block);
                break;

            case BBJ_EHFINALLYRET:
                {
                    // If the current block is a successor to a BBJ_EHFINALLYRET (return from finally),
                    // then the lexically previous block should be a call to the same finally.
                    // Verify all of that.

                    unsigned hndIndex = blockPred->getHndIndex();
                    EHblkDsc* ehDsc = ehGetDsc(hndIndex);
                    BasicBlock* finBeg = ehDsc->ebdHndBeg;

                    // Because there is no bbPrev, we have to search for the lexically previous
                    // block.  We can shorten the search by only looking in places where it is legal
                    // to have a call to the finally.

                    BasicBlock* begBlk;
                    BasicBlock* endBlk;
                    ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

                    for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
                    {
                        if  (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                            continue;

                        if  (block == bcall->bbNext)
                            goto PRED_OK;
                    }

#if FEATURE_EH_FUNCLETS

                    if (fgFuncletsCreated)
                    {
                        // There is no easy way to search just the funclets that were pulled out of
                        // the corresponding try body, so instead we search all the funclets, and if
                        // we find a potential 'hit' we check if the funclet we're looking at is
                        // from the correct try region.

                        for (BasicBlock* bcall = fgFirstFuncletBB; bcall; bcall = bcall->bbNext)
                        {
                            if  (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
                                continue;

                            if  (block != bcall->bbNext)
                                continue;

                            if (ehCallFinallyInCorrectRegion(bcall, hndIndex))
                                goto PRED_OK;
                        }
                    }

#endif // FEATURE_EH_FUNCLETS

                    noway_assert(!"BBJ_EHFINALLYRET predecessor of block that doesn't follow a BBJ_CALLFINALLY!");
                }
                break;

            case BBJ_THROW:
            case BBJ_RETURN:
                noway_assert(!"THROW and RETURN block cannot be in the predecessor list!");
                break;

            case BBJ_SWITCH:
                unsigned        jumpCnt; jumpCnt = blockPred->bbJumpSwt->bbsCount;
                BasicBlock**    jumpTab; jumpTab = blockPred->bbJumpSwt->bbsDstTab;

                do
                {
                    if  (block == *jumpTab)
                    {
                        goto PRED_OK;
                    }
                }
                while (++jumpTab, --jumpCnt);

                noway_assert(!"SWITCH in the predecessor list with no jump label to BLOCK!");
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
            }

PRED_OK:;

        }

        /* Check the bbRefs */
        noway_assert(!checkBBRefs || block->bbRefs == blockRefs);

        /* Check that BBF_HAS_HANDLER is valid bbTryIndex */
        if (block->hasTryIndex())
        {
            noway_assert(block->getTryIndex() < compHndBBtabCount);
        }

        /* Check if BBF_RUN_RARELY is set that we have bbWeight of zero */
        if (block->isRunRarely())
        {
            noway_assert(block->bbWeight == BB_ZERO_WEIGHT);
        }
        else
        {
            noway_assert(block->bbWeight > BB_ZERO_WEIGHT);
        }
    }

    // Make sure the one return BB is not changed.
    if (genReturnBB)
    {
        noway_assert(genReturnBB->bbTreeList);
        noway_assert(genReturnBB->bbTreeList->gtOper == GT_STMT);
        noway_assert(genReturnBB->bbTreeList->gtType == TYP_VOID);
    }

    // The general encoder/decoder (currently) only reports "this" as a generics context as a stack location,
    // so we mark info.compThisArg as lvAddrTaken to ensure that it is not enregistered. Otherwise, it should
    // not be address-taken.  This variable determines if the address-taken-ness of "thisArg" is "OK".
    bool copiedForGenericsCtxt;
#ifndef JIT32_GCENCODER
    copiedForGenericsCtxt = ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0);
#else  // JIT32_GCENCODER
    copiedForGenericsCtxt = FALSE;
#endif  // JIT32_GCENCODER

    // This if only in support of the noway_asserts it contains.
    if (info.compIsStatic)
    {
        // For static method, should have never grabbed the temp.
        noway_assert(lvaArg0Var == BAD_VAR_NUM);
    }
    else
    {
        // For instance method:
        assert(info.compThisArg != BAD_VAR_NUM);
        bool compThisArgAddrExposedOK = !lvaTable[info.compThisArg].lvAddrExposed;
#ifndef JIT32_GCENCODER
        compThisArgAddrExposedOK = compThisArgAddrExposedOK || copiedForGenericsCtxt;
#endif  // !JIT32_GCENCODER
        noway_assert(compThisArgAddrExposedOK &&                  //     should never expose the address of arg 0 or
                     !lvaTable[info.compThisArg].lvArgWrite &&    //     write to arg 0.
                     (                                                  //   In addition,
                        lvaArg0Var == info.compThisArg ||               //     lvArg0Var should remain 0 if arg0 is not written to or address-exposed.
                        lvaArg0Var != info.compThisArg &&
                        (lvaTable[lvaArg0Var].lvAddrExposed || lvaTable[lvaArg0Var].lvArgWrite || copiedForGenericsCtxt)
                     ));
    }
}

/*****************************************************************************
 *
 * A DEBUG routine to check the that the exception flags are correctly set.
 *
 ****************************************************************************/

void                Compiler::fgDebugCheckFlags(GenTreePtr tree)
{
    noway_assert(tree->gtOper != GT_STMT);

    genTreeOps      oper        = tree->OperGet();
    unsigned        kind        = tree->OperKind();
    unsigned        treeFlags   = tree->gtFlags & GTF_ALL_EFFECT;
    unsigned        chkFlags    = 0;

    /* Is this a leaf node? */

    if  (kind & GTK_LEAF)
    {
        switch (oper)
        {
        case GT_CLS_VAR:
            chkFlags |= GTF_GLOB_REF;
            break;

        case GT_CATCH_ARG:
            chkFlags |= GTF_ORDER_SIDEEFF;
            break;

        default:
            break;
        }
    }

    /* Is it a 'simple' unary/binary operator? */

    else if  (kind & GTK_SMPOP)
    {
        GenTreePtr      op1 = tree->gtOp.gtOp1;
        GenTreePtr      op2 = tree->gtGetOp2();

        // During GS work, we make shadow copies for params.
        // In gsParamsToShadows(), we create a shadow var of TYP_INT for every small type param.
        // Then in gsReplaceShadowParams(), we change the gtLclNum to the shadow var.
        // We also change the types of the local var tree and the assignment tree to TYP_INT if necessary.
        // However, since we don't morph the tree at this late stage. Manually propagating
        // TYP_INT up to the GT_ASG tree is only correct if we don't need to propagate the TYP_INT back up.
        // The following checks will ensure this.

        // Is the left child of "tree" a GT_ASG?,
        if (op1 && op1->gtOper == GT_ASG)
        {
            assert(tree->gtType == TYP_VOID ||  // If parent is a TYP_VOID, we don't no need to propagate TYP_INT up. We are fine.
                   tree->gtOper == GT_COMMA);   // (or) If GT_ASG is the left child of a GT_COMMA, the type of the GT_COMMA node will
        }                                       // be determined by its right child. So we don't need to propagate TYP_INT up either. We are fine.

        // Is the right child of "tree" a GT_ASG?,
        if (op2 && op2->gtOper == GT_ASG)
        {
            assert(tree->gtType == TYP_VOID);   // If parent is a TYP_VOID, we don't no need to propagate TYP_INT up. We are fine.
        }

        switch (oper)
        {
        case GT_QMARK:
            if (op1->OperIsCompare())
            {
                noway_assert(op1->gtFlags & GTF_DONT_CSE);
            }
            else
            {
                noway_assert( (op1->gtOper == GT_CNS_INT) &&
                              ((op1->gtIntCon.gtIconVal == 0) || (op1->gtIntCon.gtIconVal == 1)) );
            }
            break;

        default:
            break;
        }

        /* Recursively check the subtrees */

        if (op1) fgDebugCheckFlags(op1);
        if (op2) fgDebugCheckFlags(op2);

        if (op1) chkFlags   |= (op1->gtFlags & GTF_ALL_EFFECT);
        if (op2) chkFlags   |= (op2->gtFlags & GTF_ALL_EFFECT);

        // We reuse the value of GTF_REVERSE_OPS for a GT_IND-specific flag,
        // so exempt that (unary) operator.
        if (tree->OperGet() != GT_IND && tree->gtFlags & GTF_REVERSE_OPS)
        {
            /* Must have two operands if GTF_REVERSE is set */
            noway_assert(op1 && op2);

            /* Make sure that the order of side effects has not been swapped. */

            /* However CSE may introduce an assignment after the reverse flag
               was set and thus GTF_ASG cannot be considered here. */

            /* For a GT_ASG(GT_IND(x), y) we are interested in the side effects of x */
            GenTreePtr  op1p;
            if ((kind & GTK_ASGOP) && (op1->gtOper == GT_IND))
            {
                op1p = op1->gtOp.gtOp1;
            }
            else
            {
                op1p = op1;
            }

            /* This isn't true any more with the sticky GTF_REVERSE */
            /*
            // if op1p has side effects, then op2 cannot have side effects
            if (op1p->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG))
            {
                if (op2->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG))
                    gtDispTree(tree);
                noway_assert(!(op2->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG)));
            }
            */
        }

        if (kind & GTK_ASGOP)
        {
            chkFlags        |= GTF_ASG;
        }

        /* Note that it is OK for treeFlags not to have a GTF_EXCEPT,
           AssertionProp's non-Null may have cleared it */
        if (tree->OperMayThrow())
        {
            chkFlags        |= (treeFlags & GTF_EXCEPT);
        }

        if (oper == GT_ADDR &&
            (op1->OperIsLocal() ||
             op1->gtOper == GT_CLS_VAR ||
             (op1->gtOper == GT_IND && op1->gtOp.gtOp1->gtOper == GT_CLS_VAR_ADDR)))
        {
            /* &aliasedVar doesn't need GTF_GLOB_REF, though alisasedVar does.
               Similarly for clsVar */
            treeFlags |= GTF_GLOB_REF;
        }
    }

    /* See what kind of a special operator we have here */

    else switch  (tree->OperGet())
    {
    case GT_CALL:

        GenTreePtr      args;
        GenTreePtr      argx;
        GenTreeCall*    call;
        
        call = tree->AsCall();

        chkFlags |= GTF_CALL;

        if ((treeFlags & GTF_EXCEPT) && !(chkFlags & GTF_EXCEPT))
        {
            switch (eeGetHelperNum(tree->gtCall.gtCallMethHnd))
            {
                // Is this a helper call that can throw an exception ?
            case CORINFO_HELP_LDIV:
            case CORINFO_HELP_LMOD:
            case CORINFO_HELP_METHOD_ACCESS_CHECK:
            case CORINFO_HELP_FIELD_ACCESS_CHECK:
            case CORINFO_HELP_CLASS_ACCESS_CHECK:
            case CORINFO_HELP_DELEGATE_SECURITY_CHECK:
                chkFlags |= GTF_EXCEPT;
                break;
            default:
                break;
            }
        }

        if (call->gtCallObjp)
        {
            fgDebugCheckFlags(call->gtCallObjp);
            chkFlags |= (call->gtCallObjp->gtFlags & GTF_SIDE_EFFECT);

            if (call->gtCallObjp->gtFlags & GTF_ASG)
            {
                treeFlags |= GTF_ASG;
            }
        }

        for (args = call->gtCallArgs; args; args = args->gtOp.gtOp2)
        {
            argx = args->gtOp.gtOp1;
            fgDebugCheckFlags(argx);

            chkFlags |= (argx->gtFlags & GTF_SIDE_EFFECT);

            if (argx->gtFlags & GTF_ASG)
            {
                treeFlags |= GTF_ASG;
            }
        }

        for (args = call->gtCallLateArgs; args; args = args->gtOp.gtOp2)
        {
            argx = args->gtOp.gtOp1;
            fgDebugCheckFlags(argx);

            chkFlags |= (argx->gtFlags & GTF_SIDE_EFFECT);

            if (argx->gtFlags & GTF_ASG)
            {
                treeFlags |= GTF_ASG;
            }
        }

        if ((call->gtCallType == CT_INDIRECT) && (call->gtCallCookie != NULL))
        {
            fgDebugCheckFlags(call->gtCallCookie);
            chkFlags |= (call->gtCallCookie->gtFlags & GTF_SIDE_EFFECT);
        }

        if (call->gtCallType == CT_INDIRECT)
        {
            fgDebugCheckFlags(call->gtCallAddr);
            chkFlags |= (call->gtCallAddr->gtFlags & GTF_SIDE_EFFECT);
        }

        if (call->IsUnmanaged() &&
            (call->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL))
        {
            if (call->gtCallArgs->gtOp.gtOp1->OperGet() == GT_NOP)
            {
                noway_assert(call->gtCallLateArgs->gtOp.gtOp1->TypeGet() == TYP_I_IMPL ||
                             call->gtCallLateArgs->gtOp.gtOp1->TypeGet() == TYP_BYREF);
            }
            else
            {
                noway_assert(call->gtCallArgs->gtOp.gtOp1->TypeGet() == TYP_I_IMPL ||
                             call->gtCallArgs->gtOp.gtOp1->TypeGet() == TYP_BYREF);
            }
        }
        break;

    case GT_ARR_ELEM:

        GenTreePtr      arrObj;
        unsigned        dim;

        arrObj = tree->gtArrElem.gtArrObj;
        fgDebugCheckFlags(arrObj);
        chkFlags   |= (arrObj->gtFlags & GTF_ALL_EFFECT);

        for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
        {
            fgDebugCheckFlags(tree->gtArrElem.gtArrInds[dim]);
            chkFlags |= tree->gtArrElem.gtArrInds[dim]->gtFlags & GTF_ALL_EFFECT;
        }
        break;

    case GT_ARR_OFFSET:
        fgDebugCheckFlags(tree->gtArrOffs.gtOffset);
        chkFlags   |= (tree->gtArrOffs.gtOffset->gtFlags & GTF_ALL_EFFECT);
        fgDebugCheckFlags(tree->gtArrOffs.gtIndex);
        chkFlags   |= (tree->gtArrOffs.gtIndex->gtFlags & GTF_ALL_EFFECT);
        fgDebugCheckFlags(tree->gtArrOffs.gtArrObj);
        chkFlags   |= (tree->gtArrOffs.gtArrObj->gtFlags & GTF_ALL_EFFECT);
        break;

    default:
        break;
    }

    if (chkFlags & ~treeFlags)
    {
        // Print the tree so we can see it in the log.
        printf("Missing flags on tree [%X]: ", tree);
        GenTree::gtDispFlags(chkFlags & ~treeFlags);
        printf("\n");
        gtDispTree(tree);

        noway_assert(!"Missing flags on tree");

        // Print the tree again so we can see it right after we hook up the debugger.
        printf("Missing flags on tree [%X]: ", tree);
        GenTree::gtDispFlags(chkFlags & ~treeFlags);
        printf("\n");
        gtDispTree(tree);
    }
    else if (treeFlags & ~chkFlags)
    {
#if 0
        // TODO-Cleanup:
        /* The tree has extra flags set. However, this will happen if we
           replace a subtree with something, but don't clear the flags up
           the tree. Can't flag this unless we start clearing flags above.

           Note: we need this working for GTF_CALL and CSEs, so I'm enabling
           it for calls.
           */
        if (tree->OperGet() != GT_CALL && (treeFlags & GTF_CALL) && !(chkFlags & GTF_CALL))
        {
            // Print the tree so we can see it in the log.
            printf("Extra GTF_CALL flags on parent tree [%X]: ", tree);
            GenTree::gtDispFlags(treeFlags & ~chkFlags);
            printf("\n");
            gtDispTree(tree);

            noway_assert(!"Extra flags on tree");

            // Print the tree again so we can see it right after we hook up the debugger.
            printf("Extra GTF_CALL flags on parent tree [%X]: ", tree);
            GenTree::gtDispFlags(treeFlags & ~chkFlags);
            printf("\n");
            gtDispTree(tree);
        }
#endif // 0
    }
}

// DEBUG routine to check correctness of the internal gtNext, gtPrev threading of a statement.
// This threading is only valid when fgStmtListThreaded is true.
// This calls an alternate method for FGOrderLinear.
void Compiler::fgDebugCheckNodeLinks(BasicBlock* block, GenTree* node)
{
    GenTreeStmt* stmt = node->AsStmt();

    assert(fgStmtListThreaded);
    if (fgOrder == FGOrderLinear)
    {
        fgDebugCheckLinearNodeLinks(block, stmt);
        return;
    }

    noway_assert(stmt->gtStmtList);

    // The first node's gtPrev must be nullptr (the gtPrev list is not circular).
    // The last node's gtNext must be nullptr (the gtNext list is not circular). This is tested if the loop below terminates.
    assert(stmt->gtStmtList->gtPrev == nullptr);

    for (GenTreePtr tree = stmt->gtStmtList;
         tree != nullptr;
         tree = tree->gtNext)
    {
        if  (tree->gtPrev)
        {
            noway_assert(tree->gtPrev->gtNext == tree);
        }
        else
        {
            noway_assert(tree == stmt->gtStmtList);
        }

        if  (tree->gtNext)
        {
            noway_assert(tree->gtNext->gtPrev == tree);
        }
        else
        {
            noway_assert(tree == stmt->gtStmtExpr);
        }

        /* Cross-check gtPrev,gtNext with gtOp for simple trees */

        GenTreePtr expectedPrevTree = NULL;

        if (tree->OperIsLeaf())
        {
            if (tree->gtOper == GT_CATCH_ARG)
            {
                // The GT_CATCH_ARG should always have GTF_ORDER_SIDEEFF set
                noway_assert(tree->gtFlags & GTF_ORDER_SIDEEFF);
                // The GT_CATCH_ARG has to be the first thing evaluated
#if JIT_FEATURE_SSA_SKIP_DEFS
                noway_assert(stmt == block->FirstNonPhiDef());
#else
                noway_assert(stmt == block->bbTreeList);
#endif
                noway_assert(stmt->gtStmtList->gtOper == GT_CATCH_ARG);
                // The root of the tree should have GTF_ORDER_SIDEEFF set
                noway_assert(stmt->gtStmtExpr->gtFlags & GTF_ORDER_SIDEEFF);
            }
        }

        if (tree->OperIsUnary() && tree->gtOp.gtOp1)
        {
            GenTreePtr lclVarTree;
            expectedPrevTree = tree->gtOp.gtOp1;
            if ((tree->gtOper == GT_LDOBJ) &&
                (tree->gtLdObj.gtFldTreeList != NULL) &&
                impIsAddressInLocal(tree->gtOp.gtOp1, &lclVarTree))
            {
                GenTreePtr* fldTreeList = tree->gtLdObj.gtFldTreeList;
                GenTreePtr prev = NULL;
                unsigned fieldCount = lvaTable[lclVarTree->gtLclVarCommon.gtLclNum].lvFieldCnt;

                for (unsigned i = 0; i < fieldCount; i++)
                {
                    if (fldTreeList[i] != NULL)
                    {
                        if (prev != NULL)
                        {
                            noway_assert(fldTreeList[i]->gtPrev == prev);
                        }
                        prev = fldTreeList[i];
                    }
                }
                noway_assert(lclVarTree->gtPrev == prev);
            }
        }
        else if (tree->OperIsBinary() && tree->gtOp.gtOp1)
        {
            switch (tree->gtOper)
            {
            case GT_QMARK:
                expectedPrevTree = tree->gtOp.gtOp2->AsColon()->ThenNode(); // "then" operand of the GT_COLON (generated second).
                break;

            case GT_COLON:
                expectedPrevTree = tree->AsColon()->ElseNode(); // "else" branch result (generated first).
                break;

            case GT_INITBLK:
            case GT_COPYBLK:
            case GT_COPYOBJ:
                // the first child is a GT_LIST, where has op1 is the dst and op2 is the src.
                // The read has to occur before the write so make sure REVERSE_OPS is set.
                assert(tree->gtOp.gtOp1->gtFlags & GTF_REVERSE_OPS);
                __fallthrough;

            default:
                if (tree->gtOp.gtOp2)
                {
                    if (tree->gtFlags & GTF_REVERSE_OPS)
                    {
                        expectedPrevTree = tree->gtOp.gtOp1;
                    }
                    else
                    {
                        expectedPrevTree = tree->gtOp.gtOp2;
                    }
                }
                else
                {
                    expectedPrevTree = tree->gtOp.gtOp1;
                }
                break;
            }
        }

        noway_assert(expectedPrevTree == NULL ||                // No expectations about the prev node
                     tree->gtPrev == expectedPrevTree);         // The "normal" case
    }
}

//------------------------------------------------------------------------
// fgDebugCheckLinearTree: Counts the nodes in a tree by doing a tree traversal,
//    and validates that GT_CATCH_ARG causes GTF_ORDER_SIDEEFF to be set on
//    itself and its containing statement.
//
// Arguments:
//    block      - The block containing 'tree'
//    stmt       - The statement containing 'tree'
//    tree       - The tree to be checked
//    printNodes - In case DebugCheckLinearNodeLinks detects a problem, we re-run
//                 printing the tree order traversal node by node for debugging purposes.
// Return Value:
//    Returns the count of nodes in the tree.
//
// Notes:
//    This is called by fgDebugCheckLinearNodeLinks(), which uses the node
//    count from the tree walk to validate against the node count from a
//    linear traversal.

unsigned Compiler::fgDebugCheckLinearTree(BasicBlock* block,
                                          GenTreePtr stmt,
                                          GenTreePtr tree,
                                          bool printNodes)
{
    if (printNodes)
    {
        DISPNODE(tree);
    }

    unsigned nodeCount = 1;
    if (tree->OperIsLeaf())
    {
        if (tree->gtOper == GT_CATCH_ARG)
        {
            // The GT_CATCH_ARG should always have GTF_ORDER_SIDEEFF set
            noway_assert(tree->gtFlags & GTF_ORDER_SIDEEFF);
            // The GT_CATCH_ARG has to be the first thing evaluated
#if JIT_FEATURE_SSA_SKIP_DEFS
            noway_assert(stmt == block->FirstNonPhiDef());
#else
            noway_assert(stmt == block->bbTreeList);
#endif
            noway_assert(stmt->gtStmt.gtStmtList->gtOper == GT_CATCH_ARG);
            // The root of the tree should have GTF_ORDER_SIDEEFF set
            noway_assert(stmt->gtStmt.gtStmtExpr->gtFlags & GTF_ORDER_SIDEEFF);
        }
    }
    else
    {
        // Do per-node checks

        // Visit children
        unsigned childCount = tree->NumChildren();
        for (unsigned i = 0;
             i < childCount;
             i++)
        {
            GenTreePtr child = tree->GetChild(i);
            nodeCount += fgDebugCheckLinearTree(block, stmt, child, printNodes);
        }
    }
    return nodeCount;
}

//------------------------------------------------------------------------
// fgDebugCheckLinearNodeLinks: DEBUG routine to check correctness of the internal
//    gtNext, gtPrev threading of a statement.
//
// Arguments:
//    block        - The block containing 'topLevelStmt'
//    topLevelStmt - The statement to be checked
//    printNodes   - 'true' if we are to print the nodes while we check them.
//
// Return Value:
//    None.
//
// Notes:
//    This function is the called by fgDebugCheckNodeLinks when fgOrder == FGOrderLinear,
//    and is only valid when fgStmtListThreaded is true and fgOrder == FGOrderLinear.
//    It compares the number of nodes encountered in tree-walking 'topLevelStmt' and all
//    of its embedded statements, against the number of nodes in a linear traversal.
void Compiler::fgDebugCheckLinearNodeLinks(BasicBlock* block,
                                           GenTreePtr topLevelStmt,
                                           bool printNodes)
{
    assert(fgStmtListThreaded);
    assert(fgOrder == FGOrderLinear);
    assert(topLevelStmt->gtOper == GT_STMT);

    // TODO-Cleanup: This is generally called for statements in order, so we'll skip the embedded ones.
    // Consider whether we should do some alternate checking in that case (e.g. just validate
    // the list is correct OR validate the corresponding top-level statement, which we probably
    // just finished doing, OR fix all callees to check whether it's top-level before calling this).
    if ((topLevelStmt->gtFlags & GTF_STMT_TOP_LEVEL) == 0)
        return;

    // We're first going to traverse the statements in linear order, counting the nodes and ensuring that
    // the links are consistent.
    // We should be able to reach all the nodes by starting with topLevelStmt->gtStmt.gtStmtList.

    unsigned linearNodeCount = 0;

    if (printNodes)
    {
        JITDUMP("\nChecking linear order first:\n");
    }

    noway_assert(topLevelStmt->gtStmt.gtStmtList);

    // The first node's gtPrev must be nullptr (the gtPrev list is not circular).
    // The last node's gtNext must be nullptr (the gtNext list is not circular). This is tested if the loop below terminates.
    noway_assert(topLevelStmt->gtStmt.gtStmtList->gtPrev == nullptr);

    for (GenTreePtr tree = topLevelStmt->gtStmt.gtStmtList;
         tree != nullptr;
         tree = tree->gtNext)
    {
        if (printNodes)
        {
            DISPNODE(tree);
        }

        linearNodeCount++;
        if  (tree->gtPrev)
        {
            noway_assert(tree->gtPrev->gtNext == tree);
        }
        else
        {
            // Because the embedded trees are fully contained within the topLevelStmt, the last node
            // we encounter must be the top-level node of the top-level statement.
            noway_assert(tree == topLevelStmt->gtStmt.gtStmtList);
        }

        if  (tree->gtNext)
        {
            noway_assert(tree->gtNext->gtPrev == tree);
        }
        else
        {
            noway_assert(tree == topLevelStmt->gtStmt.gtStmtExpr);
        }
    }

    // Next, we're going to traverse the statements in tree order, ensuring that we encounter the right
    // number of nodes.
    // Stop when we reach the next top-level statement (or nullptr)

    unsigned treeNodeCount = 0;
    GenTreeStmt* stmt = topLevelStmt->AsStmt();

    if (printNodes)
    {
        JITDUMP("\nNow checking tree-ordering:\n");
    }

    do
    {
        treeNodeCount += fgDebugCheckLinearTree(block, stmt, stmt->gtStmtExpr, printNodes);
        stmt = stmt->gtNextStmt;
    } 
    while (stmt != nullptr && (stmt->gtFlags & GTF_STMT_TOP_LEVEL) == 0);

    if (treeNodeCount != linearNodeCount)
    {
        if (!printNodes)
        {
            JITDUMP("Tree order vs linear order counts differed!\n");
            fgDebugCheckLinearNodeLinks(block, topLevelStmt, true);
        }
    }
    assert (treeNodeCount == linearNodeCount);
}


//------------------------------------------------------------------------
// fgStmtContainsNode:
//    Debugging method to check whether a tree is inside the given
//    statement.
//
// Arguments:
//    stmt - The statement whose tree is presumably contained inside
//    tree - GenTree to be checked.
//
// Return Value:
//    True in case 'tree' is contained inside statement 'stmt'
//
bool Compiler::fgStmtContainsNode(GenTreeStmt* stmt, GenTree* tree)
{
    GenTree* first = stmt->gtStmtList;
    for (GenTree* actual = first;
         actual != nullptr;
         actual = actual->gtNext)
    {
        if (actual == tree)
            return true;
    }
    return false;
}

/*****************************************************************************
 *
 * A DEBUG routine to check the correctness of the links between GT_STMT nodes
 * and ordinary nodes within a statement.
 *
 ****************************************************************************/

void                Compiler::fgDebugCheckLinks(bool morphTrees)
{
    // This used to be only on for stress, and there was a comment stating that
    // it was "quite an expensive operation" but I did not find that to be true.
    // Set DO_SANITY_DEBUG_CHECKS to false to revert to that behavior.
    const bool DO_SANITY_DEBUG_CHECKS = true;

    if (!DO_SANITY_DEBUG_CHECKS &&
        !compStressCompile(STRESS_CHK_FLOW_UPDATE, 30))
    {
        return;
    }

    fgDebugCheckBlockLinks();

    /* For each basic block check the bbTreeList links */
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
PROCESS_BLOCK_AGAIN:;

        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            /* Verify that bbTreeList is threaded correctly */
            /* Note that for the GT_STMT list, the gtPrev list is circular. The gtNext list is not: gtNext of the last GT_STMT in a block is nullptr. */

            noway_assert(stmt->gtPrev);

            if  (stmt == block->bbTreeList)
            {
                noway_assert(stmt->gtPrev->gtNext == nullptr);
            }
            else
            {
                noway_assert(stmt->gtPrev->gtNext == stmt);
            }

            if  (stmt->gtNext)
            {
                noway_assert(stmt->gtNext->gtPrev == stmt);
            }
            else
            {
                noway_assert(block->lastStmt() == stmt);
            }

            /* For each statement check that the exception flags are properly set */

            noway_assert(stmt->gtStmtExpr);

            if (verbose && 0)
            {
                gtDispTree(stmt->gtStmtExpr);
            }

            fgDebugCheckFlags(stmt->gtStmtExpr);

            // Not only will this stress fgMorphBlockStmt(), but we also get all the checks
            // done by fgMorphTree()

            if (morphTrees)
            {
                // If 'stmt' is removed from the block, restart
                if (fgMorphBlockStmt(block, stmt DEBUGARG("test morphing")))
                {
                    goto PROCESS_BLOCK_AGAIN;
                }
            }

            /* For each GT_STMT node check that the nodes are threaded correcly - gtStmtList */

            if (fgStmtListThreaded)
            {
                fgDebugCheckNodeLinks(block, stmt);
            }
        }
    }
}

// ensure that bbNext and bbPrev are consistent
void Compiler::fgDebugCheckBlockLinks()
{
    assert(fgFirstBB->bbPrev == nullptr);

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        if (block->bbNext)
        {
            assert(block->bbNext->bbPrev == block);
        }
        else
        {
            assert(block == fgLastBB);
        }

        if (block->bbPrev)
        {
            assert(block->bbPrev->bbNext == block);
        }
        else
        {
            assert(block == fgFirstBB);
        }

        // If this is a switch, check that the tables are consistent.
        // Note that we don't call GetSwitchDescMap(), because it has the side-effect
        // of allocating it if it is not present.
        if (block->bbJumpKind == BBJ_SWITCH && m_switchDescMap != nullptr)
        {
            SwitchUniqueSuccSet uniqueSuccSet;
            if (m_switchDescMap->Lookup(block, &uniqueSuccSet))
            {
                // Create a set with all the successors. Don't use BlockSet, so we don't need to worry
                // about the BlockSet epoch.
                BitVecTraits bitVecTraits(fgBBNumMax + 1, this);
                BitVec BITVEC_INIT_NOCOPY(succBlocks, BitVecOps::MakeEmpty(&bitVecTraits));
                BasicBlock** jumpTable = block->bbJumpSwt->bbsDstTab;
                unsigned jumpCount = block->bbJumpSwt->bbsCount;
                for (unsigned i = 0; i < jumpCount; i++)
                {
                    BitVecOps::AddElemD(&bitVecTraits, succBlocks, jumpTable[i]->bbNum);
                }
                // Now we should have a set of unique successors that matches what's in the switchMap.
                // First, check the number of entries, then make sure all the blocks in uniqueSuccSet
                // are in the BlockSet.
                unsigned count = BitVecOps::Count(&bitVecTraits, succBlocks);
                assert(uniqueSuccSet.numDistinctSuccs == count);
                for (unsigned i = 0; i < uniqueSuccSet.numDistinctSuccs; i++)
                {
                    assert(BitVecOps::IsMember(&bitVecTraits, succBlocks, uniqueSuccSet.nonDuplicates[i]->bbNum));
                }
            }
        }
    }
}

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/

//------------------------------------------------------------------------
// fgNodeContainsEmbeddedStatement:
//    Predicate that verifies whether the given tree has an embedded statement
//    in it.
//
// Arguments:
//    tree     - GenTree to be checked.
//    topLevel - The top-level statement where 'tree' lives.
//
// Assumptions:
//    The given 'tree' must be contained inside 'topLevel' (i.e. is a descendant
//    of topLevel.gtStmtExpr
//
// Return Value:
//    True in case 'tree' contains an embedded statement.
//
bool Compiler::fgNodeContainsEmbeddedStatement(GenTree* tree, GenTreeStmt* topLevel)
{
    assert(fgStmtContainsNode(topLevel, tree));

    for (GenTree* actual = fgGetFirstNode(tree);
         actual != tree;
         actual = actual->gtNext)
    {
        for (GenTree* curStmt = topLevel->gtNext;
             curStmt != nullptr && curStmt->gtStmt.gtStmtIsEmbedded();
             curStmt = curStmt->gtNext)
        {
            if (curStmt->gtStmt.gtStmtList == actual)
                return true;
        }
    }
    return false;
}

//------------------------------------------------------------------------
// fgRemoveContainedEmbeddedStatements:
//    If a tree contains a subtree, recursively remove all embedded
//    statements "contained" in the subtree.
//
// Arguments:
//    tree     - GenTree to be checked.
//    stmt - The statement where 'tree' lives.
//    block    - block where "topLevel" lives.
//
// Assumptions:
//    The given 'tree' must be contained inside 'stmt' (i.e. is a descendant
//    of stmt.gtStmtExpr and 'stmt' is in 'block'
//
// Return Value:
//    None, but all embedded statements that the tree depends on are removed.
//
void Compiler::fgRemoveContainedEmbeddedStatements(GenTreePtr tree, GenTreeStmt* stmt, BasicBlock* block)
{
    assert(fgStmtContainsNode(stmt, tree));

    GenTreePtr embCursor = stmt->gtNext;
    // Get the first node that will be evaluated in the subtree,
    // "tree" will be the last node to be evaluated.
    for (GenTree* child = fgGetFirstNode(tree); child != tree; child = child->gtNext)
    {
        // Now check each following stmt to see if "tree"
        // is actually the first node in its stmt list.
        for (GenTreePtr cur = embCursor;
             cur != nullptr && cur->gtStmt.gtStmtIsEmbedded();
             cur = cur->gtNext)
        {
            if (cur->gtStmt.gtStmtList == child)
            {
                fgRemoveContainedEmbeddedStatements(cur->gtStmt.gtStmtExpr, cur->AsStmt(), block);
                fgRemoveStmt(block, cur);
                embCursor = cur->gtNext;
                break;
            }
        }
    }
}

//------------------------------------------------------------------------
// fgCheckForInlineDepthAndRecursion: compute depth of the candidate, and
// check for recursion and excessive depth
//
// Return Value:
//    The depth of the inline candidate. The root method is a depth 0, top-level
//    candidates at depth 1, etc.
//
// Notes:
//    We generally disallow recursive inlines by policy. However, they are
//    supported by the underlying machinery.
//
//    Likewise the depth limit is a policy consideration, and serves mostly
//    as a safeguard to prevent runaway inlining of small methods.

unsigned     Compiler::fgCheckInlineDepthAndRecursion(InlineInfo* inlineInfo)
{
    BYTE*          candidateCode = inlineInfo->inlineCandidateInfo->methInfo.ILCode;
    InlineContext* inlineContext = inlineInfo->iciStmt->gtStmt.gtInlineContext;
    InlineResult*  inlineResult  = inlineInfo->inlineResult;

    // There should be a context for all candidates.
    assert(inlineContext != nullptr);

    const DWORD MAX_INLINING_RECURSION_DEPTH = 20;
    DWORD depth = 0;

    for (; inlineContext != nullptr; inlineContext = inlineContext->GetParent())
    {
        // Hard limit just to catch pathological cases
        depth++;

        if (inlineContext->GetCode() == candidateCode)
        {
            // This inline candidate has the same IL code buffer as an already
            // inlined method does.
            inlineResult->NoteFatal(InlineObservation::CALLSITE_IS_RECURSIVE);
            break;
        }

        if (depth > MAX_INLINING_RECURSION_DEPTH)
        {
            inlineResult->NoteFatal(InlineObservation::CALLSITE_IS_TOO_DEEP);
            break;
        }
    }

    inlineResult->NoteInt(InlineObservation::CALLSITE_DEPTH, depth);
    return depth;
}

/*****************************************************************************
 *
 *  Inlining phase
 */


void                Compiler::fgInline()
{
    if (!opts.OptEnabled(CLFLG_INLINING))
        return;

#ifdef DEBUG
    if  (verbose)
        printf("*************** In fgInline()\n");
#endif // DEBUG

    BasicBlock* block = fgFirstBB;
    noway_assert(block != nullptr);

    // Set the root inline context on all statements
    InlineContext* rootContext = InlineContext::NewRoot(this);

    for (; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt();
             stmt;
             stmt = stmt->gtNextStmt)
        {
            stmt->gtInlineContext = rootContext;
        }
    }

    // Reset block back to start for inlining
    block = fgFirstBB;

    do
    {
        /* Make the current basic block address available globally */

        compCurBB = block;

        GenTreeStmt* stmt;
        GenTreePtr   expr;

        for (stmt = block->firstStmt();
             stmt != nullptr;
             stmt = stmt->gtNextStmt)
        {
            expr = stmt->gtStmtExpr;

            // See if we can expand the inline candidate
            if ((expr->gtOper == GT_CALL) && ((expr->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0))
            {
                GenTreeCall* call = expr->AsCall();
                InlineResult inlineResult(this, call, "fgInline");

                fgMorphStmt = stmt;

                fgMorphCallInline(call, &inlineResult);

                if (stmt->gtStmtExpr->IsNothingNode())
                {
                    fgRemoveStmt(block, stmt);
                    continue;
                }
            }
            else
            {
#ifdef DEBUG
                // Look for non-candidates.
                fgWalkTreePre(&stmt->gtStmtExpr, fgFindNonInlineCandidate, stmt);
#endif
            }

            // See if we need to replace the return value place holder.
            fgWalkTreePre(&stmt->gtStmtExpr,
                          fgUpdateInlineReturnExpressionPlaceHolder,
                          (void *) this);

            // See if stmt is of the form GT_COMMA(call, nop)
            // If yes, we can get rid of GT_COMMA.            
            if (expr->OperGet() == GT_COMMA &&
                expr->gtOp.gtOp1->OperGet() == GT_CALL &&
                expr->gtOp.gtOp2->OperGet() == GT_NOP)
            {
                stmt->gtStmtExpr = expr->gtOp.gtOp1;
            }
        }

        block = block->bbNext;

    } while (block);

#ifdef DEBUG

    // Check that we should not have any inline candidate or return value place holder left.

    block = fgFirstBB;
    noway_assert(block);

    do
    {
        GenTreeStmt* stmt;

        for (stmt = block->firstStmt();
             stmt;
             stmt = stmt->gtNextStmt)
        {
            // Call Compiler::fgDebugCheckInlineCandidates on each node
            fgWalkTreePre(&stmt->gtStmtExpr, fgDebugCheckInlineCandidates);
        }

        block = block->bbNext;

    } while (block);

    fgVerifyHandlerTab();

    if  (verbose)
    {
        printf("*************** After fgInline()\n");
        fgDispBasicBlocks(true);
        fgDispHandlerTab();
    }

    if  (verbose || fgPrintInlinedMethods)
    {
       printf("**************** Inline Tree\n");
       rootContext->Dump(this);
    }

#endif // DEBUG
}

#ifdef DEBUG

//------------------------------------------------------------------------
// fgFindNonInlineCandidate: tree walk helper to ensure that a tree node
// that is not an inline candidate is noted as a failed inline.
//
// Arguments:
//    pTree - pointer to pointer tree node being walked
//    data  - contextual data for the walk
//
// Return Value:
//    walk result
//
// Note:
//    Invokes fgNoteNonInlineCandidate on the nodes it finds.

Compiler::fgWalkResult      Compiler::fgFindNonInlineCandidate(GenTreePtr* pTree,
                                                               fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    if (tree->gtOper == GT_CALL)
    {
        Compiler*    compiler = data->compiler;
        GenTreePtr   stmt     = (GenTreePtr) data->pCallbackData;
        GenTreeCall* call     = tree->AsCall();

        compiler->fgNoteNonInlineCandidate(stmt, call);
    }
    return WALK_CONTINUE;
}

//------------------------------------------------------------------------
// fgNoteNonInlineCandidate: account for inlining failures in calls
// not marked as inline candidates.
//
// Arguments:
//    tree  - statement containing the call
//    call  - the call itself
//
// Notes:
//    Used in debug only to try and place descriptions of inline failures
//    into the proper context in the inline tree.

void Compiler::fgNoteNonInlineCandidate(GenTreePtr   tree,
                                        GenTreeCall* call)
{
    InlineResult inlineResult(this, call, "fgNotInlineCandidate");
    InlineObservation currentObservation = InlineObservation::CALLSITE_NOT_CANDIDATE;

    // Try and recover the reason left behind when the jit decided
    // this call was not a candidate.
    InlineObservation priorObservation = call->gtInlineObservation;

    if (InlIsValidObservation(priorObservation))
    {
        currentObservation = priorObservation;
    }

    // Would like to just call noteFatal here, since this
    // observation blocked candidacy, but policy comes into play
    // here too.  Also note there's no need to re-report these
    // failures, since we reported them during the initial
    // candidate scan.
    InlineImpact impact = InlGetImpact(currentObservation);

    if (impact == InlineImpact::FATAL)
    {
        inlineResult.NoteFatal(currentObservation);
    }
    else
    {
        inlineResult.Note(currentObservation);
    }

    inlineResult.SetReported();

    if (call->gtCallType == CT_USER_FUNC)
    {
        // Create InlineContext for the failure
        InlineContext::NewFailure(this, tree, &inlineResult);
    }
}

#endif

#if defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

/*********************************************************************************
 *
 * tree - The node which needs to be converted to a struct pointer.
 *
 *  Return the pointer by either __replacing__ the tree node with a suitable pointer
 *  type or __without replacing__ and just returning a subtree or by __modifying__
 *  a subtree.
 */
GenTreePtr Compiler::fgGetStructAsStructPtr(GenTreePtr tree)
{
    noway_assert((tree->gtOper == GT_LCL_VAR) ||
                 (tree->gtOper == GT_FIELD)   ||
                 (tree->gtOper == GT_IND)     ||
                 (tree->gtOper == GT_LDOBJ)   ||
                 tree->OperIsSIMD()           ||
                 // tree->gtOper == GT_CALL     || cannot get address of call.
                 // tree->gtOper == GT_MKREFANY || inlining should've been aborted due to mkrefany opcode.
                 // tree->gtOper == GT_RET_EXPR || cannot happen after fgUpdateInlineReturnExpressionPlaceHolder
                 (tree->gtOper == GT_COMMA));

    switch (tree->OperGet())
    {
    case GT_LDOBJ:
    case GT_IND:
        return tree->gtOp.gtOp1;

    case GT_COMMA:
        tree->gtOp.gtOp2 = fgGetStructAsStructPtr(tree->gtOp.gtOp2);
        tree->gtType = TYP_BYREF;
        return tree;

    default:
        return gtNewOperNode(GT_ADDR, TYP_BYREF, tree);
    }
}

/***************************************************************************************************
 * child     - The inlinee of the retExpr node.
 * retClsHnd - The struct class handle of the type of the inlinee.
 *
 * Assign the inlinee to a tmp, if it is a call, just assign it to a lclVar, else we can
 * use a copyblock to do the assignment.
 */
GenTreePtr Compiler::fgAssignStructInlineeToVar(GenTreePtr child, CORINFO_CLASS_HANDLE retClsHnd)
{
    assert(child->gtOper != GT_RET_EXPR && child->gtOper != GT_MKREFANY);

    unsigned tmpNum = lvaGrabTemp(false DEBUGARG("RetBuf for struct inline return candidates."));
    lvaSetStruct(tmpNum, retClsHnd, false);
    var_types structType = lvaTable[tmpNum].lvType;

    GenTreePtr dst = gtNewLclvNode(tmpNum, structType);

    // If we have a call, we'd like it to be: V00 = call(), but first check if
    // we have a ", , , call()" -- this is very defensive as we may never get
    // an inlinee that is made of commas. If the inlinee is not a call, then
    // we use a copy block to do the assignment.
    GenTreePtr src = child;
    GenTreePtr lastComma = NULL;
    while (src->gtOper == GT_COMMA)
    {
        lastComma = src;
        src = src->gtOp.gtOp2;
    }

    GenTreePtr newInlinee = NULL;
    if (src->gtOper == GT_CALL)
    {
        // If inlinee was just a call, new inlinee is v05 = call()
        newInlinee = gtNewAssignNode(dst, src);

        // If inlinee was comma, but a deeper call, new inlinee is (, , , v05 = call())
        if (child->gtOper == GT_COMMA)
        {
            lastComma->gtOp.gtOp2 = newInlinee;
            newInlinee = child;
        }
    }
    else
    {
        // Inlinee is not a call, so just create a copy block to the tmp.
        src = child;
        GenTreePtr dstAddr = fgGetStructAsStructPtr(dst);
        GenTreePtr srcAddr = fgGetStructAsStructPtr(src);
        newInlinee = gtNewCpObjNode(dstAddr, srcAddr, retClsHnd, false);
    }

    GenTreePtr production = gtNewLclvNode(tmpNum, structType);
    return gtNewOperNode(GT_COMMA, structType, newInlinee, production);
}

/***************************************************************************************************
 * tree      - The tree pointer that has one of its child nodes as retExpr.
 * child     - The inlinee child.
 * retClsHnd - The struct class handle of the type of the inlinee.
 *
 * V04 = call() assignments are okay as we codegen it. Everything else needs to be a copy block or
 * would need a temp. For example, a cast(ldobj) will then be, cast(v05 = ldobj, v05); But it is
 * a very rare (or impossible) scenario that we'd have a retExpr transform into a ldobj other than
 * a lclVar/call. So it is not worthwhile to do pattern matching optimizations like addr(ldobj(op1))
 * can just be op1.
 */
void Compiler::fgAttachStructInlineeToAsg(GenTreePtr tree, GenTreePtr child, CORINFO_CLASS_HANDLE retClsHnd)
{
    // We are okay to have:
    // 1. V02 = call();
    // 2. copyBlk(dstAddr, srcAddr);
    assert(tree->gtOper == GT_ASG);

    // We have an assignment, we codegen only V05 = call().
    if (child->gtOper == GT_CALL && tree->gtOp.gtOp1->gtOper == GT_LCL_VAR)
    {
        return;
    }

    GenTreePtr dstAddr = fgGetStructAsStructPtr(tree->gtOp.gtOp1);
    GenTreePtr srcAddr = fgGetStructAsStructPtr((child->gtOper == GT_CALL)
                            ? fgAssignStructInlineeToVar(child, retClsHnd) // Assign to a variable if it is a call.
                            : child);                                   // Just get the address, if not a call.

    tree->CopyFrom(gtNewCpObjNode(dstAddr, srcAddr, retClsHnd, false), this);
}

#endif // defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

/*****************************************************************************
 * Callback to replace the inline return expression place holder (GT_RET_EXPR)
 */

/* static */
Compiler::fgWalkResult      Compiler::fgUpdateInlineReturnExpressionPlaceHolder(GenTreePtr* pTree,
                                                                                fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    Compiler*  comp = data->compiler;

    if (tree->gtOper == GT_RET_EXPR)
    {
#if defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        // We are going to copy the tree from the inlinee, so save the handle now.
        CORINFO_CLASS_HANDLE retClsHnd = varTypeIsStruct(tree)
                                       ? tree->gtRetExpr.gtRetClsHnd
                                       : NO_CLASS_HANDLE;
#endif // defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

        do
        {
            // Obtained the expanded inline candidate
            GenTreePtr inlineCandidate;

            inlineCandidate = tree->gtRetExpr.gtInlineCandidate;

            // If the inlineCandidate node is a leaf, we can just overwrite "tree" with it.
            // But if it's not, we have to make sure to do a deep copy before overwriting it.
            if (inlineCandidate->OperIsLeaf())
            {
                tree->CopyFrom(inlineCandidate, comp);
            }
            else
            {
                tree->CopyFrom(comp->gtCloneExpr(inlineCandidate), comp);
#ifdef DEBUG
                comp->CopyTestDataToCloneTree(inlineCandidate, tree);
#endif // DEBUG
            }

#ifdef DEBUG
            if (false && comp->verbose)
            {

                printf("\nAfter updating the return expression place holder ");
                printTreeID(tree);
                printf(" for call ");
                printTreeID(inlineCandidate);
                printf(":\n");
                comp->gtDispTree(tree);
            }
#endif // DEBUG
        }
        while (tree->gtOper == GT_RET_EXPR);

#if defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#if defined(_TARGET_ARM_)
        if (retClsHnd != NO_CLASS_HANDLE && comp->IsHfa(retClsHnd))
#elif defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (retClsHnd != NO_CLASS_HANDLE && comp->IsRegisterPassable(retClsHnd))
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        {
            GenTreePtr parent = data->parent;
            // See assert below, we only look one level above for an asg parent.
            if (parent->gtOper == GT_ASG)
            {
                // Either lhs is a call V05 = call(); or lhs is addr, and asg becomes a copyBlk.
                comp->fgAttachStructInlineeToAsg(parent, tree, retClsHnd);
            }
            else
            {
                // Just assign the inlinee to a variable to keep it simple.
                tree->CopyFrom(comp->fgAssignStructInlineeToVar(tree, retClsHnd), comp);
            }
        }
#endif // defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    }

#if defined(DEBUG) && (defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING))
    // Make sure we don't have a tree like so: V05 = (, , , retExpr);
    // Since we only look one level above for the parent for '=' and
    // do not check if there is a series of COMMAs. See above.
    // Importer and FlowGraph will not generate such a tree, so just
    // leaving an assert in here. This can be fixed by looking ahead
    // when we visit GT_ASG similar to fgAttachStructInlineeToAsg.
    else if (tree->gtOper == GT_ASG &&
             tree->gtOp.gtOp2->gtOper == GT_COMMA)
    {
        GenTreePtr comma;
        for (comma = tree->gtOp.gtOp2;
             comma->gtOper == GT_COMMA;
             comma = comma->gtOp.gtOp2)
        {
            // empty
        }

#if defined(_TARGET_ARM_)
        noway_assert(!varTypeIsStruct(comma) ||
                     comma->gtOper != GT_RET_EXPR ||
                     (!comp->IsHfa(comma->gtRetExpr.gtRetClsHnd)));
#elif defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        noway_assert(!varTypeIsStruct(comma) ||
                     comma->gtOper != GT_RET_EXPR ||
                     (!comp->IsRegisterPassable(comma->gtRetExpr.gtRetClsHnd)));
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    }
#endif // defined(DEBUG) && (defined(_TARGET_ARM_) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING))

    return WALK_CONTINUE;
}

#ifdef DEBUG

/*****************************************************************************
 * Callback to make sure there is no more GT_RET_EXPR and GTF_CALL_INLINE_CANDIDATE nodes.
 */

/* static */
Compiler::fgWalkResult      Compiler::fgDebugCheckInlineCandidates(GenTreePtr* pTree,
                                                                   fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    if (tree->gtOper == GT_CALL)
    {
        assert((tree->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);
    }
    else
    {
        assert(tree->gtOper != GT_RET_EXPR);
    }

    return WALK_CONTINUE;
}

#endif // DEBUG


void       Compiler::fgInvokeInlineeCompiler(GenTreeCall*  call,
                                             InlineResult* inlineResult)
{
    noway_assert(call->gtOper == GT_CALL);
    noway_assert((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0);
    noway_assert(opts.OptEnabled(CLFLG_INLINING));

    // This is the InlineInfo struct representing a method to be inlined.
    InlineInfo inlineInfo = {0};

    CORINFO_METHOD_HANDLE fncHandle = call->gtCallMethHnd;

    inlineInfo.fncHandle             = fncHandle;
    inlineInfo.iciCall               = call;
    inlineInfo.iciStmt               = fgMorphStmt;
    inlineInfo.iciBlock              = compCurBB;
    inlineInfo.thisDereferencedFirst = false;
    inlineInfo.retExpr               = NULL;
    inlineInfo.inlineResult          = inlineResult;
#ifdef FEATURE_SIMD
    inlineInfo.hasSIMDTypeArgLocalOrReturn = false;
#endif // FEATURE_SIMD

    InlineCandidateInfo* inlineCandidateInfo = call->gtInlineCandidateInfo;
    noway_assert(inlineCandidateInfo);
    // Store the link to inlineCandidateInfo into inlineInfo
    inlineInfo.inlineCandidateInfo = inlineCandidateInfo;

    unsigned inlineDepth = fgCheckInlineDepthAndRecursion(&inlineInfo);

    if (inlineResult->IsFailure())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Recursive or deep inline recursion detected. Will not expand this INLINECANDIDATE \n");
        }
#endif // DEBUG
        return;
    }

    // Set the trap to catch all errors (including recoverable ones from the EE)
    struct Param
    {
        Compiler* pThis;
        GenTree* call;
        CORINFO_METHOD_HANDLE fncHandle;
        InlineCandidateInfo* inlineCandidateInfo;
        InlineInfo* inlineInfo;
    } param = {0};

    param.pThis = this;
    param.call = call;
    param.fncHandle = fncHandle;
    param.inlineCandidateInfo = inlineCandidateInfo;
    param.inlineInfo = &inlineInfo;
    setErrorTrap(info.compCompHnd, Param*, pParam, &param)
    {
        // Init the local var info of the inlinee
        pParam->pThis->impInlineInitVars(pParam->inlineInfo);

        if (pParam->inlineInfo->inlineResult->IsCandidate())
        {
            /* Clear the temp table */
            memset(pParam->inlineInfo->lclTmpNum, -1, sizeof(pParam->inlineInfo->lclTmpNum));

            //
            // Prepare the call to jitNativeCode
            //

            pParam->inlineInfo->InlinerCompiler = pParam->pThis;
            if (pParam->pThis->impInlineInfo == NULL)
            {
                pParam->inlineInfo->InlineRoot = pParam->pThis;
            }
            else
            {
                pParam->inlineInfo->InlineRoot = pParam->pThis->impInlineInfo->InlineRoot;
            }
            pParam->inlineInfo->argCnt                   = pParam->inlineCandidateInfo->methInfo.args.totalILArgs();
            pParam->inlineInfo->tokenLookupContextHandle = pParam->inlineCandidateInfo->exactContextHnd;

            JITLOG_THIS(pParam->pThis,
                        (LL_INFO100000,
                         "INLINER: inlineInfo.tokenLookupContextHandle for %s set to 0x%p:\n",
                         pParam->pThis->eeGetMethodFullName(pParam->fncHandle),
                         pParam->pThis->dspPtr(pParam->inlineInfo->tokenLookupContextHandle)));

            CORJIT_FLAGS compileFlagsForInlinee;
            memcpy(&compileFlagsForInlinee, pParam->pThis->opts.jitFlags, sizeof(compileFlagsForInlinee));
            compileFlagsForInlinee.corJitFlags &= ~CORJIT_FLG_LOST_WHEN_INLINING;
            compileFlagsForInlinee.corJitFlags |= CORJIT_FLG_SKIP_VERIFICATION;

#ifdef DEBUG
            if (pParam->pThis->verbose)
            {
                printf("\nInvoking compiler for the inlinee method %s :\n",
                       pParam->pThis->eeGetMethodFullName(pParam->fncHandle));
            }
#endif // DEBUG

            int result = jitNativeCode(pParam->fncHandle,
                          pParam->inlineCandidateInfo->methInfo.scope,
                          pParam->pThis->info.compCompHnd,
                          &pParam->inlineCandidateInfo->methInfo,
                          (void**)pParam->inlineInfo,
                          NULL,
                          &compileFlagsForInlinee,
                          pParam->inlineInfo);

            if (result != CORJIT_OK)
            {
                // If we haven't yet determined why this inline fails, use
                // a catch-all something bad happened observation.
                InlineResult* innerInlineResult = pParam->inlineInfo->inlineResult;

                if (!innerInlineResult->IsFailure())
                {
                    innerInlineResult->NoteFatal(InlineObservation::CALLSITE_COMPILATION_FAILURE);
                }
            }
        }
    }
    impErrorTrap()
    {
#ifdef DEBUG
        if (verbose)
        {
             printf("\nInlining failed due to an exception during invoking the compiler for the inlinee method %s.\n",
                    eeGetMethodFullName(fncHandle));
        }
#endif // DEBUG

        // If we haven't yet determined why this inline fails, use
        // a catch-all something bad happened observation.
        if (!inlineResult->IsFailure())
        {
            inlineResult->NoteFatal(InlineObservation::CALLSITE_COMPILATION_ERROR);
        }
    }
    endErrorTrap();

    if (inlineResult->IsFailure())
    {
#if defined(DEBUG) || MEASURE_INLINING
        ++Compiler::jitInlineInitVarsFailureCount;
#endif // defined(DEBUG) || MEASURE_INLINING
        return;
    }

#ifdef DEBUG
    if (0 && verbose)
    {
         printf("\nDone invoking compiler for the inlinee method %s\n",
                eeGetMethodFullName(fncHandle));
    }
#endif // DEBUG

    // If there is non-NULL return, but we haven't set the pInlineInfo->retExpr,
    // That means we haven't imported any BB that contains CEE_RET opcode.
    // (This could happen for example for a BBJ_THROW block fall through a BBJ_RETURN block which
    // causes the BBJ_RETURN block not to be imported at all.)
    // Fail the inlining attempt
    if (inlineCandidateInfo->fncRetType != TYP_VOID && inlineInfo.retExpr == NULL)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nInlining failed because pInlineInfo->retExpr is not set in the inlinee method %s.\n",
                    eeGetMethodFullName(fncHandle));
        }
#endif // DEBUG
        inlineResult->NoteFatal(InlineObservation::CALLEE_LACKS_RETURN);
        return;
    }

    if (inlineCandidateInfo->initClassResult & CORINFO_INITCLASS_SPECULATIVE)
    {
        // we defer the call to initClass() until inlining is completed in case it fails. If inlining succeeds,
        // we will call initClass().
        if (!(info.compCompHnd->initClass(NULL /* field */, fncHandle /* method */,
                inlineCandidateInfo->exactContextHnd /* context */) & CORINFO_INITCLASS_INITIALIZED))
        {
            inlineResult->NoteFatal(InlineObservation::CALLEE_CLASS_INIT_FAILURE);
            return;
        }
    }

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // The inlining attempt cannot be failed starting from this point.
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    // We've successfully obtain the list of inlinee's basic blocks.
    // Let's insert it to inliner's basic block list.
    fgInsertInlineeBlocks(&inlineInfo);

#ifdef DEBUG
    ++fgInlinedCount;

    if (verbose || fgPrintInlinedMethods)
    {
        printf("Successfully inlined %s (%d IL bytes) (depth %d) [%s]\n",
               eeGetMethodFullName(fncHandle),
               inlineCandidateInfo->methInfo.ILCodeSize,
               inlineDepth,
               inlineResult->ReasonString());
    }

    if (verbose)
    {
        printf("--------------------------------------------------------------------------------------------\n");
    }
#endif // DEBUG

#if defined(DEBUG)
    impInlinedCodeSize += inlineCandidateInfo->methInfo.ILCodeSize;
#endif

    // We inlined...
    inlineResult->NoteSuccess();
}

// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// The inlining attempt cannot be failed starting from this point.
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
void Compiler::fgInsertInlineeBlocks(InlineInfo* pInlineInfo)
{
    GenTreePtr   iciCall  = pInlineInfo->iciCall;
    GenTreePtr   iciStmt  = pInlineInfo->iciStmt;
    BasicBlock*  iciBlock = pInlineInfo->iciBlock;
    BasicBlock*  block;

    // We can write better assert here. For example, we can check that
    // iciBlock contains iciStmt, which in turn contains iciCall.
    noway_assert(iciBlock->bbTreeList != NULL);
    noway_assert(iciStmt->gtStmt.gtStmtExpr != NULL);
    noway_assert(iciCall->gtOper == GT_CALL);

#ifdef DEBUG

    GenTreePtr currentDumpStmt = NULL;

    if (verbose)
    {
        printf("\n\n----------- Statements (and blocks) added due to the inlining of call ");
        printTreeID(iciCall);
        printf(" -----------\n");
        // gtDispTree(iciStmt);
    }

#endif // DEBUG

#if defined(DEBUG) || MEASURE_INLINING

    if (iciStmt->gtNext == NULL)
    {
        ++Compiler::jitIciStmtIsTheLastInBB;
    }

    if (InlineeCompiler->fgBBcount == 1)
    {
        ++Compiler::jitInlineeContainsOnlyOneBB;
    }

#endif // defined(DEBUG) || MEASURE_INLINING

    //
    // Create a new inline context and mark the inlined statements with it
    //
    InlineContext* calleeContext = InlineContext::NewSuccess(this, pInlineInfo);

    for (block = InlineeCompiler->fgFirstBB;
         block != nullptr;
         block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt();
             stmt;
             stmt = stmt->gtNextStmt)
        {
            stmt->gtInlineContext = calleeContext;
        }
    }

    //
    // Prepend statements.
    //
    GenTreePtr stmtAfter;
    stmtAfter = fgInlinePrependStatements(pInlineInfo);

#ifdef DEBUG
    if (verbose)
    {
        currentDumpStmt = stmtAfter;
        printf("\nInlinee method body:");
    }
#endif // DEBUG

    if (InlineeCompiler->fgBBcount == 1)
    {
        // When fgBBCount is 1 we will always have a non-NULL fgFirstBB
        //
        PREFAST_ASSUME(InlineeCompiler->fgFirstBB != NULL);

        // DDB 91389: Don't throw away the (only) inlinee block
        // when its return type is not BBJ_RETURN.
        // In other words, we need its BBJ_ to perform the right thing.
        if (InlineeCompiler->fgFirstBB->bbJumpKind == BBJ_RETURN)
        {
            // Inlinee contains just one BB. So just insert its statement list to topBlock.
            if (InlineeCompiler->fgFirstBB->bbTreeList)
            {
                stmtAfter = fgInsertStmtListAfter(iciBlock,
                                                  stmtAfter,
                                                  InlineeCompiler->fgFirstBB->bbTreeList);
            }
#ifdef DEBUG
            if (verbose)
            {
                noway_assert(currentDumpStmt);

                if  (currentDumpStmt != stmtAfter)
                {
                    do
                    {
                        currentDumpStmt = currentDumpStmt->gtNext;

                        printf("\n");

                        noway_assert(currentDumpStmt->gtOper == GT_STMT);

                        gtDispTree(currentDumpStmt);
                        printf("\n");

                    }  while (currentDumpStmt != stmtAfter);
                }
            }
#endif // DEBUG
            goto _Done;
        }
    }

    //
    // ======= Inserting inlinee's basic blocks ===============
    //

    BasicBlock* topBlock;
    BasicBlock* bottomBlock;

    topBlock = iciBlock;

    bottomBlock = fgNewBBafter(topBlock->bbJumpKind, topBlock, true);
    bottomBlock->bbRefs = 1;
    bottomBlock->bbJumpDest = topBlock->bbJumpDest;
    bottomBlock->inheritWeight(topBlock);

    topBlock->bbJumpKind = BBJ_NONE;

    // Update block flags
    unsigned originalFlags;
    originalFlags = topBlock->bbFlags;
    noway_assert((originalFlags & BBF_SPLIT_NONEXIST) == 0);
    topBlock->bbFlags    &= ~(BBF_SPLIT_LOST);
    bottomBlock->bbFlags |= originalFlags & BBF_SPLIT_GAINED;

    //
    // Split statements between topBlock and bottomBlock
    //
    GenTreePtr topBlock_Begin;
    GenTreePtr topBlock_End;
    GenTreePtr bottomBlock_Begin;
    GenTreePtr bottomBlock_End;

    topBlock_Begin = NULL;
    topBlock_End = NULL;
    bottomBlock_Begin = NULL;
    bottomBlock_End = NULL;

    //
    // First figure out bottomBlock_Begin
    //

    bottomBlock_Begin = stmtAfter->gtNext;

    if (topBlock->bbTreeList == NULL)
    {
        // topBlock is empty before the split.
        // In this case, both topBlock and bottomBlock should be empty
        noway_assert(bottomBlock_Begin == NULL);
        topBlock->bbTreeList = NULL;
        bottomBlock->bbTreeList = NULL;
    }
    else if (topBlock->bbTreeList == bottomBlock_Begin)
    {
        noway_assert(bottomBlock_Begin);

        // topBlock contains at least one statement before the split.
        // And the split is before the first statement.
        // In this case, topBlock should be empty, and everything else should be moved to the bottonBlock.
        bottomBlock->bbTreeList = topBlock->bbTreeList;
        topBlock->bbTreeList = NULL;
    }
    else if (bottomBlock_Begin == NULL)
    {
        noway_assert(topBlock->bbTreeList);

        // topBlock contains at least one statement before the split.
        // And the split is at the end of the topBlock.
        // In this case, everything should be kept in the topBlock, and the bottomBlock should be empty

        bottomBlock->bbTreeList = NULL;
    }
    else
    {
        noway_assert(topBlock->bbTreeList);
        noway_assert(bottomBlock_Begin);

        // This is the normal case where both blocks should contain at least one statement.
        topBlock_Begin  = topBlock->bbTreeList;
        noway_assert(topBlock_Begin);
        topBlock_End    = bottomBlock_Begin->gtPrev;
        noway_assert(topBlock_End);
        bottomBlock_End = topBlock->lastStmt();
        noway_assert(bottomBlock_End);

        // Break the linkage between 2 blocks.
        topBlock_End->gtNext = NULL;

        // Fix up all the pointers.
        topBlock->bbTreeList         = topBlock_Begin;
        topBlock->bbTreeList->gtPrev = topBlock_End;

        bottomBlock->bbTreeList         = bottomBlock_Begin;
        bottomBlock->bbTreeList->gtPrev = bottomBlock_End;
    }

    //
    // Set the try and handler index and fix the jump types of inlinee's blocks.
    //

    bool inheritWeight;
    inheritWeight = true; // The firstBB does inherit the weight from the iciBlock

    for (block = InlineeCompiler->fgFirstBB;
         block != NULL;
         block = block->bbNext)
    {
        noway_assert(!block->hasTryIndex());
        noway_assert(!block->hasHndIndex());
        block->copyEHRegion(iciBlock);
        block->bbFlags   |=  iciBlock->bbFlags & BBF_BACKWARD_JUMP;

        if (iciStmt->gtStmt.gtStmtILoffsx != BAD_IL_OFFSET)
        {
            block->bbCodeOffs    = jitGetILoffs(iciStmt->gtStmt.gtStmtILoffsx);
            block->bbCodeOffsEnd = block->bbCodeOffs + 1;  // TODO: is code size of 1 some magic number for inlining?
        }
        else
        {
            block->bbCodeOffs     = 0; // TODO: why not BAD_IL_OFFSET?
            block->bbCodeOffsEnd  = 0;
            block->bbFlags    |= BBF_INTERNAL;
        }

        if (block->bbJumpKind == BBJ_RETURN)
        {
            inheritWeight = true; // A return block does inherit the weight from the iciBlock
            noway_assert((block->bbFlags & BBF_HAS_JMP) == 0);
            if (block->bbNext)
            {
                block->bbJumpKind = BBJ_ALWAYS;
                block->bbJumpDest = bottomBlock;
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nConvert bbJumpKind of BB%02u to BBJ_ALWAYS to bottomBlock BB%02u\n",
                           block->bbNum, bottomBlock->bbNum);
                }
#endif // DEBUG
            }
            else
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nConvert bbJumpKind of BB%02u to BBJ_NONE\n", block->bbNum);
                }
#endif // DEBUG
                block->bbJumpKind = BBJ_NONE;
            }
        }
        if (inheritWeight)
        {
            block->inheritWeight(iciBlock);
            inheritWeight = false;
        }
        else
        {
            block->modifyBBWeight(iciBlock->bbWeight / 2);
        }
    }

    // Insert inlinee's blocks into inliner's block list.
    topBlock->setNext(InlineeCompiler->fgFirstBB);
    InlineeCompiler->fgLastBB->setNext(bottomBlock);

    //
    // Add inlinee's block count to inliner's.
    //
    fgBBcount += InlineeCompiler->fgBBcount;

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks(InlineeCompiler->fgFirstBB, InlineeCompiler->fgLastBB, true);
    }
#endif // DEBUG

_Done:

    //
    // At this point, we have successully inserted inlinee's code.
    //

    //
    // Copy out some flags
    //
    compLongUsed              |= InlineeCompiler->compLongUsed;
    compFloatingPointUsed     |= InlineeCompiler->compFloatingPointUsed;
    compLocallocUsed          |= InlineeCompiler->compLocallocUsed;
    compQmarkUsed             |= InlineeCompiler->compQmarkUsed;
    compUnsafeCastUsed        |= InlineeCompiler->compUnsafeCastUsed;
    compNeedsGSSecurityCookie |= InlineeCompiler->compNeedsGSSecurityCookie;
    compGSReorderStackLayout  |= InlineeCompiler->compGSReorderStackLayout;

    // If there is non-NULL return, replace the GT_CALL with its return value expression,
    // so later it will be picked up by the GT_RET_EXPR node.
    if ((pInlineInfo->inlineCandidateInfo->fncRetType != TYP_VOID) || (iciCall->gtCall.gtReturnType == TYP_STRUCT))
    {
        noway_assert(pInlineInfo->retExpr);
        iciCall->CopyFrom(pInlineInfo->retExpr, this);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nReturn expression for inlinee ");
            printTreeID(iciCall);
            printf(" :\n");
            gtDispTree(iciCall);
        }
#endif // DEBUG
    }

    //
    // Detach the GT_CALL node from the original statement by hanging a "nothing" node under it,
    // so that fgMorphStmts can remove the statment once we return from here.
    //
    iciStmt->gtStmt.gtStmtExpr = gtNewNothingNode();
}

// Prepend the statements that are needed before the inlined call.
// Return the last statement that is prepended.

GenTreePtr      Compiler::fgInlinePrependStatements(InlineInfo* inlineInfo)
{
    BasicBlock* block = inlineInfo->iciBlock;

    GenTreePtr callStmt  = inlineInfo->iciStmt;
    noway_assert(callStmt->gtOper == GT_STMT);
    IL_OFFSETX callILOffset = callStmt->gtStmt.gtStmtILoffsx;

    GenTreePtr afterStmt = callStmt; // afterStmt is the place where the new statements should be inserted after.
    GenTreePtr newStmt;

    GenTreePtr call = inlineInfo->iciCall;
    noway_assert(call->gtOper == GT_CALL);

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nfgInlinePrependStatements for iciCall= ");
        printTreeID(call);
        printf(":\n");
    }
#endif

    // Prepend statements for any initialization / side effects

    InlArgInfo*       inlArgInfo = inlineInfo->inlArgInfo;
    InlLclVarInfo*    lclVarInfo = inlineInfo->lclVarInfo;

    GenTreePtr tree;

    // Create the null check statement (but not appending it to the statement list yet) for the 'this' pointer if necessary.
    // The NULL check should be done after "argument setup statements".
    // The only reason we move it here is for calling "impInlineFetchArg(0,..." to reserve a temp
    // for the "this" pointer.
    // Note: Here we no longer do the optimization that was done by thisDereferencedFirst in the old inliner.
    // However the assetionProp logic will remove any unecessary null checks that we may have added
    //
    GenTreePtr nullcheck = NULL;

    if (call->gtFlags & GTF_CALL_NULLCHECK && !inlineInfo->thisDereferencedFirst)
    {
        // Call impInlineFetchArg to "reserve" a temp for the "this" pointer.
        nullcheck = gtNewOperNode(GT_IND, TYP_INT,
                                  impInlineFetchArg(0, inlArgInfo, lclVarInfo));
        nullcheck->gtFlags |= GTF_EXCEPT;

        // The NULL-check statement will be inserted to the statement list after those statements
        // that assign arguments to temps and before the actual body of the inlinee method.
    }

    /* Treat arguments that had to be assigned to temps */
    if (inlineInfo->argCnt)
    {

#ifdef DEBUG
        if (verbose)
        {
            printf("\nArguments setup:\n");
        }
#endif // DEBUG

        for (unsigned argNum = 0; argNum < inlineInfo->argCnt; argNum++)
        {
            if (inlArgInfo[argNum].argHasTmp)
            {
                noway_assert(inlArgInfo[argNum].argIsUsed);

                /* argBashTmpNode is non-NULL iff the argument's value was
                   referenced exactly once by the original IL. This offers an
                   oppportunity to avoid an intermediate temp and just insert
                   the original argument tree.

                   However, if the temp node has been cloned somewhere while
                   importing (e.g. when handling isinst or dup), or if the IL
                   took the address of the argument, then argBashTmpNode will
                   be set (because the value was only explicitly retrieved
                   once) but the optimization cannot be applied.
                 */

                GenTreePtr argSingleUseNode = inlArgInfo[argNum].argBashTmpNode;

                if (argSingleUseNode &&
                    !(argSingleUseNode->gtFlags & GTF_VAR_CLONED) &&
                    !inlArgInfo[argNum].argHasLdargaOp)
                {
                    /* Change the temp in-place to the actual argument */

                    argSingleUseNode->CopyFrom(inlArgInfo[argNum].argNode, this);
                    continue;
                }
                else
                {
                    /* Create the temp assignment for this argument */

                    CORINFO_CLASS_HANDLE structType = DUMMY_INIT(0);

                    if (varTypeIsStruct(lclVarInfo[argNum].lclTypeInfo))
                    {
                        if (inlArgInfo[argNum].argNode->gtOper == GT_LDOBJ)
                        {
                            structType = inlArgInfo[argNum].argNode->gtLdObj.gtClass;
                        }
                        else if (inlArgInfo[argNum].argNode->gtOper == GT_MKREFANY)
                        {
                            structType = lclVarInfo[argNum].lclVerTypeInfo.GetClassHandle();
                        }
                        else
                        {
                            noway_assert(!"Unknown struct type");
                        }
                    }

                    // Unsafe value cls check is not needed for argTmpNum here since in-linee compiler instance would have
                    // iterated over these and marked them accordingly.
                    impAssignTempGen(inlArgInfo[argNum].argTmpNum,
                                     inlArgInfo[argNum].argNode,
                                     structType,
                                     (unsigned)CHECK_SPILL_NONE,
                                     & afterStmt,
                                     callILOffset,
                                     block);

#ifdef DEBUG
                    if (verbose)
                    {
                        gtDispTree(afterStmt);
                    }
#endif // DEBUG

                }
            }
            else if (inlArgInfo[argNum].argIsByRefToStructLocal)
            {
                // Do nothing.
            }
            else
            {
                /* The argument is either not used or a const or lcl var */

                noway_assert(!inlArgInfo[argNum].argIsUsed  ||
                              inlArgInfo[argNum].argIsInvariant ||
                              inlArgInfo[argNum].argIsLclVar );

                /* Make sure we didnt change argNode's along the way, or else
                   subsequent uses of the arg would have worked with the bashed value */
                if (inlArgInfo[argNum].argIsInvariant)
                {
                    assert(inlArgInfo[argNum].argNode->OperIsConst() ||
                           inlArgInfo[argNum].argNode->gtOper == GT_ADDR);
                }
                noway_assert((inlArgInfo[argNum].argIsLclVar == 0) ==
                             (inlArgInfo[argNum].argNode->gtOper != GT_LCL_VAR || (inlArgInfo[argNum].argNode->gtFlags & GTF_GLOB_REF)));

                /* If the argument has side effects, append it */

                if (inlArgInfo[argNum].argHasSideEff)
                {
                    noway_assert(inlArgInfo[argNum].argIsUsed == false);

                    if (inlArgInfo[argNum].argNode->gtOper == GT_LDOBJ ||
                        inlArgInfo[argNum].argNode->gtOper == GT_MKREFANY)
                    {
                        // Don't put GT_LDOBJ node under a GT_COMMA.
                        // Codegen can't deal with it.
                        // Just hang the address here in case there are side-effect.
                        newStmt = gtNewStmt(gtUnusedValNode(inlArgInfo[argNum].argNode->gtOp.gtOp1), callILOffset);
                    }
                    else
                    {
                        newStmt = gtNewStmt(gtUnusedValNode(inlArgInfo[argNum].argNode), callILOffset);
                    }
                    afterStmt = fgInsertStmtAfter(block, afterStmt, newStmt);

#ifdef DEBUG
                    if (verbose)
                    {
                        gtDispTree(afterStmt);
                    }
#endif // DEBUG

                }
            }
        }
    }

    // Add the CCTOR check if asked for.
    // Note: We no longer do the optimization that is done before by staticAccessedFirstUsingHelper in the old inliner.
    //       Therefore we might prepend redundant call to HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
    //       before the inlined method body, even if a static field of this type was accessed in the inlinee
    //       using a helper before any other observable side-effect.

    if (inlineInfo->inlineCandidateInfo->initClassResult & CORINFO_INITCLASS_USE_HELPER)
    {
        CORINFO_CONTEXT_HANDLE exactContext = inlineInfo->inlineCandidateInfo->exactContextHnd;
        CORINFO_CLASS_HANDLE exactClass;

        if (((SIZE_T)exactContext & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
        {
            exactClass = CORINFO_CLASS_HANDLE((SIZE_T)exactContext & ~CORINFO_CONTEXTFLAGS_MASK);
        }
        else
        {
            exactClass = info.compCompHnd->getMethodClass(CORINFO_METHOD_HANDLE((SIZE_T)exactContext & ~CORINFO_CONTEXTFLAGS_MASK));
        }

        tree = fgGetSharedCCtor(exactClass);
        newStmt = gtNewStmt(tree, callILOffset);
        afterStmt = fgInsertStmtAfter(block, afterStmt, newStmt);
    }

    // Insert the nullcheck statement now.
    if (nullcheck)
    {
        newStmt = gtNewStmt(nullcheck, callILOffset);
        afterStmt = fgInsertStmtAfter(block, afterStmt, newStmt);
    }

    //
    // Now zero-init inlinee locals
    //

    CORINFO_METHOD_INFO* InlineeMethodInfo = InlineeCompiler->info.compMethodInfo;

    unsigned lclCnt = InlineeMethodInfo->locals.numArgs;

    // Does callee contain any zero-init local?
    if ((lclCnt != 0) &&
        (InlineeMethodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0)
    {

#ifdef DEBUG
        if (verbose)
        {
            printf("\nZero init inlinee locals:\n");
        }
#endif // DEBUG

        for (unsigned lclNum = 0; lclNum < lclCnt; lclNum++)
        {
            unsigned  tmpNum = inlineInfo->lclTmpNum[lclNum];

            // Is the local used at all?
            if (tmpNum != BAD_VAR_NUM)
            {
                var_types lclTyp = (var_types)lvaTable[tmpNum].lvType;
                noway_assert(lclTyp == lclVarInfo[lclNum + inlineInfo->argCnt].lclTypeInfo);

                if (!varTypeIsStruct(lclTyp))
                {
                    // Unsafe value cls check is not needed here since in-linee compiler instance would have
                    // iterated over locals and marked accordingly.
                    impAssignTempGen(tmpNum,
                                     gtNewZeroConNode(genActualType(lclTyp)),
                                     NO_CLASS_HANDLE,
                                     (unsigned)CHECK_SPILL_NONE,
                                     & afterStmt,
                                     callILOffset,
                                     block);
                }
                else
                {
                    CORINFO_CLASS_HANDLE structType = lclVarInfo[lclNum + inlineInfo->argCnt].lclVerTypeInfo.GetClassHandle();

                    tree = gtNewOperNode(GT_ADDR, TYP_BYREF,
                                         gtNewLclvNode(tmpNum, lclTyp));

                    tree = gtNewBlkOpNode(GT_INITBLK,
                                          tree,             // Dest
                                          gtNewIconNode(0), // Value
                                          gtNewIconNode(info.compCompHnd->getClassSize(structType)), // Size
                                          false);           // volatil

                    newStmt = gtNewStmt(tree, callILOffset);
                    afterStmt = fgInsertStmtAfter(block, afterStmt, newStmt);
                }

#ifdef DEBUG
                if (verbose)
                {
                    gtDispTree(afterStmt);
                }
#endif // DEBUG
            }
        }
    }

    return afterStmt;
}


/*****************************************************************************/
/*static*/
Compiler::fgWalkResult  Compiler::fgChkThrowCB(GenTreePtr* pTree,
                                               fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    // If this tree doesn't have the EXCEPT flag set, then there is no
    // way any of the child nodes could throw, so we can stop recursing.
    if (!(tree->gtFlags & GTF_EXCEPT))
    {
        return Compiler::WALK_SKIP_SUBTREES;
    }

    switch (tree->gtOper)
    {
    case GT_MUL:
    case GT_ADD:
    case GT_SUB:
    case GT_ASG_ADD:
    case GT_ASG_SUB:
    case GT_CAST:
        if (tree->gtOverflow())
            return Compiler::WALK_ABORT;
        break;

    case GT_INDEX:
        if (tree->gtFlags & GTF_INX_RNGCHK)
            return Compiler::WALK_ABORT;
        break;

    case GT_ARR_BOUNDS_CHECK:
        return Compiler::WALK_ABORT;

    default:
        break;
    }

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult  Compiler::fgChkLocAllocCB(GenTreePtr* pTree,
                                                  fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    if (tree->gtOper == GT_LCLHEAP)
        return Compiler::WALK_ABORT;

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult  Compiler::fgChkQmarkCB(GenTreePtr* pTree,
                                               fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    if (tree->gtOper == GT_QMARK)
        return Compiler::WALK_ABORT;

    return Compiler::WALK_CONTINUE;
}


void Compiler::fgLclFldAssign(unsigned lclNum)
{
    assert(varTypeIsStruct(lvaTable[lclNum].lvType));
    if (lvaTable[lclNum].lvPromoted && lvaTable[lclNum].lvFieldCnt > 1)
    {
        lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DNER_LocalField));
    }
}
