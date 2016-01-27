// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// A storage structure for the information parsed out of the
    /// /proc/net/snmp file, related to ICMPv4 protocol statistics.
    /// </summary>
    internal struct Icmpv4StatisticsTable
    {
        public int InMsgs;
        public int InErrors;
        public int InCsumErrors;
        public int InDestUnreachs;
        public int InTimeExcds;
        public int InParmProbs;
        public int InSrcQuenchs;
        public int InRedirects;
        public int InEchos;
        public int InEchoReps;
        public int InTimestamps;
        public int InTimeStampReps;
        public int InAddrMasks;
        public int InAddrMaskReps;
        public int OutMsgs;
        public int OutErrors;
        public int OutDestUnreachs;
        public int OutTimeExcds;
        public int OutParmProbs;
        public int OutSrcQuenchs;
        public int OutRedirects;
        public int OutEchos;
        public int OutEchoReps;
        public int OutTimestamps;
        public int OutTimestampReps;
        public int OutAddrMasks;
        public int OutAddrMaskReps;
    }

    /// <summary>
    /// A storage structure for the information parsed out of the
    /// /proc/net/snmp6 file, related to ICMPv6 protocol statistics.
    /// </summary>
    internal struct Icmpv6StatisticsTable
    {
        public int InDestUnreachs;
        public int OutDestUnreachs;
        public int InEchoReplies;
        public int InEchos;
        public int OutEchoReplies;
        public int OutEchos;
        public int InErrors;
        public int OutErrors;
        public int InGroupMembQueries;
        public int OutInGroupMembQueries;
        public int InGroupMembReductions;
        public int OutGroupMembReductions;
        public int InGroupMembResponses;
        public int OutGroupMembResponses;
        public int InMsgs;
        public int OutMsgs;
        public int InNeighborAdvertisements;
        public int OutNeighborAdvertisements;
        public int InNeighborSolicits;
        public int OutNeighborSolicits;
        public int InPktTooBigs;
        public int OutPktTooBigs;
        public int InParmProblems;
        public int OutParmProblems;
        public int InRedirects;
        public int OutRedirects;
        public int InRouterSolicits;
        public int OutRouterSolicits;
        public int InRouterAdvertisements;
        public int OutRouterAdvertisements;
        public int InTimeExcds;
        public int OutTimeExcds;
    }

    internal struct TcpGlobalStatisticsTable
    {
        public int RtoAlgorithm;
        public int RtoMin;
        public int RtoMax;
        public int MaxConn;
        public int ActiveOpens;
        public int PassiveOpens;
        public int AttemptFails;
        public int EstabResets;
        public int CurrEstab;
        public int InSegs;
        public int OutSegs;
        public int RetransSegs;
        public int InErrs;
        public int OutRsts;
        public int InCsumErrors;
    }

    internal struct UdpGlobalStatisticsTable
    {
        public int InDatagrams;
        public int NoPorts;
        public int InErrors;
        public int OutDatagrams;
        public int RcvbufErrors;
        public int SndbufErrors;
        public int InCsumErrors;
    }

    /// <summary>
    /// Storage structure for IP Global statistics from /proc/net/snmp
    /// </summary>
    internal struct IPGlobalStatisticsTable
    {
        // Information exposed in the snmp (ipv4) and snmp6 (ipv6) files under /proc/net
        // Each piece corresponds to a data item defined in the MIB-II specification:
        // http://www.ietf.org/rfc/rfc1213.txt
        // Each field's comment indicates the name as it appears in the snmp (snmp6) file.
        // In the snmp6 file, each name is prefixed with "IP6".

        public bool Forwarding; // Forwarding
        public int DefaultTtl; // DefaultTTL

        public int InReceives; // InReceives
        public int InHeaderErrors; // InHdrErrors
        public int InAddressErrors; // InAddrErrors
        public int ForwardedDatagrams; // ForwDatagrams (IP6OutForwDatagrams)
        public int InUnknownProtocols; // InUnknownProtos
        public int InDiscards; // InDiscards
        public int InDelivers; // InDelivers

        public int OutRequests; // OutRequestscat 
        public int OutDiscards; // OutDiscards
        public int OutNoRoutes; // OutNoRoutes

        public int ReassemblyTimeout; // ReasmTimeout
        public int ReassemblyRequireds; // ReasmReqds
        public int ReassemblyOKs; // ReasmOKs
        public int ReassemblyFails; // ReasmFails

        public int FragmentOKs; // FragOKs
        public int FragmentFails; // FragFails
        public int FragmentCreates; // FragCreates
    }

    internal struct IPInterfaceStatisticsTable
    {
        // Receive section
        public uint BytesReceived;
        public uint PacketsReceived;
        public uint ErrorsReceived;
        public uint IncomingPacketsDropped;
        public uint FifoBufferErrorsReceived;
        public uint PacketFramingErrorsReceived;
        public uint CompressedPacketsReceived;
        public uint MulticastFramesReceived;

        // Transmit section
        public uint BytesTransmitted;
        public uint PacketsTransmitted;
        public uint ErrorsTransmitted;
        public uint OutgoingPacketsDropped;
        public uint FifoBufferErrorsTransmitted;
        public uint CollisionsDetected;
        public uint CarrierLosses;
        public uint CompressedPacketsTransmitted;
    }

    internal static partial class StringParsingHelpers
    {
        // Parses ICMP v4 statistics from /proc/net/snmp
        public static Icmpv4StatisticsTable ParseIcmpv4FromSnmpFile(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            int firstIpHeader = fileContents.IndexOf("Icmp:", StringComparison.Ordinal);
            int secondIpHeader = fileContents.IndexOf("Icmp:", firstIpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondIpHeader, StringComparison.Ordinal);
            string icmpData = fileContents.Substring(secondIpHeader, endOfSecondLine - secondIpHeader);
            StringParser parser = new StringParser(icmpData, ' ');

            parser.MoveNextOrFail(); // Skip Icmp:

            return new Icmpv4StatisticsTable()
            {
                InMsgs = parser.ParseNextInt32(),
                InErrors = parser.ParseNextInt32(),
                InCsumErrors = parser.ParseNextInt32(),
                InDestUnreachs = parser.ParseNextInt32(),
                InTimeExcds = parser.ParseNextInt32(),
                InParmProbs = parser.ParseNextInt32(),
                InSrcQuenchs = parser.ParseNextInt32(),
                InRedirects = parser.ParseNextInt32(),
                InEchos = parser.ParseNextInt32(),
                InEchoReps = parser.ParseNextInt32(),
                InTimestamps = parser.ParseNextInt32(),
                InTimeStampReps = parser.ParseNextInt32(),
                InAddrMasks = parser.ParseNextInt32(),
                InAddrMaskReps = parser.ParseNextInt32(),
                OutMsgs = parser.ParseNextInt32(),
                OutErrors = parser.ParseNextInt32(),
                OutDestUnreachs = parser.ParseNextInt32(),
                OutTimeExcds = parser.ParseNextInt32(),
                OutParmProbs = parser.ParseNextInt32(),
                OutSrcQuenchs = parser.ParseNextInt32(),
                OutRedirects = parser.ParseNextInt32(),
                OutEchos = parser.ParseNextInt32(),
                OutEchoReps = parser.ParseNextInt32(),
                OutTimestamps = parser.ParseNextInt32(),
                OutTimestampReps = parser.ParseNextInt32(),
                OutAddrMasks = parser.ParseNextInt32(),
                OutAddrMaskReps = parser.ParseNextInt32()
            };
        }

        public static Icmpv6StatisticsTable ParseIcmpv6FromSnmp6File(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);

            return new Icmpv6StatisticsTable()
            {
                InMsgs = reader.GetNextValueAsInt32("Icmp6InMsgs"),
                InErrors = reader.GetNextValueAsInt32("Icmp6InErrors"),
                OutMsgs = reader.GetNextValueAsInt32("Icmp6OutMsgs"),
                OutErrors = reader.GetNextValueAsInt32("Icmp6OutErrors"),
                InDestUnreachs = reader.GetNextValueAsInt32("Icmp6InDestUnreachs"),
                InPktTooBigs = reader.GetNextValueAsInt32("Icmp6InPktTooBigs"),
                InTimeExcds = reader.GetNextValueAsInt32("Icmp6InTimeExcds"),
                InParmProblems = reader.GetNextValueAsInt32("Icmp6InParmProblems"),
                InEchos = reader.GetNextValueAsInt32("Icmp6InEchos"),
                InEchoReplies = reader.GetNextValueAsInt32("Icmp6InEchoReplies"),
                InGroupMembQueries = reader.GetNextValueAsInt32("Icmp6InGroupMembQueries"),
                InGroupMembResponses = reader.GetNextValueAsInt32("Icmp6InGroupMembResponses"),
                InGroupMembReductions = reader.GetNextValueAsInt32("Icmp6InGroupMembReductions"),
                InRouterSolicits = reader.GetNextValueAsInt32("Icmp6InRouterSolicits"),
                InRouterAdvertisements = reader.GetNextValueAsInt32("Icmp6InRouterAdvertisements"),
                InNeighborSolicits = reader.GetNextValueAsInt32("Icmp6InNeighborSolicits"),
                InNeighborAdvertisements = reader.GetNextValueAsInt32("Icmp6InNeighborAdvertisements"),
                InRedirects = reader.GetNextValueAsInt32("Icmp6InRedirects"),
                OutDestUnreachs = reader.GetNextValueAsInt32("Icmp6OutDestUnreachs"),
                OutPktTooBigs = reader.GetNextValueAsInt32("Icmp6OutPktTooBigs"),
                OutTimeExcds = reader.GetNextValueAsInt32("Icmp6OutTimeExcds"),
                OutParmProblems = reader.GetNextValueAsInt32("Icmp6OutParmProblems"),
                OutEchos = reader.GetNextValueAsInt32("Icmp6OutEchos"),
                OutEchoReplies = reader.GetNextValueAsInt32("Icmp6OutEchoReplies"),
                OutInGroupMembQueries = reader.GetNextValueAsInt32("Icmp6OutGroupMembQueries"),
                OutGroupMembResponses = reader.GetNextValueAsInt32("Icmp6OutGroupMembResponses"),
                OutGroupMembReductions = reader.GetNextValueAsInt32("Icmp6OutGroupMembReductions"),
                OutRouterSolicits = reader.GetNextValueAsInt32("Icmp6OutRouterSolicits"),
                OutRouterAdvertisements = reader.GetNextValueAsInt32("Icmp6OutRouterAdvertisements"),
                OutNeighborSolicits = reader.GetNextValueAsInt32("Icmp6OutNeighborSolicits"),
                OutNeighborAdvertisements = reader.GetNextValueAsInt32("Icmp6OutNeighborAdvertisements"),
                OutRedirects = reader.GetNextValueAsInt32("Icmp6OutRedirects")
            };
        }

        public static IPGlobalStatisticsTable ParseIPv4GlobalStatisticsFromSnmpFile(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);

            int firstIpHeader = fileContents.IndexOf("Ip:", StringComparison.Ordinal);
            int secondIpHeader = fileContents.IndexOf("Ip:", firstIpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondIpHeader, StringComparison.Ordinal);
            string ipData = fileContents.Substring(secondIpHeader, endOfSecondLine - secondIpHeader);
            StringParser parser = new StringParser(ipData, ' ');

            parser.MoveNextOrFail(); // Skip Ip:

            // According to RFC 1213, "1" indicates "acting as a gateway". "2" indicates "NOT acting as a gateway".
            return new IPGlobalStatisticsTable()
            {
                Forwarding = parser.MoveAndExtractNext() == "1",
                DefaultTtl = parser.ParseNextInt32(),
                InReceives = parser.ParseNextInt32(),
                InHeaderErrors = parser.ParseNextInt32(),
                InAddressErrors = parser.ParseNextInt32(),
                ForwardedDatagrams = parser.ParseNextInt32(),
                InUnknownProtocols = parser.ParseNextInt32(),
                InDiscards = parser.ParseNextInt32(),
                InDelivers = parser.ParseNextInt32(),
                OutRequests = parser.ParseNextInt32(),
                OutDiscards = parser.ParseNextInt32(),
                OutNoRoutes = parser.ParseNextInt32(),
                ReassemblyTimeout = parser.ParseNextInt32(),
                ReassemblyRequireds = parser.ParseNextInt32(),
                ReassemblyOKs = parser.ParseNextInt32(),
                ReassemblyFails = parser.ParseNextInt32(),
                FragmentOKs = parser.ParseNextInt32(),
                FragmentFails = parser.ParseNextInt32(),
                FragmentCreates = parser.ParseNextInt32(),
            };
        }

        internal static IPGlobalStatisticsTable ParseIPv6GlobalStatisticsFromSnmp6File(string filePath)
        {
            // Read the remainder of statistics from snmp6.
            string fileContents = File.ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);

            return new IPGlobalStatisticsTable()
            {
                InReceives = reader.GetNextValueAsInt32("Ip6InReceives"),
                InHeaderErrors = reader.GetNextValueAsInt32("Ip6InHdrErrors"),
                InAddressErrors = reader.GetNextValueAsInt32("Ip6InAddrErrors"),
                InUnknownProtocols = reader.GetNextValueAsInt32("Ip6InUnknownProtos"),
                InDiscards = reader.GetNextValueAsInt32("Ip6InDiscards"),
                InDelivers = reader.GetNextValueAsInt32("Ip6InDelivers"),
                ForwardedDatagrams = reader.GetNextValueAsInt32("Ip6OutForwDatagrams"),
                OutRequests = reader.GetNextValueAsInt32("Ip6OutRequests"),
                OutDiscards = reader.GetNextValueAsInt32("Ip6OutDiscards"),
                OutNoRoutes = reader.GetNextValueAsInt32("Ip6OutNoRoutes"),
                ReassemblyTimeout = reader.GetNextValueAsInt32("Ip6ReasmTimeout"),
                ReassemblyRequireds = reader.GetNextValueAsInt32("Ip6ReasmReqds"),
                ReassemblyOKs = reader.GetNextValueAsInt32("Ip6ReasmOKs"),
                ReassemblyFails = reader.GetNextValueAsInt32("Ip6ReasmFails"),
                FragmentOKs = reader.GetNextValueAsInt32("Ip6FragOKs"),
                FragmentFails = reader.GetNextValueAsInt32("Ip6FragFails"),
                FragmentCreates = reader.GetNextValueAsInt32("Ip6FragCreates"),
            };
        }

        internal static TcpGlobalStatisticsTable ParseTcpGlobalStatisticsFromSnmpFile(string filePath)
        {
            // NOTE: There is no information in the snmp6 file regarding TCP statistics,
            // so the statistics are always pulled from /proc/net/snmp.
            string fileContents = File.ReadAllText(filePath);
            int firstTcpHeader = fileContents.IndexOf("Tcp:", StringComparison.Ordinal);
            int secondTcpHeader = fileContents.IndexOf("Tcp:", firstTcpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondTcpHeader, StringComparison.Ordinal);
            string tcpData = fileContents.Substring(secondTcpHeader, endOfSecondLine - secondTcpHeader);
            StringParser parser = new StringParser(tcpData, ' ');

            parser.MoveNextOrFail(); // Skip Tcp:

            return new TcpGlobalStatisticsTable()
            {
                RtoAlgorithm = parser.ParseNextInt32(),
                RtoMin = parser.ParseNextInt32(),
                RtoMax = parser.ParseNextInt32(),
                MaxConn = parser.ParseNextInt32(),
                ActiveOpens = parser.ParseNextInt32(),
                PassiveOpens = parser.ParseNextInt32(),
                AttemptFails = parser.ParseNextInt32(),
                EstabResets = parser.ParseNextInt32(),
                CurrEstab = parser.ParseNextInt32(),
                InSegs = parser.ParseNextInt32(),
                OutSegs = parser.ParseNextInt32(),
                RetransSegs = parser.ParseNextInt32(),
                InErrs = parser.ParseNextInt32(),
                OutRsts = parser.ParseNextInt32(),
                InCsumErrors = parser.ParseNextInt32()
            };
        }

        internal static UdpGlobalStatisticsTable ParseUdpv4GlobalStatisticsFromSnmpFile(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            int firstUdpHeader = fileContents.IndexOf("Udp:", StringComparison.Ordinal);
            int secondUdpHeader = fileContents.IndexOf("Udp:", firstUdpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondUdpHeader, StringComparison.Ordinal);
            string tcpData = fileContents.Substring(secondUdpHeader, endOfSecondLine - secondUdpHeader);
            StringParser parser = new StringParser(tcpData, ' ');

            parser.MoveNextOrFail(); // Skip Udp:

            return new UdpGlobalStatisticsTable()
            {
                InDatagrams = parser.ParseNextInt32(),
                NoPorts = parser.ParseNextInt32(),
                InErrors = parser.ParseNextInt32(),
                OutDatagrams = parser.ParseNextInt32(),
                RcvbufErrors = parser.ParseNextInt32(),
                SndbufErrors = parser.ParseNextInt32(),
                InCsumErrors = parser.ParseNextInt32()
            };
        }

        internal static UdpGlobalStatisticsTable ParseUdpv6GlobalStatisticsFromSnmp6File(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);

            return new UdpGlobalStatisticsTable()
            {
                InDatagrams = reader.GetNextValueAsInt32("Udp6InDatagrams"),
                NoPorts = reader.GetNextValueAsInt32("Udp6NoPorts"),
                InErrors = reader.GetNextValueAsInt32("Udp6InErrors"),
                OutDatagrams = reader.GetNextValueAsInt32("Udp6OutDatagrams"),
                RcvbufErrors = reader.GetNextValueAsInt32("Udp6RcvbufErrors"),
                SndbufErrors = reader.GetNextValueAsInt32("Udp6SndbufErrors"),
                InCsumErrors = reader.GetNextValueAsInt32("Udp6InCsumErrors"),
            };
        }

        internal static IPInterfaceStatisticsTable ParseInterfaceStatisticsTableFromFile(string filePath, string name)
        {
            using (StreamReader sr = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false)))
            {
                sr.ReadLine();
                sr.ReadLine();
                int index = 0;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line.Contains(name))
                    {
                        string[] pieces = line.Split(new char[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);

                        return new IPInterfaceStatisticsTable()
                        {
                            BytesReceived = ParseUInt64AndClampToUInt32(pieces[1]),
                            PacketsReceived = ParseUInt64AndClampToUInt32(pieces[2]),
                            ErrorsReceived = ParseUInt64AndClampToUInt32(pieces[3]),
                            IncomingPacketsDropped = ParseUInt64AndClampToUInt32(pieces[4]),
                            FifoBufferErrorsReceived = ParseUInt64AndClampToUInt32(pieces[5]),
                            PacketFramingErrorsReceived = ParseUInt64AndClampToUInt32(pieces[6]),
                            CompressedPacketsReceived = ParseUInt64AndClampToUInt32(pieces[7]),
                            MulticastFramesReceived = ParseUInt64AndClampToUInt32(pieces[8]),

                            BytesTransmitted = ParseUInt64AndClampToUInt32(pieces[9]),
                            PacketsTransmitted = ParseUInt64AndClampToUInt32(pieces[10]),
                            ErrorsTransmitted = ParseUInt64AndClampToUInt32(pieces[11]),
                            OutgoingPacketsDropped = ParseUInt64AndClampToUInt32(pieces[12]),
                            FifoBufferErrorsTransmitted = ParseUInt64AndClampToUInt32(pieces[13]),
                            CollisionsDetected = ParseUInt64AndClampToUInt32(pieces[14]),
                            CarrierLosses = ParseUInt64AndClampToUInt32(pieces[15]),
                            CompressedPacketsTransmitted = ParseUInt64AndClampToUInt32(pieces[16]),
                        };
                    }
                    index += 1;
                }

                throw ExceptionHelper.CreateForParseFailure();
            }
        }

        private static uint ParseUInt64AndClampToUInt32(string value)
        {
            return (uint)Math.Min(uint.MaxValue, ulong.Parse(value));
        }
    }
}
