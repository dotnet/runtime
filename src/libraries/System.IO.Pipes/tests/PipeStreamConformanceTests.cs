// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Tests;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public abstract class PipeStreamConformanceTests : ConnectedStreamConformanceTests
    {
        /// <summary>Get a unique pipe name very unlikely to be in use elsewhere.</summary>
        public static string GetUniquePipeName() =>
            PlatformDetection.IsInAppContainer ? @"LOCAL\" + Path.GetRandomFileName() :
            Path.GetRandomFileName();

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool CansReturnFalseAfterDispose => false;
        protected override bool FullyCancelableOperations => !OperatingSystem.IsWindows();

        [PlatformSpecific(TestPlatforms.Windows)] // WaitForPipeDrain isn't supported on Unix
        [Fact]
        public async Task PipeStream_WaitForPipeDrain()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                byte[] sent = new byte[] { 123 };
                byte[] received = new byte[] { 0 };

                Task t = Task.Run(() => writeable.Write(sent, 0, sent.Length));
                Assert.Equal(sent.Length, readable.Read(received, 0, sent.Length));
                Assert.Equal(sent, received);
                ((PipeStream)writeable).WaitForPipeDrain();
                await t;
            }
        }
    }

    public abstract class AnonymousPipeStreamConformanceTests : PipeStreamConformanceTests
    {
        protected override bool BrokenPipePropagatedImmediately => true;

        protected abstract (AnonymousPipeServerStream Server, AnonymousPipeClientStream Client) CreateServerAndClientStreams();

        protected sealed override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (AnonymousPipeServerStream server, AnonymousPipeClientStream client) = CreateServerAndClientStreams();

            Assert.True(server.IsConnected);
            Assert.True(client.IsConnected);

            return Task.FromResult<StreamPair>((server, client));
        }
    }

    public abstract class NamedPipeStreamConformanceTests : PipeStreamConformanceTests
    {
        protected override bool BrokenPipePropagatedImmediately => OperatingSystem.IsWindows(); // On Unix, implemented on Sockets, where it won't propagate immediate

        protected abstract (NamedPipeServerStream Server, NamedPipeClientStream Client) CreateServerAndClientStreams();

        protected sealed override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            await Task.WhenAll(client.ConnectAsync(), server.WaitForConnectionAsync());

            Assert.True(server.IsConnected);
            Assert.True(client.IsConnected);

            return (server, client);
        }

        protected (NamedPipeServerStream Server, NamedPipeClientStream Client) GetClientAndServer(StreamPair streams)
        {
            if (streams.Stream1 is NamedPipeServerStream)
            {
                Assert.IsType<NamedPipeClientStream>(streams.Stream2);
                return ((NamedPipeServerStream)streams.Stream1, (NamedPipeClientStream)streams.Stream2);
            }

            Assert.IsType<NamedPipeClientStream>(streams.Stream1);
            return ((NamedPipeServerStream)streams.Stream2, (NamedPipeClientStream)streams.Stream1);
        }

        /// <summary>
        /// Yields every combination of testing options for the OneWayReadWrites test
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<object[]> OneWayReadWritesMemberData() =>
            from serverOption in new[] { PipeOptions.None, PipeOptions.Asynchronous }
            from clientOption in new[] { PipeOptions.None, PipeOptions.Asynchronous }
            from asyncServerOps in new[] { false, true }
            from asyncClientOps in new[] { false, true }
            select new object[] { serverOption, clientOption, asyncServerOps, asyncClientOps };

        [Fact]
        public async Task ClonedServer_ActsAsOriginalServer()
        {
            byte[] msg1 = new byte[] { 5, 7, 9, 10 };
            byte[] received1 = new byte[] { 0, 0, 0, 0 };

            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            if (writeable is NamedPipeServerStream serverBase)
            {
                Task<int> clientTask = readable.ReadAsync(received1, 0, received1.Length);
                using (NamedPipeServerStream server = new NamedPipeServerStream(PipeDirection.Out, false, true, serverBase.SafePipeHandle))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.Equal(1, ((NamedPipeClientStream)readable).NumberOfServerInstances);
                    }
                    server.Write(msg1, 0, msg1.Length);
                    int receivedLength = await clientTask;
                    Assert.Equal(msg1.Length, receivedLength);
                    Assert.Equal(msg1, received1);
                }
            }
            else
            {
                Task clientTask = writeable.WriteAsync(msg1, 0, msg1.Length);
                using (NamedPipeServerStream server = new NamedPipeServerStream(PipeDirection.In, false, true, ((NamedPipeServerStream)readable).SafePipeHandle))
                {
                    int receivedLength = server.Read(received1, 0, msg1.Length);
                    Assert.Equal(msg1.Length, receivedLength);
                    Assert.Equal(msg1, received1);
                    await clientTask;
                }
            }
        }

        [Fact]
        public async Task ClonedClient_ActsAsOriginalClient()
        {
            byte[] msg1 = new byte[] { 5, 7, 9, 10 };
            byte[] received1 = new byte[] { 0, 0, 0, 0 };

            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            if (writeable is NamedPipeServerStream server)
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(PipeDirection.In, false, true, ((NamedPipeClientStream)readable).SafePipeHandle))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.Equal(1, client.NumberOfServerInstances);
                    }
                    Task<int> clientTask = client.ReadAsync(received1, 0, received1.Length);
                    server.Write(msg1, 0, msg1.Length);
                    int receivedLength = await clientTask;
                    Assert.Equal(msg1.Length, receivedLength);
                    Assert.Equal(msg1, received1);
                }
            }
            else
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(PipeDirection.Out, false, true, ((NamedPipeClientStream)writeable).SafePipeHandle))
                {
                    Task clientTask = client.WriteAsync(msg1, 0, msg1.Length);
                    int receivedLength = readable.Read(received1, 0, msg1.Length);
                    Assert.Equal(msg1.Length, receivedLength);
                    Assert.Equal(msg1, received1);
                    await clientTask;
                }
            }
        }

        [Fact]
        public async Task ConnectOnAlreadyConnectedClient_Throws_InvalidOperationException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            NamedPipeClientStream client = streams.Stream1 as NamedPipeClientStream ?? (NamedPipeClientStream)streams.Stream2;

            Assert.Throws<InvalidOperationException>(() => client.Connect());
        }

        [Fact]
        public async Task WaitForConnectionOnAlreadyConnectedServer_Throws_InvalidOperationException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();

            NamedPipeServerStream server = streams.Stream1 as NamedPipeServerStream ?? (NamedPipeServerStream)streams.Stream2;

            Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
        }

        [Fact]
        public async Task CancelTokenOn_ServerWaitForConnectionAsync_Throws_OperationCanceledException()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            using StreamPair streams = (server, client);

            var ctx = new CancellationTokenSource();

            Task serverWaitTimeout = server.WaitForConnectionAsync(ctx.Token);
            ctx.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWaitTimeout);

            Assert.True(server.WaitForConnectionAsync(ctx.Token).IsCanceled);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOff_ServerWaitForConnectionAsyncWithOuterCancellation_Throws_OperationCanceledException()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            using StreamPair streams = (server, client);

            Task waitForConnectionTask = server.WaitForConnectionAsync(CancellationToken.None);

            Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitForConnectionTask);
            Assert.True(waitForConnectionTask.IsCanceled);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOn_ServerWaitForConnectionAsyncWithOuterCancellation_Throws_IOException()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            using StreamPair streams = (server, client);

            var cts = new CancellationTokenSource();
            Task waitForConnectionTask = server.WaitForConnectionAsync(cts.Token);

            Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
            await Assert.ThrowsAsync<IOException>(() => waitForConnectionTask);
        }

        [Fact]
        public async Task OperationsOnDisconnectedServer()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
            Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
            await Assert.ThrowsAsync<InvalidOperationException>(() => server.WaitForConnectionAsync()); // fails because allowed connections is set to 1

            server.Disconnect();
            Assert.Throws<InvalidOperationException>(() => server.Disconnect()); // double disconnect

            byte[] buffer = new byte[4];

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                if (ReferenceEquals(writeable, server))
                {
                    Assert.Throws<InvalidOperationException>(() => server.Write(buffer, 0, buffer.Length));
                    Assert.Throws<InvalidOperationException>(() => server.WriteByte(5));
                    Assert.Throws<InvalidOperationException>(() => { server.WriteAsync(buffer, 0, buffer.Length); });
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => server.Read(buffer, 0, buffer.Length));
                    Assert.Throws<InvalidOperationException>(() => server.ReadByte());
                    Assert.Throws<InvalidOperationException>(() => { server.ReadAsync(buffer, 0, buffer.Length); });
                }
            }

            Assert.Throws<InvalidOperationException>(() => server.Flush());
            Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
            Assert.Throws<InvalidOperationException>(() => server.GetImpersonationUserName());
        }

        [Fact]
        public virtual async Task OperationsOnDisconnectedClient()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            Assert.Throws<InvalidOperationException>(() => client.IsMessageComplete);
            Assert.Throws<InvalidOperationException>(() => client.Connect());
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync());

            server.Disconnect();

            var buffer = new byte[4];

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                if (ReferenceEquals(writeable, client))
                {
                    if (OperatingSystem.IsWindows()) // writes on Unix may still succeed after other end disconnects, due to socket being used
                    {
                        // Pipe is broken
                        Assert.Throws<IOException>(() => client.Write(buffer, 0, buffer.Length));
                        Assert.Throws<IOException>(() => client.WriteByte(5));
                        Assert.Throws<IOException>(() => { client.WriteAsync(buffer, 0, buffer.Length); });
                        Assert.Throws<IOException>(() => client.Flush());
                        Assert.Throws<IOException>(() => client.NumberOfServerInstances);
                    }
                }
                else
                {
                    // Nothing for the client to read, but no exception throwing
                    Assert.Equal(0, client.Read(buffer, 0, buffer.Length));
                    Assert.Equal(-1, client.ReadByte());

                    if (!OperatingSystem.IsWindows()) // NumberOfServerInstances not supported on Unix
                    {
                        Assert.Throws<PlatformNotSupportedException>(() => client.NumberOfServerInstances);
                    }
                }
            }

            Assert.Throws<InvalidOperationException>(() => client.IsMessageComplete);
        }

        [Fact]
        public async Task Windows_OperationsOnNamedServerWithDisposedClient()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            client.Dispose();

            if (OperatingSystem.IsWindows())
            {
                Assert.Throws<IOException>(() => server.WaitForConnection());
                await Assert.ThrowsAsync<IOException>(() => server.WaitForConnectionAsync());
                Assert.Throws<IOException>(() => server.GetImpersonationUserName());
            }
            else
            {
                // On Unix, the server still thinks that it is connected after client Disposal.
                Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
                await Assert.ThrowsAsync<InvalidOperationException>(() => server.WaitForConnectionAsync());
                Assert.NotNull(server.GetImpersonationUserName());
            }
        }

        [Fact]
        public void OperationsOnUnconnectedServer()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            using StreamPair streams = (server, client);

            // doesn't throw exceptions
            PipeTransmissionMode transmitMode = server.TransmissionMode;
            Assert.Throws<ArgumentOutOfRangeException>(() => server.ReadMode = (PipeTransmissionMode)999);

            var buffer = new byte[4];

            foreach ((Stream writeable, Stream readable) in GetReadWritePairs(streams))
            {
                if (ReferenceEquals(writeable, server))
                {
                    Assert.Equal(0, server.OutBufferSize);
                    Assert.Throws<InvalidOperationException>(() => server.Write(buffer, 0, buffer.Length));
                    Assert.Throws<InvalidOperationException>(() => server.WriteByte(5));
                    Assert.Throws<InvalidOperationException>(() => { server.WriteAsync(buffer, 0, buffer.Length); });
                }
                else
                {
                    Assert.Equal(0, server.InBufferSize);
                    PipeTransmissionMode readMode = server.ReadMode;
                    Assert.Throws<InvalidOperationException>(() => server.Read(buffer, 0, buffer.Length));
                    Assert.Throws<InvalidOperationException>(() => server.ReadByte());
                    Assert.Throws<InvalidOperationException>(() => { server.ReadAsync(buffer, 0, buffer.Length); });
                }
            }

            Assert.Throws<InvalidOperationException>(() => server.Disconnect());    // disconnect when not connected
            Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
        }

        [Fact]
        public void OperationsOnUnconnectedClient()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            using StreamPair streams = (server, client);

            var buffer = new byte[4];

            if (client.CanRead)
            {
                Assert.Throws<InvalidOperationException>(() => client.Read(buffer, 0, buffer.Length));
                Assert.Throws<InvalidOperationException>(() => client.ReadByte());
                Assert.Throws<InvalidOperationException>(() => { client.ReadAsync(buffer, 0, buffer.Length); });
                Assert.Throws<InvalidOperationException>(() => client.ReadMode);
                Assert.Throws<InvalidOperationException>(() => client.ReadMode = PipeTransmissionMode.Byte);
            }

            if (client.CanWrite)
            {
                Assert.Throws<InvalidOperationException>(() => client.Write(buffer, 0, buffer.Length));
                Assert.Throws<InvalidOperationException>(() => client.WriteByte(5));
                Assert.Throws<InvalidOperationException>(() => { client.WriteAsync(buffer, 0, buffer.Length); });
            }

            Assert.Throws<InvalidOperationException>(() => client.NumberOfServerInstances);
            Assert.Throws<InvalidOperationException>(() => client.TransmissionMode);
            Assert.Throws<InvalidOperationException>(() => client.InBufferSize);
            Assert.Throws<InvalidOperationException>(() => client.OutBufferSize);
            Assert.Throws<InvalidOperationException>(() => client.SafePipeHandle);
        }

        [Fact]
        public async Task DisposedServerPipe_Throws_ObjectDisposedException()
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = CreateServerAndClientStreams();
            server.Dispose();

            Assert.Throws<ObjectDisposedException>(() => server.Disconnect());
            Assert.Throws<ObjectDisposedException>(() => server.GetImpersonationUserName());
            Assert.Throws<ObjectDisposedException>(() => server.WaitForConnection());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => server.WaitForConnectionAsync());
        }

        [Fact]
        public async Task DisposedClientPipe_Throws_ObjectDisposedException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => client.Connect());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.ConnectAsync());
            Assert.Throws<ObjectDisposedException>(() => client.NumberOfServerInstances);
        }

        [Fact]
        public async Task ReadAsync_DisconnectDuringRead_Returns0()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            Task<int> readTask = readable.ReadAsync(new byte[1], 0, 1);
            writeable.Dispose();
            Assert.Equal(0, await readTask);
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Unix named pipes are on sockets, where small writes with an empty buffer will succeed immediately
        [Fact]
        public async Task WriteAsync_DisconnectDuringWrite_Throws()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (Stream writeable, Stream readable) = GetReadWritePair(streams);

            Task writeTask = writeable.WriteAsync(new byte[1], 0, 1);
            readable.Dispose();
            await Assert.ThrowsAsync<IOException>(() => writeTask);
        }

        [Fact]
        public async Task Server_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (server.CanRead && client.CanWrite)
            {
                var ctx1 = new CancellationTokenSource();

                Task<int> serverReadToken = server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token);
                ctx1.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverReadToken);

                ctx1.Cancel();
                Assert.True(server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token).IsCanceled);
            }

            if (server.CanWrite)
            {
                var ctx1 = new CancellationTokenSource();
                if (OperatingSystem.IsWindows()) // On Unix WriteAsync's aren't cancelable once initiated
                {
                    Task serverWriteToken = server.WriteAsync(buffer, 0, buffer.Length, ctx1.Token);
                    ctx1.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWriteToken);
                }
                ctx1.Cancel();
                Assert.True(server.WriteAsync(buffer, 0, buffer.Length, ctx1.Token).IsCanceled);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOff_Server_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (server.CanRead)
            {
                Task serverReadToken = server.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverReadToken);
                Assert.True(serverReadToken.IsCanceled);
            }

            if (server.CanWrite)
            {
                Task serverWriteToken = server.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWriteToken);
                Assert.True(serverWriteToken.IsCanceled);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOn_Server_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (server.CanRead)
            {
                var cts = new CancellationTokenSource();
                Task serverReadToken = server.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverReadToken);
            }
            if (server.CanWrite)
            {
                var cts = new CancellationTokenSource();
                Task serverWriteToken = server.WriteAsync(buffer, 0, buffer.Length, cts.Token);

                Assert.True(InteropTest.CancelIoEx(server.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWriteToken);
            }
        }

        [Fact]
        public async Task Client_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (client.CanRead)
            {
                var ctx1 = new CancellationTokenSource();

                Task serverReadToken = client.ReadAsync(buffer, 0, buffer.Length, ctx1.Token);
                ctx1.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverReadToken);

                Assert.True(client.ReadAsync(buffer, 0, buffer.Length, ctx1.Token).IsCanceled);
            }

            if (client.CanWrite)
            {
                var ctx1 = new CancellationTokenSource();
                if (OperatingSystem.IsWindows()) // On Unix WriteAsync's aren't cancelable once initiated
                {
                    Task serverWriteToken = client.WriteAsync(buffer, 0, buffer.Length, ctx1.Token);
                    ctx1.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWriteToken);
                }
                ctx1.Cancel();
                Assert.True(client.WriteAsync(buffer, 0, buffer.Length, ctx1.Token).IsCanceled);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOff_Client_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (client.CanRead)
            {
                Task clientReadToken = client.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.True(InteropTest.CancelIoEx(client.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientReadToken);
                Assert.True(clientReadToken.IsCanceled);
            }

            if (client.CanWrite)
            {
                Task clientWriteToken = client.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.True(InteropTest.CancelIoEx(client.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientWriteToken);
                Assert.True(clientWriteToken.IsCanceled);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoking to Win32 functions
        public async Task CancelTokenOn_Client_ReadWriteCancelledToken_Throws_OperationCanceledException()
        {
            using StreamPair streams = await CreateConnectedStreamsAsync();
            (NamedPipeServerStream server, NamedPipeClientStream client) = GetClientAndServer(streams);

            var buffer = new byte[4];

            if (client.CanRead)
            {
                var cts = new CancellationTokenSource();
                Task clientReadToken = client.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                Assert.True(InteropTest.CancelIoEx(client.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientReadToken);
            }

            if (client.CanWrite)
            {
                var cts = new CancellationTokenSource();
                Task clientWriteToken = client.WriteAsync(buffer, 0, buffer.Length, cts.Token);

                Assert.True(InteropTest.CancelIoEx(client.SafePipeHandle), "Outer cancellation failed");
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientWriteToken);
            }
        }
    }

    public sealed class AnonymousPipeTest_ServerIn_ClientOut : AnonymousPipeStreamConformanceTests
    {
        protected override (AnonymousPipeServerStream Server, AnonymousPipeClientStream Client) CreateServerAndClientStreams()
        {
            var server = new AnonymousPipeServerStream(PipeDirection.In);
            var client = new AnonymousPipeClientStream(PipeDirection.Out, server.ClientSafePipeHandle);
            return (server, client);
        }
    }

    public sealed class AnonymousPipeTest_ServerOut_ClientIn : AnonymousPipeStreamConformanceTests
    {
        protected override (AnonymousPipeServerStream Server, AnonymousPipeClientStream Client) CreateServerAndClientStreams()
        {
            var server = new AnonymousPipeServerStream(PipeDirection.Out);
            var client = new AnonymousPipeClientStream(PipeDirection.In, server.ClientSafePipeHandle);
            return (server, client);
        }
    }

    public sealed class NamedPipeTest_ServerOut_ClientIn : NamedPipeStreamConformanceTests
    {
        protected override (NamedPipeServerStream Server, NamedPipeClientStream Client) CreateServerAndClientStreams()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
            return (server, client);
        }
    }

    public sealed class NamedPipeTest_ServerIn_ClientOut : NamedPipeStreamConformanceTests
    {
        protected override (NamedPipeServerStream Server, NamedPipeClientStream Client) CreateServerAndClientStreams()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            return (server, client);
        }
    }

    public sealed class NamedPipeTest_ServerInOut_ClientInOut : NamedPipeStreamConformanceTests
    {
        protected override (NamedPipeServerStream Server, NamedPipeClientStream Client) CreateServerAndClientStreams()
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            return (server, client);
        }
    }
}
