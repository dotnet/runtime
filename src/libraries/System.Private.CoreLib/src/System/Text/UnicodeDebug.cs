// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text
{
    internal static class UnicodeDebug
    {
        [Conditional("DEBUG")]
        internal static void AssertIsBmpCodePoint(uint codePoint)
        {
            if (!UnicodeUtility.IsBmpCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid BMP code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsHighSurrogateCodePoint(uint codePoint)
        {
            if (!UnicodeUtility.IsHighSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 high surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsLowSurrogateCodePoint(uint codePoint)
        {
            if (!UnicodeUtility.IsLowSurrogateCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid UTF-16 low surrogate code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidCodePoint(uint codePoint)
        {
            if (!UnicodeUtility.IsValidCodePoint(codePoint))
            {
                Debug.Fail($"The value {ToHexString(codePoint)} is not a valid Unicode code point.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidScalar(uint scalarValue)
        {
            if (!UnicodeUtility.IsValidUnicodeScalar(scalarValue))
            {
                Debug.Fail($"The value {ToHexString(scalarValue)} is not a valid Unicode scalar value.");
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertIsValidSupplementaryPlaneScalar(uint scalarValue)
        {
            if (!UnicodeUtility.IsValidUnicodeScalar(scalarValue) || UnicodeUtility.IsBmpCodePoint(scalarValue))
            {
                Debug.Fail($"The value {ToHexString(scalarValue)} is not a valid supplementary plane Unicode scalar value.");
            }
        }

        /// <summary>
        /// Formats a code point as the hex string "U+XXXX".
        /// </summary>
        /// <remarks>
        /// The input value doesn't have to be a real code point in the Unicode codespace. It can be any integer.
        /// </remarks>
        private static string ToHexString(uint codePoint)
        {
            return FormattableString.Invariant($"U+{codePoint:X4}");
        }
    }
}
