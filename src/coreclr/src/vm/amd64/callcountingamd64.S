// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY OnCallCountThresholdReachedStub, _TEXT
    // Pop the return address (the stub-identifying token) into a non-argument volatile register that can be trashed
    pop     rax
    jmp     C_FUNC(OnCallCountThresholdReachedStub2)
LEAF_END OnCallCountThresholdReachedStub, _TEXT

NESTED_ENTRY OnCallCountThresholdReachedStub2, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    lea     rdi, [rsp + __PWTB_TransitionBlock] // TransitionBlock *
    mov     rsi, rax // stub-identifying token, see OnCallCountThresholdReachedStub
    call    C_FUNC(OnCallCountThresholdReached)

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    TAILJMP_RAX
NESTED_END OnCallCountThresholdReachedStub2, _TEXT
