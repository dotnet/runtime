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
        JitTls::GetCompiler()->fgDispBasicBlocks(true);
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
// fgMakeEmbeddedStmt: insert the given subtree as an embedded statement
//
// Arguments:
//    block - The block containing the parentStmt, into which the new embedded
//            statement will go
//    tree  - The tree that will be the gtStmtExpr of the new embedded statement
//    parentStmt - A statement (top-level or embedded) that 'tree' is fully contained in
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
        // Note, however, that all the tree links have already been fixed up.
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
//    The caller must ensure that '*ppTree' is part of compCurStmt, and that
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


//--------------------------------------------------------------------------------------
// RewriteTopLevelComma - remove a top-level comma by creating a new preceding statement
//                        from its LHS and replacing the comma with its RHS (unless the
//                        comma's RHS is a NOP, in which case the comma is replaced with
//                        its LHS and no new statement is created)
//
// Returns the location of the statement that contains the LHS of the removed comma.
//--------------------------------------------------------------------------------------

Location Rationalizer::RewriteTopLevelComma(Location loc)
{
    GenTreeStmt* commaStmt = loc.tree->AsStmt();

    GenTree* commaOp = commaStmt->gtStmtExpr;
    assert(commaOp->OperGet() == GT_COMMA);

    GenTree* commaOp1 = commaOp->gtGetOp1();
    GenTree* commaOp2 = commaOp->gtGetOp2();

    if (commaOp2->IsNothingNode())
    {
#ifdef DEBUG
        if (comp->verbose)
        {
            printf("Replacing GT_COMMA(X, GT_NOP) by X\n");
            comp->gtDispTree(commaOp);
            printf("\n");
        }
#endif // DEBUG

        comp->fgSnipNode(commaStmt, commaOp);
        comp->fgDeleteTreeFromList(commaStmt, commaOp2);
        commaStmt->gtStmtExpr = commaOp1;

        return loc;
    }

    JITDUMP("splitting top level comma!\n");

    // Replace the comma node in the original statement with the RHS of the comma node.
    comp->fgDeleteTreeFromList(commaStmt, commaOp1);
    comp->fgSnipNode(commaStmt, commaOp);
    commaStmt->gtStmtExpr = commaOp2;

    // Create and insert a new preceding statement from the LHS of the comma node.
    GenTreeStmt* newStatement = comp->gtNewStmt(commaOp1, commaStmt->gtStmtILoffsx);
    newStatement->CopyCosts(commaOp1);
    newStatement->gtStmtList = Compiler::fgGetFirstNode(commaOp1);
    newStatement->gtStmtList->gtPrev = nullptr;
    commaOp1->gtNext = nullptr;

    comp->fgInsertStmtBefore(loc.block, commaStmt, newStatement);

    return Location(newStatement, loc.block);
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

    genTreeOps lhsOper = lhs->OperGet();
    genTreeOps storeOper;

    assert(lhsOper == GT_LCL_VAR || lhsOper == GT_LCL_FLD);

    storeOper = storeForm(lhsOper);
#ifdef DEBUG
    JITDUMP("rewriting asg(%s, X) to %s(X)\n", GenTree::NodeName(lhsOper), GenTree::NodeName(storeOper));
#endif // DEBUG

    GenTreeLclVarCommon* var = lhs->AsLclVarCommon();
    pTree->SetOper(storeOper);
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
        DBEXEC(VERBOSE, dumpTreeStack(JitTls::GetCompiler(), parentStack));
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
Location Rationalizer::TreeTransformRationalization(Location loc)
{
    GenTree*     savedCurStmt = comp->compCurStmt;
    GenTreeStmt* statement    = (loc.tree)->AsStmt();
    GenTree*     tree         = statement->gtStmt.gtStmtExpr;

    JITDUMP("TreeTransformRationalization, with statement:\n");
    DISPTREE(statement);
    JITDUMP("\n");

    DBEXEC(TRUE, loc.Validate());
    DBEXEC(TRUE, ValidateStatement(loc));

    if (statement->gtStmtIsTopLevel())
    {
        comp->compCurBB = loc.block;
        comp->compCurStmt = statement;

        while (tree->OperGet() == GT_COMMA)
        {
            // RewriteTopLevelComma may create a new preceding statement for the LHS of a
            // top-level comma. If it does, we need to process that statement now.
            Location newLoc = RewriteTopLevelComma(loc);
            if (newLoc.tree != statement)
            {
                (void)TreeTransformRationalization(newLoc);
            }

            // RewriteTopLevelComma also replaces the tree for this statement with the RHS
            // of the comma (or the LHS, if the RHS is a NOP), so we must reload it for
            // correctness.
            tree = statement->gtStmt.gtStmtExpr;
        }

        if (tree->OperKind() & GTK_CONST)
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

    DuplicateCommaProcessOneTree(comp, this, loc.block, loc.tree);

    JITDUMP("After simple transforms:\n");
    DISPTREE(statement);
    JITDUMP("\n");

    DBEXEC(TRUE, ValidateStatement(loc));

    comp->compCurStmt = savedCurStmt;
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

    (void) ((Rationalizer *)tmpState->thisPhase)->TreeTransformRationalization(Location(newStmt, tmpState->block));

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

        (void) ((Rationalizer *)tmpState->thisPhase)->TreeTransformRationalization(Location(newStmt, tmpState->block));

        if (!nested)
            comp->fgFixupIfCallArg(data->parentStack, comma, newSrc);

        (void) ((Rationalizer *)tmpState->thisPhase)->TreeTransformRationalization(Location(newStmt, tmpState->block));

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

// Rewrite GT_OBJ of SIMD Vector as GT_IND(GT_LEA(obj.op1)) of a SIMD type.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the GT_OBJ
//    fgWalkData  - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// TODO-Cleanup: Once SIMD types are plumbed through the frontend, this will no longer
// be required.
//
void Rationalizer::RewriteObj(GenTreePtr* ppTree, Compiler::fgWalkData* data)
{    
#ifdef FEATURE_SIMD
    Compiler* comp = data->compiler;
    GenTreeObj* obj = (*ppTree)->AsObj();

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    // For UNIX struct passing, we can have Obj nodes for arguments.
    // For other cases, we should never see a non-SIMD type here.

    if (!varTypeIsSIMD(obj))
    {
        return;
    }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
    // Should come here only if featureSIMD is enabled
    noway_assert(comp->featureSIMD);
    // On  we should only call this with a SIMD type.
    noway_assert(varTypeIsSIMD(obj));
    var_types simdType = obj->TypeGet();

    // If the operand of obj is a GT_ADDR(GT_LCL_VAR) and LclVar is known to be a SIMD type,
    // replace obj by GT_LCL_VAR.
    GenTree* srcAddr = obj->gtGetOp1();
    if (srcAddr->OperGet() == GT_ADDR && comp->isSIMDTypeLocal(srcAddr->gtGetOp1()))
    {
        GenTree* src = srcAddr->gtGetOp1();
        comp->fgSnipInnerNode(srcAddr);
        // It is possible for the obj to be the last node in the tree, if its result is
        // not actually stored anywhere and is not eliminated.
        // This can happen with an unused SIMD expression involving a localVar or temporary value,
        // where the SIMD expression is returning a non-SIMD value, and the expression is sufficiently
        // complex (e.g. a call to vector * scalar which is inlined but not an intrinsic).
        // The obj of the localVar is not eliminated, because it involves an indirection,
        // and therefore appears potentially unsafe to eliminate. However, when we transform the obj into
        // a plain localVar during the Rationalizer, we need to correctly handle the case where it has
        // no parent.
        // This happens, for example, with this source code:
        //      Vector4.Dot(default(Vector4) * 2f, Vector4.One);
        if (obj->gtNext == nullptr)
        {
            SplitData *tmpState = (SplitData *) data->pCallbackData;
            comp->fgSnipNode(tmpState->root->AsStmt(), obj);
        }
        else
        {
            comp->fgSnipInnerNode(obj);
        }
        comp->fgFixupIfCallArg(data->parentStack, obj, src);
        src->gtType = simdType;

        *ppTree = src;
    }
    else 
    {
        obj->SetOper(GT_IND);
        obj->gtType = simdType;
    }
#else
    // we should never reach without feature SIMD
    assert(!"Unexpected obj during rationalization\n");
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
    call->gtCall.setEntryPoint(entryPoint);
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
            CLANG_FORMAT_COMMENT_ANCHOR;
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

        case GT_OBJ:
            RewriteObj(ppTree, data);
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
    DBEXEC(TRUE, JitTls::GetCompiler()->fgDebugCheckNodeLinks(block, tree));
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
                // QMARK nodes should have been removed before this phase.
                assert(tree->OperGet() != GT_QMARK);

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

    // break up the trees at side effects, etc
    Location loc(comp->fgFirstBB);
    while (loc.block)
    {
        loc = TreeTransformRationalization(loc);
        loc = loc.Next();
    }

    DBEXEC(TRUE, SanityCheckRational());

    comp->compRationalIRForm = true;
}
