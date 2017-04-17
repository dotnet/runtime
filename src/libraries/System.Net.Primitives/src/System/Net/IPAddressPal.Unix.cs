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

            port = 0;
            long address;
            int end = ipString.Length;
            fixed (char* ipStringPtr = ipString)
            {
                address = IPv4AddressHelper.ParseNonCanonical(ipStringPtr, 0, ref end, notImplicitFile: true);
            }

            if (address == IPv4AddressHelper.Invalid || end != ipString.Length)
            {
                return (uint)SocketError.InvalidArgument;
            }

            bytes[0] = (byte)((0xFF000000 & address) >> 24);
            bytes[1] = (byte)((0x00FF0000 & address) >> 16);
            bytes[2] = (byte)((0x0000FF00 & address) >> 8);
            bytes[3] = (byte)((0x000000FF & address) >> 0);
            return (uint)SocketError.Success;
        }

        public static unsafe uint Ipv6StringToAddress(string ipString, byte* bytes, int bytesLength, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytesLength >= IPAddressParserStatics.IPv6AddressBytes);

            scope = 0;

            int offset = 0;
            if (ipString[0] != '[')
            {
                ipString = ipString + ']'; //for Uri parser to find the terminator.
            }
            else
            {
                offset = 1;
            }

            int end = ipString.Length;
            fixed (char* name = ipString)
            {
                if (IPv6AddressHelper.IsValidStrict(name, offset, ref end) || (end != ipString.Length))
                {
                    ushort* numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressBytes / 2];
                    string scopeId = null;
                    IPv6AddressHelper.Parse(ipString, numbers, 0, ref scopeId);

                    long result = 0;
                    if (!string.IsNullOrEmpty(scopeId))
                    {
                        if (scopeId.Length < 2)
                        {
                            return (uint)SocketError.InvalidArgument;
                        }

                        for (int i = 1; i < scopeId.Length; i++)
                        {
                            char c = scopeId[i];
                            if (c < '0' || c > '9')
                            {
                                return (uint)SocketError.InvalidArgument;
                            }
                            result = (result * 10) + (c - '0');
                            if (result > uint.MaxValue)
                            {
                                return (uint)SocketError.InvalidArgument;
                            }
                        }

                        scope = (uint)result;
                    }

                    for (int i = 0; i < IPAddressParserStatics.IPv6AddressBytes / 2; i++)
                    {
                        bytes[i * 2 + 1] = (byte)(numbers[i] & 0xFF);
                        bytes[i * 2] = (byte)((numbers[i] & 0xFF00) >> 8);
                    }
                    return (uint)SocketError.Success;
                }
            }

            return (uint)SocketError.InvalidArgument;
        }

        public static SocketError GetSocketErrorForErrorCode(uint status) => (SocketError)status;
    }
}
