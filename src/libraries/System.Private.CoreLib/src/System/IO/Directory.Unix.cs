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
            // that'll result in a unique directory name.
            string tempPath = Path.GetTempPath();
            int tempPathByteCount = Encoding.UTF8.GetByteCount(tempPath);
            int prefixByteCount = prefix is not null ? Encoding.UTF8.GetByteCount(prefix) : 0;
            int totalByteCount = checked(tempPathByteCount + prefixByteCount + 6 + 1);

            Span<byte> path = (uint)totalByteCount <= 256 ? stackalloc byte[totalByteCount] : new byte[totalByteCount];
            int pos = Encoding.UTF8.GetBytes(tempPath, path);
            if (prefix is not null)
            {
                pos += Encoding.UTF8.GetBytes(prefix, path.Slice(pos));
            }
            path.Slice(pos, 6).Fill((byte)'X');
            path[pos + 6] = 0;

            // Create the temp directory.
            fixed (byte* pPath = path)
            {
                if (Interop.Sys.MkdTemp(pPath) is null)
                {
                    Interop.ThrowIOExceptionForLastError();
                }
            }

            // 'path' is now the name of the directory
            Debug.Assert(path[^1] == 0);
            return Encoding.UTF8.GetString(path.Slice(0, path.Length - 1)); // trim off the trailing '\0'
        }
    }
}
