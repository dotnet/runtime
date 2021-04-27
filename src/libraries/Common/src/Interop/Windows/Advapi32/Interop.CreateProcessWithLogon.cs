// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateProcessWithLogonW")]
        internal static extern unsafe bool CreateProcessWithLogonW(
            string userName,
            string domain,
            IntPtr password,
            LogonFlags logonFlags,
            string? appName,
            char* cmdLine,
            int creationFlags,
            IntPtr environmentBlock,
            string? lpCurrentDirectory,
            ref Interop.Kernel32.STARTUPINFO lpStartupInfo,
            ref Interop.Kernel32.PROCESS_INFORMATION lpProcessInformation);

        [Flags]
        internal enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }
    }
}
