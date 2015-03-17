// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Psapi_Obsolete, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "K32EnumProcessModules")]
        internal static extern bool EnumProcessModules(SafeProcessHandle handle, IntPtr modules, int size, ref int needed);
    }
}
