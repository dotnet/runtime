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

        public static unsafe uint Ipv4AddressToString(byte[] address, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv4AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET_ADDRSTRLEN);

            return Interop.Sys.IPAddressToString(address, false, buffer);
        }

        public static unsafe uint Ipv6AddressToString(byte[] address, uint scopeId, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv6AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET6_ADDRSTRLEN);

            return Interop.Sys.IPAddressToString(address, true, buffer, scopeId);
        }

        public static unsafe uint Ipv4StringToAddress(string ipString, byte* bytes, int bytesLength, out ushort port)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytesLength >= IPAddressParserStatics.IPv4AddressBytes);

            return unchecked((uint)Interop.Sys.IPv4StringToAddress(ipString, bytes, bytesLength, out port));
        }

        private static bool IsHexString(string input, int startIndex)
        {
            // "0[xX][A-Fa-f0-9]+"
            if (startIndex >= input.Length - 3 ||
                input[startIndex] != '0' ||
                (input[startIndex + 1] != 'x' && input[startIndex + 1] != 'X'))
            {
                return false;
            }

            for (int i = startIndex + 2; i < input.Length; i++)
            {
                char c = input[i];
                if ((c < 'A' || c > 'F') && (c < 'a' || c > 'f') && (c < '0' || c > '9'))
                {
                    return false;
                }
            }

            return true;
        }

        // Splits an IPv6 address of the form '[.*]:.*' into its host and port parts and removes
        // surrounding square brackets, if any.
        private static bool TryPreprocessIPv6Address(string input, out string host, out string port)
        {
            Debug.Assert(input != null);

            if (input == "")
            {
                host = null;
                port = null;
                return false;
            }

            bool hasLeadingBracket = input[0] == '[';
            int trailingBracketIndex = -1;
            int portSeparatorIndex = -1;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] == ']')
                {
                    trailingBracketIndex = i;
                    break;
                }

                if (input[i] == ':')
                {
                    if (i >= 1 && input[i - 1] == ']')
                    {
                        trailingBracketIndex = i - 1;
                        portSeparatorIndex = i;
                    }
                    break;
                }
            }

            bool hasTrailingBracket = trailingBracketIndex != -1;
            if (hasLeadingBracket != hasTrailingBracket)
            {
                host = null;
                port = null;
                return false;
            }

            if (!hasLeadingBracket)
            {
                host = input;
                port = null;
            }
            else
            {
                host = input.Substring(1, trailingBracketIndex - 1);
                port = portSeparatorIndex != -1 && !IsHexString(input, portSeparatorIndex + 1) ?
                    input.Substring(portSeparatorIndex + 1) :
                    null;
            }
            return true;
        }

        public static unsafe uint Ipv6StringToAddress(string ipString, byte* bytes, int bytesLength, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytesLength >= IPAddressParserStatics.IPv6AddressBytes);

            string host, port;
            if (!TryPreprocessIPv6Address(ipString, out host, out port))
            {
                scope = 0;
                return unchecked((uint)Interop.Sys.GetAddrInfoErrorFlags.EAI_NONAME);
            }

            return unchecked((uint)Interop.Sys.IPv6StringToAddress(host, port, bytes, bytesLength, out scope));
        }

        public static SocketError GetSocketErrorForErrorCode(uint status)
        {
            switch (unchecked((int)status))
            {
                case 0:
                    return SocketError.Success;
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_BADFLAGS:
                case (int)Interop.Sys.GetAddrInfoErrorFlags.EAI_NONAME:
                    return SocketError.InvalidArgument;
                default:
                    return (SocketError)status;
            }
        }
    }
}
