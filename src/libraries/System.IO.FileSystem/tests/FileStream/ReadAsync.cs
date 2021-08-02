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
        protected abstract Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadAsyncCanceledFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                while(fs.Length < 128 * 1024)
                    fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
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

            byte[][] buffers = Enumerable.Repeat(Enumerable.Repeat(byte.MaxValue, fileSize * 2).ToArray(), 10).ToArray();

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
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_ReadAsync_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_BeginEndRead_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(
                (callback, state) => stream.BeginRead(buffer, offset, count, callback, state),
                iar => stream.EndRead(iar),
                null);
    }
}
