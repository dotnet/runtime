// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Text.Unicode
{
    /// <summary>
    /// Provides static methods that validate UTF-16 strings.
    /// </summary>
    public static class Utf16
    {
        /// <summary>
        /// Validates that the value is well-formed UTF-16.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"/> containing the UTF-16 input text to validate.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is well-formed UTF-16, <c>false</c> otherwise.</returns>
        public static bool IsValid(ReadOnlySpan<char> value) =>
            Utf16Utility.GetIndexOfFirstInvalidUtf16Sequence(value) < 0;

        /// <summary>
        /// Finds the index of the first invalid UTF-16 subsequence.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"/> containing the UTF-16 input text to examine.</param>
        /// <returns>The index of the first invalid UTF-16 subsequence, or <c>-1</c> if the entire input is valid.</returns>
        public static int IndexOfInvalidSubsequence(ReadOnlySpan<char> value) =>
            Utf16Utility.GetIndexOfFirstInvalidUtf16Sequence(value);
    }
}
