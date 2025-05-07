// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        /// <summary>
        /// Extracts all of the files in the archive to a directory on the file system. The specified directory may already exist.
        /// This method will create all subdirectories and the specified directory if necessary.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to destinationDirectoryName as the
        /// entry has to the root of the archive. If a file to be archived has an invalid last modified time, the first datetime
        /// representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An archive entry?s name is zero-length, contains only whitespace, or contains one or more invalid
        /// characters as defined by InvalidPathChars. -or- Extracting an archive entry would have resulted in a destination
        /// file that is outside destinationDirectoryName (for example, if the entry name contains parent directory accessors).
        /// -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="InvalidDataException">An archive entry was not found or was corrupt.
        /// -or- An archive entry has been compressed using a compression method that is not supported.</exception>
        /// <param name="source">The zip archive to extract files from.</param>
        /// <param name="destinationDirectoryName">The path to the directory on the file system.
        /// The directory specified must not exist. The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName) =>
            ExtractToDirectory(source, destinationDirectoryName, overwriteFiles: false);

        /// <summary>
        /// Extracts all of the files in the archive to a directory on the file system. The specified directory may already exist.
        /// This method will create all subdirectories and the specified directory if necessary.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to destinationDirectoryName as the
        /// entry has to the root of the archive. If a file to be archived has an invalid last modified time, the first datetime
        /// representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An archive entry?s name is zero-length, contains only whitespace, or contains one or more invalid
        /// characters as defined by InvalidPathChars. -or- Extracting an archive entry would have resulted in a destination
        /// file that is outside destinationDirectoryName (for example, if the entry name contains parent directory accessors).
        /// -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="InvalidDataException">An archive entry was not found or was corrupt.
        /// -or- An archive entry has been compressed using a compression method that is not supported.</exception>
        /// <param name="source">The zip archive to extract files from.</param>
        /// <param name="destinationDirectoryName">The path to the directory on the file system.
        /// The directory specified must not exist. The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="overwriteFiles">True to indicate overwrite.</param>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destinationDirectoryName);

            foreach (ZipArchiveEntry entry in source.Entries)
            {
                entry.ExtractRelativeToDirectory(destinationDirectoryName, overwriteFiles);
            }
        }
    }
}
