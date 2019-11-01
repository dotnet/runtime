// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_DisposeAsync : FileSystemTest
    {
        [Fact]
        public void CanDisposeAsync()
        {
            Assert.True(new FileStream(GetTestFilePath(), FileMode.Create).DisposeAsync().IsCompletedSuccessfully);
        }

        [Fact]
        public async Task DisposeAsyncClosesHandle()
        {
            SafeFileHandle handle;
            var fs = new FileStream(GetTestFilePath(), FileMode.Create);
            handle = fs.SafeFileHandle;
            await fs.DisposeAsync();
            Assert.True(handle.IsClosed);
        }

        [Fact]
        public async Task HandlesMultipleDisposeAsync()
        {
            var fs = new FileStream(GetTestFilePath(), FileMode.Create);
            await fs.DisposeAsync();
            fs.Dispose();
            await fs.DisposeAsync();
        }

        [Fact]
        public async Task DisposeAsyncFlushes()
        {
            string path = GetTestFilePath();

            var fs1 = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs1.Write(new byte[100], 0, 100);

            using (var fs2 = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Assert.Equal(0, fs2.Length);
            }

            await fs1.DisposeAsync();

            using (var fs2 = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Assert.Equal(100, fs2.Length);
            }
        }

        [Fact]
        public async Task DerivedFileStreamDisposeUsedForDisposeAsync()
        {
            var fs = new OverridesDisposeFileStream(GetTestFilePath(), FileMode.Create);
            Assert.False(fs.DisposeInvoked);
            await fs.DisposeAsync();
            Assert.True(fs.DisposeInvoked);
        }

        private sealed class OverridesDisposeFileStream : FileStream
        {
            public bool DisposeInvoked;
            public OverridesDisposeFileStream(string path, FileMode mode) : base(path, mode) { }
            protected override void Dispose(bool disposing)
            {
                DisposeInvoked = true;
                base.Dispose(disposing);
            }
        }
    }
}
