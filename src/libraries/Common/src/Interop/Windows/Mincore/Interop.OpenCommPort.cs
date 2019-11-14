// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreComm_L1_1_1, SetLastError = true)]
        internal static extern SafeFileHandle OpenCommPort(
             uint uPortNumber,
             int dwDesiredAccess,
             int dwFlagsAndAttributes);
    }
}
