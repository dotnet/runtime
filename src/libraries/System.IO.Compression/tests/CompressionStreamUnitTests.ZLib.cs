// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression.Tests;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Compression
{
    public class ZLibStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZLibStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZLibStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZLibStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZLibStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) => new ZLibStream(stream, options, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((ZLibStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZLibTestData", Path.GetFileName(uncompressedPath) + ".z");

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StreamCorruption_IsDetected()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch("System.IO.Compression.UseStrictValidation", true);

                byte[] source = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
                var buffer = new byte[64];
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

                for (int byteToCorrupt = 0; byteToCorrupt < compressedData.Length; byteToCorrupt++)
                {
                    // corrupt the data
                    compressedData[byteToCorrupt]++;

                    using (var decompressedStream = new MemoryStream(compressedData))
                    {
                        using (Stream decompressor = CreateStream(decompressedStream, CompressionMode.Decompress))
                        {
                            Assert.Throws<InvalidDataException>(() =>
                            {
                                while (ZipFileTestBase.ReadAllBytes(decompressor, buffer, 0, buffer.Length) != 0);
                            });
                        }
                    }

                    // restore the data
                    compressedData[byteToCorrupt]--;
                }
            }).Dispose();
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
                                        while (ZipFileTestBase.ReadAllBytes(decompressor, buffer, 0, buffer.Length) != 0) { };
                                        break;

                                    case TestScenario.ReadAsync:
                                        while (await ZipFileTestBase.ReadAllBytesAsync(decompressor, buffer, 0, buffer.Length) != 0) { };
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

        [Theory]
        [MemberData(nameof(UncompressedTestFilesZLib))]
        public async void ZLibCompressionLevel_SizeInOrder(string testFile)
        {
            using var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);

            async Task<long> GetLengthAsync(int compressionLevel)
            {
                uncompressedStream.Position = 0;
                using var mms = new MemoryStream();
                using var compressor = new ZLibStream(mms, new ZLibCompressionOptions() { CompressionLevel = compressionLevel, CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly }, leaveOpen: false);
                await uncompressedStream.CopyToAsync(compressor);
                await compressor.FlushAsync();
                return mms.Length;
            }

            long level0 = await GetLengthAsync(0);
            long level1 = await GetLengthAsync(1);
            long level2 = await GetLengthAsync(2);
            long level3 = await GetLengthAsync(3);
            long level4 = await GetLengthAsync(4);
            long level5 = await GetLengthAsync(5);
            long level6 = await GetLengthAsync(6);
            long level7 = await GetLengthAsync(7);
            long level8 = await GetLengthAsync(8);
            long level9 = await GetLengthAsync(9);

            Assert.True(level1 <= level0);
            Assert.True(level2 <= level1);
            Assert.True(level3 <= level2);
            Assert.True(level4 <= level3);
            Assert.True(level5 <= level4);
            Assert.True(level6 <= level5);
            Assert.True(level7 <= level6);
            Assert.True(level8 <= level7);
            Assert.True(level9 <= level8);
        }
    }
}
