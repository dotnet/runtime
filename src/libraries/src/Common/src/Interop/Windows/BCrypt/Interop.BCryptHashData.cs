// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static NTSTATUS BCryptHashData(SafeBCryptHashHandle hHash, ReadOnlySpan<byte> pbInput, int cbInput, int dwFlags) =>
            BCryptHashData(hHash, ref MemoryMarshal.GetReference(pbInput), cbInput, dwFlags);

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        private static extern NTSTATUS BCryptHashData(SafeBCryptHashHandle hHash, ref byte pbInput, int cbInput, int dwFlags);
    }
}
