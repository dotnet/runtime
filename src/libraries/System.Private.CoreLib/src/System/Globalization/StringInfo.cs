// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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

        public override bool Equals(object? value)
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
                _str = value ?? throw new ArgumentNullException(nameof(value));
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

        public static string GetNextTextElement(string str) => GetNextTextElement(str, 0);

        /// <summary>
        /// Returns the str containing the next text element in str starting at
        /// index index. If index is not supplied, then it will start at the beginning
        /// of str. It recognizes a base character plus one or more combining
        /// characters or a properly formed surrogate pair as a text element.
        /// See also the ParseCombiningCharacters() and the ParseSurrogates() methods.
        /// </summary>
        public static string GetNextTextElement(string str, int index)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }
            if ((uint)index > (uint)str.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            return str.Substring(index, TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(str.AsSpan(index)));
        }

        public static TextElementEnumerator GetTextElementEnumerator(string str) => GetTextElementEnumerator(str, 0);

        public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
        {
            if (str is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
            }
            if ((uint)index > (uint)str.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
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

            // This method is optimized for small-ish strings.
            // If a large string is seen we'll go down a slower code path.

            if (str.Length > 256)
            {
                return ParseCombiningCharactersForLargeString(str);
            }

            Span<int> baseOffsets = stackalloc int[str.Length];
            int graphemeClusterCount = 0;

            ReadOnlySpan<char> remaining = str;
            while (!remaining.IsEmpty)
            {
                baseOffsets[graphemeClusterCount++] = str.Length - remaining.Length;
                remaining = remaining.Slice(TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(remaining));
            }

            return baseOffsets.Slice(0, graphemeClusterCount).ToArray();
        }

        private static int[] ParseCombiningCharactersForLargeString(string str)
        {
            Debug.Assert(str != null);

            // If we have a large string, we may as well take the hit of using a List<int>
            // instead of trying the stackalloc optimizations we have for smaller strings.

            List<int> baseOffsets = new List<int>();

            ReadOnlySpan<char> remaining = str;
            while (!remaining.IsEmpty)
            {
                baseOffsets.Add(str.Length - remaining.Length);
                remaining = remaining.Slice(TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(remaining));
            }

            return baseOffsets.ToArray();
        }
    }
}
