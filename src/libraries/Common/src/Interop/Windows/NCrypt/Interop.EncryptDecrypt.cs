// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [LibraryImport(Interop.Libraries.NCrypt)]
        internal static unsafe partial ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, ReadOnlySpan<byte> pbInput, int cbInput, void* pPaddingInfo, Span<byte> pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt)]
        internal static unsafe partial ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, ReadOnlySpan<byte> pbInput, int cbInput, void* pPaddingInfo, Span<byte> pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);
    }
}
