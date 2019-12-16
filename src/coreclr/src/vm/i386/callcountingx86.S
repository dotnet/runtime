// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY OnCallCountThresholdReachedStub, _TEXT
    // Pop the return address (the stub-identifying token) into a non-argument volatile register that can be trashed
    pop     eax
    jmp     C_FUNC(OnCallCountThresholdReachedStub2)
LEAF_END OnCallCountThresholdReachedStub, _TEXT

NESTED_ENTRY OnCallCountThresholdReachedStub2, _TEXT, NoHandler
    STUB_PROLOG

    mov     esi, esp

    // Align the stack for the call
    lea     ebx, [esp - 8]
    and     ebx, 0fh
    sub     esp, ebx

    push    eax // stub-identifying token, see OnCallCountThresholdReachedStub
    push    esi // TransitionBlock *
    CHECK_STACK_ALIGNMENT
    call    C_FUNC(OnCallCountThresholdReached)

    mov     esp, esi

    STUB_EPILOG
    jmp     eax

    // This will never be executed. It is just to help out stack-walking logic
    // which disassembles the epilog to unwind the stack.
    ret
NESTED_END OnCallCountThresholdReachedStub2, _TEXT
