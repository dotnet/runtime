// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Similar to the BSD and Linux code in use of getifaddrs()
// Everything else is different (properties and statistics via kstats)

// TODO: Future enhancements via kstat API:
// - Link speed lookup (currently returns value from getifaddrs, may be 0)
// - Per-interface statistics (packets, bytes, errors) for GetIPStatistics()/GetIPv4Statistics()
// - System-wide properties (routing tables, global interface info)

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Implements a NetworkInterface on SunOS.
    /// </summary>
    internal sealed class SunOSNetworkInterface : UnixNetworkInterface
    {
        private OperationalStatus _operationalStatus;
        private bool _supportsMulticast;
        private long _speed;
        internal int _mtu;
        private NetworkInterfaceType _interfaceType = NetworkInterfaceType.Unknown;
        private readonly SunOSIPInterfaceProperties _ipProperties;

        // Linux has class LinuxNetworkInterfaceSystemProperties here,
        // and those are added as an arg to the constructor below.
        // We may do similarly later, but using kstats.

        internal SunOSNetworkInterface(string name, int index) : base(name)
        {
            _index = index;
            _ipProperties = new SunOSIPInterfaceProperties(this);
        }

        internal static unsafe SunOSNetworkInterface[] GetSunOSNetworkInterfaces()
        {
            // Here Linux gets additional properties and passes to the constructor below.

            int interfaceCount = 0;
            int addressCount = 0;
            Interop.Sys.NetworkInterfaceInfo* nii = null;
            Interop.Sys.IpAddressInfo* ai = null;
            IntPtr globalMemory = (IntPtr)null;

            if (Interop.Sys.GetNetworkInterfaces(&interfaceCount, &nii, &addressCount, &ai) != 0)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            globalMemory = (IntPtr)nii;
            try
            {
                SunOSNetworkInterface[] interfaces = new SunOSNetworkInterface[interfaceCount];
                Dictionary<int, SunOSNetworkInterface> interfacesByIndex = new Dictionary<int, SunOSNetworkInterface>(interfaceCount);

                for (int i = 0; i < interfaceCount; i++)
                {
                    var lni = new SunOSNetworkInterface(Utf8StringMarshaller.ConvertToManaged(nii->Name)!, nii->InterfaceIndex);
                    lni._interfaceType = (NetworkInterfaceType)nii->HardwareType;
                    lni._speed = nii->Speed; // Note: not filled in (-1)
                    lni._operationalStatus = (OperationalStatus)nii->OperationalState;
                    lni._mtu = nii->Mtu;
                    lni._supportsMulticast = nii->SupportsMulticast != 0;

                    if (nii->NumAddressBytes > 0)
                    {
                        lni._physicalAddress = new PhysicalAddress(new ReadOnlySpan<byte>(nii->AddressBytes, nii->NumAddressBytes).ToArray());
                    }

                    interfaces[i] = lni;
                    interfacesByIndex.Add(nii->InterfaceIndex, lni);
                    nii++;
                }

                while (addressCount != 0)
                {
                    var address = new IPAddress(new ReadOnlySpan<byte>(ai->AddressBytes, ai->NumAddressBytes));
                    if (address.IsIPv6LinkLocal)
                    {
                        address.ScopeId = ai->InterfaceIndex;
                    }

                    if (interfacesByIndex.TryGetValue(ai->InterfaceIndex, out SunOSNetworkInterface? lni))
                    {
                        lni.AddAddress(address, ai->PrefixLength);
                    }

                    ai++;
                    addressCount--;
                }

                return interfaces;
            }
            finally
            {
                Marshal.FreeHGlobal(globalMemory);
            }
        }

        public override bool SupportsMulticast
        {
            get
            {
                return _supportsMulticast;
            }
        }

        public override IPInterfaceProperties GetIPProperties()
        {
            return _ipProperties;
        }

        // Future: implement via kstats
        public override IPInterfaceStatistics GetIPStatistics()
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        // Future: implement via kstats
        public override IPv4InterfaceStatistics GetIPv4Statistics()
            => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

        public override NetworkInterfaceType NetworkInterfaceType { get { return _interfaceType; } }

        // Future: implement via kstats
        // Return zero when speed is unknown.
        public override long Speed
        {
            get
            {
                return _speed < 0 ? 0 : _speed;
            }
        }

        public override bool IsReceiveOnly { get { return false; } }
    }
}
