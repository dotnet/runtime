;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

ifdef FEATURE_DYNAMIC_CODE
;;;;;;;;;;;;;;;;;;;;;;; CallingConventionConverter Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;------------------------------------------------------------------------------
; This helper routine enregisters the appropriate arguments and makes the
; actual call.
;------------------------------------------------------------------------------
; void __fastcall CallDescrWorker(CallDescrWorkerParams *  pParams)
FASTCALL_FUNC   RhCallDescrWorker, 4
        push    ebp
        mov     ebp, esp
        push    ebx
        mov     ebx, ecx

        mov     ecx, [ebx + OFFSETOF__CallDescrData__numStackSlots]
        mov     eax, [ebx + OFFSETOF__CallDescrData__pSrc]            ; copy the stack
        test    ecx, ecx
        jz      donestack
        lea     eax, [eax + 4 * ecx - 4]          ; last argument
        push    dword ptr [eax]
        dec     ecx
        jz      donestack
        sub     eax, 4
        push    dword ptr [eax]
        dec     ecx
        jz      donestack
stackloop:
        sub     eax, 4
        push    dword ptr [eax]
        dec     ecx
        jnz     stackloop
donestack:

        ; now we must push each field of the ArgumentRegister structure
        mov     eax, [ebx + OFFSETOF__CallDescrData__pArgumentRegisters]
        mov     edx, dword ptr [eax]
        mov     ecx, dword ptr [eax + 4]
        mov     eax,[ebx + OFFSETOF__CallDescrData__pTarget]
        call    eax

        EXPORT_POINTER_TO_ADDRESS _PointerToReturnFromCallDescrThunk

        ; Symbol used to identify thunk call to managed function so the special
        ; case unwinder can unwind through this function. Sadly we cannot directly
        ; export this symbol right now because it confuses DIA unwinder to believe
        ; it's the beginning of a new method, therefore we export the address
        ; by means of an auxiliary variable.

        ; Save FP return value if necessary
        mov     ecx, [ebx + OFFSETOF__CallDescrData__fpReturnSize]
        cmp     ecx, 0
        je      ReturnsInt

        cmp     ecx, 4
        je      ReturnsFloat
        cmp     ecx, 8
        je      ReturnsDouble
        ; unexpected
        jmp     Epilog

ReturnsInt:
; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        mov     ebx, [ebx + OFFSETOF__CallDescrData__pReturnBuffer]
        mov     [ebx], eax
        mov     [ebx + 4], edx

Epilog:
        pop     ebx
        pop     ebp
        retn

ReturnsFloat:
        mov     ebx, [ebx + OFFSETOF__CallDescrData__pReturnBuffer]
        fstp    dword ptr [ebx]    ; Spill the Float return value
        jmp     Epilog

ReturnsDouble:
        mov     ebx, [ebx + OFFSETOF__CallDescrData__pReturnBuffer]
        fstp    qword ptr [ebx]    ; Spill the Double return value
        jmp     Epilog

FASTCALL_ENDFUNC

endif

end
