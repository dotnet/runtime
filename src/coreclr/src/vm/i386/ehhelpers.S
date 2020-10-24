// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// DWORD_PTR STDCALL CallEHFunclet(Object *pThrowable, UINT_PTR pFuncletToInvoke, UINT_PTR *pFirstNonVolReg, UINT_PTR *pFuncletCallerSP);
// ESP based frame
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

    ESP_PROLOG_BEG
    ESP_PROLOG_PUSH ebp
    ESP_PROLOG_PUSH ebx
    ESP_PROLOG_PUSH esi
    ESP_PROLOG_PUSH edi
    ESP_PROLOG_ALLOC 12
    ESP_PROLOG_END

    lea     ebp, [esp + 3*4 + 12]

    // On entry:
    //
    // [ebp+ 8] = throwable
    // [ebp+12] = PC to invoke
    // [ebp+16] = address of EDI register in CONTEXT record // used to restore the non-volatile registers of CrawlFrame
    // [ebp+20] = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    // Save the SP of this function
    mov     eax, [ebp + 20]
    mov     [eax], esp
    // Save the funclet PC for later call
    mov     edx, [ebp + 12]
    // Pass throwable object to funclet
    mov     eax, [ebp +  8]
    // Restore non-volatiles registers
    mov     ecx, [ebp + 16]
    mov     edi, [ecx]
    mov     esi, [ecx +  4]
    mov     ebx, [ecx +  8]
    mov     ebp, [ecx + 24]
    // Invoke the funclet
    call    edx

    ESP_EPILOG_BEG
    ESP_EPILOG_FREE 12
    ESP_EPILOG_POP edi
    ESP_EPILOG_POP esi
    ESP_EPILOG_POP ebx
    ESP_EPILOG_POP ebp
    ESP_EPILOG_END

    ret     16

NESTED_END CallEHFunclet, _TEXT

// DWORD_PTR STDCALL CallEHFilterFunclet(Object *pThrowable, TADDR CallerSP, UINT_PTR pFuncletToInvoke, UINT_PTR *pFuncletCallerSP);
// ESP based frame
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

    ESP_PROLOG_BEG
    ESP_PROLOG_PUSH ebp
    ESP_PROLOG_PUSH ebx
    ESP_PROLOG_PUSH esi
    ESP_PROLOG_PUSH edi
    ESP_PROLOG_ALLOC 12
    ESP_PROLOG_END

    lea     ebp, [esp + 3*4 + 12]

    // On entry:
    //
    // [ebp+ 8] = throwable
    // [ebp+12] = FP to restore
    // [ebp+16] = PC to invoke
    // [ebp+20] = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    // Save the SP of this function
    mov     eax, [ebp + 20]
    mov     [eax], esp
    // Save the funclet PC for later call
    mov     edx, [ebp + 16]
    // Pass throwable object to funclet
    mov     eax, [ebp +  8]
    // Restore FP
    mov     ebp, [ebp + 12]
    // Invoke the funclet
    call    edx

    ESP_EPILOG_BEG
    ESP_EPILOG_FREE 12
    ESP_EPILOG_POP edi
    ESP_EPILOG_POP esi
    ESP_EPILOG_POP ebx
    ESP_EPILOG_POP ebp
    ESP_EPILOG_END

    ret     16

NESTED_END CallEHFilterFunclet, _TEXT
