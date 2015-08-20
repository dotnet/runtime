// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
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
        [Fact]
        public void ReadWithNullBuffer_Throws_ArgumentNullException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.True(pipe.CanRead);

                // Null is an invalid Buffer
                Assert.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, 0, 1));
                Assert.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, 0, 1); });

                // Buffer validity is checked before Offset
                Assert.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, 1));
                Assert.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, 1); });

                // Buffer validity is checked before Count
                Assert.Throws<ArgumentNullException>("buffer", () => pipe.Read(null, -1, -1));
                Assert.Throws<ArgumentNullException>("buffer", () => { pipe.ReadAsync(null, -1, -1); });
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
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => pipe.Read(new byte[6], -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => { pipe.ReadAsync(new byte[4], -1, 1); });
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
                Assert.Throws<ArgumentOutOfRangeException>("count", () => pipe.Read(new byte[3], 0, -1));
                Assert.Throws<System.ArgumentOutOfRangeException>("count", () => { pipe.ReadAsync(new byte[7], 0, -1); });
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
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], 1, 0));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[2], 1, 2));

                // edges
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, 0));
                Assert.Throws<ArgumentException>(null, () => pipe.Read(new byte[0], int.MaxValue, int.MaxValue));

                Assert.Throws<ArgumentException>(() => pipe.Read(new byte[5], 3, 4));

                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 1, 1); });

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[1], 2, 0); });

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], 1, 0); });

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[2], 1, 2); });

                // edges
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, 0); });
                Assert.Throws<ArgumentException>(null, () => { pipe.ReadAsync(new byte[0], int.MaxValue, int.MaxValue); });

                Assert.Throws<ArgumentException>(() => { pipe.ReadAsync(new byte[5], 3, 4); });
            }
        }

        [Fact]
        public virtual void WriteToReadOnlyPipe_Throws_NotSupportedException()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                PipeStream pipe = pair.readablePipe;
                Assert.True(pipe.IsConnected);
                Assert.False(pipe.CanWrite);

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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        [InlineData(5000, 1, 1)]       // very small buffers
        [InlineData(500, 21, 18)]      // lots of iterations, with read buffer smaller than write buffer
        [InlineData(500, 18, 21)]      // lots of iterations, with write buffer smaller than read buffer
        [InlineData(5, 128000, 64000)] // very large buffers
        public async Task AsyncReadWriteChain(int iterations, int writeBufferSize, int readBufferSize)
        {
            var writeBuffer = new byte[writeBufferSize];
            var readBuffer = new byte[readBufferSize];
            var rand = new Random();

            using (ServerClientPair pair = CreateServerClientPair())
            {
                // Repeatedly and asynchronously write to the writeable pipe and read from the readable pipe,
                // verifying that the correct data made it through.
                for (int iter = 0; iter < iterations; iter++)
                {
                    rand.NextBytes(writeBuffer);
                    Task writerTask = pair.writeablePipe.WriteAsync(writeBuffer, 0, writeBuffer.Length);

                    int totalRead = 0;
                    while (totalRead < writeBuffer.Length)
                    {
                        int numRead = await pair.readablePipe.ReadAsync(readBuffer, 0, readBuffer.Length);
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
