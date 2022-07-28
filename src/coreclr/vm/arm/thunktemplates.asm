; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm.h"
#include "asmconstants.h"
#include "asmmacros.h"


    TEXTAREA

    ALIGN 4

    #define DATA_SLOT(stub, field) stub##Code + PAGE_SIZE + stub##Data__##field

    LEAF_ENTRY StubPrecodeCode
        ldr r12, DATA_SLOT(StubPrecode, MethodDesc)
        ldr pc, DATA_SLOT(StubPrecode, Target)
    LEAF_END_MARKED StubPrecodeCode

    ALIGN 4

    LEAF_ENTRY FixupPrecodeCode
        ldr pc, DATA_SLOT(FixupPrecode, Target)
        ldr r12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr pc, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
    LEAF_END_MARKED FixupPrecodeCode

    ALIGN 4

    LEAF_ENTRY CallCountingStubCode
        push {r0}
        ldr r12, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh r0, [r12]
        subs r0, r0, #1
        strh r0, [r12]
        pop {r0}
        beq CountReachedZero
        ldr pc, DATA_SLOT(CallCountingStub, TargetForMethod)
CountReachedZero
        ldr pc, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
    LEAF_END_MARKED CallCountingStubCode

    END
