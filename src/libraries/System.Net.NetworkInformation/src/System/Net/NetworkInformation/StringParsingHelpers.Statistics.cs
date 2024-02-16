// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// A storage structure for the information parsed out of the
    /// /proc/net/snmp file, related to ICMPv4 protocol statistics.
    /// </summary>
    internal struct Icmpv4StatisticsTable
    {
        public long InMsgs;
        public long InErrors;
        public long InCsumErrors;
        public long InDestUnreachs;
        public long InTimeExcds;
        public long InParmProbs;
        public long InSrcQuenchs;
        public long InRedirects;
        public long InEchos;
        public long InEchoReps;
        public long InTimestamps;
        public long InTimeStampReps;
        public long InAddrMasks;
        public long InAddrMaskReps;
        public long OutMsgs;
        public long OutErrors;
        public long OutDestUnreachs;
        public long OutTimeExcds;
        public long OutParmProbs;
        public long OutSrcQuenchs;
        public long OutRedirects;
        public long OutEchos;
        public long OutEchoReps;
        public long OutTimestamps;
        public long OutTimestampReps;
        public long OutAddrMasks;
        public long OutAddrMaskReps;
    }

    /// <summary>
    /// A storage structure for the information parsed out of the
    /// /proc/net/snmp6 file, related to ICMPv6 protocol statistics.
    /// </summary>
    internal struct Icmpv6StatisticsTable
    {
        public long InDestUnreachs;
        public long OutDestUnreachs;
        public long InEchoReplies;
        public long InEchos;
        public long OutEchoReplies;
        public long OutEchos;
        public long InErrors;
        public long OutErrors;
        public long InGroupMembQueries;
        public long OutInGroupMembQueries;
        public long InGroupMembReductions;
        public long OutGroupMembReductions;
        public long InGroupMembResponses;
        public long OutGroupMembResponses;
        public long InMsgs;
        public long OutMsgs;
        public long InNeighborAdvertisements;
        public long OutNeighborAdvertisements;
        public long InNeighborSolicits;
        public long OutNeighborSolicits;
        public long InPktTooBigs;
        public long OutPktTooBigs;
        public long InParmProblems;
        public long OutParmProblems;
        public long InRedirects;
        public long OutRedirects;
        public long InRouterSolicits;
        public long OutRouterSolicits;
        public long InRouterAdvertisements;
        public long OutRouterAdvertisements;
        public long InTimeExcds;
        public long OutTimeExcds;
    }

    internal struct TcpGlobalStatisticsTable
    {
        public long RtoAlgorithm;
        public long RtoMin;
        public long RtoMax;
        public long MaxConn;
        public long ActiveOpens;
        public long PassiveOpens;
        public long AttemptFails;
        public long EstabResets;
        public long CurrEstab;
        public long InSegs;
        public long OutSegs;
        public long RetransSegs;
        public long InErrs;
        public long OutRsts;
        public long InCsumErrors;
    }

    internal struct UdpGlobalStatisticsTable
    {
        public long InDatagrams;
        public long NoPorts;
        public long InErrors;
        public long OutDatagrams;
        public long RcvbufErrors;
        public long SndbufErrors;
        public long InCsumErrors;
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

        public long InReceives; // InReceives
        public long InHeaderErrors; // InHdrErrors
        public long InAddressErrors; // InAddrErrors
        public long ForwardedDatagrams; // ForwDatagrams (IP6OutForwDatagrams)
        public long InUnknownProtocols; // InUnknownProtos
        public long InDiscards; // InDiscards
        public long InDelivers; // InDelivers

        public long OutRequests; // OutRequestscat
        public long OutDiscards; // OutDiscards
        public long OutNoRoutes; // OutNoRoutes

        public long ReassemblyTimeout; // ReasmTimeout
        public long ReassemblyRequireds; // ReasmReqds
        public long ReassemblyOKs; // ReasmOKs
        public long ReassemblyFails; // ReasmFails

        public long FragmentOKs; // FragOKs
        public long FragmentFails; // FragFails
        public long FragmentCreates; // FragCreates
    }

    internal struct IPInterfaceStatisticsTable
    {
        // Receive section
        public long BytesReceived;
        public long PacketsReceived;
        public long ErrorsReceived;
        public long IncomingPacketsDropped;
        public long FifoBufferErrorsReceived;
        public long PacketFramingErrorsReceived;
        public long CompressedPacketsReceived;
        public long MulticastFramesReceived;

        // Transmit section
        public long BytesTransmitted;
        public long PacketsTransmitted;
        public long ErrorsTransmitted;
        public long OutgoingPacketsDropped;
        public long FifoBufferErrorsTransmitted;
        public long CollisionsDetected;
        public long CarrierLosses;
        public long CompressedPacketsTransmitted;
    }

    internal static partial class StringParsingHelpers
    {
        // Parses ICMP v4 statistics from /proc/net/snmp
        public static Icmpv4StatisticsTable ParseIcmpv4FromSnmpFile(string filePath)
        {
            string fileContents = ReadAllText(filePath);
            int firstIpHeader = fileContents.IndexOf("Icmp:", StringComparison.Ordinal);
            int secondIpHeader = fileContents.IndexOf("Icmp:", firstIpHeader + 1, StringComparison.Ordinal);
            int inCsumErrorsIdx = fileContents.IndexOf("InCsumErrors", firstIpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondIpHeader, StringComparison.Ordinal);
            string icmpData = fileContents.Substring(secondIpHeader, endOfSecondLine - secondIpHeader);
            StringParser parser = new StringParser(icmpData, ' ');

            parser.MoveNextOrFail(); // Skip Icmp:

            return new Icmpv4StatisticsTable()
            {
                InMsgs = parser.ParseNextInt64(),
                InErrors = parser.ParseNextInt64(),
                InCsumErrors = inCsumErrorsIdx == -1 ? 0 : parser.ParseNextInt64(),
                InDestUnreachs = parser.ParseNextInt64(),
                InTimeExcds = parser.ParseNextInt64(),
                InParmProbs = parser.ParseNextInt64(),
                InSrcQuenchs = parser.ParseNextInt64(),
                InRedirects = parser.ParseNextInt64(),
                InEchos = parser.ParseNextInt64(),
                InEchoReps = parser.ParseNextInt64(),
                InTimestamps = parser.ParseNextInt64(),
                InTimeStampReps = parser.ParseNextInt64(),
                InAddrMasks = parser.ParseNextInt64(),
                InAddrMaskReps = parser.ParseNextInt64(),
                OutMsgs = parser.ParseNextInt64(),
                OutErrors = parser.ParseNextInt64(),
                OutDestUnreachs = parser.ParseNextInt64(),
                OutTimeExcds = parser.ParseNextInt64(),
                OutParmProbs = parser.ParseNextInt64(),
                OutSrcQuenchs = parser.ParseNextInt64(),
                OutRedirects = parser.ParseNextInt64(),
                OutEchos = parser.ParseNextInt64(),
                OutEchoReps = parser.ParseNextInt64(),
                OutTimestamps = parser.ParseNextInt64(),
                OutTimestampReps = parser.ParseNextInt64(),
                OutAddrMasks = parser.ParseNextInt64(),
                OutAddrMaskReps = parser.ParseNextInt64()
            };
        }

        public static Icmpv6StatisticsTable ParseIcmpv6FromSnmp6File(string filePath)
        {
            string fileContents = ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);
            bool hasIcmp6OutErrors = fileContents.Contains("Icmp6OutErrors");

            return new Icmpv6StatisticsTable()
            {
                InMsgs = reader.GetNextValueAsInt64("Icmp6InMsgs"),
                InErrors = reader.GetNextValueAsInt64("Icmp6InErrors"),
                OutMsgs = reader.GetNextValueAsInt64("Icmp6OutMsgs"),
                OutErrors = hasIcmp6OutErrors ? reader.GetNextValueAsInt64("Icmp6OutErrors") : 0,
                InDestUnreachs = reader.GetNextValueAsInt64("Icmp6InDestUnreachs"),
                InPktTooBigs = reader.GetNextValueAsInt64("Icmp6InPktTooBigs"),
                InTimeExcds = reader.GetNextValueAsInt64("Icmp6InTimeExcds"),
                InParmProblems = reader.GetNextValueAsInt64("Icmp6InParmProblems"),
                InEchos = reader.GetNextValueAsInt64("Icmp6InEchos"),
                InEchoReplies = reader.GetNextValueAsInt64("Icmp6InEchoReplies"),
                InGroupMembQueries = reader.GetNextValueAsInt64("Icmp6InGroupMembQueries"),
                InGroupMembResponses = reader.GetNextValueAsInt64("Icmp6InGroupMembResponses"),
                InGroupMembReductions = reader.GetNextValueAsInt64("Icmp6InGroupMembReductions"),
                InRouterSolicits = reader.GetNextValueAsInt64("Icmp6InRouterSolicits"),
                InRouterAdvertisements = reader.GetNextValueAsInt64("Icmp6InRouterAdvertisements"),
                InNeighborSolicits = reader.GetNextValueAsInt64("Icmp6InNeighborSolicits"),
                InNeighborAdvertisements = reader.GetNextValueAsInt64("Icmp6InNeighborAdvertisements"),
                InRedirects = reader.GetNextValueAsInt64("Icmp6InRedirects"),
                OutDestUnreachs = reader.GetNextValueAsInt64("Icmp6OutDestUnreachs"),
                OutPktTooBigs = reader.GetNextValueAsInt64("Icmp6OutPktTooBigs"),
                OutTimeExcds = reader.GetNextValueAsInt64("Icmp6OutTimeExcds"),
                OutParmProblems = reader.GetNextValueAsInt64("Icmp6OutParmProblems"),
                OutEchos = reader.GetNextValueAsInt64("Icmp6OutEchos"),
                OutEchoReplies = reader.GetNextValueAsInt64("Icmp6OutEchoReplies"),
                OutInGroupMembQueries = reader.GetNextValueAsInt64("Icmp6OutGroupMembQueries"),
                OutGroupMembResponses = reader.GetNextValueAsInt64("Icmp6OutGroupMembResponses"),
                OutGroupMembReductions = reader.GetNextValueAsInt64("Icmp6OutGroupMembReductions"),
                OutRouterSolicits = reader.GetNextValueAsInt64("Icmp6OutRouterSolicits"),
                OutRouterAdvertisements = reader.GetNextValueAsInt64("Icmp6OutRouterAdvertisements"),
                OutNeighborSolicits = reader.GetNextValueAsInt64("Icmp6OutNeighborSolicits"),
                OutNeighborAdvertisements = reader.GetNextValueAsInt64("Icmp6OutNeighborAdvertisements"),
                OutRedirects = reader.GetNextValueAsInt64("Icmp6OutRedirects")
            };
        }

        public static IPGlobalStatisticsTable ParseIPv4GlobalStatisticsFromSnmpFile(string filePath)
        {
            string fileContents = ReadAllText(filePath);

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
                InReceives = parser.ParseNextInt64(),
                InHeaderErrors = parser.ParseNextInt64(),
                InAddressErrors = parser.ParseNextInt64(),
                ForwardedDatagrams = parser.ParseNextInt64(),
                InUnknownProtocols = parser.ParseNextInt64(),
                InDiscards = parser.ParseNextInt64(),
                InDelivers = parser.ParseNextInt64(),
                OutRequests = parser.ParseNextInt64(),
                OutDiscards = parser.ParseNextInt64(),
                OutNoRoutes = parser.ParseNextInt64(),
                ReassemblyTimeout = parser.ParseNextInt64(),
                ReassemblyRequireds = parser.ParseNextInt64(),
                ReassemblyOKs = parser.ParseNextInt64(),
                ReassemblyFails = parser.ParseNextInt64(),
                FragmentOKs = parser.ParseNextInt64(),
                FragmentFails = parser.ParseNextInt64(),
                FragmentCreates = parser.ParseNextInt64(),
            };
        }

        internal static IPGlobalStatisticsTable ParseIPv6GlobalStatisticsFromSnmp6File(string filePath)
        {
            // Read the remainder of statistics from snmp6.
            string fileContents = ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);

            return new IPGlobalStatisticsTable()
            {
                InReceives = reader.GetNextValueAsInt64("Ip6InReceives"),
                InHeaderErrors = reader.GetNextValueAsInt64("Ip6InHdrErrors"),
                InAddressErrors = reader.GetNextValueAsInt64("Ip6InAddrErrors"),
                InUnknownProtocols = reader.GetNextValueAsInt64("Ip6InUnknownProtos"),
                InDiscards = reader.GetNextValueAsInt64("Ip6InDiscards"),
                InDelivers = reader.GetNextValueAsInt64("Ip6InDelivers"),
                ForwardedDatagrams = reader.GetNextValueAsInt64("Ip6OutForwDatagrams"),
                OutRequests = reader.GetNextValueAsInt64("Ip6OutRequests"),
                OutDiscards = reader.GetNextValueAsInt64("Ip6OutDiscards"),
                OutNoRoutes = reader.GetNextValueAsInt64("Ip6OutNoRoutes"),
                ReassemblyTimeout = reader.GetNextValueAsInt64("Ip6ReasmTimeout"),
                ReassemblyRequireds = reader.GetNextValueAsInt64("Ip6ReasmReqds"),
                ReassemblyOKs = reader.GetNextValueAsInt64("Ip6ReasmOKs"),
                ReassemblyFails = reader.GetNextValueAsInt64("Ip6ReasmFails"),
                FragmentOKs = reader.GetNextValueAsInt64("Ip6FragOKs"),
                FragmentFails = reader.GetNextValueAsInt64("Ip6FragFails"),
                FragmentCreates = reader.GetNextValueAsInt64("Ip6FragCreates"),
            };
        }

        internal static TcpGlobalStatisticsTable ParseTcpGlobalStatisticsFromSnmpFile(string filePath)
        {
            // NOTE: There is no information in the snmp6 file regarding TCP statistics,
            // so the statistics are always pulled from /proc/net/snmp.
            string fileContents = ReadAllText(filePath);
            int firstTcpHeader = fileContents.IndexOf("Tcp:", StringComparison.Ordinal);
            int secondTcpHeader = fileContents.IndexOf("Tcp:", firstTcpHeader + 1, StringComparison.Ordinal);
            int inCsumErrorsIdx = fileContents.IndexOf("InCsumErrors", firstTcpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondTcpHeader, StringComparison.Ordinal);
            string tcpData = fileContents.Substring(secondTcpHeader, endOfSecondLine - secondTcpHeader);
            StringParser parser = new StringParser(tcpData, ' ');

            parser.MoveNextOrFail(); // Skip Tcp:

            return new TcpGlobalStatisticsTable()
            {
                RtoAlgorithm = parser.ParseNextInt64(),
                RtoMin = parser.ParseNextInt64(),
                RtoMax = parser.ParseNextInt64(),
                MaxConn = parser.ParseNextInt64(),
                ActiveOpens = parser.ParseNextInt64(),
                PassiveOpens = parser.ParseNextInt64(),
                AttemptFails = parser.ParseNextInt64(),
                EstabResets = parser.ParseNextInt64(),
                CurrEstab = parser.ParseNextInt64(),
                InSegs = parser.ParseNextInt64(),
                OutSegs = parser.ParseNextInt64(),
                RetransSegs = parser.ParseNextInt64(),
                InErrs = parser.ParseNextInt64(),
                OutRsts = parser.ParseNextInt64(),
                InCsumErrors = inCsumErrorsIdx == -1 ? 0 : parser.ParseNextInt64()
            };
        }

        internal static UdpGlobalStatisticsTable ParseUdpv4GlobalStatisticsFromSnmpFile(string filePath)
        {
            string fileContents = ReadAllText(filePath);
            int firstUdpHeader = fileContents.IndexOf("Udp:", StringComparison.Ordinal);
            int secondUdpHeader = fileContents.IndexOf("Udp:", firstUdpHeader + 1, StringComparison.Ordinal);
            int inCsumErrorsIdx = fileContents.IndexOf("InCsumErrors", firstUdpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = fileContents.IndexOf(Environment.NewLine, secondUdpHeader, StringComparison.Ordinal);
            string tcpData = fileContents.Substring(secondUdpHeader, endOfSecondLine - secondUdpHeader);
            StringParser parser = new StringParser(tcpData, ' ');

            parser.MoveNextOrFail(); // Skip Udp:

            return new UdpGlobalStatisticsTable()
            {
                InDatagrams = parser.ParseNextInt64(),
                NoPorts = parser.ParseNextInt64(),
                InErrors = parser.ParseNextInt64(),
                OutDatagrams = parser.ParseNextInt64(),
                RcvbufErrors = parser.ParseNextInt64(),
                SndbufErrors = parser.ParseNextInt64(),
                InCsumErrors = inCsumErrorsIdx == -1 ? 0 : parser.ParseNextInt64()
            };
        }

        internal static UdpGlobalStatisticsTable ParseUdpv6GlobalStatisticsFromSnmp6File(string filePath)
        {
            string fileContents = ReadAllText(filePath);
            RowConfigReader reader = new RowConfigReader(fileContents);
            bool hasUdp6Errors = fileContents.Contains("Udp6SndbufErrors");

            return new UdpGlobalStatisticsTable()
            {
                InDatagrams = reader.GetNextValueAsInt64("Udp6InDatagrams"),
                NoPorts = reader.GetNextValueAsInt64("Udp6NoPorts"),
                InErrors = reader.GetNextValueAsInt64("Udp6InErrors"),
                OutDatagrams = reader.GetNextValueAsInt64("Udp6OutDatagrams"),
                RcvbufErrors = hasUdp6Errors ? reader.GetNextValueAsInt64("Udp6RcvbufErrors") : 0,
                SndbufErrors = hasUdp6Errors ? reader.GetNextValueAsInt64("Udp6SndbufErrors") : 0,
                InCsumErrors = hasUdp6Errors ? reader.GetNextValueAsInt64("Udp6InCsumErrors") : 0,
            };
        }

        internal static IPInterfaceStatisticsTable ParseInterfaceStatisticsTableFromFile(string filePath, string name)
        {
            using (StreamReader sr = OpenStreamReader(filePath))
            {
                sr.ReadLine();
                sr.ReadLine();
                Span<Range> pieces = stackalloc Range[18]; // [0]-[16] used, +1 to ensure any additional segment goes into [17]
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine()!;
                    if (line.Contains(name))
                    {
                        ReadOnlySpan<char> lineSpan = line;
                        int pieceCount = lineSpan.SplitAny(pieces, " :", StringSplitOptions.RemoveEmptyEntries);

                        if (pieceCount < 17)
                        {
                            continue;
                        }

                        if (!lineSpan[pieces[0]].SequenceEqual(name))
                        {
                            // The adapter name doesn't exactly match.
                            continue;
                        }

                        return new IPInterfaceStatisticsTable()
                        {
                            BytesReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[1]]),
                            PacketsReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[2]]),
                            ErrorsReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[3]]),
                            IncomingPacketsDropped = ParseUInt64AndClampToInt64(lineSpan[pieces[4]]),
                            FifoBufferErrorsReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[5]]),
                            PacketFramingErrorsReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[6]]),
                            CompressedPacketsReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[7]]),
                            MulticastFramesReceived = ParseUInt64AndClampToInt64(lineSpan[pieces[8]]),

                            BytesTransmitted = ParseUInt64AndClampToInt64(lineSpan[pieces[9]]),
                            PacketsTransmitted = ParseUInt64AndClampToInt64(lineSpan[pieces[10]]),
                            ErrorsTransmitted = ParseUInt64AndClampToInt64(lineSpan[pieces[11]]),
                            OutgoingPacketsDropped = ParseUInt64AndClampToInt64(lineSpan[pieces[12]]),
                            FifoBufferErrorsTransmitted = ParseUInt64AndClampToInt64(lineSpan[pieces[13]]),
                            CollisionsDetected = ParseUInt64AndClampToInt64(lineSpan[pieces[14]]),
                            CarrierLosses = ParseUInt64AndClampToInt64(lineSpan[pieces[15]]),
                            CompressedPacketsTransmitted = ParseUInt64AndClampToInt64(lineSpan[pieces[16]]),
                        };
                    }
                }

                throw ExceptionHelper.CreateForParseFailure();
            }
        }

        private static long ParseUInt64AndClampToInt64(ReadOnlySpan<char> value)
        {
            return (long)Math.Min(long.MaxValue, ulong.Parse(value, CultureInfo.InvariantCulture));
        }
    }
}
