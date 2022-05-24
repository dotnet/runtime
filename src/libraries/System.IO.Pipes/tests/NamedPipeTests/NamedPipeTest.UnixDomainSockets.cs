// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_UnixDomainSockets
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49873", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public void NamedPipeServer_Connects_With_UnixDomainSocketEndPointClient()
        {
            string pipeName = Path.Combine("/tmp", "pipe-tests-corefx-" + Path.GetRandomFileName());
            var endPoint = new UnixDomainSocketEndPoint(pipeName);

            using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
            using (var sockectClient = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                sockectClient.Connect(endPoint);
                Assert.True(File.Exists(pipeName));
            }

            Assert.False(File.Exists(pipeName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49873", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        public async Task NamedPipeClient_Connects_With_UnixDomainSocketEndPointServer()
        {
            string pipeName = Path.Combine("/tmp", "pipe-tests-corefx-" + Path.GetRandomFileName());
            var endPoint = new UnixDomainSocketEndPoint(pipeName);

            using (var socketServer = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                socketServer.Bind(endPoint);
                socketServer.Listen(1);

                var pipeConnectTask = pipeClient.ConnectAsync(15_000);
                using (Socket accepted = socketServer.Accept())
                {
                    await pipeConnectTask;
                    Assert.True(File.Exists(pipeName));
                }
            }

            Assert.False(File.Exists(pipeName));
        }
    }
}
