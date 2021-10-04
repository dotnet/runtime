// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

#if MS_IO_REDIST
using Microsoft.IO.Enumeration;

namespace Microsoft.IO
#else
using System.IO.Enumeration;

namespace System.IO
#endif
{
    public partial class FileSystemInfo
    {
        // Cache the file/directory information
        private Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA _data;

        // Cache any error retrieving the file/directory information
        // We use this field in conjunction with the Refresh method which should never throw.
        // If we succeed we store a zero, on failure we store the error code so that we can
        // throw an appropriate error when attempting to access the cached info.
        private int _dataInitialized = -1;

        protected FileSystemInfo()
        {
        }

        internal static unsafe FileSystemInfo Create(string fullPath, ref FileSystemEntry findData)
        {
            FileSystemInfo info = findData.IsDirectory
                ? (FileSystemInfo)new DirectoryInfo(fullPath, fileName: findData.FileName.ToString(), isNormalized: true)
                : new FileInfo(fullPath, fileName: findData.FileName.ToString(), isNormalized: true);

            Debug.Assert(!PathInternal.IsPartiallyQualified(fullPath.AsSpan()), $"'{fullPath}' should be fully qualified when constructed from directory enumeration");

            info.Init(findData._info);
            return info;
        }

        internal void InvalidateCore() => _dataInitialized = -1;

        internal unsafe void Init(Interop.NtDll.FILE_FULL_DIR_INFORMATION* info)
        {
            _data.dwFileAttributes = (int)info->FileAttributes;
            _data.ftCreationTime = *((Interop.Kernel32.FILE_TIME*)&info->CreationTime);
            _data.ftLastAccessTime = *((Interop.Kernel32.FILE_TIME*)&info->LastAccessTime);
            _data.ftLastWriteTime = *((Interop.Kernel32.FILE_TIME*)&info->LastWriteTime);
            _data.nFileSizeHigh = (uint)(info->EndOfFile >> 32);
            _data.nFileSizeLow = (uint)info->EndOfFile;
            _dataInitialized = 0;
        }

        public FileAttributes Attributes
        {
            get
            {
                EnsureDataInitialized();
                return (FileAttributes)_data.dwFileAttributes;
            }
            set
            {
                FileSystem.SetAttributes(FullPath, value);
                _dataInitialized = -1;
            }
        }

        internal bool ExistsCore
        {
            get
            {
                if (_dataInitialized == -1)
                    RefreshCore();
                if (_dataInitialized != 0)
                {
                    // Refresh was unable to initialize the data.
                    // We should normally be throwing an exception here,
                    // but Exists is supposed to return true or false.
                    return false;
                }
                return (_data.dwFileAttributes != -1) && ((this is DirectoryInfo) == ((_data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY));
            }
        }

        internal DateTimeOffset CreationTimeCore
        {
            get
            {
                EnsureDataInitialized();
                return _data.ftCreationTime.ToDateTimeOffset();
            }
            set
            {
                FileSystem.SetCreationTime(FullPath, value, this is DirectoryInfo);
                _dataInitialized = -1;
            }
        }

        internal DateTimeOffset LastAccessTimeCore
        {
            get
            {
                EnsureDataInitialized();
                return _data.ftLastAccessTime.ToDateTimeOffset();
            }
            set
            {
                FileSystem.SetLastAccessTime(FullPath, value, (this is DirectoryInfo));
                _dataInitialized = -1;
            }
        }

        internal DateTimeOffset LastWriteTimeCore
        {
            get
            {
                EnsureDataInitialized();
                return _data.ftLastWriteTime.ToDateTimeOffset();
            }
            set
            {
                FileSystem.SetLastWriteTime(FullPath, value, (this is DirectoryInfo));
                _dataInitialized = -1;
            }
        }

        internal long LengthCore
        {
            get
            {
                EnsureDataInitialized();
                return ((long)_data.nFileSizeHigh) << 32 | _data.nFileSizeLow & 0xFFFFFFFFL;
            }
        }

        private void EnsureDataInitialized()
        {
            if (_dataInitialized == -1)
            {
                _data = default;
                RefreshCore();
            }

            if (_dataInitialized != 0) // Refresh was unable to initialize the data
                throw Win32Marshal.GetExceptionForWin32Error(_dataInitialized, FullPath);
        }

        public void Refresh()
        {
            _linkTargetIsValid = false;
            RefreshCore();
        }

        private void RefreshCore()
        {
            // This should not throw, instead we store the result so that we can throw it
            // when someone actually accesses a property
            _dataInitialized = FileSystem.FillAttributeInfo(FullPath, ref _data, returnErrorOnNotFound: false);
        }

        // If we're opened around a enumerated path that ends in a period or space we need to be able to
        // act on the path normally (open streams/writers/etc.)
        internal string NormalizedPath
            => PathInternal.EndsWithPeriodOrSpace(FullPath) ? PathInternal.EnsureExtendedPrefix(FullPath) : FullPath;
    }
}
