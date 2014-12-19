// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            this._innerStream = new Win32FileStream(handle, access, bufferSize);
        }

        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            this._innerStream = new Win32FileStream(handle, access, bufferSize, isAsync);
        }
    }
}
