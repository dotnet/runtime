// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

// void* PacStripPtr(void *);
// This function strips the pointer of PAC info that is passed as an argument.
// We prefer to strip a pointer where it's not going to be used to branch execution to.
.arch_extension pauth
    LEAF_ENTRY PacStripPtr, _TEXT
        xpaci x0
        ret
    LEAF_END PacStripPtr, _TEXT

// void* PacSignPtr(void *, void *);
// This function sign the input pointer using zero as salt.
// Thus we need to move input in lr, sign it and then copy it back to the result register.
.arch_extension pauth
    LEAF_ENTRY PacSignPtr, _TEXT
        mov x17, x0
        mov x16, x1
        pacia1716
        mov x0, x17
        ret
    LEAF_END PacSignPtr, _TEXT
