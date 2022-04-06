// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        internal static partial ulong GetTickCount64();
    }
}
