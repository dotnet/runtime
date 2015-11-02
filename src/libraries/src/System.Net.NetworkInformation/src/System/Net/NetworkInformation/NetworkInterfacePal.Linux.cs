// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            return LinuxNetworkInterface.GetLinuxNetworkInterfaces();
        }

        public static bool GetIsNetworkAvailable()
        {
            foreach (var ni in GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    return true;
                }
            }

            return false;
        }

        public static int IPv6LoopbackInterfaceIndex { get { return LoopbackInterfaceIndex; } }

        public static int LoopbackInterfaceIndex
        {
            get
            {
                NetworkInterface[] interfaces = LinuxNetworkInterface.GetLinuxNetworkInterfaces();
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
