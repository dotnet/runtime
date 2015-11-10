//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "unixasmmacros.inc"

.syntax unified
.thumb

LEAF_ENTRY DBG_DebugBreak, _TEXT
    EMIT_BREAKPOINT
    bx lr
LEAF_END DBG_DebugBreak, _TEXT

