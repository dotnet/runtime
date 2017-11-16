// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;


using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

internal static partial class Interop
{
    public static partial class cryptoapi
    {
        [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern unsafe bool CryptAcquireContext(out IntPtr psafeProvHandle, char* pszContainer, char* pszProvider, int dwProvType, CryptAcquireContextFlags dwFlags);
    }
}


