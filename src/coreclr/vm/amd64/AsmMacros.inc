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
; Inlined version of GetThread
; Trashes rax and r11
;
gCurrentThreadInfo  TEXTEQU <?gCurrentThreadInfo@@3UThreadLocalInfo@@A>

INLINE_GETTHREAD macro Reg

        EXTERN _tls_index : DWORD
        EXTERN gCurrentThreadInfo:DWORD

        mov     r11d, [_tls_index]
        mov     rax, gs:[OFFSET__TEB__ThreadLocalStoragePointer]
        mov     rax, [rax + r11 * 8]
        mov     r11d, SECTIONREL gCurrentThreadInfo
        mov     Reg, [rax + r11]

        endm

; if you change this code there will be corresponding code in JITInterfaceGen.cpp which will need to be changed
;

JIT_HELPER_MONITOR_THUNK macro THUNK_NAME, Section
Section segment para 'CODE'
        align 16
        public THUNK_NAME
THUNK_NAME proc
        xor     edx, edx
THUNK_NAME endp
Section ends
        endm

;
; Useful for enabling C++ to know where to patch code at runtime.
;
PATCH_LABEL macro Name
        public Name
Name::
        endm

;
; Define alternate entry macro.
;
ALTERNATE_ENTRY macro Name
        public Name
Name label proc
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


; Stack layout:
;
; (stack parameters)
; ...
; r9
; r8
; rdx
; rcx                       <- __PWTB_ArgumentRegisters
; return address
; CalleeSavedRegisters::r15
; CalleeSavedRegisters::r14
; CalleeSavedRegisters::r13
; CalleeSavedRegisters::r12
; CalleeSavedRegisters::rbp
; CalleeSavedRegisters::rbx
; CalleeSavedRegisters::rsi
; CalleeSavedRegisters::rdi <- __PWTB_StackAlloc
; padding to align xmm save area
; xmm3
; xmm2
; xmm1
; xmm0                      <- __PWTB_FloatArgumentRegisters
; extra locals + padding to qword align
; callee's r9
; callee's r8
; callee's rdx
; callee's rcx

PROLOG_WITH_TRANSITION_BLOCK macro extraLocals := <0>, stackAllocOnEntry := <0>, stackAllocSpill1, stackAllocSpill2, stackAllocSpill3

        __PWTB_FloatArgumentRegisters = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + extraLocals

        if (__PWTB_FloatArgumentRegisters mod 16) ne 0
        __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
        endif

        __PWTB_StackAlloc = __PWTB_FloatArgumentRegisters + 4 * 16 + 8
        __PWTB_TransitionBlock = __PWTB_StackAlloc
        __PWTB_ArgumentRegisters = __PWTB_StackAlloc + 9 * 8

        .errnz stackAllocOnEntry ge 4*8, Max supported stackAllocOnEntry is 3*8

        if stackAllocOnEntry gt 0
        .allocstack stackAllocOnEntry
        endif

        ; PUSH_CALLEE_SAVED_REGISTERS expanded here

        if stackAllocOnEntry lt 8
        push_nonvol_reg r15
        endif

        if stackAllocOnEntry lt 2*8
        push_nonvol_reg r14
        endif

        if stackAllocOnEntry lt 3*8
        push_nonvol_reg r13
        endif

        push_nonvol_reg r12
        push_nonvol_reg rbp
        push_nonvol_reg rbx
        push_nonvol_reg rsi
        push_nonvol_reg rdi

        alloc_stack     __PWTB_StackAlloc
        SAVE_ARGUMENT_REGISTERS __PWTB_ArgumentRegisters
        SAVE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters

        if stackAllocOnEntry ge 3*8
        mov stackAllocSpill3, [rsp + __PWTB_StackAlloc + 28h]
        save_reg_postrsp    r13, __PWTB_StackAlloc + 28h
        endif

        if stackAllocOnEntry ge 2*8
        mov stackAllocSpill2, [rsp + __PWTB_StackAlloc + 30h]
        save_reg_postrsp    r14, __PWTB_StackAlloc + 30h
        endif

        if stackAllocOnEntry ge 8
        mov stackAllocSpill1, [rsp + __PWTB_StackAlloc + 38h]
        save_reg_postrsp    r15, __PWTB_StackAlloc + 38h
        endif

        END_PROLOGUE

        endm

EPILOG_WITH_TRANSITION_BLOCK_RETURN macro

        add rsp, __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS
        ret

        endm

EPILOG_WITH_TRANSITION_BLOCK_TAILCALL macro

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        RESTORE_ARGUMENT_REGISTERS __PWTB_ArgumentRegisters
        add rsp, __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS

        endm
