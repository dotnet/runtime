// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1c, C_FUNC(\Handler) // 0x1c == DW_EH_PE_pcrel | DW_EH_PE_sdata8
        .endif
.endm

.macro NESTED_END Name, Section
        LEAF_END \Name, \Section
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro ALTERNATE_ENTRY Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LEAF_END Name, Section
        .size \Name, .-\Name
        .cfi_endproc
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
        la.local  \HelperReg, \Name
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
        la.local  \HelperReg, \Name
        ld.d  \HelperReg, \HelperReg, 0
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
        la.local  \HelperReg, \Name
        ld.w  \HelperReg, \HelperReg, 0
.endm

.macro EPILOG_STACK_RESTORE
        ori  $sp, $fp, 0
        .cfi_restore 3
.endm

////NOTE: reg must be the number and GPR type !!!
.macro PROLOG_SAVE_REG reg, ofs
        st.d $r\reg, $sp, \ofs
        .cfi_rel_offset \reg, \ofs
.endm

.macro EPILOG_RETURN
        jirl  $r0, $ra, 0
.endm

// Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs

        st.d  $a0, \reg, \ofs
        st.d  $a1, \reg, \ofs+8
        st.d  $a2, \reg, \ofs+16
        st.d  $a3, \reg, \ofs+24
        st.d  $a4, \reg, \ofs+32
        st.d  $a5, \reg, \ofs+40
        st.d  $a6, \reg, \ofs+48
        st.d  $a7, \reg, \ofs+56

.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        fst.d $f0, \reg, \ofs
        fst.d $f1, \reg, \ofs+8
        fst.d $f2, \reg, \ofs+16
        fst.d $f3, \reg, \ofs+24
        fst.d $f4, \reg, \ofs+32
        fst.d $f5, \reg, \ofs+40
        fst.d $f6, \reg, \ofs+48
        fst.d $f7, \reg, \ofs+56

.endm

.macro EMIT_BREAKPOINT
        break 0
.endm

.macro EPILOG_BRANCH_REG reg
        jirl  $r0, \reg, 0
.endm
