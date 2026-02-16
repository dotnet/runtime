// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public static partial class ZipFileExtensions
    {
        /// <summary>
        /// Creates a file on the file system with the entry's contents and the specified name. The last write time of the file is set to the
        /// entry's last write time. This method does not allow overwriting of an existing file with the same name. Attempting to extract explicit
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
        /// Creates a file on the file system with the entry's contents and the specified name.
        /// The last write time of the file is set to the entry's last write time.
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
            ExtractToFileInitialize(source, destinationFileName, overwrite, useAsync: false, out FileStreamOptions fileStreamOptions);

            // When overwriting, extract to a temporary file first to avoid corrupting the destination file
            // if an exception occurs during extraction (e.g., password-protected archive, corrupted data).
            string extractPath = destinationFileName;
            string? tempPath = null;

            if (overwrite && File.Exists(destinationFileName))
            {
                // Use GetTempFileName for a unique temp file in the system temp directory.
                // This avoids conflicts and ensures cleanup by the OS if the process crashes.
                tempPath = Path.GetTempFileName();
                extractPath = tempPath;
            }

            try
            {
                using (FileStream fs = new FileStream(extractPath, fileStreamOptions))
                {
                    using (Stream es = source.Open())
                        es.CopyTo(fs);
                }

                // Move the temporary file to the destination only after successful extraction
                if (tempPath is not null)
                {
                    File.Move(tempPath, destinationFileName, overwrite: true);
                }

                ExtractToFileFinalize(source, destinationFileName);
            }
            catch
            {
                // Clean up the temporary file if extraction failed
                if (tempPath is not null && File.Exists(tempPath))
                {
                    // Ignore exceptions during cleanup; the original exception is more important
                    try { File.Delete(tempPath); } catch { }
                }
                throw;
            }
        }

        /// <summary>
        /// Creates a file on the file system with the entry's contents decrypted using the specified password.
        /// The last write time of the file is set to the entry's last write time.
        /// This method does not allow overwriting of an existing file with the same name.
        /// </summary>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.</param>
        /// <param name="password">The password used to decrypt the encrypted entry.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, string password) =>
            ExtractToFile(source, destinationFileName, overwrite: false, password: password);

        /// <summary>
        /// Creates a file on the file system with the entry's contents decrypted using the specified password.
        /// The last write time of the file is set to the entry's last write time.
        /// This method allows overwriting of an existing file with the same name.
        /// </summary>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.</param>
        /// <param name="overwrite">True to indicate overwrite.</param>
        /// <param name="password">The password used to decrypt the encrypted entry.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite, string password)
        {
            ExtractToFileInitialize(source, destinationFileName, overwrite, useAsync: false, out FileStreamOptions fileStreamOptions);

            // When overwriting, extract to a temporary file first to avoid corrupting the destination file
            // if an exception occurs during extraction (e.g., password-protected archive, corrupted data).
            string extractPath = destinationFileName;
            string? tempPath = null;

            if (overwrite && File.Exists(destinationFileName))
            {
                tempPath = Path.GetTempFileName();
                extractPath = tempPath;
            }

            try
            {
                using (FileStream fs = new FileStream(extractPath, fileStreamOptions))
                {
                    using (Stream es = !string.IsNullOrEmpty(password) ? source.Open(password) : source.Open())
                        es.CopyTo(fs);
                }

                // Move the temporary file to the destination only after successful extraction
                if (tempPath is not null)
                {
                    File.Move(tempPath, destinationFileName, overwrite: true);
                }

                ExtractToFileFinalize(source, destinationFileName);
            }
            catch
            {
                // Clean up the temporary file if extraction failed
                if (tempPath is not null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw;
            }
        }

        private static void ExtractToFileInitialize(ZipArchiveEntry source, string destinationFileName, bool overwrite, bool useAsync, out FileStreamOptions fileStreamOptions)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destinationFileName);

            fileStreamOptions = new()
            {
                Access = FileAccess.Write,
                Mode = overwrite ? FileMode.Create : FileMode.CreateNew,
                Share = FileShare.None,
                BufferSize = ZipFile.FileStreamBufferSize,
                Options = useAsync ? FileOptions.Asynchronous : FileOptions.None
            };

            const UnixFileMode OwnershipPermissions =
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

            // Restore Unix permissions.
            // For security, limit to ownership permissions, and respect umask (through UnixCreateMode).
            // We don't apply UnixFileMode.None because .zip files created on Windows and .zip files created
            // with previous versions of .NET don't include permissions.
            UnixFileMode mode = (UnixFileMode)(source.ExternalAttributes >> 16) & OwnershipPermissions;
            if (mode != UnixFileMode.None && !OperatingSystem.IsWindows())
            {
                fileStreamOptions.UnixCreateMode = mode;
            }
        }

        private static void ExtractToFileFinalize(ZipArchiveEntry source, string destinationFileName) =>
            ArchivingUtils.AttemptSetLastWriteTime(destinationFileName, source.LastWriteTime);

        private static bool ExtractRelativeToDirectoryCheckIfFile(ZipArchiveEntry source, string destinationDirectoryName, out string fileDestinationPath)
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

            fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, ArchivingUtils.SanitizeEntryFilePath(source.FullName)));

            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
                throw new IOException(SR.IO_ExtractingResultsInOutside);

            if (Path.GetFileName(fileDestinationPath).Length == 0)
            {
                if (source.Length != 0)
                    throw new IOException(SR.IO_DirectoryNameWithData);

                Directory.CreateDirectory(fileDestinationPath);

                return false; // It is a directory
            }

            return true; // It is a file
        }

        internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite, string? password = null)
        {
            if (ExtractRelativeToDirectoryCheckIfFile(source, destinationDirectoryName, out string fileDestinationPath))
            {
                // If it is a file:
                // Create containing directory:
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                if (!string.IsNullOrEmpty(password))
                    source.ExtractToFile(fileDestinationPath, overwrite: overwrite, password: password);
                else
                    source.ExtractToFile(fileDestinationPath, overwrite: overwrite);
            }
        }
    }
}
