// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Tests that cover Read and ReadAsync behaviors that are shared between
    /// AnonymousPipes and NamedPipes
    /// </summary>
    public abstract partial class PipeTest_Read : PipeTestBase
    {
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
    }
}
