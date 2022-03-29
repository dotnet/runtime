// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum GetDefaultProviderFlags : int
        {
            CRYPT_MACHINE_DEFAULT = 0x00000001,
            CRYPT_USER_DEFAULT = 0x00000002
        }

        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "CryptGetDefaultProviderW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptGetDefaultProvider(
            int dwProvType,
            IntPtr pdwReserved,
            GetDefaultProviderFlags dwFlags,
            [Out] char[]? pszProvName,
            ref int pcbProvName);
    }
}
