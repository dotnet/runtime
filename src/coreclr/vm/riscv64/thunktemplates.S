// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY StubPrecodeCode
    auipc t1, 0x4
    ld t2, (StubPrecodeData__MethodDesc)(t1)
    ld t1, (StubPrecodeData__Target)(t1)
    jr t1
LEAF_END_MARKED StubPrecodeCode

LEAF_ENTRY FixupPrecodeCode
    auipc t2, 0x4
    ld t2, (FixupPrecodeData__Target)(t2)
    c.jr t2

    auipc t2, 0x4
    ld t1, (FixupPrecodeData__PrecodeFixupThunk - 0xa)(t2)
    ld t2, (FixupPrecodeData__MethodDesc - 0xa)(t2)
    jr t1
LEAF_END_MARKED FixupPrecodeCode

LEAF_ENTRY CallCountingStubCode
    auipc t2, 0x4
    ld t3, (CallCountingStubData__RemainingCallCountCell)(t2)
    lh t1, 0(t3)
    addiw t1, t1, -1
    sh t1, 0(t3)
    beq t1, zero, LOCAL_LABEL(CountReachedZero)
    ld t1, (CallCountingStubData__TargetForMethod)(t2)
    jr t1
LOCAL_LABEL(CountReachedZero):
    ld t1, (CallCountingStubData__TargetForThresholdReached)(t2)
    jr t1
LEAF_END_MARKED CallCountingStubCode
