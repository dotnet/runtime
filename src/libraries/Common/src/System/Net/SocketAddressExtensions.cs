// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;

namespace System.Net.Sockets
{
    internal static partial class SocketAddressExtensions
    {
        public static IPAddress GetIPAddress(this SocketAddress socketAddress) => IPEndPointExtensions.GetIPAddress(socketAddress.Buffer.Span);
        public static int GetPort(this SocketAddress socketAddress)
        {
            Debug.Assert(socketAddress.Family == AddressFamily.InterNetwork || socketAddress.Family == AddressFamily.InterNetworkV6);
            return (int)SocketAddressPal.GetPort(socketAddress.Buffer.Span);
        }

        public static IPEndPoint GetIPEndPoint(this SocketAddress socketAddress)
        {
            return new IPEndPoint(socketAddress.GetIPAddress(), socketAddress.GetPort());
        }

        public static bool Equals(this SocketAddress socketAddress, EndPoint? endPoint)
        {
            if (socketAddress.Family == endPoint?.AddressFamily && endPoint is IPEndPoint ipe)
            {
                return ipe.Equals(socketAddress.Buffer.Span);
            }

            // We could serialize other EndPoints and compare socket addresses.
            // But that would do two allocations and is probably as expensive as
            // allocating new EndPoint.
            // This may change if https://github.com/dotnet/runtime/issues/78993 is done
            return false;
        }
    }
}
