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
        (ancestor->GetRuntimeContext() == inlineInfo->inlineCandidateInfo->exactContextHnd))
    {
        JITDUMP("Call site is trivially recursive\n");
        return true;
    }

    // None of the inline heuristics take into account that inlining will cause
    // type/method loading for generic contexts. When polymorphic recursion is
    // involved this can quickly consume a large amount of resources, so try to
    // verify that we aren't inlining recursively with complex contexts.
    if (info.compCompHnd->haveSameMethodDefinition(inlineInfo->fncHandle, ancestor->GetCallee()) &&
        ContextComplexityExceeds(inlineInfo->inlineCandidateInfo->exactContextHnd, 64))
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

class SubstitutePlaceholdersAndDevirtualizeWalker : public GenTreeVisitor<SubstitutePlaceholdersAndDevirtualizeWalker>
{
    bool m_madeChanges = false;

public:
    enum
    {
        DoPreOrder        = true,
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    SubstitutePlaceholdersAndDevirtualizeWalker(Compiler* comp) : GenTreeVisitor(comp)
    {
    }

    bool MadeChanges()
    {
        return m_madeChanges;
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        // All the operations here and in the corresponding postorder
        // callback (LateDevirtualization) are triggered by GT_CALL or
        // GT_RET_EXPR trees, and these (should) have the call side
        // effect flag.
        //
        // So bail out for any trees that don't have this flag.
        if ((tree->gtFlags & GTF_CALL) == 0)
        {
            return fgWalkResult::WALK_SKIP_SUBTREES;
        }

        if (tree->OperIs(GT_RET_EXPR))
        {
            UpdateInlineReturnExpressionPlaceHolder(use, user);
        }

#if FEATURE_MULTIREG_RET
#if defined(DEBUG)

        // Make sure we don't have a tree like so: V05 = (, , , retExpr);
        // Since we only look one level above for the parent for '=' and
        // do not check if there is a series of COMMAs. See above.
        // Importer and FlowGraph will not generate such a tree, so just
        // leaving an assert in here. This can be fixed by looking ahead
        // when we visit stores similar to AttachStructInlineeToStore.
        //
        if (tree->OperIsStore())
        {
            GenTree* value = tree->Data();

            if (value->OperGet() == GT_COMMA)
            {
                GenTree* effectiveValue = value->gtEffectiveVal();

                noway_assert(
                    !varTypeIsStruct(effectiveValue) || (effectiveValue->OperGet() != GT_RET_EXPR) ||
                    !m_compiler->IsMultiRegReturnedType(effectiveValue->AsRetExpr()->gtInlineCandidate->gtRetClsHnd,
                                                        CorInfoCallConvExtension::Managed));
            }
        }

#endif // defined(DEBUG)
#endif // FEATURE_MULTIREG_RET
        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        LateDevirtualization(use, user);
        return fgWalkResult::WALK_CONTINUE;
    }

private:
    //------------------------------------------------------------------------
    // UpdateInlineReturnExpressionPlaceHolder: replace an
    // inline return expression placeholder if there is one.
    //
    // Arguments:
    //    use -- edge for the tree that is a GT_RET_EXPR node
    //    parent -- node containing the edge
    //
    // Returns:
    //    fgWalkResult indicating the walk should continue; that
    //    is we wish to fully explore the tree.
    //
    // Notes:
    //    Looks for GT_RET_EXPR nodes that arose from tree splitting done
    //    during importation for inline candidates, and replaces them.
    //
    //    For successful inlines, substitutes the return value expression
    //    from the inline body for the GT_RET_EXPR.
    //
    //    For failed inlines, rejoins the original call into the tree from
    //    whence it was split during importation.
    //
    //    The code doesn't actually know if the corresponding inline
    //    succeeded or not; it relies on the fact that gtInlineCandidate
    //    initially points back at the call and is modified in place to
    //    the inlinee return expression if the inline is successful (see
    //    tail end of fgInsertInlineeBlocks for the update of iciCall).
    //
    //    If the return type is a struct type and we're on a platform
    //    where structs can be returned in multiple registers, ensure the
    //    call has a suitable parent.
    //
    //    If the original call type and the substitution type are different
    //    the functions makes necessary updates. It could happen if there was
    //    an implicit conversion in the inlinee body.
    //
    void UpdateInlineReturnExpressionPlaceHolder(GenTree** use, GenTree* parent)
    {
        CORINFO_CLASS_HANDLE retClsHnd = NO_CLASS_HANDLE;

        while ((*use)->OperIs(GT_RET_EXPR))
        {
            GenTree* tree = *use;
            // We are going to copy the tree from the inlinee,
            // so record the handle now.
            //
            if (varTypeIsStruct(tree))
            {
                retClsHnd = tree->AsRetExpr()->gtInlineCandidate->gtRetClsHnd;
            }

            // Skip through chains of GT_RET_EXPRs (say from nested inlines)
            // to the actual tree to use.
            //
            BasicBlock* inlineeBB       = nullptr;
            GenTree*    inlineCandidate = tree;
            do
            {
                GenTreeRetExpr* retExpr = inlineCandidate->AsRetExpr();
                inlineCandidate         = retExpr->gtSubstExpr;
                inlineeBB               = retExpr->gtSubstBB;
            } while (inlineCandidate->OperIs(GT_RET_EXPR));

            // We might as well try and fold the return value. Eg returns of
            // constant bools will have CASTS. This folding may uncover more
            // GT_RET_EXPRs, so we loop around until we've got something distinct.
            //
            inlineCandidate   = m_compiler->gtFoldExpr(inlineCandidate);
            var_types retType = tree->TypeGet();

#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("\nReplacing the return expression placeholder ");
                Compiler::printTreeID(tree);
                printf(" with ");
                Compiler::printTreeID(inlineCandidate);
                printf("\n");
                // Dump out the old return expression placeholder it will be overwritten by the ReplaceWith below
                m_compiler->gtDispTree(tree);
            }
#endif // DEBUG

            var_types newType = inlineCandidate->TypeGet();

            // If we end up swapping type we may need to retype the tree:
            if (retType != newType)
            {
                if ((retType == TYP_BYREF) && (tree->OperGet() == GT_IND))
                {
                    // - in an RVA static if we've reinterpreted it as a byref;
                    assert(newType == TYP_I_IMPL);
                    JITDUMP("Updating type of the return GT_IND expression to TYP_BYREF\n");
                    inlineCandidate->gtType = TYP_BYREF;
                }
            }

            *use          = inlineCandidate;
            m_madeChanges = true;

            if (inlineeBB != nullptr)
            {
                // IR may potentially contain nodes that requires mandatory BB flags to be set.
                // Propagate those flags from the containing BB.
                m_compiler->compCurBB->CopyFlags(inlineeBB, BBF_COPY_PROPAGATE);
            }

#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("\nInserting the inline return expression\n");
                m_compiler->gtDispTree(inlineCandidate);
                printf("\n");
            }
#endif // DEBUG
        }

        // If an inline was rejected and the call returns a struct, we may
        // have deferred some work when importing call for cases where the
        // struct is returned in multiple registers.
        //
        // See the bail-out clauses in impFixupCallStructReturn for inline
        // candidates.
        //
        // Do the deferred work now.
        if (retClsHnd != NO_CLASS_HANDLE)
        {
            Compiler::structPassingKind howToReturnStruct;
            var_types                   returnType =
                m_compiler->getReturnTypeForStruct(retClsHnd, CorInfoCallConvExtension::Managed, &howToReturnStruct);

            switch (howToReturnStruct)
            {
#if FEATURE_MULTIREG_RET
                // Force multi-reg nodes into the "lcl = node()" form if necessary.
                // TODO-ASG: this code could be improved substantially. There is no need
                // to introduce temps if the inlinee is not actually a multi-reg node.
                //
                case Compiler::SPK_ByValue:
                case Compiler::SPK_ByValueAsHfa:
                {
                    // See assert below, we only look one level above for a store parent.
                    if (parent->OperIsStore())
                    {
                        // The inlinee can only be the value.
                        assert(parent->Data() == *use);
                        AttachStructInlineeToStore(parent, retClsHnd);
                    }
                    else
                    {
                        // Just store the inlinee to a variable to keep it simple.
                        *use = StoreStructInlineeToVar(*use, retClsHnd);
                    }
                    m_madeChanges = true;
                }
                break;

#endif // FEATURE_MULTIREG_RET

                case Compiler::SPK_EnclosingType:
                case Compiler::SPK_PrimitiveType:
                    // No work needs to be done, the call has struct type and should keep it.
                    break;

                case Compiler::SPK_ByReference:
                    // We should have already added the return buffer
                    // when we first imported the call
                    break;

                default:
                    noway_assert(!"Unexpected struct passing kind");
                    break;
            }
        }
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

        // We need to force all assignments from multi-reg nodes into the "lcl = node()" form.
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
        assert(!inlinee->OperIs(GT_MKREFANY, GT_RET_EXPR));

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
            assert((parent != nullptr) && parent->OperGet() == GT_NOP);
            return;
        }

        if (tree->OperGet() == GT_CALL)
        {
            GenTreeCall* call          = tree->AsCall();
            bool         tryLateDevirt = call->IsVirtual() && (call->gtCallType == CT_USER_FUNC);

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

                CORINFO_CONTEXT_HANDLE context                = nullptr;
                CORINFO_METHOD_HANDLE  method                 = call->gtCallMethHnd;
                unsigned               methodFlags            = 0;
                const bool             isLateDevirtualization = true;
                const bool             explicitTailCall       = call->IsTailPrefixedCall();

                if ((call->gtCallMoreFlags & GTF_CALL_M_HAS_LATE_DEVIRT_INFO) != 0)
                {
                    context = call->gtLateDevirtualizationInfo->exactContextHnd;
                    // Note: we might call this multiple times for the same trees.
                    // If the devirtualization below succeeds, the call becomes
                    // non-virtual and we won't get here again. If it does not
                    // succeed we might get here again so we keep the late devirt
                    // info.
                }

                CORINFO_CONTEXT_HANDLE contextInput = context;
                m_compiler->impDevirtualizeCall(call, nullptr, &method, &methodFlags, &contextInput, &context,
                                                isLateDevirtualization, explicitTailCall);
                m_madeChanges = true;
            }
        }
        else if (tree->OperIs(GT_STORE_LCL_VAR))
        {
            const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
            GenTree* const value  = tree->AsLclVarCommon()->Data();

            // If we're storing to a ref typed local that has one definition,
            // we may be able to sharpen the type for the local.
            if (tree->TypeGet() == TYP_REF)
            {
                LclVarDsc* lcl = m_compiler->lvaGetDesc(lclNum);

                if (lcl->lvSingleDef)
                {
                    bool                 isExact   = false;
                    bool                 isNonNull = false;
                    CORINFO_CLASS_HANDLE newClass  = m_compiler->gtGetClassHandle(value, &isExact, &isNonNull);

                    if (newClass != NO_CLASS_HANDLE)
                    {
                        m_compiler->lvaUpdateClass(lclNum, newClass, isExact);
                        m_madeChanges = true;
                    }
                }
            }

            // If we created a self-store (say because we are sharing return spill temps) we can remove it.
            //
            if (value->OperIs(GT_LCL_VAR) && (value->AsLclVar()->GetLclNum() == lclNum))
            {
                JITDUMP("... removing self-assignment\n");
                DISPTREE(tree);
                tree->gtBashToNOP();
                m_madeChanges = true;
            }
        }
        else if (tree->OperGet() == GT_JTRUE)
        {
            // See if this jtrue is now foldable.
            BasicBlock* block    = m_compiler->compCurBB;
            GenTree*    condTree = tree->AsOp()->gtOp1;
            assert(tree == block->lastStmt()->GetRootNode());

            if (condTree->OperGet() == GT_CNS_INT)
            {
                JITDUMP(" ... found foldable jtrue at [%06u] in " FMT_BB "\n", m_compiler->dspTreeID(tree),
                        block->bbNum);

                // We have a constant operand, and should have the all clear to optimize.
                // Update side effects on the tree, assert there aren't any, and bash to nop.
                m_compiler->gtUpdateNodeSideEffects(tree);
                assert((tree->gtFlags & GTF_SIDE_EFFECT) == 0);
                tree->gtBashToNOP();
                m_madeChanges = true;

                if (condTree->IsIntegralConst(0))
                {
                    m_compiler->fgRemoveRefPred(block->GetTrueEdge());
                    block->SetKindAndTargetEdge(BBJ_ALWAYS, block->GetFalseEdge());
                    block->SetFlags(BBF_NONE_QUIRK);
                }
                else
                {
                    m_compiler->fgRemoveRefPred(block->GetFalseEdge());
                    block->SetKind(BBJ_ALWAYS);
                }
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

    noway_assert(fgFirstBB != nullptr);

    BasicBlock*                                 block = fgFirstBB;
    SubstitutePlaceholdersAndDevirtualizeWalker walker(this);
    bool                                        madeChanges = false;

    do
    {
        // Make the current basic block address available globally
        compCurBB = block;

        for (Statement* const stmt : block->Statements())
        {

#if defined(DEBUG)
            // In debug builds we want the inline tree to show all failed
            // inlines. Some inlines may fail very early and never make it to
            // candidate stage. So scan the tree looking for those early failures.
            fgWalkTreePre(stmt->GetRootNodePointer(), fgFindNonInlineCandidate, stmt);
#endif
            // See if we need to replace some return value place holders.
            // Also, see if this replacement enables further devirtualization.
            //
            // Note we are doing both preorder and postorder work in this walker.
            //
            // The preorder callback is responsible for replacing GT_RET_EXPRs
            // with the appropriate expansion (call or inline result).
            // Replacement may introduce subtrees with GT_RET_EXPR and so
            // we rely on the preorder to recursively process those as well.
            //
            // On the way back up, the postorder callback then re-examines nodes for
            // possible further optimization, as the (now complete) GT_RET_EXPR
            // replacement may have enabled optimizations by providing more
            // specific types for trees or variables.
            walker.WalkTree(stmt->GetRootNodePointer(), nullptr);

            GenTree* expr = stmt->GetRootNode();

            // The importer ensures that all inline candidates are
            // statement expressions.  So see if we have a call.
            if (expr->IsCall())
            {
                GenTreeCall* call = expr->AsCall();

                // We do. Is it an inline candidate?
                //
                // Note we also process GuardeDevirtualizationCandidates here as we've
                // split off GT_RET_EXPRs for them even when they are not inline candidates
                // as we need similar processing to ensure they get patched back to where
                // they belong.
                if (call->IsInlineCandidate() || call->IsGuardedDevirtualizationCandidate())
                {
                    InlineResult inlineResult(this, call, stmt, "fgInline");

                    fgMorphStmt = stmt;

                    fgMorphCallInline(call, &inlineResult);

                    // If there's a candidate to process, we will make changes
                    madeChanges = true;

                    // fgMorphCallInline may have updated the
                    // statement expression to a GT_NOP if the
                    // call returned a value, regardless of
                    // whether the inline succeeded or failed.
                    //
                    // If so, remove the GT_NOP and continue
                    // on with the next statement.
                    if (stmt->GetRootNode()->IsNothingNode())
                    {
                        fgRemoveStmt(block, stmt);
                        continue;
                    }
                }
            }

            // See if stmt is of the form GT_COMMA(call, nop)
            // If yes, we can get rid of GT_COMMA.
            if (expr->OperGet() == GT_COMMA && expr->AsOp()->gtOp1->OperGet() == GT_CALL &&
                expr->AsOp()->gtOp2->OperGet() == GT_NOP)
            {
                madeChanges = true;
                stmt->SetRootNode(expr->AsOp()->gtOp1);
            }
        }

        block = block->Next();

    } while (block);

    madeChanges |= walker.MadeChanges();

#ifdef DEBUG

    // Check that we should not have any inline candidate or return value place holder left.

    block = fgFirstBB;
    noway_assert(block);

    do
    {
        for (Statement* const stmt : block->Statements())
        {
            // Call Compiler::fgDebugCheckInlineCandidates on each node
            fgWalkTreePre(stmt->GetRootNodePointer(), fgDebugCheckInlineCandidates);
        }

        block = block->Next();

    } while (block);

    fgVerifyHandlerTab();

    if (verbose || fgPrintInlinedMethods)
    {
        JITDUMP("**************** Inline Tree");
        printf("\n");
        m_inlineStrategy->Dump(verbose || JitConfig.JitPrintInlinedMethodsVerbose());
    }

#endif // DEBUG

    if (madeChanges)
    {
        // Optional quirk to keep this as zero diff. Some downstream phases are bbNum sensitive
        // but rely on the ambient bbNums.
        //
        fgRenumberBlocks();
    }

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
void Compiler::fgMorphCallInline(GenTreeCall* call, InlineResult* inlineResult)
{
    bool inliningFailed = false;

    InlineCandidateInfo* inlCandInfo = call->GetSingleInlineCandidateInfo();

    // Is this call an inline candidate?
    if (call->IsInlineCandidate())
    {
        InlineContext* createdContext = nullptr;
        // Attempt the inline
        fgMorphCallInlineHelper(call, inlineResult, &createdContext);

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
            // inlining all candidates.
            //
            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
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
            JITDUMP("Inlining [%06u] failed, so bashing " FMT_STMT " to NOP\n", dspTreeID(call), fgMorphStmt->GetID());

            // Detach the GT_CALL tree from the original statement by
            // hanging a "nothing" node to it. Later the "nothing" node will be removed
            // and the original GT_CALL tree will be picked up by the GT_RET_EXPR node.
            inlCandInfo->retExpr->gtSubstExpr = call;
            inlCandInfo->retExpr->gtSubstBB   = compCurBB;

            noway_assert(fgMorphStmt->GetRootNode() == call);
            fgMorphStmt->SetRootNode(gtNewNothingNode());
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
void Compiler::fgMorphCallInlineHelper(GenTreeCall* call, InlineResult* result, InlineContext** createdContext)
{
    // Don't expect any surprises here.
    assert(result->IsCandidate());

    if (lvaCount >= MAX_LV_NUM_COUNT_FOR_INLINING)
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

    fgInvokeInlineeCompiler(call, result, createdContext);

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
    if (tree->gtOper == GT_CALL)
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
    if (call->IsInlineCandidate() || call->IsGuardedDevirtualizationCandidate())
    {
        return;
    }

    InlineResult      inlineResult(this, call, nullptr, "fgNoteNonInlineCandidate", false);
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
    if (tree->gtOper == GT_CALL)
    {
        assert((tree->gtFlags & GTF_CALL_INLINE_CANDIDATE) == 0);
    }
    else
    {
        assert(tree->gtOper != GT_RET_EXPR);
    }

    return WALK_CONTINUE;
}

#endif // DEBUG

void Compiler::fgInvokeInlineeCompiler(GenTreeCall* call, InlineResult* inlineResult, InlineContext** createdContext)
{
    noway_assert(call->gtOper == GT_CALL);
    noway_assert(call->IsInlineCandidate());
    noway_assert(opts.OptEnabled(CLFLG_INLINING));

    // This is the InlineInfo struct representing a method to be inlined.
    InlineInfo            inlineInfo{};
    CORINFO_METHOD_HANDLE fncHandle = call->gtCallMethHnd;

    inlineInfo.fncHandle              = fncHandle;
    inlineInfo.iciCall                = call;
    inlineInfo.iciStmt                = fgMorphStmt;
    inlineInfo.iciBlock               = compCurBB;
    inlineInfo.thisDereferencedFirst  = false;
    inlineInfo.retExprClassHnd        = nullptr;
    inlineInfo.retExprClassHndIsExact = false;
    inlineInfo.inlineResult           = inlineResult;
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
                        ->NewContext(pParam->inlineInfo->inlineCandidateInfo->inlinersContext,
                                     pParam->inlineInfo->iciStmt, pParam->inlineInfo->iciCall);
                pParam->inlineInfo->argCnt                   = pParam->inlineCandidateInfo->methInfo.args.totalILArgs();
                pParam->inlineInfo->tokenLookupContextHandle = pParam->inlineCandidateInfo->exactContextHnd;

                JITLOG_THIS(pParam->pThis,
                            (LL_INFO100000, "INLINER: inlineInfo.tokenLookupContextHandle for %s set to 0x%p:\n",
                             pParam->pThis->eeGetMethodFullName(pParam->fncHandle),
                             pParam->pThis->dspPtr(pParam->inlineInfo->tokenLookupContextHandle)));

                JitFlags compileFlagsForInlinee = *pParam->pThis->opts.jitFlags;

                // The following flags are lost when inlining.
                // (This is checked in Compiler::compInitOptions().)
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_BBINSTR);
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_BBINSTR_IF_LOOPS);
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
    if ((inlineCandidateInfo->fncRetType != TYP_VOID) && (inlineCandidateInfo->retExpr->gtSubstExpr == nullptr))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nInlining failed because pInlineInfo->retExpr is not set in the inlinee method %s.\n",
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
    // Let's insert it to inliner's basic block list.
    fgInsertInlineeBlocks(&inlineInfo);

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

//------------------------------------------------------------------------
// fgInsertInlineeBlocks: incorporate statements for an inline into the
// root method.
//
// Arguments:
//    inlineInfo -- info for the inline
//
// Notes:
//    The inlining attempt cannot be failed once this method is called.
//
//    Adds all inlinee statements, plus any glue statements needed
//    either before or after the inlined call.
//
//    Updates flow graph and assigns weights to inlinee
//    blocks. Currently does not attempt to read IBC data for the
//    inlinee.
//
//    Updates relevant root method status flags (eg optMethodFlags) to
//    include information from the inlinee.
//
//    Marks newly added statements with an appropriate inline context.

void Compiler::fgInsertInlineeBlocks(InlineInfo* pInlineInfo)
{
    GenTreeCall* iciCall  = pInlineInfo->iciCall;
    Statement*   iciStmt  = pInlineInfo->iciStmt;
    BasicBlock*  iciBlock = pInlineInfo->iciBlock;

    noway_assert(iciBlock->bbStmtList != nullptr);
    noway_assert(iciStmt->GetRootNode() != nullptr);
    assert(iciStmt->GetRootNode() == iciCall);
    noway_assert(iciCall->gtOper == GT_CALL);

#ifdef DEBUG

    Statement* currentDumpStmt = nullptr;

    if (verbose)
    {
        printf("\n\n----------- Statements (and blocks) added due to the inlining of call ");
        printTreeID(iciCall);
        printf(" -----------\n");
    }

#endif // DEBUG

    // Mark success.
    pInlineInfo->inlineContext->SetSucceeded(pInlineInfo);

    // Prepend statements
    Statement* stmtAfter = fgInlinePrependStatements(pInlineInfo);

#ifdef DEBUG
    if (verbose)
    {
        currentDumpStmt = stmtAfter;
        printf("\nInlinee method body:");
    }
#endif // DEBUG

    BasicBlock* topBlock            = iciBlock;
    BasicBlock* bottomBlock         = nullptr;
    bool        insertInlineeBlocks = true;

    if (InlineeCompiler->fgBBcount == 1)
    {
        // When fgBBCount is 1 we will always have a non-NULL fgFirstBB
        //
        PREFAST_ASSUME(InlineeCompiler->fgFirstBB != nullptr);

        // DDB 91389: Don't throw away the (only) inlinee block
        // when its return type is not BBJ_RETURN.
        // In other words, we need its BBJ_ to perform the right thing.
        if (InlineeCompiler->fgFirstBB->KindIs(BBJ_RETURN))
        {
            // Inlinee contains just one BB. So just insert its statement list to topBlock.
            if (InlineeCompiler->fgFirstBB->bbStmtList != nullptr)
            {
                stmtAfter = fgInsertStmtListAfter(iciBlock, stmtAfter, InlineeCompiler->fgFirstBB->firstStmt());
            }

            // Copy inlinee bbFlags to caller bbFlags.
            const BasicBlockFlags inlineeBlockFlags = InlineeCompiler->fgFirstBB->GetFlagsRaw();
            noway_assert((inlineeBlockFlags & BBF_HAS_JMP) == 0);
            noway_assert((inlineeBlockFlags & BBF_KEEP_BBJ_ALWAYS) == 0);

            // Todo: we may want to exclude other flags here.
            iciBlock->SetFlags(inlineeBlockFlags & ~BBF_RUN_RARELY);

#ifdef DEBUG
            if (verbose)
            {
                noway_assert(currentDumpStmt);

                if (currentDumpStmt != stmtAfter)
                {
                    do
                    {
                        currentDumpStmt = currentDumpStmt->GetNextStmt();

                        printf("\n");

                        gtDispStmt(currentDumpStmt);
                        printf("\n");

                    } while (currentDumpStmt != stmtAfter);
                }
            }
#endif // DEBUG

            // Append statements to null out gc ref locals, if necessary.
            fgInlineAppendStatements(pInlineInfo, iciBlock, stmtAfter);
            insertInlineeBlocks = false;
        }
    }

    //
    // ======= Inserting inlinee's basic blocks ===============
    //
    if (insertInlineeBlocks)
    {
        bottomBlock              = fgSplitBlockAfterStatement(topBlock, stmtAfter);
        unsigned const baseBBNum = fgBBNumMax;

        // The newly split block is not special so doesn't need to be kept.
        //
        bottomBlock->RemoveFlags(BBF_DONT_REMOVE);

        // Set the try and handler index and fix the jump types of inlinee's blocks.
        //
        for (BasicBlock* const block : InlineeCompiler->Blocks())
        {
            noway_assert(!block->hasTryIndex());
            noway_assert(!block->hasHndIndex());
            block->copyEHRegion(iciBlock);
            block->CopyFlags(iciBlock, BBF_BACKWARD_JUMP);

            // Update block nums appropriately
            //
            block->bbNum += baseBBNum;
            fgBBNumMax = max(block->bbNum, fgBBNumMax);

            DebugInfo di = iciStmt->GetDebugInfo().GetRoot();
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
                JITDUMP("\nConvert bbKind of " FMT_BB " to BBJ_ALWAYS to bottomBlock " FMT_BB "\n", block->bbNum,
                        bottomBlock->bbNum);

                FlowEdge* const newEdge = fgAddRefPred(bottomBlock, block);
                newEdge->setLikelihood(1.0);
                block->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);

                if (block == InlineeCompiler->fgLastBB)
                {
                    block->SetFlags(BBF_NONE_QUIRK);
                }
            }
        }

        // Inlinee's top block will have an artificial ref count. Remove.
        assert(InlineeCompiler->fgFirstBB->bbRefs > 0);
        InlineeCompiler->fgFirstBB->bbRefs--;

        // Insert inlinee's blocks into inliner's block list.
        assert(topBlock->KindIs(BBJ_ALWAYS));
        assert(topBlock->TargetIs(bottomBlock));
        FlowEdge* const oldEdge = fgRemoveRefPred(bottomBlock, topBlock);
        FlowEdge* const newEdge = fgAddRefPred(InlineeCompiler->fgFirstBB, topBlock, oldEdge);

        topBlock->SetNext(InlineeCompiler->fgFirstBB);
        topBlock->SetTargetEdge(newEdge);
        topBlock->SetFlags(BBF_NONE_QUIRK);
        InlineeCompiler->fgLastBB->SetNext(bottomBlock);

        //
        // Add inlinee's block count to inliner's.
        //
        fgBBcount += InlineeCompiler->fgBBcount;

        // Append statements to null out gc ref locals, if necessary.
        fgInlineAppendStatements(pInlineInfo, bottomBlock, nullptr);
        JITDUMPEXEC(fgDispBasicBlocks(InlineeCompiler->fgFirstBB, InlineeCompiler->fgLastBB, true));
    }

    //
    // At this point, we have successully inserted inlinee's code.
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

    lvaGenericsContextInUse |= InlineeCompiler->lvaGenericsContextInUse;

#ifdef TARGET_ARM64
    info.compNeedsConsecutiveRegisters |= InlineeCompiler->info.compNeedsConsecutiveRegisters;
#endif

    // If the inlinee compiler encounters switch tables, disable hot/cold splitting in the root compiler.
    // TODO-CQ: Implement hot/cold splitting of methods with switch tables.
    if (InlineeCompiler->fgHasSwitch && opts.compProcedureSplitting)
    {
        opts.compProcedureSplitting = false;
        JITDUMP("Turning off procedure splitting for this method, as inlinee compiler encountered switch tables; "
                "implementation limitation.\n");
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

    // Update optMethodFlags
    CLANG_FORMAT_COMMENT_ANCHOR;

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

    //
    // Detach the GT_CALL node from the original statement by hanging a "nothing" node under it,
    // so that fgMorphStmts can remove the statement once we return from here.
    //
    iciStmt->SetRootNode(gtNewNothingNode());
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
//    and are are given the same inline context as the call any calls
//    added here will appear to have been part of the immediate caller.

Statement* Compiler::fgInlinePrependStatements(InlineInfo* inlineInfo)
{
    BasicBlock*      block     = inlineInfo->iciBlock;
    Statement*       callStmt  = inlineInfo->iciStmt;
    const DebugInfo& callDI    = callStmt->GetDebugInfo();
    Statement*       postStmt  = callStmt->GetNextStmt();
    Statement*       afterStmt = callStmt; // afterStmt is the place where the new statements should be inserted after.
    Statement*       newStmt   = nullptr;
    GenTreeCall*     call      = inlineInfo->iciCall->AsCall();

    noway_assert(call->gtOper == GT_CALL);

#ifdef DEBUG
    if (0 && verbose)
    {
        printf("\nfgInlinePrependStatements for iciCall= ");
        printTreeID(call);
        printf(":\n");
    }
#endif

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
        GenTree* thisOp = impInlineFetchArg(0, inlArgInfo, lclVarInfo);
        if (fgAddrCouldBeNull(thisOp))
        {
            nullcheck = gtNewNullCheck(thisOp, block);
            // The NULL-check statement will be inserted to the statement list after those statements
            // that assign arguments to temps and before the actual body of the inlinee method.
        }
    }

    /* Treat arguments that had to be assigned to temps */
    if (inlineInfo->argCnt)
    {

#ifdef DEBUG
        if (verbose)
        {
            printf("\nArguments setup:\n");
        }
#endif // DEBUG

        for (unsigned argNum = 0; argNum < inlineInfo->argCnt; argNum++)
        {
            const InlArgInfo& argInfo        = inlArgInfo[argNum];
            const bool        argIsSingleDef = !argInfo.argHasLdargaOp && !argInfo.argHasStargOp;
            CallArg*          arg            = argInfo.arg;
            GenTree*          argNode        = arg->GetNode();

            assert(!argNode->OperIs(GT_RET_EXPR));

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
                    assert(argNode->gtOper != GT_BLK);
                    argSingleUseNode->ReplaceWith(argNode, this);
                    continue;
                }
                else
                {
                    // We're going to assign the argument value to the temp we use for it in the inline body.
                    GenTree* store = gtNewTempStore(argInfo.argTmpNum, argNode);

                    newStmt = gtNewStmt(store, callDI);
                    fgInsertStmtAfter(block, afterStmt, newStmt);
                    afterStmt = newStmt;

                    DISPSTMT(afterStmt);
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
                noway_assert((argInfo.argIsLclVar == 0) ==
                             (argNode->gtOper != GT_LCL_VAR || (argNode->gtFlags & GTF_GLOB_REF)));

                // If the argument has side effects, append it
                if (argInfo.argHasSideEff)
                {
                    noway_assert(argInfo.argIsUsed == false);
                    newStmt     = nullptr;
                    bool append = true;

                    if (argNode->gtOper == GT_BLK || argNode->gtOper == GT_MKREFANY)
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
                        assert(!argNode->OperIs(GT_RET_EXPR));

                        // For case (1)
                        //
                        // Look for the following tree shapes
                        // prejit: (IND (ADD (CONST, CALL(special dce helper...))))
                        // jit   : (COMMA (CALL(special dce helper...), (FIELD ...)))
                        if (argNode->gtOper == GT_COMMA)
                        {
                            // Look for (COMMA (CALL(special dce helper...), (FIELD ...)))
                            GenTree* op1 = argNode->AsOp()->gtOp1;
                            GenTree* op2 = argNode->AsOp()->gtOp2;
                            if (op1->IsCall() &&
                                ((op1->AsCall()->gtCallMoreFlags & GTF_CALL_M_HELPER_SPECIAL_DCE) != 0) &&
                                op2->OperIs(GT_IND) && op2->gtGetOp1()->IsIconHandle() &&
                                ((op2->gtFlags & GTF_EXCEPT) == 0))
                            {
                                JITDUMP("\nPerforming special dce on unused arg [%06u]:"
                                        " actual arg [%06u] helper call [%06u]\n",
                                        argNode->gtTreeID, argNode->gtTreeID, op1->gtTreeID);
                                // Drop the whole tree
                                append = false;
                            }
                        }
                        else if (argNode->gtOper == GT_IND)
                        {
                            // Look for (IND (ADD (CONST, CALL(special dce helper...))))
                            GenTree* addr = argNode->AsOp()->gtOp1;

                            if (addr->gtOper == GT_ADD)
                            {
                                GenTree* op1 = addr->AsOp()->gtOp1;
                                GenTree* op2 = addr->AsOp()->gtOp2;
                                if (op1->IsCall() &&
                                    ((op1->AsCall()->gtCallMoreFlags & GTF_CALL_M_HELPER_SPECIAL_DCE) != 0) &&
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

                        fgInsertStmtAfter(block, afterStmt, newStmt);
                        afterStmt = newStmt;
#ifdef DEBUG
                        if (verbose)
                        {
                            gtDispStmt(afterStmt);
                        }
#endif // DEBUG
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
    }

    // Add the CCTOR check if asked for.
    // Note: We no longer do the optimization that is done before by staticAccessedFirstUsingHelper in the old inliner.
    //       Therefore we might prepend redundant call to HELPER.CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
    //       before the inlined method body, even if a static field of this type was accessed in the inlinee
    //       using a helper before any other observable side-effect.

    if (inlineInfo->inlineCandidateInfo->initClassResult & CORINFO_INITCLASS_USE_HELPER)
    {
        CORINFO_CLASS_HANDLE exactClass = eeGetClassFromContext(inlineInfo->inlineCandidateInfo->exactContextHnd);

        tree    = fgGetSharedCCtor(exactClass);
        newStmt = gtNewStmt(tree, callDI);
        fgInsertStmtAfter(block, afterStmt, newStmt);
        afterStmt = newStmt;
    }

    // Insert the nullcheck statement now.
    if (nullcheck)
    {
        newStmt = gtNewStmt(nullcheck, callDI);
        fgInsertStmtAfter(block, afterStmt, newStmt);
        afterStmt = newStmt;
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

                newStmt = gtNewStmt(tree, callDI);
                fgInsertStmtAfter(block, afterStmt, newStmt);
                afterStmt = newStmt;

                DISPSTMT(afterStmt);
            }
        }
    }

    return afterStmt;
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
        if ((inlCandInfo->retExpr != nullptr) && (inlCandInfo->retExpr->gtSubstExpr != nullptr))
        {
            const bool interferesWithReturn = gtHasRef(inlCandInfo->retExpr->gtSubstExpr, tmpNum);
            noway_assert(!interferesWithReturn);
        }

        // Assign null to the local.
        GenTree*   nullExpr = gtNewTempStore(tmpNum, gtNewZeroConNode(lclTyp));
        Statement* nullStmt = gtNewStmt(nullExpr, callDI);

        if (stmtAfter == nullptr)
        {
            fgInsertStmtAtBeg(block, nullStmt);
        }
        else
        {
            fgInsertStmtAfter(block, stmtAfter, nullStmt);
        }
        stmtAfter = nullStmt;

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
