// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
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
        /// <param name="value">The <see cref="ReadOnlySpan{T}"/> string.</param>
        /// <returns><c>true</c> if value is well-formed UTF-16, <c>false</c> otherwise.</returns>
        public static bool IsValid(ReadOnlySpan<char> value) =>
            IndexOfInvalidSubsequence(value) < 0;

        /// <summary>
        /// Find the index of the first invalid UTF-16 subsequence.
        /// </summary>
        /// <param name="value">The <see cref="ReadOnlySpan{T}"/> string.</param>
        /// <returns>The index of the first invalid UTF-16 subsequence, or <c>-1</c> if the entire input is valid.</returns>
        public static unsafe int IndexOfInvalidSubsequence(ReadOnlySpan<char> value)
        {
            fixed (char* pValue = &MemoryMarshal.GetReference(value))
            {
                char* pFirstInvalidChar = Utf16Utility.GetPointerToFirstInvalidChar(pValue, value.Length, out _, out _);
                int index = (int)(pFirstInvalidChar - pValue);

                return (index < value.Length) ? index : -1;
            }
        }
    }
}
