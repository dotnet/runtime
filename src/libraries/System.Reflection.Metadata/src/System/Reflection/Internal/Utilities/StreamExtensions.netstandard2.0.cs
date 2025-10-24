// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static partial class StreamExtensions
    {
        private static bool IsWindows => Path.DirectorySeparatorChar == '\\';

        // From System.IO.Stream.CopyTo:
        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        internal const int StreamCopyBufferSize = 81920;

        /// <summary>
        /// Copies specified amount of data from given stream to a target memory pointer.
        /// </summary>
        /// <exception cref="IOException">unexpected stream end.</exception>
        internal static unsafe void CopyTo(this Stream source, byte* destination, int size)
        {
            byte[] buffer = new byte[Math.Min(StreamCopyBufferSize, size)];
            while (size > 0)
            {
                int readSize = Math.Min(size, buffer.Length);
                int bytesRead = source.Read(buffer, 0, readSize);

                if (bytesRead <= 0 || bytesRead > readSize)
                {
                    throw new IOException(SR.UnexpectedStreamEnd);
                }

                Marshal.Copy(buffer, 0, (IntPtr)destination, bytesRead);

                destination += bytesRead;
                size -= bytesRead;
            }
        }

        private static SafeHandle? GetSafeFileHandle(FileStream stream)
        {
            SafeHandle handle;
            try
            {
                handle = stream.SafeFileHandle;
            }
            catch
            {
                // Some FileStream implementations (e.g. IsolatedStorage) restrict access to the underlying handle by throwing
                // Tolerate it and fall back to slow path.
                return null;
            }

            if (handle != null && handle.IsInvalid)
            {
                // Also allow for FileStream implementations that do return a non-null, but invalid underlying OS handle.
                // This is how brokered files on WinRT will work. Fall back to slow path.
                return null;
            }

            return handle;
        }

        /// <summary>
        /// Reads from a <see cref="FileStream"/> into an unmanaged buffer.
        /// </summary>
        /// <remarks>
        /// If the platform is not Windows, this function will always return zero and perform no reads.
        /// </remarks>
        private static unsafe int TryReadWin32File(this FileStream stream, byte* buffer, int size)
        {
            if (!IsWindows)
            {
                return 0;
            }

            SafeHandle? handle = GetSafeFileHandle(stream);
            if (handle == null)
            {
                return 0;
            }

            int result = Interop.Kernel32.ReadFile(handle, buffer, size, out int bytesRead, IntPtr.Zero);
            return result == 0 ? 0 : bytesRead;
        }

        internal static unsafe void ReadExactly(this Stream stream, byte* buffer, int size)
        {
            int bytesRead = stream is FileStream fs ? TryReadWin32File(fs, buffer, size) : 0;
            if (bytesRead != size)
            {
                stream.CopyTo(buffer + bytesRead, size - bytesRead);
            }
        }
    }
}
