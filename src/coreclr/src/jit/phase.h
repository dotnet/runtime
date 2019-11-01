// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef _PHASE_H_
#define _PHASE_H_

class Phase
{
public:
    virtual void Run();

protected:
    Phase(Compiler* _comp, const char* _name, Phases _phase = PHASE_NUMBER_OF)
        : comp(_comp), name(_name), phase(_phase), doChecks(true)
    {
    }

    virtual void PrePhase();
    virtual void DoPhase() = 0;
    virtual void PostPhase();

    Compiler*   comp;
    const char* name;
    Phases      phase;
    bool        doChecks;
};

inline void Phase::Run()
{
    PrePhase();
    DoPhase();
    PostPhase();
}

inline void Phase::PrePhase()
{
#ifdef DEBUG
    if (VERBOSE)
    {
        printf("*************** In %s\n", name);
        printf("Trees before %s\n", name);
        comp->fgDispBasicBlocks(true);
    }

    if (doChecks && comp->expensiveDebugCheckLevel >= 2)
    {
        // If everyone used the Phase class, this would duplicate the PostPhase() from the previous phase.
        // But, not everyone does, so go ahead and do the check here, too.
        comp->fgDebugCheckBBlist();
        comp->fgDebugCheckLinks();
    }
#endif // DEBUG
}

inline void Phase::PostPhase()
{
#ifdef DEBUG
    if (VERBOSE)
    {
        printf("*************** Exiting %s\n", name);
        printf("Trees after %s\n", name);
        comp->fgDispBasicBlocks(true);
    }
#endif // DEBUG

    if (phase != PHASE_NUMBER_OF)
    {
        comp->EndPhase(phase);
    }

#ifdef DEBUG
    if (doChecks)
    {
        comp->fgDebugCheckBBlist();
        comp->fgDebugCheckLinks();
    }
#endif // DEBUG
}

#endif /* End of _PHASE_H_ */
