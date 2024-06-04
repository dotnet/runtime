// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the RISCV64 platform.
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
#pragma error("TODO-RISCV64-CQ: unimplemented on RISCV64 yet")
#endif

    mv  t4, a0
    lw  t1, CONTEXT_ContextFlags(t4)
    andi t1, t1, 0x1 << CONTEXT_FLOATING_POINT_BIT
    beqz t1, LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT)

    //64-bits FPR.
    addi t0, t4, CONTEXT_FPU_OFFSET

    fld  f0, (CONTEXT_F0)(t0)
    fld  f1, (CONTEXT_F1)(t0)
    fld  f2, (CONTEXT_F2)(t0)
    fld  f3, (CONTEXT_F3)(t0)
    fld  f4, (CONTEXT_F4)(t0)
    fld  f5, (CONTEXT_F5)(t0)
    fld  f6, (CONTEXT_F6)(t0)
    fld  f7, (CONTEXT_F7)(t0)
    fld  f8, (CONTEXT_F8)(t0)
    fld  f9, (CONTEXT_F9)(t0)
    fld  f10, (CONTEXT_F10)(t0)
    fld  f11, (CONTEXT_F11)(t0)
    fld  f12, (CONTEXT_F12)(t0)
    fld  f13, (CONTEXT_F13)(t0)
    fld  f14, (CONTEXT_F14)(t0)
    fld  f15, (CONTEXT_F15)(t0)
    fld  f16, (CONTEXT_F16)(t0)
    fld  f17, (CONTEXT_F17)(t0)
    fld  f18, (CONTEXT_F18)(t0)
    fld  f19, (CONTEXT_F19)(t0)
    fld  f20, (CONTEXT_F20)(t0)
    fld  f21, (CONTEXT_F21)(t0)
    fld  f22, (CONTEXT_F22)(t0)
    fld  f23, (CONTEXT_F23)(t0)
    fld  f24, (CONTEXT_F24)(t0)
    fld  f25, (CONTEXT_F25)(t0)
    fld  f26, (CONTEXT_F26)(t0)
    fld  f27, (CONTEXT_F27)(t0)
    fld  f28, (CONTEXT_F28)(t0)
    fld  f29, (CONTEXT_F29)(t0)
    fld  f30, (CONTEXT_F30)(t0)
    fld  f31, (CONTEXT_F31)(t0)

    lw t1, (CONTEXT_FLOAT_CONTROL_OFFSET)(t0)
    fscsr x0, t1

LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT):

    lw  t1, CONTEXT_ContextFlags(t4)
    andi t1, t1, 0x1 << CONTEXT_INTEGER_BIT
    beqz t1, LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

    ld  tp, (CONTEXT_Tp)(a0)
    ld  gp, (CONTEXT_Gp)(a0)
    ld  a1, (CONTEXT_A1)(a0)
    ld  a2, (CONTEXT_A2)(a0)
    ld  a3, (CONTEXT_A3)(a0)
    ld  a4, (CONTEXT_A4)(a0)
    ld  a5, (CONTEXT_A5)(a0)
    ld  a6, (CONTEXT_A6)(a0)
    ld  a7, (CONTEXT_A7)(a0)
    ld  t0, (CONTEXT_T0)(a0)
    ld  t1, (CONTEXT_T1)(a0)
    ld  t2, (CONTEXT_T2)(a0)
    ld  t3, (CONTEXT_T3)(a0)
    ld  t5, (CONTEXT_T5)(a0)
    ld  t6, (CONTEXT_T6)(a0)

    ld  s1, (CONTEXT_S1)(a0)
    ld  s2, (CONTEXT_S2)(a0)
    ld  s3, (CONTEXT_S3)(a0)
    ld  s4, (CONTEXT_S4)(a0)
    ld  s5, (CONTEXT_S5)(a0)
    ld  s6, (CONTEXT_S6)(a0)
    ld  s7, (CONTEXT_S7)(a0)
    ld  s8, (CONTEXT_S8)(a0)
    ld  s9, (CONTEXT_S9)(a0)
    ld  s10, (CONTEXT_S10)(a0)
    ld  s11, (CONTEXT_S11)(a0)

    ld  a0, (CONTEXT_A0)(a0)

LOCAL_LABEL(No_Restore_CONTEXT_INTEGER):

    lw  t1, CONTEXT_ContextFlags(t4)
    andi t1, t1, 0x1 << CONTEXT_CONTROL_BIT
    beqz t1, LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ld  ra, (CONTEXT_Ra)(t4)
    ld  fp, (CONTEXT_Fp)(t4)
    ld  sp, (CONTEXT_Sp)(t4)
    ld  t1, (CONTEXT_Pc)(t4) // Since we cannot control $pc directly, we're going to corrupt t1
    ld  t4, (CONTEXT_T4)(t4)
    jr  t1

LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ld  t4, (CONTEXT_T4)(t4)
    ret
LEAF_END RtlRestoreContext, _TEXT

// Incoming:
//  a0: Context*

LEAF_ENTRY RtlCaptureContext, _TEXT
    PROLOG_STACK_ALLOC 16
    sd  t1, 0(sp)
    li  t1, CONTEXT_FULL
    sw  t1, CONTEXT_ContextFlags(a0)
    ld  t1, 0(sp)
    EPILOG_STACK_FREE 16
    tail CONTEXT_CaptureContext
LEAF_END RtlCaptureContext, _TEXT

// Incoming:
//  a0: Context*
//

LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    PROLOG_STACK_ALLOC 24
    sd  t0, 0(sp)
    sd  t1, 8(sp)
    sd  t3, 16(sp)

    lw  t1, CONTEXT_ContextFlags(a0)
    li  t0, CONTEXT_CONTROL
    and  t3, t1, t0
    bne t3, t0, LOCAL_LABEL(Done_CONTEXT_CONTROL)

    addi  t0, sp, 24
    sd  fp, CONTEXT_Fp(a0)
    sd  t0, CONTEXT_Sp(a0)
    sd  ra, CONTEXT_Ra(a0)
    sd  ra, CONTEXT_Pc(a0)

LOCAL_LABEL(Done_CONTEXT_CONTROL):

    li  t0, CONTEXT_INTEGER
    and  t3, t1, t0
    bne  t3, t0, LOCAL_LABEL(Done_CONTEXT_INTEGER)

    ld  t0, 0(sp)
    ld  t1, 8(sp)
    ld  t3, 16(sp)

    sd  x0, (CONTEXT_X0)(a0)
    sd  tp, (CONTEXT_Tp)(a0)
    sd  gp, (CONTEXT_Gp)(a0)
    sd  a0, (CONTEXT_A0)(a0)
    sd  a1, (CONTEXT_A1)(a0)
    sd  a2, (CONTEXT_A2)(a0)
    sd  a3, (CONTEXT_A3)(a0)
    sd  a4, (CONTEXT_A4)(a0)
    sd  a5, (CONTEXT_A5)(a0)
    sd  a6, (CONTEXT_A6)(a0)
    sd  a7, (CONTEXT_A7)(a0)
    sd  t0, (CONTEXT_T0)(a0)
    sd  t1, (CONTEXT_T1)(a0)
    sd  t2, (CONTEXT_T2)(a0)
    sd  t3, (CONTEXT_T3)(a0)
    sd  t4, (CONTEXT_T4)(a0)
    sd  t5, (CONTEXT_T5)(a0)
    sd  t6, (CONTEXT_T6)(a0)

    sd  s1, (CONTEXT_S1)(a0)
    sd  s2, (CONTEXT_S2)(a0)
    sd  s3, (CONTEXT_S3)(a0)
    sd  s4, (CONTEXT_S4)(a0)
    sd  s5, (CONTEXT_S5)(a0)
    sd  s6, (CONTEXT_S6)(a0)
    sd  s7, (CONTEXT_S7)(a0)
    sd  s8, (CONTEXT_S8)(a0)
    sd  s9, (CONTEXT_S9)(a0)
    sd  s10, (CONTEXT_S10)(a0)
    sd  s11, (CONTEXT_S11)(a0)

LOCAL_LABEL(Done_CONTEXT_INTEGER):
    lw  t1, CONTEXT_ContextFlags(a0)

    li  t0, CONTEXT_FLOATING_POINT
    and  t3, t1, t0
    bne  t3, t0, LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)

    addi  a0, a0, CONTEXT_FPU_OFFSET

    fsd  f0, (CONTEXT_F0)(a0)
    fsd  f1, (CONTEXT_F1)(a0)
    fsd  f2, (CONTEXT_F2)(a0)
    fsd  f3, (CONTEXT_F3)(a0)
    fsd  f4, (CONTEXT_F4)(a0)
    fsd  f5, (CONTEXT_F5)(a0)
    fsd  f6, (CONTEXT_F6)(a0)
    fsd  f7, (CONTEXT_F7)(a0)
    fsd  f8, (CONTEXT_F8)(a0)
    fsd  f9, (CONTEXT_F9)(a0)
    fsd  f10, (CONTEXT_F10)(a0)
    fsd  f11, (CONTEXT_F11)(a0)
    fsd  f12, (CONTEXT_F12)(a0)
    fsd  f13, (CONTEXT_F13)(a0)
    fsd  f14, (CONTEXT_F14)(a0)
    fsd  f15, (CONTEXT_F15)(a0)
    fsd  f16, (CONTEXT_F16)(a0)
    fsd  f17, (CONTEXT_F17)(a0)
    fsd  f18, (CONTEXT_F18)(a0)
    fsd  f19, (CONTEXT_F19)(a0)
    fsd  f20, (CONTEXT_F20)(a0)
    fsd  f21, (CONTEXT_F21)(a0)
    fsd  f22, (CONTEXT_F22)(a0)
    fsd  f23, (CONTEXT_F23)(a0)
    fsd  f24, (CONTEXT_F24)(a0)
    fsd  f25, (CONTEXT_F25)(a0)
    fsd  f26, (CONTEXT_F26)(a0)
    fsd  f27, (CONTEXT_F27)(a0)
    fsd  f28, (CONTEXT_F28)(a0)
    fsd  f29, (CONTEXT_F29)(a0)
    fsd  f30, (CONTEXT_F30)(a0)
    fsd  f31, (CONTEXT_F31)(a0)

    frcsr t0
    sd  t0, (CONTEXT_FLOAT_CONTROL_OFFSET)(a0)

LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    EPILOG_STACK_FREE 24
    ret
LEAF_END CONTEXT_CaptureContext, _TEXT
