// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
                if (_dnsAddresses == null)
                {
                    throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
                }

                return _dnsAddresses.Count > 0;
            }
        }

        public sealed override string DnsSuffix
        {
            get
            {
                if (_dnsSuffix == null)
                {
                    throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
                }

                return _dnsSuffix;
            }
        }

        public sealed override IPAddressCollection DnsAddresses
        {
            get
            {
                if (_dnsAddresses == null)
                {
                    throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
                }

                return _dnsAddresses;
            }
        }

        private static UnicastIPAddressInformationCollection GetUnicastAddresses(UnixNetworkInterface uni)
        {
            var collection = new UnicastIPAddressInformationCollection();
            foreach (IPAddress address in uni.Addresses.Where((addr) => !IPAddressUtil.IsMulticast(addr)))
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
            foreach (IPAddress address in uni.Addresses.Where(IPAddressUtil.IsMulticast))
            {
                collection.InternalAdd(new UnixMulticastIPAddressInformation(address));
            }

            return collection;
        }

        private static string GetDnsSuffix()
        {
            try
            {
                return StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(NetworkFiles.EtcResolvConfFile);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private static IPAddressCollection GetDnsAddresses()
        {
            try
            {
                List<IPAddress> internalAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(NetworkFiles.EtcResolvConfFile);
                return new InternalIPAddressCollection(internalAddresses);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
