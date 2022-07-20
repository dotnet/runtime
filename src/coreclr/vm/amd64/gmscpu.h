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

#ifndef __gmsAMD64_h__
#define __gmsAMD64_h__

#ifdef _DEBUG
class HelperMethodFrame;
struct MachState;
EXTERN_C MachState* __stdcall HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal);
#endif // _DEBUG

// A MachState indicates the register state of the processor at some point in time (usually
// just before or after a call is made).  It can be made one of two ways.  Either explicitly
// (when you for some reason know the values of all the registers), or implicitly using the
// GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState
{
    MachState()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        INDEBUG(memset(this, 0xCC, sizeof(MachState));)
    }

    bool   isValid()    { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pRetAddr) != INVALID_POINTER_CC); return(_pRetAddr != 0); }
    TADDR* pRetAddr()   { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_pRetAddr); }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return *_pRetAddr; }
#ifndef DACCESS_COMPILE
    void SetRetAddr(TADDR* addr) { _ASSERTE(isValid()); _pRetAddr = addr; }
#endif

    friend class HelperMethodFrame;
    friend class CheckAsmOffsets;
    friend struct LazyMachState;
#ifdef _DEBUG
    friend MachState* __stdcall HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal);
#endif

protected:
    PCODE m_Rip;
    TADDR m_Rsp;

    //
    // These "capture" fields are READ ONLY once initialized by
    // LazyMachStateCaptureState because we are racing to update
    // the MachState when we do a stackwalk so, we must not update
    // any state used to initialize the unwind from the captured
    // state to the managed caller.
    //
    // Note also, that these fields need to be in the base struct
    // because the context pointers below may point up to these
    // fields.
    //
    CalleeSavedRegisters m_Capture;

    // context pointers for preserved registers
    CalleeSavedRegistersPointers m_Ptrs;

    PTR_TADDR _pRetAddr;

#ifdef TARGET_UNIX
    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, preserve
    // the unwound values.
    CalleeSavedRegisters m_Unwound;
#endif
};

/********************************************************************/
/* This allows you to defer the computation of the Machine state
   until later.  Note that we don't reuse slots, because we want
   this to be threadsafe without locks */

EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState);

typedef DPTR(struct LazyMachState) PTR_LazyMachState;
struct LazyMachState : public MachState
{
    // compute the machine state of the processor as it will exist just
    // after the return after at most'funCallDepth' number of functions.
    // if 'testFtn' is non-NULL, the return address is tested at each
    // return instruction encountered.  If this test returns non-NULL,
    // then stack walking stops (thus you can walk up to the point that the
    // return address matches some criteria

    // Normally this is called with funCallDepth=1 and testFtn = 0 so that
    // it returns the state of the processor after the function that called 'captureState()'
    void setLazyStateFromUnwind(MachState* copy);
    static void unwindLazyState(LazyMachState* baseState,
                                MachState* lazyState,
                                DWORD threadId,
                                int funCallDepth = 1,
                                HostCallPreference hostCallPreference = AllowHostCalls);

    friend class HelperMethodFrame;
    friend class CheckAsmOffsets;

    //
    // These "capture" fields are READ ONLY once initialized by
    // LazyMachStateCaptureState because we are racing to update
    // the MachState when we do a stackwalk so, we must not update
    // any state used to initialize the unwind from the captured
    // state to the managed caller.
    //
    ULONG64 m_CaptureRip;
    ULONG64 m_CaptureRsp;
};

inline void LazyMachState::setLazyStateFromUnwind(MachState* copy)
{
    LIMITED_METHOD_CONTRACT;

#if defined(DACCESS_COMPILE)
    // This function cannot be called in DAC because DAC cannot update target memory.
    DacError(E_FAIL);
    return;

#else  // !DACCESS_COMPILE
    this->m_Rip = copy->m_Rip;
    this->m_Rsp = copy->m_Rsp;

#ifdef TARGET_UNIX
    this->m_Unwound = copy->m_Unwound;
#endif

    // Capture* has already been set, so there is no need to touch it

    // loop over the nonvolatile context pointers and make
    // sure to properly copy interior pointers into the
    // new struct

    PULONG64* pSrc = (PULONG64 *)&copy->m_Ptrs;
    PULONG64* pDst = (PULONG64 *)&this->m_Ptrs;

    const PULONG64 LowerBoundDst = (PULONG64) this;
    const PULONG64 LowerBoundSrc = (PULONG64) copy;

    const PULONG64 UpperBoundSrc = (PULONG64) ((BYTE*)LowerBoundSrc + sizeof(*copy));

    for (int i = 0; i < NUM_CALLEE_SAVED_REGISTERS; i++)
    {
        PULONG64 valueSrc = *pSrc++;

        if ((LowerBoundSrc <= valueSrc) && (valueSrc < UpperBoundSrc))
        {
            // make any pointer interior to 'src' interior to 'dst'
            valueSrc = (PULONG64)((BYTE*)valueSrc - (BYTE*)LowerBoundSrc + (BYTE*)LowerBoundDst);
        }

        *pDst++ = valueSrc;
    }

    // this has to be last because we depend on write ordering to
    // synchronize the race implicit in updating this struct
    VolatileStore(&_pRetAddr, (PTR_TADDR)(TADDR)&m_Rip);

#endif // !DACCESS_COMPILE
}

// Do the initial capture of the machine state.  This is meant to be
// as light weight as possible, as we may never need the state that
// we capture.  Thus to complete the process you need to call
// 'getMachState()', which finishes the process
EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState);

// CAPTURE_STATE captures just enough register state so that the state of the
// processor can be deterined just after the routine that has CAPTURE_STATE in
// it returns.

#define CAPTURE_STATE(machState, ret)                           \
    LazyMachStateCaptureState(machState)

#endif // __gmsAMD64_h__
