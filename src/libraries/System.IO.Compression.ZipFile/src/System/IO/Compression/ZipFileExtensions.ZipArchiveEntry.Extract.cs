// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.IO.Compression
{
    /// <summary>Provides extension methods for the <see cref="System.IO.Compression.ZipArchive" /> and <see cref="System.IO.Compression.ZipArchiveEntry" /> classes.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks](~/includes/remarks/System.IO.Compression/ZipFileExtensions/ZipFileExtensions.md)]
    /// ]]></format></remarks>
    public static partial class ZipFileExtensions
    {
        /// <summary>Extracts an entry in the zip archive to a file.</summary>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The path of the file to create from the contents of the entry. You can  specify either a relative or an absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <remarks>If the destination file already exists, this method does not overwrite it; it throws an <see cref="System.IO.IOException" /> exception. To overwrite an existing file, use the <see cref="System.IO.Compression.ZipFileExtensions.ExtractToFile(ZipArchiveEntry, string, bool)" /> method overload instead.
        /// The last write time of the file is set to the last time the entry in the zip archive was changed; this value is stored in the <see cref="System.IO.Compression.ZipArchiveEntry.LastWriteTime" /> property.
        /// You cannot use this method to extract a directory; use the <see cref="O:System.IO.Compression.ZipFileExtensions.ExtractToDirectory" /> method instead.</remarks>
        /// <example>The following example shows how to iterate through the contents of a zip archive file and extract files that have a .txt extension.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.ZipArchive#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.ZipArchive#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program1.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentException"><paramref name="destinationFileName" /> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// <paramref name="destinationFileName" /> specifies a directory.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="destinationFileName" /> already exists.
        /// -or-
        /// An I/O error occurred.
        /// -or-
        /// The entry is currently open for writing.
        /// -or-
        /// The entry has been deleted from the archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission to create the new file.</exception>
        /// <exception cref="System.IO.InvalidDataException">The entry is missing from the archive, or is corrupt and cannot be read.
        /// -or-
        /// The entry has been compressed by using a compression method that is not supported.</exception>
        /// <exception cref="System.ObjectDisposedException">The zip archive that this entry belongs to has been disposed.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationFileName" /> is in an invalid format.
        /// -or-
        /// The zip archive for this entry was opened in <see cref="System.IO.Compression.ZipArchiveMode.Create" /> mode, which does not permit the retrieval of entries.</exception>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName) =>
            ExtractToFile(source, destinationFileName, false);

        /// <summary>Extracts an entry in the zip archive to a file, and optionally overwrites an existing file that has the same name.</summary>
        /// <param name="source">The zip archive entry to extract a file from.</param>
        /// <param name="destinationFileName">The path of the file to create from the contents of the entry. You can specify either a relative or an absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="overwrite"><see langword="true" /> to overwrite an existing file that has the same name as the destination file; otherwise, <see langword="false" />.</param>
        /// <remarks>The last write time of the file is set to the last time the entry in the zip archive was changed; this value is stored in the <see cref="System.IO.Compression.ZipArchiveEntry.LastWriteTime" /> property.
        /// You cannot use this method to extract a directory; use the <see cref="O:System.IO.Compression.ZipFileExtensions.ExtractToDirectory" /> method instead.</remarks>
        /// <example>The following example shows how to iterate through the contents of a zip archive file, and extract files that have a .txt extension. It overwrites an existing file that has the same name in the destination folder. In order to compiler this code example, you must reference the `System.IO.Compression` and `System.IO.Compression.FileSystem` assemblies in your project.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.ZipArchive#2](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program2.cs#2)]
        /// [!code-vb[System.IO.Compression.ZipArchive#2](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program2.vb#2)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentException"><paramref name="destinationFileName" /> is a zero-length string, contains only white space, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// <paramref name="destinationFileName" /> specifies a directory.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException"><paramref name="destinationFileName" /> already exists and <paramref name="overwrite" /> is <see langword="false" />.
        /// -or-
        /// An I/O error occurred.
        /// -or-
        /// The entry is currently open for writing.
        /// -or-
        /// The entry has been deleted from the archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission to create the new file.</exception>
        /// <exception cref="System.IO.InvalidDataException">The entry is missing from the archive or is corrupt and cannot be read.
        /// -or-
        /// The entry has been compressed by using a compression method that is not supported.</exception>
        /// <exception cref="System.ObjectDisposedException">The zip archive that this entry belongs to has been disposed.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationFileName" /> is in an invalid format.
        /// -or-
        /// The zip archive for this entry was opened in <see cref="System.IO.Compression.ZipArchiveMode.Create" /> mode, which does not permit the retrieval of entries.</exception>
        public static void ExtractToFile(this ZipArchiveEntry source, string destinationFileName, bool overwrite)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            // Rely on FileStream's ctor for further checking destinationFileName parameter
            FileMode fMode = overwrite ? FileMode.Create : FileMode.CreateNew;

            using (Stream fs = new FileStream(destinationFileName, fMode, FileAccess.Write, FileShare.None, bufferSize: 0x1000, useAsync: false))
            {
                using (Stream es = source.Open())
                    es.CopyTo(fs);
            }

            File.SetLastWriteTime(destinationFileName, source.LastWriteTime.DateTime);
        }

        internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName) =>
            ExtractRelativeToDirectory(source, destinationDirectoryName, overwrite: false);

        internal static void ExtractRelativeToDirectory(this ZipArchiveEntry source, string destinationDirectoryName, bool overwrite)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;
            if (!destinationDirectoryFullPath.EndsWith(Path.DirectorySeparatorChar))
                destinationDirectoryFullPath += Path.DirectorySeparatorChar;

            string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, source.FullName));

            if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, PathInternal.StringComparison))
                throw new IOException(SR.IO_ExtractingResultsInOutside);

            if (Path.GetFileName(fileDestinationPath).Length == 0)
            {
                // If it is a directory:

                if (source.Length != 0)
                    throw new IOException(SR.IO_DirectoryNameWithData);

                Directory.CreateDirectory(fileDestinationPath);
            }
            else
            {
                // If it is a file:
                // Create containing directory:
                Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath)!);
                source.ExtractToFile(fileDestinationPath, overwrite: overwrite);
            }
        }
    }
}
