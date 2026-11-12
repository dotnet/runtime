// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal const int LOGON32_LOGON_INTERACTIVE = 2;
        internal const int LOGON32_PROVIDER_DEFAULT = 0;

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Advapi32, EntryPoint = "LogonUserW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LogonUser(
            string username,
            string? domain,
            IntPtr password,
            int logonType,
            int logonProvider,
            out SafeTokenHandle token);
    }
}
