; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm.h"
#include "asmconstants.h"
#include "asmmacros.h"


    TEXTAREA

    ALIGN 4

    #define DATA_SLOT(stub, field) stub##Code + PAGE_SIZE + stub##Data__##field

    LEAF_ENTRY StubPrecodeCode
        ldr r12, DATA_SLOT(StubPrecode, MethodDesc)
        ldr pc, DATA_SLOT(StubPrecode, Target)
    LEAF_END_MARKED StubPrecodeCode

    ALIGN 4

    LEAF_ENTRY FixupPrecodeCode
        ldr pc, DATA_SLOT(FixupPrecode, Target)
        ldr r12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr pc, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
    LEAF_END_MARKED FixupPrecodeCode

    ALIGN 4

    LEAF_ENTRY CallCountingStubCode
        push {r0}
        ldr r12, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh r0, [r12]
        subs r0, r0, #1
        strh r0, [r12]
        pop {r0}
        beq CountReachedZero
        ldr pc, DATA_SLOT(CallCountingStub, TargetForMethod)
CountReachedZero
        ldr pc, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
    LEAF_END_MARKED CallCountingStubCode

    ALIGN 4

    LEAF_ENTRY LookupStubCode
        ldr r12, DATA_SLOT(LookupStub, DispatchToken)
        ldr pc, DATA_SLOT(LookupStub, ResolveWorkerTarget)
    LEAF_END_MARKED LookupStubCode

    ALIGN 4

    LEAF_ENTRY DispatchStubCode
    PATCH_LABEL DispatchStubCode_ThisDeref
        ldr r12, [r0]
        push {r5}
        ldr r5, DATA_SLOT(DispatchStub, ExpectedMT)
        cmp r5, r12
        pop {r5}
        bne FailTarget
        ldr pc, DATA_SLOT(DispatchStub, ImplTarget)
FailTarget
        ldr pc, DATA_SLOT(DispatchStub, FailTarget)
    LEAF_END_MARKED DispatchStubCode

    ALIGN 4

    LEAF_ENTRY ResolveStubCode
    PATCH_LABEL ResolveStubCode_ResolveEntry
    PATCH_LABEL ResolveStubCode_ThisDeref
        ldr r12, [r0]
        push {r5, r6}
        add r6, r12, r12 lsr #12
        ldr r5, DATA_SLOT(ResolveStub, HashedToken)
        eor r6, r6, r5
        mov r5, #CALL_STUB_CACHE_MASK_ASM * 4
        and r6, r6, r5
        ldr r5, DATA_SLOT(ResolveStub, CacheAddress)
        ldr r6, [r5, r6]
Loop
        ldr r5, [r6]
        cmp r12, r5
        bne NextEntry
        ldr r5, DATA_SLOT(ResolveStub, Token)
        ldr r12, [r6, #4]
        cmp r12, r5
        bne NextEntry
        ldr r12, [r6, #8]
        pop {r5, r6}
        bx r12
NextEntry
        ldr r6, [r6, #12]
        cbz r6, Slow
        ldr r12, [r0]
        b Loop
Slow
        pop {r5, r6}
        nop
        ldr r12, DATA_SLOT(ResolveStub, Token)
        ldr pc, DATA_SLOT(ResolveStub, ResolveWorkerTarget)
    PATCH_LABEL ResolveStubCode_FailEntry
        push {r5}
        adr r5, DATA_SLOT(ResolveStub, Counter)
        ldr r12, [r5]
        subs r12, r12, #1
        str r12, [r5]
        pop {r5}
        bge ResolveStubCode
        orr r4, r4, #1; SDF_ResolveBackPatch
        b ResolveStubCode
    LEAF_END_MARKED ResolveStubCode

    END
