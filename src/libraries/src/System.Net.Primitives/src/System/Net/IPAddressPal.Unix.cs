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

        public static unsafe uint Ipv4AddressToString(byte[] address, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParser.IPv4AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET_ADDRSTRLEN);

            var sockaddr = new Interop.libc.sockaddr_in {
                sin_family = Interop.libc.AF_INET,
                sin_port = 0
            };

            sockaddr.sin_addr.s_addr = address.NetworkBytesToNetworkUInt32(0);

            int err = Interop.libc.getnameinfo((Interop.libc.sockaddr*)&sockaddr, (uint)sizeof(Interop.libc.sockaddr_in), buffer, (uint)buffer.Capacity, null, 0, Interop.libc.NI_NUMERICHOST);
            return unchecked((uint)err);
        }

        public static unsafe uint Ipv6AddressToString(byte[] address, uint scopeId, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParser.IPv6AddressBytes);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Capacity >= IPAddressParser.INET6_ADDRSTRLEN);

            var sockaddr = new Interop.libc.sockaddr_in6 {
                sin6_family = Interop.libc.AF_INET6,
                sin6_port = 0,
                sin6_scope_id = scopeId
            };

            Debug.Assert(sizeof(Interop.libc.in6_addr) == IPAddressParser.IPv6AddressBytes);
            for (int i = 0; i < IPAddressParser.IPv6AddressBytes; i++)
            {
                sockaddr.sin6_addr.s6_addr[i] = address[i];
            }

            int err = Interop.libc.getnameinfo((Interop.libc.sockaddr*)&sockaddr, (uint)sizeof(Interop.libc.sockaddr_in6), buffer, (uint)buffer.Capacity, null, 0, Interop.libc.NI_NUMERICHOST);
            return unchecked((uint)err);
        }

        public static unsafe uint Ipv4StringToAddress(string ipString, byte[] bytes, out ushort port)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytes.Length == IPAddressParser.IPv4AddressBytes);

            port = 0;

            var hints = new Interop.libc.addrinfo {
                ai_flags = Interop.libc.AI_NUMERICHOST | Interop.libc.AI_NUMERICSERV,
                ai_family = Interop.libc.AF_INET,
                ai_socktype = 0,
                ai_protocol = 0
            };

            Interop.libc.addrinfo* addrinfo = null;
            int err = Interop.libc.getaddrinfo(ipString, null, &hints, &addrinfo);
            if (err != 0)
            {
                Debug.Assert(addrinfo == null);
                return unchecked((uint)err);
            }

            Debug.Assert(addrinfo != null);
            Debug.Assert(addrinfo->ai_addr != null);
            Debug.Assert(addrinfo->ai_addr->sa_family == Interop.libc.AF_INET);

            Interop.libc.sockaddr_in* sockaddr = (Interop.libc.sockaddr_in*)addrinfo->ai_addr;

            sockaddr->sin_addr.s_addr.NetworkToNetworkBytes(bytes, 0);
            port = sockaddr->sin_port;

            Interop.libc.freeaddrinfo(addrinfo);

            Debug.Assert(err == 0);
            return 0;
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
                var c = input[i];
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

            var hasLeadingBracket = input[0] == '[';
            var trailingBracketIndex = -1;
            var portSeparatorIndex = -1;
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] == ']')
                {
                    trailingBracketIndex = i;
                    break;
                }

                if (input[i] == ':')
                {
                    Debug.Assert(i >= 1);
                    if (input[i - 1] == ']')
                    {
                        trailingBracketIndex = i - 1;
                        portSeparatorIndex = i;
                    }
                    break;
                }
            }

            var hasTrailingBracket = trailingBracketIndex != -1;
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

        public static unsafe uint Ipv6StringToAddress(string ipString, byte[] bytes, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytes.Length == IPAddressParser.IPv6AddressBytes);

            string host, port;
            if (!TryPreprocessIPv6Address(ipString, out host, out port))
            {
                scope = 0;
                return unchecked((uint)Interop.libc.EAI_NONAME);
            }

            var hints = new Interop.libc.addrinfo {
                ai_flags = Interop.libc.AI_NUMERICHOST | Interop.libc.AI_NUMERICSERV,
                ai_family = Interop.libc.AF_INET6,
                ai_socktype = 0,
                ai_protocol = 0
            };

            Interop.libc.addrinfo* addrinfo = null;
            int err = Interop.libc.getaddrinfo(host, port, &hints, &addrinfo);
            if (err != 0)
            {
                Debug.Assert(addrinfo == null);

                scope = 0;
                return unchecked((uint)err);
            }

            Debug.Assert(addrinfo != null);
            Debug.Assert(addrinfo->ai_addr != null);
            Debug.Assert(addrinfo->ai_addr->sa_family == Interop.libc.AF_INET6);

            Interop.libc.sockaddr_in6* sockaddr = (Interop.libc.sockaddr_in6*)addrinfo->ai_addr;

            Debug.Assert(sizeof(Interop.libc.in6_addr) == IPAddressParser.IPv6AddressBytes);
            for (int i = 0; i < IPAddressParser.IPv6AddressBytes; i++)
            {
                bytes[i] = sockaddr->sin6_addr.s6_addr[i];
            }

            scope = sockaddr->sin6_scope_id;

            Interop.libc.freeaddrinfo(addrinfo);

            Debug.Assert(err == 0);
            return 0;
        }

        public static SocketError GetSocketErrorForErrorCode(uint status)
        {
            switch (unchecked((int)status))
            {
                case 0:
                    return SocketError.Success;
                case Interop.libc.EAI_BADFLAGS:
                case Interop.libc.EAI_NONAME:
                    return SocketError.InvalidArgument;
                default:
                    return (SocketError)status;
            }
        }
    }
}
