// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            return NetNativeNetworkInterface.GetNetworkInterfaces();
        }

        public static bool GetIsNetworkAvailable()
        {
            return NetNativeNetworkInterface.InternalGetIsNetworkAvailable();
        }

        public static int LoopbackInterfaceIndex
        {
            get
            {
                return NetNativeNetworkInterface.InternalLoopbackInterfaceIndex;
            }
        }

        public static int IPv6LoopbackInterfaceIndex
        {
            get
            {
                return NetNativeNetworkInterface.InternalIPv6LoopbackInterfaceIndex;
            }
        }
    }
}
