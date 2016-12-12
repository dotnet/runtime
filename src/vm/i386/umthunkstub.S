// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// eax = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
    // Preserve argument registers
    push    ecx
    push    edx

    push    eax
    call    C_FUNC(TheUMEntryPrestubWorker)
    pop     edx

    // Restore argument registers
    pop     edx
    pop     ecx

    jmp     eax     // Tail Jmp
NESTED_END TheUMEntryPrestub, _TEXT


NESTED_ENTRY UMThunkStub, _TEXT, UnhandledExceptionHandlerUnix
    int     3       // implement here
    ret
NESTED_END UMThunkStub, _TEXT
