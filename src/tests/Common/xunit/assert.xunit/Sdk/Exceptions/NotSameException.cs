#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when two values are unexpected the same instance.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class NotSameException : XunitException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NotSameException"/> class.
		/// </summary>
		public NotSameException() :
			base("Assert.NotSame() Failure")
		{ }
	}
}
