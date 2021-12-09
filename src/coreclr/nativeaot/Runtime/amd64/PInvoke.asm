;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include asmmacros.inc

extern RhpReversePInvokeBadTransition : proc


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForSuspend -- rare path for RhpPInvoke and RhpReversePInvokeReturn
;;
;;
;; INPUT: none
;;
;; TRASHES: none
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpWaitForSuspend, _TEXT
        push_vol_reg    rax
        alloc_stack     60h

        ; save the arg regs in the caller's scratch space
        save_reg_postrsp        rcx, 70h
        save_reg_postrsp        rdx, 78h
        save_reg_postrsp        r8, 80h
        save_reg_postrsp        r9, 88h

        ; save the FP arg regs in our stack frame
        save_xmm128_postrsp     xmm0, (20h + 0*10h)
        save_xmm128_postrsp     xmm1, (20h + 1*10h)
        save_xmm128_postrsp     xmm2, (20h + 2*10h)
        save_xmm128_postrsp     xmm3, (20h + 3*10h)

        END_PROLOGUE

        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jz          NoWait

        call        RhpWaitForSuspend2

NoWait:
        movdqa      xmm0, [rsp + 20h + 0*10h]
        movdqa      xmm1, [rsp + 20h + 1*10h]
        movdqa      xmm2, [rsp + 20h + 2*10h]
        movdqa      xmm3, [rsp + 20h + 3*10h]

        mov         rcx, [rsp + 70h]
        mov         rdx, [rsp + 78h]
        mov         r8,  [rsp + 80h]
        mov         r9,  [rsp + 88h]

        add         rsp, 60h
        pop         rax
        ret

NESTED_END RhpWaitForSuspend, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGCNoAbort -- rare path for RhpPInvokeReturn
;;
;;
;; INPUT: RCX: transition frame
;;
;; TRASHES: RCX, RDX, R8, R9, R10, R11
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpWaitForGCNoAbort, _TEXT
        push_vol_reg    rax                 ; don't trash the integer return value
        alloc_stack     30h
        movdqa          [rsp + 20h], xmm0   ; don't trash the FP return value
        END_PROLOGUE

        mov         rdx, [rcx + OFFSETOF__PInvokeTransitionFrame__m_pThread]

        test        dword ptr [rdx + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc
        jnz         Done

        ; passing transition frame pointer in rcx
        call        RhpWaitForGC2

Done:
        movdqa      xmm0, [rsp + 20h]
        add         rsp, 30h
        pop         rax
        ret

NESTED_END RhpWaitForGCNoAbort, _TEXT

EXTERN RhpThrowHwEx : PROC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpWaitForGC -- rare path for RhpPInvokeReturn
;;
;;
;; INPUT: RCX: transition frame
;;
;; TRASHES: RCX, RDX, R8, R9, R10, R11
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpWaitForGC, _TEXT
        push_nonvol_reg rbx
        END_PROLOGUE

        mov         rbx, rcx

        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jz          NoWait

        call        RhpWaitForGCNoAbort
NoWait:
        test        [RhpTrapThreads], TrapThreadsFlags_AbortInProgress
        jz          Done
        test        dword ptr [rbx + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_THREAD_ABORT
        jz          Done

        mov         rcx, STATUS_REDHAWK_THREAD_ABORT
        pop         rbx
        pop         rdx                 ; return address as exception RIP
        jmp         RhpThrowHwEx        ; Throw the ThreadAbortException as a special kind of hardware exception

Done:
        pop         rbx
        ret

NESTED_END RhpWaitForGC, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpReversePInvokeAttachOrTrapThread
;;
;;
;; INCOMING:  RAX -- address of reverse pinvoke frame
;;
;; PRESERVES: RCX, RDX, R8, R9 -- need to preserve these because the caller assumes they aren't trashed
;;
;; TRASHES:   RAX, R10, R11
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpReversePInvokeAttachOrTrapThread, _TEXT
        alloc_stack     88h     ; alloc scratch area and frame

        ; save the integer arg regs
        save_reg_postrsp        rcx, (20h + 0*8)
        save_reg_postrsp        rdx, (20h + 1*8)
        save_reg_postrsp        r8,  (20h + 2*8)
        save_reg_postrsp        r9,  (20h + 3*8)

        ; save the FP arg regs
        save_xmm128_postrsp     xmm0, (20h + 4*8 + 0*10h)
        save_xmm128_postrsp     xmm1, (20h + 4*8 + 1*10h)
        save_xmm128_postrsp     xmm2, (20h + 4*8 + 2*10h)
        save_xmm128_postrsp     xmm3, (20h + 4*8 + 3*10h)

        END_PROLOGUE

        mov         rcx, rax        ; rcx <- reverse pinvoke frame
        call        RhpReversePInvokeAttachOrTrapThread2

        movdqa      xmm0, [rsp + (20h + 4*8 + 0*10h)]
        movdqa      xmm1, [rsp + (20h + 4*8 + 1*10h)]
        movdqa      xmm2, [rsp + (20h + 4*8 + 2*10h)]
        movdqa      xmm3, [rsp + (20h + 4*8 + 3*10h)]

        mov         rcx, [rsp + (20h + 0*8)]
        mov         rdx, [rsp + (20h + 1*8)]
        mov         r8,  [rsp + (20h + 2*8)]
        mov         r9,  [rsp + (20h + 3*8)]

        ;; epilog
        add         rsp, 88h
        ret

NESTED_END RhpReversePInvokeAttachOrTrapThread, _TEXT


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvoke
;;
;; IN:  RCX: address of pinvoke frame
;;
;; TRASHES: R10, R11
;;
;; This helper assumes that its callsite is as good to start the stackwalk as the actual PInvoke callsite.
;; The codegenerator must treat the callsite of this helper as GC triggering and generate the GC info for it.
;; Also, the codegenerator must ensure that there are no live GC references in callee saved registers.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
LEAF_ENTRY RhpPInvoke, _TEXT
        ;; R10 = GetThread(), TRASHES R11
        INLINE_GETTHREAD r10, r11

        mov         r11, [rsp]                  ; r11 <- return address
        mov         qword ptr [rcx + OFFSETOF__PInvokeTransitionFrame__m_pThread], r10
        mov         qword ptr [rcx + OFFSETOF__PInvokeTransitionFrame__m_FramePointer], rbp
        mov         qword ptr [rcx + OFFSETOF__PInvokeTransitionFrame__m_RIP], r11

        lea         r11, [rsp + 8]              ; r11 <- caller SP
        mov         dword ptr [rcx + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_SAVE_RSP
        mov         qword ptr [rcx + OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs], r11

        mov         qword ptr [r10 + OFFSETOF__Thread__m_pTransitionFrame], rcx

        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        jmp         RhpWaitForSuspend
LEAF_END RhpPInvoke, _TEXT


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpPInvokeReturn
;;
;; IN:  RCX: address of pinvoke frame
;;
;; TRASHES: RCX, RDX, R8, R9, R10, R11
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
LEAF_ENTRY RhpPInvokeReturn, _TEXT
        mov         rdx, [rcx + OFFSETOF__PInvokeTransitionFrame__m_pThread]
        mov         qword ptr [rdx + OFFSETOF__Thread__m_pTransitionFrame], 0
        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        ; passing transition frame pointer in rcx
        jmp         RhpWaitForGC
LEAF_END RhpPInvokeReturn, _TEXT


END
