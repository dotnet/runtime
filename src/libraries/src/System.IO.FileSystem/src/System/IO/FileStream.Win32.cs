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

        static partial void ValidatePath(string fullPath)
        {
            // Prevent access to your disk drives as raw block devices.
            if (fullPath.StartsWith("\\\\.\\", StringComparison.Ordinal))
                throw new ArgumentException(SR.Arg_DevicesNotSupported);

            // Check for additional invalid characters.  Most invalid characters were checked above
            // in our call to Path.GetFullPath(path);
            if (fullPath.IndexOfAny(s_additionalInvalidChars) != -1)
                throw new ArgumentException(SR.Argument_InvalidPathChars);

            if (fullPath.IndexOf(':', 2) != -1)
                throw new NotSupportedException(SR.Argument_PathFormatNotSupported);
        }

        private static readonly char[] s_additionalInvalidChars = new[] { '?', '*' };
    }
}
