// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.SecurityCpwl, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateProcessWithLogonW")]
        internal static extern bool CreateProcessWithLogonW(
            string userName,
            string domain,
            string passwordInClearText,
            LogonFlags logonFlags,
            [MarshalAs(UnmanagedType.LPTStr)] string appName,
            StringBuilder cmdLine,
            int creationFlags,
            IntPtr environmentBlock,
            [MarshalAs(UnmanagedType.LPTStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            PROCESS_INFORMATION lpProcessInformation);

        [Flags]
        internal enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }
    }
}
