;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; DWORD_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilterIP, UINT_PTR* pFuncletCallerSP)
;;
;; INPUT:  RCX:  exception object
;;         RDX:  RBP of the parent method
;;         R8:   filter funclet address
;;         R9:   address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: RAX:  result of the filter execution
;;
;; NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the frame layout.
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY CallEHFilterFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 0, 1

        mov     [r9], rsp
        mov     rbp, rdx

        call    r8

ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        ret

NESTED_END CallEHFilterFunclet, _TEXT

        end
