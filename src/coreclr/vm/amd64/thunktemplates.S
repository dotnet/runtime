// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

PAGE_SIZE = 4096

#define DATA_SLOT(stub, field) (stub##Code + PAGE_SIZE + stub##Data__##field)

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
        call   QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode, _TEXT

LEAF_ENTRY LookupStubCode, _TEXT
       push   QWORD PTR [rip + DATA_SLOT(LookupStub, DispatchToken)]
       jmp    QWORD PTR [rip + DATA_SLOT(LookupStub, ResolveWorkerTarget)]
LEAF_END_MARKED LookupStubCode, _TEXT

LEAF_ENTRY DispatchStubCode, _TEXT
       mov    rax,QWORD PTR [rip + DATA_SLOT(DispatchStub, ExpectedMT)]
PATCH_LABEL DispatchStubCode_ThisDeref
       cmp    QWORD PTR [rdi],rax;
       jne    LOCAL_LABEL(Fail)
       jmp    QWORD PTR [rip + DATA_SLOT(DispatchStub, ImplTarget)]
    LOCAL_LABEL(Fail):
       jmp    QWORD PTR [rip + DATA_SLOT(DispatchStub, FailTarget)]
LEAF_END_MARKED DispatchStubCode, _TEXT

LEAF_ENTRY ResolveStubCode, _TEXT
PATCH_LABEL ResolveStubCode_ResolveEntry
    LOCAL_LABEL(Resolve):
        push   rdx
        mov    r10,QWORD PTR [rip + DATA_SLOT(ResolveStub, CacheAddress)]
PATCH_LABEL ResolveStubCode_ThisDeref
        mov    rax,QWORD PTR [rdi]
        mov    rdx,rax
        shr    rax,12
        add    rax,rdx
        xor    eax,DWORD PTR [rip + DATA_SLOT(ResolveStub, HashedToken)]
        and    eax, CALL_STUB_CACHE_MASK_ASM * 8
        mov    rax,QWORD PTR [r10+rax*1]
        mov    r10,QWORD PTR [rip + DATA_SLOT(ResolveStub, Token)]
        cmp    rdx,QWORD PTR [rax]
        jne    LOCAL_LABEL(Miss)
        cmp    r10,QWORD PTR [rax+8]
        jne    LOCAL_LABEL(Miss)
        pop    rdx
        jmp    QWORD PTR [rax+0x10]
PATCH_LABEL ResolveStubCode_FailEntry
        add    DWORD PTR [rip + DATA_SLOT(ResolveStub, Counter)], -1
        jge    Resolve
        or     r11, 1 // SDF_ResolveBackPatch
PATCH_LABEL ResolveStubCode_SlowEntry
        push   rdx
        mov    r10,QWORD PTR [rip + DATA_SLOT(ResolveStub, Token)]
    LOCAL_LABEL(Miss):
        push   rax
        jmp    QWORD PTR [rip + DATA_SLOT(ResolveStub, ResolveWorkerTarget)]
LEAF_END_MARKED ResolveStubCode, _TEXT
