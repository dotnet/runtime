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
// DecomposeLongs::PrepareForDecomposition:
//    Do one-time preparation required for LONG decomposition. Namely,
//    promote long variables to multi-register structs.
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
// DecomposeLongs::DecomposeBlock:
//    Do LONG decomposition on all the nodes in the given block. This must
//    be done before lowering the block, as decomposition can insert
//    additional nodes.
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

    m_blockWeight = block->getBBWeight(m_compiler);
    m_range = &LIR::AsRange(block);
    DecomposeRangeHelper();
}

//------------------------------------------------------------------------
// DecomposeLongs::DecomposeRange:
//    Do LONG decomposition on all the nodes in the given range. This must
//    be done before inserting a range of un-decomposed IR into a block
//    that has already been decomposed.
//
// Arguments:
//    compiler    - The compiler context.
//    blockWeight - The weight of the block into which the range will be
//                  inserted.
//    range       - The range to decompose.
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeRange(Compiler* compiler, unsigned blockWeight, LIR::Range& range)
{
    assert(compiler != nullptr);

    DecomposeLongs decomposer(compiler);
    decomposer.m_blockWeight = blockWeight;
    decomposer.m_range = &range;

    decomposer.DecomposeRangeHelper();
}

//------------------------------------------------------------------------
// DecomposeLongs::DecomposeRangeHelper:
//    Decompiose each node in the current range.
//
//    Decomposition is done as an execution-order walk. Decomposition of
//    a particular node can create new nodes that need to be further
//    decomposed at higher levels. That is, decomposition "bubbles up"
//    through dataflow.
//
void DecomposeLongs::DecomposeRangeHelper()
{
    assert(m_range != nullptr);

    GenTree* node = Range().FirstNonPhiNode();
    while (node != nullptr)
    {
        node = DecomposeNode(node);
    }

    assert(Range().CheckLIR(m_compiler));
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
GenTree* DecomposeLongs::DecomposeNode(GenTree* tree)
{
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
                m_compiler->gtDispTreeRange(Range(), tree);
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
        m_compiler->gtDispTreeRange(Range(), tree);
    }
#endif // DEBUG

    LIR::Use use;
    if (!Range().TryGetUse(tree, &use))
    {
        use = LIR::Use::GetDummyUse(Range(), tree);
    }

    GenTree* nextNode = nullptr;
    switch (tree->OperGet())
    {
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
            nextNode = DecomposeMul(use);
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
            nextNode = DecomposeShift(use);
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
        m_compiler->gtDispTreeRange(Range(), use.Def());
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
    assert(Range().Contains(loResult));
    assert(Range().Contains(hiResult));
    assert(loResult->Precedes(hiResult));

    GenTree* gtLong = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
    Range().InsertAfter(hiResult, gtLong);

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
    Range().InsertAfter(loResult, hiResult);

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
    Range().InsertAfter(loResult, hiResult);

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
    if ((rhs->OperGet() == GT_PHI) || (rhs->OperGet() == GT_CALL) ||
            ((rhs->OperGet() == GT_MUL_LONG) && (rhs->gtFlags & GTF_MUL_64RSLT) != 0))
    {
        // GT_CALLs are not decomposed, so will not be converted to GT_LONG
        // GT_STORE_LCL_VAR = GT_CALL are handled in genMultiRegCallStoreToLocal
        // GT_MULs are not decomposed, so will not be converted to GT_LONG
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
    Range().Remove(rhs);

    tree->gtOp.gtOp1 = loRhs;
    tree->gtType     = TYP_INT;

    hiStore->gtOp.gtOp1 = hiRhs;
    hiStore->gtFlags |= GTF_VAR_DEF;

    m_compiler->lvaIncRefCnts(tree);
    m_compiler->lvaIncRefCnts(hiStore);

    Range().InsertAfter(tree, hiStore);

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

    GenTree* cast     = use.Def()->AsCast();
    GenTree* loResult = nullptr;
    GenTree* hiResult = nullptr;

    var_types srcType = cast->CastFromType();
    var_types dstType = cast->CastToType();

    if ((cast->gtFlags & GTF_UNSIGNED) != 0)
    {
        srcType = genUnsignedType(srcType);
    }

    if (varTypeIsLong(srcType))
    {
        if (cast->gtOverflow() && (varTypeIsUnsigned(srcType) != varTypeIsUnsigned(dstType)))
        {
            GenTree* srcOp = cast->gtGetOp1();
            noway_assert(srcOp->OperGet() == GT_LONG);
            GenTree* loSrcOp = srcOp->gtGetOp1();
            GenTree* hiSrcOp = srcOp->gtGetOp2();

            //
            // When casting between long types an overflow check is needed only if the types
            // have different signedness. In both cases (long->ulong and ulong->long) we only
            // need to check if the high part is negative or not. Use the existing cast node
            // to perform a int->uint cast of the high part to take advantage of the overflow
            // check provided by codegen.
            //

            loResult = loSrcOp;

            hiResult                       = cast;
            hiResult->gtType               = TYP_INT;
            hiResult->AsCast()->gtCastType = TYP_UINT;
            hiResult->gtFlags &= ~GTF_UNSIGNED;
            hiResult->gtOp.gtOp1 = hiSrcOp;

            Range().Remove(cast);
            Range().Remove(srcOp);
            Range().InsertAfter(hiSrcOp, hiResult);
        }
        else
        {
            NYI("Unimplemented long->long no-op cast decomposition");
        }
    }
    else if (varTypeIsIntegralOrI(srcType))
    {
        if (cast->gtOverflow() && !varTypeIsUnsigned(srcType) && varTypeIsUnsigned(dstType))
        {
            //
            // An overflow check is needed only when casting from a signed type to ulong.
            // Change the cast type to uint to take advantage of the overflow check provided
            // by codegen and then zero extend the resulting uint to ulong.
            //

            loResult                       = cast;
            loResult->AsCast()->gtCastType = TYP_UINT;
            loResult->gtType               = TYP_INT;

            hiResult = m_compiler->gtNewZeroConNode(TYP_INT);

            Range().InsertAfter(loResult, hiResult);
        }
        else
        {
            if (varTypeIsUnsigned(srcType))
            {
                loResult = cast->gtGetOp1();
                hiResult = m_compiler->gtNewZeroConNode(TYP_INT);

                Range().Remove(cast);
                Range().InsertAfter(loResult, hiResult);
            }
            else
            {
                LIR::Use src(Range(), &(cast->gtOp.gtOp1), cast);
                unsigned lclNum = src.ReplaceWithLclVar(m_compiler, m_blockWeight);

                loResult = src.Def();

                GenTree* loCopy  = m_compiler->gtNewLclvNode(lclNum, TYP_INT);
                GenTree* shiftBy = m_compiler->gtNewIconNode(31, TYP_INT);
                hiResult         = m_compiler->gtNewOperNode(GT_RSH, TYP_INT, loCopy, shiftBy);

                Range().Remove(cast);
                Range().InsertAfter(loResult, loCopy, shiftBy, hiResult);
                m_compiler->lvaIncRefCnts(loCopy);
            }
        }
    }
    else
    {
        NYI("Unimplemented cast decomposition");
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
    Range().InsertAfter(loResult, hiResult);

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
    return StoreNodeToVar(use);
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

    // Save address to a temp. It is used in storeIndLow and storeIndHigh trees.
    LIR::Use address(Range(), &tree->gtOp.gtOp1, tree);
    address.ReplaceWithLclVar(m_compiler, m_blockWeight);
    JITDUMP("[DecomposeStoreInd]: Saving address tree to a temp var:\n");
    DISPTREERANGE(Range(), address.Def());

    if (!gtLong->gtOp.gtOp1->OperIsLeaf())
    {
        LIR::Use op1(Range(), &gtLong->gtOp.gtOp1, gtLong);
        op1.ReplaceWithLclVar(m_compiler, m_blockWeight);
        JITDUMP("[DecomposeStoreInd]: Saving low data tree to a temp var:\n");
        DISPTREERANGE(Range(), op1.Def());
    }

    if (!gtLong->gtOp.gtOp2->OperIsLeaf())
    {
        LIR::Use op2(Range(), &gtLong->gtOp.gtOp2, gtLong);
        op2.ReplaceWithLclVar(m_compiler, m_blockWeight);
        JITDUMP("[DecomposeStoreInd]: Saving high data tree to a temp var:\n");
        DISPTREERANGE(Range(), op2.Def());
    }

    GenTree* addrBase    = tree->gtOp.gtOp1;
    GenTree* dataHigh    = gtLong->gtOp.gtOp2;
    GenTree* dataLow     = gtLong->gtOp.gtOp1;
    GenTree* storeIndLow = tree;

    Range().Remove(gtLong);
    Range().Remove(dataHigh);
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

    Range().InsertAfter(storeIndLow, dataHigh, addrBaseHigh, addrHigh, storeIndHigh);

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

    LIR::Use address(Range(), &indLow->gtOp.gtOp1, indLow);
    address.ReplaceWithLclVar(m_compiler, m_blockWeight);
    JITDUMP("[DecomposeInd]: Saving addr tree to a temp var:\n");
    DISPTREERANGE(Range(), address.Def());

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

    Range().InsertAfter(indLow, addrBaseHigh, addrHigh, indHigh);

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

    Range().Remove(gtLong);

    GenTree* loResult    = tree;
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;

    GenTree* hiResult = new (m_compiler, GT_NOT) GenTreeOp(GT_NOT, TYP_INT, hiOp1, nullptr);
    Range().InsertAfter(loResult, hiResult);

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

    LIR::Use op1(Range(), &gtLong->gtOp.gtOp1, gtLong);
    op1.ReplaceWithLclVar(m_compiler, m_blockWeight);

    LIR::Use op2(Range(), &gtLong->gtOp.gtOp2, gtLong);
    op2.ReplaceWithLclVar(m_compiler, m_blockWeight);

    // Neither GT_NEG nor the introduced temporaries have side effects.
    tree->gtFlags &= ~GTF_ALL_EFFECT;
    GenTree* loOp1 = gtLong->gtGetOp1();
    GenTree* hiOp1 = gtLong->gtGetOp2();

    Range().Remove(gtLong);

    GenTree* loResult    = tree;
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;

    GenTree* zero     = m_compiler->gtNewZeroConNode(TYP_INT);
    GenTree* hiAdjust = m_compiler->gtNewOperNode(GT_ADD_HI, TYP_INT, hiOp1, zero);
    GenTree* hiResult = m_compiler->gtNewOperNode(GT_NEG, TYP_INT, hiAdjust);
    hiResult->gtFlags = tree->gtFlags;

    Range().InsertAfter(loResult, zero, hiAdjust, hiResult);

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

    // Now, remove op1 and op2 from the node list.
    Range().Remove(op1);
    Range().Remove(op2);

    // We will reuse "tree" for the loResult, which will now be of TYP_INT, and its operands
    // will be the lo halves of op1 from above.
    GenTree* loResult = tree;
    loResult->SetOper(GetLoOper(oper));
    loResult->gtType     = TYP_INT;
    loResult->gtOp.gtOp1 = loOp1;
    loResult->gtOp.gtOp2 = loOp2;

    GenTree* hiResult = new (m_compiler, oper) GenTreeOp(GetHiOper(oper), TYP_INT, hiOp1, hiOp2);
    Range().InsertAfter(loResult, hiResult);

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
// DecomposeShift: Decompose GT_LSH, GT_RSH, GT_RSZ. For shift nodes, we need to use
// the shift helper functions, so we here convert the shift into a helper call by
// pulling its arguments out of linear order and making them the args to a call, then
// replacing the original node with the new call.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeShift(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTree* tree   = use.Def();
    GenTree* gtLong = tree->gtGetOp1();
    genTreeOps oper = tree->OperGet();

    assert((oper == GT_LSH) || (oper == GT_RSH) || (oper == GT_RSZ));

    LIR::Use loOp1Use(Range(), &gtLong->gtOp.gtOp1, gtLong);
    loOp1Use.ReplaceWithLclVar(m_compiler, m_blockWeight);

    LIR::Use hiOp1Use(Range(), &gtLong->gtOp.gtOp2, gtLong);
    hiOp1Use.ReplaceWithLclVar(m_compiler, m_blockWeight);

    LIR::Use shiftWidthUse(Range(), &tree->gtOp.gtOp2, tree);
    shiftWidthUse.ReplaceWithLclVar(m_compiler, m_blockWeight);

    GenTree* loOp1 = gtLong->gtGetOp1();
    GenTree* hiOp1 = gtLong->gtGetOp2();

    GenTree* shiftWidthOp = tree->gtGetOp2();

    Range().Remove(gtLong);
    Range().Remove(loOp1);
    Range().Remove(hiOp1);

    Range().Remove(shiftWidthOp);

    // TODO-X86-CQ: If the shift operand is a GT_CNS_INT, we should pipe the instructions through to codegen
    // and generate the shift instructions ourselves there, rather than replacing it with a helper call.

    unsigned helper;

    switch (oper)
    {
        case GT_LSH:
            helper = CORINFO_HELP_LLSH;
            break;
        case GT_RSH:
            helper = CORINFO_HELP_LRSH;
            break;
        case GT_RSZ:
            helper = CORINFO_HELP_LRSZ;
            break;
        default:
            unreached();
    }

    GenTreeArgList* argList = m_compiler->gtNewArgList(loOp1, hiOp1, shiftWidthOp);

    GenTree* call = m_compiler->gtNewHelperCallNode(helper, TYP_LONG, 0, argList);

    GenTreeCall*    callNode    = call->AsCall();
    ReturnTypeDesc* retTypeDesc = callNode->GetReturnTypeDesc();
    retTypeDesc->InitializeLongReturnType(m_compiler);

    call = m_compiler->fgMorphArgs(callNode);
    Range().InsertAfter(tree, LIR::SeqTree(m_compiler, call));
    
    Range().Remove(tree);
    use.ReplaceWith(m_compiler, call);
    return call;
}

//------------------------------------------------------------------------
// DecomposeMul: Decompose GT_MUL. The only GT_MULs that make it to decompose are
// those with the GTF_MUL_64RSLT flag set. These muls result in a mul instruction that
// returns its result in two registers like GT_CALLs do. Additionally, these muls are
// guaranteed to be in the form long = (long)int * (long)int. Therefore, to decompose
// these nodes, we convert them into GT_MUL_LONGs, undo the cast from int to long by
// stripping out the lo ops, and force them into the form var = mul, as we do for
// GT_CALLs. In codegen, we then produce a mul instruction that produces the result
// in edx:eax, and store those registers on the stack in genStoreLongLclVar.
//
// All other GT_MULs have been converted to helper calls in morph.cpp
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeMul(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTree*   tree = use.Def();
    genTreeOps oper = tree->OperGet();

    assert(oper == GT_MUL);
    assert((tree->gtFlags & GTF_MUL_64RSLT) != 0);

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    GenTree* loOp1 = op1->gtGetOp1();
    GenTree* hiOp1 = op1->gtGetOp2();
    GenTree* loOp2 = op2->gtGetOp1();
    GenTree* hiOp2 = op2->gtGetOp2();

    Range().Remove(hiOp1);
    Range().Remove(hiOp2);
    Range().Remove(op1);
    Range().Remove(op2);

    // Get rid of the hi ops. We don't need them.
    tree->gtOp.gtOp1 = loOp1;
    tree->gtOp.gtOp2 = loOp2;
    tree->gtOper = GT_MUL_LONG;

    return StoreNodeToVar(use);
}

//------------------------------------------------------------------------
// StoreNodeToVar: Check if the user is a STORE_LCL_VAR, and if it isn't,
// store the node to a var. Then decompose the new LclVar.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::StoreNodeToVar(LIR::Use& use)
{
    if (use.IsDummyUse())
        return use.Def()->gtNext;

    GenTree* tree = use.Def();
    GenTree* user = use.User();

    if (user->OperGet() == GT_STORE_LCL_VAR)
    {
        // If parent is already a STORE_LCL_VAR, we can skip it if
        // it is already marked as lvIsMultiRegRet.
        unsigned varNum = user->AsLclVarCommon()->gtLclNum;
        if (m_compiler->lvaTable[varNum].lvIsMultiRegRet)
        {
            return tree->gtNext;
        }
        else if (!m_compiler->lvaTable[varNum].lvPromoted)
        {
            // If var wasn't promoted, we can just set lvIsMultiRegRet.
            m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;
            return tree->gtNext;
        }
    }

    // Otherwise, we need to force var = call()
    unsigned varNum                              = use.ReplaceWithLclVar(m_compiler, m_blockWeight);
    m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;

    // Decompose the new LclVar use
    return DecomposeLclVar(use);
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
