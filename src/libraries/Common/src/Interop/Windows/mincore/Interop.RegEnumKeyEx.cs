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
        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegEnumKeyExW")]
        internal unsafe static extern int RegEnumKeyEx(
            SafeRegistryHandle hKey,
            int dwIndex,
            char* lpName,
            ref int lpcbName,
            int[] lpReserved,
            [Out]StringBuilder lpClass,
            int[] lpcbClass,
            long[] lpftLastWriteTime);
    }
}
