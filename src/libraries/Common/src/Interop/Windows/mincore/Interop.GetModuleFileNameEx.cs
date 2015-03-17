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
        [DllImport(Libraries.Psapi_Obsolete, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "K32GetModuleFileNameExW")]
        internal static extern int GetModuleFileNameEx(SafeProcessHandle processHandle, IntPtr moduleHandle, StringBuilder baseName, int size);
    }
}
