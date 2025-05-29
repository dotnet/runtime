// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/*                       gmscpu.h                             */
/**************************************************************/
/* HelperFrame is defines 'GET_STATE(machState)' macro, which
   figures out what the state of the machine will be when the
   current method returns.  It then stores the state in the
   JIT_machState structure.  */

/**************************************************************/

#ifndef __gmscpu_h__
#define __gmscpu_h__

#define __gmscpu_h__

// CalleeSaveRegisters
#define NUM_NONVOLATILE_CONTEXT_POINTERS 14

struct MachState {
    ULONG64        captureCalleeSavedRegisters[NUM_NONVOLATILE_CONTEXT_POINTERS]; // preserved registers
    PTR_ULONG64    ptrCalleeSavedRegisters[NUM_NONVOLATILE_CONTEXT_POINTERS]; // pointers to preserved registers
    TADDR          _pc; // program counter after the function returns
    TADDR          _sp; // stack pointer after the function returns
    BOOL           _isValid;

    BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return _isValid; }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return _pc; }
};

#endif
