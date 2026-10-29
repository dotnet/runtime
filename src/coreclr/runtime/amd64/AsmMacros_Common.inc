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

;
;  - TAILJMP_RAX: ("jmp rax") should be used for tailcalls, this emits an instruction
;            sequence which is recognized by the unwinder as a valid epilogue terminator
;
TAILJMP_RAX TEXTEQU <DB 048h, 0FFh, 0E0h>
