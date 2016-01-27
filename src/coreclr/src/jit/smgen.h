// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __smgen_h__
#define __smgen_h__

#include <windows.h>
#include <assert.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>

#include "smcommon.h"

#define MAX_NUM_STATES   (1 << (sizeof(SM_STATE_ID)*8))
#define MAX_CELL_COUNT   420
#define SM_BUFFER_SIZE   (sizeof(SMState)*MAX_NUM_STATES + sizeof(JumpTableCell)*MAX_CELL_COUNT)   // Change to a formular of  MAX_NUM_STATES and MAX_CELL_COUNT

class SMGen;  // Forward declaration of SMGen

// The struct representing an edge in the state machine.
struct SMGenDestStateDesc
{
    SMGenDestStateDesc *   next;      // Linkage
    SM_OPCODE              opcode;    
    SM_STATE_ID            destState;
};

// The struct representing a state in a state machine.
class  SMGenState
{
public:

    SM_STATE_ID   id;                      // the ID of the state
    bool          term;                    // does this state terminate a code sequence?    
    BYTE          length;                  // the length of currently matched opcodes
    SM_STATE_ID   longestTermState;        // the ID of the longest matched terminate state

    SM_STATE_ID   prevState;               // previous state
    SM_OPCODE     opc;                     // opcode that leads from the previous state to current state

    SMGenDestStateDesc * destStateList;    // the list of outgoing edges

    // Given an opcode, get the next state the machine will jump to.
    SM_STATE_ID   getDestState(SM_OPCODE opcode); 

    // Given an opcode, set where the next state the machine should jump to.
    void          setDestState(SM_OPCODE opcode, SM_STATE_ID destState); 
};

// The state machine generator
class   SMGen     
{
public :

    SM_STATE_ID      lastStateID; 
    SMGenState       States[MAX_NUM_STATES];  

    SMState *        pAllStates;
    JumpTableCell *  pAllJumpTables;
    JumpTableCell *  pJumpTableMax;
    unsigned         totalCellNeeded;

    unsigned         emitBufferSize;    
    BYTE             emitBuffer[SM_BUFFER_SIZE];
    
    SMGen();
    ~SMGen();

    void           ProcessSeq(SM_OPCODE * CodeSeq);
    void           Emit();
    void           EmitDone();
    unsigned short JumpTableSet(SM_STATE_ID id);
    bool           bJumpTableFit(JumpTableCell * pTable, SM_STATE_ID id);
    void           JumpTableFill(JumpTableCell * pTable, SM_STATE_ID id);
    char *         StateDesc(SM_STATE_ID stateID);   
};

#endif /* __smgen_h__ */

