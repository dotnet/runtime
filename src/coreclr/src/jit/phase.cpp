// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "phase.h"

//------------------------------------------------------------------------
// Run: execute a phase and any before and after actions
//
void Phase::Run()
{
    PrePhase();
    DoPhase();
    PostPhase();
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

        printf("Trees before %s\n", m_name);
        comp->fgDispBasicBlocks(true);
    }

    if ((comp->activePhaseChecks == PhaseChecks::CHECK_ALL) && (comp->expensiveDebugCheckLevel >= 2))
    {
        // If everyone used the Phase class, this would duplicate the PostPhase() from the previous phase.
        // But, not everyone does, so go ahead and do the check here, too.
        comp->fgDebugCheckBBlist();
        comp->fgDebugCheckLinks();
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// PostPhase: perform dumps and checks after a phase executes
//
void Phase::PostPhase()
{
#ifdef DEBUG
    if (VERBOSE)
    {
        if (comp->compIsForInlining())
        {
            printf("\n*************** Inline @[%06u] Finishing PHASE %s\n",
                   Compiler::dspTreeID(comp->impInlineInfo->iciCall), m_name);
        }
        else
        {
            printf("\n*************** Finishing PHASE %s\n", m_name);
        }

        printf("Trees after %s\n", m_name);
        comp->fgDispBasicBlocks(true);
    }
#endif // DEBUG

#if DUMP_FLOWGRAPHS
    comp->fgDumpFlowGraph(m_phase);
#endif // DUMP_FLOWGRAPHS

#ifdef DEBUG
    if (comp->activePhaseChecks == PhaseChecks::CHECK_ALL)
    {
        comp->fgDebugCheckBBlist();
        comp->fgDebugCheckLinks();
        comp->fgDebugCheckNodesUniqueness();
    }
#endif // DEBUG

    comp->EndPhase(m_phase);
}
