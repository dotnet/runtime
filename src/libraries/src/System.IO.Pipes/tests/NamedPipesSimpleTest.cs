// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

public class NamedPipesSimpleTest
{
    [Fact]
    public static void ServerSendsByteClientReceives()
    {
        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.Out))
        {
            byte[] sent = new byte[] { 123 };
            byte[] received = new byte[] { 0 };
            Task t = Task.Run(() =>
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.In))
                {
                    client.Connect();
                    Assert.True(client.IsConnected);

                    Assert.Equal(1, client.Read(received, 0, 1));
                }
            });
            server.WaitForConnection();
            Assert.True(server.IsConnected);

            server.Write(sent, 0, 1);

            t.Wait();
            Assert.Equal(sent[0], received[0]);
        }
    }

    [Fact]
    public static void ClientSendsByteServerReceives()
    {
        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.In))
        {
            byte[] sent = new byte[] { 123 };
            byte[] received = new byte[] { 0 };
            Task clientTask = Task.Run(() => {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.Out))
                {
                    client.Connect();
                    Assert.True(client.IsConnected);

                    client.Write(sent, 0, 1);
                }
            });
            server.WaitForConnection();
            Assert.True(server.IsConnected);

            Assert.Equal(1, server.Read(received, 0, 1));
            Assert.Equal(sent[0], received[0]);

            clientTask.Wait();
        }
    }

    public static void DoStreamOperations(PipeStream stream)
    {
        if (stream.CanWrite)
        {
            stream.Write(new byte[] { 123, 124 }, 0, 2);
        }
        if (stream.CanRead)
        {
            Assert.Equal(123, stream.ReadByte());
            Assert.Equal(124, stream.ReadByte());
        }
    }

    public static void StartClient(PipeDirection direction)
    {
        using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", direction))
        {
            client.Connect();
            DoStreamOperations(client);
        }
    }

    public static Task DoServerOperationsAsync(NamedPipeServerStream server)
    {
        return Task.Run(() =>
        {
            server.WaitForConnection();
            DoStreamOperations(server);
        });
    }

    [Fact]
    public static void ServerPInvokeChecks()
    {
        // calling every API related to server and client to detect any bad PInvokes
        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.Out))
        {
            Task clientTask = Task.Run(() => StartClient(PipeDirection.In));
            server.WaitForConnection();

            Assert.False(server.CanRead);
            Assert.False(server.CanSeek);
            Assert.False(server.CanTimeout);
            Assert.True(server.CanWrite);
            Assert.False(server.IsAsync);
            Assert.True(server.IsConnected);
            Assert.Equal(0, server.OutBufferSize);
            Assert.Equal(PipeTransmissionMode.Byte, server.ReadMode);
            Assert.NotNull(server.SafePipeHandle);
            Assert.Equal(PipeTransmissionMode.Byte, server.TransmissionMode);

            server.Write(new byte[] { 123 }, 0, 1);
            server.WriteAsync(new byte[] { 124 }, 0, 1).Wait();
            server.Flush();
            server.WaitForPipeDrain();

            clientTask.Wait();
        }

        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.In))
        {
            Task clientTask = Task.Run(() => StartClient(PipeDirection.Out));
            server.WaitForConnection();

            Assert.Equal(0, server.InBufferSize);
            byte[] readData = new byte[] { 0, 1 };
            Assert.Equal(1, server.Read(readData, 0, 1));
            Assert.Equal(1, server.ReadAsync(readData, 1, 1).Result);
            Assert.Equal(123, readData[0]);
            Assert.Equal(124, readData[1]);
        }
    }

    [Fact]
    public static void ClientPInvokeChecks()
    {
        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.In))
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.Out))
            {
                Task serverTask = DoServerOperationsAsync(server);
                client.Connect();

                Assert.False(client.CanRead);
                Assert.False(client.CanSeek);
                Assert.False(client.CanTimeout);
                Assert.True(client.CanWrite);
                Assert.False(client.IsAsync);
                Assert.True(client.IsConnected);
                Assert.Equal(0, client.OutBufferSize);
                Assert.Equal(PipeTransmissionMode.Byte, client.ReadMode);
                Assert.NotNull(client.SafePipeHandle);
                Assert.Equal(PipeTransmissionMode.Byte, client.TransmissionMode);

                client.Write(new byte[] { 123 }, 0, 1);
                client.WriteAsync(new byte[] { 124 }, 0, 1).Wait();
                client.WaitForPipeDrain();
                client.Flush();

                serverTask.Wait();
            }
        }

        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.Out))
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.In))
            {
                Task serverTask = DoServerOperationsAsync(server);
                client.Connect();

                Assert.Equal(0, client.InBufferSize);
                byte[] readData = new byte[] { 0, 1 };
                Assert.Equal(1, client.Read(readData, 0, 1));
                Assert.Equal(1, client.ReadAsync(readData, 1, 1).Result);
                Assert.Equal(123, readData[0]);
                Assert.Equal(124, readData[1]);

                serverTask.Wait();
            }
        }
    }
}
