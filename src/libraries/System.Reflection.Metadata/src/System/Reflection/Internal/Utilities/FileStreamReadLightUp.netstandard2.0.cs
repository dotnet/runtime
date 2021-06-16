// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class FileStreamReadLightUp
    {
        private static bool IsReadFileAvailable => Path.DirectorySeparatorChar == '\\';

        internal static bool IsFileStream(Stream stream) => stream is FileStream;

        private static SafeHandle? GetSafeFileHandle(Stream stream)
        {
            SafeHandle handle;
            try
            {
                handle = ((FileStream)stream).SafeFileHandle;
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

        internal static unsafe int ReadFile(Stream stream, byte* buffer, int size)
        {
            if (!IsReadFileAvailable)
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
    }
}
