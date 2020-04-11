// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class CreateSocket
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

        [OuterLoop]
        [Theory, MemberData(nameof(DualModeSuccessInputs))]
        public void DualMode_Success(SocketType socketType, ProtocolType protocolType)
        {
            using (new Socket(socketType, protocolType))
            {
            }
        }

        [OuterLoop]
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

        [OuterLoop]
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

        [OuterLoop]
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

        [Theory]
        [InlineData(true, 0)] // Accept
        [InlineData(false, 0)]
        [InlineData(true, 1)] // AcceptAsync
        [InlineData(false, 1)]
        [InlineData(true, 2)] // Begin/EndAccept
        [InlineData(false, 2)]
        public void CtorAndAccept_SocketNotKeptAliveViaInheritance(bool validateClientOuter, int acceptApiOuter)
        {
            // 300 ms should be long enough to connect if the socket is actually present & listening.
            const int ConnectionTimeoutMs = 300;

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
                    listener.Listen();
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
                                    bool connected = tmpClient.TryConnect(ep, ConnectionTimeoutMs);

                                    // Let the child process terminate.
                                    serverPipe.WriteByte(42);

                                    Assert.False(connected);
                                }
                            }
                        }
                    }
                }
            }, validateClientOuter.ToString(), acceptApiOuter.ToString()).Dispose();
        }

        [Theory]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [PlatformSpecific(~TestPlatforms.Linux)]
        public void Ctor_Netcoreapp_Throws(AddressFamily addressFamily)
        {
            // All protocols are Linux specific and throw on other platforms
            Assert.Throws<SocketException>(() => new Socket(addressFamily, SocketType.Raw, 0));
        }

        [Theory]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void Ctor_Netcoreapp_Success(AddressFamily addressFamily)
        {
            Socket s = null;
            try
            {
                s = new Socket(addressFamily, SocketType.Raw, ProtocolType.Raw);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AccessDenied ||
                                            e.SocketErrorCode == SocketError.ProtocolNotSupported ||
                                            e.SocketErrorCode == SocketError.AddressFamilyNotSupported)
            {
                // Ignore. We may not have privilege or protocol modules are not loaded.
                return;
            }
            s.Close();
        }

        [Fact]
        public void Ctor_SafeHandle_Invalid_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("handle", () => new Socket(null));
            AssertExtensions.Throws<ArgumentException>("handle", () => new Socket(new SafeSocketHandle((IntPtr)(-1), false)));

            using (var pipe = new AnonymousPipeServerStream())
            {
                SocketException se = Assert.Throws<SocketException>(() => new Socket(new SafeSocketHandle(pipe.ClientSafePipeHandle.DangerousGetHandle(), false)));
                Assert.Equal(SocketError.NotSocket, se.SocketErrorCode);
            }
        }

        [Theory]
        [InlineData(AddressFamily.ControllerAreaNetwork, SocketType.Raw, ProtocolType.Unspecified)]
        [InlineData(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Unspecified)]
        [InlineData(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)]
        [InlineData(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetworkV6, SocketType.Raw, ProtocolType.Unspecified)]
        [InlineData(AddressFamily.Packet, SocketType.Raw, ProtocolType.Raw)]
        [InlineData(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)]
        public void Ctor_SafeHandle_BasicPropertiesPropagate_Success(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            Socket tmpOrig;
            try
            {
                tmpOrig = new Socket(addressFamily, socketType, protocolType);
            }
            catch (SocketException e) when (
                e.SocketErrorCode == SocketError.AccessDenied ||
                e.SocketErrorCode == SocketError.ProtocolNotSupported ||
                e.SocketErrorCode == SocketError.AddressFamilyNotSupported)
            {
                // We can't test this combination on this platform.
                return;
            }

            using Socket orig = tmpOrig;
            using var copy = new Socket(orig.SafeHandle);

            Assert.False(orig.Connected);
            Assert.False(copy.Connected);

            Assert.Null(orig.LocalEndPoint);
            Assert.Null(orig.RemoteEndPoint);
            Assert.False(orig.IsBound);
            if (copy.IsBound)
            {
                // On Unix, we may successfully obtain an (empty) local end point, even though Bind wasn't called.
                Debug.Assert(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // OSX gets some strange results in some cases, e.g. "@\0\0\0\0\0\0\0\0\0\0\0\0\0" for a UDS
                {
                    switch (addressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            Assert.Equal(new IPEndPoint(IPAddress.Any, 0), copy.LocalEndPoint);
                            break;

                        case AddressFamily.InterNetworkV6:
                            Assert.Equal(new IPEndPoint(IPAddress.IPv6Any, 0), copy.LocalEndPoint);
                            break;

                        case AddressFamily.Unix:
                            Assert.IsType<UnixDomainSocketEndPoint>(copy.LocalEndPoint);
                            Assert.Equal("", copy.LocalEndPoint.ToString());
                            break;

                        default:
                            Assert.Null(copy.LocalEndPoint);
                            break;
                    }
                }
            }
            else
            {
                Assert.Equal(orig.LocalEndPoint, copy.LocalEndPoint);
                Assert.Equal(orig.LocalEndPoint, copy.RemoteEndPoint);
            }

            Assert.Equal(addressFamily, orig.AddressFamily);
            Assert.Equal(socketType, orig.SocketType);
            Assert.Equal(protocolType, orig.ProtocolType);

            Assert.Equal(addressFamily, copy.AddressFamily);
            Assert.Equal(socketType, copy.SocketType);
            Assert.True(copy.ProtocolType == orig.ProtocolType || copy.ProtocolType == ProtocolType.Unknown, $"Expected: {protocolType} or Unknown, Actual: {copy.ProtocolType}");

            Assert.True(orig.Blocking);
            Assert.True(copy.Blocking);

            if (orig.AddressFamily == copy.AddressFamily)
            {
                AssertEqualOrSameException(() => orig.DontFragment, () => copy.DontFragment);
                AssertEqualOrSameException(() => orig.MulticastLoopback, () => copy.MulticastLoopback);
                AssertEqualOrSameException(() => orig.Ttl, () => copy.Ttl);
            }

            AssertEqualOrSameException(() => orig.EnableBroadcast, () => copy.EnableBroadcast);
            AssertEqualOrSameException(() => orig.LingerState.Enabled, () => copy.LingerState.Enabled);
            AssertEqualOrSameException(() => orig.LingerState.LingerTime, () => copy.LingerState.LingerTime);
            AssertEqualOrSameException(() => orig.NoDelay, () => copy.NoDelay);

            Assert.Equal(orig.Available, copy.Available);
            Assert.Equal(orig.ExclusiveAddressUse, copy.ExclusiveAddressUse);
            Assert.Equal(orig.Handle, copy.Handle);
            Assert.Equal(orig.ReceiveBufferSize, copy.ReceiveBufferSize);
            Assert.Equal(orig.ReceiveTimeout, copy.ReceiveTimeout);
            Assert.Equal(orig.SendBufferSize, copy.SendBufferSize);
            Assert.Equal(orig.SendTimeout, copy.SendTimeout);
            Assert.Equal(orig.UseOnlyOverlappedIO, copy.UseOnlyOverlappedIO);
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)]
        [InlineData(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)]
        public async Task Ctor_SafeHandle_Tcp_SendReceive_Success(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            using var orig = new Socket(addressFamily, socketType, protocolType);
            using var listener = new Socket(addressFamily, socketType, protocolType);
            listener.Bind(new IPEndPoint(addressFamily == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback, 0));
            listener.Listen(1);
            await orig.ConnectAsync(listener.LocalEndPoint);
            using var server = await listener.AcceptAsync();

            using var client = new Socket(orig.SafeHandle);

            Assert.True(client.Connected);
            Assert.Equal(orig.AddressFamily, client.AddressFamily);
            Assert.Equal(orig.SocketType, client.SocketType);
            Assert.True(client.ProtocolType == orig.ProtocolType || client.ProtocolType == ProtocolType.Unknown, $"Expected: {protocolType} or Unknown, Actual: {client.ProtocolType}");

            // Validate accessing end points
            Assert.Equal(orig.LocalEndPoint, client.LocalEndPoint);
            Assert.Equal(orig.RemoteEndPoint, client.RemoteEndPoint);

            // Validating accessing other properties
            Assert.Equal(orig.Available, client.Available);
            Assert.True(orig.Blocking);
            Assert.True(client.Blocking);
            AssertEqualOrSameException(() => orig.DontFragment, () => client.DontFragment);
            AssertEqualOrSameException(() => orig.EnableBroadcast, () => client.EnableBroadcast);
            Assert.Equal(orig.ExclusiveAddressUse, client.ExclusiveAddressUse);
            Assert.Equal(orig.Handle, client.Handle);
            Assert.Equal(orig.IsBound, client.IsBound);
            Assert.Equal(orig.LingerState.Enabled, client.LingerState.Enabled);
            Assert.Equal(orig.LingerState.LingerTime, client.LingerState.LingerTime);
            AssertEqualOrSameException(() => orig.MulticastLoopback, () => client.MulticastLoopback);
            Assert.Equal(orig.NoDelay, client.NoDelay);
            Assert.Equal(orig.ReceiveBufferSize, client.ReceiveBufferSize);
            Assert.Equal(orig.ReceiveTimeout, client.ReceiveTimeout);
            Assert.Equal(orig.SendBufferSize, client.SendBufferSize);
            Assert.Equal(orig.SendTimeout, client.SendTimeout);
            Assert.Equal(orig.Ttl, client.Ttl);
            Assert.Equal(orig.UseOnlyOverlappedIO, client.UseOnlyOverlappedIO);

            // Validate setting various properties on the new instance and seeing them roundtrip back to the original.
            client.ReceiveTimeout = 42;
            Assert.Equal(client.ReceiveTimeout, orig.ReceiveTimeout);

            // Validate sending and receiving
            Assert.Equal(1, await client.SendAsync(new byte[1] { 42 }, SocketFlags.None));
            var buffer = new byte[1];
            Assert.Equal(1, await server.ReceiveAsync(buffer, SocketFlags.None));
            Assert.Equal(42, buffer[0]);

            Assert.Equal(1, await server.SendAsync(new byte[1] { 42 }, SocketFlags.None));
            buffer[0] = 0;
            Assert.Equal(1, await client.ReceiveAsync(buffer, SocketFlags.None));
            Assert.Equal(42, buffer[0]);
        }

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)] // OSX/FreeBSD doesn't support SO_ACCEPTCONN, so we can't query for whether a socket is listening
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Ctor_SafeHandle_Listening_Success(bool shareSafeHandle)
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();
            Assert.Equal(1, listener.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.AcceptConnection));

            using var listenerCopy = new Socket(shareSafeHandle ? listener.SafeHandle : new SafeSocketHandle(listener.Handle, ownsHandle: false));
            Assert.Equal(1, listenerCopy.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.AcceptConnection));

            Assert.Equal(listener.AddressFamily, listenerCopy.AddressFamily);
            Assert.Equal(listener.Handle, listenerCopy.Handle);
            Assert.Equal(listener.IsBound, listenerCopy.IsBound);
            Assert.Equal(listener.LocalEndPoint, listener.LocalEndPoint);
            Assert.True(listenerCopy.ProtocolType == listener.ProtocolType || listenerCopy.ProtocolType == ProtocolType.Unknown, $"Expected: {listener.ProtocolType} or Unknown, Actual: {listenerCopy.ProtocolType}");
            Assert.Equal(listener.SocketType, listenerCopy.SocketType);

            foreach (Socket listenerSocket in new[] { listener, listenerCopy })
            {
                using (var client1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task connect1 = client1.ConnectAsync(listenerSocket.LocalEndPoint);
                    using (Socket server1 = listenerSocket.Accept())
                    {
                        await connect1;
                        server1.Send(new byte[] { 42 });
                        Assert.Equal(1, client1.Receive(new byte[1]));
                    }
                }
            }
        }

        private static void AssertEqualOrSameException<T>(Func<T> expected, Func<T> actual)
        {
            T r1 = default, r2 = default;
            Exception e1 = null, e2 = null;

            try { r1 = expected(); }
            catch (Exception e) { e1 = e; };

            try { r2 = actual(); }
            catch (Exception e) { e2 = e; };

            Assert.Equal(e1 is null, e2 is null);
            if (e1 is null)
            {
                Assert.Equal(r1, r2);
            }
            else
            {
                Assert.Equal(e1.GetType(), e2.GetType());
            }
        }
    }
}
