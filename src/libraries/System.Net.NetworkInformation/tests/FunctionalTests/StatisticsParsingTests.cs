// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class StatisticsParsingTests : FileCleanupTestBase
    {
        [Fact]
        public void Icmpv4Parsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp", fileName);
            Icmpv4StatisticsTable table = StringParsingHelpers.ParseIcmpv4FromSnmpFile(fileName);
            Assert.Equal(1, table.InMsgs);
            Assert.Equal(2, table.InErrors);
            Assert.Equal(3, table.InCsumErrors);
            Assert.Equal(4, table.InDestUnreachs);
            Assert.Equal(5, table.InTimeExcds);
            Assert.Equal(6, table.InParmProbs);
            Assert.Equal(7, table.InSrcQuenchs);
            Assert.Equal(8, table.InRedirects);
            Assert.Equal(9, table.InEchos);
            Assert.Equal(10, table.InEchoReps);
            Assert.Equal(20, table.InTimestamps);
            Assert.Equal(30, table.InTimeStampReps);
            Assert.Equal(40, table.InAddrMasks);
            Assert.Equal(50, table.InAddrMaskReps);
            Assert.Equal(60, table.OutMsgs);
            Assert.Equal(70, table.OutErrors);
            Assert.Equal(80, table.OutDestUnreachs);
            Assert.Equal(90, table.OutTimeExcds);
            Assert.Equal(100, table.OutParmProbs);
            Assert.Equal(255, table.OutSrcQuenchs);
            Assert.Equal(1024, table.OutRedirects);
            Assert.Equal(256, table.OutEchos);
            Assert.Equal(9001, table.OutEchoReps);
            Assert.Equal(42, table.OutTimestamps);
            Assert.Equal(4100414, table.OutTimestampReps);
            Assert.Equal(2147483647, table.OutAddrMasks);
            Assert.Equal(0, table.OutAddrMaskReps);
        }

        [Fact]
        public void Icmpv6Parsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp6", fileName);
            Icmpv6StatisticsTable table = StringParsingHelpers.ParseIcmpv6FromSnmp6File(fileName);
            Assert.Equal(1, table.InMsgs);
            Assert.Equal(2, table.InErrors);
            Assert.Equal(3, table.OutMsgs);
            Assert.Equal(4, table.OutErrors);
            Assert.Equal(6, table.InDestUnreachs);
            Assert.Equal(7, table.InPktTooBigs);
            Assert.Equal(8, table.InTimeExcds);
            Assert.Equal(9, table.InParmProblems);
            Assert.Equal(10, table.InEchos);
            Assert.Equal(11, table.InEchoReplies);
            Assert.Equal(12, table.InGroupMembQueries);
            Assert.Equal(13, table.InGroupMembResponses);
            Assert.Equal(14, table.InGroupMembReductions);
            Assert.Equal(15, table.InRouterSolicits);
            Assert.Equal(16, table.InRouterAdvertisements);
            Assert.Equal(17, table.InNeighborSolicits);
            Assert.Equal(18, table.InNeighborAdvertisements);
            Assert.Equal(19, table.InRedirects);
            Assert.Equal(21, table.OutDestUnreachs);
            Assert.Equal(22, table.OutPktTooBigs);
            Assert.Equal(23, table.OutTimeExcds);
            Assert.Equal(24, table.OutParmProblems);
            Assert.Equal(25, table.OutEchos);
            Assert.Equal(26, table.OutEchoReplies);
            Assert.Equal(27, table.OutInGroupMembQueries);
            Assert.Equal(28, table.OutGroupMembResponses);
            Assert.Equal(29, table.OutGroupMembReductions);
            Assert.Equal(30, table.OutRouterSolicits);
            Assert.Equal(31, table.OutRouterAdvertisements);
            Assert.Equal(32, table.OutNeighborSolicits);
            Assert.Equal(33, table.OutNeighborAdvertisements);
            Assert.Equal(34, table.OutRedirects);
        }

        [Fact]
        public void TcpGlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp", fileName);
            TcpGlobalStatisticsTable table = StringParsingHelpers.ParseTcpGlobalStatisticsFromSnmpFile(fileName);
            Assert.Equal(1, table.RtoAlgorithm);
            Assert.Equal(200, table.RtoMin);
            Assert.Equal(120000, table.RtoMax);
            Assert.Equal(-1, table.MaxConn);
            Assert.Equal(359, table.ActiveOpens);
            Assert.Equal(28, table.PassiveOpens);
            Assert.Equal(2, table.AttemptFails);
            Assert.Equal(53, table.EstabResets);
            Assert.Equal(4, table.CurrEstab);
            Assert.Equal(21368, table.InSegs);
            Assert.Equal(20642, table.OutSegs);
            Assert.Equal(19, table.RetransSegs);
            Assert.Equal(0, table.InErrs);
            Assert.Equal(111, table.OutRsts);
            Assert.Equal(0, table.InCsumErrors);
        }

        [Fact]
        public void Udpv4GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp", fileName);
            UdpGlobalStatisticsTable table = StringParsingHelpers.ParseUdpv4GlobalStatisticsFromSnmpFile(fileName);
            Assert.Equal(7181, table.InDatagrams);
            Assert.Equal(150, table.NoPorts);
            Assert.Equal(0, table.InErrors);
            Assert.Equal(4386, table.OutDatagrams);
            Assert.Equal(0, table.RcvbufErrors);
            Assert.Equal(0, table.SndbufErrors);
            Assert.Equal(1, table.InCsumErrors);
        }

        [Fact]
        public void Udpv6GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp6", fileName);
            UdpGlobalStatisticsTable table = StringParsingHelpers.ParseUdpv6GlobalStatisticsFromSnmp6File(fileName);
            Assert.Equal(19, table.InDatagrams);
            Assert.Equal(0, table.NoPorts);
            Assert.Equal(0, table.InErrors);
            Assert.Equal(21, table.OutDatagrams);
            Assert.Equal(99999, table.RcvbufErrors);
            Assert.Equal(11011011, table.SndbufErrors);
            Assert.Equal(0, table.InCsumErrors);
        }

        [Fact]
        public void Ipv4GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp", fileName);
            IPGlobalStatisticsTable table = StringParsingHelpers.ParseIPv4GlobalStatisticsFromSnmpFile(fileName);

            Assert.Equal(false, table.Forwarding);
            Assert.Equal(64, table.DefaultTtl);
            Assert.Equal(28121, table.InReceives);
            Assert.Equal(0, table.InHeaderErrors);
            Assert.Equal(2, table.InAddressErrors);
            Assert.Equal(0, table.ForwardedDatagrams);
            Assert.Equal(0, table.InUnknownProtocols);
            Assert.Equal(0, table.InDiscards);
            Assert.Equal(28117, table.InDelivers);
            Assert.Equal(24616, table.OutRequests);
            Assert.Equal(48, table.OutDiscards);
            Assert.Equal(0, table.OutNoRoutes);
            Assert.Equal(0, table.ReassemblyTimeout);
            Assert.Equal(0, table.ReassemblyRequireds);
            Assert.Equal(1, table.ReassemblyOKs);
            Assert.Equal(2, table.ReassemblyFails);
            Assert.Equal(14, table.FragmentOKs);
            Assert.Equal(0, table.FragmentFails);
            Assert.Equal(92, table.FragmentCreates);
        }

        [Fact]
        public void Ipv6GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("snmp6", fileName);
            IPGlobalStatisticsTable table = StringParsingHelpers.ParseIPv6GlobalStatisticsFromSnmp6File(fileName);

            Assert.Equal(189, table.InReceives);
            Assert.Equal(0, table.InHeaderErrors);
            Assert.Equal(2000, table.InAddressErrors);
            Assert.Equal(42, table.InUnknownProtocols);
            Assert.Equal(0, table.InDiscards);
            Assert.Equal(189, table.InDelivers);
            Assert.Equal(55, table.ForwardedDatagrams);
            Assert.Equal(199, table.OutRequests);
            Assert.Equal(0, table.OutDiscards);
            Assert.Equal(53, table.OutNoRoutes);
            Assert.Equal(2121, table.ReassemblyTimeout);
            Assert.Equal(1, table.ReassemblyRequireds);
            Assert.Equal(2, table.ReassemblyOKs);
            Assert.Equal(4, table.ReassemblyFails);
            Assert.Equal(8, table.FragmentOKs);
            Assert.Equal(16, table.FragmentFails);
            Assert.Equal(32, table.FragmentCreates);
        }

        [Fact]
        public void IpInterfaceStatisticsParsingFirst()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("dev", fileName);
            IPInterfaceStatisticsTable table = StringParsingHelpers.ParseInterfaceStatisticsTableFromFile(fileName, "wlan0");

            Assert.Equal(26622u, table.BytesReceived);
            Assert.Equal(394u, table.PacketsReceived);
            Assert.Equal(2u, table.ErrorsReceived);
            Assert.Equal(4u, table.IncomingPacketsDropped);
            Assert.Equal(6u, table.FifoBufferErrorsReceived);
            Assert.Equal(8u, table.PacketFramingErrorsReceived);
            Assert.Equal(10u, table.CompressedPacketsReceived);
            Assert.Equal(12u, table.MulticastFramesReceived);

            Assert.Equal(27465u, table.BytesTransmitted);
            Assert.Equal(208u, table.PacketsTransmitted);
            Assert.Equal(1u, table.ErrorsTransmitted);
            Assert.Equal(2u, table.OutgoingPacketsDropped);
            Assert.Equal(3u, table.FifoBufferErrorsTransmitted);
            Assert.Equal(4u, table.CollisionsDetected);
            Assert.Equal(5u, table.CarrierLosses);
            Assert.Equal(6u, table.CompressedPacketsTransmitted);
        }

        [Fact]
        public void IpInterfaceStatisticsParsingLast()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("dev", fileName);
            IPInterfaceStatisticsTable table = StringParsingHelpers.ParseInterfaceStatisticsTableFromFile(fileName, "lo");

            Assert.Equal(uint.MaxValue, table.BytesReceived);
            Assert.Equal(302u, table.PacketsReceived);
            Assert.Equal(0u, table.ErrorsReceived);
            Assert.Equal(0u, table.IncomingPacketsDropped);
            Assert.Equal(0u, table.FifoBufferErrorsReceived);
            Assert.Equal(0u, table.PacketFramingErrorsReceived);
            Assert.Equal(0u, table.CompressedPacketsReceived);
            Assert.Equal(0u, table.MulticastFramesReceived);

            Assert.Equal(30008u, table.BytesTransmitted);
            Assert.Equal(302u, table.PacketsTransmitted);
            Assert.Equal(0u, table.ErrorsTransmitted);
            Assert.Equal(0u, table.OutgoingPacketsDropped);
            Assert.Equal(0u, table.FifoBufferErrorsTransmitted);
            Assert.Equal(0u, table.CollisionsDetected);
            Assert.Equal(0u, table.CarrierLosses);
            Assert.Equal(0u, table.CompressedPacketsTransmitted);
        }
    }
}
