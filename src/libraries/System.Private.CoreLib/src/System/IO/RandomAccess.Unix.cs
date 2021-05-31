// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Strategies;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        internal static unsafe long GetFileLength(SafeFileHandle handle, string? path)
        {
            int result = Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(result, path);
            return status.Size;
        }

        private static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                int result = Interop.Sys.PRead(handle, bufPtr, buffer.Length, fileOffset);
                FileStreamHelpers.CheckFileCall(result, path: null);
                return  result;
            }
        }

        private static unsafe int WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                int result = Interop.Sys.PWrite(handle, bufPtr, buffer.Length, fileOffset);
                FileStreamHelpers.CheckFileCall(result, path: null);
                return  result;
            }
        }
    }
}
