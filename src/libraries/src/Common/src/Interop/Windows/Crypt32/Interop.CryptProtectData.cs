// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class Crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptProtectData(
                  [In] ref DATA_BLOB pDataIn,
                  [In] string szDataDescr,
                  [In] ref DATA_BLOB pOptionalEntropy,
                  [In] IntPtr pvReserved,
                  [In] IntPtr pPromptStruct,
                  [In] CryptProtectDataFlags dwFlags,
                  [Out] out DATA_BLOB pDataOut);
    }
}
