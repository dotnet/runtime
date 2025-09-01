; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

    .586
    .model  flat

include asmconstants.inc

    assume fs: nothing
    option  casemap:none
    .code

ifdef FEATURE_EH_FUNCLETS

; DWORD_PTR STDCALL CallEHFunclet(Object *pThrowable, UINT_PTR pFuncletToInvoke, CONTEXT *pContext, UINT_PTR *pFuncletCallerSP);
; ESP based frame
_CallEHFunclet@16 proc public

    push ebp
    push ebx
    push esi
    push edi

    lea     ebp, [esp + 3*4]

    ; On entry:
    ;
    ; [ebp+ 8] = throwable
    ; [ebp+12] = PC to invoke
    ; [ebp+16] = address of CONTEXT record; used to restore the non-volatile registers of CrawlFrame
    ; [ebp+20] = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    ;

    ; Save the SP of this function
    mov     eax, [ebp + 20]
    mov     [eax], esp
    ; Save the funclet PC for later call
    mov     edx, [ebp + 12]
    ; Pass throwable object to funclet
    mov     eax, [ebp +  8]
    ; Restore non-volatiles registers
    mov     ecx, [ebp + 16]
    mov     edi, [ecx + CONTEXT_Edi]
    mov     esi, [ecx + CONTEXT_Esi]
    mov     ebx, [ecx + CONTEXT_Ebx]
    mov     ebp, [ecx + CONTEXT_Ebp]
    ; Invoke the funclet
    call    edx

    pop edi
    pop esi
    pop ebx
    pop ebp

    ret     16

_CallEHFunclet@16 endp

; DWORD_PTR STDCALL CallEHFilterFunclet(Object *pThrowable, TADDR CallerSP, UINT_PTR pFuncletToInvoke, UINT_PTR *pFuncletCallerSP);
; ESP based frame
_CallEHFilterFunclet@16 proc public

    push ebp
    push ebx
    push esi
    push edi

    lea     ebp, [esp + 3*4]

    ; On entry:
    ;
    ; [ebp+ 8] = throwable
    ; [ebp+12] = FP to restore
    ; [ebp+16] = PC to invoke
    ; [ebp+20] = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    ;

    ; Save the SP of this function
    mov     eax, [ebp + 20]
    mov     [eax], esp
    ; Save the funclet PC for later call
    mov     edx, [ebp + 16]
    ; Pass throwable object to funclet
    mov     eax, [ebp +  8]
    ; Restore FP
    mov     ebp, [ebp + 12]
    ; Invoke the funclet
    call    edx

    pop edi
    pop esi
    pop ebx
    pop ebp

    ret     16

_CallEHFilterFunclet@16 endp

endif ; FEATURE_EH_FUNCLETS

    end
