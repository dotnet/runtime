// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Inline Support

/*****************************************************************************/

//------------------------------------------------------------------------
// fgCheckForInlineDepthAndRecursion: compute depth of the candidate, and
// check for recursion.
//
// Return Value:
//    The depth of the inline candidate. The root method is a depth 0, top-level
//    candidates at depth 1, etc.
//
// Notes:
//    The depth limit is a policy consideration, and serves mostly as a
//    safeguard to prevent runaway inlining of small methods.
//
unsigned Compiler::fgCheckInlineDepthAndRecursion(InlineInfo* inlineInfo)
{
    InlineContext* inlineContext = inlineInfo->inlineCandidateInfo->inlinersContext;
    InlineResult*  inlineResult  = inlineInfo->inlineResult;

    // There should be a context for all candidates.
    assert(inlineContext != nullptr);

    int depth = 0;

    for (; inlineContext != nullptr; inlineContext = inlineContext->GetParent())
    {
        depth++;

        if (IsDisallowedRecursiveInline(inlineContext, inlineInfo))
        {
            // This is a recursive inline
            //
            inlineResult->NoteFatal(InlineObservation::CALLSITE_IS_RECURSIVE);

            // No need to note CALLSITE_DEPTH since we're already rejecting this candidate
            //
            return depth;
        }

        if (depth > InlineStrategy::IMPLEMENTATION_MAX_INLINE_DEPTH)
        {
            break;
        }
    }

    inlineResult->NoteInt(InlineObservation::CALLSITE_DEPTH, depth);
    return depth;
}

//------------------------------------------------------------------------
// IsDisallowedRecursiveInline: Check whether 'info' is a recursive inline (of
// 'ancestor'), and whether it should be disallowed.
//
// Return Value:
//    True if the inline is recursive and should be disallowed.
//
bool Compiler::IsDisallowedRecursiveInline(InlineContext* ancestor, InlineInfo* inlineInfo)
{
    // We disallow inlining the exact same instantiation.
    if ((ancestor->GetCallee() == inlineInfo->fncHandle) &&
        (ancestor->GetRuntimeContext() == inlineInfo->inlineCandidateInfo->exactContextHandle))
    {
        JITDUMP("Call site is trivially recursive\n");
        return true;
    }

    // None of the inline heuristics take into account that inlining will cause
    // type/method loading for generic contexts. When polymorphic recursion is
    // involved this can quickly consume a large amount of resources, so try to
    // verify that we aren't inlining recursively with complex contexts.
    if (info.compCompHnd->haveSameMethodDefinition(inlineInfo->fncHandle, ancestor->GetCallee()) &&
        ContextComplexityExceeds(inlineInfo->inlineCandidateInfo->exactContextHandle, 64))
    {
        JITDUMP("Call site is recursive with a complex generic context\n");
        return true;
    }

    // Not recursive, or allowed recursive inline.
    return false;
}

//------------------------------------------------------------------------
// ContextComplexityExceeds: Check whether the complexity of a generic context
// exceeds a specified maximum.
//
// Arguments:
//    handle - Handle for the generic context
//    max    - Max complexity
//
// Return Value:
//    True if the max was exceeded.
//
bool Compiler::ContextComplexityExceeds(CORINFO_CONTEXT_HANDLE handle, int max)
{
    if (handle == nullptr)
    {
        return false;
    }

    int cur = 0;

    // We do not expect to try to inline with the sentinel context.
    assert(handle != METHOD_BEING_COMPILED_CONTEXT());

    if (((size_t)handle & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
    {
        return MethodInstantiationComplexityExceeds(CORINFO_METHOD_HANDLE((size_t)handle & ~CORINFO_CONTEXTFLAGS_MASK),
                                                    cur, max);
    }

    return TypeInstantiationComplexityExceeds(CORINFO_CLASS_HANDLE((size_t)handle & ~CORINFO_CONTEXTFLAGS_MASK), cur,
                                              max);
}

//------------------------------------------------------------------------
// MethodInstantiationComplexityExceeds: Check whether the complexity of a
// method's instantiation exceeds a specified maximum.
//
// Arguments:
//    handle - Handle for a method that may be generic
//    cur    - [in, out] Current complexity (number of types seen in the instantiation)
//    max    - Max complexity
//
// Return Value:
//    True if the max was exceeded.
//
bool Compiler::MethodInstantiationComplexityExceeds(CORINFO_METHOD_HANDLE handle, int& cur, int max)
{
    CORINFO_SIG_INFO sig;
    info.compCompHnd->getMethodSig(handle, &sig);

    cur += sig.sigInst.classInstCount + sig.sigInst.methInstCount;
    if (cur > max)
    {
        return true;
    }

    for (unsigned i = 0; i < sig.sigInst.classInstCount; i++)
    {
        if (TypeInstantiationComplexityExceeds(sig.sigInst.classInst[i], cur, max))
        {
            return true;
        }
    }

    for (unsigned i = 0; i < sig.sigInst.methInstCount; i++)
    {
        if (TypeInstantiationComplexityExceeds(sig.sigInst.methInst[i], cur, max))
        {
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// TypeInstantiationComplexityExceeds: Check whether the complexity of a type's
// instantiation exceeds a specified maximum.
//
// Arguments:
//    handle - Handle for a class that may be generic
//    cur    - [in, out] Current complexity (number of types seen in the instantiation)
//    max    - Max complexity
//
// Return Value:
//    True if the max was exceeded.
//
bool Compiler::TypeInstantiationComplexityExceeds(CORINFO_CLASS_HANDLE handle, int& cur, int max)
{
    for (int i = 0;; i++)
    {
        CORINFO_CLASS_HANDLE instArg = info.compCompHnd->getTypeInstantiationArgument(handle, i);

        if (instArg == NO_CLASS_HANDLE)
        {
            break;
        }

        if (++cur > max)
        {
            return true;
        }

        if (TypeInstantiationComplexityExceeds(instArg, cur, max))
        {
            return true;
        }
    }

    return false;
}

class InlineAndDevirtualizeWalker : public GenTreeVisitor<InlineAndDevirtualizeWalker>
{
    bool m_madeChanges = false;
    BasicBlock* m_block = nullptr;
    Statement* m_statement = nullptr;
    BasicBlock* m_nextBlock = nullptr;
    Statement* m_nextStatement = nullptr;


public:
    enum
    {
        DoPreOrder        = true,
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    InlineAndDevirtualizeWalker(Compiler* comp)
        : GenTreeVisitor(comp)
    {
    }

    bool MadeChanges() const
    {
        return m_madeChanges;
    }

    BasicBlock* NextBlock()
    {
        return m_nextBlock;
    }

    Statement* NextStatement()
    {
        return m_nextStatement;
    }

    void VisitStatement(BasicBlock* block, Statement* statement)
    {
        m_nextBlock = nullptr;
        m_nextStatement = nullptr;
        m_block = block;
        m_statement = statement;
        WalkTree(statement->GetRootNodePointer(), nullptr);
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        // Inlining and late devirt are triggered by GT_CALL with the call
        // side effect flag, so bail out for any trees that don't have this
        // flag.
        if ((tree->gtFlags & GTF_CALL) == 0)
        {
            return fgWalkResult::WALK_SKIP_SUBTREES;
        }

        while ((*use)->IsCall() && TryInline(use, user))
        {
            if (m_nextBlock != nullptr)
            {
                return fgWalkResult::WALK_ABORT;
            }
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        LateDevirtualization(use, user);

        return fgWalkResult::WALK_CONTINUE;
    }

private:
    bool TryInline(GenTree** use, GenTree* parent)
    {
        GenTreeCall* call = (*use)->AsCall();

        if (!call->IsInlineCandidate())
        {
            return false;
        }

        InlineResult inlineResult(m_compiler, call, call->gtInlineCandidateInfo->inlinersContext, "TryInline");

        m_compiler->compCurBB = m_block;
        m_compiler->fgMorphStmt = m_statement;

        InlineInfo inlineInfo{};
        m_compiler->fgMorphCallInline(inlineInfo, call, &inlineResult);

        assert(inlineResult.IsSuccess() == call->IsInlineCandidate());
        if (!inlineResult.IsSuccess())
        {
            // If the inline was rejected and returns a retbuffer, then mark that
            // local as DNER now so that promotion knows to leave it up to physical
            // promotion.
            CallArg* retBuffer = call->gtArgs.GetRetBufferArg();
            if ((retBuffer != nullptr) && retBuffer->GetNode()->OperIs(GT_LCL_ADDR))
            {
                m_compiler->lvaSetVarDoNotEnregister(retBuffer->GetNode()->AsLclVarCommon()->GetLclNum()
                                                         DEBUGARG(DoNotEnregisterReason::HiddenBufferStructArg));
            }

            return false;
        }

        JITDUMP("Inlining candidate [%06u] in " FMT_STMT "\n", Compiler::dspTreeID(call), m_statement->GetID());
        DISPSTMT(m_statement);

        if (InsertMidStatement(inlineInfo, use))
        {
            return true;
        }

        JITDUMP("Splitting is required\n");
        BasicBlock* callBlock = m_compiler->compCurBB;
        Statement* callStmt = m_compiler->fgMorphStmt;
        Statement* newStmt = nullptr;
        GenTree** use2 = nullptr;
        m_compiler->gtSplitTree(callBlock, callStmt, call, &newStmt, &use2, /* includeOperands */ false, /* early */ true);
        assert(use2 == use);

        JITDUMP("After split:\n");
        while ((newStmt != callStmt) && (newStmt != nullptr))
        {
            DISPSTMT(newStmt);
            newStmt = newStmt->GetNextStmt();
        }

        DISPSTMT(callStmt);

        Statement* predStmt = callStmt == callBlock->firstStmt() ? nullptr : callStmt->GetPrevStmt();

        inlineInfo.setupStatements.InsertIntoBlockBefore(callBlock, callStmt);

        GenTree* substExpr = call->gtInlineCandidateInfo->result.substExpr;
        if ((substExpr != nullptr) && substExpr->IsValue() && m_compiler->gtComplexityExceeds(substExpr, 16))
        {
            JITDUMP("Substitution expression is complex so spilling it to its own statement\n");
            unsigned lclNum = m_compiler->lvaGrabTemp(false DEBUGARG("Complex inlinee substitution expression"));
            Statement* storeTemp = m_compiler->gtNewStmt(m_compiler->gtNewTempStore(lclNum, substExpr));
            DISPSTMT(storeTemp);
            inlineInfo.teardownStatements.Append(storeTemp);
            *use = m_compiler->gtNewLclvNode(lclNum, genActualType(substExpr));
        }
        else
        {
            *use = substExpr;
        }

        if (*use == nullptr)
        {
            *use = m_compiler->gtNewNothingNode();
        }
        else
        {
            if ((*use)->IsValue() && !(*use)->IsCall() && (use == callStmt->GetRootNodePointer()))
            {
                *use = m_compiler->gtUnusedValNode(*use);
            }
        }

        if (call->gtInlineCandidateInfo->result.substBB != nullptr)
        {
            // IR may potentially contain nodes that requires mandatory BB flags to be set.
            // Propagate those flags from the containing BB.
            callBlock->CopyFlags(call->gtInlineCandidateInfo->result.substBB, BBF_COPY_PROPAGATE);
        }

        m_compiler->gtUpdateStmtSideEffects(callStmt);

        if (InsertMidBlock(inlineInfo, callBlock, callStmt))
        {
            m_nextBlock = callBlock;
            m_nextStatement = predStmt == nullptr ? callBlock->firstStmt() : predStmt->GetNextStmt();
            return true;
        }

        BasicBlock* continueBlock = m_compiler->fgSplitBlockBeforeStatement(callBlock, callStmt);
        unsigned const baseBBNum = m_compiler->fgBBNumMax;

        JITDUMP("split " FMT_BB " after the inlinee call site; after portion is now " FMT_BB "\n", callBlock->bbNum,
                continueBlock->bbNum);

        // The newly split block is not special so doesn't need to be kept.
        //
        continueBlock->RemoveFlags(BBF_DONT_REMOVE);

        // Set the try and handler index and fix the jump types of inlinee's blocks.
        //
        for (BasicBlock* const block : m_compiler->InlineeCompiler->Blocks())
        {
            noway_assert(!block->hasTryIndex());
            noway_assert(!block->hasHndIndex());
            block->copyEHRegion(callBlock);
            block->CopyFlags(callBlock, BBF_BACKWARD_JUMP | BBF_PROF_WEIGHT);

            // Update block nums appropriately
            //
            block->bbNum += baseBBNum;
            m_compiler->fgBBNumMax = max(block->bbNum, m_compiler->fgBBNumMax);

            DebugInfo di = callStmt->GetDebugInfo().GetRoot();
            if (di.IsValid())
            {
                block->bbCodeOffs    = di.GetLocation().GetOffset();
                block->bbCodeOffsEnd = block->bbCodeOffs + 1; // TODO: is code size of 1 some magic number for inlining?
            }
            else
            {
                block->bbCodeOffs    = 0; // TODO: why not BAD_IL_OFFSET?
                block->bbCodeOffsEnd = 0;
                block->SetFlags(BBF_INTERNAL);
            }

            if (block->KindIs(BBJ_RETURN))
            {
                noway_assert(!block->HasFlag(BBF_HAS_JMP));
                JITDUMP("\nConvert bbKind of " FMT_BB " to BBJ_ALWAYS to bottom block " FMT_BB "\n", block->bbNum,
                        continueBlock->bbNum);

                FlowEdge* const newEdge = m_compiler->fgAddRefPred(continueBlock, block);
                block->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);
            }
        }

        // Inlinee's top block will have an artificial ref count. Remove.
        assert(m_compiler->InlineeCompiler->fgFirstBB->bbRefs > 0);
        m_compiler->InlineeCompiler->fgFirstBB->bbRefs--;

        // Insert inlinee's blocks into inliner's block list.
        assert(callBlock->KindIs(BBJ_ALWAYS));
        assert(callBlock->TargetIs(continueBlock));
        m_compiler->fgRedirectTargetEdge(callBlock, m_compiler->InlineeCompiler->fgFirstBB);

        callBlock->SetNext(m_compiler->InlineeCompiler->fgFirstBB);
        m_compiler->InlineeCompiler->fgFirstBB->SetPrev(callBlock);
        m_compiler->InlineeCompiler->fgLastBB->SetNext(continueBlock);
        continueBlock->SetPrev(m_compiler->InlineeCompiler->fgLastBB);

        //
        // Add inlinee's block count to inliner's.
        //
        m_compiler->fgBBcount += m_compiler->InlineeCompiler->fgBBcount;

        // Append statements to null out gc ref locals, if necessary.
        inlineInfo.teardownStatements.InsertIntoBlockAtBeginning(continueBlock);
        JITDUMPEXEC(m_compiler->fgDispBasicBlocks(m_compiler->InlineeCompiler->fgFirstBB, m_compiler->InlineeCompiler->fgLastBB, true));

        m_nextBlock = callBlock;
        m_nextStatement = predStmt == nullptr ? callBlock->firstStmt() : predStmt->GetNextStmt();

        return true;
    }

    bool InsertMidStatement(InlineInfo& inlineInfo, GenTree** use)
    {
        Compiler* inlineeComp = m_compiler->InlineeCompiler;
        if (inlineeComp->fgBBcount != 1)
        {
            return false;
        }

        if (!inlineeComp->fgFirstBB->KindIs(BBJ_RETURN))
        {
            return false;
        }

        if ((inlineeComp->fgFirstBB->bbStmtList != nullptr) ||
            !inlineInfo.setupStatements.Empty() ||
            !inlineInfo.teardownStatements.Empty())
        {
            return false;
        }

        if (use == m_statement->GetRootNodePointer())
        {
            // Leave this case up to the general handling.
            return false;
        }

        GenTree* call = *use;
        *use = inlineInfo.inlineCandidateInfo->result.substExpr;

        if (m_compiler->gtComplexityExceeds(m_statement->GetRootNode(), 16))
        {
            *use = call;
            return false;
        }

        JITDUMP("Inlinee tree can be inserted mid-statement\n");

        if (*use == nullptr)
        {
            *use = m_compiler->gtNewNothingNode();
        }

        if (inlineInfo.inlineCandidateInfo->result.substBB != nullptr)
        {
            // IR may potentially contain nodes that requires mandatory BB flags to be set.
            // Propagate those flags from the containing BB.
            m_block->CopyFlags(inlineInfo.inlineCandidateInfo->result.substBB, BBF_COPY_PROPAGATE);
        }

        return true;
    }

    bool InsertMidBlock(InlineInfo& inlineInfo, BasicBlock* block, Statement* stmt)
    {
        Compiler* inlineeComp = m_compiler->InlineeCompiler;
        if (inlineeComp->fgBBcount != 1)
        {
            return false;
        }

        if (!inlineeComp->fgFirstBB->KindIs(BBJ_RETURN))
        {
            return false;
        }

        JITDUMP("Inlinee does not have control flow; inserting mid-block\n");

#ifdef DEBUG
        for (Statement* stmt = inlineeComp->fgFirstBB->bbStmtList; stmt != nullptr; stmt = stmt->GetNextStmt())
        {
            DISPSTMT(stmt);
        }
#endif

        m_compiler->fgInsertStmtListBefore(block, stmt, inlineeComp->fgFirstBB->bbStmtList);

        const BasicBlockFlags inlineeBlockFlags = inlineeComp->fgFirstBB->GetFlagsRaw();
        noway_assert((inlineeBlockFlags & BBF_HAS_JMP) == 0);
        noway_assert((inlineeBlockFlags & BBF_KEEP_BBJ_ALWAYS) == 0);

        // Todo: we may want to exclude other flags here.
        block->SetFlags(inlineeBlockFlags & ~BBF_RUN_RARELY);

        inlineInfo.teardownStatements.InsertIntoBlockBefore(block, stmt);
        return true;
    }

#if FEATURE_MULTIREG_RET
    //------------------------------------------------------------------------
    // AttachStructInlineeToStore: Update a "STORE(..., inlinee)" tree.
    //
    // Morphs inlinees that are multi-reg nodes into the (only) supported shape
    // of "lcl = node()", either by marking the store local "lvIsMultiRegRet" or
    // storing the node into a temp and using that as the new value.
    //
    // Arguments:
    //    store     - The store with the inlinee as value
    //    retClsHnd - The struct handle for the inlinee
    //
    void AttachStructInlineeToStore(GenTree* store, CORINFO_CLASS_HANDLE retClsHnd)
    {
        assert(store->OperIsStore());
        GenTree* dst     = store;
        GenTree* inlinee = store->Data();

        // We need to force all stores from multi-reg nodes into the "lcl = node()" form.
        if (inlinee->IsMultiRegNode())
        {
            // Special case: we already have a local, the only thing to do is mark it appropriately. Except
            // if it may turn into an indirection. TODO-Bug: this does not account for x86 varargs args.
            if (store->OperIs(GT_STORE_LCL_VAR) && !m_compiler->lvaIsImplicitByRefLocal(store->AsLclVar()->GetLclNum()))
            {
                m_compiler->lvaGetDesc(store->AsLclVar())->lvIsMultiRegRet = true;
            }
            else
            {
                // Here, we store our node into a fresh temp and then use that temp as the new value.
                store->Data() = StoreStructInlineeToVar(inlinee, retClsHnd);
            }
        }
    }

    //------------------------------------------------------------------------
    // AssignStructInlineeToVar: Store the struct inlinee to a temp local.
    //
    // Arguments:
    //    inlinee   - The inlinee of the RET_EXPR node
    //    retClsHnd - The struct class handle of the type of the inlinee.
    //
    // Return Value:
    //    Value representing the freshly defined temp.
    //
    GenTree* StoreStructInlineeToVar(GenTree* inlinee, CORINFO_CLASS_HANDLE retClsHnd)
    {
        unsigned   lclNum = m_compiler->lvaGrabTemp(false DEBUGARG("RetBuf for struct inline return candidates."));
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        m_compiler->lvaSetStruct(lclNum, retClsHnd, false);

        // Sink the store below any COMMAs: this is required for multi-reg nodes.
        GenTree* src       = inlinee;
        GenTree* lastComma = nullptr;
        while (src->OperIs(GT_COMMA))
        {
            lastComma = src;
            src       = src->AsOp()->gtOp2;
        }

        // When storing a multi-register value to a local var, make sure the variable is marked as lvIsMultiRegRet.
        if (src->IsMultiRegNode())
        {
            varDsc->lvIsMultiRegRet = true;
        }

        GenTree* store = m_compiler->gtNewStoreLclVarNode(lclNum, src);

        // If inlinee was comma, new inlinee is (, , , lcl = inlinee).
        if (inlinee->OperIs(GT_COMMA))
        {
            lastComma->AsOp()->gtOp2 = store;
            store                    = inlinee;
        }

        GenTree* lcl = m_compiler->gtNewLclvNode(lclNum, varDsc->TypeGet());
        return m_compiler->gtNewOperNode(GT_COMMA, lcl->TypeGet(), store, lcl);
    }
#endif // FEATURE_MULTIREG_RET

    CORINFO_METHOD_HANDLE GetMethodHandle(GenTreeCall* call)
    {
        assert(call->IsDevirtualizationCandidate(m_compiler));
        if (call->IsVirtual())
        {
            return call->gtCallMethHnd;
        }
        else
        {
            GenTree* runtimeMethHndNode =
                call->gtCallAddr->AsCall()->gtArgs.FindWellKnownArg(WellKnownArg::RuntimeMethodHandle)->GetNode();
            assert(runtimeMethHndNode != nullptr);
            switch (runtimeMethHndNode->OperGet())
            {
                case GT_RUNTIMELOOKUP:
                    return runtimeMethHndNode->AsRuntimeLookup()->GetMethodHandle();
                case GT_CNS_INT:
                    return CORINFO_METHOD_HANDLE(runtimeMethHndNode->AsIntCon()->IconValue());
                default:
                    assert(!"Unexpected type in RuntimeMethodHandle arg.");
                    return nullptr;
            }
            return nullptr;
        }
    }

    //------------------------------------------------------------------------
    // LateDevirtualization: re-examine calls after inlining to see if we
    //   can do more devirtualization
    //
    // Arguments:
    //    pTree -- pointer to tree to examine for updates
    //    parent -- parent node containing the pTree edge
    //
    // Returns:
    //    fgWalkResult indicating the walk should continue; that
    //    is we wish to fully explore the tree.
    //
    // Notes:
    //    We used to check this opportunistically in the preorder callback for
    //    calls where the `obj` was fed by a return, but we now re-examine
    //    all calls.
    //
    //    Late devirtualization (and eventually, perhaps, other type-driven
    //    opts like cast optimization) can happen now because inlining or other
    //    optimizations may have provided more accurate types than we saw when
    //    first importing the trees.
    //
    //    It would be nice to screen candidate sites based on the likelihood
    //    that something has changed. Otherwise we'll waste some time retrying
    //    an optimization that will just fail again.
    void LateDevirtualization(GenTree** pTree, GenTree* parent)
    {
        GenTree* tree = *pTree;
        // In some (rare) cases the parent node of tree will be smashed to a NOP during
        // the preorder by AttachStructToInlineeArg.
        //
        // jit\Methodical\VT\callconv\_il_reljumper3 for x64 linux
        //
        // If so, just bail out here.
        if (tree == nullptr)
        {
            assert((parent != nullptr) && parent->OperIs(GT_NOP));
            return;
        }

        if (tree->IsCall())
        {
            GenTreeCall* call = tree->AsCall();
            // TODO-CQ: Drop `call->gtCallType == CT_USER_FUNC` once we have GVM devirtualization
            bool tryLateDevirt = call->IsDevirtualizationCandidate(m_compiler) && (call->gtCallType == CT_USER_FUNC);

#ifdef DEBUG
            tryLateDevirt = tryLateDevirt && (JitConfig.JitEnableLateDevirtualization() == 1);
#endif // DEBUG

            if (tryLateDevirt)
            {
#ifdef DEBUG
                if (m_compiler->verbose)
                {
                    printf("**** Late devirt opportunity\n");
                    m_compiler->gtDispTree(call);
                }
#endif // DEBUG

                CORINFO_CONTEXT_HANDLE context                = call->gtLateDevirtualizationInfo->exactContextHnd;
                InlineContext*         inlinersContext        = call->gtLateDevirtualizationInfo->inlinersContext;
                CORINFO_METHOD_HANDLE  method                 = GetMethodHandle(call);
                unsigned               methodFlags            = 0;
                const bool             isLateDevirtualization = true;
                const bool             explicitTailCall       = call->IsTailPrefixedCall();

                CORINFO_CONTEXT_HANDLE contextInput = context;
                context                             = nullptr;
                m_compiler->impDevirtualizeCall(call, nullptr, &method, &methodFlags, &contextInput, &context,
                                                isLateDevirtualization, explicitTailCall);

                if (!call->IsDevirtualizationCandidate(m_compiler))
                {
                    assert(context != nullptr);
                    assert(inlinersContext != nullptr);
                    CORINFO_CALL_INFO callInfo = {};
                    callInfo.hMethod           = method;
                    callInfo.methodFlags       = methodFlags;
                    m_compiler->impMarkInlineCandidate(call, context, false, &callInfo, inlinersContext);

                    if (call->IsInlineCandidate())
                    {
                        Statement* newStmt = nullptr;
                        GenTree**  callUse = nullptr;
                        if (m_compiler->gtSplitTree(m_compiler->compCurBB, m_curStmt, call, &newStmt, &callUse, true))
                        {
                            if (m_firstNewStmt == nullptr)
                            {
                                m_firstNewStmt = newStmt;
                            }
                        }

                        // If the call is the root expression in a statement, and it returns void,
                        // we can inline it directly without creating a RET_EXPR.
                        if (parent != nullptr || call->gtReturnType != TYP_VOID)
                        {
                            Statement* stmt = m_compiler->gtNewStmt(call);
                            m_compiler->fgInsertStmtBefore(m_compiler->compCurBB, m_curStmt, stmt);
                            if (m_firstNewStmt == nullptr)
                            {
                                m_firstNewStmt = stmt;
                            }

                            GenTreeRetExpr* retExpr =
                                m_compiler->gtNewInlineCandidateReturnExpr(call->AsCall(),
                                                                           genActualType(call->TypeGet()));
                            call->GetSingleInlineCandidateInfo()->retExpr = retExpr;

                            JITDUMP("Creating new RET_EXPR for [%06u]:\n", call->gtTreeID);
                            DISPTREE(retExpr);

                            *pTree = retExpr;
                        }

                        JITDUMP("New inline candidate due to late devirtualization:\n");
                        DISPTREE(call);
                    }
                }
                m_madeChanges = true;
            }
        }
        else if (tree->OperIs(GT_STORE_LCL_VAR))
        {
            const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
            GenTree* const value  = tree->AsLclVarCommon()->Data();

            // If we're storing to a ref typed local that has one definition,
            // we may be able to sharpen the type for the local.
            if (tree->TypeIs(TYP_REF))
            {
                LclVarDsc* lcl = m_compiler->lvaGetDesc(lclNum);

                if (lcl->lvSingleDef)
                {
                    bool                 isExact;
                    bool                 isNonNull;
                    CORINFO_CLASS_HANDLE newClass = m_compiler->gtGetClassHandle(value, &isExact, &isNonNull);

                    if (newClass != NO_CLASS_HANDLE)
                    {
                        m_compiler->lvaUpdateClass(lclNum, newClass, isExact);
                        m_madeChanges                    = true;
                        m_compiler->hasUpdatedTypeLocals = true;
                    }
                }
            }

            // If we created a self-store (say because we are sharing return spill temps) we can remove it.
            //
            if (value->OperIs(GT_LCL_VAR) && (value->AsLclVar()->GetLclNum() == lclNum))
            {
                JITDUMP("... removing self-store\n");
                DISPTREE(tree);
                tree->gtBashToNOP();
                m_madeChanges = true;
            }
        }
        else if (tree->OperIs(GT_JTRUE))
        {
            // See if this jtrue is now foldable.
            BasicBlock* block = m_block;
            GenTree*    condTree = tree->AsOp()->gtOp1;
            assert(tree == block->lastStmt()->GetRootNode());

            while (condTree->OperIs(GT_COMMA))
            {
                // Tree is a root node, and condTree its only child.
                // Move comma effects to a prior statement.
                //
                GenTree* sideEffects = nullptr;
                m_compiler->gtExtractSideEffList(condTree->gtGetOp1(), &sideEffects);

                if (sideEffects != nullptr)
                {
                    m_compiler->fgNewStmtNearEnd(block, sideEffects);
                }

                // Splice out the comma with its value
                //
                GenTree* const valueTree = condTree->gtGetOp2();
                condTree                 = valueTree;
                tree->AsOp()->gtOp1      = valueTree;
                modifiedTree             = true;
            }

            if (modifiedTree)
            {
                m_compiler->gtUpdateNodeSideEffects(tree);
            }

            assert(condTree->OperIs(GT_CNS_INT) || condTree->OperIsCompare());

            if (condTree->OperIs(GT_CNS_INT))
            {
                JITDUMP(" ... found foldable jtrue at [%06u] in " FMT_BB "\n", m_compiler->dspTreeID(tree),
                        block->bbNum);
                m_compiler->Metrics.InlinerBranchFold++;

                // We have a constant operand, and should have the all clear to optimize.
                // Update side effects on the tree, assert there aren't any, and bash to nop.
                m_compiler->gtUpdateNodeSideEffects(tree);
                assert((tree->gtFlags & GTF_SIDE_EFFECT) == 0);
                tree->gtBashToNOP();
                m_madeChanges          = true;
                FlowEdge* removedEdge  = nullptr;
                FlowEdge* retainedEdge = nullptr;

                if (condTree->IsIntegralConst(0))
                {
                    removedEdge  = block->GetTrueEdge();
                    retainedEdge = block->GetFalseEdge();
                }
                else
                {
                    removedEdge  = block->GetFalseEdge();
                    retainedEdge = block->GetTrueEdge();
                }

                m_compiler->fgRemoveRefPred(removedEdge);
                block->SetKindAndTargetEdge(BBJ_ALWAYS, retainedEdge);

                // Update profile, make it consistent if possible.
                //
                m_compiler->fgRepairProfileCondToUncond(block, retainedEdge, removedEdge,
                                                        &m_compiler->Metrics.ProfileInconsistentInlinerBranchFold);
            }
        }
        else
        {
            *pTree        = m_compiler->gtFoldExpr(tree);
            m_madeChanges = true;
        }
    }
};

//------------------------------------------------------------------------
// fgInline - expand inline candidates
//
// Returns:
//   phase status indicating if anything was modified
//
// Notes:
//   Inline candidates are identified during importation and candidate calls
//   must be top-level expressions. In input IR, the result of the call (if any)
//   is consumed elsewhere by a GT_RET_EXPR node.
//
//   For successful inlines, calls are replaced by a sequence of argument setup
//   instructions, the inlined method body, and return value cleanup. Note
//   Inlining may introduce new inline candidates. These are processed in a
//   depth-first fashion, as the inliner walks the IR in statement order.
//
//   After inline expansion in a statement, the statement tree
//   is walked to locate GT_RET_EXPR nodes. These are replaced by either
//   * the original call tree, if the inline failed
//   * the return value tree from the inlinee, if the inline succeeded
//
//   This replacement happens in preorder; on the postorder side of the same
//   tree walk, we look for opportunities to devirtualize or optimize now that
//   we know the context for the newly supplied return value tree.
//
//   Inline arguments may be directly substituted into the body of the inlinee
//   in some cases. See impInlineFetchArg.
//
PhaseStatus Compiler::fgInline()
{
    if (!opts.OptEnabled(CLFLG_INLINING))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    fgPrintInlinedMethods =
        JitConfig.JitPrintInlinedMethods().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args);
#endif // DEBUG

    if (fgPgoConsistent)
    {
        Metrics.ProfileConsistentBeforeInline++;
    }

    if (!fgHaveProfileWeights())
    {
        JITDUMP("INLINER: no pgo data\n");
    }
    else
    {
        JITDUMP("INLINER: pgo source is %s; pgo data is %sconsistent; %strusted; %ssufficient\n",
                compGetPgoSourceName(), fgPgoConsistent ? "" : "not ", fgHaveTrustedProfileWeights() ? "" : "not ",
                fgHaveSufficientProfileWeights() ? "" : "not ");
    }

    noway_assert(fgFirstBB != nullptr);

    InlineAndDevirtualizeWalker walker(this);
    bool                                        madeChanges = false;
    BasicBlock* currentBlock = fgFirstBB;

    while (currentBlock != nullptr)
    {
        Statement* currentStmt = currentBlock->firstStmt();
        while (currentStmt != nullptr)
        {
            // In debug builds we want the inline tree to show all failed
            // inlines. Some inlines may fail very early and never make it to
            // candidate stage. So scan the tree looking for those early failures.
            INDEBUG(fgWalkTreePre(currentStmt->GetRootNodePointer(), fgFindNonInlineCandidate, currentStmt));

            walker.VisitStatement(currentBlock, currentStmt);

            if (walker.NextBlock() != nullptr)
            {
                currentBlock = walker.NextBlock();
                currentStmt = walker.NextStatement();
                continue;
            }

            Statement* nextStmt = currentStmt->GetNextStmt();

            if (currentStmt->GetRootNode()->IsNothingNode())
            {
                fgRemoveStmt(currentBlock, currentStmt);
                madeChanges = true;
            }

            currentStmt = nextStmt;
        }

        currentBlock = currentBlock->Next();

    }

    madeChanges |= walker.MadeChanges();

#ifdef DEBUG

    // Check that we should not have any inline candidates left.
    for (BasicBlock* block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            // Call Compiler::fgDebugCheckInlineCandidates on each node
            fgWalkTreePre(stmt->GetRootNodePointer(), fgDebugCheckInlineCandidates);
        }

    }

    fgVerifyHandlerTab();

    if (verbose || fgPrintInlinedMethods)
    {
        JITDUMP("**************** Inline Tree");
        printf("\n");
        m_inlineStrategy->Dump(verbose || JitConfig.JitPrintInlinedMethodsVerbose());
    }

#endif // DEBUG

    if (fgPgoConsistent)
    {
        Metrics.ProfileConsistentAfterInline++;
    }

    Metrics.InlineCount   = m_inlineStrategy->GetInlineCount();
    Metrics.InlineAttempt = m_inlineStrategy->GetImportCount();

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------------
// fgMorphCallInline: attempt to inline a call
//
// Arguments:
//    call         - call expression to inline, inline candidate
//    inlineResult - result tracking and reporting
//
// Notes:
//    Attempts to inline the call.
//
//    If successful, callee's IR is inserted in place of the call, and
//    is marked with an InlineContext.
//
//    If unsuccessful, the transformations done in anticipation of a
//    possible inline are undone, and the candidate flag on the call
//    is cleared.
//
void Compiler::fgMorphCallInline(InlineInfo& inlineInfo, GenTreeCall* call, InlineResult* inlineResult)
{
    bool inliningFailed = false;

    InlineCandidateInfo* inlCandInfo = call->GetSingleInlineCandidateInfo();

    // Is this call an inline candidate?
    if (call->IsInlineCandidate())
    {
        InlineContext* createdContext = nullptr;
        // Attempt the inline
        fgMorphCallInlineHelper(inlineInfo, call, inlineResult, &createdContext);

        // We should have made up our minds one way or another....
        assert(inlineResult->IsDecided());

        // If we failed to inline, we have a bit of work to do to cleanup
        if (inlineResult->IsFailure())
        {
            if (createdContext != nullptr)
            {
                // We created a context before we got to the failure, so mark
                // it as failed in the tree.
                createdContext->SetFailed(inlineResult);
            }
            else
            {
#ifdef DEBUG
                // In debug we always put all inline attempts into the inline tree.
                InlineContext* ctx = m_inlineStrategy->NewContext(call->GetSingleInlineCandidateInfo()->inlinersContext,
                                                                  fgMorphStmt, call);
                ctx->SetFailed(inlineResult);
#endif
            }

            inliningFailed = true;

            // Clear the Inline Candidate flag so we can ensure later we tried
            // inlining all candidates. In debug, remember that this was an inline candidate.
            //
            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
            INDEBUG(call->SetWasInlineCandidate());
        }
    }
    else
    {
        // This wasn't an inline candidate. So it must be a GDV candidate.
        assert(call->IsGuardedDevirtualizationCandidate());

        // We already know we can't inline this call, so don't even bother to try.
        inliningFailed = true;
    }

    // If we failed to inline (or didn't even try), do some cleanup.
    if (inliningFailed)
    {
        if (call->gtReturnType != TYP_VOID)
        {
            JITDUMP("Inline [%06u] marked as failed\n", dspTreeID(call));

            // Detach the GT_CALL tree from the original statement by
            // hanging a "nothing" node to it. Later the "nothing" node will be removed
            // and the original GT_CALL tree will be picked up by the GT_RET_EXPR node.
            inlCandInfo->result.substExpr = nullptr;
            inlCandInfo->result.substBB = nullptr;
        }

        // Inlinee compiler may have determined call does not return; if so, update this compiler's state.
        if (call->IsNoReturn())
        {
            setMethodHasNoReturnCalls();
        }
    }
}

//------------------------------------------------------------------------------
// fgMorphCallInlineHelper: Helper to attempt to inline a call
//
// Arguments:
//    call           - call expression to inline, inline candidate
//    result         - result to set to success or failure
//    createdContext - The context that was created if the inline attempt got to the inliner.
//
// Notes:
//    Attempts to inline the call.
//
//    If successful, callee's IR is inserted in place of the call, and
//    is marked with an InlineContext.
//
//    If unsuccessful, the transformations done in anticipation of a
//    possible inline are undone, and the candidate flag on the call
//    is cleared.
//
//    If a context was created because we got to the importer then it is output by this function.
//    If the inline succeeded, this context will already be marked as successful. If it failed and
//    a context is returned, then it will not have been marked as success or failed.
//
void Compiler::fgMorphCallInlineHelper(InlineInfo& inlineInfo, GenTreeCall* call, InlineResult* result, InlineContext** createdContext)
{
    // Don't expect any surprises here.
    assert(result->IsCandidate());

#if defined(DEBUG)
    // Fail if we're inlining and we've reached the acceptance limit.
    //
    int      limit   = JitConfig.JitInlineLimit();
    unsigned current = m_inlineStrategy->GetInlineCount();

    if ((limit >= 0) && (current >= static_cast<unsigned>(limit)))
    {
        result->NoteFatal(InlineObservation::CALLSITE_OVER_INLINE_LIMIT);
        return;
    }
#endif // defined(DEBUG)

    if (lvaHaveManyLocals(0.9f))
    {
        // For now, attributing this to call site, though it's really
        // more of a budget issue (lvaCount currently includes all
        // caller and prospective callee locals). We still might be
        // able to inline other callees into this caller, or inline
        // this callee in other callers.
        result->NoteFatal(InlineObservation::CALLSITE_TOO_MANY_LOCALS);
        return;
    }

    if (call->IsVirtual())
    {
        result->NoteFatal(InlineObservation::CALLSITE_IS_VIRTUAL);
        return;
    }

    // Re-check this because guarded devirtualization may allow these through.
    if (gtIsRecursiveCall(call) && call->IsImplicitTailCall())
    {
        result->NoteFatal(InlineObservation::CALLSITE_IMPLICIT_REC_TAIL_CALL);
        return;
    }

    if (call->IsAsync() && info.compUsesAsyncContinuation)
    {
        // Currently not supported. Could provide a nice perf benefit for
        // Task -> runtime async thunks if we supported it.
        result->NoteFatal(InlineObservation::CALLER_ASYNC_USED_CONTINUATION);
        return;
    }

    // impMarkInlineCandidate() is expected not to mark tail prefixed calls
    // and recursive tail calls as inline candidates.
    noway_assert(!call->IsTailPrefixedCall());
    noway_assert(!call->IsImplicitTailCall() || !gtIsRecursiveCall(call));

    //
    // Calling inlinee's compiler to inline the method.
    //

    unsigned const startVars     = lvaCount;
    unsigned const startBBNumMax = fgBBNumMax;

#ifdef DEBUG
    if (verbose)
    {
        printf("Expanding INLINE_CANDIDATE in statement ");
        printStmtID(fgMorphStmt);
        printf(" in " FMT_BB ":\n", compCurBB->bbNum);
        gtDispStmt(fgMorphStmt);
        if (call->IsImplicitTailCall())
        {
            printf("Note: candidate is implicit tail call\n");
        }
    }
#endif

    impInlineRoot()->m_inlineStrategy->NoteAttempt(result);

    //
    // Invoke the compiler to inline the call.
    //

    fgInvokeInlineeCompiler(inlineInfo, call, result, createdContext);

    if (result->IsFailure())
    {
        // Undo some changes made during the inlining attempt.
        // Zero out the used locals
        memset((void*)(lvaTable + startVars), 0, (lvaCount - startVars) * sizeof(*lvaTable));
        for (unsigned i = startVars; i < lvaCount; i++)
        {
            new (&lvaTable[i], jitstd::placement_t()) LclVarDsc(); // call the constructor.
        }

        // Reset local var count and max bb num
        lvaCount   = startVars;
        fgBBNumMax = startBBNumMax;

#ifdef DEBUG
        for (BasicBlock* block : Blocks())
        {
            assert(block->bbNum <= fgBBNumMax);
        }
#endif
    }
}

#if defined(DEBUG)

//------------------------------------------------------------------------
// fgFindNonInlineCandidate: tree walk helper to ensure that a tree node
// that is not an inline candidate is noted as a failed inline.
//
// Arguments:
//    pTree - pointer to pointer tree node being walked
//    data  - contextual data for the walk
//
// Return Value:
//    walk result
//
// Note:
//    Invokes fgNoteNonInlineCandidate on the nodes it finds.

Compiler::fgWalkResult Compiler::fgFindNonInlineCandidate(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;
    if (tree->OperIs(GT_CALL))
    {
        Compiler*    compiler = data->compiler;
        Statement*   stmt     = (Statement*)data->pCallbackData;
        GenTreeCall* call     = tree->AsCall();

        compiler->fgNoteNonInlineCandidate(stmt, call);
    }
    return WALK_CONTINUE;
}

//------------------------------------------------------------------------
// fgNoteNonInlineCandidate: account for inlining failures in calls
// not marked as inline candidates.
//
// Arguments:
//    stmt  - statement containing the call
//    call  - the call itself
//
// Notes:
//    Used in debug only to try and place descriptions of inline failures
//    into the proper context in the inline tree.

void Compiler::fgNoteNonInlineCandidate(Statement* stmt, GenTreeCall* call)
{
    if (call->IsInlineCandidate() || call->IsGuardedDevirtualizationCandidate() || call->WasInlineCandidate())
    {
        return;
    }

    InlineResult      inlineResult(this, call, nullptr, "fgNoteNonInlineCandidate", true);
    InlineObservation currentObservation = InlineObservation::CALLSITE_NOT_CANDIDATE;

    // Try and recover the reason left behind when the jit decided
    // this call was not a candidate.
    InlineObservation priorObservation = call->gtInlineObservation;

    if (InlIsValidObservation(priorObservation))
    {
        currentObservation = priorObservation;
    }

    // Propagate the prior failure observation to this result.
    inlineResult.NotePriorFailure(currentObservation);

    if (call->gtCallType == CT_USER_FUNC)
    {
        m_inlineStrategy->NewContext(call->gtInlineContext, stmt, call)->SetFailed(&inlineResult);
    }
}

#endif

#ifdef DEBUG

/*****************************************************************************
 * Callback to make sure there is no more GT_RET_EXPR and GTF_CALL_INLINE_CANDIDATE nodes.
 */

/* static */
Compiler::fgWalkResult Compiler::fgDebugCheckInlineCandidates(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;
    if (tree->OperIs(GT_CALL))
    {
        assert((tree->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);
    }

    return WALK_CONTINUE;
}

#endif // DEBUG

void Compiler::fgInvokeInlineeCompiler(InlineInfo& inlineInfo, GenTreeCall* call, InlineResult* inlineResult, InlineContext** createdContext)
{
    noway_assert(call->OperIs(GT_CALL));
    noway_assert(call->IsInlineCandidate());
    noway_assert(opts.OptEnabled(CLFLG_INLINING));

    // This is the InlineInfo struct representing a method to be inlined.
    CORINFO_METHOD_HANDLE fncHandle = call->gtCallMethHnd;

    inlineInfo.fncHandle              = fncHandle;
    inlineInfo.iciCall                = call;
    inlineInfo.iciStmt                = fgMorphStmt;
    inlineInfo.iciBlock               = compCurBB;
    inlineInfo.thisDereferencedFirst  = false;
    inlineInfo.retExprClassHnd        = nullptr;
    inlineInfo.retExprClassHndIsExact = false;
    inlineInfo.inlineResult           = inlineResult;
    inlineInfo.inlInstParamArgInfo    = nullptr;
    inlineInfo.inlRetBufferArgInfo    = nullptr;
#ifdef FEATURE_SIMD
    inlineInfo.hasSIMDTypeArgLocalOrReturn = false;
#endif // FEATURE_SIMD

    InlineCandidateInfo* inlineCandidateInfo = call->GetSingleInlineCandidateInfo();
    noway_assert(inlineCandidateInfo);
    // Store the link to inlineCandidateInfo into inlineInfo
    inlineInfo.inlineCandidateInfo = inlineCandidateInfo;

    unsigned inlineDepth = fgCheckInlineDepthAndRecursion(&inlineInfo);

    if (inlineResult->IsFailure())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("Recursive or deep inline recursion detected. Will not expand this INLINECANDIDATE \n");
        }
#endif // DEBUG
        return;
    }

    // Set the trap to catch all errors (including recoverable ones from the EE)
    struct Param
    {
        Compiler*             pThis;
        GenTree*              call;
        CORINFO_METHOD_HANDLE fncHandle;
        InlineCandidateInfo*  inlineCandidateInfo;
        InlineInfo*           inlineInfo;
    } param;
    memset(&param, 0, sizeof(param));

    param.pThis               = this;
    param.call                = call;
    param.fncHandle           = fncHandle;
    param.inlineCandidateInfo = inlineCandidateInfo;
    param.inlineInfo          = &inlineInfo;
    bool success              = eeRunWithErrorTrap<Param>(
        [](Param* pParam) {
        // Init the local var info of the inlinee
        pParam->pThis->impInlineInitVars(pParam->inlineInfo);

        if (pParam->inlineInfo->inlineResult->IsCandidate())
        {
            /* Clear the temp table */
            memset(pParam->inlineInfo->lclTmpNum, -1, sizeof(pParam->inlineInfo->lclTmpNum));

            //
            // Prepare the call to jitNativeCode
            //

            pParam->inlineInfo->InlinerCompiler = pParam->pThis;
            if (pParam->pThis->impInlineInfo == nullptr)
            {
                pParam->inlineInfo->InlineRoot = pParam->pThis;
            }
            else
            {
                pParam->inlineInfo->InlineRoot = pParam->pThis->impInlineInfo->InlineRoot;
            }

            // The inline context is part of debug info and must be created
            // before we start creating statements; we lazily create it as
            // late as possible, which is here.
            pParam->inlineInfo->inlineContext =
                pParam->inlineInfo->InlineRoot->m_inlineStrategy
                    ->NewContext(pParam->inlineInfo->inlineCandidateInfo->inlinersContext, pParam->inlineInfo->iciStmt,
                                              pParam->inlineInfo->iciCall);
            pParam->inlineInfo->argCnt                   = pParam->inlineCandidateInfo->methInfo.args.totalILArgs();
            pParam->inlineInfo->tokenLookupContextHandle = pParam->inlineCandidateInfo->exactContextHandle;

            JITLOG_THIS(pParam->pThis,
                                     (LL_INFO100000, "INLINER: inlineInfo.tokenLookupContextHandle for %s set to 0x%p:\n",
                         pParam->pThis->eeGetMethodFullName(pParam->fncHandle),
                         pParam->pThis->dspPtr(pParam->inlineInfo->tokenLookupContextHandle)));

            JitFlags compileFlagsForInlinee = *pParam->pThis->opts.jitFlags;

            // The following flags are lost when inlining.
            // (This is checked in Compiler::compInitOptions().)
            compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_PROF_ENTERLEAVE);
            compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_DEBUG_EnC);
            compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_REVERSE_PINVOKE);
            compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_TRACK_TRANSITIONS);

#ifdef DEBUG
            if (pParam->pThis->verbose)
            {
                printf("\nInvoking compiler for the inlinee method %s :\n",
                       pParam->pThis->eeGetMethodFullName(pParam->fncHandle));
            }
#endif // DEBUG

            int result =
                jitNativeCode(pParam->fncHandle, pParam->inlineCandidateInfo->methInfo.scope,
                              pParam->pThis->info.compCompHnd, &pParam->inlineCandidateInfo->methInfo,
                              (void**)pParam->inlineInfo, nullptr, &compileFlagsForInlinee, pParam->inlineInfo);

            if (result != CORJIT_OK)
            {
                // If we haven't yet determined why this inline fails, use
                // a catch-all something bad happened observation.
                InlineResult* innerInlineResult = pParam->inlineInfo->inlineResult;

                if (!innerInlineResult->IsFailure())
                {
                    innerInlineResult->NoteFatal(InlineObservation::CALLSITE_COMPILATION_FAILURE);
                }
            }
        }
    },
        &param);
    if (!success)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nInlining failed due to an exception during invoking the compiler for the inlinee method %s.\n",
                   eeGetMethodFullName(fncHandle));
        }
#endif // DEBUG

        // If we haven't yet determined why this inline fails, use
        // a catch-all something bad happened observation.
        if (!inlineResult->IsFailure())
        {
            inlineResult->NoteFatal(InlineObservation::CALLSITE_COMPILATION_ERROR);
        }
    }

    *createdContext = inlineInfo.inlineContext;

    if (inlineResult->IsFailure())
    {
        return;
    }

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nDone invoking compiler for the inlinee method %s\n", eeGetMethodFullName(fncHandle));
    }
#endif // DEBUG

    // If there is non-NULL return, but we haven't set the substExpr,
    // That means we haven't imported any BB that contains CEE_RET opcode.
    // (This could happen for example for a BBJ_THROW block fall through a BBJ_RETURN block which
    // causes the BBJ_RETURN block not to be imported at all.)
    // Fail the inlining attempt
    if ((inlineCandidateInfo->methInfo.args.retType != CORINFO_TYPE_VOID) &&
        (inlineCandidateInfo->result.substExpr == nullptr))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nInlining failed because pInlineInfo->result.substExpr is not set in the inlinee method %s.\n",
                   eeGetMethodFullName(fncHandle));
        }
#endif // DEBUG
        inlineResult->NoteFatal(InlineObservation::CALLEE_LACKS_RETURN);
        return;
    }

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // The inlining attempt cannot be failed starting from this point.
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    // We've successfully obtain the list of inlinee's basic blocks.
    fgFinalizeInlineeStatements(&inlineInfo);

#ifdef DEBUG

    if (verbose)
    {
        printf("Successfully inlined %s (%d IL bytes) (depth %d) [%s]\n", eeGetMethodFullName(fncHandle),
               inlineCandidateInfo->methInfo.ILCodeSize, inlineDepth, inlineResult->ReasonString());
    }

    if (verbose)
    {
        printf("--------------------------------------------------------------------------------------------\n");
    }
#endif // DEBUG

#if defined(DEBUG)
    impInlinedCodeSize += inlineCandidateInfo->methInfo.ILCodeSize;
#endif

    // We inlined...
    inlineResult->NoteSuccess();
}

void StatementListBuilder::Append(Statement* stmt)
{
    if (m_head == nullptr)
    {
        m_head = m_tail = stmt;
        stmt->SetPrevStmt(nullptr);
        stmt->SetNextStmt(nullptr);
        return;
    }

    stmt->SetPrevStmt(m_tail);
    stmt->SetNextStmt(nullptr);
    m_tail->SetNextStmt(stmt);
    m_tail = stmt;
}

void StatementListBuilder::InsertIntoBlockAtBeginning(BasicBlock* block)
{
    if (m_head == nullptr)
    {
        return;
    }

    Statement* lastStmt = block->lastStmt();
    if (lastStmt == nullptr)
    {
        lastStmt = m_tail;
    }

    if (block->bbStmtList != nullptr)
    {
        m_tail->SetNextStmt(block->bbStmtList);
        block->bbStmtList->SetPrevStmt(m_tail);
    }

    block->bbStmtList = m_head;
    m_head->SetPrevStmt(lastStmt);
}

void StatementListBuilder::InsertIntoBlockBefore(BasicBlock* block, Statement* before)
{
    assert(before != nullptr);

    if (before == block->firstStmt())
    {
        InsertIntoBlockAtBeginning(block);
        return;
    }

    if (m_head == nullptr)
    {
        return;
    }

    m_head->SetPrevStmt(before->GetPrevStmt());
    m_tail->SetNextStmt(before);

    m_head->GetPrevStmt()->SetNextStmt(m_head);
    m_tail->GetNextStmt()->SetPrevStmt(m_tail);
}

void Compiler::fgFinalizeInlineeStatements(InlineInfo* pInlineInfo)
{
#ifdef DEBUG

    if (verbose)
    {
        printf("\n\n----------- Statements (and blocks) added due to the inlining of call ");
        printTreeID(pInlineInfo->iciCall);
        printf(" -----------\n");
    }

#endif // DEBUG

    // Mark success.
    pInlineInfo->inlineContext->SetSucceeded(pInlineInfo);

    // Prepend statements
    fgInlinePrependStatements(pInlineInfo);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nInlinee method body:");
    }
#endif // DEBUG

    fgInlineAppendStatements(pInlineInfo->teardownStatements, pInlineInfo);

    //
    // At this point we have the inlinee's code in in inlinee compiler and setup/teardown statements
    // in the InlineInfo instance.
    //

    //
    // Copy out some flags
    //
    compLongUsed |= InlineeCompiler->compLongUsed;
    compFloatingPointUsed |= InlineeCompiler->compFloatingPointUsed;
    compLocallocUsed |= InlineeCompiler->compLocallocUsed;
    compLocallocOptimized |= InlineeCompiler->compLocallocOptimized;
    compQmarkUsed |= InlineeCompiler->compQmarkUsed;
    compGSReorderStackLayout |= InlineeCompiler->compGSReorderStackLayout;
    compHasBackwardJump |= InlineeCompiler->compHasBackwardJump;
    compMaskConvertUsed |= InlineeCompiler->compMaskConvertUsed;

    lvaGenericsContextInUse |= InlineeCompiler->lvaGenericsContextInUse;

#ifdef TARGET_ARM64
    info.compNeedsConsecutiveRegisters |= InlineeCompiler->info.compNeedsConsecutiveRegisters;
#endif

    if (InlineeCompiler->fgHasSwitch)
    {
        fgHasSwitch = true;

        // If the inlinee compiler encounters switch tables, disable hot/cold splitting in the root compiler.
        // TODO-CQ: Implement hot/cold splitting of methods with switch tables.
        if (opts.compProcedureSplitting)
        {
            opts.compProcedureSplitting = false;
            JITDUMP("Turning off procedure splitting for this method, as inlinee compiler encountered switch tables; "
                    "implementation limitation.\n");
        }
    }

#ifdef FEATURE_SIMD
    if (InlineeCompiler->usesSIMDTypes())
    {
        setUsesSIMDTypes(true);
    }
#endif // FEATURE_SIMD

    // Update unmanaged call details
    info.compUnmanagedCallCountWithGCTransition += InlineeCompiler->info.compUnmanagedCallCountWithGCTransition;

    // Update stats for inlinee PGO
    //
    if (InlineeCompiler->fgPgoSchema != nullptr)
    {
        fgPgoInlineePgo++;
    }
    else if (InlineeCompiler->fgPgoFailReason != nullptr)
    {
        // Single block inlinees may not have probes
        // when we've ensabled minimal profiling (which
        // is now the default).
        //
        if (InlineeCompiler->fgBBcount == 1)
        {
            fgPgoInlineeNoPgoSingleBlock++;
        }
        else
        {
            fgPgoInlineeNoPgo++;
        }
    }

    // Update no-return call count
    optNoReturnCallCount += InlineeCompiler->optNoReturnCallCount;

#ifdef DEBUG
    // Update metrics
    Metrics.mergeToRoot(InlineeCompiler);
#endif

    // Update optMethodFlags

#ifdef DEBUG
    unsigned optMethodFlagsBefore = optMethodFlags;
#endif

    optMethodFlags |= InlineeCompiler->optMethodFlags;

#ifdef DEBUG
    if (optMethodFlags != optMethodFlagsBefore)
    {
        JITDUMP("INLINER: Updating optMethodFlags --  root:%0x callee:%0x new:%0x\n", optMethodFlagsBefore,
                InlineeCompiler->optMethodFlags, optMethodFlags);
    }
#endif

    // Update profile consistency
    //
    // If inlinee is inconsistent, root method will be inconsistent too.
    //
    if (!InlineeCompiler->fgPgoConsistent)
    {
        if (fgPgoConsistent)
        {
            JITDUMP("INLINER: profile data in root now inconsistent -- inlinee had inconsistency\n");
            Metrics.ProfileInconsistentInlinee++;
            fgPgoConsistent = false;
        }
    }

    BasicBlock* iciBlock = pInlineInfo->iciBlock;
    // If we inline a no-return call at a site with profile weight,
    // we will introduce inconsistency.
    //
    if (InlineeCompiler->fgReturnCount == 0)
    {
        JITDUMP("INLINER: no-return inlinee\n");

        if (iciBlock->bbWeight > 0)
        {
            if (fgPgoConsistent)
            {
                JITDUMP("INLINER: profile data in root now inconsistent -- no-return inlinee at call site in " FMT_BB
                        " with weight " FMT_WT "\n",
                        iciBlock->bbNum, iciBlock->bbWeight);
                Metrics.ProfileInconsistentNoReturnInlinee++;
                fgPgoConsistent = false;
            }
        }
        else
        {
            // Inlinee scaling should assure this is so.
            //
            assert(InlineeCompiler->fgFirstBB->bbWeight == 0);
        }
    }

    // If the call site is not in a try and the callee has a throw,
    // we may introduce inconsistency.
    //
    if (InlineeCompiler->fgThrowCount > 0)
    {
        JITDUMP("INLINER: may-throw inlinee\n");

        if (iciBlock->bbWeight > 0)
        {
            if (fgPgoConsistent)
            {
                JITDUMP("INLINER: profile data in root now inconsistent -- may-throw inlinee at call site in " FMT_BB
                        " with weight " FMT_WT "\n",
                        iciBlock->bbNum, iciBlock->bbWeight);
                Metrics.ProfileInconsistentMayThrowInlinee++;
                fgPgoConsistent = false;
            }
        }
        else
        {
            // Inlinee scaling should assure this is so.
            //
            assert(InlineeCompiler->fgFirstBB->bbWeight == 0);
        }
    }

    // If an inlinee needs GS cookie we need to make sure that the cookie will not be allocated at zero stack offset.
    // Note that if the root method needs GS cookie then this has already been taken care of.
    if (!getNeedsGSSecurityCookie() && InlineeCompiler->getNeedsGSSecurityCookie())
    {
        setNeedsGSSecurityCookie();
        const unsigned dummy         = lvaGrabTempWithImplicitUse(false DEBUGARG("GSCookie dummy for inlinee"));
        LclVarDsc*     gsCookieDummy = lvaGetDesc(dummy);
        gsCookieDummy->lvType        = TYP_INT;
        gsCookieDummy->lvIsTemp      = true; // It is not alive at all, set the flag to prevent zero-init.
        lvaSetVarDoNotEnregister(dummy DEBUGARG(DoNotEnregisterReason::VMNeedsStackAddr));
    }
}

//------------------------------------------------------------------------
// fgInsertInlineeArgument: wire up the given argument from the callsite with the inlinee
//
// Arguments:
//    argInfo   - information about the argument
//    block     - block to insert the argument into
//    afterStmt - statement to insert the argument after
//    newStmt   - updated with the new statement
//    callDI    - debug info for the call
//
void Compiler::fgInsertInlineeArgument(StatementListBuilder& statements, const InlArgInfo& argInfo, const DebugInfo& callDI)
{
    const bool argIsSingleDef = !argInfo.argHasLdargaOp && !argInfo.argHasStargOp;
    CallArg*   arg            = argInfo.arg;
    GenTree*   argNode        = arg->GetNode();

    if (argInfo.argHasTmp)
    {
        noway_assert(argInfo.argIsUsed);

        /* argBashTmpNode is non-NULL iff the argument's value was
           referenced exactly once by the original IL. This offers an
           opportunity to avoid an intermediate temp and just insert
           the original argument tree.

           However, if the temp node has been cloned somewhere while
           importing (e.g. when handling isinst or dup), or if the IL
           took the address of the argument, then argBashTmpNode will
           be set (because the value was only explicitly retrieved
           once) but the optimization cannot be applied.
         */

        GenTree* argSingleUseNode = argInfo.argBashTmpNode;

        if ((argSingleUseNode != nullptr) && !(argSingleUseNode->gtFlags & GTF_VAR_MOREUSES) && argIsSingleDef)
        {
            // Change the temp in-place to the actual argument.
            // We currently do not support this for struct arguments, so it must not be a GT_BLK.
            assert(!argNode->OperIs(GT_BLK));
            argSingleUseNode->ReplaceWith(argNode, this);
            return;
        }
        else
        {
            // We're going to assign the argument value to the temp we use for it in the inline body.
            GenTree* store = gtNewTempStore(argInfo.argTmpNum, argNode);

            Statement* newStmt = gtNewStmt(store, callDI);
            statements.Append(newStmt);
            DISPSTMT(newStmt);
        }
    }
    else if (argInfo.argIsByRefToStructLocal)
    {
        // Do nothing. Arg was directly substituted as we read
        // the inlinee.
    }
    else
    {
        // The argument is either not used or a const or lcl var
        noway_assert(!argInfo.argIsUsed || argInfo.argIsInvariant || argInfo.argIsLclVar);
        noway_assert((argInfo.argIsLclVar == 0) == (!argNode->OperIs(GT_LCL_VAR) || (argNode->gtFlags & GTF_GLOB_REF)));

        // If the argument has side effects, append it
        if (argInfo.argHasSideEff)
        {
            noway_assert(argInfo.argIsUsed == false);
            Statement* newStmt = nullptr;
            bool append = true;

            if (argNode->OperIs(GT_BLK))
            {
                // Don't put GT_BLK node under a GT_COMMA.
                // Codegen can't deal with it.
                // Just hang the address here in case there are side-effect.
                newStmt = gtNewStmt(gtUnusedValNode(argNode->AsOp()->gtOp1), callDI);
            }
            else
            {
                // In some special cases, unused args with side effects can
                // trigger further changes.
                //
                // (1) If the arg is a static field access and the field access
                // was produced by a call to EqualityComparer<T>.get_Default, the
                // helper call to ensure the field has a value can be suppressed.
                // This helper call is marked as a "Special DCE" helper during
                // importation, over in fgGetStaticsCCtorHelper.
                //
                // (2) NYI. If we find that the actual arg expression
                // has no side effects, we can skip appending all
                // together. This will help jit TP a bit.
                //

                // For case (1)
                //
                // Look for the following tree shapes
                // prejit: (IND (ADD (CONST, CALL(special dce helper...))))
                // jit   : (COMMA (CALL(special dce helper...), (FIELD ...)))
                if (argNode->OperIs(GT_COMMA))
                {
                    // Look for (COMMA (CALL(special dce helper...), (FIELD ...)))
                    GenTree* op1 = argNode->AsOp()->gtOp1;
                    GenTree* op2 = argNode->AsOp()->gtOp2;
                    if (op1->IsCall() && ((op1->AsCall()->gtCallMoreFlags & GTF_CALL_M_HELPER_SPECIAL_DCE) != 0) &&
                        op2->OperIs(GT_IND) && op2->gtGetOp1()->IsIconHandle() && ((op2->gtFlags & GTF_EXCEPT) == 0))
                    {
                        JITDUMP("\nPerforming special dce on unused arg [%06u]:"
                                " actual arg [%06u] helper call [%06u]\n",
                                argNode->gtTreeID, argNode->gtTreeID, op1->gtTreeID);
                        // Drop the whole tree
                        append = false;
                    }
                }
                else if (argNode->OperIs(GT_IND))
                {
                    // Look for (IND (ADD (CONST, CALL(special dce helper...))))
                    GenTree* addr = argNode->AsOp()->gtOp1;

                    if (addr->OperIs(GT_ADD))
                    {
                        GenTree* op1 = addr->AsOp()->gtOp1;
                        GenTree* op2 = addr->AsOp()->gtOp2;
                        if (op1->IsCall() && ((op1->AsCall()->gtCallMoreFlags & GTF_CALL_M_HELPER_SPECIAL_DCE) != 0) &&
                            op2->IsCnsIntOrI())
                        {
                            // Drop the whole tree
                            JITDUMP("\nPerforming special dce on unused arg [%06u]:"
                                    " actual arg [%06u] helper call [%06u]\n",
                                    argNode->gtTreeID, argNode->gtTreeID, op1->gtTreeID);
                            append = false;
                        }
                    }
                }
            }

            if (!append)
            {
                assert(newStmt == nullptr);
                JITDUMP("Arg tree side effects were discardable, not appending anything for arg\n");
            }
            else
            {
                // If we don't have something custom to append,
                // just append the arg node as an unused value.
                if (newStmt == nullptr)
                {
                    newStmt = gtNewStmt(gtUnusedValNode(argNode), callDI);
                }

                statements.Append(newStmt);
                DISPSTMT(newStmt);
            }
        }
        else if (argNode->IsBoxedValue())
        {
            // Try to clean up any unnecessary boxing side effects
            // since the box itself will be ignored.
            gtTryRemoveBoxUpstreamEffects(argNode);
        }
    }
}

//------------------------------------------------------------------------
// fgInlinePrependStatements: prepend statements needed to match up
// caller and inlined callee
//
// Arguments:
//    inlineInfo -- info for the inline
//
// Return Value:
//    The last statement that was added, or the original call if no
//    statements were added.
//
// Notes:
//    Statements prepended may include the following:
//    * This pointer null check
//    * Class initialization
//    * Zeroing of must-init locals in the callee
//    * Passing of call arguments via temps
//
//    Newly added statements are placed just after the original call
//    and are given the same inline context as the call any calls
//    added here will appear to have been part of the immediate caller.
//
void Compiler::fgInlinePrependStatements(InlineInfo* inlineInfo)
{
    BasicBlock* block = inlineInfo->iciBlock;
    const DebugInfo& callDI    = inlineInfo->iciStmt->GetDebugInfo();
    GenTreeCall*     call      = inlineInfo->iciCall->AsCall();

    noway_assert(call->OperIs(GT_CALL));

    // Prepend statements for any initialization / side effects

    InlArgInfo*    inlArgInfo = inlineInfo->inlArgInfo;
    InlLclVarInfo* lclVarInfo = inlineInfo->lclVarInfo;

    GenTree* tree;

    // Create the null check statement (but not appending it to the statement list yet) for the 'this' pointer if
    // necessary.
    // The NULL check should be done after "argument setup statements".
    // The only reason we move it here is for calling "impInlineFetchArg(0,..." to reserve a temp
    // for the "this" pointer.
    // Note: Here we no longer do the optimization that was done by thisDereferencedFirst in the old inliner.
    // However the assetionProp logic will remove any unnecessary null checks that we may have added
    //
    GenTree* nullcheck = nullptr;

    if (call->gtFlags & GTF_CALL_NULLCHECK && !inlineInfo->thisDereferencedFirst)
    {
        // Call impInlineFetchArg to "reserve" a temp for the "this" pointer.
        GenTree* thisOp = impInlineFetchArg(inlArgInfo[0], lclVarInfo[0]);
        if (fgAddrCouldBeNull(thisOp))
        {
            nullcheck = gtNewNullCheck(thisOp);
            // The NULL-check statement will be inserted to the statement list after those statements
            // that assign arguments to temps and before the actual body of the inlinee method.
        }
    }

    // Append the InstParam
    if (inlineInfo->inlInstParamArgInfo != nullptr)
    {
        fgInsertInlineeArgument(inlineInfo->setupStatements, *inlineInfo->inlInstParamArgInfo, callDI);
    }

    if (inlineInfo->inlRetBufferArgInfo != nullptr)
    {
        fgInsertInlineeArgument(inlineInfo->setupStatements, *inlineInfo->inlRetBufferArgInfo, callDI);
    }

    // Treat arguments that had to be assigned to temps
    if (inlineInfo->argCnt)
    {
        JITDUMP("\nArguments setup:\n");
    }
#endif

    unsigned ilArgNum = 0;
    for (CallArg& arg : call->gtArgs.Args())
    {
        InlArgInfo* argInfo = nullptr;
        switch (arg.GetWellKnownArg())
        {
            fgInsertInlineeArgument(inlineInfo->setupStatements, inlArgInfo[argNum], callDI);
        }

        assert(argInfo != nullptr);
        fgInsertInlineeArgument(*argInfo, block, &afterStmt, &newStmt, callDI);
    }

    // Add the CCTOR check if asked for.
    // Note: We no longer do the optimization that is done before by staticAccessedFirstUsingHelper in the old inliner.
    //       Therefore we might prepend redundant call to HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
    //       before the inlined method body, even if a static field of this type was accessed in the inlinee
    //       using a helper before any other observable side-effect.

    if (inlineInfo->inlineCandidateInfo->initClassResult & CORINFO_INITCLASS_USE_HELPER)
    {
        CORINFO_CLASS_HANDLE exactClass = eeGetClassFromContext(inlineInfo->inlineCandidateInfo->exactContextHandle);

        tree    = fgGetSharedCCtor(exactClass);
        Statement* newStmt = gtNewStmt(tree, callDI);
        inlineInfo->setupStatements.Append(newStmt);
    }

    // Insert the nullcheck statement now.
    if (nullcheck)
    {
        Statement* newStmt = gtNewStmt(nullcheck, callDI);
        inlineInfo->setupStatements.Append(newStmt);
    }

    //
    // Now zero-init inlinee locals
    //

    CORINFO_METHOD_INFO* InlineeMethodInfo = InlineeCompiler->info.compMethodInfo;

    unsigned lclCnt     = InlineeMethodInfo->locals.numArgs;
    bool     bbInALoop  = block->HasFlag(BBF_BACKWARD_JUMP);
    bool     bbIsReturn = block->KindIs(BBJ_RETURN);

    // If the callee contains zero-init locals, we need to explicitly initialize them if we are
    // in a loop or if the caller doesn't have compInitMem set. Otherwise we can rely on the
    // normal logic in the caller to insert zero-init in the prolog if necessary.
    if ((lclCnt != 0) && ((InlineeMethodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0) &&
        ((bbInALoop && !bbIsReturn) || !info.compInitMem))
    {

#ifdef DEBUG
        if (verbose)
        {
            printf("\nZero init inlinee locals:\n");
        }
#endif // DEBUG

        for (unsigned lclNum = 0; lclNum < lclCnt; lclNum++)
        {
            unsigned tmpNum = inlineInfo->lclTmpNum[lclNum];

            // If the local is used check whether we need to insert explicit zero initialization.
            if (tmpNum != BAD_VAR_NUM)
            {
                LclVarDsc* const tmpDsc = lvaGetDesc(tmpNum);
                if (!fgVarNeedsExplicitZeroInit(tmpNum, bbInALoop, bbIsReturn))
                {
                    JITDUMP("\nSuppressing zero-init for V%02u -- expect to zero in prolog\n", tmpNum);
                    tmpDsc->lvSuppressedZeroInit = 1;
                    compSuppressedZeroInit       = true;
                    continue;
                }

                var_types lclTyp = tmpDsc->TypeGet();
                noway_assert(lclTyp == lclVarInfo[lclNum + inlineInfo->argCnt].lclTypeInfo);

                tree = gtNewTempStore(tmpNum, (lclTyp == TYP_STRUCT) ? gtNewIconNode(0) : gtNewZeroConNode(lclTyp));

                Statement* newStmt = gtNewStmt(tree, callDI);
                inlineInfo->setupStatements.Append(newStmt);

                DISPSTMT(newStmt);
            }
        }
    }
}

//------------------------------------------------------------------------
// fgInlineAppendStatements: Append statements that are needed
// after the inlined call.
//
// Arguments:
//    inlineInfo - information about the inline
//    block      - basic block for the new statements
//    stmtAfter  - (optional) insertion point for mid-block cases
//
// Notes:
//    If the call we're inlining is in tail position then
//    we skip nulling the locals, since it can interfere
//    with tail calls introduced by the local.
//
void Compiler::fgInlineAppendStatements(InlineInfo* inlineInfo, BasicBlock* block, Statement* stmtAfter)
{
    // Null out any gc ref locals
    if (!inlineInfo->HasGcRefLocals())
    {
        // No ref locals, nothing to do.
        JITDUMP("fgInlineAppendStatements: no gc ref inline locals.\n");
        return;
    }

    if (inlineInfo->iciCall->IsImplicitTailCall())
    {
        JITDUMP("fgInlineAppendStatements: implicit tail call; skipping nulling.\n");
        return;
    }

    JITDUMP("fgInlineAppendStatements: nulling out gc ref inlinee locals.\n");

    Statement*           callStmt          = inlineInfo->iciStmt;
    const DebugInfo&     callDI            = callStmt->GetDebugInfo();
    CORINFO_METHOD_INFO* InlineeMethodInfo = InlineeCompiler->info.compMethodInfo;
    const unsigned       lclCnt            = InlineeMethodInfo->locals.numArgs;
    InlLclVarInfo*       lclVarInfo        = inlineInfo->lclVarInfo;
    unsigned             gcRefLclCnt       = inlineInfo->numberOfGcRefLocals;
    const unsigned       argCnt            = inlineInfo->argCnt;
    InlineCandidateInfo* inlCandInfo       = inlineInfo->inlineCandidateInfo;

    for (unsigned lclNum = 0; lclNum < lclCnt; lclNum++)
    {
        // Is the local a gc ref type? Need to look at the
        // inline info for this since we will not have local
        // temps for unused inlinee locals.
        const var_types lclTyp = lclVarInfo[argCnt + lclNum].lclTypeInfo;

        if (!varTypeIsGC(lclTyp))
        {
            // Nope, nothing to null out.
            continue;
        }

        // Ensure we're examining just the right number of locals.
        assert(gcRefLclCnt > 0);
        gcRefLclCnt--;

        // Fetch the temp for this inline local
        const unsigned tmpNum = inlineInfo->lclTmpNum[lclNum];

        // Is the local used at all?
        if (tmpNum == BAD_VAR_NUM)
        {
            // Nope, nothing to null out.
            continue;
        }

        // Local was used, make sure the type is consistent.
        assert(lvaTable[tmpNum].lvType == lclTyp);

        // Does the local we're about to null out appear in the return
        // expression?  If so we somehow messed up and didn't properly
        // spill the return value. See impInlineFetchLocal.
        if (inlCandInfo->result.substExpr != nullptr)
        {
            const bool interferesWithReturn = gtHasRef(inlCandInfo->result.substExpr, tmpNum);
            noway_assert(!interferesWithReturn);
        }

        // Assign null to the local.
        GenTree*   nullExpr = gtNewTempStore(tmpNum, gtNewZeroConNode(lclTyp));
        Statement* nullStmt = gtNewStmt(nullExpr, callDI);

        statements.Append(nullStmt);

#ifdef DEBUG
        if (verbose)
        {
            gtDispStmt(nullStmt);
        }
#endif // DEBUG
    }

    // There should not be any GC ref locals left to null out.
    assert(gcRefLclCnt == 0);
}

//------------------------------------------------------------------------
// fgNeedReturnSpillTemp: Answers does the inlinee need to spill all returns
//  as a temp.
//
// Return Value:
//    true if the inlinee has to spill return exprs.
bool Compiler::fgNeedReturnSpillTemp()
{
    assert(compIsForInlining());
    return (lvaInlineeReturnSpillTemp != BAD_VAR_NUM);
}
