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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadLink", SetLastError = true)]
        private static extern int ReadLink(ref byte path, byte[] buffer, int bufferSize);

        /// <summary>
        /// Takes a path to a symbolic link and returns the link target path.
        /// </summary>
        /// <param name="path">The path to the symlink.</param>
        /// <returns>Returns the link to the target path on success; and null otherwise.</returns>
        internal static string? ReadLink(ReadOnlySpan<char> path)
        {
            int outputBufferSize = 1024;

            // Use an initial buffer size that prevents disposing and renting
            // a second time when calling ConvertAndTerminateString.
            using var converter = new ValueUtf8Converter(stackalloc byte[1024]);

            while (true)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(outputBufferSize);
                try
                {
                    int resultLength = Interop.Sys.ReadLink(
                        ref MemoryMarshal.GetReference(converter.ConvertAndTerminateString(path)),
                        buffer,
                        buffer.Length);

                    if (resultLength < 0)
                    {
                        // error
                        return null;
                    }
                    else if (resultLength < buffer.Length)
                    {
                        // success
                        return Encoding.UTF8.GetString(buffer, 0, resultLength);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                // Output buffer was too small, loop around again and try with a larger buffer.
                outputBufferSize = buffer.Length * 2;
            }
        }
    }
}
