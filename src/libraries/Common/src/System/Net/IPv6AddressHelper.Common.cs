// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static partial class IPv6AddressHelper
    {
        private const int Decimal = 10;
        private const int Hex = 16;
        private const int NumberOfLabels = 8;

        // RFC 5952 Section 4.2.3
        // Longest consecutive sequence of zero segments, minimum 2.
        // On equal, first sequence wins. <-1, -1> for no compression.
        internal static (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(ReadOnlySpan<ushort> numbers)
        {
            int longestSequenceLength = 0, longestSequenceStart = -1, currentSequenceLength = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                if (numbers[i] == 0)
                {
                    currentSequenceLength++;
                    if (currentSequenceLength > longestSequenceLength)
                    {
                        longestSequenceLength = currentSequenceLength;
                        longestSequenceStart = i - currentSequenceLength + 1;
                    }
                }
                else
                {
                    currentSequenceLength = 0;
                }
            }

            return longestSequenceLength > 1 ?
                (longestSequenceStart, longestSequenceStart + longestSequenceLength) :
                (-1, 0);
        }

        // Returns true if the IPv6 address should be formatted with an embedded IPv4 address:
        // ::192.168.1.1
        internal static bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers)
        {
            // 0:0 : 0:0 : x:x : x.x.x.x
            if (numbers[0] == 0 && numbers[1] == 0 && numbers[2] == 0 && numbers[3] == 0 && numbers[6] != 0)
            {
                // RFC 5952 Section 5 - 0:0 : 0:0 : 0:[0 | FFFF] : x.x.x.x
                if (numbers[4] == 0 && (numbers[5] == 0 || numbers[5] == 0xFFFF))
                {
                    return true;
                }
                // SIIT - 0:0 : 0:0 : FFFF:0 : x.x.x.x
                else if (numbers[4] == 0xFFFF && numbers[5] == 0)
                {
                    return true;
                }
            }

            // ISATAP
            return numbers[4] == 0 && numbers[5] == 0x5EFE;
        }

        //
        // IsValidStrict
        //
        //  Determine whether a name is a valid IPv6 address. Rules are:
        //
        //   *  8 groups of 16-bit hex numbers, separated by ':'
        //   *  a *single* run of zeros can be compressed using the symbol '::'
        //   *  an optional string of a ScopeID delimited by '%'
        //   *  the last 32 bits in an address can be represented as an IPv4 address
        //
        //  Difference between IsValid() and IsValidStrict() is that IsValid() expects part of the string to
        //  be ipv6 address where as IsValidStrict() expects strict ipv6 address.
        //
        // Inputs:
        //  <argument>  name
        //      IPv6 address in string format
        //
        // Outputs:
        //  Nothing
        //
        // Assumes:
        //  the correct name is terminated by  ']' character
        //
        // Returns:
        //  true if <name> is IPv6  address, else false
        //
        // Throws:
        //  Nothing
        //

        //  Remarks: MUST NOT be used unless all input indexes are verified and trusted.
        //           start must be next to '[' position, or error is reported
        internal static unsafe bool IsValidStrict<TChar>(TChar* name, int start, ref int end)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Number of components in this IPv6 address
            int sequenceCount = 0;
            // Length of the component currently being constructed
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool expectingNumber = true;
            // Start position of the previous component
            int lastSequence = 1;

            bool needsClosingBracket = false;

            // An IPv6 address may begin with a start character ('['). If it does, it must end with an end
            // character (']').
            if (start < end && name[start] == TChar.CreateTruncating('['))
            {
                start++;
                needsClosingBracket = true;

                // IsValidStrict() is only called if there is a ':' in the name string, i.e.
                // it is a possible IPv6 address. So, if the string starts with a '[' and
                // the pointer is advanced here there are still more characters to parse.
                Debug.Assert(start < end);
            }

            // Starting with a colon character is only valid if another colon follows.
            if (name[start] == TChar.CreateTruncating(':') && (start + 1 >= end || name[start + 1] != TChar.CreateTruncating(':')))
            {
                return false;
            }

            int i;
            for (i = start; i < end; ++i)
            {
                int currentCh = int.CreateTruncating(name[i]);

                if (HexConverter.IsHexChar(currentCh))
                {
                    ++sequenceLength;
                    expectingNumber = false;
                }
                else
                {
                    if (sequenceLength > 4)
                    {
                        return false;
                    }
                    if (sequenceLength != 0)
                    {
                        ++sequenceCount;
                        lastSequence = i - sequenceLength;
                        sequenceLength = 0;
                    }

                    switch (currentCh)
                    {
                        case '%':
                            // An IPv6 address is separated from its scope by a '%' character. The scope
                            // is terminated by the natural end of the address, the address end character (']')
                            // or the start of the prefix ('/').
                            while (i + 1 < end)
                            {
                                i++;
                                if (name[i] == TChar.CreateTruncating(']'))
                                {
                                    goto case ']';
                                }
                                else if (name[i] == TChar.CreateTruncating('/'))
                                {
                                    goto case '/';
                                }
                            }
                            break;

                        case ']':
                            if (!needsClosingBracket)
                            {
                                return false;
                            }
                            needsClosingBracket = false;

                            // If there's more after the closing bracket, it must be a port.
                            // We don't use the port, but we still validate it.
                            if (i + 1 < end && name[i + 1] != TChar.CreateTruncating(':'))
                            {
                                return false;
                            }

                            // If there is a port, it must either be a hexadecimal or decimal number.
                            // If the next two characters are '0x' then it's a hexadecimal number. Skip the prefix.
                            if (i + 3 < end && name[i + 2] == TChar.CreateTruncating('0') && name[i + 3] == TChar.CreateTruncating('x'))
                            {
                                i += 4;
                                for (; i < end; i++)
                                {
                                    int ch = int.CreateTruncating(name[i]);

                                    if (!HexConverter.IsHexChar(ch))
                                    {
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                i += 2;
                                for (; i < end; i++)
                                {
                                    if (uint.CreateTruncating(name[i] - TChar.CreateTruncating('0')) >= IPv6AddressHelper.Decimal)
                                    {
                                        return false;
                                    }
                                }
                            }
                            continue;

                        case ':':
                            // If the next character after a colon is another colon, the address contains a compressor ('::').
                            if ((i > 0) && (name[i - 1] == TChar.CreateTruncating(':')))
                            {
                                if (haveCompressor)
                                {
                                    // can only have one per IPv6 address
                                    return false;
                                }
                                haveCompressor = true;
                                expectingNumber = false;
                            }
                            else
                            {
                                expectingNumber = true;
                            }
                            break;

                        case '/':
                            // A prefix in an IPv6 address is invalid.
                            return false;

                        case '.':
                            if (haveIPv4Address)
                            {
                                return false;
                            }

                            i = end;
                            if (!IPv4AddressHelper.IsValid(name, lastSequence, ref i, true, false, false))
                            {
                                return false;
                            }
                            // An IPv4 address takes 2 slots in an IPv6 address. One was just counted meeting the '.'
                            ++sequenceCount;
                            lastSequence = i - sequenceLength;
                            sequenceLength = 0;
                            haveIPv4Address = true;
                            --i;            // it will be incremented back on the next loop
                            break;

                        default:
                            return false;
                    }
                    sequenceLength = 0;
                }
            }

            if (sequenceLength != 0)
            {
                if (sequenceLength > 4)
                {
                    return false;
                }

                ++sequenceCount;
            }

            // These sequence counts are -1 because it is implied in end-of-sequence.

            const int ExpectedSequenceCount = 8;
            return
                !expectingNumber &&
                (haveCompressor ? (sequenceCount < ExpectedSequenceCount) : (sequenceCount == ExpectedSequenceCount)) &&
                !needsClosingBracket;
        }

        //
        // Parse
        //
        //  Convert this IPv6 address into a sequence of 8 16-bit numbers
        //
        // Inputs:
        //  <member>    Name
        //      The validated IPv6 address
        //
        // Outputs:
        //  <member>    numbers
        //      Array filled in with the numbers in the IPv6 groups
        //
        //  <member>    scopeId
        //      Set to the text after the scope separator (%) if found
        //
        // Assumes:
        //  <Name> has been validated and contains only hex digits in groups of
        //  16-bit numbers, the characters ':', '/' and '%', and a possible IPv4
        //  address
        //
        // Throws:
        //  Nothing
        //

        internal static void Parse<TChar>(ReadOnlySpan<TChar> address, scoped Span<ushort> numbers, out ReadOnlySpan<TChar> scopeId)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            ushort number = 0;
            int currentCh;
            int index = 0;
            int compressorIndex = -1;
            bool numberIsValid = true;

            scopeId = ReadOnlySpan<TChar>.Empty;
            // Skip the start '[' character, if present. Stop parsing at the end IPv6 address terminator (']').
            for (int i = (address[0] == TChar.CreateTruncating('[') ? 1 : 0); i < address.Length && address[i] != TChar.CreateTruncating(']');)
            {
                currentCh = int.CreateTruncating(address[i]);

                switch (currentCh)
                {
                    case '%':
                        if (numberIsValid)
                        {
                            numbers[index++] = number;
                            numberIsValid = false;
                        }

                        // The scope follows a '%' and terminates at the natural end of the address, the address terminator (']') or the prefix delimiter ('/').
                        int scopeStart = i;

                        for (++i; i < address.Length && address[i] != TChar.CreateTruncating(']') && address[i] != TChar.CreateTruncating('/'); ++i)
                        {
                        }
                        scopeId = address.Slice(scopeStart, i - scopeStart);

                        // Ignore the prefix (if any.)
                        for (; i < address.Length && address[i] != TChar.CreateTruncating(']'); ++i)
                        {
                        }
                        break;

                    case ':':
                        numbers[index++] = number;
                        number = 0;
                        // Two sequential colons form a compressor ('::').
                        ++i;
                        if (address[i] == TChar.CreateTruncating(':'))
                        {
                            compressorIndex = index;
                            ++i;
                        }
                        else if ((compressorIndex < 0) && (index < 6))
                        {
                            // No point checking for IPv4 address if we don't
                            // have a compressor or we haven't seen 6 16-bit
                            // numbers yet.
                            break;
                        }

                        // Check to see if the upcoming number is really an IPv4
                        // address. If it is, convert it to 2 ushort numbers
                        for (int j = i; j < address.Length &&
                                        (address[j] != TChar.CreateTruncating(']')) &&
                                        (address[j] != TChar.CreateTruncating(':')) &&
                                        (address[j] != TChar.CreateTruncating('%')) &&
                                        (address[j] != TChar.CreateTruncating('/')) &&
                                        (j < i + 4); ++j)
                        {

                            if (address[j] == TChar.CreateTruncating('.'))
                            {
                                // We have an IPv4 address. Find the end of it:
                                // we know that since we have a valid IPv6
                                // address, the only things that will terminate
                                // the IPv4 address are the prefix delimiter '/'
                                // or the end-of-string (which we conveniently
                                // delimited with ']').
                                while (j < address.Length && (address[j] != TChar.CreateTruncating(']')) && (address[j] != TChar.CreateTruncating('/')) && (address[j] != TChar.CreateTruncating('%')))
                                {
                                    ++j;
                                }
                                int ipv4Address = IPv4AddressHelper.ParseHostNumber(address, i, j);

                                numbers[index++] = (ushort)(ipv4Address >> 16);
                                numbers[index++] = (ushort)(ipv4Address & 0xFFFF);
                                i = j;

                                // Set this to avoid adding another number to
                                // the array if there's a prefix
                                number = 0;
                                numberIsValid = false;
                                break;
                            }
                        }
                        break;

                    case '/':
                        if (numberIsValid)
                        {
                            numbers[index++] = number;
                            numberIsValid = false;
                        }

                        for (++i; i < address.Length && address[i] != TChar.CreateTruncating(']'); i++)
                        {
                        }

                        break;

                    default:
                        TChar ch = address[i++];
                        int castCharacter = int.CreateTruncating(ch);
                        int characterValue = HexConverter.FromChar(castCharacter);

                        number = (ushort)(number * IPv6AddressHelper.Hex + characterValue);
                        break;
                }
            }

            // Add number to the array if it's not the prefix length or part of
            // an IPv4 address that's already been handled
            if (numberIsValid)
            {
                numbers[index++] = number;
            }

            // If we had a compressor sequence ("::") then we need to expand the
            // numbers array.
            if (compressorIndex > 0)
            {
                int toIndex = NumberOfLabels - 1;
                int fromIndex = index - 1;

                // If fromIndex and toIndex are the same, it means that "zero bits" are already in the correct place.
                // This happens for leading and trailing compression.
                if (fromIndex != toIndex)
                {
                    for (int i = index - compressorIndex; i > 0; --i)
                    {
                        numbers[toIndex--] = numbers[fromIndex];
                        numbers[fromIndex--] = 0;
                    }
                }
            }
        }
    }
}
