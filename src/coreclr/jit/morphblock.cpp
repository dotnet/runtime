// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

class MorphInitBlockHelper
{
public:
    static GenTree* MorphInitBlock(Compiler* comp, GenTree* tree);

protected:
    MorphInitBlockHelper(Compiler* comp, GenTree* asg, bool initBlock);

    GenTree* Morph();

    void         PrepareDst();
    virtual void PrepareSrc();

    virtual void TrySpecialCases();
    virtual void MorphStructCases();

    virtual const char* GetHelperName() const
    {
        return "MorphInitBlock";
    }

    static GenTree* MorphBlock(Compiler* comp, GenTree* tree, bool isDest);
    static GenTree* MorphCommaBlock(Compiler* comp, GenTreeOp* firstComma);

protected:
    Compiler* m_comp;
    bool      m_initBlock;

    GenTreeOp* m_asg = nullptr;
    GenTree*   m_dst = nullptr;
    GenTree*   m_src = nullptr;

    unsigned             m_blockSize          = 0;
    unsigned             m_dstLclNum          = BAD_VAR_NUM;
    GenTreeLclVarCommon* m_dstLclNode         = nullptr;
    LclVarDsc*           m_dstVarDsc          = nullptr;
    FieldSeqNode*        m_dstFldSeq          = nullptr;
    unsigned             m_dstLclOffset       = 0;
    bool                 m_dstUseLclFld       = false;
    bool                 m_dstSingleLclVarAsg = false;
    GenTree*             m_dstAddr            = nullptr;
    ssize_t              m_dstAddOff          = 0;

#if defined(DEBUG)
    bool m_isLateArg = false;
#endif // DEBUG

    enum class BlockTransformation
    {
        Undefined,
        FieldByField,
        OneAsgBlock,
        StructBlock,
        SkipCallSrc,
        SkipMultiRegIntrinsicSrc,
        Nop
    };

    BlockTransformation m_transformationDecision = BlockTransformation::Undefined;
    GenTree*            m_result                 = nullptr;
};

//------------------------------------------------------------------------
// MorphInitBlock: Morph a block initialization assignment tree.
//
// Arguments:
//    comp - a compiler instance;
//    tree - A GT_ASG tree that performs block initialization.
//
// Return Value:
//    A possibly modified tree to perfom the initializetion.
//
// static
GenTree* MorphInitBlockHelper::MorphInitBlock(Compiler* comp, GenTree* tree)
{
    const bool           initBlock = true;
    MorphInitBlockHelper helper(comp, tree, initBlock);
    return helper.Morph();
}

//------------------------------------------------------------------------
// MorphInitBlockHelper: helper's constructor.
//
// Arguments:
//    comp - a compiler instance;
//    initBlock - true if this is init block op, false if it is a copy block;
//    asg - GT_ASG node to morph.
//
// Notes:
//    Most class members are initialized via in-class member initializers.
//
MorphInitBlockHelper::MorphInitBlockHelper(Compiler* comp, GenTree* asg, bool initBlock = true)
    : m_comp(comp), m_initBlock(initBlock)
{
    assert(asg->OperIs(GT_ASG));
#if defined(DEBUG)
    if (m_initBlock)
    {
        assert(asg->OperIsInitBlkOp());
    }
    else
    {
        assert(asg->OperIsCopyBlkOp());
    }
#endif // DEBUG
    m_asg = asg->AsOp();
}

//------------------------------------------------------------------------
// Morph: transform the asg to a possible better form and changes its children
//    to an appropriate form for later phases, for example, adds SIMD_INIT nodes
//    or sets lvDoNotEnregister on locals.
//
// Return Value:
//    A possibly modified tree to perfom the block operation.
//
// Notes:
//    It is used for both init and copy block.
//
GenTree* MorphInitBlockHelper::Morph()
{
    JITDUMP("%s:\n", GetHelperName());

    PrepareDst();
    PrepareSrc();

    INDEBUG(m_isLateArg = (m_asg->gtFlags & GTF_LATE_ARG) != 0);

    TrySpecialCases();

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        GenTree* oneAsgTree = nullptr;
        if (m_dst != m_dstLclNode)
        {
            oneAsgTree = m_comp->fgMorphOneAsgBlockOp(m_asg);
        }
        if (oneAsgTree != nullptr)
        {
            assert((m_asg == oneAsgTree) && "fgMorphOneAsgBlock must return the incoming tree.");

            m_transformationDecision = BlockTransformation::OneAsgBlock;
            m_result                 = oneAsgTree;
        }
        else
        {
            MorphStructCases();
        }
    }

    assert(m_transformationDecision != BlockTransformation::Undefined);
    assert(m_result != nullptr);

    if (m_result != m_asg)
    {
        const bool isLateArg = ((m_asg->gtFlags & GTF_LATE_ARG) != 0);
        assert(m_isLateArg == isLateArg);
        if (isLateArg)
        {
            assert(!m_initBlock && "do not expect a block init as a late arg.");
            m_result->gtFlags |= GTF_LATE_ARG;
        }
    }

#ifdef DEBUG
    if (m_result != m_asg)
    {
        m_result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
    }
    if (m_comp->verbose)
    {
        printf("%s (after):\n", GetHelperName());
        m_comp->gtDispTree(m_result);
    }
#endif // DEBUG

    return m_result;
}

//------------------------------------------------------------------------
// PrepareDst: Transform the asg destination to an appropriate form and initialize member fields
//    with information about it.
//
// Notes:
//    When assertion propagation is enabled this method kills assertions about the dst local,
//    so the correctness depends on `IsLocalAddrExpr` recognizing all patterns.
//
void MorphInitBlockHelper::PrepareDst()
{
    GenTree* origDst = m_asg->gtGetOp1();
    m_dst            = MorphBlock(m_comp, origDst, true);
    if (m_dst != origDst)
    {
        m_asg->gtOp1 = m_dst;
    }

    if (m_asg->TypeGet() != m_dst->TypeGet())
    {
        assert(!m_initBlock && "the asg type should be final for an init block.");
        JITDUMP("changing type of asignment from %-6s to %-6s\n", varTypeName(m_asg->TypeGet()),
                varTypeName(m_dst->TypeGet()));

        m_asg->ChangeType(m_dst->TypeGet());
    }

    if (m_dst->IsLocal())
    {
        m_dstLclNode = m_dst->AsLclVarCommon();
        m_dstVarDsc  = m_comp->lvaGetDesc(m_dstLclNode);

        if (m_dst->OperIs(GT_LCL_VAR))
        {
            if (m_dstVarDsc->TypeGet() == TYP_STRUCT)
            {
#ifdef DEBUG
                const bool isSizeMistmatch =
                    (m_dstVarDsc->lvExactSize != m_comp->info.compCompHnd->getClassSize(m_dstVarDsc->GetStructHnd()));
                const bool isStackAllocCandidate =
                    m_comp->compObjectStackAllocation() && !m_dstVarDsc->GetLayout()->IsValueClass();
                // There were cases where for temps lvExactSize did not correspond to the struct size
                // so we were using `getClassSize` result here, however, now this cases are gone and the only
                // scenario when `getClassSize` != `lvExactSize` it is a class object optimized to be on stack
                assert(!isSizeMistmatch || isStackAllocCandidate);
#endif // DEBUG
                m_blockSize = m_dstVarDsc->lvExactSize;
            }
            else
            {
                m_blockSize = genTypeSize(m_dstVarDsc);
            }
        }
        else
        {
            assert(m_dst->OperIs(GT_LCL_FLD) && !m_dst->TypeIs(TYP_STRUCT));
            GenTreeLclFld* destFld = m_dst->AsLclFld();
            m_blockSize            = genTypeSize(destFld->TypeGet());
            m_dstFldSeq            = destFld->GetFieldSeq();
        }
    }
    else
    {
        assert(m_dst == m_dst->gtEffectiveVal() && "the commas were skipped in MorphBlock");
        assert(m_dst->OperIs(GT_IND, GT_BLK, GT_OBJ));

        GenTree* dstAddr = m_dst->AsIndir()->Addr();
        if (m_dst->OperGet() == GT_IND)
        {
            assert(m_dst->TypeGet() != TYP_STRUCT);
            m_blockSize = genTypeSize(m_dst);
        }
        else
        {
            assert(m_dst->OperIsBlk());
            GenTreeBlk* blk = m_dst->AsBlk();
            m_blockSize     = blk->Size();
        }

        noway_assert(dstAddr->TypeIs(TYP_BYREF, TYP_I_IMPL));
        if (dstAddr->IsLocalAddrExpr(m_comp, &m_dstLclNode, &m_dstFldSeq, &m_dstAddOff))
        {
            // Note that lclNode can be a field, like `BLK<4> struct(ADD(ADDR(LCL_FLD int), CNST_INT))`.
            m_dstVarDsc = m_comp->lvaGetDesc(m_dstLclNode);
        }
    }

    if (m_dstLclNode != nullptr)
    {
        m_dstLclNum    = m_dstLclNode->GetLclNum();
        m_dstLclOffset = m_dstLclNode->GetLclOffs();

        // Kill everything about m_dstLclNum (and its field locals)
        if (m_comp->optLocalAssertionProp && (m_comp->optAssertionCount > 0))
        {
            m_comp->fgKillDependentAssertions(m_dstLclNum DEBUGARG(m_asg));
        }
    }

#if defined(DEBUG)
    if (m_comp->verbose)
    {
        printf("PrepareDst for [%06u] ", m_comp->dspTreeID(origDst));
        if (m_dstLclNode != nullptr)
        {
            printf("have found a local var V%02u.\n", m_dstLclNum);
        }
        else
        {
            printf("have not found a local var.\n");
        }
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// PrepareSrc: Transform the asg src to an appropriate form and initialize member fields
//    with information about it.
//
void MorphInitBlockHelper::PrepareSrc()
{
    m_src = m_asg->gtGetOp2();
}

//------------------------------------------------------------------------
// TrySpecialCases: check special cases that require special transformations.
//    We don't have any for for init block.
//
void MorphInitBlockHelper::TrySpecialCases()
{
    return;
}

//------------------------------------------------------------------------
// MorphStructCases: transforms the asg as field by field init or keeps it as a block init
//    but sets appropriate flags for the involved lclVars.
//
// Assumptions:
//    we have already checked that it is not a special case and can't be transformed as OneAsgBlock.
//
void MorphInitBlockHelper::MorphStructCases()
{
    GenTree* initVal = m_src->OperIsInitVal() ? m_src->gtGetOp1() : m_src;

    if (m_dstLclNum != BAD_VAR_NUM)
    {
        // If we have already determined that a promoted TYP_STRUCT lclVar will not be enregistered,
        // we are better off doing a block init.
        bool tryFieldByField = false;
        if (m_dstVarDsc->lvPromoted)
        {
            assert(m_dstLclNode->OperIs(GT_LCL_VAR)); // Don't expect a promoted LCL_VAR with a field reference.
            if (m_dstVarDsc->lvDoNotEnregister)
            {
                tryFieldByField = false;
            }
            else
            {
                tryFieldByField = true;
            }
        }

        if (tryFieldByField)
        {
            GenTreeLclVar* dstLclVar = m_dstLclNode->AsLclVar();
            GenTree*       newTree   = m_comp->fgMorphPromoteLocalInitBlock(dstLclVar, initVal, m_blockSize);

            if (newTree != nullptr)
            {
                m_transformationDecision = BlockTransformation::FieldByField;
                m_result                 = newTree;
            }
        }
    }

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        // For an InitBlock we always require a block operand.
        m_dst = m_comp->fgMorphBlockOperand(m_dst, m_dst->TypeGet(), m_blockSize, true /*isBlkReqd*/);
        m_transformationDecision = BlockTransformation::StructBlock;
        m_dst->gtFlags |= GTF_DONT_CSE;
        m_result                = m_asg;
        m_result->AsOp()->gtOp1 = m_dst;
        m_result->gtFlags |= (m_dst->gtFlags & GTF_ALL_EFFECT);

#if FEATURE_SIMD
        if (varTypeIsSIMD(m_asg) && (m_dst == m_dstLclNode))
        {
            // For a SIMD local init we need to call SIMDIntrinsic init.
            // We need this block becuase morph does not create SIMD init for promoted lclVars.
            assert(m_src->IsIntegralConst(0) || m_src->IsFPZero());
            assert(m_dstVarDsc != nullptr);
            const var_types asgType         = m_asg->TypeGet();
            CorInfoType     simdBaseJitType = m_dstVarDsc->GetSimdBaseJitType();
            m_src = m_comp->gtNewSIMDNode(asgType, m_src, SIMDIntrinsicInit, simdBaseJitType, m_blockSize);
            m_result->AsOp()->gtOp2 = m_src;
        }
#endif // FEATURE_SIMD

        if (m_dstVarDsc != nullptr)
        {
            if (m_dst != m_dstLclNode)
            {
                // If we access the dst as a whole but not directly, for example, with OBJ(ADDR(LCL_VAR))
                // then set doNotEnreg.
                // TODO-1stClassStructs: remove it when we can represent narowing struct cast
                // without taking address of the lcl.
                m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::CastTakesAddr));
            }
            else if (m_dstVarDsc->lvPromoted)
            {
                m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
            }
        }
    }
}

//------------------------------------------------------------------------
// MorphBlock: Morph a block node preparatory to morphing a block assignment.
//
// Arguments:
//    comp - a compiler instance;
//    tree - a struct type node;
//    isDest - true if this is the destination of an assignment;
//
// Return Value:
//    Returns the possibly-morphed node. The caller is responsible for updating
//    the parent of this node.
//
// static
GenTree* MorphInitBlockHelper::MorphBlock(Compiler* comp, GenTree* tree, bool isDest)
{
    JITDUMP("MorphBlock for %s tree, before:\n", (isDest ? "dst" : "src"));
    DISPTREE(tree);

    // Src can be a primitive type.
    assert(!isDest || varTypeIsStruct(tree));

    GenTree* handleTree = nullptr;
    GenTree* addr       = nullptr;

    if (tree->OperIs(GT_COMMA))
    {
        // TODO-Cleanup: this block is not needed for not struct nodes, but
        // fgMorphOneAsgBlockOp works wrong without this transformation.
        tree = MorphCommaBlock(comp, tree->AsOp());
    }

    if (!tree->OperIsBlk())
    {
        JITDUMP("MorphBlock after:\n");
        DISPTREE(tree);
        return tree;
    }

    GenTree* blkAddr = tree->AsBlk()->Addr();
    assert(blkAddr != nullptr);
    assert(blkAddr->TypeIs(TYP_I_IMPL, TYP_BYREF, TYP_REF));
    // GT_ADDR, GT_LCL_VAR/FLD, GT_ADD, GT_COMMA, GT_CALL, GT_CNS_INT, GT_LCL_VAR/FLD_ADDR

    JITDUMP("MorphBlock after:\n");
    DISPTREE(tree);
    return tree;
}

//------------------------------------------------------------------------
// MorphCommaBlock: transform COMMA<struct>(X) as OBJ<STRUCT>(COMMA byref(ADDR(X)).
//
// Notes:
//    In order to CSE and value number array index expressions and bounds checks,
//    the commas in which they are contained need to match.
//    The pattern is that the COMMA should be the address expression.
//    Therefore, we insert a GT_ADDR just above the node, and wrap it in an obj or ind.
//    TODO-1stClassStructs: Consider whether this can be improved.
//    Example:
//      before: [3] comma struct <- [2] comma struct <- [1] LCL_VAR struct
//      after: [5] obj <- [3] comma byref <- [2] comma byref <- [4] addr byref <- [1] LCL_VAR struct
//
// static
GenTree* MorphInitBlockHelper::MorphCommaBlock(Compiler* comp, GenTreeOp* firstComma)
{
    assert(firstComma->OperIs(GT_COMMA));

    ArrayStack<GenTree*> commas(comp->getAllocator(CMK_ArrayStack));
    for (GenTree* currComma = firstComma; currComma != nullptr && currComma->OperIs(GT_COMMA);
         currComma          = currComma->gtGetOp2())
    {
        commas.Push(currComma);
    }

    GenTree* lastComma = commas.Top();

    GenTree* effectiveVal = lastComma->gtGetOp2();

    assert(effectiveVal == firstComma->gtEffectiveVal());

    GenTree* effectiveValAddr = comp->gtNewOperNode(GT_ADDR, TYP_BYREF, effectiveVal);

    INDEBUG(effectiveValAddr->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);

    lastComma->AsOp()->gtOp2 = effectiveValAddr;

    while (!commas.Empty())
    {
        GenTree* comma = commas.Pop();
        comma->gtType  = TYP_BYREF;
        comp->gtUpdateNodeSideEffects(comma);
    }

    const var_types blockType = effectiveVal->TypeGet();
    GenTree*        addr      = firstComma;

    GenTree* res;

    if (blockType == TYP_STRUCT)
    {
        CORINFO_CLASS_HANDLE structHnd = comp->gtGetStructHandleIfPresent(effectiveVal);
        if (structHnd == NO_CLASS_HANDLE)
        {
            // TODO-1stClassStructs: get rid of all such cases.
            res = comp->gtNewIndir(blockType, addr);
        }
        else
        {
            res = comp->gtNewObjNode(structHnd, addr);
            comp->gtSetObjGcInfo(res->AsObj());
        }
    }
    else
    {
        res = comp->gtNewIndir(blockType, addr);
    }

    comp->gtUpdateNodeSideEffects(res);
    INDEBUG(res->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);
    return res;
}

class MorphCopyBlockHelper : public MorphInitBlockHelper
{
public:
    static GenTree* MorphCopyBlock(Compiler* comp, GenTree* tree);

protected:
    MorphCopyBlockHelper(Compiler* comp, GenTree* asg);

    void PrepareSrc() override;

    void TrySpecialCases() override;

    void     MorphStructCases() override;
    GenTree* CopyFieldByField();

    const char* GetHelperName() const override
    {
        return "MorphCopyBlock";
    }

protected:
    unsigned             m_srcLclNum          = BAD_VAR_NUM;
    LclVarDsc*           m_srcVarDsc          = nullptr;
    GenTreeLclVarCommon* m_srcLclNode         = nullptr;
    FieldSeqNode*        m_srcFldSeq          = nullptr;
    bool                 m_srcUseLclFld       = false;
    unsigned             m_srcLclOffset       = 0;
    bool                 m_srcSingleLclVarAsg = false;
    GenTree*             m_srcAddr            = nullptr;
    ssize_t              m_srcAddOff          = 0;

    bool m_dstDoFldAsg = false;
    bool m_srcDoFldAsg = false;
};

//------------------------------------------------------------------------
// MorphCopyBlock: Morph a block copy assignment tree.
//
// Arguments:
//    comp - a compiler instance;
//    tree - A GT_ASG tree that performs block copy.
//
// Return Value:
//    A possibly modified tree to perfom the copy.
//
// static
GenTree* MorphCopyBlockHelper::MorphCopyBlock(Compiler* comp, GenTree* tree)
{
    MorphCopyBlockHelper helper(comp, tree);
    return helper.Morph();
}

//------------------------------------------------------------------------
// MorphCopyBlockHelper: helper's constructor.
//
// Arguments:
//    comp - a compiler instance;
//    asg - GT_ASG node to morph.
//
// Notes:
//    Most class members are initialized via in-class member initializers.
//
MorphCopyBlockHelper::MorphCopyBlockHelper(Compiler* comp, GenTree* asg) : MorphInitBlockHelper(comp, asg, false)
{
}

//------------------------------------------------------------------------
// PrepareSrc: Transform the asg src to an appropriate form and initialize member fields
//    with information about it.
//
void MorphCopyBlockHelper::PrepareSrc()
{
    GenTree* origSrc = m_asg->gtGetOp2();
    m_src            = MorphBlock(m_comp, origSrc, false);
    if (m_src != origSrc)
    {
        m_asg->gtOp2 = m_src;
    }

    if (m_src->IsLocal())
    {
        m_srcLclNode = m_src->AsLclVarCommon();
        m_srcLclNum  = m_srcLclNode->GetLclNum();
        if (m_src->OperGet() == GT_LCL_FLD)
        {
            m_srcFldSeq = m_src->AsLclFld()->GetFieldSeq();
        }
    }
    else if (m_src->OperIsIndir())
    {
        if (m_src->AsOp()->gtOp1->IsLocalAddrExpr(m_comp, &m_srcLclNode, &m_srcFldSeq, &m_srcAddOff))
        {
            m_srcLclNum = m_srcLclNode->GetLclNum();
        }
        else
        {
            m_srcAddr = m_src->AsOp()->gtOp1;
        }
    }
    if (m_srcLclNum != BAD_VAR_NUM)
    {
        m_srcLclOffset = m_srcLclNode->GetLclOffs();
        m_srcVarDsc    = m_comp->lvaGetDesc(m_srcLclNum);
    }
}

// TrySpecialCases: check special cases that require special transformations.
//    The current special cases include asignments with calls in RHS.
//
// Notes:
//    It could change multiReg flags or change m_dst node.
//
void MorphCopyBlockHelper::TrySpecialCases()
{
#ifdef FEATURE_HW_INTRINSICS
    if (m_src->OperIsHWIntrinsic() && HWIntrinsicInfo::IsMultiReg(m_src->AsHWIntrinsic()->GetHWIntrinsicId()))
    {
        assert(m_src->IsMultiRegNode());
        JITDUMP("Not morphing a multireg intrinsic\n");
        m_transformationDecision = BlockTransformation::SkipMultiRegIntrinsicSrc;
        m_result                 = m_asg;
    }
#endif // FEATURE_HW_INTRINSICS

#if FEATURE_MULTIREG_RET
    // If this is a multi-reg return, we will not do any morphing of this node.
    if (m_src->IsMultiRegCall())
    {
        assert(m_dst->OperGet() == GT_LCL_VAR);
        JITDUMP("Not morphing a multireg call return\n");
        m_transformationDecision = BlockTransformation::SkipCallSrc;
        m_result                 = m_asg;
    }
    else if (m_dst->IsMultiRegLclVar() && !m_src->IsMultiRegNode())
    {
        m_dst->AsLclVar()->ClearMultiReg();
    }
#endif // FEATURE_MULTIREG_RET

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        if (m_src->IsCall())
        {
            if (m_dst->OperIs(GT_OBJ))
            {
                GenTreeLclVar* lclVar = m_comp->fgMorphTryFoldObjAsLclVar(m_dst->AsObj());
                if (lclVar != nullptr)
                {
                    m_dst        = lclVar;
                    m_asg->gtOp1 = lclVar;
                }
            }
            if (m_dst->OperIs(GT_LCL_VAR))
            {
                LclVarDsc* varDsc = m_comp->lvaGetDesc(m_dst->AsLclVar());
                if (varTypeIsStruct(varDsc) && varDsc->CanBeReplacedWithItsField(m_comp))
                {
                    m_dst->gtFlags |= GTF_DONT_CSE;
                    JITDUMP("Not morphing a single reg call return\n");
                    m_transformationDecision = BlockTransformation::SkipCallSrc;
                    m_result                 = m_asg;
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// MorphStructCases: transforms the asg as field by field copy or keeps it as a block init
//    but sets appropriate flags for the involved lclVars.
//
// Assumptions:
//    we have already checked that it is not a special case and can't be transformed as OneAsgBlock.
//
void MorphCopyBlockHelper::MorphStructCases()
{
    JITDUMP("block assignment to morph:\n");
    DISPTREE(m_asg);

    if (m_dstVarDsc != nullptr)
    {
        if (m_dstVarDsc->lvPromoted)
        {
            noway_assert(varTypeIsStruct(m_dstVarDsc));
            noway_assert(!m_comp->opts.MinOpts());

            if (m_blockSize == m_dstVarDsc->lvExactSize)
            {
                JITDUMP(" (m_dstDoFldAsg=true)");
                // We may decide later that a copyblk is required when this struct has holes
                m_dstDoFldAsg = true;
            }
            else
            {
                JITDUMP(" with mismatched dest size");
            }
        }
    }

    if (m_srcVarDsc != nullptr)
    {
        if (m_srcVarDsc->lvPromoted)
        {
            noway_assert(varTypeIsStruct(m_srcVarDsc));
            noway_assert(!m_comp->opts.MinOpts());

            if (m_blockSize == m_srcVarDsc->lvExactSize)
            {
                JITDUMP(" (m_srcDoFldAsg=true)");
                // We may decide later that a copyblk is required when this struct has holes
                m_srcDoFldAsg = true;
            }
            else
            {
                JITDUMP(" with mismatched src size");
            }
        }
    }

    // Check to see if we are doing a copy to/from the same local block.
    // If so, morph it to a nop.
    if ((m_dstVarDsc != nullptr) && (m_srcVarDsc == m_dstVarDsc) && (m_dstFldSeq == m_srcFldSeq) &&
        m_dstFldSeq != FieldSeqStore::NotAField())
    {
        JITDUMP("Self-copy; replaced with a NOP.\n");
        m_transformationDecision = BlockTransformation::Nop;
        GenTree* nop             = m_comp->gtNewNothingNode();
        m_result                 = nop;
        return;
    }

    // Check to see if we are required to do a copy block because the struct contains holes
    // and either the src or dest is externally visible
    //
    bool requiresCopyBlock = false;

    // If either src or dest is a reg-sized non-field-addressed struct, keep the copyBlock.
    if ((m_dstVarDsc != nullptr && m_dstVarDsc->lvRegStruct) || (m_srcVarDsc != nullptr && m_srcVarDsc->lvRegStruct))
    {
        requiresCopyBlock = true;
    }

    // Can we use field by field assignment for the dest?
    if (m_dstDoFldAsg && m_dstVarDsc->lvCustomLayout && m_dstVarDsc->lvContainsHoles)
    {
        JITDUMP(" dest contains custom layout and contains holes");
        // C++ style CopyBlock with holes
        requiresCopyBlock = true;
    }

    // Can we use field by field assignment for the src?
    if (m_srcDoFldAsg && m_srcVarDsc->lvCustomLayout && m_srcVarDsc->lvContainsHoles)
    {
        JITDUMP(" src contains custom layout and contains holes");
        // C++ style CopyBlock with holes
        requiresCopyBlock = true;
    }

#if defined(TARGET_ARM)
    if ((m_src->OperIsIndir()) && (m_src->gtFlags & GTF_IND_UNALIGNED))
    {
        JITDUMP(" src is unaligned");
        requiresCopyBlock = true;
    }

    if (m_asg->gtFlags & GTF_BLK_UNALIGNED)
    {
        JITDUMP(" m_asg is unaligned");
        requiresCopyBlock = true;
    }
#endif // TARGET_ARM

    // Don't use field by field assignment if the src is a call,
    // lowering will handle it without spilling the call result into memory
    // to access the individual fields.
    //
    if (m_src->OperGet() == GT_CALL)
    {
        JITDUMP(" src is a call");
        requiresCopyBlock = true;
    }

    // If we passed the above checks, then we will check these two
    if (!requiresCopyBlock)
    {
        // It is not always profitable to do field by field init for structs that are allocated to memory.
        // A struct with 8 bool fields will require 8 moves instead of one if we do this transformation.
        // A simple heuristic when field by field copy is prefered:
        // - if fields can be enregistered;
        // - if the struct has GCPtrs (block copy would be done via helper that is expensive);
        // - if the struct has only one field.
        bool dstFldIsProfitable =
            ((m_dstVarDsc != nullptr) &&
             (!m_dstVarDsc->lvDoNotEnregister || m_dstVarDsc->HasGCPtr() || (m_dstVarDsc->lvFieldCnt == 1)));
        bool srcFldIsProfitable =
            ((m_srcVarDsc != nullptr) &&
             (!m_srcVarDsc->lvDoNotEnregister || m_srcVarDsc->HasGCPtr() || (m_srcVarDsc->lvFieldCnt == 1)));
        // Are both dest and src promoted structs?
        if (m_dstDoFldAsg && m_srcDoFldAsg && (dstFldIsProfitable || srcFldIsProfitable))
        {
            // Both structs should be of the same type, or have the same number of fields of the same type.
            // If not we will use a copy block.
            bool misMatchedTypes = false;
            if (m_dstVarDsc->GetStructHnd() != m_srcVarDsc->GetStructHnd())
            {
                if (m_dstVarDsc->lvFieldCnt != m_srcVarDsc->lvFieldCnt)
                {
                    misMatchedTypes = true;
                }
                else
                {
                    for (int i = 0; i < m_dstVarDsc->lvFieldCnt; i++)
                    {
                        LclVarDsc* destFieldVarDsc = m_comp->lvaGetDesc(m_dstVarDsc->lvFieldLclStart + i);
                        LclVarDsc* srcFieldVarDsc  = m_comp->lvaGetDesc(m_srcVarDsc->lvFieldLclStart + i);
                        if ((destFieldVarDsc->lvType != srcFieldVarDsc->lvType) ||
                            (destFieldVarDsc->lvFldOffset != srcFieldVarDsc->lvFldOffset))
                        {
                            misMatchedTypes = true;
                            break;
                        }
                    }
                }
                if (misMatchedTypes)
                {
                    requiresCopyBlock = true; // Mismatched types, leave as a CopyBlock
                    JITDUMP(" with mismatched types");
                }
            }
        }
        else if (m_dstDoFldAsg && dstFldIsProfitable)
        {
            // Match the following kinds of trees:
            //  fgMorphTree BB01, stmt 9 (before)
            //   [000052] ------------        const     int    8
            //   [000053] -A--G-------     copyBlk   void
            //   [000051] ------------           addr      byref
            //   [000050] ------------              lclVar    long   V07 loc5
            //   [000054] --------R---        <list>    void
            //   [000049] ------------           addr      byref
            //   [000048] ------------              lclVar    struct(P) V06 loc4
            //                                              long   V06.h (offs=0x00) -> V17 tmp9
            // Yields this transformation
            //  fgMorphCopyBlock (after):
            //   [000050] ------------        lclVar    long   V07 loc5
            //   [000085] -A----------     =         long
            //   [000083] D------N----        lclVar    long   V17 tmp9
            //
            if ((m_dstVarDsc->lvFieldCnt == 1) && (m_srcVarDsc != nullptr) &&
                (m_blockSize == genTypeSize(m_srcVarDsc->TypeGet())))
            {
                // Reject the following tree:
                //  - seen on x86chk    jit\jit64\hfa\main\hfa_sf3E_r.exe
                //
                //  fgMorphTree BB01, stmt 6 (before)
                //   [000038] -------------        const     int    4
                //   [000039] -A--G--------     copyBlk   void
                //   [000037] -------------           addr      byref
                //   [000036] -------------              lclVar    int    V05 loc3
                //   [000040] --------R----        <list>    void
                //   [000035] -------------           addr      byref
                //   [000034] -------------              lclVar    struct(P) V04 loc2
                //                                          float  V04.f1 (offs=0x00) -> V13 tmp6
                // As this would framsform into
                //   float V13 = int V05
                //
                unsigned  fieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart;
                var_types destType    = m_comp->lvaGetDesc(fieldLclNum)->TypeGet();
                if (m_srcVarDsc->TypeGet() == destType)
                {
                    m_srcSingleLclVarAsg = true;
                }
            }
        }
        else if (m_srcDoFldAsg && srcFldIsProfitable)
        {
            // Check for the symmetric case (which happens for the _pointer field of promoted spans):
            //
            //               [000240] -----+------             /--*  lclVar    struct(P) V18 tmp9
            //                                                  /--*    byref  V18._value (offs=0x00) -> V30
            //                                                  tmp21
            //               [000245] -A------R---             *  =         struct (copy)
            //               [000244] -----+------             \--*  obj(8)    struct
            //               [000243] -----+------                \--*  addr      byref
            //               [000242] D----+-N----                   \--*  lclVar    byref  V28 tmp19
            //
            if ((m_srcVarDsc->lvFieldCnt == 1) && (m_dstVarDsc != nullptr) &&
                (m_blockSize == genTypeSize(m_dstVarDsc->TypeGet())))
            {
                // Check for type agreement
                unsigned  fieldLclNum = m_comp->lvaGetDesc(m_srcLclNum)->lvFieldLclStart;
                var_types srcType     = m_comp->lvaGetDesc(fieldLclNum)->TypeGet();
                if (m_dstVarDsc->TypeGet() == srcType)
                {
                    m_dstSingleLclVarAsg = true;
                }
            }
        }
        // Are neither dest or src promoted structs?
        else
        {
            assert(!(m_dstDoFldAsg && dstFldIsProfitable) && !(m_srcDoFldAsg && srcFldIsProfitable));
            requiresCopyBlock = true; // Leave as a CopyBlock
            JITDUMP(" with no promoted structs");
        }
    }

    // If we require a copy block the set both of the field assign bools to false
    if (requiresCopyBlock)
    {
        // If a copy block is required then we won't do field by field assignments
        m_dstDoFldAsg = false;
        m_srcDoFldAsg = false;
    }

    JITDUMP(requiresCopyBlock ? " this requires a CopyBlock.\n" : " using field by field assignments.\n");

    if (requiresCopyBlock)
    {
        const var_types asgType   = m_dst->TypeGet();
        bool            isBlkReqd = (asgType == TYP_STRUCT);
        m_dst                     = m_comp->fgMorphBlockOperand(m_dst, asgType, m_blockSize, isBlkReqd);
        m_dst->gtFlags |= GTF_DONT_CSE;
        m_asg->gtOp1 = m_dst;
        m_asg->gtFlags |= (m_dst->gtFlags & GTF_ALL_EFFECT);

        // Eliminate the "OBJ or BLK" node on the src.
        m_src        = m_comp->fgMorphBlockOperand(m_src, asgType, m_blockSize, false /*!isBlkReqd*/);
        m_asg->gtOp2 = m_src;

        m_result                 = m_asg;
        m_transformationDecision = BlockTransformation::StructBlock;
    }
    else
    {
        m_result                 = CopyFieldByField();
        m_transformationDecision = BlockTransformation::FieldByField;
    }

    // Mark the dest/src structs as DoNotEnreg when they are not being fully referenced as the same type.
    //
    if (!m_dstDoFldAsg && (m_dstVarDsc != nullptr) && !m_dstSingleLclVarAsg)
    {
        if (m_dst != m_dstLclNode)
        {
            // Mark it as DoNotEnregister.
            m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::CastTakesAddr));
        }
        else if (m_dstVarDsc->lvPromoted)
        {
            // Mark it as DoNotEnregister.
            m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }
        else if (m_dst->IsMultiRegLclVar())
        {
            // Handle this as lvIsMultiRegRet; this signals to SSA that it can't consider these fields
            // SSA candidates (we don't have a way to represent multiple SSANums on MultiRegLclVar nodes).
            m_dstVarDsc->lvIsMultiRegRet = true;
        }
    }

    if (!m_srcDoFldAsg && (m_srcVarDsc != nullptr) && !m_srcSingleLclVarAsg)
    {
        if (m_src != m_srcLclNode)
        {
            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::CastTakesAddr));
        }
        else if (m_srcVarDsc->lvPromoted)
        {
            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }
    }
}

//------------------------------------------------------------------------
// CopyFieldByField: transform the copy block to a field by field asignment.
//
// Notes:
//    We do it for promoted lclVars which fields can be enregistered.
//
GenTree* MorphCopyBlockHelper::CopyFieldByField()
{
    GenTreeOp* asgFields = nullptr;

    GenTree* addrSpill          = nullptr;
    unsigned addrSpillSrcLclNum = BAD_VAR_NUM;
    unsigned addrSpillTemp      = BAD_VAR_NUM;

    GenTree* addrSpillAsg = nullptr;

    unsigned fieldCnt = DUMMY_INIT(0);

    if (m_dstDoFldAsg && m_srcDoFldAsg)
    {
        // To do fieldwise assignments for both sides.
        // The structs do not have to be the same exact types but have to have same field types
        // at the same offsets.
        assert(m_dstLclNum != BAD_VAR_NUM && m_srcLclNum != BAD_VAR_NUM);
        assert(m_dstVarDsc != nullptr && m_srcVarDsc != nullptr && m_dstVarDsc->lvFieldCnt == m_srcVarDsc->lvFieldCnt);

        fieldCnt = m_dstVarDsc->lvFieldCnt;
    }
    else if (m_dstDoFldAsg)
    {
        fieldCnt = m_dstVarDsc->lvFieldCnt;
        m_src    = m_comp->fgMorphBlockOperand(m_src, m_asg->TypeGet(), m_blockSize, false /*isBlkReqd*/);

        m_srcUseLclFld = m_comp->fgMorphCanUseLclFldForCopy(m_dstLclNum, m_srcLclNum);

        if (!m_srcUseLclFld && m_srcAddr == nullptr)
        {
            m_srcAddr = m_comp->fgMorphGetStructAddr(&m_src, m_dstVarDsc->GetStructHnd(), true /* rValue */);
        }

        if (!m_srcUseLclFld)
        {
            if (m_comp->gtClone(m_srcAddr))
            {
                // m_srcAddr is simple expression. No need to spill.
                noway_assert((m_srcAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
            }
            else
            {
                // m_srcAddr is complex expression. Clone and spill it (unless the destination is
                // a struct local that only has one field, in which case we'd only use the
                // address value once...)
                if (m_dstVarDsc->lvFieldCnt > 1)
                {
                    // We will spill m_srcAddr (i.e. assign to a temp "BlockOp address local")
                    // no need to clone a new copy as it is only used once
                    //
                    addrSpill          = m_srcAddr; // addrSpill represents the 'm_srcAddr'
                    addrSpillSrcLclNum = m_srcLclNum;
                }
            }
        }
    }
    else
    {
        assert(m_srcDoFldAsg);
        fieldCnt = m_srcVarDsc->lvFieldCnt;
        m_dst    = m_comp->fgMorphBlockOperand(m_dst, m_dst->TypeGet(), m_blockSize, false /*isBlkReqd*/);
        if (m_dst->OperIsBlk())
        {
            m_dst->SetOper(GT_IND);
            m_dst->gtType = TYP_STRUCT;
        }
        m_dstUseLclFld = m_comp->fgMorphCanUseLclFldForCopy(m_srcLclNum, m_dstLclNum);
        if (!m_dstUseLclFld)
        {
            m_dstAddr = m_comp->gtNewOperNode(GT_ADDR, TYP_BYREF, m_dst);
        }

        // If we're doing field-wise stores, to an address within a local, and we copy
        // the address into "addrSpill", do *not* declare the original local var node in the
        // field address as GTF_VAR_DEF and GTF_VAR_USEASG; we will declare each of the
        // field-wise assignments as an "indirect" assignment to the local.
        // ("m_dstLclNode" is a subtree of "m_dstAddr"; make sure we remove the flags before
        // we clone it.)
        if (m_dstLclNode != nullptr)
        {
            m_dstLclNode->gtFlags &= ~(GTF_VAR_DEF | GTF_VAR_USEASG);
        }

        if (!m_dstUseLclFld)
        {
            if (m_comp->gtClone(m_dstAddr))
            {
                // m_dstAddr is simple expression. No need to spill
                noway_assert((m_dstAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
            }
            else
            {
                // m_dstAddr is complex expression. Clone and spill it (unless
                // the source is a struct local that only has one field, in which case we'd only
                // use the address value once...)
                if (m_srcVarDsc->lvFieldCnt > 1)
                {
                    // We will spill m_dstAddr (i.e. assign to a temp "BlockOp address local")
                    // no need to clone a new copy as it is only used once
                    //
                    addrSpill          = m_dstAddr; // addrSpill represents the 'm_dstAddr'
                    addrSpillSrcLclNum = m_dstLclNum;
                }
            }
        }
    }

    if (addrSpill != nullptr)
    {
        // 'addrSpill' is already morphed

        // Spill the (complex) address to a BYREF temp.
        // Note, at most one address may need to be spilled.
        addrSpillTemp = m_comp->lvaGrabTemp(true DEBUGARG("BlockOp address local"));

        LclVarDsc* addrSpillDsc = m_comp->lvaGetDesc(addrSpillTemp);

        addrSpillDsc->lvType = TYP_BYREF;

        if (addrSpillSrcLclNum != BAD_VAR_NUM)
        {
            // addrSpill represents the address of LclVar[varNum] in our local stack frame.
            addrSpillDsc->lvStackByref = true;
        }

        GenTreeLclVar* addrSpillNode = m_comp->gtNewLclvNode(addrSpillTemp, TYP_BYREF);
        addrSpillAsg                 = m_comp->gtNewAssignNode(addrSpillNode, addrSpill);

        // If we are assigning the address of a LclVar here liveness will not
        // account for this kind of address taken use. Mark the local as
        // address-exposed so that we don't do illegal optimizations with it.
        //
        // TODO-CQ: usage of "addrSpill" for local addresses is a workaround
        // for cases where we fail to use LCL_FLD nodes instead. Fix them and
        // delete this code.
        //
        if (addrSpillSrcLclNum != BAD_VAR_NUM)
        {
            m_comp->lvaSetVarAddrExposed(addrSpillSrcLclNum DEBUGARG(AddressExposedReason::COPY_FLD_BY_FLD));
        }
    }

    // We may have allocated a temp above, and that may have caused the lvaTable to be expanded.
    // So, beyond this point we cannot rely on the old values of 'm_srcVarDsc' and 'm_dstVarDsc'.
    for (unsigned i = 0; i < fieldCnt; ++i)
    {
        GenTree* dstFld;
        if (m_dstDoFldAsg)
        {
            noway_assert(m_dstLclNum != BAD_VAR_NUM);
            unsigned dstFieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart + i;
            dstFld = m_comp->gtNewLclvNode(dstFieldLclNum, m_comp->lvaGetDesc(dstFieldLclNum)->TypeGet());
            // If it had been labeled a "USEASG", assignments to the individual promoted fields are not.
            if (m_dstAddr != nullptr)
            {
                noway_assert(m_dstAddr->AsOp()->gtOp1->gtOper == GT_LCL_VAR);
                dstFld->gtFlags |= m_dstAddr->AsOp()->gtOp1->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);
            }
            else
            {
                noway_assert(m_dstLclNode != nullptr);
                dstFld->gtFlags |= m_dstLclNode->gtFlags & ~(GTF_NODE_MASK | GTF_VAR_USEASG);
            }
            // Don't CSE the lhs of an assignment.
            dstFld->gtFlags |= GTF_DONT_CSE;
        }
        else
        {
            noway_assert(m_srcDoFldAsg);

            if (m_dstSingleLclVarAsg)
            {
                noway_assert(fieldCnt == 1);
                noway_assert(m_dstVarDsc != nullptr);
                noway_assert(addrSpill == nullptr);

                dstFld = m_comp->gtNewLclvNode(m_dstLclNum, m_dstVarDsc->TypeGet());
            }
            else
            {
                GenTree* dstAddrClone = nullptr;
                if (!m_dstUseLclFld)
                {
                    // Need address of the destination.
                    if (addrSpill)
                    {
                        assert(addrSpillTemp != BAD_VAR_NUM);
                        dstAddrClone = m_comp->gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                    }
                    else
                    {
                        if (i == (fieldCnt - 1))
                        {
                            // Reuse the orginal m_dstAddr tree for the last field.
                            dstAddrClone = m_dstAddr;
                        }
                        else
                        {
                            // We can't clone multiple copies of a tree with persistent side effects
                            noway_assert((m_dstAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);

                            dstAddrClone = m_comp->gtCloneExpr(m_dstAddr);
                            noway_assert(dstAddrClone != nullptr);

                            JITDUMP("dstAddr - Multiple Fields Clone created:\n");
                            DISPTREE(dstAddrClone);

                            // Morph the newly created tree
                            dstAddrClone = m_comp->fgMorphTree(dstAddrClone);
                        }

                        // Is the address of a local?
                        GenTreeLclVarCommon* lclVarTree = nullptr;
                        bool                 isEntire   = false;
                        bool*                pIsEntire  = &isEntire;
                        if (dstAddrClone->DefinesLocalAddr(m_comp, m_blockSize, &lclVarTree, pIsEntire))
                        {
                            lclVarTree->gtFlags |= GTF_VAR_DEF;
                            if (!isEntire)
                            {
                                lclVarTree->gtFlags |= GTF_VAR_USEASG;
                            }
                        }
                    }
                }

                LclVarDsc* srcVarDsc      = m_comp->lvaGetDesc(m_srcLclNum);
                unsigned   srcFieldLclNum = srcVarDsc->lvFieldLclStart + i;
                LclVarDsc* srcFieldVarDsc = m_comp->lvaGetDesc(srcFieldLclNum);

                // Have to set the field sequence -- which means we need the field handle.
                CORINFO_CLASS_HANDLE classHnd = srcVarDsc->GetStructHnd();
                CORINFO_FIELD_HANDLE fieldHnd =
                    m_comp->info.compCompHnd->getFieldInClass(classHnd, srcFieldVarDsc->lvFldOrdinal);
                FieldSeqNode* curFieldSeq = m_comp->GetFieldSeqStore()->CreateSingleton(fieldHnd);

                unsigned  srcFieldOffset = m_comp->lvaGetDesc(srcFieldLclNum)->lvFldOffset;
                var_types srcType        = srcFieldVarDsc->TypeGet();

                if (!m_dstUseLclFld)
                {

                    if (srcFieldOffset == 0)
                    {
                        m_comp->fgAddFieldSeqForZeroOffset(dstAddrClone, curFieldSeq);
                    }
                    else
                    {
                        GenTree* fieldOffsetNode = m_comp->gtNewIconNode(srcFieldVarDsc->lvFldOffset, curFieldSeq);
                        dstAddrClone = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, dstAddrClone, fieldOffsetNode);
                    }

                    dstFld = m_comp->gtNewIndir(srcType, dstAddrClone);
                }
                else
                {
                    assert(m_dstAddOff == 0);
                    assert(dstAddrClone == nullptr);
                    assert((m_dstLclOffset == 0) || (m_dstFldSeq != nullptr));
                    // If the dst was a struct type field "B" in a struct "A" then we add
                    // add offset of ("B" in "A") + current offset in "B".
                    unsigned summOffset        = m_dstLclOffset + srcFieldOffset;
                    dstFld                     = m_comp->gtNewLclFldNode(m_dstLclNum, srcType, summOffset);
                    FieldSeqNode* dstFldFldSeq = m_comp->GetFieldSeqStore()->Append(m_dstFldSeq, curFieldSeq);
                    dstFld->AsLclFld()->SetFieldSeq(dstFldFldSeq);

                    // TODO-1stClassStructs: remove this and implement storing to a field in a struct in a reg.
                    m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                }

                // !!! The destination could be on stack. !!!
                // This flag will let us choose the correct write barrier.
                dstFld->gtFlags |= GTF_IND_TGTANYWHERE;
            }
        }

        GenTree* srcFld = nullptr;
        if (m_srcDoFldAsg)
        {
            noway_assert((m_srcLclNum != BAD_VAR_NUM) && (m_srcLclNode != nullptr));
            unsigned srcFieldLclNum = m_comp->lvaGetDesc(m_srcLclNum)->lvFieldLclStart + i;

            srcFld = m_comp->gtNewLclvNode(srcFieldLclNum, m_comp->lvaGetDesc(srcFieldLclNum)->TypeGet());
        }
        else
        {
            noway_assert(m_dstDoFldAsg);
            noway_assert(m_dstLclNum != BAD_VAR_NUM);
            unsigned dstFieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart + i;

            if (m_srcSingleLclVarAsg)
            {
                noway_assert(fieldCnt == 1);
                noway_assert(m_srcLclNum != BAD_VAR_NUM);
                noway_assert(addrSpill == nullptr);

                srcFld = m_comp->gtNewLclvNode(m_srcLclNum, m_comp->lvaGetDesc(m_srcLclNum)->TypeGet());
            }
            else
            {
                GenTree* srcAddrClone = nullptr;
                if (!m_srcUseLclFld)
                {
                    // Need address of the source.
                    if (addrSpill)
                    {
                        assert(addrSpillTemp != BAD_VAR_NUM);
                        srcAddrClone = m_comp->gtNewLclvNode(addrSpillTemp, TYP_BYREF);
                    }
                    else
                    {
                        if (i == (fieldCnt - 1))
                        {
                            // Reuse the orginal m_srcAddr tree for the last field.
                            srcAddrClone = m_srcAddr;
                        }
                        else
                        {
                            // We can't clone multiple copies of a tree with persistent side effects
                            noway_assert((m_srcAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);

                            srcAddrClone = m_comp->gtCloneExpr(m_srcAddr);
                            noway_assert(srcAddrClone != nullptr);

                            JITDUMP("m_srcAddr - Multiple Fields Clone created:\n");
                            DISPTREE(srcAddrClone);

                            // Morph the newly created tree
                            srcAddrClone = m_comp->fgMorphTree(srcAddrClone);
                        }
                    }
                }

                CORINFO_CLASS_HANDLE classHnd = m_comp->lvaGetDesc(m_dstLclNum)->GetStructHnd();
                CORINFO_FIELD_HANDLE fieldHnd =
                    m_comp->info.compCompHnd->getFieldInClass(classHnd,
                                                              m_comp->lvaGetDesc(dstFieldLclNum)->lvFldOrdinal);
                FieldSeqNode* curFieldSeq = m_comp->GetFieldSeqStore()->CreateSingleton(fieldHnd);
                var_types     destType    = m_comp->lvaGetDesc(dstFieldLclNum)->lvType;

                bool done = false;
                if (m_comp->lvaGetDesc(dstFieldLclNum)->lvFldOffset == 0)
                {
                    // If this is a full-width use of the src via a different type, we need to create a
                    // GT_LCL_FLD.
                    // (Note that if it was the same type, 'm_srcSingleLclVarAsg' would be true.)
                    if (m_srcLclNum != BAD_VAR_NUM)
                    {
                        noway_assert(m_srcLclNode != nullptr);
                        assert(destType != TYP_STRUCT);
                        unsigned destSize = genTypeSize(destType);
                        m_srcVarDsc       = m_comp->lvaGetDesc(m_srcLclNum);
                        unsigned srcSize =
                            (m_srcVarDsc->lvType == TYP_STRUCT) ? m_srcVarDsc->lvExactSize : genTypeSize(m_srcVarDsc);
                        if (destSize == srcSize)
                        {
                            m_srcLclNode->gtFlags |= GTF_VAR_CAST;
                            m_srcLclNode->ChangeOper(GT_LCL_FLD);
                            m_srcLclNode->gtType = destType;
                            m_srcLclNode->AsLclFld()->SetFieldSeq(curFieldSeq);
                            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                            srcFld = m_srcLclNode;
                            done   = true;
                        }
                    }
                }
                if (!done)
                {
                    unsigned fldOffset = m_comp->lvaGetDesc(dstFieldLclNum)->lvFldOffset;
                    if (!m_srcUseLclFld)
                    {
                        assert(srcAddrClone != nullptr);
                        if (fldOffset == 0)
                        {
                            m_comp->fgAddFieldSeqForZeroOffset(srcAddrClone, curFieldSeq);
                        }
                        else
                        {
                            GenTreeIntCon* fldOffsetNode = m_comp->gtNewIconNode(fldOffset, curFieldSeq);
                            srcAddrClone = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, srcAddrClone, fldOffsetNode);
                        }
                        srcFld = m_comp->gtNewIndir(destType, srcAddrClone);
                    }
                    else
                    {
                        assert((m_srcLclOffset == 0) || (m_srcFldSeq != 0));
                        assert(m_srcAddOff == 0);
                        // If the src was a struct type field "B" in a struct "A" then we add
                        // add offset of ("B" in "A") + current offset in "B".
                        unsigned summOffset        = m_srcLclOffset + fldOffset;
                        srcFld                     = m_comp->gtNewLclFldNode(m_srcLclNum, destType, summOffset);
                        FieldSeqNode* srcFldFldSeq = m_comp->GetFieldSeqStore()->Append(m_srcFldSeq, curFieldSeq);
                        srcFld->AsLclFld()->SetFieldSeq(srcFldFldSeq);
                        // TODO-1stClassStructs: remove this and implement reading a field from a struct in a
                        // reg.
                        m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                    }
                }
            }
        }
        assert(srcFld != nullptr);
        noway_assert(dstFld->TypeGet() == srcFld->TypeGet());

        GenTreeOp* asgOneFld = m_comp->gtNewAssignNode(dstFld, srcFld);

        // If we spilled the address, and we didn't do individual field assignments to promoted fields,
        // and it was of a local, ensure that the destination local variable has been marked as address
        // exposed. Neither liveness nor SSA are able to track this kind of indirect assignments.
        if (addrSpill && !m_dstDoFldAsg && m_dstLclNum != BAD_VAR_NUM)
        {
            noway_assert(m_comp->lvaGetDesc(m_dstLclNum)->IsAddressExposed());
        }

        if (m_comp->optLocalAssertionProp)
        {
            m_comp->optAssertionGen(asgOneFld);
        }

        if (addrSpillAsg != nullptr)
        {
            asgFields    = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, addrSpillAsg, asgOneFld)->AsOp();
            addrSpillAsg = nullptr;
        }
        else if (asgFields != nullptr)
        {
            asgFields = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, asgFields, asgOneFld)->AsOp();
        }
        else
        {
            asgFields = asgOneFld;
        }
    }
    return asgFields;
}

//------------------------------------------------------------------------
// fgMorphCopyBlock: Perform the morphing of a block copy.
//
// Arguments:
//    tree - a block copy (i.e. an assignment with a block op on the lhs).
//
// Return Value:
//    We can return the orginal block copy unmodified (least desirable, but always correct)
//    We can return a single assignment, when fgMorphOneAsgBlockOp transforms it (most desirable).
//    If we have performed struct promotion of the Source() or the Dest() then we will try to
//    perform a field by field assignment for each of the promoted struct fields.
//
// Assumptions:
//    The child nodes for tree have already been Morphed.
//
// Notes:
//    If we leave it as a block copy we will call lvaSetVarDoNotEnregister() on Source() or Dest()
//    if they cannot be enregistered.
//    When performing a field by field assignment we can have one of Source() or Dest treated as a blob of bytes
//    and in such cases we will call lvaSetVarDoNotEnregister() on the one treated as a blob of bytes.
//    If the Source() or Dest() is a struct that has a "CustomLayout" and "ConstainsHoles" then we
//    can not use a field by field assignment and must leave the orginal block copy unmodified.
//
GenTree* Compiler::fgMorphCopyBlock(GenTree* tree)
{
    return MorphCopyBlockHelper::MorphCopyBlock(this, tree);
}

//------------------------------------------------------------------------
// fgMorphInitBlock: Morph a block initialization assignment tree.
//
// Arguments:
//    tree - A GT_ASG tree that performs block initialization.
//
// Return Value:
//    A single assignment, when fgMorphOneAsgBlockOp transforms it.
//    If the destination is a promoted struct local variable then we will try to
//    perform a field by field assignment for each of the promoted struct fields.
//    This is not always possible (e.g. if the struct is address exposed).
//
//    Otherwise the orginal GT_ASG tree is returned unmodified, note that the
//    nodes can still be changed.
//
// Assumptions:
//    GT_ASG's children have already been morphed.
//
GenTree* Compiler::fgMorphInitBlock(GenTree* tree)
{
    return MorphInitBlockHelper::MorphInitBlock(this, tree);
}

//------------------------------------------------------------------------
// fgMorphStoreDynBlock: Morph a dynamic block store (GT_STORE_DYN_BLK).
//
// Performs full (pre-order and post-order) morphing for a STORE_DYN_BLK.
//
// Arguments:
//    tree - The GT_STORE_DYN_BLK tree to morph.
//
// Return Value:
//    In case the size turns into a constant - the store, transformed
//    into an "ordinary" ASG(BLK, Data()) one, and further morphed by
//    "fgMorphInitBlock"/"fgMorphCopyBlock". Otherwise, the original
//    tree (fully morphed).
//
GenTree* Compiler::fgMorphStoreDynBlock(GenTreeStoreDynBlk* tree)
{
    tree->Addr()        = fgMorphTree(tree->Addr());
    tree->Data()        = fgMorphTree(tree->Data());
    tree->gtDynamicSize = fgMorphTree(tree->gtDynamicSize);

    if (tree->gtDynamicSize->IsIntegralConst())
    {
        int64_t size = tree->gtDynamicSize->AsIntConCommon()->IntegralValue();
        assert(FitsIn<int32_t>(size));

        if (size != 0)
        {
            GenTree* lhs = gtNewBlockVal(tree->Addr(), static_cast<unsigned>(size));
            lhs->SetIndirExceptionFlags(this);

            GenTree* asg = gtNewAssignNode(lhs, tree->Data());
            asg->gtFlags |= (tree->gtFlags & (GTF_ALL_EFFECT | GTF_BLK_VOLATILE | GTF_BLK_UNALIGNED));
            INDEBUG(asg->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);

            JITDUMP("MorphStoreDynBlock: trasformed STORE_DYN_BLK into ASG(BLK, Data())\n");

            return tree->OperIsCopyBlkOp() ? fgMorphCopyBlock(asg) : fgMorphInitBlock(asg);
        }
    }

    tree->SetAllEffectsFlags(tree->Addr(), tree->Data(), tree->gtDynamicSize);
    tree->SetIndirExceptionFlags(this);
    tree->gtFlags |= GTF_ASG;

    return tree;
}
