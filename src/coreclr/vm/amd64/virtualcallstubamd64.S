// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

// This is the number of times a successful chain lookup will occur before the
// entry is promoted to the front of the chain. This is declared as extern because
// the default value (CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT) is defined in the header.
// extern size_t g_dispatch_cache_chain_success_counter;
#define CHAIN_SUCCESS_COUNTER g_dispatch_cache_chain_success_counter

// The reason for not using .equ or '=' here is that otherwise the assembler compiles e.g.
// mov rax, BACKPATCH_FLAG as mov rax, [BACKPATCH_FLAG]
#define BACKPATCH_FLAG             1        // Also known as SDF_ResolveBackPatch    in the EE
#define PROMOTE_CHAIN_FLAG         2        // Also known as SDF_ResolvePromoteChain in the EE
#define INITIAL_SUCCESS_COUNT      0x100

// On Input:
//    r11                    contains the address of the indirection cell (with the flags in the low bits)
//  [rsp+0] m_Datum:         contains the dispatch token  (slot number or MethodDesc) for the target
//                                 or the ResolveCacheElem when r11 has the PROMOTE_CHAIN_FLAG set
//  [rsp+8] m_ReturnAddress: contains the return address of caller to stub

NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, rdx, 0, 0

        // token stored in rdx by prolog

        lea             rdi, [rsp + __PWTB_TransitionBlock]        // pTransitionBlock
        mov             rsi, r11                                   // indirection cell + flags
        mov             rcx,  rsi
        and             rcx,  7                                    // flags
        sub             rsi, rcx                                   // indirection cell

        call            C_FUNC(VSD_ResolveWorker)

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END ResolveWorkerAsmStub, _TEXT

// extern void ResolveWorkerChainLookupAsmStub()
LEAF_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT
// This will perform a quick chained lookup of the entry if the initial cache lookup fails
// On Input:
//   rdx       contains our type     (MethodTable)
//   r10       contains our contract (DispatchToken)
//   r11       contains the address of the indirection (and the flags in the low two bits)
// [rsp+0x00]  contains the pointer to the ResolveCacheElem
// [rsp+0x08]  contains the saved value of rsi
// [rsp+0x10]  contains the return address of caller to stub
//
        mov     rax, BACKPATCH_FLAG  // First we check if r11 has the BACKPATCH_FLAG set
        and     rax, r11             // Set the flags based on (BACKPATCH_FLAG and r11)
        pop     rax                  // pop the pointer to the ResolveCacheElem from the top of stack (leaving the flags unchanged)
        jnz     Fail_RWCLAS          // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

MainLoop_RWCLAS:
        mov     rax, [rax+0x18]   // get the next entry in the chain (don't bother checking the first entry again)
        test    rax,rax          // test if we hit a terminating NULL
        jz      Fail_RWCLAS

        cmp    rdx, [rax+0x00]    // compare our MT with the one in the ResolveCacheElem
        jne    MainLoop_RWCLAS
        cmp    r10, [rax+0x08]    // compare our DispatchToken with one in the ResolveCacheElem
        jne    MainLoop_RWCLAS
Success_RWCLAS:
        PREPARE_EXTERNAL_VAR CHAIN_SUCCESS_COUNTER, rdx
        sub    qword ptr [rdx],1 // decrement success counter
        jl     Promote_RWCLAS
        mov    rax, [rax+0x10]    // get the ImplTarget
        pop    rdx
        jmp    rax

Promote_RWCLAS:                  // Move this entry to head postion of the chain
        // be quick to reset the counter so we don't get a bunch of contending threads
        mov    qword ptr [rdx], INITIAL_SUCCESS_COUNT
        or     r11, PROMOTE_CHAIN_FLAG
        mov    r10, rax          // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken
Fail_RWCLAS:
        pop    rdx               // Restore the original saved rdx value
        push   r10               // pass the DispatchToken or ResolveCacheElem to promote to ResolveWorkerAsmStub

        jmp    C_FUNC(ResolveWorkerAsmStub)

LEAF_END ResolveWorkerChainLookupAsmStub, _TEXT

