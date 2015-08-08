// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO
{
    partial class FileSystemInfo
    {
        internal IFileSystemObject FileSystemObject
        {
            get { return this; }
        }

        internal void Invalidate()
        {
            _dataInitialized = -1;
        }
    }
}
