// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Contains helper methods specifically for Named pipes
    /// </summary>
    public class NamedPipeTestBase : PipeTestBase
    {
        /// <summary>
        /// Represents a Server-Client pair in which both pipes are specifically
        /// Named Pipes. Used for tests that do not have an AnonymousPipe equivalent
        /// </summary>
        public class NamedPipePair : IDisposable
        {
            public NamedPipeServerStream serverStream;
            public NamedPipeClientStream clientStream;
            public bool writeToServer;     // True if the serverStream should be written to in one-way tests

            public void Dispose()
            {
                if (clientStream != null)
                    clientStream.Dispose();
                serverStream.Dispose();
            }

            public void Connect()
            {
                Task clientConnect = clientStream.ConnectAsync();
                serverStream.WaitForConnection();
                clientConnect.Wait();
            }
        }

        protected virtual NamedPipePair CreateNamedPipePair()
        {
            return CreateNamedPipePair(PipeOptions.Asynchronous, PipeOptions.Asynchronous);
        }

        protected virtual NamedPipePair CreateNamedPipePair(PipeOptions serverOptions, PipeOptions clientOptions)
        {
            return null;
        }

        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }

        protected static byte[] ReadBytes(PipeStream pipeStream, int length)
        {
            Assert.True(pipeStream.IsConnected);

            byte[] buffer = new byte[length];
            Assert.True(length > 0);

            buffer[0] = (byte)pipeStream.ReadByte();
            if (length > 1)
            {
                int len = pipeStream.Read(buffer, 1, length - 1);
                Assert.Equal(length - 1, len);
            }

            return buffer;
        }

        protected static void WriteBytes(PipeStream pipeStream, byte[] buffer)
        {
            Assert.True(pipeStream.IsConnected);
            Assert.True(buffer.Length > 0);

            pipeStream.WriteByte(buffer[0]);
            if (buffer.Length > 1)
            {
                pipeStream.Write(buffer, 1, buffer.Length - 1);
            }
        }

        protected static async Task<byte[]> ReadBytesAsync(PipeStream pipeStream, int length)
        {
            Assert.True(pipeStream.IsConnected);

            byte[] buffer = new byte[length];
            Assert.True(length > 0);

            int readSoFar = 0;

            while (readSoFar < length)
            {
                int len = await pipeStream.ReadAsync(buffer, readSoFar, length - readSoFar);
                if (len == 0) break;
                readSoFar += len;
            }

            return buffer;
        }

        protected static Task WriteBytesAsync(PipeStream pipeStream, byte[] buffer)
        {
            Assert.True(pipeStream.IsConnected);
            Assert.True(buffer.Length > 0);
            return pipeStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
