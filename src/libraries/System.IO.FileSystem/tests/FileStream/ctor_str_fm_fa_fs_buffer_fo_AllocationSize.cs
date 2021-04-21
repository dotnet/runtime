// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public abstract class FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize : FileStream_ctor_str_fm_fa_fs_buffer_fo
    {
        protected abstract long AllocationSize { get; }

        protected override long InitialLength => 0;

        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, allocationSize: AllocationSize);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(path, mode, access, allocationSize: AllocationSize);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(path, mode, access, share, bufferSize, options, allocationSize: AllocationSize);
    }

    public class FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize_Default : FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize
    {
        protected override long AllocationSize => 0; // specyfing 0 should have no effect

        protected override long InitialLength => 0;
    }

    public class FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize_Negative : FileStream_ctor_str_fm_fa_fs_buffer_fo_AllocationSize
    {
        protected override long AllocationSize => -1; // specyfing negative value should have no effect

        protected override long InitialLength => 0;
    }
}
