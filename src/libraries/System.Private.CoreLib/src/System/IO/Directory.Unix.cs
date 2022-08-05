// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

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

        private static unsafe string CreateTempSubdirectoryCore(string? prefix)
        {
            // mkdtemp takes a char* and overwrites the XXXXXX with six characters
            // that'll result in a unique file name.
            string template = $"{Path.GetTempPath()}{prefix}XXXXXX\0";
            byte[] name = Encoding.UTF8.GetBytes(template);

            // Create the temp directory.
            byte* result = Interop.Sys.MkdTemp(name);
            if (result == null)
            {
                Interop.CheckIo(-1);
            }

            // 'name' is now the name of the directory
            Debug.Assert(name[^1] == '\0');
            return Encoding.UTF8.GetString(name, 0, name.Length - 1); // trim off the trailing '\0'
        }
    }
}
