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
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class ZipFileExtensions
    {
        /// <summary>Archives a file by compressing it and adding it to the zip archive.</summary>
        /// <param name="destination">The zip archive to add the file to.</param>
        /// <param name="sourceFileName">The path to the file to be archived. You can specify either a relative or an absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="entryName">The name of the entry to create in the zip archive.</param>
        /// <returns>A wrapper for the new entry in the zip archive.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The new entry in the archive contains the contents of the file specified by `sourceFileName`. If an entry with the specified name (`entryName`) already exists in the archive, a second entry is created with an identical name. The <xref:System.IO.Compression.ZipArchiveEntry.LastWriteTime%2A> property of the entry is set to the last time the file on the file system was changed.
        /// When `ZipArchiveMode.Update` is present, the size limit of an entry is limited to <xref:int.MaxValue?displayProperty=nameWithType>. This limit is because update mode uses a <xref:System.IO.MemoryStream> internally to allow the seeking required when updating an archive, and <xref:System.IO.MemoryStream> has a maximum equal to the size of an int.
        /// ## Examples
        /// The following example shows how to create a new entry in a zip archive from an existing file.
        /// [!code-csharp[System.IO.Compression.ZipArchive#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.ZipArchive#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program3.vb#3)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryName" /> is <see cref="string.Empty" />.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceFileName" /> or <paramref name="entryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="sourceFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="sourceFileName" /> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The file specified by <paramref name="sourceFileName" /> cannot be opened, or is too large to be updated (current limit is <see cref="int.MaxValue" />).</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="sourceFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the file specified by <paramref name="sourceFileName" />.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="sourceFileName" /> is not found.</exception>
        /// <exception cref="System.NotSupportedException">The <paramref name="sourceFileName" /> parameter is in an invalid format.
        /// -or-
        /// The zip archive does not support writing.</exception>
        /// <exception cref="System.ObjectDisposedException">The zip archive has been disposed.</exception>
        public static ZipArchiveEntry CreateEntryFromFile(this ZipArchive destination, string sourceFileName, string entryName) =>
            DoCreateEntryFromFile(destination, sourceFileName, entryName, null);

        /// <summary>Archives a file by compressing it using the specified compression level and adding it to the zip archive.</summary>
        /// <param name="destination">The zip archive to add the file to.</param>
        /// <param name="sourceFileName">The path to the file to be archived. You can specify either a relative or an absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="entryName">The name of the entry to create in the zip archive.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression effectiveness when creating the entry.</param>
        /// <returns>A wrapper for the new entry in the zip archive.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The new entry in the archive contains the contents of the file specified by `sourceFileName`. If an entry with the specified name (`entryName`) already exists in the archive, a second entry is created with an identical name. The <xref:System.IO.Compression.ZipArchiveEntry.LastWriteTime%2A> property of the entry is set to the last time the file on the file system was changed.
        /// When `ZipArchiveMode.Update` is present, the size limit of an entry is limited to <xref:int.MaxValue?displayProperty=nameWithType>. This limit is because update mode uses a <xref:System.IO.MemoryStream> internally to allow the seeking required when updating an archive, and <xref:System.IO.MemoryStream> has a maximum equal to the size of an int.
        /// ## Examples
        /// The following example shows how to create a new entry in a zip archive from an existing file, and specify the compression level.
        /// [!code-csharp[System.IO.Compression.ZipArchive#4](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program4.cs#4)]
        /// [!code-vb[System.IO.Compression.ZipArchive#4](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program4.vb#4)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryName" /> is <see cref="string.Empty" />.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceFileName" /> or <paramref name="entryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="sourceFileName" /> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.PathTooLongException">In <paramref name="sourceFileName" />, the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.IOException">The file specified by <paramref name="sourceFileName" /> cannot be opened, or is too large to be updated (current limit is <see cref="int.MaxValue" />).</exception>
        /// <exception cref="System.UnauthorizedAccessException"><paramref name="sourceFileName" /> specifies a directory.
        /// -or-
        /// The caller does not have the required permission to access the file specified by <paramref name="sourceFileName" />.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="sourceFileName" /> is not found.</exception>
        /// <exception cref="System.NotSupportedException">The <paramref name="sourceFileName" /> parameter is in an invalid format.
        /// -or-
        /// The zip archive does not support writing.</exception>
        /// <exception cref="System.ObjectDisposedException">The zip archive has been disposed.</exception>
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
