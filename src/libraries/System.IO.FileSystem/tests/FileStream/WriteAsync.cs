// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.FileSystem.Tests
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
