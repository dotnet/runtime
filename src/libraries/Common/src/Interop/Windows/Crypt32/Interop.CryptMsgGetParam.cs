// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out int pvData,
            [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out CryptMsgType pvData,
            [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            [Out] byte[]? pvData,
            [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            IntPtr pvData,
            [In, Out] ref int pcbData);
    }
}
