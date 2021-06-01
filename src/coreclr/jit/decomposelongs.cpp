// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifndef TARGET_64BIT // DecomposeLongs is only used on 32-bit platforms

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
//    range       - The range to decompose.
//
// Return Value:
//    None.
//
void DecomposeLongs::DecomposeRange(Compiler* compiler, LIR::Range& range)
{
    assert(compiler != nullptr);

    DecomposeLongs decomposer(compiler);
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

    assert(Range().CheckLIR(m_compiler, true));
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
        LclVarDsc* varDsc = m_compiler->lvaTable + tree->AsLclVarCommon()->GetLclNum();
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
            unsigned loVarNum = varDsc->lvFieldLclStart;
            tree->AsLclVarCommon()->SetLclNum(loVarNum);
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
            assert(tree->AsOp()->gtOp1->OperGet() == GT_LONG);
            break;

        case GT_STOREIND:
            nextNode = DecomposeStoreInd(use);
            break;

        case GT_STORE_LCL_FLD:
            nextNode = DecomposeStoreLclFld(use);
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

        case GT_UMOD:
            nextNode = DecomposeUMod(use);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
            nextNode = DecomposeShift(use);
            break;

        case GT_ROL:
        case GT_ROR:
            nextNode = DecomposeRotate(use);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            nextNode = DecomposeSimd(use);
            break;
#endif // FEATURE_SIMD

        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
            NYI("Interlocked operations on TYP_LONG");
            break;

        default:
        {
            JITDUMP("Illegal TYP_LONG node %s in Decomposition.", GenTree::OpName(tree->OperGet()));
            assert(!"Illegal TYP_LONG node in Decomposition.");
            break;
        }
    }

    // If we replaced the argument to a GT_FIELD_LIST element with a GT_LONG node, split that field list
    // element into two elements: one for each half of the GT_LONG.
    if ((use.Def()->OperGet() == GT_LONG) && !use.IsDummyUse() && (use.User()->OperGet() == GT_FIELD_LIST))
    {
        DecomposeFieldList(use.User()->AsFieldList(), use.Def()->AsOp());
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
//    hiResult - the decomposed high part
//    insertResultAfter - the node that the GT_LONG should be inserted after
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::FinalizeDecomposition(LIR::Use& use,
                                               GenTree*  loResult,
                                               GenTree*  hiResult,
                                               GenTree*  insertResultAfter)
{
    assert(use.IsInitialized());
    assert(loResult != nullptr);
    assert(hiResult != nullptr);
    assert(Range().Contains(loResult));
    assert(Range().Contains(hiResult));

    GenTree* gtLong = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loResult, hiResult);
    if (use.IsDummyUse())
    {
        gtLong->SetUnusedValue();
    }

    loResult->ClearUnusedValue();
    hiResult->ClearUnusedValue();

    Range().InsertAfter(insertResultAfter, gtLong);

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

    GenTree*   tree     = use.Def();
    unsigned   varNum   = tree->AsLclVarCommon()->GetLclNum();
    LclVarDsc* varDsc   = m_compiler->lvaTable + varNum;
    GenTree*   loResult = tree;
    loResult->gtType    = TYP_INT;

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
        loResult->SetOper(GT_LCL_FLD);
        loResult->AsLclFld()->SetLclOffs(0);
        loResult->AsLclFld()->SetFieldSeq(FieldSeqStore::NotAField());

        hiResult->SetOper(GT_LCL_FLD);
        hiResult->AsLclFld()->SetLclOffs(4);
        hiResult->AsLclFld()->SetFieldSeq(FieldSeqStore::NotAField());
    }

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
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

    GenTree* hiResult = m_compiler->gtNewLclFldNode(loResult->GetLclNum(), TYP_INT, loResult->GetLclOffs() + 4);
    Range().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
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

    unsigned   varNum = tree->AsLclVarCommon()->GetLclNum();
    LclVarDsc* varDsc = m_compiler->lvaTable + varNum;
    if (!varDsc->lvPromoted)
    {
        // We cannot decompose a st.lclVar that is not promoted because doing so
        // changes its liveness semantics. For example, consider the following
        // decomposition of a st.lclVar into two st.lclFlds:
        //
        // Before:
        //
        //          /--* t0      int
        //          +--* t1      int
        //     t2 = *  gt_long   long
        //
        //          /--* t2      long
        //          *  st.lclVar long    V0
        //
        // After:
        //          /--* t0      int
        //          *  st.lclFld int     V0    [+0]
        //
        //          /--* t1      int
        //          *  st.lclFld int     V0    [+4]
        //
        // Before decomposition, the `st.lclVar` is a simple def of `V0`. After
        // decomposition, each `st.lclFld` is a partial def of `V0`. This partial
        // def is treated as both a use and a def of the appropriate lclVar. This
        // difference will affect any situation in which the liveness of a variable
        // at a def matters (e.g. dead store elimination, live-in sets, etc.). As
        // a result, we leave these stores as-is and generate the decomposed store
        // in the code generator.
        //
        // NOTE: this does extend the lifetime of the low half of the `GT_LONG`
        // node as compared to the decomposed form. If we start doing more code
        // motion in the backend, this may cause some CQ issues and some sort of
        // decomposition could be beneficial.
        return tree->gtNext;
    }

    assert(varDsc->lvFieldCnt == 2);
    GenTreeOp* value = rhs->AsOp();
    Range().Remove(value);

    const unsigned loVarNum = varDsc->lvFieldLclStart;
    GenTree*       loStore  = tree;
    loStore->AsLclVarCommon()->SetLclNum(loVarNum);
    loStore->AsOp()->gtOp1 = value->gtOp1;
    loStore->gtType        = TYP_INT;

    const unsigned hiVarNum = loVarNum + 1;
    GenTree*       hiStore  = m_compiler->gtNewLclLNode(hiVarNum, TYP_INT);
    hiStore->SetOper(GT_STORE_LCL_VAR);
    hiStore->AsOp()->gtOp1 = value->gtOp2;
    hiStore->gtFlags |= GTF_VAR_DEF;

    Range().InsertAfter(tree, hiStore);

    return hiStore->gtNext;
}

//------------------------------------------------------------------------
// DecomposeStoreLclFld: Decompose GT_STORE_LCL_FLD.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeStoreLclFld(LIR::Use& use)
{
    assert(use.IsInitialized());
    assert(use.Def()->OperGet() == GT_STORE_LCL_FLD);

    GenTreeLclFld* store = use.Def()->AsLclFld();

    GenTreeOp* value = store->gtOp1->AsOp();
    assert(value->OperGet() == GT_LONG);
    Range().Remove(value);

    // The original store node will be repurposed to store the low half of the GT_LONG.
    GenTreeLclFld* loStore = store;
    loStore->gtOp1         = value->gtOp1;
    loStore->gtType        = TYP_INT;
    loStore->gtFlags |= GTF_VAR_USEASG;

    // Create the store for the upper half of the GT_LONG and insert it after the low store.
    GenTreeLclFld* hiStore = m_compiler->gtNewLclFldNode(loStore->GetLclNum(), TYP_INT, loStore->GetLclOffs() + 4);
    hiStore->SetOper(GT_STORE_LCL_FLD);
    hiStore->gtOp1 = value->gtOp2;
    hiStore->gtFlags |= (GTF_VAR_DEF | GTF_VAR_USEASG);

    Range().InsertAfter(loStore, hiStore);

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

    bool skipDecomposition = false;

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

            const bool signExtend = (cast->gtFlags & GTF_UNSIGNED) == 0;
            loResult              = EnsureIntSized(loSrcOp, signExtend);

            hiResult                       = cast;
            hiResult->gtType               = TYP_INT;
            hiResult->AsCast()->gtCastType = TYP_UINT;
            hiResult->gtFlags &= ~GTF_UNSIGNED;
            hiResult->AsOp()->gtOp1 = hiSrcOp;

            Range().Remove(srcOp);
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
            if (!use.IsDummyUse() && (use.User()->OperGet() == GT_MUL))
            {
                //
                // This int->long cast is used by a GT_MUL that will be transformed by DecomposeMul into a
                // GT_LONG_MUL and as a result the high operand produced by the cast will become dead.
                // Skip cast decomposition so DecomposeMul doesn't need to bother with dead code removal,
                // especially in the case of sign extending casts that also introduce new lclvars.
                //

                assert((use.User()->gtFlags & GTF_MUL_64RSLT) != 0);

                skipDecomposition = true;
            }
            else if (varTypeIsUnsigned(srcType))
            {
                const bool signExtend = (cast->gtFlags & GTF_UNSIGNED) == 0;
                loResult              = EnsureIntSized(cast->gtGetOp1(), signExtend);

                hiResult = m_compiler->gtNewZeroConNode(TYP_INT);

                Range().InsertAfter(cast, hiResult);
                Range().Remove(cast);
            }
            else
            {
                LIR::Use src(Range(), &(cast->AsOp()->gtOp1), cast);
                unsigned lclNum = src.ReplaceWithLclVar(m_compiler);

                loResult = src.Def();

                GenTree* loCopy  = m_compiler->gtNewLclvNode(lclNum, TYP_INT);
                GenTree* shiftBy = m_compiler->gtNewIconNode(31, TYP_INT);
                hiResult         = m_compiler->gtNewOperNode(GT_RSH, TYP_INT, loCopy, shiftBy);

                Range().InsertAfter(cast, loCopy, shiftBy, hiResult);
                Range().Remove(cast);
            }
        }
    }
    else
    {
        NYI("Unimplemented cast decomposition");
    }

    if (skipDecomposition)
    {
        return cast->gtNext;
    }

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
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

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeFieldList: Decompose GT_FIELD_LIST.
//
// Arguments:
//    fieldList - the GT_FIELD_LIST node that uses the given GT_LONG node.
//    longNode - the node to decompose
//
// Return Value:
//    The next node to process.
//
// Notes:
//    Split a LONG field list element into two elements: one for each half of the GT_LONG.
//
GenTree* DecomposeLongs::DecomposeFieldList(GenTreeFieldList* fieldList, GenTreeOp* longNode)
{
    assert(longNode->OperGet() == GT_LONG);

    GenTreeFieldList::Use* loUse = nullptr;
    for (GenTreeFieldList::Use& use : fieldList->Uses())
    {
        if (use.GetNode() == longNode)
        {
            loUse = &use;
            break;
        }
    }
    assert(loUse != nullptr);

    Range().Remove(longNode);

    loUse->SetNode(longNode->gtGetOp1());
    loUse->SetType(TYP_INT);

    fieldList->InsertFieldLIR(m_compiler, loUse, longNode->gtGetOp2(), loUse->GetOffset() + 4, TYP_INT);

    return fieldList->gtNext;
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

    assert(tree->AsOp()->gtOp2->OperGet() == GT_LONG);

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

    GenTree* gtLong = tree->AsOp()->gtOp2;

    // Save address to a temp. It is used in storeIndLow and storeIndHigh trees.
    LIR::Use address(Range(), &tree->AsOp()->gtOp1, tree);
    address.ReplaceWithLclVar(m_compiler);
    JITDUMP("[DecomposeStoreInd]: Saving address tree to a temp var:\n");
    DISPTREERANGE(Range(), address.Def());

    if (!gtLong->AsOp()->gtOp1->OperIsLeaf())
    {
        LIR::Use op1(Range(), &gtLong->AsOp()->gtOp1, gtLong);
        op1.ReplaceWithLclVar(m_compiler);
        JITDUMP("[DecomposeStoreInd]: Saving low data tree to a temp var:\n");
        DISPTREERANGE(Range(), op1.Def());
    }

    if (!gtLong->AsOp()->gtOp2->OperIsLeaf())
    {
        LIR::Use op2(Range(), &gtLong->AsOp()->gtOp2, gtLong);
        op2.ReplaceWithLclVar(m_compiler);
        JITDUMP("[DecomposeStoreInd]: Saving high data tree to a temp var:\n");
        DISPTREERANGE(Range(), op2.Def());
    }

    GenTree* addrBase    = tree->AsOp()->gtOp1;
    GenTree* dataHigh    = gtLong->AsOp()->gtOp2;
    GenTree* dataLow     = gtLong->AsOp()->gtOp1;
    GenTree* storeIndLow = tree;

    Range().Remove(gtLong);
    Range().Remove(dataHigh);
    storeIndLow->AsOp()->gtOp2 = dataLow;
    storeIndLow->gtType        = TYP_INT;

    GenTree* addrBaseHigh = new (m_compiler, GT_LCL_VAR)
        GenTreeLclVar(GT_LCL_VAR, addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum());
    GenTree* addrHigh =
        new (m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTree* storeIndHigh = new (m_compiler, GT_STOREIND) GenTreeStoreInd(TYP_INT, addrHigh, dataHigh);
    storeIndHigh->gtFlags = (storeIndLow->gtFlags & (GTF_ALL_EFFECT | GTF_LIVENESS_MASK));

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

    LIR::Use address(Range(), &indLow->AsOp()->gtOp1, indLow);
    address.ReplaceWithLclVar(m_compiler);
    JITDUMP("[DecomposeInd]: Saving addr tree to a temp var:\n");
    DISPTREERANGE(Range(), address.Def());

    // Change the type of lower ind.
    indLow->gtType = TYP_INT;

    // Create tree of ind(addr+4)
    GenTree* addrBase     = indLow->gtGetOp1();
    GenTree* addrBaseHigh = new (m_compiler, GT_LCL_VAR)
        GenTreeLclVar(GT_LCL_VAR, addrBase->TypeGet(), addrBase->AsLclVarCommon()->GetLclNum());
    GenTree* addrHigh =
        new (m_compiler, GT_LEA) GenTreeAddrMode(TYP_REF, addrBaseHigh, nullptr, 0, genTypeSize(TYP_INT));
    GenTree* indHigh = new (m_compiler, GT_IND) GenTreeIndir(GT_IND, TYP_INT, addrHigh, nullptr);
    indHigh->gtFlags |= (indLow->gtFlags & (GTF_GLOB_REF | GTF_EXCEPT | GTF_IND_FLAGS));

    Range().InsertAfter(indLow, addrBaseHigh, addrHigh, indHigh);

    return FinalizeDecomposition(use, indLow, indHigh, indHigh);
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

    GenTree* loResult       = tree;
    loResult->gtType        = TYP_INT;
    loResult->AsOp()->gtOp1 = loOp1;

    GenTree* hiResult = new (m_compiler, GT_NOT) GenTreeOp(GT_NOT, TYP_INT, hiOp1, nullptr);
    Range().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
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

    GenTree* loOp1 = gtLong->gtGetOp1();
    GenTree* hiOp1 = gtLong->gtGetOp2();

    Range().Remove(gtLong);

    GenTree* loResult       = tree;
    loResult->gtType        = TYP_INT;
    loResult->AsOp()->gtOp1 = loOp1;

    GenTree* zero = m_compiler->gtNewZeroConNode(TYP_INT);

#if defined(TARGET_X86)

    GenTree* hiAdjust = m_compiler->gtNewOperNode(GT_ADD_HI, TYP_INT, hiOp1, zero);
    GenTree* hiResult = m_compiler->gtNewOperNode(GT_NEG, TYP_INT, hiAdjust);
    Range().InsertAfter(loResult, zero, hiAdjust, hiResult);

    loResult->gtFlags |= GTF_SET_FLAGS;
    hiAdjust->gtFlags |= GTF_USE_FLAGS;

#elif defined(TARGET_ARM)

    // We tend to use "movs" to load zero to a register, and that sets the flags, so put the
    // zero before the loResult, which is setting the flags needed by GT_SUB_HI.
    GenTree* hiResult = m_compiler->gtNewOperNode(GT_SUB_HI, TYP_INT, zero, hiOp1);
    Range().InsertBefore(loResult, zero);
    Range().InsertAfter(loResult, hiResult);

    loResult->gtFlags |= GTF_SET_FLAGS;
    hiResult->gtFlags |= GTF_USE_FLAGS;

#endif

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
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
    loResult->gtType        = TYP_INT;
    loResult->AsOp()->gtOp1 = loOp1;
    loResult->AsOp()->gtOp2 = loOp2;

    GenTree* hiResult = new (m_compiler, oper) GenTreeOp(GetHiOper(oper), TYP_INT, hiOp1, hiOp2);
    Range().InsertAfter(loResult, hiResult);

    if ((oper == GT_ADD) || (oper == GT_SUB))
    {
        loResult->gtFlags |= GTF_SET_FLAGS;
        hiResult->gtFlags |= GTF_USE_FLAGS;

        if ((loResult->gtFlags & GTF_OVERFLOW) != 0)
        {
            hiResult->gtFlags |= GTF_OVERFLOW | GTF_EXCEPT;
            loResult->gtFlags &= ~(GTF_OVERFLOW | GTF_EXCEPT);
        }
        if (loResult->gtFlags & GTF_UNSIGNED)
        {
            hiResult->gtFlags |= GTF_UNSIGNED;
        }
    }

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
}

//------------------------------------------------------------------------
// DecomposeShift: Decompose GT_LSH, GT_RSH, GT_RSZ. For shift nodes being shifted
// by a constant int, we can inspect the shift amount and decompose to the appropriate
// node types, generating a shl/shld pattern for GT_LSH, a shrd/shr pattern for GT_RSZ,
// and a shrd/sar pattern for GT_SHR for most shift amounts. Shifting by 0, >= 32 and
// >= 64 are special cased to produce better code patterns.
//
// For all other shift nodes, we need to use the shift helper functions, so we here convert
// the shift into a helper call by pulling its arguments out of linear order and making
// them the args to a call, then replacing the original node with the new call.
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

    GenTree* shift     = use.Def();
    GenTree* gtLong    = shift->gtGetOp1();
    GenTree* loOp1     = gtLong->gtGetOp1();
    GenTree* hiOp1     = gtLong->gtGetOp2();
    GenTree* shiftByOp = shift->gtGetOp2();

    genTreeOps oper        = shift->OperGet();
    genTreeOps shiftByOper = shiftByOp->OperGet();

    // tLo = ...
    // ...
    // tHi = ...
    // ...
    // tLong = long tLo, tHi
    // ...
    // tShiftAmount = ...
    // ...
    // tShift = shift tLong, tShiftAmount

    assert((oper == GT_LSH) || (oper == GT_RSH) || (oper == GT_RSZ));

    // If we are shifting by a constant int, we do not want to use a helper, instead, we decompose.
    if (shiftByOper == GT_CNS_INT)
    {
        // Reduce count modulo 64 to match behavior found in the shift helpers,
        // Compiler::gtFoldExpr and ValueNumStore::EvalOpIntegral.
        unsigned int count = shiftByOp->AsIntCon()->gtIconVal & 0x3F;
        Range().Remove(shiftByOp);

        if (count == 0)
        {
            GenTree* next = shift->gtNext;
            // Remove shift and don't do anything else.
            if (shift->IsUnusedValue())
            {
                gtLong->SetUnusedValue();
            }
            Range().Remove(shift);
            use.ReplaceWith(m_compiler, gtLong);
            return next;
        }

        GenTree* loResult;
        GenTree* hiResult;

        GenTree* insertAfter;

        switch (oper)
        {
            case GT_LSH:
            {
                if (count < 32)
                {
                    // For shifts of < 32 bits, we transform the code to:
                    //
                    //     tLo = ...
                    //           st.lclVar vLo, tLo
                    //     ...
                    //     tHi = ...
                    //     ...
                    //     tShiftLo = lsh vLo, tShiftAmountLo
                    //     tShitHiLong = long vLo, tHi
                    //     tShiftHi = lsh_hi tShiftHiLong, tShiftAmountHi
                    //
                    // This will produce:
                    //
                    //     reg1 = lo
                    //     shl lo, shift
                    //     shld hi, reg1, shift

                    Range().Remove(gtLong);

                    loOp1                = RepresentOpAsLocalVar(loOp1, gtLong, &gtLong->AsOp()->gtOp1);
                    unsigned loOp1LclNum = loOp1->AsLclVarCommon()->GetLclNum();
                    Range().Remove(loOp1);

                    GenTree* shiftByHi = m_compiler->gtNewIconNode(count, TYP_INT);
                    GenTree* shiftByLo = m_compiler->gtNewIconNode(count, TYP_INT);

                    loResult = m_compiler->gtNewOperNode(GT_LSH, TYP_INT, loOp1, shiftByLo);

                    // Create a GT_LONG that contains loCopy and hiOp1. This will be used in codegen to
                    // generate the shld instruction
                    GenTree* loCopy = m_compiler->gtNewLclvNode(loOp1LclNum, TYP_INT);
                    GenTree* hiOp   = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loCopy, hiOp1);
                    hiResult        = m_compiler->gtNewOperNode(GT_LSH_HI, TYP_INT, hiOp, shiftByHi);

                    Range().InsertBefore(shift, loOp1, shiftByLo, loResult);
                    Range().InsertBefore(shift, loCopy, hiOp, shiftByHi, hiResult);

                    insertAfter = hiResult;
                }
                else
                {
                    assert(count >= 32 && count < 64);

                    // Since we're left shifting at least 32 bits, we can remove the hi part of the shifted value iff
                    // it has no side effects.
                    //
                    // TODO-CQ: we could go perform this removal transitively (i.e. iteratively remove everything that
                    // feeds the hi operand while there are no side effects)
                    if ((hiOp1->gtFlags & GTF_ALL_EFFECT) == 0)
                    {
                        Range().Remove(hiOp1, true);
                    }
                    else
                    {
                        hiOp1->SetUnusedValue();
                    }

                    if (count == 32)
                    {
                        // Move loOp1 into hiResult (shift of 32 bits is just a mov of lo to hi)
                        // We need to make sure that we save lo to a temp variable so that we don't overwrite lo
                        // before saving it to hi in the case that we are doing an inplace shift. I.e.:
                        // x = x << 32

                        LIR::Use loOp1Use(Range(), &gtLong->AsOp()->gtOp1, gtLong);
                        loOp1Use.ReplaceWithLclVar(m_compiler);

                        hiResult = loOp1Use.Def();
                        Range().Remove(gtLong);
                    }
                    else
                    {
                        Range().Remove(gtLong);
                        assert(count > 32 && count < 64);

                        // Move loOp1 into hiResult, do a GT_LSH with count - 32.
                        // We will compute hiResult before loResult in this case, so we don't need to store lo to a
                        // temp
                        GenTree* shiftBy = m_compiler->gtNewIconNode(count - 32, TYP_INT);
                        hiResult         = m_compiler->gtNewOperNode(oper, TYP_INT, loOp1, shiftBy);
                        Range().InsertBefore(shift, shiftBy, hiResult);
                    }

                    // Zero out loResult (shift of >= 32 bits shifts all lo bits to hiResult)
                    loResult = m_compiler->gtNewZeroConNode(TYP_INT);
                    Range().InsertBefore(shift, loResult);

                    insertAfter = loResult;
                }
            }
            break;
            case GT_RSZ:
            {
                Range().Remove(gtLong);

                if (count < 32)
                {
                    // Hi is a GT_RSZ, lo is a GT_RSH_LO. Will produce:
                    // reg1 = hi
                    // shrd lo, reg1, shift
                    // shr hi, shift

                    hiOp1                = RepresentOpAsLocalVar(hiOp1, gtLong, &gtLong->AsOp()->gtOp2);
                    unsigned hiOp1LclNum = hiOp1->AsLclVarCommon()->GetLclNum();
                    GenTree* hiCopy      = m_compiler->gtNewLclvNode(hiOp1LclNum, TYP_INT);

                    GenTree* shiftByHi = m_compiler->gtNewIconNode(count, TYP_INT);
                    GenTree* shiftByLo = m_compiler->gtNewIconNode(count, TYP_INT);

                    hiResult = m_compiler->gtNewOperNode(GT_RSZ, TYP_INT, hiOp1, shiftByHi);

                    // Create a GT_LONG that contains loOp1 and hiCopy. This will be used in codegen to
                    // generate the shrd instruction
                    GenTree* loOp = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loOp1, hiCopy);
                    loResult      = m_compiler->gtNewOperNode(GT_RSH_LO, TYP_INT, loOp, shiftByLo);

                    Range().InsertBefore(shift, hiCopy, loOp);
                    Range().InsertBefore(shift, shiftByLo, loResult);
                    Range().InsertBefore(shift, shiftByHi, hiResult);
                }
                else
                {
                    assert(count >= 32 && count < 64);

                    // Since we're right shifting at least 32 bits, we can remove the lo part of the shifted value iff
                    // it has no side effects.
                    //
                    // TODO-CQ: we could go perform this removal transitively (i.e. iteratively remove everything that
                    // feeds the lo operand while there are no side effects)
                    if ((loOp1->gtFlags & (GTF_ALL_EFFECT | GTF_SET_FLAGS)) == 0)
                    {
                        Range().Remove(loOp1, true);
                    }
                    else
                    {
                        loOp1->SetUnusedValue();
                    }

                    if (count == 32)
                    {
                        // Move hiOp1 into loResult.
                        loResult = hiOp1;
                    }
                    else
                    {
                        assert(count > 32 && count < 64);

                        // Move hiOp1 into loResult, do a GT_RSZ with count - 32.
                        GenTree* shiftBy = m_compiler->gtNewIconNode(count - 32, TYP_INT);
                        loResult         = m_compiler->gtNewOperNode(oper, TYP_INT, hiOp1, shiftBy);
                        Range().InsertBefore(shift, shiftBy, loResult);
                    }

                    // Zero out hi
                    hiResult = m_compiler->gtNewZeroConNode(TYP_INT);
                    Range().InsertBefore(shift, hiResult);
                }

                insertAfter = hiResult;
            }
            break;
            case GT_RSH:
            {
                Range().Remove(gtLong);

                hiOp1                = RepresentOpAsLocalVar(hiOp1, gtLong, &gtLong->AsOp()->gtOp2);
                unsigned hiOp1LclNum = hiOp1->AsLclVarCommon()->GetLclNum();
                GenTree* hiCopy      = m_compiler->gtNewLclvNode(hiOp1LclNum, TYP_INT);
                Range().Remove(hiOp1);

                if (count < 32)
                {
                    // Hi is a GT_RSH, lo is a GT_RSH_LO. Will produce:
                    // reg1 = hi
                    // shrd lo, reg1, shift
                    // sar hi, shift

                    GenTree* shiftByHi = m_compiler->gtNewIconNode(count, TYP_INT);
                    GenTree* shiftByLo = m_compiler->gtNewIconNode(count, TYP_INT);

                    hiResult = m_compiler->gtNewOperNode(GT_RSH, TYP_INT, hiOp1, shiftByHi);

                    // Create a GT_LONG that contains loOp1 and hiCopy. This will be used in codegen to
                    // generate the shrd instruction
                    GenTree* loOp = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loOp1, hiCopy);
                    loResult      = m_compiler->gtNewOperNode(GT_RSH_LO, TYP_INT, loOp, shiftByLo);

                    Range().InsertBefore(shift, hiCopy, loOp);
                    Range().InsertBefore(shift, shiftByLo, loResult);
                    Range().InsertBefore(shift, shiftByHi, hiOp1, hiResult);
                }
                else
                {
                    assert(count >= 32 && count < 64);

                    // Since we're right shifting at least 32 bits, we can remove the lo part of the shifted value iff
                    // it has no side effects.
                    //
                    // TODO-CQ: we could go perform this removal transitively (i.e. iteratively remove everything that
                    // feeds the lo operand while there are no side effects)
                    if ((loOp1->gtFlags & (GTF_ALL_EFFECT | GTF_SET_FLAGS)) == 0)
                    {
                        Range().Remove(loOp1, true);
                    }
                    else
                    {
                        loOp1->SetUnusedValue();
                    }

                    if (count == 32)
                    {
                        // Move hiOp1 into loResult.
                        loResult = hiOp1;
                        Range().InsertBefore(shift, loResult);
                    }
                    else
                    {
                        assert(count > 32 && count < 64);

                        // Move hiOp1 into loResult, do a GT_RSH with count - 32.
                        GenTree* shiftBy = m_compiler->gtNewIconNode(count - 32, TYP_INT);
                        loResult         = m_compiler->gtNewOperNode(oper, TYP_INT, hiOp1, shiftBy);
                        Range().InsertBefore(shift, hiOp1, shiftBy, loResult);
                    }

                    // Propagate sign bit in hiResult
                    GenTree* shiftBy = m_compiler->gtNewIconNode(31, TYP_INT);
                    hiResult         = m_compiler->gtNewOperNode(GT_RSH, TYP_INT, hiCopy, shiftBy);
                    Range().InsertBefore(shift, shiftBy, hiCopy, hiResult);
                }

                insertAfter = hiResult;
            }
            break;
            default:
                unreached();
        }

        // Remove shift from Range
        Range().Remove(shift);

        return FinalizeDecomposition(use, loResult, hiResult, insertAfter);
    }
    else
    {
        // Because calls must be created as HIR and lowered to LIR, we need to dump
        // any LIR temps into lclVars before using them as arguments.
        shiftByOp = RepresentOpAsLocalVar(shiftByOp, shift, &shift->AsOp()->gtOp2);
        loOp1     = RepresentOpAsLocalVar(loOp1, gtLong, &gtLong->AsOp()->gtOp1);
        hiOp1     = RepresentOpAsLocalVar(hiOp1, gtLong, &gtLong->AsOp()->gtOp2);

        Range().Remove(shiftByOp);
        Range().Remove(gtLong);
        Range().Remove(loOp1);
        Range().Remove(hiOp1);

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

        GenTreeCall::Use* argList = m_compiler->gtNewCallArgs(loOp1, hiOp1, shiftByOp);

        GenTreeCall* call = m_compiler->gtNewHelperCallNode(helper, TYP_LONG, argList);
        call->gtFlags |= shift->gtFlags & GTF_ALL_EFFECT;

        if (shift->IsUnusedValue())
        {
            call->SetUnusedValue();
        }

        call = m_compiler->fgMorphArgs(call);
        Range().InsertAfter(shift, LIR::SeqTree(m_compiler, call));

        Range().Remove(shift);
        use.ReplaceWith(m_compiler, call);
        return call;
    }
}

//------------------------------------------------------------------------
// DecomposeRotate: Decompose GT_ROL and GT_ROR with constant shift amounts. We can
// inspect the rotate amount and decompose to the appropriate node types, generating
// a shld/shld pattern for GT_ROL, a shrd/shrd pattern for GT_ROR, for most rotate
// amounts.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeRotate(LIR::Use& use)
{
    GenTree* tree       = use.Def();
    GenTree* gtLong     = tree->gtGetOp1();
    GenTree* rotateByOp = tree->gtGetOp2();

    genTreeOps oper = tree->OperGet();

    assert((oper == GT_ROL) || (oper == GT_ROR));
    assert(rotateByOp->IsCnsIntOrI());

    // For longs, we need to change rols into two GT_LSH_HIs and rors into two GT_RSH_LOs
    // so we will get:
    //
    // shld lo, hi, rotateAmount
    // shld hi, loCopy, rotateAmount
    //
    // or:
    //
    // shrd lo, hi, rotateAmount
    // shrd hi, loCopy, rotateAmount

    if (oper == GT_ROL)
    {
        oper = GT_LSH_HI;
    }
    else
    {
        oper = GT_RSH_LO;
    }

    unsigned count = (unsigned)rotateByOp->AsIntCon()->gtIconVal;
    Range().Remove(rotateByOp);

    // Make sure the rotate amount is between 0 and 63.
    assert((count < 64) && (count != 0));

    GenTree* loResult;
    GenTree* hiResult;

    if (count == 32)
    {
        // If the rotate amount is 32, then swap hi and lo
        LIR::Use loOp1Use(Range(), &gtLong->AsOp()->gtOp1, gtLong);
        loOp1Use.ReplaceWithLclVar(m_compiler);

        LIR::Use hiOp1Use(Range(), &gtLong->AsOp()->gtOp2, gtLong);
        hiOp1Use.ReplaceWithLclVar(m_compiler);

        hiResult              = loOp1Use.Def();
        loResult              = hiOp1Use.Def();
        gtLong->AsOp()->gtOp1 = loResult;
        gtLong->AsOp()->gtOp2 = hiResult;

        if (tree->IsUnusedValue())
        {
            gtLong->SetUnusedValue();
        }

        GenTree* next = tree->gtNext;
        // Remove tree and don't do anything else.
        Range().Remove(tree);
        use.ReplaceWith(m_compiler, gtLong);
        return next;
    }
    else
    {
        GenTree* loOp1;
        GenTree* hiOp1;

        if (count > 32)
        {
            // If count > 32, we swap hi and lo, and subtract 32 from count
            hiOp1 = gtLong->gtGetOp1();
            loOp1 = gtLong->gtGetOp2();

            Range().Remove(gtLong);
            loOp1 = RepresentOpAsLocalVar(loOp1, gtLong, &gtLong->AsOp()->gtOp2);
            hiOp1 = RepresentOpAsLocalVar(hiOp1, gtLong, &gtLong->AsOp()->gtOp1);

            count -= 32;
        }
        else
        {
            loOp1 = gtLong->gtGetOp1();
            hiOp1 = gtLong->gtGetOp2();

            Range().Remove(gtLong);
            loOp1 = RepresentOpAsLocalVar(loOp1, gtLong, &gtLong->AsOp()->gtOp1);
            hiOp1 = RepresentOpAsLocalVar(hiOp1, gtLong, &gtLong->AsOp()->gtOp2);
        }

        unsigned loOp1LclNum = loOp1->AsLclVarCommon()->GetLclNum();
        unsigned hiOp1LclNum = hiOp1->AsLclVarCommon()->GetLclNum();

        Range().Remove(loOp1);
        Range().Remove(hiOp1);

        GenTree* rotateByHi = m_compiler->gtNewIconNode(count, TYP_INT);
        GenTree* rotateByLo = m_compiler->gtNewIconNode(count, TYP_INT);

        // Create a GT_LONG that contains loOp1 and hiCopy. This will be used in codegen to
        // generate the shld instruction
        GenTree* hiCopy = m_compiler->gtNewLclvNode(hiOp1LclNum, TYP_INT);
        GenTree* loOp   = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, hiCopy, loOp1);
        loResult        = m_compiler->gtNewOperNode(oper, TYP_INT, loOp, rotateByLo);

        // Create a GT_LONG that contains loCopy and hiOp1. This will be used in codegen to
        // generate the shld instruction
        GenTree* loCopy = m_compiler->gtNewLclvNode(loOp1LclNum, TYP_INT);
        GenTree* hiOp   = new (m_compiler, GT_LONG) GenTreeOp(GT_LONG, TYP_LONG, loCopy, hiOp1);
        hiResult        = m_compiler->gtNewOperNode(oper, TYP_INT, hiOp, rotateByHi);

        Range().InsertBefore(tree, hiCopy, loOp1, loOp);
        Range().InsertBefore(tree, rotateByLo, loResult);
        Range().InsertBefore(tree, loCopy, hiOp1, hiOp);
        Range().InsertBefore(tree, rotateByHi, hiResult);

        Range().Remove(tree);

        return FinalizeDecomposition(use, loResult, hiResult, hiResult);
    }
}

//------------------------------------------------------------------------
// DecomposeMul: Decompose GT_MUL. The only GT_MULs that make it to decompose are
// those with the GTF_MUL_64RSLT flag set. These muls result in a mul instruction that
// returns its result in two registers like GT_CALLs do. Additionally, these muls are
// guaranteed to be in the form long = (long)int * (long)int. Therefore, to decompose
// these nodes, we convert them into GT_MUL_LONGs, undo the cast from int to long by
// stripping out the lo ops, and force them into the form var = mul, as we do for
// GT_CALLs. In codegen, we then produce a mul instruction that produces the result
// in edx:eax on x86 or in any two chosen by RA registers on arm32, and store those
// registers on the stack in genStoreLongLclVar.
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

    // We expect both operands to be int->long casts. DecomposeCast specifically
    // ignores such casts when they are used by GT_MULs.
    assert((op1->OperGet() == GT_CAST) && (op1->TypeGet() == TYP_LONG));
    assert((op2->OperGet() == GT_CAST) && (op2->TypeGet() == TYP_LONG));

    Range().Remove(op1);
    Range().Remove(op2);

    tree->AsOp()->gtOp1 = op1->gtGetOp1();
    tree->AsOp()->gtOp2 = op2->gtGetOp1();
    tree->SetOper(GT_MUL_LONG);

    return StoreNodeToVar(use);
}

//------------------------------------------------------------------------
// DecomposeUMod: Decompose GT_UMOD. The only GT_UMODs that make it to decompose
// are guaranteed to be an unsigned long mod with op2 which is a cast to long from
// a constant int whose value is between 2 and 0x3fffffff. All other GT_UMODs are
// morphed into helper calls. These GT_UMODs will actually return an int value in
// RDX. In decompose, we make the lo operation a TYP_INT GT_UMOD, with op2 as the
// original lo half and op1 as a GT_LONG. We make the hi part 0,  so we end up with:
//
// GT_UMOD[TYP_INT] ( GT_LONG [TYP_LONG] (loOp1, hiOp1), loOp2 [TYP_INT] )
//
// With the expectation that we will generate:
//
// EDX = hiOp1
// EAX = loOp1
// reg = loOp2
// idiv reg
// EDX is the remainder, and result of GT_UMOD
// mov hiReg = 0
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeUMod(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTree*   tree = use.Def();
    genTreeOps oper = tree->OperGet();

    assert(oper == GT_UMOD);

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();
    assert(op1->OperGet() == GT_LONG);
    assert(op2->OperGet() == GT_LONG);

    GenTree* loOp2 = op2->gtGetOp1();
    GenTree* hiOp2 = op2->gtGetOp2();

    assert(loOp2->OperGet() == GT_CNS_INT);
    assert(hiOp2->OperGet() == GT_CNS_INT);
    assert((loOp2->AsIntCon()->gtIconVal >= 2) && (loOp2->AsIntCon()->gtIconVal <= 0x3fffffff));
    assert(hiOp2->AsIntCon()->gtIconVal == 0);

    // Get rid of op2's hi part. We don't need it.
    Range().Remove(hiOp2);
    Range().Remove(op2);

    // Lo part is the GT_UMOD
    GenTree* loResult       = tree;
    loResult->AsOp()->gtOp2 = loOp2;
    loResult->gtType        = TYP_INT;

    // Set the high part to 0
    GenTree* hiResult = m_compiler->gtNewZeroConNode(TYP_INT);

    Range().InsertAfter(loResult, hiResult);

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
}

#ifdef FEATURE_SIMD

//------------------------------------------------------------------------
// DecomposeSimd: Decompose GT_SIMD.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeSimd(LIR::Use& use)
{
    GenTree*   tree = use.Def();
    genTreeOps oper = tree->OperGet();

    assert(oper == GT_SIMD);

    GenTreeSIMD* simdTree = tree->AsSIMD();

    switch (simdTree->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicGetItem:
            return DecomposeSimdGetItem(use);

        default:
            noway_assert(!"unexpected GT_SIMD node in long decomposition");
            break;
    }

    return nullptr;
}

//------------------------------------------------------------------------
// DecomposeSimdGetItem: Decompose GT_SIMD -- SIMDIntrinsicGetItem.
//
// Decompose a get[i] node on Vector<long>. For:
//
// GT_SIMD{get_item}[long](simd_var, index)
//
// create:
//
// tmp_simd_var = simd_var
// tmp_index = index
// loResult = GT_SIMD{get_item}[int](tmp_simd_var, tmp_index * 2)
// hiResult = GT_SIMD{get_item}[int](tmp_simd_var, tmp_index * 2 + 1)
// return: GT_LONG(loResult, hiResult)
//
// This isn't optimal codegen, since SIMDIntrinsicGetItem sometimes requires
// temps that could be shared, for example.
//
// Arguments:
//    use - the LIR::Use object for the def that needs to be decomposed.
//
// Return Value:
//    The next node to process.
//
GenTree* DecomposeLongs::DecomposeSimdGetItem(LIR::Use& use)
{
    GenTree*   tree = use.Def();
    genTreeOps oper = tree->OperGet();

    assert(oper == GT_SIMD);

    GenTreeSIMD* simdTree = tree->AsSIMD();
    var_types    baseType = simdTree->GetSimdBaseType();
    unsigned     simdSize = simdTree->GetSimdSize();

    assert(simdTree->gtSIMDIntrinsicID == SIMDIntrinsicGetItem);
    assert(varTypeIsLong(baseType));
    assert(varTypeIsLong(simdTree));
    assert(varTypeIsSIMD(simdTree->AsOp()->gtOp1->gtType));
    assert(simdTree->AsOp()->gtOp2->gtType == TYP_INT);

    bool    indexIsConst = simdTree->AsOp()->gtOp2->IsCnsIntOrI();
    ssize_t index        = 0;
    if (indexIsConst)
    {
        index = simdTree->AsOp()->gtOp2->AsIntCon()->gtIconVal;
    }

    GenTree* simdTmpVar    = RepresentOpAsLocalVar(simdTree->AsOp()->gtOp1, simdTree, &simdTree->AsOp()->gtOp1);
    unsigned simdTmpVarNum = simdTmpVar->AsLclVarCommon()->GetLclNum();
    JITDUMP("[DecomposeSimdGetItem]: Saving op1 tree to a temp var:\n");
    DISPTREERANGE(Range(), simdTmpVar);
    Range().Remove(simdTmpVar);

    GenTree* indexTmpVar    = nullptr;
    unsigned indexTmpVarNum = 0;
    if (!indexIsConst)
    {
        indexTmpVar    = RepresentOpAsLocalVar(simdTree->AsOp()->gtOp2, simdTree, &simdTree->AsOp()->gtOp2);
        indexTmpVarNum = indexTmpVar->AsLclVarCommon()->GetLclNum();
        JITDUMP("[DecomposeSimdGetItem]: Saving op2 tree to a temp var:\n");
        DISPTREERANGE(Range(), indexTmpVar);
        Range().Remove(indexTmpVar);
    }

    // Create:
    //      loResult = GT_SIMD{get_item}[int](tmp_simd_var, index * 2)

    GenTree* simdTmpVar1 = simdTmpVar;
    GenTree* indexTimesTwo1;

    if (indexIsConst)
    {
        // Reuse the existing index constant node.
        indexTimesTwo1 = simdTree->AsOp()->gtOp2;
        Range().Remove(indexTimesTwo1);
        indexTimesTwo1->AsIntCon()->gtIconVal = index * 2;

        Range().InsertBefore(simdTree, simdTmpVar1, indexTimesTwo1);
    }
    else
    {
        GenTree* indexTmpVar1 = indexTmpVar;
        GenTree* two1         = m_compiler->gtNewIconNode(2, TYP_INT);
        indexTimesTwo1        = m_compiler->gtNewOperNode(GT_MUL, TYP_INT, indexTmpVar1, two1);
        Range().InsertBefore(simdTree, simdTmpVar1, indexTmpVar1, two1, indexTimesTwo1);
    }

    GenTree* loResult = m_compiler->gtNewSIMDNode(TYP_INT, simdTmpVar1, indexTimesTwo1, SIMDIntrinsicGetItem,
                                                  CORINFO_TYPE_INT, simdSize);
    Range().InsertBefore(simdTree, loResult);

    // Create:
    //      hiResult = GT_SIMD{get_item}[int](tmp_simd_var, index * 2 + 1)

    GenTree* simdTmpVar2 = m_compiler->gtNewLclLNode(simdTmpVarNum, simdTree->AsOp()->gtOp1->gtType);
    GenTree* indexTimesTwoPlusOne;

    if (indexIsConst)
    {
        indexTimesTwoPlusOne = m_compiler->gtNewIconNode(index * 2 + 1, TYP_INT);
        Range().InsertBefore(simdTree, simdTmpVar2, indexTimesTwoPlusOne);
    }
    else
    {
        GenTree* indexTmpVar2   = m_compiler->gtNewLclLNode(indexTmpVarNum, TYP_INT);
        GenTree* two2           = m_compiler->gtNewIconNode(2, TYP_INT);
        GenTree* indexTimesTwo2 = m_compiler->gtNewOperNode(GT_MUL, TYP_INT, indexTmpVar2, two2);
        GenTree* one            = m_compiler->gtNewIconNode(1, TYP_INT);
        indexTimesTwoPlusOne    = m_compiler->gtNewOperNode(GT_ADD, TYP_INT, indexTimesTwo2, one);
        Range().InsertBefore(simdTree, simdTmpVar2, indexTmpVar2, two2, indexTimesTwo2);
        Range().InsertBefore(simdTree, one, indexTimesTwoPlusOne);
    }

    GenTree* hiResult = m_compiler->gtNewSIMDNode(TYP_INT, simdTmpVar2, indexTimesTwoPlusOne, SIMDIntrinsicGetItem,
                                                  CORINFO_TYPE_INT, simdSize);
    Range().InsertBefore(simdTree, hiResult);

    // Done with the original tree; remove it.

    Range().Remove(simdTree);

    return FinalizeDecomposition(use, loResult, hiResult, hiResult);
}

#endif // FEATURE_SIMD

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
        unsigned varNum = user->AsLclVarCommon()->GetLclNum();
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
    unsigned varNum                              = use.ReplaceWithLclVar(m_compiler);
    m_compiler->lvaTable[varNum].lvIsMultiRegRet = true;

    // Decompose the new LclVar use
    return DecomposeLclVar(use);
}

//------------------------------------------------------------------------
// Check is op already local var, if not store it to local.
//
// Arguments:
//    op - GenTree* to represent as local variable
//    user - user of op
//    edge - edge from user to op
//
// Return Value:
//    op represented as local var
//
GenTree* DecomposeLongs::RepresentOpAsLocalVar(GenTree* op, GenTree* user, GenTree** edge)
{
    if (op->OperGet() == GT_LCL_VAR)
    {
        return op;
    }
    else
    {
        LIR::Use opUse(Range(), edge, user);
        opUse.ReplaceWithLclVar(m_compiler);
        return *edge;
    }
}

//------------------------------------------------------------------------
// DecomposeLongs::EnsureIntSized:
//    Checks to see if the given node produces an int-sized value and
//    performs the appropriate widening if it does not.
//
// Arguments:
//    node       - The node that may need to be widened.
//    signExtend - True if the value should be sign-extended; false if it
//                 should be zero-extended.
//
// Return Value:
//    The node that produces the widened value.
GenTree* DecomposeLongs::EnsureIntSized(GenTree* node, bool signExtend)
{
    assert(node != nullptr);
    if (!varTypeIsSmall(node))
    {
        assert(genTypeSize(node) == genTypeSize(TYP_INT));
        return node;
    }

    if (node->OperIs(GT_LCL_VAR) && !m_compiler->lvaTable[node->AsLclVarCommon()->GetLclNum()].lvNormalizeOnLoad())
    {
        node->gtType = TYP_INT;
        return node;
    }

    GenTree* const cast = m_compiler->gtNewCastNode(TYP_INT, node, !signExtend, node->TypeGet());
    Range().InsertAfter(node, cast);
    return cast;
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

#endif // !TARGET_64BIT
