// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    add     r0, sp, #__PWTB_TransitionBlock // TransitionBlock *
    mov     r1, r12 // stub-identifying token
    bl      C_FUNC(OnCallCountThresholdReached)
    mov     r12, r0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG r12
NESTED_END OnCallCountThresholdReachedStub, _TEXT
