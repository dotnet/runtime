// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.macro NESTED_ENTRY Name, Section, Handler
        LEAF_ENTRY \Name, \Section
        .ifnc \Handler, NoHandler
        .cfi_personality 0x1b, C_FUNC(\Handler) // 0x1b == DW_EH_PE_pcrel | DW_EH_PE_sdata4
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

.macro PREPARE_EXTERNAL_VAR Name, HelperReg
	std r2, -8(r1) 			// save r2 
	ld r2, C_FUNC(\Name)@got(r2) 	//load the address from got entry to r2 considering r2 has toc, relative to toc the got value will be calculated.
        ld \HelperReg, 0(r2)
	ld r2, -8(r1)			// restore r2
.endm

.macro EMIT_BREAKPOINT
    trap			// equivalent to: tw 31,0,0 
.endm

.macro EPILOG_STACK_FREE Size
        addi %r1, %r1, \Size
.endm

.macro PROLOG_SAVE_REG_PAIR_INDEXED reg1, reg2, ssize
// TODO POWERPC64
   bl abort
   nop
.endm
