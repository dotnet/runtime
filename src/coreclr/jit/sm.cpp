// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                 State machine used in the JIT                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "smcommon.cpp"

//
// The array to map from EE opcodes (i.e. CEE_ ) to state machine opcodes (i.e. SM_ )
//
const SM_OPCODE smOpcodeMap[] = {
#define OPCODEMAP(eename, eestring, smname) smname,
#include "smopcodemap.def"
#undef OPCODEMAP
};

// ????????? How to make this method inlinable, since it refers to smOpcodeMap????
/* static */ SM_OPCODE CodeSeqSM::MapToSMOpcode(OPCODE opcode)
{
    assert(opcode < CEE_COUNT);

    SM_OPCODE smOpcode = smOpcodeMap[opcode];
    assert(smOpcode < SM_COUNT);
    return smOpcode;
}

void CodeSeqSM::Start(Compiler* comp)
{
    pComp          = comp;
    States         = gp_SMStates;
    JumpTableCells = gp_SMJumpTableCells;
    StateWeights   = gp_StateWeights;
    NativeSize     = 0;

    Reset();
}

void CodeSeqSM::Reset()
{
    curState = SM_STATE_ID_START;
}

void CodeSeqSM::End()
{
    if (States[curState].term)
    {
        TermStateMatch(curState DEBUGARG(pComp->verbose));
    }
}

void CodeSeqSM::Run(SM_OPCODE opcode DEBUGARG(int level))
{
    SM_STATE_ID nextState;
    SM_STATE_ID rollbackState;

    SM_OPCODE opcodesToRevisit[MAX_CODE_SEQUENCE_LENGTH];

    assert(level <= MAX_CODE_SEQUENCE_LENGTH);

_Next:
    nextState = GetDestState(curState, opcode);

    if (nextState != 0)
    {
        // This is easy, Just go to the next state.
        curState = nextState;
        return;
    }

    assert(curState != SM_STATE_ID_START);

    if (States[curState].term)
    {
        TermStateMatch(curState DEBUGARG(pComp->verbose));
        curState = SM_STATE_ID_START;
        goto _Next;
    }

    // This is hard. We need to rollback to the longest matched term state and restart from there.

    rollbackState = States[curState].longestTermState;
    TermStateMatch(rollbackState DEBUGARG(pComp->verbose));

    assert(States[curState].length > States[rollbackState].length);

    unsigned numOfOpcodesToRevisit = States[curState].length - States[rollbackState].length + 1;
    assert(numOfOpcodesToRevisit > 1 &&
           numOfOpcodesToRevisit <= MAX_CODE_SEQUENCE_LENGTH); // So it can fit in the local array opcodesToRevisit[]

    SM_OPCODE* p = opcodesToRevisit + (numOfOpcodesToRevisit - 1);

    *p = opcode;

    // Fill in the local array:
    for (unsigned i = 0; i < numOfOpcodesToRevisit - 1; ++i)
    {
        *(--p)   = States[curState].opc;
        curState = States[curState].prevState;
    }

    assert(curState == rollbackState);

    // Now revisit these opcodes, starting from SM_STATE_ID_START.
    curState = SM_STATE_ID_START;
    for (p = opcodesToRevisit; p < opcodesToRevisit + numOfOpcodesToRevisit; ++p)
    {
        Run(*p DEBUGARG(level + 1));
    }
}

SM_STATE_ID CodeSeqSM::GetDestState(SM_STATE_ID srcState, SM_OPCODE opcode)
{
    assert(opcode < SM_COUNT);

    JumpTableCell* pThisJumpTable = (JumpTableCell*)(((PBYTE)JumpTableCells) + States[srcState].jumpTableByteOffset);

    JumpTableCell* cell = pThisJumpTable + opcode;

    if (cell->srcState != srcState)
    {
        assert(cell->srcState == 0 ||
               cell->srcState != srcState); // Either way means there is not outgoing edge from srcState.
        return 0;
    }
    else
    {
        return cell->destState;
    }
}

#ifdef DEBUG

const char* CodeSeqSM::StateDesc(SM_STATE_ID stateID)
{
    static char      s_StateDesc[500];
    static SM_OPCODE s_StateDescOpcodes[MAX_CODE_SEQUENCE_LENGTH];

    if (stateID == 0)
    {
        return "invalid";
    }
    if (stateID == SM_STATE_ID_START)
    {
        return "start";
    }
    unsigned i = 0;

    SM_STATE_ID b = stateID;

    while (States[b].prevState != 0)
    {
        s_StateDescOpcodes[i] = States[b].opc;
        b                     = States[b].prevState;
        ++i;
    }

    assert(i == States[stateID].length && i > 0);

    *s_StateDesc = 0;

    while (--i > 0)
    {
        strcat(s_StateDesc, smOpcodeNames[s_StateDescOpcodes[i]]);
        strcat(s_StateDesc, " -> ");
    }

    strcat(s_StateDesc, smOpcodeNames[s_StateDescOpcodes[0]]);

    return s_StateDesc;
}

#endif // DEBUG
