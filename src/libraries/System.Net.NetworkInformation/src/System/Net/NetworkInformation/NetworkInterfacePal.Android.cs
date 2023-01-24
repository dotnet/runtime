// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation
{
    internal static class NetworkInterfacePal
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces() => GetAndroidNetworkInterfaces();
        public static bool GetIsNetworkAvailable() => TransformNetworkInterfacess(IsSomeNetworkUp);
        public static int IPv6LoopbackInterfaceIndex => LoopbackInterfaceIndex;
        public static int LoopbackInterfaceIndex => TransformNetworkInterfacess(FindLoopbackInterfaceIndex);

        internal static unsafe AndroidNetworkInterface[] GetAndroidNetworkInterfaces()
            => TransformNetworkInterfacess(ToAndroidNetworkInterfaceArray);

        private static unsafe T TransformNetworkInterfacess<T>(Func<int, IntPtr, int, IntPtr, T> transform)
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
                return transform(interfaceCount, (IntPtr)networkInterfaceInfo, addressCount, (IntPtr)addressInfo);
            }
            finally
            {
                Marshal.FreeHGlobal(globalMemory);
            }
        }

        private static unsafe AndroidNetworkInterface[] ToAndroidNetworkInterfaceArray(int interfaceCount, IntPtr networkInterfacesPtr, int addressCount, IntPtr addressPtr)
        {
            var networkInterfaces = new AndroidNetworkInterface[interfaceCount];

            var networkInterfaceInfo = (Interop.Sys.NetworkInterfaceInfo*)networkInterfacesPtr;
            for (int i = 0; i < interfaceCount; i++, networkInterfaceInfo++)
            {
                var name = Marshal.PtrToStringUTF8((IntPtr)networkInterfaceInfo->Name);
                networkInterfaces[i] = new AndroidNetworkInterface(name!, networkInterfaceInfo);
            }

            var addressInfo = (Interop.Sys.IpAddressInfo*)addressPtr;
            for (int i = 0; i < addressCount; i++, addressInfo++)
            {
                // there is usually just a handful of few network interfaces on Android devices
                // and this linear search does not have any impact on performance
                foreach (var networkInterface in networkInterfaces)
                {
                    if (networkInterface.Index == addressInfo->InterfaceIndex)
                    {
                        networkInterface.AddAddress(addressInfo);
                        break;
                    }
                }
            }

            return networkInterfaces;
        }

        private static unsafe int FindLoopbackInterfaceIndex(int interfaceCount, IntPtr networkInterfacesPtr, int addressCount, IntPtr addressPtr)
        {
            var networkInterfaceInfo = (Interop.Sys.NetworkInterfaceInfo*)networkInterfacesPtr;
            for (int i = 0; i < interfaceCount; i++, networkInterfaceInfo++)
            {
                if (networkInterfaceInfo->HardwareType == (int)NetworkInterfaceType.Loopback)
                {
                    return networkInterfaceInfo->InterfaceIndex;
                }
            }

            throw new NetworkInformationException(SR.net_NoLoopback);
        }

        private static unsafe bool IsSomeNetworkUp(int interfaceCount, IntPtr networkInterfacesPtr, int addressCount, IntPtr addressPtr)
        {
            var networkInterfaceInfo = (Interop.Sys.NetworkInterfaceInfo*)networkInterfacesPtr;
            for (int i = 0; i < interfaceCount; i++, networkInterfaceInfo++)
            {
                if (networkInterfaceInfo->HardwareType == (int)NetworkInterfaceType.Loopback
                    || networkInterfaceInfo->HardwareType == (int)NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                if (networkInterfaceInfo->OperationalState == (int)OperationalStatus.Up)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
