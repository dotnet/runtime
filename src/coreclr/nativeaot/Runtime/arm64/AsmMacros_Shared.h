// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#ifdef TARGET_WINDOWS
#include "AsmMacros.h"

#define G_TRAP_THREADS RhpTrapThreads

    EXTERN RhpGcPollRare

    MACRO
        TAILCALL_GC_POLL_RARE

        b       RhpGcPollRare
    MEND

;; CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY.
;; $reg is the register number: 19-28 for x19-x28, which are followed by fp (29) in the REGDISPLAY.
    MACRO
        LOAD_FUNCLET_REGISTER $base, $reg, $scratch

        ldr     $scratch, [$base, #(OFFSETOF__REGDISPLAY__pX19 + ($reg - 19) * 8)]
        ldr     x$reg, [$scratch]
    MEND

    MACRO
        STORE_FUNCLET_REGISTER $base, $reg, $scratch

        ldr     $scratch, [$base, #(OFFSETOF__REGDISPLAY__pX19 + ($reg - 19) * 8)]
        str     x$reg, [$scratch]
    MEND

    MACRO
        LOAD_FUNCLET_FP_REGISTER $base, $index

        ldr     d$index, [$base, #(OFFSETOF__REGDISPLAY__D + ($index - 8) * 8)]
    MEND

    MACRO
        STORE_FUNCLET_FP_REGISTER $base, $index

        str     d$index, [$base, #(OFFSETOF__REGDISPLAY__D + ($index - 8) * 8)]
    MEND

#else
#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.macro TAILCALL_GC_POLL_RARE
        b       C_FUNC(RhpGcPollRare)
.endm

// CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY.
// reg is the register number: 19-28 for x19-x28, which are followed by fp (29) in the REGDISPLAY.
.macro LOAD_FUNCLET_REGISTER base, reg, scratch
        ldr     \scratch, [\base, #(OFFSETOF__REGDISPLAY__pX19 + (\reg - 19) * 8)]
        ldr     x\reg, [\scratch]
.endm

.macro STORE_FUNCLET_REGISTER base, reg, scratch
        ldr     \scratch, [\base, #(OFFSETOF__REGDISPLAY__pX19 + (\reg - 19) * 8)]
        str     x\reg, [\scratch]
.endm

.macro LOAD_FUNCLET_FP_REGISTER base, index
        ldr     d\index, [\base, #(OFFSETOF__REGDISPLAY__D + (\index - 8) * 8)]
.endm

.macro STORE_FUNCLET_FP_REGISTER base, index
        str     d\index, [\base, #(OFFSETOF__REGDISPLAY__D + (\index - 8) * 8)]
.endm
#endif
