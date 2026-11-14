// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible


#include "asmconstants.h"
#include "unixasmmacros.inc"

.macro TAILCALL_GC_POLL_RARE
        PREPARE_EXTERNAL_VAR_INDIRECT g_pPollGC, r2
        EPILOG_BRANCH_REG r2
.endm

// CallEHFunclet and CallEHFilterFunclet load the preserved registers of the parent frame from the CONTEXT.
// reg and index are the register numbers; the CONTEXT holds the integer and floating point registers in order.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ldr     r\reg, [\base, #(OFFSETOF__CONTEXT__R4 + (\reg - 4) * 4)]
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        vldr    d\index, [\base, #(OFFSETOF__CONTEXT__D + \index * 8)]
.endm
