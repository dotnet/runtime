// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Net
{
    internal static partial class IPv4AddressHelper
    {
        internal const long Invalid = -1;
        private const long MaxIPv4Value = uint.MaxValue; // the native parser cannot handle MaxIPv4Value, only MaxIPv4Value - 1

        private const int Octal = 8;
        private const int Decimal = 10;
        private const int Hex = 16;

        private const int NumberOfLabels = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ToUShort<TChar>(TChar value)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            return typeof(TChar) == typeof(char)
                ? (char)(object)value
                : (byte)(object)value;
        }

        // Only called from the IPv6Helper, only parse the canonical format
        internal static int ParseHostNumber<TChar>(ReadOnlySpan<TChar> str, int start, int end)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            Span<byte> numbers = stackalloc byte[NumberOfLabels];

            for (int i = 0; i < numbers.Length; ++i)
            {
                int b = 0;
                int ch;

                for (; (start < end) && (ch = ToUShort(str[start])) != '.' && ch != ':'; ++start)
                {
                    b = (b * 10) + ch - '0';
                }

                numbers[i] = (byte)b;
                ++start;
            }

            return BinaryPrimitives.ReadInt32BigEndian(numbers);
        }

        //
        // IsValid
        //
        //  Performs IsValid on a substring. Updates the index to where we
        //  believe the IPv4 address ends
        //
        // Inputs:
        //  <argument>  name
        //      string containing possible IPv4 address
        //
        //  <argument>  start
        //      offset in <name> to start checking for IPv4 address
        //
        //  <argument>  end
        //      offset in <name> of the last character we can touch in the check
        //
        // Outputs:
        //  <argument>  end
        //      index of last character in <name> we checked
        //
        //  <argument>  allowIPv6
        //      enables parsing IPv4 addresses embedded in IPv6 address literals
        //
        //  <argument>  notImplicitFile
        //      do not consider this URI holding an implicit filename
        //
        //  <argument>  unknownScheme
        //      the check is made on an unknown scheme (suppress IPv4 canonicalization)
        //
        // Assumes:
        // The address string is terminated by either
        // end of the string, characters ':' '/' '\' '?'
        //
        //
        // Returns:
        //  bool
        //
        // Throws:
        //  Nothing
        //

        //Remark: MUST NOT be used unless all input indexes are verified and trusted.
        internal static unsafe bool IsValid<TChar>(TChar* name, int start, ref int end, bool allowIPv6, bool notImplicitFile, bool unknownScheme)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // IPv6 can only have canonical IPv4 embedded. Unknown schemes will not attempt parsing of non-canonical IPv4 addresses.
            if (allowIPv6 || unknownScheme)
            {
                return IsValidCanonical(name, start, ref end, allowIPv6, notImplicitFile);
            }
            else
            {
                return ParseNonCanonical(name, start, ref end, notImplicitFile) != Invalid;
            }
        }

        //
        // IsValidCanonical
        //
        //  Checks if the substring is a valid canonical IPv4 address or an IPv4 address embedded in an IPv6 literal
        //  This is an attempt to parse ABNF productions from RFC3986, Section 3.2.2:
        //     IP-literal = "[" ( IPv6address / IPvFuture  ) "]"
        //     IPv4address = dec-octet "." dec-octet "." dec-octet "." dec-octet
        //     dec-octet   = DIGIT                 ; 0-9
        //                 / %x31-39 DIGIT         ; 10-99
        //                 / "1" 2DIGIT            ; 100-199
        //                 / "2" %x30-34 DIGIT     ; 200-249
        //                 / "25" %x30-35          ; 250-255
        //
        internal static unsafe bool IsValidCanonical<TChar>(TChar* name, int start, ref int end, bool allowIPv6, bool notImplicitFile)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int dots = 0;
            long number = 0;
            bool haveNumber = false;
            bool firstCharIsZero = false;

            while (start < end)
            {
                int ch = ToUShort(name[start]);

                if (allowIPv6)
                {
                    // For an IPv4 address nested inside an IPv6 address, the terminator is either the IPv6 address terminator (']'), prefix ('/') or ScopeId ('%')
                    if (ch == ']' || ch == '/' || ch == '%')
                    {
                        break;
                    }
                }
                else if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
                {
                    // For a normal IPv4 address, the terminator is the prefix ('/' or its counterpart, '\'). If notImplicitFile is set, the terminator
                    // is one of the characters which signify the start of the rest of the URI - the port number (':'), query string ('?') or fragment ('#')

                    break;
                }

                // An explicit cast to an unsigned integer forces character values preceding '0' to underflow, eliminating one comparison below.
                uint parsedCharacter = (uint)(ch - '0');

                if (parsedCharacter < IPv4AddressHelper.Decimal)
                {
                    // A number starting with zero should be interpreted in base 8 / octal
                    if (!haveNumber && parsedCharacter == 0)
                    {
                        if ((start + 1 < end) && name[start + 1] == TChar.CreateTruncating('0'))
                        {
                            // 00 is not allowed as a prefix.
                            return false;
                        }

                        firstCharIsZero = true;
                    }

                    haveNumber = true;
                    number = number * IPv4AddressHelper.Decimal + parsedCharacter;
                    if (number > byte.MaxValue)
                    {
                        return false;
                    }
                }
                else if (ch == '.')
                {
                    // If the current character is not an integer, it may be the IPv4 component separator ('.')

                    if (!haveNumber || (number > 0 && firstCharIsZero))
                    {
                        // 0 is not allowed to prefix a number.
                        return false;
                    }
                    ++dots;
                    haveNumber = false;
                    number = 0;
                    firstCharIsZero = false;
                }
                else
                {
                    return false;
                }
                ++start;
            }
            bool res = (dots == 3) && haveNumber;
            if (res)
            {
                end = start;
            }
            return res;
        }

        // Parse any canonical or noncanonical IPv4 formats and return a long between 0 and MaxIPv4Value.
        // Return Invalid (-1) for failures.
        // If the address has less than three dots, only the rightmost section is assumed to contain the combined value for
        // the missing sections: 0xFF00FFFF == 0xFF.0x00.0xFF.0xFF == 0xFF.0xFFFF
        internal static unsafe long ParseNonCanonical<TChar>(TChar* name, int start, ref int end, bool notImplicitFile)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int numberBase = IPv4AddressHelper.Decimal;
            int ch = 0;
            long* parts = stackalloc long[3]; // One part per octet. Final octet doesn't have a terminator, so is stored in currentValue.
            long currentValue = 0;
            bool atLeastOneChar = false;

            // Parse one dotted section at a time
            int dotCount = 0; // Limit 3
            int current = start;

            for (; current < end; current++)
            {
                ch = ToUShort(name[current]);
                currentValue = 0;

                // Figure out what base this section is in, default to base 10.
                // A number starting with zero should be interpreted in base 8 / octal
                // If the number starts with 0x, it should be interpreted in base 16 / hex
                numberBase = IPv4AddressHelper.Decimal;

                if (ch == '0')
                {
                    current++;
                    atLeastOneChar = true;
                    if (current < end)
                    {
                        ch = ToUShort(name[current]);

                        if (ch == 'x' || ch == 'X')
                        {
                            numberBase = IPv4AddressHelper.Hex;

                            current++;
                            atLeastOneChar = false;
                        }
                        else
                        {
                            numberBase = IPv4AddressHelper.Octal;
                        }
                    }
                }

                // Parse this section
                for (; current < end; current++)
                {
                    ch = ToUShort(name[current]);
                    int digitValue = HexConverter.FromChar(ch);

                    if (digitValue >= numberBase)
                    {
                        break; // Invalid/terminator
                    }
                    currentValue = (currentValue * numberBase) + digitValue;

                    if (currentValue > MaxIPv4Value) // Overflow
                    {
                        return Invalid;
                    }

                    atLeastOneChar = true;
                }

                if (current < end && ch == '.')
                {
                    if (dotCount >= 3 // Max of 3 dots and 4 segments
                        || !atLeastOneChar // No empty segments: 1...1
                                           // Only the last segment can be more than 255 (if there are less than 3 dots)
                        || currentValue > 0xFF)
                    {
                        return Invalid;
                    }
                    parts[dotCount] = currentValue;
                    dotCount++;
                    atLeastOneChar = false;
                    continue;
                }
                // We don't get here unless we find an invalid character or a terminator
                break;
            }

            // Terminators
            if (!atLeastOneChar)
            {
                return Invalid;  // Empty trailing segment: 1.1.1.
            }
            else if (current >= end)
            {
                // end of string, allowed
            }
            else if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
            {
                // For a normal IPv4 address, the terminator is the prefix ('/' or its counterpart, '\'). If notImplicitFile is set, the terminator
                // is one of the characters which signify the start of the rest of the URI - the port number (':'), query string ('?') or fragment ('#')

                end = current;
            }
            else
            {
                // not a valid terminating character
                return Invalid;
            }

            // Parsed, reassemble and check for overflows in the last part. Previous parts have already been checked in the loop
            switch (dotCount)
            {
                case 0: // 0xFFFFFFFF
                    return currentValue;
                case 1: // 0xFF.0xFFFFFF
                    Debug.Assert(parts[0] <= 0xFF);
                    if (currentValue > 0xffffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | currentValue;
                case 2: // 0xFF.0xFF.0xFFFF
                    Debug.Assert(parts[0] <= 0xFF);
                    Debug.Assert(parts[1] <= 0xFF);
                    if (currentValue > 0xffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | currentValue;
                case 3: // 0xFF.0xFF.0xFF.0xFF
                    Debug.Assert(parts[0] <= 0xFF);
                    Debug.Assert(parts[1] <= 0xFF);
                    Debug.Assert(parts[2] <= 0xFF);
                    if (currentValue > 0xff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | (parts[2] << 8) | currentValue;
                default:
                    return Invalid;
            }
        }
    }
}
