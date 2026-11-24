// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#include "AsmOffsets.inc"
#include <unixasmmacros.inc>

.macro TAILCALL_GC_POLL_RARE
        b       C_FUNC(RhpGcPollRare)
.endm

// CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY.
// reg is the register number; the REGDISPLAY holds the integer registers in order and d8-d15 in D.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ldr     \scratch, [\base, #(OFFSETOF__REGDISPLAY__pR4 + (\reg - 4) * 4)]
        ldr     r\reg, [\scratch]
.endm

.macro STORE_FUNCLET_REGISTER base, reg, scratch
        ldr     \scratch, [\base, #(OFFSETOF__REGDISPLAY__pR4 + (\reg - 4) * 4)]
        str     r\reg, [\scratch]
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        vldr    d\index, [\base, #(OFFSETOF__REGDISPLAY__D + (\index - 8) * 8)]
.endm

.macro STORE_FUNCLET_FP_REGISTER base, index
        vstr    d\index, [\base, #(OFFSETOF__REGDISPLAY__D + (\index - 8) * 8)]
.endm
