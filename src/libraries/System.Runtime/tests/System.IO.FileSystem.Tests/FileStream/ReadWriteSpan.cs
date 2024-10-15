// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public sealed class Sync_DerivedFileStream_ReadWrite_Span : FileSystemTest
    {
        [Fact]
        public void CallSpanReadWriteOnDerivedFileStream_ArrayMethodsUsed()
        {
            using (var fs = new DerivedFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 0x1000, FileOptions.None))
            {
                Assert.False(fs.WriteArrayInvoked);
                Assert.False(fs.ReadArrayInvoked);

                fs.Write(new ReadOnlySpan<byte>(new byte[1]));
                Assert.True(fs.WriteArrayInvoked);
                Assert.False(fs.ReadArrayInvoked);

                fs.Position = 0;
                fs.Read(new Span<byte>(new byte[1]));
                Assert.True(fs.WriteArrayInvoked);
                Assert.True(fs.ReadArrayInvoked);
            }
        }

        [Fact]
        public async Task CallMemoryReadWriteAsyncOnDerivedFileStream_ArrayMethodsUsed()
        {
            using (var fs = new DerivedFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 0x1000, FileOptions.None))
            {
                Assert.False(fs.WriteAsyncArrayInvoked);
                Assert.False(fs.ReadAsyncArrayInvoked);

                await fs.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]));
                Assert.True(fs.WriteAsyncArrayInvoked);
                Assert.False(fs.ReadAsyncArrayInvoked);

                fs.Position = 0;
                await fs.ReadAsync(new Memory<byte>(new byte[1]));
                Assert.True(fs.WriteAsyncArrayInvoked);
                Assert.True(fs.ReadAsyncArrayInvoked);
            }
        }
    }

    internal sealed class DerivedFileStream : FileStream
    {
        public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
        public bool ReadAsyncArrayInvoked = false, WriteAsyncArrayInvoked = false;

        public DerivedFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) :
            base(path, mode, access, share, bufferSize, options)
        {
        }

        public override int Read(byte[] array, int offset, int count)
        {
            ReadArrayInvoked = true;
            return base.Read(array, offset, count);
        }

        public override void Write(byte[] array, int offset, int count)
        {
            WriteArrayInvoked = true;
            base.Write(array, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadAsyncArrayInvoked = true;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WriteAsyncArrayInvoked = true;
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
