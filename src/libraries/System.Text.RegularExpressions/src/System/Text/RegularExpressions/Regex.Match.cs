// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Searches the input string for one or more occurrences of the text supplied in the given pattern.
        /// </summary>
        public static bool IsMatch(string input, string pattern) =>
            RegexCache.GetOrAdd(pattern).IsMatch(input);

        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter with matching options supplied in the options
        /// parameter.
        /// </summary>
        public static bool IsMatch(string input, string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);

        public static bool IsMatch(string input, string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);

        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern,
        /// options, and starting position.
        /// </summary>
        public bool IsMatch(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Run(quick: true, -1, input, 0, input.Length, UseOptionR() ? input.Length : 0) is null;
        }

        /// <summary>
        /// Searches the input string for one or more matches using the previous pattern and options,
        /// with a new starting position.
        /// </summary>
        public bool IsMatch(string input, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Run(quick: true, -1, input, 0, input.Length, startat) is null;
        }

        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter.
        /// </summary>
        public static Match Match(string input, string pattern) =>
            RegexCache.GetOrAdd(pattern).Match(input);

        /// <summary>
        /// Searches the input string for one or more occurrences of the text
        /// supplied in the pattern parameter. Matching is modified with an option
        /// string.
        /// </summary>
        public static Match Match(string input, string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Match(input);

        public static Match Match(string input, string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Match(input);

        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a Match object.
        /// </summary>
        public Match Match(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Run(quick: false, -1, input, 0, input.Length, UseOptionR() ? input.Length : 0)!;
        }

        /// <summary>
        /// Matches a regular expression with a string and returns
        /// the precise result as a Match object.
        /// </summary>
        public Match Match(string input, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Run(quick: false, -1, input, 0, input.Length, startat)!;
        }

        /// <summary>
        /// Matches a regular expression with a string and returns the precise result as a Match object.
        /// </summary>
        public Match Match(string input, int beginning, int length)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Run(quick: false, -1, input, beginning, length, UseOptionR() ? beginning + length : beginning)!;
        }

        /// <summary>
        /// Returns all the successful matches as if Match were called iteratively numerous times.
        /// </summary>
        public static MatchCollection Matches(string input, string pattern) =>
            RegexCache.GetOrAdd(pattern).Matches(input);

        /// <summary>
        /// Returns all the successful matches as if Match were called iteratively numerous times.
        /// </summary>
        public static MatchCollection Matches(string input, string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Matches(input);

        public static MatchCollection Matches(string input, string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Matches(input);

        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return new MatchCollection(this, input, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Returns all the successful matches as if Match was called iteratively numerous times.
        /// </summary>
        public MatchCollection Matches(string input, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return new MatchCollection(this, input, startat);
        }
    }
}
