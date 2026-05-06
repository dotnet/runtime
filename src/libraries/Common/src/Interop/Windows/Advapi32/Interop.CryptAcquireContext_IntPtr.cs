// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "CryptAcquireContextW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CryptAcquireContext(
            out IntPtr psafeProvHandle,
            char* pszContainer,
            char* pszProvider,
            int dwProvType,
            Interop.Crypt32.CryptAcquireContextFlags dwFlags);
    }
}
