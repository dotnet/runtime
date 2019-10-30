// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    // UDP statistics.
    internal class SystemUdpStatistics : UdpStatistics
    {
        private readonly Interop.IpHlpApi.MibUdpStats _stats;

        private SystemUdpStatistics() { }

        internal SystemUdpStatistics(AddressFamily family)
        {
            uint result = Interop.IpHlpApi.GetUdpStatisticsEx(out _stats, family);

            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }
        }

        public override long DatagramsReceived { get { return _stats.datagramsReceived; } }

        public override long IncomingDatagramsDiscarded { get { return _stats.incomingDatagramsDiscarded; } }

        public override long IncomingDatagramsWithErrors { get { return _stats.incomingDatagramsWithErrors; } }

        public override long DatagramsSent { get { return _stats.datagramsSent; } }

        public override int UdpListeners { get { return (int)_stats.udpListeners; } }
    }
}
