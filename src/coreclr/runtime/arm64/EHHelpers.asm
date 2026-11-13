;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

    TEXTAREA

;;
;; Prologue of the funclet calling helpers. They save all callee-saved registers since the funclets do not
;; preserve them. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the resulting frame layout.
;;
;; The helpers set fp to the frame pointer of the parent frame before calling the funclet, so the frame is
;; SP based rather than using PROLOG_SAVE_REG_PAIR fp, lr, which would set fp up as the frame pointer.
;;
#define FUNCLET_CALL_FRAME_SIZE 0xB0
#define FUNCLET_CALL_LOCALS     0xA0

    MACRO
        FUNCLET_CALL_PROLOGUE

        PROLOG_SAVE_REG_PAIR_NO_FP fp, lr, #-FUNCLET_CALL_FRAME_SIZE!
        PROLOG_SAVE_REG_PAIR    x19, x20, #0x10
        PROLOG_SAVE_REG_PAIR    x21, x22, #0x20
        PROLOG_SAVE_REG_PAIR    x23, x24, #0x30
        PROLOG_SAVE_REG_PAIR    x25, x26, #0x40
        PROLOG_SAVE_REG_PAIR    x27, x28, #0x50
        PROLOG_SAVE_REG_PAIR    d8, d9, #0x60
        PROLOG_SAVE_REG_PAIR    d10, d11, #0x70
        PROLOG_SAVE_REG_PAIR    d12, d13, #0x80
        PROLOG_SAVE_REG_PAIR    d14, d15, #0x90
    MEND

;;
;; Epilogue of the funclet calling helpers
;;
    MACRO
        FUNCLET_CALL_EPILOGUE

        EPILOG_RESTORE_REG_PAIR d14, d15, #0x90
        EPILOG_RESTORE_REG_PAIR d12, d13, #0x80
        EPILOG_RESTORE_REG_PAIR d10, d11, #0x70
        EPILOG_RESTORE_REG_PAIR d8, d9, #0x60
        EPILOG_RESTORE_REG_PAIR x27, x28, #0x50
        EPILOG_RESTORE_REG_PAIR x25, x26, #0x40
        EPILOG_RESTORE_REG_PAIR x23, x24, #0x30
        EPILOG_RESTORE_REG_PAIR x21, x22, #0x20
        EPILOG_RESTORE_REG_PAIR x19, x20, #0x10
        EPILOG_RESTORE_REG_PAIR fp, lr, #FUNCLET_CALL_FRAME_SIZE!
    MEND

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; UINT_PTR CallEHFunclet(Object* pThrowable, PCODE pFunclet, void* pRegs, UINT_PTR* pFuncletCallerSP)
;;
;; Calls a catch or finally funclet with the preserved registers of the parent frame loaded from pRegs
;; (the CONTEXT on CoreCLR, the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER
;; and LOAD_FUNCLET_FP_REGISTER.
;;
;; On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved registers are written back
;; through the REGDISPLAY after it returns since a GC during the funclet may have updated them.
;;
;; INPUT:  X0:  exception object (null for finally funclets)
;;         X1:  funclet address
;;         X2:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
;;         X3:  address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: X0:  resume address for catch funclets
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY CallEHFunclet

        FUNCLET_CALL_PROLOGUE

#define locThread   (FUNCLET_CALL_LOCALS)
#define locRegs     (FUNCLET_CALL_LOCALS + 8)

        mov     x4, sp
        str     x4, [x3]

#ifdef FEATURE_NATIVEAOT
        str     x2, [sp, #locRegs]

        INLINE_GETTHREAD x4, x5
        str     x4, [sp, #locThread]

        ;; Clear the DoNotTriggerGc state before calling out to the managed funclet.
        add     x12, x4, #OFFSETOF__Thread__m_ThreadStateFlags
CallEHFunclet_ClearRetry
        ldxr    w4, [x12]
        bic     w4, w4, #TSF_DoNotTriggerGc
        stxr    w5, w4, [x12]
        cbnz    w5, CallEHFunclet_ClearRetry
#endif

        LOAD_FUNCLET_REGISTER x2, 19, x12
        LOAD_FUNCLET_REGISTER x2, 20, x12
        LOAD_FUNCLET_REGISTER x2, 21, x12
        LOAD_FUNCLET_REGISTER x2, 22, x12
        LOAD_FUNCLET_REGISTER x2, 23, x12
        LOAD_FUNCLET_REGISTER x2, 24, x12
        LOAD_FUNCLET_REGISTER x2, 25, x12
        LOAD_FUNCLET_REGISTER x2, 26, x12
        LOAD_FUNCLET_REGISTER x2, 27, x12
        LOAD_FUNCLET_REGISTER x2, 28, x12
        LOAD_FUNCLET_REGISTER x2, 29, x12

        LOAD_FUNCLET_FP_REGISTER x2, 8
        LOAD_FUNCLET_FP_REGISTER x2, 9
        LOAD_FUNCLET_FP_REGISTER x2, 10
        LOAD_FUNCLET_FP_REGISTER x2, 11
        LOAD_FUNCLET_FP_REGISTER x2, 12
        LOAD_FUNCLET_FP_REGISTER x2, 13
        LOAD_FUNCLET_FP_REGISTER x2, 14
        LOAD_FUNCLET_FP_REGISTER x2, 15

        blr     x1

    ALTERNATE_ENTRY CallEHFunclet2

#ifdef FEATURE_NATIVEAOT
        ldr     x2, [sp, #locRegs]
        STORE_FUNCLET_REGISTER x2, 19, x12
        STORE_FUNCLET_REGISTER x2, 20, x12
        STORE_FUNCLET_REGISTER x2, 21, x12
        STORE_FUNCLET_REGISTER x2, 22, x12
        STORE_FUNCLET_REGISTER x2, 23, x12
        STORE_FUNCLET_REGISTER x2, 24, x12
        STORE_FUNCLET_REGISTER x2, 25, x12
        STORE_FUNCLET_REGISTER x2, 26, x12
        STORE_FUNCLET_REGISTER x2, 27, x12
        STORE_FUNCLET_REGISTER x2, 28, x12
        STORE_FUNCLET_REGISTER x2, 29, x12

        STORE_FUNCLET_FP_REGISTER x2, 8
        STORE_FUNCLET_FP_REGISTER x2, 9
        STORE_FUNCLET_FP_REGISTER x2, 10
        STORE_FUNCLET_FP_REGISTER x2, 11
        STORE_FUNCLET_FP_REGISTER x2, 12
        STORE_FUNCLET_FP_REGISTER x2, 13
        STORE_FUNCLET_FP_REGISTER x2, 14
        STORE_FUNCLET_FP_REGISTER x2, 15

        ;; Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        ldr     x12, [sp, #locThread]
        add     x12, x12, #OFFSETOF__Thread__m_ThreadStateFlags
CallEHFunclet_SetRetry
        ldxr    w4, [x12]
        orr     w4, w4, #TSF_DoNotTriggerGc
        stxr    w5, w4, [x12]
        cbnz    w5, CallEHFunclet_SetRetry
#else
        nop
#endif

        FUNCLET_CALL_EPILOGUE
        EPILOG_RETURN

#undef locThread
#undef locRegs

    NESTED_END CallEHFunclet

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; int CallEHFilterFunclet(Object* pThrowable, void* pRegs, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
;;
;; Calls a filter funclet with the frame pointer of the parent frame loaded from pRegs (the CONTEXT on CoreCLR,
;; the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER.
;;
;; INPUT:  X0:  exception object
;;         X1:  CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
;;         X2:  filter funclet address
;;         X3:  address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: X0:  result of the filter execution
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY CallEHFilterFunclet

        FUNCLET_CALL_PROLOGUE

        mov     x4, sp
        str     x4, [x3]

        LOAD_FUNCLET_REGISTER x1, 29, x12

        blr     x2

    ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        EPILOG_RETURN

    NESTED_END CallEHFilterFunclet

    END
