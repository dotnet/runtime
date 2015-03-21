// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.IO, SetLastError = true)]
        internal static unsafe extern bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);
    }
}
