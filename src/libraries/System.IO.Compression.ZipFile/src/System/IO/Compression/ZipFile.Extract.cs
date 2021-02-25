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
        /// <summary>Extracts all the files in the specified zip archive to a directory on the file system.</summary>
        /// <param name="sourceArchiveFileName">The path to the archive that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <remarks>This method creates the specified directory and all subdirectories. The destination directory cannot already exist. Exceptions related to validating the paths in the <paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted. Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName" /> as its source entry has to the root of the archive.</remarks>
        /// <example>This example shows how to create and extract a zip archive by using the <see cref="System.IO.Compression.ZipFile" /> class. It compresses the contents of a folder into a zip archive and extracts that content to a new folder. To use the <see cref="System.IO.Compression.ZipFile" /> class, you must reference the `System.IO.Compression.FileSystem` assembly in your project.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.ZipFile#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.zipfile/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.ZipFile#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.zipfile/vb/program1.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The directory specified by <paramref name="destinationDirectoryName" /> already exists.
        /// -or-
        /// The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// An archive entry to extract has the same name as an entry that has already been extracted from the same archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="sourceArchiveFileName" /> was not found.</exception>
        /// <exception cref="System.IO.InvalidDataException">The archive specified by <paramref name="sourceArchiveFileName" /> is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: false);

        /// <summary>Extracts all of the files in the specified archive to a directory on the file system.</summary>
        /// <param name="sourceArchiveFileName">The path on the file system to the archive that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the destination directory on the file system. The directory specified must not exist, but the directory that it is contained in must exist.</param>
        /// <param name="overwriteFiles"><see langword="true" /> to overwrite files; <see langword="false" /> otherwise.</param>
        /// <remarks>The specified directory must not exist. The method creates the specified directory and all subdirectories.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to the <paramref name="destinationDirectoryName" /> as the entry has to the archive.
        /// The path can specify relative or absolute path information. A relative path is interpreted as relative to the current working directory.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is a zero-length string, contains only whitespace, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> specifies a path, a file name, or both that exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The path specified by <paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The directory specified by <paramref name="destinationDirectoryName" /> already exists.
        /// -or-
        /// An I/O error has occurred.
        /// -or-
        /// The name of a <see cref="System.IO.Compression.ZipArchiveEntry" /> is zero-length, contains only whitespace, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// Extracting a <see cref="System.IO.Compression.ZipArchiveEntry" /> would result in a file destination that is outside the destination directory (for example, because of parent directory accessors).
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is in an invalid format.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="sourceArchiveFileName" /> was not found.</exception>
        /// <exception cref="System.IO.InvalidDataException">The archive specified by <paramref name="sourceArchiveFileName" /> is not a valid <see cref="System.IO.Compression.ZipArchive" />.
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> was not found or was corrupt.
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> has been compressed using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: overwriteFiles);

        /// <summary>Extracts all the files in the specified zip archive to a directory on the file system and uses the specified character encoding for entry names.</summary>
        /// <param name="sourceArchiveFileName">The path to the archive that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <remarks>This method creates the specified directory and all subdirectories. The destination directory cannot already exist. Exceptions related to validating the paths in the <paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted. Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName" /> as its source entry has to the root of the archive.
        /// If <paramref name="entryNameEncoding" /> is set to a value other than <see langword="null" />, entry names are decoded according to the following rules:
        /// -   For entry names where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, the entry names are decoded by using the specified encoding.
        /// -   For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.
        /// If <paramref name="entryNameEncoding" /> is set to <see langword="null" />, entry names are decoded according to the following rules:
        /// -   For entries where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, entry names are decoded by using the current system default code page.
        /// -   For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.</remarks>
        /// <exception cref="System.ArgumentException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryNameEncoding" /> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The directory specified by <paramref name="destinationDirectoryName" /> already exists.
        /// -or-
        /// The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// An archive entry to extract has the same name as an entry that has already been extracted from the same archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="destinationDirectoryName" /> or <paramref name="sourceArchiveFileName" /> contains an invalid format.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="sourceArchiveFileName" /> was not found.</exception>
        /// <exception cref="System.IO.InvalidDataException">The archive specified by <paramref name="sourceArchiveFileName" /> is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: entryNameEncoding, overwriteFiles: false);

        /// <summary>Extracts all of the files in the specified archive to a directory on the file system.</summary>
        /// <param name="sourceArchiveFileName">The path on the file system to the archive that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the destination directory on the file system. The directory specified must not exist, but the directory that it is contained in must exist.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading entry names in this <see cref="System.IO.Compression.ZipArchive" />.</param>
        /// <param name="overwriteFiles"><see langword="true" /> to overwrite files; <see langword="false" /> otherwise.</param>
        /// <remarks>The specified directory must not exist. This method will create the specified directory and all subdirectories.
        /// If there is an error while extracting the archive, the archive will remain partially extracted.
        /// Each entry will be extracted such that the extracted file has the same relative path to the <paramref name="destinationDirectoryName" /> as the entry has to the archive.
        /// The path can specify relative or absolute path information. A relative path is interpreted as relative to the current working directory.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// <exception cref="System.ArgumentException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is a zero-length string, contains only whitespace, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// <paramref name="entryNameEncoding" /> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is <see langword="null" />.</exception>
        /// <exception cref="System.IO.PathTooLongException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> specifies a path, a file name, or both that exceed the system-defined maximum length.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The path specified by <paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="System.IO.IOException">The directory specified by <paramref name="destinationDirectoryName" /> already exists.
        /// -or-
        /// An I/O error has occurred.
        /// -or-
        /// The name of a <see cref="System.IO.Compression.ZipArchiveEntry" /> is zero-length, contains only whitespace, or contains one or more invalid characters as defined by <see cref="System.IO.Path.InvalidPathChars" />.
        /// -or-
        /// Extracting a <see cref="System.IO.Compression.ZipArchiveEntry" /> would result in a file destination that is outside the destination directory (for example, because of parent directory accessors).
        /// -or-
        /// A <see cref="System.IO.Compression.ZipArchiveEntry" /> has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="sourceArchiveFileName" /> or <paramref name="destinationDirectoryName" /> is in an invalid format.</exception>
        /// <exception cref="System.IO.FileNotFoundException"><paramref name="sourceArchiveFileName" /> was not found.</exception>
        /// <exception cref="System.IO.InvalidDataException">The archive specified by <paramref name="sourceArchiveFileName" /> is not a valid <see cref="System.IO.Compression.ZipArchive" />.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry has been compressed using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
        {
            if (sourceArchiveFileName == null)
                throw new ArgumentNullException(nameof(sourceArchiveFileName));

            using (ZipArchive archive = Open(sourceArchiveFileName, ZipArchiveMode.Read, entryNameEncoding))
            {
                archive.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
            }
        }
    }
}
