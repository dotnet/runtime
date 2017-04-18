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
        public const uint SuccessErrorCode = (uint)SocketError.Success;

        public static unsafe uint Ipv6AddressToString(ushort[] address, uint scopeId, StringBuilder buffer)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv6AddressBytes / 2);
            Debug.Assert(buffer != null);

            if (IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
            {
                AppendSections(address, 0, 6, buffer);
                if (buffer[buffer.Length - 1] != ':')
                {
                    buffer.Append(':');
                }
                buffer.Append(IPAddressParser.IPv4AddressToString(ExtractIPv4Address(address)));
            }
            else
            {
                AppendSections(address, 0, 8, buffer);
            }

            if (scopeId != 0)
            {
                buffer.Append('%').Append(scopeId);
            }

            return SuccessErrorCode;
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
            return SuccessErrorCode;
        }

        public static unsafe uint Ipv6StringToAddress(string ipString, byte* bytes, int bytesLength, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(bytes != null);
            Debug.Assert(bytesLength >= IPAddressParserStatics.IPv6AddressBytes);

            scope = 0;

            int end = ipString.Length;
            fixed (char* name = ipString)
            {
                if (IPv6AddressHelper.IsValidStrict(name, 0, ref end) || (end != ipString.Length))
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
                    return SuccessErrorCode;
                }
            }

            return (uint)SocketError.InvalidArgument;
        }

        public static SocketError GetSocketErrorForErrorCode(uint status) => (SocketError)status;

        /// <summary>
        /// Appends each of the numbers in address in indexed range [fromInclusive, toExclusive),
        /// while also replacing the longest sequence of 0s found in that range with "::", as long
        /// as the sequence is more than one 0.
        /// </summary>
        private static void AppendSections(ushort[] address, int fromInclusive, int toExclusive, StringBuilder buffer)
        {
            // Find the longest sequence of zeros to be combined into a "::"
            (int zeroStart, int zeroEnd) = IPv6AddressHelper.FindCompressionRange(address, fromInclusive, toExclusive);
            bool needsColon = false;

            // Output all of the numbers before the zero sequence
            for (int i = fromInclusive; i < zeroStart; i++)
            {
                if (needsColon)
                {
                    buffer.Append(':');
                }
                needsColon = true;
                AppendHex(address[i], buffer);
            }

            // Output the zero sequence if there is one
            if (zeroStart >= 0)
            {
                buffer.Append("::");
                needsColon = false;
                fromInclusive = zeroEnd;
            }

            // Output everything after the zero sequence
            for (int i = fromInclusive; i < toExclusive; i++)
            {
                if (needsColon)
                {
                    buffer.Append(':');
                }
                needsColon = true;
                AppendHex(address[i], buffer);
            }
        }

        /// <summary>Appends a number as hexadecimal (without the leading "0x") to the StringBuilder.</summary>
        private static unsafe void AppendHex(int value, StringBuilder buffer)
        {
            const int MaxLength = 8;
            char* chars = stackalloc char[MaxLength];
            int len = 0;

            do
            {
                int rem;
                value = Math.DivRem(value, 16, out rem);
                chars[len++] = rem < 10 ?
                    (char)('0' + rem) :
                    (char)('a' + (rem - 10));
            }
            while (value != 0);

            int mid = len / 2;
            for (int i = 0; i < mid; i++)
            {
                char c = chars[i];
                chars[i] = chars[len - i - 1];
                chars[len - i - 1] = c;
            }

            buffer.Append(chars, len);
        }

        /// <summary>Extracts the IPv4 address from the end of the IPv6 address byte array.</summary>
        private static uint ExtractIPv4Address(ushort[] address) => (uint)(Reverse(address[7]) << 16) | Reverse(address[6]);

        /// <summary>Reverses the two bytes in the ushort.</summary>
        private static ushort Reverse(ushort number) => (ushort)(((number >> 8) & 0xFF) | ((number << 8) & 0xFF00));
    }
}
