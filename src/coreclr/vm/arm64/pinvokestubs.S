// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data
//
//
    NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler

        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
        PROLOG_SAVE_REG    x19, 16           //the stack slot at sp+24 is empty for 16 byte alignment

        mov     x19, x0

        // x19 = pFrame

        // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
        mov     x9, #FRAMETYPE_InlinedCallFrame
        str     x9, [x19]

        str     xzr, [x19, #InlinedCallFrame__m_Datum]

        add     x9, sp, 32
        str     x9, [x19, #InlinedCallFrame__m_pCallSiteSP]
        str     lr, [x19, #InlinedCallFrame__m_pCallerReturnAddress]

        ldr     x9, [sp,#0]
        str     x9, [x19, #InlinedCallFrame__m_pCalleeSavedFP]

        // x0 = GetThread()
        bl      C_FUNC(GetThreadHelper)
        str     x0, [x19, #InlinedCallFrame__m_pThread]

        // pFrame->m_Next = pThread->m_pFrame;
        ldr     x9, [x0, #Thread_m_pFrame]
        str     x9, [x19, #Frame__m_Next]

        // pThread->m_pFrame = pFrame;
        str     x19, [x0, #Thread_m_pFrame]

        // pThread->m_fPreemptiveGCDisabled = 0
        str     wzr, [x0, #Thread_m_fPreemptiveGCDisabled]

        EPILOG_RESTORE_REG      x19, 16    //the stack slot at sp+24 is empty for 16 byte alignment
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
        EPILOG_RETURN

    NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

        ldr     x1, [x0, #InlinedCallFrame__m_pThread]

        // x0 = pFrame
        // x1 = pThread

        // pThread->m_fPreemptiveGCDisabled = 1
        mov     x9, 1
        str     w9, [x1, #Thread_m_fPreemptiveGCDisabled]

        // Check return trap
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, x9
        ldr     w9, [x9]
        cbnz    w9, LOCAL_LABEL(RarePath)

        // pThread->m_pFrame = pFrame->m_Next
        ldr     x9, [x0, #Frame__m_Next]
        str     x9, [x1, #Thread_m_pFrame]

        ret

LOCAL_LABEL(RarePath):
        b       C_FUNC(JIT_PInvokeEndRarePath)

    LEAF_END JIT_PInvokeEnd, _TEXT
