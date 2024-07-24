// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO.Compression.Tests;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

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

        [Theory]
        [MemberData(nameof(ZLibOptionsRoundTripTestData))]
        public async Task RoundTripWithOptions(string testFile, ZLibCompressionOptions options)
        {
            var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);
            var compressedStream = CompressTestFile(uncompressedStream, options);
            using var decompressor = new DeflateStream(compressedStream, mode: CompressionMode.Decompress);
            var decompressorOutput = new MemoryStream();
            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            bool finished = false;
            int retCount;
            while (!finished)
            {
                retCount = await decompressor.ReadAsync(bytes, 0, _bufferSize);

                if (retCount != 0)
                    await decompressorOutput.WriteAsync(bytes, 0, retCount);
                else
                    finished = true;
            }
            decompressor.Dispose();
            decompressorOutput.Position = 0;
            uncompressedStream.Position = 0;

            byte[] uncompressedStreamBytes = uncompressedStream.ToArray();
            byte[] decompressorOutputBytes = decompressorOutput.ToArray();

            Assert.Equal(uncompressedStreamBytes.Length, decompressorOutputBytes.Length);
            for (int i = 0; i < uncompressedStreamBytes.Length; i++)
            {
                Assert.Equal(uncompressedStreamBytes[i], decompressorOutputBytes[i]);
            }
        }

        private MemoryStream CompressTestFile(LocalMemoryStream testStream, ZLibCompressionOptions options)
        {
            var compressorOutput = new MemoryStream();
            using (var compressionStream = new DeflateStream(compressorOutput, options, leaveOpen: true))
            {
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = testStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    compressionStream.Write(buffer, 0, bytesRead);
                }
            }

            compressorOutput.Position = 0;
            return compressorOutput;
        }

        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        public async void DeflateCompression_SizeInOrder(string testFile)
        {
            using var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);

            async Task<long> GetLengthAsync(int compressionLevel)
            {
                uncompressedStream.Position = 0;
                using var mms = new MemoryStream();
                using var compressor = new DeflateStream(mms, new ZLibCompressionOptions() { CompressionLevel = compressionLevel, CompressionStrategy = ZLibCompressionStrategy.RunLengthEncoding }, leaveOpen: false);
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

            // Depending on the file type the compression level is not linearly affect the compressed size
            Assert.True(level1 <= level0);
            Assert.True(level2 <= level1);
            Assert.True(level3 <= level2);
            Assert.True(level4 <= level2);
            Assert.True(level5 <= level3);
            Assert.True(level6 <= level3);
            Assert.True(level8 <= level6);
            Assert.True(level9 <= level7);
        }
    }
}
