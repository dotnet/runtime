// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out] byte[] lpData,
            ref int lpcbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string lpValueName,
            int[] lpReserved,
            ref int lpType,
            ref int lpData,
            ref int lpcbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            ref long lpData,
            ref int lpcbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out] char[] lpData,
            ref int lpcbData);

        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out]StringBuilder lpData,
            ref int lpcbData);
    }
}
