// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="IsMatch(string, string)"/> method is typically used to validate a string or to ensure
        /// that a string conforms to a particular pattern without retrieving that string for subsequent manipulation.
        /// To retrieve matched strings, call the <see cref="Match(string, string)"/> or
        /// <see cref="Matches(string, string)"/> method instead.
        /// </para>
        /// <para>
        /// The static <see cref="IsMatch(string, string)"/> method is equivalent to constructing a
        /// <see cref="Regex"/> object with the specified pattern and calling the <see cref="IsMatch(string)"/>
        /// instance method. The pattern is cached for rapid retrieval by the regular expression engine.
        /// </para>
        /// </remarks>
        public static bool IsMatch(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).IsMatch(input);

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input span.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).IsMatch(input);

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input string,
        /// using the specified matching options.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid <see cref="RegexOptions"/> value.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="IsMatch(string, string, RegexOptions)"/> method is typically used to validate a string
        /// or to ensure that a string conforms to a particular pattern without retrieving that string for
        /// subsequent manipulation. To retrieve matched strings, call the
        /// <see cref="Match(string, string, RegexOptions)"/> or
        /// <see cref="Matches(string, string, RegexOptions)"/> method instead.
        /// </para>
        /// <para>
        /// The static <see cref="IsMatch(string, string, RegexOptions)"/> method is equivalent to constructing a
        /// <see cref="Regex"/> object with the specified pattern and options and calling the
        /// <see cref="IsMatch(string)"/> instance method. The pattern is cached for rapid retrieval by the
        /// regular expression engine.
        /// </para>
        /// </remarks>
        public static bool IsMatch(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input span,
        /// using the specified matching options.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid <see cref="RegexOptions"/> value.
        /// </exception>
        public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).IsMatch(input);

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input string,
        /// using the specified matching options and time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate
        /// that the method should not time out.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid <see cref="RegexOptions"/> value.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="IsMatch(string, string, RegexOptions, TimeSpan)"/> method is typically used to validate
        /// a string or to ensure that a string conforms to a particular pattern without retrieving that string
        /// for subsequent manipulation. To retrieve matched strings, call the
        /// <see cref="Match(string, string, RegexOptions, TimeSpan)"/> or
        /// <see cref="Matches(string, string, RegexOptions, TimeSpan)"/> method instead.
        /// </para>
        /// <para>
        /// The static <see cref="IsMatch(string, string, RegexOptions, TimeSpan)"/> method is equivalent to
        /// constructing a <see cref="Regex"/> object with the specified pattern and options and calling the
        /// <see cref="IsMatch(string)"/> instance method. The pattern is cached for rapid retrieval by the
        /// regular expression engine.
        /// </para>
        /// <para>
        /// The <paramref name="matchTimeout"/> parameter specifies how long a pattern matching method should try
        /// to find a match before it times out. Setting a time-out interval prevents regular expressions that
        /// rely on excessive backtracking from appearing to stop responding when they process input that contains
        /// near matches. If no match is found in that time interval, the method throws a
        /// <see cref="RegexMatchTimeoutException"/> exception. <paramref name="matchTimeout"/> overrides any
        /// default time-out value defined for the application domain in which the method executes.
        /// </para>
        /// </remarks>
        public static bool IsMatch(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input span,
        /// using the specified matching options and time-out interval.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate
        /// that the method should not time out.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid <see cref="RegexOptions"/> value
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        public static bool IsMatch(ReadOnlySpan<char> input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).IsMatch(input);

        /// <summary>
        /// Indicates whether the regular expression specified in the <see cref="Regex"/> constructor finds a
        /// match in a specified input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="IsMatch(string)"/> method is typically used to validate a string or to ensure that a
        /// string conforms to a particular pattern without retrieving that string for subsequent manipulation.
        /// To determine whether one or more strings match a regular expression pattern and retrieve them for
        /// subsequent manipulation, call the <see cref="Match(string)"/> or <see cref="Matches(string)"/> method.
        /// </para>
        /// <para>
        /// The <see cref="RegexMatchTimeoutException"/> exception is thrown if the execution time of the matching
        /// operation exceeds the time-out interval specified by the
        /// <see cref="Regex(string, RegexOptions, TimeSpan)"/> constructor. If you do not set a time-out interval
        /// when you call the constructor, the exception is thrown if the operation exceeds any time-out value
        /// established for the application domain in which the <see cref="Regex"/> object is created. If no
        /// time-out is defined in the <see cref="Regex"/> constructor call or in the application domain's
        /// properties, or if the time-out value is <see cref="Regex.InfiniteMatchTimeout"/>, no exception
        /// is thrown.
        /// </para>
        /// </remarks>
        public bool IsMatch(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, 0, input.Length, RightToLeft ? input.Length : 0) is null;
        }

        /// <summary>
        /// Indicates whether the regular expression specified in the <see cref="Regex"/> constructor finds a
        /// match in the specified input string, beginning at the specified starting position in the string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="startat">The character position at which to start the search.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="IsMatch(string, int)"/> method is typically used to validate a string or to ensure
        /// that a string conforms to a particular pattern without retrieving that string for subsequent
        /// manipulation. To retrieve matched strings, call the <see cref="Match(string, int)"/> or
        /// <see cref="Matches(string, int)"/> method instead.
        /// </para>
        /// <para>
        /// For more details about <paramref name="startat"/>, see the remarks for
        /// <see cref="Match(string, int)"/>.
        /// </para>
        /// </remarks>
        public bool IsMatch(string input, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, 0, input.Length, startat) is null;
        }

        /// <summary>
        /// Indicates whether the regular expression specified in the <see cref="Regex"/> constructor finds a
        /// match in a specified input span.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public bool IsMatch(ReadOnlySpan<char> input) =>
            IsMatch(input, RightToLeft ? input.Length : 0);

        /// <summary>
        /// Indicates whether the regular expression specified in the <see cref="Regex"/> constructor finds a
        /// match in a specified input span, starting at the specified position.
        /// </summary>
        /// <param name="input">The span to search for a match.</param>
        /// <param name="startat">The zero-based character position at which to start the search.</param>
        /// <returns>
        /// <see langword="true"/> if the regular expression finds a match; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        public bool IsMatch(ReadOnlySpan<char> input, int startat) =>
            RunSingleMatch(RegexRunnerMode.ExistenceRequired, -1, input, startat).Success;

        /// <summary>
        /// Searches the specified input string for the first occurrence of the specified regular expression.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Match(string, string)"/> method is equivalent to constructing a
        /// <see cref="Regex"/> object with the specified pattern and calling the instance
        /// <see cref="Match(string)"/> method. The regular expression engine caches the pattern.
        /// </para>
        /// <para>
        /// You can determine whether the regular expression pattern has been found in the input string by
        /// checking the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Group.Success"/> property. If a match
        /// is found, the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Capture.Value"/> property contains the
        /// matched substring. If no match is found, its value is <see cref="string.Empty"/>.
        /// </para>
        /// <para>
        /// You can retrieve subsequent matches by repeatedly calling the returned <see cref="System.Text.RegularExpressions.Match"/> object's
        /// <see cref="Match.NextMatch"/> method. You can also retrieve all matches in a single method call by
        /// calling the <see cref="Matches(string, string)"/> method.
        /// </para>
        /// </remarks>
        public static Match Match(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).Match(input);

        /// <summary>
        /// Searches the input string for the first occurrence of the specified regular expression, using the
        /// specified matching options.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Match(string, string, RegexOptions)"/> method is equivalent to constructing a
        /// <see cref="Regex"/> object with the <see cref="Regex(string, RegexOptions)"/> constructor and calling
        /// the instance <see cref="Match(string)"/> method.
        /// </para>
        /// <para>
        /// You can determine whether the regular expression pattern has been found in the input string by
        /// checking the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Group.Success"/> property. You can
        /// retrieve subsequent matches by repeatedly calling the returned <see cref="System.Text.RegularExpressions.Match"/> object's
        /// <see cref="Match.NextMatch"/> method. You can also retrieve all matches in a single method call by
        /// calling the <see cref="Matches(string, string, RegexOptions)"/> method.
        /// </para>
        /// </remarks>
        public static Match Match(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Match(input);

        /// <summary>
        /// Searches the input string for the first occurrence of the specified regular expression, using the
        /// specified matching options and time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate
        /// that the method should not time out.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Match(string, string, RegexOptions, TimeSpan)"/> method is equivalent to
        /// constructing a <see cref="Regex"/> object with the
        /// <see cref="Regex(string, RegexOptions, TimeSpan)"/> constructor and calling the instance
        /// <see cref="Match(string)"/> method.
        /// </para>
        /// <para>
        /// You can determine whether the regular expression pattern has been found in the input string by
        /// checking the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Group.Success"/> property. You can
        /// retrieve subsequent matches by repeatedly calling the returned <see cref="System.Text.RegularExpressions.Match"/> object's
        /// <see cref="Match.NextMatch"/> method.
        /// </para>
        /// <para>
        /// The <paramref name="matchTimeout"/> parameter specifies how long a pattern matching method should try
        /// to find a match before it times out. Setting a time-out interval prevents regular expressions that
        /// rely on excessive backtracking from appearing to stop responding when they process input that contains
        /// near matches. If no match is found in that time interval, the method throws a
        /// <see cref="RegexMatchTimeoutException"/> exception. <paramref name="matchTimeout"/> overrides any
        /// default time-out value defined for the application domain in which the method executes.
        /// </para>
        /// </remarks>
        public static Match Match(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Match(input);

        /// <summary>
        /// Searches the specified input string for the first occurrence of the regular expression specified in
        /// the <see cref="Regex"/> constructor.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Match(string)"/> method returns the first substring that matches a regular expression
        /// pattern in an input string.
        /// </para>
        /// <para>
        /// You can determine whether the regular expression pattern has been found in the input string by
        /// checking the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Group.Success"/> property. If a match
        /// is found, the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Capture.Value"/> property contains the
        /// matched substring. If no match is found, its value is <see cref="string.Empty"/>.
        /// </para>
        /// <para>
        /// This method returns the first match. You can retrieve subsequent matches by repeatedly calling the
        /// returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Match.NextMatch"/> method. You can also retrieve all
        /// matches in a single method call by calling <see cref="Matches(string)"/>.
        /// </para>
        /// <para>
        /// The <see cref="RegexMatchTimeoutException"/> exception is thrown if the execution time of the matching
        /// operation exceeds the time-out interval specified by the
        /// <see cref="Regex(string, RegexOptions, TimeSpan)"/> constructor. If you do not set a time-out interval
        /// when you call the constructor, the exception is thrown if the operation exceeds any time-out value
        /// established for the application domain in which the <see cref="Regex"/> object is created. If no
        /// time-out is defined in the <see cref="Regex"/> constructor call or in the application domain's
        /// properties, or if the time-out value is <see cref="Regex.InfiniteMatchTimeout"/>, no exception
        /// is thrown.
        /// </para>
        /// </remarks>
        public Match Match(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, 0, input.Length, RightToLeft ? input.Length : 0)!;
        }

        /// <summary>
        /// Searches the input string for the first occurrence of a regular expression, beginning at the
        /// specified starting position in the string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="startat">The zero-based character position at which to start the search.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// For more information about <paramref name="startat"/>, see
        /// <see href="https://github.com/dotnet/docs/blob/main/docs/fundamentals/runtime-libraries/system-text-regularexpressions-regex-match.md">
        /// Supplemental API remarks for Regex.Match</see>.
        /// </remarks>
        public Match Match(string input, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, 0, input.Length, startat)!;
        }

        /// <summary>
        /// Searches the input string for the first occurrence of a regular expression, beginning at the
        /// specified starting position and searching only the specified number of characters.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="beginning">The zero-based character position in the input string that defines the
        /// leftmost position to be searched.</param>
        /// <param name="length">The number of characters in the substring to include in the search.</param>
        /// <returns>An object that contains information about the match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="beginning"/> is less than zero or greater than the length of
        /// <paramref name="input"/>.
        /// -or-
        /// <paramref name="length"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// -or-
        /// <paramref name="beginning"/> + <paramref name="length"/> - 1 identifies a position that is outside
        /// the range of <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Match(string, int, int)"/> method searches the portion of <paramref name="input"/>
        /// defined by the <paramref name="beginning"/> and <paramref name="length"/> parameters for the regular
        /// expression pattern. <paramref name="beginning"/> always defines the index of the leftmost character
        /// to include in the search, and <paramref name="length"/> defines the maximum number of characters to
        /// search. Together, they define the range of the search. The behavior is exactly as if the input was
        /// effectively <c>input.Substring(beginning, length)</c>, except that the index of any match is counted
        /// relative to the start of <paramref name="input"/>. This means that any anchors or zero-width
        /// assertions at the start or end of the pattern behave as if there is no input outside of this range.
        /// </para>
        /// <para>
        /// If the search proceeds from left to right (the default), the regular expression engine searches from
        /// the character at index <paramref name="beginning"/> to the character at index
        /// <paramref name="beginning"/> + <paramref name="length"/> - 1. If the regular expression engine was
        /// instantiated by using the <see cref="RegexOptions.RightToLeft"/> option, the engine searches from the
        /// character at index <paramref name="beginning"/> + <paramref name="length"/> - 1 to the character at
        /// index <paramref name="beginning"/>.
        /// </para>
        /// <para>
        /// This method returns the first match found within this range. You can retrieve subsequent matches by
        /// repeatedly calling the returned <see cref="System.Text.RegularExpressions.Match"/> object's <see cref="Match.NextMatch"/> method.
        /// </para>
        /// </remarks>
        public Match Match(string input, int beginning, int length)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return RunSingleMatch(RegexRunnerMode.FullMatchRequired, -1, input, beginning, length, RightToLeft ? beginning + length : beginning)!;
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of a specified regular expression.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <returns>
        /// A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the
        /// method returns an empty collection object.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Matches(string, string)"/> method is similar to the
        /// <see cref="Match(string, string)"/> method, except that it returns information about all the matches
        /// found in the input string, instead of a single match.
        /// </para>
        /// <para>
        /// The static <c>Matches</c> methods are equivalent to constructing a <see cref="Regex"/> object with
        /// the specified regular expression pattern and calling the instance method <c>Matches</c>.
        /// </para>
        /// <para>
        /// The <see cref="Matches(string, string)"/> method uses lazy evaluation to populate the returned
        /// <see cref="MatchCollection"/> object. Accessing members of this collection such as
        /// <see cref="MatchCollection.Count"/> and <see cref="MatchCollection.CopyTo(System.Array, int)"/> causes the collection to
        /// be populated immediately. To take advantage of lazy evaluation, iterate the collection by using
        /// <see langword="foreach"/>.
        /// </para>
        /// <para>
        /// Because of its lazy evaluation, calling the <see cref="Matches(string, string)"/> method does not
        /// throw a <see cref="RegexMatchTimeoutException"/> exception. However, the exception is thrown when an
        /// operation is performed on the <see cref="MatchCollection"/> object returned by this method, if a
        /// matching operation exceeds the time-out interval.
        /// </para>
        /// </remarks>
        public static MatchCollection Matches(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern) =>
            RegexCache.GetOrAdd(pattern).Matches(input);

        /// <summary>
        /// Searches the specified input string for all occurrences of a specified regular expression, using the
        /// specified matching options.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <returns>
        /// A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the
        /// method returns an empty collection object.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Matches(string, string, RegexOptions)"/> method is similar to the
        /// <see cref="Match(string, string, RegexOptions)"/> method, except that it returns information about all
        /// the matches found in the input string, instead of a single match.
        /// </para>
        /// <para>
        /// The static <c>Matches</c> methods are equivalent to constructing a <see cref="Regex"/> object with
        /// the specified regular expression pattern and calling the instance method <c>Matches</c>.
        /// </para>
        /// <para>
        /// The <see cref="Matches(string, string, RegexOptions)"/> method uses lazy evaluation to populate the
        /// returned <see cref="MatchCollection"/> object. Accessing members of this collection such as
        /// <see cref="MatchCollection.Count"/> and <see cref="MatchCollection.CopyTo(System.Array, int)"/> causes the collection to
        /// be populated immediately. To take advantage of lazy evaluation, iterate the collection by using
        /// <see langword="foreach"/>.
        /// </para>
        /// <para>
        /// Because of its lazy evaluation, calling the <see cref="Matches(string, string, RegexOptions)"/>
        /// method does not throw a <see cref="RegexMatchTimeoutException"/> exception. However, the exception is
        /// thrown when an operation is performed on the <see cref="MatchCollection"/> object returned by this
        /// method, if a matching operation exceeds the time-out interval.
        /// </para>
        /// </remarks>
        public static MatchCollection Matches(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Matches(input);

        /// <summary>
        /// Searches the specified input string for all occurrences of a specified regular expression, using the
        /// specified matching options and time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specify options for matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate
        /// that the method should not time out.</param>
        /// <returns>
        /// A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the
        /// method returns an empty collection object.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Matches(string, string, RegexOptions, TimeSpan)"/> method is similar to the
        /// <see cref="Match(string, string, RegexOptions, TimeSpan)"/> method, except that it returns information
        /// about all the matches found in the input string, instead of a single match.
        /// </para>
        /// <para>
        /// The static <c>Matches</c> methods are equivalent to constructing a <see cref="Regex"/> object with
        /// the specified regular expression pattern and calling the instance method <c>Matches</c>.
        /// </para>
        /// <para>
        /// The <see cref="Matches(string, string, RegexOptions, TimeSpan)"/> method uses lazy evaluation to
        /// populate the returned <see cref="MatchCollection"/> object. Accessing members of this collection
        /// such as <see cref="MatchCollection.Count"/> and <see cref="MatchCollection.CopyTo(System.Array, int)"/> causes the
        /// collection to be populated immediately. To take advantage of lazy evaluation, iterate the collection
        /// by using <see langword="foreach"/>.
        /// </para>
        /// <para>
        /// Because of its lazy evaluation, calling the
        /// <see cref="Matches(string, string, RegexOptions, TimeSpan)"/> method does not throw a
        /// <see cref="RegexMatchTimeoutException"/> exception. However, an exception is thrown when an operation
        /// is performed on the <see cref="MatchCollection"/> object returned by this method, if a matching
        /// operation exceeds the time-out interval specified by the <paramref name="matchTimeout"/> parameter.
        /// </para>
        /// </remarks>
        public static MatchCollection Matches(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Matches(input);

        /// <summary>
        /// Searches the specified input string for all occurrences of a regular expression.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>
        /// A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the
        /// method returns an empty collection object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Matches(string)"/> method is similar to the <see cref="Match(string)"/> method,
        /// except that it returns information about all the matches found in the input string, instead of a
        /// single match. The collection includes only matches and terminates at the first non-match.
        /// </para>
        /// <para>
        /// The <see cref="Matches(string)"/> method uses lazy evaluation to populate the returned
        /// <see cref="MatchCollection"/> object. Accessing members of this collection such as
        /// <see cref="MatchCollection.Count"/> and <see cref="MatchCollection.CopyTo(System.Array, int)"/> causes the collection to
        /// be populated immediately. To take advantage of lazy evaluation, iterate the collection by using
        /// <see langword="foreach"/>.
        /// </para>
        /// <para>
        /// Because of its lazy evaluation, calling the <see cref="Matches(string)"/> method does not throw a
        /// <see cref="RegexMatchTimeoutException"/> exception. However, the exception is thrown when an operation
        /// is performed on the <see cref="MatchCollection"/> object returned by this method, if the
        /// <see cref="Regex.MatchTimeout"/> property is not <see cref="Regex.InfiniteMatchTimeout"/> and a
        /// matching operation exceeds the time-out interval.
        /// </para>
        /// </remarks>
        public MatchCollection Matches(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return new MatchCollection(this, input, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of a regular expression, beginning at the
        /// specified starting position in the string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="startat">The character position in the input string at which to start the search.</param>
        /// <returns>
        /// A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the
        /// method returns an empty collection object.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The <see cref="Matches(string, int)"/> method is similar to the <see cref="Match(string, int)"/>
        /// method, except that it returns information about all the matches found in the input string, instead
        /// of a single match.
        /// </para>
        /// <para>
        /// For more details about <paramref name="startat"/>, see the remarks for
        /// <see cref="Match(string, int)"/>.
        /// </para>
        /// <para>
        /// The <see cref="Matches(string, int)"/> method uses lazy evaluation to populate the returned
        /// <see cref="MatchCollection"/> object. Accessing members of this collection such as
        /// <see cref="MatchCollection.Count"/> and <see cref="MatchCollection.CopyTo(System.Array, int)"/> causes the collection to
        /// be populated immediately. To take advantage of lazy evaluation, iterate the collection by using
        /// <see langword="foreach"/>.
        /// </para>
        /// <para>
        /// Because of its lazy evaluation, calling the <see cref="Matches(string, int)"/> method does not throw
        /// a <see cref="RegexMatchTimeoutException"/> exception. However, the exception is thrown when an
        /// operation is performed on the <see cref="MatchCollection"/> object returned by this method, if the
        /// <see cref="Regex.MatchTimeout"/> property is not <see cref="Regex.InfiniteMatchTimeout"/> and a
        /// matching operation exceeds the time-out interval.
        /// </para>
        /// </remarks>
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
