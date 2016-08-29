// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =================================================================================
//  Code that works with liveness and related concepts (interference, debug scope)
// =================================================================================

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************
 *
 *  Helper for Compiler::fgPerBlockLocalVarLiveness().
 *  The goal is to compute the USE and DEF sets for a basic block.
 *  However with the new improvement to the data flow analysis (DFA),
 *  we do not mark x as used in x = f(x) when there are no side effects in f(x).
 *  'asgdLclVar' is set when 'tree' is part of an expression with no side-effects
 *  which is assigned to asgdLclVar, ie. asgdLclVar = (... tree ...)
 */
void Compiler::fgMarkUseDef(GenTreeLclVarCommon* tree, GenTree* asgdLclVar)
{
    bool       rhsUSEDEF = false;
    unsigned   lclNum;
    unsigned   lhsLclNum;
    LclVarDsc* varDsc;

    noway_assert(tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_VAR_ADDR || tree->gtOper == GT_LCL_FLD ||
                 tree->gtOper == GT_LCL_FLD_ADDR || tree->gtOper == GT_STORE_LCL_VAR ||
                 tree->gtOper == GT_STORE_LCL_FLD);

    if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_VAR_ADDR || tree->gtOper == GT_STORE_LCL_VAR)
    {
        lclNum = tree->gtLclNum;
    }
    else
    {
        noway_assert(tree->OperIsLocalField());
        lclNum = tree->gtLclFld.gtLclNum;
    }

    noway_assert(lclNum < lvaCount);
    varDsc = lvaTable + lclNum;

    // We should never encounter a reference to a lclVar that has a zero refCnt.
    if (varDsc->lvRefCnt == 0 && (!varTypeIsPromotable(varDsc) || !varDsc->lvPromoted))
    {
        JITDUMP("Found reference to V%02u with zero refCnt.\n", lclNum);
        assert(!"We should never encounter a reference to a lclVar that has a zero refCnt.");
        varDsc->lvRefCnt = 1;
    }

    // NOTE: the analysis done below is neither necessary nor correct for LIR: it depends on
    // the nodes that precede `asgdLclVar` in execution order to factor into the dataflow for the
    // value being assigned to the local var, which is not necessarily the case without tree
    // order. Furthermore, LIR is always traversed in an order that reflects the dataflow for the
    // block.
    if (asgdLclVar != nullptr)
    {
        assert(!compCurBB->IsLIR());

        /* we have an assignment to a local var : asgdLclVar = ... tree ...
         * check for x = f(x) case */

        noway_assert(asgdLclVar->gtOper == GT_LCL_VAR || asgdLclVar->gtOper == GT_STORE_LCL_VAR);
        noway_assert(asgdLclVar->gtFlags & GTF_VAR_DEF);

        lhsLclNum = asgdLclVar->gtLclVarCommon.gtLclNum;

        if ((lhsLclNum == lclNum) && ((tree->gtFlags & GTF_VAR_DEF) == 0) && (tree != asgdLclVar))
        {
            /* bingo - we have an x = f(x) case */
            noway_assert(lvaTable[lhsLclNum].lvType != TYP_STRUCT);
            asgdLclVar->gtFlags |= GTF_VAR_USEDEF;
            rhsUSEDEF = true;
        }
    }

    /* Is this a tracked variable? */

    if (varDsc->lvTracked)
    {
        noway_assert(varDsc->lvVarIndex < lvaTrackedCount);

        if ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & (GTF_VAR_USEASG | GTF_VAR_USEDEF)) == 0)
        {
            // if  (!(fgCurUseSet & bitMask)) printf("V%02u,T%02u def at %08p\n", lclNum, varDsc->lvVarIndex, tree);
            VarSetOps::AddElemD(this, fgCurDefSet, varDsc->lvVarIndex);
        }
        else
        {
            // if  (!(fgCurDefSet & bitMask))
            // {
            //      printf("V%02u,T%02u use at ", lclNum, varDsc->lvVarIndex);
            //      printTreeID(tree);
            //      printf("\n");
            // }

            /* We have the following scenarios:
             *   1. "x += something" - in this case x is flagged GTF_VAR_USEASG
             *   2. "x = ... x ..." - the LHS x is flagged GTF_VAR_USEDEF,
             *                        the RHS x is has rhsUSEDEF = true
             *                        (both set by the code above)
             *
             * We should not mark an USE of x in the above cases provided the value "x" is not used
             * further up in the tree. For example "while (i++)" is required to mark i as used.
             */

            /* make sure we don't include USEDEF variables in the USE set
             * The first test is for LSH, the second (!rhsUSEDEF) is for any var in the RHS */

            if ((tree->gtFlags & (GTF_VAR_USEASG | GTF_VAR_USEDEF)) == 0)
            {
                /* Not a special flag - check to see if used to assign to itself */

                if (rhsUSEDEF)
                {
                    /* assign to itself - do not include it in the USE set */
                    if (!opts.MinOpts() && !opts.compDbgCode)
                    {
                        return;
                    }
                }
            }

            /* Fall through for the "good" cases above - add the variable to the USE set */

            if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
            {
                VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
            }

            // For defs, also add to the (all) def set.
            if ((tree->gtFlags & GTF_VAR_DEF) != 0)
            {
                VarSetOps::AddElemD(this, fgCurDefSet, varDsc->lvVarIndex);
            }
        }
    }
    else if (varTypeIsStruct(varDsc))
    {
        noway_assert(!varDsc->lvTracked);

        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType != PROMOTION_TYPE_NONE)
        {
            VARSET_TP VARSET_INIT_NOCOPY(bitMask, VarSetOps::MakeEmpty(this));

            for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
            {
                noway_assert(lvaTable[i].lvIsStructField);
                if (lvaTable[i].lvTracked)
                {
                    noway_assert(lvaTable[i].lvVarIndex < lvaTrackedCount);
                    VarSetOps::AddElemD(this, bitMask, lvaTable[i].lvVarIndex);
                }
            }

            // For pure defs (i.e. not an "update" def which is also a use), add to the (all) def set.
            if ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & (GTF_VAR_USEASG | GTF_VAR_USEDEF)) == 0)
            {
                VarSetOps::UnionD(this, fgCurDefSet, bitMask);
            }
            else if (!VarSetOps::IsSubset(this, bitMask, fgCurDefSet))
            {
                // Mark as used any struct fields that are not yet defined.
                VarSetOps::UnionD(this, fgCurUseSet, bitMask);
            }
        }
    }
}

/*****************************************************************************/
void Compiler::fgLocalVarLiveness()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgLocalVarLiveness()\n");

#ifndef LEGACY_BACKEND
        if (compRationalIRForm)
        {
            lvaTableDump();
        }
#endif // !LEGACY_BACKEND
    }
#endif // DEBUG

    // Init liveness data structures.
    fgLocalVarLivenessInit();
    assert(lvaSortAgain == false); // Set to false by lvaSortOnly()

    EndPhase(PHASE_LCLVARLIVENESS_INIT);

    // Make sure we haven't noted any partial last uses of promoted structs.
    GetPromotedStructDeathVars()->RemoveAll();

    // Initialize the per-block var sets.
    fgInitBlockVarSets();

    fgLocalVarLivenessChanged = false;
    do
    {
        /* Figure out use/def info for all basic blocks */
        fgPerBlockLocalVarLiveness();
        EndPhase(PHASE_LCLVARLIVENESS_PERBLOCK);

        /* Live variable analysis. */

        fgStmtRemoved = false;
        fgInterBlockLocalVarLiveness();
    } while (fgStmtRemoved && fgLocalVarLivenessChanged);

    // If we removed any dead code we will have set 'lvaSortAgain' via decRefCnts
    if (lvaSortAgain)
    {
        JITDUMP("In fgLocalVarLiveness, setting lvaSortAgain back to false (set during dead-code removal)\n");
        lvaSortAgain = false; // We don't re-Sort because we just performed LclVar liveness.
    }

    EndPhase(PHASE_LCLVARLIVENESS_INTERBLOCK);
}

/*****************************************************************************/
void Compiler::fgLocalVarLivenessInit()
{
    // If necessary, re-sort the variable table by ref-count...before creating any varsets using this sorting.
    if (lvaSortAgain)
    {
        JITDUMP("In fgLocalVarLivenessInit, sorting locals\n");
        lvaSortByRefCount();
        assert(lvaSortAgain == false); // Set to false by lvaSortOnly()
    }

#ifdef LEGACY_BACKEND // RyuJIT backend does not use interference info

    for (unsigned i = 0; i < lclMAX_TRACKED; i++)
    {
        VarSetOps::AssignNoCopy(this, lvaVarIntf[i], VarSetOps::MakeEmpty(this));
    }

    /* If we're not optimizing at all, things are simple */
    if (opts.MinOpts())
    {
        VARSET_TP VARSET_INIT_NOCOPY(allOnes, VarSetOps::MakeFull(this));
        for (unsigned i = 0; i < lvaTrackedCount; i++)
        {
            VarSetOps::Assign(this, lvaVarIntf[i], allOnes);
        }
        return;
    }
#endif // LEGACY_BACKEND

    // We mark a lcl as must-init in a first pass of local variable
    // liveness (Liveness1), then assertion prop eliminates the
    // uninit-use of a variable Vk, asserting it will be init'ed to
    // null.  Then, in a second local-var liveness (Liveness2), the
    // variable Vk is no longer live on entry to the method, since its
    // uses have been replaced via constant propagation.
    //
    // This leads to a bug: since Vk is no longer live on entry, the
    // register allocator sees Vk and an argument Vj as having
    // disjoint lifetimes, and allocates them to the same register.
    // But Vk is still marked "must-init", and this initialization (of
    // the register) trashes the value in Vj.
    //
    // Therefore, initialize must-init to false for all variables in
    // each liveness phase.
    for (unsigned lclNum = 0; lclNum < lvaCount; ++lclNum)
    {
        lvaTable[lclNum].lvMustInit = false;
    }
}

// Note that for the LEGACY_BACKEND this method is replaced with
// fgLegacyPerStatementLocalVarLiveness and it lives in codegenlegacy.cpp
//
#ifndef LEGACY_BACKEND
//------------------------------------------------------------------------
// fgPerNodeLocalVarLiveness:
//   Set fgCurHeapUse and fgCurHeapDef when the global heap is read or updated
//   Call fgMarkUseDef for any Local variables encountered
//
// Arguments:
//    tree       - The current node.
//    asgdLclVar - Either nullptr or the assignement's left-hand-side GT_LCL_VAR.
//                 Used as an argument to fgMarkUseDef(); only valid for HIR blocks.
//
void Compiler::fgPerNodeLocalVarLiveness(GenTree* tree, GenTree* asgdLclVar)
{
    assert(tree != nullptr);
    assert(asgdLclVar == nullptr || !compCurBB->IsLIR());

    switch (tree->gtOper)
    {
        case GT_QMARK:
        case GT_COLON:
            // We never should encounter a GT_QMARK or GT_COLON node
            noway_assert(!"unexpected GT_QMARK/GT_COLON");
            break;

        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_VAR_ADDR:
        case GT_LCL_FLD_ADDR:
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            fgMarkUseDef(tree->AsLclVarCommon(), asgdLclVar);
            break;

        case GT_CLS_VAR:
            // For Volatile indirection, first mutate the global heap
            // see comments in ValueNum.cpp (under case GT_CLS_VAR)
            // This models Volatile reads as def-then-use of the heap.
            // and allows for a CSE of a subsequent non-volatile read
            if ((tree->gtFlags & GTF_FLD_VOLATILE) != 0)
            {
                // For any Volatile indirection, we must handle it as a
                // definition of the global heap
                fgCurHeapDef = true;
            }
            // If the GT_CLS_VAR is the lhs of an assignment, we'll handle it as a heap def, when we get to assignment.
            // Otherwise, we treat it as a use here.
            if (!fgCurHeapDef && (tree->gtFlags & GTF_CLS_VAR_ASG_LHS) == 0)
            {
                fgCurHeapUse = true;
            }
            break;

        case GT_IND:
            // For Volatile indirection, first mutate the global heap
            // see comments in ValueNum.cpp (under case GT_CLS_VAR)
            // This models Volatile reads as def-then-use of the heap.
            // and allows for a CSE of a subsequent non-volatile read
            if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
            {
                // For any Volatile indirection, we must handle it as a
                // definition of the global heap
                fgCurHeapDef = true;
            }

            // If the GT_IND is the lhs of an assignment, we'll handle it
            // as a heap def, when we get to assignment.
            // Otherwise, we treat it as a use here.
            if ((tree->gtFlags & GTF_IND_ASG_LHS) == 0)
            {
                GenTreeLclVarCommon* dummyLclVarTree = nullptr;
                bool                 dummyIsEntire   = false;
                GenTreePtr           addrArg         = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
                if (!addrArg->DefinesLocalAddr(this, /*width doesn't matter*/ 0, &dummyLclVarTree, &dummyIsEntire))
                {
                    if (!fgCurHeapDef)
                    {
                        fgCurHeapUse = true;
                    }
                }
                else
                {
                    // Defines a local addr
                    assert(dummyLclVarTree != nullptr);
                    fgMarkUseDef(dummyLclVarTree->AsLclVarCommon(), asgdLclVar);
                }
            }
            break;

        // These should have been morphed away to become GT_INDs:
        case GT_FIELD:
        case GT_INDEX:
            unreached();
            break;

        // We'll assume these are use-then-defs of the heap.
        case GT_LOCKADD:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
            if (!fgCurHeapDef)
            {
                fgCurHeapUse = true;
            }
            fgCurHeapDef   = true;
            fgCurHeapHavoc = true;
            break;

        case GT_MEMORYBARRIER:
            // Simliar to any Volatile indirection, we must handle this as a definition of the global heap
            fgCurHeapDef = true;
            break;

        // For now, all calls read/write the heap, the latter in its entirety.  Might tighten this case later.
        case GT_CALL:
        {
            GenTreeCall* call    = tree->AsCall();
            bool         modHeap = true;
            if (call->gtCallType == CT_HELPER)
            {
                CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);

                if (!s_helperCallProperties.MutatesHeap(helpFunc) && !s_helperCallProperties.MayRunCctor(helpFunc))
                {
                    modHeap = false;
                }
            }
            if (modHeap)
            {
                if (!fgCurHeapDef)
                {
                    fgCurHeapUse = true;
                }
                fgCurHeapDef   = true;
                fgCurHeapHavoc = true;
            }
        }

            // If this is a p/invoke unmanaged call or if this is a tail-call
            // and we have an unmanaged p/invoke call in the method,
            // then we're going to run the p/invoke epilog.
            // So we mark the FrameRoot as used by this instruction.
            // This ensures that the block->bbVarUse will contain
            // the FrameRoot local var if is it a tracked variable.

            if ((tree->gtCall.IsUnmanaged() || (tree->gtCall.IsTailCall() && info.compCallUnmanaged)))
            {
                assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
                if (!opts.ShouldUsePInvokeHelpers())
                {
                    /* Get the TCB local and mark it as used */

                    noway_assert(info.compLvFrameListRoot < lvaCount);

                    LclVarDsc* varDsc = &lvaTable[info.compLvFrameListRoot];

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
                        }
                    }
                }
            }

            break;

        default:

            // Determine whether it defines a heap location.
            if (tree->OperIsAssignment() || tree->OperIsBlkOp())
            {
                GenTreeLclVarCommon* dummyLclVarTree = nullptr;
                if (!tree->DefinesLocal(this, &dummyLclVarTree))
                {
                    // If it doesn't define a local, then it might update the heap.
                    fgCurHeapDef = true;
                }
            }
            break;
    }
}

void Compiler::fgPerStatementLocalVarLiveness(GenTree* startNode, GenTree* asgdLclVar)
{
    // The startNode must be the 1st node of the statement.
    assert(startNode == compCurStmt->gtStmt.gtStmtList);

    // The asgdLclVar node must be either nullptr or a GT_LCL_VAR or GT_STORE_LCL_VAR
    assert((asgdLclVar == nullptr) || (asgdLclVar->gtOper == GT_LCL_VAR || asgdLclVar->gtOper == GT_STORE_LCL_VAR));

    // We always walk every node in statement list
    for (GenTreePtr node = startNode; node != nullptr; node = node->gtNext)
    {
        fgPerNodeLocalVarLiveness(node, asgdLclVar);
    }
}

#endif // !LEGACY_BACKEND

/*****************************************************************************/
void Compiler::fgPerBlockLocalVarLiveness()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgPerBlockLocalVarLiveness()\n");
    }
#endif // DEBUG

    BasicBlock* block;

#if CAN_DISABLE_DFA

    /* If we're not optimizing at all, things are simple */

    if (opts.MinOpts())
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        VARSET_TP VARSET_INIT_NOCOPY(liveAll, VarSetOps::MakeEmpty(this));

        /* We simply make everything live everywhere */

        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            if (varDsc->lvTracked)
            {
                VarSetOps::AddElemD(this, liveAll, varDsc->lvVarIndex);
            }
        }

        for (block = fgFirstBB; block; block = block->bbNext)
        {
            // Strictly speaking, the assignments for the "Def" cases aren't necessary here.
            // The empty set would do as well.  Use means "use-before-def", so as long as that's
            // "all", this has the right effect.
            VarSetOps::Assign(this, block->bbVarUse, liveAll);
            VarSetOps::Assign(this, block->bbVarDef, liveAll);
            VarSetOps::Assign(this, block->bbLiveIn, liveAll);
            VarSetOps::Assign(this, block->bbLiveOut, liveAll);
            block->bbHeapUse     = true;
            block->bbHeapDef     = true;
            block->bbHeapLiveIn  = true;
            block->bbHeapLiveOut = true;

            switch (block->bbJumpKind)
            {
                case BBJ_EHFINALLYRET:
                case BBJ_THROW:
                case BBJ_RETURN:
                    VarSetOps::AssignNoCopy(this, block->bbLiveOut, VarSetOps::MakeEmpty(this));
                    break;
                default:
                    break;
            }
        }
        return;
    }

#endif // CAN_DISABLE_DFA

    // Avoid allocations in the long case.
    VarSetOps::AssignNoCopy(this, fgCurUseSet, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, fgCurDefSet, VarSetOps::MakeEmpty(this));

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr stmt;
        GenTreePtr tree;
        GenTreePtr asgdLclVar;

        VarSetOps::ClearD(this, fgCurUseSet);
        VarSetOps::ClearD(this, fgCurDefSet);

        fgCurHeapUse   = false;
        fgCurHeapDef   = false;
        fgCurHeapHavoc = false;

        compCurBB = block;

        if (!block->IsLIR())
        {
            for (stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
            {
                noway_assert(stmt->gtOper == GT_STMT);

                compCurStmt = stmt;

                asgdLclVar = nullptr;
                tree       = stmt->gtStmt.gtStmtExpr;
                noway_assert(tree);

                // The following code checks if we have an assignment expression
                // which may become a GTF_VAR_USEDEF - x=f(x).
                // consider if LHS is local var - ignore if RHS contains SIDE_EFFECTS

                if ((tree->gtOper == GT_ASG && tree->gtOp.gtOp1->gtOper == GT_LCL_VAR) ||
                    tree->gtOper == GT_STORE_LCL_VAR)
                {
                    noway_assert(tree->gtOp.gtOp1);
                    GenTreePtr rhsNode;
                    if (tree->gtOper == GT_ASG)
                    {
                        noway_assert(tree->gtOp.gtOp2);
                        asgdLclVar = tree->gtOp.gtOp1;
                        rhsNode    = tree->gtOp.gtOp2;
                    }
                    else
                    {
                        asgdLclVar = tree;
                        rhsNode    = tree->gtOp.gtOp1;
                    }

                    // If this is an assignment to local var with no SIDE EFFECTS,
                    // set asgdLclVar so that genMarkUseDef will flag potential
                    // x=f(x) expressions as GTF_VAR_USEDEF.
                    // Reset the flag before recomputing it - it may have been set before,
                    // but subsequent optimizations could have removed the rhs reference.
                    asgdLclVar->gtFlags &= ~GTF_VAR_USEDEF;
                    if ((rhsNode->gtFlags & GTF_SIDE_EFFECT) == 0)
                    {
                        noway_assert(asgdLclVar->gtFlags & GTF_VAR_DEF);
                    }
                    else
                    {
                        asgdLclVar = nullptr;
                    }
                }

#ifdef LEGACY_BACKEND
                tree = fgLegacyPerStatementLocalVarLiveness(stmt->gtStmt.gtStmtList, NULL, asgdLclVar);

                // We must have walked to the end of this statement.
                noway_assert(!tree);
#else  // !LEGACY_BACKEND
                fgPerStatementLocalVarLiveness(stmt->gtStmt.gtStmtList, asgdLclVar);
#endif // !LEGACY_BACKEND
            }
        }
        else
        {
#ifdef LEGACY_BACKEND
            unreached();
#else  // !LEGACY_BACKEND
            // NOTE: the `asgdLclVar` analysis done above is not correct for LIR: it depends
            // on all of the nodes that precede `asgdLclVar` in execution order to factor into the
            // dataflow for the value being assigned to the local var, which is not necessarily the
            // case without tree order. As a result, we simply pass `nullptr` for `asgdLclVar`.
            for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
            {
                fgPerNodeLocalVarLiveness(node, nullptr);
            }
#endif // !LEGACY_BACKEND
        }

        /* Get the TCB local and mark it as used */

        if (block->bbJumpKind == BBJ_RETURN && info.compCallUnmanaged)
        {
            assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
            if (!opts.ShouldUsePInvokeHelpers())
            {
                noway_assert(info.compLvFrameListRoot < lvaCount);

                LclVarDsc* varDsc = &lvaTable[info.compLvFrameListRoot];

                if (varDsc->lvTracked)
                {
                    if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
                    {
                        VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
                    }
                }
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            VARSET_TP VARSET_INIT_NOCOPY(allVars, VarSetOps::Union(this, fgCurUseSet, fgCurDefSet));
            printf("BB%02u", block->bbNum);
            printf(" USE(%d)=", VarSetOps::Count(this, fgCurUseSet));
            lvaDispVarSet(fgCurUseSet, allVars);
            if (fgCurHeapUse)
            {
                printf(" + HEAP");
            }
            printf("\n     DEF(%d)=", VarSetOps::Count(this, fgCurDefSet));
            lvaDispVarSet(fgCurDefSet, allVars);
            if (fgCurHeapDef)
            {
                printf(" + HEAP");
            }
            if (fgCurHeapHavoc)
            {
                printf("*");
            }
            printf("\n\n");
        }
#endif // DEBUG

        VarSetOps::Assign(this, block->bbVarUse, fgCurUseSet);
        VarSetOps::Assign(this, block->bbVarDef, fgCurDefSet);
        block->bbHeapUse   = fgCurHeapUse;
        block->bbHeapDef   = fgCurHeapDef;
        block->bbHeapHavoc = fgCurHeapHavoc;

        /* also initialize the IN set, just in case we will do multiple DFAs */

        VarSetOps::AssignNoCopy(this, block->bbLiveIn, VarSetOps::MakeEmpty(this));
        block->bbHeapLiveIn = false;
    }
}

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************/

// Helper functions to mark variables live over their entire scope

void Compiler::fgBeginScopeLife(VARSET_TP* inScope, VarScopeDsc* var)
{
    assert(var);

    LclVarDsc* lclVarDsc1 = &lvaTable[var->vsdVarNum];

    if (lclVarDsc1->lvTracked)
    {
        VarSetOps::AddElemD(this, *inScope, lclVarDsc1->lvVarIndex);
    }
}

void Compiler::fgEndScopeLife(VARSET_TP* inScope, VarScopeDsc* var)
{
    assert(var);

    LclVarDsc* lclVarDsc1 = &lvaTable[var->vsdVarNum];

    if (lclVarDsc1->lvTracked)
    {
        VarSetOps::RemoveElemD(this, *inScope, lclVarDsc1->lvVarIndex);
    }
}

/*****************************************************************************/

void Compiler::fgMarkInScope(BasicBlock* block, VARSET_VALARG_TP inScope)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Scope info: block BB%02u marking in scope: ", block->bbNum);
        dumpConvertedVarSet(this, inScope);
        printf("\n");
    }
#endif // DEBUG

    /* Record which vars are artifically kept alive for debugging */

    VarSetOps::Assign(this, block->bbScope, inScope);

    /* Being in scope implies a use of the variable. Add the var to bbVarUse
       so that redoing fgLiveVarAnalysis() will work correctly */

    VarSetOps::UnionD(this, block->bbVarUse, inScope);

    /* Artifically mark all vars in scope as alive */

    VarSetOps::UnionD(this, block->bbLiveIn, inScope);
    VarSetOps::UnionD(this, block->bbLiveOut, inScope);
}

void Compiler::fgUnmarkInScope(BasicBlock* block, VARSET_VALARG_TP unmarkScope)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Scope info: block BB%02u UNmarking in scope: ", block->bbNum);
        dumpConvertedVarSet(this, unmarkScope);
        printf("\n");
    }
#endif // DEBUG

    assert(VarSetOps::IsSubset(this, unmarkScope, block->bbScope));

    VarSetOps::DiffD(this, block->bbScope, unmarkScope);
    VarSetOps::DiffD(this, block->bbVarUse, unmarkScope);
    VarSetOps::DiffD(this, block->bbLiveIn, unmarkScope);
    VarSetOps::DiffD(this, block->bbLiveOut, unmarkScope);
}

#ifdef DEBUG

void Compiler::fgDispDebugScopes()
{
    printf("\nDebug scopes:\n");

    BasicBlock* block;
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        printf("BB%02u: ", block->bbNum);
        dumpConvertedVarSet(this, block->bbScope);
        printf("\n");
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 * Mark variables live across their entire scope.
 */

#if FEATURE_EH_FUNCLETS

void Compiler::fgExtendDbgScopes()
{
    compResetScopeLists();

#ifdef DEBUG
    if (verbose)
    {
        printf("\nMarking vars alive over their entire scope :\n\n");
    }

    if (verbose)
    {
        compDispScopeLists();
    }
#endif // DEBUG

    VARSET_TP VARSET_INIT_NOCOPY(inScope, VarSetOps::MakeEmpty(this));

    // Mark all tracked LocalVars live over their scope - walk the blocks
    // keeping track of the current life, and assign it to the blocks.

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        // If we get to a funclet, reset the scope lists and start again, since the block
        // offsets will be out of order compared to the previous block.

        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            compResetScopeLists();
            VarSetOps::ClearD(this, inScope);
        }

        // Process all scopes up to the current offset

        if (block->bbCodeOffs != BAD_IL_OFFSET)
        {
            compProcessScopesUntil(block->bbCodeOffs, &inScope, &Compiler::fgBeginScopeLife, &Compiler::fgEndScopeLife);
        }

        // Assign the current set of variables that are in scope to the block variables tracking this.

        fgMarkInScope(block, inScope);
    }

#ifdef DEBUG
    if (verbose)
    {
        fgDispDebugScopes();
    }
#endif // DEBUG
}

#else // !FEATURE_EH_FUNCLETS

void Compiler::fgExtendDbgScopes()
{
    compResetScopeLists();

#ifdef DEBUG
    if (verbose)
    {
        printf("\nMarking vars alive over their entire scope :\n\n");
        compDispScopeLists();
    }
#endif // DEBUG

    VARSET_TP VARSET_INIT_NOCOPY(inScope, VarSetOps::MakeEmpty(this));
    compProcessScopesUntil(0, &inScope, &Compiler::fgBeginScopeLife, &Compiler::fgEndScopeLife);

    IL_OFFSET lastEndOffs = 0;

    // Mark all tracked LocalVars live over their scope - walk the blocks
    // keeping track of the current life, and assign it to the blocks.

    BasicBlock* block;
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        // Find scopes becoming alive. If there is a gap in the instr
        // sequence, we need to process any scopes on those missing offsets.

        if (block->bbCodeOffs != BAD_IL_OFFSET)
        {
            if (lastEndOffs != block->bbCodeOffs)
            {
                noway_assert(lastEndOffs < block->bbCodeOffs);

                compProcessScopesUntil(block->bbCodeOffs, &inScope, &Compiler::fgBeginScopeLife,
                                       &Compiler::fgEndScopeLife);
            }
            else
            {
                while (VarScopeDsc* varScope = compGetNextEnterScope(block->bbCodeOffs))
                {
                    fgBeginScopeLife(&inScope, varScope);
                }
            }
        }

        // Assign the current set of variables that are in scope to the block variables tracking this.

        fgMarkInScope(block, inScope);

        // Find scopes going dead.

        if (block->bbCodeOffsEnd != BAD_IL_OFFSET)
        {
            VarScopeDsc* varScope;
            while ((varScope = compGetNextExitScope(block->bbCodeOffsEnd)) != nullptr)
            {
                fgEndScopeLife(&inScope, varScope);
            }

            lastEndOffs = block->bbCodeOffsEnd;
        }
    }

    /* Everything should be out of scope by the end of the method. But if the
       last BB got removed, then inScope may not be empty. */

    noway_assert(VarSetOps::IsEmpty(this, inScope) || lastEndOffs < info.compILCodeSize);
}

#endif // !FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 * For debuggable code, we allow redundant assignments to vars
 * by marking them live over their entire scope.
 */

void Compiler::fgExtendDbgLifetimes()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgExtendDbgLifetimes()\n");
    }
#endif // DEBUG

    noway_assert(opts.compDbgCode && (info.compVarScopesCount > 0));

    /*-------------------------------------------------------------------------
     *   Extend the lifetimes over the entire reported scope of the variable.
     */

    fgExtendDbgScopes();

/*-------------------------------------------------------------------------
 * Partly update liveness info so that we handle any funky BBF_INTERNAL
 * blocks inserted out of sequence.
 */

#ifdef DEBUG
    if (verbose && 0)
    {
        fgDispBBLiveness();
    }
#endif

    fgLiveVarAnalysis(true);

    /* For compDbgCode, we prepend an empty BB which will hold the
       initializations of variables which are in scope at IL offset 0 (but
       not initialized by the IL code). Since they will currently be
       marked as live on entry to fgFirstBB, unmark the liveness so that
       the following code will know to add the initializations. */

    assert(fgFirstBBisScratch());

    VARSET_TP VARSET_INIT_NOCOPY(trackedArgs, VarSetOps::MakeEmpty(this));

    for (unsigned argNum = 0; argNum < info.compArgsCount; argNum++)
    {
        LclVarDsc* argDsc = lvaTable + argNum;
        if (argDsc->lvPromoted)
        {
            lvaPromotionType promotionType = lvaGetPromotionType(argDsc);

            if (promotionType == PROMOTION_TYPE_INDEPENDENT)
            {
                noway_assert(argDsc->lvFieldCnt == 1); // We only handle one field here

                unsigned fieldVarNum = argDsc->lvFieldLclStart;
                argDsc               = lvaTable + fieldVarNum;
            }
        }
        noway_assert(argDsc->lvIsParam);
        if (argDsc->lvTracked)
        {
            noway_assert(!VarSetOps::IsMember(this, trackedArgs, argDsc->lvVarIndex)); // Each arg should define a
                                                                                       // different bit.
            VarSetOps::AddElemD(this, trackedArgs, argDsc->lvVarIndex);
        }
    }

    // Don't unmark struct locals, either.
    VARSET_TP VARSET_INIT_NOCOPY(noUnmarkVars, trackedArgs);

    for (unsigned i = 0; i < lvaCount; i++)
    {
        LclVarDsc* varDsc = &lvaTable[i];
        if (varTypeIsStruct(varDsc) && varDsc->lvTracked)
        {
            VarSetOps::AddElemD(this, noUnmarkVars, varDsc->lvVarIndex);
        }
    }
    fgUnmarkInScope(fgFirstBB, VarSetOps::Diff(this, fgFirstBB->bbScope, noUnmarkVars));

    /*-------------------------------------------------------------------------
     * As we keep variables artifically alive over their entire scope,
     * we need to also artificially initialize them if the scope does
     * not exactly match the real lifetimes, or they will contain
     * garbage until they are initialized by the IL code.
     */

    VARSET_TP VARSET_INIT_NOCOPY(initVars, VarSetOps::MakeEmpty(this)); // Vars which are artificially made alive

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        VarSetOps::ClearD(this, initVars);

        switch (block->bbJumpKind)
        {
            case BBJ_NONE:
                PREFIX_ASSUME(block->bbNext != nullptr);
                VarSetOps::UnionD(this, initVars, block->bbNext->bbScope);
                break;

            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
                VarSetOps::UnionD(this, initVars, block->bbJumpDest->bbScope);
                break;

            case BBJ_CALLFINALLY:
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());
                    PREFIX_ASSUME(block->bbNext != nullptr);
                    VarSetOps::UnionD(this, initVars, block->bbNext->bbScope);
                }
                VarSetOps::UnionD(this, initVars, block->bbJumpDest->bbScope);
                break;

            case BBJ_COND:
                PREFIX_ASSUME(block->bbNext != nullptr);
                VarSetOps::UnionD(this, initVars, block->bbNext->bbScope);
                VarSetOps::UnionD(this, initVars, block->bbJumpDest->bbScope);
                break;

            case BBJ_SWITCH:
            {
                BasicBlock** jmpTab;
                unsigned     jmpCnt;

                jmpCnt = block->bbJumpSwt->bbsCount;
                jmpTab = block->bbJumpSwt->bbsDstTab;

                do
                {
                    VarSetOps::UnionD(this, initVars, (*jmpTab)->bbScope);
                } while (++jmpTab, --jmpCnt);
            }
            break;

            case BBJ_EHFINALLYRET:
            case BBJ_RETURN:
                break;

            case BBJ_THROW:
                /* We don't have to do anything as we mark
                 * all vars live on entry to a catch handler as
                 * volatile anyway
                 */
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

        /* If the var is already live on entry to the current BB,
           we would have already initialized it. So ignore bbLiveIn */

        VarSetOps::DiffD(this, initVars, block->bbLiveIn);

        /* Add statements initializing the vars, if there are any to initialize */
        VARSET_ITER_INIT(this, iter, initVars, varIndex);
        while (iter.NextElem(this, &varIndex))
        {
            /* Create initialization tree */

            unsigned   varNum = lvaTrackedToVarNum[varIndex];
            LclVarDsc* varDsc = &lvaTable[varNum];
            var_types  type   = varDsc->TypeGet();

            // Don't extend struct lifetimes -- they aren't enregistered, anyway.
            if (type == TYP_STRUCT)
            {
                continue;
            }

            // TODO-LIR: the code below does not work for blocks that contain LIR. As a result,
            //           we must run liveness at least once before any LIR is created in order
            //           to ensure that this code doesn't attempt to insert HIR into LIR blocks.

            // If we haven't already done this ...
            if (!fgLocalVarLivenessDone)
            {
                assert(!block->IsLIR());

                // Create a "zero" node

                GenTreePtr zero = gtNewZeroConNode(genActualType(type));

                // Create initialization node

                GenTreePtr varNode  = gtNewLclvNode(varNum, type);
                GenTreePtr initNode = gtNewAssignNode(varNode, zero);
                GenTreePtr initStmt = gtNewStmt(initNode);

                gtSetStmtInfo(initStmt);

                /* Assign numbers and next/prev links for this tree */

                fgSetStmtSeq(initStmt);

                /* Finally append the statement to the current BB */

                fgInsertStmtNearEnd(block, initStmt);

#ifdef DEBUG
                if (verbose)
                {
                    printf("Created zero-init of V%02u in BB%02u\n", varNum, block->bbNum);
                }
#endif // DEBUG

                varDsc->incRefCnts(block->getBBWeight(this), this);
            }

            /* Update liveness information so that redoing fgLiveVarAnalysis()
               will work correctly if needed */

            VarSetOps::AddElemD(this, block->bbVarDef, varIndex);
            VarSetOps::AddElemD(this, block->bbLiveOut, varIndex);
            block->bbFlags |= BBF_CHANGED; // indicates that the liveness info has changed
        }
    }

    // raMarkStkVars() reserves stack space for unused variables (which
    //   needs to be initialized). However, arguments don't need to be initialized.
    //   So just ensure that they don't have a 0 ref cnt

    unsigned lclNum = 0;
    for (LclVarDsc *varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        if (varDsc->lvRefCnt == 0 && varDsc->lvIsRegArg)
        {
            varDsc->lvRefCnt = 1;
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBB liveness after fgExtendDbgLifetimes():\n\n");
        fgDispBBLiveness();
        printf("\n");
    }
#endif // DEBUG
}

/*****************************************************************************/
#endif // DEBUGGING_SUPPORT
/*****************************************************************************/

VARSET_VALRET_TP Compiler::fgGetHandlerLiveVars(BasicBlock* block)
{
    noway_assert(block);
    noway_assert(ehBlockHasExnFlowDsc(block));

    VARSET_TP VARSET_INIT_NOCOPY(liveVars, VarSetOps::MakeEmpty(this));
    EHblkDsc* HBtab = ehGetBlockExnFlowDsc(block);

    do
    {
        /* Either we enter the filter first or the catch/finally */

        if (HBtab->HasFilter())
        {
            VarSetOps::UnionD(this, liveVars, HBtab->ebdFilter->bbLiveIn);
#if FEATURE_EH_FUNCLETS
            // The EH subsystem can trigger a stack walk after the filter
            // has returned, but before invoking the handler, and the only
            // IP address reported from this method will be the original
            // faulting instruction, thus everything in the try body
            // must report as live any variables live-out of the filter
            // (which is the same as those live-in to the handler)
            VarSetOps::UnionD(this, liveVars, HBtab->ebdHndBeg->bbLiveIn);
#endif // FEATURE_EH_FUNCLETS
        }
        else
        {
            VarSetOps::UnionD(this, liveVars, HBtab->ebdHndBeg->bbLiveIn);
        }

        /* If we have nested try's edbEnclosing will provide them */
        noway_assert((HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) ||
                     (HBtab->ebdEnclosingTryIndex > ehGetIndex(HBtab)));

        unsigned outerIndex = HBtab->ebdEnclosingTryIndex;
        if (outerIndex == EHblkDsc::NO_ENCLOSING_INDEX)
        {
            break;
        }
        HBtab = ehGetDsc(outerIndex);

    } while (true);

    return liveVars;
}

/*****************************************************************************
 *
 *  This is the classic algorithm for Live Variable Analysis.
 *  If updateInternalOnly==true, only update BBF_INTERNAL blocks.
 */

void Compiler::fgLiveVarAnalysis(bool updateInternalOnly)
{
    BasicBlock* block;
    bool        change;
#ifdef DEBUG
    VARSET_TP VARSET_INIT_NOCOPY(extraLiveOutFromFinally, VarSetOps::MakeEmpty(this));
#endif // DEBUG
    bool keepAliveThis = lvaKeepAliveAndReportThis() && lvaTable[info.compThisArg].lvTracked;

    /* Live Variable Analysis - Backward dataflow */

    bool hasPossibleBackEdge = false;

    do
    {
        change = false;

        /* Visit all blocks and compute new data flow values */

        VARSET_TP VARSET_INIT_NOCOPY(liveIn, VarSetOps::MakeEmpty(this));
        VARSET_TP VARSET_INIT_NOCOPY(liveOut, VarSetOps::MakeEmpty(this));

        bool heapLiveIn  = false;
        bool heapLiveOut = false;

        for (block = fgLastBB; block; block = block->bbPrev)
        {
            // sometimes block numbers are not monotonically increasing which
            // would cause us not to identify backedges
            if (block->bbNext && block->bbNext->bbNum <= block->bbNum)
            {
                hasPossibleBackEdge = true;
            }

            if (updateInternalOnly)
            {
                /* Only update BBF_INTERNAL blocks as they may be
                   syntactically out of sequence. */

                noway_assert(opts.compDbgCode && (info.compVarScopesCount > 0));

                if (!(block->bbFlags & BBF_INTERNAL))
                {
                    continue;
                }
            }

            /* Compute the 'liveOut' set */

            VarSetOps::ClearD(this, liveOut);
            heapLiveOut = false;
            if (block->endsWithJmpMethod(this))
            {
                // A JMP uses all the arguments, so mark them all
                // as live at the JMP instruction
                //
                const LclVarDsc* varDscEndParams = lvaTable + info.compArgsCount;
                for (LclVarDsc* varDsc = lvaTable; varDsc < varDscEndParams; varDsc++)
                {
                    noway_assert(!varDsc->lvPromoted);
                    if (varDsc->lvTracked)
                    {
                        VarSetOps::AddElemD(this, liveOut, varDsc->lvVarIndex);
                    }
                }
            }

            // Additionally, union in all the live-in tracked vars of successors.
            AllSuccessorIter succsEnd = block->GetAllSuccs(this).end();
            for (AllSuccessorIter succs = block->GetAllSuccs(this).begin(); succs != succsEnd; ++succs)
            {
                BasicBlock* succ = (*succs);
                VarSetOps::UnionD(this, liveOut, succ->bbLiveIn);
                heapLiveOut = heapLiveOut || (*succs)->bbHeapLiveIn;
                if (succ->bbNum <= block->bbNum)
                {
                    hasPossibleBackEdge = true;
                }
            }

            /* For lvaKeepAliveAndReportThis methods, "this" has to be kept alive everywhere
               Note that a function may end in a throw on an infinite loop (as opposed to a return).
               "this" has to be alive everywhere even in such methods. */

            if (keepAliveThis)
            {
                VarSetOps::AddElemD(this, liveOut, lvaTable[info.compThisArg].lvVarIndex);
            }

            /* Compute the 'liveIn'  set */

            VarSetOps::Assign(this, liveIn, liveOut);
            VarSetOps::DiffD(this, liveIn, block->bbVarDef);
            VarSetOps::UnionD(this, liveIn, block->bbVarUse);

            heapLiveIn = (heapLiveOut && !block->bbHeapDef) || block->bbHeapUse;

            /* Can exceptions from this block be handled (in this function)? */

            if (ehBlockHasExnFlowDsc(block))
            {
                VARSET_TP VARSET_INIT_NOCOPY(liveVars, fgGetHandlerLiveVars(block));

                VarSetOps::UnionD(this, liveIn, liveVars);
                VarSetOps::UnionD(this, liveOut, liveVars);
            }

            /* Has there been any change in either live set? */

            if (!VarSetOps::Equal(this, block->bbLiveIn, liveIn) || !VarSetOps::Equal(this, block->bbLiveOut, liveOut))
            {
                if (updateInternalOnly)
                {
                    // Only "extend" liveness over BBF_INTERNAL blocks

                    noway_assert(block->bbFlags & BBF_INTERNAL);

                    if (!VarSetOps::Equal(this, VarSetOps::Intersection(this, block->bbLiveIn, liveIn), liveIn) ||
                        !VarSetOps::Equal(this, VarSetOps::Intersection(this, block->bbLiveOut, liveOut), liveOut))
                    {
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("Scope info: block BB%02u LiveIn+ ", block->bbNum);
                            dumpConvertedVarSet(this, VarSetOps::Diff(this, liveIn, block->bbLiveIn));
                            printf(", LiveOut+ ");
                            dumpConvertedVarSet(this, VarSetOps::Diff(this, liveOut, block->bbLiveOut));
                            printf("\n");
                        }
#endif // DEBUG

                        VarSetOps::UnionD(this, block->bbLiveIn, liveIn);
                        VarSetOps::UnionD(this, block->bbLiveOut, liveOut);
                        change = true;
                    }
                }
                else
                {
                    VarSetOps::Assign(this, block->bbLiveIn, liveIn);
                    VarSetOps::Assign(this, block->bbLiveOut, liveOut);
                    change = true;
                }
            }

            if ((block->bbHeapLiveIn == 1) != heapLiveIn || (block->bbHeapLiveOut == 1) != heapLiveOut)
            {
                block->bbHeapLiveIn  = heapLiveIn;
                block->bbHeapLiveOut = heapLiveOut;
                change               = true;
            }
        }
        // if there is no way we could have processed a block without seeing all of its predecessors
        // then there is no need to iterate
        if (!hasPossibleBackEdge)
        {
            break;
        }
    } while (change);

//-------------------------------------------------------------------------

#ifdef DEBUG

    if (verbose && !updateInternalOnly)
    {
        printf("\nBB liveness after fgLiveVarAnalysis():\n\n");
        fgDispBBLiveness();
    }

#endif // DEBUG
}

/*****************************************************************************
 *
 *  Mark any variables in varSet1 as interfering with any variables
 *  specified in varSet2.
 *  We ensure that the interference graph is reflective:
 *  (if T11 interferes with T16, then T16 interferes with T11)
 *  returns true if an interference was added
 *  This function returns true if any new interferences were added
 *  and returns false if no new interference were added
 */
bool Compiler::fgMarkIntf(VARSET_VALARG_TP varSet1, VARSET_VALARG_TP varSet2)
{
#ifdef LEGACY_BACKEND
    /* If either set has no bits set (or we are not optimizing), take an early out */
    if (VarSetOps::IsEmpty(this, varSet2) || VarSetOps::IsEmpty(this, varSet1) || opts.MinOpts())
    {
        return false;
    }

    bool addedIntf = false; // This is set to true if we add any new interferences

    VarSetOps::Assign(this, fgMarkIntfUnionVS, varSet1);
    VarSetOps::UnionD(this, fgMarkIntfUnionVS, varSet2);

    VARSET_ITER_INIT(this, iter, fgMarkIntfUnionVS, refIndex);
    while (iter.NextElem(this, &refIndex))
    {
        // if varSet1 has this bit set then it interferes with varSet2
        if (VarSetOps::IsMember(this, varSet1, refIndex))
        {
            // Calculate the set of new interference to add
            VARSET_TP VARSET_INIT_NOCOPY(newIntf, VarSetOps::Diff(this, varSet2, lvaVarIntf[refIndex]));
            if (!VarSetOps::IsEmpty(this, newIntf))
            {
                addedIntf = true;
                VarSetOps::UnionD(this, lvaVarIntf[refIndex], newIntf);
            }
        }

        // if varSet2 has this bit set then it interferes with varSet1
        if (VarSetOps::IsMember(this, varSet2, refIndex))
        {
            // Calculate the set of new interference to add
            VARSET_TP VARSET_INIT_NOCOPY(newIntf, VarSetOps::Diff(this, varSet1, lvaVarIntf[refIndex]));
            if (!VarSetOps::IsEmpty(this, newIntf))
            {
                addedIntf = true;
                VarSetOps::UnionD(this, lvaVarIntf[refIndex], newIntf);
            }
        }
    }

    return addedIntf;
#else
    return false;
#endif
}

/*****************************************************************************
 *
 *  Mark any variables in varSet as interfering with each other,
 *  This is a specialized version of the above, when both args are the same
 *  We ensure that the interference graph is reflective:
 *  (if T11 interferes with T16, then T16 interferes with T11)
 *  This function returns true if any new interferences were added
 *  and returns false if no new interference were added
 */

bool Compiler::fgMarkIntf(VARSET_VALARG_TP varSet)
{
#ifdef LEGACY_BACKEND
    /* No bits set or we are not optimizing, take an early out */
    if (VarSetOps::IsEmpty(this, varSet) || opts.MinOpts())
        return false;

    bool addedIntf = false; // This is set to true if we add any new interferences

    VARSET_ITER_INIT(this, iter, varSet, refIndex);
    while (iter.NextElem(this, &refIndex))
    {
        // Calculate the set of new interference to add
        VARSET_TP VARSET_INIT_NOCOPY(newIntf, VarSetOps::Diff(this, varSet, lvaVarIntf[refIndex]));
        if (!VarSetOps::IsEmpty(this, newIntf))
        {
            addedIntf = true;
            VarSetOps::UnionD(this, lvaVarIntf[refIndex], newIntf);
        }
    }

    return addedIntf;
#else  // !LEGACY_BACKEND
    return false;
#endif // !LEGACY_BACKEND
}

/*****************************************************************************
 * For updating liveset during traversal AFTER fgComputeLife has completed
 */

VARSET_VALRET_TP Compiler::fgUpdateLiveSet(VARSET_VALARG_TP liveSet, GenTreePtr tree)
{
    VARSET_TP VARSET_INIT(this, newLiveSet, liveSet);
    assert(fgLocalVarLivenessDone == true);
    GenTreePtr lclVarTree = tree; // After the tests below, "lclVarTree" will be the local variable.
    if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_FLD || tree->gtOper == GT_REG_VAR ||
        (lclVarTree = fgIsIndirOfAddrOfLocal(tree)) != nullptr)
    {
        VARSET_TP VARSET_INIT_NOCOPY(varBits, fgGetVarBits(lclVarTree));

        if (!VarSetOps::IsEmpty(this, varBits))
        {
            if (tree->gtFlags & GTF_VAR_DEATH)
            {
                // We'd like to be able to assert the following, however if we are walking
                // through a qmark/colon tree, we may encounter multiple last-use nodes.
                // assert (VarSetOps::IsSubset(this, varBits, newLiveSet));

                // We maintain the invariant that if the lclVarTree is a promoted struct, but the
                // the lookup fails, then all the field vars (i.e., "varBits") are dying.
                VARSET_TP* deadVarBits = nullptr;
                if (varTypeIsStruct(lclVarTree) && GetPromotedStructDeathVars()->Lookup(lclVarTree, &deadVarBits))
                {
                    VarSetOps::DiffD(this, newLiveSet, *deadVarBits);
                }
                else
                {
                    VarSetOps::DiffD(this, newLiveSet, varBits);
                }
            }
            else if ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & GTF_VAR_USEASG) == 0)
            {
                assert(tree == lclVarTree); // LDOBJ case should only be a use.

                // This shouldn't be in newLiveSet, unless this is debug code, in which
                // case we keep vars live everywhere, OR it is address-exposed, OR this block
                // is part of a try block, in which case it may be live at the handler
                // Could add a check that, if it's in the newLiveSet, that it's also in
                // fgGetHandlerLiveVars(compCurBB), but seems excessive
                //
                assert(VarSetOps::IsEmptyIntersection(this, newLiveSet, varBits) || opts.compDbgCode ||
                       lvaTable[tree->gtLclVarCommon.gtLclNum].lvAddrExposed ||
                       (compCurBB != nullptr && ehBlockHasExnFlowDsc(compCurBB)));
                VarSetOps::UnionD(this, newLiveSet, varBits);
            }
        }
    }
    return newLiveSet;
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeCall: compute the changes to local var liveness
//                              due to a GT_CALL node.
//
// Arguments:
//    life - The live set that is being computed.
//    call - The call node in question.
//
void Compiler::fgComputeLifeCall(VARSET_TP& life, GenTreeCall* call)
{
    assert(call != nullptr);

    // If this is a tail-call and we have any unmanaged p/invoke calls in
    // the method then we're going to run the p/invoke epilog
    // So we mark the FrameRoot as used by this instruction.
    // This ensure that this variable is kept alive at the tail-call
    if (call->IsTailCall() && info.compCallUnmanaged)
    {
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            /* Get the TCB local and make it live */

            noway_assert(info.compLvFrameListRoot < lvaCount);

            LclVarDsc* frameVarDsc = &lvaTable[info.compLvFrameListRoot];

            if (frameVarDsc->lvTracked)
            {
                VARSET_TP VARSET_INIT_NOCOPY(varBit, VarSetOps::MakeSingleton(this, frameVarDsc->lvVarIndex));

                VarSetOps::AddElemD(this, life, frameVarDsc->lvVarIndex);

                /* Record interference with other live variables */

                fgMarkIntf(life, varBit);
            }
        }
    }

    /* GC refs cannot be enregistered accross an unmanaged call */

    // TODO: we should generate the code for saving to/restoring
    //       from the inlined N/Direct frame instead.

    /* Is this call to unmanaged code? */
    if (call->IsUnmanaged())
    {
        /* Get the TCB local and make it live */
        assert((!opts.ShouldUsePInvokeHelpers()) || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            noway_assert(info.compLvFrameListRoot < lvaCount);

            LclVarDsc* frameVarDsc = &lvaTable[info.compLvFrameListRoot];

            if (frameVarDsc->lvTracked)
            {
                unsigned varIndex = frameVarDsc->lvVarIndex;
                noway_assert(varIndex < lvaTrackedCount);

                // Is the variable already known to be alive?
                //
                if (VarSetOps::IsMember(this, life, varIndex))
                {
                    // Since we may call this multiple times, clear the GTF_CALL_M_FRAME_VAR_DEATH if set.
                    //
                    call->gtCallMoreFlags &= ~GTF_CALL_M_FRAME_VAR_DEATH;
                }
                else
                {
                    // The variable is just coming to life
                    // Since this is a backwards walk of the trees
                    // that makes this change in liveness a 'last-use'
                    //
                    VarSetOps::AddElemD(this, life, varIndex);
                    call->gtCallMoreFlags |= GTF_CALL_M_FRAME_VAR_DEATH;
                }

                // Record an interference with the other live variables
                //
                VARSET_TP VARSET_INIT_NOCOPY(varBit, VarSetOps::MakeSingleton(this, varIndex));
                fgMarkIntf(life, varBit);
            }
        }

        /* Do we have any live variables? */

        if (!VarSetOps::IsEmpty(this, life))
        {
            // For each live variable if it is a GC-ref type, we
            // mark it volatile to prevent if from being enregistered
            // across the unmanaged call.

            unsigned   lclNum;
            LclVarDsc* varDsc;
            for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
            {
                /* Ignore the variable if it's not tracked */

                if (!varDsc->lvTracked)
                {
                    continue;
                }

                unsigned varNum = varDsc->lvVarIndex;

                /* Ignore the variable if it's not live here */

                if (!VarSetOps::IsMember(this, life, varDsc->lvVarIndex))
                {
                    continue;
                }

                // If it is a GC-ref type then mark it DoNotEnregister.
                if (varTypeIsGC(varDsc->TypeGet()))
                {
                    lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LiveAcrossUnmanagedCall));
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeLocal: compute the changes to local var liveness
//                               due to a use or a def of a local var and
//                               indicates wither the use/def is a dead
//                               store.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - The currents set of variables to keep alive
//                    regardless of their actual lifetime.
//    lclVarNode    - The node that corresponds to the local var def or
//                    use. Only differs from `node` when targeting the
//                    legacy backend.
//    node          - The actual tree node being processed.
//
// Returns:
//    `true` if the local var node corresponds to a dead store; `false`
//    otherwise.
//
bool Compiler::fgComputeLifeLocal(VARSET_TP& life, VARSET_TP& keepAliveVars, GenTree* lclVarNode, GenTree* node)
{
    unsigned lclNum = lclVarNode->gtLclVarCommon.gtLclNum;

    noway_assert(lclNum < lvaCount);
    LclVarDsc* varDsc = &lvaTable[lclNum];

    unsigned  varIndex;
    VARSET_TP varBit;

    // Is this a tracked variable?
    if (varDsc->lvTracked)
    {
        varIndex = varDsc->lvVarIndex;
        noway_assert(varIndex < lvaTrackedCount);

        /* Is this a definition or use? */

        if (lclVarNode->gtFlags & GTF_VAR_DEF)
        {
            /*
                The variable is being defined here. The variable
                should be marked dead from here until its closest
                previous use.

                IMPORTANT OBSERVATION:

                    For GTF_VAR_USEASG (i.e. x <op>= a) we cannot
                    consider it a "pure" definition because it would
                    kill x (which would be wrong because x is
                    "used" in such a construct) -> see below the case when x is live
             */

            if (VarSetOps::IsMember(this, life, varIndex))
            {
                /* The variable is live */

                if ((lclVarNode->gtFlags & GTF_VAR_USEASG) == 0)
                {
                    /* Mark variable as dead from here to its closest use */

                    if (!VarSetOps::IsMember(this, keepAliveVars, varIndex))
                    {
                        VarSetOps::RemoveElemD(this, life, varIndex);
                    }
#ifdef DEBUG
                    if (verbose && 0)
                    {
                        printf("Def V%02u,T%02u at ", lclNum, varIndex);
                        printTreeID(lclVarNode);
                        printf(" life %s -> %s\n",
                               VarSetOps::ToString(this, VarSetOps::Union(this, life,
                                                                          VarSetOps::MakeSingleton(this, varIndex))),
                               VarSetOps::ToString(this, life));
                    }
#endif // DEBUG
                }
            }
            else
            {
                /* Dead assignment to the variable */
                lclVarNode->gtFlags |= GTF_VAR_DEATH;

                if (!opts.MinOpts())
                {
                    // keepAliveVars always stay alive
                    noway_assert(!VarSetOps::IsMember(this, keepAliveVars, varIndex));

                    /* This is a dead store unless the variable is marked
                       GTF_VAR_USEASG and we are in an interior statement
                       that will be used (e.g. while (i++) or a GT_COMMA) */

                    // Do not consider this store dead if the target local variable represents
                    // a promoted struct field of an address exposed local or if the address
                    // of the variable has been exposed. Improved alias analysis could allow
                    // stores to these sorts of variables to be removed at the cost of compile
                    // time.
                    return !varDsc->lvAddrExposed &&
                           !(varDsc->lvIsStructField && lvaTable[varDsc->lvParentLcl].lvAddrExposed);
                }
            }

            return false;
        }
        else // it is a use
        {
            // Is the variable already known to be alive?
            if (VarSetOps::IsMember(this, life, varIndex))
            {
                // Since we may do liveness analysis multiple times, clear the GTF_VAR_DEATH if set.
                lclVarNode->gtFlags &= ~GTF_VAR_DEATH;
                return false;
            }

#ifdef DEBUG
            if (verbose && 0)
            {
                printf("Ref V%02u,T%02u] at ", lclNum, varIndex);
                printTreeID(node);
                printf(" life %s -> %s\n", VarSetOps::ToString(this, life),
                       VarSetOps::ToString(this, VarSetOps::Union(this, life, varBit)));
            }
#endif // DEBUG

            // The variable is being used, and it is not currently live.
            // So the variable is just coming to life
            lclVarNode->gtFlags |= GTF_VAR_DEATH;
            VarSetOps::AddElemD(this, life, varIndex);

            // Record interference with other live variables
            fgMarkIntf(life, VarSetOps::MakeSingleton(this, varIndex));
        }
    }
    // Note that promoted implies not tracked (i.e. only the fields are tracked).
    else if (varTypeIsStruct(varDsc->lvType))
    {
        noway_assert(!varDsc->lvTracked);

        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType != PROMOTION_TYPE_NONE)
        {
            VarSetOps::AssignNoCopy(this, varBit, VarSetOps::MakeEmpty(this));

            for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
            {
#if !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
                if (!varTypeIsLong(lvaTable[i].lvType) || !lvaTable[i].lvPromoted)
#endif // !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
                {
                    noway_assert(lvaTable[i].lvIsStructField);
                }
                if (lvaTable[i].lvTracked)
                {
                    varIndex = lvaTable[i].lvVarIndex;
                    noway_assert(varIndex < lvaTrackedCount);
                    VarSetOps::AddElemD(this, varBit, varIndex);
                }
            }
            if (node->gtFlags & GTF_VAR_DEF)
            {
                VarSetOps::DiffD(this, varBit, keepAliveVars);
                VarSetOps::DiffD(this, life, varBit);
                return false;
            }
            // This is a use.

            // Are the variables already known to be alive?
            if (VarSetOps::IsSubset(this, varBit, life))
            {
                node->gtFlags &= ~GTF_VAR_DEATH; // Since we may now call this multiple times, reset if live.
                return false;
            }

            // Some variables are being used, and they are not currently live.
            // So they are just coming to life, in the backwards traversal; in a forwards
            // traversal, one or more are dying.  Mark this.

            node->gtFlags |= GTF_VAR_DEATH;

            // Are all the variables becoming alive (in the backwards traversal), or just a subset?
            if (!VarSetOps::IsEmptyIntersection(this, varBit, life))
            {
                // Only a subset of the variables are become live; we must record that subset.
                // (Lack of an entry for "lclVarNode" will be considered to imply all become dead in the
                // forward traversal.)
                VARSET_TP* deadVarSet = new (this, CMK_bitset) VARSET_TP;
                VarSetOps::AssignNoCopy(this, *deadVarSet, VarSetOps::Diff(this, varBit, life));
                GetPromotedStructDeathVars()->Set(lclVarNode, deadVarSet);
            }

            // In any case, all the field vars are now live (in the backwards traversal).
            VarSetOps::UnionD(this, life, varBit);

            // Record interference with other live variables
            fgMarkIntf(life, varBit);
        }
    }

    return false;
}

/*****************************************************************************
 *
 * Compute the set of live variables at each node in a given statement
 * or subtree of a statement moving backward from startNode to endNode
 */

#ifndef LEGACY_BACKEND
VARSET_VALRET_TP Compiler::fgComputeLife(VARSET_VALARG_TP lifeArg,
                                         GenTreePtr       startNode,
                                         GenTreePtr       endNode,
                                         VARSET_VALARG_TP volatileVars,
                                         bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    GenTreePtr tree;
    unsigned   lclNum;

    VARSET_TP VARSET_INIT(this, life, lifeArg); // lifeArg is const ref; copy to allow modification.

    VARSET_TP VARSET_INIT(this, keepAliveVars, volatileVars);
#ifdef DEBUGGING_SUPPORT
    VarSetOps::UnionD(this, keepAliveVars, compCurBB->bbScope); // Don't kill vars in scope
#endif

    noway_assert(VarSetOps::Equal(this, VarSetOps::Intersection(this, keepAliveVars, life), keepAliveVars));
    noway_assert(compCurStmt->gtOper == GT_STMT);
    noway_assert(endNode || (startNode == compCurStmt->gtStmt.gtStmtExpr));

    // NOTE: Live variable analysis will not work if you try
    // to use the result of an assignment node directly!
    for (tree = startNode; tree != endNode; tree = tree->gtPrev)
    {
    AGAIN:
        assert(tree->OperGet() != GT_QMARK);

        if (tree->gtOper == GT_CALL)
        {
            fgComputeLifeCall(life, tree->AsCall());
        }
        else if (tree->OperIsNonPhiLocal() || tree->OperIsLocalAddr())
        {
            bool isDeadStore = fgComputeLifeLocal(life, keepAliveVars, tree, tree);
            if (isDeadStore)
            {
                LclVarDsc* varDsc = &lvaTable[tree->gtLclVarCommon.gtLclNum];

                bool doAgain = false;
                if (fgRemoveDeadStore(&tree, varDsc, life, &doAgain, pStmtInfoDirty DEBUGARG(treeModf)))
                {
                    assert(!doAgain);
                    break;
                }

                if (doAgain)
                {
                    goto AGAIN;
                }
            }
        }
    }

    // Return the set of live variables out of this statement
    return life;
}

VARSET_VALRET_TP Compiler::fgComputeLifeLIR(VARSET_VALARG_TP lifeArg, BasicBlock* block, VARSET_VALARG_TP volatileVars)
{
    VARSET_TP VARSET_INIT(this, life, lifeArg); // lifeArg is const ref; copy to allow modification.

    VARSET_TP VARSET_INIT(this, keepAliveVars, volatileVars);
#ifdef DEBUGGING_SUPPORT
    VarSetOps::UnionD(this, keepAliveVars, block->bbScope); // Don't kill vars in scope
#endif

    noway_assert(VarSetOps::Equal(this, VarSetOps::Intersection(this, keepAliveVars, life), keepAliveVars));

    LIR::Range& blockRange      = LIR::AsRange(block);
    GenTree*    firstNonPhiNode = blockRange.FirstNonPhiNode();
    if (firstNonPhiNode == nullptr)
    {
        return life;
    }

    for (GenTree *node = blockRange.LastNode(), *next = nullptr, *end = firstNonPhiNode->gtPrev; node != end;
         node = next)
    {
        next = node->gtPrev;

        if (node->OperGet() == GT_CALL)
        {
            fgComputeLifeCall(life, node->AsCall());
        }
        else if (node->OperIsNonPhiLocal() || node->OperIsLocalAddr())
        {
            bool isDeadStore = fgComputeLifeLocal(life, keepAliveVars, node, node);
            if (isDeadStore)
            {
                fgTryRemoveDeadLIRStore(blockRange, node, &next);
            }
        }
    }

    return life;
}

#else // LEGACY_BACKEND

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

VARSET_VALRET_TP Compiler::fgComputeLife(VARSET_VALARG_TP lifeArg,
                                         GenTreePtr       startNode,
                                         GenTreePtr       endNode,
                                         VARSET_VALARG_TP volatileVars,
                                         bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    GenTreePtr tree;
    unsigned   lclNum;

    GenTreePtr gtQMark       = NULL; // current GT_QMARK node (walking the trees backwards)
    GenTreePtr nextColonExit = 0;    // gtQMark->gtOp.gtOp2 while walking the 'else' branch.
                                     // gtQMark->gtOp.gtOp1 while walking the 'then' branch

    VARSET_TP VARSET_INIT(this, life, lifeArg); // lifeArg is const ref; copy to allow modification.

    // TBD: This used to be an initialization to VARSET_NOT_ACCEPTABLE.  Try to figure out what's going on here.
    VARSET_TP  VARSET_INIT_NOCOPY(entryLiveSet, VarSetOps::MakeFull(this));   // liveness when we see gtQMark
    VARSET_TP  VARSET_INIT_NOCOPY(gtColonLiveSet, VarSetOps::MakeFull(this)); // liveness when we see gtColon
    GenTreePtr gtColon = NULL;

    VARSET_TP VARSET_INIT(this, keepAliveVars, volatileVars);
#ifdef DEBUGGING_SUPPORT
    VarSetOps::UnionD(this, keepAliveVars, compCurBB->bbScope); /* Dont kill vars in scope */
#endif
    noway_assert(VarSetOps::Equal(this, VarSetOps::Intersection(this, keepAliveVars, life), keepAliveVars));
    noway_assert(compCurStmt->gtOper == GT_STMT);
    noway_assert(endNode || (startNode == compCurStmt->gtStmt.gtStmtExpr));

    /* NOTE: Live variable analysis will not work if you try
     * to use the result of an assignment node directly */

    for (tree = startNode; tree != endNode; tree = tree->gtPrev)
    {
    AGAIN:
        /* For ?: nodes if we're done with the then branch, remember
         * the liveness */
        if (gtQMark && (tree == gtColon))
        {
            VarSetOps::Assign(this, gtColonLiveSet, life);
            VarSetOps::Assign(this, gtQMark->gtQmark.gtThenLiveSet, gtColonLiveSet);
        }

        /* For ?: nodes if we're done with the else branch
         * then set the correct life as the union of the two branches */

        if (gtQMark && (tree == gtQMark->gtOp.gtOp1))
        {
            noway_assert(tree->gtFlags & GTF_RELOP_QMARK);
            noway_assert(gtQMark->gtOp.gtOp2->gtOper == GT_COLON);

            GenTreePtr thenNode = gtColon->AsColon()->ThenNode();
            GenTreePtr elseNode = gtColon->AsColon()->ElseNode();

            noway_assert(thenNode && elseNode);

            VarSetOps::Assign(this, gtQMark->gtQmark.gtElseLiveSet, life);

            /* Check if we optimized away the ?: */

            if (elseNode->IsNothingNode())
            {
                if (thenNode->IsNothingNode())
                {
                    /* This can only happen for VOID ?: */
                    noway_assert(gtColon->gtType == TYP_VOID);

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("BB%02u - Removing dead QMark - Colon ...\n", compCurBB->bbNum);
                        gtDispTree(gtQMark);
                        printf("\n");
                    }
#endif // DEBUG

                    /* Remove the '?:' - keep the side effects in the condition */

                    noway_assert(tree->OperKind() & GTK_RELOP);

                    /* Change the node to a NOP */

                    gtQMark->gtBashToNOP();
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG

                    /* Extract and keep the side effects */

                    if (tree->gtFlags & GTF_SIDE_EFFECT)
                    {
                        GenTreePtr sideEffList = NULL;

                        gtExtractSideEffList(tree, &sideEffList);

                        if (sideEffList)
                        {
                            noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("Extracted side effects list from condition...\n");
                                gtDispTree(sideEffList);
                                printf("\n");
                            }
#endif // DEBUG
                            fgUpdateRefCntForExtract(tree, sideEffList);

                            /* The NOP node becomes a GT_COMMA holding the side effect list */

                            gtQMark->ChangeOper(GT_COMMA);
                            gtQMark->gtFlags |= sideEffList->gtFlags & GTF_ALL_EFFECT;

                            if (sideEffList->gtOper == GT_COMMA)
                            {
                                gtQMark->gtOp.gtOp1 = sideEffList->gtOp.gtOp1;
                                gtQMark->gtOp.gtOp2 = sideEffList->gtOp.gtOp2;
                            }
                            else
                            {
                                gtQMark->gtOp.gtOp1 = sideEffList;
                                gtQMark->gtOp.gtOp2 = gtNewNothingNode();
                            }
                        }
                        else
                        {
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("\nRemoving tree ");
                                printTreeID(tree);
                                printf(" in BB%02u as useless\n", compCurBB->bbNum);
                                gtDispTree(tree);
                                printf("\n");
                            }
#endif // DEBUG
                            fgUpdateRefCntForExtract(tree, NULL);
                        }
                    }

                    /* If top node without side effects remove it */

                    if ((gtQMark == compCurStmt->gtStmt.gtStmtExpr) && gtQMark->IsNothingNode())
                    {
                        fgRemoveStmt(compCurBB, compCurStmt);
                        break;
                    }

                    /* Re-link the nodes for this statement */

                    fgSetStmtSeq(compCurStmt);

                    /* Continue analysis from this node */

                    tree = gtQMark;

                    /* As the 'then' and 'else' branches are emtpy, liveness
                       should not have changed */

                    noway_assert(VarSetOps::Equal(this, life, entryLiveSet));
                    goto SKIP_QMARK;
                }
                else
                {
                    // The 'else' branch is empty and the 'then' branch is non-empty
                    // so swap the two branches and reverse the condition.  If one is
                    // non-empty, we want it to be the 'else'

                    GenTreePtr tmp = thenNode;

                    gtColon->AsColon()->ThenNode() = thenNode = elseNode;
                    gtColon->AsColon()->ElseNode() = elseNode = tmp;
                    noway_assert(tree == gtQMark->gtOp.gtOp1);
                    gtReverseCond(tree);

                    // Remember to also swap the live sets of the two branches.
                    VARSET_TP VARSET_INIT_NOCOPY(tmpVS, gtQMark->gtQmark.gtElseLiveSet);
                    VarSetOps::AssignNoCopy(this, gtQMark->gtQmark.gtElseLiveSet, gtQMark->gtQmark.gtThenLiveSet);
                    VarSetOps::AssignNoCopy(this, gtQMark->gtQmark.gtThenLiveSet, tmpVS);

                    /* Re-link the nodes for this statement */

                    fgSetStmtSeq(compCurStmt);
                }
            }

            /* Variables in the two branches that are live at the split
             * must interfere with each other */

            fgMarkIntf(life, gtColonLiveSet);

            /* The live set at the split is the union of the two branches */

            VarSetOps::UnionD(this, life, gtColonLiveSet);

        SKIP_QMARK:

            /* We are out of the parallel branches, the rest is sequential */

            gtQMark = NULL;
        }

        if (tree->gtOper == GT_CALL)
        {
            fgComputeLifeCall(life, tree->AsCall());
            continue;
        }

        // Is this a use/def of a local variable?
        // Generally, the last use information is associated with the lclVar node.
        // However, for LEGACY_BACKEND, the information must be associated
        // with the OBJ itself for promoted structs.
        // In that case, the LDOBJ may be require an implementation that might itself allocate registers,
        // so the variable(s) should stay live until the end of the LDOBJ.
        // Note that for promoted structs lvTracked is false.

        GenTreePtr lclVarTree = nullptr;
        if (tree->gtOper == GT_OBJ)
        {
            // fgIsIndirOfAddrOfLocal returns nullptr if the tree is
            // not an indir(addr(local)), in which case we will set lclVarTree
            // back to the original tree, and not handle it as a use/def.
            lclVarTree = fgIsIndirOfAddrOfLocal(tree);
            if ((lclVarTree != nullptr) && lvaTable[lclVarTree->gtLclVarCommon.gtLclNum].lvTracked)
            {
                lclVarTree = nullptr;
            }
        }
        if (lclVarTree == nullptr)
        {
            lclVarTree = tree;
        }

        if (lclVarTree->OperIsNonPhiLocal() || lclVarTree->OperIsLocalAddr())
        {
            bool isDeadStore = fgComputeLifeLocal(life, keepAliveVars, lclVarTree, tree);
            if (isDeadStore)
            {
                LclVarDsc* varDsc = &lvaTable[lclVarTree->gtLclVarCommon.gtLclNum];

                bool doAgain = false;
                if (fgRemoveDeadStore(&tree, varDsc, life, &doAgain, pStmtInfoDirty DEBUGARG(treeModf)))
                {
                    assert(!doAgain);
                    break;
                }

                if (doAgain)
                {
                    goto AGAIN;
                }
            }
        }
        else
        {
            if (tree->gtOper == GT_QMARK && tree->gtOp.gtOp1)
            {
                /* Special cases - "? :" operators.

                   The trees are threaded as shown below with nodes 1 to 11 linked
                   by gtNext. Both GT_<cond>->gtLiveSet and GT_COLON->gtLiveSet are
                   the union of the liveness on entry to thenTree and elseTree.

                                  +--------------------+
                                  |      GT_QMARK    11|
                                  +----------+---------+
                                             |
                                             *
                                            / \
                                          /     \
                                        /         \
                   +---------------------+       +--------------------+
                   |      GT_<cond>    3 |       |     GT_COLON     7 |
                   |  w/ GTF_RELOP_QMARK |       |  w/ GTF_COLON_COND |
                   +----------+----------+       +---------+----------+
                              |                            |
                              *                            *
                             / \                          / \
                           /     \                      /     \
                         /         \                  /         \
                        2           1          thenTree 6       elseTree 10
                                   x               |                |
                                  /                *                *
      +----------------+        /                 / \              / \
      |prevExpr->gtNext+------/                 /     \          /     \
      +----------------+                      /         \      /         \
                                             5           4    9           8

                 */

                noway_assert(tree->gtOp.gtOp1->OperKind() & GTK_RELOP);
                noway_assert(tree->gtOp.gtOp1->gtFlags & GTF_RELOP_QMARK);
                noway_assert(tree->gtOp.gtOp2->gtOper == GT_COLON);

                if (gtQMark)
                {
                    /* This is a nested QMARK sequence - we need to use recursion.
                     * Compute the liveness for each node of the COLON branches
                     * The new computation starts from the GT_QMARK node and ends
                     * when the COLON branch of the enclosing QMARK ends */

                    noway_assert(nextColonExit &&
                                 (nextColonExit == gtQMark->gtOp.gtOp1 || nextColonExit == gtQMark->gtOp.gtOp2));

                    VarSetOps::AssignNoCopy(this, life, fgComputeLife(life, tree, nextColonExit, volatileVars,
                                                                      pStmtInfoDirty DEBUGARG(treeModf)));

                    /* Continue with exit node (the last node in the enclosing colon branch) */

                    tree = nextColonExit;
                    goto AGAIN;
                }
                else
                {
                    gtQMark = tree;
                    VarSetOps::Assign(this, entryLiveSet, life);
                    gtColon       = gtQMark->gtOp.gtOp2;
                    nextColonExit = gtColon;
                }
            }

            /* If found the GT_COLON, start the new branch with the original life */

            if (gtQMark && tree == gtQMark->gtOp.gtOp2)
            {
                /* The node better be a COLON. */
                noway_assert(tree->gtOper == GT_COLON);

                VarSetOps::Assign(this, life, entryLiveSet);
                nextColonExit = gtQMark->gtOp.gtOp1;
            }
        }
    }

    /* Return the set of live variables out of this statement */

    return life;
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // !LEGACY_BACKEND

bool Compiler::fgTryRemoveDeadLIRStore(LIR::Range& blockRange, GenTree* node, GenTree** next)
{
    assert(node != nullptr);
    assert(next != nullptr);

    assert(node->OperIsLocalStore() || node->OperIsLocalAddr());

    GenTree* store = nullptr;
    GenTree* value = nullptr;
    if (node->OperIsLocalStore())
    {
        store = node;
        value = store->gtGetOp1();
    }
    else if (node->OperIsLocalAddr())
    {
        LIR::Use addrUse;
        if (!blockRange.TryGetUse(node, &addrUse) || (addrUse.User()->OperGet() != GT_STOREIND))
        {
            *next = node->gtPrev;
            return false;
        }

        store = addrUse.User();
        value = store->gtGetOp2();
    }

    bool               isClosed      = false;
    unsigned           sideEffects   = 0;
    LIR::ReadOnlyRange operandsRange = blockRange.GetRangeOfOperandTrees(store, &isClosed, &sideEffects);
    if (!isClosed || ((sideEffects & GTF_SIDE_EFFECT) != 0) ||
        (((sideEffects & GTF_ORDER_SIDEEFF) != 0) && (value->OperGet() == GT_CATCH_ARG)))
    {
        // If the range of the operands contains unrelated code or if it contains any side effects,
        // do not remove it. Instead, just remove the store.

        *next = node->gtPrev;
    }
    else
    {
        // Okay, the operands to the store form a contiguous range that has no side effects. Remove the
        // range containing the operands and decrement the local var ref counts appropriately.

        // Compute the next node to process. Note that we must be careful not to set the next node to
        // process to a node that we are about to remove.
        if (node->OperIsLocalStore())
        {
            assert(node == store);
            *next = (operandsRange.LastNode()->gtNext == store) ? operandsRange.FirstNode()->gtPrev : node->gtPrev;
        }
        else
        {
            assert(operandsRange.Contains(node));
            *next = operandsRange.FirstNode()->gtPrev;
        }

        blockRange.Delete(this, compCurBB, std::move(operandsRange));
    }

    // If the store is marked as a late argument, it is referenced by a call. Instead of removing it,
    // bash it to a NOP.
    if ((store->gtFlags & GTF_LATE_ARG) != 0)
    {
        if (store->IsLocal())
        {
            lvaDecRefCnts(compCurBB, store);
        }

        store->gtBashToNOP();
    }
    else
    {
        blockRange.Delete(this, compCurBB, store);
    }

    return true;
}

// fgRemoveDeadStore - remove a store to a local which has no exposed uses.
//
//   pTree          - GenTree** to local, including store-form local or local addr (post-rationalize)
//   varDsc         - var that is being stored to
//   life           - current live tracked vars (maintained as we walk backwards)
//   doAgain        - out parameter, true if we should restart the statement
//   pStmtInfoDirty - should defer the cost computation to the point after the reverse walk is completed?
//
// Returns: true if we should skip the rest of the statement, false if we should continue

bool Compiler::fgRemoveDeadStore(
    GenTree** pTree, LclVarDsc* varDsc, VARSET_TP life, bool* doAgain, bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    assert(!compRationalIRForm);

    // Vars should have already been checked for address exposure by this point.
    assert(!varDsc->lvIsStructField || !lvaTable[varDsc->lvParentLcl].lvAddrExposed);
    assert(!varDsc->lvAddrExposed);

    GenTree*       asgNode  = nullptr;
    GenTree*       rhsNode  = nullptr;
    GenTree*       addrNode = nullptr;
    GenTree* const tree     = *pTree;

    GenTree* nextNode = tree->gtNext;

    // First, characterize the lclVarTree and see if we are taking its address.
    if (tree->OperIsLocalStore())
    {
        rhsNode = tree->gtOp.gtOp1;
        asgNode = tree;
    }
    else if (tree->OperIsLocal())
    {
        if (nextNode == nullptr)
        {
            return false;
        }
        if (nextNode->OperGet() == GT_ADDR)
        {
            addrNode = nextNode;
            nextNode = nextNode->gtNext;
        }
    }
    else
    {
        assert(tree->OperIsLocalAddr());
        addrNode = tree;
    }

    // Next, find the assignment.
    if (asgNode == nullptr)
    {
        if (addrNode == nullptr)
        {
            asgNode = nextNode;
        }
        else if (asgNode == nullptr)
        {
            // This may be followed by GT_IND/assign or GT_STOREIND.
            if (nextNode == nullptr)
            {
                return false;
            }
            switch (nextNode->OperGet())
            {
                default:
                    break;
                case GT_IND:
                    asgNode = nextNode->gtNext;
                    break;
                case GT_STOREIND:
                    asgNode = nextNode;
                    break;
                case GT_LIST:
                {
                    GenTree* sizeNode = nextNode->gtNext;
                    if ((sizeNode == nullptr) || (sizeNode->OperGet() != GT_CNS_INT))
                    {
                        return false;
                    }
                    asgNode = sizeNode->gtNext;
                    rhsNode = nextNode->gtGetOp2();
                }
                break;
            }
        }
    }

    if (asgNode == nullptr)
    {
        return false;
    }

    if (asgNode->OperIsAssignment())
    {
        rhsNode = asgNode->gtGetOp2();
    }
    else if (rhsNode == nullptr)
    {
        return false;
    }

    if (asgNode && (asgNode->gtFlags & GTF_ASG))
    {
        noway_assert(rhsNode);
        noway_assert(tree->gtFlags & GTF_VAR_DEF);

        if (asgNode->gtOper != GT_ASG && asgNode->gtOverflowEx())
        {
            // asgNode may be <op_ovf>= (with GTF_OVERFLOW). In that case, we need to keep the <op_ovf>

            // Dead <OpOvf>= assignment. We change it to the right operation (taking out the assignment),
            // update the flags, update order of statement, as we have changed the order of the operation
            // and we start computing life again from the op_ovf node (we go backwards). Note that we
            // don't need to update ref counts because we don't change them, we're only changing the
            // operation.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (verbose)
            {
                printf("\nChanging dead <asgop> ovf to <op> ovf...\n");
            }
#endif // DEBUG

            switch (asgNode->gtOper)
            {
                case GT_ASG_ADD:
                    asgNode->gtOper = GT_ADD;
                    break;
                case GT_ASG_SUB:
                    asgNode->gtOper = GT_SUB;
                    break;
                default:
                    // Only add and sub allowed, we don't have ASG_MUL and ASG_DIV for ints, and
                    // floats don't allow OVF forms.
                    noway_assert(!"Unexpected ASG_OP");
            }

            asgNode->gtFlags &= ~GTF_REVERSE_OPS;
            if (!((asgNode->gtOp.gtOp1->gtFlags | rhsNode->gtFlags) & GTF_ASG))
            {
                asgNode->gtFlags &= ~GTF_ASG;
            }
            asgNode->gtOp.gtOp1->gtFlags &= ~(GTF_VAR_DEF | GTF_VAR_USEASG);

#ifdef DEBUG
            *treeModf = true;
#endif // DEBUG

            // Make sure no previous cousin subtree rooted at a common ancestor has
            // asked to defer the recomputation of costs.
            if (!*pStmtInfoDirty)
            {
                /* Update ordering, costs, FP levels, etc. */
                gtSetStmtInfo(compCurStmt);

                /* Re-link the nodes for this statement */
                fgSetStmtSeq(compCurStmt);

                // Start from the old assign node, as we have changed the order of its operands.
                // No need to update liveness, as nothing has changed (the target of the asgNode
                // either goes dead here, in which case the whole expression is now dead, or it
                // was already live).

                // TODO-Throughput: Redo this so that the graph is modified BEFORE traversing it!
                // We can determine this case when we first see the asgNode

                *pTree = asgNode;

                *doAgain = true;
            }
            return false;
        }

        // Do not remove if this local variable represents
        // a promoted struct field of an address exposed local.
        if (varDsc->lvIsStructField && lvaTable[varDsc->lvParentLcl].lvAddrExposed)
        {
            return false;
        }

        // Do not remove if the address of the variable has been exposed.
        if (varDsc->lvAddrExposed)
        {
            return false;
        }

        /* Test for interior statement */

        if (asgNode->gtNext == nullptr)
        {
            /* This is a "NORMAL" statement with the
             * assignment node hanging from the GT_STMT node */

            noway_assert(compCurStmt->gtStmt.gtStmtExpr == asgNode);
            JITDUMP("top level assign\n");

            /* Check for side effects */

            if (rhsNode->gtFlags & GTF_SIDE_EFFECT)
            {
            EXTRACT_SIDE_EFFECTS:
                /* Extract the side effects */

                GenTreePtr sideEffList = nullptr;
#ifdef DEBUG
                if (verbose)
                {
                    printf("BB%02u - Dead assignment has side effects...\n", compCurBB->bbNum);
                    gtDispTree(asgNode);
                    printf("\n");
                }
#endif // DEBUG
                gtExtractSideEffList(rhsNode, &sideEffList);

                if (sideEffList)
                {
                    noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Extracted side effects list...\n");
                        gtDispTree(sideEffList);
                        printf("\n");
                    }
#endif // DEBUG
                    fgUpdateRefCntForExtract(asgNode, sideEffList);

                    /* Replace the assignment statement with the list of side effects */
                    noway_assert(sideEffList->gtOper != GT_STMT);

                    *pTree = compCurStmt->gtStmt.gtStmtExpr = sideEffList;
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG
                    /* Update ordering, costs, FP levels, etc. */
                    gtSetStmtInfo(compCurStmt);

                    /* Re-link the nodes for this statement */
                    fgSetStmtSeq(compCurStmt);

                    // Since the whole statement gets replaced it is safe to
                    // re-thread and update order. No need to compute costs again.
                    *pStmtInfoDirty = false;

                    /* Compute the live set for the new statement */
                    *doAgain = true;
                    return false;
                }
                else
                {
                    /* No side effects, most likely we forgot to reset some flags */
                    fgRemoveStmt(compCurBB, compCurStmt);

                    return true;
                }
            }
            else
            {
                /* If this is GT_CATCH_ARG saved to a local var don't bother */

                JITDUMP("removing stmt with no side effects\n");

                if (asgNode->gtFlags & GTF_ORDER_SIDEEFF)
                {
                    if (rhsNode->gtOper == GT_CATCH_ARG)
                    {
                        goto EXTRACT_SIDE_EFFECTS;
                    }
                }

                /* No side effects - remove the whole statement from the block->bbTreeList */

                fgRemoveStmt(compCurBB, compCurStmt);

                /* Since we removed it do not process the rest (i.e. RHS) of the statement
                 * variables in the RHS will not be marked as live, so we get the benefit of
                 * propagating dead variables up the chain */

                return true;
            }
        }
        else
        {
            /* This is an INTERIOR STATEMENT with a dead assignment - remove it */

            noway_assert(!VarSetOps::IsMember(this, life, varDsc->lvVarIndex));

            if (rhsNode->gtFlags & GTF_SIDE_EFFECT)
            {
                /* :-( we have side effects */

                GenTreePtr sideEffList = nullptr;
#ifdef DEBUG
                if (verbose)
                {
                    printf("BB%02u - INTERIOR dead assignment has side effects...\n", compCurBB->bbNum);
                    gtDispTree(asgNode);
                    printf("\n");
                }
#endif // DEBUG
                gtExtractSideEffList(rhsNode, &sideEffList);

                if (!sideEffList)
                {
                    goto NO_SIDE_EFFECTS;
                }

                noway_assert(sideEffList->gtFlags & GTF_SIDE_EFFECT);
#ifdef DEBUG
                if (verbose)
                {
                    printf("Extracted side effects list from condition...\n");
                    gtDispTree(sideEffList);
                    printf("\n");
                }
#endif // DEBUG
                if (sideEffList->gtOper == asgNode->gtOper)
                {
                    fgUpdateRefCntForExtract(asgNode, sideEffList);
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG
                    asgNode->gtOp.gtOp1 = sideEffList->gtOp.gtOp1;
                    asgNode->gtOp.gtOp2 = sideEffList->gtOp.gtOp2;
                    asgNode->gtType     = sideEffList->gtType;
                }
                else
                {
                    fgUpdateRefCntForExtract(asgNode, sideEffList);
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG
                    /* Change the node to a GT_COMMA holding the side effect list */
                    asgNode->gtBashToNOP();

                    asgNode->ChangeOper(GT_COMMA);
                    asgNode->gtFlags |= sideEffList->gtFlags & GTF_ALL_EFFECT;

                    if (sideEffList->gtOper == GT_COMMA)
                    {
                        asgNode->gtOp.gtOp1 = sideEffList->gtOp.gtOp1;
                        asgNode->gtOp.gtOp2 = sideEffList->gtOp.gtOp2;
                    }
                    else
                    {
                        asgNode->gtOp.gtOp1 = sideEffList;
                        asgNode->gtOp.gtOp2 = gtNewNothingNode();
                    }
                }
            }
            else
            {
            NO_SIDE_EFFECTS:
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nRemoving tree ");
                    printTreeID(asgNode);
                    printf(" in BB%02u as useless\n", compCurBB->bbNum);
                    gtDispTree(asgNode);
                    printf("\n");
                }
#endif // DEBUG
                /* No side effects - Remove the interior statement */
                fgUpdateRefCntForExtract(asgNode, nullptr);

                /* Change the assignment to a GT_NOP node */

                asgNode->gtBashToNOP();

#ifdef DEBUG
                *treeModf = true;
#endif // DEBUG
            }

            /* Re-link the nodes for this statement - Do not update ordering! */

            // Do not update costs by calling gtSetStmtInfo. fgSetStmtSeq modifies
            // the tree threading based on the new costs. Removing nodes could
            // cause a subtree to get evaluated first (earlier second) during the
            // liveness walk. Instead just set a flag that costs are dirty and
            // caller has to call gtSetStmtInfo.
            *pStmtInfoDirty = true;

            fgSetStmtSeq(compCurStmt);

            /* Continue analysis from this node */

            *pTree = asgNode;

            return false;
        }
    }
    return false;
}

/*****************************************************************************
 *
 *  Iterative data flow for live variable info and availability of range
 *  check index expressions.
 */
void Compiler::fgInterBlockLocalVarLiveness()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgInterBlockLocalVarLiveness()\n");
    }
#endif

    /* This global flag is set whenever we remove a statement */

    fgStmtRemoved = false;

    // keep track if a bbLiveIn changed due to dead store removal
    fgLocalVarLivenessChanged = false;

    /* Compute the IN and OUT sets for tracked variables */

    fgLiveVarAnalysis();

//-------------------------------------------------------------------------

#ifdef DEBUGGING_SUPPORT

    /* For debuggable code, we mark vars as live over their entire
     * reported scope, so that it will be visible over the entire scope
     */

    if (opts.compDbgCode && (info.compVarScopesCount > 0))
    {
        fgExtendDbgLifetimes();
    }

#endif // DEBUGGING_SUPPORT

    /*-------------------------------------------------------------------------
     * Variables involved in exception-handlers and finally blocks need
     * to be specially marked
     */
    BasicBlock* block;

    VARSET_TP VARSET_INIT_NOCOPY(exceptVars, VarSetOps::MakeEmpty(this));  // vars live on entry to a handler
    VARSET_TP VARSET_INIT_NOCOPY(finallyVars, VarSetOps::MakeEmpty(this)); // vars live on exit of a 'finally' block
    VARSET_TP VARSET_INIT_NOCOPY(filterVars, VarSetOps::MakeEmpty(this));  // vars live on exit from a 'filter'

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        if (block->bbCatchTyp != BBCT_NONE)
        {
            /* Note the set of variables live on entry to exception handler */

            VarSetOps::UnionD(this, exceptVars, block->bbLiveIn);
        }

        if (block->bbJumpKind == BBJ_EHFILTERRET)
        {
            /* Get the set of live variables on exit from a 'filter' */
            VarSetOps::UnionD(this, filterVars, block->bbLiveOut);
        }
        else if (block->bbJumpKind == BBJ_EHFINALLYRET)
        {
            /* Get the set of live variables on exit from a 'finally' block */

            VarSetOps::UnionD(this, finallyVars, block->bbLiveOut);
        }
#if FEATURE_EH_FUNCLETS
        // Funclets are called and returned from, as such we can only count on the frame
        // pointer being restored, and thus everything live in or live out must be on the
        // stack
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            VarSetOps::UnionD(this, exceptVars, block->bbLiveIn);
        }
        if ((block->bbJumpKind == BBJ_EHFINALLYRET) || (block->bbJumpKind == BBJ_EHFILTERRET) ||
            (block->bbJumpKind == BBJ_EHCATCHRET))
        {
            VarSetOps::UnionD(this, exceptVars, block->bbLiveOut);
        }
#endif // FEATURE_EH_FUNCLETS
    }

    LclVarDsc* varDsc;
    unsigned   varNum;

    for (varNum = 0, varDsc = lvaTable; varNum < lvaCount; varNum++, varDsc++)
    {
        /* Ignore the variable if it's not tracked */

        if (!varDsc->lvTracked)
        {
            continue;
        }

        if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            continue;
        }

        /* Un-init locals may need auto-initialization. Note that the
           liveness of such locals will bubble to the top (fgFirstBB)
           in fgInterBlockLocalVarLiveness() */

        if (!varDsc->lvIsParam && VarSetOps::IsMember(this, fgFirstBB->bbLiveIn, varDsc->lvVarIndex) &&
            (info.compInitMem || varTypeIsGC(varDsc->TypeGet())))
        {
            varDsc->lvMustInit = true;
        }

        // Mark all variables that are live on entry to an exception handler
        // or on exit from a filter handler or finally as DoNotEnregister */

        if (VarSetOps::IsMember(this, exceptVars, varDsc->lvVarIndex) ||
            VarSetOps::IsMember(this, filterVars, varDsc->lvVarIndex))
        {
            /* Mark the variable appropriately */
            lvaSetVarDoNotEnregister(varNum DEBUGARG(DNER_LiveInOutOfHandler));
        }

        /* Mark all pointer variables live on exit from a 'finally'
           block as either volatile for non-GC ref types or as
           'explicitly initialized' (volatile and must-init) for GC-ref types */

        if (VarSetOps::IsMember(this, finallyVars, varDsc->lvVarIndex))
        {
            lvaSetVarDoNotEnregister(varNum DEBUGARG(DNER_LiveInOutOfHandler));

            /* Don't set lvMustInit unless we have a non-arg, GC pointer */

            if (varDsc->lvIsParam)
            {
                continue;
            }

            if (!varTypeIsGC(varDsc->TypeGet()))
            {
                continue;
            }

            /* Mark it */
            varDsc->lvMustInit = true;
        }
    }

    /*-------------------------------------------------------------------------
     * Now fill in liveness info within each basic block - Backward DataFlow
     */

    // This is used in the liveness computation, as a temporary.
    VarSetOps::AssignNoCopy(this, fgMarkIntfUnionVS, VarSetOps::MakeEmpty(this));

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        /* Tell everyone what block we're working on */

        compCurBB = block;

        /* Remember those vars live on entry to exception handlers */
        /* if we are part of a try block */

        VARSET_TP VARSET_INIT_NOCOPY(volatileVars, VarSetOps::MakeEmpty(this));

        if (ehBlockHasExnFlowDsc(block))
        {
            VarSetOps::Assign(this, volatileVars, fgGetHandlerLiveVars(block));

            // volatileVars is a subset of exceptVars
            noway_assert(VarSetOps::IsSubset(this, volatileVars, exceptVars));
        }

        /* Start with the variables live on exit from the block */

        VARSET_TP VARSET_INIT(this, life, block->bbLiveOut);

        /* Mark any interference we might have at the end of the block */

        fgMarkIntf(life);

        if (!block->IsLIR())
        {
            /* Get the first statement in the block */

            GenTreePtr firstStmt = block->FirstNonPhiDef();

            if (!firstStmt)
            {
                continue;
            }

            /* Walk all the statements of the block backwards - Get the LAST stmt */

            GenTreePtr nextStmt = block->bbTreeList->gtPrev;

            do
            {
#ifdef DEBUG
                bool treeModf = false;
#endif // DEBUG
                noway_assert(nextStmt);
                noway_assert(nextStmt->gtOper == GT_STMT);

                compCurStmt = nextStmt;
                nextStmt    = nextStmt->gtPrev;

                /* Compute the liveness for each tree node in the statement */
                bool stmtInfoDirty = false;

                VarSetOps::AssignNoCopy(this, life, fgComputeLife(life, compCurStmt->gtStmt.gtStmtExpr, nullptr,
                                                                  volatileVars, &stmtInfoDirty DEBUGARG(&treeModf)));

                if (stmtInfoDirty)
                {
                    gtSetStmtInfo(compCurStmt);
                    fgSetStmtSeq(compCurStmt);
                }

#ifdef DEBUG
                if (verbose && treeModf)
                {
                    printf("\nfgComputeLife modified tree:\n");
                    gtDispTree(compCurStmt->gtStmt.gtStmtExpr);
                    printf("\n");
                }
#endif // DEBUG
            } while (compCurStmt != firstStmt);
        }
        else
        {
#ifdef LEGACY_BACKEND
            unreached();
#else  // !LEGACY_BACKEND
            VarSetOps::AssignNoCopy(this, life, fgComputeLifeLIR(life, block, volatileVars));
#endif // !LEGACY_BACKEND
        }

        /* Done with the current block - if we removed any statements, some
         * variables may have become dead at the beginning of the block
         * -> have to update bbLiveIn */

        if (!VarSetOps::Equal(this, life, block->bbLiveIn))
        {
            /* some variables have become dead all across the block
               So life should be a subset of block->bbLiveIn */

            // We changed the liveIn of the block, which may affect liveOut of others,
            // which may expose more dead stores.
            fgLocalVarLivenessChanged = true;

            noway_assert(VarSetOps::Equal(this, VarSetOps::Intersection(this, life, block->bbLiveIn), life));

            /* set the new bbLiveIn */

            VarSetOps::Assign(this, block->bbLiveIn, life);

            /* compute the new bbLiveOut for all the predecessors of this block */
        }

        noway_assert(compCurBB == block);
#ifdef DEBUG
        compCurBB = nullptr;
#endif
    }

    fgLocalVarLivenessDone = true;
}

#ifdef DEBUG

/*****************************************************************************/

void Compiler::fgDispBBLiveness(BasicBlock* block)
{
    VARSET_TP VARSET_INIT_NOCOPY(allVars, VarSetOps::Union(this, block->bbLiveIn, block->bbLiveOut));
    printf("BB%02u", block->bbNum);
    printf(" IN (%d)=", VarSetOps::Count(this, block->bbLiveIn));
    lvaDispVarSet(block->bbLiveIn, allVars);
    if (block->bbHeapLiveIn)
    {
        printf(" + HEAP");
    }
    printf("\n     OUT(%d)=", VarSetOps::Count(this, block->bbLiveOut));
    lvaDispVarSet(block->bbLiveOut, allVars);
    if (block->bbHeapLiveOut)
    {
        printf(" + HEAP");
    }
    printf("\n\n");
}

void Compiler::fgDispBBLiveness()
{
    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        fgDispBBLiveness(block);
    }
}

#endif // DEBUG
