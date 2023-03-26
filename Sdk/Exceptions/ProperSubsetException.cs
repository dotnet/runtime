#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a set is not a proper subset of another set.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class ProperSubsetException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ProperSubsetException"/> class.
		/// </summary>
		/// <param name="expected">The expected value</param>
		/// <param name="actual">The actual value</param>
#if XUNIT_NULLABLE
		public ProperSubsetException(
			IEnumerable expected,
			IEnumerable? actual) :
#else
		public ProperSubsetException(
			IEnumerable expected,
			IEnumerable actual) :
#endif
				base(expected, actual, "Assert.ProperSubset() Failure")
		{ }
	}
}
