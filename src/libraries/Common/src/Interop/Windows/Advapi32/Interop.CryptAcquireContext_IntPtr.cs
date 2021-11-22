// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "CryptAcquireContextW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CryptAcquireContext(
            out IntPtr psafeProvHandle,
            char* pszContainer,
            char* pszProvider,
            int dwProvType,
            Interop.Crypt32.CryptAcquireContextFlags dwFlags);
    }
}
