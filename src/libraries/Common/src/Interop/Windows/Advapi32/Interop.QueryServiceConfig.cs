// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "QueryServiceConfigW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool QueryServiceConfig(SafeServiceHandle serviceHandle, IntPtr queryServiceConfigPtr, int bufferSize, out int bytesNeeded);

    }
}
