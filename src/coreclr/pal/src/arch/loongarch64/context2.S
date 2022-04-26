// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the LOONGARCH platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Incoming:
//  a0: Context*
//  a1: Exception*
//
LEAF_ENTRY RtlRestoreContext, _TEXT
#ifdef HAS_ASAN
    ld.w  $r21, $a0, CONTEXT_ContextFlags
    ext $r21, $r21, CONTEXT_FLOATING_POINT_BIT, 1
    beq $r21, $r0, LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT)

    addi.d  $sp, $sp, -16
    st.d  a0, $sp, 0
    st.d  a1, $sp, 8

    bl  __asan_handle_no_return

    ld.d  $a0, $sp, 0
    ld.d  $a1, $sp, 8
    addi.d  $sp, $sp, 16

LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT):
#endif

    ori $t4, $a0, 0
    ld.w  $r21, $t4, CONTEXT_ContextFlags
    bstrpick.w $t1, $r21, CONTEXT_FLOATING_POINT_BIT, CONTEXT_FLOATING_POINT_BIT
    beq $t1, $r0, LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT)

    //64-bits FPR.
    addi.d $t0, $t4,   CONTEXT_FPU_OFFSET

    fld.d  $f0, $t0, CONTEXT_F0
    fld.d  $f1, $t0, CONTEXT_F1
    fld.d  $f2, $t0, CONTEXT_F2
    fld.d  $f3, $t0, CONTEXT_F3
    fld.d  $f4, $t0, CONTEXT_F4
    fld.d  $f5, $t0, CONTEXT_F5
    fld.d  $f6, $t0, CONTEXT_F6
    fld.d  $f7, $t0, CONTEXT_F7
    fld.d  $f8, $t0, CONTEXT_F8
    fld.d  $f9, $t0, CONTEXT_F9
    fld.d  $f10, $t0, CONTEXT_F10
    fld.d  $f11, $t0, CONTEXT_F11
    fld.d  $f12, $t0, CONTEXT_F12
    fld.d  $f13, $t0, CONTEXT_F13
    fld.d  $f14, $t0, CONTEXT_F14
    fld.d  $f15, $t0, CONTEXT_F15
    fld.d  $f16, $t0, CONTEXT_F16
    fld.d  $f17, $t0, CONTEXT_F17
    fld.d  $f18, $t0, CONTEXT_F18
    fld.d  $f19, $t0, CONTEXT_F19
    fld.d  $f20, $t0, CONTEXT_F20
    fld.d  $f21, $t0, CONTEXT_F21
    fld.d  $f22, $t0, CONTEXT_F22
    fld.d  $f23, $t0, CONTEXT_F23
    fld.d  $f24, $t0, CONTEXT_F24
    fld.d  $f25, $t0, CONTEXT_F25
    fld.d  $f26, $t0, CONTEXT_F26
    fld.d  $f27, $t0, CONTEXT_F27
    fld.d  $f28, $t0, CONTEXT_F28
    fld.d  $f29, $t0, CONTEXT_F29
    fld.d  $f30, $t0, CONTEXT_F30
    fld.d  $f31, $t0, CONTEXT_F31

    ld.w $t1, $t0, CONTEXT_FLOAT_CONTROL_OFFSET
    movgr2fcsr  $fcsr0, $t1

LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT):

    bstrpick.w  $t1, $r21, CONTEXT_INTEGER_BIT, CONTEXT_INTEGER_BIT
    beq $t1, $r0, LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

    ld.d  $tp, $a0, CONTEXT_Tp
    ld.d  $a1, $a0, CONTEXT_A1
    ld.d  $a2, $a0, CONTEXT_A2
    ld.d  $a3, $a0, CONTEXT_A3
    ld.d  $a4, $a0, CONTEXT_A4
    ld.d  $a5, $a0, CONTEXT_A5
    ld.d  $a6, $a0, CONTEXT_A6
    ld.d  $a7, $a0, CONTEXT_A7
    ld.d  $t0, $a0, CONTEXT_T0
    ld.d  $t1, $a0, CONTEXT_T1
    ld.d  $t2, $a0, CONTEXT_T2
    ld.d  $t3, $a0, CONTEXT_T3
    ld.d  $t5, $a0, CONTEXT_T5
    ld.d  $t6, $a0, CONTEXT_T6
    ld.d  $t7, $a0, CONTEXT_T7
    ld.d  $t8, $a0, CONTEXT_T8

    ld.d  $s0, $a0, CONTEXT_S0
    ld.d  $s1, $a0, CONTEXT_S1
    ld.d  $s2, $a0, CONTEXT_S2
    ld.d  $s3, $a0, CONTEXT_S3
    ld.d  $s4, $a0, CONTEXT_S4
    ld.d  $s5, $a0, CONTEXT_S5
    ld.d  $s6, $a0, CONTEXT_S6
    ld.d  $s7, $a0, CONTEXT_S7
    ld.d  $s8, $a0, CONTEXT_S8

    ld.d  $a0, $a0, CONTEXT_A0

LOCAL_LABEL(No_Restore_CONTEXT_INTEGER):

    ld.w  $r21, $t4, CONTEXT_ContextFlags
    bstrpick.w  $r21, $r21, CONTEXT_CONTROL_BIT, CONTEXT_CONTROL_BIT
    beq  $r21, $r0, LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ld.d  $ra, $t4, CONTEXT_Ra
    ld.d  $fp, $t4, CONTEXT_Fp
    ld.d  $sp, $t4, CONTEXT_Sp
    ld.d  $r21, $t4, CONTEXT_Pc
    ld.d  $t4, $t4, CONTEXT_T4
    jirl  $r0, $r21, 0


LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ld.d  $t4, $t4, CONTEXT_T4
    jirl  $r0, $ra, 0
LEAF_END RtlRestoreContext, _TEXT

// Incoming:
//  a0: Context*

LEAF_ENTRY RtlCaptureContext, _TEXT
    PROLOG_STACK_ALLOC 16
    st.d  $r21, $sp, 0
    li.w  $r21, CONTEXT_FULL
    st.w  $r21, $a0, CONTEXT_ContextFlags
    ld.d  $r21, $sp, 0
    EPILOG_STACK_FREE 16
    b  C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

// Incoming:
//  a0: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    PROLOG_STACK_ALLOC 24
    st.d  $t0, $sp, 0
    st.d  $t1, $sp, 8
    st.d  $t3, $sp, 16

    ld.w  $t1, $a0, CONTEXT_ContextFlags
    li.w  $t0, CONTEXT_CONTROL
    and  $t3, $t1, $t0
    bne $t3, $t0, LOCAL_LABEL(Done_CONTEXT_CONTROL)

    addi.d  $t0, $sp, 24
    st.d  $fp, $a0, CONTEXT_Fp
    st.d  $t0, $a0, CONTEXT_Sp
    st.d  $ra, $a0, CONTEXT_Ra
    st.d  $ra, $a0, CONTEXT_Pc

LOCAL_LABEL(Done_CONTEXT_CONTROL):

    li.w  $t0, CONTEXT_INTEGER
    and  $t3, $t1, $t0
    bne  $t3, $t0, LOCAL_LABEL(Done_CONTEXT_INTEGER)

    ld.d  $t0, $sp, 0
    ld.d  $t1, $sp, 8
    ld.d  $t3, $sp, 16

    st.d  $tp, $a0, CONTEXT_Tp
    st.d  $a0, $a0, CONTEXT_A0
    st.d  $a1, $a0, CONTEXT_A1
    st.d  $a2, $a0, CONTEXT_A2
    st.d  $a3, $a0, CONTEXT_A3
    st.d  $a4, $a0, CONTEXT_A4
    st.d  $a5, $a0, CONTEXT_A5
    st.d  $a6, $a0, CONTEXT_A6
    st.d  $a7, $a0, CONTEXT_A7
    st.d  $t0, $a0, CONTEXT_T0
    st.d  $t1, $a0, CONTEXT_T1
    st.d  $t2, $a0, CONTEXT_T2
    st.d  $t3, $a0, CONTEXT_T3
    st.d  $t4, $a0, CONTEXT_T4
    st.d  $t5, $a0, CONTEXT_T5
    st.d  $t6, $a0, CONTEXT_T6
    st.d  $t7, $a0, CONTEXT_T7
    st.d  $t8, $a0, CONTEXT_T8
    st.d  $r21,$a0, CONTEXT_X0
    st.d  $s0, $a0, CONTEXT_S0
    st.d  $s1, $a0, CONTEXT_S1
    st.d  $s2, $a0, CONTEXT_S2
    st.d  $s3, $a0, CONTEXT_S3
    st.d  $s4, $a0, CONTEXT_S4
    st.d  $s5, $a0, CONTEXT_S5
    st.d  $s6, $a0, CONTEXT_S6
    st.d  $s7, $a0, CONTEXT_S7
    st.d  $s8, $a0, CONTEXT_S8

LOCAL_LABEL(Done_CONTEXT_INTEGER):
    ld.w  $t1, $a0, CONTEXT_ContextFlags

    li.w  $t0, CONTEXT_FLOATING_POINT
    and  $t3, $t1, $t0
    bne  $t3, $t0, LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)

    addi.d  $a0, $a0, CONTEXT_FPU_OFFSET

    fst.d  $f0 , $a0, CONTEXT_F0
    fst.d  $f1 , $a0, CONTEXT_F1
    fst.d  $f2 , $a0, CONTEXT_F2
    fst.d  $f3 , $a0, CONTEXT_F3
    fst.d  $f4 , $a0, CONTEXT_F4
    fst.d  $f5 , $a0, CONTEXT_F5
    fst.d  $f6 , $a0, CONTEXT_F6
    fst.d  $f7 , $a0, CONTEXT_F7
    fst.d  $f8 , $a0, CONTEXT_F8
    fst.d  $f9 , $a0, CONTEXT_F9
    fst.d  $f10, $a0, CONTEXT_F10
    fst.d  $f11, $a0, CONTEXT_F11
    fst.d  $f12, $a0, CONTEXT_F12
    fst.d  $f13, $a0, CONTEXT_F13
    fst.d  $f14, $a0, CONTEXT_F14
    fst.d  $f15, $a0, CONTEXT_F15
    fst.d  $f16, $a0, CONTEXT_F16
    fst.d  $f17, $a0, CONTEXT_F17
    fst.d  $f18, $a0, CONTEXT_F18
    fst.d  $f19, $a0, CONTEXT_F19
    fst.d  $f20, $a0, CONTEXT_F20
    fst.d  $f21, $a0, CONTEXT_F21
    fst.d  $f22, $a0, CONTEXT_F22
    fst.d  $f23, $a0, CONTEXT_F23
    fst.d  $f24, $a0, CONTEXT_F24
    fst.d  $f25, $a0, CONTEXT_F25
    fst.d  $f26, $a0, CONTEXT_F26
    fst.d  $f27, $a0, CONTEXT_F27
    fst.d  $f28, $a0, CONTEXT_F28
    fst.d  $f29, $a0, CONTEXT_F29
    fst.d  $f30, $a0, CONTEXT_F30
    fst.d  $f31, $a0, CONTEXT_F31
    movfcsr2gr  $t0, $fcsr0
    st.d  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET

LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    EPILOG_STACK_FREE 24
    jirl  $r0, $ra, 0
LEAF_END CONTEXT_CaptureContext, _TEXT
