// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file implements the transformation of C# async methods into state
// machines. The following key operations are performed:
//
// 1. Early, after import but before inlining: for async calls that require
//    ExecutionContext/SynchronizationContext save/restore semantics, capture and
//    restore calls are inserted around the async call site. This ensures proper
//    context flow across await boundaries when the continuation may run on
//    different threads or synchronization contexts. The captured contexts
//    are stored in temporary locals and restored after the async call completes,
//    with special handling for calls inside try regions using try-finally blocks.
//
// Later, right before lowering the actual transformation to a state machine is
// performed:
//
// 2. Each async call becomes a suspension point where execution can pause and
//    return to the caller, accompanied by a resumption point where execution can
//    continue when the awaited operation completes.
//
// 3. When suspending at a suspension point a continuation object is created that contains:
//    - All live local variables
//    - State number to identify which await is being resumed
//    - Return value from the awaited operation (filled in by the callee later)
//    - Exception information if an exception occurred
//    - Resumption function pointer
//    - Flags containing additional information
//
// 4. The method entry is modified to include dispatch logic that checks for an
//    incoming continuation and jumps to the appropriate resumption point.
//
// 5. Special handling is included for:
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

//------------------------------------------------------------------------
// Compiler::SaveAsyncContexts:
//   Insert code to save and restore contexts around async call sites.
//
// Returns:
//   Suitable phase status.
//
// Remarks:
//   Runs early, after import but before inlining. Thus RET_EXPRs may be
//   present, and async calls may later be inlined.
//
PhaseStatus Compiler::SaveAsyncContexts()
{
    if (!compMustSaveAsyncContexts)
    {
        JITDUMP("No async calls where execution context capture/restore is necessary\n");
        ValidateNoAsyncSavesNecessary();
        return PhaseStatus::MODIFIED_NOTHING;
    }

    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    BasicBlock* curBB = fgFirstBB;
    while (curBB != nullptr)
    {
        BasicBlock* nextBB = curBB->Next();

        for (Statement* stmt : curBB->Statements())
        {
            GenTree* tree = stmt->GetRootNode();
            if (tree->OperIs(GT_STORE_LCL_VAR))
            {
                tree = tree->AsLclVarCommon()->Data();
            }

            if (!tree->IsCall())
            {
                ValidateNoAsyncSavesNecessaryInStatement(stmt);
                continue;
            }

            GenTreeCall* call = tree->AsCall();
            if (!call->IsAsync())
            {
                ValidateNoAsyncSavesNecessaryInStatement(stmt);
                continue;
            }

            const AsyncCallInfo& asyncCallInfo = call->GetAsyncInfo();

            // Currently we always expect that ExecutionContext and
            // SynchronizationContext correlate about their save/restore
            // behavior.
            assert((asyncCallInfo.ExecutionContextHandling == ExecutionContextHandling::SaveAndRestore) ==
                   asyncCallInfo.SaveAndRestoreSynchronizationContextField);

            if (asyncCallInfo.ExecutionContextHandling != ExecutionContextHandling::SaveAndRestore)
            {
                continue;
            }

            unsigned suspendedLclNum =
                lvaGrabTemp(false DEBUGARG(printfAlloc("Suspended indicator for [%06u]", dspTreeID(call))));
            unsigned execCtxLclNum =
                lvaGrabTemp(false DEBUGARG(printfAlloc("ExecutionContext for [%06u]", dspTreeID(call))));
            unsigned syncCtxLclNum =
                lvaGrabTemp(false DEBUGARG(printfAlloc("SynchronizationContext for [%06u]", dspTreeID(call))));

            LclVarDsc* suspendedLclDsc     = lvaGetDesc(suspendedLclNum);
            suspendedLclDsc->lvType        = TYP_UBYTE;
            suspendedLclDsc->lvHasLdAddrOp = true;

            LclVarDsc* execCtxLclDsc     = lvaGetDesc(execCtxLclNum);
            execCtxLclDsc->lvType        = TYP_REF;
            execCtxLclDsc->lvHasLdAddrOp = true;

            LclVarDsc* syncCtxLclDsc     = lvaGetDesc(syncCtxLclNum);
            syncCtxLclDsc->lvType        = TYP_REF;
            syncCtxLclDsc->lvHasLdAddrOp = true;

            call->asyncInfo->SynchronizationContextLclNum = syncCtxLclNum;

            call->gtArgs.PushBack(this, NewCallArg::Primitive(gtNewLclAddrNode(suspendedLclNum, 0))
                                            .WellKnown(WellKnownArg::AsyncSuspendedIndicator));

            JITDUMP("Saving contexts around [%06u], ExecutionContext = V%02u, SynchronizationContext = V%02u\n",
                    call->gtTreeID, execCtxLclNum, syncCtxLclNum);

            CORINFO_ASYNC_INFO* asyncInfo = eeGetAsyncInfo();

            GenTreeCall* capture = gtNewCallNode(CT_USER_FUNC, asyncInfo->captureContextsMethHnd, TYP_VOID);
            capture->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclAddrNode(syncCtxLclNum, 0)));
            capture->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclAddrNode(execCtxLclNum, 0)));

            CORINFO_CALL_INFO callInfo = {};
            callInfo.hMethod           = capture->gtCallMethHnd;
            callInfo.methodFlags       = info.compCompHnd->getMethodAttribs(callInfo.hMethod);
            impMarkInlineCandidate(capture, MAKE_METHODCONTEXT(callInfo.hMethod), false, &callInfo, compInlineContext);

            Statement* captureStmt = fgNewStmtFromTree(capture);
            fgInsertStmtBefore(curBB, stmt, captureStmt);

            JITDUMP("Inserted capture:\n");
            DISPSTMT(captureStmt);

            BasicBlock* restoreBB        = curBB;
            Statement*  restoreAfterStmt = stmt;

            if (call->IsInlineCandidate() && (call->gtReturnType != TYP_VOID))
            {
                restoreAfterStmt = stmt->GetNextStmt();
                assert(restoreAfterStmt->GetRootNode()->OperIs(GT_RET_EXPR) ||
                       (restoreAfterStmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR) &&
                        restoreAfterStmt->GetRootNode()->AsLclVarCommon()->Data()->OperIs(GT_RET_EXPR)));
            }

            if (curBB->hasTryIndex())
            {
#ifdef FEATURE_EH_WINDOWS_X86
                IMPL_LIMITATION("Cannot handle insertion of try-finally without funclets");
#else
                // Await is inside a try, need to insert try-finally around it.
                restoreBB        = InsertTryFinallyForContextRestore(curBB, stmt, restoreAfterStmt);
                restoreAfterStmt = nullptr;
                // we have split the block that could have another await.
                nextBB = restoreBB->Next();
#endif
            }

            GenTreeCall* restore = gtNewCallNode(CT_USER_FUNC, asyncInfo->restoreContextsMethHnd, TYP_VOID);
            restore->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclVarNode(syncCtxLclNum)));
            restore->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclVarNode(execCtxLclNum)));
            restore->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclVarNode(suspendedLclNum)));

            callInfo             = {};
            callInfo.hMethod     = restore->gtCallMethHnd;
            callInfo.methodFlags = info.compCompHnd->getMethodAttribs(callInfo.hMethod);
            impMarkInlineCandidate(restore, MAKE_METHODCONTEXT(callInfo.hMethod), false, &callInfo, compInlineContext);

            Statement* restoreStmt = fgNewStmtFromTree(restore);
            if (restoreAfterStmt == nullptr)
            {
                fgInsertStmtNearEnd(restoreBB, restoreStmt);
            }
            else
            {
                fgInsertStmtAfter(restoreBB, restoreAfterStmt, restoreStmt);
            }

            JITDUMP("Inserted restore:\n");
            DISPSTMT(restoreStmt);

            result = PhaseStatus::MODIFIED_EVERYTHING;
        }

        curBB = nextBB;
    }

    return result;
}

//------------------------------------------------------------------------
// Compiler::ValidateNoAsyncSavesNecessary:
//   Check that there are no async calls requiring saving of ExecutionContext
//   in the method.
//
void Compiler::ValidateNoAsyncSavesNecessary()
{
#ifdef DEBUG
    for (BasicBlock* block : Blocks())
    {
        for (Statement* stmt : block->Statements())
        {
            ValidateNoAsyncSavesNecessaryInStatement(stmt);
        }
    }
#endif
}

//------------------------------------------------------------------------
// Compiler::ValidateNoAsyncSavesNecessaryInStatement:
//   Check that there are no async calls requiring saving of ExecutionContext
//   in the statement.
//
// Parameters:
//   stmt - The statement
//
void Compiler::ValidateNoAsyncSavesNecessaryInStatement(Statement* stmt)
{
#ifdef DEBUG
    struct Visitor : GenTreeVisitor<Visitor>
    {
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* comp)
            : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if (((*use)->gtFlags & GTF_CALL) == 0)
            {
                return WALK_SKIP_SUBTREES;
            }

            if ((*use)->IsCall())
            {
                assert(!(*use)->AsCall()->IsAsyncAndAlwaysSavesAndRestoresExecutionContext());
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this);
    visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
#endif
}

//------------------------------------------------------------------------
// Compiler::InsertTryFinallyForContextRestore:
//   Insert a try-finally around the specified statements in the specified
//   block.
//
// Returns:
//   Finally block of inserted try-finally.
//
BasicBlock* Compiler::InsertTryFinallyForContextRestore(BasicBlock* block, Statement* firstStmt, Statement* lastStmt)
{
    assert(!block->hasHndIndex());
    EHblkDsc* ebd = fgTryAddEHTableEntries(block->bbTryIndex - 1, 1);
    if (ebd == nullptr)
    {
        IMPL_LIMITATION("Awaits require insertion of too many EH clauses");
    }

    if (firstStmt == block->firstStmt())
    {
        block = fgSplitBlockAtBeginning(block);
    }
    else
    {
        block = fgSplitBlockAfterStatement(block, firstStmt->GetPrevStmt());
    }

    BasicBlock* tailBB = fgSplitBlockAfterStatement(block, lastStmt);

    BasicBlock* callFinally    = fgNewBBafter(BBJ_CALLFINALLY, block, false);
    BasicBlock* callFinallyRet = fgNewBBafter(BBJ_CALLFINALLYRET, callFinally, false);
    BasicBlock* finallyRet     = fgNewBBafter(BBJ_EHFINALLYRET, callFinallyRet, false);
    BasicBlock* goToTailBlock  = fgNewBBafter(BBJ_ALWAYS, finallyRet, false);

    callFinally->inheritWeight(block);
    callFinallyRet->inheritWeight(block);
    finallyRet->inheritWeight(block);
    goToTailBlock->inheritWeight(block);

    // Set some info the starting blocks like fgFindBasicBlocks does
    block->SetFlags(BBF_DONT_REMOVE);
    finallyRet->SetFlags(BBF_DONT_REMOVE);
    finallyRet->bbRefs++; // Artificial ref count on handler begins

    fgRemoveRefPred(block->GetTargetEdge());
    // Wire up the control flow for the new blocks
    block->SetTargetEdge(fgAddRefPred(callFinally, block));
    callFinally->SetTargetEdge(fgAddRefPred(finallyRet, callFinally));

    FlowEdge** succs = new (this, CMK_BasicBlock) FlowEdge* [1] {
        fgAddRefPred(callFinallyRet, finallyRet)
    };
    succs[0]->setLikelihood(1.0);
    BBJumpTable* ehfDesc = new (this, CMK_BasicBlock) BBJumpTable(succs, 1);
    finallyRet->SetEhfTargets(ehfDesc);

    callFinallyRet->SetTargetEdge(fgAddRefPred(goToTailBlock, callFinallyRet));
    goToTailBlock->SetTargetEdge(fgAddRefPred(tailBB, goToTailBlock));

    // Most of these blocks go in the old EH region
    callFinally->bbTryIndex    = block->bbTryIndex;
    callFinallyRet->bbTryIndex = block->bbTryIndex;
    finallyRet->bbTryIndex     = block->bbTryIndex;
    goToTailBlock->bbTryIndex  = block->bbTryIndex;

    callFinally->bbHndIndex    = block->bbHndIndex;
    callFinallyRet->bbHndIndex = block->bbHndIndex;
    finallyRet->bbHndIndex     = block->bbHndIndex;
    goToTailBlock->bbHndIndex  = block->bbHndIndex;

    // block goes into the inserted EH clause and the finally becomes the handler
    block->bbTryIndex--;
    finallyRet->bbHndIndex = block->bbTryIndex;

    ebd->ebdID          = impInlineRoot()->compEHID++;
    ebd->ebdHandlerType = EH_HANDLER_FINALLY;

    ebd->ebdTryBeg  = block;
    ebd->ebdTryLast = block;

    ebd->ebdHndBeg  = finallyRet;
    ebd->ebdHndLast = finallyRet;

    ebd->ebdTyp               = 0;
    ebd->ebdEnclosingTryIndex = (unsigned short)goToTailBlock->getTryIndex();
    ebd->ebdEnclosingHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;

    ebd->ebdTryBegOffset    = block->bbCodeOffs;
    ebd->ebdTryEndOffset    = block->bbCodeOffsEnd;
    ebd->ebdFilterBegOffset = 0;
    ebd->ebdHndBegOffset    = 0;
    ebd->ebdHndEndOffset    = 0;

    finallyRet->bbCatchTyp = BBCT_FINALLY;
    GenTree*   retFilt     = gtNewOperNode(GT_RETFILT, TYP_VOID, nullptr);
    Statement* retFiltStmt = fgNewStmtFromTree(retFilt);
    fgInsertStmtAtEnd(finallyRet, retFiltStmt);

    return finallyRet;
}

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
    template <typename Functor>
    void GetLiveLocals(jitstd::vector<LiveLocalInfo>& liveLocals, Functor includeLocal);

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
//   liveLocals   - Vector to add live local information into
//   includeLocal - Functor to check if a local should be included
//
template <typename Functor>
void AsyncLiveness::GetLiveLocals(jitstd::vector<LiveLocalInfo>& liveLocals, Functor includeLocal)
{
    for (unsigned lclNum = 0; lclNum < m_numVars; lclNum++)
    {
        if (includeLocal(lclNum) && IsLive(lclNum))
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

    m_asyncInfo = m_comp->eeGetAsyncInfo();

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

    ContinuationLayout layout = LayOutContinuation(block, call, ContinuationNeedsKeepAlive(life), liveLocals);

    ClearSuspendedIndicator(block, call);

    CallDefinitionInfo callDefInfo = CanonicalizeCallDefinition(block, call, life);

    unsigned stateNum = (unsigned)m_resumptionBBs.size();
    JITDUMP("  Assigned state %u\n", stateNum);

    BasicBlock* suspendBB = CreateSuspension(block, call, stateNum, life, layout);

    CreateCheckAndSuspendAfterCall(block, call, callDefInfo, life, suspendBB, remainder);

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
    SmallHashTable<unsigned, bool> excludedLocals(m_comp->getAllocator(CMK_Async));

    auto visitDef = [&](const LocalDef& def) {
        if (def.IsEntire)
        {
            JITDUMP("  V%02u is fully defined and will not be considered live\n", def.Def->GetLclNum());
            excludedLocals.AddOrUpdate(def.Def->GetLclNum(), true);
        }
        return GenTree::VisitResult::Continue;
    };

    call->VisitLocalDefs(m_comp, visitDef);

    const AsyncCallInfo& asyncInfo = call->GetAsyncInfo();

    if (asyncInfo.SynchronizationContextLclNum != BAD_VAR_NUM)
    {
        // This one is only live on the synchronous path, which liveness cannot prove
        excludedLocals.AddOrUpdate(asyncInfo.SynchronizationContextLclNum, true);
    }

    life.GetLiveLocals(liveLocals, [&](unsigned lclNum) {
        return !excludedLocals.Contains(lclNum);
    });
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
// AsyncTransformation::ContinuationNeedsKeepAlive:
//   Check whether we need to allocate a "KeepAlive" field in the continuation.
//
// Parameters:
//   life - Live locals
//
// Returns:
//   True if we need to keep a LoaderAllocator for generic context or
//   collectible method alive.
//
bool AsyncTransformation::ContinuationNeedsKeepAlive(AsyncLiveness& life)
{
    if (m_asyncInfo->continuationsNeedMethodHandle)
    {
        return true;
    }

    const unsigned GENERICS_CTXT_FROM = CORINFO_GENERICS_CTXT_FROM_METHODDESC | CORINFO_GENERICS_CTXT_FROM_METHODTABLE;
    if (((m_comp->info.compMethodInfo->options & GENERICS_CTXT_FROM) != 0) && life.IsLive(m_comp->info.compTypeCtxtArg))
    {
        return true;
    }

    return false;
}

class GCPointerBitMapBuilder
{
    bool*  m_objRefs;
    size_t m_size;

public:
    GCPointerBitMapBuilder(bool* objRefs, size_t size)
        : m_objRefs(objRefs)
        , m_size(size)
    {
    }

    INDEBUG(unsigned NumObjRefs = 0);

    void Set(unsigned offset)
    {
        assert((offset % TARGET_POINTER_SIZE) == 0);
        assert(offset < m_size);
        unsigned slot = offset / TARGET_POINTER_SIZE;

        // We do not expect to set the same offset multiple times
        assert(!m_objRefs[slot]);
        m_objRefs[slot] = true;
    }

    void SetIfNotMax(unsigned offset)
    {
        if (offset != UINT_MAX)
        {
            Set(offset);
        }
    }

    void SetType(unsigned offset, var_types type, ClassLayout* layout)
    {
        if (type == TYP_REF)
        {
            Set(offset);
        }
        else if (type == TYP_STRUCT)
        {
            for (unsigned slot = 0; slot < layout->GetSlotCount(); slot++)
            {
                if (layout->GetGCPtrType(slot) == TYP_REF)
                {
                    Set(offset + slot * TARGET_POINTER_SIZE);
                }
            }
        }
    }
};

//------------------------------------------------------------------------
// AsyncTransformation::LayOutContinuation:
//   Create the layout of the GC pointer and data arrays in the continuation
//   object.
//
// Parameters:
//   block          - The block containing the async call
//   call           - The async call
//   needsKeepAlive - Whether the layout needs a "keep alive" field allocated
//   liveLocals     - [in, out] Information about each live local. Size/alignment
//                    information is read and offset/index information is written.
//
// Returns:
//   Layout information.
//
ContinuationLayout AsyncTransformation::LayOutContinuation(BasicBlock*                    block,
                                                           GenTreeCall*                   call,
                                                           bool                           needsKeepAlive,
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
                inf.Alignment = 1;
                inf.Size      = layout->GetSize();
            }
            else
            {
                inf.Alignment = m_comp->info.compCompHnd->getClassAlignmentRequirement(layout->GetClassHandle());
                inf.Size      = layout->GetSize();
            }
        }
        else if (dsc->TypeIs(TYP_REF))
        {
            inf.Alignment = TARGET_POINTER_SIZE;
            inf.Size      = TARGET_POINTER_SIZE;
        }
        else
        {
            assert(!dsc->TypeIs(TYP_BYREF));

            inf.Alignment = genTypeAlignments[dsc->TypeGet()];
            inf.Size      = genTypeSize(dsc);
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

    if (call->gtReturnType == TYP_STRUCT)
    {
        layout.ReturnStructLayout = m_comp->typGetObjLayout(call->gtRetClsHnd);
        layout.ReturnSize         = layout.ReturnStructLayout->GetSize();
        layout.ReturnAlignment    = m_comp->info.compCompHnd->getClassAlignmentRequirement(call->gtRetClsHnd);
    }
    else
    {
        layout.ReturnSize      = genTypeSize(call->gtReturnType);
        layout.ReturnAlignment = layout.ReturnSize;
    }

    assert((layout.ReturnSize > 0) == (call->gtReturnType != TYP_VOID));

    auto allocLayout = [&layout](unsigned align, unsigned size) {
        layout.Size     = roundUp(layout.Size, align);
        unsigned offset = layout.Size;
        layout.Size += size;
        return offset;
    };

    // For OSR, we store the IL offset that inspired the OSR method at the
    // beginning of the data (and store -1 in the tier0 version). This must be
    // at the beginning because the tier0 and OSR versions need to agree on
    // this.
    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
    {
        JITDUMP("  Method %s; keeping IL offset that inspired OSR method at the beginning of non-GC data\n",
                m_comp->doesMethodHavePatchpoints() ? "has patchpoints" : "is an OSR method");
        allocLayout(sizeof(int), sizeof(int));
    }

    if (layout.ReturnSize > 0)
    {
        layout.ReturnValOffset = allocLayout(layout.ReturnAlignment, layout.ReturnSize);

        JITDUMP("  Will store return of type %s, size %u at offset %u\n",
                call->gtReturnType == TYP_STRUCT ? layout.ReturnStructLayout->GetClassName()
                                                 : varTypeName(call->gtReturnType),
                layout.ReturnSize, layout.ReturnValOffset);
    }

    if (block->hasTryIndex())
    {
        layout.ExceptionOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
        JITDUMP("  " FMT_BB " is in try region %u; exception will be at offset %u\n", block->bbNum,
                block->getTryIndex(), layout.ExceptionOffset);
    }

    if (call->GetAsyncInfo().ContinuationContextHandling == ContinuationContextHandling::ContinueOnCapturedContext)
    {
        layout.ContinuationContextOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
        JITDUMP("  Continuation continues on captured context; context will be at offset %u\n",
                layout.ContinuationContextOffset);
    }

    if (call->GetAsyncInfo().ExecutionContextHandling == ExecutionContextHandling::AsyncSaveAndRestore)
    {
        layout.ExecContextOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
        JITDUMP("  Call has async-only save and restore of ExecutionContext; ExecutionContext will be at offset %u\n",
                layout.ExecContextOffset);
    }

    unsigned keepAliveOffset = UINT_MAX;
    if (needsKeepAlive)
    {
        keepAliveOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    for (LiveLocalInfo& inf : liveLocals)
    {
        inf.Offset = allocLayout(inf.Alignment, inf.Size);
    }

    layout.Size = roundUp(layout.Size, TARGET_POINTER_SIZE);

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  Continuation layout (%u bytes):\n", layout.Size);
        for (LiveLocalInfo& inf : liveLocals)
        {
            printf("    +%03u V%02u: %u bytes\n", inf.Offset, inf.LclNum, inf.Size);
        }
    }
#endif

    // Now create continuation type. First create bitmap of object refs.
    bool* objRefs = new (m_comp, CMK_Async) bool[layout.Size / TARGET_POINTER_SIZE]{};

    GCPointerBitMapBuilder bitmapBuilder(objRefs, layout.Size);
    bitmapBuilder.SetIfNotMax(layout.ExceptionOffset);
    bitmapBuilder.SetIfNotMax(layout.ContinuationContextOffset);
    bitmapBuilder.SetIfNotMax(layout.ExecContextOffset);
    bitmapBuilder.SetIfNotMax(keepAliveOffset);

    if (layout.ReturnSize > 0)
    {
        bitmapBuilder.SetType(layout.ReturnValOffset, call->gtReturnType, layout.ReturnStructLayout);
    }

    for (LiveLocalInfo& inf : liveLocals)
    {
        LclVarDsc*   dsc = m_comp->lvaGetDesc(inf.LclNum);
        var_types    storedType;
        ClassLayout* layout;
        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            storedType = TYP_STRUCT;
            layout     = dsc->GetLayout();
        }
        else
        {
            storedType = dsc->TypeGet();
            layout     = NULL;
        }
        bitmapBuilder.SetType(inf.Offset, storedType, layout);
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("Getting continuation layout size = %u, numGCRefs = %u (Continuation_%s_%u_%u)\n", layout.Size,
               bitmapBuilder.NumObjRefs, m_comp->info.compMethodName, layout.Size, bitmapBuilder.NumObjRefs);
        bool* start        = objRefs;
        bool* endOfObjRefs = objRefs + layout.Size / TARGET_POINTER_SIZE;
        while (start < endOfObjRefs)
        {
            while (start < endOfObjRefs && !*start)
                start++;

            if (start >= endOfObjRefs)
                break;

            bool* end = start;
            while (end < endOfObjRefs && *end)
                end++;

            printf("  [%3u..%3u) obj refs\n", (start - objRefs) * TARGET_POINTER_SIZE,
                   (end - objRefs) * TARGET_POINTER_SIZE);
            start = end;
        }
    }
#endif

    CORINFO_CONTINUATION_DATA_OFFSETS offsets;
    offsets.Result              = layout.ReturnValOffset;
    offsets.Exception           = layout.ExceptionOffset;
    offsets.ContinuationContext = layout.ContinuationContextOffset;
    offsets.KeepAlive           = keepAliveOffset;
    layout.ClassHnd             = m_comp->info.compCompHnd->getContinuationType(layout.Size, objRefs, offsets);

    return layout;
}

//------------------------------------------------------------------------
// AsyncTransformation::ClearSuspendedIndicator:
//   Generate IR to clear the value of the suspended indicator local.
//
// Parameters:
//   block - Block to generate IR into
//   call  - The async call (not contained in "block")
//
void AsyncTransformation::ClearSuspendedIndicator(BasicBlock* block, GenTreeCall* call)
{
    CallArg* suspendedArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncSuspendedIndicator);
    if (suspendedArg == nullptr)
    {
        return;
    }

    GenTree* suspended = suspendedArg->GetNode();
    if (!suspended->IsLclVarAddr() &&
        (!suspended->OperIs(GT_LCL_VAR) || m_comp->lvaVarAddrExposed(suspended->AsLclVarCommon()->GetLclNum())))
    {
        // We will need a second use of this, so spill to a local
        LIR::Use use(LIR::AsRange(block), &suspendedArg->NodeRef(), call);
        use.ReplaceWithLclVar(m_comp);
        suspended = use.Def();
    }

    GenTree* value = m_comp->gtNewIconNode(0);
    GenTree* storeSuspended =
        m_comp->gtNewStoreValueNode(TYP_UBYTE, m_comp->gtCloneExpr(suspended), value, GTF_IND_NONFAULTING);

    LIR::AsRange(block).InsertBefore(call, LIR::SeqTree(m_comp, storeSuspended));
}

//------------------------------------------------------------------------
// AsyncTransformation::SetSuspendedIndicator:
//   Generate IR to set the value of the suspended indicator local, and remove
//   the argument from the call.
//
// Parameters:
//   block     - Block to generate IR into
//   callBlock - Block containing the call
//   call      - The async call
//
void AsyncTransformation::SetSuspendedIndicator(BasicBlock* block, BasicBlock* callBlock, GenTreeCall* call)
{
    CallArg* suspendedArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncSuspendedIndicator);
    if (suspendedArg == nullptr)
    {
        return;
    }

    GenTree* suspended = suspendedArg->GetNode();
    assert(suspended->IsLclVarAddr() || suspended->OperIs(GT_LCL_VAR)); // Ensured by ClearSuspendedIndicator

    GenTree* value = m_comp->gtNewIconNode(1);
    GenTree* storeSuspended =
        m_comp->gtNewStoreValueNode(TYP_UBYTE, m_comp->gtCloneExpr(suspended), value, GTF_IND_NONFAULTING);

    LIR::AsRange(block).InsertAtEnd(LIR::SeqTree(m_comp, storeSuspended));

    call->gtArgs.RemoveUnsafe(suspendedArg);
    call->asyncInfo->HasSuspensionIndicatorDef = false;

    // Avoid leaving LCL_ADDR around which will DNER the local.
    if (suspended->IsLclVarAddr())
    {
        LIR::AsRange(callBlock).Remove(suspended);
    }
    else
    {
        suspended->SetUnusedValue();
    }
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
//   call     - The async call
//   stateNum - State number assigned to this suspension point
//   life     - Liveness information about live locals
//   layout   - Layout information for the continuation object
//
// Returns:
//   The new basic block that was created.
//
BasicBlock* AsyncTransformation::CreateSuspension(
    BasicBlock* block, GenTreeCall* call, unsigned stateNum, AsyncLiveness& life, const ContinuationLayout& layout)
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

    GenTreeCall* allocContinuation = CreateAllocContinuationCall(life, returnedContinuation, layout.ClassHnd);

    m_comp->compCurBB = suspendBB;
    m_comp->fgMorphTree(allocContinuation);

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, allocContinuation));

    GenTree* storeNewContinuation = m_comp->gtNewStoreLclVarNode(m_newContinuationVar, allocContinuation);
    LIR::AsRange(suspendBB).InsertAtEnd(storeNewContinuation);

    // Fill in 'Resume'
    GenTree* newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned resumeOffset    = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationResumeFldHnd);
    GenTree* resumeStubAddr  = CreateResumptionStubAddrTree();
    GenTree* storeResume     = StoreAtOffset(newContinuation, resumeOffset, resumeStubAddr, TYP_I_IMPL);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeResume));

    // Fill in 'state'
    newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned stateOffset  = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
    GenTree* stateNumNode = m_comp->gtNewIconNode((ssize_t)stateNum, TYP_INT);
    GenTree* storeState   = StoreAtOffset(newContinuation, stateOffset, stateNumNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeState));

    // Fill in 'flags'
    const AsyncCallInfo& callInfo          = call->GetAsyncInfo();
    unsigned             continuationFlags = 0;
    if (block->hasTryIndex())
        continuationFlags |= CORINFO_CONTINUATION_NEEDS_EXCEPTION;
    if (callInfo.ContinuationContextHandling == ContinuationContextHandling::ContinueOnThreadPool)
        continuationFlags |= CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL;

    newContinuation      = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
    unsigned flagsOffset = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationFlagsFldHnd);
    GenTree* flagsNode   = m_comp->gtNewIconNode((ssize_t)continuationFlags, TYP_INT);
    GenTree* storeFlags  = StoreAtOffset(newContinuation, flagsOffset, flagsNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storeFlags));

    if (layout.Size > 0)
    {
        FillInDataOnSuspension(call, layout, suspendBB);
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
GenTreeCall* AsyncTransformation::CreateAllocContinuationCall(AsyncLiveness&       life,
                                                              GenTree*             prevContinuation,
                                                              CORINFO_CLASS_HANDLE contClassHnd)
{
    GenTree* contClassHndNode = m_comp->gtNewIconEmbClsHndNode(contClassHnd);
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
    else if (m_asyncInfo->continuationsNeedMethodHandle)
    {
        methodHandleArg = m_comp->gtNewIconEmbMethHndNode(m_comp->info.compMethodHnd);
    }

    if (methodHandleArg != nullptr)
    {
        return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION_METHOD, TYP_REF, prevContinuation,
                                           contClassHndNode, methodHandleArg);
    }

    if (classHandleArg != nullptr)
    {
        return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION_CLASS, TYP_REF, prevContinuation,
                                           contClassHndNode, classHandleArg);
    }

    return m_comp->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION, TYP_REF, prevContinuation, contClassHndNode);
}

//------------------------------------------------------------------------
// AsyncTransformation::FillInDataOnSuspension:
//   Create IR that fills the data array of the continuation object.
//
// Parameters:
//   call      - The async call
//   layout    - Information about the continuation layout
//   suspendBB - Basic block to add IR to.
//
void AsyncTransformation::FillInDataOnSuspension(GenTreeCall*              call,
                                                 const ContinuationLayout& layout,
                                                 BasicBlock*               suspendBB)
{
    if (m_comp->doesMethodHavePatchpoints() || m_comp->opts.IsOSR())
    {
        GenTree* ilOffsetToStore;
        if (m_comp->doesMethodHavePatchpoints())
            ilOffsetToStore = m_comp->gtNewIconNode(-1);
        else
            ilOffsetToStore = m_comp->gtNewIconNode((int)m_comp->info.compILEntry);

        GenTree* newContinuation       = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned offset                = OFFSETOF__CORINFO_Continuation__data;
        GenTree* storePatchpointOffset = StoreAtOffset(newContinuation, offset, ilOffsetToStore, TYP_INT);
        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, storePatchpointOffset));
    }

    // Fill in data
    for (const LiveLocalInfo& inf : layout.Locals)
    {
        assert(inf.Size > 0);

        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        GenTree* newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned offset          = OFFSETOF__CORINFO_Continuation__data + inf.Offset;

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
            GenTree* addr = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation, cns);
            store         = m_comp->gtNewStoreValueNode(dsc->GetLayout(), addr, value, GTF_IND_NONFAULTING);
        }
        else
        {
            store = StoreAtOffset(newContinuation, offset, value, dsc->TypeGet());
        }

        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, store));
    }

    if (layout.ContinuationContextOffset != UINT_MAX)
    {
        const AsyncCallInfo& callInfo = call->GetAsyncInfo();
        assert(callInfo.SaveAndRestoreSynchronizationContextField);
        assert(callInfo.ExecutionContextHandling == ExecutionContextHandling::SaveAndRestore);
        assert(callInfo.SynchronizationContextLclNum != BAD_VAR_NUM);

        // Insert call
        //   AsyncHelpers.CaptureContinuationContext(
        //     syncContextFromBeforeCall,
        //     ref newContinuation.ContinuationContext,
        //     ref newContinuation.Flags).
        GenTree*     syncContextPlaceholder    = m_comp->gtNewNull();
        GenTree*     contextElementPlaceholder = m_comp->gtNewZeroConNode(TYP_BYREF);
        GenTree*     flagsPlaceholder          = m_comp->gtNewZeroConNode(TYP_BYREF);
        GenTreeCall* captureCall =
            m_comp->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->captureContinuationContextMethHnd, TYP_VOID);

        captureCall->gtArgs.PushFront(m_comp, NewCallArg::Primitive(flagsPlaceholder));
        captureCall->gtArgs.PushFront(m_comp, NewCallArg::Primitive(contextElementPlaceholder));
        captureCall->gtArgs.PushFront(m_comp, NewCallArg::Primitive(syncContextPlaceholder));

        m_comp->compCurBB = suspendBB;
        m_comp->fgMorphTree(captureCall);

        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_comp, captureCall));

        // Replace sync context placeholder with actual sync context from before call
        LIR::Use use;
        bool     gotUse = LIR::AsRange(suspendBB).TryGetUse(syncContextPlaceholder, &use);
        assert(gotUse);
        GenTree* syncContextLcl = m_comp->gtNewLclvNode(callInfo.SynchronizationContextLclNum, TYP_REF);
        LIR::AsRange(suspendBB).InsertBefore(syncContextPlaceholder, syncContextLcl);
        use.ReplaceWith(syncContextLcl);
        LIR::AsRange(suspendBB).Remove(syncContextPlaceholder);

        // Replace contextElementPlaceholder with actual address of the context element
        gotUse = LIR::AsRange(suspendBB).TryGetUse(contextElementPlaceholder, &use);
        assert(gotUse);

        GenTree* newContinuation      = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned offset               = OFFSETOF__CORINFO_Continuation__data + layout.ContinuationContextOffset;
        GenTree* contextElementOffset = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                                              m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(contextElementPlaceholder, LIR::SeqTree(m_comp, contextElementOffset));
        use.ReplaceWith(contextElementOffset);
        LIR::AsRange(suspendBB).Remove(contextElementPlaceholder);

        // Replace flagsPlaceholder with actual address of the flags
        gotUse = LIR::AsRange(suspendBB).TryGetUse(flagsPlaceholder, &use);
        assert(gotUse);

        newContinuation          = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned flagsOffset     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationFlagsFldHnd);
        GenTree* flagsOffsetNode = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                                         m_comp->gtNewIconNode((ssize_t)flagsOffset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(flagsPlaceholder, LIR::SeqTree(m_comp, flagsOffsetNode));
        use.ReplaceWith(flagsOffsetNode);
        LIR::AsRange(suspendBB).Remove(flagsPlaceholder);
    }

    if (layout.ExecContextOffset != UINT_MAX)
    {
        GenTreeCall* captureExecContext =
            m_comp->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->captureExecutionContextMethHnd, TYP_REF);

        m_comp->compCurBB = suspendBB;
        m_comp->fgMorphTree(captureExecContext);

        GenTree* newContinuation = m_comp->gtNewLclvNode(m_newContinuationVar, TYP_REF);
        unsigned offset          = OFFSETOF__CORINFO_Continuation__data + layout.ExecContextOffset;
        GenTree* store           = StoreAtOffset(newContinuation, offset, captureExecContext, TYP_REF);
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
//   call        - The async call
//   callDefInfo - Information about the async call's definition
//   life        - Liveness information about live locals
//   suspendBB   - Basic block to add IR to
//   remainder   - [out] The remainder block containing the IR that was after the async call.
//
void AsyncTransformation::CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                         GenTreeCall*              call,
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

    resumeBB->bbSetRunRarely();
    resumeBB->CopyFlags(remainder, BBF_PROF_WEIGHT);
    resumeBB->SetTargetEdge(remainderEdge);
    resumeBB->clearTryIndex();
    resumeBB->clearHndIndex();
    resumeBB->SetFlags(BBF_ASYNC_RESUMPTION);
    m_lastResumptionBB = resumeBB;

    JITDUMP("  Creating resumption " FMT_BB " for state %u\n", resumeBB->bbNum, stateNum);

    SetSuspendedIndicator(resumeBB, block, call);

    if (layout.Size > 0)
    {
        RestoreFromDataOnResumption(layout, resumeBB);
    }

    BasicBlock* storeResultBB = resumeBB;

    if (layout.ExceptionOffset != UINT_MAX)
    {
        storeResultBB = RethrowExceptionOnResumption(block, layout, resumeBB);
    }

    if ((layout.ReturnSize > 0) && (callDefInfo.DefinitionNode != nullptr))
    {
        CopyReturnValueOnResumption(call, callDefInfo, layout, storeResultBB);
    }

    return resumeBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::RestoreFromDataOnResumption:
//   Create IR that restores locals from the data array of the continuation
//   object.
//
// Parameters:
//   layout   - Information about the continuation layout
//   resumeBB - Basic block to append IR to
//
void AsyncTransformation::RestoreFromDataOnResumption(const ContinuationLayout& layout, BasicBlock* resumeBB)
{
    if (layout.ExecContextOffset != BAD_VAR_NUM)
    {
        GenTree*     valuePlaceholder = m_comp->gtNewZeroConNode(TYP_REF);
        GenTreeCall* restoreCall =
            m_comp->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->restoreExecutionContextMethHnd, TYP_VOID);
        restoreCall->gtArgs.PushFront(m_comp, NewCallArg::Primitive(valuePlaceholder));

        m_comp->compCurBB = resumeBB;
        m_comp->fgMorphTree(restoreCall);

        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_comp, restoreCall));

        LIR::Use valueUse;
        bool     gotUse = LIR::AsRange(resumeBB).TryGetUse(valuePlaceholder, &valueUse);
        assert(gotUse);

        GenTree* continuation      = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned execContextOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExecContextOffset;
        GenTree* execContextValue  = LoadFromOffset(continuation, execContextOffset, TYP_REF);

        LIR::AsRange(resumeBB).InsertBefore(valuePlaceholder, LIR::SeqTree(m_comp, execContextValue));
        valueUse.ReplaceWith(execContextValue);

        LIR::AsRange(resumeBB).Remove(valuePlaceholder);
    }

    // Copy data
    for (const LiveLocalInfo& inf : layout.Locals)
    {
        LclVarDsc* dsc = m_comp->lvaGetDesc(inf.LclNum);

        GenTree* continuation = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offset       = OFFSETOF__CORINFO_Continuation__data + inf.Offset;
        GenTree* cns          = m_comp->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
        GenTree* addr         = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuation, cns);

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
// AsyncTransformation::RethrowExceptionOnResumption:
//   Create IR that checks for an exception and rethrows it at the original
//   suspension point if necessary.
//
// Parameters:
//   block                 - The block containing the async call
//   layout                - Layout information for the continuation object
//   resumeBB              - Basic block to append IR to
//
// Returns:
//   The new non-exception successor basic block for resumption. This is the
//   basic block where execution will continue if there was no exception to
//   rethrow.
//
BasicBlock* AsyncTransformation::RethrowExceptionOnResumption(BasicBlock*               block,
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
    BasicBlock* remainder = resumeBB->GetTarget();
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
    GenTree* continuation    = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
    unsigned exceptionOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExceptionOffset;
    GenTree* exceptionInd    = LoadFromOffset(continuation, exceptionOffset, TYP_REF);
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
//   layout                - Layout information for the continuation object
//   storeResultBB         - Basic block to append IR to
//
void AsyncTransformation::CopyReturnValueOnResumption(GenTreeCall*              call,
                                                      const CallDefinitionInfo& callDefInfo,
                                                      const ContinuationLayout& layout,
                                                      BasicBlock*               storeResultBB)
{
    GenTree* resultBase   = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
    unsigned resultOffset = OFFSETOF__CORINFO_Continuation__data + layout.ReturnValOffset;

    assert(callDefInfo.DefinitionNode != nullptr);
    LclVarDsc* resultLcl = m_comp->lvaGetDesc(callDefInfo.DefinitionNode);

    // TODO-TP: We can use liveness to avoid generating a lot of this IR.
    if (call->gtReturnType == TYP_STRUCT)
    {
        if (m_comp->lvaGetPromotionType(resultLcl) != Compiler::PROMOTION_TYPE_INDEPENDENT)
        {
            GenTree* resultOffsetNode = m_comp->gtNewIconNode((ssize_t)resultOffset, TYP_I_IMPL);
            GenTree* resultAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, resultBase, resultOffsetNode);
            GenTree* resultData =
                m_comp->gtNewLoadValueNode(layout.ReturnStructLayout, resultAddr, GTF_IND_NONFAULTING);
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

                // Can be reallocated by above call to GetResultBaseVar
                resultLcl = m_comp->lvaGetDesc(callDefInfo.DefinitionNode);
            }

            assert(callDefInfo.DefinitionNode->OperIs(GT_STORE_LCL_VAR));
            for (unsigned i = 0; i < resultLcl->lvFieldCnt; i++)
            {
                unsigned   fieldLclNum = resultLcl->lvFieldLclStart + i;
                LclVarDsc* fieldDsc    = m_comp->lvaGetDesc(fieldLclNum);

                unsigned fldOffset = resultOffset + fieldDsc->lvFldOffset;
                GenTree* value     = LoadFromOffset(resultBase, fldOffset, fieldDsc->TypeGet(), GTF_IND_NONFAULTING);
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
        GenTree* value = LoadFromOffset(resultBase, resultOffset, call->gtReturnType, GTF_IND_NONFAULTING);

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
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
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
        unsigned stateOffset     = m_comp->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
        GenTree* stateOffsetNode = m_comp->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_comp->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_comp->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* switchNode      = m_comp->gtNewOperNode(GT_SWITCH, TYP_VOID, stateInd);

        LIR::AsRange(switchBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, switchNode);

        m_comp->fgHasSwitch = true;

        // Default case. TODO-CQ: Support bbsHasDefault = false before lowering.
        m_resumptionBBs.push_back(m_resumptionBBs[0]);
        const size_t     numCases       = m_resumptionBBs.size();
        FlowEdge** const cases          = new (m_comp, CMK_FlowEdge) FlowEdge*[numCases * 2];
        FlowEdge** const succs          = cases + numCases;
        unsigned         numUniqueSuccs = 0;

        const weight_t stateLikelihood = 1.0 / m_resumptionBBs.size();
        for (size_t i = 0; i < numCases; i++)
        {
            FlowEdge* const edge = m_comp->fgAddRefPred(m_resumptionBBs[i], switchBB);
            edge->setLikelihood(stateLikelihood);
            cases[i] = edge;

            if (edge->getDupCount() == 1)
            {
                succs[numUniqueSuccs++] = edge;
            }
        }

        BBswtDesc* const swtDesc = new (m_comp, CMK_BasicBlock)
            BBswtDesc(succs, numUniqueSuccs, cases, (unsigned)numCases, /* hasDefault */ true);
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
        m_comp->fgRedirectEdge(newEntryBB->TrueEdgeRef(), checkILOffsetBB);
        newEntryBB->GetTrueEdge()->setLikelihood(0);
        checkILOffsetBB->inheritWeightPercentage(newEntryBB, 0);

        FlowEdge* toOnContinuationBB = m_comp->fgAddRefPred(onContinuationBB, checkILOffsetBB);
        FlowEdge* toCallHelperBB     = m_comp->fgAddRefPred(callHelperBB, checkILOffsetBB);
        checkILOffsetBB->SetCond(toCallHelperBB, toOnContinuationBB);
        toCallHelperBB->setLikelihood(0);
        toOnContinuationBB->setLikelihood(1);
        callHelperBB->inheritWeightPercentage(checkILOffsetBB, 0);

        // We need to dispatch to the OSR version if the IL offset is non-negative.
        continuationArg           = m_comp->gtNewLclvNode(m_comp->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Continuation__data;
        GenTree* ilOffset         = LoadFromOffset(continuationArg, offsetOfIlOffset, TYP_INT);
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
        m_comp->fgRedirectEdge(newEntryBB->TrueEdgeRef(), checkILOffsetBB);
        newEntryBB->GetTrueEdge()->setLikelihood(0);
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
        unsigned offsetOfIlOffset = OFFSETOF__CORINFO_Continuation__data;
        GenTree* ilOffset         = LoadFromOffset(continuationArg, offsetOfIlOffset, TYP_INT);
        GenTree* zero             = m_comp->gtNewIconNode(0);
        GenTree* ltZero           = m_comp->gtNewOperNode(GT_LT, TYP_INT, ilOffset, zero);
        GenTree* jtrue            = m_comp->gtNewOperNode(GT_JTRUE, TYP_VOID, ltZero);
        LIR::AsRange(checkILOffsetBB).InsertAtEnd(LIR::SeqTree(m_comp, jtrue));
    }
}
