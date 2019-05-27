// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public abstract partial class PipeTest_Read : PipeTestBase
    {
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
