// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    partial class Win32FileSystem
    {
        internal sealed class Win32FileSystemObject : IFileSystemObject
        {
            private readonly bool _asDirectory;

            // Cache the file/directory information
            internal Interop.WIN32_FILE_ATTRIBUTE_DATA _data;

            // Cache any error retrieving the file/directory information
            // We use this field in conjunction with the Refresh method which should never throw.
            // If we succeed we store a zero, on failure we store the HResult so that we can
            // throw an appropriate error when attempting to access the cached info.
            internal int _dataInitialised;

            private readonly string _fullPath;

            public Win32FileSystemObject(string fullPath, bool asDirectory)
            {
                _asDirectory = asDirectory;
                _fullPath = fullPath;
                _dataInitialised = -1;
            }

            public Win32FileSystemObject(string fullPath, Interop.WIN32_FIND_DATA findData, bool asDirectory)
            {
                _asDirectory = asDirectory;
                _fullPath = fullPath;
                // Copy the information to data
                _data.fileAttributes = (int)findData.dwFileAttributes;
                _data.ftCreationTimeLow = findData.ftCreationTime.dwLowDateTime;
                _data.ftCreationTimeHigh = findData.ftCreationTime.dwHighDateTime;
                _data.ftLastAccessTimeLow = findData.ftLastAccessTime.dwLowDateTime;
                _data.ftLastAccessTimeHigh = findData.ftLastAccessTime.dwHighDateTime;
                _data.ftLastWriteTimeLow = findData.ftLastWriteTime.dwLowDateTime;
                _data.ftLastWriteTimeHigh = findData.ftLastWriteTime.dwHighDateTime;
                _data.fileSizeHigh = findData.nFileSizeHigh;
                _data.fileSizeLow = findData.nFileSizeLow;
                _dataInitialised = 0;
            }

            public FileAttributes Attributes
            {
                get
                {
                    EnsureDataInitialized();
                    return (FileAttributes)_data.fileAttributes;
                }
                set
                {
                    SetAttributesInternal(_fullPath, value);
                    _dataInitialised = -1;
                }
            }

            public DateTimeOffset CreationTime
            {
                get
                {
                    EnsureDataInitialized();
                    long dt = ((long)(_data.ftCreationTimeHigh) << 32) | ((long)_data.ftCreationTimeLow);
                    return DateTimeOffset.FromFileTime(dt);
                }
                set
                {
                    SetCreationTimeInternal(_fullPath, value, _asDirectory);
                    _dataInitialised = -1;
                }
            }

            public bool Exists
            {
                get
                {
                    if (_dataInitialised == -1)
                        Refresh();
                    if (_dataInitialised != 0)
                    {
                        // Refresh was unable to initialise the data.
                        // We should normally be throwing an exception here, 
                        // but Exists is supposed to return true or false.
                        return false;
                    }
                    return (_data.fileAttributes != -1) && (_asDirectory == ((_data.fileAttributes & Interop.FILE_ATTRIBUTE_DIRECTORY) == Interop.FILE_ATTRIBUTE_DIRECTORY));
                }
            }

            public DateTimeOffset LastAccessTime
            {
                get
                {
                    EnsureDataInitialized();
                    long dt = ((long)(_data.ftLastAccessTimeHigh) << 32) | ((long)_data.ftLastAccessTimeLow);
                    return DateTimeOffset.FromFileTime(dt);
                }
                set
                {
                    SetLastAccessTimeInternal(_fullPath, value, _asDirectory);
                    _dataInitialised = -1;
                }
            }

            public DateTimeOffset LastWriteTime
            {
                get
                {
                    EnsureDataInitialized();
                    long dt = ((long)(_data.ftLastWriteTimeHigh) << 32) | ((long)_data.ftLastWriteTimeLow);
                    return DateTimeOffset.FromFileTime(dt);
                }
                set
                {
                    SetLastWriteTimeInternal(_fullPath, value, _asDirectory);
                    _dataInitialised = -1;
                }
            }

            public long Length
            {
                get
                {
                    EnsureDataInitialized();
                    return ((long)_data.fileSizeHigh) << 32 | ((long)_data.fileSizeLow & 0xFFFFFFFFL);
                }
            }

            private void EnsureDataInitialized()
            {
                if (_dataInitialised == -1)
                {
                    _data = new Interop.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    throw Win32Marshal.GetExceptionForWin32Error(_dataInitialised, _fullPath);
            }

            public void Refresh()
            {
                // This should not throw, instead we store the result so that we can throw it
                // when someone actually accesses a property
                _dataInitialised = FillAttributeInfo(_fullPath, ref _data, false, false);
            }
        }
    }
}
