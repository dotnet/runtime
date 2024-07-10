// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static class IPAddressParser
    {
        internal const int MaxIPv4StringLength = 15; // 4 numbers separated by 3 periods, with up to 3 digits per number
        internal const int MaxIPv6StringLength = 65;

        internal static IPAddress? Parse(ReadOnlySpan<char> ipSpan, bool tryParse)
        {
            if (ipSpan.Contains(':'))
            {
                // The address is parsed as IPv6 if and only if it contains a colon. This is valid because
                // we don't support/parse a port specification at the end of an IPv4 address.
                Span<ushort> numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressShorts];
                numbers.Clear();
                if (TryParseIPv6(ipSpan, numbers, IPAddressParserStatics.IPv6AddressShorts, out uint scope))
                {
                    return new IPAddress(numbers, scope);
                }
            }
            else if (TryParseIpv4(ipSpan, out long address))
            {
                return new IPAddress(address);
            }

            if (tryParse)
            {
                return null;
            }

            throw new FormatException(SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
        }

        private static unsafe bool TryParseIpv4(ReadOnlySpan<char> ipSpan, out long address)
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

            // Failed to parse the address.
            address = 0;
            return false;
        }

        private static unsafe bool TryParseIPv6(ReadOnlySpan<char> ipSpan, Span<ushort> numbers, int numbersLength, out uint scope)
        {
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

        internal static int FormatIPv4Address<TChar>(uint address, Span<TChar> addressString) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            address = (uint)IPAddress.NetworkToHostOrder(unchecked((int)address));

            int pos = FormatByte(address >> 24, addressString);
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address >> 16, addressString.Slice(pos));
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address >> 8, addressString.Slice(pos));
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address, addressString.Slice(pos));

            return pos;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int FormatByte(uint number, Span<TChar> addressString)
            {
                number &= 0xFF;

                if (number >= 10)
                {
                    uint hundreds, tens;
                    if (number >= 100)
                    {
                        (uint hundredsAndTens, number) = Math.DivRem(number, 10);
                        (hundreds, tens) = Math.DivRem(hundredsAndTens, 10);

                        addressString[2] = TChar.CreateTruncating('0' + number);
                        addressString[1] = TChar.CreateTruncating('0' + tens);
                        addressString[0] = TChar.CreateTruncating('0' + hundreds);
                        return 3;
                    }

                    (tens, number) = Math.DivRem(number, 10);
                    addressString[1] = TChar.CreateTruncating('0' + number);
                    addressString[0] = TChar.CreateTruncating('0' + tens);
                    return 2;
                }

                addressString[0] = TChar.CreateTruncating('0' + number);
                return 1;
            }
        }

        internal static int FormatIPv6Address<TChar>(ushort[] address, uint scopeId, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            int pos = 0;

            if (IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
            {
                // We need to treat the last 2 ushorts as a 4-byte IPv4 address,
                // so output the first 6 ushorts normally, followed by the IPv4 address.
                AppendSections(address.AsSpan(0, 6), destination, ref pos);
                if (destination[pos - 1] != TChar.CreateTruncating(':'))
                {
                    destination[pos++] = TChar.CreateTruncating(':');
                }

                pos += FormatIPv4Address(ExtractIPv4Address(address), destination.Slice(pos));
            }
            else
            {
                // No IPv4 address.  Output all 8 sections as part of the IPv6 address
                // with normal formatting rules.
                AppendSections(address.AsSpan(0, 8), destination, ref pos);
            }

            // If there's a scope ID, append it.
            if (scopeId != 0)
            {
                destination[pos++] = TChar.CreateTruncating('%');

                // TODO https://github.com/dotnet/runtime/issues/84527: Use UInt32 TryFormat for both char and byte once IUtf8SpanFormattable implementation exists
                Span<TChar> chars = stackalloc TChar[10];
                int bytesPos = 10;
                do
                {
                    (scopeId, uint digit) = Math.DivRem(scopeId, 10);
                    chars[--bytesPos] = TChar.CreateTruncating('0' + digit);
                }
                while (scopeId != 0);
                Span<TChar> used = chars.Slice(bytesPos);
                used.CopyTo(destination.Slice(pos));
                pos += used.Length;
            }

            return pos;

            // Appends each of the numbers in address in indexed range [fromInclusive, toExclusive),
            // while also replacing the longest sequence of 0s found in that range with "::", as long
            // as the sequence is more than one 0.
            static void AppendSections(ReadOnlySpan<ushort> address, Span<TChar> destination, ref int offset)
            {
                // Find the longest sequence of zeros to be combined into a "::"
                (int zeroStart, int zeroEnd) = IPv6AddressHelper.FindCompressionRange(address);
                bool needsColon = false;

                // Handle a zero sequence if there is one
                if (zeroStart >= 0)
                {
                    // Output all of the numbers before the zero sequence
                    for (int i = 0; i < zeroStart; i++)
                    {
                        if (needsColon)
                        {
                            destination[offset++] = TChar.CreateTruncating(':');
                        }
                        needsColon = true;
                        AppendHex(address[i], destination, ref offset);
                    }

                    // Output the zero sequence if there is one
                    destination[offset++] = TChar.CreateTruncating(':');
                    destination[offset++] = TChar.CreateTruncating(':');
                    needsColon = false;
                }

                // Output everything after the zero sequence
                for (int i = zeroEnd; i < address.Length; i++)
                {
                    if (needsColon)
                    {
                        destination[offset++] = TChar.CreateTruncating(':');
                    }
                    needsColon = true;
                    AppendHex(address[i], destination, ref offset);
                }
            }

            // Appends a number as hexadecimal (without the leading "0x")
            static void AppendHex(ushort value, Span<TChar> destination, ref int offset)
            {
                if ((value & 0xFFF0) != 0)
                {
                    if ((value & 0xFF00) != 0)
                    {
                        if ((value & 0xF000) != 0)
                        {
                            destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 12));
                        }

                        destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 8));
                    }

                    destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 4));
                }

                destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value));
            }
        }

        /// <summary>Extracts the IPv4 address from the end of the IPv6 address byte array.</summary>
        private static uint ExtractIPv4Address(ushort[] address)
        {
            uint ipv4address = (uint)address[6] << 16 | address[7];
            return (uint)IPAddress.HostToNetworkOrder(unchecked((int)ipv4address));
        }
    }
}
