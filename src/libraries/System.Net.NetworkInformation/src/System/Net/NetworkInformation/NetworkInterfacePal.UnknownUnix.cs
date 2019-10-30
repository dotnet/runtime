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
