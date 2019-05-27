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
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream()
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
        }

        [Fact]
        public void SetAccessControl_NamedPipeStream_BeforeWaitingToConnect()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            // ServerStream.State = WaitingToConnect, ClientStream.State = WaitingToConnect
            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.Throws<InvalidOperationException>(() => client.GetAccessControl());
            Assert.Throws<InvalidOperationException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream_ClientDisposed()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            // ServerStream.State = Broken, ClientStream.State = Closed
            client.Dispose();
            Assert.Throws<IOException>(() => server.Write(new byte[] { 0 }, 0, 1)); // Sets the servers PipeState to Broken
            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.Throws<ObjectDisposedException>(() => client.GetAccessControl());
            Assert.Throws<ObjectDisposedException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream_ClientHandleClosed()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            // ServerStream.State = Broken, ClientStream.State = Closed
            client.SafePipeHandle.Close();
            Assert.Throws<IOException>(() => server.Write(new byte[] { 0 }, 0, 1));
            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.Throws<ObjectDisposedException>(() => client.GetAccessControl());
            Assert.Throws<ObjectDisposedException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream_ServerDisconnected()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();
            
            // ServerStream.State = Disconnected, ClientStream.State = Broken
            server.Disconnect();
            Assert.Throws<IOException>(() => client.Write(new byte[] { 0 }, 0, 1));
            Assert.NotNull(server.GetAccessControl());
            server.SetAccessControl(new PipeSecurity());
            Assert.Throws<InvalidOperationException>(() => client.GetAccessControl());
            Assert.Throws<IOException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream_ServerDisposed()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            // ServerStream.State = Closed, ClientStream.State = Broken
            server.Dispose();
            Assert.Throws<IOException>(() => client.Write(new byte[] { 0 }, 0, 1));
            Assert.Throws<ObjectDisposedException>(() => server.GetAccessControl());
            Assert.Throws<ObjectDisposedException>(() => server.SetAccessControl(new PipeSecurity()));
            Assert.NotNull(client.GetAccessControl());
            Assert.Throws<IOException>(() => client.SetAccessControl(new PipeSecurity()));
        }

        [Fact]
        [ActiveIssue(22271, TargetFrameworkMonikers.Uap)]
        public void SetAccessControl_NamedPipeStream_ServerHandleClosed()
        {
            string pipeName = GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            Task clientConnect = client.ConnectAsync();
            server.WaitForConnection();
            clientConnect.Wait();

            // ServerStream.State = Closed, ClientStream.State = Broken
            server.SafePipeHandle.Close();
            Assert.Throws<IOException>(() => client.Write(new byte[] { 0 }, 0, 1));
            Assert.Throws<ObjectDisposedException>(() => server.GetAccessControl());
            Assert.Throws<ObjectDisposedException>(() => server.SetAccessControl(new PipeSecurity()));
            Assert.NotNull(client.GetAccessControl());
            Assert.Throws<IOException>(() => client.SetAccessControl(new PipeSecurity()));
        }
    }
}
