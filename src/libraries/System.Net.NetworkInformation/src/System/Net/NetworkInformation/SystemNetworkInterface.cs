// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation
{
    internal sealed class SystemNetworkInterface : NetworkInterface
    {
        private readonly string _name;
        private readonly string _id;
        private readonly string _description;
        private readonly byte[] _physicalAddress;
        private readonly NetworkInterfaceType _type;
        private readonly OperationalStatus _operStatus;
        private readonly long _speed;

        // Any interface can have two completely different valid indexes for ipv4 and ipv6.
        private readonly uint _index;
        private readonly uint _ipv6Index;
        private readonly Interop.IpHlpApi.AdapterFlags _adapterFlags;
        private readonly SystemIPInterfaceProperties _interfaceProperties;

        internal static int InternalLoopbackInterfaceIndex
        {
            get
            {
                return GetBestInterfaceForAddress(IPAddress.Loopback);
            }
        }

        internal static int InternalIPv6LoopbackInterfaceIndex
        {
            get
            {
                return GetBestInterfaceForAddress(IPAddress.IPv6Loopback);
            }
        }

        private static unsafe int GetBestInterfaceForAddress(IPAddress addr)
        {
            int index;
            Span<byte> buffer = stackalloc byte[SocketAddressPal.IPv6AddressSize];
            IPEndPointExtensions.SetIPAddress(buffer, addr);

            int error = (int)Interop.IpHlpApi.GetBestInterfaceEx(buffer, &index);
            if (error != 0)
            {
                throw new NetworkInformationException(error);
            }

            return index;
        }

        internal static bool InternalGetIsNetworkAvailable()
        {
            try
            {
                NetworkInterface[] networkInterfaces = GetNetworkInterfaces();
                foreach (NetworkInterface netInterface in networkInterfaces)
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up && netInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                        && netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        return true;
                    }
                }
            }
            catch (NetworkInformationException nie)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nie);
            }

            return false;
        }

        internal static unsafe NetworkInterface[] GetNetworkInterfaces()
        {
            uint bufferSize = 0;

            List<SystemNetworkInterface> interfaceList = new List<SystemNetworkInterface>();

            // GetAdaptersAddresses without GAA_FLAG_INCLUDE_ALL_INTERFACES returns only real
            // network adapters — roughly the same set shown by ipconfig and ncpa.cpl, and the
            // same set .NET 8 returned. Starting with .NET 9, GAA_FLAG_INCLUDE_ALL_INTERFACES
            // was added, which also surfaces NDIS filter modules (WFP, QoS, Hyper-V extension
            // filters) that have no IP stack and are not shown by any standard Windows tool.
            // Those extra entries all report OperationalStatus.Up, which causes
            // GetIsNetworkAvailable() to return true even when no real network is present.
            //
            // Collect the adapter GUIDs from the non-IncludeAllInterfaces call (the
            // "ipconfig-equivalent" set). Any adapter absent from that set but reporting Up
            // is a filter module; mark it Unknown so callers can restore the original behavior:
            //   ni.OperationalStatus != OperationalStatus.Unknown
            HashSet<string> legacyGuids = GetLegacyAdapterGuids();

            // Full list including all NDIS interfaces.
            Interop.IpHlpApi.GetAdaptersAddressesFlags flags =
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways |
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins |
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeAllInterfaces;

            // Figure out the right buffer size for the adapter information.
            uint result = Interop.IpHlpApi.GetAdaptersAddresses(
                AddressFamily.Unspecified, (uint)flags, IntPtr.Zero, IntPtr.Zero, &bufferSize);

            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {
                // Allocate the buffer and get the adapter info.
                IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                try
                {
                    result = Interop.IpHlpApi.GetAdaptersAddresses(
                        AddressFamily.Unspecified, (uint)flags, IntPtr.Zero, buffer, &bufferSize);

                    // If succeeded, we're going to add each new interface.
                    if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                    {
                        // Linked list of interfaces.
                        Interop.IpHlpApi.IpAdapterAddresses* adapterAddresses = (Interop.IpHlpApi.IpAdapterAddresses*)buffer;
                        while (adapterAddresses != null)
                        {
                            // Traverse the list, marshal in the native structures, and create new NetworkInterfaces.
                            interfaceList.Add(new SystemNetworkInterface(in *adapterAddresses, legacyGuids));
                            adapterAddresses = adapterAddresses->next;
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            // If we don't have any interfaces detected, return empty.
            if (result == Interop.IpHlpApi.ERROR_NO_DATA || result == Interop.IpHlpApi.ERROR_INVALID_PARAMETER)
            {
                return Array.Empty<SystemNetworkInterface>();
            }

            // Otherwise we throw on an error.
            if (result != Interop.IpHlpApi.ERROR_SUCCESS)
            {
                throw new NetworkInformationException((int)result);
            }

            return interfaceList.ToArray();
        }

        // Calls GetAdaptersAddresses without GAA_FLAG_INCLUDE_ALL_INTERFACES and returns
        // the set of adapter GUIDs (AdapterName) it reports — the same adapters visible
        // in ipconfig and ncpa.cpl.  NDIS filter modules are absent from this set.
        private static unsafe HashSet<string> GetLegacyAdapterGuids()
        {
            Interop.IpHlpApi.GetAdaptersAddressesFlags legacyFlags =
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways |
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins;

            uint bufferSize = 0;
            uint result = Interop.IpHlpApi.GetAdaptersAddresses(
                AddressFamily.Unspecified, (uint)legacyFlags, IntPtr.Zero, IntPtr.Zero, &bufferSize);

            HashSet<string> guids = new HashSet<string>();
            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {
                IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                try
                {
                    result = Interop.IpHlpApi.GetAdaptersAddresses(
                        AddressFamily.Unspecified, (uint)legacyFlags, IntPtr.Zero, buffer, &bufferSize);

                    if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                    {
                        Interop.IpHlpApi.IpAdapterAddresses* addr = (Interop.IpHlpApi.IpAdapterAddresses*)buffer;
                        while (addr != null)
                        {
                            guids.Add(addr->AdapterName);
                            addr = addr->next;
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return guids;
        }

        internal SystemNetworkInterface(in Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses, HashSet<string> legacyGuids)
        {
            // Store the common API information.
            _id = ipAdapterAddresses.AdapterName;
            _name = ipAdapterAddresses.FriendlyName;
            _description = ipAdapterAddresses.Description;
            _index = ipAdapterAddresses.index;

            _physicalAddress = ipAdapterAddresses.Address;

            _type = ipAdapterAddresses.type;
            // Interfaces absent from the ipconfig-equivalent set (see GetLegacyAdapterGuids) are
            // NDIS filter modules or adapters with no IP stack — mark them Unknown so callers can
            // restore ipconfig-equivalent behavior: ni.OperationalStatus != OperationalStatus.Unknown.
            _operStatus = legacyGuids.Contains(_id) ? ipAdapterAddresses.operStatus : OperationalStatus.Unknown;
            _speed = unchecked((long)ipAdapterAddresses.receiveLinkSpeed);

            // API specific info.
            _ipv6Index = ipAdapterAddresses.ipv6Index;

            _adapterFlags = ipAdapterAddresses.flags;
            _interfaceProperties = new SystemIPInterfaceProperties(ipAdapterAddresses);
        }

        public override string Id { get { return _id; } }

        public override string Name { get { return _name; } }

        public override string Description { get { return _description; } }

        public override PhysicalAddress GetPhysicalAddress()
        {
            return new PhysicalAddress(_physicalAddress);
        }

        public override NetworkInterfaceType NetworkInterfaceType { get { return _type; } }

        public override IPInterfaceProperties GetIPProperties()
        {
            return _interfaceProperties;
        }

        public override IPv4InterfaceStatistics GetIPv4Statistics()
        {
            return new SystemIPv4InterfaceStatistics(_index);
        }

        public override IPInterfaceStatistics GetIPStatistics()
        {
            return new SystemIPInterfaceStatistics(_index);
        }

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            if (networkInterfaceComponent == NetworkInterfaceComponent.IPv6
                && ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0))
            {
                return true;
            }

            if (networkInterfaceComponent == NetworkInterfaceComponent.IPv4
                && ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0))
            {
                return true;
            }

            return false;
        }

        // We cache this to be consistent across all platforms.
        public override OperationalStatus OperationalStatus
        {
            get
            {
                return _operStatus;
            }
        }

        public override long Speed
        {
            get
            {
                return _speed;
            }
        }

        public override bool IsReceiveOnly
        {
            get
            {
                return ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.ReceiveOnly) > 0);
            }
        }

        /// <summary>The interface doesn't allow multicast.</summary>
        public override bool SupportsMulticast
        {
            get
            {
                return ((_adapterFlags & Interop.IpHlpApi.AdapterFlags.NoMulticast) == 0);
            }
        }
    }
}
