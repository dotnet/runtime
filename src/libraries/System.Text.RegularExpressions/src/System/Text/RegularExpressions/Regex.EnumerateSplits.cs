// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char}, string)"/> is similar to the behavior of <see cref="Split(string, string)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string, string)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char}, string)"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string, string)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char}, string)"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).EnumerateSplits(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions)"/> is similar to the behavior of <see cref="Split(string, string, RegexOptions)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string, string, RegexOptions)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions)"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string, string, RegexOptions)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions)"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).EnumerateSplits(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions, TimeSpan)"/> is similar to the behavior of <see cref="Split(string, string, RegexOptions, TimeSpan)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string, string, RegexOptions, TimeSpan)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions, TimeSpan)"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string, string, RegexOptions, TimeSpan)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char}, string, RegexOptions, TimeSpan)"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values, or <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).EnumerateSplits(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char})"/> is similar to the behavior of <see cref="Split(string)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char})"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char})"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the matches.</returns>
        public ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input) =>
            EnumerateSplits(input, count: 0);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char}, int)"/> is similar to the behavior of <see cref="Split(string, int)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string, int)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char}, int)"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string, int)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char}, int)"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="count">The maximum number of times the split can occur. If 0, all splits are available.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the matches.</returns>
        public ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input, int count) =>
            EnumerateSplits(input, count, startat: RightToLeft ? input.Length : 0);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns a <see cref="ValueSplitEnumerator"/> to iterate over the splits around matches.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The behavior of <see cref="EnumerateSplits(ReadOnlySpan{char}, int, int)"/> is similar to the behavior of <see cref="Split(string, int, int)"/>, producing the splits
        /// one at a time as part of iterating through the resulting enumerator rather than all at once as part of a single array. However, there are a few notable differences.
        /// <see cref="Split(string, int, int)"/> will include the contents of capture groups in the resulting splits, while <see cref="EnumerateSplits(ReadOnlySpan{char}, int, int)"/> will not.
        /// And if <see cref="RegexOptions.RightToLeft"/> is specified, <see cref="Split(string, int, int)"/> will reverse the order of the resulting splits to be left-to-right, whereas
        /// <see cref="EnumerateSplits(ReadOnlySpan{char}, int, int)"/> will yield the splits in the order they're found right-to-left.
        /// </para>
        /// <para>
        /// Each match won't actually happen until <see cref="ValueSplitEnumerator.MoveNext"/> is invoked on the enumerator, with one match being performed per <see cref="ValueSplitEnumerator.MoveNext"/> call.
        /// Since the evaluation of the match happens lazily, any changes to the passed in input in between calls to <see cref="ValueSplitEnumerator.MoveNext"/> may affect the match results;
        /// such changes should be avoided and are not supported.
        /// </para>
        /// </remarks>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="count">The maximum number of times the split can occur. If 0, all splits are available.</param>
        /// <param name="startat">The zero-based character position at which to start the search.</param>
        /// <returns>A <see cref="ValueSplitEnumerator"/> to iterate over the matches.</returns>
        public ValueSplitEnumerator EnumerateSplits(ReadOnlySpan<char> input, int count, int startat)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
            }

            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            return new ValueSplitEnumerator(this, input, count, startat, RightToLeft);
        }

        /// <summary>
        /// Represents an enumerator containing the set of splits around successful matches found by iteratively applying a regular expression pattern to the input span.
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public ref struct ValueSplitEnumerator : IEnumerator<Range>
        {
            private readonly Regex _regex;
            private readonly ReadOnlySpan<char> _input;

            private int _startAt;
            private (int Index, int Length) _lastMatch;
            private Range _currentSplit;
            private int _remainingCount;

            /// <summary>
            /// Creates an instance of the <see cref="ValueSplitEnumerator"/> for the passed in <paramref name="regex"/> which iterates over <paramref name="input"/>.
            /// </summary>
            /// <param name="regex">The <see cref="Regex"/> to use for finding matches.</param>
            /// <param name="input">The input span to iterate over.</param>
            /// <param name="count">The maximum number of times the split can occur.</param>
            /// <param name="startAt">The position from where the engine should start looking for matches.</param>
            /// <param name="rtl">Whether the engine is matching from right to left.</param>
            internal ValueSplitEnumerator(Regex regex, ReadOnlySpan<char> input, int count, int startAt, bool rtl)
            {
                _regex = regex;
                _input = input;
                _startAt = startAt;
                _lastMatch = (rtl ? input.Length : 0, -1);
                _remainingCount = count != 0 ? count : int.MaxValue; // Maintain same behavior as Split(..., count: 0, ...), which treats it as effectively infinite.
            }

            /// <summary>Provides an enumerator that iterates through the splits in the input span.</summary>
            /// <returns>A copy of this enumerator.</returns>
            public readonly ValueSplitEnumerator GetEnumerator() => this;

            /// <summary>Advances the enumerator to the next split.</summary>
            /// <returns>
            /// <see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator cannot find additional matches.
            /// </returns>
            public bool MoveNext()
            {
                // If we've already found all the splits, we're done.
                if (_remainingCount == 0)
                {
                    return false;
                }

                if (_remainingCount == 1)
                {
                    // If we've reached the last split, include everything that remains.
                    _currentSplit = !_regex.RightToLeft ? (_lastMatch.Index + _lastMatch.Length).._input.Length : 0.._lastMatch.Index;
                }
                else
                {
                    // Perform the next match.
                    (bool Success, int Index, int Length, int TextPosition) match = _regex.RunSingleMatch(RegexRunnerMode.BoundsRequired, _lastMatch.Length, _input, _startAt);

                    // If the match was successful, update the current result to be the input between the last match and this one.
                    // Otherwise, update the current result to be the input between the last match and the end of the input.
                    if (!_regex.RightToLeft)
                    {
                        int start = _lastMatch.Index + Math.Max(_lastMatch.Length, 0);
                        if (match.Success)
                        {
                            _currentSplit = start..match.Index;
                            _lastMatch = (match.Index, match.Length);
                        }
                        else
                        {
                            _currentSplit = start.._input.Length;
                            _remainingCount = 1;
                        }
                    }
                    else
                    {
                        if (match.Success)
                        {
                            int start = _lastMatch.Index;
                            _currentSplit = (match.Index + match.Length)..start;
                            _lastMatch = (match.Index, match.Length);
                        }
                        else
                        {
                            _currentSplit = 0.._lastMatch.Index;
                            _remainingCount = 1;
                        }
                    }

                    // Update the position from which to perform the next match.
                    _startAt = match.TextPosition;
                }

                // Decrement the remaining count now that we're successfully yielding the next split.
                _remainingCount--;
                return true;
            }

            /// <summary>
            /// Gets the <see cref="ValueMatch"/> element at the current position of the enumerator.
            /// </summary>
            /// <exception cref="InvalidOperationException">Enumeration has either not started or has already finished.</exception>
            public readonly Range Current => _currentSplit;

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            void IEnumerator.Reset() => throw new NotSupportedException();

            /// <inheritdoc/>
            void IDisposable.Dispose() { }
        }
    }
}
