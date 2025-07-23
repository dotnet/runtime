// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public static partial class ZipFileExtensions
{
    /// <summary>
    /// Asynchronously creates a file on the file system with the entry's contents and the specified name. The last write time of the file is set to the
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
    /// /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    public static Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, CancellationToken cancellationToken = default) =>
        ExtractToFileAsync(source, destinationFileName, false, cancellationToken);

    /// <summary>
    /// Asynchronously creates a file on the file system with the entry's contents and the specified name.
    /// The last write time of the file is set to the entry's last write time.
    /// This method does allows overwriting of an existing file with the same name.
    /// </summary>
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
    /// <exception cref="OperationCanceledException">An asynchronous operation is cancelled.</exception>
    /// <param name="source">The zip archive entry to extract a file from.</param>
    /// <param name="destinationFileName">The name of the file that will hold the contents of the entry.
    /// The path is permitted to specify relative or absolute path information.
    /// Relative path information is interpreted as relative to the current working directory.</param>
    /// <param name="overwrite">True to indicate overwrite.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ExtractToFileInitialize(source, destinationFileName, overwrite, out FileStreamOptions fileStreamOptions);

        FileStream fs = new FileStream(destinationFileName, fileStreamOptions);
        await using (fs)
        {
            Stream es = await source.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (es)
            {
                await es.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }
        }

        ExtractToFileFinalize(source, destinationFileName);
    }

    internal static async Task ExtractRelativeToDirectoryAsync(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ExtractRelativeToDirectoryCheckIfFile(source, destinationDirectoryName, out string fileDestinationPath))
        {
            // If it is a file:
            // Create containing directory:
            Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
            await source.ExtractToFileAsync(fileDestinationPath, overwrite: overwrite, cancellationToken).ConfigureAwait(false);
        }
    }
}
