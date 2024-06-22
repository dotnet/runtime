// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal static string ParseCanonicalName<TChar>(ReadOnlySpan<TChar> str, ref bool isLoopback)
            where TChar : unmanaged, IBinaryInteger<TChar>
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
        private static bool Parse<TChar>(ReadOnlySpan<TChar> name, Span<byte> numbers)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // "name" parameter includes ports, so bytesConsumed may be different from span length
            long result = ParseNonCanonical(name, out _, true);

            Debug.Assert(result != Invalid, $"Failed to parse after already validated: {string.Join(string.Empty, name.ToArray())}");

            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(numbers, (uint)result);

            return numbers[0] == 127;
        }
    }
}
