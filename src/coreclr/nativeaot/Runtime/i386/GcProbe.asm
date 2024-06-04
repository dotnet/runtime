;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .xmm
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

DEFAULT_PROBE_SAVE_FLAGS        equ PTFF_SAVE_ALL_PRESERVED + PTFF_SAVE_RSP

;;
;; The prolog for all GC suspension hijackes (normal and stress). Sets up an EBP frame,
;; fixes up the hijacked return address, and clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  EAX: not trashed or saved
;;  EBP: new EBP frame with correct return address
;;  ESP: points to saved scratch registers (ECX & EDX)
;;  ECX: return value flags
;;  EDX: thread pointer
;;
HijackFixupProlog macro
        push        eax         ; save a slot for the repaired return address
        push        ebp
        mov         ebp, esp
        push        ecx         ; save scratch registers
        push        edx         ; save scratch registers

        ;; edx <- GetThread(), TRASHES ecx
        INLINE_GETTHREAD edx, ecx

        ;;
        ;; Fix the stack by pushing the original return address
        ;;
        mov         ecx, [edx + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        mov         [ebp + 4], ecx

        ;; Fetch the return address flags
        mov         ecx, [edx + OFFSETOF__Thread__m_uHijackedReturnValueFlags]

        ;;
        ;; Clear hijack state
        ;;
        mov         dword ptr [edx + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation], 0
        mov         dword ptr [edx + OFFSETOF__Thread__m_pvHijackedReturnAddress], 0
        mov         dword ptr [edx + OFFSETOF__Thread__m_uHijackedReturnValueFlags], 0

endm

;;
;; Epilog for the normal and GC stress hijack functions. Restores scratch registers
;; and returns to the original return address.
;;
;; Register state on entry:
;;  ESP: points to saved scratch registers
;;  EBP: ebp frame
;;  ECX, EDX: trashed
;;  All other registers correct for return to the original return address.
;;
;; Register state on exit:
;;  All registers restored as they were when the hijack was first reached.
;;
HijackFixupEpilog macro
        pop         edx
        pop         ecx
        pop         ebp
        ret
endm

;;
;; Sets up a PInvokeTranstionFrame with room for all registers.
;;
;; Register state on entry:
;;  EDX: thread pointer
;;  BITMASK_REG_OR_VALUE: register bitmask, PTTR_SAVE_ALL_PRESERVED at a minimum
;;  EBP: ebp frame setup with correct return address
;;  ESP: points to saved scratch registers
;;
;; Register state on exit:
;;  ESP: pointer to a PInvokeTransitionFrame on the stack
;;  EBX: thread pointer
;;  EAX: trashed
;;  ESI, EDI, EBX, EAX all saved in the frame
;;
;;  ECX is NOT trashed if BITMASK_REG_OR_VALUE is a literal value and not a register
;;
PushProbeFrame macro BITMASK_REG_OR_VALUE
        push        eax                     ; EAX
        lea         eax, [ebp + 8]                      ; get caller ESP
        push        eax                     ; ESP
        push        edi                     ; EDI
        push        esi                     ; ESI
        push        ebx                     ; EBX
        push        BITMASK_REG_OR_VALUE    ; register bitmask
ifdef _DEBUG
        mov         eax, BITMASK_REG_OR_VALUE
        and         eax, DEFAULT_PROBE_SAVE_FLAGS
        cmp         eax, DEFAULT_PROBE_SAVE_FLAGS ; make sure we have at least the flags to match what the macro pushes
        je          @F
        call        RhDebugBreak
@@:
endif ;; _DEBUG
        push        edx                     ; Thread *
        mov         eax, [ebp + 0]                      ; find previous EBP value
        push        eax                     ; m_FramePointer
        mov         eax, [ebp + 4]                      ; get return address
        push        eax                     ; m_RIP

        mov         ebx, edx                            ; save Thread pointer for later
endm

;;
;; Pops off the PInvokeTransitionFrame setup in PushProbeFrame above, restoring all registers.
;;
;; Register state on entry:
;;  ESP: pointer to a PInvokeTransitionFrame on the stack
;;
;; Register state on exit:
;;  ESP: points to saved scratch registers, PInvokeTransitionFrame removed
;;  EBX: restored
;;  ESI: restored
;;  EDI: restored
;;  EAX: restored
;;
PopProbeFrame macro
        add         esp, 4*4h
        pop         ebx
        pop         esi
        pop         edi
        pop         eax     ; discard ESP
        pop         eax
endm

RhpThrowHwEx equ @RhpThrowHwEx@8
extern RhpThrowHwEx : proc

;;
;; Main worker for our GC probes.  Do not call directly!! This assumes that HijackFixupProlog has been done.
;; Instead, go through RhpGcProbeHijack* or RhpGcStressHijack*. This waits for the
;; GC to complete then returns to the original return address.
;;
;; Register state on entry:
;;  ECX: register bitmask
;;  EDX: thread pointer
;;  EBP: EBP frame
;;  ESP: scratch registers pushed (ECX & EDX)
;;
;; Register state on exit:
;;  All registers restored as they were when the hijack was first reached.
;;
RhpWaitForGC  proc
        PushProbeFrame ecx      ; bitmask in ECX

        mov         ecx, esp
        call        RhpWaitForGC2

        mov         edx, [esp + OFFSETOF__PInvokeTransitionFrame__m_Flags]
        ;;
        ;; Restore preserved registers -- they may have been updated by GC
        ;;
        PopProbeFrame

        test        edx, PTFF_THREAD_ABORT
        jnz         Abort

        HijackFixupEpilog
Abort:
        mov         ecx, STATUS_REDHAWK_THREAD_ABORT
        pop         edx
        pop         eax         ;; ecx was pushed here, but we don't care for its value
        pop         ebp
        pop         edx         ;; return address as exception RIP
        jmp         RhpThrowHwEx

RhpWaitForGC  endp

RhpGcPoll  proc
        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        jmp         RhpGcPollRare

RhpGcPoll  endp

RhpGcPollRare  proc
        push        ebp
        mov         ebp, esp
        PUSH_COOP_PINVOKE_FRAME ecx
        call        RhpGcPoll2
        POP_COOP_PINVOKE_FRAME
        pop         ebp
        ret
RhpGcPollRare  endp

ifdef FEATURE_GC_STRESS
;;
;; Set the Thread state and invoke RhpStressGC().
;;
;; Assumes EBX is the Thread pointer.
;;
;; Register state on entry:
;;  EBX: thread pointer
;;  EBP: EBP frame
;;  ESP: pointer to a PInvokeTransitionFrame on the stack
;;
;; Register state on exit:
;;  ESP: pointer to a PInvokeTransitionFrame on the stack
;;  EBP: EBP frame
;;  All other registers trashed
;;
StressGC macro
        mov         [ebx + OFFSETOF__Thread__m_pDeferredTransitionFrame], esp
        call        RhpStressGc
endm

;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack. This performs the GC Stress
;; work and returns to the original return address.
;;
;; Register state on entry:
;;  EDX: thread pointer
;;  ECX: register bitmask
;;  EBP: EBP frame
;;  ESP: scratch registers pushed (ECX and EDX)
;;
;; Register state on exit:
;;  All registers restored as they were when the hijack was first reached.
;;
RhpGcStressProbe  proc
        PushProbeFrame ecx      ; bitmask in ECX

        StressGC

        ;;
        ;; Restore preserved registers -- they may have been updated by GC
        ;;
        PopProbeFrame

        HijackFixupEpilog

RhpGcStressProbe  endp

endif ;; FEATURE_GC_STRESS

FASTCALL_FUNC RhpGcProbeHijack, 0
        HijackFixupProlog
        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jnz         WaitForGC
        HijackFixupEpilog

WaitForGC:
        or          ecx, DEFAULT_PROBE_SAVE_FLAGS + PTFF_SAVE_RAX
        jmp         RhpWaitForGC

FASTCALL_ENDFUNC

ifdef FEATURE_GC_STRESS
FASTCALL_FUNC RhpGcStressHijack, 0

        HijackFixupProlog
        or          ecx, DEFAULT_PROBE_SAVE_FLAGS + PTFF_SAVE_RAX
        jmp         RhpGcStressProbe

FASTCALL_ENDFUNC

FASTCALL_FUNC RhpHijackForGcStress, 0
        push        ebp
        mov         ebp, esp

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;

        push        edx
        push        ecx
        push        ebx
        push        eax
        push        esi
        push        edi

        mov         eax, [ebp]
        push        eax             ;; (caller) Ebp
        lea         eax, [ebp + 8]
        push        eax             ;; Esp
        mov         eax, [ebp + 4]
        push        eax             ;; Eip

        push        esp        ;; address of PAL_LIMITED_CONTEXT
        call        THREAD__HIJACKFORGCSTRESS

        ;; Note: we only restore the scratch registers here. No GC has occurred, so restoring
        ;; the callee saved ones is unnecessary.
        add         esp, 14h
        pop         eax
        pop         ebx
        pop         ecx
        pop         edx
        pop         ebp
        ret
FASTCALL_ENDFUNC
endif ;; FEATURE_GC_STRESS

        end
