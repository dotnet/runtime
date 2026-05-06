// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the method that is called each time a regular expression match is found during a
    /// <see cref="Regex.Replace(string, MatchEvaluator)"/> method operation.
    /// </summary>
    /// <param name="match">The <see cref="Match"/> object that represents a single regular expression
    /// match during a <see cref="Regex.Replace(string, MatchEvaluator)"/> method operation.</param>
    /// <returns>A string returned by the method that is represented by the
    /// <see cref="MatchEvaluator"/> delegate.</returns>
    /// <remarks>
    /// You can use a <see cref="MatchEvaluator"/> delegate method to perform a custom verification or
    /// manipulation operation for each match found by a replacement method such as
    /// <see cref="Regex.Replace(string, MatchEvaluator)"/>. For each matched string, the
    /// <see cref="Regex.Replace(string, MatchEvaluator)"/> method calls the
    /// <see cref="MatchEvaluator"/> delegate method with a <see cref="Match"/> object that represents
    /// the match. The delegate method performs whatever processing you prefer and returns a string that
    /// the <see cref="Regex.Replace(string, MatchEvaluator)"/> method substitutes for the matched string.
    /// </remarks>
    public delegate string MatchEvaluator(Match match);

    internal delegate bool MatchCallback<TState>(ref TState state, Match match);

    public partial class Regex
    {
        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression pattern
        /// with a specified replacement string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the method
        /// returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="replacement"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Replace(string, string, string)"/> methods are equivalent to constructing a
        /// <see cref="Regex"/> object with the specified regular expression pattern and calling the instance method
        /// <see cref="Replace(string, string)"/>.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter specifies the string that replaces each match in
        /// <paramref name="input"/>. <paramref name="replacement"/> can consist of any combination of literal text
        /// and <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// Substitutions are the only regular expression language elements that are recognized in a replacement
        /// pattern.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement) =>
            RegexCache.GetOrAdd(pattern).Replace(input, replacement);

        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression with a
        /// specified replacement string. Specified options modify the matching operation.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for
        /// matching.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the method
        /// returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="replacement"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Replace(string, string, string, RegexOptions)"/> methods are equivalent to
        /// constructing a <see cref="Regex"/> object with the specified regular expression pattern and calling the
        /// instance method <see cref="Replace(string, string)"/>.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter specifies the string that replaces each match in
        /// <paramref name="input"/>. <paramref name="replacement"/> can consist of any combination of literal text
        /// and <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// Substitutions are the only regular expression language elements that are recognized in a replacement
        /// pattern.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the <paramref name="options"/> parameter,
        /// the search for matches begins at the end of the input string and moves left; otherwise, the search
        /// begins at the start of the input string and moves right.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, string replacement, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, replacement);

        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression with a
        /// specified replacement string. Additional parameters specify options that modify the matching operation
        /// and a time-out interval if no match is found.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for
        /// matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to
        /// indicate that the method should not time out.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the method
        /// returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="replacement"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The static <see cref="Replace(string, string, string, RegexOptions, TimeSpan)"/> methods are equivalent
        /// to constructing a <see cref="Regex"/> object with the specified regular expression pattern and calling
        /// the instance method <see cref="Replace(string, string)"/>.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter specifies the string that replaces each match in
        /// <paramref name="input"/>. <paramref name="replacement"/> can consist of any combination of literal text
        /// and <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// Substitutions are the only regular expression language elements that are recognized in a replacement
        /// pattern.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the <paramref name="options"/> parameter,
        /// the search for matches begins at the end of the input string and moves left; otherwise, the search
        /// begins at the start of the input string and moves right.
        /// </para>
        /// <para>
        /// The <paramref name="matchTimeout"/> parameter specifies how long a pattern matching method should try
        /// to find a match before it times out. Setting a time-out interval prevents regular expressions that rely
        /// on excessive backtracking from appearing to stop responding when they process input that contains near
        /// matches. <paramref name="matchTimeout"/> overrides any default time-out value defined for the
        /// application domain in which the method executes.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, replacement);

        /// <summary>
        /// In a specified input string, replaces all strings that match a regular expression pattern with a
        /// specified replacement string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="replacement"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The search for the pattern begins at the beginning of the <paramref name="input"/> string.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter specifies the string that replaces each match.
        /// <paramref name="replacement"/> can consist of any combination of literal text and
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// Substitutions are the only regular expression language elements that are recognized in a replacement
        /// pattern.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public string Replace(string input, string replacement)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(input, replacement, -1, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// In a specified input string, replaces a specified maximum number of strings that match a regular
        /// expression pattern with a specified replacement string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="count">The maximum number of times the replacement can occur.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="replacement"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// The search begins at the start of the <paramref name="input"/> string. The <paramref name="replacement"/>
        /// parameter specifies the string that replaces each match and supports
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// </para>
        /// <para>
        /// If <paramref name="count"/> is negative, replacements continue to the end of the string.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public string Replace(string input, string replacement, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(input, replacement, count, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// In a specified input substring, replaces a specified maximum number of strings that match a regular
        /// expression pattern with a specified replacement string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="count">The maximum number of times the replacement can occur.</param>
        /// <param name="startat">The character position in the input string where the search begins.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that the replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="replacement"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// For more details about <paramref name="startat"/>, see the Remarks section of
        /// <see cref="Match(string, int)"/>.
        /// </para>
        /// <para>
        /// The <paramref name="replacement"/> parameter specifies the string that replaces each match and supports
        /// <see href="https://learn.microsoft.com/dotnet/standard/base-types/substitutions-in-regular-expressions">substitutions</see>.
        /// </para>
        /// <para>
        /// If <paramref name="count"/> is negative, replacements continue to the end of the string.
        /// </para>
        /// </remarks>
        public string Replace(string input, string replacement, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }
            if (replacement is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
            }

            // Gets the weakly cached replacement helper or creates one if there isn't one already,
            // then uses it to perform the replace.
            return
                RegexReplacement.GetOrCreate(RegexReplacementWeakReference, replacement, caps!, capsize, capnames!, roptions).
                Replace(this, input, count, startat);
        }

        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression with a
        /// string returned by a <see cref="MatchEvaluator"/> delegate.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="evaluator"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful for replacing a regular expression match if any of the following conditions is
        /// true: the replacement string cannot readily be specified by a regular expression replacement pattern,
        /// the replacement string results from processing the matched string, or the replacement string results
        /// from conditional processing.
        /// </para>
        /// <para>
        /// The method is equivalent to calling the <see cref="Regex.Matches(string, string)"/> method and passing
        /// each <see cref="System.Text.RegularExpressions.Match"/> object in the returned <see cref="MatchCollection"/> to the
        /// <paramref name="evaluator"/> delegate.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, MatchEvaluator evaluator) =>
            RegexCache.GetOrAdd(pattern).Replace(input, evaluator);

        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression with a
        /// string returned by a <see cref="MatchEvaluator"/> delegate. Specified options modify the matching
        /// operation.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for
        /// matching.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="evaluator"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful for replacing a regular expression match if any of the following conditions is
        /// true: the replacement string cannot readily be specified by a regular expression replacement pattern,
        /// the replacement string results from processing the matched string, or the replacement string results
        /// from conditional processing.
        /// </para>
        /// <para>
        /// The method is equivalent to calling the <see cref="Regex.Matches(string, string)"/> method and passing
        /// each <see cref="System.Text.RegularExpressions.Match"/> object in the returned <see cref="MatchCollection"/> to the
        /// <paramref name="evaluator"/> delegate.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the <paramref name="options"/> parameter,
        /// the search for matches begins at the end of the input string and moves left; otherwise, the search
        /// begins at the start of the input string and moves right.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, MatchEvaluator evaluator, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, evaluator);

        /// <summary>
        /// In a specified input string, replaces all substrings that match a specified regular expression with a
        /// string returned by a <see cref="MatchEvaluator"/> delegate. Additional parameters specify options that
        /// modify the matching operation and a time-out interval if no match is found.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <param name="options">A bitwise combination of the enumeration values that provide options for
        /// matching.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to
        /// indicate that the method should not time out.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If <paramref name="pattern"/> is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">A regular expression parsing error occurred.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/>, <paramref name="pattern"/>, or <paramref name="evaluator"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="options"/> is not a valid bitwise combination of <see cref="RegexOptions"/> values.
        /// -or-
        /// <paramref name="matchTimeout"/> is negative, zero, or greater than approximately 24 days.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful for replacing a regular expression match if any of the following conditions is
        /// true: the replacement string cannot readily be specified by a regular expression replacement pattern,
        /// the replacement string results from processing the matched string, or the replacement string results
        /// from conditional processing.
        /// </para>
        /// <para>
        /// The method is equivalent to calling the <see cref="Regex.Matches(string, string)"/> method and passing
        /// each <see cref="System.Text.RegularExpressions.Match"/> object in the returned <see cref="MatchCollection"/> to the
        /// <paramref name="evaluator"/> delegate.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// <para>
        /// If you specify <see cref="RegexOptions.RightToLeft"/> for the <paramref name="options"/> parameter,
        /// the search for matches begins at the end of the input string and moves left; otherwise, the search
        /// begins at the start of the input string and moves right.
        /// </para>
        /// <para>
        /// The <paramref name="matchTimeout"/> parameter specifies how long a pattern matching method should try
        /// to find a match before it times out. <paramref name="matchTimeout"/> overrides any default time-out
        /// value defined for the application domain in which the method executes.
        /// </para>
        /// </remarks>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, evaluator);

        /// <summary>
        /// In a specified input string, replaces all strings that match a specified regular expression with a
        /// string returned by a <see cref="MatchEvaluator"/> delegate.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="evaluator"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful for replacing a regular expression match if any of the following conditions is
        /// true: the replacement string cannot readily be specified by a regular expression replacement pattern,
        /// the replacement string results from processing the matched string, or the replacement string results
        /// from conditional processing.
        /// </para>
        /// <para>
        /// The method is equivalent to calling the <see cref="Regex.Matches(string)"/> method and passing each
        /// <see cref="System.Text.RegularExpressions.Match"/> object in the returned <see cref="MatchCollection"/> to the
        /// <paramref name="evaluator"/> delegate.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public string Replace(string input, MatchEvaluator evaluator)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, -1, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// In a specified input string, replaces a specified maximum number of strings that match a regular
        /// expression pattern with a string returned by a <see cref="MatchEvaluator"/> delegate.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <param name="count">The maximum number of times the replacement will occur.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="evaluator"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method is useful for replacing a regular expression match if any of the following conditions is
        /// true: the replacement string cannot readily be specified by a regular expression replacement pattern,
        /// the replacement string results from processing the matched string, or the replacement string results
        /// from conditional processing.
        /// </para>
        /// <para>
        /// The method is equivalent to calling the <see cref="Regex.Matches(string)"/> method and passing the
        /// first <paramref name="count"/> <see cref="System.Text.RegularExpressions.Match"/> objects in the returned
        /// <see cref="MatchCollection"/> to the <paramref name="evaluator"/> delegate.
        /// </para>
        /// <para>
        /// If <paramref name="count"/> is negative, replacements continue to the end of the string.
        /// </para>
        /// <para>
        /// Because the method returns <paramref name="input"/> unchanged if there is no match, you can use the
        /// <see cref="object.ReferenceEquals(object?, object?)"/> method to determine whether the method has made
        /// any replacements.
        /// </para>
        /// </remarks>
        public string Replace(string input, MatchEvaluator evaluator, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, count, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// In a specified input substring, replaces a specified maximum number of strings that match a regular
        /// expression pattern with a string returned by a <see cref="MatchEvaluator"/> delegate.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original
        /// matched string or a replacement string.</param>
        /// <param name="count">The maximum number of times the replacement will occur.</param>
        /// <param name="startat">The character position in the input string where the search begins.</param>
        /// <returns>
        /// A new string that is identical to the input string, except that a replacement string takes the place
        /// of each matched string. If the regular expression pattern is not matched in the current instance, the
        /// method returns the current instance unchanged.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> or <paramref name="evaluator"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startat"/> is less than zero or greater than the length of <paramref name="input"/>.
        /// </exception>
        /// <exception cref="RegexMatchTimeoutException">A time-out occurred.</exception>
        /// <remarks>
        /// <para>
        /// For more details about <paramref name="startat"/>, see the Remarks section of
        /// <see cref="Match(string, int)"/>.
        /// </para>
        /// <para>
        /// The method passes the first <paramref name="count"/> <see cref="System.Text.RegularExpressions.Match"/> objects to the
        /// <paramref name="evaluator"/> delegate.
        /// </para>
        /// </remarks>
        public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement evaluator.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        private static string Replace(MatchEvaluator evaluator, Regex regex, string input, int count, int startat)
        {
            if (evaluator is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evaluator);
            }
            if (count < -1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
            }
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            if (count == 0)
            {
                return input;
            }

            var state = (segments: new StructListBuilder<ReadOnlyMemory<char>>(), evaluator, prevat: 0, input, count);

            if (!regex.RightToLeft)
            {
                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(state.prevat, input.Length - state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(0, state.prevat));
                state.segments.AsSpan().Reverse();
            }

            return SegmentsToStringAndDispose(ref state.segments);
        }

        /// <summary>Creates a string from all the segments in the builder and then disposes of the builder.</summary>
        internal static string SegmentsToStringAndDispose(ref StructListBuilder<ReadOnlyMemory<char>> segments)
        {
            Span<ReadOnlyMemory<char>> span = segments.AsSpan();

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                length += span[i].Length;
            }

            string result = string.Create(length, span, static (dest, span) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    ReadOnlySpan<char> segment = span[i].Span;
                    segment.CopyTo(dest);
                    dest = dest.Slice(segment.Length);
                }
            });

            segments.Dispose();

            return result;
        }
    }
}
