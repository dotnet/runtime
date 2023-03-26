#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a test should be skipped.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class SkipException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SkipException"/> class. This is a special
		/// exception that, when thrown, will cause xUnit.net to mark your test as skipped
		/// rather than failed.
		/// </summary>
		public SkipException(string message)
			: base($"{DynamicSkipToken.Value}{message}")
		{ }
	}
}
