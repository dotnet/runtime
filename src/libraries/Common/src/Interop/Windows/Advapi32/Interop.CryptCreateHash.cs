// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [Flags]
        internal enum CryptCreateHashFlags : int
        {
            None = 0,
        }

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CryptCreateHash(
            SafeProvHandle hProv,
            int Algid,
            SafeCapiKeyHandle hKey,
            CryptCreateHashFlags dwFlags,
            out SafeHashHandle phHash);
    }
}
