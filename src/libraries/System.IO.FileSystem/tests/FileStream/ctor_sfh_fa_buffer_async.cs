// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ctor_sfh_fa_buffer_async : FileStream_ctor_sfh_fa_buffer
    {
        protected sealed override FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            // default for isAsync is platform specific, we need a better way to determine this
            return CreateFileStream(handle, access, bufferSize, true);
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
