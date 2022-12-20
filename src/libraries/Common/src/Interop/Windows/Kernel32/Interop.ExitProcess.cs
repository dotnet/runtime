// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        [DoesNotReturn]
        [LibraryImport(Libraries.Kernel32, EntryPoint = "ExitProcess")]
        internal static partial void ExitProcess(int exitCode);
    }
}
