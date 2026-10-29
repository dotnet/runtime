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

        addi  sp, sp, -0x70
        .cfi_adjust_cfa_offset 0x70
        PROLOG_SAVE_REG fp,  0x00
        PROLOG_SAVE_REG ra,  0x08
        PROLOG_SAVE_REG s1,  0x10
        PROLOG_SAVE_REG s2,  0x18
        PROLOG_SAVE_REG s3,  0x20
        PROLOG_SAVE_REG s4,  0x28
        PROLOG_SAVE_REG s5,  0x30
        PROLOG_SAVE_REG s6,  0x38
        PROLOG_SAVE_REG s7,  0x40
        PROLOG_SAVE_REG s8,  0x48
        PROLOG_SAVE_REG s9,  0x50
        PROLOG_SAVE_REG s10, 0x58
        PROLOG_SAVE_REG s11, 0x60
        // Slot at 0x68 is alignment padding

        addi  sp, sp, -0x60
        .cfi_adjust_cfa_offset 0x60
        fsd   fs0,  0x00(sp)
        fsd   fs1,  0x08(sp)
        fsd   fs2,  0x10(sp)
        fsd   fs3,  0x18(sp)
        fsd   fs4,  0x20(sp)
        fsd   fs5,  0x28(sp)
        fsd   fs6,  0x30(sp)
        fsd   fs7,  0x38(sp)
        fsd   fs8,  0x40(sp)
        fsd   fs9,  0x48(sp)
        fsd   fs10, 0x50(sp)
        fsd   fs11, 0x58(sp)

        sd    sp, 0(a3)
        mv    fp, a1

        jalr  a2

ALTERNATE_ENTRY CallEHFilterFunclet2

        fld   fs0,  0x00(sp)
        fld   fs1,  0x08(sp)
        fld   fs2,  0x10(sp)
        fld   fs3,  0x18(sp)
        fld   fs4,  0x20(sp)
        fld   fs5,  0x28(sp)
        fld   fs6,  0x30(sp)
        fld   fs7,  0x38(sp)
        fld   fs8,  0x40(sp)
        fld   fs9,  0x48(sp)
        fld   fs10, 0x50(sp)
        fld   fs11, 0x58(sp)
        addi  sp, sp, 0x60
        .cfi_adjust_cfa_offset -0x60

        EPILOG_RESTORE_REG s1,  0x10
        EPILOG_RESTORE_REG s2,  0x18
        EPILOG_RESTORE_REG s3,  0x20
        EPILOG_RESTORE_REG s4,  0x28
        EPILOG_RESTORE_REG s5,  0x30
        EPILOG_RESTORE_REG s6,  0x38
        EPILOG_RESTORE_REG s7,  0x40
        EPILOG_RESTORE_REG s8,  0x48
        EPILOG_RESTORE_REG s9,  0x50
        EPILOG_RESTORE_REG s10, 0x58
        EPILOG_RESTORE_REG s11, 0x60
        EPILOG_RESTORE_REG fp,  0x00
        EPILOG_RESTORE_REG ra,  0x08
        addi  sp, sp, 0x70
        .cfi_adjust_cfa_offset -0x70
        EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT
