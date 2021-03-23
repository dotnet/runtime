// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//-----------------------------------------------------------------------------
// This helper routine enregisters the appropriate arguments and makes the
// actual call.
//-----------------------------------------------------------------------------
//void CallDescrWorkerInternal(CallDescrData * pCallDescrData);

NESTED_ENTRY CallDescrWorkerInternal, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
    PROLOG_SAVE_REG    x19, 16           //the stack slot at sp+24 is empty for 16 byte alligment

    mov     x19, x0 // save pCallDescrData in x19

    ldr     w1, [x19,#CallDescrData__numStackSlots]
    cbz     w1, LOCAL_LABEL(donestack)

    // Add frame padding to ensure frame size is a multiple of 16 (a requirement of the OS ABI).
    // We push two registers (above) and numStackSlots arguments (below). If this comes to an odd number
    // of slots we must pad with another. This simplifies to "if the low bit of numStackSlots is set,
    // extend the stack another eight bytes".
    ldr     x0, [x19,#CallDescrData__pSrc]
    add     x0, x0, x1, lsl #3           // pSrcEnd=pSrc+8*numStackSlots
    ands    x2, x1, #1
    beq     LOCAL_LABEL(stackloop)

    // This loop copies numStackSlots words
    // from [pSrcEnd-8,pSrcEnd-16,...] to [sp-8,sp-16,...]

    // pad and store one stack slot as number of slots are odd
    ldr     x4, [x0,#-8]!
    str     x4, [sp,#-16]!
    subs    x1, x1, #1
    beq     LOCAL_LABEL(donestack)
LOCAL_LABEL(stackloop):
    ldp     x2, x4, [x0,#-16]!
    stp     x2, x4, [sp,#-16]!
    subs    x1, x1, #2
    bne     LOCAL_LABEL(stackloop)
LOCAL_LABEL(donestack):

    // If FP arguments are supplied in registers (x9 != NULL) then initialize all of them from the pointer
    // given in x8.
    ldr     x9, [x19,#CallDescrData__pFloatArgumentRegisters]
    cbz     x9, LOCAL_LABEL(NoFloatingPoint)
    ldp     q0, q1, [x9]
    ldp     q2, q3, [x9, #32]
    ldp     q4, q5, [x9, #64]
    ldp     q6, q7, [x9, #96]
LOCAL_LABEL(NoFloatingPoint):

    // Copy [pArgumentRegisters, ..., pArgumentRegisters + 56]
    // into x0, ..., x7

    ldr     x9, [x19,#CallDescrData__pArgumentRegisters]
    ldp     x0, x1, [x9]
    ldp     x2, x3, [x9, #16]
    ldp     x4, x5, [x9, #32]
    ldp     x6, x7, [x9, #48]

    // Copy pRetBuffArg into x8
    ldr     x9, [x19,#CallDescrData__pRetBuffArg]
    ldr     x8, [x9]

    // call pTarget
    ldr     x9, [x19,#CallDescrData__pTarget]
    blr     x9

    ldr     w3, [x19,#CallDescrData__fpReturnSize]

    // Int return case
    cbz     w3, LOCAL_LABEL(IntReturn)

    // Float return case
    cmp     w3, #4
    beq     LOCAL_LABEL(FloatReturn)

    // Double return case
    cmp     w3, #8
    bne     LOCAL_LABEL(NoDoubleReturn)

LOCAL_LABEL(FloatReturn):
    str     q0, [x19, #(CallDescrData__returnValue + 0)]
    b       LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(NoDoubleReturn):

    //FloatHFAReturn  return case
    cmp     w3, #16
    bne     LOCAL_LABEL(NoFloatHFAReturn)

    stp     s0, s1, [x19, #(CallDescrData__returnValue + 0)]
    stp     s2, s3, [x19, #(CallDescrData__returnValue + 0x08)]
    b       LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(NoFloatHFAReturn):

    //DoubleHFAReturn  return case
    cmp     w3, #32
    bne     LOCAL_LABEL(NoDoubleHFAReturn)

    stp     d0, d1, [x19, #(CallDescrData__returnValue + 0)]
    stp     d2, d3, [x19, #(CallDescrData__returnValue + 0x10)]
    b       LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(NoDoubleHFAReturn):

    //VectorHFAReturn  return case
    cmp     w3, #64
    bne     LOCAL_LABEL(LNoVectorHFAReturn)

    stp     q0, q1, [x19, #(CallDescrData__returnValue + 0)]
    stp     q2, q3, [x19, #(CallDescrData__returnValue + 0x20)]
    b       LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(LNoVectorHFAReturn):

    EMIT_BREAKPOINT // Unreachable

LOCAL_LABEL(IntReturn):
    // Save return value into retbuf for int
    stp     x0, x1, [x19, #(CallDescrData__returnValue + 0)]

LOCAL_LABEL(ReturnDone):

#ifdef _DEBUG
    // trash the floating point registers to ensure that the HFA return values
    // won't survive by accident
    ldp     d0, d1, [sp]
    ldp     d2, d3, [sp, #16]
#endif

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG      x19, 16    //the stack slot at sp+24 is empty for 16 byte alligment
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallDescrWorkerInternal, _TEXT
