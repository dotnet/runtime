// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

PAGE_SIZE = 4096

#define DATA_SLOT(stub, field) C_FUNC(stub##Code) + PAGE_SIZE + stub##Data__##field

LEAF_ENTRY StubPrecodeCode, _TEXT
        mov    r10, [rip + DATA_SLOT(StubPrecode, MethodDesc)]
        jmp    [rip + DATA_SLOT(StubPrecode, Target)]
LEAF_END_MARKED StubPrecodeCode, _TEXT

LEAF_ENTRY FixupPrecodeCode, _TEXT
        jmp    [rip + DATA_SLOT(FixupPrecode, Target)]
PATCH_LABEL FixupPrecodeCode_Fixup
        mov    r10, [rip + DATA_SLOT(FixupPrecode, MethodDesc)]
        jmp    [rip + DATA_SLOT(FixupPrecode, PrecodeFixupThunk)]
LEAF_END_MARKED FixupPrecodeCode, _TEXT

LEAF_ENTRY CallCountingStubCode, _TEXT
        mov    rax,QWORD PTR [rip + DATA_SLOT(CallCountingStub, RemainingCallCountCell)]
        dec    WORD PTR [rax]
        je     LOCAL_LABEL(CountReachedZero)
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForMethod)]
    LOCAL_LABEL(CountReachedZero):
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode, _TEXT
