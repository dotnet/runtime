// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFilePathFromHandle", SetLastError = true)]
        internal static unsafe partial int GetFilePathFromHandle(SafeFileHandle fd, byte* buffer, int bufferSize);

        internal static unsafe string? GetFilePathFromHandle(SafeFileHandle fd)
        {
            // PATH_MAX on Linux is 4096; macOS/BSD MAXPATHLEN is 1024.
            // Using 4096 covers all Unix platforms without requiring buffer growing.
            const int PathMaxSize = 4096;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(PathMaxSize);
            try
            {
                int result;
                fixed (byte* bufPtr = buffer)
                {
                    result = GetFilePathFromHandle(fd, bufPtr, PathMaxSize);
                }

                if (result != 0)
                {
                    return null;
                }

                int length = ((ReadOnlySpan<byte>)buffer).Slice(0, PathMaxSize).IndexOf((byte)0);
                return Encoding.UTF8.GetString(buffer, 0, length < 0 ? PathMaxSize : length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
