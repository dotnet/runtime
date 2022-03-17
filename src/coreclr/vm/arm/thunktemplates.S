// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

    .align 4

PAGE_SIZE = 4096

#define DATA_SLOT(stub, field) stub##Code + PAGE_SIZE + stub##Data__##field

    LEAF_ENTRY StubPrecodeCode
        ldr r12, DATA_SLOT(StubPrecode, MethodDesc)
        ldr pc, DATA_SLOT(StubPrecode, Target)
    LEAF_END_MARKED StubPrecodeCode

    .align 4

    LEAF_ENTRY FixupPrecodeCode
        ldr pc, DATA_SLOT(FixupPrecode, Target)
        ldr r12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr pc, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
    LEAF_END_MARKED FixupPrecodeCode

    .align 4

    LEAF_ENTRY CallCountingStubCode
        push {r0}
        ldr r12, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh r0, [r12]
        subs r0, r0, #1
        strh r0, [r12]
        pop {r0}
        beq LOCAL_LABEL(CountReachedZero)
        ldr pc, DATA_SLOT(CallCountingStub, TargetForMethod)
LOCAL_LABEL(CountReachedZero):
        ldr pc, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
    LEAF_END_MARKED CallCountingStubCode
