// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    partial class FileSystemInfo
    {
        private IFileSystemObject _fileSystemObject;

        internal IFileSystemObject FileSystemObject
        {
            get { return _fileSystemObject ?? (_fileSystemObject = MultiplexingWin32WinRTFileSystem.GetFileSystemObject(this, FullPath)); }
        }

        internal void Invalidate()
        {
            _dataInitialized = -1;
            _fileSystemObject = null;
        }
    }
}
