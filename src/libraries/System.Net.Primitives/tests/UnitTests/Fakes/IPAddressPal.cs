// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal static class IPAddressPal
    {
        public const uint SuccessErrorCode = 0;

        public static uint Ipv4AddressToString(byte[] address, StringBuilder buffer)
        {
            return 0;
        }

        public static uint Ipv6AddressToString(byte[] address, uint scopeId, StringBuilder buffer)
        {
            return 0;
        }

        public static unsafe uint Ipv4StringToAddress(string ipString, byte* bytes, int bytesLength, out ushort port)
        {
            port = 0;
            return 0;
        }

        public static unsafe uint Ipv6StringToAddress(string ipString, byte* bytes, int bytesLength, out uint scope)
        {
            scope = 0;
            return 0;
        }

        public static SocketError GetSocketErrorForErrorCode(uint status)
        {
            return SocketError.Success;
        }
    }
}
