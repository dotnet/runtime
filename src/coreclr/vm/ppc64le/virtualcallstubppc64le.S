// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

// This is the number of times a successful chain lookup will occur before the
// entry is promoted to the front of the chain. This is declared as extern because
// the default value (CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT) is defined in the header.
// extern size_t g_dispatch_cache_chain_success_counter;
#define CHAIN_SUCCESS_COUNTER g_dispatch_cache_chain_success_counter

#define BACKPATCH_FLAG             1        // Also known as SDF_ResolveBackPatch    in the EE
#define PROMOTE_CHAIN_FLAG         2        // Also known as SDF_ResolvePromoteChain in the EE
#define INITIAL_SUCCESS_COUNT      0x100

.machine "ppc64le"
//-----------------------------------------------------------------------------
// ResolveWorkerAsmStub
// Inputs:
//   r3: object reference
//   r5: dispatch token (or ResolveCacheElem* when PROMOTE_CHAIN_FLAG set)
//   40(r1): indirection cell | flags  (caller wrote at 8(r1))
// Output:
//   Tail-calls target (via CTR)
// Clobbers: r0, r3-r12 (volatile only)
// VSD_ResolveWorker args: r3=obj, r4=cell, r5=token|node, r7=flags
//-----------------------------------------------------------------------------
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler
    stdu    %r1, -32(%r1)               // 32-byte frame
    mflr    %r0
    std     %r0, 16(%r1)
    .cfi_def_cfa_offset 32
    .cfi_offset 65, 16

    ld      %r4, 40(%r1)                // r4 = cell|flags (from caller’s 8(r1))
    rldicl  %r7, %r4, 0, 62             // r7 = flags (keep low 2 bits)
    // Clear *rightmost* 2 bits of r4 (cell) — clrrdi r4,r4,2 is alias of rldicr
    rldicr  %r4, %r4, 0, 61             // r4 = cell (low 2 bits zeroed)

    bl      C_FUNC(VSD_ResolveWorker) // r3 = target PC
    mr      %r12, %r3                   // move target to r12

    ld      %r0, 16(%r1)
    mtlr    %r0
    addi    %r1, %r1, 32
    mtctr   %r12
    bctr
NESTED_END ResolveWorkerAsmStub, _TEXT


//-----------------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
// Inputs:
//   r3: object reference
//   r5: dispatch token
//   8(r1): indirection cell | flags
//   16(r1): ResolveCacheElem* head
// Output:
//   Hit:    tail-calls cached target
//   Miss/promote: jumps to ResolveWorkerAsmStub
// Clobbers: r0, r3-r12 (volatile only)
// ResolveCacheElem layout assumed:
//   0: cached MethodTable
//   8: cached token
//  16: target PC
//  24: next (ResolveCacheElem*)
//-----------------------------------------------------------------------------
LEAF_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT
    // If BACKPATCH_FLAG set, go straight to worker
    ld      %r0, 8(%r1)                 // cell|flags
    andi.   %r0, %r0, BACKPATCH_FLAG
    bne     ResolveWorkerAsmStub

    ld      %r9, 16(%r1)                // r9 = head
.Lprobe:
    cmpdi   cr0, %r9, 0
    beq     ResolveWorkerAsmStub      // empty chain → resolve

    // Compare MethodTable
    ld      %r6, 0(%r3)                 // actual MethodTable
    ld      %r0, 0(%r9)                 // cached MethodTable
    cmpd    cr0, %r6, %r0
    bne     .Lnext

    // Compare token
    ld      %r0, 8(%r9)                 // cached token
    cmpd    cr0, %r5, %r0
    bne     .Lnext

    // Cache hit: r12 = target
    ld      %r12, 16(%r9)

    // Decrement global success counter atomically
    addis  %r11, %r2, g_dispatch_cache_chain_success_counter@toc@ha
    ld     %r11, g_dispatch_cache_chain_success_counter@toc@l(%r11)
.Ldec_retry:
    lwarx   %r0, 0, %r11
    addi    %r0, %r0, -1
    stwcx.  %r0, 0, %r11
    bne-    .Ldec_retry

    // If counter >= 0 → tail-call; else promote chain
    cmpwi   cr0, %r0, 0
    bge     .Ltail_call

    // Promotion path: reset counter, set PROMOTE_CHAIN_FLAG in caller slot
    li      %r0, INITIAL_SUCCESS_COUNT
    std     %r0, 0(%r11)

    ld      %r0, 8(%r1)                 // cell|flags (caller frame)
    ori     %r0, %r0, PROMOTE_CHAIN_FLAG
    std     %r0, 8(%r1)

    mr      %r5, %r9                    // r5 = ResolveCacheElem*
    b       ResolveWorkerAsmStub

.Lnext:
    ld      %r9, 24(%r9)                // advance to next
    b       .Lprobe

.Ltail_call:
    mtctr   %r12
    bctr
LEAF_END ResolveWorkerChainLookupAsmStub, _TEXT
