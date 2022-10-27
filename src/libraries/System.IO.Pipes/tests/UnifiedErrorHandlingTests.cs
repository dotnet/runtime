// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class UnifiedErrorHandlingTests
    {
        [Fact]
        public async Task WhenAnonymousPipeServerIsClosedAnonymousPipeClientReadAsyncReturnsZero()
        {
            (AnonymousPipeServerStream server, AnonymousPipeClientStream client) = GetAnonymousPipeStreams(PipeDirection.Out, PipeDirection.In);

            await server.DisposeAsync();

            await AssertZeroByteReadAsync(client);
        }

        [Fact]
        public async Task WhenAnonymousPipeServerIsClosedFileStreamClientReadAsyncReturnsZero()
        {
            (AnonymousPipeServerStream server, FileStream client) = GetAnonymousPipeServerAndFileStreamClient(PipeDirection.Out, FileAccess.Read);

            await server.DisposeAsync();

            await AssertZeroByteReadAsync(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenNamedPipeServerIsClosedNamedPipeClientReadAsyncReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In);

            await server.DisposeAsync();

            await AssertZeroByteReadAsync(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)] // OS-specific accessing named pipe via path
        public async Task WhenNamedPipeServerIsClosedFileStreamClientReadAsyncReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, FileStream client) = await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read);

            await server.DisposeAsync();

            await AssertZeroByteReadAsync(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenNamedPipeServerDisconnectsNamedPipeClientReadAsyncReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In);

            using (server)
            {
                server.Disconnect();

                await AssertZeroByteReadAsync(client);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)] // OS-specific accessing named pipe via path
        public async Task WhenNamedPipeServerDisconnectsFileStreamClientReadAsyncReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, FileStream client) = await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read);

            using (server)
            {
                server.Disconnect();

                await AssertZeroByteReadAsync(client);
            }
        }

        [Fact]
        public void WhenAnonymousPipeServerIsClosedAnonymousPipeClientReadReturnsZero()
        {
            (AnonymousPipeServerStream server, AnonymousPipeClientStream client) = GetAnonymousPipeStreams(PipeDirection.Out, PipeDirection.In);

            server.Dispose();

            AssertZeroByteRead(client);
        }

        [Fact]
        public void WhenAnonymousPipeServerIsClosedFileStreamClientReadReturnsZero()
        {
            (AnonymousPipeServerStream server, FileStream client) = GetAnonymousPipeServerAndFileStreamClient(PipeDirection.Out, FileAccess.Read);

            server.Dispose();

            AssertZeroByteRead(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenNamedPipeServerIsClosedNamedPipeClientReadReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In);

            server.Dispose();

            AssertZeroByteRead(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task WhenNamedPipeServerIsClosedFileStreamClientReadReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, FileStream client) = await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read);

            server.Dispose();

            AssertZeroByteRead(client);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenNamedPipeServerDisconnectsNamedPipeClientReadReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, NamedPipeClientStream client) = await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In);

            using (server)
            {
                server.Disconnect();

                AssertZeroByteRead(client);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task WhenNamedPipeServerDisconnectsFileStreamClientReadReturnsZero(bool asyncHandles)
        {
            (NamedPipeServerStream server, FileStream client) = await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read);

            using (server)
            {
                server.Disconnect();

                AssertZeroByteRead(client);
            }
        }

        private static async Task AssertZeroByteReadAsync(Stream client)
        {
            using (client)
            {
                Assert.Equal(0, await client.ReadAsync(new byte[100]));
                Assert.Equal(0, await client.ReadAsync(new byte[100], 0, 100));
            }
        }

        private static void AssertZeroByteRead(Stream client)
        {
            using (client)
            {
                Assert.Equal(0, client.Read(new byte[100]));
                Assert.Equal(0, client.Read(new byte[100], 0, 100));
            }
        }

        private static async Task<(NamedPipeServerStream server, NamedPipeClientStream client)> GetConnectedNamedPipeStreams(
            bool asyncHandles, PipeDirection serverDirection, PipeDirection clientDirection)
        {
            PipeOptions options = asyncHandles ? PipeOptions.Asynchronous : PipeOptions.None;
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            NamedPipeServerStream server = new(pipeName, serverDirection, 1, PipeTransmissionMode.Byte, options);
            NamedPipeClientStream client = new(".", pipeName, clientDirection, options);

            await Task.WhenAll(client.ConnectAsync(), server.WaitForConnectionAsync());

            return (server, client);
        }

        private static async Task<(NamedPipeServerStream server, FileStream client)> GetConnectedNamedPipeServerAndFileStreamClientStreams(
            bool asyncHandles, PipeDirection serverDirection, FileAccess clientAccess)
        {
            PipeOptions pipeOptions = asyncHandles ? PipeOptions.Asynchronous : PipeOptions.None;
            FileOptions fileOptions = asyncHandles ? FileOptions.Asynchronous : FileOptions.None;
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            NamedPipeServerStream server = new(pipeName, serverDirection, 1, PipeTransmissionMode.Byte, pipeOptions);
            FileStream client = new($@"\\.\pipe\{pipeName}", FileMode.Open, clientAccess, FileShare.None, 0, fileOptions);

            await server.WaitForConnectionAsync();

            return (server, client);
        }

        private static (AnonymousPipeServerStream server, AnonymousPipeClientStream client) GetAnonymousPipeStreams(
            PipeDirection serverDirection, PipeDirection clientDirection)
        {
            AnonymousPipeServerStream server = new(serverDirection);
            AnonymousPipeClientStream client = new(clientDirection, server.ClientSafePipeHandle);

            return (server, client);
        }

        private static (AnonymousPipeServerStream server, FileStream client) GetAnonymousPipeServerAndFileStreamClient(
            PipeDirection serverDirection, FileAccess clientAccess)
        {
            AnonymousPipeServerStream server = new(serverDirection);
            FileStream client = new(new SafeFileHandle(nint.Parse(server.GetClientHandleAsString()), ownsHandle: true), clientAccess);

            return (server, client);
        }
    }
}
