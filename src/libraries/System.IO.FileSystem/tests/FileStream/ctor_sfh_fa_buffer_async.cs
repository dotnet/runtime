// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_sfh_fa_buffer_async : FileStream_ctor_sfh_fa_buffer
    {
        protected sealed override FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            return CreateFileStream(handle, access, bufferSize, false);
        }

        protected virtual FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            return new FileStream(handle, access, bufferSize, isAsync);
        }

        [Fact]
        public void MatchedAsync()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, true))
            {
                using (CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, true))
                { }
            }
        }

        [Fact]
        public void UnmatchedAsyncThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, true))
            {
                Assert.Throws<ArgumentException>(() => CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, false));
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 4096, false))
            {
                Assert.Throws<ArgumentException>(() => CreateFileStream(fs.SafeFileHandle, FileAccess.ReadWrite, 4096, true));
            }
        }
    }
}
