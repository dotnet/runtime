// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptMsgControl(
            SafeCryptMsgHandle hCryptMsg,
            int dwFlags,
            MsgControlType dwCtrlType,
            ref CMSG_CTRL_DECRYPT_PARA pvCtrlPara);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptMsgControl(
            SafeCryptMsgHandle hCryptMsg,
            int dwFlags,
            MsgControlType dwCtrlType,
            ref CMSG_CTRL_KEY_AGREE_DECRYPT_PARA pvCtrlPara);
    }
}
