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

LEAF_ENTRY macro functionName
    functionName PROC PUBLIC
endm

LEAF_END macro functionName
    functionName ENDP
endm

LEAF_END_MARKED macro functionName
    LOCAL stackArgsSize, bareFunctionName, endMarkerName
    stackArgsSize TEXTEQU @SubStr(functionName, @InStr(,functionName, <@>))
    bareFunctionName TEXTEQU @SubStr(functionName, 1, @SizeStr(functionName)-@SizeStr(%stackArgsSize))
    endMarkerName TEXTEQU @CatStr(%bareFunctionName, <_End@0>)
    %endMarkerName:
    PUBLIC endMarkerName
    functionName ENDP
    ; make sure this symbol gets its own address
    nop
endm

PATCH_LABEL macro labelName
    labelName:
    PUBLIC labelName
endm
