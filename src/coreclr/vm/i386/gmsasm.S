// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// int __fastcall LazyMachStateCaptureState(struct LazyMachState *pState);
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
    // marks that this is not yet valid
    mov dword ptr [ecx+MachState__pRetAddr], 0

    // remember register values
    mov [ecx+MachState__edi], edi
    mov [ecx+MachState__esi], esi
    mov [ecx+MachState__ebx], ebx
    mov [ecx+LazyMachState_captureEbp], ebp
    mov [ecx+LazyMachState_captureEsp], esp

    // capture return address
    mov eax, [esp]
    mov dword ptr [ecx+LazyMachState_captureEip], eax

    // return 0
    xor eax, eax
    ret
LEAF_END LazyMachStateCaptureState, _TEXT
