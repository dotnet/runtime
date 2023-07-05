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

// S0 - S8, FP, TP
#define NUM_CALLEESAVED_REGISTERS 11

struct MachState {
    ULONG64        captureCalleeSavedRegisters[NUM_CALLEESAVED_REGISTERS]; // preserved registers
    PTR_ULONG64    ptrCalleeSavedRegisters[NUM_CALLEESAVED_REGISTERS]; // pointers to preserved registers
    TADDR          _pc; // program counter after the function returns
    TADDR          _sp; // stack pointer after the function returns
    BOOL           _isValid;

    BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return _isValid; }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return _pc; }
};

struct LazyMachState : public MachState{

    TADDR          captureSp;         // Stack pointer at the time of capture
    TADDR          captureIp;         // Instruction pointer at the time of capture

    void setLazyStateFromUnwind(MachState* copy);
    static void unwindLazyState(LazyMachState* baseState,
                                MachState* lazyState,
                                DWORD threadId,
                                int funCallDepth = 1,
                                HostCallPreference hostCallPreference = AllowHostCalls);
};

inline void LazyMachState::setLazyStateFromUnwind(MachState* copy)
{
#if defined(DACCESS_COMPILE)
    // This function cannot be called in DAC because DAC cannot update target memory.
    DacError(E_FAIL);
    return;

#else  // !DACCESS_COMPILE

    _sp = copy->_sp;
    _pc = copy->_pc;

    // Capture* has already been set, so there is no need to touch it

    // loop over the nonvolatile context pointers and make
    // sure to properly copy interior pointers into the
    // new struct

    PULONG64* pSrc = (PULONG64 *)&copy->ptrCalleeSavedRegisters;
    PULONG64* pDst = (PULONG64 *)&this->ptrCalleeSavedRegisters;

    const PULONG64 LowerBoundDst = (PULONG64) this;
    const PULONG64 LowerBoundSrc = (PULONG64) copy;

    const PULONG64 UpperBoundSrc = (PULONG64) ((BYTE*)LowerBoundSrc + sizeof(*copy));

    for (int i = 0; i < NUM_CALLEESAVED_REGISTERS; i++)
    {
        PULONG64 valueSrc = *pSrc++;

        if ((LowerBoundSrc <= valueSrc) && (valueSrc < UpperBoundSrc))
        {
            // make any pointer interior to 'src' interior to 'dst'
            valueSrc = (PULONG64)((BYTE*)valueSrc - (BYTE*)LowerBoundSrc + (BYTE*)LowerBoundDst);
        }

        *pDst++ = valueSrc;
        captureCalleeSavedRegisters[i] = copy->captureCalleeSavedRegisters[i];
    }


    // this has to be last because we depend on write ordering to
    // synchronize the race implicit in updating this struct
    VolatileStore(&_isValid, TRUE);
#endif // DACCESS_COMPILE
}

// Do the initial capture of the machine state.  This is meant to be
// as light weight as possible, as we may never need the state that
// we capture.
EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState);

#define CAPTURE_STATE(machState, ret)                       \
    LazyMachStateCaptureState(machState)


#endif
