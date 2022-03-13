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

.macro LEAF_ENTRY Name, Section
        .global C_FUNC(\Name)
        .type \Name, %function
C_FUNC(\Name):
        .cfi_startproc
.endm

.macro PATCH_LABEL Name
        .global C_FUNC(\Name)
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
.endm

.macro PROLOG_BEG
        push ebp
        .cfi_def_cfa_offset 8
        .cfi_offset ebp, -8
        mov ebp, esp
.endm

.macro PROLOG_PUSH Reg
        push \Reg
        .cfi_adjust_cfa_offset 4
        .cfi_rel_offset \Reg, 0
.endm

.macro PROLOG_END
        .cfi_def_cfa_register ebp
        .cfi_def_cfa_offset 8
.endm

.macro EPILOG_BEG
.endm

.macro EPILOG_POP Reg
        pop \Reg
        .cfi_restore \Reg
.endm

.macro EPILOG_END
        pop ebp
.endm

.macro ESP_PROLOG_BEG
.endm

.macro ESP_PROLOG_PUSH Reg
        PROLOG_PUSH \Reg
.endm

.macro ESP_PROLOG_ALLOC Size
        sub esp, \Size
        .cfi_adjust_cfa_offset \Size
.endm

.macro ESP_PROLOG_END
        .cfi_def_cfa_register esp
.endm

.macro ESP_EPILOG_BEG
.endm

.macro ESP_EPILOG_POP Reg
        EPILOG_POP \Reg
.endm

.macro ESP_EPILOG_FREE Size
        add esp, \Size
        .cfi_adjust_cfa_offset -\Size
.endm

.macro ESP_EPILOG_END
.endm

.macro PREPARE_EXTERNAL_VAR Name, Reg
.att_syntax
    call  0f
0:
    popl  %\Reg
1:
    addl  $_GLOBAL_OFFSET_TABLE_ + (1b - 0b), %\Reg
    movl  C_FUNC(\Name)@GOT(%\Reg), %\Reg
.intel_syntax noprefix
.endm

.macro CHECK_STACK_ALIGNMENT
#ifdef _DEBUG
    test    esp, 0Fh
    je      0f
    int3
0:
#endif // _DEBUG
.endm
