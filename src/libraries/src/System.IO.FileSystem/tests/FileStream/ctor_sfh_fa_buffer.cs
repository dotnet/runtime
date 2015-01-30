// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ctor_sfh_fa_buffer : FileStream_ctor_sfh_fa
    {
        protected sealed override FileStream CreateFileStream(SafeFileHandle handle, FileAccess access)
        {
            return CreateFileStream(handle, access, 4096);
        }

        protected virtual FileStream CreateFileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
        {
            return new FileStream(handle, access, bufferSize);
        }

        [Fact]
        public void InvalidBufferSizeThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => CreateFileStream(fs.SafeFileHandle, FileAccess.Read, -1));
                Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => CreateFileStream(fs.SafeFileHandle, FileAccess.Read, 0));
            }
        }

        [Fact]
        public void ValidBufferSize()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                using (FileStream fsw = CreateFileStream(fs.SafeFileHandle, FileAccess.Write, 64 * 1024))
                { }
            }
        }
    }
}
