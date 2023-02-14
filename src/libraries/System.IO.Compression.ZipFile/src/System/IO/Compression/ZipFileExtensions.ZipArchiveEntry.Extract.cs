// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name. The last write time of the file is set to the
        /// entry?s last write time. This method does not allow overwriting of an existing file with the same name. Attempting to extract explicit
        /// directories (entries with names that end in directory separator characters) will not result in the creation of a directory.
        /// </summary>
        ///
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only whitespace, or contains one or more
        /// invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid (for example, it is on
        /// an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred. -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName) =>
            ExtractToFile(source, destinationFileName, false);

        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name.
        /// The last write time of the file is set to the entry?s last write time.
        /// This method does allows overwriting of an existing file with the same name.
        /// </summary>
        ///
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred.
        /// -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="overwrite">True to indicate overwrite.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destinationFileName);

            FileStreamOptions fileStreamOptions = new()
            {
                Access = FileAccess.Write,
                Mode = overwrite ? FileMode.Create : FileMode.CreateNew,
                Share = FileShare.None,
                BufferSize = 0x1000
            };

            const UnixFileMode OwnershipPermissions =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite |  UnixFileMode.OtherExecute;

            // Restore Unix permissions.
            // For security, limit to ownership permissions, and respect umask (through UnixCreateMode).
            // We don't apply UnixFileMode.None because .zip files created on Windows and .zip files created
            // with previous versions of .NET don't include permissions.
            UnixFileMode mode = (UnixFileMode)(source.ExternalAttributes >> 16) & OwnershipPermissions;
            if (mode != UnixFileMode.None && !OperatingSystem.IsWindows())
            {
                fileStreamOptions.UnixCreateMode = mode;
            }

            using (FileStream fs = new FileStream(destinationFileName, fileStreamOptions))
            {
                using (Stream es = source.Open())
                    es.CopyTo(fs);
            }

            ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, source.LastWriteTime);
        }

        internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName) =>
            ExtractRelativeToDirectory(source, destinationDirectoryName, overwrite: false);

        internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destinationDirectoryName);

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;
            if (!destinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                char sep = Path.DirectorySeparatorChar;
                destinationDirectoryFullPath = string.Concat(destinationDirectoryFullPath, new ReadOnlySpan<char>(in sep));
            }

            string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, ArchivingUtils.SanitizeEntryFilePath(source.FullName)));

            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
                throw new IOException(SR.IO_ExtractingResultsInOutside);

            if (Path.GetFileName(fileDestinationPath).Length == 0)
            {
                // If it is a directory:

                if (source.Length != 0)
                    throw new IOException(SR.IO_DirectoryNameWithData);

                Directory.CreateDirectory(fileDestinationPath);
            }
            else
            {
                // If it is a file:
                // Create containing directory:
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                source.ExtractToFile(fileDestinationPath, overwrite: overwrite);
            }
        }
    }
}
