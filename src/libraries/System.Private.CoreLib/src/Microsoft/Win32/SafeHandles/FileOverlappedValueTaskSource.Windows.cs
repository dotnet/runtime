// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Strategies;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed unsafe class FileOverlappedValueTaskSource : OverlappedValueTaskSource
    {
        private readonly SafeFileHandle _safeFileHandle;
        private readonly OSFileStreamStrategy? _strategy;

        internal FileOverlappedValueTaskSource(SafeFileHandle fileHandle, OSFileStreamStrategy? strategy = null)
            : base(fileHandle, fileHandle.ThreadPoolBinding!, fileHandle.CanSeek)
        {
            _safeFileHandle = fileHandle;
            _strategy = strategy;
        }

        protected override bool TryToReuse() => _safeFileHandle.TryToReuse(this);

        internal override void UpdateOwnerState(uint errorCode, uint byteCount)
        {
            if (_strategy is null)
            {
                return; // not every SafeFileHandle is owned by a FileStreamStrategy
            }

            switch (errorCode)
            {
                case Interop.Errors.ERROR_SUCCESS:
                case Interop.Errors.ERROR_BROKEN_PIPE:
                case Interop.Errors.ERROR_NO_DATA:
                case Interop.Errors.ERROR_PIPE_NOT_CONNECTED:
                case Interop.Errors.ERROR_HANDLE_EOF:
                case Interop.Errors.ERROR_MORE_DATA:
                    if (_bufferSize != byteCount) // true only for incomplete operations
                    {
                        _strategy.OnIncompleteOperation(_bufferSize, (int)byteCount);
                    }
                    break;

                default:
                    // Failure or cancellation (must not rely on byteCount)
                    _strategy.OnIncompleteOperation(_bufferSize, 0);
                    break;
            }
        }
    }
}
