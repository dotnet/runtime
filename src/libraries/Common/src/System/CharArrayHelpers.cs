// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal static class CharArrayHelpers
    {
        internal static bool EqualsOrdinalAsciiIgnoreCase(string left, char[] right, int rightStartIndex, int rightLength)
        {
            Debug.Assert(left != null, "Expected non-null string");
            DebugAssertArrayInputs(right, rightStartIndex, rightLength);

            return
                left.Length == rightLength &&
                left.AsSpan().Equals(right.AsSpan(rightStartIndex, rightLength), StringComparison.OrdinalIgnoreCase);
        }

        internal static void Trim(char[] array, ref int startIndex, ref int length)
        {
            DebugAssertArrayInputs(array, startIndex, length);

            int offset = 0;
            while (offset < length && char.IsWhiteSpace(array[startIndex + offset]))
            {
                offset++;
            }

            int end = length - 1;
            while (end >= offset && char.IsWhiteSpace(array[startIndex + end]))
            {
                end--;
            }

            startIndex += offset;
            length = end - offset + 1;
        }

        [Conditional("DEBUG")]
        internal static void DebugAssertArrayInputs(char[] array, int startIndex, int length)
        {
            Debug.Assert(array != null, "Null array");
            Debug.Assert(startIndex >= 0, $"Expected {nameof(startIndex)} to be >= 0, got {startIndex}");
            Debug.Assert(length >= 0, $"Expected {nameof(length)} to be >= 0, got {length}");
            Debug.Assert(startIndex <= array.Length - length, $"Expected {startIndex} to be <= {array.Length} - {length}, got {startIndex}");
        }
    }
}
