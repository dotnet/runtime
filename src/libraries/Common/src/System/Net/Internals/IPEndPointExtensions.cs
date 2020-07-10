// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Sockets
{
    internal static class IPEndPointExtensions
    {
        public static Internals.SocketAddress Serialize(EndPoint endpoint)
        {
            Debug.Assert(!(endpoint is DnsEndPoint));

            var ipEndPoint = endpoint as IPEndPoint;
            if (ipEndPoint != null)
            {
                return new Internals.SocketAddress(ipEndPoint.Address, ipEndPoint.Port);
            }

            System.Net.SocketAddress address = endpoint.Serialize();
            return GetInternalSocketAddress(address);
        }

        public static EndPoint Create(this EndPoint thisObj, Internals.SocketAddress socketAddress)
        {
            AddressFamily family = socketAddress.Family;
            if (family != thisObj.AddressFamily)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidAddressFamily, family.ToString(), thisObj.GetType().FullName, thisObj.AddressFamily.ToString()), nameof(socketAddress));
            }

            if (family == AddressFamily.InterNetwork || family == AddressFamily.InterNetworkV6)
            {
                if (socketAddress.Size < 8)
                {
                    throw new ArgumentException(SR.Format(SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, thisObj.GetType().FullName), nameof(socketAddress));
                }

                return socketAddress.GetIPEndPoint();
            }
            else if (family == AddressFamily.Unknown)
            {
                return thisObj;
            }

            System.Net.SocketAddress address = GetNetSocketAddress(socketAddress);
            return thisObj.Create(address);
        }

        private static Internals.SocketAddress GetInternalSocketAddress(System.Net.SocketAddress address)
        {
            var result = new Internals.SocketAddress(address.Family, address.Size);
            for (int index = 0; index < address.Size; index++)
            {
                result[index] = address[index];
            }

            return result;
        }

        internal static System.Net.SocketAddress GetNetSocketAddress(Internals.SocketAddress address)
        {
            var result = new System.Net.SocketAddress(address.Family, address.Size);
            for (int index = 0; index < address.Size; index++)
            {
                result[index] = address[index];
            }

            return result;
        }
    }
}
