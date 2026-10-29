// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

//
// DWORD_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilterIP, UINT_PTR* pFuncletCallerSP)
//
// INPUT:  X0:  exception object
//         X1:  FP of the parent method
//         X2:  filter funclet address
//         X3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: X0:  result of the filter execution
//
// NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the frame layout. The CFA must stay
// SP-based since FP holds the parent's frame pointer while the filter runs.
//
    NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED fp, lr, -0x60
        PROLOG_SAVE_REG_PAIR x19, x20, 0x10
        PROLOG_SAVE_REG_PAIR x21, x22, 0x20
        PROLOG_SAVE_REG_PAIR x23, x24, 0x30
        PROLOG_SAVE_REG_PAIR x25, x26, 0x40
        PROLOG_SAVE_REG_PAIR x27, x28, 0x50
        PROLOG_STACK_ALLOC 0x40
        .cfi_adjust_cfa_offset 0x40
        stp d8, d9,   [sp, #0x00]
        stp d10, d11, [sp, #0x10]
        stp d12, d13, [sp, #0x20]
        stp d14, d15, [sp, #0x30]

        mov         x4, sp
        str         x4, [x3]
        mov         fp, x1

        blr         x2

ALTERNATE_ENTRY CallEHFilterFunclet2

        ldp         d8, d9,   [sp, #0x00]
        ldp         d10, d11, [sp, #0x10]
        ldp         d12, d13, [sp, #0x20]
        ldp         d14, d15, [sp, #0x30]
        EPILOG_STACK_FREE 0x40
        EPILOG_RESTORE_REG_PAIR x19, x20, 0x10
        EPILOG_RESTORE_REG_PAIR x21, x22, 0x20
        EPILOG_RESTORE_REG_PAIR x23, x24, 0x30
        EPILOG_RESTORE_REG_PAIR x25, x26, 0x40
        EPILOG_RESTORE_REG_PAIR x27, x28, 0x50
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 0x60
        EPILOG_RETURN

    NESTED_END CallEHFilterFunclet, _TEXT
