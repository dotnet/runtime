// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class IPv6AddressHelper
    {
        private const int NumberOfLabels = 8;

        // RFC 5952 Section 4.2.3
        // Longest consecutive sequence of zero segments, minimum 2.
        // On equal, first sequence wins. <-1, -1> for no compression.
        internal unsafe static (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(
            ushort[] numbers, int fromInclusive, int toExclusive)
        {
            Debug.Assert(fromInclusive >= 0);
            Debug.Assert(toExclusive <= NumberOfLabels);
            Debug.Assert(fromInclusive <= toExclusive);

            int longestSequenceLength = 0, longestSequenceStart = -1, currentSequenceLength = 0;

            for (int i = fromInclusive; i < toExclusive; i++)
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
                (-1, -1);
        }

        // Returns true if the IPv6 address should be formatted with an embedded IPv4 address:
        // ::192.168.1.1
        internal unsafe static bool ShouldHaveIpv4Embedded(ushort[] numbers)
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
        internal unsafe static bool IsValidStrict(char* name, int start, ref int end)
        {
            int sequenceCount = 0;
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool expectingNumber = true;
            int lastSequence = 1;

            bool needsClosingBracket = false;
            if (start < end && name[start] == '[')
            {
                start++;
                needsClosingBracket = true;
            }

            int i;
            for (i = start; i < end; ++i)
            {
                if (Uri.IsHexDigit(name[i]))
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
                    switch (name[i])
                    {
                        case '%':
                            while (i+1 < end)
                            {
                                i++;
                                if (name[i] == ']')
                                {
                                    goto case ']';
                                }
                                else if (name[i] == '/')
                                {
                                    goto case '/';
                                }
                                else if (name[i] < '0' || name[i] > '9')
                                {
                                    // scope ID must only contain digits
                                    return false;
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
                            if (i + 1 < end && name[i + 1] != ':')
                            {
                                return false;
                            }

                            // If there is a port, it must either be a hexadecimal or decimal number.
                            if (i + 3 < end && name[i + 2] == '0' && name[i + 3] == 'x')
                            {
                                i += 4;
                                for (; i < end; i++)
                                {
                                    if (!Uri.IsHexDigit(name[i]))
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
                                    if (name[i] < '0' || name[i] > '9')
                                    {
                                        return false;
                                    }
                                }
                            }
                            continue;
                        case ':':
                            if ((i > 0) && (name[i - 1] == ':'))
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
                            // ipv4 address takes 2 slots in ipv6 address, one was just counted meeting the '.'
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

            // these sequence counts are -1 because it is implied in end-of-sequence

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
        //  <member>    PrefixLength
        //      Set to the number after the prefix separator (/) if found
        //
        // Assumes:
        //  <Name> has been validated and contains only hex digits in groups of
        //  16-bit numbers, the characters ':' and '/', and a possible IPv4
        //  address
        //
        // Throws:
        //  Nothing
        //

        internal static unsafe void Parse(ReadOnlySpan<char> address, ushort* numbers, int start, ref string scopeId)
        {
            int number = 0;
            int index = 0;
            int compressorIndex = -1;
            bool numberIsValid = true;

            //This used to be a class instance member but have not been used so far
            int PrefixLength = 0;
            if (address[start] == '[')
            {
                ++start;
            }

            for (int i = start; i < address.Length && address[i] != ']';)
            {
                switch (address[i])
                {
                    case '%':
                        if (numberIsValid)
                        {
                            numbers[index++] = (ushort)number;
                            numberIsValid = false;
                        }

                        start = i;
                        for (++i; i < address.Length && address[i] != ']' && address[i] != '/'; ++i)
                        {
                        }
                        scopeId = new string(address.Slice(start, i - start));
                        // ignore prefix if any
                        for (; i < address.Length && address[i] != ']'; ++i)
                        {
                        }
                        break;

                    case ':':
                        numbers[index++] = (ushort)number;
                        number = 0;
                        ++i;
                        if (address[i] == ':')
                        {
                            compressorIndex = index;
                            ++i;
                        }
                        else if ((compressorIndex < 0) && (index < 6))
                        {
                            // no point checking for IPv4 address if we don't
                            // have a compressor or we haven't seen 6 16-bit
                            // numbers yet
                            break;
                        }

                        // check to see if the upcoming number is really an IPv4
                        // address. If it is, convert it to 2 ushort numbers
                        for (int j = i; j < address.Length &&
                                        (address[j] != ']') &&
                                        (address[j] != ':') &&
                                        (address[j] != '%') &&
                                        (address[j] != '/') &&
                                        (j < i + 4); ++j)
                        {

                            if (address[j] == '.')
                            {
                                // we have an IPv4 address. Find the end of it:
                                // we know that since we have a valid IPv6
                                // address, the only things that will terminate
                                // the IPv4 address are the prefix delimiter '/'
                                // or the end-of-string (which we conveniently
                                // delimited with ']')
                                while (j < address.Length && (address[j] != ']') && (address[j] != '/') && (address[j] != '%'))
                                {
                                    ++j;
                                }
                                number = IPv4AddressHelper.ParseHostNumber(address, i, j);
                                numbers[index++] = (ushort)(number >> 16);
                                numbers[index++] = (ushort)number;
                                i = j;

                                // set this to avoid adding another number to
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
                            numbers[index++] = (ushort)number;
                            numberIsValid = false;
                        }

                        // since we have a valid IPv6 address string, the prefix
                        // length is the last token in the string
                        for (++i; address[i] != ']'; ++i)
                        {
                            PrefixLength = PrefixLength * 10 + (address[i] - '0');
                        }
                        break;

                    default:
                        number = number * 16 + Uri.FromHex(address[i++]);
                        break;
                }
            }

            // add number to the array if its not the prefix length or part of
            // an IPv4 address that's already been handled
            if (numberIsValid)
            {
                numbers[index++] = (ushort)number;
            }

            // if we had a compressor sequence ("::") then we need to expand the
            // numbers array
            if (compressorIndex > 0)
            {
                int toIndex = NumberOfLabels - 1;
                int fromIndex = index - 1;

                for (int i = index - compressorIndex; i > 0; --i)
                {
                    numbers[toIndex--] = numbers[fromIndex];
                    numbers[fromIndex--] = 0;
                }
            }
        }
    }
}
