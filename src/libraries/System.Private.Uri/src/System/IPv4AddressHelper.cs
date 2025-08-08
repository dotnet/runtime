// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv4AddressHelper
    {
        // methods
        // Parse and canonicalize
        internal static string ParseCanonicalName(string str, int start, int end, ref bool isLoopback)
        {
            // end includes ports, so changedEnd may be different from end
            int changedEnd = end;
            long result;

            unsafe
            {
                fixed (char* ipString = str)
                {
                    result = ParseNonCanonical(ipString, start, ref changedEnd, true);
                }
            }

            Debug.Assert(result != Invalid, $"Failed to parse after already validated: {str}");

            Span<byte> numbers =
                unchecked(
                [
                    (byte)(result >> 24),
                    (byte)(result >> 16),
                    (byte)(result >> 8),
                    (byte)(result)
                ]);

            isLoopback = numbers[0] == 127;

            Span<char> stackSpace = stackalloc char[NumberOfLabels * 3 + 3];
            int totalChars = 0, charsWritten;
            for (int i = 0; i < NumberOfLabels - 1; i++)
            {
                numbers[i].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
                int periodPos = totalChars + charsWritten;
                stackSpace[periodPos] = '.';
                totalChars = periodPos + 1;
            }

            numbers[3].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
            return new string(stackSpace.Slice(0, totalChars + charsWritten));
        }
    }
}
