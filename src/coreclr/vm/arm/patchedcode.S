// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

// ------------------------------------------------------------------
// Start of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        bx      lr
    LEAF_END JIT_PatchedCodeStart, _TEXT

// ------------------------------------------------------------------
// GC write barrier support.
//
// GC Write barriers are defined in asmhelpers.asm. The following functions are used to define
// patchable location where the write-barriers are copied over at runtime

    LEAF_ENTRY JIT_PatchedWriteBarrierStart, _TEXT
    LEAF_END JIT_PatchedWriteBarrierStart, _TEXT

    // These write barriers are overwritten on the fly
    // See ValidateWriteBarriers on how the sizes of these should be calculated
        .align 4
    LEAF_ENTRY JIT_WriteBarrier, _TEXT
    .space (0x84)
    LEAF_END_MARKED JIT_WriteBarrier, _TEXT

        .align 4
    LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT
    .space (0x9C)
    LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT

        .align 4
    LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
    .space (0xA0)
    LEAF_END_MARKED JIT_ByRefWriteBarrier , _TEXT

    LEAF_ENTRY JIT_PatchedWriteBarrierLast, _TEXT
    LEAF_END JIT_PatchedWriteBarrierLast, _TEXT

// ------------------------------------------------------------------
// End of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        bx      lr
    LEAF_END JIT_PatchedCodeLast, _TEXT
