// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptUnprotectData(
                  [In] ref DATA_BLOB pDataIn,
                  [In] IntPtr ppszDataDescr,
                  [In] ref DATA_BLOB pOptionalEntropy,
                  [In] IntPtr pvReserved,
                  [In] IntPtr pPromptStruct,
                  [In] CryptProtectDataFlags dwFlags,
                  [Out] out DATA_BLOB pDataOut);
    }
}
