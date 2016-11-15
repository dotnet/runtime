// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ZipFileExtensions
    {
        #region ZipArchive extensions


        /// <summary>
        /// <p>Adds a file from the file system to the archive under the specified entry name.
        /// The new entry in the archive will contain the contents of the file.
        /// The last write time of the archive entry is set to the last write time of the file on the file system.
        /// If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.
        /// If the specified source file has an invalid last modified time, the first datetime representable in the Zip timestamp format
        /// (midnight on January 1, 1980) will be used.</p>
        /// 
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        /// 
        /// <p>Since no <code>CompressionLevel</code> is specified, the default provided by the implementation of the underlying compression
        /// algorithm will be used; the <code>ZipArchive</code> will not impose its own default.
        /// (Currently, the underlying compression algorithm is provided by the <code>System.IO.Compression.DeflateStream</code> class.)</p>
        /// </summary>
        /// 
        /// <exception cref="ArgumentException">sourceFileName is a zero-length string, contains only white space, or contains one or more
        /// invalid characters as defined by InvalidPathChars. -or- entryName is a zero-length string.</exception>
        /// <exception cref="ArgumentNullException">sourceFileName or entryName is null.</exception>
        /// <exception cref="PathTooLongException">In sourceFileName, the specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified sourceFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file specified by sourceFileName.</exception>
        /// <exception cref="UnauthorizedAccessException">sourceFileName specified a directory. -or- The caller does not have the
        /// required permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in sourceFileName was not found. </exception>
        /// <exception cref="NotSupportedException">sourceFileName is in an invalid format or the ZipArchive does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// 
        /// <param name="sourceFileName">The path to the file on the file system to be copied from. The path is permitted to specify
        /// relative or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="entryName">The name of the entry to be created.</param>
        /// <returns>A wrapper for the newly created entry.</returns>
        public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName)
        {
            Contract.Ensures(Contract.Result<ZipArchiveEntry>() != null);
            Contract.EndContractBlock();

            return DoCreateEntryFromFile(destination, sourceFileName, entryName, null);
        }


        /// <summary>
        /// <p>Adds a file from the file system to the archive under the specified entry name.
        /// The new entry in the archive will contain the contents of the file.
        /// The last write time of the archive entry is set to the last write time of the file on the file system.
        /// If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.
        /// If the specified source file has an invalid last modified time, the first datetime representable in the Zip timestamp format
        /// (midnight on January 1, 1980) will be used.</p>
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        /// </summary>
        /// <exception cref="ArgumentException">sourceFileName is a zero-length string, contains only white space, or contains one or more
        /// invalid characters as defined by InvalidPathChars. -or- entryName is a zero-length string.</exception>
        /// <exception cref="ArgumentNullException">sourceFileName or entryName is null.</exception>
        /// <exception cref="PathTooLongException">In sourceFileName, the specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified sourceFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file specified by sourceFileName.</exception>
        /// <exception cref="UnauthorizedAccessException">sourceFileName specified a directory.
        /// -or- The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in sourceFileName was not found. </exception>
        /// <exception cref="NotSupportedException">sourceFileName is in an invalid format or the ZipArchive does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive has already been closed.</exception>
        /// 
        /// <param name="sourceFileName">The path to the file on the file system to be copied from. The path is permitted to specify relative
        /// or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="entryName">The name of the entry to be created.</param>
        /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
        /// <returns>A wrapper for the newly created entry.</returns>   
        public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination,
                                                          string sourceFileName, string entryName, CompressionLevel compressionLevel)
        {
            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            Contract.Ensures(Contract.Result<ZipArchiveEntry>() != null);
            Contract.EndContractBlock();

            return DoCreateEntryFromFile(destination, sourceFileName, entryName, compressionLevel);
        }


        /// <summary>
        /// Extracts all of the files in the archive to a directory on the file system. The specified directory may already exist.
        /// This method will create all subdirectories and the specified directory if necessary.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to destinationDirectoryName as the
        /// entry has to the root of the archive. If a file to be archived has an invalid last modified time, the first datetime
        /// representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        /// 
        /// <exception cref="ArgumentException">destinationDirectoryName is a zero-length string, contains only white space,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An archive entry?s name is zero-length, contains only white space, or contains one or more invalid
        /// characters as defined by InvalidPathChars. -or- Extracting an archive entry would have resulted in a destination
        /// file that is outside destinationDirectoryName (for example, if the entry name contains parent directory accessors).
        /// -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="InvalidDataException">An archive entry was not found or was corrupt.
        /// -or- An archive entry has been compressed using a compression method that is not supported.</exception>
        /// 
        /// <param name="destinationDirectoryName">The path to the directory on the file system.
        /// The directory specified must not exist. The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            Contract.EndContractBlock();

            // Rely on Directory.CreateDirectory for validation of destinationDirectoryName.

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry entry in source.Entries)
            {
                string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, entry.FullName));

                if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
                    throw new IOException(SR.IO_ExtractingResultsInOutside);

                if (Path.GetFileName(fileDestinationPath).Length == 0)
                {
                    // If it is a directory:

                    if (entry.Length != 0)
                        throw new IOException(SR.IO_DirectoryNameWithData);

                    Directory.CreateDirectory(fileDestinationPath);
                }
                else
                {
                    // If it is a file:
                    // Create containing directory:
                    Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                    entry.ExtractToFile(fileDestinationPath, overwrite: false);
                }
            }
        }


        internal static ZipArchiveEntry DoCreateEntryFromFile(ZipArchive destination,
                                                              string sourceFileName, string entryName, CompressionLevel? compressionLevel)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (entryName == null)
                throw new ArgumentNullException(nameof(entryName));

            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            // Argument checking gets passed down to FileStream's ctor and CreateEntry
            Contract.Ensures(Contract.Result<ZipArchiveEntry>() != null);
            Contract.EndContractBlock();

            using (Stream fs = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false))
            {
                ZipArchiveEntry entry = compressionLevel.HasValue
                                                ? destination.CreateEntry(entryName, compressionLevel.Value)
                                                : destination.CreateEntry(entryName);

                DateTime lastWrite = File.GetLastWriteTime(sourceFileName);

                // If file to be archived has an invalid last modified time, use the first datetime representable in the Zip timestamp format
                // (midnight on January 1, 1980):
                if (lastWrite.Year < 1980 || lastWrite.Year > 2107)
                    lastWrite = new DateTime(1980, 1, 1, 0, 0, 0);

                entry.LastWriteTime = lastWrite;

                using (Stream es = entry.Open())
                    fs.CopyTo(es);

                return entry;
            }
        }

        #endregion ZipArchive extensions


        #region ZipArchiveEntry extensions

        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name. The last write time of the file is set to the
        /// entry?s last write time. This method does not allow overwriting of an existing file with the same name. Attempting to extract explicit
        /// directories (entries with names that end in directory separator characters) will not result in the creation of a directory.
        /// </summary>
        /// 
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only white space, or contains one or more
        /// invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid (for example, it is on
        /// an unmapped drive).</exception>
        /// <exception cref="IOException">destinationFileName already exists.
        /// -or- An I/O error has occurred. -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// 
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName)
        {
            ExtractToFile(source, destinationFileName, false);
        }


        /// <summary>
        /// Creates a file on the file system with the entry?s contents and the specified name.
        /// The last write time of the file is set to the entry?s last write time.
        /// This method does allows overwriting of an existing file with the same name.
        /// </summary>
        /// 
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">destinationFileName is a zero-length string, contains only white space,
        /// or contains one or more invalid characters as defined by InvalidPathChars. -or- destinationFileName specifies a directory.</exception>
        /// <exception cref="ArgumentNullException">destinationFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in destinationFileName is invalid
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">destinationFileName exists and overwrite is false.
        /// -or- An I/O error has occurred.
        /// -or- The entry is currently open for writing.
        /// -or- The entry has been deleted from the archive.</exception>
        /// <exception cref="NotSupportedException">destinationFileName is in an invalid format
        /// -or- The ZipArchive that this entry belongs to was opened in a write-only mode.</exception>
        /// <exception cref="InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read
        /// -or- The entry has been compressed using a compression method that is not supported.</exception>
        /// <exception cref="ObjectDisposedException">The ZipArchive that this entry belongs to has been disposed.</exception>
        /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="overwrite">True to indicate overwrite.</param>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            // Rely on FileStream's ctor for further checking destinationFileName parameter

            Contract.EndContractBlock();

            FileMode fMode = overwrite ? FileMode.Create : FileMode.CreateNew;

            using (Stream fs = new FileStream(destinationFileName, fMode, FileAccess.Write, FileShare.None, bufferSize: 0x1000, useAsync: false))
            {
                using (Stream es = source.Open())
                    es.CopyTo(fs);
            }

            File.SetLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
        }
        #endregion ZipArchiveEntry extensions

    }  // class ZipFileExtensions
}  // namespace
