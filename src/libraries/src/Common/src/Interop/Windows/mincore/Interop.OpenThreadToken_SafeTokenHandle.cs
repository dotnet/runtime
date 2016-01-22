// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.ProcessThread_L1, SetLastError = true)]
        internal static extern bool OpenThreadToken(IntPtr ThreadHandle, TokenAccessLevels dwDesiredAccess, bool bOpenAsSelf, out SafeTokenHandle phThreadToken);
    }
}
