; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

include AsmMacros.inc

G_TRAP_THREADS TEXTEQU <RhpTrapThreads>

;; CallEHFunclet accesses the preserved registers through the REGDISPLAY
OFFSETOF__EHFuncletRegs__pRbx   equ OFFSETOF__REGDISPLAY__pRbx
OFFSETOF__EHFuncletRegs__pRbp   equ OFFSETOF__REGDISPLAY__pRbp
OFFSETOF__EHFuncletRegs__pRsi   equ OFFSETOF__REGDISPLAY__pRsi
OFFSETOF__EHFuncletRegs__pRdi   equ OFFSETOF__REGDISPLAY__pRdi
OFFSETOF__EHFuncletRegs__pR12   equ OFFSETOF__REGDISPLAY__pR12
OFFSETOF__EHFuncletRegs__pR13   equ OFFSETOF__REGDISPLAY__pR13
OFFSETOF__EHFuncletRegs__pR14   equ OFFSETOF__REGDISPLAY__pR14
OFFSETOF__EHFuncletRegs__pR15   equ OFFSETOF__REGDISPLAY__pR15
OFFSETOF__EHFuncletRegs__Xmm    equ OFFSETOF__REGDISPLAY__Xmm

EXTERN RhpGcPollRare : PROC

TAILCALL_GC_POLL_RARE macro
        jmp     RhpGcPollRare
        endm
