; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

ALTERNATE_ENTRY macro Name

Name label proc
PUBLIC Name
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

__tls_array     equ 2Ch     ;; offsetof(TEB, ThreadLocalStoragePointer)

;; GC type flags
GC_ALLOC_FINALIZE               equ 1
GC_ALLOC_ALIGN8_BIAS            equ 4
GC_ALLOC_ALIGN8                 equ 8
