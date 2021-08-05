// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_ctor_str_fm_fa_fs_buffer_async : FileStream_ctor_str_fm_fa_fs_buffer
    {
        protected sealed override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return CreateFileStream(path, mode, access, share, bufferSize, false);
        }

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
        {
            return new FileStream(path, mode, access, share, bufferSize, useAsync);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ValidUseAsync(bool isAsync)
        {
            using (FileStream fs = CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, isAsync))
            {
                Assert.Equal(isAsync, fs.IsAsync);
            }
        }
    }
}
