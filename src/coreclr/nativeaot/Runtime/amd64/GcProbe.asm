;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

;;
;; See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves RAX and accepts
;; the register bitmask
;;
;; On entry:
;;  - BITMASK: bitmask describing pushes, a volatile register
;;  - RAX: managed function return value, may be an object or byref
;;  - preserved regs: need to stay preserved, may contain objects or byrefs
;;
;; INVARIANTS
;; - The macro assumes it is called from a prolog, prior to a frame pointer being setup.
;; - All preserved registers remain unchanged from their values in managed code.
;;
PUSH_PROBE_FRAME macro threadReg, trashReg, BITMASK

    push_vol_reg    rax                         ; save RAX, it might contain an objectref
    lea             trashReg, [rsp + 10h]
    push_vol_reg    trashReg                    ; save caller's RSP
    push_nonvol_reg r15                         ; save preserved registers
    push_nonvol_reg r14                         ;   ..
    push_nonvol_reg r13                         ;   ..
    push_nonvol_reg r12                         ;   ..
    push_nonvol_reg rdi                         ;   ..
    push_nonvol_reg rsi                         ;   ..
    push_nonvol_reg rbx                         ;   ..
    push_vol_reg    BITMASK                     ; save the register bitmask passed in by caller
    push_vol_reg    threadReg                   ; Thread * (unused by stackwalker)
    push_nonvol_reg rbp                         ; save caller's RBP
    mov             trashReg, [rsp + 12*8]      ; Find the return address
    push_vol_reg    trashReg                    ; save m_RIP
    lea             trashReg, [rsp + 0]         ; trashReg == address of frame

    ;; allocate scratch space and any required alignment
    alloc_stack     20h + 10h

    ;; save xmm0 in case it's being used as a return value
    movdqa          [rsp + 20h], xmm0

    ;; link the frame into the Thread
    mov             [threadReg + OFFSETOF__Thread__m_pDeferredTransitionFrame], trashReg
endm

;;
;; Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
;; registers and return value to their values from before the probe was called (while also updating any
;; object refs or byrefs).
;;
POP_PROBE_FRAME macro 
    movdqa      xmm0, [rsp + 20h]
    add         rsp, 20h + 10h + 8  ; deallocate stack and discard saved m_RIP
    pop         rbp
    pop         rax     ; discard Thread*
    pop         rax     ; discard BITMASK
    pop         rbx
    pop         rsi
    pop         rdi
    pop         r12
    pop         r13
    pop         r14
    pop         r15
    pop         rax     ; discard caller RSP
    pop         rax
endm

;;
;; The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
;; clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  RDX: thread pointer
;;  RCX: return value flags
;;  RAX: preserved, other volatile regs trashed
;;
FixupHijackedCallstack macro
        ;; rdx <- GetThread(), TRASHES rcx
        INLINE_GETTHREAD rdx, rcx

        ;; Fix the stack by pushing the original return address
        mov         rcx, [rdx + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        push        rcx

        ;; Fetch the return address flags
        mov         rcx, [rdx + OFFSETOF__Thread__m_uHijackedReturnValueFlags]

        ;; Clear hijack state
        xor         r9, r9
        mov         [rdx + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation], r9
        mov         [rdx + OFFSETOF__Thread__m_pvHijackedReturnAddress], r9
        mov         [rdx + OFFSETOF__Thread__m_uHijackedReturnValueFlags], r9
endm

EXTERN RhpPInvokeExceptionGuard : PROC

;;
;; GC Probe Hijack target
;;
NESTED_ENTRY RhpGcProbeHijack, _TEXT, RhpPInvokeExceptionGuard
        END_PROLOGUE
        FixupHijackedCallstack

        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jnz         @f
        ret
@@:
        or          ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX
        jmp         RhpWaitForGC
NESTED_END RhpGcProbeHijack, _TEXT

EXTERN RhpThrowHwEx : PROC

NESTED_ENTRY RhpWaitForGC, _TEXT
        PUSH_PROBE_FRAME rdx, rax, rcx
        END_PROLOGUE

        mov         rbx, rdx
        mov         rcx, [rbx + OFFSETOF__Thread__m_pDeferredTransitionFrame]
        call        RhpWaitForGC2

        mov         rax, [rbx + OFFSETOF__Thread__m_pDeferredTransitionFrame]
        test        dword ptr [rax + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_THREAD_ABORT
        jnz         Abort
        POP_PROBE_FRAME
        ret
Abort:
        POP_PROBE_FRAME
        mov         rcx, STATUS_REDHAWK_THREAD_ABORT
        pop         rdx         ;; return address as exception RIP
        jmp         RhpThrowHwEx ;; Throw the ThreadAbortException as a special kind of hardware exception

NESTED_END RhpWaitForGC, _TEXT

LEAF_ENTRY RhpGcPoll, _TEXT
        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        jmp         RhpGcPollRare
LEAF_END RhpGcPoll, _TEXT

NESTED_ENTRY RhpGcPollRare, _TEXT
        PUSH_COOP_PINVOKE_FRAME rcx
        END_PROLOGUE
        call        RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        ret
NESTED_END RhpGcPollRare, _TEXT



ifdef FEATURE_GC_STRESS

;;
;; GC Stress Hijack targets
;;
LEAF_ENTRY RhpGcStressHijack, _TEXT
        FixupHijackedCallstack
        or          ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX
        jmp         RhpGcStressProbe
LEAF_END RhpGcStressHijack, _TEXT

;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack{Scalar|Object|Byref}.
;; This worker performs the GC Stress work and returns to the original return address.
;;
;; Register state on entry:
;;  RDX: thread pointer
;;  RCX: register bitmask
;;
;; Register state on exit:
;;  Scratch registers, except for RAX, have been trashed
;;  All other registers restored as they were when the hijack was first reached.
;;
NESTED_ENTRY RhpGcStressProbe, _TEXT
        PUSH_PROBE_FRAME rdx, rax, rcx
        END_PROLOGUE

        call        RhpStressGc

        POP_PROBE_FRAME
        ret
NESTED_END RhpGcStressProbe, _TEXT

;; PAL_LIMITED_CONTEXT, 6 xmm regs to save, 2 scratch regs to save, plus 20h bytes for scratch space
RhpHijackForGcStress_FrameSize equ SIZEOF__PAL_LIMITED_CONTEXT + 6*10h + 2*8h + 20h

; -----------------------------------------------------------------------------------------------------------
; RhpHijackForGcStress
;
; Called at the beginning of the epilog when a method is bound with /gcstress
;
; N.B. -- Leaf frames may not have aligned the stack or reserved any scratch space on the stack.  Also, in
;         order to have a resonable stacktrace in the debugger, we must use the .pushframe unwind directive.
;
; N.B. #2 -- The "EH jump epilog" codegen depends on rcx/rdx being preserved across this call.  We currently
;            will trash R8-R11, but we can do better, if necessary.
;
NESTED_ENTRY RhpHijackForGcStress, _TEXT

        lea         r10, [rsp+8]        ;; save the original RSP (prior to call)
        mov         r11, [rsp]          ;; get the return address

        ;; Align the stack
        and         rsp, -16

        ;; Push the expected "machine frame" for the unwinder to see.  All that it looks at is the RSP and
        ;; RIP, so we push zero for the others.
        xor     r8, r8
        push    r8              ;; just aligning the stack
        push    r8              ;; SS
        push    r10             ;; original RSP
        push    r8              ;; EFLAGS
        push    r8              ;; CS
        push    r11             ;; return address

        ; Tell the unwinder that the frame is there now
        .pushframe

        alloc_stack     RhpHijackForGcStress_FrameSize
        END_PROLOGUE

        ;; Save xmm scratch regs -- this is probably overkill, only the return value reg is
        ;; likely to be interesting at this point, but it's a bit ambiguous.
        movdqa      [rsp + 20h + 0*10h], xmm0
        movdqa      [rsp + 20h + 1*10h], xmm1
        movdqa      [rsp + 20h + 2*10h], xmm2
        movdqa      [rsp + 20h + 3*10h], xmm3
        movdqa      [rsp + 20h + 4*10h], xmm4
        movdqa      [rsp + 20h + 5*10h], xmm5

        mov         [rsp + 20h + 6*10h + 0*8h], rcx
        mov         [rsp + 20h + 6*10h + 1*8h], rdx

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;
        ;; This is very likely overkill since the calculation of the return address should only need RSP and
        ;; RBP, but this is test code, so I'm not too worried about efficiency.
        ;;
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__IP],  r11     ; rip at callsite
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsp], r10     ; rsp at callsite
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rbp], rbp
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rdi], rdi
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsi], rsi
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rax], rax
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rbx], rbx

        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R12], r12
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R13], r13
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R14], r14
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R15], r15

        lea         rcx, [rsp + 20h + 6*10h + 2*8h]   ;; address of PAL_LIMITED_CONTEXT
        call        THREAD__HIJACKFORGCSTRESS

        ;; Note: we only restore the scratch registers here. No GC has occurred, so restoring
        ;; the callee saved ones is unnecessary.
        mov         rax, [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rax]
        mov         rcx, [rsp + 20h + 6*10h + 0*8h]
        mov         rdx, [rsp + 20h + 6*10h + 1*8h]

        ;; Restore xmm scratch regs
        movdqa      xmm0, [rsp + 20h + 0*10h]
        movdqa      xmm1, [rsp + 20h + 1*10h]
        movdqa      xmm2, [rsp + 20h + 2*10h]
        movdqa      xmm3, [rsp + 20h + 3*10h]
        movdqa      xmm4, [rsp + 20h + 4*10h]
        movdqa      xmm5, [rsp + 20h + 5*10h]

        ;; epilog
        mov         r10, [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsp]
        lea         rsp, [r10 - 8]              ;; adjust RSP to point back at the return address
        ret
NESTED_END RhpHijackForGcStress, _TEXT

g_pTheRuntimeInstance equ ?g_pTheRuntimeInstance@@3PEAVRuntimeInstance@@EA
EXTERN g_pTheRuntimeInstance : QWORD
RuntimeInstance__ShouldHijackLoopForGcStress equ ?ShouldHijackLoopForGcStress@RuntimeInstance@@QEAA_N_K@Z
EXTERN RuntimeInstance__ShouldHijackLoopForGcStress : PROC

EXTERN g_fGcStressStarted : DWORD

;;
;; INVARIANT: Don't trash the argument registers, the binder codegen depends on this.
;;
LEAF_ENTRY RhpSuppressGcStress, _TEXT

        INLINE_GETTHREAD    rax, r10
   lock or          dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], TSF_SuppressGcStress
        ret

LEAF_END RhpSuppressGcStress, _TEXT

endif ;; FEATURE_GC_STRESS



        end
