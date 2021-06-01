// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class CreateSocket
    {
        readonly ITestOutputHelper _output;

        public CreateSocket(ITestOutputHelper output)
        {
            _output = output;
        }

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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
        [SkipOnPlatform(TestPlatforms.Linux, "Not supported on Linux.")]
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
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void Ctor_Socket_FromPipeHandle_Ctor_Dispose_Success(bool ownsHandle)
        {
            (int fd1, int fd2) = pipe2();
            close(fd2);

            using var _ = new Socket(new SafeSocketHandle(new IntPtr(fd1), ownsHandle));
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
            bool isRawPacket = (addressFamily == AddressFamily.Packet) &&
                               (socketType == SocketType.Raw);
            if (isRawPacket)
            {
                // protocol is the IEEE 802.3 protocol number in network byte order.
                const short ETH_P_ARP = 0x0806;
                protocolType = (ProtocolType)IPAddress.HostToNetworkOrder(ETH_P_ARP);
            }

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
                Assert.Null(copy.RemoteEndPoint);
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
            ProtocolType expectedProtocolType = protocolType;
            if (isRawPacket)
            {
                // raw packet doesn't support getting the protocol using getsockopt SO_PROTOCOL.
                expectedProtocolType = ProtocolType.Unspecified;
            }
            Assert.Equal(expectedProtocolType, copy.ProtocolType);

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
            Assert.Equal(orig.ProtocolType, client.ProtocolType);

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Ctor_SafeHandle_Listening_Success(bool shareSafeHandle)
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen();

            using var listenerCopy = new Socket(shareSafeHandle ? listener.SafeHandle : new SafeSocketHandle(listener.Handle, ownsHandle: false));
            Assert.False(listenerCopy.Connected);
            // This will throw if _isListening is set internally. (before reaching any real code)
            Assert.Throws<InvalidOperationException>(() => listenerCopy.Connect(new IPEndPoint(IPAddress.Loopback,0)));

            Assert.Equal(listener.AddressFamily, listenerCopy.AddressFamily);
            Assert.Equal(listener.Handle, listenerCopy.Handle);
            Assert.Equal(listener.IsBound, listenerCopy.IsBound);
            Assert.Equal(listener.LocalEndPoint, listenerCopy.LocalEndPoint);
            Assert.Equal(listener.ProtocolType, listenerCopy.ProtocolType);
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

        [DllImport("libc")]
        private static extern int socket(int domain, int type, int protocol);

        private const int PF_NETLINK = 16;

        private class NlEndPoint : EndPoint
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct sockaddr_nl
            {
                internal ushort sin_family;
                private ushort pad;
                internal int pid;
                private int nl_groups;
            }

            private readonly int _pid;

            public NlEndPoint(int pid)
            {
                _pid = pid;
            }

            public override AddressFamily AddressFamily
            {
                get
                {
                    return AddressFamily.Unknown;
                }
            }

            public class NlSocketAddress : SocketAddress
            {
                // We need to create base from something known.
                public unsafe NlSocketAddress(int pid) : base(AddressFamily.Packet)
                {
                    sockaddr_nl addr = default;

                    addr.sin_family = PF_NETLINK;
                    addr.pid = pid;

                    var bytes = new ReadOnlySpan<byte>(&addr, sizeof(sockaddr_nl));

                    for (int i = 0; i < bytes.Length; i++)
                    {
                        this[i] = bytes[i];
                    }
                }
            }

            public override SocketAddress Serialize()
            {
                SocketAddress a = (SocketAddress)new NlSocketAddress(_pid);
                return a;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct nlmsghdr
        {
            internal int nlmsg_len;       /* Length of message including header */
            internal ushort nlmsg_type;   /* Type of message content */
            internal ushort nlmsg_flags;  /* Additional flags */
            internal int nlmsg_seq;       /* Sequence number */
            internal uint nlmsg_pid;      /* Sender port ID */
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct nlmsgerr {
            internal int     error;
            internal nlmsghdr msg;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct rtmsg
        {
            internal byte rtm_family;
            internal byte rtm_dst_len;
            internal byte rtm_src_len;
            internal byte rtm_tos;

            internal byte rtm_table;
            internal byte rtm_protocol;
            internal byte rtm_scope;
            internal byte rtm_type;

            internal uint rtm_flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct nl_request
        {
            internal nlmsghdr nlh;
            internal rtmsg rtm;
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public unsafe void Ctor_SafeHandle_UnknownSocket_Success()
        {
            const int PF_INET = 2;
            const int NETLINK_ROUTE = 0;
            const int SOCK_DGRAM = 2;
            const int RTM_NEWROUTE = 24;
            const int RTM_GETROUTE = 26;
            const int NLM_F_REQUEST = 1;
            const int NLM_F_DUMP  = 0x300;
            const int NLMSG_ERROR = 2;
            const int SEQ = 42;

            int fd = socket(PF_NETLINK, SOCK_DGRAM, NETLINK_ROUTE);
            Assert.InRange(fd, 0, int.MaxValue);
            using (Socket netlink = new Socket(new SafeSocketHandle((IntPtr)fd, ownsHandle: true)))
            {
                Assert.Equal(AddressFamily.Unknown, netlink.AddressFamily);

                netlink.Bind(new NlEndPoint(Environment.ProcessId));

                nl_request req = default;
                req.nlh.nlmsg_pid = (uint)Environment.ProcessId;
                req.nlh.nlmsg_type = RTM_GETROUTE;  /* We wish to get routes */
                req.nlh.nlmsg_flags = NLM_F_REQUEST | NLM_F_DUMP;
                req.nlh.nlmsg_len = sizeof(nl_request);
                req.nlh.nlmsg_seq = SEQ;
                req.rtm.rtm_family = PF_INET;

                netlink.Send(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref req), sizeof(nl_request)));

                Assert.True(netlink.Poll(TestSettings.PassingTestTimeout, SelectMode.SelectRead));

                byte[] response = new byte[4000];
                int readBytes = netlink.Receive(response);
                // We should get at least header.
                Assert.True(readBytes > sizeof(nlmsghdr));

                MemoryMarshal.TryRead<nlmsghdr>(response.AsSpan(), out nlmsghdr nlh);
                Assert.Equal(SEQ, nlh.nlmsg_seq);

                if (nlh.nlmsg_type == NLMSG_ERROR)
                {
                    MemoryMarshal.TryRead<nlmsgerr>(response.AsSpan(sizeof(nlmsghdr)), out nlmsgerr err);
                    _output.WriteLine("Netlink request failed with {0}", err.error);
                }

                Assert.Equal(RTM_NEWROUTE, nlh.nlmsg_type);
            }
        }


        [DllImport("libc")]
        private static unsafe extern int socketpair(int domain, int type, int protocol, int* ptr);

        [DllImport("libc")]
        private static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        private static unsafe extern int pipe2(int* pipefd, int flags);

        private static unsafe (int, int) pipe2(int flags = 0)
        {
            Span<int> pipefd = stackalloc int[2];
            fixed (int* ptr = pipefd)
            {
                if (pipe2(ptr, flags) == 0)
                {
                    return (pipefd[0], pipefd[1]);
                }
                else
                {
                    throw new Win32Exception();
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public unsafe void Ctor_SafeHandle_SocketPair_Success()
        {
            // This is platform dependent but it seems like this is same on all supported platforms.
            const int AF_UNIX = 1;
            const int SOCK_STREAM = 1;
            Span<int> ptr = stackalloc int[2];

            fixed (int* bufferPtr = ptr)
            {
                int result = socketpair(AF_UNIX, SOCK_STREAM, 0, bufferPtr);
                Assert.Equal(0, result);
            }

            for (int i = 0; i <= 1; i++)
            {
                Assert.InRange(ptr[0], 0, int.MaxValue);
                Socket s = new Socket(new SafeSocketHandle((IntPtr)ptr[i], ownsHandle: false));

                Assert.True(s.Connected);
                Assert.Equal(AddressFamily.Unix, s.AddressFamily);
                Assert.Equal(SocketType.Stream, s.SocketType);
                Assert.Equal(ProtocolType.Unspecified, s.ProtocolType);
            }

            close(ptr[0]);
            close(ptr[1]);
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
