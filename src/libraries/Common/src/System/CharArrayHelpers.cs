// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    internal static class CharArrayHelpers
    {
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
