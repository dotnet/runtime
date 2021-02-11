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


// EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState)
LEAF_ENTRY LazyMachStateCaptureState, _TEXT

        mov     rdx, [rsp]      // get the return address

        mov     [rdi + OFFSETOF__MachState__m_Capture + 0*8], r12
        mov     [rdi + OFFSETOF__MachState__m_Capture + 1*8], r13
        mov     [rdi + OFFSETOF__MachState__m_Capture + 2*8], r14
        mov     [rdi + OFFSETOF__MachState__m_Capture + 3*8], r15
        mov     [rdi + OFFSETOF__MachState__m_Capture + 4*8], rbx
        mov     [rdi + OFFSETOF__MachState__m_Capture + 5*8], rbp

        mov     qword ptr [rdi + OFFSETOF__MachState___pRetAddr], 0

        mov     [rdi + OFFSETOF__LazyMachState__m_CaptureRip], rdx
        mov     [rdi + OFFSETOF__LazyMachState__m_CaptureRsp], rsp

        ret

LEAF_END LazyMachStateCaptureState, _TEXT
