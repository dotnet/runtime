// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            byte[]? lpData,
            ref int lpcbData);

        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            ref int lpData,
            ref int lpcbData);

        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            ref long lpData,
            ref int lpcbData);

        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int[]? lpReserved,
            ref int lpType,
            [Out] char[]? lpData,
            ref int lpcbData);
    }
}
