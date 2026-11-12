;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

        TEXTAREA

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 15)&(~15))

#define HARDWARE_EXCEPTION 1
#define SOFTWARE_EXCEPTION 0

;; -----------------------------------------------------------------------------
;; Macro used to create frame of exception throwing helpers (RhpThrowEx, RhpThrowHwEx)
    MACRO
        ALLOC_THROW_FRAME $exceptionType

        PROLOG_NOP mov x3, sp

        ;; Setup a PAL_LIMITED_CONTEXT on the stack {
        IF $exceptionType == HARDWARE_EXCEPTION
            PROLOG_NOP sub sp,sp,#0x50
            PROLOG_NOP stp x3, x1, [sp]   ; x3 is the SP and x1 is the IP of the fault site
            PROLOG_PUSH_MACHINE_FRAME
        ELSE
            PROLOG_STACK_ALLOC 0x50
            PROLOG_NOP stp x3, lr, [sp]   ; x3 is the SP and lr is the IP of the fault site
        ENDIF
        PROLOG_NOP stp d8, d9, [sp, #0x10]
        PROLOG_NOP stp d10, d11, [sp, #0x20]
        PROLOG_NOP stp d12, d13, [sp, #0x30]
        PROLOG_NOP stp d14, d15, [sp, #0x40]
        PROLOG_SAVE_REG_PAIR fp, lr, #-0x70!
        PROLOG_NOP stp xzr, xzr, [sp, #0x10] ; locations reserved for return value, not used for exception handling
        PROLOG_SAVE_REG_PAIR x19, x20, #0x20
        PROLOG_SAVE_REG_PAIR x21, x22, #0x30
        PROLOG_SAVE_REG_PAIR x23, x24, #0x40
        PROLOG_SAVE_REG_PAIR x25, x26, #0x50
        PROLOG_SAVE_REG_PAIR x27, x28, #0x60
        ;; } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo
    MEND

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowHwEx
;;
;; INPUT:  W0:  exception code of fault
;;         X1:  faulting IP
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpThrowHwEx

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

        ALLOC_THROW_FRAME HARDWARE_EXCEPTION

        mov         w19, w0                                         ;; w19 <- exception code

        add         x0, sp, #rsp_offsetof_ExInfo                    ;; x0 <- ExInfo*
        add         x1, sp, #rsp_offsetof_Context                   ;; x1 <- PAL_LIMITED_CONTEXT*
        mov         w2, #2                                          ;; w2 <- ExKind.HardwareFault
        bl          RhpPushExInfo

        mov         w0, w19                                         ;; w0 <- exception code
        add         x1, sp, #rsp_offsetof_ExInfo                    ;; x1 <- ExInfo*
        bl          RhThrowHwEx

    ALTERNATE_ENTRY RhpThrowHwEx2

        ;; no return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowHwEx

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowExact
;;
;; SUMMARY:  Similar to RhpThrowEx, except that it sets the rethrow flag
;;
;; INPUT:  X0:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpThrowExact

        mov         w4, #4                                          ;; w4 = ExKind.RethrowFlag
        b           RhpThrowImpl

    LEAF_END RhpThrowExact

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowEx
;;
;; INPUT:  X0:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpThrowEx

        mov         w4, #1                                          ;; w4 = ExKind.Throw
        b           RhpThrowImpl

    LEAF_END RhpThrowEx

    NESTED_ENTRY RhpThrowImpl

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        mov         x19, x0                                         ;; x19 <- exception object

        add         x0, sp, #rsp_offsetof_ExInfo                    ;; x0 <- ExInfo*
        add         x1, sp, #rsp_offsetof_Context                   ;; x1 <- PAL_LIMITED_CONTEXT*
        mov         w2, w4                                          ;; w2 <- ExKind
        bl          RhpPushExInfo

        mov         x0, x19                                         ;; x0 <- exception object
        add         x1, sp, #rsp_offsetof_ExInfo                    ;; x1 <- ExInfo*
        bl          RhThrowEx

    ALTERNATE_ENTRY RhpThrowImpl2

        ;; no return
        EMIT_BREAKPOINT
    NESTED_END RhpThrowImpl

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpRethrow()
;;
;; SUMMARY:  Similar to RhpThrowEx, except that it passes along the currently active ExInfo
;;
;; INPUT:
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpRethrow

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        add         x0, sp, #rsp_offsetof_ExInfo                    ;; x0 <- ExInfo*
        add         x1, sp, #rsp_offsetof_Context                   ;; x1 <- PAL_LIMITED_CONTEXT*
        mov         w2, #0                                          ;; w2 <- ExKind.None
        bl          RhpPushExInfo

        ;; x0 contains the currently active ExInfo
        add         x1, sp, #rsp_offsetof_ExInfo                    ;; x1 <- ExInfo*
        bl          RhRethrow

    ALTERNATE_ENTRY RhpRethrow2

        ;; no return
        EMIT_BREAKPOINT
    NESTED_END RhpRethrow

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpResumeAfterCatch(REGDISPLAY* pRegDisplay, void* pResumeIP)
;;
;; Resumes execution after a catch funclet called by CallEHFunclet has returned and the dispatcher has popped
;; the ExInfos below the resume SP. Does not return.
;;
;; INPUT:  X0:  REGDISPLAY* of the frame that contains the catch
;;         X1:  resume address returned by the catch funclet
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpResumeAfterCatch

        INLINE_GETTHREAD    x3, x4                              ;; x3 <- Thread*, x4 <- trashed

        ;; Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
        add         x12, x3, #OFFSETOF__Thread__m_ThreadStateFlags
ResumeAfterCatch_ClearRetry
        ldxr        w4, [x12]
        bic         w4, w4, #TSF_DoNotTriggerGc
        stxr        w5, w4, [x12]
        cbnz        w5, ResumeAfterCatch_ClearRetry

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK x3, x4, x12                      ;; Thread in x3, trashes x4 and x12

        LOAD_FUNCLET_REGISTER x0, 19, x12
        LOAD_FUNCLET_REGISTER x0, 20, x12
        LOAD_FUNCLET_REGISTER x0, 21, x12
        LOAD_FUNCLET_REGISTER x0, 22, x12
        LOAD_FUNCLET_REGISTER x0, 23, x12
        LOAD_FUNCLET_REGISTER x0, 24, x12
        LOAD_FUNCLET_REGISTER x0, 25, x12
        LOAD_FUNCLET_REGISTER x0, 26, x12
        LOAD_FUNCLET_REGISTER x0, 27, x12
        LOAD_FUNCLET_REGISTER x0, 28, x12
        LOAD_FUNCLET_REGISTER x0, 29, x12

        LOAD_FUNCLET_FP_REGISTER x0, 8
        LOAD_FUNCLET_FP_REGISTER x0, 9
        LOAD_FUNCLET_FP_REGISTER x0, 10
        LOAD_FUNCLET_FP_REGISTER x0, 11
        LOAD_FUNCLET_FP_REGISTER x0, 12
        LOAD_FUNCLET_FP_REGISTER x0, 13
        LOAD_FUNCLET_FP_REGISTER x0, 14
        LOAD_FUNCLET_FP_REGISTER x0, 15

        ;; reset SP and jump to continuation address
        ldr         x2, [x0, #OFFSETOF__REGDISPLAY__SP]
        mov         sp, x2
        br          x1

    LEAF_END RhpResumeAfterCatch

    end
