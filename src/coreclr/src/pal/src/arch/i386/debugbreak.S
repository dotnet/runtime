//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

.intel_syntax noprefix
#include "unixasmmacros.inc"

LEAF_ENTRY DBG_DebugBreak, _TEXT
        int3
        ret
LEAF_END DBG_DebugBreak, _TEXT

