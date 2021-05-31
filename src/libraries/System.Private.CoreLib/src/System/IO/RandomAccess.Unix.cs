// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        internal static unsafe long GetFileLength(SafeFileHandle handle, string? path)
        {
            int result = Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(returnCode, path);
            return status.Size
        }

        private static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return FileStreamHelpers.CheckFileCall(
                    Interop.Sys.Pread(handle, bufPtr, buffer.Length, fileOffset),
                    path: null);
            }
        }

        private static unsafe int WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                return FileStreamHelpers.CheckFileCall(
                    Interop.Sys.Pwrite(handle, bufPtr, buffer.Length, fileOffset),
                    path: null);
            }
        }
    }
}
