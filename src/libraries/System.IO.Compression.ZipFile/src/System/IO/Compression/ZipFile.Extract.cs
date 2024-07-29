// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.Compression
{
    public static partial class ZipFile
    {
        /// <summary>
        /// Extracts all of the files in the specified archive to a directory on the file system.
        /// The specified directory must not exist. This method will create all subdirectories and the specified directory.
        /// If there is an error while extracting the archive, the archive will remain partially extracted. Each entry will
        /// be extracted such that the extracted file has the same relative path to the destinationDirectoryName as the entry
        /// has to the archive. The path is permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file to be archived has an invalid last modified
        /// time, the first datetime representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">sourceArchive or destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">sourceArchive or destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">sourceArchive or destinationDirectoryName specifies a path, file name,
        /// or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified by sourceArchive or destinationDirectoryName is invalid,
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred. -or- An archive entry's name is zero-length, contains only whitespace, or contains one or
        /// more invalid characters as defined by InvalidPathChars. -or- Extracting an archive entry would result in a file destination that is outside the destination directory (for example, because of parent directory accessors). -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">sourceArchive or destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="FileNotFoundException">sourceArchive was not found.</exception>
        /// <exception cref="InvalidDataException">The archive specified by sourceArchive: Is not a valid ZipArchive
        /// -or- An archive entry was not found or was corrupt. -or- An archive entry has been compressed using a compression method
        /// that is not supported.</exception>
        ///
        /// <param name="sourceArchiveFileName">The path to the archive on the file system that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: false);

        /// <summary>
        /// Extracts all of the files in the specified archive to a directory on the file system.
        /// The specified directory must not exist. This method will create all subdirectories and the specified directory.
        /// If there is an error while extracting the archive, the archive will remain partially extracted. Each entry will
        /// be extracted such that the extracted file has the same relative path to the destinationDirectoryName as the entry
        /// has to the archive. The path is permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file to be archived has an invalid last modified
        /// time, the first datetime representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">sourceArchive or destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">sourceArchive or destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">sourceArchive or destinationDirectoryName specifies a path, file name,
        /// or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified by sourceArchive or destinationDirectoryName is invalid,
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred. -or- An archive entry's name is zero-length, contains only whitespace, or contains one or
        /// more invalid characters as defined by InvalidPathChars. -or- Extracting an archive entry would result in a file destination that is outside the destination directory (for example, because of parent directory accessors). -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">sourceArchive or destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="FileNotFoundException">sourceArchive was not found.</exception>
        /// <exception cref="InvalidDataException">The archive specified by sourceArchive: Is not a valid ZipArchive
        /// -or- An archive entry was not found or was corrupt. -or- An archive entry has been compressed using a compression method
        /// that is not supported.</exception>
        ///
        /// <param name="sourceArchiveFileName">The path to the archive on the file system that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="overwriteFiles">True to indicate overwrite.</param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: overwriteFiles);

        /// <summary>
        /// Extracts all of the files in the specified archive to a directory on the file system.
        /// The specified directory must not exist. This method will create all subdirectories and the specified directory.
        /// If there is an error while extracting the archive, the archive will remain partially extracted. Each entry will
        /// be extracted such that the extracted file has the same relative path to the destinationDirectoryName as the entry
        /// has to the archive. The path is permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file to be archived has an invalid last modified
        /// time, the first datetime representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">sourceArchive or destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">sourceArchive or destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">sourceArchive or destinationDirectoryName specifies a path, file name,
        /// or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified by sourceArchive or destinationDirectoryName is invalid,
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred. -or- An archive entry's name is zero-length, contains only whitespace, or contains one or
        /// more invalid characters as defined by InvalidPathChars. -or- Extracting an archive entry would result in a file destination that is outside the destination directory (for example, because of parent directory accessors). -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">sourceArchive or destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="FileNotFoundException">sourceArchive was not found.</exception>
        /// <exception cref="InvalidDataException">The archive specified by sourceArchive: Is not a valid ZipArchive
        /// -or- An archive entry was not found or was corrupt. -or- An archive entry has been compressed using a compression method
        /// that is not supported.</exception>
        ///
        /// <param name="sourceArchiveFileName">The path to the archive on the file system that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory on the file system. The directory specified must not exist, but the directory that it is contained in must exist.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this ZipArchive.
        ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
        ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
        ///         UTF-8 encoding for entry names.<br />
        ///         This value is used as follows:</para>
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
        ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
        ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
        /// </param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding) =>
            ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding: entryNameEncoding, overwriteFiles: false);

        /// <summary>
        /// Extracts all of the files in the specified archive to a directory on the file system.
        /// The specified directory must not exist. This method will create all subdirectories and the specified directory.
        /// If there is an error while extracting the archive, the archive will remain partially extracted. Each entry will
        /// be extracted such that the extracted file has the same relative path to the destinationDirectoryName as the entry
        /// has to the archive. The path is permitted to specify relative or absolute path information. Relative path information
        /// is interpreted as relative to the current working directory. If a file to be archived has an invalid last modified
        /// time, the first datetime representable in the Zip timestamp format (midnight on January 1, 1980) will be used.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">sourceArchive or destinationDirectoryName is a zero-length string, contains only whitespace,
        /// or contains one or more invalid characters as defined by InvalidPathChars.</exception>
        /// <exception cref="ArgumentNullException">sourceArchive or destinationDirectoryName is null.</exception>
        /// <exception cref="PathTooLongException">sourceArchive or destinationDirectoryName specifies a path, file name,
        /// or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters,
        /// and file names must be less than 260 characters.</exception>
        /// <exception cref="DirectoryNotFoundException">The path specified by sourceArchive or destinationDirectoryName is invalid,
        /// (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">An I/O error has occurred. -or- An archive entry's name is zero-length, contains only whitespace, or contains one or
        /// more invalid characters as defined by InvalidPathChars. -or- Extracting an archive entry would result in a file destination that is outside the destination directory (for example, because of parent directory accessors). -or- An archive entry has the same name as an already extracted entry from the same archive.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="NotSupportedException">sourceArchive or destinationDirectoryName is in an invalid format. </exception>
        /// <exception cref="FileNotFoundException">sourceArchive was not found.</exception>
        /// <exception cref="InvalidDataException">The archive specified by sourceArchive: Is not a valid ZipArchive
        /// -or- An archive entry was not found or was corrupt. -or- An archive entry has been compressed using a compression method
        /// that is not supported.</exception>
        ///
        /// <param name="sourceArchiveFileName">The path to the archive on the file system that is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="overwriteFiles">True to indicate overwrite.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this ZipArchive.
        ///         ///     <para>NOTE: Specifying this parameter to values other than <c>null</c> is discouraged.
        ///         However, this may be necessary for interoperability with ZIP archive tools and libraries that do not correctly support
        ///         UTF-8 encoding for entry names.<br />
        ///         This value is used as follows:</para>
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
        ///     <para>Note that Unicode encodings other than UTF-8 may not be currently used for the <c>entryNameEncoding</c>,
        ///     otherwise an <see cref="ArgumentException"/> is thrown.</para>
        /// </param>
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
        {
            ArgumentNullException.ThrowIfNull(sourceArchiveFileName);

            using (ZipArchive archive = Open(sourceArchiveFileName, ZipArchiveMode.Read, entryNameEncoding))
            {
                archive.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
            }
        }

        /// <summary>
        /// Extracts all the files from the zip archive stored in the specified stream and places them in the specified destination directory on the file system.
        /// </summary>
        /// <param name="source">The stream from which the zip archive is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <remarks> This method creates the specified directory and all subdirectories. The destination directory cannot already exist.
        /// Exceptions related to validating the paths in the <paramref name="destinationDirectoryName"/> or the files in the zip archive contained in <paramref name="source"/> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted.
        /// Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName"/> as its source entry has to the root of the archive.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// <exception cref="ArgumentException"><paramref name="destinationDirectoryName" />> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// An archive entry to extract has the same name as an entry that has already been extracted or that exists in <paramref name="destinationDirectoryName" />.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destinationDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="InvalidDataException">The archive contained in the <paramref name="source" /> stream is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName) =>
            ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: false);

        /// <summary>
        /// Extracts all the files from the zip archive stored in the specified stream and places them in the specified destination directory on the file system, and optionally allows choosing if the files in the destination directory should be overwritten.
        /// </summary>
        /// <param name="source">The stream from which the zip archive is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="overwriteFiles"><see langword="true" /> to overwrite files; <see langword="false" /> otherwise.</param>
        /// <remarks> This method creates the specified directory and all subdirectories. The destination directory cannot already exist.
        /// Exceptions related to validating the paths in the <paramref name="destinationDirectoryName"/> or the files in the zip archive contained in <paramref name="source"/> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted.
        /// Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName"/> as its source entry has to the root of the archive.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// <exception cref="ArgumentException"><paramref name="destinationDirectoryName" />> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// <paramref name="overwriteFiles" /> is <see langword="false" /> and an archive entry to extract has the same name as an entry that has already been extracted or that exists in <paramref name="destinationDirectoryName" />.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destinationDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="InvalidDataException">The archive contained in the <paramref name="source" /> stream is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles) =>
            ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding: null, overwriteFiles: overwriteFiles);

        /// <summary>
        /// Extracts all the files from the zip archive stored in the specified stream and places them in the specified destination directory on the file system and uses the specified character encoding for entry names.
        /// </summary>
        /// <param name="source">The stream from which the zip archive is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <remarks> This method creates the specified directory and all subdirectories. The destination directory cannot already exist.
        /// Exceptions related to validating the paths in the <paramref name="destinationDirectoryName"/> or the files in the zip archive contained in <paramref name="source"/> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted.
        /// Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName"/> as its source entry has to the root of the archive.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// If <paramref name="entryNameEncoding"/> is set to a value other than <see langword="null"/>, entry names are decoded according to the following rules:
        /// - For entry names where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, the entry names are decoded by using the specified encoding.
        /// - For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.
        /// If <paramref name="entryNameEncoding"/> is set to <see langword="null"/>, entry names are decoded according to the following rules:
        /// - For entries where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, entry names are decoded by using the current system default code page.
        /// - For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.
        /// <exception cref="ArgumentException"><paramref name="destinationDirectoryName" />> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryNameEncoding"/> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// An archive entry to extract has the same name as an entry that has already been extracted or that exists in <paramref name="destinationDirectoryName" />.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destinationDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="InvalidDataException">The archive contained in the <paramref name="source" /> stream is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, Encoding? entryNameEncoding) =>
            ExtractToDirectory(source, destinationDirectoryName, entryNameEncoding: entryNameEncoding, overwriteFiles: false);

        /// <summary>
        /// Extracts all the files from the zip archive stored in the specified stream and places them in the specified destination directory on the file system, uses the specified character encoding for entry names, and optionally allows choosing if the files in the destination directory should be overwritten.
        /// </summary>
        /// <param name="source">The stream from which the zip archive is to be extracted.</param>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
        /// <param name="entryNameEncoding">The encoding to use when reading or writing entry names in this archive. Specify a value for this parameter only when an encoding is required for interoperability with zip archive tools and libraries that do not support UTF-8 encoding for entry names.</param>
        /// <param name="overwriteFiles"><see langword="true" /> to overwrite files; <see langword="false" /> otherwise.</param>
        /// <remarks> This method creates the specified directory and all subdirectories. The destination directory cannot already exist.
        /// Exceptions related to validating the paths in the <paramref name="destinationDirectoryName"/> or the files in the zip archive contained in <paramref name="source"/> parameters are thrown before extraction. Otherwise, if an error occurs during extraction, the archive remains partially extracted.
        /// Each extracted file has the same relative path to the directory specified by <paramref name="destinationDirectoryName"/> as its source entry has to the root of the archive.
        /// If a file to be archived has an invalid last modified time, the first date and time representable in the Zip timestamp format (midnight on January 1, 1980) will be used.</remarks>
        /// If <paramref name="entryNameEncoding"/> is set to a value other than <see langword="null"/>, entry names are decoded according to the following rules:
        /// - For entry names where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, the entry names are decoded by using the specified encoding.
        /// - For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.
        /// If <paramref name="entryNameEncoding"/> is set to <see langword="null"/>, entry names are decoded according to the following rules:
        /// - For entries where the language encoding flag (in the general-purpose bit flag of the local file header) is not set, entry names are decoded by using the current system default code page.
        /// - For entries where the language encoding flag is set, the entry names are decoded by using UTF-8.
        /// <exception cref="ArgumentException"><paramref name="destinationDirectoryName" />> is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// <paramref name="entryNameEncoding"/> is set to a Unicode encoding other than UTF-8.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationDirectoryName" /> or <paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="PathTooLongException">The specified path in <paramref name="destinationDirectoryName" /> exceeds the system-defined maximum length.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IOException">The name of an entry in the archive is <see cref="string.Empty" />, contains only white space, or contains at least one invalid character.
        /// -or-
        /// Extracting an archive entry would create a file that is outside the directory specified by <paramref name="destinationDirectoryName" />. (For example, this might happen if the entry name contains parent directory accessors.)
        /// -or-
        /// <paramref name="overwriteFiles" /> is <see langword="false" /> and an archive entry to extract has the same name as an entry that has already been extracted or that exists in <paramref name="destinationDirectoryName" />.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to access the archive or the destination directory.</exception>
        /// <exception cref="NotSupportedException"><paramref name="destinationDirectoryName" /> contains an invalid format.</exception>
        /// <exception cref="InvalidDataException">The archive contained in the <paramref name="source" /> stream is not a valid zip archive.
        /// -or-
        /// An archive entry was not found or was corrupt.
        /// -or-
        /// An archive entry was compressed by using a compression method that is not supported.</exception>
        public static void ExtractToDirectory(Stream source, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!source.CanRead)
            {
                throw new ArgumentException(SR.UnreadableStream, nameof(source));
            }

            using ZipArchive archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding);
            archive.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
        }
    }
}
