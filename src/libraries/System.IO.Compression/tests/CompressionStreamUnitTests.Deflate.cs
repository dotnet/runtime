// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Compression.Tests;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private const int DeflaterPoolCapacity = 8;

        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new DeflateStream(stream, level, leaveOpen);
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) => new DeflateStream(stream, options, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((DeflateStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("DeflateTestData", Path.GetFileName(uncompressedPath));

        [Fact]
        public void DeflaterPool_ReusesNativeStateForSequentialStreams()
        {
            const int Iterations = 16;
            byte[] input = Encoding.UTF8.GetBytes("Short-lived DeflateStream compression should reuse native state.");
            HashSet<object> states = new();

            byte[]? expectedCompressedData = null;
            for (int i = 0; i < Iterations; i++)
            {
                using var compressedData = new MemoryStream();
                object currentState;
                using (var compressor = new DeflateStream(compressedData, CompressionLevel.Optimal, leaveOpen: true))
                {
                    currentState = GetZLibStreamHandle(compressor);
                    compressor.Write(input);
                }

                states.Add(currentState);

                byte[] compressedBytes = compressedData.ToArray();
                if (expectedCompressedData is null)
                {
                    expectedCompressedData = compressedBytes;
                }
                else
                {
                    Assert.Equal(expectedCompressedData, compressedBytes);
                }

                using var compressedInput = new MemoryStream(compressedBytes);
                using var decompressor = new DeflateStream(compressedInput, CompressionMode.Decompress);
                using var decompressedData = new MemoryStream();
                decompressor.CopyTo(decompressedData);
                Assert.Equal(input, decompressedData.ToArray());
            }

            Assert.True(states.Count < Iterations, "Sequential streams should reuse native state.");
        }

        [Fact]
        public void DeflaterPool_UsesRequestedCompressionStrategy()
        {
            byte[] input = Enumerable.Range(0, 4096).Select(i => (byte)(i % 17)).ToArray();
            var defaultOptions = new ZLibCompressionOptions { CompressionLevel = 6 };
            var huffmanOptions = new ZLibCompressionOptions
            {
                CompressionLevel = 6,
                CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly
            };

            (object defaultState, byte[] defaultData) = CompressWithHandle(input, defaultOptions);
            (object huffmanState, byte[] huffmanData) = CompressWithHandle(input, huffmanOptions);
            Assert.NotSame(defaultState, huffmanState);
            Assert.NotEqual(defaultData, huffmanData);

            Assert.Equal(defaultData, CompressWithHandle(input, defaultOptions).CompressedData);
            Assert.Equal(huffmanData, CompressWithHandle(input, huffmanOptions).CompressedData);
            Assert.Equal(input, Decompress(defaultData));
            Assert.Equal(input, Decompress(huffmanData));
        }

        [Fact]
        public void DeflaterPool_RotatesEvictionAcrossConfigurations()
        {
            byte[] input = Enumerable.Range(0, 4096).Select(i => (byte)(i % 17)).ToArray();
            int warmCount = DeflaterPoolCapacity * 2;
            DeflateStream[] warmCompressors = new DeflateStream[warmCount];
            MemoryStream[] warmOutputs = new MemoryStream[warmCount];
            SafeHandle[] warmStates = new SafeHandle[warmCount];

            for (int i = 0; i < warmCount; i++)
            {
                warmOutputs[i] = new MemoryStream();
                warmCompressors[i] = new DeflateStream(warmOutputs[i], CompressionLevel.Optimal, leaveOpen: true);
                warmStates[i] = GetZLibStreamHandle(warmCompressors[i]);
            }

            for (int i = 0; i < warmCompressors.Length; i++)
            {
                warmCompressors[i].Dispose();
                warmOutputs[i].Dispose();
            }

            for (int i = 0; i < DeflaterPoolCapacity; i++)
            {
                Assert.True(warmStates[i].IsClosed, "Evicted native state should be disposed.");
            }

            ZLibCompressionOptions[] options =
            [
                new() { CompressionLevel = 1, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 1, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 13 },
                new() { CompressionLevel = 2, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 0, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 3, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 4, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
                new() { CompressionLevel = 6, CompressionStrategy = ZLibCompressionStrategy.Filtered, WindowLog2 = 12 },
            ];

            object[] states = new object[DeflaterPoolCapacity];
            byte[][] compressedData = new byte[DeflaterPoolCapacity][];
            for (int i = 0; i < DeflaterPoolCapacity; i++)
            {
                (states[i], compressedData[i]) = CompressWithHandle(input, options[i]);
                Assert.Equal(input, Decompress(compressedData[i]));

                for (int j = 0; j < i; j++)
                {
                    Assert.NotSame(states[i], states[j]);
                }
            }

            for (int i = DeflaterPoolCapacity; i < warmCount; i++)
            {
                Assert.True(warmStates[i].IsClosed, "Replacing cached native state should dispose the evicted state.");
            }

            for (int i = 0; i < DeflaterPoolCapacity; i++)
            {
                (object repeatedState, byte[] repeatedData) = CompressWithHandle(input, options[i]);
                Assert.Same(states[i], repeatedState);
                Assert.Equal(compressedData[i], repeatedData);
            }
        }

        [Fact]
        public void DeflaterPool_KeepsSimultaneouslyRentedStatesDistinct()
        {
            const int BatchCount = 2;
            int compressorCount = DeflaterPoolCapacity * 2;

            for (int batch = 0; batch < BatchCount; batch++)
            {
                byte[][] input = new byte[compressorCount][];
                MemoryStream[] compressedData = new MemoryStream[compressorCount];
                DeflateStream[] compressors = new DeflateStream[compressorCount];
                object[] states = new object[compressorCount];

                for (int i = 0; i < compressorCount; i++)
                {
                    input[i] = Enumerable.Range(0, 2048).Select(j => (byte)(j + i + batch)).ToArray();
                    compressedData[i] = new MemoryStream();
                    compressors[i] = new DeflateStream(compressedData[i], CompressionLevel.Optimal, leaveOpen: true);
                    states[i] = GetZLibStreamHandle(compressors[i]);
                    compressors[i].Write(input[i]);
                }

                for (int i = 0; i < compressorCount; i++)
                {
                    for (int j = i + 1; j < compressorCount; j++)
                    {
                        Assert.NotSame(states[i], states[j]);
                    }

                    compressors[i].Dispose();
                }

                for (int i = 0; i < compressorCount; i++)
                {
                    compressedData[i].Position = 0;
                    using var decompressor = new DeflateStream(compressedData[i], CompressionMode.Decompress);
                    using var decompressedData = new MemoryStream();
                    decompressor.CopyTo(decompressedData);
                    Assert.Equal(input[i], decompressedData.ToArray());
                    compressedData[i].Dispose();
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DeflaterPool_ReusesStateAfterDestinationFailure(bool useAsync)
        {
            byte[] input = new byte[32 * 1024];
            new Random(1).NextBytes(input);
            DeflateStream[] activeCompressors = new DeflateStream[DeflaterPoolCapacity];
            MemoryStream[] activeOutputs = new MemoryStream[DeflaterPoolCapacity];

            for (int i = 0; i < DeflaterPoolCapacity; i++)
            {
                activeOutputs[i] = new MemoryStream();
                activeCompressors[i] = new DeflateStream(activeOutputs[i], CompressionLevel.Optimal, leaveOpen: true);
            }

            var failingDestination = new ThrowsAfterNWritesStream(writesAllowedBeforeThrow: 0);
            var failingCompressor = new DeflateStream(failingDestination, CompressionLevel.Optimal, leaveOpen: true);
            object failedState = GetZLibStreamHandle(failingCompressor);

            if (useAsync)
            {
                await Assert.ThrowsAsync<IOException>(() => failingCompressor.WriteAsync(input).AsTask());
                await Assert.ThrowsAsync<IOException>(async () => await failingCompressor.DisposeAsync());
            }
            else
            {
                Assert.Throws<IOException>(() => failingCompressor.Write(input));
                Assert.Throws<IOException>(() => failingCompressor.Dispose());
            }

            Assert.True(failingDestination.DidThrow);
            failingDestination.StopThrowing();

            using (var compressedData = new MemoryStream())
            {
                object reusedState;
                using (var compressor = new DeflateStream(compressedData, CompressionLevel.Optimal, leaveOpen: true))
                {
                    reusedState = GetZLibStreamHandle(compressor);
                    if (useAsync)
                    {
                        await compressor.WriteAsync(input);
                    }
                    else
                    {
                        compressor.Write(input);
                    }
                }

                Assert.Same(failedState, reusedState);
                Assert.Equal(input, Decompress(compressedData.ToArray()));
            }

            for (int i = 0; i < DeflaterPoolCapacity; i++)
            {
                activeCompressors[i].Dispose();
                activeOutputs[i].Dispose();
            }
        }

        private static SafeHandle GetZLibStreamHandle(DeflateStream stream)
        {
            FieldInfo deflaterField = typeof(DeflateStream).GetField("_deflater", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object deflater = deflaterField.GetValue(stream)!;
            FieldInfo stateField = deflater.GetType().GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object state = stateField.GetValue(deflater)!;
            FieldInfo streamField = state.GetType().GetField("_stream", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return Assert.IsAssignableFrom<SafeHandle>(streamField.GetValue(state));
        }

        private static (object StreamHandle, byte[] CompressedData) CompressWithHandle(byte[] input, ZLibCompressionOptions options)
        {
            using var compressedData = new MemoryStream();
            object state;
            using (var compressor = new DeflateStream(compressedData, options, leaveOpen: true))
            {
                state = GetZLibStreamHandle(compressor);
                compressor.Write(input);
            }

            return (state, compressedData.ToArray());
        }

        private static byte[] Decompress(byte[] compressedData)
        {
            using var compressedInput = new MemoryStream(compressedData);
            using var decompressor = new DeflateStream(compressedInput, CompressionMode.Decompress);
            using var decompressedData = new MemoryStream();
            decompressor.CopyTo(decompressedData);
            return decompressedData.ToArray();
        }

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
            using (LocalMemoryStream baseStream = await LocalMemoryStream.ReadAppFileAsync(fileName))
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
                                        while (ZipFileTestBase.ReadAllBytes(decompressor, buffer, 0, buffer.Length) != 0) { }
                                        break;

                                    case TestScenario.ReadAsync:
                                        while (await ZipFileTestBase.ReadAllBytesAsync(decompressor, buffer, 0, buffer.Length) != 0) { }
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

        [Fact]
        public void EmptyDeflateStream_WritesOutput()
        {
            using (var ms = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
                {
                    // Write nothing
                }

                // DeflateStream should now write output even for empty streams
                Assert.True(ms.Length > 0, "Empty DeflateStream should write finalization data");
            }
        }

        [Fact]
        public void EmptyStream_CanBeDecompressed()
        {
            // for compatibility reasons, an empty stream should be decompressible back to an empty stream
            using (var ms = new MemoryStream())
            {
                ms.Position = 0;

                using (var deflateStream = new DeflateStream(ms, CompressionMode.Decompress))
                using (var reader = new StreamReader(deflateStream))
                {
                    string result = reader.ReadToEnd();
                    Assert.Equal(string.Empty, result);
                }
            }
        }
    }
}
