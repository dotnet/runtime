// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#define THUNK_CODESIZE 0x10 // 7-byte mov, 6-byte jmp, 3 bytes of padding
#define THUNK_DATASIZE 0x10 // 2 qwords

#define POINTER_SIZE 0x08

#define THUNKS_MAP_SIZE 0x8000

#define PAGE_SIZE 0x1000
#define PAGE_SIZE_LOG2 12

// THUNK_POOL_NUM_THUNKS_PER_PAGE = min(PAGE_SIZE / THUNK_CODESIZE, (PAGE_SIZE - POINTER_SIZE) / THUNK_DATASIZE)
#define THUNK_POOL_NUM_THUNKS_PER_PAGE 0xff

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Thunk Pages ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

.macro THUNKS_PAGE_BLOCK
    IN_PAGE_INDEX = 0
    .rept THUNK_POOL_NUM_THUNKS_PER_PAGE

    .p2align 4

    mov     r10, qword ptr [rip + THUNKS_MAP_SIZE - 7]
    jmp     qword ptr [rip + THUNKS_MAP_SIZE + POINTER_SIZE - 13]

    IN_PAGE_INDEX = IN_PAGE_INDEX + 1
    .endr
.endm

#ifdef TARGET_APPLE
// Thunk pool
    .text
    .p2align PAGE_SIZE_LOG2
PATCH_LABEL ThunkPool
    .rept (THUNKS_MAP_SIZE / PAGE_SIZE)
    .p2align PAGE_SIZE_LOG2
    THUNKS_PAGE_BLOCK
    .endr
    .p2align PAGE_SIZE_LOG2
#else
#error Unsupported OS
#endif

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; General Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

//
// IntPtr RhpGetThunksBase()
//
LEAF_ENTRY RhpGetThunksBase
    // Return the address of the first thunk pool to the caller (this is really the base address)
    lea     rax, [rip + C_FUNC(ThunkPool)]
    ret
LEAF_END RhpGetThunksBase

//
// int RhpGetNumThunksPerBlock()
//
LEAF_ENTRY RhpGetNumThunksPerBlock
    mov     rax, THUNK_POOL_NUM_THUNKS_PER_PAGE
    ret
LEAF_END RhpGetNumThunksPerBlock

//
// int RhpGetThunkSize()
//
LEAF_ENTRY RhpGetThunkSize
    mov     rax, THUNK_CODESIZE
    ret
LEAF_END RhpGetThunkSize

//
// int RhpGetNumThunkBlocksPerMapping()
//
LEAF_ENTRY RhpGetNumThunkBlocksPerMapping
    mov     rax, (THUNKS_MAP_SIZE / PAGE_SIZE)
    ret
LEAF_END RhpGetNumThunkBlocksPerMapping

//
// int RhpGetThunkBlockSize
//
LEAF_ENTRY RhpGetThunkBlockSize
    mov     rax, PAGE_SIZE
    ret
LEAF_END RhpGetThunkBlockSize

//
// IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
//
LEAF_ENTRY RhpGetThunkDataBlockAddress
    mov     rax, rdi
    mov     rdi, PAGE_SIZE - 1
    not     rdi
    and     rax, rdi
    add     rax, THUNKS_MAP_SIZE
    ret
LEAF_END RhpGetThunkDataBlockAddress

//
// IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
//
LEAF_ENTRY RhpGetThunkStubsBlockAddress
    mov     rax, rdi
    mov     rdi, PAGE_SIZE - 1
    not     rdi
    and     rax, rdi
    sub     rax, THUNKS_MAP_SIZE
    ret
LEAF_END RhpGetThunkStubsBlockAddress
