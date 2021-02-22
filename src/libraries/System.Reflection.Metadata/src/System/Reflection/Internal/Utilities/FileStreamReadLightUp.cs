// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class FileStreamReadLightUp
    {
        private static bool IsReadFileAvailable =>
#if NETCOREAPP
            OperatingSystem.IsWindows();
#else
            Path.DirectorySeparatorChar == '\\';
#endif

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

        internal static unsafe bool TryReadFile(Stream stream, byte* buffer, long start, int size)
        {
            if (!IsReadFileAvailable)
            {
                return false;
            }

            SafeHandle? handle = GetSafeFileHandle(stream);
            if (handle == null)
            {
                return false;
            }

            int result = Interop.Kernel32.ReadFile(handle, buffer, size, out int bytesRead, IntPtr.Zero);

            if (result == 0 || bytesRead != size)
            {
                // We used to throw here, but this is where we land if the FileStream was
                // opened with useAsync: true, which is currently the default on .NET Core.
                // https://github.com/dotnet/corefx/pull/987 filed to look in to how best to
                // handle this, but in the meantime, we'll fall back to the slower code path
                // just as in the case where the native API is unavailable in the current platform.
                return false;
            }

            return true;
        }
    }
}
