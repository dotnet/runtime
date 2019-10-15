// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include <unixasmmacros.inc>

LEAF_ENTRY FPFillR8, _TEXT
            movdqa  xmm0, xmmword ptr [rdi]
            ret
LEAF_END FPFillR8, _TEXT
