;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

.586
.model  flat
option  casemap:none
.code

;; -----------------------------------------------------------------------------------------------------------
;; standard macros
;; -----------------------------------------------------------------------------------------------------------
LEAF_ENTRY macro Name, Section
    Section segment para 'CODE'
    public  Name
    Name    proc
endm

LEAF_END macro Name, Section
    Name    endp
    Section ends
endm


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;
;; struct ReturnBlock
;; {
;;   8 bytes of space
;;   Used to hold return information.
;;   eax, and 32bit float returns use the first 4 bytes,
;;   eax,edx and 64bit float returns use the full 8 bytes
;; };
;;

ReturnInformation__ReturnData EQU 4h

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Interop Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; ? CallingConventionConverter_ReturnVoidReturnThunk(int cbBytesOfStackToPop)
;;
LEAF_ENTRY CallingConventionConverter_ReturnVoidReturnThunk, _TEXT
        pop edx     ; pop return address into edx
        add esp,ecx ; remove ecx bytes from the call stack
        push edx    ; put the return address back on the stack
        ret         ; return to it (use a push/ret pair here so that the return stack buffer still works)
LEAF_END CallingConventionConverter_ReturnVoidReturnThunk, _TEXT

;;
;; int CallingConventionConverter_ReturnIntegerReturnThunk(int cbBytesOfStackToPop, ReturnBlock*)
;;
LEAF_ENTRY CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT
        pop eax           ; pop return address into edx
        add esp,ecx       ; remove ecx bytes from the call stack
        push eax          ; put the return address back on the stack
        mov eax, [edx]    ; setup eax and edx to hold the return value
        mov edx, [edx + 4]
        ret               ; return  (use a push/ret pair here so that the return stack buffer still works)
LEAF_END CallingConventionConverter_ReturnIntegerReturnThunk, _TEXT

;;
;; float CallingConventionConverter_Return4ByteFloatReturnThunk(int cbBytesOfStackToPop, ReturnBlock*)
;;
LEAF_ENTRY CallingConventionConverter_Return4ByteFloatReturnThunk, _TEXT
        pop eax            ; pop return address into edx
        add esp,ecx        ; remove ecx bytes from the call stack
        push eax           ; put the return address back on the stack
        fld dword ptr [edx]; fill in the return value
        ret                ; return (use a push/ret pair here so that the return stack buffer still works)
LEAF_END CallingConventionConverter_Return4ByteFloatReturnThunk, _TEXT

;;
;; double CallingConventionConverter_Return4ByteFloatReturnThunk(int cbBytesOfStackToPop, ReturnBlock*)
;;
LEAF_ENTRY CallingConventionConverter_Return8ByteFloatReturnThunk, _TEXT
        pop eax            ; pop return address into edx
        add esp,ecx        ; remove ecx bytes from the call stack
        push eax           ; put the return address back on the stack
        fld qword ptr [edx]; fill in the return value
        ret                ; return (use a push/ret pair here so that the return stack buffer still works)
LEAF_END CallingConventionConverter_Return8ByteFloatReturnThunk, _TEXT

;;
;; Note: The "__jmpstub__" prefix is used to indicate to debugger
;; that it must step-through this stub when it encounters it while
;; stepping.
;;

;;
;; __jmpstub__CallingConventionConverter_CommonCallingStub(?)
;;
LEAF_ENTRY __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT
        ;; rax <- stub info
        push        ebp
        mov         ebp, esp
        push        [eax]   ; First argument
        mov         eax,[eax+4] ;
        push        [eax]   ; Pointer to CallingConventionConverter Managed thunk
        mov         eax,[eax+4] ; Pointer to UniversalTransitionThunk
        jmp         eax
LEAF_END __jmpstub__CallingConventionConverter_CommonCallingStub, _TEXT

    ;;
    ;; void CallingConventionConverter_GetStubs(IntPtr *returnVoidStub, IntPtr *returnIntegerStub, IntPtr* commonCallingStub, IntPtr *return4ByteFloat, IntPtr *return8ByteFloat)
    ;;
LEAF_ENTRY CallingConventionConverter_GetStubs, _TEXT
        lea     eax, [CallingConventionConverter_ReturnVoidReturnThunk]
        mov     ecx, [esp+04h]
        mov     [ecx], eax
        lea     eax, [CallingConventionConverter_ReturnIntegerReturnThunk]
        mov     ecx, [esp+08h]
        mov     [ecx], eax
        lea     eax, [__jmpstub__CallingConventionConverter_CommonCallingStub]
        mov     ecx, [esp+0Ch]
        mov     [ecx], eax
        lea     eax, [CallingConventionConverter_Return4ByteFloatReturnThunk]
        mov     ecx, [esp+10h]
        mov     [ecx], eax
        lea     eax, [CallingConventionConverter_Return8ByteFloatReturnThunk]
        mov     ecx, [esp+14h]
        mov     [ecx], eax
        retn 14h
LEAF_END CallingConventionConverter_GetStubs, _TEXT


end
