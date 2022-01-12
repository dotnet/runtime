// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces() => GetAndroidNetworkInterfaces();

        public static bool GetIsNetworkAvailable()
        {
            foreach (var ni in GetAndroidNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    return true;
                }
            }

            return false;
        }

        public static int IPv6LoopbackInterfaceIndex => LoopbackInterfaceIndex;

        public static int LoopbackInterfaceIndex
        {
            get
            {
                foreach (var networkInterface in GetAndroidNetworkInterfaces())
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        return networkInterface.Index;
                    }
                }

                throw new NetworkInformationException(SR.net_NoLoopback);
            }
        }

        internal static unsafe AndroidNetworkInterface[] GetAndroidNetworkInterfaces()
        {
            int interfaceCount = 0;
            int addressCount = 0;
            Interop.Sys.NetworkInterfaceInfo *networkInterfaceInfo = null;
            Interop.Sys.IpAddressInfo *addressInfo = null;

            if (Interop.Sys.GetNetworkInterfaces(&interfaceCount, &networkInterfaceInfo, &addressCount, &addressInfo) != 0)
            {
                string message = Interop.Sys.GetLastErrorInfo().GetErrorMessage();
                throw new NetworkInformationException(message);
            }

            // the native implementation of Interop.Sys.GetNetworkInterfaces allocates one block of memory
            // for both networkInterfaceInfo and addressInfo so we only need to call free once pointing at
            // the start of the network itnerfaces list
            var globalMemory = (IntPtr)networkInterfaceInfo;

            try
            {
                var networkInterfaces = new AndroidNetworkInterface[interfaceCount];
                var interfacesByIndex = new Dictionary<int, AndroidNetworkInterface>(interfaceCount);

                for (int i = 0; i < interfaceCount; i++, networkInterfaceInfo++)
                {
                    var name = Marshal.PtrToStringAnsi((IntPtr)networkInterfaceInfo->Name);
                    var networkInterface = new AndroidNetworkInterface(name!, networkInterfaceInfo);
                    networkInterfaces[i] = interfacesByIndex[networkInterface.Index] = networkInterface;
                }

                for (int i = 0; i < addressCount; i++, addressInfo++)
                {
                    if (interfacesByIndex.TryGetValue(addressInfo->InterfaceIndex, out var networkInterface))
                    {
                        networkInterface.AddAddress(addressInfo);
                    }
                }

                return networkInterfaces;
            }
            finally
            {
                Marshal.FreeHGlobal(globalMemory);
            }
        }
    }
}
