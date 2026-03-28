// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.BCrypt)]
        internal static partial NTSTATUS BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);
    }
}
