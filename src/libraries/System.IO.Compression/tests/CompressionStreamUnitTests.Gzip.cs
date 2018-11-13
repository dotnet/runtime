// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class GzipStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new GZipStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((GZipStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");

        [Fact]
        public void Precancellation()
        {
            var ms = new MemoryStream();
            using (Stream compressor = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
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
        public void ConcatenatedGzipStreams()
        {
            using (MemoryStream concatStream = new MemoryStream())
            {
                using (var gz = new GZipStream(concatStream, CompressionLevel.NoCompression, true))
                using (var sw = new StreamWriter(gz))
                    sw.WriteLine("Stream 1");

                using (var gz = new GZipStream(concatStream, CompressionLevel.NoCompression, true))
                using (var sw = new StreamWriter(gz))
                    sw.WriteLine("Stream 2");

                new GZipStream(concatStream, CompressionLevel.NoCompression, true).Dispose();

                concatStream.Seek(0, SeekOrigin.Begin);
                using (var gz = new GZipStream(concatStream, CompressionMode.Decompress))
                using (var sr = new StreamReader(gz))
                {
                    Assert.Equal("Stream 1", sr.ReadLine());
                    Assert.Equal("Stream 2", sr.ReadLine());
                    Assert.Empty(sr.ReadToEnd());
                }
            }
        }

        [Theory]
        [InlineData(1000, false, false, 1000, 1)]
        [InlineData(1000, false, true, 0, 1)]
        [InlineData(1000, true, false, 1000, 1)]
        [InlineData(1000, false, false, 1, 1)]
        [InlineData(1000, true, false, 1, 1)]
        [InlineData(1000, false, false, 1001 * 24, 1)]
        [InlineData(1000, true, false, 1001 * 24, 1)]
        public async void ManyConcatenatedGzipStreams(int streamCount, bool useAsync, bool useReadByte, int bufferSize, int bytesPerStream)
        {
            await TestConcatenatedGzipStreams(streamCount, useAsync, useReadByte, bufferSize, bytesPerStream);
        }

        [Theory]
        [OuterLoop("Tests take a very long time to complete")]
        [InlineData(400000, false, false, 1000, 1)]
        [InlineData(400000, true, false, 1000, 1)]
        [InlineData(1000, false, false, 1000, 20000)]
        [InlineData(1000, false, true, 0, 20000)]
        [InlineData(1000, true, false, 1000, 9000)]
        [InlineData(1000, false, false, 1, 9000)]
        [InlineData(1000, true, false, 1, 9000)]
        [InlineData(1000, false, false, 1001 * 24, 9000)]
        [InlineData(1000, true, false, 1001 * 24, 9000)]
        public async void ManyManyConcatenatedGzipStreams(int streamCount, bool useAsync, bool useReadByte, int bufferSize, int bytesPerStream)
        {
            await TestConcatenatedGzipStreams(streamCount, useAsync, useReadByte, bufferSize, bytesPerStream);
        }

        private async Task TestConcatenatedGzipStreams(int streamCount, bool useAsync, bool useReadByte, int bufferSize, int bytesPerStream = 1)
        {
            using (var correctDecompressedOutput = new MemoryStream())
            using (var compressedStream = new MemoryStream())
            using (var decompressorOutput = new MemoryStream())
            {
                for (int i = 0; i < streamCount; i++)
                {
                    using (var gz = new GZipStream(compressedStream, CompressionLevel.NoCompression, true))
                    {
                        for (int j = 0; j < bytesPerStream; j++)
                        {
                            byte b = (byte)((i * j) % 256);
                            gz.WriteByte( b);
                            correctDecompressedOutput.WriteByte(b);
                        }
                    }
                }
                compressedStream.Seek(0, SeekOrigin.Begin);

                var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
                //Thread.Sleep(10000);

                var bytes = new byte[bufferSize];
                bool finished = false;
                int retCount = 0, totalRead = 0;
                while (!finished)
                {
                    if (useAsync)
                    {
                        try
                        {
                            retCount = await decompressor.ReadAsync(bytes, 0, bufferSize);
                            totalRead += retCount;
                            if (retCount != 0)
                                await decompressorOutput.WriteAsync(bytes, 0, retCount);
                            else
                                finished = true;
                        }
                        catch (Exception)
                        {
                            throw new Exception(retCount + " " + totalRead);
                        }
                    }
                    else if (useReadByte)
                    {
                        int b = decompressor.ReadByte();

                        if (b != -1)
                            decompressorOutput.WriteByte((byte)b);
                        else
                            finished = true;
                    }
                    else
                    {
                        retCount = decompressor.Read(bytes, 0, bufferSize);

                        if (retCount != 0)
                            decompressorOutput.Write(bytes, 0, retCount);
                        else
                            finished = true;
                    }
                }
                decompressor.Dispose();
                decompressorOutput.Position = 0;

                byte[] decompressorOutputBytes = decompressorOutput.ToArray();
                byte[] correctOutputBytes = correctDecompressedOutput.ToArray();

                Assert.Equal(correctOutputBytes.Length, decompressorOutputBytes.Length);
                for (int i = 0; i < correctOutputBytes.Length; i++)
                {
                    Assert.Equal(correctOutputBytes[i], decompressorOutputBytes[i]);
                }
            }
        }

        [Fact]
        public void DerivedStream_ReadWriteSpan_UsesReadWriteArray()
        {
            var ms = new MemoryStream();
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.Write(new Span<byte>(new byte[1]));
                Assert.True(compressor.WriteArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.Read(new Span<byte>(new byte[1]));
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Decompress, leaveOpen: true))
            {
                compressor.ReadAsync(new Memory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.ReadArrayInvoked);
            }
            ms.Position = 0;
            using (var compressor = new DerivedGZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                compressor.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])).AsTask().Wait();
                Assert.True(compressor.WriteArrayInvoked);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, false)]
        public async Task DisposeAsync_Flushes(bool derived, bool leaveOpen)
        {
            var ms = new MemoryStream();
            var gs = derived ?
                new DerivedGZipStream(ms, CompressionMode.Compress, leaveOpen) :
                new GZipStream(ms, CompressionMode.Compress, leaveOpen);
            gs.WriteByte(1);
            await gs.FlushAsync();

            long pos = ms.Position;
            gs.WriteByte(1);
            Assert.Equal(pos, ms.Position);

            await gs.DisposeAsync();
            Assert.InRange(ms.ToArray().Length, pos + 1, int.MaxValue);
            if (leaveOpen)
            {
                Assert.InRange(ms.Position, pos + 1, int.MaxValue);
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => ms.Position);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, false)]
        public async Task DisposeAsync_MultipleCallsAllowed(bool derived, bool leaveOpen)
        {
            using (var gs = derived ?
                new DerivedGZipStream(new MemoryStream(), CompressionMode.Compress, leaveOpen) :
                new GZipStream(new MemoryStream(), CompressionMode.Compress, leaveOpen))
            {
                await gs.DisposeAsync();
                await gs.DisposeAsync();
            }
        }

        private sealed class DerivedGZipStream : GZipStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedGZipStream(Stream stream, CompressionMode mode) : base(stream, mode) { }
            internal DerivedGZipStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

            public override int Read(byte[] array, int offset, int count)
            {
                ReadArrayInvoked = true;
                return base.Read(array, offset, count);
            }

            public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
            {
                ReadArrayInvoked = true;
                return base.ReadAsync(array, offset, count, cancellationToken);
            }

            public override void Write(byte[] array, int offset, int count)
            {
                WriteArrayInvoked = true;
                base.Write(array, offset, count);
            }

            public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
            {
                WriteArrayInvoked = true;
                return base.WriteAsync(array, offset, count, cancellationToken);
            }
        }
    }
}
