// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"

.syntax unified
.thumb

LEAF_ENTRY DBG_DebugBreak, _TEXT
    EMIT_BREAKPOINT
    bx lr
LEAF_END_MARKED DBG_DebugBreak, _TEXT

