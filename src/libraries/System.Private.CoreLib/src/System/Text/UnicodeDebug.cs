// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Text
{
    internal static class UnicodeDebug
    {
        [Conditional("DEBUG")]
        internal static void AssertIsBmpCodePoint(uint codePoint)
        {
            Debug.Assert(UnicodeUtility.IsBmpCodePoint(codePoint), $"The value {ToHexString(codePoint)} is not a valid BMP code point.");
        }

        [Conditional("DEBUG")]
        internal static void AssertIsHighSurrogateCodePoint(uint codePoint)
        {
            Debug.Assert(UnicodeUtility.IsHighSurrogateCodePoint(codePoint), $"The value {ToHexString(codePoint)} is not a valid UTF-16 high surrogate code point.");
        }

        [Conditional("DEBUG")]
        internal static void AssertIsLowSurrogateCodePoint(uint codePoint)
        {
            Debug.Assert(UnicodeUtility.IsLowSurrogateCodePoint(codePoint), $"The value {ToHexString(codePoint)} is not a valid UTF-16 low surrogate code point.");
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidCodePoint(uint codePoint)
        {
            Debug.Assert(UnicodeUtility.IsValidCodePoint(codePoint), $"The value {ToHexString(codePoint)} is not a valid Unicode code point.");
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidScalar(uint scalarValue)
        {
            Debug.Assert(UnicodeUtility.IsValidUnicodeScalar(scalarValue), $"The value {ToHexString(scalarValue)} is not a valid Unicode scalar value.");
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidSupplementaryPlaneScalar(uint scalarValue)
        {
            Debug.Assert(
                UnicodeUtility.IsValidUnicodeScalar(scalarValue) && !UnicodeUtility.IsBmpCodePoint(scalarValue),
                $"The value {ToHexString(scalarValue)} is not a valid supplementary plane Unicode scalar value.");
        }

        /// <summary>
        /// Formats a code point as the hex string "U+XXXX".
        /// </summary>
        /// <remarks>
        /// The input value doesn't have to be a real code point in the Unicode codespace. It can be any integer.
        /// </remarks>
        private static string ToHexString(uint codePoint) => $"U+{codePoint:X4}";
    }
}
