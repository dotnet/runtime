// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Managed structure used by GCHeapHash in CLR to provide a hashtable manipulated
    /// by C++ runtime code which manages its memory in the GC heap.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class GCHeapHash
    {
        Array _data = null!;
        int _count;
        int _deletedCount;
    }
}
