// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
    // keep in sync with GC_ALLOC_FLAGS in gcinterface.h
    [Flags]
    internal enum GC_ALLOC_FLAGS
    {
        GC_ALLOC_NO_FLAGS = 0,
        GC_ALLOC_ZEROING_OPTIONAL = 16,
        GC_ALLOC_PINNED_OBJECT_HEAP = 64,
    };
}
