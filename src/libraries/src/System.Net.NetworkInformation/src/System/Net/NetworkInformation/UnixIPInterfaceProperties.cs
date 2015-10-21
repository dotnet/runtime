// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPInterfaceProperties : IPInterfaceProperties
    {
        private UnicastIPAddressInformationCollection _unicastAddresses;
        private MulticastIPAddressInformationCollection _multicastAddreses;
        private readonly UnixNetworkInterface _uni;
        private readonly string _dnsSuffix;
        private readonly IPAddressCollection _dnsAddresses;

        public UnixIPInterfaceProperties(UnixNetworkInterface uni)
        {
            _uni = uni;
            _dnsSuffix = GetDnsSuffix();
            _dnsAddresses = GetDnsAddresses();
        }

        public sealed override UnicastIPAddressInformationCollection UnicastAddresses
        {
            get
            {
                return _unicastAddresses ?? (_unicastAddresses = GetUnicastAddresses(_uni));
            }
        }

        public sealed override MulticastIPAddressInformationCollection MulticastAddresses
        {
            get
            {
                return _multicastAddreses ?? (_multicastAddreses = GetMulticastAddresses(_uni));
            }
        }

        public override bool IsDnsEnabled
        {
            get
            {
                return DnsAddresses.Count > 0;
            }
        }

        public sealed override string DnsSuffix { get { return _dnsSuffix; } }

        public sealed override IPAddressCollection DnsAddresses { get { return _dnsAddresses; } }

        private static UnicastIPAddressInformationCollection GetUnicastAddresses(UnixNetworkInterface uni)
        {
            var collection = new UnicastIPAddressInformationCollection();
            foreach (IPAddress address in uni.Addresses.Where((addr) => !IsMulticast(addr)))
            {
                IPAddress netMask = (address.AddressFamily == AddressFamily.InterNetwork)
                                    ? uni.GetNetMaskForIPv4Address(address)
                                    : IPAddress.Any; // Windows compatibility
                collection.InternalAdd(new UnixUnicastIPAddressInformation(address, netMask));
            }

            return collection;
        }

        private static MulticastIPAddressInformationCollection GetMulticastAddresses(UnixNetworkInterface uni)
        {
            var collection = new MulticastIPAddressInformationCollection();
            foreach (IPAddress address in uni.Addresses.Where(IsMulticast))
            {
                collection.InternalAdd(new UnixMulticastIPAddressInformation(address));
            }

            return collection;
        }

        private static bool IsMulticast(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address.IsIPv6Multicast;
            }
            else
            {
                byte firstByte = address.GetAddressBytes()[0];
                return firstByte >= 224 && firstByte <= 239;
            }
        }

        private static string GetDnsSuffix()
        {
            string data = File.ReadAllText(NetworkFiles.EtcResolvConfFile);
            RowConfigReader rcr = new RowConfigReader(data);
            string dnsSuffix;

            return rcr.TryGetNextValue("search", out dnsSuffix) ? dnsSuffix : string.Empty;
        }

        private static IPAddressCollection GetDnsAddresses()
        {
            // Parse /etc/resolv.conf for all of the "nameserver" entries.
            // These are the DNS servers the machine is configured to use.
            // On OSX, this file is not directly used by most processes for DNS
            // queries/routing, but it is automatically generated instead, with
            // the machine's DNS servers listed in it.
            string data = File.ReadAllText(NetworkFiles.EtcResolvConfFile);
            RowConfigReader rcr = new RowConfigReader(data);
            InternalIPAddressCollection addresses = new InternalIPAddressCollection();

            string addressString = null;
            while (rcr.TryGetNextValue("nameserver", out addressString))
            {
                IPAddress parsedAddress;
                if (IPAddress.TryParse(addressString, out parsedAddress))
                {
                    addresses.InternalAdd(parsedAddress);
                }
            }

            return addresses;
        }
    }
}
