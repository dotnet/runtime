// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFilePathFromHandle", SetLastError = true)]
        internal static unsafe partial int GetFilePathFromHandle(IntPtr fd, byte* buffer, int bufferSize);

        internal static unsafe string? GetFilePathFromHandle(IntPtr fd)
        {
            const int InitialBufferSize = 256;

            byte[] arrayBuffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
            try
            {
                while (true)
                {
                    int result;
                    fixed (byte* bufPtr = arrayBuffer)
                    {
                        result = GetFilePathFromHandle(fd, bufPtr, arrayBuffer.Length);
                    }

                    if (result == 0)
                    {
                        // Success: find null terminator to determine the length
                        ReadOnlySpan<byte> span = arrayBuffer;
                        int length = span.IndexOf((byte)0);
                        return length < 0
                            ? Encoding.UTF8.GetString(arrayBuffer)
                            : Encoding.UTF8.GetString(arrayBuffer, 0, length);
                    }

                    ErrorInfo errorInfo = GetLastErrorInfo();
                    if (errorInfo.Error == Error.ENAMETOOLONG)
                    {
                        // Buffer was too small, try again with a larger one
                        byte[] toReturn = arrayBuffer;
                        arrayBuffer = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);
                        ArrayPool<byte>.Shared.Return(toReturn);
                        continue;
                    }

                    // ENOTSUP or any other error: return null to signal unknown path
                    return null;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arrayBuffer);
            }
        }
    }
}
