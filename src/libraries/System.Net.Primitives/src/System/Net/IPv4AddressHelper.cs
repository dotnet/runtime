// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static class IPv4AddressHelper
    {
        internal const long Invalid = -1;
        private const long MaxIPv4Value = uint.MaxValue; // the native parser cannot handle MaxIPv4Value, only MaxIPv4Value - 1
        private const int Octal = 8;
        private const int Decimal = 10;
        private const int Hex = 16;

        private const int NumberOfLabels = 4;

        // Only called from the IPv6Helper, only parse the canonical format
        internal static unsafe int ParseHostNumber(ReadOnlySpan<char> str, int start, int end)
        {
            byte* numbers = stackalloc byte[NumberOfLabels];
            ParseCanonical(str, numbers, start, end);
            return (numbers[0] << 24) + (numbers[1] << 16) + (numbers[2] << 8) + numbers[3];
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

        //Remark: MUST NOT be used unless all input indexes are are verified and trusted.
        internal static unsafe bool IsValid(char* name, int start, ref int end, bool allowIPv6, bool notImplicitFile, bool unknownScheme)
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
        internal static unsafe bool IsValidCanonical(char* name, int start, ref int end, bool allowIPv6, bool notImplicitFile)
        {
            int dots = 0;
            int number = 0;
            bool haveNumber = false;
            bool firstCharIsZero = false;

            while (start < end)
            {
                char ch = name[start];
                if (allowIPv6)
                {
                    // for ipv4 inside ipv6 the terminator is either ScopeId, prefix or ipv6 terminator
                    if (ch == ']' || ch == '/' || ch == '%')
                        break;
                }
                else if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
                {
                    break;
                }

                if (ch <= '9' && ch >= '0')
                {
                    if (!haveNumber && (ch == '0'))
                    {
                        if ((start + 1 < end) && name[start + 1] == '0')
                        {
                            // 00 is not allowed as a prefix.
                            return false;
                        }

                        firstCharIsZero = true;
                    }

                    haveNumber = true;
                    number = number * 10 + (name[start] - '0');
                    if (number > 255)
                    {
                        return false;
                    }
                }
                else if (ch == '.')
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
        internal static unsafe long ParseNonCanonical(char* name, int start, ref int end, bool notImplicitFile)
        {
            int numberBase = Decimal;
            char ch;
            long* parts = stackalloc long[4];
            long currentValue = 0;
            bool atLeastOneChar = false;

            // Parse one dotted section at a time
            int dotCount = 0; // Limit 3
            int current = start;
            for (; current < end; current++)
            {
                ch = name[current];
                currentValue = 0;

                // Figure out what base this section is in
                numberBase = Decimal;
                if (ch == '0')
                {
                    numberBase = Octal;
                    current++;
                    atLeastOneChar = true;
                    if (current < end)
                    {
                        ch = name[current];
                        if (ch == 'x' || ch == 'X')
                        {
                            numberBase = Hex;
                            current++;
                            atLeastOneChar = false;
                        }
                    }
                }

                // Parse this section
                for (; current < end; current++)
                {
                    ch = name[current];
                    int digitValue;

                    if ((numberBase == Decimal || numberBase == Hex) && '0' <= ch && ch <= '9')
                    {
                        digitValue = ch - '0';
                    }
                    else if (numberBase == Octal && '0' <= ch && ch <= '7')
                    {
                        digitValue = ch - '0';
                    }
                    else if (numberBase == Hex && 'a' <= ch && ch <= 'f')
                    {
                        digitValue = ch + 10 - 'a';
                    }
                    else if (numberBase == Hex && 'A' <= ch && ch <= 'F')
                    {
                        digitValue = ch + 10 - 'A';
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

                if (current < end && name[current] == '.')
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
            else if (current >= end)
            {
                // end of string, allowed
            }
            else if ((ch = name[current]) == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
            {
                end = current;
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
                    return parts[0];
                case 1: // 0xFF.0xFFFFFF
                    if (parts[1] > 0xffffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] & 0xffffff);
                case 2: // 0xFF.0xFF.0xFFFF
                    if (parts[2] > 0xffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | ((parts[1] & 0xff) << 16) | (parts[2] & 0xffff);
                case 3: // 0xFF.0xFF.0xFF.0xFF
                    if (parts[3] > 0xff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | ((parts[1] & 0xff) << 16) | ((parts[2] & 0xff) << 8) | (parts[3] & 0xff);
                default:
                    return Invalid;
            }
        }

        // Assumes:
        //  <Name> has been validated and contains only decimal digits in groups
        //  of 8-bit numbers and the characters '.'
        //  Address may terminate with ':' or with the end of the string
        //
        private static unsafe bool ParseCanonical(ReadOnlySpan<char> name, byte* numbers, int start, int end)
        {
            for (int i = 0; i < NumberOfLabels; ++i)
            {

                byte b = 0;
                char ch;
                for (; (start < end) && (ch = name[start]) != '.' && ch != ':'; ++start)
                {
                    b = (byte)(b * 10 + (byte)(ch - '0'));
                }
                numbers[i] = b;
                ++start;
            }
            return numbers[0] == 127;
        }
    }
}
