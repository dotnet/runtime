// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_AclExtensions : PipeTest_AclExtensions
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var writeablePipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var readablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            Task clientConnect = readablePipe.ConnectAsync();
            writeablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }

        [Fact]
        public void GetAccessControl_NamedPipe_BeforeWaitingToConnect()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.Throws<InvalidOperationException>(() => client.GetAccessControl());
            Assert.Throws<InvalidOperationException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        /// <summary>
        /// Tests that SetAccessControl on a Broken NamedPipeClientStream throws an Exception.
        /// </summary>
        [Fact]
        public void GetAccessControl_NamedPipeClientStream_Broken()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.NotNull(client.GetAccessControl());
            client.SetAccessControl(new PipeSecurity());

            server.Dispose();
            Assert.Throws<IOException>(() => client.Write(new byte[] { 0 }, 0, 1)); // Sets the clients PipeState to Broken
            Assert.Throws<InvalidOperationException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        /// <summary>
        /// Tests that SetAccessControl on a Broken NamedPipeServerStream succeeds
        /// </summary>
        [Fact]
        public void GetAccessControl_NamedPipeServerStream_Broken()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.NotNull(client.GetAccessControl());
            client.SetAccessControl(new PipeSecurity());

            client.Dispose();
            Assert.Throws<IOException>(() => server.Write(new byte[] { 0 }, 0, 1)); // Sets the servers PipeState to Broken
            server.SetAccessControl(new PipeSecurity());
        }
    }
}
