// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Text;
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

        public static IEnumerable<object[]> DecompressFailsWithWrapperStream_MemberData()
        {
            foreach (object[] testFile in UncompressedTestFiles())
            {
                yield return new object[] { testFile[0], "GZipTestData", ".gz" };
                yield return new object[] { testFile[0], "ZLibTestData", ".z" };
            }
        }

        /// <summary>Test to pass GZipStream data and ZLibStream data to a DeflateStream</summary>
        [Theory]
        [MemberData(nameof(DecompressFailsWithWrapperStream_MemberData))]
        public async Task DecompressFailsWithWrapperStream(string uncompressedPath, string newDirectory, string newSuffix)
        {
            string fileName = Path.Combine(newDirectory, Path.GetFileName(uncompressedPath) + newSuffix);
            using (LocalMemoryStream baseStream = await LocalMemoryStream.readAppFileAsync(fileName))
            using (Stream cs = CreateStream(baseStream, CompressionMode.Decompress))
            {
                int _bufferSize = 2048;
                var bytes = new byte[_bufferSize];
                Assert.Throws<InvalidDataException>(() => { cs.Read(bytes, 0, _bufferSize); });
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
        [InlineData(false)]
        [InlineData(true)]
        public void CompressorNotClosed_DecompressorStillSuccessful(bool closeCompressorBeforeDecompression)
        {
            const string Input = "example";

            var ms = new MemoryStream();

            using (var compressor = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: closeCompressorBeforeDecompression))
            {
                compressor.Write(Encoding.ASCII.GetBytes(Input));
                compressor.Flush();
                if (closeCompressorBeforeDecompression)
                {
                    compressor.Dispose();
                }

                ms.Position = 0;
                using (var decompressor = new DeflateStream(ms, CompressionMode.Decompress, leaveOpen: true))
                {
                    var decompressed = new MemoryStream();
                    decompressor.CopyTo(decompressed);
                    Assert.Equal(Input, Encoding.ASCII.GetString(decompressed.ToArray()));
                }
            }
        }

        private sealed class DerivedDeflateStream : DeflateStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedDeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadArrayInvoked = true;
                return base.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ReadArrayInvoked = true;
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteArrayInvoked = true;
                base.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                WriteArrayInvoked = true;
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }
    }
}
