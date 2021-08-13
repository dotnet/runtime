// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal static NTSTATUS BCryptFinishHash(SafeBCryptHashHandle hHash, Span<byte> pbOutput, int cbOutput, int dwFlags) =>
            BCryptFinishHash(hHash, ref MemoryMarshal.GetReference(pbOutput), cbOutput, dwFlags);

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        private static extern NTSTATUS BCryptFinishHash(SafeBCryptHashHandle hHash, ref byte pbOutput, int cbOutput, int dwFlags);
    }
}
