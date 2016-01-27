// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "hashbv.h"


#ifdef DEBUG

void dumpMethod()
{
    if (VERBOSE)
        GetTlsCompiler()->fgDispBasicBlocks(true);
}

void dumpTreeStack(Compiler *comp, ArrayStack<GenTree *> *stack)
{
    printf("=TOS=================\n");
    for (int i=0; i<stack->Height(); i++)
    {
        comp->gtDispNode(stack->Index(i), 0, "");
        printf("\n");
    }
    printf("=====================\n");
}

void dumpArgTable(Compiler *comp, GenTree *call)
{
    noway_assert(call->IsCall());
    fgArgInfoPtr argInfo = call->gtCall.fgArgInfo;
    noway_assert(argInfo != NULL);

    unsigned            argCount = argInfo->ArgCount();
    fgArgTabEntryPtr *  argTable = argInfo->ArgTable();
    fgArgTabEntryPtr    curArgTabEntry = NULL; 

    JITDUMP("ARG TABLE for call ");
    Compiler::printTreeID(call);
    JITDUMP(":\n");
    for (unsigned i=0; i < argCount; i++)
    {
        curArgTabEntry = argTable[i];
        JITDUMP("entry %d\n", i);
        DISPTREE(curArgTabEntry->node);
    }
    JITDUMP("--------------ARG TABLE END --------------\n");
}

#endif // DEBUG



// state carried over the tree walk, to be used in making
// a splitting decision.
struct SplitData
{
    // callbacks to determine if we should split here, in pre and post order traversals
    Compiler::fgSplitPredicate *predicatePre;
    Compiler::fgSplitPredicate *predicatePost;
    
    GenTree *root; // root stmt of tree being processed
    BasicBlock *block;
    Rationalizer *thisPhase;
    
    bool continueSubtrees; // whether to continue after splitting off a tree (in pre-order)
};
   
//------------------------------------------------------------------------------
// RewriteOneQuestion - split a question op into three parts: the test and branch, 
//                      and true and false parts, with accompanying flow
//------------------------------------------------------------------------------

Location Rationalizer::RewriteOneQuestion(BasicBlock *block, GenTree *qmarkTree, GenTree *stmt, GenTree *dst)
{
    // First create all the blocks this is going to turn into.
    // We end the current block here and insert a diamond
    // consisting of the then/else blocks and the remainder, which is
    // the point where flow merges back and the rest of the current block will end up

    // TODO-Cleanup: avoid creating one of these blocks if that part of the qmark is a NOP
    BasicBlock* remainderBlock = comp->fgSplitBlockAfterStatement(block, stmt);
    BasicBlock*      elseBlock = comp->fgSplitBlockAfterStatement(block, stmt);
    BasicBlock*      thenBlock = comp->fgSplitBlockAfterStatement(block, stmt);

    // wire up the flow between the blocks and adjust their preds
    block->bbJumpKind = BBJ_COND;
    block->bbJumpDest = elseBlock;
        
    thenBlock->bbJumpKind = BBJ_ALWAYS;
    thenBlock->bbJumpDest = remainderBlock;
    thenBlock->bbFlags &= ~BBF_JMP_TARGET;

    elseBlock->bbJumpKind = BBJ_NONE; // falls through to remainder
    elseBlock->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
    comp->fgAddRefPred(elseBlock, block);

    //JITDUMP("after splitting Q1");
    //dumpMethod();

    remainderBlock->bbFlags |= (BBF_JMP_TARGET | BBF_HAS_LABEL);
    comp->fgAddRefPred(remainderBlock, thenBlock);
    comp->fgRemoveRefPred(elseBlock, thenBlock);

    //JITDUMP("after splitting Q2");
    //dumpMethod();

    // remove flag marking this as a question conditional
    GenTree *conditionExpr = qmarkTree->gtGetOp1();
    assert(conditionExpr->gtFlags & GTF_RELOP_QMARK);
    conditionExpr->gtFlags &= ~GTF_RELOP_QMARK;
    
    comp->gtReverseCond(conditionExpr);
    
    // Wire up the jmp part.
    // Note that, unlike later Rationalizer passes, the QMarks are done prior to the comma
    // processing, and therefore prior to the creation of embedded statements.
    // So we can safely re-sequence.
    GenTree *jmpStmt = comp->fgNewStmtFromTree(comp->gtNewOperNode(GT_JTRUE, TYP_VOID, qmarkTree->gtGetOp1()), qmarkTree->gtStmt.gtStmtILoffsx);
    comp->fgInsertStmtAtEnd(block, jmpStmt);

    DBEXEC(TRUE, ValidateStatement(Location(jmpStmt, block)));

    //JITDUMP("before splitting Q3");
    //dumpMethod();
    //DBEXEC(TRUE, comp->fgDebugCheckBBlist());

    GenTree *trueExpr = qmarkTree->gtGetOp2()->gtGetOp2();
    GenTree *falseExpr = qmarkTree->gtGetOp2()->gtGetOp1();
 
    IL_OFFSETX ilOffset = stmt->gtStmt.gtStmtILoffsx;
    comp->fgRemoveStmt(block, stmt, false);

    unsigned lclNum = 0;
    bool resultUsed = false;

    // if the dst of the qmark was a local then we can write directly to it
    // otherwise make a temp and then do the indir/field/whatever writeback
    if (dst && dst->IsLocal())
    {
        resultUsed = true;
        lclNum = dst->gtLclVarCommon.gtLclNum;

        // Increment its lvRefCnt and lvRefCntWtd
        comp->lvaTable[lclNum].incRefCnts(block->getBBWeight(comp), comp);
    }
    else if (qmarkTree->TypeGet() != TYP_VOID)
    {
        resultUsed = true; // just guessing here
        lclNum = comp->lvaGrabTemp(true DEBUGARG("lower question"));
        comp->lvaTable[lclNum].lvType = qmarkTree->TypeGet();

        // Increment its lvRefCnt and lvRefCntWtd twice, one for the def and one for the use
        comp->lvaTable[lclNum].incRefCnts(block->getBBWeight(comp), comp);
        comp->lvaTable[lclNum].incRefCnts(block->getBBWeight(comp), comp);
    }

    // assign the trueExpr into the dst or tmp, insert in thenBlock
    if (trueExpr->OperGet() != GT_NOP)
    {
        if (trueExpr->TypeGet() != TYP_VOID)
        {
            assert(resultUsed);
            trueExpr = CreateTempAssignment(comp, lclNum, trueExpr);
        }
        GenTree *trueStmt = comp->fgNewStmtFromTree(trueExpr, thenBlock, ilOffset);
        comp->fgInsertStmtAtEnd(thenBlock, trueStmt);
    }

    // assign the falseExpr into the dst or tmp, insert in elseBlock
    if (falseExpr->OperGet() != GT_NOP)
    {
        if (falseExpr->TypeGet() != TYP_VOID)
        {
            assert(resultUsed);
            falseExpr = CreateTempAssignment(comp, lclNum, falseExpr);
        }
        GenTree *falseStmt = comp->fgNewStmtFromTree(falseExpr, elseBlock, ilOffset);
        comp->fgInsertStmtAtEnd(elseBlock, falseStmt);
    }

    // if the dst is a local we have just written it out 
    // but if not (like an indir or something) then we copy from the temp we allocated
    if (dst && !dst->IsLocal())
    {
        GenTree *writeback = comp->gtNewAssignNode(dst, comp->gtNewLclvNode(lclNum, qmarkTree->TypeGet()));
        GenTree *writeStmt = comp->fgNewStmtFromTree(writeback, remainderBlock, ilOffset);
        comp->fgInsertStmtAtBeg(remainderBlock, writeStmt);
    }

    //JITDUMP("after splitting all");
    //dumpMethod();

    return Location(jmpStmt, block);
}

//------------------------------------------------------------------------------
// isNodeCallArg - given a context (stack of parent nodes), determine if the TOS is an arg to a call
//------------------------------------------------------------------------------

GenTree *isNodeCallArg(ArrayStack<GenTree *> *parentStack)
{
    for (int i = 1; // 0 is current node, so start at 1
         i < parentStack->Height();
         i++)
    {
        GenTree *node = parentStack->Index(i);
        switch (node->OperGet())
        {
        case GT_LIST:
        case GT_ARGPLACE:
            break;
        case GT_NOP:
            // Currently there's an issue when the rationalizer performs
            // the fixup of a call argument: the case is when we remove an
            // inserted NOP as a parent of a call introduced by fgMorph;
            // when then the rationalizer removes it, the tree stack in the 
            // walk is not consistent with the node it was just deleted, so the
            // solution is just to go 1 level deeper.
            // TODO-Cleanup: This has to be fixed in a proper way: make the rationalizer 
            // correctly modify the evaluation stack when removing treenodes.
            if (node->gtOp.gtOp1->gtOper == GT_CALL)
            {
                return node->gtOp.gtOp1;
            }
            break;
        case GT_CALL:
            return node;
        default:
            return NULL;
        }
    }
    return NULL;
}

//------------------------------------------------------------------------------
// shouldSplitRationalPre - invoked in preorder in a tree walk to determine if 
//                          we should split at this point in the tree, and at this 
//                          point in the walk
//------------------------------------------------------------------------------

bool shouldSplitRationalPre(GenTree *tree, GenTree *parent, 
                            Compiler::fgWalkData *data)
{
    if (!parent || parent->gtOper == GT_STMT)
        return false;
    
    // the only thing we split in preorder are qmark ops
    if ((tree->OperGet() == GT_QMARK)
        && parent->gtOper != GT_ASG)
        return true;

    return false;
}


//------------------------------------------------------------------------------
// shouldSplitRationalPost - invoked in postorder in a tree walk to determine if 
//                           we should split at this point in the tree, and at this 
//                           point in the walk
//------------------------------------------------------------------------------

bool shouldSplitRationalPost(GenTree *tree, GenTree *parent, 
                             Compiler::fgWalkData *data)
{
    if (!parent || parent->gtOper == GT_STMT)
        return false;
    
    SplitData *splitData = (SplitData *) data->pCallbackData;
    auto phase = splitData->thisPhase;

#ifndef _TARGET_AMD64_
    if (tree->OperIsAssignment() 
        // late args are not truly embedded assigns... or at least they are very limited ones
        && !( isNodeCallArg(data->parentStack) && (tree->gtFlags & GTF_LATE_ARG)))
    {
        JITDUMP("splitting at assignment\n");
        return true;
    }

    if (tree->IsCall() && !parent->OperIsAssignment())
    {
        JITDUMP("splitting a nested call\n");
        return true;
    }
#endif

    if ((tree->OperGet() == GT_QMARK)
        && parent->gtOper != GT_ASG)
    {
        JITDUMP("splitting a question\n");
        return true;
    }

    return false;
}

//------------------------------------------------------------------------------
// fgSpliceTreeBefore - insert the given subtree 'tree' as a top level statement 
//                      placed before top level statement 'insertionPoint'
//------------------------------------------------------------------------------

GenTreeStmt *
Compiler::fgSpliceTreeBefore(BasicBlock* block, GenTreeStmt* insertionPoint, GenTree* tree, IL_OFFSETX ilOffset)
{
    assert(tree->gtOper != GT_STMT);
    assert(fgBlockContainsStatementBounded(block, insertionPoint));

    GenTreeStmt* newStmt = gtNewStmt(tree, ilOffset);
    newStmt->CopyCosts(tree);
    GenTreePtr newStmtFirstNode = Compiler::fgGetFirstNode(tree);
    newStmt->gtStmt.gtStmtList = newStmtFirstNode;
    newStmtFirstNode->gtPrev = nullptr;
    tree->gtNext = nullptr;

    fgInsertStmtBefore(block, insertionPoint, newStmt);

    return newStmt;
}

//------------------------------------------------------------------------------
// fgMakeEmbeddedStmt: insert the given subtree as an embedded statement 
//
// Arguments:
//    block - The block containing the parentStmt, into which the new embedded
//            statement will go
//    tree  - The tree that will be the gtStmtExpr of the new embedded statement
//    parentStmt - The top-level statement that 'tree' is embedded in
//
// Return Value:
//    A pointer to the new statement.
//
// Assumptions:
//    'tree' is fully contained in the linear order of parentStmt
//
// Notes:
//    If 'tree' is at the beginning of the linear order of 'parentStmt', it
//    is made into a top-level statement.

GenTreeStmt*
Compiler::fgMakeEmbeddedStmt(BasicBlock* block, GenTree* tree, GenTree* parentStmt)
{
    assert(tree->gtOper != GT_STMT);
    assert(parentStmt->gtOper == GT_STMT);
    assert(fgBlockContainsStatementBounded(block, parentStmt));

    GenTreePtr newStmtFirstNode = fgGetFirstNode(tree);
    GenTreePtr parentStmtFirstNode = parentStmt->gtStmt.gtStmtList;
    GenTreePtr prevStmt = parentStmt;
    bool newTopLevelStmt = false;
    bool splitParentStmt = false;
    if (newStmtFirstNode == parentStmtFirstNode)
    {
        // If this is the first node of the new statement, split them.
        parentStmt->gtStmt.gtStmtList = tree->gtNext;
        prevStmt = parentStmt->gtPrev;
        splitParentStmt = true;
    }
    GenTreeStmt* newStmt = gtNewStmt(tree, parentStmt->gtStmt.gtStmtILoffsx);    // Use same IL offset as parent statement
    newStmt->CopyCosts(tree);
    newStmt->gtStmtList = newStmtFirstNode;
    if (splitParentStmt && parentStmt->gtStmt.gtStmtIsTopLevel())
    {
        newTopLevelStmt = true;
        tree->gtNext->gtPrev = nullptr;
        tree->gtNext = nullptr;
    }
    else
    {
        newStmt->gtFlags &= ~(GTF_STMT_TOP_LEVEL);
    }

    // Does parentStmt already have embedded statements?
    // If so, determine where this fits in the linear order.
    // Note that if we have the splitParentStmt case, some of parentStmt's embedded statements
    // may need to move with the new statement
    GenTreePtr nextStmt = parentStmt->gtNext;
    GenTreePtr nextLinearNode;
    GenTreePtr searchNode;
    if (splitParentStmt)
    {
        nextLinearNode = newStmtFirstNode;
        // In this case, we're going to search for the LAST linear node in the new statement
        // in order to determine which embedded statements will move with this one.
        searchNode = tree;
    }
    else
    {
        nextLinearNode = parentStmt->gtStmt.gtStmtList;
        // In this case, we're going to search for the FIRST linear node in the new statement
        // so that we can insert this after any embedded statements that START before it.
        searchNode = newStmtFirstNode;
    }
    // Remember if we find any embedded statements before encountering 'searchNode'.
    bool foundEmbeddedStmts = false;
    while (nextStmt != nullptr && nextStmt->gtStmt.gtStmtIsEmbedded())
    {
        GenTreePtr nextEmbeddedNode = nextStmt->gtStmt.gtStmtList;
        while (nextLinearNode != searchNode && nextLinearNode != nextEmbeddedNode)
        {
            nextLinearNode = nextLinearNode->gtNext;
            assert (nextLinearNode != nullptr);
        }
        if (nextLinearNode == searchNode)
            break;
        prevStmt = nextStmt;
        nextStmt = nextStmt->gtNext;
        foundEmbeddedStmts = true;
    }

    if (newTopLevelStmt)
    {
        // For this case, we are actually going to insert it BEFORE parentStmt.
        // However if we have a new prevStmt (i.e. there are some embedded statements
        // to be included in newStmt) then those need to be moved as well.
        // Note, however, that all the tree links have alraedy been fixed up.
        fgInsertStmtBefore(block, parentStmt, newStmt);
        if (foundEmbeddedStmts)
        {
            GenTreePtr firstEmbeddedStmt = parentStmt->gtNext;
            assert(firstEmbeddedStmt->gtStmt.gtStmtIsEmbedded());
            assert(prevStmt->gtStmt.gtStmtIsEmbedded());
            parentStmt->gtNext = prevStmt->gtNext;
            if (parentStmt->gtNext != nullptr)
            {
                parentStmt->gtNext->gtPrev = parentStmt;
            }
            else
            {
                block->bbTreeList->gtPrev = parentStmt;
            }

            parentStmt->gtPrev = prevStmt;
            prevStmt->gtNext = parentStmt;

            newStmt->gtNext = firstEmbeddedStmt;
            firstEmbeddedStmt->gtPrev = newStmt;
        }
    }
    else
    {
        fgInsertStmtAfter(block, prevStmt, newStmt);
    }

    return newStmt;
}

//------------------------------------------------------------------------------
// fgInsertLinearNodeBefore: insert the given single node before 'before'.
//
// Arguments:
//    newNode - The node to be inserted
//    before  - The node to insert it before
//
// Return Value:
//    None.
//
// Assumptions:
//    Either the callee must ensure that 'before' is part of compCurStmt,
//    or before->gtPrev must be non-null

void
Compiler::fgInsertLinearNodeBefore(GenTreePtr newNode, GenTreePtr before)
{
    GenTreePtr prevNode = before->gtPrev;
    newNode->gtPrev = prevNode;
    if (prevNode == nullptr)
    {
        assert(compCurStmt->gtStmt.gtStmtList == before && compCurStmt->gtStmt.gtStmtIsTopLevel());
    }
    else
    {
        prevNode->gtNext = newNode;
    }
    // Note that 'before' may be the first node in gtStmtList even if its gtPrev is non-null,
    // since compCurStmt may be embedded.
    if (compCurStmt->gtStmt.gtStmtList == before)
    {
        compCurStmt->gtStmt.gtStmtList = newNode;
    }
    newNode->gtNext = before;
    before->gtPrev = newNode;
}

//-----------------------------------------------------------------------------------------------
// fgInsertEmbeddedFormTemp: Assign a variable to hold the result of *ppTree, possibly creating a new variable
//                           and creating a new (possibly embedded) statement for it.  The original
//                           subtree will be replaced with a use of the temp.
//
// Arguments:
//    ppTree  - a pointer to the child node we will be replacing with a reference to the new temp.
//    lclNum  - local var to use, or BAD_VAR_NUM to create one
//
// Return Value:
//    The new statement.
//
// Assumptions:
//    The callee must ensure that '*ppTree' is part of compCurStmt, and that
//    compCurStmt is in compCurBB;

GenTreeStmt*
Compiler::fgInsertEmbeddedFormTemp(GenTree** ppTree, unsigned lclNum)
{
    GenTree* subTree = *ppTree;

    if (lclNum == BAD_VAR_NUM)
    {
        lclNum = lvaGrabTemp(true DEBUGARG("fgInsertEmbeddedFormTemp is creating a new local variable"));
    }

    // Increment its lvRefCnt and lvRefCntWtd twice, one for the def and one for the use
    lvaTable[lclNum].incRefCnts(compCurBB->getBBWeight(this), this);
    lvaTable[lclNum].incRefCnts(compCurBB->getBBWeight(this), this);

    GenTreeLclVar* store = gtNewTempAssign(lclNum, subTree)->AsLclVar();
    gtSetEvalOrder(store);

    subTree->InsertAfterSelf(store);

    GenTree* load = new (this, GT_LCL_VAR) GenTreeLclVar(store->TypeGet(), store->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);
    gtSetEvalOrder(load);

    store->InsertAfterSelf(load);

    *ppTree = load;

    JITDUMP("fgInsertEmbeddedFormTemp created store :\n");
    DISPTREE(store);

    GenTreeStmt* stmt = fgMakeEmbeddedStmt(compCurBB, store, compCurStmt);
    stmt->gtStmtILoffsx = compCurStmt->gtStmt.gtStmtILoffsx;
#ifdef DEBUG
    stmt->gtStmtLastILoffs = compCurStmt->gtStmt.gtStmtLastILoffs;
#endif // DEBUG

    return stmt;
}

//------------------------------------------------------------------------------
// RenameUpdatedVars - detect trees that have internal assignments with preceding reads 
//                     of the variables being written.
//                     Replace the preceding reads with references to copies made in advance
//                     in order to make breaking out the assignments legal
//------------------------------------------------------------------------------

void Rationalizer::RenameUpdatedVars(Location loc)
{
    // A variable which is assigned within the tree will have different 
    // values at different points in the tree.  The rationalizer tries to
    // break internal assignments out into their own tree and place those trees before
    // the original tree.  This could result in changed meaning unless
    // we have a way of differentiating between original and modified values

    GenTree *statement = loc.tree;
    assert(statement->IsStatement());

    GenTree *tree = loc.tree->gtStmt.gtStmtExpr;

    use->ZeroAll();
    usedef->ZeroAll();
    rename->ZeroAll();
    unexp->ZeroAll();
    
    int *renameMap = (int *) alloca(sizeof(int) * comp->lvaCount);
    var_types *renameTypeMap = (var_types *) alloca(sizeof(var_types) * comp->lvaCount);

    // find locals that are redefined within the tree
    foreach_treenode_execution_order(tree, statement)
    {
        if (tree->IsLocal())
        {
            int varIdx = tree->gtLclVarCommon.gtLclNum;
            if (tree->gtFlags & GTF_VAR_DEF       // definition
                || tree->gtFlags & GTF_VAR_USEDEF // this is a use/def as in x=x+y (only the lhs x is tagged)
                || tree->gtFlags & GTF_VAR_USEASG)// this is a use/def for a x<op>=y
            {
                if (use->testBit(varIdx))
                {
                    usedef->setBit(varIdx);
                }
                else
                {
                    unexp->setBit(varIdx);
                }
            }
            else
            {
                if (usedef->testBit(varIdx))
                {
                    rename->setBit(varIdx);
                    renameTypeMap[varIdx] = tree->TypeGet();
                }
                else
                {
                    use->setBit(varIdx); // it's a plain use
                }
            }
        }
    }

    if (!rename->anySet())
        return;

    indexType index;

    // create the new variables and establish the mapping
    // also insert copies before the statement
    FOREACH_HBV_BIT_SET(index, rename)
    {
        JITDUMP("had to rename idx:%d in tree!\n", index);
        DISPTREE(statement);

        unsigned tmpIndex = 
            renameMap[index] = 
            comp->lvaGrabTemp(true DEBUGARG("rationalize renaming"));

        LclVarDsc *newVar = &comp->lvaTable[tmpIndex];

        newVar->lvType = renameTypeMap[index];

        // Increment its lvRefCnt and lvRefCntWtd
        comp->lvaTable[tmpIndex].incRefCnts(loc.block->getBBWeight(comp), comp);

        // only need a copy for exposed uses, otherwise a def is the first occurence
        if (!unexp->testBit(index))
        {
            GenTree *write = comp->gtNewAssignNode(comp->gtNewLclvNode(renameMap[index], newVar->TypeGet()), 
                                                   comp->gtNewLclvNode((int)index, newVar->TypeGet()));

            write = comp->fgNewStmtFromTree(write, statement->gtStmt.gtStmtILoffsx);
            comp->fgInsertStmtBefore(loc.block, statement, write);

            JITDUMP("New write tree:\n");
            DISPTREE(write);
        }
    }
    NEXT_HBV_BIT_SET;

    hashBv *seenUse = hashBv::Create(this->comp);
    hashBv *seenRedef = hashBv::Create(this->comp);

    // we are looking for a def after use
    // don't just start renaming if it kicks off with a def
    foreach_treenode_execution_order(tree, statement)
    {
        if (tree->IsLocal())
        {
            int varIdx = tree->gtLclVarCommon.gtLclNum;
            if (rename->testBit(varIdx))
            {
                if (tree->gtFlags & GTF_VAR_DEF
                    //|| tree->gtFlags & GTF_VAR_USEDEF
                    || tree->gtFlags & GTF_VAR_USEASG)
                {
                    if (seenUse->testBit(varIdx))
                        seenRedef->setBit(varIdx);
                }
                else
                {
                    seenUse->setBit(varIdx);
                }
                if (!seenRedef->testBit(varIdx))
                {
                    tree->gtLclVarCommon.SetLclNum(renameMap[varIdx]);
                }
            }
        }
    }
}

// return op that is the store equivalent of the given load opcode
genTreeOps storeForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
    case GT_LCL_VAR:
        return GT_STORE_LCL_VAR;
    case GT_LCL_FLD:
        return GT_STORE_LCL_FLD;
    case GT_REG_VAR:
        noway_assert(!"reg vars only supported in classic backend\n");
        unreached();
    default:
        noway_assert(!"not a data load opcode\n");
        unreached();
    }
}

// return op that is the addr equivalent of the given load opcode
genTreeOps addrForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
    case GT_LCL_VAR:
        return GT_LCL_VAR_ADDR;
    case GT_LCL_FLD:
        return GT_LCL_FLD_ADDR;
    default:
        noway_assert(!"not a data load opcode\n");
        unreached();
    }
}

// copy the flags determined by mask from src to dst
void copyFlags(GenTree *dst, GenTree *src, unsigned mask)
{
    dst->gtFlags &= ~mask;
    dst->gtFlags |= (src->gtFlags & mask);
}


//------------------------------------------------------------------------------
// RewriteQuestions - transform qmark ops, expanding them into multiple blocks
//                    They should all be at the top level or immediately under an
//                    assignment at this point
//------------------------------------------------------------------------------

Location Rationalizer::RewriteQuestions(Location loc)
{
    GenTree *topNode = loc.tree->gtStmt.gtStmtExpr;
    // we should have things broken up so all questions are at top level
    if (topNode->gtOper == GT_QMARK)
    {
        loc = RewriteOneQuestion(loc.block, topNode, loc.tree, NULL);
        return loc;
    }
    else if (topNode->gtOper == GT_ASG)
    {
        // hope there's never a QMARK on the LHS
        assert(topNode->gtGetOp1()->gtOper != GT_QMARK);

        GenTree *questionOp = topNode->gtGetOp2();
        if (questionOp->gtOper != GT_QMARK)
            return loc;

        loc = RewriteOneQuestion(loc.block, questionOp, loc.tree, topNode->gtGetOp1());
        return loc;
    }
    else
    {
        return loc;
    }
}


//--------------------------------------------------------------------------------------
// RewriteTopLevelComma - split a top-level comma into two top level statements.
//                                returns (as out params) the two new locations
//--------------------------------------------------------------------------------------

void Rationalizer::RewriteTopLevelComma(Location loc, Location* out1, Location* out2)
{
    GenTreeStmt* commaStmt = loc.tree->AsStmt();
    GenTree* commaOp = commaStmt->gtStmtExpr;

    assert(commaOp->OperGet() == GT_COMMA);
    JITDUMP("splitting top level comma!\n");
    
    GenTreeStmt* newStatement1 = comp->fgSpliceTreeBefore(loc.block, commaStmt, commaOp->gtGetOp1(), commaStmt->gtStmtILoffsx);
    GenTreeStmt* newStatement2 = comp->fgSpliceTreeBefore(loc.block, commaStmt, commaOp->gtGetOp2(), commaStmt->gtStmtILoffsx);
    
    comp->fgRemoveStmt(loc.block, commaStmt, false);
    
    // these two subtrees still need to be processed
    loc = Location(newStatement1, loc.block);

    *out1 = Location(newStatement1, loc.block);
    *out2 = Location(newStatement2, loc.block);
}

//--------------------------------------------------------------------------------------
// TreeTransformRationalization - Run the set of rationalizations on one statement that
//                                transforms its underlying trees but doesn't perform 
//                                tree walks to introduce new statements.
//--------------------------------------------------------------------------------------

Location Rationalizer::TreeTransformRationalization(Location loc)
{
top:
    assert(loc.tree);

    DBEXEC(TRUE, didSplit = false);

    JITDUMP("Tree Transform Rationalization: BB%02u\n", loc.block->bbNum);
    DISPTREE(loc.tree);
    JITDUMP("\n");

    comp->compCurStmt = loc.tree;
    comp->compCurBB = loc.block;

    // top level comma is a special case
    if (loc.tree->gtStmt.gtStmtExpr->OperGet() == GT_COMMA)
    {
        Location loc1, loc2;
        RewriteTopLevelComma(loc, &loc1, &loc2);
        
        loc = loc1;
        goto top;
    }

    DBEXEC(TRUE, loc.Validate());

#ifdef LEGACY_BACKEND
    if (comp->compQmarkUsed)
    {
        loc = RewriteQuestions(loc);
    }
#endif // LEGACY_BACKEND

    DBEXEC(TRUE, ValidateStatement(loc));

    loc = RewriteSimpleTransforms(loc);
    DBEXEC(TRUE, ValidateStatement(loc));

    JITDUMP("comma processing top level statment:\n");
    DISPTREE(loc.tree);
    JITDUMP("\n");

    DuplicateCommaProcessOneTree(comp, this, loc.block, loc.tree);
    
    DBEXEC(didSplit, comp->fgDebugCheckBBlist());
            
    return loc;
}


//------------------------------------------------------------------------------
// MorphAsgIntoStoreLcl -
//   Receives an assignment of type GT_ASG(Lhs, Rhs) where:
//   -- Lhs can be GT_LCL_VAR or GT_LCL_FLD
//   -- Rhs is an arbitrary tree and converts that into its corresponding 
//   store local form.
//
//   Returns the tree converted into GT_STORE_LCL_VAR or GT_STORE_LCL_FLD form.
//
//   If stmt is null, this is a newly created tree that is not yet contained in
//   a stmt.
//------------------------------------------------------------------------------
void Rationalizer::MorphAsgIntoStoreLcl(GenTreeStmt* stmt, GenTreePtr pTree)
{
    assert(pTree->OperGet() == GT_ASG);

    GenTreePtr lhs = pTree->gtGetOp1();
    GenTreePtr rhs = pTree->gtGetOp2();

    assert(lhs->OperGet() == GT_LCL_VAR ||
           lhs->OperGet() == GT_LCL_FLD);

#ifdef DEBUG
    if (lhs->OperGet() == GT_LCL_VAR)
    {
        JITDUMP("rewriting GT_ASG(GT_LCL_VAR, X) to GT_STORE_LCL_VAR(X)\n");
    }
    else
    {
        assert(lhs->OperGet() == GT_LCL_FLD);
        JITDUMP("rewriting GT_ASG(GT_LCL_FLD, X) to GT_STORE_LCL_FLD(X)\n");
    }
#endif // DEBUG

    GenTreeLclVarCommon* var = lhs->AsLclVarCommon();
    pTree->SetOper(storeForm(var->OperGet()));
    GenTreeLclVarCommon* dst = pTree->AsLclVarCommon();
    dst->SetLclNum(var->gtLclNum);
    dst->SetSsaNum(var->gtSsaNum);
    dst->gtType = lhs->gtType;

    if (lhs->OperGet() == GT_LCL_FLD)
    {
        dst->gtLclFld.gtLclOffs  = lhs->gtLclFld.gtLclOffs;
        dst->gtLclFld.gtFieldSeq = lhs->gtLclFld.gtFieldSeq;
    }

    copyFlags(dst, var, GTF_LIVENESS_MASK);
    dst->gtOp.gtOp1 = rhs;

    if (stmt != nullptr)
    {
        assert(stmt->OperGet() == GT_STMT);
        Compiler::fgDeleteTreeFromList(stmt, lhs);
    }

    DISPNODE(pTree);
    JITDUMP("\n");
}

//------------------------------------------------------------------------------
// CreateTempAssignment -
// Constructs an assignment where its left hand side is a GenTree node 
// representing the given local variable number and the right hand side is
// the given tree.
//
// This calls gtNewTempAssig(), which produces a GT_STORE_LCL_VAR instead of a
// GT_ASG when we are in linear order, which we are in the Rationalizer.
//
//------------------------------------------------------------------------------
GenTreePtr Rationalizer::CreateTempAssignment(Compiler* comp, unsigned lclNum, GenTreePtr rhs)
{
    GenTreePtr gtAsg = comp->gtNewTempAssign(lclNum, rhs);
    return gtAsg;
}


// turn "comma(lcl x, lcl x)" into "lcl x"
// this is produced by earlier transformations

void Rationalizer::DuplicateCommaProcessOneTree(Compiler *comp, Rationalizer *irt, BasicBlock *block, GenTree *statement)
{
    SplitData tmpState = {0};
    tmpState.root = statement;
    tmpState.continueSubtrees = true;
    tmpState.thisPhase = irt;
    tmpState.block = block;
    
    assert(statement->IsStatement());

    comp->fgWalkTree(&(statement->gtStmt.gtStmtExpr),
                    NULL,
                    CommaHelper,
                    &tmpState);

#if 0
    JITDUMP("resulting block\n");
    DBEXEC(VERBOSE, comp->fgDispBasicBlocks(block, block, true));
#endif
}

// call args have other pointers to them which must be fixed up if 
// they are replaced
void Compiler::fgFixupIfCallArg(ArrayStack<GenTree *> *parentStack,
                                  GenTree *oldChild, 
                                  GenTree *newChild)
{
    GenTree *parentCall = isNodeCallArg(parentStack);
    if (!parentCall) 
    {
        DBEXEC(VERBOSE, dumpTreeStack(GetTlsCompiler(), parentStack));
        return;
    }
     
    // we have replaced an arg, so update pointers in argtable
    fgFixupArgTabEntryPtr(parentCall, oldChild, newChild);
}

//------------------------------------------------------------------------
// fgFixupArgTabEntryPtr: Fixup the fgArgTabEntryPtr of parentCall after 
//                        replacing oldArg with newArg 
//
// Arguments:
//    parentCall - a pointer to the parent call node
//    oldArg     - the original argument node
//    newArg     - the replacement argument node
//

void Compiler::fgFixupArgTabEntryPtr(GenTreePtr parentCall,
                                     GenTreePtr oldArg,
                                     GenTreePtr newArg)
{
    assert(parentCall != nullptr);
    assert(oldArg != nullptr);
    assert(newArg != nullptr);

    JITDUMP("parent call was :\n");
    DISPTREE(parentCall);

    JITDUMP("old child was :\n");
    DISPTREE(oldArg);
    
    if (oldArg->gtFlags & GTF_LATE_ARG)
    {
        newArg->gtFlags |= GTF_LATE_ARG;
    }
    else
    {
        fgArgTabEntryPtr fp = Compiler::gtArgEntryByNode(parentCall, oldArg);
        assert(fp->node == oldArg);
        fp->node = newArg;
    }

    JITDUMP("parent call:\n");
    DISPTREE(parentCall);
}

//------------------------------------------------------------------------
// CommaUselessChild: removes commas with useless first child:
//                    - Turns "comma(lcl x, Y)" into "Y"
//                    - Turns "comma(NOP, Y)" into "Y"
//
// Arguments:
//    ppTree    - a pointer to the parent pointer for a comma node
//    data      - the traversal data
//
// Return Value:
//    Returns "true" if it found a comma with a useless child, and transformed it.
//
// Notes:
//    These comma forms are produced by earlier transformations.

bool
Rationalizer::CommaUselessChild(GenTree **ppTree, Compiler::fgWalkData *data)
{
    GenTree *tree = *ppTree;
    GenTree *subChild1, *subChild2;
    SplitData *tmpState = (SplitData *) data->pCallbackData;

    assert(tree->OperGet() == GT_COMMA);

    subChild1 = tree->gtGetOp1();
    subChild2 = tree->gtGetOp2();

    if (subChild1->OperGet() == GT_COMMA)
    {
        data->parentStack->Push(tree->gtOp.gtOp1);
        CommaUselessChild(&(tree->gtOp.gtOp1), data);
        subChild1 = tree->gtGetOp1();
        data->parentStack->Pop();
    }

    if (subChild2->OperGet() == GT_COMMA)
    {
        data->parentStack->Push(tree->gtOp.gtOp2);
        CommaUselessChild(&(tree->gtOp.gtOp2), data);
        subChild2 = tree->gtGetOp2();
        data->parentStack->Pop();
    }

    if (subChild1 != nullptr &&
        subChild2 != nullptr &&
        (subChild1->OperIsLocalRead() ||
         (subChild1->OperGet() == GT_NOP && subChild1->gtGetOp1() == nullptr)))
    {
        JITDUMP("found comma subtree with useless child:\n");
        DISPTREE(tree);
        JITDUMP("\n");

#ifdef DEBUG
        if (isNodeCallArg(data->parentStack))
        {
            JITDUMP("PARENT TREE:\n");
            DISPTREE(isNodeCallArg(data->parentStack));
            JITDUMP("\n");
        }
#endif // DEBUG

        Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
        Compiler::fgSnipNode(tmpState->root->AsStmt(), subChild1);
        *ppTree = subChild2;

        if (tree->gtFlags & GTF_LATE_ARG)
        {
            subChild2->gtFlags |= GTF_LATE_ARG;
            // If we just have a bare local as a late ("SETUP") arg then that is effectively a NOP
            // however if that local node is a last use, codegen will not count it as such, and blow up
            // so get rid of those here
            if (subChild2->IsLocal())
                subChild2->gtBashToNOP();
        }

        tmpState->thisPhase->comp->fgFixupIfCallArg(data->parentStack, tree, subChild2);
        return true;
    }
    return false;
}

// Call CommaUselessChild() to turn "comma(lcl x, lcl x)" into "lcl x"

Compiler::fgWalkResult Rationalizer::CommaHelper(GenTree **ppTree, Compiler::fgWalkData *data)
{
    GenTree *tree = *ppTree;
    Compiler* comp = data->compiler;

    SplitData *tmpState = (SplitData *) data->pCallbackData;


    if (tree->OperGet() == GT_COMMA &&
        CommaUselessChild(ppTree, data))
    {
        return Compiler::WALK_SKIP_SUBTREES;
    }

    return Compiler::WALK_CONTINUE;
}

// rewrite ASG nodes as either local store or indir store forms
// also remove ADDR nodes
Location Rationalizer::RewriteSimpleTransforms(Location loc)
{
    GenTreeStmt * statement = (loc.tree)->AsStmt();
    GenTree *     tree      = statement->gtStmt.gtStmtExpr;

    JITDUMP("RewriteSimpleTransforms, with statement:\n");
    DISPTREE(statement);
    JITDUMP("\n");

    if (statement->gtStmtIsTopLevel())
    {
        if (tree->OperGet() == GT_COMMA)
        {
            Location loc1, loc2;
            RewriteTopLevelComma(loc, &loc1, &loc2);
            RewriteSimpleTransforms(loc1);
            RewriteSimpleTransforms(loc2);
            return loc1;
        }
        else if (tree->OperKind() & GTK_CONST)
        {
            // Don't bother generating a top level statement that is just a constant.
            // We can get these if we decide to hoist a large constant value out of a loop.
            tree->gtBashToNOP();
        }
    }

    SplitData tmpState = {0};
    tmpState.root = statement;
    tmpState.continueSubtrees = true;
    tmpState.thisPhase = this;
    tmpState.block = loc.block;

    comp->fgWalkTree(&(statement->gtStmt.gtStmtExpr),
        SimpleTransformHelper,
        NULL,
        &tmpState);

    tree = statement->gtStmt.gtStmtExpr;
    if (tree->OperIsLocalRead())
    {
        comp->lvaTable[tree->AsLclVarCommon()->gtLclNum].decRefCnts(comp->compCurBB->getBBWeight(comp), comp);
        tree->gtBashToNOP();
    }

    JITDUMP("After simple transforms:\n");
    DISPTREE(statement);
    JITDUMP("\n");

    return loc;
}

// RecursiveRewriteComma
//
// This routine deals with subtrees composed entirely of commas, and the expressions that hang off of them.
// The degenerate case is a single comma but (?????)
//
// ppTree : pointer to a link to a comma node
// discard: true if any value produced by the node will ultimately be discarded.  
//          In a tree of commas with some non-comma expressions hanging off the terminal commas, 
//          ultimately all results of those expressions will be discarded except for 
//          the expression reached by following the second link of of all commas on a path from the base
//          ex: in "comma(comma(exp1, exp2), comma(exp3, comma(exp4, exp5)))"
//          the only expression whose value makes it to the root of the comma tree is exp5
// nested: true if there is another comma as the parent
// 
void Rationalizer::RecursiveRewriteComma(GenTree **ppTree, Compiler::fgWalkData *data, bool discard, bool nested)
{
    GenTree* comma = *ppTree;
    assert(comma->gtOper == GT_COMMA);
    GenTreePtr op2 = comma->gtOp.gtOp2;
    GenTreePtr op1 = comma->gtOp.gtOp1;
    SplitData *tmpState = (SplitData *) data->pCallbackData;
    GenTreePtr stmt     =  tmpState->root;
    Compiler*  comp     = data->compiler;

    JITDUMP("recursive rewrite comma :\n");
    DISPTREE(comma);
    JITDUMP("\n");
    
    if (op1->gtOper == GT_COMMA)
    {
        // embed all of the expressions reachable from op1.  
        // Since they feed into op1, their results are discarded (not used up the tree)
        RecursiveRewriteComma(&(comma->gtOp.gtOp1), data, true, true);
    }

    // Although most top-level commas have already been handled, we may create new ones
    // (for example by splitting a comma above another comma).
    Compiler::fgSnipNode(stmt->AsStmt(), comma);
    *ppTree = op2;
    JITDUMP("pptree now : ");
    DISPNODE(op2);
    if (data->parentStack->Top() == comma)
    {
        data->parentStack->Pop();
        data->parentStack->Push(op2);
    }

    GenTree* commaNext = comma->gtNext;

    op1 = comma->gtOp.gtOp1;
    
    // op1 of the comma will now be a new statement, either top-level or embedded
    // depending on the execution order.
    // The comma is simply eliminated.
    GenTreePtr newStmt = comp->fgMakeEmbeddedStmt(tmpState->block, op1, tmpState->root);

    if (!nested)
        comp->fgFixupIfCallArg(data->parentStack, comma, *ppTree);
    
    JITDUMP("Split comma into %s statements. New statement:\n", (newStmt->gtFlags & GTF_STMT_TOP_LEVEL) ? "top-level" : "embedded");
    DISPTREE(newStmt);
    JITDUMP("\nOld statement:\n");
    DISPTREE(stmt);
    JITDUMP("\n");

    (void) ((Rationalizer *)tmpState->thisPhase)->RewriteSimpleTransforms(Location(newStmt, tmpState->block));

    // In a sense, assignment nodes have two destinations: 1) whatever they are writing to
    // and 2) they also produce the value that was written so their parent can consume it.
    // In the case where the parent is going to consume the value, 
    // insert the assign as an embedded statement and clone the destination to replace itself in the tree.
    
    if (op2->OperGet() == GT_ASG && !discard)
    {
        JITDUMP("op2 of comma was an assignment, doing additional work\n");
        assert(op2->gtNext);
        GenTree* dst = op2->gtOp.gtOp1;
        GenTree* newSrc = nullptr;
        GenTreeStmt* newStmt;

        newStmt = comp->fgMakeEmbeddedStmt(tmpState->block, op2, tmpState->root);

        // can this happen ? 
        assert(dst->OperIsLocal());
        
        newSrc = comp->gtClone(dst);
        newSrc->gtFlags &= ~GTF_VAR_DEF;

        *ppTree = newSrc;
        comp->fgInsertTreeInListBefore(newSrc, commaNext, stmt->AsStmt());


        JITDUMP("Split comma into %s statements. New statement:\n", (newStmt->gtFlags & GTF_STMT_TOP_LEVEL) ? "top-level":"embedded");
        DISPTREE(newStmt);
        JITDUMP("\nOld statement:\n");
        DISPTREE(stmt);
        JITDUMP("\n");

        (void) ((Rationalizer *)tmpState->thisPhase)->RewriteSimpleTransforms(Location(newStmt, tmpState->block));

        if (!nested)
            comp->fgFixupIfCallArg(data->parentStack, comma, newSrc);

        (void) ((Rationalizer *)tmpState->thisPhase)->RewriteSimpleTransforms(Location(newStmt, tmpState->block));

        return;
    }
    JITDUMP("\nreturning from RecursiveRewriteComma\n");
}

//------------------------------------------------------------------------
// RewriteOneComma: Rewrites the trees to remove a comma
//
// Arguments:
//    ppTree    - a pointer to the parent pointer for a comma node
//    data      - the traversal data
//
// Return Value:
//    None.
//
// Assumptions:
//    This method is always called during a traversal (hence the fgWalkData).
//    'ppTree' must point to a GT_COMMA GenTreePtr
//
// Notes:
//    If op1 of the comma is a (unused) lclVar, it is deleted by CommmaUselessChild()

void Rationalizer::RewriteOneComma(GenTree **ppTree, Compiler::fgWalkData *data)
{
    GenTreePtr comma    = *ppTree;
    Compiler*  comp     = data->compiler;
    SplitData* tmpState = (SplitData *) data->pCallbackData;
    GenTreePtr stmt     =  tmpState->root;

    assert(comma->gtOper == GT_COMMA);
    GenTreePtr op2 = comma->gtOp.gtOp2;
    GenTreePtr op1 = comma->gtOp.gtOp1;

    // Remove the comma from the tree; we know it has non-null gtPrev, otherwise
    // we would have handled it as a top-level comma.
    assert (comma->gtPrev != nullptr);
    JITDUMP("Rationalizing comma:");
    DISPNODE(comma);

    if (!CommaUselessChild(ppTree, data))
    {
        // Set 'discard' to true when the comma tree does not return a value 
        // If the comma's type is TYP_VOID then 'discard' is set to true
        // otherwise 'discard' is set to false 
        bool discard = (comma->TypeGet() == TYP_VOID);
        RecursiveRewriteComma(ppTree, data, discard, false);
    }
}

// Rewrite InitBlk involving SIMD vector into stlcl.var of a SIMD type.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_INITBLK
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteInitBlk(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{   
#ifdef FEATURE_SIMD
    Compiler* comp = data->compiler;

    // No lowering is needed for non-SIMD nodes, so early out if featureSIMD is not enabled.
    if (!comp->featureSIMD) 
    {
        return;
    }

    // See if this is a SIMD initBlk that needs to be changed to a simple st.lclVar.
    GenTreeInitBlk* tree = (*ppTree)->AsInitBlk();

    // Is the dstAddr is addr of a SIMD type lclVar?
    GenTreePtr dstAddr = tree->Dest();
    if (dstAddr->OperGet() != GT_ADDR)
    {
        return;
    }

    GenTree* dst = dstAddr->gtGetOp1();        
    var_types baseType = comp->getBaseTypeOfSIMDLocal(dst);
    if (baseType == TYP_UNKNOWN)
    {
        return;
    }
    CORINFO_CLASS_HANDLE typeHnd = comp->lvaTable[dst->AsLclVarCommon()->gtLclNum].lvVerTypeInfo.GetClassHandle();
    unsigned simdLocalSize = comp->getSIMDTypeSizeInBytes(typeHnd);

    JITDUMP("Rewriting SIMD InitBlk\n");
    DISPTREE(tree);

    // Get rid of the parent node in GT_ADDR(GT_LCL_VAR)    
    comp->fgSnipInnerNode(dstAddr);

    assert((dst->gtFlags &GTF_VAR_USEASG) == 0);

    // Remove 'size' from execution order
    // There are currently only three sizes supported: 8 bytes, 16 bytes or the vector register length.
    GenTreeIntConCommon* sizeNode = tree->Size()->AsIntConCommon();
    unsigned int size = (unsigned int) roundUp(sizeNode->IconValue(), TARGET_POINTER_SIZE);
    var_types simdType = comp->getSIMDTypeForSize(size);
    assert(roundUp(simdLocalSize, TARGET_POINTER_SIZE) == size);
    comp->fgSnipInnerNode(sizeNode);

    GenTree* initVal = tree->InitVal();
    GenTreeSIMD* simdTree = new (comp, GT_SIMD) GenTreeSIMD(simdType, initVal, SIMDIntrinsicInit, baseType, (unsigned)sizeNode->IconValue());
    dst->SetOper(GT_STORE_LCL_VAR);
    dst->gtType = simdType;
    dst->gtOp.gtOp1 = simdTree;
    dst->gtFlags |= (simdTree->gtFlags & GTF_ALL_EFFECT);

    initVal->gtNext = simdTree;
    simdTree->gtPrev = initVal;

    simdTree->gtNext = dst;
    dst->gtPrev = simdTree;

    GenTree* nextNode = tree->gtNext;
    dst->gtNext = nextNode;
    if (nextNode != nullptr)
    {
        nextNode->gtPrev = dst;
    }

    *ppTree = dst;

    JITDUMP("After rewriting SIMD InitBlk:\n");
    DISPTREE(*ppTree);
    JITDUMP("\n");
#endif // FEATURE_SIMD
}

// Transform CopyBlk involving SIMD vectors into stlclvar or stind of a SIMD type.
// Transformation is done if either src or dst are known to be SIMD vectors.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_COPYBLK
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// If either the source or the dst are known to be SIMD (a lclVar or SIMD intrinsic),
// get the simdType (TYP_DOUBLE or a SIMD type for SSE2) from the size of the SIMD node.
// 
// For the source:
// - If it is a SIMD intrinsic or a lvSIMDType lclVar, change the node type to simdType.
// - Otherwise, add a GT_IND of simdType.
// For the dst:
// - If it is a lclVar of a SIMD type, chanage the node type to simdType.
// - Otherwise, change it to a GT_STORE_IND of simdType
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteCopyBlk(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{   
#ifdef FEATURE_SIMD
    Compiler* comp = data->compiler;

    // No need to transofrm non-SIMD nodes, if featureSIMD is not enabled.
    if (!comp->featureSIMD) 
    {
        return;
    }

    // See if this is a SIMD copyBlk
    GenTreeCpBlk* tree = (*ppTree)->AsCpBlk();      
    genTreeOps oper = GT_NONE;
    GenTreePtr dstAddr = tree->Dest();    
    GenTree* srcAddr = tree->Source(); 
        
    // Do not transform if neither src or dst is known to be a SIMD type.
    // If src tree type is something we cannot reason but if dst is known to be of a SIMD type
    // we will treat src tree as a SIMD type and vice versa.
    if (!(comp->isAddrOfSIMDType(srcAddr) || comp->isAddrOfSIMDType(dstAddr)))
    {
        return;
    }       

    // At this point it is known to be a copyblk of SIMD vectors and we can 
    // start transforming the original tree. Prior to this point do not perform
    // any modifications to the original tree.
    JITDUMP("\nRewriting SIMD CopyBlk\n");
    DISPTREE(tree);

    // Remove 'size' from execution order
    // There are currently only three sizes supported: 8 bytes, 12 bytes, 16 bytes or the vector register length.
    GenTreeIntConCommon* sizeNode = tree->Size()->AsIntConCommon();
    var_types simdType = comp->getSIMDTypeForSize((unsigned int) sizeNode->IconValue());
    comp->fgSnipInnerNode(sizeNode);

    // Is destination a lclVar which is not an arg?
    // If yes then we can turn it to a stlcl.var, otherwise turn into stind.
    GenTree* simdDst = nullptr;
    if (dstAddr->OperGet() == GT_ADDR && comp->isSIMDTypeLocal(dstAddr->gtGetOp1()))
    {
        // Get rid of parent node in GT_ADDR(GT_LCL_VAR)
        comp->fgSnipInnerNode(dstAddr);
        simdDst = dstAddr->gtGetOp1();
        simdDst->gtType = simdType;
        oper = GT_STORE_LCL_VAR;        

        // For structs that are padded (e.g. Vector3f, Vector3i), the morpher will have marked them
        // as GTF_VAR_USEASG.  Unmark them.
        simdDst->gtFlags &= ~(GTF_VAR_USEASG);
    }
    else
    {
        // Address of a non-local var
        simdDst = dstAddr;
        oper = GT_STOREIND;        
    }

    // Src: Get rid of parent node of GT_ADDR(..) if its child happens to be of a SIMD type.
    GenTree* simdSrc = nullptr;
    if (srcAddr->OperGet() == GT_ADDR && varTypeIsSIMD(srcAddr->gtGetOp1()))
    {
        comp->fgSnipInnerNode(srcAddr);
        simdSrc = srcAddr->gtGetOp1();
    }
    else
    {
        // Since destination is known to be a SIMD type, src must be a SIMD type too
        // though we cannot figure it out easily enough. Transform src into
        // GT_IND(src) of simdType.
        //
        // We need to initialize costs on indir so that CopyCosts() while creating
        // an addrmode will not hit asserts. These costs are not used further down
        // but setting them to a reasonable value based on the logic in gtSetEvalOrder().
        GenTree* indir = comp->gtNewOperNode(GT_IND, simdType, srcAddr);
        indir->SetCosts(IND_COST_EX, 2);
        srcAddr->InsertAfterSelf(indir);
        
        tree->gtGetOp1()->gtOp.gtOp2 = indir;
        simdSrc = indir;
    }
    simdSrc->gtType = simdType;

    // Change cpblk to either a st.lclvar or st.ind.
    // At this point we are manipulating cpblk node with the knowledge of
    // its internals (i.e. op1 is the size node, and the src & dst are in a GT_LIST on op2).
    // This logic might need to be changed if we ever restructure cpblk node.

    assert(simdDst != nullptr);
    assert(simdSrc != nullptr);

    GenTree *newTree = nullptr;
    GenTree* list = tree->gtGetOp1();
    if (oper == GT_STORE_LCL_VAR)
    {
        // get rid of the list node
        comp->fgSnipInnerNode(list);

        newTree = simdDst;
        newTree->SetOper(oper);
        newTree->gtOp.gtOp1 = simdSrc;         
        newTree->gtType = simdType;
        newTree->gtFlags |= (simdSrc->gtFlags & GTF_ALL_EFFECT);
        simdSrc->gtNext = newTree;
        newTree->gtPrev = simdSrc;            
    }
    else
    {
        assert(oper == GT_STOREIND);

        newTree = list;
        newTree->SetOper(oper);
        newTree->gtType = simdType;
        newTree->gtFlags |= (simdSrc->gtFlags & GTF_ALL_EFFECT);
        newTree->gtOp.gtOp1 = simdDst;
        newTree->gtOp.gtOp2 = simdSrc;
    } 

    assert(newTree != nullptr);
    GenTree* nextNode = tree->gtNext;
    newTree->gtNext = nextNode;
    if (nextNode != nullptr)
    {
        nextNode->gtPrev = newTree;
    }

    *ppTree = newTree;

    JITDUMP("After rewriting SIMD CopyBlk:\n");
    DISPTREE(*ppTree);
    JITDUMP("\n");
#endif // FEATURE_SIMD
}

// Rewrite GT_LDOBJ of SIMD Vector as GT_IND(GT_LEA(ldobj.op1)) of a SIMD type.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_LDOBJ
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteLdObj(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{    
#ifdef FEATURE_SIMD
    Compiler* comp = data->compiler;
    GenTreeLdObj* ldObj = (*ppTree)->AsLdObj();

    // For UNIX struct passing, we can have LdObj nodes for arguments.
    // For other cases, we should never see a non-SIMD type here.
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    if (!varTypeIsSIMD(ldObj))
    {
        return;
    }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
    // Should come here only if featureSIMD is enabled
    noway_assert(comp->featureSIMD);
    // On  we should only call this with a SIMD type.
    noway_assert(varTypeIsSIMD(ldObj));
    var_types simdType = ldObj->TypeGet();

    // If the operand of ldobj is a GT_ADDR(GT_LCL_VAR) and LclVar is known to be a SIMD type,
    // replace ldobj by GT_LCL_VAR.
    GenTree* srcAddr = ldObj->gtGetOp1();
    if (srcAddr->OperGet() == GT_ADDR && comp->isSIMDTypeLocal(srcAddr->gtGetOp1()))
    {
        GenTree* src = srcAddr->gtGetOp1();
        comp->fgSnipInnerNode(srcAddr);
        // It is possible for the ldobj to be the last node in the tree, if its result is
        // not actually stored anywhere and is not eliminated.
        // This can happen with an unused SIMD expression involving a localVar or temporary value,
        // where the SIMD expression is returning a non-SIMD value, and the expression is sufficiently
        // complex (e.g. a call to vector * scalar which is inlined but not an intrinsic).
        // The ldobj of the localVar is not eliminated, because it involves an indirection,
        // and therefore appears potentially unsafe to eliminate. However, when we transform the ldobj into
        // a plain localVar during the Rationalizer, we need to correctly handle the case where it has
        // no parent.
        // This happens, for example, with this source code:
        //      Vector4.Dot(default(Vector4) * 2f, Vector4.One);
        if (ldObj->gtNext == nullptr)
        {
            SplitData *tmpState = (SplitData *) data->pCallbackData;
            comp->fgSnipNode(tmpState->root->AsStmt(), ldObj);
        }
        else
        {
            comp->fgSnipInnerNode(ldObj);
        }
        comp->fgFixupIfCallArg(data->parentStack, ldObj, src);
        src->gtType = simdType;

        *ppTree = src;
    }
    else 
    {
        ldObj->SetOper(GT_IND);
        ldObj->gtType = simdType;
    }
#else
    // we should never reach without feature SIMD
    assert(!"Unexpected Ldobj during rationalization\n");
    unreached();
#endif
}

// RewriteNodeAsCall : Replace the given tree node by a GT_CALL.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the tree node
//    fgWalkData  - A pointer to tree walk data providing the context
//    callHnd     - The method handle of the call to be generated
//    entryPoint  - The method entrypoint of the call to be generated
//    args        - The argument list of the call to be generated
//
// Return Value:
//    None.
//

void Rationalizer::RewriteNodeAsCall(GenTreePtr* ppTree, Compiler::fgWalkData* data,
    CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN_COMPILER
    CORINFO_CONST_LOOKUP entryPoint,
#endif
    GenTreeArgList* args)
{
    GenTreePtr tree = *ppTree;
    Compiler*  comp = data->compiler;
    SplitData* tmpState = (SplitData *)data->pCallbackData;
    GenTreePtr root = tmpState->root;
    GenTreePtr treeFirstNode = comp->fgGetFirstNode(tree);
    GenTreePtr treeLastNode = tree;
    GenTreePtr treePrevNode = treeFirstNode->gtPrev;
    GenTreePtr treeNextNode = treeLastNode->gtNext;

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->gtType, args);
    call = comp->fgMorphArgs(call);
    call->CopyCosts(tree);
#ifdef FEATURE_READYTORUN_COMPILER
    call->gtCall.gtEntryPoint = entryPoint;
#endif

    // Replace "tree" with "call"
    *ppTree = call;
        
    // Rebuild the evaluation order.
    comp->gtSetStmtInfo(root);

    // Rebuild the execution order.
    comp->fgSetTreeSeq(call, treePrevNode);

    // Restore linear-order Prev and Next for "call".
    if (treePrevNode)
    {
        treeFirstNode = comp->fgGetFirstNode(call);
        treeFirstNode->gtPrev = treePrevNode;
        treePrevNode->gtNext = treeFirstNode;
    }
    else
    {
        // Update the linear oder start of "root" if treeFirstNode 
        // appears to have replaced the original first node.
        assert(treeFirstNode == root->gtStmt.gtStmtList);
        root->gtStmt.gtStmtList = comp->fgGetFirstNode(call);
    }

    if (treeNextNode)
    {
        treeLastNode = call;
        treeLastNode->gtNext = treeNextNode;
        treeNextNode->gtPrev = treeLastNode;
    }
    
    comp->fgFixupIfCallArg(data->parentStack, tree, call);

    // Propagate flags of "call" to its parents.
    // 0 is current node, so start at 1
    for (int i = 1; i < data->parentStack->Height(); i++)
    {
        GenTree *node = data->parentStack->Index(i);
        node->gtFlags |= GTF_CALL;
        node->gtFlags |= call->gtFlags & GTF_ALL_EFFECT;
    }

    // Since "tree" is replaced with "call", pop "tree" node (i.e the current node)
    // and replace it with "call" on parent stack.
    assert(data->parentStack->Top() == tree);
    (void)data->parentStack->Pop();
    data->parentStack->Push(call);

    DBEXEC(TRUE, ValidateStatement(root, tmpState->block));
}

// RewriteIntrinsicAsUserCall : Rewrite an intrinsic operator as a GT_CALL to the original method.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the intrinsic node
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// Some intrinsics, such as operation Sqrt, are rewritten back to calls, and some are not. 
// The ones that are not being rewritten here must be handled in Codegen.
// Conceptually, the lower is the right place to do the rewrite. Keeping it in rationalization is
// mainly for throughput issue.

void Rationalizer::RewriteIntrinsicAsUserCall(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{    
    GenTreePtr tree = *ppTree;
    Compiler*  comp = data->compiler;
    GenTreeArgList* args;

    assert(tree->OperGet() == GT_INTRINSIC);

    if (tree->gtOp.gtOp2 == nullptr)
    {
        args = comp->gtNewArgList(tree->gtOp.gtOp1);
    }
    else
    {
        args = comp->gtNewArgList(tree->gtOp.gtOp1, tree->gtOp.gtOp2);
    }

    RewriteNodeAsCall(ppTree, data,
        tree->gtIntrinsic.gtMethodHandle,
#ifdef FEATURE_READYTORUN_COMPILER
        tree->gtIntrinsic.gtEntryPoint,
#endif
        args);
}

// tree walker callback function that rewrites ASG and ADDR nodes
Compiler::fgWalkResult Rationalizer::SimpleTransformHelper(GenTree **ppTree, Compiler::fgWalkData *data)
{
    GenTree *tree = *ppTree;
    Compiler* comp = data->compiler;
    SplitData *tmpState = (SplitData *) data->pCallbackData;

    while (tree->OperGet() == GT_COMMA)
    {
        RewriteOneComma(ppTree, data);
        tree = *ppTree;
    }
            
    if (tree->OperIsAssignment())
    {
        GenTree *lhs = tree->gtGetOp1();
        GenTree *dataSrc = tree->gtGetOp2();

        // the other assign ops should have already been rewritten to ASG
        assert(tree->OperGet() == GT_ASG);

        while (lhs->OperGet() == GT_COMMA)
        {
            RewriteOneComma(&(tree->gtOp.gtOp1), data);
            lhs = tree->gtGetOp1();
        }
        switch (lhs->OperGet())
        {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_REG_VAR:
        case GT_PHI_ARG:
            MorphAsgIntoStoreLcl(tmpState->root->AsStmt(), tree);
            tree->gtFlags &= ~GTF_REVERSE_OPS;
            break;

        case GT_IND:
            {
                GenTreeStoreInd *store = new(comp, GT_STOREIND) GenTreeStoreInd(lhs->TypeGet(), lhs->gtGetOp1(), dataSrc);
                if (tree->IsReverseOp()) store->gtFlags |= GTF_REVERSE_OPS;
                store->gtFlags |= (lhs->gtFlags & GTF_IND_FLAGS);
                store->CopyCosts(tree);

                JITDUMP("Rewriting GT_ASG(GT_IND, X) to GT_STOREIND(X):\n");
                DISPTREE(store);
                JITDUMP("\n");

                // Snip out the old GT_IND node
                GenTreePtr indPrev = lhs->gtPrev;
                indPrev->gtNext = lhs->gtNext;
                indPrev->gtNext->gtPrev = indPrev;

                // Replace "tree" with "store"
                *ppTree = store;
                store->gtNext = tree->gtNext;
                store->gtPrev = tree->gtPrev;
                if (store->gtNext != nullptr)
                    store->gtNext->gtPrev = store;
                assert (store->gtPrev != nullptr);
                store->gtPrev->gtNext = store;

                // Since "tree" is replaced with "store", pop "tree" node (i.e the current node)
                // and replace it with "store" on parent stack.
                assert(data->parentStack->Top() == tree);
                (void)data->parentStack->Pop();
                data->parentStack->Push(store);

                JITDUMP("root:\n");
                DISPTREE(tmpState->root);
                JITDUMP("\n");
            }
            break;

        case GT_CLS_VAR:
            {
                lhs->gtOper = GT_CLS_VAR_ADDR;
                lhs->gtType = TYP_BYREF;
                tree->gtOper = GT_STOREIND;

                JITDUMP("Rewriting GT_ASG(GT_CLS_VAR, X) to GT_STOREIND(GT_CLS_VAR_ADDR, X):\n");
                DISPTREE(tree);
                JITDUMP("\n");
            }
            break;

        default:
            assert(!"unhandled op\n");
            break;
        }
    }
    else if (tree->OperGet() == GT_BOX)
    {
        // GT_BOX at this level just passes through so get rid of it
        Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
        *ppTree = tree->gtOp.gtOp1;
        comp->fgFixupIfCallArg(data->parentStack, tree, *ppTree);
        JITDUMP("Rewriting GT_BOX(X) to X:\n");
        DISPTREE(*ppTree);
        JITDUMP("\n");
        return SimpleTransformHelper(ppTree, data);
    }
    else if (tree->gtOper == GT_ADDR)
    {
        GenTree *child = tree->gtOp.gtOp1;
        if (child->IsLocal())
        {
            // We are changing the child from GT_LCL_VAR TO GT_LCL_VAR_ADDR.
            // Therefore gtType of the child needs to be changed to a TYP_BYREF

#ifdef DEBUG
            if (child->gtOper == GT_LCL_VAR)
            {
                JITDUMP("Rewriting GT_ADDR(GT_LCL_VAR) to GT_LCL_VAR_ADDR:\n");
            }
            else
            {
                assert(child->gtOper == GT_LCL_FLD);
                JITDUMP("Rewriting GT_ADDR(GT_LCL_FLD) to GT_LCL_FLD_ADDR:\n");
            }
#endif // DEBUG

            Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
            child->gtOper = addrForm(child->gtOper);
            child->gtType = TYP_BYREF;
            copyFlags(child, tree, GTF_ALL_EFFECT);
            *ppTree = child;
        }
        else if (child->gtOper == GT_CLS_VAR)
        {
            Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
            child->gtOper = GT_CLS_VAR_ADDR;
            child->gtType = TYP_BYREF;
            copyFlags(child, tree, GTF_ALL_EFFECT);
            *ppTree = child;

            JITDUMP("Rewriting GT_ADDR(GT_CLS_VAR) to GT_CLS_VAR_ADDR:\n");
        }
        else if (child->gtOper == GT_IND)
        {
            Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
            Compiler::fgSnipNode(tmpState->root->AsStmt(), child);
            *ppTree = child->gtOp.gtOp1;
            JITDUMP("Rewriting GT_ADDR(GT_IND(X)) to X:\n");
        }
        comp->fgFixupIfCallArg(data->parentStack, tree, *ppTree);
        DISPTREE(*ppTree);
        JITDUMP("\n");
    }
    else if (tree->gtOper == GT_NOP
             && tree->gtOp.gtOp1)
    {
        // fgmorph sometimes inserts NOP nodes between def and use
        // supposedly 'to prevent constant folding' 
        Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);
        *ppTree = tree->gtOp.gtOp1;
        comp->fgFixupIfCallArg(data->parentStack, tree, *ppTree);

        // Since GT_NOP(op1) is replaced with op1, pop GT_NOP node (i.e the current node)
        // and replace it with op1 on parent stack.
        (void)data->parentStack->Pop();
        data->parentStack->Push(tree->gtOp.gtOp1);

        JITDUMP("Rewriting GT_NOP(X) to X:\n");
        DISPTREE(*ppTree);
        JITDUMP("\n");
        return SimpleTransformHelper(ppTree, data);
    }
    else if (tree->gtOper == GT_QMARK)
    {
        // only certain forms of qmarks are allowed
        // qmark(conditionExpr, 1, 0) is equivalent to conditionExpr
        GenTree* colonNode = tree->gtOp.gtOp2;
        GenTree* thenNode = colonNode->AsColon()->ThenNode();
        GenTree* elseNode = colonNode->AsColon()->ElseNode();
        assert(thenNode->IsCnsIntOrI());
        assert(elseNode->IsCnsIntOrI());
        assert(thenNode->gtIntConCommon.IconValue() == 1);
        assert(elseNode->gtIntConCommon.IconValue() == 0);

        Compiler::fgSnipNode(tmpState->root->AsStmt(), elseNode);
        Compiler::fgSnipNode(tmpState->root->AsStmt(), thenNode);
        Compiler::fgSnipNode(tmpState->root->AsStmt(), colonNode);
        Compiler::fgSnipNode(tmpState->root->AsStmt(), tree);

        *ppTree = tree->gtOp.gtOp1;
        (*ppTree)->gtFlags &= ~GTF_RELOP_QMARK;
        comp->fgFixupIfCallArg(data->parentStack, tree, *ppTree);

        JITDUMP("Rewriting GT_QMARK(conditionExpr, 1, 0) to conditionExpr:\n");
        DISPTREE(*ppTree);
        JITDUMP("\n");
    }
#ifdef _TARGET_XARCH_
    else if (tree->gtOper == GT_CLS_VAR)
    {
        // rewrite "clsvar" as [&clsvar] so indirs are explicit
        tree->gtOper = GT_CLS_VAR_ADDR;
        GenTree *ind = comp->gtNewOperNode(GT_IND, tree->TypeGet(), tree);
        tree->gtType = TYP_BYREF;
        ind->CopyCosts(tree);
        tree->InsertAfterSelf(ind, tmpState->root->AsStmt());
        *ppTree = ind;
        comp->fgFixupIfCallArg(data->parentStack, tree, ind);

        JITDUMP("Rewriting GT_CLS_VAR to GT_IND(GT_CLS_VAR_ADDR(GT_CLS_VAR)):\n");
        DISPTREE(tmpState->root);
        JITDUMP("\n");
    }
#endif // _TARGET_XARCH_
    else if ((tree->gtOper == GT_INTRINSIC) && 
             Compiler::IsIntrinsicImplementedByUserCall(tree->gtIntrinsic.gtIntrinsicId))
    {
        RewriteIntrinsicAsUserCall(ppTree, data);
    }
#ifdef FEATURE_SIMD
    else
    {       
        assert(tree->gtOper != GT_INTRINSIC || Compiler::IsTargetIntrinsic(tree->gtIntrinsic.gtIntrinsicId));

        // Transform the treeNode types for SIMD nodes.
        // If we have a SIMD type, set its size in simdSize, and later we will
        // set the actual type according to its size (which may be less than a full
        // vector register).
        unsigned simdSize = 0;
        switch(tree->gtOper)
        {
        default:
            // Nothing to do for most nodes.
            break;

        case GT_INITBLK:
            RewriteInitBlk(ppTree, data);
            break;

        case GT_COPYBLK:
            RewriteCopyBlk(ppTree, data);
            break;

        case GT_LDOBJ:
            RewriteLdObj(ppTree, data);
            break;

        case GT_LCL_FLD:
        case GT_STORE_LCL_FLD:
            FixupIfSIMDLocal(comp, tree->AsLclVarCommon());
            break;

        case GT_STOREIND:
        case GT_IND:
            if (tree->gtType == TYP_STRUCT)
            {
                GenTree* addr = tree->AsIndir()->Addr();
                assert(addr->OperIsLocal() && addr->TypeGet() == TYP_BYREF);
                LclVarDsc* varDsc = &(comp->lvaTable[addr->AsLclVarCommon()->gtLclNum]);
                assert(varDsc->lvSIMDType);
                simdSize = (unsigned int) roundUp(varDsc->lvExactSize, TARGET_POINTER_SIZE);
                tree->gtType = comp->getSIMDTypeForSize(simdSize);
            }
            break;

        case GT_SIMD:
            {
                noway_assert(comp->featureSIMD);
                GenTreeSIMD* simdTree = (*ppTree)->AsSIMD();
                simdSize = simdTree->gtSIMDSize;
                var_types simdType = comp->getSIMDTypeForSize(simdSize);
                // TODO-Cleanup: This is no-longer required once we plumb SIMD types thru front-end
                if (simdTree->gtType == TYP_I_IMPL && simdTree->gtSIMDSize == TARGET_POINTER_SIZE)
                {
                    // This happens when it is consumed by a GT_RET_EXPR.
                    // It can only be a Vector2f or Vector2i.
                    assert(genTypeSize(simdTree->gtSIMDBaseType) == 4);
                    simdTree->gtType = TYP_SIMD8;
                }
                else if (simdTree->gtType == TYP_STRUCT || varTypeIsSIMD(simdTree))
                {
                    tree->gtType = simdType;
                }
                // Certain SIMD trees require rationalizing.
                if (simdTree->gtSIMD.gtSIMDIntrinsicID == SIMDIntrinsicInitArray)
                {
                    // Rewrite this as an explicit load.
                    JITDUMP("Rewriting GT_SIMD array init as an explicit load:\n");
                    unsigned int baseTypeSize = genTypeSize(simdTree->gtSIMDBaseType);
                    GenTree* address = new (comp, GT_LEA) GenTreeAddrMode(TYP_BYREF, simdTree->gtOp1, simdTree->gtOp2, baseTypeSize,  offsetof(CORINFO_Array, u1Elems));
                    GenTree *ind = comp->gtNewOperNode(GT_IND, simdType, address);
                    address->CopyCosts(simdTree);
                    ind->CopyCosts(simdTree);

                    // Fix up the links.
                    GenTreePtr addressPrev = simdTree->gtPrev;
                    assert(addressPrev != nullptr);
                    GenTree* indNext = simdTree->gtNext;
                    // We don't have any top-level GT_SIMD nodes.
                    assert(addressPrev != nullptr);

                    address->gtPrev = addressPrev;
                    addressPrev->gtNext = address;

                    ind->gtPrev = address;
                    address->gtNext = ind;

                    indNext->gtPrev = ind;
                    ind->gtNext = indNext;

                    // Replace "simdTree" with "ind"
                    *ppTree = ind;

                    DISPTREE(tmpState->root);
                    JITDUMP("\n");
                }
                else 
                {
                    // This code depends on the fact that NONE of the SIMD intrinsics take vector operands
                    // of a different width.  If that assumption changes, we will EITHER have to make these type
                    // transformations during importation, and plumb the types all the way through the JIT,
                    // OR add a lot of special handling here.
                    GenTree* op1 = simdTree->gtGetOp1();
                    if (op1 != nullptr && op1->gtType == TYP_STRUCT)
                    {
                        op1->gtType = simdType;
                    }
                    GenTree* op2 = simdTree->gtGetOp2();
                    if (op2 != nullptr && op2->gtType == TYP_STRUCT)
                    {
                        op2->gtType = simdType;
                    }
                }
            }
            break;
        }
        if ((*ppTree) != tree)
        {
            return SimpleTransformHelper(ppTree, data);
        }
    }
#endif // FEATURE_SIMD

    return Compiler::WALK_CONTINUE;
}

// FixupIfSIMDLocal: Fixup the type of a lclVar tree, as needed, if it is a SIMD type vector.
//
// Arguments:
//    comp      - the Compiler object.
//    tree      - the GenTreeLclVarCommon tree to be fixed up.
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.

void Rationalizer::FixupIfSIMDLocal(Compiler* comp, GenTreeLclVarCommon* tree)
{
#ifdef FEATURE_SIMD
    if (!comp->featureSIMD)
    {
        return;
    }

    LclVarDsc* varDsc = &(comp->lvaTable[tree->gtLclNum]);

    // Don't mark byref of SIMD vector as a SIMD type.
    // Note that struct args though marked as lvIsSIMD=true,
    // the tree node representing such an arg should not be 
    // marked as a SIMD type, since it is a byref of a SIMD type.
    if (!varTypeIsSIMD(varDsc))
    {
        return;
    }
    switch(tree->OperGet())
    {
    default:
        // Nothing to do for most tree nodes.
        break;

    case GT_LCL_FLD:
        // We may see a lclFld used for pointer-sized structs that have been morphed, in which
        // case we can change it to GT_LCL_VAR.
        // However, we may also see a lclFld with FieldSeqStore::NotAField() for structs that can't
        // be analyzed, e.g. those with overlapping fields such as the IL implementation of Vector<T>.
        if ((tree->AsLclFld()->gtFieldSeq == FieldSeqStore::NotAField()) &&
            (tree->AsLclFld()->gtLclOffs == 0)                           &&
            (tree->gtType == TYP_I_IMPL)                                 && 
            (varDsc->lvExactSize == TARGET_POINTER_SIZE))
        {
            tree->SetOper(GT_LCL_VAR);
            tree->gtFlags &= ~(GTF_VAR_USEASG);
        }
        else
        {
            // If we access a field of a SIMD lclVar via GT_LCL_FLD, it cannot have been
            // independently promoted.
            assert(comp->lvaGetPromotionType(varDsc) != Compiler::PROMOTION_TYPE_INDEPENDENT);
            return;
        }
        break;
    case GT_STORE_LCL_FLD:
        assert(tree->gtType == TYP_I_IMPL);
        tree->SetOper(GT_STORE_LCL_VAR);
        tree->gtFlags &= ~(GTF_VAR_USEASG);
        break;
    }
    unsigned simdSize = (unsigned int) roundUp(varDsc->lvExactSize, TARGET_POINTER_SIZE);
    tree->gtType = comp->getSIMDTypeForSize(simdSize);
#endif // FEATURE_SIMD
}

#ifdef DEBUG

void Rationalizer::ValidateStatement(Location loc)
{
    ValidateStatement(loc.tree, loc.block);
}

void Rationalizer::ValidateStatement(GenTree *tree, BasicBlock *block)
{
    assert(tree->gtOper == GT_STMT);
    DBEXEC(TRUE, GetTlsCompiler()->fgDebugCheckNodeLinks(block, tree));
}

// sanity checks that apply to all kinds of IR
void Rationalizer::SanityCheck()
{
    BasicBlock * block;
    foreach_block(comp, block)
    {
        for (GenTree* statement = block->bbTreeList;
             statement != nullptr;
             statement = statement->gtNext)
        {
            ValidateStatement(statement, block);

            for (GenTree *tree = statement->gtStmt.gtStmtList;
                 tree; 
                 tree = tree->gtNext)
            {
                if (tree->OperGet() == GT_ASG)
                {
                    if (tree->gtGetOp1()->OperGet() == GT_LCL_VAR)
                    {
                        assert(tree->gtGetOp1()->gtFlags & GTF_VAR_DEF);
                    }
                    else if (tree->gtGetOp2()->OperGet() == GT_LCL_VAR)
                    {
                        assert(!(tree->gtGetOp2()->gtFlags & GTF_VAR_DEF));
                    }
                }
            }
        }
    }
}

void Rationalizer::SanityCheckRational()
{
    // TODO-Cleanup : check that the tree is rational here
    // then do normal checks
    SanityCheck();
}

#endif // DEBUG

void Rationalizer::DoPhase()
{
    DBEXEC(TRUE, SanityCheck());

    comp->compCurBB = NULL;
    comp->fgOrder = Compiler::FGOrderLinear;

    // If the first block is BBF_INTERNAL, it is special.  Zero-inits must be placed in 
    // this block, and it must fall through to the next block.  
    // If there is a question op in the block (as can be the case with a just-my-code helper)
    // then the rationalizer will expand that to flow and break the fallthrough invariant.  
    // However, we need to still keep the zero-inits in the original block, so only split before
    // the statement containing the qmark.

    if (comp->fgFirstBB->bbFlags & BBF_INTERNAL)
    {
        BasicBlock* const block = comp->fgFirstBB;
        for (GenTree* stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            GenTreePtr node;
            foreach_treenode_execution_order(node, stmt)
            {
                if (node->gtOper == GT_QMARK)
                {
                    BasicBlock* newBlock;
                    if (stmt == block->bbTreeList)
                        newBlock = comp->fgSplitBlockAtBeginning(comp->fgFirstBB);
                    else
                        newBlock = comp->fgSplitBlockAfterStatement(block, stmt);
                    newBlock->bbFlags &= ~BBF_INTERNAL;
                }
            }
        }
    }

    use     = hashBv::Create(this->comp); // is used
    usedef  = hashBv::Create(this->comp); // is used and then defined
    rename  = hashBv::Create(this->comp); // is used, defined and used again
    unexp   = hashBv::Create(this->comp); // is unexposed - there is a def in this tree before any uses

    // break up the trees at side effects, etc
    Location loc(comp->fgFirstBB);
    while (loc.block)
    {
        RenameUpdatedVars(loc);

        // If we have a top-level GT_COMMA(X, GT_NOP), replace it by X.
        if (loc.tree->gtStmt.gtStmtExpr->OperGet() == GT_COMMA)
        {
            GenTree* commaStmt = loc.tree;
            GenTree* commaOp = commaStmt->gtStmt.gtStmtExpr;
            if (commaOp->gtGetOp2()->OperGet() == GT_NOP)
            {
#ifdef DEBUG
                if (comp->verbose)
                {
                    printf("Replacing GT_COMMA(X, GT_NOP) by X\n");
                    comp->gtDispTree(commaOp);
                    printf("\n");
                }
#endif // DEBUG

                comp->fgSnipNode(commaStmt->AsStmt(), commaOp);
                comp->fgDeleteTreeFromList(commaStmt->AsStmt(), commaOp->gtGetOp2());
                commaStmt->gtStmt.gtStmtExpr = commaOp->gtGetOp1();
            }
        }

        loc = loc.Next();
    }

    loc.Reset(comp);

    while (loc.block)
    {
        loc = TreeTransformRationalization(loc);
        loc = loc.Next();
    }

    DBEXEC(TRUE, SanityCheckRational());

    comp->compRationalIRForm = true;
}
