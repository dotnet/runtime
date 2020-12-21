// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal enum GetDefaultProviderFlags : int
        {
            CRYPT_MACHINE_DEFAULT = 0x00000001,
            CRYPT_USER_DEFAULT = 0x00000002
        }

        [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptGetDefaultProviderW")]
        public static extern bool CryptGetDefaultProvider(
            int dwProvType,
            IntPtr pdwReserved,
            GetDefaultProviderFlags dwFlags,
#pragma warning disable CA1838 // not on a hot path
            [Out] StringBuilder? pszProvName,
#pragma warning restore CA1838
            ref int pcbProvName);
    }
}
