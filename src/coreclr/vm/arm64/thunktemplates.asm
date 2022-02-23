; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

#define DATA_SLOT(stub, field) (stub##Code + PAGE_SIZE + stub##Data__##field)

    LEAF_ENTRY StubPrecodeCode
        ldr x10, DATA_SLOT(StubPrecode, Target)
        ldr x12, DATA_SLOT(StubPrecode, MethodDesc)
        br x10
    LEAF_END_MARKED StubPrecodeCode

    LEAF_ENTRY FixupPrecodeCode
        ldr x11, DATA_SLOT(FixupPrecode, Target)
        br  x11
        ldr x12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr x11, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
        br  x11        
    LEAF_END_MARKED FixupPrecodeCode

    LEAF_ENTRY CallCountingStubCode
        ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh w10, [x9]
        subs w10, w10, #1
        strh w10, [x9]
        beq CountReachedZero
        ldr  x9, DATA_SLOT(CallCountingStub, TargetForMethod)
        br   x9
CountReachedZero        
        adr  x10, CallCountingStubCode
        ldr  x9, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
        br   x9
    LEAF_END_MARKED CallCountingStubCode


    LEAF_ENTRY LookupStubCode
        ldr x12, DATA_SLOT(LookupStub, DispatchToken)
        ldr x10, DATA_SLOT(LookupStub, ResolveWorkerTarget)
        br x10
    LEAF_END_MARKED LookupStubCode

    LEAF_ENTRY DispatchStubCode
    PATCH_LABEL DispatchStubCode_ThisDeref
        ldr x13, [x0] ; methodTable from object in x0
        adr x9, DATA_SLOT(DispatchStub, ExpectedMT)
        ldp x10, x12, [x9] ; x10 = ExpectedMT & x12 = ImplTarget
        cmp x13, x10
        bne Fail
        br x12
Fail
        ldr x9, DATA_SLOT(DispatchStub, FailTarget)
        br x9
    LEAF_END_MARKED DispatchStubCode

    LEAF_ENTRY ResolveStubCode
    PATCH_LABEL ResolveStubCode_ResolveEntry
    PATCH_LABEL ResolveStubCode_ThisDeref
        ldr x12, [x0]
        add x9, x12, x12, lsr #12
        ldr w13, DATA_SLOT(ResolveStub, HashedToken)
        eor x9, x9, x13
        and x9, x9, #CALL_STUB_CACHE_MASK_ASM * 8
        ldr x13, DATA_SLOT(ResolveStub, CacheAddress)
        ldr x9, [x13, x9]
        ldr x15, DATA_SLOT(ResolveStub, Token)
        ldr x13, [x9, #ResolveCacheElem__pMT]
        cmp x12, x13
        bne ResolveStubCode_SlowEntry
        ldr x13, [x9, #ResolveCacheElem__token]
        cmp x15, x13
        bne ResolveStubCode_SlowEntry
        ldr x12, [x9, ResolveCacheElem__target]
        br x12
    PATCH_LABEL ResolveStubCode_SlowEntry
        ldr x12, DATA_SLOT(ResolveStub, Token)
        ldr x13, DATA_SLOT(ResolveStub, ResolveWorkerTarget)
        br x13
    PATCH_LABEL ResolveStubCode_FailEntry
        adr x10, DATA_SLOT(ResolveStub, Counter)
        ldr w9, [x10]
        subs w9, w9, #1
        str w9, [x10]
        bge ResolveStubCode
        orr x11, x11, #1; SDF_ResolveBackPatch
        b ResolveStubCode    
    LEAF_END_MARKED ResolveStubCode

    END
