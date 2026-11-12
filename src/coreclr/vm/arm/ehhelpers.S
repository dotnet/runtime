// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

//
// WARNING!!  These functions immediately ruin thread unwindability.  This is
// WARNING!!  OK as long as there is a mechanism for saving the thread context
// WARNING!!  prior to running these functions as well as a mechanism for
// WARNING!!  restoring the context prior to any stackwalk.  This means that
// WARNING!!  we need to ensure that no GC can occur while the stack is
// WARNING!!  unwalkable.  This further means that we cannot allow any exception
// WARNING!!  to occur when the stack is unwalkable
//

        // alignment padding
OFFSET_OF_FRAME=0

        .macro GenerateRedirectedStubWithFrame STUB, TARGET

        //
        // This is the primary function to which execution will be redirected to.
        //
        NESTED_ENTRY \STUB, _TEXT, NoHandler

        //
        // IN: lr: original IP before redirect
        //

        PROLOG_PUSH  "{r4,r7,lr}"
        alloc_stack  OFFSET_OF_FRAME + SIZEOF__FaultingExceptionFrame

        // At this point, the stack maybe misaligned if the thread abort was asynchronously
        // triggered in the prolog or epilog of the managed method. For such a case, we must
        // align the stack before calling into the VM.
        //
        // Runtime check for 8-byte alignment.
        PROLOG_STACK_SAVE r7
        // We lose stack unwindability here by configuring fp(r7) incorrectely
        // here.
        and r0, r7, #4
        sub sp, sp, r0

        // Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        add r4, sp, #OFFSET_OF_FRAME

        // Prepare to initialize to NULL
        mov r1,#0
        str r1, [r4]                                                        // Initialize vtbl (it is not strictly necessary)
        str r1, [r4, #FaultingExceptionFrame__m_fFilterExecuted]            // Initialize BOOL for personality routine

        mov r0, r4                      // move the ptr to FEF in R0

        // stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        bl            C_FUNC(\TARGET)

        // Target should not return.
        EMIT_BREAKPOINT

        NESTED_END \STUB, _TEXT

        .endm

// ------------------------------------------------------------------
//
// Helpers for ThreadAbort exceptions
//

        NESTED_ENTRY RedirectForThreadAbort2, _TEXT, NoHandler
        PROLOG_PUSH  "{r7, lr}"

        // stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        // On entry:
        //
        // r0 = address of FaultingExceptionFrame
        //
        // Invoke the helper to setup the FaultingExceptionFrame and raise the exception
        bl              C_FUNC(ThrowControlForThread)

        // ThrowControlForThread doesn't return.
        EMIT_BREAKPOINT

        NESTED_END RedirectForThreadAbort2, _TEXT

GenerateRedirectedStubWithFrame RedirectForThreadAbort, RedirectForThreadAbort2
