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


; void SaveInterpCalleeSavedRegisters(CalleeSavedRegisters* regs, M128A* fpRegs)
; Saves callee-saved GP registers into the provided buffer.
; Also saves xmm6-xmm15 into fpRegs.
; The register order matches the Windows CalleeSavedRegisters struct:
; Rdi, Rsi, Rbx, Rbp, R12, R13, R14, R15
LEAF_ENTRY SaveInterpCalleeSavedRegisters, _TEXT

        mov [rcx +  0], rdi
        mov [rcx +  8], rsi
        mov [rcx + 16], rbx
        mov [rcx + 24], rbp
        mov [rcx + 32], r12
        mov [rcx + 40], r13
        mov [rcx + 48], r14
        mov [rcx + 56], r15
        movdqa [rdx +   0], xmm6
        movdqa [rdx +  16], xmm7
        movdqa [rdx +  32], xmm8
        movdqa [rdx +  48], xmm9
        movdqa [rdx +  64], xmm10
        movdqa [rdx +  80], xmm11
        movdqa [rdx +  96], xmm12
        movdqa [rdx + 112], xmm13
        movdqa [rdx + 128], xmm14
        movdqa [rdx + 144], xmm15
        ret

LEAF_END SaveInterpCalleeSavedRegisters, _TEXT


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

        end
