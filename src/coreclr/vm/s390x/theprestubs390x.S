// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
        stmg    %r2,%r15,16(%r15)
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
        lay     %r15,-160(%r15)
        .cfi_def_cfa_offset 320

        std     %f0,288(%r15)
        std     %f2,296(%r15)
        std     %f4,304(%r15)
        std     %f6,312(%r15)

        la      %r2,160(%r15)  // pTransitionBlock*
        lgr     %r3,METHODDESC_REGISTER
        brasl   %r14,C_FUNC(PreStubWorker)
        lgr     %r1,%r2

        ld %f0, 288(%r15)
        ld %f2, 296(%r15)
        ld %f4, 304(%r15)
        ld %f6, 312(%r15)

        lmg     %r2,%r15,176(%r15)
        br      %r1
NESTED_END ThePreStub, _TEXT

LEAF_ENTRY ThePreStubPatch, _TEXT
        // make sure that the basic block is unique
        nopr %r7
PATCH_LABEL ThePreStubPatchLabel
        br %r14
LEAF_END ThePreStubPatch, _TEXT
