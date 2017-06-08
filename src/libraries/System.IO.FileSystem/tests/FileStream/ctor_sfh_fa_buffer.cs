// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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


        [Theory,
            InlineData(0),
            InlineData(-1)]
        public void InvalidBufferSize_Throws(int size)
        {
            using (var handle = new SafeFileHandle(new IntPtr(1), ownsHandle: false))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => CreateFileStream(handle, FileAccess.Read, size));
            }
        }

        [ActiveIssue(20797, TargetFrameworkMonikers.NetFramework)]  // This fails on desktop
        [Fact]
        public void InvalidBufferSize_DoesNotCloseHandle()
        {
            using (var handle = new SafeFileHandle(new IntPtr(1), ownsHandle: false))
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
