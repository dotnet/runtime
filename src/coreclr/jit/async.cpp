// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file implements the transformation of C# async methods into state
// machines. The transformation takes place late in the JIT pipeline, when most
// optimizations have already been performed, right before lowering.
//
// The transformation performs the following key operations:
//
// 1. Each async call becomes a suspension point where execution can pause and
//    return to the caller, accompanied by a resumption point where execution can
//    continue when the awaited operation completes.
//
// 2. When suspending at a suspension point a continuation object is created that contains:
//    - All live local variables
//    - State number to identify which await is being resumed
//    - Return value from the awaited operation (filled in by the callee later)
//    - Exception information if an exception occurred
//    - Resumption function pointer
//    - Flags containing additional information
//
// 3. The method entry is modified to include dispatch logic that checks for an
//    incoming continuation and jumps to the appropriate resumption point.
//
// 4. Special handling is included for:
//    - Exception propagation across await boundaries
//    - Return value management for different types (primitives, references, structs)
//    - Tiered compilation and On-Stack Replacement (OSR)
//    - Optimized state capture based on variable liveness analysis
//
// The transformation ensures that the semantics of the original async method are
// preserved while enabling efficient suspension and resumption of execution.
//

#include "jitpch.h"
#include "jitstd/algorithm.h"
#include "async.h"

class AsyncLiveness
{
    Compiler*              m_comp;
    bool                   m_hasLiveness;
    TreeLifeUpdater<false> m_updater;
    unsigned               m_numVars;

public:
    AsyncLiveness(Compiler* comp, bool hasLiveness)
        : m_comp(comp)
        , m_hasLiveness(hasLiveness)
        , m_updater(comp)
        , m_numVars(comp->lvaCount)
    {
    }

    void StartBlock(BasicBlock* block);
    void Update(GenTree* node);
    bool IsLive(unsigned lclNum);
    void GetLiveLocals(jitstd::vector<LiveLocalInfo>& liveLocals, unsigned fullyDefinedRetBufLcl);

private:
    bool IsLocalCaptureUnnecessary(unsigned lclNum);
};

//------------------------------------------------------------------------
// AsyncLiveness::StartBlock:
//   Indicate that we are now starting a new block, and do relevant liveness
//   updates for it.
//
// Parameters:
//   block - The block that we are starting.
//
void AsyncLiveness::StartBlock(BasicBlock* block)
{
    if (!m_hasLiveness)
        return;

    VarSetOps::Assign(m_comp, m_comp->compCurLife, block->bbLiveIn);
}

//------------------------------------------------------------------------
// AsyncLiveness::Update:
//   Update liveness to be consistent with the specified node having been
//   executed.
//
// Parameters:
//   node - The node.
//
void AsyncLiveness::Update(GenTree* node)
{
    if (!m_hasLiveness)
        return;

    m_updater.UpdateLife(node);
}

//------------------------------------------------------------------------
// AsyncLiveness::IsLocalCaptureUnnecessary:
//   Check if capturing a specified local can be skipped.
//
// Parameters:
//   lclNum - The local
//
// Returns:
//   True if the local should not be captured. Even without liveness
//
bool AsyncLiveness::IsLocalCaptureUnnecessary(unsigned lclNum)
{
#if FEATURE_FIXED_OUT_ARGS
    if (lclNum == m_comp->lvaOutgoingArgSpaceVar)
    {
        return true;
    }
#endif

    if (lclNum == m_comp->info.compRetBuffArg)
    {
        return true;
    }

    if (lclNum == m_comp->lvaGSSecurityCookie)
    {
        // Initialized in prolog
        return true;
    }

    if (lclNum == m_comp->info.compLvFrameListRoot)
    {
        return true;
    }

    if (lclNum == m_comp->lvaInlinedPInvokeFrameVar)
    {
        return true;
    }

#ifdef FEATURE_EH_WINDOWS_X86
    if (lclNum == m_comp->lvaShadowSPslotsVar)
    {
        // Only expected to be live in handlers
        return true;
    }
#endif

    if (lclNum == m_comp->lvaRetAddrVar)
    {
        return true;
    }

    if (lclNum == m_comp->lvaAsyncContinuationArg)
    {
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// AsyncLiveness::IsLive:
//   Check if the specified local is live at this point and should be captured.
//
// Parameters:
//   lclNum - The local
//
// Returns:
//   True if the local is live and capturing it is necessary.
//
bool AsyncLiveness::IsLive(unsigned lclNum)
{
    if (IsLocalCaptureUnnecessary(lclNum))
    {
        return false;
    }

    LclVarDsc* dsc = m_comp->lvaGetDesc(lclNum);

    if ((dsc->TypeIs(TYP_BYREF) && !dsc->IsImplicitByRef()) ||
        (dsc->TypeIs(TYP_STRUCT) && dsc->GetLayout()->HasGCByRef()))
    {
        // Even if these are address exposed we expect them to be dead at
        // suspension points. TODO: It would be good to somehow verify these
        // aren't obviously live, if the JIT creates live ranges that span a
        // suspension point then this makes it quite hard to diagnose that.
        return false;
    }

    if (!m_hasLiveness)
    {
        return true;
    }

    if (dsc->lvRefCnt(RCS_NORMAL) == 0)
    {
        return false;
    }

    Compiler::lvaPromotionType promoType = m_comp->lvaGetPromotionType(dsc);
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

        for (unsigned i = 0; i < dsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldDsc = m_comp->lvaGetDesc(dsc->lvFieldLclStart + i);
            if (!fieldDsc->lvTracked || VarSetOps::IsMember(m_comp, m_comp->compCurLife, fieldDsc->lvVarIndex))
            {
                return true;
            }
        }

        return false;
    }

    if (dsc->lvIsStructField && (m_comp->lvaGetParentPromotionType(dsc) == Compiler::PROMOTION_TYPE_DEPENDENT))
    {
        return false;
    }

    return !dsc->lvTracked || VarSetOps::IsMember(m_comp, m_comp->compCurLife, dsc->lvVarIndex);
}

//------------------------------------------------------------------------
// AsyncLiveness::GetLiveLocals:
//   Get live locals that should be captured at this point.
//
// Parameters:
//   liveLocals            - Vector to add live local information into
//   fullyDefinedRetBufLcl - Local to skip even if live
//
void AsyncLiveness::GetLiveLocals(jitstd::vector<LiveLocalInfo>& liveLocals, unsigned fullyDefinedRetBufLcl)
{
    for (unsigned lclNum = 0; lclNum < m_numVars; lclNum++)
    {
        if ((lclNum != fullyDefinedRetBufLcl) && IsLive(lclNum))
        {
            liveLocals.push_back(LiveLocalInfo(lclNum));
        }
    }
}

//------------------------------------------------------------------------
// TransformAsync: Run async transformation.
//
// Returns:
//   Suitable phase status.
//
// Remarks:
//   This transformation creates the state machine structure of the async
//   function. After each async call a check for whether that async call
//   suspended is inserted. If the check passes a continuation is allocated
//   into which the live state is stored. The continuation is returned back to
//   the caller to indicate that now this function also suspended.
//
//   Associated with each suspension point is also resumption IR. The
//   resumption IR restores all live state from the continuation object. IR is
//   inserted at the beginning of the function to dispatch on the continuation
//   (if one is present), which each suspension point having an associated
//   state number that can be switched over.
//
PhaseStatus Compiler::TransformAsync()
{
    assert(compIsAsync());

    AsyncTransformation transformation(this);
    return transformation.Run();
}

//------------------------------------------------------------------------
// AsyncTransformation::Run:
//   Run the transformation over all the IR.
//
// Returns:
//   Suitable phase status.
//
PhaseStatus AsyncTransformation::Run()
{
    ArrayStack<BasicBlock*> worklist(m_comp->getAllocator(CMK_Async));

    // First find all basic blocks with awaits in them. We'll have to track
    // liveness in these basic blocks, so it does not help to record the calls
    // ahead of time.
    for (BasicBlock* block : m_comp->Blocks())
    {
        for (GenTree* tree : LIR::AsRange(block))
        {
            if (tree->IsCall() && tree->AsCall()->IsAsync() && !tree->AsCall()->IsTailCall())
            {
                JITDUMP(FMT_BB " contains await(s)\n", block->bbNum);
                worklist.Push(block);
                break;
            }
        }
    }

    JITDUMP("Found %d blocks with awaits\n", worklist.Height());

    if (worklist.Height() <= 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Ask the VM to create a resumption stub for this specific version of the
    // code. It is stored in the continuation as a function pointer, so we need
    // the fixed entry point here.
    m_resumeStub = m_comp->info.compCompHnd->getAsyncResumptionStub();
    m_comp->info.compCompHnd->getFunctionFixedEntryPoint(m_resumeStub, false, &m_resumeStubLookup);

    m_returnedContinuationVar = m_comp->lvaGrabTemp(false DEBUGARG("returned continuation"));
    m_comp->lvaGetDesc(m_returnedContinuationVar)->lvType = TYP_REF;
    m_newContinuationVar                                  = m_comp->lvaGrabTemp(false DEBUGARG("new continuation"));
    m_comp->lvaGetDesc(m_newContinuationVar)->lvType      = TYP_REF;

    m_comp->info.compCompHnd->getAsyncInfo(&m_asyncInfo);

#ifdef JIT32_GCENCODER
    // Due to a hard cap on epilogs we need a shared return here.
    m_sharedReturnBB = m_comp->fgNewBBafter(BBJ_RETURN, m_comp->fgLastBBInMainFunction(), false);
    m_sharedReturnBB->bbSetRunRarely();
    m_sharedReturnBB->clearTryIndex();
    m_sharedReturnBB->clearHndIndex();

    if (m_comp->fgIsUsingProfileWeights())
    {
        // All suspension BBs are cold, so we do not need to propagate any
        // weights, but we do need to propagate the flag.
        m_sharedReturnBB->SetFlags(BBF_PROF_WEIGHT);
    }

    GenTree* continuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    GenTree* ret          = m_comp->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, continuation);
    LIR::AsRange(m_sharedReturnBB).InsertAtEnd(continuation, ret);

    JITDUMP("Created shared return BB " FMT_BB "\n", m_sharedReturnBB->bbNum);

    DISPRANGE(LIR::AsRange(m_sharedReturnBB));
#endif

    // Compute liveness to be used for determining what must be captured on
    // suspension. In unoptimized codegen we capture everything.
    if (m_comp->opts.OptimizationEnabled())
    {
        if (m_comp->m_dfsTree == nullptr)
        {
            m_comp->m_dfsTree = m_comp->fgComputeDfs<false>();
        }

        m_comp->lvaComputeRefCounts(true, false);
        m_comp->fgLocalVarLiveness();
        INDEBUG(m_comp->mostRecentlyActivePhase = PHASE_ASYNC);
        VarSetOps::AssignNoCopy(m_comp, m_comp->compCurLife, VarSetOps::MakeEmpty(m_comp));
    }

    AsyncLiveness liveness(m_comp, m_comp->opts.OptimizationEnabled());

    // Now walk the IR for all the blocks that contain async calls. Keep track
    // of liveness and outstanding LIR edges as we go; the LIR edges that cross
    // async calls are additional live variables that must be spilled.
    jitstd::vector<GenTree*> defs(m_comp->getAllocator(CMK_Async));

    for (int i = 0; i < worklist.Height(); i++)
    {
        assert(defs.size() == 0);

        BasicBlock* block = worklist.Bottom(i);
        liveness.StartBlock(block);

        bool any;
        do
        {
            any = false;
            for (GenTree* tree : LIR::AsRange(block))
            {
                // Remove all consumed defs; those are no longer 'live' LIR
                // edges.
                tree->VisitOperands([&defs](GenTree* op) {
                    if (op->IsValue())
                    {
                        for (size_t i = defs.size(); i > 0; i--)
                        {
                            if (op == defs[i - 1])
                            {
                                defs[i - 1] = defs[defs.size() - 1];
                                defs.erase(defs.begin() + (defs.size() - 1), defs.end());
                                break;
                            }
                        }
                    }

                    return GenTree::VisitResult::Continue;
                });

                // Update liveness to reflect state after this node.
                liveness.Update(tree);

                if (tree->IsCall() && tree->AsCall()->IsAsync() && !tree->AsCall()->IsTailCall())
                {
                    // Transform call; continue with the remainder block
                    Transform(block, tree->AsCall(), defs, liveness, &block);
                    defs.clear();
                    any = true;
                    break;
                }

                // Push a new definition if necessary; this defined value is
                // now a live LIR edge.
                if (tree->IsValue() && !tree->IsUnusedValue())
                {
                    defs.push_back(tree);
                }
            }
        } while (any);
    }

    // After transforming all async calls we have created resumption blocks;
    // create the resumption switch.
    CreateResumptionSwitch();

    m_comp->fgInvalidateDfsTree();

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// AsyncTransformation::Transform:
//   Transform a single async call in the specified block.
//
// Parameters:
//   block     - The block containing the async call
//   call      - The async call
//   defs      - Current live LIR edges
//   life      - Liveness information about live locals
//   remainder - [out] Remainder block after the transformation
//
void AsyncTransformation::Transform(
    BasicBlock* block, GenTreeCall* call, jitstd::vector<GenTree*>& defs, AsyncLiveness& life, BasicBlock** remainder)
{
#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("Processing call [%06u] in " FMT_BB "\n", Compiler::dspTreeID(call), block->bbNum);
        printf("  %zu live LIR edges\n", defs.size());

        if (defs.size() > 0)
        {
            const char* sep = "    ";
            for (GenTree* tree : defs)
            {
                printf("%s[%06u] (%s)", sep, Compiler::dspTreeID(tree), varTypeName(tree->TypeGet()));
                sep = ", ";
            }

            printf("\n");
        }
    }
#endif

    m_liveLocalsScratch.clear();
    jitstd::vector<LiveLocalInfo>& liveLocals = m_liveLocalsScratch;

    CreateLiveSetForSuspension(block, call, defs, life, liveLocals);

    ContinuationLayout layout = LayOutContinuation(block, call, liveLocals);

    CallDefinitionInfo callDefInfo = CanonicalizeCallDefinition(block, call, life);

    unsigned stateNum = (unsigned)m_resumptionBBs.size();
    JITDUMP("  Assigned state %u\n", stateNum);

    BasicBlock* suspendBB = CreateSuspension(block, stateNum, life, layout);

    CreateCheckAndSuspendAfterCall(block, callDefInfo, life, suspendBB, remainder);

    BasicBlock* resumeBB = CreateResumption(block, *remainder, call, callDefInfo, stateNum, layout);

    m_resumptionBBs.push_back(resumeBB);
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateLiveSetForSuspension:
//   Create the set of live state to be captured for suspension, for the
//   specified call.
//
// Parameters:
//   block        - The block containing the async call
//   call         - The async call
//   defs         - Current live LIR edges
//   life         - Liveness information about live locals
//   liveLocals   - Information about each live local.
//
void AsyncTransformation::CreateLiveSetForSuspension(BasicBlock*                     block,
                                                     GenTreeCall*                    call,
                                                     const jitstd::vector<GenTree*>& defs,
                                                     AsyncLiveness&                  life,
                                                     jitstd::vector<LiveLocalInfo>&  liveLocals)
{
    unsigned fullyDefinedRetBufLcl = BAD_VAR_NUM;
    CallArg* retbufArg             = call->gtArgs.GetRetBufferArg();
    if (retbufArg != nullptr)
    {
        GenTree* retbuf = retbufArg->GetNode();
        if (retbuf->IsLclVarAddr())
        {
            LclVarDsc*   dsc       = m_comp->lvaGetDesc(retbuf->AsLclVarCommon());
            ClassLayout* defLayout = m_comp->typGetObjLayout(call->gtRetClsHnd);
            if (defLayout->GetSize() == dsc->lvExactSize())
            {
                // This call fully defines this retbuf. There is no need to
                // consider it live across the call since it is going to be
                // overridden anyway.
                fullyDefinedRetBufLcl = retbuf->AsLclVarCommon()->GetLclNum();
                JITDUMP("  V%02u is a fully defined retbuf and will not be considered live\n", fullyDefinedRetBufLcl);
            }
        }
    }

    life.GetLiveLocals(liveLocals, fullyDefinedRetBufLcl);
    LiftLIREdges(block, defs, liveLocals);

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  %zu live locals\n", liveLocals.size());

        if (liveLocals.size() > 0)
        {
            const char* sep = "    ";
            for (LiveLocalInfo& inf : liveLocals)
            {
                printf("%sV%02u (%s)", sep, inf.LclNum, varTypeName(m_comp->lvaGetDesc(inf.LclNum)->TypeGet()));
                sep = ", ";
            }

            printf("\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// AsyncTransformation::LiftLIREdges:
//   Create locals capturing outstanding LIR edges and add information
//   indicating that these locals are live.
//
// Parameters:
//   block      - The block containing the definitions of the LIR edges
//   defs       - Current outstanding LIR edges
//   liveLocals - [out] Vector to add new live local information into
//
void AsyncTransformation::LiftLIREdges(BasicBlock*                     block,
                                       const jitstd::vector<GenTree*>& defs,
                                       jitstd::vector<LiveLocalInfo>&  liveLocals)
{
    if (defs.size() <= 0)
    {
        return;
    }

    for (GenTree* tree : defs)
    {
        // TODO-CQ: Enable this. It currently breaks our recognition of how the
        // call is stored.
        // if (tree->OperIs(GT_LCL_VAR))
        //{
        //    LclVarDsc* dsc = m_comp->lvaGetDesc(tree->AsLclVarCommon());
        //    if (!dsc->IsAddressExposed())
        //    {
        //        // No interference by IR invariants.
        //        LIR::AsRange(block).Remove(tree);
        //        LIR::AsRange(block).InsertAfter(beyond, tree);
        //        continue;
        //    }
        //}

        LIR::Use use;
        bool     gotUse = LIR::AsRange(block).TryGetUse(tree, &use);
        assert(gotUse); // Defs list should not contain unused values.

        unsigned newLclNum = use.ReplaceWithLclVar(m_comp);
        liveLocals.push_back(LiveLocalInfo(newLclNum));
        GenTree* newUse = use.Def();
        LIR::AsRange(block).Remove(newUse);
        LIR::AsRange(block).InsertBefore(use.User(), newUse);
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::LayOutContinuation:
//   Create the layout of the GC pointer and data arrays in the continuation
//   object.
//
// Parameters:
//   block      - The block containing the async call
//   call       - The async call
//   liveLocals - [in, out] Information about each live local. Size/alignment
//                information is read and offset/index information is written.
//
// Returns:
//   Layout information.
//
ContinuationLayout AsyncTransformation::LayOutContinuation(BasicBlock*                    block,
                                                           GenTreeCall*                   call,
                                                           jitstd::vector<LiveLocalInfo>& liveLocals)
{
    ContinuationLayout layout(liveLocals);

    for (LiveLocalInfo& inf : liveLocals)
    {
        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            ClassLayout* layout = dsc->GetLayout();
            assert(!layout->HasGCByRef());

            if (layout->IsCustomLayout())
            {
                inf.Alignment   = 1;
                inf.DataSize    = layout->GetSize();
                inf.GCDataCount = layout->GetGCPtrCount();
            }
            else
            {
                inf.Alignment = m_comp->info.compCompHnd->getClassAlignmentRequirement(layout->GetClassHandle());
                if ((layout->GetGCPtrCount() * TARGET_POINTER_SIZE) == layout->GetSize())
                {
                    inf.DataSize = 0;
                }
                else
                {
                    inf.DataSize = layout->GetSize();
                }

                inf.GCDataCount = layout->GetGCPtrCount();
            }
        }
        else if (dsc->TypeIs(TYP_REF))
        {
            inf.Alignment   = TARGET_POINTER_SIZE;
            inf.DataSize    = 0;
            inf.GCDataCount = 1;
        }
        else
        {
            assert(!dsc->TypeIs(TYP_BYREF));

            inf.Alignment   = genTypeAlignments[dsc->TypeGet()];
            inf.DataSize    = genTypeSize(dsc);
            inf.GCDataCount = 0;
        }
    }

    jitstd::sort(liveLocals.begin(), liveLocals.end(), [](const LiveLocalInfo& lhs, const LiveLocalInfo& rhs) {
        if (lhs.Alignment == rhs.Alignment)
        {
            // Prefer lowest local num first for same alignment.
            return lhs.LclNum < rhs.LclNum;
        }

        // Otherwise prefer highest alignment first.
        return lhs.Alignment > rhs.Alignment;
    });

    // For OSR, we store the IL offset that inspired the OSR method at the
    // beginning of the data (-1 in the tier0 version):
    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
    {
        JITDUMP("  Method %s; keeping IL offset that inspired OSR method at the beginning of non-GC data\n",
                m_comp->doesMethodHavePatchpoints() ? "has patchpoints" : "is an OSR method");
        layout.DataSize += sizeof(int);
    }

    if (call->gtReturnType == TYP_STRUCT)
    {
        layout.ReturnStructLayout = m_comp->typGetObjLayout(call->gtRetClsHnd);
        layout.ReturnSize         = layout.ReturnStructLayout->GetSize();
        layout.ReturnInGCData     = layout.ReturnStructLayout->HasGCPtr();
    }
    else
    {
        layout.ReturnSize     = genTypeSize(call->gtReturnType);
        layout.ReturnInGCData = varTypeIsGC(call->gtReturnType);
    }

    assert((layout.ReturnSize > 0) == (call->gtReturnType != TYP_VOID));

    // The return value is always stored:
    // 1. At index 0 in GCData if it is a TYP_REF or a struct with GC references
    // 2. At index 0 in Data, for non OSR methods without GC ref returns
    // 3. At index 4 in Data for OSR methods without GC ref returns. The
    // continuation flags indicates this scenario with a flag.
    if (layout.ReturnInGCData)
    {
        layout.GCRefsCount++;
    }
    else if (layout.ReturnSize > 0)
    {
        layout.ReturnValDataOffset = layout.DataSize;
        layout.DataSize += layout.ReturnSize;
    }

#ifdef DEBUG
    if (layout.ReturnSize > 0)
    {
        JITDUMP("  Will store return of type %s, size %u in",
                call->gtReturnType == TYP_STRUCT ? layout.ReturnStructLayout->GetClassName()
                                                 : varTypeName(call->gtReturnType),
                layout.ReturnSize);

        if (layout.ReturnInGCData)
        {
            JITDUMP(" GC data\n");
        }
        else
        {
            JITDUMP(" non-GC data at offset %u\n", layout.ReturnValDataOffset);
        }
    }
#endif

    if (block->hasTryIndex())
    {
        layout.ExceptionGCDataIndex = layout.GCRefsCount++;
        JITDUMP("  " FMT_BB " is in try region %u; exception will be at GC@+%02u in GC data\n", block->bbNum,
                block->getTryIndex(), layout.ExceptionGCDataIndex);
    }

    for (LiveLocalInfo& inf : liveLocals)
    {
        layout.DataSize = roundUp(layout.DataSize, inf.Alignment);

        inf.DataOffset  = layout.DataSize;
        inf.GCDataIndex = layout.GCRefsCount;

        layout.DataSize += inf.DataSize;
        layout.GCRefsCount += inf.GCDataCount;
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  Continuation layout (%u bytes, %u GC pointers):\n", layout.DataSize, layout.GCRefsCount);
        for (LiveLocalInfo& inf : liveLocals)
        {
            printf("    +%03u (GC@+%02u) V%02u: %u bytes, %u GC pointers\n", inf.DataOffset, inf.GCDataIndex,
                   inf.LclNum, inf.DataSize, inf.GCDataCount);
        }
    }
#endif

    return layout;
}

//------------------------------------------------------------------------
// AsyncTransformation::CanonicalizeCallDefinition:
//   Put the call definition in a canonical form. This ensures that either the
//   value is defined by a LCL_ADDR retbuffer or by a
//   STORE_LCL_VAR/STORE_LCL_FLD that follows the call node.
//
// Parameters:
//   block - The block containing the async call
//   call  - The async call
//   life  - Liveness information about live locals
//
// Returns:
//   Information about the definition after canonicalization.
//
CallDefinitionInfo AsyncTransformation::CanonicalizeCallDefinition(BasicBlock*    block,
                                                                   GenTreeCall*   call,
                                                                   AsyncLiveness& life)
{
    CallDefinitionInfo callDefInfo;

    callDefInfo.InsertAfter = call;

    CallArg* retbufArg = call->gtArgs.GetRetBufferArg();

    if (!call->TypeIs(TYP_VOID) && !call->IsUnusedValue())
    {
        assert(retbufArg == nullptr);
        assert(call->gtNext != nullptr);
        if (!call->gtNext->OperIsLocalStore() || (call->gtNext->Data() != call))
        {
            LIR::Use use;
            bool     gotUse = LIR::AsRange(block).TryGetUse(call, &use);
            assert(gotUse);

            use.ReplaceWithLclVar(m_comp);
        }
        else
        {
            // We will split after the store, but we still have to update liveness for it.
            life.Update(call->gtNext);
        }

        assert(call->gtNext->OperIsLocalStore() && (call->gtNext->Data() == call));
        callDefInfo.DefinitionNode = call->gtNext->AsLclVarCommon();
        callDefInfo.InsertAfter    = call->gtNext;
    }

    if (retbufArg != nullptr)
    {
        assert(call->TypeIs(TYP_VOID));

        // For async methods we always expect retbufs to point to locals. We
        // ensure this in impStoreStruct.
        noway_assert(retbufArg->GetNode()->OperIs(GT_LCL_ADDR));

        callDefInfo.DefinitionNode = retbufArg->GetNode()->AsLclVarCommon();
    }

    return callDefInfo;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateSuspension:
//   Create the basic block that when branched to suspends execution after the
//   specified async call.
//
// Parameters:
//   block    - The block containing the async call
//   stateNum - State number assigned to this suspension point
//   life     - Liveness information about live locals
//   layout   - Layout information for the continuation object
//
// Returns:
//   The new basic block that was created.
//
BasicBlock* AsyncTransformation::CreateSuspension(BasicBlock*               block,
                                                  unsigned                  stateNum,
                                                  AsyncLiveness&            life,
                                                  const ContinuationLayout& layout)
{
    if (m_lastSuspensionBB == nullptr)
    {
        m_lastSuspensionBB = m_comp->fgLastBBInMainFunction();
    }

    BasicBlock* suspendBB = m_comp->fgNewBBafter(BBJ_RETURN, m_lastSuspensionBB, false);
    suspendBB->clearTryIndex();
    suspendBB->clearHndIndex();
    suspendBB->inheritWeightPercentage(block, 0);
    m_lastSuspensionBB = suspendBB;

    if (m_sharedReturnBB != nullptr)
    {
        suspendBB->SetKindAndTargetEdge(BBJ_ALWAYS, m_comp->fgAddRefPred(m_sharedReturnBB, suspendBB));
    }

    JITDUMP("  Creating suspension " FMT_BB " for state %u\n", suspendBB->bbNum, stateNum);

    // Allocate continuation
    GenTree* returnedContinuation = m_comp->gtNewLclvNode(m_returnedContinuationVar, TYP_REF);

    GenTreeCall* allocContinuation =
        CreateAllocContinuationCall(life, returnedContinuation, layout.GCRefsCount, layout.DataSize);

    m_comp->compCurBB = suspendBB;
    m_comp->fgMorphTree(allocContinuation);

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, allocContinuation));

    GenTree* storeNewContinuation = m_comp->gtNewStoreLclVarNode(m_newContinuationVar, allocContinuation);
    LIR::AsRange(suspendBB).InsertAtEnd(storeNewContinuation);

    // Fill in 'Resume'
    GenTree* newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned resumeOffset    = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationResumeFldHnd);
    GenTree* resumeStubAddr  = CreateResumptionStubAddrTree();
    GenTree* storeResume     = StoreAtOffset(newContinuation, resumeOffset, resumeStubAddr, TYP_I_IMPL);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResume));

    // Fill in 'state'
    newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned stateOffset  = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationStateFldHnd);
    GenTree* stateNumNode = m_comp->gtNewIconNode((ssize_t)stateNum, TYP_INT);
    GenTree* storeState   = StoreAtOffset(newContinuation, stateOffset, stateNumNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeState));

    // Fill in 'flags'
    unsigned continuationFlags = 0;
    if (layout.ReturnInGCData)
        continuationFlags |= CORINFO_CONTINUATION_RESULT_IN_GCDATA;
    if (block->hasTryIndex())
        continuationFlags |= CORINFO_CONTINUATION_NEEDS_EXCEPTION;
    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
        continuationFlags |= CORINFO_CONTINUATION_OSR_IL_OFFSET_IN_DATA;

    newContinuation      = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned flagsOffset = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationFlagsFldHnd);
    GenTree* flagsNode   = m_comp->gtNewIconNode((ssize_t)continuationFlags, TYP_INT);
    GenTree* storeFlags  = StoreAtOffset(newContinuation, flagsOffset, flagsNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeFlags));

    if (layout.GCRefsCount > 0)
    {
        FillInGCPointersOnSuspension(layout.Locals, suspendBB);
    }

    if (layout.DataSize > 0)
    {
        FillInDataOnSuspension(layout.Locals, suspendBB);
    }

    if (suspendBB->KindIs(BBJ_RETURN))
    {
        newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        GenTree* ret    = m_comp->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, newContinuation);
        LIR::AsRange(suspendBB).InsertAtEnd(newContinuation, ret);
    }

    return suspendBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateAllocContinuationCall:
//   Create a call to the JIT helper that allocates a continuation.
//
// Parameters:
//   life             - Liveness information about live locals
//   prevContinuation - IR node that has the value of the previous continuation object
//   gcRefsCount      - Number of GC refs to allocate in the continuation object
//   dataSize         - Number of bytes to allocate in the continuation object
//
// Returns:
//   IR node representing the allocation.
//
GenTreeCall* AsyncTransformation::CreateAllocContinuationCall(AsyncLiveness& life,
                                                              GenTree*       prevContinuation,
                                                              unsigned       gcRefsCount,
                                                              unsigned       dataSize)
{
    GenTree* gcRefsCountNode = m_comp->gtNewIconNode((ssize_t)gcRefsCount, TYP_I_IMPL);
    GenTree* dataSizeNode    = m_comp->gtNewIconNode((ssize_t)dataSize, TYP_I_IMPL);
    // If VM requests that we report the method handle, or if we have a shared generic context method handle
    // that is live here, then we need to call a different helper to keep the loader alive.
    GenTree* methodHandleArg = nullptr;
    GenTree* classHandleArg  = nullptr;
    if (((m_comp->info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_METHODDESC) != 0) &&
        life.IsLive(m_comp->info.compTypeCtxtArg))
    {
        methodHandleArg = m_comp->gtNewLclvNode(m_comp->info.compTypeCtxtArg, TYP_I_IMPL);
    }
    else if (((m_comp->info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_METHODTABLE) != 0) &&
             life.IsLive(m_comp->info.compTypeCtxtArg))
    {
        classHandleArg = m_comp->gtNewLclvNode(m_comp->info.compTypeCtxtArg, TYP_I_IMPL);
    }
    else if (m_asyncInfo.continuationsNeedMethodHandle)
    {
        methodHandleArg = m_comp->gtNewIconEmbMethHndNode(m_comp->info.compMethodHnd);
    }

    if (methodHandleArg != nullptr)
    {
        return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION_METHOD, TYP_REF, prevContinuation,
                                           gcRefsCountNode, dataSizeNode, methodHandleArg);
    }

    if (classHandleArg != nullptr)
    {
        return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION_CLASS, TYP_REF, prevContinuation,
                                           gcRefsCountNode, dataSizeNode, classHandleArg);
    }

    return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION, TYP_REF, prevContinuation, gcRefsCountNode,
                                       dataSizeNode);
}

//------------------------------------------------------------------------
// AsyncTransformation::FillInGCPointersOnSuspension:
//   Create IR that fills the GC pointers of the continuation object.
//   This also nulls out the GC pointers in the locals if the local has data
//   parts that need to be stored.
//
// Parameters:
//   liveLocals - Information about each live local.
//   suspendBB  - Basic block to add IR to.
//
void AsyncTransformation::FillInGCPointersOnSuspension(const jitstd::vector<LiveLocalInfo>& liveLocals,
                                                       BasicBlock*                          suspendBB)
{
    unsigned objectArrLclNum = GetGCDataArrayVar();

    GenTree* newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned gcDataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationGCDataFldHnd);
    GenTree* gcDataInd             = LoadFromOffset(newContinuation, gcDataOffset, TYP_REF);
    GenTree* storeAllocedObjectArr = m_comp->gtNewStoreLclVarNode(objectArrLclNum, gcDataInd);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeAllocedObjectArr));

    for (const LiveLocalInfo& inf : liveLocals)
    {
        if (inf.GCDataCount <= 0)
        {
            continue;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);
        if (dsc->TypeIs(TYP_REF))
        {
            GenTree* value     = m_comp->gtNewLclvNode(inf.LclNum, TYP_REF);
            GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
            GenTree* store =
                StoreAtOffset(objectArr, OFFSETOF__CORINFO_Array__data + (inf.GCDataIndex * TARGET_POINTER_SIZE), value,
                              TYP_REF);
            LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
        }
        else
        {
            assert(dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef());
            ClassLayout* layout     = dsc->GetLayout();
            unsigned     numSlots   = layout->GetSlotCount();
            unsigned     gcRefIndex = 0;
            for (unsigned i = 0; i < numSlots; i++)
            {
                var_types gcPtrType = layout->GetGCPtrType(i);
                assert((gcPtrType == TYP_I_IMPL) || (gcPtrType == TYP_REF));
                if (gcPtrType != TYP_REF)
                {
                    continue;
                }

                GenTree* value;
                if (dsc->IsImplicitByRef())
                {
                    GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                    value             = LoadFromOffset(baseAddr, i * TARGET_POINTER_SIZE, TYP_REF);
                }
                else
                {
                    value = m_comp->gtNewLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE);
                }

                GenTree* objectArr = m_comp->gtNewLclvNode(objectArrLclNum, TYP_REF);
                unsigned offset =
                    OFFSETOF__CORINFO_Array__data + ((inf.GCDataIndex + gcRefIndex) * TARGET_POINTER_SIZE);
                GenTree* store = StoreAtOffset(objectArr, offset, value, TYP_REF);
                LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, store));

                gcRefIndex++;

                if (inf.DataSize > 0)
                {
                    // Null out the GC field in preparation of storing the rest.
                    GenTree* null = m_comp->gtNewNull();

                    if (dsc->IsImplicitByRef())
                    {
                        GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                        store             = StoreAtOffset(baseAddr, i * TARGET_POINTER_SIZE, null, TYP_REF);
                    }
                    else
                    {
                        store = m_comp->gtNewStoreLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE, null);
                    }

                    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
                }
            }

            if (!dsc->IsImplicitByRef())
            {
                m_comp->lvaSetVarDoNotEnregister(inf.LclNum DEBUGARG(DoNotEnregisterReason::LocalField));
            }
        }
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::FillInDataOnSuspension:
//   Create IR that fills the data array of the continuation object.
//
// Parameters:
//   liveLocals - Information about each live local.
//   suspendBB  - Basic block to add IR to.
//
void AsyncTransformation::FillInDataOnSuspension(const jitstd::vector<LiveLocalInfo>& liveLocals, BasicBlock* suspendBB)
{
    unsigned byteArrLclNum = GetDataArrayVar();

    GenTree* newContinuation     = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned dataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationDataFldHnd);
    GenTree* dataInd             = LoadFromOffset(newContinuation, dataOffset, TYP_REF);
    GenTree* storeAllocedByteArr = m_comp->gtNewStoreLclVarNode(byteArrLclNum, dataInd);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeAllocedByteArr));

    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
    {
        GenTree* ilOffsetToStore;
        if (m_comp->doesMethodHavePatchpoints())
            ilOffsetToStore = m_comp->gtNewIconNode(-1);
        else
            ilOffsetToStore = m_comp->gtNewIconNode((int)m_comp->info.compILEntry);

        GenTree* byteArr               = m_comp->gtNewLclvNode(byteArrLclNum, TYP_REF);
        unsigned offset                = OFFSETOF__CORINFO_Array__data;
        GenTree* storePatchpointOffset = StoreAtOffset(byteArr, offset, ilOffsetToStore, TYP_INT);
        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storePatchpointOffset));
    }

    // Fill in data
    for (const LiveLocalInfo& inf : liveLocals)
    {
        if (inf.DataSize <= 0)
        {
            continue;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        GenTree* byteArr = m_comp->gtNewLclvNode(byteArrLclNum, TYP_REF);
        unsigned offset  = OFFSETOF__CORINFO_Array__data + inf.DataOffset;

        GenTree* value;
        if (dsc->IsImplicitByRef())
        {
            GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
            value             = m_comp->gtNewLoadValueNode(dsc->GetLayout(), baseAddr, GTF_IND_NONFAULTING);
        }
        else
        {
            value = m_comp->gtNewLclVarNode(inf.LclNum);
        }

        GenTree* store;
        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            GenTree* cns  = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
            GenTree* addr = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, byteArr, cns);
            // This is to heap, but all GC refs are nulled out already, so we can skip the write barrier.
            // TODO-CQ: Backend does not care about GTF_IND_TGT_NOT_HEAP for STORE_BLK.
            store =
                m_comp->gtNewStoreValueNode(dsc->GetLayout(), addr, value, GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP);
        }
        else
        {
            store = StoreAtOffset(byteArr, offset, value, dsc->TypeGet());
        }

        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateCheckAndSuspendAfterCall:
//   Split the block containing the specified async call, and create the IR
//   that checks whether suspension should be done after an async call.
//
// Parameters:
//   block       - The block containing the async call
//   callDefInfo - Information about the async call's definition
//   life        - Liveness information about live locals
//   suspendBB   - Basic block to add IR to
//   remainder   - [out] The remainder block containing the IR that was after the async call.
//
void AsyncTransformation::CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                         const CallDefinitionInfo& callDefInfo,
                                                         AsyncLiveness&            life,
                                                         BasicBlock*               suspendBB,
                                                         BasicBlock**              remainder)
{
    GenTree* continuationArg = new (m_comp, GT_ASYNC_CONTINUATION) GenTree(GT_ASYNC_CONTINUATION, TYP_REF);
    continuationArg->SetHasOrderingSideEffect();

    GenTree* storeContinuation = m_comp->gtNewStoreLclVarNode(m_returnedContinuationVar, continuationArg);
    LIR::AsRange(block).InsertAfter(callDefInfo.InsertAfter, continuationArg, storeContinuation);

    GenTree* null                 = m_comp->gtNewNull();
    GenTree* returnedContinuation = m_comp->gtNewLclvNode(m_returnedContinuationVar, TYP_REF);
    GenTree* neNull               = m_comp->gtNewOperNode(GT_NE, TYP_INT, returnedContinuation, null);
    GenTree* jtrue                = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);

    LIR::AsRange(block).InsertAfter(storeContinuation, null, returnedContinuation, neNull, jtrue);
    *remainder = m_comp->fgSplitBlockAfterNode(block, jtrue);
    JITDUMP("  Remainder is " FMT_BB "\n", (*remainder)->bbNum);

    FlowEdge* retBBEdge = m_comp->fgAddRefPred(suspendBB, block);
    block->SetCond(retBBEdge, block->GetTargetEdge());

    block->GetTrueEdge()->setLikelihood(0);
    block->GetFalseEdge()->setLikelihood(1);
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumption:
//   Create the basic block that when branched to resumes execution on entry to
//   the function.
//
// Parameters:
//   block       - The block containing the async call
//   remainder   - The block that contains the IR after the (split) async call
//   call        - The async call
//   callDefInfo - Information about the async call's definition
//   stateNum    - State number assigned to this suspension point
//   layout      - Layout information for the continuation object
//
// Returns:
//   The new basic block that was created.
//
BasicBlock* AsyncTransformation::CreateResumption(BasicBlock*               block,
                                                  BasicBlock*               remainder,
                                                  GenTreeCall*              call,
                                                  const CallDefinitionInfo& callDefInfo,
                                                  unsigned                  stateNum,
                                                  const ContinuationLayout& layout)
{
    if (m_lastResumptionBB == nullptr)
    {
        m_lastResumptionBB = m_comp->fgLastBBInMainFunction();
    }

    BasicBlock* resumeBB      = m_comp->fgNewBBafter(BBJ_ALWAYS, m_lastResumptionBB, true);
    FlowEdge*   remainderEdge = m_comp->fgAddRefPred(remainder, resumeBB);

    // It does not really make sense to inherit from the target, but given this
    // is always 0% this just propagates the profile weight flag + sets
    // BBF_RUN_RARELY.
    resumeBB->inheritWeightPercentage(remainder, 0);
    resumeBB->SetTargetEdge(remainderEdge);
    resumeBB->clearTryIndex();
    resumeBB->clearHndIndex();
    resumeBB->SetFlags(BBF_ASYNC_RESUMPTION);
    m_lastResumptionBB = resumeBB;

    JITDUMP("  Creating resumption " FMT_BB " for state %u\n", resumeBB->bbNum, stateNum);

    // We need to restore data before we restore GC pointers, since restoring
    // the data may also write the GC pointer fields with nulls.
    unsigned resumeByteArrLclNum = BAD_VAR_NUM;
    if (layout.DataSize > 0)
    {
        resumeByteArrLclNum = GetDataArrayVar();

        GenTree* newContinuation     = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned dataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationDataFldHnd);
        GenTree* dataInd             = LoadFromOffset(newContinuation, dataOffset, TYP_REF);
        GenTree* storeAllocedByteArr = m_comp->gtNewStoreLclVarNode(resumeByteArrLclNum, dataInd);

        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeAllocedByteArr));

        RestoreFromDataOnResumption(resumeByteArrLclNum, layout.Locals, resumeBB);
    }

    unsigned    resumeObjectArrLclNum = BAD_VAR_NUM;
    BasicBlock* storeResultBB         = resumeBB;

    if (layout.GCRefsCount > 0)
    {
        resumeObjectArrLclNum = GetGCDataArrayVar();

        GenTree* newContinuation       = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned gcDataOffset          = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationGCDataFldHnd);
        GenTree* gcDataInd             = LoadFromOffset(newContinuation, gcDataOffset, TYP_REF);
        GenTree* storeAllocedObjectArr = m_comp->gtNewStoreLclVarNode(resumeObjectArrLclNum, gcDataInd);
        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeAllocedObjectArr));

        RestoreFromGCPointersOnResumption(resumeObjectArrLclNum, layout.Locals, resumeBB);

        if (layout.ExceptionGCDataIndex != UINT_MAX)
        {
            storeResultBB = RethrowExceptionOnResumption(block, remainder, resumeObjectArrLclNum, layout, resumeBB);
        }
    }

    // Copy call return value.
    if ((layout.ReturnSize > 0) && (callDefInfo.DefinitionNode != nullptr))
    {
        CopyReturnValueOnResumption(call, callDefInfo, resumeByteArrLclNum, resumeObjectArrLclNum, layout,
                                    storeResultBB);
    }

    return resumeBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::RestoreFromDataOnResumption:
//   Create IR that restores locals from the data array of the continuation
//   object.
//
// Parameters:
//   resumeByteArrLclNum - Local that has the continuation object's data array
//   liveLocals          - Information about each live local.
//   resumeBB            - Basic block to append IR to
//
void AsyncTransformation::RestoreFromDataOnResumption(unsigned                             resumeByteArrLclNum,
                                                      const jitstd::vector<LiveLocalInfo>& liveLocals,
                                                      BasicBlock*                          resumeBB)
{
    // Copy data
    for (const LiveLocalInfo& inf : liveLocals)
    {
        if (inf.DataSize <= 0)
        {
            continue;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        GenTree* byteArr = m_comp->gtNewLclvNode(resumeByteArrLclNum, TYP_REF);
        unsigned offset  = OFFSETOF__CORINFO_Array__data + inf.DataOffset;
        GenTree* cns     = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
        GenTree* addr    = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, byteArr, cns);

        GenTree* value;
        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            value = m_comp->gtNewLoadValueNode(dsc->GetLayout(), addr, GTF_IND_NONFAULTING);
        }
        else
        {
            value = m_comp->gtNewIndir(dsc->TypeGet(), addr, GTF_IND_NONFAULTING);
        }

        GenTree* store;
        if (dsc->IsImplicitByRef())
        {
            GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
            store             = m_comp->gtNewStoreValueNode(dsc->GetLayout(), baseAddr, value,
                                                            GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP);
        }
        else
        {
            store = m_comp->gtNewStoreLclVarNode(inf.LclNum, value);
        }

        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::RestoreFromGCPointersOnResumption:
//   Create IR that restores locals from the GC pointers array of the
//   continuation object.
//
// Parameters:
//   resumeObjectArrLclNum - Local that has the continuation object's GC pointers array
//   liveLocals            - Information about each live local.
//   resumeBB              - Basic block to append IR to
//
void AsyncTransformation::RestoreFromGCPointersOnResumption(unsigned                             resumeObjectArrLclNum,
                                                            const jitstd::vector<LiveLocalInfo>& liveLocals,
                                                            BasicBlock*                          resumeBB)
{
    for (const LiveLocalInfo& inf : liveLocals)
    {
        if (inf.GCDataCount <= 0)
        {
            continue;
        }

        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);
        if (dsc->TypeIs(TYP_REF))
        {
            GenTree* objectArr = m_comp->gtNewLclvNode(resumeObjectArrLclNum, TYP_REF);
            unsigned offset    = OFFSETOF__CORINFO_Array__data + (inf.GCDataIndex * TARGET_POINTER_SIZE);
            GenTree* value     = LoadFromOffset(objectArr, offset, TYP_REF);
            GenTree* store     = m_comp->gtNewStoreLclVarNode(inf.LclNum, value);

            LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
        }
        else
        {
            assert(dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef());
            ClassLayout* layout     = dsc->GetLayout();
            unsigned     numSlots   = layout->GetSlotCount();
            unsigned     gcRefIndex = 0;
            for (unsigned i = 0; i < numSlots; i++)
            {
                var_types gcPtrType = layout->GetGCPtrType(i);
                assert((gcPtrType == TYP_I_IMPL) || (gcPtrType == TYP_REF));
                if (gcPtrType != TYP_REF)
                {
                    continue;
                }

                GenTree* objectArr = m_comp->gtNewLclvNode(resumeObjectArrLclNum, TYP_REF);
                unsigned offset =
                    OFFSETOF__CORINFO_Array__data + ((inf.GCDataIndex + gcRefIndex) * TARGET_POINTER_SIZE);
                GenTree* value = LoadFromOffset(objectArr, offset, TYP_REF);
                GenTree* store;
                if (dsc->IsImplicitByRef())
                {
                    GenTree* baseAddr = m_comp->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
                    store             = StoreAtOffset(baseAddr, i * TARGET_POINTER_SIZE, value, TYP_REF);
                    // Implicit byref args are never on heap
                    store->gtFlags |= GTF_IND_TGT_NOT_HEAP;
                }
                else
                {
                    store = m_comp->gtNewStoreLclFldNode(inf.LclNum, TYP_REF, i * TARGET_POINTER_SIZE, value);
                }

                LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, store));

                gcRefIndex++;
            }
        }
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::RethrowExceptionOnResumption:
//   Create IR that checks for an exception and rethrows it at the original
//   suspension point if necessary.
//
// Parameters:
//   block                 - The block containing the async call
//   remainder             - The block that contains the IR after the (split) async call
//   resumeObjectArrLclNum - Local that has the continuation object's GC pointers array
//   layout                - Layout information for the continuation object
//   resumeBB              - Basic block to append IR to
//
// Returns:
//   The new non-exception successor basic block for resumption. This is the
//   basic block where execution will continue if there was no exception to
//   rethrow.
//
BasicBlock* AsyncTransformation::RethrowExceptionOnResumption(BasicBlock*               block,
                                                              BasicBlock*               remainder,
                                                              unsigned                  resumeObjectArrLclNum,
                                                              const ContinuationLayout& layout,
                                                              BasicBlock*               resumeBB)
{
    JITDUMP("  We need to rethrow an exception\n");

    BasicBlock* rethrowExceptionBB =
        m_comp->fgNewBBinRegion(BBJ_THROW, block, /* runRarely */ true, /* insertAtEnd */ true);
    JITDUMP("  Created " FMT_BB " to rethrow exception on resumption\n", rethrowExceptionBB->bbNum);

    BasicBlock* storeResultBB = m_comp->fgNewBBafter(BBJ_ALWAYS, resumeBB, true);
    JITDUMP("  Created " FMT_BB " to store result when resuming with no exception\n", storeResultBB->bbNum);

    FlowEdge* rethrowEdge     = m_comp->fgAddRefPred(rethrowExceptionBB, resumeBB);
    FlowEdge* storeResultEdge = m_comp->fgAddRefPred(storeResultBB, resumeBB);

    assert(resumeBB->KindIs(BBJ_ALWAYS));
    m_comp->fgRemoveRefPred(resumeBB->GetTargetEdge());

    resumeBB->SetCond(rethrowEdge, storeResultEdge);
    rethrowEdge->setLikelihood(0);
    storeResultEdge->setLikelihood(1);
    rethrowExceptionBB->inheritWeightPercentage(resumeBB, 0);
    storeResultBB->inheritWeightPercentage(resumeBB, 100);
    JITDUMP("  Resumption " FMT_BB " becomes BBJ_COND to check for non-null exception\n", resumeBB->bbNum);

    FlowEdge* remainderEdge = m_comp->fgAddRefPred(remainder, storeResultBB);
    storeResultBB->SetTargetEdge(remainderEdge);

    m_lastResumptionBB = storeResultBB;

    // Check if we have an exception.
    unsigned exceptionLclNum = GetExceptionVar();
    GenTree* objectArr       = m_comp->gtNewLclvNode(resumeObjectArrLclNum, TYP_REF);
    unsigned exceptionOffset = OFFSETOF__CORINFO_Array__data + layout.ExceptionGCDataIndex * TARGET_POINTER_SIZE;
    GenTree* exceptionInd    = LoadFromOffset(objectArr, exceptionOffset, TYP_REF);
    GenTree* storeException  = m_comp->gtNewStoreLclVarNode(exceptionLclNum, exceptionInd);
    LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, storeException));

    GenTree* exception = m_comp->gtNewLclVarNode(exceptionLclNum, TYP_REF);
    GenTree* null      = m_comp->gtNewNull();
    GenTree* neNull    = m_comp->gtNewOperNode(GT_NE, TYP_INT, exception, null);
    GenTree* jtrue     = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
    LIR::AsRange(resumeBB).InsertAtEnd(exception, null, neNull, jtrue);

    exception = m_comp->gtNewLclVarNode(exceptionLclNum, TYP_REF);

    GenTreeCall* rethrowException = m_comp->gtNewHelperCallNode(CORINFO_HELP_THROWEXACT, TYP_VOID, exception);

    m_comp->compCurBB = rethrowExceptionBB;
    m_comp->fgMorphTree(rethrowException);

    LIR::AsRange(rethrowExceptionBB).InsertAtEnd(LIR::SeqTree(m_comp, rethrowException));

    storeResultBB->SetFlags(BBF_ASYNC_RESUMPTION);
    JITDUMP("  Added " FMT_BB " to rethrow exception at suspension point\n", rethrowExceptionBB->bbNum);

    return storeResultBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::CopyReturnValueOnResumption:
//   Create IR that copies the return value from the continuation object to the
//   right local.
//
// Parameters:
//   call                  - The async call
//   callDefInfo           - Information about the async call's definition
//   block                 - The block containing the async call
//   resumeByteArrLclNum   - Local that has the continuation object's data array
//   resumeObjectArrLclNum - Local that has the continuation object's GC pointers array
//   layout                - Layout information for the continuation object
//   storeResultBB         - Basic block to append IR to
//
void AsyncTransformation::CopyReturnValueOnResumption(GenTreeCall*              call,
                                                      const CallDefinitionInfo& callDefInfo,
                                                      unsigned                  resumeByteArrLclNum,
                                                      unsigned                  resumeObjectArrLclNum,
                                                      const ContinuationLayout& layout,
                                                      BasicBlock*               storeResultBB)
{
    GenTree*     resultBase;
    unsigned     resultOffset;
    GenTreeFlags resultIndirFlags = GTF_IND_NONFAULTING;
    if (layout.ReturnInGCData)
    {
        assert(resumeObjectArrLclNum != BAD_VAR_NUM);
        resultBase = m_comp->gtNewLclvNode(resumeObjectArrLclNum, TYP_REF);

        if (call->gtReturnType == TYP_STRUCT)
        {
            // Boxed struct.
            resultBase   = LoadFromOffset(resultBase, OFFSETOF__CORINFO_Array__data, TYP_REF);
            resultOffset = TARGET_POINTER_SIZE; // Offset of data inside box
        }
        else
        {
            assert(call->gtReturnType == TYP_REF);
            resultOffset = OFFSETOF__CORINFO_Array__data;
        }
    }
    else
    {
        assert(resumeByteArrLclNum != BAD_VAR_NUM);
        resultBase   = m_comp->gtNewLclvNode(resumeByteArrLclNum, TYP_REF);
        resultOffset = OFFSETOF__CORINFO_Array__data + layout.ReturnValDataOffset;
        if (layout.ReturnValDataOffset != 0)
            resultIndirFlags = GTF_IND_UNALIGNED;
    }

    assert(callDefInfo.DefinitionNode != nullptr);
    LclVarDsc* resultLcl = m_comp->lvaGetDesc(callDefInfo.DefinitionNode);

    // TODO-TP: We can use liveness to avoid generating a lot of this IR.
    if (call->gtReturnType == TYP_STRUCT)
    {
        if (m_comp->lvaGetPromotionType(resultLcl) != Compiler::PROMOTION_TYPE_INDEPENDENT)
        {
            GenTree* resultOffsetNode = m_comp->gtNewIconNode((ssize_t)resultOffset, TYP_I_IMPL);
            GenTree* resultAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, resultBase, resultOffsetNode);
            GenTree* resultData = m_comp->gtNewLoadValueNode(layout.ReturnStructLayout, resultAddr, resultIndirFlags);
            GenTree* storeResult;
            if ((callDefInfo.DefinitionNode->GetLclOffs() == 0) &&
                ClassLayout::AreCompatible(resultLcl->GetLayout(), layout.ReturnStructLayout))
            {
                storeResult = m_comp->gtNewStoreLclVarNode(callDefInfo.DefinitionNode->GetLclNum(), resultData);
            }
            else
            {
                storeResult = m_comp->gtNewStoreLclFldNode(callDefInfo.DefinitionNode->GetLclNum(), TYP_STRUCT,
                                                           layout.ReturnStructLayout,
                                                           callDefInfo.DefinitionNode->GetLclOffs(), resultData);
            }

            LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResult));
        }
        else
        {
            assert(!call->gtArgs.HasRetBuffer()); // Locals defined through retbufs are never independently promoted.

            if ((resultLcl->lvFieldCnt > 1) && !resultBase->OperIsLocal())
            {
                unsigned resultBaseVar   = GetResultBaseVar();
                GenTree* storeResultBase = m_comp->gtNewStoreLclVarNode(resultBaseVar, resultBase);
                LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResultBase));

                resultBase = m_comp->gtNewLclVarNode(resultBaseVar, TYP_REF);
            }

            assert(callDefInfo.DefinitionNode->OperIs(GT_STORE_LCL_VAR));
            for (unsigned i = 0; i < resultLcl->lvFieldCnt; i++)
            {
                unsigned   fieldLclNum = resultLcl->lvFieldLclStart + i;
                LclVarDsc* fieldDsc    = m_comp->lvaGetDesc(fieldLclNum);

                unsigned fldOffset = resultOffset + fieldDsc->lvFldOffset;
                GenTree* value     = LoadFromOffset(resultBase, fldOffset, fieldDsc->TypeGet(), resultIndirFlags);
                GenTree* store     = m_comp->gtNewStoreLclVarNode(fieldLclNum, value);
                LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_comp, store));

                if (i + 1 != resultLcl->lvFieldCnt)
                {
                    resultBase = m_comp->gtCloneExpr(resultBase);
                }
            }
        }
    }
    else
    {
        GenTree* value = LoadFromOffset(resultBase, resultOffset, call->gtReturnType, resultIndirFlags);

        GenTree* storeResult;
        if (callDefInfo.DefinitionNode->OperIs(GT_STORE_LCL_VAR))
        {
            storeResult = m_comp->gtNewStoreLclVarNode(callDefInfo.DefinitionNode->GetLclNum(), value);
        }
        else
        {
            storeResult = m_comp->gtNewStoreLclFldNode(callDefInfo.DefinitionNode->GetLclNum(),
                                                       callDefInfo.DefinitionNode->TypeGet(),
                                                       callDefInfo.DefinitionNode->GetLclOffs(), value);
        }

        LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResult));
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::LoadFromOffset:
//   Create a load.
//
// Parameters:
//   base       - Base address of the load
//   offset     - Offset to add on top of the base address
//   type       - Type of the load to create
//   indirFlags - Flags to add to the load
//
// Returns:
//   IR node of the load.
//
GenTreeIndir* AsyncTransformation::LoadFromOffset(GenTree*     base,
                                                  unsigned     offset,
                                                  var_types    type,
                                                  GenTreeFlags indirFlags)
{
    assert(base->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL));
    GenTree*      cns      = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types     addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*      addr     = m_comp->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeIndir* load     = m_comp->gtNewIndir(type, addr, indirFlags);
    return load;
}

//------------------------------------------------------------------------
// AsyncTransformation::StoreAtOffset:
//   Create a store.
//
// Parameters:
//   base       - Base address of the store
//   offset     - Offset to add on top of the base address
//   value      - Value to store
//   storeType  - Type of store
//
// Returns:
//   IR node of the store.
//
GenTreeStoreInd* AsyncTransformation::StoreAtOffset(GenTree* base, unsigned offset, GenTree* value, var_types storeType)
{
    assert(base->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL));
    GenTree*         cns      = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types        addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*         addr     = m_comp->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeStoreInd* store    = m_comp->gtNewStoreIndNode(storeType, addr, value, GTF_IND_NONFAULTING);
    return store;
}

//------------------------------------------------------------------------
// AsyncTransformation::GetDataArrayVar:
//   Create a new local to hold the data array of the continuation object. This
//   local can be validly used for the entire suspension point; the returned
//   local may be used by multiple suspension points.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetDataArrayVar()
{
    // Create separate locals unless we have many locals in the method for live
    // range splitting purposes. This helps LSRA to avoid create additional
    // callee saves that harm the prolog/epilog.
    if ((m_dataArrayVar == BAD_VAR_NUM) || !m_comp->lvaHaveManyLocals())
    {
        m_dataArrayVar                             = m_comp->lvaGrabTemp(false DEBUGARG("byte[] for continuation"));
        m_comp->lvaGetDesc(m_dataArrayVar)->lvType = TYP_REF;
    }

    return m_dataArrayVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::GetGCDataArrayVar:
//   Create a new local to hold the GC pointers array of the continuation
//   object. This local can be validly used for the entire suspension point;
//   the returned local may be used by multiple suspension points.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetGCDataArrayVar()
{
    if ((m_gcDataArrayVar == BAD_VAR_NUM) || !m_comp->lvaHaveManyLocals())
    {
        m_gcDataArrayVar                             = m_comp->lvaGrabTemp(false DEBUGARG("object[] for continuation"));
        m_comp->lvaGetDesc(m_gcDataArrayVar)->lvType = TYP_REF;
    }

    return m_gcDataArrayVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::GetResultBaseVar:
//   Create a new local to hold the base address of the incoming result from
//   the continuation. This local can be validly used for the entire suspension
//   point; the returned local may be used by multiple suspension points.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetResultBaseVar()
{
    if ((m_resultBaseVar == BAD_VAR_NUM) || !m_comp->lvaHaveManyLocals())
    {
        m_resultBaseVar = m_comp->lvaGrabTemp(false DEBUGARG("object for resuming result base"));
        m_comp->lvaGetDesc(m_resultBaseVar)->lvType = TYP_REF;
    }

    return m_resultBaseVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::GetExceptionVar:
//   Create a new local to hold the exception in the continuation. This
//   local can be validly used for the entire suspension point; the returned
//   local may be used by multiple suspension points.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetExceptionVar()
{
    if ((m_exceptionVar == BAD_VAR_NUM) || !m_comp->lvaHaveManyLocals())
    {
        m_exceptionVar = m_comp->lvaGrabTemp(false DEBUGARG("object for resuming exception"));
        m_comp->lvaGetDesc(m_exceptionVar)->lvType = TYP_REF;
    }

    return m_exceptionVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumptionStubAddrTree:
//   Create a tree that represents the address of the resumption stub entry
//   point.
//
// Returns:
//   IR node.
//
GenTree* AsyncTransformation::CreateResumptionStubAddrTree()
{
    switch (m_resumeStubLookup.accessType)
    {
        case IAT_VALUE:
        {
            return CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
        }
        case IAT_PVALUE:
        {
            GenTree* tree = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            tree          = m_comp->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            return tree;
        }
        case IAT_PPVALUE:
        {
            noway_assert(!"Unexpected IAT_PPVALUE");
            return nullptr;
        }
        case IAT_RELPVALUE:
        {
            GenTree* addr = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            GenTree* tree = CreateFunctionTargetAddr(m_resumeStub, m_resumeStubLookup);
            tree          = m_comp->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            tree          = m_comp->gtNewOperNode(GT_ADD, TYP_I_IMPL, tree, addr);
            return tree;
        }
        default:
        {
            noway_assert(!"Bad accessType");
            return nullptr;
        }
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateFunctionTargetAddr:
//   Create a tree that represents the address of the resumption stub entry
//   point.
//
// Returns:
//   IR node.
//
GenTree* AsyncTransformation::CreateFunctionTargetAddr(CORINFO_METHOD_HANDLE       methHnd,
                                                       const CORINFO_CONST_LOOKUP& lookup)
{
    GenTree* con = m_comp->gtNewIconHandleNode((size_t)lookup.addr, GTF_ICON_FTN_ADDR);
    INDEBUG(con->AsIntCon()->gtTargetHandle = (size_t)methHnd);
    return con;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumptionSwitch:
//   Create the IR for the entry of the function that checks the continuation
//   and dispatches on its state number.
//
void AsyncTransformation::CreateResumptionSwitch()
{
    m_comp->fgCreateNewInitBB();
    BasicBlock* newEntryBB = m_comp->fgFirstBB;

    GenTree* continuationArg = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
    GenTree* null            = m_comp->gtNewNull();
    GenTree* neNull          = m_comp->gtNewOperNode(GT_NE, TYP_INT, continuationArg, null);
    GenTree* jtrue           = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
    LIR::AsRange(newEntryBB).InsertAtEnd(continuationArg, null, neNull, jtrue);

    FlowEdge* resumingEdge;

    if (m_resumptionBBs.size() == 1)
    {
        JITDUMP("  Redirecting entry " FMT_BB " directly to " FMT_BB " as it is the only resumption block\n",
                newEntryBB->bbNum, m_resumptionBBs[0]->bbNum);
        resumingEdge = m_comp->fgAddRefPred(m_resumptionBBs[0], newEntryBB);
    }
    else if (m_resumptionBBs.size() == 2)
    {
        BasicBlock* condBB = m_comp->fgNewBBbefore(BBJ_COND, m_resumptionBBs[0], true);
        condBB->inheritWeightPercentage(newEntryBB, 0);

        FlowEdge* to0 = m_comp->fgAddRefPred(m_resumptionBBs[0], condBB);
        FlowEdge* to1 = m_comp->fgAddRefPred(m_resumptionBBs[1], condBB);
        condBB->SetCond(to1, to0);
        to1->setLikelihood(0.5);
        to0->setLikelihood(0.5);

        resumingEdge = m_comp->fgAddRefPred(condBB, newEntryBB);

        JITDUMP("  Redirecting entry " FMT_BB " to BBJ_COND " FMT_BB " for resumption with 2 states\n",
                newEntryBB->bbNum, condBB->bbNum);

        continuationArg          = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationStateFldHnd);
        GenTree* stateOffsetNode = m_comp->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_comp->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* zero            = m_comp->gtNewZeroConNode(TYP_INT);
        GenTree* stateNeZero     = m_comp->gtNewOperNode(GT_NE, TYP_INT, stateInd, zero);
        GenTree* jtrue           = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, stateNeZero);

        LIR::AsRange(condBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, zero, stateNeZero,
                                         jtrue);
    }
    else
    {
        BasicBlock* switchBB = m_comp->fgNewBBbefore(BBJ_SWITCH, m_resumptionBBs[0], true);
        switchBB->inheritWeightPercentage(newEntryBB, 0);

        resumingEdge = m_comp->fgAddRefPred(switchBB, newEntryBB);

        JITDUMP("  Redirecting entry " FMT_BB " to BBJ_SWITCH " FMT_BB " for resumption with %zu states\n",
                newEntryBB->bbNum, switchBB->bbNum, m_resumptionBBs.size());

        continuationArg          = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationStateFldHnd);
        GenTree* stateOffsetNode = m_comp->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_comp->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* switchNode      = m_comp->gtNewOperNode(GT_SWITCH, TYP_VOID, stateInd);

        LIR::AsRange(switchBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, switchNode);

        m_comp->fgHasSwitch = true;

        // Default case. TODO-CQ: Support bbsHasDefault = false before lowering.
        m_resumptionBBs.push_back(m_resumptionBBs[0]);
        BBswtDesc* swtDesc     = new (m_comp, CMK_BasicBlock) BBswtDesc;
        swtDesc->bbsCount      = (unsigned)m_resumptionBBs.size();
        swtDesc->bbsHasDefault = true;
        swtDesc->bbsDstTab     = new (m_comp, CMK_Async) FlowEdge*[m_resumptionBBs.size()];

        weight_t stateLikelihood = 1.0 / m_resumptionBBs.size();
        for (size_t i = 0; i < m_resumptionBBs.size(); i++)
        {
            swtDesc->bbsDstTab[i] = m_comp->fgAddRefPred(m_resumptionBBs[i], switchBB);
            swtDesc->bbsDstTab[i]->setLikelihood(stateLikelihood);
        }

        switchBB->SetSwitch(swtDesc);
    }

    newEntryBB->SetCond(resumingEdge, newEntryBB->GetTargetEdge());
    resumingEdge->setLikelihood(0);
    newEntryBB->GetFalseEdge()->setLikelihood(1);

    if (m_comp->doesMethodHavePatchpoints())
    {
        JITDUMP("  Method has patch points...\n");
        // If we have patchpoints then first check if we need to resume in the OSR version.
        BasicBlock* callHelperBB = m_comp->fgNewBBafter(BBJ_THROW, m_comp->fgLastBBInMainFunction(), false);
        callHelperBB->bbSetRunRarely();
        callHelperBB->clearTryIndex();
        callHelperBB->clearHndIndex();

        JITDUMP("    Created " FMT_BB " for transitions back into OSR method\n", callHelperBB->bbNum);

        BasicBlock* onContinuationBB = newEntryBB->GetTrueTarget();
        BasicBlock* checkILOffsetBB  = m_comp->fgNewBBbefore(BBJ_COND, onContinuationBB, true);

        JITDUMP("    Created " FMT_BB " to check whether we should transition immediately to OSR\n",
                checkILOffsetBB->bbNum);

        // Redirect newEntryBB -> onContinuationBB into newEntryBB -> checkILOffsetBB -> onContinuationBB
        m_comp->fgRemoveRefPred(newEntryBB->GetTrueEdge());

        FlowEdge* toCheckILOffsetBB = m_comp->fgAddRefPred(checkILOffsetBB, newEntryBB);
        newEntryBB->SetTrueEdge(toCheckILOffsetBB);
        toCheckILOffsetBB->setLikelihood(0);
        checkILOffsetBB->inheritWeightPercentage(newEntryBB, 0);

        FlowEdge* toOnContinuationBB = m_comp->fgAddRefPred(onContinuationBB, checkILOffsetBB);
        FlowEdge* toCallHelperBB     = m_comp->fgAddRefPred(callHelperBB, checkILOffsetBB);
        checkILOffsetBB->SetCond(toCallHelperBB, toOnContinuationBB);
        toCallHelperBB->setLikelihood(0);
        toOnContinuationBB->setLikelihood(1);
        callHelperBB->inheritWeightPercentage(checkILOffsetBB, 0);

        // We need to dispatch to the OSR version if the IL offset is non-negative.
        continuationArg           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfData     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationDataFldHnd);
        GenTree* dataArr          = LoadFromOffset(continuationArg, offsetOfData, TYP_REF);
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Array__data;
        GenTree* ilOffset         = LoadFromOffset(dataArr, offsetOfIlOffset, TYP_INT);
        unsigned ilOffsetLclNum   = m_comp->lvaGrabTemp(false DEBUGARG("IL offset for tier0 OSR method"));
        m_comp->lvaGetDesc(ilOffsetLclNum)->lvType = TYP_INT;
        GenTree* storeIlOffset                     = m_comp->gtNewStoreLclVarNode(ilOffsetLclNum, ilOffset);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(LIR::SeqTree(m_comp, storeIlOffset));

        ilOffset        = m_comp->gtNewLclvNode(ilOffsetLclNum, TYP_INT);
        GenTree* zero   = m_comp->gtNewIconNode(0);
        GenTree* geZero = m_comp->gtNewOperNode(GT_GE, TYP_INT, ilOffset, zero);
        GenTree* jtrue  = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, geZero);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(ilOffset, zero, geZero, jtrue);

        ilOffset = m_comp->gtNewLclvNode(ilOffsetLclNum, TYP_INT);

        GenTreeCall* callHelper = m_comp->gtNewHelperCallNode(CORINFO_HELP_PATCHPOINT_FORCED, TYP_VOID, ilOffset);
        callHelper->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;

        m_comp->compCurBB = callHelperBB;
        m_comp->fgMorphTree(callHelper);

        LIR::AsRange(callHelperBB).InsertAtEnd(LIR::SeqTree(m_comp, callHelper));
    }
    else if (m_comp->opts.IsOSR())
    {
        JITDUMP("  Method is an OSR function\n");
        // If the tier-0 version resumed and then transitioned to the OSR
        // version by normal means then we will see a non-zero continuation
        // here that belongs to the tier0 method. In that case we should just
        // ignore it, so create a BB that jumps back.
        BasicBlock* onContinuationBB   = newEntryBB->GetTrueTarget();
        BasicBlock* onNoContinuationBB = newEntryBB->GetFalseTarget();
        BasicBlock* checkILOffsetBB    = m_comp->fgNewBBbefore(BBJ_COND, onContinuationBB, true);

        // Switch newEntryBB -> onContinuationBB into newEntryBB -> checkILOffsetBB
        m_comp->fgRemoveRefPred(newEntryBB->GetTrueEdge());
        FlowEdge* toCheckILOffset = m_comp->fgAddRefPred(checkILOffsetBB, newEntryBB);
        newEntryBB->SetTrueEdge(toCheckILOffset);
        toCheckILOffset->setLikelihood(0);
        checkILOffsetBB->inheritWeightPercentage(newEntryBB, 0);

        // Make checkILOffsetBB ->(true)  onNoContinuationBB
        //                      ->(false) onContinuationBB

        FlowEdge* toOnContinuationBB   = m_comp->fgAddRefPred(onContinuationBB, checkILOffsetBB);
        FlowEdge* toOnNoContinuationBB = m_comp->fgAddRefPred(onNoContinuationBB, checkILOffsetBB);
        checkILOffsetBB->SetCond(toOnNoContinuationBB, toOnContinuationBB);
        toOnContinuationBB->setLikelihood(0);
        toOnNoContinuationBB->setLikelihood(1);

        JITDUMP("    Created " FMT_BB " to check for Tier-0 continuations\n", checkILOffsetBB->bbNum);

        continuationArg           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfData     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo.continuationDataFldHnd);
        GenTree* dataArr          = LoadFromOffset(continuationArg, offsetOfData, TYP_REF);
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Array__data;
        GenTree* ilOffset         = LoadFromOffset(dataArr, offsetOfIlOffset, TYP_INT);
        GenTree* zero             = m_comp->gtNewIconNode(0);
        GenTree* ltZero           = m_comp->gtNewOperNode(GT_LT, TYP_INT, ilOffset, zero);
        GenTree* jtrue            = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, ltZero);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(LIR::SeqTree(m_comp, jtrue));
    }
}
