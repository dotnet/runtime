// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public static class DuplexStreamTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WriteOnlyProxyDispose_CompletesDuplexStream(bool isAsync)
        {
            var stream = new TestDuplexStream();
            Stream writeOnlyStream = stream.GetWriteOnlyStream();

            if (isAsync) await writeOnlyStream.DisposeAsync();
            else writeOnlyStream.Dispose();

            Assert.True(stream.CompleteWritesCount == 1, $"Disposing the write-only stream must call {nameof(TestDuplexStream.CompleteWrites)}.");

            if (isAsync) await writeOnlyStream.DisposeAsync();
            else writeOnlyStream.Dispose();

            Assert.True(stream.CompleteWritesCount == 1, $"Disposing the write-only stream multiple times must not call {nameof(TestDuplexStream.CompleteWrites)} multiple times.");
        }

        [Fact]
        public static async Task WriteOnlyProxy_IsWriteOnly()
        {
            Stream stream = new TestDuplexStream().GetWriteOnlyStream();

            Assert.False(stream.CanRead);
            Assert.ThrowsAny<NotSupportedException>(() => _ = stream.ReadTimeout);
            Assert.ThrowsAny<NotSupportedException>(() => stream.ReadTimeout = 1);
            Assert.ThrowsAny<NotSupportedException>(() => stream.Read(Array.Empty<byte>()));
            await Assert.ThrowsAnyAsync<NotSupportedException>(() => stream.ReadAsync(Array.Empty<byte>()).AsTask());
            Assert.ThrowsAny<NotSupportedException>(() => stream.CopyTo(Stream.Null));
            await Assert.ThrowsAnyAsync<NotSupportedException>(() => stream.CopyToAsync(Stream.Null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WriteOnlyProxy_UnusableAfterDispose(bool isAsync)
        {
            Stream stream = new TestDuplexStream().GetWriteOnlyStream();

            if (isAsync) await stream.DisposeAsync();
            else stream.Dispose();

            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
            Assert.False(stream.CanTimeout);
            Assert.ThrowsAny<ObjectDisposedException>(() => _ = stream.Position);
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.Position = 1);
            Assert.ThrowsAny<ObjectDisposedException>(() => _ = stream.ReadTimeout);
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.ReadTimeout = 1);
            Assert.ThrowsAny<ObjectDisposedException>(() => _ = stream.WriteTimeout);
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.WriteTimeout = 1);
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.Write(Array.Empty<byte>()));
            await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => stream.WriteAsync(Array.Empty<byte>()).AsTask());
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.Flush());
            await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => stream.FlushAsync());
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.ThrowsAny<ObjectDisposedException>(() => stream.SetLength(0));
        }

        private sealed class TestDuplexStream : DuplexStream
        {
            public int CompleteWritesCount { get; private set; }

            public override void CompleteWrites() =>
                ++CompleteWritesCount;

            public override ValueTask CompleteWritesAsync(CancellationToken cancellationToken = default)
            {
                ++CompleteWritesCount;
                return default;
            }

            // note: throwing Exception rather than NotImplementedException differentiates between
            // the write proxy throwing other exceptions versus incorrectly forwarding calls to its DuplexStream.

            public override bool CanTimeout => throw new Exception();
            public override bool CanRead => throw new Exception();
            public override bool CanSeek => throw new Exception();
            public override bool CanWrite => throw new Exception();
            public override long Length => throw new Exception();
            public override long Position { get => throw new Exception(); set => throw new Exception(); }
            public override int ReadTimeout { get => throw new Exception(); set => throw new Exception(); }
            public override int WriteTimeout { get => throw new Exception(); set => throw new Exception(); }

            public override void Flush() => throw new Exception();
            public override int Read(byte[] buffer, int offset, int count) => throw new Exception();
            public override long Seek(long offset, SeekOrigin origin) => throw new Exception();
            public override void SetLength(long value) => throw new Exception();
            public override void Write(byte[] buffer, int offset, int count) => throw new Exception();
            public override void CopyTo(Stream destination, int bufferSize) => throw new Exception();
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new Exception();
        }
    }
}
