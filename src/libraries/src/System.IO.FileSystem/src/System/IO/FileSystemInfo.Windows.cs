// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.IO
{
    partial class FileSystemInfo : IFileSystemObject
    {
        // Cache the file/directory information
        private Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA _data;

        // Cache any error retrieving the file/directory information
        // We use this field in conjunction with the Refresh method which should never throw.
        // If we succeed we store a zero, on failure we store the HResult so that we can
        // throw an appropriate error when attempting to access the cached info.
        private int _dataInitialized = -1;

        [SecurityCritical]
        internal void Init(ref Interop.Kernel32.WIN32_FIND_DATA findData)
        {
            // Copy the information to data
            _data.PopulateFrom(ref findData);
            _dataInitialized = 0;
        }

        FileAttributes IFileSystemObject.Attributes
        {
            get
            {
                EnsureDataInitialized();
                return (FileAttributes)_data.fileAttributes;
            }
            set
            {
                FileSystem.Current.SetAttributes(FullPath, value);
                _dataInitialized = -1;
            }
        }

        bool IFileSystemObject.Exists
        {
            get
            {
                if (_dataInitialized == -1)
                    Refresh();
                if (_dataInitialized != 0)
                {
                    // Refresh was unable to initialize the data.
                    // We should normally be throwing an exception here,
                    // but Exists is supposed to return true or false.
                    return false;
                }
                return (_data.fileAttributes != -1) && ((this is DirectoryInfo) == ((_data.fileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY));
            }
        }

        DateTimeOffset IFileSystemObject.CreationTime
        {
            get
            {
                EnsureDataInitialized();
                long dt = ((long)(_data.ftCreationTimeHigh) << 32) | ((long)_data.ftCreationTimeLow);
                return DateTimeOffset.FromFileTime(dt);
            }
            set
            {
                FileSystem.Current.SetCreationTime(FullPath, value, this is DirectoryInfo);
                _dataInitialized = -1;
            }
        }

        DateTimeOffset IFileSystemObject.LastAccessTime
        {
            get
            {
                EnsureDataInitialized();
                long dt = ((long)(_data.ftLastAccessTimeHigh) << 32) | ((long)_data.ftLastAccessTimeLow);
                return DateTimeOffset.FromFileTime(dt);
            }
            set
            {
                FileSystem.Current.SetLastAccessTime(FullPath, value, (this is DirectoryInfo));
                _dataInitialized = -1;
            }
        }

        DateTimeOffset IFileSystemObject.LastWriteTime
        {
            get
            {
                EnsureDataInitialized();
                long dt = ((long)(_data.ftLastWriteTimeHigh) << 32) | ((long)_data.ftLastWriteTimeLow);
                return DateTimeOffset.FromFileTime(dt);
            }
            set
            {
                FileSystem.Current.SetLastWriteTime(FullPath, value, (this is DirectoryInfo));
                _dataInitialized = -1;
            }
        }

        long IFileSystemObject.Length
        {
            get
            {
                EnsureDataInitialized();
                return ((long)_data.fileSizeHigh) << 32 | ((long)_data.fileSizeLow & 0xFFFFFFFFL);
            }
        }

        private void EnsureDataInitialized()
        {
            if (_dataInitialized == -1)
            {
                _data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
                Refresh();
            }

            if (_dataInitialized != 0) // Refresh was unable to initialize the data
                throw Win32Marshal.GetExceptionForWin32Error(_dataInitialized, FullPath);
        }

        void IFileSystemObject.Refresh()
        {
            // This should not throw, instead we store the result so that we can throw it
            // when someone actually accesses a property
            _dataInitialized = Win32FileSystem.FillAttributeInfo(FullPath, ref _data, false, false);
        }
    }
}
