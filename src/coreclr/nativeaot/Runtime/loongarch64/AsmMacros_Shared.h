// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.macro TAILCALL_GC_POLL_RARE
        b       C_FUNC(RhpGcPollRare)
.endm

// CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY.
// reg is the register number; the REGDISPLAY holds the integer registers in order and fs0-fs7 (f24-f31) in F.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ld.d    \scratch, \base, (OFFSETOF__REGDISPLAY__pFP + (\reg - 22) * 8)
        ld.d    $r\reg, \scratch, 0
.endm

.macro STORE_FUNCLET_REGISTER base, reg, scratch
        ld.d    \scratch, \base, (OFFSETOF__REGDISPLAY__pFP + (\reg - 22) * 8)
        st.d    $r\reg, \scratch, 0
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        fld.d   $f\index, \base, (OFFSETOF__REGDISPLAY__F + (\index - 24) * 8)
.endm

.macro STORE_FUNCLET_FP_REGISTER base, index
        fst.d   $f\index, \base, (OFFSETOF__REGDISPLAY__F + (\index - 24) * 8)
.endm
