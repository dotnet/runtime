// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define xip0 x16
#define xip1 x17
#define xpr x18

// On MacOS, local labels cannot be used in arithmetic expressions.
#if defined(__APPLE__)
#define FIXUP_LABEL(name) name
#else
#define FIXUP_LABEL(name) .L##name
#endif

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
#if defined(__APPLE__)
        .text
        .p2align        2
#else
#ifdef FEATURE_NATIVEAOT
        // Not for CoreCLR: the DAC exports its LEAF_ENTRY stubs generated from libredefines.inc.
        .hidden C_FUNC(\Name)
#endif
        .type \Name, %function
#endif
C_FUNC(\Name):
        .cfi_startproc
.endm

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
.endm

.macro ALTERNATE_ENTRY Name
#if defined(__APPLE__)
        .alt_entry C_FUNC(\Name)
        .private_extern C_FUNC(\Name)
#else
        .global C_FUNC(\Name)
        .hidden C_FUNC(\Name)
#endif
C_FUNC(\Name):
.endm

.macro PATCH_LABEL Name
#if defined(__APPLE__)
        .alt_entry C_FUNC(\Name)
        .private_extern C_FUNC(\Name)
#else
        .global C_FUNC(\Name)
#endif
C_FUNC(\Name):
.endm

.macro LEAF_END Name, Section
#if !defined(__APPLE__)
        .size \Name, .-\Name
#endif
        .cfi_endproc
.endm

.macro LEAF_END_MARKED Name, Section
#if defined(__APPLE__)
        .alt_entry C_FUNC(\Name\()_End)
        .private_extern C_FUNC(\Name\()_End)
#else
        .global C_FUNC(\Name\()_End)
#endif
C_FUNC(\Name\()_End):
        LEAF_END \Name, \Section
        // make sure this symbol gets its own address
        nop
.endm

.macro EMIT_BREAKPOINT
        brk #0xf000
.endm

.macro LABELED_RETURN_ADDRESS Name
        .global C_FUNC(\Name)
#if !defined(__APPLE__)
        .hidden C_FUNC(\Name)
#endif
C_FUNC(\Name):
.endm

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
#if defined(__APPLE__)
        adrp    \HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr     \HelperReg, [\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
#else
        adrp    \HelperReg, C_FUNC(\Name)
        add     \HelperReg, \HelperReg, :lo12:C_FUNC(\Name)
#endif
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
#if defined(__APPLE__)
        adrp \HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  \HelperReg, [\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  \HelperReg, [\HelperReg]
#else
        adrp \HelperReg, C_FUNC(\Name)
        ldr  \HelperReg, [\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
#if defined(__APPLE__)
        adrp x\HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  x\HelperReg, [x\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  w\HelperReg, [x\HelperReg]
#else
        adrp x\HelperReg, C_FUNC(\Name)
        ldr  w\HelperReg, [x\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm

.macro PROLOG_STACK_ALLOC Size
        sub sp, sp, \Size
.endm

.macro EPILOG_STACK_FREE Size
        add sp, sp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro EPILOG_STACK_RESTORE
        mov sp, fp
        .cfi_restore sp
.endm

.macro PROLOG_SAVE_REG reg, ofs
        str \reg, [sp, \ofs]
        .cfi_rel_offset \reg, \ofs
.endm

.macro PROLOG_SAVE_REG_PAIR reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]
        .cfi_rel_offset \reg1, \ofs
        .cfi_rel_offset \reg2, \ofs + 8
        .ifc \reg1, fp
        mov fp, sp
        .cfi_def_cfa_register fp
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]!
        .cfi_adjust_cfa_offset -\ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
        .ifc \reg1, fp
        mov fp, sp
        .cfi_def_cfa_register fp
        .endif
.endm

.macro PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED reg1, reg2, ofs
        stp \reg1, \reg2, [sp, \ofs]!
        .cfi_adjust_cfa_offset -\ofs
        .cfi_rel_offset \reg1, 0
        .cfi_rel_offset \reg2, 8
.endm

.macro EPILOG_RESTORE_REG reg, ofs
        ldr \reg, [sp, \ofs]
        .cfi_restore \reg
.endm

.macro EPILOG_RESTORE_REG_PAIR reg1, reg2, ofs
        ldp \reg1, \reg2, [sp, \ofs]
        .cfi_restore \reg1
        .cfi_restore \reg2
.endm

.macro EPILOG_RESTORE_REG_PAIR_INDEXED reg1, reg2, ofs
        ldp \reg1, \reg2, [sp], \ofs
        .cfi_restore \reg1
        .cfi_restore \reg2
        .cfi_adjust_cfa_offset -\ofs
.endm

.macro EPILOG_RETURN
        ret
.endm

.macro EPILOG_BRANCH Target
        b \Target
.endm

//-----------------------------------------------------------------------------
// The Following sets of SAVE_*_REGISTERS expect the memory to be reserved and
// base address to be passed in $reg
//

// Reserve 72 bytes of memory before calling  SAVE_ARGUMENT_REGISTERS
.macro SAVE_ARGUMENT_REGISTERS reg, ofs

        str                    x8, [\reg, #(\ofs)]
        stp                    x0, x1, [\reg, #(\ofs + 8)]
        stp                    x2, x3, [\reg, #(\ofs + 24)]
        stp                    x4, x5, [\reg, #(\ofs + 40)]
        stp                    x6, x7, [\reg, #(\ofs + 56)]

.endm

.macro RESTORE_ARGUMENT_REGISTERS reg, ofs

        ldr                    x8, [\reg, #(\ofs)]
        ldp                    x0, x1, [\reg, #(\ofs + 8)]
        ldp                    x2, x3, [\reg, #(\ofs + 24)]
        ldp                    x4, x5, [\reg, #(\ofs + 40)]
        ldp                    x6, x7, [\reg, #(\ofs + 56)]

.endm

// Reserve 128 bytes of memory before calling  SAVE_FLOAT_ARGUMENT_REGISTERS
.macro SAVE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        stp                    q0, q1, [\reg, #(\ofs)]
        stp                    q2, q3, [\reg, #(\ofs + 32)]
        stp                    q4, q5, [\reg, #(\ofs + 64)]
        stp                    q6, q7, [\reg, #(\ofs + 96)]

.endm

.macro RESTORE_FLOAT_ARGUMENT_REGISTERS reg, ofs

        ldp                    q0, q1, [\reg, #(\ofs)]
        ldp                    q2, q3, [\reg, #(\ofs + 32)]
        ldp                    q4, q5, [\reg, #(\ofs + 64)]
        ldp                    q6, q7, [\reg, #(\ofs + 96)]

.endm

.macro EPILOG_BRANCH_REG reg

        br \reg

.endm

//-----------------------------------------------------------------------------
// Macro used to check (in debug builds only) whether the stack is 16-bytes aligned (a requirement before calling
// out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
// before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
// is misaligned.
//
.macro  CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        add     x9, sp, xzr
        tst     x9, #15
        beq     0f
        EMIT_BREAKPOINT
0:
#endif
.endm

.macro  InterlockedOperationBarrier
    dmb ish
.endm
