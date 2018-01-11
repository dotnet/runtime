// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixNetworkInterface : NetworkInterface
    {
        protected string _name;
        protected int _index = -1;
        protected NetworkInterfaceType _networkInterfaceType = NetworkInterfaceType.Unknown;
        protected PhysicalAddress _physicalAddress = PhysicalAddress.None;
        protected List<IPAddress> _addresses = new List<IPAddress>();
        protected Dictionary<IPAddress, IPAddress> _netMasks = new Dictionary<IPAddress, IPAddress>();
        // If this is an ipv6 device, contains the Scope ID.
        protected uint? _ipv6ScopeId = null;

        protected UnixNetworkInterface(string name)
        {
            _name = name;
        }

        public sealed override string Id { get { return _name; } }

        public sealed override string Name { get { return _name; } }

        public sealed override string Description { get { return _name; } }

        public sealed override NetworkInterfaceType NetworkInterfaceType { get { return _networkInterfaceType; } }

        public sealed override PhysicalAddress GetPhysicalAddress() { return _physicalAddress; }

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            Sockets.AddressFamily family =
                (networkInterfaceComponent == NetworkInterfaceComponent.IPv4)
                ? Sockets.AddressFamily.InterNetwork
                : Sockets.AddressFamily.InterNetworkV6;

            return _addresses.Any(addr => addr.AddressFamily == family);
        }

        /// <summary>
        /// The system's index for this network device.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// Returns a list of all of the interface's IP Addresses.
        /// </summary>
        public List<IPAddress> Addresses { get { return _addresses; } }

        // Adds any IPAddress to this interface's List of addresses.
        protected void AddAddress(IPAddress ipAddress)
        {
            _addresses.Add(ipAddress);
        }

        public IPAddress GetNetMaskForIPv4Address(IPAddress address)
        {
            Debug.Assert(address.AddressFamily == Sockets.AddressFamily.InterNetwork);
            return _netMasks[address];
        }

        protected unsafe void ProcessIpv4Address(Interop.Sys.IpAddressInfo* addressInfo, Interop.Sys.IpAddressInfo* netMask)
        {
            IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(addressInfo);
            IPAddress netMaskAddress = IPAddressUtil.GetIPAddressFromNativeInfo(netMask);
            AddAddress(ipAddress);
            _netMasks[ipAddress] = netMaskAddress;
            _index = addressInfo->InterfaceIndex;
        }

        protected unsafe void ProcessIpv6Address(Interop.Sys.IpAddressInfo* addressInfo, uint scopeId)
        {
            IPAddress address = IPAddressUtil.GetIPAddressFromNativeInfo(addressInfo);
            address.ScopeId = scopeId;
            AddAddress(address);
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
