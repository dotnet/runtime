// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

public class AnonymousPipesSimpleTest
{
    [Fact]
    public static void ServerSendsByteClientReceives()
    {
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            Assert.True(server.IsConnected);
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.GetClientHandleAsString()))
            {
                Assert.True(server.IsConnected);
                Assert.True(client.IsConnected);

                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };
                server.Write(sent, 0, 1);

                Assert.Equal(1, client.Read(received, 0, 1));
                Assert.Equal(sent[0], received[0]);
            }
            Assert.Throws<System.IO.IOException>(() => server.WriteByte(5));
        }
    }

    [Fact]
    public static void ServerSendsByteClientReceivesServerClone()
    {
        using (AnonymousPipeServerStream serverBase = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, serverBase.SafePipeHandle, serverBase.ClientSafePipeHandle))
            {
                Assert.True(server.IsConnected);
                using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.GetClientHandleAsString()))
                {
                    Assert.True(server.IsConnected);
                    Assert.True(client.IsConnected);

                    byte[] sent = new byte[] { 123 };
                    byte[] received = new byte[] { 0 };
                    server.Write(sent, 0, 1);

                    Assert.Equal(1, client.Read(received, 0, 1));
                    Assert.Equal(sent[0], received[0]);
                }
                Assert.Throws<System.IO.IOException>(() => server.WriteByte(5));
            }
        }
    }

    [Fact]
    public static async Task ServerSendsByteClientReceivesAsync()
    {
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            Assert.True(server.IsConnected);
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(server.GetClientHandleAsString()))
            {
                Assert.True(server.IsConnected);
                Assert.True(client.IsConnected);

                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };
                await server.WriteAsync(sent, 0, 1);

                Assert.Equal(1, await client.ReadAsync(received, 0, 1));
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
            server.ReadMode = PipeTransmissionMode.Byte;

            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                Assert.True(server.IsConnected);
                Assert.True(client.IsConnected);
                client.ReadMode = PipeTransmissionMode.Byte;

                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };
                client.Write(sent, 0, 1);

                server.DisposeLocalCopyOfClientHandle();

                Assert.Equal(1, server.Read(received, 0, 1));
                Assert.Equal(sent[0], received[0]);
            }
            // not sure why the following isn't thrown because pipe is broken
            //Assert.Throws<System.IO.IOException>(() => server.ReadByte());
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

    public static void StartClient(PipeDirection direction, SafePipeHandle clientPipeHandle)
    {
        using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(direction, clientPipeHandle))
        {
            DoStreamOperations(client);
        }
    }

    [Fact]
    public static async Task ServerPInvokeChecks()
    {
        // calling every API related to server and client to detect any bad PInvokes
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, 4096))
        {
            Task clientTask = Task.Run(() => StartClient(PipeDirection.In, server.ClientSafePipeHandle));

            Assert.False(server.CanRead);
            Assert.False(server.CanSeek);
            Assert.False(server.CanTimeout);
            Assert.True(server.CanWrite);
            Assert.False(string.IsNullOrWhiteSpace(server.GetClientHandleAsString()));
            Assert.False(server.IsAsync);
            Assert.True(server.IsConnected);
            if (Interop.IsWindows || Interop.IsLinux)
            {
                Assert.True(server.OutBufferSize > 0);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => server.OutBufferSize);
            }
            Assert.Equal(PipeTransmissionMode.Byte, server.ReadMode);
            Assert.NotNull(server.SafePipeHandle);
            Assert.Equal(PipeTransmissionMode.Byte, server.TransmissionMode);

            server.Write(new byte[] { 123 }, 0, 1);
            await server.WriteAsync(new byte[] { 124 }, 0, 1);
            server.Flush();
            if (Interop.IsWindows)
            {
                server.WaitForPipeDrain();
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => server.WaitForPipeDrain());
            }

            await clientTask;
            server.DisposeLocalCopyOfClientHandle();
        }

        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In))
        {
            Task clientTask = Task.Run(() => StartClient(PipeDirection.Out, server.ClientSafePipeHandle));

            if (Interop.IsWindows)
            {
                Assert.Equal(4096, server.InBufferSize);
            }
            else if (Interop.IsLinux)
            {
                Assert.True(server.InBufferSize > 0);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => server.InBufferSize);
            }
            byte[] readData = new byte[] { 0, 1 };
            Assert.Equal(1, server.Read(readData, 0, 1));
            Assert.Equal(1, server.ReadAsync(readData, 1, 1).Result);
            Assert.Equal(123, readData[0]);
            Assert.Equal(124, readData[1]);

            await clientTask;
        }
    }

    [Fact]
    public static async Task ClientPInvokeChecks()
    {
        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.In, System.IO.HandleInheritability.None, 4096))
        {
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
            {
                Task serverTask = Task.Run(() => DoStreamOperations(server));

                Assert.False(client.CanRead);
                Assert.False(client.CanSeek);
                Assert.False(client.CanTimeout);
                Assert.True(client.CanWrite);
                Assert.False(client.IsAsync);
                Assert.True(client.IsConnected);
                if (Interop.IsWindows)
                {
                    Assert.Equal(0, client.OutBufferSize);
                }
                else if (Interop.IsLinux)
                {
                    Assert.True(client.OutBufferSize > 0);
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => client.OutBufferSize);
                }
                Assert.Equal(PipeTransmissionMode.Byte, client.ReadMode);
                Assert.NotNull(client.SafePipeHandle);
                Assert.Equal(PipeTransmissionMode.Byte, client.TransmissionMode);

                client.Write(new byte[] { 123 }, 0, 1);
                await client.WriteAsync(new byte[] { 124 }, 0, 1);
                if (Interop.IsWindows)
                {
                    client.WaitForPipeDrain();
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => client.WaitForPipeDrain());
                }
                client.Flush();

                await serverTask;
            }
        }

        using (AnonymousPipeServerStream server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            using (AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
            {
                Task serverTask = Task.Run(() => DoStreamOperations(server));

                if (Interop.IsWindows)
                {
                    Assert.Equal(4096, client.InBufferSize);
                }
                else if (Interop.IsLinux)
                {
                    Assert.True(client.InBufferSize > 0);
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => client.InBufferSize);
                }
                byte[] readData = new byte[] { 0, 1 };
                Assert.Equal(1, client.Read(readData, 0, 1));
                Assert.Equal(1, client.ReadAsync(readData, 1, 1).Result);
                Assert.Equal(123, readData[0]);
                Assert.Equal(124, readData[1]);

                await serverTask;
            }
        }
    }

    [Fact]
    [PlatformSpecific(PlatformID.Linux)]
    public static void BufferSizeRoundtrips()
    {
        int desiredBufferSize;
        using (var server = new AnonymousPipeServerStream(PipeDirection.Out))
        {
            desiredBufferSize = server.OutBufferSize * 2;
            Assert.True(desiredBufferSize > 0);
        }

        using (var server = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.None, desiredBufferSize))
        using (var client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle))
        {
            Assert.Equal(desiredBufferSize, server.OutBufferSize);
            Assert.Equal(desiredBufferSize, client.InBufferSize);
        }

        using (var server = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None, desiredBufferSize))
        using (var client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle))
        {
            Assert.Equal(desiredBufferSize, server.InBufferSize);
            Assert.Equal(desiredBufferSize, client.OutBufferSize);
        }
    }

}
