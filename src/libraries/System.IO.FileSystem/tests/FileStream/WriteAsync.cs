// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_AsyncWrites : FileSystemTest
    {
        private Task WriteAsync(FileStream stream, byte[] buffer, int offset, int count) =>
            WriteAsync(stream, buffer, offset, count, CancellationToken.None);
        protected abstract Task WriteAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        [Fact]
        public void WriteAsyncDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], 0, 1)));
                // even for noop WriteAsync
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], 0, 0)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[2], 1, 2)));
            }
        }

        [Fact]
        public void ReadOnlyThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            { }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], 0, 1)));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], 0, 1)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[2], 1, 2)));
            }
        }

        [Fact]
        public async Task NoopWriteAsyncsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // note that these do not succeed synchronously even though they do nothing.

                await WriteAsync(fs, new byte[0], 0, 0);
                await WriteAsync(fs, new byte[1], 0, 0);
                // even though offset is out of bounds of buffer, this is still allowed
                // for the last element
                await WriteAsync(fs, new byte[1], 1, 0);
                await WriteAsync(fs, new byte[2], 1, 0);
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Fact]
        public void WriteAsyncBufferedCompletesSynchronously()
        {
            using (FileStream fs = new FileStream(
                GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete,
                TestBuffer.Length * 2, useAsync: true))
            {
                FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[TestBuffer.Length], 0, TestBuffer.Length));
            }
        }

        [Fact]
        public async Task SimpleWriteAsync()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                await WriteAsync(fs, TestBuffer, 0, TestBuffer.Length);
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Theory]
        [InlineData(0, true)] // 0 == no buffering
        [InlineData(4096, true)] // 4096 == default buffer size
        [InlineData(0, false)]
        [InlineData(4096, false)]
        public async Task WriteAsyncCancelledFile(int bufferSize, bool isAsync)
        {
            const int writeSize = 1024 * 1024;
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, isAsync))
            {
                byte[] buffer = new byte[writeSize];
                CancellationTokenSource cts = new CancellationTokenSource();
                Task writeTask = WriteAsync(fs, buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();
                try
                {
                    await writeTask;
                }
                catch (OperationCanceledException oce)
                {
                    // Ideally we'd be doing an Assert.Throws<OperationCanceledException>
                    // but since cancellation is a race condition we accept either outcome
                    Assert.Equal(cts.Token, oce.CancellationToken);

                    Assert.Equal(0, fs.Length); // if write was cancelled, the file should be empty
                    Assert.Equal(0, fs.Position); // if write was cancelled, the Position should remain unchanged
                }
            }
        }

        [Fact]
        public async Task WriteAsyncInternalBufferOverflow()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write, FileShare.None, 3, useAsync: true))
            {
                // Fill buffer; should trigger flush of full buffer, no additional I/O
                await WriteAsync(fs, TestBuffer, 0, 3);
                Assert.True(fs.Length == 3);

                // Add to next buffer
                await WriteAsync(fs, TestBuffer, 0, 1);
                Assert.True(fs.Length == 4);

                // Complete that buffer; should trigger flush of full buffer, no additional I/O
                await WriteAsync(fs, TestBuffer, 0, 2);
                Assert.True(fs.Length == 6);

                // Add to next buffer
                await WriteAsync(fs, TestBuffer, 0, 2);
                Assert.True(fs.Length == 8);

                // Overflow buffer with amount that could fit in a buffer; should trigger a flush, with additional I/O
                await WriteAsync(fs, TestBuffer, 0, 2);
                Assert.True(fs.Length == 10);

                // Overflow buffer with amount that couldn't fit in a buffer; shouldn't be anything to flush, just an additional I/O
                await WriteAsync(fs, TestBuffer, 0, 4);
                Assert.True(fs.Length == 14);
            }
        }

        public static IEnumerable<object[]> MemberData_FileStreamAsyncWriting()
        {
            foreach (bool useAsync in new[] { true, false })
            {
                if (useAsync && !OperatingSystem.IsWindows())
                {
                    // We don't have a special async I/O implementation in FileStream on Unix.
                    continue;
                }

                foreach (bool preSize in new[] { true, false })
                {
                    foreach (bool cancelable in new[] { true, false })
                    {
                        yield return new object[] { useAsync, preSize, false, cancelable, 0x1000, 0x100, 100 };
                        yield return new object[] { useAsync, preSize, false, cancelable, 0x1, 0x1, 1000 };
                        yield return new object[] { useAsync, preSize, true, cancelable, 0x2, 0x100, 100 };
                        yield return new object[] { useAsync, preSize, false, cancelable, 0x4000, 0x10, 100 };
                        yield return new object[] { useAsync, preSize, true, cancelable, 0x1000, 99999, 10 };
                    }
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // Browser PNSE: Cannot wait on monitors
        [PlatformSpecific(TestPlatforms.Windows)]
        public Task ManyConcurrentWriteAsyncs()
        {
            // For inner loop, just test one case
            return ManyConcurrentWriteAsyncs_OuterLoop(
                useAsync: true,
                presize: false,
                exposeHandle: false,
                cancelable: true,
                bufferSize: 4096,
                writeSize: 1024,
                numWrites: 10);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [PlatformSpecific(TestPlatforms.Windows)] // testing undocumented feature that's legacy in the Windows implementation
        [MemberData(nameof(MemberData_FileStreamAsyncWriting))]
        [OuterLoop] // many combinations: we test just one in inner loop and the rest outer
        public async Task ManyConcurrentWriteAsyncs_OuterLoop(
            bool useAsync, bool presize, bool exposeHandle, bool cancelable, int bufferSize, int writeSize, int numWrites)
        {
            long totalLength = writeSize * numWrites;
            var expectedData = new byte[totalLength];
            new Random(42).NextBytes(expectedData);
            CancellationToken cancellationToken = cancelable ? new CancellationTokenSource().Token : CancellationToken.None;

            string path = GetTestFilePath();
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, useAsync))
            {
                if (presize)
                {
                    fs.SetLength(totalLength);
                }
                if (exposeHandle)
                {
                    _ = fs.SafeFileHandle;
                }

                Task[] writes = new Task[numWrites];
                for (int i = 0; i < numWrites; i++)
                {
                    writes[i] = WriteAsync(fs, expectedData, i * writeSize, writeSize, cancellationToken);
                    Assert.Null(writes[i].Exception);
                    if (useAsync)
                    {
                        // To ensure that the buffer of a FileStream opened for async IO is flushed
                        // by FlushAsync in asynchronous way, we aquire a lock for every buffered WriteAsync.
                        // The side effect of this is that the Position of FileStream is not updated until
                        // the lock is released by a previous operation.
                        // So now all WriteAsync calls should be awaited before starting another async file operation.
                        if (PlatformDetection.IsNet5CompatFileStreamEnabled)
                        {
                            Assert.Equal((i + 1) * writeSize, fs.Position);
                        }
                    }
                }

                await Task.WhenAll(writes);
            }

            byte[] actualData = File.ReadAllBytes(path);
            Assert.Equal(expectedData.Length, actualData.Length);
            if (useAsync)
            {
                Assert.Equal<byte>(expectedData, actualData);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BufferCorrectlyMaintainedWhenReadAndWrite(bool useAsync)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, TestBuffer);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 2, useAsync))
            {
                Assert.Equal(TestBuffer[0], await ReadByteAsync(fs));
                Assert.Equal(TestBuffer[1], await ReadByteAsync(fs));
                Assert.Equal(TestBuffer[2], await ReadByteAsync(fs));
                await WriteAsync(fs, TestBuffer, 0, TestBuffer.Length);

                fs.Position = 0;
                Assert.Equal(TestBuffer[0], await ReadByteAsync(fs));
                Assert.Equal(TestBuffer[1], await ReadByteAsync(fs));
                Assert.Equal(TestBuffer[2], await ReadByteAsync(fs));
                for (int i = 0; i < TestBuffer.Length; i++)
                {
                    Assert.Equal(TestBuffer[i], await ReadByteAsync(fs));
                }
            }
        }

        private static async Task<byte> ReadByteAsync(FileStream fs)
        {
            byte[] oneByte = new byte[1];
            Assert.Equal(1, await fs.ReadAsync(oneByte, 0, 1));
            return oneByte[0];
        }

        [Fact, OuterLoop]
        public async Task WriteAsyncMiniStress()
        {
            TimeSpan testRunTime = TimeSpan.FromSeconds(10);
            const int MaximumWriteSize = 16 * 1024;
            const int NormalWriteSize = 4 * 1024;

            Random rand = new Random();
            DateTime testStartTime = DateTime.UtcNow;

            // Generate data to write (NOTE: Randomizing this is important as some file systems may optimize writing 0s)
            byte[] dataToWrite = new byte[MaximumWriteSize];
            rand.NextBytes(dataToWrite);

            string writeFileName = GetTestFilePath();
            do
            {

                int totalBytesWritten = 0;

                using (var stream = new FileStream(writeFileName, FileMode.Create, FileAccess.Write))
                {
                    do
                    {
                        // 20%: random write size
                        int bytesToWrite = (rand.NextDouble() < 0.2 ? rand.Next(16, MaximumWriteSize) : NormalWriteSize);

                        if (rand.NextDouble() < 0.1)
                        {
                            // 10%: Sync write
                            stream.Write(dataToWrite, 0, bytesToWrite);
                        }
                        else
                        {
                            // 90%: Async write
                            await WriteAsync(stream, dataToWrite, 0, bytesToWrite);
                        }

                        totalBytesWritten += bytesToWrite;
                    // Cap written bytes at 10 million to avoid writing too much to disk
                    } while (totalBytesWritten < 10_000_000);
                }
            } while (DateTime.UtcNow - testStartTime <= testRunTime);
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_WriteAsync_AsyncWrites : FileStream_AsyncWrites
    {
        protected override Task WriteAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.WriteAsync(buffer, offset, count, cancellationToken);

        [Fact]
        public void CancelledTokenFastPath()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken cancelledToken = cts.Token;

            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                FSAssert.IsCancelled(WriteAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                // before read only check
                FSAssert.IsCancelled(WriteAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);

                fs.Dispose();
                // before disposed check
                FSAssert.IsCancelled(WriteAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);

                // out of bounds checking happens first
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[2], 1, 2, cancelledToken)));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], 0, -1, cancelledToken)));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, new byte[1], -1, -1, cancelledToken)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(WriteAsync(fs, null, -1, -1, cancelledToken)));
            }
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class FileStream_BeginEndWrite_AsyncWrites : FileStream_AsyncWrites
    {
        protected override Task WriteAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(
                (callback, state) => stream.BeginWrite(buffer, offset, count, callback, state),
                iar => stream.EndWrite(iar),
                null);
    }
}
