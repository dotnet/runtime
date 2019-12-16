// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "asmconstants.h"
#include "unixasmmacros.inc"

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    add     x0, sp, #__PWTB_TransitionBlock // TransitionBlock *
    mov     x1, x10 // stub-identifying token
    bl      C_FUNC(OnCallCountThresholdReached)
    mov     x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG x9
NESTED_END OnCallCountThresholdReachedStub, _TEXT
