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

;
; Flares for interop debugging.
; Flares are exceptions (breakpoints) at well known addresses which the RS
; listens for when interop debugging.
;

    LEAF_ENTRY SignalHijackStartedFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x1, x1
    ret     lr
    LEAF_END

    LEAF_ENTRY ExceptionForRuntimeHandoffStartFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x2, x2
    ret     lr
    LEAF_END

    LEAF_ENTRY ExceptionForRuntimeHandoffCompleteFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x3, x3
    ret     lr
    LEAF_END

    LEAF_ENTRY SignalHijackCompleteFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x4, x4
    ret     lr
    LEAF_END

    LEAF_ENTRY ExceptionNotForRuntimeFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x5, x5
    ret     lr
    LEAF_END

    LEAF_ENTRY NotifyRightSideOfSyncCompleteFlare
    EMIT_BREAKPOINT
    ; make sure that the basic block is unique
    add     x0, x6, x6
    ret     lr
    LEAF_END

    ; must be at end of file
    END
    
