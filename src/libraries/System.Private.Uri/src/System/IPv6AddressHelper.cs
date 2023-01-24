// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv6AddressHelper
    {
        internal static unsafe string ParseCanonicalName(string str, int start, ref bool isLoopback, ref string? scopeId)
        {
            Span<ushort> numbers = stackalloc ushort[NumberOfLabels];
            numbers.Clear();
            Parse(str, numbers, start, ref scopeId);
            isLoopback = IsLoopback(numbers);

            // RFC 5952 Sections 4 & 5 - Compressed, lower case, with possible embedded IPv4 addresses.

            // Start to finish, inclusive.  <-1, -1> for no compression
            (int rangeStart, int rangeEnd) = FindCompressionRange(numbers);
            bool ipv4Embedded = ShouldHaveIpv4Embedded(numbers);

            Span<char> stackSpace = stackalloc char[48]; // large enough for any IPv6 string, including brackets
            stackSpace[0] = '[';
            int pos = 1;
            int charsWritten;
            bool success;
            for (int i = 0; i < NumberOfLabels; i++)
            {
                if (ipv4Embedded && i == (NumberOfLabels - 2))
                {
                    stackSpace[pos++] = ':';

                    // Write the remaining digits as an IPv4 address
                    success = (numbers[i] >> 8).TryFormat(stackSpace.Slice(pos), out charsWritten);
                    Debug.Assert(success);
                    pos += charsWritten;

                    stackSpace[pos++] = '.';
                    success = (numbers[i] & 0xFF).TryFormat(stackSpace.Slice(pos), out charsWritten);
                    Debug.Assert(success);
                    pos += charsWritten;

                    stackSpace[pos++] = '.';
                    success = (numbers[i + 1] >> 8).TryFormat(stackSpace.Slice(pos), out charsWritten);
                    Debug.Assert(success);
                    pos += charsWritten;

                    stackSpace[pos++] = '.';
                    success = (numbers[i + 1] & 0xFF).TryFormat(stackSpace.Slice(pos), out charsWritten);
                    Debug.Assert(success);
                    pos += charsWritten;
                    break;
                }

                // Compression; 1::1, ::1, 1::
                if (rangeStart == i)
                {
                    // Start compression, add :
                    stackSpace[pos++] = ':';
                }

                if (rangeStart <= i && rangeEnd == NumberOfLabels)
                {
                    // Remainder compressed; 1::
                    stackSpace[pos++] = ':';
                    break;
                }

                if (rangeStart <= i && i < rangeEnd)
                {
                    continue; // Compressed
                }

                if (i != 0)
                {
                    stackSpace[pos++] = ':';
                }
                success = numbers[i].TryFormat(stackSpace.Slice(pos), out charsWritten, format: "x");
                Debug.Assert(success);
                pos += charsWritten;
            }

            stackSpace[pos++] = ']';
            return new string(stackSpace.Slice(0, pos));
        }

        private static unsafe bool IsLoopback(ReadOnlySpan<ushort> numbers)
        {
            //
            // is the address loopback? Loopback is defined as one of:
            //
            //  0:0:0:0:0:0:0:1
            //  0:0:0:0:0:0:127.0.0.1       == 0:0:0:0:0:0:7F00:0001
            //  0:0:0:0:0:FFFF:127.0.0.1    == 0:0:0:0:0:FFFF:7F00:0001
            //

            return ((numbers[0] == 0)
                            && (numbers[1] == 0)
                            && (numbers[2] == 0)
                            && (numbers[3] == 0)
                            && (numbers[4] == 0))
                           && (((numbers[5] == 0)
                                && (numbers[6] == 0)
                                && (numbers[7] == 1))
                               || (((numbers[6] == 0x7F00)
                                    && (numbers[7] == 0x0001))
                                   && ((numbers[5] == 0)
                                       || (numbers[5] == 0xFFFF))));
        }

        //
        // InternalIsValid
        //
        //  Determine whether a name is a valid IPv6 address. Rules are:
        //
        //   *  8 groups of 16-bit hex numbers, separated by ':'
        //   *  a *single* run of zeros can be compressed using the symbol '::'
        //   *  an optional string of a ScopeID delimited by '%'
        //   *  an optional (last) 1 or 2 character prefix length field delimited by '/'
        //   *  the last 32 bits in an address can be represented as an IPv4 address
        //
        // Inputs:
        //  <argument>  name
        //      Domain name field of a URI to check for pattern match with
        //      IPv6 address
        //  validateStrictAddress: if set to true, it expects strict ipv6 address. Otherwise it expects
        //      part of the string in ipv6 format.
        //
        // Outputs:
        //  Nothing
        //
        // Assumes:
        //  the correct name is terminated by  ']' character
        //
        // Returns:
        //  true if <name> has IPv6 format/ipv6 address based on validateStrictAddress, else false
        //
        // Throws:
        //  Nothing
        //

        //  Remarks: MUST NOT be used unless all input indexes are verified and trusted.
        //           start must be next to '[' position, or error is reported
        private static unsafe bool InternalIsValid(char* name, int start, ref int end, bool validateStrictAddress)
        {
            int sequenceCount = 0;
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool havePrefix = false;
            bool expectingNumber = true;
            int lastSequence = 1;

            // Starting with a colon character is only valid if another colon follows.
            if (name[start] == ':' && (start + 1 >= end || name[start + 1] != ':'))
            {
                return false;
            }

            int i;
            for (i = start; i < end; ++i)
            {
                if (havePrefix ? char.IsAsciiDigit(name[i]) : char.IsAsciiHexDigit(name[i]))
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
                    }
                    switch (name[i])
                    {
                        case '%':
                            while (true)
                            {
                                //accept anything in scopeID
                                if (++i == end)
                                {
                                    // no closing ']', fail
                                    return false;
                                }
                                if (name[i] == ']')
                                {
                                    goto case ']';
                                }
                                else if (name[i] == '/')
                                {
                                    goto case '/';
                                }
                            }
                        case ']':
                            start = i;
                            i = end;
                            //this will make i = end+1
                            continue;
                        case ':':
                            if ((i > 0) && (name[i - 1] == ':'))
                            {
                                if (haveCompressor)
                                {
                                    //
                                    // can only have one per IPv6 address
                                    //

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
                            if (validateStrictAddress)
                            {
                                return false;
                            }
                            if ((sequenceCount == 0) || havePrefix)
                            {
                                return false;
                            }
                            havePrefix = true;
                            expectingNumber = true;
                            break;

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
                            haveIPv4Address = true;
                            --i;            // it will be incremented back on the next loop
                            break;

                        default:
                            return false;
                    }
                    sequenceLength = 0;
                }
            }

            //
            // if the last token was a prefix, check number of digits
            //

            if (havePrefix && ((sequenceLength < 1) || (sequenceLength > 2)))
            {
                return false;
            }

            //
            // these sequence counts are -1 because it is implied in end-of-sequence
            //

            int expectedSequenceCount = 8 + (havePrefix ? 1 : 0);

            if (!expectingNumber && (sequenceLength <= 4) && (haveCompressor ? (sequenceCount < expectedSequenceCount) : (sequenceCount == expectedSequenceCount)))
            {
                if (i == end + 1)
                {
                    // ']' was found
                    end = start + 1;
                    return true;
                }
                return false;
            }
            return false;
        }

        //
        // IsValid
        //
        //  Determine whether a name is a valid IPv6 address. Rules are:
        //
        //   *  8 groups of 16-bit hex numbers, separated by ':'
        //   *  a *single* run of zeros can be compressed using the symbol '::'
        //   *  an optional string of a ScopeID delimited by '%'
        //   *  an optional (last) 1 or 2 character prefix length field delimited by '/'
        //   *  the last 32 bits in an address can be represented as an IPv4 address
        //
        // Inputs:
        //  <argument>  name
        //      Domain name field of a URI to check for pattern match with
        //      IPv6 address
        //
        // Outputs:
        //  Nothing
        //
        // Assumes:
        //  the correct name is terminated by  ']' character
        //
        // Returns:
        //  true if <name> has IPv6 format, else false
        //
        // Throws:
        //  Nothing
        //

        //  Remarks: MUST NOT be used unless all input indexes are verified and trusted.
        //           start must be next to '[' position, or error is reported

        internal static unsafe bool IsValid(char* name, int start, ref int end)
        {
            return InternalIsValid(name, start, ref end, false);
        }
    }
}
