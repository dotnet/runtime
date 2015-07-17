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

        private static readonly char[] s_additionalInvalidChars = new[] { '?', '*' };
    }
}
