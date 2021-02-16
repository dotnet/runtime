// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Text.Encodings.Web.Tests
{
    /// <summary>
    /// Dummy encoder used for unit testing.
    /// </summary>
    public sealed class ConfigurableScalarTextEncoder : TextEncoder
    {
        private readonly Predicate<int> _isScalarAllowed;

        public ConfigurableScalarTextEncoder(Predicate<int> isScalarAllowed)
        {
            _isScalarAllowed = isScalarAllowed;
        }

        public override int MaxOutputCharactersPerInputCharacter => 8; // "[10FFFF]".Length

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
            => FindFirstCharacterToEncode(new ReadOnlySpan<char>(text, textLength));

        private int FindFirstCharacterToEncode(ReadOnlySpan<char> span)
        {
            int originalLength = span.Length;

            while (!span.IsEmpty)
            {
                if (!TryGetNextScalarValue(span, out int scalarValue) || !_isScalarAllowed(scalarValue))
                {
                    return originalLength - span.Length; // couldn't extract scalar or failed predicate
                }

                span = span.Slice(UnicodeUtility.GetUtf16SequenceLength((uint)scalarValue));
            }

            return -1; // entire span was consumed
        }

        private static bool TryGetNextScalarValue(ReadOnlySpan<char> span, out int scalarValue)
        {
            if (!span.IsEmpty)
            {
                // non-surrogate char?
                char firstChar = span[0];
                if (!char.IsSurrogate(firstChar))
                {
                    scalarValue = firstChar;
                    return true;
                }

                // well-formed surrogate pair?
                if (char.IsHighSurrogate(firstChar))
                {
                    if (span.Length > 1)
                    {
                        char secondChar = span[1];
                        if (char.IsLowSurrogate(secondChar))
                        {
                            scalarValue = char.ConvertToUtf32(firstChar, secondChar);
                            return true;
                        }
                    }
                }
            }

            // if we got to this point, span was empty or ill-formed surrogate found
            scalarValue = default;
            return false;
        }

        public override bool WillEncode(int unicodeScalar) => !_isScalarAllowed(unicodeScalar);

        /// <summary>
        /// Encodes scalar as an unsigned hexadecimal number (min. 4 hex digits) surrounded by square brackets: "[XXXX]".
        /// </summary>
        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            string encoded = FormattableString.Invariant($"[{(uint)unicodeScalar:X4}]");
            numberOfCharactersWritten = (encoded.Length <= (uint)bufferLength) ? encoded.Length : 0;
            return encoded.AsSpan().TryCopyTo(new Span<char>(buffer, bufferLength));
        }
    }
}
