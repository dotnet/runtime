// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

LEAF_ENTRY DBG_DebugBreak, _TEXT
        .word 0x0001
        br %r14
LEAF_END_MARKED DBG_DebugBreak, _TEXT

