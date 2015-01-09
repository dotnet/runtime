// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

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

                        int bytesReceived = client.Read(received, 0, 1);
                        Assert.Equal(1, bytesReceived);
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
            Task t = Task.Run(() =>
                {
                    using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.Out))
                    {
                        client.Connect();
                        Assert.True(client.IsConnected);

                        client.Write(sent, 0, 1);
                    }
                });
            server.WaitForConnection();
            Assert.True(server.IsConnected);

            int bytesReceived = server.Read(received, 0, 1);
            Assert.Equal(1, bytesReceived);

            t.Wait();
            Assert.Equal(sent[0], received[0]);
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
            if (stream.ReadByte() != 123)
            {
                Console.WriteLine("First byte read != 123");
            }
            if (stream.ReadByte() != 124)
            {
                Console.WriteLine("Second byte read != 124");
            }
        }
        Console.WriteLine("*** Operations finished. ***");
    }

    public static void StartClient(PipeDirection direction)
    {
        using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", direction))
        {
            client.Connect();
            DoStreamOperations(client);
        }
    }

    public static Task StartClientAsync(PipeDirection direction)
    {
        return Task.Run(() => StartClient(direction));
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
            Task clientTask = StartClientAsync(PipeDirection.In);

            server.WaitForConnection();
            Console.WriteLine("server.CanRead = {0}", server.CanRead);
            Console.WriteLine("server.CanSeek = {0}", server.CanSeek);
            Console.WriteLine("server.CanTimeout = {0}", server.CanTimeout);
            Console.WriteLine("server.CanWrite = {0}", server.CanWrite);
            Console.WriteLine("server.IsAsync = {0}", server.IsAsync);
            Console.WriteLine("server.IsConnected = {0}", server.IsConnected);
            Console.WriteLine("server.OutBufferSize = {0}", server.OutBufferSize);
            Console.WriteLine("server.ReadMode = {0}", server.ReadMode);
            Console.WriteLine("server.SafePipeHandle = {0}", server.SafePipeHandle);
            Console.WriteLine("server.TransmissionMode = {0}", server.TransmissionMode);
            server.Write(new byte[] { 123 }, 0, 1);
            server.WriteAsync(new byte[] { 124 }, 0, 1).Wait();
            server.Flush();
            server.WaitForPipeDrain();
            clientTask.Wait();
        }
        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.In))
        {
            Task clientTask = StartClientAsync(PipeDirection.Out);

            server.WaitForConnection();
            Console.WriteLine("server.InBufferSize = {0}", server.InBufferSize);
            byte[] readData = new byte[] { 0, 1 };
            server.Read(readData, 0, 1);
            server.ReadAsync(readData, 1, 1).Wait();
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
                Console.WriteLine("client.CanRead = {0}", client.CanRead);
                Console.WriteLine("client.CanSeek = {0}", client.CanSeek);
                Console.WriteLine("client.CanTimeout = {0}", client.CanTimeout);
                Console.WriteLine("client.CanWrite = {0}", client.CanWrite);
                Console.WriteLine("client.IsAsync = {0}", client.IsAsync);
                Console.WriteLine("client.IsConnected = {0}", client.IsConnected);
                Console.WriteLine("client.OutBufferSize = {0}", client.OutBufferSize);
                Console.WriteLine("client.ReadMode = {0}", client.ReadMode);
                Console.WriteLine("client.SafePipeHandle = {0}", client.SafePipeHandle);
                Console.WriteLine("client.TransmissionMode = {0}", client.TransmissionMode);

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

                Console.WriteLine("client.InBufferSize = {0}", client.InBufferSize);
                byte[] readData = new byte[] { 0, 1 };
                client.Read(readData, 0, 1);
                client.ReadAsync(readData, 1, 1).Wait();
                Assert.Equal(123, readData[0]);
                Assert.Equal(124, readData[1]);

                serverTask.Wait();
            }
        }
    }
}
