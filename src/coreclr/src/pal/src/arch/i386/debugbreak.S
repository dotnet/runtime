// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

LEAF_ENTRY DBG_DebugBreak, _TEXT
        int3
        ret
LEAF_END_MARKED DBG_DebugBreak, _TEXT

