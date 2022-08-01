; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

PAGE_SIZE = 4096

DATA_SLOT macro stub, field
    exitm @CatStr(stub, <Code + PAGE_SIZE + >, stub, <Data__>, field)
endm

LEAF_ENTRY StubPrecodeCode, _TEXT
        mov    r10, QWORD PTR [DATA_SLOT(StubPrecode, MethodDesc)]
        jmp    QWORD PTR [DATA_SLOT(StubPrecode, Target)]
LEAF_END_MARKED StubPrecodeCode, _TEXT

LEAF_ENTRY FixupPrecodeCode, _TEXT
        jmp QWORD PTR [DATA_SLOT(FixupPrecode, Target)]
PATCH_LABEL FixupPrecodeCode_Fixup
        mov    r10, QWORD PTR [DATA_SLOT(FixupPrecode, MethodDesc)]
        jmp    QWORD PTR [DATA_SLOT(FixupPrecode, PrecodeFixupThunk)]
LEAF_END_MARKED FixupPrecodeCode, _TEXT

LEAF_ENTRY CallCountingStubCode, _TEXT
        mov    rax,QWORD PTR [DATA_SLOT(CallCountingStub, RemainingCallCountCell)]
        dec    WORD PTR [rax]
        je     CountReachedZero
        jmp    QWORD PTR [DATA_SLOT(CallCountingStub, TargetForMethod)]
    CountReachedZero:
        jmp    QWORD PTR [DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode, _TEXT

        end
