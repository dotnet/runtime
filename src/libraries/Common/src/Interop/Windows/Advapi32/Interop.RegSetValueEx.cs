// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int Reserved,
            int dwType,
            byte[]? lpData,
            int cbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int Reserved,
            int dwType,
            char[]? lpData,
            int cbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int Reserved,
            int dwType,
            ref int lpData,
            int cbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int Reserved,
            int dwType,
            ref long lpData,
            int cbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            string? lpValueName,
            int Reserved,
            int dwType,
            string? lpData,
            int cbData);
    }
}
