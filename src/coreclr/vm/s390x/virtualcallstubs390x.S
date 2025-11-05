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

// On Input:
//  8(%r15)  contains the address of the indirection cell (with the flags in the low bits)
//  %r0      contains the dispatch token  (slot number or MethodDesc) for the target
//                    or the ResolveCacheElem when the PROMOTE_CHAIN_FLAG is set
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler
        // Create a full transition block.
        stmg %r2, %r15, 16(%r15)
        .cfi_offset 6, -112
        .cfi_offset 7, -104
        .cfi_offset 8, -96
        .cfi_offset 9, -88
        .cfi_offset 10, -80
        .cfi_offset 11, -72
        .cfi_offset 12, -64
        .cfi_offset 13, -56
        .cfi_offset 14, -48
        .cfi_offset 15, -40
        lay %r15, -160(%r15)
        .cfi_def_cfa_offset 320

        // Save floating-point argument registers.
        std %f0, 288(%r15)
        std %f2, 296(%r15)
        std %f4, 304(%r15)
        std %f6, 312(%r15)

        la %r2, 160(%r15)             // transition block
        lg %r3, 168(%r15)             // indirection cell + flags
        lgr %r4, %r0                  // dispatch token
        risbg %r5, %r3, 62, 128+63, 0 // flags
        nill %r3, 65532               // indirection cell

        brasl %r14, C_FUNC(VSD_ResolveWorker)
        // Native target adress now in %r2.
        lgr %r1, %r2

        // Restore floating-point argument registers.
        ld %f0, 288(%r15)
        ld %f2, 296(%r15)
        ld %f4, 304(%r15)
        ld %f6, 312(%r15)

        // Restore integer parameter and call-saved registers.
        lmg %r2, %r15, 176(%r15)
        .cfi_restore 15
        .cfi_restore 14
        .cfi_restore 13
        .cfi_restore 12
        .cfi_restore 11
        .cfi_restore 10
        .cfi_restore 9
        .cfi_restore 8
        .cfi_restore 7
        .cfi_restore 6
        .cfi_def_cfa 15, 160

        // Tail call native target.
        br %r1
NESTED_END ResolveWorkerAsmStub, _TEXT

// This will perform a quick chained lookup of the entry if the initial cache lookup fails
// On Input:
//  16(%r15) contains the pointer to the ResolveCacheElem
//  8(%r15)  contains the address of the indirection cell (with the flags in the low bits)
//  %r0      contains the dispatch token
//  %r2      contains the object ref (used to look up the MethodTable)
LEAF_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT
        // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub
        tm 8+7(%r15), BACKPATCH_FLAG
        jgne C_FUNC(ResolveWorkerAsmStub)

        lg %r1, 16(%r15)        // Load the pointer to the ResolveCacheElem
LOCAL_LABEL(MainLoop):
        ltg %r1, 24(%r1)        // Get the next entry in the chain (don't bother checking the first entry again)
        jge C_FUNC(ResolveWorkerAsmStub)  // Fail if we hit a terminating NULL
        clc 0(8,%r2), 0(%r1)    // Compare our MT with the one in the ResolveCacheElem
        jne LOCAL_LABEL(MainLoop)
        clg %r0, 8(%r1)         // Compare our DispatchToken with one in the ResolveCacheElem
        jne LOCAL_LABEL(MainLoop)

LOCAL_LABEL(Success):
        mvc 24(8,%r15), 16(%r1) // Save the ImplTarget to a temp location
        PREPARE_EXTERNAL_VAR CHAIN_SUCCESS_COUNTER, %r1
        agsi 0(%r1), -1
        jl LOCAL_LABEL(Promote)
        lg %r1, 24(%r15)        // Branch to the target
        br %r1

LOCAL_LABEL(Promote):           // Move this entry to head position of the chain
        // Be quick to reset the counter so we don't get a bunch of contending threads
        mvghi 0(%r1), INITIAL_SUCCESS_COUNT

        // Set the PROMOTE_CHAIN_FLAG and pass the ResolveCacheElem to
        // ResolveWorkerAsmStub instead of the DispatchToken
        oi 8+7(%r15), PROMOTE_CHAIN_FLAG
        lg %r0, 16(%r15)
        jg C_FUNC(ResolveWorkerAsmStub)

LEAF_END ResolveWorkerChainLookupAsmStub, _TEXT
