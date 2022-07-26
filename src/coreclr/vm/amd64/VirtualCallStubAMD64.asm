; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

CHAIN_SUCCESS_COUNTER  equ ?g_dispatch_cache_chain_success_counter@@3_KA

        extern  VSD_ResolveWorker:proc
        extern  CHAIN_SUCCESS_COUNTER:dword

BACKPATCH_FLAG                  equ    1        ;; Also known as SDF_ResolveBackPatch    in the EE
PROMOTE_CHAIN_FLAG              equ    2        ;; Also known as SDF_ResolvePromoteChain in the EE
INITIAL_SUCCESS_COUNT           equ  100h

;; On Input:
;;    r11                    contains the address of the indirection cell (with the flags in the low bits)
;;  [rsp+0] m_Datum:         contains the dispatch token  (slot number or MethodDesc) for the target
;;                                 or the ResolveCacheElem when r11 has the PROMOTE_CHAIN_FLAG set
;;  [rsp+8] m_ReturnAddress: contains the return address of caller to stub

NESTED_ENTRY ResolveWorkerAsmStub, _TEXT

        PROLOG_WITH_TRANSITION_BLOCK 0, 8, r8

        ; token stored in r8 by prolog

        lea             rcx, [rsp + __PWTB_TransitionBlock]         ; pTransitionBlock
        mov             rdx, r11                                  ; indirection cell + flags
        mov             r9,  rdx
        and             r9,  7                                    ; flags
        sub             rdx, r9                                   ; indirection cell

        call            VSD_ResolveWorker

        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
        TAILJMP_RAX

NESTED_END ResolveWorkerAsmStub, _TEXT

;; extern void ResolveWorkerChainLookupAsmStub()
LEAF_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT
;; This will perform a quick chained lookup of the entry if the initial cache lookup fails
;; On Input:
;;   rdx       contains our type     (MethodTable)
;;   r10       contains our contract (DispatchToken)
;;   r11       contains the address of the indirection (and the flags in the low two bits)
;; [rsp+0x00]  contains the pointer to the ResolveCacheElem
;; [rsp+0x08]  contains the saved value of rdx
;; [rsp+0x10]  contains the return address of caller to stub
;;
        mov     rax, BACKPATCH_FLAG  ;; First we check if r11 has the BACKPATCH_FLAG set
        and     rax, r11             ;; Set the flags based on (BACKPATCH_FLAG and r11)
        pop     rax                  ;; pop the pointer to the ResolveCacheElem from the top of stack (leaving the flags unchanged)
        jnz     Fail                 ;; If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

MainLoop:
        mov     rax, [rax+18h]   ;; get the next entry in the chain (don't bother checking the first entry again)
        test    rax,rax          ;; test if we hit a terminating NULL
        jz      Fail

        cmp    rdx, [rax+00h]    ;; compare our MT with the one in the ResolveCacheElem
        jne    MainLoop
        cmp    r10, [rax+08h]    ;; compare our DispatchToken with one in the ResolveCacheElem
        jne    MainLoop
Success:
        sub    [CHAIN_SUCCESS_COUNTER],1 ;; decrement success counter
        jl     Promote
        mov    rax, [rax+10h]    ;; get the ImplTarget
        pop    rdx
        jmp    rax

Promote:                         ;; Move this entry to head position of the chain
        ;; be quick to reset the counter so we don't get a bunch of contending threads
        mov    [CHAIN_SUCCESS_COUNTER], INITIAL_SUCCESS_COUNT
        or     r11, PROMOTE_CHAIN_FLAG
        mov    r10, rax          ;; We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken
Fail:
        pop    rdx               ;; Restore the original saved rdx value
        push   r10               ;; pass the DispatchToken or ResolveCacheElem to promote to ResolveWorkerAsmStub

        jmp    ResolveWorkerAsmStub

LEAF_END ResolveWorkerChainLookupAsmStub, _TEXT

        end
