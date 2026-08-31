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
                    // The base SafeFileHandle getter synchronizes the file offset before exposing the handle
                    // since the in-memory position may be out-of-sync with the actual file position.
                    // However, some files such as certain pseudofiles on AzureLinux may report CanSeek = true
                    // but still fail with ESPIPE when attempting to seek. We tolerate this specific error
                    // to match the behavior in RandomAccess where we ignore pwrite and pread failures on certain file types.
                    long result = Interop.Sys.LSeek(_fileHandle, _filePosition, Interop.Sys.SeekWhence.SEEK_SET);
                    if (result < 0)
                    {
                        Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                        if (errorInfo.Error != Interop.Error.ESPIPE)
                        {
                            throw Interop.GetExceptionForIoErrno(errorInfo, _fileHandle.Path);
                        }
                        _fileHandle.SupportsRandomAccess = false;
                    }
                }

                return _fileHandle;
            }
        }
    }
}
