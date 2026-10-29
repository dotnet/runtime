// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Prologue of all funclet calling helpers
//
.macro FUNCLET_CALL_PROLOGUE localsCount, alignStack

    push_nonvol_reg r15     // save preserved regs for OS stackwalker
    push_nonvol_reg r14     // ...
    push_nonvol_reg r13     // ...
    push_nonvol_reg r12     // ...
    push_nonvol_reg rbx     // ...
    push_nonvol_reg rbp     // ...

    stack_alloc_size = \localsCount * 8 + \alignStack * 8

    alloc_stack     stack_alloc_size

    END_PROLOGUE
.endm

//
// Epilogue of all funclet calling helpers
//
.macro FUNCLET_CALL_EPILOGUE
    free_stack      stack_alloc_size

    pop_nonvol_reg rbp
    pop_nonvol_reg rbx
    pop_nonvol_reg r12
    pop_nonvol_reg r13
    pop_nonvol_reg r14
    pop_nonvol_reg r15
.endm
