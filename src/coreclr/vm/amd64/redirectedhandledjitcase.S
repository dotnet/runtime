// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#define OFFSET_OF_FRAME 0

.macro GenerateRedirectedStubWithFrame stub, target

    //
    // This is the primary function to which execution will be redirected to.
    //
    NESTED_ENTRY \stub, _TEXT, NoHandler

        //
        // IN: rdi: original IP before redirect
        //     rsi: Rip from the Thread::GetAbortContext()
        //

        mov             rdx, rsp

        // This push of the return address must not be recorded in the unwind
        // info.  After this push, unwinding will work.
        push            rdi

        test            rsp, 0x0f
        jnz             C_FUNC(\stub\()_FixRsp)

LOCAL_LABEL(\stub\()_RspAligned):

        // Any stack operations hereafter must be recorded in the unwind info, but
        // only nonvolatile register locations are needed.  Anything else is only
        // a "sub rsp, 8" to the unwinder.

        alloc_stack     OFFSET_OF_FRAME + SIZEOF__FaultingExceptionFrame

        END_PROLOGUE

        lea             rdi, [rsp + OFFSET_OF_FRAME]

        mov             dword ptr [rdi], 0                                                          // Initialize vtbl (it is not strictly necessary)
        mov             dword ptr [rdi + OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted], 0    // Initialize BOOL for personality routine

        call C_FUNC(\target)

        // Target should not return.
        int             3

    NESTED_END \stub, _TEXT

// This function is used by the stub above to adjust the stack alignment.  The
// stub can't conditionally push something on the stack because the unwind
// encodings have no way to express that.
//
// CONSIDER: we could move the frame pointer above the FaultingExceptionFrame,
// and detect the misalignment adjustment in
// GetFrameFromRedirectedStubStackFrame.  This is probably less code and more
// straightforward.
LEAF_ENTRY \stub\()_FixRsp, _TEXT

        call            LOCAL_LABEL(\stub\()_RspAligned)

        // Target should not return.
        int             3

LEAF_END \stub\()_FixRsp, _TEXT

.endm

REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE = 8

NESTED_ENTRY RedirectForThrowControl2, _TEXT, NoHandler

        // On entry
        // rdi -> FaultingExceptionFrame
        // rsi -> Rip from the Thread::GetAbortContext()
        // rdx -> Original RSP

        push_register   rax // Align stack

        END_PROLOGUE

        mov             [rdx - 8], rsi // Set return address slot to the Rip from the Thread::GetAbortContext()

        call            C_FUNC(ThrowControlForThread)

        // ThrowControlForThread doesn't return.
        int             3

NESTED_END RedirectForThrowControl2, _TEXT

GenerateRedirectedStubWithFrame RedirectForThrowControl, RedirectForThrowControl2
