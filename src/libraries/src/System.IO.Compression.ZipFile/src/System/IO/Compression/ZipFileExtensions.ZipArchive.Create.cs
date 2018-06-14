// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.IO.Compression
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class ZipFileExtensions
    {
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
        /// <exception cref="ArgumentException">sourceFileName is a zero-length string, contains only whitespace, or contains one or more
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
        public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName) =>
            DoCreateEntryFromFile(destination, sourceFileName, entryName, null);


        /// <summary>
        /// <p>Adds a file from the file system to the archive under the specified entry name.
        /// The new entry in the archive will contain the contents of the file.
        /// The last write time of the archive entry is set to the last write time of the file on the file system.
        /// If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.
        /// If the specified source file has an invalid last modified time, the first datetime representable in the Zip timestamp format
        /// (midnight on January 1, 1980) will be used.</p>
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        /// </summary>
        /// <exception cref="ArgumentException">sourceFileName is a zero-length string, contains only whitespace, or contains one or more
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
                                                          string sourceFileName, string entryName, CompressionLevel compressionLevel) =>
            DoCreateEntryFromFile(destination, sourceFileName, entryName, compressionLevel);

        internal static ZipArchiveEntry DoCreateEntryFromFile(this ZipArchive destination,
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
    }
}
