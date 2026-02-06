; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Define macros to build unwind data for prologues.
;

FASTCALL_FUNC macro FuncName,cbArgs
    FuncNameReal EQU @&FuncName&@&cbArgs
    FuncNameReal proc public
    FuncName label proc
    PUBLIC FuncName

endm

FASTCALL_ENDFUNC macro
    FuncNameReal endp
endm

ALTERNATE_ENTRY macro Name

Name label proc
PUBLIC Name
        endm

__tls_array     equ 2Ch     ;; offsetof(TEB, ThreadLocalStoragePointer)

t_CurrentThreadInfo  TEXTEQU <?t_CurrentThreadInfo@@3UThreadLocalInfo@@A>

INLINE_GETTHREAD macro destReg, trashReg
    ASSUME fs : NOTHING

    EXTERN __tls_index : DWORD
    EXTERN t_CurrentThreadInfo : DWORD

    mov         destReg, [__tls_index]
    mov         trashReg, fs:[__tls_array]
    mov         trashReg, [trashReg + destReg * 4]
    add         trashReg, SECTIONREL t_CurrentThreadInfo
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

FASTCALL_FUNC macro FuncName,cbArgs
    FuncNameReal EQU @&FuncName&@&cbArgs
    FuncNameReal proc public
endm

FASTCALL_ENDFUNC macro
    FuncNameReal endp
endm

PATCH_LABEL macro labelName
    labelName:
    PUBLIC labelName
endm

t_runtime_thread_locals  TEXTEQU <?t_runtime_thread_locals@@3URuntimeThreadLocals@@A>

; Inlined version of GetThreadEEAllocContext. Trashes trashReg
INLINE_GET_ALLOC_CONTEXT_BASE macro destReg, trashReg
    ASSUME fs : NOTHING

    EXTERN __tls_index : DWORD
    EXTERN t_runtime_thread_locals : DWORD

    mov     destReg, [__tls_index]
    mov     trashReg, fs:[__tls_array]
    mov     destReg, [trashReg + destReg * 4]
    add     destReg, SECTIONREL t_runtime_thread_locals
endm

OFFSETOF__ee_alloc_context  EQU OFFSETOF__RuntimeThreadLocals__ee_alloc_context

PUSH_COOP_PINVOKE_FRAME macro target
    ; push ebp-frame
    push        ebp
    mov         ebp, esp

    ; save CalleeSavedRegisters
    push        ebx
    push        esi
    push        edi

    ; reserve space for ArgumentRegisters
    sub         esp, 8

    mov         target, esp
endm

POP_COOP_PINVOKE_FRAME macro
    ; skip over ArgumentRegisters space
    add         esp, 8

    ; pop CalleeSavedRegisters
    pop         edi
    pop         esi
    pop         ebx
    pop         ebp
endm

;; GC type flags
GC_ALLOC_FINALIZE               equ 1
GC_ALLOC_ALIGN8_BIAS            equ 4
GC_ALLOC_ALIGN8                 equ 8
