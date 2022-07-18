;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGCNoAbort
;;
;;
;; INPUT: x9: transition frame
;;
;; TRASHES: None
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpWaitForGCNoAbort

        ;; FP and LR registers
        PROLOG_SAVE_REG_PAIR   fp, lr, #-0x40!            ;; Push down stack pointer and store FP and LR

        ;; Save the integer return registers, as well as the floating return registers
        stp         x0, x1, [sp, #0x10]
        stp         d0, d1, [sp, #0x20]
        stp         d2, d3, [sp, #0x30]

        ldr         x0, [x9, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        ldr         w0, [x0, #OFFSETOF__Thread__m_ThreadStateFlags]
        tbnz        x0, #TSF_DoNotTriggerGc_Bit, Done

        mov         x0, x9      ; passing transition frame in x0
        bl          RhpWaitForGC2

Done
        ldp         x0, x1, [sp, #0x10]
        ldp         d0, d1, [sp, #0x20]
        ldp         d2, d3, [sp, #0x30]
        EPILOG_RESTORE_REG_PAIR   fp, lr, #0x40!
        EPILOG_RETURN

    NESTED_END RhpWaitForGCNoAbort

    EXTERN RhpThrowHwEx

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGC
;;
;;
;; INPUT: x9: transition frame
;;
;; TRASHES: x0, x1, x10
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpWaitForGC

        PROLOG_SAVE_REG_PAIR    fp, lr, #-0x10!

        ldr         x10, =RhpTrapThreads
        ldr         w10, [x10]
        tbz         x10, #TrapThreadsFlags_TrapThreads_Bit, NoWait
        bl          RhpWaitForGCNoAbort
NoWait
        tbz         x10, #TrapThreadsFlags_AbortInProgress_Bit, NoAbort
        ldr         x10, [x9, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        tbz         x10, #PTFF_THREAD_ABORT_BIT, NoAbort

        EPILOG_RESTORE_REG_PAIR fp, lr, #0x10!
        EPILOG_NOP  mov w0, #STATUS_REDHAWK_THREAD_ABORT
        EPILOG_NOP  mov x1, lr          ; hijack target address as exception PC
        EPILOG_NOP  b RhpThrowHwEx

NoAbort
        EPILOG_RESTORE_REG_PAIR fp, lr, #0x10!
        EPILOG_RETURN

    NESTED_END RhpWaitForGC

    INLINE_GETTHREAD_CONSTANT_POOL

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvoke
;;
;; IN: x0: address of pinvoke frame
;;
;; TRASHES: x9
;;
;; This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
;; The codegenerator must treat the callsite of this helper as GC triggering and generate the GC info for it.
;; Also, the codegenerator must ensure that there are no live GC references in callee saved registers.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpPInvoke, _TEXT

        str     fp, [x0, #OFFSETOF__PInvokeTransitionFrame__m_FramePointer]
        str     lr, [x0, #OFFSETOF__PInvokeTransitionFrame__m_RIP]
        mov     x9, sp
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs]
        mov     x9, #PTFF_SAVE_SP
        str     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_Flags]

        INLINE_GETTHREAD x1, x9

        str     x1, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        str     x0, [x1, #OFFSETOF__Thread__m_pTransitionFrame]
        ret
    NESTED_END RhpPInvoke

    INLINE_GETTHREAD_CONSTANT_POOL


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvokeReturn
;;
;; IN: x0: address of pinvoke frame
;;
;; TRASHES: x9, x10
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    LEAF_ENTRY RhpPInvokeReturn, _TEXT
        ldr     x9, [x0, #OFFSETOF__PInvokeTransitionFrame__m_pThread]
        mov     x10, 0
        str     x10, [x9, #OFFSETOF__Thread__m_pTransitionFrame]

        ldr     x9, =RhpTrapThreads
        ldr     w9, [x9]
        cbnz    w9, %ft0 ;; TrapThreadsFlags_None = 0
        ret
0
        ;; passing transition frame pointer in x0
        b       RhpWaitForGC2
    LEAF_END RhpPInvokeReturn

    end
