// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// Implementation of CONTEXT_CaptureContext for the Intel x86 platform.
//
//  extern void CONTEXT_CaptureContext(LPCONTEXT lpContext);
//
// This function is processor-dependent. It is used by exception handling,
// and is always apply to the current thread.
//
LEAF_ENTRY CONTEXT_CaptureContext, _TEXT
    // Store
    push  eax
    push  ebx

    // The stack will contain the following elements on the top of
    // the caller's stack
    //  [ebx]             / esp + 00
    //  [eax]             / esp + 04
    //  [ret]             / esp + 08
    //  [arg0: lpContext] / esp + 12

    mov   eax, [esp + 12] // eax will point to lpContext

    // Capture INTEGER registers
    mov   ebx, [esp + 4]
    mov   [eax + CONTEXT_Eax], ebx
    mov   ebx, [esp]
    mov   [eax + CONTEXT_Ebx], ebx
    mov   [eax + CONTEXT_Ecx], ecx
    mov   [eax + CONTEXT_Edx], edx
    mov   [eax + CONTEXT_Esi], esi
    mov   [eax + CONTEXT_Edi], edi

    // Capture CONTROL registers
    mov   [eax + CONTEXT_Ebp], ebp
    lea   ebx, [esp + 12]
    mov   [eax + CONTEXT_Esp], ebx
    mov   ebx, [esp + 8]
    mov   [eax + CONTEXT_Eip], ebx

    push  cs
    xor   ebx, ebx
    pop   bx
    mov   [eax + CONTEXT_SegCs], ebx

    push  ss
    xor   ebx, ebx
    pop   bx
    mov   [eax + CONTEXT_SegSs], ebx

    pushf
    xor   ebx, ebx
    pop   bx
    mov   [eax + CONTEXT_EFlags], ebx

    test   BYTE PTR [eax + CONTEXT_ContextFlags], CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT)
    // Capture FPU status
    fnsave [eax + CONTEXT_FloatSave]
    frstor [eax + CONTEXT_FloatSave]
LOCAL_LABEL(Done_CONTEXT_FLOATING_POINT):

    test   BYTE PTR [eax + CONTEXT_ContextFlags], CONTEXT_EXTENDED_REGISTERS
    je     LOCAL_LABEL(Done_CONTEXT_EXTENDED_REGISTERS)
    movdqu [eax + CONTEXT_Xmm0], xmm0
    movdqu [eax + CONTEXT_Xmm1], xmm1
    movdqu [eax + CONTEXT_Xmm2], xmm2
    movdqu [eax + CONTEXT_Xmm3], xmm3
    movdqu [eax + CONTEXT_Xmm4], xmm4
    movdqu [eax + CONTEXT_Xmm5], xmm5
    movdqu [eax + CONTEXT_Xmm6], xmm6
    movdqu [eax + CONTEXT_Xmm7], xmm7
LOCAL_LABEL(Done_CONTEXT_EXTENDED_REGISTERS):

    // Restore
    pop   ebx
    pop   eax
    ret
LEAF_END CONTEXT_CaptureContext, _TEXT

LEAF_ENTRY RtlCaptureContext, _TEXT
    push    eax
    mov     eax, [esp + 8]
    mov     DWORD PTR [eax + CONTEXT_ContextFlags], (CONTEXT_FLOATING_POINT)
    pop     eax
    jmp     C_FUNC(CONTEXT_CaptureContext)
LEAF_END RtlCaptureContext, _TEXT

LEAF_ENTRY RtlRestoreContext, _TEXT

#ifdef HAS_ASAN
    call    EXTERNAL_C_FUNC(__asan_handle_no_return)
#endif
    mov     eax, [esp + 4]
    test    BYTE PTR [eax + CONTEXT_ContextFlags], CONTEXT_FLOATING_POINT
    je      LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT)
    frstor  [eax + CONTEXT_FloatSave]
LOCAL_LABEL(Done_Restore_CONTEXT_FLOATING_POINT):

    test   BYTE PTR [eax + CONTEXT_ContextFlags], CONTEXT_EXTENDED_REGISTERS
    je     LOCAL_LABEL(Done_Restore_CONTEXT_EXTENDED_REGISTERS)
    movdqu xmm0, [eax + CONTEXT_Xmm0]
    movdqu xmm1, [eax + CONTEXT_Xmm1]
    movdqu xmm2, [eax + CONTEXT_Xmm2]
    movdqu xmm3, [eax + CONTEXT_Xmm3]
    movdqu xmm4, [eax + CONTEXT_Xmm4]
    movdqu xmm5, [eax + CONTEXT_Xmm5]
    movdqu xmm6, [eax + CONTEXT_Xmm6]
    movdqu xmm7, [eax + CONTEXT_Xmm7]
LOCAL_LABEL(Done_Restore_CONTEXT_EXTENDED_REGISTERS):

    // Restore Stack
    mov   esp, [eax + CONTEXT_Esp]

    // Create a minimal frame
    push  DWORD PTR [eax + CONTEXT_Eip]

    // Restore register(s)
    mov   ebp, [eax + CONTEXT_Ebp]
    mov   edi, [eax + CONTEXT_Edi]
    mov   esi, [eax + CONTEXT_Esi]
    mov   edx, [eax + CONTEXT_Edx]
    mov   ecx, [eax + CONTEXT_Ecx]
    mov   ebx, [eax + CONTEXT_Ebx]
    mov   eax, [eax + CONTEXT_Eax]

    // Resume
    ret
LEAF_END RtlRestoreContext, _TEXT

