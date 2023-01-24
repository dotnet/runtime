// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    /// <summary>
    /// Provides static methods for creating and extracting tar archives.
    /// </summary>
    public static class TarFile
    {
        /// <summary>
        /// Creates a tar stream that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destination">The destination stream the archive.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the archive entry names.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="sourceDirectoryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="destination"/> does not support writing.</para></exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="sourceDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentNullException.ThrowIfNull(destination);

            if (!destination.CanWrite)
            {
                throw new ArgumentException(SR.IO_NotSupported_UnwritableStream, nameof(destination));
            }

            if (!Directory.Exists(sourceDirectoryName))
            {
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceDirectoryName));
            }

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            CreateFromDirectoryInternal(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: true);
        }

        /// <summary>
        /// Asynchronously creates a tar stream that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destination">The destination stream of the archive.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous creation operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="sourceDirectoryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="destination"/> does not support writing.</para></exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="sourceDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static Task CreateFromDirectoryAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentNullException.ThrowIfNull(destination);

            if (!destination.CanWrite)
            {
                return Task.FromException(new ArgumentException(SR.IO_NotSupported_UnwritableStream, nameof(destination)));
            }

            if (!Directory.Exists(sourceDirectoryName))
            {
                return Task.FromException(new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceDirectoryName)));
            }

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            return CreateFromDirectoryInternalAsync(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: true, cancellationToken);
        }

        /// <summary>
        /// Creates a tar file that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destinationFileName">The path of the destination archive file.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName"/> or <paramref name="destinationFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourceDirectoryName"/> or <paramref name="destinationFileName"/> is empty.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="sourceDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationFileName = Path.GetFullPath(destinationFileName);

            if (!Directory.Exists(sourceDirectoryName))
            {
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceDirectoryName));
            }

            // Throws if the destination file exists
            using FileStream fs = new(destinationFileName, FileMode.CreateNew, FileAccess.Write);

            CreateFromDirectoryInternal(sourceDirectoryName, fs, includeBaseDirectory, leaveOpen: false);
        }

        /// <summary>
        /// Asynchronously creates a tar archive from the contents of the specified directory, and outputs them into the specified path. Can optionally include the base directory as the prefix for the entry names.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destinationFileName">The path of the destination archive file.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous creation operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName"/> or <paramref name="destinationFileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourceDirectoryName"/> or <paramref name="destinationFileName"/> is empty.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="sourceDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationFileName = Path.GetFullPath(destinationFileName);

            if (!Directory.Exists(sourceDirectoryName))
            {
                return Task.FromException(new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, sourceDirectoryName)));
            }

            return CreateFromDirectoryInternalAsync(sourceDirectoryName, destinationFileName, includeBaseDirectory, cancellationToken);
        }

        /// <summary>
        /// Extracts the contents of a stream that represents a tar archive into the specified directory.
        /// </summary>
        /// <param name="source">The stream containing the tar archive.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destinationDirectoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="destinationDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        /// <exception cref="ArgumentException"><para>Extracting tar entry would have resulted in a file outside the specified destination directory.</para>
        /// <para>-or-</para>
        /// <para><paramref name="destinationDirectoryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="source"/> does not support reading.</para></exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            if (!source.CanRead)
            {
                throw new ArgumentException(SR.IO_NotSupported_UnreadableStream, nameof(source));
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, destinationDirectoryName));
            }

            // Rely on Path.GetFullPath for validation of paths
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            ExtractToDirectoryInternal(source, destinationDirectoryName, overwriteFiles, leaveOpen: true);
        }

        /// <summary>
        /// Asynchronously extracts the contents of a stream that represents a tar archive into the specified directory.
        /// </summary>
        /// <param name="source">The stream containing the tar archive.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="destinationDirectoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="destinationDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        /// <exception cref="ArgumentException"><para>Extracting tar entry would have resulted in a file outside the specified destination directory.</para>
        /// <para>-or-</para>
        /// <para><paramref name="destinationDirectoryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="source"/> does not support reading.</para></exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static Task ExtractToDirectoryAsync(Stream source, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            if (!source.CanRead)
            {
                return Task.FromException(new ArgumentException(SR.IO_NotSupported_UnreadableStream, nameof(source)));
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                return Task.FromException(new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, destinationDirectoryName)));
            }

            // Rely on Path.GetFullPath for validation of paths
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            return ExtractToDirectoryInternalAsync(source, destinationDirectoryName, overwriteFiles, leaveOpen: true, cancellationToken);
        }

        /// <summary>
        /// Extracts the contents of a tar file into the specified directory.
        /// </summary>
        /// <param name="sourceFileName">The path of the tar file to extract.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="destinationDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="FileNotFoundException"> The <paramref name="sourceFileName"/> file path was not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        /// <exception cref="ArgumentException"><para>Extracting tar entry would have resulted in a file outside the specified destination directory.</para>
        /// <para>-or-</para>
        /// <para><paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> is empty.</para></exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            // Rely on Path.GetFullPath for validation of paths
            sourceFileName = Path.GetFullPath(sourceFileName);
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            if (!File.Exists(sourceFileName))
            {
                throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, sourceFileName));
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, destinationDirectoryName));
            }

            using FileStream archive = File.OpenRead(sourceFileName);

            ExtractToDirectoryInternal(archive, destinationDirectoryName, overwriteFiles, leaveOpen: false);
        }

        /// <summary>
        /// Asynchronously extracts the contents of a tar file into the specified directory.
        /// </summary>
        /// <param name="sourceFileName">The path of the tar file to extract.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The <paramref name="destinationDirectoryName"/> directory path was not found.</exception>
        /// <exception cref="FileNotFoundException"> The <paramref name="sourceFileName"/> file path was not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        /// <exception cref="ArgumentException"><para>Extracting tar entry would have resulted in a file outside the specified destination directory.</para>
        /// <para>-or-</para>
        /// <para><paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> is empty.</para></exception>
        /// <exception cref="IOException">An I/O exception occurred.</exception>
        public static Task ExtractToDirectoryAsync(string sourceFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            // Rely on Path.GetFullPath for validation of paths
            sourceFileName = Path.GetFullPath(sourceFileName);
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            if (!File.Exists(sourceFileName))
            {
                return Task.FromException(new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, sourceFileName)));
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                return Task.FromException(new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, destinationDirectoryName)));
            }

            return ExtractToDirectoryInternalAsync(sourceFileName, destinationDirectoryName, overwriteFiles, cancellationToken);
        }

        // Creates an archive from the contents of a directory.
        // It assumes the sourceDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static void CreateFromDirectoryInternal(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool leaveOpen)
        {
            VerifyCreateFromDirectoryArguments(sourceDirectoryName, destination);

            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen))
            {
                DirectoryInfo di = new(sourceDirectoryName);
                string basePath = GetBasePathForCreateFromDirectory(di, includeBaseDirectory);

                bool skipBaseDirRecursion = false;
                if (includeBaseDirectory)
                {
                    writer.WriteEntry(di.FullName, GetEntryNameForBaseDirectory(di.Name));
                    skipBaseDirRecursion = (di.Attributes & FileAttributes.ReparsePoint) != 0;
                }

                if (skipBaseDirRecursion)
                {
                    // The base directory is a symlink, do not recurse into it
                    return;
                }

                foreach (FileSystemInfo file in GetFileSystemEnumerationForCreation(sourceDirectoryName))
                {
                    writer.WriteEntry(file.FullName, GetEntryNameForFileSystemInfo(file, basePath.Length));
                }
            }
        }

        // Asynchronously creates a tar archive from the contents of the specified directory, and outputs them into the specified path.
        private static async Task CreateFromDirectoryInternalAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceDirectoryName));
            Debug.Assert(!string.IsNullOrEmpty(destinationFileName));

            cancellationToken.ThrowIfCancellationRequested();

            FileStreamOptions options = new()
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew,
                Options = FileOptions.Asynchronous,
            };
            // Throws if the destination file exists
            FileStream archive = new(destinationFileName, options);
            await using (archive.ConfigureAwait(false))
            {
                await CreateFromDirectoryInternalAsync(sourceDirectoryName, archive, includeBaseDirectory, leaveOpen: false, cancellationToken).ConfigureAwait(false);
            }
        }

        // Asynchronously creates an archive from the contents of a directory.
        // It assumes the sourceDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static async Task CreateFromDirectoryInternalAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool leaveOpen, CancellationToken cancellationToken)
        {
            VerifyCreateFromDirectoryArguments(sourceDirectoryName, destination);
            cancellationToken.ThrowIfCancellationRequested();

            TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen);
            await using (writer.ConfigureAwait(false))
            {
                DirectoryInfo di = new(sourceDirectoryName);
                string basePath = GetBasePathForCreateFromDirectory(di, includeBaseDirectory);

                bool skipBaseDirRecursion = false;
                if (includeBaseDirectory)
                {
                    await writer.WriteEntryAsync(di.FullName, GetEntryNameForBaseDirectory(di.Name), cancellationToken).ConfigureAwait(false);
                    skipBaseDirRecursion = (di.Attributes & FileAttributes.ReparsePoint) != 0;
                }

                if (skipBaseDirRecursion)
                {
                    // The base directory is a symlink, do not recurse into it
                    return;
                }

                foreach (FileSystemInfo file in GetFileSystemEnumerationForCreation(sourceDirectoryName))
                {
                    await writer.WriteEntryAsync(file.FullName, GetEntryNameForFileSystemInfo(file, basePath.Length), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Generates a recursive enumeration of the filesystem entries inside the specified source directory, while
        // making sure that directory symlinks do not get recursed.
        private static FileSystemEnumerable<FileSystemInfo> GetFileSystemEnumerationForCreation(string sourceDirectoryName)
        {
            return new FileSystemEnumerable<FileSystemInfo>(
                directory: sourceDirectoryName,
                transform: (ref FileSystemEntry entry) => entry.ToFileSystemInfo(),
                options: new EnumerationOptions()
                {
                    RecurseSubdirectories = true
                })
            {
                ShouldRecursePredicate = IsNotADirectorySymlink
            };

            static bool IsNotADirectorySymlink(ref FileSystemEntry entry) => entry.IsDirectory && (entry.Attributes & FileAttributes.ReparsePoint) == 0;
        }

        // Determines what should be the base path for all the entries when creating an archive.
        private static string GetBasePathForCreateFromDirectory(DirectoryInfo di, bool includeBaseDirectory) =>
            includeBaseDirectory && di.Parent != null ? di.Parent.FullName : di.FullName;

        // Constructs the entry name used for a filesystem entry when creating an archive.
        private static string GetEntryNameForFileSystemInfo(FileSystemInfo file, int basePathLength)
        {
            bool isDirectory = (file.Attributes & FileAttributes.Directory) != 0;
            return ArchivingUtils.EntryFromPath(file.FullName.AsSpan(basePathLength), appendPathSeparator: isDirectory);
        }

        private static string GetEntryNameForBaseDirectory(string name)
        {
            return ArchivingUtils.EntryFromPath(name, appendPathSeparator: true);
        }

        // Extracts an archive into the specified directory.
        // It assumes the destinationDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static void ExtractToDirectoryInternal(Stream source, string destinationDirectoryPath, bool overwriteFiles, bool leaveOpen)
        {
            VerifyExtractToDirectoryArguments(source, destinationDirectoryPath);

            using TarReader reader = new TarReader(source, leaveOpen);

            SortedDictionary<string, UnixFileMode>? pendingModes = TarHelpers.CreatePendingModesDictionary();
            TarEntry? entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                if (entry.EntryType is not TarEntryType.GlobalExtendedAttributes)
                {
                    entry.ExtractRelativeToDirectory(destinationDirectoryPath, overwriteFiles, pendingModes);
                }
            }
            TarHelpers.SetPendingModes(pendingModes);
        }

        // Asynchronously extracts the contents of a tar file into the specified directory.
        private static async Task ExtractToDirectoryInternalAsync(string sourceFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFileName));
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectoryName));

            cancellationToken.ThrowIfCancellationRequested();

            FileStreamOptions options = new()
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.Asynchronous,
            };
            FileStream archive = new(sourceFileName, options);
            await using (archive.ConfigureAwait(false))
            {
                await ExtractToDirectoryInternalAsync(archive, destinationDirectoryName, overwriteFiles, leaveOpen: false, cancellationToken).ConfigureAwait(false);
            }
        }

        // Asynchronously extracts an archive into the specified directory.
        // It assumes the destinationDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static async Task ExtractToDirectoryInternalAsync(Stream source, string destinationDirectoryPath, bool overwriteFiles, bool leaveOpen, CancellationToken cancellationToken)
        {
            VerifyExtractToDirectoryArguments(source, destinationDirectoryPath);
            cancellationToken.ThrowIfCancellationRequested();

            SortedDictionary<string, UnixFileMode>? pendingModes = TarHelpers.CreatePendingModesDictionary();
            TarReader reader = new TarReader(source, leaveOpen);
            await using (reader.ConfigureAwait(false))
            {
                TarEntry? entry;
                while ((entry = await reader.GetNextEntryAsync(cancellationToken: cancellationToken).ConfigureAwait(false)) != null)
                {
                    if (entry.EntryType is not TarEntryType.GlobalExtendedAttributes)
                    {
                        await entry.ExtractRelativeToDirectoryAsync(destinationDirectoryPath, overwriteFiles, pendingModes, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            TarHelpers.SetPendingModes(pendingModes);
        }

        [Conditional("DEBUG")]
        private static void VerifyCreateFromDirectoryArguments(string sourceDirectoryName, Stream destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceDirectoryName));
            Debug.Assert(destination != null);
            Debug.Assert(Path.IsPathFullyQualified(sourceDirectoryName));
            Debug.Assert(destination.CanWrite);
        }

        [Conditional("DEBUG")]
        private static void VerifyExtractToDirectoryArguments(Stream source, string destinationDirectoryPath)
        {
            Debug.Assert(source != null);
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectoryPath));
            Debug.Assert(Path.IsPathFullyQualified(destinationDirectoryPath));
            Debug.Assert(source.CanRead);
        }
    }
}
