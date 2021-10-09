// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    public class NetworkInterfaceIPv4Statistics
    {
        private readonly ITestOutputHelper _log;

        public NetworkInterfaceIPv4Statistics()
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Not all APIs are supported on Linux and OSX
        public void BasicTest_GetIPv4InterfaceStatistics_Success()
        {
            // This API is not actually IPv4 specific.
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                _log.WriteLine("IncomingUnknownProtocolPackets: " + stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                _log.WriteLine("NonUnicastPacketsSent: " + stats.NonUnicastPacketsSent);
                _log.WriteLine("OutgoingPacketsDiscarded: " + stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]  // Some APIs are not supported on Windows and OSX
        public void BasicTest_GetIPv4InterfaceStatistics_Success_Linux()
        {
            // This API is not actually IPv4 specific.
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                Assert.Throws<PlatformNotSupportedException>(() => stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                Assert.Throws<PlatformNotSupportedException>(() => stats.NonUnicastPacketsSent);
                _log.WriteLine("OutgoingPacketsDiscarded: " + stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // Some APIs are not supported on OSX
        public void BasicTest_GetIPv4InterfaceStatistics_Success_OSX()
        {
            // This API is not actually IPv4 specific.
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPv4InterfaceStatistics stats = nic.GetIPv4Statistics();

                _log.WriteLine("- Stats for : " + nic.Name);
                _log.WriteLine("BytesReceived: " + stats.BytesReceived);
                _log.WriteLine("BytesSent: " + stats.BytesSent);
                _log.WriteLine("IncomingPacketsDiscarded: " + stats.IncomingPacketsDiscarded);
                _log.WriteLine("IncomingPacketsWithErrors: " + stats.IncomingPacketsWithErrors);
                _log.WriteLine("IncomingUnknownProtocolPackets: " + stats.IncomingUnknownProtocolPackets);
                _log.WriteLine("NonUnicastPacketsReceived: " + stats.NonUnicastPacketsReceived);
                _log.WriteLine("NonUnicastPacketsSent: " + stats.NonUnicastPacketsSent);
                Assert.Throws<PlatformNotSupportedException>(() => stats.OutgoingPacketsDiscarded);
                _log.WriteLine("OutgoingPacketsWithErrors: " + stats.OutgoingPacketsWithErrors);
                _log.WriteLine("OutputQueueLength: " + stats.OutputQueueLength);
                _log.WriteLine("UnicastPacketsReceived: " + stats.UnicastPacketsReceived);
                _log.WriteLine("UnicastPacketsSent: " + stats.UnicastPacketsSent);
            }
        }
    }
}
