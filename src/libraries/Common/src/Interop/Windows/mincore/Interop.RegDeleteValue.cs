// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.REGISTRY_L1_APISET, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegDeleteValueW")]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);
    }
}
