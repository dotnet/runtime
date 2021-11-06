// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgGetParam(
#else
        [DllImport(Libraries.Crypt32, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
#endif
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out int pvData,
            ref int pcbData);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static unsafe partial bool CryptMsgGetParam(
#else
        [DllImport(Libraries.Crypt32, SetLastError = true)]
        internal static unsafe extern bool CryptMsgGetParam(
#endif
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            byte* pvData,
            ref int pcbData);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgGetParam(
#else
        [DllImport(Libraries.Crypt32, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
#endif
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            out CryptMsgType pvData,
            ref int pcbData);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial bool CryptMsgGetParam(
#else
        [DllImport(Libraries.Crypt32, SetLastError = true)]
        internal static extern bool CryptMsgGetParam(
#endif
            SafeCryptMsgHandle hCryptMsg,
            CryptMsgParamType dwParamType,
            int dwIndex,
            IntPtr pvData,
            ref int pcbData);
    }
}
