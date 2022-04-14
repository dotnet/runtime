// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously creates a tar stream that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destination">The destination stream of the archive.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous creation operation.</returns>
        public static Task CreateFromDirectoryAsync(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a tar file that contains all the filesystem entries from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destinationFileName">The path of the destination archive file.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory)
        {
            // Rely on Path.GetFullPath for validation of paths
            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationFileName = Path.GetFullPath(destinationFileName);

            using FileStream fs = File.Create(destinationFileName, bufferSize: 0x1000, FileOptions.None);

            using (TarWriter writer = new TarWriter(fs, TarFormat.Pax))
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

        /// <summary>
        /// Asynchronously creates a tar archive from the contents of the specified directory, and outputs them into the specified path. Can optionally include the base directory as the prefix for the the entry names.
        /// </summary>
        /// <param name="sourceDirectoryName">The path of the directory to archive.</param>
        /// <param name="destinationFileName">The path of the destination archive file.</param>
        /// <param name="includeBaseDirectory"><see langword="true"/> to include the base directory name as the first path segment in all the names of the archive entries. <see langword="false"/> to exclude the base directory name from the entry name paths.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous creation operation.</returns>
        public static Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts the contents of a stream that represents a tar archive into the specified directory.
        /// </summary>
        /// <param name="source">The stream containing the tar archive.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously extracts the contents of a stream that represents a tar archive into the specified directory.
        /// </summary>
        /// <param name="source">The stream containing the tar archive.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public static Task ExtractToDirectoryAsync(Stream source, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Extracts the contents of a tar file into the specified directory.
        /// </summary>
        /// <param name="sourceFileName">The path of the tar file to extract.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
            ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName);

            FileStreamOptions fileStreamOptions = new()
            {
                Access = FileAccess.Read,
                BufferSize = 0x1000,
                Mode = FileMode.Open,
                Share = FileShare.None
            };

            using (FileStream archive = File.Open(sourceFileName, fileStreamOptions))
            {
                using (TarReader reader = new TarReader(archive))
                {
                    TarEntry? entry;
                    while ((entry = reader.GetNextEntry()) != null)
                    {
                        entry.ExtractRelativeToDirectory(destinationDirectoryName, overwriteFiles);
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously extracts the contents of a tar file into the specified directory.
        /// </summary>
        /// <param name="sourceFileName">The path of the tar file to extract.</param>
        /// <param name="destinationDirectoryName">The path of the destination directory where the filesystem entries should be extracted.</param>
        /// <param name="overwriteFiles"><see langword="true"/> to overwrite files and directories in <paramref name="destinationDirectoryName"/>; <see langword="false"/> to avoid overwriting, and throw if any files or directories are found with existing names.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public static Task ExtractToDirectoryAsync(string sourceFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
