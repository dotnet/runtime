; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

; STUB_PAGE_SIZE must match the behavior of GetStubCodePageSize() on this architecture/os
#define STUB_PAGE_SIZE 16384

#define DATA_SLOT(stub, field) (stub##Code + STUB_PAGE_SIZE + stub##Data__##field)
#define XIP_RELATIVE_DATA_SLOT(stub, field, offset) (STUB_PAGE_SIZE + stub##Data__##field - offset)

    LEAF_ENTRY StubPrecodeCode
        dmb ishld
        ldr x10, DATA_SLOT(StubPrecode, Target)
        ldr x12, DATA_SLOT(StubPrecode, SecretParam)
        br x10
    LEAF_END_MARKED StubPrecodeCode

    LEAF_ENTRY FixupPrecodeCode
        ldr xip0, DATA_SLOT(FixupPrecode, Target)
        br  xip0
        ldr x12, [xip0, XIP_RELATIVE_DATA_SLOT(FixupPrecode, MethodDesc, 8)]
        ldr x11, [xip0, XIP_RELATIVE_DATA_SLOT(FixupPrecode, PrecodeFixupThunk, 8)]
        br  x11        
    LEAF_END_MARKED FixupPrecodeCode

    LEAF_ENTRY CallCountingStubCode
        dmb ishld
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
