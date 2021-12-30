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
        internal static partial bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out int pvData,
            ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static unsafe partial bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            byte* pvData,
            ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out CryptMsgType pvData,
            ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgGetParam(
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            IntPtr pvData,
            ref int pcbData);
    }
}
