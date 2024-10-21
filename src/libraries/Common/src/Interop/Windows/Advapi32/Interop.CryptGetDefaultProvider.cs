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

        [LibraryImport(Libraries.Advapi32, EntryPoint = "CryptGetDefaultProviderW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CryptGetDefaultProvider(
            int dwProvType,
            IntPtr pdwReserved,
            GetDefaultProviderFlags dwFlags,
            [Out] char[]? pszProvName,
            ref int pcbProvName);
    }
}
