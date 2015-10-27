// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal class LinuxIPGlobalProperties : IPGlobalProperties
    {
        private static readonly string[] s_newLineSeparator = new string[] { Environment.NewLine }; // Used for string splitting

        public override string DhcpScopeName
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override string DomainName
        {
            get
            {
                return HostInformation.DomainName;
            }
        }

        public override string HostName
        {
            get
            {
                return HostInformation.HostName;
            }
        }

        public override bool IsWinsProxy
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override NetBiosNodeType NodeType
        {
            get
            {
                return NetBiosNodeType.Unknown;
            }
        }

        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            string tcp4FileContents = File.ReadAllText(NetworkFiles.Tcp4ConnectionsFile);
            string[] v4connections = tcp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            string tcp6FileContents = File.ReadAllText(NetworkFiles.Tcp6ConnectionsFile);
            string[] v6connections = tcp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            TcpConnectionInformation[] connections = new TcpConnectionInformation[v4connections.Length + v6connections.Length - 2]; // First line is header in each file
            int index = 0;

            // UDP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header
            {
                string line = v4connections[i];
                connections[index++] = ParseTcpConnectionInformationFromLine(line);
            }

            // UDP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header
            {
                string line = v6connections[i];
                connections[index++] = ParseTcpConnectionInformationFromLine(line);
            }

            return connections;
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            string tcp4FileContents = File.ReadAllText(NetworkFiles.Tcp4ConnectionsFile);
            string[] v4connections = tcp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            string tcp6FileContents = File.ReadAllText(NetworkFiles.Tcp6ConnectionsFile);
            string[] v6connections = tcp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            IPEndPoint[] endPoints = new IPEndPoint[v4connections.Length + v6connections.Length - 2]; // First line is header in each file
            int index = 0;

            // TCP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header
            {
                string line = v4connections[i];
                IPAddress remoteIPAddress;
                int remotePort;
                ParseRemoteConnectionInformation(line, out remoteIPAddress, out remotePort);

                endPoints[index++] = new IPEndPoint(remoteIPAddress, remotePort);
            }

            // TCP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header
            {
                string line = v6connections[i];
                IPAddress remoteIPAddress;
                int remotePort;
                ParseRemoteConnectionInformation(line, out remoteIPAddress, out remotePort);

                endPoints[index++] = new IPEndPoint(remoteIPAddress, remotePort);
            }

            return endPoints;
        }

        public override IPEndPoint[] GetActiveUdpListeners()
        {
            string udp4FileContents = File.ReadAllText(NetworkFiles.Udp4ConnectionsFile);
            string[] v4connections = udp4FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            string udp6FileContents = File.ReadAllText(NetworkFiles.Udp6ConnectionsFile);
            string[] v6connections = udp6FileContents.Split(s_newLineSeparator, StringSplitOptions.RemoveEmptyEntries);

            IPEndPoint[] endPoints = new IPEndPoint[v4connections.Length + v6connections.Length - 2]; // First line is header in each file
            int index = 0;

            // UDP Connections
            for (int i = 1; i < v4connections.Length; i++) // Skip first line header
            {
                string line = v4connections[i];
                IPAddress remoteIPAddress;
                int remotePort;
                ParseRemoteConnectionInformation(line, out remoteIPAddress, out remotePort);

                endPoints[index++] = new IPEndPoint(remoteIPAddress, remotePort);
            }

            // UDP6 Connections
            for (int i = 1; i < v6connections.Length; i++) // Skip first line header
            {
                string line = v6connections[i];
                IPAddress remoteIPAddress;
                int remotePort;
                ParseRemoteConnectionInformation(line, out remoteIPAddress, out remotePort);

                endPoints[index++] = new IPEndPoint(remoteIPAddress, remotePort);
            }

            return endPoints;
        }

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            return LinuxIcmpV4Statistics.CreateIcmpV4Statistics();
        }

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            return LinuxIcmpV6Statistics.CreateIcmpV6Statistics();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            return LinuxIPGlobalStatistics.CreateIPv4Statistics();
        }

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            return LinuxIPGlobalStatistics.CreateIPv6Statistics();
        }

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            return new LinuxTcpStatistics(ipv6:false);
        }

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            return new LinuxTcpStatistics(ipv6:true);
        }

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            return LinuxUdpStatistics.CreateUdpIPv4Statistics();
        }

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            return LinuxUdpStatistics.CreateUdpIPv6Statistics();
        }

        private UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            UnicastIPAddressInformationCollection collection = new UnicastIPAddressInformationCollection();
            foreach (UnicastIPAddressInformation info in
                LinuxNetworkInterface.GetLinuxNetworkInterfaces().SelectMany(lni => lni.GetIPProperties().UnicastAddresses))
            {
                // PERF: Use Interop.Sys.EnumerateInterfaceAddresses directly here.
                collection.InternalAdd(info);
            }

            return collection;
        }

        public override Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            return Task.Run((Func<UnicastIPAddressInformationCollection>)GetUnicastAddresses);
        }

        // Parsing logic for local and remote addresses and ports, as well as socket state.
        private static TcpConnectionInformation ParseTcpConnectionInformationFromLine(string line)
        {
            StringParser parser = new StringParser(line, ' ', true);
            parser.MoveNextOrFail(); // skip Index

            string localAddressAndPort = parser.MoveAndExtractNext(); // local_address
            IPAddress localAddress;
            int localPort;
            ParseAddressAndPort(localAddressAndPort, out localAddress, out localPort);

            string remoteAddressAndPort = parser.MoveAndExtractNext(); // rem_address
            IPAddress remoteAddress;
            int remotePort;
            ParseAddressAndPort(remoteAddressAndPort, out remoteAddress, out remotePort);

            string socketStateHex = parser.MoveAndExtractNext();
            int state;
            if (!int.TryParse(socketStateHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out state))
            {
                throw new InvalidOperationException("Invalid state value: " + socketStateHex);
            }
            TcpState tcpState = MapTcpState((Interop.LinuxTcpState)state);

            IPEndPoint localEndPoint = new IPEndPoint(localAddress, localPort);
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);

            return new SimpleTcpConnectionInformation(localEndPoint, remoteEndPoint, tcpState);
        }

        // Maps from Linux TCP states (include/net/tcp_states.h) to .NET TcpStates
        // TODO: Move this to the native shim.
        private static TcpState MapTcpState(Interop.LinuxTcpState state)
        {
            switch (state)
            {
                case Interop.LinuxTcpState.TCP_ESTABLISHED:
                    return TcpState.Established;
                case Interop.LinuxTcpState.TCP_SYN_SENT:
                    return TcpState.SynSent;
                case Interop.LinuxTcpState.TCP_SYN_RECV:
                    return TcpState.SynReceived;
                case Interop.LinuxTcpState.TCP_FIN_WAIT1:
                    return TcpState.FinWait1;
                case Interop.LinuxTcpState.TCP_FIN_WAIT2:
                    return TcpState.FinWait2;
                case Interop.LinuxTcpState.TCP_TIME_WAIT:
                    return TcpState.TimeWait;
                case Interop.LinuxTcpState.TCP_CLOSE:
                    return TcpState.Closing;
                case Interop.LinuxTcpState.TCP_CLOSE_WAIT:
                    return TcpState.CloseWait;
                case Interop.LinuxTcpState.TCP_LAST_ACK:
                    return TcpState.LastAck;
                case Interop.LinuxTcpState.TCP_LISTEN:
                    return TcpState.Listen;
                case Interop.LinuxTcpState.TCP_CLOSING:
                    return TcpState.Closing;
                case Interop.LinuxTcpState.TCP_NEW_SYN_RECV:
                    return TcpState.Unknown;
                case Interop.LinuxTcpState.TCP_MAX_STATES:
                    return TcpState.Unknown;
                default:
                    throw new InvalidOperationException("Invalid LinuxTcpState: " + state);
            }
        }

        // Common parsing logic for the remote connection information
        private static void ParseRemoteConnectionInformation(string line, out IPAddress remoteIPAddress, out int remotePort)
        {
            StringParser parser = new StringParser(line, ' ', true);
            parser.MoveNextOrFail(); // skip Index
            parser.MoveNextOrFail(); // skip local_address

            string remoteAddressAndPort = parser.MoveAndExtractNext();
            int indexOfColon = remoteAddressAndPort.IndexOf(':');
            if (indexOfColon == -1)
            {
                throw new InvalidOperationException("Parsing error. No colon in " + remoteAddressAndPort);
            }

            string remoteAddressString = remoteAddressAndPort.Substring(0, indexOfColon);
            remoteIPAddress = ParseHexIPAddress(remoteAddressString);

            string portString = remoteAddressAndPort.Substring(indexOfColon + 1, remoteAddressAndPort.Length - (indexOfColon + 1));
            if (!int.TryParse(portString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out remotePort))
            {
                throw new InvalidOperationException("Couldn't parse remote port number " + portString);
            }
        }

        private static void ParseAddressAndPort(string colonSeparatedAddress, out IPAddress ipAddress, out int port)
        {
            int indexOfColon = colonSeparatedAddress.IndexOf(':');
            if (indexOfColon == -1)
            {
                throw new InvalidOperationException("Parsing error. No colon in " + colonSeparatedAddress);
            }

            string remoteAddressString = colonSeparatedAddress.Substring(0, indexOfColon);
            ipAddress = ParseHexIPAddress(remoteAddressString);

            string portString = colonSeparatedAddress.Substring(indexOfColon + 1, colonSeparatedAddress.Length - (indexOfColon + 1));
            if (!int.TryParse(portString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out port))
            {
                throw new InvalidOperationException("Couldn't parse remote port number " + portString);
            }
        }

        private static IPAddress ParseHexIPAddress(string remoteAddressString)
        {
            if (remoteAddressString.Length <= 8) // IPv4 Address
            {
                return ParseIPv4HexString(remoteAddressString);
            }
            else if (remoteAddressString.Length == 32) // IPv6 Address
            {
                return ParseIPv6HexString(remoteAddressString);
            }
            else
            {
                throw new NetworkInformationException("Invalid address string: " + remoteAddressString);
            }
        }

        // Simply converst the hex string into a long and uses the IPAddress(long) constructor.
        // Strings passed to this method must be 8 or less characters in length (32-bit address).
        private static IPAddress ParseIPv4HexString(string hexAddress)
        {
            IPAddress ipAddress;
            long addressValue;
            if (!long.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addressValue))
            {
                throw new NetworkInformationException("Invalid address string: " + hexAddress);
            }
            ipAddress = new IPAddress(addressValue);
            return ipAddress;
        }

        // Parses a 128-bit IPv6 Address stored as a 32-character hex number.
        // Strings passed to this must be 32 characters in length.
        private static IPAddress ParseIPv6HexString(string hexAddress)
        {
            Debug.Assert(hexAddress.Length == 32);
            byte[] addressBytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                addressBytes[i] = (byte)(HexToByte(hexAddress[(i * 2)])
                                    + HexToByte(hexAddress[(i * 2) + 1]));
            }

            IPAddress ipAddress = new IPAddress(addressBytes);
            return ipAddress;
        }

        private static byte HexToByte(char val)
        {
            if (val <= '9' && val >= '0')
                return (byte)(val - '0');
            else if (val >= 'a' && val <= 'f')
                return (byte)((val - 'a') + 10);
            else if (val >= 'A' && val <= 'F')
                return (byte)((val - 'A') + 10);
            else
                throw new InvalidOperationException("Invalid hex character.");
        }
    }
}
