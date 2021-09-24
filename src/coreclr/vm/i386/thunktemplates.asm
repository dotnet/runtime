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

PAGE_SIZE EQU 4096

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
        call   dword ptr  DATA_SLOT(CallCountingStub, TargetForThresholdReached)
SLOT_ADDRESS_PATCH_LABEL CallCountingStub, TargetForThresholdReached
        int    3
LEAF_END_MARKED _CallCountingStubCode@0

LEAF_ENTRY _LookupStubCode@0
        push   eax
        push   dword ptr DATA_SLOT(LookupStub, DispatchToken)
SLOT_ADDRESS_PATCH_LABEL LookupStub, DispatchToken
        jmp    dword ptr DATA_SLOT(LookupStub, ResolveWorkerTarget)
SLOT_ADDRESS_PATCH_LABEL LookupStub, ResolveWorkerTarget
LEAF_END_MARKED _LookupStubCode@0

LEAF_ENTRY _DispatchStubCode@0
        push   eax
        mov    eax, dword ptr DATA_SLOT(DispatchStub, ExpectedMT)
SLOT_ADDRESS_PATCH_LABEL DispatchStub, ExpectedMT
PATCH_LABEL _DispatchStubCode_ThisDeref@0
        cmp    dword ptr [ecx],eax
        pop    eax
        jne    NoMatch
        jmp    dword ptr DATA_SLOT(DispatchStub, ImplTarget)
SLOT_ADDRESS_PATCH_LABEL DispatchStub, ImplTarget
NoMatch:
        jmp    dword ptr DATA_SLOT(DispatchStub, FailTarget)
SLOT_ADDRESS_PATCH_LABEL DispatchStub, FailTarget
LEAF_END_MARKED _DispatchStubCode@0

LEAF_ENTRY _ResolveStubCode@0
_ResolveStubCode_FailEntry@0:
PUBLIC _ResolveStubCode_FailEntry@0
        sub dword ptr DATA_SLOT(ResolveStub, Counter), 1
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Counter, -5
        jl Backpatcher
PATCH_LABEL _ResolveStubCode_ResolveEntry@0
        push    eax
PATCH_LABEL _ResolveStubCode_ThisDeref@0
        mov     eax,dword ptr [ecx]
        push    edx
        mov     edx,eax
        shr     eax, 12
        add     eax,edx
        xor     eax,dword ptr DATA_SLOT(ResolveStub, HashedToken)
SLOT_ADDRESS_PATCH_LABEL ResolveStub, HashedToken
        and     eax,CALL_STUB_CACHE_MASK_ASM * 4
        add     eax,dword ptr DATA_SLOT(ResolveStub, CacheAddress)
SLOT_ADDRESS_PATCH_LABEL ResolveStub, CacheAddress
        mov     eax,dword ptr [eax]
        cmp     edx,dword ptr [eax]
        jne     Miss
        mov     edx,dword ptr DATA_SLOT(ResolveStub, Token)
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Token,, 1
        cmp     edx,dword ptr [eax + 4]
        jne     Miss
        mov     eax,dword ptr [eax + 8]
        pop     edx
        add     esp, 4
        jmp     eax
Miss:
        pop     edx
Slow:
        push    dword ptr DATA_SLOT(ResolveStub, Token)
SLOT_ADDRESS_PATCH_LABEL ResolveStub, Token,, 2
        jmp     dword ptr DATA_SLOT(ResolveStub, ResolveWorkerTarget); <<< resolveWorker == ResolveWorkerChainLookupAsmStub or ResolveWorkerAsmStub
SLOT_ADDRESS_PATCH_LABEL ResolveStub, ResolveWorkerTarget
Backpatcher:
        call    dword ptr DATA_SLOT(ResolveStub, PatcherTarget); <<< backpatcherWorker == BackPatchWorkerAsmStub
SLOT_ADDRESS_PATCH_LABEL ResolveStub, PatcherTarget
        jmp     _ResolveStubCode_ResolveEntry@0
LEAF_END_MARKED _ResolveStubCode@0

        end