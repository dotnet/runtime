// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.IO.Enumeration;

namespace System.IO.Compression
{
    public static partial class ZipFile
    {
        /// <summary>
        /// Opens a <code>ZipArchive</code> on the specified path for reading. The specified file is opened with <code>FileMode.Open</code>.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">archiveFileName is a zero-length string, contains only whitespace, or contains one
        ///                                     or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">archiveFileName is null.</exception>
        /// <exception cref="PathTooLongException">The specified archiveFileName exceeds the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters,
        ///                                        and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified archiveFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="UnauthorizedAccessException">archiveFileName specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="FileNotFoundException">The file specified in archiveFileName was not found.</exception>
        /// <exception cref="NotSupportedException">archiveFileName is in an invalid format. </exception>
        /// <exception cref="InvalidDataException">The specified file could not be interpreted as a Zip file.</exception>
        ///
        /// <param name="archiveFileName">A string specifying the path on the filesystem to open the archive on. The path is permitted
        /// to specify relative or absolute path information. Relative path information is interpreted as relative to the current working directory.</param>
        public static ZipArchive OpenRead(string archiveFileName) => Open(archiveFileName, ZipArchiveMode.Read);

        /// <summary>
        /// Opens a <code>ZipArchive</code> on the specified <code>archiveFileName</code> in the specified <code>ZipArchiveMode</code> mode.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">archiveFileName is a zero-length string, contains only whitespace,
        ///                                     or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">path is null.</exception>
        /// <exception cref="PathTooLongException">The specified archiveFileName exceeds the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters,
        ///                                        and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified archiveFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="UnauthorizedAccessException">archiveFileName specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><code>mode</code> specified an invalid value.</exception>
        /// <exception cref="FileNotFoundException">The file specified in <code>archiveFileName</code> was not found. </exception>
        /// <exception cref="NotSupportedException"><code>archiveFileName</code> is in an invalid format.</exception>
        /// <exception cref="InvalidDataException">The specified file could not be interpreted as a Zip file.
        ///                                        -OR- <code>mode</code> is <code>Update</code> and an entry is missing from the archive or
        ///                                        is corrupt and cannot be read.
        ///                                        -OR- <code>mode</code> is <code>Update</code> and an entry is too large to fit into memory.</exception>
        ///
        /// <param name="archiveFileName">A string specifying the path on the filesystem to open the archive on.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="mode">See the description of the <code>ZipArchiveMode</code> enum.
        /// If <code>Read</code> is specified, the file is opened with <code>System.IO.FileMode.Open</code>, and will throw
        /// a <code>FileNotFoundException</code> if the file does not exist.
        /// If <code>Create</code> is specified, the file is opened with <code>System.IO.FileMode.CreateNew</code>, and will throw
        /// a <code>System.IO.IOException</code> if the file already exists.
        /// If <code>Update</code> is specified, the file is opened with <code>System.IO.FileMode.OpenOrCreate</code>.
        /// If the file exists and is a Zip file, its entries will become accessible, and may be modified, and new entries may be created.
        /// If the file exists and is not a Zip file, a <code>ZipArchiveException</code> will be thrown.
        /// If the file exists and is empty or does not exist, a new Zip file will be created.
        /// Note that creating a Zip file with the <code>ZipArchiveMode.Create</code> mode is more efficient when creating a new Zip file.</param>
        public static ZipArchive Open(string archiveFileName, ZipArchiveMode mode) => Open(archiveFileName, mode, entryNameEncoding: null);

        /// <summary>
        /// Opens a <code>ZipArchive</code> on the specified <code>archiveFileName</code> in the specified <code>ZipArchiveMode</code> mode.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">archiveFileName is a zero-length string, contains only whitespace,
        ///                                     or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">path is null.</exception>
        /// <exception cref="PathTooLongException">The specified archiveFileName exceeds the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters,
        ///                                        and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified archiveFileName is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An unspecified I/O error occurred while opening the file.</exception>
        /// <exception cref="UnauthorizedAccessException">archiveFileName specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><code>mode</code> specified an invalid value.</exception>
        /// <exception cref="FileNotFoundException">The file specified in <code>archiveFileName</code> was not found. </exception>
        /// <exception cref="NotSupportedException"><code>archiveFileName</code> is in an invalid format.</exception>
        /// <exception cref="InvalidDataException">The specified file could not be interpreted as a Zip file.
        ///                                        -OR- <code>mode</code> is <code>Update</code> and an entry is missing from the archive or
        ///                                        is corrupt and cannot be read.
        ///                                        -OR- <code>mode</code> is <code>Update</code> and an entry is too large to fit into memory.</exception>
        ///
        /// <param name="archiveFileName">A string specifying the path on the filesystem to open the archive on.
        /// The path is permitted to specify relative or absolute path information.
        /// Relative path information is interpreted as relative to the current working directory.</param>
        /// <param name="mode">See the description of the <code>ZipArchiveMode</code> enum.
        /// If <code>Read</code> is specified, the file is opened with <code>System.IO.FileMode.Open</code>, and will throw
        /// a <code>FileNotFoundException</code> if the file does not exist.
        /// If <code>Create</code> is specified, the file is opened with <code>System.IO.FileMode.CreateNew</code>, and will throw
        /// a <code>System.IO.IOException</code> if the file already exists.
        /// If <code>Update</code> is specified, the file is opened with <code>System.IO.FileMode.OpenOrCreate</code>.
        /// If the file exists and is a Zip file, its entries will become accessible, and may be modified, and new entries may be created.
        /// If the file exists and is not a Zip file, a <code>ZipArchiveException</code> will be thrown.
        /// If the file exists and is empty or does not exist, a new Zip file will be created.
        /// Note that creating a Zip file with the <code>ZipArchiveMode.Create</code> mode is more efficient when creating a new Zip file.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this ZipArchive.
        ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
        ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
        ///         UTF-8 encoding for entry names.<br />
        ///         This value is used as follows:</para>
        ///     <para><strong>Reading (opening) ZIP archive files:</strong></para>
        ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
        ///     <list>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
        ///         use the current system default code page (<c>Encoding.Default</c>) in order to decode the entry name.</item>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
        ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name.</item>
        ///     </list>
        ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
        ///     <list>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header is <em>not</em> set,
        ///         use the specified <c>entryNameEncoding</c> in order to decode the entry name.</item>
        ///         <item>For entries where the language encoding flag (EFS) in the general purpose bit flag of the local file header <em>is</em> set,
        ///         use UTF-8 (<c>Encoding.UTF8</c>) in order to decode the entry name.</item>
        ///     </list>
        ///     <para><strong>Writing (saving) ZIP archive files:</strong></para>
        ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
        ///     <list>
        ///         <item>For entry names that contain characters outside the ASCII range,
        ///         the language encoding flag (EFS) will be set in the general purpose bit flag of the local file header,
        ///         and UTF-8 (<c>Encoding.UTF8</c>) will be used in order to encode the entry name into bytes.</item>
        ///         <item>For entry names that do not contain characters outside the ASCII range,
        ///         the language encoding flag (EFS) will not be set in the general purpose bit flag of the local file header,
        ///         and the current system default code page (<c>Encoding.Default</c>) will be used to encode the entry names into bytes.</item>
        ///     </list>
        ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
        ///     <list>
        ///         <item>The specified <c>entryNameEncoding</c> will always be used to encode the entry names into bytes.
        ///         The language encoding flag (EFS) in the general purpose bit flag of the local file header will be set if and only
        ///         if the specified <c>entryNameEncoding</c> is a UTF-8 encoding.</item>
        ///     </list>
        ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
        ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
        /// </param>
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

        /// <summary>
        /// <p>Creates a Zip archive at the path <code>destinationArchiveFileName</code> that contains the files and directories from
        /// the directory specified by <code>sourceDirectoryName</code>. The directory structure is preserved in the archive, and a
        /// recursive search is done for files to be archived. The archive must not exist. If the directory is empty, an empty
        /// archive will be created. If a file in the directory cannot be added to the archive, the archive will be left incomplete
        /// and invalid and the method will throw an exception. This method does not include the base directory into the archive.
        /// If an error is encountered while adding files to the archive, this method will stop adding files and leave the archive
        /// in an invalid state. The paths are permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file in the archive has data in the last write time
        /// field that is not a valid Zip timestamp, an indicator value of 1980 January 1 at midnight will be used for the file's
        /// last modified time.</p>
        ///
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        ///
        /// <p>Since no <code>CompressionLevel</code> is specified, the default provided by the implementation of the underlying compression
        /// algorithm will be used; the <code>ZipArchive</code> will not impose its own default.
        /// (Currently, the underlying compression algorithm is provided by the <code>System.IO.Compression.DeflateStream</code> class.)</p>
        /// </summary>
        ///
        /// <exception cref="ArgumentException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is a zero-length
        ///                                     string, contains only whitespace, or contains one or more invalid characters as defined by
        ///                                     <code>InvalidPathChars</code>.</exception>
        /// <exception cref="ArgumentNullException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is null.</exception>
        /// <exception cref="PathTooLongException">In <code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>, the specified
        ///                                        path, file name, or both exceed the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters, and file
        ///                                        names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in <code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>
        ///                                              is invalid, (for example, it is on an unmapped drive).
        ///                                              -OR- The directory specified by <code>sourceDirectoryName</code> does not exist.</exception>
        /// <exception cref="IOException"><code>destinationArchiveFileName</code> already exists.
        ///                                     -OR- An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="UnauthorizedAccessException"><code>destinationArchiveFileName</code> specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is
        ///                                         in an invalid format.</exception>
        ///
        /// <param name="sourceDirectoryName">The path to the directory on the file system to be archived. </param>
        /// <param name="destinationArchiveFileName">The name of the archive to be created.</param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel: null, includeBaseDirectory: false, entryNameEncoding: null);

        /// <summary>
        /// <p>Creates a Zip archive at the path <code>destinationArchiveFileName</code> that contains the files and directories in the directory
        /// specified by <code>sourceDirectoryName</code>. The directory structure is preserved in the archive, and a recursive search is
        /// done for files to be archived. The archive must not exist. If the directory is empty, an empty archive will be created.
        /// If a file in the directory cannot be added to the archive, the archive will be left incomplete and invalid and the
        /// method will throw an exception. This method optionally includes the base directory in the archive.
        /// If an error is encountered while adding files to the archive, this method will stop adding files and leave the archive
        /// in an invalid state. The paths are permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file in the archive has data in the last write time
        /// field that is not a valid Zip timestamp, an indicator value of 1980 January 1 at midnight will be used for the file's
        /// last modified time.</p>
        ///
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        ///
        /// <p>Since no <code>CompressionLevel</code> is specified, the default provided by the implementation of the underlying compression
        /// algorithm will be used; the <code>ZipArchive</code> will not impose its own default.
        /// (Currently, the underlying compression algorithm is provided by the <code>System.IO.Compression.DeflateStream</code> class.)</p>
        /// </summary>
        ///
        /// <exception cref="ArgumentException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is a zero-length
        ///                                     string, contains only whitespace, or contains one or more invalid characters as defined by
        ///                                     <code>InvalidPathChars</code>.</exception>
        /// <exception cref="ArgumentNullException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is null.</exception>
        /// <exception cref="PathTooLongException">In <code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>, the
        ///                                        specified path, file name, or both exceed the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters,
        ///                                        and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in <code>sourceDirectoryName</code> or
        ///                                              <code>destinationArchiveFileName</code> is invalid, (for example, it is on an unmapped drive).
        ///                                              -OR- The directory specified by <code>sourceDirectoryName</code> does not exist.</exception>
        /// <exception cref="IOException"><code>destinationArchiveFileName</code> already exists.
        ///                               -OR- An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="UnauthorizedAccessException"><code>destinationArchiveFileName</code> specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>
        ///                                         is in an invalid format.</exception>
        ///
        /// <param name="sourceDirectoryName">The path to the directory on the file system to be archived.</param>
        /// <param name="destinationArchiveFileName">The name of the archive to be created.</param>
        /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
        /// <param name="includeBaseDirectory"><code>true</code> to indicate that a directory named <code>sourceDirectoryName</code> should
        /// be included at the root of the archive. <code>false</code> to indicate that the files and directories in <code>sourceDirectoryName</code>
        /// should be included directly in the archive.</param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding: null);

        /// <summary>
        /// <p>Creates a Zip archive at the path <code>destinationArchiveFileName</code> that contains the files and directories in the directory
        /// specified by <code>sourceDirectoryName</code>. The directory structure is preserved in the archive, and a recursive search is
        /// done for files to be archived. The archive must not exist. If the directory is empty, an empty archive will be created.
        /// If a file in the directory cannot be added to the archive, the archive will be left incomplete and invalid and the
        /// method will throw an exception. This method optionally includes the base directory in the archive.
        /// If an error is encountered while adding files to the archive, this method will stop adding files and leave the archive
        /// in an invalid state. The paths are permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file in the archive has data in the last write time
        /// field that is not a valid Zip timestamp, an indicator value of 1980 January 1 at midnight will be used for the file's
        /// last modified time.</p>
        ///
        /// <p>If an entry with the specified name already exists in the archive, a second entry will be created that has an identical name.</p>
        ///
        /// <p>Since no <code>CompressionLevel</code> is specified, the default provided by the implementation of the underlying compression
        /// algorithm will be used; the <code>ZipArchive</code> will not impose its own default.
        /// (Currently, the underlying compression algorithm is provided by the <code>System.IO.Compression.DeflateStream</code> class.)</p>
        /// </summary>
        ///
        /// <exception cref="ArgumentException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is a zero-length
        ///                                     string, contains only whitespace, or contains one or more invalid characters as defined by
        ///                                     <code>InvalidPathChars</code>.</exception>
        /// <exception cref="ArgumentNullException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code> is null.</exception>
        /// <exception cref="PathTooLongException">In <code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>, the
        ///                                        specified path, file name, or both exceed the system-defined maximum length.
        ///                                        For example, on Windows-based platforms, paths must be less than 248 characters,
        ///                                        and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified in <code>sourceDirectoryName</code> or
        ///                                              <code>destinationArchiveFileName</code> is invalid, (for example, it is on an unmapped drive).
        ///                                              -OR- The directory specified by <code>sourceDirectoryName</code> does not exist.</exception>
        /// <exception cref="IOException"><code>destinationArchiveFileName</code> already exists.
        ///                               -OR- An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="UnauthorizedAccessException"><code>destinationArchiveFileName</code> specified a directory.
        ///                                               -OR- The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException"><code>sourceDirectoryName</code> or <code>destinationArchiveFileName</code>
        ///                                         is in an invalid format.</exception>
        ///
        /// <param name="sourceDirectoryName">The path to the directory on the file system to be archived.</param>
        /// <param name="destinationArchiveFileName">The name of the archive to be created.</param>
        /// <param name="compressionLevel">The level of the compression (speed/memory vs. compressed size trade-off).</param>
        /// <param name="includeBaseDirectory"><code>true</code> to indicate that a directory named <code>sourceDirectoryName</code> should
        /// be included at the root of the archive. <code>false</code> to indicate that the files and directories in <code>sourceDirectoryName</code>
        /// should be included directly in the archive.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this ZipArchive.
        ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
        ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
        ///         UTF-8 encoding for entry names.<br />
        ///         This value is used as follows while creating the archive:</para>
        ///     <para>If <c>entryNameEncoding</c> is not specified (<c>== null</c>):</para>
        ///     <list>
        ///         <item>For file names that contain characters outside the ASCII range:<br />
        ///         The language encoding flag (EFS) will be set in the general purpose bit flag of the local file header of the corresponding entry,
        ///         and UTF-8 (<c>Encoding.UTF8</c>) will be used in order to encode the entry name into bytes.</item>
        ///         <item>For file names that do not contain characters outside the ASCII range:<br />
        ///         the language encoding flag (EFS) will not be set in the general purpose bit flag of the local file header of the corresponding entry,
        ///         and the current system default code page (<c>Encoding.Default</c>) will be used to encode the entry names into bytes.</item>
        ///     </list>
        ///     <para>If <c>entryNameEncoding</c> is specified (<c>!= null</c>):</para>
        ///     <list>
        ///         <item>The specified <c>entryNameEncoding</c> will always be used to encode the entry names into bytes.
        ///         The language encoding flag (EFS) in the general purpose bit flag of the local file header for each entry will be set if and only
        ///         if the specified <c>entryNameEncoding</c> is a UTF-8 encoding.</item>
        ///     </list>
        ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
        ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
        /// </param>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName,
                                               CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding) =>
            DoCreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory, entryNameEncoding);

        /// <summary>
        /// Creates a zip archive in the specified stream that contains the files and directories from the specified directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destination">The stream where the zip archive is to be stored.</param>
        /// <remarks>
        /// The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created.
        /// This method overload does not include the base directory in the archive and does not allow you to specify a compression level.
        /// If you want to include the base directory or specify a compression level, call the <see cref="CreateFromDirectory(string, Stream, CompressionLevel, bool)"/> method overload.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <see cref="IOException"/> exception.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="sourceDirectoryName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// The <paramref name="destination"/> stream does not support writing.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">In <paramref name="sourceDirectoryName" /> the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">A file in the specified directory could not be opened.
        ///-or-
        ///An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceDirectoryName" /> contains an invalid format.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination) =>
           DoCreateFromDirectory(sourceDirectoryName, destination, compressionLevel: null, includeBaseDirectory: false, entryNameEncoding: null);

        /// <summary>
        /// Creates a zip archive in the specified stream that contains the files and directories from the specified directory, uses the specified compression level, and optionally includes the base directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destination">The stream where the zip archive is to be stored.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression effectiveness when creating the entry.</param>
        /// <param name="includeBaseDirectory"><see langword="true" /> to include the directory name from <paramref name="sourceDirectoryName" /> at the root of the archive; <see langword="false" /> to include only the contents of the directory.</param>
        /// <remarks>
        /// The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created.
        /// Use this method overload to specify the compression level and whether to include the base directory in the archive.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <see cref="IOException"/> exception.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="sourceDirectoryName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// The <paramref name="destination"/> stream does not support writing.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">In <paramref name="sourceDirectoryName" /> the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">A file in the specified directory could not be opened.
        ///-or-
        ///An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination, CompressionLevel compressionLevel, bool includeBaseDirectory) =>
            DoCreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, entryNameEncoding: null);

        /// <summary>
        /// Creates a zip archive in the specified stream that contains the files and directories from the specified directory, uses the specified compression level and character encoding for entry names, and optionally includes the base directory.
        /// </summary>
        /// <param name="sourceDirectoryName">The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="destination">The stream where the zip archive is to be stored.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression effectiveness when creating the entry.</param>
        /// <param name="includeBaseDirectory"><see langword="true" /> to include the directory name from <paramref name="sourceDirectoryName" /> at the root of the archive; <see langword="false" /> to include only the contents of the directory.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <remarks>
        /// The directory structure from the file system is preserved in the archive. If the directory is empty, an empty archive is created.
        /// Use this method overload to specify the compression level and character encoding, and whether to include the base directory in the archive.
        /// If a file in the directory cannot be added to the archive, the archive is left incomplete and invalid, and the method throws an <see cref="IOException"/> exception.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="sourceDirectoryName" /> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// The <paramref name="destination"/> stream does not support writing.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceDirectoryName" /> or <paramref name="destination" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">In <paramref name="sourceDirectoryName" /> the specified path, file name, or both exceed the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="sourceDirectoryName" /> is invalid or does not exist (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">A file in the specified directory could not be opened.
        ///-or-
        ///An I/O error occurred while opening a file to be archived.</exception>
        /// <exception cref="NotSupportedException"><paramref name="sourceDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionLevel"/> is not a valid <see cref="CompressionLevel"/> value.</exception>
        public static void CreateFromDirectory(string sourceDirectoryName, Stream destination,
                                               CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding) =>
            DoCreateFromDirectory(sourceDirectoryName, destination, compressionLevel, includeBaseDirectory, entryNameEncoding);

        private static void DoCreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName,
                                                  CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)

        {
            // Rely on Path.GetFullPath for validation of sourceDirectoryName and destinationArchive

            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);
            destinationArchiveFileName = Path.GetFullPath(destinationArchiveFileName);

            using ZipArchive archive = Open(destinationArchiveFileName, ZipArchiveMode.Create, entryNameEncoding);
            CreateZipArchiveFromDirectory(sourceDirectoryName, archive, compressionLevel, includeBaseDirectory);
        }

        private static void DoCreateFromDirectory(string sourceDirectoryName, Stream destination,
                                                  CompressionLevel? compressionLevel, bool includeBaseDirectory, Encoding? entryNameEncoding)
        {
            ArgumentNullException.ThrowIfNull(destination);
            if (!destination.CanWrite)
            {
                throw new ArgumentException(SR.UnwritableStream, nameof(destination));
            }
            if (compressionLevel.HasValue && !Enum.IsDefined(compressionLevel.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(compressionLevel));
            }

            // Rely on Path.GetFullPath for validation of sourceDirectoryName

            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            using ZipArchive archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding);
            CreateZipArchiveFromDirectory(sourceDirectoryName, archive, compressionLevel, includeBaseDirectory);
        }

        private static void CreateZipArchiveFromDirectory(string sourceDirectoryName, ZipArchive archive,
                                                          CompressionLevel? compressionLevel, bool includeBaseDirectory)
        {
            bool directoryIsEmpty = true;

            //add files and directories
            DirectoryInfo di = new DirectoryInfo(sourceDirectoryName);

            string basePath = di.FullName;

            if (includeBaseDirectory && di.Parent != null)
                basePath = di.Parent.FullName;

            FileSystemEnumerable<(string, CreateEntryType)> fse = CreateEnumerableForCreate(di.FullName);

            foreach ((string fullPath, CreateEntryType type) in fse)
            {
                directoryIsEmpty = false;

                switch (type)
                {
                    case CreateEntryType.File:
                        {
                            // Create entry for file:
                            string entryName = ArchivingUtils.EntryFromPath(fullPath.AsSpan(basePath.Length));
                            ZipFileExtensions.DoCreateEntryFromFile(archive, fullPath, entryName, compressionLevel);
                        }
                        break;
                    case CreateEntryType.Directory:
                        if (ArchivingUtils.IsDirEmpty(fullPath))
                        {
                            // Create entry marking an empty dir:
                            // FullName never returns a directory separator character on the end,
                            // but Zip archives require it to specify an explicit directory:
                            string entryName = ArchivingUtils.EntryFromPath(fullPath.AsSpan(basePath.Length), appendPathSeparator: true);
                            archive.CreateEntry(entryName);
                        }
                        break;
                    case CreateEntryType.Unsupported:
                    default:
                        throw new IOException(SR.Format(SR.ZipUnsupportedFile, fullPath));
                }
            }

            // If no entries create an empty root directory entry:
            if (includeBaseDirectory && directoryIsEmpty)
                archive.CreateEntry(ArchivingUtils.EntryFromPath(di.Name, appendPathSeparator: true));
        }

        private enum CreateEntryType
        {
            File,
            Directory,
            Unsupported
        }
    }
}
