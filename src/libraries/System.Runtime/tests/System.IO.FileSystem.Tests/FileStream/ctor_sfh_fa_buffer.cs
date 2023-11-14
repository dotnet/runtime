// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
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
        public void NegativeBufferSize_Throws()
        {
            using (var handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => CreateFileStream(handle, FileAccess.Read, -1));
            }
        }

        [Fact]
        public void InvalidBufferSize_DoesNotCloseHandle()
        {
            using (var handle = File.OpenHandle(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateFileStream(handle, FileAccess.Read, -1));
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Assert.False(handle.IsClosed);
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
