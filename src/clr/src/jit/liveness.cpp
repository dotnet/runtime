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
#ifndef LEGACY_BACKEND
#include "lower.h" // for LowerRange()
#endif

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
    if (varDsc->lvRefCnt == 0 && (!varTypeIsPromotable(varDsc) || !varDsc->lvPromoted))
    {
        JITDUMP("Found reference to V%02u with zero refCnt.\n", lclNum);
        assert(!"We should never encounter a reference to a lclVar that has a zero refCnt.");
        varDsc->lvRefCnt = 1;
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
        VARSET_TP allOnes(VarSetOps::MakeFull(this));
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
                GenTreePtr           addrArg         = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
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
            if (tree->OperIsAssignment() || tree->OperIsBlkOp())
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
        if (!block->IsLIR())
        {
            for (GenTreeStmt* stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNextStmt)
            {
                compCurStmt = stmt;

#ifdef LEGACY_BACKEND
                GenTree* tree = fgLegacyPerStatementLocalVarLiveness(stmt->gtStmtList, nullptr);
                assert(tree == nullptr);
#else  // !LEGACY_BACKEND
                for (GenTree* node = stmt->gtStmtList; node != nullptr; node = node->gtNext)
                {
                    fgPerNodeLocalVarLiveness(node);
                }
#endif // !LEGACY_BACKEND
            }
        }
        else
        {
#ifdef LEGACY_BACKEND
            unreached();
#else  // !LEGACY_BACKEND
            for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
            {
                fgPerNodeLocalVarLiveness(node);
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
            VARSET_TP allVars(VarSetOps::Union(this, fgCurUseSet, fgCurDefSet));
            printf("BB%02u", block->bbNum);
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
        unsigned blockWeight = block->getBBWeight(this);

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
                    GenTree* initStmt = gtNewStmt(initNode);
                    gtSetStmtInfo(initStmt);
                    fgSetStmtSeq(initStmt);
                    fgInsertStmtNearEnd(block, initStmt);
                }
                else
                {
                    GenTree* store =
                        new (this, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, type, varNum, BAD_IL_OFFSET);
                    store->gtOp.gtOp1 = zero;
                    store->gtFlags |= (GTF_VAR_DEF | GTF_ASG);

                    LIR::Range initRange = LIR::EmptyRange();
                    initRange.InsertBefore(nullptr, zero, store);

#ifndef LEGACY_BACKEND
#if !defined(_TARGET_64BIT_)
                    DecomposeLongs::DecomposeRange(this, blockWeight, initRange);
#endif // !defined(_TARGET_64BIT_)
                    m_pLowering->LowerRange(block, initRange);
#endif // !LEGACY_BACKEND

                    // Naively inserting the initializer at the end of the block may add code after the block's
                    // terminator, in which case the inserted code will never be executed (and the IR for the
                    // block will be invalid). Use `LIR::InsertBeforeTerminator` to avoid this problem.
                    LIR::InsertBeforeTerminator(block, std::move(initRange));
                }

#ifdef DEBUG
                if (verbose)
                {
                    printf("Created zero-init of V%02u in BB%02u\n", varNum, block->bbNum);
                }
#endif // DEBUG

                varDsc->incRefCnts(block->getBBWeight(this), this);

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

        /* Can exceptions from this block be handled (in this function)? */

        if (m_compiler->ehBlockHasExnFlowDsc(block))
        {
            const VARSET_TP& liveVars(m_compiler->fgGetHandlerLiveVars(block));

            VarSetOps::UnionD(m_compiler, m_liveIn, liveVars);
            VarSetOps::UnionD(m_compiler, m_liveOut, liveVars);
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
                        printf("Scope info: block BB%02u LiveIn+ ", block->bbNum);
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

//------------------------------------------------------------------------
// Compiler::fgMarkIntf:
//    Mark any variables in varSet1 as interfering with any variables
//    specified in varSet2.
//
//    We ensure that the interference graph is reflective: if T_x
//    interferes with T_y, then T_y interferes with T_x.
//
//    Note that this function is a no-op when targeting the RyuJIT
//    backend, as it does not require the interference graph.
//
// Arguments:
//    varSet1 - The first set of variables.
//    varSet2 - The second set of variables.
//
// Returns:
//    True if any new interferences were recorded; false otherwise.
//
bool Compiler::fgMarkIntf(VARSET_VALARG_TP varSet1, VARSET_VALARG_TP varSet2)
{
#ifdef LEGACY_BACKEND
    /* If either set has no bits set (or we are not optimizing), take an early out */
    if (opts.MinOpts() || VarSetOps::IsEmpty(this, varSet2) || VarSetOps::IsEmpty(this, varSet1))
    {
        return false;
    }

    bool addedIntf = false; // This is set to true if we add any new interferences

    VarSetOps::Assign(this, fgMarkIntfUnionVS, varSet1);
    VarSetOps::UnionD(this, fgMarkIntfUnionVS, varSet2);

    VarSetOps::Iter iter(this, fgMarkIntfUnionVS);
    unsigned        refIndex = 0;
    while (iter.NextElem(&refIndex))
    {
        // if varSet1 has this bit set then it interferes with varSet2
        if (VarSetOps::IsMember(this, varSet1, refIndex))
        {
            // Calculate the set of new interference to add
            VARSET_TP newIntf(VarSetOps::Diff(this, varSet2, lvaVarIntf[refIndex]));
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
            VARSET_TP newIntf(VarSetOps::Diff(this, varSet1, lvaVarIntf[refIndex]));
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

//------------------------------------------------------------------------
// Compiler::fgMarkIntf:
//    Mark any variables in varSet1 as interfering with the variable
//    specified by varIndex.
//
//    We ensure that the interference graph is reflective: if T_x
//    interferes with T_y, then T_y interferes with T_x.
//
//    Note that this function is a no-op when targeting the RyuJIT
//    backend, as it does not require the interference graph.
//
// Arguments:
//    varSet1  - The first set of variables.
//    varIndex - The second variable.
//
// Returns:
//    True if any new interferences were recorded; false otherwise.
//
bool Compiler::fgMarkIntf(VARSET_VALARG_TP varSet, unsigned varIndex)
{
#ifdef LEGACY_BACKEND
    // If the input set has no bits set (or we are not optimizing), take an early out
    if (opts.MinOpts() || VarSetOps::IsEmpty(this, varSet))
    {
        return false;
    }

    bool addedIntf = false; // This is set to true if we add any new interferences

    VarSetOps::Assign(this, fgMarkIntfUnionVS, varSet);
    VarSetOps::AddElemD(this, fgMarkIntfUnionVS, varIndex);

    VarSetOps::Iter iter(this, fgMarkIntfUnionVS);
    unsigned        refIndex = 0;
    while (iter.NextElem(&refIndex))
    {
        // if varSet has this bit set then it interferes with varIndex
        if (VarSetOps::IsMember(this, varSet, refIndex))
        {
            // Calculate the set of new interference to add
            if (!VarSetOps::IsMember(this, lvaVarIntf[refIndex], varIndex))
            {
                addedIntf = true;
                VarSetOps::AddElemD(this, lvaVarIntf[refIndex], varIndex);
            }
        }

        // if this bit is the same as varIndex then it interferes with varSet1
        if (refIndex == varIndex)
        {
            // Calculate the set of new interference to add
            VARSET_TP newIntf(VarSetOps::Diff(this, varSet, lvaVarIntf[refIndex]));
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

    VarSetOps::Iter iter(this, varSet);
    unsigned        refIndex = 0;
    while (iter.NextElem(&refIndex))
    {
        // Calculate the set of new interference to add
        VARSET_TP newIntf(VarSetOps::Diff(this, varSet, lvaVarIntf[refIndex]));
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
    VARSET_TP newLiveSet(VarSetOps::MakeCopy(this, liveSet));
    assert(fgLocalVarLivenessDone == true);
    GenTreePtr lclVarTree = tree; // After the tests below, "lclVarTree" will be the local variable.
    if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_FLD || tree->gtOper == GT_REG_VAR ||
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
                VarSetOps::AddElemD(this, life, frameVarDsc->lvVarIndex);

                // Record interference with other live variables
                fgMarkIntf(life, frameVarDsc->lvVarIndex);
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

                // Record an interference with the other live variables
                fgMarkIntf(life, varIndex);
            }
        }

#ifdef LEGACY_BACKEND
        /* Do we have any live variables? */
        if (!VarSetOps::IsEmpty(this, life))
        {
            // For each live variable if it is a GC-ref type, mark it volatile to prevent if from being enregistered
            // across the unmanaged call.
            //
            // Note that this is not necessary when targeting the RyuJIT backend, as its RA handles these kills itself.

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
#endif // LEGACY_BACKEND
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

    // Record interference with other live variables
    fgMarkIntf(life, varIndex);
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
//    lclVarNode    - The node that corresponds to the local var def or use. Only differs from `node` when targeting
//                    the legacy backend.
//    node          - The actual tree node being processed.
void Compiler::fgComputeLifeUntrackedLocal(
    VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, LclVarDsc& varDsc, GenTreeLclVarCommon* lclVarNode, GenTree* node)
{
    assert(lclVarNode != nullptr);
    assert(node != nullptr);

    if (!varTypeIsStruct(varDsc.lvType) || (lvaGetPromotionType(&varDsc) == PROMOTION_TYPE_NONE))
    {
        return;
    }

    VARSET_TP varBit(VarSetOps::MakeEmpty(this));

    for (unsigned i = varDsc.lvFieldLclStart; i < varDsc.lvFieldLclStart + varDsc.lvFieldCnt; ++i)
    {
#if !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
        if (!varTypeIsLong(lvaTable[i].lvType) || !lvaTable[i].lvPromoted)
#endif // !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
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
    if (node->gtFlags & GTF_VAR_DEF)
    {
        VarSetOps::DiffD(this, varBit, keepAliveVars);
        VarSetOps::DiffD(this, life, varBit);
        return;
    }
    // This is a use.

    // Are the variables already known to be alive?
    if (VarSetOps::IsSubset(this, varBit, life))
    {
        node->gtFlags &= ~GTF_VAR_DEATH; // Since we may now call this multiple times, reset if live.
        return;
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

//------------------------------------------------------------------------
// Compiler::fgComputeLifeLocal:
//    Compute the changes to local var liveness due to a use or a def of a local var and indicates whether the use/def
//    is a dead store.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - The current set of variables to keep alive regardless of their actual lifetime.
//    lclVarNode    - The node that corresponds to the local var def or use. Only differs from `node` when targeting
//                    the legacy backend.
//    node          - The actual tree node being processed.
//
// Returns:
//    `true` if the local var node corresponds to a dead store; `false` otherwise.
bool Compiler::fgComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode, GenTree* node)
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
        fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon(), node);
    }
    return false;
}

/*****************************************************************************
 *
 * Compute the set of live variables at each node in a given statement
 * or subtree of a statement moving backward from startNode to endNode
 */

#ifndef LEGACY_BACKEND
void Compiler::fgComputeLife(VARSET_TP&       life,
                             GenTreePtr       startNode,
                             GenTreePtr       endNode,
                             VARSET_VALARG_TP volatileVars,
                             bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    GenTreePtr tree;
    unsigned   lclNum;

    // Don't kill vars in scope
    VARSET_TP keepAliveVars(VarSetOps::Union(this, volatileVars, compCurBB->bbScope));

    noway_assert(VarSetOps::IsSubset(this, keepAliveVars, life));
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
                    fgStmtRemoved = !opts.MinOpts() && !opts.ShouldUsePInvokeHelpers() &&
                                    ((call->IsTailCall() && info.compCallUnmanaged) || call->IsUnmanaged()) &&
                                    lvaTable[info.compLvFrameListRoot].lvTracked;
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
                    fgStmtRemoved = varDsc.lvTracked && !opts.MinOpts();
                }
                else if (varDsc.lvTracked)
                {
                    fgComputeLifeTrackedLocalUse(life, varDsc, lclVarNode);
                }
                else
                {
                    fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode, node);
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
                    fgStmtRemoved = isTracked && !opts.MinOpts();
                }
                else
                {
                    isDeadStore = fgComputeLifeLocal(life, keepAliveVars, node, node);
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

                        lvaDecRefCnts(block, node);

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
                    fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode, node);
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
            case GT_JCMP:
            case GT_CMP:
            case GT_JCC:
            case GT_JTRUE:
            case GT_RETURN:
            case GT_SWITCH:
            case GT_RETFILT:
            case GT_START_NONGC:
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
                    unsigned sideEffects = node->gtFlags & (GTF_SIDE_EFFECT | GTF_SET_FLAGS);
                    if ((sideEffects == 0) || ((sideEffects == GTF_EXCEPT) && !node->OperMayThrow(this)))
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

#else // LEGACY_BACKEND

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

void Compiler::fgComputeLife(VARSET_TP&       life,
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

    // TBD: This used to be an initialization to VARSET_NOT_ACCEPTABLE.  Try to figure out what's going on here.
    VARSET_TP  entryLiveSet(VarSetOps::MakeFull(this));   // liveness when we see gtQMark
    VARSET_TP  gtColonLiveSet(VarSetOps::MakeFull(this)); // liveness when we see gtColon
    GenTreePtr gtColon = NULL;

    VARSET_TP keepAliveVars(VarSetOps::Union(this, volatileVars, compCurBB->bbScope)); /* Dont kill vars in scope */

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
                    const VARSET_TP& tmpVS(gtQMark->gtQmark.gtElseLiveSet);
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

                    fgComputeLife(life, tree, nextColonExit, volatileVars, pStmtInfoDirty DEBUGARG(treeModf));

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
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // !LEGACY_BACKEND

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
            if (nextNode->OperIsIndir())
            {
                // This must be a non-nullcheck form of indir, or it would not be a def.
                assert(nextNode->OperGet() != GT_NULLCHECK);
                if (nextNode->OperIsStore())
                {
                    asgNode = nextNode;
                    if (asgNode->OperIsBlk())
                    {
                        rhsNode = asgNode->AsBlk()->Data();
                    }
                    // TODO-1stClassStructs: There should be an else clause here to handle
                    // the non-block forms of store ops (GT_STORE_LCL_VAR, etc.) for which
                    // rhsNode is op1. (This isn't really a 1stClassStructs item, but the
                    // above was added to catch what used to be dead block ops, and that
                    // made this omission apparent.)
                }
                else
                {
                    asgNode = nextNode->gtNext;
                }
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
                    asgNode->SetOperRaw(GT_ADD);
                    break;
                case GT_ASG_SUB:
                    asgNode->SetOperRaw(GT_SUB);
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

    // This is used in the liveness computation, as a temporary.
    VarSetOps::AssignNoCopy(this, fgMarkIntfUnionVS, VarSetOps::MakeEmpty(this));

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

                fgComputeLife(life, compCurStmt->gtStmt.gtStmtExpr, nullptr, volatileVars,
                              &stmtInfoDirty DEBUGARG(&treeModf));

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
            fgComputeLifeLIR(life, block, volatileVars);
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
    printf("BB%02u", block->bbNum);
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
