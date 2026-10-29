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

;
; Prologue of all funclet calling helpers
;
FUNCLET_CALL_PROLOGUE macro localsCount, alignStack

        push_nonvol_reg r15     ; save preserved regs for OS stackwalker
        push_nonvol_reg r14     ; ...
        push_nonvol_reg r13     ; ...
        push_nonvol_reg r12     ; ...
        push_nonvol_reg rbx     ; ...
        push_nonvol_reg rsi     ; ...
        push_nonvol_reg rdi     ; ...
        push_nonvol_reg rbp     ; ...

        arguments_scratch_area_size = 20h
        xmm_save_area_size = 10 * 10h ; xmm6..xmm15 save area
        stack_alloc_size = arguments_scratch_area_size + localsCount * 8 + alignStack * 8 + xmm_save_area_size
        rsp_offsetof_arguments = stack_alloc_size + 8*8h + 8h
        rsp_offsetof_locals = arguments_scratch_area_size + xmm_save_area_size

        alloc_stack     stack_alloc_size

        save_xmm128_postrsp xmm6,  (arguments_scratch_area_size + 0 * 10h)
        save_xmm128_postrsp xmm7,  (arguments_scratch_area_size + 1 * 10h)
        save_xmm128_postrsp xmm8,  (arguments_scratch_area_size + 2 * 10h)
        save_xmm128_postrsp xmm9,  (arguments_scratch_area_size + 3 * 10h)
        save_xmm128_postrsp xmm10, (arguments_scratch_area_size + 4 * 10h)
        save_xmm128_postrsp xmm11, (arguments_scratch_area_size + 5 * 10h)
        save_xmm128_postrsp xmm12, (arguments_scratch_area_size + 6 * 10h)
        save_xmm128_postrsp xmm13, (arguments_scratch_area_size + 7 * 10h)
        save_xmm128_postrsp xmm14, (arguments_scratch_area_size + 8 * 10h)
        save_xmm128_postrsp xmm15, (arguments_scratch_area_size + 9 * 10h)

        END_PROLOGUE

        endm

;
; Epilogue of all funclet calling helpers
;
FUNCLET_CALL_EPILOGUE macro

        movdqa  xmm6,  [rsp + arguments_scratch_area_size + 0 * 10h]
        movdqa  xmm7,  [rsp + arguments_scratch_area_size + 1 * 10h]
        movdqa  xmm8,  [rsp + arguments_scratch_area_size + 2 * 10h]
        movdqa  xmm9,  [rsp + arguments_scratch_area_size + 3 * 10h]
        movdqa  xmm10, [rsp + arguments_scratch_area_size + 4 * 10h]
        movdqa  xmm11, [rsp + arguments_scratch_area_size + 5 * 10h]
        movdqa  xmm12, [rsp + arguments_scratch_area_size + 6 * 10h]
        movdqa  xmm13, [rsp + arguments_scratch_area_size + 7 * 10h]
        movdqa  xmm14, [rsp + arguments_scratch_area_size + 8 * 10h]
        movdqa  xmm15, [rsp + arguments_scratch_area_size + 9 * 10h]

        add     rsp, stack_alloc_size
        pop     rbp
        pop     rdi
        pop     rsi
        pop     rbx
        pop     r12
        pop     r13
        pop     r14
        pop     r15

        endm
