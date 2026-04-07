// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit.Sdk;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZstandardStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZstandardStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level)
        {
            if (PlatformDetection.Is32BitProcess && level == CompressionLevel.SmallestSize)
            {
                // Zstandard smallest size requires too much working memory
                // (800+ MB) and causes intermittent allocation errors on 32-bit
                // processes in CI.
                level = CompressionLevel.Optimal;
            }

            return new ZstandardStream(stream, level);
        }
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen)
        {
            if (PlatformDetection.Is32BitProcess && level == CompressionLevel.SmallestSize)
            {
                // Zstandard smallest size requires too much working memory
                // (800+ MB) and causes intermittent allocation errors on 32-bit
                // processes in CI.
                level = CompressionLevel.Optimal;
            }

            return new ZstandardStream(stream, level, leaveOpen);
        }
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new ZstandardStream(stream, options == null ? null : new ZstandardCompressionOptions { Quality = options.CompressionLevel }, leaveOpen);

        public override Stream BaseStream(Stream stream) => ((ZstandardStream)stream).BaseStream;

        // The tests are relying on an implementation detail of ZstandardStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZstandardTestData", Path.GetFileName(uncompressedPath) + ".zst");

        [Fact]
        public void ZstandardStream_WithEncoder_CompressesData()
        {
            ZstandardEncoder encoder = new(5, 10);
            byte[] testData = ZstandardTestUtils.CreateTestData();
            using MemoryStream input = new(testData);
            using MemoryStream output = new();

            using (ZstandardStream compressionStream = new(output, encoder, leaveOpen: true))
            {
                input.CopyTo(compressionStream);
            }

            // Verify data was compressed
            Assert.True(output.Length > 0);
            Assert.True(output.Length < testData.Length);

            // Verify the encoder was reset, not disposed (should be reusable)
            using MemoryStream output2 = new();
            using (ZstandardStream compressionStream2 = new(output2, encoder, leaveOpen: true))
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
            byte[] testData = ZstandardTestUtils.CreateTestData();
            byte[] compressedData = new byte[ZstandardEncoder.GetMaxCompressedLength(testData.Length)];
            bool compressResult = ZstandardEncoder.TryCompress(testData, compressedData, out int compressedLength);
            Assert.True(compressResult);

            Array.Resize(ref compressedData, compressedLength);

            ZstandardDecoder decoder = new();
            using MemoryStream input = new(compressedData);
            using MemoryStream output = new();

            using (ZstandardStream decompressionStream = new(input, decoder, leaveOpen: true))
            {
                decompressionStream.CopyTo(output);
            }

            // Verify data was decompressed correctly
            Assert.Equal(testData, output.ToArray());

            // Verify the decoder was reset, not disposed (should be reusable)
            using MemoryStream output2 = new();
            using (ZstandardStream decompressionStream2 = new(input, decoder, leaveOpen: true))
            {
                input.Position = 0;
                decompressionStream2.CopyTo(output2);
            }

            Assert.Equal(testData, output2.ToArray());
            decoder.Dispose(); // Clean up
        }

        [Theory]
        [InlineData(true, -1)]
        [InlineData(false, -1)]
        [InlineData(true, 2)]
        [InlineData(false, 2)]
        public async Task ZstandardStream_SetSourceLength_SizeDiffers_InvalidDataException(bool async, long delta)
        {
            byte[] testData = ZstandardTestUtils.CreateTestData();
            using MemoryStream output = new();
            ZstandardStream compressionStream = new(output, CompressionLevel.Optimal);

            compressionStream.SetSourceLength(testData.Length + delta);
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                // for shorter source length, the error occurs during Write/WriteAsync
                // for longer source length, the error occurs as part of Dispose/DisposeAsync
                if (async)
                {
                    await compressionStream.WriteAsync(testData, 0, testData.Length);
                    await compressionStream.DisposeAsync();
                }
                else
                {
                    compressionStream.Write(testData, 0, testData.Length);
                    compressionStream.Dispose();
                }
            });
        }

        [Fact]
        public void ZstandardStream_DecompressInvalidData_InvalidDataException()
        {
            byte[] invalidCompressedData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using MemoryStream input = new(invalidCompressedData);
            using ZstandardStream decompressionStream = new(input, CompressionMode.Decompress);
            byte[] buffer = new byte[16];

            Assert.Throws<InvalidDataException>(() => decompressionStream.Read(buffer, 0, buffer.Length));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_Roundtrip_WithDictionary(bool async)
        {
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            byte[] testData = ZstandardTestUtils.CreateTestData(5000);

            using MemoryStream compressedStream = new();
            using (ZstandardStream compressionStream = new(compressedStream, CompressionMode.Compress, dictionary, leaveOpen: true))
            {
                if (async)
                {
                    await compressionStream.WriteAsync(testData, 0, testData.Length);
                }
                else
                {
                    compressionStream.Write(testData, 0, testData.Length);
                }
            }

            compressedStream.Position = 0;

            using MemoryStream decompressedStream = new();
            using (ZstandardStream decompressionStream = new(compressedStream, CompressionMode.Decompress, dictionary))
            {
                if (async)
                {
                    await decompressionStream.CopyToAsync(decompressedStream);
                }
                else
                {
                    decompressionStream.CopyTo(decompressedStream);
                }
            }

            Assert.Equal(testData, decompressedStream.ToArray());
        }

        [InlineData(TestScenario.ReadAsync)]
        [InlineData(TestScenario.Read)]
        [InlineData(TestScenario.Copy)]
        [InlineData(TestScenario.CopyAsync)]
        [InlineData(TestScenario.ReadByte)]
        [InlineData(TestScenario.ReadByteAsync)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StreamTruncation_IsDetected(TestScenario testScenario)
        {
            RemoteExecutor.Invoke(async (testScenario) =>
            {
                TestScenario scenario = Enum.Parse<TestScenario>(testScenario);

                AppContext.SetSwitch("System.IO.Compression.UseStrictValidation", true);

                var buffer = new byte[16];
                byte[] source = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
                byte[] compressedData;
                using (var compressed = new MemoryStream())
                using (Stream compressor = CreateStream(compressed, CompressionMode.Compress))
                {
                    foreach (byte b in source)
                    {
                        compressor.WriteByte(b);
                    }

                    compressor.Dispose();
                    compressedData = compressed.ToArray();
                }

                for (var i = 1; i <= compressedData.Length; i += 1)
                {
                    bool expectException = i < compressedData.Length;
                    using (var compressedStream = new MemoryStream(compressedData.Take(i).ToArray()))
                    {
                        using (Stream decompressor = CreateStream(compressedStream, CompressionMode.Decompress))
                        {
                            var decompressedStream = new MemoryStream();

                            try
                            {
                                switch (scenario)
                                {
                                    case TestScenario.Copy:
                                        decompressor.CopyTo(decompressedStream);
                                        break;

                                    case TestScenario.CopyAsync:
                                        await decompressor.CopyToAsync(decompressedStream);
                                        break;

                                    case TestScenario.Read:
                                        while (decompressor.Read(buffer, 0, buffer.Length) != 0) { }
                                        break;

                                    case TestScenario.ReadAsync:
                                        while (await decompressor.ReadAsync(buffer, 0, buffer.Length) != 0) { }
                                        break;

                                    case TestScenario.ReadByte:
                                        while (decompressor.ReadByte() != -1) { }
                                        break;

                                    case TestScenario.ReadByteAsync:
                                        while (await decompressor.ReadByteAsync() != -1) { }
                                        break;
                                }
                            }
                            catch (InvalidDataException e)
                            {
                                if (expectException)
                                    continue;

                                throw new XunitException($"An unexpected error occurred while decompressing data:{e}");
                            }

                            if (expectException)
                            {
                                throw new XunitException($"Truncated stream was decompressed successfully but exception was expected: length={i}/{compressedData.Length}");
                            }
                        }
                    }
                }
            }, testScenario.ToString()).Dispose();
        }

    }
}
