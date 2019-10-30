// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Capture is just a location/length pair that indicates the
// location of a regular expression match. A single regexp
// search may return multiple Capture within each capturing
// RegexGroup.

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single subexpression capture. The object represents
    /// one substring for a single successful capture.
    /// </summary>
    public class Capture
    {
        internal Capture(string text, int index, int length)
        {
            Text = text;
            Index = index;
            Length = length;
        }

        /// <summary>
        /// Returns the position in the original string where the first character of
        /// captured substring was found.
        /// </summary>
        public int Index { get; private protected set; }

        /// <summary>
        /// Returns the length of the captured substring.
        /// </summary>
        public int Length { get; private protected set; }

        /// <summary>
        /// The original string
        /// </summary>
        internal string Text { get; private protected set; }

        /// <summary>
        /// Returns the value of this Regex Capture.
        /// </summary>
        public string Value => Text.Substring(Index, Length);

        /// <summary>
        /// Returns the substring that was matched.
        /// </summary>
        public override string ToString() => Value;

        /// <summary>
        /// The substring to the left of the capture
        /// </summary>
        internal ReadOnlySpan<char> GetLeftSubstring() => Text.AsSpan(0, Index);

        /// <summary>
        /// The substring to the right of the capture
        /// </summary>
        internal ReadOnlySpan<char> GetRightSubstring() => Text.AsSpan(Index + Length, Text.Length - Index - Length);
    }
}
