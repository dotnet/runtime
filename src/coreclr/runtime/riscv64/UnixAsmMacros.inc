// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
    LEAF_ENTRY \Name, \Section
    .ifnc \Handler, NoHandler
        .cfi_personality 0x1B, C_FUNC(\Handler) // 0x1B == DW_EH_PE_pcrel | DW_EH_PE_sdata4
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
    // make sure this symbol gets its own address
    nop
.endm

.macro ALTERNATE_ENTRY Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro LABELED_RETURN_ADDRESS Name
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
C_FUNC(\Name):
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
    la \HelperReg, C_FUNC(\Name)
    lw \HelperReg, 0(\HelperReg)
.endm

.macro PROLOG_SAVE_REG reg, ofs
    sd \reg, \ofs(sp)
    .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ssize
    addi  sp, sp, -\ssize
    .cfi_adjust_cfa_offset  \ssize

    sd  \reg1, 0(sp)
    sd  \reg2, 8(sp)

    .cfi_rel_offset  \reg1, 0
    .cfi_rel_offset  \reg2, 8
.endm

.macro EPILOG_RESTORE_REG reg, ofs
    ld  \reg, (\ofs)(sp)
    .cfi_restore \reg
.endm

.macro EPILOG_RETURN
    ret
.endm

.macro EPILOG_BRANCH Target
    j \Target
.endm

// Reserve 64 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs
    sd  a0, (\ofs)(\reg)
    sd  a1, (\ofs + 8)(\reg)
    sd  a2, (\ofs + 16)(\reg)
    sd  a3, (\ofs + 24)(\reg)
    sd  a4, (\ofs + 32)(\reg)
    sd  a5, (\ofs + 40)(\reg)
    sd  a6, (\ofs + 48)(\reg)
    sd  a7, (\ofs + 56)(\reg)
.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs
    fsd  fa0, (\ofs)(\reg)
    fsd  fa1, (\ofs + 8)(\reg)
    fsd  fa2, (\ofs + 16)(\reg)
    fsd  fa3, (\ofs + 24)(\reg)
    fsd  fa4, (\ofs + 32)(\reg)
    fsd  fa5, (\ofs + 40)(\reg)
    fsd  fa6, (\ofs + 48)(\reg)
    fsd  fa7, (\ofs + 56)(\reg)
.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs
    ld  a0, (\ofs)(\reg)
    ld  a1, (\ofs + 8)(\reg)
    ld  a2, (\ofs + 16)(\reg)
    ld  a3, (\ofs + 24)(\reg)
    ld  a4, (\ofs + 32)(\reg)
    ld  a5, (\ofs + 40)(\reg)
    ld  a6, (\ofs + 48)(\reg)
    ld  a7, (\ofs + 56)(\reg)
.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs
    fld  fa0, (\ofs)(\reg)
    fld  fa1, (\ofs + 8)(\reg)
    fld  fa2, (\ofs + 16)(\reg)
    fld  fa3, (\ofs + 24)(\reg)
    fld  fa4, (\ofs + 32)(\reg)
    fld  fa5, (\ofs + 40)(\reg)
    fld  fa6, (\ofs + 48)(\reg)
    fld  fa7, (\ofs + 56)(\reg)
.endm

// Reserve 64 bytes of memory before calling  SAVE_CALLEESAVED_REGISTERS
.macro SAVE_CALLEESAVED_REGISTERS reg, ofs
    PROLOG_SAVE_REG_PAIR s1, s2, \ofs + 16
    PROLOG_SAVE_REG_PAIR s3, s4, \ofs + 32
    PROLOG_SAVE_REG_PAIR s5, s6, \ofs + 48
    PROLOG_SAVE_REG_PAIR s7, s8, \ofs + 64
    PROLOG_SAVE_REG_PAIR s9, s10, \ofs + 80
    PROLOG_SAVE_REG_PAIR s11, tp  \ofs + 96
    PROLOG_SAVE_REG      gp,  \ofs + 112
.endm

// Reserve 64 bytes of memory before calling  SAVE_FLOAT_CALLEESAVED_REGISTERS
.macro SAVE_FLOAT_CALLEESAVED_REGISTERS reg, ofs
// TODO RISCV NYI
    sw  ra, 0(zero)
.endm

.macro RESTORE_CALLEESAVED_REGISTERS reg, ofs
    EPILOG_RESTORE_REG      gp  \ofs + 112
    EPILOG_RESTORE_REG_PAIR s11, tp  \ofs + 96
    EPILOG_RESTORE_REG_PAIR s9, s10, \ofs + 80
    EPILOG_RESTORE_REG_PAIR s7, s8,  \ofs + 64
    EPILOG_RESTORE_REG_PAIR s5, s6,  \ofs + 48
    EPILOG_RESTORE_REG_PAIR s3, s4,  \ofs + 32
    EPILOG_RESTORE_REG_PAIR s1, s2,  \ofs + 16
.endm

.macro RESTORE_FLOAT_CALLEESAVED_REGISTERS reg, ofs
// TODO RISCV NYI
    sw  ra, 0(zero)
.endm

.macro EMIT_BREAKPOINT
    ebreak
.endm

//-----------------------------------------------------------------------------
// Macro used to check (in debug builds only) whether the stack is 16-bytes aligned (a requirement before calling
// out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
// before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
// is misaligned.
//
.macro  CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        andi    t4, sp, 0xf
        beq     t4, zero, 0f
        EMIT_BREAKPOINT
0:
#endif
.endm

.macro InterlockedOperationBarrier
    fence rw, rw
.endm
