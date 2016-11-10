// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    public static class File
    {
        internal const int DefaultBufferSize = 4096;

        public static StreamReader OpenText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            return new StreamReader(path);
        }

        public static StreamWriter CreateText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            return new StreamWriter(path, append: false);
        }

        public static StreamWriter AppendText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            return new StreamWriter(path, append: true);
        }


        // Copies an existing file to a new file. An exception is raised if the
        // destination file already exists. Use the 
        // Copy(String, String, boolean) method to allow 
        // overwriting an existing file.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName and Create
        // and Write permissions to destFileName.
        // 
        public static void Copy(String sourceFileName, String destFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), SR.ArgumentNull_FileName);
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (sourceFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, false);
        }

        // Copies an existing file to a new file. If overwrite is 
        // false, then an IOException is thrown if the destination file 
        // already exists.  If overwrite is true, the file is 
        // overwritten.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName 
        // and Write permissions to destFileName.
        // 
        public static void Copy(String sourceFileName, String destFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), SR.ArgumentNull_FileName);
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (sourceFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, overwrite);
        }

        /// <devdoc>
        ///    Note: This returns the fully qualified name of the destination file.
        /// </devdoc>
        [System.Security.SecuritySafeCritical]
        internal static String InternalCopy(String sourceFileName, String destFileName, bool overwrite)
        {
            Debug.Assert(sourceFileName != null);
            Debug.Assert(destFileName != null);
            Debug.Assert(sourceFileName.Length > 0);
            Debug.Assert(destFileName.Length > 0);

            String fullSourceFileName = Path.GetFullPath(sourceFileName);
            String fullDestFileName = Path.GetFullPath(destFileName);

            FileSystem.Current.CopyFile(fullSourceFileName, fullDestFileName, overwrite);

            return fullDestFileName;
        }


        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite access and cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        //
        // Your application must have Create, Read, and Write permissions to
        // the file.
        // 
        public static FileStream Create(string path)
        {
            return Create(path, DefaultBufferSize);
        }

        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite access and cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        //
        // Your application must have Create, Read, and Write permissions to
        // the file.
        // 
        public static FileStream Create(String path, int bufferSize)
        {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        public static FileStream Create(String path, int bufferSize, FileOptions options)
        {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite,
                                  FileShare.None, bufferSize, options);
        }
 
        // Deletes a file. The file specified by the designated path is deleted.
        // If the file does not exist, Delete succeeds without throwing
        // an exception.
        // 
        // On NT, Delete will fail for a file that is open for normal I/O
        // or a file that is memory mapped.  
        // 
        // Your application must have Delete permission to the target file.
        // 
        [System.Security.SecuritySafeCritical]
        public static void Delete(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPath(path);

            FileSystem.Current.DeleteFile(fullPath);
        }


        // Tests if a file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  Note that if path describes a directory,
        // Exists will return true.
        //
        // Your application must have Read permission for the target directory.
        // 
        [System.Security.SecuritySafeCritical]
        public static bool Exists(String path)
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                path = Path.GetFullPath(path);
                // After normalizing, check whether path ends in directory separator.
                // Otherwise, FillAttributeInfo removes it and we may return a false positive.
                // GetFullPath should never return null
                Debug.Assert(path != null, "File.Exists: GetFullPath returned null");
                if (path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]))
                {
                    return false;
                }

                return InternalExists(path);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { } // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool InternalExists(String path)
        {
            return FileSystem.Current.FileExists(path);
        }

        public static FileStream Open(String path, FileMode mode)
        {
            return Open(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access)
        {
            return Open(path, mode, access, FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(path, mode, access, share);
        }

        internal static DateTimeOffset GetUtcDateTimeOffset(DateTime dateTime)
        {
            // File and Directory UTC APIs treat a DateTimeKind.Unspecified as UTC whereas 
            // ToUniversalTime treats this as local.
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            return dateTime.ToUniversalTime();
        }

        public static void SetCreationTime(String path, DateTime creationTime)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetCreationTime(fullPath, creationTime, asDirectory: false);
        }

        public static void SetCreationTimeUtc(String path, DateTime creationTimeUtc)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetCreationTime(fullPath, GetUtcDateTimeOffset(creationTimeUtc), asDirectory: false);
        }

        [System.Security.SecuritySafeCritical]
        public static DateTime GetCreationTime(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetCreationTime(fullPath).LocalDateTime;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static DateTime GetCreationTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetCreationTime(fullPath).UtcDateTime;
        }

        public static void SetLastAccessTime(String path, DateTime lastAccessTime)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetLastAccessTime(fullPath, lastAccessTime, asDirectory: false);
        }

        public static void SetLastAccessTimeUtc(String path, DateTime lastAccessTimeUtc)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetLastAccessTime(fullPath, GetUtcDateTimeOffset(lastAccessTimeUtc), asDirectory: false);
        }

        [System.Security.SecuritySafeCritical]
        public static DateTime GetLastAccessTime(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetLastAccessTime(fullPath).LocalDateTime;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static DateTime GetLastAccessTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetLastAccessTime(fullPath).UtcDateTime;
        }

        public static void SetLastWriteTime(String path, DateTime lastWriteTime)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetLastWriteTime(fullPath, lastWriteTime, asDirectory: false);
        }

        public static void SetLastWriteTimeUtc(String path, DateTime lastWriteTimeUtc)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetLastWriteTime(fullPath, GetUtcDateTimeOffset(lastWriteTimeUtc), asDirectory: false);
        }

        [System.Security.SecuritySafeCritical]
        public static DateTime GetLastWriteTime(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetLastWriteTime(fullPath).LocalDateTime;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static DateTime GetLastWriteTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetLastWriteTime(fullPath).UtcDateTime;
        }

        [System.Security.SecuritySafeCritical]
        public static FileAttributes GetAttributes(String path)
        {
            String fullPath = Path.GetFullPath(path);
            return FileSystem.Current.GetAttributes(fullPath);
        }

        [System.Security.SecurityCritical]
        public static void SetAttributes(String path, FileAttributes fileAttributes)
        {
            String fullPath = Path.GetFullPath(path);
            FileSystem.Current.SetAttributes(fullPath, fileAttributes);
        }

        [System.Security.SecuritySafeCritical]
        public static FileStream OpenRead(String path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }


        public static FileStream OpenWrite(String path)
        {
            return new FileStream(path, FileMode.OpenOrCreate,
                                  FileAccess.Write, FileShare.None);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String ReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return InternalReadAllText(path, Encoding.UTF8);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String ReadAllText(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return InternalReadAllText(path, encoding);
        }

        [System.Security.SecurityCritical]
        private static String InternalReadAllText(String path, Encoding encoding)
        {
            Debug.Assert(path != null);
            Debug.Assert(encoding != null);
            Debug.Assert(path.Length > 0);

            using (StreamReader sr = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true))
                return sr.ReadToEnd();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void WriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(contents);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void WriteAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            using (StreamWriter sw = new StreamWriter(path, false, encoding))
            {
                sw.Write(contents);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static byte[] ReadAllBytes(String path)
        {
            return InternalReadAllBytes(path);
        }

        [System.Security.SecurityCritical]
        private static byte[] InternalReadAllBytes(String path)
        {
            // bufferSize == 1 used to avoid unnecessary buffer in FileStream
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
            {
                long fileLength = fs.Length;
                if (fileLength > Int32.MaxValue)
                    throw new IOException(SR.IO_FileTooLong2GB);

                int index = 0;
                int count = (int)fileLength;
                byte[] bytes = new byte[count];
                while (count > 0)
                {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        throw Error.GetEndOfFile();
                    index += n;
                    count -= n;
                }
                return bytes;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void WriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), SR.ArgumentNull_Path);
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            Contract.EndContractBlock();

            InternalWriteAllBytes(path, bytes);
        }

        [System.Security.SecurityCritical]
        private static void InternalWriteAllBytes(String path, byte[] bytes)
        {
            Debug.Assert(path != null);
            Debug.Assert(path.Length != 0);
            Debug.Assert(bytes != null);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }
        public static String[] ReadAllLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return InternalReadAllLines(path, Encoding.UTF8);
        }

        public static String[] ReadAllLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return InternalReadAllLines(path, encoding);
        }

        private static String[] InternalReadAllLines(String path, Encoding encoding)
        {
            Debug.Assert(path != null);
            Debug.Assert(encoding != null);
            Debug.Assert(path.Length != 0);

            String line;
            List<String> lines = new List<String>();

            using (StreamReader sr = new StreamReader(path, encoding))
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);

            return lines.ToArray();
        }

        public static IEnumerable<String> ReadLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return ReadLinesIterator.CreateIterator(path, Encoding.UTF8);
        }

        public static IEnumerable<String> ReadLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            return ReadLinesIterator.CreateIterator(path, encoding);
        }

        public static void WriteAllLines(String path, String[] contents)
        {
            WriteAllLines(path, (IEnumerable<String>)contents);
        }

        public static void WriteAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path), contents);
        }

        public static void WriteAllLines(String path, String[] contents, Encoding encoding)
        {
            WriteAllLines(path, (IEnumerable<String>)contents, encoding);
        }

        public static void WriteAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        private static void InternalWriteAllLines(TextWriter writer, IEnumerable<String> contents)
        {
            Debug.Assert(writer != null);
            Debug.Assert(contents != null);

            using (writer)
            {
                foreach (String line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static void AppendAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            using (StreamWriter sw = new StreamWriter(path, append: true))
            {
                sw.Write(contents);
            }
        }

        public static void AppendAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            using (StreamWriter sw = new StreamWriter(path, true, encoding))
            {
                sw.Write(contents);
            }
        }

        public static void AppendAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, append: true), contents);
        }

        public static void AppendAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, true, encoding), contents);
        }

        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName)
        {
            Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);
        }

        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));
            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            FileSystem.Current.ReplaceFile(
                Path.GetFullPath(sourceFileName), 
                Path.GetFullPath(destinationFileName),
                destinationBackupFileName != null ? Path.GetFullPath(destinationBackupFileName) : null,
                ignoreMetadataErrors);
        }

        // Moves a specified file to a new location and potentially a new file name.
        // This method does work across volumes.
        //
        // The caller must have certain FileIOPermissions.  The caller must
        // have Read and Write permission to 
        // sourceFileName and Write 
        // permissions to destFileName.
        // 
        [System.Security.SecuritySafeCritical]
        public static void Move(String sourceFileName, String destFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), SR.ArgumentNull_FileName);
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (sourceFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            String fullSourceFileName = Path.GetFullPath(sourceFileName);
            String fullDestFileName = Path.GetFullPath(destFileName);

            if (!InternalExists(fullSourceFileName))
            {
                throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, fullSourceFileName), fullSourceFileName);
            }

            FileSystem.Current.MoveFile(fullSourceFileName, fullDestFileName);
        }

        public static void Encrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // TODO: Not supported on Unix or in WinRt, and the EncryptFile API isn't currently
            // available in OneCore.  For now, we just throw PNSE everywhere.  When the API is
            // available, we can put this into the FileSystem abstraction and implement it
            // properly for Win32.

            throw new PlatformNotSupportedException();
        }

        public static void Decrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // TODO: Not supported on Unix or in WinRt, and the EncryptFile API isn't currently
            // available in OneCore.  For now, we just throw PNSE everywhere.  When the API is
            // available, we can put this into the FileSystem abstraction and implement it
            // properly for Win32.

            throw new PlatformNotSupportedException();
        }
    }
}
