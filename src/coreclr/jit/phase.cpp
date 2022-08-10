// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "phase.h"

//------------------------------------------------------------------------
// Observations ctor: snapshot key compiler variables before running a phase
//
// Arguments:
//    compiler - current compiler instance
//
Phase::Observations::Observations(Compiler* compiler)
{
#ifdef DEBUG
    m_compiler          = compiler->impInlineRoot();
    m_fgBBcount         = m_compiler->fgBBcount;
    m_fgBBNumMax        = m_compiler->fgBBNumMax;
    m_compHndBBtabCount = m_compiler->compHndBBtabCount;
    m_lvaCount          = m_compiler->lvaCount;
    m_compGenTreeID     = m_compiler->compGenTreeID;
    m_compStatementID   = m_compiler->compStatementID;
    m_compBasicBlockID  = m_compiler->compBasicBlockID;
#endif
}

//------------------------------------------------------------------------
// Observations Check: verify key compiler variables are unchanged
//    if phase claims it made no modifications
//
// Arguments:
//    status - status from the just-completed phase
//
void Phase::Observations::Check(PhaseStatus status)
{
#ifdef DEBUG
    if (status == PhaseStatus::MODIFIED_NOTHING)
    {
        assert(m_fgBBcount == m_compiler->fgBBcount);
        assert(m_fgBBNumMax == m_compiler->fgBBNumMax);
        assert(m_compHndBBtabCount == m_compiler->compHndBBtabCount);
        assert(m_lvaCount == m_compiler->lvaCount);
        assert(m_compGenTreeID == m_compiler->compGenTreeID);
        assert(m_compStatementID == m_compiler->compStatementID);
        assert(m_compBasicBlockID == m_compiler->compBasicBlockID);
    }
#endif
}

//------------------------------------------------------------------------
// Run: execute a phase and any before and after actions
//
void Phase::Run()
{
    Observations observations(comp);
    PrePhase();
    PhaseStatus status = DoPhase();
    PostPhase(status);
    observations.Check(status);
}

//------------------------------------------------------------------------
// PrePhase: perform dumps and checks before a phase executes
//
void Phase::PrePhase()
{
    comp->BeginPhase(m_phase);

#ifdef DEBUG

    // To help in the incremental conversion of jit activity to phases
    // without greatly increasing dump size or checked jit time, we
    // currently allow the phases that do pre-phase checks and
    // dumps via the phase object, and not via explicit calls from
    // the various methods in the phase.
    //
    // In the long run the aim is to get rid of all pre-phase checks
    // and dumps, relying instead on post-phase checks and dumps from
    // the preceding phase.
    //
    // Currently the list is just the set of phases that have custom
    // derivations from the Phase class.
    static Phases s_allowlist[] = {PHASE_BUILD_SSA, PHASE_OPTIMIZE_VALNUM_CSES, PHASE_RATIONALIZE, PHASE_LOWERING,
                                   PHASE_STACK_LEVEL_SETTER};
    bool doPrePhase = false;

    for (size_t i = 0; i < sizeof(s_allowlist) / sizeof(Phases); i++)
    {
        if (m_phase == s_allowlist[i])
        {
            doPrePhase = true;
            break;
        }
    }

    if (VERBOSE)
    {
        if (doPrePhase)
        {
            printf("Trees before %s\n", m_name);
            comp->fgDispBasicBlocks(true);
        }

        if (comp->compIsForInlining())
        {
            printf("\n*************** Inline @[%06u] Starting PHASE %s\n",
                   Compiler::dspTreeID(comp->impInlineInfo->iciCall), m_name);
        }
        else
        {
            printf("\n*************** Starting PHASE %s\n", m_name);
        }
    }

    if (doPrePhase)
    {
        if ((comp->activePhaseChecks == PhaseChecks::CHECK_ALL) && (comp->expensiveDebugCheckLevel >= 2))
        {
            // If everyone used the Phase class, this would duplicate the PostPhase() from the previous phase.
            // But, not everyone does, so go ahead and do the check here, too.
            comp->fgDebugCheckBBlist();
            comp->fgDebugCheckLinks();
        }
    }
#endif // DEBUG

#if DUMP_FLOWGRAPHS
    comp->fgDumpFlowGraph(m_phase, Compiler::PhasePosition::PrePhase);
#endif // DUMP_FLOWGRAPHS
}

//------------------------------------------------------------------------
// PostPhase: perform dumps and checks after a phase executes
//
// Arguments:
//    status - status from the DoPhase call for this phase
//
void Phase::PostPhase(PhaseStatus status)
{
    comp->EndPhase(m_phase);

#ifdef DEBUG

    // Don't dump or check post phase unless the phase made changes.
    const bool        madeChanges   = (status != PhaseStatus::MODIFIED_NOTHING);
    const char* const statusMessage = madeChanges ? "" : " [no changes]";
    bool              doPostPhase   = false;

    // To help in the incremental conversion of jit activity to phases
    // without greatly increasing dump size or checked jit time, we
    // currently allow the phases that do post-phase checks and
    // dumps via the phase object, and not via explicit calls from
    // the various methods in the phase.
    //
    // As we remove the explicit checks and dumps from each phase, we
    // will add to this list; once all phases are updated, we can
    // remove the list entirely.
    //
    // This list includes custom derivations from the Phase class as
    // well as the new-style phases that have been updated to return
    // PhaseStatus from their DoPhase methods.
    //
    // clang-format off

    static Phases s_allowlist[] = {
        // pre import
        PHASE_INCPROFILE,
        PHASE_IBCPREP,
        PHASE_IMPORTATION,
        PHASE_PATCHPOINTS,
        PHASE_IBCINSTR,
        PHASE_INDXCALL,
        // post import
        // morph init
        PHASE_MORPH_INLINE,
        PHASE_ALLOCATE_OBJECTS,
        // add internal
        PHASE_EMPTY_TRY,
        PHASE_EMPTY_FINALLY,
        PHASE_MERGE_FINALLY_CHAINS,
        PHASE_CLONE_FINALLY,
        // finally flags
        // compute preds
        PHASE_MERGE_THROWS,
        // early fg update
        // promote structs
        // mark addr exposed locals
        PHASE_FWD_SUB,
        // morph implicit byref
        //
        // (enable all phase checks)
        //
        PHASE_MORPH_GLOBAL,
        // gs cookie
        // compute edge weights
        // create funclets
        PHASE_INVERT_LOOPS,
        PHASE_OPTIMIZE_FLOW,
        // reachability
        // block weights
        PHASE_FIND_LOOPS,
        PHASE_CLONE_LOOPS,
        PHASE_UNROLL_LOOPS,
        PHASE_CLEAR_LOOP_INFO,
        PHASE_MARK_LOCAL_VARS,
        PHASE_OPTIMIZE_ADD_COPIES,
        PHASE_OPTIMIZE_BOOLS,
        PHASE_FIND_OPER_ORDER,
        PHASE_SET_BLOCK_ORDER,
        PHASE_BUILD_SSA,
        // (ssa subphases)
        PHASE_EARLY_PROP,
        PHASE_VALUE_NUMBER,
        PHASE_HOIST_LOOP_CODE,
        // copy prop
        // PHASE_OPTIMIZE_BRANCHES,
        // cse
        // assertion prop
        // range check
        // update flow
        // edge weights 2
        PHASE_INSERT_GC_POLLS,
        PHASE_OPTIMIZE_LAYOUT,
        // first cold block
        PHASE_RATIONALIZE,
        PHASE_LOWERING,
        // lsra
        PHASE_STACK_LEVEL_SETTER
        // align loops
        // codegen
    };

    // clang-format on

    // Also note when this phase has not opted into the active post phase checks.
    //
    const bool  doPostPhaseChecks = comp->activePhaseChecks == PhaseChecks::CHECK_ALL;
    const char* checkMessage =
        madeChanges && doPostPhaseChecks ? " [phase has not yet enabled common post phase checks]" : "";

    if (madeChanges)
    {
        for (size_t i = 0; i < sizeof(s_allowlist) / sizeof(Phases); i++)
        {
            if (m_phase == s_allowlist[i])
            {
                doPostPhase  = true;
                checkMessage = "";
                break;
            }
        }
    }

    if (VERBOSE)
    {
        if (comp->compIsForInlining())
        {
            printf("\n*************** Inline @[%06u] Finishing PHASE %s%s%s\n",
                   Compiler::dspTreeID(comp->impInlineInfo->iciCall), m_name, statusMessage, checkMessage);
        }
        else
        {
            printf("\n*************** Finishing PHASE %s%s%s\n", m_name, statusMessage, checkMessage);
        }

        if (doPostPhase)
        {
            printf("Trees after %s\n", m_name);
            comp->fgDispBasicBlocks(true);
        }
    }

    if (doPostPhase && doPostPhaseChecks)
    {
        comp->fgDebugCheckBBlist();
        comp->fgDebugCheckLinks();
        comp->fgDebugCheckNodesUniqueness();
        comp->fgVerifyHandlerTab();
        comp->fgDebugCheckLoopTable();
    }

    // Optionally check profile data, if we have any.
    //
    // There's no point checking until we've built pred lists, as
    // we can't easily reason about consistency without them.
    //
    // Bypass the "doPostPhase" filter until we're sure all
    // phases that mess with profile counts set their phase status
    // appropriately.
    //
    if ((JitConfig.JitProfileChecks() > 0) && comp->fgHaveProfileData() && comp->fgComputePredsDone)
    {
        comp->fgDebugCheckProfileData();
    }

#endif // DEBUG

#if DUMP_FLOWGRAPHS
    comp->fgDumpFlowGraph(m_phase, Compiler::PhasePosition::PostPhase);
#endif // DUMP_FLOWGRAPHS
}
