// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    internal static class IPAddressExtensions
    {
        public static IPAddress Snapshot(this IPAddress original)
        {
            switch (original.AddressFamily)
            {
                case AddressFamily.InterNetwork:
#pragma warning disable CS0618 // IPAddress.Address is obsoleted, but it's the most efficient way to get the Int32 IPv4 address
                    return new IPAddress(original.Address);
#pragma warning restore CS0618

                case AddressFamily.InterNetworkV6:
                    return new IPAddress(original.GetAddressBytes(), (uint)original.ScopeId);
            }

            throw new InternalException();
        }

        public static long GetAddress(this IPAddress thisObj)
        {
            byte[] addressBytes = thisObj.GetAddressBytes();
            Debug.Assert(addressBytes.Length == 4);
            return (long)BitConverter.ToInt32(addressBytes, 0);
        }
    }
}
