;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

;;
;; See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves volatile argument registers
;; and accepts the register bitmask
;;
;; On entry:
;;  - BITMASK: bitmask describing pushes, a volatile register
;;  - RAX: managed function return value, may be an object or byref
;;  - RCX: managed function return value (async continuation), may be an object
;;  - RDX, R8, R9: may contain objects or byrefs at the hijack point
;;  - preserved regs: need to stay preserved, may contain objects or byrefs
;;
;; INVARIANTS
;; - The macro assumes it is called from a prolog, prior to a frame pointer being setup.
;; - All preserved registers remain unchanged from their values in managed code.
;;
PUSH_PROBE_FRAME macro threadReg, trashReg, BITMASK

    push_vol_reg    r9                          ; save R9, it might contain an objectref
    push_vol_reg    r8                          ; save R8, it might contain an objectref
    push_vol_reg    rdx                         ; save RDX, it might contain an objectref
    push_vol_reg    rcx                         ; save RCX, it might contain an objectref (async continuation)
    push_vol_reg    rax                         ; save RAX, it might contain an objectref
    lea             trashReg, [rsp + 30h]
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
    mov             trashReg, [rsp + 16*8]      ; Find the return address
    push_vol_reg    trashReg                    ; save m_RIP
    lea             trashReg, [rsp + 0]         ; trashReg == address of frame

    ;; allocate scratch space (20h home space + 40h for xmm0..xmm3)
    alloc_stack     20h + 40h

    ;; save xmm argument registers in case they contain live values at the hijack point
    movdqa          [rsp + 20h + 00h], xmm0
    movdqa          [rsp + 20h + 10h], xmm1
    movdqa          [rsp + 20h + 20h], xmm2
    movdqa          [rsp + 20h + 30h], xmm3

    ;; link the frame into the Thread
    mov             [threadReg + OFFSETOF__Thread__m_pDeferredTransitionFrame], trashReg
endm

;;
;; Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
;; registers and return value to their values from before the probe was called (while also updating any
;; object refs or byrefs).
;;
POP_PROBE_FRAME macro
    movdqa      xmm0, [rsp + 20h + 00h]
    movdqa      xmm1, [rsp + 20h + 10h]
    movdqa      xmm2, [rsp + 20h + 20h]
    movdqa      xmm3, [rsp + 20h + 30h]
    add         rsp, 20h + 40h + 8  ; deallocate scratch space and discard saved m_RIP
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
    pop         rcx
    pop         rdx
    pop         r8
    pop         r9
endm

;;
;; The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
;; clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  R10: thread pointer
;;  RAX/RCX/RDX/R8/R9: preserved, R11 trashed
;;
FixupHijackedCallstack macro
        ;; r10 <- GetThread(), TRASHES r11
        INLINE_GETTHREAD r10, r11

        ;; Fix the stack by pushing the original return address
        mov         r11, [r10 + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        push        r11

        ;; Clear hijack state
        xor         r11, r11
        mov         [r10 + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation], r11
        mov         [r10 + OFFSETOF__Thread__m_pvHijackedReturnAddress], r11
endm

;;
;; GC Probe Hijack target
;;
NESTED_ENTRY RhpGcProbeHijack, _TEXT
        END_PROLOGUE
        FixupHijackedCallstack

        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jnz         @f
        ret
@@:
        mov         r11d, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_SAVE_RCX + PTFF_SAVE_RDX + PTFF_SAVE_R8 + PTFF_SAVE_R9 + PTFF_THREAD_HIJACK
        jmp         RhpWaitForGC
NESTED_END RhpGcProbeHijack, _TEXT

NESTED_ENTRY RhpWaitForGC, _TEXT
        PUSH_PROBE_FRAME r10, rax, r11
        END_PROLOGUE

        mov         rbx, r10
        mov         rcx, [rbx + OFFSETOF__Thread__m_pDeferredTransitionFrame]
        call        RhpWaitForGC2

        POP_PROBE_FRAME
        ret

NESTED_END RhpWaitForGC, _TEXT

NESTED_ENTRY RhpGcPollRare, _TEXT
        PUSH_COOP_PINVOKE_FRAME rcx
        call        RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        ret
NESTED_END RhpGcPollRare, _TEXT

        end
