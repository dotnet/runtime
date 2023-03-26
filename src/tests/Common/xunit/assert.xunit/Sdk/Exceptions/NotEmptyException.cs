#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a collection is unexpectedly empty.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class NotEmptyException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NotEmptyException"/> class.
		/// </summary>
		public NotEmptyException() :
			base("Assert.NotEmpty() Failure")
		{ }
	}
}
