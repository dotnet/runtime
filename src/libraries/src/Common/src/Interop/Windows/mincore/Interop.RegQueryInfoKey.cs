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
        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegQueryInfoKeyW")]
        internal static extern int RegQueryInfoKey(
            SafeRegistryHandle hKey,
            [Out]StringBuilder lpClass,
            int[] lpcbClass,
            IntPtr lpReserved_MustBeZero,
            ref int lpcSubKeys,
            int[] lpcbMaxSubKeyLen,
            int[] lpcbMaxClassLen,
            ref int lpcValues,
            int[] lpcbMaxValueNameLen,
            int[] lpcbMaxValueLen,
            int[] lpcbSecurityDescriptor,
            int[] lpftLastWriteTime);
    }
}
