#if XUNIT_SPAN

#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
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
		// NOTE: there is an implicit conversion operator on Memory<T> to ReadOnlyMemory<T> - however, I have found that the compiler sometimes struggles
		// with identifying the proper methods to use, thus I have overloaded quite a few of the assertions in terms of supplying both
		// Memory and ReadOnlyMemory based methods

		// NOTE: we could consider StartsWith<T> and EndsWith<T> with both arguments as ReadOnlyMemory<T>, and use the Memory extension methods on Span to check difference
		// BUT: the current Exceptions for startswith and endswith are only built for string types, so those would need a change (or new non-string versions created).

		// NOTE: Memory and ReadonlyMemory, even when null, are coerced into empty arrays of the specified type when a value is grabbed. Thus some of the code below
		// for null scenarios looks odd, but is safe and correct.

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			Memory<char> expectedSubMemory,
			Memory<char> actualMemory) =>
				Contains((ReadOnlyMemory<char>)expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			Memory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory) =>
				Contains((ReadOnlyMemory<char>)expectedSubMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			ReadOnlyMemory<char> expectedSubMemory,
			Memory<char> actualMemory) =>
				Contains(expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			ReadOnlyMemory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory) =>
				Contains(expectedSubMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			Memory<char> expectedSubMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains((ReadOnlyMemory<char>)expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			Memory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains((ReadOnlyMemory<char>)expectedSubMemory, actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			ReadOnlyMemory<char> expectedSubMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				Contains(expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains(
			ReadOnlyMemory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			GuardArgumentNotNull(nameof(expectedSubMemory), expectedSubMemory);

			Contains(expectedSubMemory.Span, actualMemory.Span, comparisonType);
		}

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains<T>(
			Memory<T> expectedSubMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					Contains((ReadOnlyMemory<T>)expectedSubMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains<T>(
			Memory<T> expectedSubMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T> =>
					Contains((ReadOnlyMemory<T>)expectedSubMemory, actualMemory);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains<T>(
			ReadOnlyMemory<T> expectedSubMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					Contains(expectedSubMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that a Memory contains a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="ContainsException">Thrown when the sub-Memory is not present inside the Memory</exception>
		public static void Contains<T>(
			ReadOnlyMemory<T> expectedSubMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T>
		{
			GuardArgumentNotNull(nameof(expectedSubMemory), expectedSubMemory);

			if (actualMemory.Span.IndexOf(expectedSubMemory.Span) < 0)
				throw new ContainsException(expectedSubMemory, actualMemory);
		}

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			Memory<char> expectedSubMemory,
			Memory<char> actualMemory) =>
				DoesNotContain((ReadOnlyMemory<char>)expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			Memory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory) =>
				DoesNotContain((ReadOnlyMemory<char>)expectedSubMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			ReadOnlyMemory<char> expectedSubMemory,
			Memory<char> actualMemory) =>
				DoesNotContain(expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			ReadOnlyMemory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory) =>
				DoesNotContain(expectedSubMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			Memory<char> expectedSubMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain((ReadOnlyMemory<char>)expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			Memory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain((ReadOnlyMemory<char>)expectedSubMemory, actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			ReadOnlyMemory<char> expectedSubMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				DoesNotContain(expectedSubMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain(
			ReadOnlyMemory<char> expectedSubMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			GuardArgumentNotNull(nameof(expectedSubMemory), expectedSubMemory);

			DoesNotContain(expectedSubMemory.Span, actualMemory.Span, comparisonType);
		}

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain<T>(
			Memory<T> expectedSubMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					DoesNotContain((ReadOnlyMemory<T>)expectedSubMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain<T>(
			Memory<T> expectedSubMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T> =>
					DoesNotContain((ReadOnlyMemory<T>)expectedSubMemory, actualMemory);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain<T>(
			ReadOnlyMemory<T> expectedSubMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					DoesNotContain(expectedSubMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that a Memory does not contain a given sub-Memory
		/// </summary>
		/// <param name="expectedSubMemory">The sub-Memory expected not to be in the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="DoesNotContainException">Thrown when the sub-Memory is present inside the Memory</exception>
		public static void DoesNotContain<T>(
			ReadOnlyMemory<T> expectedSubMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T>
		{
			GuardArgumentNotNull(nameof(expectedSubMemory), expectedSubMemory);

			if (actualMemory.Span.IndexOf(expectedSubMemory.Span) > -1)
				throw new DoesNotContainException(expectedSubMemory, actualMemory);
		}

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			Memory<char> expectedStartMemory,
			Memory<char> actualMemory) =>
				StartsWith((ReadOnlyMemory<char>)expectedStartMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			Memory<char> expectedStartMemory,
			ReadOnlyMemory<char> actualMemory) =>
				StartsWith((ReadOnlyMemory<char>)expectedStartMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			ReadOnlyMemory<char> expectedStartMemory,
			Memory<char> actualMemory) =>
				StartsWith(expectedStartMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the default StringComparison.CurrentCulture comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			ReadOnlyMemory<char> expectedStartMemory,
			ReadOnlyMemory<char> actualMemory) =>
				StartsWith(expectedStartMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			Memory<char> expectedStartMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith((ReadOnlyMemory<char>)expectedStartMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			Memory<char> expectedStartMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith((ReadOnlyMemory<char>)expectedStartMemory, actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			ReadOnlyMemory<char> expectedStartMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				StartsWith(expectedStartMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory starts with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedStartMemory">The sub-Memory expected to be at the start of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="StartsWithException">Thrown when the Memory does not start with the expected subMemory</exception>
		public static void StartsWith(
			ReadOnlyMemory<char> expectedStartMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			GuardArgumentNotNull(nameof(expectedStartMemory), expectedStartMemory);

			StartsWith(expectedStartMemory.Span, actualMemory.Span, comparisonType);
		}

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			Memory<char> expectedEndMemory,
			Memory<char> actualMemory) =>
				EndsWith((ReadOnlyMemory<char>)expectedEndMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			Memory<char> expectedEndMemory,
			ReadOnlyMemory<char> actualMemory) =>
				EndsWith((ReadOnlyMemory<char>)expectedEndMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			ReadOnlyMemory<char> expectedEndMemory,
			Memory<char> actualMemory) =>
				EndsWith(expectedEndMemory, (ReadOnlyMemory<char>)actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the default <see cref="StringComparison.CurrentCulture"/> comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			ReadOnlyMemory<char> expectedEndMemory,
			ReadOnlyMemory<char> actualMemory) =>
				EndsWith(expectedEndMemory, actualMemory, StringComparison.CurrentCulture);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			Memory<char> expectedEndMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith((ReadOnlyMemory<char>)expectedEndMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			Memory<char> expectedEndMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith((ReadOnlyMemory<char>)expectedEndMemory, actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			ReadOnlyMemory<char> expectedEndMemory,
			Memory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture) =>
				EndsWith(expectedEndMemory, (ReadOnlyMemory<char>)actualMemory, comparisonType);

		/// <summary>
		/// Verifies that a Memory ends with a given sub-Memory, using the given comparison type.
		/// </summary>
		/// <param name="expectedEndMemory">The sub-Memory expected to be at the end of the Memory</param>
		/// <param name="actualMemory">The Memory to be inspected</param>
		/// <param name="comparisonType">The type of string comparison to perform</param>
		/// <exception cref="EndsWithException">Thrown when the Memory does not end with the expected subMemory</exception>
		public static void EndsWith(
			ReadOnlyMemory<char> expectedEndMemory,
			ReadOnlyMemory<char> actualMemory,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			GuardArgumentNotNull(nameof(expectedEndMemory), expectedEndMemory);

			EndsWith(expectedEndMemory.Span, actualMemory.Span, comparisonType);
		}

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			Memory<char> expectedMemory,
			Memory<char> actualMemory) =>
				Equal((ReadOnlyMemory<char>)expectedMemory, (ReadOnlyMemory<char>)actualMemory, false, false, false);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			Memory<char> expectedMemory,
			ReadOnlyMemory<char> actualMemory) =>
				Equal((ReadOnlyMemory<char>)expectedMemory, actualMemory, false, false, false);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			ReadOnlyMemory<char> expectedMemory,
			Memory<char> actualMemory) =>
				Equal(expectedMemory, (ReadOnlyMemory<char>)actualMemory, false, false, false);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			ReadOnlyMemory<char> expectedMemory,
			ReadOnlyMemory<char> actualMemory) =>
				Equal(expectedMemory, actualMemory, false, false, false);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			Memory<char> expectedMemory,
			Memory<char> actualMemory,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false) =>
				Equal((ReadOnlyMemory<char>)expectedMemory, (ReadOnlyMemory<char>)actualMemory, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			Memory<char> expectedMemory,
			ReadOnlyMemory<char> actualMemory,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false) =>
				Equal((ReadOnlyMemory<char>)expectedMemory, actualMemory, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			ReadOnlyMemory<char> expectedMemory,
			Memory<char> actualMemory,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false) =>
				Equal(expectedMemory, (ReadOnlyMemory<char>)actualMemory, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
		/// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
		/// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal(
			ReadOnlyMemory<char> expectedMemory,
			ReadOnlyMemory<char> actualMemory,
			bool ignoreCase = false,
			bool ignoreLineEndingDifferences = false,
			bool ignoreWhiteSpaceDifferences = false)
		{
			GuardArgumentNotNull(nameof(expectedMemory), expectedMemory);

			Equal(
				expectedMemory.Span,
				actualMemory.Span,
				ignoreCase,
				ignoreLineEndingDifferences,
				ignoreWhiteSpaceDifferences
			);
		}

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal<T>(
			Memory<T> expectedMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					Equal((ReadOnlyMemory<T>)expectedMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal<T>(
			Memory<T> expectedMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T> =>
					Equal((ReadOnlyMemory<T>)expectedMemory, actualMemory);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal<T>(
			ReadOnlyMemory<T> expectedMemory,
			Memory<T> actualMemory)
				where T : IEquatable<T> =>
					Equal(expectedMemory, (ReadOnlyMemory<T>)actualMemory);

		/// <summary>
		/// Verifies that two Memory values are equivalent.
		/// </summary>
		/// <param name="expectedMemory">The expected Memory value.</param>
		/// <param name="actualMemory">The actual Memory value.</param>
		/// <exception cref="EqualException">Thrown when the Memory values are not equivalent.</exception>
		public static void Equal<T>(
			ReadOnlyMemory<T> expectedMemory,
			ReadOnlyMemory<T> actualMemory)
				where T : IEquatable<T>
		{
			GuardArgumentNotNull(nameof(expectedMemory), expectedMemory);

			if (!expectedMemory.Span.SequenceEqual(actualMemory.Span))
				Equal<object>(expectedMemory.Span.ToArray(), actualMemory.Span.ToArray());
		}
	}
}

#endif
