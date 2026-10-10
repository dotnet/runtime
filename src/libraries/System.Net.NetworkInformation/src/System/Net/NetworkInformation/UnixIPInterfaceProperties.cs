// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPInterfaceProperties : IPInterfaceProperties
    {
        private UnicastIPAddressInformationCollection? _unicastAddresses;
        private MulticastIPAddressInformationCollection? _multicastAddreses;
        private readonly UnixNetworkInterface _uni;
        internal string? _dnsSuffix;
        internal IPAddressCollection _dnsAddresses;

        public UnixIPInterfaceProperties(UnixNetworkInterface uni, bool globalConfig = false)
        {
            _uni = uni;
            if (!globalConfig)
            {
                _dnsSuffix = GetDnsSuffix();
                _dnsAddresses = GetDnsAddresses();
            }
            else
            {
                _dnsAddresses = new InternalIPAddressCollection();
            }
        }

        public override UnicastIPAddressInformationCollection UnicastAddresses =>
            _unicastAddresses ??= GetUnicastAddresses(_uni);

        public sealed override MulticastIPAddressInformationCollection MulticastAddresses =>
            _multicastAddreses ??= GetMulticastAddresses(_uni);

        public override bool IsDnsEnabled
        {
            get => _dnsAddresses.Count > 0;
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
            get => _dnsAddresses;
        }

        private static UnicastIPAddressInformationCollection GetUnicastAddresses(UnixNetworkInterface uni)
        {
            var collection = new UnicastIPAddressInformationCollection();
            foreach (UnixUnicastIPAddressInformation address in uni.UnicastAddress)
            {
                collection.InternalAdd(address);
            }

            return collection;
        }

        private static MulticastIPAddressInformationCollection GetMulticastAddresses(UnixNetworkInterface uni)
        {
            var collection = new MulticastIPAddressInformationCollection();

            if (uni.MulticastAddresess != null)
            {
                foreach (IPAddress address in uni.MulticastAddresess)
                {
                    collection.InternalAdd(new UnixMulticastIPAddressInformation(address));
                }
            }

            return collection;
        }

        private static string? GetDnsSuffix()
        {
            string? resolverConfig = StringParsingHelpers.ReadResolvConfFile(NetworkFiles.EtcResolvConfFile);
            return resolverConfig is null ? null : StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(resolverConfig);
        }

        private static InternalIPAddressCollection GetDnsAddresses()
        {
            string? resolverConfig = StringParsingHelpers.ReadResolvConfFile(NetworkFiles.EtcResolvConfFile);
            return resolverConfig is null ?
                new InternalIPAddressCollection() :
                new InternalIPAddressCollection(StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(resolverConfig));
        }
    }
}
