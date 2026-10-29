// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

//
// DWORD_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilterIP, UINT_PTR* pFuncletCallerSP)
//
// INPUT:  a0:  exception object
//         a1:  FP of the parent method
//         a2:  filter funclet address
//         a3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: a0:  result of the filter execution
//
// NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the frame layout. The CFA must stay
// SP-based since FP holds the parent's frame pointer while the filter runs.
//
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        addi.d  $sp, $sp, -0x60
        .cfi_adjust_cfa_offset 0x60
        PROLOG_SAVE_REG 22, 0x00
        PROLOG_SAVE_REG 1,  0x08
        PROLOG_SAVE_REG 23, 0x10
        PROLOG_SAVE_REG 24, 0x18
        PROLOG_SAVE_REG 25, 0x20
        PROLOG_SAVE_REG 26, 0x28
        PROLOG_SAVE_REG 27, 0x30
        PROLOG_SAVE_REG 28, 0x38
        PROLOG_SAVE_REG 29, 0x40
        PROLOG_SAVE_REG 30, 0x48
        PROLOG_SAVE_REG 31, 0x50
        // Slot at 0x58 is alignment padding

        addi.d  $sp, $sp, -0x40
        .cfi_adjust_cfa_offset 0x40
        fst.d   $f24, $sp, 0x00
        fst.d   $f25, $sp, 0x08
        fst.d   $f26, $sp, 0x10
        fst.d   $f27, $sp, 0x18
        fst.d   $f28, $sp, 0x20
        fst.d   $f29, $sp, 0x28
        fst.d   $f30, $sp, 0x30
        fst.d   $f31, $sp, 0x38

        st.d    $sp, $a3, 0
        ori     $fp, $a1, 0

        jirl    $ra, $a2, 0

ALTERNATE_ENTRY CallEHFilterFunclet2

        fld.d   $f24, $sp, 0x00
        fld.d   $f25, $sp, 0x08
        fld.d   $f26, $sp, 0x10
        fld.d   $f27, $sp, 0x18
        fld.d   $f28, $sp, 0x20
        fld.d   $f29, $sp, 0x28
        fld.d   $f30, $sp, 0x30
        fld.d   $f31, $sp, 0x38
        addi.d  $sp, $sp, 0x40
        .cfi_adjust_cfa_offset -0x40

        ld.d    $r23, $sp, 0x10
        ld.d    $r24, $sp, 0x18
        ld.d    $r25, $sp, 0x20
        ld.d    $r26, $sp, 0x28
        ld.d    $r27, $sp, 0x30
        ld.d    $r28, $sp, 0x38
        ld.d    $r29, $sp, 0x40
        ld.d    $r30, $sp, 0x48
        ld.d    $r31, $sp, 0x50
        ld.d    $fp,  $sp, 0x00
        ld.d    $ra,  $sp, 0x08
        .cfi_restore 23
        .cfi_restore 24
        .cfi_restore 25
        .cfi_restore 26
        .cfi_restore 27
        .cfi_restore 28
        .cfi_restore 29
        .cfi_restore 30
        .cfi_restore 31
        .cfi_restore 22
        .cfi_restore 1
        addi.d  $sp, $sp, 0x60
        .cfi_adjust_cfa_offset -0x60
        EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT
