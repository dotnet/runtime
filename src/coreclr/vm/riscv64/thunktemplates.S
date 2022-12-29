// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

LEAF_ENTRY StubPrecodeCode
    auipc t2, 0x1
    addi t1, t2, 0x4 // 4, for Type encoding.
    ld t2,(StubPrecodeData__MethodDesc - 0x4)(t1)
    ld t1,(StubPrecodeData__Target - 0x4)(t1)
    jalr x0,t1,0
LEAF_END_MARKED StubPrecodeCode

LEAF_ENTRY FixupPrecodeCode
    auipc t2, 0x1
    addi t1, t2, 0x3 // 3, for Type encoding.
    ld t2, (FixupPrecodeData__Target - 3)(t1)
    c.jr t2

    auipc t2, 0x1
    addi t1, t2, 0x3 // 3, for Type encoding.
    ld t2, (FixupPrecodeData__MethodDesc - 0x3 - 0xe)(t1)
    ld t1, (FixupPrecodeData__PrecodeFixupThunk - 0x3 - 0xe)(t1)
    jalr x0, t1, 0
LEAF_END_MARKED FixupPrecodeCode

LEAF_ENTRY CallCountingStubCode
    auipc t2, 0x1
    ld t3, (CallCountingStubData__RemainingCallCountCell)(t2)
    lh t1, 0(t3)
    addiw t1, t1, -1
    sh t1, 0(t3)
    beq t1, zero, LOCAL_LABEL(CountReachedZero)
    ld t1, (CallCountingStubData__TargetForMethod)(t2)
    jalr x0, 0(t1)
LOCAL_LABEL(CountReachedZero):
    ld t1, (CallCountingStubData__TargetForThresholdReached)(t2)
    jalr x0,t1,0
LEAF_END_MARKED CallCountingStubCode
