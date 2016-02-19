// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class AnonymousPipeTest_Read_ServerIn_ClientOut : AnonymousPipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.readablePipe = new AnonymousPipeServerStream(PipeDirection.In);
            ret.writeablePipe = new AnonymousPipeClientStream(PipeDirection.Out, ((AnonymousPipeServerStream)ret.readablePipe).ClientSafePipeHandle);
            return ret;
        }
    }

    public class AnonymousPipeTest_Read_ServerOut_ClientIn : AnonymousPipeTest_Read
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.writeablePipe = new AnonymousPipeServerStream(PipeDirection.Out);
            ret.readablePipe = new AnonymousPipeClientStream(PipeDirection.In, ((AnonymousPipeServerStream)ret.writeablePipe).ClientSafePipeHandle);
            return ret;
        }
    }

    public abstract class AnonymousPipeTest_Read : PipeTest_Read
    {
        [Theory]
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
    }

}
