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

;; CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY
    MACRO
        LOAD_FUNCLET_REGISTERS $regs, $scratch

        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX19]
        ldr     x19, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX20]
        ldr     x20, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX21]
        ldr     x21, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX22]
        ldr     x22, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX23]
        ldr     x23, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX24]
        ldr     x24, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX25]
        ldr     x25, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX26]
        ldr     x26, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX27]
        ldr     x27, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX28]
        ldr     x28, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pFP]
        ldr     fp, [$scratch]

        add     $scratch, $regs, #OFFSETOF__REGDISPLAY__D
        ldp     d8, d9, [$scratch, #0x00]
        ldp     d10, d11, [$scratch, #0x10]
        ldp     d12, d13, [$scratch, #0x20]
        ldp     d14, d15, [$scratch, #0x30]
    MEND

    MACRO
        STORE_FUNCLET_REGISTERS $regs, $scratch

        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX19]
        str     x19, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX20]
        str     x20, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX21]
        str     x21, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX22]
        str     x22, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX23]
        str     x23, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX24]
        str     x24, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX25]
        str     x25, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX26]
        str     x26, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX27]
        str     x27, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pX28]
        str     x28, [$scratch]
        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pFP]
        str     fp, [$scratch]

        add     $scratch, $regs, #OFFSETOF__REGDISPLAY__D
        stp     d8, d9, [$scratch, #0x00]
        stp     d10, d11, [$scratch, #0x10]
        stp     d12, d13, [$scratch, #0x20]
        stp     d14, d15, [$scratch, #0x30]
    MEND

    MACRO
        LOAD_FUNCLET_FRAME_POINTER $regs, $scratch

        ldr     $scratch, [$regs, #OFFSETOF__REGDISPLAY__pFP]
        ldr     fp, [$scratch]
    MEND

#else
#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

.macro TAILCALL_GC_POLL_RARE
        b       C_FUNC(RhpGcPollRare)
.endm

// CallEHFunclet loads and writes back the preserved registers of the parent frame through the REGDISPLAY
.macro LOAD_FUNCLET_REGISTERS regs, scratch
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX19]
        ldr     x19, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX20]
        ldr     x20, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX21]
        ldr     x21, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX22]
        ldr     x22, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX23]
        ldr     x23, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX24]
        ldr     x24, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX25]
        ldr     x25, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX26]
        ldr     x26, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX27]
        ldr     x27, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX28]
        ldr     x28, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pFP]
        ldr     fp, [\scratch]

        add     \scratch, \regs, #OFFSETOF__REGDISPLAY__D
        ldp     d8, d9, [\scratch, #0x00]
        ldp     d10, d11, [\scratch, #0x10]
        ldp     d12, d13, [\scratch, #0x20]
        ldp     d14, d15, [\scratch, #0x30]
.endm

.macro STORE_FUNCLET_REGISTERS regs, scratch
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX19]
        str     x19, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX20]
        str     x20, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX21]
        str     x21, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX22]
        str     x22, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX23]
        str     x23, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX24]
        str     x24, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX25]
        str     x25, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX26]
        str     x26, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX27]
        str     x27, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pX28]
        str     x28, [\scratch]
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pFP]
        str     fp, [\scratch]

        add     \scratch, \regs, #OFFSETOF__REGDISPLAY__D
        stp     d8, d9, [\scratch, #0x00]
        stp     d10, d11, [\scratch, #0x10]
        stp     d12, d13, [\scratch, #0x20]
        stp     d14, d15, [\scratch, #0x30]
.endm

.macro LOAD_FUNCLET_FRAME_POINTER regs, scratch
        ldr     \scratch, [\regs, #OFFSETOF__REGDISPLAY__pFP]
        ldr     fp, [\scratch]
.endm
#endif
