// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;

public class AnonymousPipesSimpleTest
{
    [Fact]
    public static void ServerSendsByteClientReceives()
    {
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            Assert.True(server.IsConnected);
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                Assert.True(server.IsConnected);
                Assert.True(client.IsConnected);

                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };
                server.Write(sent, 0, 1);
                int bytesReceived = client.Read(received, 0, 1);
                Assert.Equal(1, bytesReceived);
                Assert.Equal(sent[0], received[0]);
            }
        }
    }

    [Fact]
    public static void ClientSendsByteServerReceives()
    {
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
        {
            Assert.True(server.IsConnected);
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                Assert.True(server.IsConnected);
                Assert.True(client.IsConnected);

                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };
                client.Write(sent, 0, 1);
                int bytesReceived = server.Read(received, 0, 1);
                Assert.Equal(1, bytesReceived);
                Assert.Equal(sent[0], received[0]);
            }
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

    public static void StartClient(PipeDirection direction, SafePipeHandle clientPipeHandle)
    {
        using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(direction, clientPipeHandle))
        {
            DoStreamOperations(client);
        }
        // If you don't see this message that means that this task crashed.
        Console.WriteLine("*** Client operations suceedeed. ***");
    }

    public static Task StartClientAsync(PipeDirection direction, SafePipeHandle clientPipeHandle)
    {
        return Task.Run(() => StartClient(direction, clientPipeHandle));
    }

    public static void DoServerOperations(AnonymousPipeServerStream server)
    {
        DoStreamOperations(server);
    }

    public static Task DoServerOperationsAsync(AnonymousPipeServerStream server)
    {
        return Task.Run(() => DoServerOperations(server));
    }

    [Fact]
    public static void ServerPInvokeChecks()
    {
        // calling every API related to server and client to detect any bad PInvokes
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            Task clientTask = StartClientAsync(PipeDirection.In, server.ClientSafePipeHandle);

            Console.WriteLine("server.CanRead = {0}", server.CanRead);
            Console.WriteLine("server.CanSeek = {0}", server.CanSeek);
            Console.WriteLine("server.CanTimeout = {0}", server.CanTimeout);
            Console.WriteLine("server.CanWrite = {0}", server.CanWrite);
            Console.WriteLine("server.GetClientHandleAsString() = {0}", server.GetClientHandleAsString());
            Console.WriteLine("server.IsAsync = {0}", server.IsAsync);
            Console.WriteLine("server.IsConnected = {0}", server.IsConnected);
            Console.WriteLine("server.OutBufferSize = {0}", server.OutBufferSize);
            Console.WriteLine("server.ReadMode = {0}", server.ReadMode);
            Console.WriteLine("server.SafePipeHandle = {0}", server.SafePipeHandle);
            Console.WriteLine("server.TransmissionMode = {0}", server.TransmissionMode);
            server.Write(new byte[] { 123 }, 0, 1);
            server.WriteAsync(new byte[] { 124 }, 0, 1).Wait();
            server.Flush();
            Console.WriteLine("Waiting for Pipe Drain.");
            server.WaitForPipeDrain();
            clientTask.Wait();
            server.DisposeLocalCopyOfClientHandle();
        }
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
        {
            Task clientTask = StartClientAsync(PipeDirection.Out, server.ClientSafePipeHandle);

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
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
        {
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                Task serverTask = DoServerOperationsAsync(server);
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

        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                Task serverTask = DoServerOperationsAsync(server);

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
