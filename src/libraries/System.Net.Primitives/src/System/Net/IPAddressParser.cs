// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Buffers.Binary;

namespace System.Net
{
    internal static class IPAddressParser
    {
        private const int MaxIPv4StringLength = 15; // 4 numbers separated by 3 periods, with up to 3 digits per number

        internal static IPAddress? Parse(ReadOnlySpan<char> ipSpan, bool tryParse)
        {
            if (ipSpan.Contains(':'))
            {
                // The address is parsed as IPv6 if and only if it contains a colon. This is valid because
                // we don't support/parse a port specification at the end of an IPv4 address.
                Span<ushort> numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressShorts];
                numbers.Clear();
                if (Ipv6StringToAddress(ipSpan, numbers, IPAddressParserStatics.IPv6AddressShorts, out uint scope))
                {
                    return new IPAddress(numbers, scope);
                }
            }
            else if (Ipv4StringToAddress(ipSpan, out long address))
            {
                return new IPAddress(address);
            }

            if (tryParse)
            {
                return null;
            }

            throw new FormatException(SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
        }

        internal static unsafe string IPv4AddressToString(uint address)
        {
            char* addressString = stackalloc char[MaxIPv4StringLength];
            int charsWritten = IPv4AddressToStringHelper(address, addressString);
            return new string(addressString, 0, charsWritten);
        }

        internal static unsafe void IPv4AddressToString(uint address, StringBuilder destination)
        {
            char* addressString = stackalloc char[MaxIPv4StringLength];
            int charsWritten = IPv4AddressToStringHelper(address, addressString);
            destination.Append(addressString, charsWritten);
        }

        internal static unsafe bool IPv4AddressToString(uint address, Span<char> formatted, out int charsWritten)
        {
            if (formatted.Length < MaxIPv4StringLength)
            {
                charsWritten = 0;
                return false;
            }

            fixed (char* formattedPtr = &MemoryMarshal.GetReference(formatted))
            {
                charsWritten = IPv4AddressToStringHelper(address, formattedPtr);
            }

            return true;
        }

        private static unsafe int IPv4AddressToStringHelper(uint address, char* addressString)
        {
            int offset = 0;
            address = (uint)IPAddress.NetworkToHostOrder(unchecked((int)address));

            FormatIPv4AddressNumber((int)((address >> 24) & 0xFF), addressString, ref offset);
            addressString[offset++] = '.';
            FormatIPv4AddressNumber((int)((address >> 16) & 0xFF), addressString, ref offset);
            addressString[offset++] = '.';
            FormatIPv4AddressNumber((int)((address >> 8) & 0xFF), addressString, ref offset);
            addressString[offset++] = '.';
            FormatIPv4AddressNumber((int)(address & 0xFF), addressString, ref offset);

            return offset;
        }

        internal static string IPv6AddressToString(ushort[] address, uint scopeId)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv6AddressShorts);

            StringBuilder buffer = IPv6AddressToStringHelper(address, scopeId);

            return StringBuilderCache.GetStringAndRelease(buffer);
        }

        internal static bool IPv6AddressToString(ushort[] address, uint scopeId, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(address != null);
            Debug.Assert(address.Length == IPAddressParserStatics.IPv6AddressShorts);

            StringBuilder buffer = IPv6AddressToStringHelper(address, scopeId);

            if (destination.Length < buffer.Length)
            {
                StringBuilderCache.Release(buffer);
                charsWritten = 0;
                return false;
            }

            buffer.CopyTo(0, destination, buffer.Length);
            charsWritten = buffer.Length;

            StringBuilderCache.Release(buffer);

            return true;
        }

        internal static StringBuilder IPv6AddressToStringHelper(ushort[] address, uint scopeId)
        {
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
                IPv4AddressToString(ExtractIPv4Address(address), buffer);
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

            return buffer;
        }

        private static unsafe void FormatIPv4AddressNumber(int number, char* addressString, ref int offset)
        {
            // Math.DivRem has no overload for byte, assert here for safety
            Debug.Assert(number < 256);

            offset += number > 99 ? 3 : number > 9 ? 2 : 1;

            int i = offset;
            do
            {
                number = Math.DivRem(number, 10, out int rem);
                addressString[--i] = (char)('0' + rem);
            } while (number != 0);
        }

        public static unsafe bool Ipv4StringToAddress(ReadOnlySpan<char> ipSpan, out long address)
        {
            int end = ipSpan.Length;
            long tmpAddr;

            fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan))
            {
                tmpAddr = IPv4AddressHelper.ParseNonCanonical(ipStringPtr, 0, ref end, notImplicitFile: true);
            }

            if (tmpAddr != IPv4AddressHelper.Invalid && end == ipSpan.Length)
            {
                // IPv4AddressHelper.ParseNonCanonical returns the bytes in host order.
                // Convert to network order and return success.
                address = (uint)IPAddress.HostToNetworkOrder(unchecked((int)tmpAddr));
                return true;
            }
            else
            {
                // Failed to parse the address.
                address = 0;
                return false;
            }
        }

        public static unsafe bool Ipv6StringToAddress(ReadOnlySpan<char> ipSpan, Span<ushort> numbers, int numbersLength, out uint scope)
        {
            Debug.Assert(numbers != null);
            Debug.Assert(numbersLength >= IPAddressParserStatics.IPv6AddressShorts);

            int end = ipSpan.Length;

            bool isValid = false;
            fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan))
            {
                isValid = IPv6AddressHelper.IsValidStrict(ipStringPtr, 0, ref end);
            }
            if (isValid || (end != ipSpan.Length))
            {
                string? scopeId = null;
                IPv6AddressHelper.Parse(ipSpan, numbers, 0, ref scopeId);

                if (scopeId?.Length > 1)
                {
                    if (uint.TryParse(scopeId.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out scope))
                    {
                        return true; // scopeId is a numeric value
                    }
                    uint interfaceIndex = InterfaceInfoPal.InterfaceNameToIndex(scopeId);
                    if (interfaceIndex > 0)
                    {
                        scope = interfaceIndex;
                        return true; // scopeId is a known interface name
                    }
                    // scopeId is an unknown interface name
                }
                // scopeId is not presented
                scope = 0;
                return true;
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
            ReadOnlySpan<ushort> addressSpan = new ReadOnlySpan<ushort>(address, fromInclusive, toExclusive - fromInclusive);
            (int zeroStart, int zeroEnd) = IPv6AddressHelper.FindCompressionRange(addressSpan);
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
        private static void AppendHex(ushort value, StringBuilder buffer)
        {
            if ((value & 0xF000) != 0)
                buffer.Append(HexConverter.ToCharLower(value >> 12));

            if ((value & 0xFF00) != 0)
                buffer.Append(HexConverter.ToCharLower(value >> 8));

            if ((value & 0xFFF0) != 0)
                buffer.Append(HexConverter.ToCharLower(value >> 4));

            buffer.Append(HexConverter.ToCharLower(value));
        }

        /// <summary>Extracts the IPv4 address from the end of the IPv6 address byte array.</summary>
        private static uint ExtractIPv4Address(ushort[] address)
        {
            uint ipv4address = (uint)address[6] << 16 | (uint)address[7];
            return (uint)IPAddress.HostToNetworkOrder(unchecked((int)ipv4address));
        }
    }
}
