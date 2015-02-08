//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*****************************************************************************/
#ifndef _PHASE_H_
#define _PHASE_H_

class Phase
{
public:
    Phase(Compiler *_comp, 
          const char *_name, 
          Phases _phase=PHASE_NUMBER_OF) 
        : comp(_comp), name(_name), phase(_phase) {}
    virtual void Run();
    virtual void PrePhase();
    virtual void DoPhase() = 0;
    virtual void PostPhase();

protected:
    Compiler *comp;
    const char *name;
    Phases phase;
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

    if (comp->expensiveDebugCheckLevel >= 2)
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
    comp->fgDebugCheckBBlist();
    comp->fgDebugCheckLinks();
#endif // DEBUG

}

#endif /* End of _PHASE_H_ */
