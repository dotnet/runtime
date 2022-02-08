// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
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
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            IEnumerable<string> directoryEnumeration;
            bool success = true;

            try
            {
                EnumerationOptions enumerationOptions = EnumerationOptions.Compatible;

                // Compatible uses `IgnoreInaccessible = false`
                enumerationOptions.IgnoreInaccessible = true;

                if (recursive)
                {
                    enumerationOptions.RecurseSubdirectories = true;
                }

                directoryEnumeration = Directory.EnumerateDirectories(sourcePath, "*", enumerationOptions);
            }
            catch (IOException)
            {
                // Return false if Enumeration is not possible
                return false;
            }

            foreach (string enumeratedDirectory in directoryEnumeration)
            {
                string newDirectoryPath = enumeratedDirectory.Replace(sourcePath, destinationPath);
                Directory.CreateDirectory(newDirectoryPath);

                cancellationToken.ThrowIfCancellationRequested();
            }

            foreach (string enumeratedFile in Directory.GetFiles(sourcePath, "*.*", searchOption))
            {
                if (skipExistingFiles && File.Exists(enumeratedFile))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    continue;
                }

                try
                {
                    CopyFile(enumeratedFile, enumeratedFile.Replace(sourcePath, destinationPath), true);
                }
                catch (IOException ex)
                {
                    switch (ex.HResult)
                    {
                        // success = false for read failures,
                        // throw for everything else
                        case Interop.Errors.ERROR_ACCESS_DENIED:
                            success = false;
                            break;
                        default:
                            throw;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
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
