; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include AsmMacros.inc

G_TRAP_THREADS TEXTEQU <RhpTrapThreads>

;; CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY
LOAD_FUNCLET_REGISTER macro base, reg, scratch
        mov     scratch, [base + OFFSETOF__REGDISPLAY__p&reg]
        mov     reg, [scratch]
        endm

STORE_FUNCLET_REGISTER macro base, reg, scratch
        mov     scratch, [base + OFFSETOF__REGDISPLAY__p&reg]
        mov     [scratch], reg
        endm

LOAD_FUNCLET_XMM_REGISTER macro base, index
        movdqa  xmm&index, [base + OFFSETOF__REGDISPLAY__Xmm + (index - 6) * 10h]
        endm

EXTERN RhpGcPollRare : PROC

TAILCALL_GC_POLL_RARE macro
        jmp     RhpGcPollRare
        endm
