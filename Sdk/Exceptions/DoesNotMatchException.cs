#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a string unexpectedly matches a regular expression.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class DoesNotMatchException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="DoesNotMatchException"/> class.
		/// </summary>
		/// <param name="expectedRegexPattern">The regular expression pattern expected not to match</param>
		/// <param name="actual">The actual value</param>
		public DoesNotMatchException(
#if XUNIT_NULLABLE
			string expectedRegexPattern,
			object? actual) :
#else
			string expectedRegexPattern,
			object actual) :
#endif
				base($"Assert.DoesNotMatch() Failure:{Environment.NewLine}Regex: {expectedRegexPattern}{Environment.NewLine}Value: {actual}")
		{ }
	}
}
