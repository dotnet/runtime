;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;; -----------------------------------------------------------------------------------------------------------
;; #include "asmmacros.inc"
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
POINTER_SIZE                        equ 08h

;;
;; void CallingConventionConverter_ReturnVoidReturnThunk()
;;
LEAF_ENTRY CallingConventionConverter_ReturnVoidReturnThunk, _TEXT
        ret
LEAF_END CallingConventionConverter_ReturnVoidReturnThunk, _TEXT

;;
;; int CallingConventionConverter_ReturnIntegerReturnThunk(int)
;;
LEAF_ENTRY CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT
        mov rax, rcx
        ret
LEAF_END CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT

;;
;; Note: The "__jmpstub__" prefix is used to indicate to debugger
;; that it must step-through this stub when it encounters it while
;; stepping.
;;

;; __jmpstub__CallingConventionConverter_CommonCallingStub
;;
;;
;; struct CallingConventionConverter_CommonCallingStub_PointerData
;; {
;;     void *ManagedCallConverterThunk;
;;     void *UniversalThunk;
;; }
;;
;; struct CommonCallingStubInputData
;; {
;;     ULONG_PTR CallingConventionId;
;;     CallingConventionConverter_CommonCallingStub_PointerData *commonData;
;; }
;;
;; r10 - Points at CommonCallingStubInputData
;;
;;
LEAF_ENTRY __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT
        mov     r11, [r10]                ; put CallingConventionId into r11 as "parameter" to universal transition thunk
        mov     r10, [r10 + POINTER_SIZE] ; get pointer to CallingConventionConverter_CommonCallingStub_PointerData into r10
        mov     rax, [r10 + POINTER_SIZE] ; get address of UniversalTransitionThunk
        mov     r10, [r10]                ; get address of ManagedCallConverterThunk
        TAILJMP_RAX
LEAF_END __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT

;;
;; void CallingConventionConverter_GetStubs(IntPtr *returnVoidStub, IntPtr *returnIntegerStub, IntPtr *commonStub)
;;
LEAF_ENTRY CallingConventionConverter_GetStubs, _TEXT
        lea     rax, [CallingConventionConverter_ReturnVoidReturnThunk]
        mov    [rcx], rax
        lea     rax, [CallingConventionConverter_ReturnIntegerReturnThunk]
        mov    [rdx], rax
        lea     rax, [__jmpstub__CallingConventionConverter_CommonCallingStub]
        mov    [r8], rax
        ret
LEAF_END CallingConventionConverter_GetStubs, _TEXT

end
