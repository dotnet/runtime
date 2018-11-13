// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class DeflateStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new DeflateStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((DeflateStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("DeflateTestData", Path.GetFileName(uncompressedPath));

        /// <summary>
        /// Test to pass gzipstream data to a deflatestream
        /// </summary>
        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        public async Task DecompressFailsWithRealGzStream(string uncompressedPath)
        {
            string fileName = Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");
            var baseStream = await LocalMemoryStream.readAppFileAsync(fileName);
            var zip = CreateStream(baseStream, CompressionMode.Decompress);
            int _bufferSize = 2048;
            var bytes = new byte[_bufferSize];
            Assert.Throws<InvalidDataException>(() => { zip.Read(bytes, 0, _bufferSize); });
            zip.Dispose();
        }

        [Fact]
        public void Precancellation()
        {
            var ms = new MemoryStream();
            using (Stream compressor = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
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
        public void DerivedStream_ReadWriteSpan_UsesReadWriteArray()
        {
            var ms = new MemoryStream();
            using (var compressor = new DerivedDeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.Write(new Span<byte>(new byte[1]));
                Assert.True(compressor.WriteArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedDeflateStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.Read(new Span<byte>(new byte[1]));
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedDeflateStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.ReadAsync(new Memory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedDeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.WriteArrayInvoked);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, false)]
        public async Task DisposeAsync_Flushes(bool derived, bool leaveOpen)
        {
            var ms = new MemoryStream();
            var ds = derived ?
                new DerivedDeflateStream(ms, CompressionMode.Compress, leaveOpen) :
                new DeflateStream(ms, CompressionMode.Compress, leaveOpen);
            ds.WriteByte(1);
            Assert.Equal(0, ms.Position);
            await ds.DisposeAsync();
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

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, false)]
        public async Task DisposeAsync_MultipleCallsAllowed(bool derived, bool leaveOpen)
        {
            using (var ds = derived ?
                new DerivedDeflateStream(new MemoryStream(), CompressionMode.Compress, leaveOpen) :
                new DeflateStream(new MemoryStream(), CompressionMode.Compress, leaveOpen))
            {
                await ds.DisposeAsync();
                await ds.DisposeAsync();
            }
        }

        private sealed class DerivedDeflateStream : DeflateStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedDeflateStream(Stream stream, CompressionMode mode) : base(stream, mode) { }
            internal DerivedDeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

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
