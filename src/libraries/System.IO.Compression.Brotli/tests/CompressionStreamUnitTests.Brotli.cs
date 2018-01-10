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
    }
}
