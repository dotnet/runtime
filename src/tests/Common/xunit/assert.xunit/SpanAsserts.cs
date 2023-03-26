#if XUNIT_SPAN

#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Globalization;
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
		// NOTE: ref struct types (Span, ReadOnlySpan) are not Nullable, and thus there is no XUNIT_NULLABLE usage currently in this class
		// This also means that null spans are identical to empty spans, (both in essence point to a 0 sized array of whatever type)

		// NOTE: we could consider StartsWith<T> and EndsWith<T> and use the Span extension methods to check difference, but, the current
		// Exceptions for StartsWith and EndsWith are only built for string types, so those would need a change (or new non-string versions created).

		// NOTE: there is an implicit conversion operator on Span<T> to ReadOnlySpan<T> - however, I have found that the compiler sometimes struggles
		// with identifying the proper methods to use, thus I have overloaded quite a few of the assertions in terms of supplying both
		// Span and ReadOnlySpan based methods

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			Span<char> expectedSubSpan,
			Span<char> actualSpan) =>
				Contains((ReadOnlySpan<char>)expectedSubSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			Span<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan) =>
				Contains((ReadOnlySpan<char>)expectedSubSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			ReadOnlySpan<char> expectedSubSpan,
			Span<char> actualSpan) =>
				Contains(expectedSubSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			ReadOnlySpan<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan) =>
				Contains(expectedSubSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			Span<char> expectedSubSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains((ReadOnlySpan<char>)expectedSubSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			Span<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains((ReadOnlySpan<char>)expectedSubSpan, actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			ReadOnlySpan<char> expectedSubSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains(expectedSubSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span contains a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains(
			ReadOnlySpan<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			if (actualSpan.IndexOf(expectedSubSpan, comparisonType) < 0)
				throw new ContainsException(expectedSubSpan.ToString(), actualSpan.ToString());
		}

		/// <summary>
		/// Verifies that a span contains a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains<T>(
			Span<T> expectedSubSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					Contains((ReadOnlySpan<T>)expectedSubSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that a span contains a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains<T>(
			Span<T> expectedSubSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T> =>
					Contains((ReadOnlySpan<T>)expectedSubSpan, actualSpan);

		/// <summary>
		/// Verifies that a span contains a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains<T>(
			ReadOnlySpan<T> expectedSubSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					Contains(expectedSubSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that a span contains a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-span is not present inside the span</exception>
		public static void Contains<T>(
			ReadOnlySpan<T> expectedSubSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T>
		{
			if (actualSpan.IndexOf(expectedSubSpan) < 0)
				throw new ContainsException(expectedSubSpan.ToArray(), actualSpan.ToArray());
		}

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			Span<char> expectedSubSpan,
			Span<char> actualSpan) =>
				DoesNotContain((ReadOnlySpan<char>)expectedSubSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			Span<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan) =>
				DoesNotContain((ReadOnlySpan<char>)expectedSubSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			ReadOnlySpan<char> expectedSubSpan,
			Span<char> actualSpan) =>
				DoesNotContain(expectedSubSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			ReadOnlySpan<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan) =>
				DoesNotContain((ReadOnlySpan<char>)expectedSubSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			Span<char> expectedSubSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain((ReadOnlySpan<char>)expectedSubSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			Span<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain((ReadOnlySpan<char>)expectedSubSpan, actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			ReadOnlySpan<char> expectedSubSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain(expectedSubSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain(
			ReadOnlySpan<char> expectedSubSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			if (actualSpan.IndexOf(expectedSubSpan, comparisonType) > -1)
				throw new DoesNotContainException(expectedSubSpan.ToString(), actualSpan.ToString());
		}

		/// <summary>
		/// Verifies that a span does not contain a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain<T>(
			Span<T> expectedSubSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					DoesNotContain((ReadOnlySpan<T>)expectedSubSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain<T>(
			Span<T> expectedSubSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T> =>
					DoesNotContain((ReadOnlySpan<T>)expectedSubSpan, actualSpan);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain<T>(
			ReadOnlySpan<T> expectedSubSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					DoesNotContain(expectedSubSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that a span does not contain a given sub-span
		/// </summary>
		/// <param name="expectedSubSpan">The sub-span expected not to be in the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-span is present inside the span</exception>
		public static void DoesNotContain<T>(
			ReadOnlySpan<T> expectedSubSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T>
		{
			if (actualSpan.IndexOf(expectedSubSpan) > -1)
				throw new DoesNotContainException(expectedSubSpan.ToArray(), actualSpan.ToArray());
		}

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			Span<char> expectedStartSpan,
			Span<char> actualSpan) =>
				StartsWith((ReadOnlySpan<char>)expectedStartSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			Span<char> expectedStartSpan,
			ReadOnlySpan<char> actualSpan) =>
				StartsWith((ReadOnlySpan<char>)expectedStartSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			ReadOnlySpan<char> expectedStartSpan,
			Span<char> actualSpan) =>
				StartsWith(expectedStartSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			ReadOnlySpan<char> expectedStartSpan,
			ReadOnlySpan<char> actualSpan) =>
				StartsWith(expectedStartSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			Span<char> expectedStartSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith((ReadOnlySpan<char>)expectedStartSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			Span<char> expectedStartSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith((ReadOnlySpan<char>)expectedStartSpan, actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			ReadOnlySpan<char> expectedStartSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith(expectedStartSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span starts with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartSpan">The sub-span expected to be at the start of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the span does not start with the expected subspan</exception>
		public static void StartsWith(
			ReadOnlySpan<char> expectedStartSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			if (!actualSpan.StartsWith(expectedStartSpan, comparisonType))
				throw new StartsWithException(expectedStartSpan.ToString(), actualSpan.ToString());
		}

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			Span<char> expectedEndSpan,
			Span<char> actualSpan) =>
				EndsWith((ReadOnlySpan<char>)expectedEndSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			Span<char> expectedEndSpan,
			ReadOnlySpan<char> actualSpan) =>
				EndsWith((ReadOnlySpan<char>)expectedEndSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			ReadOnlySpan<char> expectedEndSpan,
			Span<char> actualSpan) =>
				EndsWith(expectedEndSpan, (ReadOnlySpan<char>)actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			ReadOnlySpan<char> expectedEndSpan,
			ReadOnlySpan<char> actualSpan) =>
				EndsWith(expectedEndSpan, actualSpan, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			Span<char> expectedEndSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith((ReadOnlySpan<char>)expectedEndSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			Span<char> expectedEndSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith((ReadOnlySpan<char>)expectedEndSpan, actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			ReadOnlySpan<char> expectedEndSpan,
			Span<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith(expectedEndSpan, (ReadOnlySpan<char>)actualSpan, comparisonType);

		/// <summary>
		/// Verifies that a span ends with a given sub-span, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndSpan">The sub-span expected to be at the end of the span</param>
		/// <param name="actualSpan">The span to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the span does not end with the expected subspan</exception>
		public static void EndsWith(
			ReadOnlySpan<char> expectedEndSpan,
			ReadOnlySpan<char> actualSpan,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			if (!actualSpan.EndsWith(expectedEndSpan, comparisonType))
				throw new EndsWithException(expectedEndSpan.ToString(), actualSpan.ToString());
		}

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			Span<char> expectedSpan,
			Span<char> actualSpan) =>
				Equal((ReadOnlySpan<char>)expectedSpan, (ReadOnlySpan<char>)actualSpan, false, false, false, false);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			Span<char> expectedSpan,
			ReadOnlySpan<char> actualSpan) =>
				Equal((ReadOnlySpan<char>)expectedSpan, actualSpan, false, false, false, false);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			ReadOnlySpan<char> expectedSpan,
			Span<char> actualSpan) =>
				Equal(expectedSpan, (ReadOnlySpan<char>)actualSpan, false, false, false, false);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			ReadOnlySpan<char> expectedSpan,
			ReadOnlySpan<char> actualSpan) =>
				Equal(expectedSpan, actualSpan, false, false, false, false);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <param name="ignoreAllWhiteSpace">If set to <c>true</c>, ignores all white space differences during comparison.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			Span<char> expectedSpan,
			Span<char> actualSpan,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false,
			bool ignoreAllWhiteSpace = false) =>
				Equal((ReadOnlySpan<char>)expectedSpan, (ReadOnlySpan<char>)actualSpan, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <param name="ignoreAllWhiteSpace">If set to <c>true</c>, ignores all white space differences during comparison.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			Span<char> expectedSpan,
			ReadOnlySpan<char> actualSpan,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false,
			bool ignoreAllWhiteSpace = false) =>
				Equal((ReadOnlySpan<char>)expectedSpan, actualSpan, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <param name="ignoreAllWhiteSpace">If set to <c>true</c>, removes all whitespaces and tabs before comparing.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			ReadOnlySpan<char> expectedSpan,
			Span<char> actualSpan,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false,
			bool ignoreAllWhiteSpace = false) =>
				Equal(expectedSpan, (ReadOnlySpan<char>)actualSpan, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <param name="ignoreAllWhiteSpace">If set to <c>true</c>, ignores all white space differences during comparison.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal(
			ReadOnlySpan<char> expectedSpan,
			ReadOnlySpan<char> actualSpan,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false,
			bool ignoreAllWhiteSpace = false)
		{
			// Walk the string, keeping separate indices since we can skip variable amounts of
			// data based on ignoreLineEndingDifferences and ignoreWhiteSpaceDifferences.
			var expectedIndex = 0;
			var actualIndex = 0;
			var expectedLength = expectedSpan.Length;
			var actualLength = actualSpan.Length;

			// Block used to fix edge case of Equal("", " ") when ignoreAllWhiteSpace enabled.
			if (ignoreAllWhiteSpace)
			{
				if (expectedLength == 0 && SkipWhitespace(actualSpan, 0) == actualLength)
					return;
				if (actualLength == 0 && SkipWhitespace(expectedSpan, 0) == expectedLength)
					return;
			}

			while (expectedIndex < expectedLength && actualIndex < actualLength)
			{
				var expectedChar = expectedSpan[expectedIndex];
				var actualChar = actualSpan[actualIndex];

				if (ignoreLineEndingDifferences && IsLineEnding(expectedChar) && IsLineEnding(actualChar))
				{
					expectedIndex = SkipLineEnding(expectedSpan, expectedIndex);
					actualIndex = SkipLineEnding(actualSpan, actualIndex);
				}
				else if (ignoreAllWhiteSpace && (IsWhiteSpace(expectedChar) || IsWhiteSpace(actualChar)))
				{
					expectedIndex = SkipWhitespace(expectedSpan, expectedIndex);
					actualIndex = SkipWhitespace(actualSpan, actualIndex);
				}
				else if (ignoreWhiteSpaceDifferences && IsWhiteSpace(expectedChar) && IsWhiteSpace(actualChar))
				{
					expectedIndex = SkipWhitespace(expectedSpan, expectedIndex);
					actualIndex = SkipWhitespace(actualSpan, actualIndex);
				}
				else
				{
					if (ignoreCase)
					{
						expectedChar = char.ToUpperInvariant(expectedChar);
						actualChar = char.ToUpperInvariant(actualChar);
					}

					if (expectedChar != actualChar)
						break;

					expectedIndex++;
					actualIndex++;
				}
			}

			if (expectedIndex < expectedLength || actualIndex < actualLength)
				throw new EqualException(expectedSpan.ToString(), actualSpan.ToString(), expectedIndex, actualIndex);
		}

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal<T>(
			Span<T> expectedSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					Equal((ReadOnlySpan<T>)expectedSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal<T>(
			Span<T> expectedSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T> =>
					Equal((ReadOnlySpan<T>)expectedSpan, actualSpan);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal<T>(
			ReadOnlySpan<T> expectedSpan,
			Span<T> actualSpan)
				where T : IEquatable<T> =>
					Equal(expectedSpan, (ReadOnlySpan<T>)actualSpan);

		/// <summary>
		/// Verifies that two spans are equivalent.
		/// </summary>
		/// <param name="expectedSpan">The expected span value.</param>
		/// <param name="actualSpan">The actual span value.</param>
		/// <exception cref="EqualException">Thrown when the spans are not equivalent.</exception>
		public static void Equal<T>(
			ReadOnlySpan<T> expectedSpan,
			ReadOnlySpan<T> actualSpan)
				where T : IEquatable<T>
		{
			if (!expectedSpan.SequenceEqual(actualSpan))
				Equal<object>(expectedSpan.ToArray(), actualSpan.ToArray());
		}

		// ReadOnlySpan<char> helper methods

		static bool IsLineEnding(char c) =>
			c == '\r' || c == '\n';

		static bool IsWhiteSpace(char c)
		{
			const char mongolianVowelSeparator = '\u180E';
			const char zeroWidthSpace = '\u200B';
			const char zeroWidthNoBreakSpace = '\uFEFF';
			const char tabulation = '\u0009';

			var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

			return
				unicodeCategory == UnicodeCategory.SpaceSeparator ||
				c == mongolianVowelSeparator ||
				c == zeroWidthSpace ||
				c == zeroWidthNoBreakSpace ||
				c == tabulation;
		}

		static int SkipLineEnding(
			ReadOnlySpan<char> value,
			int index)
		{
			if (value[index] == '\r')
				++index;

			if (index < value.Length && value[index] == '\n')
				++index;

			return index;
		}

		static int SkipWhitespace(
			ReadOnlySpan<char> value,
			int index)
		{
			while (index < value.Length)
			{
				if (IsWhiteSpace(value[index]))
					index++;
				else
					return index;
			}

			return index;
		}
	}
}

#endif
