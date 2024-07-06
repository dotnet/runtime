// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;

namespace System.Net
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv4AddressHelper
    {
        // methods
        // Parse and canonicalize
        internal static string ParseCanonicalName(ReadOnlySpan<char> str, ref bool isLoopback)
        {
            Span<byte> numbers = stackalloc byte[NumberOfLabels];
            isLoopback = Parse(str, numbers);

            Span<char> stackSpace = stackalloc char[NumberOfLabels * 3 + 3];
            int totalChars = 0, charsWritten;
            for (int i = 0; i < 3; i++)
            {
                numbers[i].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
                int periodPos = totalChars + charsWritten;
                stackSpace[periodPos] = '.';
                totalChars = periodPos + 1;
            }
            numbers[3].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
            return new string(stackSpace.Slice(0, totalChars + charsWritten));
        }

        //
        // Parse
        //
        //  Convert this IPv4 address into a sequence of 4 8-bit numbers
        //
        private static bool Parse(ReadOnlySpan<char> name, Span<byte> numbers)
        {
            // "name" parameter includes ports, so charsConsumed may be different from span length
            long result = ParseNonCanonical(name, out _, true);

            Debug.Assert(result != Invalid, $"Failed to parse after already validated: {name}");

            BinaryPrimitives.WriteUInt32BigEndian(numbers, (uint)result);

            return numbers[0] == 127;
        }
    }
}
