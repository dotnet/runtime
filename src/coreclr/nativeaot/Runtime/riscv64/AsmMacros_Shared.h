// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.macro TAILCALL_GC_POLL_RARE
        tail    C_FUNC(RhpGcPollRare)
.endm

// CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY.
// reg and index are the register numbers; the REGDISPLAY holds the integer and floating point registers in order.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ld      \scratch, (OFFSETOF__REGDISPLAY__pFP + (\reg - 8) * 8)(\base)
        ld      x\reg, 0(\scratch)
.endm

.macro STORE_FUNCLET_REGISTER base, reg, scratch
        ld      \scratch, (OFFSETOF__REGDISPLAY__pFP + (\reg - 8) * 8)(\base)
        sd      x\reg, 0(\scratch)
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        fld     f\index, (OFFSETOF__REGDISPLAY__F + \index * 8)(\base)
.endm

.macro STORE_FUNCLET_FP_REGISTER base, index
        fsd     f\index, (OFFSETOF__REGDISPLAY__F + \index * 8)(\base)
.endm
