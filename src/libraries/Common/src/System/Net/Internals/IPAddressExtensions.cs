// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Sockets
{
    public static class IPAddressExtensions
    {
        public static IPAddress Snapshot(this IPAddress original)
        {
            switch (original.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return new IPAddress(original.GetAddressBytes());

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
