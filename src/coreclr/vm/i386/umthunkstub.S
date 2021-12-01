// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// eax = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
#define STK_ALIGN_PADDING 8
    sub     esp, STK_ALIGN_PADDING
    push    eax  // UMEntryThunk*
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(TheUMEntryPrestubWorker)
    add     esp, (4 + STK_ALIGN_PADDING)

    // eax = PCODE
    jmp     eax     // Tail Jmp
#undef STK_ALIGN_PADDING
NESTED_END TheUMEntryPrestub, _TEXT
