// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegEnumValueW")]
        internal unsafe static extern int RegEnumValue(
            SafeRegistryHandle hKey,
            int dwIndex,
            char* lpValueName,
            ref int lpcbValueName,
            IntPtr lpReserved_MustBeZero,
            int[] lpType,
            byte[] lpData,
            int[] lpcbData);
    }
}
