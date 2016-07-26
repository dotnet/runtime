; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT FuncEvalHijackWorker
    IMPORT FuncEvalHijackPersonalityRoutine
    IMPORT ExceptionHijackWorker
    IMPORT ExceptionHijackPersonalityRoutine
    EXPORT ExceptionHijackEnd
;
; hijacking stub used to perform a func-eval, see Debugger::FuncEvalSetup() for use.
;
; on entry:
;   x0  : pointer to DebuggerEval object
;

    NESTED_ENTRY FuncEvalHijack,,FuncEvalHijackPersonalityRoutine

    ; NOTE: FuncEvalHijackPersonalityRoutine is dependent on the stack layout so if
    ;       you change the prolog you will also need to update the personality routine.

    ; push arg to the stack so our personality routine can find it
    ; push lr to get good stacktrace in debugger
    PROLOG_SAVE_REG_PAIR           fp, lr, #-32!
    str x0, [sp, #16]
    ; FuncEvalHijackWorker returns the address we should jump to. 
    bl FuncEvalHijackWorker

    EPILOG_STACK_FREE 32
    EPILOG_BRANCH_REG x0
    NESTED_END FuncEvalHijack

    NESTED_ENTRY ExceptionHijack,,ExceptionHijackPersonalityRoutine

    ; make the call
    bl ExceptionHijackWorker

    ; effective NOP to terminate unwind
    mov x3, x3

    ; *** should never get here ***
    EMIT_BREAKPOINT

; exported label so the debugger knows where the end of this function is
ExceptionHijackEnd
    NESTED_END ExceptionHijack

    ; must be at end of file
    END
    
