// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if REGISTRY_ASSEMBLY
using Microsoft.Win32.SafeHandles;
#else
using Internal.Win32.SafeHandles;
#endif
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegOpenKeyEx(
#else
        [DllImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int RegOpenKeyEx(
#endif
            SafeRegistryHandle hKey,
            string? lpSubKey,
            int ulOptions,
            int samDesired,
            out SafeRegistryHandle hkResult);


#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial int RegOpenKeyEx(
#else
        [DllImport(Libraries.Advapi32, EntryPoint = "RegOpenKeyExW", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int RegOpenKeyEx(
#endif
            IntPtr hKey,
            string? lpSubKey,
            int ulOptions,
            int samDesired,
            out SafeRegistryHandle hkResult);
    }
}
