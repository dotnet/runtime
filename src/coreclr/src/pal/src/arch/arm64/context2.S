// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Implementation of _CONTEXT_CaptureContext for the ARM platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"

#define CONTEXT_ARM64   0x00400000L

#define CONTEXT_CONTROL (CONTEXT_ARM64 | 0x1L)
#define CONTEXT_INTEGER (CONTEXT_ARM64 | 0x2L)
#define CONTEXT_FLOATING_POINT  (CONTEXT_ARM64 | 0x4L)
#define CONTEXT_DEBUG_REGISTERS (CONTEXT_ARM64 | 0x8L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT)

#define CONTEXT_ContextFlags 0
#define CONTEXT_Cpsr         CONTEXT_ContextFlags+4
#define CONTEXT_X0           CONTEXT_Cpsr+4
#define CONTEXT_X1           CONTEXT_X0+8
#define CONTEXT_X2           CONTEXT_X1+8
#define CONTEXT_X3           CONTEXT_X2+8
#define CONTEXT_X4           CONTEXT_X3+8
#define CONTEXT_X5           CONTEXT_X4+8
#define CONTEXT_X6           CONTEXT_X5+8
#define CONTEXT_X7           CONTEXT_X6+8
#define CONTEXT_X8           CONTEXT_X7+8
#define CONTEXT_X9           CONTEXT_X8+8
#define CONTEXT_X10          CONTEXT_X9+8
#define CONTEXT_X11          CONTEXT_X10+8
#define CONTEXT_X12          CONTEXT_X11+8
#define CONTEXT_X13          CONTEXT_X12+8
#define CONTEXT_X14          CONTEXT_X13+8
#define CONTEXT_X15          CONTEXT_X14+8
#define CONTEXT_X16          CONTEXT_X15+8
#define CONTEXT_X17          CONTEXT_X16+8
#define CONTEXT_X18          CONTEXT_X17+8
#define CONTEXT_X19          CONTEXT_X18+8
#define CONTEXT_X20          CONTEXT_X19+8
#define CONTEXT_X21          CONTEXT_X20+8
#define CONTEXT_X22          CONTEXT_X21+8
#define CONTEXT_X23          CONTEXT_X22+8
#define CONTEXT_X24          CONTEXT_X23+8
#define CONTEXT_X25          CONTEXT_X24+8
#define CONTEXT_X26          CONTEXT_X25+8
#define CONTEXT_X27          CONTEXT_X26+8
#define CONTEXT_X28          CONTEXT_X27+8
#define CONTEXT_Fp           CONTEXT_X28+8
#define CONTEXT_Lr           CONTEXT_Fp+8
#define CONTEXT_Sp           CONTEXT_Lr+8
#define CONTEXT_Pc           CONTEXT_Sp+8
#define CONTEXT_NEON_OFFSET  CONTEXT_Pc+8
#define CONTEXT_V0           0
#define CONTEXT_V1           CONTEXT_V0+16
#define CONTEXT_V2           CONTEXT_V1+16
#define CONTEXT_V3           CONTEXT_V2+16
#define CONTEXT_V4           CONTEXT_V3+16
#define CONTEXT_V5           CONTEXT_V4+16
#define CONTEXT_V6           CONTEXT_V5+16
#define CONTEXT_V7           CONTEXT_V6+16
#define CONTEXT_V8           CONTEXT_V7+16
#define CONTEXT_V9           CONTEXT_V8+16
#define CONTEXT_V10          CONTEXT_V9+16
#define CONTEXT_V11          CONTEXT_V10+16
#define CONTEXT_V12          CONTEXT_V11+16
#define CONTEXT_V13          CONTEXT_V12+16
#define CONTEXT_V14          CONTEXT_V13+16
#define CONTEXT_V15          CONTEXT_V14+16
#define CONTEXT_V16          CONTEXT_V15+16
#define CONTEXT_V17          CONTEXT_V16+16
#define CONTEXT_V18          CONTEXT_V17+16
#define CONTEXT_V19          CONTEXT_V18+16
#define CONTEXT_V20          CONTEXT_V19+16
#define CONTEXT_V21          CONTEXT_V20+16
#define CONTEXT_V22          CONTEXT_V21+16
#define CONTEXT_V23          CONTEXT_V22+16
#define CONTEXT_V24          CONTEXT_V23+16
#define CONTEXT_V25          CONTEXT_V24+16
#define CONTEXT_V26          CONTEXT_V25+16
#define CONTEXT_V27          CONTEXT_V26+16
#define CONTEXT_V28          CONTEXT_V27+16
#define CONTEXT_V29          CONTEXT_V28+16
#define CONTEXT_V30          CONTEXT_V29+16
#define CONTEXT_V31          CONTEXT_V30+16
#define CONTEXT_FLOAT_CONTROL_OFFSET  CONTEXT_V31
#define CONTEXT_Fpcr         0
#define CONTEXT_Fpsr         CONTEXT_Fpcr+4

// Incoming:
//  x0: Context*
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    sub sp, sp, #32
    // save x1, x2 and x3 on stack so we can use them as scratch
    stp x1, x2, [sp]
    str x3, [sp, 16]
    // save the current flags on the stack
    mrs x1, nzcv
    str x1, [sp, 24]

    ldr w1, [x0, CONTEXT_ContextFlags]
    // clangs assembler doesn't seem to support the mov Wx, imm32 yet
    movz w2, #0x40, lsl #16
    movk w2, #0x1
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(Done_CONTEXT_CONTROL)

    // save the cpsr
    ldr x2, [sp, 24]
    str w2, [x0, CONTEXT_Cpsr]
    stp fp, lr, [x0, CONTEXT_Fp]
    add sp, sp, #32
    mov x2, sp
    stp x2, lr, [x0, CONTEXT_Sp]
    sub sp, sp, #32

LOCAL_LABEL(Done_CONTEXT_CONTROL): 
    // we dont clobber x1 in the CONTEXT_CONTROL case
    // ldr w1, [x0, CONTEXT_ContextFlags]
    // clangs assembler doesn't seem to support the mov Wx, imm32 yet
    movz w2, #0x40, lsl #16
    movk w2, #0x2
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(Done_CONTEXT_INTEGER)

    ldp x1, x2,   [sp]
    ldr x3,       [sp, 16]

    stp x0, x1,   [x0, CONTEXT_X0]
    stp x2, x3,   [x0, CONTEXT_X2]
    stp x4, x5,   [x0, CONTEXT_X4]
    stp x6, x7,   [x0, CONTEXT_X6]
    stp x8, x9,   [x0, CONTEXT_X8]
    stp x10, x11, [x0, CONTEXT_X10]
    stp x12, x13, [x0, CONTEXT_X12]
    stp x14, x15, [x0, CONTEXT_X14]
    stp x16, x17, [x0, CONTEXT_X16]
    stp x18, x19, [x0, CONTEXT_X18]
    stp x20, x21, [x0, CONTEXT_X20]
    stp x22, x23, [x0, CONTEXT_X22]
    stp x24, x25, [x0, CONTEXT_X24]
    stp x26, x27, [x0, CONTEXT_X26]
    str x28,      [x0, CONTEXT_X28]


LOCAL_LABEL(Done_CONTEXT_INTEGER): 
    ldr w1, [x0, CONTEXT_ContextFlags]
    // clangs assembler doesn't seem to support the mov Wx, imm32 yet
    movz w2, #0x40, lsl #16
    movk w2, #0x4
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)

    add x0, x0,   CONTEXT_NEON_OFFSET
    stp q0, q1,   [x0, CONTEXT_V0]
    stp q2, q3,   [x0, CONTEXT_V2]
    stp q4, q5,   [x0, CONTEXT_V4]
    stp q6, q7,   [x0, CONTEXT_V6]
    stp q8, q9,   [x0, CONTEXT_V8]
    stp q10, q11, [x0, CONTEXT_V10]
    stp q12, q13, [x0, CONTEXT_V12]
    stp q14, q15, [x0, CONTEXT_V14]
    stp q16, q17, [x0, CONTEXT_V16]
    stp q18, q19, [x0, CONTEXT_V18]
    stp q20, q21, [x0, CONTEXT_V20]
    stp q22, q23, [x0, CONTEXT_V22]
    stp q24, q25, [x0, CONTEXT_V24]
    stp q26, q27, [x0, CONTEXT_V26]
    stp q28, q29, [x0, CONTEXT_V28]
    stp q30, q31, [x0, CONTEXT_V30]
    add x0, x0,   CONTEXT_FLOAT_CONTROL_OFFSET
    mrs x1, fpcr
    mrs x2, fpsr
    sub x0, x0,   CONTEXT_FLOAT_CONTROL_OFFSET
    stp x1, x2,   [x0, CONTEXT_Fpcr]
    sub x0, x0,   CONTEXT_NEON_OFFSET

LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):
    
    add sp, sp, #32
    ret
LEAF_END CONTEXT_CaptureContext, _TEXT

// Incoming:
//  x0: Context*

LEAF_ENTRY RtlCaptureContext, _TEXT
    sub sp, sp, #16
    str x1, [sp]
    // same as above, clang doesn't like mov with #imm32
    // keep this in sync if CONTEXT_FULL changes
    movz w1, #0x40, lsl #16
    orr w1, w1, #0x1
    orr w1, w1, #0x2
    orr w1, w1, #0x4
    orr w1, w1, #0x8
    str w1, [x0, CONTEXT_ContextFlags]
    ldr x1, [sp]
    add sp, sp, #16
    b C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

// Incoming:
//  x0: Context*
//  x1: Exception*
//
LEAF_ENTRY RtlRestoreContext, _TEXT
    // aarch64 specifies:
    //   IP0 and IP1, the Intra-Procedure Call temporary registers,
    //   are available for use by e.g. veneers or branch islands during a procedure call.
    //   They are otherwise corruptible.
    // Since we cannot control $pc directly, we're going to corrupt x16 and x17
    // so that we can restore control
    // since we potentially clobber x0 below, we'll bank it in x16
    mov x16, x0

    ldr w2, [x16, CONTEXT_ContextFlags]
    // clangs assembler doesn't seem to support the mov Wx, imm32 yet
    movz w3, #0x40, lsl #16
    movk w3, #0x4
    mov w4, w3
    and w3, w2, w3
    cmp w3, w4
    b.ne LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT)

    add x16, x16,   CONTEXT_NEON_OFFSET
    ldp q0, q1,   [x16, CONTEXT_V0]
    ldp q2, q3,   [x16, CONTEXT_V2]
    ldp q4, q5,   [x16, CONTEXT_V4]
    ldp q6, q7,   [x16, CONTEXT_V6]
    ldp q8, q9,   [x16, CONTEXT_V8]
    ldp q10, q11, [x16, CONTEXT_V10]
    ldp q12, q13, [x16, CONTEXT_V12]
    ldp q14, q15, [x16, CONTEXT_V14]
    ldp q16, q17, [x16, CONTEXT_V16]
    ldp q18, q19, [x16, CONTEXT_V18]
    ldp q20, q21, [x16, CONTEXT_V20]
    ldp q22, q23, [x16, CONTEXT_V22]
    ldp q24, q25, [x16, CONTEXT_V24]
    ldp q26, q27, [x16, CONTEXT_V26]
    ldp q28, q29, [x16, CONTEXT_V28]
    ldp q30, q31, [x16, CONTEXT_V30]
    ldp x1, x2,   [x16, CONTEXT_Fpcr]
    msr fpcr, x1
    msr fpsr, x2
    sub x16, x16,   CONTEXT_NEON_OFFSET

LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT):
    movz w2, #0x40, lsl #16
    movk w2, #0x2
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

    ldp x0, x1,   [x16, CONTEXT_X0]
    ldp x2, x3,   [x16, CONTEXT_X2]
    ldp x4, x5,   [x16, CONTEXT_X4]
    ldp x6, x7,   [x16, CONTEXT_X6]
    ldp x8, x9,   [x16, CONTEXT_X8]
    ldp x10, x11, [x16, CONTEXT_X10]
    ldp x12, x13, [x16, CONTEXT_X12]
    ldp x14, x15, [x16, CONTEXT_X14]
    ldp x18, x19, [x16, CONTEXT_X18]
    ldp x20, x21, [x16, CONTEXT_X20]
    ldp x22, x23, [x16, CONTEXT_X22]
    ldp x24, x25, [x16, CONTEXT_X24]
    ldp x26, x27, [x16, CONTEXT_X26]
    ldr x28,      [x16, CONTEXT_X28]

LOCAL_LABEL(No_Restore_CONTEXT_INTEGER):
    movz w2, #0x40, lsl #16
    movk w2, #0x2
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ldr w17, [x16, CONTEXT_Cpsr]
    msr nzcv, x17
    ldp fp, lr, [x16, CONTEXT_Fp]
    ldr x17, [x16, CONTEXT_Sp]
    mov sp, x17
    ldr x17, [x16, CONTEXT_Pc]
    br x17

LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ret

LEAF_END RtlRestoreContext, _TEXT
