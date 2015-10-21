// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxUdpStatistics : UdpStatistics
    {
        private int _inDatagrams;
        private int _noPorts;
        private int _inErrors;
        private int _outDatagrams;
        private int _rcvbufErrors;
        private int _sndbufErrors;
        private int _inCsumErrors;

        private int _udpListeners;

        private LinuxUdpStatistics() { }

        public static UdpStatistics CreateUdpIPv4Statistics()
        {
            LinuxUdpStatistics stats = new LinuxUdpStatistics();
            string fileContents = File.ReadAllText(NetworkFiles.SnmpV4StatsFile);
            int firstUdpHeader = fileContents.IndexOf("Udp:");
            int secondUdpHeader = fileContents.IndexOf("Udp:", firstUdpHeader + 1);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondUdpHeader);
            string tcpData = fileContents.Substring(secondUdpHeader, endOfSecondLine - secondUdpHeader);
            StringParser parser = new StringParser(tcpData, ' ');

            // NOTE: Need to verify that this order is consistent. Otherwise, we need to parse the first-line header
            // to determine the order of information contained in the file.

            parser.MoveNextOrFail(); // Skip Udp:

            stats._inDatagrams = parser.ParseNextInt32();
            stats._noPorts = parser.ParseNextInt32();
            stats._inErrors = parser.ParseNextInt32();
            stats._outDatagrams = parser.ParseNextInt32();
            stats._rcvbufErrors = parser.ParseNextInt32();
            stats._sndbufErrors = parser.ParseNextInt32();
            stats._inCsumErrors = parser.ParseNextInt32();

            // Parse the number of active connections out of /proc/net/sockstat
            string sockstatFile = File.ReadAllText(NetworkFiles.SockstatFile);
            int indexOfUdp = sockstatFile.IndexOf("UDP:");
            int endOfUdpLine = sockstatFile.IndexOf(Environment.NewLine, indexOfUdp + 1);
            string udpLineData = sockstatFile.Substring(indexOfUdp, endOfUdpLine - indexOfUdp);
            StringParser sockstatParser = new StringParser(udpLineData, ' ');
            sockstatParser.MoveNextOrFail(); // Skip "UDP:"
            sockstatParser.MoveNextOrFail(); // Skip: "inuse"
            stats._udpListeners = sockstatParser.ParseNextInt32();

            return stats;
        }

        public static UdpStatistics CreateUdpIPv6Statistics()
        {
            LinuxUdpStatistics stats = new LinuxUdpStatistics();

            string fileContents = File.ReadAllText(NetworkFiles.SnmpV6StatsFile);

            RowConfigReader reader = new RowConfigReader(fileContents);

            stats._inDatagrams = reader.GetNextValueAsInt32("Udp6InDatagrams");
            stats._noPorts = reader.GetNextValueAsInt32("Udp6NoPorts");
            stats._inErrors = reader.GetNextValueAsInt32("Udp6InErrors");
            stats._outDatagrams = reader.GetNextValueAsInt32("Udp6OutDatagrams");
            stats._rcvbufErrors = reader.GetNextValueAsInt32("Udp6RcvbufErrors");
            stats._sndbufErrors = reader.GetNextValueAsInt32("Udp6SndbufErrors");
            stats._inCsumErrors = reader.GetNextValueAsInt32("Udp6InCsumErrors");

            // Parse the number of active connections out of /proc/net/sockstat6
            string sockstatFile = File.ReadAllText(NetworkFiles.Sockstat6File);
            int indexOfUdp = sockstatFile.IndexOf("UDP6:");
            int endOfUdpLine = sockstatFile.IndexOf(Environment.NewLine, indexOfUdp + 1);
            string udpLineData = sockstatFile.Substring(indexOfUdp, endOfUdpLine - indexOfUdp);
            StringParser sockstatParser = new StringParser(udpLineData, ' ');
            sockstatParser.MoveNextOrFail(); // Skip "UDP6:"
            sockstatParser.MoveNextOrFail(); // Skip: "inuse"
            stats._udpListeners = sockstatParser.ParseNextInt32();

            return stats;
        }

        public override long DatagramsReceived { get { return _inDatagrams; } }
        public override long DatagramsSent { get { return _outDatagrams; } }
        public override long IncomingDatagramsDiscarded { get { throw new PlatformNotSupportedException(); } }
        public override long IncomingDatagramsWithErrors { get { return _inErrors; } }
        public override int UdpListeners { get { return _udpListeners; } }
    }
}
