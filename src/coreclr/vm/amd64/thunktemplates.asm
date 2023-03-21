; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc


DATA_SLOT macro stub, field, page_size
    exitm @CatStr(stub, <Code + page_size + >, stub, <Data__>, field)
endm

STUB_PAGE_SIZE = 16384

LEAF_ENTRY StubPrecodeCode, _TEXT
        mov    r10, QWORD PTR [DATA_SLOT(StubPrecode, MethodDesc, STUB_PAGE_SIZE)]
        jmp    QWORD PTR [DATA_SLOT(StubPrecode, Target, STUB_PAGE_SIZE)]
LEAF_END_MARKED StubPrecodeCode, _TEXT

LEAF_ENTRY FixupPrecodeCode, _TEXT
        jmp QWORD PTR [DATA_SLOT(FixupPrecode, Target, STUB_PAGE_SIZE)]
        mov    r10, QWORD PTR [DATA_SLOT(FixupPrecode, MethodDesc, STUB_PAGE_SIZE)]
        jmp    QWORD PTR [DATA_SLOT(FixupPrecode, PrecodeFixupThunk, STUB_PAGE_SIZE)]
LEAF_END_MARKED FixupPrecodeCode, _TEXT

LEAF_ENTRY CallCountingStubCode, _TEXT
        mov    rax,QWORD PTR [DATA_SLOT(CallCountingStub, RemainingCallCountCell, STUB_PAGE_SIZE)]
        dec    WORD PTR [rax]
        je     CountReachedZero
        jmp    QWORD PTR [DATA_SLOT(CallCountingStub, TargetForMethod, STUB_PAGE_SIZE)]
    CountReachedZero:
        jmp    QWORD PTR [DATA_SLOT(CallCountingStub, TargetForThresholdReached, STUB_PAGE_SIZE)]
LEAF_END_MARKED CallCountingStubCode, _TEXT

        end
