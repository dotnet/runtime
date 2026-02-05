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

#include "jitstd/algorithm.h"

template <typename TLiveness>
class Liveness
{
    Compiler* m_compiler;

    VARSET_TP     m_curUseSet;                         // vars used     by block (before a def)
    VARSET_TP     m_curDefSet;                         // vars assigned by block (before a use)
    MemoryKindSet m_curMemoryUse = emptyMemoryKindSet; // True iff the current basic block uses memory.
    MemoryKindSet m_curMemoryDef = emptyMemoryKindSet; // True iff the current basic block modifies memory.
    MemoryKindSet m_curMemoryHavoc =
        emptyMemoryKindSet; // True if  the current basic block is known to set memory to a "havoc" value.

    VARSET_TP     m_liveIn;
    VARSET_TP     m_liveOut;
    VARSET_TP     m_ehHandlerLiveVars;
    MemoryKindSet m_memoryLiveIn  = emptyMemoryKindSet;
    MemoryKindSet m_memoryLiveOut = emptyMemoryKindSet;

    bool m_livenessChanged = false;

protected:
    enum
    {
        // Whether the liveness computed is for SSA and should follow
        // same modelling rules as SSA. SSA models partial defs like (v.x = 123) as
        // (v = v with x = 123), which also implies that these partial definitions
        // become uses. For dead-code elimination this is more conservative than
        // needed, so outside SSA we do not model partial defs in this way:
        //
        // * In SSA: Partial defs are full defs but are also uses. They impact both
        // bbVarUse and bbVarDef.
        //
        // * Outside SSA: Partial defs are _not_ full defs and are also not
        // considered uses. They do not get included in bbVarUse/bbVarDef.
        SsaLiveness           = false,
        ComputeMemoryLiveness = false,
        IsLIR                 = false,
        IsEarly               = false,
    };

    Liveness(Compiler* compiler)
        : m_compiler(compiler)
        , m_curUseSet(VarSetOps::UninitVal())
        , m_curDefSet(VarSetOps::UninitVal())
        , m_liveIn(VarSetOps::UninitVal())
        , m_liveOut(VarSetOps::UninitVal())
        , m_ehHandlerLiveVars(VarSetOps::UninitVal())
    {
    }

private:
    void Init();
    void SelectTrackedLocals();

    void PerBlockLocalVarLiveness();
    void PerNodeLocalVarLiveness(GenTree* tree);
#ifdef FEATURE_HW_INTRINSICS
    void PerNodeLocalVarLiveness(GenTreeHWIntrinsic* hwintrinsic);
#endif
    void MarkUseDef(GenTreeLclVarCommon* tree);

    void                 InterBlockLocalVarLiveness();
    void                 DoLiveVarAnalysis();
    bool                 PerBlockAnalysis(BasicBlock* block, bool keepAliveThis);
    void                 ComputeLife(VARSET_TP& tp,
                                     GenTree*   startNode,
                                     GenTree*   endNode,
                                     VARSET_VALARG_TP,
                                     bool* pStmtInfoDirty DEBUGARG(bool* treeModf));
    GenTreeLclVarCommon* ComputeLifeCall(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTreeCall* call);
    bool                 ComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode);
    void                 ComputeLifeTrackedLocalUse(VARSET_TP& life, LclVarDsc& varDsc, GenTreeLclVarCommon* node);
    bool                 ComputeLifeTrackedLocalDef(VARSET_TP&           life,
                                                    VARSET_VALARG_TP     keepAliveVars,
                                                    LclVarDsc&           varDsc,
                                                    GenTreeLclVarCommon* node);
    bool                 ComputeLifeUntrackedLocal(VARSET_TP&           life,
                                                   VARSET_VALARG_TP     keepAliveVars,
                                                   LclVarDsc&           varDsc,
                                                   GenTreeLclVarCommon* lclVarNode);
    bool                 RemoveDeadStore(GenTree**           pTree,
                                         LclVarDsc*          varDsc,
                                         VARSET_VALARG_TP    life,
                                         bool*               doAgain,
                                         bool*               pStmtInfoDirty,
                                         bool* pStoreRemoved DEBUGARG(bool* treeModf));

    void ComputeLifeLIR(VARSET_TP& life, BasicBlock* block, VARSET_VALARG_TP keepAliveVars);
    bool IsTrackedRetBufferAddress(LIR::Range& range, GenTree* node);
    bool TryRemoveDeadStoreLIR(GenTree* store, GenTreeLclVarCommon* lclNode, BasicBlock* block);
    bool TryRemoveNonLocalLIR(GenTree* node, LIR::Range* blockRange);
    bool CanUncontainOrRemoveOperands(GenTree* node);

    GenTree* TryRemoveDeadStoreEarly(Statement* stmt, GenTreeLclVarCommon* cur);

public:
    void Run();
};

template <typename TLiveness>
void Liveness<TLiveness>::Run()
{
#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("*************** In Liveness::Run()\n");

        if (TLiveness::IsLIR)
        {
            m_compiler->lvaTableDump();
        }
    }
#endif // DEBUG

    // Init liveness data structures.
    Init();

    m_compiler->EndPhase(PHASE_LCLVARLIVENESS_INIT);

    do
    {
        // Figure out use/def info for all basic blocks
        PerBlockLocalVarLiveness();
        m_compiler->EndPhase(PHASE_LCLVARLIVENESS_PERBLOCK);

        // Live variable analysis.
        InterBlockLocalVarLiveness();
    } while (m_compiler->fgStmtRemoved && m_livenessChanged);

    m_compiler->EndPhase(PHASE_LCLVARLIVENESS_INTERBLOCK);
}

template <typename TLiveness>
void Liveness<TLiveness>::Init()
{
    JITDUMP("In Liveness::Init\n");

    SelectTrackedLocals();

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
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; ++lclNum)
    {
        m_compiler->lvaTable[lclNum].lvMustInit = false;
    }

    for (BasicBlock* const block : m_compiler->Blocks())
    {
        block->InitVarSets(m_compiler);
    }

    m_compiler->fgBBVarSetsInited = true;
}

// LclVarDsc "less" comparer used to compare the weight of two locals, when optimizing for small code.
class LclVarDsc_SmallCode_Less
{
    const LclVarDsc* m_lvaTable;
    RefCountState    m_rcs;
    INDEBUG(unsigned m_lvaCount;)

public:
    LclVarDsc_SmallCode_Less(const LclVarDsc* lvaTable, RefCountState rcs DEBUGARG(unsigned lvaCount))
        : m_lvaTable(lvaTable)
        , m_rcs(rcs)
#ifdef DEBUG
        , m_lvaCount(lvaCount)
#endif
    {
    }

    bool operator()(unsigned n1, unsigned n2)
    {
        assert(n1 < m_lvaCount);
        assert(n2 < m_lvaCount);

        const LclVarDsc* dsc1 = &m_lvaTable[n1];
        const LclVarDsc* dsc2 = &m_lvaTable[n2];

        // We should not be sorting untracked variables
        assert(dsc1->lvTracked);
        assert(dsc2->lvTracked);
        // We should not be sorting after registers have been allocated
        assert(!dsc1->lvRegister);
        assert(!dsc2->lvRegister);

        unsigned weight1 = dsc1->lvRefCnt(m_rcs);
        unsigned weight2 = dsc2->lvRefCnt(m_rcs);

#ifndef TARGET_ARM
        // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
        // legacy backend. It should be enabled and verified.

        // Force integer candidates to sort above float candidates.
        const bool isFloat1 = isFloatRegType(dsc1->lvType);
        const bool isFloat2 = isFloatRegType(dsc2->lvType);

        if (isFloat1 != isFloat2)
        {
            if ((weight2 != 0) && isFloat1)
            {
                return false;
            }

            if ((weight1 != 0) && isFloat2)
            {
                return true;
            }
        }
#endif

        if (weight1 != weight2)
        {
            return weight1 > weight2;
        }

        // If the weighted ref counts are different then use their difference.
        if (dsc1->lvRefCntWtd() != dsc2->lvRefCntWtd())
        {
            return dsc1->lvRefCntWtd() > dsc2->lvRefCntWtd();
        }

        // We have equal ref counts and weighted ref counts.
        // Break the tie by:
        //   - Increasing the weight by 2   if we are a register arg.
        //   - Increasing the weight by 0.5 if we are a GC type.
        //
        // Review: seems odd that this is mixing counts and weights.

        if (weight1 != 0)
        {
            if (dsc1->lvIsRegArg)
            {
                weight1 += 2 * BB_UNITY_WEIGHT_UNSIGNED;
            }

            if (varTypeIsGC(dsc1->TypeGet()))
            {
                weight1 += BB_UNITY_WEIGHT_UNSIGNED / 2;
            }
        }

        if (weight2 != 0)
        {
            if (dsc2->lvIsRegArg)
            {
                weight2 += 2 * BB_UNITY_WEIGHT_UNSIGNED;
            }

            if (varTypeIsGC(dsc2->TypeGet()))
            {
                weight2 += BB_UNITY_WEIGHT_UNSIGNED / 2;
            }
        }

        if (weight1 != weight2)
        {
            return weight1 > weight2;
        }

        // To achieve a stable sort we use the LclNum (by way of the pointer address).
        return dsc1 < dsc2;
    }
};

// LclVarDsc "less" comparer used to compare the weight of two locals, when optimizing for blended code.
class LclVarDsc_BlendedCode_Less
{
    const LclVarDsc* m_lvaTable;
    RefCountState    m_rcs;
    INDEBUG(unsigned m_lvaCount;)

public:
    LclVarDsc_BlendedCode_Less(const LclVarDsc* lvaTable, RefCountState rcs DEBUGARG(unsigned lvaCount))
        : m_lvaTable(lvaTable)
        , m_rcs(rcs)
#ifdef DEBUG
        , m_lvaCount(lvaCount)
#endif
    {
    }

    bool operator()(unsigned n1, unsigned n2)
    {
        assert(n1 < m_lvaCount);
        assert(n2 < m_lvaCount);

        const LclVarDsc* dsc1 = &m_lvaTable[n1];
        const LclVarDsc* dsc2 = &m_lvaTable[n2];

        // We should not be sorting untracked variables
        assert(dsc1->lvTracked);
        assert(dsc2->lvTracked);
        // We should not be sorting after registers have been allocated
        assert(!dsc1->lvRegister);
        assert(!dsc2->lvRegister);

        weight_t weight1 = dsc1->lvRefCntWtd(m_rcs);
        weight_t weight2 = dsc2->lvRefCntWtd(m_rcs);

#ifndef TARGET_ARM
        // ARM-TODO: this was disabled for ARM under !FEATURE_FP_REGALLOC; it was probably a left-over from
        // legacy backend. It should be enabled and verified.

        // Force integer candidates to sort above float candidates.
        const bool isFloat1 = isFloatRegType(dsc1->lvType);
        const bool isFloat2 = isFloatRegType(dsc2->lvType);

        if (isFloat1 != isFloat2)
        {
            if (!Compiler::fgProfileWeightsEqual(weight2, 0) && isFloat1)
            {
                return false;
            }

            if (!Compiler::fgProfileWeightsEqual(weight1, 0) && isFloat2)
            {
                return true;
            }
        }
#endif

        if (!Compiler::fgProfileWeightsEqual(weight1, 0) && dsc1->lvIsRegArg)
        {
            weight1 += 2 * BB_UNITY_WEIGHT;
        }

        if (!Compiler::fgProfileWeightsEqual(weight2, 0) && dsc2->lvIsRegArg)
        {
            weight2 += 2 * BB_UNITY_WEIGHT;
        }

        if (!Compiler::fgProfileWeightsEqual(weight1, weight2))
        {
            return weight1 > weight2;
        }

        // If the weighted ref counts are different then try the unweighted ref counts.
        if (dsc1->lvRefCnt(m_rcs) != dsc2->lvRefCnt(m_rcs))
        {
            return dsc1->lvRefCnt(m_rcs) > dsc2->lvRefCnt(m_rcs);
        }

        // If one is a GC type and the other is not the GC type wins.
        if (varTypeIsGC(dsc1->TypeGet()) != varTypeIsGC(dsc2->TypeGet()))
        {
            return varTypeIsGC(dsc1->TypeGet());
        }

        // To achieve a stable sort we use the LclNum (by way of the pointer address).
        return dsc1 < dsc2;
    }
};

/*****************************************************************************
 *
 *  Sort the local variable table by refcount and assign tracking indices.
 */
template <typename TLiveness>
void Liveness<TLiveness>::SelectTrackedLocals()
{
    m_compiler->lvaTrackedCount             = 0;
    m_compiler->lvaTrackedCountInSizeTUnits = 0;

#ifdef DEBUG
    VarSetOps::AssignNoCopy(m_compiler, m_compiler->lvaTrackedVars, VarSetOps::MakeEmpty(m_compiler));
#endif

    if (m_compiler->lvaCount == 0)
    {
        return;
    }

    /* We'll sort the variables by ref count - allocate the sorted table */

    if (m_compiler->lvaTrackedToVarNumSize < m_compiler->lvaCount)
    {
        m_compiler->lvaTrackedToVarNumSize = m_compiler->lvaCount;
        m_compiler->lvaTrackedToVarNum =
            new (m_compiler->getAllocator(CMK_LvaTable)) unsigned[m_compiler->lvaTrackedToVarNumSize];
    }

    unsigned  trackedCandidateCount = 0;
    unsigned* trackedCandidates     = m_compiler->lvaTrackedToVarNum;

    // Fill in the table used for sorting

    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);

        // Start by assuming that the variable will be tracked.
        varDsc->lvTracked = 1;
        INDEBUG(varDsc->lvTrackedWithoutIndex = 0);

        if (varDsc->lvRefCnt(m_compiler->lvaRefCountState) == 0)
        {
            // Zero ref count, make this untracked.
            varDsc->lvTracked = 0;
            varDsc->setLvRefCntWtd(0, m_compiler->lvaRefCountState);
        }

#if !defined(TARGET_64BIT)
        if (varTypeIsLong(varDsc) && varDsc->lvPromoted)
        {
            varDsc->lvTracked = 0;
        }
#endif // !defined(TARGET_64BIT)

        // Variables that are address-exposed, and all struct locals, are never enregistered, or tracked.
        // (The struct may be promoted, and its field variables enregistered/tracked, or the VM may "normalize"
        // its type so that its not seen by the JIT as a struct.)
        // Pinned variables may not be tracked (a condition of the GCInfo representation)
        // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
        // references when using the general GC encoding.
        if (varDsc->IsAddressExposed())
        {
            varDsc->lvTracked = 0;
            assert(varDsc->lvType != TYP_STRUCT || varDsc->lvDoNotEnregister); // For structs, should have set this when
                                                                               // we set m_addrExposed.
        }
        if (varTypeIsStruct(varDsc))
        {
            // Promoted structs will never be considered for enregistration anyway,
            // and the DoNotEnregister flag was used to indicate whether promotion was
            // independent or dependent.
            if (varDsc->lvPromoted)
            {
                varDsc->lvTracked = 0;
            }
            else if (!varDsc->IsEnregisterableType())
            {
                m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::NotRegSizeStruct));
            }
            else if (varDsc->lvType == TYP_STRUCT)
            {
                if (!varDsc->lvRegStruct && !m_compiler->compEnregStructLocals())
                {
                    m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::DontEnregStructs));
                }
                else if (varDsc->lvIsMultiRegArgOrRet())
                {
                    // Prolog and return generators do not support SIMD<->general register moves.
                    m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::IsStructArg));
                }
#if defined(TARGET_ARM)
                else if (varDsc->lvIsParam)
                {
                    // On arm we prespill all struct args,
                    // TODO-Arm-CQ: keep them in registers, it will need a fix
                    // to "On the ARM we will spill any incoming struct args" logic in codegencommon.
                    m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::IsStructArg));
                }
#endif // TARGET_ARM
            }
        }
        if (varDsc->lvIsStructField &&
            (m_compiler->lvaGetParentPromotionType(lclNum) != Compiler::PROMOTION_TYPE_INDEPENDENT))
        {
            m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::DepField));
        }
        if (varDsc->lvPinned)
        {
            varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
            m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::PinningRef));
#endif
        }
        if (!m_compiler->compEnregLocals())
        {
            m_compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::NoRegVars));
        }

        var_types type = genActualType(varDsc->TypeGet());

        switch (type)
        {
            case TYP_FLOAT:
            case TYP_DOUBLE:
            case TYP_INT:
            case TYP_LONG:
            case TYP_REF:
            case TYP_BYREF:
#ifdef FEATURE_SIMD
            case TYP_SIMD8:
            case TYP_SIMD12:
            case TYP_SIMD16:
#ifdef TARGET_XARCH
            case TYP_SIMD32:
            case TYP_SIMD64:
#endif // TARGET_XARCH
#ifdef FEATURE_MASKED_HW_INTRINSICS
            case TYP_MASK:
#endif // FEATURE_MASKED_HW_INTRINSICS
#endif // FEATURE_SIMD
            case TYP_STRUCT:
                break;

            case TYP_UNDEF:
            case TYP_UNKNOWN:
                noway_assert(!"lvType not set correctly");
                varDsc->lvType = TYP_INT;

                FALLTHROUGH;

            default:
                varDsc->lvTracked = 0;
        }

        if (varDsc->lvTracked)
        {
            trackedCandidates[trackedCandidateCount++] = lclNum;
        }
    }

    m_compiler->lvaTrackedCount = min(trackedCandidateCount, (unsigned)JitConfig.JitMaxLocalsToTrack());

    // Sort the candidates. In the late liveness passes we want lower tracked
    // indices to be more important variables, so we always do this. In early
    // liveness it does not matter, so we can skip it when we are going to
    // track everything.
    // TODO-TP: For early liveness we could do a partial sort for the large
    // case.
    if (!TLiveness::IsEarly || (m_compiler->lvaTrackedCount < trackedCandidateCount))
    {
        // Now sort the tracked variable table by ref-count
        if (m_compiler->compCodeOpt() == Compiler::SMALL_CODE)
        {
            jitstd::sort(trackedCandidates, trackedCandidates + trackedCandidateCount,
                         LclVarDsc_SmallCode_Less(m_compiler->lvaTable,
                                                  m_compiler->lvaRefCountState DEBUGARG(m_compiler->lvaCount)));
        }
        else
        {
            jitstd::sort(trackedCandidates, trackedCandidates + trackedCandidateCount,
                         LclVarDsc_BlendedCode_Less(m_compiler->lvaTable,
                                                    m_compiler->lvaRefCountState DEBUGARG(m_compiler->lvaCount)));
        }
    }

    JITDUMP("Tracked variable (%u out of %u) table:\n", m_compiler->lvaTrackedCount, m_compiler->lvaCount);

    // Assign indices to all the variables we've decided to track
    for (unsigned varIndex = 0; varIndex < m_compiler->lvaTrackedCount; varIndex++)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(trackedCandidates[varIndex]);
        assert(varDsc->lvTracked);
        varDsc->lvVarIndex = static_cast<unsigned short>(varIndex);

        INDEBUG(if (m_compiler->verbose) { m_compiler->gtDispLclVar(trackedCandidates[varIndex]); })
        JITDUMP(" [%6s]: refCnt = %4u, refCntWtd = %6s\n", varTypeName(varDsc->TypeGet()),
                varDsc->lvRefCnt(m_compiler->lvaRefCountState),
                refCntWtd2str(varDsc->lvRefCntWtd(m_compiler->lvaRefCountState), /* padForDecimalPlaces */ true));
    }

    JITDUMP("\n");

    // Mark all variables past the first 'lclMAX_TRACKED' as untracked
    for (unsigned varIndex = m_compiler->lvaTrackedCount; varIndex < trackedCandidateCount; varIndex++)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(trackedCandidates[varIndex]);
        assert(varDsc->lvTracked);
        varDsc->lvTracked = 0;
    }

    // We have a new epoch, and also cache the tracked var count in terms of size_t's sufficient to hold that many bits.
    m_compiler->lvaCurEpoch++;
    m_compiler->lvaTrackedCountInSizeTUnits =
        roundUp((unsigned)m_compiler->lvaTrackedCount, (unsigned)(sizeof(size_t) * 8)) / unsigned(sizeof(size_t) * 8);

#ifdef DEBUG
    VarSetOps::AssignNoCopy(m_compiler, m_compiler->lvaTrackedVars, VarSetOps::MakeFull(m_compiler));
#endif
}

//------------------------------------------------------------------------
// PerBlockLocalVarLiveness:
//   Compute def and use sets for the IR.
//
template <typename TLiveness>
void Liveness<TLiveness>::PerBlockLocalVarLiveness()
{
#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("*************** In Liveness::PerBlockLocalVarLiveness()\n");
    }
#endif // DEBUG

    unsigned livenessVarEpoch = m_compiler->GetCurLVEpoch();

    // Avoid allocations in the long case.
    VarSetOps::AssignNoCopy(m_compiler, m_curUseSet, VarSetOps::MakeEmpty(m_compiler));
    VarSetOps::AssignNoCopy(m_compiler, m_curDefSet, VarSetOps::MakeEmpty(m_compiler));

    // GC Heap and ByrefExposed can share states unless we see a def of byref-exposed
    // memory that is not a GC Heap def.
    m_compiler->byrefStatesMatchGcHeapStates = true;

    for (unsigned i = m_compiler->m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_compiler->m_dfsTree->GetPostOrder(i - 1);
        VarSetOps::ClearD(m_compiler, m_curUseSet);
        VarSetOps::ClearD(m_compiler, m_curDefSet);

        if (TLiveness::ComputeMemoryLiveness)
        {
            m_curMemoryUse   = emptyMemoryKindSet;
            m_curMemoryDef   = emptyMemoryKindSet;
            m_curMemoryHavoc = emptyMemoryKindSet;
        }

        m_compiler->compCurBB = block;
        if (TLiveness::IsLIR)
        {
            for (GenTree* node : LIR::AsRange(block))
            {
                PerNodeLocalVarLiveness(node);
            }
        }
        else if (!TLiveness::IsEarly)
        {
            assert(m_compiler->fgNodeThreading == NodeThreading::AllTrees);
            for (Statement* const stmt : block->NonPhiStatements())
            {
                m_compiler->compCurStmt = stmt;
                for (GenTree* const node : stmt->TreeList())
                {
                    PerNodeLocalVarLiveness(node);
                }
            }
        }
        else
        {
            assert(m_compiler->fgNodeThreading == NodeThreading::AllLocals);

            if (m_compiler->compQmarkUsed)
            {
                for (Statement* stmt : block->Statements())
                {
                    GenTree* dst;
                    GenTree* qmark = m_compiler->fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
                    if (qmark == nullptr)
                    {
                        for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                        {
                            MarkUseDef(lcl);
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
                            bool isUse = (lcl->gtFlags & GTF_VAR_DEF) == 0;
                            // We can still handle the pure def at the top level.
                            bool conditional = lcl != dst;
                            if (isUse || !conditional)
                            {
                                MarkUseDef(lcl);
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
                        MarkUseDef(lcl);
                    }
                }
            }
        }

        // Mark the FrameListRoot as used, if applicable.

        if (block->KindIs(BBJ_RETURN) && m_compiler->compMethodRequiresPInvokeFrame())
        {
            assert(!m_compiler->opts.ShouldUsePInvokeHelpers() ||
                   (m_compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
            if (!m_compiler->opts.ShouldUsePInvokeHelpers())
            {
                // 32-bit targets always pop the frame in the epilog.
                // For 64-bit targets, we only do this in the epilog for IL stubs;
                // for non-IL stubs the frame is popped after every PInvoke call.
#ifdef TARGET_64BIT
                if (m_compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
#endif
                {
                    LclVarDsc* varDsc = m_compiler->lvaGetDesc(m_compiler->info.compLvFrameListRoot);

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(m_compiler, m_curDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(m_compiler, m_curUseSet, varDsc->lvVarIndex);
                        }
                    }
                }
            }
        }

        VarSetOps::Assign(m_compiler, block->bbVarUse, m_curUseSet);
        VarSetOps::Assign(m_compiler, block->bbVarDef, m_curDefSet);

        /* also initialize the IN set, just in case we will do multiple DFAs */

        VarSetOps::AssignNoCopy(m_compiler, block->bbLiveIn, VarSetOps::MakeEmpty(m_compiler));

        if (TLiveness::ComputeMemoryLiveness)
        {
            block->bbMemoryUse    = m_curMemoryUse;
            block->bbMemoryDef    = m_curMemoryDef;
            block->bbMemoryHavoc  = m_curMemoryHavoc;
            block->bbMemoryLiveIn = emptyMemoryKindSet;
        }
    }

    noway_assert(livenessVarEpoch == m_compiler->GetCurLVEpoch());
#ifdef DEBUG
    if (VERBOSE)
    {
        for (BasicBlock* block : m_compiler->Blocks())
        {
            VARSET_TP allVars(VarSetOps::Union(m_compiler, block->bbVarUse, block->bbVarDef));
            printf(FMT_BB, block->bbNum);
            printf(" USE(%d)=", VarSetOps::Count(m_compiler, block->bbVarUse));
            m_compiler->lvaDispVarSet(block->bbVarUse, allVars);
            if (TLiveness::ComputeMemoryLiveness)
            {
                for (MemoryKind memoryKind : allMemoryKinds())
                {
                    if ((block->bbMemoryUse & memoryKindSet(memoryKind)) != 0)
                    {
                        printf(" + %s", memoryKindNames[memoryKind]);
                    }
                }
            }
            printf("\n     DEF(%d)=", VarSetOps::Count(m_compiler, block->bbVarDef));
            m_compiler->lvaDispVarSet(block->bbVarDef, allVars);
            if (TLiveness::ComputeMemoryLiveness)
            {
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
            }
            printf("\n\n");
        }

        if (TLiveness::ComputeMemoryLiveness)
        {
            printf("** Memory liveness computed, GcHeap states and ByrefExposed states %s\n",
                   (m_compiler->byrefStatesMatchGcHeapStates ? "match" : "diverge"));
        }
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// PerNodeLocalVarLiveness:
//   Set m_curMemoryUse and m_curMemoryDef when memory is read or updated
//   Call fgMarkUseDef for any Local variables encountered
//
// Template arguments:
//    lowered - Whether or not this is liveness on lowered IR, where LCL_ADDRs
//    on tracked locals may appear.
//
// Arguments:
//    tree       - The current node.
//
template <typename TLiveness>
void Liveness<TLiveness>::PerNodeLocalVarLiveness(GenTree* tree)
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
            MarkUseDef(tree->AsLclVarCommon());
            break;

        case GT_LCL_ADDR:
            if (TLiveness::IsLIR)
            {
                // If this is a definition of a retbuf then we process it as
                // part of the GT_CALL node.
                if (IsTrackedRetBufferAddress(LIR::AsRange(m_compiler->compCurBB), tree))
                {
                    break;
                }

                MarkUseDef(tree->AsLclVarCommon());
            }
            break;

        case GT_IND:
        case GT_BLK:
            if (TLiveness::ComputeMemoryLiveness)
            {
                // For Volatile indirection, first mutate GcHeap/ByrefExposed
                // see comments in ValueNum.cpp (under the memory read case)
                // This models Volatile reads as def-then-use of memory.
                // and allows for a CSE of a subsequent non-volatile read
                if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
                {
                    // For any Volatile indirection, we must handle it as a
                    // definition of the GcHeap/ByrefExposed
                    m_curMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                }

                m_curMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
            }
            break;

        // We'll assume these are use-then-defs of memory.
        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
            if (TLiveness::ComputeMemoryLiveness)
            {
                m_curMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                m_curMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                m_curMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
            }
            break;

        case GT_STOREIND:
        case GT_STORE_BLK:
        case GT_MEMORYBARRIER: // Similar to Volatile indirections, we must handle this as a memory def.
            if (TLiveness::ComputeMemoryLiveness)
            {
                m_curMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
            }
            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
        {
            PerNodeLocalVarLiveness(tree->AsHWIntrinsic());
            break;
        }
#endif // FEATURE_HW_INTRINSICS

        // For now, all calls read/write GcHeap/ByrefExposed, writes in their entirety.  Might tighten this case later.
        case GT_CALL:
        {
            GenTreeCall* call = tree->AsCall();
            if (TLiveness::ComputeMemoryLiveness)
            {
                bool modHeap = true;
                if (call->IsHelperCall())
                {
                    CorInfoHelpFunc helpFunc = m_compiler->eeGetHelperNum(call->gtCallMethHnd);

                    if (!Compiler::s_helperCallProperties.MutatesHeap(helpFunc) &&
                        !Compiler::s_helperCallProperties.MayRunCctor(helpFunc))
                    {
                        modHeap = false;
                    }
                }

                if (modHeap)
                {
                    m_curMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                    m_curMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                    m_curMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                }
            }

            // If this is a p/invoke unmanaged call or if this is a tail-call via helper,
            // and we have an unmanaged p/invoke call in the method,
            // then we're going to run the p/invoke epilog.
            // So we mark the FrameRoot as used by this instruction.
            // This ensures that the block->bbVarUse will contain
            // the FrameRoot local var if is it a tracked variable.

            if ((call->IsUnmanaged() || call->IsTailCallViaJitHelper()) && m_compiler->compMethodRequiresPInvokeFrame())
            {
                assert(!m_compiler->opts.ShouldUsePInvokeHelpers() ||
                       (m_compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
                if (!m_compiler->opts.ShouldUsePInvokeHelpers() && !call->IsSuppressGCTransition())
                {
                    // Get the FrameRoot local and mark it as used.

                    LclVarDsc* varDsc = m_compiler->lvaGetDesc(m_compiler->info.compLvFrameListRoot);

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(m_compiler, m_curDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(m_compiler, m_curUseSet, varDsc->lvVarIndex);
                        }
                    }
                }
            }

            auto visitDef = [=](GenTreeLclVarCommon* lcl) {
                MarkUseDef(lcl);
                return GenTree::VisitResult::Continue;
            };
            call->VisitLocalDefNodes(m_compiler, visitDef);
            break;
        }

        default:
            break;
    }
}

#if defined(FEATURE_HW_INTRINSICS)
template <typename TLiveness>
void Liveness<TLiveness>::PerNodeLocalVarLiveness(GenTreeHWIntrinsic* hwintrinsic)
{
    if (TLiveness::ComputeMemoryLiveness)
    {
        NamedIntrinsic intrinsicId = hwintrinsic->GetHWIntrinsicId();

        // We can't call fgMutateGcHeap unless the block has recorded a MemoryDef
        //
        if (hwintrinsic->OperIsMemoryStoreOrBarrier())
        {
            // We currently handle this like a Volatile store or GT_MEMORYBARRIER
            // so it counts as a definition of GcHeap/ByrefExposed
            m_curMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
        }
        else if (hwintrinsic->OperIsMemoryLoad())
        {
            // This instruction loads from memory and we need to record this information
            m_curMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
        }
    }
}
#endif // FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// MarkUseDef:
//   Mark a local in the current def/use set.
//
// Parameters:
//   tree - The local
//
// Template parameters:
//   ssaLiveness - Whether the liveness computed is for SSA and should follow
//   same modelling rules as SSA. SSA models partial defs like (v.x = 123) as
//   (v = v with x = 123), which also implies that these partial definitions
//   become uses. For dead-code elimination this is more conservative than
//   needed, so outside SSA we do not model partial defs in this way:
//
//   * In SSA: Partial defs are full defs but are also uses. They impact both
//   bbVarUse and bbVarDef.
//
//   * Outside SSA: Partial defs are _not_ full defs and are also not
//   considered uses. They do not get included in bbVarUse/bbVarDef.
//
template <typename TLiveness>
void Liveness<TLiveness>::MarkUseDef(GenTreeLclVarCommon* tree)
{
    assert((tree->OperIsLocal() && !tree->OperIs(GT_PHI_ARG)) || tree->OperIs(GT_LCL_ADDR));

    const unsigned   lclNum = tree->GetLclNum();
    LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

    // We should never encounter a reference to a lclVar that has a zero refCnt.
    if (varDsc->lvRefCnt(m_compiler->lvaRefCountState) == 0 && (!varTypeIsPromotable(varDsc) || !varDsc->lvPromoted))
    {
        JITDUMP("Found reference to V%02u with zero refCnt.\n", lclNum);
        assert(!"We should never encounter a reference to a lclVar that has a zero refCnt.");
        varDsc->setLvRefCnt(1);
    }

    const bool isDef     = ((tree->gtFlags & GTF_VAR_DEF) != 0);
    const bool isFullDef = isDef && ((tree->gtFlags & GTF_VAR_USEASG) == 0);
    const bool isUse     = TLiveness::SsaLiveness ? !isFullDef : !isDef;

    if (varDsc->lvTracked)
    {
        assert(varDsc->lvVarIndex < m_compiler->lvaTrackedCount);

        // We don't treat stores to tracked locals as modifications of ByrefExposed memory;
        // Make sure no tracked local is addr-exposed, to make sure we don't incorrectly CSE byref
        // loads aliasing it across a store to it.
        assert(!varDsc->IsAddressExposed());

        if (TLiveness::IsLIR && (varDsc->lvType != TYP_STRUCT) && !varTypeIsMultiReg(varDsc))
        {
            // If this is an enregisterable variable that is not marked doNotEnregister and not defined via address,
            // we should only see direct references (not ADDRs).
            assert(varDsc->lvDoNotEnregister || varDsc->lvDefinedViaAddress ||
                   tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR));
        }

        if (isUse && !VarSetOps::IsMember(m_compiler, m_curDefSet, varDsc->lvVarIndex))
        {
            // This is an exposed use; add it to the set of uses.
            VarSetOps::AddElemD(m_compiler, m_curUseSet, varDsc->lvVarIndex);
        }

        if (TLiveness::SsaLiveness ? isDef : isFullDef)
        {
            // This is a def, add it to the set of defs.
            VarSetOps::AddElemD(m_compiler, m_curDefSet, varDsc->lvVarIndex);
        }
    }
    else
    {
        if (TLiveness::ComputeMemoryLiveness && varDsc->IsAddressExposed())
        {
            // Reflect the effect on ByrefExposed memory

            if (isUse)
            {
                m_curMemoryUse |= memoryKindSet(ByrefExposed);
            }
            if (isDef)
            {
                m_curMemoryDef |= memoryKindSet(ByrefExposed);

                // We've found a store that modifies ByrefExposed
                // memory but not GcHeap memory, so track their
                // states separately.
                m_compiler->byrefStatesMatchGcHeapStates = false;
            }
        }

        if (varTypeIsPromotable(varDsc))
        {
            Compiler::lvaPromotionType promotionType = m_compiler->lvaGetPromotionType(varDsc);

            if (promotionType != Compiler::PROMOTION_TYPE_NONE)
            {
                for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
                {
                    if (!m_compiler->lvaTable[i].lvTracked)
                    {
                        continue;
                    }

                    unsigned varIndex = m_compiler->lvaTable[i].lvVarIndex;
                    if (isUse && !VarSetOps::IsMember(m_compiler, m_curDefSet, varIndex))
                    {
                        VarSetOps::AddElemD(m_compiler, m_curUseSet, varIndex);
                    }

                    if (TLiveness::SsaLiveness ? isDef : isFullDef)
                    {
                        VarSetOps::AddElemD(m_compiler, m_curDefSet, varIndex);
                    }
                }
            }
        }
    }
}

/*****************************************************************************
 *
 *  Iterative data flow for live variable info and availability of range
 *  check index expressions.
 */
template <typename TLiveness>
void Liveness<TLiveness>::InterBlockLocalVarLiveness()
{
#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("*************** IngInterBlockLocalVarLiveness()\n");
    }
#endif

    /* This global flag is set whenever we remove a statement */

    m_compiler->fgStmtRemoved = false;

    // keep track if a bbLiveIn changed due to dead store removal
    m_livenessChanged = false;

    /* Compute the IN and OUT sets for tracked variables */

    DoLiveVarAnalysis();

    //-------------------------------------------------------------------------
    // Variables involved in exception-handlers and finally blocks need
    // to be specially marked
    //

    VARSET_TP exceptVars(VarSetOps::MakeEmpty(m_compiler));  // vars live on entry to a handler
    VARSET_TP finallyVars(VarSetOps::MakeEmpty(m_compiler)); // vars live on exit of a 'finally' block

    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (block->hasEHBoundaryIn())
        {
            // Note the set of variables live on entry to exception handler.
            VarSetOps::UnionD(m_compiler, exceptVars, block->bbLiveIn);
        }

        if (block->hasEHBoundaryOut())
        {
            // Get the set of live variables on exit from an exception region.
            VarSetOps::UnionD(m_compiler, exceptVars, block->bbLiveOut);
            if (block->KindIs(BBJ_EHFINALLYRET))
            {
                // Live on exit from finally.
                // We track these separately because, in addition to having EH live-out semantics,
                // they are must-init.
                VarSetOps::UnionD(m_compiler, finallyVars, block->bbLiveOut);
            }
        }
    }

    if (!TLiveness::IsEarly)
    {
        LclVarDsc* varDsc;
        unsigned   varNum;

        for (varNum = 0, varDsc = m_compiler->lvaTable; varNum < m_compiler->lvaCount; varNum++, varDsc++)
        {
            // Ignore the variable if it's not tracked

            if (!varDsc->lvTracked)
            {
                continue;
            }

            // Fields of dependently promoted structs may be tracked. We shouldn't set lvMustInit on them since
            // the whole parent struct will be initialized; however, lvLiveInOutOfHndlr should be set on them
            // as appropriate.

            bool fieldOfDependentlyPromotedStruct = m_compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc);

            // Un-init locals may need auto-initialization. Note that the
            // liveness of such locals will bubble to the top (fgFirstBB)
            // in fgInterBlockLocalVarLiveness()

            if (!varDsc->lvIsParam && !varDsc->lvIsParamRegTarget &&
                VarSetOps::IsMember(m_compiler, m_compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex) &&
                (m_compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet())) && !fieldOfDependentlyPromotedStruct)
            {
                varDsc->lvMustInit = true;
            }

            // Mark all variables that are live on entry to an exception handler
            // or on exit from a filter handler or finally.

            bool isFinallyVar = VarSetOps::IsMember(m_compiler, finallyVars, varDsc->lvVarIndex);
            if (isFinallyVar || VarSetOps::IsMember(m_compiler, exceptVars, varDsc->lvVarIndex))
            {
                // Mark the variable appropriately.
                m_compiler->lvaSetVarLiveInOutOfHandler(varNum);

                // Mark all pointer variables live on exit from a 'finally' block as
                // 'explicitly initialized' (must-init) for GC-ref types.

                if (isFinallyVar)
                {
                    // Set lvMustInit only if we have a non-arg, GC pointer.
                    if (!varDsc->lvIsParam && !varDsc->lvIsParamRegTarget && varTypeIsGC(varDsc->TypeGet()))
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

    VARSET_TP keepAliveVars(VarSetOps::MakeEmpty(m_compiler));

    for (unsigned i = m_compiler->m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_compiler->m_dfsTree->GetPostOrder(i - 1);
        /* Tell everyone what block we're working on */

        m_compiler->compCurBB = block;

        /* Remember those vars live on entry to exception handlers */
        /* if we are part of a try block */
        VarSetOps::ClearD(m_compiler, keepAliveVars);

        if (block->HasPotentialEHSuccs(m_compiler))
        {
            MemoryKindSet memoryLiveness = 0;
            m_compiler->fgAddHandlerLiveVars(block, keepAliveVars, memoryLiveness);

            // keepAliveVars is a subset of exceptVars
            noway_assert(VarSetOps::IsSubset(m_compiler, keepAliveVars, exceptVars));
        }

        /* Start with the variables live on exit from the block */

        VARSET_TP life(VarSetOps::MakeCopy(m_compiler, block->bbLiveOut));

        /* Mark any interference we might have at the end of the block */

        if (TLiveness::IsLIR)
        {
            ComputeLifeLIR(life, block, keepAliveVars);
        }
        else if (!TLiveness::IsEarly)
        {
            assert(m_compiler->fgNodeThreading == NodeThreading::AllTrees);
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

                m_compiler->compCurStmt = nextStmt;
                nextStmt                = nextStmt->GetPrevStmt();

                /* Compute the liveness for each tree node in the statement */
                bool stmtInfoDirty = false;

                ComputeLife(life, m_compiler->compCurStmt->GetRootNode(), nullptr, keepAliveVars,
                            &stmtInfoDirty DEBUGARG(&treeModf));

                if (stmtInfoDirty)
                {
                    m_compiler->gtSetStmtInfo(m_compiler->compCurStmt);
                    m_compiler->fgSetStmtSeq(m_compiler->compCurStmt);
                    m_compiler->gtUpdateStmtSideEffects(m_compiler->compCurStmt);
                }

#ifdef DEBUG
                if (m_compiler->verbose && treeModf)
                {
                    printf("\nfgComputeLife modified tree:\n");
                    m_compiler->gtDispTree(m_compiler->compCurStmt->GetRootNode());
                    printf("\n");
                }
#endif // DEBUG
            } while (m_compiler->compCurStmt != firstStmt);
        }
        else
        {
            assert(m_compiler->fgNodeThreading == NodeThreading::AllLocals);
            m_compiler->compCurStmt = nullptr;

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
                if (m_compiler->compQmarkUsed)
                {
                    qmark = m_compiler->fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
                }

                if (qmark != nullptr)
                {
                    for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr;)
                    {
                        assert(cur->OperIsAnyLocal());
                        bool isDef       = (cur->gtFlags & GTF_VAR_DEF) != 0;
                        bool conditional = cur != dst;
                        // Ignore conditional defs that would otherwise
                        // (incorrectly) interfere with liveness in other
                        // branches of the qmark.
                        if (isDef && conditional)
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        if (!ComputeLifeLocal(life, keepAliveVars, cur))
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        assert(cur == dst);
                        cur = TryRemoveDeadStoreEarly(stmt, cur->AsLclVarCommon());
                    }
                }
                else
                {
                    for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr;)
                    {
                        assert(cur->OperIsAnyLocal());
                        if (!ComputeLifeLocal(life, keepAliveVars, cur))
                        {
                            cur = cur->gtPrev;
                            continue;
                        }

                        cur = TryRemoveDeadStoreEarly(stmt, cur->AsLclVarCommon());
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
        if (!VarSetOps::Equal(m_compiler, life, block->bbLiveIn))
        {
            /* some variables have become dead all across the block
               So life should be a subset of block->bbLiveIn */

            // We changed the liveIn of the block, which may affect liveOut of others,
            // which may expose more dead stores.
            m_livenessChanged = true;

            noway_assert(VarSetOps::IsSubset(m_compiler, life, block->bbLiveIn));

            /* set the new bbLiveIn */

            VarSetOps::Assign(m_compiler, block->bbLiveIn, life);

            /* compute the new bbLiveOut for all the predecessors of this block */
        }

        noway_assert(m_compiler->compCurBB == block);
        INDEBUG(m_compiler->compCurBB = nullptr);
    }

    m_compiler->fgLocalVarLivenessDone = true;
}

/*****************************************************************************
 *
 *  This is the classic algorithm for Live Variable Analysis.
 *  If updateInternalOnly==true, only update BBF_INTERNAL blocks.
 */
template <typename TLiveness>
void Liveness<TLiveness>::DoLiveVarAnalysis()
{
    m_liveIn            = VarSetOps::MakeEmpty(m_compiler);
    m_liveOut           = VarSetOps::MakeEmpty(m_compiler);
    m_ehHandlerLiveVars = VarSetOps::MakeEmpty(m_compiler);

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

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("\nBB liveness after DoLiveVarAnalysis():\n\n");
        m_compiler->fgDispBBLiveness();
    }
#endif // DEBUG
}

template <typename TLiveness>
bool Liveness<TLiveness>::PerBlockAnalysis(BasicBlock* block, bool keepAliveThis)
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

    if (TLiveness::IsEarly && m_compiler->opts.IsOSR() && block->HasFlag(BBF_RECURSIVE_TAILCALL))
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

#ifdef DEBUG

void Compiler::fgDispBBLiveness()
{
    for (BasicBlock* const block : Blocks())
    {
        fgDispBBLiveness(block);
    }
}

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

#endif // DEBUG

/*****************************************************************************
 *
 * Compute the set of live variables at each node in a given statement
 * or subtree of a statement moving backward from startNode to endNode
 */

template <typename TLiveness>
void Liveness<TLiveness>::ComputeLife(VARSET_TP&           life,
                                      GenTree*             startNode,
                                      GenTree*             endNode,
                                      VARSET_VALARG_TP     keepAliveVars,
                                      bool* pStmtInfoDirty DEBUGARG(bool* treeModf))
{
    // Don't kill vars in scope
    noway_assert(VarSetOps::IsSubset(m_compiler, keepAliveVars, life));
    noway_assert(endNode || (startNode == m_compiler->compCurStmt->GetRootNode()));
    assert(!TLiveness::IsEarly);

    for (GenTree* tree = startNode; tree != endNode; tree = tree->gtPrev)
    {
    AGAIN:
        assert(!tree->OperIs(GT_QMARK));

        bool       isUse        = false;
        bool       doAgain      = false;
        bool       storeRemoved = false;
        LclVarDsc* varDsc       = nullptr;

        if (tree->IsCall())
        {
            GenTreeLclVarCommon* const partialDef = ComputeLifeCall(life, keepAliveVars, tree->AsCall());
            if (partialDef != nullptr)
            {
                assert((partialDef->gtFlags & GTF_VAR_USEASG) != 0);
                isUse  = true;
                varDsc = m_compiler->lvaGetDesc(partialDef);
            }
        }
        else if (tree->OperIsNonPhiLocal())
        {
            isUse            = (tree->gtFlags & GTF_VAR_USEASG) != 0;
            bool isDeadStore = ComputeLifeLocal(life, keepAliveVars, tree);
            if (isDeadStore)
            {
                varDsc = m_compiler->lvaGetDesc(tree->AsLclVarCommon());

                if (RemoveDeadStore(&tree, varDsc, life, &doAgain, pStmtInfoDirty, &storeRemoved DEBUGARG(treeModf)))
                {
                    assert(!doAgain);
                    break;
                }
            }
        }

        // SSA and VN treat "partial definitions" as true uses, so for this
        // front-end liveness pass we must add them to the live set in case
        // we failed to remove the dead store.
        //
        if ((varDsc != nullptr) && isUse && !storeRemoved)
        {
            if (varDsc->lvTracked)
            {
                VarSetOps::AddElemD(m_compiler, life, varDsc->lvVarIndex);
            }
            if (varDsc->lvPromoted)
            {
                for (unsigned fieldIndex = 0; fieldIndex < varDsc->lvFieldCnt; fieldIndex++)
                {
                    LclVarDsc* fieldVarDsc = m_compiler->lvaGetDesc(varDsc->lvFieldLclStart + fieldIndex);
                    if (fieldVarDsc->lvTracked)
                    {
                        VarSetOps::AddElemD(m_compiler, life, fieldVarDsc->lvVarIndex);
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

//------------------------------------------------------------------------
// ComputeLifeCall: compute the changes to local var liveness
//                              due to a GT_CALL node.
//
// Arguments:
//    life          - The live set that is being computed.
//    keepAliveVars - Tracked locals that must be kept alive everywhere in the block
//    call          - The call node in question.
//
// Returns:
//    partially defined local by the call, if any (eg retbuf)
//
template <typename TLiveness>
GenTreeLclVarCommon* Liveness<TLiveness>::ComputeLifeCall(VARSET_TP&       life,
                                                          VARSET_VALARG_TP keepAliveVars,
                                                          GenTreeCall*     call)
{
    assert(call != nullptr);
    // If this is a tail-call via helper, and we have any unmanaged p/invoke calls in
    // the method, then we're going to run the p/invoke epilog
    // So we mark the FrameRoot as used by this instruction.
    // This ensure that this variable is kept alive at the tail-call
    if (call->IsTailCallViaJitHelper() && m_compiler->compMethodRequiresPInvokeFrame())
    {
        assert(!m_compiler->opts.ShouldUsePInvokeHelpers() || (m_compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!m_compiler->opts.ShouldUsePInvokeHelpers())
        {
            // Get the FrameListRoot local and make it live.

            LclVarDsc* frameVarDsc = m_compiler->lvaGetDesc(m_compiler->info.compLvFrameListRoot);

            if (frameVarDsc->lvTracked)
            {
                VarSetOps::AddElemD(m_compiler, life, frameVarDsc->lvVarIndex);
            }
        }
    }

    // TODO: we should generate the code for saving to/restoring
    //       from the inlined PInvoke frame instead.

    /* Is this call to unmanaged code? */
    if (call->IsUnmanaged() && m_compiler->compMethodRequiresPInvokeFrame())
    {
        // Get the FrameListRoot local and make it live.
        assert(!m_compiler->opts.ShouldUsePInvokeHelpers() || (m_compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!m_compiler->opts.ShouldUsePInvokeHelpers() && !call->IsSuppressGCTransition())
        {
            LclVarDsc* frameVarDsc = m_compiler->lvaGetDesc(m_compiler->info.compLvFrameListRoot);

            if (frameVarDsc->lvTracked)
            {
                unsigned varIndex = frameVarDsc->lvVarIndex;
                noway_assert(varIndex < m_compiler->lvaTrackedCount);

                // Is the variable already known to be alive?
                //
                if (VarSetOps::IsMember(m_compiler, life, varIndex))
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
                    VarSetOps::AddElemD(m_compiler, life, varIndex);
                    call->gtCallMoreFlags |= GTF_CALL_M_FRAME_VAR_DEATH;
                }
            }
        }
    }

    GenTreeLclVarCommon* partialDef = nullptr;

    auto visitDef = [&](const LocalDef& def) {
        if (!def.IsEntire)
        {
            assert(partialDef == nullptr);
            partialDef = def.Def;
        }

        ComputeLifeLocal(life, keepAliveVars, def.Def);
        return GenTree::VisitResult::Continue;
    };

    call->VisitLocalDefs(m_compiler, visitDef);

    return partialDef;
}

//------------------------------------------------------------------------
// Compiler::ComputeLifeLocal:
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
template <typename TLiveness>
bool Liveness<TLiveness>::ComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode)
{
    unsigned lclNum = lclVarNode->AsLclVarCommon()->GetLclNum();

    assert(lclNum < m_compiler->lvaCount);
    LclVarDsc& varDsc = m_compiler->lvaTable[lclNum];
    bool       isDef  = ((lclVarNode->gtFlags & GTF_VAR_DEF) != 0);

    // Is this a tracked variable?
    if (varDsc.lvTracked)
    {
        /* Is this a definition or use? */
        if (isDef)
        {
            return ComputeLifeTrackedLocalDef(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon());
        }
        else
        {
            ComputeLifeTrackedLocalUse(life, varDsc, lclVarNode->AsLclVarCommon());
        }
    }
    else
    {
        return ComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode->AsLclVarCommon());
    }
    return false;
}

//------------------------------------------------------------------------
// Compiler::fgComputeLifeTrackedLocalUse:
//    Compute the changes to local var liveness due to a use of a tracked local var.
//
// Arguments:
//    life          - The live set that is being computed.
//    varDsc        - The LclVar descriptor for the variable being used or defined.
//    node          - The node that is defining the lclVar.
template <typename TLiveness>
void Liveness<TLiveness>::ComputeLifeTrackedLocalUse(VARSET_TP& life, LclVarDsc& varDsc, GenTreeLclVarCommon* node)
{
    assert(node != nullptr);
    assert((node->gtFlags & GTF_VAR_DEF) == 0);
    assert(varDsc.lvTracked);

    const unsigned varIndex = varDsc.lvVarIndex;

    // Is the variable already known to be alive?
    if (VarSetOps::IsMember(m_compiler, life, varIndex))
    {
        // Since we may do liveness analysis multiple times, clear the GTF_VAR_DEATH if set.
        node->gtFlags &= ~GTF_VAR_DEATH;
        return;
    }

#ifdef DEBUG
    if (m_compiler->verbose && 0)
    {
        printf("Ref V%02u,T%02u] at ", node->GetLclNum(), varIndex);
        Compiler::printTreeID(node);
        printf(" life %s -> %s\n", VarSetOps::ToString(m_compiler, life),
               VarSetOps::ToString(m_compiler, VarSetOps::AddElem(m_compiler, life, varIndex)));
    }
#endif // DEBUG

    // The variable is being used, and it is not currently live.
    // So the variable is just coming to life
    node->gtFlags |= GTF_VAR_DEATH;
    VarSetOps::AddElemD(m_compiler, life, varIndex);
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
template <typename TLiveness>
bool Liveness<TLiveness>::ComputeLifeTrackedLocalDef(VARSET_TP&           life,
                                                     VARSET_VALARG_TP     keepAliveVars,
                                                     LclVarDsc&           varDsc,
                                                     GenTreeLclVarCommon* node)
{
    assert(node != nullptr);
    assert((node->gtFlags & GTF_VAR_DEF) != 0);
    assert(varDsc.lvTracked);

    const unsigned varIndex = varDsc.lvVarIndex;
    if (VarSetOps::IsMember(m_compiler, life, varIndex))
    {
        // The variable is live
        node->gtFlags &= ~GTF_VAR_DEATH;

        if ((node->gtFlags & GTF_VAR_USEASG) == 0)
        {
            // Remove the variable from the live set if it is not in the keepalive set.
            if (!VarSetOps::IsMember(m_compiler, keepAliveVars, varIndex))
            {
                VarSetOps::RemoveElemD(m_compiler, life, varIndex);
            }
#ifdef DEBUG
            if (m_compiler->verbose && 0)
            {
                printf("Def V%02u,T%02u at ", node->GetLclNum(), varIndex);
                Compiler::printTreeID(node);
                printf(" life %s -> %s\n",
                       VarSetOps::ToString(m_compiler,
                                           VarSetOps::Union(m_compiler, life,
                                                            VarSetOps::MakeSingleton(m_compiler, varIndex))),
                       VarSetOps::ToString(m_compiler, life));
            }
#endif // DEBUG
        }
    }
    else
    {
        // Dead store
        node->gtFlags |= GTF_VAR_DEATH;

        // keepAliveVars always stay alive
        noway_assert(!VarSetOps::IsMember(m_compiler, keepAliveVars, varIndex));

        // Do not consider this store dead if the target local variable represents
        // a promoted struct field of an address exposed local or if the address
        // of the variable has been exposed. Improved alias analysis could allow
        // stores to these sorts of variables to be removed at the cost of compile
        // time.
        return !varDsc.IsAddressExposed() &&
               !(varDsc.lvIsStructField && m_compiler->lvaTable[varDsc.lvParentLcl].IsAddressExposed());
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
template <typename TLiveness>
bool Liveness<TLiveness>::ComputeLifeUntrackedLocal(VARSET_TP&           life,
                                                    VARSET_VALARG_TP     keepAliveVars,
                                                    LclVarDsc&           varDsc,
                                                    GenTreeLclVarCommon* lclVarNode)
{
    assert(lclVarNode != nullptr);

    bool isDef = ((lclVarNode->gtFlags & GTF_VAR_DEF) != 0);

    // We have accurate ref counts when running late liveness so we can eliminate
    // some stores if the lhs local has a ref count of 1.
    if (isDef && TLiveness::IsLIR && (varDsc.lvRefCnt() == 1) && !varDsc.lvPinned)
    {
        if (varDsc.lvIsStructField)
        {
            if ((m_compiler->lvaGetDesc(varDsc.lvParentLcl)->lvRefCnt() == 1) &&
                (m_compiler->lvaGetParentPromotionType(&varDsc) == Compiler::PROMOTION_TYPE_DEPENDENT))
            {
                return true;
            }
        }
        else if (varTypeIsPromotable(varDsc.lvType))
        {
            if (m_compiler->lvaGetPromotionType(&varDsc) != Compiler::PROMOTION_TYPE_INDEPENDENT)
            {
                return true;
            }
        }
        else
        {
            return true;
        }
    }

    if (!varTypeIsPromotable(varDsc.TypeGet()) ||
        (m_compiler->lvaGetPromotionType(&varDsc) == Compiler::PROMOTION_TYPE_NONE))
    {
        return false;
    }

    assert(varDsc.lvFieldCnt <= 4);

    lclVarNode->gtFlags &= ~GTF_VAR_DEATH_MASK;
    bool anyFieldLive = false;
    for (unsigned i = varDsc.lvFieldLclStart; i < varDsc.lvFieldLclStart + varDsc.lvFieldCnt; ++i)
    {
        LclVarDsc* fieldVarDsc = m_compiler->lvaGetDesc(i);
#if !defined(TARGET_64BIT)
        if (!varTypeIsLong(fieldVarDsc->lvType) || !fieldVarDsc->lvPromoted)
#endif // !defined(TARGET_64BIT)
        {
            noway_assert(fieldVarDsc->lvIsStructField);
        }
        if (fieldVarDsc->lvTracked)
        {
            const unsigned varIndex  = fieldVarDsc->lvVarIndex;
            bool           fieldLive = VarSetOps::IsMember(m_compiler, life, varIndex);
            anyFieldLive |= fieldLive;

            if (!fieldLive)
            {
                lclVarNode->SetLastUse(i - varDsc.lvFieldLclStart);
            }

            if (isDef)
            {
                if (((lclVarNode->gtFlags & GTF_VAR_USEASG) == 0) &&
                    !VarSetOps::IsMember(m_compiler, keepAliveVars, varIndex))
                {
                    VarSetOps::RemoveElemD(m_compiler, life, varIndex);
                }
            }
            else
            {
                VarSetOps::AddElemD(m_compiler, life, varIndex);
            }
        }
        else
        {
            anyFieldLive = true;
        }
    }

    if (isDef && !anyFieldLive)
    {
        // Do not consider this store dead if the parent local variable is an address exposed local or
        // if the struct has any significant padding we must retain the value of.
        return !varDsc.IsAddressExposed();
    }

    return false;
}

//---------------------------------------------------------------------
// RemoveDeadStore - remove a store to a local which has no exposed uses.
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
template <typename TLiveness>
bool Liveness<TLiveness>::RemoveDeadStore(GenTree**           pTree,
                                          LclVarDsc*          varDsc,
                                          VARSET_VALARG_TP    life,
                                          bool*               doAgain,
                                          bool*               pStmtInfoDirty,
                                          bool* pStoreRemoved DEBUGARG(bool* treeModf))
{
    assert(!TLiveness::IsLIR);

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
        if (m_compiler->verbose)
        {
            printf(FMT_BB " - Dead store has side effects...\n", m_compiler->compCurBB->bbNum);
            m_compiler->gtDispTree(store);
            printf("\n");
        }
#endif // DEBUG

        m_compiler->gtExtractSideEffList(value, &sideEffList);
    }

    // Test for interior statement
    if (store->gtNext == nullptr)
    {
        // This is a "NORMAL" statement with the store node hanging from the statement.

        noway_assert(m_compiler->compCurStmt->GetRootNode() == store);
        JITDUMP("top level store\n");

        if (sideEffList != nullptr)
        {
            noway_assert((sideEffList->gtFlags & GTF_SIDE_EFFECT) != 0);
#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("Extracted side effects list...\n");
                m_compiler->gtDispTree(sideEffList);
                printf("\n");
            }
#endif // DEBUG

            // Replace the store statement with the list of side effects
            *pTree = sideEffList;
            m_compiler->compCurStmt->SetRootNode(sideEffList);
#ifdef DEBUG
            *treeModf = true;
#endif // DEBUG
       // Update ordering, costs, FP levels, etc.
            m_compiler->gtSetStmtInfo(m_compiler->compCurStmt);

            // Re-link the nodes for this statement
            m_compiler->fgSetStmtSeq(m_compiler->compCurStmt);

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
            m_compiler->fgRemoveStmt(m_compiler->compCurBB, m_compiler->compCurStmt);

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
            noway_assert(!VarSetOps::IsMember(m_compiler, life, varDsc->lvVarIndex));
        }
        else
        {
            for (unsigned i = 0; i < varDsc->lvFieldCnt; ++i)
            {
                unsigned fieldVarNum = varDsc->lvFieldLclStart + i;
                {
                    LclVarDsc* fieldVarDsc = m_compiler->lvaGetDesc(fieldVarNum);
                    noway_assert(fieldVarDsc->lvTracked &&
                                 !VarSetOps::IsMember(m_compiler, life, fieldVarDsc->lvVarIndex));
                }
            }
        }

        if (sideEffList != nullptr)
        {
            noway_assert((sideEffList->gtFlags & GTF_SIDE_EFFECT) != 0);
#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("Extracted side effects list from condition...\n");
                m_compiler->gtDispTree(sideEffList);
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
                store->AsOp()->gtOp2 = m_compiler->gtNewNothingNode();
            }
        }
        else
        {
#ifdef DEBUG
            if (m_compiler->verbose)
            {
                printf("\nRemoving tree ");
                Compiler::printTreeID(store);
                printf(" in " FMT_BB " as useless\n", m_compiler->compCurBB->bbNum);
                m_compiler->gtDispTree(store);
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

        m_compiler->fgSetStmtSeq(m_compiler->compCurStmt);

        // Continue analysis from this node
        *pTree = store;

        return false;
    }

    return false;
}

//---------------------------------------------------------------------
// ComputeLifeLIR - fill out liveness flags in the IR nodes of the block
// provided the live-out set.
//
// Arguments
//    life          - the set of live-out variables from the block
//    block         - the block
//    keepAliveVars - variables that are globally live (usually due to being live into an EH successor)
//
template <typename TLiveness>
void Liveness<TLiveness>::ComputeLifeLIR(VARSET_TP& life, BasicBlock* block, VARSET_VALARG_TP keepAliveVars)
{
    noway_assert(VarSetOps::IsSubset(m_compiler, keepAliveVars, life));

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
                if ((call->TypeIs(TYP_VOID) || call->IsUnusedValue()) && !call->HasSideEffects(m_compiler))
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
                    if (!m_compiler->opts.ShouldUsePInvokeHelpers() &&
                        ((call->IsTailCall() && m_compiler->compMethodRequiresPInvokeFrame()) ||
                         (call->IsUnmanaged() && !call->IsSuppressGCTransition())) &&
                        m_compiler->lvaTable[m_compiler->info.compLvFrameListRoot].lvTracked)
                    {
                        m_compiler->fgStmtRemoved = true;
                    }
                }
                else
                {
                    ComputeLifeCall(life, keepAliveVars, call);
                }
                break;
            }

            case GT_LCL_VAR:
            case GT_LCL_FLD:
            {
                GenTreeLclVarCommon* const lclVarNode = node->AsLclVarCommon();
                LclVarDsc&                 varDsc     = m_compiler->lvaTable[lclVarNode->GetLclNum()];

                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead LclVar use:\n");
                    DISPNODE(lclVarNode);

                    blockRange.Delete(m_compiler, block, node);
                    if (varDsc.lvTracked && !m_compiler->opts.MinOpts())
                    {
                        m_compiler->fgStmtRemoved = true;
                    }
                }
                else if (varDsc.lvTracked)
                {
                    ComputeLifeTrackedLocalUse(life, varDsc, lclVarNode);
                }
                else
                {
                    ComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode);
                }
                break;
            }

            case GT_LCL_ADDR:
                if (node->IsUnusedValue())
                {
                    JITDUMP("Removing dead LclVar address:\n");
                    DISPNODE(node);

                    const bool isTracked = m_compiler->lvaTable[node->AsLclVarCommon()->GetLclNum()].lvTracked;
                    blockRange.Delete(m_compiler, block, node);
                    if (isTracked && !m_compiler->opts.MinOpts())
                    {
                        m_compiler->fgStmtRemoved = true;
                    }
                }
                else
                {
                    // For LCL_ADDRs that are defined by being passed as a
                    // retbuf we will handle them when we get to the call. We
                    // cannot consider them to be defined at the point of the
                    // LCL_ADDR since there may be uses between the LCL_ADDR
                    // and call.
                    if (IsTrackedRetBufferAddress(blockRange, node))
                    {
                        break;
                    }

                    isDeadStore = ComputeLifeLocal(life, keepAliveVars, node);
                    if (isDeadStore)
                    {
                        LIR::Use addrUse;
                        if (blockRange.TryGetUse(node, &addrUse) && addrUse.User()->OperIs(GT_STOREIND, GT_STORE_BLK))
                        {
                            GenTreeIndir* const store = addrUse.User()->AsIndir();

                            if (TryRemoveDeadStoreLIR(store, node->AsLclVarCommon(), block))
                            {
                                JITDUMP("Removing dead LclVar address:\n");
                                DISPNODE(node);
                                blockRange.Remove(node);

                                GenTree* data = store->AsIndir()->Data();
                                data->SetUnusedValue();

                                if (data->isIndir())
                                {
                                    Lowering::TransformUnusedIndirection(data->AsIndir(), m_compiler, block);
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

                LclVarDsc& varDsc = m_compiler->lvaTable[lclVarNode->GetLclNum()];
                if (varDsc.lvTracked)
                {
                    isDeadStore = ComputeLifeTrackedLocalDef(life, keepAliveVars, varDsc, lclVarNode);
                }
                else
                {
                    isDeadStore = ComputeLifeUntrackedLocal(life, keepAliveVars, varDsc, lclVarNode);
                }

                if (isDeadStore && TryRemoveDeadStoreLIR(node, lclVarNode, block))
                {
                    GenTree* value = lclVarNode->Data();
                    value->SetUnusedValue();

                    if (value->isIndir())
                    {
                        Lowering::TransformUnusedIndirection(value->AsIndir(), m_compiler, block);
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
#if defined(FEATURE_SIMD)
            case GT_CNS_VEC:
#endif // FEATURE_SIMD
#if defined(FEATURE_MASKED_HW_INTRINSICS)
            case GT_CNS_MSK:
#endif // FEATURE_MASKED_HW_INTRINSICS
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
            case GT_RETURN_SUSPEND:
            case GT_SWITCH:
            case GT_RETFILT:
            case GT_START_NONGC:
            case GT_START_PREEMPTGC:
            case GT_PROF_HOOK:
            case GT_SWITCH_TABLE:
            case GT_PINVOKE_PROLOG:
            case GT_PINVOKE_EPILOG:
            case GT_RETURNTRAP:
            case GT_PUTARG_STK:
            case GT_IL_OFFSET:
            case GT_RECORD_ASYNC_RESUME:
            case GT_KEEPALIVE:
            case GT_SWIFT_ERROR_RET:
            case GT_GCPOLL:
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

                TryRemoveNonLocalLIR(node, &blockRange);
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
                TryRemoveNonLocalLIR(node, &blockRange);
            }
            break;

            case GT_BLK:
            {
                bool removed = TryRemoveNonLocalLIR(node, &blockRange);
                if (!removed && node->IsUnusedValue())
                {
                    // IR doesn't expect dummy uses of `GT_BLK`.
                    JITDUMP("Transform an unused BLK node [%06d]\n", Compiler::dspTreeID(node));
                    Lowering::TransformUnusedIndirection(node->AsIndir(), m_compiler, block);
                }
            }
            break;

            default:
                TryRemoveNonLocalLIR(node, &blockRange);
                break;
        }
    }
}

//---------------------------------------------------------------------
// IsTrackedRetBufferAddress - given a LCL_ADDR node, check if it is the
// return buffer definition of a call.
//
// Arguments
//    range - the block range containing the LCL_ADDR
//    node  - the LCL_ADDR
//
template <typename TLiveness>
bool Liveness<TLiveness>::IsTrackedRetBufferAddress(LIR::Range& range, GenTree* node)
{
    assert(node->OperIs(GT_LCL_ADDR));
    if ((node->gtFlags & GTF_VAR_DEF) == 0)
    {
        return false;
    }

    LclVarDsc* dsc = m_compiler->lvaGetDesc(node->AsLclVarCommon());
    if (!dsc->lvTracked)
    {
        return false;
    }

    GenTree* curNode = node;
    do
    {
        LIR::Use use;
        if (!range.TryGetUse(curNode, &use))
        {
            return false;
        }

        curNode = use.User();

        if (curNode->IsCall())
        {
            return m_compiler->gtCallGetDefinedRetBufLclAddr(curNode->AsCall()) == node;
        }
    } while (curNode->OperIs(GT_FIELD_LIST) || curNode->OperIsPutArg());

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
template <typename TLiveness>
bool Liveness<TLiveness>::TryRemoveDeadStoreLIR(GenTree* store, GenTreeLclVarCommon* lclNode, BasicBlock* block)
{
    // We cannot remove stores to (tracked) TYP_STRUCT locals with GC pointers marked as "explicit init",
    // as said locals will be reported to the GC untracked, and deleting the explicit initializer risks
    // exposing uninitialized references.
    if ((lclNode->gtFlags & GTF_VAR_USEASG) == 0)
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNode);
        if (varDsc->lvHasExplicitInit && varDsc->TypeIs(TYP_STRUCT) && varDsc->HasGCPtr() && (varDsc->lvRefCnt() > 1))
        {
            JITDUMP("Not removing a potential explicit init [%06u] of V%02u\n", Compiler::dspTreeID(store),
                    lclNode->GetLclNum());
            return false;
        }
    }

    JITDUMP("Removing dead %s:\n", store->OperIsIndir() ? "indirect store" : "local store");
    DISPNODE(store);

    LIR::AsRange(block).Remove(store);
    m_compiler->fgStmtRemoved = true;

    return true;
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
template <typename TLiveness>
bool Liveness<TLiveness>::TryRemoveNonLocalLIR(GenTree* node, LIR::Range* blockRange)
{
    assert(!node->OperIsLocal());
    if (!node->IsValue() || node->IsUnusedValue())
    {
        // We are only interested in avoiding the removal of nodes with direct side effects
        // (as opposed to side effects of their children).
        // This default case should never include calls or stores.
        assert(!node->OperRequiresAsgFlag() && !node->OperIs(GT_CALL));
        if (!node->gtSetFlags() && !node->OperMayThrow(m_compiler) && CanUncontainOrRemoveOperands(node))
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
// CanUncontainOrRemoveOperands - Check if the operands of a node that is
// slated for removal can be either uncontained or deleted entirely.
//
// Arguments:
//   node - The node whose operands are to be checked
//
// Return Value:
//   Whether the operands can be uncontained or removed.
//
// Remarks:
//   Only embedded mask ops do not support standalone codegen. All other
//   nodes can be uncontained.
//
template <typename TLiveness>
bool Liveness<TLiveness>::CanUncontainOrRemoveOperands(GenTree* node)
{
#ifdef FEATURE_HW_INTRINSICS
    auto visit = [=](GenTree* op) {
        if (!op->isContained() || !op->IsEmbMaskOp() || !op->NodeOrContainedOperandsMayThrow(m_compiler))
        {
            return GenTree::VisitResult::Continue;
        }

        return GenTree::VisitResult::Abort;
    };

    return node->VisitOperands(visit) != GenTree::VisitResult::Abort;
#else
    return true;
#endif
}

//------------------------------------------------------------------------
// TryRemoveDeadStoreEarly:
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
template <typename TLiveness>
GenTree* Liveness<TLiveness>::TryRemoveDeadStoreEarly(Statement* stmt, GenTreeLclVarCommon* cur)
{
    if (!stmt->GetRootNode()->OperIsLocalStore() || (stmt->GetRootNode() != cur))
    {
        return cur->gtPrev;
    }

    JITDUMP("Store [%06u] is dead", Compiler::dspTreeID(stmt->GetRootNode()));
    // The def ought to be the last thing.
    assert(stmt->GetTreeListEnd() == cur);

    GenTree* sideEffects = nullptr;
    m_compiler->gtExtractSideEffList(stmt->GetRootNode()->AsLclVarCommon()->Data(), &sideEffects);

    if (sideEffects == nullptr)
    {
        JITDUMP(" and has no side effects, removing statement\n");
        m_compiler->fgRemoveStmt(m_compiler->compCurBB, stmt DEBUGARG(false));
        return nullptr;
    }
    else
    {
        JITDUMP(" but has side effects. Replacing with:\n\n");
        stmt->SetRootNode(sideEffects);
        m_compiler->fgSequenceLocals(stmt);
        DISPTREE(sideEffects);
        JITDUMP("\n");
        // continue at tail of the side effects
        return stmt->GetTreeListEnd();
    }
}

//------------------------------------------------------------------------
// fgSsaLiveness: Run SSA liveness.
//
void Compiler::fgSsaLiveness()
{
    struct SsaLivenessClass : public Liveness<SsaLivenessClass>
    {
        enum
        {
            SsaLiveness           = true,
            ComputeMemoryLiveness = true,
            IsLIR                 = false,
            IsEarly               = false,
        };

        SsaLivenessClass(Compiler* comp)
            : Liveness(comp)
        {
        }
    };

    SsaLivenessClass liveness(this);
    liveness.Run();
}

//------------------------------------------------------------------------
// fgAsyncLiveness: Run async liveness.
//
void Compiler::fgAsyncLiveness()
{
    struct AsyncLiveness : public Liveness<AsyncLiveness>
    {
        enum
        {
            SsaLiveness           = false,
            ComputeMemoryLiveness = false,
            IsLIR                 = true,
            IsEarly               = false,
        };

        AsyncLiveness(Compiler* comp)
            : Liveness(comp)
        {
        }
    };

    AsyncLiveness liveness(this);
    liveness.Run();
}

//------------------------------------------------------------------------
// fgPostLowerLiveness: Run post-lower liveness.
//
void Compiler::fgPostLowerLiveness()
{
    struct PostLowerLiveness : public Liveness<PostLowerLiveness>
    {
        enum
        {
            SsaLiveness           = false,
            ComputeMemoryLiveness = false,
            IsLIR                 = true,
            IsEarly               = false,
        };

        PostLowerLiveness(Compiler* comp)
            : Liveness(comp)
        {
        }
    };

    PostLowerLiveness liveness(this);
    liveness.Run();
}

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

    struct EarlyLiveness : public Liveness<EarlyLiveness>
    {
        enum
        {
            SsaLiveness           = false,
            ComputeMemoryLiveness = false,
            IsLIR                 = false,
            IsEarly               = true,
        };

        EarlyLiveness(Compiler* comp)
            : Liveness(comp)
        {
        }
    };

    EarlyLiveness liveness(this);
    liveness.Run();
    fgDidEarlyLiveness = true;
    return PhaseStatus::MODIFIED_EVERYTHING;
}
