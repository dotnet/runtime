// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
            Internals.SocketAddress address = new Internals.SocketAddress(addr);
            fixed (byte* buffer = address.Buffer)
            {
                int error = (int)Interop.IpHlpApi.GetBestInterfaceEx(buffer, &index);
                if (error != 0)
                {
                    throw new NetworkInformationException(error);
                }
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
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, nie);
            }

            return false;
        }

        internal static unsafe NetworkInterface[] GetNetworkInterfaces()
        {
            AddressFamily family = AddressFamily.Unspecified;
            uint bufferSize = 0;

            List<SystemNetworkInterface> interfaceList = new List<SystemNetworkInterface>();

            Interop.IpHlpApi.GetAdaptersAddressesFlags flags =
                Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways
                | Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins;

            // Figure out the right buffer size for the adapter information.
            uint result = Interop.IpHlpApi.GetAdaptersAddresses(
                family, (uint)flags, IntPtr.Zero, IntPtr.Zero, &bufferSize);

            while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
            {

                // Allocate the buffer and get the adapter info.
                IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                try
                {
                    result = Interop.IpHlpApi.GetAdaptersAddresses(
                        family, (uint)flags, IntPtr.Zero, buffer, &bufferSize);

                    // If succeeded, we're going to add each new interface.
                    if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                    {
                        // Linked list of interfaces.
                        Interop.IpHlpApi.IpAdapterAddresses* adapterAddresses = (Interop.IpHlpApi.IpAdapterAddresses*)buffer;
                        while (adapterAddresses != null)
                        {
                            // Traverse the list, marshal in the native structures, and create new NetworkInterfaces.
                            interfaceList.Add(new SystemNetworkInterface(in *adapterAddresses));
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

        internal SystemNetworkInterface(in Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
        {
            // Store the common API information.
            _id = ipAdapterAddresses.AdapterName;
            _name = ipAdapterAddresses.FriendlyName;
            _description = ipAdapterAddresses.Description;
            _index = ipAdapterAddresses.index;

            _physicalAddress = ipAdapterAddresses.Address;

            _type = ipAdapterAddresses.type;
            _operStatus = ipAdapterAddresses.operStatus;
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
