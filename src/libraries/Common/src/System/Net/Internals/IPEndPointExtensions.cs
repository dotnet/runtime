// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    public static class IPEndPointExtensions
    {
        public static Internals.SocketAddress Serialize(EndPoint endpoint)
        {
            Debug.Assert(endpoint is IPEndPoint);

            return new Internals.SocketAddress(((IPEndPoint)endpoint).Address, ((IPEndPoint)endpoint).Port);
        }

        public static EndPoint Create(this EndPoint thisObj, Internals.SocketAddress socketAddress)
        {
            if (socketAddress.Family != thisObj.AddressFamily)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidAddressFamily, socketAddress.Family.ToString(), thisObj.GetType().FullName, thisObj.AddressFamily.ToString()), "socketAddress");
            }
            if (socketAddress.Size < 8)
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, thisObj.GetType().FullName), "socketAddress");
            }

            return socketAddress.GetIPEndPoint();
        }

        internal static IPEndPoint Snapshot(this IPEndPoint thisObj)
        {
            return new IPEndPoint(thisObj.Address.Snapshot(), thisObj.Port);
        }
    }
}
