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

        private const int Octal = 8;
        private const int Decimal = 10;
        private const int Hex = 16;

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
                int ch;

                for (; (start < str.Length) && (ch = int.CreateTruncating(str[start])) != '.' && ch != ':'; ++start)
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

                int parsedCharacter = int.CreateTruncating(ch) - '0';

                if (parsedCharacter >= 0 && parsedCharacter <= 9)
                {
                    // A number starting with zero should be interpreted in base 8 / octal
                    if (!haveNumber && parsedCharacter == 0)
                    {
                        if (current + 1 < name.Length && name[current + 1] == TChar.CreateTruncating('0'))
                        {
                            // 00 is not allowed as a prefix.
                            return false;
                        }

                        firstCharIsZero = true;
                    }

                    haveNumber = true;
                    number = number * Decimal + parsedCharacter;
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
            int numberBase = IPv4AddressHelper.Decimal;
            Span<uint> parts = stackalloc uint[4];
            long currentValue = 0;
            bool atLeastOneChar = false;

            // Parse one dotted section at a time
            int dotCount = 0; // Limit 3
            int current;

            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            charsConsumed = 0;
            for (current = 0; current < name.Length; current++)
            {
                TChar ch = name[current];
                int maxCharacterValue = '9';
                currentValue = 0;

                // Figure out what base this section is in, default to base 10
                numberBase = IPv4AddressHelper.Decimal;
                // A number starting with zero should be interpreted in base 8 / octal
                // If the number starts with 0x, it should be interpreted in base 16 / hex
                if (ch == TChar.CreateTruncating('0'))
                {
                    current++;
                    atLeastOneChar = true;
                    if (current < name.Length)
                    {
                        ch = name[current];

                        if ((ch == TChar.CreateTruncating('x')) || (ch == TChar.CreateTruncating('X')))
                        {
                            numberBase = IPv4AddressHelper.Hex;

                            current++;
                            atLeastOneChar = false;
                        }
                        else
                        {
                            numberBase = IPv4AddressHelper.Octal;
                            maxCharacterValue = '7';
                        }
                    }
                }

                // Parse this section
                for (; current < name.Length; current++)
                {
                    ch = name[current];
                    int characterValue = int.CreateTruncating(ch);
                    int digitValue;

                    if (characterValue >= '0' && characterValue <= maxCharacterValue)
                    {
                        digitValue = characterValue - '0';
                    }
                    else if (numberBase == IPv4AddressHelper.Hex)
                    {
                        if (characterValue >= 'a' && characterValue <= 'f')
                        {
                            digitValue = 10 + characterValue - 'a';
                        }
                        else if (characterValue >= 'A' && characterValue <= 'F')
                        {
                            digitValue = 10 + characterValue - 'A';
                        }
                        else
                        {
                            break; // Invalid/terminator
                        }
                    }
                    else
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

                if (ch == TChar.CreateTruncating('.'))
                {
                    if (dotCount >= 3 // Max of 3 dots and 4 segments
                        || !atLeastOneChar // No empty segments: 1...1
                                           // Only the last segment can be more than 255 (if there are less than 3 dots)
                        || currentValue > 0xFF)
                    {
                        return Invalid;
                    }
                    parts[dotCount] = unchecked((uint)currentValue);
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
            else if (current >= name.Length)
            {
                // end of string, allowed
            }
            // For a normal IPv4 address, the terminator is the prefix ('/' or its counterpart, '\'). If notImplicitFile is set, the terminator
            // is one of the characters which signify the start of the rest of the URI - the port number (':'), query string ('?') or fragment ('#')
            else if (name[current] == TChar.CreateTruncating('/') || name[current] == TChar.CreateTruncating('\\')
                    || (notImplicitFile && (name[current] == TChar.CreateTruncating(':') || name[current] == TChar.CreateTruncating('?') || name[current] == TChar.CreateTruncating('#'))))
            {
                // end of string, (as terminated) allowed
            }
            else
            {
                // not a valid terminating character
                return Invalid;
            }

            parts[dotCount] = unchecked((uint)currentValue);
            charsConsumed = current;

            // Parsed, reassemble and check for overflows in the last part. Previous parts have already been checked in the loop
            switch (dotCount)
            {
                case 0: // 0xFFFFFFFF
                    return parts[0];
                case 1: // 0xFF.0xFFFFFF
                    if (parts[1] > 0xffffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | parts[1];
                case 2: // 0xFF.0xFF.0xFFFF
                    if (parts[2] > 0xffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | parts[2];
                case 3: // 0xFF.0xFF.0xFF.0xFF
                    if (parts[3] > 0xff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | (parts[2] << 8) | parts[3];
                default:
                    return Invalid;
            }
        }
    }
}
