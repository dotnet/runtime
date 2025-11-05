// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// METHODDESC_REGISTER: UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
        // We need to save argument registers and %r14-%15.
        // For simplicity we just save everything right now.
        stmg %r2, %r15, 16(%r15)
        .cfi_offset 6, -112
        .cfi_offset 7, -104
        .cfi_offset 8, -96
        .cfi_offset 9, -88
        .cfi_offset 10, -80
        .cfi_offset 11, -72
        .cfi_offset 12, -64
        .cfi_offset 13, -56
        .cfi_offset 14, -48
        .cfi_offset 15, -40
        lay %r15, -160(%r15)
        .cfi_def_cfa_offset 320

        // Save floating-point argument registers.
        std %f0, 288(%r15)
        std %f2, 296(%r15)
        std %f4, 304(%r15)
        std %f6, 312(%r15)

        // Call TheUMEntryPrestubWorker with the UMEntryThunk*
        lgr %r2, METHODDESC_REGISTER
        brasl %r14, C_FUNC(TheUMEntryPrestubWorker)

        // Native target adress now in %r2.
        lgr %r1, %r2

        // Restore floating-point argument registers.
        ld %f0, 288(%r15)
        ld %f2, 296(%r15)
        ld %f4, 304(%r15)
        ld %f6, 312(%r15)

        // Restore integer parameter and call-saved registers.
        lmg %r2, %r15, 176(%r15)
        .cfi_restore 15
        .cfi_restore 14
        .cfi_restore 13
        .cfi_restore 12
        .cfi_restore 11
        .cfi_restore 10
        .cfi_restore 9
        .cfi_restore 8
        .cfi_restore 7
        .cfi_restore 6
        .cfi_def_cfa 15, 160

        // Tail call native target.
        br %r1
NESTED_END TheUMEntryPrestub, _TEXT
