// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPInterfaceProperties : IPInterfaceProperties
    {
        private UnicastIPAddressInformationCollection? _unicastAddresses;
        private MulticastIPAddressInformationCollection? _multicastAddreses;
        private readonly UnixNetworkInterface _uni;
        internal string? _dnsSuffix;
        internal IPAddressCollection? _dnsAddresses;

        public UnixIPInterfaceProperties(UnixNetworkInterface uni, bool globalConfig = false)
        {
            _uni = uni;
            if (!globalConfig)
            {
                _dnsSuffix = GetDnsSuffix();
                _dnsAddresses = GetDnsAddresses();
            }
        }

        public override UnicastIPAddressInformationCollection UnicastAddresses =>
            _unicastAddresses ??= GetUnicastAddresses(_uni);

        public sealed override MulticastIPAddressInformationCollection MulticastAddresses =>
            _multicastAddreses ??= GetMulticastAddresses(_uni);

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
            try
            {
                return StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(NetworkFiles.EtcResolvConfFile));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private static IPAddressCollection? GetDnsAddresses()
        {
            try
            {
                List<IPAddress> internalAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(File.ReadAllText(NetworkFiles.EtcResolvConfFile));
                return new InternalIPAddressCollection(internalAddresses);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
