#if XUNIT_NULLABLE
#nullable enable
#endif

using System.Collections;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception thrown when a set is not a proper superset of another set.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class ProperSupersetException : AssertActualExpectedException
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ProperSupersetException"/> class.
		/// </summary>
#if XUNIT_NULLABLE
		public ProperSupersetException(IEnumerable expected, IEnumerable? actual)
#else
		public ProperSupersetException(IEnumerable expected, IEnumerable actual)
#endif
			: base(expected, actual, "Assert.ProperSuperset() Failure")
		{ }
	}
}
