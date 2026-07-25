// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Splits an input string into an array of substrings at the positions defined by a regular
        /// expression pattern.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Split(string, string)"/> methods are equivalent to constructing a
        /// <see cref="Regex"/> object with the specified regular expression pattern and calling the
        /// instance method <see cref="Split(string)"/>.
        /// </para>
        /// <para>
        /// The <see cref="Regex.Split(string)">Regex.Split</see> methods are similar to the
        /// <see cref="string.Split(char[])"/> method, except that <see cref="Regex.Split(string)">Regex.Split</see> splits
        /// the string at a delimiter determined by a regular expression instead of a set of
        /// characters. If the regular expression pattern includes capturing parentheses, the
        /// captured text is included in the resulting string array. If the pattern includes
        /// capturing parentheses, any captured text is included in the resulting string array, but
        /// is not counted when determining whether the count limit has been reached.
        /// </para>
        /// <para>
        /// If two adjacent matches are found, an empty string is placed in the array.
        /// </para>
        /// </remarks>
        public static string[] Split(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).Split(input);

        /// <summary>
        /// Splits an input string into an array of substrings at the positions defined by a
        /// specified regular expression pattern. Specified options modify the matching operation.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">
        /// A bitwise combination of the enumeration values that provide options for matching.
        /// </param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of
        /// <see cref="RegexOptions"/> values.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Split(string, string, RegexOptions)"/> methods are equivalent to
        /// constructing a <see cref="Regex"/> object with the specified regular expression pattern
        /// and calling the instance method <see cref="Split(string)"/>.
        /// </para>
        /// <para>
        /// The <see cref="Regex.Split(string)">Regex.Split</see> methods are similar to the
        /// <see cref="string.Split(char[])"/> method, except that <see cref="Regex.Split(string)">Regex.Split</see> splits
        /// the string at a delimiter determined by a regular expression instead of a set of
        /// characters. If the regular expression pattern includes capturing parentheses, the
        /// captured text is included in the resulting string array. If the pattern includes
        /// capturing parentheses, any captured text is included in the resulting string array, but
        /// is not counted when determining whether the count limit has been reached.
        /// </para>
        /// <para>
        /// If two adjacent matches are found, an empty string is placed in the array.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the
        /// <paramref name="options"/> parameter, the search for matches begins at the end of the
        /// input string and moves left.
        /// </para>
        /// </remarks>
        public static string[] Split(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Split(input);

        /// <summary>
        /// Splits an input string into an array of substrings at the positions defined by a
        /// specified regular expression pattern. Additional parameters specify options that modify
        /// the matching operation and a time-out interval if no match is found.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">
        /// A bitwise combination of the enumeration values that provide options for matching.
        /// </param>
        /// <param name="matchTimeout">
        /// A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate that the
        /// method should not time out.
        /// </param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of
        /// <see cref="RegexOptions"/> values.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Split(string, string, RegexOptions, TimeSpan)"/> methods are
        /// equivalent to constructing a <see cref="Regex"/> object with the specified regular
        /// expression pattern and calling the instance method <see cref="Split(string)"/>.
        /// </para>
        /// <para>
        /// The <see cref="Regex.Split(string)">Regex.Split</see> methods are similar to the
        /// <see cref="string.Split(char[])"/> method, except that <see cref="Regex.Split(string)">Regex.Split</see> splits
        /// the string at a delimiter determined by a regular expression instead of a set of
        /// characters. If the regular expression pattern includes capturing parentheses, the
        /// captured text is included in the resulting string array. If the pattern includes
        /// capturing parentheses, any captured text is included in the resulting string array, but
        /// is not counted when determining whether the count limit has been reached.
        /// </para>
        /// <para>
        /// If two adjacent matches are found, an empty string is placed in the array.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the
        /// <paramref name="options"/> parameter, the search for matches begins at the end of the
        /// input string and moves left.
        /// </para>
        /// <para>
        /// The <paramref name="matchTimeout"/> parameter specifies how long a pattern matching
        /// method should try to find a match before it times out.
        /// <paramref name="matchTimeout"/> overrides any default time-out value defined for the
        /// application domain in which the method executes.
        /// </para>
        /// </remarks>
        public static string[] Split(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Split(input);

        /// <summary>
        /// Splits an input string into an array of substrings at the positions defined by a
        /// regular expression pattern specified in the <see cref="Regex"/> constructor.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Regex.Split(string)">Regex.Split</see> methods are similar to the
        /// <see cref="string.Split(char[])"/> method, except that <see cref="Regex.Split(string)">Regex.Split</see> splits
        /// the string at a delimiter determined by a regular expression instead of a set of
        /// characters. The string is split as many times as possible. If no match is found, the
        /// return value contains one element whose value is the original input string.
        /// </para>
        /// <para>
        /// If the regular expression can match the empty string, <see cref="Split(string)"/> will
        /// split the string into an array of single-character strings because the empty string
        /// delimiter can be found at every location.
        /// </para>
        /// <para>
        /// If capturing parentheses are used in the expression, any captured text is included in
        /// the resulting string array.
        /// </para>
        /// <para>
        /// If two adjacent matches are found, an empty string is placed in the array.
        /// </para>
        /// </remarks>
        public string[] Split(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, 0, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Splits an input string a specified maximum number of times into an array of substrings,
        /// at the positions defined by a regular expression specified in the <see cref="Regex"/>
        /// constructor.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <param name="count">The maximum number of times the split can occur.</param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Regex.Split(string)">Regex.Split</see> methods are similar to
        /// <see cref="string.Split(char[])"/>. The <paramref name="count"/> parameter specifies
        /// the maximum number of substrings into which the input string can be split; the last
        /// string contains the unsplit remainder of the string. A <paramref name="count"/> value of
        /// zero provides the default behavior of splitting as many times as possible.
        /// </para>
        /// <para>
        /// If capturing parentheses are used in the expression, any captured text is included in
        /// the resulting string array but is not counted toward the <paramref name="count"/> limit.
        /// </para>
        /// <para>
        /// Empty strings that result from adjacent matches are counted when determining whether
        /// the number of matches has reached <paramref name="count"/>.
        /// </para>
        /// </remarks>
        public string[] Split(string input, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, count, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Splits an input string a specified maximum number of times into an array of substrings,
        /// at the positions defined by a regular expression specified in the <see cref="Regex"/>
        /// constructor. The search for the regular expression pattern starts at a specified
        /// character position in the input string.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <param name="count">The maximum number of times the split can occur.</param>
        /// <param name="startat">
        /// The character position in the input string where the search begins.
        /// </param>
        /// <returns>An array of strings.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of
        /// <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// For more details about <paramref name="startat"/>, see the Remarks section of
        /// <see cref="Match(string, int)"/>.
        /// </para>
        /// <para>
        /// If capturing parentheses are used in the expression, any captured text is included in
        /// the resulting string array but is not counted toward the <paramref name="count"/> limit.
        /// </para>
        /// <para>
        /// Empty strings that result from adjacent matches are counted when determining whether
        /// the number of matches has reached <paramref name="count"/>.
        /// </para>
        /// </remarks>
        public string[] Split(string input, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, count, startat);
        }

        /// <summary>
        /// Does a split. In the right-to-left case we reorder the
        /// array to be forwards.
        /// </summary>
        private static string[] Split(Regex regex, string input, int count, int startat)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
            }
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            if (count == 1)
            {
                return [input];
            }

            count--;
            var state = (results: new List<string>(), prevat: 0, input, count);

            if (!regex.RightToLeft)
            {
                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (List<string> results, int prevat, string input, int count) state, Match match) =>
                {
                    state.results.Add(state.input.Substring(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;

                    // add all matched capture groups to the list.
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.IsMatched(i))
                        {
                            state.results.Add(match.Groups[i].Value);
                        }
                    }

                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);

                if (state.results.Count == 0)
                {
                    return [input];
                }

                state.results.Add(input.Substring(state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (List<string> results, int prevat, string input, int count) state, Match match) =>
                {
                    state.results.Add(state.input.Substring(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;

                    // add all matched capture groups to the list.
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.IsMatched(i))
                        {
                            state.results.Add(match.Groups[i].Value);
                        }
                    }

                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);

                if (state.results.Count == 0)
                {
                    return [input];
                }

                state.results.Add(input.Substring(0, state.prevat));
                state.results.Reverse(0, state.results.Count);
            }

            return state.results.ToArray();
        }
    }
}
