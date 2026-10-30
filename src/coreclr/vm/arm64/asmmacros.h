// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "arm64/AsmMacros_Windows.h"

;-----------------------------------------------------------------------------
; The following group of macros assist in implementing prologs and epilogs for methods that set up some
; subclass of TransitionFrame. They ensure that the SP is 16-byte aligned at the conclusion of the prolog

;-----------------------------------------------------------------------------
; Define the prolog for a TransitionFrame-based method. This macro should be called first in the method and
; comprises the entire prolog (i.e. don't modify SP after calling this).The locals must be 8 byte aligned
;
; $pushCalleeSavedFloatRegs - Optional parameter. If set to PushCalleeSavedFloatRegs, the macro will also save
;                             the callee-saved floating point registers (d8-d15) to the stack. These registers
;                             are NOT restored by the EPILOG_WITH_TRANSITION_BLOCK variants.
;
    MACRO
        PROLOG_WITH_TRANSITION_BLOCK $extraLocals, $SaveFPArgs, $pushCalleeSavedFloatRegs

        GBLA __PWTB_FloatArgumentRegisters
        GBLA __PWTB_FloatCalleeSavedRegisters
        GBLA __PWTB_ArgumentRegisters
        GBLA __PWTB_ArgumentRegister_FirstArg ; We save the x8 register ahead of the first argument, so this
                                              ; is different from the start of the argument register save area.
        GBLA __PWTB_StackAlloc
        GBLA __PWTB_TransitionBlock
        GBLL __PWTB_SaveFPArgs

        IF "$SaveFPArgs" != ""
__PWTB_SaveFPArgs SETL $SaveFPArgs
        ELSE
__PWTB_SaveFPArgs SETL {true}
        ENDIF

        IF "$extraLocals" != ""
__PWTB_FloatCalleeSavedRegisters SETA $extraLocals
        ELSE
__PWTB_FloatCalleeSavedRegisters SETA 0
        ENDIF

        IF __PWTB_FloatCalleeSavedRegisters:MOD:16 != 0
__PWTB_FloatCalleeSavedRegisters SETA __PWTB_FloatCalleeSavedRegisters + 8
        ENDIF

        ; If PushCalleeSavedFloatRegs is specified, reserve space for d8-d15 (8 registers * 8 bytes = 64 bytes)
        IF "$pushCalleeSavedFloatRegs" == "PushCalleeSavedFloatRegs"
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatCalleeSavedRegisters + 64
        ELSE
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatCalleeSavedRegisters
        ENDIF

        IF __PWTB_SaveFPArgs
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters + SIZEOF__FloatArgumentRegisters
        ELSE
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters
        ENDIF

__PWTB_StackAlloc SETA __PWTB_TransitionBlock
__PWTB_ArgumentRegisters SETA __PWTB_StackAlloc + 104
__PWTB_ArgumentRegister_FirstArg SETA __PWTB_ArgumentRegisters + 8

        PROLOG_SAVE_REG_PAIR   fp, lr, #-176!
        ; Spill callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, #16
        PROLOG_SAVE_REG_PAIR   x21, x22, #32
        PROLOG_SAVE_REG_PAIR   x23, x24, #48
        PROLOG_SAVE_REG_PAIR   x25, x26, #64
        PROLOG_SAVE_REG_PAIR   x27, x28, #80

        ; Allocate space for the rest of the frame
        PROLOG_STACK_ALLOC  __PWTB_StackAlloc

        ; Spill argument registers.
        SAVE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters

        IF __PWTB_SaveFPArgs
        SAVE_FLOAT_ARGUMENT_REGISTERS  sp, __PWTB_FloatArgumentRegisters
        ENDIF

        ; Save callee-saved floating point registers if requested
        IF "$pushCalleeSavedFloatRegs" == "PushCalleeSavedFloatRegs"
        stp     d8, d9, [sp, #__PWTB_FloatCalleeSavedRegisters]
        stp     d10, d11, [sp, #(__PWTB_FloatCalleeSavedRegisters + 16)]
        stp     d12, d13, [sp, #(__PWTB_FloatCalleeSavedRegisters + 32)]
        stp     d14, d15, [sp, #(__PWTB_FloatCalleeSavedRegisters + 48)]
        ENDIF

    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK

        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and returns to caller.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        EPILOG_WITH_TRANSITION_BLOCK
		EPILOG_RETURN
    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and ends by preparing for tail-calling.
; Since this is a tail call argument registers are restored.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        IF __PWTB_SaveFPArgs
        RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, __PWTB_FloatArgumentRegisters
        ENDIF

        RESTORE_ARGUMENT_REGISTERS        sp, __PWTB_ArgumentRegisters
        EPILOG_STACK_FREE                 __PWTB_StackAlloc

        EPILOG_RESTORE_REG_PAIR   x19, x20, #16
        EPILOG_RESTORE_REG_PAIR   x21, x22, #32
        EPILOG_RESTORE_REG_PAIR   x23, x24, #48
        EPILOG_RESTORE_REG_PAIR   x25, x26, #64
        EPILOG_RESTORE_REG_PAIR   x27, x28, #80
        EPILOG_RESTORE_REG_PAIR   fp, lr,   #176!
    MEND

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
