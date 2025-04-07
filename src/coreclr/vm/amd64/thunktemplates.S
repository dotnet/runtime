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

#define STUB_PRECODE_CODESIZE 0x10 // 3 instructions, 4 bytes each (and we also have 4 bytes of padding)
#define STUB_PRECODE_DATASIZE 0x10 // 2 qwords
#define STUB_PRECODE_NUM_THUNKS_PER_MAPPING 1024 // (THUNKS_MAP_SIZE / STUB_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_STUB_PRECODE
    IN_PAGE_INDEX = 0
    .rept STUB_PRECODE_NUM_THUNKS_PER_MAPPING

    mov    r10, [rip + DATA_SLOT(StubPrecode, SecretParam, STUB_PRECODE_CODESIZE, StubPrecodeCode)]
    jmp    [rip + DATA_SLOT(StubPrecode, Target, STUB_PRECODE_CODESIZE, StubPrecodeCode)]
    // The above is 19 bytes
    int 3
    int 3
    int 3
    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY StubPrecodeCode
    THUNKS_BLOCK_STUB_PRECODE
LEAF_END_MARKED StubPrecodeCode, _TEXT

// ----------
// FixupPrecode
// ----------

#define FIXUP_PRECODE_CODESIZE 0x18 
#define FIXUP_PRECODE_DATASIZE 0x18 // 3 qwords
#define FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING 682 // (THUNKS_MAP_SIZE / FIXUP_PRECODE_CODESIZE)

.macro THUNKS_BLOCK_FIXUP_PRECODE
    IN_PAGE_INDEX = 0
    .rept FIXUP_PRECODE_NUM_THUNKS_PER_MAPPING

        jmp    [rip + DATA_SLOT(FixupPrecode, Target, FIXUP_PRECODE_CODESIZE, FixupPrecodeCode)]
        mov    r10, [rip + DATA_SLOT(FixupPrecode, MethodDesc, FIXUP_PRECODE_CODESIZE, FixupPrecodeCode)]
        jmp    [rip + DATA_SLOT(FixupPrecode, PrecodeFixupThunk, FIXUP_PRECODE_CODESIZE, FixupPrecodeCode)]
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
LEAF_ENTRY FixupPrecodeCode
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
LEAF_END_MARKED FixupPrecodeCode, _TEXT

// ----------
// CallCountingStub
// ----------

#define CALLCOUNTING_CODESIZE 0x18
#define CALLCOUNTING_DATASIZE 0x18 // 3 qwords
#define CALLCOUNTING_NUM_THUNKS_PER_MAPPING 682 // (THUNKS_MAP_SIZE / CALLCOUNTING_CODESIZE)

.macro THUNKS_BLOCK_CALLCOUNTING
    IN_PAGE_INDEX = 0
    .rept CALLCOUNTING_NUM_THUNKS_PER_MAPPING

        mov    rax,QWORD PTR [rip + DATA_SLOT(CallCountingStub, RemainingCallCountCell, CALLCOUNTING_CODESIZE, CallCountingStubCode)]
        dec    WORD PTR [rax]
        je     0f
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForMethod, CALLCOUNTING_CODESIZE, CallCountingStubCode)]
    0:
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached, CALLCOUNTING_CODESIZE, CallCountingStubCode)]
    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

    .text
    .p2align PAGE_SIZE_LOG2
LEAF_ENTRY CallCountingStubCode
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
LEAF_END_MARKED CallCountingStubCode, _TEXT

//#else
// STUB_PAGE_SIZE must match the behavior of GetStubCodePageSize() on this architecture/os
STUB_PAGE_SIZE = 16384
#undef DATA_SLOT
#define DATA_SLOT(stub, field) C_FUNC(stub##Code2) + STUB_PAGE_SIZE + stub##Data__##field

LEAF_ENTRY StubPrecodeCode2, _TEXT
        mov    r10, [rip + DATA_SLOT(StubPrecode, SecretParam)]
        jmp    [rip + DATA_SLOT(StubPrecode, Target)]
LEAF_END_MARKED StubPrecodeCode2, _TEXT

LEAF_ENTRY FixupPrecodeCode2, _TEXT
        jmp    [rip + DATA_SLOT(FixupPrecode, Target)]
PATCH_LABEL FixupPrecodeCode_Fixup
        mov    r10, [rip + DATA_SLOT(FixupPrecode, MethodDesc)]
        jmp    [rip + DATA_SLOT(FixupPrecode, PrecodeFixupThunk)]
LEAF_END_MARKED FixupPrecodeCode2, _TEXT

LEAF_ENTRY CallCountingStubCode2, _TEXT
        mov    rax,QWORD PTR [rip + DATA_SLOT(CallCountingStub, RemainingCallCountCell)]
        dec    WORD PTR [rax]
        je     LOCAL_LABEL(CountReachedZero)
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForMethod)]
    LOCAL_LABEL(CountReachedZero):
        jmp    QWORD PTR [rip + DATA_SLOT(CallCountingStub, TargetForThresholdReached)]
LEAF_END_MARKED CallCountingStubCode2, _TEXT
#endif