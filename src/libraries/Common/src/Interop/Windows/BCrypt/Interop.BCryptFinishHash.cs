// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        internal static partial NTSTATUS BCryptFinishHash(SafeBCryptHashHandle hHash, Span<byte> pbOutput, int cbOutput, int dwFlags);
    }
}
