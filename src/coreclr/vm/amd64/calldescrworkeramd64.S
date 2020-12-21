// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#define real4 dword
#define real8 qword

//extern CallDescrWorkerUnwindFrameChainHandler:proc

//
//      EXTERN_C void FastCallFinalizeWorker(Object *obj, PCODE funcPtr);
//
NESTED_ENTRY FastCallFinalizeWorker, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov     rbp, rsp
        END_PROLOGUE

        //
        // RDI: already contains obj*
        // RSI: address of finalizer method to call
        //

        // !!!!!!!!!
        // NOTE:  you cannot tail call here because we must have the CallDescrWorkerUnwindFrameChainHandler
        //        personality routine on the stack.
        // !!!!!!!!!
        call    rsi
        xor     rax, rax

        // epilog
        pop_nonvol_reg rbp
        ret


NESTED_END FastCallFinalizeWorker, _TEXT

//extern "C" void CallDescrWorkerInternal(CallDescrData * pCallDescrData);

NESTED_ENTRY CallDescrWorkerInternal, _TEXT, NoHandler
        push_nonvol_reg rbp
        mov     rbp, rsp
        push_nonvol_reg rbx
        alloc_stack 8                   // ensure proper alignment of the rsp
        set_cfa_register rbp, (2*8)
        END_PROLOGUE

        mov     rbx, rdi                // save pCallDescrData in rbx

        mov     ecx, dword ptr [rbx + CallDescrData__numStackSlots]

        and     ecx, ecx
        jz      LOCAL_LABEL(NoStackArguments)

        test    ecx, 1
        jz      LOCAL_LABEL(StackAligned)
        push    rax
LOCAL_LABEL(StackAligned):

        mov     rsi, [rbx + CallDescrData__pSrc] // set source argument list address
        lea     rsi, [rsi + 8 * rcx]

LOCAL_LABEL(StackCopyLoop):                          // copy the arguments to stack top-down to carefully probe for sufficient stack space
        sub     rsi, 8
        push    qword ptr [rsi]
        dec     ecx
        jnz     LOCAL_LABEL(StackCopyLoop)
LOCAL_LABEL(NoStackArguments):
        // All argument registers are loaded regardless of the actual number
        // of arguments.

        mov     rax, [rbx + CallDescrData__pArgumentRegisters]
        mov     rdi, [rax + 0]
        mov     rsi, [rax + 8]
        mov     rdx, [rax + 16]
        mov     rcx, [rax + 24]
        mov     r8, [rax + 32]
        mov     r9, [rax + 40]

        // All float argument registers are loaded regardless of the actual number
        // of arguments.

        mov     rax, [rbx + CallDescrData__pFloatArgumentRegisters]
        and     rax, rax
        jz      LOCAL_LABEL(NoFloatArguments)
        movsd   xmm0, [rax + 0]
        movsd   xmm1, [rax + 16]
        movsd   xmm2, [rax + 32]
        movsd   xmm3, [rax + 48]
        movsd   xmm4, [rax + 64]
        movsd   xmm5, [rax + 80]
        movsd   xmm6, [rax + 96]
        movsd   xmm7, [rax + 112]
LOCAL_LABEL(NoFloatArguments):
        call    qword ptr [rbx + CallDescrData__pTarget]     // call target function

        // Save FP return value

        mov     ecx, dword ptr [rbx + CallDescrData__fpReturnSize]
        test    ecx, ecx
        jz      LOCAL_LABEL(ReturnsInt)

        cmp     ecx, 4
        je      LOCAL_LABEL(ReturnsFloat)
        cmp     ecx, 8
        je      LOCAL_LABEL(ReturnsDouble)

#if defined(UNIX_AMD64_ABI)
        // Struct with two integer eightbytes
        cmp     ecx, 16
        jne     LOCAL_LABEL(NotTwoIntegerEightbytes)
        mov     qword ptr [rbx+CallDescrData__returnValue], rax
        mov     qword ptr [rbx+CallDescrData__returnValue + 8], rdx
        jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(NotTwoIntegerEightbytes):
        // Struct with the first eightbyte SSE and the second one integer
        cmp     ecx, 16 + 1
        jne     LOCAL_LABEL(NotFirstSSESecondIntegerEightbyte)
        movsd   real8 ptr [rbx+CallDescrData__returnValue], xmm0
        mov     qword ptr [rbx+CallDescrData__returnValue + 8], rax
        jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(NotFirstSSESecondIntegerEightbyte):
        // Struct with the first eightbyte integer and the second one SSE
        cmp     ecx, 16 + 2
        jne     LOCAL_LABEL(NotFirstIntegerSecondSSEEightbyte)
        mov     qword ptr [rbx+CallDescrData__returnValue], rax
        movsd   real8 ptr [rbx+CallDescrData__returnValue + 8], xmm0
        jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(NotFirstIntegerSecondSSEEightbyte):
        // Struct with two SSE eightbytes
        cmp     ecx, 16 + 3
        jne     LOCAL_LABEL(Epilog) // unexpected
        movsd   real8 ptr [rbx+CallDescrData__returnValue], xmm0
        movsd   real8 ptr [rbx+CallDescrData__returnValue + 8], xmm1
#endif // UNIX_AMD64_ABI

        jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(ReturnsInt):
        mov     qword ptr [rbx+CallDescrData__returnValue], rax

LOCAL_LABEL(Epilog):
        lea     rsp, [rbp - 8]          // deallocate arguments
        set_cfa_register rsp, (3*8)
        pop_nonvol_reg rbx
        pop_nonvol_reg rbp
        ret

LOCAL_LABEL(ReturnsFloat):
        movss   real4 ptr [rbx+CallDescrData__returnValue], xmm0
        jmp     LOCAL_LABEL(Epilog)

LOCAL_LABEL(ReturnsDouble):
        movsd   real8 ptr [rbx+CallDescrData__returnValue], xmm0
        jmp     LOCAL_LABEL(Epilog)

NESTED_END CallDescrWorkerInternal, _TEXT


