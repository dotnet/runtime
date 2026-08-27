// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class Wtsapi32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Wtsapi32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool WTSUnRegisterSessionNotification(IntPtr hWnd);
    }
}
