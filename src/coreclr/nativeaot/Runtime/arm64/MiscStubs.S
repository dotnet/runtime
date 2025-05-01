// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

// void* PacStripPtr(void *);
// This function strips the pointer of PAC info that is passed as an agrument.
// To avoid failing on non-PAC enabled machines, we use xpaclri (instead of xpaci) which strips lr explicitly.
// Thus we move need to move input in lr, strip it and copy it back to the result register.
.arch_extension pauth
    LEAF_ENTRY PacStripPtr, _TEXT
        mov x9, lr
        mov lr, x0
        xpaclri
        mov x0, lr
        ret x9
    LEAF_END PacStripPtr, _TEXT

// void* PacSignPtr(void *);
// This function sign the input pointer using zero as salt.
// To avoid failing on non-PAC enabled machines, we use paciaz (instead of paciza) which signs lr explicitly.
// Thus we need to move input in lr, sign it and then copy it back to the result register.
.arch_extension pauth
    LEAF_ENTRY PacSignPtr, _TEXT
        mov x9, lr
        mov lr, x0
        paciaz
        mov x0, lr
        ret x9
    LEAF_END PacSignPtr, _TEXT