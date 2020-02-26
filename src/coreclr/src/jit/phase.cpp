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

    // To help in the incremental conversion of jit activity to phases
    // without greatly increasing dump size or checked jit time, we
    // currently whitelist the phases that do pre-phase checks and
    // dumps via the phase object, and not via explicit calls from
    // the various methods in the phase.
    //
    // In the long run the aim is to get rid of all pre-phase checks
    // and dumps, relying instead on post-phase checks and dumps from
    // the preceeding phase.
    //
    // Currently the list is just the set of phases that have custom
    // derivations from the Phase class.
    static Phases s_whitelist[] = {PHASE_ALLOCATE_OBJECTS, PHASE_BUILD_SSA, PHASE_RATIONALIZE, PHASE_LOWERING,
                                   PHASE_STACK_LEVEL_SETTER};
    bool doPrePhase = false;

    for (int i = 0; i < sizeof(s_whitelist) / sizeof(Phases); i++)
    {
        if (m_phase == s_whitelist[i])
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
}

//------------------------------------------------------------------------
// PostPhase: perform dumps and checks after a phase executes
//
void Phase::PostPhase()
{
#ifdef DEBUG

    // To help in the incremental conversion of jit activity to phases
    // without greatly increasing dump size or checked jit time, we
    // currently whitelist the phases that do post-phase checks and
    // dumps via the phase object, and not via explicit calls from
    // the various methods in the phase.
    //
    // As we remove the explicit checks and dumps from each phase, we
    // will add to thist list; once all phases are updated, we can
    // remove the list entirely.
    //
    // Currently the list is just the set of phases that have custom
    // derivations from the Phase class.
    static Phases s_whitelist[] = {PHASE_ALLOCATE_OBJECTS, PHASE_BUILD_SSA, PHASE_RATIONALIZE, PHASE_LOWERING,
                                   PHASE_STACK_LEVEL_SETTER};
    bool doPostPhase = false;

    for (int i = 0; i < sizeof(s_whitelist) / sizeof(Phases); i++)
    {
        if (m_phase == s_whitelist[i])
        {
            doPostPhase = true;
            break;
        }
    }

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

        if (doPostPhase)
        {
            printf("Trees after %s\n", m_name);
            comp->fgDispBasicBlocks(true);
        }

#if DUMP_FLOWGRAPHS
        comp->fgDumpFlowGraph(m_phase);
#endif // DUMP_FLOWGRAPHS
    }

    if (doPostPhase)
    {
        if (comp->activePhaseChecks == PhaseChecks::CHECK_ALL)
        {
            comp->fgDebugCheckBBlist();
            comp->fgDebugCheckLinks();
            comp->fgDebugCheckNodesUniqueness();
        }
    }

#endif // DEBUG

    comp->EndPhase(m_phase);
}
