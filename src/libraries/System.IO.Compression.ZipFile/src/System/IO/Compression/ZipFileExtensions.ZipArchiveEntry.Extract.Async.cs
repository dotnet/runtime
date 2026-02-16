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
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous extract operation. The task completes when the entry contents have been written to the destination file.</returns>
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
    /// <returns>A task that represents the asynchronous extract operation. The task completes when the entry contents have been written to the destination file.</returns>
    public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, bool overwrite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ExtractToFileInitialize(source, destinationFileName, overwrite, useAsync: true, out FileStreamOptions fileStreamOptions);

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
            FileStream fs = new FileStream(extractPath, fileStreamOptions);
            await using (fs.ConfigureAwait(false))
            {
                Stream es = await source.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using (es.ConfigureAwait(false))
                {
                    await es.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }
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

    public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, string password, CancellationToken cancellationToken = default) =>
        await ExtractToFileAsync(source, destinationFileName, false, password, cancellationToken).ConfigureAwait(false);

    public static async Task ExtractToFileAsync(this ZipArchiveEntry source, string destinationFileName, bool overwrite, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ExtractToFileInitialize(source, destinationFileName, overwrite, useAsync: true, out FileStreamOptions fileStreamOptions);

        FileStream fs = new FileStream(destinationFileName, fileStreamOptions);
        await using (fs)
        {
            Stream es;
            if (!string.IsNullOrEmpty(password))
                es = await source.OpenAsync(password, cancellationToken: cancellationToken).ConfigureAwait(false);
            else
                es = await source.OpenAsync(cancellationToken).ConfigureAwait(false);
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

    internal static async Task ExtractRelativeToDirectoryAsync(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ExtractRelativeToDirectoryCheckIfFile(source, destinationDirectoryName, out string fileDestinationPath))
        {
            // If it is a file:
            // Create containing directory:
            Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
            await source.ExtractToFileAsync(fileDestinationPath, overwrite: overwrite, password: password, cancellationToken).ConfigureAwait(false);
        }
    }
}
