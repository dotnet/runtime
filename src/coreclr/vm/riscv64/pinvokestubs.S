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
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 32
    PROLOG_SAVE_REG    s1, 16           // the stack slot at sp+24 is empty for 16 byte alignment

    mv  s1, a0

    // s1 = pFrame
    // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
    li  t4, FRAMETYPE_InlinedCallFrame
    sd  t4, 0(s1)

    sd  zero, (InlinedCallFrame__m_Datum)(s1)

    addi  t0, sp, 32
    sd  t0, (InlinedCallFrame__m_pCallSiteSP)(s1)
    sd  ra, (InlinedCallFrame__m_pCallerReturnAddress)(s1)

    ld  t4, 0(sp)
    sd  t4, (InlinedCallFrame__m_pCalleeSavedFP)(s1)

    // v0 = GetThread()
    call  GetThreadHelper

    sd  a0, (InlinedCallFrame__m_pThread)(s1)

    // pFrame->m_Next = pThread->m_pFrame;
    ld  t4, Thread_m_pFrame(a0)
    sd  t4, Frame__m_Next(s1)

    // pThread->m_pFrame = pFrame;
    sd  s1, (Thread_m_pFrame)(a0)

    // pThread->m_fPreemptiveGCDisabled = 0
    sw  zero, (Thread_m_fPreemptiveGCDisabled)(a0)

    EPILOG_RESTORE_REG      s1, 16    //the stack slot at sp+24 is empty for 16 byte alignment
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, ra, 32
    EPILOG_RETURN

   NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame (x0) = pointer to the InlinedCallFrame data
//
//
    LEAF_ENTRY JIT_PInvokeEnd, _TEXT

    ld  a1, (InlinedCallFrame__m_pThread)(a0)
    // a0 = pFrame
    // a1 = pThread

    // pThread->m_fPreemptiveGCDisabled = 1
    li   t4, 1
    sw   t4, (Thread_m_fPreemptiveGCDisabled)(a1)

    // Check return trap
    PREPARE_EXTERNAL_VAR  g_TrapReturningThreads, t0
    lw  t4, 0(t0)
    bne  t4, zero, LOCAL_LABEL(RarePath)

    // pThread->m_pFrame = pFrame->m_Next
    ld  t4, (Frame__m_Next)(a0)
    sd  t4, (Thread_m_pFrame)(a1)

    jr  ra

LOCAL_LABEL(RarePath):
    tail  JIT_PInvokeEndRarePath

LEAF_END JIT_PInvokeEnd, _TEXT
