// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv6AddressHelper
    {
        internal static string ParseCanonicalName(ReadOnlySpan<char> str, ref bool isLoopback, out ReadOnlySpan<char> scopeId)
        {
            Span<ushort> numbers = stackalloc ushort[NumberOfLabels];
            numbers.Clear();
            Parse(str, numbers, out scopeId);
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

        private static bool IsLoopback(ReadOnlySpan<ushort> numbers)
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

        /// <summary>
        /// Determine whether a name is a valid IPv6 address. Rules are:
        /// <para>*  8 groups of 16-bit hex numbers, separated by ':'</para>
        /// <para>*  a *single* run of zeros can be compressed using the symbol '::'</para>
        /// <para>*  an optional string of a ScopeID delimited by '%'</para>
        /// <para>*  the last 32 bits in an address can be represented as an IPv4 address</para>
        /// </summary>
        /// <param name="name">The host to validate.</param>
        /// <param name="length">The length of the IPv6 address (index of ']' + 1).</param>
        /// <remarks>Assumes that the caller already checked that the first character is '['.</remarks>
        public static bool IsValid(ReadOnlySpan<char> name, out int length)
        {
            Debug.Assert(name.StartsWith('['));

            length = 0; // Default value in case of failure
            int sequenceCount = 0;
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool expectingNumber = true;
            int lastSequence = 1;

            // Starting with a colon character is only valid if another colon follows.
            if (name.Length < 3 || (name[1] == ':' && name[2] != ':'))
            {
                return false;
            }

            int i;
            for (i = 1; i < name.Length; ++i)
            {
                if (char.IsAsciiHexDigit(name[i]))
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
                                if (++i == name.Length)
                                {
                                    // no closing ']', fail
                                    return false;
                                }

                                if (name[i] == ']')
                                {
                                    goto case ']';
                                }

                                // Our general IPv6 parsing rules are very lenient on the ZoneID.
                                // Since this is the logic specific to Uri, we restrict the set of allowed characters (mainly to exclude delimiters).
                                if (name[i] != '%' && !UriHelper.Unreserved.Contains(name[i]))
                                {
                                    return false;
                                }
                            }

                        case ']':
                            const int ExpectedSequenceCount = 8;

                            if (!expectingNumber &&
                                (haveCompressor ? (sequenceCount < ExpectedSequenceCount) : (sequenceCount == ExpectedSequenceCount)))
                            {
                                length = i + 1;
                                return true;
                            }

                            return false;

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

                        case '.':
                            if (haveIPv4Address)
                            {
                                return false;
                            }

                            i = name.Length;
                            if (!IPv4AddressHelper.IsValid(name.Slice(lastSequence, i - lastSequence), out int seqEnd, true, false, false))
                            {
                                return false;
                            }
                            i = lastSequence + seqEnd;
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

            return false;
        }
    }
}
