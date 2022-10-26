// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#error "TODO-RISCV64: review this; missing many macros for VM"

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1c, C_FUNC(\Handler) // TODO RISCV NYI
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro LEAF_END_MARKED Name, Section
C_FUNC(\Name\()_End):
        .global C_FUNC(\Name\()_End)
        LEAF_END \Name, \Section
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ofs
        addi sp, sp, -\ofs
        .cfi_adjust_cfa_offset -\ofs
        sd \reg1, sp, 0
        sd \ret2, sp, 8
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
        .ifc \reg1, fp
        mov fp, sp
        .cfi_def_cfa_register fp
        .endif
.endm


.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ld \reg2, sp, 8
        ld \reg1, sp, 0
        .cfi_restore \reg1
        .cfi_restore \reg2

        addi sp, sp, \ofs
        .cfi_adjust_cfa_offset -\ofs
.endm

.macro EMIT_BREAKPOINT
        ebreak
.endm
