// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;

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
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentNullException.ThrowIfNull(destination);

            if (!destination.CanWrite)
            {
                throw new IOException(SR.IO_NotSupported_UnwritableStream);
            }

            if (!Directory.Exists(sourceDirectoryName))
            {
                throw new DirectoryNotFoundException(string.Format(SR.IO_PathNotFound_Path, sourceDirectoryName));
            }

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            CreateFromDirectoryInternal(sourceDirectoryName, destination, includeBaseDirectory, leaveOpen: true);
        }

        // /// <summary>
        // /// Asynchronously creates a tar stream that contains all the filesystem entries from the specified directory.
        // /// </summary>
        // /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        // /// <param name="destination">The destination stream of the archive.</param>
        // /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <returns>A task that represents the asynchronous creation operation.</returns>
        // public static Task CreateFromDirectoryAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Creates a tar file that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destinationFileName">The path of the destination archive file.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName);
            ArgumentException.ThrowIfNullOrEmpty(destinationFileName);

            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationFileName = Path.GetFullPath(destinationFileName);

            if (!Directory.Exists(sourceDirectoryName))
            {
                throw new DirectoryNotFoundException(string.Format(SR.IO_PathNotFound_Path, sourceDirectoryName));
            }

            // Throws if the destination file exists
            using FileStream fs = new(destinationFileName, FileMode.CreateNew, FileAccess.Write);

            CreateFromDirectoryInternal(sourceDirectoryName, fs, includeBaseDirectory, leaveOpen: false);
        }

        // /// <summary>
        // /// Asynchronously creates a tar archive from the contents of the specified directory, and outputs them into the specified path. Can optionally include the base directory as the prefix for the entry names.
        // /// </summary>
        // /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        // /// <param name="destinationFileName">The path of the destination archive file.</param>
        // /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <returns>A task that represents the asynchronous creation operation.</returns>
        // public static Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Extracts the contents of a stream that represents a tar archive into the specified directory.
        /// </summary>
        /// <param name="source">The stream containing the tar archive.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        /// <exception cref="IOException">Extracting tar entry would have resulted in a file outside the specified destination directory.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            if (!source.CanRead)
            {
                throw new IOException(SR.IO_NotSupported_UnreadableStream);
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                throw new DirectoryNotFoundException(string.Format(SR.IO_PathNotFound_Path, destinationDirectoryName));
            }

            // Rely on Path.GetFullPath for validation of paths
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            ExtractToDirectoryInternal(source, destinationDirectoryName, overwriteFiles, leaveOpen: true);
        }

        // /// <summary>
        // /// Asynchronously extracts the contents of a stream that represents a tar archive into the specified directory.
        // /// </summary>
        // /// <param name="source">The stream containing the tar archive.</param>
        // /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        // /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <returns>A task that represents the asynchronous extraction operation.</returns>
        // /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        // /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        // /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        // public static Task ExtractToDirectoryAsync(Stream source, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        /// <summary>
        /// Extracts the contents of a tar file into the specified directory.
        /// </summary>
        /// <param name="sourceFileName">The path of the tar file to extract.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            // Rely on Path.GetFullPath for validation of paths
            sourceFileName = Path.GetFullPath(sourceFileName);
            destinationDirectoryName = Path.GetFullPath(destinationDirectoryName);

            if (!File.Exists(sourceFileName))
            {
                throw new FileNotFoundException(string.Format(SR.IO_FileNotFound, sourceFileName));
            }

            if (!Directory.Exists(destinationDirectoryName))
            {
                throw new DirectoryNotFoundException(string.Format(SR.IO_PathNotFound_Path, destinationDirectoryName));
            }

            using FileStream archive = File.OpenRead(sourceFileName);

            ExtractToDirectoryInternal(archive, destinationDirectoryName, overwriteFiles, leaveOpen: false);
        }

        // /// <summary>
        // /// Asynchronously extracts the contents of a tar file into the specified directory.
        // /// </summary>
        // /// <param name="sourceFileName">The path of the tar file to extract.</param>
        // /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        // /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        // /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        // /// <returns>A task that represents the asynchronous extraction operation.</returns>
        // /// <remarks><para>Files of type <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> or <see cref="TarEntryType.Fifo"/> can only be extracted in Unix platforms.</para>
        // /// <para>Elevation is required to extract a <see cref="TarEntryType.BlockDevice"/> or <see cref="TarEntryType.CharacterDevice"/> to disk.</para></remarks>
        // /// <exception cref="UnauthorizedAccessException">Operation not permitted due to insufficient permissions.</exception>
        // public static Task ExtractToDirectoryAsync(string sourceFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        // {
        //     throw new NotImplementedException();
        // }

        // Creates an archive from the contents of a directory.
        // It assumes the sourceDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static void CreateFromDirectoryInternal(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool leaveOpen)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceDirectoryName));
            Debug.Assert(destination != null);
            Debug.Assert(Path.IsPathFullyQualified(sourceDirectoryName));
            Debug.Assert(destination.CanWrite);

            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen))
            {
                bool baseDirectoryIsEmpty = true;
                DirectoryInfo di = new(sourceDirectoryName);
                string basePath = di.FullName;

                if (includeBaseDirectory && di.Parent != null)
                {
                    basePath = di.Parent.FullName;
                }

                // Windows' MaxPath (260) is used as an arbitrary default capacity, as it is likely
                // to be greater than the length of typical entry names from the file system, even
                // on non-Windows platforms. The capacity will be increased, if needed.
                const int DefaultCapacity = 260;
                char[] entryNameBuffer = ArrayPool<char>.Shared.Rent(DefaultCapacity);

                try
                {
                    foreach (FileSystemInfo file in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        baseDirectoryIsEmpty = false;

                        int entryNameLength = file.FullName.Length - basePath.Length;
                        Debug.Assert(entryNameLength > 0);

                        bool isDirectory = file.Attributes.HasFlag(FileAttributes.Directory);
                        string entryName = ArchivingUtils.EntryFromPath(file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer, appendPathSeparator: isDirectory);
                        writer.WriteEntry(file.FullName, entryName);
                    }

                    if (includeBaseDirectory && baseDirectoryIsEmpty)
                    {
                        string entryName = ArchivingUtils.EntryFromPath(di.Name, 0, di.Name.Length, ref entryNameBuffer, appendPathSeparator: true);
                        PaxTarEntry entry = new PaxTarEntry(TarEntryType.Directory, entryName);
                        writer.WriteEntry(entry);
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(entryNameBuffer);
                }
            }
        }

        // Extracts an archive into the specified directory.
        // It assumes the destinationDirectoryName is a fully qualified path, and allows choosing if the archive stream should be left open or not.
        private static void ExtractToDirectoryInternal(Stream source, string destinationDirectoryPath, bool overwriteFiles, bool leaveOpen)
        {
            Debug.Assert(source != null);
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectoryPath));
            Debug.Assert(Path.IsPathFullyQualified(destinationDirectoryPath));
            Debug.Assert(source.CanRead);

            using TarReader reader = new TarReader(source, leaveOpen);

            TarEntry? entry;
            while ((entry = reader.GetNextEntry()) != null)
            {
                entry.ExtractRelativeToDirectory(destinationDirectoryPath, overwriteFiles);
            }
        }
    }
}
