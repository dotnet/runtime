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
    //                           $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0x20
    //             $s0=23.
    PROLOG_SAVE_REG 23, 16
    ld.wu  $a1, $a0, CallDescrData__numStackSlots

    ori  $s0, $a0, 0 // save pCallDescrData in s0
    beq  $a1, $zero, LOCAL_LABEL(donestack)

    slli.d  $a2, $a1, 3
    andi  $a0, $a2, 0x8
    sub.d  $t4, $sp, $a0   //padding on high-addr.
    add.d  $a0, $a0, $a2
    sub.d  $sp, $sp, $a0   //stack-16byte aligned.

    ld.d  $a0, $s0, CallDescrData__pSrc

    add.d  $a2, $a0, $a2           // pSrcEnd=pSrc+8*numStackSlots

    // This loop copies numStackSlots words
    // from [pSrcEnd-8,pSrcEnd-16,...] to [sp-8,sp-16,...]
LOCAL_LABEL(stackloop):
    addi.d  $a2, $a2, -8
    ld.d  $a4, $a2, 0
    addi.d  $t4, $t4, -8
    st.d  $a4, $t4, 0
    addi.d    $a1, $a1, -1
    bne $a1, $zero, LOCAL_LABEL(stackloop)

LOCAL_LABEL(donestack):
    // If FP arguments are supplied in registers ($t4 != NULL)
    ld.d  $t4, $s0, CallDescrData__pFloatArgumentRegisters
    beq  $t4, $zero, LOCAL_LABEL(NoFloatingPoint)

    fld.d  $fa0, $t4, 0
    fld.d  $fa1, $t4, 8
    fld.d  $fa2, $t4, 16
    fld.d  $fa3, $t4, 24
    fld.d  $fa4, $t4, 32
    fld.d  $fa5, $t4, 40
    fld.d  $fa6, $t4, 48
    fld.d  $fa7, $t4, 56
LOCAL_LABEL(NoFloatingPoint):

    // Copy [pArgumentRegisters, ..., pArgumentRegisters + 56]
    // into $a0, ..., a7

    ld.d  $t4, $s0, CallDescrData__pArgumentRegisters
    ld.d  $a0, $t4, 0
    ld.d  $a1, $t4, 8
    ld.d  $a2, $t4, 16
    ld.d  $a3, $t4, 24
    ld.d  $a4, $t4, 32
    ld.d  $a5, $t4, 40
    ld.d  $a6, $t4, 48
    ld.d  $a7, $t4, 56

    ld.d  $t4, $s0, CallDescrData__pTarget

    // call pTarget
    jirl $ra, $t4, 0

    ld.w  $a3, $s0, CallDescrData__fpReturnSize

    // Int return case
    beq  $a3, $zero, LOCAL_LABEL(IntReturn)

    // Struct with Float/Double field return case.
    ori  $t4, $zero, CallDescrData__flagOneFloat
    beq  $t4, $a3, LOCAL_LABEL(FloatReturn)

    ori  $t4, $zero, CallDescrData__flagOneDouble
    beq  $t4, $a3, LOCAL_LABEL(DoubleReturn)

    ori  $t4, $zero, CallDescrData__flagFloatInt
    beq  $t4, $a3, LOCAL_LABEL(FloatIntReturn)

    ori  $t4, $zero, CallDescrData__flagDoubleInt
    beq  $t4, $a3, LOCAL_LABEL(DoubleIntReturn)

    ori  $t4, $zero, CallDescrData__flagFloatLong
    beq  $t4, $a3, LOCAL_LABEL(FloatLongReturn)

    ori  $t4, $zero, CallDescrData__flagDoubleLong
    beq  $t4, $a3, LOCAL_LABEL(DoubleLongReturn)

    ori  $t4, $zero, CallDescrData__flagIntFloat
    beq  $t4, $a3, LOCAL_LABEL(IntFloatReturn)

    ori  $t4, $zero, CallDescrData__flagLongFloat
    beq  $t4, $a3, LOCAL_LABEL(LongFloatReturn)

    ori  $t4, $zero, CallDescrData__flagIntDouble
    beq  $t4, $a3, LOCAL_LABEL(IntDoubleReturn)

    ori  $t4, $zero, CallDescrData__flagLongDouble
    beq  $t4, $a3, LOCAL_LABEL(LongDoubleReturn)

    ori  $t4, $zero, CallDescrData__flagFloatFloat
    beq  $t4, $a3, LOCAL_LABEL(FloatFloatReturn)

    ori  $t4, $zero, CallDescrData__flagDoubleFloat
    beq  $t4, $a3, LOCAL_LABEL(DoubleFloatReturn)

    ori  $t4, $zero, CallDescrData__flagFloatDouble
    beq  $t4, $a3, LOCAL_LABEL(FloatDoubleReturn)

    ori  $t4, $zero, CallDescrData__flagDoubleDouble
    beq  $t4, $a3, LOCAL_LABEL(DoubleDoubleReturn)

    //b  LOCAL_LABEL(NotCorrectReturn)
LOCAL_LABEL(NotCorrectReturn):
    st.w  $ra, $zero, 0
    EMIT_BREAKPOINT // Unreachable

LOCAL_LABEL(FloatReturn):
    fst.s  $f0, $s0, CallDescrData__returnValue
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleReturn):
    fst.d  $f0, $s0, CallDescrData__returnValue
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatIntReturn):
    fst.s  $f0, $s0, CallDescrData__returnValue
    st.w  $a0, $s0, CallDescrData__returnValue + 4
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleIntReturn):
    fst.d  $f0, $s0, CallDescrData__returnValue
    st.w  $a0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatLongReturn):
    fst.s  $f0, $s0, CallDescrData__returnValue
    st.d  $a0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleLongReturn):
    fst.d  $f0, $s0, CallDescrData__returnValue
    st.d  $a0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntFloatReturn):
    st.w  $a0, $s0, CallDescrData__returnValue
    fst.s  $f0, $s0, CallDescrData__returnValue + 4
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(LongFloatReturn):
    st.d  $a0, $s0, CallDescrData__returnValue
    fst.s  $f0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntDoubleReturn):
    st.w  $a0, $s0, CallDescrData__returnValue
    fst.d  $f0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(LongDoubleReturn):
    st.d  $a0, $s0, CallDescrData__returnValue
    fst.d  $f0, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatFloatReturn):
    fst.s  $f0, $s0, CallDescrData__returnValue
    fst.s  $f1, $s0, CallDescrData__returnValue + 4
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleFloatReturn):
    fst.d  $f0, $s0, CallDescrData__returnValue
    fst.s  $f1, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(FloatDoubleReturn):
    fst.s  $f0, $s0, CallDescrData__returnValue
    fst.d  $f1, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(DoubleDoubleReturn):
    fst.d  $f0, $s0, CallDescrData__returnValue
    fst.d  $f1, $s0, CallDescrData__returnValue + 8
    b  LOCAL_LABEL(ReturnDone)

LOCAL_LABEL(IntReturn):
    // Save return value into retbuf for int
    st.d  $a0, $s0, CallDescrData__returnValue
    st.d  $a1, $s0, CallDescrData__returnValue + 8

LOCAL_LABEL(ReturnDone):

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG  23, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x20
    jirl  $r0, $ra, 0
NESTED_END CallDescrWorkerInternal, _TEXT
