// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Buffers;
using System.Text;
using System;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Takes a path to a symbolic link and attempts to place the link target path into the buffer. If the buffer is too
        /// small, the path will be truncated. No matter what, the buffer will not be null terminated.
        /// </summary>
        /// <param name="path">The path to the symlink</param>
        /// <param name="buffer">The buffer to hold the output path</param>
        /// <param name="bufferSize">The size of the buffer</param>
        /// <returns>
        /// Returns the number of bytes placed into the buffer on success; bufferSize if the buffer is too small; and -1 on error.
        /// </returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadLink", SetLastError = true)]
        private static partial int ReadLink(ref byte path, ref byte buffer, int bufferSize);

        /// <summary>
        /// Takes a path to a symbolic link and returns the link target path.
        /// </summary>
        /// <param name="path">The path to the symlink.</param>
        /// <returns>Returns the link to the target path on success; and null otherwise.</returns>
        internal static unsafe string? ReadLink(ReadOnlySpan<char> path)
        {
            const int stackBufferSize = 256;
            int bufferSize = stackBufferSize;

            // Use an initial buffer size that prevents disposing and renting
            // a second time when calling ConvertAndTerminateString.
            using var converter = new ValueUtf8Converter(stackalloc byte[stackBufferSize]);
            Span<byte> stackBuffer = stackalloc byte[stackBufferSize];
            byte[]? arrayBuffer = null;
            ref byte pathReference = ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path));
            ref byte bufferReference = ref MemoryMarshal.GetReference(stackBuffer);
            int error = 0;
            while (true)
            {
                try
                {
                    int resultLength = ReadLink(ref pathReference, ref bufferReference, bufferSize);
                    error = Marshal.GetLastPInvokeError();

                    if (resultLength < 0)
                    {
                        // error
                        return null;
                    }
                    else if (resultLength < bufferSize)
                    {
                        // success
                        fixed(byte* bufferPtr = &bufferReference)
                        {
                            return Encoding.UTF8.GetString(bufferPtr, resultLength);
                        }
                    }
                }
                finally
                {
                    if (arrayBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(arrayBuffer);
                    }

                    if (error > 0)
                    {
                        Marshal.SetLastPInvokeError(error);
                    }
                }

                // Output buffer was too small, loop around again and try with a larger buffer.
                arrayBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                bufferSize = arrayBuffer.Length;
                bufferReference = ref arrayBuffer[0];
            }
        }
    }
}
