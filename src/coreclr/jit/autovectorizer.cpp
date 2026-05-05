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

    if (m_compiler->m_loops == nullptr)
    {
        Dump("AutoVec: no loop table\n");
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
                 plan.InductionVar, plan.VectorizationFactor, plan.VectorSizeBytes);
        }
    }

    Dump("AutoVec: analysis found %u candidate loop%s\n", candidateCount, (candidateCount == 1) ? "" : "s");
    return PhaseStatus::MODIFIED_NOTHING;
}

PhaseStatus AutoVectorizer::RunRewrite()
{
    Dump("AutoVec: rewrite phase enabled\n");
    return PhaseStatus::MODIFIED_NOTHING;
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
    const unsigned vectorSizeBytes = m_compiler->getMaxVectorByteLength();
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
        Reject(loop, "no canonical counted iteration");
        return false;
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
    plan->TestOper     = testOper;
    plan->Step         = 1;
#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = m_compiler->getMaxVectorByteLength();
    plan->VectorizationFactor = plan->VectorSizeBytes / genTypeSize(TYP_INT);
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(loop, "vectorization factor is too small");
        return false;
    }

    Dump("AutoVec: loop " FMT_LP " canonical IV V%02u, init=0, step=1, test=%s\n", loop->GetIndex(),
         iterInfo.IterVar, GenTree::OpName(testOper));
    return true;
}

void AutoVectorizer::Reject(FlowGraphNaturalLoop* loop, const char* reason) const
{
    Dump("AutoVec: rejected loop " FMT_LP ": %s\n", loop->GetIndex(), reason);
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
