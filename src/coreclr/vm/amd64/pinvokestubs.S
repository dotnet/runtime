// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"


//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data
//
//
NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler

        push_nonvol_reg r14
        END_PROLOGUE

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov             qword ptr [rdi], FRAMETYPE_InlinedCallFrame

        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_Datum], 0

        lea             rax, [rsp + 0x10]
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCallSiteSP], rax
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCalleeSavedFP], rbp

        mov             rax, qword ptr [rsp + 8]
        mov             qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pCallerReturnAddress], rax

        // Save pFrame in callee saved register
        mov             r14, rdi

        // Get current thread and cache it in current frame
        call            C_FUNC(GetThreadHelper)
        mov             qword ptr [r14 + OFFSETOF__InlinedCallFrame__m_pThread], rax

        // pFrame->m_Next = pThread->m_pFrame;
        mov             rdx, qword ptr [rax + OFFSETOF__Thread__m_pFrame]
        mov             qword ptr [r14 + OFFSETOF__Frame__m_Next], rdx

        // pThread->m_pFrame = pFrame;
        mov             qword ptr [rax + OFFSETOF__Thread__m_pFrame], r14

        // pThread->m_fPreemptiveGCDisabled = 0
        mov             dword ptr [rax + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 0

        pop_nonvol_reg  r14
        ret

NESTED_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (rdi) = pointer to the InlinedCallFrame data
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        mov             rsi, qword ptr [rdi + OFFSETOF__InlinedCallFrame__m_pThread]

        // rdi = pFrame
        // rsi = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov             dword ptr [rsi + OFFSETOF__Thread__m_fPreemptiveGCDisabled], 1

        // Check return trap
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, rax
        cmp             dword ptr [rax], 0
        jnz             RarePath

        // pThread->m_pFrame = pFrame->m_Next
        mov             rax, qword ptr [rdi + OFFSETOF__Frame__m_Next]
        mov             qword ptr [rsi + OFFSETOF__Thread__m_pFrame], rax

        ret

RarePath:
        jmp             C_FUNC(JIT_PInvokeEndRarePath)

LEAF_END JIT_PInvokeEnd, _TEXT
