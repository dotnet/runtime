// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The Specific NamedPipe tests cover edge cases or otherwise narrow cases that
    /// show up within particular server/client directional combinations.
    /// </summary>
    public class NamedPipeTest_Specific : NamedPipeTestBase
    {
        [Fact]
        public void InvalidConnectTimeout_Throws_ArgumentOutOfRangeException()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                Assert.Throws<ArgumentOutOfRangeException>("timeout", () => client.Connect(-111));
                Assert.Throws<ArgumentOutOfRangeException>("timeout", () => { client.ConnectAsync(-111); });
            }
        }

        [Fact]
        [ActiveIssue(812, PlatformID.AnyUnix)] // Unix implementation currently ignores timeout and cancellation token once the operation has been initiated]
        public async Task ConnectToNonExistentServer_Throws_TimeoutException()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere"))
            {
                var ctx = new CancellationTokenSource();
                Assert.Throws<TimeoutException>(() => client.Connect(60));  // 60 to be over internal 50 interval
                await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(50));
                await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(60, ctx.Token)); // testing Token overload; ctx is not cancelled in this test
            }
        }

        [Fact]
        public async Task CancelConnectToNonExistentServer_Throws_OperationCanceledException()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere"))
            {
                var ctx = new CancellationTokenSource();

                Task clientConnectToken = client.ConnectAsync(ctx.Token);
                ctx.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientConnectToken);

                ctx.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ConnectAsync(ctx.Token));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // Unix implementation uses bidirectional sockets
        public void ConnectWithConflictingDirections_Throws_UnauthorizedAccessException()
        {
            string serverName1 = GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName1, PipeDirection.Out))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName1, PipeDirection.Out))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                Assert.False(client.IsConnected);
            }

            string serverName2 = GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName2, PipeDirection.In))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName2, PipeDirection.In))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                Assert.False(client.IsConnected);
            }
        }

        [Theory]
        [InlineData(PipeOptions.None)]
        [InlineData(PipeOptions.Asynchronous)]
        [PlatformSpecific(PlatformID.Windows)] // Unix currently doesn't support message mode
        public async Task Windows_MessagePipeTransissionMode(PipeOptions serverOptions)
        {
            byte[] msg1 = new byte[] { 5, 7, 9, 10 };
            byte[] msg2 = new byte[] { 2, 4 };
            byte[] received1 = new byte[] { 0, 0, 0, 0 };
            byte[] received2 = new byte[] { 0, 0 };
            byte[] received3 = new byte[] { 0, 0, 0, 0 };
            byte[] received4 = new byte[] { 0, 0, 0, 0 };
            byte[] received5 = new byte[] { 0, 0 };
            byte[] received6 = new byte[] { 0, 0, 0, 0 };
            string pipeName = GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, serverOptions))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                {
                    server.ReadMode = PipeTransmissionMode.Message;
                    Assert.Equal(PipeTransmissionMode.Message, server.ReadMode);

                    Task clientTask = Task.Run(() =>
                    {
                        client.Connect();

                        client.Write(msg1, 0, msg1.Length);
                        client.Write(msg2, 0, msg2.Length);
                        client.Write(msg1, 0, msg1.Length);

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

                    int expectedRead = msg1.Length - 1;
                    int len3 = server.Read(received3, 0, expectedRead);  // read one less than message
                    Assert.False(server.IsMessageComplete);
                    Assert.Equal(expectedRead, len3);
                    for (int i = 0; i < expectedRead; ++i)
                    {
                        Assert.Equal(msg1[i], received3[i]);
                    }

                    expectedRead = msg1.Length - expectedRead;
                    Assert.Equal(expectedRead, server.Read(received3, len3, expectedRead));
                    Assert.True(server.IsMessageComplete);
                    Assert.Equal(msg1, received3);

                    Assert.Equal(msg1.Length, await server.ReadAsync(received4, 0, msg1.Length));
                    Assert.True(server.IsMessageComplete);
                    Assert.Equal(msg1, received4);

                    Assert.Equal(msg2.Length, await server.ReadAsync(received5, 0, msg2.Length));
                    Assert.True(server.IsMessageComplete);
                    Assert.Equal(msg2, received5);

                    expectedRead = msg1.Length - 1;
                    Assert.Equal(expectedRead, await server.ReadAsync(received6, 0, expectedRead));  // read one less than message
                    Assert.False(server.IsMessageComplete);
                    for (int i = 0; i < expectedRead; ++i)
                    {
                        Assert.Equal(msg1[i], received6[i]);
                    }

                    expectedRead = msg1.Length - expectedRead;
                    Assert.Equal(expectedRead, await server.ReadAsync(received6, msg1.Length - expectedRead, expectedRead));
                    Assert.True(server.IsMessageComplete);
                    Assert.Equal(msg1, received6);

                    await clientTask;
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // Unix doesn't support MaxNumberOfServerInstances
        public async Task Windows_Get_NumberOfServerInstances_Succeed()
        {
            string pipeName = GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 3))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                {
                    int expectedNumberOfServerInstances;
                    Task serverTask = server.WaitForConnectionAsync();

                    client.Connect();
                    await serverTask;

                    Assert.True(Interop.TryGetNumberOfServerInstances(client.SafePipeHandle, out expectedNumberOfServerInstances), "GetNamedPipeHandleState failed");
                    Assert.Equal(expectedNumberOfServerInstances, client.NumberOfServerInstances);
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // Win32 P/Invokes to verify the user name
        public async Task Windows_GetImpersonationUserName_Succeed()
        {
            string pipeName = GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                {
                    string expectedUserName;
                    Task serverTask = server.WaitForConnectionAsync();

                    client.Connect();
                    await serverTask;

                    Assert.True(Interop.TryGetImpersonationUserName(server.SafePipeHandle, out expectedUserName), "GetNamedPipeHandleState failed");
                    Assert.Equal(expectedUserName, server.GetImpersonationUserName());
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public async Task Unix_GetImpersonationUserName_Succeed()
        {
            string pipeName = GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
            {
                Task serverTask = server.WaitForConnectionAsync();

                client.Connect();
                await serverTask;

                string name = server.GetImpersonationUserName();
                Assert.NotNull(name);
                Assert.False(string.IsNullOrWhiteSpace(name));
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_MessagePipeTransissionMode()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeServerStream(GetUniquePipeName(), PipeDirection.InOut, 1, PipeTransmissionMode.Message));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.Out)]
        [InlineData(PipeDirection.InOut)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public static void Unix_BufferSizeRoundtripping(PipeDirection direction)
        {
            int desiredBufferSize = 0;
            string pipeName = GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, desiredBufferSize, desiredBufferSize))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                desiredBufferSize = server.OutBufferSize * 2;
            }

            using (var server = new NamedPipeServerStream(pipeName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, desiredBufferSize, desiredBufferSize))
            using (var client = new NamedPipeClientStream(".", pipeName, direction == PipeDirection.In ? PipeDirection.Out : PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                if ((direction & PipeDirection.Out) != 0)
                {
                    Assert.InRange(server.OutBufferSize, desiredBufferSize, int.MaxValue);
                }

                if ((direction & PipeDirection.In) != 0)
                {
                    Assert.InRange(server.InBufferSize, desiredBufferSize, int.MaxValue);
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void Windows_BufferSizeRoundtripping()
        {
            int desiredBufferSize = 10;
            string pipeName = GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, desiredBufferSize, desiredBufferSize))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                Assert.Equal(desiredBufferSize, server.OutBufferSize);
                Assert.Equal(desiredBufferSize, client.InBufferSize);
            }

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, desiredBufferSize, desiredBufferSize))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                Assert.Equal(desiredBufferSize, server.InBufferSize);
                Assert.Equal(0, client.OutBufferSize);
            }
        }

        [Fact]
        public void PipeTransmissionMode_Returns_Byte()
        {
            using (ServerClientPair pair = CreateServerClientPair())
            {
                Assert.Equal(PipeTransmissionMode.Byte, pair.writeablePipe.TransmissionMode);
                Assert.Equal(PipeTransmissionMode.Byte, pair.readablePipe.TransmissionMode);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)] // Unix doesn't currently support message mode
        public void Windows_SetReadModeTo__PipeTransmissionModeByte()
        {
            string pipeName = GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                // Throws regardless of connection status for the pipe that is set to PipeDirection.In
                Assert.Throws<UnauthorizedAccessException>(() => server.ReadMode = PipeTransmissionMode.Byte);
                client.ReadMode = PipeTransmissionMode.Byte;
            }

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                // Throws regardless of connection status for the pipe that is set to PipeDirection.In
                Assert.Throws<UnauthorizedAccessException>(() => client.ReadMode = PipeTransmissionMode.Byte);
                server.ReadMode = PipeTransmissionMode.Byte;
            }

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                server.ReadMode = PipeTransmissionMode.Byte;
                client.ReadMode = PipeTransmissionMode.Byte;
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_SetReadModeTo__PipeTransmissionModeByte()
        {
            string pipeName = GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                server.ReadMode = PipeTransmissionMode.Byte;
                client.ReadMode = PipeTransmissionMode.Byte;
            }

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                client.ReadMode = PipeTransmissionMode.Byte;
                server.ReadMode = PipeTransmissionMode.Byte;
            }

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                server.ReadMode = PipeTransmissionMode.Byte;
                client.ReadMode = PipeTransmissionMode.Byte;
            }
        }

        [Theory]
        [InlineData(PipeDirection.Out, PipeDirection.In)]
        [InlineData(PipeDirection.In, PipeDirection.Out)]
        public void InvalidReadMode_Throws_ArgumentOutOfRangeException(PipeDirection serverDirection, PipeDirection clientDirection)
        {
            string pipeName = GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, serverDirection, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, clientDirection))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                Assert.Throws<ArgumentOutOfRangeException>(() => server.ReadMode = (PipeTransmissionMode)999);
                Assert.Throws<ArgumentOutOfRangeException>(() => client.ReadMode = (PipeTransmissionMode)999);
            }
        }

    }
}
