// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize) :
            this(handle, access, bufferSize, handle.IsAsync ?? false)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            this._innerStream = new UnixFileStream(handle, access, bufferSize, isAsync);
        }
    }
}
