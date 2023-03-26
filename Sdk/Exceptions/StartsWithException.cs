#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a string does not start with the expected value.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class StartsWithException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="StartsWithException"/> class.
		/// </summary>
		/// <param name="expected">The expected string value</param>
		/// <param name="actual">The actual value</param>
#if XUNIT_NULLABLE
		public StartsWithException(
			string? expected,
			string? actual) :
#else
		public StartsWithException(
			string expected,
			string actual) :
#endif
				base($"Assert.StartsWith() Failure:{Environment.NewLine}Expected: {expected ?? "(null)"}{Environment.NewLine}Actual:   {ShortenActual(expected, actual) ?? "(null)"}")
		{ }

#if XUNIT_NULLABLE
		static string? ShortenActual(string? expected, string? actual)
#else
		static string ShortenActual(string expected, string actual)
#endif
		{
			if (expected == null || actual == null || actual.Length <= expected.Length)
				return actual;

			return actual.Substring(0, expected.Length) + "...";
		}
	}
}
