// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Strategies;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed unsafe class FileOverlappedValueTaskSource : OverlappedValueTaskSource
    {
        internal FileOverlappedValueTaskSource(SafeFileHandle fileHandle) : base(fileHandle, fileHandle.ThreadPoolBinding!, fileHandle.CanSeek)
        {
        }

        protected override void TryToReuse()
        {
            ((SafeFileHandle)_fileHandle).TryToReuse(this);
        }

        protected override void HandleIncomplete(Stream owner, uint errorCode, uint byteCount)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_SUCCESS:
                case Interop.Errors.ERROR_BROKEN_PIPE:
                case Interop.Errors.ERROR_NO_DATA:
                case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                    if (_bufferSize != byteCount) // true only for incomplete operations
                    {
                        ((OSFileStreamStrategy)owner).OnIncompleteOperation(_bufferSize, (int)byteCount);
                    }
                    break;

                default:
                    // Failure or cancellation
                    ((OSFileStreamStrategy)owner).OnIncompleteOperation(_bufferSize, 0);
                    break;
            }
        }
    }
}
