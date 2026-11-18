// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
#ifdef FEATURE_NATIVEAOT
        // Not for CoreCLR: the DAC exports its LEAF_ENTRY stubs generated from libredefines.inc.
        .hidden C_FUNC(\Name)
#endif
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

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

.macro LEAF_END_MARKED Name, Section
C_FUNC(\Name\()_End):
        .global C_FUNC(\Name\()_End)
        LEAF_END \Name, \Section
        // make sure this symbol gets its own address
        nop
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

.macro PROLOG_STACK_ALLOC Size
        addi.d  $sp, $sp, -\Size
        .cfi_adjust_cfa_offset \Size
.endm

.macro EPILOG_STACK_FREE Size
        addi.d  $sp, $sp, \Size
        .cfi_adjust_cfa_offset -\Size
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

////NOTE: reg1 and reg2 must be the number and GPR type !!!
.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs, __def_cfa_save=0
        st.d  $r\reg1, $sp, \ofs
        st.d  $r\reg2, $sp, \ofs + 8
        .cfi_rel_offset  \reg1, \ofs
        .cfi_rel_offset  \reg2, \ofs + 8
        .if (\__def_cfa_save == 1)
        ori  $fp, $sp, 0
        .cfi_def_cfa_register  22
        .endif
.endm

////NOTE: reg1 and reg2 must be the number and GPR type !!!
.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize, __def_cfa_save=1
        addi.d  $sp, $sp, -\ssize
        .cfi_adjust_cfa_offset  \ssize

        st.d  $r\reg1, $sp, 0
        st.d  $r\reg2, $sp, 8
        .cfi_rel_offset  \reg1, 0
        .cfi_rel_offset  \reg2, 8
        .if (\__def_cfa_save == 1)
        ori  $fp, $sp, 0
        .cfi_def_cfa_register  22
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ssize
        addi.d  $sp, $sp, -\ssize
        .cfi_adjust_cfa_offset  \ssize

        st.d  $r\reg1, $sp, 0
        st.d  $r\reg2, $sp, 8
        .cfi_rel_offset  \reg1, 0
        .cfi_rel_offset  \reg2, 8
.endm

.macro EPILOG_RESTORE_REG reg, ofs
        ld.d  $r\reg, $sp, \ofs
        .cfi_restore  \reg
.endm

// Restoring $fp requires $sp == $fp if $fp is the CFA register.
.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        .ifc  \reg1, 22
        .cfi_def_cfa_register  3
        .endif
        ld.d  $r\reg2, $sp, \ofs + 8
        ld.d  $r\reg1, $sp, \ofs
        .cfi_restore  \reg2
        .cfi_restore  \reg1
.endm

// Restoring $fp requires $sp == $fp if $fp is the CFA register.
.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ssize
        .ifc  \reg1, 22
        .cfi_def_cfa_register  3
        .endif
        ld.d  $r\reg2, $sp, 8
        ld.d  $r\reg1, $sp, 0
        .cfi_restore  \reg2
        .cfi_restore  \reg1

        addi.d  $sp, $sp, \ssize
        .cfi_adjust_cfa_offset  -\ssize
.endm

.macro EPILOG_RETURN
        jirl  $r0, $ra, 0
.endm

.macro EPILOG_BRANCH Target
        b \Target
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

// Reserve 64 bytes of memory before calling  SAVE_CALLEESAVED_REGISTERS
.macro SAVE_CALLEESAVED_REGISTERS reg, ofs

        PROLOG_SAVE_REG_PAIR 23, 24, \ofs + 16
        PROLOG_SAVE_REG_PAIR 25, 26, \ofs + 32
        PROLOG_SAVE_REG_PAIR 27, 28, \ofs + 48
        PROLOG_SAVE_REG_PAIR 29, 30, \ofs + 64
        PROLOG_SAVE_REG      31,     \ofs + 80

.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_CALLEESAVED_REGISTERS
.macro SAVE_FLOAT_CALLEESAVED_REGISTERS reg, ofs

        fst.d  $f24, \reg, \ofs
        fst.d  $f25, \reg, \ofs+8
        fst.d  $f26, \reg, \ofs+16
        fst.d  $f27, \reg, \ofs+24
        fst.d  $f28, \reg, \ofs+32
        fst.d  $f29, \reg, \ofs+40
        fst.d  $f30, \reg, \ofs+48
        fst.d  $f31, \reg, \ofs+56

.endm

.macro RESTORE_CALLEESAVED_REGISTERS reg, ofs

        EPILOG_RESTORE_REG      31,     \ofs + 80
        EPILOG_RESTORE_REG_PAIR 29, 30, \ofs + 64
        EPILOG_RESTORE_REG_PAIR 27, 28, \ofs + 48
        EPILOG_RESTORE_REG_PAIR 25, 26, \ofs + 32
        EPILOG_RESTORE_REG_PAIR 23, 24, \ofs + 16
.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs

        ld.d $a7, \reg, \ofs+56
        ld.d $a6, \reg, \ofs+48
        ld.d $a5, \reg, \ofs+40
        ld.d $a4, \reg, \ofs+32
        ld.d $a3, \reg, \ofs+24
        ld.d $a2, \reg, \ofs+16
        ld.d $a1, \reg, \ofs+8
        ld.d $a0, \reg, \ofs

.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        fld.d  $f7, \reg, \ofs+56
        fld.d  $f6, \reg, \ofs+48
        fld.d  $f5, \reg, \ofs+40
        fld.d  $f4, \reg, \ofs+32
        fld.d  $f3, \reg, \ofs+24
        fld.d  $f2, \reg, \ofs+16
        fld.d  $f1, \reg, \ofs+8
        fld.d  $f0, \reg, \ofs

.endm

.macro RESTORE_FLOAT_CALLEESAVED_REGISTERS reg, ofs

        fld.d  $f24, $r\reg, \ofs
        fld.d  $f25, $r\reg, \ofs+8
        fld.d  $f26, $r\reg, \ofs+16
        fld.d  $f27, $r\reg, \ofs+24
        fld.d  $f28, $r\reg, \ofs+32
        fld.d  $f29, $r\reg, \ofs+40
        fld.d  $f30, $r\reg, \ofs+48
        fld.d  $f31, $r\reg, \ofs+56

.endm

.macro EMIT_BREAKPOINT
        break 0
.endm

.macro EPILOG_BRANCH_REG reg
        jirl  $r0, \reg, 0
.endm

//-----------------------------------------------------------------------------
// Macro used to check (in debug builds only) whether the stack is 16-bytes aligned (a requirement before calling
// out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
// before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
// is misaligned.
//
.macro  CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        andi    $t4, $sp, 0xf
        beq     $t4, $r0, 0f
        EMIT_BREAKPOINT
0:
#endif
.endm

.macro  InterlockedOperationBarrier
    dbar  0
.endm

// Loads the address of a thread-local variable into the target register,
// which cannot be a0. Preserves all other registers.
.macro INLINE_GET_TLS_VAR target, var
    .ifc  \target, $a0
        .error "target cannot be a0"
    .endif

    addi.d  $sp, $sp, -16
    st.d  $a0, $sp, 0
    st.d  $ra, $sp, 8

    // This instruction is recognized and potentially patched
    // by the linker (GD->IE/LE relaxation).
    la.tls.desc  $a0, \var

    ori  \target, $tp, 0
    add.d  \target, \target, $a0

    ld.d  $a0, $sp, 0
    ld.d  $ra, $sp, 8
    addi.d $sp, $sp, 16
.endm
