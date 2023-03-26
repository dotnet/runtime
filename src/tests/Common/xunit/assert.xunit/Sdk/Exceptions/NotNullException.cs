#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when an object is unexpectedly null.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class NotNullException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NotNullException"/> class.
		/// </summary>
		public NotNullException() :
			base("Assert.NotNull() Failure")
		{ }
	}
}
