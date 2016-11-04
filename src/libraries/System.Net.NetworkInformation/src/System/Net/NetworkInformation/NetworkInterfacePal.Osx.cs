// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            return OsxNetworkInterface.GetOsxNetworkInterfaces();
        }

        public static bool GetIsNetworkAvailable()
        {
            foreach (var ni in GetAllNetworkInterfaces())
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

        public static int IPv6LoopbackInterfaceIndex
        {
            get
            {
                return LoopbackInterfaceIndex;
            }
        }

        public static int LoopbackInterfaceIndex
        {
            get
            {
                var interfaces = OsxNetworkInterface.GetOsxNetworkInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i].NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        return ((UnixNetworkInterface)interfaces[i]).Index;
                    }
                }

                throw new NetworkInformationException(SR.net_NoLoopback);
            }
        }
    }
}
