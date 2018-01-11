// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class GzipStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new GZipStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((GZipStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");

        [Fact]
        public void Precancellation()
        {
            var ms = new MemoryStream();
            using (Stream compressor = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
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
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.Write(new Span<byte>(new byte[1]));
                Assert.True(compressor.WriteArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.Read(new Span<byte>(new byte[1]));
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.ReadAsync(new Memory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])).Wait();
                Assert.True(compressor.WriteArrayInvoked);
            }
        }

        private sealed class DerivedGZipStream : GZipStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedGZipStream(Stream stream, CompressionMode mode) : base(stream, mode) { }
            internal DerivedGZipStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

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
