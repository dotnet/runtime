// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            [Out] byte[]? lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            ref int lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            ref long lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            [Out] char[]? lpData,
            ref int lpcbData);
    }
}
