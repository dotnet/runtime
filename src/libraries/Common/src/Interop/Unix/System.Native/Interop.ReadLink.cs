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
        internal static string? ReadLink(ReadOnlySpan<char> path)
        {
            const int StackBufferSize = 256;

            // Use an initial buffer size that prevents disposing and renting
            // a second time when calling ConvertAndTerminateString.
            using var converter = new ValueUtf8Converter(stackalloc byte[StackBufferSize]);
            Span<byte> spanBuffer = stackalloc byte[StackBufferSize];
            byte[]? arrayBuffer = null;
            ref byte pathReference = ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path));
            while (true)
            {
                int error = 0;
                try
                {
                    int resultLength = ReadLink(ref pathReference, ref MemoryMarshal.GetReference(spanBuffer), spanBuffer.Length);

                    if (resultLength < 0)
                    {
                        // error
                        error = Marshal.GetLastPInvokeError();
                        return null;
                    }
                    else if (resultLength < spanBuffer.Length)
                    {
                        // success
                        return Encoding.UTF8.GetString(spanBuffer.Slice(0, resultLength));
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
                arrayBuffer = ArrayPool<byte>.Shared.Rent(spanBuffer.Length * 2);
                spanBuffer = arrayBuffer;
            }
        }
    }
}
