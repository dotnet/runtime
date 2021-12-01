; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include <AsmConstants.inc>

extern CallDescrWorkerUnwindFrameChainHandler:proc

;;
;;      EXTERN_C void FastCallFinalizeWorker(Object *obj, PCODE funcPtr);
;;
        NESTED_ENTRY FastCallFinalizeWorker, _TEXT, CallDescrWorkerUnwindFrameChainHandler
        alloc_stack     28h     ;; alloc callee scratch and align the stack
        END_PROLOGUE

        ;
        ; RCX: already contains obj*
        ; RDX: address of finalizer method to call
        ;

        ; !!!!!!!!!
        ; NOTE:  you cannot tail call here because we must have the CallDescrWorkerUnwindFrameChainHandler
        ;        personality routine on the stack.
        ; !!!!!!!!!
        call    rdx
        xor     rax, rax

        ; epilog
        add     rsp, 28h
        ret


        NESTED_END FastCallFinalizeWorker, _TEXT

;;extern "C" void CallDescrWorkerInternal(CallDescrData * pCallDescrData);

        NESTED_ENTRY CallDescrWorkerInternal, _TEXT, CallDescrWorkerUnwindFrameChainHandler

        push_nonvol_reg rbx             ; save nonvolatile registers
        push_nonvol_reg rsi             ;
        push_nonvol_reg rbp             ;
        set_frame rbp, 0                ; set frame pointer

        END_PROLOGUE

        mov     rbx, rcx                ; save pCallDescrData in rbx

        mov     ecx, dword ptr [rbx + CallDescrData__numStackSlots]

        test    ecx, 1
        jz      StackAligned
        push    rax
StackAligned:

        mov     rsi, [rbx + CallDescrData__pSrc] ; set source argument list address
        lea     rsi, [rsi + 8 * rcx]

StackCopyLoop:                          ; copy the arguments to stack top-down to carefully probe for sufficient stack space
        sub     rsi, 8
        push    qword ptr [rsi]
        dec     ecx
        jnz     StackCopyLoop

        ;
        ; N.B. All four argument registers are loaded regardless of the actual number
        ;      of arguments.
        ;

        mov     rax, [rbx + CallDescrData__dwRegTypeMap] ; save the reg (arg) type map

        mov     rcx, 0[rsp]             ; load first four argument registers
        movss   xmm0, real4 ptr 0[rsp]  ;
        cmp     al, ASM_ELEMENT_TYPE_R8 ;
        jnz     Arg2                    ;
        movsd   xmm0, real8 ptr 0[rsp]  ;
Arg2:
        mov     rdx, 8[rsp]             ;
        movss   xmm1, real4 ptr 8[rsp]  ;
        cmp     ah, ASM_ELEMENT_TYPE_R8 ;
        jnz     Arg3                    ;
        movsd   xmm1, real8 ptr 8[rsp]  ;
Arg3:
        mov     r8, 10h[rsp]            ;
        movss   xmm2, real4 ptr 10h[rsp];
        shr     eax, 16                 ;
        cmp     al, ASM_ELEMENT_TYPE_R8 ;
        jnz     Arg4                    ;
        movsd   xmm2, real8 ptr 10h[rsp];
Arg4:
        mov     r9, 18h[rsp]            ;
        movss   xmm3, real4 ptr 18h[rsp];
        cmp     ah, ASM_ELEMENT_TYPE_R8 ;
        jnz     DoCall                  ;
        movsd   xmm3, real8 ptr 18h[rsp];
DoCall:
        call    qword ptr [rbx+CallDescrData__pTarget]     ; call target function

        ; Save FP return value

        mov     ecx, dword ptr [rbx+CallDescrData__fpReturnSize]
        test    ecx, ecx
        jz      ReturnsInt

        cmp     ecx, 4
        je      ReturnsFloat
        cmp     ecx, 8
        je      ReturnsDouble
        ; unexpected
        jmp     Epilog

ReturnsInt:
        mov     [rbx+CallDescrData__returnValue], rax

Epilog:
        lea     rsp, 0[rbp]             ; deallocate argument list
        pop     rbp                     ; restore nonvolatile register
        pop     rsi                     ;
        pop     rbx                     ;
        ret

ReturnsFloat:
        movss   real4 ptr [rbx+CallDescrData__returnValue], xmm0
        jmp     Epilog

ReturnsDouble:
        movsd   real8 ptr [rbx+CallDescrData__returnValue], xmm0
        jmp     Epilog

        NESTED_END CallDescrWorkerInternal, _TEXT

        end
