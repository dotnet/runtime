// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_AsyncReads : FileSystemTest
    {
        protected abstract Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

        [Fact]
        public async Task EmptyFileReadAsyncSucceedSynchronously()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                byte[] buffer = new byte[TestBuffer.Length];

                // use a recognizable pattern
                TestBuffer.CopyTo(buffer, 0);

                // note that these do not succeed synchronously even though they do nothing.
                Assert.Equal(0, await ReadAsync(fs, buffer, 0, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await ReadAsync(fs, buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await ReadAsync(fs, buffer, buffer.Length - 1, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await ReadAsync(fs, buffer, buffer.Length / 2, buffer.Length - buffer.Length / 2));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public async Task ReadAsyncBufferedCompletesSynchronously()
        {
            string fileName = GetTestFilePath();

            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, TestBuffer.Length * 2, useAsync: true))
            {
                byte[] buffer = new byte[TestBuffer.Length];

                // prime the internal buffer
                Assert.Equal(TestBuffer.Length, await ReadAsync(fs, buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                Array.Clear(buffer);

                // read should now complete synchronously since it is serviced by the read buffer filled in the first request
                Assert.Equal(TestBuffer.Length, FSAssert.CompletesSynchronously(ReadAsync(fs, buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0, true)] // 0 == no buffering
        [InlineData(4096, true)] // 4096 == default buffer size
        [InlineData(0, false)]
        [InlineData(4096, false)]
        public async Task ReadAsyncCanceledFile(int bufferSize, bool isAsync)
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                while(fs.Length < 128 * 1024)
                    fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, isAsync))
            {
                byte[] buffer = new byte[fs.Length];
                CancellationTokenSource cts = new CancellationTokenSource();
                Task<int> readTask = ReadAsync(fs, buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();
                try
                {
                    await readTask;
                    // we may not have canceled before the task completed.
                }
                catch (OperationCanceledException oce)
                {
                    // Ideally we'd be doing an Assert.Throws<OperationCanceledException>
                    // but since cancellation is a race condition we accept either outcome
                    Assert.Equal(cts.Token, oce.CancellationToken);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(FileShare.None, FileOptions.Asynchronous)] // FileShare.None: exclusive access
        [InlineData(FileShare.ReadWrite, FileOptions.Asynchronous)] // FileShare.ReadWrite: others can write to the file, the length can't be cached
        [InlineData(FileShare.None, FileOptions.None)]
        [InlineData(FileShare.ReadWrite, FileOptions.None)]
        public async Task IncompleteReadCantSetPositionBeyondEndOfFile(FileShare fileShare, FileOptions options)
        {
            const int fileSize = 10_000;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, content);

            byte[][] buffers = Enumerable.Range(0, 10).Select(_ => new byte[fileSize * 2]).ToArray();

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, fileShare, bufferSize: 0, options))
            {
                Task<int>[] reads = buffers.Select(buffer => fs.ReadAsync(buffer, 0, buffer.Length)).ToArray();

                // the reads were not awaited, it's an anti-pattern and Position can be (0, buffersLength) now:
                Assert.InRange(fs.Position, 0, buffers.Sum(buffer => buffer.Length));

                await Task.WhenAll(reads);
                // but when they are finished, the first buffer should contain valid data:
                Assert.Equal(fileSize, reads.First().Result);
                AssertExtensions.SequenceEqual(content, buffers.First().AsSpan(0, fileSize));
                // and other reads should return 0:
                Assert.All(reads.Skip(1), read => Assert.Equal(0, read.Result));
                // and the Position must be correct:
                Assert.Equal(fileSize, fs.Position);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public async Task BypassingCacheInvalidatesCachedData(bool fsIsAsync, bool asyncReads)
        {
            const int BufferSize = 4096;
            const int FileSize = BufferSize * 4;
            string filePath = GetTestFilePath();
            byte[] content = RandomNumberGenerator.GetBytes(FileSize);
            File.WriteAllBytes(filePath, content);

            await Test(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, fsIsAsync));
            await Test(new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, fsIsAsync), BufferSize));

            async Task Test(Stream stream)
            {
                try
                {
                    // 1. Populates the private stream buffer, leaves bufferSize - 1 bytes available for next read.
                    await ReadAndAssertAsync(stream, 1);
                    // 2. Consumes all available data from the buffer, reads another bufferSize-many bytes from the disk and copies the 1 missing byte.
                    await ReadAndAssertAsync(stream, BufferSize);
                    // 3. Seek back by the number of bytes consumed from the buffer, all buffered data is now available for next read.
                    stream.Position -= 1;
                    // 4. Consume all buffered data.
                    await ReadAndAssertAsync(stream, BufferSize);
                    // 5. Bypass the cache (all buffered data has been consumed and we need bufferSize-many bytes).
                    // The cache should get invalidated now!!
                    await ReadAndAssertAsync(stream,BufferSize);
                    // 6. Seek back by just a few bytes.
                    stream.Position -= 9;
                    // 7. Perform a read, which should not use outdated buffered data.
                    await ReadAndAssertAsync(stream,BufferSize);
                }
                finally
                {
                    await stream.DisposeAsync();
                }
            }

            async Task ReadAndAssertAsync(Stream stream, int size)
            {
                var initialPosition = stream.Position;
                var buffer = new byte[size];

                var count = asyncReads
                    ? await ReadAsync(stream, buffer, 0, size)
                    : stream.Read(buffer);

                Assert.Equal(content.Skip((int)initialPosition).Take(count), buffer.Take(count));
            }
        }
    }

    public class FileStream_ReadAsync_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public class FileStream_BeginEndRead_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(
                (callback, state) => stream.BeginRead(buffer, offset, count, callback, state),
                iar => stream.EndRead(iar),
                null);
    }
}
