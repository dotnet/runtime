;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

FASTCALL_FUNC RhpPInvoke, 4
        INLINE_GETTHREAD eax, edx

        mov         edx, [esp]                  ; edx <- return address
        mov         dword ptr [ecx + OFFSETOF__PInvokeTransitionFrame__m_pThread], eax
        mov         dword ptr [ecx + OFFSETOF__PInvokeTransitionFrame__m_FramePointer], ebp
        mov         dword ptr [ecx + OFFSETOF__PInvokeTransitionFrame__m_RIP], edx

        lea         edx, [esp + 4]              ; edx <- caller SP
        mov         dword ptr [ecx + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_SAVE_RSP
        mov         dword ptr [ecx + OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs], edx

        mov         dword ptr [eax + OFFSETOF__Thread__m_pTransitionFrame], ecx

        ret
FASTCALL_ENDFUNC

FASTCALL_FUNC RhpPInvokeReturn, 4
        mov         edx, [ecx + OFFSETOF__PInvokeTransitionFrame__m_pThread]
        mov         dword ptr [edx + OFFSETOF__Thread__m_pTransitionFrame], 0
        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        ; passing transition frame pointer in rcx
        jmp         RhpWaitForGC2
FASTCALL_ENDFUNC

        end
