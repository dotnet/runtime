;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

    EXTERN      RhpGcPoll2
    EXTERN      g_fGcStressStarted

PROBE_SAVE_FLAGS_EVERYTHING     equ DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_ALL_SCRATCH + PTFF_SAVE_LR

    ;; Build a map of symbols representing offsets into the transition frame (see PInvokeTransitionFrame in
    ;; rhbinder.h) and keep these two in sync.
    map 0
            field OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs
            field 10 * 8 ; x19..x28
m_CallersSP field 8      ; SP at routine entry
            field 19 * 8 ; x0..x18
            field 8      ; lr
m_SavedNZCV field 8      ; Saved condition flags
            field 4 * 8  ; d0..d3
PROBE_FRAME_SIZE    field 0

    ;; Support for setting up a transition frame when performing a GC probe. In many respects this is very
    ;; similar to the logic in PUSH_COOP_PINVOKE_FRAME in AsmMacros.h. In most cases setting up the
    ;; transition frame comprises the entirety of the caller's prolog (and initial non-prolog code) and
    ;; similarly for the epilog. Those cases can be dealt with using PROLOG_PROBE_FRAME and EPILOG_PROBE_FRAME
    ;; defined below. For the special cases where additional work has to be done in the prolog we also provide
    ;; the lower level macros ALLOC_PROBE_FRAME, FREE_PROBE_FRAME and INIT_PROBE_FRAME that allow more control
    ;; to be asserted.
    ;;
    ;; Note that we currently employ a significant simplification of frame setup: we always allocate a
    ;; maximally-sized PInvokeTransitionFrame and save all of the registers. Depending on the caller this can
    ;; lead to up to 20 additional register saves (x0-x18, lr) or 160 bytes of stack space. I have done no
    ;; analysis to see whether any of the worst cases occur on performance sensitive paths and whether the
    ;; additional saves will show any measurable degradation.

    ;; Perform the parts of setting up a probe frame that can occur during the prolog (and indeed this macro
    ;; can only be called from within the prolog).
    MACRO
        ALLOC_PROBE_FRAME $extraStackSpace, $saveFPRegisters

        ;; First create PInvokeTransitionFrame
        PROLOG_SAVE_REG_PAIR   fp, lr, #-(PROBE_FRAME_SIZE + $extraStackSpace)!      ;; Push down stack pointer and store FP and LR

        ;; Slot at [sp, #0x10] is reserved for Thread *
        ;; Slot at [sp, #0x18] is reserved for bitmask of saved registers

        ;; Save callee saved registers
        PROLOG_SAVE_REG_PAIR   x19, x20, #0x20
        PROLOG_SAVE_REG_PAIR   x21, x22, #0x30
        PROLOG_SAVE_REG_PAIR   x23, x24, #0x40
        PROLOG_SAVE_REG_PAIR   x25, x26, #0x50
        PROLOG_SAVE_REG_PAIR   x27, x28, #0x60

        ;; Slot at [sp, #0x70] is reserved for caller sp

        ;; Save the scratch registers
        PROLOG_NOP str         x0,       [sp, #0x78]
        PROLOG_NOP stp         x1, x2,   [sp, #0x80]
        PROLOG_NOP stp         x3, x4,   [sp, #0x90]
        PROLOG_NOP stp         x5, x6,   [sp, #0xA0]
        PROLOG_NOP stp         x7, x8,   [sp, #0xB0]
        PROLOG_NOP stp         x9, x10,  [sp, #0xC0]
        PROLOG_NOP stp         x11, x12, [sp, #0xD0]
        PROLOG_NOP stp         x13, x14, [sp, #0xE0]
        PROLOG_NOP stp         x15, x16, [sp, #0xF0]
        PROLOG_NOP stp         x17, x18, [sp, #0x100]
        PROLOG_NOP str         lr,       [sp, #0x110]

        ;; Slot at [sp, #0x118] is reserved for NZCV

        ;; Save the floating return registers
        IF $saveFPRegisters
            PROLOG_NOP stp         d0, d1,   [sp, #0x120]
            PROLOG_NOP stp         d2, d3,   [sp, #0x130]
        ENDIF

    MEND

    ;; Undo the effects of an ALLOC_PROBE_FRAME. This may only be called within an epilog. Note that all
    ;; registers are restored (apart for sp and pc), even volatiles.
    MACRO
        FREE_PROBE_FRAME $extraStackSpace, $restoreFPRegisters

        ;; Restore the scratch registers
        PROLOG_NOP ldr          x0,       [sp, #0x78]
        PROLOG_NOP ldp          x1, x2,   [sp, #0x80]
        PROLOG_NOP ldp          x3, x4,   [sp, #0x90]
        PROLOG_NOP ldp          x5, x6,   [sp, #0xA0]
        PROLOG_NOP ldp          x7, x8,   [sp, #0xB0]
        PROLOG_NOP ldp          x9, x10,  [sp, #0xC0]
        PROLOG_NOP ldp          x11, x12, [sp, #0xD0]
        PROLOG_NOP ldp          x13, x14, [sp, #0xE0]
        PROLOG_NOP ldp          x15, x16, [sp, #0xF0]
        PROLOG_NOP ldp          x17, x18, [sp, #0x100]
        PROLOG_NOP ldr          lr,       [sp, #0x110]

        ; Restore the floating return registers
        IF $restoreFPRegisters
            EPILOG_NOP ldp          d0, d1,   [sp, #0x120]
            EPILOG_NOP ldp          d2, d3,   [sp, #0x130]
        ENDIF

        ;; Restore callee saved registers
        EPILOG_RESTORE_REG_PAIR x19, x20, #0x20
        EPILOG_RESTORE_REG_PAIR x21, x22, #0x30
        EPILOG_RESTORE_REG_PAIR x23, x24, #0x40
        EPILOG_RESTORE_REG_PAIR x25, x26, #0x50
        EPILOG_RESTORE_REG_PAIR x27, x28, #0x60

        EPILOG_RESTORE_REG_PAIR fp, lr, #(PROBE_FRAME_SIZE + $extraStackSpace)!
    MEND

    ;; Complete the setup of a probe frame allocated with ALLOC_PROBE_FRAME with the initialization that can
    ;; occur only outside the prolog (includes linking the frame to the current Thread). This macro assumes SP
    ;; is invariant outside of the prolog.
    ;;
    ;;  $threadReg     : register containing the Thread* (this will be preserved)
    ;;  $trashReg      : register that can be trashed by this macro
    ;;  $savedRegsMask : value to initialize m_Flags field with (register or #constant)
    ;;  $gcFlags       : value of gcref / gcbyref flags for saved registers, used only if $savedRegsMask is constant
    ;;  $frameSize     : total size of the method's stack frame (including probe frame size)
    MACRO
        INIT_PROBE_FRAME $threadReg, $trashReg, $savedRegsMask, $gcFlags, $frameSize

        LCLS BitmaskStr
BitmaskStr SETS "$savedRegsMask"

        str         $threadReg, [sp, #OFFSETOF__PInvokeTransitionFrame__m_pThread]            ; Thread *
        IF          BitmaskStr:LEFT:1 == "#"
            ;; The savedRegsMask is a constant, remove the leading "#" since the MOVL64 doesn't expect it
BitmaskStr  SETS BitmaskStr:RIGHT:(:LEN:BitmaskStr - 1)
            MOVL64      $trashReg, $BitmaskStr, $gcFlags
        ELSE
            ASSERT "$gcFlags" == ""
            ;; The savedRegsMask is a register
            mov         $trashReg, $savedRegsMask
        ENDIF
        str         $trashReg, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        add         $trashReg, sp, #$frameSize
        str         $trashReg, [sp, #m_CallersSP]
    MEND

    ;; Simple macro to use when setting up the probe frame can comprise the entire prolog. Call this macro
    ;; first in the method (no further prolog instructions can be added after this).
    ;;
    ;;  $threadReg     : register containing the Thread* (this will be preserved). If defaulted (specify |) then
    ;;                   the current thread will be calculated inline into r2 ($trashReg must not equal r2 in
    ;;                   this case)
    ;;  $trashReg      : register that can be trashed by this macro
    ;;  $savedRegsMask : value to initialize m_dwFlags field with (register or #constant)
    ;;  $gcFlags       : value of gcref / gcbyref flags for saved registers, used only if $savedRegsMask is constant
    MACRO
        PROLOG_PROBE_FRAME $threadReg, $trashReg, $savedRegsMask, $gcFlags

        ; Local string tracking the name of the register in which the Thread* is kept. Defaults to the value
        ; of $threadReg.
        LCLS __PPF_ThreadReg
__PPF_ThreadReg SETS "$threadReg"

        ; Define the method prolog, allocating enough stack space for the PInvokeTransitionFrame and saving
        ; incoming register values into it.
        ALLOC_PROBE_FRAME 0, {true}

        ; If the caller didn't provide a value for $threadReg then generate code to fetch the Thread* into x2.
        ; Record that x2 holds the Thread* in our local variable.
        IF "$threadReg" == ""
            ASSERT "$trashReg" != "x2"
__PPF_ThreadReg SETS "x2"
            INLINE_GETTHREAD $__PPF_ThreadReg, $trashReg
        ENDIF

        ; Perform the rest of the PInvokeTransitionFrame initialization.
        INIT_PROBE_FRAME $__PPF_ThreadReg, $trashReg, $savedRegsMask, $gcFlags, PROBE_FRAME_SIZE
        mov         $trashReg, sp
        str         $trashReg, [$__PPF_ThreadReg, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
    MEND

    ; Simple macro to use when PROLOG_PROBE_FRAME was used to set up and initialize the prolog and
    ; PInvokeTransitionFrame. This will define the epilog including a return via the restored LR.
    MACRO
        EPILOG_PROBE_FRAME

        FREE_PROBE_FRAME 0, {true}
        EPILOG_RETURN
    MEND

;; In order to avoid trashing VFP registers across the loop hijack we must save all user registers, so that
;; registers used by the loop being hijacked will not be affected. Unlike ARM32 where neon registers (NQ0, ..., NQ15)
;; are fully covered by the floating point registers D0 ... D31, we have 32 neon registers Q0, ... Q31 on ARM64
;; which are not fully covered by the register D0 ... D31. Therefore we must explicitly save all Q registers.
EXTRA_SAVE_SIZE equ (32*16)

    MACRO
        ALLOC_LOOP_HIJACK_FRAME

        PROLOG_STACK_ALLOC EXTRA_SAVE_SIZE

        ;; Save all neon registers
        PROLOG_NOP stp         q0, q1,   [sp]
        PROLOG_NOP stp         q2, q3,   [sp, #0x20]
        PROLOG_NOP stp         q4, q5,   [sp, #0x40]
        PROLOG_NOP stp         q6, q7,   [sp, #0x60]
        PROLOG_NOP stp         q8, q9,   [sp, #0x80]
        PROLOG_NOP stp         q10, q11, [sp, #0xA0]
        PROLOG_NOP stp         q12, q13, [sp, #0xC0]
        PROLOG_NOP stp         q14, q15, [sp, #0xE0]
        PROLOG_NOP stp         q16, q17, [sp, #0x100]
        PROLOG_NOP stp         q18, q19, [sp, #0x120]
        PROLOG_NOP stp         q20, q21, [sp, #0x140]
        PROLOG_NOP stp         q22, q23, [sp, #0x160]
        PROLOG_NOP stp         q24, q25, [sp, #0x180]
        PROLOG_NOP stp         q26, q27, [sp, #0x1A0]
        PROLOG_NOP stp         q28, q29, [sp, #0x1C0]
        PROLOG_NOP stp         q30, q31, [sp, #0x1E0]

        ALLOC_PROBE_FRAME 0, {false}
    MEND

    MACRO
        FREE_LOOP_HIJACK_FRAME

        FREE_PROBE_FRAME 0, {false}

        ;; restore all neon registers
        PROLOG_NOP ldp         q0, q1,   [sp]
        PROLOG_NOP ldp         q2, q3,   [sp, #0x20]
        PROLOG_NOP ldp         q4, q5,   [sp, #0x40]
        PROLOG_NOP ldp         q6, q7,   [sp, #0x60]
        PROLOG_NOP ldp         q8, q9,   [sp, #0x80]
        PROLOG_NOP ldp         q10, q11, [sp, #0xA0]
        PROLOG_NOP ldp         q12, q13, [sp, #0xC0]
        PROLOG_NOP ldp         q14, q15, [sp, #0xE0]
        PROLOG_NOP ldp         q16, q17, [sp, #0x100]
        PROLOG_NOP ldp         q18, q19, [sp, #0x120]
        PROLOG_NOP ldp         q20, q21, [sp, #0x140]
        PROLOG_NOP ldp         q22, q23, [sp, #0x160]
        PROLOG_NOP ldp         q24, q25, [sp, #0x180]
        PROLOG_NOP ldp         q26, q27, [sp, #0x1A0]
        PROLOG_NOP ldp         q28, q29, [sp, #0x1C0]
        PROLOG_NOP ldp         q30, q31, [sp, #0x1E0]

        EPILOG_STACK_FREE EXTRA_SAVE_SIZE
    MEND

;;
;; Macro to clear the hijack state. This is safe to do because the suspension code will not Unhijack this
;; thread if it finds it at an IP that isn't managed code.
;;
;; Register state on entry:
;;  x2: thread pointer
;;
;; Register state on exit:
;;
    MACRO
        ClearHijackState

        ASSERT OFFSETOF__Thread__m_pvHijackedReturnAddress == (OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation + 8)
        ;; Clear m_ppvHijackedReturnAddressLocation and m_pvHijackedReturnAddress
        stp         xzr, xzr, [x2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        ;; Clear m_uHijackedReturnValueFlags
        str         xzr, [x2, #OFFSETOF__Thread__m_uHijackedReturnValueFlags]
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

        ClearHijackState
    MEND

;;
;; Set the Thread state and wait for a GC to complete.
;;
;; Register state on entry:
;;  x4: thread pointer
;;
;; Register state on exit:
;;  x4: thread pointer
;;  All other registers trashed
;;

    EXTERN RhpWaitForGCNoAbort

    MACRO
        WaitForGCCompletion

        ldr         w2, [x4, #OFFSETOF__Thread__m_ThreadStateFlags]
        tst         w2, #TSF_SuppressGcStress__OR__TSF_DoNotTriggerGC
        bne         %ft0

        ldr         x9, [x4, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
        bl          RhpWaitForGCNoAbort
0
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
;;
;;
;; GC Probe Hijack targets
;;
;;
    EXTERN RhpPInvokeExceptionGuard

    NESTED_ENTRY RhpGcProbeHijackWrapper, .text, RhpPInvokeExceptionGuard
        HijackTargetFakeProlog

    LABELED_RETURN_ADDRESS RhpGcProbeHijack

        FixupHijackedCallstack
        orr         x12, x12, #DEFAULT_FRAME_SAVE_FLAGS
        b           RhpGcProbe
    NESTED_END RhpGcProbeHijackWrapper

#ifdef FEATURE_GC_STRESS
;;
;;
;; GC Stress Hijack targets
;;
;;
    LEAF_ENTRY RhpGcStressHijack
        FixupHijackedCallstack
        orr         x12, x12, #DEFAULT_FRAME_SAVE_FLAGS
        b           RhpGcStressProbe
    LEAF_END RhpGcStressHijack
;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack{Scalar|Object|Byref}.
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
        PROLOG_PROBE_FRAME x2, x3, x12,

        bl          $REDHAWKGCINTERFACE__STRESSGC

        EPILOG_PROBE_FRAME
    NESTED_END RhpGcStressProbe
#endif ;; FEATURE_GC_STRESS

    LEAF_ENTRY RhpGcProbe
        ldr         x3, =RhpTrapThreads
        ldr         w3, [x3]
        tbnz        x3, #TrapThreadsFlags_TrapThreads_Bit, RhpGcProbeRare
        ret
    LEAF_END RhpGcProbe

    EXTERN RhpThrowHwEx

    NESTED_ENTRY RhpGcProbeRare
        PROLOG_PROBE_FRAME x2, x3, x12,

        mov         x4, x2
        WaitForGCCompletion

        ldr         x2, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        tbnz        x2, #PTFF_THREAD_ABORT_BIT, %F1

        EPILOG_PROBE_FRAME

1
        FREE_PROBE_FRAME 0, {true}
        EPILOG_NOP mov w0, #STATUS_REDHAWK_THREAD_ABORT
        EPILOG_NOP mov x1, lr ;; return address as exception PC
        EPILOG_NOP b RhpThrowHwEx
    NESTED_END RhpGcProbeRare

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

    INLINE_GETTHREAD_CONSTANT_POOL

    end

