// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegCreateKeyExW")]
        internal static extern int RegCreateKeyEx(
            SafeRegistryHandle hKey,
            String lpSubKey,
            int Reserved,
            String lpClass,
            int dwOptions,
            int samDesired,
            ref SECURITY_ATTRIBUTES secAttrs,
            out SafeRegistryHandle hkResult,
            out int lpdwDisposition);
    }
}
