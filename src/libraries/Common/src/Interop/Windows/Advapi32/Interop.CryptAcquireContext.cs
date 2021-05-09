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
        internal enum CryptAcquireContextFlags : uint
        {
            None = 0x00000000,
            CRYPT_NEWKEYSET = 0x00000008,         // CRYPT_NEWKEYSET
            CRYPT_DELETEKEYSET = 0x00000010,      // CRYPT_DELETEKEYSET
            CRYPT_MACHINE_KEYSET = 0x00000020,    // CRYPT_MACHINE_KEYSET
            CRYPT_SILENT = 0x00000040,            // CRYPT_SILENT
            CRYPT_VERIFYCONTEXT = 0xF0000000      // CRYPT_VERIFYCONTEXT
        }

        [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW")]
        public static extern bool CryptAcquireContext(
            out SafeProvHandle phProv,
            string? szContainer,
            string? szProvider,
            int dwProvType,
            uint dwFlags);
    }
}
