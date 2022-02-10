// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class FileSystem
    {
        internal static void VerifyValidPath(string path, string argName)
        {
            ArgumentException.ThrowIfNullOrEmpty(path, argName);
            if (path.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidPathChars, argName);
            }
        }

        internal static void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            ReadOnlySpan<char> srcNoDirectorySeparator = Path.TrimEndingDirectorySeparator(sourceFullPath.AsSpan());
            ReadOnlySpan<char> destNoDirectorySeparator = Path.TrimEndingDirectorySeparator(destFullPath.AsSpan());

            // Don't allow the same path, except for changing the casing of the filename.
            if (srcNoDirectorySeparator.Equals(destNoDirectorySeparator, PathInternal.StringComparison))
            {
                ReadOnlySpan<char> srcFileName = Path.GetFileName(srcNoDirectorySeparator);
                ReadOnlySpan<char> destFileName = Path.GetFileName(destNoDirectorySeparator);
                if (srcFileName.SequenceEqual(destFileName))
                {
                    throw new IOException(SR.IO_SourceDestMustBeDifferent);
                }
            }

            MoveDirectoryCore(sourceFullPath, destFullPath);
        }
    }
}
