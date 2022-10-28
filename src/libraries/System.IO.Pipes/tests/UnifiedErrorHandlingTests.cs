// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public static class UnifiedErrorHandlingTests
    {
        [Fact]
        public static void WhenAnonymousPipeServerIsClosedAnonymousPipeClientReadReturnsZero()
            => DiposeServerAndVerifyClientBehaviour(
                    GetAnonymousPipeStreams(PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteRead);

        [Fact]
        public static void WhenAnonymousPipeServerIsClosedFileStreamClientReadReturnsZero()
            => DiposeServerAndVerifyClientBehaviour(
                    GetAnonymousPipeServerAndFileStreamClient(PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteRead);

        [Fact]
        public static void WhenAnonymousPipeServerIsClosedAnonymousPipeClientWriteThrows()
            => DiposeServerAndVerifyClientBehaviour(
                    GetAnonymousPipeStreams(PipeDirection.In, PipeDirection.Out),
                    AssertWriteThrows);

        [Fact]
        public static void WhenAnonymousPipeServerIsClosedFileStreamClientWriteThrows()
            => DiposeServerAndVerifyClientBehaviour(
                    GetAnonymousPipeServerAndFileStreamClient(PipeDirection.In, FileAccess.Write),
                    AssertWriteThrows);

        [Fact]
        public static Task WhenAnonymousPipeServerIsClosedAnonymousPipeClientReadAsyncReturnsZero()
            => DiposeServerAndVerifyClientBehaviourAsync(
                    GetAnonymousPipeStreams(PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteReadAsync);

        [Fact]
        public static Task WhenAnonymousPipeServerIsClosedFileStreamClientReadAsyncReturnsZero()
            => DiposeServerAndVerifyClientBehaviourAsync(
                    GetAnonymousPipeServerAndFileStreamClient(PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteReadAsync);

        [Fact]
        public static Task WhenAnonymousPipeServerIsClosedAnonymousPipeClientWriteAsyncThrows()
            => DiposeServerAndVerifyClientBehaviourAsync(
                    GetAnonymousPipeStreams(PipeDirection.In, PipeDirection.Out),
                    AssertWriteAsyncThrows);

        [Fact]
        public static Task WhenAnonymousPipeServerIsClosedFileStreamClientWriteAsyncThrows()
            => DiposeServerAndVerifyClientBehaviourAsync(
                    GetAnonymousPipeServerAndFileStreamClient(PipeDirection.In, FileAccess.Write),
                    AssertWriteAsyncThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerIsClosedNamedPipeClientReadReturnsZero(bool asyncHandles)
            => DiposeServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteRead);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerIsClosedFileStreamClientReadReturnsZero(bool asyncHandles)
            => DiposeServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteRead);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerIsClosedNamedPipeClientWriteThrows(bool asyncHandles)
            => DiposeServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.In, PipeDirection.Out),
                    AssertWriteThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerIsClosedFileStreamClientWriteThrows(bool asyncHandles)
            => DiposeServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.In, FileAccess.Write),
                    AssertWriteThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerIsClosedNamedPipeClientReadAsyncReturnsZero(bool asyncHandles)
            => await DiposeServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteReadAsync);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerIsClosedFileStreamClientReadAsyncReturnsZero(bool asyncHandles)
            => await DiposeServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteReadAsync);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerIsClosedNamedPipeClientWriteAsyncThrows(bool asyncHandles)
            => await DiposeServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.In, PipeDirection.Out),
                    AssertWriteAsyncThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerIsClosedFileStreamClientWriteAsyncThrows(bool asyncHandles)
            => await DiposeServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.In, FileAccess.Write),
                    AssertWriteAsyncThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerDisconnectsNamedPipeClientReadReturnsZero(bool asyncHandles)
            => DisconnectServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteRead);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerDisconnectsFileStreamClientReadReturnsZero(bool asyncHandles)
            => DisconnectServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteRead);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerDisconnectsNamedPipeClientWriteThrows(bool asyncHandles)
            => DisconnectServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.In, PipeDirection.Out),
                    AssertWriteThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerDisconnectsFileStreamClientWriteThrows(bool asyncHandles)
            => DisconnectServerAndVerifyClientBehaviour(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.In, FileAccess.Write),
                    AssertWriteThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerDisconnectsNamedPipeClientReadAsyncReturnsZero(bool asyncHandles)
            => await DisconnectServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.Out, PipeDirection.In),
                    AssertZeroByteReadAsync);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerDisconnectsFileStreamClientReadAsyncReturnsZero(bool asyncHandles)
            => await DisconnectServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.Out, FileAccess.Read),
                    AssertZeroByteReadAsync);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WhenNamedPipeServerDisconnectsNamedPipeClientWriteAsyncThrows(bool asyncHandles)
            => await DisconnectServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeStreams(asyncHandles, PipeDirection.In, PipeDirection.Out),
                    AssertWriteAsyncThrows);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static async Task WhenNamedPipeServerDisconnectsFileStreamClientWriteAsyncThrows(bool asyncHandles)
            => await DisconnectServerAndVerifyClientBehaviourAsync(
                    await GetConnectedNamedPipeServerAndFileStreamClientStreams(asyncHandles, PipeDirection.In, FileAccess.Write),
                    AssertWriteAsyncThrows);

        // TODO: CopyToAsync

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
            FileStream client = new(new SafeFileHandle(nint.Parse(server.GetClientHandleAsString()), ownsHandle: true), clientAccess, 0);

            return (server, client);
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

        private static void DiposeServerAndVerifyClientBehaviour((Stream server, Stream client) connectedStreams, Action<Stream> assertMethod)
        {
            connectedStreams.server.Dispose();

            assertMethod(connectedStreams.client);
        }

        private static void DisconnectServerAndVerifyClientBehaviour((NamedPipeServerStream server, Stream client) connectedStreams, Action<Stream> assertMethod)
        {
            connectedStreams.server.Disconnect();

            assertMethod(connectedStreams.client);
        }

        private static async Task DiposeServerAndVerifyClientBehaviourAsync((Stream server, Stream client) connectedStreams, Func<Stream, Task> assertMethod)
        {
            await connectedStreams.server.DisposeAsync();

            await assertMethod(connectedStreams.client);
        }

        private static async Task DisconnectServerAndVerifyClientBehaviourAsync((NamedPipeServerStream server, Stream client) connectedStreams, Func<Stream, Task> assertMethod)
        {
            connectedStreams.server.Disconnect();

            await assertMethod(connectedStreams.client);
        }

        private static void AssertZeroByteRead(Stream client)
        {
            using (client)
            {
                Assert.Equal(0, client.Read(new byte[100]));
                Assert.Equal(0, client.Read(new byte[100], 0, 100));
            }
        }

        private static void AssertWriteThrows(Stream client)
        {
            using (client)
            {
                Assert.Throws<IOException>(() => client.Write(new byte[100], 0, 100));
                Assert.Throws<IOException>(() => client.Write(new byte[100]));
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

        private static async Task AssertWriteAsyncThrows(Stream client)
        {
            using (client)
            {
                await Assert.ThrowsAsync<IOException>(() => client.WriteAsync(new byte[100], 0, 100));
                await Assert.ThrowsAsync<IOException>(() => client.WriteAsync(new byte[100]).AsTask());
            }
        }
    }
}
