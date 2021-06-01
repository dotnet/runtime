// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        internal static unsafe int ReadFile(Stream stream, byte* buffer, int size)
        {
            if (readFileNotAvailable)
            {
                return 0;
            }

            SafeHandle handle = GetSafeFileHandle(stream);
            if (handle == null)
            {
                return 0;
            }

            try
            {
                int result = Interop.Kernel32.ReadFile(handle, buffer, size, out int bytesRead, IntPtr.Zero);
                return result == 0 ? 0 : bytesRead;
            }
            catch
            {
                readFileNotAvailable = true;
                return 0;
            }
        }
    }
}
