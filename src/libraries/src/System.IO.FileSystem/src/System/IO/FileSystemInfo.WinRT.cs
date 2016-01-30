// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
