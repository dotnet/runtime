// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "autovectorizer.h"

AutoVectorizer::AutoVectorizer(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus AutoVectorizer::Run()
{
    if (!IsEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!IsSupportedCompilation())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool                     changed = false;
    jitstd::vector<unsigned> rewrittenHeaders(m_compiler->getAllocator(CMK_LoopOpt));

    while (true)
    {
        if (!RecomputeLoopTable())
        {
            break;
        }

        bool rewrote = false;
        for (FlowGraphNaturalLoop* const loop : m_compiler->m_loops->InPostOrder())
        {
            BasicBlock* const header           = loop->GetHeader();
            bool              alreadyRewritten = false;
            for (unsigned rewrittenHeader : rewrittenHeaders)
            {
                if (rewrittenHeader == header->bbNum)
                {
                    alreadyRewritten = true;
                    break;
                }
            }

            if (alreadyRewritten)
            {
                JITDUMP("loop " FMT_LP " header " FMT_BB " already has a vector epilogue, skip\n", loop->GetIndex(),
                        header->bbNum);
                continue;
            }

            LoopVectorizationPlan plan;
            if (TryCreateLoopPlan(loop, &plan) && TryRewritePlan(&plan))
            {
                rewrittenHeaders.push_back(header->bbNum);
                changed = true;
                rewrote = true;
                break;
            }
        }

        if (!rewrote)
        {
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

bool AutoVectorizer::IsAggressiveVectorizing() const
{
    return JitConfig.JitAggressiveVectorizing() != 0;
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

bool AutoVectorizer::RecomputeLoopTable()
{
    m_compiler->fgInvalidateDfsTree();

    m_compiler->m_dfsTree = m_compiler->fgComputeDfs();
    m_compiler->m_loops   = FlowGraphNaturalLoops::Find(m_compiler->m_dfsTree);

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

bool AutoVectorizer::IsSupportedUnaryOp(genTreeOps oper, var_types elementType) const
{
    if (!IsSupportedElementType(elementType))
    {
        return false;
    }

    switch (oper)
    {
        case GT_NEG:
            return varTypeIsArithmetic(elementType);

        case GT_NOT:
            return varTypeIsIntegral(elementType);

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
        case GT_AND_NOT:
        case GT_OR:
        case GT_XOR:
            return varTypeIsIntegral(elementType);

        case GT_DIV:
            return varTypeIsFloating(elementType);

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
            return varTypeIsIntegral(elementType);

        default:
            return false;
    }
}

bool AutoVectorizer::IsSupportedCompareOp(genTreeOps oper, var_types elementType) const
{
    return IsSupportedElementType(elementType) && GenTree::OperIsCompare(oper);
}

bool AutoVectorizer::IsSupportedIntrinsic(NamedIntrinsic intrinsic, var_types elementType) const
{
    if (!IsSupportedElementType(elementType))
    {
        return false;
    }

    switch (intrinsic)
    {
        case NI_System_Math_Abs:
            return varTypeIsFloating(elementType);

        case NI_System_Math_Ceiling:
        case NI_System_Math_Floor:
        case NI_System_Math_Round:
        case NI_System_Math_Sqrt:
        case NI_System_Math_Truncate:
            return varTypeIsFloating(elementType);

        case NI_System_Math_Min:
        case NI_System_Math_Max:
        case NI_System_Math_MinNative:
        case NI_System_Math_MaxNative:
            return varTypeIsArithmetic(elementType);

        case NI_System_Math_MinMagnitude:
        case NI_System_Math_MaxMagnitude:
        case NI_System_Math_MinMagnitudeNumber:
        case NI_System_Math_MaxMagnitudeNumber:
        case NI_System_Math_MinNumber:
        case NI_System_Math_MaxNumber:
            return varTypeIsFloating(elementType);

        case NI_System_Math_MinUnsigned:
        case NI_System_Math_MaxUnsigned:
            return varTypeIsIntegral(elementType) && varTypeIsUnsigned(elementType);

        default:
            return false;
    }
}

bool AutoVectorizer::TrySelectVectorSizeAndBuildSLPPlan(LoopVectorizationPlan* plan)
{
#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned maxVectorSizeBytes = GetVectorSizeBytes(plan->ElementType);
    if (maxVectorSizeBytes == 0)
    {
        JITDUMP("no supported vector size for %s, bail out\n", varTypeName(plan->ElementType));
        return false;
    }

    unsigned vectorSizes[3]  = {};
    unsigned vectorSizeCount = 0;

#if defined(TARGET_XARCH)
    if (maxVectorSizeBytes >= ZMM_REGSIZE_BYTES)
    {
        vectorSizes[vectorSizeCount++] = ZMM_REGSIZE_BYTES;
    }

    if (maxVectorSizeBytes >= YMM_REGSIZE_BYTES)
    {
        vectorSizes[vectorSizeCount++] = YMM_REGSIZE_BYTES;
    }

    vectorSizes[vectorSizeCount++] = XMM_REGSIZE_BYTES;
#elif defined(TARGET_ARM64)
    vectorSizes[vectorSizeCount++] = FP_REGSIZE_BYTES;
#endif

    const LoopVectorizationPlan originalPlan = *plan;

    for (unsigned i = 0; i < vectorSizeCount; i++)
    {
        *plan = originalPlan;

        plan->VectorSizeBytes     = vectorSizes[i];
        plan->VectorizationFactor = plan->VectorSizeBytes / plan->ElementSize;

        if (plan->VectorizationFactor < 2)
        {
            JITDUMP("vector width policy candidate: size=%u, elementSize=%u, VF=%u\n", plan->VectorSizeBytes,
                    plan->ElementSize, plan->VectorizationFactor);
            JITDUMP("vectorization factor is too small, trying next width\n");
            continue;
        }

        if (!TryBuildSLPPlan(plan))
        {
            JITDUMP("vector width policy candidate %u did not build a legal SLP plan\n", plan->VectorSizeBytes);
            continue;
        }

        if (IsAggressiveVectorizing())
        {
            JITDUMP("JitAggressiveVectorizing selected %u-byte vectors, VF=%u\n", plan->VectorSizeBytes,
                    plan->VectorizationFactor);
            return true;
        }

        if (IsProfitableVectorSize(plan, maxVectorSizeBytes))
        {
            JITDUMP("vector width policy selected %u-byte vectors, VF=%u\n", plan->VectorSizeBytes,
                    plan->VectorizationFactor);
            return true;
        }

        JITDUMP("vector width policy rejected %u-byte vectors, trying next width\n", plan->VectorSizeBytes);
    }

    *plan = originalPlan;
    JITDUMP("vector width policy found no profitable vector size, bail out\n");
    return false;
#else
    return false;
#endif
}

bool AutoVectorizer::IsProfitableVectorSize(const LoopVectorizationPlan* plan, unsigned maxVectorSizeBytes) const
{
    unsigned   constantTripCount    = 0;
    const bool hasConstantTripCount = TryGetConstantTripCount(plan, &constantTripCount);
    if (hasConstantTripCount)
    {
        JITDUMP("vector width policy: constant trip count=%u\n", constantTripCount);
        if (constantTripCount < plan->VectorizationFactor)
        {
            JITDUMP("trip count is smaller than VF=%u\n", plan->VectorizationFactor);
            return false;
        }

        if ((plan->VectorSizeBytes > 16) && (constantTripCount < (2 * plan->VectorizationFactor)))
        {
            JITDUMP("tiny constant trip count favors a narrower vector width\n");
            return false;
        }
    }

    const SLPPlan& slpPlan        = plan->BodyPlan;
    const unsigned vectorPressure = EstimateVectorPressure(plan);
    const unsigned loopOverhead =
        6 + (3 * slpPlan.RootCount) + (5 * plan->ReductionCount) + (plan->NeedsOverlapCheck ? 8 : 0);
    const unsigned benefit = (slpPlan.EstimatedScalarCost > slpPlan.EstimatedVectorCost)
                                 ? (slpPlan.EstimatedScalarCost - slpPlan.EstimatedVectorCost)
                                 : 0;

    const bool isCold = plan->Header->isRunRarely() || plan->Header->isBBWeightCold(m_compiler);
    const bool isHot  = plan->Header->getBBWeight(m_compiler) >= (BB_UNITY_WEIGHT * BB_LOOP_WEIGHT_SCALE);

    JITDUMP("vector width policy: size=%u, VF=%u, max=%u, benefit=%u, overhead=%u, pressure=%u, hot=%s, cold=%s\n",
            plan->VectorSizeBytes, plan->VectorizationFactor, maxVectorSizeBytes, benefit, loopOverhead, vectorPressure,
            dspBool(isHot), dspBool(isCold));

    if (isCold)
    {
        JITDUMP("cold loop, bail out\n");
        return false;
    }

    if (benefit < (loopOverhead / 2))
    {
        JITDUMP("estimated benefit is too small for vector-loop overhead\n");
        return false;
    }

#if defined(TARGET_XARCH)
    if (plan->VectorSizeBytes == ZMM_REGSIZE_BYTES)
    {
        if (plan->NeedsOverlapCheck)
        {
            JITDUMP("runtime overlap checks favor a narrower vector width\n");
            return false;
        }

        if ((plan->ReductionCount != 0) && !isHot)
        {
            JITDUMP("512-bit reduction without hotness proof favors a narrower vector width\n");
            return false;
        }

        if ((vectorPressure > 12) || ((slpPlan.NodeCount > 24) && !isHot))
        {
            JITDUMP("512-bit candidate has too much estimated pressure\n");
            return false;
        }
    }

    if ((plan->VectorSizeBytes == YMM_REGSIZE_BYTES) && (vectorPressure > 18) && !isHot)
    {
        JITDUMP("256-bit candidate has too much estimated pressure for an unproven-hot loop\n");
        return false;
    }
#endif

    return true;
}

bool AutoVectorizer::TryGetConstantTripCount(const LoopVectorizationPlan* plan, unsigned* tripCount) const
{
    if (plan->IsPostIV || !plan->HasConstInit || (plan->End == nullptr) || !plan->End->IsCnsIntOrI())
    {
        return false;
    }

    const ssize_t limit = plan->End->AsIntConCommon()->IconValue();
    ssize_t       trip;
    if (plan->Step < 0)
    {
        trip = static_cast<ssize_t>(plan->ConstInitValue) - limit;
    }
    else
    {
        trip = limit - static_cast<ssize_t>(plan->ConstInitValue);
    }

    if ((plan->TestOper == GT_LE) || (plan->TestOper == GT_GE))
    {
        trip++;
    }

    if (trip <= 0)
    {
        *tripCount = 0;
        return true;
    }

    if (trip > UINT_MAX)
    {
        return false;
    }

    *tripCount = static_cast<unsigned>(trip);
    return true;
}

unsigned AutoVectorizer::EstimateVectorPressure(const LoopVectorizationPlan* plan) const
{
    const SLPPlan& slpPlan  = plan->BodyPlan;
    unsigned       pressure = plan->StoreCount + plan->LoadCount + slpPlan.RootCount;

    for (unsigned i = 0; i < slpPlan.NodeCount; i++)
    {
        const PackNode& node = slpPlan.Nodes[i];
        if ((node.Kind == PackKind::SplatConstant) || (node.Kind == PackKind::SplatScalar) ||
            (node.Kind == PackKind::BinaryOp) || (node.Kind == PackKind::TernaryOp) ||
            (node.Kind == PackKind::CompareOp) || (node.Kind == PackKind::Select))
        {
            pressure++;
        }
    }

    if (plan->ReductionCount != 0)
    {
        pressure += 2 * plan->ReductionCount;
    }

    return pressure;
}

bool AutoVectorizer::TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    JITDUMP("considering loop " FMT_LP "\n", loop->GetIndex());

    if (loop->EntryEdges().size() != 1)
    {
        JITDUMP("loop " FMT_LP " entry edge count=%zu\n", loop->GetIndex(), loop->EntryEdges().size());
        JITDUMP("loop does not have exactly one entry, bail out\n");
        return false;
    }

    if (loop->BackEdges().size() != 1)
    {
        JITDUMP("loop " FMT_LP " backedge count=%zu\n", loop->GetIndex(), loop->BackEdges().size());
        JITDUMP("loop does not have exactly one backedge, bail out\n");
        return false;
    }

    if (loop->ExitEdges().size() != 1)
    {
        JITDUMP("loop " FMT_LP " exit edge count=%zu\n", loop->GetIndex(), loop->ExitEdges().size());
        JITDUMP("loop does not have exactly one normal exit, bail out\n");
        return false;
    }

    if (loop->GetChild() != nullptr)
    {
        JITDUMP("loop " FMT_LP " has child loop " FMT_LP "\n", loop->GetIndex(), loop->GetChild()->GetIndex());
        JITDUMP("non-innermost loop, bail out\n");
        return false;
    }

    BasicBlock* const preheader = loop->EntryEdge(0)->getSourceBlock();
    BasicBlock* const latch     = loop->BackEdge(0)->getSourceBlock();
    BasicBlock* const exit      = loop->ExitEdge(0)->getDestinationBlock();

    if ((preheader == nullptr) || !preheader->KindIs(BBJ_ALWAYS) || !preheader->TargetIs(loop->GetHeader()))
    {
        JITDUMP("loop " FMT_LP " preheader candidate is " FMT_BB "\n", loop->GetIndex(),
                preheader == nullptr ? 0 : preheader->bbNum);
        JITDUMP("missing canonical preheader, bail out\n");
        return false;
    }

    if (preheader->hasTryIndex() || preheader->hasHndIndex() || exit->hasTryIndex() || exit->hasHndIndex())
    {
        JITDUMP("loop " FMT_LP " preheader try=%u hnd=%u, exit try=%u hnd=%u\n", loop->GetIndex(),
                preheader->getTryIndex(), preheader->getHndIndex(), exit->getTryIndex(), exit->getHndIndex());
        JITDUMP("EH region near loop, bail out\n");
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
        JITDUMP("loop " FMT_LP " has a block inside an EH region\n", loop->GetIndex());
        JITDUMP("EH region in loop, bail out\n");
        return false;
    }

    LoopVectorizationPlan postIVPlan;
    if (TryCreatePostIVLoopPlan(loop, &postIVPlan))
    {
        *plan = postIVPlan;
        return true;
    }

    LoopVectorizationPlan localLimitPlan;
    if (TryCreateLocalLimitLoopPlan(loop, &localLimitPlan))
    {
        *plan = localLimitPlan;
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

    int step = iterInfo.IterConst();
    if (iterInfo.IterOper() == GT_SUB)
    {
        step = -step;
    }

    const genTreeOps testOper = iterInfo.TestOper();

    const bool isSupportedNotEqualLoop =
        (testOper == GT_NE) && iterInfo.HasConstInit && (step == 1) &&
        (iterInfo.HasArrayLengthLimit || iterInfo.HasInvariantLocalLimit ||
         (iterInfo.HasConstLimit && (iterInfo.ConstLimit() > iterInfo.ConstInitValue)));
    const bool isSupportedIncreasingLoop = iterInfo.IsIncreasingLoop() || isSupportedNotEqualLoop;
    const bool isSupportedDecreasingLoop = iterInfo.IsDecreasingLoop();

    if ((abs(step) != 1) || (!isSupportedIncreasingLoop && !isSupportedDecreasingLoop))
    {
        JITDUMP("IV update:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(iterInfo.IterTree));
        JITDUMP("canonical loop does not have contiguous unit-stride memory, bail out\n");
        return false;
    }

    if (!GenTree::StaticOperIs(testOper, GT_LT, GT_LE, GT_GT, GT_GE) && !isSupportedNotEqualLoop)
    {
        JITDUMP("test:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(iterInfo.TestTree));
        JITDUMP("unsupported loop test, bail out\n");
        return false;
    }

    if (!iterInfo.HasConstLimit && !iterInfo.HasArrayLengthLimit && !iterInfo.HasInvariantLocalLimit)
    {
        JITDUMP("limit:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(iterInfo.Limit()));
        JITDUMP("limit is not constant, array length, or invariant local, bail out\n");
        return false;
    }

    plan->Loop           = loop;
    plan->Preheader      = preheader;
    plan->Header         = loop->GetHeader();
    plan->Latch          = latch;
    plan->Exit           = exit;
    plan->InductionVar   = iterInfo.IterVar;
    plan->End            = iterInfo.Limit();
    plan->IterTree       = iterInfo.IterTree;
    plan->TestTree       = iterInfo.TestTree;
    plan->TestBlock      = iterInfo.TestBlock;
    plan->TestOper       = testOper;
    plan->Step           = isSupportedDecreasingLoop ? -1 : 1;
    plan->HasConstInit   = iterInfo.HasConstInit;
    plan->ConstInitValue = iterInfo.HasConstInit ? static_cast<int>(iterInfo.ConstInitValue) : 0;
    plan->ElementType    = TYP_UNDEF;
    plan->ElementSize    = 0;

    if (iterInfo.HasConstInit)
    {
        JITDUMP("loop " FMT_LP " canonical IV V%02u, init=%d, step=%d, test=%s\n", loop->GetIndex(), iterInfo.IterVar,
                iterInfo.ConstInitValue, plan->Step, GenTree::OpName(testOper));
    }
    else
    {
        JITDUMP("loop " FMT_LP " canonical IV V%02u, init=<unknown>, step=%d, test=%s\n", loop->GetIndex(),
                iterInfo.IterVar, plan->Step, GenTree::OpName(testOper));
    }

    if (!TryAnalyzeMemory(plan))
    {
        return false;
    }

    if (!TrySelectVectorSizeAndBuildSLPPlan(plan))
    {
        return false;
    }

    JITDUMP("accepted loop " FMT_LP " as canonical candidate\n", plan->Loop->GetIndex());
    JITDUMP("  preheader=" FMT_BB ", header=" FMT_BB ", latch=" FMT_BB ", exit=" FMT_BB "\n", plan->Preheader->bbNum,
            plan->Header->bbNum, plan->Latch->bbNum, plan->Exit->bbNum);
    JITDUMP("  iv=V%02u, step=%d, test=%s, element=%s, vectorSize=%u, VF=%u, stores=%u, loads=%u\n", plan->InductionVar,
            plan->Step, GenTree::OpName(plan->TestOper), varTypeName(plan->ElementType), plan->VectorSizeBytes,
            plan->VectorizationFactor, plan->StoreCount, plan->LoadCount);
    JITDUMP("loop test:\n");
    JITDUMPEXEC(m_compiler->gtDispTree(plan->TestTree));
    DumpSLPPlan(*plan);
    return true;
}

bool AutoVectorizer::TryCreatePostIVLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    BasicBlock* const header = loop->GetHeader();
    if (header != loop->BackEdge(0)->getSourceBlock())
    {
        JITDUMP("post-IV candidate header=" FMT_BB ", backedge source=" FMT_BB "\n", header->bbNum,
                loop->BackEdge(0)->getSourceBlock()->bbNum);
        JITDUMP("post-IV form is not a single-block loop, bail out\n");
        return false;
    }

    if (!header->KindIs(BBJ_COND))
    {
        JITDUMP("post-IV candidate header " FMT_BB " jump kind=%s\n", header->bbNum, bbKindNames[header->GetKind()]);
        JITDUMP("post-IV loop header is not conditional, bail out\n");
        return false;
    }

    Statement* const testStmt = header->lastStmt();
    if ((testStmt == nullptr) || !testStmt->GetRootNode()->OperIs(GT_JTRUE))
    {
        if (testStmt != nullptr)
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(testStmt));
        }
        else
        {
            JITDUMP("post-IV candidate has no last statement\n");
        }

        JITDUMP("post-IV loop has no bottom test, bail out\n");
        return false;
    }

    GenTree* const relop = testStmt->GetRootNode()->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        JITDUMP("post-IV loop test:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(relop));
        JITDUMP("post-IV loop test is not a compare, bail out\n");
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
        JITDUMP("post-IV loop test:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(relop));
        JITDUMP("post-IV loop test does not compare local to zero, bail out\n");
        return false;
    }

    genTreeOps testOper = relop->OperGet();
    if (reversed)
    {
        testOper = GenTree::SwapRelop(testOper);
    }

    if (!GenTree::StaticOperIs(testOper, GT_NE, GT_GT))
    {
        JITDUMP("post-IV loop test:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(relop));
        JITDUMP("post-IV normalized test %s is not a positive remaining-count test, bail out\n",
                GenTree::OpName(testOper));
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
    JITDUMP("post-IV loop test:\n");
    JITDUMPEXEC(m_compiler->gtDispTree(relop));

    if (!TryAnalyzePostIVMemory(plan) || !TrySelectVectorSizeAndBuildSLPPlan(plan))
    {
        return false;
    }

    JITDUMP("accepted loop " FMT_LP " as post-IV candidate\n", plan->Loop->GetIndex());
    JITDUMP("  preheader=" FMT_BB ", header=" FMT_BB ", latch=" FMT_BB ", exit=" FMT_BB "\n", plan->Preheader->bbNum,
            plan->Header->bbNum, plan->Latch->bbNum, plan->Exit->bbNum);
    JITDUMP("  tripCount=V%02u, test=%s, element=%s, vectorSize=%u, VF=%u, stores=%u, loads=%u\n", plan->TripCountVar,
            GenTree::OpName(plan->TestOper), varTypeName(plan->ElementType), plan->VectorSizeBytes,
            plan->VectorizationFactor, plan->StoreCount, plan->LoadCount);
    JITDUMP("loop test:\n");
    JITDUMPEXEC(m_compiler->gtDispTree(plan->TestTree));
    DumpSLPPlan(*plan);
    return true;
}

bool AutoVectorizer::TryCreateLocalLimitLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    BasicBlock* const header = loop->GetHeader();
    if (header != loop->BackEdge(0)->getSourceBlock())
    {
        return false;
    }

    if (!header->KindIs(BBJ_COND))
    {
        return false;
    }

    Statement* const testStmt = header->lastStmt();
    if ((testStmt == nullptr) || !testStmt->GetRootNode()->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const relop = testStmt->GetRootNode()->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        return false;
    }

    GenTree*   op1      = UnwrapCommaValue(relop->AsOp()->gtOp1);
    GenTree*   op2      = UnwrapCommaValue(relop->AsOp()->gtOp2);
    genTreeOps testOper = relop->OperGet();

    unsigned ivLcl = BAD_VAR_NUM;
    GenTree* limit = nullptr;
    if (op1->OperIs(GT_LCL_VAR) && op2->OperIs(GT_LCL_VAR) && GenTree::StaticOperIs(testOper, GT_LT, GT_LE))
    {
        ivLcl = op1->AsLclVarCommon()->GetLclNum();
        limit = op2;
    }
    else if (op1->OperIs(GT_LCL_VAR) && op2->OperIs(GT_LCL_VAR) && GenTree::StaticOperIs(testOper, GT_GT, GT_GE))
    {
        ivLcl    = op2->AsLclVarCommon()->GetLclNum();
        limit    = op1;
        testOper = (testOper == GT_GT) ? GT_LT : GT_LE;
    }
    else if (op1->OperIs(GT_LCL_VAR) && op2->OperIs(GT_LCL_VAR) && (testOper == GT_NE))
    {
        ivLcl = op1->AsLclVarCommon()->GetLclNum();
        limit = op2;
    }
    else
    {
        return false;
    }

    plan->Loop         = loop;
    plan->Preheader    = loop->EntryEdge(0)->getSourceBlock();
    plan->Header       = header;
    plan->Latch        = header;
    plan->Exit         = loop->ExitEdge(0)->getDestinationBlock();
    plan->InductionVar = ivLcl;
    plan->End          = limit;
    plan->TestTree     = relop;
    plan->TestBlock    = header;
    plan->TestOper     = testOper;
    plan->Step         = 1;

    for (Statement* const stmt : header->Statements())
    {
        RecordLocalDefs(plan, stmt->GetRootNode());
    }

    for (BasicBlock* block = plan->Preheader; block != nullptr; block = block->GetUniquePred(m_compiler))
    {
        for (Statement* const stmt : block->Statements())
        {
            RecordLocalDefs(plan, stmt->GetRootNode());
        }
    }

    GenTree* initDef = nullptr;
    if (TryGetLocalDef(plan, ivLcl, &initDef) && initDef->IsCnsIntOrI())
    {
        const ssize_t initValue = initDef->AsIntConCommon()->IconValue();
        if ((initValue >= INT_MIN) && (initValue <= INT_MAX))
        {
            plan->HasConstInit   = true;
            plan->ConstInitValue = static_cast<int>(initValue);
        }
    }

    Statement* iterStmt = nullptr;
    for (Statement* const stmt : header->Statements())
    {
        GenTree* const root = stmt->GetRootNode();
        if (!root->OperIs(GT_STORE_LCL_VAR) || (root->AsLclVarCommon()->GetLclNum() != ivLcl))
        {
            continue;
        }

        int offset = 0;
        if (!TryAnalyzeIndexExpr(plan, root->AsLclVarCommon()->Data(), ivLcl, &offset) || (offset != 1))
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
            JITDUMP("local-limit IV update is not +1, bail out\n");
            return false;
        }

        iterStmt = stmt;
        break;
    }

    if (iterStmt == nullptr)
    {
        JITDUMPEXEC(m_compiler->gtDispTree(relop));
        JITDUMP("local-limit loop has no IV update, bail out\n");
        return false;
    }

    plan->IterTree = iterStmt->GetRootNode();

    JITDUMP("loop " FMT_LP " local-limit IV V%02u, step=1, test=%s\n", loop->GetIndex(), ivLcl,
            GenTree::OpName(testOper));
    JITDUMP("loop test:\n");
    JITDUMPEXEC(m_compiler->gtDispTree(relop));

    if (!TryAnalyzeMemory(plan))
    {
        return false;
    }

    if (!TrySelectVectorSizeAndBuildSLPPlan(plan))
    {
        return false;
    }

    JITDUMP("accepted loop " FMT_LP " as local-limit candidate\n", plan->Loop->GetIndex());
    JITDUMP("  preheader=" FMT_BB ", header=" FMT_BB ", latch=" FMT_BB ", exit=" FMT_BB "\n", plan->Preheader->bbNum,
            plan->Header->bbNum, plan->Latch->bbNum, plan->Exit->bbNum);
    JITDUMP("  iv=V%02u, step=%d, test=%s, element=%s, vectorSize=%u, VF=%u, stores=%u, loads=%u\n", plan->InductionVar,
            plan->Step, GenTree::OpName(plan->TestOper), varTypeName(plan->ElementType), plan->VectorSizeBytes,
            plan->VectorizationFactor, plan->StoreCount, plan->LoadCount);
    DumpSLPPlan(*plan);
    return true;
}

bool AutoVectorizer::AddStore(LoopVectorizationPlan*                     plan,
                              Statement*                                 stmt,
                              GenTree*                                   value,
                              const LoopVectorizationPlan::ScalarAccess& access)
{
    if (plan->StoreCount >= LoopVectorizationPlan::MaxStores)
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("store count exceeds budget %u, bail out\n", LoopVectorizationPlan::MaxStores);
        return false;
    }

    if (!IsSupportedElementType(access.ElementType))
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("store has unsupported element type %s, bail out\n", varTypeName(access.ElementType));
        return false;
    }

    if ((plan->ElementType != TYP_UNDEF) && (plan->ElementType != access.ElementType))
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("store element type %s does not match prior element type %s, bail out\n",
                varTypeName(access.ElementType), varTypeName(plan->ElementType));
        return false;
    }

    const unsigned index       = plan->StoreCount++;
    plan->StoreStmts[index]    = stmt;
    plan->StoreValues[index]   = value;
    plan->StoreAccesses[index] = access;

    if (plan->StoreStmt == nullptr)
    {
        plan->StoreStmt   = stmt;
        plan->StoreAccess = access;
    }

    plan->ElementType = access.ElementType;
    plan->ElementSize = access.ElementSize;
    return true;
}

bool AutoVectorizer::TryAddReduction(LoopVectorizationPlan* plan, Statement* stmt, GenTreeLclVarCommon* storeLcl)
{
    if (plan->ReductionCount >= LoopVectorizationPlan::MaxReductions)
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("reduction count exceeds budget %u, bail out\n", LoopVectorizationPlan::MaxReductions);
        return false;
    }

    const unsigned reductionLcl = storeLcl->GetLclNum();
    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        if (plan->Reductions[i].Lcl == reductionLcl)
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
            JITDUMP("multiple updates to reduction V%02u, bail out\n", reductionLcl);
            return false;
        }
    }

    GenTree* const data         = UnwrapCommaValue(storeLcl->Data());
    genTreeOps     reductionOp  = data->OperGet();
    NamedIntrinsic reductionNI  = NI_Illegal;
    GenTree*       op1          = nullptr;
    GenTree*       op2          = nullptr;

    if (data->OperIs(GT_ADD, GT_SUB))
    {
        op1 = UnwrapCommaValue(data->AsOp()->gtOp1);
        op2 = UnwrapCommaValue(data->AsOp()->gtOp2);
        if (!op1->OperIs(GT_LCL_VAR) || (op1->AsLclVarCommon()->GetLclNum() != reductionLcl))
        {
            if (data->OperIs(GT_SUB))
            {
                return false;
            }

            std::swap(op1, op2);
        }

        if (!op1->OperIs(GT_LCL_VAR) || (op1->AsLclVarCommon()->GetLclNum() != reductionLcl))
        {
            return false;
        }
    }
    else if (data->OperIs(GT_INTRINSIC) && IsReductionMinMaxIntrinsic(data->AsIntrinsic()->gtIntrinsicName))
    {
        GenTreeIntrinsic* const intrinsic = data->AsIntrinsic();
        op1                               = UnwrapCommaValue(intrinsic->gtGetOp1());
        op2                               = UnwrapCommaValue(intrinsic->gtGetOp2());
        if (!op1->OperIs(GT_LCL_VAR) || (op1->AsLclVarCommon()->GetLclNum() != reductionLcl))
        {
            std::swap(op1, op2);
        }

        if (!op1->OperIs(GT_LCL_VAR) || (op1->AsLclVarCommon()->GetLclNum() != reductionLcl))
        {
            return false;
        }

        reductionNI = intrinsic->gtIntrinsicName;
        reductionOp = GT_INTRINSIC;
    }
    else
    {
        return false;
    }

    const var_types elementType = genActualType(storeLcl->TypeGet());
    if (!varTypeIsArithmetic(elementType) || !IsSupportedElementType(elementType))
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("reduction type %s is not supported, bail out\n", varTypeName(elementType));
        return false;
    }

    if ((plan->ElementType != TYP_UNDEF) && (plan->ElementType != elementType))
    {
        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("reduction element type %s does not match loop element type %s, bail out\n", varTypeName(elementType),
                varTypeName(plan->ElementType));
        return false;
    }

    LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[plan->ReductionCount++];
    reduction.Stmt                                  = stmt;
    reduction.Lcl                                   = reductionLcl;
    reduction.Oper                                  = reductionOp;
    reduction.Intrinsic                             = reductionNI;
    reduction.Value                                 = op2;
    plan->ElementType                               = elementType;
    plan->ElementSize                               = genTypeSize(elementType);
    return true;
}

bool AutoVectorizer::AddLoad(LoopVectorizationPlan*                     plan,
                             const LoopVectorizationPlan::ScalarAccess& access,
                             unsigned*                                  index)
{
    for (unsigned i = 0; i < plan->LoadCount; i++)
    {
        const LoopVectorizationPlan::ScalarAccess& existing = plan->LoadAccesses[i];
        if ((existing.Address == access.Address) ||
            ((existing.BaseLocalIfKnown == access.BaseLocalIfKnown) &&
             (existing.OffsetLocalIfKnown == access.OffsetLocalIfKnown) &&
             (existing.IndexOffset == access.IndexOffset) && (existing.ElementType == access.ElementType) &&
             (existing.IsArray == access.IsArray) && (existing.IsByrefLocal == access.IsByrefLocal) &&
             (existing.IsByrefBaseWithOffset == access.IsByrefBaseWithOffset) &&
             (existing.IsByrefWithIndex == access.IsByrefWithIndex)))
        {
            *index = i;
            return true;
        }
    }

    if (plan->LoadCount >= LoopVectorizationPlan::MaxAccesses)
    {
        JITDUMP("load count exceeds budget %u, bail out\n", LoopVectorizationPlan::MaxAccesses);
        return false;
    }

    if ((plan->ElementType != TYP_UNDEF) && (plan->ElementType != access.ElementType))
    {
        JITDUMP("load element type %s does not match loop element type %s, bail out\n", varTypeName(access.ElementType),
                varTypeName(plan->ElementType));
        return false;
    }

    *index                     = plan->LoadCount++;
    plan->LoadAccesses[*index] = access;
    plan->LoadAccess           = plan->LoadAccesses[0];
    plan->MinIndexOffset       = std::min(plan->MinIndexOffset, access.IndexOffset);
    plan->MaxIndexOffset       = std::max(plan->MaxIndexOffset, access.IndexOffset);
    return true;
}

bool AutoVectorizer::MayAlias(const LoopVectorizationPlan::ScalarAccess& first,
                              const LoopVectorizationPlan::ScalarAccess& second) const
{
    if (first.IsArray && second.IsArray)
    {
        return true;
    }

    if ((first.IsByrefLocal || first.IsByrefBaseWithOffset || first.IsByrefWithIndex) &&
        (second.IsByrefLocal || second.IsByrefBaseWithOffset || second.IsByrefWithIndex))
    {
        return true;
    }

    // Array and byref/span bases can still describe the same storage after morphing.
    return true;
}

bool AutoVectorizer::ValidateMemoryDependences(LoopVectorizationPlan* plan)
{
    for (unsigned storeIndex = 0; storeIndex < plan->StoreCount; storeIndex++)
    {
        const LoopVectorizationPlan::ScalarAccess& store = plan->StoreAccesses[storeIndex];

        plan->MinIndexOffset = std::min(plan->MinIndexOffset, store.IndexOffset);
        plan->MaxIndexOffset = std::max(plan->MaxIndexOffset, store.IndexOffset);

        for (unsigned loadIndex = 0; loadIndex < plan->LoadCount; loadIndex++)
        {
            const LoopVectorizationPlan::ScalarAccess& load = plan->LoadAccesses[loadIndex];
            if (MayAlias(store, load) && (store.IndexOffset != load.IndexOffset))
            {
                if (!plan->IsPostIV && (plan->Step > 0) && (load.IndexOffset > store.IndexOffset))
                {
                    JITDUMP("store offset=%d, load offset=%d; forward load is not loop-carried\n", store.IndexOffset,
                            load.IndexOffset);
                    continue;
                }

                if (!plan->IsPostIV && (plan->Step < 0) && (load.IndexOffset < store.IndexOffset))
                {
                    JITDUMP("store offset=%d, load offset=%d; backward load is not loop-carried\n", store.IndexOffset,
                            load.IndexOffset);
                    continue;
                }

                if (plan->IsPostIV && (plan->StoreCount == 1) && (plan->LoadCount == 1))
                {
                    plan->NeedsOverlapCheck = true;
                    JITDUMP("store/load may overlap; will generate runtime overlap check\n");
                    continue;
                }

                JITDUMP("store offset=%d, load offset=%d\n", store.IndexOffset, load.IndexOffset);
                JITDUMPEXEC(m_compiler->gtDispStmt(store.StatementRoot));
                JITDUMPEXEC(m_compiler->gtDispTree(load.Address));
                JITDUMP("possible loop-carried dependence, bail out\n");
                return false;
            }
        }

        for (unsigned otherIndex = storeIndex + 1; otherIndex < plan->StoreCount; otherIndex++)
        {
            const LoopVectorizationPlan::ScalarAccess& other = plan->StoreAccesses[otherIndex];
            if (MayAlias(store, other) && (store.IndexOffset != other.IndexOffset))
            {
                JITDUMP("store offset=%d, other store offset=%d\n", store.IndexOffset, other.IndexOffset);
                JITDUMPEXEC(m_compiler->gtDispStmt(store.StatementRoot));
                JITDUMPEXEC(m_compiler->gtDispStmt(other.StatementRoot));
                JITDUMP("possibly overlapping stores, bail out\n");
                return false;
            }
        }
    }

    return true;
}

bool AutoVectorizer::TryAnalyzeMemory(LoopVectorizationPlan* plan)
{
    bool failed = false;

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
            GenTree* const root               = stmt->GetRootNode();
            bool           stmtHasBoundsCheck = false;
            RecordLocalDefs(plan, root, &stmtHasBoundsCheck);
            plan->SawBoundsCheck |= stmtHasBoundsCheck;

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
                const unsigned lclNum = root->AsLclVarCommon()->GetLclNum();
                if (((root->gtFlags & GTF_CALL) != 0) || !m_compiler->lvaGetDesc(lclNum)->lvIsTemp)
                {
                    if (((root->gtFlags & GTF_CALL) == 0) && TryAddReduction(plan, stmt, root->AsLclVarCommon()))
                    {
                        continue;
                    }

                    failed = true;
                    JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                    JITDUMP("unsupported local store in loop body, bail out\n");
                    return BasicBlockVisit::Abort;
                }

                continue;
            }

            if (!root->OperIs(GT_STOREIND))
            {
                failed = true;
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                JITDUMP("unsupported statement in loop body, bail out\n");
                return BasicBlockVisit::Abort;
            }

            if ((root->gtFlags & GTF_CALL) != 0)
            {
                failed = true;
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                JITDUMP("store statement has unsupported calls, bail out\n");
                return BasicBlockVisit::Abort;
            }

            GenTreeStoreInd* const              store = root->AsStoreInd();
            LoopVectorizationPlan::ScalarAccess storeAccess;
            if (!TryAnalyzeIndirAccess(plan, stmt, store, true, plan->InductionVar, &storeAccess))
            {
                failed = true;
                JITDUMP("store address:\n");
                JITDUMPEXEC(m_compiler->gtDispTree(store->Addr()));
                JITDUMP("unsupported store access, bail out\n");
                return BasicBlockVisit::Abort;
            }

            GenTree* const storeData = UnwrapCommaValue(store->Data());
            if (!AddStore(plan, stmt, storeData, storeAccess))
            {
                failed = true;
                return BasicBlockVisit::Abort;
            }
        }

        return BasicBlockVisit::Continue;
    });

    if (failed)
    {
        return false;
    }

    if ((plan->StoreCount == 0) && (plan->ReductionCount == 0))
    {
        JITDUMP("no vectorizable store or reduction, bail out\n");
        return false;
    }

    for (unsigned i = 0; i < plan->StoreCount; i++)
    {
        JITDUMP("canonical store %u, offset %d:\n", i, plan->StoreAccesses[i].IndexOffset);
        JITDUMPEXEC(m_compiler->gtDispStmt(plan->StoreStmts[i]));
        JITDUMP("store address:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(plan->StoreAccesses[i].Address));
    }

    return true;
}

bool AutoVectorizer::TryAnalyzePostIVMemory(LoopVectorizationPlan* plan)
{
    assert(plan->IsPostIV);

    BasicBlock* const block                = plan->Header;
    bool              foundTripCountUpdate = false;

    for (BasicBlock* pred = plan->Preheader; pred != nullptr; pred = pred->GetUniquePred(m_compiler))
    {
        for (Statement* const stmt : pred->Statements())
        {
            RecordLocalDefs(plan, stmt->GetRootNode());
        }
    }

    for (Statement* const stmt : block->Statements())
    {
        GenTree* const root = stmt->GetRootNode();
        RecordLocalDefs(plan, root);

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
            GenTreeStoreInd* const              store = root->AsStoreInd();
            LoopVectorizationPlan::ScalarAccess storeAccess;
            if (!TryAnalyzePostIVAddress(stmt, store->Addr(), &storeAccess))
            {
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                JITDUMP("post-IV store is not a local-addressed supported store, bail out\n");
                return false;
            }

            if (storeAccess.ElementType == TYP_UNDEF)
            {
                storeAccess.ElementType = store->TypeGet();
                storeAccess.ElementSize = genTypeSize(storeAccess.ElementType);
            }

            if (!IsSupportedElementType(storeAccess.ElementType))
            {
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                JITDUMP("post-IV store has unsupported element type, bail out\n");
                return false;
            }

            if (genActualType(store->TypeGet()) != genActualType(storeAccess.ElementType))
            {
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                JITDUMP("post-IV store type does not match memory element type, bail out\n");
                return false;
            }

            storeAccess.ElementSize  = genTypeSize(storeAccess.ElementType);
            storeAccess.IsStore      = true;
            GenTree* const storeData = UnwrapCommaValue(store->Data());
            if (!AddStore(plan, stmt, storeData, storeAccess))
            {
                return false;
            }

            JITDUMP("post-IV store:\n");
            JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
            JITDUMP("post-IV store address:\n");
            JITDUMPEXEC(m_compiler->gtDispTree(storeAccess.Address));
            continue;
        }

        if (!root->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
            JITDUMP("unsupported post-IV statement, bail out\n");
            return false;
        }

        GenTreeLclVarCommon* const storeLcl = root->AsLclVarCommon();
        GenTree* const             data     = storeLcl->Data();
        const unsigned             lclNum   = storeLcl->GetLclNum();

        if ((lclNum == plan->TripCountVar) && data->OperIs(GT_ADD, GT_SUB))
        {
            GenTree*   op1 = data->AsOp()->gtOp1;
            GenTree*   op2 = data->AsOp()->gtOp2;
            const bool decrementsByOne =
                (data->OperIs(GT_ADD) && op2->IsIntegralConst(-1)) || (data->OperIs(GT_SUB) && op2->IsIntegralConst(1));
            if (decrementsByOne && op1->OperIs(GT_LCL_VAR) &&
                (op1->AsLclVarCommon()->GetLclNum() == plan->TripCountVar))
            {
                foundTripCountUpdate = true;
                JITDUMP("post-IV trip-count update:\n");
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                continue;
            }
        }

        if (data->OperIs(GT_ADD, GT_SUB))
        {
            GenTree* op1 = data->AsOp()->gtOp1;
            GenTree* op2 = data->AsOp()->gtOp2;

            if (op2->IsCnsIntOrI() && op1->OperIs(GT_LCL_VAR) && (op1->AsLclVarCommon()->GetLclNum() == lclNum) &&
                storeLcl->TypeIs(TYP_BYREF, TYP_I_IMPL, TYP_LONG))
            {
                int delta = static_cast<int>(op2->AsIntConCommon()->IconValue());
                if (data->OperIs(GT_SUB))
                {
                    delta = -delta;
                }

                RecordAddressUpdate(plan, lclNum, delta);
                JITDUMP("post-IV address update V%02u by %d bytes:\n", lclNum, delta);
                JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
                continue;
            }
        }

        if (((root->gtFlags & GTF_CALL) == 0) && m_compiler->lvaGetDesc(lclNum)->lvIsTemp)
        {
            JITDUMP("post-IV computed value temp V%02u:\n", lclNum);
            JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
            continue;
        }

        if (((root->gtFlags & GTF_CALL) == 0) && TryAddReduction(plan, stmt, storeLcl))
        {
            continue;
        }

        JITDUMPEXEC(m_compiler->gtDispStmt(stmt));
        JITDUMP("unsupported post-IV statement, bail out\n");
        return false;
    }

    if (((plan->StoreCount == 0) && (plan->ReductionCount == 0)) || !foundTripCountUpdate)
    {
        JITDUMP("storeCount=%u, reductions=%u, foundTripCountUpdate=%s\n", plan->StoreCount, plan->ReductionCount,
                dspBool(foundTripCountUpdate));
        JITDUMP("incomplete post-IV memory pattern, bail out\n");
        return false;
    }

    if ((plan->ElementType == TYP_UNDEF) || !IsSupportedElementType(plan->ElementType))
    {
        JITDUMP("post-IV element type=%s\n", varTypeName(plan->ElementType));
        JITDUMP("post-IV unsupported element type, bail out\n");
        return false;
    }

    bool foundTripCountInit = false;
    for (Statement* const stmt : plan->Preheader->Statements())
    {
        GenTree* const root = stmt->GetRootNode();
        if (!root->OperIs(GT_STORE_LCL_VAR))
        {
            continue;
        }

        const unsigned lclNum = root->AsLclVarCommon()->GetLclNum();
        foundTripCountInit |= (lclNum == plan->TripCountVar);
    }

    if (!foundTripCountInit)
    {
        JITDUMP("foundTripCountInit=%s\n", dspBool(foundTripCountInit));
        JITDUMP("missing post-IV preheader initialization, bail out\n");
        return false;
    }

    JITDUMP("found post-IV %s loop with %u store(s), trip count V%02u\n", varTypeName(plan->ElementType),
            plan->StoreCount, plan->TripCountVar);
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

AutoVectorizer::PackNode* AutoVectorizer::TryBuildComparePack(
    LoopVectorizationPlan* plan, Statement* stmt, GenTree* value, var_types elementType, unsigned depth)
{
    if (depth > MaxPackDepth)
    {
        JITDUMP("pack recursion depth exceeded %u, bail out\n", MaxPackDepth);
        return nullptr;
    }

    value = UnwrapCommaValue(value);
    if (!value->OperIsCompare() || !IsSupportedCompareOp(value->OperGet(), elementType))
    {
        JITDUMPEXEC(m_compiler->gtDispTree(value));
        JITDUMP("condition is not a supported SIMD comparison, bail out\n");
        return nullptr;
    }

    PackNode* const op1 = TryBuildPack(plan, stmt, value->AsOp()->gtOp1, elementType, depth + 1);
    if (op1 == nullptr)
    {
        return nullptr;
    }

    PackNode* const op2 = TryBuildPack(plan, stmt, value->AsOp()->gtOp2, elementType, depth + 1);
    if (op2 == nullptr)
    {
        return nullptr;
    }

    PackNode* const cmp = NewPackNode(&plan->BodyPlan, PackKind::CompareOp, elementType, plan->VectorizationFactor);
    if (cmp == nullptr)
    {
        JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
        JITDUMP("SLP node budget exceeded, bail out\n");
        return nullptr;
    }

    cmp->Oper        = value->OperGet();
    cmp->Operands[0] = op1;
    cmp->Operands[1] = op2;
    cmp->Cost        = op1->Cost + op2->Cost + 1;
    return cmp;
}

bool AutoVectorizer::TryGetScalarFromCreateScalar(LoopVectorizationPlan* plan,
                                                  GenTree*               tree,
                                                  GenTree**              scalar,
                                                  unsigned               depth)
{
    if (depth > MaxPackDepth)
    {
        return false;
    }

    tree = UnwrapCommaValue(tree);
    if (tree->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (TryGetLocalDef(plan, tree->AsLclVarCommon()->GetLclNum(), &def))
        {
            return TryGetScalarFromCreateScalar(plan, def, scalar, depth + 1);
        }
    }

    if (!tree->OperIs(GT_HWINTRINSIC))
    {
        return false;
    }

    GenTreeHWIntrinsic* const create = tree->AsHWIntrinsic();
    const NamedIntrinsic      id     = create->GetHWIntrinsicId();
    if ((create->GetOperandCount() == 1) &&
        ((id == NI_Vector128_CreateScalar) || (id == NI_Vector128_CreateScalarUnsafe)
#if defined(TARGET_XARCH)
         || (id == NI_Vector256_CreateScalar) || (id == NI_Vector256_CreateScalarUnsafe) ||
         (id == NI_Vector512_CreateScalar) || (id == NI_Vector512_CreateScalarUnsafe)
#elif defined(TARGET_ARM64)
         || (id == NI_Vector64_CreateScalar) || (id == NI_Vector64_CreateScalarUnsafe)
#endif
             ))
    {
        *scalar = create->Op(1);
        return true;
    }

    return false;
}

AutoVectorizer::PackNode* AutoVectorizer::TryBuildScalarHWINTRINSICPack(
    LoopVectorizationPlan* plan, Statement* stmt, GenTreeHWIntrinsic* intrinsic, var_types elementType, unsigned depth)
{
    if (!varTypeIsFloating(elementType))
    {
        return nullptr;
    }

    const NamedIntrinsic intrinsicId = intrinsic->GetHWIntrinsicId();
    if ((intrinsicId != NI_Vector128_ToScalar)
#if defined(TARGET_XARCH)
        && (intrinsicId != NI_Vector256_ToScalar) && (intrinsicId != NI_Vector512_ToScalar)
#elif defined(TARGET_ARM64)
        && (intrinsicId != NI_Vector64_ToScalar)
#endif
    )
    {
        return nullptr;
    }

    if ((intrinsic->GetOperandCount() != 1) || !intrinsic->Op(1)->OperIs(GT_HWINTRINSIC))
    {
        return nullptr;
    }

    GenTreeHWIntrinsic* const fma = intrinsic->Op(1)->AsHWIntrinsic();

    GenTree* scalarOp1 = nullptr;
    GenTree* scalarOp2 = nullptr;
    GenTree* scalarOp3 = nullptr;

    switch (fma->GetHWIntrinsicId())
    {
#if defined(TARGET_XARCH)
        case NI_AVX2_MultiplyAddScalar:
        case NI_AVX512_FusedMultiplyAddScalar:
            if (fma->GetOperandCount() != 3)
            {
                return nullptr;
            }

            if (!TryGetScalarFromCreateScalar(plan, fma->Op(1), &scalarOp1) ||
                !TryGetScalarFromCreateScalar(plan, fma->Op(2), &scalarOp2) ||
                !TryGetScalarFromCreateScalar(plan, fma->Op(3), &scalarOp3))
            {
                return nullptr;
            }
            break;
#elif defined(TARGET_ARM64)
        case NI_AdvSimd_FusedMultiplyAddScalar:
            if (fma->GetOperandCount() != 3)
            {
                return nullptr;
            }

            if (!TryGetScalarFromCreateScalar(plan, fma->Op(3), &scalarOp1) ||
                !TryGetScalarFromCreateScalar(plan, fma->Op(2), &scalarOp2) ||
                !TryGetScalarFromCreateScalar(plan, fma->Op(1), &scalarOp3))
            {
                return nullptr;
            }
            break;
#endif

        default:
            return nullptr;
    }

    PackNode* const op1 = TryBuildPack(plan, stmt, scalarOp1, elementType, depth + 1);
    if (op1 == nullptr)
    {
        return nullptr;
    }

    PackNode* const op2 = TryBuildPack(plan, stmt, scalarOp2, elementType, depth + 1);
    if (op2 == nullptr)
    {
        return nullptr;
    }

    PackNode* const op3 = TryBuildPack(plan, stmt, scalarOp3, elementType, depth + 1);
    if (op3 == nullptr)
    {
        return nullptr;
    }

    PackNode* const ternary = NewPackNode(&plan->BodyPlan, PackKind::TernaryOp, elementType, plan->VectorizationFactor);
    if (ternary == nullptr)
    {
        JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
        JITDUMP("SLP node budget exceeded, bail out\n");
        return nullptr;
    }

    ternary->Oper          = GT_HWINTRINSIC;
    ternary->IntrinsicName = NI_System_Math_FusedMultiplyAdd;
    ternary->Operands[0]   = op1;
    ternary->Operands[1]   = op2;
    ternary->Operands[2]   = op3;
    ternary->Cost          = op1->Cost + op2->Cost + op3->Cost + 1;
    return ternary;
}

AutoVectorizer::PackNode* AutoVectorizer::TryBuildPack(
    LoopVectorizationPlan* plan, Statement* stmt, GenTree* value, var_types elementType, unsigned depth)
{
    if (depth > MaxPackDepth)
    {
        JITDUMP("pack recursion depth exceeded %u, bail out\n", MaxPackDepth);
        return nullptr;
    }

    value = UnwrapCommaValue(value);

    if (!TryNormalizeScalarValue(&value, elementType))
    {
        JITDUMPEXEC(m_compiler->gtDispTree(value));
        JITDUMP("value cannot be normalized to %s, bail out\n", varTypeName(elementType));
        return nullptr;
    }

    GenTree* indir = nullptr;
    if (TryGetIndirOperand(value, &indir))
    {
        LoopVectorizationPlan::ScalarAccess access;
        if (!TryAnalyzeIndirAccess(plan, stmt, indir, false, plan->InductionVar, &access))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(indir));
            JITDUMP("unsupported load access, bail out\n");
            return nullptr;
        }

        if (access.ElementType != elementType)
        {
            JITDUMPEXEC(m_compiler->gtDispTree(indir));
            JITDUMP("load element type %s does not match expression type %s, bail out\n",
                    varTypeName(access.ElementType), varTypeName(elementType));
            return nullptr;
        }

        unsigned accessIndex = UINT_MAX;
        if (!AddLoad(plan, access, &accessIndex))
        {
            return nullptr;
        }

        PackNode* const load =
            NewPackNode(&plan->BodyPlan, PackKind::LoadContiguous, elementType, plan->VectorizationFactor);
        if (load == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        load->AccessIndex = accessIndex;
        load->Lanes[0]    = access.Address;
        load->Cost        = 1;
        return load;
    }

    if (value->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (TryGetLocalDef(plan, value->AsLclVarCommon()->GetLclNum(), &def) && (def != value) && !def->OperIs(GT_PHI))
        {
            return TryBuildPack(plan, stmt, def, elementType, depth + 1);
        }
    }

    if (value->IsCnsIntOrI() || value->IsCnsFltOrDbl() ||
        TryGetInvariantOperand(plan->Loop, plan->IsPostIV ? plan->TripCountVar : plan->InductionVar, value,
                               elementType))
    {
        const PackKind kind =
            value->IsCnsIntOrI() || value->IsCnsFltOrDbl() ? PackKind::SplatConstant : PackKind::SplatScalar;
        PackNode* const splat = NewPackNode(&plan->BodyPlan, kind, elementType, plan->VectorizationFactor);
        if (splat == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        splat->Lanes[0] = value;
        splat->Cost     = 1;
        return splat;
    }

    if (value->OperIs(GT_NEG, GT_NOT))
    {
        if (!IsSupportedUnaryOp(value->OperGet(), elementType))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(value));
            JITDUMP("unsupported unary operation for %s, bail out\n", varTypeName(elementType));
            return nullptr;
        }

        PackNode* const operand = TryBuildPack(plan, stmt, value->AsOp()->gtOp1, elementType, depth + 1);
        if (operand == nullptr)
        {
            return nullptr;
        }

        PackNode* const unary = NewPackNode(&plan->BodyPlan, PackKind::UnaryOp, elementType, plan->VectorizationFactor);
        if (unary == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        unary->Oper        = value->OperGet();
        unary->Operands[0] = operand;
        unary->Cost        = operand->Cost + 1;
        return unary;
    }

    if (value->OperIs(GT_INTRINSIC))
    {
        GenTreeIntrinsic* const intrinsic = value->AsIntrinsic();
        if (!IsSupportedIntrinsic(intrinsic->gtIntrinsicName, elementType))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(value));
            JITDUMP("unsupported intrinsic for %s, bail out\n", varTypeName(elementType));
            return nullptr;
        }

        if (intrinsic->gtGetOp2() == nullptr)
        {
            PackNode* const operand = TryBuildPack(plan, stmt, intrinsic->gtGetOp1(), elementType, depth + 1);
            if (operand == nullptr)
            {
                return nullptr;
            }

            PackNode* const unary =
                NewPackNode(&plan->BodyPlan, PackKind::UnaryOp, elementType, plan->VectorizationFactor);
            if (unary == nullptr)
            {
                JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
                JITDUMP("SLP node budget exceeded, bail out\n");
                return nullptr;
            }

            unary->Oper          = GT_INTRINSIC;
            unary->IntrinsicName = intrinsic->gtIntrinsicName;
            unary->Operands[0]   = operand;
            unary->Cost          = operand->Cost + 1;
            return unary;
        }

        if ((intrinsic->gtIntrinsicName != NI_System_Math_Min) && (intrinsic->gtIntrinsicName != NI_System_Math_Max) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MinNative) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MaxNative) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MinUnsigned) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MaxUnsigned) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MinMagnitude) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MaxMagnitude) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MinMagnitudeNumber) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MaxMagnitudeNumber) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MinNumber) &&
            (intrinsic->gtIntrinsicName != NI_System_Math_MaxNumber))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(value));
            JITDUMP("intrinsic has unsupported operand shape, bail out\n");
            return nullptr;
        }

        PackNode* const op1 = TryBuildPack(plan, stmt, intrinsic->gtGetOp1(), elementType, depth + 1);
        if (op1 == nullptr)
        {
            return nullptr;
        }

        PackNode* const op2 = TryBuildPack(plan, stmt, intrinsic->gtGetOp2(), elementType, depth + 1);
        if (op2 == nullptr)
        {
            return nullptr;
        }

        PackNode* const binary =
            NewPackNode(&plan->BodyPlan, PackKind::BinaryOp, elementType, plan->VectorizationFactor);
        if (binary == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        binary->Oper          = GT_INTRINSIC;
        binary->IntrinsicName = intrinsic->gtIntrinsicName;
        binary->Operands[0]   = op1;
        binary->Operands[1]   = op2;
        binary->Cost          = op1->Cost + op2->Cost + 1;
        return binary;
    }

    if (value->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_AND, GT_AND_NOT, GT_OR, GT_XOR, GT_DIV, GT_LSH, GT_RSH, GT_RSZ))
    {
        if (!IsSupportedBinaryOp(value->OperGet(), elementType))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(value));
            JITDUMP("unsupported binary operation for %s, bail out\n", varTypeName(elementType));
            return nullptr;
        }

        PackNode* const op1 = TryBuildPack(plan, stmt, value->AsOp()->gtOp1, elementType, depth + 1);
        if (op1 == nullptr)
        {
            return nullptr;
        }

        PackNode* const op2 = TryBuildPack(plan, stmt, value->AsOp()->gtOp2, elementType, depth + 1);
        if (op2 == nullptr)
        {
            return nullptr;
        }

        if (((value->OperGet() == GT_LSH) || (value->OperGet() == GT_RSH) || (value->OperGet() == GT_RSZ)) &&
            (op2->Kind != PackKind::SplatConstant) && (op2->Kind != PackKind::SplatScalar))
        {
            JITDUMPEXEC(m_compiler->gtDispTree(value));
            JITDUMP("shift count is not loop-invariant, bail out\n");
            return nullptr;
        }

        PackNode* const binary =
            NewPackNode(&plan->BodyPlan, PackKind::BinaryOp, elementType, plan->VectorizationFactor);
        if (binary == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        binary->Oper        = value->OperGet();
        binary->Operands[0] = op1;
        binary->Operands[1] = op2;
        binary->Cost        = op1->Cost + op2->Cost + 1;
        return binary;
    }

    if (value->OperIs(GT_SELECT))
    {
        GenTreeConditional* const select = value->AsConditional();
        PackNode* const           cond   = TryBuildComparePack(plan, stmt, select->gtCond, elementType, depth + 1);
        if (cond == nullptr)
        {
            return nullptr;
        }

        PackNode* const trueValue = TryBuildPack(plan, stmt, select->gtOp1, elementType, depth + 1);
        if (trueValue == nullptr)
        {
            return nullptr;
        }

        PackNode* const falseValue = TryBuildPack(plan, stmt, select->gtOp2, elementType, depth + 1);
        if (falseValue == nullptr)
        {
            return nullptr;
        }

        PackNode* const selectPack =
            NewPackNode(&plan->BodyPlan, PackKind::Select, elementType, plan->VectorizationFactor);
        if (selectPack == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", plan->BodyPlan.NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return nullptr;
        }

        selectPack->Operands[0] = cond;
        selectPack->Operands[1] = trueValue;
        selectPack->Operands[2] = falseValue;
        selectPack->Cost        = cond->Cost + trueValue->Cost + falseValue->Cost + 1;
        return selectPack;
    }

    if (value->OperIs(GT_HWINTRINSIC))
    {
        PackNode* const intrinsicPack =
            TryBuildScalarHWINTRINSICPack(plan, stmt, value->AsHWIntrinsic(), elementType, depth + 1);
        if (intrinsicPack != nullptr)
        {
            return intrinsicPack;
        }

        JITDUMPEXEC(m_compiler->gtDispTree(value));
        JITDUMP("unsupported scalar hardware intrinsic, bail out\n");
        return nullptr;
    }

    JITDUMPEXEC(m_compiler->gtDispTree(value));
    JITDUMP("unsupported value, bail out\n");
    return nullptr;
}

bool AutoVectorizer::TryBuildSLPPlan(LoopVectorizationPlan* plan)
{
    assert(plan->VectorizationFactor <= MaxLanes);

    SLPPlan* const slpPlan = &plan->BodyPlan;
    *slpPlan               = SLPPlan();

    for (unsigned i = 0; i < plan->StoreCount; i++)
    {
        PackNode* const value = TryBuildPack(plan, plan->StoreStmts[i], plan->StoreValues[i], plan->ElementType);
        if (value == nullptr)
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(plan->StoreStmts[i]));
            JITDUMP("store value cannot be packed, bail out\n");
            return false;
        }

        PackNode* const store =
            NewPackNode(slpPlan, PackKind::StoreContiguous, plan->ElementType, plan->VectorizationFactor);
        if (store == nullptr)
        {
            JITDUMP("SLP node count=%u, max=%u\n", slpPlan->NodeCount, MaxPackNodes);
            JITDUMP("SLP node budget exceeded, bail out\n");
            return false;
        }

        store->AccessIndex = i;
        store->Lanes[0]    = plan->StoreAccesses[i].Address;
        store->Operands[0] = value;
        store->Cost        = value->Cost + 1;

        slpPlan->Roots[slpPlan->RootCount++] = store;
        slpPlan->Root                        = store;
    }

    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[i];
        PackNode* const pack = TryBuildPack(plan, reduction.Stmt, reduction.Value, plan->ElementType);
        if (pack == nullptr)
        {
            JITDUMPEXEC(m_compiler->gtDispStmt(reduction.Stmt));
            JITDUMP("reduction value cannot be packed, bail out\n");
            return false;
        }

        reduction.Pack = pack;
    }

    if (plan->LoadCount == 0)
    {
        JITDUMP("SLP found no vector loads\n");
    }

    if (!ValidateMemoryDependences(plan))
    {
        return false;
    }

    if (plan->SawBoundsCheck)
    {
        if (plan->Step < 0)
        {
            JITDUMP("remaining bounds check in descending loop, bail out\n");
            return false;
        }

        if (!plan->HasConstInit || ((plan->ConstInitValue + plan->MinIndexOffset) < 0))
        {
            JITDUMP("hasConstInit=%s, init=%d, minIndexOffset=%d\n", dspBool(plan->HasConstInit), plan->ConstInitValue,
                    plan->MinIndexOffset);
            JITDUMP("remaining bounds check without lower-bound proof, bail out\n");
            return false;
        }

        bool hasArrayAccess    = false;
        bool hasNonArrayAccess = false;
        for (unsigned i = 0; i < plan->StoreCount; i++)
        {
            hasArrayAccess |= plan->StoreAccesses[i].IsArray;
            hasNonArrayAccess |= !plan->StoreAccesses[i].IsArray;
        }

        for (unsigned i = 0; i < plan->LoadCount; i++)
        {
            hasArrayAccess |= plan->LoadAccesses[i].IsArray;
            hasNonArrayAccess |= !plan->LoadAccesses[i].IsArray;
        }

        if (hasNonArrayAccess && !TryProveRemainingBoundsChecks(plan))
        {
            JITDUMP("remaining bounds check on byref/span access is not proven by loop limit, bail out\n");
            return false;
        }

        if (hasArrayAccess)
        {
            unsigned lengthLcls[LoopVectorizationPlan::MaxAccesses] = {};
            int      endOffsets[LoopVectorizationPlan::MaxAccesses] = {};
            unsigned lengthCount                                    = 0;
            if (!TryCollectArrayLengthLimitLocals(plan, plan->End, lengthLcls, endOffsets,
                                                  LoopVectorizationPlan::MaxAccesses, &lengthCount))
            {
                JITDUMP("loop limit:\n");
                JITDUMPEXEC(m_compiler->gtDispTree(plan->End));
                JITDUMP("remaining bounds check without vector-limit proof for max offset %d, bail out\n",
                        plan->MaxIndexOffset);
                return false;
            }

            auto hasLimitForAccess = [=](const LoopVectorizationPlan::ScalarAccess& access) {
                if (access.InvariantIndexLocal != BAD_VAR_NUM)
                {
                    return false;
                }

                for (unsigned i = 0; i < lengthCount; i++)
                {
                    const int accessOffset = access.IndexOffset;
                    if ((lengthLcls[i] == access.BaseLocalIfKnown) &&
                        ((plan->TestOper == GT_LE) ? (endOffsets[i] <= -1 - accessOffset)
                                                   : (endOffsets[i] <= -accessOffset)))
                    {
                        return true;
                    }
                }

                return false;
            };

            for (unsigned i = 0; i < plan->StoreCount; i++)
            {
                if (plan->StoreAccesses[i].IsArray && !hasLimitForAccess(plan->StoreAccesses[i]))
                {
                    JITDUMPEXEC(m_compiler->gtDispStmt(plan->StoreStmts[i]));
                    JITDUMP("store array does not match a proven loop limit, bail out\n");
                    return false;
                }
            }

            for (unsigned i = 0; i < plan->LoadCount; i++)
            {
                if (plan->LoadAccesses[i].IsArray && !hasLimitForAccess(plan->LoadAccesses[i]))
                {
                    JITDUMPEXEC(m_compiler->gtDispTree(plan->LoadAccesses[i].Address));
                    JITDUMP("load array does not match a proven loop limit, bail out\n");
                    return false;
                }
            }
        }
    }

    if (plan->IsPostIV)
    {
        auto hasExpectedAddressUpdate = [=](unsigned updateVar) {
            for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
            {
                if (plan->AddressUpdateVars[i] == updateVar)
                {
                    return abs(plan->AddressUpdateDeltas[i]) == static_cast<int>(plan->ElementSize);
                }
            }

            return false;
        };

        for (unsigned i = 0; i < plan->StoreCount; i++)
        {
            const LoopVectorizationPlan::ScalarAccess& access = plan->StoreAccesses[i];
            const unsigned updateVar = access.IsByrefLocal ? access.BaseLocalIfKnown : access.OffsetLocalIfKnown;
            if (!hasExpectedAddressUpdate(updateVar))
            {
                JITDUMP("store update var V%02u, expected absolute delta=%u\n", updateVar, plan->ElementSize);
                JITDUMP("missing post-IV address update, bail out\n");
                return false;
            }
        }

        for (unsigned i = 0; i < plan->LoadCount; i++)
        {
            const LoopVectorizationPlan::ScalarAccess& access = plan->LoadAccesses[i];
            const unsigned updateVar = access.IsByrefLocal ? access.BaseLocalIfKnown : access.OffsetLocalIfKnown;
            if (!hasExpectedAddressUpdate(updateVar))
            {
                JITDUMP("load update var V%02u, expected absolute delta=%u\n", updateVar, plan->ElementSize);
                JITDUMP("missing post-IV address update, bail out\n");
                return false;
            }
        }

        if (plan->NeedsOverlapCheck)
        {
            for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
            {
                if (plan->AddressUpdateDeltas[i] < 0)
                {
                    JITDUMP("runtime overlap check for descending post-IV walk is not supported, bail out\n");
                    return false;
                }
            }
        }
    }

    for (unsigned i = 0; i < slpPlan->RootCount; i++)
    {
        slpPlan->EstimatedVectorCost += slpPlan->Roots[i]->Cost;
    }

    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        slpPlan->EstimatedVectorCost += plan->Reductions[i].Pack->Cost + 3;
    }

    slpPlan->EstimatedScalarCost    = plan->VectorizationFactor * slpPlan->EstimatedVectorCost;
    slpPlan->EstimatedCodeSizeDelta = 8 + (4 * slpPlan->RootCount);

    JITDUMP("SLP accepted, estimated scalar=%u, vector=%u, codeSizeDelta=%u\n", slpPlan->EstimatedScalarCost,
            slpPlan->EstimatedVectorCost, slpPlan->EstimatedCodeSizeDelta);
    return true;
}

bool AutoVectorizer::TryRewritePlan(LoopVectorizationPlan* plan)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    if (!ReportVectorIsa(plan->VectorSizeBytes))
    {
        JITDUMP("selected vector size %u is not available on this target\n", plan->VectorSizeBytes);
        JITDUMP("target ISA unavailable for selected vector size, bail out\n");
        return false;
    }

    if ((plan->StoreCount == 0) && (plan->ReductionCount == 0))
    {
        JITDUMP("loop " FMT_LP " has no selected store or reduction statement\n", plan->Loop->GetIndex());
        JITDUMP("missing statement for rewrite, bail out\n");
        return false;
    }

    const bool needsOverlapCheck = plan->NeedsOverlapCheck;

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

    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        const var_types simdType = Compiler::getSIMDTypeForSize(plan->VectorSizeBytes);
        LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[i];
        reduction.VectorLcl = m_compiler->lvaGrabTemp(false DEBUGARG("auto vectorization reduction accumulator"));
        m_compiler->lvaTable[reduction.VectorLcl].lvType = simdType;

        Statement* const reductionInitStmt = m_compiler->fgNewStmtAtEnd(plan->Preheader, BuildReductionInit(plan, i));
        m_compiler->gtSetStmtInfo(reductionInitStmt);
        m_compiler->fgSetStmtSeq(reductionInitStmt);
        JITDUMP("generated reduction init in " FMT_BB ":\n", plan->Preheader->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(reductionInitStmt));
    }

    GenTree* const   vectorTest = BuildVectorLoopTest(plan);
    Statement* const testStmt   = m_compiler->fgNewStmtAtEnd(vectorCheck, vectorTest);
    m_compiler->gtSetStmtInfo(testStmt);
    m_compiler->fgSetStmtSeq(testStmt);
    JITDUMP("generated vector-loop test statement in " FMT_BB ":\n", vectorCheck->bbNum);
    JITDUMPEXEC(m_compiler->gtDispStmt(testStmt));

    for (unsigned i = 0; i < plan->BodyPlan.RootCount; i++)
    {
        PackNode* const root = plan->BodyPlan.Roots[i];
        JITDUMP("scalar store selected for rewrite:\n");
        JITDUMPEXEC(m_compiler->gtDispStmt(plan->StoreStmts[root->AccessIndex]));

        GenTree* const vectorStore = BuildVectorStore(plan, root);
        JITDUMP("generated vector store:\n");
        JITDUMPEXEC(m_compiler->gtDispTree(vectorStore));

        Statement* const storeStmt = m_compiler->fgNewStmtAtEnd(vectorBody, vectorStore);
        m_compiler->gtSetStmtInfo(storeStmt);
        m_compiler->fgSetStmtSeq(storeStmt);
    }

    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[i];
        JITDUMP("scalar reduction selected for rewrite:\n");
        JITDUMPEXEC(m_compiler->gtDispStmt(reduction.Stmt));

        Statement* const reductionUpdateStmt = m_compiler->fgNewStmtAtEnd(vectorBody, BuildReductionUpdate(plan, i));
        m_compiler->gtSetStmtInfo(reductionUpdateStmt);
        m_compiler->fgSetStmtSeq(reductionUpdateStmt);
        JITDUMP("generated vector reduction update:\n");
        JITDUMPEXEC(m_compiler->gtDispStmt(reductionUpdateStmt));
    }

    if (plan->IsPostIV)
    {
        for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
        {
            Statement* const addressUpdateStmt =
                m_compiler->fgNewStmtAtEnd(vectorBody, BuildAddressUpdate(plan, plan->AddressUpdateVars[i]));
            m_compiler->gtSetStmtInfo(addressUpdateStmt);
            m_compiler->fgSetStmtSeq(addressUpdateStmt);
            JITDUMP("generated post-IV address update V%02u in " FMT_BB ":\n", plan->AddressUpdateVars[i],
                    vectorBody->bbNum);
            JITDUMPEXEC(m_compiler->gtDispStmt(addressUpdateStmt));
        }

        Statement* const tripCountUpdateStmt =
            m_compiler->fgNewStmtAtEnd(vectorBody,
                                       BuildTripCountUpdate(plan, -static_cast<int>(plan->VectorizationFactor)));
        m_compiler->gtSetStmtInfo(tripCountUpdateStmt);
        m_compiler->fgSetStmtSeq(tripCountUpdateStmt);
        JITDUMP("generated post-IV trip-count update in " FMT_BB ":\n", vectorBody->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(tripCountUpdateStmt));
    }
    else
    {
        GenTree* const   ivUpdate = BuildIVUpdate(plan);
        Statement* const ivStmt   = m_compiler->fgNewStmtAtEnd(vectorBody, ivUpdate);
        m_compiler->gtSetStmtInfo(ivStmt);
        m_compiler->fgSetStmtSeq(ivStmt);
        JITDUMP("generated IV update in " FMT_BB ":\n", vectorBody->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(ivStmt));
    }

    for (unsigned i = 0; i < plan->ReductionCount; i++)
    {
        Statement* const reductionFinalizeStmt = m_compiler->fgNewStmtAtEnd(scalarGuard, BuildReductionFinalize(plan, i));
        m_compiler->gtSetStmtInfo(reductionFinalizeStmt);
        m_compiler->fgSetStmtSeq(reductionFinalizeStmt);
        JITDUMP("generated reduction finalize in " FMT_BB ":\n", scalarGuard->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(reductionFinalizeStmt));
    }

    Statement* const scalarGuardStmt = m_compiler->fgNewStmtAtEnd(scalarGuard, BuildScalarRemainderTest(plan));
    m_compiler->gtSetStmtInfo(scalarGuardStmt);
    m_compiler->fgSetStmtSeq(scalarGuardStmt);
    JITDUMP("generated scalar-epilogue guard in " FMT_BB ":\n", scalarGuard->bbNum);
    JITDUMPEXEC(m_compiler->gtDispStmt(scalarGuardStmt));

    JITDUMP("CFG rewrite blocks: vectorBody=" FMT_BB ", vectorCheck=" FMT_BB ", scalarGuard=" FMT_BB, vectorBody->bbNum,
            vectorCheck->bbNum, scalarGuard->bbNum);
    if (sameStartCheck != nullptr)
    {
        JITDUMP(", sameStartCheck=" FMT_BB ", storeBeforeCheck=" FMT_BB ", loadBeforeCheck=" FMT_BB,
                sameStartCheck->bbNum, storeBeforeCheck->bbNum, loadBeforeCheck->bbNum);
    }
    JITDUMP("\n");
    JITDUMP("redirecting preheader " FMT_BB " to vector check " FMT_BB "\n", plan->Preheader->bbNum,
            vectorCheck->bbNum);
    m_compiler->fgRedirectEdge(plan->Preheader->TargetEdgeRef(), vectorCheck);

    FlowEdge* const vectorCheckToVectorBody =
        m_compiler->fgAddRefPred(needsOverlapCheck ? sameStartCheck : vectorBody, vectorCheck);
    FlowEdge* const vectorCheckToScalar = m_compiler->fgAddRefPred(scalarGuard, vectorCheck);
    JITDUMP("edge " FMT_BB " vector-test true -> " FMT_BB " likelihood=0.9\n", vectorCheck->bbNum,
            needsOverlapCheck ? sameStartCheck->bbNum : vectorBody->bbNum);
    JITDUMP("edge " FMT_BB " vector-test false -> " FMT_BB " likelihood=0.1\n", vectorCheck->bbNum, scalarGuard->bbNum);
    vectorCheckToVectorBody->setLikelihood(0.9);
    vectorCheckToScalar->setLikelihood(0.1);
    vectorCheck->SetCond(vectorCheckToVectorBody, vectorCheckToScalar);

    FlowEdge* const vectorBodyToCheck = m_compiler->fgAddRefPred(vectorCheck, vectorBody);
    JITDUMP("edge " FMT_BB " vector-latch -> " FMT_BB " likelihood=1.0\n", vectorBody->bbNum, vectorCheck->bbNum);
    vectorBodyToCheck->setLikelihood(1.0);
    vectorBody->SetKindAndTargetEdge(BBJ_ALWAYS, vectorBodyToCheck);

    if (sameStartCheck != nullptr)
    {
        Statement* const sameStartStmt = m_compiler->fgNewStmtAtEnd(sameStartCheck, BuildPostIVSameStartCheck(plan));
        m_compiler->gtSetStmtInfo(sameStartStmt);
        m_compiler->fgSetStmtSeq(sameStartStmt);
        JITDUMP("generated same-start overlap check in " FMT_BB ":\n", sameStartCheck->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(sameStartStmt));

        Statement* const storeBeforeStmt =
            m_compiler->fgNewStmtAtEnd(storeBeforeCheck, BuildPostIVStoreBeforeLoadCheck(plan));
        m_compiler->gtSetStmtInfo(storeBeforeStmt);
        m_compiler->fgSetStmtSeq(storeBeforeStmt);
        JITDUMP("generated store-before-load overlap check in " FMT_BB ":\n", storeBeforeCheck->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(storeBeforeStmt));

        Statement* const loadBeforeStmt =
            m_compiler->fgNewStmtAtEnd(loadBeforeCheck, BuildPostIVLoadBeforeStoreCheck(plan));
        m_compiler->gtSetStmtInfo(loadBeforeStmt);
        m_compiler->fgSetStmtSeq(loadBeforeStmt);
        JITDUMP("generated load-before-store overlap check in " FMT_BB ":\n", loadBeforeCheck->bbNum);
        JITDUMPEXEC(m_compiler->gtDispStmt(loadBeforeStmt));

        FlowEdge* const sameStartToVector = m_compiler->fgAddRefPred(vectorBody, sameStartCheck);
        FlowEdge* const sameStartToNext   = m_compiler->fgAddRefPred(storeBeforeCheck, sameStartCheck);
        JITDUMP("edge " FMT_BB " same-start true -> " FMT_BB " likelihood=0.1\n", sameStartCheck->bbNum,
                vectorBody->bbNum);
        JITDUMP("edge " FMT_BB " same-start false -> " FMT_BB " likelihood=0.9\n", sameStartCheck->bbNum,
                storeBeforeCheck->bbNum);
        sameStartToVector->setLikelihood(0.1);
        sameStartToNext->setLikelihood(0.9);
        sameStartCheck->SetCond(sameStartToVector, sameStartToNext);

        FlowEdge* const storeBeforeToVector = m_compiler->fgAddRefPred(vectorBody, storeBeforeCheck);
        FlowEdge* const storeBeforeToNext   = m_compiler->fgAddRefPred(loadBeforeCheck, storeBeforeCheck);
        JITDUMP("edge " FMT_BB " store-before-load true -> " FMT_BB " likelihood=0.45\n", storeBeforeCheck->bbNum,
                vectorBody->bbNum);
        JITDUMP("edge " FMT_BB " store-before-load false -> " FMT_BB " likelihood=0.55\n", storeBeforeCheck->bbNum,
                loadBeforeCheck->bbNum);
        storeBeforeToVector->setLikelihood(0.45);
        storeBeforeToNext->setLikelihood(0.55);
        storeBeforeCheck->SetCond(storeBeforeToVector, storeBeforeToNext);

        FlowEdge* const loadBeforeToVector = m_compiler->fgAddRefPred(vectorBody, loadBeforeCheck);
        FlowEdge* const loadBeforeToScalar = m_compiler->fgAddRefPred(scalarGuard, loadBeforeCheck);
        JITDUMP("edge " FMT_BB " load-before-store true -> " FMT_BB " likelihood=0.45\n", loadBeforeCheck->bbNum,
                vectorBody->bbNum);
        JITDUMP("edge " FMT_BB " load-before-store false -> " FMT_BB " likelihood=0.55\n", loadBeforeCheck->bbNum,
                scalarGuard->bbNum);
        loadBeforeToVector->setLikelihood(0.45);
        loadBeforeToScalar->setLikelihood(0.55);
        loadBeforeCheck->SetCond(loadBeforeToVector, loadBeforeToScalar);
    }

    FlowEdge* const scalarGuardToExit   = m_compiler->fgAddRefPred(plan->Exit, scalarGuard);
    FlowEdge* const scalarGuardToScalar = m_compiler->fgAddRefPred(plan->Header, scalarGuard);
    JITDUMP("edge " FMT_BB " scalar-guard true -> exit " FMT_BB " likelihood=0.5\n", scalarGuard->bbNum,
            plan->Exit->bbNum);
    JITDUMP("edge " FMT_BB " scalar-guard false -> scalar loop " FMT_BB " likelihood=0.5\n", scalarGuard->bbNum,
            plan->Header->bbNum);
    scalarGuardToExit->setLikelihood(0.5);
    scalarGuardToScalar->setLikelihood(0.5);
    scalarGuard->SetCond(scalarGuardToExit, scalarGuardToScalar);

    JITDUMP("rewrote loop " FMT_LP " with %u-byte vectors and scalar epilogue\n", plan->Loop->GetIndex(),
            plan->VectorSizeBytes);
    m_compiler->Metrics.LoopsVectorized++;
    return true;
#else
    JITDUMP("SIMD rewrite not supported on target, bail out\n");
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
        case PackKind::UnaryOp:
            return "unary";
        case PackKind::BinaryOp:
            return "binary";
        case PackKind::TernaryOp:
            return "ternary";
        case PackKind::CompareOp:
            return "compare";
        case PackKind::Select:
            return "select";
        default:
            return "invalid";
    }
}

void AutoVectorizer::DumpSLPPlan(const LoopVectorizationPlan& plan) const
{
#ifdef DEBUG
    const SLPPlan& slpPlan = plan.BodyPlan;
    JITDUMP("pack:\n");
    for (unsigned i = 0; i < slpPlan.NodeCount; i++)
    {
        const PackNode& node = slpPlan.Nodes[i];
        JITDUMP("  N%02u %-17s lanes=%u elem=%s cost=%u", i, PackKindName(node.Kind), node.LaneCount,
                varTypeName(node.ElementType), node.Cost);

        if ((node.Kind == PackKind::UnaryOp) || (node.Kind == PackKind::BinaryOp) ||
            (node.Kind == PackKind::TernaryOp) || (node.Kind == PackKind::CompareOp))
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

        if (node.Operands[2] != nullptr)
        {
            JITDUMP(" op2=N%02u", static_cast<unsigned>(node.Operands[2] - slpPlan.Nodes));
        }

        JITDUMP("\n");
    }
#endif
}

GenTree* AutoVectorizer::BuildAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access)
{
    if (plan->IsPostIV)
    {
        return BuildPostIVAddress(plan, access);
    }

    if (access.IsArray)
    {
        return BuildArrayAddress(plan, access);
    }

    return BuildByrefAddress(plan, access);
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

    if ((plan->Step < 0) && (plan->VectorizationFactor > 1))
    {
        index = m_compiler->gtNewOperNode(GT_SUB, index->TypeGet(), index,
                                          m_compiler->gtNewIconNode(plan->VectorizationFactor - 1, index->TypeGet()));
    }

    if (access.InvariantIndexLocal != BAD_VAR_NUM)
    {
        LclVarDsc* const invariantDsc = m_compiler->lvaGetDesc(access.InvariantIndexLocal);
        GenTree* const   invariant =
            m_compiler->gtNewCastNode(TYP_I_IMPL,
                                      m_compiler->gtNewLclvNode(access.InvariantIndexLocal, invariantDsc->TypeGet()),
                                      varTypeIsUnsigned(invariantDsc->TypeGet()), TYP_I_IMPL);
        index = m_compiler->gtNewCastNode(TYP_I_IMPL, index, varTypeIsUnsigned(index->TypeGet()), TYP_I_IMPL);
        index = m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, index, invariant);
    }

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

GenTree* AutoVectorizer::BuildByrefAddress(LoopVectorizationPlan*                     plan,
                                           const LoopVectorizationPlan::ScalarAccess& access)
{
    assert(access.IsByrefWithIndex);

    LclVarDsc* const baseDsc = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
    GenTree* const   base    = m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, baseDsc->TypeGet());
    LclVarDsc* const ivDsc   = m_compiler->lvaGetDesc(plan->InductionVar);
    GenTree*         index   = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());

    if ((plan->Step < 0) && (plan->VectorizationFactor > 1))
    {
        index = m_compiler->gtNewOperNode(GT_SUB, index->TypeGet(), index,
                                          m_compiler->gtNewIconNode(plan->VectorizationFactor - 1, index->TypeGet()));
    }

    if (access.InvariantIndexLocal != BAD_VAR_NUM)
    {
        LclVarDsc* const invariantDsc = m_compiler->lvaGetDesc(access.InvariantIndexLocal);
        GenTree* const   invariant =
            m_compiler->gtNewCastNode(TYP_I_IMPL,
                                      m_compiler->gtNewLclvNode(access.InvariantIndexLocal, invariantDsc->TypeGet()),
                                      varTypeIsUnsigned(invariantDsc->TypeGet()), TYP_I_IMPL);
        index = m_compiler->gtNewCastNode(TYP_I_IMPL, index, varTypeIsUnsigned(index->TypeGet()), TYP_I_IMPL);
        index = m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, index, invariant);
    }

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
    if (access.ElementSize > 1)
    {
        offset = m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL, offset,
                                           m_compiler->gtNewIconNode(access.ElementSize, TYP_I_IMPL));
    }

    return m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, base, offset);
}

GenTree* AutoVectorizer::BuildVectorLoopTest(LoopVectorizationPlan* plan)
{
    if (plan->IsPostIV)
    {
        LclVarDsc* const tripCountDsc = m_compiler->lvaGetDesc(plan->TripCountVar);
        GenTree* const   tripCount    = m_compiler->gtNewLclvNode(plan->TripCountVar, tripCountDsc->TypeGet());
        GenTree* const   cmp =
            m_compiler->gtNewOperNode(GT_GE, TYP_INT, tripCount,
                                      m_compiler->gtNewIconNode(plan->VectorizationFactor, tripCountDsc->TypeGet()));
        return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
    }

    LclVarDsc* const ivDsc = m_compiler->lvaGetDesc(plan->InductionVar);
    GenTree*         iv    = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());
    GenTree*         end   = m_compiler->gtCloneExpr(plan->End);

    GenTree* lastLane = m_compiler->gtNewCastNode(TYP_LONG, iv, false, TYP_LONG);
    if (plan->VectorizationFactor > 1)
    {
        lastLane =
            m_compiler->gtNewOperNode(plan->Step < 0 ? GT_SUB : GT_ADD, TYP_LONG, lastLane,
                                      m_compiler->gtNewLconNode(static_cast<int64_t>(plan->VectorizationFactor - 1)));
    }

    end = m_compiler->gtNewCastNode(TYP_LONG, end, false, TYP_LONG);

    genTreeOps cmpOper;
    switch (plan->TestOper)
    {
        case GT_LT:
        case GT_LE:
        case GT_GT:
        case GT_GE:
            cmpOper = plan->TestOper;
            break;
        default:
            unreached();
    }

    GenTree* const cmp = m_compiler->gtNewOperNode(cmpOper, TYP_INT, lastLane, end);
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVSameStartCheck(LoopVectorizationPlan* plan)
{
    GenTree* const cmp = m_compiler->gtNewOperNode(GT_EQ, TYP_INT, BuildPostIVAddress(plan, plan->StoreAccess),
                                                   BuildPostIVAddress(plan, plan->LoadAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVStoreBeforeLoadCheck(LoopVectorizationPlan* plan)
{
    LclVarDsc* const tripCountDsc = m_compiler->lvaGetDesc(plan->TripCountVar);
    GenTree* const   byteCount =
        m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL,
                                  m_compiler->gtNewCastNode(TYP_I_IMPL,
                                                            m_compiler->gtNewLclvNode(plan->TripCountVar,
                                                                                      tripCountDsc->TypeGet()),
                                                            varTypeIsUnsigned(tripCountDsc->TypeGet()), TYP_I_IMPL),
                                  m_compiler->gtNewIconNode(plan->ElementSize, TYP_I_IMPL));
    GenTree* const storeEnd =
        m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, BuildPostIVAddress(plan, plan->StoreAccess), byteCount);
    GenTree* const cmp =
        m_compiler->gtNewOperNode(GT_LE, TYP_INT, storeEnd, BuildPostIVAddress(plan, plan->LoadAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVLoadBeforeStoreCheck(LoopVectorizationPlan* plan)
{
    LclVarDsc* const tripCountDsc = m_compiler->lvaGetDesc(plan->TripCountVar);
    GenTree* const   byteCount =
        m_compiler->gtNewOperNode(GT_MUL, TYP_I_IMPL,
                                  m_compiler->gtNewCastNode(TYP_I_IMPL,
                                                            m_compiler->gtNewLclvNode(plan->TripCountVar,
                                                                                      tripCountDsc->TypeGet()),
                                                            varTypeIsUnsigned(tripCountDsc->TypeGet()), TYP_I_IMPL),
                                  m_compiler->gtNewIconNode(plan->ElementSize, TYP_I_IMPL));
    GenTree* const loadEnd =
        m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, BuildPostIVAddress(plan, plan->LoadAccess), byteCount);
    GenTree* const cmp =
        m_compiler->gtNewOperNode(GT_LE, TYP_INT, loadEnd, BuildPostIVAddress(plan, plan->StoreAccess));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildPostIVAddress(LoopVectorizationPlan*                     plan,
                                            const LoopVectorizationPlan::ScalarAccess& access)
{
    auto adjustForDescendingWalk = [=](GenTree* addr) -> GenTree* {
        const unsigned updateVar = access.IsByrefLocal ? access.BaseLocalIfKnown : access.OffsetLocalIfKnown;
        int            delta     = static_cast<int>(access.ElementSize);
        for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
        {
            if (plan->AddressUpdateVars[i] == updateVar)
            {
                delta = plan->AddressUpdateDeltas[i];
                break;
            }
        }

        if (delta >= 0)
        {
            return addr;
        }

        const int firstLaneDelta = -static_cast<int>(plan->VectorSizeBytes - access.ElementSize);
        return m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, addr,
                                         m_compiler->gtNewIconNode(firstLaneDelta, TYP_I_IMPL));
    };

    if (access.IsByrefLocal)
    {
        LclVarDsc* const addrDsc = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
        return adjustForDescendingWalk(m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, addrDsc->TypeGet()));
    }

    if (access.IsByrefBaseWithOffset)
    {
        LclVarDsc* const baseDsc   = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
        LclVarDsc* const offsetDsc = m_compiler->lvaGetDesc(access.OffsetLocalIfKnown);
        GenTree* const   base      = m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, baseDsc->TypeGet());
        GenTree* const   offset    = m_compiler->gtNewLclvNode(access.OffsetLocalIfKnown, offsetDsc->TypeGet());
        return adjustForDescendingWalk(m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, base, offset));
    }

    GenTreeArrAddr* const oldArrAddr = access.Address->AsArrAddr();
    LclVarDsc* const      arrayDsc   = m_compiler->lvaGetDesc(access.BaseLocalIfKnown);
    LclVarDsc* const      offsetDsc  = m_compiler->lvaGetDesc(access.OffsetLocalIfKnown);
    GenTree* const        arrRef     = m_compiler->gtNewLclvNode(access.BaseLocalIfKnown, arrayDsc->TypeGet());
    GenTree* const        offset     = m_compiler->gtNewLclvNode(access.OffsetLocalIfKnown, offsetDsc->TypeGet());
    GenTree* const        addr = adjustForDescendingWalk(m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, arrRef, offset));
    GenTreeArrAddr* const arrAddr = new (m_compiler, GT_ARR_ADDR)
        GenTreeArrAddr(addr, access.ElementType, oldArrAddr->GetElemClassHandle(), oldArrAddr->GetFirstElemOffset());
    arrAddr->gtFlags |= oldArrAddr->gtFlags & GTF_ARR_ADDR_NONNULL;
    return arrAddr;
}

GenTree* AutoVectorizer::BuildPackNode(LoopVectorizationPlan* plan, PackNode* node)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned  simdSize = plan->VectorSizeBytes;
    const var_types simdType = Compiler::getSIMDTypeForSize(simdSize);

    switch (node->Kind)
    {
        case PackKind::LoadContiguous:
            return m_compiler->gtNewSimdLoadNode(simdType, BuildAddress(plan, plan->LoadAccesses[node->AccessIndex]),
                                                 node->ElementType, simdSize);

        case PackKind::SplatConstant:
        case PackKind::SplatScalar:
            return m_compiler->gtNewSimdCreateBroadcastNode(simdType, m_compiler->gtCloneExpr(node->Lanes[0]),
                                                            node->ElementType, simdSize);

        case PackKind::UnaryOp:
            if (node->Oper == GT_INTRINSIC)
            {
                switch (node->IntrinsicName)
                {
                    case NI_System_Math_Abs:
                        return m_compiler->gtNewSimdAbsNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                            node->ElementType, simdSize);
                    case NI_System_Math_Ceiling:
                        return m_compiler->gtNewSimdCeilNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                             node->ElementType, simdSize);
                    case NI_System_Math_Floor:
                        return m_compiler->gtNewSimdFloorNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                              node->ElementType, simdSize);
                    case NI_System_Math_Round:
                        return m_compiler->gtNewSimdRoundNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                              node->ElementType, simdSize);
                    case NI_System_Math_Sqrt:
                        return m_compiler->gtNewSimdSqrtNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                             node->ElementType, simdSize);
                    case NI_System_Math_Truncate:
                        return m_compiler->gtNewSimdTruncNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                              node->ElementType, simdSize);
                    default:
                        unreached();
                }
            }

            return m_compiler->gtNewSimdUnOpNode(node->Oper, simdType, BuildPackNode(plan, node->Operands[0]),
                                                 node->ElementType, simdSize);

        case PackKind::BinaryOp:
        {
            GenTree* const op1 = BuildPackNode(plan, node->Operands[0]);
            GenTree*       op2 = BuildPackNode(plan, node->Operands[1]);
            if (node->Oper == GT_INTRINSIC)
            {
                const bool isMax = (node->IntrinsicName == NI_System_Math_Max) ||
                                   (node->IntrinsicName == NI_System_Math_MaxNative) ||
                                   (node->IntrinsicName == NI_System_Math_MaxUnsigned) ||
                                   (node->IntrinsicName == NI_System_Math_MaxMagnitude) ||
                                   (node->IntrinsicName == NI_System_Math_MaxMagnitudeNumber) ||
                                   (node->IntrinsicName == NI_System_Math_MaxNumber);
                const bool isMagnitude = (node->IntrinsicName == NI_System_Math_MinMagnitude) ||
                                         (node->IntrinsicName == NI_System_Math_MaxMagnitude) ||
                                         (node->IntrinsicName == NI_System_Math_MinMagnitudeNumber) ||
                                         (node->IntrinsicName == NI_System_Math_MaxMagnitudeNumber);
                const bool isNumber = (node->IntrinsicName == NI_System_Math_MinNumber) ||
                                      (node->IntrinsicName == NI_System_Math_MaxNumber) ||
                                      (node->IntrinsicName == NI_System_Math_MinMagnitudeNumber) ||
                                      (node->IntrinsicName == NI_System_Math_MaxMagnitudeNumber);
                if ((node->IntrinsicName == NI_System_Math_MinNative) ||
                    (node->IntrinsicName == NI_System_Math_MaxNative))
                {
                    return m_compiler->gtNewSimdMinMaxNativeNode(simdType, op1, op2, node->ElementType, simdSize,
                                                                 isMax);
                }

                return m_compiler->gtNewSimdMinMaxNode(simdType, op1, op2, node->ElementType, simdSize, isMax,
                                                       isMagnitude, isNumber);
            }

            if ((node->Oper == GT_LSH) || (node->Oper == GT_RSH) || (node->Oper == GT_RSZ))
            {
                if ((node->Operands[1]->Kind != PackKind::SplatConstant) &&
                    (node->Operands[1]->Kind != PackKind::SplatScalar))
                {
                    unreached();
                }

                op2 = m_compiler->gtCloneExpr(node->Operands[1]->Lanes[0]);
            }

            return m_compiler->gtNewSimdBinOpNode(node->Oper, simdType, op1, op2, node->ElementType, simdSize);
        }

        case PackKind::TernaryOp:
            assert(node->IntrinsicName == NI_System_Math_FusedMultiplyAdd);
            return m_compiler->gtNewSimdFmaNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                BuildPackNode(plan, node->Operands[1]),
                                                BuildPackNode(plan, node->Operands[2]), node->ElementType, simdSize);

        case PackKind::CompareOp:
            return m_compiler->gtNewSimdCmpOpNode(node->Oper, simdType, BuildPackNode(plan, node->Operands[0]),
                                                  BuildPackNode(plan, node->Operands[1]), node->ElementType, simdSize);

        case PackKind::Select:
            return m_compiler->gtNewSimdCndSelNode(simdType, BuildPackNode(plan, node->Operands[0]),
                                                   BuildPackNode(plan, node->Operands[1]),
                                                   BuildPackNode(plan, node->Operands[2]), node->ElementType, simdSize);

        default:
            unreached();
    }
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildVectorStore(LoopVectorizationPlan* plan, PackNode* node)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    assert(node->Kind == PackKind::StoreContiguous);

    const unsigned simdSize     = plan->VectorSizeBytes;
    GenTree* const storeAddress = BuildAddress(plan, plan->StoreAccesses[node->AccessIndex]);
    GenTree* const vectorResult = BuildPackNode(plan, node->Operands[0]);
    return m_compiler->gtNewSimdStoreNode(storeAddress, vectorResult, node->ElementType, simdSize);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildReductionInit(LoopVectorizationPlan* plan, unsigned reductionIndex)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[reductionIndex];
    const var_types                             simdType  = Compiler::getSIMDTypeForSize(plan->VectorSizeBytes);
    GenTree* initValue;
    if (reduction.Oper == GT_INTRINSIC)
    {
        LclVarDsc* const reductionDsc = m_compiler->lvaGetDesc(reduction.Lcl);
        initValue = m_compiler->gtNewSimdCreateBroadcastNode(
            simdType, m_compiler->gtNewLclvNode(reduction.Lcl, reductionDsc->TypeGet()), plan->ElementType,
            plan->VectorSizeBytes);
    }
    else
    {
        initValue = m_compiler->gtNewZeroConNode(simdType);
    }

    return m_compiler->gtNewStoreLclVarNode(reduction.VectorLcl, initValue);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildReductionUpdate(LoopVectorizationPlan* plan, unsigned reductionIndex)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const LoopVectorizationPlan::ReductionInfo& reduction = plan->Reductions[reductionIndex];
    const unsigned                              simdSize  = plan->VectorSizeBytes;
    const var_types                             simdType  = Compiler::getSIMDTypeForSize(simdSize);
    GenTree* const                              current = m_compiler->gtNewLclvNode(reduction.VectorLcl, simdType);
    GenTree* const                              value   = BuildPackNode(plan, reduction.Pack);
    GenTree* const                              result  = BuildVectorReductionOp(plan, reduction, current, value);
    return m_compiler->gtNewStoreLclVarNode(reduction.VectorLcl, result);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildReductionFinalize(LoopVectorizationPlan* plan, unsigned reductionIndex)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const LoopVectorizationPlan::ReductionInfo& reduction   = plan->Reductions[reductionIndex];
    const unsigned                              simdSize    = plan->VectorSizeBytes;
    const var_types                             simdType    = Compiler::getSIMDTypeForSize(simdSize);
    LclVarDsc* const                            reductionDsc = m_compiler->lvaGetDesc(reduction.Lcl);

    if (reduction.Oper == GT_INTRINSIC)
    {
        GenTree* scalar = m_compiler->gtNewSimdToScalarNode(
            reductionDsc->TypeGet(), m_compiler->gtNewLclvNode(reduction.VectorLcl, simdType), plan->ElementType,
            simdSize);

        for (unsigned lane = 1; lane < plan->VectorizationFactor; lane++)
        {
            GenTree* const laneValue =
                m_compiler->gtNewSimdGetElementNode(reductionDsc->TypeGet(),
                                                    m_compiler->gtNewLclvNode(reduction.VectorLcl, simdType),
                                                    m_compiler->gtNewIconNode(lane), plan->ElementType, simdSize);
            scalar = BuildReductionIntrinsicNode(reduction, scalar, laneValue, reductionDsc->TypeGet());
        }

        return m_compiler->gtNewStoreLclVarNode(reduction.Lcl, scalar);
    }

    GenTree* const current = m_compiler->gtNewLclvNode(reduction.Lcl, reductionDsc->TypeGet());
    GenTree* const vector  = m_compiler->gtNewLclvNode(reduction.VectorLcl, simdType);
    GenTree* const scalar  = m_compiler->gtNewSimdSumNode(plan->ElementType, vector, plan->ElementType, simdSize);
    GenTree* const sum     = m_compiler->gtNewOperNode(reduction.Oper, reductionDsc->TypeGet(), current, scalar);
    return m_compiler->gtNewStoreLclVarNode(reduction.Lcl, sum);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildReductionIntrinsicNode(const LoopVectorizationPlan::ReductionInfo& reduction,
                                                     GenTree*                                    op1,
                                                     GenTree*                                    op2,
                                                     var_types                                   resultType)
{
    assert(IsReductionMinMaxIntrinsic(reduction.Intrinsic));
    return new (m_compiler, GT_INTRINSIC)
        GenTreeIntrinsic(resultType, op1, op2, reduction.Intrinsic, nullptr R2RARG(CORINFO_CONST_LOOKUP{IAT_VALUE}));
}

GenTree* AutoVectorizer::BuildVectorReductionOp(LoopVectorizationPlan*                     plan,
                                                const LoopVectorizationPlan::ReductionInfo& reduction,
                                                GenTree*                                    op1,
                                                GenTree*                                    op2)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const var_types simdType = Compiler::getSIMDTypeForSize(plan->VectorSizeBytes);
    if (reduction.Oper != GT_INTRINSIC)
    {
        return m_compiler->gtNewSimdBinOpNode(GT_ADD, simdType, op1, op2, plan->ElementType, plan->VectorSizeBytes);
    }

    const bool isMax = (reduction.Intrinsic == NI_System_Math_Max) ||
                       (reduction.Intrinsic == NI_System_Math_MaxNative) ||
                       (reduction.Intrinsic == NI_System_Math_MaxUnsigned) ||
                       (reduction.Intrinsic == NI_System_Math_MaxMagnitude) ||
                       (reduction.Intrinsic == NI_System_Math_MaxMagnitudeNumber) ||
                       (reduction.Intrinsic == NI_System_Math_MaxNumber);
    const bool isMagnitude = (reduction.Intrinsic == NI_System_Math_MinMagnitude) ||
                             (reduction.Intrinsic == NI_System_Math_MaxMagnitude) ||
                             (reduction.Intrinsic == NI_System_Math_MinMagnitudeNumber) ||
                             (reduction.Intrinsic == NI_System_Math_MaxMagnitudeNumber);
    const bool isNumber = (reduction.Intrinsic == NI_System_Math_MinNumber) ||
                          (reduction.Intrinsic == NI_System_Math_MaxNumber) ||
                          (reduction.Intrinsic == NI_System_Math_MinMagnitudeNumber) ||
                          (reduction.Intrinsic == NI_System_Math_MaxMagnitudeNumber);
    if ((reduction.Intrinsic == NI_System_Math_MinNative) || (reduction.Intrinsic == NI_System_Math_MaxNative))
    {
        return m_compiler->gtNewSimdMinMaxNativeNode(simdType, op1, op2, plan->ElementType, plan->VectorSizeBytes,
                                                     isMax);
    }

    return m_compiler->gtNewSimdMinMaxNode(simdType, op1, op2, plan->ElementType, plan->VectorSizeBytes, isMax,
                                           isMagnitude, isNumber);
#else
    unreached();
#endif
}

bool AutoVectorizer::IsReductionMinMaxIntrinsic(NamedIntrinsic intrinsic) const
{
    switch (intrinsic)
    {
        case NI_System_Math_Min:
        case NI_System_Math_Max:
        case NI_System_Math_MinNative:
        case NI_System_Math_MaxNative:
        case NI_System_Math_MinUnsigned:
        case NI_System_Math_MaxUnsigned:
        case NI_System_Math_MinMagnitude:
        case NI_System_Math_MaxMagnitude:
        case NI_System_Math_MinMagnitudeNumber:
        case NI_System_Math_MaxMagnitudeNumber:
        case NI_System_Math_MinNumber:
        case NI_System_Math_MaxNumber:
            return true;

        default:
            return false;
    }
}

GenTree* AutoVectorizer::BuildIVUpdate(LoopVectorizationPlan* plan)
{
    LclVarDsc* const ivDsc = m_compiler->lvaGetDesc(plan->InductionVar);
    GenTree* const   iv    = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());
    GenTree* const   update =
        m_compiler->gtNewOperNode(plan->Step < 0 ? GT_SUB : GT_ADD, ivDsc->TypeGet(), iv,
                                  m_compiler->gtNewIconNode(plan->VectorizationFactor, ivDsc->TypeGet()));
    return m_compiler->gtNewStoreLclVarNode(plan->InductionVar, update);
}

GenTree* AutoVectorizer::BuildAddressUpdate(LoopVectorizationPlan* plan, unsigned addressVar)
{
    LclVarDsc* const addressDsc   = m_compiler->lvaGetDesc(addressVar);
    GenTree* const   address      = m_compiler->gtNewLclvNode(addressVar, addressDsc->TypeGet());
    int              elementDelta = static_cast<int>(plan->ElementSize);
    for (unsigned i = 0; i < plan->AddressUpdateCount; i++)
    {
        if (plan->AddressUpdateVars[i] == addressVar)
        {
            elementDelta = plan->AddressUpdateDeltas[i];
            break;
        }
    }

    const int vectorDelta =
        (elementDelta < 0) ? -static_cast<int>(plan->VectorSizeBytes) : static_cast<int>(plan->VectorSizeBytes);
    GenTree* const add = m_compiler->gtNewOperNode(GT_ADD, addressDsc->TypeGet(), address,
                                                   m_compiler->gtNewIconNode(vectorDelta, TYP_I_IMPL));
    return m_compiler->gtNewStoreLclVarNode(addressVar, add);
}

GenTree* AutoVectorizer::BuildTripCountUpdate(LoopVectorizationPlan* plan, int delta)
{
    LclVarDsc* const tripCountDsc = m_compiler->lvaGetDesc(plan->TripCountVar);
    GenTree* const   tripCount    = m_compiler->gtNewLclvNode(plan->TripCountVar, tripCountDsc->TypeGet());
    GenTree* const   add          = m_compiler->gtNewOperNode(GT_ADD, tripCountDsc->TypeGet(), tripCount,
                                                              m_compiler->gtNewIconNode(delta, tripCountDsc->TypeGet()));
    return m_compiler->gtNewStoreLclVarNode(plan->TripCountVar, add);
}

GenTree* AutoVectorizer::BuildScalarRemainderTest(LoopVectorizationPlan* plan)
{
    GenTree* cmp;
    if (plan->IsPostIV)
    {
        LclVarDsc* const tripCountDsc = m_compiler->lvaGetDesc(plan->TripCountVar);
        GenTree* const   tripCount    = m_compiler->gtNewLclvNode(plan->TripCountVar, tripCountDsc->TypeGet());
        cmp =
            m_compiler->gtNewOperNode(GT_EQ, TYP_INT, tripCount, m_compiler->gtNewIconNode(0, tripCountDsc->TypeGet()));
    }
    else
    {
        LclVarDsc* const ivDsc = m_compiler->lvaGetDesc(plan->InductionVar);
        GenTree* const   iv    = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());
        GenTree* const   end   = m_compiler->gtCloneExpr(plan->End);
        genTreeOps       op;
        switch (plan->TestOper)
        {
            case GT_LT:
                op = GT_GE;
                break;
            case GT_LE:
                op = GT_GT;
                break;
            case GT_GT:
                op = GT_LE;
                break;
            case GT_GE:
                op = GT_LT;
                break;
            case GT_NE:
                op = GT_EQ;
                break;
            default:
                unreached();
        }

        cmp = m_compiler->gtNewOperNode(op, TYP_INT, iv, end);
    }

    if (!plan->IsPostIV && (cmp->gtGetOp1()->TypeGet() != cmp->gtGetOp2()->TypeGet()))
    {
        cmp->AsOp()->gtOp1 = m_compiler->gtNewCastNode(TYP_LONG, cmp->gtGetOp1(), false, TYP_LONG);
        cmp->AsOp()->gtOp2 = m_compiler->gtNewCastNode(TYP_LONG, cmp->gtGetOp2(), false, TYP_LONG);
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

bool AutoVectorizer::TryAnalyzeIndirAccess(LoopVectorizationPlan*               plan,
                                           Statement*                           stmt,
                                           GenTree*                             indir,
                                           bool                                 isStore,
                                           unsigned                             ivLcl,
                                           LoopVectorizationPlan::ScalarAccess* access)
{
    assert(indir->OperIs(GT_IND, GT_STOREIND));

    GenTreeIndir* const indirNode = indir->AsIndir();
    if (indirNode->IsVolatile())
    {
        return false;
    }

    if (plan->IsPostIV)
    {
        if (!TryAnalyzePostIVAddress(stmt, indirNode->Addr(), access))
        {
            return false;
        }

        if (access->ElementType == TYP_UNDEF)
        {
            access->ElementType = indir->TypeGet();
            access->ElementSize = genTypeSize(access->ElementType);
        }

        access->IsLoad     = !isStore;
        access->IsStore    = isStore;
        access->IsVolatile = false;
        return IsSupportedElementType(access->ElementType);
    }

    if (TryAnalyzeArrayAccess(plan, stmt, indir, isStore, ivLcl, access))
    {
        return true;
    }

    GenTree* const  addr        = UnwrapCommaValue(indirNode->Addr());
    const var_types elementType = indir->TypeGet();
    if (!TryAnalyzeByrefAddress(plan, addr, ivLcl, elementType, access))
    {
        return false;
    }

    access->StatementRoot = stmt;
    access->Address       = addr;
    access->ElementType   = elementType;
    access->ElementSize   = genTypeSize(elementType);
    access->IsLoad        = !isStore;
    access->IsStore       = isStore;
    access->IsVolatile    = false;

    return IsSupportedElementType(access->ElementType);
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

bool AutoVectorizer::TryAnalyzeByrefAddress(LoopVectorizationPlan*               plan,
                                            GenTree*                             addr,
                                            unsigned                             ivLcl,
                                            var_types                            elementType,
                                            LoopVectorizationPlan::ScalarAccess* access)
{
    struct AddressParts
    {
        unsigned BaseLcl      = BAD_VAR_NUM;
        unsigned InvariantLcl = BAD_VAR_NUM;
        ssize_t  Offset       = 0;
        ssize_t  IndexScale   = 0;
    };

    const unsigned elemSize = genTypeSize(elementType);
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

            if (tree->OperIs(GT_ADD))
            {
                return Analyze(tree->AsOp()->gtOp1) && Analyze(tree->AsOp()->gtOp2);
            }

            if (tree->IsCnsIntOrI())
            {
                m_parts->Offset += tree->AsIntConCommon()->IconValue();
                return true;
            }

            if (tree->OperIs(GT_LCL_VAR) && tree->TypeIs(TYP_BYREF))
            {
                const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
                if ((m_parts->BaseLcl != BAD_VAR_NUM) && (m_parts->BaseLcl != lclNum))
                {
                    return false;
                }

                m_parts->BaseLcl = lclNum;
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

                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, index, m_ivLcl, &indexOffset, &invariantLcl, &sawIv) ||
                    !sawIv)
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                if (invariantLcl != BAD_VAR_NUM)
                {
                    if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                    {
                        return false;
                    }

                    m_parts->InvariantLcl = invariantLcl;
                }
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

                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree->AsOp()->gtOp1, m_ivLcl, &indexOffset,
                                                       &invariantLcl, &sawIv) ||
                    !sawIv)
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                if (invariantLcl != BAD_VAR_NUM)
                {
                    if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                    {
                        return false;
                    }

                    m_parts->InvariantLcl = invariantLcl;
                }
                return true;
            }

            if (m_elemSize == 1)
            {
                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree, m_ivLcl, &indexOffset, &invariantLcl, &sawIv) &&
                    sawIv)
                {
                    m_parts->IndexScale += 1;
                    m_parts->Offset += indexOffset;
                    if (invariantLcl != BAD_VAR_NUM)
                    {
                        if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                        {
                            return false;
                        }

                        m_parts->InvariantLcl = invariantLcl;
                    }
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
    if (!visitor.Analyze(addr) || (parts.BaseLcl == BAD_VAR_NUM) || (parts.IndexScale != elemSize))
    {
        return false;
    }

    if ((parts.Offset % static_cast<ssize_t>(elemSize)) != 0)
    {
        return false;
    }

    access->BaseLocalIfKnown    = parts.BaseLcl;
    access->InvariantIndexLocal = parts.InvariantLcl;
    access->IndexOffset         = static_cast<int>(parts.Offset / static_cast<ssize_t>(elemSize));
    access->ElementSize         = elemSize;
    access->ElementType         = elementType;
    access->IsByrefWithIndex    = true;
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

bool AutoVectorizer::TryAnalyzeIndexExpr(LoopVectorizationPlan* plan,
                                         GenTree*               tree,
                                         unsigned               ivLcl,
                                         int*                   offset,
                                         unsigned*              invariantLcl,
                                         bool*                  sawIv,
                                         unsigned               depth)
{
    if (depth > MaxPackDepth)
    {
        return false;
    }

    bool localSawIv = false;
    if (sawIv == nullptr)
    {
        sawIv = &localSawIv;
    }

    tree = UnwrapCommaValue(tree);

    if (tree->OperIs(GT_CAST))
    {
        return TryAnalyzeIndexExpr(plan, tree->AsCast()->CastOp(), ivLcl, offset, invariantLcl, sawIv, depth + 1);
    }

    if (tree->OperIs(GT_LCL_VAR) && (tree->AsLclVarCommon()->GetLclNum() == ivLcl))
    {
        if (*sawIv)
        {
            return false;
        }

        *sawIv = true;
        return true;
    }

    if (tree->OperIs(GT_LCL_VAR))
    {
        const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
        GenTree*       def    = nullptr;
        if (TryGetLocalDef(plan, lclNum, &def))
        {
            return TryAnalyzeIndexExpr(plan, def, ivLcl, offset, invariantLcl, sawIv, depth + 1);
        }

        if ((invariantLcl != nullptr) && tree->TypeIs(TYP_INT, TYP_UINT, TYP_I_IMPL, TYP_LONG, TYP_ULONG) &&
            !plan->Loop->HasDef(lclNum))
        {
            if ((*invariantLcl != BAD_VAR_NUM) && (*invariantLcl != lclNum))
            {
                return false;
            }

            *invariantLcl = lclNum;
            return true;
        }
    }

    if (tree->OperIs(GT_ADD))
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op1->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(plan, op2, ivLcl, offset, invariantLcl, sawIv, depth + 1);
        }

        if (op2->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op2->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(plan, op1, ivLcl, offset, invariantLcl, sawIv, depth + 1);
        }

        return TryAnalyzeIndexExpr(plan, op1, ivLcl, offset, invariantLcl, sawIv, depth + 1) &&
               TryAnalyzeIndexExpr(plan, op2, ivLcl, offset, invariantLcl, sawIv, depth + 1);
    }

    if (tree->OperIs(GT_SUB))
    {
        GenTree* const op1 = tree->AsOp()->gtOp1;
        GenTree* const op2 = tree->AsOp()->gtOp2;
        if (op2->IsCnsIntOrI())
        {
            *offset -= static_cast<int>(op2->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(plan, op1, ivLcl, offset, invariantLcl, sawIv, depth + 1);
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
        unsigned ArrayLcl     = BAD_VAR_NUM;
        unsigned InvariantLcl = BAD_VAR_NUM;
        ssize_t  Offset       = 0;
        ssize_t  IndexScale   = 0;
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

                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, index, m_ivLcl, &indexOffset, &invariantLcl, &sawIv) ||
                    !sawIv)
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                if (invariantLcl != BAD_VAR_NUM)
                {
                    if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                    {
                        return false;
                    }

                    m_parts->InvariantLcl = invariantLcl;
                }
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

                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree->AsOp()->gtOp1, m_ivLcl, &indexOffset,
                                                       &invariantLcl, &sawIv) ||
                    !sawIv)
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                if (invariantLcl != BAD_VAR_NUM)
                {
                    if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                    {
                        return false;
                    }

                    m_parts->InvariantLcl = invariantLcl;
                }
                return true;
            }

            if (m_elemSize == 1)
            {
                int      indexOffset  = 0;
                unsigned invariantLcl = BAD_VAR_NUM;
                bool     sawIv        = false;
                if (m_vectorizer->TryAnalyzeIndexExpr(m_plan, tree, m_ivLcl, &indexOffset, &invariantLcl, &sawIv) &&
                    sawIv)
                {
                    m_parts->IndexScale += 1;
                    m_parts->Offset += indexOffset;
                    if (invariantLcl != BAD_VAR_NUM)
                    {
                        if ((m_parts->InvariantLcl != BAD_VAR_NUM) && (m_parts->InvariantLcl != invariantLcl))
                        {
                            return false;
                        }

                        m_parts->InvariantLcl = invariantLcl;
                    }
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

    access->BaseLocalIfKnown    = parts.ArrayLcl;
    access->InvariantIndexLocal = parts.InvariantLcl;
    access->IndexOffset         = static_cast<int>(offsetBytes / static_cast<ssize_t>(elemSize));
    access->ElementSize         = elemSize;
    access->ElementType         = arrAddr->GetElemType();
    access->IsArray             = true;
    return true;
}

bool AutoVectorizer::IsSameLimit(LoopVectorizationPlan* plan, GenTree* first, GenTree* second, unsigned depth)
{
    if (depth > MaxPackDepth)
    {
        return false;
    }

    first  = UnwrapCommaValue(first);
    second = UnwrapCommaValue(second);

    if (GenTree::Compare(first, second))
    {
        return true;
    }

    if (first->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (TryGetLocalDef(plan, first->AsLclVarCommon()->GetLclNum(), &def))
        {
            return IsSameLimit(plan, def, second, depth + 1);
        }
    }

    if (second->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (TryGetLocalDef(plan, second->AsLclVarCommon()->GetLclNum(), &def))
        {
            return IsSameLimit(plan, first, def, depth + 1);
        }
    }

    return false;
}

bool AutoVectorizer::TryProveRemainingBoundsChecks(LoopVectorizationPlan* plan)
{
    if (plan->IsPostIV || (plan->End == nullptr))
    {
        return false;
    }

    bool failed = false;

    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, AutoVectorizer* vectorizer, LoopVectorizationPlan* plan, bool* failed)
            : GenTreeVisitor<Visitor>(compiler)
            , m_vectorizer(vectorizer)
            , m_plan(plan)
            , m_failed(failed)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;
            if (!tree->OperIs(GT_BOUNDS_CHECK))
            {
                return fgWalkResult::WALK_CONTINUE;
            }

            GenTreeBoundsChk* const boundsCheck = tree->AsBoundsChk();
            int                     indexOffset = 0;
            bool                    sawIv       = false;
            if (!m_vectorizer->TryAnalyzeIndexExpr(m_plan, boundsCheck->GetIndex(), m_plan->InductionVar, &indexOffset,
                                                   nullptr, &sawIv) ||
                !sawIv)
            {
                JITDUMPEXEC(m_compiler->gtDispTree(tree));
                JITDUMP("bounds-check index is not the loop IV, bail out\n");
                *m_failed = true;
                return fgWalkResult::WALK_ABORT;
            }

            if (indexOffset != 0)
            {
                JITDUMPEXEC(m_compiler->gtDispTree(tree));
                JITDUMP("byref/span bounds-check index offset %d is not supported, bail out\n", indexOffset);
                *m_failed = true;
                return fgWalkResult::WALK_ABORT;
            }

            if (!m_vectorizer->IsSameLimit(m_plan, boundsCheck->GetArrayLength(), m_plan->End))
            {
                JITDUMPEXEC(m_compiler->gtDispTree(tree));
                JITDUMP("bounds-check length does not match loop limit, bail out\n");
                *m_failed = true;
                return fgWalkResult::WALK_ABORT;
            }

            return fgWalkResult::WALK_CONTINUE;
        }

    private:
        AutoVectorizer*        m_vectorizer;
        LoopVectorizationPlan* m_plan;
        bool*                  m_failed;
    };

    plan->Loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* root = stmt->GetRootNode();
            Visitor  visitor(m_compiler, this, plan, &failed);
            visitor.WalkTree(&root, nullptr);
            if (failed)
            {
                return BasicBlockVisit::Abort;
            }
        }

        return BasicBlockVisit::Continue;
    });

    return !failed;
}

void AutoVectorizer::RecordLocalDefs(LoopVectorizationPlan* plan, GenTree* tree, bool* foundBoundsCheck)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, AutoVectorizer* vectorizer, LoopVectorizationPlan* plan, bool* foundBoundsCheck)
            : GenTreeVisitor<Visitor>(compiler)
            , m_vectorizer(vectorizer)
            , m_plan(plan)
            , m_foundBoundsCheck(foundBoundsCheck)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const tree = *use;
            if ((m_foundBoundsCheck != nullptr) && tree->OperIs(GT_BOUNDS_CHECK))
            {
                *m_foundBoundsCheck = true;
            }

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
        bool*                  m_foundBoundsCheck;
    };

    Visitor visitor(m_compiler, this, plan, foundBoundsCheck);
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

bool AutoVectorizer::TryCollectArrayLengthLimitLocals(LoopVectorizationPlan* plan,
                                                      GenTree*               tree,
                                                      unsigned*              lclNums,
                                                      int*                   offsets,
                                                      unsigned               maxCount,
                                                      unsigned*              count,
                                                      unsigned               depth)
{
    if (depth > MaxPackDepth)
    {
        return false;
    }

    tree = UnwrapCommaValue(tree);

    if (tree->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (!TryGetLocalDef(plan, tree->AsLclVarCommon()->GetLclNum(), &def))
        {
            return false;
        }

        return TryCollectArrayLengthLimitLocals(plan, def, lclNums, offsets, maxCount, count, depth + 1);
    }

    unsigned lclNum = BAD_VAR_NUM;
    int      offset = 0;
    if (TryGetArrayLengthLimitLocal(plan, tree, &lclNum, &offset, depth + 1))
    {
        for (unsigned i = 0; i < *count; i++)
        {
            if (lclNums[i] == lclNum)
            {
                offsets[i] = std::min(offsets[i], offset);
                return true;
            }
        }

        if (*count >= maxCount)
        {
            return false;
        }

        lclNums[*count] = lclNum;
        offsets[*count] = offset;
        (*count)++;
        return true;
    }

    if (tree->OperIs(GT_SELECT))
    {
        GenTreeConditional* const select = tree->AsConditional();
        GenTree* const            cond   = UnwrapCommaValue(select->gtCond);
        GenTree* const            thenOp = UnwrapCommaValue(select->gtOp1);
        GenTree* const            elseOp = UnwrapCommaValue(select->gtOp2);

        if (cond->OperIsCompare())
        {
            GenTree* const cmpOp1 = UnwrapCommaValue(cond->AsOp()->gtOp1);
            GenTree* const cmpOp2 = UnwrapCommaValue(cond->AsOp()->gtOp2);

            const bool isMinSelect = (GenTree::StaticOperIs(cond->OperGet(), GT_LT, GT_LE) &&
                                      GenTree::Compare(cmpOp1, thenOp) && GenTree::Compare(cmpOp2, elseOp)) ||
                                     (GenTree::StaticOperIs(cond->OperGet(), GT_GT, GT_GE) &&
                                      GenTree::Compare(cmpOp2, thenOp) && GenTree::Compare(cmpOp1, elseOp));

            if (isMinSelect)
            {
                return TryCollectArrayLengthLimitLocals(plan, thenOp, lclNums, offsets, maxCount, count, depth + 1) &&
                       TryCollectArrayLengthLimitLocals(plan, elseOp, lclNums, offsets, maxCount, count, depth + 1);
            }
        }

        return false;
    }

    if (!tree->OperIs(GT_INTRINSIC))
    {
        return false;
    }

    GenTreeIntrinsic* const intrinsic = tree->AsIntrinsic();
    switch (intrinsic->gtIntrinsicName)
    {
        case NI_System_Math_Min:
        case NI_System_Math_MinNative:
        case NI_System_Math_MinUnsigned:
            return TryCollectArrayLengthLimitLocals(plan, intrinsic->gtGetOp1(), lclNums, offsets, maxCount, count,
                                                    depth + 1) &&
                   TryCollectArrayLengthLimitLocals(plan, intrinsic->gtGetOp2(), lclNums, offsets, maxCount, count,
                                                    depth + 1);

        default:
            return false;
    }
}

bool AutoVectorizer::TryGetArrayLengthLimitLocal(
    LoopVectorizationPlan* plan, GenTree* tree, unsigned* lclNum, int* offset, unsigned depth)
{
    if (depth > MaxPackDepth)
    {
        return false;
    }

    tree = UnwrapCommaValue(tree);

    if (tree->OperIs(GT_LCL_VAR))
    {
        GenTree* def = nullptr;
        if (!TryGetLocalDef(plan, tree->AsLclVarCommon()->GetLclNum(), &def))
        {
            return false;
        }

        return TryGetArrayLengthLimitLocal(plan, def, lclNum, offset, depth + 1);
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
            return abs(plan->AddressUpdateDeltas[i]) == static_cast<int>(plan->ElementSize);
        }
    }

    return false;
}

PhaseStatus Compiler::optAutoVectorize()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.Run();
}
