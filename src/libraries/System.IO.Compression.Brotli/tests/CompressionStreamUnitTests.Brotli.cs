// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new BrotliStream(stream, options == null ? null : new BrotliCompressionOptions() { Quality = options.CompressionLevel }, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((BrotliStream)stream).BaseStream;

        protected override bool FlushGuaranteesAllDataWritten => false;

        public static IEnumerable<object[]> UncompressedTestFilesBrotli()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "alice29.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "asyoulik.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "cp.html") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "fields.c") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "lcet10.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "plrabn12.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "sum") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "xargs.1") };
        }

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
            Assert.Throws<ArgumentOutOfRangeException>("inputSize", () => BrotliEncoder.GetMaxCompressedLength(2_146_959_482));
            Assert.InRange(BrotliEncoder.GetMaxCompressedLength(2_146_959_481), 0, int.MaxValue); // 2_146_959_481 produces int.MaxValue
            Assert.Equal(2, BrotliEncoder.GetMaxCompressedLength(0));
        }

        [Fact]
        public void DestinationBufferWithSizeEqualToMaxCompressedLength_ShouldAlwaysSucceed()
        {
            byte[] source = new byte[256000];
            var rng = new Random(1234);
            rng.NextBytes(source);

            int maxLength = BrotliEncoder.GetMaxCompressedLength(source.Length);
            var resultBuffer = new byte[maxLength];

            Assert.True(BrotliEncoder.TryCompress(source, resultBuffer, out int bytesWritten, quality: 5, window: 10));
            Assert.True(maxLength >= bytesWritten);
        }

        [Fact]
        public void InvalidBrotliCompressionQuality()
        {
            BrotliCompressionOptions options = new();

            Assert.Equal(4, options.Quality); // default value
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Quality = -1);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Quality = 12);
        }

        [Fact]
        public void InvalidBrotliCompressionWindowLog2()
        {
            BrotliCompressionOptions options = new();

            Assert.Equal(22, options.WindowLog2); // default value
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.WindowLog2 = -1);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.WindowLog2 = 9);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.WindowLog2 = 25);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(22)]
        [InlineData(24)]
        public void BrotliCompressionWindowLog2_RoundTrip(int windowLog2)
        {
            byte[] testData = new byte[10000];
            Random.Shared.NextBytes(testData);

            // Compress with specific window size
            byte[] compressed;
            using (var ms = new MemoryStream())
            {
                using (var compressor = new BrotliStream(ms, new BrotliCompressionOptions() { WindowLog2 = windowLog2 }, leaveOpen: true))
                {
                    compressor.Write(testData);
                }
                compressed = ms.ToArray();
            }

            // Decompress and verify
            byte[] decompressed;
            using (var ms = new MemoryStream(compressed))
            using (var decompressor = new BrotliStream(ms, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                decompressor.CopyTo(resultStream);
                decompressed = resultStream.ToArray();
            }

            Assert.Equal(testData.Length, decompressed.Length);
            Assert.Equal<byte>(testData, decompressed);
        }

        [Theory]
        [MemberData(nameof(UncompressedTestFilesBrotli))]
        public async Task BrotliCompressionQuality_SizeInOrder(string testFile)
        {
            using var uncompressedStream = await LocalMemoryStream.ReadAppFileAsync(testFile);

            async Task<long> GetLengthAsync(int compressionQuality)
            {
                uncompressedStream.Position = 0;
                using var mms = new MemoryStream();
                using var compressor = new BrotliStream(mms, new BrotliCompressionOptions() { Quality = compressionQuality });
                await uncompressedStream.CopyToAsync(compressor);
                await compressor.FlushAsync();
                await uncompressedStream.FlushAsync();
                return mms.Length;
            }

            long prev = await GetLengthAsync(0);
            for (int i = 1; i < 12; i++)
            {
                long cur = await GetLengthAsync(i);
                Assert.True(cur <= prev, $"Expected {cur} <= {prev} for quality {i}");
                prev = cur;
            }
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

        [Fact]
        public void InvalidBrotliDecompressionMaxWindowLog2()
        {
            BrotliDecompressionOptions options = new();

            Assert.Equal(24, options.MaxWindowLog2); // default value
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.MaxWindowLog2 = -1);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.MaxWindowLog2 = 9);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.MaxWindowLog2 = 31);
        }

        [Fact]
        public void BrotliDecompressionOptions_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>("decompressionOptions", () => new BrotliDecoder(null));
            Assert.Throws<ArgumentNullException>("decompressionOptions", () => new BrotliStream(new MemoryStream(), (BrotliDecompressionOptions)null));
            Assert.Throws<ArgumentNullException>("stream", () => new BrotliStream(null, new BrotliDecompressionOptions()));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(22)]
        [InlineData(24)]
        public void BrotliDecompressionOptions_StandardWindow_RoundTrips(int windowLog2)
        {
            byte[] testData = CreateWindowTestData();
            byte[] compressed = CompressWithWindowLog2(testData, windowLog2);

            Assert.Equal<byte>(testData, DecompressWithOptions(compressed, new BrotliDecompressionOptions { MaxWindowLog2 = windowLog2 }));
        }

        [Theory]
        [InlineData(25)]
        [InlineData(30)]
        public void BrotliLargeWindow_RequiresOptIn(int windowLog2)
        {
            byte[] testData = s_largeWindowPayload;
            byte[] compressed = CreateLargeWindowCompressed(windowLog2);

            // Large Window Brotli is not part of RFC 7932, so a decoder that did not opt in rejects the stream.
            Assert.Throws<InvalidOperationException>(() => DecompressWithOptions(compressed, decompressionOptions: null));

            Assert.Equal<byte>(testData, DecompressWithOptions(compressed, new BrotliDecompressionOptions { MaxWindowLog2 = windowLog2 }));

            // A window larger than the configured maximum is reported the same way as any other invalid stream.
            Assert.Throws<InvalidOperationException>(() => DecompressWithOptions(compressed, new BrotliDecompressionOptions { MaxWindowLog2 = windowLog2 - 1 }));
        }

        [Fact]
        public void BrotliDecompressionOptions_RejectsWindowLargerThanMaxWindowLog2()
        {
            byte[] testData = CreateWindowTestData();
            byte[] compressed = CompressWithWindowLog2(testData, 22);

            // A window larger than the configured maximum is reported the same way as any other invalid stream.
            Assert.Throws<InvalidOperationException>(() => DecompressWithOptions(compressed, new BrotliDecompressionOptions { MaxWindowLog2 = 10 }));

            Assert.Equal<byte>(testData, DecompressWithOptions(compressed, new BrotliDecompressionOptions { MaxWindowLog2 = 22 }));
        }

        [Fact]
        public void BrotliDecoder_LargeWindow_RoundTrips()
        {
            byte[] testData = s_largeWindowPayload;
            byte[] compressed = CreateLargeWindowCompressed(30);
            byte[] decompressed = new byte[testData.Length];

            using (BrotliDecoder decoder = new(new BrotliDecompressionOptions { MaxWindowLog2 = 30 }))
            {
                Assert.Equal(OperationStatus.Done, decoder.Decompress(compressed, decompressed, out int bytesConsumed, out int bytesWritten));
                Assert.Equal(compressed.Length, bytesConsumed);
                Assert.Equal(testData.Length, bytesWritten);
            }

            Assert.Equal<byte>(testData, decompressed);

            BrotliDecompressionOptions limitedOptions = new() { MaxWindowLog2 = 29 };
            using BrotliDecoder limitedDecoder = new(limitedOptions);
            Assert.Equal(OperationStatus.InvalidData, limitedDecoder.Decompress(compressed, new byte[testData.Length], out int limitedBytesConsumed, out int limitedBytesWritten));
            Assert.Equal(0, limitedBytesConsumed);
            Assert.Equal(0, limitedBytesWritten);
        }

        [Fact]
        public void BrotliDecoder_LargeWindowHeaderSplitAcrossCalls_ReportsNeedMoreData()
        {
            byte[] testData = s_largeWindowPayload;
            byte[] compressed = CreateLargeWindowCompressed(30);

            using BrotliDecoder decoder = new(new BrotliDecompressionOptions { MaxWindowLog2 = 30 });

            // The Large Window Brotli header spans two bytes, so one byte is not enough to determine the window size.
            Assert.Equal(OperationStatus.NeedMoreData, decoder.Decompress(compressed.AsSpan(0, 1), new byte[64], out int bytesConsumed, out int bytesWritten));
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            // Resubmitting the unconsumed header alongside the rest of the stream, through a small nonempty destination
            // that forces repeated DestinationTooSmall calls, must still complete and retain the validated window.
            byte[] decompressed = new byte[testData.Length];
            int totalConsumed = 0, totalWritten = 0;
            ReadOnlySpan<byte> remaining = compressed;
            OperationStatus status;

            do
            {
                int chunkSize = Math.Min(4, decompressed.Length - totalWritten);
                status = decoder.Decompress(remaining, decompressed.AsSpan(totalWritten, chunkSize), out bytesConsumed, out bytesWritten);
                remaining = remaining.Slice(bytesConsumed);
                totalConsumed += bytesConsumed;
                totalWritten += bytesWritten;
            } while (status == OperationStatus.DestinationTooSmall);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressed.Length, totalConsumed);
            Assert.Equal(testData.Length, totalWritten);
            Assert.Equal<byte>(testData, decompressed);
        }

        [Fact]
        public void BrotliDecoder_EmptyDestination_DoesNotBypassWindowValidation()
        {
            byte[] testData = s_largeWindowPayload;
            byte[] largeWindowCompressed = CreateLargeWindowCompressed(30);

            using BrotliDecoder decoder = new(new BrotliDecompressionOptions { MaxWindowLog2 = 25 });

            // An empty destination makes no decompression progress, so the source below is never actually consumed.
            byte[] unconsumedSource = { 0x00 };
            Assert.Equal(OperationStatus.DestinationTooSmall, decoder.Decompress(unconsumedSource, Array.Empty<byte>(), out int bytesConsumed, out int bytesWritten));
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            // Swapping in a stream whose window (30) exceeds the configured maximum (25) must still be rejected, even
            // though the previous call saw a header that would have been within range.
            Assert.Equal(OperationStatus.InvalidData, decoder.Decompress(largeWindowCompressed, new byte[testData.Length], out bytesConsumed, out bytesWritten));
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void BrotliStream_SingleByteLargeWindowEscape_ThrowsForBothConstructors()
        {
            // 0x11 starts the "Large Window Brotli" escape sequence. Without opting in, the native decoder rejects it
            // immediately rather than waiting for the second header byte.
            byte[] truncatedLargeWindowEscape = { 0x11 };

            Assert.Throws<InvalidOperationException>(() => Decompress_Stream(truncatedLargeWindowEscape));
            Assert.Throws<InvalidOperationException>(() => DecompressWithOptions(truncatedLargeWindowEscape, new BrotliDecompressionOptions()));
        }

        private static byte[] CreateWindowTestData()
        {
            // Deliberately smaller than the encoder input block size, so that declaring a large window does not make either
            // side allocate a buffer proportional to that window.
            return CreatePattern(8 * 1024);
        }

        private static byte[] CompressWithWindowLog2(byte[] input, int windowLog2)
        {
            using MemoryStream output = new();
            using (BrotliStream compressor = new(output, new BrotliCompressionOptions { WindowLog2 = windowLog2 }, leaveOpen: true))
            {
                compressor.Write(input);
            }

            return output.ToArray();
        }

        // BrotliEncoder only ever produces streams up to the standard 24-bit window (RFC 7932); it has no "Large Window
        // Brotli" support, and this test suite intentionally keeps it that way (see PR discussion on #135846). This
        // fixture was generated once, offline, by driving the native encoder directly (BrotliEncoderCompressStream with
        // BROTLI_PARAM_LARGE_WINDOW=1) to produce a genuine "Large Window Brotli" stream for s_largeWindowPayload.
        // Only byte[1] (the declared window value, 10-30, encoded in the low 6 bits) varies between window sizes; the
        // metablock payload for this tiny input is identical regardless of the configured window.
        private static readonly byte[] s_largeWindowPayload = CreatePattern(64);

        private static readonly byte[] s_largeWindowFixtureTemplate =
        {
            0x11, 0x1E, 0x7E, 0x00, 0x02, 0x00, 0x1F, 0x3E, 0x5D, 0x7C, 0x9B, 0xBA, 0xD9, 0xF8, 0x17, 0x36,
            0x55, 0x74, 0x93, 0xB2, 0xD1, 0xF0, 0x0F, 0x2E, 0x4D, 0x6C, 0x8B, 0xAA, 0xC9, 0xE8, 0x07, 0x26,
            0x45, 0x64, 0x83, 0xA2, 0xC1, 0xE0, 0xFF, 0x1E, 0x3D, 0x5C, 0x7B, 0x9A, 0xB9, 0xD8, 0xF7, 0x16,
            0x35, 0x54, 0x73, 0x92, 0xB1, 0xD0, 0xEF, 0x0E, 0x2D, 0x4C, 0x6B, 0x8A, 0xA9, 0xC8, 0xE7, 0x06,
            0x25, 0x44, 0x63, 0x82, 0xA1, 0x03
        };

        private static byte[] CreatePattern(int length)
        {
            byte[] data = new byte[length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i * 31);
            }

            return data;
        }

        private static byte[] CreateLargeWindowCompressed(int windowLog2)
        {
            byte[] compressed = (byte[])s_largeWindowFixtureTemplate.Clone();
            compressed[1] = (byte)windowLog2;
            return compressed;
        }

        private static byte[] DecompressWithOptions(byte[] compressed, BrotliDecompressionOptions decompressionOptions)
        {
            using MemoryStream input = new(compressed);
            using BrotliStream decompressor = decompressionOptions is null
                ? new BrotliStream(input, CompressionMode.Decompress)
                : new BrotliStream(input, decompressionOptions);
            using MemoryStream output = new();

            decompressor.CopyTo(output);
            return output.ToArray();
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
