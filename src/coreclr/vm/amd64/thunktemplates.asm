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
        call   QWORD PTR [DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode, _TEXT

LEAF_ENTRY LookupStubCode, _TEXT
       push   QWORD PTR [DATA_SLOT(LookupStub, DispatchToken)]
       jmp    QWORD PTR [DATA_SLOT(LookupStub, ResolveWorkerTarget)]
LEAF_END_MARKED LookupStubCode, _TEXT

LEAF_ENTRY DispatchStubCode, _TEXT
       mov    rax,QWORD PTR [DATA_SLOT(DispatchStub, ExpectedMT)]
PATCH_LABEL DispatchStubCode_ThisDeref
       cmp    QWORD PTR [rcx],rax;
       jne    Fail
       jmp    QWORD PTR [DATA_SLOT(DispatchStub, ImplTarget)]
    Fail:
       jmp    QWORD PTR [DATA_SLOT(DispatchStub, FailTarget)]
LEAF_END_MARKED DispatchStubCode, _TEXT

LEAF_ENTRY ResolveStubCode, _TEXT
PATCH_LABEL ResolveStubCode_ResolveEntry
        push   rdx
        mov    r10,QWORD PTR [DATA_SLOT(ResolveStub, CacheAddress)]
PATCH_LABEL ResolveStubCode_ThisDeref
        mov    rax,QWORD PTR [rcx]
        mov    rdx,rax
        shr    rax,12
        add    rax,rdx
        xor    eax,DWORD PTR [DATA_SLOT(ResolveStub, HashedToken)]
        and    eax, CALL_STUB_CACHE_MASK_ASM * 8
        mov    rax,QWORD PTR [r10+rax*1]
        mov    r10,QWORD PTR [DATA_SLOT(ResolveStub, Token)]
        cmp    rdx,QWORD PTR [rax]
        jne    Miss
        cmp    r10,QWORD PTR [rax+8]
        jne    Miss
        pop    rdx
        jmp    QWORD PTR [rax+10h]
PATCH_LABEL ResolveStubCode_FailEntry
        add    DWORD PTR [DATA_SLOT(ResolveStub, Counter)], -1
        jge    ResolveStubCode
        or     r11, 1; SDF_ResolveBackPatch
PATCH_LABEL ResolveStubCode_SlowEntry
        push   rdx
        mov    r10,QWORD PTR [DATA_SLOT(ResolveStub, Token)]
Miss:
        push   rax
        jmp    QWORD PTR [DATA_SLOT(ResolveStub, ResolveWorkerTarget)]
LEAF_END_MARKED ResolveStubCode, _TEXT

        end
