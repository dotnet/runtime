; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

#define DATA_SLOT(stub, field) (stub##Code + PAGE_SIZE + stub##Data__##field)

    LEAF_ENTRY StubPrecodeCode
        ldr x10, DATA_SLOT(StubPrecode, Target)
        ldr x12, DATA_SLOT(StubPrecode, MethodDesc)
        br x10
    LEAF_END_MARKED StubPrecodeCode

    LEAF_ENTRY FixupPrecodeCode
        ldr x11, DATA_SLOT(FixupPrecode, Target)
        br  x11
        ldr x12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr x11, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
        br  x11        
    LEAF_END_MARKED FixupPrecodeCode

    LEAF_ENTRY CallCountingStubCode
        ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh w10, [x9]
        subs w10, w10, #1
        strh w10, [x9]
        beq CountReachedZero
        ldr  x9, DATA_SLOT(CallCountingStub, TargetForMethod)
        br   x9
CountReachedZero        
        ldr  x10, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
        br   x10
    LEAF_END_MARKED CallCountingStubCode

    END
