// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Unicode;

namespace System.Globalization
{
    /// <summary>
    /// This class defines behaviors specific to a writing system.
    /// A writing system is the collection of scripts and orthographic rules
    /// required to represent a language as text.
    /// </summary>
    public class StringInfo
    {
        private string _str;

        private int[]? _indexes;

        public StringInfo() : this(string.Empty)
        {
        }

        public StringInfo(string value)
        {
            this.String = value;
        }

        public override bool Equals([NotNullWhen(true)] object? value)
        {
            return value is StringInfo otherStringInfo
                && _str.Equals(otherStringInfo._str);
        }

        public override int GetHashCode() => _str.GetHashCode();

        /// <summary>
        /// Our zero-based array of index values into the string. Initialize if
        /// our private array is not yet, in fact, initialized.
        /// </summary>
        private int[]? Indexes
        {
            get
            {
                if (_indexes == null && String.Length > 0)
                {
                    _indexes = StringInfo.ParseCombiningCharacters(String);
                }

                return _indexes;
            }
        }

        public string String
        {
            get => _str;
            [MemberNotNull(nameof(_str))]
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _str = value;
                _indexes = null;
            }
        }

        public int LengthInTextElements => Indexes?.Length ?? 0;

        public string SubstringByTextElements(int startingTextElement)
        {
            return SubstringByTextElements(startingTextElement, (Indexes?.Length ?? 0) - startingTextElement);
        }

        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements)
        {
            int[] indexes = Indexes ?? Array.Empty<int>();

            if ((uint)startingTextElement >= (uint)indexes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTextElement), startingTextElement, SR.Arg_ArgumentOutOfRangeException);
            }
            if ((uint)lengthInTextElements > (uint)(indexes.Length - startingTextElement))
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInTextElements), lengthInTextElements, SR.Arg_ArgumentOutOfRangeException);
            }

            int start = indexes[startingTextElement];
            Index end = ^0; // assume reading to end of the string unless the caller told us to stop early

            if ((uint)(startingTextElement + lengthInTextElements) < (uint)indexes.Length)
            {
                end = indexes[startingTextElement + lengthInTextElements];
            }

            return String[start..end];
        }

        /// <summary>
        /// Returns the first text element (extended grapheme cluster) that occurs in the input string.
        /// </summary>
        /// <remarks>
        /// A grapheme cluster is a sequence of one or more Unicode code points that should be treated as a single unit.
        /// </remarks>
        /// <param name="str">The input string to analyze.</param>
        /// <returns>The substring corresponding to the first text element within <paramref name="str"/>,
        /// or the empty string if <paramref name="str"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is null.</exception>
        public static string GetNextTextElement(string str) => GetNextTextElement(str, 0);

        /// <summary>
        /// Returns the first text element (extended grapheme cluster) that occurs in the input string
        /// starting at the specified index.
        /// </summary>
        /// <remarks>
        /// A grapheme cluster is a sequence of one or more Unicode code points that should be treated as a single unit.
        /// </remarks>
        /// <param name="str">The input string to analyze.</param>
        /// <param name="index">The char offset in <paramref name="str"/> at which to begin analysis.</param>
        /// <returns>The substring corresponding to the first text element within <paramref name="str"/> starting
        /// at index <paramref name="index"/>, or the empty string if <paramref name="index"/> corresponds to
        /// the end of <paramref name="str"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or beyond the end of <paramref name="str"/>.</exception>
        public static string GetNextTextElement(string str, int index)
        {
            int nextTextElementLength = GetNextTextElementLength(str, index);
            return str.Substring(index, nextTextElementLength);
        }

        /// <summary>
        /// Returns the length of the first text element (extended grapheme cluster) that occurs in the input string.
        /// </summary>
        /// <remarks>
        /// A grapheme cluster is a sequence of one or more Unicode code points that should be treated as a single unit.
        /// </remarks>
        /// <param name="str">The input string to analyze.</param>
        /// <returns>The length (in chars) of the substring corresponding to the first text element within <paramref name="str"/>,
        /// or 0 if <paramref name="str"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is null.</exception>
        public static int GetNextTextElementLength(string str) => GetNextTextElementLength(str, 0);

        /// <summary>
        /// Returns the length of the first text element (extended grapheme cluster) that occurs in the input string
        /// starting at the specified index.
        /// </summary>
        /// <remarks>
        /// A grapheme cluster is a sequence of one or more Unicode code points that should be treated as a single unit.
        /// </remarks>
        /// <param name="str">The input string to analyze.</param>
        /// <param name="index">The char offset in <paramref name="str"/> at which to begin analysis.</param>
        /// <returns>The length (in chars) of the substring corresponding to the first text element within <paramref name="str"/> starting
        /// at index <paramref name="index"/>, or 0 if <paramref name="index"/> corresponds to the end of <paramref name="str"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or beyond the end of <paramref name="str"/>.</exception>
        public static int GetNextTextElementLength(string str, int index)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }
            if ((uint)index > (uint)str.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return GetNextTextElementLength(str.AsSpan(index));
        }

        /// <summary>
        /// Returns the length of the first text element (extended grapheme cluster) that occurs in the input span.
        /// </summary>
        /// <remarks>
        /// A grapheme cluster is a sequence of one or more Unicode code points that should be treated as a single unit.
        /// </remarks>
        /// <param name="str">The input span to analyze.</param>
        /// <returns>The length (in chars) of the substring corresponding to the first text element within <paramref name="str"/>,
        /// or 0 if <paramref name="str"/> is empty.</returns>
        public static int GetNextTextElementLength(ReadOnlySpan<char> str) => TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(str);

        public static TextElementEnumerator GetTextElementEnumerator(string str) => GetTextElementEnumerator(str, 0);

        public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }
            if ((uint)index > (uint)str.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            return new TextElementEnumerator(str, index);
        }

        /// <summary>
        /// Returns the indices of each base character or properly formed surrogate
        /// pair  within the str. It recognizes a base character plus one or more
        /// combining characters or a properly formed surrogate pair as a text
        /// element and returns the index of the base character or high surrogate.
        /// Each index is the beginning of a text element within a str. The length
        /// of each element is easily computed as the difference between successive
        /// indices. The length of the array will always be less than or equal to
        /// the length of the str. For example, given the str
        /// \u4f00\u302a\ud800\udc00\u4f01, this method would return the indices:
        /// 0, 2, 4.
        /// </summary>
        public static int[] ParseCombiningCharacters(string str)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }

            ValueListBuilder<int> builder = new ValueListBuilder<int>(stackalloc int[64]); // 64 arbitrarily chosen
            ReadOnlySpan<char> remaining = str;

            while (!remaining.IsEmpty)
            {
                builder.Append(str.Length - remaining.Length); // a new extended grapheme cluster begins at this offset
                remaining = remaining.Slice(GetNextTextElementLength(remaining)); // consume this cluster
            }

            int[] retVal = builder.AsSpan().ToArray();
            builder.Dispose();

            return retVal;
        }
    }
}
