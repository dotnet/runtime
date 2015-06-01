// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

public class NamedPipesSimpleTest
{
    static byte[] ReadBytes(PipeStream pipeStream, int length)
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

    static void WriteBytes(PipeStream pipeStream, byte[] buffer)
    {
        Assert.True(pipeStream.IsConnected);
        Assert.True(buffer.Length > 0);

        pipeStream.WriteByte(buffer[0]);
        if (buffer.Length > 1)
        {
            pipeStream.Write(buffer, 1, buffer.Length - 1);
        }
    }

    static async Task<byte[]> ReadBytesAsync(PipeStream pipeStream, int length)
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

    static Task WriteBytesAsync(PipeStream pipeStream, byte[] buffer)
    {
        Assert.True(pipeStream.IsConnected);
        Assert.True(buffer.Length > 0);
        return pipeStream.WriteAsync(buffer, 0, buffer.Length);
    }

    static byte[] sendBytes = new byte[] { 123, 234 };

    [Theory]
    [InlineData("TestOut0", PipeDirection.Out, false, false, false, false)]
    [InlineData("TestOut1", PipeDirection.Out, false, false, true, false)]
    [InlineData("TestOut2", PipeDirection.Out, false, false, false, true)]
    [InlineData("TestOut3", PipeDirection.Out, false, false, true, true)]
    [InlineData("TestOut4", PipeDirection.Out, false, true, false, false)]
    [InlineData("TestOut5", PipeDirection.Out, false, true, true, false)]
    [InlineData("TestOut6", PipeDirection.Out, false, true, false, true)]
    [InlineData("TestOut7", PipeDirection.Out, false, true, true, true)]
    [InlineData("TestOut8", PipeDirection.Out, true, false, false, false)]
    [InlineData("TestOut9", PipeDirection.Out, true, false, true, false)]
    [InlineData("TestOutA", PipeDirection.Out, true, false, false, true)]
    [InlineData("TestOutB", PipeDirection.Out, true, false, true, true)]
    [InlineData("TestOutC", PipeDirection.Out, true, true, false, false)]
    [InlineData("TestOutD", PipeDirection.Out, true, true, true, false)]
    [InlineData("TestOutE", PipeDirection.Out, true, true, false, true)]
    [InlineData("TestOutF", PipeDirection.Out, true, true, true, true)]
    [InlineData("TestIn0", PipeDirection.In, false, false, false, false)]
    [InlineData("TestIn1", PipeDirection.In, false, false, true, false)]
    [InlineData("TestIn2", PipeDirection.In, false, false, false, true)]
    [InlineData("TestIn3", PipeDirection.In, false, false, true, true)]
    [InlineData("TestIn4", PipeDirection.In, false, true, false, false)]
    [InlineData("TestIn5", PipeDirection.In, false, true, true, false)]
    [InlineData("TestIn6", PipeDirection.In, false, true, false, true)]
    [InlineData("TestIn7", PipeDirection.In, false, true, true, true)]
    [InlineData("TestIn8", PipeDirection.In, true, false, false, false)]
    [InlineData("TestIn9", PipeDirection.In, true, false, true, false)]
    [InlineData("TestInA", PipeDirection.In, true, false, false, true)]
    [InlineData("TestInB", PipeDirection.In, true, false, true, true)]
    [InlineData("TestInC", PipeDirection.In, true, true, false, false)]
    [InlineData("TestInD", PipeDirection.In, true, true, true, false)]
    [InlineData("TestInE", PipeDirection.In, true, true, false, true)]
    [InlineData("TestInF", PipeDirection.In, true, true, true, true)]
    public static async Task ClientServerOneWayOperations(
        string pipeName, PipeDirection serverDirection,
        bool asyncServerPipe, bool asyncClientPipe,
        bool asyncServerOps, bool asyncClientOps)
    {
        PipeDirection clientDirection = serverDirection == PipeDirection.Out ? PipeDirection.In : PipeDirection.Out;
        PipeOptions serverOptions = asyncServerPipe ? PipeOptions.Asynchronous : PipeOptions.None;
        PipeOptions clientOptions = asyncClientPipe ? PipeOptions.Asynchronous : PipeOptions.None;

        using (NamedPipeServerStream server = new NamedPipeServerStream(pipeName, serverDirection, 1, PipeTransmissionMode.Byte, serverOptions))
        {
            byte[] received = new byte[] { 0 };
            Task clientTask = Task.Run(async () =>
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, clientDirection, clientOptions))
                {
                    if (asyncClientOps)
                    {
                        await client.ConnectAsync();
                        if (clientDirection == PipeDirection.In)
                        {
                            received = await ReadBytesAsync(client, sendBytes.Length);
                        }
                        else
                        {
                            await WriteBytesAsync(client, sendBytes);
                        }
                    }
                    else
                    {
                        client.Connect();
                        if (clientDirection == PipeDirection.In)
                        {
                            received = ReadBytes(client, sendBytes.Length);
                        }
                        else
                        {
                            WriteBytes(client, sendBytes);
                        }
                    }
                }
            });

            if (asyncServerOps)
            {
                await server.WaitForConnectionAsync();
                if (serverDirection == PipeDirection.Out)
                {
                    await WriteBytesAsync(server, sendBytes);
                }
                else
                {
                    received = await ReadBytesAsync(server, sendBytes.Length);
                }
            }
            else {
                server.WaitForConnection();
                if (serverDirection == PipeDirection.Out)
                {
                    WriteBytes(server, sendBytes);
                }
                else
                {
                    received = ReadBytes(server, sendBytes.Length);
                }
            }

            await clientTask;
            Assert.Equal(sendBytes, received);

            server.Disconnect();
            Assert.False(server.IsConnected);
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
    public static async Task ServerPInvokeChecks()
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
            if (Interop.IsWindows)
            {
                Assert.Equal(0, server.OutBufferSize);
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
        }

        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.In))
        {
            Task clientTask = Task.Run(() => StartClient(PipeDirection.Out));
            server.WaitForConnection();

            if (Interop.IsWindows)
            {
                Assert.Equal(0, server.InBufferSize);
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
        }
    }

    [Fact]
    public static async Task ClientPInvokeChecks()
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
                if (Interop.IsWindows)
                {
                    Assert.Equal(0, client.OutBufferSize);
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

        using (NamedPipeServerStream server = new NamedPipeServerStream("foo", PipeDirection.Out))
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foo", PipeDirection.In))
            {
                Task serverTask = DoServerOperationsAsync(server);
                client.Connect();

                if (Interop.IsWindows)
                {
                    Assert.Equal(0, client.InBufferSize);
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
    [PlatformSpecific(PlatformID.Windows)]
    public static void ClientServerMessages()
    {
        byte[] msg1 = new byte[] { 5, 7, 9, 10 };
        byte[] msg2 = new byte[] { 2, 4 };
        byte[] received1 = new byte[] { 0, 0, 0, 0 };
        byte[] received2 = new byte[] { 0, 0 };
        byte[] received3 = new byte[] { 0, 0, 0, 0}; ;

        using (NamedPipeServerStream server = new NamedPipeServerStream("foomsg", PipeDirection.InOut, 1, PipeTransmissionMode.Message))
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "foomsg", PipeDirection.InOut, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Identification))
            {
                Task clientTask = Task.Run(() =>
                {
                    client.Connect();
                    
                    client.Write(msg1, 0, msg1.Length);
                    client.Write(msg2, 0, msg2.Length);
                    client.Write(msg1, 0, msg1.Length);

                    int serverCount = client.NumberOfServerInstances;
                    Assert.Equal(1, serverCount);
                });

                server.WaitForConnection();
                int len1 = server.Read(received1, 0, msg1.Length);
                Assert.True(server.IsMessageComplete);
                Assert.Equal(msg1.Length, len1);
                Assert.Equal(msg1, received1);

                int len2 = server.Read(received2, 0, msg2.Length);
                Assert.True(server.IsMessageComplete);
                Assert.Equal(msg2.Length, len2);
                Assert.Equal(msg2, received2);

                int len3 = server.Read(received3, 0, msg1.Length - 1);  // read one less than message
                Assert.False(server.IsMessageComplete);
                Assert.Equal(msg1.Length - 1, len3);

                int len4 = server.Read(received3, len3, received3.Length - len3);
                Assert.True(server.IsMessageComplete);
                Assert.Equal(msg1.Length, len3 + len4);
                Assert.Equal(msg1, received3);

                string userName = server.GetImpersonationUserName();    // not sure what to test here that will work in all cases
            }
        }
    }

    [Fact]
    public static async Task ServerCloneTests()
    {
        const string pipeName = "fooclone";
        byte[] msg1 = new byte[] { 5, 7, 9, 10 };
        byte[] received1 = new byte[] { 0, 0, 0, 0 };

        using (NamedPipeServerStream serverBase = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte))
        using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None))
        {
            Task clientTask = Task.Run(() =>
            {
                client.Connect();
                client.Write(msg1, 0, msg1.Length);
                if (Interop.IsWindows)
                {
                    Assert.Equal(1, client.NumberOfServerInstances);
                }
            });

            serverBase.WaitForConnection();
            using (NamedPipeServerStream server = new NamedPipeServerStream(PipeDirection.In, false, true, serverBase.SafePipeHandle))
            {
                int len1 = server.Read(received1, 0, msg1.Length);
                Assert.Equal(msg1.Length, len1);
                Assert.Equal(msg1, received1);
                await clientTask;
            }
        }
    }

    [Fact]
    public static async Task ClientCloneTests()
    {
        const string pipeName = "fooClientclone";

        byte[] msg1 = new byte[] { 5, 7, 9, 10 };
        byte[] received0 = new byte[] { };
        byte[] received1 = new byte[] { 0, 0, 0, 0 };

        using (NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte))
        using (NamedPipeClientStream clientBase = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None))
        {
            await Task.WhenAll(server.WaitForConnectionAsync(), clientBase.ConnectAsync());

            using (NamedPipeClientStream client = new NamedPipeClientStream(PipeDirection.Out, false, true, clientBase.SafePipeHandle))
            {
                if (Interop.IsWindows)
                {
                    Assert.Equal(1, client.NumberOfServerInstances);
                }
                Assert.Equal(PipeTransmissionMode.Byte, client.TransmissionMode);

                Task clientTask = Task.Run(() => client.Write(msg1, 0, msg1.Length));
                int len1 = server.Read(received1, 0, msg1.Length);
                await clientTask;
                Assert.Equal(msg1.Length, len1);
                Assert.Equal(msg1, received1);

                // test special cases of buffer lengths = 0
                int len0 = server.Read(received0, 0, 0);
                Assert.Equal(0, len0);
                Assert.Equal(0, await server.ReadAsync(received0, 0, 0));
            }
        }
    }
}
