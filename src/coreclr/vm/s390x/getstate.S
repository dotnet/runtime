// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY GetCurrentSP, _TEXT

        lgr %r2, %r15
        br %r14

LEAF_END GetCurrentSP, _TEXT


LEAF_ENTRY GetCurrentIP, _TEXT

        lgr %r2, %r14
        br %r14

LEAF_END GetCurrentIP, _TEXT


// EXTERN_C void LazyMachStateCaptureState(struct LazyMachState *pState)
LEAF_ENTRY LazyMachStateCaptureState, _TEXT

        stmg    %r6, %r13, OFFSETOF__MachState__m_Capture(%r2)
        stg     %r14, OFFSETOF__LazyMachState__m_CaptureRip(%r2)
        stg     %r15, OFFSETOF__LazyMachState__m_CaptureRsp(%r2)

        lghi    %r0, 0
        stg     %r0, OFFSETOF__MachState___pRetAddr(%r2)

        br      %r14

LEAF_END LazyMachStateCaptureState, _TEXT
