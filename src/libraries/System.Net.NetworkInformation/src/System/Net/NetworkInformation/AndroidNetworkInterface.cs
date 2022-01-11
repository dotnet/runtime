// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Net;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Implements a NetworkInterface on Android.
    /// </summary>
    internal sealed class AndroidNetworkInterface : UnixNetworkInterface
    {
        private OperationalStatus _operationalStatus;
        private bool _supportsMulticast;
        private long _speed;
        internal int _mtu;
        private NetworkInterfaceType _interfaceType = NetworkInterfaceType.Unknown;
        private readonly AndroidIPInterfaceProperties _ipProperties;

        internal AndroidNetworkInterface(string name, int index) : base(name)
        {
            _index = index;
            _ipProperties = new AndroidIPInterfaceProperties(this);
        }

        public static unsafe NetworkInterface[] GetAndroidNetworkInterfaces()
        {
            int interfaceCount=0;
            int addressCount=0;
            Interop.Sys.NetworkInterfaceInfo * nii = null;
            Interop.Sys.IpAddressInfo * ai = null;
            IntPtr globalMemory = (IntPtr)null;

            if (Interop.Sys.GetNetworkInterfaces(&interfaceCount, &nii, &addressCount, &ai) != 0)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            globalMemory = (IntPtr)nii;
            try
            {
                NetworkInterface[] interfaces = new NetworkInterface[interfaceCount];
                Dictionary<int, AndroidNetworkInterface> interfacesByIndex = new Dictionary<int, AndroidNetworkInterface>(interfaceCount);

                for (int i = 0; i < interfaceCount; i++)
                {
                    var ani = new AndroidNetworkInterface(Marshal.PtrToStringAnsi((IntPtr)nii->Name)!, nii->InterfaceIndex);
                    ani._interfaceType = (NetworkInterfaceType)nii->HardwareType;
                    ani._operationalStatus = (OperationalStatus)nii->OperationalState;
                    ani._mtu = nii->Mtu;
                    ani._speed = nii->Speed;
                    ani._supportsMulticast = nii->SupportsMulticast != 0;

                    if (nii->NumAddressBytes > 0)
                    {
                        ani._physicalAddress = new PhysicalAddress(new ReadOnlySpan<byte>(nii->AddressBytes, nii->NumAddressBytes).ToArray());
                    }

                    interfaces[i] = ani;
                    interfacesByIndex.Add(nii->InterfaceIndex, ani);
                    nii++;
                }

                while (addressCount != 0)
                {
                    var address = new IPAddress(new ReadOnlySpan<byte>(ai->AddressBytes, ai->NumAddressBytes));
                    if (address.IsIPv6LinkLocal)
                    {
                        address.ScopeId = ai->InterfaceIndex;
                    }

                    if (interfacesByIndex.TryGetValue(ai->InterfaceIndex, out AndroidNetworkInterface? ani))
                    {
                        ani.AddAddress(address, ai->PrefixLength);
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

        public override bool SupportsMulticast => _supportsMulticast;

        public override IPInterfaceProperties GetIPProperties() => _ipProperties;

        public override IPInterfaceStatistics GetIPStatistics() => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

        public override NetworkInterfaceType NetworkInterfaceType { get { return _interfaceType; } }

        public override long Speed => _speed;

        public override bool IsReceiveOnly { get { return false; } }
    }
}
