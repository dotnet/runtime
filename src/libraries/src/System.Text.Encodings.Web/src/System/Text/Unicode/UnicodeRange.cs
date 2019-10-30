// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Text.Unicode
{
    /// <summary>
    /// Represents a contiguous range of Unicode code points.
    /// </summary>
    /// <remarks>
    /// Currently only the Basic Multilingual Plane is supported.
    /// </remarks>
    public sealed class UnicodeRange
    {
        /// <summary>
        /// Creates a new <see cref="UnicodeRange"/>.
        /// </summary>
        /// <param name="firstCodePoint">The first code point in the range.</param>
        /// <param name="length">The number of code points in the range.</param>
        public UnicodeRange(int firstCodePoint, int length)
        {
            // Parameter checking: the first code point and last code point must
            // lie within the BMP. See http://unicode.org/faq/blocks_ranges.html for more info.
            if (firstCodePoint < 0 || firstCodePoint > 0xFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(firstCodePoint));
            }
            if (length < 0 || ((long)firstCodePoint + (long)length > 0x10000))
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            FirstCodePoint = firstCodePoint;
            Length = length;
        }

        /// <summary>
        /// The first code point in this range.
        /// </summary>
        public int FirstCodePoint { get; private set; }

        /// <summary>
        /// The number of code points in this range.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Creates a new <see cref="UnicodeRange"/> from a span of characters.
        /// </summary>
        /// <param name="firstCharacter">The first character in the range.</param>
        /// <param name="lastCharacter">The last character in the range.</param>
        /// <returns>The <see cref="UnicodeRange"/> representing this span.</returns>
        public static UnicodeRange Create(char firstCharacter, char lastCharacter)
        {
            if (lastCharacter < firstCharacter)
            {
                throw new ArgumentOutOfRangeException(nameof(lastCharacter));
            }

            return new UnicodeRange(firstCharacter, 1 + (int)(lastCharacter - firstCharacter));
        }
    }
}
