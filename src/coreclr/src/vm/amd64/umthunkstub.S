// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// METHODDESC_REGISTER: UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
    PUSH_ARGUMENT_REGISTERS
    // +8 for alignment
    alloc_stack (SIZEOF_MAX_FP_ARG_SPILL + 8)
    SAVE_FLOAT_ARGUMENT_REGISTERS 0
    END_PROLOGUE

    mov rdi, METHODDESC_REGISTER
    call C_FUNC(TheUMEntryPrestubWorker)

    // we're going to tail call to the exec stub that we just setup

    RESTORE_FLOAT_ARGUMENT_REGISTERS 0
    free_stack (SIZEOF_MAX_FP_ARG_SPILL + 8)
    POP_ARGUMENT_REGISTERS
    TAILJMP_RAX

NESTED_END TheUMEntryPrestub, _TEXT
