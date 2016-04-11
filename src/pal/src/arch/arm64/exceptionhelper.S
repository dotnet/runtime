// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"

LEAF_ENTRY ThrowExceptionFromContextInternal, _TEXT
    EMIT_BREAKPOINT
LEAF_END ThrowExceptionFromContextInternal, _TEXT
