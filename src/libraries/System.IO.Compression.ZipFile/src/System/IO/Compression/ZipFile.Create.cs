// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.IO.Compression
{
    /// <summary>Provides static methods for creating, extracting, and opening zip archives.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks](~/includes/remarks/System.IO.Compression/ZipFile/ZipFile.md)]
    /// ]]></format></remarks>
    public static partial class ZipFile
    {
        /// <summary>Opens a zip archive for reading at the specified path.</summary>
        /// <param name="archiveFileName">The path to the archive to open, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <returns>The opened zip archive.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method is equivalent to calling the <xref:System.IO.Compression.ZipFile.Open%2A> method and setting the `mode` parameter to <xref:System.IO.Compression.ZipArchiveMode.Read>. The archive is opened with <xref:System.IO.FileMode.Open?displayProperty=nameWithType> as the file mode value. If the archive does not exist, a <xref:System.IO.FileNotFoundException> exception is thrown.
        /// ## Examples
        /// The following example shows how to open a zip archive for reading.
        /// [!code-csharp[System.IO.Compression.ZipArchive#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.ZipArchive#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program1.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="archiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="archiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="archiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="archiveFileName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="archiveFileName" /> could not be opened.
        /// -or-
        /// An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="archiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the file specified in <paramref name="archiveFileName" />.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified in <paramref name="archiveFileName" /> is not found.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="archiveFileName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.InvalidDataException"><paramref name="archiveFileName" /> could not be interpreted as a zip archive.</exception>
        public static ZipArchive OpenRead(string archiveFileName) => Open(archiveFileName, ZipArchiveMode.Read);

        /// <summary>Opens a zip archive at the specified path and in the specified mode.</summary>
        /// <param name="archiveFileName">The path to the archive to open, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="mode">One of the enumeration values that specifies the actions which are allowed on the entries in the opened archive.</param>
        /// <returns>The opened zip archive.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// When you set the `mode` parameter to <xref:System.IO.Compression.ZipArchiveMode.Read>, the archive is opened with <xref:System.IO.FileMode.Open> from the <xref:System.IO.FileMode> enumeration as the file mode value. If the archive does not exist, a <xref:System.IO.FileNotFoundException> exception is thrown. Setting the `mode` parameter to <xref:System.IO.Compression.ZipArchiveMode.Read> is equivalent to calling the <xref:System.IO.Compression.ZipFile.OpenRead%2A> method.
        /// When you set the `mode` parameter to <xref:System.IO.Compression.ZipArchiveMode.Create>, the archive is opened with <xref:System.IO.FileMode.CreateNew?displayProperty=nameWithType> as the file mode value. If the archive already exists, an <xref:System.IO.IOException> is thrown.
        /// When you set the `mode` parameter to <xref:System.IO.Compression.ZipArchiveMode.Update>,  the archive is opened with <xref:System.IO.FileMode.OpenOrCreate?displayProperty=nameWithType> as the file mode value. If the archive exists, it is opened. The existing entries can be modified and new entries can be created. If the archive does not exist, a new archive is created; however, creating a zip archive in <xref:System.IO.Compression.ZipArchiveMode.Update> mode is not as efficient as creating it in <xref:System.IO.Compression.ZipArchiveMode.Create> mode.
        /// ## Examples
        /// The following example shows how to open a zip archive in the update mode and add an entry to the archive.
        /// [!code-csharp[System.IO.Compression.ZipArchive#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.ZipArchive#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program3.vb#3)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="archiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="archiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="archiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="archiveFileName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="archiveFileName" /> could not be opened.
        /// -or-
        /// <paramref name="mode" /> is set to <see cref="System.IO.Compression.ZipArchiveMode.Create" />, but the file specified in <paramref name="archiveFileName" /> already exists.
        /// -or-
        /// An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="archiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the file specified in <paramref name="archiveFileName" />.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode" /> specifies an invalid value.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="mode" /> is set to <see cref="System.IO.Compression.ZipArchiveMode.Read" />, but the file specified in <paramref name="archiveFileName" /> is not found.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="archiveFileName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.InvalidDataException"><paramref name="archiveFileName" /> could not be interpreted as a zip archive.
        /// -or-
        /// <paramref name="mode" /> is <see cref="System.IO.Compression.ZipArchiveMode.Update" />, but an entry is missing or corrupt and cannot be read.
        /// -or-
        /// <paramref name="mode" /> is <see cref="System.IO.Compression.ZipArchiveMode.Update" />, but an entry is too large to fit into memory.</exception>
        public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode) => Open(archiveFileName, mode, entryNameEncoding: null);

        /// <summary>Opens a zip archive at the specified path, in the specified mode, and by using the specified character encoding for entry names.</summary>
        /// <param name="archiveFileName">The path to the archive to open, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="mode">One of the enumeration values that specifies the actions that are allowed on the entries in the opened archive.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <returns>The opened zip archive.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// [!INCLUDE[remarks](~/includes/remarks/System.IO.Compression/ZipFile/Open.md)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="archiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryNameEncoding" /> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="archiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="archiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="archiveFileName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="archiveFileName" /> could not be opened.
        /// -or-
        /// <paramref name="mode" /> is set to <see cref="System.IO.Compression.ZipArchiveMode.Create" />, but the file specified in <paramref name="archiveFileName" /> already exists.
        /// -or-
        /// An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="archiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the file specified in <paramref name="archiveFileName" />.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode" /> specifies an invalid value.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="mode" /> is set to <see cref="System.IO.Compression.ZipArchiveMode.Read" />, but the file specified in <paramref name="archiveFileName" /> is not found.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="archiveFileName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.InvalidDataException"><paramref name="archiveFileName" /> could not be interpreted as a zip archive.
        /// -or-
        /// <paramref name="mode" /> is <see cref="System.IO.Compression.ZipArchiveMode.Update" />, but an entry is missing or corrupt and cannot be read.
        /// -or-
        /// <paramref name="mode" /> is <see cref="System.IO.Compression.ZipArchiveMode.Update" />, but an entry is too large to fit into memory.</exception>
        public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode, Encoding? entryNameEncoding)
        {
            // Relies on FileStream's ctor for checking of archiveFileName

            FileMode fileMode;
            FileAccess access;
            FileShare fileShare;

            switch (mode)
            {
                case ZipArchiveMode.Read:
                    fileMode = FileMode.Open;
                    access = FileAccess.Read;
                    fileShare = FileShare.Read;
                    break;

                case ZipArchiveMode.Create:
                    fileMode = FileMode.CreateNew;
                    access = FileAccess.Write;
                    fileShare = FileShare.None;
                    break;

                case ZipArchiveMode.Update:
                    fileMode = FileMode.OpenOrCreate;
                    access = FileAccess.ReadWrite;
                    fileShare = FileShare.None;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            // Suppress CA2000: fs gets passed to the new ZipArchive, which stores it internally.
            // The stream will then be owned by the archive and be disposed when the archive is disposed.
            // If the ctor completes without throwing, we know fs has been successfully stores in the archive;
            // If the ctor throws, we need to close it here.

            FileStream fs = new FileStream(archiveFileName, fileMode, access, fileShare, bufferSize: 0x1000, useAsync: false);

            try
            {
                return new ZipArchive(fs, mode, leaveOpen: false, entryNameEncoding: entryNameEncoding);
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        /// <summary>Creates a zip archive that contains the files and directories from the specified directory.</summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destinationArchiveFileName">The path of the archive to be created, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created. This method overload does not include the base directory in the archive and does not allow you to specify a compression level. If you want to include the base directory or specify a compression level, call the <xref:System.IO.Compression.ZipFile.CreateFromDirectory%28string%2Cstring%2CSystem.IO.Compression.CompressionLevel%2Cbool%29> method overload.
        /// If the archive already exists, an <xref:System.IO.IOException> exception is thrown. If an entry with the specified name already exists in the archive, a second entry is created with an identical name.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <xref:System.IO.IOException> exception.
        /// ## Examples
        /// This example shows how to create and extract a zip archive by using the <xref:System.IO.Compression.ZipFile> class. It compresses the contents of a folder into a zip archive, and then extracts that content to a new folder. To use the <xref:System.IO.Compression.ZipFile> class, you must reference the `System.IO.Compression.FileSystem` assembly in your project.
        /// [!code-csharp[System.IO.Compression.ZipFile#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.zipfile/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.ZipFile#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.zipfile/vb/program1.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="destinationArchiveFileName" /> already exists.
        /// -or-
        /// A file in the specified directory could not be opened.
        /// -or-
        /// An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="destinationArchiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the directory specified in <paramref name="sourceDirectoryName" /> or the file specified in <paramref name="destinationArchiveFileName" />.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> contains an invalid format.
        /// -or-
        /// The zip archive does not support writing.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel: null, includeBaseDirectory: false, entryNameEncoding: null);

        /// <summary>Creates a zip archive that contains the files and directories from the specified directory, uses the specified compression level, and optionally includes the base directory.</summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destinationArchiveFileName">The path of the archive to be created, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression effectiveness when creating the entry.</param>
        /// <param name="includeBaseDirectory"><see langword="true" /> to include the directory name from <paramref name="sourceDirectoryName" /> at the root of the archive; <see langword="false" /> to include only the contents of the directory.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created. Use this method overload to specify the compression level and whether to include the base directory in the archive.
        /// If the archive already exists, an <xref:System.IO.IOException> exception is thrown. If an entry with the specified name already exists in the archive, a second entry is created with an identical name.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <xref:System.IO.IOException> exception.
        /// ## Examples
        /// This example shows how to create and extract a zip archive by using the <xref:System.IO.Compression.ZipFile> class. It compresses the contents of a folder into a zip archive, and then extracts that content to a new folder. When compressing the archive, the base directory is included and the compression level is set to emphasize the speed of the operation over efficiency. To use the <xref:System.IO.Compression.ZipFile> class, you must reference the `System.IO.Compression.FileSystem` assembly in your project.
        /// [!code-csharp[System.IO.Compression.ZipFile#2](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.zipfile/cs/program2.cs#2)]
        /// [!code-vb[System.IO.Compression.ZipFile#2](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.zipfile/vb/program2.vb#2)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="destinationArchiveFileName" /> already exists.
        /// -or-
        /// A file in the specified directory could not be opened.
        /// -or-
        /// An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="destinationArchiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the directory specified in <paramref name="sourceDirectoryName" /> or the file specified in <paramref name="destinationArchiveFileName" />.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> contains an invalid format.
        /// -or-
        /// The zip archive does not support writing.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding: null);

        /// <summary>Creates a zip archive that contains the files and directories from the specified directory, uses the specified compression level and character encoding for entry names, and optionally includes the base directory.</summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destinationArchiveFileName">The path of the archive to be created, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression effectiveness when creating the entry.</param>
        /// <param name="includeBaseDirectory"><see langword="true" /> to include the directory name from <paramref name="sourceDirectoryName" /> at the root of the archive; <see langword="false" /> to include only the contents of the directory.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <remarks>The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created. Use this method overload to specify the compression level and character encoding, and whether to include the base directory in the archive.
        /// If the archive already exists, an <see cref="System.IO.IOException" /> exception is thrown. If an entry with the specified name already exists in the archive, a second entry is created with an identical name.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <see cref="System.IO.IOException" /> exception.
        /// If <paramref name="entryNameEncoding" /> is set to a value other than <see langword="null" />, the entry names are encoded by using the specified encoding. If the specified encoding is a UTF-8, the language encoding flag (in the general-purpose bit flag of the local file header) is set for each entry,
        /// If <paramref name="entryNameEncoding" /> is set to <see langword="null" />, the entry names are encoded according to the following rules:
        /// -   For entry names that contain characters outside the ASCII range, the language encoding flag is set, and UTF-8 is used to encode the entry name.
        /// -   For entry names that contain only ASCII characters, the language encoding flag is set, and the current system default code page is used to encode the entry names.</remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryNameEncoding" /> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="destinationArchiveFileName" /> already exists.
        /// -or-
        /// A file in the specified directory could not be opened.
        /// -or-
        /// An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="destinationArchiveFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the directory specified in <paramref name="sourceDirectoryName" /> or the file specified in <paramref name="destinationArchiveFileName" />.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="sourceDirectoryName" /> or <paramref name="destinationArchiveFileName" /> contains an invalid format.
        /// -or-
        /// The zip archive does not support writing.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName,
                                               CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);

        private static void DoCreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName,
                                                  CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)

        {
            // Rely on Path.GetFullPath for validation of sourceDirectoryName and destinationArchive

            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            using (ZipArchive archive = Open(destinationArchiveFileName, ZipArchiveMode.Create, entryNameEncoding))
            {
                bool directoryIsEmpty = true;

                //add files and directories
                DirectoryInfo di = new DirectoryInfo(sourceDirectoryName);

                string basePath = di.FullName;

                if (includeBaseDirectory && di.Parent != null)
                    basePath = di.Parent.FullName;

                // Windows' MaxPath (260) is used as an arbitrary default capacity, as it is likely
                // to be greater than the length of typical entry names from the file system, even
                // on non-Windows platforms. The capacity will be increased, if needed.
                const int DefaultCapacity = 260;
                char[] entryNameBuffer = ArrayPool<char>.Shared.Rent(DefaultCapacity);

                try
                {
                    foreach (FileSystemInfo file in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        directoryIsEmpty = false;

                        int entryNameLength = file.FullName.Length - basePath.Length;
                        Debug.Assert(entryNameLength > 0);

                        if (file is FileInfo)
                        {
                            // Create entry for file:
                            string entryName = ZipFileUtils.EntryFromPath(file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer);
                            ZipFileExtensions.DoCreateEntryFromFile(archive, file.FullName, entryName, compressionLevel);
                        }
                        else
                        {
                            // Entry marking an empty dir:
                            if (file is DirectoryInfo possiblyEmpty && ZipFileUtils.IsDirEmpty(possiblyEmpty))
                            {
                                // FullName never returns a directory separator character on the end,
                                // but Zip archives require it to specify an explicit directory:
                                string entryName = ZipFileUtils.EntryFromPath(file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer, appendPathSeparator: true);
                                archive.CreateEntry(entryName);
                            }
                        }
                    }  // foreach

                    // If no entries create an empty root directory entry:
                    if (includeBaseDirectory && directoryIsEmpty)
                        archive.CreateEntry(ZipFileUtils.EntryFromPath(di.Name, 0, di.Name.Length, ref entryNameBuffer, appendPathSeparator: true));
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(entryNameBuffer);
                }

            }
        }
    }
}
