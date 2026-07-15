// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "async.h"

// DataFlow::ForwardAnalysis callback used by DefaultValueAnalysis and
// PreservedValueAnalysis.
class MutationDataFlowCallback
{
    Compiler*  m_compiler;
    VARSET_TP  m_preMergeIn;
    VARSET_TP* m_mutatedVars;
    VARSET_TP* m_mutatedVarsIn;

public:
    MutationDataFlowCallback(Compiler* compiler, VARSET_TP* mutatedVars, VARSET_TP* mutatedVarsIn)
        : m_compiler(compiler)
        , m_preMergeIn(VarSetOps::UninitVal())
        , m_mutatedVars(mutatedVars)
        , m_mutatedVarsIn(mutatedVarsIn)
    {
    }

    void StartMerge(BasicBlock* block)
    {
        // Save the current in set for change detection later.
        VarSetOps::Assign(m_compiler, m_preMergeIn, m_mutatedVarsIn[block->bbNum]);
    }

    void Merge(BasicBlock* block, BasicBlock* predBlock, unsigned dupCount)
    {
        // The out set of a predecessor is its in set plus the locals
        // mutated in that block: mutatedOut = mutatedIn | mutated.
        VarSetOps::UnionD(m_compiler, m_mutatedVarsIn[block->bbNum], m_mutatedVarsIn[predBlock->bbNum]);
        VarSetOps::UnionD(m_compiler, m_mutatedVarsIn[block->bbNum], m_mutatedVars[predBlock->bbNum]);
    }

    void MergeHandler(BasicBlock* block, BasicBlock* firstTryBlock, BasicBlock* lastTryBlock)
    {
        // A handler can be reached from any point in the try region.
        // A local is mutated at handler entry if it was mutated at try
        // entry or mutated anywhere within the try region.
        for (BasicBlock* tryBlock = firstTryBlock; tryBlock != lastTryBlock->Next(); tryBlock = tryBlock->Next())
        {
            VarSetOps::UnionD(m_compiler, m_mutatedVarsIn[block->bbNum], m_mutatedVarsIn[tryBlock->bbNum]);
            VarSetOps::UnionD(m_compiler, m_mutatedVarsIn[block->bbNum], m_mutatedVars[tryBlock->bbNum]);
        }
    }

    bool EndMerge(BasicBlock* block)
    {
        return !VarSetOps::Equal(m_compiler, m_preMergeIn, m_mutatedVarsIn[block->bbNum]);
    }
};

//------------------------------------------------------------------------
// DefaultValueAnalysis::Run:
//   Run the default value analysis: compute per-block mutation sets, then
//   propagate default value information forward through the flow graph.
//
// Remarks:
//   Computes which tracked locals have their default (zero) value at each
//   basic block entry. A tracked local that still has its default value at a
//   suspension point does not need to be hoisted into the continuation.
//
//   The analysis has two phases:
//     1. Per-block: compute which tracked locals are mutated (assigned a
//        non-default value or have their address taken) in each block.
//     2. Inter-block: forward dataflow to propagate default value information
//        across blocks. At merge points the sets are unioned (a local is mutated
//        if it is mutated on any incoming path).
//
void DefaultValueAnalysis::Run()
{
#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitAsyncDefaultValueAnalysisRange());

    if (!s_range.Contains(m_compiler->info.compMethodHash()))
    {
        JITDUMP("Default value analysis disabled because of method range\n");
        m_mutatedVarsIn = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);
        for (BasicBlock* block : m_compiler->Blocks())
        {
            VarSetOps::AssignNoCopy(m_compiler, m_mutatedVarsIn[block->bbNum], VarSetOps::MakeFull(m_compiler));
        }

        return;
    }
#endif

    ComputePerBlockMutatedVars();
    ComputeInterBlockDefaultValues();
}

//------------------------------------------------------------------------
// DefaultValueAnalysis::GetMutatedVarsIn:
//   Get the set of tracked locals that have been mutated to a non-default
//   value on entry to the specified block.
//
// Parameters:
//   block - The basic block.
//
// Returns:
//   The VARSET_TP of tracked locals mutated on entry. A local NOT in this
//   set is guaranteed to have its default value.
//
const VARSET_TP& DefaultValueAnalysis::GetMutatedVarsIn(BasicBlock* block) const
{
    assert(m_mutatedVarsIn != nullptr);
    return m_mutatedVarsIn[block->bbNum];
}

//------------------------------------------------------------------------
// IsDefaultValue:
//   Check if a node represents a default (zero) value.
//
// Parameters:
//   node - The node to check.
//
// Returns:
//   True if the node is a constant zero value (integral, floating-point, or
//   vector).
//
static bool IsDefaultValue(GenTree* node)
{
    return node->IsIntegralConst(0) || node->IsFloatPositiveZero() || node->IsVectorZero();
}

//------------------------------------------------------------------------
// MarkMutatedVarDsc:
//   Mark a VarDsc (or its promoted fields) in the specified varset.
//
// Parameters:
//   compiler - The compiler instance.
//   varDsc   - The var.
//   mutated  - [in/out] The set to update.
//
static void MarkMutatedVarDsc(Compiler* compiler, LclVarDsc* varDsc, VARSET_TP& mutated)
{
    if (varDsc->lvTracked)
    {
        VarSetOps::AddElemD(compiler, mutated, varDsc->lvVarIndex);
        return;
    }

    if (varDsc->lvPromoted)
    {
        for (unsigned i = 0; i < varDsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + i);
            if (fieldDsc->lvTracked)
            {
                VarSetOps::AddElemD(compiler, mutated, fieldDsc->lvVarIndex);
            }
        }
    }
}

//------------------------------------------------------------------------
// UpdateMutatedLocal:
//   If the given node is a local store or LCL_ADDR, and the local is tracked,
//   mark it as mutated in the provided set. Stores of a default (zero) value
//   are not considered mutations.
//
// Parameters:
//   compiler - The compiler instance.
//   node     - The IR node to check.
//   mutated  - [in/out] The set to update.
//
static void UpdateMutatedLocal(Compiler* compiler, GenTree* node, VARSET_TP& mutated)
{
    if (node->OperIsLocalStore())
    {
        // If this is a zero initialization then we do not need to consider it
        // mutated if we know the prolog will zero it anyway (otherwise we
        // could be skipping this explicit zero init on resumption).
        // We could improve this a bit by still skipping it but inserting
        // explicit zero init on resumption, but these cases seem to be rare
        // and that would require tracking additional information.
        if (IsDefaultValue(node->AsLclVarCommon()->Data()) &&
            !compiler->fgVarNeedsExplicitZeroInit(node->AsLclVarCommon()->GetLclNum(), /* bbInALoop */ false,
                                                  /* bbIsReturn */ false))
        {
            return;
        }
    }
    else if (node->OperIs(GT_LCL_ADDR))
    {
        // Fall through
    }
    else
    {
        return;
    }

    LclVarDsc* varDsc = compiler->lvaGetDesc(node->AsLclVarCommon());
    MarkMutatedVarDsc(compiler, varDsc, mutated);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// PrintVarSet:
//   Print a varset as a space-separated list of locals.
//
// Parameters:
//   comp - Compiler instance
//   set  - The varset to print.
//
void AsyncAnalysis::PrintVarSet(Compiler* comp, VARSET_VALARG_TP set)
{
    VarSetOps::Iter iter(comp, set);
    unsigned        varIndex = 0;
    const char*     sep      = "";
    while (iter.NextElem(&varIndex))
    {
        unsigned lclNum = comp->lvaTrackedToVarNum[varIndex];
        printf("%sV%02u", sep, lclNum);
        sep = " ";
    }
    printf("\n");
}
#endif

//------------------------------------------------------------------------
// DefaultValueAnalysis::ComputePerBlockMutatedVars:
//   Phase 1: For each reachable basic block compute the set of tracked locals
//   that are mutated to a non-default value.
//
//   A tracked local is considered mutated if:
//     - It has a store (STORE_LCL_VAR / STORE_LCL_FLD) whose data operand is
//       not a zero constant.
//     - It has a LCL_ADDR use (address taken that we cannot reason about).
//
void DefaultValueAnalysis::ComputePerBlockMutatedVars()
{
    m_mutatedVars = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);

    for (unsigned i = 0; i <= m_compiler->fgBBNumMax; i++)
    {
        VarSetOps::AssignNoCopy(m_compiler, m_mutatedVars[i], VarSetOps::MakeEmpty(m_compiler));
    }

    for (BasicBlock* block : m_compiler->Blocks())
    {
        VARSET_TP& mutated = m_mutatedVars[block->bbNum];

        for (GenTree* node : LIR::AsRange(block))
        {
            UpdateMutatedLocal(m_compiler, node, mutated);
        }
    }

    JITDUMP("Default value analysis: per-block mutated vars\n");
    JITDUMPEXEC(DumpMutatedVars());
}

//------------------------------------------------------------------------
// DefaultValueAnalysis::ComputeInterBlockDefaultValues:
//   Phase 2: Forward dataflow to compute for each block the set of tracked
//   locals that have been mutated to a non-default value on entry.
//
//   Transfer function: mutatedOut[B] = mutatedIn[B] | mutated[B]
//   Merge: mutatedIn[B] = union of mutatedOut[pred] for all preds
//
//   At entry, only parameters and OSR locals are considered mutated.
//
void DefaultValueAnalysis::ComputeInterBlockDefaultValues()
{
    m_mutatedVarsIn = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);

    for (unsigned i = 0; i <= m_compiler->fgBBNumMax; i++)
    {
        VarSetOps::AssignNoCopy(m_compiler, m_mutatedVarsIn[i], VarSetOps::MakeEmpty(m_compiler));
    }

    // Parameters and OSR locals are considered mutated at method entry.
    for (unsigned i = 0; i < m_compiler->lvaTrackedCount; i++)
    {
        unsigned   lclNum = m_compiler->lvaTrackedToVarNum[i];
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        if (varDsc->lvIsParam || varDsc->lvIsOSRLocal)
        {
            VarSetOps::AddElemD(m_compiler, m_mutatedVarsIn[m_compiler->fgFirstBB->bbNum], varDsc->lvVarIndex);
        }
    }

    MutationDataFlowCallback callback(m_compiler, m_mutatedVars, m_mutatedVarsIn);
    DataFlow                 flow(m_compiler);
    flow.ForwardAnalysis(callback);

    JITDUMP("Default value analysis: per-block mutated vars on entry\n");
    JITDUMPEXEC(DumpMutatedVarsIn());
}

#ifdef DEBUG
//------------------------------------------------------------------------
// DefaultValueAnalysis::DumpMutatedVars:
//   Debug helper to print the per-block mutated variable sets.
//
void DefaultValueAnalysis::DumpMutatedVars()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (!VarSetOps::IsEmpty(m_compiler, m_mutatedVars[block->bbNum]))
        {
            printf("  " FMT_BB " mutated: ", block->bbNum);
            AsyncAnalysis::PrintVarSet(m_compiler, m_mutatedVars[block->bbNum]);
        }
    }
}

//------------------------------------------------------------------------
// DefaultValueAnalysis::DumpMutatedVarsIn:
//   Debug helper to print the per-block mutated-on-entry variable sets.
//
void DefaultValueAnalysis::DumpMutatedVarsIn()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        printf("  " FMT_BB " mutated on entry: ", block->bbNum);

        if (VarSetOps::IsEmpty(m_compiler, m_mutatedVarsIn[block->bbNum]))
        {
            printf("<none>\n");
        }
        else
        {
            AsyncAnalysis::PrintVarSet(m_compiler, m_mutatedVarsIn[block->bbNum]);
        }
    }
}
#endif

//------------------------------------------------------------------------
// MarkMutatedLocal:
//   If the given node is a local store or LCL_ADDR, and the local is tracked,
//   mark it as mutated in the provided set. Unlike UpdateMutatedLocal, all
//   stores count as mutations (including stores of default values).
//
// Parameters:
//   compiler - The compiler instance.
//   node     - The IR node to check.
//   mutated  - [in/out] The set to update.
//
static void MarkMutatedLocal(Compiler* compiler, GenTree* node, VARSET_TP& mutated)
{
    if (node->IsCall())
    {
        auto visitDef = [&](GenTreeLclVarCommon* lcl) {
            MarkMutatedVarDsc(compiler, compiler->lvaGetDesc(lcl), mutated);
            return GenTree::VisitResult::Continue;
        };
        node->VisitLocalDefNodes(compiler, visitDef);
    }
    else if (node->OperIsLocalStore() || node->OperIs(GT_LCL_ADDR))
    {
        MarkMutatedVarDsc(compiler, compiler->lvaGetDesc(node->AsLclVarCommon()), mutated);
    }
    else if (node->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
             compiler->lvaIsImplicitByRefLocal(node->AsLclVarCommon()->GetLclNum()))
    {
        MarkMutatedVarDsc(compiler, compiler->lvaGetDesc(node->AsLclVarCommon()), mutated);
    }
    else
    {
        return;
    }
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::Run:
//   Run the preserved value analysis: identify await blocks, compute
//   resume-reachable blocks, then compute per-block and inter-block
//   mutation sets relative to resumption points.
//
// Parameters:
//   awaitblocks - Blocks containing async calls
//
// Remarks:
//   Computes which tracked locals may have been mutated since the previous
//   resumption point. A local that has not been mutated since the last
//   resumption does not need to be stored into a reused continuation, because
//   the continuation already holds the correct value.
//
//   The analysis proceeds in several steps:
//
//   0. Identify blocks that contain awaits and compute which blocks are
//      reachable after resumption.
//
//   1. Per-block: compute which tracked locals are mutated (assigned any
//      value or have their address taken) in each block. Only mutations
//      reachable after resumption need to be taken into account.
//
//   2. Inter-block: forward dataflow to compute for each block the set of
//      tracked locals that have been mutated since the previous resumption.
//      At merge points the sets are unioned (a local is mutated if it is
//      mutated on any incoming path).
//
void PreservedValueAnalysis::Run(ArrayStack<BasicBlock*>& awaitBlocks)
{
#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitAsyncPreservedValueAnalysisRange());

    if (!s_range.Contains(m_compiler->info.compMethodHash()))
    {
        JITDUMP("Preserved value analysis disabled because of method range\n");
        m_mutatedVarsIn = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);
        m_mutatedVars   = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);
        for (BasicBlock* block : m_compiler->Blocks())
        {
            VarSetOps::AssignNoCopy(m_compiler, m_mutatedVarsIn[block->bbNum], VarSetOps::MakeFull(m_compiler));
            VarSetOps::AssignNoCopy(m_compiler, m_mutatedVars[block->bbNum], VarSetOps::MakeFull(m_compiler));
        }

        m_resumeReachableBlocks = BitVecOps::MakeFull(&m_blockTraits);
        m_awaitBlocks           = BitVecOps::MakeFull(&m_blockTraits);

        return;
    }
#endif

    ComputeResumeReachableBlocks(awaitBlocks);
    ComputePerBlockMutatedVars();
    ComputeInterBlockMutatedVars();
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::GetMutatedVarsIn:
//   Get the set of tracked locals that may have been mutated since the
//   previous resumption on entry to the specified block.
//
// Parameters:
//   block - The basic block.
//
// Returns:
//   The VARSET_TP of tracked locals that may have been mutated since last
//   resumption. A tracked local NOT in this set has a preserved value in the
//   continuation and does not need to be re-stored when reusing it.
//
const VARSET_TP& PreservedValueAnalysis::GetMutatedVarsIn(BasicBlock* block) const
{
    assert(m_mutatedVarsIn != nullptr);
    return m_mutatedVarsIn[block->bbNum];
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::IsResumeReachable:
//   Check if the specified basic block is reachable after a previous resumption.
//
// Parameters:
//   block - The basic block.
//
// Returns:
//   True if so. Blocks that are not resume-reachable will never be able to
//   reuse a continuation. Also, mutations of locals that are not
//   resume-reachable do not need to be considered for preserved value
//   analysis.
//
bool PreservedValueAnalysis::IsResumeReachable(BasicBlock* block)
{
    return BitVecOps::IsMember(&m_blockTraits, m_resumeReachableBlocks, block->bbNum);
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::ComputeResumeReachableBlocks:
//   Phase 0: Identify blocks containing awaits, then compute the set of
//   blocks reachable after any resumption via a DFS starting from await blocks.
//
void PreservedValueAnalysis::ComputeResumeReachableBlocks(ArrayStack<BasicBlock*>& awaitBlocks)
{
    m_awaitBlocks           = BitVecOps::MakeEmpty(&m_blockTraits);
    m_resumeReachableBlocks = BitVecOps::MakeEmpty(&m_blockTraits);

    ArrayStack<BasicBlock*> worklist(m_compiler->getAllocator(CMK_Async));
    // Find all blocks that contain awaits.
    for (BasicBlock* awaitBlock : awaitBlocks.BottomUpOrder())
    {
        BitVecOps::AddElemD(&m_blockTraits, m_awaitBlocks, awaitBlock->bbNum);
        worklist.Push(awaitBlock);
    }

    JITDUMP("Preserved value analysis: blocks containing awaits\n");
    JITDUMPEXEC(DumpAwaitBlocks());

    // DFS from those blocks.
    while (!worklist.Empty())
    {
        BasicBlock* block = worklist.Pop();

        block->VisitAllSuccs(m_compiler, [&](BasicBlock* succ) {
            if (BitVecOps::TryAddElemD(&m_blockTraits, m_resumeReachableBlocks, succ->bbNum))
            {
                worklist.Push(succ);
            }
            return BasicBlockVisit::Continue;
        });
    }

    JITDUMP("Preserved value analysis: blocks reachable after resuming\n");
    JITDUMPEXEC(DumpResumeReachableBlocks());
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::ComputePerBlockMutatedVars:
//   Phase 1: For each reachable basic block compute the set of tracked locals
//   that are mutated.
//
//   For blocks that are reachable after resumption the full set of mutations
//   in the block is recorded.
//
//   For blocks that are NOT reachable after resumption but contain an await,
//   only mutations that occur after the first suspension point are recorded,
//   because mutations before the first suspension are not relevant to
//   preserved values (the continuation did not exist yet or was not being
//   reused at that point).
//
void PreservedValueAnalysis::ComputePerBlockMutatedVars()
{
    m_mutatedVars = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);

    for (unsigned i = 0; i <= m_compiler->fgBBNumMax; i++)
    {
        VarSetOps::AssignNoCopy(m_compiler, m_mutatedVars[i], VarSetOps::MakeEmpty(m_compiler));
    }

    for (BasicBlock* block : m_compiler->Blocks())
    {
        VARSET_TP& mutated = m_mutatedVars[block->bbNum];

        bool isAwaitBlock      = BitVecOps::IsMember(&m_blockTraits, m_awaitBlocks, block->bbNum);
        bool isResumeReachable = BitVecOps::IsMember(&m_blockTraits, m_resumeReachableBlocks, block->bbNum);

        if (!isResumeReachable && !isAwaitBlock)
        {
            continue;
        }

        GenTree* node = block->GetFirstLIRNode();

        if (!isResumeReachable)
        {
            while (!node->IsCall() || !node->AsCall()->IsAsync())
            {
                node = node->gtNext;
                assert(node != nullptr);
            }
        }

        while (node != nullptr)
        {
            MarkMutatedLocal(m_compiler, node, mutated);
            node = node->gtNext;
        }
    }

    JITDUMP("Preserved value analysis: per-block mutated vars after resumption\n");
    JITDUMPEXEC(DumpMutatedVars());
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::ComputeInterBlockMutatedVars:
//   Phase 2: Forward dataflow to compute for each block the set of tracked
//   locals that have been mutated since the previous resumption on entry.
//
//   Transfer function: mutatedOut[B] = mutatedIn[B] | mutated[B]
//   Merge: mutatedIn[B] = union of mutatedOut[pred] for all preds
//
//   At method entry no locals are considered mutated (not reachable from a resumption).
//
void PreservedValueAnalysis::ComputeInterBlockMutatedVars()
{
    m_mutatedVarsIn = m_compiler->fgAllocateTypeForEachBlk<VARSET_TP>(CMK_Async);

    for (unsigned i = 0; i <= m_compiler->fgBBNumMax; i++)
    {
        VarSetOps::AssignNoCopy(m_compiler, m_mutatedVarsIn[i], VarSetOps::MakeEmpty(m_compiler));
    }

    MutationDataFlowCallback callback(m_compiler, m_mutatedVars, m_mutatedVarsIn);
    DataFlow                 flow(m_compiler);
    flow.ForwardAnalysis(callback);

    JITDUMP("Preserved value analysis: per-block mutated vars on entry\n");
    JITDUMPEXEC(DumpMutatedVarsIn());
}

#ifdef DEBUG
//------------------------------------------------------------------------
// PreservedValueAnalysis::DumpAwaitBlocks:
//   Debug helper to print the set of blocks containing awaits.
//
void PreservedValueAnalysis::DumpAwaitBlocks()
{
    printf("  Await blocks:");
    const char* sep = " ";
    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (BitVecOps::IsMember(&m_blockTraits, m_awaitBlocks, block->bbNum))
        {
            printf("%s" FMT_BB, sep, block->bbNum);
            sep = ", ";
        }
    }
    printf("\n");
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::DumpResumeReachableBlocks:
//   Debug helper to print the set of resume-reachable blocks.
//
void PreservedValueAnalysis::DumpResumeReachableBlocks()
{
    printf("  Resume-reachable blocks:");
    const char* sep = " ";
    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (BitVecOps::IsMember(&m_blockTraits, m_resumeReachableBlocks, block->bbNum))
        {
            printf("%s" FMT_BB, sep, block->bbNum);
            sep = ", ";
        }
    }
    printf("\n");
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::DumpMutatedVars:
//   Debug helper to print the per-block mutated variable sets.
//
void PreservedValueAnalysis::DumpMutatedVars()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (!VarSetOps::IsEmpty(m_compiler, m_mutatedVars[block->bbNum]))
        {
            printf("  " FMT_BB " mutated: ", block->bbNum);
            AsyncAnalysis::PrintVarSet(m_compiler, m_mutatedVars[block->bbNum]);
        }
    }
}

//------------------------------------------------------------------------
// PreservedValueAnalysis::DumpMutatedVarsIn:
//   Debug helper to print the per-block mutated-on-entry variable sets.
//
void PreservedValueAnalysis::DumpMutatedVarsIn()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        printf("  " FMT_BB " mutated since resumption on entry: ", block->bbNum);

        if (VarSetOps::IsEmpty(m_compiler, m_mutatedVarsIn[block->bbNum]))
        {
            printf("<none>\n");
        }
        else if (VarSetOps::Equal(m_compiler, m_mutatedVarsIn[block->bbNum], VarSetOps::MakeFull(m_compiler)))
        {
            printf("<all>\n");
        }
        else
        {
            AsyncAnalysis::PrintVarSet(m_compiler, m_mutatedVarsIn[block->bbNum]);
        }
    }
}
#endif

//------------------------------------------------------------------------
// AsyncAnalysis::StartBlock:
//   Indicate that we are now starting a new block, and do relevant liveness
//   and other analysis updates for it.
//
// Parameters:
//   block - The block that we are starting.
//
void AsyncAnalysis::StartBlock(BasicBlock* block)
{
    VarSetOps::Assign(m_compiler, m_compiler->compCurLife, block->bbLiveIn);
    VarSetOps::Assign(m_compiler, m_mutatedValues, m_defaultValueAnalysis.GetMutatedVarsIn(block));
    VarSetOps::Assign(m_compiler, m_mutatedSinceResumption, m_preservedValueAnalysis.GetMutatedVarsIn(block));
    m_resumeReachable = m_preservedValueAnalysis.IsResumeReachable(block);
}

//------------------------------------------------------------------------
// AsyncAnalysis::Update:
//   Update liveness to be consistent with the specified node having been
//   executed.
//
// Parameters:
//   node - The node.
//
void AsyncAnalysis::Update(GenTree* node)
{
    m_updater.UpdateLife<true>(node);
    UpdateMutatedLocal(m_compiler, node, m_mutatedValues);

    // If this is an async call then we can reach defs after resumption now.
    // Make sure defs happening as part of the call are included as mutated since resumption.
    m_resumeReachable |= node->IsCall() && node->AsCall()->IsAsync();
    if (m_resumeReachable)
    {
        MarkMutatedLocal(m_compiler, node, m_mutatedSinceResumption);
    }
}

//------------------------------------------------------------------------
// AsyncAnalysis::IsLocalCaptureUnnecessary:
//   Check if capturing a specified local can be skipped.
//
// Parameters:
//   lclNum - The local
//
// Returns:
//   True if the local should not be captured. Even without liveness
//
bool AsyncAnalysis::IsLocalCaptureUnnecessary(unsigned lclNum)
{
#if FEATURE_FIXED_OUT_ARGS
    if (lclNum == m_compiler->lvaOutgoingArgSpaceVar)
    {
        return true;
    }
#endif

    if (lclNum == m_compiler->info.compRetBuffArg)
    {
        return true;
    }

    if (lclNum == m_compiler->lvaGSSecurityCookie)
    {
        // Initialized in prolog
        return true;
    }

    if (lclNum == m_compiler->info.compLvFrameListRoot)
    {
        return true;
    }

    if (lclNum == m_compiler->lvaInlinedPInvokeFrameVar)
    {
        return true;
    }

    if (lclNum == m_compiler->lvaRetAddrVar)
    {
        return true;
    }

    if (lclNum == m_compiler->lvaAsyncContinuationArg)
    {
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// AsyncAnalysis::IsLive:
//   Check if the specified local is live at this point and should be captured.
//
// Parameters:
//   lclNum - The local
//
// Returns:
//   True if the local is live and capturing it is necessary.
//
bool AsyncAnalysis::IsLive(unsigned lclNum)
{
    if (IsLocalCaptureUnnecessary(lclNum))
    {
        return false;
    }

    LclVarDsc* dsc = m_compiler->lvaGetDesc(lclNum);

    if (dsc->TypeIs(TYP_BYREF) && !dsc->IsImplicitByRef())
    {
        // Even if these are address exposed we expect them to be dead at
        // suspension points. TODO: It would be good to somehow verify these
        // aren't obviously live, if the JIT creates live ranges that span a
        // suspension point then this makes it quite hard to diagnose that.
        return false;
    }

    if ((dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef()) && dsc->GetLayout()->HasGCByRef())
    {
        // Same as above
        return false;
    }

    if (m_compiler->opts.compDbgCode && (lclNum < m_compiler->info.compLocalsCount))
    {
        // Keep all IL locals in debug codegen
        return true;
    }

    if (dsc->lvRefCnt(RCS_NORMAL) == 0)
    {
        return false;
    }

    Compiler::lvaPromotionType promoType = m_compiler->lvaGetPromotionType(dsc);
    if (promoType == Compiler::PROMOTION_TYPE_INDEPENDENT)
    {
        // Independently promoted structs are handled only through their
        // fields.
        return false;
    }

    if (promoType == Compiler::PROMOTION_TYPE_DEPENDENT)
    {
        // Dependently promoted structs are handled only through the base
        // struct local.
        //
        // A dependently promoted struct is live if any of its fields are live.

        bool anyLive    = false;
        bool anyMutated = false;
        for (unsigned i = 0; i < dsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldDsc = m_compiler->lvaGetDesc(dsc->lvFieldLclStart + i);
            anyLive |=
                !fieldDsc->lvTracked || VarSetOps::IsMember(m_compiler, m_compiler->compCurLife, fieldDsc->lvVarIndex);
            anyMutated |=
                !fieldDsc->lvTracked || VarSetOps::IsMember(m_compiler, m_mutatedValues, fieldDsc->lvVarIndex);
        }

        return anyLive && anyMutated;
    }

    if (dsc->lvIsStructField && (m_compiler->lvaGetParentPromotionType(dsc) == Compiler::PROMOTION_TYPE_DEPENDENT))
    {
        return false;
    }

    if (!dsc->lvTracked)
    {
        return true;
    }

    if (!VarSetOps::IsMember(m_compiler, m_compiler->compCurLife, dsc->lvVarIndex))
    {
        return false;
    }

    if (!VarSetOps::IsMember(m_compiler, m_mutatedValues, dsc->lvVarIndex))
    {
        return false;
    }

    return true;
}
