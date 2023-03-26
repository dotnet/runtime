#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections.Generic;
using System.Linq;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when two values are unexpectedly not equal.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class EquivalentException : AssertActualExpectedException
	{
#if XUNIT_NULLABLE
		readonly string? message;
#else
		readonly string message;
#endif

		EquivalentException(string message) :
			base(null, null, null)
		{
			this.message = message;
		}

		EquivalentException(
#if XUNIT_NULLABLE
			object? expected,
			object? actual,
			string messageSuffix,
			string? expectedTitle = null,
			string? actualTitle = null) :
#else
			object expected,
			object actual,
			string messageSuffix,
			string expectedTitle = null,
			string actualTitle = null) :
#endif
				base(expected, actual, "Assert.Equivalent() Failure" + messageSuffix, expectedTitle, actualTitle)
		{ }

		/// <inheritdoc/>
		public override string Message =>
			message ?? base.Message;

		static string FormatMemberNameList(
			IEnumerable<string> memberNames,
			string prefix) =>
				"[" + string.Join(", ", memberNames.Select(k => $"\"{prefix}{k}\"")) + "]";

		/// <summary>
		/// Creates a new instance of <see cref="EquivalentException"/> which shows a message that indicates
		/// a circular reference was discovered.
		/// </summary>
		/// <param name="memberName">The name of the member that caused the circular reference</param>
		public static EquivalentException ForCircularReference(string memberName) =>
			new EquivalentException($"Assert.Equivalent() Failure: Circular reference found in '{memberName}'");

		/// <summary>
		/// Creates a new instance of <see cref="EquivalentException"/> which shows a message that indicates
		/// that the list of available members does not match.
		/// </summary>
		/// <param name="expectedMemberNames">The expected member names</param>
		/// <param name="actualMemberNames">The actual member names</param>
		/// <param name="prefix">The prefix to be applied to the member names (may be an empty string for a
		/// top-level object, or a name in "member." format used as a prefix to show the member name list)</param>
		public static EquivalentException ForMemberListMismatch(
			IEnumerable<string> expectedMemberNames,
			IEnumerable<string> actualMemberNames,
			string prefix)
		{
			return new EquivalentException(
				FormatMemberNameList(expectedMemberNames, prefix),
				FormatMemberNameList(actualMemberNames, prefix),
				": Mismatched member list"
			);
		}

		/// <summary>
		/// Creates a new instance of <see cref="EquivalentException"/> which shows a message that indicates
		/// that the fault comes from an individual value mismatch one of the members.
		/// </summary>
		/// <param name="expected">The expected member value</param>
		/// <param name="actual">The actual member value</param>
		/// <param name="memberName">The name of the mismatched member (may be an empty string for a
		/// top-level object)</param>
		public static EquivalentException ForMemberValueMismatch(
#if XUNIT_NULLABLE
			object? expected,
			object? actual,
#else
			object expected,
			object actual,
#endif
			string memberName) =>
				new EquivalentException(
					expected,
					actual,
					memberName == string.Empty ? string.Empty : $": Mismatched value on member '{memberName}'"
				);

		/// <summary>
		/// Creates a new instance of <see cref="EquivalentException"/> which shows a message that indicates
		/// a value was missing from the <paramref name="actual"/> collection.
		/// </summary>
		/// <param name="expected">The object that was expected to be found in <paramref name="actual"/> collection.</param>
		/// <param name="actual">The actual collection which was missing the object.</param>
		/// <param name="memberName">The name of the member that was being inspected (may be an empty
		/// string for a top-level collection)</param>
		public static EquivalentException ForMissingCollectionValue(
#if XUNIT_NULLABLE
			object? expected,
			IEnumerable<object?> actual,
#else
			object expected,
			IEnumerable<object> actual,
#endif
			string memberName) =>
				new EquivalentException(
					expected,
					ArgumentFormatter.Format(actual),
					$": Collection value not found{(memberName == string.Empty ? string.Empty : $" in member '{memberName}'")}",
					actualTitle: "In"
				);

		/// <summary>
		/// Creates a new instance of <see cref="EquivalentException"/> which shows a message that indicates
		/// that <paramref name="actual"/> contained one or more values that were not specified
		/// in <paramref name="expected"/>.
		/// </summary>
		/// <param name="expected">The values expected to be found in the <paramref name="actual"/>
		/// collection.</param>
		/// <param name="actual">The actual collection values.</param>
		/// <param name="actualLeftovers">The values from <paramref name="actual"/> that did not have
		/// matching <paramref name="expected"/> values</param>
		/// <param name="memberName">The name of the member that was being inspected (may be an empty
		/// string for a top-level collection)</param>
		public static EquivalentException ForExtraCollectionValue(
#if XUNIT_NULLABLE
			IEnumerable<object?> expected,
			IEnumerable<object?> actual,
			IEnumerable<object?> actualLeftovers,
#else
			IEnumerable<object> expected,
			IEnumerable<object> actual,
			IEnumerable<object> actualLeftovers,
#endif
			string memberName) =>
				new EquivalentException(
					ArgumentFormatter.Format(expected),
					$"{ArgumentFormatter.Format(actualLeftovers)} left over from {ArgumentFormatter.Format(actual)}",
					$": Extra values found{(memberName == string.Empty ? string.Empty : $" in member '{memberName}'")}"
				);
	}
}
