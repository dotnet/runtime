// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY StubPrecodeCode
    fence r,rw
    auipc t1, 0x4
    ld t2, (StubPrecodeData__SecretParam - 0x4)(t1)
    ld t1, (StubPrecodeData__Target - 0x4)(t1)
    jr t1
LEAF_END_MARKED StubPrecodeCode

LEAF_ENTRY FixupPrecodeCode
    auipc t2, 0x4
    ld t2, (FixupPrecodeData__Target)(t2)
    c.jr t2

    fence r,rw
    auipc t2, 0x4
    ld t1, (FixupPrecodeData__PrecodeFixupThunk - 0xe)(t2)
    ld t2, (FixupPrecodeData__MethodDesc - 0xe)(t2)
    jr t1
LEAF_END_MARKED FixupPrecodeCode

LEAF_ENTRY CallCountingStubCode
    fence r,rw
    auipc t2, 0x4
    ld t3, (CallCountingStubData__RemainingCallCountCell - 0x4)(t2)
    lh t1, 0(t3)
    addiw t1, t1, -1
    sh t1, 0(t3)
    beq t1, zero, LOCAL_LABEL(CountReachedZero)
    ld t1, (CallCountingStubData__TargetForMethod - 0x4)(t2)
    c.jr t1
LOCAL_LABEL(CountReachedZero):
    ld t1, (CallCountingStubData__TargetForThresholdReached - 0x4)(t2)
    c.jr t1
LEAF_END_MARKED CallCountingStubCode
