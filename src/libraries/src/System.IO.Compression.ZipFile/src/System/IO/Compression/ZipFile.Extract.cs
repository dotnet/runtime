// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <exception cref="IOException">The directory specified by destinationDirectoryName already exists.
        /// -or- An I/O error has occurred. -or- An archive entry?s name is zero-length, contains only whitespace, or contains one or
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
        /// <exception cref="IOException">The directory specified by destinationDirectoryName already exists.
        /// -or- An I/O error has occurred. -or- An archive entry?s name is zero-length, contains only whitespace, or contains one or
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
        /// <exception cref="IOException">The directory specified by destinationDirectoryName already exists.
        /// -or- An I/O error has occurred. -or- An archive entry?s name is zero-length, contains only whitespace, or contains one or
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
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding entryNameEncoding) => 
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
        /// <exception cref="IOException">The directory specified by destinationDirectoryName already exists.
        /// -or- An I/O error has occurred. -or- An archive entry?s name is zero-length, contains only whitespace, or contains one or
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
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding entryNameEncoding, bool overwriteFiles)
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
