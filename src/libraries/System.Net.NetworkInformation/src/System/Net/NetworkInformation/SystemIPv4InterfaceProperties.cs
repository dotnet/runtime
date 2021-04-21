// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation
{
    internal sealed class SystemIPv4InterfaceProperties : IPv4InterfaceProperties
    {
        // These are only valid for ipv4 interfaces.
        private readonly bool _haveWins;
        private readonly bool _dhcpEnabled;
        private readonly bool _routingEnabled;
        private readonly uint _index;
        private readonly uint _mtu;
        private bool _autoConfigEnabled;
        private bool _autoConfigActive;

        internal SystemIPv4InterfaceProperties(Interop.IpHlpApi.FIXED_INFO fixedInfo, Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
        {
            _index = ipAdapterAddresses.index;
            _routingEnabled = fixedInfo.enableRouting;
            _dhcpEnabled = ((ipAdapterAddresses.flags & Interop.IpHlpApi.AdapterFlags.DhcpEnabled) != 0);
            _haveWins = (ipAdapterAddresses.firstWinsServerAddress != IntPtr.Zero);

            _mtu = ipAdapterAddresses.mtu;

            GetPerAdapterInfo(ipAdapterAddresses.index);
        }

        /// Only valid for Ipv4 Uses WINS for name resolution.
        public override bool UsesWins { get { return _haveWins; } }

        public override bool IsDhcpEnabled
        {
            get { return _dhcpEnabled; }
        }

        public override bool IsForwardingEnabled { get { return _routingEnabled; } }

        /// Auto configuration of an ipv4 address for a client on a network where a DHCP server isn't available.
        public override bool IsAutomaticPrivateAddressingEnabled
        {
            get
            {
                return _autoConfigEnabled;
            }
        }

        public override bool IsAutomaticPrivateAddressingActive
        {
            get
            {
                return _autoConfigActive;
            }
        }

        // Specifies the Maximum transmission unit in bytes. Uses GetIFEntry.
        // We cache this to be consistent across all platforms.
        public override int Mtu
        {
            get
            {
                return unchecked((int)_mtu);
            }
        }

        public override int Index
        {
            get
            {
                return (int)_index;
            }
        }

        private void GetPerAdapterInfo(uint index)
        {
            if (index != 0)
            {
                uint size = 0;

                uint result = Interop.IpHlpApi.GetPerAdapterInfo(index, IntPtr.Zero, ref size);
                while (result == Interop.IpHlpApi.ERROR_BUFFER_OVERFLOW)
                {
                    // Now we allocate the buffer and read the network parameters.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetPerAdapterInfo(index, buffer, ref size);
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            Interop.IpHlpApi.IpPerAdapterInfo ipPerAdapterInfo =
                                Marshal.PtrToStructure<Interop.IpHlpApi.IpPerAdapterInfo>(buffer);

                            _autoConfigEnabled = ipPerAdapterInfo.autoconfigEnabled;
                            _autoConfigActive = ipPerAdapterInfo.autoconfigActive;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                if (result != Interop.IpHlpApi.ERROR_SUCCESS)
                {
                    throw new NetworkInformationException((int)result);
                }
            }
        }
    }
}
