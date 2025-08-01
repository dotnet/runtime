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
#ifdef HAS_ADDRESS_SANITIZER
    ld.w  $r21, $a0, CONTEXT_ContextFlags
    andi  $r21, $r21, (1 << CONTEXT_CONTROL_BIT)
    beqz  $r21, LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT)

    addi.d  $sp, $sp, -16
    st.d  $a0, $sp, 0
    st.d  $a1, $sp, 8

    bl  __asan_handle_no_return

    ld.d  $a0, $sp, 0
    ld.d  $a1, $sp, 8
    addi.d  $sp, $sp, 16

LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT):
#endif

    ori $t4, $a0, 0
    ld.w  $r21, $a0, CONTEXT_ContextFlags
    andi $t1, $r21, (1 << CONTEXT_FLOATING_POINT_BIT)
    beqz $t1, LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT)

    andi $t1, $r21, CONTEXT_LASX
    bnez $t1, LOCAL_LABEL(Restore_CONTEXT_LASX)
    andi $t1, $r21, CONTEXT_LSX
    bnez $t1, LOCAL_LABEL(Restore_CONTEXT_LSX)

    // Neither LSX or LASX is supported.
    fld.d  $f0 , $a0, CONTEXT_FPU_OFFSET
    fld.d  $f1 , $a0, CONTEXT_FPU_OFFSET + 8*1
    fld.d  $f2 , $a0, CONTEXT_FPU_OFFSET + 8*2
    fld.d  $f3 , $a0, CONTEXT_FPU_OFFSET + 8*3
    fld.d  $f4 , $a0, CONTEXT_FPU_OFFSET + 8*4
    fld.d  $f5 , $a0, CONTEXT_FPU_OFFSET + 8*5
    fld.d  $f6 , $a0, CONTEXT_FPU_OFFSET + 8*6
    fld.d  $f7 , $a0, CONTEXT_FPU_OFFSET + 8*7
    fld.d  $f8 , $a0, CONTEXT_FPU_OFFSET + 8*8
    fld.d  $f9 , $a0, CONTEXT_FPU_OFFSET + 8*9
    fld.d  $f10, $a0, CONTEXT_FPU_OFFSET + 8*10
    fld.d  $f11, $a0, CONTEXT_FPU_OFFSET + 8*11
    fld.d  $f12, $a0, CONTEXT_FPU_OFFSET + 8*12
    fld.d  $f13, $a0, CONTEXT_FPU_OFFSET + 8*13
    fld.d  $f14, $a0, CONTEXT_FPU_OFFSET + 8*14
    fld.d  $f15, $a0, CONTEXT_FPU_OFFSET + 8*15
    fld.d  $f16, $a0, CONTEXT_FPU_OFFSET + 8*16
    fld.d  $f17, $a0, CONTEXT_FPU_OFFSET + 8*17
    fld.d  $f18, $a0, CONTEXT_FPU_OFFSET + 8*18
    fld.d  $f19, $a0, CONTEXT_FPU_OFFSET + 8*19
    fld.d  $f20, $a0, CONTEXT_FPU_OFFSET + 8*20
    fld.d  $f21, $a0, CONTEXT_FPU_OFFSET + 8*21
    fld.d  $f22, $a0, CONTEXT_FPU_OFFSET + 8*22
    fld.d  $f23, $a0, CONTEXT_FPU_OFFSET + 8*23
    fld.d  $f24, $a0, CONTEXT_FPU_OFFSET + 8*24
    fld.d  $f25, $a0, CONTEXT_FPU_OFFSET + 8*25
    fld.d  $f26, $a0, CONTEXT_FPU_OFFSET + 8*26
    fld.d  $f27, $a0, CONTEXT_FPU_OFFSET + 8*27
    fld.d  $f28, $a0, CONTEXT_FPU_OFFSET + 8*28
    fld.d  $f29, $a0, CONTEXT_FPU_OFFSET + 8*29
    fld.d  $f30, $a0, CONTEXT_FPU_OFFSET + 8*30
    fld.d  $f31, $a0, CONTEXT_FPU_OFFSET + 8*31
    b  LOCAL_LABEL(Restore_CONTEXT_FLOATING_CONTROL)

LOCAL_LABEL(Restore_CONTEXT_LSX):
    // 128-bits SIMD:LSX.
    vld  $vr0 , $a0, CONTEXT_FPU_OFFSET
    vld  $vr1 , $a0, CONTEXT_FPU_OFFSET + 16*1
    vld  $vr2 , $a0, CONTEXT_FPU_OFFSET + 16*2
    vld  $vr3 , $a0, CONTEXT_FPU_OFFSET + 16*3
    vld  $vr4 , $a0, CONTEXT_FPU_OFFSET + 16*4
    vld  $vr5 , $a0, CONTEXT_FPU_OFFSET + 16*5
    vld  $vr6 , $a0, CONTEXT_FPU_OFFSET + 16*6
    vld  $vr7 , $a0, CONTEXT_FPU_OFFSET + 16*7
    vld  $vr8 , $a0, CONTEXT_FPU_OFFSET + 16*8
    vld  $vr9 , $a0, CONTEXT_FPU_OFFSET + 16*9
    vld  $vr10, $a0, CONTEXT_FPU_OFFSET + 16*10
    vld  $vr11, $a0, CONTEXT_FPU_OFFSET + 16*11
    vld  $vr12, $a0, CONTEXT_FPU_OFFSET + 16*12
    vld  $vr13, $a0, CONTEXT_FPU_OFFSET + 16*13
    vld  $vr14, $a0, CONTEXT_FPU_OFFSET + 16*14
    vld  $vr15, $a0, CONTEXT_FPU_OFFSET + 16*15
    vld  $vr16, $a0, CONTEXT_FPU_OFFSET + 16*16
    vld  $vr17, $a0, CONTEXT_FPU_OFFSET + 16*17
    vld  $vr18, $a0, CONTEXT_FPU_OFFSET + 16*18
    vld  $vr19, $a0, CONTEXT_FPU_OFFSET + 16*19
    vld  $vr20, $a0, CONTEXT_FPU_OFFSET + 16*20
    vld  $vr21, $a0, CONTEXT_FPU_OFFSET + 16*21
    vld  $vr22, $a0, CONTEXT_FPU_OFFSET + 16*22
    vld  $vr23, $a0, CONTEXT_FPU_OFFSET + 16*23
    vld  $vr24, $a0, CONTEXT_FPU_OFFSET + 16*24
    vld  $vr25, $a0, CONTEXT_FPU_OFFSET + 16*25
    vld  $vr26, $a0, CONTEXT_FPU_OFFSET + 16*26
    vld  $vr27, $a0, CONTEXT_FPU_OFFSET + 16*27
    vld  $vr28, $a0, CONTEXT_FPU_OFFSET + 16*28
    vld  $vr29, $a0, CONTEXT_FPU_OFFSET + 16*29
    vld  $vr30, $a0, CONTEXT_FPU_OFFSET + 16*30
    vld  $vr31, $a0, CONTEXT_FPU_OFFSET + 16*31
    b  LOCAL_LABEL(Restore_CONTEXT_FLOATING_CONTROL)

LOCAL_LABEL(Restore_CONTEXT_LASX):
    // 256-bits SIMD:LASX.
    xvld  $xr0 , $a0, CONTEXT_FPU_OFFSET
    xvld  $xr1 , $a0, CONTEXT_FPU_OFFSET + 32*1
    xvld  $xr2 , $a0, CONTEXT_FPU_OFFSET + 32*2
    xvld  $xr3 , $a0, CONTEXT_FPU_OFFSET + 32*3
    xvld  $xr4 , $a0, CONTEXT_FPU_OFFSET + 32*4
    xvld  $xr5 , $a0, CONTEXT_FPU_OFFSET + 32*5
    xvld  $xr6 , $a0, CONTEXT_FPU_OFFSET + 32*6
    xvld  $xr7 , $a0, CONTEXT_FPU_OFFSET + 32*7
    xvld  $xr8 , $a0, CONTEXT_FPU_OFFSET + 32*8
    xvld  $xr9 , $a0, CONTEXT_FPU_OFFSET + 32*9
    xvld  $xr10, $a0, CONTEXT_FPU_OFFSET + 32*10
    xvld  $xr11, $a0, CONTEXT_FPU_OFFSET + 32*11
    xvld  $xr12, $a0, CONTEXT_FPU_OFFSET + 32*12
    xvld  $xr13, $a0, CONTEXT_FPU_OFFSET + 32*13
    xvld  $xr14, $a0, CONTEXT_FPU_OFFSET + 32*14
    xvld  $xr15, $a0, CONTEXT_FPU_OFFSET + 32*15
    xvld  $xr16, $a0, CONTEXT_FPU_OFFSET + 32*16
    xvld  $xr17, $a0, CONTEXT_FPU_OFFSET + 32*17
    xvld  $xr18, $a0, CONTEXT_FPU_OFFSET + 32*18
    xvld  $xr19, $a0, CONTEXT_FPU_OFFSET + 32*19
    xvld  $xr20, $a0, CONTEXT_FPU_OFFSET + 32*20
    xvld  $xr21, $a0, CONTEXT_FPU_OFFSET + 32*21
    xvld  $xr22, $a0, CONTEXT_FPU_OFFSET + 32*22
    xvld  $xr23, $a0, CONTEXT_FPU_OFFSET + 32*23
    xvld  $xr24, $a0, CONTEXT_FPU_OFFSET + 32*24
    xvld  $xr25, $a0, CONTEXT_FPU_OFFSET + 32*25
    xvld  $xr26, $a0, CONTEXT_FPU_OFFSET + 32*26
    xvld  $xr27, $a0, CONTEXT_FPU_OFFSET + 32*27
    xvld  $xr28, $a0, CONTEXT_FPU_OFFSET + 32*28
    xvld  $xr29, $a0, CONTEXT_FPU_OFFSET + 32*29
    xvld  $xr30, $a0, CONTEXT_FPU_OFFSET + 32*30
    xvld  $xr31, $a0, CONTEXT_FPU_OFFSET + 32*31

LOCAL_LABEL(Restore_CONTEXT_FLOATING_CONTROL):
    ld.d  $t1, $a0, CONTEXT_FLOAT_CONTROL_OFFSET
    movgr2cf  $fcc0, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc1, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc2, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc3, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc4, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc5, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc6, $t1
    srli.d  $t1, $t1, 8
    movgr2cf  $fcc7, $t1

    ld.w $t1, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 8
    movgr2fcsr  $fcsr0, $t1

LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT):

    andi  $t1, $r21, (1 << CONTEXT_INTEGER_BIT)
    beqz  $t1, LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

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

    andi  $r21, $r21, (1 << CONTEXT_CONTROL_BIT)
    beqz  $r21, LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ld.d  $ra, $t4, CONTEXT_Ra
    ld.d  $fp, $t4, CONTEXT_Fp
    ld.d  $r21, $t4, CONTEXT_Pc
    ld.d  $sp, $t4, CONTEXT_Sp
    // NOTE: the r21 and t4 had been trashed.
    jirl  $r0, $r21, 0


LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ld.d  $r21, $t4, CONTEXT_R21
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
    PROLOG_STACK_ALLOC 32
    st.d  $t0, $sp, 0
    st.d  $t1, $sp, 8
    st.d  $t3, $sp, 16

    ld.w  $t1, $a0, CONTEXT_ContextFlags
    andi  $t3, $t1, (1 << CONTEXT_CONTROL_BIT)
    beqz $t3, LOCAL_LABEL(Done_CONTEXT_CONTROL)

    addi.d  $t0, $sp, 32
    st.d  $fp, $a0, CONTEXT_Fp
    st.d  $t0, $a0, CONTEXT_Sp
    st.d  $ra, $a0, CONTEXT_Ra
    st.d  $ra, $a0, CONTEXT_Pc

LOCAL_LABEL(Done_CONTEXT_CONTROL):

    andi  $t3, $t1, (1 << CONTEXT_INTEGER_BIT)
    beqz  $t3, LOCAL_LABEL(Done_CONTEXT_INTEGER)

    ld.d  $t0, $sp, 0
    ld.d  $t1, $sp, 8
    ld.d  $t3, $sp, 16

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
    st.d  $r21,$a0, CONTEXT_R21
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

    andi  $t3, $t1, (1 << CONTEXT_FLOATING_POINT_BIT)
    beqz  $t3, LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)

    andi $t3, $t1, CONTEXT_LASX
    bnez $t3, LOCAL_LABEL(Store_CONTEXT_LASX)
    andi $t3, $t1, CONTEXT_LSX
    bnez $t3, LOCAL_LABEL(Store_CONTEXT_LSX)

    // Neither LSX or LASX is supported.
    fst.d  $f0 , $a0, CONTEXT_FPU_OFFSET
    fst.d  $f1 , $a0, CONTEXT_FPU_OFFSET + 8*1
    fst.d  $f2 , $a0, CONTEXT_FPU_OFFSET + 8*2
    fst.d  $f3 , $a0, CONTEXT_FPU_OFFSET + 8*3
    fst.d  $f4 , $a0, CONTEXT_FPU_OFFSET + 8*4
    fst.d  $f5 , $a0, CONTEXT_FPU_OFFSET + 8*5
    fst.d  $f6 , $a0, CONTEXT_FPU_OFFSET + 8*6
    fst.d  $f7 , $a0, CONTEXT_FPU_OFFSET + 8*7
    fst.d  $f8 , $a0, CONTEXT_FPU_OFFSET + 8*8
    fst.d  $f9 , $a0, CONTEXT_FPU_OFFSET + 8*9
    fst.d  $f10, $a0, CONTEXT_FPU_OFFSET + 8*10
    fst.d  $f11, $a0, CONTEXT_FPU_OFFSET + 8*11
    fst.d  $f12, $a0, CONTEXT_FPU_OFFSET + 8*12
    fst.d  $f13, $a0, CONTEXT_FPU_OFFSET + 8*13
    fst.d  $f14, $a0, CONTEXT_FPU_OFFSET + 8*14
    fst.d  $f15, $a0, CONTEXT_FPU_OFFSET + 8*15
    fst.d  $f16, $a0, CONTEXT_FPU_OFFSET + 8*16
    fst.d  $f17, $a0, CONTEXT_FPU_OFFSET + 8*17
    fst.d  $f18, $a0, CONTEXT_FPU_OFFSET + 8*18
    fst.d  $f19, $a0, CONTEXT_FPU_OFFSET + 8*19
    fst.d  $f20, $a0, CONTEXT_FPU_OFFSET + 8*20
    fst.d  $f21, $a0, CONTEXT_FPU_OFFSET + 8*21
    fst.d  $f22, $a0, CONTEXT_FPU_OFFSET + 8*22
    fst.d  $f23, $a0, CONTEXT_FPU_OFFSET + 8*23
    fst.d  $f24, $a0, CONTEXT_FPU_OFFSET + 8*24
    fst.d  $f25, $a0, CONTEXT_FPU_OFFSET + 8*25
    fst.d  $f26, $a0, CONTEXT_FPU_OFFSET + 8*26
    fst.d  $f27, $a0, CONTEXT_FPU_OFFSET + 8*27
    fst.d  $f28, $a0, CONTEXT_FPU_OFFSET + 8*28
    fst.d  $f29, $a0, CONTEXT_FPU_OFFSET + 8*29
    fst.d  $f30, $a0, CONTEXT_FPU_OFFSET + 8*30
    fst.d  $f31, $a0, CONTEXT_FPU_OFFSET + 8*31
    b  LOCAL_LABEL(Store_CONTEXT_FLOAT_CONTROL)

LOCAL_LABEL(Store_CONTEXT_LSX):
    // 128-bits SIMD:LSX.
    vst  $vr0 , $a0, CONTEXT_FPU_OFFSET
    vst  $vr1 , $a0, CONTEXT_FPU_OFFSET + 16*1
    vst  $vr2 , $a0, CONTEXT_FPU_OFFSET + 16*2
    vst  $vr3 , $a0, CONTEXT_FPU_OFFSET + 16*3
    vst  $vr4 , $a0, CONTEXT_FPU_OFFSET + 16*4
    vst  $vr5 , $a0, CONTEXT_FPU_OFFSET + 16*5
    vst  $vr6 , $a0, CONTEXT_FPU_OFFSET + 16*6
    vst  $vr7 , $a0, CONTEXT_FPU_OFFSET + 16*7
    vst  $vr8 , $a0, CONTEXT_FPU_OFFSET + 16*8
    vst  $vr9 , $a0, CONTEXT_FPU_OFFSET + 16*9
    vst  $vr10, $a0, CONTEXT_FPU_OFFSET + 16*10
    vst  $vr11, $a0, CONTEXT_FPU_OFFSET + 16*11
    vst  $vr12, $a0, CONTEXT_FPU_OFFSET + 16*12
    vst  $vr13, $a0, CONTEXT_FPU_OFFSET + 16*13
    vst  $vr14, $a0, CONTEXT_FPU_OFFSET + 16*14
    vst  $vr15, $a0, CONTEXT_FPU_OFFSET + 16*15
    vst  $vr16, $a0, CONTEXT_FPU_OFFSET + 16*16
    vst  $vr17, $a0, CONTEXT_FPU_OFFSET + 16*17
    vst  $vr18, $a0, CONTEXT_FPU_OFFSET + 16*18
    vst  $vr19, $a0, CONTEXT_FPU_OFFSET + 16*19
    vst  $vr20, $a0, CONTEXT_FPU_OFFSET + 16*20
    vst  $vr21, $a0, CONTEXT_FPU_OFFSET + 16*21
    vst  $vr22, $a0, CONTEXT_FPU_OFFSET + 16*22
    vst  $vr23, $a0, CONTEXT_FPU_OFFSET + 16*23
    vst  $vr24, $a0, CONTEXT_FPU_OFFSET + 16*24
    vst  $vr25, $a0, CONTEXT_FPU_OFFSET + 16*25
    vst  $vr26, $a0, CONTEXT_FPU_OFFSET + 16*26
    vst  $vr27, $a0, CONTEXT_FPU_OFFSET + 16*27
    vst  $vr28, $a0, CONTEXT_FPU_OFFSET + 16*28
    vst  $vr29, $a0, CONTEXT_FPU_OFFSET + 16*29
    vst  $vr30, $a0, CONTEXT_FPU_OFFSET + 16*30
    vst  $vr31, $a0, CONTEXT_FPU_OFFSET + 16*31
    b  LOCAL_LABEL(Store_CONTEXT_FLOAT_CONTROL)

LOCAL_LABEL(Store_CONTEXT_LASX):
    // 256-bits SIMD:LASX.
    xvst  $xr0 , $a0, CONTEXT_FPU_OFFSET
    xvst  $xr1 , $a0, CONTEXT_FPU_OFFSET + 32*1
    xvst  $xr2 , $a0, CONTEXT_FPU_OFFSET + 32*2
    xvst  $xr3 , $a0, CONTEXT_FPU_OFFSET + 32*3
    xvst  $xr4 , $a0, CONTEXT_FPU_OFFSET + 32*4
    xvst  $xr5 , $a0, CONTEXT_FPU_OFFSET + 32*5
    xvst  $xr6 , $a0, CONTEXT_FPU_OFFSET + 32*6
    xvst  $xr7 , $a0, CONTEXT_FPU_OFFSET + 32*7
    xvst  $xr8 , $a0, CONTEXT_FPU_OFFSET + 32*8
    xvst  $xr9 , $a0, CONTEXT_FPU_OFFSET + 32*9
    xvst  $xr10, $a0, CONTEXT_FPU_OFFSET + 32*10
    xvst  $xr11, $a0, CONTEXT_FPU_OFFSET + 32*11
    xvst  $xr12, $a0, CONTEXT_FPU_OFFSET + 32*12
    xvst  $xr13, $a0, CONTEXT_FPU_OFFSET + 32*13
    xvst  $xr14, $a0, CONTEXT_FPU_OFFSET + 32*14
    xvst  $xr15, $a0, CONTEXT_FPU_OFFSET + 32*15
    xvst  $xr16, $a0, CONTEXT_FPU_OFFSET + 32*16
    xvst  $xr17, $a0, CONTEXT_FPU_OFFSET + 32*17
    xvst  $xr18, $a0, CONTEXT_FPU_OFFSET + 32*18
    xvst  $xr19, $a0, CONTEXT_FPU_OFFSET + 32*19
    xvst  $xr20, $a0, CONTEXT_FPU_OFFSET + 32*20
    xvst  $xr21, $a0, CONTEXT_FPU_OFFSET + 32*21
    xvst  $xr22, $a0, CONTEXT_FPU_OFFSET + 32*22
    xvst  $xr23, $a0, CONTEXT_FPU_OFFSET + 32*23
    xvst  $xr24, $a0, CONTEXT_FPU_OFFSET + 32*24
    xvst  $xr25, $a0, CONTEXT_FPU_OFFSET + 32*25
    xvst  $xr26, $a0, CONTEXT_FPU_OFFSET + 32*26
    xvst  $xr27, $a0, CONTEXT_FPU_OFFSET + 32*27
    xvst  $xr28, $a0, CONTEXT_FPU_OFFSET + 32*28
    xvst  $xr29, $a0, CONTEXT_FPU_OFFSET + 32*29
    xvst  $xr30, $a0, CONTEXT_FPU_OFFSET + 32*30
    xvst  $xr31, $a0, CONTEXT_FPU_OFFSET + 32*31

LOCAL_LABEL(Store_CONTEXT_FLOAT_CONTROL):
    ori $t0, $r0, 0
    movcf2gr  $t0, $fcc0
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET
    movcf2gr  $t0, $fcc1
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 1
    movcf2gr  $t0, $fcc2
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 2
    movcf2gr  $t0, $fcc3
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 3
    movcf2gr  $t0, $fcc4
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 4
    movcf2gr  $t0, $fcc5
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 5
    movcf2gr  $t0, $fcc6
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 6
    movcf2gr  $t0, $fcc7
    st.b  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 7

    movfcsr2gr  $t0, $fcsr0
    st.w  $t0, $a0, CONTEXT_FLOAT_CONTROL_OFFSET + 8

LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    EPILOG_STACK_FREE 32
    jirl  $r0, $ra, 0
LEAF_END CONTEXT_CaptureContext, _TEXT
