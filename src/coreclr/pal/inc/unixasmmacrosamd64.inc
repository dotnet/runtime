// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
#if defined(__APPLE__)
        .cfi_personality 0x9b, C_FUNC(\Handler) // 0x9b == DW_EH_PE_indirect | DW_EH_PE_pcrel | DW_EH_PE_sdata4
#else
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4
#endif
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
#if defined(__APPLE__)
        .set LOCAL_LABEL(\Name\()_Size), . - C_FUNC(\Name)
        .section __LD,__compact_unwind,regular,debug
        .quad C_FUNC(\Name)
        .long LOCAL_LABEL(\Name\()_Size)
        .long 0x04000000 # DWARF
        .quad 0
        .quad 0
#endif
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
        C_FUNC(\Name) = .
.endm

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
#if defined(__APPLE__)
        .text
#else
        .type \Name, %function
#endif
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
#if !defined(__APPLE__)
        .size \Name, .-\Name
#endif
        .cfi_endproc
.endm

.macro LEAF_END_MARKED Name, Section
C_FUNC(\Name\()_End):
        .global C_FUNC(\Name\()_End)
        LEAF_END \Name, \Section
.endm

.macro NOP_6_BYTE
        .byte 0x66
        .byte 0x0F
        .byte 0x1F
        .byte 0x44
        .byte 0x00
        .byte 0x00
.endm

.macro NOP_3_BYTE
        nop dword ptr [rax]
.endm

.macro NOP_2_BYTE
        xchg ax, ax
.endm

.macro REPRET
        .byte 0xf3
        .byte 0xc3
.endm

.macro TAILJMP_RAX
        .byte 0x48
        .byte 0xFF
        .byte 0xE0
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        mov \HelperReg, [rip + C_FUNC(\Name)@GOTPCREL]
.endm

.macro push_nonvol_reg Register
        push \Register
        .cfi_adjust_cfa_offset 8
        .cfi_rel_offset \Register, 0
.endm

.macro pop_nonvol_reg Register
        pop \Register
        .cfi_adjust_cfa_offset -8
        .cfi_restore \Register
.endm

.macro alloc_stack Size
.att_syntax
        lea -(\Size)(%rsp), %rsp
.intel_syntax noprefix
        .cfi_adjust_cfa_offset \Size
.endm

.macro free_stack Size
.att_syntax
        lea \Size(%rsp), %rsp
.intel_syntax noprefix
        .cfi_adjust_cfa_offset -\Size
.endm

.macro set_cfa_register Reg, Offset
        .cfi_def_cfa_register \Reg
        .cfi_def_cfa_offset \Offset
.endm

.macro save_reg_postrsp Reg, Offset
        __Offset = \Offset
        mov     qword ptr [rsp + __Offset], \Reg
        .cfi_rel_offset \Reg, __Offset
.endm

.macro restore_reg Reg, Offset
        __Offset = \Offset
        mov             \Reg, [rsp + __Offset]
        .cfi_restore \Reg
.endm

.macro save_xmm128_postrsp Reg, Offset
        __Offset = \Offset
        movdqa  xmmword ptr [rsp + __Offset], \Reg
        // NOTE: We cannot use ".cfi_rel_offset \Reg, __Offset" here,
        // the xmm registers are not supported by the libunwind
.endm

.macro restore_xmm128 Reg, ofs
        __Offset = \ofs
        movdqa          \Reg, xmmword ptr [rsp + __Offset]
        // NOTE: We cannot use ".cfi_restore \Reg" here,
        // the xmm registers are not supported by the libunwind

.endm

.macro PUSH_CALLEE_SAVED_REGISTERS

        push_register rbp
        push_register rbx
        push_register r15
        push_register r14
        push_register r13
        push_register r12

.endm

.macro POP_CALLEE_SAVED_REGISTERS

        pop_nonvol_reg r12
        pop_nonvol_reg r13
        pop_nonvol_reg r14
        pop_nonvol_reg r15
        pop_nonvol_reg rbx
        pop_nonvol_reg rbp

.endm

.macro push_register Reg
        push            \Reg
        .cfi_adjust_cfa_offset 8
.endm

.macro push_eflags
        pushfq
        .cfi_adjust_cfa_offset 8
.endm

.macro push_argument_register Reg
        push_register \Reg
.endm

.macro PUSH_ARGUMENT_REGISTERS

        push_argument_register r9
        push_argument_register r8
        push_argument_register rcx
        push_argument_register rdx
        push_argument_register rsi
        push_argument_register rdi

.endm

.macro pop_register Reg
        pop            \Reg
        .cfi_adjust_cfa_offset -8
.endm

.macro pop_eflags
        popfq
        .cfi_adjust_cfa_offset -8
.endm

.macro pop_argument_register Reg
        pop_register \Reg
.endm

.macro POP_ARGUMENT_REGISTERS

        pop_argument_register rdi
        pop_argument_register rsi
        pop_argument_register rdx
        pop_argument_register rcx
        pop_argument_register r8
        pop_argument_register r9

.endm

.macro SAVE_FLOAT_ARGUMENT_REGISTERS ofs

        save_xmm128_postrsp xmm0, \ofs
        save_xmm128_postrsp xmm1, \ofs + 0x10
        save_xmm128_postrsp xmm2, \ofs + 0x20
        save_xmm128_postrsp xmm3, \ofs + 0x30
        save_xmm128_postrsp xmm4, \ofs + 0x40
        save_xmm128_postrsp xmm5, \ofs + 0x50
        save_xmm128_postrsp xmm6, \ofs + 0x60
        save_xmm128_postrsp xmm7, \ofs + 0x70

.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS ofs

        restore_xmm128  xmm0, \ofs
        restore_xmm128  xmm1, \ofs + 0x10
        restore_xmm128  xmm2, \ofs + 0x20
        restore_xmm128  xmm3, \ofs + 0x30
        restore_xmm128  xmm4, \ofs + 0x40
        restore_xmm128  xmm5, \ofs + 0x50
        restore_xmm128  xmm6, \ofs + 0x60
        restore_xmm128  xmm7, \ofs + 0x70

.endm

// Stack layout:
//
// (stack parameters)
// ...
// return address
// CalleeSavedRegisters::rbp
// CalleeSavedRegisters::rbx
// CalleeSavedRegisters::r15
// CalleeSavedRegisters::r14
// CalleeSavedRegisters::r13
// CalleeSavedRegisters::r12
// ArgumentRegisters::r9
// ArgumentRegisters::r8
// ArgumentRegisters::rcx
// ArgumentRegisters::rdx
// ArgumentRegisters::rsi
// ArgumentRegisters::rdi    <- __PWTB_StackAlloc, __PWTB_TransitionBlock
// padding to align xmm save area
// xmm7
// xmm6
// xmm5
// xmm4
// xmm3
// xmm2
// xmm1
// xmm0                      <- __PWTB_FloatArgumentRegisters
// extra locals + padding to qword align
.macro PROLOG_WITH_TRANSITION_BLOCK extraLocals = 0, stackAllocOnEntry = 0, stackAllocSpill1, stackAllocSpill2, stackAllocSpill3

        set_cfa_register rsp, 8

        __PWTB_FloatArgumentRegisters = \extraLocals

        .if ((__PWTB_FloatArgumentRegisters % 16) != 0)
        __PWTB_FloatArgumentRegisters = __PWTB_FloatArgumentRegisters + 8
        .endif

        __PWTB_StackAlloc = __PWTB_FloatArgumentRegisters + 8 * 16 + 8 // 8 floating point registers
        __PWTB_TransitionBlock = __PWTB_StackAlloc

        .if \stackAllocOnEntry >= 4*8
        .error "Max supported stackAllocOnEntry is 3*8"
        .endif

        .if \stackAllocOnEntry > 0
        .cfi_adjust_cfa_offset \stackAllocOnEntry
        .endif

        // PUSH_CALLEE_SAVED_REGISTERS expanded here

        .if \stackAllocOnEntry < 8
        push_nonvol_reg rbp
        mov rbp, rsp
        .endif

        .if \stackAllocOnEntry < 2*8
        push_nonvol_reg rbx
        .endif

        .if \stackAllocOnEntry < 3*8
        push_nonvol_reg r15
        .endif

        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12

        // ArgumentRegisters
        PUSH_ARGUMENT_REGISTERS

        .if \stackAllocOnEntry >= 3*8
        mov \stackAllocSpill3, [rsp + 0x48]
        save_reg_postrsp    r15, 0x48
        .endif

        .if \stackAllocOnEntry >= 2*8
        mov \stackAllocSpill2, [rsp + 0x50]
        save_reg_postrsp    rbx, 0x50
        .endif

        .if \stackAllocOnEntry >= 8
        mov \stackAllocSpill1, [rsp + 0x58]
        save_reg_postrsp    rbp, 0x58
        lea rbp, [rsp + 0x58]
        .endif

        alloc_stack     __PWTB_StackAlloc
        SAVE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters

        END_PROLOGUE

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_RETURN

        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS
        ret

.endm

.macro EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS

.endm

.macro RESET_FRAME_WITH_RBP

        mov     rsp, rbp
        set_cfa_register rsp, 16
        pop_nonvol_reg rbp
        .cfi_same_value rbp

.endm
