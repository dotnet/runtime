// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal class IPAddressParser
    {
        internal static unsafe IPAddress Parse(string ipString, bool tryParse)
        {
            if (ipString == null)
            {
                if (tryParse)
                {
                    return null;
                }
                throw new ArgumentNullException(nameof(ipString));
            }

            if (ipString.IndexOf(':') != -1)
            {
                // If the address string contains the colon character then it can only be an IPv6 address.
                // This is valid because we don't support/parse a port specification at the end of an IPv4 address.
                uint scope;
                ushort* numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressShorts];
                if (Ipv6StringToAddress(ipString, numbers, IPAddressParserStatics.IPv6AddressShorts, out scope))
                {
                    return new IPAddress(numbers, IPAddressParserStatics.IPv6AddressShorts, scope);
                }
            }
            else
            {
                long address;
                if (Ipv4StringToAddress(ipString, out address))
                {
                    return new IPAddress(address);
                }
            }

            if (tryParse)
            {
                return null;
            }

            throw new FormatException(SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
        }

        internal static unsafe string IPv4AddressToString(uint address)
        {
            const int MaxLength = 15;
            char* addressString = stackalloc char[MaxLength];
            int offset = MaxLength;

            FormatIPv4AddressNumber((int)((address >> 24) & 0xFF), addressString, ref offset);
            addressString[--offset] = '.';
            FormatIPv4AddressNumber((int)((address >> 16) & 0xFF), addressString, ref offset);
            addressString[--offset] = '.';
            FormatIPv4AddressNumber((int)((address >> 8) & 0xFF), addressString, ref offset);
            addressString[--offset] = '.';
            FormatIPv4AddressNumber((int)(address & 0xFF), addressString, ref offset);

            return new string(addressString, offset, MaxLength - offset);
        }

        internal static string IPv6AddressToString(ushort[] address, uint scopeId)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv6AddressShorts);

            const int INET6_ADDRSTRLEN = 65;
            StringBuilder buffer = StringBuilderCache.Acquire(INET6_ADDRSTRLEN);

            if (IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
            {
                // We need to treat the last 2 ushorts as a 4-byte IPv4 address,
                // so output the first 6 ushorts normally, followed by the IPv4 address.
                AppendSections(address, 0, 6, buffer);
                if (buffer[buffer.Length - 1] != ':')
                {
                    buffer.Append(':');
                }
                buffer.Append(IPAddressParser.IPv4AddressToString(ExtractIPv4Address(address)));
            }
            else
            {
                // No IPv4 address.  Output all 8 sections as part of the IPv6 address
                // with normal formatting rules.
                AppendSections(address, 0, 8, buffer);
            }

            // If there's a scope ID, append it.
            if (scopeId != 0)
            {
                buffer.Append('%').Append(scopeId);
            }

            return StringBuilderCache.GetStringAndRelease(buffer);
        }

        private static unsafe void FormatIPv4AddressNumber(int number, char* addressString, ref int offset)
        {
            int i = offset;
            do
            {
                int rem;
                number = Math.DivRem(number, 10, out rem);
                addressString[--i] = (char)('0' + rem);
            } while (number != 0);
            offset = i;
        }

        public static unsafe bool Ipv4StringToAddress(string ipString, out long address)
        {
            Debug.Assert(ipString != null);

            long tmpAddr;
            int end = ipString.Length;
            fixed (char* ipStringPtr = ipString)
            {
                tmpAddr = IPv4AddressHelper.ParseNonCanonical(ipStringPtr, 0, ref end, notImplicitFile: true);
            }

            if (tmpAddr != IPv4AddressHelper.Invalid && end == ipString.Length)
            {
                // IPv4AddressHelper.ParseNonCanonical returns the bytes in the inverse order.
                // Reverse them and return success.
                address =
                    ((0xFF000000 & tmpAddr) >> 24) |
                    ((0x00FF0000 & tmpAddr) >> 8) |
                    ((0x0000FF00 & tmpAddr) << 8) |
                    ((0x000000FF & tmpAddr) << 24);
                return true;
            }
            else
            {
                // Failed to parse the address.
                address = 0;
                return false;
            }
        }

        public static unsafe bool Ipv6StringToAddress(string ipString, ushort* numbers, int numbersLength, out uint scope)
        {
            Debug.Assert(ipString != null);
            Debug.Assert(numbers != null);
            Debug.Assert(numbersLength >= IPAddressParserStatics.IPv6AddressShorts);

            int end = ipString.Length;
            fixed (char* name = ipString)
            {
                if (IPv6AddressHelper.IsValidStrict(name, 0, ref end) || (end != ipString.Length))
                {
                    string scopeId = null;
                    IPv6AddressHelper.Parse(ipString, numbers, 0, ref scopeId);

                    long result = 0;
                    if (!string.IsNullOrEmpty(scopeId))
                    {
                        if (scopeId.Length < 2)
                        {
                            scope = 0;
                            return false;
                        }

                        for (int i = 1; i < scopeId.Length; i++)
                        {
                            char c = scopeId[i];
                            if (c < '0' || c > '9')
                            {
                                scope = 0;
                                return false;
                            }
                            result = (result * 10) + (c - '0');
                            if (result > uint.MaxValue)
                            {
                                scope = 0;
                                return false;
                            }
                        }
                    }

                    scope = (uint)result;
                    return true;
                }
            }

            scope = 0;
            return false;
        }

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
        private static unsafe void AppendHex(ushort value, StringBuilder buffer)
        {
            const int MaxLength = sizeof(ushort) * 2; // two hex chars per byte
            char* chars = stackalloc char[MaxLength];
            int pos = MaxLength;

            do
            {
                int rem = value % 16;
                value /= 16;
                chars[--pos] = rem < 10 ? (char)('0' + rem) : (char)('a' + (rem - 10));
                Debug.Assert(pos >= 0);
            }
            while (value != 0);

            buffer.Append(chars + pos, MaxLength - pos);
        }

        /// <summary>Extracts the IPv4 address from the end of the IPv6 address byte array.</summary>
        private static uint ExtractIPv4Address(ushort[] address) => (uint)(Reverse(address[7]) << 16) | Reverse(address[6]);

        /// <summary>Reverses the two bytes in the ushort.</summary>
        private static ushort Reverse(ushort number) => (ushort)(((number >> 8) & 0xFF) | ((number << 8) & 0xFF00));
    }
}
