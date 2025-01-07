// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_DisposeAsync : FileSystemTest
    {
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
        public async Task DerivedFileStreamDisposeAndCloseUsedForDisposeAsync()
        {
            var fs = new DerivedFileStream(GetTestFilePath(), FileMode.Create);
            Assert.False(fs.DisposeInvoked);
            Assert.False(fs.CloseInvoked);
            await fs.DisposeAsync();
            Assert.True(fs.DisposeInvoked);
            Assert.True(fs.CloseInvoked);
        }

        [Fact]
        public async Task DerivedFileStream_PropertiesDontThrow_OnDisposeAsync()
        {
            var fs = new FileStream_Dispose.DerivedFileStreamAccessingPropertiesOnDispose(GetTestFilePath(), FileMode.Create);
            await fs.DisposeAsync();
            fs.VerifyAfterDispose();
        }

        private sealed class DerivedFileStream : FileStream
        {
            public bool CloseInvoked;
            public bool DisposeInvoked;
            public DerivedFileStream(string path, FileMode mode) : base(path, mode) { }
            protected override void Dispose(bool disposing)
            {
                DisposeInvoked = true;
                base.Dispose(disposing);
            }
            public override void Close()
            {
                CloseInvoked = true;
                base.Close();
            }
        }
    }
}
