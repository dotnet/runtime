; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==

include <AsmMacros.inc>
include AsmConstants.inc

extern s_pStubHelperFrameVPtr:qword
extern JIT_FailFast:proc
extern s_gsCookie:qword


OFFSETOF_SECRET_PARAMS              equ 0h
OFFSETOF_GSCOOKIE                   equ OFFSETOF_SECRET_PARAMS + \
                                        18h + 8h ; +8 for stack alignment padding
OFFSETOF_FRAME                      equ OFFSETOF_GSCOOKIE + \
                                        8h
OFFSETOF_FRAME_REGISTERS            equ OFFSETOF_FRAME + \
                                        SIZEOF__Frame
SIZEOF_FIXED_FRAME                  equ OFFSETOF_FRAME_REGISTERS + \
                                        SIZEOF_CalleeSavedRegisters + 8h ; +8 for return address

.errnz SIZEOF_FIXED_FRAME mod 16, SIZEOF_FIXED_FRAME not aligned

;
; This method takes three secret parameters on the stack:
;
; incoming:
;
;       rsp ->  nStackSlots
;               entrypoint of shared MethodDesc
;               extra stack param
;               <space for StubHelperFrame>
;               return address
;               rcx home
;               rdx home
;                 :
;
;
; Stack Layout:
;
; rsp-> callee scratch
; + 8h  callee scratch
; +10h  callee scratch
; +18h  callee scratch
;       :
;       stack arguments
;       :
; rbp->     nStackSlots
; + 8h      entrypoint of shared MethodDesc
; +10h      extra stack param
; +18h      padding
; +20h      gsCookie
; +28h      __VFN_table
; +30h      m_Next
; +38h      m_calleeSavedRegisters
; +98h      m_ReturnAddress
; +a0h  rcx home
; +a8h  rdx home
; +b0h  r8 home
; +b8h  r9 home
;
NESTED_ENTRY InstantiatingMethodStubWorker, _TEXT
        .allocstack             SIZEOF_FIXED_FRAME - 8h     ; -8 for return address

        SAVE_CALLEE_SAVED_REGISTERS OFFSETOF_FRAME_REGISTERS

        SAVE_ARGUMENT_REGISTERS SIZEOF_FIXED_FRAME

        set_frame               rbp, 0
    END_PROLOGUE

        sub     rsp, SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES

        ;
        ; fully initialize the StubHelperFrame
        ;
        mov     rax, s_pStubHelperFrameVPtr
        mov     [rbp + OFFSETOF_FRAME], rax

        mov     rax, s_gsCookie
        mov     [rbp + OFFSETOF_GSCOOKIE], rax

        ;
        ; link the StubHelperFrame
        ;
        INLINE_GETTHREAD r12
        mov     rdx, [r12 + OFFSETOF__Thread__m_pFrame]
        mov     [rbp + OFFSETOF_FRAME + OFFSETOF__Frame__m_Next], rdx
        lea     rcx, [rbp + OFFSETOF_FRAME]
        mov     [r12 + OFFSETOF__Thread__m_pFrame], rcx

        add     rsp, SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES

        mov     rcx, [rbp + OFFSETOF_SECRET_PARAMS + 0h]        ; nStackSlots (includes padding for stack alignment)

        lea     rsi, [rbp + SIZEOF_FIXED_FRAME + SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + 8 * rcx]

StackCopyLoop:                          ; copy the arguments to stack top-down to carefully probe for sufficient stack space
        sub     rsi, 8
        push    qword ptr [rsi]
        dec     rcx
        jnz     StackCopyLoop

        push    qword ptr [rbp+OFFSETOF_SECRET_PARAMS + 10h]    ; push extra stack arg
        sub     rsp, SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES

        mov     rcx, [rbp + SIZEOF_FIXED_FRAME + 00h]
        mov     rdx, [rbp + SIZEOF_FIXED_FRAME + 08h]
        mov     r8, [rbp + SIZEOF_FIXED_FRAME + 10h]
        mov     r9, [rbp + SIZEOF_FIXED_FRAME + 18h]

        call    qword ptr [rbp+OFFSETOF_SECRET_PARAMS + 8h]     ; call target

ifdef _DEBUG
        mov     rcx, s_gsCookie
        cmp     [rbp + OFFSETOF_GSCookie], rcx
        je      GoodGSCookie
        call    JIT_FailFast
GoodGSCookie:
endif ; _DEBUG

        ;
        ; unlink the StubHelperFrame
        ;
        mov     rcx, [rbp + OFFSETOF_FRAME + OFFSETOF__Frame__m_Next]
        mov     [r12 + OFFSETOF__Thread__m_pFrame], rcx

        ;
        ; epilog
        ;

        lea     rsp, [rbp + OFFSETOF_FRAME_REGISTERS]

        POP_CALLEE_SAVED_REGISTERS

        ret

NESTED_END InstantiatingMethodStubWorker, _TEXT


        end

