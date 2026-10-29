// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"
#include <amd64/FuncletCall.inc>

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// DWORD_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilterIP, UINT_PTR* pFuncletCallerSP)
//
// INPUT:  RDI:  exception object
//         RSI:  RBP of the parent method
//         RDX:  filter funclet address
//         RCX:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: RAX:  result of the filter execution
//
// NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the frame layout.
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE 0, 1

        mov     rbp, rsi
        mov     [rcx], rsp

        call    rdx

ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        ret

NESTED_END CallEHFilterFunclet, _TEXT
