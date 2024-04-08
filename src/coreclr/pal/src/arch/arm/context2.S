// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the ARM platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
#ifndef __armv6__
.thumb
#endif

#define CONTEXT_ARM     0x00200000

#define CONTEXT_CONTROL 1 // Sp, Lr, Pc, Cpsr
#define CONTEXT_INTEGER 2 // R0-R12
#define CONTEXT_FLOATING_POINT 4
#define CONTEXT_DEBUG_REGISTERS 8 //

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)


// Incoming:
//  r0: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    // Ensure we save these registers
    push {r4-r11}
    // Save processor flags before calling any of the following 'test' instructions
    // because they will modify state of some flags
    push {r1}
    mrs r1, apsr // Get APSR - equivalent to eflags
    push {r1} // Save APSR
    END_PROLOGUE

    push {r2}
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_INTEGER)
    pop {r2}

    // Add 4 to stack so we point at R1, pop, then sub 8 to point at APSR
    add sp, sp, #4
    pop {r1}
    sub sp, sp, #8

    itttt ne
    strne r0, [r0, #(CONTEXT_R0)]
    addne r0, CONTEXT_R1
    stmiane r0, {r1-r12}
    subne r0, CONTEXT_R1

    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_CONTROL)

    ittt ne
    addne sp, sp, #(10*4) // This needs to put the stack in the same state as it started
    strne sp, [r0, #(CONTEXT_Sp)]
    subne sp, sp, #(10*4)

    itt ne
    strne lr, [r0, #(CONTEXT_Lr)]
    strne lr, [r0, #(CONTEXT_Pc)]

    // Get the APSR pushed onto the stack at the start
    pop {r1}
    it ne
    strne r1, [r0, #(CONTEXT_Cpsr)]

    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_FLOATING_POINT)

    itt ne
    vmrsne r3, fpscr
    strne r3, [r0, #(CONTEXT_Fpscr)]

    itttt ne
    addne r0, CONTEXT_D0
    vstmiane r0!, {d0-d15}
#if CLR_ARM_FPU_CAPABILITY & 0x4
    vstmiane r0!, {d16-d31}
#else
    addne r0, r0, #128
#endif
    subne r0, CONTEXT_D31

    // Make sure sp is restored
    add sp, sp, #4

    // Restore callee saved registers
    pop {r4-r11}
    bx lr
LEAF_END CONTEXT_CaptureContext, _TEXT

// Incoming:
//  R0: Context*
//
LEAF_ENTRY RtlCaptureContext, _TEXT
    push {r1}
    mov r1, #0
    orr r1, r1, #CONTEXT_ARM
    orr r1, r1, #CONTEXT_INTEGER
    orr r1, r1, #CONTEXT_CONTROL
    orr r1, r1, #CONTEXT_FLOATING_POINT
    str r1, [r0, #(CONTEXT_ContextFlags)]
    pop {r1}
    b C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

// Incoming:
//  r0: Context*
//  r1: Exception*
//
LEAF_ENTRY RtlRestoreContext, _TEXT
    END_PROLOGUE

#ifdef HAS_ADDRESS_SANITIZER
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_CONTROL)
    beq     LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT)

    push {r0, r1}
    bl EXTERNAL_C_FUNC(__asan_handle_no_return)
    pop {r0, r1}

LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT):
#endif
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_FLOATING_POINT)

    itttt ne
    addne r0, CONTEXT_D0
    vldmiane r0!, {d0-d15}
#if CLR_ARM_FPU_CAPABILITY & 0x4
    vldmiane r0, {d16-d31}
#else
    nopne
#endif
    subne r0, CONTEXT_D16

    itt ne
    ldrne r3, [r0, #(CONTEXT_Fpscr)]
    vmrsne r3, FPSCR

    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_CONTROL)

    it eq
    beq LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_INTEGER)

    it eq
    beq LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

    ldr R2, [r0, #(CONTEXT_Cpsr)]
    msr APSR, r2

    // Ideally, we would like to use `ldmia r0, {r0-r12, sp, lr, pc}` here,
    // but clang 3.6 and later, as per ARM recommendation, disallows using
    // Sp in the register list, and Pc and Lr simultaneously.
    // So we are going to use the IPC register r12 to copy Sp, Lr and Pc
    // which should be ok -- TODO: Is this really ok?
    add r12, r0, CONTEXT_R0
    ldm r12, {r0-r11}
    ldr sp, [r12, #(CONTEXT_Sp - (CONTEXT_R0))]
    ldr lr, [r12, #(CONTEXT_Lr - (CONTEXT_R0))]
    ldr pc, [r12, #(CONTEXT_Pc - (CONTEXT_R0))]

LOCAL_LABEL(No_Restore_CONTEXT_INTEGER):

    ldr r2, [r0, #(CONTEXT_Cpsr)]
    msr APSR, r2

    ldr sp, [r0, #(CONTEXT_Sp)]
    ldr lr, [r0, #(CONTEXT_Lr)]
    ldr pc, [r0, #(CONTEXT_Pc)]

LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_INTEGER)

    itt ne
    addne r0, CONTEXT_R0
    ldmiane r0, {r0-r12}

    sub sp, sp, #4
    bx lr
LEAF_END RtlRestoreContext, _TEXT
