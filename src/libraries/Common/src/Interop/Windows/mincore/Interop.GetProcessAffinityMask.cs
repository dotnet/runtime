// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ProcessTopology, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        internal static extern bool GetProcessAffinityMask(SafeProcessHandle handle, out IntPtr processMask, out IntPtr systemMask);
    }
}
