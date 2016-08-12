// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               DecomposeLongs                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX*/

//
// This file contains code to decompose 64-bit LONG operations on 32-bit platforms
// into multiple single-register operations so individual register usage and requirements
// are explicit for LSRA. The rationale behind this is to avoid adding code complexity
// downstream caused by the introduction of handling longs as special cases,
// especially in LSRA.
//
// Long decomposition happens on a statement immediately prior to more general
// purpose lowering.
//

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator
#ifndef _TARGET_64BIT_ // DecomposeLongs is only used on 32-bit platforms

#include "decomposelongs.h"

//------------------------------------------------------------------------

//------------------------------------------------------------------------
// PrepareForDecomposition: Do one-time preparation required for LONG decomposition.
// Namely, promote long variables to multi-register structs.
//
// Arguments:
//    None
//
// Return Value:
//    None.
//
void DecomposeLongs::PrepareForDecomposition()
{
    m_compiler->lvaPromoteLongVars();
}


//------------------------------------------------------------------------
// DecomposeBlock: Do LONG decomposition to all the statements in the given block.
// This must be done before lowering the block, as decomposition can insert
// additional statements.
//
// Decomposition is done as a post-order tree walk. Lower levels of the tree can
// create new nodes that need to be further decomposed at higher levels. That is,
// the decomposition "bubbles up" the tree.
//
// Arguments:
//    block - the block to process
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeBlock(BasicBlock* block)
{
    assert(block == m_compiler->compCurBB); // compCurBB must already be set.

    for (GenTree* stmt = block->bbTreeList; stmt != nullptr; stmt = stmt->gtNext)
    {
#ifdef DEBUG
        if (m_compiler->verbose)
        {
            printf("Decomposing BB%02u, stmt id %u\n", block->bbNum, stmt->gtTreeID);
        }
#endif // DEBUG

        DecomposeStmt(stmt->AsStmt());
    }
}


//------------------------------------------------------------------------
// DecomposeStmt: Do LONG decomposition to a statement tree.
//
// Arguments:
//    stmt - the statement to process
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeStmt(GenTreeStmt* stmt)
{
    GenTree* savedStmt = m_compiler->compCurStmt; // We'll need to restore this later, in case this call was recursive.
    m_compiler->compCurStmt = stmt;               // Publish the current statement globally. One reason:
                                                  // fgInsertEmbeddedFormTemp requires it.
    m_compiler->fgWalkTreePost(&stmt->gtStmt.gtStmtExpr, &DecomposeLongs::DecompNodeHelper, this, true);
    m_compiler->compCurStmt = savedStmt;
}


//------------------------------------------------------------------------
// DecompNodeHelper: fgWalkTreePost callback helper for LONG decomposition
//
// Arguments:
//    ppTree - tree node we are working on.
//    data - tree walk context, with data->pCallbackData as a DecomposeLongs*
//
// Return Value:
//    Standard tree walk result.
//
// static
Compiler::fgWalkResult DecomposeLongs::DecompNodeHelper(GenTree** ppTree, Compiler::fgWalkData* data)
{
    DecomposeLongs* decomp = (DecomposeLongs*)data->pCallbackData;
    decomp->DecomposeNode(ppTree, data);
    return Compiler::WALK_CONTINUE;
}


//------------------------------------------------------------------------
// DecomposeNode: Decompose long-type trees into lower and upper halves.
//
// Arguments:
//    *ppTree - A node that may or may not require decomposition.
//    data    - The tree-walk data that provides the context.
//
// Return Value:
//    None. It the tree at *ppTree is of TYP_LONG, it will generally be replaced.
//
void DecomposeLongs::DecomposeNode(GenTree** ppTree, Compiler::fgWalkData* data)
{
    GenTree* tree = *ppTree;

    // Handle the case where we are implicitly using the lower half of a long lclVar.
    if ((tree->TypeGet() == TYP_INT) && tree->OperIsLocal())
    {
        LclVarDsc* varDsc = m_compiler->lvaTable + tree->AsLclVarCommon()->gtLclNum;
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("Changing implicit reference to lo half of long lclVar to an explicit reference of its promoted half:\n");
                m_compiler->gtDispTree(tree);
            }
#endif // DEBUG
            m_compiler->lvaDecRefCnts(tree);
            unsigned loVarNum = varDsc->lvFieldLclStart;
            tree->AsLclVarCommon()->SetLclNum(loVarNum);
            m_compiler->lvaIncRefCnts(tree);
            return;
        }
    }

    if (tree->TypeGet() != TYP_LONG)
    {
        return;
    }

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("Decomposing TYP_LONG tree.  BEFORE:\n");
        m_compiler->gtDispTree(tree);
    }
#endif // DEBUG

    switch (tree->OperGet())
    {
    case GT_PHI:
    case GT_PHI_ARG:
        break;

    case GT_LCL_VAR:
        DecomposeLclVar(ppTree, data);
        break;

    case GT_LCL_FLD:
        DecomposeLclFld(ppTree, data);
        break;

    case GT_STORE_LCL_VAR:
        DecomposeStoreLclVar(ppTree, data);
        break;

    case GT_CAST:
        DecomposeCast(ppTree, data);
        break;

    case GT_CNS_LNG:
        DecomposeCnsLng(ppTree, data);
        break;

    case GT_CALL:
        DecomposeCall(ppTree, data);
        break;

    case GT_RETURN:
        assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
        break;

    case GT_STOREIND:
        DecomposeStoreInd(ppTree, data);
        break;

    case GT_STORE_LCL_FLD:
        assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
        NYI("st.lclFld of of TYP_LONG");
        break;

    case GT_IND:
        DecomposeInd(ppTree, data);
        break;

    case GT_NOT:
        DecomposeNot(ppTree, data);
        break;

    case GT_NEG:
        DecomposeNeg(ppTree, data);
        break;

    // Binary operators. Those that require different computation for upper and lower half are
    // handled by the use of GetHiOper().
    case GT_ADD:
    case GT_SUB:
    case GT_OR:
    case GT_XOR:
    case GT_AND:
        DecomposeArith(ppTree, data);
        break;

    case GT_MUL:
        NYI("Arithmetic binary operators on TYP_LONG - GT_MUL");
        break;

    case GT_DIV:
        NYI("Arithmetic binary operators on TYP_LONG - GT_DIV");
        break;

    case GT_MOD:
        NYI("Arithmetic binary operators on TYP_LONG - GT_MOD");
        break;

    case GT_UDIV:
        NYI("Arithmetic binary operators on TYP_LONG - GT_UDIV");
        break;

    case GT_UMOD:
        NYI("Arithmetic binary operators on TYP_LONG - GT_UMOD");
        break;

    case GT_LSH:
    case GT_RSH:
    case GT_RSZ:
        NYI("Arithmetic binary operators on TYP_LONG - SHIFT");
        break;

    case GT_ROL:
    case GT_ROR:
        NYI("Arithmetic binary operators on TYP_LONG - ROTATE");
        break;

    case GT_MULHI:
        NYI("Arithmetic binary operators on TYP_LONG - MULHI");
        break;

    case GT_LOCKADD:
    case GT_XADD:
    case GT_XCHG:
    case GT_CMPXCHG:
        NYI("Interlocked operations on TYP_LONG");
        break;

    default:
        {
            JITDUMP("Illegal TYP_LONG node %s in Decomposition.", GenTree::NodeName(tree->OperGet()));
            noway_assert(!"Illegal TYP_LONG node in Decomposition.");
            break;
        }
    }

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("  AFTER:\n");
        m_compiler->gtDispTree(*ppTree);
    }
#endif
}
 

//------------------------------------------------------------------------
// FinalizeDecomposition: A helper function to finalize LONG decomposition by
// taking the resulting two halves of the decomposition, and tie them together
// with a new GT_LONG node that will replace the original node.
//
// Arguments:
//    ppTree - the original tree node
//    data - tree walk context
//    loResult - the decomposed low part
//    hiResult - the decomposed high part
//
// Return Value:
//    None.
//
void DecomposeLongs::FinalizeDecomposition(GenTree** ppTree, Compiler::fgWalkData* data, GenTree* loResult, GenTree* hiResult)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert(loResult != nullptr);
    assert(hiResult != nullptr);
    assert(m_compiler->compCurStmt != nullptr);

    GenTree* tree = *ppTree;

    m_compiler->fgInsertTreeInListAfter(hiResult, loResult, m_compiler->compCurStmt->AsStmt());
    hiResult->CopyCosts(tree);

    GenTree* newTree = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
    SimpleLinkNodeAfter(hiResult, newTree);
    m_compiler->fgFixupIfCallArg(data->parentStack, tree, newTree);
    newTree->CopyCosts(tree);
    *ppTree = newTree;
}


//------------------------------------------------------------------------
// DecomposeLclVar: Decompose GT_LCL_VAR.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeLclVar(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_LCL_VAR);

    GenTree* tree = *ppTree;
    unsigned varNum = tree->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = m_compiler->lvaTable + varNum;
    m_compiler->lvaDecRefCnts(tree);

    GenTree* loResult = tree;
    loResult->gtType = TYP_INT;
    GenTree* hiResult = m_compiler->gtNewLclLNode(varNum, TYP_INT);

    if (varDsc->lvPromoted)
    {
        assert(varDsc->lvFieldCnt == 2);
        unsigned loVarNum = varDsc->lvFieldLclStart;
        unsigned hiVarNum = loVarNum + 1;
        loResult->AsLclVarCommon()->SetLclNum(loVarNum);
        hiResult->AsLclVarCommon()->SetLclNum(hiVarNum);
    }
    else
    {
        noway_assert(varDsc->lvLRACandidate == false);

        loResult->SetOper(GT_LCL_FLD);
        loResult->AsLclFld()->gtLclOffs = 0;
        loResult->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();

        hiResult->SetOper(GT_LCL_FLD);
        hiResult->AsLclFld()->gtLclOffs = 4;
        hiResult->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
    }

    m_compiler->lvaIncRefCnts(loResult);
    m_compiler->lvaIncRefCnts(hiResult);

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}


//------------------------------------------------------------------------
// DecomposeLclFld: Decompose GT_LCL_FLD.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeLclFld(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_LCL_FLD);

    GenTree* tree = *ppTree;
    GenTreeLclFld* loResult = tree->AsLclFld();
    loResult->gtType = TYP_INT;

    GenTree* hiResult = m_compiler->gtNewLclFldNode(loResult->gtLclNum,
                                              TYP_INT,
                                              loResult->gtLclOffs + 4);

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}


//------------------------------------------------------------------------
// DecomposeStoreLclVar: Decompose GT_STORE_LCL_VAR.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeStoreLclVar(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_STORE_LCL_VAR);
    assert(m_compiler->compCurStmt != nullptr);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();

    GenTree* tree = *ppTree;
    GenTree* nextTree = tree->gtNext;
    GenTree* rhs = tree->gtGetOp1();
    if ((rhs->OperGet() == GT_PHI) || (rhs->OperGet() == GT_CALL))
    {
        // GT_CALLs are not decomposed, so will not be converted to GT_LONG
        // GT_STORE_LCL_VAR = GT_CALL are handled in genMultiRegCallStoreToLocal
        return;
    }

    noway_assert(rhs->OperGet() == GT_LONG);
    unsigned varNum = tree->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = m_compiler->lvaTable + varNum;
    m_compiler->lvaDecRefCnts(tree);

    GenTree* loRhs = rhs->gtGetOp1();
    GenTree* hiRhs = rhs->gtGetOp2();
    GenTree* hiStore = m_compiler->gtNewLclLNode(varNum, TYP_INT);

    if (varDsc->lvPromoted)
    {
        assert(varDsc->lvFieldCnt == 2);

        unsigned loVarNum = varDsc->lvFieldLclStart;
        unsigned hiVarNum = loVarNum + 1;
        tree->AsLclVarCommon()->SetLclNum(loVarNum);
        hiStore->SetOper(GT_STORE_LCL_VAR);
        hiStore->AsLclVarCommon()->SetLclNum(hiVarNum);
    }
    else
    {
        noway_assert(varDsc->lvLRACandidate == false);

        tree->SetOper(GT_STORE_LCL_FLD);
        tree->AsLclFld()->gtLclOffs = 0;
        tree->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();

        hiStore->SetOper(GT_STORE_LCL_FLD);
        hiStore->AsLclFld()->gtLclOffs = 4;
        hiStore->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
    }

    tree->gtOp.gtOp1 = loRhs;
    tree->gtType = TYP_INT;

    loRhs->gtNext = tree;
    tree->gtPrev = loRhs;

    hiStore->gtOp.gtOp1 = hiRhs;
    hiStore->CopyCosts(tree);
    hiStore->gtFlags |= GTF_VAR_DEF;

    m_compiler->lvaIncRefCnts(tree);
    m_compiler->lvaIncRefCnts(hiStore);

    tree->gtNext = hiRhs;
    hiRhs->gtPrev = tree;
    hiRhs->gtNext = hiStore;
    hiStore->gtPrev = hiRhs;
    hiStore->gtNext = nextTree;
    if (nextTree != nullptr)
    {
        nextTree->gtPrev = hiStore;
    }
    nextTree = hiRhs;

    bool isEmbeddedStmt = !curStmt->gtStmtIsTopLevel();
    if (!isEmbeddedStmt)
    {
        tree->gtNext = nullptr;
        hiRhs->gtPrev = nullptr;
    }

    InsertNodeAsStmt(hiStore);
}


//------------------------------------------------------------------------
// DecomposeCast: Decompose GT_CAST.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeCast(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_CAST);
    assert(m_compiler->compCurStmt != nullptr);

    GenTree* tree = *ppTree;
    GenTree* loResult = nullptr;
    GenTree* hiResult = nullptr;
    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();

    assert(tree->gtPrev == tree->gtGetOp1());
    NYI_IF(tree->gtOverflow(), "TYP_LONG cast with overflow");
    switch (tree->AsCast()->CastFromType())
    {
    case TYP_INT:
        if (tree->gtFlags & GTF_UNSIGNED)
        {
            loResult = tree->gtGetOp1();
            hiResult = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_INT, 0);
            m_compiler->fgSnipNode(curStmt, tree);
        }
        else
        {
            NYI("Lowering of signed cast TYP_INT->TYP_LONG");
        }
        break;

    default:
        NYI("Unimplemented type for Lowering of cast to TYP_LONG");
        break;
    }

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}


//------------------------------------------------------------------------
// DecomposeCnsLng: Decompose GT_CNS_LNG.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeCnsLng(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_CNS_LNG);

    GenTree* tree = *ppTree;
    INT32 hiVal = tree->AsLngCon()->HiVal();

    GenTree* loResult = tree;
    loResult->ChangeOperConst(GT_CNS_INT);
    loResult->gtType = TYP_INT;

    GenTree* hiResult = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_INT, hiVal);

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}


//------------------------------------------------------------------------
// DecomposeCall: Decompose GT_CALL.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeCall(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_CALL);

    GenTree* parent = data->parent;

    // We only need to force var = call() if the call is not a top-level node.
    if (parent == nullptr)
        return;

    if (parent->gtOper == GT_STORE_LCL_VAR)
    {
        // If parent is already a STORE_LCL_VAR, we can skip it if
        // it is already marked as lvIsMultiRegRet.
        unsigned varNum = parent->AsLclVarCommon()->gtLclNum;
        if (m_compiler->lvaTable[varNum].lvIsMultiRegRet)
        {
            return;
        }
        else if (!m_compiler->lvaTable[varNum].lvPromoted)
        {
            // If var wasn't promoted, we can just set lvIsMultiRegRet.
            m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;
            return;
        }
    }

    // Otherwise, we need to force var = call()
    GenTree* tree = *ppTree;
    GenTree** treePtr = nullptr;
    parent = tree->gtGetParent(&treePtr);

    assert(treePtr != nullptr);

    GenTreeStmt* asgStmt = m_compiler->fgInsertEmbeddedFormTemp(treePtr);
    GenTree* stLclVar = asgStmt->gtStmtExpr;
    assert(stLclVar->OperIsLocalStore());

    unsigned varNum = stLclVar->AsLclVarCommon()->gtLclNum;
    m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;
    m_compiler->fgFixupIfCallArg(data->parentStack, tree, *treePtr);

    // Decompose new node
    DecomposeNode(treePtr, data);
}


//------------------------------------------------------------------------
// DecomposeStoreInd: Decompose GT_STOREIND.
//
// Arguments:
//    tree - the tree to decompose
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeStoreInd(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_STOREIND);
    assert(m_compiler->compCurStmt != nullptr);

    GenTree* tree = *ppTree;

    assert(tree->gtOp.gtOp2->OperGet() == GT_LONG);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();
    bool isEmbeddedStmt = !curStmt->gtStmtIsTopLevel();

    // Example input trees (a nested embedded statement case)
    //
    //   <linkBegin Node>
    //   *  stmtExpr  void  (top level) (IL   ???...  ???)
    //   |  /--*  argPlace  ref    $280
    //   |  +--*  argPlace  int    $4a
    //   |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //   |  |  {  |     /--*  lclVar    ref    V11 tmp9         u:3 $21c
    //   |  |  {  |     +--*  const     int    4 $44
    //   |  |  {  |  /--*  +         byref  $2c8
    //   |  |  {  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //   |  |  {  |  |     {  |  /--*  lclFld    long   V01 arg1         u:2[+8] Fseq[i] $380
    //   |  |  {  |  |     {  \--*  st.lclVar long  (P) V21 cse8
    //   |  |  {  |  |     {  \--*    int    V21.hi (offs=0x00) -> V22 rat0    
    //   |  |  {  |  |     {  \--*    int    V21.hi (offs=0x04) -> V23 rat1    
    //   |  |  {  |  |  /--*  lclVar    int    V22 rat0          $380
    //   |  |  {  |  |  +--*  lclVar    int    V23 rat1
    //   |  |  {  |  +--*  gt_long   long
    //   |  |  {  \--*  storeIndir long
    //   |  +--*  lclVar    ref    V11 tmp9         u:3 (last use) $21c
    //   |  +--*  lclVar    ref    V02 tmp0         u:3 $280
    //   |  +--*  const     int    8 $4a
    //   \--*  call help void   HELPER.CORINFO_HELP_ARRADDR_ST $205
    //  <linkEndNode>
    //
    // (editor brace matching compensation: }}}}}}}}}}}}}}}}}})

    GenTree* linkBegin = m_compiler->fgGetFirstNode(tree)->gtPrev;
    GenTree* linkEnd = tree->gtNext;
    GenTree* gtLong = tree->gtOp.gtOp2;

    // Save address to a temp. It is used in storeIndLow and storeIndHigh trees.
    GenTreeStmt* addrStmt = CreateTemporary(&tree->gtOp.gtOp1);
    JITDUMP("[DecomposeStoreInd]: Saving address tree to a temp var:\n");
    DISPTREE(addrStmt);

    if (!gtLong->gtOp.gtOp1->OperIsLeaf())
    {
        GenTreeStmt* dataLowStmt = CreateTemporary(&gtLong->gtOp.gtOp1);
        JITDUMP("[DecomposeStoreInd]: Saving low data tree to a temp var:\n");
        DISPTREE(dataLowStmt);
    }

    if (!gtLong->gtOp.gtOp2->OperIsLeaf())
    {
        GenTreeStmt* dataHighStmt = CreateTemporary(&gtLong->gtOp.gtOp2);
        JITDUMP("[DecomposeStoreInd]: Saving high data tree to a temp var:\n");
        DISPTREE(dataHighStmt);
    }

    // Example trees after embedded statements for address and data are added.
    // This example saves all address and data trees into temp variables 
    // to show how those embedded statements are created.
    //
    //  *  stmtExpr  void  (top level) (IL   ???...  ???)
    //  |  /--*  argPlace  ref    $280
    //  |  +--*  argPlace  int    $4a
    //  |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |     /--*  lclVar    ref    V11 tmp9         u:3 $21c
    //  |  |  {  |     +--*  const     int    4 $44
    //  |  |  {  |  /--*  +         byref  $2c8
    //  |  |  {  \--*  st.lclVar byref  V24 rat2
    //  |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |  /--*  lclVar    byref  V24 rat2
    //  |  |  {  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |  |     {  |  /--*  lclFld    long   V01 arg1         u:2[+8] Fseq[i] $380380
    //  |  |  {  |  |     {  \--*  st.lclVar long  (P) V21 cse8
    //  |  |  {  |  |     {  \--*    int    V21.hi (offs=0x00) -> V22 rat0
    //  |  |  {  |  |     {  \--*    int    V21.hi (offs=0x04) -> V23 rat1
    //  |  |  {  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |  |     {  |  /--*  lclVar    int    V22 rat0          $380
    //  |  |  {  |  |     {  \--*  st.lclVar int    V25 rat3
    //  |  |  {  |  |  /--*  lclVar    int    V25 rat3
    //  |  |  {  |  |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |  |  |  {  |  /--*  lclVar    int    V23 rat1
    //  |  |  {  |  |  |  {  \--*  st.lclVar int    V26 rat4
    //  |  |  {  |  |  +--*  lclVar    int    V26 rat4
    //  |  |  {  |  +--*  gt_long   long
    //  |  |  {  \--*  storeIndir long
    //  |  +--*  lclVar    ref    V11 tmp9         u:3 (last use) $21c
    //  |  +--*  lclVar    ref    V02 tmp0         u:3 $280
    //  |  +--*  const     int    8 $4a
    //  \--*  call help void   HELPER.CORINFO_HELP_ARRADDR_ST $205
    //
    // (editor brace matching compensation: }}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}})

    GenTree* addrBase = tree->gtOp.gtOp1;
    GenTree* dataHigh = gtLong->gtOp.gtOp2;
    GenTree* dataLow = gtLong->gtOp.gtOp1;
    GenTree* storeIndLow = tree;

    // Rewrite storeIndLow tree to save only lower 32-bit data.
    // 
    //  |  |  {  |  /--*  lclVar    byref  V24 rat2   (address)
    //  ...
    //  |  |  {  |  +--*  lclVar    int    V25 rat3   (lower 32-bit data)
    //  |  |  {  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |  {  |  {  |  /--*  lclVar    int    V23 rat1
    //  |  |  {  |  {  \--*  st.lclVar int    V26 rat4
    //  |  |  {  \--*  storeIndir int
    //
    // (editor brace matching compensation: }}}}}}}}})

    m_compiler->fgSnipNode(curStmt, gtLong);
    m_compiler->fgSnipNode(curStmt, dataHigh);
    storeIndLow->gtOp.gtOp2 = dataLow;
    storeIndLow->gtType = TYP_INT;

    // Construct storeIndHigh tree
    //
    // | | {  *stmtExpr  void  (embedded)(IL ? ? ? ... ? ? ? )
    // | | { | / --*  lclVar    int    V26 rat4
    // | | { | | / --*  lclVar    byref  V24 rat2
    // | | { | +--*  lea(b + 4)  ref
    // | | {  \--*  storeIndir int
    //
    // (editor brace matching compensation: }}}}})

    GenTree* addrBaseHigh = new(m_compiler, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, 
        addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);
    GenTree* addrHigh = new(m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTree* storeIndHigh = new(m_compiler, GT_STOREIND) GenTreeStoreInd(TYP_INT, addrHigh, dataHigh);
    storeIndHigh->gtFlags = (storeIndLow->gtFlags & (GTF_ALL_EFFECT | GTF_LIVENESS_MASK));
    storeIndHigh->gtFlags |= GTF_REVERSE_OPS;
    storeIndHigh->CopyCosts(storeIndLow);

    // Internal links of storeIndHigh tree
    dataHigh->gtPrev = nullptr;
    dataHigh->gtNext = nullptr;
    SimpleLinkNodeAfter(dataHigh, addrBaseHigh);
    SimpleLinkNodeAfter(addrBaseHigh, addrHigh);
    SimpleLinkNodeAfter(addrHigh, storeIndHigh);
    
    // External links of storeIndHigh tree
    // dataHigh->gtPrev = nullptr;
    if (isEmbeddedStmt)
    {
        // If storeIndTree is an embedded statement, connect storeIndLow
        // and dataHigh
        storeIndLow->gtNext = dataHigh;
        dataHigh->gtPrev = storeIndLow;
    }
    storeIndHigh->gtNext = linkEnd;
    if (linkEnd != nullptr)
    {
        linkEnd->gtPrev = storeIndHigh;
    }

    InsertNodeAsStmt(storeIndHigh);

    // Example final output 
    //
    //  *  stmtExpr  void  (top level) (IL   ???...  ???)
    //  |  /--*  argPlace  ref    $280
    //  |  +--*  argPlace  int    $4a
    //  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |     /--*  lclVar    ref    V11 tmp9         u:3 $21c
    //  |  |     {  |     +--*  const     int    4 $44
    //  |  |     {  |  /--*  +         byref  $2c8
    //  |  |     {  \--*  st.lclVar byref  V24 rat2
    //  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |  /--*  lclVar    byref  V24 rat2
    //  |  |     {  |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |  |  {  |     /--*  lclFld    int    V01 arg1         u:2[+8] Fseq[i] $380
    //  |  |     {  |  |  {  |     +--*  lclFld    int    V01 arg1         [+12]
    //  |  |     {  |  |  {  |  /--*  gt_long   long
    //  |  |     {  |  |  {  \--*  st.lclVar long  (P) V21 cse8
    //  |  |     {  |  |  {  \--*    int    V21.hi (offs=0x00) -> V22 rat0    
    //  |  |     {  |  |  {  \--*    int    V21.hi (offs=0x04) -> V23 rat1    
    //  |  |     {  |  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |  |  {  |  /--*  lclVar    int    V22 rat0          $380
    //  |  |     {  |  |  {  \--*  st.lclVar int    V25 rat3
    //  |  |     {  |  +--*  lclVar    int    V25 rat3
    //  |  |     {  |  {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |  {  |  /--*  lclVar    int    V23 rat1
    //  |  |     {  |  {  \--*  st.lclVar int    V26 rat4
    //  |  |     {  \--*  storeIndir int
    //  |  |     {  *  stmtExpr  void  (embedded) (IL   ???...  ???)
    //  |  |     {  |  /--*  lclVar    int    V26 rat4
    //  |  |     {  |  |  /--*  lclVar    byref  V24 rat2
    //  |  |     {  |  +--*  lea(b+4)  ref
    //  |  |     {  \--*  storeIndir int
    //  |  |  /--*  lclVar    ref    V11 tmp9         u:3 (last use) $21c
    //  |  +--*  putarg_stk [+0x00] ref
    //  |  |  /--*  lclVar    ref    V02 tmp0         u:3 $280
    //  |  +--*  putarg_reg ref
    //  |  |  /--*  const     int    8 $4a
    //  |  +--*  putarg_reg int
    //  \--*  call help void   HELPER.CORINFO_HELP_ARRADDR_ST $205
    //
    // (editor brace matching compensation: }}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}})
}


//------------------------------------------------------------------------
// DecomposeInd: Decompose GT_IND.
//
// Arguments:
//    tree - the tree to decompose
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeInd(GenTree** ppTree, Compiler::fgWalkData* data)
{
    GenTreePtr indLow = *ppTree;
    GenTreeStmt* addrStmt = CreateTemporary(&indLow->gtOp.gtOp1);
    JITDUMP("[DecomposeInd]: Saving addr tree to a temp var:\n");
    DISPTREE(addrStmt);

    // Change the type of lower ind.
    indLow->gtType = TYP_INT;

    // Create tree of ind(addr+4)
    GenTreePtr addrBase = indLow->gtGetOp1();
    GenTreePtr addrBaseHigh = new(m_compiler, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR,
        addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);
    GenTreePtr addrHigh = new(m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTreePtr indHigh = new (m_compiler, GT_IND) GenTreeIndir(GT_IND, TYP_INT, addrHigh, nullptr);
    
    // Connect linear links
    SimpleLinkNodeAfter(addrBaseHigh, addrHigh);
    SimpleLinkNodeAfter(addrHigh, indHigh);

    FinalizeDecomposition(ppTree, data, indLow, indHigh);
}

//------------------------------------------------------------------------
// DecomposeNot: Decompose GT_NOT.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeNot(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_NOT);
    assert(m_compiler->compCurStmt != nullptr);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();

    GenTree* tree = *ppTree;
    GenTree* op1 = tree->gtGetOp1();
    noway_assert(op1->OperGet() == GT_LONG);
    GenTree* loOp1 = op1->gtGetOp1();
    GenTree* hiOp1 = op1->gtGetOp2();
    m_compiler->fgSnipNode(curStmt, op1);

    GenTree* loResult = tree;
    loResult->gtType = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;
    loOp1->gtNext = loResult;
    loResult->gtPrev = loOp1;

    GenTree* hiResult = new (m_compiler, GT_NOT) GenTreeOp(GT_NOT, TYP_INT, hiOp1, nullptr);
    hiOp1->gtNext = hiResult;
    hiResult->gtPrev = hiOp1;

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeNeg: Decompose GT_NEG.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeNeg(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert((*ppTree)->OperGet() == GT_NEG);
    assert(m_compiler->compCurStmt != nullptr);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();
    GenTree* tree = *ppTree;
    GenTree* op1 = tree->gtGetOp1();
    noway_assert(op1->OperGet() == GT_LONG);

    CreateTemporary(&(op1->gtOp.gtOp1));
    CreateTemporary(&(op1->gtOp.gtOp2));
    // Neither GT_NEG nor the introduced temporaries have side effects.
    tree->gtFlags &= ~GTF_ALL_EFFECT;
    GenTree* loOp1 = op1->gtGetOp1();
    GenTree* hiOp1 = op1->gtGetOp2();
    Compiler::fgSnipNode(curStmt, op1);

    GenTree* loResult = tree;
    loResult->gtType = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;

    GenTree* zero = m_compiler->gtNewZeroConNode(TYP_INT);
    GenTree* hiAdjust = m_compiler->gtNewOperNode(GT_ADD_HI, TYP_INT, hiOp1, zero);
    GenTree* hiResult = m_compiler->gtNewOperNode(GT_NEG, TYP_INT, hiAdjust);
    hiResult->gtFlags = tree->gtFlags;

    Compiler::fgSnipNode(curStmt, hiOp1);
    // fgSnipNode doesn't clear gtNext/gtPrev...
    hiOp1->gtNext = nullptr;
    hiOp1->gtPrev = nullptr;
    SimpleLinkNodeAfter(hiOp1, zero);
    SimpleLinkNodeAfter(zero, hiAdjust);
    SimpleLinkNodeAfter(hiAdjust, hiResult);

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeArith: Decompose GT_ADD, GT_SUB, GT_OR, GT_XOR, GT_AND.
//
// Arguments:
//    ppTree - the tree to decompose
//    data - tree walk context
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeArith(GenTree** ppTree, Compiler::fgWalkData* data)
{
    assert(ppTree != nullptr);
    assert(*ppTree != nullptr);
    assert(data != nullptr);
    assert(m_compiler->compCurStmt != nullptr);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();
    GenTree* tree = *ppTree;
    genTreeOps oper = tree->OperGet();

    assert((oper == GT_ADD) ||
           (oper == GT_SUB) ||
           (oper == GT_OR)  ||
           (oper == GT_XOR) ||
           (oper == GT_AND));

    NYI_IF((tree->gtFlags & GTF_REVERSE_OPS) != 0, "Binary operator with GTF_REVERSE_OPS");

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    // Both operands must have already been decomposed into GT_LONG operators.
    noway_assert((op1->OperGet() == GT_LONG) && (op2->OperGet() == GT_LONG));

    // Capture the lo and hi halves of op1 and op2.
    GenTree* loOp1 = op1->gtGetOp1();
    GenTree* hiOp1 = op1->gtGetOp2();
    GenTree* loOp2 = op2->gtGetOp1();
    GenTree* hiOp2 = op2->gtGetOp2();

    // We don't have support to decompose a TYP_LONG node that already has a child that has
    // been decomposed into parts, where the high part depends on the value generated by the
    // low part (via the flags register). For example, if we have:
    //    +(gt_long(+(lo3, lo4), +Hi(hi3, hi4)), gt_long(lo2, hi2))
    // We would decompose it here to:
    //    gt_long(+(+(lo3, lo4), lo2), +Hi(+Hi(hi3, hi4), hi2))
    // But this would generate incorrect code, because the "+Hi(hi3, hi4)" code generation
    // needs to immediately follow the "+(lo3, lo4)" part. Also, if this node is one that
    // requires a unique high operator, and the child nodes are not simple locals (e.g.,
    // they are decomposed nodes), then we also can't decompose the node, as we aren't
    // guaranteed the high and low parts will be executed immediately after each other.
    
    NYI_IF(hiOp1->OperIsHigh() ||
           hiOp2->OperIsHigh() ||
           (GenTree::OperIsHigh(GetHiOper(oper)) &&
            (!loOp1->OperIsLeaf() ||
             !hiOp1->OperIsLeaf() ||
             !loOp1->OperIsLeaf() ||
             !hiOp2->OperIsLeaf())),
            "Can't decompose expression tree TYP_LONG node");

    // Now, remove op1 and op2 from the node list.
    m_compiler->fgSnipNode(curStmt, op1);
    m_compiler->fgSnipNode(curStmt, op2);

    // We will reuse "tree" for the loResult, which will now be of TYP_INT, and its operands
    // will be the lo halves of op1 from above.
    GenTree* loResult = tree;
    loResult->SetOper(GetLoOper(loResult->OperGet()));
    loResult->gtType = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;
    loResult->gtOp.gtOp2 = loOp2;

    // The various halves will be correctly threaded internally. We simply need to
    // relink them into the proper order, i.e. loOp1 is followed by loOp2, and then
    // the loResult node.
    // (This rethreading, and that below, are where we need to address the reverse ops case).
    // The current order is (after snipping op1 and op2):
    // ... loOp1-> ... hiOp1->loOp2First ... loOp2->hiOp2First ... hiOp2
    // The order we want is:
    // ... loOp1->loOp2First ... loOp2->loResult
    // ... hiOp1->hiOp2First ... hiOp2->hiResult
    // i.e. we swap hiOp1 and loOp2, and create (for now) separate loResult and hiResult trees
    GenTree* loOp2First = hiOp1->gtNext;
    GenTree* hiOp2First = loOp2->gtNext;

    // First, we will NYI if both hiOp1 and loOp2 have side effects.
    NYI_IF(((loOp2->gtFlags & GTF_ALL_EFFECT) != 0) && ((hiOp1->gtFlags & GTF_ALL_EFFECT) != 0),
           "Binary long operator with non-reorderable sub expressions");

    // Now, we reorder the loOps and the loResult.
    loOp1->gtNext      = loOp2First;
    loOp2First->gtPrev = loOp1;
    loOp2->gtNext      = loResult;
    loResult->gtPrev   = loOp2;

    // Next, reorder the hiOps and the hiResult.
    GenTree* hiResult = new (m_compiler, oper) GenTreeOp(GetHiOper(oper), TYP_INT, hiOp1, hiOp2);
    hiOp1->gtNext      = hiOp2First;
    hiOp2First->gtPrev = hiOp1;
    hiOp2->gtNext      = hiResult;
    hiResult->gtPrev   = hiOp2;

    if ((oper == GT_ADD) || (oper == GT_SUB))
    {
        if (loResult->gtOverflow())
        {
            hiResult->gtFlags |= GTF_OVERFLOW;
            loResult->gtFlags &= ~GTF_OVERFLOW;
        }
        if (loResult->gtFlags & GTF_UNSIGNED)
        {
            hiResult->gtFlags |= GTF_UNSIGNED;
        }
    }

    FinalizeDecomposition(ppTree, data, loResult, hiResult);
}


//------------------------------------------------------------------------
// CreateTemporary: call fgInsertEmbeddedFormTemp to replace *ppTree with
// a new temp that is assigned to the value previously at *ppTree by inserting
// an embedded statement. In addition, if the resulting statement actually ends
// up being top-level, it might pull along some embedded statements that have
// not yet been decomposed. So recursively decompose those before returning.
//
// Arguments:
//    *ppTree - tree to replace with a temp.
//
// Return Value:
//    The new statement that was created to create the temp.
//
GenTreeStmt* DecomposeLongs::CreateTemporary(GenTree** ppTree)
{
    GenTreeStmt* newStmt = m_compiler->fgInsertEmbeddedFormTemp(ppTree);
    if (newStmt->gtStmtIsTopLevel())
    {
        for (GenTreeStmt* nextEmbeddedStmt = newStmt->gtStmtNextIfEmbedded();
             nextEmbeddedStmt != nullptr;
             nextEmbeddedStmt = nextEmbeddedStmt->gtStmt.gtStmtNextIfEmbedded())
        {
            DecomposeStmt(nextEmbeddedStmt);
        }
    }

    return newStmt;
}


//------------------------------------------------------------------------
// InsertNodeAsStmt: Insert a node as the root node of a new statement.
// If the current statement is embedded, the new statement will also be
// embedded. Otherwise, the new statement will be top level.
//
// Arguments:
//    node - node to insert
//
// Return Value:
//    None
//
// Notes:
// compCurStmt and compCurBB must be correctly set.
//
void DecomposeLongs::InsertNodeAsStmt(GenTree* node)
{
    assert(node != nullptr);
    assert(m_compiler->compCurBB != nullptr);
    assert(m_compiler->compCurStmt != nullptr);

    GenTreeStmt* curStmt = m_compiler->compCurStmt->AsStmt();
    GenTreeStmt* newStmt;

    if (curStmt->gtStmtIsTopLevel())
    {
        newStmt = m_compiler->fgNewStmtFromTree(node);

        // Find an insert point. Skip all embedded statements.
        GenTree* insertPt = curStmt;
        while ((insertPt->gtNext != nullptr) && (!insertPt->gtNext->AsStmt()->gtStmtIsTopLevel()))
        {
            insertPt = insertPt->gtNext;
        }

        m_compiler->fgInsertStmtAfter(m_compiler->compCurBB, insertPt, newStmt);
    }
    else
    {
        // The current statement is an embedded statement. Create a new embedded statement to
        // contain the node. First, find the parent non-embedded statement containing the
        // current statement.
        GenTree* parentStmt = curStmt;
        while ((parentStmt != nullptr) && (!parentStmt->AsStmt()->gtStmtIsTopLevel()))
        {
            parentStmt = parentStmt->gtPrev;
        }
        assert(parentStmt != nullptr);

        newStmt = m_compiler->fgMakeEmbeddedStmt(m_compiler->compCurBB, node, parentStmt);
    }

    newStmt->gtStmtILoffsx = curStmt->gtStmtILoffsx;
#ifdef DEBUG
    newStmt->gtStmtLastILoffs = curStmt->gtStmtLastILoffs;
#endif // DEBUG
}


//------------------------------------------------------------------------
// GetHiOper: Convert arithmetic operator to "high half" operator of decomposed node.
//
// Arguments:
//    oper - operator to map
//
// Return Value:
//    mapped operator
//
// static
genTreeOps DecomposeLongs::GetHiOper(genTreeOps oper)
{
    switch (oper)
    {
    case GT_ADD: return GT_ADD_HI;  break;
    case GT_SUB: return GT_SUB_HI;  break;
    case GT_MUL: return GT_MUL_HI;  break;
    case GT_DIV: return GT_DIV_HI;  break;
    case GT_MOD: return GT_MOD_HI;  break;
    case GT_OR:  return GT_OR;      break;
    case GT_AND: return GT_AND;     break;
    case GT_XOR: return GT_XOR;     break;
    default:
        assert(!"GetHiOper called for invalid oper");
        return GT_NONE;
    }
}


//------------------------------------------------------------------------
// GetLoOper: Convert arithmetic operator to "low half" operator of decomposed node.
//
// Arguments:
//    oper - operator to map
//
// Return Value:
//    mapped operator
//
// static
genTreeOps DecomposeLongs::GetLoOper(genTreeOps oper)
{
    switch (oper)
    {
    case GT_ADD: return GT_ADD_LO;  break;
    case GT_SUB: return GT_SUB_LO;  break;
    case GT_OR:  return GT_OR;      break;
    case GT_AND: return GT_AND;     break;
    case GT_XOR: return GT_XOR;     break;
    default:
        assert(!"GetLoOper called for invalid oper");
        return GT_NONE;
    }
}


//------------------------------------------------------------------------
// SimpleLinkNodeAfter: insert a node after a given node in the execution order.
// NOTE: Does not support inserting after the last node of a statement, which
// would require updating the statement links.
//
// Arguments:
//    insertionPoint - Insert after this tree node.
//    node - The node to insert.
//
// Return Value:
//    None
//
// Notes:
// Seems like this should be moved to someplace that houses all the flowgraph
// manipulation functions.
//
void DecomposeLongs::SimpleLinkNodeAfter(GenTree* insertionPoint, GenTree* node)
{
    assert(insertionPoint != nullptr);
    assert(node != nullptr);

    GenTree* nextTree = insertionPoint->gtNext;
    node->gtPrev = insertionPoint;
    node->gtNext = nextTree;
    insertionPoint->gtNext = node;
    if (nextTree != nullptr)
    {
        nextTree->gtPrev = node;
    }
}


#endif // !_TARGET_64BIT_
#endif // !LEGACY_BACKEND
