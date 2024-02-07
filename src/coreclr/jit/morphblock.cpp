// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

class MorphInitBlockHelper
{
public:
    static GenTree* MorphInitBlock(Compiler* comp, GenTree* tree);

protected:
    MorphInitBlockHelper(Compiler* comp, GenTree* store, bool initBlock);

    GenTree* Morph();

    void         PrepareDst();
    virtual void PrepareSrc();

    virtual void TrySpecialCases();
    virtual void MorphStructCases();

    void PropagateBlockAssertions();
    void PropagateExpansionAssertions();

    virtual const char* GetHelperName() const
    {
        return "MorphInitBlock";
    }

private:
    void     TryInitFieldByField();
    void     TryPrimitiveInit();
    GenTree* EliminateCommas(GenTree** commaPool);

protected:
    Compiler* m_comp;
    bool      m_initBlock;

    GenTree* m_store = nullptr;
    GenTree* m_src   = nullptr;

    unsigned             m_blockSize            = 0;
    ClassLayout*         m_blockLayout          = nullptr;
    unsigned             m_dstLclNum            = BAD_VAR_NUM;
    GenTreeLclVarCommon* m_dstLclNode           = nullptr;
    LclVarDsc*           m_dstVarDsc            = nullptr;
    unsigned             m_dstLclOffset         = 0;
    bool                 m_dstUseLclFld         = false;
    bool                 m_dstSingleStoreLclVar = false;

    enum class BlockTransformation
    {
        Undefined,
        FieldByField,
        OneStoreBlock,
        StructBlock,
        SkipMultiRegSrc,
        SkipSingleRegCallSrc,
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
//    tree - A store tree that performs block initialization.
//
// Return Value:
//    A possibly modified tree to perform the initialization.
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
//    store - store node to morph.
//
// Notes:
//    Most class members are initialized via in-class member initializers.
//
MorphInitBlockHelper::MorphInitBlockHelper(Compiler* comp, GenTree* store, bool initBlock = true)
    : m_comp(comp), m_initBlock(initBlock)
{
    assert(store->OperIsStore());
    assert((m_initBlock == store->OperIsInitBlkOp()) && (!m_initBlock == store->OperIsCopyBlkOp()));
    m_store = store;
}

//------------------------------------------------------------------------
// Morph: transform the store to a possible better form and changes its
//    operands to an appropriate form for later phases.
//
// Return Value:
//    A possibly modified tree to perform the block operation.
//
// Notes:
//    It is used for both init and copy block.
//
GenTree* MorphInitBlockHelper::Morph()
{
    JITDUMP("%s:\n", GetHelperName());

    GenTree* commaPool;
    GenTree* sideEffects = EliminateCommas(&commaPool);

    PrepareDst();
    PrepareSrc();
    PropagateBlockAssertions();
    TrySpecialCases();

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        MorphStructCases();
    }

    PropagateExpansionAssertions();

    assert(m_transformationDecision != BlockTransformation::Undefined);
    assert(m_result != nullptr);

#ifdef DEBUG
    // If we are going to return a different node than the input then morph
    // expects us to have set GTF_DEBUG_NODE_MORPHED.
    if ((m_result != m_store) || (sideEffects != nullptr))
    {
        m_result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED;
    }
#endif

    while (sideEffects != nullptr)
    {
        if (commaPool != nullptr)
        {
            GenTree* comma = commaPool;
            commaPool      = commaPool->gtNext;

            assert(comma->OperIs(GT_COMMA));
            comma->gtType        = TYP_VOID;
            comma->AsOp()->gtOp1 = sideEffects;
            comma->AsOp()->gtOp2 = m_result;
            comma->gtFlags       = (sideEffects->gtFlags | m_result->gtFlags) & GTF_ALL_EFFECT;

            m_result = comma;
        }
        else
        {
            m_result = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, sideEffects, m_result);
        }
        INDEBUG(m_result->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);

        sideEffects = sideEffects->gtNext;
    }

    JITDUMP("%s (after):\n", GetHelperName());
    DISPTREE(m_result);

    return m_result;
}

//------------------------------------------------------------------------
// PrepareDst: Initialize member fields with information about the store's
//    destination.
//
void MorphInitBlockHelper::PrepareDst()
{
    if (m_store->OperIsLocalStore())
    {
        m_dstLclNode   = m_store->AsLclVarCommon();
        m_dstLclOffset = m_dstLclNode->GetLclOffs();
        m_dstLclNum    = m_dstLclNode->GetLclNum();
        m_dstVarDsc    = m_comp->lvaGetDesc(m_dstLclNum);

        // Kill everything about m_dstLclNum (and its field locals)
        if (m_comp->optLocalAssertionProp && (m_comp->optAssertionCount > 0))
        {
            m_comp->fgKillDependentAssertions(m_dstLclNum DEBUGARG(m_store));
        }
    }
    else
    {
        assert(m_store->OperIs(GT_STOREIND, GT_STORE_BLK));
    }

    if (m_store->TypeIs(TYP_STRUCT))
    {
        m_blockLayout = m_store->GetLayout(m_comp);
        m_blockSize   = m_blockLayout->GetSize();
    }
    else
    {
        m_blockSize = genTypeSize(m_store);
    }

    assert(m_blockSize != 0);

#if defined(DEBUG)
    if (m_comp->verbose)
    {
        printf("PrepareDst for [%06u] ", m_comp->dspTreeID(m_store));
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
// PropagateBlockAssertions: propagate assertions based on the original tree
//
// Notes:
//    Once the init or copy tree is morphed, assertion gen can no
//    longer recognize what it means.
//
//    So we generate assertions based on the original tree.
//
void MorphInitBlockHelper::PropagateBlockAssertions()
{
    if (m_comp->optLocalAssertionProp)
    {
        m_comp->fgAssertionGen(m_store);
    }
}

//------------------------------------------------------------------------
// PropagateExpansionAssertions: propagate assertions based on the
//   expanded tree
//
// Notes:
//    After the copy/init is expanded, we may see additional assertions
//    to generate.
//
void MorphInitBlockHelper::PropagateExpansionAssertions()
{
    // Consider doing this for FieldByField as well
    //
    if (m_comp->optLocalAssertionProp && (m_transformationDecision == BlockTransformation::OneStoreBlock))
    {
        m_comp->fgAssertionGen(m_store);
    }
}

//------------------------------------------------------------------------
// PrepareSrc: Initialize member fields with information about the store's
//    source value.
//
void MorphInitBlockHelper::PrepareSrc()
{
    m_src = m_store->Data();
}

//------------------------------------------------------------------------
// TrySpecialCases: check special cases that require special transformations.
//    We don't have any for init block.
//
void MorphInitBlockHelper::TrySpecialCases()
{
    return;
}

//------------------------------------------------------------------------
// MorphStructCases: transforms the store as field by field init or keeps it as a block init
//    but sets appropriate flags for the involved lclVars.
//
// Assumptions:
//    we have already checked that it is not a special case.
//
void MorphInitBlockHelper::MorphStructCases()
{
    // See if we can use the promoted fields to initialize the local: if we have already determined
    // that a promoted local will not be enregistered, we are better off doing a block init.
    if ((m_dstVarDsc != nullptr) && m_dstVarDsc->lvPromoted && !m_dstVarDsc->lvDoNotEnregister)
    {
        TryInitFieldByField();
    }

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        TryPrimitiveInit();
    }

    if (m_transformationDecision == BlockTransformation::Undefined)
    {
        m_result                 = m_store;
        m_transformationDecision = BlockTransformation::StructBlock;

        if (m_dstVarDsc != nullptr)
        {
            if (m_store->OperIs(GT_STORE_LCL_FLD))
            {
                m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
            }
            else if (m_dstVarDsc->lvPromoted)
            {
                m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
            }
        }
    }
}

//------------------------------------------------------------------------
// InitFieldByField: Attempts to promote a local block init tree to a tree
// of promoted field initialization assignments.
//
// If successful, will set "m_transformationDecision" to "FieldByField" and
// "m_result" to the final tree.
//
// Notes:
//    This transforms a single block initialization assignment like:
//
//    *  STORE_BLK struct<12> (init)
//    +--*  LCL_ADDR  byref V02 loc0
//    |  \--  int    V02.a (offs=0x00) -> V06 tmp3
//    |  \--  ubyte  V02.c (offs=0x04) -> V07 tmp4
//    |  \--  float  V02.d (offs=0x08) -> V08 tmp5
//    \--*  INIT_VAL  int
//       \--*  CNS_INT   int    42
//
//    into a COMMA tree of stores that initialize each promoted struct
//    field:
//
//    *  COMMA     void
//    +--*  COMMA     void
//    |  +--*  STORE_LCL_VAR int    V06 tmp3
//    |  |  \--*  CNS_INT   int    0x2A2A2A2A
//    |  \--*  STORE_LCL_VAR ubyte  V07 tmp4
//    |     \--*  CNS_INT   int    42
//    \--*  STORE_LCL_VAR float  V08 tmp5
//       \--*  CNS_DBL   float  1.5113661732714390e-13
//
void MorphInitBlockHelper::TryInitFieldByField()
{
    assert((m_dstVarDsc != nullptr) && (m_dstVarDsc->lvPromoted));

    LclVarDsc* destLclVar = m_dstVarDsc;
    unsigned   blockSize  = m_blockSize;

    if (destLclVar->IsAddressExposed() && destLclVar->lvContainsHoles)
    {
        JITDUMP(" dest is address exposed and contains holes.\n");
        return;
    }

    if (destLclVar->lvAnySignificantPadding)
    {
        JITDUMP(" dest has significant padding.\n");
        return;
    }

    if (m_dstLclOffset != 0)
    {
        JITDUMP(" dest not at a zero offset.\n");
        return;
    }

    if (destLclVar->lvExactSize() != blockSize)
    {
        JITDUMP(" dest size mismatch.\n");
        return;
    }

    GenTree* initVal = m_src->OperIsInitVal() ? m_src->gtGetOp1() : m_src;

    if (!initVal->OperIs(GT_CNS_INT))
    {
        JITDUMP(" source is not constant.\n");
        return;
    }

    const uint8_t initPattern = (uint8_t)(initVal->AsIntCon()->IconValue() & 0xFF);

    if (initPattern != 0)
    {
        for (unsigned i = 0; i < destLclVar->lvFieldCnt; ++i)
        {
            LclVarDsc* fieldDesc = m_comp->lvaGetDesc(destLclVar->lvFieldLclStart + i);

            if (varTypeIsGC(fieldDesc))
            {
                // Cannot initialize GC types with a non-zero constant. The
                // former is completely bogus.
                JITDUMP(" dest contains GC fields and source constant is not 0.\n");
                return;
            }
        }
    }

    JITDUMP(" using field by field initialization.\n");

    GenTree* tree = nullptr;

    for (unsigned i = 0; i < destLclVar->lvFieldCnt; ++i)
    {
        unsigned fieldLclNum = destLclVar->lvFieldLclStart + i;

        if (m_comp->fgGlobalMorph && m_dstLclNode->IsLastUse(i))
        {
            JITDUMP("Field-by-field init skipping write to dead field V%02u\n", fieldLclNum);
            continue;
        }

        LclVarDsc* fieldDesc = m_comp->lvaGetDesc(fieldLclNum);
        var_types  fieldType = fieldDesc->TypeGet();

        GenTree* src   = m_comp->gtNewConWithPattern(fieldType, initPattern);
        GenTree* store = m_comp->gtNewTempStore(fieldLclNum, src);

        if (m_comp->optLocalAssertionProp)
        {
            m_comp->fgAssertionGen(store);
        }

        if (tree != nullptr)
        {
            tree = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, tree, store);
        }
        else
        {
            tree = store;
        }
    }

    if (tree == nullptr)
    {
        tree = m_comp->gtNewNothingNode();
    }

    m_result                 = tree;
    m_transformationDecision = BlockTransformation::FieldByField;
}

//------------------------------------------------------------------------
// TryPrimitiveInit: Replace block zero-initialization with a primitive store.
//
// Transforms patterns like "STORE_BLK(LCL_VAR_ADDR, 0)" into simple
// stores: "STORE_LCL_VAR<int>(0)".
//
// If successful, will set "m_transformationDecision" to "OneStoreBlock".
//
void MorphInitBlockHelper::TryPrimitiveInit()
{
    if (m_src->IsIntegralConst(0) && (m_dstVarDsc != nullptr) && (genTypeSize(m_dstVarDsc) == m_blockSize))
    {
        var_types lclVarType = m_dstVarDsc->TypeGet();
        if (varTypeIsSIMD(lclVarType))
        {
            m_src = m_comp->gtNewZeroConNode(lclVarType);
        }
        else
        {
            m_src->BashToZeroConst(lclVarType);
        }

        m_store->ChangeType(m_dstVarDsc->lvNormalizeOnLoad() ? lclVarType : genActualType(lclVarType));
        m_store->ChangeOper(GT_STORE_LCL_VAR);
        m_store->AsLclVar()->SetLclNum(m_dstLclNum);
        m_store->gtFlags |= GTF_VAR_DEF;

        m_result                 = m_store;
        m_transformationDecision = BlockTransformation::OneStoreBlock;
    }
}

//------------------------------------------------------------------------
// EliminateCommas: Prepare for block morphing by removing commas from the
// source operand of the assignment.
//
// Parameters:
//   commaPool - [out] Pool of GT_COMMA nodes linked by their gtNext nodes that
//                     can be used by the caller to avoid unnecessarily creating
//                     new commas.
//
// Returns:
//   Extracted side effects, in reverse order, linked via the gtNext fields of
//   the nodes.
//
// Notes:
//   We have a tree like the following:
//
//         STOREIND
//         /      \.
//        B      COMMA
//                /  \.
//               C    D
//
//   We'd like downstream code to just see and expand STOREIND(B, D).
//   We will produce:
//
//                     COMMA
//                   /       \.
//    STORE_LCL_VAR<tmp>    COMMA
//           /              /   \.
//          B              C  STOREIND
//                             /  \.
//                           tmp   D
//
//   If the store has GTF_REVERSE_OPS then we will produce:
//
//     COMMA
//     /   \.
//    C   STOREIND
//         /  \.
//        B    D
//
//   While keeping the GTF_REVERSE_OPS.
//
//   Note that the final resulting tree is created in the caller since it also
//   needs to propagate side effect flags from the decomposed store to all the
//   created commas. Therefore this function just returns a linked list of the
//   side effects to be used for that purpose.
//
GenTree* MorphInitBlockHelper::EliminateCommas(GenTree** commaPool)
{
    *commaPool = nullptr;

    GenTree* sideEffects = nullptr;
    auto addSideEffect   = [&sideEffects](GenTree* sideEff) {
        sideEff->gtNext = sideEffects;
        sideEffects     = sideEff;
    };

    auto addComma = [commaPool, &addSideEffect](GenTree* comma) {
        addSideEffect(comma->gtGetOp1());
        comma->gtNext = *commaPool;
        *commaPool    = comma;
    };

    GenTree* src = m_store->Data();

    if (m_store->IsReverseOp())
    {
        while (src->OperIs(GT_COMMA))
        {
            addComma(src);
            src = src->gtGetOp2();
        }
    }
    else
    {
        if (m_store->OperIsIndir() && src->OperIs(GT_COMMA))
        {
            GenTree* addr = m_store->AsIndir()->Addr();
            if (((addr->gtFlags & GTF_ALL_EFFECT) != 0) || (((src->gtFlags & GTF_ASG) != 0) && !addr->IsInvariant()))
            {
                unsigned lhsAddrLclNum = m_comp->lvaGrabTemp(true DEBUGARG("Block morph LHS addr"));

                addSideEffect(m_comp->gtNewTempStore(lhsAddrLclNum, addr));
                m_store->AsUnOp()->gtOp1 = m_comp->gtNewLclvNode(lhsAddrLclNum, genActualType(addr));
                m_comp->gtUpdateNodeSideEffects(m_store);
            }
        }

        while (src->OperIs(GT_COMMA))
        {
            addComma(src);
            src = src->gtGetOp2();
        }
    }

    if (sideEffects != nullptr)
    {
        m_store->Data() = src;
        m_comp->gtUpdateNodeSideEffects(m_store);
    }

    return sideEffects;
}

class MorphCopyBlockHelper : public MorphInitBlockHelper
{
public:
    static GenTree* MorphCopyBlock(Compiler* comp, GenTree* tree);

protected:
    MorphCopyBlockHelper(Compiler* comp, GenTree* store);

    void PrepareSrc() override;

    void TrySpecialCases() override;

    void     MorphStructCases() override;
    void     TryPrimitiveCopy();
    GenTree* CopyFieldByField();

    const char* GetHelperName() const override
    {
        return "MorphCopyBlock";
    }

protected:
    unsigned             m_srcLclNum            = BAD_VAR_NUM;
    LclVarDsc*           m_srcVarDsc            = nullptr;
    GenTreeLclVarCommon* m_srcLclNode           = nullptr;
    bool                 m_srcUseLclFld         = false;
    unsigned             m_srcLclOffset         = 0;
    bool                 m_srcSingleStoreLclVar = false;

    bool m_dstDoFldStore = false;
    bool m_srcDoFldStore = false;

private:
    bool CanReuseAddressForDecomposedStore(GenTree* addr);
};

//------------------------------------------------------------------------
// MorphCopyBlock: Morph a block copy assignment tree.
//
// Arguments:
//    comp - a compiler instance;
//    tree - A store tree that performs block copy.
//
// Return Value:
//    A possibly modified tree to perform the copy.
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
//    store - store node to morph.
//
// Notes:
//    Most class members are initialized via in-class member initializers.
//
MorphCopyBlockHelper::MorphCopyBlockHelper(Compiler* comp, GenTree* store) : MorphInitBlockHelper(comp, store, false)
{
}

//------------------------------------------------------------------------
// PrepareSrc: Initialize member fields with information about the store's
//    source value.
//
void MorphCopyBlockHelper::PrepareSrc()
{
    m_src = m_store->Data();

    if (m_src->IsLocal())
    {
        m_srcLclNode   = m_src->AsLclVarCommon();
        m_srcLclOffset = m_srcLclNode->GetLclOffs();
        m_srcLclNum    = m_srcLclNode->GetLclNum();
        m_srcVarDsc    = m_comp->lvaGetDesc(m_srcLclNum);
    }

    // Verify that the types of the store and data match.
    assert(m_store->TypeGet() == m_src->TypeGet());
    if (m_store->TypeIs(TYP_STRUCT))
    {
        assert(ClassLayout::AreCompatible(m_blockLayout, m_src->GetLayout(m_comp)));
    }
}

// TrySpecialCases: check special cases that require special transformations.
//    The current special cases include assignments with calls in RHS.
//
void MorphCopyBlockHelper::TrySpecialCases()
{
    if (m_src->IsMultiRegNode())
    {
        assert(m_store->OperIs(GT_STORE_LCL_VAR));

        m_dstVarDsc->lvIsMultiRegRet = true;

        JITDUMP("Not morphing a multireg node return\n");
        m_transformationDecision = BlockTransformation::SkipMultiRegSrc;
        m_result                 = m_store;
    }
    else if (m_src->IsCall() && m_store->OperIs(GT_STORE_LCL_VAR) && m_dstVarDsc->CanBeReplacedWithItsField(m_comp))
    {
        JITDUMP("Not morphing a single reg call return\n");
        m_transformationDecision = BlockTransformation::SkipSingleRegCallSrc;
        m_result                 = m_store;
    }
}

//------------------------------------------------------------------------
// MorphStructCases: transforms the store as field by field copy or keeps it as a block init
//    but sets appropriate flags for the involved lclVars.
//
// Assumptions:
//    We have already checked that it is not a special case.
//
void MorphCopyBlockHelper::MorphStructCases()
{
    JITDUMP("block assignment to morph:\n");
    DISPTREE(m_store);

    if (m_dstVarDsc != nullptr)
    {
        if (m_dstVarDsc->lvPromoted)
        {
            noway_assert(varTypeIsStruct(m_dstVarDsc));
            noway_assert(!m_comp->opts.MinOpts());

            if (m_blockSize == m_dstVarDsc->lvExactSize())
            {
                JITDUMP(" (m_dstDoFldStore=true)");
                // We may decide later that a copyblk is required when this struct has holes
                m_dstDoFldStore = true;
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

            if (m_blockSize == m_srcVarDsc->lvExactSize())
            {
                JITDUMP(" (m_srcDoFldStore=true)");
                // We may decide later that a copyblk is required when this struct has holes
                m_srcDoFldStore = true;
            }
            else
            {
                JITDUMP(" with mismatched src size");
            }
        }
    }

    // Check to see if we are doing a copy to/from the same local block. If so, morph it to a nop.
    // Don't do this for SSA definitions as we have no way to update downstream uses.
    if ((m_dstVarDsc != nullptr) && (m_srcVarDsc == m_dstVarDsc) && (m_dstLclOffset == m_srcLclOffset) &&
        !m_store->AsLclVarCommon()->HasSsaIdentity())
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

    // If either src or dest is a reg-sized non-field-addressed struct, keep the copyBlock;
    // this will avoid having to DNER the enregisterable local when creating LCL_FLD nodes.
    if ((m_store->OperIs(GT_STORE_LCL_VAR) && m_dstVarDsc->lvRegStruct) ||
        (m_src->OperIs(GT_LCL_VAR) && m_srcVarDsc->lvRegStruct))
    {
        requiresCopyBlock = true;
    }

    // Can we use field by field assignment for the dest?
    if (m_dstDoFldStore && m_dstVarDsc->lvAnySignificantPadding)
    {
        JITDUMP(" dest has significant padding");
        // C++ style CopyBlock with holes
        requiresCopyBlock = true;
    }

    // Can we use field by field assignment for the src?
    if (m_srcDoFldStore && m_srcVarDsc->lvAnySignificantPadding)
    {
        JITDUMP(" src has significant padding");
        // C++ style CopyBlock with holes
        requiresCopyBlock = true;
    }

#if defined(TARGET_ARM)
    if (m_store->OperIsIndir() && m_store->AsIndir()->IsUnaligned())
    {
        JITDUMP(" store is unaligned");
        requiresCopyBlock = true;
    }

    if (m_src->OperIsIndir() && m_src->AsIndir()->IsUnaligned())
    {
        JITDUMP(" src is unaligned");
        requiresCopyBlock = true;
    }
#endif // TARGET_ARM

    // Don't use field by field assignment if the src is a call, lowering will handle
    // it without spilling the call result into memory to access the individual fields.
    // For HWI/SIMD/CNS_VEC, we don't expect promoted destinations - we purposefully
    // mark SIMDs used in such copies as "used in a SIMD intrinsic", to prevent their
    // promotion.
    if ((m_srcVarDsc == nullptr) && !m_src->OperIsIndir())
    {
        JITDUMP(" src is not an L-value");
        requiresCopyBlock = true;
    }

    // If we passed the above checks, then we will check these two
    if (!requiresCopyBlock)
    {
        // It is not always profitable to do field by field init for structs that are allocated to memory.
        // A struct with 8 bool fields will require 8 moves instead of one if we do this transformation.
        // A simple heuristic when field by field copy is preferred:
        // - if fields can be enregistered;
        // - if the struct has only one field.
        // - if the copy involves GC pointers and the destination isn't stack (backend uses a helper for these block
        // copies)
        bool dstFldIsProfitable =
            ((m_dstVarDsc != nullptr) && (!m_dstVarDsc->lvDoNotEnregister || (m_dstVarDsc->lvFieldCnt == 1)));
        bool srcFldIsProfitable = ((m_srcVarDsc != nullptr) && (!m_srcVarDsc->lvDoNotEnregister ||
                                                                (m_srcVarDsc->HasGCPtr() && (m_dstVarDsc == nullptr)) ||
                                                                (m_srcVarDsc->lvFieldCnt == 1)));
        // Are both dest and src promoted structs?
        if (m_dstDoFldStore && m_srcDoFldStore && (dstFldIsProfitable || srcFldIsProfitable))
        {
            // Both structs should be of the same type, or have the same number of fields of the same type.
            // If not we will use a copy block.
            bool misMatchedTypes = false;
            if (m_dstVarDsc->GetLayout() != m_srcVarDsc->GetLayout())
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
        else if (m_dstDoFldStore && dstFldIsProfitable)
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
                    m_srcSingleStoreLclVar = true;
                }
            }
        }
        else if (m_srcDoFldStore && srcFldIsProfitable)
        {
            // Check for the symmetric case (which happens for the _reference field of promoted spans):
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
                    m_dstSingleStoreLclVar = true;
                }
            }
        }
        // Are neither dest or src promoted structs?
        else
        {
            assert(!(m_dstDoFldStore && dstFldIsProfitable) && !(m_srcDoFldStore && srcFldIsProfitable));
            requiresCopyBlock = true; // Leave as a CopyBlock
            JITDUMP(" with no promoted structs");
        }
    }

    // If we require a copy block the set both of the field assign bools to false
    if (requiresCopyBlock)
    {
        // If a copy block is required then we won't do field by field assignments
        m_dstDoFldStore = false;
        m_srcDoFldStore = false;
    }

    JITDUMP(requiresCopyBlock ? " this requires a CopyBlock.\n" : " using field by field assignments.\n");

    if (requiresCopyBlock)
    {
        TryPrimitiveCopy();

        if (m_transformationDecision == BlockTransformation::Undefined)
        {
            m_result                 = m_store;
            m_transformationDecision = BlockTransformation::StructBlock;
        }
    }
    else
    {
        m_result                 = CopyFieldByField();
        m_transformationDecision = BlockTransformation::FieldByField;
    }

    // Mark the dest/src structs as DoNotEnreg when they are not being fully referenced as the same type.
    //
    if (!m_dstDoFldStore && (m_dstVarDsc != nullptr) && !m_dstSingleStoreLclVar)
    {
        if (m_store->OperIs(GT_STORE_LCL_FLD))
        {
            m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        else if (m_dstVarDsc->lvPromoted)
        {
            // Mark it as DoNotEnregister.
            m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }
    }

    if (!m_srcDoFldStore && (m_srcVarDsc != nullptr) && !m_srcSingleStoreLclVar)
    {
        if (m_src->OperIs(GT_LCL_FLD))
        {
            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        else if (m_srcVarDsc->lvPromoted)
        {
            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }
    }
}

//------------------------------------------------------------------------
// TryPrimitiveCopy: Attempt to replace a block assignment with a scalar assignment.
//
// If successful, will set "m_transformationDecision" to "OneStoreBlock".
//
void MorphCopyBlockHelper::TryPrimitiveCopy()
{
    if (!m_store->TypeIs(TYP_STRUCT))
    {
        return;
    }

    if (m_comp->opts.OptimizationDisabled() && (m_blockSize >= genTypeSize(TYP_INT)))
    {
        return;
    }

    var_types storeType = TYP_UNDEF;

    // Can we use the LHS local directly?
    if (m_store->OperIs(GT_STORE_LCL_FLD))
    {
        if (m_blockSize == genTypeSize(m_dstVarDsc))
        {
            storeType = m_dstVarDsc->TypeGet();
        }
    }
    else if (!m_store->OperIsIndir())
    {
        return;
    }

    if (m_srcVarDsc != nullptr)
    {
        if ((storeType == TYP_UNDEF) && (m_blockSize == genTypeSize(m_srcVarDsc)))
        {
            storeType = m_srcVarDsc->TypeGet();
        }
    }
    else if (!m_src->OperIsIndir())
    {
        return;
    }

    if (storeType == TYP_UNDEF)
    {
        return;
    }

    auto doRetypeNode = [storeType](GenTree* op, LclVarDsc* varDsc, bool isUse) {
        if (op->OperIsIndir())
        {
            op->SetOper(isUse ? GT_IND : GT_STOREIND);
            op->ChangeType(storeType);
        }
        else if (varDsc->TypeGet() == storeType)
        {
            op->SetOper(isUse ? GT_LCL_VAR : GT_STORE_LCL_VAR);
            op->ChangeType(varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc));
            op->gtFlags &= ~GTF_VAR_USEASG;
        }
        else
        {
            if (op->OperIsScalarLocal())
            {
                op->SetOper(isUse ? GT_LCL_FLD : GT_STORE_LCL_FLD);
            }
            op->ChangeType(storeType);
        }
    };

    doRetypeNode(m_store, m_dstVarDsc, /* isUse */ false);
    doRetypeNode(m_src, m_srcVarDsc, /* isUse */ true);

    m_result                 = m_store;
    m_transformationDecision = BlockTransformation::OneStoreBlock;
}

//------------------------------------------------------------------------
// CopyFieldByField: transform the copy block to a field by field assignment.
//
// Notes:
//    We do it for promoted lclVars which fields can be enregistered.
//
GenTree* MorphCopyBlockHelper::CopyFieldByField()
{
    GenTree* result = nullptr;

    GenTree*       addr               = nullptr;
    target_ssize_t addrBaseOffs       = 0;
    FieldSeq*      addrBaseOffsFldSeq = nullptr;
    GenTree*       addrSpill          = nullptr;
    unsigned       addrSpillTemp      = BAD_VAR_NUM;

    GenTree* addrSpillStore = nullptr;

    unsigned fieldCnt = 0;

    unsigned dyingFieldCnt = 0;
    if (m_dstDoFldStore)
    {
        fieldCnt = m_dstVarDsc->lvFieldCnt;

        if (m_comp->fgGlobalMorph)
        {
            dyingFieldCnt =
                genCountBits(static_cast<unsigned>(m_dstLclNode->gtFlags & m_dstVarDsc->AllFieldDeathFlags()));
            assert(dyingFieldCnt <= fieldCnt);
        }
    }

    if (m_dstDoFldStore && m_srcDoFldStore)
    {
        // To do fieldwise assignments for both sides.
        // The structs do not have to be the same exact types but have to have same field types
        // at the same offsets.
        assert(m_dstLclNum != BAD_VAR_NUM && m_srcLclNum != BAD_VAR_NUM);
        assert(m_dstVarDsc != nullptr && m_srcVarDsc != nullptr && m_dstVarDsc->lvFieldCnt == m_srcVarDsc->lvFieldCnt);
    }
    else if (m_dstDoFldStore)
    {
        m_srcUseLclFld = m_srcVarDsc != nullptr;

        if (!m_srcUseLclFld)
        {
            addr = m_src->AsIndir()->Addr();

            m_comp->gtPeelOffsets(&addr, &addrBaseOffs, &addrBaseOffsFldSeq);

            // "addr" might be a complex expression that we need to clone
            // and spill, unless we only end up using the address once.
            if (fieldCnt - dyingFieldCnt > 1)
            {
                if (CanReuseAddressForDecomposedStore(addr))
                {
                    // "srcAddr" is simple expression. No need to spill.
                    noway_assert((addr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
                }
                else
                {
                    addrSpill = addr;
                }
            }
        }
    }
    else
    {
        assert(m_srcDoFldStore);
        fieldCnt       = m_srcVarDsc->lvFieldCnt;
        m_dstUseLclFld = m_dstVarDsc != nullptr;

        // Clear the def flags, we'll reuse the node below and reset them.
        if (m_dstLclNode != nullptr)
        {
            m_dstLclNode->gtFlags &= ~(GTF_VAR_DEF | GTF_VAR_USEASG);
        }

        if (!m_dstUseLclFld)
        {
            addr = m_store->AsIndir()->Addr();

            m_comp->gtPeelOffsets(&addr, &addrBaseOffs, &addrBaseOffsFldSeq);

            // "addr" might be a complex expression that we need to clone
            // and spill, unless we only end up using the address once.
            if (m_srcVarDsc->lvFieldCnt > 1)
            {
                if (CanReuseAddressForDecomposedStore(addr))
                {
                    // "dstAddr" is simple expression. No need to spill
                    noway_assert((addr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);
                }
                else
                {
                    addrSpill = addr;
                }
            }
        }
    }

    if (addrSpill != nullptr)
    {
        // 'addrSpill' is already morphed

        // Spill the (complex) address to a temp.
        // Note, at most one address may need to be spilled.
        addrSpillTemp = m_comp->lvaGrabTemp(true DEBUGARG("BlockOp address local"));

        LclVarDsc* addrSpillDsc = m_comp->lvaGetDesc(addrSpillTemp);
        addrSpillStore          = m_comp->gtNewTempStore(addrSpillTemp, addrSpill);
    }

    auto grabAddr = [=, &result](unsigned offs) {
        GenTree* addrClone = nullptr;
        // Need address of the source.
        if (addrSpill)
        {
            assert(addrSpillTemp != BAD_VAR_NUM);
            addrClone = m_comp->gtNewLclvNode(addrSpillTemp, addrSpill->TypeGet());
        }
        else
        {
            if (result == nullptr)
            {
                // Reuse the original address tree for the first field.
                addrClone = addr;
            }
            else
            {
                // We can't clone multiple copies of a tree with persistent side effects
                noway_assert((addr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0);

                addrClone = m_comp->gtCloneExpr(addr);
                noway_assert(addrClone != nullptr);

                JITDUMP("Multiple Fields Clone created:\n");
                DISPTREE(addrClone);

                // Morph the newly created tree
                addrClone = m_comp->fgMorphTree(addrClone);
            }
        }

        assert(addrClone != nullptr);
        target_ssize_t fullOffs = addrBaseOffs + (target_ssize_t)offs;
        if ((fullOffs != 0) || (addrBaseOffsFldSeq != nullptr))
        {
            // Avoid using unsigned overload of gtNewIconNode
            // that takes field seq to get correct overflow
            // handling.
            GenTreeIntCon* fldOffsetNode = m_comp->gtNewIconNode(fullOffs, TYP_I_IMPL);
            fldOffsetNode->gtFieldSeq    = addrBaseOffsFldSeq;
            addrClone = m_comp->gtNewOperNode(GT_ADD, varTypeIsGC(addrClone) ? TYP_BYREF : TYP_I_IMPL, addrClone,
                                              fldOffsetNode);
            // Avoid constant prop propagating each field access with a large
            // constant address. TODO-Cleanup: We should tune constant prop to
            // have better heuristics around this.
            addrClone->gtFlags |= GTF_DONT_CSE;
        }

        return addrClone;
    };

    if (dyingFieldCnt == fieldCnt)
    {
        JITDUMP("All fields of destination of field-by-field copy are dying, skipping entirely\n");

        if (m_srcUseLclFld)
        {
            return m_comp->gtNewNothingNode();
        }
        else
        {
            JITDUMP("  ...but keeping a nullcheck\n");
            return m_comp->gtNewIndir(TYP_BYTE, grabAddr(0));
        }
    }

    // We may have allocated a temp above, and that may have caused the lvaTable to be expanded.
    // So, beyond this point we cannot rely on the old values of 'm_srcVarDsc' and 'm_dstVarDsc'.
    for (unsigned i = 0; i < fieldCnt; ++i)
    {
        if (m_dstDoFldStore && m_comp->fgGlobalMorph && m_dstLclNode->IsLastUse(i))
        {
            INDEBUG(unsigned dstFieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart + i);
            JITDUMP("Field-by-field copy skipping write to dead field V%02u\n", dstFieldLclNum);
            continue;
        }

        GenTree* srcFld = nullptr;
        if (m_srcDoFldStore)
        {
            noway_assert((m_srcLclNum != BAD_VAR_NUM) && (m_srcLclNode != nullptr));
            unsigned srcFieldLclNum = m_comp->lvaGetDesc(m_srcLclNum)->lvFieldLclStart + i;

            srcFld = m_comp->gtNewLclvNode(srcFieldLclNum, m_comp->lvaGetDesc(srcFieldLclNum)->TypeGet());
        }
        else
        {
            noway_assert(m_dstDoFldStore);
            noway_assert(m_dstLclNum != BAD_VAR_NUM);
            unsigned dstFieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart + i;

            if (m_srcSingleStoreLclVar)
            {
                noway_assert(fieldCnt == 1);
                noway_assert(m_srcLclNum != BAD_VAR_NUM);
                noway_assert(addrSpill == nullptr);

                srcFld = m_comp->gtNewLclvNode(m_srcLclNum, m_comp->lvaGetDesc(m_srcLclNum)->TypeGet());
            }
            else
            {
                unsigned  fldOffset = m_comp->lvaGetDesc(dstFieldLclNum)->lvFldOffset;
                var_types destType  = m_comp->lvaGetDesc(dstFieldLclNum)->lvType;

                bool done = false;
                if (fldOffset == 0)
                {
                    // If this is a full-width use of the src via a different type, we need to create a
                    // GT_LCL_FLD.
                    // (Note that if it was the same type, 'm_srcSingleStoreLclVar' would be true.)
                    if (m_srcLclNum != BAD_VAR_NUM)
                    {
                        noway_assert(m_srcLclNode != nullptr);
                        assert(destType != TYP_STRUCT);
                        unsigned destSize = genTypeSize(destType);
                        m_srcVarDsc       = m_comp->lvaGetDesc(m_srcLclNum);
                        unsigned srcSize  = m_srcVarDsc->lvExactSize();
                        if (destSize == srcSize)
                        {
                            m_srcLclNode->ChangeOper(GT_LCL_FLD);
                            m_srcLclNode->gtType = destType;
                            m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                            srcFld = m_srcLclNode;
                            done   = true;
                        }
                    }
                }
                if (!done)
                {
                    if (!m_srcUseLclFld)
                    {
                        GenTree* fldAddr = grabAddr(fldOffset);
                        srcFld           = m_comp->gtNewIndir(destType, fldAddr);
                    }
                    else
                    {
                        // If the src was a struct type field "B" in a struct "A" then we add
                        // add offset of ("B" in "A") + current offset in "B".
                        unsigned totalOffset = m_srcLclOffset + fldOffset;
                        srcFld               = m_comp->gtNewLclFldNode(m_srcLclNum, destType, totalOffset);

                        // TODO-1stClassStructs: remove this and implement reading a field from a struct in a reg.
                        m_comp->lvaSetVarDoNotEnregister(m_srcLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                    }
                }
            }
        }
        assert(srcFld != nullptr);

        GenTree* dstFldStore;
        if (m_dstDoFldStore)
        {
            noway_assert(m_dstLclNum != BAD_VAR_NUM);

            unsigned dstFieldLclNum = m_comp->lvaGetDesc(m_dstLclNum)->lvFieldLclStart + i;
            dstFldStore             = m_comp->gtNewStoreLclVarNode(dstFieldLclNum, srcFld);
        }
        else
        {
            noway_assert(m_srcDoFldStore);

            if (m_dstSingleStoreLclVar)
            {
                noway_assert(fieldCnt == 1);
                noway_assert(m_dstVarDsc != nullptr);
                noway_assert(addrSpill == nullptr);

                dstFldStore = m_comp->gtNewStoreLclVarNode(m_dstLclNum, srcFld);
            }
            else
            {
                LclVarDsc* srcVarDsc      = m_comp->lvaGetDesc(m_srcLclNum);
                unsigned   srcFieldLclNum = srcVarDsc->lvFieldLclStart + i;
                LclVarDsc* srcFieldVarDsc = m_comp->lvaGetDesc(srcFieldLclNum);
                unsigned   srcFieldOffset = srcFieldVarDsc->lvFldOffset;
                var_types  srcType        = srcFieldVarDsc->TypeGet();

                if (!m_dstUseLclFld)
                {
                    GenTree* fldAddr = grabAddr(srcFieldOffset);
                    dstFldStore      = m_comp->gtNewStoreIndNode(srcType, fldAddr, srcFld);
                }
                else
                {
                    // If the dst was a struct type field "B" in a struct "A" then we add
                    // add offset of ("B" in "A") + current offset in "B".
                    unsigned totalOffset = m_dstLclOffset + srcFieldOffset;
                    dstFldStore          = m_comp->gtNewStoreLclFldNode(m_dstLclNum, srcType, totalOffset, srcFld);

                    // TODO-1stClassStructs: remove this and implement storing to a field in a struct in a reg.
                    m_comp->lvaSetVarDoNotEnregister(m_dstLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                }
            }
        }
        noway_assert(dstFldStore->TypeGet() == srcFld->TypeGet());

        if (m_comp->optLocalAssertionProp)
        {
            m_comp->fgAssertionGen(dstFldStore);
        }

        if (addrSpillStore != nullptr)
        {
            result         = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, addrSpillStore, dstFldStore);
            addrSpillStore = nullptr;
        }
        else if (result != nullptr)
        {
            result = m_comp->gtNewOperNode(GT_COMMA, TYP_VOID, result, dstFldStore);
        }
        else
        {
            result = dstFldStore;
        }
    }

    return result;
}

//------------------------------------------------------------------------
// CanReuseAddressForDecomposedStore: Check if it is safe to reuse the
// specified address node for each decomposed store of a block copy.
//
// Arguments:
//   addrNode - The address node
//
// Return Value:
//   True if the caller can reuse the address by cloning.
//
bool MorphCopyBlockHelper::CanReuseAddressForDecomposedStore(GenTree* addrNode)
{
    if (addrNode->OperIsLocalRead())
    {
        GenTreeLclVarCommon* lcl    = addrNode->AsLclVarCommon();
        unsigned             lclNum = lcl->GetLclNum();
        LclVarDsc*           lclDsc = m_comp->lvaGetDesc(lclNum);
        if (lclDsc->IsAddressExposed())
        {
            // Address could be pointing to itself
            return false;
        }

        // The store can also directly write to the address. For example:
        //
        //  ▌  STORE_LCL_VAR struct<Program+ListElement, 16>(P) V00 loc0
        //  ▌    long   V00.Program+ListElement:Next (offs=0x00) -> V03 tmp1
        //  ▌    int    V00.Program+ListElement:Value (offs=0x08) -> V04 tmp2
        //  └──▌  BLK       struct<Program+ListElement, 16>
        //     └──▌  LCL_VAR   long   V03 tmp1          (last use)
        //
        // If we reused the address we would produce
        //
        //  ▌  COMMA     void
        //  ├──▌  STORE_LCL_VAR long   V03 tmp1
        //  │  └──▌  IND       long
        //  │     └──▌  LCL_VAR   long   V03 tmp1          (last use)
        //  └──▌  STORE_LCL_VAR int    V04 tmp2
        //     └──▌  IND       int
        //        └──▌  ADD       byref
        //           ├──▌  LCL_VAR   long   V03 tmp1          (last use)
        //           └──▌  CNS_INT   long   8
        //
        // which is also obviously incorrect.
        //

        if (m_dstLclNum == BAD_VAR_NUM)
        {
            return true;
        }

        return (lclNum != m_dstLclNum) && (!lclDsc->lvIsStructField || (lclDsc->lvParentLcl != m_dstLclNum));
    }

    return addrNode->IsInvariant();
}

//------------------------------------------------------------------------
// fgMorphCopyBlock: Perform the morphing of a block copy.
//
// Arguments:
//    tree - a block copy (i.e. a store with a struct type).
//
// Return Value:
//    We can return the original block copy unmodified (least desirable, but always correct)
//    We can return a single assignment, when TryPrimitiveCopy transforms it (most desirable).
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
//    If the Source() or Dest() is a struct that has a "CustomLayout" and "ContainsHoles" then we
//    can not use a field by field assignment and must leave the original block copy unmodified.
//
GenTree* Compiler::fgMorphCopyBlock(GenTree* tree)
{
    return MorphCopyBlockHelper::MorphCopyBlock(this, tree);
}

//------------------------------------------------------------------------
// fgMorphInitBlock: Morph a block initialization assignment tree.
//
// Arguments:
//    tree - A store tree that performs block initialization.
//
// Return Value:
//    If the destination is a promoted struct local variable then we will try to
//    perform a field by field assignment for each of the promoted struct fields.
//    This is not always possible (e.g. if the struct is address exposed).
//
//    Otherwise the original store tree is returned unmodified, note that the
//    nodes can still be changed.
//
// Assumptions:
//    store's children have already been morphed.
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
//    into an "ordinary" STORE_BLK<size> one, and further morphed by
//    "fgMorphInitBlock"/"fgMorphCopyBlock". Otherwise, the original
//    tree (fully morphed).
//
GenTree* Compiler::fgMorphStoreDynBlock(GenTreeStoreDynBlk* tree)
{
    if (!tree->Data()->OperIs(GT_CNS_INT, GT_INIT_VAL))
    {
        // Data is a location and required to have GTF_DONT_CSE.
        tree->Data()->gtFlags |= GTF_DONT_CSE;
    }

    tree->Addr()        = fgMorphTree(tree->Addr());
    tree->Data()        = fgMorphTree(tree->Data());
    tree->gtDynamicSize = fgMorphTree(tree->gtDynamicSize);

    if (tree->gtDynamicSize->IsIntegralConst())
    {
        int64_t size = tree->gtDynamicSize->AsIntConCommon()->IntegralValue();

        if ((size != 0) && FitsIn<int32_t>(size))
        {
            ClassLayout* layout = typGetBlkLayout(static_cast<unsigned>(size));
            GenTree*     src    = tree->Data();
            if (src->OperIs(GT_IND))
            {
                assert(src->TypeIs(TYP_STRUCT));
                src->SetOper(GT_BLK);
                src->AsBlk()->Initialize(layout);
            }

            GenTree* store = gtNewStoreValueNode(layout, tree->Addr(), src, tree->gtFlags & GTF_IND_FLAGS);
            store->AddAllEffectsFlags(tree);
            INDEBUG(store->gtDebugFlags |= GTF_DEBUG_NODE_MORPHED);

            JITDUMP("MorphStoreDynBlock: transformed STORE_DYN_BLK into STORE_BLK\n");

            return tree->OperIsCopyBlkOp() ? fgMorphCopyBlock(store) : fgMorphInitBlock(store);
        }
    }

    tree->SetAllEffectsFlags(tree->Addr(), tree->Data(), tree->gtDynamicSize);

    if (tree->OperMayThrow(this))
    {
        tree->gtFlags |= GTF_EXCEPT;
    }
    else
    {
        tree->gtFlags |= GTF_IND_NONFAULTING;
    }

    tree->gtFlags |= GTF_ASG;

    return tree;
}
