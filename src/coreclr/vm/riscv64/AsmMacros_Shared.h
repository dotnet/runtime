// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible


#include "asmconstants.h"
#include "unixasmmacros.inc"

.macro TAILCALL_GC_POLL_RARE
        PREPARE_EXTERNAL_VAR g_pPollGC, t0
        ld      t0, 0(t0)
        jr      t0
.endm

// CallEHFunclet and CallEHFilterFunclet load the preserved registers of the parent frame from the CONTEXT.
// reg and index are the register numbers; the CONTEXT holds the integer and floating point registers in order.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ld      x\reg, (OFFSETOF__CONTEXT__Fp + (\reg - 8) * 8)(\base)
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        fld     f\index, (OFFSETOF__CONTEXT__F + \index * 8)(\base)
.endm
