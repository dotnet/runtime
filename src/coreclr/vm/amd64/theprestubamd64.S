// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
        // Extra locals: 8 (Swift return buffer) + 384 (upper vector halves)
        PROLOG_WITH_TRANSITION_BLOCK 392, 0, 0, 0, 0
        mov             [rsp], rax // Return buffer in Swift calling convention

        SAVE_UPPER_VECTOR_REGISTERS 0x08

        //
        // call PreStubWorker
        //
        lea             rdi, [rsp + __PWTB_TransitionBlock]     // pTransitionBlock*
        mov             rsi, METHODDESC_REGISTER
        call            C_FUNC(PreStubWorker)
        mov             r10, rax

        mov             rax, [rsp]

        RESTORE_FLOAT_ARGUMENT_REGISTERS __PWTB_FloatArgumentRegisters
        RESTORE_UPPER_VECTOR_REGISTERS 0x08

        free_stack      __PWTB_StackAlloc
        POP_ARGUMENT_REGISTERS
        POP_CALLEE_SAVED_REGISTERS
        jmp             r10

NESTED_END ThePreStub, _TEXT

LEAF_ENTRY ThePreStubPatch, _TEXT
        // make sure that the basic block is unique
        test            eax,34
PATCH_LABEL ThePreStubPatchLabel
        ret
LEAF_END ThePreStubPatch, _TEXT
