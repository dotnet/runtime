// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Kernel32_L1, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        internal static extern int SetThreadIdealProcessor(SafeThreadHandle handle, int processor);
    }
}
