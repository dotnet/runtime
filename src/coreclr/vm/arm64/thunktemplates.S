// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE

#define POINTER_SIZE 0x08

#define THUNKS_MAP_SIZE 0x4000

#define PAGE_SIZE 0x4000
#define PAGE_SIZE_LOG2 14


#define DATA_SLOT(stub, field, thunksPerPage, thunkTemplateName) . - (. - C_FUNC(thunkTemplateName)) + \THUNKS_MAP_SIZE + stub##Data__##field + IN_PAGE_INDEX * STUB_THUNK_CODESIZE

// ----------
// StubPrecode
// ----------

#define STUB_PRECODE_CODESIZE 0x18 // 3 instructions, 4 bytes each (and we also have 12 bytes of padding)
#define STUB_PRECODE_DATASIZE 0x18 // 2 qwords + 1 byte
.set STUB_PRECODE_NUM_THUNKS_PER_MAPPING,(THUNKS_MAP_SIZE / STUB_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_STUB_PRECODE
    IN_PAGE_INDEX = 0
    .rept STUB_PRECODE_NUM_THUNKS_PER_MAP

    ldr x10, DATA_SLOT(StubPrecode, Target, STUB_PRECODE_NUM_THUNKS_PER_MAPPING, StubPrecodeCodeTemplate)
    ldr x12, DATA_SLOT(StubPrecode, SecretParam, STUB_PRECODE_NUM_THUNKS_PER_MAPPING, StubPrecodeCodeTemplate)
    br x10

    brk     0xf000      // Stubs need to be 24-byte in size to allow for the data to be 2 pointers + 1 byte
    brk     0xf000      // Stubs need to be 24-byte in size to allow for the data to be 2 pointers + 1 byte
    brk     0xf000      // Stubs need to be 24-byte in size to allow for the data to be 2 pointers + 1 byte

    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY StubPrecodeCodeTemplate
    THUNKS_BLOCK_STUB_PRECODE
LEAF_END_MARKED StubPrecodeCodeTemplate, _TEXT

// ----------
// FixupPrecode
// ----------

#define FIXUP_PRECODE_CODESIZE 0x18 // 5 instructions, 4 bytes each (and we also have 4 bytes of padding)
#define FIXUP_PRECODE_DATASIZE 0x18 // 3 qwords
.set FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING,(THUNKS_MAP_SIZE / FIXUP_PRECODE_CODESIZE)
#define FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING (THUNKS_MAP_SIZE / FIXUP_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_FIXUP_PRECODE
    IN_PAGE_INDEX = 0
    .rept FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING

    ldr x11, DATA_SLOT(FixupPrecode, Target, FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING, FixupPrecodeCodeTemplate)
    br  x11
    ldr x12, DATA_SLOT(FixupPrecode, MethodDesc, FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING, FixupPrecodeCodeTemplate)
    ldr x11, DATA_SLOT(FixupPrecode, PrecodeFixupThunk, FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING, FixupPrecodeCodeTemplate)
    br  x11        
    brk     0xf000      // Stubs need to be 24-byte in size to allow for the data to be 2 pointers

    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY FixupPrecodeCodeTemplate
    THUNKS_BLOCK_FIXUP_PRECODE
LEAF_END_MARKED FixupPrecodeCodeTemplate, _TEXT

// ----------
// CallCountingStub
// ----------

#define CALLCOUNTING_CODESIZE 0x28 // 5 instructions, 4 bytes each (and we also have 4 bytes of padding)
#define CALLCOUNTING_DATASIZE 0x18 // 3 qwords
#define CALLCOUNTING_NUM_THUNKS_PER_MAPPING (THUNKS_MAP_SIZE / CALLCOUNTING_CODESIZE)

.macro THUNKS_BLOCK_CALLCOUNTING
    IN_PAGE_INDEX = 0
    .rept CALLCOUNTING_NUM_THUNKS_PER_MAPPING

        ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh w10, [x9]
        subs w10, w10, #1
        strh w10, [x9]
        beq LOCAL_LABEL(CountReachedZero\IN_PAGE_INDEX)
        ldr  x9, DATA_SLOT(CallCountingStub, TargetForMethod)
        br   x9
LOCAL_LABEL(CountReachedZero\IN_PAGE_INDEX):
        ldr  x10, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
        br   x10
        brk     0xf000      // Stubs need to be 16-byte in size to allow for the data to be 2 pointers

    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY CallCountingStubCodeTemplate
    THUNKS_BLOCK_CALLCOUNTING
LEAF_END_MARKED CallCountingStubCodeTemplate, _TEXT

#else
#define DATA_SLOT(stub, field) . - (. - C_FUNC(stub##Code\STUB_PAGE_SIZE)) + \STUB_PAGE_SIZE + stub##Data__##field

    .irp STUB_PAGE_SIZE, 16384, 32768, 65536

    LEAF_ENTRY StubPrecodeCode\STUB_PAGE_SIZE
        ldr x10, DATA_SLOT(StubPrecode, Target)
        ldr x12, DATA_SLOT(StubPrecode, SecretParam)
        br x10
    LEAF_END_MARKED StubPrecodeCode\STUB_PAGE_SIZE

    LEAF_ENTRY FixupPrecodeCode\STUB_PAGE_SIZE
        ldr x11, DATA_SLOT(FixupPrecode, Target)
        br  x11
        ldr x12, DATA_SLOT(FixupPrecode, MethodDesc)
        ldr x11, DATA_SLOT(FixupPrecode, PrecodeFixupThunk)
        br  x11        
    LEAF_END_MARKED FixupPrecodeCode\STUB_PAGE_SIZE

    LEAF_ENTRY CallCountingStubCode\STUB_PAGE_SIZE
LOCAL_LABEL(StubStart\STUB_PAGE_SIZE):
        ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
        ldrh w10, [x9]
        subs w10, w10, #1
        strh w10, [x9]
        beq LOCAL_LABEL(CountReachedZero\STUB_PAGE_SIZE)
        ldr  x9, DATA_SLOT(CallCountingStub, TargetForMethod)
        br   x9
LOCAL_LABEL(CountReachedZero\STUB_PAGE_SIZE):
        ldr  x10, DATA_SLOT(CallCountingStub, TargetForThresholdReached)
        br   x10
    LEAF_END_MARKED CallCountingStubCode\STUB_PAGE_SIZE

    .endr
#endif