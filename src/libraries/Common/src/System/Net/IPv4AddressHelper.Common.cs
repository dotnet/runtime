// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static partial class IPv4AddressHelper
    {
        internal const long Invalid = -1;
        private const long MaxIPv4Value = uint.MaxValue; // the native parser cannot handle MaxIPv4Value, only MaxIPv4Value - 1

        private const int NumberOfLabels = 4;

        // Only called from the IPv6Helper, only parse the canonical format
        internal static int ParseHostNumber<TChar>(ReadOnlySpan<TChar> str)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Span<byte> numbers = stackalloc byte[NumberOfLabels];
            int start = 0;

            for (int i = 0; i < numbers.Length; ++i)
            {
                int b = 0;
                TChar ch;

                for (; (start < str.Length) && (ch = str[start]) != TChar.CreateTruncating('.') && ch != TChar.CreateTruncating(':'); ++start)
                {
                    b = (b * 10) + int.CreateTruncating(ch) - '0';
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
        //  <argument>  allowIPv6
        //      enables parsing IPv4 addresses embedded in IPv6 address literals
        //
        //  <argument>  notImplicitFile
        //      do not consider this URI holding an implicit filename
        //
        //  <argument>  unknownScheme
        //      the check is made on an unknown scheme (suppress IPv4 canonicalization)
        //
        // Outputs:
        //  <argument>  charsConsumed
        //      index of last character in <name> we checked
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
        internal static bool IsValid<TChar>(ReadOnlySpan<TChar> name, out int charsConsumed, bool allowIPv6, bool notImplicitFile, bool unknownScheme)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // IPv6 can only have canonical IPv4 embedded. Unknown schemes will not attempt parsing of non-canonical IPv4 addresses.
            if (allowIPv6 || unknownScheme)
            {
                return IsValidCanonical(name, out charsConsumed, allowIPv6, notImplicitFile);
            }
            else
            {
                return ParseNonCanonical(name, out charsConsumed, notImplicitFile) != Invalid;
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
        internal static bool IsValidCanonical<TChar>(ReadOnlySpan<TChar> name, out int charsConsumed, bool allowIPv6, bool notImplicitFile)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            int dots = 0;
            int number = 0;
            bool haveNumber = false;
            bool firstCharIsZero = false;
            int current;

            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            charsConsumed = 0;
            for (current = 0; current < name.Length; current++)
            {
                TChar ch = name[current];

                if (allowIPv6)
                {
                    // For an IPv4 address nested inside an IPv6, the terminator is either ScopeId ('%'), prefix ('/') or ipv6 address terminator (']')
                    if (ch == TChar.CreateTruncating('%') || ch == TChar.CreateTruncating('/') || ch == TChar.CreateTruncating(']'))
                    {
                        break;
                    }
                }
                // For a normal IPv4 address, the terminator is the prefix ('/' or its counterpart, '\'). If notImplicitFile is set, the terminator
                // is one of the characters which signify the start of the rest of the URI - the port number (':'), query string ('?') or fragment ('#')
                else if (ch == TChar.CreateTruncating('/') || ch == TChar.CreateTruncating('\\')
                    || (notImplicitFile && (ch == TChar.CreateTruncating(':') || ch == TChar.CreateTruncating('?') || ch == TChar.CreateTruncating('#'))))
                {
                    break;
                }

                if (IPAddressParser.TryParseInteger(IPAddressParser.Decimal, ch, out int parsedCharacter))
                {
                    // A number starting with zero should be interpreted in base 8 / octal
                    if (!haveNumber && ch == TChar.CreateTruncating('0'))
                    {
                        if (current + 1 < name.Length && name[current + 1] == TChar.CreateTruncating('0'))
                        {
                            // 00 is not allowed as a prefix.
                            return false;
                        }

                        firstCharIsZero = true;
                    }

                    haveNumber = true;
                    number = number * IPAddressParser.Decimal + parsedCharacter;
                    if (number > byte.MaxValue)
                    {
                        return false;
                    }
                }
                // If the current character is not an integer, it may be the IPv4 component separator ('.')
                else if (ch == TChar.CreateTruncating('.'))
                {
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
            }
            bool res = (dots == 3) && haveNumber;
            if (res)
            {
                charsConsumed = current;
            }
            return res;
        }

        // Parse any canonical or noncanonical IPv4 formats and return a long between 0 and MaxIPv4Value.
        // Return Invalid (-1) for failures.
        // If the address has less than three dots, only the rightmost section is assumed to contain the combined value for
        // the missing sections: 0xFF00FFFF == 0xFF.0x00.0xFF.0xFF == 0xFF.0xFFFF
        internal static long ParseNonCanonical<TChar>(ReadOnlySpan<TChar> name, out int charsConsumed, bool notImplicitFile)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            int numberBase = IPAddressParser.Decimal;
            Span<long> parts = stackalloc long[4];
            long currentValue = 0;
            bool atLeastOneChar = false;

            // Parse one dotted section at a time
            int dotCount = 0; // Limit 3
            int current;

            charsConsumed = 0;
            for (current = 0; current < name.Length; current++)
            {
                TChar ch = name[current];
                currentValue = 0;

                // Figure out what base this section is in, default to base 10
                numberBase = IPAddressParser.Decimal;
                // A number starting with zero should be interpreted in base 8 / octal
                // If the number starts with 0x, it should be interpreted in base 16 / hex
                if (ch == TChar.CreateTruncating('0'))
                {
                    numberBase = IPAddressParser.Octal;
                    current++;
                    atLeastOneChar = true;
                    if (current < name.Length)
                    {
                        // Force an uppercase 'X' to lowercase 'x'.
                        ch = name[current] | TChar.CreateTruncating(0x20);

                        if (ch == TChar.CreateTruncating('x'))
                        {
                            numberBase = IPAddressParser.Hex;
                            current++;
                            atLeastOneChar = false;
                        }
                    }
                }

                // Parse this section
                for (; current < name.Length; current++)
                {
                    ch = name[current];

                    if (!IPAddressParser.TryParseInteger(numberBase, ch, out int digitValue))
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

                if (current < name.Length && name[current] == TChar.CreateTruncating('.'))
                {
                    if (dotCount >= 3 // Max of 3 dots and 4 segments
                        || !atLeastOneChar // No empty segmets: 1...1
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
                // We don't get here unless We find an invalid character or a terminator
                break;
            }

            // Terminators
            if (!atLeastOneChar)
            {
                return Invalid;  // Empty trailing segment: 1.1.1.
            }
            else if (current >= name.Length)
            {
                // end of string, allowed
            }
            // For a normal IPv4 address, the terminator is the prefix ('/' or its counterpart, '\'). If notImplicitFile is set, the terminator
            // is one of the characters which signify the start of the rest of the URI - the port number (':'), query string ('?') or fragment ('#')
            else if (name[current] == TChar.CreateTruncating('/') || name[current] == TChar.CreateTruncating('\\')
                    || (notImplicitFile && (name[current] == TChar.CreateTruncating(':') || name[current] == TChar.CreateTruncating('?') || name[current] == TChar.CreateTruncating('#'))))
            {
                charsConsumed = current;
            }
            else
            {
                // not a valid terminating character
                return Invalid;
            }

            parts[dotCount] = currentValue;

            // Parsed, reassemble and check for overflows
            switch (dotCount)
            {
                case 0: // 0xFFFFFFFF
                    if (parts[0] > MaxIPv4Value)
                    {
                        return Invalid;
                    }
                    charsConsumed = current;
                    return parts[0];
                case 1: // 0xFF.0xFFFFFF
                    if (parts[1] > 0xffffff)
                    {
                        return Invalid;
                    }
                    charsConsumed = current;
                    return (parts[0] << 24) | (parts[1] & 0xffffff);
                case 2: // 0xFF.0xFF.0xFFFF
                    if (parts[2] > 0xffff)
                    {
                        return Invalid;
                    }
                    charsConsumed = current;
                    return (parts[0] << 24) | ((parts[1] & 0xff) << 16) | (parts[2] & 0xffff);
                case 3: // 0xFF.0xFF.0xFF.0xFF
                    if (parts[3] > 0xff)
                    {
                        return Invalid;
                    }
                    charsConsumed = current;
                    return (parts[0] << 24) | ((parts[1] & 0xff) << 16) | ((parts[2] & 0xff) << 8) | (parts[3] & 0xff);
                default:
                    return Invalid;
            }
        }
    }
}
