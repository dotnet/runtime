// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __gmscpu_h__
#define __gmscpu_h__

#define __gmscpu_h__

    // A MachState indicates the register state of the processor at some point in time (usually
    // just before or after a call is made).  It can be made one of two ways.  Either explicitly
    // (when you for some reason know the values of all the registers), or implicitly using the
    // GET_STATE macros.

    typedef DPTR(struct MachState) PTR_MachState;
    struct MachState {
    
        BOOL   isValid()    { LIMITED_METHOD_DAC_CONTRACT; return TRUE; }
        TADDR  GetRetAddr() { LIMITED_METHOD_DAC_CONTRACT; return 0; }
    
        friend class HelperMethodFrame;
        friend class CheckAsmOffsets;
        friend struct LazyMachState;
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
                                    int funCallDepth = 1);
    
        friend class HelperMethodFrame;
        friend class CheckAsmOffsets;
    };
    
    // R4 - R11
    #define NUM_NONVOLATILE_CONTEXT_POINTERS 8
    
    inline void LazyMachState::setLazyStateFromUnwind(MachState* copy)
    {
        _ASSERTE("LazyMachState::setLazyStateFromUnwind is not implemented");
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
