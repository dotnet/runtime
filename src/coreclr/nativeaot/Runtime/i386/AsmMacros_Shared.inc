; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include AsmMacros.inc

G_TRAP_THREADS TEXTEQU <RhpTrapThreads>

;; CallEHFunclet and CallEHFilterFunclet are called with the managed calling convention, which passes the
;; stack arguments in the reverse order of __fastcall.
FUNCLET_CALL_ARG3 equ 8
FUNCLET_CALL_ARG4 equ 4

;; The REGDISPLAY names the preserved registers like their x64 counterparts.
OFFSETOF__REGDISPLAY__pEbx equ OFFSETOF__REGDISPLAY__pRbx
OFFSETOF__REGDISPLAY__pEbp equ OFFSETOF__REGDISPLAY__pRbp
OFFSETOF__REGDISPLAY__pEsi equ OFFSETOF__REGDISPLAY__pRsi
OFFSETOF__REGDISPLAY__pEdi equ OFFSETOF__REGDISPLAY__pRdi

;; CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY
LOAD_FUNCLET_REGISTER macro base, reg
        mov     reg, [base + OFFSETOF__REGDISPLAY__p&reg]
        mov     reg, [reg]
        endm

STORE_FUNCLET_REGISTER macro base, reg, scratch
        mov     scratch, [base + OFFSETOF__REGDISPLAY__p&reg]
        mov     [scratch], reg
        endm

EXTERN RhpGcPollRare : PROC

TAILCALL_GC_POLL_RARE macro
        jmp     RhpGcPollRare
        endm