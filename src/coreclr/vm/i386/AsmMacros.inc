; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Define macros to build unwind data for prologues.
;

include i386\AsmMacros_Windows.inc

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

FASTCALL_FUNC macro FuncName,cbArgs
    FuncNameReal EQU @&FuncName&@&cbArgs
    FuncNameReal proc public
endm

FASTCALL_ENDFUNC macro
    FuncNameReal endp
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
