// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv4AddressHelper<TChar>
    {
        // methods
        // Parse and canonicalize
        internal static string ParseCanonicalName(ReadOnlySpan<TChar> str, ref bool isLoopback)
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
        private static bool Parse(ReadOnlySpan<TChar> name, Span<byte> numbers)
        {
            // "name" parameter includes ports, so bytesConsumed may be different from span length
            int bytesConsumed = 0;
            long result = ParseNonCanonical(name, ref bytesConsumed, true);

            Debug.Assert(result != Invalid, $"Failed to parse after already validated: {string.Join(string.Empty, name.ToArray())}");

            unchecked
            {
                numbers[0] = (byte)(result >> 24);
                numbers[1] = (byte)(result >> 16);
                numbers[2] = (byte)(result >> 8);
                numbers[3] = (byte)(result);
            }

            return numbers[0] == 127;
        }
    }
}
