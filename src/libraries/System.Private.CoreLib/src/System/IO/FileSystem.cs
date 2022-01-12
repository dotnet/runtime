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

        internal static void MoveDirectory(string sourceFullPath, string destFullPath, string destinationWithSeparator, bool? sourceDirectoryExists = default)
        {
            string sourcePath = PathInternal.EnsureTrailingSeparator(sourceFullPath);

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
            if (!sameDirectoryDifferentCase && string.Equals(sourcePath, destinationWithSeparator, fileSystemSensitivity))
                throw new IOException(SR.IO_SourceDestMustBeDifferent);

            ReadOnlySpan<char> sourceRoot = Path.GetPathRoot(sourcePath.AsSpan());
            ReadOnlySpan<char> destinationRoot = Path.GetPathRoot(destinationWithSeparator.AsSpan());

            // Compare paths for the same, skip this step if we already know the paths are identical.
            if (!sourceRoot.Equals(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new IOException(SR.IO_SourceDestMustHaveSameRoot);

            if (sourceDirectoryExists is null)
            {
                sourceDirectoryExists = DirectoryExists(sourceFullPath);
            }

            // Windows will throw if the source file/directory doesn't exist, we preemptively check
            // to make sure our cross platform behavior matches .NET Framework behavior.
            if (!sourceDirectoryExists.Value && !FileExists(sourceFullPath))
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceFullPath));

            if (!sameDirectoryDifferentCase // This check is to allow renaming of directories
                && DirectoryExists(destFullPath))
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destFullPath));

            // If the directories aren't the same and the OS says the directory exists already, fail.
            if (!sameDirectoryDifferentCase && Directory.Exists(destFullPath))
                throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, destFullPath));

            MoveDirectory(sourceFullPath, destFullPath);
        }
    }
}
