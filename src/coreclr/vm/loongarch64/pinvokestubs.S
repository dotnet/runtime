// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame ($a0) = pointer to the InlinedCallFrame data
//
//
NESTED_ENTRY JIT_PInvokeBegin, _TEXT, NoHandler

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 32
    //                $s0=23.
    PROLOG_SAVE_REG    23, 16           //the stack slot at $sp+24 is empty for 16 byte alignment

    move  $s0, $a0

    // s0 = pFrame
    // set first slot to the value of InlinedCallFrame identifier (checked by runtime code)
    li.d  $t4, FRAMETYPE_InlinedCallFrame
    st.d  $t4, $s0, 0

    st.d  $zero, $s0, InlinedCallFrame__m_Datum

    addi.d  $t0, $sp, 32
    st.d  $t0, $s0, InlinedCallFrame__m_pCallSiteSP
    st.d  $ra, $s0, InlinedCallFrame__m_pCallerReturnAddress

    ld.d  $t4, $sp, 0
    st.d  $t4, $s0, InlinedCallFrame__m_pCalleeSavedFP

    // a0 = GetThread()
    bl  GetThreadHelper

    st.d  $a0, $s0, InlinedCallFrame__m_pThread

    // pFrame->m_Next = pThread->m_pFrame;
    ld.d  $t4, $a0, Thread_m_pFrame
    st.d  $t4, $s0, Frame__m_Next

    // pThread->m_pFrame = pFrame;
    st.d  $s0, $a0, Thread_m_pFrame

    // pThread->m_fPreemptiveGCDisabled = 0
    st.w     $zero, $a0, Thread_m_fPreemptiveGCDisabled

    EPILOG_RESTORE_REG      23, 16    //the stack slot at $sp+24 is empty for 16 byte alignment
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 32
    EPILOG_RETURN

NESTED_END JIT_PInvokeBegin, _TEXT

// ------------------------------------------------------------------
// IN:
// InlinedCallFrame ($a0) = pointer to the InlinedCallFrame data, including the GS cookie slot (GS cookie right
//                         before actual InlinedCallFrame data)
//
//
LEAF_ENTRY JIT_PInvokeEnd, _TEXT

    ld.d      $a1, $a0, InlinedCallFrame__m_pThread
    // $a0 = pFrame
    // $a1 = pThread

    // pThread->m_fPreemptiveGCDisabled = 1
    ori  $t4, $r0, 1
    st.w   $t4, $a1, Thread_m_fPreemptiveGCDisabled

    // Check return trap
    PREPARE_EXTERNAL_VAR  g_TrapReturningThreads, $t0
    ld.w  $t4, $t0, 0
    bne  $t4, $zero, LOCAL_LABEL(RarePath)

    // pThread->m_pFrame = pFrame->m_Next
    ld.d  $t4, $a0, Frame__m_Next
    st.d  $t4, $a1, Thread_m_pFrame

    jirl  $r0, $ra, 0

LOCAL_LABEL(RarePath):
    b  JIT_PInvokeEndRarePath

LEAF_END JIT_PInvokeEnd, _TEXT
