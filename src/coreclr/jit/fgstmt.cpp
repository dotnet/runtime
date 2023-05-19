// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Statements

#ifdef DEBUG
// Check to see if block contains a statement but don't spend more than a certain
// budget doing this per method compiled.
// If the budget is exceeded, return 'answerOnBoundExceeded' as the answer.
/* static */
bool Compiler::fgBlockContainsStatementBounded(BasicBlock* block,
                                               Statement*  stmt,
                                               bool        answerOnBoundExceeded /*= true*/)
{
    const __int64 maxLinks = 100000000;

    __int64* numTraversed = &JitTls::GetCompiler()->compNumStatementLinksTraversed;

    if (*numTraversed > maxLinks)
    {
        return answerOnBoundExceeded;
    }

    Statement* curr = block->firstStmt();
    do
    {
        (*numTraversed)++;
        if (curr == stmt)
        {
            break;
        }
        curr = curr->GetNextStmt();
    } while (curr != nullptr);
    return curr != nullptr;
}
#endif // DEBUG

//------------------------------------------------------------------------
// fgInsertStmtAtBeg: Insert the given statement at the start of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
// Notes:
//    We always insert phi statements at the beginning.
//    In other cases, if there are any phi assignments and/or an assignment of
//    the GT_CATCH_ARG, we insert after those.
//
void Compiler::fgInsertStmtAtBeg(BasicBlock* block, Statement* stmt)
{
    Statement* firstStmt = block->firstStmt();

    if (stmt->IsPhiDefnStmt())
    {
        // The new tree will now be the first one of the block.
        block->bbStmtList = stmt;
        stmt->SetNextStmt(firstStmt);

        // Are there any statements in the block?
        if (firstStmt != nullptr)
        {
            // There is at least one statement already.
            Statement* lastStmt = firstStmt->GetPrevStmt();
            noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);

            // Insert the statement in front of the first one.
            firstStmt->SetPrevStmt(stmt);
            stmt->SetPrevStmt(lastStmt);
        }
        else
        {
            // The block was completely empty.
            stmt->SetPrevStmt(stmt);
        }
    }
    else
    {
        Statement* insertBeforeStmt = block->FirstNonPhiDefOrCatchArgAsg();
        if (insertBeforeStmt != nullptr)
        {
            fgInsertStmtBefore(block, insertBeforeStmt, stmt);
        }
        else
        {
            // There were no non-phi/non-catch arg statements, insert `stmt` at the end.
            fgInsertStmtAtEnd(block, stmt);
        }
    }
}

//------------------------------------------------------------------------
// fgNewStmtAtBeg: Insert the given tree as a new statement at the start of the given basic block.
//
// Arguments:
//   block - the block into which 'tree' will be inserted;
//   tree  - the tree to be inserted.
//   di    - the debug info to use for the new statement.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
Statement* Compiler::fgNewStmtAtBeg(BasicBlock* block, GenTree* tree, const DebugInfo& di)
{
    Statement* stmt = gtNewStmt(tree, di);
    fgInsertStmtAtBeg(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtAtEnd: Insert the given statement at the end of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   If the block can be a conditional block, use fgInsertStmtNearEnd.
//
void Compiler::fgInsertStmtAtEnd(BasicBlock* block, Statement* stmt)
{

    assert(stmt->GetNextStmt() == nullptr); // We don't set it, and it needs to be this after the insert

    Statement* firstStmt = block->firstStmt();
    if (firstStmt != nullptr)
    {
        // There is at least one statement already.
        Statement* lastStmt = firstStmt->GetPrevStmt();
        noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);

        // Append the statement after the last one.
        lastStmt->SetNextStmt(stmt);
        stmt->SetPrevStmt(lastStmt);
        firstStmt->SetPrevStmt(stmt);
    }
    else
    {
        // The block is completely empty.
        block->bbStmtList = stmt;
        stmt->SetPrevStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgNewStmtAtEnd: Insert the given tree as a new statement at the end of the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   tree  - the tree to be inserted.
//   di    - the debug info to use for the new statement.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
// Note:
//   If the block can be a conditional block, use fgNewStmtNearEnd.
//
Statement* Compiler::fgNewStmtAtEnd(BasicBlock* block, GenTree* tree, const DebugInfo& di)
{
    Statement* stmt = gtNewStmt(tree, di);
    fgInsertStmtAtEnd(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtNearEnd: Insert the given statement at the end of the given basic block,
//   but before the terminating node, if present.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   stmt  - the statement to be inserted.
//
void Compiler::fgInsertStmtNearEnd(BasicBlock* block, Statement* stmt)
{
    // This routine can only be used when in tree order.
    assert(fgOrder == FGOrderTree);

    if (block->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_COND, BBJ_SWITCH, BBJ_RETURN))
    {
        Statement* firstStmt = block->firstStmt();
        noway_assert(firstStmt != nullptr);
        Statement* lastStmt = block->lastStmt();
        noway_assert(lastStmt != nullptr && lastStmt->GetNextStmt() == nullptr);
        Statement* insertionPoint = lastStmt->GetPrevStmt();

#if DEBUG
        if (block->KindIs(BBJ_COND))
        {
            assert(lastStmt->GetRootNode()->OperIs(GT_JTRUE));
        }
        else if (block->KindIs(BBJ_RETURN))
        {
            assert((lastStmt->GetRootNode()->OperIs(GT_RETURN, GT_JMP)) ||
                   // BBJ_RETURN blocks in functions returning void do not get a GT_RETURN node if they
                   // have a .tail prefix (even if canTailCall returns false for these calls)
                   // code:Compiler::impImportBlockCode (search for the RET: label)
                   // Ditto for real tail calls (all code after them has been removed)
                   (lastStmt->GetRootNode()->OperIs(GT_CALL) &&
                    ((info.compRetType == TYP_VOID) || lastStmt->GetRootNode()->AsCall()->IsTailCall())));
        }
        else if (block->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET))
        {
            assert(lastStmt->GetRootNode()->OperIs(GT_RETFILT));
        }
        else
        {
            assert(block->KindIs(BBJ_SWITCH));
            assert(lastStmt->GetRootNode()->OperIs(GT_SWITCH));
        }
#endif // DEBUG

        // Append 'stmt' before 'lastStmt'.
        stmt->SetNextStmt(lastStmt);
        lastStmt->SetPrevStmt(stmt);

        if (firstStmt == lastStmt)
        {
            // There is only one stmt in the block.
            block->bbStmtList = stmt;
            stmt->SetPrevStmt(lastStmt);
        }
        else
        {
            // Append 'stmt' after 'insertionPoint'.
            noway_assert(insertionPoint != nullptr && (insertionPoint->GetNextStmt() == lastStmt));
            insertionPoint->SetNextStmt(stmt);
            stmt->SetPrevStmt(insertionPoint);
        }
    }
    else
    {
        fgInsertStmtAtEnd(block, stmt);
    }
}

//------------------------------------------------------------------------
// fgNewStmtNearEnd: Insert the given tree as a new statement at the end of the given basic block,
//   but before the GT_JTRUE, if present.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   tree  - the tree to be inserted.
//   di    - the debug info to use for the new statement.
//
// Return Value:
//    The new created statement with `tree` inserted into `block`.
//
Statement* Compiler::fgNewStmtNearEnd(BasicBlock* block, GenTree* tree, const DebugInfo& di)
{
    Statement* stmt = gtNewStmt(tree, di);
    fgInsertStmtNearEnd(block, stmt);
    return stmt;
}

//------------------------------------------------------------------------
// fgInsertStmtAfter: Insert the given statement after the insertion point in the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   insertionPoint - the statement after which `stmt` will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   `block` is needed to update the last statement pointer and for debugging checks.
//
void Compiler::fgInsertStmtAfter(BasicBlock* block, Statement* insertionPoint, Statement* stmt)
{
    assert(block->bbStmtList != nullptr);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint->GetNextStmt() == nullptr)
    {
        // Ok, we want to insert after the last statement of the block.
        stmt->SetNextStmt(nullptr);
        stmt->SetPrevStmt(insertionPoint);

        insertionPoint->SetNextStmt(stmt);

        // Update the backward link of the first statement of the block
        // to point to the new last statement.
        assert(block->bbStmtList->GetPrevStmt() == insertionPoint);
        block->bbStmtList->SetPrevStmt(stmt);
    }
    else
    {
        stmt->SetNextStmt(insertionPoint->GetNextStmt());
        stmt->SetPrevStmt(insertionPoint);

        insertionPoint->GetNextStmt()->SetPrevStmt(stmt);
        insertionPoint->SetNextStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgInsertStmtBefore: Insert the given statement before the insertion point in the given basic block.
//
// Arguments:
//   block - the block into which 'stmt' will be inserted;
//   insertionPoint - the statement before which `stmt` will be inserted;
//   stmt  - the statement to be inserted.
//
// Note:
//   `block` is needed to update the first statement pointer and for debugging checks.
//
void Compiler::fgInsertStmtBefore(BasicBlock* block, Statement* insertionPoint, Statement* stmt)
{
    assert(block->bbStmtList != nullptr);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));
    assert(!fgBlockContainsStatementBounded(block, stmt, false));

    if (insertionPoint == block->bbStmtList)
    {
        // We're inserting before the first statement in the block.
        Statement* first = block->firstStmt();
        Statement* last  = block->lastStmt();

        stmt->SetNextStmt(first);
        stmt->SetPrevStmt(last);

        block->bbStmtList = stmt;
        first->SetPrevStmt(stmt);
    }
    else
    {
        stmt->SetNextStmt(insertionPoint);
        stmt->SetPrevStmt(insertionPoint->GetPrevStmt());

        insertionPoint->GetPrevStmt()->SetNextStmt(stmt);
        insertionPoint->SetPrevStmt(stmt);
    }
}

//------------------------------------------------------------------------
// fgInsertStmtListAfter: Insert the list of statements stmtList after the stmtAfter in block.
//
// Arguments:
//   block - the block where stmtAfter is in;
//   stmtAfter - the statement where stmtList should be inserted after;
//   stmtList - the statement list to insert.
//
// Return value:
//   the last statement in the united list.
//
Statement* Compiler::fgInsertStmtListAfter(BasicBlock* block, Statement* stmtAfter, Statement* stmtList)
{
    // Currently we can handle when stmtAfter and stmtList are non-NULL. This makes everything easy.
    noway_assert(stmtAfter != nullptr);
    noway_assert(stmtList != nullptr);

    // Last statement in a non-empty list, circular in the GetPrevStmt() list.
    Statement* stmtLast = stmtList->GetPrevStmt();
    noway_assert(stmtLast != nullptr);
    noway_assert(stmtLast->GetNextStmt() == nullptr);

    Statement* stmtNext = stmtAfter->GetNextStmt();

    if (stmtNext == nullptr)
    {
        stmtAfter->SetNextStmt(stmtList);
        stmtList->SetPrevStmt(stmtAfter);
        block->bbStmtList->SetPrevStmt(stmtLast);
    }
    else
    {
        stmtAfter->SetNextStmt(stmtList);
        stmtList->SetPrevStmt(stmtAfter);

        stmtLast->SetNextStmt(stmtNext);
        stmtNext->SetPrevStmt(stmtLast);
    }

    noway_assert(block->bbStmtList == nullptr || block->bbStmtList->GetPrevStmt()->GetNextStmt() == nullptr);

    return stmtLast;
}

/*****************************************************************************
 *
 *  Create a new statement from tree and wire the links up.
 */
Statement* Compiler::fgNewStmtFromTree(GenTree* tree, BasicBlock* block, const DebugInfo& di)
{
    Statement* stmt = gtNewStmt(tree, di);

    if (fgNodeThreading == NodeThreading::AllTrees)
    {
        gtSetStmtInfo(stmt);
        fgSetStmtSeq(stmt);
    }
    else if (fgNodeThreading == NodeThreading::AllLocals)
    {
        fgSequenceLocals(stmt);
    }

#if DEBUG
    if (block != nullptr)
    {
        fgDebugCheckNodeLinks(block, stmt);
    }
#endif

    return stmt;
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree)
{
    return fgNewStmtFromTree(tree, nullptr, DebugInfo());
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree, BasicBlock* block)
{
    return fgNewStmtFromTree(tree, block, DebugInfo());
}

Statement* Compiler::fgNewStmtFromTree(GenTree* tree, const DebugInfo& di)
{
    return fgNewStmtFromTree(tree, nullptr, di);
}

//------------------------------------------------------------------------
// fgUnlinkStmt: unlink a statement from a block's statement list
//
// Arguments:
//   block - the block from which 'stmt' will be unlinked
//   stmt  - the statement to be unlinked
//
// Notes:
//   next and previous links are nulled out, in anticipation
//   of this statement being re-inserted somewhere else.
//
void Compiler::fgUnlinkStmt(BasicBlock* block, Statement* stmt)
{
    constexpr bool isUnlink = true;
    fgRemoveStmt(block, stmt DEBUGARG(isUnlink));
    stmt->SetNextStmt(nullptr);
    stmt->SetPrevStmt(nullptr);
}

//------------------------------------------------------------------------
// fgRemoveStmt: remove a statement from a block's statement list
//
// Arguments:
//   block - the block from which 'stmt' will be removed
//   stmt  - the statement to be removed
//   isUnlink - ultimate plan is to move the statement, not delete it
//
void Compiler::fgRemoveStmt(BasicBlock* block, Statement* stmt DEBUGARG(bool isUnlink))
{
    assert(fgOrder == FGOrderTree);

#ifdef DEBUG
    // Don't print if it is a GT_NOP. Too much noise from the inliner.
    if (verbose && (stmt->GetRootNode()->gtOper != GT_NOP))
    {
        printf("\n%s ", isUnlink ? "unlinking" : "removing useless");
        gtDispStmt(stmt);
        printf(" from " FMT_BB "\n", block->bbNum);
    }
#endif // DEBUG

    if (opts.compDbgCode && stmt->GetPrevStmt() != stmt && stmt->GetDebugInfo().IsValid())
    {
        /* TODO: For debuggable code, should we remove significant
           statement boundaries. Or should we leave a GT_NO_OP in its place? */
    }

    Statement* firstStmt = block->firstStmt();
    if (firstStmt == stmt) // Is it the first statement in the list?
    {
        if (firstStmt->GetNextStmt() == nullptr)
        {
            assert(firstStmt == block->lastStmt());

            /* this is the only statement - basic block becomes empty */
            block->bbStmtList = nullptr;
        }
        else
        {
            block->bbStmtList = firstStmt->GetNextStmt();
            block->bbStmtList->SetPrevStmt(firstStmt->GetPrevStmt());
        }
    }
    else if (stmt == block->lastStmt()) // Is it the last statement in the list?
    {
        stmt->GetPrevStmt()->SetNextStmt(nullptr);
        block->bbStmtList->SetPrevStmt(stmt->GetPrevStmt());
    }
    else // The statement is in the middle.
    {
        assert(stmt->GetPrevStmt() != nullptr && stmt->GetNextStmt() != nullptr);

        Statement* prev = stmt->GetPrevStmt();

        prev->SetNextStmt(stmt->GetNextStmt());
        stmt->GetNextStmt()->SetPrevStmt(prev);
    }

    noway_assert(!optValnumCSE_phase);

    fgStmtRemoved = true;

#ifdef DEBUG
    if (verbose)
    {
        if (block->bbStmtList == nullptr)
        {
            printf("\n" FMT_BB " becomes empty\n", block->bbNum);
        }
    }
#endif // DEBUG
}

/******************************************************************************/
// Returns true if the operator is involved in control-flow.
// TODO-Cleanup: Make this a GenTreeOperKind.
//
inline bool OperIsControlFlow(genTreeOps oper)
{
    switch (oper)
    {
        case GT_JTRUE:
        case GT_JCMP:
        case GT_JTEST:
        case GT_JCC:
        case GT_SWITCH:
        case GT_LABEL:

        case GT_CALL:
        case GT_JMP:

        case GT_RETURN:
        case GT_RETFILT:
#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            return true;

        default:
            return false;
    }
}

/******************************************************************************
 *  Tries to throw away a stmt. The statement can be anywhere in block->bbStmtList.
 *  Returns true if it did remove the statement.
 */

bool Compiler::fgCheckRemoveStmt(BasicBlock* block, Statement* stmt)
{
    if (opts.compDbgCode)
    {
        return false;
    }

    GenTree*   tree = stmt->GetRootNode();
    genTreeOps oper = tree->OperGet();

    if (OperIsControlFlow(oper) || oper == GT_NO_OP)
    {
        return false;
    }

    // TODO: Use a recursive version of gtNodeHasSideEffects()
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        return false;
    }

    fgRemoveStmt(block, stmt);
    return true;
}
