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
    PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0x20
    PROLOG_SAVE_REG s1, 16

    lwu a1, CallDescrData__numStackSlots(a0)

    addi s1, a0, 0 // save pCallDescrData in s1
    beq a1, zero, LOCAL_LABEL(donestack)

    slli a2, a1, 3
    andi a0, a2, 0x8
    sub t4, sp, a0 // padding on high-addr
    add a0, a0, a2
    sub sp, sp, a0 // stack-16byte aligned

    ld a0, CallDescrData__pSrc(s1)

    add a2, a0, a2 // pSrcEnd=pSrc+8*numStackSlots

    // This loop copies numStackSlots words
    // from [pSrcEnd-8,pSrcEnd-16,...] to [sp-8,sp-16,...]
LOCAL_LABEL(stackloop):
    addi a2, a2, -8
    ld a4, 0(a2)
    addi t4, t4, -8
    sd a4, 0(t4)
    addi a1, a1, -1
    bne a1, zero, LOCAL_LABEL(stackloop)

LOCAL_LABEL(donestack):
    // If FP arguments are supplied in registers (t4 != NULL)
    ld t4, CallDescrData__pFloatArgumentRegisters(s1)
    beq t4, zero, LOCAL_LABEL(NoFloatingPoint)

    fld fa0, 0(t4)
    fld fa1, 8(t4)
    fld fa2, 16(t4)
    fld fa3, 24(t4)
    fld fa4, 32(t4)
    fld fa5, 40(t4)
    fld fa6, 48(t4)
    fld fa7, 56(t4)

LOCAL_LABEL(NoFloatingPoint):
    // Copy [pArgumentRegisters, ..., pArgumentRegisters + 56]
    // into a0, ..., a7
    ld t4, CallDescrData__pArgumentRegisters(s1)
    ld a0, 0(t4)
    ld a1, 8(t4)
    ld a2, 16(t4)
    ld a3, 24(t4)
    ld a4, 32(t4)
    ld a5, 40(t4)
    ld a6, 48(t4)
    ld a7, 56(t4)

    ld t4, CallDescrData__pTarget(s1)

    // call pTarget
    jalr t4

    lw a3, CallDescrData__fpReturnSize(s1)

    // Int return case
    beq a3, zero, LOCAL_LABEL(IntReturn)

    // Struct with Float/Double field return case.
    ori  t4, zero, CallDescrData__flagOneFloat
    beq  t4, a3, LOCAL_LABEL(FloatReturn)

    ori  t4, zero, CallDescrData__flagOneDouble
    beq  t4, a3, LOCAL_LABEL(DoubleReturn)

    ori  t4, zero, CallDescrData__flagFloatInt
    beq  t4, a3, LOCAL_LABEL(FloatIntReturn)

    ori  t4, zero, CallDescrData__flagDoubleInt
    beq  t4, a3, LOCAL_LABEL(DoubleIntReturn)

    ori  t4, zero, CallDescrData__flagFloatLong
    beq  t4, a3, LOCAL_LABEL(FloatLongReturn)

    ori  t4, zero, CallDescrData__flagDoubleLong
    beq  t4, a3, LOCAL_LABEL(DoubleLongReturn)

    ori  t4, zero, CallDescrData__flagIntFloat
    beq  t4, a3, LOCAL_LABEL(IntFloatReturn)

    ori  t4, zero, CallDescrData__flagLongFloat
    beq  t4, a3, LOCAL_LABEL(LongFloatReturn)

    ori  t4, zero, CallDescrData__flagIntDouble
    beq  t4, a3, LOCAL_LABEL(IntDoubleReturn)

    ori  t4, zero, CallDescrData__flagLongDouble
    beq  t4, a3, LOCAL_LABEL(LongDoubleReturn)

    ori  t4, zero, CallDescrData__flagFloatFloat
    beq  t4, a3, LOCAL_LABEL(FloatFloatReturn)

    ori  t4, zero, CallDescrData__flagDoubleFloat
    beq  t4, a3, LOCAL_LABEL(DoubleFloatReturn)

    ori  t4, zero, CallDescrData__flagFloatDouble
    beq  t4, a3, LOCAL_LABEL(FloatDoubleReturn)

    ori  t4, zero, CallDescrData__flagDoubleDouble
    beq  t4, a3, LOCAL_LABEL(DoubleDoubleReturn)

LOCAL_LABEL(NotCorrectReturn):
    sw  ra, 0(zero)
    EMIT_BREAKPOINT // Unreachable

LOCAL_LABEL(FloatReturn):
    fsw  f0, CallDescrData__returnValue(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleReturn):
    fsd  fa0, CallDescrData__returnValue(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatIntReturn):
    fsw  fa0, CallDescrData__returnValue(s1)
    sw  a0, (CallDescrData__returnValue + 4)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleIntReturn):
    fsd  fa0, CallDescrData__returnValue(s1)
    sw  a0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatLongReturn):
    fsw  fa0, CallDescrData__returnValue(s1)
    sd  a0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleLongReturn):
    fsd  fa0, CallDescrData__returnValue(s1)
    sd  a0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntFloatReturn):
    sw  a0, CallDescrData__returnValue(s1)
    fsw  fa0, (CallDescrData__returnValue + 4)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(LongFloatReturn):
    sd  a0, CallDescrData__returnValue(s1)
    fsw  fa0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntDoubleReturn):
    sw  a0, CallDescrData__returnValue(s1)
    fsd  fa0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(LongDoubleReturn):
    sd  a0, CallDescrData__returnValue(s1)
    fsd  fa0, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatFloatReturn):
    fsw  fa0, CallDescrData__returnValue(s1)
    fsw  fa1, (CallDescrData__returnValue + 4)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleFloatReturn):
    fsd  fa0, CallDescrData__returnValue(s1)
    fsw  fa1, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatDoubleReturn):
    fsw  fa0, CallDescrData__returnValue(s1)
    fsd  fa1, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleDoubleReturn):
    fsd  fa0, CallDescrData__returnValue(s1)
    fsd  fa1, (CallDescrData__returnValue + 8)(s1)
    j  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntReturn):
    // Save return value into retbuf for int
    sd  a0, CallDescrData__returnValue(s1)
    sd  a1, (CallDescrData__returnValue + 8)(s1)

LOCAL_LABEL(ReturnDone):

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG  s1, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x20
    ret
NESTED_END CallDescrWorkerInternal, _TEXT
