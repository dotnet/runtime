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

// A MachState indicates the register state of the processor at some point in time (usually
// just before or after a call is made).  It can be made one of two ways.  Either explicitly
// (when you for some reason know the values of all the registers), or implicitly using the
// GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState {

    BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return _isValid; }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return _pc; }

    friend class CheckAsmOffsets;


protected:
    // The simplest way to understand the relationship between capturedR4_R11 (registers
	// representing the captured state) and _R4_R11 (pointers to registers representing
	// preserved state) is as follows:
	//
	// 1) LazyMachState::unwindLazyState is invoked by HelperMethodFrame to initialize the captured
	//    state. It then performs an unwind and copies the register pointers to _R4_R11.
	//
	// 2) HelperMethodFrame::UpdateRegdisplay is invoked by our StackWalker that initializes
	//    the regdisplay with the updated register state.

    TADDR      captureR4_R11[8];  // Registers R4..R11 at the time of capture

    PTR_DWORD     _R4_R11[8];  // Preserved registers

    TADDR     _pc;        // program counter after the function returns
    TADDR     _sp;        // stack pointer after the function returns

    BOOL      _isValid;
};

#endif
