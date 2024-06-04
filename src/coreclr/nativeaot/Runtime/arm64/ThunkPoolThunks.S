// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#define THUNK_CODESIZE 0x10 // 3 instructions, 4 bytes each (and we also have 4 bytes of padding)
#define THUNK_DATASIZE 0x10 // 2 qwords

#define POINTER_SIZE 0x08

#define THUNKS_MAP_SIZE 0x8000

#ifdef TARGET_APPLE
#define PAGE_SIZE 0x4000
#define PAGE_SIZE_LOG2 14
#else
#error Unsupported OS
#endif

// THUNK_POOL_NUM_THUNKS_PER_PAGE = min(PAGE_SIZE / THUNK_CODESIZE, (PAGE_SIZE - POINTER_SIZE) / THUNK_DATASIZE)
#define THUNK_POOL_NUM_THUNKS_PER_PAGE 0x3ff

//;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Thunk Pages ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

.macro THUNKS_PAGE_BLOCK
    IN_PAGE_INDEX = 0
    .rept THUNK_POOL_NUM_THUNKS_PER_PAGE

    // Set xip0 to the address of the current thunk's data block.
    adr      xip0, THUNKS_MAP_SIZE

    // start                                        : xip0 points to the current thunks first data cell in the data page
    // set xip0 to beginning of data page           : xip0 <- xip0 - (THUNK_DATASIZE * current thunk's index)
    // fix offset to point to last QWROD in page    : xip1 <- [xip0 + PAGE_SIZE - POINTER_SIZE]
    // tailcall to the location pointed at by the last qword in the data page
    ldr      xip1, [xip0, #(PAGE_SIZE - POINTER_SIZE - (THUNK_DATASIZE * IN_PAGE_INDEX))]
    br       xip1

    brk     0xf000      // Stubs need to be 16-byte aligned for CFG table. Filling padding with a
                        // deterministic brk instruction, instead of having it just filled with zeros.

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
    adrp     x0, C_FUNC(ThunkPool)@PAGE
    add      x0, x0, C_FUNC(ThunkPool)@PAGEOFF
    ret
LEAF_END RhpGetThunksBase

//
// int RhpGetNumThunksPerBlock()
//
LEAF_ENTRY RhpGetNumThunksPerBlock
    mov     x0, THUNK_POOL_NUM_THUNKS_PER_PAGE
    ret
LEAF_END RhpGetNumThunksPerBlock

//
// int RhpGetThunkSize()
//
LEAF_ENTRY RhpGetThunkSize
    mov     x0, THUNK_CODESIZE
    ret
LEAF_END RhpGetThunkSize

//
// int RhpGetNumThunkBlocksPerMapping()
//
LEAF_ENTRY RhpGetNumThunkBlocksPerMapping
    mov     x0, (THUNKS_MAP_SIZE / PAGE_SIZE)
    ret
LEAF_END RhpGetNumThunkBlocksPerMapping

//
// int RhpGetThunkBlockSize
//
LEAF_ENTRY RhpGetThunkBlockSize
    mov     x0, PAGE_SIZE
    ret
LEAF_END RhpGetThunkBlockSize

//
// IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
//
LEAF_ENTRY RhpGetThunkDataBlockAddress
    mov     x12, PAGE_SIZE - 1
    bic     x0, x0, x12
    mov     x12, THUNKS_MAP_SIZE
    add     x0, x0, x12
    ret
LEAF_END RhpGetThunkDataBlockAddress

//
// IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
//
LEAF_ENTRY RhpGetThunkStubsBlockAddress
    mov     x12, PAGE_SIZE - 1
    bic     x0, x0, x12
    mov     x12, THUNKS_MAP_SIZE
    sub     x0, x0, x12
    ret
LEAF_END RhpGetThunkStubsBlockAddress
