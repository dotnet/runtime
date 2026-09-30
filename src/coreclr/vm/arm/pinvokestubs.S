// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

.syntax unified
.thumb

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (r0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
    NESTED_ENTRY JIT_PInvokeBegin,_TEXT,NoHandler

        PROLOG_PUSH  "{r4, lr}"

        mov     r4, r0

        // r4 = pFrame

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov     r1, #FRAMETYPE_InlinedCallFrame
        str     r1, [r4]

        mov     r1, 0
        str     r1, [r4, #InlinedCallFrame__m_Datum]

        add     r1, sp, 8
        str     r1, [r4, #InlinedCallFrame__m_pCallSiteSP]
        str     r11, [r4, #InlinedCallFrame__m_pCalleeSavedFP]
        str     lr, [r4, #InlinedCallFrame__m_pCallerReturnAddress]
        str     r9, [r4, #InlinedCallFrame__m_pSPAfterProlog]

        // r0 = GetThread()
        bl      C_FUNC(GetThreadHelper)
        str     r0, [r4, #InlinedCallFrame__m_pThread]

        // pFrame->m_Next = pThread->m_pFrame;
        ldr     r1, [r0, #Thread_m_pFrame]
        str     r1, [r4, #Frame__m_Next]

        // pThread->m_pFrame = pFrame;
        str     r4, [r0, #Thread_m_pFrame]

        // pThread->m_fPreemptiveGCDisabled = 0
        mov     r1, 0
        str     r1, [r0, #Thread_m_fPreemptiveGCDisabled]

        EPILOG_POP   "{r4, pc}"

    NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (r0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                          before actual InlinedCallFrame data)
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        ldr     r1, [r0, #InlinedCallFrame__m_pThread]

        // r0 = pFrame
        // r1 = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov     r2, 1
        str     r2, [r1, #Thread_m_fPreemptiveGCDisabled]

        // Check return trap
#if defined(__clang__)
        ldr     r2, =g_TrapReturningThreads-(1f+4)
1:
        add     r2, pc
#else
        ldr     r2, =g_TrapReturningThreads
#endif
        ldr     r2, [r2]
        cbnz    r2, LOCAL_LABEL(RarePath)

        // pThread->m_pFrame = pFrame->m_Next
        ldr     r2, [r0, #Frame__m_Next]
        str     r2, [r1, #Thread_m_pFrame]

        bx      lr

LOCAL_LABEL(RarePath):
        b       C_FUNC(JIT_PInvokeEndRarePath)

    LEAF_END JIT_PInvokeEnd, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (r4) = pointer to the InlinedCallFrame data
// OUT:
// Thread (r5) = pointer to Thread
//
//
    LEAF_ENTRY JIT_InitPInvokeFrame, _TEXT

        PROLOG_PUSH  "{r0-r4, lr}"

        bl      C_FUNC(GetThreadHelper)
        mov     r5, r0

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov     r6, #FRAMETYPE_InlinedCallFrame
        str     r6, [r4]

        // pFrame->m_Next = pThread->m_pFrame;
        ldr     r6, [r5, #Thread_m_pFrame]
        str     r6, [r4, #Frame__m_Next]

        str     r11, [r4, #InlinedCallFrame__m_pCalleeSavedFP]
        str     r9, [r4, #InlinedCallFrame__m_pSPAfterProlog]
        mov     r6, 0
        str     r6, [r4, #InlinedCallFrame__m_pCallerReturnAddress]
        add     r6, sp, 24
        str     r6, [r4, #InlinedCallFrame__m_pCallSiteSP]

        // pThread->m_pFrame = pFrame;
        str     r4, [r5, #Thread_m_pFrame]

        EPILOG_POP   "{r0-r4, pc}"

    LEAF_END JIT_InitPInvokeFrame, _TEXT
