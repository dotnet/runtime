// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: A collection of methods for manipulating Files.
**
**        April 09,2000 (some design refactorization)
**
===========================================================*/

using System.Security.Permissions;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    [ComVisible(true)]
    public static class File
    {
        private const int ERROR_INVALID_PARAMETER = 87;
        internal const int GENERIC_READ = unchecked((int)0x80000000);

        private const int GetFileExInfoStandard = 0;

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
            return new StreamWriter(path,false);
        }

        public static StreamWriter AppendText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();
            return new StreamWriter(path,true);
        }


        // Copies an existing file to a new file. An exception is raised if the
        // destination file already exists. Use the 
        // Copy(String, String, boolean) method to allow 
        // overwriting an existing file.
        public static void Copy(String sourceFileName, String destFileName) {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(destFileName));
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, false);
        }
    
        // Copies an existing file to a new file. If overwrite is 
        // false, then an IOException is thrown if the destination file 
        // already exists.  If overwrite is true, the file is 
        // overwritten.
        public static void Copy(String sourceFileName, String destFileName, bool overwrite) {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(destFileName));
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, overwrite);
        }

        /// <devdoc>
        ///    Note: This returns the fully qualified name of the destination file.
        /// </devdoc>
        internal static String InternalCopy(String sourceFileName, String destFileName, bool overwrite)
        {
            Contract.Requires(sourceFileName != null);
            Contract.Requires(destFileName != null);
            Contract.Requires(sourceFileName.Length > 0);
            Contract.Requires(destFileName.Length > 0);

            String fullSourceFileName = Path.GetFullPath(sourceFileName);
            String fullDestFileName = Path.GetFullPath(destFileName);

            bool r = Win32Native.CopyFile(fullSourceFileName, fullDestFileName, !overwrite);
            if (!r) {
                // Save Win32 error because subsequent checks will overwrite this HRESULT.
                int errorCode = Marshal.GetLastWin32Error();
                String fileName = destFileName;

                if (errorCode != Win32Native.ERROR_FILE_EXISTS) {
                    if (errorCode == Win32Native.ERROR_ACCESS_DENIED) {
                        if (Directory.InternalExists(fullDestFileName))
                            throw new IOException(Environment.GetResourceString("Arg_FileIsDirectory_Name", destFileName), Win32Native.ERROR_ACCESS_DENIED, fullDestFileName);
                    }
                }

                __Error.WinIOError(errorCode, fileName);
            }

            return fullDestFileName;
        }

        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite accessand cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        public static FileStream Create(String path) {
            return Create(path, FileStream.DefaultBufferSize);
        }
        
        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite access and cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        public static FileStream Create(String path, int bufferSize) {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        public static FileStream Create(String path, int bufferSize, FileOptions options) {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite,
                                  FileShare.None, bufferSize, options);
        }

        // Deletes a file. The file specified by the designated path is deleted.
        // If the file does not exist, Delete succeeds without throwing
        // an exception.
        // 
        // On NT, Delete will fail for a file that is open for normal I/O
        // or a file that is memory mapped.  
        public static void Delete(String path) {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            InternalDelete(path);
        }

        internal static void InternalDelete(String path)
        {
            String fullPath = Path.GetFullPath(path);
            bool r = Win32Native.DeleteFile(fullPath);
            if (!r) {
                int hr = Marshal.GetLastWin32Error();
                if (hr==Win32Native.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    __Error.WinIOError(hr, fullPath);
            }
        }

        // Tests if a file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  Note that if path describes a directory,
        // Exists will return true.
        public static bool Exists(String path)
        {
            return InternalExistsHelper(path);
        }

        private static bool InternalExistsHelper(String path)
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

        internal static bool InternalExists(String path) {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(path, ref data, false, true);

            return (dataInitialised == 0) && (data.fileAttributes != -1) 
                    && ((data.fileAttributes  & Win32Native.FILE_ATTRIBUTE_DIRECTORY) == 0);
        }

        public static FileStream Open(String path, FileMode mode) {
            return Open(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access) {
            return Open(path,mode, access, FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access, FileShare share) {
            return new FileStream(path, mode, access, share);
        }

        public static DateTime GetCreationTime(String path)
        {
            return InternalGetCreationTimeUtc(path).ToLocalTime();
        }

        public static DateTime GetCreationTimeUtc(String path)
        {
            return InternalGetCreationTimeUtc(path); // this API isn't exposed in Silverlight
        }

        private static DateTime InternalGetCreationTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)(data.ftCreationTimeHigh) << 32) | ((long)data.ftCreationTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static DateTime GetLastAccessTime(String path)
        {
            return InternalGetLastAccessTimeUtc(path).ToLocalTime();
        }

        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return InternalGetLastAccessTimeUtc(path);
        }

        private static DateTime InternalGetLastAccessTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)(data.ftLastAccessTimeHigh) << 32) | ((long)data.ftLastAccessTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static DateTime GetLastWriteTime(String path)
        {
            return InternalGetLastWriteTimeUtc(path).ToLocalTime();
        }

        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return InternalGetLastWriteTimeUtc(path);
        }

        private static DateTime InternalGetLastWriteTimeUtc(String path)
        {
            String fullPath = Path.GetFullPath(path);

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)data.ftLastWriteTimeHigh << 32) | ((long)data.ftLastWriteTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static FileAttributes GetAttributes(String path) 
        {
            String fullPath = Path.GetFullPath(path);

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, true);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            return (FileAttributes) data.fileAttributes;
        }

        public static void SetAttributes(String path, FileAttributes fileAttributes) 
        {
            String fullPath = Path.GetFullPath(path);
            bool r = Win32Native.SetFileAttributes(fullPath, (int) fileAttributes);
            if (!r) {
                int hr = Marshal.GetLastWin32Error();
                if (hr==ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
                 __Error.WinIOError(hr, fullPath);
            }
        }

        public static FileStream OpenRead(String path) {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static FileStream OpenWrite(String path) {
            return new FileStream(path, FileMode.OpenOrCreate, 
                                  FileAccess.Write, FileShare.None);
        }

        public static String ReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllText(path, Encoding.UTF8);
        }

        public static String ReadAllText(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllText(path, encoding);
        }

        private static String InternalReadAllText(String path, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamReader sr = new StreamReader(path, encoding, true, StreamReader.DefaultBufferSize))
                return sr.ReadToEnd();
        }

        public static void WriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllText(path, contents, StreamWriter.UTF8NoBOM);
        }

        public static void WriteAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllText(path, contents, encoding);
        }

        private static void InternalWriteAllText(String path, String contents, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamWriter sw = new StreamWriter(path, false, encoding, StreamWriter.DefaultBufferSize))
                sw.Write(contents);
        } 

        public static byte[] ReadAllBytes(String path)
        {
            byte[] bytes;
            using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 
                FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false)) {
                // Do a blocking read
                int index = 0;
                long fileLength = fs.Length;
                if (fileLength > Int32.MaxValue)
                    throw new IOException(Environment.GetResourceString("IO.IO_FileTooLong2GB"));
                int count = (int) fileLength;
                bytes = new byte[count];
                while(count > 0) {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        __Error.EndOfFile();
                    index += n;
                    count -= n;
                }
            }
            return bytes;
        }

        public static void WriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            Contract.EndContractBlock();

            InternalWriteAllBytes(path, bytes);
        }

        private static void InternalWriteAllBytes(String path, byte[] bytes)
        {
            Contract.Requires(path != null);
            Contract.Requires(path.Length != 0);
            Contract.Requires(bytes != null);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read,
                    FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static String[] ReadAllLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllLines(path, encoding);
        }

        private static String[] InternalReadAllLines(String path, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length != 0);

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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), nameof(path));
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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), nameof(path));
            Contract.EndContractBlock();

            return ReadLinesIterator.CreateIterator(path, encoding);
        }

        public static void WriteAllLines(String path, String[] contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
        }

        public static void WriteAllLines(String path, String[] contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        public static void WriteAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        private static void InternalWriteAllLines(TextWriter writer, IEnumerable<String> contents)
        {
            Contract.Requires(writer != null);
            Contract.Requires(contents != null);

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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalAppendAllText(path, contents, StreamWriter.UTF8NoBOM);
        }

        public static void AppendAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalAppendAllText(path, contents, encoding);
        }

        private static void InternalAppendAllText(String path, String contents, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamWriter sw = new StreamWriter(path, true, encoding))
                sw.Write(contents);
        }

        public static void AppendAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, true, StreamWriter.UTF8NoBOM), contents);
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
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, true, encoding), contents);
        }

        // Moves a specified file to a new location and potentially a new file name.
        // This method does work across volumes.
        public static void Move(String sourceFileName, String destFileName) {
            InternalMove(sourceFileName, destFileName);
        }

        private static void InternalMove(String sourceFileName, String destFileName) {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(sourceFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), nameof(destFileName));
            Contract.EndContractBlock();
            
            String fullSourceFileName = Path.GetFullPath(sourceFileName);
            String fullDestFileName = Path.GetFullPath(destFileName);

            if (!InternalExists(fullSourceFileName))
                __Error.WinIOError(Win32Native.ERROR_FILE_NOT_FOUND, fullSourceFileName);
            
            if (!Win32Native.MoveFile(fullSourceFileName, fullDestFileName))
            {
                __Error.WinIOError();
            }
        }


        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));
            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));
            Contract.EndContractBlock();

            InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, false);
        }

        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));
            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));
            Contract.EndContractBlock();

            InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        }

        private static void InternalReplace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            Contract.Requires(sourceFileName != null);
            Contract.Requires(destinationFileName != null);

            String fullSrcPath = Path.GetFullPath(sourceFileName);
            String fullDestPath = Path.GetFullPath(destinationFileName);
            String fullBackupPath = null;
            if (destinationBackupFileName != null)
                fullBackupPath = Path.GetFullPath(destinationBackupFileName);

            int flags = Win32Native.REPLACEFILE_WRITE_THROUGH;
            if (ignoreMetadataErrors)
                flags |= Win32Native.REPLACEFILE_IGNORE_MERGE_ERRORS;

            bool r = Win32Native.ReplaceFile(fullDestPath, fullSrcPath, fullBackupPath, flags, IntPtr.Zero, IntPtr.Zero);
            if (!r)
                __Error.WinIOError();
        }


        // Returns 0 on success, otherwise a Win32 error code.  Note that
        // classes should use -1 as the uninitialized state for dataInitialized.
        internal static int FillAttributeInfo(String path, ref Win32Native.WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain, bool returnErrorOnNotFound)
        {
            int dataInitialised = 0;
            if (tryagain) // someone has a handle to the file open, or other error
            {
                Win32Native.WIN32_FIND_DATA findData;
                findData =  new Win32Native.WIN32_FIND_DATA (); 
                
                // Remove trialing slash since this can cause grief to FindFirstFile. You will get an invalid argument error
                String tempPath = path.TrimEnd(new char [] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar});

                // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool error = false;
                    SafeFindHandle handle = Win32Native.FindFirstFile(tempPath,findData);
                    try {
                        if (handle.IsInvalid) {
                            error = true;
                            dataInitialised = Marshal.GetLastWin32Error();
                            
                            if (dataInitialised == Win32Native.ERROR_FILE_NOT_FOUND ||
                                dataInitialised == Win32Native.ERROR_PATH_NOT_FOUND ||
                                dataInitialised == Win32Native.ERROR_NOT_READY)  // floppy device not ready
                            {
                                if (!returnErrorOnNotFound) {
                                    // Return default value for backward compatibility
                                    dataInitialised = 0;
                                    data.fileAttributes = -1;
                                }
                            }
                            return dataInitialised;
                        }
                    }
                    finally {
                        // Close the Win32 handle
                        try {
                            handle.Close();
                        }
                        catch {
                            // if we're already returning an error, don't throw another one. 
                            if (!error) {
                                Debug.Assert(false, "File::FillAttributeInfo - FindClose failed!");
                                __Error.WinIOError();
                            }
                        }
                    }
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }

                // Copy the information to data
                data.PopulateFrom(findData);
            }
            else
            {   
                 // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                bool success = false;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    success = Win32Native.GetFileAttributesEx(path, GetFileExInfoStandard, ref data);
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }

                if (!success) {
                    dataInitialised = Marshal.GetLastWin32Error();
                    if (dataInitialised != Win32Native.ERROR_FILE_NOT_FOUND &&
                        dataInitialised != Win32Native.ERROR_PATH_NOT_FOUND &&
                        dataInitialised != Win32Native.ERROR_NOT_READY)  // floppy device not ready
                    {
                     // In case someone latched onto the file. Take the perf hit only for failure
                        return FillAttributeInfo(path, ref data, true, returnErrorOnNotFound);
                    }
                    else {
                        if (!returnErrorOnNotFound) {
                            // Return default value for backward compbatibility
                            dataInitialised = 0;
                            data.fileAttributes = -1;
                        }
                    }
                }
            }

            return dataInitialised;
        }
    }
}
