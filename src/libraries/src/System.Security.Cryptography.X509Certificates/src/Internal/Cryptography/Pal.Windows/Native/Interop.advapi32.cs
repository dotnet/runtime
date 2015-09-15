// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Internal.Cryptography.Pal.Native;

internal static partial class Interop
{
    public static class advapi32
    {
#if !NETNATIVE
        [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern unsafe bool CryptAcquireContext(out IntPtr psafeProvHandle, char* pszContainer, char* pszProvider, int dwProvType, CryptAcquireContextFlags dwFlags);
#endif
    }
}

