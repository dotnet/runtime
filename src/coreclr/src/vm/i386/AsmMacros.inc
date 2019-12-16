; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

;
; Define macros to build unwind data for prologues.
;

;
; FramedMethodFrame prolog
;
STUB_PROLOG  macro
    ; push ebp-frame
    push        ebp
    mov         ebp,esp

    ; save CalleeSavedRegisters
    push        ebx
    push        esi
    push        edi

    ; push ArgumentRegisters
    push        ecx
    push        edx
endm

;
; FramedMethodFrame epilog
;
STUB_EPILOG macro
    ; pop ArgumentRegisters
    pop     edx
    pop     ecx

    ; pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp
endm

__tls_array     equ 2Ch     ;; offsetof(TEB, ThreadLocalStoragePointer)

INLINE_GETTHREAD macro destReg, trashReg
    ASSUME fs : NOTHING

    EXTERN __tls_index:DWORD
    EXTERN _gCurrentThreadInfo:DWORD

    mov         destReg, [__tls_index]
    mov         trashReg, fs:[__tls_array]
    mov         trashReg, [trashReg + destReg * 4]
    add         trashReg, SECTIONREL _gCurrentThreadInfo
    mov         destReg, [trashReg]
endm
