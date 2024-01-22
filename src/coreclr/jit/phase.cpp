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
    if (VERBOSE)
    {
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

#if DUMP_FLOWGRAPHS
    comp->fgDumpFlowGraph(m_phase, Compiler::PhasePosition::PostPhase);
#endif // DUMP_FLOWGRAPHS

    // Don't dump or check post phase unless the phase made changes.
    //
    const bool madeChanges       = (status != PhaseStatus::MODIFIED_NOTHING);
    const bool doPostPhase       = madeChanges;
    const bool doPostPhaseChecks = (comp->activePhaseChecks != PhaseChecks::CHECK_NONE);
    const bool doPostPhaseDumps  = (comp->activePhaseDumps == PhaseDumps::DUMP_ALL);

    const char* const statusMessage = madeChanges ? "" : " [no changes]";

    if (VERBOSE)
    {
        if (comp->compIsForInlining())
        {
            printf("\n*************** Inline @[%06u] Finishing PHASE %s%s\n",
                   Compiler::dspTreeID(comp->impInlineInfo->iciCall), m_name, statusMessage);
        }
        else
        {
            printf("\n*************** Finishing PHASE %s%s\n", m_name, statusMessage);
        }

        if (doPostPhase && doPostPhaseDumps)
        {
            printf("Trees after %s\n", m_name);
            comp->fgDispBasicBlocks(true);
        }
    }

    if (doPostPhase && doPostPhaseChecks)
    {
        if ((comp->activePhaseChecks & PhaseChecks::CHECK_UNIQUE) == PhaseChecks::CHECK_UNIQUE)
        {
            comp->fgDebugCheckNodesUniqueness();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_FG) == PhaseChecks::CHECK_FG)
        {
            comp->fgDebugCheckBBlist();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_IR) == PhaseChecks::CHECK_IR)
        {
            comp->fgDebugCheckLinks();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_EH) == PhaseChecks::CHECK_EH)
        {
            comp->fgVerifyHandlerTab();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_LOOPS) == PhaseChecks::CHECK_LOOPS)
        {
            comp->fgDebugCheckLoops();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_PROFILE) == PhaseChecks::CHECK_PROFILE)
        {
            comp->fgDebugCheckProfileWeights();
        }

        if ((comp->activePhaseChecks & PhaseChecks::CHECK_LINKED_LOCALS) == PhaseChecks::CHECK_LINKED_LOCALS)
        {
            comp->fgDebugCheckLinkedLocals();
        }

        if (comp->m_dfsTree != nullptr)
        {
            comp->fgDebugCheckDfsTree();
        }
    }
#endif // DEBUG
}
