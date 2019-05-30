// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_AsyncReads : FileSystemTest
    {
        protected virtual string BufferParamName => "buffer";
        protected virtual string OffsetParamName => "offset";
        protected virtual string CountParamName => "count";
        protected abstract Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        private Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count) =>
            ReadAsync(stream, buffer, offset, count, CancellationToken.None);

        [Fact]
        public void NullBufferThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, 0, 1)));
            }
        }

        [Fact]
        public void NegativeOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(OffsetParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], -1, 1)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, -1, 1)));
            }
        }

        [Fact]
        public void NegativeCountThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(CountParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, -1)));

                // offset is checked before count
                AssertExtensions.Throws<ArgumentOutOfRangeException>(OffsetParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], -1, -1)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, -1, -1)));
            }
        }

        [Fact]
        public void BufferOutOfBoundsThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 1, 1)));

                // offset out of bounds for 0 count ReadAsync
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 2, 0)));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[0], 1, 0)));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[2], 1, 2)));

                // edges
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[0], int.MaxValue, 0)));
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[0], int.MaxValue, int.MaxValue)));
            }
        }

        [Fact]
        public void ReadAsyncDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, 1)));
                // even for noop ReadAsync
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, 0)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[2], 1, 2)));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(CountParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, -1)));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(OffsetParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], -1, -1)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, -1, -1)));
            }
        }

        [Fact]
        public void WriteOnlyThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, 1)));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, 1)));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[2], 1, 2)));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(CountParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, -1)));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(OffsetParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], -1, -1)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () => 
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, -1, -1)));
            }
        }

        [Fact]
        public async Task NoopReadAsyncsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // note that these do not succeed synchronously even though they do nothing.

                Assert.Equal(0, await ReadAsync(fs, new byte[0], 0, 0));
                Assert.Equal(0, await ReadAsync(fs, new byte[1], 0, 0));
                // even though offset is out of bounds of buffer, this is still allowed
                // for the last element
                Assert.Equal(0, await ReadAsync(fs, new byte[1], 1, 0));
                Assert.Equal(0, await ReadAsync(fs, new byte[2], 1, 0));
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

                Array.Clear(buffer, 0, buffer.Length);

                // read should now complete synchronously since it is serviced by the read buffer filled in the first request
                Assert.Equal(TestBuffer.Length, FSAssert.CompletesSynchronously(ReadAsync(fs, buffer, 0, buffer.Length)));
                Assert.Equal(TestBuffer, buffer);
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
                Assert.Equal(TestBuffer.Length, await ReadAsync(fs, buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                // ReadAsync with too large buffer at front of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await ReadAsync(fs, buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer.Take(TestBuffer.Length));
                // Remainder of buffer should be untouched.
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length], buffer.Skip(TestBuffer.Length));

                // ReadAsync with too large buffer in middle of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, await ReadAsync(fs, buffer, 2, buffer.Length - 2));
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
                Task<int> readTask = ReadAsync(fs, buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();
                try
                {
                    await readTask;
                    // we may not have canceled before the task completed.
                }
                catch(OperationCanceledException oce)
                {
                    // Ideally we'd be doing an Assert.Throws<OperationCanceledException>
                    // but since cancellation is a race condition we accept either outcome
                    Assert.Equal(cts.Token, oce.CancellationToken);
                }
            }
        }

        [Fact, OuterLoop]
        public async Task ReadAsyncMiniStress()
        {
            TimeSpan testRunTime = TimeSpan.FromSeconds(10);
            const int MaximumReadSize = 16 * 1024;
            const int NormalReadSize = 4 * 1024;

            Random rand = new Random();
            DateTime testStartTime = DateTime.UtcNow;

            // Generate file data
            byte[] readableFileContents = new byte[MaximumReadSize * 16];
            rand.NextBytes(readableFileContents);

            // Create and fill file
            string readableFilePath = GetTestFilePath();
            using (var stream = new FileStream(readableFilePath, FileMode.CreateNew, FileAccess.Write))
            {
                await stream.WriteAsync(readableFileContents, 0, readableFileContents.Length);
            }

            using (var stream = new FileStream(readableFilePath, FileMode.Open, FileAccess.Read))
            {
                // Create a new token that expires between 100-1000ms
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(rand.Next(100, 1000));

                int currentPosition = 0;
                byte[] buffer = new byte[MaximumReadSize];
                do
                {
                    try
                    {
                        // 20%: random read size
                        int bytesToRead = (rand.NextDouble() < 0.2 ? rand.Next(16, MaximumReadSize) : NormalReadSize);

                        int bytesRead;
                        if (rand.NextDouble() < 0.1)
                        {
                            // 10%: Sync read
                            bytesRead = stream.Read(buffer, 0, bytesToRead);
                        }
                        else
                        {
                            // 90%: Async read
                            bytesRead = await ReadAsync(stream, buffer, 0, bytesToRead, tokenSource.Token);
                        }

                        // 10%: Verify data (burns a lot of CPU time)
                        if (rand.NextDouble() < 0.1)
                        {
                            // Validate data read
                            Assert.True(bytesRead + currentPosition <= readableFileContents.Length, "Too many bytes read");
                            Assert.Equal(readableFileContents.Skip(currentPosition).Take(bytesRead), buffer.Take(bytesRead));
                        }

                        // Advance position and reset if we are at the end
                        currentPosition += bytesRead;
                        if (currentPosition >= readableFileContents.Length)
                        {
                            currentPosition = 0;
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Once the token has expired, generate a new one
                        Assert.True(tokenSource.Token.IsCancellationRequested, "Received cancellation exception before token expired");
                        tokenSource = new CancellationTokenSource();
                        tokenSource.CancelAfter(rand.Next(100, 1000));
                    }
                } while (DateTime.UtcNow - testStartTime <= testRunTime);
            }
        }
    }

    public class FileStream_ReadAsync_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            stream.ReadAsync(buffer, offset, count, cancellationToken);

        [Fact]
        public void CancelledTokenFastPath()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken cancelledToken = cts.Token;

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                FSAssert.IsCancelled(ReadAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);
            }

            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                // before write only check
                FSAssert.IsCancelled(ReadAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);

                fs.Dispose();
                // before disposed check
                FSAssert.IsCancelled(ReadAsync(fs, new byte[1], 0, 1, cancelledToken), cancelledToken);

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () =>
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[2], 1, 2, cancelledToken)));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(CountParamName, () =>
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], 0, -1, cancelledToken)));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>(OffsetParamName, () =>
                    FSAssert.CompletesSynchronously(ReadAsync(fs, new byte[1], -1, -1, cancelledToken)));

                // buffer is checked first
                AssertExtensions.Throws<ArgumentNullException>(BufferParamName, () =>
                    FSAssert.CompletesSynchronously(ReadAsync(fs, null, -1, -1, cancelledToken)));
            }
        }
    }

    public class FileStream_BeginEndRead_AsyncReads : FileStream_AsyncReads
    {
        protected override Task<int> ReadAsync(FileStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.Factory.FromAsync(
                (callback, state) => stream.BeginRead(buffer, offset, count, callback, state),
                iar => stream.EndRead(iar),
                null);

        protected override string BufferParamName => "array";
        protected override string CountParamName => "numBytes";
    }
}
