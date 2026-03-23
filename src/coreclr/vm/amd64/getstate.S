// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY GetCurrentSP, _TEXT

        mov rax, rsp
        add rax, 8
        ret

LEAF_END GetCurrentSP, _TEXT


LEAF_ENTRY GetCurrentIP, _TEXT

        mov rax, [rsp]
        ret

LEAF_END GetCurrentIP, _TEXT

// void SaveInterpCalleeSavedRegisters(CalleeSavedRegisters* regs, void* fpRegs)
// Saves callee-saved GP registers into the provided buffer.
// The register order matches the CalleeSavedRegisters struct:
// R12, R13, R14, R15, Rbx, Rbp
// No FP callee-saved registers on Unix AMD64, second arg is unused.
LEAF_ENTRY SaveInterpCalleeSavedRegisters, _TEXT

        mov [rdi +  0], r12
        mov [rdi +  8], r13
        mov [rdi + 16], r14
        mov [rdi + 24], r15
        mov [rdi + 32], rbx
        mov [rdi + 40], rbp
        ret

LEAF_END SaveInterpCalleeSavedRegisters, _TEXT
