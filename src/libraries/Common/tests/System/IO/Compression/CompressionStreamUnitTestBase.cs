// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    public abstract class CompressionStreamUnitTestBase : CompressionStreamTestBase
    {
        [Fact]
        public virtual void FlushAsync_DuringWriteAsync()
        {
            if (FlushNoOps)
                return;
            byte[] buffer = new byte[100000];
            Random rand = new Random();
            rand.NextBytes(buffer);

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var compressor = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    // Write needs to be big enough to trigger a write to the underlying base stream so the WriteAsync call doesn't immediately complete.
                    task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    while (task.IsCompleted)
                    {
                        rand.NextBytes(buffer);
                        task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    }
                    Assert.Throws<InvalidOperationException>(() => { compressor.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(10 * 500), "Original WriteAsync Task did not complete in time");
                    Assert.True(writeStream.WriteHit, "BaseStream Write function was not called");
                }
            }
        }

        [Fact]
        public async Task FlushAsync_DuringReadAsync()
        {
            if (FlushNoOps)
                return;
            byte[] buffer = new byte[32];
            string testFilePath = CompressedTestFile(UncompressedTestFile());
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false))
            using (var decompressor = CreateStream(readStream, CompressionMode.Decompress, true))
            {
                Task task = null;
                try
                {
                    readStream.manualResetEvent.Reset();
                    readStream.ReadHit = false;
                    task = decompressor.ReadAsync(buffer, 0, 32);
                    Assert.True(readStream.ReadHit);
                    Assert.Throws<InvalidOperationException>(() => { decompressor.FlushAsync(); }); // "overlapping read"
                }
                finally
                {
                    // Unblock Async operations
                    readStream.manualResetEvent.Set();
                    // The original ReadAsync should be able to complete
                    Assert.True(task.Wait(10 * 500), "Original ReadAsync Task did not complete in time");
                }
            }
        }

        [Fact]
        public async Task FlushAsync_DuringFlushAsync()
        {
            if (FlushNoOps)
                return;
            byte[] buffer = null;
            string testFilePath = CompressedTestFile(UncompressedTestFile());
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
                    await zip.WriteAsync(buffer, 0, buffer.Length);
                    writeStream.manualResetEvent.Reset();
                    writeStream.WriteHit = false;
                    task = zip.FlushAsync();
                    while (!writeStream.WriteHit && task.IsCompleted)
                    {
                        zip.Write(buffer, 0, 1);
                        task = zip.FlushAsync();
                    }

                    Assert.Throws<InvalidOperationException>(() => { zip.FlushAsync(); }); // "overlapping flushes"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(5000), "Original write Task did not complete in time");
                    Assert.True(writeStream.WriteHit, "Underlying Writesync function was not called.");

                }
            }
        }

        [Fact]
        public virtual void WriteAsync_DuringWriteAsync()
        {
            byte[] buffer = new byte[100000];
            Random rand = new Random();
            rand.NextBytes(buffer);

            using (var writeStream = new ManualSyncMemoryStream(false))
            using (var compressor = CreateStream(writeStream, CompressionMode.Compress))
            {
                Task task = null;
                try
                {
                    // Write needs to be big enough to trigger a write to the underlying base stream so the WriteAsync call doesn't immediately complete.
                    task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    while (task.IsCompleted)
                    {
                        rand.NextBytes(buffer);
                        task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    }
                    Assert.Throws<InvalidOperationException>(() => { compressor.WriteAsync(buffer, 32, 32); }); // "overlapping write"
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // The original WriteAsync should be able to complete
                    Assert.True(task.Wait(10 * 500), "Original WriteAsync Task did not complete in time");
                    Assert.True(writeStream.WriteHit, "BaseStream Write function was not called");
                }
            }
        }

        [Fact]
        public async Task ReadAsync_DuringReadAsync()
        {
            byte[] buffer = new byte[32];
            string testFilePath = CompressedTestFile(UncompressedTestFile());
            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(testFilePath, false))
            using (var decompressor = CreateStream(readStream, CompressionMode.Decompress, true))
            {
                Task<int> task = null;
                try
                {
                    task = decompressor.ReadAsync(buffer, 0, 32);
                    Assert.Throws<InvalidOperationException>(() => { decompressor.ReadAsync(buffer, 0, 32); }); // "overlapping read"
                }
                finally
                {
                    // Unblock Async operations
                    readStream.manualResetEvent.Set();
                    // The original ReadAsync should be able to complete
                    Assert.True(task.Wait(10 * 500), "The original ReadAsync should be able to complete");
                    Assert.True(readStream.ReadHit, "BaseStream ReadAsync should have been called");
                }
            }
        }

        [Fact]
        public virtual void Dispose_WithUnfinishedWriteAsync()
        {
            byte[] buffer = new byte[100000];
            Random rand = new Random();
            rand.NextBytes(buffer);

            using (var writeStream = new ManualSyncMemoryStream(false))
            {
                var compressor = CreateStream(writeStream, CompressionMode.Compress, leaveOpen: true);
                compressor.Write(buffer, 0, buffer.Length);
                int writesBeingFlushed = 2;
                Task task = null;
                try
                {
                    // Write needs to be big enough to trigger a write to the underlying base stream so the WriteAsync call doesn't immediately complete.
                    task = compressor.WriteAsync(buffer, 0, buffer.Length);
                    while (task.IsCompleted)
                    {
                        rand.NextBytes(buffer);
                        task = compressor.WriteAsync(buffer, 0, buffer.Length);
                        writesBeingFlushed++;
                    }

                    // WriteAsync will be blocked on writing the output to the underlying stream. Calling Dispose will trigger a Finish call with unwritten output
                    // still available.
                    Assert.InRange(writeStream.Length, 0, buffer.Length);
                    compressor.Dispose();
                    Assert.InRange(writeStream.Length, 0, buffer.Length * writesBeingFlushed);
                    Assert.False(task.IsCompleted);
                }
                finally
                {
                    // Unblock Async operations
                    writeStream.manualResetEvent.Set();
                    // WriteAsync call will return to the compression stream's WriteAsync which will attempt to 
                    // access members of the now disposed stream. 
                    Assert.Throws<AggregateException>(() => task.Wait(1000));
                }
            }
        }

        [Fact]
        public virtual async Task Dispose_WithUnfinishedReadAsync()
        {
            string compressedPath = CompressedTestFile(UncompressedTestFile());
            int uncompressedSize = (int)new FileInfo(UncompressedTestFile()).Length;
            byte[] uncompressedBytes = new byte[uncompressedSize];

            using (var readStream = await ManualSyncMemoryStream.GetStreamFromFileAsync(compressedPath, false))
            {
                var decompressor = CreateStream(readStream, CompressionMode.Decompress, true);
                Task task = decompressor.ReadAsync(uncompressedBytes, 0, uncompressedBytes.Length);
                decompressor.Dispose();
                readStream.manualResetEvent.Set();
                Assert.Throws<AggregateException>(() => task.Wait(1000));
            }
        }

        [Theory]
        [MemberData(nameof(UncompressedTestFiles))]
        public async Task Read(string testFile)
        {
            var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);
            var compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(testFile));
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
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

        [Fact]
        public async Task Read_EndOfStreamPosition()
        {
            var compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            int compressedEndPosition = (int)compressedStream.Length;
            var rand = new Random(1024);
            int _bufferSize = BufferSize * 2 - 568;
            var bytes = new byte[_bufferSize];
            rand.NextBytes(bytes);
            compressedStream.Position = compressedEndPosition;
            compressedStream.Write(bytes, 0, _bufferSize);
            compressedStream.Write(bytes, 0, _bufferSize);
            compressedStream.Position = 0;
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);

            while (decompressor.Read(bytes, 0, _bufferSize) > 0);
            Assert.Equal(((compressedEndPosition / BufferSize) + 1) * BufferSize, compressedStream.Position);
        }

        [Fact]
        public async Task Read_BaseStreamSlowly()
        {
            string testFile = UncompressedTestFile();
            var uncompressedStream = await LocalMemoryStream.readAppFileAsync(testFile);
            var compressedStream = new BadWrappedStream(BadWrappedStream.Mode.ReadSlowly, File.ReadAllBytes(CompressedTestFile(testFile)));
            var decompressor = CreateStream(compressedStream, CompressionMode.Decompress);
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

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void CanReadCanWrite(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            Assert.True(mode == CompressionMode.Compress ? compressor.CanWrite : compressor.CanRead);

            compressor.Dispose();
            Assert.False(compressor.CanRead);
            Assert.False(compressor.CanWrite);
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void CanDisposeBaseStream(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            ms.Dispose(); // This would throw if this was invalid
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_NullStream(CompressionMode mode)
        {
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, mode));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_DisposedBaseStream(CompressionMode mode)
        {
            MemoryStream ms = new MemoryStream();
            ms.Dispose();
            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(ms, mode));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Ctor_InvalidStreamCanReadCanWrite(CompressionMode mode)
        {
            LocalMemoryStream ms = new LocalMemoryStream();
            ms.SetCanRead(mode == CompressionMode.Compress);
            ms.SetCanWrite(mode == CompressionMode.Decompress);

            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(ms, mode));
        }

        public IEnumerable<Func<Stream, Stream>> CtorFunctions()
        {
            CompressionLevel[] legalValues = new CompressionLevel[] { CompressionLevel.Optimal, CompressionLevel.Fastest, CompressionLevel.NoCompression };
            yield return new Func<Stream, Stream>((stream) => CreateStream(stream, CompressionMode.Compress));

            foreach (CompressionLevel level in legalValues)
            {
                yield return new Func<Stream, Stream>((stream) => CreateStream(stream, level));

                bool[] boolValues = new bool[] { true, false };

                foreach (bool remainsOpen in boolValues)
                {
                    yield return new Func<Stream, Stream>((stream) => CreateStream(stream, level, remainsOpen));
                }
            }
        }

        [Fact]
        public void TestCompressCtor()
        {
            Assert.All(CtorFunctions(), (create) =>
            {
                //Create the Stream
                int _bufferSize = 1024;
                var bytes = new byte[_bufferSize];
                var baseStream = new MemoryStream(bytes, writable: true);
                Stream compressor = create(baseStream);

                //Write some data and Close the stream
                string strData = "Test Data";
                var encoding = Encoding.UTF8;
                byte[] data = encoding.GetBytes(strData);
                compressor.Write(data, 0, data.Length);
                compressor.Flush();
                compressor.Dispose();
                baseStream.Dispose();

                //Read the data
                byte[] data2 = new byte[_bufferSize];
                baseStream = new MemoryStream(bytes, writable: false);
                var decompressor = CreateStream(baseStream, CompressionMode.Decompress);
                int size = decompressor.Read(data2, 0, _bufferSize - 5);

                //Verify the data roundtripped
                for (int i = 0; i < size + 5; i++)
                {
                    if (i < data.Length)
                    {
                        Assert.Equal(data[i], data2[i]);
                    }
                    else
                    {
                        Assert.Equal(data2[i], (byte)0);
                    }
                }
            });
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void TestLeaveOpen(CompressionMode mode)
        {
            //Create the Stream
            var baseStream = new MemoryStream();
            Stream compressor = CreateStream(baseStream, mode, leaveOpen: false);
            compressor.Dispose();

            //Check that Close has really closed the underlying stream
            Assert.Throws<ObjectDisposedException>(() => baseStream.Write(new byte[] { }, 0, 0));
        }

        [Fact]
        public void TestLeaveOpenAfterValidCompress()
        {
            //Create the Stream
            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            var baseStream = new MemoryStream(bytes, writable: true);
            Stream compressor = CreateStream(baseStream, CompressionMode.Compress, leaveOpen: false);

            //Write some data and Close the stream
            string strData = "Test Data";
            var encoding = Encoding.UTF8;
            byte[] data = encoding.GetBytes(strData);
            compressor.Write(data, 0, data.Length);
            compressor.Flush();
            compressor.Dispose();

            //Check that Close has really closed the underlying stream
            Assert.Throws<ObjectDisposedException>(() => baseStream.Write(bytes, 0, bytes.Length));
        }

        [Fact]
        public async void TestLeaveOpenAfterValidDecompress()
        {
            //Create the Stream
            int _bufferSize = 1024;
            var bytes = new byte[_bufferSize];
            Stream compressedStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            Stream decompressor = CreateStream(compressedStream, CompressionMode.Decompress, leaveOpen: false);

            //Read some data and Close the stream
            decompressor.Read(bytes, 0, _bufferSize);
            decompressor.Flush();
            decompressor.Dispose();

            //Check that Close has really closed the underlying stream
            Assert.Throws<ObjectDisposedException>(() => compressedStream.Read(bytes, 0, bytes.Length));
        }

        [Fact]
        public void Ctor_ArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionLevel.Fastest));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Decompress));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Compress));

            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionLevel.Fastest, true));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Decompress, false));
            Assert.Throws<ArgumentNullException>(() => CreateStream(null, CompressionMode.Compress, true));

            AssertExtensions.Throws<ArgumentException>("mode", () => CreateStream(new MemoryStream(), (CompressionMode)42));
            AssertExtensions.Throws<ArgumentException>("mode", () => CreateStream(new MemoryStream(), (CompressionMode)43, true));

            AssertExtensions.Throws<ArgumentException>("stream", () => CreateStream(new MemoryStream(new byte[1], writable: false), CompressionLevel.Optimal));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public async Task Flush(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Flush();
            await compressor.FlushAsync();
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Flush_Double(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Flush();
            compressor.Flush();
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Dispose_Double(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();
            compressor.Dispose();
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Flush_FollowedByDispose(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Flush();
            compressor.Dispose();
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void Dispose_FollowedBySyncOperations(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();

            if (mode == CompressionMode.Compress)
                Assert.Throws<ObjectDisposedException>(() => compressor.Write(new byte[1], 0, 1));
            else
                Assert.Throws<ObjectDisposedException>(() => compressor.Read(new byte[1], 0, 1));
            Assert.Throws<ObjectDisposedException>(() => compressor.Flush());
            Assert.Throws<ObjectDisposedException>(() => compressor.CopyTo(new MemoryStream()));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public virtual async Task Dispose_FollowedByAsyncOperations(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();

            if (mode == CompressionMode.Compress)
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.WriteAsync(new byte[1], 0, 1));
            else
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.ReadAsync(new byte[1], 0, 1));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.FlushAsync());
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await compressor.CopyToAsync(new MemoryStream()));
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void TestSeekMethods(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var decompressor = CreateStream(ms, mode);
            Assert.False(decompressor.CanSeek, "CanSeek should be false");
            Assert.Throws<NotSupportedException>(() => decompressor.Length);
            Assert.Throws<NotSupportedException>(() => decompressor.SetLength(1));
            Assert.Throws<NotSupportedException>(() => decompressor.Position);
            Assert.Throws<NotSupportedException>(() => decompressor.Position = 100L);
            Assert.Throws<NotSupportedException>(() => decompressor.Seek(100L, SeekOrigin.Begin));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public virtual void Write_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Compress))
            {
                Assert.Throws<ArgumentNullException>(() => { if (useAsync) { decompressor.WriteAsync(null, 0, 0).Wait(); } else { decompressor.Write(null, 0, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], -1, 0).Wait(); } else { decompressor.Write(new byte[1], -1, 0); } });
                Assert.Throws<ArgumentOutOfRangeException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, -1).Wait(); } else { decompressor.Write(new byte[1], 0, -1); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 2).Wait(); } else { decompressor.Write(new byte[1], 0, 2); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });
                Assert.Throws<InvalidOperationException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, 1).Result : decompressor.Read(new byte[1], 0, 1));
                Assert.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                if (useAsync)
                { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); }
                else
                { decompressor.Write(new byte[1], 0, 1); }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(true)]
        public virtual void Read_ArgumentValidation(bool useAsync)
        {
            using (var decompressor = CreateStream(new MemoryStream(), CompressionMode.Decompress))
            {
                Assert.Throws<ArgumentNullException>(() => useAsync ? decompressor.ReadAsync(null, 0, 0).Result : decompressor.Read(null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], -1, 0).Result : decompressor.Read(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => useAsync ? decompressor.ReadAsync(new byte[1], 0, -1).Result : decompressor.Read(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 0, 2).Result : decompressor.Read(new byte[1], 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => useAsync ? decompressor.ReadAsync(new byte[1], 1, 1).Result : decompressor.Read(new byte[1], 1, 1));
                Assert.Throws<InvalidOperationException>(() => { if (useAsync) { decompressor.WriteAsync(new byte[1], 0, 1).Wait(); } else { decompressor.Write(new byte[1], 0, 1); } });
                Assert.Throws<ArgumentException>(null, () => { if (useAsync) { decompressor.WriteAsync(new byte[1], 1, 1).Wait(); } else { decompressor.Write(new byte[1], 1, 1); } });

                var data = new byte[1] { 42 };
                Assert.Equal(0, useAsync ? decompressor.ReadAsync(data, 0, 0).Result : decompressor.Read(data, 0, 0));
                Assert.Equal(42, data[0]);
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void CopyToAsync_ArgumentValidation(CompressionMode mode)
        {
            using (Stream compressor = CreateStream(new MemoryStream(), mode))
            {
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { compressor.CopyToAsync(null); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { compressor.CopyToAsync(new MemoryStream(), 0); });
                Assert.Throws<NotSupportedException>(() => { compressor.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });
                compressor.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { compressor.CopyToAsync(new MemoryStream()); });
            }
        }

        public enum ReadWriteMode
        {
            SyncArray,
            SyncSpan,
            AsyncArray,
            AsyncMemory,
            AsyncBeginEnd
        }

        public static IEnumerable<object[]> RoundtripCompressDecompressOuterData
        {
            get
            {
                foreach (ReadWriteMode readWriteMode in new[] { ReadWriteMode.SyncArray, ReadWriteMode.SyncSpan, ReadWriteMode.AsyncArray, ReadWriteMode.AsyncMemory, ReadWriteMode.AsyncBeginEnd })
                {
                    foreach (var level in new[] { CompressionLevel.Fastest, CompressionLevel.Optimal, CompressionLevel.NoCompression }) // compression level
                    {
                        yield return new object[] { readWriteMode, 1, 5, level }; // smallest possible writes
                        yield return new object[] { readWriteMode, 1023, 1023 * 10, level }; // overflowing internal buffer
                        yield return new object[] { readWriteMode, 1024 * 1024, 1024 * 1024, level }; // large single write
                    }
                }
            }
        }

        [Fact]
        public async Task CompressDecompress_RoundTrip()
        {
            await CompressDecompress_RoundTrip(ReadWriteMode.SyncArray, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await CompressDecompress_RoundTrip(ReadWriteMode.SyncSpan, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await CompressDecompress_RoundTrip(ReadWriteMode.AsyncArray, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await CompressDecompress_RoundTrip(ReadWriteMode.AsyncMemory, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await CompressDecompress_RoundTrip(ReadWriteMode.AsyncBeginEnd, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await CompressDecompress_RoundTrip(ReadWriteMode.AsyncArray, chunkSize: 1024, totalSize: 8192, level: CompressionLevel.Optimal);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(RoundtripCompressDecompressOuterData))]
        public async Task CompressDecompress_RoundTrip(ReadWriteMode readWriteMode, int chunkSize, int totalSize, CompressionLevel level)
        {
            byte[] data = new byte[totalSize];
            new Random(42).NextBytes(data);

            var compressed = new MemoryStream();
            using (var compressor = CreateStream(compressed, level, true))
            {
                for (int i = 0; i < data.Length; i += chunkSize) // not using CopyTo{Async} due to optimizations in MemoryStream's implementation that avoid what we're trying to test
                {
                    switch (readWriteMode)
                    {
                        case ReadWriteMode.AsyncArray:
                            await compressor.WriteAsync(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncArray:
                            compressor.Write(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncSpan:
                            compressor.Write(new ReadOnlySpan<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncMemory:
                            await compressor.WriteAsync(new ReadOnlyMemory<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncBeginEnd:
                            await Task.Factory.FromAsync(compressor.BeginWrite, compressor.EndWrite, data, i, chunkSize, null);
                            break;
                    }
                }
            }
            compressed.Position = 0;
            await ReadAndValidateCompressedData(readWriteMode, chunkSize, compressed, data);
            compressed.Dispose();
        }

        [Fact]
        public async Task Flush_RoundTrip()
        {
            if (FlushNoOps)
                return;
            await Flush_RoundTrip(ReadWriteMode.SyncArray, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await Flush_RoundTrip(ReadWriteMode.AsyncArray, chunkSize: 1024, totalSize: 8192, level: CompressionLevel.Optimal);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(RoundtripCompressDecompressOuterData))]
        public async Task Flush_RoundTrip(ReadWriteMode readWriteMode, int chunkSize, int totalSize, CompressionLevel level)
        {
            if (FlushNoOps)
                return;
            byte[] data = new byte[totalSize];
            new Random(42).NextBytes(data);

            using (var compressed = new MemoryStream())
            using (var compressor = CreateStream(compressed, level, true))
            {
                for (int i = 0; i < data.Length; i += chunkSize) // not using CopyTo{Async} due to optimizations in MemoryStream's implementation that avoid what we're trying to test
                {
                    switch (readWriteMode)
                    {
                        case ReadWriteMode.AsyncArray:
                            await compressor.WriteAsync(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncArray:
                            compressor.Write(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncSpan:
                            compressor.Write(new ReadOnlySpan<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncMemory:
                            await compressor.WriteAsync(new ReadOnlyMemory<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncBeginEnd:
                            await Task.Factory.FromAsync(compressor.BeginWrite, compressor.EndWrite, data, i, chunkSize, null);
                            break;
                    }
                }
                switch (readWriteMode)
                {
                    case ReadWriteMode.AsyncArray:
                    case ReadWriteMode.AsyncMemory:
                    case ReadWriteMode.AsyncBeginEnd:
                        await compressor.FlushAsync();
                        break;
                    case ReadWriteMode.SyncSpan:
                    case ReadWriteMode.SyncArray:
                        compressor.Flush();
                        break;
                }
                if (!FlushCompletes)
                    compressor.Dispose();
                compressed.Position = 0;
                await ReadAndValidateCompressedData(readWriteMode, chunkSize, compressed, data);
            }
        }

        [Fact]
        public async Task Flush_Consecutive()
        {
            if (FlushNoOps)
                return;
            await Flush_Consecutive(ReadWriteMode.SyncArray, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await Flush_Consecutive(ReadWriteMode.AsyncArray, chunkSize: 1024, totalSize: 8192, level: CompressionLevel.Optimal);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(RoundtripCompressDecompressOuterData))]
        public async Task Flush_Consecutive(ReadWriteMode readWriteMode, int chunkSize, int totalSize, CompressionLevel level)
        {
            if (FlushNoOps)
                return;
            byte[] data = new byte[totalSize];
            List<byte> expected = new List<byte>();
            new Random(42).NextBytes(data);

            using (var compressed = new MemoryStream())
            using (var compressor = CreateStream(compressed, level, true))
            {
                for (int i = 0; i < data.Length; i += chunkSize) // not using CopyTo{Async} due to optimizations in MemoryStream's implementation that avoid what we're trying to test
                {
                    switch (readWriteMode)
                    {
                        case ReadWriteMode.AsyncArray:
                            await compressor.WriteAsync(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncArray:
                            compressor.Write(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncSpan:
                            compressor.Write(new ReadOnlySpan<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncMemory:
                            await compressor.WriteAsync(new ReadOnlyMemory<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncBeginEnd:
                            await Task.Factory.FromAsync(compressor.BeginWrite, compressor.EndWrite, data, i, chunkSize, null);
                            break;
                    }
                    for (int j = i; j < i + chunkSize; j++)
                        expected.Insert(j, data[j]);

                    switch (readWriteMode)
                    {
                        case ReadWriteMode.AsyncArray:
                        case ReadWriteMode.AsyncMemory:
                        case ReadWriteMode.AsyncBeginEnd:
                            await compressor.FlushAsync();
                            break;
                        case ReadWriteMode.SyncSpan:
                        case ReadWriteMode.SyncArray:
                            compressor.Flush();
                            break;
                    }

                    if (FlushCompletes)
                    {
                        MemoryStream partiallyCompressed = new MemoryStream(compressed.ToArray());
                        partiallyCompressed.Position = 0;
                        await ReadAndValidateCompressedData(readWriteMode, chunkSize, partiallyCompressed, expected.ToArray());
                    }
                }
                if (!FlushCompletes)
                    compressor.Dispose();
                MemoryStream fullyCompressed = new MemoryStream(compressed.ToArray());
                fullyCompressed.Position = 0;
                await ReadAndValidateCompressedData(readWriteMode, chunkSize, fullyCompressed, expected.ToArray());
            }
        }

        [Fact]
        public async Task Flush_BeforeFirstWrites()
        {
            if (FlushNoOps)
                return;
            await Flush_BeforeFirstWrites(ReadWriteMode.SyncArray, chunkSize: 1, totalSize: 10, level: CompressionLevel.Fastest);
            await Flush_BeforeFirstWrites(ReadWriteMode.AsyncArray, chunkSize: 1024, totalSize: 8192, level: CompressionLevel.Optimal);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(RoundtripCompressDecompressOuterData))]
        public async Task Flush_BeforeFirstWrites(ReadWriteMode readWriteMode, int chunkSize, int totalSize, CompressionLevel level)
        {
            if (FlushNoOps)
                return;
            byte[] data = new byte[totalSize];
            new Random(42).NextBytes(data);

            using (var compressed = new MemoryStream())
            using (var compressor = CreateStream(compressed, level, true))
            {
                switch (readWriteMode)
                {
                    case ReadWriteMode.AsyncArray:
                    case ReadWriteMode.AsyncMemory:
                    case ReadWriteMode.AsyncBeginEnd:
                        await compressor.FlushAsync();
                        break;
                    case ReadWriteMode.SyncSpan:
                    case ReadWriteMode.SyncArray:
                        compressor.Flush();
                        break;
                }

                for (int i = 0; i < data.Length; i += chunkSize) // not using CopyTo{Async} due to optimizations in MemoryStream's implementation that avoid what we're trying to test
                {
                    switch (readWriteMode)
                    {
                        case ReadWriteMode.AsyncArray:
                            await compressor.WriteAsync(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncArray:
                            compressor.Write(data, i, chunkSize);
                            break;
                        case ReadWriteMode.SyncSpan:
                            compressor.Write(new ReadOnlySpan<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncMemory:
                            await compressor.WriteAsync(new ReadOnlyMemory<byte>(data, i, chunkSize));
                            break;
                        case ReadWriteMode.AsyncBeginEnd:
                            await Task.Factory.FromAsync(compressor.BeginWrite, compressor.EndWrite, data, i, chunkSize, null);
                            break;
                    }
                }

                switch (readWriteMode)
                {
                    case ReadWriteMode.AsyncArray:
                    case ReadWriteMode.AsyncMemory:
                    case ReadWriteMode.AsyncBeginEnd:
                        await compressor.FlushAsync();
                        break;
                    case ReadWriteMode.SyncSpan:
                    case ReadWriteMode.SyncArray:
                        compressor.Flush();
                        break;
                }
                if (!FlushCompletes)
                    compressor.Dispose();
                compressed.Position = 0;
                await ReadAndValidateCompressedData(readWriteMode, chunkSize, compressed, data);
            }
        }

        /// <summary>
        /// Given a MemoryStream of compressed data and a byte array of desired output, decompresses
        /// the stream and validates that it is equal to the expected array.
        /// </summary>
        private async Task ReadAndValidateCompressedData(ReadWriteMode readWriteMode, int chunkSize, MemoryStream compressed, byte[] expected)
        {
            using (MemoryStream decompressed = new MemoryStream())
            using (Stream decompressor = CreateStream(compressed, CompressionMode.Decompress, true))
            {
                int bytesRead;
                var buffer = new byte[chunkSize];
                switch (readWriteMode)
                {
                    case ReadWriteMode.SyncSpan:
                        while ((bytesRead = decompressor.Read(new Span<byte>(buffer))) != 0)
                        {
                            decompressed.Write(buffer, 0, bytesRead);
                        }
                        break;
                    case ReadWriteMode.SyncArray:
                        while ((bytesRead = decompressor.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            decompressed.Write(buffer, 0, bytesRead);
                        }
                        break;
                    case ReadWriteMode.AsyncArray:
                        while ((bytesRead = await decompressor.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            decompressed.Write(buffer, 0, bytesRead);
                        }
                        break;
                    case ReadWriteMode.AsyncMemory:
                        while ((bytesRead = await decompressor.ReadAsync(new Memory<byte>(buffer))) != 0)
                        {
                            decompressed.Write(buffer, 0, bytesRead);
                        }
                        break;
                    case ReadWriteMode.AsyncBeginEnd:
                        while ((bytesRead = await Task<int>.Factory.FromAsync(decompressor.BeginRead, decompressor.EndRead, buffer, 0, buffer.Length, null)) != 0)
                        {
                            decompressed.Write(buffer, 0, bytesRead);
                        }
                        break;
                }
                Assert.Equal<byte>(expected, decompressed.ToArray());
            }
        }

        [Theory]
        [InlineData(ReadWriteMode.SyncArray)]
        [InlineData(ReadWriteMode.AsyncArray)]
        [InlineData(ReadWriteMode.SyncSpan)]
        [InlineData(ReadWriteMode.AsyncMemory)]
        [InlineData(ReadWriteMode.AsyncBeginEnd)]
        public async Task Read_SequentialReadsOnMemoryStream_Return_SameBytes(ReadWriteMode readWriteMode)
        {
            byte[] data = new byte[1024 * 10];
            new Random(42).NextBytes(data);

            var compressed = new MemoryStream();
            using (var compressor = CreateStream(compressed, CompressionMode.Compress, true))
            {
                for (int i = 0; i < data.Length; i += 1024)
                {
                    switch (readWriteMode)
                    {
                        case ReadWriteMode.SyncArray:
                            compressor.Write(data, i, 1024);
                            break;
                        case ReadWriteMode.AsyncArray:
                            await compressor.WriteAsync(data, i, 1024);
                            break;
                        case ReadWriteMode.SyncSpan:
                            compressor.Write(new Span<byte>(data, i, 1024));
                            break;
                        case ReadWriteMode.AsyncMemory:
                            await compressor.WriteAsync(new ReadOnlyMemory<byte>(data, i, 1024));
                            break;
                        case ReadWriteMode.AsyncBeginEnd:
                            await Task.Factory.FromAsync(compressor.BeginWrite, compressor.EndWrite, data, i, 1024, null);
                            break;
                    }
                }
            }
            compressed.Position = 0;

            using (var decompressor = CreateStream(compressed, CompressionMode.Decompress, true))
            {
                int i, j;
                byte[] array = new byte[100];
                byte[] array2 = new byte[100];

                // only read in the first 100 bytes
                switch (readWriteMode)
                {
                    case ReadWriteMode.SyncArray:
                        decompressor.Read(array, 0, array.Length);
                        break;
                    case ReadWriteMode.AsyncArray:
                        await decompressor.ReadAsync(array, 0, array.Length);
                        break;
                    case ReadWriteMode.SyncSpan:
                        decompressor.Read(new Span<byte>(array));
                        break;
                    case ReadWriteMode.AsyncMemory:
                        await decompressor.ReadAsync(new Memory<byte>(array));
                        break;
                    case ReadWriteMode.AsyncBeginEnd:
                        await Task<int>.Factory.FromAsync(decompressor.BeginRead, decompressor.EndRead, array, 0, array.Length, null);
                        break;
                }
                for (i = 0; i < array.Length; i++)
                {
                    Assert.Equal(data[i], array[i]);
                }

                // read in the next 100 bytes and make sure nothing is missing
                switch (readWriteMode)
                {
                    case ReadWriteMode.SyncArray:
                        decompressor.Read(array2, 0, array2.Length);
                        break;
                    case ReadWriteMode.AsyncArray:
                        await decompressor.ReadAsync(array2, 0, array2.Length);
                        break;
                    case ReadWriteMode.SyncSpan:
                        decompressor.Read(new Span<byte>(array2));
                        break;
                    case ReadWriteMode.AsyncMemory:
                        await decompressor.ReadAsync(new Memory<byte>(array2));
                        break;
                    case ReadWriteMode.AsyncBeginEnd:
                        await Task<int>.Factory.FromAsync(decompressor.BeginRead, decompressor.EndRead, array2, 0, array2.Length, null);
                        break;
                }
                for (j = 0; j < array2.Length; j++)
                {
                    Assert.Equal(data[j], array[j]);
                }
            }
        }

        [Fact]
        public void WriteByte_RountTrip()
        {
            byte[] data = new byte[1024 * 10];
            new Random(42).NextBytes(data);

            var compressed = new MemoryStream();
            using (var compressor = CreateStream(compressed, CompressionMode.Compress, true))
            {
                compressor.Write(data, 0, data.Length);
            }
            compressed.Position = 0;

            using (var decompressor = CreateStream(compressed, CompressionMode.Decompress, true))
            {
                for (int i = 0; i < data.Length; i++)
                    Assert.Equal(data[i], decompressor.ReadByte());
            }
        }

        [Fact]
        public async Task WrapNullReturningTasksStream()
        {
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnNullTasks), CompressionMode.Decompress))
                Assert.Equal(0, await decompressor.ReadAsync(new byte[1024], 0, 1024));
        }

        [Fact]
        public async Task WrapStreamReturningBadReadValues()
        {
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                Assert.Throws<InvalidDataException>(() => decompressor.Read(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                await Assert.ThrowsAsync<InvalidDataException>(() => decompressor.ReadAsync(new byte[1024], 0, 1024));
            using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooLargeCounts), CompressionMode.Decompress))
                await Assert.ThrowsAsync<InvalidDataException>(async () => { await decompressor.ReadAsync(new Memory<byte>(new byte[1024])); });

            if (!FlushNoOps)
            {
                using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                    Assert.Equal(0, decompressor.Read(new byte[1024], 0, 1024));
                using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                    Assert.Equal(0, await decompressor.ReadAsync(new byte[1024], 0, 1024));
                using (var decompressor = CreateStream(new BadWrappedStream(BadWrappedStream.Mode.ReturnTooSmallCounts), CompressionMode.Decompress))
                    Assert.Equal(0, await decompressor.ReadAsync(new Memory<byte>(new byte[1024])));
            }
        }

        [Theory]
        [OuterLoop]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CopyTo_Roundtrip_OutputMatchesInput(bool useAsync)
        {
            var rand = new Random();
            foreach (int dataSize in new[] { 1, 1024, 4095, 1024 * 1024 })
            {
                var data = new byte[dataSize];
                rand.NextBytes(data);

                var compressed = new MemoryStream();
                using (var ds = CreateStream(compressed, CompressionMode.Compress, leaveOpen: true))
                {
                    ds.Write(data, 0, data.Length);
                }
                byte[] compressedData = compressed.ToArray();

                foreach (int copyBufferSize in new[] { 1, 4096, 80 * 1024 })
                {
                    // Memory source
                    var m = new MemoryStream(compressedData, writable: false);
                    await CopyTo_Roundtrip_OutputMatchesInput_Verify(data, copyBufferSize, m, useAsync);

                    // File sources, sync and async
                    foreach (bool useAsyncFileOptions in new[] { true, false })
                    {
                        string path = Path.GetTempFileName();
                        File.WriteAllBytes(path, compressedData);

                        FileOptions options = FileOptions.DeleteOnClose;
                        if (useAsyncFileOptions)
                            options |= FileOptions.Asynchronous;
                        await CopyTo_Roundtrip_OutputMatchesInput_Verify(data, copyBufferSize, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, options), useAsync);
                    }
                }
            }
        }

        private async Task CopyTo_Roundtrip_OutputMatchesInput_Verify(byte[] expectedDecrypted, int copyBufferSize, Stream source, bool useAsync)
        {
            var m = new MemoryStream();
            using (Stream ds = CreateStream(source, CompressionMode.Decompress))
            {
                if (useAsync)
                    await ds.CopyToAsync(m);
                else
                    ds.CopyTo(m, copyBufferSize);
            }
            Assert.Equal(expectedDecrypted, m.ToArray());
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void BaseStream(CompressionMode mode)
        {
            using (var baseStream = new MemoryStream())
            using (var compressor = CreateStream(baseStream, mode))
            {
                Assert.Same(BaseStream(compressor), baseStream);
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public async Task BaseStream_Modify(CompressionMode mode)
        {
            using (var baseStream = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile())))
            using (var compressor = CreateStream(baseStream, mode))
            {
                int size = 1024;
                byte[] bytes = new byte[size];
                if (mode == CompressionMode.Compress)
                    BaseStream(compressor).Write(bytes, 0, size); // This will throw if the underlying stream is not writable as expected
                else
                    BaseStream(compressor).Read(bytes, 0, size); // This will throw if the underlying stream is not readable as expected
            }
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public void BaseStream_NullAfterDisposeWithFalseLeaveOpen(CompressionMode mode)
        {
            var ms = new MemoryStream();
            var compressor = CreateStream(ms, mode);
            compressor.Dispose();

            Assert.Null(BaseStream(compressor));

            compressor.Dispose(); // Should be a no-op
        }

        [Theory]
        [InlineData(CompressionMode.Compress)]
        [InlineData(CompressionMode.Decompress)]
        public async Task BaseStream_ValidAfterDisposeWithTrueLeaveOpen(CompressionMode mode)
        {
            var ms = await LocalMemoryStream.readAppFileAsync(CompressedTestFile(UncompressedTestFile()));
            var decompressor = CreateStream(ms, mode, leaveOpen: true);
            var baseStream = BaseStream(decompressor);
            Assert.Same(ms, baseStream);
            decompressor.Dispose();

            int size = 1024;
            byte[] bytes = new byte[size];
            if (mode == CompressionMode.Compress)
                baseStream.Write(bytes, 0, size);
            else
                baseStream.Read(bytes, 0, size);
        }
    }

    internal sealed class BadWrappedStream : MemoryStream
    {
        public enum Mode
        {
            Default,
            ReturnNullTasks,
            ReturnTooSmallCounts,
            ReturnTooLargeCounts,
            ReadSlowly
        }

        private readonly Mode _mode;

        public BadWrappedStream(Mode mode) { _mode = mode; }
        public BadWrappedStream(Mode mode, byte[] buffer) : base(buffer) { _mode = mode; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            switch (_mode)
            {
                case Mode.ReturnTooSmallCounts:
                    return -1;
                case Mode.ReturnTooLargeCounts:
                    return buffer.Length + 1;
                case Mode.ReadSlowly:
                    return base.Read(buffer, offset, 1);
                default:
                    return 0;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _mode == Mode.ReturnNullTasks ?
               null :
               base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Flush() { }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
    }

    internal partial class ManualSyncMemoryStream : MemoryStream
    {
        private bool isSync;
        public ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(initialState: false);

        public bool ReadHit = false;  // For validation of the async methods we want to ensure they correctly delegate the async
        public bool WriteHit = false; // methods of the underlying stream. This bool acts as a toggle to check that they're being used.

        public static async Task<ManualSyncMemoryStream> GetStreamFromFileAsync(string testFile, bool sync = false)
        {
            var baseStream = await StreamHelpers.CreateTempCopyStream(testFile);
            var ms = new ManualSyncMemoryStream(sync);
            await baseStream.CopyToAsync(ms);

            ms.Position = 0;
            return ms;
        }

        public ManualSyncMemoryStream(bool sync = false) : base()
        {
            isSync = sync;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        public override int EndRead(IAsyncResult asyncResult) => TaskToApm.End<int>(asyncResult);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        public override void EndWrite(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public override async Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            ReadHit = true;
            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            return await base.ReadAsync(array, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            WriteHit = true;
            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            await base.WriteAsync(array, offset, count, cancellationToken);
        }
        #if STREAM_MEMORY_OVERLOADS_AVAILABLE

        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            ReadHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }
            return await base.ReadAsync(destination, cancellationToken);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            WriteHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken)).ConfigureAwait(false);
            }

            await base.WriteAsync(source, cancellationToken);
        }
        #endif
    }
}
