// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// STUB_PAGE_SIZE must match the behavior of GetStubCodePageSize() on this architecture/os
STUB_PAGE_SIZE = 16384

#define DATA_SLOT(stub, field) C_FUNC(stub##Code) + STUB_PAGE_SIZE + stub##Data__##field

LEAF_ENTRY StubPrecodeCode, _TEXT
        lgrl %r0, DATA_SLOT(StubPrecode, MethodDesc)
        lgrl %r1, DATA_SLOT(StubPrecode, Target)
        br %r1
LEAF_END_MARKED StubPrecodeCode, _TEXT

LEAF_ENTRY FixupPrecodeCode, _TEXT
        lgrl %r1, DATA_SLOT(FixupPrecode, Target)
        br %r1
        lgrl %r0, DATA_SLOT(FixupPrecode, MethodDesc)
        lgrl %r1, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
        br %r1
LEAF_END_MARKED FixupPrecodeCode, _TEXT

LEAF_ENTRY CallCountingStubCode, _TEXT
        lghi %r0, -1
        lgrl %r1, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ah %r0, 0(%r1)
        sth %r0, 0(%r1)
        lgrl %r1, DATA_SLOT(CallCountingStub, TargetForMethod)
        bner %r1
        lgrl %r0, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        lgrl %r1, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
        br %r1
LEAF_END_MARKED CallCountingStubCode, _TEXT
