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

// X19 - X29
#define NUM_NONVOLATILE_CONTEXT_POINTERS 11

struct MachState {
    ULONG64        captureX19_X29[NUM_NONVOLATILE_CONTEXT_POINTERS]; // preserved registers
    PTR_ULONG64    ptrX19_X29[NUM_NONVOLATILE_CONTEXT_POINTERS]; // pointers to preserved registers
    TADDR          _pc; // program counter after the function returns
    TADDR          _sp; // stack pointer after the function returns
    BOOL           _isValid;
#ifdef __APPLE__
    // libunwind on macOS doesn't support context pointers and we cannot modify the captureX19_X29,
    // so we store the unwound values in a separate array.
    ULONG64        unwoundX19_X29[NUM_NONVOLATILE_CONTEXT_POINTERS]; // preserved registers
#endif // __APPLE__

    BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return _isValid; }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return _pc; }
};

#endif
