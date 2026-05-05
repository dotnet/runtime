// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "autovectorizer.h"

AutoVectorizer::AutoVectorizer(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus AutoVectorizer::RunAnalyze()
{
    if (!IsEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!IsSupportedCompilation())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!EnsureLoopTable())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    unsigned candidateCount = 0;

    for (FlowGraphNaturalLoop* const loop : m_compiler->m_loops->InPostOrder())
    {
        LoopVectorizationPlan plan;
        if (TryCreateLoopPlan(loop, &plan))
        {
            candidateCount++;
            JITDUMP("accepted loop " FMT_LP ", IV V%02u, VF=%u, vectorSize=%u\n", loop->GetIndex(),
                    plan.IsPostIV ? plan.TripCountVar : plan.InductionVar, plan.VectorizationFactor,
                    plan.VectorSizeBytes);
            DumpSLPPlan(plan);
        }
    }

    JITDUMP("analysis found %u candidate loop%s\n", candidateCount, (candidateCount == 1) ? "" : "s");
    return PhaseStatus::MODIFIED_NOTHING;
}

PhaseStatus AutoVectorizer::RunRewrite()
{
    if (!IsEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!IsSupportedCompilation())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!EnsureLoopTable())
    {
        m_compiler->fgInvalidateDfsTree();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool changed = false;

    for (FlowGraphNaturalLoop* const loop : m_compiler->m_loops->InPostOrder())
    {
        LoopVectorizationPlan plan;
        if (TryCreateLoopPlan(loop, &plan) && TryRewritePlan(&plan))
        {
            changed = true;
            break;
        }
    }

    if (!changed)
    {
        m_compiler->fgInvalidateDfsTree();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    m_compiler->fgModified              = true;
    m_compiler->fgMightHaveNaturalLoops = true;
    m_compiler->optLoopsCanonical       = false;
    m_compiler->fgLocalVarLivenessDone  = false;
    m_compiler->fgInvalidateDfsTree();

    return PhaseStatus::MODIFIED_EVERYTHING;
}

bool AutoVectorizer::IsEnabled() const
{
    return JitConfig.JitAutoVectorization() != 0;
}

bool AutoVectorizer::IsSupportedCompilation() const
{
    if (m_compiler->opts.MinOpts() || m_compiler->opts.compDbgCode)
    {
        JITDUMP("disabled for MinOpts/debuggable code\n");
        return false;
    }

#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned vectorSizeBytes = GetVectorSizeBytes(TYP_INT);
    if (vectorSizeBytes < 16)
    {
        JITDUMP("target vector size %u is too small\n", vectorSizeBytes);
        return false;
    }

    return true;
#else
    JITDUMP("unsupported target\n");
    return false;
#endif
}

unsigned AutoVectorizer::GetVectorSizeBytes(var_types elementType) const
{
    if (!IsSupportedElementType(elementType))
    {
        return 0;
    }

#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
    if (m_compiler->opts.compSupportsISA.HasInstructionSet(InstructionSet_AVX512))
    {
        return ZMM_REGSIZE_BYTES;
    }

    if (m_compiler->opts.compSupportsISA.HasInstructionSet(InstructionSet_AVX2))
    {
        return YMM_REGSIZE_BYTES;
    }

    return XMM_REGSIZE_BYTES;
#elif defined(FEATURE_SIMD) && defined(TARGET_ARM64)
    return FP_REGSIZE_BYTES;
#else
    return 0;
#endif
}

bool AutoVectorizer::ReportVectorIsa(unsigned vectorSizeBytes) const
{
#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
    if (vectorSizeBytes == ZMM_REGSIZE_BYTES)
    {
        return m_compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512);
    }

    if (vectorSizeBytes == YMM_REGSIZE_BYTES)
    {
        return m_compiler->compOpportunisticallyDependsOn(InstructionSet_AVX2);
    }

    return vectorSizeBytes == XMM_REGSIZE_BYTES;
#elif defined(FEATURE_SIMD) && defined(TARGET_ARM64)
    return vectorSizeBytes == FP_REGSIZE_BYTES;
#else
    return false;
#endif
}

bool AutoVectorizer::EnsureLoopTable()
{
    if (m_compiler->m_dfsTree == nullptr)
    {
        m_compiler->m_dfsTree = m_compiler->fgComputeDfs();
    }

    if (m_compiler->m_loops == nullptr)
    {
        m_compiler->m_loops = FlowGraphNaturalLoops::Find(m_compiler->m_dfsTree);
    }

    if (m_compiler->m_loops->NumLoops() == 0)
    {
        JITDUMP("no loops for rewrite\n");
        return false;
    }

    return true;
}

bool AutoVectorizer::IsSupportedElementType(var_types elementType) const
{
    switch (elementType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        case TYP_LONG:
        case TYP_ULONG:
        case TYP_FLOAT:
        case TYP_DOUBLE:
            return true;
        default:
            return false;
    }
}

bool AutoVectorizer::IsSupportedBinaryOp(genTreeOps oper, var_types elementType) const
{
    if (!IsSupportedElementType(elementType))
    {
        return false;
    }

    switch (oper)
    {
        case GT_ADD:
        case GT_SUB:
            return true;

        case GT_MUL:
            return true;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            return varTypeIsIntegral(elementType);

        default:
            return false;
    }
}

bool AutoVectorizer::TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    JITDUMP("considering loop " FMT_LP "\n", loop->GetIndex());

    if (loop->EntryEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one entry");
        return false;
    }

    if (loop->BackEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one backedge");
        return false;
    }

    if (loop->ExitEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one normal exit");
        return false;
    }

    if (loop->GetChild() != nullptr)
    {
        Reject(loop, "non-innermost loop");
        return false;
    }

    BasicBlock* const preheader = loop->EntryEdge(0)->getSourceBlock();
    BasicBlock* const latch     = loop->BackEdge(0)->getSourceBlock();
    BasicBlock* const exit      = loop->ExitEdge(0)->getDestinationBlock();

    if ((preheader == nullptr) || !preheader->KindIs(BBJ_ALWAYS) || !preheader->TargetIs(loop->GetHeader()))
    {
        Reject(loop, "missing canonical preheader");
        return false;
    }

    if (preheader->hasTryIndex() || preheader->hasHndIndex() || exit->hasTryIndex() || exit->hasHndIndex())
    {
        Reject(loop, "EH region near loop");
        return false;
    }

    bool hasEH = false;
    loop->VisitLoopBlocks([&](BasicBlock* block) {
        if (block->hasTryIndex() || block->hasHndIndex())
        {
            hasEH = true;
            return BasicBlockVisit::Abort;
        }

        return BasicBlockVisit::Continue;
    });

    if (hasEH)
    {
        Reject(loop, "EH region in loop");
        return false;
    }

    LoopVectorizationPlan postIVPlan;
    if (TryCreatePostIVLoopPlan(loop, &postIVPlan))
    {
        *plan = postIVPlan;
        return true;
    }

    NaturalLoopIterInfo iterInfo;
    if (!loop->AnalyzeIteration(&iterInfo))
    {
        if ((iterInfo.IterVar == BAD_VAR_NUM) || (iterInfo.TestTree == nullptr) || (iterInfo.IterTree == nullptr) ||
            (iterInfo.TestBlock == nullptr) ||
            (!iterInfo.HasConstLimit && !iterInfo.HasArrayLengthLimit && !iterInfo.HasInvariantLocalLimit))
        {
            return false;
        }

        JITDUMP("loop " FMT_LP " has guarded canonical iteration\n", loop->GetIndex());
    }

    if (!iterInfo.HasConstInit || (iterInfo.ConstInitValue < 0))
    {
        Reject(loop, "IV init is not a non-negative constant");
        return false;
    }

    int step = iterInfo.IterConst();
    if (iterInfo.IterOper() == GT_SUB)
    {
        step = -step;
    }

    const genTreeOps testOper = iterInfo.TestOper();
    const bool       isSupportedNotEqualLoop =
        (testOper == GT_NE) && (step == 1) &&
        (iterInfo.HasArrayLengthLimit || iterInfo.HasInvariantLocalLimit ||
         (iterInfo.HasConstLimit && (iterInfo.ConstLimit() > iterInfo.ConstInitValue)));
    const bool isSupportedIncreasingLoop = iterInfo.IsIncreasingLoop() || isSupportedNotEqualLoop;

    if ((step != 1) || !isSupportedIncreasingLoop)
    {
        Reject(loop, "canonical loop does not have contiguous unit-stride memory");
        return false;
    }

    if (!GenTree::StaticOperIs(testOper, GT_LT, GT_LE) && !isSupportedNotEqualLoop)
    {
        Reject(loop, "unsupported loop test");
        return false;
    }

    if (!iterInfo.HasConstLimit && !iterInfo.HasArrayLengthLimit && !iterInfo.HasInvariantLocalLimit)
    {
        Reject(loop, "limit is not constant, array length, or invariant local");
        return false;
    }

    plan->Loop         = loop;
    plan->Preheader    = preheader;
    plan->Header       = loop->GetHeader();
    plan->Latch        = latch;
    plan->Exit         = exit;
    plan->InductionVar = iterInfo.IterVar;
    plan->End          = iterInfo.Limit();
    plan->IterTree     = iterInfo.IterTree;
    plan->TestTree     = iterInfo.TestTree;
    plan->TestBlock    = iterInfo.TestBlock;
    plan->TestOper     = testOper;
    plan->Step         = 1;
    plan->ElementType  = TYP_UNDEF;
    plan->ElementSize  = 0;

    JITDUMP("loop " FMT_LP " canonical IV V%02u, init=%d, step=1, test=%s\n", loop->GetIndex(), iterInfo.IterVar,
            iterInfo.ConstInitValue, GenTree::OpName(testOper));

    if (!TryAnalyzeMemory(plan))
    {
        return false;
    }

#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = GetVectorSizeBytes(plan->ElementType);
    plan->VectorizationFactor = plan->VectorSizeBytes / plan->ElementSize;
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(loop, "vectorization factor is too small");
        return false;
    }

    return TryBuildSLPPlan(plan);
}

bool AutoVectorizer::TryCreatePostIVLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    BasicBlock* const header = loop->GetHeader();
    if (header != loop->BackEdge(0)->getSourceBlock())
    {
        Reject(loop, "post-IV form is not a single-block loop");
        return false;
    }

    if (!header->KindIs(BBJ_COND))
    {
        Reject(loop, "post-IV loop header is not conditional");
        return false;
    }

    Statement* const testStmt = header->lastStmt();
    if ((testStmt == nullptr) || !testStmt->GetRootNode()->OperIs(GT_JTRUE))
    {
        Reject(loop, "post-IV loop has no bottom test");
        return false;
    }

    GenTree* const relop = testStmt->GetRootNode()->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        Reject(loop, "post-IV loop test is not a compare");
        return false;
    }

    GenTree* op1      = relop->AsOp()->gtOp1;
    GenTree* op2      = relop->AsOp()->gtOp2;
    bool     reversed = false;
    if (op1->IsIntegralConst(0))
    {
        std::swap(op1, op2);
        reversed = true;
    }

    if (!op1->OperIs(GT_LCL_VAR) || !op2->IsIntegralConst(0))
    {
        Reject(loop, "post-IV loop test does not compare local to zero");
        return false;
    }

    genTreeOps testOper = relop->OperGet();
    if (reversed)
    {
        testOper = GenTree::SwapRelop(testOper);
    }

    if (!GenTree::StaticOperIs(testOper, GT_NE, GT_GT))
    {
        Reject(loop, "post-IV loop test is not a positive remaining-count test");
        return false;
    }

    plan->Loop         = loop;
    plan->Preheader    = loop->EntryEdge(0)->getSourceBlock();
    plan->Header       = header;
    plan->Latch        = header;
    plan->Exit         = loop->ExitEdge(0)->getDestinationBlock();
    plan->IsPostIV     = true;
    plan->TripCountVar = op1->AsLclVarCommon()->GetLclNum();
    plan->TestTree     = relop;
    plan->TestBlock    = header;
    plan->TestOper     = testOper;
    plan->Step         = -1;
    JITDUMP("loop " FMT_LP " post-IV trip count V%02u\n", loop->GetIndex(), plan->TripCountVar);
    return TryAnalyzePostIVMemory(plan) && TryBuildSLPPlan(plan);
}

void AutoVectorizer::Reject(FlowGraphNaturalLoop* loop, const char* reason) const
{
    JITDUMP("rejected loop " FMT_LP ": %s\n", loop->GetIndex(), reason);
}

bool AutoVectorizer::TryAnalyzeMemory(LoopVectorizationPlan* plan)
{
    bool     foundStore     = false;
    bool     failed         = false;
    unsigned valueTempVar   = BAD_VAR_NUM;
    bool     foundValueTmp  = false;
    bool     sawBoundsCheck = false;

    auto analyzeValue = [&](Statement* stmt, GenTree* value) -> bool {
        if (!TryNormalizeScalarValue(&value, plan->ElementType) ||
            !value->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_AND, GT_OR, GT_XOR))
        {
            Reject(plan->Loop, "store data is not a supported binary operation");
            return false;
        }

        GenTreeOp* const binOp       = value->AsOp();
        GenTree*         load        = nullptr;
        GenTree*         scalar      = nullptr;
        bool             scalarIsRhs = true;

        GenTree* op1 = UnwrapCommaValue(binOp->gtOp1);
        GenTree* op2 = UnwrapCommaValue(binOp->gtOp2);

        if (TryGetIndirOperand(op1, &load))
        {
            scalar      = op2;
            scalarIsRhs = true;
        }
        else if (TryGetIndirOperand(op2, &load))
        {
            scalar      = op1;
            scalarIsRhs = false;
        }
        else
        {
            Reject(plan->Loop, "binary operation does not contain an array load");
            return false;
        }

        if (!TryAnalyzeArrayAccess(plan, stmt, load, false, plan->InductionVar, &plan->LoadAccess))
        {
            Reject(plan->Loop, "unsupported load access");
            return false;
        }

        if (!IsSupportedBinaryOp(value->OperGet(), plan->LoadAccess.ElementType))
        {
            Reject(plan->Loop, "unsupported binary operation");
            return false;
        }

        if (!TryGetInvariantOperand(plan->Loop, plan->InductionVar, scalar, plan->LoadAccess.ElementType))
        {
            Reject(plan->Loop, "binary operand is not loop invariant");
            return false;
        }

        plan->ScalarOperand      = scalar;
        plan->ScalarOper         = value->OperGet();
        plan->ScalarOperandIsRhs = scalarIsRhs;
        plan->ElementType        = plan->LoadAccess.ElementType;
        plan->ElementSize        = plan->LoadAccess.ElementSize;
        return true;
    };

    for (BasicBlock* block = plan->Preheader; block != nullptr; block = block->GetUniquePred(m_compiler))
    {
        for (Statement* const stmt : block->Statements())
        {
            RecordLocalDefs(plan, stmt->GetRootNode());
        }
    }

    plan->Loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* const root = stmt->GetRootNode();
            RecordLocalDefs(plan, root);

            if (root == plan->IterTree)
            {
                continue;
            }

            if (root->OperIs(GT_JTRUE) && (root->gtGetOp1() == plan->TestTree))
            {
                continue;
            }

            if (root->OperIs(GT_STORE_LCL_VAR))
            {
                sawBoundsCheck |= ContainsOper(root, GT_BOUNDS_CHECK);

                if (foundValueTmp || ((root->gtFlags & GTF_CALL) != 0) ||
                    !analyzeValue(stmt, root->AsLclVarCommon()->Data()))
                {
                    failed = true;
                    Reject(plan->Loop, "unsupported value temp in loop body");
                    return BasicBlockVisit::Abort;
                }

                valueTempVar  = root->AsLclVarCommon()->GetLclNum();
                foundValueTmp = true;
                continue;
            }

            if (!root->OperIs(GT_STOREIND))
            {
                failed = true;
                Reject(plan->Loop, "unsupported statement in loop body");
                return BasicBlockVisit::Abort;
            }

            if (foundStore)
            {
                failed = true;
                Reject(plan->Loop, "multiple stores");
                return BasicBlockVisit::Abort;
            }

            if ((root->gtFlags & GTF_CALL) != 0)
            {
                failed = true;
                Reject(plan->Loop, "store statement has unsupported calls");
                return BasicBlockVisit::Abort;
            }

            sawBoundsCheck |= ContainsOper(root, GT_BOUNDS_CHECK);

            GenTreeStoreInd* const store = root->AsStoreInd();
            if (!TryAnalyzeArrayAccess(plan, stmt, store, true, plan->InductionVar, &plan->StoreAccess))
            {
                failed = true;
                Reject(plan->Loop, "unsupported store access");
                return BasicBlockVisit::Abort;
            }

            GenTree* const storeData = UnwrapCommaValue(store->Data());
            if (storeData->OperIs(GT_LCL_VAR) && (storeData->AsLclVarCommon()->GetLclNum() == valueTempVar))
            {
                if (!foundValueTmp)
                {
                    failed = true;
                    Reject(plan->Loop, "store uses unknown value temp");
                    return BasicBlockVisit::Abort;
                }
            }
            else if (!analyzeValue(stmt, storeData))
            {
                failed = true;
                return BasicBlockVisit::Abort;
            }

            if ((plan->StoreAccess.ElementType != plan->LoadAccess.ElementType) &&
                (!varTypeIsIntegral(plan->StoreAccess.ElementType) ||
                 !varTypeIsIntegral(plan->LoadAccess.ElementType) ||
                 (plan->StoreAccess.ElementSize != plan->LoadAccess.ElementSize)))
            {
                failed = true;
                Reject(plan->Loop, "load/store element type mismatch");
                return BasicBlockVisit::Abort;
            }

            if (sawBoundsCheck)
            {
                unsigned lengthLcl = BAD_VAR_NUM;
                int      endOffset = 0;
                if (!TryGetArrayLengthLimitLocal(plan, plan->End, &lengthLcl, &endOffset) ||
                    (lengthLcl != plan->StoreAccess.BaseLocalIfKnown) ||
                    (lengthLcl != plan->LoadAccess.BaseLocalIfKnown) ||
                    ((plan->TestOper == GT_LE) ? (endOffset > -1) : (endOffset > 0)))
                {
                    failed = true;
                    Reject(plan->Loop, "remaining bounds check without vector-limit proof");
                    return BasicBlockVisit::Abort;
                }
            }

            if (plan->StoreAccess.IndexOffset != plan->LoadAccess.IndexOffset)
            {
                failed = true;
                Reject(plan->Loop, "possible loop-carried dependence");
                return BasicBlockVisit::Abort;
            }

            if (plan->StoreAccess.IndexOffset != 0)
            {
                failed = true;
                Reject(plan->Loop, "non-zero index offset");
                return BasicBlockVisit::Abort;
            }

            plan->StoreStmt   = stmt;
            plan->ElementType = plan->LoadAccess.ElementType;
            plan->ElementSize = plan->LoadAccess.ElementSize;
            foundStore        = true;
        }

        return BasicBlockVisit::Continue;
    });

    if (failed)
    {
        return false;
    }

    if (!foundStore)
    {
        Reject(plan->Loop, "no vectorizable store");
        return false;
    }

    JITDUMP("found %s[] store/load base V%02u, offset %d\n", varTypeName(plan->ElementType),
            plan->StoreAccess.BaseLocalIfKnown, plan->StoreAccess.IndexOffset);
    return true;
}

bool AutoVectorizer::TryAnalyzePostIVMemory(LoopVectorizationPlan* plan)
{
    assert(plan->IsPostIV);

    BasicBlock* const block                = plan->Header;
    unsigned          tempVar              = BAD_VAR_NUM;
    bool              foundTempStore       = false;
    bool              foundStore           = false;
    bool              foundTripCountUpdate = false;

    for (Statement* const stmt : block->Statements())
    {
        GenTree* const root = stmt->GetRootNode();

        if (root->OperIs(GT_JTRUE) && (root->gtGetOp1() == plan->TestTree))
        {
            continue;
        }

        if (root->OperIs(GT_STORE_LCL_VAR) && root->AsLclVarCommon()->Data()->OperIs(GT_PHI))
        {
            continue;
        }

        if (root->OperIs(GT_STOREIND))
        {
            GenTreeStoreInd* const store = root->AsStoreInd();
            if (!TryAnalyzePostIVAddress(stmt, store->Addr(), &plan->StoreAccess))
            {
                Reject(plan->Loop, "post-IV store is not a local-addressed supported store");
                return false;
            }

            if (plan->StoreAccess.ElementType == TYP_UNDEF)
            {
                plan->StoreAccess.ElementType = store->TypeGet();
                plan->StoreAccess.ElementSize = genTypeSize(plan->StoreAccess.ElementType);
            }

            if (!IsSupportedElementType(plan->StoreAccess.ElementType))
            {
                Reject(plan->Loop, "post-IV store has unsupported element type");
                return false;
            }

            if (genActualType(store->TypeGet()) != genActualType(plan->StoreAccess.ElementType))
            {
                Reject(plan->Loop, "post-IV store type does not match memory element type");
                return false;
            }

            if ((plan->ElementType != TYP_UNDEF) && (plan->ElementType != plan->StoreAccess.ElementType) &&
                (!varTypeIsIntegral(plan->ElementType) || !varTypeIsIntegral(plan->StoreAccess.ElementType) ||
                 (plan->ElementSize != plan->StoreAccess.ElementSize)))
            {
                Reject(plan->Loop, "post-IV store element type mismatch");
                return false;
            }

            plan->ElementType = (plan->ElementType == TYP_UNDEF) ? plan->StoreAccess.ElementType : plan->ElementType;
            plan->ElementSize = genTypeSize(plan->ElementType);

            plan->StoreStmt               = stmt;
            plan->StoreAccess.ElementSize = plan->ElementSize;
            plan->StoreAccess.ElementType = plan->ElementType;
            plan->StoreAccess.IsStore     = true;

            GenTree* const storeData = UnwrapCommaValue(store->Data());
            if (storeData->OperIs(GT_LCL_VAR))
            {
                if (storeData->AsLclVarCommon()->GetLclNum() != tempVar)
                {
                    Reject(plan->Loop, "post-IV store data does not match computed temp");
                    return false;
                }
            }
            else if (TryAnalyzePostIVValue(stmt, storeData, plan))
            {
                foundTempStore = true;
            }
            else
            {
                Reject(plan->Loop, "post-IV store data is unsupported");
                return false;
            }

            foundStore = true;
            continue;
        }

        if (!root->OperIs(GT_STORE_LCL_VAR))
        {
            Reject(plan->Loop, "unsupported post-IV statement");
            return false;
        }

        GenTreeLclVarCommon* const storeLcl = root->AsLclVarCommon();
        GenTree* const             data     = storeLcl->Data();
        const unsigned             lclNum   = storeLcl->GetLclNum();

        if ((lclNum == plan->TripCountVar) && data->OperIs(GT_ADD))
        {
            GenTree* op1 = data->AsOp()->gtOp1;
            GenTree* op2 = data->AsOp()->gtOp2;
            if (op2->IsIntegralConst(-1) && op1->OperIs(GT_LCL_VAR) &&
                (op1->AsLclVarCommon()->GetLclNum() == plan->TripCountVar))
            {
                foundTripCountUpdate = true;
                continue;
            }
        }

        if (data->OperIs(GT_ADD))
        {
            GenTree* op1 = data->AsOp()->gtOp1;
            GenTree* op2 = data->AsOp()->gtOp2;

            if (op2->IsCnsIntOrI() && op1->OperIs(GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == lclNum) &&
                storeLcl->TypeIs(TYP_BYREF, TYP_I_IMPL, TYP_LONG))
            {
                RecordAddressUpdate(plan, lclNum, static_cast<int>(op2->AsIntConCommon()->IconValue()));
                continue;
            }
        }

        if (TryAnalyzePostIVValue(stmt, data, plan))
        {
            tempVar        = lclNum;
            foundTempStore = true;
            continue;
        }

        Reject(plan->Loop, "unsupported post-IV statement");
        return false;
    }

    if (!foundTempStore || !foundStore || !foundTripCountUpdate)
    {
        Reject(plan->Loop, "incomplete post-IV memory pattern");
        return false;
    }

    if ((plan->ElementType == TYP_UNDEF) || !IsSupportedElementType(plan->ElementType))
    {
        Reject(plan->Loop, "post-IV unsupported element type");
        return false;
    }

    if (plan->StoreAccess.IsArray || plan->LoadAccess.IsArray)
    {
        if (!plan->StoreAccess.IsArray || !plan->LoadAccess.IsArray ||
            (plan->StoreAccess.OffsetLocalIfKnown != plan->LoadAccess.OffsetLocalIfKnown))
        {
            Reject(plan->Loop, "post-IV array accesses do not share the same offset");
            return false;
        }
    }

    if (plan->StoreAccess.IsByrefBaseWithOffset || plan->LoadAccess.IsByrefBaseWithOffset)
    {
        if (!plan->StoreAccess.IsByrefBaseWithOffset || !plan->LoadAccess.IsByrefBaseWithOffset ||
            (plan->StoreAccess.OffsetLocalIfKnown != plan->LoadAccess.OffsetLocalIfKnown))
        {
            Reject(plan->Loop, "post-IV byref+offset accesses do not share the same offset");
            return false;
        }
    }

#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = GetVectorSizeBytes(plan->ElementType);
    plan->VectorizationFactor = plan->VectorSizeBytes / plan->ElementSize;
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(plan->Loop, "vectorization factor is too small");
        return false;
    }

    const unsigned storeUpdateVar =
        plan->StoreAccess.IsByrefLocal ? plan->StoreAccess.BaseLocalIfKnown : plan->StoreAccess.OffsetLocalIfKnown;
    const unsigned loadUpdateVar =
        plan->LoadAccess.IsByrefLocal ? plan->LoadAccess.BaseLocalIfKnown : plan->LoadAccess.OffsetLocalIfKnown;

    if (!HasAddressUpdate(plan, storeUpdateVar) || !HasAddressUpdate(plan, loadUpdateVar))
    {
        Reject(plan->Loop, "missing post-IV address update");
        return false;
    }

    bool foundAddressInit     = false;
    bool foundLoadAddressInit = false;
    bool foundTripCountInit   = false;
    for (Statement* const stmt : plan->Preheader->Statements())
    {
        GenTree* const root = stmt->GetRootNode();
        if (!root->OperIs(GT_STORE_LCL_VAR))
        {
            continue;
        }

        const unsigned lclNum = root->AsLclVarCommon()->GetLclNum();
        foundAddressInit |= (lclNum == storeUpdateVar);
        foundLoadAddressInit |= (lclNum == loadUpdateVar);
        foundTripCountInit |= (lclNum == plan->TripCountVar);
    }

    if (!foundAddressInit || !foundLoadAddressInit || !foundTripCountInit)
    {
        Reject(plan->Loop, "missing post-IV preheader initialization");
        return false;
    }

    JITDUMP("found post-IV %s byref loop store V%02u, load V%02u, trip count V%02u\n", varTypeName(plan->ElementType),
            plan->StoreAccess.BaseLocalIfKnown, plan->LoadAccess.BaseLocalIfKnown, plan->TripCountVar);
    return true;
}

bool AutoVectorizer::TryAnalyzePostIVValue(Statement* stmt, GenTree* data, LoopVectorizationPlan* plan)
{
    if (!TryNormalizeScalarValue(&data, plan->ElementType))
    {
        return false;
    }

    if (!data->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_AND, GT_OR, GT_XOR))
    {
        return false;
    }

    GenTree*   load        = nullptr;
    GenTree*   scalar      = nullptr;
    bool       scalarIsRhs = true;
    genTreeOps oper        = data->OperGet();

    GenTree* op1 = UnwrapCommaValue(data->AsOp()->gtOp1);
    GenTree* op2 = UnwrapCommaValue(data->AsOp()->gtOp2);

    if (TryGetIndirOperand(op1, &load))
    {
        scalar      = op2;
        scalarIsRhs = true;
    }
    else if (TryGetIndirOperand(op2, &load))
    {
        scalar      = op1;
        scalarIsRhs = false;
    }

    if (load == nullptr)
    {
        return false;
    }

    if (!TryAnalyzePostIVAddress(stmt, load->AsIndir()->Addr(), &plan->LoadAccess))
    {
        Reject(plan->Loop, "post-IV load is not a local-addressed supported load");
        return false;
    }

    if (plan->LoadAccess.ElementType == TYP_UNDEF)
    {
        plan->LoadAccess.ElementType = load->TypeGet();
        plan->LoadAccess.ElementSize = genTypeSize(plan->LoadAccess.ElementType);
    }

    if (!IsSupportedElementType(plan->LoadAccess.ElementType))
    {
        Reject(plan->Loop, "post-IV load has unsupported element type");
        return false;
    }

    if (!IsSupportedBinaryOp(oper, plan->LoadAccess.ElementType))
    {
        Reject(plan->Loop, "post-IV unsupported binary operation");
        return false;
    }

    if (!TryGetInvariantOperand(plan->Loop, plan->TripCountVar, scalar, plan->LoadAccess.ElementType))
    {
        Reject(plan->Loop, "post-IV scalar operand is not invariant");
        return false;
    }

    if ((plan->ElementType != TYP_UNDEF) && (plan->ElementType != plan->LoadAccess.ElementType))
    {
        Reject(plan->Loop, "post-IV load element type mismatch");
        return false;
    }

    plan->ElementType = plan->LoadAccess.ElementType;
    plan->ElementSize = genTypeSize(plan->ElementType);

    plan->LoadAccess.ElementSize = plan->ElementSize;
    plan->LoadAccess.ElementType = plan->ElementType;
    plan->LoadAccess.IsLoad      = true;
    plan->ScalarOperand          = scalar;
    plan->ScalarOper             = oper;
    plan->ScalarOperandIsRhs     = scalarIsRhs;
    return true;
}

bool AutoVectorizer::TryGetIndirOperand(GenTree* tree, GenTree** indir)
{
    tree = UnwrapCommaValue(tree);
    if (tree->OperIs(GT_IND))
    {
        *indir = tree;
        return true;
    }

    if (!tree->OperIs(GT_CAST) || tree->AsCast()->gtOverflow())
    {
        return false;
    }

    GenTree* const castOp = UnwrapCommaValue(tree->AsCast()->CastOp());
    if (!castOp->OperIs(GT_IND))
    {
        return false;
    }

    *indir = castOp;
    return true;
}

bool AutoVectorizer::TryNormalizeScalarValue(GenTree** value, var_types elementType) const
{
    GenTree* tree = *value;
    while (tree->OperIs(GT_COMMA))
    {
        tree = tree->AsOp()->gtOp2;
    }

    if (!tree->OperIs(GT_CAST))
    {
        *value = tree;
        return true;
    }

    GenTreeCast* const cast = tree->AsCast();
    if (cast->gtOverflow())
    {
        return false;
    }

    const var_types castToType = cast->CastToType();
    if ((elementType == TYP_UNDEF) && varTypeIsIntegral(castToType))
    {
        *value = cast->CastOp();
        return true;
    }

    if (varTypeIsIntegral(elementType) && (genActualType(castToType) == genActualType(elementType)))
    {
        *value = cast->CastOp();
        return true;
    }

    return false;
}

bool AutoVectorizer::TryBuildSLPPlan(LoopVectorizationPlan* plan)
{
    assert(plan->VectorizationFactor <= MaxLanes);

    SLPPlan* const slpPlan = &plan->BodyPlan;

    PackNode* const load = NewPackNode(slpPlan, PackKind::LoadContiguous, plan->ElementType, plan->VectorizationFactor);
    if (load == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    load->Lanes[0] = plan->LoadAccess.Address;
    load->Cost     = 1;

    const PackKind  splatKind = plan->ScalarOperand->IsCnsIntOrI() ? PackKind::SplatConstant : PackKind::SplatScalar;
    PackNode* const splat     = NewPackNode(slpPlan, splatKind, plan->ElementType, plan->VectorizationFactor);
    if (splat == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    splat->Lanes[0] = plan->ScalarOperand;
    splat->Cost     = 1;

    PackNode* const add = NewPackNode(slpPlan, PackKind::BinaryOp, plan->ElementType, plan->VectorizationFactor);
    if (add == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    add->Oper        = plan->ScalarOper;
    add->Operands[0] = plan->ScalarOperandIsRhs ? load : splat;
    add->Operands[1] = plan->ScalarOperandIsRhs ? splat : load;
    add->Cost        = 1;

    PackNode* const store =
        NewPackNode(slpPlan, PackKind::StoreContiguous, plan->ElementType, plan->VectorizationFactor);
    if (store == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    store->Lanes[0]    = plan->StoreAccess.Address;
    store->Operands[0] = add;
    store->Cost        = 1;

    slpPlan->Root                   = store;
    slpPlan->EstimatedScalarCost    = plan->VectorizationFactor * 3;
    slpPlan->EstimatedVectorCost    = load->Cost + splat->Cost + add->Cost + store->Cost;
    slpPlan->EstimatedCodeSizeDelta = 12;

    JITDUMP("SLP accepted, estimated scalar=%u, vector=%u, codeSizeDelta=%u\n", slpPlan->EstimatedScalarCost,
            slpPlan->EstimatedVectorCost, slpPlan->EstimatedCodeSizeDelta);
    return true;
}

bool AutoVectorizer::TryRewritePlan(LoopVectorizationPlan* plan)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    if (!ReportVectorIsa(plan->VectorSizeBytes))
    {
        Reject(plan->Loop, "target ISA unavailable for selected vector size");
        return false;
    }

    if (plan->StoreStmt == nullptr)
    {
        Reject(plan->Loop, "missing store statement for rewrite");
        return false;
    }

    const bool needsOverlapCheck = plan->IsPostIV &&
                                   (plan->LoadAccess.IsByrefLocal || plan->LoadAccess.IsByrefBaseWithOffset) &&
                                   (plan->StoreAccess.IsByrefLocal || plan->StoreAccess.IsByrefBaseWithOffset) &&
                                   ((plan->LoadAccess.BaseLocalIfKnown != plan->StoreAccess.BaseLocalIfKnown) ||
                                    (plan->LoadAccess.OffsetLocalIfKnown != plan->StoreAccess.OffsetLocalIfKnown));

    BasicBlock* const scalarGuard = m_compiler->fgNewBBbefore(BBJ_COND, plan->Header, false);
    BasicBlock* const loadBeforeCheck =
        needsOverlapCheck ? m_compiler->fgNewBBbefore(BBJ_COND, scalarGuard, false) : nullptr;
    BasicBlock* const storeBeforeCheck =
        needsOverlapCheck ? m_compiler->fgNewBBbefore(BBJ_COND, loadBeforeCheck, false) : nullptr;
    BasicBlock* const sameStartCheck =
        needsOverlapCheck ? m_compiler->fgNewBBbefore(BBJ_COND, storeBeforeCheck, false) : nullptr;
    BasicBlock* const vectorCheck =
        m_compiler->fgNewBBbefore(BBJ_COND, needsOverlapCheck ? sameStartCheck : scalarGuard, false);
    BasicBlock* const vectorBody = m_compiler->fgNewBBbefore(BBJ_ALWAYS, vectorCheck, false);

    vectorCheck->clearTryIndex();
    vectorCheck->clearHndIndex();
    vectorBody->clearTryIndex();
    vectorBody->clearHndIndex();
    if (sameStartCheck != nullptr)
    {
        sameStartCheck->clearTryIndex();
        sameStartCheck->clearHndIndex();
        storeBeforeCheck->clearTryIndex();
        storeBeforeCheck->clearHndIndex();
        loadBeforeCheck->clearTryIndex();
        loadBeforeCheck->clearHndIndex();
    }
    scalarGuard->clearTryIndex();
    scalarGuard->clearHndIndex();

    vectorCheck->inheritWeight(plan->Preheader);
    vectorBody->inheritWeight(plan->Header);
    vectorBody->scaleBBWeight(1.0 / static_cast<weight_t>(plan->VectorizationFactor));
    scalarGuard->inheritWeightPercentage(plan->Header, 25);
    if (sameStartCheck != nullptr)
    {
        sameStartCheck->inheritWeightPercentage(plan->Header, 25);
        storeBeforeCheck->inheritWeightPercentage(plan->Header, 25);
        loadBeforeCheck->inheritWeightPercentage(plan->Header, 25);
    }

    GenTree* const   vectorTest = BuildVectorLoopTest(plan);
    Statement* const testStmt   = m_compiler->fgNewStmtAtEnd(vectorCheck, vectorTest);
    m_compiler->gtSetStmtInfo(testStmt);
    m_compiler->fgSetStmtSeq(testStmt);

    JITDUMP("scalar store selected for rewrite:\n");
    JITDUMPEXEC(m_compiler->gtDispStmt(plan->StoreStmt));

    GenTree* const vectorStore = BuildVectorStore(plan);
    JITDUMP("generated vector store tree:\n");
    JITDUMPEXEC(m_compiler->gtDispTree(vectorStore));

    Statement* const storeStmt = m_compiler->fgNewStmtAtEnd(vectorBody, vectorStore);
    m_compiler->gtSetStmtInfo(storeStmt);
    m_compiler->fgSetStmtSeq(storeStmt);

    if (plan->IsPostIV)
    {
        const unsigned storeUpdateVar =
            plan->StoreAccess.IsByrefLocal ? plan->StoreAccess.BaseLocalIfKnown : plan->StoreAccess.OffsetLocalIfKnown;
        const unsigned loadUpdateVar =
            plan->LoadAccess.IsByrefLocal ? plan->LoadAccess.BaseLocalIfKnown : plan->LoadAccess.OffsetLocalIfKnown;

        Statement* const addressUpdateStmt =
            m_compiler->fgNewStmtAtEnd(vectorBody, BuildAddressUpdate(plan, storeUpdateVar));
        m_compiler->gtSetStmtInfo(addressUpdateStmt);
        m_compiler->fgSetStmtSeq(addressUpdateStmt);

        if (loadUpdateVar != storeUpdateVar)
        {
            Statement* const loadAddressUpdateStmt =
                m_compiler->fgNewStmtAtEnd(vectorBody, BuildAddressUpdate(plan, loadUpdateVar));
            m_compiler->gtSetStmtInfo(loadAddressUpdateStmt);
            m_compiler->fgSetStmtSeq(loadAddressUpdateStmt);
        }

        Statement* const tripCountUpdateStmt =
            m_compiler->fgNewStmtAtEnd(vectorBody,
                                       BuildTripCountUpdate(plan, -static_cast<int>(plan->VectorizationFactor)));
        m_compiler->gtSetStmtInfo(tripCountUpdateStmt);
        m_compiler->fgSetStmtSeq(tripCountUpdateStmt);
    }
    else
    {
        GenTree* const   ivUpdate = BuildIVUpdate(plan);
        Statement* const ivStmt   = m_compiler->fgNewStmtAtEnd(vectorBody, ivUpdate);
        m_compiler->gtSetStmtInfo(ivStmt);
        m_compiler->fgSetStmtSeq(ivStmt);
    }

    Statement* const scalarGuardStmt = m_compiler->fgNewStmtAtEnd(scalarGuard, BuildScalarRemainderTest(plan));
    m_compiler->gtSetStmtInfo(scalarGuardStmt);
    m_compiler->fgSetStmtSeq(scalarGuardStmt);

    m_compiler->fgRedirectEdge(plan->Preheader->TargetEdgeRef(), vectorCheck);

    FlowEdge* const vectorCheckToVectorBody =
        m_compiler->fgAddRefPred(needsOverlapCheck ? sameStartCheck : vectorBody, vectorCheck);
    FlowEdge* const vectorCheckToScalar = m_compiler->fgAddRefPred(scalarGuard, vectorCheck);
    vectorCheckToVectorBody->setLikelihood(0.9);
    vectorCheckToScalar->setLikelihood(0.1);
    vectorCheck->SetCond(vectorCheckToVectorBody, vectorCheckToScalar);

    FlowEdge* const vectorBodyToCheck = m_compiler->fgAddRefPred(vectorCheck, vectorBody);
    vectorBodyToCheck->setLikelihood(1.0);
    vectorBody->SetKindAndTargetEdge(BBJ_ALWAYS, vectorBodyToCheck);

    if (sameStartCheck != nullptr)
    {
        Statement* const sameStartStmt = m_compiler->fgNewStmtAtEnd(sameStartCheck, BuildPostIVSameStartCheck(plan));
        m_compiler->gtSetStmtInfo(sameStartStmt);
        m_compiler->fgSetStmtSeq(sameStartStmt);

        Statement* const storeBeforeStmt =
            m_compiler->fgNewStmtAtEnd(storeBeforeCheck, BuildPostIVStoreBeforeLoadCheck(plan));
        m_compiler->gtSetStmtInfo(storeBeforeStmt);
        m_compiler->fgSetStmtSeq(storeBeforeStmt);

        Statement* const loadBeforeStmt =
            m_compiler->fgNewStmtAtEnd(loadBeforeCheck, BuildPostIVLoadBeforeStoreCheck(plan));
        m_compiler->gtSetStmtInfo(loadBeforeStmt);
        m_compiler->fgSetStmtSeq(loadBeforeStmt);

        FlowEdge* const sameStartToVector = m_compiler->fgAddRefPred(vectorBody, sameStartCheck);
        FlowEdge* const sameStartToNext   = m_compiler->fgAddRefPred(storeBeforeCheck, sameStartCheck);
        sameStartToVector->setLikelihood(0.1);
        sameStartToNext->setLikelihood(0.9);
        sameStartCheck->SetCond(sameStartToVector, sameStartToNext);

        FlowEdge* const storeBeforeToVector = m_compiler->fgAddRefPred(vectorBody, storeBeforeCheck);
        FlowEdge* const storeBeforeToNext   = m_compiler->fgAddRefPred(loadBeforeCheck, storeBeforeCheck);
        storeBeforeToVector->setLikelihood(0.45);
        storeBeforeToNext->setLikelihood(0.55);
        storeBeforeCheck->SetCond(storeBeforeToVector, storeBeforeToNext);

        FlowEdge* const loadBeforeToVector = m_compiler->fgAddRefPred(vectorBody, loadBeforeCheck);
        FlowEdge* const loadBeforeToScalar = m_compiler->fgAddRefPred(scalarGuard, loadBeforeCheck);
        loadBeforeToVector->setLikelihood(0.45);
        loadBeforeToScalar->setLikelihood(0.55);
        loadBeforeCheck->SetCond(loadBeforeToVector, loadBeforeToScalar);
    }

    FlowEdge* const scalarGuardToExit   = m_compiler->fgAddRefPred(plan->Exit, scalarGuard);
    FlowEdge* const scalarGuardToScalar = m_compiler->fgAddRefPred(plan->Header, scalarGuard);
    scalarGuardToExit->setLikelihood(0.5);
    scalarGuardToScalar->setLikelihood(0.5);
    scalarGuard->SetCond(scalarGuardToExit, scalarGuardToScalar);

    JITDUMP("rewrote loop " FMT_LP " with %u-byte vectors and scalar epilogue\n", plan->Loop->GetIndex(),
            plan->VectorSizeBytes);
    m_compiler->Metrics.LoopsVectorized++;
    return true;
#else
    Reject(plan->Loop, "SIMD rewrite not supported on target");
    return false;
#endif
}

AutoVectorizer::PackNode* AutoVectorizer::NewPackNode(SLPPlan*  slpPlan,
                                                      PackKind  kind,
                                                      var_types elementType,
                                                      unsigned  laneCount)
{
    if ((slpPlan->NodeCount >= MaxPackNodes) || (laneCount > MaxLanes))
    {
        return nullptr;
    }

    PackNode* const node = &slpPlan->Nodes[slpPlan->NodeCount++];
    node->Kind           = kind;
    node->ElementType    = elementType;
    node->LaneCount      = laneCount;
    return node;
}

const char* AutoVectorizer::PackKindName(PackKind kind) const
{
    switch (kind)
    {
        case PackKind::LoadContiguous:
            return "load-contiguous";
        case PackKind::StoreContiguous:
            return "store-contiguous";
        case PackKind::SplatConstant:
            return "splat-constant";
        case PackKind::SplatScalar:
            return "splat-scalar";
        case PackKind::BinaryOp:
            return "binary";
        default:
            return "invalid";
    }
}

void AutoVectorizer::DumpSLPPlan(const LoopVectorizationPlan& plan) const
{
#ifdef DEBUG
    const SLPPlan& slpPlan = plan.BodyPlan;
    JITDUMP("pack tree:\n");
    for (unsigned i = 0; i < slpPlan.NodeCount; i++)
    {
        const PackNode& node = slpPlan.Nodes[i];
        JITDUMP("  N%02u %-17s lanes=%u elem=%s cost=%u", i, PackKindName(node.Kind), node.LaneCount,
                varTypeName(node.ElementType), node.Cost);

        if (node.Kind == PackKind::BinaryOp)
        {
            JITDUMP(" op=%s", GenTree::OpName(node.Oper));
        }

        if (node.Operands[0] != nullptr)
        {
            JITDUMP(" op0=N%02u", static_cast<unsigned>(node.Operands[0] - slpPlan.Nodes));
        }

        if (node.Operands[1] != nullptr)
        {
            JITDUMP(" op1=N%02u", static_cast<unsigned>(node.Operands[1] - slpPlan.Nodes));
        }

        JITDUMP("\n");
    }
#endif
}

GenTree* AutoVectorizer::BuildArrayAddress(LoopVectorizationPlan*                     plan,
                                           const LoopVectorizationPlan::ScalarAccess& access)
{
    GenTreeArrAddr* const oldArrAddr  = access.Address->AsArrAddr();
    const unsigned        elemSize    = access.ElementSize;
    const unsigned        firstOffset = oldArrAddr->GetFirstElemOffset();
    const unsigned        arrayLcl    = access.BaseLocalIfKnown;

    LclVarDsc* const arrayDsc = m_compiler->lvaGetDesc(arrayLcl);
    GenTree* const   arrRef   = m_compiler->gtNewLclvNode(arrayLcl, arrayDsc->TypeGet());
    LclVarDsc* const ivDsc    = m_compiler->lvaGetDesc(plan->InductionVar);
    GenTree*         index    = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());

    if (access.IndexOffset != 0)
    {
        index =
            m_compiler->gtNewOperNode(GT_ADD, index->TypeGet(), index, m_compiler->gtNewIconNode(access.IndexOffset));
    }

#ifdef TARGET_64BIT
    if (!index->TypeIs(TYP_I_IMPL))
    {
        index = m_compiler->gtNewCastNode(TYP_I_IMPL, index, true, TYP_I_IMPL);
    }
#endif

    GenTree* offset = index;
    if (elemSize > 1)
    {
        GenTree* const scale = m_compiler->gtNewIconNode(elemSize, TYP_I_IMPL);
        scale->gtFlags |= GTF_DONT_CSE;
        offset = m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL, offset, scale);
    }

    if (firstOffset != 0)
    {
        offset =
            m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, offset, m_compiler->gtNewIconNode(firstOffset, TYP_I_IMPL));
    }

    const var_types arrPtrType = arrRef->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree* const  addr       = m_compiler->gtNewOperNode(GT_ADD, arrPtrType, arrRef, offset);
    GenTreeArrAddr* arrAddr    = new (m_compiler, GT_ARR_ADDR)
        GenTreeArrAddr(addr, access.ElementType, oldArrAddr->GetElemClassHandle(), static_cast<uint8_t>(firstOffset));
    arrAddr->gtFlags |= oldArrAddr->gtFlags & GTF_ARR_ADDR_NONNULL;
    return arrAddr;
}

GenTree* AutoVectorizer::BuildVectorLoopTest(LoopVectorizationPlan* plan)
{
    if (plan->IsPostIV)
    {
        GenTree* const tripCount = m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT);
        GenTree* const cmp =
            m_compiler->gtNewOperNode(GT_GE, TYP_INT, tripCount, m_compiler->gtNewIconNode(plan->VectorizationFactor));
        return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
    }

    GenTree* iv  = m_compiler->gtNewLclvNode(plan->InductionVar, TYP_INT);
    GenTree* end = m_compiler->gtCloneExpr(plan->End);

    GenTree* lastLane = m_compiler->gtNewCastNode(TYP_LONG, iv, false, TYP_LONG);
    if (plan->VectorizationFactor > 1)
    {
        lastLane =
            m_compiler->gtNewOperNode(GT_ADD, TYP_LONG, lastLane,
                                      m_compiler->gtNewLconNode(static_cast<int64_t>(plan->VectorizationFactor - 1)));
    }

    end = m_compiler->gtNewCastNode(TYP_LONG, end, false, TYP_LONG);

    const genTreeOps cmpOper = (plan->TestOper == GT_LE) ? GT_LE : GT_LT;
    GenTree* const   cmp     = m_compiler->gtNewOperNode(cmpOper, TYP_INT, lastLane, end);
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVSameStartCheck(LoopVectorizationPlan* plan)
{
    GenTree* const cmp = m_compiler->gtNewOperNode(GT_EQ, TYP_INT, BuildPostIVAddress(plan->StoreAccess),
                                                   BuildPostIVAddress(plan->LoadAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVStoreBeforeLoadCheck(LoopVectorizationPlan* plan)
{
    GenTree* const byteCount =
        m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL,
                                  m_compiler->gtNewCastNode(TYP_I_IMPL,
                                                            m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT),
                                                            true, TYP_I_IMPL),
                                  m_compiler->gtNewIconNode(plan->ElementSize, TYP_I_IMPL));
    GenTree* const storeEnd =
        m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, BuildPostIVAddress(plan->StoreAccess), byteCount);
    GenTree* const cmp = m_compiler->gtNewOperNode(GT_LE, TYP_INT, storeEnd, BuildPostIVAddress(plan->LoadAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVLoadBeforeStoreCheck(LoopVectorizationPlan* plan)
{
    GenTree* const byteCount =
        m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL,
                                  m_compiler->gtNewCastNode(TYP_I_IMPL,
                                                            m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT),
                                                            true, TYP_I_IMPL),
                                  m_compiler->gtNewIconNode(plan->ElementSize, TYP_I_IMPL));
    GenTree* const loadEnd =
        m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, BuildPostIVAddress(plan->LoadAccess), byteCount);
    GenTree* const cmp = m_compiler->gtNewOperNode(GT_LE, TYP_INT, loadEnd, BuildPostIVAddress(plan->StoreAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVAddress(const LoopVectorizationPlan::ScalarAccess& access)
{
    if (access.IsByrefLocal)
    {
        LclVarDsc* const addrDsc = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
        return m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, addrDsc->TypeGet());
    }

    if (access.IsByrefBaseWithOffset)
    {
        LclVarDsc* const baseDsc   = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
        LclVarDsc* const offsetDsc = m_compiler->lvaGetDesc(access.OffsetLocalIfKnown);
        GenTree* const   base      = m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, baseDsc->TypeGet());
        GenTree* const   offset    = m_compiler->gtNewLclvNode(access.OffsetLocalIfKnown, offsetDsc->TypeGet());
        return m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, base, offset);
    }

    GenTreeArrAddr* const oldArrAddr = access.Address->AsArrAddr();
    LclVarDsc* const      arrayDsc   = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
    LclVarDsc* const      offsetDsc  = m_compiler->lvaGetDesc(access.OffsetLocalIfKnown);
    GenTree* const        arrRef     = m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, arrayDsc->TypeGet());
    GenTree* const        offset     = m_compiler->gtNewLclvNode(access.OffsetLocalIfKnown, offsetDsc->TypeGet());
    GenTree* const        addr       = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, arrRef, offset);
    GenTreeArrAddr* const arrAddr    = new (m_compiler, GT_ARR_ADDR)
        GenTreeArrAddr(addr, access.ElementType, oldArrAddr->GetElemClassHandle(), oldArrAddr->GetFirstElemOffset());
    arrAddr->gtFlags |= oldArrAddr->gtFlags & GTF_ARR_ADDR_NONNULL;
    return arrAddr;
}

GenTree* AutoVectorizer::BuildVectorStore(LoopVectorizationPlan* plan)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned  simdSize = plan->VectorSizeBytes;
    const var_types simdType = Compiler::getSIMDTypeForSize(simdSize);

    GenTree* const loadAddress =
        plan->IsPostIV ? BuildPostIVAddress(plan->LoadAccess) : BuildArrayAddress(plan, plan->LoadAccess);
    GenTree* const storeAddress =
        plan->IsPostIV ? BuildPostIVAddress(plan->StoreAccess) : BuildArrayAddress(plan, plan->StoreAccess);
    GenTree* const vectorLoad = m_compiler->gtNewSimdLoadNode(simdType, loadAddress, plan->ElementType, simdSize);
    GenTree* const scalar     = m_compiler->gtCloneExpr(plan->ScalarOperand);
    GenTree* const splat      = m_compiler->gtNewSimdCreateBroadcastNode(simdType, scalar, plan->ElementType, simdSize);
    GenTree* const op1        = plan->ScalarOperandIsRhs ? vectorLoad : splat;
    GenTree* const op2        = plan->ScalarOperandIsRhs ? splat : vectorLoad;
    GenTree* const vectorResult =
        m_compiler->gtNewSimdBinOpNode(plan->ScalarOper, simdType, op1, op2, plan->ElementType, simdSize);
    return m_compiler->gtNewSimdStoreNode(storeAddress, vectorResult, plan->ElementType, simdSize);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildIVUpdate(LoopVectorizationPlan* plan)
{
    GenTree* const iv = m_compiler->gtNewLclvNode(plan->InductionVar, TYP_INT);
    GenTree* const add =
        m_compiler->gtNewOperNode(GT_ADD, TYP_INT, iv, m_compiler->gtNewIconNode(plan->VectorizationFactor));
    return m_compiler->gtNewStoreLclVarNode(plan->InductionVar, add);
}

GenTree* AutoVectorizer::BuildAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar)
{
    LclVarDsc* const addressDsc = m_compiler->lvaGetDesc(addressVar);
    GenTree* const   address    = m_compiler->gtNewLclvNode(addressVar, addressDsc->TypeGet());
    GenTree* const   add        = m_compiler->gtNewOperNode(GT_ADD, addressDsc->TypeGet(), address,
                                                            m_compiler->gtNewIconNode(plan->VectorSizeBytes, TYP_I_IMPL));
    return m_compiler->gtNewStoreLclVarNode(addressVar, add);
}

GenTree* AutoVectorizer::BuildTripCountUpdate(LoopVectorizationPlan* plan, int delta)
{
    GenTree* const tripCount = m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT);
    GenTree* const add       = m_compiler->gtNewOperNode(GT_ADD, TYP_INT, tripCount, m_compiler->gtNewIconNode(delta));
    return m_compiler->gtNewStoreLclVarNode(plan->TripCountVar, add);
}

GenTree* AutoVectorizer::BuildScalarRemainderTest(LoopVectorizationPlan* plan)
{
    GenTree* cmp;
    if (plan->IsPostIV)
    {
        GenTree* const tripCount = m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT);
        cmp                      = m_compiler->gtNewOperNode(GT_EQ, TYP_INT, tripCount, m_compiler->gtNewIconNode(0));
    }
    else
    {
        GenTree* const iv  = m_compiler->gtNewLclvNode(plan->InductionVar, TYP_INT);
        GenTree* const end = m_compiler->gtCloneExpr(plan->End);
        genTreeOps     op;
        switch (plan->TestOper)
        {
            case GT_LT:
                op = GT_GE;
                break;
            case GT_LE:
                op = GT_GT;
                break;
            case GT_NE:
                op = GT_EQ;
                break;
            default:
                unreached();
        }

        cmp = m_compiler->gtNewOperNode(op, TYP_INT, iv, end);
    }

    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::UnwrapCommaValue(GenTree* tree)
{
    while (tree->OperIs(GT_COMMA))
    {
        tree = tree->AsOp()->gtOp2;
    }

    return tree;
}

bool AutoVectorizer::TryAnalyzeArrayAccess(LoopVectorizationPlan*               plan,
                                           Statement*                           stmt,
                                           GenTree*                             indir,
                                           bool                                 isStore,
                                           unsigned                             ivLcl,
                                           LoopVectorizationPlan::ScalarAccess* access)
{
    assert(indir->OperIs(GT_IND, GT_STOREIND));

    GenTreeIndir* const indirNode = indir->AsIndir();
    GenTree* const      addr      = UnwrapCommaValue(indirNode->Addr());
    if (indirNode->IsVolatile() || !addr->OperIs(GT_ARR_ADDR))
    {
        return false;
    }

    if (!TryAnalyzeArrayAddress(plan, addr->AsArrAddr(), ivLcl, access))
    {
        return false;
    }

    access->StatementRoot = stmt;
    access->Address       = addr;
    access->ElementSize   = genTypeSize(access->ElementType);
    access->IsLoad        = !isStore;
    access->IsStore       = isStore;
    access->IsVolatile    = indirNode->IsVolatile();

    return true;
}

bool AutoVectorizer::TryAnalyzePostIVAddress(Statement*                           stmt,
                                             GenTree*                             addr,
                                             LoopVectorizationPlan::ScalarAccess* access)
{
    addr = UnwrapCommaValue(addr);

    unsigned byrefLcl = BAD_VAR_NUM;
    if (TryAnalyzeByrefLocalAddress(addr, &byrefLcl))
    {
        access->StatementRoot    = stmt;
        access->Address          = addr;
        access->BaseLocalIfKnown = byrefLcl;
        access->IsByrefLocal     = true;
        return true;
    }

    if (addr->OperIs(GT_ARR_ADDR))
    {
        access->StatementRoot = stmt;
        access->Address       = addr;
        access->IsArray       = true;
        return TryAnalyzePostIVArrayAddress(addr->AsArrAddr(), access);
    }

    if (addr->OperIs(GT_ADD) && addr->TypeIs(TYP_BYREF))
    {
        GenTree* op1 = UnwrapCommaValue(addr->AsOp()->gtOp1);
        GenTree* op2 = UnwrapCommaValue(addr->AsOp()->gtOp2);

        if (!op1->OperIs(GT_LCL_VAR) || !op1->TypeIs(TYP_BYREF))
        {
            std::swap(op1, op2);
        }

        if (op1->OperIs(GT_LCL_VAR) && op1->TypeIs(TYP_BYREF) && op2->OperIs(GT_LCL_VAR) &&
            op2->TypeIs(TYP_I_IMPL, TYP_LONG, TYP_INT))
        {
            access->StatementRoot         = stmt;
            access->Address               = addr;
            access->BaseLocalIfKnown      = op1->AsLclVarCommon()->GetLclNum();
            access->OffsetLocalIfKnown    = op2->AsLclVarCommon()->GetLclNum();
            access->IsByrefBaseWithOffset = true;
            return true;
        }
    }

    return false;
}

bool AutoVectorizer::TryAnalyzeByrefLocalAddress(GenTree* addr, unsigned* lclNum)
{
    if (addr->OperIs(GT_COMMA))
    {
        return TryAnalyzeByrefLocalAddress(addr->AsOp()->gtOp2, lclNum);
    }

    if (!addr->OperIs(GT_LCL_VAR) || !addr->TypeIs(TYP_BYREF))
    {
        return false;
    }

    *lclNum = addr->AsLclVarCommon()->GetLclNum();
    return true;
}

bool AutoVectorizer::TryAnalyzePostIVArrayAddress(GenTreeArrAddr* arrAddr, LoopVectorizationPlan::ScalarAccess* access)
{
    struct AddressParts
    {
        unsigned ArrayLcl  = BAD_VAR_NUM;
        unsigned OffsetLcl = BAD_VAR_NUM;
    };

    AddressParts parts;

    class AddressVisitor
    {
    public:
        explicit AddressVisitor(AddressParts* parts)
            : m_parts(parts)
        {
        }

        bool Analyze(GenTree* tree)
        {
            tree = Unwrap(tree);

            if (tree->OperIs(GT_ARR_ADDR))
            {
                return Analyze(tree->AsArrAddr()->Addr());
            }

            if (tree->OperIs(GT_ADD))
            {
                return Analyze(tree->AsOp()->gtOp1) && Analyze(tree->AsOp()->gtOp2);
            }

            if (tree->IsCnsIntOrI())
            {
                return true;
            }

            if (tree->OperIs(GT_LCL_VAR))
            {
                const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
                if (varTypeIsGC(tree->TypeGet()))
                {
                    if ((m_parts->ArrayLcl != BAD_VAR_NUM) && (m_parts->ArrayLcl != lclNum))
                    {
                        return false;
                    }

                    m_parts->ArrayLcl = lclNum;
                    return true;
                }

                if (tree->TypeIs(TYP_I_IMPL, TYP_LONG, TYP_INT))
                {
                    if ((m_parts->OffsetLcl != BAD_VAR_NUM) && (m_parts->OffsetLcl != lclNum))
                    {
                        return false;
                    }

                    m_parts->OffsetLcl = lclNum;
                    return true;
                }
            }

            return false;
        }

    private:
        static GenTree* Unwrap(GenTree* tree)
        {
            while (tree->OperIs(GT_COMMA))
            {
                tree = tree->AsOp()->gtOp2;
            }

            return tree;
        }

        AddressParts* m_parts;
    };

    AddressVisitor visitor(&parts);
    if (!visitor.Analyze(arrAddr->Addr()) || (parts.ArrayLcl == BAD_VAR_NUM) || (parts.OffsetLcl == BAD_VAR_NUM))
    {
        return false;
    }

    access->BaseLocalIfKnown   = parts.ArrayLcl;
    access->OffsetLocalIfKnown = parts.OffsetLcl;
    access->ElementSize        = genTypeSize(arrAddr->GetElemType());
    access->ElementType        = arrAddr->GetElemType();
    access->IsArray            = true;
    return IsSupportedElementType(access->ElementType);
}

bool AutoVectorizer::TryAnalyzeIndexExpr(LoopVectorizationPlan* plan, GenTree* tree, unsigned ivLcl, int* offset)
{
    tree = UnwrapCommaValue(tree);

    if (tree->OperIs(GT_CAST))
    {
        return TryAnalyzeIndexExpr(plan, tree->AsCast()->CastOp(), ivLcl, offset);
    }

    if (tree->OperIs(GT_LCL_VAR) && (tree->AsLclVarCommon()->GetLclNum() == ivLcl))
    {
        return true;
    }

    if (tree->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (TryGetLocalDef(plan, tree->AsLclVarCommon()->GetLclNum(), &def))
        {
            return TryAnalyzeIndexExpr(plan, def, ivLcl, offset);
        }
    }

    if (tree->OperIs(GT_ADD))
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op1->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(plan, op2, ivLcl, offset);
        }

        if (op2->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op2->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(plan, op1, ivLcl, offset);
        }
    }

    return false;
}

bool AutoVectorizer::TryAnalyzeArrayAddress(LoopVectorizationPlan*               plan,
                                            GenTreeArrAddr*                      arrAddr,
                                            unsigned                             ivLcl,
                                            LoopVectorizationPlan::ScalarAccess* access)
{
    struct AddressParts
    {
        unsigned ArrayLcl   = BAD_VAR_NUM;
        ssize_t  Offset     = 0;
        ssize_t  IndexScale = 0;
    };

    const unsigned elemSize        = genTypeSize(arrAddr->GetElemType());
    const ssize_t  firstElemOffset = arrAddr->GetFirstElemOffset();
    AddressParts   parts;

    class AddressVisitor
    {
    public:
        AddressVisitor(AutoVectorizer*        vectorizer,
                       LoopVectorizationPlan* plan,
                       unsigned               ivLcl,
                       unsigned               elemSize,
                       AddressParts*          parts)
            : m_vectorizer(vectorizer)
            , m_plan(plan)
            , m_ivLcl(ivLcl)
            , m_elemSize(elemSize)
            , m_parts(parts)
        {
        }

        bool Analyze(GenTree* tree)
        {
            tree = m_vectorizer->UnwrapCommaValue(tree);

            if (tree->OperIs(GT_ARR_ADDR))
            {
                return Analyze(tree->AsArrAddr()->Addr());
            }

            if (tree->OperIs(GT_ADD))
            {
                return Analyze(tree->AsOp()->gtOp1) && Analyze(tree->AsOp()->gtOp2);
            }

            if (tree->IsCnsIntOrI())
            {
                m_parts->Offset += tree->AsIntConCommon()->IconValue();
                return true;
            }

            if (tree->OperIs(GT_LCL_VAR) && varTypeIsGC(tree->TypeGet()))
            {
                if (m_parts->ArrayLcl != BAD_VAR_NUM)
                {
                    return false;
                }

                m_parts->ArrayLcl = tree->AsLclVarCommon()->GetLclNum();
                return true;
            }

            if (tree->OperIs(GT_MUL))
            {
                GenTree* index = tree->AsOp()->gtOp1;
                GenTree* scale = tree->AsOp()->gtOp2;

                if (!scale->IsCnsIntOrI())
                {
                    std::swap(index, scale);
                }

                if (!scale->IsCnsIntOrI() || (scale->AsIntConCommon()->IconValue() != m_elemSize))
                {
                    return false;
                }

                int indexOffset = 0;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, index, m_ivLcl, &indexOffset))
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                return true;
            }

            if (tree->OperIs(GT_LSH) && tree->AsOp()->gtOp2->IsCnsIntOrI())
            {
                const ssize_t shift = tree->AsOp()->gtOp2->AsIntConCommon()->IconValue();
                if ((shift < 0) || (shift >= static_cast<ssize_t>(sizeof(ssize_t) * BITS_PER_BYTE)) ||
                    ((static_cast<ssize_t>(1) << shift) != static_cast<ssize_t>(m_elemSize)))
                {
                    return false;
                }

                int indexOffset = 0;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree->AsOp()->gtOp1, m_ivLcl, &indexOffset))
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                return true;
            }

            if (m_elemSize == 1)
            {
                int indexOffset = 0;
                if (m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree, m_ivLcl, &indexOffset))
                {
                    m_parts->IndexScale += 1;
                    m_parts->Offset += indexOffset;
                    return true;
                }
            }

            return false;
        }

    private:
        AutoVectorizer*        m_vectorizer;
        LoopVectorizationPlan* m_plan;
        unsigned               m_ivLcl;
        unsigned               m_elemSize;
        AddressParts*          m_parts;
    };

    AddressVisitor visitor(this, plan, ivLcl, elemSize, &parts);
    if (!visitor.Analyze(arrAddr->Addr()))
    {
        return false;
    }

    if ((parts.ArrayLcl == BAD_VAR_NUM) || (parts.IndexScale != elemSize) || (parts.Offset < firstElemOffset))
    {
        return false;
    }

    const ssize_t offsetBytes = parts.Offset - firstElemOffset;
    if ((offsetBytes % static_cast<ssize_t>(elemSize)) != 0)
    {
        return false;
    }

    access->BaseLocalIfKnown = parts.ArrayLcl;
    access->IndexOffset      = static_cast<int>(offsetBytes / static_cast<ssize_t>(elemSize));
    access->ElementSize      = elemSize;
    access->ElementType      = arrAddr->GetElemType();
    access->IsArray          = true;
    return true;
}

void AutoVectorizer::RecordLocalDefs(LoopVectorizationPlan* plan, GenTree* tree)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, AutoVectorizer* vectorizer, LoopVectorizationPlan* plan)
            : GenTreeVisitor<Visitor>(compiler)
            , m_vectorizer(vectorizer)
            , m_plan(plan)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;
            if (tree->OperIs(GT_STORE_LCL_VAR))
            {
                m_vectorizer->RecordLocalDef(m_plan, tree->AsLclVarCommon()->GetLclNum(),
                                             tree->AsLclVarCommon()->Data());
            }

            return fgWalkResult::WALK_CONTINUE;
        }

    private:
        AutoVectorizer*        m_vectorizer;
        LoopVectorizationPlan* m_plan;
    };

    Visitor visitor(m_compiler, this, plan);
    visitor.WalkTree(&tree, nullptr);
}

void AutoVectorizer::RecordLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree* value)
{
    for (unsigned i = 0; i < plan->LocalDefCount; i++)
    {
        if (plan->LocalDefVars[i] == lclNum)
        {
            plan->LocalDefValues[i] = (plan->LocalDefValues[i] == value) ? value : nullptr;
            return;
        }
    }

    if (plan->LocalDefCount >= LoopVectorizationPlan::MaxLocalDefs)
    {
        return;
    }

    const unsigned index        = plan->LocalDefCount++;
    plan->LocalDefVars[index]   = lclNum;
    plan->LocalDefValues[index] = value;
}

bool AutoVectorizer::TryGetLocalDef(LoopVectorizationPlan* plan, unsigned lclNum, GenTree** value)
{
    for (unsigned i = 0; i < plan->LocalDefCount; i++)
    {
        if ((plan->LocalDefVars[i] == lclNum) && (plan->LocalDefValues[i] != nullptr))
        {
            *value = plan->LocalDefValues[i];
            return true;
        }
    }

    return false;
}

bool AutoVectorizer::TryGetArrayLengthLimitLocal(LoopVectorizationPlan* plan,
                                                 GenTree*               tree,
                                                 unsigned*              lclNum,
                                                 int*                   offset)
{
    tree = UnwrapCommaValue(tree);

    if (tree->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (!TryGetLocalDef(plan, tree->AsLclVarCommon()->GetLclNum(), &def))
        {
            return false;
        }

        return TryGetArrayLengthLimitLocal(plan, def, lclNum, offset);
    }

    if (TryGetArrayLengthLocal(tree, lclNum))
    {
        *offset = 0;
        return true;
    }

    if (!tree->OperIs(GT_ADD, GT_SUB))
    {
        return false;
    }

    GenTree* arrLen = tree->AsOp()->gtOp1;
    GenTree* cns    = tree->AsOp()->gtOp2;
    if (tree->OperIs(GT_ADD) && arrLen->IsCnsIntOrI())
    {
        std::swap(arrLen, cns);
    }

    if (!cns->IsCnsIntOrI() || !TryGetArrayLengthLocal(arrLen, lclNum))
    {
        return false;
    }

    const ssize_t cnsVal = cns->AsIntConCommon()->IconValue();
    if ((cnsVal < INT_MIN) || (cnsVal > INT_MAX))
    {
        return false;
    }

    if (tree->OperIs(GT_SUB) && (cnsVal == INT_MIN))
    {
        return false;
    }

    *offset = static_cast<int>(tree->OperIs(GT_SUB) ? -cnsVal : cnsVal);
    return true;
}

bool AutoVectorizer::TryGetArrayLengthLocal(GenTree* tree, unsigned* lclNum)
{
    if (!tree->OperIs(GT_ARR_LENGTH))
    {
        return false;
    }

    GenTree* const arrRef = tree->AsArrLen()->ArrRef();
    if (!arrRef->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    *lclNum = arrRef->AsLclVarCommon()->GetLclNum();
    return true;
}

bool AutoVectorizer::IsCompatibleScalarType(GenTree* tree, var_types elementType) const
{
    if (varTypeIsFloating(elementType))
    {
        return tree->TypeIs(elementType);
    }

    if (varTypeIsIntegral(elementType))
    {
        return genActualType(tree) == genActualType(elementType);
    }

    return false;
}

bool AutoVectorizer::TryGetInvariantOperand(FlowGraphNaturalLoop* loop,
                                            unsigned              ivLcl,
                                            GenTree*              tree,
                                            var_types             elementType)
{
    if (!IsCompatibleScalarType(tree, elementType))
    {
        return false;
    }

    if (tree->IsCnsIntOrI() || tree->IsCnsFltOrDbl())
    {
        return true;
    }

    if (!tree->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
    return (lclNum != ivLcl) && !loop->HasDef(lclNum);
}

void AutoVectorizer::RecordAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar, int delta)
{
    for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
    {
        if (plan->AddressUpdateVars[i] == addressVar)
        {
            plan->AddressUpdateDeltas[i] = delta;
            return;
        }
    }

    if (plan->AddressUpdateCount < LoopVectorizationPlan::MaxAddressUpdates)
    {
        const unsigned index             = plan->AddressUpdateCount++;
        plan->AddressUpdateVars[index]   = addressVar;
        plan->AddressUpdateDeltas[index] = delta;
    }
}

bool AutoVectorizer::HasAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar)
{
    for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
    {
        if (plan->AddressUpdateVars[i] == addressVar)
        {
            return plan->AddressUpdateDeltas[i] == static_cast<int>(plan->ElementSize);
        }
    }

    return false;
}

bool AutoVectorizer::ContainsOper(GenTree* tree, genTreeOps oper)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, genTreeOps oper)
            : GenTreeVisitor<Visitor>(compiler)
            , m_oper(oper)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if ((*use)->OperIs(m_oper))
            {
                m_found = true;
                return fgWalkResult::WALK_ABORT;
            }

            return fgWalkResult::WALK_CONTINUE;
        }

        bool Found() const
        {
            return m_found;
        }

    private:
        genTreeOps m_oper;
        bool       m_found = false;
    };

    Visitor visitor(m_compiler, oper);
    visitor.WalkTree(&tree, nullptr);
    return visitor.Found();
}

PhaseStatus Compiler::optAutoVectorizeAnalyze()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunAnalyze();
}

PhaseStatus Compiler::optAutoVectorizeRewrite()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunRewrite();
}
