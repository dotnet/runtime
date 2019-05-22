// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class CreateSocket
    {
        public static object[][] DualModeSuccessInputs = {
            new object[] { SocketType.Stream, ProtocolType.Tcp },
            new object[] { SocketType.Dgram, ProtocolType.Udp },
        };

        public static object[][] DualModeFailureInputs = {
            new object[] { SocketType.Dgram, ProtocolType.Tcp },

            new object[] { SocketType.Rdm, ProtocolType.Tcp },
            new object[] { SocketType.Seqpacket, ProtocolType.Tcp },
            new object[] { SocketType.Unknown, ProtocolType.Tcp },
            new object[] { SocketType.Rdm, ProtocolType.Udp },
            new object[] { SocketType.Seqpacket, ProtocolType.Udp },
            new object[] { SocketType.Stream, ProtocolType.Udp },
            new object[] { SocketType.Unknown, ProtocolType.Udp },
        };

        private static bool SupportsRawSockets => AdminHelpers.IsProcessElevated();
        private static bool NotSupportsRawSockets => !SupportsRawSockets;

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(DualModeSuccessInputs))]
        public void DualMode_Success(SocketType socketType, ProtocolType protocolType)
        {
            using (new Socket(socketType, protocolType))
            {
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(DualModeFailureInputs))]
        public void DualMode_Failure(SocketType socketType, ProtocolType protocolType)
        {
            Assert.Throws<SocketException>(() => new Socket(socketType, protocolType));
        }

        public static object[][] CtorSuccessInputs = {
            new object[] { AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp },
            new object[] { AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp },
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(CtorSuccessInputs))]
        public void Ctor_Success(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            using (new Socket(addressFamily, socketType, protocolType))
            {
            }
        }

        public static object[][] CtorFailureInputs = {
            new object[] { AddressFamily.Unknown, SocketType.Stream, ProtocolType.Tcp },
            new object[] { AddressFamily.Unknown, SocketType.Dgram, ProtocolType.Udp },
            new object[] { AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetwork, SocketType.Rdm, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetwork, SocketType.Seqpacket, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetwork, SocketType.Unknown, ProtocolType.Tcp },
            new object[] { AddressFamily.InterNetwork, SocketType.Rdm, ProtocolType.Udp },
            new object[] { AddressFamily.InterNetwork, SocketType.Seqpacket, ProtocolType.Udp },
            new object[] { AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp },
            new object[] { AddressFamily.InterNetwork, SocketType.Unknown, ProtocolType.Udp },
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(CtorFailureInputs))]
        public void Ctor_Failure(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            Assert.Throws<SocketException>(() => new Socket(addressFamily, socketType, protocolType));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Icmp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.IcmpV6)]
        [ConditionalTheory(nameof(SupportsRawSockets))]
        public void Ctor_Raw_Supported_Success(AddressFamily addressFamily, ProtocolType protocolType)
        {
            using (new Socket(addressFamily, SocketType.Raw, protocolType))
            {
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetwork, ProtocolType.Icmp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetworkV6, ProtocolType.IcmpV6)]
        [ConditionalTheory(nameof(NotSupportsRawSockets))]
        public void Ctor_Raw_NotSupported_ExpectedError(AddressFamily addressFamily, ProtocolType protocolType)
        {
            SocketException e = Assert.Throws<SocketException>(() => new Socket(addressFamily, SocketType.Raw, protocolType));
            Assert.Contains(e.SocketErrorCode, new[] { SocketError.AccessDenied, SocketError.ProtocolNotSupported });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Sockets are still inheritable on netfx: https://github.com/dotnet/corefx/pull/32903")]
        [Theory]
        [InlineData(true, 0)] // Accept
        [InlineData(false, 0)]
        [InlineData(true, 1)] // AcceptAsync
        [InlineData(false, 1)]
        [InlineData(true, 2)] // Begin/EndAccept
        [InlineData(false, 2)]
        public void CtorAndAccept_SocketNotKeptAliveViaInheritance(bool validateClientOuter, int acceptApiOuter)
        {
            // Run the test in another process so as to not have trouble with other tests
            // launching child processes that might impact inheritance.
            RemoteExecutor.Invoke((validateClientString, acceptApiString) =>
            {
                bool validateClient = bool.Parse(validateClientString);
                int acceptApi = int.Parse(acceptApiString);

                // Create a listening server.
                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(int.MaxValue);
                    EndPoint ep = listener.LocalEndPoint;

                    // Create a client and connect to that listener.
                    using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        client.Connect(ep);

                        // Accept the connection using one of multiple accept mechanisms.
                        Socket server =
                            acceptApi == 0 ? listener.Accept() :
                            acceptApi == 1 ? listener.AcceptAsync().GetAwaiter().GetResult() :
                            acceptApi == 2 ? Task.Factory.FromAsync(listener.BeginAccept, listener.EndAccept, null).GetAwaiter().GetResult() :
                            throw new Exception($"Unexpected {nameof(acceptApi)}: {acceptApi}");

                        // Get streams for the client and server, and create a pipe that we'll use
                        // to communicate with a child process.
                        using (var serverStream = new NetworkStream(server, ownsSocket: true))
                        using (var clientStream = new NetworkStream(client, ownsSocket: true))
                        using (var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
                        {
                            // Create a child process that blocks waiting to receive a signal on the anonymous pipe.
                            // The whole purpose of the child is to test whether handles are inherited, so we
                            // keep the child process alive until we're done validating that handles close as expected.
                            using (RemoteExecutor.Invoke(clientPipeHandle =>
                                   {
                                       using (var clientPipe = new AnonymousPipeClientStream(PipeDirection.In, clientPipeHandle))
                                       {
                                           Assert.Equal(42, clientPipe.ReadByte());
                                       }
                                   }, serverPipe.GetClientHandleAsString()))
                            {
                                if (validateClient) // Validate that the child isn't keeping alive the "new Socket" for the client
                                {
                                    // Send data from the server to client, then validate the client gets EOF when the server closes.
                                    serverStream.WriteByte(84);
                                    Assert.Equal(84, clientStream.ReadByte());
                                    serverStream.Close();
                                    Assert.Equal(-1, clientStream.ReadByte());
                                }
                                else // Validate that the child isn't keeping alive the "listener.Accept" for the server
                                {
                                    // Send data from the client to server, then validate the server gets EOF when the client closes.
                                    clientStream.WriteByte(84);
                                    Assert.Equal(84, serverStream.ReadByte());
                                    clientStream.Close();
                                    Assert.Equal(-1, serverStream.ReadByte());
                                }

                                // And validate that we after closing the listening socket, we're not able to connect.
                                listener.Dispose();
                                using (var tmpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                {
                                    Assert.ThrowsAny<SocketException>(() => tmpClient.Connect(ep));
                                }

                                // Let the child process terminate.
                                serverPipe.WriteByte(42);
                            }
                        }
                    }
                }
            }, validateClientOuter.ToString(), acceptApiOuter.ToString()).Dispose();
        }
    }
}
