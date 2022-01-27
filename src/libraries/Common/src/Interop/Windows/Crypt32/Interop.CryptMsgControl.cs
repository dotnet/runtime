// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we add support for non-blittable struct marshalling.
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgControl(
            SafeCryptMsgHandle hCryptMsg,
            int dwFlags,
            MsgControlType dwCtrlType,
            ref CMSG_CTRL_DECRYPT_PARA pvCtrlPara);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CryptMsgControl(
            SafeCryptMsgHandle hCryptMsg,
            int dwFlags,
            MsgControlType dwCtrlType,
            ref CMSG_CTRL_KEY_AGREE_DECRYPT_PARA pvCtrlPara);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    }
}
