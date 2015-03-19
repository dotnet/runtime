// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Handle, SetLastError = true, BestFitMapping = false)]
        internal static extern bool DuplicateHandle(
            SafeProcessHandle hSourceProcessHandle,
            SafeHandle hSourceHandle,
            SafeProcessHandle hTargetProcess,
            out SafeFileHandle targetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

        [DllImport(Libraries.Handle, SetLastError = true, BestFitMapping = false)]
        internal static extern bool DuplicateHandle(
            SafeProcessHandle hSourceProcessHandle,
            SafeHandle hSourceHandle,
            SafeProcessHandle hTargetProcess,
            out SafeWaitHandle targetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

    }
}
