// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueMatchEnumerator"/> to iterate over the matches.
        /// </summary>
        /// <remarks>
        /// Each match won't actually happen until <see cref="ValueMatchEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueMatchEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueMatchEnumerator.MoveNext"/> will affect the match results.
        /// The enumerator returned by this method, as well as the structs returned by the enumerator that wrap each match found in the input are ref structs which
        /// make this method be amortized allocation free.
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>A <see cref="ValueMatchEnumerator"/> to iterate over the matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).EnumerateMatches(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueMatchEnumerator"/> to iterate over the matches.
        /// </summary>
        /// <remarks>
        /// Each match won't actually happen until <see cref="ValueMatchEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueMatchEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueMatchEnumerator.MoveNext"/> will affect the match results.
        /// The enumerator returned by this method, as well as the structs returned by the enumerator that wrap each match found in the input are ref structs which
        /// make this method be amortized allocation free.
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <returns>A <see cref="ValueMatchEnumerator"/> to iterate over the matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).EnumerateMatches(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueMatchEnumerator"/> to iterate over the matches.
        /// </summary>
        /// <remarks>
        /// Each match won't actually happen until <see cref="ValueMatchEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueMatchEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueMatchEnumerator.MoveNext"/> will affect the match results.
        /// The enumerator returned by this method, as well as the structs returned by the enumerator that wrap each match found in the input are ref structs which
        /// make this method be amortized allocation free.
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>A <see cref="ValueMatchEnumerator"/> to iterate over the matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values, or <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).EnumerateMatches(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueMatchEnumerator"/> to iterate over the matches.
        /// </summary>
        /// <remarks>
        /// Each match won't actually happen until <see cref="ValueMatchEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueMatchEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueMatchEnumerator.MoveNext"/> will affect the match results.
        /// The enumerator returned by this method, as well as the structs returned by the enumerator that wrap each match found in the input are ref structs which
        /// make this method be amortized allocation free.
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <returns>A <see cref="ValueMatchEnumerator"/> to iterate over the matches.</returns>
        public ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input) =>
            new ValueMatchEnumerator(this, input, RightToLeft ? input.Length : 0);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueMatchEnumerator"/> to iterate over the matches.
        /// </summary>
        /// <remarks>
        /// Each match won't actually happen until <see cref="ValueMatchEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueMatchEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueMatchEnumerator.MoveNext"/> will affect the match results.
        /// The enumerator returned by this method, as well as the structs returned by the enumerator that wrap each match found in the input are ref structs which
        /// make this method be amortized allocation free.
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="startat">The zero-based character position at which to start the search.</param>
        /// <returns>A <see cref="ValueMatchEnumerator"/> to iterate over the matches.</returns>
        public ValueMatchEnumerator EnumerateMatches(ReadOnlySpan<char> input, int startat) =>
            new ValueMatchEnumerator(this, input, startat);

        /// <summary>
        /// Represents an enumerator containing the set of successful matches found by iteratively applying a regular expression pattern to the input span.
        /// </summary>
        /// <remarks>
        /// The enumerator has no public constructor. The <see cref="Regex.EnumerateMatches(ReadOnlySpan{char})"/> method returns a <see cref="Regex.ValueMatchEnumerator"/>
        /// object.The enumerator will lazily iterate over zero or more <see cref="ValueMatch"/> objects. If there is at least one successful match in the span, then
        /// <see cref="MoveNext"/> returns <see langword="true"/> and <see cref="Current"/> will contain the first <see cref="ValueMatch"/>. If there are no successful matches,
        /// then <see cref="MoveNext"/> returns <see langword="false"/> and <see cref="Current"/> throws an <see cref="InvalidOperationException"/>.
        ///
        /// This type is a ref struct since it stores the input span as a field in order to be able to lazily iterate over it.
        /// </remarks>
        public ref struct ValueMatchEnumerator
        {
            private readonly Regex _regex;
            private readonly ReadOnlySpan<char> _input;
            private ValueMatch _current;
            private int _startAt;
            private int _prevLen;

            /// <summary>
            /// Creates an instance of the <see cref="ValueMatchEnumerator"/> for the passed in <paramref name="regex"/> which iterates over <paramref name="input"/>.
            /// </summary>
            /// <param name="regex">The <see cref="Regex"/> to use for finding matches.</param>
            /// <param name="input">The input span to iterate over.</param>
            /// <param name="startAt">The position where the engine should start looking for matches from.</param>
            internal ValueMatchEnumerator(Regex regex, ReadOnlySpan<char> input, int startAt)
            {
                _regex = regex;
                _input = input;
                _current = default;
                _startAt = startAt;
                _prevLen = -1;
            }

            /// <summary>
            /// Provides an enumerator that iterates through the matches in the input span.
            /// </summary>
            /// <returns>A copy of this enumerator.</returns>
            public readonly ValueMatchEnumerator GetEnumerator() => this;

            /// <summary>
            /// Advances the enumerator to the next match in the span.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator cannot find additional matches.
            /// </returns>
            public bool MoveNext()
            {
                (bool Success, int Index, int Length, int TextPosition) match = _regex.RunSingleMatch(RegexRunnerMode.BoundsRequired, _prevLen, _input, _startAt);
                if (match.Success)
                {
                    _current = new ValueMatch(match.Index, match.Length);
                    _startAt = match.TextPosition;
                    _prevLen = match.Length;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets the <see cref="ValueMatch"/> element at the current position of the enumerator.
            /// </summary>
            /// <exception cref="InvalidOperationException">Enumeration has either not started or has already finished.</exception>
            public readonly ValueMatch Current => _current;
        }
    }
}
