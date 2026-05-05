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
            Dump("AutoVec: accepted loop " FMT_LP ", IV V%02u, VF=%u, vectorSize=%u\n", loop->GetIndex(),
                 plan.IsPostIV ? plan.TripCountVar : plan.InductionVar, plan.VectorizationFactor,
                 plan.VectorSizeBytes);
            DumpSLPPlan(plan);
        }
    }

    Dump("AutoVec: analysis found %u candidate loop%s\n", candidateCount, (candidateCount == 1) ? "" : "s");
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
        Dump("AutoVec: disabled for MinOpts/debuggable code\n");
        return false;
    }

#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned vectorSizeBytes = GetVectorSizeBytes(TYP_INT);
    if (vectorSizeBytes < 16)
    {
        Dump("AutoVec: target vector size %u is too small\n", vectorSizeBytes);
        return false;
    }

    return true;
#else
    Dump("AutoVec: unsupported target\n");
    return false;
#endif
}

unsigned AutoVectorizer::GetVectorSizeBytes(var_types elementType) const
{
    if (elementType != TYP_INT)
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
        Dump("AutoVec: no loops for rewrite\n");
        return false;
    }

    return true;
}

bool AutoVectorizer::TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    Dump("AutoVec: considering loop " FMT_LP "\n", loop->GetIndex());

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
        Reject(loop, "nested loop");
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

    NaturalLoopIterInfo iterInfo;
    if (!loop->AnalyzeIteration(&iterInfo))
    {
        return TryCreatePostIVLoopPlan(loop, plan);
    }

    if (!iterInfo.HasConstInit || (iterInfo.ConstInitValue != 0))
    {
        Reject(loop, "IV init is not zero");
        return false;
    }

    if (!iterInfo.IsIncreasingLoop() || (iterInfo.IterOper() != GT_ADD) || (iterInfo.IterConst() != 1))
    {
        Reject(loop, "IV is not incremented by one");
        return false;
    }

    const genTreeOps testOper = iterInfo.TestOper();
    if (!GenTree::StaticOperIs(testOper, GT_LT, GT_LE))
    {
        Reject(loop, "unsupported loop test");
        return false;
    }

    if (!iterInfo.HasArrayLengthLimit && !iterInfo.HasInvariantLocalLimit)
    {
        Reject(loop, "limit is not array length or invariant local");
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
#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = GetVectorSizeBytes(TYP_INT);
    plan->VectorizationFactor = plan->VectorSizeBytes / genTypeSize(TYP_INT);
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(loop, "vectorization factor is too small");
        return false;
    }

    Dump("AutoVec: loop " FMT_LP " canonical IV V%02u, init=0, step=1, test=%s\n", loop->GetIndex(),
         iterInfo.IterVar, GenTree::OpName(testOper));
    return TryAnalyzeMemory(plan) && TryBuildSLPPlan(plan);
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
    if (!relop->OperIs(GT_NE))
    {
        Reject(loop, "post-IV loop test is not a remaining-count test");
        return false;
    }

    GenTree* op1 = relop->AsOp()->gtOp1;
    GenTree* op2 = relop->AsOp()->gtOp2;
    if (op1->IsIntegralConst(0))
    {
        std::swap(op1, op2);
    }

    if (!op1->OperIs(GT_LCL_VAR) || !op2->IsIntegralConst(0))
    {
        Reject(loop, "post-IV loop test does not compare local to zero");
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
    plan->TestOper     = GT_NE;
    plan->Step         = -1;
#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = GetVectorSizeBytes(TYP_INT);
    plan->VectorizationFactor = plan->VectorSizeBytes / genTypeSize(TYP_INT);
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(loop, "vectorization factor is too small");
        return false;
    }

    Dump("AutoVec: loop " FMT_LP " post-IV trip count V%02u\n", loop->GetIndex(), plan->TripCountVar);
    return TryAnalyzePostIVMemory(plan) && TryBuildSLPPlan(plan);
}

void AutoVectorizer::Reject(FlowGraphNaturalLoop* loop, const char* reason) const
{
    Dump("AutoVec: rejected loop " FMT_LP ": %s\n", loop->GetIndex(), reason);
}

bool AutoVectorizer::TryAnalyzeMemory(LoopVectorizationPlan* plan)
{
    bool foundStore = false;
    bool failed     = false;

    plan->Loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* const root = stmt->GetRootNode();

            if (root == plan->IterTree)
            {
                continue;
            }

            if (root->OperIs(GT_JTRUE) && (root->gtGetOp1() == plan->TestTree))
            {
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

            if ((root->gtFlags & (GTF_CALL | GTF_ORDER_SIDEEFF)) != 0)
            {
                failed = true;
                Reject(plan->Loop, "store statement has unsupported side effects");
                return BasicBlockVisit::Abort;
            }

            if (ContainsOper(root, GT_BOUNDS_CHECK))
            {
                failed = true;
                Reject(plan->Loop, "remaining bounds check");
                return BasicBlockVisit::Abort;
            }

            GenTreeStoreInd* const store = root->AsStoreInd();
            if (!TryAnalyzeArrayAccess(stmt, store, true, plan->InductionVar, &plan->StoreAccess))
            {
                failed = true;
                Reject(plan->Loop, "unsupported store access");
                return BasicBlockVisit::Abort;
            }

            if (!store->Data()->OperIs(GT_ADD))
            {
                failed = true;
                Reject(plan->Loop, "store data is not an add");
                return BasicBlockVisit::Abort;
            }

            GenTreeOp* const add  = store->Data()->AsOp();
            GenTree*         load = nullptr;
            GenTree*         scalar = nullptr;
            bool             scalarIsRhs = true;

            if (add->gtOp1->OperIs(GT_IND))
            {
                load        = add->gtOp1;
                scalar      = add->gtOp2;
                scalarIsRhs = true;
            }
            else if (add->gtOp2->OperIs(GT_IND))
            {
                load        = add->gtOp2;
                scalar      = add->gtOp1;
                scalarIsRhs = false;
            }
            else
            {
                failed = true;
                Reject(plan->Loop, "add does not contain an array load");
                return BasicBlockVisit::Abort;
            }

            if (!TryAnalyzeArrayAccess(stmt, load, false, plan->InductionVar, &plan->LoadAccess))
            {
                failed = true;
                Reject(plan->Loop, "unsupported load access");
                return BasicBlockVisit::Abort;
            }

            if (!TryGetInvariantInt(plan->Loop, plan->InductionVar, scalar))
            {
                failed = true;
                Reject(plan->Loop, "add operand is not loop invariant int");
                return BasicBlockVisit::Abort;
            }

            if ((plan->StoreAccess.ElementType != TYP_INT) || (plan->LoadAccess.ElementType != TYP_INT))
            {
                failed = true;
                Reject(plan->Loop, "element type is not int");
                return BasicBlockVisit::Abort;
            }

            if (plan->StoreAccess.BaseLocalIfKnown != plan->LoadAccess.BaseLocalIfKnown)
            {
                failed = true;
                Reject(plan->Loop, "load/store base mismatch");
                return BasicBlockVisit::Abort;
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

            unsigned lengthLcl = BAD_VAR_NUM;
            if (!TryGetArrayLengthLocal(plan->End, &lengthLcl) || (lengthLcl != plan->StoreAccess.BaseLocalIfKnown))
            {
                failed = true;
                Reject(plan->Loop, "loop limit is not the accessed array length");
                return BasicBlockVisit::Abort;
            }

            plan->StoreStmt          = stmt;
            plan->ScalarOperand      = scalar;
            plan->ScalarOper         = GT_ADD;
            plan->ScalarOperandIsRhs = scalarIsRhs;
            foundStore               = true;
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

    Dump("AutoVec: found int[] store/load base V%02u, offset %d\n", plan->StoreAccess.BaseLocalIfKnown,
         plan->StoreAccess.IndexOffset);
    return true;
}

bool AutoVectorizer::TryAnalyzePostIVMemory(LoopVectorizationPlan* plan)
{
    assert(plan->IsPostIV);

    BasicBlock* const block = plan->Header;
    unsigned          tempVar = BAD_VAR_NUM;
    GenTree*          scalar = nullptr;
    bool              scalarIsRhs = true;
    bool              foundTempStore = false;
    bool              foundStore = false;
    bool              foundAddressUpdate = false;
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
            unsigned addressVar = BAD_VAR_NUM;
            if (!store->TypeIs(TYP_INT) || !TryAnalyzeByrefLocalAddress(store->Addr(), &addressVar) ||
                !store->Data()->OperIs(GT_LCL_VAR))
            {
                Reject(plan->Loop, "post-IV store is not a local-addressed int store");
                return false;
            }

            if ((plan->AddressVar != BAD_VAR_NUM) && (plan->AddressVar != addressVar))
            {
                Reject(plan->Loop, "post-IV store address does not match load address");
                return false;
            }

            plan->AddressVar = addressVar;
            tempVar          = store->Data()->AsLclVarCommon()->GetLclNum();

            plan->StoreStmt                    = stmt;
            plan->StoreAccess.StatementRoot    = stmt;
            plan->StoreAccess.Address          = store->Addr();
            plan->StoreAccess.BaseLocalIfKnown = addressVar;
            plan->StoreAccess.ElementSize      = genTypeSize(TYP_INT);
            plan->StoreAccess.ElementType      = TYP_INT;
            plan->StoreAccess.IsStore          = true;
            plan->StoreAccess.IsByrefLocal     = true;

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

            if (op2->IsIntegralConst(genTypeSize(TYP_INT)) && op1->OperIs(GT_LCL_VAR) &&
                (op1->AsLclVarCommon()->GetLclNum() == lclNum) && storeLcl->TypeIs(TYP_BYREF))
            {
                if ((plan->AddressVar != BAD_VAR_NUM) && (plan->AddressVar != lclNum))
                {
                    Reject(plan->Loop, "post-IV address update does not match store address");
                    return false;
                }

                plan->AddressVar = lclNum;
                foundAddressUpdate = true;
                continue;
            }
        }

        if (data->OperIs(GT_ADD))
        {
            GenTree* load = nullptr;
            scalar        = nullptr;
            scalarIsRhs   = true;

            if (data->AsOp()->gtOp1->OperIs(GT_COMMA))
            {
                GenTree* const commaValue = data->AsOp()->gtOp1->AsOp()->gtOp2;
                if (commaValue->OperIs(GT_IND))
                {
                    load        = commaValue;
                    scalar      = data->AsOp()->gtOp2;
                    scalarIsRhs = true;
                }
            }
            else if (data->AsOp()->gtOp1->OperIs(GT_IND))
            {
                load        = data->AsOp()->gtOp1;
                scalar      = data->AsOp()->gtOp2;
                scalarIsRhs = true;
            }
            else if (data->AsOp()->gtOp2->OperIs(GT_IND))
            {
                load        = data->AsOp()->gtOp2;
                scalar      = data->AsOp()->gtOp1;
                scalarIsRhs = false;
            }

            if (load != nullptr)
            {
                unsigned addressVar = BAD_VAR_NUM;
                if (!load->TypeIs(TYP_INT) || !TryAnalyzeByrefLocalAddress(load->AsIndir()->Addr(), &addressVar))
                {
                    Reject(plan->Loop, "post-IV load is not a local-addressed int load");
                    return false;
                }

                if (!TryGetInvariantInt(plan->Loop, plan->TripCountVar, scalar))
                {
                    Reject(plan->Loop, "post-IV scalar operand is not invariant int");
                    return false;
                }

                if ((plan->AddressVar != BAD_VAR_NUM) && (plan->AddressVar != addressVar))
                {
                    Reject(plan->Loop, "post-IV load address does not match store address");
                    return false;
                }

                plan->AddressVar                  = addressVar;
                plan->LoadAccess.StatementRoot    = stmt;
                plan->LoadAccess.Address          = load->AsIndir()->Addr();
                plan->LoadAccess.BaseLocalIfKnown = addressVar;
                plan->LoadAccess.ElementSize      = genTypeSize(TYP_INT);
                plan->LoadAccess.ElementType      = TYP_INT;
                plan->LoadAccess.IsLoad           = true;
                plan->LoadAccess.IsByrefLocal     = true;
                plan->ScalarOperand               = scalar;
                plan->ScalarOper                  = GT_ADD;
                plan->ScalarOperandIsRhs          = scalarIsRhs;

                tempVar = lclNum;
                foundTempStore = true;
                continue;
            }
        }

        Reject(plan->Loop, "unsupported post-IV statement");
        return false;
    }

    if (!foundTempStore || !foundStore || !foundAddressUpdate || !foundTripCountUpdate)
    {
        Reject(plan->Loop, "incomplete post-IV memory pattern");
        return false;
    }

    if (tempVar == BAD_VAR_NUM)
    {
        Reject(plan->Loop, "post-IV temporary store not found");
        return false;
    }

    bool foundAddressInit = false;
    bool foundTripCountInit = false;
    for (Statement* const stmt : plan->Preheader->Statements())
    {
        GenTree* const root = stmt->GetRootNode();
        if (!root->OperIs(GT_STORE_LCL_VAR))
        {
            continue;
        }

        const unsigned lclNum = root->AsLclVarCommon()->GetLclNum();
        foundAddressInit |= (lclNum == plan->AddressVar);
        foundTripCountInit |= (lclNum == plan->TripCountVar);
    }

    if (!foundAddressInit || !foundTripCountInit)
    {
        Reject(plan->Loop, "missing post-IV preheader initialization");
        return false;
    }

    Dump("AutoVec: found post-IV int byref loop address V%02u, trip count V%02u\n", plan->AddressVar,
         plan->TripCountVar);
    return true;
}

bool AutoVectorizer::TryBuildSLPPlan(LoopVectorizationPlan* plan)
{
    assert(plan->VectorizationFactor <= MaxLanes);

    SLPPlan* const slpPlan = &plan->BodyPlan;

    PackNode* const load = NewPackNode(slpPlan, PackKind::LoadContiguous, TYP_INT, plan->VectorizationFactor);
    if (load == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    load->Lanes[0] = plan->LoadAccess.Address;
    load->Cost     = 1;

    const PackKind splatKind = plan->ScalarOperand->IsCnsIntOrI() ? PackKind::SplatConstant : PackKind::SplatScalar;
    PackNode* const splat = NewPackNode(slpPlan, splatKind, TYP_INT, plan->VectorizationFactor);
    if (splat == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    splat->Lanes[0] = plan->ScalarOperand;
    splat->Cost     = 1;

    PackNode* const add = NewPackNode(slpPlan, PackKind::BinaryOp, TYP_INT, plan->VectorizationFactor);
    if (add == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    add->Oper        = plan->ScalarOper;
    add->Operands[0] = plan->ScalarOperandIsRhs ? load : splat;
    add->Operands[1] = plan->ScalarOperandIsRhs ? splat : load;
    add->Cost        = 1;

    PackNode* const store = NewPackNode(slpPlan, PackKind::StoreContiguous, TYP_INT, plan->VectorizationFactor);
    if (store == nullptr)
    {
        Reject(plan->Loop, "SLP node budget exceeded");
        return false;
    }
    store->Lanes[0]    = plan->StoreAccess.Address;
    store->Operands[0] = add;
    store->Cost        = 1;

    slpPlan->Root                = store;
    slpPlan->EstimatedScalarCost = plan->VectorizationFactor * 3;
    slpPlan->EstimatedVectorCost = load->Cost + splat->Cost + add->Cost + store->Cost;
    slpPlan->EstimatedCodeSizeDelta = 12;

    Dump("AutoVec: SLP accepted, estimated scalar=%u, vector=%u, codeSizeDelta=%u\n",
         slpPlan->EstimatedScalarCost, slpPlan->EstimatedVectorCost, slpPlan->EstimatedCodeSizeDelta);
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

    BasicBlock* const vectorCheck = m_compiler->fgNewBBbefore(BBJ_COND, plan->Header, false);
    BasicBlock* const vectorBody  = m_compiler->fgNewBBbefore(BBJ_ALWAYS, vectorCheck, false);
    BasicBlock* const scalarGuard = plan->IsPostIV ? m_compiler->fgNewBBbefore(BBJ_COND, plan->Header, false) : nullptr;

    vectorCheck->clearTryIndex();
    vectorCheck->clearHndIndex();
    vectorBody->clearTryIndex();
    vectorBody->clearHndIndex();
    if (scalarGuard != nullptr)
    {
        scalarGuard->clearTryIndex();
        scalarGuard->clearHndIndex();
    }

    vectorCheck->inheritWeight(plan->Preheader);
    vectorBody->inheritWeight(plan->Header);
    vectorBody->scaleBBWeight(1.0 / static_cast<weight_t>(plan->VectorizationFactor));
    if (scalarGuard != nullptr)
    {
        scalarGuard->inheritWeightPercentage(plan->Header, 25);
    }

    GenTree* const vectorTest = BuildVectorLoopTest(plan);
    Statement* const testStmt = m_compiler->fgNewStmtAtEnd(vectorCheck, vectorTest, plan->StoreStmt->GetDebugInfo());
    m_compiler->gtSetStmtInfo(testStmt);
    m_compiler->fgSetStmtSeq(testStmt);

    GenTree* const vectorStore = BuildVectorStore(plan);
    Statement* const storeStmt = m_compiler->fgNewStmtAtEnd(vectorBody, vectorStore, plan->StoreStmt->GetDebugInfo());
    m_compiler->gtSetStmtInfo(storeStmt);
    m_compiler->fgSetStmtSeq(storeStmt);

    if (plan->IsPostIV)
    {
        Statement* const addressUpdateStmt =
            m_compiler->fgNewStmtAtEnd(vectorBody, BuildAddressUpdate(plan), plan->StoreStmt->GetDebugInfo());
        m_compiler->gtSetStmtInfo(addressUpdateStmt);
        m_compiler->fgSetStmtSeq(addressUpdateStmt);

        Statement* const tripCountUpdateStmt =
            m_compiler->fgNewStmtAtEnd(vectorBody, BuildTripCountUpdate(plan, -static_cast<int>(plan->VectorizationFactor)),
                                       plan->StoreStmt->GetDebugInfo());
        m_compiler->gtSetStmtInfo(tripCountUpdateStmt);
        m_compiler->fgSetStmtSeq(tripCountUpdateStmt);

        Statement* const scalarGuardStmt =
            m_compiler->fgNewStmtAtEnd(scalarGuard, BuildScalarRemainderTest(plan), plan->StoreStmt->GetDebugInfo());
        m_compiler->gtSetStmtInfo(scalarGuardStmt);
        m_compiler->fgSetStmtSeq(scalarGuardStmt);
    }
    else
    {
        GenTree* const ivUpdate = BuildIVUpdate(plan);
        Statement* const ivStmt = m_compiler->fgNewStmtAtEnd(vectorBody, ivUpdate, plan->StoreStmt->GetDebugInfo());
        m_compiler->gtSetStmtInfo(ivStmt);
        m_compiler->fgSetStmtSeq(ivStmt);
    }

    m_compiler->fgRedirectEdge(plan->Preheader->TargetEdgeRef(), vectorCheck);

    FlowEdge* const vectorCheckToVectorBody = m_compiler->fgAddRefPred(vectorBody, vectorCheck);
    FlowEdge* const vectorCheckToScalar     =
        m_compiler->fgAddRefPred(plan->IsPostIV ? scalarGuard : plan->Header, vectorCheck);
    vectorCheckToVectorBody->setLikelihood(0.9);
    vectorCheckToScalar->setLikelihood(0.1);
    vectorCheck->SetCond(vectorCheckToVectorBody, vectorCheckToScalar);

    FlowEdge* const vectorBodyToCheck = m_compiler->fgAddRefPred(vectorCheck, vectorBody);
    vectorBodyToCheck->setLikelihood(1.0);
    vectorBody->SetKindAndTargetEdge(BBJ_ALWAYS, vectorBodyToCheck);

    if (scalarGuard != nullptr)
    {
        FlowEdge* const scalarGuardToExit   = m_compiler->fgAddRefPred(plan->Exit, scalarGuard);
        FlowEdge* const scalarGuardToScalar = m_compiler->fgAddRefPred(plan->Header, scalarGuard);
        scalarGuardToExit->setLikelihood(0.5);
        scalarGuardToScalar->setLikelihood(0.5);
        scalarGuard->SetCond(scalarGuardToExit, scalarGuardToScalar);
    }

    Dump("AutoVec: rewrote loop " FMT_LP " with %u-byte vectors and scalar epilogue\n", plan->Loop->GetIndex(),
         plan->VectorSizeBytes);
    return true;
#else
    Reject(plan->Loop, "SIMD rewrite not supported on target");
    return false;
#endif
}

AutoVectorizer::PackNode* AutoVectorizer::NewPackNode(
    SLPPlan* slpPlan, PackKind kind, var_types elementType, unsigned laneCount)
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
    if (!ShouldDump())
    {
        return;
    }

    const SLPPlan& slpPlan = plan.BodyPlan;
    printf("AutoVec: pack tree:\n");
    for (unsigned i = 0; i < slpPlan.NodeCount; i++)
    {
        const PackNode& node = slpPlan.Nodes[i];
        printf("  N%02u %-17s lanes=%u elem=%s cost=%u", i, PackKindName(node.Kind), node.LaneCount,
               varTypeName(node.ElementType), node.Cost);

        if (node.Kind == PackKind::BinaryOp)
        {
            printf(" op=%s", GenTree::OpName(node.Oper));
        }

        if (node.Operands[0] != nullptr)
        {
            printf(" op0=N%02u", static_cast<unsigned>(node.Operands[0] - slpPlan.Nodes));
        }

        if (node.Operands[1] != nullptr)
        {
            printf(" op1=N%02u", static_cast<unsigned>(node.Operands[1] - slpPlan.Nodes));
        }

        printf("\n");
    }
#endif
}

GenTree* AutoVectorizer::BuildArrayAddress(LoopVectorizationPlan* plan, const LoopVectorizationPlan::ScalarAccess& access)
{
    GenTreeArrAddr* const oldArrAddr = access.Address->AsArrAddr();
    const unsigned        elemSize    = access.ElementSize;
    const unsigned        firstOffset = oldArrAddr->GetFirstElemOffset();
    const unsigned        arrayLcl   = access.BaseLocalIfKnown;

    LclVarDsc* const arrayDsc = m_compiler->lvaGetDesc(arrayLcl);
    GenTree* const   arrRef   = m_compiler->gtNewLclvNode(arrayLcl, arrayDsc->TypeGet());
    LclVarDsc* const ivDsc    = m_compiler->lvaGetDesc(plan->InductionVar);
    GenTree*         index    = m_compiler->gtNewLclvNode(plan->InductionVar, ivDsc->TypeGet());

    if (access.IndexOffset != 0)
    {
        index = m_compiler->gtNewOperNode(GT_ADD, index->TypeGet(), index, m_compiler->gtNewIconNode(access.IndexOffset));
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
        offset = m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, offset, m_compiler->gtNewIconNode(firstOffset, TYP_I_IMPL));
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
        GenTree* const cmp = m_compiler->gtNewOperNode(GT_GE, TYP_INT, tripCount,
                                                       m_compiler->gtNewIconNode(plan->VectorizationFactor));
        return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
    }

    GenTree* const iv     = m_compiler->gtNewLclvNode(plan->InductionVar, TYP_INT);
    GenTree* const end    = m_compiler->gtCloneExpr(plan->End);
    GenTree* const limit  = m_compiler->gtNewOperNode(GT_SUB, TYP_INT, end,
                                                      m_compiler->gtNewIconNode(plan->VectorizationFactor));
    GenTree* const cmp    = m_compiler->gtNewOperNode(GT_LE, TYP_INT, iv, limit);
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

GenTree* AutoVectorizer::BuildVectorStore(LoopVectorizationPlan* plan)
{
#if defined(FEATURE_HW_INTRINSICS) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned simdSize = plan->VectorSizeBytes;
    const var_types simdType = Compiler::getSIMDTypeForSize(simdSize);

    GenTree* const loadAddress = plan->IsPostIV
                                     ? m_compiler->gtNewLclvNode(plan->AddressVar,
                                                                 m_compiler->lvaGetDesc(plan->AddressVar)->TypeGet())
                                     : BuildArrayAddress(plan, plan->LoadAccess);
    GenTree* const storeAddress = plan->IsPostIV
                                      ? m_compiler->gtNewLclvNode(plan->AddressVar,
                                                                  m_compiler->lvaGetDesc(plan->AddressVar)->TypeGet())
                                      : BuildArrayAddress(plan, plan->StoreAccess);
    GenTree* const vectorLoad   = m_compiler->gtNewSimdLoadNode(simdType, loadAddress, TYP_INT, simdSize);
    GenTree* const scalar       = m_compiler->gtCloneExpr(plan->ScalarOperand);
    GenTree* const splat        = m_compiler->gtNewSimdCreateBroadcastNode(simdType, scalar, TYP_INT, simdSize);
    GenTree* const vectorAdd    = m_compiler->gtNewSimdBinOpNode(GT_ADD, simdType, vectorLoad, splat, TYP_INT, simdSize);
    return m_compiler->gtNewSimdStoreNode(storeAddress, vectorAdd, TYP_INT, simdSize);
#else
    unreached();
#endif
}

GenTree* AutoVectorizer::BuildIVUpdate(LoopVectorizationPlan* plan)
{
    GenTree* const iv     = m_compiler->gtNewLclvNode(plan->InductionVar, TYP_INT);
    GenTree* const add    = m_compiler->gtNewOperNode(GT_ADD, TYP_INT, iv,
                                                   m_compiler->gtNewIconNode(plan->VectorizationFactor));
    return m_compiler->gtNewStoreLclVarNode(plan->InductionVar, add);
}

GenTree* AutoVectorizer::BuildAddressUpdate(LoopVectorizationPlan* plan)
{
    LclVarDsc* const addressDsc = m_compiler->lvaGetDesc(plan->AddressVar);
    GenTree* const   address    = m_compiler->gtNewLclvNode(plan->AddressVar, addressDsc->TypeGet());
    GenTree* const   add        = m_compiler->gtNewOperNode(
        GT_ADD, addressDsc->TypeGet(), address, m_compiler->gtNewIconNode(plan->VectorSizeBytes, TYP_I_IMPL));
    return m_compiler->gtNewStoreLclVarNode(plan->AddressVar, add);
}

GenTree* AutoVectorizer::BuildTripCountUpdate(LoopVectorizationPlan* plan, int delta)
{
    GenTree* const tripCount = m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT);
    GenTree* const add = m_compiler->gtNewOperNode(GT_ADD, TYP_INT, tripCount, m_compiler->gtNewIconNode(delta));
    return m_compiler->gtNewStoreLclVarNode(plan->TripCountVar, add);
}

GenTree* AutoVectorizer::BuildScalarRemainderTest(LoopVectorizationPlan* plan)
{
    GenTree* const tripCount = m_compiler->gtNewLclvNode(plan->TripCountVar, TYP_INT);
    GenTree* const cmp       = m_compiler->gtNewOperNode(GT_EQ, TYP_INT, tripCount, m_compiler->gtNewIconNode(0));
    return m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, cmp);
}

bool AutoVectorizer::TryAnalyzeArrayAccess(
    Statement* stmt, GenTree* indir, bool isStore, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access)
{
    assert(indir->OperIs(GT_IND, GT_STOREIND));

    GenTreeIndir* const indirNode = indir->AsIndir();
    if (indirNode->IsVolatile() || indirNode->Addr()->OperIs(GT_COMMA) || !indirNode->Addr()->OperIs(GT_ARR_ADDR))
    {
        return false;
    }

    if (!TryAnalyzeArrayAddress(indirNode->Addr()->AsArrAddr(), ivLcl, access))
    {
        return false;
    }

    access->StatementRoot = stmt;
    access->Address       = indirNode->Addr();
    access->ElementSize   = genTypeSize(access->ElementType);
    access->IsLoad        = !isStore;
    access->IsStore       = isStore;
    access->IsVolatile    = indirNode->IsVolatile();

    return true;
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

bool AutoVectorizer::TryAnalyzeIndexExpr(GenTree* tree, unsigned ivLcl, int* offset)
{
    if (tree->OperIs(GT_CAST))
    {
        return TryAnalyzeIndexExpr(tree->AsCast()->CastOp(), ivLcl, offset);
    }

    if (tree->OperIs(GT_LCL_VAR) && (tree->AsLclVarCommon()->GetLclNum() == ivLcl))
    {
        return true;
    }

    if (tree->OperIs(GT_ADD))
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op1->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(op2, ivLcl, offset);
        }

        if (op2->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op2->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(op1, ivLcl, offset);
        }
    }

    return false;
}

bool AutoVectorizer::TryAnalyzeArrayAddress(
    GenTreeArrAddr* arrAddr, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access)
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
        AddressVisitor(AutoVectorizer* vectorizer, unsigned ivLcl, unsigned elemSize, AddressParts* parts)
            : m_vectorizer(vectorizer)
            , m_ivLcl(ivLcl)
            , m_elemSize(elemSize)
            , m_parts(parts)
        {
        }

        bool Analyze(GenTree* tree)
        {
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
                if (!m_vectorizer->TryAnalyzeIndexExpr(index, m_ivLcl, &indexOffset))
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
                if (m_vectorizer->TryAnalyzeIndexExpr(tree, m_ivLcl, &indexOffset))
                {
                    m_parts->IndexScale += 1;
                    m_parts->Offset += indexOffset;
                    return true;
                }
            }

            return false;
        }

    private:
        AutoVectorizer* m_vectorizer;
        unsigned        m_ivLcl;
        unsigned        m_elemSize;
        AddressParts*   m_parts;
    };

    AddressVisitor visitor(this, ivLcl, elemSize, &parts);
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

bool AutoVectorizer::TryGetInvariantInt(FlowGraphNaturalLoop* loop, unsigned ivLcl, GenTree* tree)
{
    if (!tree->TypeIs(TYP_INT))
    {
        return false;
    }

    if (tree->IsCnsIntOrI())
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

bool AutoVectorizer::ShouldDump() const
{
#ifdef DEBUG
    return m_compiler->verbose ||
           JitConfig.JitAutoVectorizationDump().contains(m_compiler->info.compMethodHnd, m_compiler->info.compClassHnd,
                                                         &m_compiler->info.compMethodInfo->args);
#else
    return false;
#endif
}

void AutoVectorizer::Dump(const char* format, ...) const
{
#ifdef DEBUG
    if (!ShouldDump())
    {
        return;
    }

    va_list args;
    va_start(args, format);
    vprintf(format, args);
    va_end(args);
#endif
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
