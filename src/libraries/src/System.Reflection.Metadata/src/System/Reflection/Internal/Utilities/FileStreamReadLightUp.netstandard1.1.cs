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
        internal static Lazy<Type> FileStreamType = new Lazy<Type>(() =>
        {
            const string systemIOFileSystem = "System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken = b03f5f7f11d50a3a";
            const string mscorlib = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            return LightUpHelper.GetType("System.IO.FileStream", systemIOFileSystem, mscorlib);
        });

        internal static Lazy<PropertyInfo> SafeFileHandle = new Lazy<PropertyInfo>(() =>
        {
            return FileStreamType.Value.GetTypeInfo().GetDeclaredProperty("SafeFileHandle");
        });

        // internal for testing
        internal static bool readFileNotAvailable;
        internal static bool safeFileHandleNotAvailable;

        internal static bool IsFileStream(Stream stream)
        {
            if (FileStreamType.Value == null)
            {
                return false;
            }

            var type = stream.GetType();
            return type == FileStreamType.Value || type.GetTypeInfo().IsSubclassOf(FileStreamType.Value);
        }

        internal static SafeHandle GetSafeFileHandle(Stream stream)
        {
            Debug.Assert(FileStreamType.IsValueCreated && FileStreamType.Value != null && IsFileStream(stream));

            if (safeFileHandleNotAvailable)
            {
                return null;
            }

            PropertyInfo safeFileHandleProperty = SafeFileHandle.Value;
            if (safeFileHandleProperty == null)
            {
                safeFileHandleNotAvailable = true;
                return null;
            }

            SafeHandle handle;
            try
            {
                handle = (SafeHandle)safeFileHandleProperty.GetValue(stream);
            }
            catch (MemberAccessException)
            {
                safeFileHandleNotAvailable = true;
                return null;
            }
            catch (InvalidOperationException)
            {
                // thrown when accessing unapproved API in a Windows Store app
                safeFileHandleNotAvailable = true;
                return null;
            }
            catch (TargetInvocationException)
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

            bool result = false;
            int bytesRead = 0;

            try
            {
                result = ReadFile(handle, buffer, size, out bytesRead, IntPtr.Zero);
            }
            catch
            {
                readFileNotAvailable = true;
                return false;
            }

            if (!result || bytesRead != size)
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

#pragma warning disable BCL0015 // Disable Pinvoke analyzer errors.
        [DllImport(@"kernel32.dll", EntryPoint = "ReadFile", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool ReadFile(
             SafeHandle fileHandle,
             byte* buffer,
             int byteCount,
             out int bytesRead,
             IntPtr overlapped
        );
#pragma warning restore BCL0015
    }
}
