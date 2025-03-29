// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// IN:
// InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeBegin, _TEXT

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov             dword ptr [ecx], FRAMETYPE_InlinedCallFrame

        mov             dword ptr [ecx + InlinedCallFrame__m_Datum], 0

        mov             eax, esp
        add             eax, 4
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallSiteSP], eax
        mov             dword ptr [ecx + InlinedCallFrame__m_pCalleeSavedFP], ebp

        mov             eax, [esp]
        mov             dword ptr [ecx + InlinedCallFrame__m_pCallerReturnAddress], eax

        // edx = GetThread(). Trashes eax
        push            ecx
        push            edx
        push            esi
        push            edi
        call            C_FUNC(GetThreadHelper)
        pop             edi
        pop             esi
        pop             edx
        pop             ecx
        mov             edx, eax

        // pFrame->m_Next = pThread->m_pFrame;
        mov             eax, dword ptr [edx + Thread_m_pFrame]
        mov             dword ptr [ecx + Frame__m_Next], eax

        // pThread->m_pFrame = pFrame;
        mov             dword ptr [edx + Thread_m_pFrame], ecx

        // pThread->m_fPreemptiveGCDisabled = 0
        mov             dword ptr [edx + Thread_m_fPreemptiveGCDisabled], 0

        ret

LEAF_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        // edx = GetThread(). Trashes eax
        push            ecx
        push            edx
        push            esi
        push            edi
        call            C_FUNC(GetThreadHelper)
        pop             edi
        pop             esi
        pop             edx
        pop             ecx
        mov             edx, eax

        // ecx = pFrame
        // edx = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov             dword ptr [edx + Thread_m_fPreemptiveGCDisabled], 1

        // Check return trap
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, eax
        cmp             DWORD PTR [eax], 0
        jnz             C_FUNC(JIT_PInvokeEndRarePath)

        // pThread->m_pFrame = pFrame->m_Next
        mov             eax, dword ptr [ecx + Frame__m_Next]
        mov             dword ptr [edx + Thread_m_pFrame], eax

        ret

LEAF_END JIT_PInvokeEnd, _TEXT

//
// IN:
// InlinedCallFrame (edi) = pointer to the InlinedCallFrame data
// OUT:
// Thread (esi) = pointer to Thread data
//
//
LEAF_ENTRY JIT_InitPInvokeFrame, _TEXT

        // esi = GetThread(). Trashes eax
        push            ecx
        push            edx
        call            GetThreadHelper
        pop             edx
        pop             ecx
        mov             esi, eax

        // edi = pFrame
        // esi = pThread

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov             dword ptr [edi], FRAMETYPE_InlinedCallFrame

        // pFrame->m_Next = pThread->m_pFrame;
        mov             eax, dword ptr [esi + Thread_m_pFrame]
        mov             dword ptr [edi + Frame__m_Next], eax

        mov             dword ptr [edi + InlinedCallFrame__m_pCalleeSavedFP], ebp
        mov             dword ptr [edi + InlinedCallFrame__m_pCallerReturnAddress], 0

        // pThread->m_pFrame = pFrame;
        mov             dword ptr [esi + Thread_m_pFrame], edi

        // leave current Thread in ESI
        ret

LEAF_END JIT_InitPInvokeFrame, _TEXT
