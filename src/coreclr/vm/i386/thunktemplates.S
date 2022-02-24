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

LEAF_ENTRY LookupStubCode
        push   eax
        push   dword ptr [DATA_SLOT(LookupStub, DispatchToken)]
SLOT_ADDRESS_PATCH_LABEL LookupStub, DispatchToken
        INDJMP DATA_SLOT(LookupStub, ResolveWorkerTarget)
SLOT_ADDRESS_PATCH_LABEL LookupStub, ResolveWorkerTarget
LEAF_END_MARKED LookupStubCode

LEAF_ENTRY DispatchStubCode
        push   eax
        mov    eax, dword ptr [DATA_SLOT(DispatchStub, ExpectedMT)]
SLOT_ADDRESS_PATCH_LABEL DispatchStub, ExpectedMT
PATCH_LABEL _DispatchStubCode_ThisDeref
        cmp    dword ptr [ecx],eax
        pop    eax
        jne    NoMatch
        INDJMP DATA_SLOT(DispatchStub, ImplTarget)
SLOT_ADDRESS_PATCH_LABEL DispatchStub, ImplTarget
NoMatch:
        INDJMP DATA_SLOT(DispatchStub, FailTarget)
SLOT_ADDRESS_PATCH_LABEL DispatchStub, FailTarget
LEAF_END_MARKED DispatchStubCode

LEAF_ENTRY ResolveStubCode
PATCH_LABEL ResolveStubCode_FailEntry
        sub dword ptr [DATA_SLOT(ResolveStub, Counter)], 1
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Counter, -5
        jl LOCAL_LABEL(Backpatcher)
PATCH_LABEL ResolveStubCode_ResolveEntry
LOCAL_LABEL(ResolveEntry):
        push    eax
PATCH_LABEL ResolveStubCode_ThisDeref
        mov     eax,dword ptr [ecx]
        push    edx
        mov     edx,eax
        shr     eax, 12
        add     eax,edx
        xor     eax,dword ptr [DATA_SLOT(ResolveStub, HashedToken)]
SLOT_ADDRESS_PATCH_LABEL ResolveStub, HashedToken
        and     eax,CALL_STUB_CACHE_MASK_ASM * 4
        add     eax,dword ptr [DATA_SLOT(ResolveStub, CacheAddress)]
SLOT_ADDRESS_PATCH_LABEL ResolveStub, CacheAddress
        mov     eax,dword ptr [eax]
        cmp     edx,dword ptr [eax]
        jne     LOCAL_LABEL(Miss)
        mov     edx,dword ptr [DATA_SLOT(ResolveStub, Token)]
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Token,, 1
        cmp     edx,dword ptr [eax + 4]
        jne     LOCAL_LABEL(Miss)
        mov     eax,dword ptr [eax + 8]
        pop     edx
        add     esp, 4
        jmp     eax
LOCAL_LABEL(Miss):
        pop     edx
        push    dword ptr [DATA_SLOT(ResolveStub, Token)]
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Token,, 2
        INDJMP DATA_SLOT(ResolveStub, ResolveWorkerTarget) // <<< resolveWorker == ResolveWorkerChainLookupAsmStub or ResolveWorkerAsmStub
SLOT_ADDRESS_PATCH_LABEL ResolveStub, ResolveWorkerTarget
LOCAL_LABEL(Backpatcher):
        INDCALL DATA_SLOT(ResolveStub, PatcherTarget) // <<< backpatcherWorker == BackPatchWorkerAsmStub
SLOT_ADDRESS_PATCH_LABEL ResolveStub, PatcherTarget
        jmp     LOCAL_LABEL(ResolveEntry)
LEAF_END_MARKED ResolveStubCode

