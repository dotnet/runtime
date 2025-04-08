// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE

#define POINTER_SIZE 0x08

#define THUNKS_MAP_SIZE 0x4000

#define PAGE_SIZE 0x4000
#define PAGE_SIZE_LOG2 14


#define DATA_SLOT(stub, field, thunkSize, thunkTemplateName) C_FUNC(thunkTemplateName) + THUNKS_MAP_SIZE + stub##Data__##field + IN_PAGE_INDEX * thunkSize

// ----------
// StubPrecode
// ----------

#define STUB_PRECODE_CODESIZE 0x18 // 3 instructions, 13 bytes encoded + 11 bytes of padding
#define STUB_PRECODE_DATASIZE 0x18 // 2 qwords + a BYTE
.set STUB_PRECODE_NUM_THUNKS_PER_MAPPING,(THUNKS_MAP_SIZE / STUB_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_STUB_PRECODE
    IN_PAGE_INDEX = 0
    .rept STUB_PRECODE_NUM_THUNKS_PER_MAPPING

    mov    r10, [rip + DATA_SLOT(StubPrecode, SecretParam, STUB_PRECODE_CODESIZE, StubPrecodeCodeTemplate)]
    jmp    [rip + DATA_SLOT(StubPrecode, Target, STUB_PRECODE_CODESIZE, StubPrecodeCodeTemplate)]
    // The above is 13 bytes
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
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

#define FIXUP_PRECODE_CODESIZE 0x18 
#define FIXUP_PRECODE_DATASIZE 0x18 // 3 qwords
.set FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING,(THUNKS_MAP_SIZE / FIXUP_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_FIXUP_PRECODE
    IN_PAGE_INDEX = 0
    .rept FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING

        jmp    [rip + DATA_SLOT(FixupPrecode, Target, FIXUP_PRECODE_CODESIZE, FixupPrecodeCodeTemplate)]
        mov    r10, [rip + DATA_SLOT(FixupPrecode, MethodDesc, FIXUP_PRECODE_CODESIZE, FixupPrecodeCodeTemplate)]
        jmp    [rip + DATA_SLOT(FixupPrecode, PrecodeFixupThunk, FIXUP_PRECODE_CODESIZE, FixupPrecodeCodeTemplate)]
        // The above is 19 bytes
        int 3
        int 3
        int 3
        int 3
        int 3
    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY FixupPrecodeCodeTemplate
    THUNKS_BLOCK_FIXUP_PRECODE
    // We need 16 bytes of padding to pad this out
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
LEAF_END_MARKED FixupPrecodeCodeTemplate, _TEXT

// ----------
// CallCountingStub
// ----------

#define CALLCOUNTING_CODESIZE 0x18
#define CALLCOUNTING_DATASIZE 0x18 // 3 qwords
.set CALLCOUNTING_NUM_THUNKS_PER_MAPPING, (THUNKS_MAP_SIZE / CALLCOUNTING_CODESIZE)
.macro THUNKS_BLOCK_CALLCOUNTING
    IN_PAGE_INDEX = 0
    .rept CALLCOUNTING_NUM_THUNKS_PER_MAPPING

        mov    rax,QWORD PTR [rip + DATA_SLOT(CallCountingStub, RemainingCallCountCell, CALLCOUNTING_CODESIZE, CallCountingStubCodeTemplate)]
        dec    WORD PTR [rax]
        je     0f
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForMethod, CALLCOUNTING_CODESIZE, CallCountingStubCodeTemplate)]
    0:
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached, CALLCOUNTING_CODESIZE, CallCountingStubCodeTemplate)]
    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY CallCountingStubCodeTemplate
    THUNKS_BLOCK_CALLCOUNTING
    // We need 16 bytes of padding to pad this out
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
    int 3
LEAF_END_MARKED CallCountingStubCodeTemplate, _TEXT

#endif

// STUB_PAGE_SIZE must match the behavior of GetStubCodePageSize() on this architecture/os
STUB_PAGE_SIZE = 16384

#ifdef DATA_SLOT
#undef DATA_SLOT
#endif

#define DATA_SLOT(stub, field) C_FUNC(stub##Code) + STUB_PAGE_SIZE + stub##Data__##field

LEAF_ENTRY StubPrecodeCode, _TEXT
        mov    r10, [rip + DATA_SLOT(StubPrecode, SecretParam)]
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
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode, _TEXT
