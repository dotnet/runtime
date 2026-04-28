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
// SetCallEntrypointForR2R:
//   Set the entrypoint for a call when compiling for Ready-to-Run.
//
// Parameters:
//   call     - The call node to set the entrypoint on.
//   compiler - The compiler instance.
//   handle   - The method handle to look up the entrypoint for.
//
static void SetCallEntrypointForR2R(GenTreeCall* call, Compiler* compiler, CORINFO_METHOD_HANDLE handle)
{
#ifdef FEATURE_READYTORUN
    if (!compiler->IsReadyToRun())
    {
        return;
    }
    CORINFO_CONST_LOOKUP entryPoint;
    compiler->info.compCompHnd->getFunctionEntryPoint(handle, &entryPoint);
    call->setEntryPoint(entryPoint);
#endif
}

//------------------------------------------------------------------------
// Compiler::SaveAsyncContexts:
//   Insert code in async methods that saves and restores contexts.
//
// Returns:
//   Suitable phase status.
//
// Remarks:
//   This inserts code to save the current ExecutionContext and
//   SynchronizationContext at the beginning of async functions, and code that
//   restores these contexts at the end. Additionally inserts uses of each of
//   these context at async calls to model the fact that on suspension, these
//   locals will be used there.
//
PhaseStatus Compiler::SaveAsyncContexts()
{
    if ((info.compMethodInfo->options & CORINFO_ASYNC_SAVE_CONTEXTS) == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Create locals for ExecutionContext and SynchronizationContext
    lvaAsyncExecutionContextVar                     = lvaGrabTemp(false DEBUGARG("Async ExecutionContext"));
    lvaGetDesc(lvaAsyncExecutionContextVar)->lvType = TYP_REF;

    lvaAsyncSynchronizationContextVar                     = lvaGrabTemp(false DEBUGARG("Async SynchronizationContext"));
    lvaGetDesc(lvaAsyncSynchronizationContextVar)->lvType = TYP_REF;

    if (opts.IsOSR())
    {
        lvaGetDesc(lvaAsyncExecutionContextVar)->lvIsOSRLocal       = true;
        lvaGetDesc(lvaAsyncSynchronizationContextVar)->lvIsOSRLocal = true;
    }

    // Create try-fault structure. This is actually a try-finally, but we
    // manually insert the restore code in a (merged) return block, so EH wise
    // we only need to restore on fault.
    BasicBlock* const tryBegBB  = fgSplitBlockAtBeginning(fgFirstBB);
    BasicBlock* const tryLastBB = fgLastBB;

    // Create fault handler block
    BasicBlock* faultBB = fgNewBBafter(BBJ_EHFAULTRET, tryLastBB, false);
    faultBB->bbRefs     = 1; // Artificial ref count
    faultBB->inheritWeightPercentage(tryBegBB, 0);

    // Add a new EH table entry. It encloses all others, so placing it at the
    // end is the right thing to do.
    unsigned  XTnew    = compHndBBtabCount;
    EHblkDsc* newEntry = fgTryAddEHTableEntries(XTnew);

    if (newEntry == nullptr)
    {
        IMPL_LIMITATION("too many exception clauses");
    }

    // Initialize the new entry
    asyncContextRestoreEHID  = impInlineRoot()->compEHID++;
    newEntry->ebdID          = asyncContextRestoreEHID;
    newEntry->ebdHandlerType = EH_HANDLER_FAULT;

    newEntry->ebdTryBeg  = tryBegBB;
    newEntry->ebdTryLast = tryLastBB;

    newEntry->ebdHndBeg  = faultBB;
    newEntry->ebdHndLast = faultBB;

    newEntry->ebdTyp = 0; // unused for fault

    newEntry->ebdEnclosingTryIndex = EHblkDsc::NO_ENCLOSING_INDEX;
    newEntry->ebdEnclosingHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;

    newEntry->ebdTryBegOffset    = tryBegBB->bbCodeOffs;
    newEntry->ebdTryEndOffset    = tryLastBB->bbCodeOffsEnd;
    newEntry->ebdFilterBegOffset = 0;
    newEntry->ebdHndBegOffset    = 0;
    newEntry->ebdHndEndOffset    = 0;

    // Set flags on new region
    tryBegBB->SetFlags(BBF_DONT_REMOVE | BBF_IMPORTED);
    faultBB->SetFlags(BBF_DONT_REMOVE | BBF_IMPORTED);
    faultBB->SetCatchType(BBCT_FAULT);

    tryBegBB->setTryIndex(XTnew);
    tryBegBB->clearHndIndex();

    faultBB->clearTryIndex();
    faultBB->setHndIndex(XTnew);

    // Walk user code blocks and set try index
    for (BasicBlock* tmpBB = tryBegBB->Next(); tmpBB != faultBB; tmpBB = tmpBB->Next())
    {
        if (!tmpBB->hasTryIndex())
        {
            tmpBB->setTryIndex(XTnew);
        }
    }

    // Walk EH table and update enclosing try indices
    for (unsigned XTnum = 0; XTnum < XTnew; XTnum++)
    {
        EHblkDsc* HBtab = &compHndBBtab[XTnum];
        if (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
        {
            HBtab->ebdEnclosingTryIndex = (unsigned short)XTnew;
        }
    }

    JITDUMP("Created EH descriptor EH#%u for try/fault wrapping body to save/restore async contexts\n", XTnew);
    INDEBUG(fgVerifyHandlerTab());

    // Get async helper methods
    CORINFO_ASYNC_INFO* asyncInfo = eeGetAsyncInfo();

    // Insert CaptureContexts call before the try (keep it before so the
    // try/finally can be removed if there is no exception side effects).
    // For OSR, we did this in the tier0 method.
    if (!opts.IsOSR())
    {
        GenTreeCall* captureCall = gtNewCallNode(CT_USER_FUNC, asyncInfo->captureContextsMethHnd, TYP_VOID);
        SetCallEntrypointForR2R(captureCall, this, asyncInfo->captureContextsMethHnd);
        captureCall->gtArgs.PushFront(this,
                                      NewCallArg::Primitive(gtNewLclAddrNode(lvaAsyncSynchronizationContextVar, 0)));
        captureCall->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclAddrNode(lvaAsyncExecutionContextVar, 0)));
        lvaGetDesc(lvaAsyncSynchronizationContextVar)->lvHasLdAddrOp = true;
        lvaGetDesc(lvaAsyncExecutionContextVar)->lvHasLdAddrOp       = true;

        CORINFO_CALL_INFO callInfo = {};
        callInfo.hMethod           = captureCall->gtCallMethHnd;
        callInfo.methodFlags       = info.compCompHnd->getMethodAttribs(callInfo.hMethod);
        impMarkInlineCandidate(captureCall, MAKE_METHODCONTEXT(callInfo.hMethod), false, &callInfo, compInlineContext);

        Statement* captureStmt = fgNewStmtFromTree(captureCall);
        fgInsertStmtAtBeg(fgFirstBB, captureStmt);

        JITDUMP("Inserted capture\n");
        DISPSTMT(captureStmt);
    }

    // Insert RestoreContexts call in fault (exceptional case)
    // First argument: resumed = (continuation != null)
    GenTree* resumed;
    if (compIsForInlining())
    {
        resumed = gtNewFalse();
    }
    else
    {
        GenTree* continuation = gtNewLclvNode(lvaAsyncContinuationArg, TYP_REF);
        GenTree* null         = gtNewNull();
        resumed               = gtNewOperNode(GT_NE, TYP_INT, continuation, null);
    }

    GenTreeCall* restoreCall = gtNewCallNode(CT_USER_FUNC, asyncInfo->restoreContextsMethHnd, TYP_VOID);
    SetCallEntrypointForR2R(restoreCall, this, asyncInfo->restoreContextsMethHnd);
    restoreCall->gtArgs.PushFront(this,
                                  NewCallArg::Primitive(gtNewLclVarNode(lvaAsyncSynchronizationContextVar, TYP_REF)));
    restoreCall->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclVarNode(lvaAsyncExecutionContextVar, TYP_REF)));
    restoreCall->gtArgs.PushFront(this, NewCallArg::Primitive(resumed));

    Statement* restoreStmt = fgNewStmtFromTree(restoreCall);
    fgInsertStmtAtEnd(faultBB, restoreStmt);

    // Now insert uses of the new contexts to all async calls (modelling the
    // fact that on suspension, we restore the context from those values). Also
    // convert BBJ_RETURNs into an exit to a block outside the region.
    BasicBlock* newReturnBB     = nullptr;
    unsigned    mergedReturnLcl = BAD_VAR_NUM;

    for (BasicBlock* block : Blocks())
    {
        if (!compIsForInlining())
        {
            AddContextArgsToAsyncCalls(block);
        }

        if (!block->KindIs(BBJ_RETURN) || (block == newReturnBB))
        {
            continue;
        }

        JITDUMP("Merging BBJ_RETURN block " FMT_BB "\n", block->bbNum);

        if (newReturnBB == nullptr)
        {
            newReturnBB = CreateReturnBB(&mergedReturnLcl);
            newReturnBB->inheritWeightPercentage(block, 0);
        }

        // When inlining we do merging during import, so we do not need to do
        // any storing there.
        if (!compIsForInlining())
        {
            // Store return value to common local
            Statement* retStmt = block->lastStmt();
            assert((retStmt != nullptr) && retStmt->GetRootNode()->OperIs(GT_RETURN));

            if (mergedReturnLcl != BAD_VAR_NUM)
            {
                GenTree*   retVal      = retStmt->GetRootNode()->AsOp()->GetReturnValue();
                Statement* insertAfter = retStmt;
                GenTree*   storeRetVal = gtNewTempStore(mergedReturnLcl, retVal, CHECK_SPILL_NONE, &insertAfter,
                                                        retStmt->GetDebugInfo(), block);
                Statement* storeStmt   = fgNewStmtFromTree(storeRetVal);
                fgInsertStmtAtEnd(block, storeStmt);
                JITDUMP("Inserted store to common return local\n");
                DISPSTMT(storeStmt);
            }

            retStmt->GetRootNode()->gtBashToNOP();
        }

        // Jump to new shared restore + return block
        block->SetKindAndTargetEdge(BBJ_ALWAYS, fgAddRefPred(newReturnBB, block));
        fgReturnCount--;
    }

    if (newReturnBB != nullptr)
    {
        newReturnBB->bbWeight = newReturnBB->computeIncomingWeight();
    }

    // After merging of returns we have at most 1 return (and we may have 0, if
    // there were no returns before due to infinite loops or exceptions).
    assert(fgReturnCount <= 1);

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// Compiler::AddContextArgsToAsyncCalls:
//   Add uses of the saved ExecutionContext and SynchronizationContext to all
//   async calls.
//
// Remarks:
//   This models the fact that calls have uses of the saved contexts on
//   suspension. The async transformation will later move the uses into the
//   suspension code path.
//
void Compiler::AddContextArgsToAsyncCalls(BasicBlock* block)
{
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
            GenTree* tree = *use;
            if ((tree->gtFlags & GTF_CALL) == 0)
            {
                return WALK_SKIP_SUBTREES;
            }

            if (!tree->IsCall() || !tree->AsCall()->IsAsync())
            {
                return WALK_CONTINUE;
            }

            GenTreeCall* call    = tree->AsCall();
            GenTree*     execCtx = m_compiler->gtNewLclVarNode(m_compiler->lvaAsyncExecutionContextVar, TYP_REF);
            GenTree*     syncCtx = m_compiler->gtNewLclVarNode(m_compiler->lvaAsyncSynchronizationContextVar, TYP_REF);
            JITDUMP("Adding exec context [%06u], sync context [%06u] to async call [%06u]\n", dspTreeID(execCtx),
                    dspTreeID(syncCtx), dspTreeID(call));
            call->gtArgs.PushFront(m_compiler,
                                   NewCallArg::Primitive(syncCtx).WellKnown(WellKnownArg::AsyncSynchronizationContext));
            call->gtArgs.PushFront(m_compiler,
                                   NewCallArg::Primitive(execCtx).WellKnown(WellKnownArg::AsyncExecutionContext));
            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this);
    for (Statement* stmt : block->Statements())
    {
        visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    }
}

//------------------------------------------------------------------------
// Compiler::CreateReturnBB:
//   Create a new return block to exit the async method.
//
// Parameters:
//   mergedReturnLcl - [out] The local created to hold the merged return value.
//                     BAD_VAR_NUM if the async method does not return a result.
//
// Returns:
//   A new basic block that restores contexts and returns a merged result.
//
BasicBlock* Compiler::CreateReturnBB(unsigned* mergedReturnLcl)
{
    BasicBlock* newReturnBB = fgNewBBafter(BBJ_RETURN, fgLastBB, /* extendRegion */ false);
    newReturnBB->bbTryIndex = 0; // EH region
    newReturnBB->bbHndIndex = 0;
    fgReturnCount++;
    JITDUMP("Created new BBJ_RETURN block " FMT_BB "\n", newReturnBB->bbNum);

    // Insert "restore" call
    CORINFO_ASYNC_INFO* asyncInfo = eeGetAsyncInfo();

    GenTree* resumed;
    if (compIsForInlining())
    {
        resumed = gtNewFalse();
    }
    else
    {
        GenTree* continuation = gtNewLclvNode(lvaAsyncContinuationArg, TYP_REF);
        GenTree* null         = gtNewNull();
        resumed               = gtNewOperNode(GT_NE, TYP_INT, continuation, null);
    }

    GenTreeCall* restoreCall = gtNewCallNode(CT_USER_FUNC, asyncInfo->restoreContextsMethHnd, TYP_VOID);
    SetCallEntrypointForR2R(restoreCall, this, asyncInfo->restoreContextsMethHnd);
    restoreCall->gtArgs.PushFront(this,
                                  NewCallArg::Primitive(gtNewLclVarNode(lvaAsyncSynchronizationContextVar, TYP_REF)));
    restoreCall->gtArgs.PushFront(this, NewCallArg::Primitive(gtNewLclVarNode(lvaAsyncExecutionContextVar, TYP_REF)));
    restoreCall->gtArgs.PushFront(this, NewCallArg::Primitive(resumed));

    // This restore is an inline candidate (unlike the fault one)
    CORINFO_CALL_INFO callInfo = {};
    callInfo.hMethod           = restoreCall->gtCallMethHnd;
    callInfo.methodFlags       = info.compCompHnd->getMethodAttribs(callInfo.hMethod);
    impMarkInlineCandidate(restoreCall, MAKE_METHODCONTEXT(callInfo.hMethod), false, &callInfo, compInlineContext);

    Statement* restoreStmt = fgNewStmtFromTree(restoreCall);
    fgInsertStmtAtEnd(newReturnBB, restoreStmt);
    JITDUMP("Inserted restore statement in return block\n");
    DISPSTMT(restoreStmt);

    if (!compIsForInlining())
    {
        *mergedReturnLcl = BAD_VAR_NUM;

        GenTree* ret;
        if (compMethodHasRetVal())
        {
            *mergedReturnLcl = lvaGrabTemp(false DEBUGARG("Async merged return local"));

            var_types retLclType = compMethodReturnsRetBufAddr() ? TYP_BYREF : genActualType(info.compRetType);

            if (varTypeIsStruct(retLclType))
            {
                lvaSetStruct(*mergedReturnLcl, info.compMethodInfo->args.retTypeClass, false);

                if (compMethodReturnsMultiRegRetType())
                {
                    lvaGetDesc(*mergedReturnLcl)->lvIsMultiRegRet = true;
                }
            }
            else
            {
                lvaGetDesc(*mergedReturnLcl)->lvType = retLclType;
            }

            GenTree* retTemp = gtNewLclVarNode(*mergedReturnLcl);
            ret              = gtNewOperNode(GT_RETURN, retTemp->TypeGet(), retTemp);
        }
        else
        {
            ret = new (this, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID);
        }

        Statement* retStmt = fgNewStmtFromTree(ret);

        fgInsertStmtAtEnd(newReturnBB, retStmt);
        JITDUMP("Inserted return statement in return block\n");
        DISPSTMT(retStmt);
    }

    return newReturnBB;
}

//------------------------------------------------------------------------
// ContinuationLayoutBuilder::AddReturn:
//   Add a return type to the continuation layout, if it is not already
//   present.
//
// Parameters:
//   info - The return type information to add.
//
void ContinuationLayoutBuilder::AddReturn(const ReturnTypeInfo& info)
{
    for (const ReturnTypeInfo& ret : m_returns)
    {
        if (ret.ReturnType != info.ReturnType)
        {
            continue;
        }

        if ((ret.ReturnType == TYP_STRUCT) && !ClassLayout::AreCompatible(ret.ReturnLayout, info.ReturnLayout))
        {
            continue;
        }

        // This return type is already in the layout, no need to add another slot for it.
        return;
    }

    m_returns.push_back(info);
}

//------------------------------------------------------------------------
// ContinuationLayoutBuilder::AddLocal:
//   Add a local to the continuation layout. Locals must be added in
//   ascending order by local number.
//
// Parameters:
//   lclNum - The local number to add.
//
void ContinuationLayoutBuilder::AddLocal(unsigned lclNum)
{
    assert(m_locals.empty() || (lclNum > m_locals[m_locals.size() - 1]));
    m_locals.push_back(lclNum);
}

//------------------------------------------------------------------------
// ContinuationLayoutBuilder::ContainsLocal:
//   Check if the specified local is in this layout.
//
// Parameters:
//   lclNum - The local number to check.
//
// Returns:
//   True if the local is contained in this layout.
//
bool ContinuationLayoutBuilder::ContainsLocal(unsigned lclNum) const
{
    return BinarySearch(m_locals.data(), (int)m_locals.size(), lclNum) != nullptr;
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
    PhaseStatus             result = PhaseStatus::MODIFIED_NOTHING;
    ArrayStack<BasicBlock*> blocksWithNormalAwaits(m_compiler->getAllocator(CMK_Async));
    ArrayStack<BasicBlock*> blocksWithTailAwaits(m_compiler->getAllocator(CMK_Async));
    int                     numNormalAwaits = 0;
    int                     numTailAwaits   = 0;
    FindAwaits(blocksWithNormalAwaits, blocksWithTailAwaits, &numNormalAwaits, &numTailAwaits);

    if (numNormalAwaits + numTailAwaits > 1)
    {
        CreateSharedReturnBB();
    }

    // Transform all tail awaits first. They will not require running all of
    // our analyses.
    if (numTailAwaits > 0)
    {
        JITDUMP("Found %d tail awaits in %d blocks\n", numTailAwaits, blocksWithTailAwaits.Height());
        TransformTailAwaits(blocksWithTailAwaits);
        m_compiler->fgInvalidateDfsTree();

        if (numNormalAwaits > 0)
        {
            // This may have changed blocks, so refind the normal awaits.
            blocksWithNormalAwaits.Reset();
            blocksWithTailAwaits.Reset();
            numNormalAwaits = 0;
            numTailAwaits   = 0;
            FindAwaits(blocksWithNormalAwaits, blocksWithTailAwaits, &numNormalAwaits, &numTailAwaits);
        }

        result = PhaseStatus::MODIFIED_EVERYTHING;
    }

    JITDUMP("Found %d awaits in %d blocks\n", numNormalAwaits, blocksWithNormalAwaits.Height());

    if (numNormalAwaits <= 0)
    {
        return result;
    }

    m_compiler->compSuspensionPoints = new (m_compiler, CMK_Async)
        jitstd::vector<ICorDebugInfo::AsyncSuspensionPoint>(m_compiler->getAllocator(CMK_Async));
    m_compiler->compAsyncVars = new (m_compiler, CMK_Async)
        jitstd::vector<ICorDebugInfo::AsyncContinuationVarInfo>(m_compiler->getAllocator(CMK_Async));

    m_asyncInfo = m_compiler->eeGetAsyncInfo();

    // Compute liveness to be used for determining what must be captured on
    // suspension.
    if (m_compiler->m_dfsTree == nullptr)
    {
        m_compiler->m_dfsTree = m_compiler->fgComputeDfs<false>();
    }

    m_compiler->lvaComputePreciseRefCounts(/* isRecompute */ true, /* setSlotNumbers */ false);
    m_compiler->fgAsyncLiveness();
    INDEBUG(m_compiler->mostRecentlyActivePhase = PHASE_ASYNC);
    VarSetOps::AssignNoCopy(m_compiler, m_compiler->compCurLife, VarSetOps::MakeEmpty(m_compiler));

    // Compute locals unchanged from their default values
    DefaultValueAnalysis defaultValues(m_compiler);
    defaultValues.Run();

    // Compute locals unchanged if we reuse a continuation
    PreservedValueAnalysis preservedValues(m_compiler);
    preservedValues.Run(blocksWithNormalAwaits);

    AsyncAnalysis analyses(m_compiler, defaultValues, preservedValues);

    // Now walk the IR for all the blocks that contain async calls. Keep track
    // the state of the analyses and outstanding LIR edges as we go; the LIR
    // edges that cross async calls are additional live variables that must be
    // spilled.
    jitstd::vector<GenTree*> defs(m_compiler->getAllocator(CMK_Async));

    for (int i = 0; i < blocksWithNormalAwaits.Height(); i++)
    {
        assert(defs.size() == 0);

        BasicBlock* block = blocksWithNormalAwaits.Bottom(i);
        analyses.StartBlock(block);

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

                if (tree->IsCall())
                {
                    GenTreeCall* call = tree->AsCall();
                    if (call->IsAsync() && !call->IsTailCall() && !call->GetAsyncInfo().IsTailAwait)
                    {
                        // Transform call; continue with the remainder block.
                        // Transform takes care to update analyses.
                        Transform(block, tree->AsCall(), defs, analyses, &block);
                        defs.clear();
                        any = true;
                        break;
                    }
                }

                // Update analyses to reflect state after this node.
                analyses.Update(tree);

                // Push a new definition if necessary; this defined value is
                // now a live LIR edge.
                if (tree->IsValue() && !tree->IsUnusedValue())
                {
                    defs.push_back(tree);
                }
            }
        } while (any);
    }

    if (ReuseContinuations())
    {
        // Set up the local containing the continuation we can reuse. For OSR
        // things are special: we can transition to the OSR method after having
        // resumed in the tier0 method. In that case we end up with the tier0
        // continuation in the OSR method, but we cannot reuse it.
        if (m_compiler->opts.IsOSR())
        {
            m_reuseContinuationVar = m_compiler->lvaGrabTemp(false DEBUGARG("OSR reusable continuation"));
            m_compiler->lvaGetDesc(m_reuseContinuationVar)->lvType = TYP_REF;
        }
        else
        {
            m_reuseContinuationVar = m_compiler->lvaAsyncContinuationArg;
        }
    }

    CreateResumptionsAndSuspensions();

    // After transforming all async calls we have created resumption blocks;
    // create the resumption switch.
    CreateResumptionSwitch();

    m_compiler->fgInvalidateDfsTree();

    if (m_compiler->opts.OptimizationDisabled())
    {
        // Rest of the compiler does not expect that we started tracking locals, so reset that state.
        for (unsigned i = 0; i < m_compiler->lvaTrackedCount; i++)
        {
            m_compiler->lvaGetDesc(m_compiler->lvaTrackedToVarNum[i])->lvTracked = false;
        }

        m_compiler->lvaCurEpoch++;
        m_compiler->lvaTrackedCount             = 0;
        m_compiler->lvaTrackedCountInSizeTUnits = 0;

        for (BasicBlock* block : m_compiler->Blocks())
        {
            block->bbLiveIn  = VarSetOps::UninitVal();
            block->bbLiveOut = VarSetOps::UninitVal();
            block->bbVarUse  = VarSetOps::UninitVal();
            block->bbVarDef  = VarSetOps::UninitVal();
        }
        m_compiler->fgBBVarSetsInited = false;
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// AsyncTransformation::FindAwaits:
//   Find the blocks that have awaits in them and do some accounting of how
//   many awaits there are.
//
// Parameters:
//   blocksWithNormalAwaits - [out] Blocks with normal awaits are pushed onto this stack
//   blocksWithTailAwaits   - [out] Blocks with tail awaits are pushed onto this stack
//   numNormalAwaits        - [out] Number of normal awaits found
//   numTailAwaits          - [out] Number of tail awaits found
//
void AsyncTransformation::FindAwaits(ArrayStack<BasicBlock*>& blocksWithNormalAwaits,
                                     ArrayStack<BasicBlock*>& blocksWithTailAwaits,
                                     int*                     numNormalAwaits,
                                     int*                     numTailAwaits)
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        bool hasNormalAwait = false;
        bool hasTailAwait   = false;
        for (GenTree* tree : LIR::AsRange(block))
        {
            if (!tree->IsCall() || !tree->AsCall()->IsAsync() || tree->AsCall()->IsTailCall())
            {
                continue;
            }

            if (tree->AsCall()->GetAsyncInfo().IsTailAwait)
            {
                hasTailAwait = true;
                (*numTailAwaits)++;
            }
            else
            {
                hasNormalAwait = true;
                (*numNormalAwaits)++;
            }
        }

        if (hasNormalAwait)
        {
            blocksWithNormalAwaits.Push(block);
        }

        if (hasTailAwait)
        {
            blocksWithTailAwaits.Push(block);
        }
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::TransformTailAwaits:
//   Transform all tail awaits in the specified blocks.
//
// Parameters:
//   blocksWithTailAwaits   - Blocks containing tail awaits
//
void AsyncTransformation::TransformTailAwaits(ArrayStack<BasicBlock*>& blocksWithTailAwaits)
{
    for (int i = 0; i < blocksWithTailAwaits.Height(); i++)
    {
        BasicBlock* block = blocksWithTailAwaits.Bottom(i);

        bool any;
        do
        {
            any = false;
            for (GenTree* tree : LIR::AsRange(block))
            {
                if (tree->IsCall() && tree->AsCall()->IsAsync() && !tree->AsCall()->IsTailCall() &&
                    tree->AsCall()->GetAsyncInfo().IsTailAwait)
                {
                    TransformTailAwait(block, tree->AsCall(), &block);
                    any = true;
                    break;
                }
            }
        } while (any);
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::TransformTailAwait:
//   Transform an await that was marked as a tail await.
//
// Parameters:
//   block     - The block containing the async call
//   call      - The async call
//   nextBlock - [out] The next block to process
//
void AsyncTransformation::TransformTailAwait(BasicBlock* block, GenTreeCall* call, BasicBlock** nextBlock)
{
    JITDUMP("Transforming tail await [%06u] in " FMT_BB "\n", Compiler::dspTreeID(call), block->bbNum);

    CallDefinitionInfo callDefInfo = CanonicalizeCallDefinition(block, call, nullptr);

    BasicBlock* suspension = CreateTailAwaitSuspension(block, call);

    CreateCheckAndSuspendAfterCall(block, call, callDefInfo, suspension, nextBlock);
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateTailAwaitSuspension:
//   Create the basic block that when branched to suspends execution after the
//   specified async call marked as a tail await.
//
// Parameters:
//   block    - The block containing the async call
//   call     - The async call
//
// Returns:
//   The new basic block that was created.
//
BasicBlock* AsyncTransformation::CreateTailAwaitSuspension(BasicBlock* block, GenTreeCall* call)
{
    BasicBlock* sharedReturnBB = m_sharedReturnBB;

    if (m_lastSuspensionBB == nullptr)
    {
        m_lastSuspensionBB = m_compiler->fgLastBBInMainFunction();
    }

    BasicBlock* suspendBB = m_compiler->fgNewBBafter(BBJ_RETURN, m_lastSuspensionBB, false);
    suspendBB->clearTryIndex();
    suspendBB->clearHndIndex();
    suspendBB->inheritWeightPercentage(block, 0);
    m_lastSuspensionBB = suspendBB;

    if (sharedReturnBB != nullptr)
    {
        suspendBB->SetKindAndTargetEdge(BBJ_ALWAYS, m_compiler->fgAddRefPred(sharedReturnBB, suspendBB));
    }

    JITDUMP("  Creating tail suspension " FMT_BB "\n", suspendBB->bbNum);

    GenTree* returnedContinuation = m_compiler->gtNewLclvNode(GetReturnedContinuationVar(), TYP_REF);

    if (suspendBB->KindIs(BBJ_RETURN))
    {
        GenTree* ret = m_compiler->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, returnedContinuation);
        LIR::AsRange(suspendBB).InsertAtEnd(returnedContinuation, ret);
    }
    else
    {
        GenTree* storeNewContinuation = m_compiler->gtNewStoreLclVarNode(GetNewContinuationVar(), returnedContinuation);
        LIR::AsRange(suspendBB).InsertAtEnd(returnedContinuation, storeNewContinuation);
    }

    return suspendBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::Transform:
//   Transform a single async call in the specified block.
//
// Parameters:
//   block     - The block containing the async call
//   call      - The async call
//   defs      - Current live LIR edges
//   analyses  - Analysis information about the async method
//   remainder - [out] Remainder block after the transformation
//
void AsyncTransformation::Transform(BasicBlock*               block,
                                    GenTreeCall*              call,
                                    jitstd::vector<GenTree*>& defs,
                                    AsyncAnalysis&            analyses,
                                    BasicBlock**              remainder)
{
#ifdef DEBUG
    if (m_compiler->verbose)
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

    bool      resumeReachable = analyses.IsResumeReachable();
    VARSET_TP mutatedSinceResumption(VarSetOps::MakeCopy(m_compiler, analyses.GetMutatedSinceResumption()));

    JITDUMP("  This suspension point is%s resume-reachable\n", resumeReachable ? "" : " NOT");
    if (resumeReachable)
    {
        JITDUMP("  Locals mutated since previous resumption: ");
        JITDUMPEXEC(AsyncAnalysis::PrintVarSet(m_compiler, mutatedSinceResumption));
    }

    ContinuationLayoutBuilder* layoutBuilder = new (m_compiler, CMK_Async) ContinuationLayoutBuilder(m_compiler);

    CreateLiveSetForSuspension(block, call, defs, analyses, layoutBuilder);

    BuildContinuation(block, call, ContinuationNeedsKeepAlive(analyses), layoutBuilder);

    CallDefinitionInfo callDefInfo = CanonicalizeCallDefinition(block, call, &analyses);

    unsigned stateNum = (unsigned)m_states.size();
    JITDUMP("  Assigned state %u\n", stateNum);

    BasicBlock* suspendBB = CreateSuspensionBlock(block, stateNum);

    CreateCheckAndSuspendAfterCall(block, call, callDefInfo, suspendBB, remainder);

    BasicBlock* resumeBB = CreateResumptionBlock(*remainder, stateNum);

    m_states.push_back(AsyncState(stateNum, layoutBuilder, block, call, callDefInfo, suspendBB, resumeBB,
                                  resumeReachable, mutatedSinceResumption));

    JITDUMP("\n");
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateLiveSetForSuspension:
//   Create the set of live state to be captured for suspension, for the
//   specified call.
//
// Parameters:
//   block         - The block containing the async call
//   call          - The async call
//   defs          - Current live LIR edges
//   analyses      - Async analyses state, including liveness and default-value info for locals
//   layoutBuilder - Layout being built
//
void AsyncTransformation::CreateLiveSetForSuspension(BasicBlock*                     block,
                                                     GenTreeCall*                    call,
                                                     const jitstd::vector<GenTree*>& defs,
                                                     AsyncAnalysis&                  analyses,
                                                     ContinuationLayoutBuilder*      layoutBuilder)
{
    SmallHashTable<unsigned, bool> excludedLocals(m_compiler->getAllocator(CMK_Async));

    // As a special case exclude locals that are fully defined by the call if
    // we don't have internal EH. Liveness does this automatically, but this
    // improves tier0 and also address exposed locals.
    auto visitDef = [&](const LocalDef& def) {
        if (def.IsEntire)
        {
            if (HasNonContextRestoreExceptionalFlow(block))
            {
                JITDUMP("  V%02u is fully defined but the block has exceptional flow\n", def.Def->GetLclNum());
            }
            else
            {
                JITDUMP("  V%02u is fully defined and will not be considered live\n", def.Def->GetLclNum());
                excludedLocals.AddOrUpdate(def.Def->GetLclNum(), true);
            }
        }
        return GenTree::VisitResult::Continue;
    };

    call->VisitLocalDefs(m_compiler, visitDef);

    // Exclude method-level context locals (only live on synchronous path)
    if (m_compiler->lvaAsyncSynchronizationContextVar != BAD_VAR_NUM)
    {
        excludedLocals.AddOrUpdate(m_compiler->lvaAsyncSynchronizationContextVar, true);
    }
    if (m_compiler->lvaAsyncExecutionContextVar != BAD_VAR_NUM)
    {
        excludedLocals.AddOrUpdate(m_compiler->lvaAsyncExecutionContextVar, true);
    }

    analyses.GetLiveLocals(layoutBuilder, [&](unsigned lclNum) {
        return !excludedLocals.Contains(lclNum);
    });
    LiftLIREdges(block, defs, layoutBuilder);

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("  %zu live locals\n", layoutBuilder->Locals().size());

        if (layoutBuilder->Locals().size() > 0)
        {
            const char* sep = "    ";
            for (unsigned lclNum : layoutBuilder->Locals())
            {
                printf("%sV%02u (%s)", sep, lclNum, varTypeName(m_compiler->lvaGetDesc(lclNum)->TypeGet()));
                sep = ", ";
            }

            printf("\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// AsyncTransformation::HasNonContextRestoreExceptionalFlow:
//   Check if there is internal control flow out of the specified block and if
//   that target is not the canonical "restore context" EH handler.
//
// Parameters:
//   block - The block
//
// Returns:
//   True if there is such control flow.
//
bool AsyncTransformation::HasNonContextRestoreExceptionalFlow(BasicBlock* block)
{
    if (!block->hasTryIndex())
    {
        return false;
    }

    EHblkDsc* ehDsc = m_compiler->ehGetDsc(block->getTryIndex());
    return (ehDsc->ebdID != m_compiler->asyncContextRestoreEHID) ||
           (ehDsc->ebdEnclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX);
}

//------------------------------------------------------------------------
// AsyncTransformation::LiftLIREdges:
//   Create locals capturing outstanding LIR edges and add information
//   indicating that these locals are live.
//
// Parameters:
//   block         - The block containing the definitions of the LIR edges
//   defs          - Current outstanding LIR edges
//   layoutBuilder - Continuation layout builder to add new locals to
//
void AsyncTransformation::LiftLIREdges(BasicBlock*                     block,
                                       const jitstd::vector<GenTree*>& defs,
                                       ContinuationLayoutBuilder*      layoutBuilder)
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
        //    LclVarDsc* dsc = m_compiler->lvaGetDesc(tree->AsLclVarCommon());
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

        unsigned newLclNum = use.ReplaceWithLclVar(m_compiler);
        layoutBuilder->AddLocal(newLclNum);
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
//   analyses - Information about analyses, used for liveness
//
// Returns:
//   True if we need to keep a LoaderAllocator for generic context or
//   collectible method alive.
//
bool AsyncTransformation::ContinuationNeedsKeepAlive(AsyncAnalysis& analyses)
{
    if (m_compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI))
    {
        // Native AOT doesn't have a LoaderAllocator
        return false;
    }

    const unsigned GENERICS_CTXT_FROM = CORINFO_GENERICS_CTXT_FROM_METHODDESC | CORINFO_GENERICS_CTXT_FROM_METHODTABLE;
    if (((m_compiler->info.compMethodInfo->options & GENERICS_CTXT_FROM) != 0) &&
        analyses.IsLive(m_compiler->info.compTypeCtxtArg))
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
        INDEBUG(NumObjRefs++);
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
// AsyncTransformation::BuildContinuation:
//   Determine what fields the continuation object needs for the specified
//   async call, including return values, exception, context, and OSR
//   support, and configure the layout builder accordingly.
//
// Parameters:
//   block          - The block containing the async call.
//   call           - The async call.
//   needsKeepAlive - Whether a KeepAlive field is needed to keep a
//                    LoaderAllocator alive.
//   layoutBuilder  - The continuation layout builder to configure.
//
void AsyncTransformation::BuildContinuation(BasicBlock*                block,
                                            GenTreeCall*               call,
                                            bool                       needsKeepAlive,
                                            ContinuationLayoutBuilder* layoutBuilder)
{
    if (call->gtReturnType != TYP_VOID)
    {
        ClassLayout* layout =
            call->gtReturnType == TYP_STRUCT ? m_compiler->typGetObjLayout(call->gtRetClsHnd) : nullptr;
        layoutBuilder->AddReturn(ReturnTypeInfo(call->gtReturnType, layout));
        JITDUMP("  Call has return; continuation will have return value\n");
    }

    // For OSR, we store the address of the OSR function at the beginning of
    // the data (and store 0 in the tier0 version). This must be at the
    // beginning because the tier0 and OSR versions need to agree on this.
    if (m_compiler->doesMethodHavePatchpoints() || m_compiler->opts.IsOSR())
    {
        JITDUMP("  Method %s; keeping OSR address at the beginning of non-GC data\n",
                m_compiler->doesMethodHavePatchpoints() ? "has patchpoints" : "is an OSR method");
        // Must be pointer sized for compatibility with Continuation methods that access fields
        layoutBuilder->SetNeedsOSRAddress();
    }

    if (HasNonContextRestoreExceptionalFlow(block))
    {
        // If we are enclosed in any try region that isn't our special "context
        // restore" try region then we need to rethrow an exception. For our
        // special "context restore" try region we know that it is a no-op on
        // the resumption path.
        layoutBuilder->SetNeedsException();
        JITDUMP("  " FMT_BB " is in try region %u; continuation will have exception\n", block->bbNum,
                block->getTryIndex());
    }

    if (call->GetAsyncInfo().ContinuationContextHandling == ContinuationContextHandling::ContinueOnCapturedContext)
    {
        layoutBuilder->SetNeedsContinuationContext();
        JITDUMP("  Continuation continues on captured context; continuation will have context\n");
    }

    if (needsKeepAlive)
    {
        layoutBuilder->SetNeedsKeepAlive();
        JITDUMP("  Continuation will have keep alive object\n");
    }

    layoutBuilder->SetNeedsExecutionContext();
    JITDUMP("  Call has async-only save and restore of ExecutionContext; continuation will have ExecutionContext\n");
}

#ifdef DEBUG
//------------------------------------------------------------------------
// ContinuationLayout::Dump:
//   Debug helper to print the continuation layout including offsets of all
//   fields and locals.
//
// Parameters:
//   indent - Number of spaces to indent the output.
//
void ContinuationLayout::Dump(int indent)
{
    printf("%*sContinuation layout (%u bytes):\n", indent, "", Size);
    if (OSRAddressOffset != UINT_MAX)
    {
        printf("%*s  +%03u OSR address\n", indent, "", OSRAddressOffset);
    }

    if (ExceptionOffset != UINT_MAX)
    {
        printf("%*s  +%03u Exception\n", indent, "", ExceptionOffset);
    }

    if (ContinuationContextOffset != UINT_MAX)
    {
        printf("%*s  +%03u Continuation context\n", indent, "", ContinuationContextOffset);
    }

    if (KeepAliveOffset != UINT_MAX)
    {
        printf("%*s  +%03u Keep alive object\n", indent, "", KeepAliveOffset);
    }

    if (ExecutionContextOffset != UINT_MAX)
    {
        printf("%*s  +%03u Execution context\n", indent, "", ExecutionContextOffset);
    }

    for (const LiveLocalInfo& inf : Locals)
    {
        printf("%*s  +%03u V%02u: %u bytes\n", indent, "", inf.Offset, inf.LclNum, inf.Size);
    }

    for (const ReturnInfo& ret : Returns)
    {
        printf("%*s  +%03u %u bytes for %s return\n", indent, "", ret.Offset, ret.Size,
               ret.Type.ReturnType == TYP_STRUCT ? ret.Type.ReturnLayout->GetClassName()
                                                 : varTypeName(ret.Type.ReturnType));
    }
}
#endif

//------------------------------------------------------------------------
// ContinuationLayout::FindReturn:
//   Find the return info entry matching the specified call's return type.
//
// Parameters:
//   comp - Compiler instance to use for looking up struct return layouts.
//   call - The async call whose return type to look up.
//
// Returns:
//   Pointer to the matching ReturnInfo entry.
//
const ReturnInfo* ContinuationLayout::FindReturn(Compiler* comp, GenTreeCall* call) const
{
    ClassLayout* layout = call->gtReturnType == TYP_STRUCT ? comp->typGetObjLayout(call->gtRetClsHnd) : nullptr;
    for (const ReturnInfo& ret : Returns)
    {
        if ((ret.Type.ReturnType == call->gtReturnType) &&
            ((call->gtReturnType != TYP_STRUCT) || ClassLayout::AreCompatible(ret.Type.ReturnLayout, layout)))
        {
            return &ret;
        }
    }

    assert(!"Could not find return for call");
    return nullptr;
}

//------------------------------------------------------------------------
// ContinuationLayoutBuilder::Create:
//   Finalize the layout by computing offsets for all fields, locals, and
//   return values. Allocates the continuation type from the VM.
//
// Returns:
//   The finalized ContinuationLayout with computed offsets and a class
//   handle for the continuation type.
//
ContinuationLayout* ContinuationLayoutBuilder::Create()
{
    ContinuationLayout* layout = new (m_compiler, CMK_Async) ContinuationLayout(m_compiler);
    layout->Locals.reserve(m_locals.size());

    for (unsigned lclNum : m_locals)
    {
        LclVarDsc*    dsc = m_compiler->lvaGetDesc(lclNum);
        LiveLocalInfo inf(lclNum);

        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            ClassLayout* layout = dsc->GetLayout();
            assert(!layout->HasGCByRef());

            if (layout->IsCustomLayout())
            {
                inf.Alignment = layout->HasGCPtr() ? TARGET_POINTER_SIZE : 1;
                inf.Size      = layout->GetSize();
            }
            else
            {
                inf.Alignment = m_compiler->info.compCompHnd->getClassAlignmentRequirement(layout->GetClassHandle());
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

        layout->Locals.push_back(inf);
    }

    jitstd::sort(layout->Locals.begin(), layout->Locals.end(), [=](const LiveLocalInfo& lhs, const LiveLocalInfo& rhs) {
        bool lhsIsRef = m_compiler->lvaGetDesc(lhs.LclNum)->TypeIs(TYP_REF);
        bool rhsIsRef = m_compiler->lvaGetDesc(rhs.LclNum)->TypeIs(TYP_REF);

        // Keep object refs first to improve sharability of continuation types.
        if (lhsIsRef != rhsIsRef)
        {
            return lhsIsRef;
        }

        // Otherwise prefer higher alignment first.
        if (lhs.HeapAlignment() != rhs.HeapAlignment())
        {
            return lhs.HeapAlignment() > rhs.HeapAlignment();
        }

        // Prefer lowest local num first for tiebreaker.
        return lhs.LclNum < rhs.LclNum;
    });

    for (const ReturnTypeInfo& ret : m_returns)
    {
        ReturnInfo retInfo(ret);

        if (ret.ReturnType == TYP_STRUCT)
        {
            retInfo.Size = ret.ReturnLayout->GetSize();
            retInfo.Alignment =
                m_compiler->info.compCompHnd->getClassAlignmentRequirement(ret.ReturnLayout->GetClassHandle());
        }
        else
        {
            retInfo.Size      = genTypeSize(ret.ReturnType);
            retInfo.Alignment = retInfo.Size;
        }

        layout->Returns.push_back(retInfo);
    }

    auto allocLayout = [layout](unsigned align, unsigned size) {
        layout->Size    = roundUp(layout->Size, align);
        unsigned offset = layout->Size;
        layout->Size += size;
        return offset;
    };

    if (m_needsOSRAddress)
    {
        // Must be pointer sized for compatibility with Continuation methods that access fields
        layout->OSRAddressOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    if (m_needsException)
    {
        layout->ExceptionOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    if (m_needsContinuationContext)
    {
        layout->ContinuationContextOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    // Now allocate all returns
    for (ReturnInfo& ret : layout->Returns)
    {
        // All returns must be pointer aligned because of the offset encoding in Continuation::Flags.
        layout->Size = roundUp(layout->Size, TARGET_POINTER_SIZE);
        ret.Offset   = allocLayout(ret.HeapAlignment(), ret.Size);
    }

    if (m_needsKeepAlive)
    {
        layout->KeepAliveOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    if (m_needsExecutionContext)
    {
        layout->ExecutionContextOffset = allocLayout(TARGET_POINTER_SIZE, TARGET_POINTER_SIZE);
    }

    // Then all locals
    for (LiveLocalInfo& inf : layout->Locals)
    {
        inf.Offset = allocLayout(inf.HeapAlignment(), inf.Size);
    }

    layout->Size = roundUp(layout->Size, TARGET_POINTER_SIZE);

    JITDUMPEXEC(layout->Dump(2));
    // Now create continuation type. First create bitmap of object refs.
    bool* objRefs = layout->Size < TARGET_POINTER_SIZE
                        ? nullptr
                        : new (m_compiler, CMK_Async) bool[layout->Size / TARGET_POINTER_SIZE]{};

    GCPointerBitMapBuilder bitmapBuilder(objRefs, layout->Size);
    bitmapBuilder.SetIfNotMax(layout->ExceptionOffset);
    bitmapBuilder.SetIfNotMax(layout->ContinuationContextOffset);
    bitmapBuilder.SetIfNotMax(layout->KeepAliveOffset);
    bitmapBuilder.SetIfNotMax(layout->ExecutionContextOffset);

    for (LiveLocalInfo& inf : layout->Locals)
    {
        LclVarDsc*   dsc = m_compiler->lvaGetDesc(inf.LclNum);
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

    for (ReturnInfo& ret : layout->Returns)
    {
        bitmapBuilder.SetType(ret.Offset, ret.Type.ReturnType, ret.Type.ReturnLayout);
    }

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("  Getting continuation layout size = %u, numGCRefs = %u\n", layout->Size, bitmapBuilder.NumObjRefs);
        bool* start        = objRefs;
        bool* endOfObjRefs = objRefs + layout->Size / TARGET_POINTER_SIZE;
        while (start < endOfObjRefs)
        {
            while (start < endOfObjRefs && !*start)
                start++;

            if (start >= endOfObjRefs)
                break;

            bool* end = start;
            while (end < endOfObjRefs && *end)
                end++;

            printf("    [%3u..%3u) obj refs\n", (start - objRefs) * TARGET_POINTER_SIZE,
                   (end - objRefs) * TARGET_POINTER_SIZE);
            start = end;
        }
    }
#endif

    // Then request the new type from the VM.

    layout->ClassHnd =
        m_compiler->info.compCompHnd->getContinuationType(layout->Size, objRefs, layout->Size / TARGET_POINTER_SIZE);

#ifdef DEBUG
    char buffer[256];
    JITDUMP("  Result = %s\n", m_compiler->eeGetClassName(layout->ClassHnd, buffer, ArrLen(buffer)));
#endif

    return layout;
}

//------------------------------------------------------------------------
// AsyncTransformation::CanonicalizeCallDefinition:
//   Put the call definition in a canonical form and update analyses for it.
//   This ensures that either the value is defined by a LCL_ADDR retbuffer or
//   by a STORE_LCL_VAR/STORE_LCL_FLD that follows the call node.
//
// Parameters:
//   block    - The block containing the async call
//   call     - The async call
//   analyses - Analysis information that is updated from the async call/a potential new store
//
// Returns:
//   Information about the definition after canonicalization.
//
CallDefinitionInfo AsyncTransformation::CanonicalizeCallDefinition(BasicBlock*    block,
                                                                   GenTreeCall*   call,
                                                                   AsyncAnalysis* analyses)
{
    CallDefinitionInfo callDefInfo;

    callDefInfo.InsertAfter = call;

    CallArg* retbufArg = call->gtArgs.GetRetBufferArg();

    if (analyses != nullptr)
    {
        analyses->Update(call);
    }

    if (!call->TypeIs(TYP_VOID) && !call->IsUnusedValue())
    {
        assert(retbufArg == nullptr);
        assert(call->gtNext != nullptr);
        // Canonicalize the store. In the common case where we are already
        // storing to a local we can usually reuse it, except if we may need to
        // preserve its value because of an exception being thrown after
        // potential resumption. (This check is conservative, we could use liveness for it as well.)
        if (!call->gtNext->OperIsLocalStore() || (call->gtNext->Data() != call) ||
            HasNonContextRestoreExceptionalFlow(block))
        {
            LIR::Use use;
            bool     gotUse = LIR::AsRange(block).TryGetUse(call, &use);
            assert(gotUse);

            unsigned newLclNum = use.ReplaceWithLclVar(m_compiler);

            // In some cases we may have been assigning a multireg promoted local from the call.
            // That's not supported with a LCL_VAR source. We need to decompose.
            if (call->IsMultiRegCall() && use.User()->OperIs(GT_STORE_LCL_VAR))
            {
                LclVarDsc* dsc = m_compiler->lvaGetDesc(use.User()->AsLclVar());
                if (m_compiler->lvaGetPromotionType(dsc) == Compiler::PROMOTION_TYPE_INDEPENDENT)
                {
                    m_compiler->lvaSetVarDoNotEnregister(newLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                    JITDUMP("  Call is multi-reg stored to an independently promoted local; decomposing store\n");
                    for (unsigned i = 0; i < dsc->lvFieldCnt; i++)
                    {
                        unsigned   fieldLclNum = dsc->lvFieldLclStart + i;
                        LclVarDsc* fieldDsc    = m_compiler->lvaGetDesc(fieldLclNum);

                        GenTree* value =
                            m_compiler->gtNewLclFldNode(newLclNum, fieldDsc->TypeGet(), fieldDsc->lvFldOffset);
                        GenTree* store = m_compiler->gtNewStoreLclVarNode(fieldLclNum, value);
                        LIR::AsRange(block).InsertBefore(use.User(), value, store);
                        DISPTREERANGE(LIR::AsRange(block), store);
                    }

                    // Remove the local and store that were created by ReplaceWithLclVar above
                    assert(use.Def()->OperIs(GT_LCL_VAR));
                    LIR::AsRange(block).Remove(use.Def());
                    LIR::AsRange(block).Remove(use.User());
                }
            }
        }
        else
        {
            if (analyses != nullptr)
            {
                // We will split after the store, but we still have to update analyses for it.
                analyses->Update(call->gtNext);
            }
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
// AsyncTransformation::CreateSuspensionBlock:
//   Create an empty basic block that will hold the suspension IR for the
//   specified async call.
//
// Parameters:
//   block    - The block containing the async call.
//   stateNum - State number assigned to this suspension point.
//
// Returns:
//   The new basic block.
//
BasicBlock* AsyncTransformation::CreateSuspensionBlock(BasicBlock* block, unsigned stateNum)
{
    if (m_lastSuspensionBB == nullptr)
    {
        m_lastSuspensionBB = m_compiler->fgLastBBInMainFunction();
    }

    BasicBlock* suspendBB = m_compiler->fgNewBBafter(BBJ_RETURN, m_lastSuspensionBB, false);
    suspendBB->clearTryIndex();
    suspendBB->clearHndIndex();
    suspendBB->inheritWeightPercentage(block, 0);
    m_lastSuspensionBB = suspendBB;

    if (m_sharedReturnBB != nullptr)
    {
        suspendBB->SetKindAndTargetEdge(BBJ_ALWAYS, m_compiler->fgAddRefPred(m_sharedReturnBB, suspendBB));
    }

    JITDUMP("  Creating suspension " FMT_BB " for state %u\n", suspendBB->bbNum, stateNum);

    return suspendBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateSuspension:
//   Populate the suspension block with IR that allocates a continuation
//   object, fills in its state, flags, resume info, data, and restores
//   contexts.
//
// Parameters:
//   callBlock - The block containing the async call.
//   call      - The async call.
//   suspendBB - The suspension basic block to add IR to.
//   stateNum  - State number assigned to this suspension point.
//   layout    - Layout information for the continuation object.
//   subLayout - Per-call layout builder indicating which fields are needed.
//   resumeReachable - Whether or not this suspension point is reachable from a previous resumption
//   mutatedSinceResumption - If this suspension point is reachable from a previous resumption
//                            then this indicates the set of tracked variables that may have been mutated since then.
//
void AsyncTransformation::CreateSuspension(BasicBlock*                      callBlock,
                                           GenTreeCall*                     call,
                                           BasicBlock*                      suspendBB,
                                           unsigned                         stateNum,
                                           const ContinuationLayout&        layout,
                                           const ContinuationLayoutBuilder& subLayout,
                                           bool                             resumeReachable,
                                           VARSET_VALARG_TP                 mutatedSinceResumption)
{
    GenTreeILOffset* ilOffsetNode =
        m_compiler->gtNewILOffsetNode(call->GetAsyncInfo().CallAsyncDebugInfo DEBUGARG(BAD_IL_OFFSET));

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, ilOffsetNode));

    GenTree* recordOffset =
        new (m_compiler, GT_RECORD_ASYNC_RESUME) GenTreeVal(GT_RECORD_ASYNC_RESUME, TYP_VOID, stateNum);
    LIR::AsRange(suspendBB).InsertAtEnd(recordOffset);

    // Allocate continuation
    GenTree* returnedContinuation = m_compiler->gtNewLclvNode(GetReturnedContinuationVar(), TYP_REF);

    GenTreeCall* allocContinuation =
        CreateAllocContinuationCall(subLayout.NeedsKeepAlive(), returnedContinuation, layout);

    m_compiler->compCurBB = suspendBB;
    m_compiler->fgMorphTree(allocContinuation);

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, allocContinuation));

    unsigned newContinuationVar   = GetNewContinuationVar();
    GenTree* storeNewContinuation = m_compiler->gtNewStoreLclVarNode(newContinuationVar, allocContinuation);
    LIR::AsRange(suspendBB).InsertAtEnd(storeNewContinuation);

    SaveSet tailSaveSet = SaveSet::All;
    if (ReuseContinuations() && resumeReachable)
    {
        // Split suspendBB into suspendBB -> [reuse continuation with Next store] -> [allocNewBlock with allocation
        // call] -> suspendBBTail [empty]
        BasicBlock* allocNewBB          = m_compiler->fgSplitBlockAfterNode(suspendBB, recordOffset);
        BasicBlock* reuseContinuationBB = m_compiler->fgSplitBlockAtEnd(suspendBB);
        BasicBlock* suspendTailBB       = m_compiler->fgSplitBlockAtEnd(allocNewBB);

        m_compiler->fgRemoveRefPred(reuseContinuationBB->GetTargetEdge());
        FlowEdge* toSuspendTail = m_compiler->fgAddRefPred(suspendTailBB, reuseContinuationBB);
        reuseContinuationBB->SetTargetEdge(toSuspendTail);

        FlowEdge* toAllocNew = m_compiler->fgAddRefPred(allocNewBB, suspendBB);
        suspendBB->SetCond(suspendBB->GetTargetEdge(), toAllocNew);
        suspendBB->GetTrueEdge()->setLikelihood(1.0);
        suspendBB->GetFalseEdge()->setLikelihood(0.0);

        JITDUMP("Continuation reuse is active. Split suspendBB into suspendBB " FMT_BB
                " -> reuseContinuationBlock " FMT_BB " -> allocNewBlock " FMT_BB " -> suspendBBTail " FMT_BB "\n",
                suspendBB->bbNum, reuseContinuationBB->bbNum, allocNewBB->bbNum, suspendTailBB->bbNum);

        // Store newContinuationVar = reusableContinuation in suspendBB
        GenTree* reusableContinuation   = m_compiler->gtNewLclvNode(m_reuseContinuationVar, TYP_REF);
        GenTree* storeContinuationParam = m_compiler->gtNewStoreLclVarNode(newContinuationVar, reusableContinuation);
        LIR::AsRange(suspendBB).InsertAtEnd(reusableContinuation, storeContinuationParam);

        // Check if newContinuationVar != null, jump to reuseContinuationBlock
        GenTree* reusedContinuation = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
        GenTree* null               = m_compiler->gtNewNull();
        GenTree* neNull             = m_compiler->gtNewOperNode(GT_NE, TYP_INT, reusedContinuation, null);
        GenTree* jtrue              = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
        LIR::AsRange(suspendBB).InsertAtEnd(reusedContinuation, null, neNull, jtrue);

        // Fill in 'Next' in reuseContinuationBB
        GenTree* newContinuation = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
        unsigned nextOffset      = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationNextFldHnd);
        returnedContinuation     = m_compiler->gtNewLclvNode(GetReturnedContinuationVar(), TYP_REF);
        GenTree* storeNext       = StoreAtOffset(returnedContinuation, nextOffset, newContinuation, TYP_REF);
        LIR::AsRange(reuseContinuationBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeNext));

        // In the path where we allocated a new continuation we save only locals that we know to be unmutated since the
        // last resumption.
        FillInDataOnSuspension(call, layout, subLayout, allocNewBB, mutatedSinceResumption, SaveSet::UnmutatedLocals);

        // We can skip saving unmutated locals in the shared path -- we only need to save locals that may have been
        // mutated since the last resumption.
        tailSaveSet = SaveSet::MutatedLocals;

        suspendBB = suspendTailBB;
    }

    // Fill in 'ResumeInfo'
    GenTree* newContinuation  = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
    unsigned resumeInfoOffset = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationResumeInfoFldHnd);
    GenTree* resumeInfoAddr =
        new (m_compiler, GT_ASYNC_RESUME_INFO) GenTreeVal(GT_ASYNC_RESUME_INFO, TYP_I_IMPL, (ssize_t)stateNum);
    GenTree* storeResume = StoreAtOffset(newContinuation, resumeInfoOffset, resumeInfoAddr, TYP_I_IMPL);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeResume));

    // Fill in 'state'
    newContinuation       = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
    unsigned stateOffset  = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
    GenTree* stateNumNode = m_compiler->gtNewIconNode((ssize_t)stateNum, TYP_INT);
    GenTree* storeState   = StoreAtOffset(newContinuation, stateOffset, stateNumNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeState));

    // Fill in 'flags'
    const AsyncCallInfo& callInfo          = call->GetAsyncInfo();
    unsigned             continuationFlags = 0;
    auto                 encodeIndex = [&continuationFlags](unsigned offset, unsigned firstBit, unsigned numBits) {
        assert(numBits < 32);
        assert((offset % TARGET_POINTER_SIZE) == 0);
        unsigned index = 1 + offset / TARGET_POINTER_SIZE;
        unsigned mask  = (1u << numBits) - 1;

        if ((index & mask) != index)
        {
            IMPL_LIMITATION("Cannot encode continuation offset in flags");
        }

        continuationFlags |= index << firstBit;
    };

    if (subLayout.NeedsException())
        encodeIndex(layout.ExceptionOffset, CORINFO_CONTINUATION_EXCEPTION_INDEX_FIRST_BIT,
                    CORINFO_CONTINUATION_EXCEPTION_INDEX_NUM_BITS);
    if (subLayout.NeedsContinuationContext())
        encodeIndex(layout.ContinuationContextOffset, CORINFO_CONTINUATION_CONTEXT_INDEX_FIRST_BIT,
                    CORINFO_CONTINUATION_CONTEXT_INDEX_NUM_BITS);
    if (call->gtReturnType != TYP_VOID)
    {
        const ReturnInfo* returnInfo = layout.FindReturn(m_compiler, call);
        assert(returnInfo != nullptr);
        encodeIndex(returnInfo->Offset, CORINFO_CONTINUATION_RESULT_INDEX_FIRST_BIT,
                    CORINFO_CONTINUATION_RESULT_INDEX_NUM_BITS);
    }
    if (callInfo.ContinuationContextHandling == ContinuationContextHandling::ContinueOnThreadPool)
        continuationFlags |= CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL;

    newContinuation      = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
    unsigned flagsOffset = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationFlagsFldHnd);
    GenTree* flagsNode   = m_compiler->gtNewIconNode((ssize_t)continuationFlags, TYP_INT);
    GenTree* storeFlags  = StoreAtOffset(newContinuation, flagsOffset, flagsNode, TYP_INT);
    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeFlags));

    FillInDataOnSuspension(call, layout, subLayout, suspendBB, mutatedSinceResumption, tailSaveSet);

    FinishContextHandlingOnSuspension(callBlock, call, suspendBB, layout, subLayout);

    if (suspendBB->KindIs(BBJ_RETURN))
    {
        newContinuation = m_compiler->gtNewLclvNode(newContinuationVar, TYP_REF);
        GenTree* ret    = m_compiler->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, newContinuation);
        LIR::AsRange(suspendBB).InsertAtEnd(newContinuation, ret);
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateAllocContinuationCall:
//   Create a call to the JIT helper that allocates a continuation.
//
// Parameters:
//   hasKeepAlive     - Whether the continuation needs a KeepAlive field.
//   prevContinuation - IR node that has the value of the previous continuation object.
//   layout           - Layout information for the continuation.
//
// Returns:
//   IR node representing the allocation.
//
GenTreeCall* AsyncTransformation::CreateAllocContinuationCall(bool                      hasKeepAlive,
                                                              GenTree*                  prevContinuation,
                                                              const ContinuationLayout& layout)
{
    GenTree* contClassHndNode = m_compiler->gtNewIconEmbClsHndNode(layout.ClassHnd);

    // If we need to keep the loader alive, use a different helper.
    if (hasKeepAlive)
    {
        assert(layout.KeepAliveOffset != UINT_MAX);
        GenTree* handleArg = m_compiler->gtNewLclvNode(m_compiler->info.compTypeCtxtArg, TYP_I_IMPL);
        // Offset passed to function is relative to instance data.
        int keepAliveOffset = (OFFSETOF__CORINFO_Continuation__data - SIZEOF__CORINFO_Object) + layout.KeepAliveOffset;
        GenTree*        keepAliveOffsetNode = m_compiler->gtNewIconNode(keepAliveOffset);
        CorInfoHelpFunc helperNum =
            (m_compiler->info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_METHODTABLE) != 0
                ? CORINFO_HELP_ALLOC_CONTINUATION_CLASS
                : CORINFO_HELP_ALLOC_CONTINUATION_METHOD;
        return m_compiler->gtNewHelperCallNode(helperNum, TYP_REF, prevContinuation, contClassHndNode,
                                               keepAliveOffsetNode, handleArg);
    }

    return m_compiler->gtNewHelperCallNode(CORINFO_HELP_ALLOC_CONTINUATION, TYP_REF, prevContinuation,
                                           contClassHndNode);
}

//------------------------------------------------------------------------
// AsyncTransformation::FillInDataOnSuspension:
//   Create IR that fills the data array of the continuation object with
//   live local values, OSR address, continuation context, and execution
//   context.
//
// Parameters:
//   call      - The async call.
//   layout    - Information about the continuation layout.
//   subLayout - Per-call layout builder indicating which fields are needed.
//   suspendBB - Basic block to add IR to.
//
void AsyncTransformation::FillInDataOnSuspension(GenTreeCall*                     call,
                                                 const ContinuationLayout&        layout,
                                                 const ContinuationLayoutBuilder& subLayout,
                                                 BasicBlock*                      suspendBB,
                                                 VARSET_VALARG_TP                 mutatedSinceResumption,
                                                 SaveSet                          saveSet)
{
    if ((saveSet != SaveSet::MutatedLocals) && (m_compiler->doesMethodHavePatchpoints() || m_compiler->opts.IsOSR()))
    {
        GenTree* osrAddressToStore;
        if (m_compiler->doesMethodHavePatchpoints())
        {
            osrAddressToStore = m_compiler->gtNewIconNode(0, TYP_I_IMPL);
        }
        else
        {
            osrAddressToStore = new (m_compiler, GT_FTN_ENTRY) GenTree(GT_FTN_ENTRY, TYP_I_IMPL);
        }

        // OSR address needs to be at offset 0 because OSR and tier0 methods
        // need to agree on that.
        assert(layout.OSRAddressOffset == 0);
        GenTree* newContinuation       = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned offset                = OFFSETOF__CORINFO_Continuation__data;
        GenTree* storeOSRAddressOffset = StoreAtOffset(newContinuation, offset, osrAddressToStore, TYP_I_IMPL);
        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeOSRAddressOffset));
    }

    // Fill in data
    for (const LiveLocalInfo& inf : layout.Locals)
    {
        if (!subLayout.ContainsLocal(inf.LclNum))
        {
            continue;
        }

        LclVarDsc* dsc = m_compiler->lvaGetDesc(inf.LclNum);

        if ((saveSet != SaveSet::All) && (GetLocalSaveSet(dsc, mutatedSinceResumption) != saveSet))
        {
            continue;
        }

        GenTree* newContinuation = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned offset          = OFFSETOF__CORINFO_Continuation__data + inf.Offset;

        GenTree* value;
        if (dsc->IsImplicitByRef())
        {
            GenTree* baseAddr = m_compiler->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
            value             = m_compiler->gtNewLoadValueNode(dsc->GetLayout(), baseAddr, GTF_IND_NONFAULTING);
        }
        else
        {
            value = m_compiler->gtNewLclVarNode(inf.LclNum);
        }

        GenTreeFlags indirFlags =
            GTF_IND_NONFAULTING | (inf.HeapAlignment() < inf.Alignment ? GTF_IND_UNALIGNED : GTF_EMPTY);

        GenTree* store;
        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            GenTree* cns  = m_compiler->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
            GenTree* addr = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation, cns);
            store         = m_compiler->gtNewStoreValueNode(dsc->GetLayout(), addr, value, indirFlags);
        }
        else
        {
            store = StoreAtOffset(newContinuation, offset, value, dsc->TypeGet(), indirFlags);
        }

        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, store));

        // Saving/storing of longs here may be the first place we introduce
        // long IR. We need to potentially decompose this on x86, so indicate
        // that to the backend.
        m_compiler->compLongUsed |= dsc->TypeIs(TYP_LONG);
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::GetLocalSaveSet:
//   Get the save set that a local should be saved as part of.
//
// Parameters:
//   dsc - The local
//   mutatedSinceResumption - Set of locals that may have been mutated since a resumption
//
// Returns:
//   The set to save the local as part of.
//
SaveSet AsyncTransformation::GetLocalSaveSet(const LclVarDsc* dsc, VARSET_VALARG_TP mutatedSinceResumption)
{
    if (dsc->lvPromoted)
    {
        for (unsigned i = 0; i < dsc->lvFieldCnt; i++)
        {
            LclVarDsc* fieldDsc = m_compiler->lvaGetDesc(dsc->lvFieldLclStart + i);
            if (!fieldDsc->lvTracked || VarSetOps::IsMember(m_compiler, mutatedSinceResumption, fieldDsc->lvVarIndex))
            {
                return SaveSet::MutatedLocals;
            }
        }

        return SaveSet::UnmutatedLocals;
    }

    // We should only see struct fields for independently promoted structs
    assert(!dsc->lvIsStructField ||
           (m_compiler->lvaGetPromotionType(dsc->lvParentLcl) == Compiler::PROMOTION_TYPE_INDEPENDENT));

    if (!dsc->lvTracked || VarSetOps::IsMember(m_compiler, mutatedSinceResumption, dsc->lvVarIndex))
    {
        return SaveSet::MutatedLocals;
    }

    return SaveSet::UnmutatedLocals;
}

//------------------------------------------------------------------------
// AsyncTransformation::FinishContextHandlingOnSuspension:
//   Generate code to finish handling of contexts on suspension:
//   - Capture SynchronizationContext or TaskScheduler into the continuation
//     if needed when later resuming
//   - Capture ExecutionContext into the continuation
//   - Restore current Thread._synchronizationContext and
//     Thread._executionContext from the state before the async call
//
// Parameters:
//   callBlock - The block containing the async call
//   call      - The async call
//   suspendBB - Basic block to add IR to.
//   layout    - Information about the continuation layout.
//   subLayout - Per-call layout builder indicating which fields are needed.
//
void AsyncTransformation::FinishContextHandlingOnSuspension(BasicBlock*                      callBlock,
                                                            GenTreeCall*                     call,
                                                            BasicBlock*                      suspendBB,
                                                            const ContinuationLayout&        layout,
                                                            const ContinuationLayoutBuilder& subLayout)
{
    CallArg* execContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncExecutionContext);
    CallArg* syncContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncSynchronizationContext);
    assert((execContextArg != nullptr) == (syncContextArg != nullptr));

    // In most cases we can use a helper. It is not the case when the call has
    // no contexts to restore, which is the case for task-returning thunks or
    // more specifically when the EE told us !CORINFO_ASYNC_SAVE_CONTEXTS.
    if (execContextArg != nullptr && subLayout.NeedsExecutionContext())
    {
        JITDUMP("    Call [%06u] has async context and captured execution context; using finish-suspension helper\n",
                Compiler::dspTreeID(call));
        FinishContextHandlingOnSuspensionWithHelper(callBlock, call, suspendBB, layout, subLayout);
        return;
    }

    if (subLayout.NeedsContinuationContext())
    {
        // Insert call
        //   AsyncHelpers.CaptureContinuationContext(
        //     ref newContinuation.ContinuationContext,
        //     ref newContinuation.Flags).
        GenTree*     contContextElementPlaceholder = m_compiler->gtNewZeroConNode(TYP_BYREF);
        GenTree*     flagsPlaceholder              = m_compiler->gtNewZeroConNode(TYP_BYREF);
        GenTreeCall* captureCall =
            m_compiler->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->captureContinuationContextMethHnd, TYP_VOID);
        SetCallEntrypointForR2R(captureCall, m_compiler, m_asyncInfo->captureContinuationContextMethHnd);

        captureCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(flagsPlaceholder));
        captureCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(contContextElementPlaceholder));

        m_compiler->compCurBB = suspendBB;
        m_compiler->fgMorphTree(captureCall);

        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, captureCall));

        // Replace contContextElementPlaceholder with actual address of the continuation context element
        LIR::Use use;
        bool     gotUse = LIR::AsRange(suspendBB).TryGetUse(contContextElementPlaceholder, &use);
        assert(gotUse);

        GenTree* newContinuation   = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned contContextOffset = OFFSETOF__CORINFO_Continuation__data + layout.ContinuationContextOffset;
        GenTree* contContextElementOffset =
            m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                      m_compiler->gtNewIconNode((ssize_t)contContextOffset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(contContextElementPlaceholder,
                                             LIR::SeqTree(m_compiler, contContextElementOffset));
        use.ReplaceWith(contContextElementOffset);
        LIR::AsRange(suspendBB).Remove(contContextElementPlaceholder);

        // Replace flagsPlaceholder with actual address of the flags
        gotUse = LIR::AsRange(suspendBB).TryGetUse(flagsPlaceholder, &use);
        assert(gotUse);

        newContinuation      = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned flagsOffset = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationFlagsFldHnd);
        GenTree* flagsOffsetNode =
            m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                      m_compiler->gtNewIconNode((ssize_t)flagsOffset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(flagsPlaceholder, LIR::SeqTree(m_compiler, flagsOffsetNode));
        use.ReplaceWith(flagsOffsetNode);
        LIR::AsRange(suspendBB).Remove(flagsPlaceholder);
    }

    if (subLayout.NeedsExecutionContext())
    {
        GenTreeCall* captureExecContext =
            m_compiler->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->captureExecutionContextMethHnd, TYP_REF);
        SetCallEntrypointForR2R(captureExecContext, m_compiler, m_asyncInfo->captureExecutionContextMethHnd);

        m_compiler->compCurBB = suspendBB;
        m_compiler->fgMorphTree(captureExecContext);

        GenTree* newContinuation = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned offset          = OFFSETOF__CORINFO_Continuation__data + layout.ExecutionContextOffset;
        GenTree* store           = StoreAtOffset(newContinuation, offset, captureExecContext, TYP_REF);
        LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, store));
    }

    RestoreContexts(callBlock, call, suspendBB);
}

//------------------------------------------------------------------------
// AsyncTransformation::FinishContextHandlingOnSuspensionWithHelper:
//   Generate code to finish handling of contexts on suspension by calling into a helper.
//
// Parameters:
//   callBlock - The block containing the async call
//   call      - The async call
//   suspendBB - Basic block to add IR to.
//   layout    - Information about the continuation layout.
//   subLayout - Per-call layout builder indicating which fields are needed.
//
// Remarks:
//   This is the common case where we need capture of execution context +
//   context restores. We do that with a single helper call that does
//   everything, for both size and to avoid multiple loads of the Thread TLS.
//
void AsyncTransformation::FinishContextHandlingOnSuspensionWithHelper(BasicBlock*                      callBlock,
                                                                      GenTreeCall*                     call,
                                                                      BasicBlock*                      suspendBB,
                                                                      const ContinuationLayout&        layout,
                                                                      const ContinuationLayoutBuilder& subLayout)
{
    CORINFO_METHOD_HANDLE helper = subLayout.NeedsContinuationContext()
                                       ? m_asyncInfo->finishSuspensionWithContinuationContextMethHnd
                                       : m_asyncInfo->finishSuspensionNoContinuationContextMethHnd;

    // Insert call
    //   finishSuspension[With|No]ContinuationContext(
    //     ref newContinuation.ContinuationContext, // optional
    //     ref newContinuation.Flags,               // optional
    //     ref newContinuation.ExecutionContext,
    //     resumed,
    //     execContext,
    //     syncContext)
    //

    CallArg* execContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncExecutionContext);
    CallArg* syncContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncSynchronizationContext);
    assert((execContextArg != nullptr) && (syncContextArg != nullptr));

    GenTree* contContextAddrPlaceholder = nullptr;
    GenTree* flagsPlaceholder           = nullptr;
    GenTree* execContextAddrPlaceholder = m_compiler->gtNewZeroConNode(TYP_BYREF);
    GenTree* resumedPlaceholder         = m_compiler->gtNewIconNode(0);
    GenTree* execContextPlaceholder     = m_compiler->gtNewNull();
    GenTree* syncContextPlaceholder     = m_compiler->gtNewNull();

    GenTreeCall* finishCall = m_compiler->gtNewCallNode(CT_USER_FUNC, helper, TYP_VOID);
    SetCallEntrypointForR2R(finishCall, m_compiler, helper);

    finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(syncContextPlaceholder));
    finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(execContextPlaceholder));
    finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(resumedPlaceholder));
    finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(execContextAddrPlaceholder));

    if (subLayout.NeedsContinuationContext())
    {
        contContextAddrPlaceholder = m_compiler->gtNewZeroConNode(TYP_BYREF);
        flagsPlaceholder           = m_compiler->gtNewZeroConNode(TYP_BYREF);
        finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(flagsPlaceholder));
        finishCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(contContextAddrPlaceholder));
    }

    m_compiler->compCurBB = suspendBB;
    m_compiler->fgMorphTree(finishCall);

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, finishCall));

    if (subLayout.NeedsContinuationContext())
    {
        // Replace contContextAddrPlaceholder with actual address of the continuation context
        LIR::Use use;
        bool     gotUse = LIR::AsRange(suspendBB).TryGetUse(contContextAddrPlaceholder, &use);
        assert(gotUse);

        GenTree* newContinuation   = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned contContextOffset = OFFSETOF__CORINFO_Continuation__data + layout.ContinuationContextOffset;
        GenTree* contContextAddrOffset =
            m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                      m_compiler->gtNewIconNode((ssize_t)contContextOffset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(contContextAddrPlaceholder,
                                             LIR::SeqTree(m_compiler, contContextAddrOffset));
        use.ReplaceWith(contContextAddrOffset);
        LIR::AsRange(suspendBB).Remove(contContextAddrPlaceholder);

        // Replace flagsPlaceholder with actual address of the flags
        gotUse = LIR::AsRange(suspendBB).TryGetUse(flagsPlaceholder, &use);
        assert(gotUse);

        newContinuation      = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
        unsigned flagsOffset = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationFlagsFldHnd);
        GenTree* flagsOffsetNode =
            m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                      m_compiler->gtNewIconNode((ssize_t)flagsOffset, TYP_I_IMPL));

        LIR::AsRange(suspendBB).InsertBefore(flagsPlaceholder, LIR::SeqTree(m_compiler, flagsOffsetNode));
        use.ReplaceWith(flagsOffsetNode);
        LIR::AsRange(suspendBB).Remove(flagsPlaceholder);
    }

    // Replace execContextAddrPlaceholder with actual address of the execution context
    LIR::Use use;
    bool     gotUse = LIR::AsRange(suspendBB).TryGetUse(execContextAddrPlaceholder, &use);
    assert(gotUse);

    GenTree* newContinuation   = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
    unsigned execContextOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExecutionContextOffset;
    GenTree* execContextAddrOffset =
        m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, newContinuation,
                                  m_compiler->gtNewIconNode((ssize_t)execContextOffset, TYP_I_IMPL));

    LIR::AsRange(suspendBB).InsertBefore(execContextAddrPlaceholder, LIR::SeqTree(m_compiler, execContextAddrOffset));
    use.ReplaceWith(execContextAddrOffset);
    LIR::AsRange(suspendBB).Remove(execContextAddrPlaceholder);

    // Replace resumedPlaceholder with actual "continuationParameter != null" arg
    gotUse = LIR::AsRange(suspendBB).TryGetUse(resumedPlaceholder, &use);
    assert(gotUse);

    GenTree* continuation = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
    GenTree* null         = m_compiler->gtNewNull();
    GenTree* resumed      = m_compiler->gtNewOperNode(GT_NE, TYP_INT, continuation, null);

    LIR::AsRange(suspendBB).InsertBefore(resumedPlaceholder, LIR::SeqTree(m_compiler, resumed));
    use.ReplaceWith(resumed);
    LIR::AsRange(suspendBB).Remove(resumedPlaceholder);

    // Replace execContextPlaceholder with actual value
    GenTree* execContext = execContextArg->GetNode();
    if (!execContext->OperIs(GT_LCL_VAR))
    {
        // We are moving execContext into a different BB so create a temp for it.
        LIR::Use use(LIR::AsRange(callBlock), &execContextArg->NodeRef(), call);
        use.ReplaceWithLclVar(m_compiler);
        execContext = use.Def();
    }

    gotUse = LIR::AsRange(suspendBB).TryGetUse(execContextPlaceholder, &use);
    assert(gotUse);

    LIR::AsRange(callBlock).Remove(execContext);
    LIR::AsRange(suspendBB).InsertBefore(execContextPlaceholder, execContext);
    use.ReplaceWith(execContext);
    LIR::AsRange(suspendBB).Remove(execContextPlaceholder);

    call->gtArgs.RemoveUnsafe(execContextArg);

    // Replace syncContextPlaceholder with actual value
    GenTree* syncContext = syncContextArg->GetNode();
    if (!syncContext->OperIs(GT_LCL_VAR))
    {
        // We are moving syncContext into a different BB so create a temp for it.
        LIR::Use use(LIR::AsRange(callBlock), &syncContextArg->NodeRef(), call);
        use.ReplaceWithLclVar(m_compiler);
        syncContext = use.Def();
    }

    gotUse = LIR::AsRange(suspendBB).TryGetUse(syncContextPlaceholder, &use);
    assert(gotUse);

    LIR::AsRange(callBlock).Remove(syncContext);
    LIR::AsRange(suspendBB).InsertBefore(syncContextPlaceholder, syncContext);
    use.ReplaceWith(syncContext);
    LIR::AsRange(suspendBB).Remove(syncContextPlaceholder);

    call->gtArgs.RemoveUnsafe(syncContextArg);

    JITDUMP("    Created FinishSuspension call on suspension:\n");
    DISPTREERANGE(LIR::AsRange(suspendBB), finishCall);
}

//------------------------------------------------------------------------
// AsyncTransformation::RestoreContexts:
//   Create IR to restore contexts on suspension.
//
// Parameters:
//   block     - Block that contains the async call
//   call      - The async call
//   suspendBB - The basic block to add IR to.
//
void AsyncTransformation::RestoreContexts(BasicBlock* block, GenTreeCall* call, BasicBlock* suspendBB)
{
    CallArg* execContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncExecutionContext);
    CallArg* syncContextArg = call->gtArgs.FindWellKnownArg(WellKnownArg::AsyncSynchronizationContext);
    assert((execContextArg != nullptr) == (syncContextArg != nullptr));
    if (execContextArg == nullptr)
    {
        JITDUMP("    Call [%06u] does not have async contexts; skipping restore on suspension\n",
                Compiler::dspTreeID(call));
        return;
    }

    JITDUMP("    Call [%06u] has async contexts; will restore on suspension\n", Compiler::dspTreeID(call));

    // Insert call
    //   AsyncHelpers.RestoreContexts(resumed, execContext, syncContext);

    GenTree*     resumedPlaceholder     = m_compiler->gtNewIconNode(0);
    GenTree*     execContextPlaceholder = m_compiler->gtNewNull();
    GenTree*     syncContextPlaceholder = m_compiler->gtNewNull();
    GenTreeCall* restoreCall =
        m_compiler->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->restoreContextsOnSuspensionMethHnd, TYP_VOID);
    SetCallEntrypointForR2R(restoreCall, m_compiler, m_asyncInfo->restoreContextsOnSuspensionMethHnd);

    restoreCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(syncContextPlaceholder));
    restoreCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(execContextPlaceholder));
    restoreCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(resumedPlaceholder));

    m_compiler->compCurBB = suspendBB;
    m_compiler->fgMorphTree(restoreCall);

    LIR::AsRange(suspendBB).InsertAtEnd(LIR::SeqTree(m_compiler, restoreCall));

    // Replace resumedPlaceholder with actual "continuationParameter != null" arg
    LIR::Use use;
    bool     gotUse = LIR::AsRange(suspendBB).TryGetUse(resumedPlaceholder, &use);
    assert(gotUse);

    GenTree* continuation = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
    GenTree* null         = m_compiler->gtNewNull();
    GenTree* resumed      = m_compiler->gtNewOperNode(GT_NE, TYP_INT, continuation, null);

    LIR::AsRange(suspendBB).InsertBefore(resumedPlaceholder, LIR::SeqTree(m_compiler, resumed));
    use.ReplaceWith(resumed);
    LIR::AsRange(suspendBB).Remove(resumedPlaceholder);

    // Replace execContextPlaceholder with actual value
    GenTree* execContext = execContextArg->GetNode();
    if (!execContext->OperIs(GT_LCL_VAR))
    {
        // We are moving execContext into a different BB so create a temp for it.
        LIR::Use use(LIR::AsRange(block), &execContextArg->NodeRef(), call);
        use.ReplaceWithLclVar(m_compiler);
        execContext = use.Def();
    }

    gotUse = LIR::AsRange(suspendBB).TryGetUse(execContextPlaceholder, &use);
    assert(gotUse);

    LIR::AsRange(block).Remove(execContext);
    LIR::AsRange(suspendBB).InsertBefore(execContextPlaceholder, execContext);
    use.ReplaceWith(execContext);
    LIR::AsRange(suspendBB).Remove(execContextPlaceholder);

    call->gtArgs.RemoveUnsafe(execContextArg);

    // Replace syncContextPlaceholder with actual value
    GenTree* syncContext = syncContextArg->GetNode();
    if (!syncContext->OperIs(GT_LCL_VAR))
    {
        // We are moving syncContext into a different BB so create a temp for it.
        LIR::Use use(LIR::AsRange(block), &syncContextArg->NodeRef(), call);
        use.ReplaceWithLclVar(m_compiler);
        syncContext = use.Def();
    }

    gotUse = LIR::AsRange(suspendBB).TryGetUse(syncContextPlaceholder, &use);
    assert(gotUse);

    LIR::AsRange(block).Remove(syncContext);
    LIR::AsRange(suspendBB).InsertBefore(syncContextPlaceholder, syncContext);
    use.ReplaceWith(syncContext);
    LIR::AsRange(suspendBB).Remove(syncContextPlaceholder);

    call->gtArgs.RemoveUnsafe(syncContextArg);

    JITDUMP("    Created RestoreContexts call on suspension:\n");
    DISPTREERANGE(LIR::AsRange(suspendBB), restoreCall);
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
//   suspendBB   - Basic block to add IR to
//   remainder   - [out] The remainder block containing the IR that was after the async call.
//
void AsyncTransformation::CreateCheckAndSuspendAfterCall(BasicBlock*               block,
                                                         GenTreeCall*              call,
                                                         const CallDefinitionInfo& callDefInfo,
                                                         BasicBlock*               suspendBB,
                                                         BasicBlock**              remainder)
{
    GenTree* continuationArg = new (m_compiler, GT_ASYNC_CONTINUATION) GenTree(GT_ASYNC_CONTINUATION, TYP_REF);
    continuationArg->SetHasOrderingSideEffect();

    GenTree* storeContinuation = m_compiler->gtNewStoreLclVarNode(GetReturnedContinuationVar(), continuationArg);
    LIR::AsRange(block).InsertAfter(callDefInfo.InsertAfter, continuationArg, storeContinuation);

    GenTree* null                 = m_compiler->gtNewNull();
    GenTree* returnedContinuation = m_compiler->gtNewLclvNode(GetReturnedContinuationVar(), TYP_REF);
    GenTree* neNull               = m_compiler->gtNewOperNode(GT_NE, TYP_INT, returnedContinuation, null);
    GenTree* jtrue                = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);

    LIR::AsRange(block).InsertAfter(storeContinuation, null, returnedContinuation, neNull, jtrue);
    *remainder = m_compiler->fgSplitBlockAfterNode(block, jtrue);
    JITDUMP("  Remainder is " FMT_BB "\n", (*remainder)->bbNum);

    // For non-inlined calls adjust offset for the split. We have the exact
    // offset of the await call, so we can do better than
    // fgSplitBlockAfterNode. The previous block contains the call so add 1 to
    // include its start offset (the IL offsets are only used for range checks
    // in the backend, so having the offset be inside an IL instruction is ok.)
    DebugInfo di = call->GetAsyncInfo().CallAsyncDebugInfo.GetRoot();
    DebugInfo par;
    if (!di.GetParent(&par))
    {
        IL_OFFSET awaitOffset    = di.GetLocation().GetOffset();
        block->bbCodeOffsEnd     = awaitOffset + 1;
        (*remainder)->bbCodeOffs = awaitOffset + 1;
    }

    FlowEdge* retBBEdge = m_compiler->fgAddRefPred(suspendBB, block);
    block->SetCond(retBBEdge, block->GetTargetEdge());

    block->GetTrueEdge()->setLikelihood(0);
    block->GetFalseEdge()->setLikelihood(1);
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumptionBlock:
//   Create an empty basic block that will hold the resumption IR for the
//   specified async call.
//
// Parameters:
//   remainder     - The block that contains the IR after the async call.
//   stateNum      - State number assigned to this suspension point.
//
// Returns:
//   The new basic block.
//
BasicBlock* AsyncTransformation::CreateResumptionBlock(BasicBlock* remainder, unsigned stateNum)
{
    if (m_lastResumptionBB == nullptr)
    {
        m_lastResumptionBB = m_compiler->fgLastBBInMainFunction();
    }

    BasicBlock* resumeBB      = m_compiler->fgNewBBafter(BBJ_ALWAYS, m_lastResumptionBB, true);
    FlowEdge*   remainderEdge = m_compiler->fgAddRefPred(remainder, resumeBB);

    resumeBB->bbSetRunRarely();
    resumeBB->CopyFlags(remainder, BBF_PROF_WEIGHT);
    resumeBB->SetTargetEdge(remainderEdge);
    resumeBB->clearTryIndex();
    resumeBB->clearHndIndex();
    resumeBB->SetFlags(BBF_ASYNC_RESUMPTION);
    m_lastResumptionBB = resumeBB;

    JITDUMP("  Creating resumption " FMT_BB " for state %u\n", resumeBB->bbNum, stateNum);

    return resumeBB;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumption:
//   Populate the resumption block with IR that restores live state from
//   the continuation object, rethrows exceptions if necessary, and copies
//   the return value.
//
// Parameters:
//   callBlock   - The block containing the async call.
//   call        - The async call.
//   resumeBB    - The resumption basic block to add IR to.
//   callDefInfo - Information about the async call's definition.
//   layout      - Layout information for the continuation object.
//   subLayout   - Per-call layout builder indicating which fields are needed.
//
void AsyncTransformation::CreateResumption(BasicBlock*                      callBlock,
                                           GenTreeCall*                     call,
                                           BasicBlock*                      resumeBB,
                                           const CallDefinitionInfo&        callDefInfo,
                                           const ContinuationLayout&        layout,
                                           const ContinuationLayoutBuilder& subLayout)
{
    GenTreeILOffset* ilOffsetNode =
        m_compiler->gtNewILOffsetNode(call->GetAsyncInfo().CallAsyncDebugInfo DEBUGARG(BAD_IL_OFFSET));

    LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, ilOffsetNode));

    if (layout.Size > 0)
    {
        RestoreFromDataOnResumption(layout, subLayout, resumeBB);
    }

    BasicBlock* storeResultBB = resumeBB;

    if (subLayout.NeedsException())
    {
        storeResultBB = RethrowExceptionOnResumption(callBlock, layout, resumeBB);
    }

    if ((call->gtReturnType != TYP_VOID) && (callDefInfo.DefinitionNode != nullptr))
    {
        CopyReturnValueOnResumption(call, callDefInfo, layout, storeResultBB);
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::RestoreFromDataOnResumption:
//   Create IR that restores locals from the data array of the continuation
//   object, including execution context restoration.
//
// Parameters:
//   layout    - Information about the continuation layout.
//   subLayout - Per-call layout builder indicating which fields are needed.
//   resumeBB  - Basic block to append IR to.
//
void AsyncTransformation::RestoreFromDataOnResumption(const ContinuationLayout&        layout,
                                                      const ContinuationLayoutBuilder& subLayout,
                                                      BasicBlock*                      resumeBB)
{
    if (subLayout.NeedsExecutionContext())
    {
        GenTree*     valuePlaceholder = m_compiler->gtNewZeroConNode(TYP_REF);
        GenTreeCall* restoreCall =
            m_compiler->gtNewCallNode(CT_USER_FUNC, m_asyncInfo->restoreExecutionContextMethHnd, TYP_VOID);
        SetCallEntrypointForR2R(restoreCall, m_compiler, m_asyncInfo->restoreExecutionContextMethHnd);
        restoreCall->gtArgs.PushFront(m_compiler, NewCallArg::Primitive(valuePlaceholder));

        m_compiler->compCurBB = resumeBB;
        m_compiler->fgMorphTree(restoreCall);

        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, restoreCall));

        LIR::Use valueUse;
        bool     gotUse = LIR::AsRange(resumeBB).TryGetUse(valuePlaceholder, &valueUse);
        assert(gotUse);

        GenTree* continuation      = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned execContextOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExecutionContextOffset;
        GenTree* execContextValue  = LoadFromOffset(continuation, execContextOffset, TYP_REF);

        LIR::AsRange(resumeBB).InsertBefore(valuePlaceholder, LIR::SeqTree(m_compiler, execContextValue));
        valueUse.ReplaceWith(execContextValue);

        LIR::AsRange(resumeBB).Remove(valuePlaceholder);
    }

    // Copy data
    for (const LiveLocalInfo& inf : layout.Locals)
    {
        if (!subLayout.ContainsLocal(inf.LclNum))
        {
            continue;
        }

        LclVarDsc* dsc = m_compiler->lvaGetDesc(inf.LclNum);

        GenTree* continuation = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned offset       = OFFSETOF__CORINFO_Continuation__data + inf.Offset;
        GenTree* cns          = m_compiler->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
        GenTree* addr         = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, continuation, cns);

        GenTreeFlags indirFlags =
            GTF_IND_NONFAULTING | (inf.HeapAlignment() < inf.Alignment ? GTF_IND_UNALIGNED : GTF_EMPTY);
        GenTree* value;
        if (dsc->TypeIs(TYP_STRUCT) || dsc->IsImplicitByRef())
        {
            value = m_compiler->gtNewLoadValueNode(dsc->GetLayout(), addr, indirFlags);
        }
        else
        {
            value = m_compiler->gtNewIndir(dsc->TypeGet(), addr, indirFlags);
        }

        GenTree* store;
        if (dsc->IsImplicitByRef())
        {
            GenTree* baseAddr = m_compiler->gtNewLclvNode(inf.LclNum, dsc->TypeGet());
            store             = m_compiler->gtNewStoreValueNode(dsc->GetLayout(), baseAddr, value,
                                                                GTF_IND_NONFAULTING | GTF_IND_TGT_NOT_HEAP);
        }
        else
        {
            store = m_compiler->gtNewStoreLclVarNode(inf.LclNum, value);
        }

        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, store));
    }

    if (subLayout.NeedsKeepAlive())
    {
        // Ensure that the continuation remains alive until we finished loading the generic context
        GenTree* continuation = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        GenTree* keepAlive    = m_compiler->gtNewKeepAliveNode(continuation);
        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, keepAlive));
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

    BasicBlock* rethrowExceptionBB = m_compiler->fgNewBBafter(BBJ_THROW, block, /* extendRegion */ true);

    JITDUMP("  Created " FMT_BB " to rethrow exception on resumption\n", rethrowExceptionBB->bbNum);

    // We split 'block' at the call, so a BBF_INTERNAL block after it would
    // result in broken debug info if the block came from IL.
    if (!block->HasFlag(BBF_INTERNAL))
    {
        rethrowExceptionBB->RemoveFlags(BBF_INTERNAL);
        // Non-internal blocks must be marked imported
        rethrowExceptionBB->SetFlags(BBF_IMPORTED);
    }

    BasicBlock* storeResultBB = m_compiler->fgNewBBafter(BBJ_ALWAYS, resumeBB, true);
    JITDUMP("  Created " FMT_BB " to store result when resuming with no exception\n", storeResultBB->bbNum);

    FlowEdge* rethrowEdge     = m_compiler->fgAddRefPred(rethrowExceptionBB, resumeBB);
    FlowEdge* storeResultEdge = m_compiler->fgAddRefPred(storeResultBB, resumeBB);

    assert(resumeBB->KindIs(BBJ_ALWAYS));
    BasicBlock* remainder = resumeBB->GetTarget();
    m_compiler->fgRemoveRefPred(resumeBB->GetTargetEdge());

    resumeBB->SetCond(rethrowEdge, storeResultEdge);
    rethrowEdge->setLikelihood(0);
    storeResultEdge->setLikelihood(1);
    rethrowExceptionBB->inheritWeightPercentage(resumeBB, 0);
    storeResultBB->inheritWeightPercentage(resumeBB, 100);
    JITDUMP("  Resumption " FMT_BB " becomes BBJ_COND to check for non-null exception\n", resumeBB->bbNum);

    FlowEdge* remainderEdge = m_compiler->fgAddRefPred(remainder, storeResultBB);
    storeResultBB->SetTargetEdge(remainderEdge);

    m_lastResumptionBB = storeResultBB;

    // Check if we have an exception.
    unsigned exceptionLclNum = GetExceptionVar();
    GenTree* continuation    = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
    unsigned exceptionOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExceptionOffset;
    GenTree* exceptionInd    = LoadFromOffset(continuation, exceptionOffset, TYP_REF);
    GenTree* storeException  = m_compiler->gtNewStoreLclVarNode(exceptionLclNum, exceptionInd);
    LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeException));

    if (ReuseContinuations())
    {
        // If we may reuse this continuation later then make sure we don't see the same exception again.
        GenTree* continuation    = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned exceptionOffset = OFFSETOF__CORINFO_Continuation__data + layout.ExceptionOffset;
        GenTree* null            = m_compiler->gtNewNull();
        GenTree* nullException   = StoreAtOffset(continuation, exceptionOffset, null, TYP_REF);
        LIR::AsRange(resumeBB).InsertAtEnd(LIR::SeqTree(m_compiler, nullException));
    }

    GenTree* exception = m_compiler->gtNewLclVarNode(exceptionLclNum, TYP_REF);
    GenTree* null      = m_compiler->gtNewNull();
    GenTree* neNull    = m_compiler->gtNewOperNode(GT_NE, TYP_INT, exception, null);
    GenTree* jtrue     = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
    LIR::AsRange(resumeBB).InsertAtEnd(exception, null, neNull, jtrue);

    exception = m_compiler->gtNewLclVarNode(exceptionLclNum, TYP_REF);

    GenTreeCall* rethrowException = m_compiler->gtNewHelperCallNode(CORINFO_HELP_THROWEXACT, TYP_VOID, exception);

    m_compiler->compCurBB = rethrowExceptionBB;
    m_compiler->fgMorphTree(rethrowException);

    LIR::AsRange(rethrowExceptionBB).InsertAtEnd(LIR::SeqTree(m_compiler, rethrowException));

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
//   call          - The async call.
//   callDefInfo   - Information about the async call's definition.
//   layout        - Layout information for the continuation object.
//   storeResultBB - Basic block to append IR to.
//
void AsyncTransformation::CopyReturnValueOnResumption(GenTreeCall*              call,
                                                      const CallDefinitionInfo& callDefInfo,
                                                      const ContinuationLayout& layout,
                                                      BasicBlock*               storeResultBB)
{
    const ReturnInfo* retInfo = layout.FindReturn(m_compiler, call);
    assert(retInfo != nullptr);
    GenTree* resultBase   = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
    unsigned resultOffset = OFFSETOF__CORINFO_Continuation__data + retInfo->Offset;

    assert(callDefInfo.DefinitionNode != nullptr);
    LclVarDsc* resultLcl = m_compiler->lvaGetDesc(callDefInfo.DefinitionNode);

    GenTreeFlags indirFlags =
        GTF_IND_NONFAULTING | (retInfo->HeapAlignment() < retInfo->Alignment ? GTF_IND_UNALIGNED : GTF_EMPTY);

    // TODO-TP: We can use liveness to avoid generating a lot of this IR.
    if (call->gtReturnType == TYP_STRUCT)
    {
        if (m_compiler->lvaGetPromotionType(resultLcl) != Compiler::PROMOTION_TYPE_INDEPENDENT)
        {
            GenTree* resultOffsetNode = m_compiler->gtNewIconNode((ssize_t)resultOffset, TYP_I_IMPL);
            GenTree* resultAddr       = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, resultBase, resultOffsetNode);
            GenTree* resultData = m_compiler->gtNewLoadValueNode(retInfo->Type.ReturnLayout, resultAddr, indirFlags);
            GenTree* storeResult;
            if ((callDefInfo.DefinitionNode->GetLclOffs() == 0) &&
                ClassLayout::AreCompatible(resultLcl->GetLayout(), retInfo->Type.ReturnLayout))
            {
                storeResult = m_compiler->gtNewStoreLclVarNode(callDefInfo.DefinitionNode->GetLclNum(), resultData);
            }
            else
            {
                storeResult = m_compiler->gtNewStoreLclFldNode(callDefInfo.DefinitionNode->GetLclNum(), TYP_STRUCT,
                                                               retInfo->Type.ReturnLayout,
                                                               callDefInfo.DefinitionNode->GetLclOffs(), resultData);
            }

            LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeResult));
        }
        else
        {
            assert(!call->gtArgs.HasRetBuffer()); // Locals defined through retbufs are never independently promoted.

            if ((resultLcl->lvFieldCnt > 1) && !resultBase->OperIsLocal())
            {
                unsigned resultBaseVar   = GetResultBaseVar();
                GenTree* storeResultBase = m_compiler->gtNewStoreLclVarNode(resultBaseVar, resultBase);
                LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeResultBase));

                resultBase = m_compiler->gtNewLclVarNode(resultBaseVar, TYP_REF);

                // Can be reallocated by above call to GetResultBaseVar
                resultLcl = m_compiler->lvaGetDesc(callDefInfo.DefinitionNode);
            }

            assert(callDefInfo.DefinitionNode->OperIs(GT_STORE_LCL_VAR));
            for (unsigned i = 0; i < resultLcl->lvFieldCnt; i++)
            {
                unsigned   fieldLclNum = resultLcl->lvFieldLclStart + i;
                LclVarDsc* fieldDsc    = m_compiler->lvaGetDesc(fieldLclNum);

                unsigned fldOffset = resultOffset + fieldDsc->lvFldOffset;
                GenTree* value     = LoadFromOffset(resultBase, fldOffset, fieldDsc->TypeGet(), indirFlags);
                GenTree* store     = m_compiler->gtNewStoreLclVarNode(fieldLclNum, value);
                LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_compiler, store));

                if (i + 1 != resultLcl->lvFieldCnt)
                {
                    resultBase = m_compiler->gtCloneExpr(resultBase);
                }
            }
        }
    }
    else
    {
        GenTree* value = LoadFromOffset(resultBase, resultOffset, call->gtReturnType, indirFlags);

        GenTree* storeResult;
        if (callDefInfo.DefinitionNode->OperIs(GT_STORE_LCL_VAR))
        {
            storeResult = m_compiler->gtNewStoreLclVarNode(callDefInfo.DefinitionNode->GetLclNum(), value);
        }
        else
        {
            storeResult = m_compiler->gtNewStoreLclFldNode(callDefInfo.DefinitionNode->GetLclNum(),
                                                           callDefInfo.DefinitionNode->TypeGet(),
                                                           callDefInfo.DefinitionNode->GetLclOffs(), value);
        }

        LIR::AsRange(storeResultBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeResult));
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
    GenTree*      cns      = m_compiler->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types     addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*      addr     = m_compiler->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeIndir* load     = m_compiler->gtNewIndir(type, addr, indirFlags);
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
//   indirFlags - Indirection flags
//
// Returns:
//   IR node of the store.
//
GenTreeStoreInd* AsyncTransformation::StoreAtOffset(
    GenTree* base, unsigned offset, GenTree* value, var_types storeType, GenTreeFlags indirFlags)
{
    assert(base->TypeIs(TYP_REF, TYP_BYREF, TYP_I_IMPL));
    GenTree*         cns      = m_compiler->gtNewIconNode((ssize_t)offset, TYP_I_IMPL);
    var_types        addrType = base->TypeIs(TYP_I_IMPL) ? TYP_I_IMPL : TYP_BYREF;
    GenTree*         addr     = m_compiler->gtNewOperNode(GT_ADD, addrType, base, cns);
    GenTreeStoreInd* store    = m_compiler->gtNewStoreIndNode(storeType, addr, value, indirFlags);
    return store;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateDebugInfoForSuspensionPoint:
//   Create debug info for the specific suspension point we just created.
//
// Parameters:
//   layout    - Layout of continuation.
//   subLayout - Per-call layout builder indicating which locals are present.
//
void AsyncTransformation::CreateDebugInfoForSuspensionPoint(const ContinuationLayout&        layout,
                                                            const ContinuationLayoutBuilder& subLayout)
{
    uint32_t numLocals = 0;
    for (const LiveLocalInfo& inf : layout.Locals)
    {
        if (!subLayout.ContainsLocal(inf.LclNum))
        {
            continue;
        }

        unsigned ilVarNum = m_compiler->compMap2ILvarNum(inf.LclNum);
        if (ilVarNum == (unsigned)ICorDebugInfo::UNKNOWN_ILNUM)
        {
            continue;
        }

        ICorDebugInfo::AsyncContinuationVarInfo varInf;
        varInf.VarNumber = ilVarNum;
        varInf.Offset    = OFFSETOF__CORINFO_Continuation__data + inf.Offset;
        m_compiler->compAsyncVars->push_back(varInf);
        numLocals++;
    }

    ICorDebugInfo::AsyncSuspensionPoint suspensionPoint;
    suspensionPoint.DiagnosticNativeOffset = 0;
    suspensionPoint.NumContinuationVars    = numLocals;
    m_compiler->compSuspensionPoints->push_back(suspensionPoint);
}

//------------------------------------------------------------------------
// AsyncTransformation::GetReturnedContinuationVar:
//   Create a new local to hold the continuation returned by called async functions.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetReturnedContinuationVar()
{
    if (m_returnedContinuationVar == BAD_VAR_NUM)
    {
        m_returnedContinuationVar = m_compiler->lvaGrabTemp(false DEBUGARG("returned continuation"));
        m_compiler->lvaGetDesc(m_returnedContinuationVar)->lvType = TYP_REF;
    }

    return m_returnedContinuationVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::GetNewContinuationVar:
//   Create a new local to hold the continuation for this function that will be
//   returned.
//
// Returns:
//   Local number.
//
unsigned AsyncTransformation::GetNewContinuationVar()
{
    if (m_newContinuationVar == BAD_VAR_NUM)
    {
        m_newContinuationVar = m_compiler->lvaGrabTemp(false DEBUGARG("new continuation"));
        m_compiler->lvaGetDesc(m_newContinuationVar)->lvType = TYP_REF;
    }

    return m_newContinuationVar;
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
    if ((m_resultBaseVar == BAD_VAR_NUM) || !m_compiler->lvaHaveManyLocals())
    {
        m_resultBaseVar = m_compiler->lvaGrabTemp(false DEBUGARG("object for resuming result base"));
        m_compiler->lvaGetDesc(m_resultBaseVar)->lvType = TYP_REF;
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
    if ((m_exceptionVar == BAD_VAR_NUM) || !m_compiler->lvaHaveManyLocals())
    {
        m_exceptionVar = m_compiler->lvaGrabTemp(false DEBUGARG("object for resuming exception"));
        m_compiler->lvaGetDesc(m_exceptionVar)->lvType = TYP_REF;
    }

    return m_exceptionVar;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateSharedReturnBB:
//   Create the shared return BB.
//
void AsyncTransformation::CreateSharedReturnBB()
{
    m_sharedReturnBB = m_compiler->fgNewBBafter(BBJ_RETURN, m_compiler->fgLastBBInMainFunction(), false);
    m_sharedReturnBB->bbSetRunRarely();
    m_sharedReturnBB->clearTryIndex();
    m_sharedReturnBB->clearHndIndex();

    if (m_compiler->fgIsUsingProfileWeights())
    {
        // All suspension BBs are cold, so we do not need to propagate any
        // weights, but we do need to propagate the flag.
        m_sharedReturnBB->SetFlags(BBF_PROF_WEIGHT);
    }

    GenTree* continuation = m_compiler->gtNewLclvNode(GetNewContinuationVar(), TYP_REF);
    GenTree* ret          = m_compiler->gtNewOperNode(GT_RETURN_SUSPEND, TYP_VOID, continuation);
    LIR::AsRange(m_sharedReturnBB).InsertAtEnd(continuation, ret);

    JITDUMP("Created shared return BB " FMT_BB "\n", m_sharedReturnBB->bbNum);

    DISPRANGE(LIR::AsRange(m_sharedReturnBB));
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumptionsAndSuspensions:
//   Walk all recorded async states and create the suspension and resumption
//   IR, continuation layouts, and debug info for each one.
//
void AsyncTransformation::CreateResumptionsAndSuspensions()
{
    bool useSharedLayout = (m_states.size() > 1) && ReuseContinuations();

    ContinuationLayout* sharedLayout = nullptr;
    if (useSharedLayout)
    {
        JITDUMP("Creating shared layout:\n");
        ContinuationLayoutBuilder* sharedLayoutBuilder =
            ContinuationLayoutBuilder::CreateSharedLayout(m_compiler, m_states);
        sharedLayout = sharedLayoutBuilder->Create();
    }

    JITDUMP("Creating suspensions and resumptions for %zu states\n", m_states.size());
    for (const AsyncState& state : m_states)
    {
        JITDUMP("State %u suspend @ " FMT_BB ", resume @ " FMT_BB "\n", state.Number, state.SuspensionBB->bbNum,
                state.ResumptionBB->bbNum);
        ContinuationLayout* layout = sharedLayout == nullptr ? state.Layout->Create() : sharedLayout;
        CreateSuspension(state.CallBlock, state.Call, state.SuspensionBB, state.Number, *layout, *state.Layout,
                         state.ResumeReachable, state.MutatedSincePreviousResumption);
        CreateResumption(state.CallBlock, state.Call, state.ResumptionBB, state.CallDefInfo, *layout, *state.Layout);
        CreateDebugInfoForSuspensionPoint(*layout, *state.Layout);

        JITDUMP("\n");
    }
}

//------------------------------------------------------------------------
// AsyncTransformation::ReuseContinuations:
//   Returns true if continuation reuse is enabled.
//
// Returns:
//   True if so.
//
bool AsyncTransformation::ReuseContinuations()
{
#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitAsyncReuseContinuationsRange());

    if (!s_range.Contains(m_compiler->info.compMethodHash()))
    {
        return false;
    }
#endif

    return JitConfig.JitAsyncReuseContinuations() != 0;
}

//------------------------------------------------------------------------
// ContinuationLayoutBuilder::CreateSharedLayout:
//   Create a shared continuation layout that is the union of all per-call
//   layouts. The shared layout contains every local, return type, and
//   optional field needed by any individual suspension point.
//
// Parameters:
//   comp   - The compiler instance.
//   states - The vector of async states to merge.
//
// Returns:
//   A new ContinuationLayoutBuilder representing the merged layout.
//
ContinuationLayoutBuilder* ContinuationLayoutBuilder::CreateSharedLayout(Compiler*                         comp,
                                                                         const jitstd::vector<AsyncState>& states)
{
    unsigned maxLocalStored = 0;
    for (const AsyncState& state : states)
    {
        jitstd::vector<unsigned>& locals = state.Layout->m_locals;
        if (locals.size() > 0)
        {
            maxLocalStored = std::max(maxLocalStored, locals[locals.size() - 1]);
        }
    }

    ContinuationLayoutBuilder* sharedLayout = new (comp, CMK_Async) ContinuationLayoutBuilder(comp);
    BitVecTraits               traits(maxLocalStored + 1, comp);
    BitVec                     locals(BitVecOps::MakeEmpty(&traits));

    for (const AsyncState& state : states)
    {
        ContinuationLayoutBuilder* layout = state.Layout;
        sharedLayout->m_needsOSRAddress |= layout->m_needsOSRAddress;
        sharedLayout->m_needsException |= layout->m_needsException;
        sharedLayout->m_needsContinuationContext |= layout->m_needsContinuationContext;
        sharedLayout->m_needsKeepAlive |= layout->m_needsKeepAlive;
        sharedLayout->m_needsExecutionContext |= layout->m_needsExecutionContext;

        for (unsigned local : layout->m_locals)
        {
            assert(local <= maxLocalStored);
            BitVecOps::AddElemD(&traits, locals, local);
        }

        for (const ReturnTypeInfo& ret : layout->m_returns)
        {
            sharedLayout->AddReturn(ret);
        }
    }

    BitVecOps::VisitBits(&traits, locals, [=](unsigned localNum) {
        sharedLayout->AddLocal(localNum);
        return true;
    });

    return sharedLayout;
}

//------------------------------------------------------------------------
// AsyncTransformation::CreateResumptionSwitch:
//   Create the IR for the entry of the function that checks the continuation
//   and dispatches on its state number.
//
void AsyncTransformation::CreateResumptionSwitch()
{
    m_compiler->fgCreateNewInitBB();
    BasicBlock* newEntryBB = m_compiler->fgFirstBB;

    GenTree* continuationArg = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
    GenTree* null            = m_compiler->gtNewNull();
    GenTree* neNull          = m_compiler->gtNewOperNode(GT_NE, TYP_INT, continuationArg, null);
    GenTree* jtrue           = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, neNull);
    LIR::AsRange(newEntryBB).InsertAtEnd(continuationArg, null, neNull, jtrue);

    FlowEdge* resumingEdge;

    if (m_states.size() == 1)
    {
        JITDUMP("  Redirecting entry " FMT_BB " directly to " FMT_BB " as it is the only resumption block\n",
                newEntryBB->bbNum, m_states[0].ResumptionBB->bbNum);
        resumingEdge = m_compiler->fgAddRefPred(m_states[0].ResumptionBB, newEntryBB);
    }
    else if (m_states.size() == 2)
    {
        BasicBlock* condBB = m_compiler->fgNewBBbefore(BBJ_COND, m_states[0].ResumptionBB, true);
        condBB->inheritWeightPercentage(newEntryBB, 0);

        FlowEdge* to0 = m_compiler->fgAddRefPred(m_states[0].ResumptionBB, condBB);
        FlowEdge* to1 = m_compiler->fgAddRefPred(m_states[1].ResumptionBB, condBB);
        condBB->SetCond(to1, to0);
        to1->setLikelihood(0.5);
        to0->setLikelihood(0.5);

        resumingEdge = m_compiler->fgAddRefPred(condBB, newEntryBB);

        JITDUMP("  Redirecting entry " FMT_BB " to BBJ_COND " FMT_BB " for resumption with 2 states\n",
                newEntryBB->bbNum, condBB->bbNum);

        continuationArg          = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
        GenTree* stateOffsetNode = m_compiler->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_compiler->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* zero            = m_compiler->gtNewZeroConNode(TYP_INT);
        GenTree* stateNeZero     = m_compiler->gtNewOperNode(GT_NE, TYP_INT, stateInd, zero);
        GenTree* jtrue           = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, stateNeZero);

        LIR::AsRange(condBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, zero, stateNeZero,
                                         jtrue);
    }
    else
    {
        BasicBlock* switchBB = m_compiler->fgNewBBbefore(BBJ_SWITCH, m_states[0].ResumptionBB, true);
        switchBB->inheritWeightPercentage(newEntryBB, 0);

        resumingEdge = m_compiler->fgAddRefPred(switchBB, newEntryBB);

        JITDUMP("  Redirecting entry " FMT_BB " to BBJ_SWITCH " FMT_BB " for resumption with %zu states\n",
                newEntryBB->bbNum, switchBB->bbNum, m_states.size());

        continuationArg          = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned stateOffset     = m_compiler->info.compCompHnd->getFieldOffset(m_asyncInfo->continuationStateFldHnd);
        GenTree* stateOffsetNode = m_compiler->gtNewIconNode((ssize_t)stateOffset, TYP_I_IMPL);
        GenTree* stateAddr       = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, continuationArg, stateOffsetNode);
        GenTree* stateInd        = m_compiler->gtNewIndir(TYP_INT, stateAddr, GTF_IND_NONFAULTING);
        GenTree* switchNode      = m_compiler->gtNewOperNode(GT_SWITCH, TYP_VOID, stateInd);

        LIR::AsRange(switchBB).InsertAtEnd(continuationArg, stateOffsetNode, stateAddr, stateInd, switchNode);

        m_compiler->fgHasSwitch = true;

        // Add 1 for default case
        const size_t     numCases       = m_states.size() + 1;
        FlowEdge** const cases          = new (m_compiler, CMK_FlowEdge) FlowEdge*[numCases * 2];
        FlowEdge** const succs          = cases + numCases;
        unsigned         numUniqueSuccs = 0;

        const weight_t stateLikelihood = 1.0 / numCases;
        for (size_t i = 0; i < numCases; i++)
        {
            // Wrap around and use first resumption BB as default case
            BasicBlock*     resumptionBB = m_states[i % m_states.size()].ResumptionBB;
            FlowEdge* const edge         = m_compiler->fgAddRefPred(resumptionBB, switchBB);
            edge->setLikelihood(stateLikelihood);
            cases[i] = edge;

            if (edge->getDupCount() == 1)
            {
                succs[numUniqueSuccs++] = edge;
            }
        }

        BBswtDesc* const swtDesc = new (m_compiler, CMK_BasicBlock)
            BBswtDesc(succs, numUniqueSuccs, cases, (unsigned)numCases, /* hasDefault */ true);
        switchBB->SetSwitch(swtDesc);
    }

    newEntryBB->SetCond(resumingEdge, newEntryBB->GetTargetEdge());
    resumingEdge->setLikelihood(0);
    newEntryBB->GetFalseEdge()->setLikelihood(1);

    if (m_compiler->doesMethodHavePatchpoints())
    {
        JITDUMP("  Method has patch points...\n");
        // If we have patchpoints then first check if we need to resume in the OSR version.
        BasicBlock* jmpOSR = m_compiler->fgNewBBafter(BBJ_THROW, m_compiler->fgLastBBInMainFunction(), false);
        jmpOSR->bbSetRunRarely();
        jmpOSR->clearTryIndex();
        jmpOSR->clearHndIndex();

        JITDUMP("    Created " FMT_BB " for transitions back into OSR method\n", jmpOSR->bbNum);

        BasicBlock* onContinuationBB        = newEntryBB->GetTrueTarget();
        BasicBlock* checkOSRAddressOffsetBB = m_compiler->fgNewBBbefore(BBJ_COND, onContinuationBB, true);

        JITDUMP("    Created " FMT_BB " to check whether we should transition immediately to OSR\n",
                checkOSRAddressOffsetBB->bbNum);

        // Redirect newEntryBB -> onContinuationBB into newEntryBB -> checkOSRAddressOffsetBB -> onContinuationBB
        m_compiler->fgRedirectEdge(newEntryBB->TrueEdgeRef(), checkOSRAddressOffsetBB);
        newEntryBB->GetTrueEdge()->setLikelihood(0);
        checkOSRAddressOffsetBB->inheritWeightPercentage(newEntryBB, 0);

        FlowEdge* toOnContinuationBB = m_compiler->fgAddRefPred(onContinuationBB, checkOSRAddressOffsetBB);
        FlowEdge* toJmpOSRBB         = m_compiler->fgAddRefPred(jmpOSR, checkOSRAddressOffsetBB);
        checkOSRAddressOffsetBB->SetCond(toJmpOSRBB, toOnContinuationBB);
        toJmpOSRBB->setLikelihood(0);
        toOnContinuationBB->setLikelihood(1);
        jmpOSR->inheritWeightPercentage(checkOSRAddressOffsetBB, 0);

        // We need to dispatch to the OSR version if the OSR address is non-zero.
        continuationArg                   = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfOSRAddressOffset = OFFSETOF__CORINFO_Continuation__data;
        GenTree* osrAddress               = LoadFromOffset(continuationArg, offsetOfOSRAddressOffset, TYP_I_IMPL);
        unsigned osrAddressLclNum         = m_compiler->lvaGrabTemp(false DEBUGARG("OSR address for tier0 OSR method"));
        m_compiler->lvaGetDesc(osrAddressLclNum)->lvType = TYP_I_IMPL;
        GenTree* storeOsrAddress = m_compiler->gtNewStoreLclVarNode(osrAddressLclNum, osrAddress);
        LIR::AsRange(checkOSRAddressOffsetBB).InsertAtEnd(LIR::SeqTree(m_compiler, storeOsrAddress));

        osrAddress      = m_compiler->gtNewLclvNode(osrAddressLclNum, TYP_I_IMPL);
        GenTree* zero   = m_compiler->gtNewIconNode(0, TYP_I_IMPL);
        GenTree* neZero = m_compiler->gtNewOperNode(GT_NE, TYP_INT, osrAddress, zero);
        GenTree* jtrue  = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, neZero);
        LIR::AsRange(checkOSRAddressOffsetBB).InsertAtEnd(osrAddress, zero, neZero, jtrue);

        osrAddress = m_compiler->gtNewLclvNode(osrAddressLclNum, TYP_I_IMPL);

        GenTree* jmpOsr = m_compiler->gtNewOperNode(GT_NONLOCAL_JMP, TYP_VOID, osrAddress);
        LIR::AsRange(jmpOSR).InsertAtEnd(LIR::SeqTree(m_compiler, jmpOsr));
    }
    else if (m_compiler->opts.IsOSR())
    {
        JITDUMP("  Method is an OSR function\n");
        // If the tier-0 version resumed and then transitioned to the OSR
        // version by normal means then we will see a non-zero continuation
        // here that belongs to the tier0 method. In that case we should just
        // ignore it, so create a BB that jumps back.
        BasicBlock* onContinuationBB        = newEntryBB->GetTrueTarget();
        BasicBlock* onNoContinuationBB      = newEntryBB->GetFalseTarget();
        BasicBlock* checkOSRAddressOffsetBB = m_compiler->fgNewBBbefore(BBJ_COND, onContinuationBB, true);

        // Switch newEntryBB -> onContinuationBB into newEntryBB -> checkOSRAddressOffsetBB
        m_compiler->fgRedirectEdge(newEntryBB->TrueEdgeRef(), checkOSRAddressOffsetBB);
        newEntryBB->GetTrueEdge()->setLikelihood(0);
        checkOSRAddressOffsetBB->inheritWeightPercentage(newEntryBB, 0);

        // Make checkOSRAddressOffsetBB ->(true)  onNoContinuationBB
        //                        ->(false) onContinuationBB

        FlowEdge* toOnContinuationBB   = m_compiler->fgAddRefPred(onContinuationBB, checkOSRAddressOffsetBB);
        FlowEdge* toOnNoContinuationBB = m_compiler->fgAddRefPred(onNoContinuationBB, checkOSRAddressOffsetBB);
        checkOSRAddressOffsetBB->SetCond(toOnNoContinuationBB, toOnContinuationBB);
        toOnContinuationBB->setLikelihood(0);
        toOnNoContinuationBB->setLikelihood(1);

        JITDUMP("    Created " FMT_BB " to check for Tier-0 continuations\n", checkOSRAddressOffsetBB->bbNum);

        continuationArg                   = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
        unsigned offsetOfOSRAddressOffset = OFFSETOF__CORINFO_Continuation__data;
        GenTree* osrAddress               = LoadFromOffset(continuationArg, offsetOfOSRAddressOffset, TYP_I_IMPL);
        GenTree* zero                     = m_compiler->gtNewIconNode(0, TYP_I_IMPL);
        GenTree* eqZero                   = m_compiler->gtNewOperNode(GT_EQ, TYP_INT, osrAddress, zero);
        GenTree* jtrue                    = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, eqZero);
        LIR::AsRange(checkOSRAddressOffsetBB).InsertAtEnd(LIR::SeqTree(m_compiler, jtrue));

        if (ReuseContinuations())
        {
            // Also, save the fact that we have a reusable continuation
            continuationArg        = m_compiler->gtNewLclvNode(m_compiler->lvaAsyncContinuationArg, TYP_REF);
            GenTree* storeReusable = m_compiler->gtNewStoreLclVarNode(m_reuseContinuationVar, continuationArg);
            LIR::AsRange(onContinuationBB).InsertAtBeginning(continuationArg, storeReusable);
        }
    }
}
