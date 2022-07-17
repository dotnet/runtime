// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single regular expression match.
    /// </summary>
    /// <remarks>
    /// The <see cref="ValueMatch"/> type is immutable and has no public constructor. An instance of the <see cref="ValueMatch"/> struct is returned by the
    /// <see cref="Regex.ValueMatchEnumerator.Current"/> method when iterating over the results from calling <see cref="Regex.EnumerateMatches(ReadOnlySpan{char})"/>.
    /// </remarks>
    public readonly ref struct ValueMatch
    {
        private readonly int _index;
        private readonly int _length;

        /// <summary>
        /// Crates an instance of the <see cref="ValueMatch"/> type based on the passed in <paramref name="index"/> and <paramref name="length"/>.
        /// </summary>
        /// <param name="index">The position in the original span where the first character of the captured sliced span is found.</param>
        /// <param name="length">The length of the captured sliced span.</param>
        internal ValueMatch(int index, int length)
        {
            _index = index;
            _length = length;
        }

        /// <summary>
        /// Gets the position in the original span where the first character of the captured sliced span is found.
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// Gets the length of the captured sliced span.
        /// </summary>
        public int Length => _length;
    }
}
