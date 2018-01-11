// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public class DeflateStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new DeflateStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new DeflateStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((DeflateStream)stream).BaseStream;
        public override bool FlushCompletes => false;
        public override bool FlushNoOps => true;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("DeflateTestData", Path.GetFileName(uncompressedPath));

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public override void Write_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Compress))
            {
                Assert.Throws<ArgumentNullException>(() => { if (useAsync) { decompressor.WriteAsync(null, 0, 0).Wait(); } else { decompressor.Write(null, 0, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], -1, 0).Wait(); } else { decompressor.Write(new byte[1], -1, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, -1).Wait(); } else { decompressor.Write(new byte[1], 0, -1); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 2).Wait(); } else { decompressor.Write(new byte[1], 0, 2); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });
                Assert.Throws<InvalidOperationException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, 1).Result : decompressor.Read(new byte[1], 0, 1));
                Assert.Throws<InvalidOperationException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                if (useAsync)
                { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); }
                else
                { decompressor.Write(new byte[1], 0, 1); }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public override void Read_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Decompress))
            {
                Assert.Throws<ArgumentNullException>(() => useAsync ? decompressor.ReadAsync(null, 0, 0).Result : decompressor.Read(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], -1, 0).Result : decompressor.Read(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, -1).Result : decompressor.Read(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 0, 2).Result : decompressor.Read(new byte[1], 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                Assert.Throws<InvalidOperationException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); } else { decompressor.Write(new byte[1], 0, 1); } });
                Assert.Throws<InvalidOperationException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });

                var data = new byte[1] { 42 };
                Assert.Equal(0, useAsync ? decompressor.ReadAsync(data, 0, 0).Result : decompressor.Read(data, 0, 0));
                Assert.Equal(42, data[0]);
            }
        }

        /// <summary>
        /// Test to pass gzipstream data to a deflatestream
        /// </summary>
        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        public async Task DecompressFailsWithRealGzStream(string uncompressedPath)
        {
            string fileName = Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");
            var baseStream = await LocalMemoryStream.readAppFileAsync(fileName);
            var zip = CreateStream(baseStream, CompressionMode.Decompress);
            int _bufferSize = 2048;
            var bytes = new byte[_bufferSize];
            Assert.Throws<InvalidDataException>(() => { zip.Read(bytes, 0, _bufferSize); });
            zip.Dispose();
        }
    }

    public class GzipStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level) => new GZipStream(stream, level);
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen) => new GZipStream(stream, level, leaveOpen);
        public override Stream BaseStream(Stream stream) => ((GZipStream)stream).BaseStream;
        public override bool FlushCompletes => false;
        public override bool FlushNoOps => true;
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("GZipTestData", Path.GetFileName(uncompressedPath) + ".gz");

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public override void Write_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Compress))
            {
                Assert.Throws<ArgumentNullException>(() => { if (useAsync) { decompressor.WriteAsync(null, 0, 0).Wait(); } else { decompressor.Write(null, 0, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], -1, 0).Wait(); } else { decompressor.Write(new byte[1], -1, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, -1).Wait(); } else { decompressor.Write(new byte[1], 0, -1); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 2).Wait(); } else { decompressor.Write(new byte[1], 0, 2); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });
                Assert.Throws<InvalidOperationException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, 1).Result : decompressor.Read(new byte[1], 0, 1));
                Assert.Throws<InvalidOperationException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                if (useAsync)
                { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); }
                else
                { decompressor.Write(new byte[1], 0, 1); }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public override void Read_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Decompress))
            {
                Assert.Throws<ArgumentNullException>(() => useAsync ? decompressor.ReadAsync(null, 0, 0).Result : decompressor.Read(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], -1, 0).Result : decompressor.Read(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, -1).Result : decompressor.Read(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 0, 2).Result : decompressor.Read(new byte[1], 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                Assert.Throws<InvalidOperationException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); } else { decompressor.Write(new byte[1], 0, 1); } });
                Assert.Throws<InvalidOperationException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });

                var data = new byte[1] { 42 };
                Assert.Equal(0, useAsync ? decompressor.ReadAsync(data, 0, 0).Result : decompressor.Read(data, 0, 0));
                Assert.Equal(42, data[0]);
            }
        }



        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public override async Task Dispose_FollowedByAsyncOperations(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();

            if (mode == CompressionMode.Compress)
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await compressor.WriteAsync(new byte[1], 0, 1));
            else
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await compressor.ReadAsync(new byte[1], 0, 1));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.FlushAsync());
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.CopyToAsync(new MemoryStream()));
        }
    }
}
