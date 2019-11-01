// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class FileStreamReadLightUp
    {
        // internal for testing
        internal static bool readFileNotAvailable = Path.DirectorySeparatorChar != '\\'; // Available on Windows only
        internal static bool safeFileHandleNotAvailable = false;

        internal static bool IsFileStream(Stream stream) => stream is FileStream;

        internal static SafeHandle GetSafeFileHandle(Stream stream)
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

        internal static unsafe bool TryReadFile(Stream stream, byte* buffer, long start, int size)
        {
            if (readFileNotAvailable)
            {
                return false;
            }

            SafeHandle handle = GetSafeFileHandle(stream);
            if (handle == null)
            {
                return false;
            }

            int result;
            int bytesRead;

            try
            {
                result = Interop.Kernel32.ReadFile(handle, buffer, size, out bytesRead, IntPtr.Zero);
            }
            catch
            {
                readFileNotAvailable = true;
                return false;
            }

            if (result == 0 || bytesRead != size)
            {
                // We used to throw here, but this is where we land if the FileStream was
                // opened with useAsync: true, which is currently the default on .NET Core.
                // Issue #987 filed to look in to how best to handle this, but in the meantime,
                // we'll fall back to the slower code path just as in the case where the native
                // API is unavailable in the current platform.
                return false;
            }

            return true;
        }
    }
}
