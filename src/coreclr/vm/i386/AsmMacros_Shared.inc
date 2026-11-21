; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include asmconstants.inc
include asmmacros.inc

G_LOWEST_ADDRESS                            equ _g_lowest_address
G_HIGHEST_ADDRESS                           equ _g_highest_address
G_EPHEMERAL_LOW                             equ _g_ephemeral_low
G_EPHEMERAL_HIGH                            equ _g_ephemeral_high
G_CARD_TABLE                                equ _g_card_table
G_TRAP_THREADS                              equ _g_TrapReturningThreads
G_POLL_GC                                   equ _g_pPollGC

EXTERN G_LOWEST_ADDRESS : DWORD
EXTERN G_HIGHEST_ADDRESS : DWORD
EXTERN G_EPHEMERAL_LOW : DWORD
EXTERN G_EPHEMERAL_HIGH : DWORD
EXTERN G_CARD_TABLE : DWORD
EXTERN G_TRAP_THREADS : DWORD
EXTERN G_POLL_GC : DWORD

;; CallEHFunclet and CallEHFilterFunclet are called with __fastcall, which passes the third argument at the
;; lower stack address.
FUNCLET_CALL_ARG3 equ 4
FUNCLET_CALL_ARG4 equ 8

;; CallEHFunclet loads the preserved registers of the parent frame from the CONTEXT
LOAD_FUNCLET_REGISTER macro base, reg
        mov     reg, [base + CONTEXT_&reg]
        endm

TAILCALL_GC_POLL_RARE macro
        mov     eax, G_POLL_GC
        jmp     eax
        endm

RhpGcAlloc TEXTEQU <_RhpGcAlloc@16>

EXTERN RhpGcAlloc : PROC
EXTERN RhExceptionHandling_FailedAllocation : PROC
