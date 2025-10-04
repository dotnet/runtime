// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZstandardStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZstandardStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZstandardStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZstandardStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new ZstandardStream(stream, options == null ? null : new ZstandardCompressionOptions { Quality = options.CompressionLevel }, leaveOpen);

        public override Stream BaseStream(Stream stream) => ((ZstandardStream)stream).BaseStream;

        // The tests are relying on an implementation detail of BrotliStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZstandardTestData", Path.GetFileName(uncompressedPath) + ".zst");

        [Fact]
        public void ZstandardStream_WithEncoder_CompressesData()
        {
            var encoder = new ZstandardEncoder(5, 10);
            byte[] testData = CreateTestData();
            using var input = new MemoryStream(testData);
            using var output = new MemoryStream();

            using (var compressionStream = new ZstandardStream(output, encoder, leaveOpen: true))
            {
                input.CopyTo(compressionStream);
            }

            // Verify data was compressed
            Assert.True(output.Length > 0);
            Assert.True(output.Length < testData.Length);

            // Verify the encoder was reset, not disposed (should be reusable)
            using var output2 = new MemoryStream();
            using (var compressionStream2 = new ZstandardStream(output2, encoder, leaveOpen: true))
            {
                input.Position = 0;
                input.CopyTo(compressionStream2);
            }

            Assert.True(output2.Length > 0);
            encoder.Dispose(); // Clean up
        }

        [Fact]
        public void ZstandardStream_WithDecoder_DecompressesData()
        {
            // First, create some compressed data
            byte[] testData = CreateTestData();
            byte[] compressedData = new byte[ZstandardEncoder.GetMaxCompressedLength(testData.Length)];
            bool compressResult = ZstandardEncoder.TryCompress(testData, compressedData, out int compressedLength);
            Assert.True(compressResult);

            Array.Resize(ref compressedData, compressedLength);

            var decoder = new ZstandardDecoder();
            using var input = new MemoryStream(compressedData);
            using var output = new MemoryStream();

            using (var decompressionStream = new ZstandardStream(input, decoder, leaveOpen: true))
            {
                decompressionStream.CopyTo(output);
            }

            // Verify data was decompressed correctly
            Assert.Equal(testData, output.ToArray());

            // Verify the decoder was reset, not disposed (should be reusable)
            using var output2 = new MemoryStream();
            using (var decompressionStream2 = new ZstandardStream(input, decoder, leaveOpen: true))
            {
                input.Position = 0;
                decompressionStream2.CopyTo(output2);
            }

            Assert.Equal(testData, output2.ToArray());
            decoder.Dispose(); // Clean up
        }

        private static byte[] CreateTestData()
        {
            // Create some test data that compresses well
            byte[] data = new byte[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 10); // Repeating pattern
            }
            return data;
        }
    }
}
