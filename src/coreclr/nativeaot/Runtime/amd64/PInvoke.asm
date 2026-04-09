;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include asmmacros.inc

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
        ret
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
        jmp         RhpWaitForGC2
LEAF_END RhpPInvokeReturn, _TEXT


END
