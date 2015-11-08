// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            throw new PlatformNotSupportedException();
        }

        public static bool GetIsNetworkAvailable()
        {
            throw new PlatformNotSupportedException();
        }

        public static int IPv6LoopbackInterfaceIndex
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public static int LoopbackInterfaceIndex
        {
            get { throw new PlatformNotSupportedException(); }
        }
    }
}
