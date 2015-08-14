//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// State machine header used ONLY in the JIT.
//

#ifndef __sm_h__
#define __sm_h__

#include "smcommon.h"

extern const SMState *       gp_SMStates;
extern const JumpTableCell * gp_SMJumpTableCells;
extern const short *         gp_StateWeights;

class   CodeSeqSM     // Represent a particualr run of the state machine
                      // For example, it maintains the array of counts for the terminated states.
                      // These counts should be stored in per method based for them to be correct
                      // under multithreadeded environment.
{
public :

    Compiler            * pComp;

    const SMState       * States;
    const JumpTableCell * JumpTableCells; 
    const short         * StateWeights;     // Weight for each state. Including non-terminate states.

    SM_STATE_ID         curState;

    int                 NativeSize;         // This is a signed integer!
       
    void                Start(Compiler  * comp);
    void                Reset();
    void                End();
    void                Run(SM_OPCODE opcode DEBUGARG(int level));       

    SM_STATE_ID         GetDestState(SM_STATE_ID srcState, SM_OPCODE opcode);

    // Matched a termination state
    inline void         TermStateMatch(SM_STATE_ID stateID DEBUGARG(bool verbose))
    {
        assert(States[stateID].term);    
        assert(StateMatchedCounts[stateID] < _UI16_MAX);
#ifdef DEBUG    
        ++StateMatchedCounts[stateID]; 
#ifndef SMGEN_COMPILE 
        if (verbose)
        {
            printf("weight=%3d : state %3d [ %s ]\n", StateWeights[stateID], stateID, StateDesc(stateID));            
        }
#endif // SMGEN_COMPILE        
#endif // DEBUG

        NativeSize += StateWeights[stateID];
    }

    // Given an SM opcode retrieve the weight for this single opcode state.
    // For example, ID for single opcode state SM_NOSHOW is 2.
    inline short        GetWeightForOpcode(SM_OPCODE opcode)
    {
        SM_STATE_ID stateID = ((SM_STATE_ID)opcode) + SM_STATE_ID_START + 1;        
        return StateWeights[stateID];
    }

#ifdef DEBUG

    WORD                StateMatchedCounts[NUM_SM_STATES]; // counts of matched states. 
    
    bool                b0Args, b1Args, b2Args, b3AndMoreArgs, bNoLocals, bNoCalls;

    size_t              codeSize;         // The size of the code including prolog and epilogs.
    size_t              BBCodeSize;       // The size of the code excluding prolog and epilogs.
    unsigned            prologSize;    
    unsigned            epilogSize;
    unsigned            epilogCount;
    unsigned            instrCount; 

    const char        * StateDesc(SM_STATE_ID stateID);
    void                PrintSampleHeader();
    void                PrintSampleResult();

    void                Test();
    void                TestSeq(SM_OPCODE * CodeSeq);
#endif

    static SM_OPCODE    MapToSMOpcode(OPCODE opcode);

};


#endif /* __sm_h__ */


