// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//////////////////////////////////////////////////////////////////////////
//
// This function creates a stack frame right below the target frame, restores all callee
// saved registers, SP, and RA from the passed in context.
// Then it uses the ThrowExceptionHelper to throw the passed in exception from that context.
// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
#ifdef HAS_ASAN
#pragma error("TODO-RISCV64-CQ: unimplemented on RISCV64 yet")
#endif
    addi  sp, sp, -16
    .cfi_adjust_cfa_offset 16

    // Save the FP & RA to the stack so that the unwind can work at the instruction after
    // loading the FP from the context, but before loading the SP from the context.
    sd  fp, 0(sp)
    sd  ra, 8(sp)
    .cfi_rel_offset fp, 0
    .cfi_rel_offset ra, 8

    ld  tp, CONTEXT_Tp(a0)
    ld  gp, CONTEXT_Gp(a0)
    ld  s1, CONTEXT_S1(a0)
    ld  s2, CONTEXT_S2(a0)
    ld  s3, CONTEXT_S3(a0)
    ld  s4, CONTEXT_S4(a0)
    ld  s5, CONTEXT_S5(a0)
    ld  s6, CONTEXT_S6(a0)
    ld  s7, CONTEXT_S7(a0)
    ld  s8, CONTEXT_S8(a0)
    ld  s9, CONTEXT_S9(a0)
    ld  s10, CONTEXT_S10(a0)
    ld  s11, CONTEXT_S11(a0)
    ld  ra, CONTEXT_Ra(a0)

    ld  fp, CONTEXT_Fp(a0)
    ld  sp, CONTEXT_Sp(a0)

    // The PAL_SEHException pointer
    addi a0, a1, 0
    tail ThrowExceptionHelper
LEAF_END ThrowExceptionFromContextInternal, _TEXT
