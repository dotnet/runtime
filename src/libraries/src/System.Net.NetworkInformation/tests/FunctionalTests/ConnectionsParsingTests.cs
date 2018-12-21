// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class ConnectionsParsingTests : FileCleanupTestBase
    {
        [Fact]
        public void NumSocketConnectionsParsing()
        {
            string sockstatFile = GetTestFilePath();
            string sockstat6File = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/sockstat", sockstatFile);
            FileUtil.NormalizeLineEndings("NetworkFiles/sockstat6", sockstat6File);

            int numTcp = StringParsingHelpers.ParseNumSocketConnections(sockstatFile, "TCP");
            Assert.Equal(4, numTcp);

            int numTcp6 = StringParsingHelpers.ParseNumSocketConnections(sockstat6File, "TCP6");
            Assert.Equal(6, numTcp6);

            int numUdp = StringParsingHelpers.ParseNumSocketConnections(sockstatFile, "UDP");
            Assert.Equal(12, numUdp);

            int numUdp6 = StringParsingHelpers.ParseNumSocketConnections(sockstat6File, "UDP6");
            Assert.Equal(3, numUdp6);
        }

        [Fact]
        public void ActiveTcpConnectionsParsing()
        {
            string tcpFile = GetTestFilePath();
            string tcp6File = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/tcp", tcpFile);
            FileUtil.NormalizeLineEndings("NetworkFiles/tcp6", tcp6File);

            TcpConnectionInformation[] infos = StringParsingHelpers.ParseActiveTcpConnectionsFromFiles(tcpFile, tcp6File);
            Assert.Equal(11, infos.Length);
            ValidateInfo(infos[0], new IPEndPoint(0xFFFFFF01L, 0x01BD), new IPEndPoint(0L, 0), TcpState.Established);
            ValidateInfo(infos[1], new IPEndPoint(0x12345678L, 0x008B), new IPEndPoint(0L, 0), TcpState.SynSent);
            ValidateInfo(infos[2], new IPEndPoint(0x0101007FL, 0x0035), new IPEndPoint(0L, 0), TcpState.SynReceived);
            ValidateInfo(infos[3], new IPEndPoint(0x0100007FL, 0x0277), new IPEndPoint(0L, 0), TcpState.FinWait1);
            ValidateInfo(infos[4], new IPEndPoint(0x0100007FL, 0x0277), new IPEndPoint(0x00000001L, 0), TcpState.SynReceived);

            ValidateInfo(
                infos[5],
                new IPEndPoint(IPAddress.Parse("::"), 0x01BD),
                new IPEndPoint(IPAddress.Parse("::"), 0x0000),
                TcpState.FinWait2);

            ValidateInfo(
                infos[6],
                new IPEndPoint(IPAddress.Parse("::"), 0x008B),
                new IPEndPoint(IPAddress.Parse("::"), 0x0000),
                TcpState.TimeWait);

            ValidateInfo(
                infos[7],
                new IPEndPoint(IPAddress.Parse("::1"), 0x0277),
                new IPEndPoint(IPAddress.Parse("::"), 0x0000),
                TcpState.Closing);

            ValidateInfo(
                infos[8],
                new IPEndPoint(IPAddress.Parse("::1"), 0xA696),
                new IPEndPoint(IPAddress.Parse("::1"), 0x0277),
                TcpState.CloseWait);

            ValidateInfo(
                infos[9],
                new IPEndPoint(IPAddress.Parse("::1"), 0xA69B),
                new IPEndPoint(IPAddress.Parse("::1"), 0x0277),
                TcpState.LastAck);

            ValidateInfo(
                infos[10],
                new IPEndPoint(IPAddress.Parse("::1"), 124),
                new IPEndPoint(IPAddress.Parse("fe80::215:5dff:fe00:402"), 123),
                TcpState.Established);
        }

        [Fact]
        public void TcpListenersParsing()
        {
            string tcpFile = GetTestFilePath();
            string tcp6File = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/tcp", tcpFile);
            FileUtil.NormalizeLineEndings("NetworkFiles/tcp6", tcp6File);

            IPEndPoint[] listeners = StringParsingHelpers.ParseActiveTcpListenersFromFiles(tcpFile, tcp6File);
            // There is only one socket in Listening state
            Assert.Equal(2, listeners.Length);
            Assert.Equal(new IPEndPoint(IPAddress.Parse("::1"), 0xA697), listeners[0]);
            Assert.Equal(new IPEndPoint(IPAddress.Parse("fec0::aa64:0:0:1"), 123), listeners[1]);
        }

        [Fact]
        public void UdpListenersParsing()
        {
            string udpFile = GetTestFilePath();
            string udp6File = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/udp", udpFile);
            FileUtil.NormalizeLineEndings("NetworkFiles/udp6", udp6File);

            IPEndPoint[] listeners = StringParsingHelpers.ParseActiveUdpListenersFromFiles(udpFile, udp6File);
            Assert.Equal(17, listeners.Length);

            Assert.Equal(listeners[0], new IPEndPoint(0x00000000, 0x8E15));
            Assert.Equal(listeners[1], new IPEndPoint(0x00000000, 0x14E9));
            Assert.Equal(listeners[2], new IPEndPoint(0x00000000, 0xB50F));
            Assert.Equal(listeners[3], new IPEndPoint(0x0101007F, 0x0035));
            Assert.Equal(listeners[4], new IPEndPoint(0x00000000, 0x0044));
            Assert.Equal(listeners[5], new IPEndPoint(0xFF83690A, 0x0089));
            Assert.Equal(listeners[6], new IPEndPoint(0x3B80690A, 0x0089));
            Assert.Equal(listeners[7], new IPEndPoint(0x00000000, 0x0089));
            Assert.Equal(listeners[8], new IPEndPoint(0xFF83690A, 0x008A));
            Assert.Equal(listeners[9], new IPEndPoint(0x3B80690A, 0x008A));
            Assert.Equal(listeners[10], new IPEndPoint(0x00000000, 0x008A));
            Assert.Equal(listeners[11], new IPEndPoint(0x00000000, 0x0277));

            Assert.Equal(listeners[12], new IPEndPoint(IPAddress.Parse("::"), 0x14E9));
            Assert.Equal(listeners[13], new IPEndPoint(IPAddress.Parse("::"), 0x96D3));
            Assert.Equal(listeners[14], new IPEndPoint(IPAddress.Parse("::"), 0x8B58));
            Assert.Equal(listeners[15], new IPEndPoint(IPAddress.Parse("fec0::aa64:0:0:1"), 123));
            Assert.Equal(listeners[16], new IPEndPoint(IPAddress.Parse("fe80::215:5dff:fe00:402"), 123));
        }

        private static void ValidateInfo(TcpConnectionInformation tcpConnectionInformation, IPEndPoint localEP, IPEndPoint remoteEP, TcpState state)
        {
            Assert.Equal(localEP, tcpConnectionInformation.LocalEndPoint);
            Assert.Equal(remoteEP, tcpConnectionInformation.RemoteEndPoint);
            Assert.Equal(state, tcpConnectionInformation.State);
        }
    }
}
