// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "arm64/AsmMacros_Windows.h"

;-----------------------------------------------------------------------------
; Macros used for shared allocation helpers

    SETALIAS t_runtime_thread_locals, ?t_runtime_thread_locals@@3URuntimeThreadLocals@@A

    MACRO
    INLINE_GET_ALLOC_CONTEXT_BASE $destReg, $trashReg

        INLINE_GET_TLS_VAR $destReg, $trashReg, $t_runtime_thread_locals
    MEND

OFFSETOF__ee_alloc_context  EQU OFFSETOF__RuntimeThreadLocals__ee_alloc_context

    MACRO
    PUSH_COOP_PINVOKE_FRAME $Target

        PROLOG_SAVE_REG_PAIR   fp, lr, #-176!

        ; Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, #16
        PROLOG_SAVE_REG_PAIR   x21, x22, #32
        PROLOG_SAVE_REG_PAIR   x23, x24, #48
        PROLOG_SAVE_REG_PAIR   x25, x26, #64
        PROLOG_SAVE_REG_PAIR   x27, x28, #80

        mov     $Target, sp
    MEND

    MACRO
    POP_COOP_PINVOKE_FRAME

        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
    MEND

; Pushes a full TransitionBlock on the stack including argument registers and
; floating point argument registers. Used for exception throw helpers where we
; need to capture the complete register state including FP callee-saved registers.
;
; Stack layout (from low to high address):
;   sp+0:     FP callee-saved registers (d8-d15, 64 bytes)
;   sp+64:    FloatArgumentRegisters (q0-q7, 128 bytes)
;   sp+192:   TransitionBlock start (176 bytes)
;             - CalleeSavedRegisters (fp, lr, x19-x28 - 96 bytes)
;             - padding (8 bytes)
;             - x8 (8 bytes)
;             - ArgumentRegisters (x0-x7, 64 bytes)
;
; On exit, $Target contains the TransitionBlock pointer (sp+192).
    MACRO
    PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS $Target

        PROLOG_SAVE_REG_PAIR   fp, lr, #-176!

        ; Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, #16
        PROLOG_SAVE_REG_PAIR   x21, x22, #32
        PROLOG_SAVE_REG_PAIR   x23, x24, #48
        PROLOG_SAVE_REG_PAIR   x25, x26, #64
        PROLOG_SAVE_REG_PAIR   x27, x28, #80

        ; Allocate space for FloatArgumentRegisters (128) + FP callee-saved (64) = 192 bytes
        PROLOG_STACK_ALLOC 192

        ; Save argument registers (x8, x0-x7) at offset 296 from sp (192 + 104)
        SAVE_ARGUMENT_REGISTERS sp, 296

        ; Save floating point argument registers (q0-q7) at sp+64
        SAVE_FLOAT_ARGUMENT_REGISTERS sp, 64

        ; Save FP callee-saved registers (d8-d15) at sp+0
        str     d8,  [sp, #0]
        str     d9,  [sp, #8]
        str     d10, [sp, #16]
        str     d11, [sp, #24]
        str     d12, [sp, #32]
        str     d13, [sp, #40]
        str     d14, [sp, #48]
        str     d15, [sp, #56]

        ; Set target to TransitionBlock pointer
        add     $Target, sp, #192
    MEND

; ------------------------------------------------------------------
; Macro to generate Redirection Stubs
;
; $reason : reason for redirection
;                     Eg. GCThreadControl
; NOTE: If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
; This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
        MACRO
        GenerateRedirectedHandledJITCaseStub $reason

        GBLS __RedirectionStubFuncName
        GBLS __RedirectionStubEndFuncName
        GBLS __RedirectionFuncName
__RedirectionStubFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_Stub"
__RedirectionStubEndFuncName SETS "RedirectedHandledJITCaseFor":CC:"$reason":CC:"_StubEnd"
__RedirectionFuncName SETS "|?RedirectedHandledJITCaseFor":CC:"$reason":CC:"@Thread@@CAXXZ|"

        IMPORT $__RedirectionFuncName

        NESTED_ENTRY $__RedirectionStubFuncName
        PROLOG_SAVE_REG_PAIR    fp, lr, #-16!
        sub sp, sp, #16                          ; stack slot for CONTEXT * and padding

        ;REDIRECTSTUB_SP_OFFSET_CONTEXT is defined in asmconstants.h and is used in GetCONTEXTFromRedirectedStubStackFrame
        ;If CONTEXT is not saved at 0 offset from SP it must be changed as well.
        ASSERT REDIRECTSTUB_SP_OFFSET_CONTEXT == 0

        ; Stack alignment. This check is necessary as this function can be
        ; entered before complete execution of the prolog of another function.
        and x8, fp, #15
        sub sp, sp, x8


        ;
        ; Save a copy of the redirect CONTEXT*.
        ; This is needed for the debugger to unwind the stack.
        ;
        bl GetCurrentSavedRedirectContext
        str x0, [sp]

        ;
        ; Fetch the interrupted pc and save it as our return address.
        ;
        ldr x1, [x0, #CONTEXT_Pc]
        str x1, [fp, #8]

        ;
        ; Call target, which will do whatever we needed to do in the context
        ; of the target thread, and will RtlRestoreContext when it is done.
        ;
        bl $__RedirectionFuncName

        EMIT_BREAKPOINT ; Unreachable

; Put a label here to tell the debugger where the end of this function is.
$__RedirectionStubEndFuncName
        EXPORT $__RedirectionStubEndFuncName

        NESTED_END

        MEND

;; -----------------------------------------------------------------------------
;;
;; Macro to get a pointer to the Thread* object for the currently executing thread
;;
    SETALIAS t_CurrentThreadInfo, ?t_CurrentThreadInfo@@3UThreadLocalInfo@@A

    MACRO
        INLINE_GETTHREAD $destReg, $trashReg

        INLINE_GET_TLS_VAR $destReg, $trashReg, $t_CurrentThreadInfo
        ldr $destReg, [$destReg]                            ;; return t_CurrentThreadInfo.m_pThread
    MEND
