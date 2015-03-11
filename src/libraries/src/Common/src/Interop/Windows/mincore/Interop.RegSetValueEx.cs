// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            byte[] lpData,
            int cbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            char[] lpData,
            int cbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            ref int lpData,
            int cbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            ref long lpData,
            int cbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegSetValueExW")]
        internal static extern int RegSetValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int Reserved,
            RegistryValueKind dwType,
            String lpData,
            int cbData);
    }
}
