// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public static partial class Directory
    {
        private static DirectoryInfo CreateDirectoryCore(string path, UnixFileMode unixCreateMode)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if ((unixCreateMode & ~FileSystem.ValidUnixFileModes) != 0)
            {
                throw new ArgumentException(SR.Arg_InvalidUnixFileMode, nameof(unixCreateMode));
            }

            string fullPath = Path.GetFullPath(path);

            FileSystem.CreateDirectory(fullPath, unixCreateMode);

            return new DirectoryInfo(path, fullPath, isNormalized: true);
        }
    }
}
