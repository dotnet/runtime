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
        // Not yet supported
        int 3
        ret
LEAF_END JIT_PInvokeBegin, _TEXT

//
// IN:
// InlinedCallFrame (ecx) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT
        // Not yet supported
        int 3
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
