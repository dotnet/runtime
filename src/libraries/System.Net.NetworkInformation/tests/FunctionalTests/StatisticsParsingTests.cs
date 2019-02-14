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
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp", fileName);
            Icmpv4StatisticsTable table = StringParsingHelpers.ParseIcmpv4FromSnmpFile(fileName);
            Assert.Equal(1L, table.InMsgs);
            Assert.Equal(2L, table.InErrors);
            Assert.Equal(3L, table.InCsumErrors);
            Assert.Equal(4L, table.InDestUnreachs);
            Assert.Equal(5L, table.InTimeExcds);
            Assert.Equal(6L, table.InParmProbs);
            Assert.Equal(7L, table.InSrcQuenchs);
            Assert.Equal(8L, table.InRedirects);
            Assert.Equal(9L, table.InEchos);
            Assert.Equal(10L, table.InEchoReps);
            Assert.Equal(20L, table.InTimestamps);
            Assert.Equal(30L, table.InTimeStampReps);
            Assert.Equal(40L, table.InAddrMasks);
            Assert.Equal(50L, table.InAddrMaskReps);
            Assert.Equal(60L, table.OutMsgs);
            Assert.Equal(70L, table.OutErrors);
            Assert.Equal(80L, table.OutDestUnreachs);
            Assert.Equal(90L, table.OutTimeExcds);
            Assert.Equal(100L, table.OutParmProbs);
            Assert.Equal(255L, table.OutSrcQuenchs);
            Assert.Equal(1024L, table.OutRedirects);
            Assert.Equal(256L, table.OutEchos);
            Assert.Equal(9001L, table.OutEchoReps);
            Assert.Equal(42L, table.OutTimestamps);
            Assert.Equal(4100414L, table.OutTimestampReps);
            Assert.Equal(2147483647L, table.OutAddrMasks);
            Assert.Equal(0L, table.OutAddrMaskReps);
        }

        [Fact]
        public void Icmpv6Parsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp6", fileName);
            Icmpv6StatisticsTable table = StringParsingHelpers.ParseIcmpv6FromSnmp6File(fileName);
            Assert.Equal(1L, table.InMsgs);
            Assert.Equal(2L, table.InErrors);
            Assert.Equal(3L, table.OutMsgs);
            Assert.Equal(4L, table.OutErrors);
            Assert.Equal(6L, table.InDestUnreachs);
            Assert.Equal(7L, table.InPktTooBigs);
            Assert.Equal(8L, table.InTimeExcds);
            Assert.Equal(9L, table.InParmProblems);
            Assert.Equal(10L, table.InEchos);
            Assert.Equal(11L, table.InEchoReplies);
            Assert.Equal(12L, table.InGroupMembQueries);
            Assert.Equal(13L, table.InGroupMembResponses);
            Assert.Equal(14L, table.InGroupMembReductions);
            Assert.Equal(15L, table.InRouterSolicits);
            Assert.Equal(16L, table.InRouterAdvertisements);
            Assert.Equal(17L, table.InNeighborSolicits);
            Assert.Equal(18L, table.InNeighborAdvertisements);
            Assert.Equal(19L, table.InRedirects);
            Assert.Equal(21L, table.OutDestUnreachs);
            Assert.Equal(22L, table.OutPktTooBigs);
            Assert.Equal(23L, table.OutTimeExcds);
            Assert.Equal(24L, table.OutParmProblems);
            Assert.Equal(25L, table.OutEchos);
            Assert.Equal(26L, table.OutEchoReplies);
            Assert.Equal(27L, table.OutInGroupMembQueries);
            Assert.Equal(28L, table.OutGroupMembResponses);
            Assert.Equal(29L, table.OutGroupMembReductions);
            Assert.Equal(30L, table.OutRouterSolicits);
            Assert.Equal(31L, table.OutRouterAdvertisements);
            Assert.Equal(32L, table.OutNeighborSolicits);
            Assert.Equal(33L, table.OutNeighborAdvertisements);
            Assert.Equal(34L, table.OutRedirects);
        }

        [Fact]
        public void TcpGlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp", fileName);
            TcpGlobalStatisticsTable table = StringParsingHelpers.ParseTcpGlobalStatisticsFromSnmpFile(fileName);
            Assert.Equal(1L, table.RtoAlgorithm);
            Assert.Equal(200L, table.RtoMin);
            Assert.Equal(120000L, table.RtoMax);
            Assert.Equal(-1L, table.MaxConn);
            Assert.Equal(359L, table.ActiveOpens);
            Assert.Equal(28L, table.PassiveOpens);
            Assert.Equal(2L, table.AttemptFails);
            Assert.Equal(53L, table.EstabResets);
            Assert.Equal(4L, table.CurrEstab);
            Assert.Equal(2592159585L, table.InSegs);
            Assert.Equal(2576867425L, table.OutSegs);
            Assert.Equal(19L, table.RetransSegs);
            Assert.Equal(0L, table.InErrs);
            Assert.Equal(111L, table.OutRsts);
            Assert.Equal(0L, table.InCsumErrors);
        }

        [Fact]
        public void Udpv4GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp", fileName);
            UdpGlobalStatisticsTable table = StringParsingHelpers.ParseUdpv4GlobalStatisticsFromSnmpFile(fileName);
            Assert.Equal(7181L, table.InDatagrams);
            Assert.Equal(150L, table.NoPorts);
            Assert.Equal(0L, table.InErrors);
            Assert.Equal(4386L, table.OutDatagrams);
            Assert.Equal(0L, table.RcvbufErrors);
            Assert.Equal(0L, table.SndbufErrors);
            Assert.Equal(1L, table.InCsumErrors);
        }

        [Fact]
        public void Udpv6GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp6", fileName);
            UdpGlobalStatisticsTable table = StringParsingHelpers.ParseUdpv6GlobalStatisticsFromSnmp6File(fileName);
            Assert.Equal(19L, table.InDatagrams);
            Assert.Equal(0L, table.NoPorts);
            Assert.Equal(0L, table.InErrors);
            Assert.Equal(21L, table.OutDatagrams);
            Assert.Equal(99999L, table.RcvbufErrors);
            Assert.Equal(11011011L, table.SndbufErrors);
            Assert.Equal(0L, table.InCsumErrors);
        }

        [Fact]
        public void Ipv4GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp", fileName);
            IPGlobalStatisticsTable table = StringParsingHelpers.ParseIPv4GlobalStatisticsFromSnmpFile(fileName);

            Assert.Equal(false, table.Forwarding);
            Assert.Equal(64L, table.DefaultTtl);
            Assert.Equal(28121L, table.InReceives);
            Assert.Equal(0L, table.InHeaderErrors);
            Assert.Equal(2L, table.InAddressErrors);
            Assert.Equal(0L, table.ForwardedDatagrams);
            Assert.Equal(0L, table.InUnknownProtocols);
            Assert.Equal(0L, table.InDiscards);
            Assert.Equal(28117L, table.InDelivers);
            Assert.Equal(24616L, table.OutRequests);
            Assert.Equal(48L, table.OutDiscards);
            Assert.Equal(0L, table.OutNoRoutes);
            Assert.Equal(0L, table.ReassemblyTimeout);
            Assert.Equal(0L, table.ReassemblyRequireds);
            Assert.Equal(1L, table.ReassemblyOKs);
            Assert.Equal(2L, table.ReassemblyFails);
            Assert.Equal(14L, table.FragmentOKs);
            Assert.Equal(0L, table.FragmentFails);
            Assert.Equal(92L, table.FragmentCreates);
        }

        [Fact]
        public void Ipv6GlobalStatisticsParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/snmp6", fileName);
            IPGlobalStatisticsTable table = StringParsingHelpers.ParseIPv6GlobalStatisticsFromSnmp6File(fileName);

            Assert.Equal(189L, table.InReceives);
            Assert.Equal(0L, table.InHeaderErrors);
            Assert.Equal(2000L, table.InAddressErrors);
            Assert.Equal(42L, table.InUnknownProtocols);
            Assert.Equal(0L, table.InDiscards);
            Assert.Equal(189L, table.InDelivers);
            Assert.Equal(55L, table.ForwardedDatagrams);
            Assert.Equal(199L, table.OutRequests);
            Assert.Equal(0L, table.OutDiscards);
            Assert.Equal(53L, table.OutNoRoutes);
            Assert.Equal(2121L, table.ReassemblyTimeout);
            Assert.Equal(1L, table.ReassemblyRequireds);
            Assert.Equal(2L, table.ReassemblyOKs);
            Assert.Equal(4L, table.ReassemblyFails);
            Assert.Equal(8L, table.FragmentOKs);
            Assert.Equal(16L, table.FragmentFails);
            Assert.Equal(32L, table.FragmentCreates);
        }

        [Fact]
        public void IpInterfaceStatisticsParsingFirst()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/dev", fileName);
            IPInterfaceStatisticsTable table = StringParsingHelpers.ParseInterfaceStatisticsTableFromFile(fileName, "wlan0");

            Assert.Equal(26622L, table.BytesReceived);
            Assert.Equal(394L, table.PacketsReceived);
            Assert.Equal(2L, table.ErrorsReceived);
            Assert.Equal(4L, table.IncomingPacketsDropped);
            Assert.Equal(6L, table.FifoBufferErrorsReceived);
            Assert.Equal(8L, table.PacketFramingErrorsReceived);
            Assert.Equal(10L, table.CompressedPacketsReceived);
            Assert.Equal(12L, table.MulticastFramesReceived);

            Assert.Equal(429496730000L, table.BytesTransmitted);
            Assert.Equal(208L, table.PacketsTransmitted);
            Assert.Equal(1L, table.ErrorsTransmitted);
            Assert.Equal(2L, table.OutgoingPacketsDropped);
            Assert.Equal(3L, table.FifoBufferErrorsTransmitted);
            Assert.Equal(4L, table.CollisionsDetected);
            Assert.Equal(5L, table.CarrierLosses);
            Assert.Equal(6L, table.CompressedPacketsTransmitted);
        }

        [Fact]
        public void IpInterfaceStatisticsParsingLast()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/dev", fileName);
            IPInterfaceStatisticsTable table = StringParsingHelpers.ParseInterfaceStatisticsTableFromFile(fileName, "lo");

            Assert.Equal(long.MaxValue, table.BytesReceived);
            Assert.Equal(302L, table.PacketsReceived);
            Assert.Equal(0L, table.ErrorsReceived);
            Assert.Equal(0L, table.IncomingPacketsDropped);
            Assert.Equal(0L, table.FifoBufferErrorsReceived);
            Assert.Equal(0L, table.PacketFramingErrorsReceived);
            Assert.Equal(0L, table.CompressedPacketsReceived);
            Assert.Equal(0L, table.MulticastFramesReceived);

            Assert.Equal(30008L, table.BytesTransmitted);
            Assert.Equal(302L, table.PacketsTransmitted);
            Assert.Equal(0L, table.ErrorsTransmitted);
            Assert.Equal(0L, table.OutgoingPacketsDropped);
            Assert.Equal(0L, table.FifoBufferErrorsTransmitted);
            Assert.Equal(0L, table.CollisionsDetected);
            Assert.Equal(0L, table.CarrierLosses);
            Assert.Equal(0L, table.CompressedPacketsTransmitted);
        }
    }
}
