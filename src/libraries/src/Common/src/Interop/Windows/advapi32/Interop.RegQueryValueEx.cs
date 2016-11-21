// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
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
            string lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out] byte[] lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            string lpValueName,
            int[] lpReserved,
            ref int lpType,
            ref int lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            ref long lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out] char[] lpData,
            ref int lpcbData);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryValueExW")]
        internal static extern int RegQueryValueEx(
            SafeRegistryHandle hKey,
            String lpValueName,
            int[] lpReserved,
            ref int lpType,
            [Out]StringBuilder lpData,
            ref int lpcbData);
    }
}
