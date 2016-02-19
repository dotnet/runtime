// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.IO.IsolatedStorage
{
    public sealed class IsolatedStorageFile : IDisposable
    {
        private string _appFilesPath;

        private bool _disposed;
        private bool _closed;

        private readonly object _internalLock = new object();
        private static string s_RootFromHost;
        private static string s_IsolatedStorageRoot;

        /*
         * Constructors
         */
        internal IsolatedStorageFile() { }

        internal bool Disposed
        {
            get
            {
                return _disposed;
            }
        }

        internal static string IsolatedStorageRoot
        {
            get
            {
                if (s_IsolatedStorageRoot == null)
                {
                    // No need to lock here, FetchOrCreateRoot is idempotent.
                    s_IsolatedStorageRoot = FetchOrCreateRoot();
                }

                return s_IsolatedStorageRoot;
            }

            private set
            {
                s_IsolatedStorageRoot = value;
            }
        }

        internal bool IsDeleted
        {
            get
            {
                try
                {
                    return !Directory.Exists(IsolatedStorageRoot);
                }
                catch (IOException)
                {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                }
            }
        }

        internal void Close()
        {
            lock (_internalLock)
            {
                if (!_closed)
                {
                    _closed = true;
                    GC.SuppressFinalize(this);
                }
            }
        }

        public void DeleteFile(String file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                String fullPath = GetFullPath(file);
                File.Delete(fullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_DeleteFile, e);
            }
        }

        public bool FileExists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            EnsureStoreIsValid();

            return File.Exists(GetFullPath(path));
        }

        public bool DirectoryExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            return Directory.Exists(GetFullPath(path));
        }

        public void CreateDirectory(String dir)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String isPath = GetFullPath(dir); // Prepend IS root

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessable and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (Directory.Exists(isPath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(isPath);
            }
            catch (Exception e)
            {
                // We have a slightly different behavior here in comparison to the traditional IsolatedStorage
                // which tries to remove any partial directories created in case of failure.
                // However providing that behavior required we could not reply on FileSystem APIs in general
                // and had to keep tabs on what all directories needed to be created and at what point we failed
                // and back-track from there. It is unclear how many apps would depend on this behavior and if required
                // we could add the behavior as a bug-fix later.
                throw GetIsolatedStorageException(SR.IsolatedStorage_CreateDirectory, e);
            }
        }

        public void DeleteDirectory(String dir)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                string fullPath = GetFullPath(dir);
                Directory.Delete(fullPath, false);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_DeleteDirectory, e);
            }
        }

        public String[] GetFileNames()
        {
            return GetFileNames("*");
        }

        /*
         * foo\abc*.txt will give all abc*.txt files in foo directory
         */
        public String[] GetFileNames(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                // FileSystem APIs return the complete path of the matching files however Iso store only provided the FileName
                // and hid the IsoStore root. Hence we find all the matching files from the fileSystem and simply return the fileNames.
                return Directory.EnumerateFiles(_appFilesPath, searchPattern).Select(f => Path.GetFileName(f)).ToArray();
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public String[] GetDirectoryNames()
        {
            return GetDirectoryNames("*");
        }

        /*
         * foo\data* will give all directory names in foo directory that
         * starts with data
         */
        public String[] GetDirectoryNames(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));
            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                // FileSystem APIs return the complete path of the matching directories however Iso store only provided the directory name
                // and hid the IsoStore root. Hence we find all the matching directories from the fileSystem and simply return their names.
                return Directory.EnumerateDirectories(_appFilesPath, searchPattern).Select(m => m.Substring(Path.GetDirectoryName(m).Length + 1)).ToArray();
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, share, this);
        }

        public IsolatedStorageFileStream CreateFile(string path)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
        }

        public DateTimeOffset GetCreationTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetCreationTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }

        public DateTimeOffset GetLastAccessTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetLastAccessTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }

        public DateTimeOffset GetLastWriteTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetLastWriteTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }


        public void CopyFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            Contract.EndContractBlock();

            CopyFile(sourceFileName, destinationFileName, false);
        }

        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceFileNameFullPath = GetFullPath(sourceFileName);
            String destinationFileNameFullPath = GetFullPath(destinationFileName);

            try
            {
                File.Copy(sourceFileNameFullPath, destinationFileNameFullPath, overwrite);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(String.Format(SR.PathNotFound_Path, sourceFileName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public void MoveFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceFileNameFullPath = GetFullPath(sourceFileName);
            String destinationFileNameFullPath = GetFullPath(destinationFileName);

            try
            {
                File.Move(sourceFileNameFullPath, destinationFileNameFullPath);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(String.Format(SR.PathNotFound_Path, sourceFileName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
        {
            if (sourceDirectoryName == null)
                throw new ArgumentNullException(nameof(sourceDirectoryName));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            if (sourceDirectoryName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceDirectoryName));
            }

            if (destinationDirectoryName == String.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationDirectoryName));
            }

            Contract.EndContractBlock();

            EnsureStoreIsValid();

            String sourceDirectoryNameFullPath = GetFullPath(sourceDirectoryName);
            String destinationDirectoryNameFullPath = GetFullPath(destinationDirectoryName);

            try
            {
                Directory.Move(sourceDirectoryNameFullPath, destinationDirectoryNameFullPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException(String.Format(SR.PathNotFound_Path, sourceDirectoryName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        /*
         * Public Static Methods
         */
        public static IsolatedStorageFile GetUserStoreForApplication()
        {
            return GetUserStore();
        }

        internal static IsolatedStorageFile GetUserStore()
        {
            IsolatedStorageRoot = FetchOrCreateRoot();

            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf._appFilesPath = IsolatedStorageRoot;
            return isf;
        }

        /*
         * Private Instance Methods
         */
        internal string GetFullPath(string partialPath)
        {
            Debug.Assert(partialPath != null, "partialPath should be non null");

            int i;

            // Chop off directory separator characters at the start of the string because they counfuse Path.Combine.
            for (i = 0; i < partialPath.Length; i++)
            {
                if (partialPath[i] != Path.DirectorySeparatorChar && partialPath[i] != Path.AltDirectorySeparatorChar)
                {
                    break;
                }
            }

            partialPath = partialPath.Substring(i);

            return Path.Combine(_appFilesPath, partialPath);
        }

        /*
         * Private Static Methods
         */
        private static void CreatePathPrefixIfNeeded(string path)
        {
            string root = Path.GetPathRoot(path);

            Debug.Assert(!String.IsNullOrEmpty(root), "Path.GetPathRoot returned null or empty for: " + path);

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (IOException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        internal static string FetchOrCreateRoot()
        {
            string rootFromHost = s_RootFromHost;

            if (s_RootFromHost == null)
            {
                string root = IsolatedStorageSecurityState.GetRootUserDirectory();
                s_RootFromHost = root;
            }

            CreatePathPrefixIfNeeded(s_RootFromHost);

            return s_RootFromHost;
        }

        internal void EnsureStoreIsValid()
        {
            if (Disposed)
                throw new ObjectDisposedException(null, SR.IsolatedStorage_StoreNotOpen);
            Contract.EndContractBlock();

            if (IsDeleted)
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_StoreNotOpen);
            }

            if (_closed)
                throw new InvalidOperationException(SR.IsolatedStorage_StoreNotOpen);
        }

        public void Dispose()
        {
            Close();
            _disposed = true;
        }


        [SecurityCritical]
        internal static Exception GetIsolatedStorageException(string exceptionMsg, Exception rootCause)
        {
            IsolatedStorageException e = new IsolatedStorageException(exceptionMsg, rootCause);
            e._underlyingException = rootCause;
            return e;
        }
    }
}
