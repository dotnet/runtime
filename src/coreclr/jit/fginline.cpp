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
//    We generally disallow recursive inlines by policy. However, they are
//    supported by the underlying machinery.
//
//    Likewise the depth limit is a policy consideration, and serves mostly
//    as a safeguard to prevent runaway inlining of small methods.
//
unsigned Compiler::fgCheckInlineDepthAndRecursion(InlineInfo* inlineInfo)
{
    BYTE*          candidateCode = inlineInfo->inlineCandidateInfo->methInfo.ILCode;
    InlineContext* inlineContext = inlineInfo->inlineCandidateInfo->inlinersContext;
    InlineResult*  inlineResult  = inlineInfo->inlineResult;

    // There should be a context for all candidates.
    assert(inlineContext != nullptr);

    int depth = 0;

    for (; inlineContext != nullptr; inlineContext = inlineContext->GetParent())
    {
        assert(inlineContext->GetCode() != nullptr);
        depth++;

        if (inlineContext->GetCode() == candidateCode)
        {
            // This inline candidate has the same IL code buffer as an already
            // inlined method does.
            inlineResult->NoteFatal(InlineObservation::CALLSITE_IS_RECURSIVE);

            // No need to note CALLSITE_DEPTH we're already rejecting this candidate
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
        // when we visit GT_ASG similar to AttachStructInlineeToAsg.
        //
        if (tree->OperGet() == GT_ASG)
        {
            GenTree* value = tree->AsOp()->gtOp2;

            if (value->OperGet() == GT_COMMA)
            {
                GenTree* effectiveValue = value->gtEffectiveVal(/*commaOnly*/ true);

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

            // TODO-Inlining: We should really only propagate BBF_COPY_PROPAGATE
            // flags here. BBF_SPLIT_GAINED includes BBF_PROF_WEIGHT, and
            // propagating that has the effect that inlining a tree from a hot
            // block into a block without profile weights means we suddenly
            // start to see the inliner block as hot and treat future inline
            // candidates more aggressively.
            //
            if (inlineeBB != nullptr)
            {
                // IR may potentially contain nodes that requires mandatory BB flags to be set.
                // Propagate those flags from the containing BB.
                m_compiler->compCurBB->bbFlags |= inlineeBB->bbFlags & BBF_COPY_PROPAGATE;
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
                //
                case Compiler::SPK_ByValue:
                case Compiler::SPK_ByValueAsHfa:
                {
                    // See assert below, we only look one level above for an asg parent.
                    if (parent->OperIs(GT_ASG))
                    {
                        // The inlinee can only be the RHS.
                        assert(parent->gtGetOp2() == *use);
                        AttachStructInlineeToAsg(parent->AsOp(), retClsHnd);
                    }
                    else
                    {
                        // Just assign the inlinee to a variable to keep it simple.
                        *use = AssignStructInlineeToVar(*use, retClsHnd);
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
    // AttachStructInlineeToAsg: Update an "ASG(..., inlinee)" tree.
    //
    // Morphs inlinees that are multi-reg nodes into the (only) supported shape
    // of "lcl = node()", either by marking the LHS local "lvIsMultiRegRet" or
    // assigning the node into a temp and using that as the RHS.
    //
    // Arguments:
    //    asg       - The assignment with the inlinee on the RHS
    //    retClsHnd - The struct handle for the inlinee
    //
    void AttachStructInlineeToAsg(GenTreeOp* asg, CORINFO_CLASS_HANDLE retClsHnd)
    {
        assert(asg->OperIs(GT_ASG));

        GenTree* dst     = asg->gtGetOp1();
        GenTree* inlinee = asg->gtGetOp2();

        // We need to force all assignments from multi-reg nodes into the "lcl = node()" form.
        if (inlinee->IsMultiRegNode())
        {
            // Special case: we already have a local, the only thing to do is mark it appropriately. Except
            // if it may turn into an indirection.
            if (dst->OperIs(GT_LCL_VAR) && !m_compiler->lvaIsImplicitByRefLocal(dst->AsLclVar()->GetLclNum()))
            {
                m_compiler->lvaGetDesc(dst->AsLclVar())->lvIsMultiRegRet = true;
            }
            else
            {
                // Here, we assign our node into a fresh temp and then use that temp as the new value.
                asg->gtOp2 = AssignStructInlineeToVar(inlinee, retClsHnd);
            }
        }
    }

    //------------------------------------------------------------------------
    // AssignStructInlineeToVar: Assign the struct inlinee to a temp local.
    //
    // Arguments:
    //    inlinee   - The inlinee of the RET_EXPR node
    //    retClsHnd - The struct class handle of the type of the inlinee.
    //
    // Return Value:
    //    Value representing the freshly assigned temp.
    //
    GenTree* AssignStructInlineeToVar(GenTree* inlinee, CORINFO_CLASS_HANDLE retClsHnd)
    {
        assert(!inlinee->OperIs(GT_MKREFANY, GT_RET_EXPR));

        unsigned   lclNum = m_compiler->lvaGrabTemp(false DEBUGARG("RetBuf for struct inline return candidates."));
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        m_compiler->lvaSetStruct(lclNum, retClsHnd, false);

        // Sink the assignment below any COMMAs: this is required for multi-reg nodes.
        GenTree* src       = inlinee;
        GenTree* lastComma = nullptr;
        while (src->OperIs(GT_COMMA))
        {
            lastComma = src;
            src       = src->AsOp()->gtOp2;
        }

        // When assigning a multi-register value to a local var, make sure the variable is marked as lvIsMultiRegRet.
        if (src->IsMultiRegNode())
        {
            varDsc->lvIsMultiRegRet = true;
        }

        GenTree* dst = m_compiler->gtNewLclvNode(lclNum, varDsc->TypeGet());
        GenTree* asg = m_compiler->gtNewBlkOpNode(dst, src, /* isVolatile */ false, /* isCopyBlock */ true);

        // If inlinee was comma, new inlinee is (, , , lcl = inlinee).
        if (inlinee->OperIs(GT_COMMA))
        {
            lastComma->AsOp()->gtOp2 = asg;
            asg                      = inlinee;
        }

        // Block morphing does not support (promoted) locals under commas, as such, instead of "COMMA(asg, lcl)" we
        // do "OBJ(COMMA(asg, ADDR(LCL)))". TODO-1stClassStructs: improve block morphing and delete this workaround.
        //
        GenTree* lcl  = m_compiler->gtNewLclvNode(lclNum, varDsc->TypeGet());
        GenTree* addr = m_compiler->gtNewOperNode(GT_ADDR, TYP_I_IMPL, lcl);
        addr          = m_compiler->gtNewOperNode(GT_COMMA, addr->TypeGet(), asg, addr);
        GenTree* obj  = m_compiler->gtNewObjNode(varDsc->GetLayout(), addr);

        return obj;
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

                m_compiler->impDevirtualizeCall(call, nullptr, &method, &methodFlags, &context, nullptr,
                                                isLateDevirtualization, explicitTailCall);
                m_madeChanges = true;
            }
        }
        else if (tree->OperGet() == GT_ASG)
        {
            // If we're assigning to a ref typed local that has one definition,
            // we may be able to sharpen the type for the local.
            GenTree* const effLhs = tree->gtGetOp1()->gtEffectiveVal();

            if ((effLhs->OperGet() == GT_LCL_VAR) && (effLhs->TypeGet() == TYP_REF))
            {
                const unsigned lclNum = effLhs->AsLclVarCommon()->GetLclNum();
                LclVarDsc*     lcl    = m_compiler->lvaGetDesc(lclNum);

                if (lcl->lvSingleDef)
                {
                    GenTree*             rhs       = tree->gtGetOp2();
                    bool                 isExact   = false;
                    bool                 isNonNull = false;
                    CORINFO_CLASS_HANDLE newClass  = m_compiler->gtGetClassHandle(rhs, &isExact, &isNonNull);

                    if (newClass != NO_CLASS_HANDLE)
                    {
                        m_compiler->lvaUpdateClass(lclNum, newClass, isExact);
                        m_madeChanges = true;
                    }
                }
            }

            // If we created a self-assignment (say because we are sharing return spill temps)
            // we can remove it.
            //
            GenTree* const lhs = tree->gtGetOp1();
            GenTree* const rhs = tree->gtGetOp2();
            if (lhs->OperIs(GT_LCL_VAR) && GenTree::Compare(lhs, rhs))
            {
                m_compiler->gtUpdateNodeSideEffects(tree);
                assert((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_ASG);
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

                noway_assert(!m_compiler->fgComputePredsDone);

                // We have a constant operand, and should have the all clear to optimize.
                // Update side effects on the tree, assert there aren't any, and bash to nop.
                m_compiler->gtUpdateNodeSideEffects(tree);
                assert((tree->gtFlags & GTF_SIDE_EFFECT) == 0);
                tree->gtBashToNOP();
                m_madeChanges = true;

                if (!condTree->IsIntegralConst(0))
                {
                    block->bbJumpKind = BBJ_ALWAYS;
                }
                else
                {
                    block->bbJumpKind = BBJ_NONE;
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
#ifdef DEBUG
    // Inliner could add basic blocks. Check that the flowgraph data is up-to-date
    fgDebugCheckBBlist(false, false);
#endif // DEBUG

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

#if defined(DEBUG) || defined(INLINE_DATA)
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

        block = block->bbNext;

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

        block = block->bbNext;

    } while (block);

    fgVerifyHandlerTab();

    if (verbose || fgPrintInlinedMethods)
    {
        JITDUMP("**************** Inline Tree");
        printf("\n");
        m_inlineStrategy->Dump(verbose || JitConfig.JitPrintInlinedMethodsVerbose());
    }

#endif // DEBUG

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#if defined(DEBUG) || defined(INLINE_DATA)

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

    InlineCandidateInfo* inlineCandidateInfo = call->gtInlineCandidateInfo;
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
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_PROF_ENTERLEAVE);
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_DEBUG_EnC);
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_REVERSE_PINVOKE);
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_TRACK_TRANSITIONS);

                compileFlagsForInlinee.Set(JitFlags::JIT_FLAG_SKIP_VERIFICATION);

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

    BasicBlock* topBlock    = iciBlock;
    BasicBlock* bottomBlock = nullptr;

    if (InlineeCompiler->fgBBcount == 1)
    {
        // When fgBBCount is 1 we will always have a non-NULL fgFirstBB
        //
        PREFAST_ASSUME(InlineeCompiler->fgFirstBB != nullptr);

        // DDB 91389: Don't throw away the (only) inlinee block
        // when its return type is not BBJ_RETURN.
        // In other words, we need its BBJ_ to perform the right thing.
        if (InlineeCompiler->fgFirstBB->bbJumpKind == BBJ_RETURN)
        {
            // Inlinee contains just one BB. So just insert its statement list to topBlock.
            if (InlineeCompiler->fgFirstBB->bbStmtList != nullptr)
            {
                stmtAfter = fgInsertStmtListAfter(iciBlock, stmtAfter, InlineeCompiler->fgFirstBB->firstStmt());
            }

            // Copy inlinee bbFlags to caller bbFlags.
            const BasicBlockFlags inlineeBlockFlags = InlineeCompiler->fgFirstBB->bbFlags;
            noway_assert((inlineeBlockFlags & BBF_HAS_JMP) == 0);
            noway_assert((inlineeBlockFlags & BBF_KEEP_BBJ_ALWAYS) == 0);

            // Todo: we may want to exclude other flags here.
            iciBlock->bbFlags |= (inlineeBlockFlags & ~BBF_RUN_RARELY);

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

            goto _Done;
        }
    }

    //
    // ======= Inserting inlinee's basic blocks ===============
    //

    bottomBlock             = fgNewBBafter(topBlock->bbJumpKind, topBlock, true);
    bottomBlock->bbRefs     = 1;
    bottomBlock->bbJumpDest = topBlock->bbJumpDest;
    bottomBlock->inheritWeight(topBlock);

    topBlock->bbJumpKind = BBJ_NONE;

    // Update block flags
    {
        const BasicBlockFlags originalFlags = topBlock->bbFlags;
        noway_assert((originalFlags & BBF_SPLIT_NONEXIST) == 0);
        topBlock->bbFlags &= ~(BBF_SPLIT_LOST);
        bottomBlock->bbFlags |= originalFlags & BBF_SPLIT_GAINED;
    }

    // Split statements between topBlock and bottomBlock.
    // First figure out bottomBlock_Begin
    Statement* bottomBlock_Begin;
    bottomBlock_Begin = stmtAfter->GetNextStmt();

    if (topBlock->bbStmtList == nullptr)
    {
        // topBlock is empty before the split.
        // In this case, both topBlock and bottomBlock should be empty
        noway_assert(bottomBlock_Begin == nullptr);
        topBlock->bbStmtList    = nullptr;
        bottomBlock->bbStmtList = nullptr;
    }
    else if (topBlock->bbStmtList == bottomBlock_Begin)
    {
        noway_assert(bottomBlock_Begin != nullptr);

        // topBlock contains at least one statement before the split.
        // And the split is before the first statement.
        // In this case, topBlock should be empty, and everything else should be moved to the bottomBlock.
        bottomBlock->bbStmtList = topBlock->bbStmtList;
        topBlock->bbStmtList    = nullptr;
    }
    else if (bottomBlock_Begin == nullptr)
    {
        noway_assert(topBlock->bbStmtList != nullptr);

        // topBlock contains at least one statement before the split.
        // And the split is at the end of the topBlock.
        // In this case, everything should be kept in the topBlock, and the bottomBlock should be empty

        bottomBlock->bbStmtList = nullptr;
    }
    else
    {
        noway_assert(topBlock->bbStmtList != nullptr);
        noway_assert(bottomBlock_Begin != nullptr);

        // This is the normal case where both blocks should contain at least one statement.
        Statement* topBlock_Begin = topBlock->firstStmt();
        noway_assert(topBlock_Begin != nullptr);
        Statement* topBlock_End = bottomBlock_Begin->GetPrevStmt();
        noway_assert(topBlock_End != nullptr);
        Statement* bottomBlock_End = topBlock->lastStmt();
        noway_assert(bottomBlock_End != nullptr);

        // Break the linkage between 2 blocks.
        topBlock_End->SetNextStmt(nullptr);

        // Fix up all the pointers.
        topBlock->bbStmtList = topBlock_Begin;
        topBlock->bbStmtList->SetPrevStmt(topBlock_End);

        bottomBlock->bbStmtList = bottomBlock_Begin;
        bottomBlock->bbStmtList->SetPrevStmt(bottomBlock_End);
    }

    //
    // Set the try and handler index and fix the jump types of inlinee's blocks.
    //
    for (BasicBlock* const block : InlineeCompiler->Blocks())
    {
        noway_assert(!block->hasTryIndex());
        noway_assert(!block->hasHndIndex());
        block->copyEHRegion(iciBlock);
        block->bbFlags |= iciBlock->bbFlags & BBF_BACKWARD_JUMP;

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
            block->bbFlags |= BBF_INTERNAL;
        }

        if (block->bbJumpKind == BBJ_RETURN)
        {
            noway_assert((block->bbFlags & BBF_HAS_JMP) == 0);
            if (block->bbNext)
            {
                JITDUMP("\nConvert bbJumpKind of " FMT_BB " to BBJ_ALWAYS to bottomBlock " FMT_BB "\n", block->bbNum,
                        bottomBlock->bbNum);
                block->bbJumpKind = BBJ_ALWAYS;
                block->bbJumpDest = bottomBlock;
            }
            else
            {
                JITDUMP("\nConvert bbJumpKind of " FMT_BB " to BBJ_NONE\n", block->bbNum);
                block->bbJumpKind = BBJ_NONE;
            }
        }
    }

    // Insert inlinee's blocks into inliner's block list.
    topBlock->setNext(InlineeCompiler->fgFirstBB);
    InlineeCompiler->fgLastBB->setNext(bottomBlock);

    //
    // Add inlinee's block count to inliner's.
    //
    fgBBcount += InlineeCompiler->fgBBcount;

    // Append statements to null out gc ref locals, if necessary.
    fgInlineAppendStatements(pInlineInfo, bottomBlock, nullptr);

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks(InlineeCompiler->fgFirstBB, InlineeCompiler->fgLastBB, true);
    }
#endif // DEBUG

_Done:

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

                if ((argSingleUseNode != nullptr) && !(argSingleUseNode->gtFlags & GTF_VAR_CLONED) && argIsSingleDef)
                {
                    // Change the temp in-place to the actual argument.
                    // We currently do not support this for struct arguments, so it must not be a GT_OBJ.
                    assert(argNode->gtOper != GT_OBJ);
                    argSingleUseNode->ReplaceWith(argNode, this);
                    continue;
                }
                else
                {
                    // We're going to assign the argument value to the
                    // temp we use for it in the inline body.
                    const unsigned  tmpNum  = argInfo.argTmpNum;
                    const var_types argType = lclVarInfo[argNum].lclTypeInfo;

                    // Create the temp assignment for this argument
                    CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;

                    if (varTypeIsStruct(argType))
                    {
                        structHnd = gtGetStructHandleIfPresent(argNode);
                        noway_assert((structHnd != NO_CLASS_HANDLE) || (argType != TYP_STRUCT));
                    }

                    // Unsafe value cls check is not needed for
                    // argTmpNum here since in-linee compiler instance
                    // would have iterated over these and marked them
                    // accordingly.
                    impAssignTempGen(tmpNum, argNode, structHnd, CHECK_SPILL_NONE, &afterStmt, callDI, block);

                    // We used to refine the temp type here based on
                    // the actual arg, but we now do this up front, when
                    // creating the temp, over in impInlineFetchArg.
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                    if (verbose)
                    {
                        gtDispStmt(afterStmt);
                    }
#endif // DEBUG
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

                    if (argNode->gtOper == GT_OBJ || argNode->gtOper == GT_MKREFANY)
                    {
                        // Don't put GT_OBJ node under a GT_COMMA.
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
                                (op2->gtOper == GT_FIELD) && ((op2->gtFlags & GTF_EXCEPT) == 0))
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
    bool     bbInALoop  = (block->bbFlags & BBF_BACKWARD_JUMP) != 0;
    bool     bbIsReturn = block->bbJumpKind == BBJ_RETURN;

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

                var_types lclTyp = (var_types)lvaTable[tmpNum].lvType;
                noway_assert(lclTyp == lclVarInfo[lclNum + inlineInfo->argCnt].lclTypeInfo);

                if (!varTypeIsStruct(lclTyp))
                {
                    // Unsafe value cls check is not needed here since in-linee compiler instance would have
                    // iterated over locals and marked accordingly.
                    impAssignTempGen(tmpNum, gtNewZeroConNode(genActualType(lclTyp)), NO_CLASS_HANDLE, CHECK_SPILL_NONE,
                                     &afterStmt, callDI, block);
                }
                else
                {
                    tree = gtNewBlkOpNode(gtNewLclvNode(tmpNum, lclTyp), // Dest
                                          gtNewIconNode(0),              // Value
                                          false,                         // isVolatile
                                          false);                        // not copyBlock

                    newStmt = gtNewStmt(tree, callDI);
                    fgInsertStmtAfter(block, afterStmt, newStmt);
                    afterStmt = newStmt;
                }

#ifdef DEBUG
                if (verbose)
                {
                    gtDispStmt(afterStmt);
                }
#endif // DEBUG
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
        GenTree*   nullExpr = gtNewTempAssign(tmpNum, gtNewZeroConNode(lclTyp));
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
