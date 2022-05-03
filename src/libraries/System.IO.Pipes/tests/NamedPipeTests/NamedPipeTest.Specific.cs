// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// The Specific NamedPipe tests cover edge cases or otherwise narrow cases that
    /// show up within particular server/client directional combinations.
    /// </summary>
    public class NamedPipeTest_Specific
    {
        [Fact]
        public void InvalidConnectTimeout_Throws_ArgumentOutOfRangeException()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => client.Connect(-111));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { client.ConnectAsync(-111); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => client.Connect(TimeSpan.FromMilliseconds(-2)));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { client.ConnectAsync(TimeSpan.FromMilliseconds(-2), default); });
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => client.Connect(TimeSpan.FromMilliseconds((long)int.MaxValue + 1)));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { client.ConnectAsync(TimeSpan.FromMilliseconds((long)int.MaxValue + 1), default); });
            }
        }

        [Fact]
        public async Task ConnectToNonExistentServer_Throws_TimeoutException()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere"))
            {
                var ctx = new CancellationTokenSource();
                Assert.Throws<TimeoutException>(() =>
                    client.Connect(TimeSpan.FromMilliseconds(60))); // 60 to be over internal 50 interval
                await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(TimeSpan.FromMilliseconds(50), default));
                await Assert.ThrowsAsync<TimeoutException>(() =>
                    client.ConnectAsync(TimeSpan.FromMilliseconds(60),
                        ctx.Token)); // testing Token overload; ctx is not canceled in this test
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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
        [PlatformSpecific(TestPlatforms.Windows)] // Unix implementation uses bidirectional sockets
        public void ConnectWithConflictingDirections_Throws_UnauthorizedAccessException()
        {
            string serverName1 = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName1, PipeDirection.Out))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName1, PipeDirection.Out))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                Assert.False(client.IsConnected);
            }

            string serverName2 = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName2, PipeDirection.In))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName2, PipeDirection.In))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                Assert.False(client.IsConnected);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task MultipleWaitingClients_ServerServesOneAtATime(int numClients)
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeServerStream server = new NamedPipeServerStream(name))
            {
                var clients = new List<Task>(from i in Enumerable.Range(0, numClients) select ConnectClientAndReadAsync());

                while (clients.Count > 0)
                {
                    Task<Task> firstClient = Task.WhenAny(clients);
                    await new Task[] { ServerWaitReadAndWriteAsync(), firstClient }.WhenAllOrAnyFailed();
                    clients.Remove(firstClient.Result);
                }

                async Task ServerWaitReadAndWriteAsync()
                {
                    await server.WaitForConnectionAsync();
                    await server.WriteAsync(new byte[1], 0, 1);
                    Assert.Equal(1, await server.ReadAsync(new byte[1], 0, 1));
                    server.Disconnect();
                }

                async Task ConnectClientAndReadAsync()
                {
                    using (var npcs = new NamedPipeClientStream(name))
                    {
                        await npcs.ConnectAsync();
                        Assert.Equal(1, await npcs.ReadAsync(new byte[1], 0, 1));
                        await npcs.WriteAsync(new byte[1], 0, 1);
                    }
                }
            }
        }

        [Fact]
        public void MaxNumberOfServerInstances_TooManyServers_Throws()
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();

            using (new NamedPipeServerStream(name, PipeDirection.InOut, 1))
            {
                // NPSS was created with max of 1, so creating another fails.
                Assert.Throws<IOException>(() => new NamedPipeServerStream(name, PipeDirection.InOut, 1));
            }

            using (new NamedPipeServerStream(name, PipeDirection.InOut, 3))
            {
                // NPSS was created with max of 3, but NPSS not only validates against the original max but also
                // against the max of the stream being created, so since there's already 1 and this specifies max == 1, it fails.
                Assert.Throws<UnauthorizedAccessException>(() => new NamedPipeServerStream(name, PipeDirection.InOut, 1));

                using (new NamedPipeServerStream(name, PipeDirection.InOut, 2)) // lower max ignored
                using (new NamedPipeServerStream(name, PipeDirection.InOut, 4)) // higher max ignored
                {
                    // NPSS was created with a max of 3, and we're creating a 4th, so it fails.
                    Assert.Throws<IOException>(() => new NamedPipeServerStream(name, PipeDirection.InOut, 3));
                }

                using (new NamedPipeServerStream(name, PipeDirection.InOut, 3))
                using (new NamedPipeServerStream(name, PipeDirection.InOut, 3))
                {
                    // NPSS was created with a max of 3, and we've already created 3, so it fails,
                    // even if the new stream tries to raise it.
                    Assert.Throws<IOException>(() => new NamedPipeServerStream(name, PipeDirection.InOut, 4));
                    Assert.Throws<IOException>(() => new NamedPipeServerStream(name, PipeDirection.InOut, 2));
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public async Task MultipleServers_ServeMultipleClientsConcurrently(int numServers)
        {
            string name = PipeStreamConformanceTests.GetUniquePipeName();

            var servers = new NamedPipeServerStream[numServers];
            var clients = new NamedPipeClientStream[servers.Length];
            try
            {
                for (int i = 0; i < servers.Length; i++)
                {
                    servers[i] = new NamedPipeServerStream(name, PipeDirection.InOut, numServers, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                }

                for (int i = 0; i < clients.Length; i++)
                {
                    clients[i] = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
                }

                Task[] serverWaits = (from server in servers select server.WaitForConnectionAsync()).ToArray();
                Task[] clientWaits = (from client in clients select client.ConnectAsync()).ToArray();
                await serverWaits.Concat(clientWaits).ToArray().WhenAllOrAnyFailed();

                Task[] serverSends = (from server in servers select server.WriteAsync(new byte[1], 0, 1)).ToArray();
                Task<int>[] clientReceives = (from client in clients select client.ReadAsync(new byte[1], 0, 1)).ToArray();
                await serverSends.Concat(clientReceives).ToArray().WhenAllOrAnyFailed();
            }
            finally
            {
                for (int i = 0; i < clients.Length; i++)
                {
                    clients[i]?.Dispose();
                }

                for (int i = 0; i < servers.Length; i++)
                {
                    servers[i]?.Dispose();
                }
            }
        }

        [Theory]
        [InlineData(PipeOptions.None)]
        [InlineData(PipeOptions.Asynchronous)]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix currently doesn't support message mode
        public void Windows_MessagePipeTransmissionMode(PipeOptions serverOptions)
        {
            byte[] msg1 = new byte[] { 5, 7, 9, 10 };
            byte[] msg2 = new byte[] { 2, 4 };
            byte[] received1 = new byte[] { 0, 0, 0, 0 };
            byte[] received2 = new byte[] { 0, 0 };
            byte[] received3 = new byte[] { 0, 0, 0, 0 };
            byte[] received4 = new byte[] { 0, 0, 0, 0 };
            byte[] received5 = new byte[] { 0, 0 };
            byte[] received6 = new byte[] { 0, 0, 0, 0 };
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, serverOptions))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                {
                    server.ReadMode = PipeTransmissionMode.Message;
                    Assert.Equal(PipeTransmissionMode.Message, server.ReadMode);

                    client.Connect();

                    Task clientTask = Task.Run(() =>
                    {
                        client.Write(msg1, 0, msg1.Length);
                        client.Write(msg2, 0, msg2.Length);
                        client.Write(msg1, 0, msg1.Length);

                        client.Write(msg1, 0, msg1.Length);
                        client.Write(msg2, 0, msg2.Length);
                        client.Write(msg1, 0, msg1.Length);

                        int serverCount = client.NumberOfServerInstances;
                        Assert.Equal(1, serverCount);
                    });

                    Task serverTask = Task.Run(async () =>
                    {
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
                    });

                    Assert.True(Task.WaitAll(new[] { clientTask, serverTask }, TimeSpan.FromSeconds(15)));
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix doesn't support MaxNumberOfServerInstances
        public async Task Windows_Get_NumberOfServerInstances_Succeed()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 3))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
                {
                    Task serverTask = server.WaitForConnectionAsync();

                    client.Connect();
                    await serverTask;

                    Assert.True(InteropTest.TryGetNumberOfServerInstances(client.SafePipeHandle, out uint expectedNumberOfServerInstances), "GetNamedPipeHandleState failed");
                    Assert.Equal(expectedNumberOfServerInstances, (uint)client.NumberOfServerInstances);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(TokenImpersonationLevel.None, false)]
        [InlineData(TokenImpersonationLevel.Anonymous, false)]
        [InlineData(TokenImpersonationLevel.Identification, true)]
        [InlineData(TokenImpersonationLevel.Impersonation, true)]
        [InlineData(TokenImpersonationLevel.Delegation, true)]
        [PlatformSpecific(TestPlatforms.Windows)] // Win32 P/Invokes to verify the user name
        public async Task Windows_GetImpersonationUserName_Succeed(TokenImpersonationLevel level, bool expectedResult)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (var server = new NamedPipeServerStream(pipeName))
            {
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, level))
                {
                    string expectedUserName;
                    Task serverTask = server.WaitForConnectionAsync();

                    client.Connect();
                    await serverTask;

                    Assert.Equal(expectedResult, InteropTest.TryGetImpersonationUserName(server.SafePipeHandle, out expectedUserName));

                    if (!expectedResult)
                    {
                        Assert.Equal(string.Empty, expectedUserName);
                        Assert.Throws<IOException>(() => server.GetImpersonationUserName());
                    }
                    else
                    {
                        string actualUserName = server.GetImpersonationUserName();
                        Assert.NotNull(actualUserName);
                        Assert.False(string.IsNullOrWhiteSpace(actualUserName));
                        Assert.Equal(expectedUserName, actualUserName);
                    }
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invoke to verify the user name
        public async Task Unix_GetImpersonationUserName_Succeed()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

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
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix currently doesn't support message mode
        public void Unix_MessagePipeTransmissionMode()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new NamedPipeServerStream(PipeStreamConformanceTests.GetUniquePipeName(), PipeDirection.InOut, 1, PipeTransmissionMode.Message));
        }

        [Theory]
        [InlineData(PipeDirection.In)]
        [InlineData(PipeDirection.Out)]
        [InlineData(PipeDirection.InOut)]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix implementation uses bidirectional sockets
        public static void Unix_BufferSizeRoundtripping(PipeDirection direction)
        {
            int desiredBufferSize = 0;
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
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
        [PlatformSpecific(TestPlatforms.Windows)] // Unix implementation uses bidirectional sockets
        public static void Windows_BufferSizeRoundtripping()
        {
            int desiredBufferSize = 10;
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
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
        public async Task PipeTransmissionMode_Returns_Byte()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                await Task.WhenAll(server.WaitForConnectionAsync(), client.ConnectAsync());
                Assert.Equal(PipeTransmissionMode.Byte, server.TransmissionMode);
                Assert.Equal(PipeTransmissionMode.Byte, client.TransmissionMode);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix doesn't currently support message mode
        public void Windows_SetReadModeTo__PipeTransmissionModeByte()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
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
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix doesn't currently support message mode
        public void Unix_SetReadModeTo__PipeTransmissionModeByte()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
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
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Checks MaxLength for PipeName on Unix
        public void NameTooLong_MaxLengthPerPlatform()
        {
            // Increase a name's length until it fails
            ArgumentOutOfRangeException e = null;
            string name = Path.GetRandomFileName();
            for (int i = 0; ; i++)
            {
                try
                {
                    name += 'c';
                    using (var s = new NamedPipeServerStream(name))
                    using (var c = new NamedPipeClientStream(name))
                    {
                        Task t = s.WaitForConnectionAsync();
                        c.Connect();
                        t.GetAwaiter().GetResult();
                    }
                }
                catch (ArgumentOutOfRangeException exc)
                {
                    e = exc;
                    break;
                }
            }
            Assert.NotNull(e);
            Assert.NotNull(e.ActualValue);

            // Validate the length was expected
            string path = (string)e.ActualValue;
            if (OperatingSystem.IsLinux())
            {
                Assert.Equal(108, path.Length);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Assert.Equal(104, path.Length);
            }
            else
            {
                Assert.InRange(path.Length, 92, int.MaxValue);
            }
        }

        [Fact]
        public void ClientConnect_Throws_Timeout_When_Pipe_Not_Found()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeClientStream client = new NamedPipeClientStream(pipeName))
            {
                Assert.Throws<TimeoutException>(() => client.Connect(91));
            }
        }

        [Theory]
        [MemberData(nameof(GetCancellationTokens))]
        public async Task ClientConnectAsync_Throws_Timeout_When_Pipe_Not_Found(CancellationToken cancellationToken)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            using (NamedPipeClientStream client = new NamedPipeClientStream(pipeName))
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(92);
                Task waitingClient = client.ConnectAsync(timeout, cancellationToken);
                await Assert.ThrowsAsync<TimeoutException>(() => { return waitingClient; });
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix ignores MaxNumberOfServerInstances and second client also connects.
        public void ClientConnect_Throws_Timeout_When_Pipe_Busy()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (NamedPipeServerStream server = new NamedPipeServerStream(pipeName))
            using (NamedPipeClientStream firstClient = new NamedPipeClientStream(pipeName))
            using (NamedPipeClientStream secondClient = new NamedPipeClientStream(pipeName))
            {
                var ctx = new CancellationTokenSource();
                TimeSpan timeout = TimeSpan.FromMilliseconds(10_000);
                Task[] clientAndServerTasks = new[]
                    {
                        firstClient.ConnectAsync(timeout, ctx.Token),
                        Task.Run(() => server.WaitForConnection())
                    };

                Assert.True(Task.WaitAll(clientAndServerTasks, timeout));

                TimeSpan connectionTimeout = TimeSpan.FromMilliseconds(93);
                Assert.Throws<TimeoutException>(() => secondClient.Connect(connectionTimeout));
            }
        }

        [Theory]
        [MemberData(nameof(GetCancellationTokens))]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix ignores MaxNumberOfServerInstances and second client also connects.
        public async Task ClientConnectAsync_With_Cancellation_Throws_Timeout_When_Pipe_Busy(CancellationToken cancellationToken)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (NamedPipeServerStream server = new NamedPipeServerStream(pipeName))
            using (NamedPipeClientStream firstClient = new NamedPipeClientStream(pipeName))
            using (NamedPipeClientStream secondClient = new NamedPipeClientStream(pipeName))
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(10_000);
                Task[] clientAndServerTasks = new[]
                    {
                        firstClient.ConnectAsync(timeout, cancellationToken),
                        Task.Run(() => server.WaitForConnection())
                    };

                Assert.True(Task.WaitAll(clientAndServerTasks, timeout));

                TimeSpan connectionTimeout = TimeSpan.FromMilliseconds(94);
                Task waitingClient = secondClient.ConnectAsync(connectionTimeout, cancellationToken);
                await Assert.ThrowsAsync<TimeoutException>(() => { return waitingClient; });
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unix implementation doesn't rely on a timeout and cancellation token when connecting
        public async Task ClientConnectAsync_Cancel_With_InfiniteTimeout()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();

            using (var cts = new CancellationTokenSource())
            using (var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1))
            using (var firstClient = new NamedPipeClientStream(pipeName))
            using (var secondClient = new NamedPipeClientStream(pipeName))
            {
                var firstConnectionTasks = new Task[]
                    {
                        firstClient.ConnectAsync(),
                        server.WaitForConnectionAsync()
                    };

                Assert.True(Task.WaitAll(firstConnectionTasks, 1000));

                cts.CancelAfter(100);

                await Assert.ThrowsAsync<OperationCanceledException>(() => secondClient.ConnectAsync(cts.Token)).WaitAsync(1000);
            }
        }

        public static IEnumerable<object[]> GetCancellationTokens =>
            new []
            {
                new object[] { CancellationToken.None },
                new object[] { new CancellationTokenSource().Token },
            };
    }
}
