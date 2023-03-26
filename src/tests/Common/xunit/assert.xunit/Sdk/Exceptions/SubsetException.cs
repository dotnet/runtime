#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a set is not a subset of another set.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class SubsetException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SubsetException"/> class.
		/// </summary>
#if XUNIT_NULLABLE
		public SubsetException(IEnumerable expected, IEnumerable? actual)
#else
		public SubsetException(IEnumerable expected, IEnumerable actual)
#endif
			: base(expected, actual, "Assert.Subset() Failure")
		{ }
	}
}
