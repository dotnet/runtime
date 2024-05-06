// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    internal static partial class IPv6AddressHelper<TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        // IPv6 address-specific generic constants.
        public static readonly TChar AddressStartCharacter = TChar.CreateTruncating('[');
        public static readonly TChar AddressEndCharacter = TChar.CreateTruncating(']');
        public static readonly TChar ComponentSeparator = TChar.CreateTruncating(':');
        public static readonly TChar ScopeSeparator = TChar.CreateTruncating('%');
        public static readonly TChar PrefixSeparator = TChar.CreateTruncating('/');
        public static readonly TChar PortSeparator = TChar.CreateTruncating(':');
        public static readonly TChar[] HexadecimalPrefix = [TChar.CreateTruncating('0'), TChar.CreateTruncating('x')];
        public static readonly TChar[] Compressor = [ComponentSeparator, ComponentSeparator];

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
        internal static bool IsValidStrict(ReadOnlySpan<TChar> name)
        {
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
            int start = 0;

            if (start < name.Length && name[start] == AddressStartCharacter)
            {
                start++;
                needsClosingBracket = true;

                // IsValidStrict() is only called if there is a ':' in the name string, i.e.
                // it is a possible IPv6 address. So, if the string starts with a '[' and
                // the pointer is advanced here there are still more characters to parse.
                Debug.Assert(start < name.Length);
            }

            // Starting with a colon character is only valid if another colon follows.
            if (name[start] == Compressor[0] && (start + 1 >= name.Length || name[start + 1] != Compressor[1]))
            {
                return false;
            }

            int i;
            for (i = start; i < name.Length; ++i)
            {
                if (IPAddressParser<TChar>.IsValidInteger(IPAddressParser<TChar>.Hex, name[i]))
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

                    if (name[i] == ScopeSeparator)
                    {
                        bool moveToNextCharacter = true;

                        while (i + 1 < name.Length)
                        {
                            i++;

                            if (name[i] == AddressEndCharacter
                                || name[i] == PrefixSeparator)
                            {
                                moveToNextCharacter = false;
                                break;
                            }
                        }

                        if (moveToNextCharacter)
                        {
                            continue;
                        }
                    }

                    if (name[i] == AddressEndCharacter)
                    {
                        if (!needsClosingBracket)
                        {
                            return false;
                        }
                        needsClosingBracket = false;

                        // If there's more after the closing bracket, it must be a port.
                        // We don't use the port, but we still validate it.
                        if (i + 1 < name.Length && name[i + 1] != PortSeparator)
                        {
                            return false;
                        }

                        int numericBase = IPAddressParser<TChar>.Decimal;

                        // Skip past the closing bracket and the port separator.
                        i += 2;
                        // If there is a port, it must either be a hexadecimal or decimal number.
                        if (i + 1 < name.Length && name.Slice(i).StartsWith(HexadecimalPrefix.AsSpan()))
                        {
                            i += HexadecimalPrefix.Length;

                            numericBase = IPAddressParser<TChar>.Hex;
                        }

                        for (; i < name.Length; i++)
                        {
                            if (!IPAddressParser<TChar>.IsValidInteger(numericBase, name[i]))
                            {
                                return false;
                            }
                        }
                        continue;
                    }
                    else if (name[i] == PrefixSeparator)
                    {
                        return false;
                    }
                    else if (name[i] == ComponentSeparator)
                    {
                        if (i > 0 && name.Slice(i - 1, 2).SequenceEqual(Compressor.AsSpan()))
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

                        sequenceLength = 0;
                        continue;
                    }
                    else if (name[i] == IPv4AddressHelper<TChar>.ComponentSeparator)
                    {
                        int ipv4AddressLength = i;

                        if (haveIPv4Address)
                        {
                            return false;
                        }

                        if (!IPv4AddressHelper<TChar>.IsValid(name.Slice(lastSequence), ref ipv4AddressLength, true, false, false))
                        {
                            return false;
                        }
                        i = lastSequence + ipv4AddressLength;
                        // ipv4 address takes 2 slots in ipv6 address, one was just counted meeting the '.'
                        ++sequenceCount;
                        lastSequence = i - sequenceLength;
                        sequenceLength = 0;
                        haveIPv4Address = true;
                        --i;            // it will be incremented back on the next loop

                        continue;
                    }

                    return false;
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

        internal static void Parse(ReadOnlySpan<TChar> address, Span<ushort> numbers, out ReadOnlySpan<TChar> scopeId)
        {
            int number = 0;
            int index = 0;
            int compressorIndex = -1;
            bool numberIsValid = true;

            scopeId = ReadOnlySpan<TChar>.Empty;
            for (int i = (address[0] == AddressStartCharacter ? 1 : 0); i < address.Length && address[i] != AddressEndCharacter;)
            {
                if (address[i] == ScopeSeparator
                    || address[i] == PrefixSeparator)
                {
                    if (numberIsValid)
                    {
                        numbers[index++] = (ushort)number;
                        numberIsValid = false;
                    }

                    if (address[i] == ScopeSeparator)
                    {
                        int scopeStart = i;

                        for (++i; i < address.Length && address[i] != AddressEndCharacter && address[i] != PrefixSeparator; ++i)
                        {
                        }
                        scopeId = address.Slice(scopeStart, i - scopeStart);
                    }
                    // ignore prefix if any
                    for (; i < address.Length && address[i] != AddressEndCharacter; ++i)
                    {
                    }
                }
                else if (address[i] == ComponentSeparator)
                {
                    numbers[index++] = (ushort)number;
                    number = 0;
                    ++i;
                    if (address[i] == Compressor[0])
                    {
                        compressorIndex = index;
                        ++i;
                    }
                    else if ((compressorIndex < 0) && (index < 6))
                    {
                        // no point checking for IPv4 address if we don't
                        // have a compressor or we haven't seen 6 16-bit
                        // numbers yet
                        continue;
                    }

                    // check to see if the upcoming number is really an IPv4
                    // address. If it is, convert it to 2 ushort numbers
                    for (int j = i; j < address.Length &&
                                    (address[j] != AddressEndCharacter) &&
                                    (address[j] != ComponentSeparator) &&
                                    (address[j] != ScopeSeparator) &&
                                    (address[j] != PrefixSeparator) &&
                                    (j < i + 4); ++j)
                    {

                        if (address[j] == IPv4AddressHelper<TChar>.ComponentSeparator)
                        {
                            // we have an IPv4 address. Find the end of it:
                            // we know that since we have a valid IPv6
                            // address, the only things that will terminate
                            // the IPv4 address are the prefix delimiter '/'
                            // or the end-of-string (which we conveniently
                            // delimited with ']')
                            while (j < address.Length && (address[j] != AddressEndCharacter) && (address[j] != PrefixSeparator) && (address[j] != ScopeSeparator))
                            {
                                ++j;
                            }
                            number = IPv4AddressHelper<TChar>.ParseHostNumber(address.Slice(i, j - i));
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
                }
                else if (IPAddressParser<TChar>.TryParseInteger(IPAddressParser<TChar>.Hex, address[i++], out int digit))
                {
                    number = number * IPAddressParser<TChar>.Hex + digit;
                }
                else
                {
                    throw new ArgumentException(null, nameof(digit));
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

                // if fromIndex and toIndex are the same, it means that "zero bits" are already in the correct place
                // it happens for leading and trailing compression
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
