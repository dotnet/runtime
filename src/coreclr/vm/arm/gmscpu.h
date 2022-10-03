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

#ifdef _DEBUG
class HelperMethodFrame;
struct MachState;
EXTERN_C MachState* __stdcall HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal);
#endif

    // A MachState indicates the register state of the processor at some point in time (usually
    // just before or after a call is made).  It can be made one of two ways.  Either explicitly
    // (when you for some reason know the values of all the registers), or implicitly using the
    // GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState {

    BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return _isValid; }
    TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return _pc; }

    friend class HelperMethodFrame;
    friend class CheckAsmOffsets;
    friend struct LazyMachState;


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
	//
	// 3) HelperMethodFrameRestoreState is invoked when the HMF state machine exits and it
	//    restores the values of unmodified registers.

    TADDR      captureR4_R11[8];  // Registers R4..R11 at the time of capture

    PTR_DWORD     _R4_R11[8];  // Preserved registers

    TADDR     _pc;        // program counter after the function returns
    TADDR     _sp;        // stack pointer after the function returns

    BOOL      _isValid;
};

/********************************************************************/
/* This allows you to defer the computation of the Machine state
   until later.  Note that we don't reuse slots, because we want
   this to be threadsafe without locks */

struct LazyMachState : public MachState {
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
private:
    TADDR            captureSp;         // Stack pointer at the time of capture
    TADDR            captureIp;         // Instruction pointer at the time of capture
};

// R4 - R11
#define NUM_NONVOLATILE_CONTEXT_POINTERS 8

inline void LazyMachState::setLazyStateFromUnwind(MachState* copy)
{
    LIMITED_METHOD_CONTRACT;

#if defined(DACCESS_COMPILE)
    // This function cannot be called in DAC because DAC cannot update target memory.
    DacError(E_FAIL);
    return;

#else  // !DACCESS_COMPILE
    this->_pc = copy->_pc;
    this->_sp = copy->_sp;

    // Capture* has already been set, so there is no need to touch it.
    // This was setup in LazyMachState::unwindLazyState just before we
    // called into the OS for unwind.

    // Prepare to loop over the nonvolatile context pointers for and
    // make sure to properly copy interior pointers into the new struct.

    PDWORD* pSrc = &copy->_R4_R11[0];
    PDWORD* pDst = &this->_R4_R11[0];

    const PDWORD LowerBoundDst = (PDWORD) this;
    const PDWORD LowerBoundSrc = (PDWORD) copy;

    // Calculate the upperbound till which we need to loop (i.e. the highest address till
    // which we have saved non-volatile pointers).
    const PDWORD UpperBoundSrc = (PDWORD) (((BYTE*)LowerBoundSrc) + offsetof(LazyMachState, _pc));

#ifdef _DEBUG
    int count = 0;
#endif // _DEBUG

    while (((PDWORD)pSrc) < UpperBoundSrc)
    {
#ifdef _DEBUG
        count++;
#endif // _DEBUG

        PDWORD valueSrc = *pSrc++;

        // If any non-volatile register pointer is pointing to the corresponding register field
        // in the MachState, then make the corresponding pointer in "this" MachState point
        // to the corresponding field.
        if ((LowerBoundSrc <= valueSrc) && (valueSrc < UpperBoundSrc))
        {
            valueSrc = (PDWORD)((BYTE*)valueSrc - (BYTE*)LowerBoundSrc + (BYTE*)LowerBoundDst);
        }

        *pDst++ = valueSrc;
    }

    CONSISTENCY_CHECK_MSGF(count == NUM_NONVOLATILE_CONTEXT_POINTERS, ("count != NUM_NONVOLATILE_CONTEXT_POINTERS, actually = %d", count));

    // this has to be last because we depend on write ordering to
    // synchronize the race implicit in updating this struct
    VolatileStore(&_isValid, TRUE);

#endif // !DACCESS_COMPILE

}
typedef DPTR(LazyMachState) PTR_LazyMachState;

// Do the initial capture of the machine state.  This is meant to be
// as light weight as possible, as we may never need the state that
// we capture.  Thus to complete the process you need to call
// 'getMachState()', which finishes the process
EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState);

// CAPTURE_STATE captures just enough register state so that the state of the
// processor can be deterined just after the routine that has CAPTURE_STATE in
// it returns.

#define CAPTURE_STATE(machState, ret)                       \
    LazyMachStateCaptureState(machState)

#endif
