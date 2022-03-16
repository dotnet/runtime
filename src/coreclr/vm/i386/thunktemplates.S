// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

PAGE_SIZE = 4096

//#define DATA_SLOT(stub, field) stub##Code + PAGE_SIZE + stub##Data__##field
#define DATA_SLOT(stub, field) PAGE_SIZE + stub##Data__##field

.macro INDJMP target
    .att_syntax
    jmp *\target
    .intel_syntax noprefix
.endm

.macro INDCALL target
    .att_syntax
    call *\target
    .intel_syntax noprefix
.endm

.macro SLOT_ADDRESS_PATCH_LABEL stub, field, offset=-4, index=""
    C_FUNC(\stub\()Code_\field\()_Offset\index) = .\offset-\stub\()Code
    .global C_FUNC(\stub\()Code_\field\()_Offset\index)
.endm

LEAF_ENTRY StubPrecodeCode
        mov     eax, dword ptr [DATA_SLOT(StubPrecode, MethodDesc)]
SLOT_ADDRESS_PATCH_LABEL StubPrecode, MethodDesc
        INDJMP DATA_SLOT(StubPrecode, Target)
SLOT_ADDRESS_PATCH_LABEL StubPrecode, Target
        nop
LEAF_END_MARKED StubPrecodeCode

LEAF_ENTRY FixupPrecodeCode
        INDJMP  DATA_SLOT(FixupPrecode, Target)
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, Target
        mov     eax, dword ptr [DATA_SLOT(FixupPrecode, MethodDesc)]
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, MethodDesc
        INDJMP  DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, PrecodeFixupThunk
LEAF_END_MARKED FixupPrecodeCode

LEAF_ENTRY CallCountingStubCode
        mov    eax, dword ptr [DATA_SLOT(CallCountingStub, RemainingCallCountCell)]
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, RemainingCallCountCell
        dec    WORD PTR [eax]
        je     LOCAL_LABEL(CountReachedZero)
        INDJMP DATA_SLOT(CallCountingStub, TargetForMethod)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, TargetForMethod
LOCAL_LABEL(CountReachedZero):
        INDJMP DATA_SLOT(CallCountingStub, TargetForThresholdReached)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, TargetForThresholdReached
LEAF_END_MARKED CallCountingStubCode
