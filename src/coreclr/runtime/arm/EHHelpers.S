// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include "AsmMacros_Shared.h"

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// DWORD_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilterIP, UINT_PTR* pFuncletCallerSP)
//
// INPUT:  R0:  exception object
//         R1:  R11 of the parent method
//         R2:  filter funclet address
//         R3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: R0:  result of the filter execution
//
// NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the frame layout.
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        PROLOG_PUSH "{r3,r4-r11,lr}"
        PROLOG_VPUSH {d8-d15}

        str         sp, [r3]
        mov         r11, r1

        blx         r2

GLOBAL_LABEL CallEHFilterFunclet2

        EPILOG_VPOP {d8-d15}
        EPILOG_POP  "{r3,r4-r11,pc}"

NESTED_END CallEHFilterFunclet, _TEXT
