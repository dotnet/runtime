// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, EntryPoint = "EnumServicesStatusExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumServicesStatusEx(
            SafeServiceHandle databaseHandle,
            int infolevel,
            int serviceType,
            int serviceState,
            IntPtr status,
            int size,
            out int bytesNeeded,
            out int servicesReturned,
            ref int resumeHandle,
            string? group);

    }
}
