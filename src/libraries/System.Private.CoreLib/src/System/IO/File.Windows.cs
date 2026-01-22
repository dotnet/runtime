// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class File
    {
        private static SafeFileHandle OpenNullHandleCore()
        {
            // Open the NUL device on Windows with read/write access and no inheritance
            return SafeFileHandle.Open("NUL", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.None, preallocationSize: 0);
        }

        private static UnixFileMode GetUnixFileModeCore(string path)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);

        private static UnixFileMode GetUnixFileModeCore(SafeFileHandle fileHandle)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);

        private static void SetUnixFileModeCore(string path, UnixFileMode mode)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);

        private static void SetUnixFileModeCore(SafeFileHandle fileHandle, UnixFileMode mode)
            => throw new PlatformNotSupportedException(SR.PlatformNotSupported_UnixFileMode);
    }
}
