// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.IO.Compression
{
    /// <summary>Provides extension methods for the <see cref="System.IO.Compression.ZipArchive" /> and <see cref="System.IO.Compression.ZipArchiveEntry" /> classes.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.IO.Compression.ZipFileExtensions> class contains only static methods that extend the <xref:System.IO.Compression.ZipArchive> and <xref:System.IO.Compression.ZipArchiveEntry> classes. You do not create an instance of the <xref:System.IO.Compression.ZipFileExtensions> class; instead, you use these methods from instances of <xref:System.IO.Compression.ZipArchive> or <xref:System.IO.Compression.ZipArchiveEntry>.
    /// To use the extension methods, you must reference the `System.IO.Compression.FileSystem` assembly in your project. The `System.IO.Compression.FileSystem` assembly is not available in [!INCLUDE[win8_appname_long](~/includes/win8-appname-long-md.md)] apps. Therefore, the <xref:System.IO.Compression.ZipFileExtensions> and <xref:System.IO.Compression.ZipFile> classes (both of which are in the `System.IO.Compression.FileSystem` assembly) are not available in [!INCLUDE[win8_appname_long](~/includes/win8-appname-long-md.md)] apps. In [!INCLUDE[win8_appname_long](~/includes/win8-appname-long-md.md)] apps, you work with compressed files by using the methods in <xref:System.IO.Compression.ZipArchive>, <xref:System.IO.Compression.ZipArchiveEntry>, <xref:System.IO.Compression.DeflateStream>, and <xref:System.IO.Compression.GZipStream>.
    /// The <xref:System.IO.Compression.ZipFileExtensions> class contains four methods that extend <xref:System.IO.Compression.ZipArchive>:
    /// -   <xref:System.IO.Compression.ZipFileExtensions.CreateEntryFromFile%28System.IO.Compression.ZipArchive%2Cstring%2Cstring%29>
    /// -   <xref:System.IO.Compression.ZipFileExtensions.CreateEntryFromFile%28System.IO.Compression.ZipArchive%2Cstring%2Cstring%2CSystem.IO.Compression.CompressionLevel%29>
    /// -   <xref:System.IO.Compression.ZipFileExtensions.ExtractToDirectory%28System.IO.Compression.ZipArchive%2Cstring%29>
    /// -   <xref:System.IO.Compression.ZipFileExtensions.ExtractToDirectory%28System.IO.Compression.ZipArchive%2Cstring%2Cbool%29>
    /// The <xref:System.IO.Compression.ZipFileExtensions> class contains two methods that extend <xref:System.IO.Compression.ZipArchiveEntry>:
    /// -   <xref:System.IO.Compression.ZipFileExtensions.ExtractToFile%28System.IO.Compression.ZipArchiveEntry%2Cstring%29>
    /// -   <xref:System.IO.Compression.ZipFileExtensions.ExtractToFile%28System.IO.Compression.ZipArchiveEntry%2Cstring%2Cbool%29>
    /// ## Examples
    /// The following example shows how to create a new entry in a zip archive from an existing file, and extract the contents of the archive to a directory.
    /// [!code-csharp[System.IO.Compression.ZipArchive#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program3.cs#3)]
    /// [!code-vb[System.IO.Compression.ZipArchive#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program3.vb#3)]
    /// The following example shows how to iterate through the contents of a zip archive and extract files that have a .txt extension.
    /// [!code-csharp[System.IO.Compression.ZipArchive#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program1.cs#1)]
    /// [!code-vb[System.IO.Compression.ZipArchive#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program1.vb#1)]
    /// ]]></format></remarks>
    public static partial class ZipFileExtensions
    {
        /// <summary>Extracts all the files in the zip archive to a directory on the file system.</summary>
        /// <param name="source">The zip archive to extract files from.</param>
        /// <param name="destinationDirectoryName">The path to the directory to place the extracted files in. You can specify either a relative or an absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// This method creates the directory specified by `destinationDirectoryName`. If the destination directory already exists, this method does not overwrite it; it throws an <xref:System.IO.IOException> exception. The method also creates subdirectories that reflect the hierarchy in the zip archive. If an error occurs during extraction, the archive remains partially extracted. Each extracted file has the same relative path to the directory specified by `destinationDirectoryName` as its source entry has to the root of the archive.
        /// ## Examples
        /// The following example shows how to create a new entry in a zip archive from an existing file, and extract the archive to a new directory. In order to compiler this code example, you must reference the `System.IO.Compression` and `System.IO.Compression.FileSystem` assemblies in your project.
        /// [!code-csharp[System.IO.Compression.ZipArchive#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.ZipArchive#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program3.vb#3)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="destinationDirectoryName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationDirectoryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path exceeds the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The directory specified by <paramref name="destinationDirectoryName" /> already exists.
        /// -or-
        /// The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an entry from the archive would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// Two or more entries in the archive have the same name.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission to write to the destination directory.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.InvalidDataException">An archive entry cannot be found or is corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName) =>
            ExtractToDirectory(source, destinationDirectoryName, overwriteFiles: false);

        /// <summary>Extracts all of the files in the archive to a directory on the file system.</summary>
        /// <param name="source">The <see cref="System.IO.Compression.ZipArchive" /> to extract.</param>
        /// <param name="destinationDirectoryName">The path to the destination directory on the file system. The path can be relative or absolute. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="overwriteFiles"><see langword="true" /> to indicate that existing files are to be overwritten; <see langword="false" /> otherwise.</param>
        /// <remarks>The specified directory may already exist. This method will create the specified directory and all subdirectories if necessary.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to <paramref name="destinationDirectoryName" /> as the entry has to the root of the archive.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// <exception cref="System.ArgumentException"><paramref name="destinationDirectoryName" /> is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationDirectoryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The name of a <see cref="System.IO.Compression.ZipArchiveEntry" /> is zero-length, contains only whitespace, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// Extracting a <see cref="System.IO.Compression.ZipArchiveEntry" /> would have resulted in a destination file that is outside <paramref name="destinationDirectoryName" /> (for example, if the entry name contains parent directory accessors).
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationDirectoryName" /> is in an invalid format.</exception>
        /// <exception cref="System.IO.InvalidDataException">A <see cref="System.IO.Compression.ZipArchiveEntry" /> was not found or was corrupt.
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> has been compressed using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, bool overwriteFiles)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            foreach (ZipArchiveEntry entry in source.Entries)
            {
                entry.ExtractRelativeToDirectory(destinationDirectoryName, overwriteFiles);
            }
        }
    }
}
