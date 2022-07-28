;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


;;;;;;;;;;;;;;;;;;;;;;; CallingConventionConverter Thunks Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;extern "C" void RhCallDescrWorker(CallDescrData * pCallDescrData);

        NESTED_ENTRY RhCallDescrWorker, _TEXT

        push_nonvol_reg rbx             ; save nonvolatile registers
        push_nonvol_reg rsi             ;
        push_nonvol_reg rbp             ;
        set_frame rbp, 0                ; set frame pointer

        END_PROLOGUE

        mov     rbx, rcx                ; save pCallDescrData in rbx

        mov     ecx, dword ptr [rbx + OFFSETOF__CallDescrData__numStackSlots]

        test    ecx, 1
        jz      StackAligned
        push    rax
StackAligned:

        mov     rsi, [rbx + OFFSETOF__CallDescrData__pSrc] ; set source argument list address
        lea     rsi, [rsi + 8 * rcx]

StackCopyLoop:                          ; copy the arguments to stack top-down to carefully probe for sufficient
                                        ; stack space
        sub     rsi, 8
        push    qword ptr [rsi]
        dec     ecx
        jnz     StackCopyLoop

        ;
        ; N.B. All four argument registers are loaded regardless of the actual number
        ;      of arguments.
        ;

        mov     rax, [rbx + OFFSETOF__CallDescrData__pFloatArgumentRegisters] ; get floating pointer arg registers pointer

        mov     rcx, 0[rsp]             ; load first four argument registers
        mov     rdx, 8[rsp]             ;
        mov     r8, 10h[rsp]            ;
        mov     r9, 18h[rsp]            ;
        test    rax, rax                ;
        jz      DoCall                  ;
        movdqa  xmm0, [rax + 00h]       ; load floating point registers if they are used
        movdqa  xmm1, [rax + 10h]       ;
        movdqa  xmm2, [rax + 20h]       ;
        movdqa  xmm3, [rax + 30h]       ;
DoCall:
        call    qword ptr [rbx + OFFSETOF__CallDescrData__pTarget]     ; call target function

        EXPORT_POINTER_TO_ADDRESS PointerToReturnFromCallDescrThunk

        ; Symbol used to identify thunk call to managed function so the special
        ; case unwinder can unwind through this function. Sadly we cannot directly
        ; export this symbol right now because it confuses DIA unwinder to believe
        ; it's the beginning of a new method, therefore we export the address
        ; of an auxiliary variable holding the address instead.

        ; Save FP return value

        mov     ecx, dword ptr [rbx + OFFSETOF__CallDescrData__fpReturnSize]
        test    ecx, ecx
        jz      ReturnsInt

        cmp     ecx, 4
        je      ReturnsFloat
        cmp     ecx, 8
        je      ReturnsDouble
        ; unexpected
        jmp     Epilog

ReturnsInt:
        mov     rbx, [rbx + OFFSETOF__CallDescrData__pReturnBuffer]
        mov     [rbx], rax

Epilog:
        lea     rsp, 0[rbp]             ; deallocate argument list
        pop     rbp                     ; restore nonvolatile register
        pop     rsi                     ;
        pop     rbx                     ;
        ret

ReturnsFloat:
; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        mov     rbx, [rbx + OFFSETOF__CallDescrData__pReturnBuffer]
        movss   real4 ptr [rbx], xmm0
        jmp     Epilog

ReturnsDouble:
; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        mov     rbx, [rbx + OFFSETOF__CallDescrData__pReturnBuffer]
        movsd   real8 ptr [rbx], xmm0
        jmp     Epilog

        NESTED_END RhCallDescrWorker, _TEXT

end
