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
    InlineContext* inlineContext = inlineInfo->iciStmt->GetInlineContext();
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
            break;
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
//   tree walk, we look for opportunties to devirtualize or optimize now that
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
    fgPrintInlinedMethods = JitConfig.JitPrintInlinedMethods().contains(info.compMethodName, info.compClassName,
                                                                        &info.compMethodInfo->args);
#endif // DEBUG

    BasicBlock* block       = fgFirstBB;
    bool        madeChanges = false;
    noway_assert(block != nullptr);

    // Set the root inline context on all statements
    InlineContext* rootContext = m_inlineStrategy->GetRootContext();

    for (; block != nullptr; block = block->bbNext)
    {
        for (Statement* stmt : block->Statements())
        {
            stmt->SetInlineContext(rootContext);
        }
    }

    // Reset block back to start for inlining
    block = fgFirstBB;

    do
    {
        // Make the current basic block address available globally
        compCurBB = block;

        for (Statement* stmt : block->Statements())
        {

#ifdef DEBUG
            // In debug builds we want the inline tree to show all failed
            // inlines. Some inlines may fail very early and never make it to
            // candidate stage. So scan the tree looking for those early failures.
            fgWalkTreePre(stmt->GetRootNodePointer(), fgFindNonInlineCandidate, stmt);
#endif

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

            // See if we need to replace some return value place holders.
            // Also, see if this replacement enables further devirtualization.
            //
            // Note we have both preorder and postorder callbacks here.
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
            fgWalkTree(stmt->GetRootNodePointer(), fgUpdateInlineReturnExpressionPlaceHolder, fgLateDevirtualization,
                       (void*)this);

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

#ifdef DEBUG

    // Check that we should not have any inline candidate or return value place holder left.

    block = fgFirstBB;
    noway_assert(block);

    do
    {
        for (Statement* stmt : block->Statements())
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
        m_inlineStrategy->Dump(verbose);
    }

#endif // DEBUG

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#ifdef DEBUG

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

    InlineResult      inlineResult(this, call, nullptr, "fgNotInlineCandidate");
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
    inlineResult.SetReported();

    if (call->gtCallType == CT_USER_FUNC)
    {
        // Create InlineContext for the failure
        m_inlineStrategy->NewFailure(stmt, &inlineResult);
    }
}

#endif

#if FEATURE_MULTIREG_RET

/*********************************************************************************
 *
 * tree - The node which needs to be converted to a struct pointer.
 *
 *  Return the pointer by either __replacing__ the tree node with a suitable pointer
 *  type or __without replacing__ and just returning a subtree or by __modifying__
 *  a subtree.
 */
GenTree* Compiler::fgGetStructAsStructPtr(GenTree* tree)
{
    noway_assert(tree->OperIs(GT_LCL_VAR, GT_FIELD, GT_IND, GT_BLK, GT_OBJ, GT_COMMA) || tree->OperIsSIMD() ||
                 tree->OperIsHWIntrinsic());
    // GT_CALL,     cannot get address of call.
    // GT_MKREFANY, inlining should've been aborted due to mkrefany opcode.
    // GT_RET_EXPR, cannot happen after fgUpdateInlineReturnExpressionPlaceHolder

    switch (tree->OperGet())
    {
        case GT_BLK:
        case GT_OBJ:
        case GT_IND:
            return tree->AsOp()->gtOp1;

        case GT_COMMA:
            tree->AsOp()->gtOp2 = fgGetStructAsStructPtr(tree->AsOp()->gtOp2);
            tree->gtType        = TYP_BYREF;
            return tree;

        default:
            return gtNewOperNode(GT_ADDR, TYP_BYREF, tree);
    }
}

/***************************************************************************************************
 * child     - The inlinee of the retExpr node.
 * retClsHnd - The struct class handle of the type of the inlinee.
 *
 * Assign the inlinee to a tmp, if it is a call, just assign it to a lclVar, else we can
 * use a copyblock to do the assignment.
 */
GenTree* Compiler::fgAssignStructInlineeToVar(GenTree* child, CORINFO_CLASS_HANDLE retClsHnd)
{
    assert(child->gtOper != GT_RET_EXPR && child->gtOper != GT_MKREFANY);

    unsigned tmpNum = lvaGrabTemp(false DEBUGARG("RetBuf for struct inline return candidates."));
    lvaSetStruct(tmpNum, retClsHnd, false);
    var_types structType = lvaTable[tmpNum].lvType;

    GenTree* dst = gtNewLclvNode(tmpNum, structType);

    // If we have a call, we'd like it to be: V00 = call(), but first check if
    // we have a ", , , call()" -- this is very defensive as we may never get
    // an inlinee that is made of commas. If the inlinee is not a call, then
    // we use a copy block to do the assignment.
    GenTree* src       = child;
    GenTree* lastComma = nullptr;
    while (src->gtOper == GT_COMMA)
    {
        lastComma = src;
        src       = src->AsOp()->gtOp2;
    }

    GenTree* newInlinee = nullptr;
    if (src->gtOper == GT_CALL)
    {
        // If inlinee was just a call, new inlinee is v05 = call()
        newInlinee = gtNewAssignNode(dst, src);

        // When returning a multi-register value in a local var, make sure the variable is
        // marked as lvIsMultiRegRet, so it does not get promoted.
        if (src->AsCall()->HasMultiRegRetVal())
        {
            lvaTable[tmpNum].lvIsMultiRegRet = true;
        }

        // If inlinee was comma, but a deeper call, new inlinee is (, , , v05 = call())
        if (child->gtOper == GT_COMMA)
        {
            lastComma->AsOp()->gtOp2 = newInlinee;
            newInlinee               = child;
        }
    }
    else
    {
        // Inlinee is not a call, so just create a copy block to the tmp.
        src              = child;
        GenTree* dstAddr = fgGetStructAsStructPtr(dst);
        GenTree* srcAddr = fgGetStructAsStructPtr(src);
        newInlinee       = gtNewCpObjNode(dstAddr, srcAddr, retClsHnd, false);
    }

    GenTree* production = gtNewLclvNode(tmpNum, structType);
    return gtNewOperNode(GT_COMMA, structType, newInlinee, production);
}

/***************************************************************************************************
 * tree      - The tree pointer that has one of its child nodes as retExpr.
 * child     - The inlinee child.
 * retClsHnd - The struct class handle of the type of the inlinee.
 *
 * V04 = call() assignments are okay as we codegen it. Everything else needs to be a copy block or
 * would need a temp. For example, a cast(ldobj) will then be, cast(v05 = ldobj, v05); But it is
 * a very rare (or impossible) scenario that we'd have a retExpr transform into a ldobj other than
 * a lclVar/call. So it is not worthwhile to do pattern matching optimizations like addr(ldobj(op1))
 * can just be op1.
 */
void Compiler::fgAttachStructInlineeToAsg(GenTree* tree, GenTree* child, CORINFO_CLASS_HANDLE retClsHnd)
{
    // We are okay to have:
    // 1. V02 = call();
    // 2. copyBlk(dstAddr, srcAddr);
    assert(tree->gtOper == GT_ASG);

    // We have an assignment, we codegen only V05 = call().
    if (child->gtOper == GT_CALL && tree->AsOp()->gtOp1->gtOper == GT_LCL_VAR)
    {
        // If it is a multireg return on x64/ux, the local variable should be marked as lvIsMultiRegRet
        if (child->AsCall()->HasMultiRegRetVal())
        {
            unsigned lclNum                  = tree->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
            lvaTable[lclNum].lvIsMultiRegRet = true;
        }
        return;
    }

    GenTree* dstAddr = fgGetStructAsStructPtr(tree->AsOp()->gtOp1);
    GenTree* srcAddr = fgGetStructAsStructPtr(
        (child->gtOper == GT_CALL)
            ? fgAssignStructInlineeToVar(child, retClsHnd) // Assign to a variable if it is a call.
            : child);                                      // Just get the address, if not a call.

    tree->ReplaceWith(gtNewCpObjNode(dstAddr, srcAddr, retClsHnd, false), this);
}

#endif // FEATURE_MULTIREG_RET

//------------------------------------------------------------------------
// fgUpdateInlineReturnExpressionPlaceHolder: callback to replace the
// inline return expression placeholder.
//
// Arguments:
//    pTree -- pointer to tree to examine for updates
//    data  -- context data for the tree walk
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
Compiler::fgWalkResult Compiler::fgUpdateInlineReturnExpressionPlaceHolder(GenTree** pTree, fgWalkData* data)
{
    // All the operations here and in the corresponding postorder
    // callback (fgLateDevirtualization) are triggered by GT_CALL or
    // GT_RET_EXPR trees, and these (should) have the call side
    // effect flag.
    //
    // So bail out for any trees that don't have this flag.
    GenTree* tree = *pTree;

    if ((tree->gtFlags & GTF_CALL) == 0)
    {
        return WALK_SKIP_SUBTREES;
    }

    Compiler*            comp      = data->compiler;
    CORINFO_CLASS_HANDLE retClsHnd = NO_CLASS_HANDLE;

    while (tree->OperGet() == GT_RET_EXPR)
    {
        // We are going to copy the tree from the inlinee,
        // so record the handle now.
        //
        if (varTypeIsStruct(tree))
        {
            retClsHnd = tree->AsRetExpr()->gtRetClsHnd;
        }

        // Skip through chains of GT_RET_EXPRs (say from nested inlines)
        // to the actual tree to use.
        //
        // Also we might as well try and fold the return value.
        // Eg returns of constant bools will have CASTS.
        // This folding may uncover more GT_RET_EXPRs, so we loop around
        // until we've got something distinct.
        //
        unsigned __int64 bbFlags         = 0;
        GenTree*         inlineCandidate = tree->gtRetExprVal(&bbFlags);
        inlineCandidate                  = comp->gtFoldExpr(inlineCandidate);
        var_types retType                = tree->TypeGet();

#ifdef DEBUG
        if (comp->verbose)
        {
            printf("\nReplacing the return expression placeholder ");
            printTreeID(tree);
            printf(" with ");
            printTreeID(inlineCandidate);
            printf("\n");
            // Dump out the old return expression placeholder it will be overwritten by the ReplaceWith below
            comp->gtDispTree(tree);
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
            else
            {
                // - under a call if we changed size of the argument.
                GenTree* putArgType = comp->fgCheckCallArgUpdate(data->parent, inlineCandidate, retType);
                if (putArgType != nullptr)
                {
                    inlineCandidate = putArgType;
                }
            }
        }

        tree->ReplaceWith(inlineCandidate, comp);
        comp->compCurBB->bbFlags |= (bbFlags & BBF_SPLIT_GAINED);

#ifdef DEBUG
        if (comp->verbose)
        {
            printf("\nInserting the inline return expression\n");
            comp->gtDispTree(tree);
            printf("\n");
        }
#endif // DEBUG
    }

    // If an inline was rejected and the call returns a struct, we may
    // have deferred some work when importing call for cases where the
    // struct is returned in register(s).
    //
    // See the bail-out clauses in impFixupCallStructReturn for inline
    // candidates.
    //
    // Do the deferred work now.
    if (retClsHnd != NO_CLASS_HANDLE)
    {
        structPassingKind howToReturnStruct;
        var_types         returnType =
            comp->getReturnTypeForStruct(retClsHnd, CorInfoCallConvExtension::Managed, &howToReturnStruct);
        GenTree* parent = data->parent;

        switch (howToReturnStruct)
        {

#if FEATURE_MULTIREG_RET

            // Is this a type that is returned in multiple registers
            // or a via a primitve type that is larger than the struct type?
            // if so we need to force into into a form we accept.
            // i.e. LclVar = call()
            case SPK_ByValue:
            case SPK_ByValueAsHfa:
            {
                // See assert below, we only look one level above for an asg parent.
                if (parent->gtOper == GT_ASG)
                {
                    // Either lhs is a call V05 = call(); or lhs is addr, and asg becomes a copyBlk.
                    comp->fgAttachStructInlineeToAsg(parent, tree, retClsHnd);
                }
                else
                {
                    // Just assign the inlinee to a variable to keep it simple.
                    tree->ReplaceWith(comp->fgAssignStructInlineeToVar(tree, retClsHnd), comp);
                }
            }
            break;

#endif // FEATURE_MULTIREG_RET

            case SPK_EnclosingType:
            case SPK_PrimitiveType:
                // No work needs to be done, the call has struct type and should keep it.
                break;

            case SPK_ByReference:
                // We should have already added the return buffer
                // when we first imported the call
                break;

            default:
                noway_assert(!"Unexpected struct passing kind");
                break;
        }
    }

#if FEATURE_MULTIREG_RET
#if defined(DEBUG)

    // Make sure we don't have a tree like so: V05 = (, , , retExpr);
    // Since we only look one level above for the parent for '=' and
    // do not check if there is a series of COMMAs. See above.
    // Importer and FlowGraph will not generate such a tree, so just
    // leaving an assert in here. This can be fixed by looking ahead
    // when we visit GT_ASG similar to fgAttachStructInlineeToAsg.
    //
    if (tree->OperGet() == GT_ASG)
    {
        GenTree* value = tree->AsOp()->gtOp2;

        if (value->OperGet() == GT_COMMA)
        {
            GenTree* effectiveValue = value->gtEffectiveVal(/*commaOnly*/ true);

            noway_assert(!varTypeIsStruct(effectiveValue) || (effectiveValue->OperGet() != GT_RET_EXPR) ||
                         !comp->IsMultiRegReturnedType(effectiveValue->AsRetExpr()->gtRetClsHnd,
                                                       CorInfoCallConvExtension::Managed));
        }
    }

#endif // defined(DEBUG)
#endif // FEATURE_MULTIREG_RET

    return WALK_CONTINUE;
}

//------------------------------------------------------------------------
// fgLateDevirtualization: re-examine calls after inlining to see if we
//   can do more devirtualization
//
// Arguments:
//    pTree -- pointer to tree to examine for updates
//    data  -- context data for the tree walk
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

Compiler::fgWalkResult Compiler::fgLateDevirtualization(GenTree** pTree, fgWalkData* data)
{
    GenTree*  tree   = *pTree;
    GenTree*  parent = data->parent;
    Compiler* comp   = data->compiler;

    // In some (rare) cases the parent node of tree will be smashed to a NOP during
    // the preorder by fgAttachStructToInlineeArg.
    //
    // jit\Methodical\VT\callconv\_il_reljumper3 for x64 linux
    //
    // If so, just bail out here.
    if (tree == nullptr)
    {
        assert((parent != nullptr) && parent->OperGet() == GT_NOP);
        return WALK_CONTINUE;
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
            if (comp->verbose)
            {
                printf("**** Late devirt opportunity\n");
                comp->gtDispTree(call);
            }
#endif // DEBUG

            CORINFO_METHOD_HANDLE  method                 = call->gtCallMethHnd;
            unsigned               methodFlags            = 0;
            CORINFO_CONTEXT_HANDLE context                = nullptr;
            const bool             isLateDevirtualization = true;
            bool explicitTailCall = (call->AsCall()->gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
            comp->impDevirtualizeCall(call, &method, &methodFlags, &context, nullptr, isLateDevirtualization,
                                      explicitTailCall);
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
            LclVarDsc*     lcl    = comp->lvaGetDesc(lclNum);

            if (lcl->lvSingleDef)
            {
                GenTree*             rhs       = tree->gtGetOp2();
                bool                 isExact   = false;
                bool                 isNonNull = false;
                CORINFO_CLASS_HANDLE newClass  = comp->gtGetClassHandle(rhs, &isExact, &isNonNull);

                if (newClass != NO_CLASS_HANDLE)
                {
                    comp->lvaUpdateClass(lclNum, newClass, isExact);
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
            comp->gtUpdateNodeSideEffects(tree);
            assert((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_ASG);
            JITDUMP("... removing self-assignment\n");
            DISPTREE(tree);
            tree->gtBashToNOP();
        }
    }
    else if (tree->OperGet() == GT_JTRUE)
    {
        // See if this jtrue is now foldable.
        BasicBlock* block    = comp->compCurBB;
        GenTree*    condTree = tree->AsOp()->gtOp1;
        assert(tree == block->lastStmt()->GetRootNode());

        if (condTree->OperGet() == GT_CNS_INT)
        {
            JITDUMP(" ... found foldable jtrue at [%06u] in " FMT_BB "\n", dspTreeID(tree), block->bbNum);
            noway_assert((block->bbNext->countOfInEdges() > 0) && (block->bbJumpDest->countOfInEdges() > 0));

            // We have a constant operand, and should have the all clear to optimize.
            // Update side effects on the tree, assert there aren't any, and bash to nop.
            comp->gtUpdateNodeSideEffects(tree);
            assert((tree->gtFlags & GTF_SIDE_EFFECT) == 0);
            tree->gtBashToNOP();

            BasicBlock* bTaken    = nullptr;
            BasicBlock* bNotTaken = nullptr;

            if (condTree->AsIntCon()->gtIconVal != 0)
            {
                block->bbJumpKind = BBJ_ALWAYS;
                bTaken            = block->bbJumpDest;
                bNotTaken         = block->bbNext;
            }
            else
            {
                block->bbJumpKind = BBJ_NONE;
                bTaken            = block->bbNext;
                bNotTaken         = block->bbJumpDest;
            }

            comp->fgRemoveRefPred(bNotTaken, block);

            // If that was the last ref, a subsequent flow-opt pass
            // will clean up the now-unreachable bNotTaken, and any
            // other transitively unreachable blocks.
            if (bNotTaken->bbRefs == 0)
            {
                JITDUMP("... it looks like " FMT_BB " is now unreachable!\n", bNotTaken->bbNum);
            }
        }
    }
    else
    {
        const var_types retType    = tree->TypeGet();
        GenTree*        foldedTree = comp->gtFoldExpr(tree);
        const var_types newType    = foldedTree->TypeGet();

        GenTree* putArgType = comp->fgCheckCallArgUpdate(data->parent, foldedTree, retType);
        if (putArgType != nullptr)
        {
            foldedTree = putArgType;
        }
        *pTree = foldedTree;
    }

    return WALK_CONTINUE;
}

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

void Compiler::fgInvokeInlineeCompiler(GenTreeCall* call, InlineResult* inlineResult)
{
    noway_assert(call->gtOper == GT_CALL);
    noway_assert((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0);
    noway_assert(opts.OptEnabled(CLFLG_INLINING));

    // This is the InlineInfo struct representing a method to be inlined.
    InlineInfo inlineInfo;
    memset(&inlineInfo, 0, sizeof(inlineInfo));
    CORINFO_METHOD_HANDLE fncHandle = call->gtCallMethHnd;

    inlineInfo.fncHandle              = fncHandle;
    inlineInfo.iciCall                = call;
    inlineInfo.iciStmt                = fgMorphStmt;
    inlineInfo.iciBlock               = compCurBB;
    inlineInfo.thisDereferencedFirst  = false;
    inlineInfo.retExpr                = nullptr;
    inlineInfo.retBB                  = nullptr;
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
                compileFlagsForInlinee.Clear(JitFlags::JIT_FLAG_DEBUG_INFO);
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

    // If there is non-NULL return, but we haven't set the pInlineInfo->retExpr,
    // That means we haven't imported any BB that contains CEE_RET opcode.
    // (This could happen for example for a BBJ_THROW block fall through a BBJ_RETURN block which
    // causes the BBJ_RETURN block not to be imported at all.)
    // Fail the inlining attempt
    if (inlineCandidateInfo->fncRetType != TYP_VOID && inlineInfo.retExpr == nullptr)
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
    BasicBlock*  block;

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

    // Create a new inline context and mark the inlined statements with it
    InlineContext* calleeContext = m_inlineStrategy->NewSuccess(pInlineInfo);

    for (block = InlineeCompiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (Statement* stmt : block->Statements())
        {
            stmt->SetInlineContext(calleeContext);
        }
    }

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
            const unsigned __int64 inlineeBlockFlags = InlineeCompiler->fgFirstBB->bbFlags;
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
        const unsigned __int64 originalFlags = topBlock->bbFlags;
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
    for (block = InlineeCompiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        noway_assert(!block->hasTryIndex());
        noway_assert(!block->hasHndIndex());
        block->copyEHRegion(iciBlock);
        block->bbFlags |= iciBlock->bbFlags & BBF_BACKWARD_JUMP;

        if (iciStmt->GetILOffsetX() != BAD_IL_OFFSET)
        {
            block->bbCodeOffs    = jitGetILoffs(iciStmt->GetILOffsetX());
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
    compUnsafeCastUsed |= InlineeCompiler->compUnsafeCastUsed;
    compGSReorderStackLayout |= InlineeCompiler->compGSReorderStackLayout;
    compHasBackwardJump |= InlineeCompiler->compHasBackwardJump;

    lvaGenericsContextInUse |= InlineeCompiler->lvaGenericsContextInUse;

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
        const unsigned dummy   = lvaGrabTempWithImplicitUse(false DEBUGARG("GSCookie dummy for inlinee"));
        lvaTable[dummy].lvType = TYP_INT;
    }

    // If there is non-NULL return, replace the GT_CALL with its return value expression,
    // so later it will be picked up by the GT_RET_EXPR node.
    if ((pInlineInfo->inlineCandidateInfo->fncRetType != TYP_VOID) || (iciCall->gtReturnType == TYP_STRUCT))
    {
        noway_assert(pInlineInfo->retExpr);
#ifdef DEBUG
        if (verbose)
        {
            printf("\nReturn expression for call at ");
            printTreeID(iciCall);
            printf(" is\n");
            gtDispTree(pInlineInfo->retExpr);
        }
#endif // DEBUG

        // Replace the call with the return expression. Note that iciCall won't be part of the IR
        // but may still be referenced from a GT_RET_EXPR node. We will replace GT_RET_EXPR node
        // in fgUpdateInlineReturnExpressionPlaceHolder. At that time we will also update the flags
        // on the basic block of GT_RET_EXPR node.
        if (iciCall->gtInlineCandidateInfo->retExpr->OperGet() == GT_RET_EXPR)
        {
            // Save the basic block flags from the retExpr basic block.
            iciCall->gtInlineCandidateInfo->retExpr->AsRetExpr()->bbFlags = pInlineInfo->retBB->bbFlags;
        }
        iciCall->ReplaceWith(pInlineInfo->retExpr, this);
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
    BasicBlock*  block        = inlineInfo->iciBlock;
    Statement*   callStmt     = inlineInfo->iciStmt;
    IL_OFFSETX   callILOffset = callStmt->GetILOffsetX();
    Statement*   postStmt     = callStmt->GetNextStmt();
    Statement*   afterStmt    = callStmt; // afterStmt is the place where the new statements should be inserted after.
    Statement*   newStmt      = nullptr;
    GenTreeCall* call         = inlineInfo->iciCall->AsCall();

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
    // However the assetionProp logic will remove any unecessary null checks that we may have added
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
            GenTree*          argNode        = inlArgInfo[argNum].argNode;
            const bool        argHasPutArg   = argNode->OperIs(GT_PUTARG_TYPE);

            unsigned __int64 bbFlags = 0;
            argNode                  = argNode->gtSkipPutArgType();
            argNode                  = argNode->gtRetExprVal(&bbFlags);

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

                // argHasPutArg disqualifies the arg from a direct substitution because we don't have information about
                // its user. For example: replace `LCL_VAR short` with `PUTARG_TYPE short->LCL_VAR int`,
                // we should keep `PUTARG_TYPE` iff the user is a call that needs `short` and delete it otherwise.
                if ((argSingleUseNode != nullptr) && !(argSingleUseNode->gtFlags & GTF_VAR_CLONED) && argIsSingleDef &&
                    !argHasPutArg)
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
                    impAssignTempGen(tmpNum, argNode, structHnd, (unsigned)CHECK_SPILL_NONE, &afterStmt, callILOffset,
                                     block);

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
                block->bbFlags |= (bbFlags & BBF_SPLIT_GAINED);
            }
            else if (argInfo.argIsByRefToStructLocal)
            {
                // Do nothing. Arg was directly substituted as we read
                // the inlinee.
            }
            else
            {
                /* The argument is either not used or a const or lcl var */

                noway_assert(!argInfo.argIsUsed || argInfo.argIsInvariant || argInfo.argIsLclVar);

                /* Make sure we didnt change argNode's along the way, or else
                   subsequent uses of the arg would have worked with the bashed value */
                if (argInfo.argIsInvariant)
                {
                    assert(argNode->OperIsConst() || argNode->gtOper == GT_ADDR);
                }
                noway_assert((argInfo.argIsLclVar == 0) ==
                             (argNode->gtOper != GT_LCL_VAR || (argNode->gtFlags & GTF_GLOB_REF)));

                /* If the argument has side effects, append it */

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
                        newStmt = gtNewStmt(gtUnusedValNode(argNode->AsOp()->gtOp1), callILOffset);
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
                        // (2) NYI. If, after tunneling through GT_RET_VALs, we find that
                        // the actual arg expression has no side effects, we can skip
                        // appending all together. This will help jit TP a bit.
                        //
                        // Chase through any GT_RET_EXPRs to find the actual argument
                        // expression.
                        GenTree* actualArgNode = argNode->gtRetExprVal(&bbFlags);

                        // For case (1)
                        //
                        // Look for the following tree shapes
                        // prejit: (IND (ADD (CONST, CALL(special dce helper...))))
                        // jit   : (COMMA (CALL(special dce helper...), (FIELD ...)))
                        if (actualArgNode->gtOper == GT_COMMA)
                        {
                            // Look for (COMMA (CALL(special dce helper...), (FIELD ...)))
                            GenTree* op1 = actualArgNode->AsOp()->gtOp1;
                            GenTree* op2 = actualArgNode->AsOp()->gtOp2;
                            if (op1->IsCall() &&
                                ((op1->AsCall()->gtCallMoreFlags & GTF_CALL_M_HELPER_SPECIAL_DCE) != 0) &&
                                (op2->gtOper == GT_FIELD) && ((op2->gtFlags & GTF_EXCEPT) == 0))
                            {
                                JITDUMP("\nPerforming special dce on unused arg [%06u]:"
                                        " actual arg [%06u] helper call [%06u]\n",
                                        argNode->gtTreeID, actualArgNode->gtTreeID, op1->gtTreeID);
                                // Drop the whole tree
                                append = false;
                            }
                        }
                        else if (actualArgNode->gtOper == GT_IND)
                        {
                            // Look for (IND (ADD (CONST, CALL(special dce helper...))))
                            GenTree* addr = actualArgNode->AsOp()->gtOp1;

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
                                            argNode->gtTreeID, actualArgNode->gtTreeID, op1->gtTreeID);
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
                            newStmt = gtNewStmt(gtUnusedValNode(argNode), callILOffset);
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

                block->bbFlags |= (bbFlags & BBF_SPLIT_GAINED);
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
        newStmt = gtNewStmt(tree, callILOffset);
        fgInsertStmtAfter(block, afterStmt, newStmt);
        afterStmt = newStmt;
    }

    // Insert the nullcheck statement now.
    if (nullcheck)
    {
        newStmt = gtNewStmt(nullcheck, callILOffset);
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
                    impAssignTempGen(tmpNum, gtNewZeroConNode(genActualType(lclTyp)), NO_CLASS_HANDLE,
                                     (unsigned)CHECK_SPILL_NONE, &afterStmt, callILOffset, block);
                }
                else
                {
                    tree = gtNewBlkOpNode(gtNewLclvNode(tmpNum, lclTyp), // Dest
                                          gtNewIconNode(0),              // Value
                                          false,                         // isVolatile
                                          false);                        // not copyBlock

                    newStmt = gtNewStmt(tree, callILOffset);
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

    // Update any newly added statements with the appropriate context.
    InlineContext* context = callStmt->GetInlineContext();
    assert(context != nullptr);
    for (Statement* addedStmt = callStmt->GetNextStmt(); addedStmt != postStmt; addedStmt = addedStmt->GetNextStmt())
    {
        assert(addedStmt->GetInlineContext() == nullptr);
        addedStmt->SetInlineContext(context);
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
    IL_OFFSETX           callILOffset      = callStmt->GetILOffsetX();
    CORINFO_METHOD_INFO* InlineeMethodInfo = InlineeCompiler->info.compMethodInfo;
    const unsigned       lclCnt            = InlineeMethodInfo->locals.numArgs;
    InlLclVarInfo*       lclVarInfo        = inlineInfo->lclVarInfo;
    unsigned             gcRefLclCnt       = inlineInfo->numberOfGcRefLocals;
    const unsigned       argCnt            = inlineInfo->argCnt;

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
        GenTree* retExpr = inlineInfo->retExpr;
        if (retExpr != nullptr)
        {
            const bool interferesWithReturn = gtHasRef(inlineInfo->retExpr, tmpNum, false);
            noway_assert(!interferesWithReturn);
        }

        // Assign null to the local.
        GenTree*   nullExpr = gtNewTempAssign(tmpNum, gtNewZeroConNode(lclTyp));
        Statement* nullStmt = gtNewStmt(nullExpr, callILOffset);

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
