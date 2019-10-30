// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
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

        [Fact]
        public void ValidUseAsync()
        {
            using (CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, true))
            { }

            using (CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, false))
            { }
        }

    }
}
