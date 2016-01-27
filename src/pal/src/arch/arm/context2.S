// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Implementation of _CONTEXT_CaptureContext for the ARM platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"

.syntax unified
.thumb

#define CONTEXT_ARM     0x00200000

#define CONTEXT_CONTROL 1 // Sp, Lr, Pc, Cpsr
#define CONTEXT_INTEGER 2 // R0-R12
#define CONTEXT_SEGMENTS 4 // 
#define CONTEXT_FLOATING_POINT 8
#define CONTEXT_DEBUG_REGISTERS 16 //

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ContextFlags 0
#define CONTEXT_R0           CONTEXT_ContextFlags+4
#define CONTEXT_R1           CONTEXT_R0+4
#define CONTEXT_R2           CONTEXT_R1+4
#define CONTEXT_R3           CONTEXT_R2+4
#define CONTEXT_R4           CONTEXT_R3+4
#define CONTEXT_R5           CONTEXT_R4+4
#define CONTEXT_R6           CONTEXT_R5+4
#define CONTEXT_R7           CONTEXT_R6+4
#define CONTEXT_R8           CONTEXT_R7+4
#define CONTEXT_R9           CONTEXT_R8+4
#define CONTEXT_R10          CONTEXT_R9+4
#define CONTEXT_R11          CONTEXT_R10+4
#define CONTEXT_R12          CONTEXT_R11+4
#define CONTEXT_Sp           CONTEXT_R12+4
#define CONTEXT_Lr           CONTEXT_Sp+4
#define CONTEXT_Pc           CONTEXT_Lr+4
#define CONTEXT_Cpsr         CONTEXT_Pc+4
#define CONTEXT_Fpscr        CONTEXT_Cpsr+4
#define CONTEXT_Padding      CONTEXT_Fpscr+4
#define CONTEXT_D0           CONTEXT_Padding+4
#define CONTEXT_D1           CONTEXT_D0+8
#define CONTEXT_D2           CONTEXT_D1+8
#define CONTEXT_D3           CONTEXT_D2+8
#define CONTEXT_D4           CONTEXT_D3+8
#define CONTEXT_D5           CONTEXT_D4+8
#define CONTEXT_D6           CONTEXT_D5+8
#define CONTEXT_D7           CONTEXT_D6+8
#define CONTEXT_D8           CONTEXT_D7+8
#define CONTEXT_D9           CONTEXT_D8+8
#define CONTEXT_D10          CONTEXT_D9+8
#define CONTEXT_D11          CONTEXT_D10+8
#define CONTEXT_D12          CONTEXT_D11+8
#define CONTEXT_D13          CONTEXT_D12+8
#define CONTEXT_D14          CONTEXT_D13+8
#define CONTEXT_D15          CONTEXT_D14+8
#define CONTEXT_D16          CONTEXT_D15+8
#define CONTEXT_D17          CONTEXT_D16+8
#define CONTEXT_D18          CONTEXT_D17+8
#define CONTEXT_D19          CONTEXT_D18+8
#define CONTEXT_D20          CONTEXT_D19+8
#define CONTEXT_D21          CONTEXT_D20+8
#define CONTEXT_D22          CONTEXT_D21+8
#define CONTEXT_D23          CONTEXT_D22+8
#define CONTEXT_D24          CONTEXT_D23+8
#define CONTEXT_D25          CONTEXT_D24+8
#define CONTEXT_D26          CONTEXT_D25+8
#define CONTEXT_D27          CONTEXT_D26+8
#define CONTEXT_D28          CONTEXT_D27+8
#define CONTEXT_D29          CONTEXT_D28+8
#define CONTEXT_D30          CONTEXT_D29+8
#define CONTEXT_D31          CONTEXT_D30+8

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
    vstmiane r0!, {d16-d31}
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
    
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_FLOATING_POINT)
    
    itttt ne
    addne r0, CONTEXT_D0
    vldmiane r0!, {d0-d15}
    vldmiane r0, {d16-d31}
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
    
    mov r12, r0 // ideally we would ldmia r0, {r0-r12, sp, lr, pc} here, but that isn't supported on new clang
    add r12, CONTEXT_R0 // so we'll burn r12 as the IPC reg for now -- TODO: is this ok?
    ldm r12, {r0-r11}
    add r12, (CONTEXT_Sp - CONTEXT_R0)
    ldr sp, [r12]
    ldr lr, [r12, #(CONTEXT_Lr - CONTEXT_Sp)]
    ldr pc, [r12, #(CONTEXT_Pc - CONTEXT_Sp)]
    
LOCAL_LABEL(No_Restore_CONTEXT_INTEGER):
    
    ldr r2, [r0, #(CONTEXT_Cpsr)]
    msr APSR, r2    
    
    add r0, CONTEXT_Sp
    ldr sp, [r0]
    ldr lr, [r0, #(CONTEXT_Lr - CONTEXT_Sp)]
    ldr pc, [r0, #(CONTEXT_Pc - CONTEXT_Sp)]
    
LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ldr r2, [r0, #(CONTEXT_ContextFlags)]
    tst r2, #(CONTEXT_INTEGER)
    
    itt ne
    addne r0, CONTEXT_R0
    ldmiane r0, {r0-r12}

    sub sp, sp, #4
    bx lr
LEAF_END RtlRestoreContext, _TEXT
