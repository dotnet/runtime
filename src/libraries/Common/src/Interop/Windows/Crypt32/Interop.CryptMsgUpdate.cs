// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgUpdate(SafeCryptMsgHandle hCryptMsg, byte[] pbData, int cbData, bool fFinal);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgUpdate(SafeCryptMsgHandle hCryptMsg, IntPtr pbData, int cbData, bool fFinal);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgUpdate(SafeCryptMsgHandle hCryptMsg, ref byte pbData, int cbData, bool fFinal);
    }
}
