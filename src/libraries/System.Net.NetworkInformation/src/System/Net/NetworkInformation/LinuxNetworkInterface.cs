// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Implements a NetworkInterface on Linux.
    /// </summary>
    internal sealed class LinuxNetworkInterface : UnixNetworkInterface
    {
        private OperationalStatus _operationalStatus;
        private bool _supportsMulticast;
        private long _speed;
        internal int _mtu;
        private NetworkInterfaceType _interfaceType = NetworkInterfaceType.Unknown;
        private readonly LinuxIPInterfaceProperties _ipProperties;

        internal sealed class LinuxNetworkInterfaceSystemProperties
        {
            internal string[]? IPv4Routes;
            internal string[]? IPv6Routes;
            internal string? DnsSuffix;
            internal IPAddressCollection? DnsAddresses;

            internal LinuxNetworkInterfaceSystemProperties()
            {
                if (File.Exists(NetworkFiles.Ipv4RouteFile))
                {
                    try
                    {
                        IPv4Routes = File.ReadAllLines(NetworkFiles.Ipv4RouteFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                if (File.Exists(NetworkFiles.Ipv6RouteFile))
                {
                    try
                    {
                        IPv6Routes = File.ReadAllLines(NetworkFiles.Ipv6RouteFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                try
                {
                    string resolverConfig = File.ReadAllText(NetworkFiles.EtcResolvConfFile);
                    DnsSuffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(resolverConfig);
                    DnsAddresses = new InternalIPAddressCollection(StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(resolverConfig));
                }
                catch (Exception e) when (e is FileNotFoundException || e is UnauthorizedAccessException)
                {
                }
            }
        }

        internal LinuxNetworkInterface(string name, int index, LinuxNetworkInterfaceSystemProperties systemProperties) : base(name)
        {
            _index = index;
            _ipProperties = new LinuxIPInterfaceProperties(this, systemProperties);
        }

        public static unsafe NetworkInterface[] GetLinuxNetworkInterfaces()
        {
            var systemProperties = new LinuxNetworkInterfaceSystemProperties();

            int interfaceCount=0;
            int addressCount=0;
            Interop.Sys.NetworkInterfaceInfo * nii = null;
            Interop.Sys.IpAddressInfo * ai = null;
            IntPtr globalMemory = (IntPtr)null;

            if (Interop.Sys.GetNetworkInterfaces(ref interfaceCount, ref nii, ref addressCount, ref ai) != 0)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            globalMemory = (IntPtr)nii;
            try
            {
                NetworkInterface[] interfaces = new NetworkInterface[interfaceCount];
                Dictionary<int, LinuxNetworkInterface> interfacesByIndex = new Dictionary<int, LinuxNetworkInterface>(interfaceCount);

                for (int i = 0; i < interfaceCount; i++)
                {
                    var lni = new LinuxNetworkInterface(Marshal.PtrToStringAnsi((IntPtr)nii->Name)!, nii->InterfaceIndex, systemProperties);
                    lni._interfaceType = (NetworkInterfaceType)nii->HardwareType;
                    lni._speed = nii->Speed;
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

                    if (interfacesByIndex.TryGetValue(ai->InterfaceIndex, out LinuxNetworkInterface? lni))
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

        public override IPInterfaceStatistics GetIPStatistics()
        {
            return new LinuxIPInterfaceStatistics(_name);
        }

        public override IPv4InterfaceStatistics GetIPv4Statistics()
        {
            return new LinuxIPv4InterfaceStatistics(_name);
        }

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

        public override NetworkInterfaceType NetworkInterfaceType { get { return _interfaceType; } }

        public override long Speed
        {
            get
            {
                return _speed;
            }
        }

        public override bool IsReceiveOnly { get { return false; } }
    }
}
