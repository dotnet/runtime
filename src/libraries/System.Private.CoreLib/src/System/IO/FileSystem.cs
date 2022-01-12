// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal static partial class FileSystem
    {
        internal static void VerifyValidPath(string path, string argName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(argName);
            }
            else if (path.Length == 0)
            {
                throw new ArgumentException(SR.Arg_PathEmpty, argName);
            }
            else if (path.Contains('\0'))
            {
                throw new ArgumentException(SR.Argument_InvalidPathChars, argName);
            }
        }

        internal static void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            ReadOnlySpan<char> srcNoDirectorySeparator = Path.TrimEndingDirectorySeparator(sourceFullPath.AsSpan());
            ReadOnlySpan<char> destNoDirectorySeparator = Path.TrimEndingDirectorySeparator(destFullPath.AsSpan());

            ReadOnlySpan<char> sourceDirNameFromFullPath = Path.GetFileName(sourceFullPath.AsSpan());
            ReadOnlySpan<char> destDirNameFromFullPath = Path.GetFileName(destFullPath.AsSpan());

            StringComparison fileSystemSensitivity = PathInternal.StringComparison;
            bool directoriesAreCaseVariants =
                !sourceDirNameFromFullPath.SequenceEqual(destDirNameFromFullPath) &&
                sourceDirNameFromFullPath.Equals(destDirNameFromFullPath, StringComparison.OrdinalIgnoreCase);
            bool sameDirectoryDifferentCase =
                directoriesAreCaseVariants &&
                destDirNameFromFullPath.Equals(sourceDirNameFromFullPath, fileSystemSensitivity);

            // If the destination directories are the exact same name
            if (!sameDirectoryDifferentCase && srcNoDirectorySeparator.Equals(destNoDirectorySeparator, fileSystemSensitivity))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            ReadOnlySpan<char> sourceRoot = Path.GetPathRoot(srcNoDirectorySeparator);
            ReadOnlySpan<char> destinationRoot = Path.GetPathRoot(destNoDirectorySeparator);

            // Compare paths for the same, skip this step if we already know the paths are identical.
            if (!sourceRoot.Equals(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            MoveDirectory(sourceFullPath, destFullPath, sameDirectoryDifferentCase);
        }
    }
}
