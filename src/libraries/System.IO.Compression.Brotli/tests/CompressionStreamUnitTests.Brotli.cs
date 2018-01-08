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
        public override int BufferSize { get => 65520; }
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

        [Fact]
        [OuterLoop("Test takes ~12 seconds to run")]
        public override void Dispose_WithUnfinishedWriteAsync() { base.Dispose_WithUnfinishedWriteAsync(); }

        [Fact]
        [OuterLoop("Test takes ~6 seconds to run")]
        public override void FlushAsync_DuringWriteAsync() { base.FlushAsync_DuringWriteAsync(); }

        [Fact]
        [OuterLoop("Test takes ~6 seconds to run")]
        public override void WriteAsync_DuringWriteAsync() { base.WriteAsync_DuringWriteAsync(); }

        [Fact]
        public void DerivedStream_ReadWriteSpan_UsesReadWriteArray()
        {
            var ms = new MemoryStream();
            using (var compressor = new DerivedBrotliStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.Write(new Span<byte>(new byte[1]));
                Assert.True(compressor.WriteArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedBrotliStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.Read(new Span<byte>(new byte[1]));
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedBrotliStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.ReadAsync(new Memory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedBrotliStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])).Wait();
                Assert.True(compressor.WriteArrayInvoked);
            }
        }

        private sealed class DerivedBrotliStream : BrotliStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedBrotliStream(Stream stream, CompressionMode mode) : base(stream, mode) { }
            internal DerivedBrotliStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

            public override int Read(byte[] array, int offset, int count)
            {
                ReadArrayInvoked = true;
                return base.Read(array, offset, count);
            }

            public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
            {
                ReadArrayInvoked = true;
                return base.ReadAsync(array, offset, count, cancellationToken);
            }

            public override void Write(byte[] array, int offset, int count)
            {
                WriteArrayInvoked = true;
                base.Write(array, offset, count);
            }

            public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
            {
                WriteArrayInvoked = true;
                return base.WriteAsync(array, offset, count, cancellationToken);
            }
        }
    }
}
