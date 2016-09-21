// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using RTConnectivity = Windows.Networking.Connectivity;

namespace System.Net.NetworkInformation
{
    internal class NetNativeNetworkInterface : NetworkInterface
    {
        public override string Description
        { 
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override string Id
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public static new int IPv6LoopbackInterfaceIndex
        { 
            get
            { 
                throw new PlatformNotSupportedException();
            }
        }

        public override bool IsReceiveOnly
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public static new int LoopbackInterfaceIndex
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override string Name
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override NetworkInterfaceType NetworkInterfaceType
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override OperationalStatus OperationalStatus
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override long Speed
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public override bool SupportsMulticast
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        public static new NetworkInterface[] GetAllNetworkInterfaces()
        {
            throw new PlatformNotSupportedException();
        }

        public override IPInterfaceProperties GetIPProperties()
        {
            throw NotImplemented.ByDesign;
        }

        public override IPInterfaceStatistics GetIPStatistics()
        {
            throw NotImplemented.ByDesign;
        }

        public override IPv4InterfaceStatistics GetIPv4Statistics()
        {
            throw NotImplemented.ByDesign;
        }

        public override PhysicalAddress GetPhysicalAddress()
        {
            throw NotImplemented.ByDesign;
        }

        public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            throw NotImplemented.ByDesign;
        }
        
        internal static NetworkInterface[] GetNetworkInterfaces()
        {
            throw NotImplemented.ByDesign;
        }
        
        internal static bool InternalGetIsNetworkAvailable()
        {
            RTConnectivity.ConnectionProfile connectionProfile = RTConnectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectionProfile == null)
            {
                return false;
            }

            RTConnectivity.NetworkConnectivityLevel level = connectionProfile.GetNetworkConnectivityLevel();
            switch (level)
            {
                case RTConnectivity.NetworkConnectivityLevel.InternetAccess:
                case RTConnectivity.NetworkConnectivityLevel.ConstrainedInternetAccess:
                case RTConnectivity.NetworkConnectivityLevel.LocalAccess:
                    return true;

                default:
                    return false;
            }
        }
        
        internal static int InternalLoopbackInterfaceIndex
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }

        internal static int InternalIPv6LoopbackInterfaceIndex
        {
            get
            {
                throw NotImplemented.ByDesign;
            }
        }
    }
}
