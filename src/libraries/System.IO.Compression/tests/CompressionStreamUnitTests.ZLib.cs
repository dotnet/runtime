// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Compression.Tests;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class ZLibStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZLibStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZLibStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new ZLibStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new ZLibStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((ZLibStream)stream).BaseStream;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZLibTestData", Path.GetFileName(uncompressedPath) + ".z");

        [ActiveIssue("https://github.com/dotnet/runtime/issues/47563")]
        [Fact]
        public void StreamCorruption_IsDetected()
        {
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
        }
    }
}
