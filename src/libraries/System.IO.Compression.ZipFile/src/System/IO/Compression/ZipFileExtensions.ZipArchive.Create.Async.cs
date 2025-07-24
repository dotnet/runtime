// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public static partial class ZipFileExtensions
{
    /// <summary>
    /// <p>Asynchronously adds a file from the file system to the archive under the specified entry name.
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
    /// <exception cref="OperationCanceledException">An asynchronous operation is cancelled.</exception>
    ///
    /// <param name="destination">The zip archive to add the file to.</param>
    /// <param name="sourceFileName">The path to the file on the file system to be copied from. The path is permitted to specify
    /// relative or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
    /// <param name="entryName">The name of the entry to be created.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A wrapper for the newly created entry.</returns>
    public static Task<ZipArchiveEntry> CreateEntryFromFileAsync(this ZipArchive destination, string sourceFileName, string entryName, CancellationToken cancellationToken = default) =>
        DoCreateEntryFromFileAsync(destination, sourceFileName, entryName, null, cancellationToken);

    /// <summary>
    /// <p>Asynchronously adds a file from the file system to the archive under the specified entry name.
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
    /// <exception cref="OperationCanceledException">An asynchronous operation is cancelled.</exception>
    ///
    /// <param name="destination">The zip archive to add the file to.</param>
    /// <param name="sourceFileName">The path to the file on the file system to be copied from. The path is permitted to specify relative
    /// or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
    /// <param name="entryName">The name of the entry to be created.</param>
    /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A wrapper for the newly created entry.</returns>
    public static Task<ZipArchiveEntry> CreateEntryFromFileAsync(this ZipArchive destination,
                                                      string sourceFileName, string entryName, CompressionLevel compressionLevel, CancellationToken cancellationToken = default) =>
        DoCreateEntryFromFileAsync(destination, sourceFileName, entryName, compressionLevel, cancellationToken);

    internal static async Task<ZipArchiveEntry> DoCreateEntryFromFileAsync(this ZipArchive destination, string sourceFileName, string entryName,
                                                    CompressionLevel? compressionLevel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        (FileStream fs, ZipArchiveEntry entry) = InitializeDoCreateEntryFromFile(destination, sourceFileName, entryName, compressionLevel, useAsync: true);

        await using (fs)
        {
            Stream es = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (es)
            {
                await fs.CopyToAsync(es, cancellationToken).ConfigureAwait(false);
            }
        }

        return entry;
    }

}
