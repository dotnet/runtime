// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs_buffer_fo_as : FileStream_ctor_str_fm_fa_fs_buffer_fo_as_base
    {
        protected override long AllocationSize => 10;

        protected override long InitialLength => 0; // Windows modifies AllocationSize, but not EndOfFile (file length)

        private long GetExpectedFileLength(long allocationSize) => 0; // Windows modifies AllocationSize, but not EndOfFile (file length)

        private unsafe long GetActualAllocationSize(FileStream fileStream)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            Assert.True(Interop.Kernel32.GetFileInformationByHandleEx(fileStream.SafeFileHandle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)));

            return info.AllocationSize;
        }
    }
}
