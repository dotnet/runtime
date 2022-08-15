// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class File
    {
        private static UnixFileMode GetUnixFileModeCore(string path)
            => FileSystem.GetUnixFileMode(Path.GetFullPath(path));

        private static UnixFileMode GetUnixFileModeCore(SafeFileHandle fileHandle)
            => FileSystem.GetUnixFileMode(fileHandle);

        private static void SetUnixFileModeCore(string path, UnixFileMode mode)
            => FileSystem.SetUnixFileMode(Path.GetFullPath(path), mode);

        private static void SetUnixFileModeCore(SafeFileHandle fileHandle, UnixFileMode mode)
            => FileSystem.SetUnixFileMode(fileHandle, mode);
    }
}
