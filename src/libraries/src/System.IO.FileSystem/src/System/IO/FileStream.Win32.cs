// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            this._innerStream = new Win32FileStream(handle, access, bufferSize, this);
        }

        public FileStream(Microsoft.Win32.SafeHandles.SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            this._innerStream = new Win32FileStream(handle, access, bufferSize, isAsync, this);
        }

        static partial void ValidatePath(string fullPath, string paramName)
        {
            // Prevent access to your disk drives as raw block devices.
            if (fullPath.StartsWith("\\\\.\\", StringComparison.Ordinal))
                throw new ArgumentException(SR.Arg_DevicesNotSupported, paramName);
        }
    }
}
