// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static partial class IPv4AddressHelper
    {
        // methods
        // Parse and canonicalize
        internal static string ParseCanonicalName(string str, int start, int end, ref bool isLoopback)
        {
            unsafe
            {
                byte* numbers = stackalloc byte[NumberOfLabels];
                isLoopback = Parse(str, numbers, start, end);

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
        }

        //
        // Parse
        //
        //  Convert this IPv4 address into a sequence of 4 8-bit numbers
        //
        private static unsafe bool Parse(string name, byte* numbers, int start, int end)
        {
            fixed (char* ipString = name)
            {
                int changedEnd = end;
                long result = IPv4AddressHelper.ParseNonCanonical(ipString, start, ref changedEnd, true);
                // end includes ports, so changedEnd may be different from end
                Debug.Assert(result != Invalid, "Failed to parse after already validated: " + name);

                unchecked
                {
                    numbers[0] = (byte)(result >> 24);
                    numbers[1] = (byte)(result >> 16);
                    numbers[2] = (byte)(result >> 8);
                    numbers[3] = (byte)(result);
                }
            }

            return numbers[0] == 127;
        }
    }
}
