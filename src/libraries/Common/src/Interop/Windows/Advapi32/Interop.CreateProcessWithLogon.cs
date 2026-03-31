// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "CreateProcessWithLogonW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CreateProcessWithLogonW(
            string userName,
            string domain,
            IntPtr password,
            LogonFlags logonFlags,
            string? appName,
            char* cmdLine,
            int creationFlags,
            IntPtr environmentBlock,
            string? lpCurrentDirectory,
            Interop.Kernel32.STARTUPINFO* lpStartupInfo,
            Interop.Kernel32.PROCESS_INFORMATION* lpProcessInformation);

        [LibraryImport(Libraries.Advapi32, EntryPoint = "LogonUserW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LogonUser(
            string userName,
            string domain,
            IntPtr password,
            int logonType,
            int logonProvider,
            out SafeTokenHandle token);

        // Logon types for LogonUser (dwLogonType parameter)
        internal const int LOGON32_LOGON_INTERACTIVE = 2;
        internal const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

        [Flags]
        internal enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }
    }
}
