#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when two values are unexpectedly equal.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class NotEqualException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NotEqualException"/> class.
		/// </summary>
		public NotEqualException(
#if XUNIT_NULLABLE
			string? expected,
			string? actual) :
#else
			string expected,
			string actual) :
#endif
				base($"Not {expected ?? "(null)"}", actual ?? "(null)", "Assert.NotEqual() Failure")
		{ }
	}
}
