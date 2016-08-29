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
    assert(block->isEmpty() || block->IsLIR());

    m_block = block;

    GenTree* node = BlockRange().FirstNonPhiNode();
    while (node != nullptr)
    {
        LIR::Use use;
        if (!BlockRange().TryGetUse(node, &use))
        {
            use = LIR::Use::GetDummyUse(BlockRange(), node);
        }

        node = DecomposeNode(use);
    }

    assert(BlockRange().CheckLIR(m_compiler));
}

//------------------------------------------------------------------------
// DecomposeNode: Decompose long-type trees into lower and upper halves.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeNode(LIR::Use& use)
{
    GenTree* tree = use.Def();

    // Handle the case where we are implicitly using the lower half of a long lclVar.
    if ((tree->TypeGet() == TYP_INT) && tree->OperIsLocal())
    {
        LclVarDsc* varDsc = m_compiler->lvaTable + tree->AsLclVarCommon()->gtLclNum;
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("Changing implicit reference to lo half of long lclVar to an explicit reference of its promoted "
                       "half:\n");
                m_compiler->gtDispTreeRange(BlockRange(), tree);
            }
#endif // DEBUG
            m_compiler->lvaDecRefCnts(tree);
            unsigned loVarNum = varDsc->lvFieldLclStart;
            tree->AsLclVarCommon()->SetLclNum(loVarNum);
            m_compiler->lvaIncRefCnts(tree);
            return tree->gtNext;
        }
    }

    if (tree->TypeGet() != TYP_LONG)
    {
        return tree->gtNext;
    }

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("Decomposing TYP_LONG tree.  BEFORE:\n");
        m_compiler->gtDispTreeRange(BlockRange(), tree);
    }
#endif // DEBUG

    GenTree* nextNode = nullptr;
    switch (tree->OperGet())
    {
        case GT_PHI:
        case GT_PHI_ARG:
            nextNode = tree->gtNext;
            break;

        case GT_LCL_VAR:
            nextNode = DecomposeLclVar(use);
            break;

        case GT_LCL_FLD:
            nextNode = DecomposeLclFld(use);
            break;

        case GT_STORE_LCL_VAR:
            nextNode = DecomposeStoreLclVar(use);
            break;

        case GT_CAST:
            nextNode = DecomposeCast(use);
            break;

        case GT_CNS_LNG:
            nextNode = DecomposeCnsLng(use);
            break;

        case GT_CALL:
            nextNode = DecomposeCall(use);
            break;

        case GT_RETURN:
            assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
            break;

        case GT_STOREIND:
            nextNode = DecomposeStoreInd(use);
            break;

        case GT_STORE_LCL_FLD:
            assert(tree->gtOp.gtOp1->OperGet() == GT_LONG);
            NYI("st.lclFld of of TYP_LONG");
            break;

        case GT_IND:
            nextNode = DecomposeInd(use);
            break;

        case GT_NOT:
            nextNode = DecomposeNot(use);
            break;

        case GT_NEG:
            nextNode = DecomposeNeg(use);
            break;

        // Binary operators. Those that require different computation for upper and lower half are
        // handled by the use of GetHiOper().
        case GT_ADD:
        case GT_SUB:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
            nextNode = DecomposeArith(use);
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
        // NOTE: st_lcl_var doesn't dump properly afterwards.
        printf("Decomposing TYP_LONG tree.  AFTER:\n");
        m_compiler->gtDispTreeRange(BlockRange(), use.Def());
    }
#endif

    return nextNode;
}

//------------------------------------------------------------------------
// FinalizeDecomposition: A helper function to finalize LONG decomposition by
// taking the resulting two halves of the decomposition, and tie them together
// with a new GT_LONG node that will replace the original node.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//    loResult - the decomposed low part
//    hiResult - the decomposed high part. This must follow loResult in the linear order,
//               as the new GT_LONG node will be inserted immediately after it.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::FinalizeDecomposition(LIR::Use& use, GenTree* loResult, GenTree* hiResult)
{
    assert(use.IsInitialized());
    assert(loResult != nullptr);
    assert(hiResult != nullptr);
    assert(BlockRange().Contains(loResult));
    assert(BlockRange().Contains(hiResult));
    assert(loResult->Precedes(hiResult));

    GenTree* gtLong = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
    BlockRange().InsertAfter(hiResult, gtLong);

    use.ReplaceWith(m_compiler, gtLong);

    return gtLong->gtNext;
}

//------------------------------------------------------------------------
// DecomposeLclVar: Decompose GT_LCL_VAR.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeLclVar(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_LCL_VAR);

    GenTree*   tree   = use.Def();
    unsigned   varNum = tree->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = m_compiler->lvaTable + varNum;
    m_compiler->lvaDecRefCnts(tree);

    GenTree* loResult = tree;
    loResult->gtType  = TYP_INT;

    GenTree* hiResult = m_compiler->gtNewLclLNode(varNum, TYP_INT);
    BlockRange().InsertAfter(loResult, hiResult);

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
        loResult->AsLclFld()->gtLclOffs  = 0;
        loResult->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();

        hiResult->SetOper(GT_LCL_FLD);
        hiResult->AsLclFld()->gtLclOffs  = 4;
        hiResult->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
    }

    m_compiler->lvaIncRefCnts(loResult);
    m_compiler->lvaIncRefCnts(hiResult);

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeLclFld: Decompose GT_LCL_FLD.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeLclFld(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_LCL_FLD);

    GenTree*       tree     = use.Def();
    GenTreeLclFld* loResult = tree->AsLclFld();
    loResult->gtType        = TYP_INT;

    GenTree* hiResult = m_compiler->gtNewLclFldNode(loResult->gtLclNum, TYP_INT, loResult->gtLclOffs + 4);
    BlockRange().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeStoreLclVar: Decompose GT_STORE_LCL_VAR.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeStoreLclVar(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_STORE_LCL_VAR);

    GenTree* tree = use.Def();
    GenTree* rhs  = tree->gtGetOp1();
    if ((rhs->OperGet() == GT_PHI) || (rhs->OperGet() == GT_CALL))
    {
        // GT_CALLs are not decomposed, so will not be converted to GT_LONG
        // GT_STORE_LCL_VAR = GT_CALL are handled in genMultiRegCallStoreToLocal
        return tree->gtNext;
    }

    noway_assert(rhs->OperGet() == GT_LONG);
    unsigned   varNum = tree->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = m_compiler->lvaTable + varNum;
    m_compiler->lvaDecRefCnts(tree);

    GenTree* loRhs   = rhs->gtGetOp1();
    GenTree* hiRhs   = rhs->gtGetOp2();
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
        tree->AsLclFld()->gtLclOffs  = 0;
        tree->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();

        hiStore->SetOper(GT_STORE_LCL_FLD);
        hiStore->AsLclFld()->gtLclOffs  = 4;
        hiStore->AsLclFld()->gtFieldSeq = FieldSeqStore::NotAField();
    }

    // 'tree' is going to steal the loRhs node for itself, so we need to remove the
    // GT_LONG node from the threading.
    BlockRange().Remove(rhs);

    tree->gtOp.gtOp1 = loRhs;
    tree->gtType     = TYP_INT;

    hiStore->gtOp.gtOp1 = hiRhs;
    hiStore->gtFlags |= GTF_VAR_DEF;

    m_compiler->lvaIncRefCnts(tree);
    m_compiler->lvaIncRefCnts(hiStore);

    BlockRange().InsertAfter(tree, hiStore);

    return hiStore->gtNext;
}

//------------------------------------------------------------------------
// DecomposeCast: Decompose GT_CAST.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeCast(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_CAST);

    GenTree* tree     = use.Def();
    GenTree* loResult = nullptr;
    GenTree* hiResult = nullptr;

    assert(tree->gtPrev == tree->gtGetOp1());
    NYI_IF(tree->gtOverflow(), "TYP_LONG cast with overflow");
    switch (tree->AsCast()->CastFromType())
    {
        case TYP_INT:
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                loResult = tree->gtGetOp1();
                BlockRange().Remove(tree);

                hiResult = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_INT, 0);
                BlockRange().InsertAfter(loResult, hiResult);
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

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeCnsLng: Decompose GT_CNS_LNG.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeCnsLng(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_CNS_LNG);

    GenTree* tree  = use.Def();
    INT32    hiVal = tree->AsLngCon()->HiVal();

    GenTree* loResult = tree;
    loResult->ChangeOperConst(GT_CNS_INT);
    loResult->gtType = TYP_INT;

    GenTree* hiResult = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_INT, hiVal);
    BlockRange().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeCall: Decompose GT_CALL.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeCall(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_CALL);

    // We only need to force var = call() if the call's result is used.
    if (use.IsDummyUse())
        return use.Def()->gtNext;

    GenTree* user = use.User();
    if (user->OperGet() == GT_STORE_LCL_VAR)
    {
        // If parent is already a STORE_LCL_VAR, we can skip it if
        // it is already marked as lvIsMultiRegRet.
        unsigned varNum = user->AsLclVarCommon()->gtLclNum;
        if (m_compiler->lvaTable[varNum].lvIsMultiRegRet)
        {
            return use.Def()->gtNext;
        }
        else if (!m_compiler->lvaTable[varNum].lvPromoted)
        {
            // If var wasn't promoted, we can just set lvIsMultiRegRet.
            m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;
            return use.Def()->gtNext;
        }
    }

    GenTree* originalNode = use.Def();

    // Otherwise, we need to force var = call()
    unsigned varNum                              = use.ReplaceWithLclVar(m_compiler, m_block->getBBWeight(m_compiler));
    m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;

    // Decompose the new LclVar use
    return DecomposeLclVar(use);
}

//------------------------------------------------------------------------
// DecomposeStoreInd: Decompose GT_STOREIND.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeStoreInd(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_STOREIND);

    GenTree* tree = use.Def();

    assert(tree->gtOp.gtOp2->OperGet() == GT_LONG);

    // Example input (address expression omitted):
    //
    //  t51 = const     int    0x37C05E7D
    // t154 = const     int    0x2A0A3C80
    //      / --*  t51    int
    //      + --*  t154   int
    // t155 = *gt_long   long
    //      / --*  t52    byref
    //      + --*  t155   long
    //      *  storeIndir long

    GenTree* gtLong      = tree->gtOp.gtOp2;
    unsigned blockWeight = m_block->getBBWeight(m_compiler);

    // Save address to a temp. It is used in storeIndLow and storeIndHigh trees.
    LIR::Use address(BlockRange(), &tree->gtOp.gtOp1, tree);
    address.ReplaceWithLclVar(m_compiler, blockWeight);
    JITDUMP("[DecomposeStoreInd]: Saving address tree to a temp var:\n");
    DISPTREERANGE(BlockRange(), address.Def());

    if (!gtLong->gtOp.gtOp1->OperIsLeaf())
    {
        LIR::Use op1(BlockRange(), &gtLong->gtOp.gtOp1, gtLong);
        op1.ReplaceWithLclVar(m_compiler, blockWeight);
        JITDUMP("[DecomposeStoreInd]: Saving low data tree to a temp var:\n");
        DISPTREERANGE(BlockRange(), op1.Def());
    }

    if (!gtLong->gtOp.gtOp2->OperIsLeaf())
    {
        LIR::Use op2(BlockRange(), &gtLong->gtOp.gtOp2, gtLong);
        op2.ReplaceWithLclVar(m_compiler, blockWeight);
        JITDUMP("[DecomposeStoreInd]: Saving high data tree to a temp var:\n");
        DISPTREERANGE(BlockRange(), op2.Def());
    }

    GenTree* addrBase    = tree->gtOp.gtOp1;
    GenTree* dataHigh    = gtLong->gtOp.gtOp2;
    GenTree* dataLow     = gtLong->gtOp.gtOp1;
    GenTree* storeIndLow = tree;

    BlockRange().Remove(gtLong);
    BlockRange().Remove(dataHigh);
    storeIndLow->gtOp.gtOp2 = dataLow;
    storeIndLow->gtType     = TYP_INT;

    GenTree* addrBaseHigh = new (m_compiler, GT_LCL_VAR)
        GenTreeLclVar(GT_LCL_VAR, addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);
    GenTree* addrHigh =
        new (m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTree* storeIndHigh = new (m_compiler, GT_STOREIND) GenTreeStoreInd(TYP_INT, addrHigh, dataHigh);
    storeIndHigh->gtFlags = (storeIndLow->gtFlags & (GTF_ALL_EFFECT | GTF_LIVENESS_MASK));
    storeIndHigh->gtFlags |= GTF_REVERSE_OPS;

    m_compiler->lvaIncRefCnts(addrBaseHigh);

    BlockRange().InsertAfter(storeIndLow, dataHigh, addrBaseHigh, addrHigh, storeIndHigh);

    return storeIndHigh;

    // Example final output:
    //
    //      /--*  t52    byref
    //      *  st.lclVar byref  V07 rat0
    // t158 = lclVar    byref  V07 rat0
    //  t51 = const     int    0x37C05E7D
    //      /--*  t158   byref
    //      +--*  t51    int
    //      *  storeIndir int
    // t154 = const     int    0x2A0A3C80
    // t159 = lclVar    byref  V07 rat0
    //        /--*  t159   byref
    // t160 = *  lea(b + 4)  ref
    //      /--*  t154   int
    //      +--*  t160   ref
    //      *  storeIndir int
}

//------------------------------------------------------------------------
// DecomposeInd: Decompose GT_IND.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeInd(LIR::Use& use)
{
    GenTree* indLow = use.Def();

    LIR::Use address(BlockRange(), &indLow->gtOp.gtOp1, indLow);
    address.ReplaceWithLclVar(m_compiler, m_block->getBBWeight(m_compiler));
    JITDUMP("[DecomposeInd]: Saving addr tree to a temp var:\n");
    DISPTREERANGE(BlockRange(), address.Def());

    // Change the type of lower ind.
    indLow->gtType = TYP_INT;

    // Create tree of ind(addr+4)
    GenTreePtr addrBase     = indLow->gtGetOp1();
    GenTreePtr addrBaseHigh = new (m_compiler, GT_LCL_VAR)
        GenTreeLclVar(GT_LCL_VAR, addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum(), BAD_IL_OFFSET);
    GenTreePtr addrHigh =
        new (m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTreePtr indHigh = new (m_compiler, GT_IND) GenTreeIndir(GT_IND, TYP_INT, addrHigh, nullptr);

    m_compiler->lvaIncRefCnts(addrBaseHigh);

    BlockRange().InsertAfter(indLow, addrBaseHigh, addrHigh, indHigh);

    return FinalizeDecomposition(use, indLow, indHigh);
}

//------------------------------------------------------------------------
// DecomposeNot: Decompose GT_NOT.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeNot(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_NOT);

    GenTree* tree   = use.Def();
    GenTree* gtLong = tree->gtGetOp1();
    noway_assert(gtLong->OperGet() == GT_LONG);
    GenTree* loOp1 = gtLong->gtGetOp1();
    GenTree* hiOp1 = gtLong->gtGetOp2();

    BlockRange().Remove(gtLong);

    GenTree* loResult    = tree;
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;

    GenTree* hiResult = new (m_compiler, GT_NOT) GenTreeOp(GT_NOT, TYP_INT, hiOp1, nullptr);
    BlockRange().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeNeg: Decompose GT_NEG.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeNeg(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_NEG);

    GenTree* tree   = use.Def();
    GenTree* gtLong = tree->gtGetOp1();
    noway_assert(gtLong->OperGet() == GT_LONG);

    unsigned blockWeight = m_block->getBBWeight(m_compiler);

    LIR::Use op1(BlockRange(), &gtLong->gtOp.gtOp1, gtLong);
    op1.ReplaceWithLclVar(m_compiler, blockWeight);

    LIR::Use op2(BlockRange(), &gtLong->gtOp.gtOp2, gtLong);
    op2.ReplaceWithLclVar(m_compiler, blockWeight);

    // Neither GT_NEG nor the introduced temporaries have side effects.
    tree->gtFlags &= ~GTF_ALL_EFFECT;
    GenTree* loOp1 = gtLong->gtGetOp1();
    GenTree* hiOp1 = gtLong->gtGetOp2();

    BlockRange().Remove(gtLong);

    GenTree* loResult    = tree;
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;

    GenTree* zero     = m_compiler->gtNewZeroConNode(TYP_INT);
    GenTree* hiAdjust = m_compiler->gtNewOperNode(GT_ADD_HI, TYP_INT, hiOp1, zero);
    GenTree* hiResult = m_compiler->gtNewOperNode(GT_NEG, TYP_INT, hiAdjust);
    hiResult->gtFlags = tree->gtFlags;

    BlockRange().InsertAfter(loResult, zero, hiAdjust, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeArith: Decompose GT_ADD, GT_SUB, GT_OR, GT_XOR, GT_AND.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeArith(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTree*   tree = use.Def();
    genTreeOps oper = tree->OperGet();

    assert((oper == GT_ADD) || (oper == GT_SUB) || (oper == GT_OR) || (oper == GT_XOR) || (oper == GT_AND));

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

    NYI_IF(hiOp1->OperIsHigh() || hiOp2->OperIsHigh() ||
               (GenTree::OperIsHigh(GetHiOper(oper)) &&
                (!loOp1->OperIsLeaf() || !hiOp1->OperIsLeaf() || !loOp1->OperIsLeaf() || !hiOp2->OperIsLeaf())),
           "Can't decompose expression tree TYP_LONG node");

    // Now, remove op1 and op2 from the node list.
    BlockRange().Remove(op1);
    BlockRange().Remove(op2);

    // We will reuse "tree" for the loResult, which will now be of TYP_INT, and its operands
    // will be the lo halves of op1 from above.
    GenTree* loResult = tree;
    loResult->SetOper(GetLoOper(loResult->OperGet()));
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;
    loResult->gtOp.gtOp2 = loOp2;

    GenTree* hiResult = new (m_compiler, oper) GenTreeOp(GetHiOper(oper), TYP_INT, hiOp1, hiOp2);
    BlockRange().InsertAfter(loResult, hiResult);

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

    return FinalizeDecomposition(use, loResult, hiResult);
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
        case GT_ADD:
            return GT_ADD_HI;
            break;
        case GT_SUB:
            return GT_SUB_HI;
            break;
        case GT_MUL:
            return GT_MUL_HI;
            break;
        case GT_DIV:
            return GT_DIV_HI;
            break;
        case GT_MOD:
            return GT_MOD_HI;
            break;
        case GT_OR:
            return GT_OR;
            break;
        case GT_AND:
            return GT_AND;
            break;
        case GT_XOR:
            return GT_XOR;
            break;
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
        case GT_ADD:
            return GT_ADD_LO;
            break;
        case GT_SUB:
            return GT_SUB_LO;
            break;
        case GT_OR:
            return GT_OR;
            break;
        case GT_AND:
            return GT_AND;
            break;
        case GT_XOR:
            return GT_XOR;
            break;
        default:
            assert(!"GetLoOper called for invalid oper");
            return GT_NONE;
    }
}

#endif // !_TARGET_64BIT_
#endif // !LEGACY_BACKEND
