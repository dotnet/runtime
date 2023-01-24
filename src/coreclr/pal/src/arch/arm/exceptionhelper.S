// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
#ifndef __armv6__
.thumb
#endif

// EXTERN_C void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex);
LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
    // Ported from src/pal/src/arch/amd64/exceptionhelper.S
#ifdef HAS_ASAN
    // Need to call __asan_handle_no_return explicitly here because we re-initialize SP before
    // throwing exception in ThrowExceptionHelper
    push_nonvol_reg "{r0, r1}"
    bl  EXTERNAL_C_FUNC(__asan_handle_no_return)
    pop_nonvol_reg "{r0, r1}"
#endif

    push_nonvol_reg {r7} /* FP. x64-RBP */

    ldr r4,   [r0, #(CONTEXT_R4)]
    ldr r5,   [r0, #(CONTEXT_R5)]
    ldr r6,   [r0, #(CONTEXT_R6)]
    ldr r7,   [r0, #(CONTEXT_R7)]
    ldr r8,   [r0, #(CONTEXT_R8)]
    ldr r9,   [r0, #(CONTEXT_R9)]
    ldr r10,  [r0, #(CONTEXT_R10)]
    ldr r11,  [r0, #(CONTEXT_R11)]
    ldr sp,   [r0, #(CONTEXT_Sp)]
    ldr lr,   [r0, #(CONTEXT_Pc)]

    vldr d8,  [r0, #(CONTEXT_D8)]
    vldr d9,  [r0, #(CONTEXT_D9)]
    vldr d10, [r0, #(CONTEXT_D10)]
    vldr d11, [r0, #(CONTEXT_D11)]
    vldr d12, [r0, #(CONTEXT_D12)]
    vldr d13, [r0, #(CONTEXT_D13)]
    vldr d14, [r0, #(CONTEXT_D14)]
    vldr d15, [r0, #(CONTEXT_D15)]

    // The PAL_SEHException pointer
    mov r0, r1
    b   EXTERNAL_C_FUNC(ThrowExceptionHelper)
LEAF_END ThrowExceptionFromContextInternal, _TEXT
