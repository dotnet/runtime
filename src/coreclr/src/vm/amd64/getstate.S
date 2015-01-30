//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
        
        mov     [rdi + OFFSETOF__MachState__m_CaptureRdi], rdi
        mov     [rdi + OFFSETOF__MachState__m_CaptureRsi], rsi
        mov     [rdi + OFFSETOF__MachState__m_CaptureRbx], rbx
        mov     [rdi + OFFSETOF__MachState__m_CaptureRbp], rbp
        mov     [rdi + OFFSETOF__MachState__m_CaptureR12], r12
        mov     [rdi + OFFSETOF__MachState__m_CaptureR13], r13
        mov     [rdi + OFFSETOF__MachState__m_CaptureR14], r14
        mov     [rdi + OFFSETOF__MachState__m_CaptureR15], r15

        mov     qword ptr [rdi + OFFSETOF__MachState___pRetAddr], 0

        mov     [rdi + OFFSETOF__LazyMachState__m_CaptureRip], rdx
        mov     [rdi + OFFSETOF__LazyMachState__m_CaptureRsp], rsp

        ret

LEAF_END LazyMachStateCaptureState, _TEXT
