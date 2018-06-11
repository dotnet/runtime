// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void Ctor_Raw_Success(AddressFamily addressFamily, ProtocolType protocolType)
        {
           using (new Socket(addressFamily, SocketType.Raw, protocolType))
           {
           }
        }
    }
}
