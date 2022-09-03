// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixNetworkInterface : NetworkInterface
    {
        protected string _name;
        protected int _index = -1;
        protected NetworkInterfaceType _networkInterfaceType = NetworkInterfaceType.Unknown;
        internal PhysicalAddress _physicalAddress = PhysicalAddress.None;
        internal List<UnixUnicastIPAddressInformation> _unicastAddresses = new List<UnixUnicastIPAddressInformation>();
        internal List<IPAddress>? _multicastAddresses;
        // If this is an ipv6 device, contains the Scope ID.
        protected uint? _ipv6ScopeId;

        protected UnixNetworkInterface(string name)
        {
            _name = name;
        }

        public sealed override string Id { get { return _name; } }

        public sealed override string Name { get { return _name; } }

        public sealed override string Description { get { return _name; } }

        public override NetworkInterfaceType NetworkInterfaceType { get { return _networkInterfaceType; } }

        public sealed override PhysicalAddress GetPhysicalAddress() { return _physicalAddress; }

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            Sockets.AddressFamily family = (networkInterfaceComponent == NetworkInterfaceComponent.IPv4) ?
                Sockets.AddressFamily.InterNetwork :
                Sockets.AddressFamily.InterNetworkV6;

            foreach (UnixUnicastIPAddressInformation addr in _unicastAddresses)
            {
                if (addr.Address.AddressFamily == family)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The system's index for this network device.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// Returns a list of all Unicast addresses of the interface's IP Addresses.
        /// </summary>
        public List<UnixUnicastIPAddressInformation> UnicastAddress { get { return _unicastAddresses; } }

        /// <summary>
        /// Returns a list of all Multicast addresses of the interface's IP Addresses.
        /// </summary>
        public List<IPAddress>? MulticastAddresess { get { return _multicastAddresses; } }

        // Adds any IPAddress to this interface's List of addresses.
        protected void AddAddress(IPAddress ipAddress, int prefix)
        {
            if (IPAddressUtil.IsMulticast(ipAddress))
            {
                // Deferred initialization.
                _multicastAddresses ??= new List<IPAddress>();

                _multicastAddresses.Add(ipAddress);
            }
            else
            {
                _unicastAddresses.Add(new UnixUnicastIPAddressInformation(ipAddress, prefix));
            }
        }

        protected unsafe void ProcessIpv4Address(Interop.Sys.IpAddressInfo* addressInfo)
        {
            IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(addressInfo);
            AddAddress(ipAddress, addressInfo->PrefixLength);
            _index = addressInfo->InterfaceIndex;
        }

        protected unsafe void ProcessIpv6Address(Interop.Sys.IpAddressInfo* addressInfo, uint scopeId)
        {
            IPAddress address = IPAddressUtil.GetIPAddressFromNativeInfo(addressInfo);
            address.ScopeId = scopeId;
            AddAddress(address, addressInfo->PrefixLength);
            _ipv6ScopeId = scopeId;
            _index = addressInfo->InterfaceIndex;
        }

        protected unsafe void ProcessLinkLayerAddress(Interop.Sys.LinkLayerAddressInfo* llAddr)
        {
            byte[] macAddress = new byte[llAddr->NumAddressBytes];
            fixed (byte* macAddressPtr = macAddress)
            {
                Buffer.MemoryCopy(llAddr->AddressBytes, macAddressPtr, llAddr->NumAddressBytes, llAddr->NumAddressBytes);
            }
            PhysicalAddress physicalAddress = new PhysicalAddress(macAddress);

            _index = llAddr->InterfaceIndex;
            _physicalAddress = physicalAddress;
            _networkInterfaceType = (NetworkInterfaceType)llAddr->HardwareType;
        }
    }
}
