// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static int CopyFile(string src, string dst, bool failIfExists)
        {
            int copyFlags = failIfExists ? Interop.Kernel32.FileOperations.COPY_FILE_FAIL_IF_EXISTS : 0;
            int cancel = 0;
            if (!Interop.Kernel32.CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, copyFlags))
            {
                return Marshal.GetLastWin32Error();
            }

            return Interop.Errors.ERROR_SUCCESS;
        }
    }
}
