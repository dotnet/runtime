; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; Define macros to build unwind data for prologues.
;

push_reg macro Reg

        push    Reg
        .pushreg Reg

        endm

push_eflags macro

        pushfq
        .allocstack 8

        endm

alloc_stack macro Size

        sub     rsp, Size
        .allocstack Size

        endm

save_reg macro Reg, Offset

        mov     Offset[rsp], Reg
        .savereg Reg, Offset

        endm

save_xmm macro Reg, Offset

        movq    Offset[rsp], Reg
        .savexmm Reg, Offset

        endm

save_xmm128 macro Reg, Offset

        movdqa  Offset[rsp], Reg
        .savexmm128 Reg, Offset

        endm

push_frame macro Code

        .pushframe Code

        endm

set_frame macro Reg, Offset

if Offset

        lea     Reg, Offset[rsp]

else

        mov     reg, rsp

endif

        .setframe Reg, Offset

        endm

END_PROLOGUE macro

        .endprolog

        endm

;
; Define alternate entry macro.
;

ALTERNATE_ENTRY macro Name

Name:

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

        endm

NESTED_END macro Name, section

Name    endp

Section ends

        endm




; 
; Appropriate instructions for certain specific scenarios:
;  - REPRET: should be used as the return instruction when the return is a branch 
;            target or immediately follows a conditional branch 
;  - TAILCALL_RAX: ("jmp rax") should be used for tailcalls, this emits an instruction 
;            sequence which is recognized by the unwinder as a valid epilogue terminator
;
REPRET      TEXTEQU <DB 0F3h, 0C3h>
TAILJMP_RAX TEXTEQU <DB 048h, 0FFh, 0E0h>
NOP_4BYTE   TEXTEQU <DB 066h, 066h, 066h, 090h>
        

        
