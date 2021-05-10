// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS BCryptOpenAlgorithmProvider(out SafeBCryptAlgorithmHandle phAlgorithm, string pszAlgId, string? pszImplementation, BCryptOpenAlgorithmProviderFlags dwFlags);

        [Flags]
        internal enum BCryptOpenAlgorithmProviderFlags : int
        {
            None = 0x00000000,
            BCRYPT_ALG_HANDLE_HMAC_FLAG = 0x00000008,
        }
    }
}
