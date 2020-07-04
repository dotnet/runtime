// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Common headers used both in smgen.exe and the JIT.
//

#ifndef __sm_common_h__
#define __sm_common_h__

#include "smopenum.h"

#define NUM_SM_STATES 250

typedef BYTE SM_STATE_ID;

static_assert_no_msg(sizeof(SM_STATE_ID) == 1); // To conserve memory, we don't want to have more than 256 states.

#define SM_STATE_ID_START 1

static_assert_no_msg(SM_STATE_ID_START == 1); // Make sure nobody changes it. We rely on this to map the SM_OPCODE
                                              // to single-opcode states. For example, in GetWeightForOpcode().

struct JumpTableCell
{
    SM_STATE_ID srcState;
    SM_STATE_ID destState;
};

struct SMState
{
    bool        term;             // does this state terminate a code sequence?
    BYTE        length;           // the length of currently matched opcodes
    SM_STATE_ID longestTermState; // the ID of the longest matched terminate state

    SM_STATE_ID prevState; // previous state
    SM_OPCODE   opc;       // opcode that leads from the previous state to current state

    unsigned short jumpTableByteOffset;
};

//
// Code sequences
//

#define MAX_CODE_SEQUENCE_LENGTH 7
#define CODE_SEQUENCE_END ((SM_OPCODE)(SM_COUNT + 1))

#endif /* __sm_common_h__ */
