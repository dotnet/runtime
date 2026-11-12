// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include "AsmMacros_Shared.h"

//
// Prologue of the funclet calling helpers. They save all callee-saved registers since the funclets do not
// preserve them. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the resulting frame layout.
//
// The helpers set r11 to the frame pointer of the parent frame before calling the funclet, so the frame is
// SP based.
//
#define FUNCLET_CALL_LOCALS_SIZE 12

.macro FUNCLET_CALL_PROLOGUE
        PROLOG_PUSH  "{r4-r11, lr}"
        PROLOG_VPUSH {d8-d15}
        PROLOG_STACK_ALLOC FUNCLET_CALL_LOCALS_SIZE
.endm

//
// Epilogue of the funclet calling helpers. The unwind directives of the prologue describe the whole function,
// so the locals are freed without EPILOG_STACK_FREE, which would cancel the .pad of PROLOG_STACK_ALLOC.
//
.macro FUNCLET_CALL_EPILOGUE
        add     sp, sp, #FUNCLET_CALL_LOCALS_SIZE
        EPILOG_VPOP {d8-d15}
        EPILOG_POP  "{r4-r11, pc}"
.endm

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// UINT_PTR CallEHFunclet(Object* pThrowable, PCODE pFunclet, void* pRegs, UINT_PTR* pFuncletCallerSP)
//
// Calls a catch or finally funclet with the preserved registers of the parent frame loaded from pRegs
// (the CONTEXT on CoreCLR, the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER
// and LOAD_FUNCLET_FP_REGISTER.
//
// On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved registers are written back
// through the REGDISPLAY after it returns since a GC during the funclet may have updated them.
//
// INPUT:  R0:  exception object (null for finally funclets)
//         R1:  funclet address
//         R2:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         R3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: R0:  resume address for catch funclets
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

#define locThread   0
#define locRegs     4

        str     sp, [r3]

#ifdef FEATURE_NATIVEAOT
        str     r2, [sp, #locRegs]

        // INLINE_GETTHREAD trashes r0-r3.
        mov     r4, r0
        mov     r5, r1
        INLINE_GETTHREAD                                    // r0 <- Thread*
        str     r0, [sp, #locThread]

        // Clear the DoNotTriggerGc state before calling out to the managed funclet.
LOCAL_LABEL(CallEHFunclet_ClearRetry):
        ldrex   r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        bic     r1, r1, #TSF_DoNotTriggerGc
        strex   r2, r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        cmp     r2, #0
        bne     LOCAL_LABEL(CallEHFunclet_ClearRetry)

        mov     r0, r4
        mov     r1, r5
        ldr     r2, [sp, #locRegs]
#endif

        LOAD_FUNCLET_REGISTER r2, 4, r12
        LOAD_FUNCLET_REGISTER r2, 5, r12
        LOAD_FUNCLET_REGISTER r2, 6, r12
        LOAD_FUNCLET_REGISTER r2, 7, r12
        LOAD_FUNCLET_REGISTER r2, 8, r12
        LOAD_FUNCLET_REGISTER r2, 9, r12
        LOAD_FUNCLET_REGISTER r2, 10, r12
        LOAD_FUNCLET_REGISTER r2, 11, r12

        LOAD_FUNCLET_FP_REGISTER r2, 8
        LOAD_FUNCLET_FP_REGISTER r2, 9
        LOAD_FUNCLET_FP_REGISTER r2, 10
        LOAD_FUNCLET_FP_REGISTER r2, 11
        LOAD_FUNCLET_FP_REGISTER r2, 12
        LOAD_FUNCLET_FP_REGISTER r2, 13
        LOAD_FUNCLET_FP_REGISTER r2, 14
        LOAD_FUNCLET_FP_REGISTER r2, 15

        blx     r1

GLOBAL_LABEL CallEHFunclet2

#ifdef FEATURE_NATIVEAOT
        ldr     r2, [sp, #locRegs]
        STORE_FUNCLET_REGISTER r2, 4, r12
        STORE_FUNCLET_REGISTER r2, 5, r12
        STORE_FUNCLET_REGISTER r2, 6, r12
        STORE_FUNCLET_REGISTER r2, 7, r12
        STORE_FUNCLET_REGISTER r2, 8, r12
        STORE_FUNCLET_REGISTER r2, 9, r12
        STORE_FUNCLET_REGISTER r2, 10, r12
        STORE_FUNCLET_REGISTER r2, 11, r12

        STORE_FUNCLET_FP_REGISTER r2, 8
        STORE_FUNCLET_FP_REGISTER r2, 9
        STORE_FUNCLET_FP_REGISTER r2, 10
        STORE_FUNCLET_FP_REGISTER r2, 11
        STORE_FUNCLET_FP_REGISTER r2, 12
        STORE_FUNCLET_FP_REGISTER r2, 13
        STORE_FUNCLET_FP_REGISTER r2, 14
        STORE_FUNCLET_FP_REGISTER r2, 15

        // Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        ldr     r3, [sp, #locThread]
LOCAL_LABEL(CallEHFunclet_SetRetry):
        ldrex   r1, [r3, #OFFSETOF__Thread__m_ThreadStateFlags]
        orr     r1, r1, #TSF_DoNotTriggerGc
        strex   r2, r1, [r3, #OFFSETOF__Thread__m_ThreadStateFlags]
        cmp     r2, #0
        bne     LOCAL_LABEL(CallEHFunclet_SetRetry)
#endif

        FUNCLET_CALL_EPILOGUE

#undef locThread
#undef locRegs

NESTED_END CallEHFunclet, _TEXT

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// int CallEHFilterFunclet(Object* pThrowable, void* pRegs, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
//
// Calls a filter funclet with the frame pointer of the parent frame loaded from pRegs (the CONTEXT on CoreCLR,
// the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER.
//
// INPUT:  R0:  exception object
//         R1:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
//         R2:  filter funclet address
//         R3:  address where the SP of this helper (the funclet's caller) is saved
//
// OUTPUT: R0:  result of the filter execution
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

        FUNCLET_CALL_PROLOGUE

        str     sp, [r3]

        LOAD_FUNCLET_REGISTER r1, 11, r12

        blx     r2

GLOBAL_LABEL CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE

NESTED_END CallEHFilterFunclet, _TEXT
