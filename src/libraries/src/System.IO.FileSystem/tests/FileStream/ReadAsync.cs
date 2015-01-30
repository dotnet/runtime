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
    public class FileStream_ReadAsync : FileSystemTest
    {
        [Fact]
        public void NullBufferThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentNullException>("buffer", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, 0, 1)));
            }
        }

        [Fact]
        public void NegativeOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], -1, 1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, -1, 1)));
            }
        }

        [Fact]
        public void NegativeCountThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<ArgumentOutOfRangeException>("count", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, -1)));

                // offset is checked before count
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, -1, -1)));
            }
        }

        [Fact]
        public void BufferOutOfBoundsThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 1, 1)));

                // offset out of bounds for 0 count ReadAsync
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 2, 0)));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[0], 1, 0)));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[2], 1, 2)));

                // edges
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[0], int.MaxValue, 0)));
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[0], int.MaxValue, int.MaxValue)));
            }
        }

        [Fact]
        public void ReadAsyncDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 1)));
                // even for noop ReadAsync
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 0)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[2], 1, 2)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, -1)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, -1, -1)));
            }
        }

        [Fact]
        public void WriteOnlyThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 1)));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, 1)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[2], 1, 2)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, -1)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], -1, -1)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () => 
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, -1, -1)));
            }
        }

        [Fact]
        public void CancelledTokenFastPath()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken cancelledToken = cts.Token;

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                FSAssert.IsCancelled(fs.ReadAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                // before write only check
                FSAssert.IsCancelled(fs.ReadAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);

                fs.Dispose();
                // before disposed check
                FSAssert.IsCancelled(fs.ReadAsync(new byte[1], 0, 1, cancelledToken), cancelledToken);

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[2], 1, 2, cancelledToken)));

                // count is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("count", () =>
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], 0, -1, cancelledToken)));

                // offset is checked prior
                Assert.Throws<ArgumentOutOfRangeException>("offset", () =>
                    FSAssert.CompletesSynchronously(fs.ReadAsync(new byte[1], -1, -1, cancelledToken)));

                // buffer is checked first
                Assert.Throws<ArgumentNullException>("buffer", () =>
                    FSAssert.CompletesSynchronously(fs.ReadAsync(null, -1, -1, cancelledToken)));
            }
        }

        [Fact]
        public async Task NoopReadAsyncsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // note that these do not succeed synchronously even though they do nothing.

                Assert.Equal(0, await fs.ReadAsync(new byte[0], 0, 0));
                Assert.Equal(0, await fs.ReadAsync(new byte[1], 0, 0));
                // even though offset is out of bounds of buffer, this is still allowed
                // for the last element
                Assert.Equal(0, await fs.ReadAsync(new byte[1], 1, 0));
                Assert.Equal(0, await fs.ReadAsync(new byte[2], 1, 0));
            }
        }

        [Fact]
        public async Task EmptyFileReadAsyncSucceedSynchronously()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                byte[] buffer = new byte[TestBuffer.Length];

                // use a recognizable pattern
                TestBuffer.CopyTo(buffer, 0);
                
                // note that these do not succeed synchronously even though they do nothing.
                Assert.Equal(0, await fs.ReadAsync(buffer, 0, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(buffer, buffer.Length - 1, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, await fs.ReadAsync(buffer, buffer.Length / 2, buffer.Length - buffer.Length / 2));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        [ActiveIssue("Buffering needs to be fixed")]
        public async Task ReadAsyncBufferedCompletesSynchronously()
        {
            // It doesn't make sense to spin up a background thread just to do a memcpy.
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            // This isn't working now for useAsync:true since we always have a usercallback 
            // that get's run on the threadpool (see Win32FileStream.EndReadTask)

            // This isn't working now for useAsync:false since we always call
            // Stream.ReadAsync that queues Read on a background thread
            foreach (bool useAsync in new[] { true, false })
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, TestBuffer.Length * 2, useAsync))
                {
                    byte[] buffer = new byte[TestBuffer.Length];

                    // Existing issue: FileStreamAsyncResult doesn't set CompletedSynchronously correctly.

                    // prime the internal buffer
                    Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 0, buffer.Length));
                    Assert.Equal(TestBuffer, buffer);

                    Array.Clear(buffer, 0, buffer.Length);

                    // read should now complete synchronously since it is serviced by the read buffer filled in the first request
                    Assert.Equal(TestBuffer.Length,
                        FSAssert.CompletesSynchronously(fs.ReadAsync(buffer, 0, buffer.Length)));
                    Assert.Equal(TestBuffer, buffer);
                }
            }
        }

        [Fact]
        public async Task ReadAsyncExistingFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                // ReadAsync with too large buffer at front of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer.Take(TestBuffer.Length));
                // Remainder of buffer should be untouched.
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length], buffer.Skip(TestBuffer.Length));

                // ReadAsync with too large buffer in middle of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await fs.ReadAsync(buffer, 2, buffer.Length - 2));
                Assert.Equal(TestBuffer, buffer.Skip(2).Take(TestBuffer.Length));
                // Remainder of buffer should be untouched.
                Assert.Equal(new byte[2], buffer.Take(2));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length - 2], buffer.Skip(2 + TestBuffer.Length));
            }
        }

        [Fact]
        public async Task ReadAsyncCancelledFile()
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
                Task<int> readTask = fs.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();
                try
                {
                    await readTask;
                    // we may not have cancelled before the task completed.
                }
                catch(OperationCanceledException oce)
                {
                    // Ideally we'd be doing an Assert.Throws<OperationCanceledException>
                    // but since cancellation is a race condition we accept either outcome
                    Assert.Equal(cts.Token, oce.CancellationToken);
                }
            }
        }
    }
}
