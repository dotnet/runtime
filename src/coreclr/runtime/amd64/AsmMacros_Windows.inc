; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Define macros to build unwind data for prologues.
;

push_nonvol_reg macro Reg

        .errnz ___STACK_ADJUSTMENT_FORBIDDEN, <push_nonvol_reg cannot be used after save_reg_postrsp>

        push    Reg
        .pushreg Reg

        endm

push_vol_reg macro Reg

        .errnz ___STACK_ADJUSTMENT_FORBIDDEN, push_vol_reg cannot be used after save_reg_postrsp

        push    Reg
        .allocstack 8

        endm

push_imm macro imm

        .errnz ___STACK_ADJUSTMENT_FORBIDDEN, push_vol_reg cannot be used after save_reg_postrsp

        push    imm
        .allocstack 8

        endm

push_eflags macro

        .errnz ___STACK_ADJUSTMENT_FORBIDDEN, push_eflags cannot be used after save_reg_postrsp

        pushfq
        .allocstack 8

        endm

alloc_stack macro Size

        .errnz ___STACK_ADJUSTMENT_FORBIDDEN, alloc_stack cannot be used after save_reg_postrsp

        sub     rsp, Size
        .allocstack Size

        endm

save_reg_frame macro Reg, FrameReg, Offset

        .erre ___FRAME_REG_SET, save_reg_frame cannot be used before set_frame

        mov     Offset[FrameReg], Reg
        .savereg Reg, Offset

        endm

save_reg_postrsp macro Reg, Offset

        .errnz ___FRAME_REG_SET, save_reg_postrsp cannot be used after set_frame

        mov     Offset[rsp], Reg
        .savereg Reg, Offset

        ___STACK_ADJUSTMENT_FORBIDDEN = 1

        endm

save_xmm128_frame macro Reg, FrameReg, Offset

        .erre ___FRAME_REG_SET, save_xmm128_frame cannot be used before set_frame

        movdqa  Offset[FrameReg], Reg
        .savexmm128 Reg, Offset

        endm

save_xmm128_postrsp macro Reg, Offset

        .errnz ___FRAME_REG_SET, save_reg_postrsp cannot be used after set_frame

        movdqa  Offset[rsp], Reg
        .savexmm128 Reg, Offset

        ___STACK_ADJUSTMENT_FORBIDDEN = 1

        endm

set_frame macro Reg, Offset

        .errnz ___FRAME_REG_SET, set_frame cannot be used more than once

if Offset

        lea     Reg, Offset[rsp]

else

        mov     reg, rsp

endif

        .setframe Reg, Offset
        ___FRAME_REG_SET = 1

        endm

END_PROLOGUE macro

        .endprolog

        endm


;
; Define function entry/end macros.
;

LEAF_ENTRY macro Name, Section

Section segment para 'CODE'

        align   16

        public  Name
Name    proc

        endm

LEAF_END macro Name, section

Name    endp

Section ends

        endm

LEAF_END_MARKED macro Name, section
        public Name&_End
Name&_End label qword

Name    endp

        ; this nop is important to keep the label in
        ; the right place in the face of BBT
        nop

Section ends

        endm

NESTED_ENTRY macro Name, Section, Handler

Section segment para 'CODE'

        align   16

        public  Name

ifb <Handler>

Name    proc    frame

else

Name    proc    frame:Handler

endif

        ___FRAME_REG_SET = 0
        ___STACK_ADJUSTMENT_FORBIDDEN = 0

        endm

NESTED_END macro Name, section

Name    endp

Section ends

        endm

NESTED_END_MARKED macro Name, section
        public Name&_End
Name&_End label qword

Name    endp

Section ends

        endm

;
; Define alternate entry macro.
;
ALTERNATE_ENTRY macro Name
        public Name
Name label proc
        endm

LABELED_RETURN_ADDRESS macro Name

Name label proc
PUBLIC Name
        endm

;
; Useful for enabling C++ to know where to patch code at runtime.
;
PATCH_LABEL macro Name
        public Name
Name::
        endm

;
; Appropriate instructions for certain specific scenarios:
;  - REPRET: should be used as the return instruction when the return is a branch
;            target or immediately follows a conditional branch
;  - TAILJMP_RAX: ("jmp rax") should be used for tailcalls, this emits an instruction
;            sequence which is recognized by the unwinder as a valid epilogue terminator
;
REPRET      TEXTEQU <DB 0F3h, 0C3h>
TAILJMP_RAX TEXTEQU <DB 048h, 0FFh, 0E0h>

NOP_2_BYTE macro

        xchg ax,ax

        endm

NOP_3_BYTE macro

        nop dword ptr [rax]

        endm

PUSH_CALLEE_SAVED_REGISTERS macro

        push_nonvol_reg r15
        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12
        push_nonvol_reg rbp
        push_nonvol_reg rbx
        push_nonvol_reg rsi
        push_nonvol_reg rdi

        endm

SAVE_CALLEE_SAVED_REGISTERS macro ofs

        save_reg_postrsp    rdi, ofs + 0h
        save_reg_postrsp    rsi, ofs + 8h
        save_reg_postrsp    rbx, ofs + 10h
        save_reg_postrsp    rbp, ofs + 18h
        save_reg_postrsp    r12, ofs + 20h
        save_reg_postrsp    r13, ofs + 28h
        save_reg_postrsp    r14, ofs + 30h
        save_reg_postrsp    r15, ofs + 38h

        endm

POP_CALLEE_SAVED_REGISTERS macro

        pop             rdi
        pop             rsi
        pop             rbx
        pop             rbp
        pop             r12
        pop             r13
        pop             r14
        pop             r15

        endm

SAVE_ARGUMENT_REGISTERS macro ofs

        save_reg_postrsp rcx, ofs + 0h
        save_reg_postrsp rdx, ofs + 8h
        save_reg_postrsp r8,  ofs + 10h
        save_reg_postrsp r9,  ofs + 18h

        endm

RESTORE_ARGUMENT_REGISTERS macro ofs

        mov             rcx, [rsp + ofs + 0h]
        mov             rdx, [rsp + ofs + 8h]
        mov             r8,  [rsp + ofs + 10h]
        mov             r9,  [rsp + ofs + 18h]

        endm

SAVE_FLOAT_ARGUMENT_REGISTERS macro ofs

        save_xmm128_postrsp xmm0, ofs
        save_xmm128_postrsp xmm1, ofs + 10h
        save_xmm128_postrsp xmm2, ofs + 20h
        save_xmm128_postrsp xmm3, ofs + 30h

        endm

RESTORE_FLOAT_ARGUMENT_REGISTERS macro ofs

        movdqa          xmm0, [rsp + ofs]
        movdqa          xmm1, [rsp + ofs + 10h]
        movdqa          xmm2, [rsp + ofs + 20h]
        movdqa          xmm3, [rsp + ofs + 30h]

        endm

_tls_array     equ 58h     ;; offsetof(TEB, ThreadLocalStoragePointer)

;;
;; __declspec(thread) variable
;;
INLINE_GET_TLS_VAR macro destReg, trashReg, variable
    EXTERN _tls_index : DWORD
    EXTERN variable:DWORD

;;
;; construct 'eax' from 'rax' so that the register size and data size match
;;
;; BEWARE: currently only r10 is allowed as destReg from the r8-r15 set.
;;
ifidni <destReg>, <r10>
destRegDWORD EQU r10d
else
destRegDWORD TEXTEQU @CatStr( e, @SubStr( destReg, 2, 2 ) )
endif

    mov         destRegDWORD, [_tls_index]
    mov         trashReg, gs:[_tls_array]
    mov         trashReg, [trashReg + destReg * 8]
    mov         destRegDWORD, SECTIONREL variable
    add         destReg, trashReg

endm

;; GC type flags
GC_ALLOC_FINALIZE               equ 1
