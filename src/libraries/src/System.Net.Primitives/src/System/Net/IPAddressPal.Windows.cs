// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal static class IPAddressPal
    {
        public const uint SuccessErrorCode = Interop.StatusOptions.STATUS_SUCCESS;

        public static uint Ipv4AddressToString(byte[] address, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParser.IPv4AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET_ADDRSTRLEN);

            uint length = (uint)buffer.Capacity;
            return Interop.NtDll.RtlIpv4AddressToStringExW(address, 0, buffer, ref length);
        }

        public static uint Ipv6AddressToString(byte[] address, uint scopeId, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParser.IPv6AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET6_ADDRSTRLEN);

            uint length = (uint)buffer.Capacity;
            return Interop.NtDll.RtlIpv6AddressToStringExW(address, scopeId, 0, buffer, ref length);
        }

        public static uint Ipv4StringToAddress(string ipString, byte[] bytes, out ushort port)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytes.Length == IPAddressParser.IPv4AddressBytes);

            return Interop.NtDll.RtlIpv4StringToAddressExW(ipString, false, bytes, out port);
        }

        public static uint Ipv6StringToAddress(string ipString, byte[] bytes, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytes.Length == IPAddressParser.IPv6AddressBytes);

            ushort port;
            return Interop.NtDll.RtlIpv6StringToAddressExW(ipString, bytes, out scope, out port);
        }

        public static SocketError GetSocketErrorForErrorCode(uint status)
        {
            switch (status)
            {
                case Interop.StatusOptions.STATUS_SUCCESS:
                    return SocketError.Success;
                case Interop.StatusOptions.STATUS_INVALID_PARAMETER:
                    return SocketError.InvalidArgument;
                default:
                    return (SocketError)status;
            }
        }
    }
}
