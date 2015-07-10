// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        internal static int CopyFile(string src, string dst, bool failIfExists)
        {
            int copyFlags = failIfExists ? Interop.mincore.FileOperations.COPY_FILE_FAIL_IF_EXISTS : 0;
            int cancel = 0;
            if (!Interop.mincore.CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, copyFlags))
            {
                return Marshal.GetLastWin32Error();
            }

            return Interop.mincore.Errors.ERROR_SUCCESS;
        }
    }
}
