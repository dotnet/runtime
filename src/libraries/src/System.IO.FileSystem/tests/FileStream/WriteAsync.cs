// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_WriteAsync : FileSystemTest
    {
        [Fact]
        public void NullBufferThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, 0, 1)));
            }
        }

        [Fact]
        public void NegativeOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], -1, 1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, -1, 1)));
            }
        }

        [Fact]
        public void NegativeCountThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, -1)));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, -1, -1)));
            }
        }

        [Fact]
        public void BufferOutOfBoundsThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 1, 1)));

                // offset out of bounds for 0 count WriteAsync
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 2, 0)));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[0], 1, 0)));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[2], 1, 2)));

                // edges
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[0], int.MaxValue, 0)));
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[0], int.MaxValue, int.MaxValue)));
            }
        }

        [Fact]
        public void WriteAsyncDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, 1)));
                // even for noop WriteAsync
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, 0)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[2], 1, 2)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, -1)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, -1, -1)));
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
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, 1)));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, 1)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[2], 1, 2)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, -1)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, -1, -1)));
            }
        }

        [Fact]
        public void CancelledTokenFastPath()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken cancelledToken = cts.Token;
            
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                FSAssert.IsCancelled(fs.WriteAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                // before read only check
                FSAssert.IsCancelled(fs.WriteAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);

                fs.Dispose();
                // before disposed check
                FSAssert.IsCancelled(fs.WriteAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[2], 1, 2, cancelledToken)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], 0, -1, cancelledToken)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(new byte[1], -1, -1, cancelledToken)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.WriteAsync(null, -1, -1, cancelledToken)));
            }
        }

        [Fact]
        public async Task NoopWriteAsyncsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // note that these do not succeed synchronously even though they do nothing.

                await fs.WriteAsync(new byte[0], 0, 0);
                await fs.WriteAsync(new byte[1], 0, 0);
                // even though offset is out of bounds of buffer, this is still allowed
                // for the last element
                await fs.WriteAsync(new byte[1], 1, 0);
                await fs.WriteAsync(new byte[2], 1, 0);
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }
        
        [Fact]
        [ActiveIssue("Buffering needs to be fixed")]
        public void WriteAsyncBufferedCompletesSynchronously()
        {
            // It doesn't make sense to spin up a background thread just to do a memcpy.

            // This isn't working now for useAsync:true since we always have a usercallback 
            // that get's run on the threadpool (see Win32FileStream.EndWriteTask)

            // This isn't working now for useAsync:false since we always call
            // Stream.WriteAsync that queues Read on a background thread
            foreach (bool useAsync in new[] { true, false })
            {
                using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, TestBuffer.Length * 2, useAsync))
                {
                    byte[] buffer = new byte[TestBuffer.Length];

                    // Existing issue: FileStreamAsyncResult doesn't set CompletedSynchronously correctly.

                    // write should now complete synchronously since it is just copying to the write buffer
                    FSAssert.CompletesSynchronously(fs.WriteAsync(buffer, 0, buffer.Length));
                }
            }
        }

        [Fact]
        public async Task SimpleWriteAsync()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                await fs.WriteAsync(TestBuffer, 0, TestBuffer.Length);
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public async Task WriteAsyncCancelledFile()
        {
            const int writeSize = 1024 * 1024;
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                byte[] buffer = new byte[writeSize];
                CancellationTokenSource cts = new CancellationTokenSource();
                Task writeTask = fs.WriteAsync(buffer, 0, buffer.Length, cts.Token);
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
                }
            }
        }

        [Fact]
        public async void WriteAsyncInternalBufferOverflow()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write, FileShare.None, 3, useAsync: true))
            {
                // Fill buffer; should trigger flush of full buffer, no additional I/O
                await fs.WriteAsync(TestBuffer, 0, 3);
                Assert.True(fs.Length == 3);

                // Add to next buffer
                await fs.WriteAsync(TestBuffer, 0, 1);
                Assert.True(fs.Length == 4);

                // Complete that buffer; should trigger flush of full buffer, no additional I/O
                await fs.WriteAsync(TestBuffer, 0, 2);
                Assert.True(fs.Length == 6);

                // Add to next buffer
                await fs.WriteAsync(TestBuffer, 0, 2);
                Assert.True(fs.Length == 8);

                // Overflow buffer with amount that could fit in a buffer; should trigger a flush, with additional I/O
                await fs.WriteAsync(TestBuffer, 0, 2);
                Assert.True(fs.Length == 10);

                // Overflow buffer with amount that couldn't fit in a buffer; shouldn't be anything to flush, just an additional I/O
                await fs.WriteAsync(TestBuffer, 0, 4);
                Assert.True(fs.Length == 14);
            }
        }

        public static IEnumerable<object[]> MemberData_FileStreamAsyncWriting()
        {
            foreach (bool useAsync in new[] { true, false })
            {
                if (useAsync && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // [ActiveIssue(812, PlatformID.AnyUnix)]
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

        [Fact]
        public Task ManyConcurrentWriteAsyncs()
        {
            // For inner loop, just test one case
            return ManyConcurrentWriteAsyncs(
                useAsync: RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                presize: false,
                exposeHandle: false,
                cancelable: true,
                bufferSize: 4096,
                writeSize: 1024,
                numWrites: 10);
        }

        [Theory]
        [MemberData("MemberData_FileStreamAsyncWriting")]
        [OuterLoop] // many combinations: we test just one in inner loop and the rest outer
        public async Task ManyConcurrentWriteAsyncs(
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
                    var ignored = fs.SafeFileHandle;
                }

                Task[] writes = new Task[numWrites];
                for (int i = 0; i < numWrites; i++)
                {
                    writes[i] = fs.WriteAsync(expectedData, i * writeSize, writeSize, cancellationToken);
                    Assert.Null(writes[i].Exception);
                    if (useAsync)
                    {
                        Assert.Equal((i + 1) * writeSize, fs.Position);
                    }
                }

                await Task.WhenAll(writes);
            }

            byte[] actualData = File.ReadAllBytes(path);
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal<byte>(expectedData, actualData);
        }

        [Fact]
        public Task CopyToAsyncBetweenFileStreams()
        {
            // For inner loop, just test one case
            return CopyToAsyncBetweenFileStreams(
                useAsync: RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                preSize: false,
                exposeHandle: false,
                cancelable: true,
                bufferSize: 4096,
                writeSize: 1024,
                numWrites: 10);
        }

        [Fact]
        public void CopyToAsync_InvalidArgs_Throws()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentNullException>("destination", () => { fs.CopyToAsync(null); });
                Assert.Throws<ArgumentOutOfRangeException>("bufferSize", () => { fs.CopyToAsync(new MemoryStream(), 0); });
                Assert.Throws<NotSupportedException>(() => { fs.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => { fs.CopyToAsync(new MemoryStream()); });
            }
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => { fs.CopyToAsync(new MemoryStream()); });
            }
        }

        [Theory]
        [MemberData("MemberData_FileStreamAsyncWriting")]
        [OuterLoop] // many combinations: we test just one in inner loop and the rest outer
        public async Task CopyToAsyncBetweenFileStreams(
            bool useAsync, bool preSize, bool exposeHandle, bool cancelable, int bufferSize, int writeSize, int numWrites)
        {
            long totalLength = writeSize * numWrites;
            var expectedData = new byte[totalLength];
            new Random(42).NextBytes(expectedData);

            string srcPath = GetTestFilePath();
            File.WriteAllBytes(srcPath, expectedData);

            string dstPath = GetTestFilePath();
            using (FileStream src = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, useAsync))
            using (FileStream dst = new FileStream(dstPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync))
            {
                await src.CopyToAsync(dst, writeSize, cancelable ? new CancellationTokenSource().Token : CancellationToken.None);
            }

            byte[] actualData = File.ReadAllBytes(dstPath);
            Assert.Equal(expectedData.Length, actualData.Length);
            Assert.Equal<byte>(expectedData, actualData);
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
                await fs.WriteAsync(TestBuffer, 0, TestBuffer.Length);

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
            TimeSpan testRunTime = TimeSpan.FromSeconds(30);
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
                // Create a new token that expires between 100-1000ms
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(rand.Next(100, 1000));

                using (var stream = new FileStream(writeFileName, FileMode.Create, FileAccess.Write))
                {                    
                    do
                    {
                        try
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
                                await stream.WriteAsync(dataToWrite, 0, bytesToWrite, tokenSource.Token);
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            Assert.True(tokenSource.Token.IsCancellationRequested, "Received cancellation exception before token expired");
                        }
                    } while (!tokenSource.Token.IsCancellationRequested);
                }
            } while (DateTime.UtcNow - testStartTime <= testRunTime);
        }
    }
}
