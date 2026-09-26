// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Implementation of _CONTEXT_CaptureContext for the ARM64 platform.
// This function is processor dependent.  It is used by exception handling,
// and is always apply to the current thread.
//

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Incoming:
//  x0: Context*
//
.arch_extension sve
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    PROLOG_STACK_ALLOC 32
    .cfi_adjust_cfa_offset 32

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
    add x2, sp, #32
    stp x2, lr, [x0, CONTEXT_Sp]

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
    stp x1, x2,   [x0, CONTEXT_Fpcr]
    sub x0, x0,   CONTEXT_FLOAT_CONTROL_OFFSET + CONTEXT_NEON_OFFSET

LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):
    ldr w1, [x0, CONTEXT_ContextFlags]
    // clangs assembler doesn't seem to support the mov Wx, imm32 yet
    movz w2, #0x40, lsl #16
    movk w2, #0x40
    mov w3, w2
    and w2, w1, w2
    cmp w2, w3
    b.ne LOCAL_LABEL(Done_CONTEXT_SVE)

    ldr x1, [x0, CONTEXT_XSTATEFEATURESMASK_OFFSET]
    tbz x1, #XSTATE_ARM64_SVE_BIT, LOCAL_LABEL(Done_CONTEXT_SVE)

    add x2, x0, CONTEXT_Size + CONTEXT_SVE_STATE_HEADER_Size
    str z0, [x2, 0, MUL VL]
    str z1, [x2, 1, MUL VL]
    str z2, [x2, 2, MUL VL]
    str z3, [x2, 3, MUL VL]
    str z4, [x2, 4, MUL VL]
    str z5, [x2, 5, MUL VL]
    str z6, [x2, 6, MUL VL]
    str z7, [x2, 7, MUL VL]
    str z8, [x2, 8, MUL VL]
    str z9, [x2, 9, MUL VL]
    str z10, [x2, 10, MUL VL]
    str z11, [x2, 11, MUL VL]
    str z12, [x2, 12, MUL VL]
    str z13, [x2, 13, MUL VL]
    str z14, [x2, 14, MUL VL]
    str z15, [x2, 15, MUL VL]
    str z16, [x2, 16, MUL VL]
    str z17, [x2, 17, MUL VL]
    str z18, [x2, 18, MUL VL]
    str z19, [x2, 19, MUL VL]
    str z20, [x2, 20, MUL VL]
    str z21, [x2, 21, MUL VL]
    str z22, [x2, 22, MUL VL]
    str z23, [x2, 23, MUL VL]
    str z24, [x2, 24, MUL VL]
    str z25, [x2, 25, MUL VL]
    str z26, [x2, 26, MUL VL]
    str z27, [x2, 27, MUL VL]
    str z28, [x2, 28, MUL VL]
    str z29, [x2, 29, MUL VL]
    str z30, [x2, 30, MUL VL]
    str z31, [x2, 31, MUL VL]
    rdvl x3, #1
    add x2, x2, x3, lsl #5

    str p0, [x2, 0, MUL VL]
    str p1, [x2, 1, MUL VL]
    str p2, [x2, 2, MUL VL]
    str p3, [x2, 3, MUL VL]
    str p4, [x2, 4, MUL VL]
    str p5, [x2, 5, MUL VL]
    str p6, [x2, 6, MUL VL]
    str p7, [x2, 7, MUL VL]
    str p8, [x2, 8, MUL VL]
    str p9, [x2, 9, MUL VL]
    str p10, [x2, 10, MUL VL]
    str p11, [x2, 11, MUL VL]
    str p12, [x2, 12, MUL VL]
    str p13, [x2, 13, MUL VL]
    str p14, [x2, 14, MUL VL]
    str p15, [x2, 15, MUL VL]
    rdffr p15.b
    str p15, [x2, 16, MUL VL]
    ldr p15, [x2, 15, MUL VL]

LOCAL_LABEL(Done_CONTEXT_SVE):

    EPILOG_STACK_FREE 32
    ret
LEAF_END CONTEXT_CaptureContext, _TEXT

// Incoming:
//  x0: Context*

LEAF_ENTRY RtlCaptureContext, _TEXT
    PROLOG_STACK_ALLOC 16
    .cfi_adjust_cfa_offset 16
    str x1, [sp]
    // same as above, clang doesn't like mov with #imm32
    // keep this in sync if CONTEXT_FULL changes
    movz w1, #0x40, lsl #16
    orr w1, w1, #0x1
    orr w1, w1, #0x2
    orr w1, w1, #0x4
    orr w1, w1, #0x8
    str w1, [x0, CONTEXT_ContextFlags]
    str xzr, [x0, CONTEXT_XSTATEFEATURESMASK_OFFSET]
    ldr x1, [sp]
    EPILOG_STACK_FREE 16
    b C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

// Incoming:
//  x0: Context*
//  x1: Exception*
//
.arch_extension sve
LEAF_ENTRY RtlRestoreContext, _TEXT

#ifdef HAS_ADDRESS_SANITIZER
    ldr w17, [x0, #(CONTEXT_ContextFlags)]
    tbz w17, #CONTEXT_CONTROL_BIT, LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT)

    stp x0, x1, [sp, -16]!
    bl EXTERNAL_C_FUNC(__asan_handle_no_return)
    ldp x0, x1, [sp], 16

LOCAL_LABEL(Restore_CONTEXT_FLOATING_POINT):
#endif
    // aarch64 specifies:
    //   IP0 and IP1, the Intra-Procedure Call temporary registers,
    //   are available for use by e.g. veneers or branch islands during a procedure call.
    //   They are otherwise corruptible.
    // Since we cannot control $pc directly, we're going to corrupt x16 and x17
    // so that we can restore control
    // since we potentially clobber x0 below, we'll bank it in x16
    mov x16, x0

    ldr w17, [x16, CONTEXT_ContextFlags]
    tbz w17, #CONTEXT_FLOATING_POINT_BIT, LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT)

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
    add x16, x16, CONTEXT_FLOAT_CONTROL_OFFSET
    ldp x1, x2,   [x16, CONTEXT_Fpcr]
    msr fpcr, x1
    msr fpsr, x2
    sub x16, x16,   CONTEXT_FLOAT_CONTROL_OFFSET + CONTEXT_NEON_OFFSET

LOCAL_LABEL(No_Restore_CONTEXT_FLOATING_POINT):
    ldr w17, [x16, CONTEXT_ContextFlags]
    tbz w17, #CONTEXT_XSTATE_BIT, LOCAL_LABEL(No_Restore_CONTEXT_SVE)

    ldr x0, [x16, CONTEXT_XSTATEFEATURESMASK_OFFSET]
    tbz x0, #XSTATE_ARM64_SVE_BIT, LOCAL_LABEL(No_Restore_CONTEXT_SVE)

    add x1, x16, CONTEXT_Size + CONTEXT_SVE_STATE_HEADER_Size
    ldr z0, [x1, 0, MUL VL]
    ldr z1, [x1, 1, MUL VL]
    ldr z2, [x1, 2, MUL VL]
    ldr z3, [x1, 3, MUL VL]
    ldr z4, [x1, 4, MUL VL]
    ldr z5, [x1, 5, MUL VL]
    ldr z6, [x1, 6, MUL VL]
    ldr z7, [x1, 7, MUL VL]
    ldr z8, [x1, 8, MUL VL]
    ldr z9, [x1, 9, MUL VL]
    ldr z10, [x1, 10, MUL VL]
    ldr z11, [x1, 11, MUL VL]
    ldr z12, [x1, 12, MUL VL]
    ldr z13, [x1, 13, MUL VL]
    ldr z14, [x1, 14, MUL VL]
    ldr z15, [x1, 15, MUL VL]
    ldr z16, [x1, 16, MUL VL]
    ldr z17, [x1, 17, MUL VL]
    ldr z18, [x1, 18, MUL VL]
    ldr z19, [x1, 19, MUL VL]
    ldr z20, [x1, 20, MUL VL]
    ldr z21, [x1, 21, MUL VL]
    ldr z22, [x1, 22, MUL VL]
    ldr z23, [x1, 23, MUL VL]
    ldr z24, [x1, 24, MUL VL]
    ldr z25, [x1, 25, MUL VL]
    ldr z26, [x1, 26, MUL VL]
    ldr z27, [x1, 27, MUL VL]
    ldr z28, [x1, 28, MUL VL]
    ldr z29, [x1, 29, MUL VL]
    ldr z30, [x1, 30, MUL VL]
    ldr z31, [x1, 31, MUL VL]
    rdvl x2, #1
    add x1, x1, x2, lsl #5

    ldr p0, [x1, 16, MUL VL]
    wrffr p0.b
    ldr p0, [x1, 0, MUL VL]
    ldr p1, [x1, 1, MUL VL]
    ldr p2, [x1, 2, MUL VL]
    ldr p3, [x1, 3, MUL VL]
    ldr p4, [x1, 4, MUL VL]
    ldr p5, [x1, 5, MUL VL]
    ldr p6, [x1, 6, MUL VL]
    ldr p7, [x1, 7, MUL VL]
    ldr p8, [x1, 8, MUL VL]
    ldr p9, [x1, 9, MUL VL]
    ldr p10, [x1, 10, MUL VL]
    ldr p11, [x1, 11, MUL VL]
    ldr p12, [x1, 12, MUL VL]
    ldr p13, [x1, 13, MUL VL]
    ldr p14, [x1, 14, MUL VL]
    ldr p15, [x1, 15, MUL VL]

LOCAL_LABEL(No_Restore_CONTEXT_SVE):
    ldr w17, [x16, CONTEXT_ContextFlags]
    tbz w17, #CONTEXT_INTEGER_BIT, LOCAL_LABEL(No_Restore_CONTEXT_INTEGER)

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
    tbz w17, #CONTEXT_CONTROL_BIT, LOCAL_LABEL(No_Restore_CONTEXT_CONTROL)

    ldr w17, [x16, CONTEXT_Cpsr]
    msr nzcv, x17
    ldp fp, lr, [x16, CONTEXT_Fp]
    ldp x16, x17, [x16, CONTEXT_Sp] // Context_Pc is right after Context_Sp
    mov sp, x16
    br x17

LOCAL_LABEL(No_Restore_CONTEXT_CONTROL):
    ret

LEAF_END RtlRestoreContext, _TEXT

#ifdef __APPLE__

// Incoming:
//  x0: Context*
//  x1: Exception*
//
LEAF_ENTRY RestoreCompleteContext, _TEXT
    // We cannot restore all registers in the user mode code, so we rely on a help from kernel here.
    // The following instruction is an undefined instruction. In the hardware exception handler, we check
    // if the faulting address is the RtlRestoreContext and in case it is, we update the context of
    // the faulting thread using the CONTEXT pointed to by the x0 register.
    // While this could be used for the full fidelity RtlRestoreContext implementation, it is too
    // expensive for general usage of the RtlRestoreContext.
    UDF #0
LEAF_END RestoreCompleteContext, _TEXT

#endif // __APPLE__
