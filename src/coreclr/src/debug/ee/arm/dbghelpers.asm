; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

#include "ksarm.h"
#include "asmconstants.h"

    IMPORT FuncEvalHijackWorker
    IMPORT FuncEvalHijackPersonalityRoutine
    IMPORT ExceptionHijackWorker
    IMPORT ExceptionHijackPersonalityRoutine
    EXPORT ExceptionHijackEnd

    MACRO
        CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        push    {r0}
        add     r0, sp, #4
        tst     r0, #7
        pop     {r0}
        beq     %0
        EMIT_BREAKPOINT
0
#endif
    MEND

    TEXTAREA

;
; hijacking stub used to perform a func-eval, see Debugger::FuncEvalSetup() for use.
;
; on entry:
;   r0  : pointer to DebuggerEval object
;

    NESTED_ENTRY FuncEvalHijack,,FuncEvalHijackPersonalityRoutine

    ; NOTE: FuncEvalHijackPersonalityRoutine is dependent on the stack layout so if
    ;       you change the prolog you will also need to update the personality routine.

    ; push arg to the stack so our personality routine can find it
    ; push lr to get good stacktrace in debugger
    PROLOG_PUSH {r0,lr}

    CHECK_STACK_ALIGNMENT

    ; FuncEvalHijackWorker returns the address we should jump to.
    bl      FuncEvalHijackWorker

    ; effective NOP to terminate unwind
    mov r2, r2

    EPILOG_STACK_FREE   8
    EPILOG_BRANCH_REG   r0

    NESTED_END FuncEvalHijack

;
; This is the general purpose hijacking stub. DacDbiInterfaceImpl::Hijack() will
; set the registers with the appropriate parameters from out-of-process.
;
; on entry:
;   r0 : pointer to CONTEXT
;   r1 : pointer to EXCEPTION_RECORD
;   r2 : EHijackReason
;   r3 : void* pdata
;

    NESTED_ENTRY ExceptionHijack,,ExceptionHijackPersonalityRoutine

    CHECK_STACK_ALIGNMENT

    ; make the call
    bl ExceptionHijackWorker

    ; effective NOP to terminate unwind
    mov r3, r3

    ; *** should never get here ***
    EMIT_BREAKPOINT

; exported label so the debugger knows where the end of this function is
ExceptionHijackEnd
    NESTED_END


    ; must be at end of file
    END

