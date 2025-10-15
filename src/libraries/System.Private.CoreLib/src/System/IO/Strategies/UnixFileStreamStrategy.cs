// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class UnixFileStreamStrategy : OSFileStreamStrategy
    {
        internal UnixFileStreamStrategy(SafeFileHandle handle, FileAccess access) : base(handle, access)
        {
        }

        internal UnixFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode) :
            base(path, mode, access, share, options, preallocationSize, unixCreateMode)
        {
        }

        internal override SafeFileHandle SafeFileHandle
        {
            get
            {
                if (CanSeek)
                {
                    // Update the file offset before exposing it since it's possible that
                    // in memory position is out-of-sync with the actual file position.
                    // Some files, such as certain pseudofiles on AzureLinux, may report CanSeek = true
                    // but still fail with ESPIPE when attempting to seek. We ignore this specific error
                    // to match the behavior in RandomAccess where we tolerate seek failures.
                    FileStreamHelpers.Seek(_fileHandle, _filePosition, SeekOrigin.Begin, ignoreSeekErrors: true);
                }

                return _fileHandle;
            }
        }
    }
}
