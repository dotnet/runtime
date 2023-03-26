#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a string does not end with the expected value.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class EndsWithException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="EndsWithException"/> class.
		/// </summary>
		/// <param name="expected">The expected value that should've ended <paramref name="actual"/></param>
		/// <param name="actual">The actual value</param>
		public EndsWithException(
#if XUNIT_NULLABLE
			string? expected,
			string? actual) :
#else
			string expected,
			string actual) :
#endif
				base($"Assert.EndsWith() Failure:{Environment.NewLine}Expected: {ShortenExpected(expected, actual) ?? "(null)"}{Environment.NewLine}Actual:   {ShortenActual(expected, actual) ?? "(null)"}")
		{ }

#if XUNIT_NULLABLE
		static string? ShortenExpected(
			string? expected,
			string? actual)
#else
		static string ShortenExpected(
			string expected,
			string actual)
#endif
		{
			if (expected == null || actual == null || actual.Length <= expected.Length)
				return expected;

			return "   " + expected;
		}

#if XUNIT_NULLABLE
		static string? ShortenActual(
			string? expected,
			string? actual)
#else
		static string ShortenActual(
			string expected,
			string actual)
#endif
		{
			if (expected == null || actual == null || actual.Length <= expected.Length)
				return actual;

			return "···" + actual.Substring(actual.Length - expected.Length);
		}
	}
}
