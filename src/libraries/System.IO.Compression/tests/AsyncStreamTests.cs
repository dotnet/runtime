// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public abstract class CompressionStreamAsyncTestBase
    {
        public abstract bool StripHeaders { get; }
        public abstract Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count);
        public abstract Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count);
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen);

        protected static string gzTestFile(string fileName) { return Path.Combine("GZTestData", fileName); }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full Framework Flush is a no-op.")]
        public async Task OverlappingFlushAsync_DuringFlushAsync()
        {
            byte[] buffer = null;
            string testFilePath = gzTestFile("GZTestDocument.pdf");
            using (var origStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            {
                buffer = origStream.ToArray();
            }

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var zip = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    writeStream.manualResetEvent.Set();
                    await WriteAsync(zip, buffer, 0, buffer.Length);
                    writeStream.manualResetEvent.Reset();
                    writeStream.WriteHit = false;
                    task = zip.FlushAsync();
                    Assert.True(writeStream.WriteHit);
                    Assert.Throws<InvalidOperationException>(() => { zip.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(100 * 500));
                }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full Framework Flush is a no-op.")]
        public async Task OverlappingFlushAsync_DuringWriteAsync()
        {
            byte[] buffer = null;
            string testFilePath = gzTestFile("GZTestDocument.pdf");
            using (var origStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            {
                buffer = origStream.ToArray();
            }

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var zip = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    task = WriteAsync(zip, buffer, 0, buffer.Length);
                    Assert.True(writeStream.WriteHit);
                    Assert.Throws<InvalidOperationException>(() => { zip.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(100 * 500));
                }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full Framework Flush is a no-op.")]
        public async Task OverlappingFlushAsync_DuringReadAsync()
        {
            byte[] buffer = new byte[32];
            string testFilePath = gzTestFile("GZTestDocument.pdf.gz");
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false, StripHeaders))
            using (var unzip = CreateStream(readStream, CompressionMode.Decompress, true))
            {
                Task task = null;
                try
                {
                    readStream.manualResetEvent.Reset();
                    readStream.ReadHit = false;
                    task = ReadAsync(unzip, buffer, 0, 32);
                    Assert.True(readStream.ReadHit);
                    Assert.Throws<InvalidOperationException>(() => { unzip.FlushAsync(); }); // "overlapping read"
                }
                finally
                {
                    // Unblock Async operations
                    readStream.manualResetEvent.Set();
                    // The original ReadAsync should be able to complete
                    Assert.True(task.Wait(100 * 500));
                }
            }
        }

        [Fact]
        public async Task OverlappingWriteAsync()
        {
            byte[] buffer = null;
            string testFilePath = gzTestFile("GZTestDocument.pdf");
            using (var origStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            {
                buffer = origStream.ToArray();
            }

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var zip = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    task = WriteAsync(zip, buffer, 0, buffer.Length);    // write needs to be bigger than the internal write buffer
                    Assert.Throws<InvalidOperationException>(() => { zip.WriteAsync(buffer, 32, 32); }); // "overlapping write"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(100 * 500));
                    Assert.True(writeStream.WriteHit);
                }
            }
        }

        [Fact]
        public async Task OverlappingReadAsync()
        {
            byte[] buffer = new byte[32];
            string testFilePath = gzTestFile("GZTestDocument.pdf.gz");
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false, StripHeaders))
            using (var unzip = CreateStream(readStream, CompressionMode.Decompress, true))
            {
                Task<int> task = null;
                try
                {
                    task = ReadAsync(unzip, buffer, 0, 32);
                    Assert.Throws<InvalidOperationException>(() => { ReadAsync(unzip, buffer, 0, 32); }); // "overlapping read"
                }
                finally
                {
                    // Unblock Async operations
                    readStream.manualResetEvent.Set();
                    // The original ReadAsync should be able to complete
                    Assert.True(task.Wait(100 * 500));
                    Assert.True(readStream.ReadHit);
                }
            }
        }

        [Fact]
        public async Task DecompressWorks()
        {
            var compareStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt"));
            var gzStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.txt.gz"));

            await DecompressAsync(compareStream, gzStream);
        }

        [Fact]
        public async Task DecompressWorksWithBinaryFile()
        {
            var compareStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.doc"));
            var gzStream = await LocalMemoryStream.readAppFileAsync(gzTestFile("GZTestDocument.doc.gz"));

            await DecompressAsync(compareStream, gzStream);
        }

        // Making this async since regular read/write are tested below
        private async Task DecompressAsync(MemoryStream compareStream, MemoryStream gzStream)
        {
            var sourceStream = StripHeaders ? StripHeaderAndFooter.Strip(gzStream) : gzStream;

            var ms = new MemoryStream();
            var zip = CreateStream(sourceStream, CompressionMode.Decompress);

            var Stream = new MemoryStream();

            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            bool finished = false;
            int retCount;
            while (!finished)
            {
                retCount = await ReadAsync(zip, bytes, 0, _bufferSize);

                if (retCount != 0)
                    await Stream.WriteAsync(bytes, 0, retCount);
                else
                    finished = true;
            }

            Stream.Position = 0;
            compareStream.Position = 0;

            byte[] compareArray = compareStream.ToArray();
            byte[] writtenArray = Stream.ToArray();

            Assert.Equal(compareArray.Length, writtenArray.Length);
            for (int i = 0; i < compareArray.Length; i++)
            {
                Assert.Equal(compareArray[i], writtenArray[i]);
            }
        }
    }

    public sealed class TaskDeflateStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => true;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) => ((DeflateStream)unzip).ReadAsync(buffer, offset, count);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) => ((DeflateStream)unzip).WriteAsync(buffer, offset, count);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
    }

    public sealed class TaskGZipStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => false;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) => ((GZipStream)unzip).ReadAsync(buffer, offset, count);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) => ((GZipStream)unzip).WriteAsync(buffer, offset, count);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
    }

    public sealed class BeginEndDeflateStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => true;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task<int>.Factory.FromAsync<byte[], int, int>(((DeflateStream)unzip).BeginRead, ((DeflateStream)unzip).EndRead, buffer, offset, count, null);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task.Factory.FromAsync<byte[], int, int>(((DeflateStream)unzip).BeginWrite, ((DeflateStream)unzip).EndWrite, buffer, offset, count, null);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
    }

    public sealed class BeginEndGZipStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => false;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task<int>.Factory.FromAsync<byte[], int, int>(((GZipStream)unzip).BeginRead, ((GZipStream)unzip).EndRead, buffer, offset, count, null);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task.Factory.FromAsync<byte[], int, int>(((GZipStream)unzip).BeginWrite, ((GZipStream)unzip).EndWrite, buffer, offset, count, null);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
    }
}
