// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.ProcessThread_L1, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr ProcessToken, TokenAccessLevels DesiredAccess, out SafeAccessTokenHandle TokenHandle);
    }
}
