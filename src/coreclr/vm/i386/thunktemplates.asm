; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat

include <AsmMacros.inc>
include AsmConstants.inc

        option  casemap:none
        .code

.686P
.XMM

PAGE_SIZE EQU 16384

DATA_SLOT macro stub, field
    exitm @CatStr(<_>, stub, <Code@0 + PAGE_SIZE + >, stub, <Data__>, field)
endm

SLOT_ADDRESS_PATCH_LABEL macro stub, field, offset:=<-4>, index:=<>
    LOCAL labelName, labelValue
labelName TEXTEQU @CatStr(<_>, stub, <Code_>, field, <_Offset>, index)
labelValue TEXTEQU @CatStr(<$>, offset, <-_>, stub, <Code@0>)
    %labelName EQU labelValue
    PUBLIC labelName
endm

LEAF_ENTRY _StubPrecodeCode@0
        mov     eax, dword ptr DATA_SLOT(StubPrecode, MethodDesc)
SLOT_ADDRESS_PATCH_LABEL StubPrecode, MethodDesc
        jmp     dword ptr DATA_SLOT(StubPrecode, Target)
SLOT_ADDRESS_PATCH_LABEL StubPrecode, Target
LEAF_END_MARKED _StubPrecodeCode@0

EXTERN _ThePreStub@0:PROC

LEAF_ENTRY _FixupPrecodeCode@0
        jmp     dword ptr DATA_SLOT(FixupPrecode, Target)
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, Target
        mov     eax, dword ptr DATA_SLOT(FixupPrecode, MethodDesc)
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, MethodDesc
        jmp     dword ptr DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
SLOT_ADDRESS_PATCH_LABEL FixupPrecode, PrecodeFixupThunk
LEAF_END_MARKED _FixupPrecodeCode@0

LEAF_ENTRY _CallCountingStubCode@0
        mov    eax, dword ptr DATA_SLOT(CallCountingStub, RemainingCallCountCell)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, RemainingCallCountCell
        dec    WORD PTR [eax]
        je     CountReachedZero
        jmp    dword ptr  DATA_SLOT(CallCountingStub, TargetForMethod)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, TargetForMethod
CountReachedZero:
        jmp    dword ptr  DATA_SLOT(CallCountingStub, TargetForThresholdReached)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, TargetForThresholdReached
LEAF_END_MARKED _CallCountingStubCode@0

        end