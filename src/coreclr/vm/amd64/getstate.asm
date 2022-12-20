; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc
include AsmConstants.inc


LEAF_ENTRY GetCurrentSP, _TEXT

        mov rax, rsp
        add rax, 8
        ret

LEAF_END GetCurrentSP, _TEXT


LEAF_ENTRY GetCurrentIP, _TEXT

        mov rax, [rsp]
        ret

LEAF_END GetCurrentIP, _TEXT


LEAF_ENTRY GetRBP, _TEXT

        mov rax, rbp
        ret

LEAF_END GetRBP, _TEXT

;// this is the same implementation as the function of the same name in di\amd64\floatconversion.asm and they must
;// remain in sync.

;// @dbgtodo inspection: remove this function when we remove the ipc event to load the float state

; extern "C" double FPFillR8(void* fpContextSlot);
LEAF_ENTRY FPFillR8, _TEXT
    movdqa  xmm0, [rcx]
    ret
LEAF_END FPFillR8, _TEXT


LEAF_ENTRY get_cycle_count, _TEXT

        rdtsc                           ; time stamp count ret'd in edx:eax
        shl     rdx, 32
        mov     edx, eax
        mov     rax, rdx                ; return tsc in rax
        ret
LEAF_END get_cycle_count, _TEXT


; EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState)
LEAF_ENTRY LazyMachStateCaptureState, _TEXT

        mov     rdx, [rsp]      ; get the return address

        mov     [rcx + OFFSETOF__MachState__m_Capture + 0*8], rdi
        mov     [rcx + OFFSETOF__MachState__m_Capture + 1*8], rsi
        mov     [rcx + OFFSETOF__MachState__m_Capture + 2*8], rbx
        mov     [rcx + OFFSETOF__MachState__m_Capture + 3*8], rbp
        mov     [rcx + OFFSETOF__MachState__m_Capture + 4*8], r12
        mov     [rcx + OFFSETOF__MachState__m_Capture + 5*8], r13
        mov     [rcx + OFFSETOF__MachState__m_Capture + 6*8], r14
        mov     [rcx + OFFSETOF__MachState__m_Capture + 7*8], r15

        mov     qword ptr [rcx + OFFSETOF__MachState___pRetAddr], 0

        mov     [rcx + OFFSETOF__LazyMachState__m_CaptureRip], rdx
        mov     [rcx + OFFSETOF__LazyMachState__m_CaptureRsp], rsp

        ret

LEAF_END LazyMachStateCaptureState, _TEXT


        end
