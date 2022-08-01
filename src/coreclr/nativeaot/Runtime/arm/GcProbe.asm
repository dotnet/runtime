;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

    ;; ARM64TODO: do same fix here as on Arm64?
    SETALIAS    g_fGcStressStarted, ?g_GCShadow@@3PAEA

    EXTERN      $g_fGcStressStarted

PROBE_SAVE_FLAGS_EVERYTHING     equ DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_ALL_SCRATCH
PROBE_SAVE_FLAGS_R0_IS_GCREF    equ DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R0 + PTFF_R0_IS_GCREF


    ;; Build a map of symbols representing offsets into a transition frame (see PInvokeTransitionFrame in
    ;; rhbinder.h and keep these two in sync.
    map 0
m_ChainPointer  field 4         ; r11 - OS frame chain used for quick stackwalks
m_RIP           field 4         ; lr
m_FramePointer  field 4         ; r7
m_pThread       field 4
m_Flags         field 4         ; bitmask of saved registers
m_PreservedRegs field (4 * 6)   ; r4-r6,r8-r10
m_CallersSP     field 4         ; sp at routine entry
m_SavedR0       field 4         ; r0
m_VolatileRegs  field (4 * 4)   ; r1-r3,lr
m_ReturnVfpRegs field (8 * 4)   ; d0-d3, not really part of the struct
m_SavedAPSR     field 4         ; saved condition codes
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
    ;; lead to upto five additional register saves (r0-r3,r12) or 20 bytes of stack space. I have done no
    ;; analysis to see whether any of the worst cases occur on performance sensitive paths and whether the
    ;; additional saves will show any measurable degradation.

    ;; Perform the parts of setting up a probe frame that can occur during the prolog (and indeed this macro
    ;; can only be called from within the prolog).
    MACRO
        ALLOC_PROBE_FRAME

        PROLOG_STACK_ALLOC  4                   ; Space for saved APSR
        PROLOG_VPUSH        {d0-d3}             ; Save floating point return registers
        PROLOG_PUSH         {r0-r3,lr}          ; Save volatile registers
        PROLOG_STACK_ALLOC  4                   ; Space for caller's SP
        PROLOG_PUSH         {r4-r6,r8-r10}      ; Save non-volatile registers
        PROLOG_STACK_ALLOC  8                   ; Space for flags and Thread*
        PROLOG_PUSH         {r7}                ; Save caller's frame pointer
        PROLOG_PUSH         {r11,lr}            ; Save frame-chain pointer and return address
    MEND

    ;; Undo the effects of an ALLOC_PROBE_FRAME. This may only be called within an epilog. Note that all
    ;; registers are restored (apart for sp and pc), even volatiles.
    MACRO
        FREE_PROBE_FRAME

        EPILOG_POP          {r11,lr}            ; Restore frame-chain pointer and return address
        EPILOG_POP          {r7}                ; Restore caller's frame pointer
        EPILOG_STACK_FREE   8                   ; Discard flags and Thread*
        EPILOG_POP          {r4-r6,r8-r10}      ; Restore non-volatile registers
        EPILOG_STACK_FREE   4                   ; Discard caller's SP
        EPILOG_POP          {r0-r3,lr}          ; Restore volatile registers
        EPILOG_VPOP         {d0-d3}             ; Restore floating point return registers
        EPILOG_STACK_FREE   4                   ; Space for saved APSR
    MEND

    ;; Complete the setup of a probe frame allocated with ALLOC_PROBE_FRAME with the initialization that can
    ;; occur only outside the prolog (includes linking the frame to the current Thread). This macro assumes SP
    ;; is invariant outside of the prolog.
    ;;
    ;;  $threadReg  : register containing the Thread* (this will be preserved)
    ;;  $trashReg   : register that can be trashed by this macro
    ;;  $BITMASK    : value to initialize m_Flags field with (register or #constant)
    ;;  $frameSize  : total size of the method's stack frame (including probe frame size)
    MACRO
        INIT_PROBE_FRAME $threadReg, $trashReg, $BITMASK, $frameSize

        str         $threadReg, [sp, #m_pThread]    ; Thread *
        mov         $trashReg, $BITMASK             ; Bitmask of preserved registers
        str         $trashReg, [sp, #m_Flags]
        add         $trashReg, sp, #$frameSize
        str         $trashReg, [sp, #m_CallersSP]
    MEND

    ;; Simple macro to use when setting up the probe frame can comprise the entire prolog. Call this macro
    ;; first in the method (no further prolog instructions can be added after this).
    ;;
    ;;  $threadReg  : register containing the Thread* (this will be preserved). If defaulted (specify |) then
    ;;                the current thread will be calculated inline into r2 ($trashReg must not equal r2 in
    ;;                this case)
    ;;  $trashReg   : register that can be trashed by this macro
    ;;  $BITMASK    : value to initialize m_Flags field with (register or #constant)
    MACRO
        PROLOG_PROBE_FRAME $threadReg, $trashReg, $BITMASK

        ; Local string tracking the name of the register in which the Thread* is kept. Defaults to the value
        ; of $threadReg.
        LCLS __PPF_ThreadReg
__PPF_ThreadReg SETS "$threadReg"

        ; Define the method prolog, allocating enough stack space for the PInvokeTransitionFrame and saving
        ; incoming register values into it.
        ALLOC_PROBE_FRAME

        ; If the caller didn't provide a value for $threadReg then generate code to fetch the Thread* into r2.
        ; Record that r2 holds the Thread* in our local variable.
        IF "$threadReg" == ""
            ASSERT "$trashReg" != "r2"
__PPF_ThreadReg SETS "r2"
            INLINE_GETTHREAD $__PPF_ThreadReg, $trashReg
        ENDIF

        ; Perform the rest of the PInvokeTransitionFrame initialization.
        INIT_PROBE_FRAME $__PPF_ThreadReg, $trashReg, $BITMASK, PROBE_FRAME_SIZE
        str         sp, [$__PPF_ThreadReg, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
    MEND

    ; Simple macro to use when PROLOG_PROBE_FRAME was used to set up and initialize the prolog and
    ; PInvokeTransitionFrame. This will define the epilog including a return via the restored LR.
    MACRO
        EPILOG_PROBE_FRAME

        FREE_PROBE_FRAME
        EPILOG_RETURN
    MEND


;;
;; The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
;; clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  r2: thread pointer
;;  r3: trashed
;;  r12: trashed
;;
    MACRO
        FixupHijackedCallstack

        ;; r2 <- GetThread(), TRASHES r3
        INLINE_GETTHREAD r2, r3

        ;;
        ;; Fix the stack by restoring the original return address
        ;;
        ldr         lr, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

        ;; Clear hijack state
        mov         r12, #0
        str         r12, [r2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         r12, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

    MEND


    MACRO
        HijackTargetFakeProlog

        ;; This is a fake entrypoint for the method that 'tricks' the OS into calling our personality routine.
        ;; The code here should never be executed, and the unwind info is bogus, but we don't mind since the
        ;; stack is broken by the hijack anyway until after we fix it below.
        PROLOG_PUSH {lr}
        nop                     ; We also need a nop here to simulate the implied bl instruction.  Without
                                ; this, an OS-applied -2 will back up into the method prolog and the unwind
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

        ldr         r3, =RhpTrapThreads
        ldr         r3, [r3]
        tst         r3, #TrapThreadsFlags_TrapThreads
        bne         %0
        bx          lr
0
        mov         r12, #(DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R0)
        b           RhpWaitForGC
    NESTED_END RhpGcProbeHijackWrapper

#ifdef FEATURE_GC_STRESS
;;
;;
;; GC Stress Hijack targets
;;
;;
    LEAF_ENTRY RhpGcStressHijack
        FixupHijackedCallstack
        mov         r12, #DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_R0
        b           RhpGcStressProbe
    LEAF_END RhpGcStressHijack

;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack.
;; This worker performs the GC Stress work and returns to the original return address.
;;
;; Register state on entry:
;;  r0: hijacked function return value
;;  r1: hijacked function return value
;;  r2: thread pointer
;;  r12: register bitmask
;;
;; Register state on exit:
;;  Scratch registers, except for r0, have been trashed
;;  All other registers restored as they were when the hijack was first reached.
;;
    NESTED_ENTRY RhpGcStressProbe
        PROLOG_PROBE_FRAME r2, r3, r12

        bl          $REDHAWKGCINTERFACE__STRESSGC

        EPILOG_PROBE_FRAME
    NESTED_END RhpGcStressProbe
#endif ;; FEATURE_GC_STRESS

    EXTERN RhpThrowHwEx

    NESTED_ENTRY RhpWaitForGC
        PROLOG_PROBE_FRAME r2, r3, r12

        ldr         r0, [r2, #OFFSETOF__Thread__m_pDeferredTransitionFrame] 
        bl          RhpWaitForGC2

        ldr         r2, [sp, #OFFSETOF__PInvokeTransitionFrame__m_Flags]
        tst         r2, #PTFF_THREAD_ABORT
        bne         %1

        EPILOG_PROBE_FRAME

1
        FREE_PROBE_FRAME
        EPILOG_NOP mov         r0, #STATUS_REDHAWK_THREAD_ABORT
        EPILOG_NOP mov         r1, lr ;; return address as exception PC
        EPILOG_BRANCH RhpThrowHwEx

    NESTED_END RhpWaitForGC

    LEAF_ENTRY RhpGcPoll
        ldr         r0, =RhpTrapThreads
        ldr         r0, [r0]
        tst         r0, #TrapThreadsFlags_TrapThreads
        bne         RhpGcPollRare
        bx          lr
    LEAF_END RhpGcPoll

    NESTED_ENTRY RhpGcPollRare
        PROLOG_PROBE_FRAME |, r3, #PROBE_SAVE_FLAGS_EVERYTHING

        ; Unhijack this thread, if necessary.
        INLINE_THREAD_UNHIJACK  r2, r0, r1       ;; trashes r0, r1

        ldr         r0, [r2, #OFFSETOF__Thread__m_pDeferredTransitionFrame]
        bl          RhpWaitForGC2

        EPILOG_PROBE_FRAME
    NESTED_END RhpGcPollRare

    LEAF_ENTRY RhpGcPollStress
        ;
        ; loop hijacking is used instead
        ;
        __debugbreak

    LEAF_END RhpGcPollStress


#ifdef FEATURE_GC_STRESS
    NESTED_ENTRY RhpHijackForGcStress
        PROLOG_PUSH {r0,r1}     ; Save return value
        PROLOG_VPUSH {d0-d3}    ; Save VFP return value

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;
        ;; This is very likely overkill since the calculation of the return address should only need SP and
        ;; LR, but this is test code, so I'm not too worried about efficiency.
        ;;
        ;; Setup a PAL_LIMITED_CONTEXT on the stack {
        ;; we'll need to reserve the size of the D registers in the context
        ;; compute in the funny way below to include any padding between LR and D
DREG_SZ equ     (SIZEOF__PAL_LIMITED_CONTEXT - (OFFSETOF__PAL_LIMITED_CONTEXT__LR + 4))

        PROLOG_STACK_ALLOC  DREG_SZ ;; Reserve space for d8-d15
        PROLOG_PUSH {r0,lr}         ;; Reserve space for SP and store LR
        PROLOG_PUSH {r0,r4-r11,lr}
        ;; } end PAL_LIMITED_CONTEXT

        ;; Compute and save SP at callsite.
        add         r0, sp, #(SIZEOF__PAL_LIMITED_CONTEXT + 0x20 + 8)   ;; +0x20 for vpush {d0-d3}, +8 for push {r0,r1}
        str         r0, [sp, #OFFSETOF__PAL_LIMITED_CONTEXT__SP]

        mov         r0, sp      ; Address of PAL_LIMITED_CONTEXT
        bl          $THREAD__HIJACKFORGCSTRESS

        ;; epilog
        EPILOG_POP  {r0,r4-r11,lr}
        EPILOG_STACK_FREE DREG_SZ + 8 ; Discard saved SP and LR and space for d8-d15
        EPILOG_VPOP {d0-d3}             ; Restore VFP return value
        EPILOG_POP  {r0,r1}             ; Restore return value
        bx          lr
    NESTED_END RhpHijackForGcStress
#endif ;; FEATURE_GC_STRESS

#ifdef FEATURE_GC_STRESS

;;
;; INVARIANT: Don't trash the argument registers, the binder codegen depends on this.
;;
    LEAF_ENTRY RhpSuppressGcStress

        push        {r0-r2}
        INLINE_GETTHREAD    r0, r1

Retry
        ldrex       r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        orr         r1, #TSF_SuppressGcStress
        strex       r2, r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        cbz         r2, Success
        b           Retry

Success
        pop         {r0-r2}
        bx          lr

    LEAF_END RhpSuppressGcStress
#endif ;; FEATURE_GC_STRESS

        INLINE_GETTHREAD_CONSTANT_POOL

        end
