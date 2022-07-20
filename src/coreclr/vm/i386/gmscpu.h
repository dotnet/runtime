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

#ifndef __gmsx86_h__
#define __gmsx86_h__

#define __gmsx86_h__

#ifdef _DEBUG
class HelperMethodFrame;
struct MachState;
EXTERN_C MachState* STDCALL HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal);
#endif

    // A MachState indicates the register state of the processor at some point in time (usually
    // just before or after a call is made).  It can be made one of two ways.  Either explicitly
    // (when you for some reason know the values of all the registers), or implicitly using the
    // GET_STATE macros.

typedef DPTR(struct MachState) PTR_MachState;
struct MachState {

    MachState()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        INDEBUG(memset(this, 0xCC, sizeof(MachState));)
    }

    bool   isValid()     { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pRetAddr) != INVALID_POINTER_CC); return(_pRetAddr != 0); }
    TADDR* pEdi()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEdi) != INVALID_POINTER_CC); return(_pEdi); }
    TADDR* pEsi()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEsi) != INVALID_POINTER_CC); return(_pEsi); }
    TADDR* pEbx()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEbx) != INVALID_POINTER_CC); return(_pEbx); }
    TADDR* pEbp()        { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(dac_cast<TADDR>(_pEbp) != INVALID_POINTER_CC); return(_pEbp); }
    TADDR  esp()         { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_esp); }
    PTR_TADDR pRetAddr() { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return(_pRetAddr); }
    TADDR  GetRetAddr()  { LIMITED_METHOD_DAC_CONTRACT; _ASSERTE(isValid()); return *_pRetAddr; }
#ifndef DACCESS_COMPILE
    void SetRetAddr(TADDR* addr) { LIMITED_METHOD_CONTRACT; _ASSERTE(isValid()); _pRetAddr = addr; }
#endif

    friend class HelperMethodFrame;
    friend class CheckAsmOffsets;
    friend struct LazyMachState;
#ifdef _DEBUG
    friend MachState* STDCALL HelperMethodFrameConfirmState(HelperMethodFrame* frame, void* esiVal, void* ediVal, void* ebxVal, void* ebpVal);
#endif


protected:
    // Note the fields are layed out to make generating a
    // MachState structure from assembly code very easy

    // The state of all the callee saved registers.
    // If the register has been spill to the stack p<REG>
    // points at this location, otherwise it points
    // at the field <REG> field itself
    PTR_TADDR _pEdi;
    TADDR     _edi;
    PTR_TADDR _pEsi;
    TADDR     _esi;
    PTR_TADDR _pEbx;
    TADDR     _ebx;
    PTR_TADDR _pEbp;
    TADDR     _ebp;

    TADDR     _esp;       // stack pointer after the function returns
    PTR_TADDR _pRetAddr;  // The address of the stored IP address (points into the stack)
};

/********************************************************************/
/* This allows you to defer the computation of the Machine state
   until later.  Note that we don't reuse slots, because we want
   this to be threadsafe without locks */

struct LazyMachState;
typedef DPTR(LazyMachState) PTR_LazyMachState;
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
    TADDR            captureEbp;        // Ebp at the time of capture
    TADDR            captureEsp;        // Esp at the time of capture
    TADDR            captureEip;        // Eip at the time of capture
};

inline void LazyMachState::setLazyStateFromUnwind(MachState* copy)
{
    // _pRetAddr has to be the last thing updated when we make the copy (because its
    // is the _pRetAddr becoming non-zero that flips this from invalid to valid.
    // we assert that it is the last field in the struct.
    static_assert_no_msg(offsetof(MachState, _pRetAddr) + sizeof(_pRetAddr) == sizeof(MachState));

    memcpy(this, copy, offsetof(MachState, _pRetAddr));

    // this has to be last
    VolatileStore((TADDR*)&_pRetAddr, dac_cast<TADDR>(copy->_pRetAddr));
}

// Do the initial capture of the machine state.  This is meant to be
// as light weight as possible, as we may never need the state that
// we capture.  Thus to complete the process you need to call
// 'getMachState()', which finishes the process
EXTERN_C int __fastcall LazyMachStateCaptureState(struct LazyMachState *pState);

// CAPTURE_STATE captures just enough register state so that the state of the
// processor can be deterined just after the routine that has CAPTURE_STATE in
// it returns.

// Note that the return is never taken, it is there for epilog walking
#define CAPTURE_STATE(machState, ret)                       \
    if (LazyMachStateCaptureState(machState)) ret

#endif
