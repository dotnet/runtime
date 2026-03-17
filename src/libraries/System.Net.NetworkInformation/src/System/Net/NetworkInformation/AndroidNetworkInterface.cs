// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Implements a NetworkInterface on Android.
    /// </summary>
    internal sealed class AndroidNetworkInterface : UnixNetworkInterface
    {
        internal readonly int _mtu;
        private readonly AndroidIPInterfaceProperties _ipProperties;

        internal unsafe AndroidNetworkInterface(string name, Interop.Sys.NetworkInterfaceInfo *networkInterfaceInfo)
            : base(name)
        {
            _index = networkInterfaceInfo->InterfaceIndex;
            if (networkInterfaceInfo->NumAddressBytes > 0)
            {
                _physicalAddress = new PhysicalAddress(new ReadOnlySpan<byte>(networkInterfaceInfo->AddressBytes, networkInterfaceInfo->NumAddressBytes).ToArray());
            }

            _mtu = networkInterfaceInfo->Mtu;
            _ipProperties = new AndroidIPInterfaceProperties(this);

            OperationalStatus = (OperationalStatus)networkInterfaceInfo->OperationalState;
            Speed = networkInterfaceInfo->Speed;
            SupportsMulticast = networkInterfaceInfo->SupportsMulticast != 0;
            NetworkInterfaceType = (NetworkInterfaceType)networkInterfaceInfo->HardwareType;
        }

        internal unsafe void AddAddress(Interop.Sys.IpAddressInfo *addressInfo)
        {
            var address = new IPAddress(new ReadOnlySpan<byte>(addressInfo->AddressBytes, addressInfo->NumAddressBytes));
            if (address.IsIPv6LinkLocal)
            {
                address.ScopeId = addressInfo->InterfaceIndex;
            }

            AddAddress(address, addressInfo->PrefixLength);
        }

        public override bool SupportsMulticast { get; }
        public override IPInterfaceProperties GetIPProperties() => _ipProperties;
        public override IPInterfaceStatistics GetIPStatistics() => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        public override OperationalStatus OperationalStatus { get; }
        public override NetworkInterfaceType NetworkInterfaceType { get; }
        public override long Speed { get; }
        public override bool IsReceiveOnly => false;
    }
}
