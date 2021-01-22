// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// A derived MemoryStream that avoids MemoryStream's fast path in CopyTo
        /// that bypasses buffering.
        /// </summary>
        private class DerivedMemoryStream : MemoryStream
        {
        }

        [Fact]
        public async Task ConcatenatedEmptyGzipStreams()
        {
            const int copyToBufferSizeRequested = 0x8000;

            // we'll request a specific size buffer, but we cannot guarantee that's the size
            // that will be used since CopyTo will rent from the array pool
            // take advantage of this knowledge to find out what size it will actually use
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(copyToBufferSizeRequested);
            int actualBufferSize = rentedBuffer.Length;
            ArrayPool<byte>.Shared.Return(rentedBuffer);

            // use 3 buffers-full so that we can prime the stream with the first buffer-full,
            // test that CopyTo successfully flushes this at the beginning of the operation,
            // then populates the second buffer-full and reads its entirety despite every
            // payload being 0 length before it reads the final buffer-full.
            int minCompressedSize = 3 * actualBufferSize;

            using (Stream compressedStream = new DerivedMemoryStream())
            {
                using (var gz = new GZipStream(compressedStream, CompressionLevel.NoCompression, leaveOpen: true))
                {
                    // write one byte in order to allow us to prime the inflater buffer
                    gz.WriteByte(3);
                }

                while (compressedStream.Length < minCompressedSize)
                {
                    using (var gz = new GZipStream(compressedStream, CompressionLevel.NoCompression, leaveOpen: true))
                    {
                        gz.Write(Array.Empty<byte>());
                    }
                }

                compressedStream.Seek(0, SeekOrigin.Begin);
                using (Stream gz = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
                using (Stream decompressedData = new DerivedMemoryStream())
                {
                    // read one byte in order to fill the inflater bufffer before copy
                    Assert.Equal(3, gz.ReadByte());

                    gz.CopyTo(decompressedData, copyToBufferSizeRequested);
                    Assert.Equal(0, decompressedData.Length);
                }

                compressedStream.Seek(0, SeekOrigin.Begin);
                using (Stream gz = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
                using (Stream decompressedData = new DerivedMemoryStream())
                {
                    // read one byte in order to fill the inflater bufffer before copy
                    Assert.Equal(3, gz.ReadByte());

                    await gz.CopyToAsync(decompressedData, copyToBufferSizeRequested);
                    Assert.Equal(0, decompressedData.Length);
                }
            }
        }

        [Theory]
        [InlineData(1000, TestScenario.Read, 1000, 1)]
        [InlineData(1000, TestScenario.ReadByte, 0, 1)]
        [InlineData(1000, TestScenario.ReadAsync, 1000, 1)]
        [InlineData(1000, TestScenario.Copy, 1000, 1)]
        [InlineData(1000, TestScenario.CopyAsync, 1000, 1)]
        [InlineData(10, TestScenario.Read, 1000, 2000)]
        [InlineData(10, TestScenario.ReadByte, 0, 2000)]
        [InlineData(10, TestScenario.ReadAsync, 1000, 2000)]
        [InlineData(10, TestScenario.Copy, 1000, 2000)]
        [InlineData(10, TestScenario.CopyAsync, 1000, 2000)]
        [InlineData(2, TestScenario.Copy, 1000, 0x2000-30)]
        [InlineData(2, TestScenario.CopyAsync, 1000, 0x2000 - 30)]
        [InlineData(1000, TestScenario.Read, 1, 1)]
        [InlineData(1000, TestScenario.ReadAsync, 1, 1)]
        [InlineData(1000, TestScenario.Read, 1001 * 24, 1)]
        [InlineData(1000, TestScenario.ReadAsync, 1001 * 24, 1)]
        [InlineData(1000, TestScenario.Copy, 1001 * 24, 1)]
        [InlineData(1000, TestScenario.CopyAsync, 1001 * 24, 1)]
        public async Task ManyConcatenatedGzipStreams(int streamCount, TestScenario scenario, int bufferSize, int bytesPerStream)
        {
            await TestConcatenatedGzipStreams(streamCount, scenario, bufferSize, bytesPerStream);
        }

        [Theory]
        [OuterLoop("Tests take a very long time to complete")]
        [InlineData(400000, TestScenario.Read, 1000, 1)]
        [InlineData(400000, TestScenario.ReadAsync, 1000, 1)]
        [InlineData(400000, TestScenario.Copy, 1000, 1)]
        [InlineData(400000, TestScenario.CopyAsync, 1000, 1)]
        [InlineData(1000, TestScenario.Read, 1000, 20000)]
        [InlineData(1000, TestScenario.ReadByte, 0, 20000)]
        [InlineData(1000, TestScenario.ReadAsync, 1000, 9000)]
        [InlineData(1000, TestScenario.Read, 1, 9000)]
        [InlineData(1000, TestScenario.ReadAsync, 1, 9000)]
        [InlineData(1000, TestScenario.Read, 1001 * 24, 9000)]
        [InlineData(1000, TestScenario.ReadAsync, 1001 * 24, 9000)]
        [InlineData(1000, TestScenario.Copy, 1001 * 24, 9000)]
        [InlineData(1000, TestScenario.CopyAsync, 1001 * 24, 9000)]
        public async Task ManyManyConcatenatedGzipStreams(int streamCount, TestScenario scenario, int bufferSize, int bytesPerStream)
        {
            await TestConcatenatedGzipStreams(streamCount, scenario, bufferSize, bytesPerStream);
        }

        public enum TestScenario
        {
            ReadByte,
            Read,
            ReadAsync,
            Copy,
            CopyAsync
        }

        private async Task TestConcatenatedGzipStreams(int streamCount, TestScenario scenario, int bufferSize, int bytesPerStream = 1)
        {
            bool isCopy = scenario == TestScenario.Copy || scenario == TestScenario.CopyAsync;

            using (MemoryStream correctDecompressedOutput = new MemoryStream())
            // For copy scenarios use a derived MemoryStream to avoid MemoryStream's Copy optimization
            // that turns the Copy into a single Write passing the backing buffer
            using (MemoryStream compressedStream = isCopy ? new DerivedMemoryStream() : new MemoryStream())
            using (MemoryStream decompressorOutput = new MemoryStream())
            {
                for (int i = 0; i < streamCount; i++)
                {
                    using (var gz = new GZipStream(compressedStream, CompressionLevel.NoCompression, true))
                    {
                        for (int j = 0; j < bytesPerStream; j++)
                        {
                            byte b = (byte)((i * j) % 256);
                            gz.WriteByte(b);
                            correctDecompressedOutput.WriteByte(b);
                        }
                    }
                }
                compressedStream.Seek(0, SeekOrigin.Begin);

                var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);

                var bytes = new byte[bufferSize];
                bool finished = false;
                int retCount = 0, totalRead = 0;
                while (!finished)
                {
                    switch (scenario)
                    {
                        case TestScenario.ReadAsync:
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
                            break;
                        case TestScenario.ReadByte:
                            int b = decompressor.ReadByte();

                            if (b != -1)
                                decompressorOutput.WriteByte((byte)b);
                            else
                                finished = true;

                            break;
                        case TestScenario.Read:
                            retCount = decompressor.Read(bytes, 0, bufferSize);

                            if (retCount != 0)
                                decompressorOutput.Write(bytes, 0, retCount);
                            else
                                finished = true;

                            break;
                        case TestScenario.Copy:
                            decompressor.CopyTo(decompressorOutput, bufferSize);
                            finished = true;
                            break;
                        case TestScenario.CopyAsync:
                            await decompressor.CopyToAsync(decompressorOutput, bufferSize);
                            finished = true;
                            break;
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

        // https://stackoverflow.com/questions/9456563/gzipstream-doesnt-detect-corrupt-data-even-crc32-passes
        [Fact]
        public void StrictValidation()
        {
            var data = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            byte[] cmpData;

            // Compress
            using (var cmpStream = new MemoryStream())
            {
                using (var hgs = new GZipStream(cmpStream, CompressionMode.Compress))
                {
                    hgs.Write(data, 0, data.Length);
                }
                cmpData = cmpStream.ToArray();
            }

            int corruptBytesNotDetected = 0;
            int corruptBytesDetected = 0;

            // corrupt data byte by byte
            for (var byteToCorrupt = 0; byteToCorrupt < cmpData.Length; byteToCorrupt++)
            {
                // corrupt the data
                cmpData[byteToCorrupt]++;

                using (var decomStream = new MemoryStream(cmpData))
                {
                    using (var hgs = new GZipStream(decomStream, CompressionMode.Decompress, false, strictValidation: true))
                    {
                        using (var reader = new StreamReader(hgs))
                        {
                            try
                            {
                                string sampleOut = reader.ReadToEnd();

                                // if we get here, the corrupt data was not detected by GZipStream
                                // ... okay so long as the correct data is extracted
                                corruptBytesNotDetected++;
                            }
                            catch (InvalidDataException)
                            {
                                corruptBytesDetected++;
                            }
                        }
                    }
                }

                // restore the data
                cmpData[byteToCorrupt]--;
            }

            Assert.Equal(0, corruptBytesNotDetected);
            Assert.Equal(52, corruptBytesDetected);
        }

        [Fact]
        public void StrictValidation2()
        {
            var source = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            var codec = new (Func<Stream, Stream> compress, Func<Stream, Stream> decompress)[]
            {
               (
                    s => new System.IO.Compression.GZipStream(s, System.IO.Compression.CompressionLevel.Fastest),
                    s => new System.IO.Compression.GZipStream(s, CompressionMode.Decompress, false, strictValidation: true)
               )
            };
            string r = null;

            try
            {
                r = RoundTrip(source, codec[0], Truncate);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            Assert.Equal("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", r);
        }

        private static string RoundTrip(
         ReadOnlySpan<byte> source,
         (Func<Stream, Stream> compress, Func<Stream, Stream> decompress) codec,
         Func<ReadOnlyMemory<byte>, IEnumerable<(Stream input, bool? shouldBeOkay)>> peturbations)
        {
            byte[] data;
            using (var compressed = new MemoryStream())
            using (var gzip = codec.compress(compressed))
            {
                foreach (var b in source)
                {
                    gzip.WriteByte(b);
                }
                gzip.Dispose();
                data = compressed.ToArray();
            }

            var actual = new StringBuilder();
            foreach (var entry in peturbations(data))
            {
                try
                {
                    using (var input = entry.input)
                    using (var gzip = codec.decompress(input))
                    using (var roundtrip = new MemoryStream())
                    {
                        gzip.CopyTo(roundtrip);
                        var result = roundtrip.ToArray();
                    }
                    actual.Append("/");
                }
                catch (Exception)
                {
                    actual.Append("x");
                }
            }

            return actual.ToString();
        }

        private static IEnumerable<(Stream, bool? shouldBeLegal)> Truncate(ReadOnlyMemory<byte> source)
        {
            var data = source.ToArray();
            for (int i = 0; i < data.Length; i++)
            {
                yield return (
                    new MemoryStream(data, 0, i, writable: false),
                    CanByteBeManipulatedSafely(data.Length, i, truncated: true));
            }
        }

        private static bool? CanByteBeManipulatedSafely(int length, int index, bool truncated)
        {
            // https://tools.ietf.org/html/rfc1952.html#section-2.2
            switch (index)
            {
                case 0: // ID1
                case 1: // ID2
                case 2: // CM (always gzip)
                    return false;
                //FLG some flags values may be illegal but ignoring for now
                case 3:
                    return truncated ? false : (bool?)null;
                // MTIME
                case 4:
                case 5:
                case 6:
                case 7:
                    return truncated ? false : true;
                //XFL some flags values may be illegal but ignoring for now
                case 8:
                    return truncated ? false : (bool?)null;
                // OS
                case 9:
                    return truncated ? false : (bool?)null;
                default:
                    // footer
                    if (length > 18 && index > length - 4)
                    {
                        // isize - should be validated
                        return false;
                    }
                    else if (length > 18 && index > length - 8)
                    {
                        // CRC32 - should be validated - technically certain manipulations could be undetected
                        return false;
                    }
                    // depending on flags/xflags there may be header entries in here that are mutable
                    // however if FLG.FHCRC was set thyen they aren't, so just assum none can be changed
                    return false;
            }
        }


        private sealed class DerivedGZipStream : GZipStream
        {
            public bool ReadArrayInvoked = false, WriteArrayInvoked = false;
            internal DerivedGZipStream(Stream stream, CompressionMode mode) : base(stream, mode) { }
            internal DerivedGZipStream(Stream stream, CompressionMode mode, bool leaveOpen) : base(stream, mode, leaveOpen) { }

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
    }
}
