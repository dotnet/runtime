// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class BrotliStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new BrotliStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new BrotliStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new BrotliStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new BrotliStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((BrotliStream)stream).BaseStream;

        // The tests are relying on an implementation detail of BrotliStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("BrotliTestData", Path.GetFileName(uncompressedPath) + ".br");

        [Fact]
        public void Precancellation()
        {
            var ms = new MemoryStream();
            using (Stream compressor = new BrotliStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                Assert.True(compressor.WriteAsync(new byte[1], 0, 1, new CancellationToken(true)).IsCanceled);
                Assert.True(compressor.FlushAsync(new CancellationToken(true)).IsCanceled);
            }
            using (Stream decompressor = CreateStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                Assert.True(decompressor.ReadAsync(new byte[1], 0, 1, new CancellationToken(true)).IsCanceled);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeAsync_Flushes(bool leaveOpen)
        {
            var ms = new MemoryStream();
            var bs = new BrotliStream(ms, CompressionMode.Compress, leaveOpen);
            bs.WriteByte(1);
            Assert.Equal(0, ms.Position);
            await bs.DisposeAsync();
            Assert.InRange(ms.ToArray().Length, 1, int.MaxValue);
            if (leaveOpen)
            {
                Assert.InRange(ms.Position, 1, int.MaxValue);
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => ms.Position);
            }
        }

        [Fact]
        [OuterLoop("Test takes ~6 seconds to run")]
        public override void FlushAsync_DuringWriteAsync() { base.FlushAsync_DuringWriteAsync(); }

        [Fact]
        [OuterLoop("Test takes ~6 seconds to run")]
        public override void WriteAsync_DuringWriteAsync() { base.WriteAsync_DuringWriteAsync(); }
    }
}
