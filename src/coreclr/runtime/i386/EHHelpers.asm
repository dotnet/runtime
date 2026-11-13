;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros_Shared.inc

;;
;; Prologue of the funclet calling helpers. They save all callee-saved registers since the funclets do not
;; preserve them. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk hard-codes the resulting frame layout.
;;
;; The helpers set ebp to the frame pointer of the parent frame before calling the funclet, so the frame is
;; ESP based.
;;
FUNCLET_CALL_PROLOGUE macro
        push    ebp
        push    ebx
        push    esi
        push    edi

        esp_offsetof_args = 4 * 4
endm

;;
;; Epilogue of the funclet calling helpers
;;
FUNCLET_CALL_EPILOGUE macro
        pop     edi
        pop     esi
        pop     ebx
        pop     ebp
endm

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; UINT_PTR FASTCALL CallEHFunclet(Object* pThrowable, PCODE pFunclet, void* pRegs, UINT_PTR* pFuncletCallerSP)
;;
;; Calls a catch or finally funclet with the preserved registers of the parent frame loaded from pRegs
;; (the CONTEXT on CoreCLR, the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER.
;;
;; On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved registers are written back
;; through the REGDISPLAY after it returns since a GC during the funclet may have updated them.
;;
;; INPUT:  ECX:                     exception object (null for finally funclets)
;;         EDX:                     funclet address
;;         [ESP + FUNCLET_CALL_ARG3]: CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
;;         [ESP + FUNCLET_CALL_ARG4]: address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: EAX:                     resume address for catch funclets
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  CallEHFunclet, 16

        FUNCLET_CALL_PROLOGUE

        mov     eax, [esp + esp_offsetof_args + FUNCLET_CALL_ARG4]
        mov     [eax], esp

ifdef FEATURE_NATIVEAOT
        INLINE_GETTHREAD    eax, ebx                                ;; eax <- Thread*, ebx is trashed

        ;; Clear the DoNotTriggerGc state before calling out to the managed funclet.
        lock and            dword ptr [eax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc
endif

        mov     eax, ecx                                            ;; eax <- exception object for the funclet
        mov     ecx, [esp + esp_offsetof_args + FUNCLET_CALL_ARG3]

        LOAD_FUNCLET_REGISTER ecx, Ebx
        LOAD_FUNCLET_REGISTER ecx, Esi
        LOAD_FUNCLET_REGISTER ecx, Edi
        LOAD_FUNCLET_REGISTER ecx, Ebp

        call    edx

ALTERNATE_ENTRY _CallEHFunclet2

ifdef FEATURE_NATIVEAOT
        mov     ecx, [esp + esp_offsetof_args + FUNCLET_CALL_ARG3]
        STORE_FUNCLET_REGISTER ecx, Ebx, edx
        STORE_FUNCLET_REGISTER ecx, Esi, edx
        STORE_FUNCLET_REGISTER ecx, Edi, edx
        STORE_FUNCLET_REGISTER ecx, Ebp, edx

        ;; Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        INLINE_GETTHREAD    ecx, edx                                ;; ecx <- Thread*, edx is trashed
        lock or             dword ptr [ecx + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc
endif

        FUNCLET_CALL_EPILOGUE
        ret     8

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; int FASTCALL CallEHFilterFunclet(Object* pThrowable, void* pRegs, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
;;
;; Calls a filter funclet with the frame pointer of the parent frame loaded from pRegs (the CONTEXT on CoreCLR,
;; the REGDISPLAY on NativeAOT) by LOAD_FUNCLET_REGISTER.
;;
;; INPUT:  ECX:                     exception object
;;         EDX:                     CONTEXT* on CoreCLR, REGDISPLAY* on NativeAOT
;;         [ESP + FUNCLET_CALL_ARG3]: filter funclet address
;;         [ESP + FUNCLET_CALL_ARG4]: address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: EAX:                     result of the filter execution
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  CallEHFilterFunclet, 16

        FUNCLET_CALL_PROLOGUE

        mov     eax, [esp + esp_offsetof_args + FUNCLET_CALL_ARG4]
        mov     [eax], esp

        mov     eax, ecx                                            ;; eax <- exception object for the filter
        mov     ecx, [esp + esp_offsetof_args + FUNCLET_CALL_ARG3]
        LOAD_FUNCLET_REGISTER edx, Ebp

        call    ecx

ALTERNATE_ENTRY _CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        ret     8

FASTCALL_ENDFUNC

        end
