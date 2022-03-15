// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Enumeration;
using System.Threading;

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

        internal static bool Copy(string sourcePath, string destinationPath, bool recursive,
            bool skipExistingFiles, CancellationToken cancellationToken)
        {
            bool success = true;
            if (!DirectoryExists(sourcePath))
            {
                throw new DirectoryNotFoundException(sourcePath);
            }

            // Create destination directory if not exists
            CreateDirectory(destinationPath);

            cancellationToken.ThrowIfCancellationRequested();
            var fse = new FileSystemEnumerable<(string childPath, bool isDirectory)>(sourcePath,
                static (ref FileSystemEntry entry) => (entry.ToFullPath(), entry.IsDirectory),
                recursive ? EnumerationOptions.CompatibleRecursive : EnumerationOptions.Compatible);

            foreach ((string childPath, bool isDirectory) in fse)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    string basePath = childPath[sourcePath.Length..];
                    if (PathInternal.IsDirectorySeparator(basePath[0]))
                    {
                        basePath = basePath[1..];
                    }

                    string destFilePath = Path.Join(destinationPath, basePath);
                    if (isDirectory)
                    {
                        CreateDirectory(destFilePath);
                    }
                    else
                    {
                        // Don't copy if file already exists and user opted-in to skip existing files.
                        CopyFile(childPath, destFilePath, overwrite: skipExistingFiles);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException)
                {
                    if (ex.Message.Contains(destinationPath))
                    {
                        throw;
                    }
                    success = false;
                }
            }

            return success;
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
