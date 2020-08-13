// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests that cover Read and ReadAsync behaviors that are shared between
    /// AnonymousPipes and NamedPipes
    /// </summary>
    public abstract class PipeTest_Read : PipeTestBase
    {
        public virtual bool SupportsBidirectionalReadingWriting => false;

        [Fact]
        public void ReadWithNullBuffer_Throws_ArgumentNullException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanRead);

                // Null is an invalid Buffer
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, 0, 1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, 0, 1); });

                // Buffer validity is checked before Offset
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, 1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, 1); });

                // Buffer validity is checked before Count
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, -1));
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, -1); });
            }
        }

        [Fact]
        public void ReadWithNegativeOffset_Throws_ArgumentOutOfRangeException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanRead);

                // Offset must be nonnegative
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Read(new byte[6], -1, 1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.ReadAsync(new byte[4], -1, 1); });
            }
        }

        [Fact]
        public void ReadWithNegativeCount_Throws_ArgumentOutOfRangeException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanRead);

                // Count must be nonnegative
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => pipe.Read(new byte[3], 0, -1));
                AssertExtensions.Throws<System.ArgumentOutOfRangeException>("count", () => { pipe.ReadAsync(new byte[7], 0, -1); });
            }
        }

        [Fact]
        public void ReadWithOutOfBoundsArray_Throws_ArgumentException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanRead);

                // offset out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], 1, 0));

                // combination offset and count out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[2], 1, 2));

                // edges
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, int.MaxValue));

                AssertExtensions.Throws<ArgumentException>(null, () => pipe.Read(new byte[5], 3, 4));

                // offset out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 1, 1); });

                // offset out of bounds for 0 count read
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 2, 0); });

                // offset out of bounds even for 0 length buffer
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], 1, 0); });

                // combination offset and count out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[2], 1, 2); });

                // edges
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, 0); });
                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, int.MaxValue); });

                AssertExtensions.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[5], 3, 4); });
            }
        }

        [Fact]
        public void WriteToReadOnlyPipe_Throws_NotSupportedException()
        {
            if (SupportsBidirectionalReadingWriting)
            {
                return;
            }

            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.False(pipe.CanWrite);
                Assert.False(pipe.CanSeek);

                Assert.Throws<NotSupportedException>(() => pipe.Write(new byte[5], 0, 5));

                Assert.Throws<NotSupportedException>(() => pipe.WriteByte(123));

                Assert.Throws<NotSupportedException>(() => pipe.Flush());

                Assert.Throws<NotSupportedException>(() => pipe.OutBufferSize);

                Assert.Throws<NotSupportedException>(() => pipe.WaitForPipeDrain());

                Assert.Throws<NotSupportedException>(() => { pipe.WriteAsync(new byte[5], 0, 5); });
            }
        }

        [Fact]
        public async Task ReadWithZeroLengthBuffer_Nop()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                var buffer = new byte[] { };

                Assert.Equal(0, pipe.Read(buffer, 0, 0));
                Task<int> read = pipe.ReadAsync(buffer, 0, 0);
                Assert.Equal(0, await read);
            }
        }

        [Fact]
        public void ReadPipeUnsupportedMembers_Throws_NotSupportedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);

                Assert.Throws<NotSupportedException>(() => pipe.Length);

                Assert.Throws<NotSupportedException>(() => pipe.SetLength(10L));

                Assert.Throws<NotSupportedException>(() => pipe.Position);

                Assert.Throws<NotSupportedException>(() => pipe.Position = 10L);

                Assert.Throws<NotSupportedException>(() => pipe.Seek(10L, System.IO.SeekOrigin.Begin));
            }
        }

        [Fact]
        public void ReadOnDisposedReadablePipe_Throws_ObjectDisposedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                pipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Assert.Throws<ObjectDisposedException>(() => pipe.Flush());
                Assert.Throws<ObjectDisposedException>(() => pipe.Read(buffer, 0, buffer.Length));
                Assert.Throws<ObjectDisposedException>(() => pipe.ReadByte());
                Assert.Throws<ObjectDisposedException>(() => { pipe.ReadAsync(buffer, 0, buffer.Length); });
                Assert.Throws<ObjectDisposedException>(() => pipe.IsMessageComplete);
                Assert.Throws<ObjectDisposedException>(() => pipe.ReadMode);
            }
        }

        [Fact]
        public void CopyToAsync_InvalidArgs_Throws()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                AssertExtensions.Throws<ArgumentNullException>("destination", () => { pair.readablePipe.CopyToAsync(null); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => { pair.readablePipe.CopyToAsync(new MemoryStream(), 0); });
                Assert.Throws<NotSupportedException>(() => { pair.readablePipe.CopyToAsync(new MemoryStream(new byte[1], writable: false)); });
                if (!pair.writeablePipe.CanRead)
                {
                    Assert.Throws<NotSupportedException>(() => { pair.writeablePipe.CopyToAsync(new MemoryStream()); });
                }
            }
        }

        [Fact]
        public virtual async Task ReadFromPipeWithClosedPartner_ReadNoBytes()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                pair.writeablePipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                // The pipe won't be marked as Broken until the first read, so prime it
                // to test both the case where it's not yet marked as "Broken" and then
                // where it is.
                Assert.Equal(0, pair.readablePipe.Read(buffer, 0, buffer.Length));

                Assert.Equal(0, pair.readablePipe.Read(buffer, 0, buffer.Length));
                Assert.Equal(-1, pair.readablePipe.ReadByte());
                Assert.Equal(0, await pair.readablePipe.ReadAsync(buffer, 0, buffer.Length));
            }
        }

        [Fact]
        public async Task ValidWriteAsync_ValidReadAsync()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.True(pair.writeablePipe.IsConnected);
                Assert.True(pair.readablePipe.IsConnected);

                byte[] sent = new byte[] { 123, 0, 5 };
                byte[] received = new byte[] { 0, 0, 0 };

                Task write = pair.writeablePipe.WriteAsync(sent, 0, sent.Length);
                Assert.Equal(sent.Length, await pair.readablePipe.ReadAsync(received, 0, sent.Length));
                Assert.Equal(sent, received);
                await write;
            }
        }

        [Fact]
        public void ValidWrite_ValidRead()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.True(pair.writeablePipe.IsConnected);
                Assert.True(pair.readablePipe.IsConnected);

                byte[] sent = new byte[] { 123, 0, 5 };
                byte[] received = new byte[] { 0, 0, 0 };

                Task.Run(() => { pair.writeablePipe.Write(sent, 0, sent.Length); });
                Assert.Equal(sent.Length, pair.readablePipe.Read(received, 0, sent.Length));
                Assert.Equal(sent, received);
                if (OperatingSystem.IsWindows()) // WaitForPipeDrain isn't supported on Unix
                    pair.writeablePipe.WaitForPipeDrain();
            }
        }

        [Fact]
        public void ValidWriteByte_ValidReadByte()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.True(pair.writeablePipe.IsConnected);
                Assert.True(pair.readablePipe.IsConnected);

                Task.Run(() => pair.writeablePipe.WriteByte(123));
                Assert.Equal(123, pair.readablePipe.ReadByte());
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(AsyncReadWriteChain_MemberData))]
        public async Task AsyncReadWriteChain_ReadWrite(int iterations, int writeBufferSize, int readBufferSize, bool cancelableToken)
        {
            var writeBuffer = new byte[writeBufferSize];
            var readBuffer = new byte[readBufferSize];
            var rand = new Random();
            var cancellationToken = cancelableToken ? new CancellationTokenSource().Token : CancellationToken.None;

            using (ServerClientPair pair = CreateServerClientPair())
            {
                // Repeatedly and asynchronously write to the writable pipe and read from the readable pipe,
                // verifying that the correct data made it through.
                for (int iter = 0; iter < iterations; iter++)
                {
                    rand.NextBytes(writeBuffer);
                    Task writerTask = pair.writeablePipe.WriteAsync(writeBuffer, 0, writeBuffer.Length, cancellationToken);

                    int totalRead = 0;
                    while (totalRead < writeBuffer.Length)
                    {
                        int numRead = await pair.readablePipe.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                        Assert.True(numRead > 0);
                        Assert.Equal<byte>(
                            new ArraySegment<byte>(writeBuffer, totalRead, numRead),
                            new ArraySegment<byte>(readBuffer, 0, numRead));
                        totalRead += numRead;
                    }
                    Assert.Equal(writeBuffer.Length, totalRead);

                    await writerTask;
                }
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(AsyncReadWriteChain_MemberData))]
        public async Task AsyncReadWriteChain_CopyToAsync(int iterations, int writeBufferSize, int readBufferSize, bool cancelableToken)
        {
            var writeBuffer = new byte[writeBufferSize * iterations];
            new Random().NextBytes(writeBuffer);
            var cancellationToken = cancelableToken ? new CancellationTokenSource().Token : CancellationToken.None;

            using (ServerClientPair pair = CreateServerClientPair())
            {
                var readData = new MemoryStream();
                Task copyTask = pair.readablePipe.CopyToAsync(readData, readBufferSize, cancellationToken);

                for (int iter = 0; iter < iterations; iter++)
                {
                    await pair.writeablePipe.WriteAsync(writeBuffer, iter * writeBufferSize, writeBufferSize, cancellationToken);
                }
                pair.writeablePipe.Dispose();

                await copyTask;
                Assert.Equal(writeBuffer.Length, readData.Length);
                Assert.Equal(writeBuffer, readData.ToArray());
            }
        }

        public static IEnumerable<object[]> AsyncReadWriteChain_MemberData()
        {
            foreach (bool cancelableToken in new[] { true, false })
            {
                yield return new object[] { 5000, 1, 1, cancelableToken };  // very small buffers
                yield return new object[] { 500, 21, 18, cancelableToken }; // lots of iterations, with read buffer smaller than write buffer
                yield return new object[] { 500, 18, 21, cancelableToken }; // lots of iterations, with write buffer smaller than read buffer
                yield return new object[] { 5, 128000, 64000, cancelableToken }; // very large buffers
            }
        }

        [Fact]
        public async Task ValidWriteAsync_ValidReadAsync_APM()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.True(pair.writeablePipe.IsConnected);
                Assert.True(pair.readablePipe.IsConnected);

                byte[] sent = new byte[] { 123, 0, 5 };
                byte[] received = new byte[] { 0, 0, 0 };

                Task write = Task.Factory.FromAsync<byte[], int, int>(pair.writeablePipe.BeginWrite, pair.writeablePipe.EndWrite, sent, 0, sent.Length, null);
                Task<int> read = Task.Factory.FromAsync<byte[], int, int, int>(pair.readablePipe.BeginRead, pair.readablePipe.EndRead, received, 0, received.Length, null);
                Assert.Equal(sent.Length, await read);
                Assert.Equal(sent, received);
                await write;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(AsyncReadWriteChain_MemberData))]
        public async Task AsyncReadWriteChain_ReadWrite_APM(int iterations, int writeBufferSize, int readBufferSize, bool cancelableToken)
        {
            var writeBuffer = new byte[writeBufferSize];
            var readBuffer = new byte[readBufferSize];
            var rand = new Random();
            var cancellationToken = cancelableToken ? new CancellationTokenSource().Token : CancellationToken.None;

            using (ServerClientPair pair = CreateServerClientPair())
            {
                // Repeatedly and asynchronously write to the writable pipe and read from the readable pipe,
                // verifying that the correct data made it through.
                for (int iter = 0; iter < iterations; iter++)
                {
                    rand.NextBytes(writeBuffer);
                    Task write = Task.Factory.FromAsync<byte[], int, int>(pair.writeablePipe.BeginWrite, pair.writeablePipe.EndWrite, writeBuffer, 0, writeBuffer.Length, null);

                    int totalRead = 0;
                    while (totalRead < writeBuffer.Length)
                    {
                        Task<int> read = Task.Factory.FromAsync<byte[], int, int, int>(pair.readablePipe.BeginRead, pair.readablePipe.EndRead, readBuffer, 0, readBuffer.Length, null);
                        int numRead = await read;
                        Assert.True(numRead > 0);
                        Assert.Equal<byte>(
                            new ArraySegment<byte>(writeBuffer, totalRead, numRead),
                            new ArraySegment<byte>(readBuffer, 0, numRead));
                        totalRead += numRead;
                    }
                    Assert.Equal(writeBuffer.Length, totalRead);

                    await write;
                }
            }
        }

        [Fact]
        public void WriteToReadOnlyPipe_Span_Throws_NotSupportedException()
        {
            if (SupportsBidirectionalReadingWriting)
            {
                return;
            }

            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.False(pipe.CanWrite);
                Assert.False(pipe.CanSeek);

                Assert.Throws<NotSupportedException>(() => pipe.Write(new ReadOnlySpan<byte>(new byte[5])));
                Assert.Throws<NotSupportedException>(() => { pipe.WriteAsync(new ReadOnlyMemory<byte>(new byte[5])); });
            }
        }

        [Fact]
        public async Task ReadWithZeroLengthBuffer_Span_Nop()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                var buffer = new byte[] { };

                Assert.Equal(0, pipe.Read(new Span<byte>(buffer)));
                ValueTask<int> read = pipe.ReadAsync(new Memory<byte>(buffer));
                Assert.Equal(0, await read);
            }
        }

        [Fact]
        public void ReadOnDisposedReadablePipe_Span_Throws_ObjectDisposedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                pipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Assert.Throws<ObjectDisposedException>(() => pipe.Read(new Span<byte>(buffer)));
                Assert.Throws<ObjectDisposedException>(() => { pipe.ReadAsync(new Memory<byte>(buffer)); });
            }
        }

        [Fact]
        public virtual async Task ReadFromPipeWithClosedPartner_Span_ReadNoBytes()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                pair.writeablePipe.Dispose();
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                // The pipe won't be marked as Broken until the first read, so prime it
                // to test both the case where it's not yet marked as "Broken" and then
                // where it is.
                Assert.Equal(0, pair.readablePipe.Read(new Span<byte>(buffer)));

                Assert.Equal(0, pair.readablePipe.Read(new Span<byte>(buffer)));
                Assert.Equal(0, await pair.readablePipe.ReadAsync(new Memory<byte>(buffer)));
            }
        }

        [Fact]
        public async Task ValidWriteAsync_Span_ValidReadAsync()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.True(pair.writeablePipe.IsConnected);
                Assert.True(pair.readablePipe.IsConnected);

                byte[] sent = new byte[] { 123, 0, 5 };
                byte[] received = new byte[] { 0, 0, 0 };

                ValueTask write = pair.writeablePipe.WriteAsync(new ReadOnlyMemory<byte>(sent));
                Assert.Equal(sent.Length, await pair.readablePipe.ReadAsync(new Memory<byte>(received, 0, sent.Length)));
                Assert.Equal(sent, received);
                await write;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(AsyncReadWriteChain_MemberData))]
        public async Task AsyncReadWriteChain_Span_ReadWrite(int iterations, int writeBufferSize, int readBufferSize, bool cancelableToken)
        {
            var writeBuffer = new byte[writeBufferSize];
            var readBuffer = new byte[readBufferSize];
            var rand = new Random();
            var cancellationToken = cancelableToken ? new CancellationTokenSource().Token : CancellationToken.None;

            using (ServerClientPair pair = CreateServerClientPair())
            {
                // Repeatedly and asynchronously write to the writable pipe and read from the readable pipe,
                // verifying that the correct data made it through.
                for (int iter = 0; iter < iterations; iter++)
                {
                    rand.NextBytes(writeBuffer);
                    ValueTask writerTask = pair.writeablePipe.WriteAsync(new ReadOnlyMemory<byte>(writeBuffer), cancellationToken);

                    int totalRead = 0;
                    while (totalRead < writeBuffer.Length)
                    {
                        int numRead = await pair.readablePipe.ReadAsync(new Memory<byte>(readBuffer), cancellationToken);
                        Assert.True(numRead > 0);
                        Assert.Equal<byte>(
                            new ArraySegment<byte>(writeBuffer, totalRead, numRead),
                            new ArraySegment<byte>(readBuffer, 0, numRead));
                        totalRead += numRead;
                    }
                    Assert.Equal(writeBuffer.Length, totalRead);

                    await writerTask;
                }
            }
        }
    }
}
