// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs_buffer_fo_as : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        protected override long AllocationSize => 10;

        protected override long InitialLength => 10;

        private long GetExpectedFileLength(long allocationSize) => allocationSize;

        private long GetActualAllocationSize(FileStream fileStream)
        {
            // On Unix posix_fallocate modifies file length and we are using fstat to get it for verificaiton
            Interop.Sys.FStat(fileStream.SafeFileHandle, out Interop.Sys.FileStatus fileStatus);
            return fileStatus.Size;
        }
    }
}
