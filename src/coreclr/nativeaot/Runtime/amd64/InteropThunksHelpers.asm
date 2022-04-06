;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.


;; -----------------------------------------------------------------------------------------------------------
;;#include "asmmacros.inc"
;; -----------------------------------------------------------------------------------------------------------

LEAF_ENTRY macro Name, Section
    Section segment para 'CODE'
    align   16
    public  Name
    Name    proc
endm

LEAF_END macro Name, Section
    Name    endp
    Section ends
endm

;  - TAILCALL_RAX: ("jmp rax") should be used for tailcalls, this emits an instruction
;            sequence which is recognized by the unwinder as a valid epilogue terminator
TAILJMP_RAX TEXTEQU <DB 048h, 0FFh, 0E0h>


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

_tls_array                          equ 58h     ;; offsetof(TEB, ThreadLocalStoragePointer)

POINTER_SIZE                        equ 08h

;; TLS variables
_TLS    SEGMENT ALIAS(".tls$")
    ThunkParamSlot  DQ 0000000000000000H
_TLS    ENDS

EXTRN   _tls_index:DWORD


;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;
;; RhCommonStub
;;
LEAF_ENTRY RhCommonStub, _TEXT
        ;; There are arbitrary callers passing arguments with arbitrary signatures.
        ;; Custom calling convention:
        ;;      r10: pointer to the current thunk's data block (data contains 2 pointer values: context + target pointers)

        ;; Save context data into the ThunkParamSlot thread-local variable
        ;; A pointer to the delegate and function pointer for open static delegate should have been saved in the thunk's context cell during thunk allocation
        mov     [rsp + 8], rcx                                     ;; Save rcx in a home scratch location. Pushing the
                                                                   ;; register on the stack will break callstack unwind
        mov     ecx, [_tls_index]
        mov     r11, gs:[_tls_array]
        mov     rax, [r11 + rcx * POINTER_SIZE]

        ;; rax = base address of TLS data
        ;; r10 = address of context cell in thunk's data
        ;; r11 = trashed

        ;; store thunk address in thread static
        mov     r11, [r10]
        mov     ecx, SECTIONREL ThunkParamSlot
        mov     [rax + rcx], r11                 ;;   ThunkParamSlot <- context slot data

        mov     rcx, [rsp + 8]                                     ;; Restore rcx

        ;; jump to the target
        mov     rax, [r10 + POINTER_SIZE]
        TAILJMP_RAX
LEAF_END RhCommonStub, _TEXT


;;
;; IntPtr RhGetCommonStubAddress()
;;
LEAF_ENTRY RhGetCommonStubAddress, _TEXT
        lea     rax, [RhCommonStub]
        ret
LEAF_END RhGetCommonStubAddress, _TEXT


;;
;; IntPtr RhGetCurrentThunkContext()
;;
LEAF_ENTRY RhGetCurrentThunkContext, _TEXT
        mov     r10d, [_tls_index]
        mov     r11, gs:[_tls_array]
        mov     r10, [r11 + r10 * POINTER_SIZE]
        mov     r8d, SECTIONREL ThunkParamSlot
        mov     rax, [r10 + r8]                 ;;   rax <- ThunkParamSlot
        ret
LEAF_END RhGetCurrentThunkContext, _TEXT


end
