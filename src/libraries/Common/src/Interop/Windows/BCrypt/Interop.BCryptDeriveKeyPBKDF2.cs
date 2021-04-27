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
        internal static extern unsafe NTSTATUS BCryptDeriveKeyPBKDF2(
            SafeBCryptAlgorithmHandle hPrf,
            byte* pbPassword,
            int cbPassword,
            byte* pbSalt,
            int cbSalt,
            ulong cIterations,
            byte* pbDerivedKey,
            int cbDerivedKey,
            uint dwFlags);
    }
}
