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

#if !defined(_TARGET_64BIT_)
#include "decomposelongs.h"
#endif
#include "lower.h" // for LowerRange()

/*****************************************************************************
 *
 *  Helper for Compiler::fgPerBlockLocalVarLiveness().
 *  The goal is to compute the USE and DEF sets for a basic block.
 */
void Compiler::fgMarkUseDef(GenTreeLclVarCommon* tree)
{
    assert((tree->OperIsLocal() && (tree->OperGet() != GT_PHI_ARG)) || tree->OperIsLocalAddr());

    const unsigned lclNum = tree->gtLclNum;
    assert(lclNum < lvaCount);

    LclVarDsc* const varDsc = &lvaTable[lclNum];

    // We should never encounter a reference to a lclVar that has a zero refCnt.
    if (varDsc->lvRefCnt() == 0 && (!varTypeIsPromotable(varDsc) || !varDsc->lvPromoted))
    {
        JITDUMP("Found reference to V%02u with zero refCnt.\n", lclNum);
        assert(!"We should never encounter a reference to a lclVar that has a zero refCnt.");
        varDsc->setLvRefCnt(1);
    }

    const bool isDef = (tree->gtFlags & GTF_VAR_DEF) != 0;
    const bool isUse = !isDef || ((tree->gtFlags & GTF_VAR_USEASG) != 0);

    if (varDsc->lvTracked)
    {
        assert(varDsc->lvVarIndex < lvaTrackedCount);

        // We don't treat stores to tracked locals as modifications of ByrefExposed memory;
        // Make sure no tracked local is addr-exposed, to make sure we don't incorrectly CSE byref
        // loads aliasing it across a store to it.
        assert(!varDsc->lvAddrExposed);

        if (compRationalIRForm && (varDsc->lvType != TYP_STRUCT) && !varTypeIsMultiReg(varDsc))
        {
            // If this is an enregisterable variable that is not marked doNotEnregister,
            // we should only see direct references (not ADDRs).
            assert(varDsc->lvDoNotEnregister || tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR));
        }

        if (isUse && !VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
        {
            // This is an exposed use; add it to the set of uses.
            VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
        }

        if (isDef)
        {
            // This is a def, add it to the set of defs.
            VarSetOps::AddElemD(this, fgCurDefSet, varDsc->lvVarIndex);
        }
    }
    else
    {
        if (varDsc->lvAddrExposed)
        {
            // Reflect the effect on ByrefExposed memory

            if (isUse)
            {
                fgCurMemoryUse |= memoryKindSet(ByrefExposed);
            }
            if (isDef)
            {
                fgCurMemoryDef |= memoryKindSet(ByrefExposed);

                // We've found a store that modifies ByrefExposed
                // memory but not GcHeap memory, so track their
                // states separately.
                byrefStatesMatchGcHeapStates = false;
            }
        }

        if (varTypeIsStruct(varDsc))
        {
            lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

            if (promotionType != PROMOTION_TYPE_NONE)
            {
                VARSET_TP bitMask(VarSetOps::MakeEmpty(this));

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
                if (!isUse)
                {
                    assert(isDef);
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
}

/*****************************************************************************/
void Compiler::fgLocalVarLiveness()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgLocalVarLiveness()\n");

        if (compRationalIRForm)
        {
            lvaTableDump();
        }
    }
#endif // DEBUG

    // Init liveness data structures.
    fgLocalVarLivenessInit();

    EndPhase(PHASE_LCLVARLIVENESS_INIT);

    // Make sure we haven't noted any partial last uses of promoted structs.
    ClearPromotedStructDeathVars();

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

    EndPhase(PHASE_LCLVARLIVENESS_INTERBLOCK);
}

/*****************************************************************************/
void Compiler::fgLocalVarLivenessInit()
{
    JITDUMP("In fgLocalVarLivenessInit\n");

    // Sort locals first, if we're optimizing
    if (opts.OptimizationEnabled())
    {
        lvaSortByRefCount();
    }

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

//------------------------------------------------------------------------
// fgPerNodeLocalVarLiveness:
//   Set fgCurMemoryUse and fgCurMemoryDef when memory is read or updated
//   Call fgMarkUseDef for any Local variables encountered
//
// Arguments:
//    tree       - The current node.
//
void Compiler::fgPerNodeLocalVarLiveness(GenTree* tree)
{
    assert(tree != nullptr);

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
            fgMarkUseDef(tree->AsLclVarCommon());
            break;

        case GT_CLS_VAR:
            // For Volatile indirection, first mutate GcHeap/ByrefExposed.
            // See comments in ValueNum.cpp (under case GT_CLS_VAR)
            // This models Volatile reads as def-then-use of memory
            // and allows for a CSE of a subsequent non-volatile read.
            if ((tree->gtFlags & GTF_FLD_VOLATILE) != 0)
            {
                // For any Volatile indirection, we must handle it as a
                // definition of GcHeap/ByrefExposed
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            }
            // If the GT_CLS_VAR is the lhs of an assignment, we'll handle it as a GcHeap/ByrefExposed def, when we get
            // to the assignment.
            // Otherwise, we treat it as a use here.
            if ((tree->gtFlags & GTF_CLS_VAR_ASG_LHS) == 0)
            {
                fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            }
            break;

        case GT_IND:
            // For Volatile indirection, first mutate GcHeap/ByrefExposed
            // see comments in ValueNum.cpp (under case GT_CLS_VAR)
            // This models Volatile reads as def-then-use of memory.
            // and allows for a CSE of a subsequent non-volatile read
            if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
            {
                // For any Volatile indirection, we must handle it as a
                // definition of the GcHeap/ByrefExposed
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            }

            // If the GT_IND is the lhs of an assignment, we'll handle it
            // as a memory def, when we get to assignment.
            // Otherwise, we treat it as a use here.
            if ((tree->gtFlags & GTF_IND_ASG_LHS) == 0)
            {
                GenTreeLclVarCommon* dummyLclVarTree = nullptr;
                bool                 dummyIsEntire   = false;
                GenTree*             addrArg         = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
                if (!addrArg->DefinesLocalAddr(this, /*width doesn't matter*/ 0, &dummyLclVarTree, &dummyIsEntire))
                {
                    fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                }
                else
                {
                    // Defines a local addr
                    assert(dummyLclVarTree != nullptr);
                    fgMarkUseDef(dummyLclVarTree->AsLclVarCommon());
                }
            }
            break;

        // These should have been morphed away to become GT_INDs:
        case GT_FIELD:
        case GT_INDEX:
            unreached();
            break;

        // We'll assume these are use-then-defs of memory.
        case GT_LOCKADD:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
            fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            fgCurMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
            break;

        case GT_MEMORYBARRIER:
            // Simliar to any Volatile indirection, we must handle this as a definition of GcHeap/ByrefExposed
            fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
        {
            GenTreeHWIntrinsic* hwIntrinsicNode = tree->AsHWIntrinsic();

            // We can't call fgMutateGcHeap unless the block has recorded a MemoryDef
            //
            if (hwIntrinsicNode->OperIsMemoryStore())
            {
                // We currently handle this like a Volatile store, so it counts as a definition of GcHeap/ByrefExposed
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            }
            if (hwIntrinsicNode->OperIsMemoryLoad())
            {
                // This instruction loads from memory and we need to record this information
                fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            }
            break;
        }
#endif

        // For now, all calls read/write GcHeap/ByrefExposed, writes in their entirety.  Might tighten this case later.
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
                fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                fgCurMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
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

            // Determine what memory locations it defines.
            if (tree->OperIs(GT_ASG) || tree->OperIsBlkOp())
            {
                GenTreeLclVarCommon* dummyLclVarTree = nullptr;
                if (tree->DefinesLocal(this, &dummyLclVarTree))
                {
                    if (lvaVarAddrExposed(dummyLclVarTree->gtLclNum))
                    {
                        fgCurMemoryDef |= memoryKindSet(ByrefExposed);

                        // We've found a store that modifies ByrefExposed
                        // memory but not GcHeap memory, so track their
                        // states separately.
                        byrefStatesMatchGcHeapStates = false;
                    }
                }
                else
                {
                    // If it doesn't define a local, then it might update GcHeap/ByrefExposed.
                    fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                }
            }
            break;
    }
}

/*****************************************************************************/
void Compiler::fgPerBlockLocalVarLiveness()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgPerBlockLocalVarLiveness()\n");
    }
#endif // DEBUG

    unsigned livenessVarEpoch = GetCurLVEpoch();

    BasicBlock* block;

    // If we don't require accurate local var lifetimes, things are simple.
    if (!backendRequiresLocalVarLifetimes())
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        VARSET_TP liveAll(VarSetOps::MakeEmpty(this));

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
            block->bbMemoryUse     = fullMemoryKindSet;
            block->bbMemoryDef     = fullMemoryKindSet;
            block->bbMemoryLiveIn  = fullMemoryKindSet;
            block->bbMemoryLiveOut = fullMemoryKindSet;

            switch (block->bbJumpKind)
            {
                case BBJ_EHFINALLYRET:
                case BBJ_THROW:
                case BBJ_RETURN:
                    VarSetOps::AssignNoCopy(this, block->bbLiveOut, VarSetOps::MakeEmpty(this));
                    break;
                default:
                    VarSetOps::Assign(this, block->bbLiveOut, liveAll);
                    break;
            }
        }

        // In minopts, we don't explicitly build SSA or value-number; GcHeap and
        // ByrefExposed implicitly (conservatively) change state at each instr.
        byrefStatesMatchGcHeapStates = true;

        return;
    }

    // Avoid allocations in the long case.
    VarSetOps::AssignNoCopy(this, fgCurUseSet, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, fgCurDefSet, VarSetOps::MakeEmpty(this));

    // GC Heap and ByrefExposed can share states unless we see a def of byref-exposed
    // memory that is not a GC Heap def.
    byrefStatesMatchGcHeapStates = true;

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        VarSetOps::ClearD(this, fgCurUseSet);
        VarSetOps::ClearD(this, fgCurDefSet);

        fgCurMemoryUse   = emptyMemoryKindSet;
        fgCurMemoryDef   = emptyMemoryKindSet;
        fgCurMemoryHavoc = emptyMemoryKindSet;

        compCurBB = block;
        if (block->IsLIR())
        {
            for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
            {
                fgPerNodeLocalVarLiveness(node);
            }
        }
        else
        {
            for (GenTreeStmt* stmt = block->FirstNonPhiDef(); stmt != nullptr; stmt = stmt->getNextStmt())
            {
                compCurStmt = stmt;
                for (GenTree* node = stmt->gtStmtList; node != nullptr; node = node->gtNext)
                {
                    fgPerNodeLocalVarLiveness(node);
                }
            }
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
            VARSET_TP allVars(VarSetOps::Union(this, fgCurUseSet, fgCurDefSet));
            printf(FMT_BB, block->bbNum);
            printf(" USE(%d)=", VarSetOps::Count(this, fgCurUseSet));
            lvaDispVarSet(fgCurUseSet, allVars);
            for (MemoryKind memoryKind : allMemoryKinds())
            {
                if ((fgCurMemoryUse & memoryKindSet(memoryKind)) != 0)
                {
                    printf(" + %s", memoryKindNames[memoryKind]);
                }
            }
            printf("\n     DEF(%d)=", VarSetOps::Count(this, fgCurDefSet));
            lvaDispVarSet(fgCurDefSet, allVars);
            for (MemoryKind memoryKind : allMemoryKinds())
            {
                if ((fgCurMemoryDef & memoryKindSet(memoryKind)) != 0)
                {
                    printf(" + %s", memoryKindNames[memoryKind]);
                }
                if ((fgCurMemoryHavoc & memoryKindSet(memoryKind)) != 0)
                {
                    printf("*");
                }
            }
            printf("\n\n");
        }
#endif // DEBUG

        VarSetOps::Assign(this, block->bbVarUse, fgCurUseSet);
        VarSetOps::Assign(this, block->bbVarDef, fgCurDefSet);
        block->bbMemoryUse   = fgCurMemoryUse;
        block->bbMemoryDef   = fgCurMemoryDef;
        block->bbMemoryHavoc = fgCurMemoryHavoc;

        /* also initialize the IN set, just in case we will do multiple DFAs */

        VarSetOps::AssignNoCopy(this, block->bbLiveIn, VarSetOps::MakeEmpty(this));
        block->bbMemoryLiveIn = emptyMemoryKindSet;
    }

    noway_assert(livenessVarEpoch == GetCurLVEpoch());
#ifdef DEBUG
    if (verbose)
    {
        printf("** Memory liveness computed, GcHeap states and ByrefExposed states %s\n",
               (byrefStatesMatchGcHeapStates ? "match" : "diverge"));
    }
#endif // DEBUG
}

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
        printf("Scope info: block " FMT_BB " marking in scope: ", block->bbNum);
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
        printf("Scope info: block " FMT_BB " UNmarking in scope: ", block->bbNum);
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
        printf(FMT_BB ": ", block->bbNum);
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

    VARSET_TP inScope(VarSetOps::MakeEmpty(this));

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

    VARSET_TP inScope(VarSetOps::MakeEmpty(this));
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

    VARSET_TP trackedArgs(VarSetOps::MakeEmpty(this));

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
    VARSET_TP noUnmarkVars(trackedArgs);

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

    VARSET_TP initVars(VarSetOps::MakeEmpty(this)); // Vars which are artificially made alive

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

        VarSetOps::Iter iter(this, initVars);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
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

            // If we haven't already done this ...
            if (!fgLocalVarLivenessDone)
            {
                // Create a "zero" node
                GenTree* zero = gtNewZeroConNode(genActualType(type));

                // Create initialization node
                if (!block->IsLIR())
                {
                    GenTree* varNode  = gtNewLclvNode(varNum, type);
                    GenTree* initNode = gtNewAssignNode(varNode, zero);

                    // Create a statement for the initializer, sequence it, and append it to the current BB.
                    GenTreeStmt* initStmt = gtNewStmt(initNode);
                    gtSetStmtInfo(initStmt);
                    fgSetStmtSeq(initStmt);
                    fgInsertStmtNearEnd(block, initStmt);
                }
                else
                {
                    GenTree* store    = new (this, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, type, varNum);
                    store->gtOp.gtOp1 = zero;
                    store->gtFlags |= (GTF_VAR_DEF | GTF_ASG);

                    LIR::Range initRange = LIR::EmptyRange();
                    initRange.InsertBefore(nullptr, zero, store);

#if !defined(_TARGET_64BIT_)
                    unsigned blockWeight = block->getBBWeight(this);
                    DecomposeLongs::DecomposeRange(this, blockWeight, initRange);
#endif // !defined(_TARGET_64BIT_)
                    m_pLowering->LowerRange(block, initRange);

                    // Naively inserting the initializer at the end of the block may add code after the block's
                    // terminator, in which case the inserted code will never be executed (and the IR for the
                    // block will be invalid). Use `LIR::InsertBeforeTerminator` to avoid this problem.
                    LIR::InsertBeforeTerminator(block, std::move(initRange));
                }

#ifdef DEBUG
                if (verbose)
                {
                    printf("Created zero-init of V%02u in " FMT_BB "\n", varNum, block->bbNum);
                }
#endif                                         // DEBUG
                block->bbFlags |= BBF_CHANGED; // indicates that the contents of the block have changed.
            }

            /* Update liveness information so that redoing fgLiveVarAnalysis()
               will work correctly if needed */

            VarSetOps::AddElemD(this, block->bbVarDef, varIndex);
            VarSetOps::AddElemD(this, block->bbLiveOut, varIndex);
        }
    }

    // raMarkStkVars() reserves stack space for unused variables (which
    //   needs to be initialized). However, arguments don't need to be initialized.
    //   So just ensure that they don't have a 0 ref cnt

    unsigned lclNum = 0;
    for (LclVarDsc *varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        if (lclNum >= info.compArgsCount)
        {
            break; // early exit for loop
        }

        if (varDsc->lvIsRegArg)
        {
            varDsc->lvImplicitlyReferenced = true;
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

//------------------------------------------------------------------------
// fgGetHandlerLiveVars: determine set of locals live because of implicit
//   exception flow from a block.
//
// Arguments:
//    block - the block in question
//
// Returns:
//    Additional set of locals to be considered live throughout the block.
//
// Notes:
//    Assumes caller has screened candidate blocks to only those with
//    exception flow, via `ehBlockHasExnFlowDsc`.
//
//    Exception flow can arise because of a newly raised exception (for
//    blocks within try regions) or because of an actively propagating exception
//    (for filter blocks). This flow effectively creates additional successor
//    edges in the flow graph that the jit does not model. This method computes
//    the net contribution from all the missing successor edges.
//
//    For example, with the following C# source, during EH processing of the throw,
//    the outer filter will execute in pass1, before the inner handler executes
//    in pass2, and so the filter blocks should show the inner handler's local is live.
//
//    try
//    {
//        using (AllocateObject())   // ==> try-finally; handler calls Dispose
//        {
//            throw new Exception();
//        }
//    }
//    catch (Exception e1) when (IsExpectedException(e1))
//    {
//        Console.WriteLine("In catch 1");
//    }

VARSET_VALRET_TP Compiler::fgGetHandlerLiveVars(BasicBlock* block)
{
    noway_assert(block);
    noway_assert(ehBlockHasExnFlowDsc(block));

    VARSET_TP liveVars(VarSetOps::MakeEmpty(this));
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

    // If this block is within a filter, we also need to report as live
    // any vars live into enclosed finally or fault handlers, since the
    // filter will run during the first EH pass, and enclosed or enclosing
    // handlers will run during the second EH pass. So all these handlers
    // are "exception flow" successors of the filter.
    //
    // Note we are relying on ehBlockHasExnFlowDsc to return true
    // for any filter block that we should examine here.
    if (block->hasHndIndex())
    {
        const unsigned thisHndIndex   = block->getHndIndex();
        EHblkDsc*      enclosingHBtab = ehGetDsc(thisHndIndex);

        if (enclosingHBtab->InFilterRegionBBRange(block))
        {
            assert(enclosingHBtab->HasFilter());

            // Search the EH table for enclosed regions.
            //
            // All the enclosed regions will be lower numbered and
            // immediately prior to and contiguous with the enclosing
            // region in the EH tab.
            unsigned index = thisHndIndex;

            while (index > 0)
            {
                index--;
                unsigned enclosingIndex = ehGetEnclosingTryIndex(index);
                bool     isEnclosed     = false;

                // To verify this is an enclosed region, search up
                // through the enclosing regions until we find the
                // region associated with the filter.
                while (enclosingIndex != EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    if (enclosingIndex == thisHndIndex)
                    {
                        isEnclosed = true;
                        break;
                    }

                    enclosingIndex = ehGetEnclosingTryIndex(enclosingIndex);
                }

                // If we found an enclosed region, check if the region
                // is a try fault or try finally, and if so, add any
                // locals live into the enclosed region's handler into this
                // block's live-in set.
                if (isEnclosed)
                {
                    EHblkDsc* enclosedHBtab = ehGetDsc(index);

                    if (enclosedHBtab->HasFinallyOrFaultHandler())
                    {
                        VarSetOps::UnionD(this, liveVars, enclosedHBtab->ebdHndBeg->bbLiveIn);
                    }
                }
                // Once we run across a non-enclosed region, we can stop searching.
                else
                {
                    break;
                }
            }
        }
    }

    return liveVars;
}

class LiveVarAnalysis
{
    Compiler* m_compiler;

    bool m_hasPossibleBackEdge;

    unsigned  m_memoryLiveIn;
    unsigned  m_memoryLiveOut;
    VARSET_TP m_liveIn;
    VARSET_TP m_liveOut;

    LiveVarAnalysis(Compiler* compiler)
        : m_compiler(compiler)
        , m_hasPossibleBackEdge(false)
        , m_memoryLiveIn(emptyMemoryKindSet)
        , m_memoryLiveOut(emptyMemoryKindSet)
        , m_liveIn(VarSetOps::MakeEmpty(compiler))
        , m_liveOut(VarSetOps::MakeEmpty(compiler))
    {
    }

    bool PerBlockAnalysis(BasicBlock* block, bool updateInternalOnly, bool keepAliveThis)
    {
        /* Compute the 'liveOut' set */
        VarSetOps::ClearD(m_compiler, m_liveOut);
        m_memoryLiveOut = emptyMemoryKindSet;
        if (block->endsWithJmpMethod(m_compiler))
        {
            // A JMP uses all the arguments, so mark them all
            // as live at the JMP instruction
            //
            const LclVarDsc* varDscEndParams = m_compiler->lvaTable + m_compiler->info.compArgsCount;
            for (LclVarDsc* varDsc = m_compiler->lvaTable; varDsc < varDscEndParams; varDsc++)
            {
                noway_assert(!varDsc->lvPromoted);
                if (varDsc->lvTracked)
                {
                    VarSetOps::AddElemD(m_compiler, m_liveOut, varDsc->lvVarIndex);
                }
            }
        }

        // Additionally, union in all the live-in tracked vars of successors.
        for (BasicBlock* succ : block->GetAllSuccs(m_compiler))
        {
            VarSetOps::UnionD(m_compiler, m_liveOut, succ->bbLiveIn);
            m_memoryLiveOut |= succ->bbMemoryLiveIn;
            if (succ->bbNum <= block->bbNum)
            {
                m_hasPossibleBackEdge = true;
            }
        }

        /* For lvaKeepAliveAndReportThis methods, "this" has to be kept alive everywhere
           Note that a function may end in a throw on an infinite loop (as opposed to a return).
           "this" has to be alive everywhere even in such methods. */

        if (keepAliveThis)
        {
            VarSetOps::AddElemD(m_compiler, m_liveOut, m_compiler->lvaTable[m_compiler->info.compThisArg].lvVarIndex);
        }

        /* Compute the 'm_liveIn'  set */
        VarSetOps::LivenessD(m_compiler, m_liveIn, block->bbVarDef, block->bbVarUse, m_liveOut);

        // Even if block->bbMemoryDef is set, we must assume that it doesn't kill memory liveness from m_memoryLiveOut,
        // since (without proof otherwise) the use and def may touch different memory at run-time.
        m_memoryLiveIn = m_memoryLiveOut | block->bbMemoryUse;

        // Does this block have implicit exception flow to a filter or handler?
        // If so, include the effects of that flow.
        if (m_compiler->ehBlockHasExnFlowDsc(block))
        {
            const VARSET_TP& liveVars(m_compiler->fgGetHandlerLiveVars(block));
            VarSetOps::UnionD(m_compiler, m_liveIn, liveVars);
            VarSetOps::UnionD(m_compiler, m_liveOut, liveVars);

            // Implicit eh edges can induce loop-like behavior,
            // so make sure we iterate to closure.
            m_hasPossibleBackEdge = true;
        }

        /* Has there been any change in either live set? */

        bool liveInChanged = !VarSetOps::Equal(m_compiler, block->bbLiveIn, m_liveIn);
        if (liveInChanged || !VarSetOps::Equal(m_compiler, block->bbLiveOut, m_liveOut))
        {
            if (updateInternalOnly)
            {
                // Only "extend" liveness over BBF_INTERNAL blocks

                noway_assert(block->bbFlags & BBF_INTERNAL);

                liveInChanged = !VarSetOps::IsSubset(m_compiler, m_liveIn, block->bbLiveIn);
                if (liveInChanged || !VarSetOps::IsSubset(m_compiler, m_liveOut, block->bbLiveOut))
                {
#ifdef DEBUG
                    if (m_compiler->verbose)
                    {
                        printf("Scope info: block " FMT_BB " LiveIn+ ", block->bbNum);
                        dumpConvertedVarSet(m_compiler, VarSetOps::Diff(m_compiler, m_liveIn, block->bbLiveIn));
                        printf(", LiveOut+ ");
                        dumpConvertedVarSet(m_compiler, VarSetOps::Diff(m_compiler, m_liveOut, block->bbLiveOut));
                        printf("\n");
                    }
#endif // DEBUG

                    VarSetOps::UnionD(m_compiler, block->bbLiveIn, m_liveIn);
                    VarSetOps::UnionD(m_compiler, block->bbLiveOut, m_liveOut);
                }
            }
            else
            {
                VarSetOps::Assign(m_compiler, block->bbLiveIn, m_liveIn);
                VarSetOps::Assign(m_compiler, block->bbLiveOut, m_liveOut);
            }
        }

        const bool memoryLiveInChanged = (block->bbMemoryLiveIn != m_memoryLiveIn);
        if (memoryLiveInChanged || (block->bbMemoryLiveOut != m_memoryLiveOut))
        {
            block->bbMemoryLiveIn  = m_memoryLiveIn;
            block->bbMemoryLiveOut = m_memoryLiveOut;
        }

        return liveInChanged || memoryLiveInChanged;
    }

    void Run(bool updateInternalOnly)
    {
        const bool keepAliveThis =
            m_compiler->lvaKeepAliveAndReportThis() && m_compiler->lvaTable[m_compiler->info.compThisArg].lvTracked;

        /* Live Variable Analysis - Backward dataflow */
        bool changed;
        do
        {
            changed = false;

            /* Visit all blocks and compute new data flow values */

            VarSetOps::ClearD(m_compiler, m_liveIn);
            VarSetOps::ClearD(m_compiler, m_liveOut);

            m_memoryLiveIn  = emptyMemoryKindSet;
            m_memoryLiveOut = emptyMemoryKindSet;

            for (BasicBlock* block = m_compiler->fgLastBB; block; block = block->bbPrev)
            {
                // sometimes block numbers are not monotonically increasing which
                // would cause us not to identify backedges
                if (block->bbNext && block->bbNext->bbNum <= block->bbNum)
                {
                    m_hasPossibleBackEdge = true;
                }

                if (updateInternalOnly)
                {
                    /* Only update BBF_INTERNAL blocks as they may be
                       syntactically out of sequence. */

                    noway_assert(m_compiler->opts.compDbgCode && (m_compiler->info.compVarScopesCount > 0));

                    if (!(block->bbFlags & BBF_INTERNAL))
                    {
                        continue;
                    }
                }

                if (PerBlockAnalysis(block, updateInternalOnly, keepAliveThis))
                {
                    changed = true;
                }
            }
            // if there is no way we could have processed a block without seeing all of its predecessors
            // then there is no need to iterate
            if (!m_hasPossibleBackEdge)
            {
                break;
            }
        } while (changed);
    }

public:
    static void Run(Compiler* compiler, bool updateInternalOnly)
    {
        LiveVarAnalysis analysis(compiler);
        analysis.Run(updateInternalOnly);
    }
};

/*****************************************************************************
 *
 *  This is the classic algorithm for Live Variable Analysis.
 *  If updateInternalOnly==true, only update BBF_INTERNAL blocks.
 */

void Compiler::fgLiveVarAnalysis(bool updateInternalOnly)
{
    if (!backendRequiresLocalVarLifetimes())
    {
        return;
    }

    LiveVarAnalysis::Run(this, updateInternalOnly);

#ifdef DEBUG
    if (verbose && !updateInternalOnly)
    {
        printf("\nBB liveness after fgLiveVarAnalysis():\n\n");
        fgDispBBLiveness();
    }
#endif // DEBUG
}

/*****************************************************************************
 * For updating liveset during traversal AFTER fgComputeLife has completed
 */

VARSET_VALRET_TP Compiler::fgUpdateLiveSet(VARSET_VALARG_TP liveSet, GenTree* tree)
{
    VARSET_TP newLiveSet(VarSetOps::MakeCopy(this, liveSet));
    assert(fgLocalVarLivenessDone == true);
    GenTree* lclVarTree = tree; // After the tests below, "lclVarTree" will be the local variable.
    if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_FLD ||
        (lclVarTree = fgIsIndirOfAddrOfLocal(tree)) != nullptr)
    {
        const VARSET_TP& varBits(fgGetVarBits(lclVarTree));

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
                if (varTypeIsStruct(lclVarTree) && LookupPromotedStructDeathVars(lclVarTree, &deadVarBits))
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
                VarSetOps::AddElemD(this, life, frameVarDsc->lvVarIndex);
            }
        }
    }

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
            }
        }
    }
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeTrackedLocalUse:
//    Compute the changes to local var liveness due to a use of a tracked local var.
//
// Arguments:
//    life          - The live set that is being computed.
//    varDsc        - The LclVar descriptor for the variable being used or defined.
//    node          - The node that is defining the lclVar.
void Compiler::fgComputeLifeTrackedLocalUse(VARSET_TP& life, LclVarDsc& varDsc, GenTreeLclVarCommon* node)
{
    assert(node != nullptr);
    assert((node->gtFlags & GTF_VAR_DEF) == 0);
    assert(varDsc.lvTracked);

    const unsigned varIndex = varDsc.lvVarIndex;

    // Is the variable already known to be alive?
    if (VarSetOps::IsMember(this, life, varIndex))
    {
        // Since we may do liveness analysis multiple times, clear the GTF_VAR_DEATH if set.
        node->gtFlags &= ~GTF_VAR_DEATH;
        return;
    }

#ifdef DEBUG
    if (verbose && 0)
    {
        printf("Ref V%02u,T%02u] at ", node->gtLclNum, varIndex);
        printTreeID(node);
        printf(" life %s -> %s\n", VarSetOps::ToString(this, life),
               VarSetOps::ToString(this, VarSetOps::AddElem(this, life, varIndex)));
    }
#endif // DEBUG

    // The variable is being used, and it is not currently live.
    // So the variable is just coming to life
    node->gtFlags |= GTF_VAR_DEATH;
    VarSetOps::AddElemD(this, life, varIndex);
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeTrackedLocalDef:
//    Compute the changes to local var liveness due to a def of a tracked local var and return `true` if the def is a
//    dead store.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - The current set of variables to keep alive regardless of their actual lifetime.
//    varDsc        - The LclVar descriptor for the variable being used or defined.
//    node          - The node that is defining the lclVar.
//
// Returns:
//    `true` if the def is a dead store; `false` otherwise.
bool Compiler::fgComputeLifeTrackedLocalDef(VARSET_TP&           life,
                                            VARSET_VALARG_TP     keepAliveVars,
                                            LclVarDsc&           varDsc,
                                            GenTreeLclVarCommon* node)
{
    assert(node != nullptr);
    assert((node->gtFlags & GTF_VAR_DEF) != 0);
    assert(varDsc.lvTracked);

    const unsigned varIndex = varDsc.lvVarIndex;
    if (VarSetOps::IsMember(this, life, varIndex))
    {
        // The variable is live
        if ((node->gtFlags & GTF_VAR_USEASG) == 0)
        {
            // Remove the variable from the live set if it is not in the keepalive set.
            if (!VarSetOps::IsMember(this, keepAliveVars, varIndex))
            {
                VarSetOps::RemoveElemD(this, life, varIndex);
            }
#ifdef DEBUG
            if (verbose && 0)
            {
                printf("Def V%02u,T%02u at ", node->gtLclNum, varIndex);
                printTreeID(node);
                printf(" life %s -> %s\n",
                       VarSetOps::ToString(this,
                                           VarSetOps::Union(this, life, VarSetOps::MakeSingleton(this, varIndex))),
                       VarSetOps::ToString(this, life));
            }
#endif // DEBUG
        }
    }
    else
    {
        // Dead store
        node->gtFlags |= GTF_VAR_DEATH;

        if (!opts.MinOpts())
        {
            // keepAliveVars always stay alive
            noway_assert(!VarSetOps::IsMember(this, keepAliveVars, varIndex));

            // Do not consider this store dead if the target local variable represents
            // a promoted struct field of an address exposed local or if the address
            // of the variable has been exposed. Improved alias analysis could allow
            // stores to these sorts of variables to be removed at the cost of compile
            // time.
            return !varDsc.lvAddrExposed && !(varDsc.lvIsStructField && lvaTable[varDsc.lvParentLcl].lvAddrExposed);
        }
    }

    return false;
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeUntrackedLocal:
//    Compute the changes to local var liveness due to a use or a def of an untracked local var.
//
// Note:
//    It may seem a bit counter-intuitive that a change to an untracked lclVar could affect the liveness of tracked
//    lclVars. In theory, this could happen with promoted (especially dependently-promoted) structs: in these cases,
//    a use or def of the untracked struct var is treated as a use or def of any of its component fields that are
//    tracked.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - The current set of variables to keep alive regardless of their actual lifetime.
//    varDsc        - The LclVar descriptor for the variable being used or defined.
//    lclVarNode    - The node that corresponds to the local var def or use.
void Compiler::fgComputeLifeUntrackedLocal(VARSET_TP&           life,
                                           VARSET_VALARG_TP     keepAliveVars,
                                           LclVarDsc&           varDsc,
                                           GenTreeLclVarCommon* lclVarNode)
{
    assert(lclVarNode != nullptr);

    if (!varTypeIsStruct(varDsc.lvType) || (lvaGetPromotionType(&varDsc) == PROMOTION_TYPE_NONE))
    {
        return;
    }

    VARSET_TP varBit(VarSetOps::MakeEmpty(this));

    for (unsigned i = varDsc.lvFieldLclStart; i < varDsc.lvFieldLclStart + varDsc.lvFieldCnt; ++i)
    {
#if !defined(_TARGET_64BIT_)
        if (!varTypeIsLong(lvaTable[i].lvType) || !lvaTable[i].lvPromoted)
#endif // !defined(_TARGET_64BIT_)
        {
            noway_assert(lvaTable[i].lvIsStructField);
        }
        if (lvaTable[i].lvTracked)
        {
            const unsigned varIndex = lvaTable[i].lvVarIndex;
            noway_assert(varIndex < lvaTrackedCount);
            VarSetOps::AddElemD(this, varBit, varIndex);
        }
    }
    if (lclVarNode->gtFlags & GTF_VAR_DEF)
    {
        VarSetOps::DiffD(this, varBit, keepAliveVars);
        VarSetOps::DiffD(this, life, varBit);
        return;
    }
    // This is a use.

    // Are the variables already known to be alive?
    if (VarSetOps::IsSubset(this, varBit, life))
    {
        lclVarNode->gtFlags &= ~GTF_VAR_DEATH; // Since we may now call this multiple times, reset if live.
        return;
    }

    // Some variables are being used, and they are not currently live.
    // So they are just coming to life, in the backwards traversal; in a forwards
    // traversal, one or more are dying.  Mark this.

    lclVarNode->gtFlags |= GTF_VAR_DEATH;

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
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeLocal:
//    Compute the changes to local var liveness due to a use or a def of a local var and indicates whether the use/def
//    is a dead store.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - The current set of variables to keep alive regardless of their actual lifetime.
//    lclVarNode    - The node that corresponds to the local var def or use.
//
// Returns:
//    `true` if the local var node corresponds to a dead store; `false` otherwise.
bool Compiler::fgComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode)
{
    unsigned lclNum = lclVarNode->gtLclVarCommon.gtLclNum;

    assert(lclNum < lvaCount);
    LclVarDsc& varDsc = lvaTable[lclNum];

    // Is this a tracked variable?
    if (varDsc.lvTracked)
    {
        /* Is this a definition or use? */
        if (lclVarNode->gtFlags & GTF_VAR_DEF)
        {
            return fgComputeLifeTrackedLocalDef(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon());
        }
        else
        {
            fgComputeLifeTrackedLocalUse(life, varDsc, lclVarNode->AsLclVarCommon());
        }
    }
    else
    {
        fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon());
    }
    return false;
}

/*****************************************************************************
 *
 * Compute the set of live variables at each node in a given statement
 * or subtree of a statement moving backward from startNode to endNode
 */

void Compiler::fgComputeLife(VARSET_TP&       life,
                             GenTree*         startNode,
                             GenTree*         endNode,
                             VARSET_VALARG_TP volatileVars,
                             bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    GenTree* tree;

    // Don't kill vars in scope
    VARSET_TP keepAliveVars(VarSetOps::Union(this, volatileVars, compCurBB->bbScope));

    noway_assert(VarSetOps::IsSubset(this, keepAliveVars, life));
    noway_assert(endNode || (startNode == compCurStmt->gtStmtExpr));

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
            bool isDeadStore = fgComputeLifeLocal(life, keepAliveVars, tree);
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
}

void Compiler::fgComputeLifeLIR(VARSET_TP& life, BasicBlock* block, VARSET_VALARG_TP volatileVars)
{
    // Don't kill volatile vars and vars in scope.
    VARSET_TP keepAliveVars(VarSetOps::Union(this, volatileVars, block->bbScope));

    noway_assert(VarSetOps::IsSubset(this, keepAliveVars, life));

    LIR::Range& blockRange      = LIR::AsRange(block);
    GenTree*    firstNonPhiNode = blockRange.FirstNonPhiNode();
    if (firstNonPhiNode == nullptr)
    {
        return;
    }
    for (GenTree *node = blockRange.LastNode(), *next = nullptr, *end = firstNonPhiNode->gtPrev; node != end;
         node = next)
    {
        next = node->gtPrev;

        bool isDeadStore;
        switch (node->OperGet())
        {
            case GT_CALL:
            {
                GenTreeCall* const call = node->AsCall();
                if (((call->TypeGet() == TYP_VOID) || call->IsUnusedValue()) && !call->HasSideEffects(this))
                {
                    JITDUMP("Removing dead call:\n");
                    DISPNODE(call);

                    node->VisitOperands([](GenTree* operand) -> GenTree::VisitResult {
                        if (operand->IsValue())
                        {
                            operand->SetUnusedValue();
                        }

                        // Special-case PUTARG_STK: since this operator is not considered a value, DCE will not remove
                        // these nodes.
                        if (operand->OperIs(GT_PUTARG_STK))
                        {
                            operand->AsPutArgStk()->gtOp1->SetUnusedValue();
                            operand->gtBashToNOP();
                        }

                        return GenTree::VisitResult::Continue;
                    });

                    blockRange.Remove(node);

                    // Removing a call does not affect liveness unless it is a tail call in a nethod with P/Invokes or
                    // is itself a P/Invoke, in which case it may affect the liveness of the frame root variable.
                    if (!opts.MinOpts() && !opts.ShouldUsePInvokeHelpers() &&
                        ((call->IsTailCall() && info.compCallUnmanaged) || call->IsUnmanaged()) &&
                        lvaTable[info.compLvFrameListRoot].lvTracked)
                    {
                        fgStmtRemoved = true;
                    }
                }
                else
                {
                    fgComputeLifeCall(life, call);
                }
                break;
            }

            case GT_LCL_VAR:
            case GT_LCL_FLD:
            {
                GenTreeLclVarCommon* const lclVarNode = node->AsLclVarCommon();
                LclVarDsc&                 varDsc     = lvaTable[lclVarNode->gtLclNum];

                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead LclVar use:\n");
                    DISPNODE(lclVarNode);

                    blockRange.Delete(this, block, node);
                    if (varDsc.lvTracked && !opts.MinOpts())
                    {
                        fgStmtRemoved = true;
                    }
                }
                else if (varDsc.lvTracked)
                {
                    fgComputeLifeTrackedLocalUse(life, varDsc, lclVarNode);
                }
                else
                {
                    fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode);
                }
                break;
            }

            case GT_LCL_VAR_ADDR:
            case GT_LCL_FLD_ADDR:
                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead LclVar address:\n");
                    DISPNODE(node);

                    const bool isTracked = lvaTable[node->AsLclVarCommon()->gtLclNum].lvTracked;
                    blockRange.Delete(this, block, node);
                    if (isTracked && !opts.MinOpts())
                    {
                        fgStmtRemoved = true;
                    }
                }
                else
                {
                    isDeadStore = fgComputeLifeLocal(life, keepAliveVars, node);
                    if (isDeadStore)
                    {
                        LIR::Use addrUse;
                        if (blockRange.TryGetUse(node, &addrUse) && (addrUse.User()->OperGet() == GT_STOREIND))
                        {
                            // Remove the store. DCE will iteratively clean up any ununsed operands.
                            GenTreeStoreInd* const store = addrUse.User()->AsStoreInd();

                            JITDUMP("Removing dead indirect store:\n");
                            DISPNODE(store);

                            assert(store->Addr() == node);
                            blockRange.Delete(this, block, node);

                            store->Data()->SetUnusedValue();

                            blockRange.Remove(store);

                            assert(!opts.MinOpts());
                            fgStmtRemoved = true;
                        }
                    }
                }
                break;

            case GT_STORE_LCL_VAR:
            case GT_STORE_LCL_FLD:
            {
                GenTreeLclVarCommon* const lclVarNode = node->AsLclVarCommon();

                LclVarDsc& varDsc = lvaTable[lclVarNode->gtLclNum];
                if (varDsc.lvTracked)
                {
                    isDeadStore = fgComputeLifeTrackedLocalDef(life, keepAliveVars, varDsc, lclVarNode);
                    if (isDeadStore)
                    {
                        JITDUMP("Removing dead store:\n");
                        DISPNODE(lclVarNode);

                        // Remove the store. DCE will iteratively clean up any ununsed operands.
                        lclVarNode->gtOp1->SetUnusedValue();

                        // If the store is marked as a late argument, it is referenced by a call. Instead of removing
                        // it, bash it to a NOP.
                        if ((node->gtFlags & GTF_LATE_ARG) != 0)
                        {
                            JITDUMP("node is a late arg; replacing with NOP\n");
                            node->gtBashToNOP();

                            // NOTE: this is a bit of a hack. We need to keep these nodes around as they are
                            // referenced by the call, but they're considered side-effect-free non-value-producing
                            // nodes, so they will be removed if we don't do this.
                            node->gtFlags |= GTF_ORDER_SIDEEFF;
                        }
                        else
                        {
                            blockRange.Remove(node);
                        }

                        assert(!opts.MinOpts());
                        fgStmtRemoved = true;
                    }
                }
                else
                {
                    fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode);
                }
                break;
            }

            case GT_LABEL:
            case GT_FTN_ADDR:
            case GT_CNS_INT:
            case GT_CNS_LNG:
            case GT_CNS_DBL:
            case GT_CNS_STR:
            case GT_CLS_VAR_ADDR:
            case GT_PHYSREG:
                // These are all side-effect-free leaf nodes.
                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead node:\n");
                    DISPNODE(node);

                    blockRange.Remove(node);
                }
                break;

            case GT_LOCKADD:
            case GT_XADD:
            case GT_XCHG:
            case GT_CMPXCHG:
            case GT_MEMORYBARRIER:
            case GT_JMP:
            case GT_STOREIND:
            case GT_ARR_BOUNDS_CHECK:
            case GT_STORE_OBJ:
            case GT_STORE_BLK:
            case GT_STORE_DYN_BLK:
#if defined(FEATURE_SIMD)
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            case GT_JCMP:
            case GT_CMP:
            case GT_JCC:
            case GT_JTRUE:
            case GT_RETURN:
            case GT_SWITCH:
            case GT_RETFILT:
            case GT_START_NONGC:
            case GT_START_PREEMPTGC:
            case GT_PROF_HOOK:
#if !FEATURE_EH_FUNCLETS
            case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            case GT_SWITCH_TABLE:
            case GT_PINVOKE_PROLOG:
            case GT_PINVOKE_EPILOG:
            case GT_RETURNTRAP:
            case GT_PUTARG_STK:
            case GT_IL_OFFSET:
#ifdef FEATURE_HW_INTRINSICS
            case GT_HWIntrinsic:
#endif // FEATURE_HW_INTRINSICS
                // Never remove these nodes, as they are always side-effecting.
                //
                // NOTE: the only side-effect of some of these nodes (GT_CMP, GT_SUB_HI) is a write to the flags
                // register.
                // Properly modeling this would allow these nodes to be removed.
                break;

            case GT_NOP:
                // NOTE: we need to keep some NOPs around because they are referenced by calls. See the dead store
                // removal code above (case GT_STORE_LCL_VAR) for more explanation.
                if ((node->gtFlags & GTF_ORDER_SIDEEFF) != 0)
                {
                    break;
                }
                __fallthrough;

            default:
                assert(!node->OperIsLocal());
                if (!node->IsValue() || node->IsUnusedValue())
                {
                    // We are only interested in avoiding the removal of nodes with direct side-effects
                    // (as opposed to side effects of their children).
                    // This default case should never include calls or assignments.
                    assert(!node->OperRequiresAsgFlag() && !node->OperIs(GT_CALL));
                    if (!node->gtSetFlags() && !node->OperMayThrow(this))
                    {
                        JITDUMP("Removing dead node:\n");
                        DISPNODE(node);

                        node->VisitOperands([](GenTree* operand) -> GenTree::VisitResult {
                            operand->SetUnusedValue();
                            return GenTree::VisitResult::Continue;
                        });

                        blockRange.Remove(node);
                    }
                }
                break;
        }
    }
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

bool Compiler::fgRemoveDeadStore(GenTree**        pTree,
                                 LclVarDsc*       varDsc,
                                 VARSET_VALARG_TP life,
                                 bool*            doAgain,
                                 bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
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

    // Next, find the assignment (i.e. if we didn't have a LocalStore)
    if (asgNode == nullptr)
    {
        if (addrNode == nullptr)
        {
            asgNode = nextNode;
        }
        else
        {
            // This may be followed by GT_IND/assign or GT_STOREIND.
            if (nextNode == nullptr)
            {
                return false;
            }
            if (nextNode->OperIsIndir())
            {
                // This must be a non-nullcheck form of indir, or it would not be a def.
                assert(nextNode->OperGet() != GT_NULLCHECK);
                if (nextNode->OperIsStore())
                {
                    // This is a store, which takes a location and a value to be stored.
                    // It's 'rhsNode' is the value to be stored.
                    asgNode = nextNode;
                    if (asgNode->OperIsBlk())
                    {
                        rhsNode = asgNode->AsBlk()->Data();
                    }
                    else
                    {
                        // This is a non-block store.
                        rhsNode = asgNode->gtGetOp2();
                    }
                }
                else
                {
                    // This is a non-store indirection, and the assignment will com after it.
                    asgNode = nextNode->gtNext;
                }
            }
        }
    }

    if (asgNode == nullptr)
    {
        return false;
    }

    if (asgNode->OperIs(GT_ASG))
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

        assert(asgNode->OperIs(GT_ASG));

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

        // Check for side effects
        GenTree* sideEffList = nullptr;
        if (rhsNode->gtFlags & GTF_SIDE_EFFECT)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf(FMT_BB " - Dead assignment has side effects...\n", compCurBB->bbNum);
                gtDispTree(asgNode);
                printf("\n");
            }
#endif // DEBUG
            // Extract the side effects
            if (rhsNode->TypeGet() == TYP_STRUCT)
            {
                // This is a block assignment. An indirection of the rhs is not considered to
                // happen until the assignment, so we will extract the side effects from only
                // the address.
                if (rhsNode->OperIsIndir())
                {
                    assert(rhsNode->OperGet() != GT_NULLCHECK);
                    rhsNode = rhsNode->AsIndir()->Addr();
                }
            }
            gtExtractSideEffList(rhsNode, &sideEffList);
        }

        // Test for interior statement

        if (asgNode->gtNext == nullptr)
        {
            // This is a "NORMAL" statement with the assignment node hanging from the GT_STMT node.

            noway_assert(compCurStmt->gtStmtExpr == asgNode);
            JITDUMP("top level assign\n");

            if (sideEffList != nullptr)
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

                // Replace the assignment statement with the list of side effects
                noway_assert(sideEffList->gtOper != GT_STMT);

                *pTree = compCurStmt->gtStmtExpr = sideEffList;
#ifdef DEBUG
                *treeModf = true;
#endif // DEBUG
                // Update ordering, costs, FP levels, etc.
                gtSetStmtInfo(compCurStmt);

                // Re-link the nodes for this statement
                fgSetStmtSeq(compCurStmt);

                // Since the whole statement gets replaced it is safe to
                // re-thread and update order. No need to compute costs again.
                *pStmtInfoDirty = false;

                // Compute the live set for the new statement
                *doAgain = true;
                return false;
            }
            else
            {
                JITDUMP("removing stmt with no side effects\n");

                // No side effects - remove the whole statement from the block->bbTreeList
                fgRemoveStmt(compCurBB, compCurStmt);

                // Since we removed it do not process the rest (i.e. RHS) of the statement
                // variables in the RHS will not be marked as live, so we get the benefit of
                // propagating dead variables up the chain
                return true;
            }
        }
        else
        {
            // This is an INTERIOR STATEMENT with a dead assignment - remove it
            noway_assert(!VarSetOps::IsMember(this, life, varDsc->lvVarIndex));

            if (sideEffList != nullptr)
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
                if (sideEffList->gtOper == asgNode->gtOper)
                {
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG
                    asgNode->gtOp.gtOp1 = sideEffList->gtOp.gtOp1;
                    asgNode->gtOp.gtOp2 = sideEffList->gtOp.gtOp2;
                    asgNode->gtType     = sideEffList->gtType;
                }
                else
                {
#ifdef DEBUG
                    *treeModf = true;
#endif // DEBUG
                    // Change the node to a GT_COMMA holding the side effect list
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
#ifdef DEBUG
                if (verbose)
                {
                    printf("\nRemoving tree ");
                    printTreeID(asgNode);
                    printf(" in " FMT_BB " as useless\n", compCurBB->bbNum);
                    gtDispTree(asgNode);
                    printf("\n");
                }
#endif // DEBUG
                // No side effects - Change the assignment to a GT_NOP node
                asgNode->gtBashToNOP();

#ifdef DEBUG
                *treeModf = true;
#endif // DEBUG
            }

            // Re-link the nodes for this statement - Do not update ordering!

            // Do not update costs by calling gtSetStmtInfo. fgSetStmtSeq modifies
            // the tree threading based on the new costs. Removing nodes could
            // cause a subtree to get evaluated first (earlier second) during the
            // liveness walk. Instead just set a flag that costs are dirty and
            // caller has to call gtSetStmtInfo.
            *pStmtInfoDirty = true;

            fgSetStmtSeq(compCurStmt);

            // Continue analysis from this node

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

    /* For debuggable code, we mark vars as live over their entire
     * reported scope, so that it will be visible over the entire scope
     */

    if (opts.compDbgCode && (info.compVarScopesCount > 0))
    {
        fgExtendDbgLifetimes();
    }

    // Nothing more to be done if the backend does not require accurate local var lifetimes.
    if (!backendRequiresLocalVarLifetimes())
    {
        fgLocalVarLivenessDone = true;
        return;
    }

    /*-------------------------------------------------------------------------
     * Variables involved in exception-handlers and finally blocks need
     * to be specially marked
     */
    BasicBlock* block;

    VARSET_TP exceptVars(VarSetOps::MakeEmpty(this));  // vars live on entry to a handler
    VARSET_TP finallyVars(VarSetOps::MakeEmpty(this)); // vars live on exit of a 'finally' block
    VARSET_TP filterVars(VarSetOps::MakeEmpty(this));  // vars live on exit from a 'filter'

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

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        /* Tell everyone what block we're working on */

        compCurBB = block;

        /* Remember those vars live on entry to exception handlers */
        /* if we are part of a try block */

        VARSET_TP volatileVars(VarSetOps::MakeEmpty(this));

        if (ehBlockHasExnFlowDsc(block))
        {
            VarSetOps::Assign(this, volatileVars, fgGetHandlerLiveVars(block));

            // volatileVars is a subset of exceptVars
            noway_assert(VarSetOps::IsSubset(this, volatileVars, exceptVars));
        }

        /* Start with the variables live on exit from the block */

        VARSET_TP life(VarSetOps::MakeCopy(this, block->bbLiveOut));

        /* Mark any interference we might have at the end of the block */

        if (block->IsLIR())
        {
            fgComputeLifeLIR(life, block, volatileVars);
        }
        else
        {
            /* Get the first statement in the block */

            GenTreeStmt* firstStmt = block->FirstNonPhiDef();

            if (firstStmt == nullptr)
            {
                continue;
            }

            /* Walk all the statements of the block backwards - Get the LAST stmt */

            GenTreeStmt* nextStmt = block->lastStmt();

            do
            {
#ifdef DEBUG
                bool treeModf = false;
#endif // DEBUG
                noway_assert(nextStmt != nullptr);

                compCurStmt = nextStmt;
                nextStmt    = nextStmt->getPrevStmt();

                /* Compute the liveness for each tree node in the statement */
                bool stmtInfoDirty = false;

                fgComputeLife(life, compCurStmt->gtStmtExpr, nullptr, volatileVars, &stmtInfoDirty DEBUGARG(&treeModf));

                if (stmtInfoDirty)
                {
                    gtSetStmtInfo(compCurStmt);
                    fgSetStmtSeq(compCurStmt);
                    gtUpdateStmtSideEffects(compCurStmt);
                }

#ifdef DEBUG
                if (verbose && treeModf)
                {
                    printf("\nfgComputeLife modified tree:\n");
                    gtDispTree(compCurStmt->gtStmtExpr);
                    printf("\n");
                }
#endif // DEBUG
            } while (compCurStmt != firstStmt);
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

            noway_assert(VarSetOps::IsSubset(this, life, block->bbLiveIn));

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
    VARSET_TP allVars(VarSetOps::Union(this, block->bbLiveIn, block->bbLiveOut));
    printf(FMT_BB, block->bbNum);
    printf(" IN (%d)=", VarSetOps::Count(this, block->bbLiveIn));
    lvaDispVarSet(block->bbLiveIn, allVars);
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((block->bbMemoryLiveIn & memoryKindSet(memoryKind)) != 0)
        {
            printf(" + %s", memoryKindNames[memoryKind]);
        }
    }
    printf("\n     OUT(%d)=", VarSetOps::Count(this, block->bbLiveOut));
    lvaDispVarSet(block->bbLiveOut, allVars);
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((block->bbMemoryLiveOut & memoryKindSet(memoryKind)) != 0)
        {
            printf(" + %s", memoryKindNames[memoryKind]);
        }
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
