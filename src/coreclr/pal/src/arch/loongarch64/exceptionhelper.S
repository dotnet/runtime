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
#pragma error("LLVM v3.9 ASAN unimplemented on LOONGARCH yet")
#endif
    addi.d  $sp, $sp, -16
    .cfi_adjust_cfa_offset 16

    // Save the FP & RA to the stack so that the unwind can work at the instruction after
    // loading the FP from the context, but before loading the SP from the context.
    st.d  $fp, $sp, 0
    st.d  $ra, $sp, 8
    .cfi_rel_offset 22, 0
    .cfi_rel_offset 1, 8

    ld.d  $tp, $a0, CONTEXT_Tp
    ld.d  $s0, $a0, CONTEXT_S0
    ld.d  $s1, $a0, CONTEXT_S1
    ld.d  $s2, $a0, CONTEXT_S2
    ld.d  $s3, $a0, CONTEXT_S3
    ld.d  $s4, $a0, CONTEXT_S4
    ld.d  $s5, $a0, CONTEXT_S5
    ld.d  $s6, $a0, CONTEXT_S6
    ld.d  $s7, $a0, CONTEXT_S7
    ld.d  $s8, $a0, CONTEXT_S8
    ld.d  $ra, $a0, CONTEXT_Ra

    ld.d  $fp, $a0, CONTEXT_Fp
    ld.d  $sp, $a0, CONTEXT_Sp

    // The PAL_SEHException pointer
    ori  $a0, $a1, 0
    b  ThrowExceptionHelper
LEAF_END ThrowExceptionFromContextInternal, _TEXT
