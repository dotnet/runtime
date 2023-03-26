#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a string does not match a regular expression.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class MatchesException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="MatchesException"/> class.
		/// </summary>
		/// <param name="expectedRegexPattern">The expected regular expression pattern</param>
		/// <param name="actual">The actual value</param>
		public MatchesException(
#if XUNIT_NULLABLE
			string? expectedRegexPattern,
			object? actual) :
#else
			string expectedRegexPattern,
			object actual) :
#endif
				base($"Assert.Matches() Failure:{Environment.NewLine}Regex: {expectedRegexPattern}{Environment.NewLine}Value: {actual}")
		{ }
	}
}
