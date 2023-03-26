#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when an object reference is unexpectedly not null.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class NullException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NullException"/> class.
		/// </summary>
		/// <param name="actual">The actual non-<c>null</c> value</param>
		public NullException(object actual) :
			base(null, actual, "Assert.Null() Failure")
		{ }
	}
}
