// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize_Unix : FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize_OS
    {
        protected override long AllocationSize => 10;

        protected override long InitialLength => 10;

        protected override long GetExpectedFileLength(long allocationSize) => allocationSize;

        protected override long GetAllocationSize(FileStream fileStream)
        {
            Interop.Sys.FStat(fileStream.SafeFileHandle, out Interop.Sys.FileStatus fileStatus);
            return fileStatus.Size;
        }
    }
}
