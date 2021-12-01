; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Define macros to build unwind data for prologues.
;

__tls_array     equ 2Ch     ;; offsetof(TEB, ThreadLocalStoragePointer)

gCurrentThreadInfo  TEXTEQU <?gCurrentThreadInfo@@3UThreadLocalInfo@@A>

INLINE_GETTHREAD macro destReg, trashReg
    ASSUME fs : NOTHING

    EXTERN __tls_index:DWORD
    EXTERN gCurrentThreadInfo:DWORD

    mov         destReg, [__tls_index]
    mov         trashReg, fs:[__tls_array]
    mov         trashReg, [trashReg + destReg * 4]
    add         trashReg, SECTIONREL gCurrentThreadInfo
    mov         destReg, [trashReg]
endm
