// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
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
        protected override bool ReadsMayBlockUntilBufferFullOrEOF => true;

        // The tests are relying on an implementation detail of BrotliStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("BrotliTestData", Path.GetFileName(uncompressedPath) + ".br");

        [Fact]
        [OuterLoop("Test takes ~6 seconds to run")]
        public override void FlushAsync_DuringWriteAsync() { base.FlushAsync_DuringWriteAsync(); }

        [Fact]
        public void InvalidQuality()
        {
            Assert.Throws<ArgumentOutOfRangeException>("quality", () => new BrotliEncoder(-1, 11));
            Assert.Throws<ArgumentOutOfRangeException>("quality", () => new BrotliEncoder(12, 11));
            Assert.Throws<ArgumentOutOfRangeException>("quality", () => BrotliEncoder.TryCompress(new ReadOnlySpan<byte>(), new Span<byte>(), out int bytesWritten, -1, 13));
            Assert.Throws<ArgumentOutOfRangeException>("quality", () => BrotliEncoder.TryCompress(new ReadOnlySpan<byte>(), new Span<byte>(), out int bytesWritten, 12, 13));
        }

        [Fact]
        public void InvalidWindow()
        {
            Assert.Throws<ArgumentOutOfRangeException>("window", () => new BrotliEncoder(10, -1));
            Assert.Throws<ArgumentOutOfRangeException>("window", () => new BrotliEncoder(10, 9));
            Assert.Throws<ArgumentOutOfRangeException>("window", () => new BrotliEncoder(10, 25));
            Assert.Throws<ArgumentOutOfRangeException>("window", () => BrotliEncoder.TryCompress(new ReadOnlySpan<byte>(), new Span<byte>(), out int bytesWritten, 6, -1));
            Assert.Throws<ArgumentOutOfRangeException>("window", () => BrotliEncoder.TryCompress(new ReadOnlySpan<byte>(), new Span<byte>(), out int bytesWritten, 6, 9));
            Assert.Throws<ArgumentOutOfRangeException>("window", () => BrotliEncoder.TryCompress(new ReadOnlySpan<byte>(), new Span<byte>(), out int bytesWritten, 6, 25));
        }

        [Fact]
        public void GetMaxCompressedSize_Basic()
        {
            Assert.Throws<ArgumentOutOfRangeException>("inputSize", () => BrotliEncoder.GetMaxCompressedLength(-1));
            Assert.Throws<ArgumentOutOfRangeException>("inputSize", () => BrotliEncoder.GetMaxCompressedLength(2147483133));
            Assert.InRange(BrotliEncoder.GetMaxCompressedLength(2147483132), 0, int.MaxValue);
            Assert.Equal(1, BrotliEncoder.GetMaxCompressedLength(0));
        }

        [Fact]
        public void GetMaxCompressedSize()
        {
            string uncompressedFile = UncompressedTestFile();
            string compressedFile = CompressedTestFile(uncompressedFile);
            int maxCompressedSize = BrotliEncoder.GetMaxCompressedLength((int)new FileInfo(uncompressedFile).Length);
            int actualCompressedSize = (int)new FileInfo(compressedFile).Length;
            Assert.True(maxCompressedSize >= actualCompressedSize, $"MaxCompressedSize: {maxCompressedSize}, ActualCompressedSize: {actualCompressedSize}");
        }

        /// <summary>
        /// Test to ensure that when given an empty Destination span, the decoder will consume no input and write no output.
        /// </summary>
        [Fact]
        public void Decompress_WithEmptyDestination()
        {
            string testFile = UncompressedTestFile();
            byte[] sourceBytes = File.ReadAllBytes(CompressedTestFile(testFile));
            byte[] destinationBytes = new byte[0];
            ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(sourceBytes);
            Span<byte> destination = new Span<byte>(destinationBytes);

            Assert.False(BrotliDecoder.TryDecompress(source, destination, out int bytesWritten), "TryDecompress completed successfully but should have failed due to too short of a destination array");
            Assert.Equal(0, bytesWritten);

            BrotliDecoder decoder = default;
            var result = decoder.Decompress(source, destination, out int bytesConsumed, out bytesWritten);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.DestinationTooSmall, result);
        }

        /// <summary>
        /// Test to ensure that when given an empty source span, the decoder will consume no input and write no output
        /// </summary>
        [Fact]
        public void Decompress_WithEmptySource()
        {
            byte[] sourceBytes = new byte[0];
            byte[] destinationBytes = new byte[100000];
            ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(sourceBytes);
            Span<byte> destination = new Span<byte>(destinationBytes);

            Assert.False(BrotliDecoder.TryDecompress(source, destination, out int bytesWritten), "TryDecompress completed successfully but should have failed due to too short of a source array");
            Assert.Equal(0, bytesWritten);

            BrotliDecoder decoder = default;
            var result = decoder.Decompress(source, destination, out int bytesConsumed, out bytesWritten);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.NeedMoreData, result);
        }

        /// <summary>
        /// Test to ensure that when given an empty Destination span, the encoder consume no input and write no output
        /// </summary>
        [Fact]
        public void Compress_WithEmptyDestination()
        {
            string testFile = UncompressedTestFile();
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] empty = new byte[0];
            ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(correctUncompressedBytes);
            Span<byte> destination = new Span<byte>(empty);

            Assert.False(BrotliEncoder.TryCompress(source, destination, out int bytesWritten), "TryCompress completed successfully but should have failed due to too short of a destination array");
            Assert.Equal(0, bytesWritten);

            BrotliEncoder encoder = default;
            var result = encoder.Compress(source, destination, out int bytesConsumed, out bytesWritten, false);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.DestinationTooSmall, result);

            result = encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.DestinationTooSmall, result);
        }

        /// <summary>
        /// Test to ensure that when given an empty source span, the decoder will consume no input and write no output (until the finishing block)
        /// </summary>
        [Fact]
        public void Compress_WithEmptySource()
        {
            byte[] sourceBytes = new byte[0];
            byte[] destinationBytes = new byte[100000];
            ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(sourceBytes);
            Span<byte> destination = new Span<byte>(destinationBytes);

            Assert.True(BrotliEncoder.TryCompress(source, destination, out int bytesWritten));
            // The only byte written should be the Brotli end of stream byte which varies based on the window/quality
            Assert.Equal(1, bytesWritten);

            BrotliEncoder encoder = default;
            var result = encoder.Compress(source, destination, out int bytesConsumed, out bytesWritten, false);
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.Done, result);

            result = encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock: true);
            Assert.Equal(1, bytesWritten);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(OperationStatus.Done, result);
        }

        /// <summary>
        /// Test that the decoder can handle partial chunks of flushed encoded data sent from the BrotliEncoder
        /// </summary>
        [Fact]
        public void RoundTrip_Chunks()
        {
            int chunkSize = 100;
            int totalSize = 20000;
            BrotliEncoder encoder = default;
            BrotliDecoder decoder = default;
            for (int i = 0; i < totalSize; i += chunkSize)
            {
                byte[] uncompressed = new byte[chunkSize];
                Random.Shared.NextBytes(uncompressed);
                byte[] compressed = new byte[BrotliEncoder.GetMaxCompressedLength(chunkSize)];
                byte[] decompressed = new byte[chunkSize];
                var uncompressedSpan = new ReadOnlySpan<byte>(uncompressed);
                var compressedSpan = new Span<byte>(compressed);
                var decompressedSpan = new Span<byte>(decompressed);

                int totalWrittenThisIteration = 0;
                var compress = encoder.Compress(uncompressedSpan, compressedSpan, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
                totalWrittenThisIteration += bytesWritten;
                compress = encoder.Flush(compressedSpan.Slice(bytesWritten), out bytesWritten);
                totalWrittenThisIteration += bytesWritten;

                var res = decoder.Decompress(compressedSpan.Slice(0, totalWrittenThisIteration), decompressedSpan, out int decompressbytesConsumed, out int decompressbytesWritten);
                Assert.Equal(totalWrittenThisIteration, decompressbytesConsumed);
                Assert.Equal(bytesConsumed, decompressbytesWritten);
                Assert.Equal<byte>(uncompressed, decompressedSpan.ToArray());
            }
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void ReadFully(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = File.ReadAllBytes(CompressedTestFile(testFile));
            byte[] actualUncompressedBytes = new byte[correctUncompressedBytes.Length + 10000];
            ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(compressedBytes);
            Span<byte> destination = new Span<byte>(actualUncompressedBytes);
            Assert.True(BrotliDecoder.TryDecompress(source, destination, out int bytesWritten), "TryDecompress did not complete successfully");
            Assert.Equal(correctUncompressedBytes.Length, bytesWritten);
            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes.AsSpan(0, correctUncompressedBytes.Length).ToArray());
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void ReadWithState(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = File.ReadAllBytes(CompressedTestFile(testFile));
            byte[] actualUncompressedBytes = new byte[correctUncompressedBytes.Length];
            Decompress_WithState(compressedBytes, actualUncompressedBytes);

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes);
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void ReadWithoutState(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = File.ReadAllBytes(CompressedTestFile(testFile));
            byte[] actualUncompressedBytes = new byte[correctUncompressedBytes.Length];
            Decompress_WithoutState(compressedBytes, actualUncompressedBytes);

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes);
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void WriteFully(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = new byte[BrotliEncoder.GetMaxCompressedLength(correctUncompressedBytes.Length)];
            byte[] actualUncompressedBytes = new byte[BrotliEncoder.GetMaxCompressedLength(correctUncompressedBytes.Length)];

            Span<byte> destination = new Span<byte>(compressedBytes);

            Assert.True(BrotliEncoder.TryCompress(correctUncompressedBytes, destination, out int bytesWritten));
            Assert.True(BrotliDecoder.TryDecompress(destination, actualUncompressedBytes, out bytesWritten));
            Assert.Equal(correctUncompressedBytes.Length, bytesWritten);

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes.AsSpan(0, correctUncompressedBytes.Length).ToArray());
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void WriteWithState(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = new byte[BrotliEncoder.GetMaxCompressedLength(correctUncompressedBytes.Length)];
            byte[] actualUncompressedBytes = new byte[correctUncompressedBytes.Length];

            Compress_WithState(correctUncompressedBytes, compressedBytes);
            Decompress_WithState(compressedBytes, actualUncompressedBytes);

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes);
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void WriteWithoutState(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = new byte[BrotliEncoder.GetMaxCompressedLength(correctUncompressedBytes.Length)];
            byte[] actualUncompressedBytes = new byte[correctUncompressedBytes.Length];

            Compress_WithoutState(correctUncompressedBytes, compressedBytes);
            Decompress_WithoutState(compressedBytes, actualUncompressedBytes);

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes);
        }

        [Theory]
        [OuterLoop("Full set of UncompressedTestFiles takes around 15s to run")]
        [MemberData(nameof(UncompressedTestFiles))]
        public void WriteStream(string testFile)
        {
            byte[] correctUncompressedBytes = File.ReadAllBytes(testFile);
            byte[] compressedBytes = Compress_Stream(correctUncompressedBytes, CompressionLevel.Optimal).ToArray();
            byte[] actualUncompressedBytes = Decompress_Stream(compressedBytes).ToArray();

            Assert.Equal<byte>(correctUncompressedBytes, actualUncompressedBytes);
        }

        [Theory]
        [InlineData(1000, CompressionLevel.Fastest)]
        [InlineData(1000, CompressionLevel.Optimal)]
        [InlineData(1000, CompressionLevel.NoCompression)]
        public static void Roundtrip_WriteByte_ReadByte_Success(int totalLength, CompressionLevel level)
        {
            byte[] correctUncompressedBytes = Enumerable.Range(0, totalLength).Select(i => (byte)i).ToArray();

            byte[] compressedBytes;
            using (var ms = new MemoryStream())
            {
                var bs = new BrotliStream(ms, level);
                foreach (byte b in correctUncompressedBytes)
                {
                    bs.WriteByte(b);
                }
                bs.Dispose();
                compressedBytes = ms.ToArray();
            }

            byte[] decompressedBytes = new byte[correctUncompressedBytes.Length];
            using (var ms = new MemoryStream(compressedBytes))
            using (var bs = new BrotliStream(ms, CompressionMode.Decompress))
            {
                for (int i = 0; i < decompressedBytes.Length; i++)
                {
                    int b = bs.ReadByte();
                    Assert.InRange(b, 0, 255);
                    decompressedBytes[i] = (byte)b;
                }
                Assert.Equal(-1, bs.ReadByte());
                Assert.Equal(-1, bs.ReadByte());
            }

            Assert.Equal<byte>(correctUncompressedBytes, decompressedBytes);
        }

        private static void Compress_WithState(ReadOnlySpan<byte> input, Span<byte> output)
        {
            BrotliEncoder encoder = default;
            while (!input.IsEmpty && !output.IsEmpty)
            {
                encoder.Compress(input, output, out int bytesConsumed, out int written, isFinalBlock: false);
                input = input.Slice(bytesConsumed);
                output = output.Slice(written);
            }
            encoder.Compress(ReadOnlySpan<byte>.Empty, output, out int bytesConsumed2, out int bytesWritten, isFinalBlock: true);
        }

        private static void Decompress_WithState(ReadOnlySpan<byte> input, Span<byte> output)
        {
            BrotliDecoder decoder = default;
            while (!input.IsEmpty && !output.IsEmpty)
            {
                decoder.Decompress(input, output, out int bytesConsumed, out int written);
                input = input.Slice(bytesConsumed);
                output = output.Slice(written);
            }
        }

        private static void Compress_WithoutState(ReadOnlySpan<byte> input, Span<byte> output)
        {
            BrotliEncoder.TryCompress(input, output, out int bytesWritten);
        }

        private static void Decompress_WithoutState(ReadOnlySpan<byte> input, Span<byte> output)
        {
            BrotliDecoder.TryDecompress(input, output, out int bytesWritten);
        }

        private static MemoryStream Compress_Stream(ReadOnlySpan<byte> input, CompressionLevel compressionLevel)
        {
            using (var inputStream = new MemoryStream(input.ToArray()))
            {
                var outputStream = new MemoryStream();
                var compressor = new BrotliStream(outputStream, compressionLevel, true);
                inputStream.CopyTo(compressor);
                compressor.Dispose();
                return outputStream;
            }
        }

        private static MemoryStream Decompress_Stream(ReadOnlySpan<byte> input)
        {
            using (var inputStream = new MemoryStream(input.ToArray()))
            {
                var outputStream = new MemoryStream();
                var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress, true);
                decompressor.CopyTo(outputStream);
                decompressor.Dispose();
                return outputStream;
            }
        }
    }
}
