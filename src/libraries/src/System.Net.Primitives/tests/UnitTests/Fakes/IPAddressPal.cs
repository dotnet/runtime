// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static uint Ipv4StringToAddress(string ipString, byte[] bytes, out ushort port)
        {
			port = 0;
            return 0;
        }

        public static uint Ipv6StringToAddress(string ipString, byte[] bytes, out uint scope)
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
