; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"

    IMPORT FixContextHandler
    IMPORT LinkFrameAndThrow
    IMPORT HijackHandler
    IMPORT ThrowControlForThread

;
; WARNING!!  These functions immediately ruin thread unwindability.  This is
; WARNING!!  OK as long as there is a mechanism for saving the thread context
; WARNING!!  prior to running these functions as well as a mechanism for
; WARNING!!  restoring the context prior to any stackwalk.  This means that
; WARNING!!  we need to ensure that no GC can occur while the stack is
; WARNING!!  unwalkable.  This further means that we cannot allow any exception
; WARNING!!  to occur when the stack is unwalkable
;

        TEXTAREA

        ; GSCookie, scratch area
        GBLA OFFSET_OF_FRAME

        ; GSCookie + alignment padding
OFFSET_OF_FRAME SETA 4 + SIZEOF__GSCookie

        MACRO
        GenerateRedirectedStubWithFrame $STUB, $TARGET

        ;
        ; This is the primary function to which execution will be redirected to.
        ;
        NESTED_ENTRY $STUB

        ;
        ; IN: lr: original IP before redirect
        ;

        PROLOG_PUSH         {r4,r7,lr}
        PROLOG_STACK_ALLOC  OFFSET_OF_FRAME + SIZEOF__FaultingExceptionFrame

        ; At this point, the stack maybe misaligned if the thread abort was asynchronously
        ; triggered in the prolog or epilog of the managed method. For such a case, we must
        ; align the stack before calling into the VM.
        ;
        ; Runtime check for 8-byte alignment.
        PROLOG_STACK_SAVE r7
        and r0, r7, #4
        sub sp, sp, r0

        ; Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        add r4, sp, #OFFSET_OF_FRAME

        ; Prepare to initialize to NULL
        mov r1,#0
        str r1, [r4]                                                        ; Initialize vtbl (it is not strictly necessary)
        str r1, [r4, #FaultingExceptionFrame__m_fFilterExecuted]            ; Initialize BOOL for personality routine

        mov r0, r4                      ; move the ptr to FEF in R0

        ; stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        bl            $TARGET

        ; Target should not return.
        EMIT_BREAKPOINT

        NESTED_END $STUB

        MEND

; ------------------------------------------------------------------
;
; Helpers for async (NullRef, AccessViolation) exceptions
;

        NESTED_ENTRY NakedThrowHelper2,,FixContextHandler
        PROLOG_PUSH         {r0, lr}

        ; On entry:
        ;
        ; R0 = Address of FaultingExceptionFrame
        bl LinkFrameAndThrow

        ; Target should not return.
        EMIT_BREAKPOINT

        NESTED_END NakedThrowHelper2


        GenerateRedirectedStubWithFrame NakedThrowHelper, NakedThrowHelper2

; ------------------------------------------------------------------
;
; Helpers for ThreadAbort exceptions
;

        NESTED_ENTRY RedirectForThreadAbort2,,HijackHandler
        PROLOG_PUSH         {r0, lr}

        ; stack must be 8 byte aligned
        CHECK_STACK_ALIGNMENT

        ; On entry:
        ;
        ; R0 = Address of FaultingExceptionFrame.
        ;
        ; Invoke the helper to setup the FaultingExceptionFrame and raise the exception
        bl              ThrowControlForThread

        ; ThrowControlForThread doesn't return.
        EMIT_BREAKPOINT

        NESTED_END RedirectForThreadAbort2

        GenerateRedirectedStubWithFrame RedirectForThreadAbort, RedirectForThreadAbort2

; ------------------------------------------------------------------

        ; This helper enables us to call into a funclet after applying the non-volatiles
        NESTED_ENTRY CallEHFunclet

        PROLOG_PUSH         {r4-r11, lr}
        PROLOG_STACK_ALLOC  4

        ; On entry:
        ;
        ; R0 = throwable
        ; R1 = PC to invoke
        ; R2 = address of R4 register in CONTEXT record; used to restore the non-volatile registers of CrawlFrame
        ; R3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        ;
        ; Save the SP of this function
        str sp, [r3]
        ; apply the non-volatiles corresponding to the CrawlFrame
        ldm r2, {r4-r11}
        ; Invoke the funclet
        blx r1

        EPILOG_STACK_FREE   4
        EPILOG_POP          {r4-r11, pc}

        NESTED_END CallEHFunclet

        ; This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
        ; frame pointer for accessing the locals in the parent method.
        NESTED_ENTRY CallEHFilterFunclet

        PROLOG_PUSH         {lr}
        PROLOG_STACK_ALLOC  4

        ; On entry:
        ;
        ; R0 = throwable
        ; R1 = SP of the caller of the method/funclet containing the filter
        ; R2 = PC to invoke
        ; R3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
        ;
        ; Save the SP of this function
        str sp, [r3]
        ; Invoke the filter funclet
        blx r2

        EPILOG_STACK_FREE   4
        EPILOG_POP          {pc}

        NESTED_END CallEHFilterFunclet
        END

