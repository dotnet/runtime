// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =================================================================================
//  Code that works with liveness and related concepts (interference, debug scope)
// =================================================================================

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if !defined(TARGET_64BIT)
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
    assert((tree->OperIsLocal() && (tree->OperGet() != GT_PHI_ARG)) || tree->OperIs(GT_LCL_ADDR));

    const unsigned   lclNum = tree->GetLclNum();
    LclVarDsc* const varDsc = lvaGetDesc(lclNum);

    // We should never encounter a reference to a lclVar that has a zero refCnt.
    if (varDsc->lvRefCnt(lvaRefCountState) == 0 && (!varTypeIsPromotable(varDsc) || !varDsc->lvPromoted))
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
        assert(!varDsc->IsAddressExposed());

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
        if (varDsc->IsAddressExposed())
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

        if (varTypeIsPromotable(varDsc))
        {
            lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

            if (promotionType != PROMOTION_TYPE_NONE)
            {
                for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
                {
                    if (!lvaTable[i].lvTracked)
                    {
                        continue;
                    }

                    unsigned varIndex = lvaTable[i].lvVarIndex;
                    if (isUse && !VarSetOps::IsMember(this, fgCurDefSet, varIndex))
                    {
                        VarSetOps::AddElemD(this, fgCurUseSet, varIndex);
                    }

                    if (isDef)
                    {
                        VarSetOps::AddElemD(this, fgCurDefSet, varIndex);
                    }
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

    // Sort locals first, if precise reference counts are required, e.g. we're optimizing
    if (PreciseRefCountsRequired())
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
// Template arguments:
//    lowered - Whether or not this is liveness on lowered IR, where LCL_ADDRs
//    on tracked locals may appear.
//
// Arguments:
//    tree       - The current node.
//
template <bool lowered>
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
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            fgMarkUseDef(tree->AsLclVarCommon());
            break;

        case GT_LCL_ADDR:
            if (lowered)
            {
                // If this is a definition of a retbuf then we process it as
                // part of the GT_CALL node.
                if (fgIsHiddenBufferAddressDef(LIR::AsRange(compCurBB), tree))
                {
                    break;
                }

                fgMarkUseDef(tree->AsLclVarCommon());
            }
            break;

        case GT_IND:
        case GT_BLK:
            // For Volatile indirection, first mutate GcHeap/ByrefExposed
            // see comments in ValueNum.cpp (under the memory read case)
            // This models Volatile reads as def-then-use of memory.
            // and allows for a CSE of a subsequent non-volatile read
            if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
            {
                // For any Volatile indirection, we must handle it as a
                // definition of the GcHeap/ByrefExposed
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            }

            fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            break;

        // We'll assume these are use-then-defs of memory.
        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
            fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            fgCurMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
            break;

        case GT_STOREIND:
        case GT_STORE_BLK:
        case GT_MEMORYBARRIER: // Similar to Volatile indirections, we must handle this as a memory def.
            fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
        {
            fgPerNodeLocalVarLiveness(tree->AsHWIntrinsic());
            break;
        }
#endif // FEATURE_HW_INTRINSICS

        // For now, all calls read/write GcHeap/ByrefExposed, writes in their entirety.  Might tighten this case later.
        case GT_CALL:
        {
            GenTreeCall* call    = tree->AsCall();
            bool         modHeap = true;
            if (call->IsHelperCall())
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

            // If this is a p/invoke unmanaged call or if this is a tail-call via helper,
            // and we have an unmanaged p/invoke call in the method,
            // then we're going to run the p/invoke epilog.
            // So we mark the FrameRoot as used by this instruction.
            // This ensures that the block->bbVarUse will contain
            // the FrameRoot local var if is it a tracked variable.

            if ((call->IsUnmanaged() || call->IsTailCallViaJitHelper()) && compMethodRequiresPInvokeFrame())
            {
                assert(!opts.ShouldUsePInvokeHelpers() || (info.compLvFrameListRoot == BAD_VAR_NUM));
                if (!opts.ShouldUsePInvokeHelpers() && !call->IsSuppressGCTransition())
                {
                    // Get the FrameRoot local and mark it as used.

                    LclVarDsc* varDsc = lvaGetDesc(info.compLvFrameListRoot);

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
                        }
                    }
                }
            }

            GenTreeLclVarCommon* definedLcl = gtCallGetDefinedRetBufLclAddr(call);
            if (definedLcl != nullptr)
            {
                fgMarkUseDef(definedLcl);
            }
            break;
        }

        default:
            break;
    }
}

#if defined(FEATURE_HW_INTRINSICS)
void Compiler::fgPerNodeLocalVarLiveness(GenTreeHWIntrinsic* hwintrinsic)
{
    NamedIntrinsic intrinsicId = hwintrinsic->GetHWIntrinsicId();

    // We can't call fgMutateGcHeap unless the block has recorded a MemoryDef
    //
    if (hwintrinsic->OperIsMemoryStoreOrBarrier())
    {
        // We currently handle this like a Volatile store or GT_MEMORYBARRIER
        // so it counts as a definition of GcHeap/ByrefExposed
        fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
    }
    else if (hwintrinsic->OperIsMemoryLoad())
    {
        // This instruction loads from memory and we need to record this information
        fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
    }
}
#endif // FEATURE_HW_INTRINSICS

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

    // Avoid allocations in the long case.
    VarSetOps::AssignNoCopy(this, fgCurUseSet, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, fgCurDefSet, VarSetOps::MakeEmpty(this));

    // GC Heap and ByrefExposed can share states unless we see a def of byref-exposed
    // memory that is not a GC Heap def.
    byrefStatesMatchGcHeapStates = true;

    for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_dfsTree->GetPostOrder(i - 1);
        VarSetOps::ClearD(this, fgCurUseSet);
        VarSetOps::ClearD(this, fgCurDefSet);

        fgCurMemoryUse   = emptyMemoryKindSet;
        fgCurMemoryDef   = emptyMemoryKindSet;
        fgCurMemoryHavoc = emptyMemoryKindSet;

        compCurBB = block;
        if (block->IsLIR())
        {
            for (GenTree* node : LIR::AsRange(block))
            {
                fgPerNodeLocalVarLiveness<true>(node);
            }
        }
        else if (fgNodeThreading == NodeThreading::AllTrees)
        {
            for (Statement* const stmt : block->NonPhiStatements())
            {
                compCurStmt = stmt;
                for (GenTree* const node : stmt->TreeList())
                {
                    fgPerNodeLocalVarLiveness<false>(node);
                }
            }
        }
        else
        {
            assert(fgIsDoingEarlyLiveness && (fgNodeThreading == NodeThreading::AllLocals));

            if (compQmarkUsed)
            {
                for (Statement* stmt : block->Statements())
                {
                    GenTree* dst;
                    GenTree* qmark = fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
                    if (qmark == nullptr)
                    {
                        for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                        {
                            fgMarkUseDef(lcl);
                        }
                    }
                    else
                    {
                        // Assigned local should be the very last local.
                        assert((dst == nullptr) ||
                               ((stmt->GetTreeListEnd() == dst) && ((dst->gtFlags & GTF_VAR_DEF) != 0)));

                        // Conservatively ignore defs that may be conditional
                        // but would otherwise still interfere with the
                        // lifetimes we compute here. We generally do not
                        // handle qmarks very precisely here -- last uses may
                        // not be marked as such due to interference with other
                        // qmark arms.
                        for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                        {
                            bool isUse = ((lcl->gtFlags & GTF_VAR_DEF) == 0) || ((lcl->gtFlags & GTF_VAR_USEASG) != 0);
                            // We can still handle the pure def at the top level.
                            bool conditional = lcl != dst;
                            if (isUse || !conditional)
                            {
                                fgMarkUseDef(lcl);
                            }
                        }
                    }
                }
            }
            else
            {
                for (Statement* stmt : block->Statements())
                {
                    for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                    {
                        fgMarkUseDef(lcl);
                    }
                }
            }
        }

        // Mark the FrameListRoot as used, if applicable.

        if (block->KindIs(BBJ_RETURN) && compMethodRequiresPInvokeFrame())
        {
            assert(!opts.ShouldUsePInvokeHelpers() || (info.compLvFrameListRoot == BAD_VAR_NUM));
            if (!opts.ShouldUsePInvokeHelpers())
            {
                // 32-bit targets always pop the frame in the epilog.
                // For 64-bit targets, we only do this in the epilog for IL stubs;
                // for non-IL stubs the frame is popped after every PInvoke call.
#ifdef TARGET_64BIT
                if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
#endif
                {
                    LclVarDsc* varDsc = lvaGetDesc(info.compLvFrameListRoot);

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
                        }
                    }
                }
            }
        }

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
        for (BasicBlock* block : Blocks())
        {
            VARSET_TP allVars(VarSetOps::Union(this, block->bbVarUse, block->bbVarDef));
            printf(FMT_BB, block->bbNum);
            printf(" USE(%d)=", VarSetOps::Count(this, block->bbVarUse));
            lvaDispVarSet(block->bbVarUse, allVars);
            for (MemoryKind memoryKind : allMemoryKinds())
            {
                if ((block->bbMemoryUse & memoryKindSet(memoryKind)) != 0)
                {
                    printf(" + %s", memoryKindNames[memoryKind]);
                }
            }
            printf("\n     DEF(%d)=", VarSetOps::Count(this, block->bbVarDef));
            lvaDispVarSet(block->bbVarDef, allVars);
            for (MemoryKind memoryKind : allMemoryKinds())
            {
                if ((block->bbMemoryDef & memoryKindSet(memoryKind)) != 0)
                {
                    printf(" + %s", memoryKindNames[memoryKind]);
                }
                if ((block->bbMemoryHavoc & memoryKindSet(memoryKind)) != 0)
                {
                    printf("*");
                }
            }
            printf("\n\n");
        }

        printf("** Memory liveness computed, GcHeap states and ByrefExposed states %s\n",
               (byrefStatesMatchGcHeapStates ? "match" : "diverge"));
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// fgAddHandlerLiveVars: determine set of locals live because of implicit
//   exception flow from a block.
//
// Arguments:
//    block - the block in question
//    ehHandlerLiveVars - On entry, contains an allocated VARSET_TP that the
//                        function will add handler live vars into.
//    memoryLiveness    - Set of memory liveness that will be added to.
//
void Compiler::fgAddHandlerLiveVars(BasicBlock* block, VARSET_TP& ehHandlerLiveVars, MemoryKindSet& memoryLiveness)
{
    assert(block->HasPotentialEHSuccs(this));

    block->VisitEHSuccs(this, [&](BasicBlock* succ) {
        VarSetOps::UnionD(this, ehHandlerLiveVars, succ->bbLiveIn);
        memoryLiveness |= succ->bbMemoryLiveIn;
        return BasicBlockVisit::Continue;
    });
}

class LiveVarAnalysis
{
    Compiler* m_compiler;

    unsigned  m_memoryLiveIn;
    unsigned  m_memoryLiveOut;
    VARSET_TP m_liveIn;
    VARSET_TP m_liveOut;
    VARSET_TP m_ehHandlerLiveVars;

    LiveVarAnalysis(Compiler* compiler)
        : m_compiler(compiler)
        , m_memoryLiveIn(emptyMemoryKindSet)
        , m_memoryLiveOut(emptyMemoryKindSet)
        , m_liveIn(VarSetOps::MakeEmpty(compiler))
        , m_liveOut(VarSetOps::MakeEmpty(compiler))
        , m_ehHandlerLiveVars(VarSetOps::MakeEmpty(compiler))
    {
    }

    bool PerBlockAnalysis(BasicBlock* block, bool keepAliveThis)
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

        if (m_compiler->fgIsDoingEarlyLiveness && m_compiler->opts.IsOSR() && block->HasFlag(BBF_RECURSIVE_TAILCALL))
        {
            // Early liveness happens between import and morph where we may
            // have identified a tailcall-to-loop candidate but not yet
            // expanded it. In OSR compilations we need to model the potential
            // backedge.
            //
            // Technically we would need to do this in normal compilations too,
            // but given that the tailcall-to-loop optimization is sound we can
            // rely on the call node we will see in this block having all the
            // necessary dependencies. That's not the case in OSR where the OSR
            // state index variable may be live at this point without appearing
            // as an explicit use anywhere.
            VarSetOps::UnionD(m_compiler, m_liveOut, m_compiler->fgEntryBB->bbLiveIn);
        }

        // Additionally, union in all the live-in tracked vars of regular
        // successors. EH successors need to be handled more conservatively
        // (their live-in state is live in this entire basic block). Those are
        // handled below.
        block->VisitRegularSuccs(m_compiler, [=](BasicBlock* succ) {
            VarSetOps::UnionD(m_compiler, m_liveOut, succ->bbLiveIn);
            m_memoryLiveOut |= succ->bbMemoryLiveIn;

            return BasicBlockVisit::Continue;
        });

        /* For lvaKeepAliveAndReportThis methods, "this" has to be kept alive everywhere
           Note that a function may end in a throw on an infinite loop (as opposed to a return).
           "this" has to be alive everywhere even in such methods. */

        if (keepAliveThis)
        {
            VarSetOps::AddElemD(m_compiler, m_liveOut, m_compiler->lvaTable[m_compiler->info.compThisArg].lvVarIndex);
        }

        /* Compute the 'm_liveIn'  set */
        VarSetOps::LivenessD(m_compiler, m_liveIn, block->bbVarDef, block->bbVarUse, m_liveOut);

        // Does this block have implicit exception flow to a filter or handler?
        // If so, include the effects of that flow.
        if (block->HasPotentialEHSuccs(m_compiler))
        {
            VarSetOps::ClearD(m_compiler, m_ehHandlerLiveVars);
            m_compiler->fgAddHandlerLiveVars(block, m_ehHandlerLiveVars, m_memoryLiveOut);
            VarSetOps::UnionD(m_compiler, m_liveIn, m_ehHandlerLiveVars);
            VarSetOps::UnionD(m_compiler, m_liveOut, m_ehHandlerLiveVars);
        }

        // Even if block->bbMemoryDef is set, we must assume that it doesn't kill memory liveness from m_memoryLiveOut,
        // since (without proof otherwise) the use and def may touch different memory at run-time.
        m_memoryLiveIn = m_memoryLiveOut | block->bbMemoryUse;

        // Has there been any change in either live set?

        bool liveInChanged = !VarSetOps::Equal(m_compiler, block->bbLiveIn, m_liveIn);
        if (liveInChanged || !VarSetOps::Equal(m_compiler, block->bbLiveOut, m_liveOut))
        {
            VarSetOps::Assign(m_compiler, block->bbLiveIn, m_liveIn);
            VarSetOps::Assign(m_compiler, block->bbLiveOut, m_liveOut);
        }

        const bool memoryLiveInChanged = (block->bbMemoryLiveIn != m_memoryLiveIn);
        if (memoryLiveInChanged || (block->bbMemoryLiveOut != m_memoryLiveOut))
        {
            block->bbMemoryLiveIn  = m_memoryLiveIn;
            block->bbMemoryLiveOut = m_memoryLiveOut;
        }

        return liveInChanged || memoryLiveInChanged;
    }

    void Run()
    {
        const bool keepAliveThis =
            m_compiler->lvaKeepAliveAndReportThis() && m_compiler->lvaTable[m_compiler->info.compThisArg].lvTracked;

        const FlowGraphDfsTree* dfsTree = m_compiler->m_dfsTree;
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

            for (unsigned i = 0; i < dfsTree->GetPostOrderCount(); i++)
            {
                BasicBlock* block = dfsTree->GetPostOrder(i);
                if (PerBlockAnalysis(block, keepAliveThis))
                {
                    changed = true;
                }
            }
        } while (changed && dfsTree->HasCycle());

        // If we had unremovable blocks that are not in the DFS tree then make
        // the 'keepAlive' set live in them. This would normally not be
        // necessary assuming those blocks are actually unreachable; however,
        // throw helpers fall into this category because we do not model them
        // correctly, and those will actually end up reachable. Fix that up
        // here.
        if (m_compiler->fgBBcount != dfsTree->GetPostOrderCount())
        {
            for (BasicBlock* block : m_compiler->Blocks())
            {
                if (dfsTree->Contains(block))
                {
                    continue;
                }

                VarSetOps::ClearD(m_compiler, block->bbLiveOut);
                if (keepAliveThis)
                {
                    unsigned thisVarIndex = m_compiler->lvaGetDesc(m_compiler->info.compThisArg)->lvVarIndex;
                    VarSetOps::AddElemD(m_compiler, block->bbLiveOut, thisVarIndex);
                }

                if (block->HasPotentialEHSuccs(m_compiler))
                {
                    block->VisitEHSuccs(m_compiler, [=](BasicBlock* succ) {
                        VarSetOps::UnionD(m_compiler, block->bbLiveOut, succ->bbLiveIn);
                        return BasicBlockVisit::Continue;
                    });
                }

                VarSetOps::Assign(m_compiler, block->bbLiveIn, block->bbLiveOut);
            }
        }
    }

public:
    static void Run(Compiler* compiler)
    {
        LiveVarAnalysis analysis(compiler);
        analysis.Run();
    }
};

/*****************************************************************************
 *
 *  This is the classic algorithm for Live Variable Analysis.
 *  If updateInternalOnly==true, only update BBF_INTERNAL blocks.
 */

void Compiler::fgLiveVarAnalysis()
{
    LiveVarAnalysis::Run(this);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nBB liveness after fgLiveVarAnalysis():\n\n");
        fgDispBBLiveness();
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeCall: compute the changes to local var liveness
//                              due to a GT_CALL node.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - Tracked locals that must be kept alive everywhere in the block
//    call          - The call node in question.
//
void Compiler::fgComputeLifeCall(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTreeCall* call)
{
    assert(call != nullptr);

    // If this is a tail-call via helper, and we have any unmanaged p/invoke calls in
    // the method, then we're going to run the p/invoke epilog
    // So we mark the FrameRoot as used by this instruction.
    // This ensure that this variable is kept alive at the tail-call
    if (call->IsTailCallViaJitHelper() && compMethodRequiresPInvokeFrame())
    {
        assert(!opts.ShouldUsePInvokeHelpers() || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers())
        {
            // Get the FrameListRoot local and make it live.

            LclVarDsc* frameVarDsc = lvaGetDesc(info.compLvFrameListRoot);

            if (frameVarDsc->lvTracked)
            {
                VarSetOps::AddElemD(this, life, frameVarDsc->lvVarIndex);
            }
        }
    }

    // TODO: we should generate the code for saving to/restoring
    //       from the inlined N/Direct frame instead.

    /* Is this call to unmanaged code? */
    if (call->IsUnmanaged() && compMethodRequiresPInvokeFrame())
    {
        // Get the FrameListRoot local and make it live.
        assert(!opts.ShouldUsePInvokeHelpers() || (info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!opts.ShouldUsePInvokeHelpers() && !call->IsSuppressGCTransition())
        {
            LclVarDsc* frameVarDsc = lvaGetDesc(info.compLvFrameListRoot);

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

    GenTreeLclVarCommon* definedLcl = gtCallGetDefinedRetBufLclAddr(call);
    if (definedLcl != nullptr)
    {
        fgComputeLifeLocal(life, keepAliveVars, definedLcl);
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
        printf("Ref V%02u,T%02u] at ", node->GetLclNum(), varIndex);
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
                printf("Def V%02u,T%02u at ", node->GetLclNum(), varIndex);
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
            return !varDsc.IsAddressExposed() &&
                   !(varDsc.lvIsStructField && lvaTable[varDsc.lvParentLcl].IsAddressExposed());
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
//
// Returns:
//    `true` if the node is a dead store (i.e. all fields are dead); `false` otherwise.
//
bool Compiler::fgComputeLifeUntrackedLocal(VARSET_TP&           life,
                                           VARSET_VALARG_TP     keepAliveVars,
                                           LclVarDsc&           varDsc,
                                           GenTreeLclVarCommon* lclVarNode)
{
    assert(lclVarNode != nullptr);

    bool isDef = ((lclVarNode->gtFlags & GTF_VAR_DEF) != 0);

    // We have accurate ref counts when running late liveness so we can eliminate
    // some stores if the lhs local has a ref count of 1.
    if (isDef && compRationalIRForm && (varDsc.lvRefCnt() == 1) && !varDsc.lvPinned)
    {
        if (varDsc.lvIsStructField)
        {
            if ((lvaGetDesc(varDsc.lvParentLcl)->lvRefCnt() == 1) &&
                (lvaGetParentPromotionType(&varDsc) == PROMOTION_TYPE_DEPENDENT))
            {
                return true;
            }
        }
        else if (varTypeIsPromotable(varDsc.lvType))
        {
            if (lvaGetPromotionType(&varDsc) != PROMOTION_TYPE_INDEPENDENT)
            {
                return true;
            }
        }
        else
        {
            return true;
        }
    }

    if (!varTypeIsPromotable(varDsc.TypeGet()) || (lvaGetPromotionType(&varDsc) == PROMOTION_TYPE_NONE))
    {
        return false;
    }

    assert(varDsc.lvFieldCnt <= 4);

    lclVarNode->gtFlags &= ~GTF_VAR_DEATH_MASK;
    bool anyFieldLive = false;
    for (unsigned i = varDsc.lvFieldLclStart; i < varDsc.lvFieldLclStart + varDsc.lvFieldCnt; ++i)
    {
        LclVarDsc* fieldVarDsc = lvaGetDesc(i);
#if !defined(TARGET_64BIT)
        if (!varTypeIsLong(fieldVarDsc->lvType) || !fieldVarDsc->lvPromoted)
#endif // !defined(TARGET_64BIT)
        {
            noway_assert(fieldVarDsc->lvIsStructField);
        }
        if (fieldVarDsc->lvTracked)
        {
            const unsigned varIndex  = fieldVarDsc->lvVarIndex;
            bool           fieldLive = VarSetOps::IsMember(this, life, varIndex);
            anyFieldLive |= fieldLive;

            if (!fieldLive)
            {
                lclVarNode->SetLastUse(i - varDsc.lvFieldLclStart);
            }

            if (isDef)
            {
                if (((lclVarNode->gtFlags & GTF_VAR_USEASG) == 0) &&
                    !VarSetOps::IsMember(this, keepAliveVars, varIndex))
                {
                    VarSetOps::RemoveElemD(this, life, varIndex);
                }
            }
            else
            {
                VarSetOps::AddElemD(this, life, varIndex);
            }
        }
        else
        {
            anyFieldLive = true;
        }
    }

    if (isDef && !anyFieldLive && !opts.MinOpts())
    {
        // Do not consider this store dead if the parent local variable is an address exposed local or
        // if the struct has any significant padding we must retain the value of.
        return !varDsc.IsAddressExposed();
    }

    return false;
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
//
bool Compiler::fgComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode)
{
    unsigned lclNum = lclVarNode->AsLclVarCommon()->GetLclNum();

    assert(lclNum < lvaCount);
    LclVarDsc& varDsc = lvaTable[lclNum];
    bool       isDef  = ((lclVarNode->gtFlags & GTF_VAR_DEF) != 0);

    // Is this a tracked variable?
    if (varDsc.lvTracked)
    {
        /* Is this a definition or use? */
        if (isDef)
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
        return fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon());
    }
    return false;
}

//------------------------------------------------------------------------
// Compiler::fgTryRemoveDeadStoreEarly:
//    Try to remove a dead store during early liveness.
//
// Arguments:
//    stmt - The statement containing the dead store.
//    dst  - The destination local of the dead store.
//
// Remarks:
//    We only handle the simple top level case since dead embedded stores are
//    extremely rare in early liveness.
//
// Returns:
//    The next node to compute liveness for (in a backwards traversal).
//
GenTree* Compiler::fgTryRemoveDeadStoreEarly(Statement* stmt, GenTreeLclVarCommon* cur)
{
    if (!stmt->GetRootNode()->OperIsLocalStore() || (stmt->GetRootNode() != cur))
    {
        return cur->gtPrev;
    }

    JITDUMP("Store [%06u] is dead", dspTreeID(stmt->GetRootNode()));
    // The def ought to be the last thing.
    assert(stmt->GetTreeListEnd() == cur);

    GenTree* sideEffects = nullptr;
    gtExtractSideEffList(stmt->GetRootNode()->AsLclVarCommon()->Data(), &sideEffects);

    if (sideEffects == nullptr)
    {
        JITDUMP(" and has no side effects, removing statement\n");
        fgRemoveStmt(compCurBB, stmt DEBUGARG(false));
        return nullptr;
    }
    else
    {
        JITDUMP(" but has side effects. Replacing with:\n\n");
        stmt->SetRootNode(sideEffects);
        fgSequenceLocals(stmt);
        DISPTREE(sideEffects);
        JITDUMP("\n");
        // continue at tail of the side effects
        return stmt->GetTreeListEnd();
    }
}

/*****************************************************************************
 *
 * Compute the set of live variables at each node in a given statement
 * or subtree of a statement moving backward from startNode to endNode
 */

void Compiler::fgComputeLife(VARSET_TP&           life,
                             GenTree*             startNode,
                             GenTree*             endNode,
                             VARSET_VALARG_TP     keepAliveVars,
                             bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    // Don't kill vars in scope
    noway_assert(VarSetOps::IsSubset(this, keepAliveVars, life));
    noway_assert(endNode || (startNode == compCurStmt->GetRootNode()));
    assert(!fgIsDoingEarlyLiveness);

    for (GenTree* tree = startNode; tree != endNode; tree = tree->gtPrev)
    {
    AGAIN:
        assert(tree->OperGet() != GT_QMARK);

        if (tree->IsCall())
        {
            fgComputeLifeCall(life, keepAliveVars, tree->AsCall());
        }
        else if (tree->OperIsNonPhiLocal())
        {
            bool isDeadStore = fgComputeLifeLocal(life, keepAliveVars, tree);
            if (isDeadStore)
            {
                LclVarDsc* varDsc       = lvaGetDesc(tree->AsLclVarCommon());
                bool       isUse        = (tree->gtFlags & GTF_VAR_USEASG) != 0;
                bool       doAgain      = false;
                bool       storeRemoved = false;

                if (fgRemoveDeadStore(&tree, varDsc, life, &doAgain, pStmtInfoDirty, &storeRemoved DEBUGARG(treeModf)))
                {
                    assert(!doAgain);
                    break;
                }

                if (isUse && !storeRemoved)
                {
                    // SSA and VN treat "partial definitions" as true uses, so for this
                    // front-end liveness pass we must add them to the live set in case
                    // we failed to remove the dead store.
                    if (varDsc->lvTracked)
                    {
                        VarSetOps::AddElemD(this, life, varDsc->lvVarIndex);
                    }
                    if (varDsc->lvPromoted)
                    {
                        for (unsigned fieldIndex = 0; fieldIndex < varDsc->lvFieldCnt; fieldIndex++)
                        {
                            LclVarDsc* fieldVarDsc = lvaGetDesc(varDsc->lvFieldLclStart + fieldIndex);
                            if (fieldVarDsc->lvTracked)
                            {
                                VarSetOps::AddElemD(this, life, fieldVarDsc->lvVarIndex);
                            }
                        }
                    }
                }

                if (doAgain)
                {
                    goto AGAIN;
                }
            }
        }
    }
}

//---------------------------------------------------------------------
// fgComputeLifeLIR - fill out liveness flags in the IR nodes of the block
// provided the live-out set.
//
// Arguments
//    life          - the set of live-out variables from the block
//    block         - the block
//    keepAliveVars - variables that are globally live (usually due to being live into an EH successor)
//
void Compiler::fgComputeLifeLIR(VARSET_TP& life, BasicBlock* block, VARSET_VALARG_TP keepAliveVars)
{
    noway_assert(VarSetOps::IsSubset(this, keepAliveVars, life));

    LIR::Range& blockRange = LIR::AsRange(block);
    GenTree*    firstNode  = blockRange.FirstNode();
    if (firstNode == nullptr)
    {
        return;
    }
    for (GenTree *node = blockRange.LastNode(), *next = nullptr, *end = firstNode->gtPrev; node != end; node = next)
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

                        // Special-case PUTARG_STK: since this operator is not considered a value, DCE will not
                        // remove these nodes.
                        if (operand->OperIs(GT_PUTARG_STK))
                        {
                            operand->AsPutArgStk()->gtOp1->SetUnusedValue();
                            operand->gtBashToNOP();
                        }

                        return GenTree::VisitResult::Continue;
                    });

                    blockRange.Remove(node);

                    // Removing a call does not affect liveness unless it is a tail call in a method with P/Invokes or
                    // is itself a P/Invoke, in which case it may affect the liveness of the frame root variable.
                    if (!opts.MinOpts() && !opts.ShouldUsePInvokeHelpers() &&
                        ((call->IsTailCall() && compMethodRequiresPInvokeFrame()) ||
                         (call->IsUnmanaged() && !call->IsSuppressGCTransition())) &&
                        lvaTable[info.compLvFrameListRoot].lvTracked)
                    {
                        fgStmtRemoved = true;
                    }
                }
                else
                {
                    fgComputeLifeCall(life, keepAliveVars, call);
                }
                break;
            }

            case GT_LCL_VAR:
            case GT_LCL_FLD:
            {
                GenTreeLclVarCommon* const lclVarNode = node->AsLclVarCommon();
                LclVarDsc&                 varDsc     = lvaTable[lclVarNode->GetLclNum()];

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

            case GT_LCL_ADDR:
                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead LclVar address:\n");
                    DISPNODE(node);

                    const bool isTracked = lvaTable[node->AsLclVarCommon()->GetLclNum()].lvTracked;
                    blockRange.Delete(this, block, node);
                    if (isTracked && !opts.MinOpts())
                    {
                        fgStmtRemoved = true;
                    }
                }
                else
                {
                    // For LCL_ADDRs that are defined by being passed as a
                    // retbuf we will handle them when we get to the call. We
                    // cannot consider them to be defined at the point of the
                    // LCL_ADDR since there may be uses between the LCL_ADDR
                    // and call.
                    if (fgIsHiddenBufferAddressDef(blockRange, node))
                    {
                        break;
                    }

                    isDeadStore = fgComputeLifeLocal(life, keepAliveVars, node);
                    if (isDeadStore)
                    {
                        LIR::Use addrUse;
                        if (blockRange.TryGetUse(node, &addrUse) && addrUse.User()->OperIs(GT_STOREIND, GT_STORE_BLK))
                        {
                            GenTreeIndir* const store = addrUse.User()->AsIndir();

                            if (fgTryRemoveDeadStoreLIR(store, node->AsLclVarCommon(), block))
                            {
                                JITDUMP("Removing dead LclVar address:\n");
                                DISPNODE(node);
                                blockRange.Remove(node);

                                GenTree* data = store->AsIndir()->Data();
                                data->SetUnusedValue();

                                if (data->isIndir())
                                {
                                    Lowering::TransformUnusedIndirection(data->AsIndir(), this, block);
                                }
                            }
                        }
                    }
                }
                break;

            case GT_STORE_LCL_VAR:
            case GT_STORE_LCL_FLD:
            {
                GenTreeLclVarCommon* const lclVarNode = node->AsLclVarCommon();

                LclVarDsc& varDsc = lvaTable[lclVarNode->GetLclNum()];
                if (varDsc.lvTracked)
                {
                    isDeadStore = fgComputeLifeTrackedLocalDef(life, keepAliveVars, varDsc, lclVarNode);
                }
                else
                {
                    isDeadStore = fgComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode);
                }

                if (isDeadStore && fgTryRemoveDeadStoreLIR(node, lclVarNode, block))
                {
                    GenTree* value = lclVarNode->Data();
                    value->SetUnusedValue();

                    if (value->isIndir())
                    {
                        Lowering::TransformUnusedIndirection(value->AsIndir(), this, block);
                    }
                }
                break;
            }

            case GT_LABEL:
            case GT_FTN_ADDR:
            case GT_CNS_INT:
            case GT_CNS_LNG:
            case GT_CNS_DBL:
            case GT_CNS_STR:
            case GT_CNS_VEC:
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
            case GT_XORR:
            case GT_XAND:
            case GT_XADD:
            case GT_XCHG:
            case GT_CMPXCHG:
            case GT_MEMORYBARRIER:
            case GT_JMP:
            case GT_STOREIND:
            case GT_BOUNDS_CHECK:
            case GT_STORE_BLK:
            case GT_JCMP:
            case GT_JTEST:
            case GT_JCC:
            case GT_JTRUE:
            case GT_RETURN:
            case GT_SWITCH:
            case GT_RETFILT:
            case GT_START_NONGC:
            case GT_START_PREEMPTGC:
            case GT_PROF_HOOK:
#if defined(FEATURE_EH_WINDOWS_X86)
            case GT_END_LFIN:
#endif // FEATURE_EH_WINDOWS_X86
            case GT_SWITCH_TABLE:
            case GT_PINVOKE_PROLOG:
            case GT_PINVOKE_EPILOG:
            case GT_RETURNTRAP:
            case GT_PUTARG_STK:
            case GT_IL_OFFSET:
            case GT_KEEPALIVE:
            case GT_SWIFT_ERROR_RET:
                // Never remove these nodes, as they are always side-effecting.
                //
                // NOTE: the only side-effect of some of these nodes (GT_CMP, GT_SUB_HI) is a write to the flags
                // register.
                // Properly modeling this would allow these nodes to be removed.
                break;

#ifdef FEATURE_HW_INTRINSICS
            case GT_HWINTRINSIC:
            {
                GenTreeHWIntrinsic* hwintrinsic = node->AsHWIntrinsic();
                NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

                if (hwintrinsic->OperIsMemoryStore())
                {
                    // Never remove these nodes, as they are always side-effecting.
                    break;
                }
                else if (HWIntrinsicInfo::HasSpecialSideEffect(intrinsicId))
                {
                    // Never remove these nodes, as they are always side-effecting
                    // or have a behavioral semantic that is undesirable to remove
                    break;
                }

                fgTryRemoveNonLocal(node, &blockRange);
                break;
            }
#endif // FEATURE_HW_INTRINSICS

            case GT_NO_OP:
                // This is a non-removable NOP
                break;

            case GT_NOP:
            {
                // NOTE: we need to keep some NOPs around because they are referenced by calls. See the dead store
                // removal code above (case GT_STORE_LCL_VAR) for more explanation.
                if ((node->gtFlags & GTF_ORDER_SIDEEFF) != 0)
                {
                    break;
                }
                fgTryRemoveNonLocal(node, &blockRange);
            }
            break;

            case GT_BLK:
            {
                bool removed = fgTryRemoveNonLocal(node, &blockRange);
                if (!removed && node->IsUnusedValue())
                {
                    // IR doesn't expect dummy uses of `GT_BLK`.
                    JITDUMP("Transform an unused BLK node [%06d]\n", dspTreeID(node));
                    Lowering::TransformUnusedIndirection(node->AsIndir(), this, block);
                }
            }
            break;

            default:
                fgTryRemoveNonLocal(node, &blockRange);
                break;
        }
    }
}

//---------------------------------------------------------------------
// fgIsHiddenBufferAddressDef - given a LCL_ADDR node, check if it is the
// return buffer definition of a call.
//
// Arguments
//    range - the block range containing the LCL_ADDR
//    node  - the LCL_ADDR
//
bool Compiler::fgIsHiddenBufferAddressDef(LIR::Range& range, GenTree* node)
{
    assert(node->OperIs(GT_LCL_ADDR));
    if ((node->gtFlags & GTF_VAR_DEF) == 0)
    {
        return false;
    }

    LclVarDsc* dsc = lvaGetDesc(node->AsLclVarCommon());
    if (!dsc->IsHiddenBufferStructArg())
    {
        return false;
    }

    if (!dsc->lvTracked)
    {
        return false;
    }

    LIR::Use use;
    do
    {
        if (!range.TryGetUse(node, &use))
        {
            return false;
        }

        if (use.User()->IsCall())
        {
            return gtCallGetDefinedRetBufLclAddr(use.User()->AsCall()) == node;
        }
    } while (use.User()->OperIs(GT_FIELD_LIST) || use.User()->OperIsPutArg());

    return false;
}

//---------------------------------------------------------------------
// fgTryRemoveNonLocal - try to remove a node if it is unused and has no direct
//   side effects.
//
// Arguments
//    node       - the non-local node to try;
//    blockRange - the block range that contains the node.
//
// Return value:
//    None
//
// Notes: local nodes are processed independently and are not expected in this function.
//
bool Compiler::fgTryRemoveNonLocal(GenTree* node, LIR::Range* blockRange)
{
    assert(!node->OperIsLocal());
    if (!node->IsValue() || node->IsUnusedValue())
    {
        // We are only interested in avoiding the removal of nodes with direct side effects
        // (as opposed to side effects of their children).
        // This default case should never include calls or stores.
        assert(!node->OperRequiresAsgFlag() && !node->OperIs(GT_CALL));
        if (!node->gtSetFlags() && !node->OperMayThrow(this))
        {
            JITDUMP("Removing dead node:\n");
            DISPNODE(node);

            node->VisitOperands([](GenTree* operand) -> GenTree::VisitResult {
                operand->SetUnusedValue();
                return GenTree::VisitResult::Continue;
            });

            if (node->OperConsumesFlags() && node->gtPrev->gtSetFlags())
            {
                node->gtPrev->gtFlags &= ~GTF_SET_FLAGS;
            }

            blockRange->Remove(node);
            return true;
        }
    }
    return false;
}

//---------------------------------------------------------------------
// fgTryRemoveDeadStoreLIR - try to remove a dead store from LIR
//
// Arguments:
//   store   - A store tree
//   lclNode - The node representing the local being stored to
//   block   - Block that the store is part of
//
// Return Value:
//    Whether the store was successfully removed from "block"'s range.
//
bool Compiler::fgTryRemoveDeadStoreLIR(GenTree* store, GenTreeLclVarCommon* lclNode, BasicBlock* block)
{
    assert(!opts.MinOpts());

    // We cannot remove stores to (tracked) TYP_STRUCT locals with GC pointers marked as "explicit init",
    // as said locals will be reported to the GC untracked, and deleting the explicit initializer risks
    // exposing uninitialized references.
    if ((lclNode->gtFlags & GTF_VAR_USEASG) == 0)
    {
        LclVarDsc* varDsc = lvaGetDesc(lclNode);
        if (varDsc->lvHasExplicitInit && (varDsc->TypeGet() == TYP_STRUCT) && varDsc->HasGCPtr() &&
            (varDsc->lvRefCnt() > 1))
        {
            JITDUMP("Not removing a potential explicit init [%06u] of V%02u\n", dspTreeID(store), lclNode->GetLclNum());
            return false;
        }
    }

    JITDUMP("Removing dead %s:\n", store->OperIsIndir() ? "indirect store" : "local store");
    DISPNODE(store);

    LIR::AsRange(block).Remove(store);
    fgStmtRemoved = true;

    return true;
}

//---------------------------------------------------------------------
// fgRemoveDeadStore - remove a store to a local which has no exposed uses.
//
//   pTree          - GenTree** to local, including store-form local or local addr (post-rationalize)
//   varDsc         - var that is being stored to
//   life           - current live tracked vars (maintained as we walk backwards)
//   doAgain        - out parameter, true if we should restart the statement
//   pStmtInfoDirty - should defer the cost computation to the point after the reverse walk is completed?
//   pStoreRemoved  - whether the assignment part of the store was removed
//
// Return Value:
//   true if we should skip the rest of the statement, false if we should continue
//
bool Compiler::fgRemoveDeadStore(GenTree**           pTree,
                                 LclVarDsc*          varDsc,
                                 VARSET_VALARG_TP    life,
                                 bool*               doAgain,
                                 bool*               pStmtInfoDirty,
                                 bool* pStoreRemoved DEBUGARG(bool* treeModf))
{
    assert(!compRationalIRForm);

    // Vars should have already been checked for address exposure by this point.
    assert(!varDsc->IsAddressExposed());

    GenTree* const tree = *pTree;

    // We can have two types of stores: STORE_LCL_VAR/STORE_LCL_FLD, ...) or a call,
    // in which case we bail (we most likely cannot remove the call anyway).
    if (!tree->OperIsLocalStore())
    {
        *pStoreRemoved = false;
        return false;
    }

    // We are now committed to removing the store.
    *pStoreRemoved = true;

    GenTreeLclVarCommon* store = tree->AsLclVarCommon();
    GenTree*             value = store->Data();

    // Check for side effects.
    GenTree* sideEffList = nullptr;
    if ((value->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf(FMT_BB " - Dead store has side effects...\n", compCurBB->bbNum);
            gtDispTree(store);
            printf("\n");
        }
#endif // DEBUG

        gtExtractSideEffList(value, &sideEffList);
    }

    // Test for interior statement
    if (store->gtNext == nullptr)
    {
        // This is a "NORMAL" statement with the store node hanging from the statement.

        noway_assert(compCurStmt->GetRootNode() == store);
        JITDUMP("top level store\n");

        if (sideEffList != nullptr)
        {
            noway_assert((sideEffList->gtFlags & GTF_SIDE_EFFECT) != 0);
#ifdef DEBUG
            if (verbose)
            {
                printf("Extracted side effects list...\n");
                gtDispTree(sideEffList);
                printf("\n");
            }
#endif // DEBUG

            // Replace the store statement with the list of side effects
            *pTree = sideEffList;
            compCurStmt->SetRootNode(sideEffList);
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

            // No side effects - remove the whole statement from the block->bbStmtList.
            fgRemoveStmt(compCurBB, compCurStmt);

            // Since we removed it do not process the rest (i.e. "data") of the statement
            // variables in "data" will not be marked as live, so we get the benefit of
            // propagating dead variables up the chain
            return true;
        }
    }
    else
    {
        // This is an INTERIOR STATEMENT with a dead store - remove it.
        // TODO-Cleanup: I'm not sure this assert is valuable; we've already determined this when
        // we computed that it was dead.
        if (varDsc->lvTracked)
        {
            noway_assert(!VarSetOps::IsMember(this, life, varDsc->lvVarIndex));
        }
        else
        {
            for (unsigned i = 0; i < varDsc->lvFieldCnt; ++i)
            {
                unsigned fieldVarNum = varDsc->lvFieldLclStart + i;
                {
                    LclVarDsc* fieldVarDsc = lvaGetDesc(fieldVarNum);
                    noway_assert(fieldVarDsc->lvTracked && !VarSetOps::IsMember(this, life, fieldVarDsc->lvVarIndex));
                }
            }
        }

        if (sideEffList != nullptr)
        {
            noway_assert((sideEffList->gtFlags & GTF_SIDE_EFFECT) != 0);
#ifdef DEBUG
            if (verbose)
            {
                printf("Extracted side effects list from condition...\n");
                gtDispTree(sideEffList);
                printf("\n");
            }
#endif // DEBUG

#ifdef DEBUG
            *treeModf = true;
#endif // DEBUG

            // Change the node to a GT_COMMA holding the side effect list.
            store->ChangeType(TYP_VOID);
            store->ChangeOper(GT_COMMA);
            store->SetAllEffectsFlags(sideEffList);

            if (sideEffList->OperIs(GT_COMMA))
            {
                store->AsOp()->gtOp1 = sideEffList->AsOp()->gtOp1;
                store->AsOp()->gtOp2 = sideEffList->AsOp()->gtOp2;
            }
            else
            {
                store->AsOp()->gtOp1 = sideEffList;
                store->AsOp()->gtOp2 = gtNewNothingNode();
            }
        }
        else
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("\nRemoving tree ");
                printTreeID(store);
                printf(" in " FMT_BB " as useless\n", compCurBB->bbNum);
                gtDispTree(store);
                printf("\n");
            }
#endif // DEBUG
       // No side effects - Change the store to a GT_NOP node
            store->gtBashToNOP();

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
        *pTree = store;

        return false;
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
    // Variables involved in exception-handlers and finally blocks need
    // to be specially marked
    //

    VARSET_TP exceptVars(VarSetOps::MakeEmpty(this));  // vars live on entry to a handler
    VARSET_TP finallyVars(VarSetOps::MakeEmpty(this)); // vars live on exit of a 'finally' block

    for (BasicBlock* block : Blocks())
    {
        if (block->hasEHBoundaryIn())
        {
            // Note the set of variables live on entry to exception handler.
            VarSetOps::UnionD(this, exceptVars, block->bbLiveIn);
        }

        if (block->hasEHBoundaryOut())
        {
            // Get the set of live variables on exit from an exception region.
            VarSetOps::UnionD(this, exceptVars, block->bbLiveOut);
            if (block->KindIs(BBJ_EHFINALLYRET))
            {
                // Live on exit from finally.
                // We track these separately because, in addition to having EH live-out semantics,
                // they are must-init.
                VarSetOps::UnionD(this, finallyVars, block->bbLiveOut);
            }
        }
    }

    if (!fgIsDoingEarlyLiveness)
    {
        LclVarDsc* varDsc;
        unsigned   varNum;

        for (varNum = 0, varDsc = lvaTable; varNum < lvaCount; varNum++, varDsc++)
        {
            // Ignore the variable if it's not tracked

            if (!varDsc->lvTracked)
            {
                continue;
            }

            // Fields of dependently promoted structs may be tracked. We shouldn't set lvMustInit on them since
            // the whole parent struct will be initialized; however, lvLiveInOutOfHndlr should be set on them
            // as appropriate.

            bool fieldOfDependentlyPromotedStruct = lvaIsFieldOfDependentlyPromotedStruct(varDsc);

            // Un-init locals may need auto-initialization. Note that the
            // liveness of such locals will bubble to the top (fgFirstBB)
            // in fgInterBlockLocalVarLiveness()

            if (!varDsc->lvIsParam && VarSetOps::IsMember(this, fgFirstBB->bbLiveIn, varDsc->lvVarIndex) &&
                (info.compInitMem || varTypeIsGC(varDsc->TypeGet())) && !fieldOfDependentlyPromotedStruct)
            {
                varDsc->lvMustInit = true;
            }

            // Mark all variables that are live on entry to an exception handler
            // or on exit from a filter handler or finally.

            bool isFinallyVar = VarSetOps::IsMember(this, finallyVars, varDsc->lvVarIndex);
            if (isFinallyVar || VarSetOps::IsMember(this, exceptVars, varDsc->lvVarIndex))
            {
                // Mark the variable appropriately.
                lvaSetVarLiveInOutOfHandler(varNum);

                // Mark all pointer variables live on exit from a 'finally' block as
                // 'explicitly initialized' (must-init) for GC-ref types.

                if (isFinallyVar)
                {
                    // Set lvMustInit only if we have a non-arg, GC pointer.
                    if (!varDsc->lvIsParam && varTypeIsGC(varDsc->TypeGet()))
                    {
                        varDsc->lvMustInit = true;
                    }
                }
            }
        }
    }

    /*-------------------------------------------------------------------------
     * Now fill in liveness info within each basic block - Backward DataFlow
     */

    VARSET_TP keepAliveVars(VarSetOps::MakeEmpty(this));

    for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_dfsTree->GetPostOrder(i - 1);
        /* Tell everyone what block we're working on */

        compCurBB = block;

        /* Remember those vars live on entry to exception handlers */
        /* if we are part of a try block */
        VarSetOps::ClearD(this, keepAliveVars);

        if (block->HasPotentialEHSuccs(this))
        {
            MemoryKindSet memoryLiveness = 0;
            fgAddHandlerLiveVars(block, keepAliveVars, memoryLiveness);

            // keepAliveVars is a subset of exceptVars
            noway_assert(VarSetOps::IsSubset(this, keepAliveVars, exceptVars));
        }

        /* Start with the variables live on exit from the block */

        VARSET_TP life(VarSetOps::MakeCopy(this, block->bbLiveOut));

        /* Mark any interference we might have at the end of the block */

        if (block->IsLIR())
        {
            fgComputeLifeLIR(life, block, keepAliveVars);
        }
        else if (fgNodeThreading == NodeThreading::AllTrees)
        {
            /* Get the first statement in the block */

            Statement* firstStmt = block->FirstNonPhiDef();

            if (firstStmt == nullptr)
            {
                continue;
            }

            /* Walk all the statements of the block backwards - Get the LAST stmt */

            Statement* nextStmt = block->lastStmt();

            do
            {
#ifdef DEBUG
                bool treeModf = false;
#endif // DEBUG
                noway_assert(nextStmt != nullptr);

                compCurStmt = nextStmt;
                nextStmt    = nextStmt->GetPrevStmt();

                /* Compute the liveness for each tree node in the statement */
                bool stmtInfoDirty = false;

                fgComputeLife(life, compCurStmt->GetRootNode(), nullptr, keepAliveVars,
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
                    gtDispTree(compCurStmt->GetRootNode());
                    printf("\n");
                }
#endif // DEBUG
            } while (compCurStmt != firstStmt);
        }
        else
        {
            assert(fgIsDoingEarlyLiveness && (fgNodeThreading == NodeThreading::AllLocals));
            compCurStmt = nullptr;

            Statement* firstStmt = block->firstStmt();

            if (firstStmt == nullptr)
            {
                continue;
            }

            Statement* stmt = block->lastStmt();

            while (true)
            {
                Statement* prevStmt = stmt->GetPrevStmt();

                GenTree* dst   = nullptr;
                GenTree* qmark = nullptr;
                if (compQmarkUsed)
                {
                    qmark = fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
                }

                if (qmark != nullptr)
                {
                    for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr;)
                    {
                        assert(cur->OperIsAnyLocal());
                        bool isDef = ((cur->gtFlags & GTF_VAR_DEF) != 0) && ((cur->gtFlags & GTF_VAR_USEASG) == 0);
                        bool conditional = cur != dst;
                        // Ignore conditional defs that would otherwise
                        // (incorrectly) interfere with liveness in other
                        // branches of the qmark.
                        if (isDef && conditional)
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        if (!fgComputeLifeLocal(life, keepAliveVars, cur))
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        assert(cur == dst);
                        cur = fgTryRemoveDeadStoreEarly(stmt, cur->AsLclVarCommon());
                    }
                }
                else
                {
                    for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr;)
                    {
                        assert(cur->OperIsAnyLocal());
                        if (!fgComputeLifeLocal(life, keepAliveVars, cur))
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        cur = fgTryRemoveDeadStoreEarly(stmt, cur->AsLclVarCommon());
                    }
                }

                if (stmt == firstStmt)
                {
                    break;
                }

                stmt = prevStmt;
            }
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
    for (BasicBlock* const block : Blocks())
    {
        fgDispBBLiveness(block);
    }
}

#endif // DEBUG

//------------------------------------------------------------------------
// fgEarlyLiveness: Run the early liveness pass.
//
// Return Value:
//     Returns MODIFIED_EVERYTHING when liveness was computed and DCE was run.
//
PhaseStatus Compiler::fgEarlyLiveness()
{
    if (!opts.OptimizationEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    static ConfigMethodRange JitEnableEarlyLivenessRange;
    JitEnableEarlyLivenessRange.EnsureInit(JitConfig.JitEnableEarlyLivenessRange());
    const unsigned hash = info.compMethodHash();
    if (!JitEnableEarlyLivenessRange.Contains(hash))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    fgIsDoingEarlyLiveness = true;
    lvaSortByRefCount();

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

    fgIsDoingEarlyLiveness = false;
    fgDidEarlyLiveness     = true;
    return PhaseStatus::MODIFIED_EVERYTHING;
}
