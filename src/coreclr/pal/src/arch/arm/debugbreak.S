// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

.syntax unified
#ifndef __armv6__
.thumb
#endif

LEAF_ENTRY DBG_DebugBreak, _TEXT
    EMIT_BREAKPOINT
    bx lr
LEAF_END_MARKED DBG_DebugBreak, _TEXT

