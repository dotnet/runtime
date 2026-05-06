// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>Searches an input string for all occurrences of a regular expression and returns the number of matches.</summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public int Count(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            int count = 0;

            RunAllMatchesWithCallback(input, RightToLeft ? input.Length : 0, ref count, static (ref int count, Match match) =>
            {
                count++;
                return true;
            }, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);

            return count;
        }

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns the number of matches.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <returns>The number of matches.</returns>
        public int Count(ReadOnlySpan<char> input) =>
            Count(input, RightToLeft ? input.Length : 0);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns the number of matches.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="startat">The zero-based character position at which to start the search.</param>
        /// <returns>The number of matches.</returns>
        public int Count(ReadOnlySpan<char> input, int startat)
        {
            int count = 0;

            RunAllMatchesWithCallback(input, startat, ref count, static (ref int count, Match match) =>
            {
                count++;
                return true;
            }, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);

            return count;
        }

        /// <summary>Searches an input string for all occurrences of a regular expression and returns the number of matches.</summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is null.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).Count(input);

        /// <summary>Searches an input string for all occurrences of a regular expression and returns the number of matches.</summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Count(input);

        /// <summary>Searches an input string for all occurrences of a regular expression and returns the number of matches.</summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values, or <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Count(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns the number of matches.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).Count(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns the number of matches.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Count(input);

        /// <summary>
        /// Searches an input span for all occurrences of a regular expression and returns the number of matches.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>The number of matches.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid bitwise combination of RegexOptions values, or <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.</exception>
        /// <exception cref="RegexParseException">A regular expression parsing error occurred.</exception>
        public static int Count(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Count(input);
    }
}
