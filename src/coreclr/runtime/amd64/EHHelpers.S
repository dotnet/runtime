// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

//
// Prologue of the funclet calling helpers. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk
// hard-codes the resulting frame layout.
//
.macro FUNCLET_CALL_PROLOGUE localsCount, alignStack

        push_nonvol_reg r15     // save preserved regs for OS stackwalker
        push_nonvol_reg r14     // ...
        push_nonvol_reg r13     // ...
        push_nonvol_reg r12     // ...
        push_nonvol_reg rbx     // ...
        push_nonvol_reg rbp     // ...

        stack_alloc_size = \localsCount * 8 + \alignStack * 8

        alloc_stack     stack_alloc_size

        END_PROLOGUE
.endm

//
// Epilogue of the funclet calling helpers
//
.macro FUNCLET_CALL_EPILOGUE
        free_stack      stack_alloc_size

        pop_nonvol_reg rbp
        pop_nonvol_reg rbx
        pop_nonvol_reg r12
        pop_nonvol_reg r13
        pop_nonvol_reg r14
        pop_nonvol_reg r15
.endm

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// UINT_PTR CallEHFunclet(Object* pThrowable, PCODE pFunclet, EHFuncletRegs* pRegs, UINT_PTR* pFuncletCallerSP)
//
// Calls a catch or finally funclet with the preserved registers of the parent frame loaded through the
// register pointers in pRegs (the REGDISPLAY on NativeAOT).
//
// On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved registers are written back
// through pRegs after it returns since a GC during the funclet may have updated them.
//
// INPUT:  RDI:  exception object (null for finally funclets)
//         RSI:  funclet address
//         RDX:  register pointers
//         RCX:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: RAX:  resume address for catch funclets
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE 2, 1

        locThread       = 0
        locRegs         = 8

        mov     [rcx], rsp

#ifdef FEATURE_NATIVEAOT
        mov     [rsp + locRegs], rdx
        mov     rbx, rdi
        mov     r12, rsi

        INLINE_GETTHREAD
        mov     [rsp + locThread], rax

        // Clear the DoNotTriggerGc state before calling out to the managed funclet.
   lock and     dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], ~TSF_DoNotTriggerGc

        mov     rdi, rbx
        mov     rsi, r12
        mov     rdx, [rsp + locRegs]
#endif

        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pRbx]
        mov     rbx, [rax]
        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pRbp]
        mov     rbp, [rax]
        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pR12]
        mov     r12, [rax]
        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pR13]
        mov     r13, [rax]
        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pR14]
        mov     r14, [rax]
        mov     rax, [rdx + OFFSETOF__EHFuncletRegs__pR15]
        mov     r15, [rax]

        call    rsi

ALTERNATE_ENTRY CallEHFunclet2

#ifdef FEATURE_NATIVEAOT
        mov     rdx, [rsp + locRegs]
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRbx]
        mov     [rcx], rbx
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRbp]
        mov     [rcx], rbp
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR12]
        mov     [rcx], r12
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR13]
        mov     [rcx], r13
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR14]
        mov     [rcx], r14
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR15]
        mov     [rcx], r15

        // Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        mov     rcx, [rsp + locThread]
   lock or      dword ptr [rcx + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc
#endif

        FUNCLET_CALL_EPILOGUE
        ret

NESTED_END CallEHFunclet, _TEXT

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// UINT_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
//
// INPUT:  RDI:  exception object
//         RSI:  RBP of the parent method
//         RDX:  filter funclet address
//         RCX:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: RAX:  result of the filter execution
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
