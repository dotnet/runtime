// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateService(SafeServiceHandle databaseHandle, string serviceName, string displayName, int access, int serviceType,
            int startType, int errorControl, string binaryPath, string loadOrderGroup, IntPtr pTagId, string dependencies,
            string servicesStartName, string password);

    }
}
