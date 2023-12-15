;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN      RhpGcPoll2
    EXTERN      g_fGcStressStarted

    ;; Build a map of symbols representing offsets into the transition frame (see PInvokeTransitionFrame in
    ;; rhbinder.h) and keep these two in sync.
    map 0
            field OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs
            field 10 * 8 ; x19..x28
m_CallersSP field 8      ; SP at routine entry
            field 2  * 8 ; x0..x1
            field 8      ; alignment padding
            field 4  * 16; q0..q3
PROBE_FRAME_SIZE    field 0

    ;; See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves return registers
    ;; and accepts the register bitmask
    ;; Call this macro first in the method (no further prolog instructions can be added after this).
    ;;
    ;;  $threadReg     : register containing the Thread* (this will be preserved).
    ;;  $trashReg      : register that can be trashed by this macro
    ;;  $BITMASK       : value to initialize m_dwFlags field with (register or #constant)
    MACRO
        PUSH_PROBE_FRAME $threadReg, $trashReg, $BITMASK

        ; Define the method prolog, allocating enough stack space for the PInvokeTransitionFrame and saving
        ; incoming register values into it.

        ;; First create PInvokeTransitionFrame
        PROLOG_SAVE_REG_PAIR   fp, lr, #-(PROBE_FRAME_SIZE)!      ;; Push down stack pointer and store FP and LR

        ;; Slot at [sp, #0x10] is reserved for Thread *
        ;; Slot at [sp, #0x18] is reserved for bitmask of saved registers

        ;; Save callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, #0x20
        PROLOG_SAVE_REG_PAIR   x21, x22, #0x30
        PROLOG_SAVE_REG_PAIR   x23, x24, #0x40
        PROLOG_SAVE_REG_PAIR   x25, x26, #0x50
        PROLOG_SAVE_REG_PAIR   x27, x28, #0x60

        ;; Slot at [sp, #0x70] is reserved for caller sp

        ;; Save the integer return registers
        PROLOG_NOP stp         x0, x1,   [sp, #0x78]

        ;; Slot at [sp, #0x88] is alignment padding

        ;; Save the FP/HFA/HVA return registers
        PROLOG_NOP stp         q0, q1,   [sp, #0x90]
        PROLOG_NOP stp         q2, q3,   [sp, #0xB0]

        ;; Perform the rest of the PInvokeTransitionFrame initialization.
        ;;   str         $threadReg,[sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]       ; Thread * (unused by stackwalker)
        ;;   str         $BITMASK,  [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]         ; save the register bitmask passed in by caller
        ASSERT OFFSETOF__PInvokeTransitionFrame__m_Flags == (OFFSETOF__PInvokeTransitionFrame__m_pThread + 8)
        stp         $threadReg, $BITMASK, [sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]

        add         $trashReg, sp,  #PROBE_FRAME_SIZE                                   ; recover value of caller's SP
        str         $trashReg, [sp, #m_CallersSP]                                       ; save caller's SP

        ;; link the frame into the Thread
        mov         $trashReg, sp
        str         $trashReg, [$threadReg, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
    MEND

;;
;; Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
;; registers and return value to their values from before the probe was called (while also updating any
;; object refs or byrefs).
;;
    MACRO
        POP_PROBE_FRAME

        ;; Restore the integer return registers
        PROLOG_NOP ldp          x0, x1,   [sp, #0x78]

        ; Restore the FP/HFA/HVA return registers
        EPILOG_NOP ldp          q0, q1,   [sp, #0x90]
        EPILOG_NOP ldp          q2, q3,   [sp, #0xB0]

        ;; Restore callee saved registers
        EPILOG_RESTORE_REG_PAIR x19, x20, #0x20
        EPILOG_RESTORE_REG_PAIR x21, x22, #0x30
        EPILOG_RESTORE_REG_PAIR x23, x24, #0x40
        EPILOG_RESTORE_REG_PAIR x25, x26, #0x50
        EPILOG_RESTORE_REG_PAIR x27, x28, #0x60

        EPILOG_RESTORE_REG_PAIR fp, lr, #(PROBE_FRAME_SIZE)!
    MEND

;;
;; The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
;; clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  x2: thread pointer
;;  x3: trashed
;;  x12: transition frame flags for the return registers x0 and x1
;;
    MACRO
        FixupHijackedCallstack

        ;; x2 <- GetThread(), TRASHES x3
        INLINE_GETTHREAD x2, x3

        ;;
        ;; Fix the stack by restoring the original return address
        ;;
        ASSERT OFFSETOF__Thread__m_uHijackedReturnValueFlags == (OFFSETOF__Thread__m_pvHijackedReturnAddress + 8)
        ;; Load m_pvHijackedReturnAddress and m_uHijackedReturnValueFlags
        ldp         lr, x12, [x2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

        ;;
        ;; Clear hijack state
        ;;
        ASSERT OFFSETOF__Thread__m_pvHijackedReturnAddress == (OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation + 8)
        ;; Clear m_ppvHijackedReturnAddressLocation and m_pvHijackedReturnAddress
        stp         xzr, xzr, [x2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        ;; Clear m_uHijackedReturnValueFlags
        str         xzr, [x2, #OFFSETOF__Thread__m_uHijackedReturnValueFlags]

    MEND

    MACRO
        HijackTargetFakeProlog

        ;; This is a fake entrypoint for the method that 'tricks' the OS into calling our personality routine.
        ;; The code here should never be executed, and the unwind info is bogus, but we don't mind since the
        ;; stack is broken by the hijack anyway until after we fix it below.
        PROLOG_SAVE_REG_PAIR   fp, lr, #-0x10!
        nop                     ; We also need a nop here to simulate the implied bl instruction.  Without
                                ; this, an OS-applied -4 will back up into the method prolog and the unwind
                                ; will not be applied as desired.

    MEND

;;
;; GC Probe Hijack target
;;
    EXTERN RhpPInvokeExceptionGuard

    NESTED_ENTRY RhpGcProbeHijackWrapper, .text, RhpPInvokeExceptionGuard
        HijackTargetFakeProlog

    LABELED_RETURN_ADDRESS RhpGcProbeHijack
        FixupHijackedCallstack

        ldr         x3, =RhpTrapThreads
        ldr         w3, [x3]
        tbnz        x3, #TrapThreadsFlags_TrapThreads_Bit, WaitForGC
        ret

WaitForGC
        orr         x12, x12, #(DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_X0 + PTFF_SAVE_X1)
        b           RhpWaitForGC
    NESTED_END RhpGcProbeHijackWrapper

    EXTERN RhpThrowHwEx

    NESTED_ENTRY RhpWaitForGC
        PUSH_PROBE_FRAME x2, x3, x12

        ldr         x0, [x2, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
        bl          RhpWaitForGC2

        ldr         x2, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        tbnz        x2, #PTFF_THREAD_ABORT_BIT, ThrowThreadAbort

        POP_PROBE_FRAME
        EPILOG_RETURN
ThrowThreadAbort
        POP_PROBE_FRAME
        EPILOG_NOP mov w0, #STATUS_REDHAWK_THREAD_ABORT
        EPILOG_NOP mov x1, lr ;; return address as exception PC
        EPILOG_NOP b RhpThrowHwEx
    NESTED_END RhpWaitForGC

    LEAF_ENTRY RhpGcPoll
        ldr         x0, =RhpTrapThreads
        ldr         w0, [x0]
        cbnz        w0, RhpGcPollRare ;; TrapThreadsFlags_None = 0
        ret
    LEAF_END RhpGcPoll

    NESTED_ENTRY RhpGcPollRare
        PUSH_COOP_PINVOKE_FRAME x0
        bl          RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        ret
    NESTED_END RhpGcPollRare


#ifdef FEATURE_GC_STRESS
;;
;;
;; GC Stress Hijack target
;;
;;
    LEAF_ENTRY RhpGcStressHijack
        FixupHijackedCallstack
        orr         x12, x12, #(DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_X0 + PTFF_SAVE_X1)
        b           RhpGcStressProbe
    LEAF_END RhpGcStressHijack
;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack.
;; This worker performs the GC Stress work and returns to the original return address.
;;
;; Register state on entry:
;;  x0: hijacked function return value
;;  x1: hijacked function return value
;;  x2: thread pointer
;;  w12: register bitmask
;;
;; Register state on exit:
;;  Scratch registers, except for x0, have been trashed
;;  All other registers restored as they were when the hijack was first reached.
;;
    NESTED_ENTRY RhpGcStressProbe
        PUSH_PROBE_FRAME x2, x3, x12

        bl          RhpStressGc

        POP_PROBE_FRAME
        EPILOG_RETURN
    NESTED_END RhpGcStressProbe

    NESTED_ENTRY RhpHijackForGcStress
        ;; This function should be called from right before epilog

        ;; Push FP and LR, and allocate stack to hold PAL_LIMITED_CONTEXT structure and VFP return value registers
        PROLOG_SAVE_REG_PAIR    fp, lr, #-(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)!

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;
        ;; This is very likely overkill since the calculation of the return address should only need SP and
        ;; LR, but this is test code, so I'm not too worried about efficiency.
        ;;
        ;; Setup a PAL_LIMITED_CONTEXT on the stack
        ;; {
            ;; FP and LR already pushed.
            PROLOG_NOP  stp         x0, x1, [sp, #0x10]
            PROLOG_SAVE_REG_PAIR    x19, x20, #0x20
            PROLOG_SAVE_REG_PAIR    x21, x22, #0x30
            PROLOG_SAVE_REG_PAIR    x23, x24, #0x40
            PROLOG_SAVE_REG_PAIR    x25, x26, #0x50
            PROLOG_SAVE_REG_PAIR    x27, x28, #0x60
            PROLOG_SAVE_REG         lr, #0x78

        ;; } end PAL_LIMITED_CONTEXT

        ;; Save VFP return value
        stp         d0, d1, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x00)]
        stp         d2, d3, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x10)]

        ;; Compute and save SP at callsite.
        add         x0, sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)   ;; +0x20 for the pushes right before the context struct
        str         x0, [sp, #OFFSETOF__PAL_LIMITED_CONTEXT__SP]

        mov         x0, sp      ; Address of PAL_LIMITED_CONTEXT
        bl          $THREAD__HIJACKFORGCSTRESS

        ;; Restore return value registers (saved in PAL_LIMITED_CONTEXT structure)
        ldp         x0, x1, [sp, #0x10]

        ;; Restore VFP return value
        ldp         d0, d1, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x00)]
        ldp         d2, d3, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x10)]

        ;; Epilog
        EPILOG_RESTORE_REG_PAIR     x19, x20, #0x20
        EPILOG_RESTORE_REG_PAIR     x21, x22, #0x30
        EPILOG_RESTORE_REG_PAIR     x23, x24, #0x40
        EPILOG_RESTORE_REG_PAIR     x25, x26, #0x50
        EPILOG_RESTORE_REG_PAIR     x27, x28, #0x60
        EPILOG_RESTORE_REG_PAIR     fp, lr, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)!
        EPILOG_RETURN

    NESTED_END RhpHijackForGcStress

    NESTED_ENTRY RhpHijackForGcStressLeaf
        ;; This should be jumped to, right before epilog
        ;; x9 has the return address (we don't care about trashing scratch regs at this point)

        ;; Push FP and LR, and allocate stack to hold PAL_LIMITED_CONTEXT structure and VFP return value registers
        PROLOG_SAVE_REG_PAIR    fp, lr, #-(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)!

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;
        ;; This is very likely overkill since the calculation of the return address should only need SP and
        ;; LR, but this is test code, so I'm not too worried about efficiency.
        ;;
        ;; Setup a PAL_LIMITED_CONTEXT on the stack
        ;; {
            ;; FP and LR already pushed.
            PROLOG_NOP  stp         x0, x1, [sp, #0x10]
            PROLOG_SAVE_REG_PAIR    x19, x20, #0x20
            PROLOG_SAVE_REG_PAIR    x21, x22, #0x30
            PROLOG_SAVE_REG_PAIR    x23, x24, #0x40
            PROLOG_SAVE_REG_PAIR    x25, x26, #0x50
            PROLOG_SAVE_REG_PAIR    x27, x28, #0x60
            ; PROLOG_SAVE_REG macro doesn't let to use scratch reg:
            PROLOG_NOP  str         x9, [sp, #0x78]           ; this is return address from RhpHijackForGcStress; lr is return address for it's caller

        ;; } end PAL_LIMITED_CONTEXT

        ;; Save VFP return value
        stp         d0, d1, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x00)]
        stp         d2, d3, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x10)]

        ;; Compute and save SP at callsite.
        add         x0, sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)   ;; +0x20 for the pushes right before the context struct
        str         x0, [sp, #OFFSETOF__PAL_LIMITED_CONTEXT__SP]

        mov         x0, sp      ; Address of PAL_LIMITED_CONTEXT
        bl          $THREAD__HIJACKFORGCSTRESS

        ;; Restore return value registers (saved in PAL_LIMITED_CONTEXT structure)
        ldp         x0, x1, [sp, #0x10]

        ;; Restore VFP return value
        ldp         d0, d1, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x00)]
        ldp         d2, d3, [sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x10)]

        ;; Epilog
        EPILOG_RESTORE_REG_PAIR     x19, x20, #0x20
        EPILOG_RESTORE_REG_PAIR     x21, x22, #0x30
        EPILOG_RESTORE_REG_PAIR     x23, x24, #0x40
        EPILOG_RESTORE_REG_PAIR     x25, x26, #0x50
        EPILOG_RESTORE_REG_PAIR     x27, x28, #0x60
        EPILOG_NOP     ldr          x9, [sp, #0x78]
        EPILOG_RESTORE_REG_PAIR     fp, lr, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x20)!
        EPILOG_NOP     br           x9

    NESTED_END RhpHijackForGcStressLeaf

;;
;; INVARIANT: Don't trash the argument registers, the binder codegen depends on this.
;;
    LEAF_ENTRY RhpSuppressGcStress
        INLINE_GETTHREAD x9, x10
        add         x9, x9, #OFFSETOF__Thread__m_ThreadStateFlags
Retry
        ldxr        w10, [x9]
        orr         w10, w10, #TSF_SuppressGcStress
        stxr        w11, w10, [x9]
        cbz         w11, Success
        b           Retry

Success
        ret
    LEAF_END RhpSuppressGcStress
#endif ;; FEATURE_GC_STRESS

    end

