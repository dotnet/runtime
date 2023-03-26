#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace Xunit
{
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	partial class Assert
	{
		/// <summary>
		/// Verifies that a string contains a given sub-string, using the current culture.
		/// </summary>
		/// <param name="expectedSubstring">The sub-string expected to be in the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-string is not present inside the string</exception>
		public static void Contains(
			string expectedSubstring,
#if XUNIT_NULLABLE
			string? actualString) =>
#else
			string actualString) =>
#endif
				Contains(expectedSubstring, actualString, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a string contains a given sub-string, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubstring">The sub-string expected to be in the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-string is not present inside the string</exception>
		public static void Contains(
			string expectedSubstring,
#if XUNIT_NULLABLE
			string? actualString,
#else
			string actualString,
#endif
			StringComparison comparisonType)
		{
			GuardArgumentNotNull(nameof(expectedSubstring), expectedSubstring);

			if (actualString == null || actualString.IndexOf(expectedSubstring, comparisonType) < 0)
				throw new ContainsException(expectedSubstring, actualString);
		}

		/// <summary>
		/// Verifies that a string does not contain a given sub-string, using the current culture.
		/// </summary>
		/// <param name="expectedSubstring">The sub-string which is expected not to be in the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-string is present inside the string</exception>
		public static void DoesNotContain(
			string expectedSubstring,
#if XUNIT_NULLABLE
			string? actualString) =>
#else
			string actualString) =>
#endif
				DoesNotContain(expectedSubstring, actualString, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a string does not contain a given sub-string, using the current culture.
		/// </summary>
		/// <param name="expectedSubstring">The sub-string which is expected not to be in the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-string is present inside the given string</exception>
		public static void DoesNotContain(
			string expectedSubstring,
#if XUNIT_NULLABLE
			string? actualString,
#else
			string actualString,
#endif
			StringComparison comparisonType)
		{
			GuardArgumentNotNull(nameof(expectedSubstring), expectedSubstring);

			if (actualString != null && actualString.IndexOf(expectedSubstring, comparisonType) >= 0)
				throw new DoesNotContainException(expectedSubstring, actualString);
		}

		/// <summary>
		/// Verifies that a string starts with a given string, using the current culture.
		/// </summary>
		/// <param name="expectedStartString">The string expected to be at the start of the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the string does not start with the expected string</exception>
		public static void StartsWith(
#if XUNIT_NULLABLE
			string? expectedStartString,
			string? actualString) =>
#else
			string expectedStartString,
			string actualString) =>
#endif
				StartsWith(expectedStartString, actualString, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a string starts with a given string, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartString">The string expected to be at the start of the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the string does not start with the expected string</exception>
		public static void StartsWith(
#if XUNIT_NULLABLE
			string? expectedStartString,
			string? actualString,
#else
			string expectedStartString,
			string actualString,
#endif
			StringComparison comparisonType)
		{
			if (expectedStartString == null || actualString == null || !actualString.StartsWith(expectedStartString, comparisonType))
				throw new StartsWithException(expectedStartString, actualString);
		}

		/// <summary>
		/// Verifies that a string ends with a given string, using the current culture.
		/// </summary>
		/// <param name="expectedEndString">The string expected to be at the end of the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the string does not end with the expected string</exception>
		public static void EndsWith(
#if XUNIT_NULLABLE
			string? expectedEndString,
			string? actualString) =>
#else
			string expectedEndString,
			string actualString) =>
#endif
				EndsWith(expectedEndString, actualString, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a string ends with a given string, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndString">The string expected to be at the end of the string</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the string does not end with the expected string</exception>
		public static void EndsWith(
#if XUNIT_NULLABLE
			string? expectedEndString,
			string? actualString,
#else
			string expectedEndString,
			string actualString,
#endif
			StringComparison comparisonType)
		{
			if (expectedEndString == null || actualString == null || !actualString.EndsWith(expectedEndString, comparisonType))
				throw new EndsWithException(expectedEndString, actualString);
		}

		/// <summary>
		/// Verifies that a string matches a regular expression.
		/// </summary>
		/// <param name="expectedRegexPattern">The regex pattern expected to match</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="MatchesException">Thrown when the string does not match the regex pattern</exception>
		public static void Matches(
			string expectedRegexPattern,
#if XUNIT_NULLABLE
			string? actualString)
#else
			string actualString)
#endif
		{
			GuardArgumentNotNull(nameof(expectedRegexPattern), expectedRegexPattern);

			if (actualString == null || !Regex.IsMatch(actualString, expectedRegexPattern))
				throw new MatchesException(expectedRegexPattern, actualString);
		}

		/// <summary>
		/// Verifies that a string matches a regular expression.
		/// </summary>
		/// <param name="expectedRegex">The regex expected to match</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="MatchesException">Thrown when the string does not match the regex</exception>
		public static void Matches(
			Regex expectedRegex,
#if XUNIT_NULLABLE
			string? actualString)
#else
			string actualString)
#endif
		{
			GuardArgumentNotNull(nameof(expectedRegex), expectedRegex);

			if (actualString == null || !expectedRegex.IsMatch(actualString))
				throw new MatchesException(expectedRegex.ToString(), actualString);
		}

		/// <summary>
		/// Verifies that a string does not match a regular expression.
		/// </summary>
		/// <param name="expectedRegexPattern">The regex pattern expected not to match</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="DoesNotMatchException">Thrown when the string matches the regex pattern</exception>
		public static void DoesNotMatch(
			string expectedRegexPattern,
#if XUNIT_NULLABLE
			string? actualString)
#else
			string actualString)
#endif
		{
			GuardArgumentNotNull(nameof(expectedRegexPattern), expectedRegexPattern);

			if (actualString != null && Regex.IsMatch(actualString, expectedRegexPattern))
				throw new DoesNotMatchException(expectedRegexPattern, actualString);
		}

		/// <summary>
		/// Verifies that a string does not match a regular expression.
		/// </summary>
		/// <param name="expectedRegex">The regex expected not to match</param>
		/// <param name="actualString">The string to be inspected</param>
		/// <exception cref="DoesNotMatchException">Thrown when the string matches the regex</exception>
		public static void DoesNotMatch(
			Regex expectedRegex,
#if XUNIT_NULLABLE
			string? actualString)
#else
			string actualString)
#endif
		{
			GuardArgumentNotNull(nameof(expectedRegex), expectedRegex);

			if (actualString != null && expectedRegex.IsMatch(actualString))
				throw new DoesNotMatchException(expectedRegex.ToString(), actualString);
		}

		/// <summary>
		/// Verifies that two strings are equivalent.
		/// </summary>
		/// <param name="expected">The expected string value.</param>
		/// <param name="actual">The actual string value.</param>
		/// <exception cref="EqualException">Thrown when the strings are not equivalent.</exception>
		public static void Equal(
#if XUNIT_NULLABLE
			string? expected,
			string? actual) =>
#else
			string expected,
			string actual) =>
#endif
				Equal(expected, actual, false, false, false);

		/// <summary>
		/// Verifies that two strings are equivalent.
		/// </summary>
		/// <param name="expected">The expected string value.</param>
		/// <param name="actual">The actual string value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <param name="ignoreAllWhiteSpace">If set to <c>true</c>, ignores all white space differences during comparison.</param>
		/// <exception cref="EqualException">Thrown when the strings are not equivalent.</exception>
		public static void Equal(
#if XUNIT_NULLABLE
			string? expected,
			string? actual,
#else
			string expected,
			string actual,
#endif
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false,
			bool ignoreAllWhiteSpace = false)
		{
#if XUNIT_SPAN
			if (expected == null && actual == null)
				return;
			if (expected == null || actual == null)
				throw new EqualException(expected, actual, -1, -1);

			Equal(expected.AsSpan(), actual.AsSpan(), ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace);
#else
			// Start out assuming the one of the values is null
			int expectedIndex = -1;
			int actualIndex = -1;
			int expectedLength = 0;
			int actualLength = 0;

			if (expected == null)
			{
				if (actual == null)
					return;
			}
			else if (actual != null)
			{
				// Walk the string, keeping separate indices since we can skip variable amounts of
				// data based on ignoreLineEndingDifferences and ignoreWhiteSpaceDifferences.
				expectedIndex = 0;
				actualIndex = 0;
				expectedLength = expected.Length;
				actualLength = actual.Length;

				// Block used to fix edge case of Equal("", " ") when ignoreAllWhiteSpace enabled.
				if (ignoreAllWhiteSpace)
				{
					if (expectedLength == 0 && SkipWhitespace(actual, 0) == actualLength)
						return;
					if (actualLength == 0 && SkipWhitespace(expected, 0) == expectedLength)
						return;
				}

				while (expectedIndex < expectedLength && actualIndex < actualLength)
				{
					char expectedChar = expected[expectedIndex];
					char actualChar = actual[actualIndex];

					if (ignoreLineEndingDifferences && IsLineEnding(expectedChar) && IsLineEnding(actualChar))
					{
						expectedIndex = SkipLineEnding(expected, expectedIndex);
						actualIndex = SkipLineEnding(actual, actualIndex);
					}
					else if (ignoreAllWhiteSpace && (IsWhiteSpace(expectedChar) || IsWhiteSpace(actualChar)))
					{
						expectedIndex = SkipWhitespace(expected, expectedIndex);
						actualIndex = SkipWhitespace(actual, actualIndex);
					}
					else if (ignoreWhiteSpaceDifferences && IsWhiteSpace(expectedChar) && IsWhiteSpace(actualChar))
					{
						expectedIndex = SkipWhitespace(expected, expectedIndex);
						actualIndex = SkipWhitespace(actual, actualIndex);
					}
					else
					{
						if (ignoreCase)
						{
							expectedChar = Char.ToUpperInvariant(expectedChar);
							actualChar = Char.ToUpperInvariant(actualChar);
						}

						if (expectedChar != actualChar)
							break;

						expectedIndex++;
						actualIndex++;
					}
				}
			}

			if (expectedIndex < expectedLength || actualIndex < actualLength)
				throw new EqualException(expected, actual, expectedIndex, actualIndex);
#endif
		}

#if !XUNIT_SPAN
		static bool IsLineEnding(char c) =>
			c == '\r' || c == '\n';

		static bool IsWhiteSpace(char c) =>
			c == ' ' || c == '\t';

		static int SkipLineEnding(
			string value,
			int index)
		{
			if (value[index] == '\r')
				++index;
			if (index < value.Length && value[index] == '\n')
				++index;

			return index;
		}

		static int SkipWhitespace(
			string value,
			int index)
		{
			while (index < value.Length)
			{
				switch (value[index])
				{
					case ' ':
					case '\t':
						index++;
						break;

					default:
						return index;
				}
			}

			return index;
		}
#endif
	}
}
