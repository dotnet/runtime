// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            byte[] lpData,
            int cbData);

        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            char[] lpData,
            int cbData);

        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            ref int lpData,
            int cbData);

        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            ref long lpData,
            int cbData);

        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            String lpData,
            int cbData);
    }
}
