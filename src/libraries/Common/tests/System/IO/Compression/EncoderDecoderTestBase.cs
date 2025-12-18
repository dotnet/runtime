
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Tests;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression
{
    public abstract class EncoderDecoderTestBase
    {
        private const int ReasonablySizedData = 100 * 1024; // 100 KB
        protected virtual bool SupportsDictionaries => false;
        protected virtual bool SupportsReset => false;

        protected virtual string WindowLogParamName => "windowLog";
        protected virtual string InputLengthParamName => "inputLength";

        protected abstract int ValidQuality { get; }
        protected abstract int ValidWindowLog { get; }
        protected abstract int InvalidQualityTooLow { get; }
        protected abstract int InvalidQualityTooHigh { get; }
        protected abstract int InvalidWindowLogTooLow { get; }
        protected abstract int InvalidWindowLogTooHigh { get; }

        public IEnumerable<int> InvalidWindowLogsTestData =>
            [
                int.MinValue,
                InvalidWindowLogTooLow,
                InvalidWindowLogTooHigh,
                int.MaxValue
            ];

        public IEnumerable<int> InvalidQualitiesTestData =>
            [
                int.MinValue,
                InvalidQualityTooLow,
                InvalidQualityTooHigh,
                int.MaxValue
            ];

        public static IEnumerable<object[]> BooleanTestData()
        {
            yield return new object[] { false };
            yield return new object[] { true };
        }

        public static IEnumerable<object[]> GetRoundTripTestData()
        {
            foreach (int quality in new[] { 1, 2, 3 })
            {
                foreach (bool useDictionary in new[] { true, false })
                {
                    foreach (bool staticEncode in new[] { true, false })
                    {
                        foreach (bool staticDecode in new[] { true, false })
                        {
                            yield return new object[] { quality, useDictionary, staticEncode, staticDecode };
                        }
                    }
                }
            }
        }

        public abstract class EncoderAdapter : IDisposable
        {
            public abstract OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock);
            public abstract OperationStatus Flush(Span<byte> destination, out int bytesWritten);
            public abstract void Reset();
            public abstract void Dispose();
        }

        public abstract class DecoderAdapter : IDisposable
        {
            public abstract OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten);
            public abstract void Reset();
            public abstract void Dispose();
        }

        public abstract class DictionaryAdapter : IDisposable
        {
            public abstract void Dispose();
        }

        protected abstract EncoderAdapter CreateEncoder();
        protected abstract EncoderAdapter CreateEncoder(int quality, int windowLog);
        protected abstract EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog);
        protected abstract DecoderAdapter CreateDecoder();
        protected abstract DecoderAdapter CreateDecoder(DictionaryAdapter dictionary);
        protected abstract DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> data, int quality);

        protected abstract bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten);
        protected abstract bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog);
        protected abstract bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog);
        protected abstract bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten);
        protected abstract bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary);

        protected abstract long GetMaxCompressedLength(long inputSize);


        [Fact]
        public void InvalidQuality_ThrowsArgumentOutOfRangeException()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[GetMaxCompressedLength(input.Length)];

            foreach (int quality in InvalidQualitiesTestData)
            {
                Assert.Throws<ArgumentOutOfRangeException>("quality", () => CreateEncoder(quality, ValidWindowLog));
                Assert.Throws<ArgumentOutOfRangeException>("quality", () => TryCompress(input, output, out _, quality: quality, windowLog: ValidWindowLog));
            }
        }

        [Fact]
        public void InvalidWindowLog_ThrowsArgumentOutOfRangeException()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[GetMaxCompressedLength(input.Length)];

            foreach (int windowLog in InvalidWindowLogsTestData)
            {
                Assert.Throws<ArgumentOutOfRangeException>(WindowLogParamName, () => CreateEncoder(ValidQuality, windowLog));
                Assert.Throws<ArgumentOutOfRangeException>(WindowLogParamName, () => TryCompress(input, output, out _, quality: ValidQuality, windowLog: windowLog));
            }
        }

        [Fact]
        public void TryCompress_WithEmptySource_ReturnsTrue()
        {
            ReadOnlySpan<byte> source = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            bool result = TryCompress(source, destination, out int bytesWritten);

            Assert.True(result);
            Assert.NotEqual(0, bytesWritten);
        }


        [Fact]
        public void TryCompress_WithValidInput_CompressesData()
        {
            byte[] input = CreateTestData(ReasonablySizedData);
            byte[] output = new byte[GetMaxCompressedLength(input.Length)];

            bool result = TryCompress(input, output, out int bytesWritten);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < input.Length); // Should compress to smaller size
        }

        [Fact]
        public void TryCompress_WithQualityAndWindowLog_CompressesData()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[GetMaxCompressedLength(input.Length)];

            bool result = TryCompress(input, output, out int bytesWritten, quality: ValidQuality, windowLog: ValidWindowLog);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void TryCompress_WithDictionary_WithQuality_Succeeds()
        {
            if (!SupportsDictionaries)
                return;

            byte[] input = CreateTestData();
            byte[] output = new byte[GetMaxCompressedLength(input.Length)];
            using DictionaryAdapter dictionary = CreateDictionary(CreateSampleDictionary(), ValidQuality);

            bool result = TryCompress(input, output, out int bytesWritten, dictionary: dictionary, windowLog: ValidWindowLog);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void TryCompress_WithEmptyDestination_ReturnsFalse(bool useDictionary)
        {
            if (useDictionary && !SupportsDictionaries)
                return;

            Span<byte> destination = Span<byte>.Empty;
            ReadOnlySpan<byte> source = new byte[100];

            DictionaryAdapter? dictionary = useDictionary ? CreateDictionary(CreateSampleDictionary(), ValidQuality) : null;
            try
            {
                bool result = useDictionary
                    ? TryCompress(source, destination, out int bytesWritten, dictionary, ValidWindowLog)
                    : TryCompress(source, destination, out bytesWritten);

                Assert.False(result);
                Assert.Equal(0, bytesWritten);
            }
            finally
            {
                dictionary?.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void TryDecompress_WithEmptySource_ReturnsFalse(bool useDictionary)
        {
            if (useDictionary && !SupportsDictionaries)
                return;

            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            DictionaryAdapter? dictionary = useDictionary ? CreateDictionary(CreateSampleDictionary(), ValidQuality) : null;
            try
            {
                bool result = useDictionary
                    ? TryDecompress(emptySource, destination, out int bytesWritten, dictionary)
                    : TryDecompress(emptySource, destination, out bytesWritten);

                Assert.False(result);
                Assert.Equal(0, bytesWritten);
            }
            finally
            {
                dictionary?.Dispose();
            }
        }

        [Fact]
        public void TryDecompress_WithDictionary_NullDictionary_ThrowsArgumentNullException()
        {
            if (!SupportsDictionaries)
                return;

            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ArgumentNullException>("dictionary", () =>
                TryDecompress(source, destination, out _, null!));
        }

        [Fact]
        public void TryDecompress_WithEmptyDestination_ReturnsFalse()
        {
            Span<byte> destination = Span<byte>.Empty;
            Span<byte> source = new byte[100];

            Assert.True(TryCompress("This is a test content"u8, source, out int bytesWritten));
            source = source.Slice(0, bytesWritten);

            Assert.False(TryDecompress(source, destination, out bytesWritten), "TryDecompress completed successfully but should have failed due to too short of a destination array");
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void TryDecompress_RandomData_ReturnsFalse(bool useDictionary)
        {
            if (useDictionary && !SupportsDictionaries)
                return;

            Span<byte> source = new byte[100];
            Span<byte> destination = new byte[5 * source.Length];

            // deterministic random data that should not match any valid compressed format
            Random rng = new Random(42);
            rng.NextBytes(source);

            Assert.False(TryDecompress(source, destination, out int bytesWritten), "TryDecompress completed successfully but should have failed");
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void Compress_WithValidInput_CompressesData(bool explicitInit)
        {
            using var encoder = explicitInit ? CreateEncoder(ValidQuality, ValidWindowLog) : CreateEncoder();
            byte[] input = CreateTestData(100000);
            byte[] output = new byte[GetMaxCompressedLength(input.Length * 2)];

            OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, bytesConsumed);
            Assert.True(bytesWritten >= 0); // Buffered data may not be flushed yet
            if (bytesWritten == 0)
            {
                result = encoder.Flush(output.AsSpan(bytesWritten), out int flushBytesWritten);
                Assert.Equal(OperationStatus.Done, result);
                Assert.True(flushBytesWritten > 0);
            }

            result = encoder.Compress(input, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void Compress_WithEmptyDestination_ReturnsDestinationTooSmall()
        {
            using EncoderAdapter encoder = CreateEncoder(ValidQuality, ValidWindowLog);
            byte[] input = CreateTestData(100000);
            byte[] output = Array.Empty<byte>();

            OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            // Assert.Equal(0, bytesConsumed); // encoder may have buffered some data internally
            Assert.Equal(0, bytesWritten);

            result = encoder.Compress(input, output, out bytesConsumed, out bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void Encoder_Finalize()
        {
            {
                EncoderAdapter encoder = CreateEncoder(ValidQuality, ValidWindowLog);
                byte[] input = CreateTestData();
                byte[] output = new byte[GetMaxCompressedLength(input.Length)];

                encoder.Compress(input, output, out _, out _, isFinalBlock: true);
                // no Dispose()
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Fact]
        public void GetMaxCompressedLength_OutOfRangeInput_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(InputLengthParamName, () => GetMaxCompressedLength(-1));
        }

        [Fact]
        public void Decoder_Finalize()
        {
            {
                DecoderAdapter decoder = CreateDecoder();
                byte[] input = CreateTestData();
                byte[] output = new byte[GetMaxCompressedLength(input.Length)];
                Assert.True(TryCompress(input, output, out int compressedLength));

                decoder.Decompress(output.AsSpan(0, compressedLength), input, out _, out _);
                // no Dispose()
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Fact]
        public void Compress_AfterDispose_ThrowsObjectDisposedException()
        {
            EncoderAdapter encoder = CreateEncoder(ValidQuality, ValidWindowLog);
            encoder.Dispose();
            byte[] input = CreateTestData();
            byte[] output = new byte[100];

            Assert.Throws<ObjectDisposedException>(() => encoder.Compress(input, output, out _, out _, isFinalBlock: true));
        }

        [Fact]
        public void Flush_WithValidEncoder_Succeeds()
        {
            using EncoderAdapter encoder = CreateEncoder(ValidQuality, ValidWindowLog);
            byte[] output = new byte[1000];

            OperationStatus result = encoder.Flush(output, out int bytesWritten);

            Assert.True(result == OperationStatus.Done);
            Assert.True(bytesWritten >= 0);
        }

        [Fact]
        public void Decompress_AfterDispose_ThrowsObjectDisposedException()
        {
            DecoderAdapter decoder = CreateDecoder();
            decoder.Dispose();

            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ObjectDisposedException>(() =>
                decoder.Decompress(source, destination, out _, out _));
        }

        [Fact]
        public void Decompress_WithEmptySource_ReturnsNeedMoreData()
        {
            using DecoderAdapter decoder = CreateDecoder();

            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            OperationStatus result = decoder.Decompress(emptySource, destination, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            Assert.False(TryDecompress(emptySource, destination, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void Decompress_WithEmptyDestination_ReturnsDestinationTooSmall()
        {
            Span<byte> destination = Span<byte>.Empty;
            Span<byte> source = new byte[100];

            Assert.True(TryCompress("This is a test content"u8, source, out int bytesWritten));
            source = source.Slice(0, bytesWritten);

            using DecoderAdapter decoder = CreateDecoder();
            OperationStatus result = decoder.Decompress(source, destination, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }


        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            EncoderAdapter encoder = CreateEncoder(ValidQuality, ValidWindowLog);

            encoder.Dispose();
            encoder.Dispose();

            DecoderAdapter decoder = CreateDecoder();

            decoder.Dispose();
            decoder.Dispose();
        }


        [Fact]
        public void Reset_AfterDispose_ThrowsObjectDisposedException()
        {
            if (!SupportsReset)
                return;

            EncoderAdapter encoder = CreateEncoder();
            encoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => encoder.Reset());

            DecoderAdapter decoder = CreateDecoder();
            decoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => decoder.Reset());
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void Reset_AllowsReuseForMultipleCompressions(bool useDictionary)
        {
            if (useDictionary && !SupportsDictionaries || !SupportsReset)
                return;

            DictionaryAdapter? dictionary = useDictionary ? CreateDictionary(CreateSampleDictionary(), ValidQuality) : null;
            try
            {
                using var encoder = useDictionary
                    ? CreateEncoder(dictionary, ValidWindowLog)
                    : CreateEncoder(ValidQuality, ValidWindowLog);

                byte[] input = CreateTestData();
                byte[] output1 = new byte[GetMaxCompressedLength(input.Length)];
                byte[] output2 = new byte[GetMaxCompressedLength(input.Length)];

                // First compression
                OperationStatus result1 = encoder.Compress(input, output1, out int consumed1, out int written1, isFinalBlock: true);
                Assert.Equal(OperationStatus.Done, result1);
                Assert.Equal(input.Length, consumed1);
                Assert.True(written1 > 0);

                // Reset and compress again
                encoder.Reset();
                OperationStatus result2 = encoder.Compress(input, output2, out int consumed2, out int written2, isFinalBlock: true);
                Assert.Equal(OperationStatus.Done, result2);
                Assert.Equal(input.Length, consumed2);
                Assert.True(written2 > 0);

                Assert.Equal(output1, output2);
            }
            finally
            {
                dictionary?.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(BooleanTestData))]
        public void Reset_AllowsReuseForMultipleDecompressions(bool useDictionary)
        {
            if (useDictionary && !SupportsDictionaries || !SupportsReset)
                return;

            DictionaryAdapter? dictionary = useDictionary ? CreateDictionary(CreateSampleDictionary(), ValidQuality) : null;
            try
            {
                // First compress some data to have something to decompress
                byte[] input = CreateTestData();
                byte[] compressed = new byte[GetMaxCompressedLength(input.Length)];
                bool compressResult = useDictionary
                    ? TryCompress(input, compressed, out int compressedLength, dictionary, ValidWindowLog)
                    : TryCompress(input, compressed, out compressedLength);
                Assert.True(compressResult);

                // Resize compressed to actual length
                Array.Resize(ref compressed, compressedLength);

                using var decoder = useDictionary
                    ? CreateDecoder(dictionary)
                    : CreateDecoder();
                byte[] output1 = new byte[input.Length];
                byte[] output2 = new byte[input.Length];

                // First decompression
                OperationStatus result1 = decoder.Decompress(compressed, output1, out int consumed1, out int written1);
                Assert.Equal(OperationStatus.Done, result1);
                Assert.Equal(compressed.Length, consumed1);
                Assert.Equal(input.Length, written1);
                Assert.Equal(input, output1);

                // Reset and decompress again
                decoder.Reset();
                OperationStatus result2 = decoder.Decompress(compressed, output2, out int consumed2, out int written2);
                Assert.Equal(OperationStatus.Done, result2);
                Assert.Equal(compressed.Length, consumed2);
                Assert.Equal(input.Length, written2);
                Assert.Equal(input, output2);
            }
            finally
            {
                dictionary?.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(GetRoundTripTestData))]
        public void RoundTrip_SuccessfullyCompressesAndDecompresses(int quality, bool useDictionary, bool staticEncode, bool staticDecode)
        {
            if (useDictionary && !SupportsDictionaries)
                return;

            byte[] originalData = "Hello, World! This is a test string for compression and decompression."u8.ToArray();
            byte[] compressedBuffer = new byte[GetMaxCompressedLength(originalData.Length)];

            DictionaryAdapter? dictionary = useDictionary ? CreateDictionary(CreateSampleDictionary(), quality) : null;
            try
            {
                int windowLog = 10;

                int bytesWritten;
                int bytesConsumed;

                // Compress
                if (staticEncode)
                {
                    bool result =
                        useDictionary
                        ? TryCompress(originalData, compressedBuffer, out bytesWritten, dictionary, windowLog)
                        : TryCompress(originalData, compressedBuffer, out bytesWritten, quality, windowLog);
                    bytesConsumed = originalData.Length;

                    Assert.True(result);
                }
                else
                {
                    using var encoder = useDictionary ? CreateEncoder(dictionary, windowLog) : CreateEncoder(quality, windowLog);

                    OperationStatus compressResult = encoder.Compress(originalData, compressedBuffer, out bytesConsumed, out bytesWritten, true);
                    Assert.Equal(OperationStatus.Done, compressResult);
                }

                Span<byte> compressedData = compressedBuffer.AsSpan(0, bytesWritten);

                byte[] decompressedBuffer = new byte[originalData.Length];

                Assert.Equal(originalData.Length, bytesConsumed);
                Assert.True(bytesWritten > 0);
                int compressedLength = bytesWritten;

                // Decompress
                if (staticDecode)
                {
                    bool result =
                        useDictionary
                        ? TryDecompress(compressedData, decompressedBuffer, out bytesWritten, dictionary)
                        : TryDecompress(compressedData, decompressedBuffer, out bytesWritten);
                    bytesConsumed = compressedLength;

                    Assert.True(result);
                }
                else
                {
                    using var decoder = useDictionary ? CreateDecoder(dictionary) : CreateDecoder();

                    OperationStatus decompressResult = decoder.Decompress(compressedBuffer.AsSpan(0, compressedLength), decompressedBuffer, out bytesConsumed, out bytesWritten);

                    Assert.Equal(OperationStatus.Done, decompressResult);
                }

                Assert.Equal(compressedLength, bytesConsumed);
                Assert.Equal(originalData.Length, bytesWritten);
                Assert.Equal(originalData, decompressedBuffer.AsSpan(0, bytesWritten));
            }
            finally
            {
                dictionary?.Dispose();
            }
        }

        [Fact]
        public void RoundTrip_Chunks()
        {
            int chunkSize = 100;
            int totalSize = 20000;
            using EncoderAdapter encoder = CreateEncoder();
            using DecoderAdapter decoder = CreateDecoder();
            for (int i = 0; i < totalSize; i += chunkSize)
            {
                byte[] uncompressed = new byte[chunkSize];
                Random.Shared.NextBytes(uncompressed);
                byte[] compressed = new byte[GetMaxCompressedLength(chunkSize)];
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

        public static byte[] CreateTestData(int size = 1000)
        {
            // Create test data of specified size
            byte[] data = new byte[size];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256); // Varying pattern
            }
            return data;
        }

        public static byte[] CreateSampleDictionary()
        {
            // Create a simple dictionary with some sample data
            return "a;owijfawoiefjawfafajzlf zfijf slifljeifa flejf;waiefjwaf"u8.ToArray();
        }
    }
}
